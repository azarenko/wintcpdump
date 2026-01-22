using System;
using System.IO;

namespace WinTcpDump
{
    /// <summary>
    /// Writes packets to a pcap file format
    /// </summary>
    public class PcapWriter : IDisposable
    {
        private readonly BinaryWriter _writer;
        private readonly bool _disposeWriter;
        private bool _headerWritten;

        // Pcap file header constants
        private const uint PCAP_MAGIC = 0xA1B2C3D4;
        private const ushort PCAP_VERSION_MAJOR = 2;
        private const ushort PCAP_VERSION_MINOR = 4;
        private const int PCAP_SNAPLEN = 65535;
        private const uint PCAP_NETWORK_RAW_IP = 101; // Raw IP (no link layer)

        public PcapWriter(string filePath)
        {
            var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            _writer = new BinaryWriter(fileStream);
            _disposeWriter = true;
            WriteFileHeader();
        }

        public PcapWriter(BinaryWriter writer)
        {
            _writer = writer ?? throw new ArgumentNullException(nameof(writer));
            _disposeWriter = false;
            WriteFileHeader();
        }

        private void WriteFileHeader()
        {
            if (_headerWritten)
                return;

            // Pcap file header (24 bytes)
            _writer.Write(PCAP_MAGIC);                    // Magic number
            _writer.Write(PCAP_VERSION_MAJOR);            // Version major
            _writer.Write(PCAP_VERSION_MINOR);            // Version minor
            _writer.Write((int)0);                        // Timezone offset (GMT)
            _writer.Write((int)0);                        // Timestamp accuracy
            _writer.Write(PCAP_SNAPLEN);                   // Max packet length
            _writer.Write(PCAP_NETWORK_RAW_IP);             // Data link type (Raw IP)

            _headerWritten = true;
        }

        public void WritePacket(byte[] packetData, DateTime timestamp)
        {
            if (packetData == null || packetData.Length == 0)
                return;

            // Calculate timestamp (seconds and microseconds since epoch)
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var timeSpan = timestamp.ToUniversalTime() - epoch;
            var seconds = (uint)timeSpan.TotalSeconds;
            var microseconds = (uint)((timeSpan.TotalMilliseconds - (seconds * 1000.0)) * 1000.0);

            // Pcap packet header (16 bytes)
            _writer.Write(seconds);                       // Timestamp seconds
            _writer.Write(microseconds);                   // Timestamp microseconds
            _writer.Write((uint)packetData.Length);       // Captured packet length
            _writer.Write((uint)packetData.Length);       // Original packet length

            // Packet data
            _writer.Write(packetData);
        }

        public void Flush()
        {
            _writer?.Flush();
        }

        public void Dispose()
        {
            if (_disposeWriter)
            {
                _writer?.Flush();
                _writer?.Dispose();
            }
        }
    }
}
