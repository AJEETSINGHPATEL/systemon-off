using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using BackendServer.Services;

public class DeviceHub : Hub
{
    // Thread-safe dictionaries for device tracking
    // deviceId -> connectionId
    private static readonly ConcurrentDictionary<string, string> devices = new();
    // connectionId -> deviceId
    private static readonly ConcurrentDictionary<string, string> connections = new();
    // deviceId -> MAC address (for Wake-on-LAN)
    private static readonly ConcurrentDictionary<string, string> deviceMacAddresses = new();
    // deviceId -> last known IP address
    private static readonly ConcurrentDictionary<string, string> deviceIpAddresses = new();

    private readonly ILogger<DeviceHub> _logger;
    private readonly WakeOnLanService _wakeOnLanService;

    public DeviceHub(ILogger<DeviceHub> logger, WakeOnLanService wakeOnLanService)
    {
        _logger = logger;
        _wakeOnLanService = wakeOnLanService;
    }

    /// <summary>
    /// Register a device when it connects
    /// </summary>
    public async Task RegisterDevice(string deviceId, string? macAddress = null, string? ipAddress = null)
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            _logger.LogWarning("RegisterDevice called with empty deviceId from connection {ConnectionId}", Context.ConnectionId);
            throw new ArgumentException("Device ID cannot be empty");
        }

        if (deviceId.Length > 100)
        {
            _logger.LogWarning("RegisterDevice called with too long deviceId from connection {ConnectionId}", Context.ConnectionId);
            throw new ArgumentException("Device ID too long (max 100 characters)");
        }

        try
        {
            // Remove old registration if this device was already connected
            if (devices.TryGetValue(deviceId, out var oldConnectionId))
            {
                connections.TryRemove(oldConnectionId, out _);
            }

            devices[deviceId] = Context.ConnectionId;
            connections[Context.ConnectionId] = deviceId;

            // Store MAC address for Wake-on-LAN
            if (!string.IsNullOrWhiteSpace(macAddress))
            {
                deviceMacAddresses[deviceId] = WakeOnLanService.FormatMacAddress(macAddress);
                _logger.LogInformation("Device {DeviceId} registered with MAC: {MacAddress}", deviceId, macAddress);
            }

            // Store IP address
            if (!string.IsNullOrWhiteSpace(ipAddress))
            {
                deviceIpAddresses[deviceId] = ipAddress;
            }

            _logger.LogInformation("Device Connected: {DeviceId}, ConnectionId: {ConnectionId}, MAC: {MacAddress}, IP: {IpAddress}", 
                deviceId, Context.ConnectionId, macAddress ?? "not provided", ipAddress ?? "not provided");

            // Send updated device list to ALL web clients with MAC addresses
            await Clients.All.SendAsync("UpdateDeviceList", GetDeviceListWithStatus());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering device {DeviceId}", deviceId);
            throw;
        }
    }

    /// <summary>
    /// Handle device disconnection
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            if (connections.TryRemove(Context.ConnectionId, out var deviceId))
            {
                devices.TryRemove(deviceId, out _);

                _logger.LogInformation("Device Disconnected: {DeviceId}", deviceId);

                // Update UI with device status (device is offline but we keep MAC for WOL)
                await Clients.All.SendAsync("UpdateDeviceList", GetDeviceListWithStatus());
            }

            if (exception != null)
            {
                _logger.LogWarning(exception, "Connection {ConnectionId} disconnected with error", Context.ConnectionId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling disconnection for connection {ConnectionId}", Context.ConnectionId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Send command to a specific device
    /// </summary>
    public async Task SendCommand(string deviceId, string command)
    {
        // Input validation
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            _logger.LogWarning("SendCommand called with empty deviceId");
            throw new ArgumentException("Device ID cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(command))
        {
            _logger.LogWarning("SendCommand called with empty command");
            throw new ArgumentException("Command cannot be empty");
        }

        // Validate command (only allow known commands for security)
        var allowedCommands = new[] { "shutdown", "restart" };
        if (!allowedCommands.Contains(command.ToLower()))
        {
            _logger.LogWarning("SendCommand called with invalid command: {Command}", command);
            throw new ArgumentException("Invalid command. Only 'shutdown' and 'restart' are allowed.");
        }

        _logger.LogInformation("Command Received: {Command} for device {DeviceId}", command, deviceId);

        try
        {
            if (devices.TryGetValue(deviceId, out var connId))
            {
                _logger.LogInformation("Sending command {Command} to device {DeviceId}", command, deviceId);
                await Clients.Client(connId).SendAsync("ReceiveCommand", command);
                _logger.LogInformation("Command {Command} sent successfully to device {DeviceId}", command, deviceId);
            }
            else
            {
                _logger.LogWarning("Device NOT FOUND: {DeviceId}", deviceId);
                throw new HubException($"Device '{deviceId}' not found or not connected");
            }
        }
        catch (Exception ex) when (ex is not HubException)
        {
            _logger.LogError(ex, "Error sending command {Command} to device {DeviceId}", command, deviceId);
            throw new HubException("Failed to send command to device");
        }
    }

    /// <summary>
    /// Get list of currently connected devices
    /// </summary>
    public Task<List<string>> GetConnectedDevices()
    {
        return Task.FromResult(devices.Keys.ToList());
    }

    /// <summary>
    /// Get list of all known devices with their status and MAC addresses
    /// </summary>
    public Task<List<DeviceInfo>> GetAllDevices()
    {
        return Task.FromResult(GetDeviceListWithStatus());
    }

    /// <summary>
    /// Wake up a device using Wake-on-LAN
    /// </summary>
    public async Task<bool> WakeUpDevice(string deviceId)
    {
        if (string.IsNullOrWhiteSpace(deviceId))
        {
            _logger.LogWarning("WakeUpDevice called with empty deviceId");
            throw new ArgumentException("Device ID cannot be empty");
        }

        _logger.LogInformation("Wake-up requested for device: {DeviceId}", deviceId);

        // Check if device has a MAC address registered
        if (!deviceMacAddresses.TryGetValue(deviceId, out var macAddress) || string.IsNullOrWhiteSpace(macAddress))
        {
            _logger.LogError("Cannot wake up device {DeviceId}: No MAC address registered", deviceId);
            throw new HubException($"Device '{deviceId}' does not have a MAC address registered. Wake-on-LAN is not possible.");
        }

        // Check if device is already online
        if (devices.ContainsKey(deviceId))
        {
            _logger.LogInformation("Device {DeviceId} is already online, no need to wake up", deviceId);
            return true;
        }

        try
        {
            _logger.LogInformation("Sending Wake-on-LAN packet to {DeviceId} with MAC {MacAddress}", deviceId, macAddress);
            
            var success = await _wakeOnLanService.WakeUpWithMultipleAttemptsAsync(macAddress);
            
            if (success)
            {
                _logger.LogInformation("Wake-on-LAN packet sent successfully to {DeviceId}", deviceId);
                
                // Notify clients that wake-up was attempted
                await Clients.All.SendAsync("DeviceWakeUpAttempted", deviceId, macAddress);
            }
            else
            {
                _logger.LogError("Failed to send Wake-on-LAN packet to {DeviceId}", deviceId);
                throw new HubException("Failed to send Wake-on-LAN packet");
            }

            return success;
        }
        catch (Exception ex) when (ex is not HubException)
        {
            _logger.LogError(ex, "Error waking up device {DeviceId}", deviceId);
            throw new HubException("Failed to wake up device");
        }
    }

    /// <summary>
    /// Get device list with online/offline status and MAC addresses
    /// </summary>
    private List<DeviceInfo> GetDeviceListWithStatus()
    {
        var allDeviceIds = new HashSet<string>(devices.Keys);
        allDeviceIds.UnionWith(deviceMacAddresses.Keys);

        return allDeviceIds.Select(id => new DeviceInfo
        {
            DeviceId = id,
            IsOnline = devices.ContainsKey(id),
            MacAddress = deviceMacAddresses.TryGetValue(id, out var mac) ? mac : null,
            IpAddress = deviceIpAddresses.TryGetValue(id, out var ip) ? ip : null
        }).ToList();
    }
}

/// <summary>
/// Device information model
/// </summary>
public class DeviceInfo
{
    public string DeviceId { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public string? MacAddress { get; set; }
    public string? IpAddress { get; set; }
}