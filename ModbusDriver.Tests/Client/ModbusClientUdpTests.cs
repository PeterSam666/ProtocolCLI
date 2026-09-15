using ModbusDriver.Client;
using ModbusDriver.Core;
using ModbusDriver.Formatters;
using System;
using Xunit;

namespace ModbusDriver.Tests.Client
{
    public class ModbusClientUdpTests
    {
        // --- Mock Stream Implementation for testing UDP I/O without real hardware ---
        private class FakeUdpStream : IModbusStream
        {
            public byte[]? BytesReceivedFromClient { get; private set; }
            public byte[]? BytesToMockResponse { get; set; }
            public bool IsConnected { get; private set; }

            public void Connect() => IsConnected = true;
            public void Disconnect() => IsConnected = false;

            public int ReadTimeout { get; set; } = 1000;
            public int WriteTimeout { get; set; } = 1000;

            public void Write(byte[] buffer, int offset, int count)
            {
                // Capture the raw datagram sent by ModbusClient for later validation
                BytesReceivedFromClient = new byte[count];
                Array.Copy(buffer, offset, BytesReceivedFromClient, 0, count);
            }

            public int Read(byte[] buffer, int offset, int count)
            {
                // Simulate a UDP datagram coming back whole from the device (unlike TCP,
                // a single Receive() call always returns one complete datagram)
                if (BytesToMockResponse == null) return 0;
                Array.Copy(BytesToMockResponse, 0, buffer, offset, BytesToMockResponse.Length);
                return BytesToMockResponse.Length;
            }

            public void Dispose() { }
        }

        [Fact]
        public void ReadHoldingRegisters_WhenUdpResponseIsValid_ShouldReturnCorrectDecodedValues()
        {
            // Arrange
            var fakeStream = new FakeUdpStream();
            var formatter = new ModbusUdpFormatter();
            var client = new ModbusClient(formatter, fakeStream);

            // Mocking a successful Modbus UDP response payload from a PLC
            // Same MBAP framing as Modbus TCP, just delivered over a UDP datagram
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
            ushort[] result = client.ReadHoldingRegisters(unitId: 1, startAddress: 0, quantity: 2);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Length);
            Assert.Equal(500, result[0]);
            Assert.Equal(1000, result[1]);

            // Bonus: confirm the datagram ModbusClient actually sent has the right MBAP header
            Assert.NotNull(fakeStream.BytesReceivedFromClient);
            Assert.Equal(12, fakeStream.BytesReceivedFromClient!.Length);
            Assert.Equal(0x03, fakeStream.BytesReceivedFromClient[7]); // Function code in the request
        }

        [Fact]
        public void ReadHoldingRegisters_WhenDeviceReturnsException_ShouldThrowModbusException()
        {
            // Arrange
            var fakeStream = new FakeUdpStream();
            var formatter = new ModbusUdpFormatter();
            var client = new ModbusClient(formatter, fakeStream);

            fakeStream.BytesToMockResponse = new byte[]
            {
                0x00, 0x01,
                0x00, 0x00,
                0x00, 0x03,
                0x01,                   // Unit ID
                0x83,                   // Function code with error bit (0x03 | 0x80)
                0x02                    // Exception code: Illegal Data Address
            };

            client.Connect();

            // Act & Assert
            Assert.Throws<ModbusException>(() => client.ReadHoldingRegisters(unitId: 1, startAddress: 0, quantity: 2));
        }

        [Fact]
        public void ReadHoldingRegisters_WhenRtuOverUdpResponseIsValid_ShouldReturnCorrectDecodedValues()
        {
            // Arrange
            var fakeStream = new FakeUdpStream();
            var formatter = new ModbusRtuOverUdpFormatter();
            var client = new ModbusClient(formatter, fakeStream);

            // Mocking a successful RTU-framed (CRC-16) response, delivered over a UDP datagram
            fakeStream.BytesToMockResponse = new byte[]
            {
                0x01,                   // Unit ID
                0x03,                   // Function Code (Read Holding Registers)
                0x04,                   // Byte Count (2 registers * 2 bytes = 4 bytes)
                0x01, 0xF4,             // Register 1 value: 500
                0x03, 0xE8,             // Register 2 value: 1000
                0xBA, 0x83              // Correct CRC-16 for the preceding 7 bytes
            };

            client.Connect();

            // Act
            ushort[] result = client.ReadHoldingRegisters(unitId: 1, startAddress: 0, quantity: 2);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Length);
            Assert.Equal(500, result[0]);
            Assert.Equal(1000, result[1]);

            Assert.NotNull(fakeStream.BytesReceivedFromClient);
            Assert.Equal(8, fakeStream.BytesReceivedFromClient!.Length);
        }

        [Fact]
        public void ReadHoldingRegisters_WhenRtuOverUdpDeviceReturnsException_ShouldThrowModbusException()
        {
            // Arrange
            var fakeStream = new FakeUdpStream();
            var formatter = new ModbusRtuOverUdpFormatter();
            var client = new ModbusClient(formatter, fakeStream);

            fakeStream.BytesToMockResponse = new byte[]
            {
                0x01, 0x83, 0x02,       // Unit ID, FC with error bit, exception code
                0xC0, 0xF1              // Correct CRC for the exception frame
            };

            client.Connect();

            // Act & Assert
            Assert.Throws<ModbusException>(() => client.ReadHoldingRegisters(unitId: 1, startAddress: 0, quantity: 2));
        }
    }
}