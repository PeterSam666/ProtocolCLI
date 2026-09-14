using ModbusDriver.Client;
using ModbusDriver.Core;
using ModbusDriver.Formatters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusDriver.Tests.Client
{
    public class ModbusClientTcpTests
    {
        // --- Mock Stream Implementation for testing Network I/O without real hardware ---
        private class FakeTcpStream : IModbusStream
        {
            public byte[]? BytesReceivedFromClient { get; private set; }
            public byte[]? BytesToMockResponse { get; set; }
            public bool IsConnected { get; private set; }

            public void Connect() => IsConnected = true;
            public void Disconnect() => IsConnected = false;

            public void Write(byte[] buffer, int offset, int count)
            {
                // Capture the raw bytes sent by ModbusClient for later validation
                BytesReceivedFromClient = new byte[count];
                Array.Copy(buffer, offset, BytesReceivedFromClient, 0, count);
            }

            public int Read(byte[] buffer, int offset, int count)
            {
                // Simulate data stream coming back from the real PLC device
                if (BytesToMockResponse == null) return 0;
                Array.Copy(BytesToMockResponse, 0, buffer, offset, BytesToMockResponse.Length);
                return BytesToMockResponse.Length;
            }

            public void Dispose() { }
        }

        // --- Test Case ---
        [Fact]
        public void ReadHoldingRegisters_WhenTcpResponseIsValid_ShouldReturnCorrectDecodedValues()
        {
            // Arrange
            var fakeStream = new FakeTcpStream();
            var formatter = new ModbusTcpFormatter();
            var client = new ModbusClient(formatter, fakeStream);

            // Mocking a successful Modbus TCP response payload from a PLC
            // Reading 2 registers, values are 500 (0x01F4) and 1000 (0x03E8)
            // Layout: [7 bytes MBAP] + [FC] + [Byte Count] + [Data Bytes...]
            fakeStream.BytesToMockResponse = new byte[]
            {
                0x00, 0x01,             // Transaction ID
                0x00, 0x00,             // Protocol ID
                0x00, 0x07,             // Length (7 bytes follow)
                0x01,                   // Unit ID
                0x03,                   // Function Code (Read Holding Registers)
                0x04,                   // Byte Count (2 registers * 2 bytes = 4 bytes)
                0x01, 0xF4,             // Register 1 value: 500
                0x03, 0xE8              // Register 2 value: 1000
            };

            client.Connect();

            // Act
            // Activating the pipeline: formats packet -> writes to stream -> reads response -> decodes
            ushort[] result = client.ReadHoldingRegisters(unitId: 1, startAddress: 0, quantity: 2);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Length);
            Assert.Equal(500, result[0]);  // Verified: 0x01F4 equals 500
            Assert.Equal(1000, result[1]); // Verified: 0x03E8 equals 1000
        }
    }
}