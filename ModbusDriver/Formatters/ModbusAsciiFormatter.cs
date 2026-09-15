using ModbusDriver.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace ModbusDriver.Formatters
{
    public class ModbusAsciiFormatter : IModbusFormatter
    {
        public byte[] BuildRequest(byte unitId, byte functionCode, ushort startAddress, ushort quantity)
        {
            byte[] rtuFrame = new byte[6];
            rtuFrame[0] = unitId;
            rtuFrame[1] = functionCode;
            rtuFrame[2] = (byte)(startAddress >> 8);
            rtuFrame[3] = (byte)(startAddress & 0xFF);
            rtuFrame[4] = (byte)(quantity >> 8);
            rtuFrame[5] = (byte)(quantity & 0xFF);

            byte lrc = CalculateLrc(rtuFrame, rtuFrame.Length);

            StringBuilder sb = new StringBuilder();
            sb.Append(":");

            for (int i = 0; i < rtuFrame.Length; i++)
            {
                sb.Append(rtuFrame[i].ToString("X2"));
            }
            sb.Append(lrc.ToString("X2"));
            sb.Append("\r\n");

            return Encoding.ASCII.GetBytes(sb.ToString());
        }

        public byte[] ParseResponse(byte[] responseBytes, byte expectedFunctionCode)
        {
            if (responseBytes == null || responseBytes.Length < 11)
            {
                throw new ModbusException("Response data is too short for Modbus ASCII framing.");
            }

            string asciiString = Encoding.ASCII.GetString(responseBytes).Trim();

            if (!asciiString.StartsWith(":"))
            {
                throw new ModbusException("Invalid Modbus ASCII frame. Missing starting character ':'.");
            }

            string hexData = asciiString.Substring(1);
            byte[] rawBytes = new byte[hexData.Length / 2];
            for (int i = 0; i < rawBytes.Length; i++)
            {
                rawBytes[i] = Convert.ToByte(hexData.Substring(i * 2, 2), 16);
            }

            byte byteCount = rawBytes[2];
            int expectedFrameLength = 3 + byteCount + 1; // ID + FC + ByteCount + เนื้อข้อมูล + 1 ไบต์ LRC

            byte receivedLrc = rawBytes[expectedFrameLength - 1];
            byte calculatedLrc = CalculateLrc(rawBytes, expectedFrameLength - 1);
            if (receivedLrc != calculatedLrc)
            {
                throw new ModbusException("LRC Check failed! Modbus ASCII data corrupted between lines.");
            }

            if ((rawBytes[1] & 0x80) != 0)
            {
                throw new ModbusException(rawBytes[2]);
            }

            if (rawBytes[1] != expectedFunctionCode)
            {
                throw new ModbusException($"Unexpected function code received. Expected: {expectedFunctionCode}, but got: {rawBytes[1]}");
            }

            byte[] data = new byte[byteCount];
            Array.Copy(rawBytes, 3, data, 0, byteCount);
            return data;
        }

        private byte CalculateLrc(byte[] data, int length)
        {
            byte lrc = 0;
            for (int i = 0; i < length; i++)
            {
                lrc += data[i];
            }
            return (byte)(~lrc + 1);
        }
    }
}
