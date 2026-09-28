using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PanningUI.Editor
{
    public static class ScreenAdaptBuilder
    {
        const string Root="Assets/Art/PanningUI/";
        public static RectTransform Rect(string name,Transform parent)
        {
            var rt=new GameObject(name,typeof(RectTransform)).GetComponent<RectTransform>();
            rt.SetParent(parent,false);Stretch(rt);return rt;
        }
        static void Stretch(RectTransform rt) {rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;}
        static Image Solid(string name,Transform parent)
        {return Rect(name,parent).gameObject.AddComponent<Image>();}

        /// <summary>Called after assembling source-coordinate widgets and before saving any popup.</summary>
        public static void Apply(GameObject root)
        {
            if(!root.name.StartsWith("Popup",StringComparison.Ordinal))return; // Home belongs to stage B.
            var scaler=root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1200,2601);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;
            var old=root.transform.Find("Design1200x2601") as RectTransform;
            if(old==null) {
                if(root.transform.Find("SafeArea/Column/Middle/PopupBody")==null)throw new Exception("Unknown popup hierarchy: "+root.name);
                var existingColumn=root.transform.Find("SafeArea/Column");
                if(existingColumn.GetComponent<RectMask2D>()==null)existingColumn.gameObject.AddComponent<RectMask2D>();
                Overlay(root);
                return;
            }
            var full=Rect("FullBleed",root.transform);full.SetAsFirstSibling();
            var dim=root.transform.Find("Dim90") as RectTransform;
            if(dim!=null){dim.SetParent(full,false);Stretch(dim);}
            var pillars=Rect("PillarFill",full).gameObject.AddComponent<PillarFill>();
            pillars.Configure(Solid("Left",pillars.transform),Solid("Right",pillars.transform),Solid("LeftHairline",pillars.transform),Solid("RightHairline",pillars.transform));
            var safe=Rect("SafeArea",root.transform);safe.gameObject.AddComponent<SafeAreaFitter>();
            var column=Rect("Column",safe);
            column.gameObject.AddComponent<RectMask2D>();
            var layout=column.gameObject.AddComponent<ColumnLayout>();
            var top=Rect("Top",column);top.anchorMin=new Vector2(0,1);top.anchorMax=Vector2.one;top.sizeDelta=Vector2.zero;
            var middle=Rect("Middle",column);
            var bottom=Rect("Bottom",column);bottom.anchorMax=new Vector2(1,0);bottom.sizeDelta=Vector2.zero;
            old.name="PopupBody";old.SetParent(middle,false);
            old.anchorMin=old.anchorMax=old.pivot=new Vector2(.5f,.5f);
            old.sizeDelta=new Vector2(1200,2601);old.anchoredPosition=Vector2.zero;
            var close=old.Find("Close") as RectTransform;
            if(close!=null)PlaceClose(close,bottom);
            layout.Configure(pillars);
            Overlay(root);
        }

        static void Overlay(GameObject root)
        {
            var body=root.transform.Find("SafeArea/Column/Middle/PopupBody");
            var layer=body.Find("ReceivedLayer") ?? body.Find("WarningLayer");
            if(layer==null || layer.GetComponent<FullBleedOverlay>()!=null)return;
            var source=layer.GetComponent<Image>();
            var dim=Solid(layer.name+"Dim",root.transform.Find("FullBleed"));
            dim.color=source.color;dim.raycastTarget=true;
            var dimCanvas=dim.gameObject.AddComponent<Canvas>();dimCanvas.overrideSorting=true;
            dimCanvas.sortingOrder=root.GetComponent<Canvas>().sortingOrder+1;
            dim.gameObject.AddComponent<GraphicRaycaster>();
            source.color=Color.clear;
            var layerCanvas=layer.gameObject.AddComponent<Canvas>();layerCanvas.overrideSorting=true;
            layerCanvas.sortingOrder=root.GetComponent<Canvas>().sortingOrder+2;
            layer.gameObject.AddComponent<GraphicRaycaster>();
            layer.gameObject.AddComponent<FullBleedOverlay>().Configure(dim.gameObject);
        }

        public static void PlaceClose(RectTransform close,Transform bottom)
        {
            close.SetParent(bottom,false);
            close.anchorMin=close.anchorMax=Vector2.zero;close.pivot=new Vector2(0,1);
            // Reference top y=2424 -> bottom-relative top 2601-2424=177. Sprite rect unchanged.
            close.anchoredPosition=new Vector2(534,177);close.sizeDelta=new Vector2(132,138);
        }

        [MenuItem("Tools/Panning UI/Build Screen Adapt Previews")]
        public static void BuildAll()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first");
            foreach(var path in Directory.GetFiles(Root+"Prefabs","Popup*.prefab")) {
                var assetPath=path.Replace('\\','/');
                var root=PrefabUtility.LoadPrefabContents(assetPath);
                try {Apply(root);PrefabUtility.SaveAsPrefabAsset(root,assetPath);}
                finally {PrefabUtility.UnloadPrefabContents(root);}
            }
            var prior=SceneManager.GetActiveScene();
            foreach(var path in Directory.GetFiles(Root+"Scenes","*Preview.unity")) {
                var assetPath=path.Replace('\\','/');
                if(assetPath.EndsWith("MainHomePreview.unity",StringComparison.Ordinal))continue;
                var scene=SceneManager.GetSceneByPath(assetPath);bool loaded=scene.IsValid()&&scene.isLoaded;
                if(loaded&&scene.isDirty)throw new InvalidOperationException("Unsaved preview preserved: "+assetPath);
                if(!loaded)scene=EditorSceneManager.OpenScene(assetPath,OpenSceneMode.Additive);
                EditorSceneManager.SaveScene(scene,assetPath);
                if(!loaded)EditorSceneManager.CloseScene(scene,true);
            }
            if(prior.IsValid()&&prior.isLoaded)SceneManager.SetActiveScene(prior);
        }
    }
}
