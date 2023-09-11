using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;

namespace FileProcessor
{
    internal class Program
    {
        private const decimal SecondsPerDay = 3600 * 24;

        private static void Main()
        {
            try
            {
                var startTime = DateTime.Now;
                var allRows = new Dictionary<int, RowFormat>();
                var folder = ConfigurationManager.AppSettings["Folder"];
                var fileMask = ConfigurationManager.AppSettings["FileMask"];
                var so = SearchOption.TopDirectoryOnly;
                try
                {
                    var opts = ConfigurationManager.AppSettings["SubDirectories"].ToUpper();
                    if (opts.StartsWith("Y") || opts.StartsWith("1") || opts.StartsWith("T")) so = SearchOption.AllDirectories;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
                var files = Directory.GetFiles(folder, fileMask, so);
                decimal totalTime = 0;

                foreach (var file in files)
                {
                    try
                    {
                        //Console.WriteLine("Reading From '" + file + "'");
                        var rows = File.ReadAllLines(file)
                            .Select(x => x.Split(','))
                            .Select(x => new RowFormat
                            {
                                GapType = x[0],
                                GapSize = int.Parse(x[1]),
                                Primes = x[2],
                                EndPrime = ulong.Parse(x[3]),
                                StartPrime = ulong.Parse(x[4]),
                                When = (decimal)float.Parse(x[5]),
                            })
                            .ToList();

                        decimal lastWhen = 0;

                        foreach (var row in rows)
                        {
                            lastWhen = row.When;
                            if (row.GapSize % 2 == 1) continue; // not interested in odds.(beginning and end gaps)

                            //var cr = allRows.FirstOrDefault(a => a.GapSize == row.GapSize);
                            var didGet = allRows.TryGetValue(row.GapSize, out RowFormat cr);

                            if (!didGet)
                            {
                                //we have not seen a gap of this size before, add it to the list.
                                allRows.Add(row.GapSize, row);
                            }
                            else if (cr.StartPrime > row.StartPrime)
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
                    catch (Exception ex)
                    {
                        Console.Error.WriteLine("With file='" + file + "'");
                        while (ex != null)
                        {
                            Console.Error.WriteLine(ex);
                            ex = ex.InnerException;
                        }
                        Console.Error.WriteLine("Continuing to the next file.");
                    }
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
                {
                    if (row.GapSize > gapSize)
                    {
                        gapSize = row.GapSize;
                        row.GapType = "Max Gap";
                    }
                    else
                    {
                        row.GapType = "1st Gap";
                    }
                }

                //calculate tails.
                ulong maxStartPrime = 0;
                foreach (var row in firstRows.OrderBy(o => o.GapSize))
                {
                    if (maxStartPrime > row.StartPrime)
                    {
                        row.Tail = false;
                    }
                    else
                    {
                        maxStartPrime = row.StartPrime;
                        row.Tail = true;
                    }
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
                    Console.WriteLine("{0},{1},{2},{3}{4}", row.GapType, row.GapSize, row.StartPrime, row.EndPrime, (canTail && row.Tail ? ",Tail" : ""));
                    lastGap = row.GapSize;
                }

                var endTime = DateTime.Now;
                Console.WriteLine("FP Runtime " + (endTime - startTime).TotalSeconds.ToString("#.##") + " seconds.");
                Console.WriteLine("AP Runtime " + (totalTime / SecondsPerDay).ToString("#.##") + " cpu-days.");
            }
            catch (Exception ex)
            {
                while (ex != null)
                {
                    Console.Error.WriteLine(ex);
                    ex = ex.InnerException;
                }
            }
        }
    }

    internal class RowFormat
    {
        public string GapType { get; set; }
        public int GapSize { get; set; }
        public string Primes { get; set; }
        public ulong EndPrime { get; set; }
        public ulong StartPrime { get; set; }
        public decimal When { get; set; }

        public bool Tail { get; set; }
    }
}