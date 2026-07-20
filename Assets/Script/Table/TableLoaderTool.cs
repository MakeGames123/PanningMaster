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
    // 따옴표 안 쉼표를 필드 구분자로 취급하지 않는 CSV 한 줄 파서("" = 이스케이프된 따옴표)
    public static System.Collections.Generic.List<string> SplitCsvLine(string line)
    {
        var result = new System.Collections.Generic.List<string>();
        var sb = new System.Text.StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"') { sb.Append('"'); i++; }
                else inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(sb.ToString());
                sb.Clear();
            }
            else sb.Append(c);
        }
        result.Add(sb.ToString());
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
