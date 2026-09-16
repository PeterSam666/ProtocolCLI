using ModbusDriver.Core;
using ModbusDriver.Formatters;
using System;
using Xunit;

namespace ModbusDriver.Tests.Formatters
{
    public class ModbusUdpFormatterTests
    {
        [Fact]
        public void BuildRequest_ReadHoldingRegisters_ShouldReturnValidMbapFrame()
        {
            var formatter = new ModbusUdpFormatter();

            byte[] frame = formatter.BuildRequest(unitId: 1, functionCode: 3, startAddress: 0, quantity: 2);

            // MBAP header: TransactionId(2) + ProtocolId(2, always 0) + Length(2) + UnitId(1) + PDU
            Assert.Equal(12, frame.Length);
            Assert.Equal(0x00, frame[2]); // Protocol ID hi
            Assert.Equal(0x00, frame[3]); // Protocol ID lo
            Assert.Equal(0x00, frame[4]); // Length hi -> length = 6
            Assert.Equal(0x06, frame[5]); // Length lo
            Assert.Equal(0x01, frame[6]); // Unit ID
            Assert.Equal(0x03, frame[7]); // Function code
            Assert.Equal(0x00, frame[8]); // Start address hi
            Assert.Equal(0x00, frame[9]); // Start address lo
            Assert.Equal(0x00, frame[10]); // Quantity hi
            Assert.Equal(0x02, frame[11]); // Quantity lo
        }

        [Fact]
        public void BuildRequest_CalledTwice_ShouldIncrementTransactionId()
        {
            var formatter = new ModbusUdpFormatter();

            byte[] first = formatter.BuildRequest(unitId: 1, functionCode: 3, startAddress: 0, quantity: 2);
            byte[] second = formatter.BuildRequest(unitId: 1, functionCode: 3, startAddress: 0, quantity: 2);

            ushort firstTxId = (ushort)((first[0] << 8) | first[1]);
            ushort secondTxId = (ushort)((second[0] << 8) | second[1]);

            Assert.Equal(firstTxId + 1, secondTxId);
        }

        [Fact]
        public void BuildRequest_WriteMultipleRegisters_ShouldIncludeByteCountAndPayload()
        {
            var formatter = new ModbusUdpFormatter();
            byte[] data = { 0x01, 0xF4, 0x03, 0xE8 }; // 500, 1000

            byte[] frame = formatter.BuildRequest(unitId: 1, functionCode: 16, startAddress: 0, quantity: 2, data: data);

            // Header(6) + UnitId + FC + StartAddr(2) + Quantity(2) + ByteCount(1) + Data(4)
            Assert.Equal(6 + 6 + 1 + 4, frame.Length);
            Assert.Equal(0x00, frame[4]); // Length hi -> length = 6 + 1 + 4 = 11
            Assert.Equal(0x0B, frame[5]); // Length lo
            Assert.Equal(0x04, frame[12]); // ByteCount
            Assert.Equal(0x01, frame[13]);
            Assert.Equal(0xF4, frame[14]);
            Assert.Equal(0x03, frame[15]);
            Assert.Equal(0xE8, frame[16]);
        }

        [Fact]
        public void ParseResponse_WhenDatagramIsValid_ShouldReturnDecodedData()
        {
            var formatter = new ModbusUdpFormatter();

            byte[] response =
            {
                0x00, 0x01,             // Transaction ID
                0x00, 0x00,             // Protocol ID
                0x00, 0x05,             // Length (unitId + fc + byteCount + 2 data bytes... adjusted below)
                0x01,                   // Unit ID
                0x03,                   // Function Code
                0x04,                   // Byte Count
                0x01, 0xF4,             // Register 1: 500
                0x03, 0xE8              // Register 2: 1000
            };

            byte[] data = formatter.ParseResponse(response, expectedFunctionCode: 3);

            Assert.Equal(4, data.Length);
            Assert.Equal(0x01, data[0]);
            Assert.Equal(0xF4, data[1]);
            Assert.Equal(0x03, data[2]);
            Assert.Equal(0xE8, data[3]);
        }

        [Fact]
        public void ParseResponse_WhenExceptionBitSet_ShouldThrowModbusExceptionWithCode()
        {
            var formatter = new ModbusUdpFormatter();

            byte[] response =
            {
                0x00, 0x01,
                0x00, 0x00,
                0x00, 0x03,
                0x01,                   // Unit ID
                0x83,                   // Function code with error bit (0x03 | 0x80)
                0x02                    // Exception code: Illegal Data Address
            };

            var ex = Assert.Throws<ModbusException>(() => formatter.ParseResponse(response, expectedFunctionCode: 3));
            Assert.Contains("Illegal Data Address", ex.Message);
        }

        [Fact]
        public void ParseResponse_WhenDatagramShorterThanDeclaredByteCount_ShouldThrow()
        {
            var formatter = new ModbusUdpFormatter();

            // Declares byteCount = 4 but only 2 data bytes actually present
            byte[] response =
            {
                0x00, 0x01,
                0x00, 0x00,
                0x00, 0x05,
                0x01,
                0x03,
                0x04,                   // Byte count says 4
                0x01, 0xF4              // Only 2 bytes present
            };

            Assert.Throws<ModbusException>(() => formatter.ParseResponse(response, expectedFunctionCode: 3));
        }

        [Fact]
        public void ParseResponse_WhenFunctionCodeMismatch_ShouldThrow()
        {
            var formatter = new ModbusUdpFormatter();

            byte[] response =
            {
                0x00, 0x01,
                0x00, 0x00,
                0x00, 0x05,
                0x01,
                0x04,                   // Actual FC = 4 (Read Input Registers)
                0x04,
                0x01, 0xF4,
                0x03, 0xE8
            };

            // Expecting FC = 3
            Assert.Throws<ModbusException>(() => formatter.ParseResponse(response, expectedFunctionCode: 3));
        }

        [Fact]
        public void ParseResponse_WhenTooShort_ShouldThrow()
        {
            var formatter = new ModbusUdpFormatter();
            byte[] response = { 0x00, 0x01, 0x00, 0x00 }; // way too short

            Assert.Throws<ModbusException>(() => formatter.ParseResponse(response, expectedFunctionCode: 3));
        }

        [Fact]
        public void TryGetFrameLength_ShouldAlwaysReturnBytesReceived()
        {
            // UDP datagrams always arrive whole, so the formatter should never
            // ask the caller to keep reading - it trusts what Receive() returned.
            var formatter = new ModbusUdpFormatter();
            byte[] buffer = new byte[512];

            Assert.Equal(0, formatter.TryGetFrameLength(buffer, 0, 3));
            Assert.Equal(9, formatter.TryGetFrameLength(buffer, 9, 3));
            Assert.Equal(256, formatter.TryGetFrameLength(buffer, 256, 3));
        }
    }
}