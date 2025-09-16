using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSorter;

public class ProgressReporter : IDisposable
{
    private long _bytesRead;
    private long _linesRead;
    private long _runsWritten;
    private long _mergesCompleted;
    private readonly Stopwatch _sw = Stopwatch.StartNew();
    private readonly Timer _timer;

    public ProgressReporter()
    {
        _timer = new Timer(_ => Print("heartbeat"), null,
            dueTime: TimeSpan.FromSeconds(15),
            period: TimeSpan.FromSeconds(15));
        Console.WriteLine("[start] Sorting...");
    }

    public void AddBytes(long n) => Interlocked.Add(ref _bytesRead, n);
    public void AddLines(long n) => Interlocked.Add(ref _linesRead, n);
    public void IncRuns() => Interlocked.Increment(ref _runsWritten);
    public void IncMerges() => Interlocked.Increment(ref _mergesCompleted);

    private void Print(string tag)
    {
        var br = Interlocked.Read(ref _bytesRead);
        var lr = Interlocked.Read(ref _linesRead);
        var rw = Interlocked.Read(ref _runsWritten);
        var mc = Interlocked.Read(ref _mergesCompleted);
        Console.WriteLine(
            string.Create(CultureInfo.InvariantCulture,
              $"[heartbeat] read={(br / 1_000_000_000.0):F1} GB lines={lr} runs={rw} merges={mc} elapsed={_sw.Elapsed:g}"));
    }

    public void Dispose()
    {
        _timer?.Dispose();
        Print("final");
    }
}