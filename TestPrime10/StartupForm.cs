using System.Diagnostics.CodeAnalysis;
#pragma warning disable JMC001
#pragma warning disable JMA001

namespace TestPrime10;

public class StartupForm : Form
{
    private readonly Label _lblProgress;
    private readonly System.Windows.Forms.Timer _progressTimer;

    public StartupForm()
    {
        var lblMessage = new Label
        {
            Text = @"Starting up, please wait...",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.None,
            Font = new Font("Segoe UI", 14F),
            Bounds = new Rectangle(0, 0, 320, 50)
        };

        // Shows the current value of MakePrimesSieve.Progress as the sieve builds.
        _lblProgress = new Label
        {
            Text = MakePrimesSieve.Progress,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.None,
            Font = new Font("Segoe UI", 9F),
            Bounds = new Rectangle(0, 50, 320, 30)
        };

        Controls.Add(lblMessage);
        Controls.Add(_lblProgress);

        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        ControlBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(320, 80);
        Text = @"Starting...";

        // Poll MakePrimesSieve.Progress every 250 ms and update the label.
        _progressTimer = new System.Windows.Forms.Timer { Interval = 250 };
        _progressTimer.Tick += (_, _) => _lblProgress.Text = MakePrimesSieve.Progress;
        _progressTimer.Start();
    }

    /// <summary>Stops the progress timer when the form is closed.</summary>
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _progressTimer.Stop();
        _progressTimer.Dispose();
        base.OnFormClosed(e);
    }

    [AllowNull] public sealed override string Text
    {
        get => base.Text;
        set => base.Text = value;
    }
}
