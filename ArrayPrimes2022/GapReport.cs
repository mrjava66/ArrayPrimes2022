﻿using System.Text;

namespace ArrayPrimes2022;

public class GapReport
{
    private readonly StringBuilder _gapFileBuilder;
    private readonly int[,] _gapFound = new int[3, 2000];
    private readonly int[,] _gapGrid = new int[780, 780];
    private readonly int[,] _gapRepeatFound = new int[11, 2000];

    private readonly DateTime _startTime = DateTime.Now;
    private int _gapRepeat;
    private ulong _lastGap;
    private ulong _minDistLon;
    private ulong _minSumLon;
    private ulong _primeCount;

    public GapReport(ulong lastPrime)
    {
        _lastGap = 0;
        LastPrimeNum = lastPrime;
        _gapFileBuilder = new StringBuilder();
        switch (lastPrime)
        {
            case > 47244640237:
                _minDistLon = 170;
                _minSumLon = 398;
                break;

            case > 4294967296:
                _minDistLon = 160;
                _minSumLon = 334;
                break;

            default:
                _minDistLon = 0;
                _minSumLon = 0;
                break;
        }
    }

    public ulong LastPrimeNum { get; private set; }

    public void LastPrime(ulong lastPrime, TextWriter gapFile)
    {
        _gapFileBuilder.AppendLine(
            $"LastPrime,{_primeCount},Primes,{lastPrime},{lastPrime},{(DateTime.Now - _startTime).TotalSeconds}");
        gapFile.Write(_gapFileBuilder.ToString());
        gapFile.Flush();
    }

    public void LoudReportGap(string extra)
    {
        _gapFileBuilder.AppendLine($"Not Gap,0,Primes,0,{extra},{(DateTime.Now - _startTime).TotalSeconds}");
    }

    //calc this at most once per reportGap.
    private double TotalSeconds(ref double totalSeconds)
    {
        return 0;
        if (totalSeconds > 0)
            return totalSeconds;
        totalSeconds = (DateTime.Now - _startTime).TotalSeconds;
        return totalSeconds;
    }

    public void ReportGap(ulong prime)
    {
        _primeCount++;

        var ulongGap = prime - LastPrimeNum;

        //if (ulongGap > 1000) Console.WriteLine($"Special Gap {ulongGap}");

        if (_lastGap == ulongGap)
            _gapRepeat++;
        else
            _gapRepeat = 1;

        double totalSeconds = 0;

        if ((int)ulongGap >= _gapFound.Length - 1)
        {
            _gapFileBuilder.AppendLine(
                $"SuperGap,{ulongGap},Primes,{prime},{LastPrimeNum},{TotalSeconds(ref totalSeconds)}");
        }
        else
        {
            if (_gapRepeat > 1 && _gapRepeatFound[_gapRepeat, ulongGap] == 0)
            {
                _gapFileBuilder.AppendLine(
                    $"1st Rep,{_gapRepeat},{ulongGap},{prime},{LastPrimeNum},{TotalSeconds(ref totalSeconds)}");
                _gapRepeatFound[_gapRepeat, ulongGap]++;
            }

            if (_gapFound[0, ulongGap] == 0)
                _gapFileBuilder.AppendLine(
                    $"1st Gap,{ulongGap},Primes,{prime},{LastPrimeNum},{TotalSeconds(ref totalSeconds)}");
            _gapFound[0, ulongGap]++;
        }

        if (_gapFound[1, _lastGap + ulongGap] == 0)
        {
            _gapFound[1, _lastGap + ulongGap]++;
            _gapFileBuilder.AppendLine(
                $"Sum Lon,{_lastGap + ulongGap},Primes,{LastPrimeNum},{LastPrimeNum},{TotalSeconds(ref totalSeconds)}");
        }

        var minDistLonely = _lastGap > ulongGap ? ulongGap : _lastGap;
        if (_gapFound[2, minDistLonely] == 0)
        {
            _gapFound[2, minDistLonely]++;
            _gapFileBuilder.AppendLine(
                $"DistLon,{minDistLonely},Primes,{LastPrimeNum},{LastPrimeNum},{TotalSeconds(ref totalSeconds)}");
        }

        if (_lastGap > 0)
            _gapGrid[_lastGap / 2, ulongGap / 2]++;

        _lastGap = ulongGap;

        LastPrimeNum = prime;
    }

    public void ReportGaps(TextWriter gapsFile)
    {
        var gaps = new StringBuilder();
        for (var i = 0; i < _gapFound.GetLength(1); i++)
            if (_gapFound[0, i] != 0)
                gaps.AppendLine($"Gap,{i},Found,{_gapFound[0, i]}");
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