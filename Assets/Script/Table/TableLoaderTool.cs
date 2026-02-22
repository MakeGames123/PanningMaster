using UnityEngine;

public static class TableLoaderTool
{
    public static int ToInt(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        string cleaned = value
            .Trim()
            .Trim('"');

        if (!int.TryParse(cleaned, out int result))
        {
            Debug.LogWarning($"Int Parse 실패: '{value}'");
            return 0;
        }

        return result;
    }
    public static double ToDouble(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        string cleaned = value
            .Trim()
            .Trim('"');

        if (!double.TryParse(cleaned, out double result))
        {
            Debug.LogWarning($"Int Parse 실패: '{value}'");
            return 0;
        }

        return result;
    }
    public static float ToFloat(string value)
    {
        if (string.IsNullOrEmpty(value))
            return 0;

        string cleaned = value
            .Trim()
            .Trim('"');

        if (!float.TryParse(cleaned, out float result))
        {
            Debug.LogWarning($"Int Parse 실패: '{value}'");
            return 0;
        }

        return result;
    }
    public static string CleanString(string value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;

        return value
            .Trim()
            .Trim('"')
            .Replace("\uFEFF", "")   // BOM
            .Replace("\u200B", "")   // Zero-width space
            .Normalize(System.Text.NormalizationForm.FormC);
    }
}
