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
    private static readonly string[] tabs = { "Targets", "History", PhilSorterLocalization.Get("settings") };
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
        else if (selectedTab == 1) // History
        {
            DrawHistoryUI();
        }
        else if (selectedTab == 2) // Settings
        {
            DrawSettingsUI();
        }
    }

    private void DrawTargetFoldersUI()
    {
        EditorGUILayout.LabelField(PhilSorterLocalization.Get("target_folders_configurable", "Target Folders (Configurable):"), EditorStyles.boldLabel);
        search = EditorGUILayout.TextField(PhilSorterLocalization.Get("search"), search);
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
                // Editable Display Name
                string newName = EditorGUILayout.TextField(folder.displayName, GUILayout.Width(120));
                if (newName != folder.displayName)
                {
                    folder.displayName = newName;
                    EditorUtility.SetDirty(config);
                    AssetDatabase.SaveAssets();
                }
                EditorGUILayout.LabelField(folder.path, EditorStyles.miniLabel);
                if (GUILayout.Button(PhilSorterLocalization.Get("remove", "Remove"), GUILayout.Width(60)))
                {
                    config.targetFolders.Remove(folder);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();
        EditorGUILayout.LabelField(PhilSorterLocalization.Get("add_new_target_folder", "Add New Target Folder:"), EditorStyles.boldLabel);
        newDisplayName = EditorGUILayout.TextField(PhilSorterLocalization.Get("display_name", "Display Name"), newDisplayName);
        // Category dropdown
        if (categories.Count == 0) categories.Add("Default");
        selectedCategoryIndex = Mathf.Clamp(selectedCategoryIndex, 0, categories.Count - 1);
        selectedCategoryIndex = EditorGUILayout.Popup("Category", selectedCategoryIndex, categories.ToArray());
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button(PhilSorterLocalization.Get("add_selected_folder")))
        {
            AddSelectedFolder();
        }
        if (GUILayout.Button(PhilSorterLocalization.Get("manage_categories"), GUILayout.Width(140)))
        {
            CategoryManagerWindow.ShowWindow(this);
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.HelpBox(PhilSorterLocalization.Get("tip_add_selected_folder", "Tip: Select one or more folders in the Project window (multi-select supported), then click 'Add Selected Folder as Target'."), MessageType.Info);
        if (GUILayout.Button(PhilSorterLocalization.Get("save_config")))
        {
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    // --- History Tab ---
    private void DrawHistoryUI()
    {
        EditorGUILayout.LabelField(PhilSorterLocalization.Get("action_history", "Action History:"), EditorStyles.boldLabel);
        if (config.history == null || config.history.Count == 0)
        {
            EditorGUILayout.LabelField(PhilSorterLocalization.Get("no_history_yet", "No history yet."));
            return;
        }
        EditorGUILayout.Space();
        foreach (var entry in config.history.OrderByDescending(e => e.timestamp))
        {
            EditorGUILayout.BeginHorizontal();
            string label = $"[{entry.timestamp}] {entry.action}: {entry.path}";
            if (!string.IsNullOrEmpty(entry.extra))
                label += $" → {entry.extra}";
            EditorGUILayout.LabelField(label, EditorStyles.miniLabel);
            if (GUILayout.Button(PhilSorterLocalization.Get("jump", "Jump"), GUILayout.Width(50)))
            {
                string jumpPath = entry.action == "Move" && !string.IsNullOrEmpty(entry.extra) ? entry.extra : entry.path;
                if (!string.IsNullOrEmpty(jumpPath))
                {
                    var obj = AssetDatabase.LoadAssetAtPath<Object>(jumpPath);
                    if (obj != null)
                        Selection.activeObject = obj;
                    else
                        EditorUtility.DisplayDialog(PhilSorterLocalization.Get("jump_failed_title", "Jump Failed"), PhilSorterLocalization.Get("jump_failed", jumpPath), PhilSorterLocalization.Get("ok"));
                }
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
        EditorGUILayout.LabelField(PhilSorterLocalization.Get("settings_title", "Phil's Sorter Settings"), titleStyle, GUILayout.Height(32));
        GUILayout.Space(8);
        // Language selection
        int langIdx = System.Array.IndexOf(PhilSorterLocalization.SupportedLanguages, PhilSorterLocalization.CurrentLanguage);
        int newLangIdx = EditorGUILayout.Popup(PhilSorterLocalization.Get("language", "Language"), langIdx >= 0 ? langIdx : 0, PhilSorterLocalization.SupportedLanguageLabels);
        if (newLangIdx != langIdx)
        {
            PhilSorterLocalization.SetLanguage(PhilSorterLocalization.SupportedLanguages[newLangIdx]);
        }


        // General Section
        EditorGUILayout.BeginVertical(sectionStyle);
        EditorGUILayout.LabelField("⚙️  " + PhilSorterLocalization.Get("general"), headerStyle);
        EditorGUI.BeginChangeCheck();
        float oldLabelWidth = EditorGUIUtility.labelWidth;
        EditorGUIUtility.labelWidth = 260f;

        config.jumpToNewFolder = EditorGUILayout.ToggleLeft(PhilSorterLocalization.Get("jump_to_new_folder", "Jump to new folder after move"), config.jumpToNewFolder);
        EditorGUILayout.LabelField(PhilSorterLocalization.Get("auto_select_after_move", "Automatically select the folder after moving."), descStyle);
        config.confirmBeforeMove = EditorGUILayout.ToggleLeft(PhilSorterLocalization.Get("always_confirm_before_move", "Always confirm before moving folders"), config.confirmBeforeMove);
        EditorGUILayout.LabelField(PhilSorterLocalization.Get("show_confirm_dialog", "Show a confirmation dialog before every move."), descStyle);
        config.showScriptWarnings = EditorGUILayout.ToggleLeft(PhilSorterLocalization.Get("warn_if_scripts", "Warn if folder contains scripts"), config.showScriptWarnings);
        EditorGUILayout.LabelField(PhilSorterLocalization.Get("extra_warning_scripts", "Extra warning if scripts are present."), descStyle);
        config.showPatchDialog = EditorGUILayout.ToggleLeft(PhilSorterLocalization.Get("show_patch_dialog", "Show patch dialog for hardcoded paths"), config.showPatchDialog);
        EditorGUILayout.LabelField(PhilSorterLocalization.Get("offer_patch_hardcoded", "Offer to patch scripts with hardcoded paths."), descStyle);
        config.showRecentTargets = EditorGUILayout.ToggleLeft(PhilSorterLocalization.Get("show_recent_targets", "Show recent targets in move menu"), config.showRecentTargets);
        EditorGUILayout.LabelField(PhilSorterLocalization.Get("display_recent_targets", "Display recently used targets."), descStyle);
        EditorGUIUtility.labelWidth = oldLabelWidth;
        EditorGUILayout.EndVertical();

        // Advanced Section
        EditorGUILayout.BeginVertical(sectionStyle);
        EditorGUILayout.LabelField("🛠️  " + PhilSorterLocalization.Get("advanced"), headerStyle);
        EditorGUIUtility.labelWidth = 260f;
        config.showDebugLogs = EditorGUILayout.ToggleLeft(PhilSorterLocalization.Get("show_debug_logs", "Show debug logs in console"), config.showDebugLogs);
        EditorGUILayout.LabelField(PhilSorterLocalization.Get("enable_extra_debug", "Enable extra debug output."), descStyle);
        config.enableExperimental = EditorGUILayout.ToggleLeft(PhilSorterLocalization.Get("enable_experimental", "Enable experimental features"), config.enableExperimental);
        EditorGUILayout.LabelField(PhilSorterLocalization.Get("try_new_features", "Try new features before release."), descStyle);
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
        EditorGUILayout.LabelField("♥  " + PhilSorterLocalization.Get("credits"), headerStyle);
        GUILayout.Space(2);
        EditorGUILayout.LabelField("Phil's Asset Sorter", new GUIStyle(EditorStyles.boldLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 13 });
        EditorGUILayout.LabelField("by Philuu", new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 12 });
        EditorGUILayout.LabelField("Special thanks to the VRChat and Unity communities!", new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, fontSize = 11 });
        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button(PhilSorterLocalization.Get("support_me"), GUILayout.Width(180), GUILayout.Height(32)))
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
        // Look for existing config anywhere in the project
        string[] guids = AssetDatabase.FindAssets("t:SorterConfig");
        if (guids != null && guids.Length > 0)
        {
            string foundPath = AssetDatabase.GUIDToAssetPath(guids[0]);
            config = AssetDatabase.LoadAssetAtPath<SorterConfig>(foundPath);
        }
        else
        {
            // Create new config next to this script (not hardcoded)
            string scriptPath = AssetDatabase.GetAssetPath(MonoScript.FromScriptableObject(this));
            string dir = Path.GetDirectoryName(scriptPath).Replace("\\", "/");
            string configPath = dir + "/PhilSorterConfig.asset";
            config = ScriptableObject.CreateInstance<SorterConfig>();
            AssetDatabase.CreateAsset(config, configPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }

    private void AddSelectedFolder()
    {
        Object[] selectedObjects = Selection.objects;
        if (selectedObjects == null || selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog(PhilSorterLocalization.Get("no_selection_title", "No Selection"), PhilSorterLocalization.Get("no_selection"), PhilSorterLocalization.Get("ok"));
            if (config != null && config.showDebugLogs) Debug.LogWarning("[Phil's Sorter] AddSelectedFolder: No selection");
            return;
        }

        List<string> added = new List<string>();
        List<string> already = new List<string>();
        List<string> invalid = new List<string>();
        string category = (categories.Count > selectedCategoryIndex && selectedCategoryIndex >= 0) ? categories[selectedCategoryIndex] : "Default";

        foreach (var obj in selectedObjects)
        {
            if (obj == null) continue;
            string path = AssetDatabase.GetAssetPath(obj);
            if (!AssetDatabase.IsValidFolder(path))
            {
                invalid.Add(path);
                if (config != null && config.showDebugLogs) Debug.LogWarning($"[Phil's Sorter] AddSelectedFolder: Not a valid folder: {path}");
                continue;
            }
            if (config.targetFolders.Any(f => f.path == path))
            {
                already.Add(path);
                if (config != null && config.showDebugLogs) Debug.LogWarning($"[Phil's Sorter] AddSelectedFolder: Already in targets: {path}");
                continue;
            }
            string displayName = string.IsNullOrEmpty(newDisplayName) || selectedObjects.Length > 1 ? Path.GetFileName(path) : newDisplayName;
            config.targetFolders.Add(new TargetFolder
            {
                path = path,
                displayName = displayName,
                category = category
            });
            added.Add(path);
            if (config != null && config.showDebugLogs) Debug.Log($"[Phil's Sorter] Added to targets: {path} (category: {category})");

            // Log to history
            if (config.history == null) config.history = new List<SorterConfig.HistoryEntry>();
            config.history.Add(new SorterConfig.HistoryEntry {
                action = "SetTarget",
                path = path,
                extra = "",
                timestamp = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
            });
        }

        if (added.Count > 0)
        {
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }


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
            EditorGUILayout.LabelField(PhilSorterLocalization.Get("category_manager", "Category Manager"), EditorStyles.boldLabel, GUILayout.Height(22));
            EditorGUILayout.LabelField(PhilSorterLocalization.Get("drag_to_reorder", "Drag to reorder, or click Remove to delete."), EditorStyles.miniLabel);
            GUILayout.Space(8);
            // Default category (label, not text field)
            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(8);
            EditorGUILayout.LabelField(PhilSorterLocalization.Get("default_category", "Default"), EditorStyles.boldLabel, GUILayout.Width(180));
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
            EditorGUILayout.LabelField(PhilSorterLocalization.Get("new_category", "New Category:"), GUILayout.Width(100));
            newCat = EditorGUILayout.TextField(newCat, GUILayout.Width(140));
            if (GUILayout.Button(PhilSorterLocalization.Get("add", "Add"), GUILayout.Width(60)))
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
            if (GUILayout.Button(PhilSorterLocalization.Get("close", "Close"), GUILayout.Width(80), GUILayout.Height(26)))
            {
                Close();
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
