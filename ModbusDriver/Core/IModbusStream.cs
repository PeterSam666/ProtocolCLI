using System;
using System.Collections.Generic;
using System.Text;

namespace ModbusDriver.Core
{
    public interface IModbusStream : IDisposable
    {
        void Connect();
        void Disconnect();
        bool IsConnected { get; }
        void Write(byte[] buffer, int offset, int count);
        int Read(byte[] buffer, int offset, int count);
    }
}
