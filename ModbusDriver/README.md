# ModbusDriver

A .NET Modbus client library supporting multiple transports and framing styles, with
built-in helpers for decoding raw registers into common data types (float, int, long,
double, etc.) without manually handling byte order.

## Features

- **Multiple transports**: Serial (RS-232/RS-485), TCP, and UDP.
- **Multiple framing styles**: RTU (CRC-16), ASCII (LRC), and Modbus TCP/UDP (MBAP header).
- **Mix and match**: RTU or ASCII framing can also be carried over TCP or UDP
  (common with serial-to-Ethernet gateways) - transport and framing are fully decoupled.
- **Type-safe register decoding**: `ushort[] -> float / int / uint / long / ulong / double`
  with explicit byte-order handling (`BigEndian`, `BigEndianInvert`, `LittleEndianInvert`,
  `LittleEndian`) to match whatever convention your device's datasheet specifies.
- **Three decoding strategies** for reading multiple values from one response:
  strict (throws on incomplete data), safe (`Try...` pattern, no exceptions), and
  best-effort (`ToAll...`, decodes as many complete values as the data allows).
- **One-liner client construction** via `ModbusClientFactory` - no need to manually
  pair up a formatter with a stream.
- **`IDisposable` support** on `ModbusClient` so `using` statements clean up the
  underlying connection automatically.

## Project Structure

```
ModbusDriver/
├── Client/            ModbusClient (the main entry point) + ModbusClientFactory
├── Core/               Shared contracts: IModbusFormatter, IModbusStream, ModbusException, ByteOrder
├── Extensions/         ModbusRegisterExtensions - decode ushort[] into .NET types
├── Formatters/         Frame encoding/decoding for RTU, ASCII, TCP, UDP, RTU-over-TCP, RTU-over-UDP
└── Streams/            Transport implementations: SerialStream, TcpStream, UdpStream

ModbusDriver.Examples/  Runnable usage examples (separate project - not part of the library build)
ModbusDriver.Tests/     xUnit test suite
```

## Installation

This library is not currently published to NuGet. Reference it as a project reference
or build and reference the DLL directly:

```bash
dotnet build ModbusDriver/ModbusDriver.csproj -c Release
```

To package it for distribution to other projects/teams:

```bash
dotnet pack ModbusDriver/ModbusDriver.csproj -c Release -o ./nupkg
```

## Quick Start

### Modbus TCP (most common - PLCs over Ethernet)

```csharp
using ModbusDriver.Client;
using ModbusDriver.Streams;
using ModbusDriver.Extensions;
using ModbusDriver.Core;

using var client = ModbusClientFactory.CreateTcp(new TcpStream("192.168.1.10", port: 502));
client.Connect();

ushort[] registers = client.ReadHoldingRegisters(unitId: 1, startAddress: 100, quantity: 2);
float temperature = registers.ToFloat(ByteOrder.BigEndianInvert);
```

### Serial (RTU)

```csharp
using System.IO.Ports;

using var client = ModbusClientFactory.CreateRtu(
    new SerialStream("COM3", 9600, Parity.None, 8, StopBits.One));
client.Connect();

ushort[] registers = client.ReadHoldingRegisters(unitId: 1, startAddress: 0, quantity: 4);
float[] values = registers.ToFloatArray(count: 2, ByteOrder.LittleEndianInvert);
```

### RTU framing over a TCP gateway

```csharp
using var client = ModbusClientFactory.CreateRtuOverTcp(new TcpStream("192.168.1.20", port: 502));
client.Connect();

ushort[] registers = client.ReadHoldingRegisters(unitId: 1, startAddress: 0, quantity: 2);
int value = registers.ToInt32(ByteOrder.BigEndian);
```

See [`ModbusDriver.Examples/ModbusExample.cs`](../ModbusDriver.Examples/ModbusExample.cs) for
a complete example of every transport, including UDP, RTU-over-UDP, and writing values.

## Byte Order

Check your device's datasheet - this is the #1 source of "the number looks almost right
but isn't" bugs.

|     `ByteOrder`      |   Byte sequence (example A,B,C,D)  |          Commonly used by          |
|----------------------|------------------------------------|------------------------------------|
| `BigEndian`          | A B C D                            | Most common default                |
| `BigEndianInvert`    | C D A B (word-swapped)             | Schneider, Moxa, many power meters |
| `LittleEndianInvert` | B A D C (byte-swapped within word) | Some vendor-specific devices       |
| `LittleEndian`       | D C B A (fully reversed)           | True little-endian devices         |

## Register Conversion

`ModbusRegisterExtensions` works on the result of **any** read function
(`ReadHoldingRegisters`, `ReadInputRegisters`, etc.) - it doesn't care where the
`ushort[]` came from.

| .NET Type | Registers needed |             Method            |
|-----------|------------------|-------------------------------|
|  `short`  |         1        | `ToInt16(index)`              |
|  `float`  |         2        | `ToFloat(order, startIndex)`  |
|  `int`    |         2        | `ToInt32(order, startIndex)`  |
|  `uint`   |         2        | `ToUInt32(order, startIndex)` |
|  `long`   |         4        | `ToInt64(order, startIndex)`  |
|  `ulong`  |         4        | `ToUInt64(order, startIndex)` |
|  `double` |         4        | `ToDouble(order, startIndex)` |
Each type also has three ways to decode multiple values at once:

```csharp
// Strict - throws ModbusException with a clear message if data is short
float[] values = registers.ToFloatArray(count: 3, ByteOrder.BigEndian);

// Safe - returns false instead of throwing
if (registers.TryToFloatArray(count: 3, out float[] values, ByteOrder.BigEndian)) { ... }

// Best-effort - decode as many complete values as the data allows
float[] all = registers.ToAllFloats(ByteOrder.BigEndian);
```

## Testing

```bash
dotnet test ModbusDriver.Tests/ModbusDriver.Tests.csproj
```

The test suite uses in-memory fake streams (implementing `IModbusStream`) so tests run
without any real hardware or network connection.

## Contributing

Issues and pull requests are welcome. Please include tests for any new formatter,
stream, or extension method.

## License

This project is licensed under the MIT License - see [LICENSE](./LICENSE) for details.