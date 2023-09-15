using System.Configuration;

internal static class ProgramClass
{
    private const decimal SecondsPerDay = 3600 * 24;

    public static void MainProgram()
    {
        try
        {
            var startTime = DateTime.Now;
            var allRows = new Dictionary<int, RowFormat>();
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

            foreach (var file in files)
                try
                {
                    //Console.WriteLine("Reading From '" + file + "'");
                    var rows = new List<RowFormat>();
                    foreach (var row in File.ReadAllLines(file))
                        try
                        {
                            var split = row.Split(',');
                            if (split.Length < 5)
                                continue;
                            var rowFormat = new RowFormat
                            {
                                GapType = split[0],
                                GapSize = int.TryParse(split[1], out var lineGapSize) ? lineGapSize : 0,
                                Primes = split[2],
                                EndPrime = ulong.TryParse(split[3], out var endPrime) ? endPrime : 0,
                                StartPrime = ulong.TryParse(split[4], out var startPrime) ? startPrime : 0,
                                When = decimal.TryParse(split[5], out var when) ? when : 0
                            };
                            rows.Add(rowFormat);
                        }
                        catch (Exception e)
                        {
                            Console.Error.WriteLine(e);
                        }

                    decimal lastWhen = 0;

                    foreach (var row in rows)
                    {
                        lastWhen = row.When;
                        if (row.GapSize % 2 == 1) continue; // not interested in odds.(beginning and end gaps)

                        //var cr = allRows.FirstOrDefault(a => a.GapSize == row.GapSize);
                        var didGet = allRows.TryGetValue(row.GapSize, out var cr);

                        if (!didGet)
                        {
                            //we have not seen a gap of this size before, add it to the list.
                            allRows.Add(row.GapSize, row);
                        }
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                        else if (cr.StartPrime > row.StartPrime)
#pragma warning restore CS8602 // Dereference of a possibly null reference.
                        {
                            //we have seen a gap of this size before, but it was at a higher value.
                            //remove the old one, and put in the new one.
                            allRows.Remove(row.GapSize);
                            allRows.Add(row.GapSize, row);
                        }
                        //else
                        //{
                        //keep cr
                        //don't all row
                        //}
                    }

                    totalTime += lastWhen;
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

            var firstRows = new List<RowFormat>();

            foreach (var rowX in allRows)
            {
                var row = rowX.Value;
                //if (row.GapSize % 2 == 1) continue; // not interested in odds.(beginning and end gaps)
                //if (allRows.Exists(x => x.GapSize == row.GapSize && x.StartPrime < row.StartPrime)) continue; // not interested if a better one was found.
                //if (firstRows.Exists(x => x.GapSize == row.GapSize && x.StartPrime == row.StartPrime)) continue; //duplicate
                firstRows.Add(row);
            }

            // sort the gaps by start prime, and find the max gaps.
            var gapSize = -1;

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

            var lastGap = 0;
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
}