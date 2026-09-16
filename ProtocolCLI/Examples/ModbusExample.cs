using ModbusDriver.Client;
using ModbusDriver.Core;
using ModbusDriver.Extensions;
using ModbusDriver.Streams;
using System;
using System.IO.Ports;

namespace ProtocolCLI.Examples
{
    public static class ModbusExample
    {
        public static void RunAll()
        {
            ExampleRtu();
            ExampleAscii();
            ExampleTcp();
            ExampleUdp();
            ExampleRtuOverTcp();
            ExampleRtuOverUdp();
            ExampleWrite();
        }

        // -----------------------------------------------------------------
        // RTU (serial, CRC-16 framing) - the classic RS-485 setup
        // -----------------------------------------------------------------
        public static void ExampleRtu()
        {
            Console.WriteLine("=== RTU (Serial) ===");

            using (var stream = new SerialStream("COM3", 9600, Parity.None, 8, StopBits.One))
            {
                using (var client = ModbusClientFactory.CreateRtu(stream))
                {
                    try
                    {
                        client.Connect();

                        ushort[] registers = client.ReadHoldingRegisters(unitId: 1, startAddress: 0, quantity: 4);
                        float[] values = registers.ToFloatArray(count: 2, ByteOrder.LittleEndianInvert);

                        Console.WriteLine($"Value 1: {values[0]}, Value 2: {values[1]}");
                    }
                    catch (ModbusException ex)
                    {
                        Console.WriteLine($"[Modbus Error] {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[System Error] {ex.Message}");
                    }
                }
            }
        }

        // -----------------------------------------------------------------
        // ASCII (serial, human-readable hex framing) - older/legacy controllers
        // -----------------------------------------------------------------
        public static void ExampleAscii()
        {
            Console.WriteLine("\n=== ASCII (Serial) ===");

            using (var stream = new SerialStream("COM4", 9600, Parity.None, 8, StopBits.One))
            {
                using (var client = ModbusClientFactory.CreateAscii(stream))
                {
                    try
                    {
                        client.Connect();
                        ushort[] registers = client.ReadHoldingRegisters(unitId: 1, startAddress: 0, quantity: 2);
                        float temperature = registers.ToFloat(ByteOrder.BigEndian);
                        Console.WriteLine($"Temperature: {temperature}");
                    }
                    catch (ModbusException ex)
                    {
                        Console.WriteLine($"[Modbus Error] {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[System Error] {ex.Message}");
                    }
                }
            }
        }

        // -----------------------------------------------------------------
        // Modbus TCP (MBAP framing over Ethernet) - the most common PLC setup
        // -----------------------------------------------------------------
        public static void ExampleTcp()
        {
            Console.WriteLine("\n=== Modbus TCP ===");

            using (var stream = new TcpStream("192.168.1.10", port: 502))
            {
                using (var client = ModbusClientFactory.CreateTcp(stream))
                {
                    try
                    {
                        client.Connect();

                        ushort[] registers = client.ReadHoldingRegisters(unitId: 1, startAddress: 100, quantity: 4);
                        float[] values = registers.ToAllFloats(ByteOrder.BigEndianInvert);

                        Console.WriteLine($"Read {values.Length} floats: {string.Join(", ", values)}");
                    }
                    catch (ModbusException ex)
                    {
                        Console.WriteLine($"[Modbus Error] {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[System Error] {ex.Message}");
                    }
                }
            }
        }

        // -----------------------------------------------------------------
        // Modbus UDP (same MBAP framing as TCP, delivered as a datagram)
        // -----------------------------------------------------------------
        public static void ExampleUdp()
        {
            Console.WriteLine("\n=== Modbus UDP ===");

            using (var stream = new UdpStream("192.168.1.10", port: 502))
            {
                using (var client = ModbusClientFactory.CreateUdp(stream)) 
                {
                    try
                    {
                        client.Connect();

                        ushort[] registers = client.ReadHoldingRegisters(unitId: 1, startAddress: 100, quantity: 2);
                        float value = registers.ToFloat(ByteOrder.BigEndian);

                        Console.WriteLine($"Value: {value}");
                    }
                    catch (ModbusException ex)
                    {
                        // Remember: UDP has no delivery guarantee. A timeout here could mean
                        // the device didn't respond, OR the request/response was simply lost.
                        Console.WriteLine($"[Modbus Error] {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[System Error] {ex.Message}");
                    }
                }
            }
        }

        // -----------------------------------------------------------------
        // RTU framing carried over TCP (serial-to-Ethernet gateway)
        // -----------------------------------------------------------------
        public static void ExampleRtuOverTcp()
        {
            Console.WriteLine("\n=== RTU over TCP ===");

            using (var stream = new TcpStream("192.168.1.20", port: 502))
            {
                using (var client = ModbusClientFactory.CreateRtuOverTcp(stream))
                {
                    try
                    {
                        client.Connect();
                        ushort[] registers = client.ReadHoldingRegisters(unitId: 1, startAddress: 0, quantity: 2);
                        int value = registers.ToInt32(ByteOrder.BigEndian);
                        Console.WriteLine($"Value: {value}");
                    }
                    catch (ModbusException ex)
                    {
                        Console.WriteLine($"[Modbus Error] {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[System Error] {ex.Message}");
                    }
                }
            }
        }

        // -----------------------------------------------------------------
        // RTU framing carried over UDP
        // -----------------------------------------------------------------
        public static void ExampleRtuOverUdp()
        {
            Console.WriteLine("\n=== RTU over UDP ===");

            using (var stream = new UdpStream("192.168.1.20", port: 502))
            {
                using (var client = ModbusClientFactory.CreateRtuOverUdp(stream))
                {
                    try
                    {
                        client.Connect();
                        ushort[] registers = client.ReadHoldingRegisters(unitId: 1, startAddress: 0, quantity: 2);
                        int value = registers.ToInt32(ByteOrder.BigEndian);
                        Console.WriteLine($"Value: {value}");
                    }
                    catch (ModbusException ex)
                    {
                        Console.WriteLine($"[Modbus Error] {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[System Error] {ex.Message}");
                    }
                }
            }
        }

        // -----------------------------------------------------------------
        // Writing values - works the same way regardless of transport
        // -----------------------------------------------------------------
        public static void ExampleWrite()
        {
            Console.WriteLine("\n=== Writing values ===");

            using (var stream = new TcpStream("192.168.1.10", port: 502))
            {
                using (var client = ModbusClientFactory.CreateTcp(stream))
                {
                    try
                    {
                        client.Connect();
                        // Turn on a single coil (e.g. start a pump)
                        client.WriteSingleCoil(unitId: 1, address: 0, value: true);
                        // Set a single register (e.g. a setpoint)
                        client.WriteSingleRegister(unitId: 1, address: 10, value: 1500);
                        // Write multiple registers at once (e.g. 100.0f as raw big-endian register bytes)
                        client.WriteMultipleRegisters(unitId: 1, startAddress: 100,
                            values: new ushort[] { 0x4248, 0x0000 });
                        Console.WriteLine("Write completed successfully.");
                    }
                    catch (ModbusException ex)
                    {
                        Console.WriteLine($"[Modbus Error] {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[System Error] {ex.Message}");
                    }
                }
            }
        }
    }
}