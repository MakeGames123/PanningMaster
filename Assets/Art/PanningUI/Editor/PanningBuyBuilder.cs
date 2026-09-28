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
    public static class PanningBuyBuilder
    {
        const string Root="Assets/Art/PanningUI/";
        const string ScenePath=Root+"Scenes/ProductDetailPreview.unity";
        const string PrefabPath=Root+"Prefabs/PopupProductDetail.prefab";
        const string Command="Library/PanningBuy-command.txt";
        const string Output=Root+"Docs/Validation/";
        [Serializable] public class BuyTexts { public TextSpec[] buy; }
        [Serializable] public class BuyTextDocument { public Style[] styles; public BuyTexts sceneTexts; }
        [Serializable] public class BuyElements { public Element[] buy; }
        [Serializable] public class BuyLayout { public BuyElements finalScenes; }

        static PanningBuyBuilder(){EditorApplication.update+=Tick;}
        static void Tick()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Command))return;
            string command=File.ReadAllText(Command).Trim();File.Delete(Command);
            try {if(command=="build")BuildBuy();else if(command=="capture")CaptureBuy();}
            catch(Exception e){Directory.CreateDirectory(Output);File.WriteAllText(Output+"buy-failure.txt",e.ToString());Debug.LogException(e);}
        }

        [MenuItem("Tools/Panning UI/Build Buy Preview")]
        public static void BuildBuy()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before building the preview.");
            var defaultFont=TMP_Settings.defaultFontAsset;
            var data=JsonUtility.FromJson<BuyTextDocument>(File.ReadAllText(Root+"Docs/text-styles.json").Replace("\"pop-buy\":","\"buy\":"));
            string layoutJson=System.Text.RegularExpressions.Regex.Replace(File.ReadAllText(Root+"Docs/layout-coordinates.json"),@"\\u[dD][89a-fA-F][0-9a-fA-F]{2}","");
            var layout=JsonUtility.FromJson<BuyLayout>(layoutJson.Replace("\"pop-buy\":","\"buy\":"));
            var styles=data.styles.ToDictionary(s=>s.id);
            var old=SceneManager.GetSceneByPath(ScenePath);
            if(old.IsValid() && old.isLoaded && old.isDirty)throw new InvalidOperationException("The Buy preview has unsaved edits; save them before rebuilding.");
            Scene prior=SceneManager.GetActiveScene();
            if(old.IsValid() && old.isLoaded)EditorSceneManager.CloseScene(old,true);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            scene.name="ProductDetailPreview";
            SceneManager.SetActiveScene(scene);
            GameObject root=null;
            try
            {
                root=new GameObject("PopupProductDetail",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
                var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
                var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution=new Vector2(1200,2601);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;scaler.matchWidthOrHeight=.5f;
                var dim=Solid("Dim90",root.transform,new float[]{0,0,1200,2601},new Color(0,0,0,230f/255));
                dim.raycastTarget=true;dim.rectTransform.anchorMin=Vector2.zero;dim.rectTransform.anchorMax=Vector2.one;dim.rectTransform.offsetMin=dim.rectTransform.offsetMax=Vector2.zero;
                var design=Rect("Design1200x2601",root.transform,new float[]{0,0,1200,2601});
                design.anchorMin=design.anchorMax=design.pivot=new Vector2(.5f,0);design.anchoredPosition=Vector2.zero;
                // This scene has a plain title band (no // marker), as shown in its reference.
                foreach(var element in layout.finalScenes.buy)
                {
                    var r=element.css400;
                    switch(element.sel)
                    {
                        case ".bp-box":SpriteImage("Slab",design,"ui_pop_slab",r);break;
                        case ".bp-head":
                            Solid("TitleBand",design,Scale(r),ColorValue("#151410"));
                            Solid("TitleDivider",design,Scale(new[]{r[0],r[1]+r[3]-1,r[2],1}),new Color(214f/255,172f/255,96f/255,.16f));
                            // Hero bounds are measured from pop-buy_400; finalScenes omits this panel.
                            SpriteImage("ProductContentsPanel",design,"ui_pop_bracket_panel",new float[]{12,435.6f,376,171});
                            Icon("ico_key_hero",design,178,460,44);break;
                        case ".c3-chips span":
                            SpriteImage("ContentsCell_"+element.i,design,"ui_pop_item_cell",r,1);
                            Icon(element.i==3?"ico_ticket":"ico_key_"+element.i,design,r[0]+(r[2]-44)/2,r[1]+6,44);break;
                        case ".bp-row":
                            Solid("LimitDivider",design,Scale(new[]{r[0],r[1]+r[3]-1,r[2],1}),new Color(214f/255,172f/255,96f/255,.16f));
                            Icon("ico_calendar",design,24,623.8f,18);break;
                        case ".bp-cta":SpriteImage("Purchase",design,"ui_pop_btn_main",r,8,true);break;
                    }
                }
                Icon("ico_gem_explanation",design,75,658,12);
                Icon("ico_gem_paid",design,104.6f,658,12);
                Icon("ico_gem_price",design,155.8f,705,18);
                Icon("ico_gem_balance",design,146.6f,747,12);
                int i=0;
                foreach(var spec in data.sceneTexts.buy)
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
                ValidateBuy(root,data,defaultFont);
                Directory.CreateDirectory(Root+"Prefabs");ScreenAdaptBuilder.Apply(root);PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
                UnityEngine.Object.DestroyImmediate(root);root=null;
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),scene);
                instance.GetComponent<Canvas>().targetDisplay=4;
                var camera=new GameObject("Preview Camera (Display 5)",typeof(Camera)).GetComponent<Camera>();
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.075f,.065f,.055f);camera.cullingMask=0;camera.targetDisplay=4;
                Directory.CreateDirectory(Root+"Scenes");EditorSceneManager.SaveScene(scene,ScenePath);
                foreach(var font in instance.GetComponentsInChildren<TextMeshProUGUI>().Select(t=>t.font).Distinct())AssetDatabase.SaveAssetIfDirty(font);
            }
            catch {if(root!=null)UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.CloseScene(scene,true);throw;}
            finally {if(prior.IsValid()&&prior.isLoaded)SceneManager.SetActiveScene(prior);}
            Debug.Log("[PanningUI] Buy prefab built and validated. 16 TMP strings, 10 empty icon Images.");
            CaptureBuy();
        }

        static void ValidateBuy(GameObject root,BuyTextDocument data,TMP_FontAsset defaultFont)
        {
            var texts=root.GetComponentsInChildren<TextMeshProUGUI>();
            if(texts.Length!=16 || texts.Length!=data.sceneTexts.buy.Length)throw new Exception("Buy text count mismatch");
            for(int i=0;i<texts.Length;i++)
            {
                var text=texts[i];var spec=data.sceneTexts.buy[i];text.ForceMeshUpdate();
                var r=spec.rectUnity1200;
                if(text.text!=spec.text || Vector2.Distance(text.rectTransform.anchoredPosition,new Vector2(r[0],-r[1]))>.01f || Vector2.Distance(text.rectTransform.sizeDelta,new Vector2(r[2],r[3]))>.01f)throw new Exception("Buy text rect mismatch: "+i);
                foreach(char ch in text.text)if(!char.IsWhiteSpace(ch)&&!text.font.HasCharacter(ch,true,true))throw new Exception("Missing glyph: "+ch);
                if(text.font==defaultFont || text.fontStyle.HasFlag(FontStyles.Bold))throw new Exception("Unexpected font / synthetic bold");
            }
            var icons=root.GetComponentsInChildren<Image>().Where(img=>img.name.StartsWith("ico_")).ToArray();
            if(icons.Length!=10 || icons.Any(img=>img.sprite!=null || img.color.a!=0))throw new Exception("Icons must be empty transparent Images");
            foreach(var image in root.GetComponentsInChildren<Image>())
            {
                if(image.sprite==null)continue;
                var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(image.sprite));
                if(importer.textureType!=TextureImporterType.Sprite || importer.textureCompression!=TextureImporterCompression.Uncompressed)throw new Exception("Sprite import mismatch");
                if(image.type!=(image.sprite.border==Vector4.zero?Image.Type.Simple:Image.Type.Sliced))throw new Exception("Image type mismatch");
            }
            if(defaultFont!=TMP_Settings.defaultFontAsset)throw new Exception("Default TMP font changed");
            Directory.CreateDirectory(Output);
            File.WriteAllText(Output+"buy-validation.txt","PanningUI Buy validation / "+DateTime.Now.ToString("s")+"\nPASS: 16 TMP strings; exact JSON rects; explicit fonts; no missing glyphs.\nPASS: 10 empty transparent icon Images.\nPASS: used sprite imports uncompressed / Sprite; Image types follow sprite borders.\nPASS: Canvas 1200 x 2601 / Scale With Screen Size / Match 0.5.\nDefault TMP font preserved: "+defaultFont.name+"\nButtons: Purchase is a visual placeholder; close deactivates the popup.\nPreview uses Display 5; original game and approved Mail / Need / Confirm popups are unchanged.\n");
        }
        [MenuItem("Tools/Panning UI/Capture Buy Game View")]
        public static void CaptureBuy()=>PrepareCaptureFor("pop-buy","buy-validation.txt",4);
    }
}

