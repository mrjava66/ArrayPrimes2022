namespace FileProcessor2022;

public static class FileExtension
{
    /// <summary>
    /// Prevent attempts to read empty files.  If a file is empty, throw an exception and make some trouble.
    /// Prevent attempts to read files that don't contain a 'LastPrime' line.
    /// </summary>
    /// <param name="file"></param>
    /// <param name="minLines"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static string[] GetReadAllLines(string file, int minLines = 460)
    {
        string msg;
        var retval = File.ReadAllLines(file);
        if (retval.Length < minLines)
        {
            msg = $"File {file} has {retval.Length} lines when {minLines} is required";
            Console.Error.WriteLine(msg);
            throw new Exception(msg);
        }

        var lastPrimeLine = retval[^1];

        if (lastPrimeLine.Length == 0)
            lastPrimeLine = retval[^2];

        if (lastPrimeLine.StartsWith("LastPrime,"))
            return retval; // happy path, normal file that ends with a LastPrime line.

        if (lastPrimeLine.Length == 0)
        {
            msg = $"File {file} ends with two blank lines when content is expected.";
            Console.Error.WriteLine(msg);
            throw new Exception(msg);
        }

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
            return retval;  // happy path, summary file that contains a LastPrime line in a sensible location.

        if (lastPrimeLine.Length == 0)
        {
            msg = $"File {file} cannot find LastPrimeLine 2";
            Console.Error.WriteLine(msg);
            throw new Exception(msg);
        }

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
            return retval;  // a LastPrime line in a not  sensible location for the 2-2**64 range.

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
