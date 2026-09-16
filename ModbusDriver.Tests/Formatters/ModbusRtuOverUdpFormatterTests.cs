using ModbusDriver.Core;
using ModbusDriver.Formatters;
using System;
using Xunit;

namespace ModbusDriver.Tests.Formatters
{
    public class ModbusRtuOverUdpFormatterTests
    {
        [Fact]
        public void BuildRequest_ReadHoldingRegisters_ShouldReturnValidRtuFrameWithCrc()
        {
            var formatter = new ModbusRtuOverUdpFormatter();

            byte[] frame = formatter.BuildRequest(unitId: 1, functionCode: 3, startAddress: 0, quantity: 2);

            Assert.Equal(8, frame.Length);
            Assert.Equal(0x01, frame[0]);
            Assert.Equal(0x03, frame[1]);
            // CRC-16(Modbus) over [01 03 00 00 00 02] = low=C4, high=0B
            Assert.Equal(0xC4, frame[6]);
            Assert.Equal(0x0B, frame[7]);
        }

        [Fact]
        public void BuildRequest_WriteMultipleRegisters_ShouldIncludeByteCountAndCorrectCrc()
        {
            var formatter = new ModbusRtuOverUdpFormatter();
            byte[] data = { 0x01, 0xF4, 0x03, 0xE8 };

            byte[] frame = formatter.BuildRequest(unitId: 1, functionCode: 16, startAddress: 0, quantity: 2, data: data);

            Assert.Equal(13, frame.Length);
            Assert.Equal(0x04, frame[6]);
            Assert.Equal(0x01, frame[7]);
            Assert.Equal(0xF4, frame[8]);
            Assert.Equal(0x03, frame[9]);
            Assert.Equal(0xE8, frame[10]);
            Assert.Equal(0xB3, frame[11]);
            Assert.Equal(0x1F, frame[12]);
        }

        [Fact]
        public void ParseResponse_WhenCrcIsValid_ShouldReturnDecodedData()
        {
            var formatter = new ModbusRtuOverUdpFormatter();

            byte[] response =
            {
                0x01, 0x03, 0x04,
                0x01, 0xF4, 0x03, 0xE8,
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
            var formatter = new ModbusRtuOverUdpFormatter();

            byte[] response =
            {
                0x01, 0x03, 0x04,
                0x01, 0xF4, 0x03, 0xE8,
                0xBB, 0x83              // Corrupted CRC
            };

            var ex = Assert.Throws<ModbusException>(() => formatter.ParseResponse(response, expectedFunctionCode: 3));
            Assert.Contains("CRC", ex.Message);
        }

        [Fact]
        public void ParseResponse_WhenExceptionBitSet_ShouldThrowWithExceptionCode()
        {
            var formatter = new ModbusRtuOverUdpFormatter();

            byte[] response =
            {
                0x01, 0x83, 0x02,
                0xC0, 0xF1              // Correct CRC over [01 83 02]
            };

            var ex = Assert.Throws<ModbusException>(() => formatter.ParseResponse(response, expectedFunctionCode: 3));
            Assert.Contains("Illegal Data Address", ex.Message);
        }

        [Fact]
        public void ParseResponse_WhenFrameTooShort_ShouldThrow()
        {
            var formatter = new ModbusRtuOverUdpFormatter();
            byte[] response = { 0x01, 0x03 };

            Assert.Throws<ModbusException>(() => formatter.ParseResponse(response, expectedFunctionCode: 3));
        }

        [Fact]
        public void TryGetFrameLength_ShouldAlwaysReturnBytesReceived()
        {
            // Unlike the over-TCP variant, a UDP datagram is never split across
            // reads, so this formatter should never ask the caller to keep reading.
            var formatter = new ModbusRtuOverUdpFormatter();
            byte[] buffer = new byte[512];

            Assert.Equal(0, formatter.TryGetFrameLength(buffer, 0, 3));
            Assert.Equal(1, formatter.TryGetFrameLength(buffer, 1, 3)); // even a single byte - no waiting for more
            Assert.Equal(9, formatter.TryGetFrameLength(buffer, 9, 3));
        }
    }
}