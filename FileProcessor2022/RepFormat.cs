namespace FileProcessor2022;

internal class RepFormat
{
    public string? GapType { get; set; }
    public int Repeat { get; set; }
    public int GapSize { get; set; }
    public ulong EndPrime { get; set; }
    public ulong StartPrime { get; set; }
    public decimal When { get; set; }
}