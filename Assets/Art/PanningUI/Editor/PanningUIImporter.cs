using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

// Assets/Art/PanningUI/Sprites 아래 PNG를 처음 가져올 때만 UI 스프라이트 설정 + 9-slice 테두리를 적용한다.
// 테두리 값은 Docs/unity-import.csv 의 "SpriteBorder(L,B,R,T px)" 열. 이미 설정된 파일은 건드리지 않는다.
public class PanningUIImporter : AssetPostprocessor
{
    const string Root = "Assets/Art/PanningUI/Sprites/";
    const string CsvPath = "Assets/Art/PanningUI/Docs/unity-import.csv";

    static Dictionary<string, Vector4> borders;

    void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith(Root)) return;
        var importer = (TextureImporter)assetImporter;
        if (!importer.importSettingsMissing) return;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.sRGBTexture = true;
        importer.mipmapEnabled = false;
        importer.isReadable = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 4096;
        importer.spritePixelsPerUnit = 100;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);

        if (LoadBorders().TryGetValue(Path.GetFileName(assetPath), out var b))
            importer.spriteBorder = b;
    }

    static Dictionary<string, Vector4> LoadBorders()
    {
        if (borders != null) return borders;
        borders = new Dictionary<string, Vector4>();
        if (!File.Exists(CsvPath)) return borders;

        var lines = File.ReadAllLines(CsvPath, Encoding.UTF8);
        for (int i = 1; i < lines.Length; i++)
        {
            var cols = SplitCsv(lines[i]);
            if (cols.Count < 5 || cols[3] != "Sliced") continue;
            var p = cols[4].Split(',');
            if (p.Length != 4) continue;
            borders[Path.GetFileName(cols[0])] =
                new Vector4(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2]), float.Parse(p[3]));
        }
        return borders;
    }

    static List<string> SplitCsv(string line)
    {
        var cols = new List<string>();
        var sb = new StringBuilder();
        bool quoted = false;
        foreach (char c in line)
        {
            if (c == '"') quoted = !quoted;
            else if (c == ',' && !quoted) { cols.Add(sb.ToString()); sb.Clear(); }
            else sb.Append(c);
        }
        cols.Add(sb.ToString());
        return cols;
    }
}
