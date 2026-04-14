using Godot;
using System.Threading.Tasks;
using BattleTank.Godot.CrashReport;

namespace BattleTank.Godot.UI;

public partial class CrashReportScreen : CanvasLayer
{
    private Label _statusLabel = null!;
    private TextEdit _commentEdit = null!;
    private Button _sendButton = null!;

    private CrashReporter _crashReporter = null!;
    private CrashReportMailer _mailer = null!;
    private string _reportPath = "";
    private string _reportContent = "";

    public override void _Ready()
    {
        Visible = false;
        Layer = 128;

        var bg = new ColorRect
        {
            Color = new Color(0, 0, 0, 0.85f)
        };
        bg.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(bg);

        var center = new CenterContainer();
        center.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        AddChild(center);

        var panel = new PanelContainer();
        panel.CustomMinimumSize = new Vector2(500, 0);
        center.AddChild(panel);

        var vbox = new VBoxContainer();
        vbox.AddThemeConstantOverride("separation", 12);
        panel.AddChild(vbox);

        var margin = new MarginContainer();
        margin.AddThemeConstantOverride("margin_left", 20);
        margin.AddThemeConstantOverride("margin_right", 20);
        margin.AddThemeConstantOverride("margin_top", 20);
        margin.AddThemeConstantOverride("margin_bottom", 20);
        vbox.AddChild(margin);

        var inner = new VBoxContainer();
        inner.AddThemeConstantOverride("separation", 12);
        margin.AddChild(inner);

        var title = new Label
        {
            Text = "CRASH DETECTED",
            HorizontalAlignment = HorizontalAlignment.Center
        };
        title.AddThemeColorOverride("font_color", new Color(1f, 0.3f, 0.3f));
        inner.AddChild(title);

        var desc = new Label
        {
            Text = "The game encountered an unexpected error.\nYou can send a crash report to help us fix it.",
            HorizontalAlignment = HorizontalAlignment.Center,
            AutowrapMode = TextServer.AutowrapMode.Word
        };
        inner.AddChild(desc);

        var commentLabel = new Label { Text = "Optional comment:" };
        inner.AddChild(commentLabel);

        _commentEdit = new TextEdit
        {
            CustomMinimumSize = new Vector2(0, 80),
            PlaceholderText = "Describe what you were doing when the crash occurred…"
        };
        inner.AddChild(_commentEdit);

        _statusLabel = new Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Text = ""
        };
        inner.AddChild(_statusLabel);

        var buttons = new HBoxContainer();
        buttons.AddThemeConstantOverride("separation", 8);
        inner.AddChild(buttons);

        _sendButton = new Button { Text = "Send Report" };
        _sendButton.Pressed += OnSendPressed;
        buttons.AddChild(_sendButton);

        var closeButton = new Button { Text = "Close" };
        closeButton.Pressed += () => Visible = false;
        buttons.AddChild(closeButton);
    }

    public void Initialize(CrashReporter crashReporter, CrashReportMailer mailer)
    {
        _crashReporter = crashReporter;
        _mailer = mailer;
    }

    public void ShowCrash(string reportPath)
    {
        _reportPath = reportPath;
        _reportContent = _crashReporter.ReadReport(reportPath);
        _statusLabel.Text = "";
        _commentEdit.Text = "";
        _sendButton.Disabled = false;
        Visible = true;
    }

    private async void OnSendPressed()
    {
        _sendButton.Disabled = true;
        _statusLabel.Text = "Sending…";

        bool success = await _mailer.SendAsync(_reportContent, _commentEdit.Text);

        if (success)
        {
            _crashReporter.MarkReportSent(_reportPath);
            _statusLabel.Text = "Report sent. Thank you!";
        }
        else
        {
            _statusLabel.Text = "Send failed — report saved locally for next launch.";
            _sendButton.Disabled = false;
        }
    }
}
