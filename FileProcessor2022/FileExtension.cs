#pragma warning disable JMA001
namespace FileProcessor2022;

/// <summary>
/// File-access helpers used throughout <see cref="ProgramClass"/>.
/// </summary>
public static class FileExtension
{
    /// <summary>
    /// Reads all lines from <paramref name="file"/>, retrying up to 100 times on failure
    /// (e.g. if the file is still being written by another process).
    /// </summary>
    /// <param name="file">Full path of the log file to read.</param>
    /// <param name="minLines">
    /// Minimum number of lines the file must contain; throws if the file is shorter.
    /// Defaults to 460 — the minimum expected for a normal block file.
    /// </param>
    /// <param name="lastPrimeRequired">
    /// When <see langword="true"/> (the default), the method verifies that a
    /// <c>LastPrime,</c> line exists somewhere near the end of the file.
    /// </param>
    /// <returns>All lines of the file as a string array.</returns>
    /// <exception cref="Exception">
    /// Thrown when the file is too short, all retries are exhausted, or no
    /// <c>LastPrime</c> line can be located.
    /// </exception>
    public static string[] GetReadAllLines(string file, int minLines = 460, bool lastPrimeRequired = true)
    {
        string msg;
        string[]? retval = null;
        var tries = 100;
        while (tries > 0)
        {
            tries--;
            try
            {
                retval = File.ReadAllLines(file);
                break;
            }
            catch (Exception)
            {
                // It doesn't matter why we failed to read the file.
                // We just need to wait one second and try again.
                Thread.Sleep(1000);
            }
        }

        // If we exhausted all our tries, then we will attempt one final read that will throw an exception if it fails.
        retval ??= File.ReadAllLines(file);

        if (retval.Length < minLines)
        {
            msg = $"File {file} has {retval.Length} lines when {minLines} is required";
            Console.Error.WriteLine(msg);
            throw new Exception(msg);
        }

        if (!lastPrimeRequired)
            return retval;

        // --- Locate the LastPrime line ---
        // Normal block files end with a LastPrime line as the very last entry.
        var lastPrimeLine = retval[^1];

        // Allow for a single trailing blank line.
        if (lastPrimeLine.Length == 0)
            lastPrimeLine = retval[^2];

        if (lastPrimeLine.StartsWith("LastPrime,"))
            return retval; // happy path — normal file that ends with a LastPrime line.

        if (lastPrimeLine.Length == 0)
        {
            msg = $"File {file} ends with two blank lines when content is expected.";
            Console.Error.WriteLine(msg);
            throw new Exception(msg);
        }

        // Summary files merge 1 024 blocks; the LastPrime line may be 1 024 rows from the end.
        if (retval.Length >= 1024)
        {
            lastPrimeLine = retval[^1024];
        }
        else
        {
            msg = $"File {file} cannot find LastPrimeLine 1";
            Console.Error.WriteLine(msg);
            throw new Exception(msg);
        }

        if (lastPrimeLine.StartsWith("LastPrime,"))
            return retval; // happy path — summary file that contains a LastPrime line in a sensible location.

        if (lastPrimeLine.Length == 0)
        {
            msg = $"File {file} cannot find LastPrimeLine 2";
            Console.Error.WriteLine(msg);
            throw new Exception(msg);
        }

        // Mega-summary files merge 2 048 blocks; try 2 048 rows from the end.
        if (retval.Length >= 2048)
        {
            lastPrimeLine = retval[^2048];
        }
        else
        {
            msg = $"File {file} cannot find LastPrimeLine 3";
            Console.Error.WriteLine(msg);
            throw new Exception(msg);
        }

        if (lastPrimeLine.StartsWith("LastPrime,"))
            return retval; // a LastPrime line in a not  sensible location for the 2-2**64 range.

        if (lastPrimeLine.Length == 0)
        {
            msg = $"File {file} cannot find LastPrimeLine 4";
            Console.Error.WriteLine(msg);
            throw new Exception(msg);
        }

        msg = $"File {file} cannot find LastPrimeLine 5";
        Console.Error.WriteLine(msg);
        throw new Exception(msg);
    }
}
