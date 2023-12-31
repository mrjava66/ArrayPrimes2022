namespace TestPrime;

public class MakePrimesSieve : IMakePrimes
{
    private static readonly List<ulong> ListAllPrimes = new() { 2, 3, 5, 7 };

    private Dictionary<ulong, ulong>? _dictAllPrimes;

    public void MakePrimesTask()
    {
        var fullDivisorList = new uint[203280222];
        MakeBaseArrays(fullDivisorList);
        _dictAllPrimes = fullDivisorList.ToDictionary(x => (ulong)x, x => (ulong)x);
        if (_dictAllPrimes.ContainsKey(0))
            _dictAllPrimes.Remove(0);
        _dictAllPrimes.Add(4294967311, 4294967311);
    }

    public Dictionary<ulong, ulong> DictAllPrimes
    {
        get
        {
            while (_dictAllPrimes == null)
                MakePrimesTask();

            return _dictAllPrimes;
        }
    }

    public int NumPrimes => _dictAllPrimes?.Count ?? 0;
    public ulong[] ArrayAllPrimes => _dictAllPrimes?.Keys?.ToArray() ?? new ulong[] { 2, 3, 5, 7 };

    private static void MakeBaseArrays(uint[] fdl)
    {
        var goal = ulong.MaxValue; // the final value to get.
        var divisorArrayMax =
            Math.Sqrt(goal) > uint.MaxValue
                ? uint.MaxValue
                : (uint)Math.Sqrt(goal); // the max value in the divisor arrays.

        var arrays = new List<byte[]>(); // the holder of the divisor arrays
        const uint baseArrayCount = 8; // number of elements in the divisor arrays.  (count)
        var baseArrayUnitSize =
            1 + divisorArrayMax / baseArrayCount / 16; // size of the divisor array element (bytes)

        //refactor into make a report base arrays.
        for (uint i = 0; i < baseArrayCount; i++) // allocate and add the divisor arrays.
        {
            //byte[] arrayX = SuperFast.InitByteArrayNormal(0xFF, baseArrayUnitSize);
            var arrayX =
                new byte[baseArrayUnitSize]; //primes and possible primes have bit value of 0, composites have bit value of 1.
            arrays.Add(arrayX);
        }

        var sieveTop = (ulong)(Math.Sqrt(uint.MaxValue) + 1); // last value to sieve through the divisor array.

        var countPrimeNumber = 0;
        fdl[0] = 2;
        //don't sieve for 2
        foreach (var seedPrime in new ulong[] { 3, 5, 7, 11, 13 })
        {
            fdl[++countPrimeNumber] = (uint)seedPrime;
            //gr.ReportGap(seedPrime);
            StartUpSieve(arrays, arrays.Count, baseArrayUnitSize, seedPrime);
        }

        ulong arraySize16 = baseArrayUnitSize * 16;

        for (var a = 0; a < baseArrayCount; a++)
        for (var l = a == 0 ? 1 : (ulong)0; l < baseArrayUnitSize; l++)
        {
            if (arrays[a][l] == 255) continue;
            for (ulong pos = 0; pos < 8; pos++) // only check odd for prime.
            {
                if (IsBitSet(arrays[a][l], (int)pos)) continue;

                var prime = (ulong)a * arraySize16 + l * 16 + pos * 2 + 1;
                fdl[++countPrimeNumber] = (uint)prime;

                if (prime < sieveTop)
                    StartUpSieve(arrays, arrays.Count, baseArrayUnitSize,
                        prime); // don't need to sieve values greater than top.
            }
        }
    }

    private static bool IsBitSet(byte b, int pos)
    {
        return (b & (1 << pos)) != 0;
    }

    private static void StartUpSieve(List<byte[]> arrays, int arrayCount, ulong arraySize, ulong prime)
    {
        var arraySize16 = arraySize * 16;
        var tasks = new List<Task>();
        var nextMark = prime * prime;
        //MarkAnArray(arraySize, prime, nextMark, arrays[0]);
        var newTask = Task.Factory.StartNew(() => MarkAnArray(arraySize, prime, nextMark, arrays[0]));
        tasks.Add(newTask);
        for (var i = 1; i < arrayCount; i++)
        {
            var array = arrays[i];
            var iArraySize16 = (ulong)i * arraySize16;
            var mulUnder = iArraySize16 / prime; // largest multiplier of prime <= i*arraySize*8
            var mulOver = (mulUnder + 1) * prime; // first multiple of prime > i*arraySize*8

            if (mulOver % 2 == 0) mulOver += prime; // don't start on an even.
            if (mulOver < nextMark) mulOver = nextMark; // use the square of mark if it is higher.

            var nextMarkL = mulOver - iArraySize16; // how far into the array that lands.
            //MarkAnArray(arraySize, prime, nextMarkL, array);
            //var ip = i;
            var newTaskL = Task.Factory.StartNew(() => MarkAnArray(arraySize, prime, nextMarkL, array));
            tasks.Add(newTaskL);
        }

        //wait for them to end.
        foreach (var task in tasks)
            task.Wait();
    }

    private static void MarkAnArray(ulong arraySize, ulong prime, ulong nextMark, byte[] array)
    {
        var by = (nextMark - 1) / 16;
        var bi = (int)((nextMark - 1 - by * 16) / 2);
        // add the base number by twice the prime value.(evens are not in the array).which is prime bits down since the array only contains odds.
        var byInc = prime / 8;
        var biInc = (int)(prime - byInc * 8);

        while (by < arraySize)
        {
            var bib = (byte)(1 << bi);

            if ((array[by] & bib) == 0) array[by] |= bib;
            by += byInc;
            bi += biInc;
            if (bi >= 8)
            {
                by++;
                bi -= 8;
            }
        }
    }
}