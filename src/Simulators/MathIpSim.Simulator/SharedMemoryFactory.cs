using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace MathIpSim.Simulator
{
    public static class SharedMemoryFactory
    {
        public static MemoryMappedFile CreateOrOpen(string name, long capacity)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows uses Named Shared Memory (Local\shmName)
                return MemoryMappedFile.CreateOrOpen(name, capacity, MemoryMappedFileAccess.ReadWrite);
            }
            else
            {
                // macOS/Linux uses a file-backed mapping inside /tmp/ to match native C mmap expectations.
                string filePath = $"/tmp/{name}";
                using (var fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    fs.SetLength(capacity);
                }
                return MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, null, capacity, MemoryMappedFileAccess.ReadWrite);
            }
        }
    }
}
