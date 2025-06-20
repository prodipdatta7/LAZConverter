﻿using LAZConverter.Contracts;
using LAZConverter.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LAZConverter
{
    public class Application
    {
        private readonly ILogger<Application> _logger;
        private readonly IDirectoryService _directoryService;
        private readonly IFileProcessorService _fileProcessorService;
        private readonly AppSettings _appSettings;

        public Application(
            ILogger<Application> logger,
            IDirectoryService directoryService,
            IFileProcessorService fileProcessorService,
            AppSettings appSettings)
        {
            _logger = logger;
            _directoryService = directoryService;
            _fileProcessorService = fileProcessorService;
            _appSettings = appSettings;
        }

        public async Task RunAsync(string[] args)
        {
            _logger.LogInformation("Application starting...");

            // Display configuration
            DisplayConfiguration();

            // Ensure directories exist
            _directoryService.EnsureDirectoryExists();

            // Clean up temporary files from previous runs
            _directoryService.CleanupTempDirectory();

            // Check if PotreeConverter exists
            if (!File.Exists(_appSettings.PotreeConverterPath))
            {
                _logger.LogError("PotreeConverter not found at: {Path}", _appSettings.PotreeConverterPath);
                _logger.LogError("Please ensure PotreeConverter is installed and the path is correct in appsettings.json");
                return;
            }

            // Get LAZ files
            var lazFiles = _directoryService.GetLazFiles();

            if (!lazFiles.Any())
            {
                _logger.LogWarning("No LAZ files found in input directory: {Directory}", _appSettings.InputDirectory);
                _logger.LogInformation("Please place LAZ files in the input directory and run the application again.");
                return;
            }

            // Process based on command line arguments
            if (args.Length > 0)
            {
                await ProcessSpecificFiles(args, lazFiles);
            }
            else
            {
                await ProcessAllFiles(lazFiles);
            }

            // Clean up temporary files
            _directoryService.CleanupTempDirectory();

            _logger.LogInformation("Application completed successfully");
        }

        private void DisplayConfiguration()
        {
            Console.WriteLine("\nConfiguration:");
            Console.WriteLine($"  Input Directory: {_appSettings.InputDirectory}");
            Console.WriteLine($"  Output Directory: {_appSettings.OutputDirectory}");
            Console.WriteLine($"  PotreeConverter Path: {_appSettings.PotreeConverterPath}");
            Console.WriteLine($"  Chunk Size: {_appSettings.ChunkSizeMB}MB");
            Console.WriteLine($"  Max Concurrent Processes: {_appSettings.MaxConcurrentProcess}");
            Console.WriteLine($"  Temp Directory: {_appSettings.TempDirectory}");
            Console.WriteLine();
        }

        private async Task ProcessAllFiles(List<string> lazFiles)
        {
            Console.WriteLine($"\nProcessing {lazFiles.Count} LAZ files...");
            Console.WriteLine("----------------------------------------");

            var startTime = DateTime.UtcNow;
            var results = await _fileProcessorService.ConvertMultipleLazFileAsync(lazFiles);
            var endTime = DateTime.UtcNow;

            DisplayResults(results, endTime - startTime);
        }

        private async Task ProcessSpecificFiles(string[] fileNames, List<string> allLazFiles)
        {
            var filesToProcess = new List<string>();

            foreach (var fileName in fileNames)
            {
                var matchingFile = allLazFiles.FirstOrDefault(f =>
                    Path.GetFileName(f).Equals(fileName, StringComparison.OrdinalIgnoreCase));

                if (matchingFile != null)
                {
                    filesToProcess.Add(matchingFile);
                }
                else
                {
                    _logger.LogWarning("File not found: {FileName}", fileName);
                }
            }

            if (filesToProcess.Any())
            {
                Console.WriteLine($"\nProcessing {filesToProcess.Count} specified LAZ files...");
                Console.WriteLine("----------------------------------------");

                var startTime = DateTime.UtcNow;
                var results = await _fileProcessorService.ConvertMultipleLazFileAsync(filesToProcess);
                var endTime = DateTime.UtcNow;

                DisplayResults(results, endTime - startTime);
            }
            else
            {
                _logger.LogError("No valid files found to process");
            }
        }

        private void DisplayResults(List<ConversionResult> results, TimeSpan totalDuration)
        {
            Console.WriteLine("\nConversion Results:");
            Console.WriteLine("===================");

            var successCount = results.Count(r => r.IsSuccess);
            var failureCount = results.Count(r => !r.IsSuccess);

            Console.WriteLine($"Total Files: {results.Count}");
            Console.WriteLine($"Successful: {successCount}");
            Console.WriteLine($"Failed: {failureCount}");
            Console.WriteLine($"Total Duration: {totalDuration:hh\\:mm\\:ss}");
            Console.WriteLine();

            foreach (var result in results)
            {
                Console.WriteLine($"File: {Path.GetFileName(result.InputFilePath)}");
                Console.WriteLine($"  ID: {result.Id}");
                Console.WriteLine($"  Status: {(result.IsSuccess ? "SUCCESS" : "FAILED")}");
                Console.WriteLine($"  Duration: {result.Duration:hh\\:mm\\:ss}");
                Console.WriteLine($"  Input Size: {result.InputFileSizeBytes / (1024 * 1024):F2} MB");

                if (result.IsSuccess)
                {
                    Console.WriteLine($"  Output Directory: {result.OutputDirectory}");
                    Console.WriteLine($"  Output Files: {result.OutputFiles.Count}");
                }
                else
                {
                    Console.WriteLine($"  Error: {result.ErrorMessage}");
                }

                Console.WriteLine();
            }

            // Save results to file
            SaveResultsToFile(results);
        }

        private void SaveResultsToFile(List<ConversionResult> results)
        {
            try
            {
                var resultsFilePath = Path.Combine(_appSettings.OutputDirectory, $"conversion_results_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");
                var jsonResults = Newtonsoft.Json.JsonConvert.SerializeObject(results, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(resultsFilePath, jsonResults);

                _logger.LogInformation("Results saved to: {FilePath}", resultsFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save results to file");
            }
        }
    }
}
