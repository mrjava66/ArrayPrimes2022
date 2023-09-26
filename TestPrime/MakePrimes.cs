namespace TestPrime;

public static class MakePrimes
{
    private static readonly List<ulong> ListAllPrimes = new() { 2, 3, 5 };

    public static ulong[] ArrayAllPrimes => ListAllPrimes.ToArray();
    public static Dictionary<ulong, ulong> DictAllPrimes => ArrayAllPrimes.ToDictionary(x=>x,x=>x);
    public static int NumPrimes => ListAllPrimes.Count;

    public static void MakePrimesTask()
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
            var two28 = Math.Sqrt(ulong.MaxValue);
            foreach (var p in AllPrimes())
                if (p > two28)
                    break;
        }
        catch
        {
            //Console.WriteLine(e);
        }
    }

    public static IEnumerable<ulong> AllPrimes()
    {
        foreach (var prime in ListAllPrimes) yield return prime;
        var i = ListAllPrimes[^1];
        while (true)
        {
            i++;
            var j = Math.Sqrt(i);
            foreach (var k in AllPrimes())
            {
                if (i % k == 0)
                    //if i is evenly divisible by k, this is not a prime, check the next number.
                    break;
                if (k > j)
                {
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
        }
        //there is no last prime.
    }
}