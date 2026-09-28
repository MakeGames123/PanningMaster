using UnityEngine;

namespace PanningUI
{
    /// <summary>Attach directly below a full-screen Canvas. Backgrounds belong outside this rect.</summary>
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(RectTransform)), DefaultExecutionOrder(100)]
    public sealed class SafeAreaFitter : MonoBehaviour
    {
        RectTransform rect;
        void OnEnable()
        {
            rect=(RectTransform)transform;
            ScreenMetrics.Changed+=Apply;
            ScreenMetrics.Poll(); Apply(ScreenMetrics.Current);
        }
        void OnDisable() { ScreenMetrics.Changed-=Apply; }
        void Update() { ScreenMetrics.Poll(); }

        public void Apply(ScreenMetricsSnapshot metrics)
        {
            if(metrics.Width<=0 || metrics.Height<=0 || metrics.SafeArea.width<=0 || metrics.SafeArea.height<=0)return;
            if(rect==null)rect=(RectTransform)transform;
            var safe=metrics.SafeArea;
            rect.anchorMin=new Vector2(safe.xMin/metrics.Width,safe.yMin/metrics.Height);
            rect.anchorMax=new Vector2(safe.xMax/metrics.Width,safe.yMax/metrics.Height);
            rect.offsetMin=rect.offsetMax=Vector2.zero;
        }
    }
}
