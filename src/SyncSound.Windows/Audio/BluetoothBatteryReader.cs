using System.Runtime.InteropServices;

namespace SyncSound.Windows.Audio;

/// <summary>
/// Queries real Windows PnP Bluetooth battery percentages via CfgMgr32.
/// Never fabricates values; returns null when Windows does not expose battery data.
/// </summary>
internal static class BluetoothBatteryReader
{
    private const uint CR_SUCCESS = 0x00000000;
    private const uint CM_GETIDLIST_FILTER_PRESENT = 0x00000100;
    private const uint CM_LOCATE_DEVNODE_NORMAL = 0x00000000;
    private const uint DEVPROP_TYPE_BYTE = 0x00000003;
    private const uint DEVPROP_TYPE_UINT32 = 0x00000007;
    private const uint DEVPROP_TYPE_STRING = 0x00000012;

    [StructLayout(LayoutKind.Sequential)]
    private struct DEVPROPKEY
    {
        public Guid fmtid;
        public uint pid;
    }

    private static readonly DEVPROPKEY DEVPKEY_Device_FriendlyName = new()
    {
        fmtid = new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"),
        pid = 14
    };

    private static readonly DEVPROPKEY DEVPKEY_Device_BatteryLevel = new()
    {
        fmtid = new Guid("104EA319-6EE2-4701-BD47-8DDBF425BBE5"),
        pid = 2
    };

    public static IReadOnlyDictionary<string, int> GetBatteryLevelsByDeviceName()
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        try
        {
            if (CM_Get_Device_ID_List_SizeW(out var listSize, null, CM_GETIDLIST_FILTER_PRESENT) != CR_SUCCESS || listSize == 0)
            {
                return result;
            }

            var buffer = new char[listSize];
            if (CM_Get_Device_ID_ListW(null, buffer, listSize, CM_GETIDLIST_FILTER_PRESENT) != CR_SUCCESS)
            {
                return result;
            }

            var allIds = new string(buffer).Split('\0', StringSplitOptions.RemoveEmptyEntries);
            foreach (var instanceId in allIds)
            {
                if (!instanceId.StartsWith("BTHENUM\\", StringComparison.OrdinalIgnoreCase) &&
                    !instanceId.StartsWith("BTHLE\\", StringComparison.OrdinalIgnoreCase) &&
                    !instanceId.StartsWith("BTHLEDevice\\", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (CM_Locate_DevNodeW(out var devInst, instanceId, CM_LOCATE_DEVNODE_NORMAL) != CR_SUCCESS)
                {
                    continue;
                }

                var battery = ReadBatteryLevel(devInst);
                if (battery is null)
                {
                    continue;
                }

                var friendlyName = ReadStringProperty(devInst, DEVPKEY_Device_FriendlyName);
                if (!string.IsNullOrWhiteSpace(friendlyName))
                {
                    result[friendlyName.Trim()] = battery.Value;
                }
            }
        }
        catch
        {
            // Battery reporting is strictly optional; never fail device enumeration.
        }

        return result;
    }

    public static int? MatchBatteryForEndpoint(string endpointFriendlyName, IReadOnlyDictionary<string, int> batteryMap)
    {
        if (batteryMap.Count == 0 || string.IsNullOrWhiteSpace(endpointFriendlyName))
        {
            return null;
        }

        if (batteryMap.TryGetValue(endpointFriendlyName, out var exact))
        {
            return exact;
        }

        foreach (var kvp in batteryMap)
        {
            if (endpointFriendlyName.Contains(kvp.Key, StringComparison.OrdinalIgnoreCase) ||
                kvp.Key.Contains(endpointFriendlyName, StringComparison.OrdinalIgnoreCase))
            {
                return kvp.Value;
            }
        }

        return null;
    }

    private static int? ReadBatteryLevel(uint devInst)
    {
        var key = DEVPKEY_Device_BatteryLevel;
        uint propSize = 0;
        CM_Get_DevNode_PropertyW(devInst, ref key, out var propType, IntPtr.Zero, ref propSize, 0);
        if (propSize == 0 || (propType != DEVPROP_TYPE_BYTE && propType != DEVPROP_TYPE_UINT32))
        {
            return null;
        }

        var ptr = Marshal.AllocHGlobal((int)propSize);
        try
        {
            if (CM_Get_DevNode_PropertyW(devInst, ref key, out propType, ptr, ref propSize, 0) != CR_SUCCESS)
            {
                return null;
            }

            var value = propType == DEVPROP_TYPE_BYTE
                ? Marshal.ReadByte(ptr)
                : (int)Marshal.ReadInt32(ptr);

            return value is >= 0 and <= 100 ? value : null;
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    private static string? ReadStringProperty(uint devInst, DEVPROPKEY key)
    {
        uint propSize = 0;
        CM_Get_DevNode_PropertyW(devInst, ref key, out var propType, IntPtr.Zero, ref propSize, 0);
        if (propSize == 0 || propType != DEVPROP_TYPE_STRING)
        {
            return null;
        }

        var ptr = Marshal.AllocHGlobal((int)propSize);
        try
        {
            if (CM_Get_DevNode_PropertyW(devInst, ref key, out _, ptr, ref propSize, 0) != CR_SUCCESS)
            {
                return null;
            }

            return Marshal.PtrToStringUni(ptr);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    [DllImport("CfgMgr32.dll", CharSet = CharSet.Unicode)]
    private static extern uint CM_Get_Device_ID_List_SizeW(out uint pulLen, string? pszFilter, uint ulFlags);

    [DllImport("CfgMgr32.dll", CharSet = CharSet.Unicode)]
    private static extern uint CM_Get_Device_ID_ListW(string? pszFilter, [Out] char[] Buffer, uint BufferLen, uint ulFlags);

    [DllImport("CfgMgr32.dll", CharSet = CharSet.Unicode)]
    private static extern uint CM_Locate_DevNodeW(out uint pdnDevInst, string pDeviceID, uint ulFlags);

    [DllImport("CfgMgr32.dll", CharSet = CharSet.Unicode)]
    private static extern uint CM_Get_DevNode_PropertyW(
        uint dnDevInst,
        ref DEVPROPKEY PropertyKey,
        out uint PropertyType,
        IntPtr PropertyBuffer,
        ref uint PropertyBufferSize,
        uint ulFlags);
}
