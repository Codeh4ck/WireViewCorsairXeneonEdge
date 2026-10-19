using System.Text;

namespace AsusWireviewPinMonitoring.Service.Wireview;

internal static class WindowsSetupApi
{
    private static readonly IntPtr InvalidHandleValue = new(-1);

    public static IntPtr SetupDiGetClassDevsAllClassesPresent() => WinApi.SetupDiGetClassDevsW(IntPtr.Zero, null, IntPtr.Zero, 6U);

    public static bool IsInvalidHandle(IntPtr h) => h == IntPtr.Zero || h == InvalidHandleValue;

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

    private static string DecodeUtf16String(byte[] bytes)
    {
        var str = Encoding.Unicode.GetString(bytes);
        var num = str.IndexOf(char.MinValue, (StringComparison)4);
        
        return num < 0 ? str : str[..num];
    }

    private static string[] DecodeMultiSzUtf16(byte[] bytes) => Encoding.Unicode.GetString(bytes).Split(char.MinValue, (StringSplitOptions)3);
}