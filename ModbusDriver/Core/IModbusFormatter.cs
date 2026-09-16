using System;
using System.Collections.Generic;
using System.Text;

namespace ModbusDriver.Core
{
    public interface IModbusFormatter
    {
        byte[] BuildRequest(byte unitId, byte functionCode, ushort startAddress, ushort quantity, byte[] data = null);
        byte[] ParseResponse(byte[] responseBytes, byte expectedFunctionCode);
    }
}
