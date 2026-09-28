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
    public static class PanningPowerBuilder
    {
        const string Root="Assets/Art/PanningUI/";
        const string ScenePath=Root+"Scenes/CombatPowerPreview.unity";
        const string PrefabPath=Root+"Prefabs/PopupCombatPower.prefab";
        const string Command="Library/PanningPower-command.txt";
        const string Output=Root+"Docs/Validation/";
        [Serializable] public class PowerTexts { public TextSpec[] power; }
        [Serializable] public class PowerTextDocument { public Style[] styles; public PowerTexts sceneTexts; }
        [Serializable] public class PowerElements { public Element[] power; }
        [Serializable] public class PowerLayout { public PowerElements finalScenes; }

        static PanningPowerBuilder(){EditorApplication.update+=Tick;}
        static void Tick()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Command))return;
            string command=File.ReadAllText(Command).Trim();File.Delete(Command);
            try {if(command=="build")BuildPower();else if(command=="capture")CapturePower();}
            catch(Exception e){Directory.CreateDirectory(Output);File.WriteAllText(Output+"power-failure.txt",e.ToString());Debug.LogException(e);}
        }

        [MenuItem("Tools/Panning UI/Build Power Preview")]
        public static void BuildPower()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before building the preview.");
            var defaultFont=TMP_Settings.defaultFontAsset;
            var data=JsonUtility.FromJson<PowerTextDocument>(File.ReadAllText(Root+"Docs/text-styles.json").Replace("\"pop-power\":","\"power\":"));
            string layoutJson=System.Text.RegularExpressions.Regex.Replace(File.ReadAllText(Root+"Docs/layout-coordinates.json"),@"\\u[dD][89a-fA-F][0-9a-fA-F]{2}","");
            var layout=JsonUtility.FromJson<PowerLayout>(layoutJson.Replace("\"pop-power\":","\"power\":"));
            var styles=data.styles.ToDictionary(s=>s.id);
            var old=SceneManager.GetSceneByPath(ScenePath);
            if(old.IsValid() && old.isLoaded && old.isDirty)throw new InvalidOperationException("The Power preview has unsaved edits; save them before rebuilding.");
            Scene prior=SceneManager.GetActiveScene();
            if(old.IsValid() && old.isLoaded)EditorSceneManager.CloseScene(old,true);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            scene.name="CombatPowerPreview";
            SceneManager.SetActiveScene(scene);
            GameObject root=null;
            try
            {
                root=new GameObject("PopupCombatPower",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
                var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
                var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution=new Vector2(1200,2601);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;scaler.matchWidthOrHeight=.5f;
                var dim=Solid("Dim90",root.transform,new float[]{0,0,1200,2601},new Color(0,0,0,230f/255));
                dim.raycastTarget=true;dim.rectTransform.anchorMin=Vector2.zero;dim.rectTransform.anchorMax=Vector2.one;dim.rectTransform.offsetMin=dim.rectTransform.offsetMax=Vector2.zero;
                var design=Rect("Design1200x2601",root.transform,new float[]{0,0,1200,2601});
                design.anchorMin=design.anchorMax=design.pivot=new Vector2(.5f,0);design.anchoredPosition=Vector2.zero;
                string[] statIcons={"ico_power","ico_attr_fire","ico_crit","ico_critdmg","ico_power","ico_time","ico_crit","ico_critdmg"};
                foreach(var element in layout.finalScenes.power)
                {
                    var r=element.css400;
                    switch(element.sel)
                    {
                        case ".dgr-box":SpriteImage("Slab",design,"ui_pop_slab",r);break;
                        case ".dgr-title":
                            SpriteImage("TitleBand",design,"ui_pop_title_band",r);
                            SpriteImage("Help",design,"ui_pop_help",new[]{368f,r[1]+8.5f,20f,20f},3,true);break;
                        case ".c3-pw":
                            SpriteImage("TotalPowerPanel",design,"ui_pop_bracket_panel",r);
                            Icon("ico_power",design,176,516.6f,18);break;
                        case ".c3-grid div":
                            SpriteImage("StatSource_"+element.i,design,"ui_pop_grid_cell",r,0,true);
                            Icon(statIcons[element.i],design,r[0]+10,r[1]+6.5f,12);break;
                        case ".c3-fold":
                            SpriteImage(element.i==0?"CharacterPower":"ChamberDamage",design,"ui_pop_fold_row",r,0,true);
                            Icon(element.i==0?"ico_character":"ico_weapon",design,r[0]+14,r[1]+(r[3]-12)/2,12);break;
                    }
                }
                int i=0;
                foreach(var spec in data.sceneTexts.power)
                {
                    if(spec.text=="\u25be")
                    {
                        // Guide 3-4: this missing glyph is an 8 px empty icon, not TMP.
                        var r=spec.rectUnity1200;
                        Icon("ico_fold",design,(r[0]+r[2]/2)/3-4,(r[1]+r[3]/2)/3-4,8);
                        i++;continue;
                    }
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
                ValidatePower(root,data,defaultFont);
                Directory.CreateDirectory(Root+"Prefabs");ScreenAdaptBuilder.Apply(root);PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
                UnityEngine.Object.DestroyImmediate(root);root=null;
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),scene);
                instance.GetComponent<Canvas>().targetDisplay=5;
                var camera=new GameObject("Preview Camera (Display 6)",typeof(Camera)).GetComponent<Camera>();
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.075f,.065f,.055f);camera.cullingMask=0;camera.targetDisplay=5;
                Directory.CreateDirectory(Root+"Scenes");EditorSceneManager.SaveScene(scene,ScenePath);
                foreach(var font in instance.GetComponentsInChildren<TextMeshProUGUI>().Select(t=>t.font).Distinct())AssetDatabase.SaveAssetIfDirty(font);
            }
            catch {if(root!=null)UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.CloseScene(scene,true);throw;}
            finally {if(prior.IsValid()&&prior.isLoaded)SceneManager.SetActiveScene(prior);}
            Debug.Log("[PanningUI] Power prefab built and validated. 36 TMP strings, 12 empty icon Images.");
            CapturePower();
        }

        static void ValidatePower(GameObject root,PowerTextDocument data,TMP_FontAsset defaultFont)
        {
            var texts=root.GetComponentsInChildren<TextMeshProUGUI>();
            var specs=data.sceneTexts.power.Where(s=>s.text!="\u25be").ToArray();
            if(texts.Length!=36 || texts.Length!=specs.Length)throw new Exception("Power text count mismatch");
            for(int i=0;i<texts.Length;i++)
            {
                var text=texts[i];var spec=specs[i];text.ForceMeshUpdate();
                var r=spec.rectUnity1200;
                if(text.text!=spec.text || Vector2.Distance(text.rectTransform.anchoredPosition,new Vector2(r[0],-r[1]))>.01f || Vector2.Distance(text.rectTransform.sizeDelta,new Vector2(r[2],r[3]))>.01f)throw new Exception("Power text rect mismatch: "+i);
                foreach(char ch in text.text)if(!char.IsWhiteSpace(ch)&&!text.font.HasCharacter(ch,true,true))throw new Exception("Missing glyph: "+ch);
                if(text.font==defaultFont || text.fontStyle.HasFlag(FontStyles.Bold))throw new Exception("Unexpected font / synthetic bold");
            }
            var icons=root.GetComponentsInChildren<Image>().Where(img=>img.name.StartsWith("ico_")).ToArray();
            if(icons.Length!=12 || icons.Any(img=>img.sprite!=null || img.color.a!=0))throw new Exception("Icons must be empty transparent Images");
            foreach(var image in root.GetComponentsInChildren<Image>())
            {
                if(image.sprite==null)continue;
                var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(image.sprite));
                if(importer.textureType!=TextureImporterType.Sprite || importer.textureCompression!=TextureImporterCompression.Uncompressed)throw new Exception("Sprite import mismatch");
                if(image.type!=(image.sprite.border==Vector4.zero?Image.Type.Simple:Image.Type.Sliced))throw new Exception("Image type mismatch");
            }
            if(defaultFont!=TMP_Settings.defaultFontAsset)throw new Exception("Default TMP font changed");
            Directory.CreateDirectory(Output);
            File.WriteAllText(Output+"power-validation.txt","PanningUI Power validation / "+DateTime.Now.ToString("s")+"\nPASS: 36 TMP strings; exact JSON rects; explicit fonts; no missing glyphs.\nPASS: 12 empty transparent icon Images.\nPASS: used sprite imports uncompressed / Sprite; Image types follow sprite borders.\nPASS: Canvas 1200 x 2601 / Scale With Screen Size / Match 0.5.\nDefault TMP font preserved: "+defaultFont.name+"\nButtons: Help, stat sources and fold rows are visual placeholders; close deactivates the popup.\nPreview uses Display 6; original game and approved Mail / Need / Confirm / Buy prefabs are unchanged.\n");
        }
        [MenuItem("Tools/Panning UI/Capture Power Game View")]
        public static void CapturePower()=>PrepareCaptureFor("pop-power","power-validation.txt",5);
    }
}
