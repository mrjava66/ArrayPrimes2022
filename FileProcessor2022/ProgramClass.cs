using System.Configuration;

namespace FileProcessor2022;

internal static class ProgramClass
{
    private const decimal SecondsPerDay = 3600 * 24;

    private static readonly Dictionary<uint, GapRowFormat> allGapRows = new();
    private static readonly Dictionary<(int, uint), RepRowFormat> allRepRows = new();
    private static readonly Dictionary<(string?, uint), RowFormat> allRows = new();
    private static readonly List<RowFormat> allLastPrimeRows = new();

    public static void MainProgram()
    {
        try
        {
            var startTime = DateTime.Now;

            var filesTasksString = ConfigurationManager.AppSettings["FileTasks"] ?? "32";
            if (!int.TryParse(filesTasksString, out var filesTasks)) filesTasks = 32;

            var folder = ConfigurationManager.AppSettings["Folder"];
            var fileMask = ConfigurationManager.AppSettings["FileMask"];
            if (string.IsNullOrWhiteSpace(folder))
                throw new Exception("Must define a folder");
            if (string.IsNullOrWhiteSpace(fileMask))
                throw new Exception("Must define a file mask");

            var so = SearchOption.TopDirectoryOnly;
            try
            {
                var opts = ConfigurationManager.AppSettings["SubDirectories"]?.ToUpper() ?? "";
                if (opts.StartsWith("Y") || opts.StartsWith("1") || opts.StartsWith("T"))
                    so = SearchOption.AllDirectories;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            var files = Directory.GetFiles(folder, fileMask, so).OrderBy(f => f.Length).ThenBy(f => f).ToArray();

            decimal totalTime = 0;

            var tasks = new List<Task<(List<GapRowFormat>, List<RepRowFormat>, List<RowFormat>)>>();
            foreach (var file in files)
                try
                {
                    var task = Task.Factory.StartNew(() => ProcessFileLines(file));
                    //task.Wait();
                    tasks.Add(task);

                    ManageTasks(tasks, filesTasks, ref totalTime);
                }
                catch (Exception? ex)
                {
                    Console.Error.WriteLine("With file='" + file + "'");
                    while (ex != null)
                    {
                        Console.Error.WriteLine(ex);
                        ex = ex.InnerException;
                    }

                    Console.Error.WriteLine("Continuing to the next file.");
                }

            ManageTasks(tasks, 0, ref totalTime);

            ulong lastStartPrime = 0;
            foreach (var val in allLastPrimeRows.OrderBy(o => ulong.MaxValue - o.StartPrime))
            {
                if (val.StartPrime == lastStartPrime)
                    continue;
                lastStartPrime = val.StartPrime;
                var endPrime = val.StartPrime != val.EndPrime ? val.EndPrime.ToString() : "";
                Console.WriteLine($"{val.GapType},{val.GapSize},{val.StartPrime},{endPrime}");
            }

            /*
odd-fix

%6!=0 not repeatable
6,12,18,24,30 already seen to max
review.
*/

            foreach (var key in allRows.Keys.OrderBy(num => num.Item1).ThenBy(num => num.Item2))
            {
                var didGet = allRows.TryGetValue(key, out var val);
                if (didGet && val is not null)
                {
                    var endPrime = val.StartPrime != val.EndPrime ? val.EndPrime.ToString() : "";
                    Console.WriteLine($"{val.GapType},{val.GapSize},{val.StartPrime},{endPrime}");
                }
            }

            var gapSizeRepeat = new Dictionary<int, uint>
                { { 2, 6 }, { 3, 6 }, { 4, 30 }, { 5, 30 }, { 6, 210 }, { 7, 210 }, { 8, 210 }, { 9, 210 } };
            uint lastGapSize = 0;
            foreach (var keyTuple in allRepRows.Keys.OrderBy(num => num.Item1 * 2000 + num.Item2))
            {
                var didGet = allRepRows.TryGetValue(keyTuple, out var val);
                if (didGet && val is not null)
                {
                    var didGetGsr = gapSizeRepeat.TryGetValue(val.Repeat, out var spaceValue);
                    while (didGetGsr && lastGapSize + spaceValue < val.GapSize)
                    {
                        lastGapSize += spaceValue;
                        Console.WriteLine($"1st Rep,{val.Repeat},{lastGapSize},{0},{0}");
                    }

                    var startPrime = val.EndPrime - (ulong)(val.Repeat * val.GapSize);

                    Console.WriteLine($"1st Rep,{val.Repeat},{val.GapSize},{startPrime},{val.EndPrime}");
                }

                lastGapSize = val.GapSize;
            }

            var firstRows = new List<GapRowFormat>();

            foreach (var rowX in allGapRows)
            {
                var row = rowX.Value;
                //if (row.GapSize % 2 == 1) continue; // not interested in odds.(beginning and end gaps)
                //if (allRows.Exists(x => x.GapSize == row.GapSize && x.StartPrime < row.StartPrime)) continue; // not interested if a better one was found.
                //if (firstRows.Exists(x => x.GapSize == row.GapSize && x.StartPrime == row.StartPrime)) continue; //duplicate
                firstRows.Add(row);
            }

            // sort the gaps by start prime, and find the max gaps.
            uint gapSize = 0;

            foreach (var row in firstRows.OrderBy(o => o.StartPrime))
                if (row.GapSize > gapSize)
                {
                    gapSize = row.GapSize;
                    row.GapType = "Max Gap";
                }
                else
                {
                    row.GapType = "1st Gap";
                }

            //calculate tails.
            ulong maxStartPrime = 0;
            foreach (var row in firstRows.OrderBy(o => o.GapSize))
                if (maxStartPrime > row.StartPrime)
                {
                    row.Tail = false;
                }
                else
                {
                    maxStartPrime = row.StartPrime;
                    row.Tail = true;
                }

            uint lastGap = 0;
            var canTail = true;
            // output the results.
            foreach (var row in firstRows.OrderBy(o => o.GapSize))
            {
                while (row.GapSize > lastGap + 2)
                {
                    canTail = false;
                    lastGap += 2;
                    Console.WriteLine($"no gap,{lastGap},0,0");
                }

                Console.WriteLine("{0},{1},{2},{3}{4}", row.GapType, row.GapSize, row.StartPrime, row.EndPrime,
                    canTail && row.Tail ? ",Tail" : "");
                lastGap = row.GapSize;
            }

            var endTime = DateTime.Now;
            Console.WriteLine("FP Runtime " + (endTime - startTime).TotalSeconds.ToString("#.##") + " seconds.");
            Console.WriteLine("AP Runtime " + (totalTime / SecondsPerDay).ToString("#.##") + " cpu-days.");
        }
        catch (Exception? ex)
        {
            while (ex != null)
            {
                Console.Error.WriteLine(ex);
                ex = ex.InnerException;
            }
        }
    }

    private static void ManageTasks(List<Task<(List<GapRowFormat>, List<RepRowFormat>, List<RowFormat>)>> tasks,
        int limitTasks, ref decimal totalTime)
    {
        while (tasks.Count > limitTasks)
        {
            var task = tasks.First();
            tasks.Remove(task);

            task.Wait();
            var (gapRows, repRows, rows) = task.Result;
            AppendAllRepRows(repRows);

            var lastWhen = AppendOtherRows(gapRows, rows);

            totalTime += lastWhen;
        }
    }

    private static decimal AppendOtherRows(List<GapRowFormat> gapRows, List<RowFormat> rows)
    {
        uint oddGaps = 0;
        foreach (var row in gapRows)
        {
            if (row.GapSize % 2 == 1)
                if (row.StartPrime != 2)
                {
                    oddGaps++;
                    continue;
                }
            // not interested in odds.(beginning and end gaps)

            var didGet = allGapRows.TryGetValue(row.GapSize, out var cr);
            if (!didGet || cr is null)
            {
                //we have not seen a gap of this size before, add it to the list.
                allGapRows.Add(row.GapSize, row);
            }
            else if (cr.StartPrime > row.StartPrime)
            {
                //we have seen a gap of this size before, but it was at a higher value.
                //remove the old one, and put in the new one.
                allGapRows.Remove(row.GapSize);
                allGapRows.Add(row.GapSize, row);
            }
        }

        decimal lastWhen = 0;
        foreach (var row in rows)
        {
            if (row.GapType == "LastPrime")
            {
                row.GapSize -= oddGaps;
                lastWhen = row.When;
                allLastPrimeRows.Add(row);
                continue;
            }

            if (row.GapSize % 2 == 1)
                continue; // not interested in odds.(beginning and end gaps)
            var didGet = allRows.TryGetValue((row.GapType, row.GapSize), out var rep);
            if (!didGet || rep is null)
            {
                allRows.Add((row.GapType, row.GapSize), row);
            }
            else if (rep.StartPrime > row.StartPrime)
            {
                allRows.Remove((row.GapType, row.GapSize));
                allRows.Add((row.GapType, row.GapSize), row);
            }
        }

        return lastWhen;
    }

    private static void AppendAllRepRows(List<RepRowFormat> repRows)
    {
        foreach (var repRow in repRows)
        {
            var didGet = allRepRows.TryGetValue((repRow.Repeat, repRow.GapSize), out var rep);
            if (!didGet || rep is null)
            {
                allRepRows.Add((repRow.Repeat, repRow.GapSize), repRow);
            }
            else if (rep.StartPrime > repRow.StartPrime)
            {
                allRepRows.Remove((repRow.Repeat, repRow.GapSize));
                allRepRows.Add((repRow.Repeat, repRow.GapSize), repRow);
            }
        }
    }

    private static (List<GapRowFormat>, List<RepRowFormat>, List<RowFormat>) ProcessFileLines(string file)
    {
        //Console.WriteLine("Reading From '" + file + "'");
        var gapRows = new List<GapRowFormat>();
        var repRows = new List<RepRowFormat>();
        var rows = new List<RowFormat>();
        foreach (var row in File.ReadAllLines(file))
            try
            {
                var split = row.Split(',');
                if (split.Length < 5)
                    continue;
                var gapType = split[0];
                if (gapType == "1st Gap")
                {
                    var rowFormat = new GapRowFormat
                    {
                        GapType = split[0],
                        GapSize = uint.TryParse(split[1], out var lineGapSize) ? lineGapSize : 0,
                        //Primes = split[2],
                        EndPrime = ulong.TryParse(split[3], out var endPrime) ? endPrime : 0,
                        StartPrime = ulong.TryParse(split[4], out var startPrime) ? startPrime : 0,
                        When = decimal.TryParse(split[5], out var when) ? when : 0
                    };
                    gapRows.Add(rowFormat);
                }
                else if (gapType == "1st Rep")
                {
                    var repFormat = new RepRowFormat
                    {
                        GapType = split[0],
                        Repeat = int.TryParse(split[1], out var repeat) ? repeat : 0,
                        GapSize = uint.TryParse(split[2], out var lineGapSize) ? lineGapSize : 0,
                        EndPrime = ulong.TryParse(split[3], out var endPrime) ? endPrime : 0,
                        StartPrime = ulong.TryParse(split[4], out var startPrime) ? startPrime : 0,
                        When = decimal.TryParse(split[5], out var when) ? when : 0
                    };
                    repRows.Add(repFormat);
                }
                else
                {
                    var rowFormat = new RowFormat
                    {
                        GapType = split[0],
                        GapSize = uint.TryParse(split[1], out var lineGapSize) ? lineGapSize : 0,
                        EndPrime = ulong.TryParse(split[3], out var endPrime) ? endPrime : 0,
                        StartPrime = ulong.TryParse(split[4], out var startPrime) ? startPrime : 0,
                        When = decimal.TryParse(split[5], out var when) ? when : 0
                    };
                    if (rowFormat.GapType == "LastPrime")
                        if (rowFormat.GapSize > rowFormat.EndPrime || rowFormat.GapSize == 0)
                            Console.WriteLine("Interpret Problem");
                    rows.Add(rowFormat);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }

        return (gapRows, repRows, rows);
    }
}