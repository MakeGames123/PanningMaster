using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace PanningUI.Editor
{
    // Explicit commands only. Nothing is rebuilt just because the editor opens.
    [InitializeOnLoad]
    public static class PanningMailBuilder
    {
        const string Root = "Assets/Art/PanningUI/";
        const string Command = "Library/PanningUI-command.txt";
        const string Output = Root + "Docs/Validation/";
        static readonly Dictionary<string, TMP_FontAsset> Fonts = new Dictionary<string, TMP_FontAsset>();
        static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        static readonly StringBuilder Report = new StringBuilder();
        static UnityEngine.Object DefaultFont;
        static EditorWindow GameView;
        static double CaptureAt;
        static bool Capturing;
        static string CaptureScene = "pop-mail";
        static string CaptureReport = "mail-validation.txt";
        static int CaptureDisplay = 1;
        [Serializable] public class Style { public string id, fontAsset, color, material; public float unityFontSize, characterSpacing; public bool italic; }
        [Serializable] public class TextSpec { public string text, style, align; public float[] rectUnity1200; }
        [Serializable] public class TextScenes { public TextSpec[] mail; }
        [Serializable] public class TextDocument { public Style[] styles; public TextScenes sceneTexts; }
        [Serializable] public class Element { public string sel; public int i; public float[] css400; }
        [Serializable] public class LayoutScenes { public Element[] mail; }
        [Serializable] public class LayoutDocument { public LayoutScenes finalScenes; }

        static PanningMailBuilder() { EditorApplication.update += Tick; }
        static void Tick()
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            if (Capturing && EditorApplication.timeSinceStartup > CaptureAt)
            {
                Capturing = false;
                try { Capture(); } catch (Exception e) { Fail(e); }
            }
            if (!File.Exists(Command)) return;
            string cmd = File.ReadAllText(Command).Trim();
            File.Delete(Command);
            try
            {
                if (cmd == "build") Build();
                else if (cmd == "capture") PrepareCapture();
                else if (cmd == "inspect") InspectGameView();
            }
            catch (Exception e) { Fail(e); }
        }
        static void Fail(Exception e)
        {
            Directory.CreateDirectory(Output);
            File.WriteAllText(Output + "failure.txt", e.ToString());
            Debug.LogException(e);
        }

        [MenuItem("Tools/Panning UI/Build Mail Preview")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Exit Play Mode before building the preview.");
            Directory.CreateDirectory(Output);
            Report.Clear();
            if(File.Exists(Output+"failure.txt")) AssetDatabase.DeleteAsset(Output+"failure.txt");
            Report.AppendLine("PanningUI mail validation / " + DateTime.Now.ToString("s"));
            DefaultFont = TMP_Settings.defaultFontAsset;
            AuditSprites();
            MakeFonts();
            MakeMaterials();
            var data = JsonUtility.FromJson<TextDocument>(File.ReadAllText(Root + "Docs/text-styles.json").Replace("\"pop-mail\":", "\"mail\":"));
            // Some unused layout text excerpts end in a truncated escaped surrogate.
            // Strip only those escapes in memory; all numeric coordinates and source files remain intact.
            string layoutJson = System.Text.RegularExpressions.Regex.Replace(File.ReadAllText(Root + "Docs/layout-coordinates.json"), @"\\u[dD][89a-fA-F][0-9a-fA-F]{2}", "");
            var layout = JsonUtility.FromJson<LayoutDocument>(layoutJson.Replace("\"pop-mail\":", "\"mail\":"));
            var styles = data.styles.ToDictionary(s => s.id);
            Scene prior = SceneManager.GetActiveScene();
            var existing = SceneManager.GetSceneByPath(Root+"Scenes/MailboxPreview.unity");
            if(existing.IsValid() && existing.isLoaded && existing.isDirty) throw new InvalidOperationException("Unsaved Mailbox preview preserved.");
            if(existing.IsValid() && existing.isLoaded) EditorSceneManager.CloseScene(existing,true);
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            scene.name = "MailboxPreview";
            SceneManager.SetActiveScene(scene);
            GameObject root = new GameObject("PopupMailbox", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1200, 2601);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.matchWidthOrHeight = .5f;
            var design = Rect("Design1200x2601", root.transform, new float[]{0,0,1200,2601});
            design.anchorMin = design.anchorMax = new Vector2(.5f,0);
            design.pivot = new Vector2(.5f,0);
            design.anchoredPosition = Vector2.zero;
            var dim = Solid("Dim90", root.transform, new float[]{0,0,1200,2601}, new Color(0,0,0,230f/255));
            dim.rectTransform.anchorMin = Vector2.zero; dim.rectTransform.anchorMax = Vector2.one;
            dim.rectTransform.offsetMin = dim.rectTransform.offsetMax = Vector2.zero;
            dim.raycastTarget = true;
            dim.transform.SetAsFirstSibling();
            foreach (var e in layout.finalScenes.mail)
            {
                var r = e.css400;
                if(e.sel == ".dgr-box") SpriteImage("Slab", design, "ui_pop_slab", r);
                else if(e.sel == ".dgr-title") SpriteImage("TitleBand", design, "ui_pop_title_band", r);
                else if(e.sel == ".mail-row")
                {
                    Solid("RowDivider_"+e.i, design, Scale(new float[]{r[0],r[1]+r[3]-1,r[2],1}), new Color(214f/255,172f/255,96f/255,.16f));
                    SpriteImage("IconFrame_"+e.i, design, "ui_pop_icon_frame", new float[]{22,r[1]+8,36,36}, 2);
                    Icon(new[]{"ico_trophy","ico_calendar","ico_gift"}[e.i],design,31,r[1]+17,18);
                }
                else if(e.sel == ".mi-go") SpriteImage("Claim_"+e.i,design,"ui_pop_btn_small",r,6,true);
                else if(e.sel == "#mail-claim") SpriteImage("ClaimAll",design,"ui_pop_btn_main",r,8,true);
                else if(e.sel == ".c3-mc span")
                {
                    string[] ids = {"ico_gold","ico_ticket","ico_crate","ico_gold","ico_crate","ico_scroll","ico_crate"};
                    // Reward boxes include the CSS gap and, after the first item, a left separator.
                    bool first = e.i == 0 || e.i == 3 || e.i == 5;
                    float ix = r[0] + (first ? 2 : 13);
                    Icon(ids[e.i],design,ix,r[1]+.5f,12);
                    if(!first) Solid("RewardDivider_"+e.i,design,Scale(new float[]{r[0]+4,r[1],1,r[3]}),new Color(214f/255,172f/255,96f/255,.16f));
                }
            }
            int ti = 0;
            foreach(var spec in data.sceneTexts.mail)
            {
                var st = styles[spec.style];
                var rt = Rect("Text_"+(ti++).ToString("D2")+"_"+spec.style,design,spec.rectUnity1200);
                var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
                tmp.font = Fonts[st.fontAsset];
                tmp.fontSize = st.unityFontSize;
                tmp.fontStyle = st.italic ? FontStyles.Italic : FontStyles.Normal;
                tmp.color = ColorValue(st.color);
                tmp.characterSpacing = st.characterSpacing;
                tmp.enableAutoSizing = false;
                tmp.textWrappingMode = TextWrappingModes.NoWrap;
                tmp.overflowMode = TextOverflowModes.Overflow;
                tmp.alignment = spec.align == "center" ? TextAlignmentOptions.Center : spec.align == "right" ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
                tmp.raycastTarget = false;
                tmp.richText = false;
                if(st.material != "default") tmp.fontSharedMaterial = Materials[st.material];
                tmp.text = spec.text;
            }
            // Navigation remains a host-game concern. Only the popup's supplied close artwork is above the dim.
            var close = SpriteImage("Close",design,"ui_nav_close_t01a",new float[]{178,808,44,46},0,true);
            UnityEditor.Events.UnityEventTools.AddBoolPersistentListener(close.GetComponent<Button>().onClick,root.SetActive,false);
            Directory.CreateDirectory(Root+"Prefabs");
            string prefabPath = Root+"Prefabs/PopupMailbox.prefab";
            ScreenAdaptBuilder.Apply(root);PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath),scene);
            // Display 2 isolates this preview from every existing scene without editing or disabling its objects.
            instance.GetComponent<Canvas>().targetDisplay = 1;
            var camera = new GameObject("Preview Camera (Display 2)",typeof(Camera)).GetComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.075f,.065f,.055f);
            camera.cullingMask = 0;
            camera.targetDisplay = 1;
            Directory.CreateDirectory(Root+"Scenes");
            EditorSceneManager.SaveScene(scene,Root+"Scenes/MailboxPreview.unity");
            SceneManager.SetActiveScene(prior);
            Validate(instance,data,styles);
            foreach(var font in Fonts.Values) AssetDatabase.SaveAssetIfDirty(font);
            foreach(var material in Materials.Values) AssetDatabase.SaveAssetIfDirty(material);
            Report.AppendLine("Default TMP font preserved: "+DefaultFont.name);
            Report.AppendLine("Preview on Display 2. Existing game scene and navigation are untouched.");
            File.WriteAllText(Output+"mail-validation.txt",Report.ToString());
            Debug.Log("[PanningUI] Mail prefab, 6 dynamic fonts and 5 materials built. "+Output+"mail-validation.txt");
            PrepareCapture();
        }

        static void AuditSprites()
        {
            var csv = File.ReadAllLines(Root+"Docs/unity-import.csv").Skip(1).Select(SplitCsv).ToDictionary(c=>Path.GetFileName(c[0]));
            int count=0, sliced=0, reset=0;
            foreach(string path in Directory.GetFiles(Root+"Sprites","*.png",SearchOption.AllDirectories))
            {
                var asset = path.Replace('\\','/');
                var imp = (TextureImporter)AssetImporter.GetAtPath(asset);
                Vector4 border=Vector4.zero;
                if(csv.TryGetValue(Path.GetFileName(asset),out var c) && c[3]=="Sliced") { var b=c[4].Split(',').Select(float.Parse).ToArray(); border=new Vector4(b[0],b[1],b[2],b[3]); sliced++; }
                var settings=new TextureImporterSettings(); imp.ReadTextureSettings(settings);
                bool wrong = imp.textureType!=TextureImporterType.Sprite || imp.spriteImportMode!=SpriteImportMode.Single || imp.textureCompression!=TextureImporterCompression.Uncompressed || imp.mipmapEnabled || imp.spriteBorder!=border || imp.spritePixelsPerUnit!=100 || settings.spriteMeshType!=SpriteMeshType.FullRect || imp.filterMode!=FilterMode.Bilinear || imp.wrapMode!=TextureWrapMode.Clamp;
                foreach(string platform in new[]{"Android","iPhone","Standalone"}) { var p=imp.GetPlatformTextureSettings(platform); if(p.overridden && p.textureCompression!=TextureImporterCompression.Uncompressed) wrong=true; }
                if(wrong)
                {
                    File.Delete(asset+".meta");
                    AssetDatabase.ImportAsset(asset,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
                    imp=(TextureImporter)AssetImporter.GetAtPath(asset);
                    Report.AppendLine("Reimported mismatched metadata: "+asset); reset++;
                    if(imp.spriteBorder!=border || imp.textureType!=TextureImporterType.Sprite || imp.textureCompression!=TextureImporterCompression.Uncompressed) throw new Exception("Import failed: "+asset);
                }
                count++;
            }
            Report.AppendLine($"Sprite audit: {count} PNG / {sliced} sliced borders match / {reset} mismatched metadata reimported.");
        }
        static List<string> SplitCsv(string s)
        {
            var list=new List<string>(); var value=new StringBuilder(); bool quoted=false;
            foreach(char c in s) { if(c=='\"') quoted=!quoted; else if(c==','&&!quoted) {list.Add(value.ToString());value.Clear();} else value.Append(c); }
            list.Add(value.ToString()); return list;
        }
        static void MakeFonts()
        {
            Directory.CreateDirectory(Root+"Fonts/SDF"); Fonts.Clear();
            foreach(string source in Directory.GetFiles(Root+"Fonts","*",SearchOption.AllDirectories).Where(p=>p.EndsWith(".otf")||p.EndsWith(".ttf")))
            {
                string name=Path.GetFileNameWithoutExtension(source)+" SDF", path=Root+"Fonts/SDF/"+name+".asset";
                var importer=(TrueTypeFontImporter)AssetImporter.GetAtPath(source.Replace('\\','/'));
                if(!importer.includeFontData) {importer.includeFontData=true;importer.SaveAndReimport();}
                var font=AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if(font==null)
                {
                    font=TMP_FontAsset.CreateFontAsset(AssetDatabase.LoadAssetAtPath<Font>(source.Replace('\\','/')),90,9,GlyphRenderMode.SDFAA,2048,2048,AtlasPopulationMode.Dynamic,true);
                    font.name=name;
                    AssetDatabase.CreateAsset(font,path);
                    font.atlasTextures[0].name=name+" Atlas";
                    font.material.name=name+" Material";
                    AssetDatabase.AddObjectToAsset(font.atlasTextures[0],font);
                    AssetDatabase.AddObjectToAsset(font.material,font);
                }
                var so=new SerializedObject(font); so.FindProperty("m_ClearDynamicDataOnBuild").boolValue=true; so.ApplyModifiedPropertiesWithoutUndo();
                Fonts[name]=font;
            }
            Fonts["Pretendard-SemiBold SDF"].fallbackFontAssetTable=new List<TMP_FontAsset>{Fonts["PretendardJP-SemiBold SDF"]};
            Fonts["Pretendard-Medium SDF"].fallbackFontAssetTable=new List<TMP_FontAsset>{Fonts["PretendardJP-Medium SDF"]};
            Fonts["GmarketSans-Bold SDF"].fallbackFontAssetTable=new List<TMP_FontAsset>{Fonts["Pretendard-SemiBold SDF"]};
            Fonts["GmarketSans-Medium SDF"].fallbackFontAssetTable=new List<TMP_FontAsset>{Fonts["Pretendard-Medium SDF"]};
            foreach(var f in Fonts.Values) EditorUtility.SetDirty(f);
            foreach(var f in Fonts.Values)
            {
                var so=new SerializedObject(f);
                if(f.atlasPopulationMode!=AtlasPopulationMode.Dynamic || f.faceInfo.pointSize!=90 || f.atlasPadding!=9 || f.atlasWidth!=2048 || f.atlasHeight!=2048 || f.atlasRenderMode!=GlyphRenderMode.SDFAA || !f.isMultiAtlasTexturesEnabled || f.sourceFontFile==null || !so.FindProperty("m_ClearDynamicDataOnBuild").boolValue)
                    throw new Exception("Font configuration mismatch: "+f.name);
            }
            if(!Fonts["GmarketSans-Bold SDF"].HasCharacter('✓',true,true) || !Fonts["GmarketSans-Medium SDF"].HasCharacter('›',true,true) || !Fonts["Pretendard-SemiBold SDF"].HasCharacter('あ',true,true) || !Fonts["Pretendard-Medium SDF"].HasCharacter('あ',true,true))
                throw new Exception("Fallback glyph verification failed");
            Report.AppendLine("Fonts: 6 dynamic / 90 pt / padding 9 / SDFAA / 2048 square / multi atlas / font data / clear on build. Fallback chains assigned.");
        }
        static void MakeMaterials()
        {
            Materials.Clear();
            string[] names={"MAT_Shadow1","MAT_BtnGoldText","MAT_NumShadow","MAT_UnlockFx","MAT_ClearFx"};
            float[] offsets={1,1,2.5f,4,5}; float[] sizes={15,17,40,46,64};
            for(int i=0;i<names.Length;i++)
            {
                string path=Root+"Fonts/SDF/"+names[i]+".mat";
                var mat=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(mat==null){mat=new Material(Fonts["Pretendard-SemiBold SDF"].material);AssetDatabase.CreateAsset(mat,path);}
                mat.name=names[i];mat.shader=Shader.Find("TextMeshPro/Distance Field");
                mat.SetColor("_FaceColor",i>=3?ColorValue("#ffd87a"):Color.white);
                mat.EnableKeyword("UNDERLAY_ON");
                mat.SetColor("_UnderlayColor",i==1?new Color(1,245f/255,210f/255,.5f):i==2?new Color(0,0,0,.35f):i>=3?ColorValue("#8a4a10"):Color.black);
                // SDF offsets are atlas units: gradientScale=padding+1 and face point size=90.
                mat.DisableKeyword("RATIOS_OFF");
                mat.SetFloat("_UnderlayOffsetY",-offsets[i]*90f/(sizes[i]*10f));
                mat.SetFloat("_UnderlaySoftness",i==2?.1f:0);
                if(i>=3)
                {
                    mat.EnableKeyword("GLOW_ON");mat.SetColor("_GlowColor",new Color(1,200f/255,80f/255,i==3?.4f:.55f));
                    float desiredRadius=i==3?22:26;
                    mat.SetFloat("_GlowOuter",Mathf.Min(1,desiredRadius*90/(sizes[i]*10)));
                    // Preserve the design target. Wide CSS glows exceed the 9 px SDF padding;
                    // their final appearance is reviewed when those effect popups are assembled.
                    mat.SetOverrideTag("PanningUI_GlowRadiusDesignPx",desiredRadius.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
                else mat.DisableKeyword("GLOW_ON");
                EditorUtility.SetDirty(mat);Materials[names[i]]=mat;
            }
        }
        static void Validate(GameObject root,TextDocument data,Dictionary<string,Style> styles)
        {
            var texts=root.GetComponentsInChildren<TextMeshProUGUI>(true);
            if(texts.Length!=data.sceneTexts.mail.Length) throw new Exception("Text count mismatch");
            foreach(var t in texts)
            {
                t.ForceMeshUpdate();
                foreach(char ch in t.text) if(!char.IsWhiteSpace(ch)&&!t.font.HasCharacter(ch,true,true)) throw new Exception("Missing glyph: "+ch);
                if(t.font==DefaultFont || t.fontStyle.HasFlag(FontStyles.Bold)) throw new Exception("Unexpected default font / synthetic bold");
            }
            if(TMP_Settings.defaultFontAsset!=DefaultFont) throw new Exception("Default font changed");
            foreach(var img in root.GetComponentsInChildren<Image>(true)) if(img.name.StartsWith("ico_") && img.sprite!=null) throw new Exception("Icon must be empty");
            Report.AppendLine($"Mail: {texts.Length} TMP strings, explicit fonts, exact JSON text rects/styles, no missing glyphs, empty icon Images.");
        }
        internal static RectTransform Rect(string name,Transform parent,float[] r)
        {
            var go=new GameObject(name,typeof(RectTransform)); var rt=(RectTransform)go.transform;
            rt.SetParent(parent,false);rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(0,1);
            rt.anchoredPosition=new Vector2(r[0],-r[1]);rt.sizeDelta=new Vector2(r[2],r[3]);return rt;
        }
        internal static float[] Scale(float[] r)=>r.Select(v=>v*3).ToArray();
        internal static Image Solid(string name,Transform parent,float[] r,Color c)
        { var image=Rect(name,parent,r).gameObject.AddComponent<Image>();image.color=c;image.raycastTarget=false;return image; }
        internal static void Icon(string name,Transform parent,float x,float y,float size)=>Solid(name,parent,Scale(new[]{x,y,size,size}),Color.clear);
        internal static Image SpriteImage(string name,Transform parent,string file,float[] rect,float padding=0,bool button=false)
        {
            string path=Directory.GetFiles(Root+"Sprites",file+".png",SearchOption.AllDirectories).Single().Replace('\\','/');
            float[] r={rect[0]-padding,rect[1]-padding,rect[2]+padding*2,rect[3]+padding*2};
            var image=Solid(name,parent,Scale(r),Color.white); image.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(path);
            image.type=image.sprite.border==Vector4.zero?Image.Type.Simple:Image.Type.Sliced;
            if(button){image.raycastTarget=true;var b=image.gameObject.AddComponent<Button>();b.targetGraphic=image;b.transition=Selectable.Transition.None;}
            return image;
        }
        internal static Color ColorValue(string s)
        {
            var parts=s.Split('@');ColorUtility.TryParseHtmlString(parts[0],out Color c);if(parts.Length>1)c.a=float.Parse(parts[1],System.Globalization.CultureInfo.InvariantCulture);return c;
        }
        [MenuItem("Tools/Panning UI/Capture Mail Game View")]
        public static void PrepareCapture() => PrepareCaptureFor("pop-mail","mail-validation.txt",1);

        internal static void PrepareCaptureFor(string sceneKey,string reportFile,int display)
        {
            CaptureScene=sceneKey;CaptureReport=reportFile;CaptureDisplay=display;
            var type=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            GameView=EditorWindow.GetWindow(type);
            var flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
            type.GetProperty("targetDisplay",flags)?.SetValue(GameView,display);
            // A fixed Game View size controls the actual rendering resolution, independent of the editor window.
            var sizesType=type.Assembly.GetType("UnityEditor.GameViewSizes");
            var singleton=typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            object sizes=singleton.GetProperty("instance",BindingFlags.Public|BindingFlags.Static).GetValue(null);
            object group=sizesType.GetMethod("GetGroup").Invoke(sizes,new object[]{0});
            var sizeType=type.Assembly.GetType("UnityEditor.GameViewSize");
            var enumType=type.Assembly.GetType("UnityEditor.GameViewSizeType");
            var ctor=sizeType.GetConstructor(flags,null,new[]{enumType,typeof(int),typeof(int),typeof(string)},null);
            object size=ctor.Invoke(new[]{Enum.ToObject(enumType,1),(object)1200,2601,"PanningUI 1200x2601"});
            int count=(int)group.GetType().GetMethod("GetTotalCount").Invoke(group,null);
            int index=-1;
            for(int i=0;i<count;i++)
            {
                var entry=group.GetType().GetMethod("GetGameViewSize").Invoke(group,new object[]{i});
                if((int)sizeType.GetProperty("width",flags).GetValue(entry)==1200 && (int)sizeType.GetProperty("height",flags).GetValue(entry)==2601) {index=i;break;}
            }
            if(index<0){group.GetType().GetMethod("AddCustomSize").Invoke(group,new[]{size});index=count;}
            type.GetProperty("selectedSizeIndex",flags).SetValue(GameView,index);
            type.GetMethod("SetDisplayViewSize",flags)?.Invoke(GameView,new object[]{display,new Vector2(1200,2601)});
            type.GetMethod("UpdateZoomAreaAndParent",flags)?.Invoke(GameView,null);
            float fit=Mathf.Min((GameView.position.width-20)/1200f,(GameView.position.height-50)/2601f);
            type.GetMethod("SnapZoom",flags)?.Invoke(GameView,new object[]{Mathf.Clamp(fit,.1f,1f)});
            GameView.Show();GameView.Focus();GameView.Repaint();
            EditorApplication.QueuePlayerLoopUpdate();Canvas.ForceUpdateCanvases();
            CaptureAt=EditorApplication.timeSinceStartup+3;Capturing=true;
        }
        static void InspectGameView()
        {
            var type=typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            var flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
            File.WriteAllLines(Output+"gameview-api.txt",type.GetMembers(flags).Select(m=>m.ToString()));
        }
        static void Capture()
        {
            var flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
            var type=GameView.GetType();
            var rt=type.GetProperty("targetTexture",flags)?.GetValue(GameView) as RenderTexture;
            if(rt==null) rt=type.GetField("m_RenderTexture",flags)?.GetValue(GameView) as RenderTexture;
            if(rt==null) {InspectGameView();throw new Exception("Game View render texture unavailable; inspect gameview-api.txt");}
            var prev=RenderTexture.active;RenderTexture.active=rt;
            var tex=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);
            tex.ReadPixels(new UnityEngine.Rect(0,0,rt.width,rt.height),0,0);tex.Apply();RenderTexture.active=prev;
            // The D3D Game View render target is vertically flipped relative to PNG readback.
            if(SystemInfo.graphicsUVStartsAtTop)
            {
                var pixels=tex.GetPixels32(); var flipped=new Color32[pixels.Length];
                for(int y=0;y<rt.height;y++) Array.Copy(pixels,y*rt.width,flipped,(rt.height-1-y)*rt.width,rt.width);
                tex.SetPixels32(flipped);tex.Apply();
            }
            if(rt.width!=1200 || rt.height!=2601) throw new Exception("Unexpected Game View capture resolution");
            File.WriteAllBytes(Output+CaptureScene+"_game_1200.png",tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
            File.AppendAllText(Output+CaptureReport,$"Game View capture: {rt.width} x {rt.height}, Display {CaptureDisplay+1}.\n");
            Debug.Log("[PanningUI] Game View captured: "+Output+CaptureScene+"_game_1200.png");
        }
    }
}
