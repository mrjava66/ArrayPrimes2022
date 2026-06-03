using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;

namespace ArrayPrimes2022.Sieve;

internal sealed class NVidiaSieveBackend : ISieveBackend, IDisposable
{
    public static float GpuMultiplier { get; set; } = 1.0f;

    private static int _maxSieveBuffers = 6;
    private static SemaphoreSlim _SieveBufferSemaphore = new(_maxSieveBuffers, _maxSieveBuffers);

    private readonly Context? _context;
    private readonly CudaAccelerator? _accelerator;
    public bool IsAvailable => _context != null && _accelerator != null;

    public string Name => "NVIDIA CUDA";

    public static int LoopSize => _loopSize;
    private static int _loopSize = 1024;

    public static int ComputeUnits => _computeUnits;
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

    public static void SetLoopSize(int taskLimit, float gpuMultiplier)
    {
        if (taskLimit == 0)
            taskLimit = 1;
        _loopSize = (int)(gpuMultiplier * _computeUnits / taskLimit);
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

    public static int MaxSimultaneousAllocateAndDispatchSieveBuffers
    {
        get => _maxSieveBuffers;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);

            _maxSieveBuffers = value;
            Interlocked.Exchange(ref _SieveBufferSemaphore, new SemaphoreSlim(value, value));

            SetLoopSize(value, GpuMultiplier);
        }
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
        var semaphore = _SieveBufferSemaphore;
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
            var divisorOffset = 0;
            do
            {
                var batchSize = Math.Min(_loopSize, divisorCount - divisorOffset);

                //if (batchSize < _loopSize && batchSize > 80) batchSize -= 80; // the true last batch should be 2D.
                kernel(
                    _accelerator.DefaultStream,
                    (Index1D)batchSize,
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

                //grl.AppendTimingMark($"kernel {divisorOffset}:{batchSize} of {divisorCount} completed.");
                _accelerator.Synchronize();
                //grl.AppendTimingMark($"After kernel {divisorOffset}:{batchSize} synchronize.");

                divisorOffset += batchSize;
            } while (divisorOffset < divisorCount);
            grl.AppendTimingMark("After kernel(_accelerator ... ) calls");

            _accelerator.Synchronize();
            grl.AppendTimingMark("After _accelerator.Synchronize");

            sieveBuffer.CopyToCPU(sieveWords);
            grl.AppendTimingMark("After sieveBuffer.CopyToCPU(sieveWords);");
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
