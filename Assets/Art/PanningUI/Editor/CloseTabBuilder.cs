using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PanningUI.Editor
{
    // Re-serialize only the confirmed close control, preserving every other UI component.
    public static class CloseTabBuilder
    {
        const string Root = "Assets/Art/PanningUI/";
        const string Report = Root + "Docs/Validation/close-tab-editor-validation.txt";
        const string PreviewPath = Root + "Scenes/CloseTabValidationPreview.unity";
        static readonly Dictionary<Behaviour,bool> Hidden = new Dictionary<Behaviour,bool>();

        [MenuItem("Tools/Panning UI/Build All Close Tab Previews")]
        public static void BuildAll()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "Sprites/Navigation/ui_nav_close_t01a.png");
            if (sprite == null || sprite.rect.size != new Vector2(132,138) || sprite.border != Vector4.zero)
                throw new Exception("T01-a sprite must be the original 132 x 138 Simple image.");
            var messages = new List<string>();
            foreach (string path in Directory.GetFiles(Root + "Prefabs", "*.prefab"))
            {
                string assetPath = path.Replace('\\','/');
                var contents = PrefabUtility.LoadPrefabContents(assetPath);
                try
                {
                    var close = contents.GetComponentsInChildren<Image>(true).SingleOrDefault(i => i.name == "Close");
                    if (close == null && contents.name == "PopupConfirm")
                    {
                        var design = contents.transform.Find("SafeArea/Column/Middle/PopupBody") ?? contents.transform.Find("Design1200x2601");
                        close = PanningMailBuilder.SpriteImage("Close",design,"ui_nav_close_t01a",new float[]{178,808,44,46},0,true);
                        UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(close.GetComponent<Button>().onClick,contents.SetActive,false);
                    }
                    if (close == null) continue;
                    close.sprite = sprite; close.type = Image.Type.Simple;
                    var rect = close.rectTransform;
                    rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0,1);
                    rect.anchoredPosition = new Vector2(534,-2424); rect.sizeDelta = new Vector2(132,138);
                    var bottom = contents.transform.Find("SafeArea/Column/Bottom");
                    if(bottom != null) ScreenAdaptBuilder.PlaceClose(rect,bottom);
                    close.raycastTarget = true;
                    var button = close.GetComponent<Button>();
                    if (button == null) throw new Exception("Missing Close Button: " + assetPath);
                    button.targetGraphic = close;
                    PrefabUtility.SaveAsPrefabAsset(contents,assetPath);
                    messages.Add("PASS close sprite / native size / position / button: " + assetPath);
                }
                finally { PrefabUtility.UnloadPrefabContents(contents); }
            }
            // Prefab-linked scenes inherit the new image. Preserve dirty scene instances in memory.
            var prior = SceneManager.GetActiveScene();
            foreach (string path in Directory.GetFiles(Root + "Scenes", "*Preview.unity"))
            {
                string assetPath = path.Replace('\\','/');
                if (assetPath == PreviewPath) continue;
                var scene = SceneManager.GetSceneByPath(assetPath);
                bool wasLoaded = scene.IsValid() && scene.isLoaded;
                if (wasLoaded && scene.isDirty)
                {
                    messages.Add("PRESERVED unsaved scene; inherits prefab update: " + assetPath); continue;
                }
                if (!wasLoaded) scene = EditorSceneManager.OpenScene(assetPath,OpenSceneMode.Additive);
                EditorSceneManager.SaveScene(scene,assetPath);
                if (!wasLoaded) EditorSceneManager.CloseScene(scene,true);
                messages.Add("PASS re-serialized linked preview: " + assetPath);
            }
            if (prior.IsValid() && prior.isLoaded) SceneManager.SetActiveScene(prior);
            File.WriteAllLines(Report,messages);
            Debug.Log("[PanningUI] T01-a close-tab update complete. " + Report);
        }

        static void ShowCapture(string prefabName,string key)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            Restore();
            var priorPreview = SceneManager.GetSceneByPath(PreviewPath);
            if (priorPreview.IsValid() && priorPreview.isLoaded)
            {
                if (priorPreview.isDirty) throw new Exception("Capture preview has unsaved changes; preserve them before replacing it.");
                EditorSceneManager.CloseScene(priorPreview,true);
            }
            for (int i=0;i<SceneManager.sceneCount;i++)
            {
                var scene=SceneManager.GetSceneAt(i);
                if (!scene.isLoaded || !scene.path.StartsWith(Root+"Scenes/",StringComparison.Ordinal)) continue;
                foreach (var root in scene.GetRootGameObjects())
                {
                    foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
                        if (canvas.targetDisplay==6) { Hidden[canvas]=canvas.enabled;canvas.enabled=false; }
                    foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                        if (camera.targetDisplay==6) { Hidden[camera]=camera.enabled;camera.enabled=false; }
                }
            }
            var preview=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            var asset=AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/"+prefabName+".prefab");
            var instance=(GameObject)PrefabUtility.InstantiatePrefab(asset,preview);
            instance.GetComponent<Canvas>().targetDisplay=6;
            var cameraObject=new GameObject("Close Tab Preview Camera",typeof(Camera));
            SceneManager.MoveGameObjectToScene(cameraObject,preview);
            var cam=cameraObject.GetComponent<Camera>();cam.targetDisplay=6;cam.cullingMask=0;
            cam.clearFlags=CameraClearFlags.SolidColor;cam.backgroundColor=new Color(.075f,.065f,.055f);
            EditorSceneManager.SaveScene(preview,PreviewPath);SceneManager.SetActiveScene(preview);
            PanningMailBuilder.PrepareCaptureFor(key,"close-tab-captures.txt",6);
        }

        [MenuItem("Tools/Panning UI/Capture Close Tab Mail Game View")]
        public static void Mail() => ShowCapture("PopupMailbox","close-t01a-mail");
        [MenuItem("Tools/Panning UI/Capture Close Tab Confirm Game View")]
        public static void Confirm() => ShowCapture("PopupConfirm","close-t01a-confirm");
        [MenuItem("Tools/Panning UI/Capture Close Tab Idle Game View")]
        public static void Idle() => ShowCapture("PopupIdleRewards","close-t01a-off");
        [MenuItem("Tools/Panning UI/Restore Previews After Close Tab Capture")]
        public static void Restore()
        {
            foreach (var item in Hidden) if(item.Key!=null)item.Key.enabled=item.Value;
            Hidden.Clear();
        }
    }
}
