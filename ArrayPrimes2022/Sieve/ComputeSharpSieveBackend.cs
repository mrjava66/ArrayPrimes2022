using ComputeSharp;
using Microsoft.Win32;

namespace ArrayPrimes2022.Sieve;

internal sealed class ComputeSharpSieveBackend : ISieveBackend
{
    private const string GraphicsDriversRegistryPath = @"SYSTEM\CurrentControlSet\Control\GraphicsDrivers\TdrDelay";

    private static readonly GraphicsDevice Device = GraphicsDevice.GetDefault();

    public string Name => "ComputeSharp";

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
        //TrySetGraphicsDriversRegistryValue();
        Device.DeviceLost += Device_DeviceLost;
        Console.WriteLine($"Chosen device: {Device.Name}:{Device.Luid}");
    }

    private static void TrySetGraphicsDriversRegistryValue()
    {
        try
        {
            using var isKey = Registry.LocalMachine.OpenSubKey(GraphicsDriversRegistryPath);
            if (isKey != null && isKey.GetValue(string.Empty) is int currentValue && currentValue >= 8)
                return;

            using var key = Registry.LocalMachine.CreateSubKey(GraphicsDriversRegistryPath, writable: true);
            key?.SetValue(string.Empty, 8, RegistryValueKind.DWord);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unable to update HKLM\\{GraphicsDriversRegistryPath}: {ex.Message}");
        }
    }

    private void Device_DeviceLost(object? sender, DeviceLostEventArgs e)
    {
        Console.WriteLine("Device lost: " + e.Reason.ToString());
    }

    /// <summary>
    /// Since the sieve is not sent to the GPU as bytes but as 32-bit uints, we need to convert the byte offsets
    /// and steps into uint offsets and steps. This method prepares the data for the GPU and then launches the
    /// compute shader to perform the sieving in parallel on the GPU.
    /// </summary>
    /// <param name="loopMinCheckedValue"></param>
    /// <param name="fullDivisorList"></param>
    /// <param name="offsets"></param>
    /// <param name="divisorsFillPosition"></param>
    /// <param name="divisorPosition"></param>
    /// <param name="sieveBytes"></param>
    public void Execute(ulong loopMinCheckedValue, uint[] fullDivisorList, uint[] offsets, ulong divisorsFillPosition,
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

            startBytes[i] = offsetByte;
            startBits[i] = offsetBit;
            shortStepBytes[i] = primeByte;
            shortStepBits[i] = primeBit;
            longStepBytes[i] = primeByteOn;
            longStepBits[i] = primeBitOn;
            startsWithLongStep[i] = onOff ? 1 : 0;
        }

        var sieveWords = new uint[sieveBytes.Length / sizeof(uint)];
        Buffer.BlockCopy(sieveBytes, 0, sieveWords, 0, sieveBytes.Length);

        using var sieveBuffer = Device.AllocateReadWriteBuffer(sieveWords);
        var sieveLength = sieveBuffer.Length;
        using var startByteBuffer = Device.AllocateReadOnlyBuffer(startBytes);
        using var startBitBuffer = Device.AllocateReadOnlyBuffer(startBits);
        using var shortStepByteBuffer = Device.AllocateReadOnlyBuffer(shortStepBytes);
        using var shortStepBitBuffer = Device.AllocateReadOnlyBuffer(shortStepBits);
        using var longStepByteBuffer = Device.AllocateReadOnlyBuffer(longStepBytes);
        using var longStepBitBuffer = Device.AllocateReadOnlyBuffer(longStepBits);
        using var startsWithLongStepBuffer = Device.AllocateReadOnlyBuffer(startsWithLongStep);

        Device.For(divisorCount, new ComputeSharpSieveShader(
            sieveBuffer,
            startByteBuffer,
            startBitBuffer,
            shortStepByteBuffer,
            shortStepBitBuffer,
            longStepByteBuffer,
            longStepBitBuffer,
            startsWithLongStepBuffer,
            divisorCount,
            sieveLength));

        sieveBuffer.CopyTo(sieveWords);

        Buffer.BlockCopy(sieveWords, 0, sieveBytes, 0, sieveBytes.Length);
        
        /*
        //for (var i = 0; i < sieveBytes.Length; i++) sieveBytes[i] = (byte)sieveWords[i];
        for (var i = 0; i < sieveBuffer.Length; i++)
        {
            var sieveWord = sieveBuffer[i];
            var sieveByteIndex = i * sizeof(uint);
            if (sieveByteIndex < sieveBytes.Length)
            sieveBytes[sieveByteIndex] = (byte)(sieveWord & 0xFF);
            if (sieveByteIndex + 1 < sieveBytes.Length)
            sieveBytes[sieveByteIndex + 1] = (byte)((sieveWord >> 8) & 0xFF);
            if (sieveByteIndex + 2 < sieveBytes.Length)
            sieveBytes[sieveByteIndex + 2] = (byte)((sieveWord >> 16) & 0xFF);
            if (sieveByteIndex + 3 < sieveBytes.Length)
            sieveBytes[sieveByteIndex + 3] = (byte)((sieveWord >> 24) & 0xFF);
        }
        */
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
        this.divisorCount = divisorCount;
        this.sieveLength = sieveLength;
    }

    public void Execute()
    {
        var divisorIndex = ThreadIds.X;
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
