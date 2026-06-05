using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;

namespace ArrayPrimes2022.Sieve;

internal sealed class NVidiaSieveBackend : ISieveBackend, IDisposable
{
    private static readonly SemaphoreSlim SieveBufferSemaphore = new(1, 1);

    private readonly Context? _context;
    private readonly CudaAccelerator? _accelerator;
    public bool IsAvailable => _context != null && _accelerator != null;

    public string Name => "NVIDIA CUDA";

    private static int _computeUnits;

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
        }
        catch (Exception ex)
        {
            Console.WriteLine($"No CUDA-capable NVIDIA devices found. {ex}");
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
        if (!IsAvailable || _accelerator == null)
            throw new InvalidOperationException("CUDA accelerator is not available.");

        grl.AppendTimingMark("BeforeOuterSemaphoreWait");
        var semaphore = SieveBufferSemaphore;
        semaphore.Wait();
        try
        {
            // we want to include the time spent waiting for the semaphore in our timing, as it is part of the overall execution time of this method.
            grl.AppendTimingMark("AfterSemaphoreWait");
            using var sieveBuffer = _accelerator.Allocate1D(sieveWords);
            var sieveLength = sieveBuffer.Length;
            using var startBytesBuffer = _accelerator.Allocate1D(startBytes);
            using var startBitsBuffer = _accelerator.Allocate1D(startBits);
            using var shortStepBytesBuffer = _accelerator.Allocate1D(shortStepBytes);
            using var shortStepBitsBuffer = _accelerator.Allocate1D(shortStepBits);
            using var longStepBytesBuffer = _accelerator.Allocate1D(longStepBytes);
            using var longStepBitsBuffer = _accelerator.Allocate1D(longStepBits);
            using var startsWithLongStepBuffer = _accelerator.Allocate1D(startsWithLongStep);

            // Load and compile the kernel (cached after first call)
            var kernel = _accelerator.LoadAutoGroupedKernel<
                Index1D,
                ArrayView<uint>,
                ArrayView<int>,
                ArrayView<int>,
                ArrayView<int>,
                ArrayView<int>,
                ArrayView<int>,
                ArrayView<int>,
                ArrayView<int>,
                int,
                int,
                int>(SieveKernel);

            // Dispatch the compute kernel in batches of _loopSize divisors
            var yDim = _accelerator.WarpSize;
            var xDim = _computeUnits / yDim;
            var num2DLoops = 2.25;
            var divisorCount1D = (int)(divisorCount - num2DLoops * xDim);
            var divisorOffset = 0;
            do
            {
                var batchSize = Math.Min(_computeUnits, divisorCount1D - divisorOffset);

                kernel(
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
                    divisorOffset,
                    divisorCount,
                    (int)sieveLength);
                _accelerator.Synchronize();

                // if the next loop will be the last 1D loop, mark the time after this loop as "After kernel(1D) calls" to include the time spent in the last 1D loop in that timing.
                if (divisorOffset + 3 * batchSize >= divisorCount1D)
                    grl.AppendTimingMark("After kernel(1D) calls");

                divisorOffset += batchSize;
            } while (divisorOffset < divisorCount1D);
            grl.AppendTimingMark("After kernel(1D) calls");

            var kernel2D = _accelerator.LoadAutoGroupedKernel<
                Index2D,
                ArrayView<uint>,
                ArrayView<int>,
                ArrayView<int>,
                ArrayView<int>,
                ArrayView<int>,
                ArrayView<int>,
                ArrayView<int>,
                ArrayView<int>,
                int,
                int,
                int,
                int>(SieveKernel2D);
            do
            {
                var batchSize2D = Math.Min(xDim, divisorCount - divisorOffset);

                while (batchSize2D * yDim * 2 <= _computeUnits)
                {
                    yDim *= 2;
                }

                kernel2D(
                    _accelerator.DefaultStream,
                    new Index2D(batchSize2D, yDim),
                    sieveBuffer.View,
                    startBytesBuffer.View,
                    startBitsBuffer.View,
                    shortStepBytesBuffer.View,
                    shortStepBitsBuffer.View,
                    longStepBytesBuffer.View,
                    longStepBitsBuffer.View,
                    startsWithLongStepBuffer.View,
                    divisorOffset,
                    divisorCount,
                    (int)sieveLength,
                    yDim);
                _accelerator.Synchronize();

                grl.AppendTimingMark("After kernel(2D) call");
                divisorOffset += batchSize2D;

            } while (divisorOffset < divisorCount);

            sieveBuffer.CopyToCPU(sieveWords);
            grl.AppendTimingMark("After sieveBuffer.CopyToCPU");
        }
        finally
        {
            semaphore.Release();
        }
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

        UpdateSieveBytes(sieveBytes, sieveLength, offsetByte, offsetBit, useLongStep, longStepByte, longStepBit, shortStepByte, shortStepBit);
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
        int divisorIndexOffset,
        int divisorCount,
        int sieveLength,
        int yDim)
    {
        var divisorIndex = index.X + divisorIndexOffset;
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

        UpdateSieveBytes(sieveBytes, newSieveLength, offsetByte, offsetBit, useLongStep, longStepByte, longStepBit, shortStepByte, shortStepBit);
    }

    private static void UpdateSieveBytes(ArrayView<uint> sieveBytes, int sieveLength, int offsetByte, int offsetBit,
        bool useLongStep, int longStepByte, int longStepBit, int shortStepByte, int shortStepBit)
    {
        while (offsetByte < sieveLength)
        {
            var bitMask = 1u << offsetBit;
            Atomic.Or(ref sieveBytes[offsetByte], bitMask);

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
