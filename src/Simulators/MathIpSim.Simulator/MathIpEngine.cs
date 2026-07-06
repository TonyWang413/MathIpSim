using System;
using System.IO.MemoryMappedFiles;

namespace MathIpSim.Simulator
{
    public class MathIpEngine : IDisposable
    {
        private const int RegBase = 0x39000;
        private const int DataBase = 0x20000;

        // Register offsets
        private const int OffsetAAddress = RegBase + 0;
        private const int OffsetBAddress = RegBase + 4;
        private const int OffsetCAddress = RegBase + 8;
        private const int OffsetDataLen = RegBase + 12;
        private const int OffsetGo = RegBase + 16;
        private const int OffsetStatus = RegBase + 20;

        private readonly MemoryMappedFile _mmf;
        private readonly MemoryMappedViewAccessor _accessor;

        public string SharedMemoryName { get; }
        public long Capacity { get; }

        public MathIpEngine() : this("MathIpSharedMemory", 0x40000)
        {
        }

        public MathIpEngine(string shmName, long capacity)
        {
            SharedMemoryName = shmName;
            Capacity = capacity;

            _mmf = SharedMemoryFactory.CreateOrOpen(shmName, capacity);
            _accessor = _mmf.CreateViewAccessor(0, capacity, MemoryMappedFileAccess.ReadWrite);

            // Initialize registers to 0
            ResetRegisters();
        }

        private void ResetRegisters()
        {
            _accessor.Write(OffsetAAddress, 0u);
            _accessor.Write(OffsetBAddress, 0u);
            _accessor.Write(OffsetCAddress, 0u);
            _accessor.Write(OffsetDataLen, 0u);
            _accessor.Write(OffsetGo, 0u);
            _accessor.Write(OffsetStatus, 0u);
        }

        public bool IsGoTriggered()
        {
            uint goValue = _accessor.ReadUInt32(OffsetGo);
            return (goValue & 1) != 0;
        }

        public void Execute()
        {
            // 1. Reset STATUS register at start of calculation
            uint status = 0;
            _accessor.Write(OffsetStatus, status);

            // 2. Read register parameters
            uint offsetA = _accessor.ReadUInt32(OffsetAAddress);
            uint offsetB = _accessor.ReadUInt32(OffsetBAddress);
            uint offsetC = _accessor.ReadUInt32(OffsetCAddress);
            uint dataLen = _accessor.ReadUInt32(OffsetDataLen);

            // 3. Perform calculations
            for (uint i = 0; i < dataLen; i++)
            {
                // Read input A element (16-bit signed, 2 bytes per element)
                long aByteOffset = DataBase + offsetA + (i * 2);
                short a = _accessor.ReadInt16(aByteOffset);

                // Read input B element
                long bByteOffset = DataBase + offsetB + (i * 2);
                short b = _accessor.ReadInt16(bByteOffset);

                // Compute operations using wider integer type (long) to detect overflow
                long addVal = (long)a + b;
                long subVal = (long)a - b;
                long mulVal = (long)a * b;

                short addRes = ClampToShort(addVal, ref status);
                short subRes = ClampToShort(subVal, ref status);
                short mulRes = ClampToShort(mulVal, ref status);

                short divRes;
                if (b == 0)
                {
                    status |= 0x01; // Bit 0: DIV_BY_ZERO
                    divRes = 0;
                }
                else
                {
                    long divVal = (long)a / b;
                    divRes = ClampToShort(divVal, ref status);
                }

                // Write results to offset C. Each dataset produces 4 results (8 bytes total)
                long cByteOffset = DataBase + offsetC + (i * 8);
                _accessor.Write(cByteOffset + 0, addRes);
                _accessor.Write(cByteOffset + 2, subRes);
                _accessor.Write(cByteOffset + 4, mulRes);
                _accessor.Write(cByteOffset + 6, divRes);
            }

            // 4. Update STATUS and clear GO
            _accessor.Write(OffsetStatus, status);
            _accessor.Write(OffsetGo, 0u); // Clear GO flag
        }

        public void WriteInputs(short[] a, short[] b)
        {
            if (a == null || b == null || a.Length != b.Length)
            {
                throw new ArgumentException("Inputs must be non-null and have identical lengths.");
            }

            uint len = (uint)a.Length;
            
            // Configure default address offsets: Input A (0x1000), B (0x2000), Output C (0x3000)
            _accessor.Write(OffsetAAddress, 0x1000u);
            _accessor.Write(OffsetBAddress, 0x2000u);
            _accessor.Write(OffsetCAddress, 0x3000u);
            _accessor.Write(OffsetDataLen, len);

            _accessor.WriteArray(DataBase + 0x1000, a, 0, a.Length);
            _accessor.WriteArray(DataBase + 0x2000, b, 0, b.Length);
        }

        public short[] ReadOutputs()
        {
            uint len = _accessor.ReadUInt32(OffsetDataLen);
            short[] dest = new short[len * 4];
            _accessor.ReadArray(DataBase + 0x3000, dest, 0, dest.Length);
            return dest;
        }

        public uint GetStatus()
        {
            return _accessor.ReadUInt32(OffsetStatus);
        }

        public bool WriteData(uint offset, short[] data, uint len)
        {
            if (data == null) return false;
            long bytesToWrite = len * sizeof(short);
            if (offset + bytesToWrite > 0x10000)
            {
                return false;
            }
            _accessor.WriteArray(DataBase + offset, data, 0, (int)len);
            return true;
        }

        public bool ReadData(uint offset, short[] dest, uint len)
        {
            if (dest == null) return false;
            long bytesToRead = len * sizeof(short);
            if (offset + bytesToRead > 0x10000)
            {
                return false;
            }
            _accessor.ReadArray(DataBase + offset, dest, 0, (int)len);
            return true;
        }

        // --- Safe Register Access Properties ---

        public uint A_ADDRESS
        {
            get => _accessor.ReadUInt32(OffsetAAddress);
            set => _accessor.Write(OffsetAAddress, value);
        }

        public uint B_ADDRESS
        {
            get => _accessor.ReadUInt32(OffsetBAddress);
            set => _accessor.Write(OffsetBAddress, value);
        }

        public uint C_ADDRESS
        {
            get => _accessor.ReadUInt32(OffsetCAddress);
            set => _accessor.Write(OffsetCAddress, value);
        }

        public uint DATA_LEN
        {
            get => _accessor.ReadUInt32(OffsetDataLen);
            set => _accessor.Write(OffsetDataLen, value);
        }

        public uint GO
        {
            get => _accessor.ReadUInt32(OffsetGo);
            set => _accessor.Write(OffsetGo, value);
        }

        public uint STATUS
        {
            get => _accessor.ReadUInt32(OffsetStatus);
        }

        private short ClampToShort(long value, ref uint status)
        {
            if (value > 32767)
            {
                status |= 0x02; // Bit 1: OVERFLOW
                return 32767;
            }
            if (value < -32768)
            {
                status |= 0x02; // Bit 1: OVERFLOW
                return -32768;
            }
            return (short)value;
        }

        public void Dispose()
        {
            _accessor.Dispose();
            _mmf.Dispose();
        }
    }
}
