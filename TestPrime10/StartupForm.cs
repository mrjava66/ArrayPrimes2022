using System.Diagnostics.CodeAnalysis;
#pragma warning disable JMC001
#pragma warning disable JMA001

namespace TestPrime10;

public class StartupForm : Form
{
    public StartupForm()
    {
        var lblMessage = new Label
        {
            Text = @"Starting up, please wait...",
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 14F)
        };

        Controls.Add(lblMessage);

        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterScreen;
        ControlBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(320, 100);
        Text = @"Starting...";
    }

    [AllowNull] public sealed override string Text
    {
        get => base.Text;
        set => base.Text = value;
    }
}
