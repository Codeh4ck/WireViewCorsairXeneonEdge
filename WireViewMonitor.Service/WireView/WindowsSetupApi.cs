using System.Runtime.InteropServices;
using System.Text;
using WireViewMonitor.Contracts.WireView;

namespace WireViewMonitor.Service.WireView;

internal static class WindowsSetupApi
{
    private static readonly IntPtr InvalidHandleValue = new(-1);

    public static IntPtr SetupDiGetClassDevsAllClassesPresent() => WinApi.SetupDiGetClassDevsW(IntPtr.Zero, null, IntPtr.Zero, 6U);
    private static IntPtr SetupDiGetClassDevsDeviceInterfacePresent(ref Guid interfaceGuid) => WinApi.SetupDiGetClassDevs(ref interfaceGuid, null, IntPtr.Zero, 18U);

    public static bool IsInvalidHandle(IntPtr h) => h == IntPtr.Zero || h == InvalidHandleValue;

    public static IEnumerable<string> EnumerateDeviceInstanceIds()
    {
        if (!OperatingSystem.IsWindows())
        {
            yield break;
        }

        var handle = SetupDiGetClassDevsAllClassesPresent();

        if (IsInvalidHandle(handle))
        {
            yield break;
        }

        try
        {
            var deviceInfoData = new SpDeviceInfoData()
            {
                CbSize = (uint)Marshal.SizeOf<SpDeviceInfoData>()
            };
                    
            for (uint i = 0; WinApi.SetupDiEnumDeviceInfo(handle, i, ref deviceInfoData); ++i)
            {
                var deviceInstanceId = TryGetDeviceInstanceId(deviceInfoData.DevInst);
                if (!string.IsNullOrWhiteSpace(deviceInstanceId))
                {
                    yield return deviceInstanceId;
                }
            }
        }
        finally
        {
            WinApi.SetupDiDestroyDeviceInfoList(handle);
        }
    }

    public static IEnumerable<(string InstanceId, string? DeviceDesc, string? FriendlyName)> EnumeratePresentDevicesWithNames()
    {
        if (!OperatingSystem.IsWindows())
        {
            yield break;
        }

        var handle = SetupDiGetClassDevsAllClassesPresent();

        if (IsInvalidHandle(handle))
        {
            yield break;
        }

        try
        {
            var deviceInfoData = new SpDeviceInfoData()
            {
                CbSize = (uint)Marshal.SizeOf<SpDeviceInfoData>()
            };
                    
            for (uint i = 0; WinApi.SetupDiEnumDeviceInfo(handle, i, ref deviceInfoData); ++i)
            {
                var deviceInstanceId = TryGetDeviceInstanceId(deviceInfoData.DevInst);

                if (string.IsNullOrWhiteSpace(deviceInstanceId)) continue;
                
                var registryPropertyString1 = TryGetDeviceRegistryPropertyString(handle, ref deviceInfoData, 0U);
                var registryPropertyString2 = TryGetDeviceRegistryPropertyString(handle, ref deviceInfoData, 12U);
                yield return (deviceInstanceId, registryPropertyString1, registryPropertyString2);
            }
        }
        finally
        {
            WinApi.SetupDiDestroyDeviceInfoList(handle);
        }
    }

    public static string? TryGetDeviceRegistryPropertyString(IntPtr devInfoSet, ref SpDeviceInfoData deviceInfoData, uint property)
    {
        WinApi.SetupDiGetDeviceRegistryPropertyW(devInfoSet, ref deviceInfoData, property, out _, null, 0U, out var requiredSize);
        
        if (requiredSize == 0U)
        {
            return null;
        }

        var numArray = new byte[(int)requiredSize];
        
        if (!WinApi.SetupDiGetDeviceRegistryPropertyW(devInfoSet, ref deviceInfoData, property, out _, numArray, (uint)numArray.Length, out var _))
        {
            return null;
        }

        var str = DecodeUtf16String(numArray);
        return !string.IsNullOrWhiteSpace(str) ? str : null;
    }

    public static string[]? TryGetDeviceRegistryPropertyMultiSz(IntPtr devInfoSet, ref SpDeviceInfoData deviceInfoData, uint property)
    {
        WinApi.SetupDiGetDeviceRegistryPropertyW(devInfoSet, ref deviceInfoData, property, out _, null, 0U, out var requiredSize);
        
        if (requiredSize == 0U)
        {
            return null;
        }

        var buffer = new byte[(int)requiredSize];

        return !WinApi.SetupDiGetDeviceRegistryPropertyW(devInfoSet, ref deviceInfoData, property, out _, buffer, (uint)buffer.Length, out var _)
            ? null
            : DecodeMultiSzUtf16(buffer);
    }

    public static IEnumerable<string> EnumerateDeviceInterfacePaths(Guid interfaceGuid)
    {
        if (!OperatingSystem.IsWindows())
        {
            yield break;
        }

        var handle = SetupDiGetClassDevsDeviceInterfacePresent(ref interfaceGuid);

        if (IsInvalidHandle(handle))
        {
            yield break;
        }

        try
        {
            var index = 0;
                    
            var deviceInterfaceData = new SpDeviceInterfaceData
            {
                CbSize = (uint)Marshal.SizeOf<SpDeviceInterfaceData>()
            };
                    
            for (var interfaceDeviceData = deviceInterfaceData; WinApi.SetupDiEnumDeviceInterfaces(handle, IntPtr.Zero, ref interfaceGuid, (uint)index, ref interfaceDeviceData); ++index)
            {
                var devicePath = TryGetDevicePath(handle, ref interfaceDeviceData);
                if (!string.IsNullOrWhiteSpace(devicePath))
                {
                    yield return devicePath;
                }
            }
        }
        finally
        {
            WinApi.SetupDiDestroyDeviceInfoList(handle);
        }
    }

    private static string? TryGetDeviceInstanceId(uint devInst)
    {
        var deviceId = new char[512];
        
        if (WinApi.CM_Get_Device_ID(devInst, deviceId, (uint)deviceId.Length, 0U) == 0U)
        {
            return new string(deviceId).TrimEnd(char.MinValue);
        }

        if (WinApi.CM_Get_Device_ID_Size(out var pulLen, devInst, 0U) != 0U || pulLen <= 0U)
        {
            return null;
        }

        var deviceIdEx = new char[(int)pulLen + 1];
        return WinApi.CM_Get_Device_ID(devInst, deviceIdEx, (uint)deviceIdEx.Length, 0U) == 0U ? new string(deviceIdEx).TrimEnd(char.MinValue) : null;
    }

    private static string DecodeUtf16String(byte[] bytes)
    {
        var str = Encoding.Unicode.GetString(bytes);
        var num = str.IndexOf(char.MinValue, (StringComparison)4);
        
        return num < 0 ? str : str[..num];
    }

    private static string[] DecodeMultiSzUtf16(byte[] bytes) => Encoding.Unicode.GetString(bytes).Split(char.MinValue, (StringSplitOptions)3);

    private static string? TryGetDevicePath(IntPtr h, ref SpDeviceInterfaceData idd)
    {
        uint requiredSize = 0;
        WinApi.SetupDiGetDeviceInterfaceDetail(h, ref idd, IntPtr.Zero, 0U, ref requiredSize, IntPtr.Zero);
        
        if (requiredSize == 0U)
        {
            return null;
        }

        var deviceInterfaceDetailData = Marshal.AllocHGlobal((int)requiredSize);
        try
        {
            var num = IntPtr.Size == 8 ? 8 : 6;
            Marshal.WriteInt32(deviceInterfaceDetailData, num);
            if (!WinApi.SetupDiGetDeviceInterfaceDetail(h, ref idd, deviceInterfaceDetailData, requiredSize, ref requiredSize, IntPtr.Zero))
            {
                return null;
            }

            var stringAuto = Marshal.PtrToStringAuto(deviceInterfaceDetailData + num);
            return string.IsNullOrWhiteSpace(stringAuto) ? null : stringAuto;
        }
        finally
        {
            Marshal.FreeHGlobal(deviceInterfaceDetailData);
        }
    }
}