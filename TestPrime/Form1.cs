using System.Text;

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

public partial class Form1 : Form
{
    private static int _numNotFoundNum;
    private static readonly object NumNotFoundObj = new();

    public Form1()
    {
        InitializeComponent();
        MakePrimes.MakePrimesTask();
    }

    private static void NumNotFound()
    {
        lock (NumNotFoundObj)
        {
            _numNotFoundNum++;
        }
    }

    private void button1_Click(object sender, EventArgs e)
    {
        try
        {
            var didStart = ulong.TryParse(textBox1.Text.FixNumber(), out var start);
            var didLen = ulong.TryParse(textBox2.Text.FixNumber(), out var len);
            if (!didStart || !didLen)
                throw new Exception("Must Provide Numbers");
            var end = start + len;
            _numNotFoundNum = 0;
            textBox3.Text = "";
            var lines = new StringBuilder();
            var lastP = Math.Sqrt(end);
            var lastP3 = Math.Pow(end, 1.0 / 3.0); // if first divisible prime is > num^1/3, then value is semi-prime.
            var somePrimes = MakePrimes.DictAllPrimes;

            var tasks = new List<Task<string>>();
            for (var check = start; check <= end; check += 2)
            {
                var aCheck = check;
                var task = Task.Run(() => CheckNumber(aCheck, lastP, lastP3, somePrimes));
                tasks.Add(task);
            }

            foreach (var task in tasks)
            {
                task.Wait();
                lines.Append(task.Result);
            }

            lines.Append($"{Environment.NewLine}NumNotFound:{_numNotFoundNum}");
            textBox3.Text = lines.ToString();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message);
        }
    }

    private string CheckNumber(ulong check, double lastP, double lastP3, Dictionary<ulong, ulong> somePrimes)
    {
        var line = new StringBuilder();
        line.Append($"{Environment.NewLine}");
        var found = false;
        var knownPrime = false;
        //var pNum = 0;
        foreach (var p in somePrimes.Keys)
        {
            if (p > lastP)
            {
                knownPrime = true;
                break;
            }

            var mod = check % p;
            if (mod != 0) continue;

            line.Append($"{p}:{mod}:");

            if (p > lastP3)
            {
                line.Append("large-factor-semi-prime:");
            }
            else
            {
                var isPrime = IsPrime(check / p, somePrimes);
                if (isPrime == true)
                    line.Append("small-factor-semi-prime:");
                else if (isPrime == null)
                    line.Append("unknown-if-semi-prime:");
            }

            found = true;
            break;
        }

        if (knownPrime)
        {
            line.Append("known prime:");
        }
        else if (!found)
        {
            NumNotFound();
            line.Append("no factor found:");
        }

        line.Append($"{check}");

        return line.ToString();
    }

    private bool? IsPrime(ulong check, Dictionary<ulong,ulong> somePrimes)
    {
        var p2 = Math.Sqrt(check);

        if (somePrimes.ContainsKey(check))
            return true;
        foreach (var p in somePrimes.Keys)
        {
            if (p > p2)
                return true;
            if (check % p == 0)
                return false;
        }

        return null;
    }

    private void button2_Click(object sender, EventArgs e)
    {
        try
        {
            var count = MakePrimes.NumPrimes;
            var prime = MakePrimes.ArrayAllPrimes[^1];
            // ReSharper disable once LocalizableElement
            label3.Text = $"Staged to {Environment.NewLine}{count:n0}:{Environment.NewLine}{prime:n0}";
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message);
        }
    }
}