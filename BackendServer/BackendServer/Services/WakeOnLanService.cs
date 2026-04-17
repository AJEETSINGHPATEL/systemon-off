using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace BackendServer.Services
{
    /// <summary>
    /// Service for sending Wake-on-LAN magic packets
    /// </summary>
    public class WakeOnLanService
    {
        private readonly ILogger<WakeOnLanService> _logger;

        public WakeOnLanService(ILogger<WakeOnLanService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Sends a Wake-on-LAN magic packet to the specified MAC address
        /// </summary>
        /// <param name="macAddress">MAC address in format "00:11:22:33:44:55" or "00-11-22-33-44-55"</param>
        /// <param name="broadcastAddress">Broadcast address (default: 255.255.255.255)</param>
        /// <param name="port">UDP port (default: 9)</param>
        /// <returns>True if packet was sent successfully</returns>
        public async Task<bool> WakeUpAsync(string macAddress, string? broadcastAddress = null, int port = 9)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(macAddress))
                {
                    _logger.LogError("MAC address is empty");
                    return false;
                }

                // Parse MAC address
                var macBytes = ParseMacAddress(macAddress);
                if (macBytes == null)
                {
                    _logger.LogError("Invalid MAC address format: {MacAddress}", macAddress);
                    return false;
                }

                // Create magic packet: 6 bytes of 0xFF followed by 16 repetitions of MAC address
                var magicPacket = new byte[6 + 16 * 6];
                
                // First 6 bytes: 0xFF
                for (int i = 0; i < 6; i++)
                {
                    magicPacket[i] = 0xFF;
                }

                // 16 repetitions of MAC address
                for (int i = 0; i < 16; i++)
                {
                    Buffer.BlockCopy(macBytes, 0, magicPacket, 6 + i * 6, 6);
                }

                // Determine broadcast address
                var broadcast = IPAddress.Parse(broadcastAddress ?? "255.255.255.255");

                // Send magic packet via UDP
                using var client = new UdpClient();
                client.EnableBroadcast = true;
                
                _logger.LogInformation("Sending Wake-on-LAN packet to {MacAddress} via {Broadcast}:{Port}", 
                    macAddress, broadcast, port);

                await client.SendAsync(magicPacket, magicPacket.Length, new IPEndPoint(broadcast, port));
                
                // Also try port 7 (alternative WOL port)
                if (port != 7)
                {
                    await client.SendAsync(magicPacket, magicPacket.Length, new IPEndPoint(broadcast, 7));
                }

                _logger.LogInformation("Wake-on-LAN packet sent successfully to {MacAddress}", macAddress);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send Wake-on-LAN packet to {MacAddress}", macAddress);
                return false;
            }
        }

        /// <summary>
        /// Sends Wake-on-LAN packet to multiple broadcast addresses for better coverage
        /// </summary>
        public async Task<bool> WakeUpWithMultipleAttemptsAsync(string macAddress)
        {
            var broadcastAddresses = new[]
            {
                "255.255.255.255",
                GetLocalBroadcastAddress()
            }.Where(a => !string.IsNullOrEmpty(a)).Distinct();

            var ports = new[] { 9, 7, 0 };
            bool success = false;

            foreach (var broadcast in broadcastAddresses)
            {
                foreach (var port in ports)
                {
                    if (await WakeUpAsync(macAddress, broadcast, port))
                    {
                        success = true;
                    }
                }
            }

            return success;
        }

        /// <summary>
        /// Validates MAC address format
        /// </summary>
        public static bool IsValidMacAddress(string macAddress)
        {
            return ParseMacAddress(macAddress) != null;
        }

        /// <summary>
        /// Parses MAC address string to byte array
        /// Supports formats: "00:11:22:33:44:55", "00-11-22-33-44-55", "001122334455"
        /// </summary>
        private static byte[]? ParseMacAddress(string macAddress)
        {
            try
            {
                // Remove separators
                var cleanMac = macAddress.Replace(":", "").Replace("-", "").Replace(".", "").ToUpper();
                
                if (cleanMac.Length != 12)
                {
                    return null;
                }

                var bytes = new byte[6];
                for (int i = 0; i < 6; i++)
                {
                    bytes[i] = Convert.ToByte(cleanMac.Substring(i * 2, 2), 16);
                }

                return bytes;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets the local network broadcast address
        /// </summary>
        private static string? GetLocalBroadcastAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                var localIp = host.AddressList
                    .FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork 
                        && !IPAddress.IsLoopback(ip));
                
                if (localIp == null) return null;

                // Assume /24 subnet and create broadcast address
                var ipBytes = localIp.GetAddressBytes();
                ipBytes[3] = 255;
                return new IPAddress(ipBytes).ToString();
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Formats MAC address to standard format (00:11:22:33:44:55)
        /// </summary>
        public static string FormatMacAddress(string macAddress)
        {
            var cleanMac = macAddress.Replace(":", "").Replace("-", "").Replace(".", "").ToLower();
            if (cleanMac.Length != 12) return macAddress;
            
            return string.Join(":", Enumerable.Range(0, 6)
                .Select(i => cleanMac.Substring(i * 2, 2)));
        }
    }
}
