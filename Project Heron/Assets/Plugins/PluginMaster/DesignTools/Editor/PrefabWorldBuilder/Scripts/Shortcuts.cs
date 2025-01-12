#if UNITY_2019_1_OR_NEWER
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace PluginMaster
{
    public static partial class Shortcuts
    {

        #region TOGGLE TOOLS

        public const string PWB_TOGGLE_PIN_SHORTCUT_ID = "Prefab World Builder/Tools - Toggle Pin Tool";

        [Shortcut(          PWB_TOGGLE_PIN_SHORTCUT_ID,
            KeyCode.Alpha1, ShortcutModifiers.Shift
                            | ShortcutModifiers.Alt )]
        private static void TogglePin() => PWBIO.ToogleTool( ToolManager.PaintTool.PIN );

        public const string PWB_TOGGLE_BRUSH_SHORTCUT_ID = "Prefab World Builder/Tools - Toggle Brush Tool";

        [Shortcut( PWB_TOGGLE_BRUSH_SHORTCUT_ID, KeyCode.Alpha2,
            ShortcutModifiers.Shift | ShortcutModifiers.Alt )]
        private static void ToggleBrush() => PWBIO.ToogleTool( ToolManager.PaintTool.BRUSH );

        public const string PWB_TOGGLE_GRAVITY_SHORTCUT_ID = "Prefab World Builder/Tools - Toggle Gravity Tool";

        [Shortcut( PWB_TOGGLE_GRAVITY_SHORTCUT_ID, KeyCode.Alpha3,
            ShortcutModifiers.Shift | ShortcutModifiers.Alt )]
        private static void ToggleGravity() => PWBIO.ToogleTool( ToolManager.PaintTool.GRAVITY );

        public const string PWB_TOGGLE_LINE_SHORTCUT_ID = "Prefab World Builder/Tools - Toggle Line Tool";

        [Shortcut( PWB_TOGGLE_LINE_SHORTCUT_ID, KeyCode.Alpha4,
            ShortcutModifiers.Shift | ShortcutModifiers.Alt )]
        private static void ToggleLine() => PWBIO.ToogleTool( ToolManager.PaintTool.LINE );

        public const string PWB_TOGGLE_SHAPE_SHORTCUT_ID = "Prefab World Builder/Tools - Toggle Shape Tool";

        [Shortcut( PWB_TOGGLE_SHAPE_SHORTCUT_ID, KeyCode.Alpha5,
            ShortcutModifiers.Shift | ShortcutModifiers.Alt )]
        private static void ToggleShape() => PWBIO.ToogleTool( ToolManager.PaintTool.SHAPE );

        public const string PWB_TOGGLE_TILING_SHORTCUT_ID = "Prefab World Builder/Tools - Toggle Tiling Tool";

        [Shortcut( PWB_TOGGLE_TILING_SHORTCUT_ID, KeyCode.Alpha6,
            ShortcutModifiers.Shift | ShortcutModifiers.Alt )]
        private static void ToggleTiling() => PWBIO.ToogleTool( ToolManager.PaintTool.TILING );

        public const string PWB_TOGGLE_REPLACER_SHORTCUT_ID = "Prefab World Builder/Tools - Toggle Replacer Tool";

        [Shortcut( PWB_TOGGLE_REPLACER_SHORTCUT_ID, KeyCode.Alpha7,
            ShortcutModifiers.Shift | ShortcutModifiers.Alt )]
        private static void ToggleReplacer() => PWBIO.ToogleTool( ToolManager.PaintTool.REPLACER );

        public const string PWB_TOGGLE_ERASER_SHORTCUT_ID = "Prefab World Builder/Tools - Toggle Eraser Tool";

        [Shortcut( PWB_TOGGLE_ERASER_SHORTCUT_ID, KeyCode.Alpha8,
            ShortcutModifiers.Shift | ShortcutModifiers.Alt )]
        private static void ToggleEraser() => PWBIO.ToogleTool( ToolManager.PaintTool.ERASER );

        public const string PWB_TOGGLE_SELECTION_SHORTCUT_ID = "Prefab World Builder/Tools - Toggle Selection Tool";

        [Shortcut( PWB_TOGGLE_SELECTION_SHORTCUT_ID, KeyCode.Alpha9,
            ShortcutModifiers.Shift | ShortcutModifiers.Alt )]
        private static void ToggleSelection() => PWBIO.ToogleTool( ToolManager.PaintTool.SELECTION );

        public const string PWB_TOGGLE_EXTRUDE_SHORTCUT_ID = "Prefab World Builder/Tools - Toggle Extrude Tool";

        [Shortcut( PWB_TOGGLE_EXTRUDE_SHORTCUT_ID, KeyCode.X,
            ShortcutModifiers.Shift | ShortcutModifiers.Alt )]
        private static void ToggleExtrude() => PWBIO.ToogleTool( ToolManager.PaintTool.EXTRUDE );

        public const string PWB_TOGGLE_MIRROR_SHORTCUT_ID = "Prefab World Builder/Tools - Toggle Mirror Tool";

        [Shortcut( PWB_TOGGLE_MIRROR_SHORTCUT_ID, KeyCode.M,
            ShortcutModifiers.Shift | ShortcutModifiers.Alt )]
        private static void ToggleMirror() => PWBIO.ToogleTool( ToolManager.PaintTool.MIRROR );

        #endregion

        #region WINDOWS

        public const string PWB_CLOSE_ALL_WINDOWS_ID = "Prefab World Builder/Close All Windows";

        [Shortcut( PWB_CLOSE_ALL_WINDOWS_ID, KeyCode.End,
            ShortcutModifiers.Shift | ShortcutModifiers.Alt )]
        private static void PWBCloseAllWindows()
        {
            ToolManager.DeselectTool();
            PWBIO.CloseAllWindows();
        }

        #endregion

    }
}
#endif