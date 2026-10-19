namespace AsusWireviewPinMonitoring.Contracts.Input;

public class UpdateConfigRequest
{
    public int PollIntervalMs { get; set; }
    public int ImbalanceAmperageThreshold { get; init; }
    public int ImbalancePercentageThreshold { get; init; } 
    public int ImbalanceDurationThresholdMs { get; init; } 
    public float WireOvercurrentAmperageThreshold  { get; init; }
    public int WireOvercurrentThresholdMs { get; init; } 
}