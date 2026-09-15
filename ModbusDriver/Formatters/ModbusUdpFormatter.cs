using ModbusDriver.Core;
using System;

namespace ModbusDriver.Formatters
{
    public class ModbusUdpFormatter : IModbusFormatter
    {
        private ushort _transactionId = 0;

        public byte[] BuildRequest(byte unitId, byte functionCode, ushort startAddress, ushort quantity, byte[] data = null)
        {
            _transactionId++;

            byte[] pdu;

            if (data == null)
            {
                pdu = new byte[6];
                pdu[0] = unitId;
                pdu[1] = functionCode;
                pdu[2] = (byte)(startAddress >> 8);
                pdu[3] = (byte)(startAddress & 0xFF);
                pdu[4] = (byte)(quantity >> 8);
                pdu[5] = (byte)(quantity & 0xFF);
            }
            else
            {
                byte byteCount = (byte)data.Length;
                pdu = new byte[7 + byteCount];
                pdu[0] = unitId;
                pdu[1] = functionCode;
                pdu[2] = (byte)(startAddress >> 8);
                pdu[3] = (byte)(startAddress & 0xFF);
                pdu[4] = (byte)(quantity >> 8);
                pdu[5] = (byte)(quantity & 0xFF);
                pdu[6] = byteCount;
                Array.Copy(data, 0, pdu, 7, byteCount);
            }

            ushort length = (ushort)pdu.Length;

            byte[] frame = new byte[6 + pdu.Length];
            frame[0] = (byte)(_transactionId >> 8);
            frame[1] = (byte)(_transactionId & 0xFF);
            frame[2] = 0;
            frame[3] = 0;
            frame[4] = (byte)(length >> 8);
            frame[5] = (byte)(length & 0xFF);
            Array.Copy(pdu, 0, frame, 6, pdu.Length);

            return frame;
        }

        public byte[] ParseResponse(byte[] responseBytes, byte expectedFunctionCode)
        {
            if (responseBytes == null || responseBytes.Length < 9)
            {
                throw new ModbusException("Invalid Modbus UDP Response.");
            }

            byte functionCode = responseBytes[7];

            if ((functionCode & 0x80) != 0)
            {
                throw new ModbusException(responseBytes[8]);
            }

            if (functionCode != expectedFunctionCode)
            {
                throw new ModbusException("Unexpected function code received.");
            }

            byte byteCount = responseBytes[8];
            if (responseBytes.Length < 9 + byteCount)
            {
                throw new ModbusException("Modbus UDP datagram shorter than declared byte count.");
            }

            byte[] data = new byte[byteCount];
            Array.Copy(responseBytes, 9, data, 0, byteCount);
            return data;
        }

        public int TryGetFrameLength(byte[] buffer, int bytesReceived, byte expectedFunctionCode)
        {
            return bytesReceived;
        }
    }
}