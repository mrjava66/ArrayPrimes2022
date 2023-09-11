namespace ArrayPrimes2022;

public class GapReport
{
    private readonly int[] _gapFound = new int[2000];
    //public ulong Gap { get; }
    private readonly int _minDescribeGap;

    private readonly DateTime _startTime = DateTime.Now;
    private ulong _lastPrime;
    public GapReport(ulong lastPrime)
    {
        //Gap = gap;
        _lastPrime = lastPrime;
        _minDescribeGap = 0;
        /*
        //if (lastPrime > 1202442089) _minDescribeGap = 252;
        if (lastPrime > 30827138509) _minDescribeGap = 332;
        if (lastPrime > 156798792223) _minDescribeGap = 386;
        if (lastPrime > 280974865361) _minDescribeGap = 420;
        if (lastPrime > 5371284217763) _minDescribeGap = 534;
        if (lastPrime > 20767330530329) _minDescribeGap = 606;
        if (lastPrime > 143679495784681) _minDescribeGap = 706;
        */
    }

    public void LastPrime(ulong lastPrime, TextWriter gapFile)
    {
        gapFile.WriteLine("LastPrime,0,Primes,{0},{0},{1}", lastPrime, (DateTime.Now - _startTime).TotalSeconds);
    }

    public void LoudReportGap(TextWriter gapFile, string extra)
    {
        gapFile.WriteLine("1st Gap,0,Primes,0,{1},{0}", (DateTime.Now - _startTime).TotalSeconds, extra);
    }
    public void ReportGap(ulong prime, TextWriter gapFile)
    {
        var ulongGap = (prime - _lastPrime);

        if (ulongGap > 1000)
        {
            Console.WriteLine($"Special Gap {ulongGap}");
        }

        int gap;
        if (ulongGap < Int32.MaxValue)
            gap = (int)ulongGap;
        else
        {
            gapFile.WriteLine("XXX Gap,{0},Primes,{1},{2},{3}", ulongGap, prime, _lastPrime, (DateTime.Now - _startTime).TotalSeconds);
            _lastPrime = prime;
            return;
        }

        if (gap >= _gapFound.Length - 1)
        {
            gapFile.WriteLine("SuperGap,{0},Primes,{1},{2},{3}", gap, prime, _lastPrime, (DateTime.Now - _startTime).TotalSeconds);
        }
        else
        {
            if (gap > _minDescribeGap && _gapFound[gap] == 0)
            {
                gapFile.WriteLine("1st Gap,{0},Primes,{1},{2},{3}", gap, prime, _lastPrime, (DateTime.Now - _startTime).TotalSeconds);
            }
            _gapFound[gap]++;
        }
        _lastPrime = prime;
    }

    public void ReportGaps(TextWriter gapsFile)
    {
        for (var i = 0; i < _gapFound.Length; i++)
        {
            if (_gapFound[i] != 0)
            {
                gapsFile.WriteLine("Gap,{0},Found,{1}", i, _gapFound[i]);
            }
        }
        gapsFile.Flush();
    }
}