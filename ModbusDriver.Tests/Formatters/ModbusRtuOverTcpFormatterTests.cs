using ModbusDriver.Core;
using ModbusDriver.Formatters;
using System;
using Xunit;

namespace ModbusDriver.Tests.Formatters
{
    public class ModbusRtuOverTcpFormatterTests
    {
        [Fact]
        public void BuildRequest_ReadHoldingRegisters_ShouldReturnValidRtuFrameWithCrc()
        {
            var formatter = new ModbusRtuOverTcpFormatter();

            byte[] frame = formatter.BuildRequest(unitId: 1, functionCode: 3, startAddress: 0, quantity: 2);

            // ID + FC + StartAddr(2) + Quantity(2) + CRC(2) = 8 bytes, no MBAP header
            Assert.Equal(8, frame.Length);
            Assert.Equal(0x01, frame[0]);
            Assert.Equal(0x03, frame[1]);
            Assert.Equal(0x00, frame[2]);
            Assert.Equal(0x00, frame[3]);
            Assert.Equal(0x00, frame[4]);
            Assert.Equal(0x02, frame[5]);
            // CRC-16(Modbus) over bytes [01 03 00 00 00 02] = 0x0BC4 -> low=C4, high=0B
            Assert.Equal(0xC4, frame[6]);
            Assert.Equal(0x0B, frame[7]);
        }

        [Fact]
        public void BuildRequest_WriteMultipleRegisters_ShouldIncludeByteCountAndCorrectCrc()
        {
            var formatter = new ModbusRtuOverTcpFormatter();
            byte[] data = { 0x01, 0xF4, 0x03, 0xE8 }; // 500, 1000

            byte[] frame = formatter.BuildRequest(unitId: 1, functionCode: 16, startAddress: 0, quantity: 2, data: data);

            // ID + FC + StartAddr(2) + Quantity(2) + ByteCount(1) + Data(4) + CRC(2) = 13 bytes
            Assert.Equal(13, frame.Length);
            Assert.Equal(0x04, frame[6]);  // Byte count
            Assert.Equal(0x01, frame[7]);  // Data[0]
            Assert.Equal(0xF4, frame[8]);  // Data[1]
            Assert.Equal(0x03, frame[9]);  // Data[2]
            Assert.Equal(0xE8, frame[10]); // Data[3]
            // CRC-16 over [01 10 00 00 00 02 04 01 F4 03 E8] = low=B3, high=1F
            Assert.Equal(0xB3, frame[11]);
            Assert.Equal(0x1F, frame[12]);
        }

        [Fact]
        public void ParseResponse_WhenCrcIsValid_ShouldReturnDecodedData()
        {
            var formatter = new ModbusRtuOverTcpFormatter();

            byte[] response =
            {
                0x01,                   // Unit ID
                0x03,                   // Function Code
                0x04,                   // Byte Count
                0x01, 0xF4,             // Register 1: 500
                0x03, 0xE8,             // Register 2: 1000
                0xBA, 0x83              // Correct CRC-16 for the preceding 7 bytes
            };

            byte[] data = formatter.ParseResponse(response, expectedFunctionCode: 3);

            Assert.Equal(4, data.Length);
            Assert.Equal(500, (data[0] << 8) | data[1]);
            Assert.Equal(1000, (data[2] << 8) | data[3]);
        }

        [Fact]
        public void ParseResponse_WhenCrcIsInvalid_ShouldThrowModbusException()
        {
            var formatter = new ModbusRtuOverTcpFormatter();

            byte[] response =
            {
                0x01, 0x03, 0x04,
                0x01, 0xF4, 0x03, 0xE8,
                0xBB, 0x83              // Deliberately corrupted CRC (low byte off by one)
            };

            var ex = Assert.Throws<ModbusException>(() => formatter.ParseResponse(response, expectedFunctionCode: 3));
            Assert.Contains("CRC", ex.Message);
        }

        [Fact]
        public void ParseResponse_WhenExceptionBitSet_ShouldThrowWithExceptionCode()
        {
            var formatter = new ModbusRtuOverTcpFormatter();

            byte[] response =
            {
                0x01,                   // Unit ID
                0x83,                   // FC 0x03 | 0x80 (exception)
                0x02,                   // Exception code: Illegal Data Address
                0xC0, 0xF1              // Correct CRC over [01 83 02]
            };

            var ex = Assert.Throws<ModbusException>(() => formatter.ParseResponse(response, expectedFunctionCode: 3));
            Assert.Contains("Illegal Data Address", ex.Message);
        }

        [Fact]
        public void ParseResponse_WhenExceptionFrameCrcInvalid_ShouldThrowCrcErrorNotExceptionCode()
        {
            var formatter = new ModbusRtuOverTcpFormatter();

            byte[] response =
            {
                0x01, 0x83, 0x02,
                0xC1, 0xF1              // Corrupted CRC for the exception frame
            };

            var ex = Assert.Throws<ModbusException>(() => formatter.ParseResponse(response, expectedFunctionCode: 3));
            Assert.Contains("CRC", ex.Message);
        }

        [Fact]
        public void ParseResponse_WhenFrameTooShort_ShouldThrow()
        {
            var formatter = new ModbusRtuOverTcpFormatter();
            byte[] response = { 0x01, 0x03 }; // way too short

            Assert.Throws<ModbusException>(() => formatter.ParseResponse(response, expectedFunctionCode: 3));
        }

        // --- TryGetFrameLength: this is the piece that matters for TCP, since reads can arrive
        // split across multiple calls and the client needs to know when to keep reading. ---

        [Fact]
        public void TryGetFrameLength_WhenOnlyFirstByteReceived_ShouldReturnMinusOne()
        {
            var formatter = new ModbusRtuOverTcpFormatter();
            byte[] buffer = { 0x01, 0, 0, 0, 0, 0, 0, 0 };

            Assert.Equal(-1, formatter.TryGetFrameLength(buffer, bytesReceived: 1, expectedFunctionCode: 3));
        }

        [Fact]
        public void TryGetFrameLength_ForReadFunctionBeforeByteCountArrives_ShouldReturnMinusOne()
        {
            var formatter = new ModbusRtuOverTcpFormatter();
            byte[] buffer = { 0x01, 0x03, 0, 0, 0, 0, 0, 0 };

            // Only unitId + functionCode received (2 bytes) - byteCount (3rd byte) hasn't arrived yet
            Assert.Equal(-1, formatter.TryGetFrameLength(buffer, bytesReceived: 2, expectedFunctionCode: 3));
        }

        [Fact]
        public void TryGetFrameLength_ForReadFunctionOnceByteCountKnown_ShouldReturnFullFrameLength()
        {
            var formatter = new ModbusRtuOverTcpFormatter();
            byte[] buffer = { 0x01, 0x03, 0x04, 0, 0, 0, 0, 0 };

            // ID + FC + ByteCount(4) + 4 data bytes + 2 CRC bytes = 9
            Assert.Equal(9, formatter.TryGetFrameLength(buffer, bytesReceived: 3, expectedFunctionCode: 3));
        }

        [Theory]
        [InlineData(5)]  // WriteSingleCoil
        [InlineData(6)]  // WriteSingleRegister
        [InlineData(15)] // WriteMultipleCoils
        [InlineData(16)] // WriteMultipleRegisters
        public void TryGetFrameLength_ForWriteFunctionEcho_ShouldReturnFixedLengthOfEight(byte functionCode)
        {
            var formatter = new ModbusRtuOverTcpFormatter();
            byte[] buffer = new byte[8];
            buffer[1] = functionCode;

            Assert.Equal(8, formatter.TryGetFrameLength(buffer, bytesReceived: 2, expectedFunctionCode: functionCode));
        }

        [Fact]
        public void TryGetFrameLength_WhenExceptionBitSet_ShouldReturnFive()
        {
            var formatter = new ModbusRtuOverTcpFormatter();
            byte[] buffer = new byte[8];
            buffer[1] = 0x83; // FC 3 with error bit

            Assert.Equal(5, formatter.TryGetFrameLength(buffer, bytesReceived: 2, expectedFunctionCode: 3));
        }
    }
}