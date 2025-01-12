using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace PluginMaster
{
    public static partial class Shortcuts
    {

        #region Public Methods

        #if UNITY_2019_1_OR_NEWER
        public static void UpdateTooltipShortcut( GUIContent button, string tooltip, string shortcutId )
        {
            string shortcut = ShortcutManager.instance.GetShortcutBinding( shortcutId ).ToString();
            if ( shortcut != string.Empty )
            {
                button.tooltip = tooltip + " ... " + shortcut;
            }
        }
        #endif

        #endregion

    }
}