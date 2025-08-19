using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.IO;
using System.Collections.Generic;
using System.Linq;

// For referencing TargetFolder type
using TargetFolder = SorterConfig.TargetFolder;

public class PhilSorterWindow : EditorWindow
{
    public SorterConfig config;
    private Vector2 scroll;
    private string newDisplayName = "";
    private string search = "";
    private int selectedTab = 0;
    private int selectedCategoryIndex = 0;
    private static readonly string[] tabs = { "Targets", "Recent", "Settings" };
    public List<string> categories = new List<string> { "Default" };
    private Object lastSelection = null;

    [MenuItem("Window/Phil's Sorter (Config)")]
    public static void ShowWindow()
    {
        GetWindow<PhilSorterWindow>("Phil's Sorter");
    }

    private void OnEnable()
    {
        LoadConfig();
        if (config != null && config.showDebugLogs) Debug.Log("[Phil's Sorter] PhilSorterWindow enabled");
        LoadCategoriesFromTargets();
    }

    private void OnGUI()
    {
        if (config == null)
        {
            EditorGUILayout.HelpBox("SorterConfig not found. Click to create one.", MessageType.Warning);
            if (GUILayout.Button("Create SorterConfig"))
            {
                if (config != null && config.showDebugLogs) Debug.Log("[Phil's Sorter] Creating new SorterConfig from OnGUI");
                LoadConfig();
            }
            return;
        }

        // Prefill Display Name if selection changed and user hasn't typed a custom name
        Object currentSelection = Selection.activeObject;
        if (currentSelection != lastSelection)
        {
            string path = currentSelection != null ? AssetDatabase.GetAssetPath(currentSelection) : null;
            if (!string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path))
            {
                // Only prefill if newDisplayName is empty or matches the previous folder name
                string folderName = Path.GetFileName(path);
                if (string.IsNullOrEmpty(newDisplayName) || (lastSelection != null && newDisplayName == Path.GetFileName(AssetDatabase.GetAssetPath(lastSelection))))
                {
                    newDisplayName = folderName;
                }
            }
            lastSelection = currentSelection;
        }

        // Always keep categories in sync
        LoadCategoriesFromTargets();

        selectedTab = GUILayout.Toolbar(selectedTab, tabs);
        EditorGUILayout.Space();

        if (selectedTab == 0) // Targets
        {
            DrawTargetFoldersUI();
        }
        else if (selectedTab == 1) // Recent
        {
            DrawRecentUI();
        }
        else if (selectedTab == 2) // Settings
        {
            DrawSettingsUI();
        }
    }

    private void DrawTargetFoldersUI()
    {
        EditorGUILayout.LabelField("Target Folders (Configurable):", EditorStyles.boldLabel);
        search = EditorGUILayout.TextField("Search", search);
        scroll = EditorGUILayout.BeginScrollView(scroll);

        var filtered = config.targetFolders
            .Where(f => string.IsNullOrEmpty(search) || (f.displayName != null && f.displayName.ToLower().Contains(search.ToLower())) || (f.path != null && f.path.ToLower().Contains(search.ToLower())))
            .ToList();

        // Show folders grouped and ordered by user category order
        foreach (var cat in categories)
        {
            var catFolders = filtered.Where(f => (string.IsNullOrEmpty(f.category) ? "Default" : f.category) == cat)
                                     .OrderBy(f => f.displayName)
                                     .ToList();
            if (catFolders.Count == 0) continue;
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(cat, EditorStyles.miniBoldLabel);
            foreach (var folder in catFolders)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button(EditorGUIUtility.IconContent("Folder Icon"), GUILayout.Width(24), GUILayout.Height(18)))
                {
                    Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(folder.path);
                }
                EditorGUILayout.LabelField(folder.displayName, GUILayout.Width(120));
                EditorGUILayout.LabelField(folder.path, EditorStyles.miniLabel);
                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    config.targetFolders.Remove(folder);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Add New Target Folder:", EditorStyles.boldLabel);
        newDisplayName = EditorGUILayout.TextField("Display Name", newDisplayName);
        // Category dropdown
        if (categories.Count == 0) categories.Add("Default");
        selectedCategoryIndex = Mathf.Clamp(selectedCategoryIndex, 0, categories.Count - 1);
        selectedCategoryIndex = EditorGUILayout.Popup("Category", selectedCategoryIndex, categories.ToArray());
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Selected Folder as Target"))
        {
            AddSelectedFolder();
        }
        if (GUILayout.Button("Manage Categories", GUILayout.Width(140)))
        {
            CategoryManagerWindow.ShowWindow(this);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.HelpBox("Tip: Select a folder in the Project window, then click 'Add Selected Folder as Target'.", MessageType.Info);
        if (GUILayout.Button("Save Config"))
        {
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private void DrawRecentUI()
    {
        EditorGUILayout.LabelField("Recently Used Targets:", EditorStyles.boldLabel);
        if (config.recentTargets == null || config.recentTargets.Count == 0)
        {
            EditorGUILayout.LabelField("No recent targets.");
            return;
        }
        foreach (var path in config.recentTargets ?? new List<string>())
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(path, EditorStyles.miniLabel);
            if (GUILayout.Button("Select", GUILayout.Width(60)))
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<Object>(path);
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    // Favorites feature removed

    private void DrawSettingsUI()
    {
        // --- Sleek Modern Settings UI (Unity rich text is limited, so use layout, icons, and font styles) ---
        GUILayout.Space(10);
        GUIStyle titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
        GUIStyle sectionStyle = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(16, 16, 12, 12), margin = new RectOffset(0,0,0,8) };
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 14 };
        GUIStyle descStyle = new GUIStyle(EditorStyles.miniLabel) { fontSize = 11, normal = { textColor = new Color(0.7f,0.7f,0.7f) } };
        Color origColor = GUI.backgroundColor;

        // Title
        EditorGUILayout.LabelField("Phil's Sorter Settings", titleStyle, GUILayout.Height(32));
        GUILayout.Space(8);

        // General Section
        EditorGUILayout.BeginVertical(sectionStyle);
        EditorGUILayout.LabelField("⚙️  General", headerStyle);
        EditorGUI.BeginChangeCheck();
        float oldLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 260f;

        config.jumpToNewFolder = EditorGUILayout.ToggleLeft("Jump to new folder after move", config.jumpToNewFolder);
        EditorGUILayout.LabelField("Automatically select the folder after moving.", descStyle);
        config.confirmBeforeMove = EditorGUILayout.ToggleLeft("Always confirm before moving folders", config.confirmBeforeMove);
        EditorGUILayout.LabelField("Show a confirmation dialog before every move.", descStyle);
        config.showScriptWarnings = EditorGUILayout.ToggleLeft("Warn if folder contains scripts", config.showScriptWarnings);
        EditorGUILayout.LabelField("Extra warning if scripts are present.", descStyle);
        config.showPatchDialog = EditorGUILayout.ToggleLeft("Show patch dialog for hardcoded paths", config.showPatchDialog);
        EditorGUILayout.LabelField("Offer to patch scripts with hardcoded paths.", descStyle);
        config.showRecentTargets = EditorGUILayout.ToggleLeft("Show recent targets in move menu", config.showRecentTargets);
        EditorGUILayout.LabelField("Display recently used targets.", descStyle);
        EditorGUIUtility.labelWidth = oldLabelWidth;
        EditorGUILayout.EndVertical();

        // Advanced Section
        EditorGUILayout.BeginVertical(sectionStyle);
        EditorGUILayout.LabelField("🛠️  Advanced", headerStyle);
        EditorGUIUtility.labelWidth = 260f;
        config.showDebugLogs = EditorGUILayout.ToggleLeft("Show debug logs in console", config.showDebugLogs);
        EditorGUILayout.LabelField("Enable extra debug output.", descStyle);
        config.enableExperimental = EditorGUILayout.ToggleLeft("Enable experimental features", config.enableExperimental);
        EditorGUILayout.LabelField("Try new features before release.", descStyle);
        EditorGUIUtility.labelWidth = oldLabelWidth;
        EditorGUILayout.EndVertical();
        GUI.backgroundColor = origColor;

        if (EditorGUI.EndChangeCheck())
        {
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }

        // Credits Section
        GUILayout.Space(8);
        GUI.backgroundColor = new Color(0.13f,0.18f,0.22f,0.10f);
        EditorGUILayout.BeginVertical(sectionStyle);
        EditorGUILayout.LabelField("♥  Credits", headerStyle);
        GUILayout.Space(2);
        EditorGUILayout.LabelField("Phil's Asset Sorter", new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 13 });
        EditorGUILayout.LabelField("by Philuu", new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 12 });
        EditorGUILayout.LabelField("Special thanks to the VRChat and Unity communities!", new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 11 });
        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Support me on Ko-fi", GUILayout.Width(180), GUILayout.Height(32)))
        {
            Application.OpenURL("https://ko-fi.com/philuu");
        }
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();
        GUI.backgroundColor = origColor;
    }

    private void LoadConfig()
    {
        // Find config next to this script, not in root, and never hardcode path
        string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
        string dir = Path.GetDirectoryName(scriptPath).Replace("\\", "/");
        string configPath = dir + "/PhilSorterConfig.asset";
        config = AssetDatabase.LoadAssetAtPath<SorterConfig>(configPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<SorterConfig>();
            AssetDatabase.CreateAsset(config, configPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private void AddSelectedFolder()
    {
        Object selected = Selection.activeObject;
        if (selected == null)
        {
            Debug.LogWarning("No object selected in the Project window.");
            if (config != null && config.showDebugLogs) Debug.LogWarning("[Phil's Sorter] AddSelectedFolder: No selection");
            return;
        }
        string path = AssetDatabase.GetAssetPath(selected);
        if (!AssetDatabase.IsValidFolder(path))
        {
            Debug.LogWarning("Selected object is not a valid folder: " + path);
            if (config != null && config.showDebugLogs) Debug.LogWarning($"[Phil's Sorter] AddSelectedFolder: Not a valid folder: {path}");
            return;
        }
        if (config.targetFolders.Any(f => f.path == path))
        {
            Debug.LogWarning("Folder already in targets: " + path);
            if (config != null && config.showDebugLogs) Debug.LogWarning($"[Phil's Sorter] AddSelectedFolder: Already in targets: {path}");
            return;
        }
        string category = (categories.Count > selectedCategoryIndex && selectedCategoryIndex >= 0) ? categories[selectedCategoryIndex] : "Default";
        config.targetFolders.Add(new TargetFolder
        {
            path = path,
            displayName = string.IsNullOrEmpty(newDisplayName) ? Path.GetFileName(path) : newDisplayName,
            category = category
        });
        EditorUtility.SetDirty(config);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        if (config != null && config.showDebugLogs) Debug.Log($"[Phil's Sorter] Added to targets: {path} (category: {category})");
        Debug.Log("Added to Phil's Sorter targets: " + path);
        newDisplayName = "";
    }

    // Loads categories in user order: Default, then customCategories, then any from targetFolders not present
    public void LoadCategoriesFromTargets()
    {
        categories = new List<string>();
        categories.Add("Default");
        if (config.customCategories != null)
        {
            foreach (var cat in config.customCategories)
                if (!string.IsNullOrWhiteSpace(cat) && cat != "Default" && !categories.Contains(cat))
                    categories.Add(cat);
        }
        // Add any categories from targetFolders not already present
        foreach (var cat in config.targetFolders.Select(f => string.IsNullOrEmpty(f.category) ? "Default" : f.category))
        {
            if (!categories.Contains(cat))
                categories.Add(cat);
        }
        if (config != null && config.showDebugLogs) Debug.Log($"[Phil's Sorter] Loaded categories: {string.Join(", ", categories)}");
    }

    // Category manager window with robust drag-and-drop using ReorderableList
    public class CategoryManagerWindow : EditorWindow
    {
        private static PhilSorterWindow parentWindow;
        private string newCat = "";
        private ReorderableList reorderableList;
        public static void ShowWindow(PhilSorterWindow parent)
        {
            parentWindow = parent;
            var win = GetWindow<CategoryManagerWindow>(true, "Manage Categories");
            win.minSize = new Vector2(300, 350);
        }
        // For editing category names in-place
        private int editingIndex = -1;
        private string editingValue = "";
        private void OnEnable()
        {
            reorderableList = new ReorderableList(parentWindow.config.customCategories, typeof(string), true, false, false, false);
            reorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                float textWidth = rect.width - 80;
                // Inline editing
                if (editingIndex == index)
                {
                    GUI.SetNextControlName("CategoryEditField");
                    editingValue = EditorGUI.TextField(new Rect(rect.x + 8, rect.y + 2, textWidth, EditorGUIUtility.singleLineHeight), editingValue);
                    // Commit on enter or focus loss
                    Event e = Event.current;
                    bool commit = (e.type == EventType.KeyDown && e.keyCode == KeyCode.Return) || (e.type == EventType.MouseDown && !new Rect(rect.x + 8, rect.y + 2, textWidth, EditorGUIUtility.singleLineHeight).Contains(e.mousePosition));
                    if (commit)
                    {
                        string oldName = parentWindow.config.customCategories[index];
                        string newName = editingValue.Trim();
                        if (!string.IsNullOrEmpty(newName) && newName != "Default" && !parentWindow.categories.Contains(newName))
                        {
                            // Update all targetFolders using this category
                            foreach (var tf in parentWindow.config.targetFolders)
                                if (tf.category == oldName) tf.category = newName;
                            parentWindow.config.customCategories[index] = newName;
                            EditorUtility.SetDirty(parentWindow.config);
                            AssetDatabase.SaveAssets();
                            parentWindow.LoadCategoriesFromTargets();
                        }
                        editingIndex = -1;
                        editingValue = "";
                        GUI.FocusControl(null);
                        Event.current.Use();
                    }
                }
                else
                {
                    // Draw as label, click to edit
                    if (GUI.Button(new Rect(rect.x + 8, rect.y + 2, textWidth, EditorGUIUtility.singleLineHeight), parentWindow.config.customCategories[index], EditorStyles.label))
                    {
                        editingIndex = index;
                        editingValue = parentWindow.config.customCategories[index];
                        GUI.FocusControl("CategoryEditField");
                    }
                }
                if (GUI.Button(new Rect(rect.x + rect.width - 65, rect.y + 2, 56, EditorGUIUtility.singleLineHeight), "Remove"))
                {
                    string cat = parentWindow.config.customCategories[index];
                    foreach (var tf in parentWindow.config.targetFolders)
                    {
                        if (tf.category == cat)
                            tf.category = "Default";
                    }
                    parentWindow.config.customCategories.RemoveAt(index);
                    EditorUtility.SetDirty(parentWindow.config);
                    AssetDatabase.SaveAssets();
                    parentWindow.LoadCategoriesFromTargets();
                    if (editingIndex == index) { editingIndex = -1; editingValue = ""; }
                }
            };
            reorderableList.elementHeight = EditorGUIUtility.singleLineHeight + 6f;
            reorderableList.drawHeaderCallback = (Rect rect) => {
                EditorGUI.LabelField(rect, "Custom Categories", EditorStyles.boldLabel);
            };
            reorderableList.onReorderCallback = (ReorderableList list) =>
            {
                EditorUtility.SetDirty(parentWindow.config);
                AssetDatabase.SaveAssets();
                parentWindow.LoadCategoriesFromTargets();
            };
        }
        private void OnGUI()
        {
            GUILayout.Space(6);
            EditorGUILayout.LabelField("Category Manager", EditorStyles.boldLabel, GUILayout.Height(22));
            EditorGUILayout.LabelField("Drag to reorder, or click Remove to delete.", EditorStyles.miniLabel);
            GUILayout.Space(8);
            // Default category (label, not text field)
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);
            EditorGUILayout.LabelField("Default", EditorStyles.boldLabel, GUILayout.Width(180));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2);
            // ReorderableList, constrained width
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);
            EditorGUILayout.BeginVertical(GUILayout.Width(320));
            if (reorderableList == null)
                OnEnable();
            reorderableList.DoLayoutList();
            EditorGUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            // Add new category
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);
            EditorGUILayout.LabelField("New Category:", GUILayout.Width(100));
            newCat = EditorGUILayout.TextField(newCat, GUILayout.Width(140));
            if (GUILayout.Button("Add", GUILayout.Width(60)))
            {
                if (!string.IsNullOrWhiteSpace(newCat) && !parentWindow.categories.Contains(newCat))
                {
                    if (parentWindow.config.customCategories == null)
                        parentWindow.config.customCategories = new List<string>();
                    parentWindow.config.customCategories.Add(newCat);
                    EditorUtility.SetDirty(parentWindow.config);
                    AssetDatabase.SaveAssets();
                    parentWindow.LoadCategoriesFromTargets();
                    newCat = "";
                    
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space();
            // Close button
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Width(80), GUILayout.Height(26)))
            {
                Close();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
