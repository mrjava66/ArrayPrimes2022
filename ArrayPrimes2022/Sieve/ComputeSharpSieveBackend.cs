using ComputeSharp;
using Microsoft.Win32;

namespace ArrayPrimes2022.Sieve;

internal sealed class ComputeSharpSieveBackend : ISieveBackend
{
    public static float GpuMultiplier { get; set; } = 1.0f;
    private const string GraphicsDriversRegistryPath = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers";
    private const string GraphicsDriverKeyName = "TdrDelay";
    //this needs to be more than the expected time of the longest compute shader dispatch to prevent Windows from killing the GPU driver during long computations. Setting it to 88 seconds should be more than enough for our use case, but it can be adjusted if needed.

    private static readonly GraphicsDevice Device = GraphicsDevice.GetDefault();
    private static int _maxSimultaneousAllocateAndDispatchSieveBuffers = 6;
    private static SemaphoreSlim _allocateAndDispatchSieveBuffersSemaphore = new(_maxSimultaneousAllocateAndDispatchSieveBuffers, _maxSimultaneousAllocateAndDispatchSieveBuffers);

    public string Name => "ComputeSharp";

    private static int _loopSize = 1024;
    private static uint _computeUnits;

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

        _computeUnits = Device.ComputeUnits;
        TrySetGraphicsDriversRegistryValue();
        Device.DeviceLost += Device_DeviceLost;
        Console.WriteLine($"Chosen device: {Device.Name}:{Device.Luid}");
    }

    public static void SetLoopSize(int taskLimit, float gpuMultiplier)
    {
        _loopSize = (int)(gpuMultiplier *_computeUnits / taskLimit);
    }

    private static void TrySetGraphicsDriversRegistryValue()
    {
        try
        {
            using var isKey = Registry.LocalMachine.OpenSubKey(GraphicsDriversRegistryPath);
            var keyObjectValue = isKey?.GetValue(GraphicsDriverKeyName);
            var currentValue = keyObjectValue as int?;

            if ((currentValue ?? 0) >= 88)
                return;

            Console.WriteLine($"Updating HKLM\\{GraphicsDriversRegistryPath}\\{GraphicsDriverKeyName} to 88 current value = {currentValue ?? -1})");
            using RegistryKey key = Registry.LocalMachine.CreateSubKey(GraphicsDriversRegistryPath, writable: true);
            key.SetValue(GraphicsDriverKeyName, 88, RegistryValueKind.DWord);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unable to update HKLM\\{GraphicsDriversRegistryPath}: {ex.Message}");
            Console.Error.WriteLine($"Unable to update HKLM\\{GraphicsDriversRegistryPath}: {ex.Message}");
        }
    }

    private static void Device_DeviceLost(object? sender, DeviceLostEventArgs e)
    {
        Console.WriteLine("Device lost: " + e.Reason);
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
    public void Execute(GapReport grl, ulong loopMinCheckedValue, uint[] fullDivisorList, uint[] offsets,
        ulong divisorsFillPosition,
        ulong divisorPosition, byte[] sieveBytes)
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

        var semaphore = _allocateAndDispatchSieveBuffersSemaphore;
        semaphore.Wait();
        try
        {
            grl.AppendTimingMark("AfterSemaphoreWait"); // we want to include the time spent waiting for the semaphore in our timing, as it is part of the overall execution time of this method.
            AllocateAndDispatchSieveBuffers(sieveWords, startBytes, startBits, shortStepBytes, shortStepBits, longStepBytes, longStepBits, startsWithLongStep, divisorCount);
        }
        finally
        {
            semaphore.Release();
        }

        Buffer.BlockCopy(sieveWords, 0, sieveBytes, 0, sieveBytes.Length);
    }

    public static int MaxSimultaneousAllocateAndDispatchSieveBuffers
    {
        get => _maxSimultaneousAllocateAndDispatchSieveBuffers;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);

            _maxSimultaneousAllocateAndDispatchSieveBuffers = value;
            Interlocked.Exchange(
                ref _allocateAndDispatchSieveBuffersSemaphore,
                new SemaphoreSlim(value, value));
            
            SetLoopSize(value, GpuMultiplier);
        }
    }

    private static void AllocateAndDispatchSieveBuffers(uint[] sieveWords, int[] startBytes, int[] startBits,
        int[] shortStepBytes, int[] shortStepBits, int[] longStepBytes, int[] longStepBits, int[] startsWithLongStep,
        int divisorCount)
    {
        using var sieveBuffer = Device.AllocateReadWriteBuffer(sieveWords);
        var sieveLength = sieveBuffer.Length;
        using var startBytesBuffer = Device.AllocateReadOnlyBuffer(startBytes);
        using var startBitsBuffer = Device.AllocateReadOnlyBuffer(startBits);
        using var shortStepByteBuffer = Device.AllocateReadOnlyBuffer(shortStepBytes);
        using var shortStepBitBuffer = Device.AllocateReadOnlyBuffer(shortStepBits);
        using var longStepByteBuffer = Device.AllocateReadOnlyBuffer(longStepBytes);
        using var longStepBitBuffer = Device.AllocateReadOnlyBuffer(longStepBits);
        using var startsWithLongStepBuffer = Device.AllocateReadOnlyBuffer(startsWithLongStep);

        // We dispatch the compute shader in batches of _loopSize divisors to avoid overwhelming the GPU with too many threads at once.
        // Each thread will handle one divisor and mark its multiples in the sieve.
        var divisorOffset = 0;
        do
        {
            Device.For(_loopSize, new ComputeSharpSieveShader(
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

            divisorOffset += _loopSize;
        } while (divisorOffset < divisorCount);

        sieveBuffer.CopyTo(sieveWords);
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

    public ComputeSharpSieveShader(
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
