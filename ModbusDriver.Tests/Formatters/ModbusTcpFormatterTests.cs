using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ModbusDriver.Core;
using ModbusDriver.Client;
using ModbusDriver.Formatters;

namespace ModbusDriver.Tests.Formatters
{
    public class ModbusTcpFormatterTests
    {
        [Fact]
        public void BuildReadRequest_ShouldReturnValidModbusTcpFrameWithMbapHeader()
        {
            // Arrange
            var formatter = new ModbusTcpFormatter();
            byte unitId = 1;
            byte functionCode = 3;       // Read Holding Registers
            ushort startAddress = 10;    // Address 0x000A
            ushort quantity = 2;         // Read 2 registers

            // Act
            byte[] actualFrame = formatter.BuildRequest(unitId, functionCode, startAddress, quantity);

            // Assert
            // Standard Modbus TCP Read Request frame must be exactly 12 bytes long 
            // (7 bytes MBAP Header + 5 bytes PDU)
            Assert.Equal(12, actualFrame.Length);

            // Verify MBAP Header
            // Transaction ID (Starts at 1 and increments, should be 0x0001)
            Assert.Equal(0x00, actualFrame[0]);
            Assert.Equal(0x01, actualFrame[1]);
            // Protocol ID (Always 0x0000 for Modbus TCP)
            Assert.Equal(0x00, actualFrame[2]);
            Assert.Equal(0x00, actualFrame[3]);
            // Length field (0x0006 bytes remaining from Unit ID to the end of the frame)
            Assert.Equal(0x00, actualFrame[4]);
            Assert.Equal(0x06, actualFrame[5]);
            // Unit ID
            Assert.Equal(unitId, actualFrame[6]);

            // Verify Modbus PDU
            Assert.Equal(functionCode, actualFrame[7]); // Function Code (0x03)
            Assert.Equal(0x00, actualFrame[8]);         // Starting Address High (0x00)
            Assert.Equal(0x0A, actualFrame[9]);         // Starting Address Low (0x0A)
            Assert.Equal(0x00, actualFrame[10]);        // Quantity High (0x00)
            Assert.Equal(0x02, actualFrame[11]);        // Quantity Low (0x02)
        }

    }
}
