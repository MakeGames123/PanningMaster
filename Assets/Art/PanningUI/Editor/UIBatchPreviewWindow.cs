using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PanningUI.Editor
{
    // Explicit menu actions only: no reload hook, polling, auto-build or auto-capture.
    public sealed class UIBatchPreviewWindow : EditorWindow
    {
        const string Root = "Assets/Art/PanningUI/";
        [Serializable] public sealed class Entry { public string key, name, prefab, scene; }
        [Serializable] public sealed class Catalog { public Entry[] screens; }
        readonly Dictionary<Behaviour, bool> suspended = new Dictionary<Behaviour, bool>();
        Catalog catalog;
        int selected;
        Vector2 scroll;
        string result;

        [MenuItem("Tools/Panning UI/Batch Preview")]
        [MenuItem("Tools/Panning UI/00 Open UI Review")]
        public static void OpenWindow() => GetWindow<UIBatchPreviewWindow>("UI Preview");

        void OnEnable() => LoadCatalog();
        void LoadCatalog()
        {
            var saved=JsonUtility.FromJson<Catalog>(File.ReadAllText(Root+"Docs/ui-batch-manifest.json"));
            catalog=new Catalog {screens=AssetDatabase.FindAssets("t:Prefab",new[]{Root+"Prefabs"})
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p=>p).Select(p=> {
                    string name=Path.GetFileNameWithoutExtension(p);
                    return saved.screens.FirstOrDefault(e=>e.name==name) ?? new Entry {
                        key=name,name=name,prefab=p.Substring(Root.Length),
                        scene="Scenes/"+(name.StartsWith("Popup",StringComparison.Ordinal)?name.Substring(5):name.Substring(6))+"Preview.unity"};
                }).ToArray()};
            selected=Mathf.Clamp(selected,0,catalog.screens.Length-1);
        }

        void OnGUI()
        {
            EditorGUILayout.LabelField("UI review", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("Display 7 · 1200 × 2601 · 24 popups + partial home. Clean UI previews close when switching. Unsaved scenes and developer scenes are preserved. Game logic is not connected.", MessageType.Info);
            if (GUILayout.Button("Load preview list"))
                LoadCatalog();
            if (catalog?.screens == null) return;
            scroll = EditorGUILayout.BeginScrollView(scroll);
            for (int i = 0; i < catalog.screens.Length; i++)
            {
                var e = catalog.screens[i];
                if (GUILayout.Toggle(selected == i, e.name+(e.key=="home"?" (4 components applied; other areas provisional)":""), "Button")) selected = i;
            }
            EditorGUILayout.EndScrollView();
            if (GUILayout.Button("Open selected preview")) Run(() => Show(catalog.screens[selected]));
            if (GUILayout.Button("Validate selected prefab")) Run(() => Validate(catalog.screens[selected]));
            using (new EditorGUI.DisabledScope(!EditorApplication.isPlaying))
                if (GUILayout.Button("Capture selected Game view")) Run(() => Capture(catalog.screens[selected]));
            if (GUILayout.Button("Restore previously visible previews")) Restore();
            if (!string.IsNullOrEmpty(result)) EditorGUILayout.HelpBox(result, MessageType.None);
        }

        void Run(Action action)
        {
            try { action(); }
            catch (Exception ex) { result = ex.Message; Debug.LogException(ex); }
        }

        void Suspend(Behaviour component)
        {
            if (!suspended.ContainsKey(component)) suspended.Add(component, component.enabled);
            component.enabled = false;
        }

        void Show(Entry entry)
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode first.");
            string path = Root + entry.scene;
            var target = SceneManager.GetSceneByPath(path);
            if (!target.IsValid() || !target.isLoaded) target = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
            // Keep dirty scenes open in memory. Both cameras and canvases must be hidden:
            // another base camera on the same display can clear an overlay preview.
            for (int i = SceneManager.sceneCount-1; i >= 0; i--)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded || scene.path == path || !scene.path.StartsWith(Root + "Scenes/", StringComparison.Ordinal)) continue;
                if(!scene.isDirty) {EditorSceneManager.CloseScene(scene,true);continue;}
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
                        if (canvas.targetDisplay == 6) Suspend(canvas);
                    foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
                        if (camera.targetDisplay == 6) Suspend(camera);
                }
            }
            foreach (var root in target.GetRootGameObjects())
            {
                foreach (var canvas in root.GetComponentsInChildren<Canvas>(true))
                    if (suspended.TryGetValue(canvas, out bool value)) { canvas.enabled = value; suspended.Remove(canvas); }
                foreach (var camera in root.GetComponentsInChildren<Camera>(true))
                    if (suspended.TryGetValue(camera, out bool value)) { camera.enabled = value; suspended.Remove(camera); }
            }
            SceneManager.SetActiveScene(target);
            Canvas.ForceUpdateCanvases();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(Root + entry.prefab);
            ScreenMatrixCapture.ShowPreviewGameView();
            result = entry.name + " opened. Choose Game Display 7. No scene saved.";
        }

        void Validate(Entry entry)
        {
            var root = PrefabUtility.LoadPrefabContents(Root + entry.prefab);
            try
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root) != 0) throw new Exception("Missing script on root");
                var scaler = root.GetComponent<CanvasScaler>();
                if (scaler == null || scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize || scaler.referenceResolution != new Vector2(1200,2601) || scaler.screenMatchMode != CanvasScaler.ScreenMatchMode.Expand) throw new Exception("Canvas configuration mismatch");
                foreach (Transform node in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject) != 0) throw new Exception("Missing script: " + node.name);
                var texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var text in texts)
                {
                    if (text.font == null || text.font == TMP_Settings.defaultFontAsset) throw new Exception("Explicit popup font missing: " + text.name);
                    if (text.fontStyle.HasFlag(FontStyles.Bold)) throw new Exception("Synthetic bold: " + text.name);
                    foreach (char c in text.text)
                        if (!char.IsWhiteSpace(c) && !text.font.HasCharacter(c, true, true)) throw new Exception("Missing glyph in " + text.name + ": " + c);
                    if (text.fontSharedMaterial == null || text.fontSharedMaterial.mainTexture != text.font.atlasTextures[0]) throw new Exception("Material atlas mismatch: " + text.name);
                }
                foreach (Image image in root.GetComponentsInChildren<Image>(true))
                    if (image.name.StartsWith("ico_", StringComparison.Ordinal) && (image.sprite != null || image.color.a != 0)) throw new Exception("Icon is not an empty Image: " + image.name);
                result = entry.name + ": references, Canvas and " + texts.Length + " TMP components passed. Visual review still required.";
                File.WriteAllText(Root + "Docs/Validation/" + entry.key + "-editor-check.txt", result);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        void Capture(Entry entry)
        {
            if(!EditorApplication.isPlaying)throw new InvalidOperationException("Enter Play Mode with only UI previews first.");
            ScreenMatrixCapture.Start(true,false,true,new[]{entry.name},new[]{new Vector2Int(1200,2601)},Root+"Docs/Validation/PreviewCaptures");
            result = "Game capture requested for " + entry.name;
        }

        void Restore()
        {
            foreach (var item in suspended) if (item.Key != null) item.Key.enabled = item.Value;
            suspended.Clear();
            result = "Previous component visibility restored; no scene saved.";
        }

        void OnDisable() => Restore();
    }
}
