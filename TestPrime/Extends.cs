namespace TestPrime;

public static class Extends
{
    public static string FixNumber(this string num)
    {
        var retval = num;
        if (string.IsNullOrWhiteSpace(retval))
            return retval;
        retval = retval.Replace(",", "");
        var retval0 = "";
        foreach (var c in retval)
        {
            if ('0'<=c && c <= '9')
                retval0 += c;
        }
        return retval0;
    }
}