using System;

namespace ModbusDriver.Core
{
    /// <summary>
    /// Represents errors that occur during Modbus communication.
    /// This class is optimized for .NET Standard 2.0 and tailored for system engineers.
    /// </summary>
    public class ModbusException : Exception
    {
        /// <summary>
        /// Gets the raw Modbus exception code returned by the slave/server device.
        /// </summary>
                public byte ExceptionCode { get; }

        /// <summary>
        /// Initializes a new instance of the ModbusException class with a specific exception code.
        /// It formats the main '.Message' property to be clear and scannable for engineers on-site.
        /// </summary>
        /// <param name="exceptionCode">The standard Modbus exception code (0x01 to 0x0B).</param>
        public ModbusException(byte exceptionCode)
            : base($"[Modbus Error 0x{exceptionCode:X2}] -> {GetErrorMessage(exceptionCode)}")
        {
            ExceptionCode = exceptionCode;
        }

        /// <summary>
        /// A static helper method that maps raw Modbus exception codes to their 
        /// standard industrial definitions and actionable troubleshooting steps.
        /// </summary>
        /// <param name="code">The standard Modbus exception code.</param>
        /// <returns>A detailed English description of the error.</returns>
        public static string GetErrorMessage(byte code)
        {
            switch (code)
            {
                case 0x01:
                    return "Illegal Function | The function code is not supported by the device. Check your Function Code implementation.";
                case 0x02:
                    return "Illegal Data Address | The data address specified does not exist in the device. Check your register offsets/mapping.";
                case 0x03:
                    return "Illegal Data Value | The data value sent is out of the acceptable range. Check data scaling or boundaries.";
                case 0x04:
                    return "Slave Device Failure | An unrecoverable error occurred while the device was processing. Check device hardware/status.";
                case 0x05:
                    return "Acknowledge | The device has accepted the request and is processing it long-term. App should poll for completion later.";
                case 0x06:
                    return "Slave Device Busy | The device is busy processing another command. The application should retry sending the frame later.";
                case 0x08:
                    return "Memory Parity Error | The device detected a parity error when attempting to read/write memory. Check internal memory health.";
                case 0x0A:
                    return "Gateway Path Unavailable | The gateway cannot route the request to the target device. Check gateway configuration/routing table.";
                case 0x0B:
                    return "Gateway Target Device Failed to Respond | The gateway targeted the device, but no response was received. Check target cable connection.";
                default:
                    return $"Unknown Error | An undefined or vendor-specific Modbus exception occurred.";
            }
        }
    }
}
