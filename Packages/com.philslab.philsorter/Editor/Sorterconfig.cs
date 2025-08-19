using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "SorterConfig", menuName = "Phil's Sorter/Config")]
public class SorterConfig : ScriptableObject
{
    // --- User Settings ---
    public bool jumpToNewFolder = true;
    public bool confirmBeforeMove = true;
    public bool showScriptWarnings = true;
    public bool showPatchDialog = true;
    public bool showRecentTargets = true;
    public bool showDebugLogs = false;
    public bool enableExperimental = false;
 
    public List<TargetFolder> targetFolders = new List<TargetFolder>();
    public List<string> recentTargets = new List<string>();
    public List<string> customCategories = new List<string>();

    [System.Serializable]
    public class TargetFolder
    {
        public string path;
        public string displayName;
        public string category;
    }
}
