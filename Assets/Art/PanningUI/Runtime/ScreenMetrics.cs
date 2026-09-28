using System;
using UnityEngine;

namespace PanningUI
{
    public readonly struct ScreenMetricsSnapshot
    {
        public readonly int Width, Height;
        public readonly Rect SafeArea;
        public readonly ScreenOrientation Orientation;
        public ScreenMetricsSnapshot(int width, int height, Rect safeArea, ScreenOrientation orientation)
        { Width=width; Height=height; SafeArea=safeArea; Orientation=orientation; }
    }

    /// <summary>One notification per changed screen state, shared by all active UI roots.</summary>
    public static class ScreenMetrics
    {
        public static event Action<ScreenMetricsSnapshot> Changed;
        public static ScreenMetricsSnapshot Current { get; private set; }
        static bool initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void Reset() { initialized=false; Changed=null; Current=default; }

        // Called by active SafeAreaFitters; comparisons and value-type snapshots allocate nothing.
        public static void Poll()
        {
            int w=Screen.width, h=Screen.height;
            Rect safe=Screen.safeArea; var orientation=Screen.orientation;
            if(w<=0 || h<=0 || safe.width<=0 || safe.height<=0)return;
            if(initialized && Current.Width==w && Current.Height==h && Current.SafeArea==safe && Current.Orientation==orientation)return;
            Current=new ScreenMetricsSnapshot(w,h,safe,orientation); initialized=true;
            Changed?.Invoke(Current);
        }
    }
}
