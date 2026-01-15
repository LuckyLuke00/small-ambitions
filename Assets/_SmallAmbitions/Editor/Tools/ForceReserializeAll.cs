using UnityEditor;
using UnityEngine;

namespace SmallAmbitions.Editor
{
    public static class ForceReserializeAll
    {
        private const string DialogTitle = "Force Reserialize All Assets";
        private const string DialogMessage = "This will reserialize all assets in the project.\n\nThis operation may take a while for large projects.";

        [MenuItem("Tools/Force Reserialize All Assets")]
        public static void Reserialize()
        {
            if (!EditorUtility.DisplayDialog(DialogTitle, DialogMessage, "Continue", "Cancel"))
            {
                return;
            }

            AssetDatabase.ForceReserializeAssets();
            AssetDatabase.Refresh();
            Debug.Log("Reserialization complete.");
        }
    }
}
