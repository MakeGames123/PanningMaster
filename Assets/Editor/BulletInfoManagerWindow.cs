using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

public class BulletInfoManagerWindow : EditorWindow
{
    private Vector2 scrollPos;
    private List<BulletInfoSO> bulletList = new();

    private const string BULLET_FOLDER_PATH = "Assets/Resources/SO";

    [MenuItem("Tools/Bullet Info Manager")]
    public static void Open()
    {
        GetWindow<BulletInfoManagerWindow>("Bullet Info Manager");
    }

    private void OnEnable()
    {
        LoadAllBullets();
    }

    private void LoadAllBullets()
    {
        bulletList.Clear();

        string[] guids = AssetDatabase.FindAssets("t:BulletInfoSO", new[] { BULLET_FOLDER_PATH });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            BulletInfoSO so = AssetDatabase.LoadAssetAtPath<BulletInfoSO>(path);
            if (so != null)
                bulletList.Add(so);
        }

        bulletList = bulletList.OrderBy(b => b.bulletId).ToList();
    }

    private void OnGUI()
    {
        if (GUILayout.Button("Reload"))
        {
            LoadAllBullets();
        }

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        int columnCount = 4; // 한 줄에 몇 개 표시할지
        int count = 0;

        EditorGUILayout.BeginVertical();

        while (count < bulletList.Count)
        {
            EditorGUILayout.BeginHorizontal();

            for (int i = 0; i < columnCount && count < bulletList.Count; i++)
            {
                DrawBulletBox(bulletList[count]);
                count++;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();
    }

    private void DrawBulletBox(BulletInfoSO bullet)
    {
        EditorGUILayout.BeginVertical("box", GUILayout.Width(220));

        EditorGUI.BeginChangeCheck();

        bullet.inventoryImage = (Sprite)EditorGUILayout.ObjectField(bullet.inventoryImage, typeof(Sprite), false, GUILayout.Width(64), GUILayout.Height(64));
        bullet.bulletName = EditorGUILayout.TextField("Name", bullet.bulletName);
        bullet.bulletType = (BulletType)EditorGUILayout.EnumPopup("Type", bullet.bulletType);
        bullet.tier = EditorGUILayout.IntField("Tier", bullet.tier);
        bullet.bulletId = EditorGUILayout.IntField("ID", bullet.bulletId);
        bullet.particleID = EditorGUILayout.IntField("Particle", bullet.particleID);

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(bullet);
        }

        EditorGUILayout.EndVertical();
    }
}