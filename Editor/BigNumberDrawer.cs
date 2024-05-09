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
    public const string NumberSeparator = " "; // Can be anything except '.', since period is used as decimal separator in Unity.

    /// <summary> If number is equals or bigger than this value, an abbreviated version of this number would appear in label. </summary>
    public const int ShowAbbreviationMin = 10_000;

    /// <summary> Amount of digits shown after '.' in abbreviated values. </summary>
    public const int DecimalPlacesInAbbreviated = 0;

    #endregion

    private static readonly NumberFormatInfo SeparatorFormat = new NumberFormatInfo { NumberGroupSeparator = NumberSeparator, NumberDecimalDigits = 0 };

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
		string toText = string.Empty;
		double number = 0;

		switch (property.type)
		{
			case "int":
				number = property.intValue;

				break;
			case "long":
				number = property.longValue;

				break;
			case "float":
				number = property.floatValue;

				break;
			case "double":
				number = property.doubleValue;
				break;
		}

		toText = AddSeparators(number);

		if (Math.Abs(number) >= ShowAbbreviationMin) label.text = $"{label.text} ({Abbreviate(number, DecimalPlacesInAbbreviated)})";


		EditorGUI.BeginProperty(position, label, property);
		toText = EditorGUI.TextField(position, label, toText);
        EditorGUI.EndProperty();


        string parsable = toText.Replace(NumberSeparator, string.Empty).Replace(" ", string.Empty);
		try
        {
            double value = double.Parse(parsable, CultureInfo.InvariantCulture);

            switch (property.type)
			{
				case "int":
                    value = Math.Clamp(value, int.MinValue, int.MaxValue);
                    property.intValue = (int)value;

					break;
				case "long":
                    value = Math.Clamp(value, long.MinValue, long.MaxValue);
                    property.longValue = (long)value;

                    break;
				case "float":
                    value = Math.Clamp(value, float.MinValue, float.MaxValue);
                    property.floatValue = (float)value;

					break;
				case "double":
					property.doubleValue = value;

					break;
			}
			property.serializedObject.ApplyModifiedProperties();
		}
		catch(System.Exception)
        {
            //Any formatting errors will be ignored, Unity will automatically revert to last correct input.
        }
	}

    private string AddSeparators(double value)
    {
        StringBuilder sb = new StringBuilder();

        sb.Append(value.ToString("N", SeparatorFormat));

        if((decimal)value % 1 != 0) //Adding separators for the decimal part.
        {
            string decimals = ((decimal)value % 1).ToString().Remove(0, 2);
            sb.Append('.');

            for (int i = 0; i < decimals.Length; i++)
            {
                if (i != 0 && i % 3 == 0) sb.Append(NumberSeparator);

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
        if (negative) numberString = numberString.Insert(0, "-");

        return numberString;
    }

    /// <summary> Suffixes for numbers based on how many digits they have left of the decimal point. </summary>
    /// <remarks> Must be ordered from small to large. </remarks>
    private enum NumberSuffix
    {
        /// <summary> Thousand = 1_000 </summary>
        K = 1,
        /// <summary> Million = 1_000_000 </summary>
        M = 2,
        /// <summary> Billion = 1_000_000_000 </summary>
        B = 3,
        /// <summary> Trillion = 1_000_000_000_000 </summary>
        T = 4,
        /// <summary> Quadrillion = 1_000_000_000_000_000</summary>
        Q = 5
    }
}