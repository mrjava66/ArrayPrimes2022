using System.Text;

namespace TestPrime;

public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();
        MakePrimes.MakePrimesTask();
    }

    private void button1_Click(object sender, EventArgs e)
    {
        try
        {
            var didStart = ulong.TryParse(textBox1.Text, out var start);
            var didLen = ulong.TryParse(textBox2.Text, out var len);
            if (!didStart || !didLen) 
                throw new Exception("Must Provide Numbers");
            var end = start + len;
            var numNotFound = 0;
            textBox3.Text = "";
            var line = new StringBuilder();
            var lastP = Math.Sqrt(end);
            var lastP3 = Math.Pow(end, 1.0 / 3.0); // if first divisible prime is > num^1/3, then value is semi-prime.

            for (var check = start; check <= end; check += 2)
            {
                line.Append($"{Environment.NewLine}{check}:");
                var found = false;
                //var pNum = 0;
                var somePrimes = MakePrimes.ArrayAllPrimes;
                foreach (var p in somePrimes)
                {
                    if (p > lastP)
                        break;
                    var mod = check % p;
                    if (mod != 0) continue;
                    if (p > lastP3)
                        line.Append("semi-prime:");
                    line.Append($"{p}:{mod}::");
                    found = true;
                    break;
                }

                if (!found)
                {
                    numNotFound++;
                    line.Append("not found.");
                }
            }

            line.Append($"{Environment.NewLine}NumNotFound:{numNotFound}");
            textBox3.Text = line.ToString();
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message);
        }
    }

    private void button2_Click(object sender, EventArgs e)
    {
        try
        {
            var count = MakePrimes.NumPrimes;
            var prime = MakePrimes.ArrayAllPrimes[^1];
            // ReSharper disable once LocalizableElement
            label3.Text = $"Staged to {Environment.NewLine}{count}:{Environment.NewLine}{prime}";
        }
        catch (Exception exception)
        {
            MessageBox.Show(exception.Message);
        }
    }
}