using System;
using System.IO.Ports;
using ModbusDriver.Core;

namespace ModbusDriver.Streams
{
    public class SerialStream : IModbusStream
    {
        private SerialPort _serialPort;
        private readonly string _portName;
        private readonly int _baudRate;
        private readonly Parity _parity;
        private readonly int _dataBits;
        private readonly StopBits _stopBits;
        private bool _isDisposed;

        public int ReadTimeout { get; set; } = 1000;
        public int WriteTimeout { get; set; } = 1000;

        public SerialStream(string portName, int baudRate = 9600, Parity parity = Parity.Even, int dataBits = 8, StopBits stopBits = StopBits.One)
        {
            _portName = portName ?? throw new ArgumentNullException(nameof(portName));
            _baudRate = baudRate;
            _parity = parity;
            _dataBits = dataBits;
            _stopBits = stopBits;
        }

        public bool IsConnected => _serialPort != null && _serialPort.IsOpen;

        public void Connect()
        {
            if (IsConnected)
            {
                return;
            }

            _serialPort = new SerialPort(_portName, _baudRate, _parity, _dataBits, _stopBits)
            {
                ReadTimeout = ReadTimeout,
                WriteTimeout = WriteTimeout
            };
            _serialPort.Open();
        }

        public void Disconnect()
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Close();
            }
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Serial port is not open.");
            }

            _serialPort.Write(buffer, offset, count);
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Serial port is not open.");
            }

            return _serialPort.Read(buffer, offset, count);
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                Disconnect();
                _serialPort?.Dispose();
                _isDisposed = true;
            }
        }
    }
}
