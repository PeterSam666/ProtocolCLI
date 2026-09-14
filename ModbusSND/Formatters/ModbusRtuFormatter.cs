using ModbusDriver.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModbusDriver.Formatters
{
    public class ModbusRtuFormatter : IModbusFormatter
    {
        public byte[] BuildRequest(byte unitId, byte functionCode, ushort startAddress, ushort quantity)
        {
            byte[] frame = new byte[8];
            frame[0] = unitId;
            frame[1] = functionCode;
            frame[2] = (byte)(startAddress >> 8);  
            frame[3] = (byte)(startAddress & 0xFF); 
            frame[4] = (byte)(quantity >> 8);
            frame[5] = (byte)(quantity & 0xFF);

            ushort crc = CalculateCrc(frame, 6);
            frame[6] = (byte)(crc & 0xFF);
            frame[7] = (byte)(crc >> 8);

            return frame;
        }

        public byte[] ParseResponse(byte[] responseBytes, byte expectedFunctionCode)
        {
            if (responseBytes == null || responseBytes.Length < 5)
                throw new Exception("Response data is too short.");

            ushort receivedCrc = (ushort)(responseBytes[responseBytes.Length - 2] | responseBytes[responseBytes.Length - 1] << 8);
            ushort calculatedCrc = CalculateCrc(responseBytes, responseBytes.Length - 2);
            if (receivedCrc != calculatedCrc)
                throw new Exception("CRC Check failed! Data corrupted.");

            if ((responseBytes[1] & 0x80) != 0)
                throw new ModbusException(responseBytes[2]);

            if (responseBytes[1] != expectedFunctionCode)
                throw new Exception("Unexpected function code received.");

            byte byteCount = responseBytes[2];
            byte[] data = new byte[byteCount];
            Array.Copy(responseBytes, 3, data, 0, byteCount);
            return data;
        }

        private ushort CalculateCrc(byte[] buffer, int length)
        {
            ushort crc = 0xFFFF;
            for (int i = 0; i < length; i++)
            {
                crc ^= buffer[i];
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x0001) != 0)
                    {
                        crc >>= 1;
                        crc ^= 0xA001;
                    }
                    else
                    {
                        crc >>= 1;
                    }
                }
            }
            return crc;
        }
    }
}
