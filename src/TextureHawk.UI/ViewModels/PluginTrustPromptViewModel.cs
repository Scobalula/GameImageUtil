using CommunityToolkit.Mvvm.ComponentModel;

namespace TextureHawk.UI.ViewModels;

/// <summary>
/// ViewModel for plugin trust confirmation before package install.
/// </summary>
public sealed partial class PluginTrustPromptViewModel : ObservableObject
{
    /// <summary>
    /// 
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanInstall))]
    public partial bool IsTrusted { get; set; }

    public PluginTrustPromptViewModel(string packagePath)
    {
        PackagePath = packagePath;
    }

    /// <summary>
    /// Selected plugin package path.
    /// </summary>
    public string PackagePath { get; }

    /// <summary>
    /// Whether install action is enabled.
    /// </summary>
    public bool CanInstall => IsTrusted;
}
