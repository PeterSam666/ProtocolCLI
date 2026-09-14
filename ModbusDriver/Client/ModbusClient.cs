using ModbusDriver.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ModbusDriver.Client
{
    public class ModbusClient
    {
        private readonly IModbusFormatter _formatter;
        private readonly IModbusStream _stream;

        public ModbusClient(IModbusFormatter formatter, IModbusStream stream)
        {
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        public void Connect() => _stream.Connect();
        public void Disconnect() => _stream.Disconnect();

        public bool[] ReadCoils(byte unitId, ushort startAddress, ushort quantity)
        {
            byte[] rawData = SendAndReceive(unitId, 1, startAddress, quantity);
            return ConvertBytesToBoils(rawData, quantity);
        }

        public bool[] ReadDiscreteInputs(byte unitId, ushort startAddress, ushort quantity)
        {
            byte[] rawData = SendAndReceive(unitId, 2, startAddress, quantity);
            return ConvertBytesToBoils(rawData, quantity);
        }

        public ushort[] ReadHoldingRegisters(byte unitId, ushort startAddress, ushort quantity)
        {
            byte[] requestBuffer = _formatter.BuildRequest(unitId, 3, startAddress, quantity);

            _stream.Write(requestBuffer, 0, requestBuffer.Length);

            byte[] readBuffer = new byte[256];
            int bytesRead = _stream.Read(readBuffer, 0, readBuffer.Length);

            byte[] rawResponse = new byte[bytesRead];
            Array.Copy(readBuffer, rawResponse, bytesRead);

            byte[] rawData = _formatter.ParseResponse(rawResponse, 3);

            ushort[] registers = new ushort[rawData.Length / 2];
            for (int i = 0; i < registers.Length; i++)
            {
                registers[i] = (ushort)((rawData[i * 2] << 8) | rawData[i * 2 + 1]);
            }


            return registers;
        }

        public ushort[] ReadInputRegisters(byte unitId, ushort startAddress, ushort quantity)
        {
            byte[] rawData = SendAndReceive(unitId, 4, startAddress, quantity);
            return ConvertBytesToRegisters(rawData);
        }

        public void WriteSingleCoil(byte unitId, ushort address, bool value)
        {
            ushort valToSend = (ushort)(value ? 0xFF00 : 0x0000);
            SendAndReceive(unitId, 5, address, valToSend);
        }

        public void WriteSingleRegister(byte unitId, ushort address, ushort value)
        {
            SendAndReceive(unitId, 6, address, value);
        }

        public void WriteMultipleCoils(byte unitId, ushort startAddress, bool[] values)
        {
            if (values is null || values.Length is 0)
            {
                throw new ArgumentException("Write value cannot be null or empty.");
            }

            BitArray bitArray = new BitArray(values);
            byte[] dataBytes = new byte[(values.Length + 7) / 8];
            bitArray.CopyTo(dataBytes, 0);

            ushort quantity = (ushort)values.Length;
            SendAndReceive(unitId, 15, startAddress, quantity);
        }

        public void WriteMultipleRegisters(byte unitId, ushort startAddress, ushort[] values)
        {
            if (values is null || values.Length is 0)
            {
                throw new ArgumentException("Write value cannot be null or empty.");
            }

            ushort quantity = (ushort)values.Length;
            SendAndReceive(unitId, 16, startAddress, quantity);
        }

        private byte[] SendAndReceive(byte unitId, byte functionCode, ushort startAddress, ushort quantityOrValue)
        {
            byte[] request = _formatter.BuildRequest(unitId, functionCode, startAddress, quantityOrValue);

            _stream.Write(request, 0, request.Length);

            byte[] buffer = new byte[512];
            int bytesRead = _stream.Read(buffer, 0, buffer.Length);

            byte[] rawResponse = new byte[bytesRead];
            Array.Copy(buffer, rawResponse, bytesRead);

            return _formatter.ParseResponse(rawResponse, functionCode);
        }

        private ushort[] ConvertBytesToRegisters(byte[] rawData)
        {
            ushort[] registers = new ushort[rawData.Length / 2];
            for (int i = 0; i < registers.Length; i++)
            {
                registers[i] = (ushort)((rawData[i * 2] << 8) | rawData[i * 2 + 1]);
            }
            return registers;
        }

        private bool[] ConvertBytesToBoils(byte[] rawData, ushort quantity)
        {
            bool[] bools = new bool[quantity];
            BitArray bitArray = new BitArray(rawData);
            for (int i = 0; i < quantity; i++)
            {
                bools[i] = bitArray[i];
            }
            return bools;
        }
    }
}
