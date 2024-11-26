namespace FileProcessor2022;

public static class FileExtension
{
    /// <summary>
    /// Prevent attempts to read empty files.  If a file is empty, throw an exception and make some trouble.
    /// </summary>
    /// <param name="file"></param>
    /// <param name="minLines"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static string[] GetReadAllLines(string file, int minLines = 10)
    {
        var retval = File.ReadAllLines(file);
        if (retval.Length >= minLines) 
            return retval;
        var msg = $"File {file} has {retval.Length} lines when {minLines} is required";
        Console.Error.WriteLine(msg);
        throw new Exception(msg);
    }
}