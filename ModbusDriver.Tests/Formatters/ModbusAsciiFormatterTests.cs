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
    public class ModbusAsciiFormatterTests
    {
        [Fact]
        public void BuildRequest_ShouldReturnValidModbusAsciiFrameWithLrcAndCrLf()
        {
            // Arrange
            var formatter = new ModbusAsciiFormatter();
            byte unitId = 1;
            byte functionCode = 3;       // Read Holding Registers
            ushort startAddress = 10;    // Address 0x000A
            ushort quantity = 2;         // Read 2 registers

            // Act
            byte[] actualFrameBytes = formatter.BuildRequest(unitId, functionCode, startAddress, quantity);

            // Convert to string to validate character array indices properly
            string actualFrame = Encoding.ASCII.GetString(actualFrameBytes);

            // Assert
            // Expected String frame: ":0103000A0002EE\r\n"
            // Length: 1 string colon + 14 hex chars + 2 chars CRLF = 17 bytes
            Assert.Equal(17, actualFrame.Length);

            // Verify ASCII Layout Content
            Assert.Equal(':', actualFrame[0]);   // Standard Starting character
            Assert.Equal("01", actualFrame.Substring(1, 2));   // Unit ID text
            Assert.Equal("03", actualFrame.Substring(3, 2));   // Function Code text
            Assert.Equal("000A", actualFrame.Substring(5, 4)); // Starting Address text
            Assert.Equal("0002", actualFrame.Substring(9, 4)); // Quantity text

            // Verify LRC Checksum text (Calculated via Two's Complement)
            Assert.Equal("F0", actualFrame.Substring(13, 2));  // LRC checksum text

            // Verify Frame Endings
            Assert.Equal('\r', actualFrame[15]); // Carriage Return
            Assert.Equal('\n', actualFrame[16]); // Line Feed
        }
    }
}
