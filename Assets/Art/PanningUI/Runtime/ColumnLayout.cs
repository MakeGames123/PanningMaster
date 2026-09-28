using UnityEngine;
using UnityEngine.UI;

namespace PanningUI
{
    [ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(RectTransform)), DefaultExecutionOrder(200)]
    public sealed class ColumnLayout : MonoBehaviour, ICanvasRaycastFilter
    {
        public const float Width=1200f;
        [SerializeField] PillarFill pillarFill;
        [SerializeField] RectTransform popupBody;
        RectTransform rect, canvasRect;
        RectMask2D clip;
        Rect lastCanvas, lastColumn;
        Vector3 lastPosition;
        bool dirty=true;

        public void Configure(PillarFill fill) {pillarFill=fill; Cache(); Refresh();}
        void OnEnable() {Cache();ScreenMetrics.Changed+=Changed;Refresh();}
        void OnDisable() {ScreenMetrics.Changed-=Changed;}
        void Changed(ScreenMetricsSnapshot _) {dirty=true;}
        void OnRectTransformDimensionsChange() {dirty=true;}
        void Cache()
        {
            rect=(RectTransform)transform;
            clip=GetComponent<RectMask2D>();
            if(popupBody==null)popupBody=transform.Find("Middle/PopupBody") as RectTransform;
            var canvas=GetComponentInParent<Canvas>();
            canvasRect=canvas==null?null:canvas.rootCanvas.transform as RectTransform;
        }
        void LateUpdate()
        {
            if(canvasRect==null)Cache();
            if(canvasRect!=null && (dirty || lastCanvas!=canvasRect.rect || lastColumn!=rect.rect || lastPosition!=rect.position)) Refresh();
        }
        public void Refresh()
        {
            if(rect==null)Cache();
            rect.anchorMin=new Vector2(.5f,0);rect.anchorMax=new Vector2(.5f,1);
            rect.pivot=new Vector2(.5f,.5f);rect.sizeDelta=new Vector2(Width,0);rect.anchoredPosition=Vector2.zero;
            if(canvasRect==null)return;
            // Preserve the source-coordinate design center on tall screens. On devices with
            // top/bottom insets, clamp the body above the fixed footer instead of overlapping X.
            if(popupBody!=null)popupBody.anchoredPosition=new Vector2(0,Mathf.Max(0,(2601-rect.rect.height)*.5f));
            // The framebuffer already clips narrow phones. Avoid a second soft clipping edge
            // there, which changes boundary pixels at the approved reference resolution.
            if(clip!=null)clip.enabled=canvasRect.rect.width>Width+.01f;
            if(pillarFill!=null)pillarFill.Refresh(rect,canvasRect);
            lastCanvas=canvasRect.rect;lastColumn=rect.rect;lastPosition=rect.position;dirty=false;
        }
        public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
        {return RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform,screenPoint,eventCamera);}
    }
}
