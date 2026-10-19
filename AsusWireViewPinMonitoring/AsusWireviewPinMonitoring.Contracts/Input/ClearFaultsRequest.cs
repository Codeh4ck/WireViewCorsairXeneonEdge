namespace AsusWireviewPinMonitoring.Contracts.Input;

public class ClearFaultsRequest
{
    // 0xFFFF indicates clear all
    public ushort FaultStatusMask { get; set; }
    public ushort FaultLogMask { get; set; } 
}