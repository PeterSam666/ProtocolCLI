using ModbusDriver.Core;
using ModbusDriver.Client;
using ModbusDriver.Formatters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ModbusDriver.Tests.Formatters
{
    public class ModbusRtuFormatterTests
    {
        [Fact]
        public void BuildRequest_ShouldReturnValidModbusRtuFrameWithCrc()
        {
            // Arrange
            var formatter = new ModbusRtuFormatter();
            byte unitId = 1;
            byte functionCode = 3;       // Read Holding Registers
            ushort startAddress = 10;    // Address 0x000A
            ushort quantity = 2;         // Read 2 registers

            // Act
            byte[] actualFrame = formatter.BuildRequest(unitId, functionCode, startAddress, quantity);

            // Assert
            // Standard Modbus RTU Read Request frame must be exactly 8 bytes long
            // Layout: [1 byte ID] + [1 byte FC] + [2 bytes Address] + [2 bytes Qty] + [2 bytes CRC]
            Assert.Equal(8, actualFrame.Length);

            // Verify Modbus PDU Data
            Assert.Equal(unitId, actualFrame[0]);       // Slave Address / Unit ID
            Assert.Equal(functionCode, actualFrame[1]); // Function Code (0x03)
            Assert.Equal(0x00, actualFrame[2]);         // Starting Address High (0x00)
            Assert.Equal(0x0A, actualFrame[3]);         // Starting Address Low (0x0A)
            Assert.Equal(0x00, actualFrame[4]);         // Quantity High (0x00)
            Assert.Equal(0x02, actualFrame[5]);         // Quantity Low (0x02)

            // Verify CRC-16 Checksum (Calculated mathematically for this specific frame)
            Assert.Equal(0xE4, actualFrame[6]);         // CRC Low byte
            Assert.Equal(0x09, actualFrame[7]);         // CRC High byte
        }
    }
}
