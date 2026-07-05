namespace AsusPinMonitoring.Contracts.Common;

public class Configuration
{
    public int PollIntervalMs { get; init; }
    public int ImbalanceAmperageThreshold { get; init; }
    public int ImbalancePercentageThreshold { get; init; } 
    public int ImbalanceDurationThresholdMs { get; init; } 
    public float WireOvercurrentAmperageThreshold  { get; init; }
    public int WireOvercurrentThresholdMs { get; init; } 
    
    public static readonly Configuration Default = new ()
    {
        PollIntervalMs = 1000,
        ImbalanceAmperageThreshold = 6,
        ImbalancePercentageThreshold = 30,
        ImbalanceDurationThresholdMs = 3000,
        WireOvercurrentAmperageThreshold = 9.2f,
        WireOvercurrentThresholdMs = 3000
    };
}