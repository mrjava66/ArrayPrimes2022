using System.Configuration;
using System.Reflection;
using ArrayPrimes2022.Sieve;

#pragma warning disable IDE0075 // Conditional Expression can be simplified

namespace ArrayPrimes2022;

internal static class ProgramClass
{
    private const ulong Two32 = (ulong)uint.MaxValue + 1; //read as 2 to the 32
    private const ulong Two31 = Two32 / 2;
    private const ulong Two30 = Two31 / 2;
    private const ulong Two28 = Two30 / 4;

    private static string _quickCheck = string.Empty; // use values in the config to check a specific range

    // run the entire number line in order, instead of the places where the big gaps are.
    private static bool _runAllBlocksInOrder = true;

    private static bool _lessRamMemory; // make-use just the primary anvil.
    private static bool _getPreviousWork; // retrieve previous work and not rerun old blocks.
    private static bool _allowQuickCheckBailout = true; // quick check does not rerun old blocks.
    private static bool _reverse; // run the number line in reverse down from 2^64
    private static string _basePath = ""; // where the files are.

    // limits the starting of tasks to one new task per 3.6 seconds
    private static DateTime _startTimeSpacer = DateTime.Now;

    private static readonly TimeSpan StartTimeSpacing = TimeSpan.FromMilliseconds(3600);

    /// <summary>
    ///     the arrays that holds all the possible arrangements of the first 7-21 divisors that we will need.
    /// </summary>
    private static readonly List<byte[]> Anvils = new();
    private static readonly List<ulong> AnvilSizes = new();
    private static uint _anvilDivisorPosition = 20;

    //     The minimum value to check.
    private static ulong _minvalueNumber;

    //helps separate computers work together
    private static ulong _blockOffsetConfig;

    private static ulong _blockOffset;

    public static int TaskLimit => _taskLimit;

    /// <summary>
    ///     How many tasks to run concurrently.  Normally overriden in the .config.
    /// </summary>
    private static int _taskLimit = 3;

    // List of Blocks of 128*Two32 numbers (1/2T) that contain a first gap.
    // This list REQUIRES a block size of 128 to be correct.
    // ReSharper disable once ArrangeObjectCreationWhenTypeEvident
    private static readonly List<uint> Guided =
    [
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
    ];

    private static readonly TimeSpan GetPreviousWorkCadence = new(1, 0, 0);
    private static DateTime _getPreviousWorkTime = DateTime.Now + GetPreviousWorkCadence;
    private static readonly ISieveBackend CpuSieveBackend = new CpuSieveBackend(Two28);
    private static readonly ISieveBackend GpuSieveBackend = new ComputeSharpSieveBackend();
    private static ISieveBackend _activeSieveBackend = CpuSieveBackend;
    private static bool _gpuFallbackTriggered;

    static ProgramClass()
    {
    }

    public static bool BigArray { get; private set; } //controls if the two-dimensional gap array is made and displayed.

    /// <summary>
    /// if (_lessRamMemory) only builds 3-17.  This is the "anvil" that we will use to quickly set bits for all the divisors in the main loop.
    /// else builds 3-73, which is enough to cover all the divisors we will need for the main loop.
    /// Building the anvil is a bit time-consuming, but it saves a lot of time in the main loop, so we do it at the start and then reuse it.
    /// </summary>
    private static void BuildAllAnvils()
    {
        //build the full list
        //2,
        //3, 5, 7, 11, 13, 17, 19,
        //23, 29, 31, 37, 41,
        //43, 47, 53, 59,
        //61, 67, 71, 73,
        var dl = new[] { 2, 3, 5, 7, 11, 13, 17, 19, 23, 29, 31, 37, 41, 43, 47, 53, 59, 61, 67, 71, 73 };

        var taskBuildAnvil0 = Task.Factory.StartNew(() => BuildAnvilLayer(dl, 1, 7));
        taskBuildAnvil0.Wait();

        Anvils.Add(taskBuildAnvil0.Result.Item1);
        AnvilSizes.Add(taskBuildAnvil0.Result.Item2);
        _anvilDivisorPosition = 7;

        if (_lessRamMemory) return;

        var taskBuildAnvil1 = Task.Factory.StartNew(() => BuildAnvilLayer(dl, 8, 12));
        var taskBuildAnvil2 = Task.Factory.StartNew(() => BuildAnvilLayer(dl, 13, 16));
        var taskBuildAnvil3 = Task.Factory.StartNew(() => BuildAnvilLayer(dl, 17, 20));
        taskBuildAnvil1.Wait();
        taskBuildAnvil2.Wait();
        taskBuildAnvil3.Wait();
        Anvils.Add(taskBuildAnvil1.Result.Item1);
        AnvilSizes.Add(taskBuildAnvil1.Result.Item2);
        Anvils.Add(taskBuildAnvil2.Result.Item1);
        AnvilSizes.Add(taskBuildAnvil2.Result.Item2);
        Anvils.Add(taskBuildAnvil3.Result.Item1);
        AnvilSizes.Add(taskBuildAnvil3.Result.Item2);
        _anvilDivisorPosition = 20;
    }

    /// <summary>
    /// Constructs an anvil, which is a data structure representing all possible arrangements 
    /// of divisors within a specified range. This method is used to generate a byte array 
    /// and its corresponding size based on the provided divisor list and range.
    /// This array can be used as a mask to quickly set which numbers are divisible by the given divisors.
    /// </summary>
    /// <param name="dl">The array of divisors used to build the anvil.</param>
    /// <param name="start">The starting index of the range within the divisor list.</param>
    /// <param name="end">The ending index of the range within the divisor list.</param>
    /// <returns>
    /// A tuple containing:
    /// <list type="bullet">
    /// <item>A byte array representing the constructed anvil.</item>
    /// <item>An unsigned long value indicating the size of the anvil.</item>
    /// </list>
    /// </returns>
    private static (byte[], ulong) BuildAnvilLayer(int[] dl, int start, int end)
    {
        ulong divisorSize = 1;
        for (var i = start; i <= end; i++)
            divisorSize *= (ulong)dl[i];

        var fullSize = divisorSize * (8 + 1);
        fullSize += Two28;
        var anvil = new byte[fullSize];

        for (var divisorPosition = start; divisorPosition <= end; divisorPosition++)
        {
            var p = dl[divisorPosition];
            var primeByte = (ulong)p / 8;
            var primeBit = p % 8;
            //var offsetByte = (ulong)0;
            //var offsetBit = 0;
            var offsetByte = (ulong)((p - 1) / 2 / 8);
            var offsetBit = (p - 1) / 2 % 8;

            while (offsetByte < fullSize)
            {
                var bib = (byte)(1 << offsetBit);

                if ((anvil[offsetByte] & bib) == 0)
                    anvil[offsetByte] |= bib;

                offsetByte += primeByte;
                offsetBit += primeBit;
                if (offsetBit < 8) continue;
                offsetBit -= 8;
                offsetByte++;
            }
        }

        return (anvil, divisorSize);
    }

    /// <summary>
    /// Reads all application settings from the .config file and applies them to the
    /// static fields that govern runtime behaviour: array sizes, task concurrency,
    /// block ordering, quick-check range, reverse mode, and the per-machine block
    /// offset used to co-ordinate multiple computers working on the same number line.
    /// Prints a summary of the resolved configuration to stdout.
    /// </summary>
    private static void LoadConfiguration()
    {
        var bigArrayString = ConfigurationManager.AppSettings["BigArray"] ?? "true";
        // ReSharper disable once SimplifyConditionalTernaryExpression
        BigArray = bool.TryParse(bigArrayString, out var bigArray) ? bigArray : true;

        var getPreviousWorkString = ConfigurationManager.AppSettings["GetPreviousWork"] ?? "true";
        // ReSharper disable once SimplifyConditionalTernaryExpression
        _getPreviousWork = bool.TryParse(getPreviousWorkString, out var getPreviousWork) ? getPreviousWork : true;

        var lessRamMemoryString = ConfigurationManager.AppSettings["LessRamMemory"] ?? "false";
        // ReSharper disable once SimplifyConditionalTernaryExpression
        _lessRamMemory = bool.TryParse(lessRamMemoryString, out var lessRamMemory) ? lessRamMemory : false;

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
                // a task limit value less than 0 "number to leave free" rather than "number to use".
                // So we add the number to leave free to the number of processors to get the number to use.
                _taskLimit = Environment.ProcessorCount + taskLimitInt;
                // but if the number left over is less than 1, we just use all but one processor, to actually have worker processes.
                if (_taskLimit < 1)
                    _taskLimit = Environment.ProcessorCount - 1;
            }
        
        try
        {
            var taskLimitSet = ConfigurationManager.AppSettings["taskLimitSet"] ?? "";
            var me = Environment.MachineName.ToLower() + ",";
            var loc = taskLimitSet.ToLower().IndexOf(me, StringComparison.Ordinal);
            if (loc >= 0)
            {
                var numString = taskLimitSet.Substring(loc + me.Length).Split(";")[0];
                var didTaskMe = int.TryParse(numString, out var taskMe);
                if (didTaskMe)
                    _taskLimit = taskMe;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        var gpuMultiplierStr = ConfigurationManager.AppSettings["GpuMultiplier"] ?? "1.0";
        var didGetGpuMultiplier = float.TryParse(gpuMultiplierStr, out var gpuMultiplier);
        if (didGetGpuMultiplier)
        {
            ComputeSharpSieveBackend.GpuMultiplier = gpuMultiplier;
        }

        var maxSimultaneousAllocateAndDispatchSieveBuffers = ConfigurationManager.AppSettings["MaxSimultaneousAllocateAndDispatchSieveBuffers"] ?? "6";
        if (int.TryParse(maxSimultaneousAllocateAndDispatchSieveBuffers, out var maxBuffers))
        {
            ComputeSharpSieveBackend.MaxSimultaneousAllocateAndDispatchSieveBuffers = maxBuffers;
        }
        else
        {
            ComputeSharpSieveBackend.MaxSimultaneousAllocateAndDispatchSieveBuffers = _taskLimit;
        }

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

        var sieveBackendSetting = (ConfigurationManager.AppSettings["SieveBackend"] ?? "gpu").ToLowerInvariant();
        _activeSieveBackend = sieveBackendSetting switch
        {
            "cpu" => CpuSieveBackend,
            "gpu" => GpuSieveBackend,
            "computesharp" => GpuSieveBackend,
            _ => GpuSieveBackend
        };
        _gpuFallbackTriggered = false;

        var minvalueString = (ConfigurationManager.AppSettings["MinValue"] ?? "").Replace(",", "");

        if (ulong.TryParse(minvalueString, out var ulongResult))
            _minvalueNumber = ulongResult;
        else if (double.TryParse(minvalueString, out var doubleResult)) _minvalueNumber = (ulong)doubleResult;

        try
        {
            var blockOffsetString = (ConfigurationManager.AppSettings["BlockOffset"] ?? "").ToLower();
            var me = Environment.MachineName.ToLower() + ",";
            var loc = blockOffsetString.IndexOf(me, StringComparison.Ordinal);
            if (loc >= 0)
            {
                var numString = blockOffsetString.Substring(loc + me.Length).Split(";")[0];
                var didBlockOffset = ulong.TryParse(numString, out var blockOffset);
                if (didBlockOffset)
                {
                    _blockOffsetConfig = blockOffset;
                    _blockOffset = blockOffset;
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Block Offset Error");
            Console.Error.WriteLine("Block Offset Error");
            Console.Error.WriteLine(e.Message);
        }

        var linkTimeLocal = Assembly.GetExecutingAssembly().GetLinkerTime();

        var quickCheckReport = string.IsNullOrWhiteSpace(_quickCheck) ? "_blank_" : _quickCheck;
        Console.WriteLine($"BuildDate={linkTimeLocal},\n" +
                          $"BigArray={BigArray},\n" +
                          $"GetPreviousWork={_getPreviousWork},\n" +
                          $"LessRamMemory={_lessRamMemory},\n" +
                          $"basePath={_basePath},\n" +
                          $"TaskLimit={_taskLimit},\n" +
                          $"BlockOrder={blockOrder},RunBlocksInOrder={_runAllBlocksInOrder},\n" +
                          $"QuickCheck={quickCheckReport},\n" +
                          $"Reverse={_reverse},\n" +
                          $"BlockOffset={_blockOffset},\n" +
                          $"SieveBackend={sieveBackendSetting},\n" +
                          $"GpuMultiplier={gpuMultiplier},\n" +
                          $"SimultaneousSieveBuffers={maxBuffers}");
    }

    /// <summary>
    /// Scans the output directory for existing GapArray log files and returns a dictionary
    /// whose keys are the block numbers that have already been processed. Block 0 is always
    /// included so the sieve-seed block is never re-run. When <see cref="_getPreviousWork"/>
    /// is false the scan is skipped and only block 0 is returned.
    /// </summary>
    /// <returns>A dictionary mapping completed block numbers to <see langword="true"/>.</returns>
    private static Dictionary<uint, bool> LoadCompletedBlocks()
    {
        var xd = new Dictionary<uint, bool> { { 0, true } };

        if (!_getPreviousWork)
        {
            Console.WriteLine($"Found Existing work. Files={xd.Count}. Not looking. {DateTime.Now}");
            return xd;
        }

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
            if (!xd.Keys.Contains(intInt)) xd.TryAdd(intInt, true);
        }

        Console.WriteLine($"Found Existing work. Files={xd.Count}. {DateTime.Now}");

        return xd;
    }

    /// <summary>
    /// Returns the next block number from the <see cref="Guided"/> list that is greater than
    /// or equal to <paramref name="runBlock"/>. This steers the main loop towards blocks that
    /// are known to contain first-occurrence prime gaps, rather than running every block
    /// in strict numerical order when <see cref="_runAllBlocksInOrder"/> is false.
    /// </summary>
    private static uint GetNextGuidedBlock(uint runBlock)
    {
        foreach (var retval in Guided)
            if (retval >= runBlock)
                return retval;

        return runBlock;
    }

    /// <summary>
    /// Returns <see langword="true"/> when bit <paramref name="pos"/> (0 = LSB) of
    /// <paramref name="b"/> is set.
    /// </summary>
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
            LoadConfiguration();

            var start = DateTime.Now;
            var taskBuildAnvil = StartAnvilBuildTask();
            var fullDivisorList = new uint[203280222];
            BuildFullDivisorList(fullDivisorList, now);
            Console.WriteLine($"Base Arrays {(DateTime.Now - start).TotalSeconds}");
            GC.Collect();
            taskBuildAnvil.Wait();
            Console.WriteLine($"Anvil {(DateTime.Now - start).TotalSeconds}");
            // ReSharper disable once RedundantAssignment
            taskBuildAnvil = null;
            GC.Collect();

            //each block represents 128*2^32 values.  blocks are numbered from 0.
            const uint blockAssignmentSize = 128;
            const uint firstBlock = 0;
            const uint lastBlock = (uint)(Two32 / blockAssignmentSize);

            var tasks = new List<Task>();

            //build a list of previous work.
            var xd = LoadCompletedBlocks();

            QuickCheck(fullDivisorList, now, xd);
            var minBlock = (uint)(_minvalueNumber / (Two32 * blockAssignmentSize));

            if (_reverse)
                for (var value = uint.MaxValue; value > 0; value--)
                {
                    if (xd.Keys.Contains(value))
                        continue;

                    PruneCompletedTasks(tasks);

                    EnforceTaskStartSpacing();
                    var val = value;
                    var taskE = Task.Factory.StartNew(() =>
                        SieveNumberBlocks(fullDivisorList, val, val + 1, now));
                    tasks.Add(taskE);
                    xd.TryAdd(val, true);
                }

            for (var runBlock = firstBlock; runBlock < lastBlock; runBlock++)
            {
                if (runBlock < minBlock) continue;

                runBlock = _runAllBlocksInOrder ? runBlock : GetNextGuidedBlock(runBlock);

                var block = runBlock;

                var minvalue = block * blockAssignmentSize;
                var maxvalue = (block + 1) * blockAssignmentSize;
                if (maxvalue == 0) maxvalue--; //special code for the very last block.  maxvalue=max uint32.

                while (minvalue < maxvalue)
                {
                    if (!xd.Keys.Contains(minvalue)) break;
                    minvalue++;
                }

                /*
                while (minvalue < maxvalue)
                {
                    if (!xd.Keys.Contains(maxvalue-1)) break;
                    maxvalue--;
                }
                */

                if (minvalue == maxvalue) continue;

                if (_blockOffset > 0)
                {
                    _blockOffset--;
                    continue;
                }

                PruneCompletedTasks(tasks);

                if (ShouldStop())
                    break;

                RefreshCompletedBlocksCache(ref xd, firstBlock, runBlock, lastBlock, minBlock, blockAssignmentSize);

                EnforceTaskStartSpacing();
                var taskE = Task.Factory.StartNew(() => SieveNumberBlocks(fullDivisorList, minvalue, maxvalue, now));
                tasks.Add(taskE);
                //SieveNumberBlocks(fullDivisorList, minvalue, maxvalue, now);
                for (var i = minvalue; i < maxvalue; i++)
                    xd.TryAdd(i, true);
            }

            DrainAllTasks(tasks);

            Console.WriteLine($"Tasks to complete. {tasks.Count} left. {DateTime.Now}");
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

    // Waits for all tasks in the provided list to complete, while
    // printing the number of remaining tasks and the current time
    // each time one has completed. The method also calls
    // <see cref="PruneCompletedTasks"/> to perform any necessary task management
    // operations and decrements the <see cref="_taskLimit"/> as
    // it runs down the list.
    private static void DrainAllTasks(List<Task> tasks)
    {
        //
        _taskLimit = tasks.Count + 1;
        var lastOutput = int.MaxValue;
        //wait for all tasks to complete.
        while (tasks.Count > 0)
        {
            if (tasks.Count != lastOutput)
            {
                lastOutput = tasks.Count;
                Console.WriteLine($"Waiting for tasks to complete. {tasks.Count} left. {DateTime.Now}");
            }
            PruneCompletedTasks(tasks);
            _taskLimit--;
        }
    }

    /// <summary>
    /// Reads the live .config file to decide whether this instance should stop scheduling
    /// new blocks. Supports a simple boolean "Stop" key as well as a "Stops" key containing
    /// per-machine schedules in the form "machinename,offHour,onHour,offDay,onDay" separated
    /// by semicolons. Returns <see langword="true"/> when the current time falls within a
    /// stop window for this machine.
    /// </summary>
    private static bool ShouldStop()
    {
        try
        {
            var mConfiguration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var value = mConfiguration.AppSettings.Settings["Stop"];
            var didValue = bool.TryParse(value.Value, out var valueBool);
            if (!didValue)
                Console.WriteLine("Could not read 'Stop' value.");
            if (didValue && valueBool)
                return true;
            var values = mConfiguration.AppSettings.Settings["Stops"].Value;
            if (string.IsNullOrWhiteSpace(values))
            {
                Console.WriteLine("Could not read 'Stops' value.");
                return false;
            }
            var valueArray = values.Split(';');
            if (valueArray.Length == 0)
            {
                Console.WriteLine("Could not parse 'Stops' value.");
                return false;
            }
            var me = Environment.MachineName.ToLower() + ",";
            foreach (var xValue in valueArray)
            {
                if (string.IsNullOrWhiteSpace(xValue))
                    continue;
                if (!xValue.ToLower().StartsWith(me))
                    continue;
                var xValues = xValue.Split(",");

                var off = xValues[1];
                var on = xValues[2];
                var offDay = xValues[3];
                var onDay = xValues[4];

                var didOff = int.TryParse(off, out var offInt);
                var didOn = int.TryParse(on, out var onInt);
                var didOffDay = int.TryParse(offDay, out var offDayInt);
                var didOnDay = int.TryParse(onDay, out var onDayInt);
                if (didOff && didOn && didOffDay && didOnDay)
                    if (offDayInt <= (int)DateTime.Now.DayOfWeek && (int)DateTime.Now.DayOfWeek <= onDayInt)
                        if (offInt <= DateTime.Now.Hour && DateTime.Now.Hour <= onInt)
                            return true;
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }

        return false;
    }

    /// <summary>
    ///     Outputs an array representing the counts of all gaps between primes,
    ///     categorized by the bit lengths of the smaller prime.
    ///     So, for each gap size, you get a count of how many times that gap size occurs in numbers of each size by the number of bits in the smaller prime.
    /// </summary>
    /// <param name="fullDivisorList">The list of divisors to process.</param>
    /// <param name="file">The output stream for writing the results.</param>
    // ReSharper disable once UnusedMember.Local
    private static void WriteGapBitGrid(uint[] fullDivisorList, TextWriter file)
    {
        var gapBitArray = new int[338, 35];
        var gapArray = new int[338];
        var bits = 0;
        ulong bitsVal = 1;
        // ReSharper disable once UselessBinaryOperation
        var bitsNextVal = 2 * bitsVal;
        ulong oldP = 2;
        foreach (var p in fullDivisorList)
        {
            if (p == 0)
                continue;
            var gap = p - oldP;
            if (gap > 1)
                gapArray[gap / 2]++;
            while (p >= bitsVal)
            {
                SnapshotGapCounts(gapArray, gapBitArray, bits);
                bits++;
                bitsVal = bitsNextVal;
                bitsNextVal = 2 * bitsVal;
            }

            oldP = p;
        }

        SnapshotGapCounts(gapArray, gapBitArray, bits);
        GapReport.WriteGapGrid(file, gapBitArray, false);
    }

    /// <summary>
    /// Copies the current per-gap counts from <paramref name="gapArray"/> into the
    /// <paramref name="bits"/>-th column of <paramref name="gapBitArray"/>, then resets
    /// <paramref name="gapArray"/> to zero ready for the next bit-length bucket.
    /// </summary>
    private static void SnapshotGapCounts(int[] gapArray, int[,] gapBitArray, int bits)
    {
        for (var j = 0; j < gapArray.Length; j++)
        {
            gapBitArray[j, bits] = gapArray[j];
            gapArray[j] = 0;
        }
    }

    /// <summary>
    ///     updates previous work dictionary 'xd' to be synced with the current files in the system
    ///     at a cadence (spacing) of GetPreviousWorkCadence.  Then, if needed, recalculates the
    ///     _blockOffset to keep this instance's work separated from other instances.
    /// </summary>
    /// <param name="xd"></param>
    /// <param name="firstBlock"></param>
    /// <param name="runBlock"></param>
    /// <param name="lastBlock"></param>
    /// <param name="minBlock"></param>
    /// <param name="blockAssignmentSize"></param>
    private static void RefreshCompletedBlocksCache(ref Dictionary<uint, bool> xd, uint firstBlock, uint runBlock,
        uint lastBlock, uint minBlock, uint blockAssignmentSize)
    {
        if (_getPreviousWorkTime > DateTime.Now)
            //not time to try
            return;

        _getPreviousWorkTime = DateTime.Now + GetPreviousWorkCadence;
        xd = LoadCompletedBlocks();

        if (_blockOffsetConfig <= 0)
            //no separator to maintain.
            return;

        //recalculate block offset
        _blockOffset = _blockOffsetConfig;
        for (var xBlock = firstBlock; xBlock <= runBlock && xBlock < lastBlock; xBlock++)
        {
            if (xBlock < minBlock) continue;
            var xMin = xBlock * blockAssignmentSize;
            var xMax = (xBlock + 1) * blockAssignmentSize;
            while (xMin < xMax)
            {
                if (!xd.ContainsKey(xMin))
                    break;
                xMin++;
            }

            if (xMin == xMax) continue;
            if (_blockOffset > 0) _blockOffset--;
        }

        Console.WriteLine($"Block Offset set to {_blockOffset} with {runBlock}:{runBlock * 128}"); //blockAssignmentSize
    }

    /// <summary>
    ///     if time since last start is long enough, go.
    ///     otherwise, wait the spacing.
    /// </summary>
    private static void EnforceTaskStartSpacing()
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
    private static void BuildFullDivisorList(uint[] fdl, string now)
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
                new byte[baseArrayUnitSize]; //primes and possible primes have a bit value of 0, composites have a bit value of 1.
            arrays.Add(arrayX);
        }

        var sieveTop = (ulong)(Math.Sqrt(uint.MaxValue) + 1); // last value to sieve through the divisor array.
        var gr = new GapReport(new GapReportCarryState { GapRepeat = 0, LastGap = 0, LastPrime = 2 });

        var countPrimeNumber = 0;
        fdl[0] = 2;
        gr.RecordPrime(2);
        //don't sieve for 2
        foreach (var seedPrime in new ulong[] { 3, 5, 7, 11, 13 })
        {
            fdl[++countPrimeNumber] = (uint)seedPrime;
            gr.RecordPrime(seedPrime);
            SeedPrimeSieve(arrays, arrays.Count, baseArrayUnitSize, seedPrime);
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

                    gr.RecordPrime(prime);

                    //outfile.WriteLine(prime);
                    // don't need to sieve values greater than top.
                    if (prime < sieveTop)
                        SeedPrimeSieve(arrays, arrays.Count, baseArrayUnitSize, prime);
                }
            }

        gr.RecordPrime(baseArrayCount * arraySize16); // do an end gap.
        var gapFile = new StreamWriter(_basePath + "0\\0\\GapPrimes.0." + now + ".log", false);
        gr.WriteLastPrimeEntry(prime, gapFile); // do an end gap.
        //gr.WriteFlush(gapFile);

        var gapsFile = new StreamWriter(_basePath + "0\\0\\GapArray.0." + now + ".log", false);
        WriteGapBitGrid(fdl, gapsFile);
        gr.WriteGapSummary(gapsFile);
    }

    /// <summary>
    ///     Gets us a build anvil process that runs concurrently with building the divisor arrays.
    /// </summary>
    /// <returns></returns>
    private static Task StartAnvilBuildTask()
    {
        var taskBuildAnvil = Task.Factory.StartNew(BuildAllAnvils);
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

    /// <summary>
    /// Removes all completed tasks from <paramref name="tasks"/> then, if the active count
    /// is still at or above <see cref="_taskLimit"/>, waits up to 200 ms for the first task
    /// to finish before looping again. Returns as soon as there is capacity for a new task.
    /// </summary>
    private static void PruneCompletedTasks(List<Task> tasks)
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
            if (tasks.Count == 0) return;

            var t0 = tasks[0];
            t0.Wait(200);
        }
    }

    /// <summary>
    /// Marks all multiples of <paramref name="prime"/> in the sieve byte array
    /// <paramref name="array"/>, starting from <paramref name="nextMark"/>.
    /// The array stores only odd numbers: byte <c>by</c>, bit <c>bi</c> represents
    /// the value <c>by*16 + bi*2 + 1</c>. Increments advance by <paramref name="prime"/>
    /// positions (i.e. by <c>prime/8</c> bytes and <c>prime%8</c> bits) each step.
    /// </summary>
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

    // The main way to improve this code is to call it less. (~4B * ~1e8 calls right now.)
    /// <summary>
    /// Computes the sieve offset for the prime at position <paramref name="divisorsFillPosition"/>
    /// in the full divisor list, relative to <paramref name="loopMinCheckedValue"/>, and stores
    /// it in <paramref name="offsets"/>. The offset is the index of the first odd multiple of
    /// that prime that falls at or above <paramref name="loopMinCheckedValue"/>.
    /// Returns the prime value so the caller can track the fill watermark.
    /// </summary>
    private static uint ComputeDivisorOffset(uint[] fdl, uint[] offsets, ulong loopMinCheckedValue,
        ulong divisorsFillPosition)
    {
        var prime = fdl[divisorsFillPosition];
        var bump = prime - loopMinCheckedValue % prime;
        bump += bump % 2 == 0 ? prime : 0;
        var offset = (bump - 1) / 2;
        offsets[divisorsFillPosition] = (uint)offset;
        return prime;
    }

    /// <summary>
    /// Applies a segmented sieve over the working byte array <paramref name="bytes0"/> for
    /// all primes from index <paramref name="divisorPosition"/> up to (not including)
    /// <paramref name="divisorsFillPosition"/> in the full divisor list. For each prime the
    /// method advances two alternating step sizes — <c>p</c> and <c>2p</c> — to skip
    /// multiples of 3, marking composite bits in <paramref name="bytes0"/>. The write-guard
    /// check (<c>bytes0[by] != 255</c> before OR-ing) reduces memory-write pressure by ~30 %
    /// when running multithreaded.
    /// </summary>
    private static void SieveProcess(GapReport grl, ulong loopMinCheckedValue, uint[] fdl, uint[] offsets,
        ulong divisorsFillPosition,
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

        try
        {
            _activeSieveBackend.Execute(grl, loopMinCheckedValue, fdl, offsets, divisorsFillPosition, divisorPosition, bytes0);
        }
        catch (Exception ex) when (!ReferenceEquals(_activeSieveBackend, CpuSieveBackend))
        {
            var ex0 = ex;
            while (ex0 != null)
            {
                Console.Error.WriteLine($"Sieve backend error: {ex0.Message}");
                ex0 = ex0.InnerException;
            }

            if (!_gpuFallbackTriggered)
            {
                Console.Error.WriteLine($"Sieve backend '{_activeSieveBackend.Name}' failed. Falling back to CPU.");
                Console.Error.WriteLine(ex.Message);
                _gpuFallbackTriggered = true;
            }

            _activeSieveBackend = CpuSieveBackend;
            CpuSieveBackend.Execute(grl, loopMinCheckedValue, fdl, offsets, divisorsFillPosition, divisorPosition, bytes0);
        }
        catch (Exception ex)
        {
            var ex0 = ex;
            while (ex0 != null)
            {
                Console.Error.WriteLine($"Sieve backend error: {ex0.Message}");
                ex0 = ex0.InnerException;
            }
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
    private static void SieveNumberBlocks(uint[] fdl, uint v1, uint v2, string now)
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

        SieveSegmentAndWriteLogs(fdl, v1, v2, now, offsets, lastCheckedPrime);
    }

    /// <summary>
    /// Inner loop of the sieve: iterates over each 2^32-wide segment in the range
    /// [<paramref name="v1"/>, <paramref name="v2"/>), builds the divisor-offset table,
    /// copies the pre-computed anvil mask, blends in additional anvil layers, applies the
    /// remaining sieve divisors, then walks the result byte-by-byte to collect primes and
    /// report gaps. Writes GapPrimes and GapArray log files for each completed segment.
    /// </summary>
    private static void SieveSegmentAndWriteLogs(uint[] fdl, uint v1, uint v2, string now, uint[] offsets,
        ulong lastCheckedPrime)
    {
        // process all the primes in segment.  (a*2^32 -> (a+1)*2^32)
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
                divisorFillLevel = ComputeDivisorOffset(fdl, offsets, loopMinCheckedValue, divisorsFillPosition);
                divisorsFillPosition++;
            }

            grl.AppendTimingMark("AfterNewStreamWriter"); // log how long it took to maintain/grow the divisor offsets.

            //// markup the array. (use the divisor array, update the divisor array)
            var bytes00 = new byte[Two28];

            var notOffset = loopMinCheckedValue / 2 % AnvilSizes[0];
            while (notOffset % 8 != 0)
                notOffset += AnvilSizes[0];
            notOffset /= 8;

            Buffer.BlockCopy(Anvils[0], (int)notOffset, bytes00, 0, (int)Two28);

            grl.AppendTimingMark("AfterBlockCopy"); // log how long it takes to put in the anvil.

            if (!_lessRamMemory)
            {
                var notOffsets = new ulong[4];
                for (var i = 1; i <= 3; i++)
                {
                    var notOffsetMore = loopMinCheckedValue / 2 % AnvilSizes[i];
                    while (notOffsetMore % 8 != 0)
                        notOffsetMore += AnvilSizes[i];
                    notOffsetMore /= 8;
                    notOffsets[i] = notOffsetMore;
                }

                ApplyAnvilLayers(bytes00, notOffsets);
                grl.AppendTimingMark("AfterBlockCopies"); // log how long it takes to put in the blend in anvils.
            }

            SieveProcess(grl, loopMinCheckedValue, fdl, offsets, divisorsFillPosition, _anvilDivisorPosition, bytes00);
            grl.AppendTimingMark("AfterSieve"); // log how long it took to apply the other divisors

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
                    grl.RecordPrime(prime);
                }
            }

            // ReSharper disable once RedundantAssignment
            // affirmatively manage memory on this.
            bytes00 = null;
            GC.Collect();

            if (a + 1 == v2)
                grl.RecordPrime(loopMaxCheckedValue); // puts an odd length gap at the end.
            /*
            else
                lastCheckedPrime = prime;
            */
            var dirPath = $"{a / (1024 * 1024)}\\{a / 1024}\\";
            var dirMakePath = _basePath + dirPath;
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

            StreamWriter? gapFile = null;
            try
            {
                while (gapFile == null)
                {
                    gapFile = new StreamWriter(dirMakePath + "GapPrimes." + a + "." + now + ".log", false);
                    grl.WriteLastPrimeEntry(prime, gapFile);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }

            // report on the array.
            StreamWriter? gapsFile = null;
            try
            {
                while (gapsFile == null)
                {
                    gapsFile = new StreamWriter(dirMakePath + "GapArray." + a + "." + now + ".log", false);
                    grl.WriteGapSummary(gapsFile);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }

            gapReportCarryState = grl.State;
        }
    }

    /// <summary>
    /// OR-blends anvil layers 1, 2, and 3 into <paramref name="bytes00"/> using the
    /// pre-computed byte offsets in <paramref name="notOffsets"/>. Each anvil layer marks
    /// composites for an additional group of small primes; combining them with the primary
    /// array pre-eliminates a large fraction of candidates before the main sieve runs.
    /// Throws if any offset would read outside the bounds of its anvil array.
    /// </summary>
    private static void ApplyAnvilLayers(byte[] bytes00, ulong[] notOffsets)
    {
        for (var j = 1; j <= 3; j++)
        {
            if (notOffsets[j] + Two28 > int.MaxValue)
                throw new Exception($"Cannot work with offset0,{j},{notOffsets[j]}");
            if ((long)notOffsets[j] + bytes00.LongLength > Anvils[j].LongLength)
                throw new Exception(
                    $"Cannot work with offset1,{j},{notOffsets[j]},{bytes00.LongLength},{Anvils[j].LongLength}");
            if (notOffsets[j] > AnvilSizes[j])
                throw new Exception($"Cannot work with offset2,{notOffsets[j]},{AnvilSizes[j]}");
        }

        var a1 = (int)notOffsets[1];
        var a2 = (int)notOffsets[2];
        var a3 = (int)notOffsets[3];
        for (var i = 0; i < bytes00.Length; i++)
        {
            var b = bytes00[i];
            b |= Anvils[1][a1 + i];
            b |= Anvils[2][a2 + i];
            b |= Anvils[3][a3 + i];
            if ((b & bytes00[i]) != 0)
                bytes00[i] = b;
        }
    }

    /// <summary>
    /// Processes the comma-separated block numbers listed in <see cref="_quickCheck"/>
    /// immediately at startup, bypassing normal block ordering. Used to re-verify or
    /// fill in specific blocks of interest. When <see cref="_allowQuickCheckBailout"/> is
    /// true, blocks that already appear in <paramref name="xd"/> are skipped. Waits for
    /// all quick-check tasks to finish before returning.
    /// </summary>
    private static void QuickCheck(uint[] fdl, string now, IDictionary<uint, bool> xd)
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

            xd.TryAdd(quickCheckBlock, true);

            PruneCompletedTasks(tasks);

            EnforceTaskStartSpacing();
            var taskE = Task.Factory.StartNew(() =>
                SieveNumberBlocks(fdl, quickCheckBlock, quickCheckBlock + 1, now));
            tasks.Add(taskE);
        }

        while (tasks.Count > 0)
        {
            tasks[0].Wait();
            PruneCompletedTasks(tasks);
        }

        Console.WriteLine("Done with quickCheck.  Press key to continue.");
        if (Console.KeyAvailable)
            Console.ReadKey();
    }

    /// <summary>
    /// Marks all multiples of <paramref name="prime"/> across every segment array in
    /// <paramref name="arrays"/>. Each array covers a different 16*<paramref name="arraySize"/>-
    /// wide window of the number line; the starting multiple for each window is calculated
    /// so that the mark begins at the correct position relative to the window's base address.
    /// All marking tasks are run in parallel and awaited before returning.
    /// </summary>
    private static void SeedPrimeSieve(List<byte[]> arrays, int arrayCount, ulong arraySize, ulong prime)
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

public static class LinkTime
{
    /// <summary>
    /// Reads the PE header of <paramref name="assembly"/>'s file to extract the linker
    /// timestamp embedded at compile time. Returns the time converted to
    /// <paramref name="target"/> (defaulting to the local time zone), or
    /// <see cref="DateTime.MinValue"/> if the file is too short to contain a valid header.
    /// </summary>
    public static DateTime GetLinkerTime(this Assembly assembly
        , TimeZoneInfo? target = null)
    {
        var filePath = assembly.Location;
        const int cPeHeaderOffset = 60;
        const int cLinkerTimestampOffset = 8;

        var buffer = new byte[2048];
        int read;

        using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
        {
            read = stream.Read(buffer, 0, 2048);
        }

        if (read < 2048)
            return DateTime.MinValue;

        var offset = BitConverter.ToInt32(buffer, cPeHeaderOffset);
        var secondsSince1970 = BitConverter.ToInt32(buffer, offset + cLinkerTimestampOffset);
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var linkTimeUtc = epoch.AddSeconds(secondsSince1970);

        var tz = target ?? TimeZoneInfo.Local;
        var localTime = TimeZoneInfo.ConvertTimeFromUtc(linkTimeUtc, tz);

        return localTime;
    }
}
