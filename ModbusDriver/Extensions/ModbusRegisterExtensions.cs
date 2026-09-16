using ModbusDriver.Core;
using System;

namespace ModbusDriver.Extensions
{
    /// <summary>
    /// Pure conversion helpers for decoding raw Modbus registers into common data types.
    /// These work on any ushort[] regardless of which Read* function produced it -
    /// ReadHoldingRegisters, ReadInputRegisters, or even a manually-built array.
    /// </summary>
    public static class ModbusRegisterExtensions
    {
        private const int Bytes32Bit = 4; // float, int, uint -> 2 registers
        private const int Bytes64Bit = 8; // long, ulong, double -> 4 registers

        // =========================================================
        // SINGLE VALUE DECODING
        // =========================================================

        public static float ToFloat(this ushort[] registers, ByteOrder order = ByteOrder.BigEndian, int startIndex = 0)
            => BitConverter.ToSingle(ToOrderedBytes(registers, startIndex, Bytes32Bit, order, nameof(Single)), 0);

        public static int ToInt32(this ushort[] registers, ByteOrder order = ByteOrder.BigEndian, int startIndex = 0)
            => BitConverter.ToInt32(ToOrderedBytes(registers, startIndex, Bytes32Bit, order, nameof(Int32)), 0);

        public static uint ToUInt32(this ushort[] registers, ByteOrder order = ByteOrder.BigEndian, int startIndex = 0)
            => BitConverter.ToUInt32(ToOrderedBytes(registers, startIndex, Bytes32Bit, order, nameof(UInt32)), 0);

        public static long ToInt64(this ushort[] registers, ByteOrder order = ByteOrder.BigEndian, int startIndex = 0)
            => BitConverter.ToInt64(ToOrderedBytes(registers, startIndex, Bytes64Bit, order, nameof(Int64)), 0);

        public static ulong ToUInt64(this ushort[] registers, ByteOrder order = ByteOrder.BigEndian, int startIndex = 0)
            => BitConverter.ToUInt64(ToOrderedBytes(registers, startIndex, Bytes64Bit, order, nameof(UInt64)), 0);

        public static double ToDouble(this ushort[] registers, ByteOrder order = ByteOrder.BigEndian, int startIndex = 0)
            => BitConverter.ToDouble(ToOrderedBytes(registers, startIndex, Bytes64Bit, order, nameof(Double)), 0);

        public static short ToInt16(this ushort[] registers, int index = 0)
        {
            if (registers == null) throw new ArgumentNullException(nameof(registers));
            if (index < 0 || index >= registers.Length)
                throw new ModbusException($"Cannot decode Int16: index {index} is out of range for an array of {registers.Length} register(s).");
            return (short)registers[index];
        }

        // =========================================================
        // STRICT BATCH DECODING - throws ModbusException if data is short
        // =========================================================

        public static float[] ToFloatArray(this ushort[] registers, int count, ByteOrder order = ByteOrder.BigEndian, int startIndex = 0)
            => DecodeArray(registers, count, Bytes32Bit, startIndex, order, b => BitConverter.ToSingle(b, 0), nameof(Single));

        public static int[] ToInt32Array(this ushort[] registers, int count, ByteOrder order = ByteOrder.BigEndian, int startIndex = 0)
            => DecodeArray(registers, count, Bytes32Bit, startIndex, order, b => BitConverter.ToInt32(b, 0), nameof(Int32));

        public static uint[] ToUInt32Array(this ushort[] registers, int count, ByteOrder order = ByteOrder.BigEndian, int startIndex = 0)
            => DecodeArray(registers, count, Bytes32Bit, startIndex, order, b => BitConverter.ToUInt32(b, 0), nameof(UInt32));

        public static long[] ToInt64Array(this ushort[] registers, int count, ByteOrder order = ByteOrder.BigEndian, int startIndex = 0)
            => DecodeArray(registers, count, Bytes64Bit, startIndex, order, b => BitConverter.ToInt64(b, 0), nameof(Int64));

        public static ulong[] ToUInt64Array(this ushort[] registers, int count, ByteOrder order = ByteOrder.BigEndian, int startIndex = 0)
            => DecodeArray(registers, count, Bytes64Bit, startIndex, order, b => BitConverter.ToUInt64(b, 0), nameof(UInt64));

        public static double[] ToDoubleArray(this ushort[] registers, int count, ByteOrder order = ByteOrder.BigEndian, int startIndex = 0)
            => DecodeArray(registers, count, Bytes64Bit, startIndex, order, b => BitConverter.ToDouble(b, 0), nameof(Double));

        // =========================================================
        // SAFE BATCH DECODING - returns false instead of throwing
        // =========================================================

        public static bool TryToFloatArray(this ushort[] registers, int count, out float[] values, ByteOrder order = ByteOrder.BigEndian, int startIndex = 0)
            => TryDecodeArray(registers, count, Bytes32Bit, startIndex, order, b => BitConverter.ToSingle(b, 0), out values);

        public static bool TryToInt32Array(this ushort[] registers, int count, out int[] values, ByteOrder order = ByteOrder.BigEndian, int startIndex = 0)
            => TryDecodeArray(registers, count, Bytes32Bit, startIndex, order, b => BitConverter.ToInt32(b, 0), out values);

        public static bool TryToUInt32Array(this ushort[] registers, int count, out uint[] values, ByteOrder order = ByteOrder.BigEndian, int startIndex = 0)
            => TryDecodeArray(registers, count, Bytes32Bit, startIndex, order, b => BitConverter.ToUInt32(b, 0), out values);

        public static bool TryToInt64Array(this ushort[] registers, int count, out long[] values, ByteOrder order = ByteOrder.BigEndian, int startIndex = 0)
            => TryDecodeArray(registers, count, Bytes64Bit, startIndex, order, b => BitConverter.ToInt64(b, 0), out values);

        public static bool TryToUInt64Array(this ushort[] registers, int count, out ulong[] values, ByteOrder order = ByteOrder.BigEndian, int startIndex = 0)
            => TryDecodeArray(registers, count, Bytes64Bit, startIndex, order, b => BitConverter.ToUInt64(b, 0), out values);

        public static bool TryToDoubleArray(this ushort[] registers, int count, out double[] values, ByteOrder order = ByteOrder.BigEndian, int startIndex = 0)
            => TryDecodeArray(registers, count, Bytes64Bit, startIndex, order, b => BitConverter.ToDouble(b, 0), out values);

        // =========================================================
        // BEST-EFFORT DECODING - decode as many complete values as the data allows,
        // silently ignoring a trailing register that isn't enough for one more value.
        // Use this when you don't know (or don't care) how many values are in the response.
        // =========================================================

        public static float[] ToAllFloats(this ushort[] registers, ByteOrder order = ByteOrder.BigEndian, int startIndex = 0)
            => DecodeAll(registers, Bytes32Bit, startIndex, order, b => BitConverter.ToSingle(b, 0));

        public static int[] ToAllInt32s(this ushort[] registers, ByteOrder order = ByteOrder.BigEndian, int startIndex = 0)
            => DecodeAll(registers, Bytes32Bit, startIndex, order, b => BitConverter.ToInt32(b, 0));

        public static uint[] ToAllUInt32s(this ushort[] registers, ByteOrder order = ByteOrder.BigEndian, int startIndex = 0)
            => DecodeAll(registers, Bytes32Bit, startIndex, order, b => BitConverter.ToUInt32(b, 0));

        public static long[] ToAllInt64s(this ushort[] registers, ByteOrder order = ByteOrder.BigEndian, int startIndex = 0)
            => DecodeAll(registers, Bytes64Bit, startIndex, order, b => BitConverter.ToInt64(b, 0));

        public static ulong[] ToAllUInt64s(this ushort[] registers, ByteOrder order = ByteOrder.BigEndian, int startIndex = 0)
            => DecodeAll(registers, Bytes64Bit, startIndex, order, b => BitConverter.ToUInt64(b, 0));

        public static double[] ToAllDoubles(this ushort[] registers, ByteOrder order = ByteOrder.BigEndian, int startIndex = 0)
            => DecodeAll(registers, Bytes64Bit, startIndex, order, b => BitConverter.ToDouble(b, 0));

        // =========================================================
        // SHARED CORE - the only place that touches indices/byte order/validation
        // =========================================================

        private static T[] DecodeArray<T>(ushort[] registers, int count, int byteSize, int startIndex,
            ByteOrder order, Func<byte[], T> convert, string typeName)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count), "count cannot be negative.");

            int regsPerValue = byteSize / 2;
            var result = new T[count];
            for (int i = 0; i < count; i++)
            {
                byte[] ordered = ToOrderedBytes(registers, startIndex + i * regsPerValue, byteSize, order, typeName);
                result[i] = convert(ordered);
            }
            return result;
        }

        private static bool TryDecodeArray<T>(ushort[] registers, int count, int byteSize, int startIndex,
            ByteOrder order, Func<byte[], T> convert, out T[] result)
        {
            result = null;

            if (registers == null || count < 0 || startIndex < 0)
                return false;

            int regsPerValue = byteSize / 2;
            int registersNeeded = count * regsPerValue;

            if (registers.Length - startIndex < registersNeeded)
                return false; // not enough data - caller decides what to do, no exception thrown

            result = new T[count];
            for (int i = 0; i < count; i++)
            {
                byte[] ordered = ToOrderedBytes(registers, startIndex + i * regsPerValue, byteSize, order, typeName: null);
                result[i] = convert(ordered);
            }
            return true;
        }

        private static T[] DecodeAll<T>(ushort[] registers, int byteSize, int startIndex, ByteOrder order, Func<byte[], T> convert)
        {
            if (registers == null) throw new ArgumentNullException(nameof(registers));
            if (startIndex < 0 || startIndex > registers.Length)
                throw new ArgumentOutOfRangeException(nameof(startIndex));

            int regsPerValue = byteSize / 2;
            int available = registers.Length - startIndex;
            int count = available / regsPerValue; // floor division - trailing partial register(s) are ignored on purpose

            var result = new T[count];
            for (int i = 0; i < count; i++)
            {
                byte[] ordered = ToOrderedBytes(registers, startIndex + i * regsPerValue, byteSize, order, typeName: null);
                result[i] = convert(ordered);
            }
            return result;
        }

        /// <summary>
        /// Builds a properly byte-ordered buffer of <paramref name="byteSize"/> bytes from
        /// <paramref name="byteSize"/>/2 registers starting at <paramref name="startIndex"/>.
        /// Throws ModbusException with a precise, actionable message when the array is too short -
        /// this is the single choke point that guards every 32-bit/64-bit conversion in this class.
        /// </summary>
        private static byte[] ToOrderedBytes(ushort[] registers, int startIndex, int byteSize, ByteOrder order, string typeName)
        {
            if (registers == null)
                throw new ArgumentNullException(nameof(registers));

            int regCount = byteSize / 2;

            if (startIndex < 0 || startIndex + regCount > registers.Length)
            {
                int available = Math.Max(0, registers.Length - startIndex);
                string what = typeName ?? "value";
                throw new ModbusException(
                    $"Cannot decode {what}: need {regCount} register(s) starting at index {startIndex}, " +
                    $"but only {available} register(s) are available (array length {registers.Length}). " +
                    "The device likely returned fewer registers than expected - check the requested read " +
                    "quantity, or use TryTo...Array / ToAll... if partial data is expected.");
            }

            byte[] wireBytes = new byte[byteSize];
            for (int i = 0; i < regCount; i++)
            {
                ushort reg = registers[startIndex + i];
                wireBytes[i * 2] = (byte)(reg >> 8);
                wireBytes[i * 2 + 1] = (byte)(reg & 0xFF);
            }

            byte[] ordered = new byte[wireBytes.Length];

            switch (order)
            {
                case ByteOrder.BigEndian:
                    Array.Copy(wireBytes, ordered, wireBytes.Length);
                    break;

                case ByteOrder.BigEndianInvert: // word swap, e.g. CDAB for a 2-register value
                    ReorderInWordPairs(wireBytes, ordered, swapWords: true, swapBytesWithinWord: false);
                    break;

                case ByteOrder.LittleEndianInvert: // byte swap within each word, e.g. BADC
                    ReorderInWordPairs(wireBytes, ordered, swapWords: false, swapBytesWithinWord: true);
                    break;

                case ByteOrder.LittleEndian: // full reverse, e.g. DCBA
                    Array.Copy(wireBytes, ordered, wireBytes.Length);
                    Array.Reverse(ordered);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(order), "Unsupported industrial byte ordering specified.");
            }

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(ordered);
            }

            return ordered;
        }

        private static void ReorderInWordPairs(byte[] wireBytes, byte[] ordered, bool swapWords, bool swapBytesWithinWord)
        {
            int wordCount = wireBytes.Length / 2;
            for (int w = 0; w < wordCount; w++)
            {
                int srcWord = swapWords ? (wordCount - 1 - w) : w;
                byte hi = wireBytes[srcWord * 2];
                byte lo = wireBytes[srcWord * 2 + 1];

                if (swapBytesWithinWord)
                {
                    ordered[w * 2] = lo;
                    ordered[w * 2 + 1] = hi;
                }
                else
                {
                    ordered[w * 2] = hi;
                    ordered[w * 2 + 1] = lo;
                }
            }
        }
    }
}