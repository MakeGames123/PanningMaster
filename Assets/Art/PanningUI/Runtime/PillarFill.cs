using UnityEngine;
using UnityEngine.UI;

namespace PanningUI
{
    /// <summary>FullBleed decoration only; four solid, non-interactive Images, no texture assets.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(RectTransform))]
    public sealed class PillarFill : MonoBehaviour
    {
        [SerializeField] Image left, right, leftLine, rightLine;
        public void Configure(Image l,Image r,Image ll,Image rl)
        {
            left=l;right=r;leftLine=ll;rightLine=rl;
            left.color=right.color=new Color32(10,9,8,255);
            leftLine.color=rightLine.color=new Color(214f/255,172f/255,96f/255,.55f);
            left.raycastTarget=right.raycastTarget=leftLine.raycastTarget=rightLine.raycastTarget=false;
        }
        public void Refresh(RectTransform column, RectTransform canvas)
        {
            bool visible=canvas.rect.width>ColumnLayout.Width+.01f;
            if(gameObject.activeSelf!=visible)gameObject.SetActive(visible);
            if(!visible || left==null || right==null || leftLine==null || rightLine==null)return;
            var parent=(RectTransform)transform;
            float x0=parent.InverseTransformPoint(column.TransformPoint(new Vector3(column.rect.xMin,0,0))).x-parent.rect.xMin;
            float x1=parent.InverseTransformPoint(column.TransformPoint(new Vector3(column.rect.xMax,0,0))).x-parent.rect.xMin;
            float width=parent.rect.width;
            Set(left.rectTransform,0,Mathf.Max(0,x0)); Set(right.rectTransform,x1,Mathf.Max(0,width-x1));
            // Lines sit outside the 1200-unit content column.
            Set(leftLine.rectTransform,x0-3,3); Set(rightLine.rectTransform,x1,3);
        }
        static void Set(RectTransform rt,float x,float width)
        {
            rt.anchorMin=Vector2.zero;rt.anchorMax=new Vector2(0,1);rt.pivot=new Vector2(0,.5f);
            rt.anchoredPosition=new Vector2(x,0);rt.sizeDelta=new Vector2(width,0);
        }
    }
}
