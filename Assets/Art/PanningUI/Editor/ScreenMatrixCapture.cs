using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PanningUI.Editor
{
    // Explicit, asynchronous Game View readback. Never saves or hides existing scenes.
    public static class ScreenMatrixCapture
    {
        const string Root = "Assets/Art/PanningUI/";
        static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        public static readonly Vector2Int[] Sizes = {
            new Vector2Int(1200,2601), new Vector2Int(1206,2622), new Vector2Int(1080,1920),
            new Vector2Int(1080,2520), new Vector2Int(904,2316), new Vector2Int(1248,1972),
            new Vector2Int(1422,2088), new Vector2Int(1920,2713), new Vector2Int(1848,2448),
            new Vector2Int(1640,2360), new Vector2Int(1600,2560), new Vector2Int(2448,1848) };
        static readonly string[] Samples = { "PopupMailbox", "PopupConfirm", "PopupIdleRewards", "PopupRevolverCompare" };
        static readonly Queue<Job> Jobs = new Queue<Job>();
        sealed class Job { public string prefab, path; public Vector2Int size; }
        static Job current;
        static EditorWindow view;
        static Scene isolated, previous;
        static int oldDisplay, oldSize, phase;
        static double next;
        static string folder;
        static GameObject instance;
        static bool oldRunInBackground;
        public static string Status { get; private set; } = "Idle";
        public static Action<GameObject,Vector2Int,string> Inspect;

        [MenuItem("Tools/Panning UI/Capture All UI Review (Play Mode)")]
        public static void StartReview()
        {
            if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode with only PanningUI preview scenes before capturing.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (!SceneManager.GetSceneAt(i).path.StartsWith(Root + "Scenes/", StringComparison.Ordinal))
                    throw new InvalidOperationException("Review captures require only PanningUI preview scenes.");
            Start(true, false, true);
        }

        public static void StartHomeMatrix() => Start(false, false, false, new[]{"ScreenMainHome"}, Sizes, Root+"Docs/ScreenMatrix/Home");

        public static void StartExport(bool highResolution=false) => Start(true, false, true, null,
            new[]{highResolution ? new Vector2Int(2400,5202) : new Vector2Int(1200,2601)},
            Root+"Docs/Delivery/"+(highResolution ? "PNG_2400" : "PNG_1200"));

        public static void Start(bool baseline, bool regression=false, bool review=false, string[] requested=null, Vector2Int[] resolutions=null, string outputFolder=null)
        {
            if (Status.StartsWith("Running")) throw new InvalidOperationException("Capture already running");
            if (EditorApplication.isCompiling) throw new InvalidOperationException("Editor must not be compiling");
            // Display 8 is reserved only while these isolated captures run.
            foreach (var c in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                if(c.isActiveAndEnabled && c.targetDisplay == 7) throw new InvalidOperationException("Display 8 already in use: " + c.name);
            foreach (var c in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                if(c.isActiveAndEnabled && c.targetDisplay == 7) throw new InvalidOperationException("Display 8 already in use: " + c.name);
            folder = baseline ? (regression ? Root+"Docs/ScreenMatrix/Reference1200" : "Library/ScreenAdapt/Baseline") : Root + "Docs/ScreenMatrix";
            if (review) folder = Root + "Docs/Validation/RemoteReview";
            if (outputFolder != null) folder = outputFolder;
            Directory.CreateDirectory(folder);
            previous = SceneManager.GetActiveScene();
            oldRunInBackground=Application.runInBackground;
            view = EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));
            oldDisplay = (int)view.GetType().GetProperty("targetDisplay",Flags).GetValue(view);
            oldSize = (int)view.GetType().GetProperty("selectedSizeIndex",Flags).GetValue(view);
            Jobs.Clear();
            foreach (var prefab in requested ?? (baseline ? Directory.GetFiles(Root+"Prefabs", review ? "*.prefab" : "Popup*.prefab").Select(Path.GetFileNameWithoutExtension).OrderBy(name=>name).ToArray() : Samples))
                foreach(var size in resolutions ?? (baseline ? Sizes.Take(1).ToArray() : Sizes))
                    Jobs.Enqueue(new Job {prefab=prefab,size=size,path=folder+"/"+prefab+"_"+size.x+"x"+size.y+".png"});
            Status = "Running"; phase=0; next=0;
            if(Application.isPlaying) Application.runInBackground=true;
            EditorApplication.update += Tick;
        }

        public static void SetSize(int width, int height)
        {
            var type=view.GetType(); var asm=type.Assembly;
            var sizesType=asm.GetType("UnityEditor.GameViewSizes");
            var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            object sizes=singleton.GetProperty("instance",BindingFlags.Public|BindingFlags.Static).GetValue(null);
            object group=sizesType.GetMethod("GetGroup").Invoke(sizes,new object[]{0});
            var sizeType=asm.GetType("UnityEditor.GameViewSize");
            int count=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null), index=-1;
            for(int i=0;i<count;i++) {
                var entry=group.GetType().GetMethod("GetGameViewSize").Invoke(group,new object[]{i});
                if((int)sizeType.GetProperty("width",Flags).GetValue(entry)==width && (int)sizeType.GetProperty("height",Flags).GetValue(entry)==height) {index=i;break;}
            }
            if(index<0) {
                var enumType=asm.GetType("UnityEditor.GameViewSizeType");
                var ctor=sizeType.GetConstructor(Flags,null,new[]{enumType,typeof(int),typeof(int),typeof(string)},null);
                var size=ctor.Invoke(new[]{Enum.ToObject(enumType,1),(object)width,height,"PanningUI "+width+"x"+height});
                group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size}); index=count;
                sizesType.GetMethod("SaveToHDD",Flags)?.Invoke(sizes,null);
            }
            type.GetProperty("targetDisplay",Flags).SetValue(view,7);
            type.GetProperty("selectedSizeIndex",Flags).SetValue(view,index);
            type.GetMethod("SetDisplayViewSize",Flags)?.Invoke(view,new object[]{7,new Vector2(width,height)});
            type.GetMethod("UpdateZoomAreaAndParent",Flags)?.Invoke(view,null);
            view.Show(); view.Repaint(); EditorApplication.QueuePlayerLoopUpdate();
        }

        public static void ShowPreviewGameView()
        {
            if(Status.StartsWith("Running"))throw new InvalidOperationException("Wait for the capture to finish");
            view=EditorWindow.GetWindow(typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView"));
            SetSize(1200,2601);
            view.GetType().GetProperty("targetDisplay",Flags).SetValue(view,6);
            view.GetType().GetMethod("SetDisplayViewSize",Flags)?.Invoke(view,new object[]{6,new Vector2(1200,2601)});
            view.Repaint();
        }

        static void Tick()
        {
            if(EditorApplication.timeSinceStartup<next) return;
            try {
                if(phase==0) {
                    if(isolated.IsValid()) {
                        if(Application.isPlaying)foreach(var go in isolated.GetRootGameObjects())UnityEngine.Object.DestroyImmediate(go);
                        else EditorSceneManager.CloseScene(isolated,true);
                    }
                    if(Jobs.Count==0) {Finish("Complete");return;}
                    current=Jobs.Dequeue(); Status="Running "+current.path+" (remaining "+Jobs.Count+")";
                    SetSize(current.size.x,current.size.y);
                    if(!isolated.IsValid())isolated=Application.isPlaying ? SceneManager.CreateScene("ScreenMatrixIsolated") : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
                    SceneManager.SetActiveScene(isolated);
                    instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Root+"Prefabs/"+current.prefab+".prefab"),isolated);
                    instance.GetComponent<Canvas>().targetDisplay=7;
                    var camera=new GameObject("Screen matrix camera",typeof(Camera)).GetComponent<Camera>();
                    camera.targetDisplay=7;camera.cullingMask=0;camera.depth=1000;camera.clearFlags=CameraClearFlags.SolidColor;
                    camera.backgroundColor=new Color(.075f,.065f,.055f);
                    phase=1;next=EditorApplication.timeSinceStartup+.3;
                } else if(phase==1) {
                    Canvas.ForceUpdateCanvases();
                    foreach(var text in instance.GetComponentsInChildren<TMPro.TMP_Text>())text.ForceMeshUpdate();
                    foreach(var graphic in instance.GetComponentsInChildren<Graphic>())graphic.SetAllDirty();
                    Canvas.ForceUpdateCanvases();
                    view.GetType().GetMethod("RepaintImmediately",Flags)?.Invoke(view,null);
                    view.Repaint();EditorApplication.QueuePlayerLoopUpdate();
                    phase=2;next=EditorApplication.timeSinceStartup+.3;
                } else {
                    Inspect?.Invoke(instance,current.size,current.path);
                    if(instance.name.StartsWith("ScreenMainHome",StringComparison.Ordinal)) HomeScreenAudit.Capture(instance,current.size,current.path);
                    else if(!folder.EndsWith("Baseline",StringComparison.Ordinal) && instance.GetComponentInChildren<ColumnLayout>() != null) ScreenAdaptAudit.Capture(instance,current.size,current.path);
                    view.GetType().GetMethod("RepaintImmediately",Flags)?.Invoke(view,null);
                    view.GetType().GetMethod("RenderView",Flags)?.Invoke(view,new object[]{Vector2.zero,false});
                    var rt=view.GetType().GetProperty("targetTexture",Flags)?.GetValue(view) as RenderTexture;
                    if(rt==null)rt=view.GetType().GetField("m_RenderTexture",Flags)?.GetValue(view) as RenderTexture;
                    if(rt==null || rt.width!=current.size.x || rt.height!=current.size.y) throw new Exception("Game View resolution mismatch");
                    var old=RenderTexture.active; var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
                    try {
                        RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);tex.Apply();
                        var sample=tex.GetPixels32();
                        if(!sample.Where((pixel,i)=>i%113==0).Any(pixel=>!pixel.Equals(sample[0])))throw new Exception("Blank frame rejected");
                        if(SystemInfo.graphicsUVStartsAtTop) {
                            var pixels=tex.GetPixels32();var flipped=new Color32[pixels.Length];
                            for(int y=0;y<rt.height;y++) Array.Copy(pixels,y*rt.width,flipped,(rt.height-1-y)*rt.width,rt.width);
                            tex.SetPixels32(flipped);tex.Apply();
                        }
                        File.WriteAllBytes(current.path,tex.EncodeToPNG());
                    } finally {RenderTexture.active=old;UnityEngine.Object.DestroyImmediate(tex);}
                    phase=0;
                }
            } catch(Exception e) {File.WriteAllText(folder+"/failure.txt",e.ToString());Finish("Failed: "+e.Message);Debug.LogException(e);}
        }

        static void Finish(string status)
        {
            EditorApplication.update-=Tick;
            if(isolated.IsValid()) {
                if(Application.isPlaying)SceneManager.UnloadSceneAsync(isolated);
                else EditorSceneManager.CloseScene(isolated,true);
            }
            if(previous.IsValid() && previous.isLoaded)SceneManager.SetActiveScene(previous);
            view.GetType().GetProperty("targetDisplay",Flags).SetValue(view,oldDisplay);
            view.GetType().GetProperty("selectedSizeIndex",Flags).SetValue(view,oldSize);
            view.Repaint(); Status=status; File.WriteAllText(folder+"/status.txt",status);
            if(Application.isPlaying)Application.runInBackground=oldRunInBackground;
        }
    }
}
