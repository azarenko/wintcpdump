# WinTcpDump

A C# console application that captures TCP packets and saves them to a pcap file using only standard .NET Framework libraries (no external pcap libraries).

## Features

- Captures TCP packets from the network interface
- Saves packets in standard pcap file format
- Uses only standard .NET Framework libraries (with P/Invoke for Windows socket APIs)
- No external dependencies

## Requirements

- .NET Framework 4.8 or later
- Windows operating system
- **Administrator privileges** (required for raw socket access)

## Building

```bash
dotnet build
```

## Usage

1. **Run as Administrator** - The application requires administrator privileges to access raw sockets.

2. **Run the application:**
   ```bash
   dotnet run
   ```
   
   Or specify an output file:
   ```bash
   dotnet run capture.pcap
   ```

3. **Stop capture** - Press `Ctrl+C` to stop capturing and save the pcap file.

## How It Works

- **PacketCapture.cs**: Uses raw sockets to capture IP packets and filters for TCP protocol (protocol number 6)
- **PcapWriter.cs**: Writes packets in the standard pcap file format (libpcap format)
- **Program.cs**: Main entry point that coordinates packet capture and file writing

## Technical Details

The application uses:
- `System.Net.Sockets.Socket` with `SocketType.Raw` and `ProtocolType.IP` to capture IP packets
- `IOControlCode.ReceiveAll` to receive all packets on the network interface
- Standard pcap file format (magic number 0xA1B2C3D4, version 2.4)
- Ethernet data link type (type 1) in pcap header

## Limitations

- Requires administrator privileges
- Captures only TCP packets (IP protocol 6)
- Captures packets on the local network interface
- Windows only (uses Windows-specific socket APIs)

## Viewing Captured Packets

You can view the captured pcap file using tools like:
- Wireshark
- tcpdump
- Any pcap-compatible packet analyzer
