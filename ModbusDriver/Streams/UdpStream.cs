using ModbusDriver.Core;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ModbusDriver.Streams
{
    public class UdpStream : IModbusStream
    {
        private readonly string _ipAddress;
        private readonly int _port;
        private UdpClient _udpClient;
        private IPEndPoint _remoteEndPoint;
        private bool _isConnected = false;
        private bool _isDisposed = false;

        public bool IsConnected => _isConnected;

        public int ReadTimeout { get; set; } = 1000;
        public int WriteTimeout { get; set; } = 1000;

        public UdpStream(string ipAddress, int port = 502)
        {
            _ipAddress = ipAddress ?? throw new ArgumentNullException(nameof(ipAddress));
            _port = port;
        }

        public void Connect()
        {
            if (_isConnected) return;

            _udpClient = new UdpClient();
            _remoteEndPoint = new IPEndPoint(IPAddress.Parse(_ipAddress), _port);

            _udpClient.Connect(_remoteEndPoint);
            _udpClient.Client.ReceiveTimeout = ReadTimeout;
            _udpClient.Client.SendTimeout = WriteTimeout;

            _isConnected = true;
        }

        public void Disconnect()
        {
            _udpClient?.Close();
            _isConnected = false;
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Modbus UDP endpoint is not connected.");

            byte[] toSend = buffer;
            if (offset != 0 || count != buffer.Length)
            {
                toSend = new byte[count];
                Array.Copy(buffer, offset, toSend, 0, count);
            }

            _udpClient.Send(toSend, toSend.Length);
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            if (!IsConnected)
                throw new InvalidOperationException("Modbus UDP endpoint is not connected.");

            IPEndPoint remoteEp = _remoteEndPoint;
            byte[] received;

            try
            {
                received = _udpClient.Receive(ref remoteEp);
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.TimedOut)
            {
                throw new ModbusException("Timed out waiting for Modbus UDP response.");
            }

            if (received.Length > count)
            {
                throw new ModbusException("Received UDP datagram larger than the read buffer.");
            }

            Array.Copy(received, 0, buffer, offset, received.Length);
            return received.Length;
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
                    _udpClient?.Dispose();
                }
                _isDisposed = true;
            }
        }
    }
}
