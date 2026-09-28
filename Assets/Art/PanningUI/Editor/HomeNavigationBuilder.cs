using System;
using UnityEngine;

namespace PanningUI.Editor
{
    public static class HomeNavigationBuilder
    {
        // Six menus around a reserved, non-interactive centre slot. The popup owns X.
        public static void Apply(GameObject root)
        {
            var body=root.transform.Find("SafeArea/Column/Bottom/BottomContent");
            if(body==null)throw new InvalidOperationException("Adapt the home hierarchy first");
            const float cell=1200f/7f;
            for(int i=0;i<6;i++) {
                int slot=i<3?i:i+1;
                var rt=(RectTransform)body.Find("NavButton_"+i);
                rt.anchoredPosition=new Vector2(slot*cell,rt.anchoredPosition.y);
                rt.sizeDelta=new Vector2(cell,rt.sizeDelta.y);
            }
            string[] names={"NavIcon_character","NavIcon_growth","NavIcon_dungeon","NavIcon_weapon","ico_lock","NavIcon_shop"};
            for(int i=0;i<names.Length;i++) {
                var rt=(RectTransform)body.Find(names[i]);int slot=i<3?i:i+1;
                rt.anchoredPosition=new Vector2((slot+.5f)*cell-rt.sizeDelta.x*.5f,rt.anchoredPosition.y);
            }
            var selected=(RectTransform)body.Find("SelectedNav");
            selected.anchoredPosition=new Vector2((4+.18f)*cell,selected.anchoredPosition.y);
            selected.sizeDelta=new Vector2(.64f*cell,selected.sizeDelta.y);
            var label=(RectTransform)body.Find("HomeText_57");
            label.anchoredPosition=new Vector2(4.5f*cell-label.sizeDelta.x*.5f,label.anchoredPosition.y);
            var dot=(RectTransform)body.Find("NavNotification_0");
            dot.anchoredPosition=new Vector2(.5f*cell+30.99f,dot.anchoredPosition.y);
            var empty=body.Find("NavCenterEmpty") as RectTransform;
            if(empty==null)empty=ScreenAdaptBuilder.Rect("NavCenterEmpty",body);
            empty.anchorMin=empty.anchorMax=empty.pivot=new Vector2(0,1);
            empty.anchoredPosition=new Vector2(3*cell,-2427);empty.sizeDelta=new Vector2(cell,174);
        }
    }
}
