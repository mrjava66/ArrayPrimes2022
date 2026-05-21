using System.Text;

namespace ArrayPrimes2022;

/// <summary>
/// Carries gap-tracking state across segment boundaries so that consecutive
/// <see cref="GapReport"/> instances (one per 2^32-wide segment) form a
/// continuous gap history over the full number line.
/// </summary>
public class GapReportCarryState
{
    /// <summary>How many times the current gap size has repeated consecutively.</summary>
    public int GapRepeat { get; set; }
    /// <summary>The gap between the two most recently seen primes.</summary>
    public ulong LastGap { get; set; }
    /// <summary>The most recently seen prime value.</summary>
    public ulong LastPrime { get; set; }
}

/// <summary>
/// Collects prime-gap statistics for one sieve segment and formats them for
/// output to a GapPrimes / GapArray log file pair.
/// <para>
/// Three tracking planes are maintained in <c>_gapFound[row, gapSize]</c>:
/// <list type="bullet">
///   <item><description>Row 0 — first-occurrence count for each gap size ("1st Gap").</description></item>
///   <item><description>Row 1 — first occurrence of each consecutive-gap sum ("Sum Lon").</description></item>
///   <item><description>Row 2 — first occurrence of each min-of-consecutive-gaps ("DistLon").</description></item>
/// </list>
/// </para>
/// </summary>
public class GapReport(GapReportCarryState gapReportCarryState)
{
    // Accumulates all log lines for the segment; written in one shot by WriteLastPrimeEntry.
    private readonly StringBuilder _gapFileBuilder = new();
    // [row, gapSize]: row 0 = first-occurrence counts, row 1 = sum-lonely, row 2 = dist-lonely.
    private readonly int[,] _gapFound = new int[3, 2000];
    // 2-D grid of (prevGap/2, curGap/2) pair counts; written by WriteGapGrid when BigArray is enabled.
    private readonly int[,] _gapGrid = new int[780, 780];
    // [repeatCount, gapSize]: records the first time a gap repeats n times in a row.
    private readonly int[,] _gapRepeatFound = new int[11, 2000];

    // Wall-clock time when this instance was created; used to timestamp log entries.
    private readonly DateTime _startTime = DateTime.Now;
    private int _gapRepeat = gapReportCarryState.GapRepeat;
    private ulong _lastGap = gapReportCarryState.LastGap;
    private ulong _lastPrimeNum = gapReportCarryState.LastPrime;
    private ulong _primeCount;

    /// <summary>
    /// Returns the carry state needed to initialise the next segment's <see cref="GapReport"/>.
    /// </summary>
    public GapReportCarryState State => new() { GapRepeat = _gapRepeat, LastGap = _lastGap, LastPrime = _lastPrimeNum };

    /// <summary>
    /// Appends the final "LastPrime" log line (prime count, value, and elapsed seconds) to
    /// the internal builder, then flushes the entire accumulated log to <paramref name="gapFile"/>.
    /// Call this once per segment after the last <see cref="RecordPrime"/> call.
    /// </summary>
    public void WriteLastPrimeEntry(ulong lastPrime, TextWriter gapFile)
    {
        _gapFileBuilder.AppendLine(
            $"LastPrime,{_primeCount},Primes,{lastPrime},{lastPrime},{(DateTime.Now - _startTime).TotalSeconds}");
        gapFile.Write(_gapFileBuilder.ToString());
        gapFile.Flush();
    }

    /// <summary>
    /// Appends a "Not Gap" timing checkpoint to the log builder. Used to measure how long
    /// each phase of the sieve (anvil copy, sieve pass, etc.) takes within a segment.
    /// </summary>
    /// <param name="extra">A label identifying the checkpoint, e.g. "AfterBlockCopy".</param>
    public void AppendTimingMark(string extra)
    {
        _gapFileBuilder.AppendLine($"Not Gap,0,Primes,0,{extra},{(DateTime.Now - _startTime).TotalSeconds}");
    }

    /// <summary>
    /// Returns the elapsed seconds since this instance was created, computing and caching
    /// the value in <paramref name="totalSeconds"/> on the first call. Currently
    /// short-circuits to 0 for performance; un-comment the body to restore live timing.
    /// </summary>
    //calc this at most once per RecordPrime call.
    private double GetElapsedSeconds(ref double totalSeconds)
    {
        return 0;
#pragma warning disable CS0162 // Unreachable code detected
        if (totalSeconds > 0)
            return totalSeconds;
        totalSeconds = (DateTime.Now - _startTime).TotalSeconds;
        return totalSeconds;
#pragma warning restore CS0162 // Unreachable code detected
    }

    /// <summary>
    /// Processes one prime value: computes the gap from the previous prime, increments the
    /// prime count, and appends log lines for any first occurrences of:
    /// <list type="bullet">
    ///   <item><description>"SuperGap" — gap too large for the tracking arrays.</description></item>
    ///   <item><description>"1st Rep" — gap repeated n times in a row for the first time.</description></item>
    ///   <item><description>"1st Gap" — this gap size seen for the first time.</description></item>
    ///   <item><description>"Sum Lon" — consecutive-gap sum seen for the first time.</description></item>
    ///   <item><description>"DistLon" — min of the two consecutive gaps, seen for the first time.</description></item>
    /// </list>
    /// Also increments the 2-D gap-pair grid when <see cref="ProgramClass.BigArray"/> is enabled.
    /// </summary>
    public void RecordPrime(ulong prime)
    {
        _primeCount++;

        var ulongGap = prime - _lastPrimeNum;

        // Track consecutive repetitions of the same gap size.
        if (_lastGap == ulongGap)
            _gapRepeat++;
        else
            _gapRepeat = 1;

        double totalSeconds = 0;

        if ((int)ulongGap >= _gapFound.Length - 1)
        {
            // Gap is too large to fit in the fixed-size tracking arrays; log it separately.
            _gapFileBuilder.AppendLine(
                $"SuperGap,{ulongGap},Primes,{prime},{_lastPrimeNum},{GetElapsedSeconds(ref totalSeconds)}");
        }
        else
        {
            // Log the first time this gap size repeats n times consecutively.
            if (_gapRepeat > 1 && _gapRepeatFound[_gapRepeat, ulongGap] == 0)
            {
                _gapFileBuilder.AppendLine(
                    $"1st Rep,{_gapRepeat},{ulongGap},{prime},{_lastPrimeNum},{GetElapsedSeconds(ref totalSeconds)}");
                _gapRepeatFound[_gapRepeat, ulongGap]++;
            }

            // Log the first occurrence of this gap size.
            if (_gapFound[0, ulongGap] == 0)
                _gapFileBuilder.AppendLine(
                    $"1st Gap,{ulongGap},Primes,{prime},{_lastPrimeNum},{GetElapsedSeconds(ref totalSeconds)}");
            _gapFound[0, ulongGap]++;
        }

        // Log the first occurrence of this consecutive-gap sum (lonely-sum metric).
        if (_gapFound[1, _lastGap + ulongGap] == 0)
        {
            _gapFound[1, _lastGap + ulongGap]++;
            _gapFileBuilder.AppendLine(
                $"Sum Lon,{_lastGap + ulongGap},Primes,{_lastPrimeNum},{_lastPrimeNum},{GetElapsedSeconds(ref totalSeconds)}");
        }

        // Log the first occurrence of the smaller of two consecutive gaps (lonely-distance metric).
        var minDistLonely = _lastGap > ulongGap ? ulongGap : _lastGap;
        if (_gapFound[2, minDistLonely] == 0)
        {
            _gapFound[2, minDistLonely]++;
            _gapFileBuilder.AppendLine(
                $"DistLon,{minDistLonely},Primes,{_lastPrimeNum},{_lastPrimeNum},{GetElapsedSeconds(ref totalSeconds)}");
        }

        // Record the (prevGap, curGap) pair in the 2-D grid for the big-array report.
        if (ProgramClass.BigArray)
            if (_lastGap > 0)
                _gapGrid[_lastGap / 2, ulongGap / 2]++;

        _lastGap = ulongGap;
        _lastPrimeNum = prime;
    }

    /// <summary>
    /// Writes the per-gap-size occurrence counts ("Gap,size,Found,count") to
    /// <paramref name="gapsFile"/>, followed by the 2-D gap-pair grid when
    /// <see cref="ProgramClass.BigArray"/> is enabled.
    /// </summary>
    public void WriteGapSummary(TextWriter gapsFile)
    {
        var gaps = new StringBuilder();
        for (var i = 0; i < _gapFound.GetLength(1); i++)
            if (_gapFound[0, i] != 0)
                gaps.AppendLine($"Gap,{i},Found,{_gapFound[0, i]}");
        gapsFile.Write(gaps.ToString());

        if (ProgramClass.BigArray)
            WriteGapGrid(gapsFile, _gapGrid, true);

        gapsFile.Flush();
    }

    /// <summary>
    /// Formats <paramref name="gridArray"/> as a fixed-width text table and writes it to
    /// <paramref name="gapsFile"/>. Column headers are multiplied by 2 when
    /// <paramref name="doubleTop"/> is <see langword="true"/> (used when the grid stores
    /// half-gap indices that must be doubled to recover actual gap sizes).
    /// </summary>
    public static void WriteGapGrid(TextWriter gapsFile, int[,] gridArray, bool doubleTop)
    {
        var maxJ = 0;
        var maxK = 0;
        for (var j = 0; j < gridArray.GetLength(0); j++)
        for (var k = 0; k < gridArray.GetLength(1); k++)
            if (gridArray[j, k] > 0)
            {
                if (j > maxJ) maxJ = j;
                if (k > maxK) maxK = k;
            }

        var grid = new StringBuilder("**** ");
        var mul = doubleTop ? 2 : 1;
        for (var k = 0; k <= maxK; k++) grid.Append($"{(mul * k).ToString().PadLeft(7, ' ')} ");

        grid.Append(Environment.NewLine);

        for (var j = 0; j <= maxJ; j++)
        {
            grid.Append($"{(2 * j).ToString().PadLeft(4, ' ')} ");

            for (var k = 0; k <= maxK; k++) grid.Append($"{gridArray[j, k].ToString().PadLeft(7, ' ')} ");
            grid.Append(Environment.NewLine);
        }

        gapsFile.Write(grid.ToString());
    }
}