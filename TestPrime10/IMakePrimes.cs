namespace TestPrime10;

public interface IMakePrimes
{
    Dictionary<ulong, ulong> DictAllPrimes { get; }
    int NumPrimes { get; }
    ulong[] ArrayAllPrimes { get; }
    void MakePrimesTask();
    static string? Progress { get; set; }
}