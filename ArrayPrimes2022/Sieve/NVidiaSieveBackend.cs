using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;

using SieveKernel1DDelegate = System.Action<ILGPU.Runtime.AcceleratorStream,
    ILGPU.Index1D,
    ILGPU.ArrayView<uint>,
    ILGPU.ArrayView<int>,
    ILGPU.ArrayView<int>,
    ILGPU.ArrayView<int>,
    ILGPU.ArrayView<int>,
    ILGPU.ArrayView<int>,
    ILGPU.ArrayView<int>,
    ILGPU.ArrayView<int>,
    //ILGPU.ArrayView<int>,
    //ILGPU.ArrayView<int>,
    int, int, int>;

using SieveKernel2DDelegate = System.Action<ILGPU.Runtime.AcceleratorStream,
    ILGPU.Index2D,
    ILGPU.ArrayView<uint>,
    ILGPU.ArrayView<int>,
    ILGPU.ArrayView<int>,
    ILGPU.ArrayView<int>,
    ILGPU.ArrayView<int>,
    ILGPU.ArrayView<int>,
    ILGPU.ArrayView<int>,
    ILGPU.ArrayView<int>,
    //ILGPU.ArrayView<int>,
    //ILGPU.ArrayView<int>,
    int, int, int, int, int, int>;

namespace ArrayPrimes2022.Sieve;

internal sealed class NVidiaSieveBackend : ISieveBackend, IDisposable
{
    private static readonly SemaphoreSlim SieveBufferSemaphore = new(1, 1);

    private readonly Context? _context;
    private readonly CudaAccelerator? _accelerator;
    private readonly SieveKernel1DDelegate? _kernel;
    private readonly SieveKernel2DDelegate? _kernel2D;

    public bool IsAvailable => _context != null && _accelerator != null;

    public string Name => "NVIDIA CUDA";

    private static int _computeUnits;
    private static readonly TimeSpan TimeWaitGoal = new(0, 0, 0, 2);
    private static TimeSpan _preworkTime;

    public NVidiaSieveBackend()
    {
        // Try to create CUDA accelerator (will throw if no devices)
        try
        {
            _context = Context.CreateDefault();

            _accelerator = _context.CreateCudaAccelerator(0);

            _computeUnits = _accelerator.MaxNumThreadsPerMultiprocessor * _accelerator.NumMultiprocessors;

            Console.WriteLine($"Chosen NVIDIA device: {_accelerator.Name}");
            Console.WriteLine($"\twith {_accelerator.NumMultiprocessors} multiprocessors");
            Console.WriteLine($"\twith {_accelerator.MaxNumThreadsPerMultiprocessor} max threads per multiprocessor");
            Console.WriteLine($"\twith {_accelerator.MemorySize:N0} bytes of memory");
            Console.WriteLine($"\twith {_accelerator.WarpSize} warp size");
            Console.WriteLine($"\tTotal compute units: {_computeUnits}");

            _kernel = _accelerator.LoadAutoGroupedKernel<
                Index1D,
                ArrayView<uint>,
                ArrayView<int>,
                ArrayView<int>,
                ArrayView<int>,
                ArrayView<int>,
                ArrayView<int>,
                ArrayView<int>,
                ArrayView<int>,
                //ILGPU.ArrayView<int>,
                //ILGPU.ArrayView<int>,
                int,
                int,
                int>(SieveKernel);

            _kernel2D = _accelerator.LoadAutoGroupedKernel<
                Index2D,
                ArrayView<uint>,
                ArrayView<int>,
                ArrayView<int>,
                ArrayView<int>,
                ArrayView<int>,
                ArrayView<int>,
                ArrayView<int>,
                ArrayView<int>,
                //ILGPU.ArrayView<int>,
                //ILGPU.ArrayView<int>,
                int,
                int,
                int,
                int,
                int,
                int>(SieveKernel2D);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"No CUDA-capable NVIDIA devices found. {ex.Message}");
        }
    }

    public void Execute(
        GapReport grl,
        ulong loopMinCheckedValue,
        uint[] fullDivisorList,
        uint[] offsets,
        ulong divisorsFillPosition,
        ulong divisorPosition,
        byte[] sieveBytes)
    {
        var divisorCount = checked((int)(divisorsFillPosition - divisorPosition));
        if (divisorCount <= 0)
            return;

        var startBytes = new int[divisorCount];
        var startBits = new int[divisorCount];
        var shortStepBytes = new int[divisorCount];
        var shortStepBits = new int[divisorCount];
        var longStepBytes = new int[divisorCount];
        var longStepBits = new int[divisorCount];
        var startsWithLongStep = new int[divisorCount];

        for (var i = 0; i < divisorCount; i++, divisorPosition++)
        {
            var p = fullDivisorList[divisorPosition];
            var o = offsets[divisorPosition];
            var primeByte = (int)(p / 32);
            var primeBit = (int)(p % 32);
            var offsetByte = (int)(o / 32);
            var offsetBit = (int)o % 32;

            var primeByteOn = primeByte + primeByte;
            var primeBitOn = primeBit + primeBit;
            if (primeBitOn >= 32)
            {
                primeBitOn -= 32;
                primeByteOn++;
            }

            var markLoc = loopMinCheckedValue + 64UL * (ulong)offsetByte + 2UL * (ulong)offsetBit + 1;
            if (markLoc % 3 == 0)
            {
                offsetByte += primeByte;
                offsetBit += primeBit;
                if (offsetBit >= 32)
                {
                    offsetBit -= 32;
                    offsetByte++;
                }

                markLoc = loopMinCheckedValue + 64UL * (ulong)offsetByte + 2UL * (ulong)offsetBit + 1;
            }

            var onOff = (2 * p + markLoc) % 3 == 0;

            var arrayLoc = divisorCount - 1 - i; // Reverse order to improve memory access patterns on the GPU
            startBytes[arrayLoc] = offsetByte;
            startBits[arrayLoc] = offsetBit;
            shortStepBytes[arrayLoc] = primeByte;
            shortStepBits[arrayLoc] = primeBit;
            longStepBytes[arrayLoc] = primeByteOn;
            longStepBits[arrayLoc] = primeBitOn;
            startsWithLongStep[arrayLoc] = onOff ? 1 : 0;
        }

        var sieveWords = new uint[sieveBytes.Length / sizeof(uint)];
        Buffer.BlockCopy(sieveBytes, 0, sieveWords, 0, sieveBytes.Length);

        AllocateAndDispatchSieveBuffers(
                grl, sieveWords,
                startBytes, startBits,
                shortStepBytes, shortStepBits,
                longStepBytes, longStepBits,
                startsWithLongStep, divisorCount);

        Buffer.BlockCopy(sieveWords, 0, sieveBytes, 0, sieveBytes.Length);
    }

    private void AllocateAndDispatchSieveBuffers(
        GapReport grl,
        uint[] sieveWords,
        int[] startBytes,
        int[] startBits,
        int[] shortStepBytes,
        int[] shortStepBits,
        int[] longStepBytes,
        int[] longStepBits,
        int[] startsWithLongStep,
        int divisorCount)
    {
        if (!IsAvailable || _accelerator == null || _kernel == null || _kernel2D == null)
            throw new InvalidOperationException("CUDA accelerator is not available.");

        grl.AppendTimingMark("BeforeSemaphoreWait");
        var semaphore = SieveBufferSemaphore;
        var timeBeforeWait = DateTime.UtcNow;
        semaphore.Wait();
        try
        {
            SetPreworkTime(DateTime.UtcNow - timeBeforeWait);
            // we want to include the time spent waiting for the semaphore in our timing, as it is part of the overall execution time of this method.
            grl.AppendTimingMark("AfterSemaphoreWait");
            using var sieveBuffer = _accelerator.Allocate1D(sieveWords);
            var sieveLength = (int)sieveBuffer.Length;
            using var startBytesBuffer = _accelerator.Allocate1D(startBytes);
            using var startBitsBuffer = _accelerator.Allocate1D(startBits);
            using var shortStepBytesBuffer = _accelerator.Allocate1D(shortStepBytes);
            using var shortStepBitsBuffer = _accelerator.Allocate1D(shortStepBits);
            using var longStepBytesBuffer = _accelerator.Allocate1D(longStepBytes);
            using var longStepBitsBuffer = _accelerator.Allocate1D(longStepBits);
            using var startsWithLongStepBuffer = _accelerator.Allocate1D(startsWithLongStep);

            //var workTry = new int[divisorCount];
            //var workDo = new int[divisorCount];
            //using var workTryBuffer = _accelerator.Allocate1D(workTry);
            //using var workDoBuffer = _accelerator.Allocate1D(workDo);

            // Dispatch the compute kernel in batches of _loopSize divisors
            var yDim = _accelerator.WarpSize;
            var xDim = _computeUnits / yDim;
            var num2DLoops = 5;
            var divisorCount1D = divisorCount - num2DLoops * xDim;
            var divisorOffset = 0;
            var loop = 0;
            var reportAt = divisorCount1D - 5 * _computeUnits;
            var maxBatchSize = _computeUnits * _accelerator.MaxNumThreadsPerGroup;
            grl.AppendTimingMark("After buffer allocation");
            do
            {
                loop++;

                var batchSize = Math.Min(maxBatchSize, (divisorCount1D - divisorOffset) / 2); // do half, but not more than the max for a single 1D batch
                if (batchSize < _computeUnits)
                    batchSize = _computeUnits; // but don't do less than the number of compute units.  This prevents getting stuck in this loop with very small batch sizes.
                batchSize = Math.Min(batchSize, divisorCount1D - divisorOffset); // don't run over the planned end of 1D loop sieving.

                _kernel!(
                    _accelerator.DefaultStream,
                    new Index1D(batchSize),
                    sieveBuffer.View,
                    startBytesBuffer.View,
                    startBitsBuffer.View,
                    shortStepBytesBuffer.View,
                    shortStepBitsBuffer.View,
                    longStepBytesBuffer.View,
                    longStepBitsBuffer.View,
                    startsWithLongStepBuffer.View,
                    //workTryBuffer.View,
                    //workDoBuffer.View,
                    divisorOffset,
                    divisorCount,
                    sieveLength);
                _accelerator.Synchronize();

                if (divisorOffset >= reportAt)
                {
                    var lastPrime = LastPrimeSieved(shortStepBytes, shortStepBits, divisorOffset, batchSize);
                    grl.AppendTimingMark($"After {loop} kernel(1D) call with {batchSize} divisors, last prime: {lastPrime}");
                }

                divisorOffset += batchSize;
            } while (divisorOffset < divisorCount1D);
            grl.AppendTimingMark("After all kernel(1D) calls");

            var stripe = 0;
            do
            {
                loop++;

                _kernel2D!(
                    _accelerator.DefaultStream,
                    new Index2D(xDim, yDim),
                    sieveBuffer.View,
                    startBytesBuffer.View,
                    startBitsBuffer.View,
                    shortStepBytesBuffer.View,
                    shortStepBitsBuffer.View,
                    longStepBytesBuffer.View,
                    longStepBitsBuffer.View,
                    startsWithLongStepBuffer.View,
                    //workTryBuffer.View,
                    //workDoBuffer.View,
                    divisorOffset,
                    divisorCount,
                    sieveLength,
                    yDim,
                    stripe,
                    num2DLoops);
                _accelerator.Synchronize();

                var lastPrime = LastPrimeSieved(shortStepBytes, shortStepBits, divisorOffset, num2DLoops * (xDim - 1) + stripe);
                grl.AppendTimingMark($"After Device.For {loop}, {yDim}, {xDim}(2D) call, last prime: {lastPrime}");

                stripe++;

            } while (stripe < num2DLoops);
            grl.AppendTimingMark("After all kernel(2D) calls");

            sieveBuffer.CopyToCPU(sieveWords);
            /*
            workTryBuffer.CopyToCPU(workTry);
            workDoBuffer.CopyToCPU(workDo);

            var sa = new StringBuilder();
            int divOff = 0;
            while (divOff < divisorCount)
            {
                var lastPrime = LastPrimeSieved(shortStepBytes, shortStepBits, divOff, 1);
                sa.Append($"Divisor {divOff}: Prime={lastPrime}: Try={workTry[divOff]}, Do={workDo[divOff]};");
                var delta = (divisorCount - divOff) / 2;
                if (delta < 1) delta = 1;
                divOff += delta;
            }
            grl.AppendTimingMark(sa.ToString());
            */
        }
        finally
        {
            semaphore.Release();
        }
    }

    public TimeSpan GetPreworkTime() => _preworkTime;

    private static void SetPreworkTime(TimeSpan timeWaited)
    {
        var miss = timeWaited - TimeWaitGoal;
        var divisor = ProgramClass.TaskLimit * 2;
        if (divisor <= 0)
            divisor = 10;
        _preworkTime += TimeSpan.FromMilliseconds(miss.TotalMilliseconds / divisor);
        if (_preworkTime.TotalSeconds < 0)
            _preworkTime = TimeSpan.Zero;
    }

    private static int LastPrimeSieved(int[] shortStepBytes, int[] shortStepBits, int divisorOffset, int batchSize)
    {
        var lastPos = divisorOffset + batchSize - 1;
        if (lastPos >= shortStepBytes.Length)
            lastPos = shortStepBytes.Length - 1;
        var by = shortStepBytes[lastPos];
        var bi = shortStepBits[lastPos];
        var lastPrime = by * 32 + bi;
        return lastPrime;
    }

    private static void SieveKernel(
        Index1D index,
        ArrayView<uint> sieveBytes,
        ArrayView<int> startBytes,
        ArrayView<int> startBits,
        ArrayView<int> shortStepBytes,
        ArrayView<int> shortStepBits,
        ArrayView<int> longStepBytes,
        ArrayView<int> longStepBits,
        ArrayView<int> startsWithLongStep,
        //ArrayView<int> workTry,
        //ArrayView<int> workDo,
        int divisorIndexOffset,
        int divisorCount,
        int sieveLength)
    {
        var divisorIndex = index + divisorIndexOffset;
        if (divisorIndex >= divisorCount)
            return;

        var offsetByte = startBytes[divisorIndex];
        var offsetBit = startBits[divisorIndex];
        var shortStepByte = shortStepBytes[divisorIndex];
        var shortStepBit = shortStepBits[divisorIndex];
        var longStepByte = longStepBytes[divisorIndex];
        var longStepBit = longStepBits[divisorIndex];
        var useLongStep = startsWithLongStep[divisorIndex] != 0;

        UpdateSieveBytes(sieveBytes, /*workTry, workDo, divisorIndex,*/ sieveLength, offsetByte, offsetBit, useLongStep, longStepByte, longStepBit, shortStepByte, shortStepBit);
    }

    private static void SieveKernel2D(
        Index2D index,
        ArrayView<uint> sieveBytes,
        ArrayView<int> startBytes,
        ArrayView<int> startBits,
        ArrayView<int> shortStepBytes,
        ArrayView<int> shortStepBits,
        ArrayView<int> longStepBytes,
        ArrayView<int> longStepBits,
        ArrayView<int> startsWithLongStep,
        //ArrayView<int> workTry,
        //ArrayView<int> workDo,
        int divisorIndexOffset,
        int divisorCount,
        int sieveLength,
        int yDim,
        int stripe,
        int num2DLoops)
    {
        var divisorIndex = stripe + num2DLoops * index.X + divisorIndexOffset;
        if (divisorIndex >= divisorCount)
            return;

        var offsetByte = startBytes[divisorIndex];
        var offsetBit = startBits[divisorIndex];
        var shortStepByte = shortStepBytes[divisorIndex];
        var shortStepBit = shortStepBits[divisorIndex];
        var longStepByte = longStepBytes[divisorIndex];
        var longStepBit = longStepBits[divisorIndex];
        var useLongStep = startsWithLongStep[divisorIndex] != 0;

        //revise the offset-s and sieve length for this thread based on its Y index, so that each thread processes a different portion of the sieve.
        var totalNumDoubleSteps = sieveLength / ((shortStepByte + longStepByte) + (shortStepBit + longStepBit) / 32); // Approximate total number of steps for this divisor
        var offsetStart = index.Y * totalNumDoubleSteps / yDim; // Starting offset for this thread in the double step sequence
        var offsetEnd = (1 + index.Y) * totalNumDoubleSteps / yDim; // Ending offset for this thread in the double step sequence
        offsetByte += offsetStart * (shortStepByte + longStepByte);
        offsetBit += offsetStart * (shortStepBit + longStepBit);
        //offsetByte += (int)(offsetBit / 32);
        //offsetBit %= 32;
        while (offsetBit >= 32) { offsetBit -= 32; offsetByte++; }
        var newSieveLength = offsetEnd * (shortStepByte + longStepByte);
        newSieveLength += (offsetEnd * (shortStepBit + longStepBit)) / 32;
        newSieveLength++;
        // don't run over the end.
        if (newSieveLength > sieveLength)
            newSieveLength = sieveLength;
        // make sure the last thread completes the end of the sieve.
        if (index.Y + 1 >= yDim)
            newSieveLength = sieveLength;

        UpdateSieveBytes(sieveBytes, /*workTry, workDo, divisorIndex,*/ newSieveLength, offsetByte, offsetBit, useLongStep, longStepByte, longStepBit, shortStepByte, shortStepBit);
    }

    private static void UpdateSieveBytes(ArrayView<uint> sieveBytes,
        /*ArrayView<int> workTry, ArrayView<int> workDo, int divisorIndex, */
        int sieveLength, int offsetByte, int offsetBit,
        bool useLongStep, int longStepByte, int longStepBit, int shortStepByte, int shortStepBit)
    {
        while (offsetByte < sieveLength)
        {
            var bitMask = 1u << offsetBit;
            /*var oldVal = */
            Atomic.Or(ref sieveBytes[offsetByte], bitMask);
            /*
            Atomic.Add(ref workTry[divisorIndex], 1);
            if ((oldVal & bitMask) == 0)
                Atomic.Add(ref workDo[divisorIndex], 1);
            */
            if (useLongStep)
            {
                offsetByte += longStepByte;
                offsetBit += longStepBit;
            }
            else
            {
                offsetByte += shortStepByte;
                offsetBit += shortStepBit;
            }

            if (offsetBit >= 32)
            {
                offsetBit -= 32;
                offsetByte++;
            }

            useLongStep = !useLongStep;
        }
    }

    public void Dispose()
    {
        _accelerator?.Dispose();
        _context?.Dispose();
    }
}
