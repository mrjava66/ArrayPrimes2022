﻿using System.Configuration;
using System.Text;

namespace FileProcessor2022;

internal static class ProgramClass
{
    //private const ulong Two32 = (ulong)uint.MaxValue + 1; //read as 2 to the 32
    private const decimal SecondsPerDay = 3600 * 24;

    private static readonly Dictionary<uint, GapRowFormat> AllGapRows = new();
    private static readonly Dictionary<(int, uint), RepRowFormat> AllRepRows = new();
    private static readonly Dictionary<(string?, uint), RowFormat> AllRows = new();
    private static List<RowFormat> _allLastPrimeRows = new();
    public static bool LastPrimeBlocks { get; set; }
    public static bool TwinTotals { get; set; }

    public static void MainProgram()
    {
        try
        {
            var startTime = DateTime.Now;

            var filesTasksString = ConfigurationManager.AppSettings["FileTasks"] ?? "32";
            if (!int.TryParse(filesTasksString, out var filesTasks)) filesTasks = 32;

            var folder = ConfigurationManager.AppSettings["Folder"];
            var fileMask = ConfigurationManager.AppSettings["FileMask"];
            var allFileMask = ConfigurationManager.AppSettings["AllFileMask"] ?? "*.*";

            if (string.IsNullOrWhiteSpace(folder))
                throw new Exception("Must define a folder");
            if (string.IsNullOrWhiteSpace(fileMask))
                throw new Exception("Must define a file mask");

            var showNumPrimesLines = true;
            try
            {
                var showNumPrimesLinesStr = ConfigurationManager.AppSettings["ShowNumPrimesLines"]?.ToUpper() ?? "true";
                var didShow = bool.TryParse(showNumPrimesLinesStr, out var sn);
                if (didShow)
                {
                    showNumPrimesLines = sn;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            var so = SearchOption.TopDirectoryOnly;
            try
            {
                var optString = ConfigurationManager.AppSettings["SubDirectories"]?.ToUpper() ?? "true";
                var didOpts = bool.TryParse(optString, out var opt);
                if (didOpts && opt)
                    so = SearchOption.AllDirectories;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            try
            {
                var lpbString = ConfigurationManager.AppSettings["LastPrimeBlocks"]?.ToUpper() ?? "true";
                var didLpb = bool.TryParse(lpbString, out var lpb);
                if (didLpb)
                    LastPrimeBlocks = lpb;
            }
            catch (Exception e)
            {
                LastPrimeBlocks = false;
                Console.WriteLine(e.Message);
            }

            try
            {
                var lpbString = ConfigurationManager.AppSettings["TwinTotals"]?.ToUpper() ?? "true";
                var didLpb = bool.TryParse(lpbString, out var lpb);
                if (didLpb)
                    TwinTotals = lpb;
            }
            catch (Exception e)
            {
                TwinTotals = false;
                Console.WriteLine(e.Message);
            }

            var files = GetFilesList(folder, allFileMask, so, fileMask);

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

            var lastContinuousCheckMsg = "LastContinuousCheck,0,0,0";
            ulong lastStartPrime = 0;
            ulong lastContinuousCheck = 0;
            ulong blockNumberFix = uint.MaxValue / 2;
            var overBlockGap = (ulong)uint.MaxValue + 4000;
            _allLastPrimeRows = _allLastPrimeRows.OrderBy(o => o.StartPrime).ThenBy(o => o.GapSize).ToList();
            foreach (var val in _allLastPrimeRows)
            {
                if (val.StartPrime == lastStartPrime)
                    continue;

                if (val.StartPrime > lastStartPrime + overBlockGap && lastContinuousCheck == 0)
                {
                    lastContinuousCheck = lastStartPrime;
                    lastContinuousCheckMsg =
                        $"LastContinuousCheck,{lastContinuousCheck},{lastContinuousCheck / uint.MaxValue / 1024},{lastContinuousCheck / uint.MaxValue}";
                    Console.WriteLine(lastContinuousCheckMsg);
                }

                lastStartPrime = val.StartPrime;

                if (LastPrimeBlocks)
                {
                    var block = ((val.StartPrime - blockNumberFix) / uint.MaxValue).ToString();
                    if (val.GapType == "LastPrime" && !showNumPrimesLines)
                    {
                        //don't show.
                    }
                    else
                    {
                        Console.WriteLine($"{block},{LastPrimeGapTypeFix(val.GapType)},{val.GapSize},{val.StartPrime}");
                    }
                }
                else
                {
                    var endPrime = val.StartPrime != val.EndPrime ? val.EndPrime.ToString() : "";
                    Console.WriteLine($"{val.GapType},{val.GapSize},{val.StartPrime},{endPrime}");
                }
            }
            if (showNumPrimesLines)
                Console.WriteLine(lastContinuousCheckMsg);

            var level = 0;
            var levelEnough = 1;
            ulong sum = 0;
            ulong twinSum = 0;
            ulong sixSum = 0;
            ulong thirtySum = 0;
            ulong twoTenSum = 0;
            ulong lastBlock = 0;
            var bits = 33;
            ulong lastPrime = 0;
            var splits2 = new StringBuilder();
            var splits6 = new StringBuilder();
            var splits30 = new StringBuilder();
            var splits210 = new StringBuilder();
            foreach (var val in _allLastPrimeRows.Skip(1))
            {
                var block = (val.StartPrime - blockNumberFix) / uint.MaxValue;
                if (block > lastBlock + 1)
                    break;
                lastBlock = block;

                sum += val.GapSize;
                IncrementTwinTotals(folder, block, lastPrime
                    , splits2, splits6, splits30, splits210
                    , ref twinSum, ref sixSum, ref thirtySum, ref twoTenSum);

                level++;
                if (level == levelEnough)
                {
                    if (TwinTotals)
                    {
                        Console.WriteLine($" Twin,{bits},{twinSum},{level}");
                        twinSum = 0;
                        Console.WriteLine($"  Six,{bits},{sixSum},{level}");
                        sixSum = 0;
                        Console.WriteLine($"Gap30,{bits},{thirtySum},{level}");
                        thirtySum = 0;
                        Console.WriteLine($"Gp210,{bits},{twoTenSum},{level}");
                        twoTenSum = 0;
                    }

                    Console.WriteLine($"Total,{bits},{sum},{level}");
                    bits++;
                    sum = 0;
                    level = 0;
                    levelEnough *= 2;
                }

                lastPrime = val.StartPrime;
            }

            Console.Write(splits2);
            Console.Write(splits6);
            Console.Write(splits30);
            Console.Write(splits210);
            _allLastPrimeRows.Clear();
            GC.Collect();

            OutputSome(lastContinuousCheck, "DistLon", 1);
            OutputSome(lastContinuousCheck, "Sum Lon", 3);

            foreach (var key in AllRows.Keys.OrderBy(num => num.Item1).ThenBy(num => num.Item2))
            {
                if ((key.Item1 ?? "").Contains("DistLon") || (key.Item1 ?? "").Contains("Sum Lon") ||
                    (key.Item1 ?? "").Contains("Not Gap"))
                    continue;
                var didGet = AllRows.TryGetValue(key, out var val);
                if (didGet && val is not null)
                {
                    var endPrime = val.StartPrime != val.EndPrime ? val.EndPrime.ToString() : "";
                    Console.WriteLine(
                        $"{FixGapType(val.GapType, lastContinuousCheck, val.StartPrime)},{val.GapSize},{val.StartPrime},{endPrime}");
                }
            }

            var gapSizeRepeat = new Dictionary<int, uint>
                { { 2, 6 }, { 3, 6 }, { 4, 30 }, { 5, 30 }, { 6, 210 }, { 7, 210 }, { 8, 210 }, { 9, 210 } };
            uint lastGapSize = 0;
            foreach (var keyTuple in AllRepRows.Keys.OrderBy(num => num.Item1 * 2000 + num.Item2))
            {
                var didGet = AllRepRows.TryGetValue(keyTuple, out var val);
                if (didGet && val is not null)
                {
                    var didGetGsr = gapSizeRepeat.TryGetValue(val.Repeat, out var spaceValue);
                    while (didGetGsr && lastGapSize + spaceValue < val.GapSize)
                    {
                        lastGapSize += spaceValue;
                        Console.WriteLine($"No  Rep,{val.Repeat},{lastGapSize},{0},{0}");
                    }

                    var startPrime = val.EndPrime - (ulong)(val.Repeat * val.GapSize);

                    Console.WriteLine(
                        $"{FixGapType("1st Rep", lastContinuousCheck, val.StartPrime)},{val.Repeat},{val.GapSize},{startPrime},{val.EndPrime}");
                }

                lastGapSize = val?.GapSize ?? 0;
            }

            var firstRows = new List<GapRowFormat>();

            foreach (var rowX in AllGapRows)
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

                Console.WriteLine("{0},{1},{2},{3}{4}", FixGapType(row.GapType, lastContinuousCheck, row.StartPrime),
                    row.GapSize, row.StartPrime, row.EndPrime,
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

    private static void IncrementTwinTotals(string folder, ulong block, ulong lastPrime
        , StringBuilder splits2, StringBuilder splits6, StringBuilder splits30, StringBuilder splits210
        , ref ulong twinSum, ref ulong sixSum, ref ulong thirtySum, ref ulong twoTenSum
        )
    {
        if (!TwinTotals)
            return;

        try
        {
            var fileSearch = $"{folder}\\{block / 1024 / 1024}\\{block / 1024}";
            var fileSearchMask = $"GapArray.{block}.*.log";
            var aFile = Directory
                .GetFiles(fileSearch, fileSearchMask, SearchOption.TopDirectoryOnly)
                .MaxBy(x => x);

            if (aFile == null)
            {
                TwinTotals = false;
                return;
            }

            var foundOddGap = false;
            var line2 = "";
            var line6 = "";
            var line30 = "";
            var line210 = "";
            var aFileLines = FileExtension.GetReadAllLines(aFile, 200, false);
            foreach (var aline in aFileLines)
                try
                {
                    var linePieces = aline.Split(',');
                    var did1 = int.TryParse(linePieces[1], out var aGapSize);
                    if (!did1)
                        continue;

                    if (aGapSize % 2 == 1)
                    {
                        foundOddGap = true;
                        continue;
                    }

                    var exit = false;
                    switch (aGapSize)
                    {
                        case 2:
                            line2 = aline;
                            break;

                        case 6:
                            line6 = aline;
                            break;

                        case 30:
                            line30 = aline;
                            break;

                        case 210:
                            line210 = aline;
                            exit = true;
                            break;
                    }

                    if (exit)
                        break;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }

            if (string.IsNullOrWhiteSpace(line2)
                || string.IsNullOrWhiteSpace(line6)
                || string.IsNullOrWhiteSpace(line30)
                || string.IsNullOrWhiteSpace(line210)
                )
            {
                TwinTotals = false;
                return;
            }

            ulong extra2 = 0;
            ulong extra6 = 0;
            ulong extra30 = 0;
            ulong extra210 = 0;
            ulong thisPrime = 0;
            ulong thisGap = 0;
            if (foundOddGap)
            {
                //ulong thisPrime = 0;
                var aFile2 = aFile.Replace("GapArray", "GapPrimes");
                var aFile2Lines = FileExtension.GetReadAllLines(aFile2);
                foreach (var aline2 in aFile2Lines)
                {
                    if (!aline2.StartsWith("1st Gap,"))
                        continue;
                    (thisPrime, thisGap) = SplitAndGet(aline2);
                    break;
                }

                var gap = thisPrime - lastPrime;
                if (thisGap % 2 == 0)
                {
                    //first gap even.
                    //Console.WriteLine($"First Gap Even {block},{aFile2}");
                }
                else if (gap == 2)
                {
                    splits2.AppendLine($"Split Twin Prime,{block},{aFile}");
                    extra2 = 1;
                }
                else if (gap == 6)
                {
                    splits6.AppendLine($"Split Six-Prime ,{block},{aFile}");
                    extra6 = 1;
                }
                else if (gap == 30)
                {
                    splits30.AppendLine($"Split 30-Prime  ,{block},{aFile}");
                    extra30 = 1;
                }
                else if (gap == 210)
                {
                    splits210.AppendLine($"Split210-Prime  ,{block},{aFile}");
                    extra210 = 1;
                }
            }

            SplitAndSum(line2, ref twinSum, extra2);
            SplitAndSum(line6, ref sixSum, extra6);
            SplitAndSum(line30, ref thirtySum, extra30);
            SplitAndSum(line210, ref twoTenSum, extra210);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private static void SplitAndSum(string line, ref ulong sum, ulong extra)
    {
        var lineParts = line.Split(',');
        if (lineParts.Length < 4)
        {
            TwinTotals = false;
        }
        else
        {
            var didNum = ulong.TryParse(lineParts[3], out var num);
            if (!didNum)
                TwinTotals = false;
            else
                sum += num + extra;
        }
    }

    private static (ulong, ulong) SplitAndGet(string line)
    {
        var lineParts = line.Split(',');
        if (lineParts.Length < 4)
            return (0, 0);

        var gapString = lineParts[1];
        var didGap = ulong.TryParse(gapString, out var gap);
        if (!didGap)
            return (0, 0);

        var primeString = lineParts[3];
        var didPrime = ulong.TryParse(primeString, out var prime);
        if (!didPrime)
            return (0, 0);
        return (prime, gap);
    }

    private static void OutputSome(ulong lastContinuousCheck, string type, uint threeSize)
    {
        var typeGroupList = AllRows.Where(kvp => (kvp.Key.Item1 ?? "").Contains(type)).Select(x => x.Value).ToList();
        typeGroupList.Add(new RowFormat { GapType = type, GapSize = threeSize, EndPrime = 3, StartPrime = 3 });
        typeGroupList = typeGroupList.OrderBy(r => r.StartPrime).ToList();
        uint size = 0;
        foreach (var row in typeGroupList)
            if (row.GapSize > size)
            {
                size = row.GapSize;
                row.GapType = $"Max {type}";
            }

        //calculate tails.
        ulong max = 0;
        foreach (var row in typeGroupList.OrderBy(o => o.GapSize))
            if (max < row.StartPrime)
            {
                max = row.StartPrime;
                row.Tail = true;
            }

        uint lastGapSize = 0;
        var canTail = true;
        foreach (var val in typeGroupList.OrderBy(r => r.GapSize))
        {
            while (lastGapSize + 2 < val.GapSize)
            {
                lastGapSize += 2;
                Console.WriteLine($"No {type},{lastGapSize},0,0");
                canTail = false;
            }

            lastGapSize = val.GapSize;
            var tailStr = canTail && val.Tail ? ",Tail" : "";
            Console.WriteLine(
                $"{FixGapType(val.GapType, lastContinuousCheck, val.StartPrime)},{val.GapSize},{val.StartPrime}{tailStr}");
        }
    }

    private static string[] GetFilesList(string folder, string allFileMask, SearchOption so, string fileMask)
    {
        var files = Directory.GetFiles(folder, allFileMask, so).OrderBy(f => f.Length).ThenBy(f => f).ToArray();

        foreach (var file in files)
        {
            var lastSlash = file.LastIndexOf('\\');
            var file0 = file.Substring(lastSlash);

            var firstDot = file0.IndexOf(".", StringComparison.Ordinal);
            if (firstDot == -1)
                continue;
            var secondDot = file0.IndexOf(".", firstDot + 1, StringComparison.Ordinal);
            if (secondDot == -1)
                continue;
            var numStr = file0.Substring(firstDot + 1, secondDot - (firstDot + 1));
            var didNum = uint.TryParse(numStr, out var num);
            if (!didNum)
                continue;
            var shouldFile = $"{folder}\\{num / 1024 / 1024}\\{num / 1024}{file0}";

            if (file != shouldFile)
            {
                var dirMakePath = $"{folder}\\{num / 1024 / 1024}\\{num / 1024}";
                while (!Directory.Exists(dirMakePath))
                    try
                    {
                        var createDirectory = Directory.CreateDirectory(dirMakePath);
                        Console.WriteLine($"{createDirectory.FullName} made");
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine(e);
                    }

                try
                {
                    Console.WriteLine($"move {file} {shouldFile}");
                    File.Move(file, shouldFile, true);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
        }

        files = Directory.GetFiles(folder, fileMask, so).OrderBy(f => f.Length).ThenBy(f => f).ToArray();

        files = TrimForSummaries(files, false);

        var didMove = false;
        uint group = 0;
        uint groupPos = 0;
        var groupFiles = new List<string>();
        foreach (var file in files)
        {
            var lastSlash = file.LastIndexOf('\\');
            var file0 = file.Substring(lastSlash);

            var firstDot = file0.IndexOf(".", StringComparison.Ordinal);
            if (firstDot == -1)
                continue;
            var secondDot = file0.IndexOf(".", firstDot + 1, StringComparison.Ordinal);
            if (secondDot == -1)
                continue;
            var numStr = file0.Substring(firstDot + 1, secondDot - (firstDot + 1));
            var didNum = uint.TryParse(numStr, out var num);
            if (!didNum)
                continue;

            var xGroup = num / 1024;
            var xGroupPos = num - xGroup * 1024;

            if (xGroup != group)
            {
                if (groupPos == 1023)
                    if (groupFiles.Count > 1023)
                    {
                        //summarize
                        var ls1 = groupFiles[0].LastIndexOf('\\');
                        var sourceDirectoryName = groupFiles[0].Substring(0, ls1);

                        var ls2 = sourceDirectoryName.LastIndexOf('\\');
                        var target = groupFiles[0].Substring(ls2, ls1 - ls2);
                        // ReSharper disable once UnusedVariable
                        var destinationArchiveFileName = groupFiles[0].Substring(0, ls2) + target + ".zip";
                        var start = group * 1024;
                        var end = group * 1024 + groupPos;
                        var ok = true;
                        for (var check = start; check <= end; check++)
                        {
                            if (groupFiles.Any(f => f.Contains($"GapPrimes.{check}.")))
                                continue;
                            ok = false;
                            break;
                        }

                        if (ok)
                        {
                            var summaryFileName = fileMask.Replace("*", $"{start}_{end}");
                            var summaryFilePath = groupFiles[0].Substring(0, ls2) + "\\" + summaryFileName;
                            if (!File.Exists(summaryFilePath))
                            {
                                CreateSummaryFile(groupFiles, summaryFilePath);
                                didMove = true;
                            }
                            //Console.WriteLine($"Zip {sourceDirectoryName} {destinationArchiveFileName}");
                            //ZipFile.CreateFromDirectory(sourceDirectoryName, destinationArchiveFileName, CompressionLevel.SmallestSize, false);
                            //?todo remove old
                        }
                        else
                        {
                            Console.WriteLine($"Interesting,{start},{end},{group}");
                        }
                    }

                //new group
                group = xGroup;
                groupPos = 0;
                groupFiles.Clear();
            }

            if (xGroupPos == groupPos)
            {
                //good.
                groupFiles.Add(file);
            }
            else if (xGroupPos == groupPos + 1)
            {
                //sill good.
                groupPos = xGroupPos;
                groupFiles.Add(file);
            }
            else
            {
                //missing file
                group = xGroup;
                groupPos = 0;
                groupFiles.Clear();
            }
        }

        if (didMove)
            files = Directory.GetFiles(folder, fileMask, so).OrderBy(f => f.Length).ThenBy(f => f).ToArray();

        files = TrimForSummaries(files, true);

        return files;
    }

    private static string[] TrimForSummaries(string[] files, bool testForGap)
    {
        var summaryDictionary = new Dictionary<uint, bool>();
        foreach (var file in files)
        {
            if (!file.Contains("_"))
                continue;

            var lastSlash = file.LastIndexOf('\\');
            var file0 = file.Substring(lastSlash);

            var firstDot = file0.IndexOf(".", StringComparison.Ordinal);
            if (firstDot == -1)
                continue;
            var secondDot = file0.IndexOf(".", firstDot + 1, StringComparison.Ordinal);
            if (secondDot == -1)
                continue;

            var numStr = file0.Substring(firstDot + 1, secondDot - (firstDot + 1));

            if (!numStr.Contains("_"))
                continue;

            var numSplit = numStr.Split("_");
            var didLowNum = uint.TryParse(numSplit[0], out var lowNum);
            if (!didLowNum)
                continue;
            var didHighNum = uint.TryParse(numSplit[1], out var highNum);
            if (!didHighNum)
                continue;
            if (lowNum > highNum)
                continue;
            if (lowNum + 1023 != highNum)
                continue;

            for (var i = lowNum; i <= highNum; i++)
                summaryDictionary.TryAdd(i, true);
        }

        if (testForGap)
        {
            //test summary dictionary
            var orderedKeys = summaryDictionary.Keys.OrderBy(x => x);
            var lastKey = uint.MaxValue;
            foreach (var key in orderedKeys)
            {
                if (lastKey + 1 != key)
                {
                    var errMsg = $"Missing Summary in range {lastKey / 1024}:{lastKey}::{key / 1024}:{key}";
                    Console.WriteLine(errMsg);
                    Console.Error.WriteLine(errMsg);
                }

                lastKey = key;
            }
        }

        var filesList = files.ToList();
        var noFileList = new List<string>();
        foreach (var file in filesList)
        {
            if (file.Contains("_"))
                continue;

            var lastSlash = file.LastIndexOf('\\');
            var file0 = file.Substring(lastSlash);

            var firstDot = file0.IndexOf(".", StringComparison.Ordinal);
            if (firstDot == -1)
                continue;
            var secondDot = file0.IndexOf(".", firstDot + 1, StringComparison.Ordinal);
            if (secondDot == -1)
                continue;

            var numStr = file0.Substring(firstDot + 1, secondDot - (firstDot + 1));
            var didNum = uint.TryParse(numStr, out var num);
            if (!didNum)
                continue;

            if (!summaryDictionary.ContainsKey(num))
                continue;

            noFileList.Add(file);
        }

        filesList = filesList.Except(noFileList).ToList();

        return filesList.ToArray();
    }

    /// <summary>
    /// Assembles a list of LastPrime files into a summary file.
    /// </summary>
    /// <param name="groupFiles"></param>
    /// <param name="summaryFilePath"></param>
    private static void CreateSummaryFile(List<string> groupFiles, string summaryFilePath)
    {
        try
        {
            var allFileRows = new List<string>();
            var tasks = new List<(Task<string[]?>, string)>();
            foreach (var groupFile in groupFiles)
            {
                var aGroupFile = groupFile;
                var task = Task.Factory.StartNew(() => ReadAllLines(aGroupFile));
                tasks.Add((task, groupFile));
            }

            foreach (var task in tasks)
            {
                task.Item1.Wait();
                //something went wrong.
                if (task.Item1.Result == null)
                    continue;
                allFileRows.AddRange(task.Item1.Result);
                var lastRowOfTask = task.Item1.Result[^1];
                if (!lastRowOfTask.StartsWith("LastPrime"))
                    throw new Exception($"File {task.Item2} does not have LastPrime");
            }

            // ReSharper disable once RedundantAssignment
            tasks = null;
            allFileRows = allFileRows.OrderBy(s => s).ToList();

            List<string> outfileRows = new List<string>();
            string lastRow = ",,,,";
            var lastRowArray = lastRow.Split(",");
            foreach (var thisRow in allFileRows)
            {
                var aThisRow = thisRow;
                var thisRowArray = thisRow.Split(",");
                if (thisRowArray.Length == 0)
                    continue;
                if (thisRowArray.Length != lastRowArray.Length)
                {
                    outfileRows.Add(lastRow);
                }
                else if (lastRowArray[0] == "LastPrime")
                {
                    outfileRows.AddIfNew(lastRow);
                }
                else if (OddFirst(lastRowArray))
                {
                    //keep all odd gaps.
                    outfileRows.Add(lastRow);
                }
                else if (ThreeEqual(thisRowArray, lastRowArray))
                {
                    var didThis = ulong.TryParse(thisRowArray[4], out var thisVal);
                    var didLast = ulong.TryParse(lastRowArray[4], out var lastVal);
                    if (didThis && didLast && lastVal < thisVal)
                    {
                        thisRowArray = lastRowArray;
                        aThisRow = lastRow;
                    }
                }
                else
                {
                    outfileRows.Add(lastRow);
                }

                lastRow = aThisRow;
                lastRowArray = thisRowArray;
            }

            outfileRows.Add(lastRow);

            if (File.Exists(summaryFilePath))
                File.Move(summaryFilePath, $"{summaryFilePath}.{DateTime.Now.Ticks}.old");

            File.WriteAllLines(summaryFilePath, outfileRows);
            Console.WriteLine($"Made {summaryFilePath}");
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }
    }

    /// <summary>
    /// Adds a string to a string list if the list does not end with the same string.
    /// </summary>
    /// <param name="list"></param>
    /// <param name="s"></param>
    public static void AddIfNew(this List<string> list, string s)
    {
        if (list.Count == 0)
        {
            list.Add(s);
        }
        else if (list[^1] != s)
        {
            list.Add(s);
        }
    }

    /// <summary>
    /// If the string array starts with a 1st gap that is even, return true
    /// i.e. the first gap is odd length
    /// </summary>
    /// <param name="lastRowArray"></param>
    /// <returns></returns>
    private static bool OddFirst(string[] lastRowArray)
    {
        if (lastRowArray.Length < 3)
            return false;

        if (lastRowArray[0] == "1st Gap" && lastRowArray[2] == "Primes")
        {
            var didGap = uint.TryParse(lastRowArray[1], out var gap);
            if (didGap && gap % 2 == 1)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Returns true if passed two string arrays with 4 or more items where the first three strings in each array are equal
    /// </summary>
    /// <param name="thisRowArray"></param>
    /// <param name="lastRowArray"></param>
    /// <returns></returns>
    private static bool ThreeEqual(string[] thisRowArray, string[] lastRowArray)
    {
        if (thisRowArray.Length < 4)
            return false;
        if (lastRowArray.Length < 4)
            return false;
        if (thisRowArray[0] != lastRowArray[0])
            return false;
        if (thisRowArray[1] != lastRowArray[1])
            return false;
        if (thisRowArray[2] != lastRowArray[2])
            return false;
        return true;
    }

    /// <summary>
    /// Reads all the lines from a file.
    /// Clears rows that fail the 'OddFirst' test.
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    private static string[]? ReadAllLines(string file)
    {
        try
        {
            var lines = FileExtension.GetReadAllLines(file);
            for (var i = 0; i < 10; i++)
                if (OddFirst(lines[i].Split(',')))
                    lines[i] = ",,,,";
            return lines;
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }

        return null;
    }

    private static string FixGapType(string? valueGapType, ulong lastContinuousCheck, ulong valueStartPrime)
    {
        if (lastContinuousCheck < valueStartPrime)
            return (valueGapType ?? "").Replace("1st ", "Fnd ");
        return (valueGapType ?? "").Contains("1st") ? valueGapType ?? "" : "1st " + (valueGapType ?? "");
    }

    private static string LastPrimeGapTypeFix(string? type)
    {
        if (string.IsNullOrWhiteSpace(type))
            return "NoType";

        if (type == "LastPrime")
            return "NumPrimes";

        return type;
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
        var oddGapList = new List<ulong>();
        //uint oddGaps = 0;
        foreach (var row in gapRows)
        {
            if (row.GapSize % 2 == 1)
                if (row.StartPrime != 2)
                {
                    if (!oddGapList.Contains(row.StartPrime))
                        oddGapList.Add(row.StartPrime);
                    //oddGaps++;
                    continue;
                }
            // not interested in odds.(beginning and end gaps)

            var didGet = AllGapRows.TryGetValue(row.GapSize, out var cr);
            if (!didGet || cr is null)
            {
                //we have not seen a gap of this size before, add it to the list.
                AllGapRows.Add(row.GapSize, row);
            }
            else if (cr.StartPrime > row.StartPrime)
            {
                //we have seen a gap of this size before, but it was at a higher value.
                //remove the old one, and put in the new one.
                AllGapRows.Remove(row.GapSize);
                AllGapRows.Add(row.GapSize, row);
            }
        }

        decimal lastWhen = 0;
        ulong lastLastPrime = 0;
        ulong previousLastLastPrime = 0;
        foreach (var row in rows.OrderBy(x => x.StartPrime).ThenBy(x => x.GapSize))
        {
            if (row.GapType == "LastPrime")
            {
                lastWhen += row.When;
                if (lastLastPrime == row.StartPrime)
                {
                    if (_allLastPrimeRows.Count == 1)
                        continue;
                    _allLastPrimeRows.RemoveAt(_allLastPrimeRows.Count - 1);
                    lastLastPrime = previousLastLastPrime;
                }

                var oddGaps = (uint)oddGapList.Count(x => lastLastPrime < x && x <= row.StartPrime);
                row.GapSize -= oddGaps;
                _allLastPrimeRows.Add(row);
                previousLastLastPrime = lastLastPrime;
                lastLastPrime = row.StartPrime;
                continue;
            }

            if (row.GapSize % 2 == 1)
                continue; // not interested in odds.(beginning and end gaps)
            var didGet = AllRows.TryGetValue((row.GapType, row.GapSize), out var rep);
            if (!didGet || rep is null)
            {
                AllRows.Add((row.GapType, row.GapSize), row);
            }
            else if (rep.StartPrime > row.StartPrime)
            {
                AllRows.Remove((row.GapType, row.GapSize));
                AllRows.Add((row.GapType, row.GapSize), row);
            }
        }

        return lastWhen;
    }

    private static void AppendAllRepRows(List<RepRowFormat> repRows)
    {
        foreach (var repRow in repRows)
        {
            var didGet = AllRepRows.TryGetValue((repRow.Repeat, repRow.GapSize), out var rep);
            if (!didGet || rep is null)
            {
                AllRepRows.Add((repRow.Repeat, repRow.GapSize), repRow);
            }
            else if (rep.StartPrime > repRow.StartPrime)
            {
                AllRepRows.Remove((repRow.Repeat, repRow.GapSize));
                AllRepRows.Add((repRow.Repeat, repRow.GapSize), repRow);
            }
        }
    }

    private static (List<GapRowFormat>, List<RepRowFormat>, List<RowFormat>) ProcessFileLines(string file)
    {
        //Console.WriteLine($"Reading From '{file}'");
        var gapRows = new List<GapRowFormat>();
        var repRows = new List<RepRowFormat>();
        var rows = new List<RowFormat>();
        var fileLines = FileExtension.GetReadAllLines(file);
        foreach (var row in fileLines)
            try
            {
                var split = row.Split(',');
                if (split.Length < 6)
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
                    //Don't count first Gap if odd Length in a non-summary file
                    if (gapRows.Count > 0 || rowFormat.GapSize % 2 == 0 || file.Contains("_"))
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