using UnityEngine;
using UnityEngine.UI;

namespace PanningUI
{
    // Simple geometric background; the label is a separate TMP component.
    [RequireComponent(typeof(CanvasRenderer))]
    [AddComponentMenu("UI/Circle Graphic")]
    public sealed class UICircleGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r=GetPixelAdjustedRect();var center=r.center;
            vh.AddVert(center,color,Vector2.one*.5f);
            const int segments=48;
            for(int i=0;i<=segments;i++)
            {
                float a=i*Mathf.PI*2/segments;
                var unit=new Vector2(Mathf.Cos(a),Mathf.Sin(a));
                vh.AddVert(center+Vector2.Scale(unit,r.size)*.5f,color,(unit+Vector2.one)*.5f);
                if(i>0)vh.AddTriangle(0,i,i+1);
            }
        }
    }
}
