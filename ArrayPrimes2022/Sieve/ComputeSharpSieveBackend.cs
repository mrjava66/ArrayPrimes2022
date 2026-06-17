using ComputeSharp;
using Microsoft.Win32;

namespace ArrayPrimes2022.Sieve;

internal sealed class ComputeSharpSieveBackend : ISieveBackend, IDisposable
{
    private static readonly SemaphoreSlim SieveBufferSemaphore = new(1, 1);

    private static readonly GraphicsDevice? Device = TryGetDevice();
    private static readonly TimeSpan TimeWaitGoal = new(0, 0, 0, 2);
    private static TimeSpan _preworkTime;
    private const int MaxThreadGroupSize = 256;

    public string Name => "ComputeSharp";
    public bool IsAvailable => (Device?.ComputeUnits ?? 0) > 0;

    private static int _computeUnits;

    public ComputeSharpSieveBackend()
    {
        foreach (var device in GraphicsDevice.EnumerateDevices())
        {
            Console.WriteLine($"Found device: {device.Name} " +
                              $"\n\twith {device.ComputeUnits} compute units " +
                              $"\n\twith {device.DedicatedMemorySize} bytes of dedicated memory" +
                              $"\n\twith {device.IsHardwareAccelerated} hardware acceleration" +
                              $"\n\twith {device.Luid} Luid" +
                              $"\n\twith {device.SharedMemorySize:N0} bytes of shared memory" +
                              $"\n\twith {device.WavefrontSize} max threads per group");
        }

        if (Device != null)
        {
            _computeUnits = Device.ComputeUnits > int.MaxValue ? int.MaxValue : (int)Device.ComputeUnits;
            Device.DeviceLost += Device_DeviceLost;
        }
        Console.WriteLine($"Chosen device: {Device?.Name ?? "none"}:{Device?.Luid}");
    }

    private static void Device_DeviceLost(object? sender, DeviceLostEventArgs e)
    {
        Console.WriteLine("Device lost: " + e.Reason);
    }

    private static GraphicsDevice? TryGetDevice()
    {
        try { return GraphicsDevice.GetDefault(); }
        catch (Exception ex)
        {
            Console.WriteLine($"No GPU device found: {ex.Message}");
            return null;
        }
    }

    public TimeSpan GetPreworkTime() => _preworkTime;

    private static void SetPreworkTime(TimeSpan timeWaited)
    {
        var miss = TimeWaitGoal - timeWaited;
        _preworkTime += TimeSpan.FromMilliseconds(miss.TotalMilliseconds / 10);
        if (_preworkTime.TotalSeconds < 0)
            _preworkTime = TimeSpan.Zero;
    }

    public void Dispose()
    {
        if (Device != null)
            Device.DeviceLost -= Device_DeviceLost;
    }

    /// <summary>
    /// Since the sieve is not sent to the GPU as bytes but as 32-bit uints, we need to convert the byte offsets
    /// and steps into uint offsets and steps. This method prepares the data for the GPU and then launches the
    /// compute shader to perform the sieving in parallel on the GPU.
    /// </summary>
    /// <param name="grl"></param>
    /// <param name="loopMinCheckedValue"></param>
    /// <param name="fullDivisorList"></param>
    /// <param name="offsets"></param>
    /// <param name="divisorsFillPosition"></param>
    /// <param name="divisorPosition"></param>
    /// <param name="sieveBytes"></param>
    public void Execute(GapReport grl,
    ulong loopMinCheckedValue,
    uint[] fullDivisorList,
    uint[] offsets,
    ulong divisorsFillPosition,
    ulong divisorPosition,
    byte[] sieveBytes
    )
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

        grl.AppendTimingMark("BeforeSemaphoreWait");
        var semaphore = SieveBufferSemaphore;
        var timeBeforeWait = DateTime.UtcNow;
        semaphore.Wait();
        try
        {
            SetPreworkTime(DateTime.UtcNow - timeBeforeWait);
            // we want to include the time spent waiting for the semaphore in our timing, as it is part of the overall execution time of this method.
            grl.AppendTimingMark("AfterSemaphoreWait");
            AllocateAndDispatchSieveBuffers(
                grl,
            sieveWords,
            startBytes, startBits,
            shortStepBytes, shortStepBits,
            longStepBytes, longStepBits,
            startsWithLongStep, divisorCount);
        }
        finally
        {
            semaphore.Release();
        }

        Buffer.BlockCopy(sieveWords, 0, sieveBytes, 0, sieveBytes.Length);
    }

    private static void AllocateAndDispatchSieveBuffers(
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
        var device = Device ?? throw new InvalidOperationException("No GPU device is available.");
        using var sieveBuffer = device.AllocateReadWriteBuffer(sieveWords);
        var sieveLength = sieveBuffer.Length;
        using var startBytesBuffer = device.AllocateReadOnlyBuffer(startBytes);
        using var startBitsBuffer = device.AllocateReadOnlyBuffer(startBits);
        using var shortStepByteBuffer = device.AllocateReadOnlyBuffer(shortStepBytes);
        using var shortStepBitBuffer = device.AllocateReadOnlyBuffer(shortStepBits);
        using var longStepByteBuffer = device.AllocateReadOnlyBuffer(longStepBytes);
        using var longStepBitBuffer = device.AllocateReadOnlyBuffer(longStepBits);
        using var startsWithLongStepBuffer = device.AllocateReadOnlyBuffer(startsWithLongStep);

        // Dispatch the compute kernel in batches of divisors.
        // Use the device's wavefront size as yDim so AMD (64) and NVIDIA (32) are handled correctly.
        var yDim = (int)device.WavefrontSize;
        var xDim = _computeUnits / yDim;
        // Reserve num2DLoops*xDim divisors for the striped 2D loop, plus fixed counts for transitional passes.
        const int num2DLoops = 4;
        var divisorCount1D = divisorCount - num2DLoops * xDim - _computeUnits / 2 - _computeUnits / 4;
        var divisorOffset = 0;
        var loop = 0;
        var reportAt = divisorCount1D - 5 * _computeUnits;
        var maxBatchSize = _computeUnits * MaxThreadGroupSize;
        do
        {
            loop++;

            var batchSize = Math.Min(maxBatchSize, (divisorCount1D - divisorOffset) / 2); // do half, but not more than the max for a single 1D batch
            if (batchSize < _computeUnits)
                batchSize = _computeUnits;
            batchSize = Math.Min(batchSize, divisorCount1D - divisorOffset); // don't run over the end.

            device.For(batchSize, new ComputeSharpSieveShader(
                sieveBuffer,
                startBytesBuffer,
                startBitsBuffer,
                shortStepByteBuffer,
                shortStepBitBuffer,
                longStepByteBuffer,
                longStepBitBuffer,
                startsWithLongStepBuffer,
                divisorOffset,
                divisorCount,
                sieveLength));

            if (divisorOffset >= reportAt)
            {
                var lastPrime = LastPrimeSieved(shortStepBytes, shortStepBits, divisorOffset, batchSize);
                grl.AppendTimingMark($"After {loop} kernel(1D) call with {batchSize} divisors, last prime: {lastPrime}");
            }

            divisorOffset += batchSize;
        } while (divisorOffset < divisorCount1D);
        grl.AppendTimingMark("After Device.For (1D) calls");

        // Transitional passes: primes large enough to benefit from 2 or 4 Y-threads but not a full wavefront.
        device.For(_computeUnits / 2, 2, new ComputeSharpSieveShader2D(
            sieveBuffer,
            startBytesBuffer,
            startBitsBuffer,
            shortStepByteBuffer,
            shortStepBitBuffer,
            longStepByteBuffer,
            longStepBitBuffer,
            startsWithLongStepBuffer,
            divisorOffset,
            divisorCount,
            sieveLength,
            2, 0, 1));
        grl.AppendTimingMark($"After Device.For 1, 2, {_computeUnits / 2}(2D) call");
        divisorOffset += _computeUnits / 2;

        device.For(_computeUnits / 4, 4, new ComputeSharpSieveShader2D(
            sieveBuffer,
            startBytesBuffer,
            startBitsBuffer,
            shortStepByteBuffer,
            shortStepBitBuffer,
            longStepByteBuffer,
            longStepBitBuffer,
            startsWithLongStepBuffer,
            divisorOffset,
            divisorCount,
            sieveLength,
            4, 0, 1));
        grl.AppendTimingMark($"After Device.For 1, 4, {_computeUnits / 4}(2D) call");
        divisorOffset += _computeUnits / 4;

        // Striped 2D passes: distribute the num2DLoops*xDim largest divisors evenly across threads.
        // Thread X in stripe S handles divisor at index: S + num2DLoops*X + divisorOffset.
        // This interleaves fast and slow primes across all threads rather than clustering them.
        var stripe = 0;
        do
        {
            loop++;
            device.For(xDim, yDim, new ComputeSharpSieveShader2D(
                sieveBuffer,
                startBytesBuffer,
                startBitsBuffer,
                shortStepByteBuffer,
                shortStepBitBuffer,
                longStepByteBuffer,
                longStepBitBuffer,
                startsWithLongStepBuffer,
                divisorOffset,
                divisorCount,
                sieveLength,
                yDim, stripe, num2DLoops));
            var lastPrime = LastPrimeSieved(shortStepBytes, shortStepBits, divisorOffset, xDim);
            grl.AppendTimingMark($"After Device.For {loop}, {yDim}, {xDim}(2D) stripe {stripe}, last prime: {lastPrime}");
            stripe++;
        }
        while (stripe < num2DLoops);
        divisorOffset += num2DLoops * xDim;
        grl.AppendTimingMark("After Device.For (2D) calls");

        sieveBuffer.CopyTo(sieveWords);
    }

    private static int LastPrimeSieved(int[] shortStepBytes, int[] shortStepBits, int divisorOffset, int batchSize)
    {
        var lastPos = divisorOffset + batchSize - 1;
        var by = shortStepBytes[lastPos];
        var bi = shortStepBits[lastPos];
        var lastPrime = by * 32 + bi;
        return lastPrime;
    }
}

[ThreadGroupSize(DefaultThreadGroupSizes.X)]
[GeneratedComputeShaderDescriptor]
internal readonly partial struct ComputeSharpSieveShader : IComputeShader
{
    private readonly ReadWriteBuffer<uint> sieveBytes;
    private readonly ReadOnlyBuffer<int> startBytes;
    private readonly ReadOnlyBuffer<int> startBits;
    private readonly ReadOnlyBuffer<int> shortStepBytes;
    private readonly ReadOnlyBuffer<int> shortStepBits;
    private readonly ReadOnlyBuffer<int> longStepBytes;
    private readonly ReadOnlyBuffer<int> longStepBits;
    private readonly ReadOnlyBuffer<int> startsWithLongStep;
    private readonly int divisorIndexOffset;
    private readonly int divisorCount;
    private readonly int sieveLength;

#pragma warning disable IDE0290 // Use primary constructor
    public ComputeSharpSieveShader(
#pragma warning restore IDE0290 // Use primary constructor
        ReadWriteBuffer<uint> sieveBytes,
        ReadOnlyBuffer<int> startBytes,
        ReadOnlyBuffer<int> startBits,
        ReadOnlyBuffer<int> shortStepBytes,
        ReadOnlyBuffer<int> shortStepBits,
        ReadOnlyBuffer<int> longStepBytes,
        ReadOnlyBuffer<int> longStepBits,
        ReadOnlyBuffer<int> startsWithLongStep,
        int divisorIndexOffset,
        int divisorCount,
        int sieveLength)
    {
        this.sieveBytes = sieveBytes;
        this.startBytes = startBytes;
        this.startBits = startBits;
        this.shortStepBytes = shortStepBytes;
        this.shortStepBits = shortStepBits;
        this.longStepBytes = longStepBytes;
        this.longStepBits = longStepBits;
        this.startsWithLongStep = startsWithLongStep;
        this.divisorIndexOffset = divisorIndexOffset;
        this.divisorCount = divisorCount;
        this.sieveLength = sieveLength;
    }

    public void Execute()
    {
        var divisorIndex = ThreadIds.X + divisorIndexOffset;
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
            //if ((sieveBytes[offsetByte] & bitMask) == 0) // this does not help.
            Hlsl.InterlockedOr(ref sieveBytes[offsetByte], bitMask);

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
}

[ThreadGroupSize(DefaultThreadGroupSizes.X)]
[GeneratedComputeShaderDescriptor]
internal readonly partial struct ComputeSharpSieveShader2D : IComputeShader
{
    private readonly ReadWriteBuffer<uint> sieveBytes;
    private readonly ReadOnlyBuffer<int> startBytes;
    private readonly ReadOnlyBuffer<int> startBits;
    private readonly ReadOnlyBuffer<int> shortStepBytes;
    private readonly ReadOnlyBuffer<int> shortStepBits;
    private readonly ReadOnlyBuffer<int> longStepBytes;
    private readonly ReadOnlyBuffer<int> longStepBits;
    private readonly ReadOnlyBuffer<int> startsWithLongStep;
    private readonly int divisorIndexOffset;
    private readonly int divisorCount;
    private readonly int sieveLength;
    private readonly int yDim;
    private readonly int stripe;
    private readonly int num2DLoops;

#pragma warning disable IDE0290 // Use primary constructor
    public ComputeSharpSieveShader2D(
#pragma warning restore IDE0290 // Use primary constructor
        ReadWriteBuffer<uint> sieveBytes,
        ReadOnlyBuffer<int> startBytes,
        ReadOnlyBuffer<int> startBits,
        ReadOnlyBuffer<int> shortStepBytes,
        ReadOnlyBuffer<int> shortStepBits,
        ReadOnlyBuffer<int> longStepBytes,
        ReadOnlyBuffer<int> longStepBits,
        ReadOnlyBuffer<int> startsWithLongStep,
        int divisorIndexOffset,
        int divisorCount,
        int sieveLength,
        int yDim,
        int stripe,
        int num2DLoops)
    {
        this.sieveBytes = sieveBytes;
        this.startBytes = startBytes;
        this.startBits = startBits;
        this.shortStepBytes = shortStepBytes;
        this.shortStepBits = shortStepBits;
        this.longStepBytes = longStepBytes;
        this.longStepBits = longStepBits;
        this.startsWithLongStep = startsWithLongStep;
        this.divisorIndexOffset = divisorIndexOffset;
        this.divisorCount = divisorCount;
        this.sieveLength = sieveLength;
        this.yDim = yDim;
        this.stripe = stripe;
        this.num2DLoops = num2DLoops;
    }

    public void Execute()
    {
        var divisorIndex = stripe + num2DLoops * ThreadIds.X + divisorIndexOffset;
        if (divisorIndex >= divisorCount)
            return;

        var offsetByte = startBytes[divisorIndex];
        var offsetBit = startBits[divisorIndex];
        var shortStepByte = shortStepBytes[divisorIndex];
        var shortStepBit = shortStepBits[divisorIndex];
        var longStepByte = longStepBytes[divisorIndex];
        var longStepBit = longStepBits[divisorIndex];
        var useLongStep = startsWithLongStep[divisorIndex] != 0;

        // Revise the offsets and sieve length for this thread based on its Y index,
        // so that each Y thread processes a different portion of the sieve.
        var totalNumDoubleSteps = sieveLength / ((shortStepByte + longStepByte) + (shortStepBit + longStepBit) / 32); // Approximate total number of double steps for this divisor
        var offsetStart = ThreadIds.Y * totalNumDoubleSteps / yDim; // Starting double-step index for this thread
        var offsetEnd = (1 + ThreadIds.Y) * totalNumDoubleSteps / yDim; // Ending double-step index for this thread
        offsetByte += offsetStart * (shortStepByte + longStepByte);
        offsetBit += offsetStart * (shortStepBit + longStepBit);
        while (offsetBit >= 32) { offsetBit -= 32; offsetByte++; }
        var newSieveLength = offsetEnd * (shortStepByte + longStepByte);
        newSieveLength += (offsetEnd * (shortStepBit + longStepBit)) / 32;
        newSieveLength++;
        // Don't run over the end.
        if (newSieveLength > sieveLength)
            newSieveLength = sieveLength;
        // Make sure the last thread completes the end of the sieve.
        if (ThreadIds.Y + 1 >= yDim)
            newSieveLength = sieveLength;

        while (offsetByte < newSieveLength)
        {
            var bitMask = 1u << offsetBit;
            Hlsl.InterlockedOr(ref sieveBytes[offsetByte], bitMask);

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
}
