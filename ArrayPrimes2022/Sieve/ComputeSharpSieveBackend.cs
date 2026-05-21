using ComputeSharp;

namespace ArrayPrimes2022.Sieve;

internal sealed class ComputeSharpSieveBackend : ISieveBackend
{
    private static readonly GraphicsDevice Device = GraphicsDevice.GetDefault();

    public string Name => "ComputeSharp";

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
            var primeByte = (int)(p / 8);
            var primeBit = (int)(p % 8);
            var offsetByte = (int)(o / 8);
            var offsetBit = (int)o % 8;

            var primeByteOn = primeByte + primeByte;
            var primeBitOn = primeBit + primeBit;
            if (primeBitOn >= 8)
            {
                primeBitOn -= 8;
                primeByteOn++;
            }

            var markLoc = loopMinCheckedValue + 16UL * (ulong)offsetByte + 2UL * (ulong)offsetBit + 1;
            if (markLoc % 3 == 0)
            {
                offsetByte += primeByte;
                offsetBit += primeBit;
                if (offsetBit >= 8)
                {
                    offsetBit -= 8;
                    offsetByte++;
                }

                markLoc = loopMinCheckedValue + 16UL * (ulong)offsetByte + 2UL * (ulong)offsetBit + 1;
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

        var sieveWords = Array.ConvertAll(sieveBytes, static b => (uint)b);

        using var sieveBuffer = Device.AllocateReadWriteBuffer(sieveWords);
        using var startByteBuffer = Device.AllocateReadOnlyBuffer(startBytes);
        using var startBitBuffer = Device.AllocateReadOnlyBuffer(startBits);
        using var shortStepByteBuffer = Device.AllocateReadOnlyBuffer(shortStepBytes);
        using var shortStepBitBuffer = Device.AllocateReadOnlyBuffer(shortStepBits);
        using var longStepByteBuffer = Device.AllocateReadOnlyBuffer(longStepBytes);
        using var longStepBitBuffer = Device.AllocateReadOnlyBuffer(longStepBits);
        using var startsWithLongStepBuffer = Device.AllocateReadOnlyBuffer(startsWithLongStep);

        Device.For(1, new ComputeSharpSieveShader(
            sieveBuffer,
            startByteBuffer,
            startBitBuffer,
            shortStepByteBuffer,
            shortStepBitBuffer,
            longStepByteBuffer,
            longStepBitBuffer,
            startsWithLongStepBuffer,
            divisorCount,
            sieveBytes.Length));

        sieveBuffer.CopyTo(sieveWords);

        for (var i = 0; i < sieveBytes.Length; i++)
            sieveBytes[i] = (byte)sieveWords[i];
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
        if (ThreadIds.X != 0)
            return;

        for (var divisorIndex = 0; divisorIndex < divisorCount; divisorIndex++)
        {
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
                sieveBytes[offsetByte] |= bitMask;

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

                if (offsetBit >= 8)
                {
                    offsetBit -= 8;
                    offsetByte++;
                }

                useLongStep = !useLongStep;
            }
        }
    }
}
