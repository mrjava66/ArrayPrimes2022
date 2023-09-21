using System.Text;

namespace TestPrime;

public partial class Form1 : Form
{
    private static readonly List<int> ListAllPrimes = new() { 2, 3, 5 };

    public Form1()
    {
        InitializeComponent();
    }

    private void button1_Click(object sender, EventArgs e)
    {
        try
        {
            var didStart = ulong.TryParse(textBox1.Text, out var start);
            var didLen = ulong.TryParse(textBox2.Text, out var len);
            var numNotFound = 0;
            textBox3.Text = "";
            var line = new StringBuilder();
            var lastP = Math.Sqrt(start + len);
            for (var check = start; check < start + len; check += 2)
            {
                line.Append($"{Environment.NewLine}{check}:");
                var found = false;
                var pNum = 0;
                foreach (var p in AllPrimes())
                {
                    pNum++;
                    if (pNum > 8123123) break;
                    if (p > lastP) 
                        break;

                    var mod = check % (ulong)p;
                    if (mod != 0) continue;
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

    public static IEnumerable<int> AllPrimes()
    {
        foreach (var prime in ListAllPrimes) yield return prime;
        var i = ListAllPrimes[ListAllPrimes.Count - 1];
        while (true)
        {
            i++;
            var j = Math.Sqrt(i);
            foreach (var k in AllPrimes())
            {
                if (i % k == 0)
                    //if i is evenly divisible by k, this is not a prime, check the next number.
                    break;
                if (k > j)
                {
                    //if none of primes less than the square root of i are evenly divisible, this is a prime.
                    ListAllPrimes.Add(i);
                    yield return i;
                    break;
                }
            }
        }
    }

    private void textBox2_TextChanged(object sender, EventArgs e)
    {

    }
}