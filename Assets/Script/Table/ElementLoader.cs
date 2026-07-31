using System;
using System.Collections.Generic;
using System.Linq;

// Element 시트 로더. 속성 6종(fire/ice/poison/electric/light/dark) — 탄환·무기 공용(프로토 v43 확장).
// 컬럼: Id/NameKo/Emoji/ColorHex — 헤더명 기준 매핑이라 구버전 탭(Id/Emoji/ColorHex 3열)도 허용.
// 시트 순서 = 무기 속성 롤 풀 순서(프로토 EL 배열 — 균등 롤).
public class ElementLoader : ISheetLoader
{
    public static ElementLoader Instance { get; private set; }

    const string SHEET_URL =
        "https://docs.google.com/spreadsheets/d/1nVXQ0fwyor6S7wXYO4MvfMzPmkY8rNwKZyc1t827Lao/gviz/tq?tqx=out:csv&sheet=Element";

    // 프로토 EL 순서 — 시트 미로드 시 폴백·구 세이브 속성 백필(mig43 결정론)용
    public static readonly string[] DefaultOrder = { "fire", "ice", "poison", "electric", "light", "dark" };

    // 시트에 NameKo 열이 아직 없을 때(구버전 탭)의 한글명 폴백
    static readonly Dictionary<string, string> fallbackNames = new()
    {
        { "fire", "화염" }, { "ice", "얼음" }, { "poison", "독" },
        { "electric", "전기" }, { "light", "빛" }, { "dark", "어둠" },
    };

    readonly Dictionary<string, ElementData> dataDict = new();
    readonly List<string> order = new();

    public bool IsLoaded { get; private set; }
    public event Action OnLoaded;

    public string Url => SHEET_URL;

    public ElementLoader() { Instance = this; }

    public void Parse(string csv)
    {
        dataDict.Clear();
        order.Clear();

        var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length <= 1)
        {
            UnityEngine.Debug.LogError("Element CSV 데이터가 비어있음");
            return;
        }

        // 헤더명으로 컬럼 인덱스 매핑(NameKo 열 유무에 관계없이 동작)
        var header = TableLoaderTool.SplitCsvLine(lines[0]).Select(TableLoaderTool.CleanString).ToList();
        int idIdx = header.IndexOf("Id");
        int nameIdx = header.IndexOf("NameKo");
        int emojiIdx = header.IndexOf("Emoji");
        int colorIdx = header.IndexOf("ColorHex");
        if (idIdx < 0)
        {
            UnityEngine.Debug.LogError("[Element] Id 컬럼이 없음 — Element 탭이 스프레드시트에 있는지 확인 필요");
            return;
        }

        for (int i = 1; i < lines.Length; i++)
        {
            var cols = TableLoaderTool.SplitCsvLine(lines[i]);
            if (cols.Count <= idIdx) continue;

            string id = TableLoaderTool.CleanString(cols[idIdx]);
            if (string.IsNullOrEmpty(id)) continue;

            ElementData d = new()
            {
                id = id,
                nameKo = nameIdx >= 0 && nameIdx < cols.Count ? TableLoaderTool.CleanString(cols[nameIdx]) : "",
                emoji = emojiIdx >= 0 && emojiIdx < cols.Count ? TableLoaderTool.CleanString(cols[emojiIdx]) : "",
                colorHex = colorIdx >= 0 && colorIdx < cols.Count ? TableLoaderTool.CleanString(cols[colorIdx]) : "#ffffff",
            };
            if (string.IsNullOrEmpty(d.nameKo) && fallbackNames.TryGetValue(id, out var fb)) d.nameKo = fb;

            dataDict[d.id] = d;
            order.Add(d.id);
        }

        if (dataDict.Count == 0)
        {
            UnityEngine.Debug.LogError("[Element] 파싱된 행이 없음 — Element 탭이 스프레드시트에 있는지 확인 필요");
            return;
        }

        IsLoaded = true;
        OnLoaded?.Invoke();

        UnityEngine.Debug.Log($"Element Loaded: {dataDict.Count}");
    }

    public ElementData Get(string id)
        => !string.IsNullOrEmpty(id) && dataDict.TryGetValue(id, out var d) ? d : null;

    // 시트 순서대로 전체 목록(무기 속성 롤 풀)
    public List<ElementData> AllOrdered() => order.Select(id => dataDict[id]).ToList();

    public int Count => dataDict.Count;

    // BulletType(유니티 탄환 4종) → 속성 Id — 무기 속성 매칭 판정용.
    // light/dark 탄환은 아직 없음(무기만 6속성 롤 — 프로토와 동일하게 매칭 상대가 생기면 자동 연결).
    public static string IdFromBulletType(BulletType t)
    {
        switch (t)
        {
            case BulletType.FlameBullet: return "fire";
            case BulletType.ElectricBullet: return "electric";
            case BulletType.IceBullet: return "ice";
            case BulletType.PoisonBullet: return "poison";
            default: return null;
        }
    }
}

[System.Serializable]
public class ElementData
{
    public string id;       // 속성 키(fire/ice/poison/electric/light/dark)
    public string nameKo;   // 한글명(화염/얼음/독/전기/빛/어둠)
    public string emoji;    // 표시 이모지
    public string colorHex; // UI 색상
}
