namespace TestPrime;

public class MakePrimesEnum : IMakePrimes
{
    private static readonly List<ulong> ListAllPrimes = [2, 3, 5];

    public ulong[] ArrayAllPrimes => ListAllPrimes.ToArray();
    public Dictionary<ulong, ulong> DictAllPrimes => ArrayAllPrimes.ToDictionary(x => x, x => x);
    public int NumPrimes => ListAllPrimes.Count;

    public void MakePrimesTask()
    {
        try
        {
            var task = Task.Factory.StartNew(GetEnoughPrimes);
            task.Start();
        }
        catch (InvalidOperationException)
        {
            //don't care.
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private static void GetEnoughPrimes()
    {
        try
        {
            var two32 = Math.Sqrt(ulong.MaxValue);
            foreach (var p in AllPrimes())
                if (p > two32)
                    break;
        }
        catch
        {
            //Console.WriteLine(e);
        }
    }

    // ReSharper disable once FunctionRecursiveOnAllPaths
    private static IEnumerable<ulong> AllPrimes()
    {
        foreach (var prime in ListAllPrimes) yield return prime;
        var i = ListAllPrimes[^1];
        var twoFour = ((i + 2) % 3) == 0; //false=add by 2, true=add by 4, for next number to check for prime
        while (true)
        {
            i += twoFour ? (ulong)4 : 2;
            twoFour = !twoFour;
            var j = Math.Sqrt(i);
            foreach (var k in AllPrimes())
            {
                //if var-i is evenly divisible by k, this is not a prime, check the next number.
                if (i % k == 0)
                    break;

                //if this k<sqrt(i), check next prime.
                if (k < j)
                    continue;

                //if none of primes less than the square root of i are evenly divisible, this is a prime.
                try
                {
                    ListAllPrimes.Add(i);
                }
                catch (Exception)
                {
                    yield break;
                    //Console.WriteLine(e);
                }

                yield return i;
                break;
            }
        }
        //there is no last prime.
    }
}