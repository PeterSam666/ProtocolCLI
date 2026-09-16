using ModbusDriver.Core;
using System;

namespace ModbusDriver.Formatters
{
    public class ModbusRtuOverUdpFormatter : IModbusFormatter
    {
        public byte[] BuildRequest(byte unitId, byte functionCode, ushort startAddress, ushort quantity, byte[] data = null)
        {
            byte[] frame;

            if (data == null)
            {
                frame = new byte[8];
                frame[0] = unitId;
                frame[1] = functionCode;
                frame[2] = (byte)(startAddress >> 8);
                frame[3] = (byte)(startAddress & 0xFF);
                frame[4] = (byte)(quantity >> 8);
                frame[5] = (byte)(quantity & 0xFF);

                ushort crc0 = CalculateCrc(frame, 6);
                frame[6] = (byte)(crc0 & 0xFF);
                frame[7] = (byte)(crc0 >> 8);
                return frame;
            }

            byte byteCount = (byte)data.Length;
            frame = new byte[7 + byteCount + 2];
            frame[0] = unitId;
            frame[1] = functionCode;
            frame[2] = (byte)(startAddress >> 8);
            frame[3] = (byte)(startAddress & 0xFF);
            frame[4] = (byte)(quantity >> 8);
            frame[5] = (byte)(quantity & 0xFF);
            frame[6] = byteCount;
            Array.Copy(data, 0, frame, 7, byteCount);

            ushort crc = CalculateCrc(frame, frame.Length - 2);
            frame[frame.Length - 2] = (byte)(crc & 0xFF);
            frame[frame.Length - 1] = (byte)(crc >> 8);
            return frame;
        }

        public byte[] ParseResponse(byte[] responseBytes, byte expectedFunctionCode)
        {
            if (responseBytes == null || responseBytes.Length < 5)
            {
                throw new ModbusException("Response data is too short.");
            }

            bool isException = (responseBytes[1] & 0x80) != 0;
            if (isException)
            {
                ushort excCrc = (ushort)(responseBytes[3] | (responseBytes[4] << 8));
                ushort excCalc = CalculateCrc(responseBytes, 3);
                if (excCrc != excCalc)
                {
                    throw new ModbusException("CRC Check failed! Data corrupted.");
                }
                throw new ModbusException(responseBytes[2]);
            }

            byte byteCount = responseBytes[2];
            int expectedFrameLength = 3 + byteCount + 2;

            if (responseBytes.Length < expectedFrameLength)
            {
                throw new ModbusException("Response frame length does not match Modbus RTU byte count specification.");
            }

            ushort receivedCrc = (ushort)(responseBytes[expectedFrameLength - 2] | (responseBytes[expectedFrameLength - 1] << 8));
            ushort calculatedCrc = CalculateCrc(responseBytes, expectedFrameLength - 2);
            if (receivedCrc != calculatedCrc)
            {
                throw new ModbusException("CRC Check failed! Data corrupted.");
            }

            if (responseBytes[1] != expectedFunctionCode)
            {
                throw new ModbusException($"Unexpected function code received. Expected: {expectedFunctionCode}, but got other code.");
            }

            byte[] data = new byte[byteCount];
            Array.Copy(responseBytes, 3, data, 0, byteCount);
            return data;
        }

        public int TryGetFrameLength(byte[] buffer, int bytesReceived, byte expectedFunctionCode)
        {
            return bytesReceived;
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