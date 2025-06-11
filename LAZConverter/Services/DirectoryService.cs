using LAZConverter.Contracts;
using LAZConverter.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LAZConverter.Services
{
    public class DirectoryService : IDirectoryService
    {
        private readonly ILogger<DirectoryService> _logger;
        private readonly AppSettings _appSettings;

        public DirectoryService(ILogger<DirectoryService> logger, AppSettings appSettings)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
        }
        public void CleanupTempDirectory()
        {
            try
            {
                if (Directory.Exists(_appSettings.TempDirectory))
                {
                    var tempFiles = Directory.GetFiles(_appSettings.TempDirectory, "*", SearchOption.AllDirectories);
                    foreach (var file in tempFiles)
                    {
                        try
                        {
                            File.Delete(file);
                            _logger.LogInformation("Deleted temporary file: {FileName}", Path.GetFileName(file));
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to delete temporary file: {FileName}", Path.GetFileName(file));
                        }
                    }
                    var tempDirs = Directory.GetDirectories(_appSettings.TempDirectory, "*", SearchOption.AllDirectories);
                    foreach (var dir in tempDirs)
                    {
                        try
                        {
                            Directory.Delete(dir, true);
                            _logger.LogInformation("Deleted temporary directory: {Directory}", dir);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to delete temporary directory: {Directory}", dir);
                        }
                    }
                    _logger.LogInformation("Temporary directory cleanup completed.");
                }
                else
                {
                    _logger.LogWarning("Temporary directory does not exist: {Directory}", _appSettings.TempDirectory);
                }
            }
            catch (Exception)
            {

                throw;
            }
        }

        public void EnsureDirectoryExists()
        {
            var directories = new[]
            {
                _appSettings.InputDirectory,
                _appSettings.OutputDirectory,
                _appSettings.TempDirectory
            };

            foreach (var directory in directories)
            {
                if(!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    _logger.LogInformation("Created directory: {Directory}", directory);
                }
                else
                {
                    _logger.LogInformation("Directory already exists: {Directory}", directory);
                }
            }
        }

        public List<string> GetLazFiles()
        {
            if (!Directory.Exists(_appSettings.InputDirectory))
            {
                _logger.LogWarning("Input directory does not exist: {Directory}", _appSettings.InputDirectory);
                return new List<string>();
            }

            var lazFiles = Directory.GetFiles(_appSettings.InputDirectory, "*.laz", SearchOption.AllDirectories).ToList();
            
            _logger.LogInformation("Found {Count} LAZ files in input directory: {Directory}", lazFiles.Count, _appSettings.InputDirectory);

            foreach (var file in lazFiles)
            {
                _logger.LogDebug("Found LAZ file: {FileName}", Path.GetFileName(file));
            }

            return lazFiles;
        }
    }
}
