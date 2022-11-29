using System.ComponentModel;
using System.Reflection;
class EnumUtil
{
    /// <summary>
    /// Generates a string representation of an Enum's values using their
    /// DescriptionAttributes, if they exist.
    /// </summary>
    /// <typeparam name="T">The Enum to generate the string representation of.</typeparam>
    /// <returns>A string with the Enum names and DescriptionAttributes. Values
    /// with no DescriptionAttribute are omitted from the string</returns>
    public static string EnumToString<T>() where T : Enum
    {
        string enumString = "";

        FieldInfo[] fieldData = typeof(T).GetFields();
        foreach (FieldInfo value in fieldData)
        {
            object[] attributeList = value.GetCustomAttributes(typeof(DescriptionAttribute), false);

            // Skip values with no Description attribute
            // This allows for selectively omitting enum values from the string
            if(attributeList.Length == 0)
                continue;
            string valueDescription = ((DescriptionAttribute)attributeList[0]).Description;

            enumString += '\n' + value.Name + " - " + valueDescription;
        }
        // Omit first newline character
        return enumString.Length == 0 ? "" : enumString.Substring(1);
    }
}