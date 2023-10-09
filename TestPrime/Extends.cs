namespace TestPrime;

public static class Extends
{
    public static string FixNumber(this string num)
    {
        var retval = num;
        if (string.IsNullOrWhiteSpace(retval))
            return retval;
        retval = retval.Replace(",", "");
        return retval;
    }
}