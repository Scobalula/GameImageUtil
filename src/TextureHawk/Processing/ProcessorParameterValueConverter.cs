using System.Globalization;

namespace TextureHawk.Processing;

internal static class ProcessorParameterValueConverter
{
    public static bool TryConvert<T>(object value, out T convertedValue)
    {
        var targetType = typeof(T);
        if (targetType == typeof(string))
        {
            convertedValue = (T)(object)value.ToString()!;
            return true;
        }

        if (targetType == typeof(bool) && TryConvertToBoolean(value, out var booleanValue))
        {
            convertedValue = (T)(object)booleanValue;
            return true;
        }

        if (TryReadNumber(value, out var number))
        {
            if (targetType == typeof(int) && number == decimal.Truncate(number) && number is >= int.MinValue and <= int.MaxValue)
            {
                convertedValue = (T)(object)decimal.ToInt32(number);
                return true;
            }

            if (targetType == typeof(float))
            {
                convertedValue = (T)(object)(float)number;
                return true;
            }

            if (targetType == typeof(double))
            {
                convertedValue = (T)(object)(double)number;
                return true;
            }
        }

        if (targetType.IsEnum && TryConvertToEnum(value, targetType, out var enumValue))
        {
            convertedValue = (T)enumValue;
            return true;
        }

        convertedValue = default!;
        return false;
    }

    private static bool TryConvertToBoolean(object value, out bool result)
    {
        if (value is bool boolValue)
        {
            result = boolValue;
            return true;
        }

        if (value is string stringValue && bool.TryParse(stringValue, out var parsedBoolean))
        {
            result = parsedBoolean;
            return true;
        }

        if (TryReadNumber(value, out var number))
        {
            result = number != 0;
            return true;
        }

        result = default;
        return false;
    }

    private static bool TryReadNumber(object value, out decimal result)
    {
        switch (value)
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

    private static bool TryConvertToEnum(object value, Type enumType, out object result)
    {
        if (value is string stringValue && Enum.TryParse(enumType, stringValue, true, out var parsedEnum))
        {
            result = parsedEnum;
            return true;
        }

        if (TryReadNumber(value, out var number) && number == decimal.Truncate(number) && number is >= int.MinValue and <= int.MaxValue)
        {
            result = Enum.ToObject(enumType, decimal.ToInt32(number));
            return true;
        }

        result = default!;
        return false;
    }
}