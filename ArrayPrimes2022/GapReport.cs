using System.Text;

namespace ArrayPrimes2022;

public class GapReport
{
    private readonly int[] _gapFound = new int[2000];
    private readonly int[,] _gapGrid = new int[780, 780];
    private readonly int[,] _gapRepeatFound = new int[11, 2000];

    //public ulong Gap { get; }
    private readonly ulong _minDescribeGap;

    private readonly DateTime _startTime = DateTime.Now;
    private int _gapRepeat;
    private ulong _lastGap;
    private ulong _lastPrime;
    private readonly StringBuilder _gapFileBuilder;

    public GapReport(ulong lastPrime)
    {
        //Gap = gap;
        _lastGap = 0;
        _lastPrime = lastPrime;
        _minDescribeGap = 0;
        _gapFileBuilder = new StringBuilder();
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
        var str = string.Format("LastPrime,0,Primes,{0},{0},{1}", lastPrime, (DateTime.Now - _startTime).TotalSeconds);
        _gapFileBuilder.AppendLine(str);
        WriteFlush(gapFile);
    }

    public void WriteFlush(TextWriter gapFile)
    {
        gapFile.Write(_gapFileBuilder.ToString());
        gapFile.Flush();
    }

    public void LoudReportGap(string extra)
    {
        _gapFileBuilder.AppendLine($"Not Gap,0,Primes,0,{extra},{(DateTime.Now - _startTime).TotalSeconds}");
    }

    public void ReportGap(ulong prime)
    {
        var ulongGap = prime - _lastPrime;

        if (ulongGap > 1000) Console.WriteLine($"Special Gap {ulongGap}");

        if (_lastGap == ulongGap)
            _gapRepeat++;
        else
            _gapRepeat = 1;

        if ((int)ulongGap >= _gapFound.Length - 1)
        {
            _gapFileBuilder.AppendLine($"SuperGap,{ulongGap},Primes,{prime},{_lastPrime},{(DateTime.Now - _startTime).TotalSeconds}");
        }
        else
        {
            if (_gapRepeat > 1 && _gapRepeatFound[_gapRepeat, ulongGap] == 0)
            {
                _gapFileBuilder.AppendLine($"1st Rep,{_gapRepeat},{ulongGap},{prime},{_lastPrime},{(DateTime.Now - _startTime).TotalSeconds}");
                _gapRepeatFound[_gapRepeat, ulongGap]++;
            }

            if (ulongGap > _minDescribeGap && _gapFound[ulongGap] == 0)
                _gapFileBuilder.AppendLine($"1st Gap,{ulongGap},Primes,{prime},{_lastPrime},{(DateTime.Now - _startTime).TotalSeconds}");
            _gapFound[ulongGap]++;
        }

        _lastPrime = prime;
        if (_lastGap > 0)
            _gapGrid[_lastGap / 2, ulongGap / 2]++;
        _lastGap = ulongGap;
    }

    public void ReportGaps(TextWriter gapsFile)
    {
        var gaps = new StringBuilder();
        for (var i = 0; i < _gapFound.Length; i++)
            if (_gapFound[i] != 0)
                gaps.AppendLine($"Gap,{i},Found,{_gapFound[i]}");
        gapsFile.Write(gaps.ToString());

        var maxJ = 0;
        var maxK = 0;
        for (var j = 0; j < _gapGrid.GetLength(0); j++)
        for (var k = 0; k < _gapGrid.GetLength(1); k++)
            if (_gapGrid[j, k] > 0)
            {
                if (j > maxJ) maxJ = j;
                if (k > maxK) maxK = k;
            }

        var grid = new StringBuilder("**** ");
        for (var k = 0; k <= maxK; k++) grid.Append($"{(2 * k).ToString().PadLeft(7, ' ')} ");

        grid.Append(Environment.NewLine);

        for (var j = 0; j <= maxJ; j++)
        {
            grid.Append($"{(2 * j).ToString().PadLeft(4, ' ')} ");

            for (var k = 0; k <= maxK; k++) grid.Append($"{_gapGrid[j, k].ToString().PadLeft(7, ' ')} ");
            grid.Append(Environment.NewLine);
        }

        gapsFile.Write(grid.ToString());

        gapsFile.Flush();
    }
}