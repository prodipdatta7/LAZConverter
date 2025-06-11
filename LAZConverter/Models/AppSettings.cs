using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LAZConverter.Models
{
    public class AppSettings
    {
        public string InputDirectory { get; set; } = string.Empty;
        public string OutputDirectory { get; set; } = string.Empty;
        public string PotreeConverterPath { get; set; } = string.Empty;
        public int ChunkSizeMB { get; set; } = 100; // Default chunk size in MB
        public int MaxConcurrentProcess { get; set; } = 2;
        public string TempDirectory { get; set; } = string.Empty;
    }

    public class ConversionResult
    {
        public string Id { get; set; } = string.Empty;
        public string InputFilePath { get; set; } = string.Empty;
        public string OutputDirectory { get; set; } = string.Empty;
        public bool IsSuccess { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public TimeSpan Duration => EndTime - StartTime;
        public long InputFileSizeBytes { get; set; }
        public List<string> OutputFiles { get; set; } = new();
    }

    public class FileChunk
    {
        public string Id { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long StartByte { get; set; }
        public long EndByte { get; set; }
        public long Size => EndByte - StartByte + 1;
        public string TempFilePath { get; set; } = string.Empty;
    }
}
