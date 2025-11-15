```

BenchmarkDotNet v0.15.6, Windows 10 (10.0.19045.5737/22H2/2022Update)
AMD Ryzen 9 5950X 3.40GHz, 1 CPU, 32 logical and 16 physical cores
.NET SDK 10.0.100
  [Host]    : .NET 8.0.14 (8.0.14, 8.0.1425.11118), X64 RyuJIT x86-64-v3
  .NET 10.0 : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v3
  .NET 8.0  : .NET 8.0.14 (8.0.14, 8.0.1425.11118), X64 RyuJIT x86-64-v3
  .NET 9.0  : .NET 9.0.2 (9.0.2, 9.0.225.6610), X64 RyuJIT x86-64-v3


```
| Method                                     | Job       | Runtime   | Mean     | Error   | StdDev   | Median   | Ratio | RatioSD | Rank | Gen0   | Allocated | Alloc Ratio |
|------------------------------------------- |---------- |---------- |---------:|--------:|---------:|---------:|------:|--------:|-----:|-------:|----------:|------------:|
| LinqOrderByInt                             | .NET 10.0 | .NET 10.0 | 119.1 ns | 2.33 ns |  2.29 ns | 119.1 ns |  1.00 |    0.03 |    2 | 0.0324 |     544 B |        1.00 |
| LinqOrderByEnum                            | .NET 10.0 | .NET 10.0 | 112.1 ns | 1.82 ns |  1.70 ns | 112.7 ns |  0.94 |    0.02 |    1 | 0.0324 |     544 B |        1.00 |
| LinqOrderByEnumFrozenDictionaryComparer    | .NET 10.0 | .NET 10.0 | 152.4 ns | 0.93 ns |  0.87 ns | 152.1 ns |  1.28 |    0.03 |    3 | 0.0324 |     544 B |        1.00 |
| LinqOrderByEnumFrozenDictionaryIntComparer | .NET 10.0 | .NET 10.0 | 155.5 ns | 1.82 ns |  1.70 ns | 155.8 ns |  1.31 |    0.03 |    3 | 0.0324 |     544 B |        1.00 |
| LinqOrderByEnumArrayComparer               | .NET 10.0 | .NET 10.0 | 146.4 ns | 2.98 ns |  3.06 ns | 146.3 ns |  1.23 |    0.03 |    3 | 0.0324 |     544 B |        1.00 |
|                                            |           |           |          |         |          |          |       |         |      |        |           |             |
| LinqOrderByInt                             | .NET 8.0  | .NET 8.0  | 135.9 ns | 2.76 ns |  4.83 ns | 134.0 ns |  1.00 |    0.05 |    1 | 0.0305 |     512 B |        1.00 |
| LinqOrderByEnum                            | .NET 8.0  | .NET 8.0  | 130.7 ns | 1.13 ns |  0.94 ns | 130.9 ns |  0.96 |    0.03 |    1 | 0.0305 |     512 B |        1.00 |
| LinqOrderByEnumFrozenDictionaryComparer    | .NET 8.0  | .NET 8.0  | 397.9 ns | 6.61 ns |  5.86 ns | 399.6 ns |  2.93 |    0.11 |    3 | 0.0305 |     512 B |        1.00 |
| LinqOrderByEnumFrozenDictionaryIntComparer | .NET 8.0  | .NET 8.0  | 650.1 ns | 3.67 ns |  2.87 ns | 650.1 ns |  4.79 |    0.17 |    4 | 0.0305 |     512 B |        1.00 |
| LinqOrderByEnumArrayComparer               | .NET 8.0  | .NET 8.0  | 173.7 ns | 2.66 ns |  3.16 ns | 173.8 ns |  1.28 |    0.05 |    2 | 0.0305 |     512 B |        1.00 |
|                                            |           |           |          |         |          |          |       |         |      |        |           |             |
| LinqOrderByInt                             | .NET 9.0  | .NET 9.0  | 129.2 ns | 1.66 ns |  1.55 ns | 129.2 ns |  1.00 |    0.02 |    1 | 0.0324 |     544 B |        1.00 |
| LinqOrderByEnum                            | .NET 9.0  | .NET 9.0  | 135.1 ns | 2.42 ns |  2.15 ns | 135.0 ns |  1.05 |    0.02 |    2 | 0.0324 |     544 B |        1.00 |
| LinqOrderByEnumFrozenDictionaryComparer    | .NET 9.0  | .NET 9.0  | 452.8 ns | 9.11 ns | 11.84 ns | 451.0 ns |  3.50 |    0.10 |    3 | 0.0324 |     544 B |        1.00 |
| LinqOrderByEnumFrozenDictionaryIntComparer | .NET 9.0  | .NET 9.0  | 619.1 ns | 5.59 ns |  4.96 ns | 617.7 ns |  4.79 |    0.07 |    4 | 0.0324 |     544 B |        1.00 |
| LinqOrderByEnumArrayComparer               | .NET 9.0  | .NET 9.0  | 137.6 ns | 2.03 ns |  1.89 ns | 137.4 ns |  1.07 |    0.02 |    2 | 0.0324 |     544 B |        1.00 |
