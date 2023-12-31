namespace TestPrime;

public interface IMakePrimes
{
    Dictionary<ulong, ulong> DictAllPrimes { get; }
    int NumPrimes { get; }
    ulong[] ArrayAllPrimes { get; }
    void MakePrimesTask();
}