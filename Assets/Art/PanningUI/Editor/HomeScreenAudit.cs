using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PanningUI.Editor
{
    public static class HomeScreenAudit
    {
        [Serializable] public class Evidence {
            public string screen="ScreenMainHome";
            public int width,height,buttons,texts;
            public Rect safeArea,column,navigation,middle;
            public bool expand,uniformScale,pillarsCorrect,navigationAt177,centreEmpty,providedAssetsCorrect;
            public string[] issues;
        }
        static Rect Bounds(RectTransform rt) {
            var c=new Vector3[4];rt.GetWorldCorners(c);
            return Rect.MinMaxRect(c[0].x,c[0].y,c[2].x,c[2].y);
        }
        static bool InInventory(Transform t) {
            for(;t!=null;t=t.parent)if(t.name=="InventoryViewport")return true;
            return false;
        }
        public static void Capture(GameObject root,Vector2Int size,string path) {
            var c=root.transform.Find("SafeArea/Column") as RectTransform;
            if(c==null)throw new InvalidOperationException("Home must use adaptive hierarchy");
            var canvas=root.GetComponent<Canvas>();
            var nav=c.Find("Bottom/BottomContent/Navigation") as RectTransform;
            var buttons=root.GetComponentsInChildren<Button>(true);
            var texts=root.GetComponentsInChildren<TMP_Text>(true);
            var e=new Evidence {width=size.x,height=size.y,safeArea=Screen.safeArea,column=Bounds(c),navigation=Bounds(nav),
                middle=Bounds((RectTransform)c.Find("Middle")),buttons=buttons.Length,texts=texts.Length,
                expand=root.GetComponent<CanvasScaler>().screenMatchMode==CanvasScaler.ScreenMatchMode.Expand,
                uniformScale=root.GetComponentsInChildren<RectTransform>(true).All(r=>Mathf.Abs(r.lossyScale.x-r.lossyScale.y)<.001f),
                pillarsCorrect=root.transform.Find("FullBleed/PillarFill").gameObject.activeSelf==(((RectTransform)root.transform).rect.width>1200.01f),
                navigationAt177=Mathf.Abs(Bounds(nav).yMax-(Screen.safeArea.yMin+177*canvas.scaleFactor))<.1f};
            var issues=new List<string>();
            var body=c.Find("Bottom/BottomContent");
            var gap=body.Find("NavCenterEmpty") as RectTransform;
            e.centreEmpty=gap!=null && gap.GetComponent<Graphic>()==null && gap.GetComponent<Button>()==null;
            if(gap!=null)foreach(var b in buttons.Where(b=>b.name.StartsWith("NavButton_",StringComparison.Ordinal))) {
                var r=(RectTransform)b.transform;
                if(r.anchoredPosition.x<gap.anchoredPosition.x+gap.sizeDelta.x-.01f && r.anchoredPosition.x+r.sizeDelta.x>gap.anchoredPosition.x+.01f)e.centreEmpty=false;
            }
            var images=root.GetComponentsInChildren<Image>(true);
            string[] required={"ui_hud_currency_frame","ui_hud_notice_background","ui_chat_bubble","ui_hud_quest","ui_hud_auto_formation"};
            e.providedAssetsCorrect=required.All(name=>images.Any(i=>i.sprite!=null&&i.sprite.name==name&&i.preserveAspect))
                && !images.Any(i=>i.sprite!=null&&(i.sprite.name.StartsWith("ui_slot_",StringComparison.Ordinal)||i.sprite.name=="ui_badge_equipped_ko"));
            foreach(var b in buttons) {
                if(!b.transform.IsChildOf(c))issues.Add("Button outside Column: "+b.name);
                var r=Bounds((RectTransform)b.transform);
                if(r.xMin<e.column.xMin-.5f || r.xMax>e.column.xMax+.5f || r.yMin<e.safeArea.yMin-.5f || r.yMax>e.safeArea.yMax+.5f)issues.Add("Button outside safe column: "+b.name);
            }
            foreach(var t in texts.Where(t=>t.IsActive()&&!InInventory(t.transform))) {
                t.ForceMeshUpdate();var b=t.textBounds;var lo=t.transform.TransformPoint(b.min);var hi=t.transform.TransformPoint(b.max);
                if(lo.x<e.column.xMin-.5f||hi.x>e.column.xMax+.5f||lo.y<e.safeArea.yMin-.5f||hi.y>e.safeArea.yMax+.5f)issues.Add("Glyph outside safe column: "+t.name);
            }
            if(e.middle.height<0)issues.Add("HUD and bottom controls overlap");
            if(!e.expand||!e.uniformScale||!e.pillarsCorrect||!e.navigationAt177)issues.Add("Canvas/scale/pillar/navigation invariant failed");
            if(!e.centreEmpty||!e.providedAssetsCorrect)issues.Add("Home asset selection or empty centre invariant failed");
            e.issues=issues.ToArray();File.WriteAllText(Path.ChangeExtension(path,"json"),JsonUtility.ToJson(e,true));
        }
    }
}
