using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace PanningUI.Editor
{
    public static class HomeProvidedAssetsBuilder
    {
        const string Root="Assets/Art/PanningUI/Sprites/";
        public static void Apply(GameObject root)
        {
            var top=root.transform.Find("SafeArea/Column/Top/TopContent");
            var bottom=root.transform.Find("SafeArea/Column/Bottom/BottomContent");
            Set(top.Find("Gold"),"HUD/ui_hud_currency_frame.png");
            Set(top.Find("Ruby"),"HUD/ui_hud_currency_frame.png");
            Set(bottom.Find("Chat"),"HUD/ui_hud_notice_background.png");
            Set(bottom.Find("AutoFormation"),"HUD/ui_hud_auto_formation.png");
            Set(bottom.Find("Quest"),"HUD/ui_hud_quest.png");
            var chat=bottom.Find("Chat");
            var bubble=chat.Find("ChatSummaryBubble") as RectTransform;
            if(bubble==null)bubble=ScreenAdaptBuilder.Rect("ChatSummaryBubble",chat);
            var image=bubble.GetComponent<Image>()??bubble.gameObject.AddComponent<Image>();
            image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+"Chat/ui_chat_bubble.png");
            image.color=Color.white;image.type=Image.Type.Simple;image.preserveAspect=true;image.raycastTarget=false;
            // The supplied quest panel already contains its arrow artwork.
            bottom.Find("HomeText_20").gameObject.SetActive(false);
            // Leave room for the source bubble and quest chevrons; do not paint over them.
            MoveX(bottom,"HomeText_12",138);
            MoveX(bottom,"HomeText_13",298.08f);
            MoveX(bottom,"HomeText_19",978.42f);
            var track=(RectTransform)bottom.Find("QuestProgressTrack");track.sizeDelta=new Vector2(240,track.sizeDelta.y);
            var progress=(RectTransform)bottom.Find("QuestProgress");progress.sizeDelta=new Vector2(72,progress.sizeDelta.y);
        }
        static void MoveX(Transform parent,string name,float x) {var rt=(RectTransform)parent.Find(name);rt.anchoredPosition=new Vector2(x,rt.anchoredPosition.y);}
        static void Set(Transform node,string path)
        {
            if(node==null)throw new InvalidOperationException("Home widget missing");
            var sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Root+path);
            if(sprite==null)throw new InvalidOperationException("Provided home sprite missing: "+path);
            var rt=(RectTransform)node;
            float centreY=rt.anchoredPosition.y-rt.sizeDelta.y*.5f;
            float height=rt.sizeDelta.x*sprite.rect.height/sprite.rect.width;
            rt.sizeDelta=new Vector2(rt.sizeDelta.x,height);
            rt.anchoredPosition=new Vector2(rt.anchoredPosition.x,centreY+height*.5f);
            var old=node.GetComponent<UIRoundedPanel>();
            if(old!=null)UnityEngine.Object.DestroyImmediate(old);
            var image=node.GetComponent<Image>()??node.gameObject.AddComponent<Image>();
            image.sprite=sprite;image.color=Color.white;image.material=null;
            image.type=Image.Type.Simple;image.preserveAspect=true;
            var button=node.GetComponent<Button>();image.raycastTarget=button!=null;
            if(button!=null)button.targetGraphic=image;
        }
    }
}
