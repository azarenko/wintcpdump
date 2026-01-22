using System;
using System.Threading;

namespace WinTcpDump
{
    class Program
    {
        private static PacketCapture _packetCapture;
        private static PcapWriter _pcapWriter;
        private static int _packetCount = 0;
        private static readonly object _lockObject = new object();

        static void Main(string[] args)
        {
            Console.WriteLine("WinTcpDump - TCP Packet Capture Tool");
            Console.WriteLine("=====================================\n");

            // Get output file path
            string outputFile = "capture.pcap";
            if (args.Length > 0)
            {
                outputFile = args[0];
            }
            else
            {
                Console.Write($"Enter output file path (default: {outputFile}): ");
                string input = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(input))
                {
                    outputFile = input;
                }
            }

            Console.WriteLine($"Output file: {outputFile}");
            Console.WriteLine("\nStarting packet capture...");
            Console.WriteLine("Press Ctrl+C to stop capture.\n");

            try
            {
                // Initialize pcap writer
                _pcapWriter = new PcapWriter(outputFile);

                // Initialize packet capture
                _packetCapture = new PacketCapture();
                _packetCapture.PacketCaptured += OnPacketCaptured;

                // Handle Ctrl+C gracefully
                Console.CancelKeyPress += (sender, e) =>
                {
                    e.Cancel = true;
                    Console.WriteLine("\n\nStopping capture...");
                    StopCapture();
                    Environment.Exit(0);
                };

                // Start capture
                _packetCapture.StartCapture();

                Console.WriteLine("Capture started. Waiting for TCP packets...\n");

                // Keep the application running
                while (true)
                {
                    Thread.Sleep(1000);
                    
                    // Display packet count every second
                    lock (_lockObject)
                    {
                        if (_packetCount > 0)
                        {
                            Console.Write($"\rPackets captured: {_packetCount}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"\nError: {ex.Message}");
                Console.Error.WriteLine("\nNote: Raw socket access requires administrator privileges.");
                Console.Error.WriteLine("Please run this application as Administrator.");
                Environment.Exit(1);
            }
        }

        private static void OnPacketCaptured(object sender, PacketCapturedEventArgs e)
        {
            lock (_lockObject)
            {
                _pcapWriter.WritePacket(e.PacketData, e.Timestamp);
                _packetCount++;
            }
        }

        private static void StopCapture()
        {
            _packetCapture?.StopCapture();
            _pcapWriter?.Flush();
            _pcapWriter?.Dispose();

            lock (_lockObject)
            {
                Console.WriteLine($"\n\nCapture stopped. Total packets captured: {_packetCount}");
                Console.WriteLine("Pcap file saved successfully.");
            }
        }
    }
}
