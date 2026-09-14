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

            _tcpClient.ReceiveTimeout = 1000;
            _tcpClient.SendTimeout = 1000;
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

            return _networkStream.Read(buffer, offset, count);
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
