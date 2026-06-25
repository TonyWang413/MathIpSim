using System;

namespace MathIpSim.Core
{
    public static class MathIpEngine
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

        public static unsafe void Execute(byte* shmBase)
        {
            if (shmBase == null) return;

            // 1. Read GO register
            uint goValue = *(uint*)(shmBase + OffsetGo);
            if ((goValue & 1) == 0)
            {
                return; // Not triggered
            }

            // 2. Reset STATUS register at start of calculation
            uint status = 0;
            *(uint*)(shmBase + OffsetStatus) = status;

            // 3. Read remaining registers
            uint offsetA = *(uint*)(shmBase + OffsetAAddress);
            uint offsetB = *(uint*)(shmBase + OffsetBAddress);
            uint offsetC = *(uint*)(shmBase + OffsetCAddress);
            uint dataLen = *(uint*)(shmBase + OffsetDataLen);

            // Pointers to data arrays
            short* aPtr = (short*)(shmBase + DataBase + offsetA);
            short* bPtr = (short*)(shmBase + DataBase + offsetB);
            short* cPtr = (short*)(shmBase + DataBase + offsetC);

            // 4. Perform calculations
            for (uint i = 0; i < dataLen; i++)
            {
                short a = aPtr[i];
                short b = bPtr[i];

                // Operations
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

                // Write results (each index takes 8 bytes)
                uint cOffset = i * 4;
                cPtr[cOffset + 0] = addRes;
                cPtr[cOffset + 1] = subRes;
                cPtr[cOffset + 2] = mulRes;
                cPtr[cOffset + 3] = divRes;
            }

            // 5. Update STATUS and clear GO
            *(uint*)(shmBase + OffsetStatus) = status;
            *(uint*)(shmBase + OffsetGo) = 0; // Clear GO flag
        }

        private static short ClampToShort(long value, ref uint status)
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
    }
}
