// Must be placed within a folder named "Editor"
using System;
using System.Globalization;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary> Use it to make large numbers appear more readable without use of exponents.
/// <para>Supported types: int, long, float, double</para></summary>
[CustomPropertyDrawer(typeof(BigNumberAttribute))]
public class BigNumberDrawer : PropertyDrawer
{
    //Use those to tweak for your taste.
    #region Options

    /// <summary> Symbol used to separate groups of digits inside a number. </summary>
    private const string NumberSeparator = " "; // Can be anything except '.', since period is used as decimal separator in Unity.

    /// <summary> If number is equals or bigger than this value, an abbreviated version would appear in label. </summary>
    private const int ShowAbbreviationMin = 10_000;

    /// <summary> Amount of digits shown after '.' in abbreviated values. </summary>
    private const int DecimalPlacesInAbbreviated = 2;

    /// <summary> Ignore any digits for float if its larger than this number. Floating point presision is rough once numbers go big. </summary>
    private const float FloatMaxNumberForDigits = 8_388_608f; //2^23
    /// <summary> Ignore any digits for double if its larger than this number. Floating point presision is rough once numbers go big. </summary>
    private const double DoubleMaxNumberForDigits = 4_503_599_627_370_496d; //2^52

    #endregion



    private static readonly NumberFormatInfo SeparatorFormat = new NumberFormatInfo { NumberGroupSeparator = NumberSeparator, NumberDecimalDigits = 0 };

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
		string toText = string.Empty;

        try
        {
            //Have to use different value types to avoid precision errors when parsing.
            switch (property.type)
            {
                case "int":
                    int numberInt = property.intValue;

                    toText = AddSeparators(numberInt);
                    if (Math.Abs(numberInt) >= ShowAbbreviationMin) label.text = $"{label.text} ({Abbreviate(numberInt, DecimalPlacesInAbbreviated)})";

                    break;
                case "long":
                    long numberLong = property.longValue;

                    toText = AddSeparators(numberLong);
                    if (Math.Abs(numberLong) >= ShowAbbreviationMin) label.text = $"{label.text} ({Abbreviate(numberLong, DecimalPlacesInAbbreviated)})";

                    break;
                case "float":
                    float numberFloat = property.floatValue;

                    toText = AddSeparators(numberFloat);
                    if (Math.Abs(numberFloat) >= ShowAbbreviationMin || (numberFloat % 1).ToString().Length > 4)
                        label.text = $"{label.text} ({Abbreviate(numberFloat, DecimalPlacesInAbbreviated)})";
                    break;
                case "double":
                    double numberDouble = property.doubleValue;

                    toText = AddSeparators(numberDouble);
                    if (Math.Abs(numberDouble) >= ShowAbbreviationMin || (numberDouble % 1).ToString().Length > 4)
                        label.text = $"{label.text} ({Abbreviate(numberDouble, DecimalPlacesInAbbreviated)})";
                    break;
            }
        }
        catch (OverflowException) { } //Trying to Math.Abs a minimum value will throw OverflowException.

        EditorGUI.BeginProperty(position, label, property);
        string input = EditorGUI.TextField(position, label, toText);
        EditorGUI.EndProperty();

        if (input == toText) return;

        string parsable = input.Replace(NumberSeparator, string.Empty).Replace(" ", string.Empty);
        if (string.IsNullOrWhiteSpace(parsable)) parsable = "0";

        //Todo: add math expression support. 
        try
        {
            switch (property.type)
			{
				case "int":
                    try {
                        property.intValue = int.Parse(parsable, CultureInfo.InvariantCulture);
                    }
                    catch (OverflowException)
                    {
                        if (property.intValue > 0) property.intValue = int.MaxValue;
                        else property.intValue = int.MinValue;
                    }
                    break;
				case "long":
                    try {
                        property.longValue = long.Parse(parsable, CultureInfo.InvariantCulture);
                    }
                    catch (OverflowException){
                        if (property.longValue > 0) property.longValue = long.MaxValue;
                        else property.longValue = long.MinValue;
                    }
                    break;
				case "float":
                    try {
                        property.floatValue = float.Parse(parsable, CultureInfo.InvariantCulture);
                    }
                    catch (OverflowException) { 
                    }
                    break;
				case "double":
					try {
                        property.doubleValue = double.Parse(parsable, CultureInfo.InvariantCulture);
                    }
                    catch (OverflowException) {
                    } 
					break;
			}
        }
        catch (System.Exception)
        {
            //Any formatting errors will be ignored, Unity will automatically revert to last correct input.
        }
        property.serializedObject.ApplyModifiedProperties();

    }

    private string AddSeparators(long value) { return value.ToString("N", SeparatorFormat); } 
    private string AddSeparators(double value)
    {
        StringBuilder sb = new StringBuilder();

        sb.Append(Math.Truncate(value).ToString("N", SeparatorFormat));

        string decimals = value.ToString(CultureInfo.InvariantCulture);
        if (decimals.Contains('.') && 
            value < DoubleMaxNumberForDigits && value > -DoubleMaxNumberForDigits)
        {
            decimals = decimals.Substring(decimals.LastIndexOf('.') + 1);
            sb.Append('.');

            for (int i = 0; i < decimals.Length; i++)
            {
                if (i != 0 && i % 3 == 0) sb.Append(NumberSeparator); //Adding separators for the decimal part.

                sb.Append(decimals[i]);
            }
        }

        return sb.ToString();
    }

    //Maybe avoidable with 'dynamic' value - avoiding it for compatibility.
    private string AddSeparators(float value)
    {
        StringBuilder sb = new StringBuilder();

        sb.Append(Mathf.Floor(value).ToString("N", SeparatorFormat)); //A very small number will display in scientific notation. Todo..? 

        string decimals = value.ToString(CultureInfo.InvariantCulture);
        if (decimals.Contains('.') && value < FloatMaxNumberForDigits && value > -FloatMaxNumberForDigits)
        {
            decimals = decimals.Substring(decimals.LastIndexOf('.') + 1);
            sb.Append('.');

            for (int i = 0; i < decimals.Length; i++)
            {
                if (i != 0 && i % 3 == 0) sb.Append(NumberSeparator); //Adding separators for the decimal part.

                sb.Append(decimals[i]);
            }
        }

        return sb.ToString();
    }

    /// <summary> Abbreviates a number into more pleasant to read (123 456 789 123 -> 123.45B) </summary>
    /// <param name="decimalPlaces"> Amount of digits after point. </param>
    public static string Abbreviate(double number, int decimalPlaces = 2)
    {
        bool negative = number < 0;
        number = Math.Abs(number);
        string numberString = number.ToString();

        if (number < ShowAbbreviationMin) 
        {
            numberString = Math.Round(number, decimalPlaces).ToString(CultureInfo.InvariantCulture);
        }
        else 
        {
            foreach (NumberSuffix suffix in Enum.GetValues(typeof(NumberSuffix)))
            {
                // Assign the amount of digits to base 10.
                double currentDigitValue = 1 * Math.Pow(10, (int)suffix * 3);

                string suffixText = Enum.GetName(typeof(NumberSuffix), (int)suffix);
                if (suffix == 0) { suffixText = string.Empty; }

                // Set the return value to a rounded value with the suffix.
                if (number >= currentDigitValue)
                {
                    numberString = $"{Math.Round(number / currentDigitValue, decimalPlaces, MidpointRounding.ToEven)} {suffixText}";
                }
            }
        }

        if (negative) numberString = numberString.Insert(0, "-");
        return numberString;
    }

    /// <summary> Suffixes for numbers based on how many digits they have left of the decimal point. </summary>
    /// <remarks> Must be ordered from small to large. </remarks>
    private enum NumberSuffix 
    {
        //Realistically could be metric symbols, feel free to rename them as you wish.

        /// <summary> Thousand (k) = 1_000 </summary>
        K = 1,
        /// <summary> Million (M) = 1_000_000 </summary>
        Million = 2,
        /// <summary> Billion (G) = 1_000_000_000 </summary>
        Billion = 3,
        /// <summary> Trillion (T) = 1_000_000_000_000 </summary>
        Trillion = 4,
        /// <summary> Quadrillion (P) = 1_000_000_000_000_000 </summary>
        Quadrillion = 5,
        /// <summary> Quintillion (E) = 1_000_000_000_000_000_000 </summary>
        Quintillion = 6,
        /// <summary> Sextillion (Z) = 1_000_000_000_000_000_000_000 </summary>
        Sextillion = 7,
        /// <summary> Septillion (Y) = 1_000_000_000_000_000_000_000_000 </summary>
        Septillion = 8,
        /// <summary> Octillion (R) = 1_000_000_000_000_000_000_000_000_000 </summary>
        Octillion = 9,
        /// <summary> Nonillion (Q) = 1_000_000_000_000_000_000_000_000_000_000 </summary>
        Nonillion = 10
    }
}