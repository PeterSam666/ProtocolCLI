using System;

namespace ModbusDriver.Core
{
    /// <summary>
    /// Specifies the byte and word ordering formats exactly as displayed in the industry-standard "Modbus Poll" software.
    /// This allows automation engineers to match display formats directly with their software configuration.
    /// </summary>
    public enum ByteOrder
    {
        /// <summary>
        /// Big-Endian format (High byte first, High word first). Layout: [A, B, C, D].
        /// Also known as standard network byte order.
        /// </summary>
        BigEndian,

        /// <summary>
        /// Little-Endian format (Low byte first, Low word first). Layout: [D, C, B, A].
        /// Native layout for Windows PCs and Intel architecture.
        /// </summary>
        LittleEndian,

        /// <summary>
        /// Big-Endian Invert format (Word-Swapped Big-Endian). Layout: [C, D, A, B].
        /// Inverts the 16-bit register word positions. The most popular format for industrial power meters.
        /// </summary>
        BigEndianInvert,

        /// <summary>
        /// Little-Endian Invert format (Byte-Swapped Big-Endian). Layout: [B, A, D, C].
        /// Inverts the internal 8-bit byte positions within each word block.
        /// </summary>
        LittleEndianInvert
    }
}