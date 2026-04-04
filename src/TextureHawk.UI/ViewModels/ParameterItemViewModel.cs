using CommunityToolkit.Mvvm.ComponentModel;
using RedFox.Graphics2D;
using TextureHawk.Processing;

namespace TextureHawk.UI.ViewModels;

/// <summary>
/// ViewModel wrapping a single <see cref="ProcessorParameter"/> with editable values.
/// </summary>
/// <remarks>
/// Exposes type-specific visibility flags and value properties so the UI can
/// render the correct input control (CheckBox, NumericUpDown, TextBox, ComboBox).
/// </remarks>
public sealed partial class ParameterItemViewModel : ObservableObject
{
    private readonly ProcessorParameter _parameter;
    private readonly Action<string, object> _onValueChanged;

    /// <summary>
    /// Parameter identifier.
    /// </summary>
    public string Id => _parameter.Id;

    /// <summary>
    /// Human-readable parameter name.
    /// </summary>
    public string Name => _parameter.Name;

    /// <summary>
    /// Parameter description text.
    /// </summary>
    public string Description => _parameter.Description;

    /// <summary>
    /// Available options for Choice and ImageFormat parameters.
    /// </summary>
    public IReadOnlyList<string> Options { get; }

    // ── Type visibility flags ──

    /// <summary>Whether this is a Boolean parameter.</summary>
    public bool IsBoolean => _parameter.Type == ProcessorParameterType.Boolean;

    /// <summary>Whether this is an Integer parameter.</summary>
    public bool IsInteger => _parameter.Type == ProcessorParameterType.Integer;

    /// <summary>Whether this is a Float parameter.</summary>
    public bool IsFloat => _parameter.Type == ProcessorParameterType.Float;

    /// <summary>Whether this is a String parameter.</summary>
    public bool IsString => _parameter.Type == ProcessorParameterType.String;

    /// <summary>Whether this is a Choice parameter.</summary>
    public bool IsChoice => _parameter.Type == ProcessorParameterType.Choice;

    /// <summary>Whether this is an ImageFormat parameter.</summary>
    public bool IsImageFormat => _parameter.Type == ProcessorParameterType.ImageFormat;

    /// <summary>Whether this is a FilePath parameter.</summary>
    public bool IsFilePath => _parameter.Type == ProcessorParameterType.FilePath;

    /// <summary>Minimum numeric value for integer/float sliders.</summary>
    public decimal MinNumeric { get; }

    /// <summary>Maximum numeric value for integer/float sliders.</summary>
    public decimal MaxNumeric { get; }

    // ── Editable values ──

    [ObservableProperty]
    public partial bool BoolValue { get; set; }

    [ObservableProperty]
    public partial decimal NumericValue { get; set; }

    [ObservableProperty]
    public partial string StringValue { get; set; } = "";

    public ParameterItemViewModel(ProcessorParameter parameter, object? currentValue, Action<string, object> onValueChanged)
    {
        _parameter = parameter;
        _onValueChanged = onValueChanged;

        MinNumeric = parameter.MinValue is not null ? Convert.ToDecimal(parameter.MinValue) : decimal.MinValue;
        MaxNumeric = parameter.MaxValue is not null ? Convert.ToDecimal(parameter.MaxValue) : decimal.MaxValue;

        Options = parameter.Type == ProcessorParameterType.Choice
            ? parameter.Options ?? []
            : parameter.Type == ProcessorParameterType.ImageFormat
                ? parameter.Options ?? Enum.GetNames<ImageFormat>()
                : [];

        ApplyValue(currentValue ?? parameter.DefaultValue);
    }

    partial void OnBoolValueChanged(bool value) => _onValueChanged(_parameter.Id, value);

    partial void OnNumericValueChanged(decimal value)
    {
        if (_parameter.Type == ProcessorParameterType.Integer)
            _onValueChanged(_parameter.Id, (int)value);
        else
            _onValueChanged(_parameter.Id, (float)value);
    }

    partial void OnStringValueChanged(string value)
    {
        if (_parameter.Type == ProcessorParameterType.ImageFormat && Enum.TryParse<ImageFormat>(value, out var fmt))
            _onValueChanged(_parameter.Id, fmt);
        else
            _onValueChanged(_parameter.Id, value);
    }

    /// <summary>
    /// Sets the value externally (e.g. when loading per-item overrides).
    /// </summary>
    public void SetValue(object? value) => ApplyValue(value);

    private void ApplyValue(object? value)
    {
        switch (_parameter.Type)
        {
            case ProcessorParameterType.Boolean:
                BoolValue = value is true;
                break;
            case ProcessorParameterType.Integer:
                NumericValue = value is not null ? Convert.ToDecimal(value) : 0;
                break;
            case ProcessorParameterType.Float:
                NumericValue = value is not null ? Convert.ToDecimal(value) : 0;
                break;
            case ProcessorParameterType.Choice:
                StringValue = value?.ToString() ?? (Options.Count > 0 ? Options[0] : "");
                break;
            case ProcessorParameterType.ImageFormat:
                StringValue = value is ImageFormat f ? f.ToString() : ImageFormat.R8G8B8A8Unorm.ToString();
                break;
            default:
                StringValue = value?.ToString() ?? "";
                break;
        }
    }
}
