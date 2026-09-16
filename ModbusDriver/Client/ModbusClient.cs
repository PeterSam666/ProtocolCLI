using ModbusDriver.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ModbusDriver.Client
{
    public class ModbusClient : IDisposable
    {
        private readonly IModbusFormatter _formatter;
        private readonly IModbusStream _stream;
        private readonly object _networkLock = new object();
        private bool _isDisposed = false;

        public ModbusClient(IModbusFormatter formatter, IModbusStream stream)
        {
            _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _stream = stream ?? throw new ArgumentNullException(nameof(stream));
        }

        public void Connect() => _stream.Connect();
        public void Disconnect() => _stream.Disconnect();

        public int ReadTimeout
        {
            get => _stream.ReadTimeout;
            set => _stream.ReadTimeout = value;
        }

        public int WriteTimeout
        {
            get => _stream.WriteTimeout;
            set => _stream.WriteTimeout = value;
        }

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
            byte[] rawData = SendAndReceive(unitId, 3, startAddress, quantity);

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
            SendAndReceive(unitId, 15, startAddress, quantity, dataBytes);
        }

        public void WriteMultipleRegisters(byte unitId, ushort startAddress, ushort[] values)
        {
            if (values is null || values.Length is 0)
                throw new ArgumentException("Write value cannot be null or empty.");

            byte[] dataBytes = new byte[values.Length * 2];
            for (int i = 0; i < values.Length; i++)
            {
                dataBytes[i * 2] = (byte)(values[i] >> 8);
                dataBytes[i * 2 + 1] = (byte)(values[i] & 0xFF);
            }

            ushort quantity = (ushort)values.Length;
            SendAndReceive(unitId, 16, startAddress, quantity, dataBytes);
        }

        private byte[] SendAndReceive(byte unitId, byte functionCode, ushort startAddress, ushort quantityOrValue, byte[] data = null)
        {
            lock (_networkLock)
            {
                byte[] request = _formatter.BuildRequest(unitId, functionCode, startAddress, quantityOrValue, data);

                _stream.Write(request, 0, request.Length);

                byte[] buffer = new byte[512];
                int bytesRead = _stream.Read(buffer, 0, buffer.Length);

                byte[] rawResponse = new byte[bytesRead];
                Array.Copy(buffer, 0, rawResponse, 0, bytesRead);

                return _formatter.ParseResponse(rawResponse, functionCode);
            }
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

        public void Dispose()
        {
            if (!_isDisposed)
            {
                _stream.Dispose();
                _isDisposed = true;
            }
        }
    }
}
