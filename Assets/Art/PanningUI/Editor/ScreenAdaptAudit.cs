using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace PanningUI.Editor
{
    public static class ScreenAdaptAudit
    {
        [Serializable] public class Evidence
        {
            public string prefab;
            public int width,height,buttons,texts;
            public Vector2 canvasSize,columnSize;
            public Rect safeArea,dimRect,closeRect;
            public bool expand,fullDim,buttonsInsideColumn,uniformScale,closeAtNavigationTop,pillarsCorrect;
            public string[] issues;
        }
        static Rect ScreenRect(RectTransform rt)
        {
            var corners=new Vector3[4];rt.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x,corners[0].y,corners[2].x,corners[2].y);
        }
        public static void Capture(GameObject root,Vector2Int size,string path)
        {
            var scaler=root.GetComponent<CanvasScaler>();var canvas=root.GetComponent<Canvas>();
            var column=root.transform.Find("SafeArea/Column") as RectTransform;
            var close=column.Find("Bottom/Close") as RectTransform;
            var dim=root.transform.Find("FullBleed/Dim90") as RectTransform;
            var buttons=root.GetComponentsInChildren<Button>(true);
            var texts=root.GetComponentsInChildren<TMPro.TMP_Text>(true);
            var e=new Evidence {prefab=root.name,width=size.x,height=size.y,buttons=buttons.Length,texts=texts.Length,
                canvasSize=((RectTransform)root.transform).rect.size,columnSize=column.rect.size,safeArea=Screen.safeArea,
                dimRect=dim==null?default:ScreenRect(dim),closeRect=close==null?default:ScreenRect(close),
                expand=scaler.screenMatchMode==CanvasScaler.ScreenMatchMode.Expand,
                buttonsInsideColumn=buttons.All(b=>b.transform.IsChildOf(column)) && column.GetComponent<RectMask2D>()!=null,
                uniformScale=root.GetComponentsInChildren<RectTransform>(true).All(r=>Mathf.Abs(r.lossyScale.x-r.lossyScale.y)<.001f),
                fullDim=dim==null || (Mathf.Abs(ScreenRect(dim).width-size.x)<.1f && Mathf.Abs(ScreenRect(dim).height-size.y)<.1f),
                closeAtNavigationTop=close==null || Mathf.Abs(ScreenRect(close).yMax-(Screen.safeArea.yMin+177*canvas.scaleFactor))<.1f,
                pillarsCorrect=root.transform.Find("FullBleed/PillarFill").gameObject.activeSelf==(((RectTransform)root.transform).rect.width>1200.01f)};
            var issues=new System.Collections.Generic.List<string>();
            foreach(var b in buttons.Where(b=>b.IsActive() && b.name!="ReceivedLayer")) {
                var r=ScreenRect((RectTransform)b.transform);
                if(r.yMin<Screen.safeArea.yMin-.1f || r.yMax>Screen.safeArea.yMax+.1f)issues.Add("Button outside safe height: "+b.name);
                if(close!=null && b.transform!=close && r.Overlaps(e.closeRect))issues.Add("Button overlaps Close: "+b.name);
            }
            foreach(var t in texts.Where(t=>t.IsActive())) {
                // Test rendered glyph bounds; TMP rects intentionally allow overflow for ascenders.
                t.ForceMeshUpdate();var bounds=t.textBounds;
                var lo=t.transform.TransformPoint(bounds.min);var hi=t.transform.TransformPoint(bounds.max);
                var c=ScreenRect(column);
                if(lo.x<c.xMin-.5f || hi.x>c.xMax+.5f || lo.y<Screen.safeArea.yMin-.5f || hi.y>Screen.safeArea.yMax+.5f)issues.Add("Glyphs outside safe column: "+t.name);
            }
            e.issues=issues.ToArray();File.WriteAllText(Path.ChangeExtension(path,"json"),JsonUtility.ToJson(e,true));
        }
    }
}
