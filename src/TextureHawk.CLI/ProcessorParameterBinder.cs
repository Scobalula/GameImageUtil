using System.Globalization;
using System.Text.Json;
using RedFox.Graphics2D;
using TextureHawk.Processing;

namespace TextureHawk.CLI;

internal static class ProcessorParameterBinder
{
    public static bool TryParseAssignment(string assignment, out string key, out string value)
    {
        var separatorIndex = assignment.IndexOf('=');
        if (separatorIndex < 1)
        {
            key = string.Empty;
            value = string.Empty;
            return false;
        }

        key = assignment[..separatorIndex].Trim();
        value = assignment[(separatorIndex + 1)..];
        return key.Length > 0;
    }

    public static bool TryBind(IProcessor processor, IReadOnlyDictionary<string, object?>? rawValues, out Dictionary<string, object> values, out string? error)
    {
        values = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        error = null;

        if (rawValues is null || rawValues.Count == 0)
            return true;

        var parameters = processor.Parameters.ToDictionary(parameter => parameter.Id, StringComparer.OrdinalIgnoreCase);
        foreach (var (key, rawValue) in rawValues)
        {
            if (!parameters.TryGetValue(key, out var parameter))
            {
                error = $"Processor '{processor.Id}' does not define parameter '{key}'.";
                return false;
            }

            if (!TryConvert(parameter, rawValue, out var value, out error))
                return false;

            if (value is not null)
                values[parameter.Id] = value;
        }

        return true;
    }

    private static bool TryConvert(ProcessorParameter parameter, object? rawValue, out object? value, out string? error)
    {
        rawValue = UnwrapJson(rawValue);
        error = null;
        value = null;

        if (rawValue is null)
            return true;

        switch (parameter.Type)
        {
            case ProcessorParameterType.Boolean:
                if (TryReadBoolean(rawValue, out var booleanValue))
                {
                    value = booleanValue;
                    return true;
                }

                error = $"Parameter '{parameter.Id}' expects true or false.";
                return false;

            case ProcessorParameterType.Integer:
                if (TryReadDecimal(rawValue, out var integerNumber)
                    && integerNumber == decimal.Truncate(integerNumber)
                    && integerNumber is >= int.MinValue and <= int.MaxValue)
                {
                    var integerValue = decimal.ToInt32(integerNumber);
                    if (!IsInRange(parameter, integerNumber, out error))
                        return false;

                    value = integerValue;
                    return true;
                }

                error = $"Parameter '{parameter.Id}' expects an integer.";
                return false;

            case ProcessorParameterType.Float:
                if (TryReadDecimal(rawValue, out var floatNumber))
                {
                    if (!IsInRange(parameter, floatNumber, out error))
                        return false;

                    value = (float)floatNumber;
                    return true;
                }

                error = $"Parameter '{parameter.Id}' expects a floating-point value.";
                return false;

            case ProcessorParameterType.Choice:
                var choiceValue = rawValue.ToString() ?? string.Empty;
                var matchedOption = parameter.Options?.FirstOrDefault(option => string.Equals(option, choiceValue, StringComparison.OrdinalIgnoreCase));
                if (matchedOption is not null)
                {
                    value = matchedOption;
                    return true;
                }

                error = $"Parameter '{parameter.Id}' expects one of: {string.Join(", ", parameter.Options ?? [])}.";
                return false;

            case ProcessorParameterType.ImageFormat:
                if (Enum.TryParse<ImageFormat>(rawValue.ToString(), true, out var imageFormat))
                {
                    value = imageFormat;
                    return true;
                }

                error = $"Parameter '{parameter.Id}' expects a valid image format name.";
                return false;

            case ProcessorParameterType.FilePath:
            case ProcessorParameterType.String:
                value = rawValue.ToString() ?? string.Empty;
                return true;

            default:
                error = $"Unsupported parameter type '{parameter.Type}'.";
                return false;
        }
    }

    private static object? UnwrapJson(object? rawValue)
    {
        if (rawValue is not JsonElement json)
            return rawValue;

        return json.ValueKind switch
        {
            JsonValueKind.String => json.GetString(),
            JsonValueKind.Number when json.TryGetInt32(out var intValue) => intValue,
            JsonValueKind.Number when json.TryGetDouble(out var doubleValue) => doubleValue,
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => json.ToString()
        };
    }

    private static bool TryReadBoolean(object rawValue, out bool result)
    {
        switch (rawValue)
        {
            case bool booleanValue:
                result = booleanValue;
                return true;

            case string stringValue when bool.TryParse(stringValue, out var parsedBoolean):
                result = parsedBoolean;
                return true;

            case string stringValue:
                switch (stringValue.Trim().ToLowerInvariant())
                {
                    case "1":
                    case "yes":
                    case "y":
                    case "on":
                        result = true;
                        return true;

                    case "0":
                    case "no":
                    case "n":
                    case "off":
                        result = false;
                        return true;
                }

                break;

            default:
                if (TryReadDecimal(rawValue, out var number))
                {
                    result = number != 0;
                    return true;
                }

                break;
        }

        result = false;
        return false;
    }

    private static bool TryReadDecimal(object rawValue, out decimal result)
    {
        switch (rawValue)
        {
            case byte byteValue:
                result = byteValue;
                return true;
            case sbyte signedByteValue:
                result = signedByteValue;
                return true;
            case short shortValue:
                result = shortValue;
                return true;
            case ushort unsignedShortValue:
                result = unsignedShortValue;
                return true;
            case int intValue:
                result = intValue;
                return true;
            case uint unsignedIntValue:
                result = unsignedIntValue;
                return true;
            case long longValue:
                result = longValue;
                return true;
            case ulong unsignedLongValue:
                result = unsignedLongValue;
                return true;
            case float floatValue when float.IsFinite(floatValue) && floatValue is >= (float)decimal.MinValue and <= (float)decimal.MaxValue:
                result = (decimal)floatValue;
                return true;
            case double doubleValue when double.IsFinite(doubleValue) && doubleValue is >= (double)decimal.MinValue and <= (double)decimal.MaxValue:
                result = (decimal)doubleValue;
                return true;
            case decimal decimalValue:
                result = decimalValue;
                return true;
            case string stringValue when decimal.TryParse(stringValue, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsedNumber):
                result = parsedNumber;
                return true;
            default:
                result = default;
                return false;
        }
    }

    private static bool IsInRange(ProcessorParameter parameter, decimal value, out string? error)
    {
        if (parameter.MinValue is not null && TryReadDecimal(parameter.MinValue, out var minValue) && value < minValue)
        {
            error = $"Parameter '{parameter.Id}' must be greater than or equal to {minValue}.";
            return false;
        }

        if (parameter.MaxValue is not null && TryReadDecimal(parameter.MaxValue, out var maxValue) && value > maxValue)
        {
            error = $"Parameter '{parameter.Id}' must be less than or equal to {maxValue}.";
            return false;
        }

        error = null;
        return true;
    }
}