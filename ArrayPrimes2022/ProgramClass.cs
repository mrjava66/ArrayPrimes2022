using System.Configuration;

namespace ArrayPrimes2022;

internal static class ProgramClass
{
    private const ulong Two32 = (ulong)uint.MaxValue + 1; //read as 2 to the 32
    private const ulong Two31 = Two32 / 2;
    private const ulong Two30 = Two31 / 2;
    private const ulong Two28 = Two30 / 4;

    public static bool BigArray { get; private set; } //controls if the two-dimensional gap array is made and displayed.

    static ProgramClass()
    {
        Anvil = new byte[FullAnvilSize];
    }

    private static string _quickCheck = string.Empty;  // use values in the config to check a specific range
    private static bool _runAllBlocksInOrder = true;   // run the entire number line in order, instead of the places where the big gaps are.
    private static bool _getPreviousWork;              // retrieve previous work and not rerun old blocks.
    private static bool _allowQuickCheckBailout = true;// quick check does not rerun old blocks.
    private static bool _reverse;                      // run the number line in reverse down from 2^64
    private static string _basePath = "";              // where the files are.
    private static DateTime _startTimeSpacer = DateTime.Now;  // limits the starting of tasks to one new task per 1.2 seconds
    private static readonly TimeSpan StartTimeSpacing = TimeSpan.FromMilliseconds(1200);

    /// <summary>
    ///     the array that holds all the possible arrangements of the first 8 divisors that we will need.
    ///     future work will allow this to cover the next 5.
    /// </summary>
    private static readonly byte[] Anvil;

    private const ulong AnvilSize = 3 * 5 * 7 * 11 * 13 * 17 * 19 * 23;
    private const ulong AnvilSize2 = 29 * 31 * 37 * 41 * 43;
    private const ulong FullAnvilSize = AnvilSize * (8 + 1) + Two28;

    /// <summary>
    ///     The minimum value to check.
    /// </summary>
    private static ulong _minvalueNumber;

    /// <summary>
    ///     How many tasks to run concurrently.  Normally overriden in the .config.
    /// </summary>
    private static int _taskLimit = 3;

    // List of Blocks of 128*Two32 numbers (1/2T) that contain a first gap.
    // This list REQUIRES a block size of 128 to be correct.
    // ReSharper disable once ArrangeObjectCreationWhenTypeEvident
    private static readonly List<uint> Guided = new List<uint>
    {
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 16, 17, 19, 20, 22,
        23, 24, 25, 27, 28, 31, 32, 33, 35, 37, 40, 42, 43, 45, 48, 51, 54, 61,
        62, 64, 69, 70, 72, 75, 77, 79, 83, 85, 86, 90, 92, 99, 104, 108, 112,
        119, 127, 134, 142, 149, 165, 176, 177, 178, 194, 201, 202, 252, 254,
        260, 261, 263, 264, 280, 283, 293, 311, 313, 335, 338, 341, 344, 389,
        396, 397, 399, 442, 445, 464, 505, 563, 589, 741, 770, 774, 829, 854,
        858, 884, 904, 905, 923, 943, 994, 1008, 1149, 1219, 1224, 1233, 1321,
        1353, 1423, 1454, 1468, 1505, 1536, 1564, 1602, 1662, 1901, 2047, 2077,
        2163, 2312, 2420, 2477, 2517, 2520, 2533, 2583, 2605, 2664, 2668, 2829,
        2908, 3068, 3079, 3110, 3172, 3735, 3764, 3868, 4180, 4345, 4378, 4440,
        4563, 4614, 4741, 4920, 5019, 5028, 5129, 5788, 6087, 6131, 6260, 6574,
        6766, 6877, 7052, 7176, 7312, 7507, 7636, 7796, 8099, 8545, 8987, 9096,
        9362, 9383, 9534, 9743, 10561, 11048, 11608, 12957, 13730, 13941,
        15363, 15963, 16206, 17168, 17267, 18278, 18634, 18862, 19101, 19420,
        19597, 20834, 21221, 21243, 21402, 21488, 21902, 24022, 25271, 26080,
        26548, 26726, 27202, 27493, 28195, 30210, 34069, 34788, 34892, 35295,
        35377, 38074, 38250, 39063, 39551, 40818, 41455, 41787, 43474, 45244,
        45504, 46362, 46751, 47319, 51422, 52100, 54270, 56513, 60417, 60551,
        63387, 65624, 67926, 69062, 72384, 73548, 75965, 79334, 79747, 80010,
        84122, 91666, 100682, 104958, 107146, 108862, 110216, 111866, 119781,
        123286, 124423, 132129, 135014, 142100, 143084, 143599, 143732, 147108,
        151198, 152201, 155164, 156689, 160234, 162288, 166036, 172452, 179054,
        179131, 183322, 186895, 207957, 219826, 227665, 229185, 233970, 252900,
        256360, 281198, 286015, 314260, 314457, 328368, 329313, 334095, 361774,
        365588, 369533, 371049, 372452, 387893, 393530, 396602, 430287, 495985,
        533808, 555529, 610829, 635567, 641232, 676599, 725924, 728034, 730196,
        735680, 758448, 760396, 771172, 806061, 809904, 838388, 849924, 864177,
        899769, 919106, 951795, 986641, 1039963, 1051194, 1100572, 1114498,
        1159029, 1228085, 1268824, 1279460, 1322554, 1333710, 1363451, 1371368,
        1402618, 1462854, 1490871, 1534589, 1553935, 1606065, 1623134, 1777421,
        1803010, 1817525, 1839520, 1863606, 1876290, 1877255, 1912014, 1927077,
        1977446, 2011854, 2063602, 2075543, 2097799, 2126930, 2147047, 2161203,
        2208470, 2422368, 2540196, 2585883, 2592374, 2602889, 2638134, 2692162,
        2870672, 3148817, 3163255, 3271555, 3272789, 3398465, 3489889, 3554882,
        3774529, 3978847, 4031373, 4041611, 4410519, 4793549, 4951312, 5103289,
        5116479, 5308680, 5325360, 5342409, 5411867, 5421875, 5644781, 5855521,
        5962680, 6242149, 6274313, 6353107, 6776163, 6823015, 6830178, 7162194,
        7467096, 7674478, 7736211, 8111596, 8379971, 8451104, 8794567, 9119971,
        9632352, 9845096, 9947651, 10264250, 10299605, 10300553, 10428705,
        11140108, 11725351, 11839395, 12347280, 12569450, 13027597, 13500439,
        13696202, 13728259, 13755782, 14165564, 15246808, 16348822, 17438150,
        17565834, 17841192, 18161148, 18378515, 20451136, 20576488, 20592223,
        21093154, 23075546, 23998453, 24578845, 24617240, 27194278, 27595883,
        28322808, 30837039, 31821539, 31931132, 32157284, 33399147
    };

    /// <summary>
    ///     All the possible arrangements of the divisors 3-23.
    /// </summary>
    private static void BuildAnvil()
    {
        //build the full list
        var dl = new[] { 2, 3, 5, 7, 11, 13, 17, 19, 23 };
        BuildAnvil0(dl);
    }

    /*
    private static void BuildAnvilNew(int[] dl)
    {
        {
            // three works differently.  this reduces the number of memory writes to 3/8 of what 3 normally does.  Also, the memory reads are not needed.
            // ReSharper disable InconsistentNaming
            const byte byte3_0 = (1 << 0) + (1 << 3) + (1 << 6);
            const byte byte3_1 = (1 << 1) + (1 << 4) + (1 << 7);
            const byte byte3_2 = (1 << 2) + (1 << 5);
            // ReSharper restore InconsistentNaming
            byte[] byte3 = { byte3_0, byte3_1, byte3_2 };
            ulong oByte = 0;
            var obit = 1;

            while (oByte < FullAnvilSize)
            {
                Anvil[oByte] = byte3[obit];
                obit++;
                if (obit > 2) obit = 0;
                oByte++;
            }
        }

        {
            // five works differently.  this reduces the number of memory writes to 5/8 of what 5 normally does.
            // ReSharper disable InconsistentNaming
            const byte byte5_0 = (1 << 0) + (1 << 5);
            const byte byte5_1 = (1 << 1) + (1 << 6);
            const byte byte5_2 = (1 << 2) + (1 << 7);
            const byte byte5_3 = 1 << 3;
            const byte byte5_4 = 1 << 4;
            // ReSharper restore InconsistentNaming
            byte[] byte5 = { byte5_0, byte5_1, byte5_2, byte5_3, byte5_4 };
            ulong oByte = 0;
            var obit = 2;

            while (oByte < FullAnvilSize)
            {
                Anvil[oByte] |= byte5[obit];
                obit += 2;
                if (obit > 4) obit -= 5;
                oByte++;
            }
        }

        //2 is not processed, 3&5 are already processed, just apply 7 and up.
        for (var divisorPosition = 3; divisorPosition < dl.Length; divisorPosition++)
        {
            var p = dl[divisorPosition];
            var primeByte = (ulong)p / 8;
            var primeBit = p % 8;
            var offsetByte = (ulong)0;
            var offsetBit = 0;

            while (offsetByte < FullAnvilSize)
            {
                var bib = (byte)(1 << offsetBit);

                if ((Anvil[offsetByte] & bib) == 0)
                    Anvil[offsetByte] |= bib;

                offsetByte += primeByte;
                offsetBit += primeBit;
                if (offsetBit < 8) continue;
                offsetBit -= 8;
                offsetByte++;
            }
        }
    }
    */

    private static void BuildAnvil0(int[] dl)
    {
        for (var divisorPosition = 1; divisorPosition < dl.Length; divisorPosition++)
        {
            var p = dl[divisorPosition];
            var primeByte = (ulong)p / 8;
            var primeBit = p % 8;
            //var offsetByte = (ulong)0;
            //var offsetBit = 0;
            var offsetByte = (ulong)((p - 1) / 2 / 8);
            var offsetBit = (p - 1) / 2 % 8;

            while (offsetByte < FullAnvilSize)
            {
                var bib = (byte)(1 << offsetBit);

                if ((Anvil[offsetByte] & bib) == 0)
                    Anvil[offsetByte] |= bib;

                offsetByte += primeByte;
                offsetBit += primeBit;
                if (offsetBit < 8) continue;
                offsetBit -= 8;
                offsetByte++;
            }
        }
    }

    private static void ConfigureSystem()
    {
        var bigArrayString = ConfigurationManager.AppSettings["BigArray"] ?? "true";
        // ReSharper disable once SimplifyConditionalTernaryExpression
        BigArray = bool.TryParse(bigArrayString, out var bigArray) ? bigArray : true;

        var getPreviousWorkString = ConfigurationManager.AppSettings["GetPreviousWork"] ?? "true";
        // ReSharper disable once SimplifyConditionalTernaryExpression
        _getPreviousWork = bool.TryParse(getPreviousWorkString, out var getPreviousWork) ? getPreviousWork : true;

        _basePath = ConfigurationManager.AppSettings["basePath"] ?? "";
        if (!string.IsNullOrWhiteSpace(_basePath) && !_basePath.EndsWith("\\"))
            _basePath += "\\";

        var taskLimit = ConfigurationManager.AppSettings["taskLimit"] ?? "-1";
        if (int.TryParse(taskLimit, out var taskLimitInt))
            if (taskLimitInt > 0)
            {
                _taskLimit = taskLimitInt;
            }
            else
            {
                _taskLimit = Environment.ProcessorCount + taskLimitInt;
                if (_taskLimit < 1)
                    _taskLimit = Environment.ProcessorCount - 1;
            }

        Console.WriteLine($"TaskLimit={_taskLimit}");

        const string linear = "linear";
        var blockOrder = (ConfigurationManager.AppSettings["BlockOrder"] ?? linear).ToLower();

        if (blockOrder != linear)
            _runAllBlocksInOrder = false;

        _quickCheck = ConfigurationManager.AppSettings["QuickCheck"] ?? "";

        var reverseStr = ConfigurationManager.AppSettings["Reverse"] ?? "true";
        var didParseReverse = bool.TryParse(reverseStr, out var reverse);
        // ReSharper disable once SimplifyConditionalTernaryExpression
        _reverse = didParseReverse ? reverse : true;

        var allowQuickCheckBailout = ConfigurationManager.AppSettings["AllowQuickCheckBailout"] ?? "true";
        var didParse = bool.TryParse(allowQuickCheckBailout, out var val);
        // ReSharper disable once SimplifyConditionalTernaryExpression
        _allowQuickCheckBailout = didParse ? val : true;

        var minvalueString = (ConfigurationManager.AppSettings["MinValue"] ?? "").Replace(",", "");

        if (ulong.TryParse(minvalueString, out var ulongResult))
            _minvalueNumber = ulongResult;
        else if (double.TryParse(minvalueString, out var doubleResult)) _minvalueNumber = (ulong)doubleResult;
    }

    private static Dictionary<uint, uint> GetPreviousWork()
    {
        var xd = new Dictionary<uint, uint> { { 0, 0 } };

        if (!_getPreviousWork)
            return xd;

        //return xd;  // enable this to ignore previous work.
        if (string.IsNullOrWhiteSpace(_basePath))
            _basePath = Directory.GetCurrentDirectory();
        var x = from f in Directory.EnumerateFiles(_basePath, "*.log", SearchOption.AllDirectories)
            where f.EndsWith("log") && f.Contains("\\GapArray.")
            select f;
        var xl = x.ToList();

        foreach (var xx in xl)
        {
            var fd = xx.IndexOf("\\GapArray.", StringComparison.OrdinalIgnoreCase);
            var sd = xx.IndexOf('.', fd + 1);
            var td = xx.IndexOf('.', sd + 1);
            var intStr = xx.Substring(sd + 1, td - (sd + 1));

            if (!uint.TryParse(intStr, out var intInt)) continue;
            if (!xd.Keys.Contains(intInt)) xd.TryAdd(intInt, intInt);
        }

        return xd;
    }

    private static uint GuidedBlocks(uint runBlock)
    {
        foreach (var retval in Guided)
            if (retval >= runBlock)
                return retval;

        return runBlock;
    }

    private static bool IsBitSet(byte b, int pos)
    {
        return (b & (1 << pos)) != 0;
    }

    /// <summary>
    ///     Less than 1B executions.
    /// </summary>
    public static void MainProgram()
    {
        try
        {
            var now = DateTime.UtcNow.ToString("u").Replace(" ", ".").Replace(":", ".");
            ConfigureSystem();

            var taskBuildAnvil = MakeRunningBuildAnvilProcess();

            var fullDivisorList = new uint[203280222];
            MakeBaseArrays(fullDivisorList, now);
            GC.Collect();
            taskBuildAnvil.Wait();
            // ReSharper disable once RedundantAssignment
            taskBuildAnvil = null;
            //BuildAnvil();
            GC.Collect();

            //each block represents 128*2^32 values.  blocks are numbered from 0.
            const uint blockAssignmentSize = 128;
            const uint firstBlock = 0;
            const uint lastBlock = (uint)(Two32 / blockAssignmentSize);

            var tasks = new List<Task>();

            //build a list of previous work.
            var xd = GetPreviousWork();

            Console.WriteLine($"Found Existing work.  Files={xd.Count}");

            QuickCheck(fullDivisorList, now, xd);
            var minBlock = (uint)(_minvalueNumber / (Two32 * blockAssignmentSize));

            if (_reverse)
                for (var value = uint.MaxValue; value > 0; value--)
                {
                    if (xd.Keys.Contains(value))
                        continue;

                    ManageTasks(tasks);

                    StartNewSpacer();
                    var val = value;
                    var taskE = Task.Factory.StartNew(() =>
                        ProcessNumberBlocks(fullDivisorList, val, val + 1, now));
                    tasks.Add(taskE);
                    xd.TryAdd(val, val);
                }

            for (var runBlock = firstBlock; runBlock < lastBlock; runBlock++)
            {
                if (runBlock < minBlock) continue;

                runBlock = _runAllBlocksInOrder ? runBlock : GuidedBlocks(runBlock);

                var block = runBlock;

                var minvalue = block * blockAssignmentSize;
                var maxvalue = (block + 1) * blockAssignmentSize;
                if (maxvalue == 0) maxvalue--; //special code for the very last block.  maxvalue=max uint32.

                while (minvalue < maxvalue)
                {
                    if (!xd.Keys.Contains(minvalue)) break;
                    minvalue++;
                }

                if (minvalue == maxvalue) continue;

                ManageTasks(tasks);

                StartNewSpacer();
                var taskE = Task.Factory.StartNew(() =>
                    ProcessNumberBlocks(fullDivisorList, minvalue, maxvalue, now));
                tasks.Add(taskE);
                //ProcessNumberBlocks(fullDivisorList, minvalue, maxvalue, now);
                for (var i = minvalue; i < maxvalue; i++)
                    xd.TryAdd(i, i);
            }

            //wait for all tasks to complete.
            while (tasks.Count > 0)
            {
                tasks[0].Wait();
                ManageTasks(tasks);
            }
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

    /// <summary>
    ///     if time since last start is long enough, go.
    ///     otherwise, wait the spacing.
    /// </summary>
    private static void StartNewSpacer()
    {
        if (_startTimeSpacer > DateTime.Now)
        {
            var wait = _startTimeSpacer - DateTime.Now;
            Thread.Sleep(wait);
        }

        _startTimeSpacer = DateTime.Now + StartTimeSpacing;
    }

    /// <summary>
    ///     Performance improvements in this section are not very important.
    ///     In 18e18 primes it would get less than 35K entries.
    ///     The goal of this code is to make FDL
    ///     (FULL DIVISOR LIST)
    ///     All the primes less than 2^32.
    /// </summary>
    /// <param name="fdl"></param>
    /// <param name="now"></param>
    /// <returns></returns>
    private static void MakeBaseArrays(uint[] fdl, string now)
    {
        //var outfile = new StreamWriter("ThreadPrimes.log", false);

        var goal = ulong.MaxValue; // the final value to get.
        var divisorArrayMax =
            Math.Sqrt(goal) > uint.MaxValue
                ? uint.MaxValue
                : (uint)Math.Sqrt(goal); // the max value in the divisor arrays.

        var arrays = new List<byte[]>(); // the holder of the divisor arrays
        const uint baseArrayCount = 8; // number of elements in the divisor arrays.  (count)
        var baseArrayUnitSize =
            1 + divisorArrayMax / baseArrayCount / 16; // size of the divisor array element (bytes)

        //refactor into make a report base arrays.
        for (uint i = 0; i < baseArrayCount; i++) // allocate and add the divisor arrays.
        {
            //byte[] arrayX = SuperFast.InitByteArrayNormal(0xFF, baseArrayUnitSize);
            var arrayX =
                new byte[baseArrayUnitSize]; //primes and possible primes have bit value of 0, composites have bit value of 1.
            arrays.Add(arrayX);
        }

        var sieveTop = (ulong)(Math.Sqrt(uint.MaxValue) + 1); // last value to sieve through the divisor array.
        var gr = new GapReport(new GapReportCarryState { GapRepeat = 0, LastGap = 0, LastPrime = 2 });

        var countPrimeNumber = 0;
        fdl[0] = 2;
        gr.ReportGap(2);
        //don't sieve for 2
        foreach (var seedPrime in new ulong[] { 3, 5, 7, 11, 13 })
        {
            fdl[++countPrimeNumber] = (uint)seedPrime;
            gr.ReportGap(seedPrime);
            StartUpSieve(arrays, arrays.Count, baseArrayUnitSize, seedPrime);
        }

        ulong arraySize16 = baseArrayUnitSize * 16;

        ulong prime = 0;
        for (var a = 0; a < baseArrayCount; a++)
        for (var l = a == 0 ? 1 : (ulong)0; l < baseArrayUnitSize; l++)
        {
            if (arrays[a][l] == 255) continue;
            for (ulong pos = 0; pos < 8; pos++) // only check odd for prime.
            {
                if (IsBitSet(arrays[a][l], (int)pos)) continue;

                prime = (ulong)a * arraySize16 + l * 16 + pos * 2 + 1;
                fdl[++countPrimeNumber] = (uint)prime;

                gr.ReportGap(prime);

                //outfile.WriteLine(prime);
                if (prime < sieveTop)
                    StartUpSieve(arrays, arrays.Count, baseArrayUnitSize,
                        prime); // don't need to sieve values greater than top.
            }
        }

        gr.ReportGap(baseArrayCount * arraySize16); // do an end gap.
        var gapFile = new StreamWriter(_basePath + "GapPrimes.0." + now + ".log", false);
        gr.LastPrime(prime, gapFile); // do an end gap.
        //gr.WriteFlush(gapFile);

        var gapsFile = new StreamWriter(_basePath + "GapArray.0." + now + ".log", false);
        gr.ReportGaps(gapsFile);
    }

    /// <summary>
    ///     Gets us a build anvil process that runs concurrently with building the divisor arrays.
    /// </summary>
    /// <returns></returns>
    private static Task MakeRunningBuildAnvilProcess()
    {
        var taskBuildAnvil = Task.Factory.StartNew(BuildAnvil);
        Thread.Sleep(250);

        // if it is still not running, start it.
        while (taskBuildAnvil.Status != TaskStatus.Running)
            try
            {
                taskBuildAnvil.Start();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }

        return taskBuildAnvil;
    }

    //private static readonly Random NotSoRand = new Random();
    //private static int _spot = 1;
    private static void ManageTasks(List<Task> tasks)
    {
        while (true)
        {
            //remove all completed tasks.
            while (true)
            {
                var x = tasks.FirstOrDefault(t => t.IsCompleted);

                if (x == null) break;
                tasks.Remove(x);
            }

            // if task.Count < max allowed, return;
            if (tasks.Count < _taskLimit) return;

            var t0 = tasks[0];
            t0.Wait(200);
        }
    }

    private static void MarkAnArray(ulong arraySize, ulong prime, ulong nextMark, byte[] array)
    {
        var by = (nextMark - 1) / 16;
        var bi = (int)((nextMark - 1 - by * 16) / 2);
        // add the base number by twice the prime value.(evens are not in the array).which is prime bits down since the array only contains odds.
        var byInc = prime / 8;
        var biInc = (int)(prime - byInc * 8);

        while (by < arraySize)
        {
            var bib = (byte)(1 << bi);

            if ((array[by] & bib) == 0) array[by] |= bib;
            by += byInc;
            bi += biInc;
            if (bi >= 8)
            {
                by++;
                bi -= 8;
            }
        }
    }

    // the main way I improve this code is to call it less.  (4B)*(~1e8) right now.
    private static uint PopulateDivisor(uint[] fdl, uint[] offsets, ulong loopMinCheckedValue,
        ulong divisorsFillPosition)
    {
        var prime = fdl[divisorsFillPosition];
        var bump = prime - loopMinCheckedValue % prime;
        bump += bump % 2 == 0 ? prime : 0;
        var offset = (bump - 1) / 2;
        offsets[divisorsFillPosition] = (uint)offset;
        return prime;
    }

    private static void SieveProcess(ulong loopMinCheckedValue, uint[] fdl, uint[] offsets, ulong divisorsFillPosition,
        ulong divisorPosition, byte[] bytes0)
    {
        /*
         uncomment this to check the Anvil
        for (ulong i = 1; i < divisorPosition; i++)
        {
            var p = fdl[i];
            var o = offsets[i];
            var primeByte = p / 8;
            var primeBit = (int)p % 8;
            var offsetByte = o / 8;
            var offsetBit = (int)o % 8;

            while (offsetByte < Two28)
            {
                //it is quicker to just check the whole byte, and only when available, check the bit.
                if (bytes0[offsetByte] != 255)
                {
                    var bib = (byte)(1 << offsetBit);
                    //when we multi-thread, write-time is much longer than read-time.  This check makes the app run about 30% faster.
                    if ((bytes0[offsetByte] & bib) == 0)
                    {
                        var loc = loopMinCheckedValue + 16 * offsetByte + 2 * (ulong)offsetBit + 1;
                        Console.Error.WriteLine($"Should be marked already.{loopMinCheckedValue}:{loc}:{p}");
                        bytes0[offsetByte] |= bib;
                    }
                }

                offsetByte += primeByte;
                offsetBit += primeBit;
                if (offsetBit >= 8)
                {
                    offsetBit -= 8;
                    offsetByte++;
                }
            }

            var ndp = (offsetByte - Two28) * 8 + (ulong)offsetBit;
            if (ndp > uint.MaxValue)
            {
                Console.Error.WriteLine($"Fixed over with {divisorPosition}:prime:{p}:ndp:{ndp}");
                ndp -= p;
            }

            if (ndp > uint.MaxValue)
                Console.Error.WriteLine($"Problem with {divisorPosition}:prime:{p}:ndp:{ndp}");
            offsets[i] = (uint)ndp;
        }
        */

        for (; divisorPosition < divisorsFillPosition; divisorPosition++)
        {
            var p = fdl[divisorPosition];
            var o = offsets[divisorPosition];
            var primeByte = p / 8;
            var primeBit = (int)p % 8;
            var offsetByte = o / 8;
            var offsetBit = (int)o % 8;

            #region div3

            //alternate moving p and 2p bits down the array to skip over divisible by 3 bits.
            var primeByteOn = primeByte + primeByte;
            var primeBitOn = primeBit + primeBit;
            if (primeBitOn >= 8)
            {
                primeBitOn -= 8;
                primeByteOn++;
            }

            var markLoc = loopMinCheckedValue + 16 * offsetByte + 2 * (ulong)offsetBit + 1;
            if (markLoc % 3 == 0)
            {
                offsetByte += primeByte;
                offsetBit += primeBit;
                if (offsetBit >= 8)
                {
                    offsetBit -= 8;
                    offsetByte++;
                }

                markLoc = loopMinCheckedValue + 16 * offsetByte + 2 * (ulong)offsetBit + 1;
            }

            var onOff = (2 * p + markLoc) % 3 == 0;

            #endregion div3

            //this is the main loop, this get's all the action.  ~2^32*(0.5*2^32/log(2^32)) entries and ( 2^32*(0.5*(2^32)/log(2^32)) )*( ((2^32)/log(2^32))*(1/2)*(1/3) ) cycles.
            while (offsetByte < Two28)
            {
                /*
                uncomment this to check the onOff (i.e. don't check bits that are divisible by 3)
                markLoc = loopMinCheckedValue + 16 * offsetByte + 2 * (ulong)offsetBit + 1;
                if (markLoc % 3 == 0)
                {
                    Console.Error.WriteLine($"this should not happen.{p}:{markLoc}:{onOff}");
                }
                */

                //it is quicker to just check the whole byte, and only when available, check the bit.
                if (bytes0[offsetByte] != 255)
                {
                    var bib = (byte)(1 << offsetBit);
                    //when we multi-thread, write-time is much longer than read-time.  This check makes the app run about 30% faster.
                    if ((bytes0[offsetByte] & bib) == 0) bytes0[offsetByte] |= bib;
                }

                #region div3b

                if (onOff)
                {
                    offsetByte += primeByteOn;
                    offsetBit += primeBitOn;
                }
                else
                {
                    offsetByte += primeByte;
                    offsetBit += primeBit;
                }

                if (offsetBit >= 8)
                {
                    offsetBit -= 8;
                    offsetByte++;
                }

                onOff = !onOff;

                #endregion div3b
            }

            /*
            var ndp = ((ulong)offsetByte - Two28) * 8 + (ulong)offsetBit;
            while (ndp > uint.MaxValue)
            {
                Console.Error.WriteLine($"Fixed over with {divisorPosition}:prime:{p}:ndp:{ndp}");
                ndp -= p;
            }

            if (ndp > uint.MaxValue)
                Console.Error.WriteLine($"Problem with {divisorPosition}:prime:{p}:ndp:{ndp}");
            offsets[divisorPosition] = (uint)ndp;
            */
        }
    }

    /// <summary>
    ///     Less than 1B executions
    ///     ~2^32/128 or 2^25=32M
    /// </summary>
    /// <param name="fdl"></param>
    /// <param name="v1"></param>
    /// <param name="v2"></param>
    /// <param name="now"></param>
    /// <returns></returns>
    private static void ProcessNumberBlocks(uint[] fdl, uint v1, uint v2, string now)
    {
        var maxCheckedValue = Two32 * v2;
        if (maxCheckedValue == 0)
            // we are INTENTIONALLY overflowing.
            // ReSharper disable once IntVariableOverflowInUncheckedContext
            maxCheckedValue--; //special code for final block, it is a uInt, so this is the maxValue.
        var maxDivisor = (ulong)Math.Sqrt(maxCheckedValue);
        //(x/log x)(1 + 1.2762/log x)
        var logMaxDivisor = Math.Log(maxDivisor);
        var predictArraySize = (ulong)(maxDivisor / logMaxDivisor * (1 + 1.2762 / logMaxDivisor));
        var offsets = new uint[predictArraySize];
        //Divisor[] divisors = new Divisor[predictArraySize]; (use fdl -> full divisor array)
        var lastCheckedPrime =
            v1 * Two32; // the gap on the first and last batch will be odd.  But, we can keep track of the rest.

        ProcessThisStuffCompact(fdl, v1, v2, now, offsets, lastCheckedPrime);
    }

    private static void ProcessThisStuffCompact(uint[] fdl, uint v1, uint v2, string now, uint[] offsets,
        ulong lastCheckedPrime)
    {
        // process all the primes in segment a.  (a*2^32 -> (a+1)*2^32)
        // ReSharper disable once ArrangeRedundantParentheses
        var gapReportCarryState = new GapReportCarryState
        {
            LastPrime = lastCheckedPrime,
            LastGap = 0,
            GapRepeat = 0
        };
        for (var a = v1; a < v2 || (v2 == 0 && a == v1); a++)
        {
            var grl = new GapReport(gapReportCarryState);

            //populate the divisor array
            //populate the divisor offsets
            var loopMinCheckedValue = a * Two32;
            var loopMaxCheckedValue = loopMinCheckedValue + Two32;
            var loopMaxDivisor = (uint)Math.Sqrt(loopMaxCheckedValue);
            if (loopMaxCheckedValue == 0) //special code for the final block.
            {
                loopMaxCheckedValue--; //max ulong value.
                loopMaxDivisor = fdl[^2];
            }

            if (loopMaxDivisor > fdl[^2])
                loopMaxDivisor = fdl[^2];

            ulong divisorFillLevel = 0;
            ulong divisorsFillPosition = 1; // don't use prime#0 (2).
            while (divisorFillLevel < loopMaxDivisor)
            {
                divisorFillLevel = PopulateDivisor(fdl, offsets, loopMinCheckedValue, divisorsFillPosition);
                divisorsFillPosition++;
            }

            grl.LoudReportGap("AfterNewStreamWriter"); // log how long it took to maintain/grow the divisor offsets.

            //// markup the array. (use the divisor array, update the divisor array)
            var bytes00 = new byte[Two28];
            //var offsetsAnvil = new uint[7];

            //the anvil process does not maintain these values, they need to be recalculated every loop.
            //for (ulong i = 1; i < 7; i++) PopulateDivisor(fdl, offsetsAnvil, loopMinCheckedValue, i);

            //var offset = GetAnvilPosition(offsetsAnvil);
            //const ulong modX = 3 * 5 * 7 * 11 * 13 * 17;
            var notOffset = loopMinCheckedValue / 2 % AnvilSize;
            while (notOffset % 8 != 0)
                notOffset += AnvilSize;
            notOffset /= 8;
            //Console.WriteLine($"{offset}:{notOffset}:{modX}");
            //Buffer.BlockCopy(Anvil, offset, bytes00, 0, (int)Two28);
            //var bytes000 = new byte[Two28];
            Buffer.BlockCopy(Anvil, (int)notOffset, bytes00, 0, (int)Two28);
            //var cmp=ArrayCompare(bytes00, bytes000);
            //bytes000 = null;
            //GC.Collect();
            //Console.WriteLine($"compare={cmp}");
            grl.LoudReportGap("AfterBlockCopy"); // log how long it takes to put in the anvil.

            const ulong divisorPosition = 9;
            SieveProcess(loopMinCheckedValue, fdl, offsets, divisorsFillPosition, divisorPosition, bytes00);
            grl.LoudReportGap("AfterSieve"); // log how long it took to apply the other divisors

            var prime = loopMaxCheckedValue;
            // analyze the array.
            for (uint i = 0; i < Two28; i++)
            {
                if (bytes00[i] == 255) continue; //all eight bits are set, we can move on to the next byte.
                for (var l = 0; l < 8; l++)
                {
                    if (IsBitSet(bytes00[i], l)) continue;
                    //if ((bytes00[i] & (1 << l)) != 0) continue;
                    prime = loopMinCheckedValue + 16 * i + 2 * (ulong)l + 1;
                    grl.ReportGap(prime);
                }
            }

            // ReSharper disable once RedundantAssignment
            // affirmatively manage memory on this.
            bytes00 = null;
            GC.Collect();

            if (a + 1 == v2)
                grl.ReportGap(loopMaxCheckedValue); // puts an odd length gap at the end.
            /*
            else
                lastCheckedPrime = prime;
            */
            var gapFile = new StreamWriter(_basePath + "GapPrimes." + a + "." + now + ".log", false);
            grl.LastPrime(prime, gapFile);

            // report on the array.
            var gapsFile = new StreamWriter(_basePath + "GapArray." + a + "." + now + ".log", false);
            grl.ReportGaps(gapsFile);
            gapReportCarryState = grl.State;
        }
    }

    private static void QuickCheck(uint[] fdl, string now, IDictionary<uint, uint> xd)
    {
        if (string.IsNullOrWhiteSpace(_quickCheck)) return;

        var quickChecks = _quickCheck.Split(',');
        var tasks = new List<Task>();

        foreach (var quickCheckStr in quickChecks)
        {
            var didQuickCheck = ulong.TryParse(quickCheckStr, out var quickCheck);
            var quickCheckBlock = (uint)(quickCheck / Two32);

            if (_allowQuickCheckBailout)
            {
                if (!didQuickCheck)
                    continue;

                if (xd.ContainsKey(quickCheckBlock))
                    continue;
            }

            xd.TryAdd(quickCheckBlock, quickCheckBlock);

            ManageTasks(tasks);

            StartNewSpacer();
            var taskE = Task.Factory.StartNew(() =>
                ProcessNumberBlocks(fdl, quickCheckBlock, quickCheckBlock + 1, now));
            tasks.Add(taskE);
        }

        while (tasks.Count > 0)
        {
            tasks[0].Wait();
            ManageTasks(tasks);
        }

        Console.WriteLine("Done with quickCheck.  Press key to continue.");
        if (Console.KeyAvailable)
            Console.ReadKey();
    }

    private static void StartUpSieve(List<byte[]> arrays, int arrayCount, ulong arraySize, ulong prime)
    {
        var arraySize16 = arraySize * 16;
        var tasks = new List<Task>();
        var nextMark = prime * prime;
        //MarkAnArray(arraySize, prime, nextMark, arrays[0]);
        var newTask = Task.Factory.StartNew(() => MarkAnArray(arraySize, prime, nextMark, arrays[0]));
        tasks.Add(newTask);
        for (var i = 1; i < arrayCount; i++)
        {
            var array = arrays[i];
            var iArraySize16 = (ulong)i * arraySize16;
            var mulUnder = iArraySize16 / prime; // largest multiplier of prime <= i*arraySize*8
            var mulOver = (mulUnder + 1) * prime; // first multiple of prime > i*arraySize*8

            if (mulOver % 2 == 0) mulOver += prime; // don't start on an even.
            if (mulOver < nextMark) mulOver = nextMark; // use the square of mark if it is higher.

            var nextMarkL = mulOver - iArraySize16; // how far into the array that lands.
            //MarkAnArray(arraySize, prime, nextMarkL, array);
            //var ip = i;
            var newTaskL = Task.Factory.StartNew(() => MarkAnArray(arraySize, prime, nextMarkL, array));
            tasks.Add(newTaskL);
        }

        //wait for them to end.
        foreach (var task in tasks)
            task.Wait();
    }
}

public class PrimeDivisor
{
    public ulong Prime { get; set; }
    public uint PrimeByte { get; set; }
    public byte PrimeBit { get; set; }

    public uint Prime2Byte { get; set; }
    public byte Prime2Bit { get; set; }

}