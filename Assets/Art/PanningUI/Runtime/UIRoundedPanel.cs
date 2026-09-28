using UnityEngine;
using UnityEngine.UI;

namespace PanningUI
{
    // Code-drawn panel geometry only. Labels and icon artwork remain separate objects.
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("UI/Rounded Gradient Panel")]
    public sealed class UIRoundedPanel : MaskableGraphic
    {
        [SerializeField] float radius;
        [SerializeField] float borderWidth;
        [SerializeField] Color bottomColor = Color.white;
        [SerializeField] Color borderColor = Color.clear;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            if (r.width <= 0 || r.height <= 0) return;
            float outerRadius = Mathf.Clamp(radius, 0, Mathf.Min(r.width, r.height) * .5f);
            float inset = Mathf.Clamp(borderWidth, 0, Mathf.Min(r.width, r.height) * .5f);
            Rect inner = new Rect(r.x + inset, r.y + inset, r.width - inset * 2, r.height - inset * 2);
            const int steps = 12, count = 4 * (steps + 1);
            vh.AddVert(inner.center, Color.Lerp(bottomColor, color, .5f), Vector2.one * .5f);
            for (int i = 0; i < count; i++)
            {
                Vector2 point = Point(inner, Mathf.Max(0, outerRadius - inset), i, steps);
                vh.AddVert(point, Color.Lerp(bottomColor, color, Mathf.InverseLerp(r.yMin, r.yMax, point.y)), Vector2.zero);
            }
            for (int i = 0; i < count; i++) vh.AddTriangle(0, 1 + i, 1 + (i + 1) % count);
            if (inset <= 0) return;
            int first = vh.currentVertCount;
            for (int i = 0; i < count; i++)
            {
                vh.AddVert(Point(r, outerRadius, i, steps), borderColor, Vector2.zero);
                vh.AddVert(Point(inner, Mathf.Max(0, outerRadius - inset), i, steps), borderColor, Vector2.zero);
            }
            for (int i = 0; i < count; i++)
            {
                int a = first + i * 2, b = first + ((i + 1) % count) * 2;
                vh.AddTriangle(a, b, a + 1); vh.AddTriangle(a + 1, b, b + 1);
            }
        }

        static Vector2 Point(Rect r, float rad, int index, int steps)
        {
            int corner = index / (steps + 1);
            float angle = (corner * 90f + index % (steps + 1) * 90f / steps) * Mathf.Deg2Rad;
            Vector2 center = new Vector2(corner == 0 || corner == 3 ? r.xMax - rad : r.xMin + rad,
                                         corner < 2 ? r.yMax - rad : r.yMin + rad);
            return center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * rad;
        }
    }
}
