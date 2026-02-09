using UnityEngine;
using UnityEditor;
using System.IO;

public class CardSpriteSetup : EditorWindow
{
    [MenuItem("Tools/Poker/Setup Card Sprites")]
    public static void ShowWindow()
    {
        GetWindow<CardSpriteSetup>("Card Sprite Setup");
    }

    void OnGUI()
    {
        GUILayout.Label("Card Sprite Setup", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "This tool copies card sprites from Assets/Sprites/Cards to Assets/Resources/Sprites/Cards " +
            "so they can be loaded at runtime using Resources.Load().",
            MessageType.Info);

        GUILayout.Space(10);

        if (GUILayout.Button("Copy Sprites to Resources Folder", GUILayout.Height(40)))
        {
            CopySpritesToResources();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Reload Card Library", GUILayout.Height(30)))
        {
            CardSpriteLibrary.Reload();
            Debug.Log("Card Library reloaded!");
        }
    }

    static void CopySpritesToResources()
    {
        string sourcePath = "Assets/Sprites/Cards";
        string destPath = "Assets/Resources/Sprites/Cards";

        if (!AssetDatabase.IsValidFolder(sourcePath))
        {
            Debug.LogError($"Source folder not found: {sourcePath}");
            return;
        }

        // Create destination folder if needed
        EnsureFolderExists(destPath);

        // Find all sprites in source
        var guids = AssetDatabase.FindAssets("t:Sprite", new[] { sourcePath });
        int copied = 0;

        foreach (var guid in guids)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var fileName = Path.GetFileName(assetPath);
            var destAssetPath = Path.Combine(destPath, fileName).Replace("\\", "/");

            // Check if already exists
            if (!File.Exists(destAssetPath))
            {
                AssetDatabase.CopyAsset(assetPath, destAssetPath);
                copied++;
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"Copied {copied} sprites to {destPath}. Total sprites in source: {guids.Length}");

        // Reload the library
        CardSpriteLibrary.Reload();
    }

    static void EnsureFolderExists(string path)
    {
        var parts = path.Split('/');
        var current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }
}
