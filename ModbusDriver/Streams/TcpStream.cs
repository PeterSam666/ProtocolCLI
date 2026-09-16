using ModbusDriver.Core;
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;

namespace ModbusDriver.Streams
{
    public class TcpStream : IModbusStream
    {
        private readonly string _ipAddress;
        private readonly int _port;
        private TcpClient _tcpClient;
        private NetworkStream _networkStream;
        private bool _isDisposed = false;

        public bool IsConnected => _tcpClient != null && _tcpClient.Connected;

        public int ReadTimeout { get; set; } = 1000;
        public int WriteTimeout { get; set; } = 1000;

        public TcpStream(string ipAddress, int port = 502)
        {
            _ipAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
            _port = port;
        }

        public void Connect()
        {
            if (IsConnected)
            {
                return;
            }

            _tcpClient = new TcpClient();
            _tcpClient.Connect(_ipAddress, _port);
            _networkStream = _tcpClient.GetStream();

            _tcpClient.ReceiveTimeout = ReadTimeout;
            _tcpClient.SendTimeout = WriteTimeout;
        }

        public void Disconnect()
        {
            _networkStream?.Close();
            _tcpClient?.Close();
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Modbus TCP device is not connected.");
            }


            _networkStream.Write(buffer, offset, count);
            _networkStream.Flush();
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("Modbus TCP device is not connected.");
            }

            int headerRead = ReadExact(buffer, offset, 6);
            if (headerRead < 6) return headerRead;

            int remaining = (buffer[offset + 4] << 8) | buffer[offset + 5]; // length field
            int bodyRead = ReadExact(buffer, offset + 6, remaining);

            return 6 + bodyRead;
        }

        private int ReadExact(byte[] buffer, int offset, int count)
        {
            int totalRead = 0;
            while (totalRead < count)
            {
                int read = _networkStream.Read(buffer, offset + totalRead, count - totalRead);
                if (read == 0) break; // connection closed
                totalRead += read;
            }
            return totalRead;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    Disconnect();
                    _networkStream?.Dispose();
                    _tcpClient?.Dispose();
                }
                _isDisposed = true;
            }
        }
    }
}
