using LAZConverter.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LAZConverter.Contracts
{
    public interface IFileProcessorService
    {
        Task<ConversionResult> ConvertLazFileAsync(string inputFilePath);
        Task<List<ConversionResult>> ConvertMultipleLazFileAsync(IEnumerable<string> inputFilePaths);
    }
}
