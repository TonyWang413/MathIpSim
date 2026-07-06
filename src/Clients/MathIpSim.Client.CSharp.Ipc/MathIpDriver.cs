using System;
using System.IO.MemoryMappedFiles;
using MathIpSim.Simulator;

namespace MathIpSim.Client.CSharp.Ipc
{
    public class MathIpDriver : IDisposable
    {
        private MemoryMappedFile? _mmf;
        private MemoryMappedViewAccessor? _accessor;

        private const int ShmSize = 0x40000; // 256 KB
        private const int RegBase = 0x39000;
        private const int DataBase = 0x20000;

        // Register offsets
        private const int OffsetAAddress = RegBase + 0;
        private const int OffsetBAddress = RegBase + 4;
        private const int OffsetCAddress = RegBase + 8;
        private const int OffsetDataLen = RegBase + 12;
        private const int OffsetGo = RegBase + 16;
        private const int OffsetStatus = RegBase + 20;

        public bool Init(string shmName)
        {
            try
            {
                Cleanup();
                _mmf = SharedMemoryFactory.CreateOrOpen(shmName, ShmSize);
                _accessor = _mmf.CreateViewAccessor(0, ShmSize, MemoryMappedFileAccess.ReadWrite);
                return true;
            }
            catch
            {
                Cleanup();
                return false;
            }
        }

        public bool WriteData(uint offset, short[] data, uint len)
        {
            if (_accessor == null || data == null) return false;

            // Boundary check: Data space is 64 KB (0x10000 bytes).
            long bytesToWrite = len * sizeof(short);
            if (offset + bytesToWrite > 0x10000)
            {
                return false; // Out of bounds
            }

            long byteOffset = DataBase + offset;
            _accessor.WriteArray(byteOffset, data, 0, (int)len);
            return true;
        }

        public bool ReadData(uint offset, short[] dest, uint len)
        {
            if (_accessor == null || dest == null) return false;

            long bytesToRead = len * sizeof(short);
            if (offset + bytesToRead > 0x10000)
            {
                return false; // Out of bounds
            }

            long byteOffset = DataBase + offset;
            _accessor.ReadArray(byteOffset, dest, 0, (int)len);
            return true;
        }

        public void Cleanup()
        {
            _accessor?.Dispose();
            _accessor = null;
            _mmf?.Dispose();
            _mmf = null;
        }

        public void WriteInputs(short[] a, short[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
            {
                throw new ArgumentException("Inputs must be non-null and have identical lengths.");
            }

            uint len = (uint)a.Length;
            A_ADDRESS = 0x1000;
            B_ADDRESS = 0x2000;
            C_ADDRESS = 0x3000;
            DATA_LEN = len;

            WriteData(A_ADDRESS, a, len);
            WriteData(B_ADDRESS, b, len);
        }

        public void Execute()
        {
            GO = 1;

            // Poll GO until it becomes 0 (cleared by hardware daemon)
            int retries = 0;
            const int maxRetries = 2000; // 2 seconds safety timeout
            while (GO == 1 && retries < maxRetries)
            {
                System.Threading.Thread.Sleep(1);
                retries++;
            }

            if (GO == 1)
            {
                throw new TimeoutException("Timeout waiting for Simulator Daemon to complete calculation (GO register not cleared).");
            }
        }

        public short[] ReadOutputs()
        {
            uint len = DATA_LEN;
            short[] dest = new short[len * 4];
            ReadData(C_ADDRESS, dest, len * 4);
            return dest;
        }

        // --- Safe Register Access Properties ---

        public uint A_ADDRESS
        {
            get => GetRegisterValue(OffsetAAddress);
            set => SetRegisterValue(OffsetAAddress, value);
        }

        public uint B_ADDRESS
        {
            get => GetRegisterValue(OffsetBAddress);
            set => SetRegisterValue(OffsetBAddress, value);
        }

        public uint C_ADDRESS
        {
            get => GetRegisterValue(OffsetCAddress);
            set => SetRegisterValue(OffsetCAddress, value);
        }

        public uint DATA_LEN
        {
            get => GetRegisterValue(OffsetDataLen);
            set => SetRegisterValue(OffsetDataLen, value);
        }

        public uint GO
        {
            get => GetRegisterValue(OffsetGo);
            set => SetRegisterValue(OffsetGo, value);
        }

        public uint STATUS
        {
            get => GetRegisterValue(OffsetStatus);
        }

        private uint GetRegisterValue(int offset)
        {
            if (_accessor == null)
            {
                throw new InvalidOperationException("Driver not initialized. Call Init() first.");
            }
            return _accessor.ReadUInt32(offset);
        }

        private void SetRegisterValue(int offset, uint value)
        {
            if (_accessor == null)
            {
                throw new InvalidOperationException("Driver not initialized. Call Init() first.");
            }
            _accessor.Write(offset, value);
        }

        public void Dispose()
        {
            Cleanup();
        }
    }
}
