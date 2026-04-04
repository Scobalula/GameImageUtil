using CommunityToolkit.Mvvm.ComponentModel;

namespace TextureHawk.UI.ViewModels;

/// <summary>
/// Base class for all view models, provided by CommunityToolkit.Mvvm.
/// </summary>
/// <remarks>
/// This is a convenience alias. ViewModels inherit <see cref="ObservableObject"/> directly
/// or use this as a thin layer if shared logic is needed in the future.
/// </remarks>
public abstract class ViewModelBase : ObservableObject;

