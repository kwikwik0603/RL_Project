#if UNITY_EDITOR
using NUnit.Framework.Interfaces;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

public class GameDataBaseWindow : OdinMenuEditorWindow
{
    // Adds a clickable button to your top Unity menu layout to launch the window
    [MenuItem("Tools/Game Database")]
    private static void OpenWindow()
    {
        // 1. Instantiate the window state object
        var window = GetWindow<GameDataBaseWindow>();

        // 2. Define your desired layout proportions (Width, Height)
        Vector2 windowSize = new Vector2(800, 500);

        // 3. Center the window on the developer's main monitor layout using standard Rect calculations
        // (Bypasses the missing helper function completely!)
        Rect centerRect = new Rect(
            (Screen.currentResolution.width - windowSize.x) * 0.5f,
            (Screen.currentResolution.height - windowSize.y) * 0.5f,
            windowSize.x,
            windowSize.y
        );

        window.position = centerRect;
        window.titleContent = new GUIContent("Database");

        // 4. Force display to screen
        window.Show();
    }

    // Odin automatically handles scanning your asset folders and building the side-menu list!
    protected override OdinMenuTree BuildMenuTree()
    {
        var tree = new OdinMenuTree();

        // This scans your entire project directory and pulls in every asset file 
        // derived from your custom ScriptableObject templates automatically
        tree.AddAllAssetsAtPath("Player", "Assets/Scripts/ScriptableObjects", typeof(PlayerData), true);
        tree.AddAllAssetsAtPath("Attacks", "Assets/Scripts/ScriptableObjects", typeof(AttackData), true);
        tree.AddAllAssetsAtPath("Boss", "Assets/Scripts/ScriptableObjects", typeof(BossData), true);

        // Sorts the list alphabetically automatically
        tree.SortMenuItemsByName();

        return tree;
    }
}
#endif