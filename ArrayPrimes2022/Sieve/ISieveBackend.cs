namespace ArrayPrimes2022.Sieve;

internal interface ISieveBackend
{
    string Name { get; }

    void Execute(
        ulong loopMinCheckedValue,
        uint[] fullDivisorList,
        uint[] offsets,
        ulong divisorsFillPosition,
        ulong divisorPosition,
        byte[] sieveBytes);
}
