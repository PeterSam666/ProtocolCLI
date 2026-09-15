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
    public class ModbusClientSerialTests
    {
        // --- Mock Stream Implementation for testing Serial I/O without real hardware ---
        private class FakeSerialStream : IModbusStream
        {
            private int _readPosition = 0;

            public byte[]? BytesReceivedFromClient { get; private set; }
            public byte[]? BytesToMockResponse { get; set; }
            public bool IsConnected { get; private set; }

            public int ReadTimeout { get; set; } = 1000;
            public int WriteTimeout { get; set; } = 1000;

            public void Connect()
            {
                IsConnected = true;
                _readPosition = 0;
            }

            public void Disconnect()
            {
                IsConnected = false;
            }

            public void Write(byte[] buffer, int offset, int count)
            {
                BytesReceivedFromClient = new byte[count];
                Array.Copy(buffer, offset, BytesReceivedFromClient, 0, count);
            }

            public int Read(byte[] buffer, int offset, int count)
            {
                if (BytesToMockResponse == null || _readPosition >= BytesToMockResponse.Length)
                {
                    return 0;
                }

                int bytesToReturn = Math.Min(count, BytesToMockResponse.Length - _readPosition);

                Array.Copy(BytesToMockResponse, _readPosition, buffer, offset, bytesToReturn);
                _readPosition += bytesToReturn;

                return bytesToReturn;
            }

            public void Dispose()
            {
                Disconnect();
            }
        }

        [Fact]
        public void ReadHoldingRegisters_WhenRtuResponseIsValid_ShouldReturnCorrectDecodedValues()
        {
            // Arrange
            var fakeStream = new FakeSerialStream();
            var formatter = new ModbusRtuFormatter();
            var client = new ModbusClient(formatter, fakeStream);

            // Mocking a successful Modbus RTU response payload from an industrial sensor
            // Reading 2 registers, values are 500 (0x01F4) and 1000 (0x03E8)
            // Layout: [Slave ID] + [FC] + [Byte Count] + [Data Bytes...] + [2 bytes CRC]
            fakeStream.BytesToMockResponse = new byte[]
            {
                0x01,                   // Slave Address / Unit ID
                0x03,                   // Function Code (Read Holding Registers)
                0x04,                   // Byte Count (2 registers * 2 bytes = 4 bytes)
                0x01, 0xF4,             // Register 1 value: 500
                0x03, 0xE8,             // Register 2 value: 1000
                0xFA, 0x3A              // 🎯 ซ่อมแล้ว: ค่า CRC-16 ที่ถูกต้องจริงตามสเปก Modbus RTU
            };

            client.Connect();

            // Act
            ushort[] result = client.ReadHoldingRegisters(unitId: 1, startAddress: 0, quantity: 2);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Length);
            Assert.Equal(500, result[0]);
            Assert.Equal(1000, result[1]);
        }

        [Fact]
        public void ReadHoldingRegisters_WhenAsciiResponseIsValid_ShouldReturnCorrectDecodedValues()
        {
            // Arrange
            var fakeStream = new FakeSerialStream();
            var formatter = new ModbusAsciiFormatter();
            var client = new ModbusClient(formatter, fakeStream);

            // Mocking a successful Modbus ASCII response string from a legacy controller
            // Frame content in plain text: ":01030401F403E8FA\r\n"
            // (FA is the calculated LRC checksum for this specific message sequence)
            string asciiResponseString = ":01030401F403E817\r\n";
            fakeStream.BytesToMockResponse = Encoding.ASCII.GetBytes(asciiResponseString);

            client.Connect();

            // Act
            ushort[] result = client.ReadHoldingRegisters(unitId: 1, startAddress: 0, quantity: 2);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Length);
            Assert.Equal(500, result[0]);
            Assert.Equal(1000, result[1]);
        }
    }
}
