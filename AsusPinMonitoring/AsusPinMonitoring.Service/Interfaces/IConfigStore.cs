using AsusPinMonitoring.Contracts.Common;

namespace AsusPinMonitoring.Service.Interfaces;

public interface IConfigStore
{
    Configuration Configuration { get; set; }
    Configuration Load();
    void Save(Configuration config);
}