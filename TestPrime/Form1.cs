using System.Text;

namespace TestPrime;

public partial class Form1 : Form
{
    private const string LFSP = "large-factor-semi-prime:";
    private const string SFSP = "small-factor-semi-prime:";
    private const string UISP = "unknown-if-semi-prime:";
    private const string KP = "known prime:";
    private const string NFF = "no factor found:";
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
            richTextBox1.Text = "";
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
            richTextBox1.Text = lines.ToString();
            ColorTextBox(richTextBox1);
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message);
        }
    }

    private static void ColorTextBox(RichTextBox richTextBox)
    {
        foreach (var line in richTextBox.Lines)
            try
            {
                var startPos = richTextBox.Text.IndexOf(line, StringComparison.Ordinal);
                richTextBox.Select(startPos, line.Length);
                if (line.Contains(KP))
                {
                    richTextBox.SelectionBackColor = Color.DarkGreen;
                    richTextBox.SelectionColor = Color.White;
                }
                else if (line.Contains(LFSP))
                {
                    richTextBox.SelectionBackColor = Color.GreenYellow;
                }
                else if (line.Contains(SFSP))
                {
                    richTextBox.SelectionBackColor = Color.DarkSeaGreen;
                }
                else if (line.Contains(NFF))
                {
                    richTextBox.SelectionBackColor = Color.Red;
                }
                else if (line.Contains(UISP))
                {
                    richTextBox.SelectionBackColor = Color.Pink;
                }
                else
                {
                    var sub = line.IndexOf(':');
                    if (sub <= 0)
                        continue;
                    var subLine = line.Substring(0, sub);
                    var didParse = int.TryParse(subLine, out var factor);
                    if (didParse)
                    {
                        //factor += 127;
                        if (factor < 127)
                            richTextBox.SelectionColor = Color.White;
                        if (factor > byte.MaxValue)
                            factor = byte.MaxValue;
                        if (factor < byte.MinValue)
                            factor = byte.MinValue;
                        richTextBox.SelectionBackColor = Color.FromArgb(factor, factor, factor);
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
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
                line.Append(LFSP);
            }
            else
            {
                var isPrime = IsPrime(check / p, somePrimes);
                if (isPrime == true)
                    line.Append(SFSP);
                else if (isPrime == null)
                    line.Append(UISP);
            }

            found = true;
            break;
        }

        if (knownPrime)
        {
            line.Append(KP);
        }
        else if (!found)
        {
            NumNotFound();
            line.Append(NFF);
        }

        line.Append($"{check}");

        return line.ToString();
    }

    private bool? IsPrime(ulong check, Dictionary<ulong, ulong> somePrimes)
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