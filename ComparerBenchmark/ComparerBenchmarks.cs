using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.Collections.Frozen;
using System.Runtime.CompilerServices;

namespace ComparerBenchmark;

public enum SomeType
{
    One,
    Two,
    Three,
    Four,
    Five,
    Six,
    Seven,
    Eight,
    Nine,
    Ten,
}

public class SomeData
{
    public int Order { get; set; }
}

public class SomeEnumData
{
    public SomeType Type { get; set; }
}


public sealed class SomeTypeFrozenDictionaryComparer : IComparer<SomeType>
{
    private static readonly FrozenDictionary<SomeType, int> Order =
        new Dictionary<SomeType, int>
            {
                // imagine here some specific order
                { SomeType.One, 3 },
                { SomeType.Two, 4 },
                { SomeType.Three, 1 },
                { SomeType.Four, 2 },
                { SomeType.Five, 5 },
                { SomeType.Six, 10 },
                { SomeType.Seven, 8 },
                { SomeType.Eight, 9 },
                { SomeType.Nine, 7 },
                { SomeType.Ten, 6 },
            }
            .ToFrozenDictionary();

    public static SomeTypeFrozenDictionaryComparer Instance { get; } = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Compare(SomeType x, SomeType y)
    {
        return Order[x].CompareTo(Order[y]);
    }
}


public sealed class SomeTypeDictionaryComparer : IComparer<SomeType>
{
    private static readonly Dictionary<SomeType, int> Order = new ()
    {
        // imagine here some specific order
        { SomeType.One, 3 },
        { SomeType.Two, 4 },
        { SomeType.Three, 1 },
        { SomeType.Four, 2 },
        { SomeType.Five, 5 },
        { SomeType.Six, 10 },
        { SomeType.Seven, 8 },
        { SomeType.Eight, 9 },
        { SomeType.Nine, 7 },
        { SomeType.Ten, 6 },
    };

    public static SomeTypeDictionaryComparer Instance { get; } = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Compare(SomeType x, SomeType y)
    {
        return Order[x].CompareTo(Order[y]);
    }
}


public sealed class SomeTypeArrayComparer : IComparer<SomeType>
{
    public static readonly int[] Order = Enum
        .GetValues<SomeType>()
        .Select(x => (int)x)
        .ToArray();

    static SomeTypeArrayComparer()
    {
        Order[(int)SomeType.One] = 3;
        Order[(int)SomeType.Two] = 4;
        Order[(int)SomeType.Three] = 1;
        Order[(int)SomeType.Four] = 2;
        Order[(int)SomeType.Five] = 5;
        Order[(int)SomeType.Six] = 10;
        Order[(int)SomeType.Seven] = 8;
        Order[(int)SomeType.Eight] = 9;
        Order[(int)SomeType.Nine] = 7;
        Order[(int)SomeType.Ten] = 6;
    }

    public static SomeTypeArrayComparer Instance { get; } = new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int Compare(SomeType x, SomeType y)
    {
        return Order[(int)x].CompareTo(Order[(int)y]);
    }
}


[SimpleJob(RuntimeMoniker.Net10_0)]
[SimpleJob(RuntimeMoniker.Net90)]
[SimpleJob(RuntimeMoniker.Net80)]
[MemoryDiagnoser]
[MedianColumn]
[RankColumn]
public class ComparerBenchmarks
{
    private List<SomeData> _items = null!;
    private List<SomeEnumData> _enumItems = null!;

    [GlobalSetup]
    public void Setup()
    {
        _enumItems = Enum.GetValues<SomeType>().Select(x => new SomeEnumData { Type = x }).ToList();
        Shuffle(_enumItems);

        _items = _enumItems.Select(x => new SomeData { Order = SomeTypeArrayComparer.Order[(int)x.Type] }).ToList();
    }

    [Benchmark(Baseline = true)]
    public SomeData[] LinqOrderByInt()
    {
        return _items.OrderBy(x => x.Order).ToArray();
    }

    [Benchmark]
    public SomeEnumData[] LinqOrderByEnum()
    {
        return _enumItems.OrderBy(x => x.Type).ToArray();
    }

    [Benchmark]
    public SomeEnumData[] LinqOrderByEnumDictionaryComparer()
    {
        return _enumItems.OrderBy(x => x.Type, SomeTypeDictionaryComparer.Instance).ToArray();
    }

    [Benchmark]
    public SomeEnumData[] LinqOrderByEnumFrozenDictionaryComparer()
    {
        return _enumItems.OrderBy(x => x.Type, SomeTypeFrozenDictionaryComparer.Instance).ToArray();
    }

    [Benchmark]
    public SomeEnumData[] LinqOrderByEnumArrayComparer()
    {
        return _enumItems.OrderBy(x => x.Type, SomeTypeArrayComparer.Instance).ToArray();
    }

    private static void Shuffle<T>(List<T> list)
    {
        var n = list.Count;
        while (n > 1)
        {
            n--;
            var k = Random.Shared.Next(n + 1);
            (list[k], list[n]) = (list[n], list[k]);
        }
    }
}
