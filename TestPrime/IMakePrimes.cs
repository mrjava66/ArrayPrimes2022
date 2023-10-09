namespace TestPrime;

public interface IMakePrimes
{
    void MakePrimesTask();
    Dictionary<ulong, ulong> DictAllPrimes { get; }
    int NumPrimes { get; }
    ulong[] ArrayAllPrimes { get; }
}