using System.Runtime.InteropServices;

namespace AsusWireviewPinMonitoring.Service.Wireview;

internal static class WinApi
{
    [DllImport("setupapi.dll", CharSet = (CharSet)3, SetLastError = true)]
    internal static extern IntPtr SetupDiGetClassDevsW(IntPtr classGuid, string? enumerator, IntPtr hwndParent, uint flags);

    [DllImport("setupapi.dll", SetLastError = true)]
    internal static extern bool SetupDiEnumDeviceInfo(IntPtr deviceInfoSet, uint memberIndex, ref SpDeviceInfoData deviceInfoData);

    [DllImport("setupapi.dll", SetLastError = true)]
    internal static extern bool SetupDiDestroyDeviceInfoList(IntPtr deviceInfoSet);

    [DllImport("setupapi.dll", CharSet = (CharSet)3, SetLastError = true)]
    internal static extern bool SetupDiGetDeviceRegistryPropertyW(IntPtr deviceInfoSet, ref SpDeviceInfoData deviceInfoData,
        uint property, out uint propertyRegDataType, byte[]? propertyBuffer, uint propertyBufferSize, out uint requiredSize);
}