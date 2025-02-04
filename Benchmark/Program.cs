using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BreadPack.Utilities;

namespace Benchmark;

[MemoryDiagnoser]
public class RangeExpressionBenchmarks
{
    [Benchmark]
    public void ParseSingleRange()
    {
        var range = new RangeExpression<int>("1..5");
        range.Contains(3);
    }

    [Benchmark]
    public void ParseMultipleRanges()
    {
        var range = new RangeExpression<int>("1..5,7..10,15..20");
        range.Contains(8);
    }

    [Benchmark]
    public void ParseOpenEndedRanges()
    {
        var range = new RangeExpression<int>("1..,..10,15..,..20");
        range.Contains(8);
    }

    [Benchmark]
    public void ParseDateTimeRanges()
    {
        var range = new RangeExpression<DateTime>("2024-01-01..2024-12-31");
        range.Contains(new DateTime(2024, 6, 15));
    }

    [Benchmark]
    public void ParseDoubleRanges()
    {
        var range = new RangeExpression<double>("-1.5..1.5,2.0..3.0");
        range.Contains(1.0);
    }

    [Benchmark]
    public void ContainsMultipleCalls()
    {
        var range = new RangeExpression<int>("1..5,7..10,15..20");
        for (int i = 0; i < 100; i++)
        {
            range.Contains(i);
        }
    }

    [Benchmark]
    public void GetRangesAllocation()
    {
        var range = new RangeExpression<int>("1..5,7..10,15..20");
        foreach (var (start, end) in range.GetRanges())
        {
            _ = start;
            _ = end;
        }
    }

    [Benchmark]
    public void ReuseRangeExpression()
    {
        var range = new RangeExpression<int>("1..5,7..10,15..20");
        for (int i = 0; i < 100; i++)
        {
            range.Contains(i % 20);
        }
    }

    [Benchmark]
    public void ParseRangeWithLongPattern()
    {
        var range = new RangeExpression<int>("1..5,7..10,15..20,25..30,35..40,45..50,55..60");
        range.Contains(30);
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<RangeExpressionBenchmarks>();
    }
}
