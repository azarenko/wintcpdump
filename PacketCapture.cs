using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace WinTcpDump
{
    /// <summary>
    /// Captures TCP packets using raw sockets on Windows
    /// </summary>
    public class PacketCapture : IDisposable
    {
        private Socket _rawSocket;
        private bool _isCapturing;
        private readonly int _bufferSize = 65535;

        public event EventHandler<PacketCapturedEventArgs> PacketCaptured;

        public void StartCapture(string interfaceName = null)
        {
            if (_isCapturing)
                return;

            try
            {
                // Create raw socket for IP protocol
                _rawSocket = new Socket(AddressFamily.InterNetwork, SocketType.Raw, ProtocolType.IP);
                
                // Try to bind to a specific local IP address first
                IPAddress localIP = GetLocalIPAddress();
                IPEndPoint localEndPoint;
                
                // Prefer binding to a specific IP, fallback to Any
                if (localIP != IPAddress.Any && localIP != IPAddress.Loopback)
                {
                    localEndPoint = new IPEndPoint(localIP, 0);
                }
                else
                {
                    localEndPoint = new IPEndPoint(IPAddress.Any, 0);
                }
                
                _rawSocket.Bind(localEndPoint);
                
                // Note: On Windows, raw sockets automatically include IP headers
                
                // Set socket to receive all packets (promiscuous mode equivalent)
                // This must be done after binding
                // Note: This may fail on some Windows systems, but we continue anyway
                try
                {
                    int receiveAll = 1;
                    byte[] inValue = BitConverter.GetBytes(receiveAll);
                    byte[] outValue = new byte[4];
                    _rawSocket.IOControl(IOControlCode.ReceiveAll, inValue, outValue);
                }
                catch (Exception ioEx)
                {
                    // IOControl may fail on some systems, but we can still capture packets
                    // destined for this machine
                    Console.WriteLine($"Warning: Could not enable promiscuous mode: {ioEx.Message}");
                    Console.WriteLine("Will capture packets destined for this machine only.");
                }

                _isCapturing = true;

                // Start receiving packets asynchronously
                Task.Run(() => ReceivePackets());
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to start packet capture: {ex.Message}\n\n" +
                    "Possible causes:\n" +
                    "1. Not running as Administrator\n" +
                    "2. Raw sockets may be restricted on this Windows version\n" +
                    "3. Network adapter may not support raw socket capture", ex);
            }
        }

        private IPAddress GetLocalIPAddress()
        {
            // Get the first available IPv4 address
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            return IPAddress.Any;
        }

        private void ReceivePackets()
        {
            byte[] buffer = new byte[_bufferSize];
            EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

            while (_isCapturing && _rawSocket != null)
            {
                try
                {
                    int bytesReceived = _rawSocket.ReceiveFrom(buffer, 0, _bufferSize, SocketFlags.None, ref remoteEndPoint);
                    
                    if (bytesReceived > 0)
                    {
                        // Parse IP header to check if it's TCP
                        if (bytesReceived >= 20) // Minimum IP header size
                        {
                            byte protocol = buffer[9]; // Protocol field in IP header
                            
                            if (protocol == 6) // TCP protocol number
                            {
                                byte[] packetData = new byte[bytesReceived];
                                Array.Copy(buffer, 0, packetData, 0, bytesReceived);
                                
                                OnPacketCaptured(packetData, DateTime.Now);
                            }
                        }
                    }
                }
                catch (SocketException)
                {
                    // Socket closed or error - stop capturing
                    if (_isCapturing)
                        break;
                }
                catch (Exception ex)
                {
                    // Log or handle other exceptions
                    Console.Error.WriteLine($"Error receiving packet: {ex.Message}");
                }
            }
        }

        protected virtual void OnPacketCaptured(byte[] packetData, DateTime timestamp)
        {
            PacketCaptured?.Invoke(this, new PacketCapturedEventArgs(packetData, timestamp));
        }

        public void StopCapture()
        {
            _isCapturing = false;
            _rawSocket?.Close();
            _rawSocket?.Dispose();
            _rawSocket = null;
        }

        public void Dispose()
        {
            StopCapture();
        }
    }

    public class PacketCapturedEventArgs : EventArgs
    {
        public byte[] PacketData { get; }
        public DateTime Timestamp { get; }

        public PacketCapturedEventArgs(byte[] packetData, DateTime timestamp)
        {
            PacketData = packetData;
            Timestamp = timestamp;
        }
    }
}
