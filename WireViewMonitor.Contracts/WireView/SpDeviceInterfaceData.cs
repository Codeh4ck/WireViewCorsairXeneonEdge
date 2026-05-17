namespace WireViewMonitor.Contracts.WireView;

public struct SpDeviceInterfaceData
{
    public uint CbSize { get; set; }
    public Guid InterfaceClassGuid { get; set; }
    public uint Flags { get; set; }
    public IntPtr Reserved { get; set; }
}