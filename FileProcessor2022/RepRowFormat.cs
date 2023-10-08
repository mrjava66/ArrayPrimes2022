namespace FileProcessor2022;

internal class RowFormat
{
    public string? GapType { get; set; }
    public uint GapSize { get; set; }
    public ulong EndPrime { get; set; }
    public ulong StartPrime { get; set; }
    public decimal When { get; set; }
}

internal class RepRowFormat : RowFormat
{
    public int Repeat { get; set; }
}

internal class GapRowFormat : RowFormat
{
    //public string? Primes { get; set; }

    public bool Tail { get; set; }
}