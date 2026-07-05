namespace AsusPinMonitoring.Contracts.Output;

public class GetConfigResponse
{
    public int PollIntervalMs { get; init; }
    public int ImbalanceAmperageThreshold { get; init; }
    public int ImbalancePercentageThreshold { get; init; } 
    public int ImbalanceDurationThresholdMs { get; init; } 
    public float WireOvercurrentAmperageThreshold  { get; init; }
    public int WireOvercurrentThresholdMs { get; init; } 
}