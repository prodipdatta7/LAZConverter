using LAZConverter.Contracts;
using LAZConverter.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Diagnostics;

namespace LAZConverter.Services
{
    public class FileProcessorService : IFileProcessorService
    {
        private readonly ILogger<FileProcessorService> _logger;
        private readonly AppSettings _appSettings;
        private readonly SemaphoreSlim _semaphore;

        public FileProcessorService(ILogger<FileProcessorService> logger, AppSettings appSettings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
            _semaphore = new SemaphoreSlim(appSettings.MaxConcurrentProcess, appSettings.MaxConcurrentProcess);
        }
        public async Task<ConversionResult> ConvertLazFileAsync(string inputFilePath)
        {
            var conversionId = Guid.NewGuid().ToString("N");
            var conversionResult = new ConversionResult
            {
                Id = conversionId,
                InputFilePath = inputFilePath,
                StartTime = DateTime.UtcNow
            };

            try
            {
                _logger.LogInformation("Starting conversion for file: {FilePath} with ID: {ConversionId}", inputFilePath, conversionId);
                if (!File.Exists(inputFilePath))
                {
                    throw new FileNotFoundException("Input file not found", inputFilePath);
                }
                var fileInfo = new FileInfo(inputFilePath);
                conversionResult.InputFileSizeBytes = fileInfo.Length;

                // Create unique output directory for this conversion
                var outputDirectory = Path.Combine(_appSettings.OutputDirectory, conversionId);
                Directory.CreateDirectory(outputDirectory);
                conversionResult.OutputDirectory = outputDirectory;

                // Check if file needs chunking based on size
                var fileSizeMB = fileInfo.Length / (1024 * 1024);
                if (fileSizeMB > _appSettings.ChunkSizeMB)
                {
                    _logger.LogInformation("File size {FileSizeMB} MB exceeds chunk size {_appSettings.ChunkSizeMB} MB, chunking required.", fileSizeMB, _appSettings.ChunkSizeMB);
                    // Implement chunking logic here
                    await ProcessLargeFileInChunksAsync(inputFilePath, outputDirectory, conversionResult);
                }
                else
                {
                    _logger.LogInformation("File size {FileSizeMB} MB is within the chunk size limit, processing directly.", fileSizeMB);
                    // Process the file directly
                    await ProcessSingleFileAsync(inputFilePath, outputDirectory, conversionResult);
                }
                conversionResult.IsSuccess = true;
                _logger.LogInformation("Conversion completed successfully for file: {FilePath} with ID: {ConversionId}", inputFilePath, conversionId);
            }
            catch (Exception ex)
            {
                conversionResult.IsSuccess = false;
                conversionResult.ErrorMessage = ex.Message;
                _logger.LogError("Error in {MethodName}. Message: {ErrorMessage}. Details: {StackTrace}", nameof(ConvertLazFileAsync), ex.Message, ex.StackTrace);
            }
            finally
            {
                conversionResult.EndTime = DateTime.UtcNow;
                _logger.LogInformation("Conversion completed for file: {FilePath} with ID: {ConversionId}. Duration: {Duration}", inputFilePath, conversionId, conversionResult.Duration);
            }
            return conversionResult;
        }

        public async Task<List<ConversionResult>> ConvertMultipleLazFileAsync(IEnumerable<string> inputFilePaths)
        {
            var tasks = inputFilePaths.Select(async filePath =>
            {
                await _semaphore.WaitAsync();
                try
                {
                    return await ConvertLazFileAsync(filePath);
                }
                finally
                {
                    _semaphore.Release();
                }
            });

            return (await Task.WhenAll(tasks)).ToList();
        }

        private async Task ProcessSingleFileAsync(string inputFilePath, string outputDir, ConversionResult result)
        {
            await RunPotreeConverterAsync(inputFilePath, outputDir);
            result.OutputFiles.AddRange(GetOutputFiles(outputDir));
        }

        private async Task ProcessLargeFileInChunksAsync(string inputFilePath, string outputDir, ConversionResult result)
        {
            var chunks = CreateFileChunks(inputFilePath);
            var tasks = new List<Task>();
            foreach (var chunk in chunks)
            {
                try
                {
                    _logger.LogInformation("Processing chunk {ChunkId} ({StartByte}-{EndByte})",
                    chunk.Id, chunk.StartByte, chunk.EndByte);

                    // Create temporary chunk file
                    await CreateChunkFileAsync(inputFilePath, chunk);

                    // Process the chunk
                    var chunkOutputDir = Path.Combine(outputDir, $"chunk_{chunk.Id}");
                    Directory.CreateDirectory(chunkOutputDir);

                    await RunPotreeConverterAsync(chunk.TempFilePath, chunkOutputDir);

                    var chunkOutputFiles = GetOutputFiles(chunkOutputDir);
                    result.OutputFiles.AddRange(chunkOutputFiles);

                    // Clean up temporary chunk file
                    if (File.Exists(chunk.TempFilePath))
                    {
                        File.Delete(chunk.TempFilePath);
                    }

                    _logger.LogInformation("Chunk {ChunkId} processed successfully", chunk.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process chunk {ChunkId}", chunk.Id);
                    throw;
                }
            }

            await MergeChunkResultsAsync(outputDir, result);
        }

        private List<FileChunk> CreateFileChunks(string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            var chunkSizeBytes = (long)_appSettings.ChunkSizeMB * 1024 * 1024;
            var chunks = new List<FileChunk>();

            long currentPosition = 0;
            int chunkIndex = 0;

            while (currentPosition < fileInfo.Length)
            {
                var endPosition = Math.Min(currentPosition + chunkSizeBytes - 1, fileInfo.Length - 1);

                var chunk = new FileChunk
                {
                    Id = $"{chunkIndex:D3}",
                    FilePath = filePath,
                    StartByte = currentPosition,
                    EndByte = endPosition,
                    TempFilePath = Path.Combine(_appSettings.TempDirectory, $"chunk_{chunkIndex:D3}_{Guid.NewGuid():N}.laz")
                };
                chunks.Add(chunk);
                currentPosition += endPosition + 1;
                chunkIndex++;
            }
            return chunks;
        }

        private async Task CreateChunkFileAsync(string sourceFilePath, FileChunk chunk)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(chunk.TempFilePath)!);

            using var sourceStream = new FileStream(sourceFilePath, FileMode.Open, FileAccess.Read);
            using var targetStream = new FileStream(chunk.TempFilePath, FileMode.Create, FileAccess.Write);

            sourceStream.Seek(chunk.StartByte, SeekOrigin.Begin);

            var buffer = new byte[8192];
            long remainingBytes = chunk.Size;

            while (remainingBytes > 0)
            {
                int bytesToRead = (int)Math.Min(buffer.Length, remainingBytes);
                int bytesRead = await sourceStream.ReadAsync(buffer, 0, bytesToRead);
                if (bytesRead == 0)
                    break; // End of file reached
                await targetStream.WriteAsync(buffer, 0, bytesRead);
                remainingBytes -= bytesRead;
            }

        }

        private async Task RunPotreeConverterAsync(string inputFilePath, string outputDirectory)
        {
            // Placeholder for actual PotreeConverter execution logic
            // This method should call the PotreeConverter executable with the necessary arguments
            // and handle the output files accordingly.
            await Task.Delay(1000); // Simulate async operation
            if (!File.Exists(_appSettings.PotreeConverterPath))
            {
                throw new FileNotFoundException("PotreeConverter executable not found", _appSettings.PotreeConverterPath);
            }

            var arguments = $"\"{inputFilePath}\" -o \"{outputDirectory}\" --overwrite";

            var processStartInfo = new ProcessStartInfo
            {
                FileName = _appSettings.PotreeConverterPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _logger.LogInformation("Running PotreeConverter for file: {FilePath} to output directory: {OutputDirectory}", inputFilePath, outputDirectory);

            using var process = new Process { StartInfo = processStartInfo };

            var outputBuilder = new System.Text.StringBuilder();
            var errorBuilder = new System.Text.StringBuilder();

            process.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    outputBuilder.AppendLine(args.Data);
                    _logger.LogInformation("PotreeConverter Output: {Output}", args.Data);
                }
            };
            process.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrEmpty(args.Data))
                {
                    errorBuilder.AppendLine(args.Data);
                    _logger.LogError("PotreeConverter Error: {Error}", args.Data);
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var errorMessage = $"PotreeConverter failed with exit code {process.ExitCode}. Output: {outputBuilder}, Error: {errorBuilder}";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }
            _logger.LogInformation("PotreeConverter completed successfully for file: {FilePath}", inputFilePath);
        }

        private List<string> GetOutputFiles(string outputDir)
        {
            if (!Directory.Exists(outputDir))
                return new List<string>();

            return Directory.GetFiles(outputDir, "*", SearchOption.AllDirectories).ToList();
        }

        private async Task MergeChunkResultsAsync(string outputDir, ConversionResult result)
        {
            // This is a placeholder for merging chunk results if needed
            // The implementation would depend on the specific requirements
            // For now, we'll just create a metadata file with chunk information

            var metadataFilePath = Path.Combine(outputDir, "conversion_metadata.json");

            var metadata = new
            {
                ConversionId = result.Id,
                InputFile = result.InputFilePath,
                ChunkCount = result.OutputFiles.Count,
                ProcessedAt = DateTime.UtcNow,
                TotalFiles = result.OutputFiles.Count
            };

            await File.WriteAllTextAsync(metadataFilePath, JsonConvert.SerializeObject(metadata, Formatting.Indented));

            result.OutputFiles.Add(metadataFilePath);
        }
    }
}
