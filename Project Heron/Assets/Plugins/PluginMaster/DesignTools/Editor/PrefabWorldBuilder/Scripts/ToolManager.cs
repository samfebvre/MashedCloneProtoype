using System;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace PluginMaster
{
    [InitializeOnLoad]
    public static class ToolManager
    {

        #region Statics and Constants

        private static PaintTool _tool = PaintTool.NONE;

        private static bool              _editMode;
        public static  Action<PaintTool> OnToolChange;
        public static  Action            OnToolModeChanged;
        public static  bool              _triggerToolChangeEvent = true;

        #endregion

        #region Public Enums

        public enum PaintTool
        {
            NONE,
            PIN,
            BRUSH,
            GRAVITY,
            LINE,
            SHAPE,
            TILING,
            REPLACER,
            ERASER,
            SELECTION,
            EXTRUDE,
            MIRROR,
        }

        public enum ToolState
        {
            NONE,
            PREVIEW,
            EDIT,
            PERSISTENT,
        }

        #endregion

        #region Public Properties

        public static bool editMode
        {
            get => _editMode;
            set
            {
                if ( _editMode == value )
                {
                    return;
                }

                _editMode = value;
                if ( OnToolModeChanged != null )
                {
                    OnToolModeChanged();
                }
            }
        }

        public static PaintTool tool
        {
            get => _tool;
            set
            {
                if ( _tool == value )
                {
                    return;
                }

                PaintTool prevTool = _tool;
                _tool = value;
                if ( _tool != prevTool )
                {
                    BoundsUtils.ClearBoundsDictionaries();
                    if ( _triggerToolChangeEvent && OnToolChange != null )
                    {
                        OnToolChange( prevTool );
                    }

                    _editMode               = false;
                    _triggerToolChangeEvent = true;
                    if ( _tool != PaintTool.NONE )
                    {
                        PWBCore.UpdateTempColliders();
                    }
                }

                switch ( _tool )
                {
                    case PaintTool.PIN:
                        PWBIO.ResetPinValues();
                        break;
                    case PaintTool.BRUSH:
                        break;
                    case PaintTool.GRAVITY:
                        PWBCore.DestroyTempColliders();
                        break;
                    case PaintTool.REPLACER:
                        PWBIO.UpdateOctree();
                        PWBIO.ResetReplacer();
                        break;
                    case PaintTool.ERASER:
                        PWBIO.UpdateOctree();
                        break;
                    case PaintTool.EXTRUDE:
                        SelectionManager.UpdateSelection();
                        PWBIO.ResetUnityCurrentTool();
                        PWBIO.ResetExtrudeState( false );
                        break;
                    case PaintTool.LINE:
                        PWBIO.ResetLineState( false );
                        PWBCore.staticData.VersionUpdate();
                        break;
                    case PaintTool.SHAPE:
                        PWBIO.ResetShapeState( false );
                        break;
                    case PaintTool.TILING:
                        PWBIO.ResetTilingState( false );
                        break;
                    case PaintTool.SELECTION:
                        PWBIO.SetSelectionOriginPosition();
                        SelectionManager.UpdateSelection();
                        PWBIO.ResetUnityCurrentTool();
                        break;
                    case PaintTool.MIRROR:
                        SelectionManager.UpdateSelection();
                        PWBIO.InitializeMirrorPose();
                        break;
                    case PaintTool.NONE:
                        PWBIO.ResetUnityCurrentTool();
                        PWBIO.ResetReplacer();
                        PWBCore.DestroyTempColliders();
                        ApplicationEventHandler.hierarchyChangedWhileUsingTools = false;
                        break;
                }

                if ( _tool != PaintTool.NONE )
                {
                    PWBIO.SaveUnityCurrentTool();
                    ToolProperties.ShowWindow();
                    PaletteManager.pickingBrushes = false;
                }

                if ( _tool    == PaintTool.BRUSH
                     || _tool == PaintTool.PIN
                     || _tool == PaintTool.GRAVITY
                     || _tool == PaintTool.REPLACER
                     || _tool == PaintTool.ERASER
                     || _tool == PaintTool.LINE
                     || _tool == PaintTool.SHAPE
                     || _tool == PaintTool.TILING )
                {
                    PrefabPalette.ShowWindow();
                    BrushProperties.ShowWindow();
                    SelectionManager.UpdateSelection();
                    if ( _tool    == PaintTool.BRUSH
                         || _tool == PaintTool.PIN
                         || _tool == PaintTool.GRAVITY
                         || _tool == PaintTool.REPLACER )
                    {
                        BrushstrokeManager.UpdateBrushstroke();
                    }

                    PWBIO.ResetAutoParent();
                }

                ToolProperties.RepainWindow();
                if ( BrushProperties.instance != null )
                {
                    BrushProperties.instance.Repaint();
                }

                if ( SceneView.sceneViews.Count > 0 )
                {
                    ( (SceneView)
                        SceneView.sceneViews[ 0 ] ).Focus();
                }
            }
        }

        #endregion

        #region Public Methods

        public static void DeselectTool( bool triggerToolChangeEvent = true )
        {
            _triggerToolChangeEvent = triggerToolChangeEvent;
            if ( tool == PaintTool.REPLACER )
            {
                PWBIO.ResetReplacer();
            }

            tool = PaintTool.NONE;
            PWBIO.ResetUnityCurrentTool();
            PWBToolbar.RepaintWindow();
        }

        public static IToolSettings GetSettingsFromTool( PaintTool tool )
        {
            switch ( tool )
            {
                case PaintTool.PIN:       return PinManager.settings;
                case PaintTool.BRUSH:     return BrushManager.settings;
                case PaintTool.GRAVITY:   return GravityToolManager.settings;
                case PaintTool.REPLACER:  return ReplacerManager.settings;
                case PaintTool.ERASER:    return EraserManager.settings;
                case PaintTool.EXTRUDE:   return ExtrudeManager.settings;
                case PaintTool.LINE:      return LineManager.settings;
                case PaintTool.SHAPE:     return ShapeManager.settings;
                case PaintTool.TILING:    return TilingManager.settings;
                case PaintTool.SELECTION: return SelectionToolManager.settings;
                case PaintTool.MIRROR:    return MirrorManager.settings;
                default:                  return null;
            }
        }

        public static PaintTool GetToolFromSettings( IToolSettings settings )
        {
            if ( settings is PinSettings )
            {
                return PaintTool.PIN;
            }

            if ( settings is GravityToolSettings )
            {
                return PaintTool.GRAVITY;
            }

            if ( settings is BrushToolSettings )
            {
                return PaintTool.BRUSH;
            }

            if ( settings is ShapeSettings )
            {
                return PaintTool.SHAPE;
            }

            if ( settings is LineSettings )
            {
                return PaintTool.LINE;
            }

            if ( settings is TilingSettings )
            {
                return PaintTool.TILING;
            }

            if ( settings is ReplacerSettings )
            {
                return PaintTool.REPLACER;
            }

            if ( settings is EraserSettings )
            {
                return PaintTool.ERASER;
            }

            if ( settings is SelectionToolSettings )
            {
                return PaintTool.SELECTION;
            }

            if ( settings is ExtrudeSettings )
            {
                return PaintTool.EXTRUDE;
            }

            if ( settings is MirrorSettings )
            {
                return PaintTool.MIRROR;
            }

            return PaintTool.NONE;
        }

        public static void OnPaletteClosed()
        {
            if ( tool    != PaintTool.ERASER
                 && tool != PaintTool.EXTRUDE )
            {
                tool = PaintTool.NONE;
            }
        }

        #endregion

        #region Private Constructors

        static ToolManager()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            PaletteManager.OnBrushChanged          += TilingManager.settings.UpdateCellSize;
        }

        #endregion

        #region Private Methods

        private static void OnPlayModeStateChanged( PlayModeStateChange state )
        {
            DeselectTool();
            PWBCore.DestroyTempColliders();
        }

        private static void OnSceneClosing( Scene scene, bool removingScene )
        {
            PWBCore.staticData.SaveAndUpdateVersion();
            DeselectTool();
        }

        #endregion

    }
}