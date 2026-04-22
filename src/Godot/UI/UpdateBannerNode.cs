using Godot;
using BattleTank.Godot.Settings;

namespace BattleTank.Godot.UI;

public partial class UpdateBannerNode : CanvasLayer
{
    private string _version = "";
    private string _assetUrl = "";

    public void Show(string version, string htmlUrl, string assetUrl)
    {
        _version = version;
        _assetUrl = assetUrl;

        var panel = new PanelContainer();
        panel.SetAnchorsPreset(Control.LayoutPreset.TopWide);
        AddChild(panel);

        var hbox = new HBoxContainer();
        hbox.AddThemeConstantOverride("separation", 12);
        panel.AddChild(hbox);

        var label = new Label { Text = $"Mise à jour disponible : v{version}" };
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        hbox.AddChild(label);

        var updateBtn = new Button { Text = "Mettre à jour" };
        updateBtn.Pressed += OnUpdatePressed;
        hbox.AddChild(updateBtn);

        var laterBtn = new Button { Text = "Plus tard" };
        laterBtn.Pressed += () => QueueFree();
        hbox.AddChild(laterBtn);
    }

    private async void OnUpdatePressed()
    {
        await UpdateLauncher.StartUpdateAsync(_version, _assetUrl);
    }
}
