namespace AsusWireviewPinMonitoring.Service.Wireview;

public struct SpDeviceInfoData
{
    public uint CbSize { get; set; }
    public Guid ClassGuid { get; set; }
    public uint DevInst { get; set; }
    public IntPtr Reserved { get; set; }
}