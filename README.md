# LAZ to Point Cloud Converter

A .NET 9 console application that converts LAZ files to point clouds using PotreeConverter. This application is designed to handle large files by processing them in chunks and supports concurrent processing for multiple files.

## Features

- **Chunked Processing**: Large LAZ files are automatically split into configurable chunks for processing
- **Concurrent Processing**: Multiple files can be processed simultaneously with configurable concurrency limits
- **Unique Output Identification**: Each conversion gets a unique identifier to separate outputs
- **Comprehensive Logging**: Detailed logging with configurable levels
- **Error Handling**: Robust error handling with detailed error reporting
- **Configuration-based**: Easy configuration through `appsettings.json`

## Prerequisites

1. **.NET 9 SDK** - Download from [Microsoft .NET](https://dotnet.microsoft.com/download/dotnet/9.0)
2. **PotreeConverter** - Download from [PotreeConverter GitHub](https://github.com/potree/PotreeConverter)

## Installation & Setup

### 1. Install PotreeConverter

1. Download PotreeConverter from the official GitHub repository
2. Extract it to a location (e.g., `C:\PotreeConverter\`)
3. Note the path to `PotreeConverter.exe`

### 2. Create Project Structure

```bash
mkdir LAZConverter
cd LAZConverter
```

### 3. Create the Project Files

1. Create `LAZConverter.csproj` with the provided content
2. Create `appsettings.json` with the provided content
3. Create the following folder structure:
   ```
   LAZConverter/
   ├── Models/
   ├── Services/
   ├── Program.cs
   ├── LAZConverter.csproj
   └── appsettings.json
   ```

### 4. Configure Application Settings

Edit `appsettings.json` to match your environment:

```json
{
  "AppSettings": {
    "InputDirectory": "C:\\LAZFiles\\Input",           // Where your LAZ files are located
    "OutputDirectory": "C:\\LAZFiles\\Output",         // Where converted files will be saved
    "PotreeConverterPath": "C:\\PotreeConverter\\PotreeConverter.exe", // Path to PotreeConverter
    "ChunkSizeMB": 100,                               // Size threshold for chunking (MB)
    "MaxConcurrentProcesses": 2,                      // Number of concurrent conversions
    "TempDirectory": "C:\\LAZFiles\\Temp"             // Temporary files location
  }
}
```

### 5. Create Required Directories

Create the directories specified in your configuration:

```bash
mkdir C:\LAZFiles\Input
mkdir C:\LAZFiles\Output
mkdir C:\LAZFiles\Temp
```

### 6. Build the Application

```bash
dotnet restore
dotnet build
```

## Usage

### 1. Place LAZ Files

Copy your LAZ files to the input directory specified in `appsettings.json` (default: `C:\LAZFiles\Input\`)

### 2. Run the Application

**Process all LAZ files in the input directory:**
```bash
dotnet run
```

**Process specific files:**
```bash
dotnet run file1.laz file2.laz
```

### 3. Monitor Output

The application will:
- Display real-time progress in the console
- Log detailed information about the conversion process
- Create unique output directories for each conversion
- Generate a results summary JSON file

## Output Structure

Each conversion creates a unique output directory structure:

```
C:\LAZFiles\Output\
├── [conversion-id-1]/
│   ├── converted_files/
│   ├── metadata.json
│   └── conversion_metadata.json
├── [conversion-id-2]/
│   ├── chunk_001/
│   ├── chunk_002/
│   └── conversion_metadata.json
└── conversion_results_[timestamp].json
```

## Configuration Options

| Setting | Description | Default |
|---------|-------------|---------|
| `InputDirectory` | Directory containing LAZ files | `C:\LAZFiles\Input` |
| `OutputDirectory` | Directory for converted outputs | `C:\LAZFiles\Output` |
| `PotreeConverterPath` | Path to PotreeConverter executable | `C:\PotreeConverter\PotreeConverter.exe` |
| `ChunkSizeMB` | File size threshold for chunking (MB) | `100` |
| `MaxConcurrentProcesses` | Maximum concurrent conversions | `2` |
| `TempDirectory` | Temporary files directory | `C:\LAZFiles\Temp` |

## File Processing Logic

### Small Files (≤ ChunkSizeMB)
- Processed directly with PotreeConverter
- Single output directory per file

### Large Files (> ChunkSizeMB)
- Split into chunks of configured size
- Each chunk processed separately
- Results organized in numbered chunk directories
- Metadata file created with chunk information

## Logging

The application provides comprehensive logging:
- **Console Output**: Real-time progress and status
- **File Logging**: Detailed logs (if configured)
- **Results JSON**: Complete conversion results with timing and file information

Log levels can be configured in `appsettings.json`:
```json
"Logging": {
  "LogLevel": {
    "Default": "Information",
    "LAZConverter": "Debug"
  }
}
```

## Error Handling

The application handles various error scenarios:
- Missing input files
- Invalid PotreeConverter path
- Insufficient disk space
- Corrupted LAZ files
- Process failures

All errors are logged with detailed information and the application continues processing remaining files.

## Performance Considerations

### Memory Usage
- Chunk processing minimizes memory usage for large files
- Configurable chunk size based on available memory

### Concurrent Processing
- Configurable concurrency to balance performance and resource usage
- Semaphore-based throttling prevents resource exhaustion

### Disk Space
- Monitor available disk space in output and temp directories
- Large files may require significant temporary storage during chunking

## Troubleshooting

### Common Issues

**PotreeConverter not found:**
```
Error: PotreeConverter not found at: [path]
```
- Verify PotreeConverter is installed
- Check the path in `appsettings.json`
- Ensure the executable has proper permissions

**No LAZ files found:**
```
Warning: No LAZ files found in input directory
```
- Verify LAZ files are in the correct input directory
- Check file extensions (.laz)
- Ensure files are not corrupted

**Insufficient permissions:**
- Ensure the application has read/write permissions for all configured directories
- Run as administrator if necessary

**Out of disk space:**
- Monitor disk space in output and temp directories
- Consider increasing chunk size to reduce temporary storage needs
- Clean up old conversions regularly

### Debug Mode

Enable debug logging for detailed troubleshooting:
```json
"Logging": {
  "LogLevel": {
    "Default": "Debug"
  }
}
```

## Integration Notes

This demo application simulates the backend processing component of a larger system. In the full implementation:

- **File Upload**: Web frontend uploads LAZ files to blob storage
- **Processing Queue**: Backend retrieves files from blob storage for processing
- **Chunked Download**: Large files downloaded in chunks from blob storage
- **Result Storage**: Converted point clouds stored back to cloud storage
- **Status Updates**: Real-time processing status updates to frontend

The current implementation uses local file system instead of blob storage for demonstration purposes.

## Example Output

```
LAZ to Point Cloud Converter
============================

Configuration:
  Input Directory: C:\LAZFiles\Input
  Output Directory: C:\LAZFiles\Output
  PotreeConverter Path: C:\PotreeConverter\PotreeConverter.exe
  Chunk Size: 100MB
  Max Concurrent Processes: 2
  Temp Directory: C:\LAZFiles\Temp

Found 3 LAZ files in input directory

Processing 3 LAZ files...
----------------------------------------
[INFO] Starting conversion for file: sample1.laz with ID: a1b2c3d4e5f6
[INFO] File size (45MB) below chunk threshold. Processing directly.
[INFO] Running PotreeConverter: "C:\LAZFiles\Input\sample1.laz" -o "C:\LAZFiles\Output\a1b2c3d4e5f6"
[INFO] Conversion completed successfully for ID: a1b2c3d4e5f6

Conversion Results:
===================
Total Files: 3
Successful: 3
Failed: 0
Total Duration: 00:05:23

File: sample1.laz
  ID: a1b2c3d4e5f6
  Status: SUCCESS
  Duration: 00:01:45
  Input Size: 45.67 MB
  Output Directory: C:\LAZFiles\Output\a1b2c3d4e5f6
  Output Files: 156

Results saved to: C:\LAZFiles\Output\conversion_results_20250611_143022.json
```
