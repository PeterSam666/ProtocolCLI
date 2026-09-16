using ModbusDriver.Client;
using ModbusDriver.Core;
using ModbusDriver.Formatters;
using ModbusDriver.Streams;

namespace ModbusDriver.Client
{
    /// <summary>
    /// One-liner constructors for ModbusClient - picks the right formatter+stream pair
    /// for each transport so callers don't have to know which formatter goes with which stream.
    /// </summary>
    public static class ModbusClientFactory
    {
        /// <summary>Modbus TCP (MBAP framing) - the standard for PLCs over Ethernet.</summary>
        public static ModbusClient CreateTcp(IModbusStream tcpStream)
            => new ModbusClient(new ModbusTcpFormatter(), tcpStream);

        /// <summary>Modbus UDP (same MBAP framing as TCP, delivered as a datagram).</summary>
        public static ModbusClient CreateUdp(IModbusStream udpStream)
            => new ModbusClient(new ModbusUdpFormatter(), udpStream);

        /// <summary>RTU framing (CRC-16) carried over a TCP socket instead of a serial port -
        /// common with serial-to-Ethernet gateways.</summary>
        public static ModbusClient CreateRtuOverTcp(IModbusStream tcpStream)
            => new ModbusClient(new ModbusRtuOverTcpFormatter(), tcpStream);

        /// <summary>RTU framing (CRC-16) carried over a UDP datagram instead of a serial port.</summary>
        public static ModbusClient CreateRtuOverUdp(IModbusStream udpStream)
            => new ModbusClient(new ModbusRtuOverUdpFormatter(), udpStream);

        /// <summary>
        /// RTU framing over a real serial port. Pass in an already-configured IModbusStream
        /// (e.g. your SerialStream with COM port/baud rate/parity) since those settings are
        /// specific to your serial hardware setup.
        /// </summary>
        public static ModbusClient CreateRtu(IModbusStream serialStream)
            => new ModbusClient(new ModbusRtuFormatter(), serialStream);

        /// <summary>ASCII framing over a real serial port - same note as CreateRtu above.</summary>
        public static ModbusClient CreateAscii(IModbusStream serialStream)
            => new ModbusClient(new ModbusAsciiFormatter(), serialStream);
    }
}