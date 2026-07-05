namespace AsusPinMonitoring.Contracts.Common;

public class WireOvercurrent
{
    public Dictionary<string, float> PinValues { get; init; } = [];
}