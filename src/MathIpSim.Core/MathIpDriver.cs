using System;
using System.IO;

namespace MathIpSim.Core
{
    public unsafe class MathIpDriver : IDisposable
    {
        private SharedMemoryWrapper _shm;
        private const int ShmSize = 0x40000; // 256 KB
        private const int RegBase = 0x39000;
        private const int DataBase = 0x20000;

        // Register Offsets
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
                _shm = new SharedMemoryWrapper(shmName, ShmSize);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool WriteData(uint offset, short[] data, uint len)
        {
            if (_shm == null || _shm.Pointer == null || data == null) return false;

            // Boundary check: ensure we don't write past Data Space (64 KB)
            // Data Space is 0x20000 to 0x30000. Maximum offset is 64KB (0x10000 bytes).
            long byteOffset = DataBase + offset;
            long bytesToWrite = len * sizeof(short);
            if (offset + bytesToWrite > 0x10000)
            {
                return false; // Out of bounds
            }

            unsafe
            {
                short* dest = (short*)(_shm.Pointer + byteOffset);
                for (int i = 0; i < len; i++)
                {
                    dest[i] = data[i];
                }
            }
            return true;
        }

        public bool ReadData(uint offset, short[] dest, uint len)
        {
            if (_shm == null || _shm.Pointer == null || dest == null) return false;

            long byteOffset = DataBase + offset;
            long bytesToRead = len * sizeof(short);
            if (offset + bytesToRead > 0x10000)
            {
                return false; // Out of bounds
            }

            unsafe
            {
                short* src = (short*)(_shm.Pointer + byteOffset);
                for (int i = 0; i < len; i++)
                {
                    dest[i] = src[i];
                }
            }
            return true;
        }

        public void Cleanup()
        {
            _shm?.Dispose();
            _shm = null;
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

        // --- Helpers to read/write registers ---
        private uint GetRegisterValue(int offset)
        {
            if (_shm == null || _shm.Pointer == null)
            {
                throw new InvalidOperationException("Driver not initialized. Call Init() first.");
            }
            unsafe
            {
                return *(uint*)(_shm.Pointer + offset);
            }
        }

        private void SetRegisterValue(int offset, uint value)
        {
            if (_shm == null || _shm.Pointer == null)
            {
                throw new InvalidOperationException("Driver not initialized. Call Init() first.");
            }
            unsafe
            {
                *(uint*)(_shm.Pointer + offset) = value;
            }
        }

        public void Dispose()
        {
            Cleanup();
        }
    }
}
