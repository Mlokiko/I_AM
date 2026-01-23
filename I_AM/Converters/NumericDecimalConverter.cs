using System.Globalization;

namespace I_AM.Converters;

/// <summary>
/// Converter that filters user input to allow only numeric characters and decimal separators
/// </summary>
public class NumericDecimalConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null)
            return string.Empty;

        return value.ToString() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string input || string.IsNullOrEmpty(input))
            return 0m;

        // Filtruj tekst - zostaw tylko cyfry, przecinki, kropki i minus na pocz¹tku
        var filtered = new System.Text.StringBuilder();
        bool hasDecimalSeparator = false;

        for (int i = 0; i < input.Length; i++)
        {
            char c = input[i];

            if (char.IsDigit(c))
            {
                filtered.Append(c);
            }
            else if ((c == ',' || c == '.') && !hasDecimalSeparator && filtered.Length > 0)
            {
                filtered.Append(c);
                hasDecimalSeparator = true;
            }
            else if (c == '-' && i == 0 && filtered.Length == 0)
            {
                filtered.Append(c);
            }
        }

        string cleanText = filtered.ToString();

        // Konwertuj na decimal
        if (string.IsNullOrEmpty(cleanText))
            return 0m;

        // Zamieñ kropkê na przecinek dla kultury polskiej
        cleanText = cleanText.Replace(".", ",");

        if (decimal.TryParse(cleanText, NumberStyles.Any, CultureInfo.GetCultureInfo("pl-PL"), out decimal result))
        {
            return result;
        }

        return 0m;
    }
}