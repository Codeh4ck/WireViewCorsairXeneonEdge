using System.Runtime.InteropServices;
using WireViewMonitor.Contracts.WireView;

namespace WireViewMonitor.Service.WireView;

internal static class Stm32PortFinder
{
    public static List<string> FindMatchingComPorts()
    {
        var matchingComPorts = new List<string>();

        if (!OperatingSystem.IsWindows())
        {
            return matchingComPorts;
        }

        IntPtr allClassesPresent = WindowsSetupApi.SetupDiGetClassDevsAllClassesPresent();

        if (WindowsSetupApi.IsInvalidHandle(allClassesPresent))
        {
            return matchingComPorts;
        }

        try
        {
            SpDeviceInfoData deviceInfoData = new SpDeviceInfoData()
            {
                CbSize = (uint)Marshal.SizeOf<SpDeviceInfoData>()
            };

            for (uint memberIndex = 0; WinApi.SetupDiEnumDeviceInfo(allClassesPresent, memberIndex, ref deviceInfoData); ++memberIndex)
            {
                var registryPropertyMultiSz = WindowsSetupApi.TryGetDeviceRegistryPropertyMultiSz(allClassesPresent, ref deviceInfoData, 1U);

                if (registryPropertyMultiSz == null || registryPropertyMultiSz.Length == 0 || !HardwareIdsContainVidPid(registryPropertyMultiSz, "VID_0483", "PID_5740")) continue;
                
                var registryPropertyString = WindowsSetupApi.TryGetDeviceRegistryPropertyString(allClassesPresent, ref deviceInfoData, 12U);

                if (string.IsNullOrWhiteSpace(registryPropertyString)) continue;
                
                var fromFriendlyName = TryExtractComPortFromFriendlyName(registryPropertyString);

                if (!string.IsNullOrWhiteSpace(fromFriendlyName))
                {
                    matchingComPorts.Add(fromFriendlyName);
                }
            }
        }
        finally
        {
            WinApi.SetupDiDestroyDeviceInfoList(allClassesPresent);
        }

        return matchingComPorts;
    }

    private static bool HardwareIdsContainVidPid(string[] hardwareIds, string vid, string pid) => hardwareIds.Any(hardwareId =>
        !string.IsNullOrWhiteSpace(hardwareId) && hardwareId.Contains(vid, StringComparison.OrdinalIgnoreCase) && hardwareId.Contains(pid, StringComparison.OrdinalIgnoreCase));

    private static string? TryExtractComPortFromFriendlyName(string friendlyName)
    {
        int comIndex = friendlyName.LastIndexOf("(COM", StringComparison.OrdinalIgnoreCase);
        
        if (comIndex < 0)
        {
            return null;
        }

        int closingParenthesisIndex = friendlyName.IndexOf(')', comIndex);
        return closingParenthesisIndex <= comIndex ? null : friendlyName.Substring(comIndex + 1, closingParenthesisIndex - comIndex - 1);
    }
}