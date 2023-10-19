using System.Text;

namespace TestPrime;

public partial class Form1 : Form
{
    private const string Composite = "Composite";
    private const string Composite2 = "Composite:first factors:";
    private const string Lfsp = "large-factor-semi-prime";
    private const string Sfsp = "small-factor-semi-prime";
    private const string Uisp = "unknown-if-semi-prime";
    private const string KnownPrime = "known prime";
    private const string Nff = "no factor found";
    private static int _numNotFoundNum;
    private static readonly object NumNotFoundObj = new();
    private static IMakePrimes? _makePrimes;

    private static readonly List<(string, string)> Locations = new()
    {
        ("756", "70099348325843"),
        ("758", "47581758352253"),
        ("778", "42842283925351"),
        ("788", "96949415903999"),
        ("804", "90874329411493"),
        ("806", "171231342420521"),
        ("1510", "6787988999657777797"),
        ("1526", "15570628755536096243"),
        ("1530", "17678654157568189057")
    };

    private int _dClickLocation = -1;

    public Form1()
    {
        InitializeComponent();
        MakePrimes.MakePrimesTask();
    }

    private static IMakePrimes MakePrimes
    {
        get
        {
            _makePrimes ??= new MakePrimesSieve();
            //_makePrimes ??= new MakePrimesEnum();
            return _makePrimes;
        }
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
            if (_makePrimes == null)
                throw new Exception("_makePrimes needs a value.");
            var n1 = textBox1.Text.FixNumber();
            var n2 = textBox2.Text.FixNumber();
            var didStart = ulong.TryParse(n1, out var start);
            if (!didStart && n1.Length > 7)
            {
                var n1a = n1.Substring(0, n1.Length - 7);
                var n1b = n1.Substring(n1a.Length, 7);
                var ds1 = ulong.TryParse(n1a, out var s1);
                var ds2 = ulong.TryParse(n1b, out var s2);
                if (ds1 && ds2) start = s1 * 1000000 + s2;
            }

            var didLen = ulong.TryParse(n2, out var len);
            if (!didStart || !didLen)
                throw new Exception("Must Provide Numbers");

            if (!chkEven.Checked)
            {
                //sensible numbers.
                if (start % 2 == 0) start++;
                if (len % 2 != 0) len++;
            }

            var end = start + len;
            if (end < start)
                end = ulong.MaxValue;
            _numNotFoundNum = 0;
            richTextBox1.Text = "";
            var lines = new StringBuilder();
            //var firstP = (ulong)Math.Floor(Math.Sqrt(start));
            var lastP = (ulong)Math.Floor(Math.Sqrt(end));
            var lastP3 =
                (ulong)Math.Floor(Math.Pow(end,
                    1.0 / 3.0)); // if first divisible prime is > num^1/3, then value is semi-prime.
            var somePrimes = _makePrimes.DictAllPrimes;
            var increment = (ulong)(chkEven.Checked ? 1 : 2);
            var dig = chkDig.Checked;

            var tasks = new List<Task<string>>();
            for (var check = start; check <= end; check += increment)
            {
                var aCheck = check;
                var task = Task.Run(() => CheckNumber(aCheck, dig, lastP, lastP3, somePrimes));
                tasks.Add(task);
                //task.Wait();//uncomment to single thread.
                if (check == ulong.MaxValue)
                    break;
            }

            Application.DoEvents();
            var screenHold = DateTime.Now;
            var delta = 3;
            foreach (var task in tasks)
            {
                //every few seconds do a ade.
                if ((DateTime.Now - screenHold).TotalSeconds > delta)
                {
                    screenHold = DateTime.Now;
                    Application.DoEvents();
                }

                task.Wait();
                //GC.Collect();
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
                if (line.Contains(KnownPrime))
                {
                    richTextBox.SelectionBackColor = Color.DarkGreen;
                    richTextBox.SelectionColor = Color.White;
                }
                else if (line.Contains(Lfsp))
                {
                    richTextBox.SelectionBackColor = Color.GreenYellow;
                }
                else if (line.Contains(Sfsp))
                {
                    richTextBox.SelectionBackColor = Color.DarkSeaGreen;
                }
                else if (line.Contains(Nff))
                {
                    richTextBox.SelectionBackColor = Color.Red;
                }
                else if (line.Contains(Uisp))
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

    private string CheckNumber(ulong checkVal, bool dig, ulong lastP0, ulong lastP3,
        Dictionary<ulong, ulong> somePrimes)
    {
        var lastP = lastP0;
        var line = new StringBuilder();
        line.Append($"{Environment.NewLine}");
        var check = checkVal;

        var factors = new List<ulong>();
        foreach (var p in somePrimes.Keys)
        {
            var exiting = false;
            while (check % p == 0)
            {
                factors.Add(p);
                if (!dig && factors.Count > 2)
                {
                    exiting = true;
                    break;
                }

                check /= p;
                if (check == 1)
                {
                    exiting = true;
                    break;
                }

                if (somePrimes.ContainsKey(check))
                {
                    factors.Add(check);
                    exiting = true;
                    break;
                }

                lastP = (ulong)Math.Floor(Math.Sqrt(check));
            }

            if (exiting) break;

            //must be prime.
            if (p > lastP)
            {
                factors.Add(check);
                break;
            }

            if (p == check)
            {
                factors.Add(check);
                break;
            }
        }

        foreach (var factor in factors) line.Append($"{factor}:");
        var factorsCount = factors.Count;
        var factors0 = factorsCount > 0 ? factors[0] : 0;
        factors.Clear();
        //GC.Collect();

        line.Append($"{checkVal}:");

        if (factorsCount == 0)
        {
            //oops.
            line.Append($"{Nff}.{checkVal}");
        }
        else if (factors0 == checkVal)
        {
            //prime
            line.Append($"{KnownPrime}");
        }
        else if (factors0 >= lastP3)
        {
            line.Append($"{Lfsp}");
        }
        else if (factorsCount == 2)
        {
            //semi-prime
            if (factors0 > lastP3)
                line.Append($"{Lfsp}");
            else
                line.Append($"{Sfsp}");
        }
        else if (!dig)
        {
            line.Append($"{Composite2}");
        }
        else
        {
            line.Append($"{Composite}");
        }

        return line.ToString();
    }

    private bool? IsPrime(ulong check, Dictionary<ulong, ulong> somePrimes)
    {
        if (somePrimes.ContainsKey(check))
            return true;

        var p2 = Math.Sqrt(check);
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

    private void textBox1_DoubleClick(object sender, EventArgs e)
    {
        _dClickLocation++;
        if (_dClickLocation > Locations.Count)
            _dClickLocation = 0;

        // keep old value.
        var didGet = Locations.Any(x => x.Item2 == textBox1.Text && x.Item1 == textBox2.Text);
        if (!didGet)
            Locations.Add((textBox2.Text, textBox1.Text));

        var val = Locations[_dClickLocation];
        textBox1.Text = val.Item2;
        textBox2.Text = val.Item1;
    }

    private void textBox1_TextChanged(object sender, EventArgs e)
    {
    }
}