using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PanningUI.Editor
{
    public static class HomeScreenAdaptBuilder
    {
        const string Root = "Assets/Art/PanningUI/";

        // Split in the source's empty band between the HUD (ends at 324) and
        // cylinder controls (begin at 663). Widget coordinates remain unchanged.
        public static void Apply(GameObject root)
        {
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.referenceResolution = new Vector2(1200,2601);
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            var source = root.transform.Find("Design1200x2601") as RectTransform;
            if (source == null) {
                if (root.transform.Find("SafeArea/Column/Bottom/BottomContent") == null)
                    throw new InvalidOperationException("Unknown home hierarchy");
                return;
            }
            var full = ScreenAdaptBuilder.Rect("FullBleed",root.transform); full.SetAsFirstSibling();
            var dim = root.transform.Find("Dim90") as RectTransform;
            if(dim != null) {dim.SetParent(full,false);Stretch(dim);} // Retain disabled source dim.
            var safe = ScreenAdaptBuilder.Rect("SafeArea",root.transform);
            safe.gameObject.AddComponent<SafeAreaFitter>();
            var column = ScreenAdaptBuilder.Rect("Column",safe);
            column.gameObject.AddComponent<RectMask2D>();
            var layout = column.gameObject.AddComponent<ColumnLayout>();
            var top = ScreenAdaptBuilder.Rect("Top",column);
            top.anchorMin = new Vector2(0,1); top.sizeDelta=Vector2.zero;
            var middle = ScreenAdaptBuilder.Rect("Middle",column);
            middle.offsetMin = new Vector2(0,1938); middle.offsetMax = new Vector2(0,-324);
            var bottom = ScreenAdaptBuilder.Rect("Bottom",column);
            bottom.anchorMax = new Vector2(1,0); bottom.sizeDelta=Vector2.zero;
            var hud = ScreenAdaptBuilder.Rect("TopContent",top);
            hud.anchorMin=hud.anchorMax=hud.pivot=new Vector2(.5f,1);
            hud.sizeDelta=new Vector2(1200,2601);hud.anchoredPosition=Vector2.zero;
            var widgets=source.Cast<Transform>().ToArray();
            source.name="BottomContent";source.SetParent(bottom,false);
            source.anchorMin=source.anchorMax=source.pivot=new Vector2(.5f,0);
            source.sizeDelta=new Vector2(1200,2601);source.anchoredPosition=Vector2.zero;
            foreach(var widget in widgets) {
                var rt=(RectTransform)widget;
                if(widget.name=="BattleArtSlot") {rt.SetParent(full,false);Stretch(rt);}
                else if(widget.name=="LowerBackground" || widget.name=="HudShade") {
                    bool upper=widget.name=="HudShade";
                    rt.SetParent(full,false);
                    rt.anchorMin=new Vector2(0,upper?1:0);rt.anchorMax=new Vector2(1,upper?1:0);
                    rt.pivot=new Vector2(0,upper?1:0);rt.anchoredPosition=Vector2.zero;
                    rt.sizeDelta=new Vector2(0,upper?222:1905);
                }
                else if(-rt.anchoredPosition.y<400) rt.SetParent(hud,false);
            }
            var pillars=ScreenAdaptBuilder.Rect("PillarFill",full).gameObject.AddComponent<PillarFill>();
            pillars.Configure(Solid("Left",pillars.transform),Solid("Right",pillars.transform),Solid("LeftHairline",pillars.transform),Solid("RightHairline",pillars.transform));
            layout.Configure(pillars);
        }
        static void Stretch(RectTransform rt) {rt.anchorMin=Vector2.zero;rt.anchorMax=Vector2.one;rt.offsetMin=rt.offsetMax=Vector2.zero;}
        static Image Solid(string name,Transform parent) => ScreenAdaptBuilder.Rect(name,parent).gameObject.AddComponent<Image>();

        [MenuItem("Tools/Panning UI/Build Adaptive Home Preview")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode first");
            const string path=Root+"Prefabs/ScreenMainHome.prefab";
            const string scenePath=Root+"Scenes/MainHomePreview.unity";
            var scene=SceneManager.GetSceneByPath(scenePath);
            bool loaded=scene.IsValid()&&scene.isLoaded;
            if(loaded&&scene.isDirty)throw new InvalidOperationException("Unsaved home preview preserved");
            var root=PrefabUtility.LoadPrefabContents(path);
            try {Apply(root);HomeNavigationBuilder.Apply(root);HomeProvidedAssetsBuilder.Apply(root);PrefabUtility.SaveAsPrefabAsset(root,path);}
            finally {PrefabUtility.UnloadPrefabContents(root);}
            var previous=SceneManager.GetActiveScene();
            if(!loaded)scene=EditorSceneManager.OpenScene(scenePath,OpenSceneMode.Additive);
            EditorSceneManager.SaveScene(scene,scenePath);
            if(!loaded)EditorSceneManager.CloseScene(scene,true);
            if(previous.IsValid()&&previous.isLoaded)SceneManager.SetActiveScene(previous);
        }
    }
}
