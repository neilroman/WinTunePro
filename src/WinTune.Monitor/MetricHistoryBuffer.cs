namespace WinTune.Monitor;

public readonly record struct MetricSample(DateTime Timestamp, float CpuPercent, float RamPercent);

public sealed class MetricHistoryBuffer
{
    private readonly MetricSample[] _buffer;
    private readonly object _sync = new();
    private int _start;
    private int _count;

    public MetricHistoryBuffer(int capacity = 10080)
    {
        _buffer = new MetricSample[capacity];
    }

    public void Add(float cpu, float ram)
    {
        var sample = new MetricSample(DateTime.UtcNow, cpu, ram);
        lock (_sync)
        {
            if (_count < _buffer.Length)
            {
                _buffer[(_start + _count) % _buffer.Length] = sample;
                _count++;
            }
            else
            {
                _buffer[_start] = sample;
                _start = (_start + 1) % _buffer.Length;
            }
        }
    }

    public IReadOnlyList<MetricSample> GetRange(TimeSpan window)
    {
        var cutoff = DateTime.UtcNow - window;
        lock (_sync)
        {
            var result = new List<MetricSample>(_count);
            for (int i = 0; i < _count; i++)
            {
                var s = _buffer[(_start + i) % _buffer.Length];
                if (s.Timestamp >= cutoff)
                    result.Add(s);
            }
            return result;
        }
    }
}
