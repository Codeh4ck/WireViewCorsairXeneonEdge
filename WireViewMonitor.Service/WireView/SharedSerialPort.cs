using System.IO.Ports;

namespace WireViewMonitor.Service.WireView;

internal sealed class SharedSerialPort(string portName, int baudRate, Parity parity, int dataBits, StopBits stopBits)
    : SerialPort(portName, baudRate, parity, dataBits, stopBits)
{
    private readonly Mutex _mutex = new Mutex(false, "Global\\Access_USB_Sensors");
    private bool _hasMutex;

    private int MutexTimeout { get; set; } = 2000;

    public new bool Open()
    {
        try
        {
            if (_mutex.WaitOne(MutexTimeout))
            {
                base.Open();
                _hasMutex = true;
            }
        }
        catch (AbandonedMutexException)
        {
            try
            {
                base.Open();
                _hasMutex = true;
            }
            catch (Exception)
            {
                _mutex.ReleaseMutex();
                _hasMutex = false;
            }
        }
        catch (Exception)
        {
            _mutex.ReleaseMutex();
            _hasMutex = false;
        }

        return _hasMutex;
    }

    public new void Close()
    {
        if (!_hasMutex)
        {
            return;
        }

        if (IsOpen)
        {
            try
            {
                BaseStream.Flush();
                BaseStream.Close();
            }
            catch
            {
                // ignored
            }
        }

        try
        {
            _hasMutex = false;
            _mutex.ReleaseMutex();
        }
        catch (Exception)
        {
            // ignored
        }
    }

    protected override void Dispose(bool disposing)
    {
        try
        {
            if (!disposing)
            {
                return;
            }

            _mutex.Dispose();
        }
        finally
        {
            base.Dispose(disposing);
        }
    }

    public new void Write(byte[] buffer, int offset, int count)
    {
        if (!_hasMutex)
        {
            return;
        }

        base.Write(buffer, offset, count);
    }

    public new int Read(byte[] buffer, int offset, int count)
    {
        return _hasMutex ? base.Read(buffer, offset, count) : 0;
    }

    public new void DiscardInBuffer()
    {
        if (!_hasMutex)
        {
            return;
        }

        base.DiscardInBuffer();
    }

    public new void DiscardOutBuffer()
    {
        if (!_hasMutex)
        {
            return;
        }

        base.DiscardOutBuffer();
    }
}