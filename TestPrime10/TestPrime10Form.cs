using System.Text;
#pragma warning disable IDE1006
#pragma warning disable IDE0028
#pragma warning disable IDE0057
#pragma warning disable CA1859

namespace TestPrime10;

public partial class TestPrime10Form : Form
{
    // Classification labels used in output lines and colour-coding.
    private const string Composite = "Composite";
    private const string Composite2 = "Composite:first factors:";
    private const string Lfsp = "large-factor-semi-prime";  // smallest factor > cbrt(n)
    private const string Sfsp = "small-factor-semi-prime";  // smallest factor <= cbrt(n)
    private const string Uisp = "unknown-if-semi-prime";
    private const string KnownPrime = "known prime";
    private const string Nff = "no factor found";
    //private static int _numNotFoundNum;
    //private static readonly object NumNotFoundObj = new();
    private static IMakePrimes? _makePrimes;

    // Preset (length, start) pairs cycled through by double-clicking txtStart.
    private static readonly List<(string, string)> Locations = new()
    {
        ("756", "70099348325843"),
        ("758", "47581758352253"),
        ("778", "42842283925351"),
        ("788", "96949415903999"),
        ("804", "90874329411493"),
        ("806", "171231342420521"),
        ("906", "218209405436543"),
        ("916", "1189459969825483"),
        ("924", "1686994940955803"),
        ("1132", "1693182318746371"),
        ("1184", "43841547845541059"),
        ("1510", "6787988999657777797"),
        ("1526", "15570628755536096243"),
        ("1530", "17678654157568189057"),
        ("1430", "4606937813294064947"),
        ("450", "8560443932347"),
        ("720", "113377199603617"),
        ("738", "1109480633819771"),
        ("840", "187891466722493")
    };

    // Index into Locations for the current double-click cycle position.
    private int _dClickLocation = -1;

    /// <summary>
    /// Initialises the form and kicks off the prime sieve on a background thread,
    /// showing a startup dialog while the sieve builds.
    /// </summary>
    public TestPrime10Form()
    {
        InitializeComponent();
        var startupForm = new StartupForm();
        Shown += async (_, _) =>
        {
            startupForm.Show(this);
            await Task.Run(() => MakePrimes.MakePrimesTask());
            startupForm.Close();
        };
    }

    /// <summary>
    /// Lazily creates and returns the prime sieve implementation.
    /// </summary>
    private static IMakePrimes MakePrimes
    {
        get
        {
            _makePrimes ??= new MakePrimesSieve();
            //_makePrimes ??= new MakePrimesEnum();
            return _makePrimes;
        }
    }

    /*
    private static void NumNotFound()
    {
        lock (NumNotFoundObj)
        {
            _numNotFoundNum++;
        }
    }
    */

    /// <summary>
    /// Classifies each number in [start, start+len) as prime, semi-prime, or composite
    /// using parallel tasks, then displays colour-coded results in <see cref="rtbOutput"/>.
    /// </summary>
    private void btnGo_Click(object sender, EventArgs e)
    {
        try
        {
            if (_makePrimes == null)
                throw new Exception("_makePrimes needs a value.");
            var n1 = txtStart.Text.FixNumber();
            var n2 = txtLen.Text.FixNumber();
            var didStart = ulong.TryParse(n1, out var start);
            var didLen = ulong.TryParse(n2, out var len);
            if (!didStart || !didLen)
                throw new Exception("Must Provide Numbers");

            if (!chkEven.Checked)
            {
                //sensible numbers.
                if (start % 2 == 0) start++;
                if (len % 2 != 0) len++;
            }

            if (start < 2)
                start = 2;

            var end = start + len;
            if (end < start)
                end = ulong.MaxValue;
            //_numNotFoundNum = 0;
            rtbOutput.Text = "";
            var lines = new StringBuilder();
            //var firstP = (ulong)Math.Floor(Math.Sqrt(start));
            var lastP = (ulong)Math.Floor(Math.Sqrt(end));
            // If the smallest factor of n is > cbrt(n), then n is a large-factor semi-prime.
            var lastP3 = (ulong)Math.Floor(Math.Pow(end + 0.9, 1.0 / 3.0));
            var lastP3X = (ulong)Math.Floor(Math.Pow(start + 0.9, 1.0 / 3.0));
            var somePrimes = _makePrimes.DictAllPrimes;
            var first = somePrimes.First(x => x.Key > lastP3X).Key;
            // cubeGap is true when the cbrt threshold varies across the checked range.
            var cubeGap = lastP3 > first;
            var increment = (ulong)(chkEven.Checked ? 1 : 2);
            var dig = chkDig.Checked;

            var tasks = new List<Task<string>>();
            for (var check = start; check <= end; check += increment)
            {
                var aCheck = check;
                var task = Task.Run(() => CheckNumber(aCheck, dig, lastP, lastP3, lastP3X, cubeGap, somePrimes));
                tasks.Add(task);
                //task.Wait(); //uncomment to single thread.
                if (check == ulong.MaxValue)
                    break;
            }

            Application.DoEvents();
            var screenHold = DateTime.Now;
            var delta = 3;
            foreach (var task in tasks)
            {
                //every few seconds do an application.doEvents.
                if ((DateTime.Now - screenHold).TotalSeconds > delta)
                {
                    screenHold = DateTime.Now;
                    Application.DoEvents();
                }

                task.Wait();
                //GC.Collect();
                lines.Append(task.Result);
            }

            //lines.Append($"{Environment.NewLine}NumNotFound:{_numNotFoundNum}");
            rtbOutput.Text = lines.ToString();
            ColorTextBox(rtbOutput);
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message);
        }
    }

    /// <summary>
    /// Applies background/foreground colour to each line in <paramref name="richTextBox"/>
    /// based on its classification label: blue = prime, green = large-factor semi-prime,
    /// yellow-green = small-factor semi-prime, red = no factor found, pink = unknown.
    /// Composite lines are shaded by their smallest factor value.
    /// </summary>
    private static void ColorTextBox(RichTextBox richTextBox)
    {
        foreach (var line in richTextBox.Lines)
            try
            {
                var startPos = richTextBox.Text.IndexOf(line, StringComparison.Ordinal);
                richTextBox.Select(startPos, line.Length);
                if (line.Contains(KnownPrime))
                {
                    richTextBox.SelectionBackColor = Color.Blue;
                    richTextBox.SelectionColor = Color.White;
                }
                else if (line.Contains(Lfsp))
                {
                    richTextBox.SelectionBackColor = Color.Green;
                }
                else if (line.Contains(Sfsp))
                {
                    richTextBox.SelectionBackColor = Color.GreenYellow;
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

    /// <summary>
    /// Trial-divides <paramref name="checkVal"/> against the known primes and returns a
    /// formatted result line classifying it as prime, large/small-factor semi-prime,
    /// composite, or "no factor found". When <paramref name="dig"/> is true all factors
    /// are collected; otherwise the search stops after three factors are found.
    /// </summary>
    private static string CheckNumber(ulong checkVal, bool dig,
        ulong lastP0, ulong lastP3, ulong lastP3X, bool cubeGap,
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

        // Recalculate the per-value cbrt threshold when it falls in the uncertain range.
        if (cubeGap && factors0 >= lastP3X && factors0 < lastP3)
            lastP3 = (ulong)Math.Floor(Math.Pow(checkVal + 0.9, 1.0 / 3.0));

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
        else if (factors0 > lastP3)
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

    /// <summary>
    /// Returns <see langword="true"/> if <paramref name="check"/> is in the pre-built prime
    /// dictionary or if no prime up to its square root divides it; <see langword="false"/> if
    /// a factor is found; <see langword="null"/> if the dictionary was exhausted before
    /// reaching the square root (result indeterminate).
    /// </summary>
    private static bool? IsPrime(ulong check, Dictionary<ulong, ulong> somePrimes)
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

    /// <summary>
    /// Convenience wrapper that returns the primality result together with the original
    /// value so tasks can be matched to their input without a separate lookup.
    /// </summary>
    private static (bool?, ulong) IsPrimeWithValue(ulong check, Dictionary<ulong, ulong> somePrimes)
    {
        return (IsPrime(check, somePrimes), check);
    }

    /// <summary>
    /// Updates <see cref="lblStage"/> with the current prime count and largest staged prime,
    /// showing how far the sieve has been built.
    /// </summary>
    private void btnStage_Click(object sender, EventArgs e)
    {
        try
        {
            var count = MakePrimes.NumPrimes;
            var prime = MakePrimes.ArrayAllPrimes[^1];
            // ReSharper disable once LocalizableElement
            lblStage.Text = $"Staged to {Environment.NewLine}{count:n0}:{Environment.NewLine}{prime:n0}";
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message);
        }
    }

    /// <summary>
    /// Cycles through the preset <see cref="Locations"/> list on each double-click,
    /// saving the current (length, start) pair back into the list if it is not already present.
    /// </summary>
    private void txtStart_DoubleClick(object sender, EventArgs e)
    {
        try
        {
            _dClickLocation++;
            if (_dClickLocation >= Locations.Count)
                _dClickLocation = 0;

            // keep old value.
            var didGet = Locations.Any(x => x.Item2 == txtStart.Text && x.Item1 == txtLen.Text);
            if (!didGet)
                Locations.Add((txtLen.Text, txtStart.Text));

            var val = Locations[_dClickLocation];
            txtStart.Text = val.Item2;
            txtLen.Text = val.Item1;
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message);
        }
    }

    /// <summary>
    /// Searches the staged primes for any twin-prime pair (p, p+2) whose midpoint
    /// (p+1)/2 is itself a prime, which would make it a twin semi-prime. Reports the
    /// total number of twin pairs checked and whether any such value was found.
    /// </summary>
    private void btnTwinSemi_Click(object sender, EventArgs e)
    {
        try
        {
            var checkedP = 0;
            if (_makePrimes == null)
                throw new Exception("_makePrimes needs a value.");

            var array = _makePrimes.ArrayAllPrimes;
            var dict = _makePrimes.DictAllPrimes;
            var lastPrime = array[0];
            foreach (var prime in array)
            {
                if (prime - lastPrime == 2)
                {
                    checkedP++;
                    var mid = (prime - 1) / 2;
                    if (mid < 4)
                        continue;
                    if (dict.ContainsKey(mid))
                    {
                        MessageBox.Show($@"Found Twin with semi {mid}");
                        break;
                    }
                }
                lastPrime = prime;
            }

            MessageBox.Show($@"Checked = {checkedP}, found=0");
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message);
        }
    }

    /// <summary>
    /// Tests primality for numbers near 10^<c>start</c>: the base value
    /// <c>1 + 10^(start-1)</c> and candidates obtained by varying each decimal digit
    /// position one at a time. Results (prime / not prime) are shown in
    /// <see cref="rtbOutput"/> with a total prime count.
    /// </summary>
    private void btnDig_Click(object sender, EventArgs e)
    {
        try
        {
            if (_makePrimes == null)
                throw new Exception("_makePrimes needs a value.");

            var n1 = txtStart.Text.FixNumber();
            var didStart = int.TryParse(n1, out var start);
            if (!didStart)
                throw new Exception($"Must Provide Numbers {txtStart.Text}");

            if (start < 2)
                throw new Exception("Start cannot be less than 2");

            if (start > 20)
                throw new Exception("Start cannot be over 20");

            var tasks = new List<Task<(bool?, ulong)>>();
            var somePrimes = _makePrimes.DictAllPrimes;
            ulong baseCheck0 = 1 + (ulong)Math.Pow(10, start - 1);
            var task0 = Task.Run(() => IsPrimeWithValue(baseCheck0, somePrimes));
            tasks.Add(task0);
            for (int i = 0; i < start; i++)
            {
                bool firstLast = i == 0 || i == start - 1;
                var level = (ulong)Math.Pow(10, i);
                var baseCheck = baseCheck0;
                if (firstLast)
                    baseCheck -= level;

                for (ulong j = 1; j < 10; j++)
                {
                    if (firstLast && j == 1)
                        continue;
                    var check = baseCheck + level * j;
                    var task = Task.Run(() => IsPrimeWithValue(check, somePrimes));
                    tasks.Add(task);
                    //task.Wait(); //uncomment to single thread.
                }
            }

            var lines = new StringBuilder();

            Application.DoEvents();
            var screenHold = DateTime.Now;
            var delta = 3;
            var count = 0;
            foreach (var task in tasks)
            {
                //every few seconds do an application.doEvents.
                if ((DateTime.Now - screenHold).TotalSeconds > delta)
                {
                    screenHold = DateTime.Now;
                    Application.DoEvents();
                }

                task.Wait();
                if (task.Result.Item1 ?? false)
                {
                    lines.AppendLine($"{task.Result.Item2} prime");
                    count++;
                }
            }

            lines.AppendLine($"{count} primes");

            rtbOutput.Text = lines.ToString();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message);
        }
    }

}