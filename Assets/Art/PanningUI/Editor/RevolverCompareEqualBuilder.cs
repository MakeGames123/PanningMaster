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
    public static class RevolverCompareEqualBuilder
    {
        const string Root="Assets/Art/PanningUI/";
        const string ScenePath=Root+"Scenes/RevolverCompareEqualPreview.unity";
        const string PrefabPath=Root+"Prefabs/PopupRevolverCompareEqual.prefab";
        const string Command="Library/RevolverCompareEqual-command.txt";
        const string Output=Root+"Docs/Validation/";
        [Serializable] public class RevolverCompareEqualTexts { public TextSpec[] revequal; }
        [Serializable] public class RevolverCompareEqualTextDocument { public Style[] styles; public RevolverCompareEqualTexts sceneTexts; }
        [Serializable] public class RevolverCompareEqualElements { public Element[] revequal; }
        [Serializable] public class RevolverCompareEqualLayout { public RevolverCompareEqualElements scenes; }

        static RevolverCompareEqualBuilder(){EditorApplication.update+=Tick;}
        static void Tick()
        {
            if(EditorApplication.isCompiling || EditorApplication.isUpdating || !File.Exists(Command))return;
            string command=File.ReadAllText(Command).Trim();File.Delete(Command);
            try {if(command=="build")BuildRevolverCompareEqual();else if(command=="capture")CaptureRevolverCompareEqual();}
            catch(Exception e){Directory.CreateDirectory(Output);File.WriteAllText(Output+"revequal-failure.txt",e.ToString());Debug.LogException(e);}
        }

        [MenuItem("Tools/Panning UI/Build RevolverCompareEqual Preview")]
        public static void BuildRevolverCompareEqual()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Exit Play Mode before building the preview.");
            var defaultFont=TMP_Settings.defaultFontAsset;
            var data=JsonUtility.FromJson<RevolverCompareEqualTextDocument>(File.ReadAllText(Root+"Docs/text-styles.json").Replace("\"rev-equal\":","\"revequal\":"));
            string layoutJson=System.Text.RegularExpressions.Regex.Replace(File.ReadAllText(Root+"Docs/layout-coordinates.json"),@"\\u[dD][89a-fA-F][0-9a-fA-F]{2}","");
            var layout=JsonUtility.FromJson<RevolverCompareEqualLayout>(layoutJson.Replace("\"rev-equal\":","\"revequal\":"));
            var styles=data.styles.ToDictionary(s=>s.id);
            var goldMaterial=MakeGoldMaterial();
            var old=SceneManager.GetSceneByPath(ScenePath);
            if(old.IsValid() && old.isLoaded && old.isDirty)throw new InvalidOperationException("The RevolverCompareEqual preview has unsaved edits; save them before rebuilding.");
            Scene prior=SceneManager.GetActiveScene();
            var approved=SceneManager.GetSceneByPath(Root+"Scenes/RevolverCompareDownPreview.unity");
            if(approved.IsValid()&&approved.isLoaded)
            {
                if(approved.isDirty)throw new InvalidOperationException("RevolverCompareDown preview has unsaved edits; cannot reuse Display 7.");
                EditorSceneManager.CloseScene(approved,true);
            }
            if(old.IsValid() && old.isLoaded)EditorSceneManager.CloseScene(old,true);
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
            scene.name="RevolverCompareEqualPreview";
            SceneManager.SetActiveScene(scene);
            GameObject root=null;
            try
            {
                root=new GameObject("PopupRevolverCompareEqual",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
                var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
                var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution=new Vector2(1200,2601);scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.Expand;scaler.matchWidthOrHeight=.5f;
                var dim=Solid("Dim90",root.transform,new float[]{0,0,1200,2601},new Color(0,0,0,230f/255));
                dim.raycastTarget=true;dim.rectTransform.anchorMin=Vector2.zero;dim.rectTransform.anchorMax=Vector2.one;dim.rectTransform.offsetMin=dim.rectTransform.offsetMax=Vector2.zero;
                var design=Rect("Design1200x2601",root.transform,new float[]{0,0,1200,2601});
                design.anchorMin=design.anchorMax=design.pivot=new Vector2(.5f,0);design.anchoredPosition=Vector2.zero;
                
                foreach(var element in layout.scenes.revequal)
                {
                    var r=element.css400;
                    switch(element.sel)
                    {
                        case ".rgc":
                            SpriteImage(element.i==0?"EquippedCard":"NewCardBase",design,"ui_rev_card_frame_normal",r,6,true);
                            break;
                        case ".rgc .hx":
                            SpriteImage(element.i==0?"EquippedHero":"NewHero",design,element.i==0?"ui_rev_hero_bg_05_A":"ui_rev_hero_bg_06_S",r);
                            Solid(element.i==0?"EquippedWeaponArt":"NewWeaponArt",design,Scale(new[]{r[0]+40,r[1]+20,106f,64f}),Color.clear);
                            break;
                        case ".rgc .hx .g":SpriteImage("Grade_"+element.i,design,element.i==0?"ui_rev_grade_05_A":"ui_rev_grade_06_S",r,14);break;
                        case ".rgc .hx .tag":SpriteImage("Tag_"+element.i,design,element.i==0?"ui_rev_tag_equipped_bg":"ui_rev_tag_new_bg",r,2);break;
                        case ".rgc .hx .lv":SpriteImage("Level_"+element.i,design,"ui_rev_lv_pill",r,2);break;
                        case ".rgc .n":
                            Solid("NameDivider_"+element.i,design,Scale(new[]{r[0],r[1]+r[3]-2,r[2],1}),new Color(214f/255,172f/255,96f/255,.16f));break;
                        case ".rgc .pw":Icon("ico_power",design,element.i==0?139:289,r[1]+6,18);break;
                        case ".rgc .k9-lb":SpriteImage("EquippedEffectsBand_"+element.i,design,"ui_rev_card_section_band",r);break;
                        case ".rgc .st":
                            int index=element.i%6;
                            string[] ids={"ico_attr_fire","ico_power","ico_crit","ico_critdmg","ico_attr_fire","ico_time"};
                            float x=index==0?(element.i==0?148:325):r[0]+4;
                            Icon(element.i==6?"ico_attr_ice":ids[index],design,x,r[1]+(r[3]-12)/2,12);
                            Solid("StatDivider_"+element.i,design,Scale(new[]{r[0],r[1]+r[3]-1,r[2],1}),new Color(214f/255,172f/255,96f/255,.08f));break;
                        case ".dk-pill":
                            SpriteImage("Explanation",design,"ui_rev_info_pill",r,3);
                            var circle=Rect("InfoCircle",design,Scale(new float[]{20,671.6f,16,16})).gameObject.AddComponent<PanningUI.UICircleGraphic>();
                            circle.color=ColorValue("#f1e6d4");circle.raycastTarget=false;
                            Icon("ico_power",design,49,673.5f,12);break;
                        case ".dk-chip":
                            SpriteImage("CrateStock",design,"ui_rev_crate_chip",r,3);
                            Icon("ico_crate",design,28,708.6f,18);break;
                        case ".dk-cta":
                            SpriteImage("Draw",design,"ui_rev_btn_gold",r,3,true);
                            Icon("ico_crate",design,207,734.7f,12);break;
                        case ".dk-x":
                            var closeLabel=Solid("CloseLabelButton",design,Scale(r),Color.clear);
                            closeLabel.raycastTarget=true;var button=closeLabel.gameObject.AddComponent<Button>();button.transition=Selectable.Transition.None;
                            UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(button.onClick,root.SetActive,false);
                            Icon("ico_close_s",design,r[0],r[1]+3,8);break;
                    }
                }
                int i=0;
                foreach(var spec in data.sceneTexts.revequal)
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
                    if(style.material!="default")text.fontSharedMaterial=style.material=="MAT_BtnGoldText"?goldMaterial:throw new Exception("Unmapped Gmarket material: "+style.material);
                    text.text=spec.text.Replace("\u2715 ","");
                    if(spec.text.StartsWith("\u2715 "))
                    {
                        // Preserve the complete JSON rect; reserve the first 12 CSS px for the empty close icon.
                        text.margin=new Vector4(36,0,0,0);
                    }
                }
                var close=SpriteImage("Close",design,"ui_nav_close_t01a",new float[]{178,808,44,46},0,true);
                UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(close.GetComponent<Button>().onClick,root.SetActive,false);
                ApplyDownTone(root);
                ValidateRevolverCompareEqual(root,data,defaultFont);
                Directory.CreateDirectory(Root+"Prefabs");ScreenAdaptBuilder.Apply(root);PrefabUtility.SaveAsPrefabAsset(root,PrefabPath);
                UnityEngine.Object.DestroyImmediate(root);root=null;
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),scene);
                instance.GetComponent<Canvas>().targetDisplay=6;
                var camera=new GameObject("Preview Camera (Display 7)",typeof(Camera)).GetComponent<Camera>();
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.075f,.065f,.055f);camera.cullingMask=0;camera.targetDisplay=6;
                Directory.CreateDirectory(Root+"Scenes");EditorSceneManager.SaveScene(scene,ScenePath);
                foreach(var font in instance.GetComponentsInChildren<TextMeshProUGUI>().Select(t=>t.font).Distinct())AssetDatabase.SaveAssetIfDirty(font);
            }
            catch {if(root!=null)UnityEngine.Object.DestroyImmediate(root);EditorSceneManager.CloseScene(scene,true);throw;}
            finally {if(prior.IsValid()&&prior.isLoaded)SceneManager.SetActiveScene(prior);}
            Debug.Log("[PanningUI] RevolverCompareEqual prefab built and validated. 49 TMP strings, 18 empty icon Images.");
            CaptureRevolverCompareEqual();
        }

        static void ApplyDownTone(GameObject root)
        {
            Directory.CreateDirectory(Root+"Materials");
            foreach(var graphic in root.GetComponentsInChildren<Graphic>())
            {
                var rect=graphic.rectTransform;
                float x=rect.anchoredPosition.x, y=-rect.anchoredPosition.y;
                if(x<198*3 || y<229*3 || y>653*3)continue;
                var text=graphic as TextMeshProUGUI;
                Material source=text!=null?text.fontSharedMaterial:graphic.defaultMaterial;
                string name=text!=null?"MAT_"+text.font.name.Replace(" ","_")+"_CompareDown":"MAT_UI_CompareDown";
                string path=Root+"Materials/"+name+".mat";
                var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(mat==null){mat=new Material(source);AssetDatabase.CreateAsset(mat,path);}
                mat.CopyPropertiesFromMaterial(source);mat.name=name;
                mat.shader=Shader.Find(text!=null?"PanningUI/TMP Compare Down":"PanningUI/Image Compare Down");
                if(mat.shader==null)throw new Exception("Missing comparison tone shader");
                if(text!=null)text.fontSharedMaterial=mat;else graphic.material=mat;
                EditorUtility.SetDirty(mat);AssetDatabase.SaveAssetIfDirty(mat);
            }
        }

        static Material MakeGoldMaterial()
        {
            var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root+"Fonts/SDF/GmarketSans-Bold SDF.asset");
            if(font==null)throw new Exception("Gmarket bold SDF missing");
            string path=Root+"Fonts/SDF/MAT_GmarketBold_BtnGoldText.mat";
            var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
            if(mat==null){mat=new Material(font.material);AssetDatabase.CreateAsset(mat,path);}
            mat.name="MAT_GmarketBold_BtnGoldText";
            mat.shader=Shader.Find("TextMeshPro/Distance Field");
            mat.mainTexture=font.atlasTextures[0];
            mat.SetFloat("_TextureWidth",font.atlasWidth);mat.SetFloat("_TextureHeight",font.atlasHeight);
            mat.SetFloat("_GradientScale",font.atlasPadding+1);
            mat.SetColor("_FaceColor",Color.white);mat.EnableKeyword("UNDERLAY_ON");mat.DisableKeyword("RATIOS_OFF");
            mat.SetColor("_UnderlayColor",new Color(1,245f/255,210f/255,.5f));
            mat.SetFloat("_UnderlayOffsetY",-90f/(16*(font.atlasPadding+1)));mat.SetFloat("_UnderlaySoftness",0);
            EditorUtility.SetDirty(mat);AssetDatabase.SaveAssetIfDirty(mat);return mat;
        }

        static void ValidateRevolverCompareEqual(GameObject root,RevolverCompareEqualTextDocument data,TMP_FontAsset defaultFont)
        {
            var texts=root.GetComponentsInChildren<TextMeshProUGUI>();
            if(texts.Length!=49 || texts.Length!=data.sceneTexts.revequal.Length)throw new Exception("RevolverCompareEqual text count mismatch");
            for(int i=0;i<texts.Length;i++)
            {
                var text=texts[i];var spec=data.sceneTexts.revequal[i];text.ForceMeshUpdate();
                var r=spec.rectUnity1200;
                if(text.text!=spec.text.Replace("\u2715 ","") || Vector2.Distance(text.rectTransform.anchoredPosition,new Vector2(r[0],-r[1]))>.01f || Vector2.Distance(text.rectTransform.sizeDelta,new Vector2(r[2],r[3]))>.01f)throw new Exception("RevolverCompareEqual text rect mismatch: "+i);
                foreach(char ch in text.text)if(!char.IsWhiteSpace(ch)&&!text.font.HasCharacter(ch,true,true))throw new Exception("Missing glyph: "+ch);
                if(text.font==defaultFont || text.fontStyle.HasFlag(FontStyles.Bold))throw new Exception("Unexpected font / synthetic bold");
            }
            var icons=root.GetComponentsInChildren<Image>().Where(img=>img.name.StartsWith("ico_")).ToArray();
            if(icons.Length!=18 || icons.Any(img=>img.sprite!=null || img.color.a!=0))throw new Exception("Icons must be empty transparent Images");
            foreach(var image in root.GetComponentsInChildren<Image>())
            {
                if(image.sprite==null)continue;
                var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(image.sprite));
                if(importer.textureType!=TextureImporterType.Sprite || importer.textureCompression!=TextureImporterCompression.Uncompressed)throw new Exception("Sprite import mismatch");
                if(image.type!=(image.sprite.border==Vector4.zero?Image.Type.Simple:Image.Type.Sliced))throw new Exception("Image type mismatch");
            }
            var label=texts.Single(t=>t.name.StartsWith("Text_46_"));
            if(label.fontSharedMaterial.mainTexture!=label.font.atlasTextures[0])throw new Exception("Gmarket material atlas mismatch");
            if(root.GetComponentsInChildren<Transform>(true).Any(t=>t.name.Contains("Reflection") || t.name.Contains("Sheen")))throw new Exception("No reflection allowed");
            if(defaultFont!=TMP_Settings.defaultFontAsset)throw new Exception("Default TMP font changed");
            Directory.CreateDirectory(Output);
            File.WriteAllText(Output+"revequal-validation.txt","PanningUI RevolverCompareEqual validation / "+DateTime.Now.ToString("s")+"\nPASS: 49 TMP strings; exact JSON rects; explicit fonts; no missing glyphs.\nPASS: 18 empty transparent icon Images.\nPASS: used sprite imports uncompressed / Sprite; Image types follow sprite borders.\nPASS: Canvas 1200 x 2601 / Scale With Screen Size / Match 0.5.\nDefault TMP font preserved: "+defaultFont.name+"\nCards and Draw are visual placeholders; both close buttons deactivate the popup.\nPreview uses Display 7; prior down-state scene remains saved. Equal state uses plain frames without reflection/glow; total power delta is neutral 0. Weapon art slots are empty.\n");
        }
        [MenuItem("Tools/Panning UI/Capture RevolverCompareEqual Game View")]
        public static void CaptureRevolverCompareEqual(){PrepareCaptureFor("rev-equal","revequal-validation.txt",6);}
    }
}
