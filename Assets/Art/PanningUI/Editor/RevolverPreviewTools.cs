using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PanningUI.Editor
{
    // Only the naming migration remains; reflection preview/recording was removed by user request.
    public static class RevolverPreviewTools
    {
        const string Root="Assets/Art/PanningUI/";
        [MenuItem("Tools/Panning UI/Normalize Preview Scene Names")]
        public static void FixNames()
        {
            string[] oldNames={"PanningMailPreview","PanningNeedPreview","PanningConfirmPreview","PanningBuyPreview","PanningPowerPreview","PanningPowerSourcePreview"};
            string[] newNames={"MailboxPreview","InsufficientCurrencyPreview","ConfirmPreview","ProductDetailPreview","CombatPowerPreview","StatSourcesPreview"};
            Scene prior=SceneManager.GetActiveScene();
            var report=new System.Text.StringBuilder();
            for(int i=0;i<oldNames.Length;i++)
            {
                string oldPath=Root+"Scenes/"+oldNames[i]+".unity", newPath=Root+"Scenes/"+newNames[i]+".unity";
                string path=File.Exists(oldPath)?oldPath:newPath;
                if(!File.Exists(path))continue;
                string guid=AssetDatabase.AssetPathToGUID(path);
                Scene scene=SceneManager.GetSceneByPath(path);bool opened=!scene.IsValid()||!scene.isLoaded;
                if(opened)scene=EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
                foreach(var instance in scene.GetRootGameObjects())
                {
                    if(!PrefabUtility.IsPartOfPrefabInstance(instance))continue;
                    var source=PrefabUtility.GetCorrespondingObjectFromSource(instance);
                    if(source==null || !AssetDatabase.GetAssetPath(source).StartsWith(Root+"Prefabs/",StringComparison.Ordinal))continue;
                    var property=new SerializedObject(instance).FindProperty("m_Name");
                    if(property.prefabOverride)PrefabUtility.RevertPropertyOverride(property,InteractionMode.AutomatedAction);
                    if(instance.name!=source.name)throw new Exception("Instance name mismatch: "+instance.name);
                    report.AppendLine(instance.name+" / prefab reference preserved");
                }
                EditorSceneManager.MarkSceneDirty(scene);EditorSceneManager.SaveScene(scene);
                if(path!=newPath)
                {
                    string error=AssetDatabase.MoveAsset(path,newPath);if(!string.IsNullOrEmpty(error))throw new Exception(error);
                }
                if(AssetDatabase.AssetPathToGUID(newPath)!=guid)throw new Exception("Scene GUID changed");
                if(opened)EditorSceneManager.CloseScene(scene,true);
            }
            if(prior.IsValid()&&prior.isLoaded)SceneManager.SetActiveScene(prior);
            File.WriteAllText(Root+"Docs/Validation/scene-naming-validation.txt",report.ToString());
            Debug.Log("[PanningUI] Preview scene names and prefab instance names normalized.");
        }
    }
}
