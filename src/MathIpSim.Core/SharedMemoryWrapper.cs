using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace MathIpSim.Core
{
    public unsafe class SharedMemoryWrapper : IDisposable
    {
        private MemoryMappedFile _mmf;
        private MemoryMappedViewAccessor _accessor;
        private byte* _ptr;

        public byte* Pointer => _ptr;
        public long Capacity { get; }

        public SharedMemoryWrapper(string name, long capacity)
        {
            Capacity = capacity;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // On Windows, use system-wide Named Shared Memory
                _mmf = MemoryMappedFile.CreateOrOpen(name, capacity, MemoryMappedFileAccess.ReadWrite);
            }
            else
            {
                // On macOS/Unix, use a file-backed mapping in /tmp/ for clean C interoperability.
                // This ensures macOS C clients can map it via standard open() and mmap() on "/tmp/MathIpSharedMemory".
                string filePath = $"/tmp/{name}";
                
                // Create or open the file and set its size
                var fileInfo = new FileInfo(filePath);
                using (var fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite))
                {
                    fs.SetLength(capacity);
                }
                
                _mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, null, capacity, MemoryMappedFileAccess.ReadWrite);
            }

            _accessor = _mmf.CreateViewAccessor(0, capacity, MemoryMappedFileAccess.ReadWrite);
            
            byte* tempPtr = null;
            _accessor.SafeMemoryMappedViewHandle.AcquirePointer(ref tempPtr);
            _ptr = tempPtr;
        }

        public void Dispose()
        {
            if (_ptr != null)
            {
                _accessor?.SafeMemoryMappedViewHandle.ReleasePointer();
                _ptr = null;
            }
            _accessor?.Dispose();
            _mmf?.Dispose();
        }
    }
}
