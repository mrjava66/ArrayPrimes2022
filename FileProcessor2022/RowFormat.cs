internal class RowFormat
{
    public string GapType { get; set; }
    public int GapSize { get; set; }
    public string Primes { get; set; }
    public ulong EndPrime { get; set; }
    public ulong StartPrime { get; set; }
    public decimal When { get; set; }

    public bool Tail { get; set; }
}