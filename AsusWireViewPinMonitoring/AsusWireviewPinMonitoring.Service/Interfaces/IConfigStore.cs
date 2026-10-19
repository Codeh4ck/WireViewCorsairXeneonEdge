using AsusWireviewPinMonitoring.Contracts.Common;

namespace AsusWireviewPinMonitoring.Service.Interfaces;

public interface IConfigStore
{
    Configuration Configuration { get; set; }
    Configuration Load();
    void Save(Configuration config);
}