using System.Text;

namespace ArrayPrimes2022;

public class GapReport
{
    private readonly int[] _gapFound = new int[2000];
    private readonly int[,] _gapRepeatFound = new int[8,2000];
    private readonly int[,] _gapGrid = new int[1000, 1000];
    //public ulong Gap { get; }
    private readonly ulong _minDescribeGap;

    private readonly DateTime _startTime = DateTime.Now;
    private ulong _lastPrime;
    private ulong _lastGap;
    private int _gapRepeat;
    public GapReport(ulong lastPrime)
    {
        //Gap = gap;
        _lastGap = 0;
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

        //int gap;
        //if (ulongGap < Int32.MaxValue)
        //    gap = (int)ulongGap;
        //else
        //{
        //    gapFile.WriteLine("XXX Gap,{0},Primes,{1},{2},{3}", ulongGap, prime, _lastPrime, (DateTime.Now - _startTime).TotalSeconds);
        //    _lastPrime = prime;
        //    return;
        //}

        if (_lastGap == ulongGap)
        {
            _gapRepeat++;
        }
        else
        {
            _gapRepeat = 1;
        }

        if ((int)ulongGap >= _gapFound.Length - 1)
        {
            gapFile.WriteLine("SuperGap,{0},Primes,{1},{2},{3}", ulongGap, prime, _lastPrime, (DateTime.Now - _startTime).TotalSeconds);
        }
        else
        {
            if (_gapRepeat > 1 && _gapRepeatFound[_gapRepeat, ulongGap] == 0)
            {
                gapFile.WriteLine("1st Rep,{0},{1},{2},{3},{4}",_gapRepeat, ulongGap, prime, _lastPrime, (DateTime.Now - _startTime).TotalSeconds);
                _gapRepeatFound[_gapRepeat, ulongGap]++;
            }
            if (ulongGap > _minDescribeGap && _gapFound[ulongGap] == 0)
            {
                gapFile.WriteLine("1st Gap,{0},Primes,{1},{2},{3}", ulongGap, prime, _lastPrime, (DateTime.Now - _startTime).TotalSeconds);
            }
            _gapFound[ulongGap]++;
        }
        _lastPrime = prime;
        if (_lastGap > 0)
            _gapGrid[_lastGap / 2, ulongGap / 2]++;
        _lastGap = ulongGap;
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

        var maxJ = 0;
        var maxK = 0;
        for (var j = 0; j < _gapGrid.GetLength(0); j++)
        {
            for (var k = 0; k < _gapGrid.GetLength(1); k++)
            {
                if (_gapGrid[j,k] > 0)
                {
                    if (j > maxJ) maxJ = j;
                    if (k > maxK) maxK = k;
                }
            }
        }

        var grid = new StringBuilder("**** ");
        for (var k = 0; k <= maxK; k++)
        {
            grid.Append($"{(2*k).ToString().PadLeft(7,' ')} ");
        }

        grid.Append(Environment.NewLine);

        for (var j = 0; j <= maxJ; j++)
        {
            grid.Append($"{(2*j).ToString().PadLeft(4, ' ')} ");

            for (var k = 0; k <= maxK; k++)
            {
                grid.Append($"{_gapGrid[j, k].ToString().PadLeft(7, ' ')} ");
            }
            grid.Append(Environment.NewLine);
        }

        gapsFile.Write(grid.ToString());

        gapsFile.Flush();
    }
}