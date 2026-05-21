namespace ArrayPrimes2022.Sieve;

internal sealed class CpuSieveBackend : ISieveBackend
{
    private readonly ulong _segmentByteLength;

    public CpuSieveBackend(ulong segmentByteLength)
    {
        _segmentByteLength = segmentByteLength;
    }

    public string Name => "CPU";

    public void Execute(
        ulong loopMinCheckedValue,
        uint[] fullDivisorList,
        uint[] offsets,
        ulong divisorsFillPosition,
        ulong divisorPosition,
        byte[] sieveBytes)
    {
        for (; divisorPosition < divisorsFillPosition; divisorPosition++)
        {
            var p = fullDivisorList[divisorPosition];
            var o = offsets[divisorPosition];
            var primeByte = p / 8;
            var primeBit = (int)p % 8;
            var offsetByte = o / 8;
            var offsetBit = (int)o % 8;

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

            while (offsetByte < _segmentByteLength)
            {
                if (sieveBytes[offsetByte] != 255)
                {
                    var bitMask = (byte)(1 << offsetBit);
                    if ((sieveBytes[offsetByte] & bitMask) == 0)
                        sieveBytes[offsetByte] |= bitMask;
                }

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
            }
        }
    }
}
