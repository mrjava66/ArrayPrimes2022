using System.Diagnostics.CodeAnalysis;

namespace TestPrime10;

/// <summary>
/// A modal splash dialog displayed while the prime sieve is being built at startup.
/// It shows a static message and a live progress label that polls
/// <see cref="MakePrimesSieve.Progress"/> every 250 ms.
/// </summary>
public class StartupForm : Form
{
    private readonly System.Windows.Forms.Timer _progressTimer;

    /// <summary>
    /// Initialises the startup form: creates the static message label, the live
    /// progress label, and starts the polling timer.
    /// </summary>
    public StartupForm()
    {
        // Fixed header label shown for the entire duration of startup.
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
        var lblProgress = new Label
        {
            Text = MakePrimesSieve.Progress,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.None,
            Font = new Font("Segoe UI", 9F),
            Bounds = new Rectangle(0, 50, 320, 30)
        };

        Controls.Add(lblMessage);
        Controls.Add(lblProgress);

        // Non-resizable, centred, no title-bar controls — keeps focus on the message.
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        ControlBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(320, 80);
        Text = @"Starting...";

        // Poll MakePrimesSieve.Progress every 250 ms and update the label.
        _progressTimer = new System.Windows.Forms.Timer { Interval = 250 };
        _progressTimer.Tick += (_, _) => lblProgress.Text = MakePrimesSieve.Progress;
        _progressTimer.Start();
    }

    /// <summary>Stops the progress timer when the form is closed.</summary>
    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _progressTimer.Stop();
        _progressTimer.Dispose();
        base.OnFormClosed(e);
    }

    /// <summary>
    /// Overrides <see cref="Form.Text"/> to suppress the nullable warning on
    /// the title-bar string assignment.
    /// </summary>
    [AllowNull] public sealed override string Text
    {
        get => base.Text;
        set => base.Text = value;
    }
}
