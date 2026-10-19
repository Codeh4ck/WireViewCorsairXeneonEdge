namespace AsusWireviewPinMonitoring.Contracts.Common;

public class Configuration
{
    public int PollIntervalMs { get; init; }
    public static readonly Configuration Default = new ()
    {
        PollIntervalMs = 1000,
    };
}