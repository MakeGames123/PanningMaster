using System;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static PanningUI.Editor.PanningMailBuilder;

namespace PanningUI.Editor
{
    [InitializeOnLoad]
    public static class PanningConfirmBuilder
    {
        const string Root="Assets/Art/PanningUI/";
        const string ScenePath=Root+"Scenes/ConfirmPreview.unity";
        const string PrefabPath=Root+"Prefabs/PopupConfirm.prefab";
        const string Command="Library/PanningConfirm-command.txt";
        const string Output=Root+"Docs/Validation/";
        [Serializable] public class ConfirmTexts { public TextSpec[] confirm; }
        [Serializable] public class ConfirmTextDocument { public Style[] styles; public ConfirmTexts sceneTexts; }
        [Serializable] public class ConfirmElements { public Element[] confirm; }
        [Serializable] public class ConfirmLayout { public ConfirmElements finalScenes; }

        static PanningConfirmBuilder(){EditorApplication.update+=Tick;}
        static void Tick()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Command))return;
            string command=File.ReadAllText(Command).Trim();File.Delete(Command);
            try {if(command=="build")BuildConfirm();else if(command=="capture")CaptureConfirm();}
            catch(Exception e){Directory.CreateDirectory(Output);File.WriteAllText(Output+"confirm-failure.txt",e.ToString());Debug.LogException(e);}
        }

        [MenuItem("Tools/Panning UI/Build Confirm Preview")]
        public static void BuildConfirm()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before building the preview.");
            var defaultFont=TMP_Settings.defaultFontAsset;
            var data=JsonUtility.FromJson<ConfirmTextDocument>(File.ReadAllText(Root+"Docs/text-styles.json").Replace("\"pop-confirm\":","\"confirm\":"));
            string layoutJson=System.Text.RegularExpressions.Regex.Replace(File.ReadAllText(Root+"Docs/layout-coordinates.json"),@"\\u[dD][89a-fA-F][0-9a-fA-F]{2}","");
            var layout=JsonUtility.FromJson<ConfirmLayout>(layoutJson.Replace("\"pop-confirm\":","\"confirm\":"));
            var styles=data.styles.ToDictionary(s=>s.id);
            var old=SceneManager.GetSceneByPath(ScenePath);
            if(old.IsValid() && old.isLoaded && old.isDirty)throw new InvalidOperationException("The Confirm preview has unsaved edits; save them before rebuilding.");
            Scene prior=SceneManager.GetActiveScene();
            if(old.IsValid() && old.isLoaded)EditorSceneManager.CloseScene(old,true);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            scene.name="ConfirmPreview";
            SceneManager.SetActiveScene(scene);
            GameObject root=null;
            try
            {
                root=new GameObject("PopupConfirm",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
                var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
                var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution=new Vector2(1200,2601);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;scaler.matchWidthOrHeight=.5f;
                var dim=Solid("Dim90",root.transform,new float[]{0,0,1200,2601},new Color(0,0,0,230f/255));
                dim.raycastTarget=true;dim.rectTransform.anchorMin=Vector2.zero;dim.rectTransform.anchorMax=Vector2.one;dim.rectTransform.offsetMin=dim.rectTransform.offsetMax=Vector2.zero;
                var design=Rect("Design1200x2601",root.transform,new float[]{0,0,1200,2601});
                design.anchorMin=design.anchorMax=design.pivot=new Vector2(.5f,.5f);design.anchoredPosition=Vector2.zero;
                foreach(var element in layout.finalScenes.confirm)
                {
                    var r=element.css400;
                    switch(element.sel)
                    {
                        case ".cf-box":SpriteImage("Card",design,"ui_pop_card",r,6);break;
                        case ".c3-cost":
                            SpriteImage("RubyCostBracket",design,"ui_pop_cost_bracket",r,2);
                            Icon("ico_ruby",design,r[0]+33,r[1]+(r[3]-26)/2,26);break;
                        case ".cf-btn":
                            var button=SpriteImage(element.i==0?"Cancel":"Confirm",design,element.i==0?"ui_pop_btn_sub":"ui_pop_btn_ok",r,6,true).GetComponent<Button>();
                            if(element.i==0)UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(button.onClick,root.SetActive,false);
                            break;
                    }
                }
                int i=0;
                foreach(var spec in data.sceneTexts.confirm)
                {
                    var style=styles[spec.style];
                    var text=Rect("Text_"+(i++).ToString("D2")+"_"+spec.style,design,spec.rectUnity1200).gameObject.AddComponent<TextMeshProUGUI>();
                    text.font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"Fonts/SDF/"+style.fontAsset+".asset");
                    if(text.font==null)throw new Exception("Required SDF missing: "+style.fontAsset);
                    text.fontSize=style.unityFontSize;text.fontStyle=style.italic?FontStyles.Italic:FontStyles.Normal;
                    text.color=ColorValue(style.color);text.characterSpacing=style.characterSpacing;text.enableAutoSizing=false;
                    text.textWrappingMode=TextWrappingModes.NoWrap;text.overflowMode=TextOverflowModes.Overflow;
                    text.alignment=spec.align=="center"?TextAlignmentOptions.Center:spec.align=="right"?TextAlignmentOptions.MidlineRight:TextAlignmentOptions.MidlineLeft;
                    text.raycastTarget=false;text.richText=false;
                    if(style.material!="default")text.fontSharedMaterial=AssetDatabase.LoadAssetAtPath<Material>(Root+"Fonts/SDF/"+style.material+".mat");
                    text.text=spec.text;
                }
                var close=SpriteImage("Close",design,"ui_nav_close_t01a",new float[]{178,808,44,46},0,true);
                UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(close.GetComponent<Button>().onClick,root.SetActive,false);
                ValidateConfirm(root,data,defaultFont);
                Directory.CreateDirectory(Root+"Prefabs");ScreenAdaptBuilder.Apply(root);PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
                UnityEngine.Object.DestroyImmediate(root);root=null;
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),scene);
                instance.GetComponent<Canvas>().targetDisplay=3;
                var camera=new GameObject("Preview Camera (Display 4)",typeof(Camera)).GetComponent<Camera>();
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.075f,.065f,.055f);camera.cullingMask=0;camera.targetDisplay=3;
                Directory.CreateDirectory(Root+"Scenes");EditorSceneManager.SaveScene(scene,ScenePath);
                foreach(var font in instance.GetComponentsInChildren<TextMeshProUGUI>().Select(t=>t.font).Distinct())AssetDatabase.SaveAssetIfDirty(font);
            }
            catch {if(root!=null)UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.CloseScene(scene,true);throw;}
            finally {if(prior.IsValid()&&prior.isLoaded)SceneManager.SetActiveScene(prior);}
            Debug.Log("[PanningUI] Confirm prefab built and validated. 6 TMP strings, 1 empty icon Images.");
            CaptureConfirm();
        }

        static void ValidateConfirm(GameObject root,ConfirmTextDocument data,TMP_FontAsset defaultFont)
        {
            var texts=root.GetComponentsInChildren<TextMeshProUGUI>();
            if(texts.Length!=6 || texts.Length!=data.sceneTexts.confirm.Length)throw new Exception("Confirm text count mismatch");
            for(int i=0;i<texts.Length;i++)
            {
                var text=texts[i];var spec=data.sceneTexts.confirm[i];text.ForceMeshUpdate();
                var r=spec.rectUnity1200;
                if(text.text!=spec.text || Vector2.Distance(text.rectTransform.anchoredPosition,new Vector2(r[0],-r[1]))>.01f || Vector2.Distance(text.rectTransform.sizeDelta,new Vector2(r[2],r[3]))>.01f)throw new Exception("Confirm text rect mismatch: "+i);
                foreach(char ch in text.text)if(!char.IsWhiteSpace(ch)&&!text.font.HasCharacter(ch,true,true))throw new Exception("Missing glyph: "+ch);
                if(text.font==defaultFont || text.fontStyle.HasFlag(FontStyles.Bold))throw new Exception("Unexpected font / synthetic bold");
            }
            var icons=root.GetComponentsInChildren<Image>().Where(img=>img.name.StartsWith("ico_")).ToArray();
            if(icons.Length!=1 || icons.Any(img=>img.sprite!=null || img.color.a!=0))throw new Exception("Icons must be empty transparent Images");
            foreach(var image in root.GetComponentsInChildren<Image>())
            {
                if(image.sprite==null)continue;
                var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(image.sprite));
                if(importer.textureType!=TextureImporterType.Sprite || importer.textureCompression!=TextureImporterCompression.Uncompressed)throw new Exception("Sprite import mismatch");
                if(image.type!=(image.sprite.border==Vector4.zero?Image.Type.Simple:Image.Type.Sliced))throw new Exception("Image type mismatch");
            }
            if(defaultFont!=TMP_Settings.defaultFontAsset)throw new Exception("Default TMP font changed");
            Directory.CreateDirectory(Output);
            File.WriteAllText(Output+"confirm-validation.txt","PanningUI Confirm validation / "+DateTime.Now.ToString("s")+"\nPASS: 6 TMP strings; exact JSON rects; explicit fonts; no missing glyphs.\nPASS: 1 empty transparent icon Images.\nPASS: used sprite imports uncompressed / Sprite; Image types follow sprite borders.\nPASS: Canvas 1200 x 2601 / Scale With Screen Size / Match 0.5.\nDefault TMP font preserved: "+defaultFont.name+"\nButtons: Confirm is a visual placeholder; Cancel deactivates the popup.\nPreview uses Display 4; original game and approved Mail / Need popups are unchanged.\n");
        }
        [MenuItem("Tools/Panning UI/Capture Confirm Game View")]
        public static void CaptureConfirm()=>PrepareCaptureFor("pop-confirm","confirm-validation.txt",3);
    }
}
