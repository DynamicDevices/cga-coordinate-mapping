#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;

namespace InstDotNet;

/// <summary>
/// Provides unique hardware identifiers for the system.
/// </summary>
public static class HardwareId
{
    private static string? _cachedId;

    /// <summary>
    /// Gets a unique hardware identifier for this system.
    /// Tries multiple methods in order of preference:
    /// 1. Systemd machine-id (/etc/machine-id)
    /// 2. DMI system UUID (/sys/class/dmi/id/product_uuid)
    /// 3. First MAC address
    /// 4. Hostname (fallback)
    /// </summary>
    public static string GetUniqueId()
    {
        if (_cachedId != null)
        {
            return _cachedId;
        }

        // Try systemd machine-id (most reliable on modern Linux)
        try
        {
            var machineIdPath = "/etc/machine-id";
            if (File.Exists(machineIdPath))
            {
                var machineId = File.ReadAllText(machineIdPath).Trim();
                if (!string.IsNullOrEmpty(machineId))
                {
                    _cachedId = $"machine-{machineId}";
                    return _cachedId;
                }
            }
        }
        catch (Exception)
        {
            // Ignore errors
        }

        // Try DMI system UUID
        try
        {
            var dmiUuidPath = "/sys/class/dmi/id/product_uuid";
            if (File.Exists(dmiUuidPath))
            {
                var dmiUuid = File.ReadAllText(dmiUuidPath).Trim();
                if (!string.IsNullOrEmpty(dmiUuid))
                {
                    _cachedId = $"dmi-{dmiUuid}";
                    return _cachedId;
                }
            }
        }
        catch (Exception)
        {
            // Ignore errors
        }

        // Try first MAC address
        try
        {
            var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                .Where(nic => nic.OperationalStatus == OperationalStatus.Up &&
                              nic.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .OrderBy(nic => nic.NetworkInterfaceType)
                .ToList();

            foreach (var nic in networkInterfaces)
            {
                var macAddress = nic.GetPhysicalAddress().ToString();
                if (!string.IsNullOrEmpty(macAddress) && macAddress != "000000000000")
                {
                    _cachedId = $"mac-{macAddress}";
                    return _cachedId;
                }
            }
        }
        catch (Exception)
        {
            // Ignore errors
        }

        // Fallback to hostname
        try
        {
            var hostname = Environment.MachineName;
            if (!string.IsNullOrEmpty(hostname))
            {
                _cachedId = $"host-{hostname}";
                return _cachedId;
            }
        }
        catch (Exception)
        {
            // Ignore errors
        }

        // Last resort: generate a random ID (not ideal, but better than nothing)
        _cachedId = $"random-{Guid.NewGuid():N}";
        return _cachedId;
    }

    /// <summary>
    /// Gets a sanitized version of the hardware ID suitable for use as an MQTT client ID.
    /// MQTT client IDs must be alphanumeric and can contain hyphens and underscores.
    /// </summary>
    /// <param name="prefix">Optional prefix to prepend to the hardware ID (e.g., "UwbManager")</param>
    /// <returns>A sanitized MQTT client ID string, truncated to 128 characters if necessary</returns>
    public static string GetMqttClientId(string? prefix = null)
    {
        var hardwareId = GetUniqueId();
        var clientId = string.IsNullOrEmpty(prefix) ? hardwareId : $"{prefix}-{hardwareId}";

        // Sanitize for MQTT: only alphanumeric, hyphens, and underscores allowed
        // Replace any invalid characters with hyphens
        var sanitized = new System.Text.StringBuilder();
        foreach (var c in clientId)
        {
            if (char.IsLetterOrDigit(c) || c == '-' || c == '_')
            {
                sanitized.Append(c);
            }
            else
            {
                sanitized.Append('-');
            }
        }

        // Ensure it's not too long (MQTT spec recommends max 23 characters for client IDs)
        // But we'll allow up to 128 characters as per MQTT 3.1.1 spec
        var result = sanitized.ToString();
        if (result.Length > 128)
        {
            result = result.Substring(0, 128);
        }

        return result;
    }
}

