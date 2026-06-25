using Xunit;
using System.Runtime.InteropServices;
using MathIpSim.Core;

namespace MathIpSim.Tests;

public class EngineTests
{
    private const int ShmSize = 0x40000; // 256 KB
    private const int RegBase = 0x39000;
    private const int DataBase = 0x20000;

    // Registers offset constants
    private const int RegAAddress = RegBase + 0;
    private const int RegBAddress = RegBase + 4;
    private const int RegCAddress = RegBase + 8;
    private const int RegDataLen = RegBase + 12;
    private const int RegGo = RegBase + 16;
    private const int RegStatus = RegBase + 20;

    private unsafe void WriteUint32(byte* basePtr, int offset, uint value)
    {
        *(uint*)(basePtr + offset) = value;
    }

    private unsafe uint ReadUint32(byte* basePtr, int offset)
    {
        return *(uint*)(basePtr + offset);
    }

    private unsafe void WriteInt16Array(byte* basePtr, int offset, short[] data)
    {
        short* dest = (short*)(basePtr + offset);
        for (int i = 0; i < data.Length; i++)
        {
            dest[i] = data[i];
        }
    }

    private unsafe short[] ReadInt16Array(byte* basePtr, int offset, int count)
    {
        short* src = (short*)(basePtr + offset);
        short[] result = new short[count];
        for (int i = 0; i < count; i++)
        {
            result[i] = src[i];
        }
        return result;
    }

    [Fact]
    public unsafe void Test_NormalCalculation_Success()
    {
        // Arrange
        byte[] shm = new byte[ShmSize];
        fixed (byte* ptr = shm)
        {
            // Set registers
            WriteUint32(ptr, RegAAddress, 0x1000); // 0x20000 + 0x1000 = 0x21000
            WriteUint32(ptr, RegBAddress, 0x2000); // 0x20000 + 0x2000 = 0x22000
            WriteUint32(ptr, RegCAddress, 0x3000); // 0x20000 + 0x3000 = 0x23000
            WriteUint32(ptr, RegDataLen, 3);
            WriteUint32(ptr, RegGo, 1);
            WriteUint32(ptr, RegStatus, 0xDEADBEEF); // Initial garbage to check reset

            // Set inputs
            short[] aData = { 1, 2, 3 };
            short[] bData = { 4, 5, 6 };
            WriteInt16Array(ptr, DataBase + 0x1000, aData);
            WriteInt16Array(ptr, DataBase + 0x2000, bData);

            // Act
            MathIpEngine.Execute(ptr);

            // Assert
            uint goValue = ReadUint32(ptr, RegGo);
            uint statusValue = ReadUint32(ptr, RegStatus);
            short[] cResults = ReadInt16Array(ptr, DataBase + 0x3000, 12);

            Assert.Equal(0u, goValue);
            Assert.Equal(0u, statusValue);

            short[] expected = {
                5, -3, 4, 0,     // 1, 4 calculations
                7, -3, 10, 0,    // 2, 5 calculations
                9, -3, 18, 0     // 3, 6 calculations
            };
            Assert.Equal(expected, cResults);
        }
    }

    [Fact]
    public unsafe void Test_DivisionByZero_SetsErrorFlag()
    {
        // Arrange
        byte[] shm = new byte[ShmSize];
        fixed (byte* ptr = shm)
        {
            WriteUint32(ptr, RegAAddress, 0x1000);
            WriteUint32(ptr, RegBAddress, 0x2000);
            WriteUint32(ptr, RegCAddress, 0x3000);
            WriteUint32(ptr, RegDataLen, 1);
            WriteUint32(ptr, RegGo, 1);

            WriteInt16Array(ptr, DataBase + 0x1000, new short[] { 5 });
            WriteInt16Array(ptr, DataBase + 0x2000, new short[] { 0 }); // Divisor is 0

            // Act
            MathIpEngine.Execute(ptr);

            // Assert
            uint goValue = ReadUint32(ptr, RegGo);
            uint statusValue = ReadUint32(ptr, RegStatus);
            short[] cResults = ReadInt16Array(ptr, DataBase + 0x3000, 4);

            Assert.Equal(0u, goValue);
            Assert.Equal(1u, statusValue & 0x01); // Bit 0 (DIV_BY_ZERO) must be 1

            // Results: 5+0=5, 5-0=5, 5*0=0, 5/0=0
            short[] expected = { 5, 5, 0, 0 };
            Assert.Equal(expected, cResults);
        }
    }

    [Fact]
    public unsafe void Test_OverflowAndUnderflow_SaturatesAndSetsFlag()
    {
        // Arrange
        byte[] shm = new byte[ShmSize];
        fixed (byte* ptr = shm)
        {
            WriteUint32(ptr, RegAAddress, 0x1000);
            WriteUint32(ptr, RegBAddress, 0x2000);
            WriteUint32(ptr, RegCAddress, 0x3000);
            WriteUint32(ptr, RegDataLen, 3);
            WriteUint32(ptr, RegGo, 1);

            // Add overflow, sub underflow, mul overflow
            short[] aData = { 32000, -32000, 1000 };
            short[] bData = { 1000, 1000, 100 };
            WriteInt16Array(ptr, DataBase + 0x1000, aData);
            WriteInt16Array(ptr, DataBase + 0x2000, bData);

            // Act
            MathIpEngine.Execute(ptr);

            // Assert
            uint goValue = ReadUint32(ptr, RegGo);
            uint statusValue = ReadUint32(ptr, RegStatus);
            short[] cResults = ReadInt16Array(ptr, DataBase + 0x3000, 12);

            Assert.Equal(0u, goValue);
            Assert.Equal(2u, statusValue & 0x02); // Bit 1 (OVERFLOW) must be 1

            // 1st pair: 32000 + 1000 = 33000 -> 32767 (Sat Max)
            Assert.Equal(32767, cResults[0]); 
            
            // 2nd pair: -32000 - 1000 = -33000 -> -32768 (Sat Min)
            Assert.Equal(-32768, cResults[5]); 

            // 3rd pair: 1000 * 100 = 100000 -> 32767 (Sat Max)
            Assert.Equal(32767, cResults[10]); 
        }
    }

    [Fact]
    public unsafe void Test_GoNotSet_DoesNotExecute()
    {
        // Arrange
        byte[] shm = new byte[ShmSize];
        fixed (byte* ptr = shm)
        {
            WriteUint32(ptr, RegGo, 0); // GO is 0

            // Act
            MathIpEngine.Execute(ptr);

            // Assert
            uint goValue = ReadUint32(ptr, RegGo);
            Assert.Equal(0u, goValue);
            // No changes, no execution
        }
    }
}
