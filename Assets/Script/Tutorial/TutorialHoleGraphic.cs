using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 사각형 구멍이 여러 개 뚫린 딤 오버레이 그래픽.
// 전체를 color로 덮되 지정한 타겟 사각형들만 비워서(프레임만 렌더) 구멍처럼 보이게 하고,
// 레이캐스트도 어느 구멍 안에서든 통과시킨다(ICanvasRaycastFilter).
public class TutorialHoleGraphic : MaskableGraphic, ICanvasRaycastFilter
{
    readonly List<RectTransform> targets = new();
    readonly List<Rect> holes = new();       // 각 타겟의 로컬 좌표 사각형
    readonly Vector3[] corners = new Vector3[4];

    bool HasHole => targets.Count > 0;

    // 전체 차단(구멍 없음)
    public void ShowFull()
    {
        if (targets.Count == 0 && holes.Count == 0) return;
        targets.Clear();
        holes.Clear();
        SetVerticesDirty();
    }

    // 타겟 사각형들만 구멍으로
    public void ShowHoles(IReadOnlyList<RectTransform> newTargets)
    {
        targets.Clear();
        if (newTargets != null)
            foreach (var t in newTargets) if (t != null) targets.Add(t);

        RebuildHoles();
        SetVerticesDirty();
    }

    void LateUpdate()
    {
        // 타겟이 레이아웃/애니메이션으로 움직이면 구멍 위치 따라가기
        if (targets.Count == 0) return;
        if (HolesChanged()) { RebuildHoles(); SetVerticesDirty(); }
    }

    bool HolesChanged()
    {
        if (holes.Count != targets.Count) return true;
        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == null) return true;
            if (LocalRectOf(targets[i]) != holes[i]) return true;
        }
        return false;
    }

    void RebuildHoles()
    {
        holes.Clear();
        foreach (var t in targets)
            if (t != null) holes.Add(LocalRectOf(t));
    }

    Rect LocalRectOf(RectTransform target)
    {
        target.GetWorldCorners(corners); // 0:BL 2:TR
        Vector2 a = rectTransform.InverseTransformPoint(corners[0]);
        Vector2 b = rectTransform.InverseTransformPoint(corners[2]);
        return Rect.MinMaxRect(
            Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y),
            Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
    }

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = rectTransform.rect;

        // 전체 rect 안으로 클램프한 유효 구멍들
        var hs = new List<Rect>();
        foreach (var h in holes)
        {
            float x0 = Mathf.Clamp(h.xMin, r.xMin, r.xMax);
            float x1 = Mathf.Clamp(h.xMax, r.xMin, r.xMax);
            float y0 = Mathf.Clamp(h.yMin, r.yMin, r.yMax);
            float y1 = Mathf.Clamp(h.yMax, r.yMin, r.yMax);
            if (x1 > x0 && y1 > y0) hs.Add(Rect.MinMaxRect(x0, y0, x1, y1));
        }

        if (hs.Count == 0) { AddQuad(vh, r.xMin, r.yMin, r.xMax, r.yMax); return; }

        // Y 밴드 경계(전체 + 각 구멍의 상/하단) 수집·정렬
        var ys = new List<float> { r.yMin, r.yMax };
        foreach (var h in hs) { ys.Add(h.yMin); ys.Add(h.yMax); }
        ys.Sort();

        // 각 Y 밴드마다, 그 밴드를 세로로 완전히 가로지르는 구멍들의 X구간만 빼고 채운다
        var segs = new List<Vector2>();
        for (int bi = 0; bi < ys.Count - 1; bi++)
        {
            float y0 = ys[bi], y1 = ys[bi + 1];
            if (y1 - y0 <= 0.0001f) continue;

            segs.Clear();
            foreach (var h in hs)
                if (h.yMin <= y0 + 0.0001f && h.yMax >= y1 - 0.0001f)
                    segs.Add(new Vector2(h.xMin, h.xMax));

            if (segs.Count == 0) { AddQuad(vh, r.xMin, y0, r.xMax, y1); continue; }

            segs.Sort((a, b) => a.x.CompareTo(b.x));
            float x = r.xMin;
            foreach (var s in segs)
            {
                if (s.x > x) AddQuad(vh, x, y0, s.x, y1);
                x = Mathf.Max(x, s.y);
            }
            if (x < r.xMax) AddQuad(vh, x, y0, r.xMax, y1);
        }
    }

    void AddQuad(VertexHelper vh, float xMin, float yMin, float xMax, float yMax)
    {
        if (xMax <= xMin || yMax <= yMin) return;

        int i = vh.currentVertCount;
        Color32 c = color;
        vh.AddVert(new Vector3(xMin, yMin), c, Vector2.zero);
        vh.AddVert(new Vector3(xMin, yMax), c, Vector2.zero);
        vh.AddVert(new Vector3(xMax, yMax), c, Vector2.zero);
        vh.AddVert(new Vector3(xMax, yMin), c, Vector2.zero);
        vh.AddTriangle(i, i + 1, i + 2);
        vh.AddTriangle(i, i + 2, i + 3);
    }

    // 레이캐스트: 어느 구멍 안이든 통과(false), 밖이면 차단(true).
    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        if (!HasHole) return true;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rectTransform, screenPoint, eventCamera, out Vector2 local);
        foreach (var h in holes)
            if (h.Contains(local)) return false;
        return true;
    }
}
