using ModbusDriver.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModbusDriver.Formatters
{
    public class ModbusTcpFormatter : IModbusFormatter
    {
        private ushort _transactionId = 0;

        public byte[] BuildRequest(byte unitId, byte functionCode, ushort startAddress, ushort quantity)
        {
            _transactionId++; 
            byte[] frame = new byte[12]; 

            frame[0] = (byte)(_transactionId >> 8);
            frame[1] = (byte)(_transactionId & 0xFF);
            frame[2] = 0; 
            frame[3] = 0;
            frame[4] = 0; 
            frame[5] = 6; 
            frame[6] = unitId;

            frame[7] = functionCode;
            frame[8] = (byte)(startAddress >> 8);
            frame[9] = (byte)(startAddress & 0xFF);
            frame[10] = (byte)(quantity >> 8);
            frame[11] = (byte)(quantity & 0xFF);

            return frame;
        }

        public byte[] ParseResponse(byte[] responseBytes, byte expectedFunctionCode)
        {
            if (responseBytes == null || responseBytes.Length < 9)
                throw new Exception("Invalid Modbus TCP Response.");

            byte functionCode = responseBytes[7];

            if ((functionCode & 0x80) != 0)
                throw new ModbusException(responseBytes[8]); 

            if (functionCode != expectedFunctionCode)
                throw new Exception("Unexpected function code received.");

            byte byteCount = responseBytes[8];
            byte[] data = new byte[byteCount];
            Array.Copy(responseBytes, 9, data, 0, byteCount);
            return data;
        }
    }
}
