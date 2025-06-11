using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LAZConverter.Contracts
{
    public interface IDirectoryService
    {
        void EnsureDirectoryExists();
        List<string> GetLazFiles();
        void CleanupTempDirectory();
    }
}
