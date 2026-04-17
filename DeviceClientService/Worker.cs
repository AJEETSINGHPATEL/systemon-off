using Microsoft.AspNetCore.SignalR.Client;
using System.Net;
using System.Net.NetworkInformation;

public class Worker : BackgroundService
{
    private HubConnection? _connection;
    private readonly ILogger<Worker> _logger;
    private readonly IConfiguration _configuration;

    public Worker(ILogger<Worker> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Service Starting...");

        // Read hub URL from configuration
        var hubUrl = _configuration.GetValue<string>("HubUrl") ?? "http://localhost:5285/deviceHub";
        var deviceName = _configuration.GetValue<string>("DeviceName") ?? Environment.MachineName;
        
        // Get MAC address and IP for Wake-on-LAN
        var macAddress = GetMacAddress();
        var ipAddress = GetLocalIPAddress();
        
        _logger.LogInformation("Device: {DeviceName}, MAC: {MacAddress}, IP: {IpAddress}", 
            deviceName, macAddress ?? "unknown", ipAddress ?? "unknown");

        _connection = new HubConnectionBuilder()
            .WithUrl(hubUrl)
            .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30) })
            .Build();

        // Handle incoming commands
        _connection.On<string>("ReceiveCommand", (command) =>
        {
            _logger.LogInformation("Received command: {Command}", command);
            ExecuteSystemCommand(command);
        });

        // Handle reconnection events
        _connection.Reconnecting += error =>
        {
            _logger.LogWarning("Connection lost. Reconnecting... Error: {Error}", error?.Message);
            return Task.CompletedTask;
        };

        _connection.Reconnected += connectionId =>
        {
            _logger.LogInformation("Reconnected with connection ID: {ConnectionId}", connectionId);
            // Re-register device after reconnection
            return RegisterDeviceAsync(deviceName, stoppingToken);
        };

        _connection.Closed += error =>
        {
            _logger.LogError("Connection closed. Error: {Error}", error?.Message);
            return Task.CompletedTask;
        };

        // Main connection loop with reconnection support
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (_connection.State == HubConnectionState.Disconnected)
                {
                    _logger.LogInformation("Connecting to {HubUrl}...", hubUrl);
                    await _connection.StartAsync(stoppingToken);
                    _logger.LogInformation("Connected successfully");

                    await RegisterDeviceAsync(deviceName, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Connection failed. Retrying in 10 seconds...");
            }

            // Wait before checking connection state again
            try
            {
                await Task.Delay(10000, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        // Cleanup
        if (_connection != null)
        {
            await _connection.DisposeAsync();
        }

        _logger.LogInformation("Service stopped");
    }

    private async Task RegisterDeviceAsync(string deviceName, CancellationToken stoppingToken)
    {
        try
        {
            if (_connection?.State == HubConnectionState.Connected)
            {
                var macAddress = GetMacAddress();
                var ipAddress = GetLocalIPAddress();
                
                await _connection.InvokeAsync("RegisterDevice", deviceName, macAddress, ipAddress, stoppingToken);
                _logger.LogInformation("Registered device: {DeviceName} with MAC: {MacAddress}, IP: {IpAddress}", 
                    deviceName, macAddress ?? "not available", ipAddress ?? "not available");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register device");
        }
    }

    /// <summary>
    /// Gets the primary MAC address of the machine
    /// </summary>
    private string? GetMacAddress()
    {
        try
        {
            var nics = NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up 
                    && nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .OrderByDescending(nic => nic.GetIPProperties().GatewayAddresses.Count)
                .ToList();

            foreach (var nic in nics)
            {
                var mac = nic.GetPhysicalAddress().ToString();
                if (!string.IsNullOrEmpty(mac) && mac != "000000000000")
                {
                    // Format as XX:XX:XX:XX:XX:XX
                    return string.Join(":", Enumerable.Range(0, 6)
                        .Select(i => mac.Substring(i * 2, 2)));
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get MAC address");
        }
        return null;
    }

    /// <summary>
    /// Gets the local IP address
    /// </summary>
    private string? GetLocalIPAddress()
    {
        try
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ip = host.AddressList
                .FirstOrDefault(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork 
                    && !IPAddress.IsLoopback(ip));
            return ip?.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get IP address");
        }
        return null;
    }

    private void ExecuteSystemCommand(string command)
    {
        try
        {
            switch (command.ToLower())
            {
                case "shutdown":
                    _logger.LogInformation("Executing SHUTDOWN command");
                    // Use ProcessStartInfo for better control
                    var shutdownInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "shutdown",
                        Arguments = "/s /t 0 /f",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    System.Diagnostics.Process.Start(shutdownInfo);
                    break;

                case "restart":
                    _logger.LogInformation("Executing RESTART command");
                    var restartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "shutdown",
                        Arguments = "/r /t 0 /f",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    System.Diagnostics.Process.Start(restartInfo);
                    break;

                default:
                    _logger.LogWarning("Unknown command received: {Command}", command);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing command: {Command}", command);
        }
    }
}