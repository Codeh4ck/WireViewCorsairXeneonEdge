namespace AsusPinMonitoring.Contracts.Common;

public class CurrentImbalance
{
    public float MaxPinAmperage { get; init; }
    public string MaxPinName { get; init; } = string.Empty;
    
    public float MinPinAmperage { get; init; }
    public string MinPinName { get; init; } = string.Empty;
    
    public int PercentageDifference { get; init; }
    public DateTimeOffset Timestamp { get; init; }
}