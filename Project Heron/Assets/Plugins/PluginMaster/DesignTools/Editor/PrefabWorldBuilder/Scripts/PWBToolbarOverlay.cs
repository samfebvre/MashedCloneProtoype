#if UNITY_2021_2_OR_NEWER
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.UIElements;

namespace PluginMaster
{

    #region TOGGLE MANAGER

    public static class ToggleManager
    {

        #region Statics and Constants

        private static Dictionary<ToolManager.PaintTool, IPWBToogle> _toggles;

        #endregion

        #region Public Properties

        public static string iconPath => EditorGUIUtility.isProSkin ? "Sprites/" : "Sprites/LightTheme/";

        #endregion

        #region Public Methods

        public static void DeselectOthers( string id )
        {
            foreach ( IPWBToogle toggle in toggles.Values )
            {
                if ( toggle == null )
                {
                    continue;
                }

                if ( id != toggle.id
                     && toggle.value )
                {
                    toggle.value = false;
                }
            }
        }

        public static string GetTooltip( string tooltip, string keyCombination ) => tooltip + " ... " + keyCombination;

        #endregion

        #region Private Properties

        private static Dictionary<ToolManager.PaintTool, IPWBToogle> toggles
        {
            get
            {
                if ( _toggles == null )
                {
                    _toggles = new Dictionary<ToolManager.PaintTool, IPWBToogle>
                    {
                        { ToolManager.PaintTool.PIN, PinToggle.instance },
                        { ToolManager.PaintTool.BRUSH, BrushToggle.instance },
                        { ToolManager.PaintTool.GRAVITY, GravityToggle.instance },
                        { ToolManager.PaintTool.LINE, LineToggle.instance },
                        { ToolManager.PaintTool.SHAPE, ShapeToggle.instance },
                        { ToolManager.PaintTool.TILING, TilingToggle.instance },
                        { ToolManager.PaintTool.REPLACER, ReplacerToggle.instance },
                        { ToolManager.PaintTool.ERASER, EraserToggle.instance },
                        { ToolManager.PaintTool.SELECTION, SelectionToggle.instance },
                        { ToolManager.PaintTool.EXTRUDE, ExtrudeToggle.instance },
                        { ToolManager.PaintTool.MIRROR, MirrorToggle.instance },
                    };
                }

                return _toggles;
            }
        }

        #endregion

    }

    #endregion

    #region TOGGLE BASE

    internal interface IPWBToogle
    {

        #region Public Properties

        public string                id    { get; }
        public ToolManager.PaintTool tool  { get; }
        public bool                  value { get; set; }

        #endregion

    }

    public abstract class ToolToggleBase<T> : EditorToolbarToggle,
        IPWBToogle where T : EditorToolbarToggle, new()
    {

        #region Statics and Constants

        #endregion

        #region Public Properties

        public abstract string            id       { get; }
        public static   ToolToggleBase<T> instance { get; private set; }

        public abstract ToolManager.PaintTool tool { get; }

        #endregion

        #region Public Constructors

        public ToolToggleBase()
        {
            instance = this;
            this.RegisterValueChangedCallback( OnValueChange );
            ToolManager.OnToolChange += OnToolChange;
        }

        #endregion

        #region Private Methods

        private void OnToolChange( ToolManager.PaintTool prevTool )
        {
            if ( tool    == prevTool
                 || tool == ToolManager.tool )
            {
                PWBIO.OnToolChange( prevTool );
            }

            if ( tool    == prevTool
                 && tool != ToolManager.tool
                 && value )
            {
                value = false;
            }

            if ( tool == ToolManager.tool
                 && !value )
            {
                value = true;
            }
        }

        private void OnValueChange( ChangeEvent<bool> evt )
        {
            if ( evt.newValue )
            {
                ToolManager.tool = tool;
                ToggleManager.DeselectOthers( id );
            }
            else if ( tool == ToolManager.tool )
            {
                ToolManager.DeselectTool();
            }
        }

        #endregion

    }

    #endregion

    #region TOOLBAR OVERLAY MANAGER

    public static class ToolbarOverlayManager
    {

        #region Public Methods

        public static void OnToolbarDisplayedChanged()
        {
            if ( !PWBCore.staticData.closeAllWindowsWhenClosingTheToolbar )
            {
                return;
            }

            if ( PWBPropPlacementToolbarOverlay.IsDisplayed )
            {
                return;
            }

            if ( PWBSelectionToolbarOverlay.IsDisplayed )
            {
                return;
            }

            if ( PWBGridToolbarOverlay.IsDisplayed )
            {
                return;
            }

            PWBIO.CloseAllWindows();
        }

        #endregion

    }

    #endregion

    #region PROP PLACEMENT TOOLS

    [EditorToolbarElement( ID, typeof(SceneView) )]
    public class PinToggle : ToolToggleBase<PinToggle>
    {

        #region Statics and Constants

        public const string ID = "PWB/PinToggle";

        #endregion

        #region Public Properties

        public override string                id   => ID;
        public override ToolManager.PaintTool tool => ToolManager.PaintTool.PIN;

        #endregion

        #region Public Constructors

        public PinToggle()
        {
            icon    = Resources.Load<Texture2D>( ToggleManager.iconPath + "Pin" );
            tooltip = ToggleManager.GetTooltip( "Pin", PWBSettings.shortcuts.toolbarPinToggle.combination.ToString() );
        }

        #endregion

    }

    [EditorToolbarElement( ID, typeof(SceneView) )]
    public class BrushToggle : ToolToggleBase<BrushToggle>
    {

        #region Statics and Constants

        public const string ID = "PWB/BrushToggle";

        #endregion

        #region Public Properties

        public override string                id   => ID;
        public override ToolManager.PaintTool tool => ToolManager.PaintTool.BRUSH;

        #endregion

        #region Public Constructors

        public BrushToggle()
        {
            icon    = Resources.Load<Texture2D>( ToggleManager.iconPath + "Brush" );
            tooltip = ToggleManager.GetTooltip( "Brush", PWBSettings.shortcuts.toolbarBrushToggle.combination.ToString() );
        }

        #endregion

    }

    [EditorToolbarElement( ID, typeof(SceneView) )]
    public class GravityToggle : ToolToggleBase<GravityToggle>
    {

        #region Statics and Constants

        public const string ID = "PWB/GravityToggle";

        #endregion

        #region Public Properties

        public override string                id   => ID;
        public override ToolManager.PaintTool tool => ToolManager.PaintTool.GRAVITY;

        #endregion

        #region Public Constructors

        public GravityToggle()
        {
            icon    = Resources.Load<Texture2D>( ToggleManager.iconPath + "GravityTool" );
            tooltip = ToggleManager.GetTooltip( "Gravity Brush", PWBSettings.shortcuts.toolbarGravityToggle.combination.ToString() );
        }

        #endregion

    }

    [EditorToolbarElement( ID, typeof(SceneView) )]
    public class LineToggle : ToolToggleBase<LineToggle>
    {

        #region Statics and Constants

        public const string ID = "PWB/LineToggle";

        #endregion

        #region Public Properties

        public override string                id   => ID;
        public override ToolManager.PaintTool tool => ToolManager.PaintTool.LINE;

        #endregion

        #region Public Constructors

        public LineToggle()
        {
            icon    = Resources.Load<Texture2D>( ToggleManager.iconPath + "Line" );
            tooltip = ToggleManager.GetTooltip( "Line", PWBSettings.shortcuts.toolbarLineToggle.combination.ToString() );
        }

        #endregion

    }

    [EditorToolbarElement( ID, typeof(SceneView) )]
    public class ShapeToggle : ToolToggleBase<ShapeToggle>
    {

        #region Statics and Constants

        public const string ID = "PWB/ShapeToggle";

        #endregion

        #region Public Properties

        public override string                id   => ID;
        public override ToolManager.PaintTool tool => ToolManager.PaintTool.SHAPE;

        #endregion

        #region Public Constructors

        public ShapeToggle()
        {
            icon    = Resources.Load<Texture2D>( ToggleManager.iconPath + "Shape" );
            tooltip = ToggleManager.GetTooltip( "Shape", PWBSettings.shortcuts.toolbarShapeToggle.combination.ToString() );
        }

        #endregion

    }

    [EditorToolbarElement( ID, typeof(SceneView) )]
    public class TilingToggle : ToolToggleBase<TilingToggle>
    {

        #region Statics and Constants

        public const string ID = "PWB/TilingToggle";

        #endregion

        #region Public Properties

        public override string                id   => ID;
        public override ToolManager.PaintTool tool => ToolManager.PaintTool.TILING;

        #endregion

        #region Public Constructors

        public TilingToggle()
        {
            icon    = Resources.Load<Texture2D>( ToggleManager.iconPath + "Tiling" );
            tooltip = ToggleManager.GetTooltip( "Tiling", PWBSettings.shortcuts.toolbarTilingToggle.combination.ToString() );
        }

        #endregion

    }

    [EditorToolbarElement( ID, typeof(SceneView) )]
    public class ReplacerToggle : ToolToggleBase<ReplacerToggle>
    {

        #region Statics and Constants

        public const string ID = "PWB/ReplacerToggle";

        #endregion

        #region Public Properties

        public override string                id   => ID;
        public override ToolManager.PaintTool tool => ToolManager.PaintTool.REPLACER;

        #endregion

        #region Public Constructors

        public ReplacerToggle()
        {
            icon    = Resources.Load<Texture2D>( ToggleManager.iconPath + "Replace" );
            tooltip = ToggleManager.GetTooltip( "Replacer", PWBSettings.shortcuts.toolbarReplacerToggle.combination.ToString() );
        }

        #endregion

    }

    [EditorToolbarElement( ID, typeof(SceneView) )]
    public class EraserToggle : ToolToggleBase<EraserToggle>
    {

        #region Statics and Constants

        public const string ID = "PWB/EraserToggle";

        #endregion

        #region Public Properties

        public override string                id   => ID;
        public override ToolManager.PaintTool tool => ToolManager.PaintTool.ERASER;

        #endregion

        #region Public Constructors

        public EraserToggle()
        {
            icon    = Resources.Load<Texture2D>( ToggleManager.iconPath + "Eraser" );
            tooltip = ToggleManager.GetTooltip( "Eraser", PWBSettings.shortcuts.toolbarEraserToggle.combination.ToString() );
        }

        #endregion

    }

    [EditorToolbarElement( ID, typeof(SceneView) )]
    public class HelpButton : EditorToolbarButton
    {

        #region Statics and Constants

        public const string ID = "PWB/HelpButton";

        #endregion

        #region Public Constructors

        public HelpButton()
        {
            icon    =  Resources.Load<Texture2D>( ToggleManager.iconPath + "Help" );
            tooltip =  "Documentation";
            clicked += OpenDocumentation;
        }

        #endregion

        #region Private Fields

        private Object _documentationPdf;

        #endregion

        #region Private Methods

        private void OpenDocumentation()
        {
            if ( _documentationPdf == null )
            {
                _documentationPdf = AssetDatabase.LoadMainAssetAtPath( PWBCore.staticData.documentationPath );
            }

            if ( _documentationPdf == null )
            {
                Debug.LogWarning( "Missing Documentation File" );
            }
            else
            {
                AssetDatabase.OpenAsset( _documentationPdf );
            }
        }

        #endregion

    }

    [Icon( "Assets/PluginMaster/DesignTools/PrefabWorldBuilder/Editor/Resources/Sprites/Brush.png" )]
    [Overlay( typeof(SceneView), "PWB Prop Placement Tools", true )]
    public class PWBPropPlacementToolbarOverlay : ToolbarOverlay
    {

        #region Statics and Constants

        #endregion

        #region Public Properties

        public static bool IsDisplayed { get; private set; }

        #endregion

        #region Private Constructors

        private PWBPropPlacementToolbarOverlay() : base( PinToggle.ID, BrushToggle.ID, GravityToggle.ID, LineToggle.ID,
            ShapeToggle.ID, TilingToggle.ID, ReplacerToggle.ID, EraserToggle.ID, HelpButton.ID )
        {
            displayedChanged += OndisplayedChanged;
        }

        #endregion

        #region Private Methods

        private void OndisplayedChanged( bool value )
        {
            IsDisplayed = value;
            ToolbarOverlayManager.OnToolbarDisplayedChanged();
        }

        #endregion

    }

    #endregion

    #region SELECTION TOOLS

    [EditorToolbarElement( ID, typeof(SceneView) )]
    public class SelectionToggle : ToolToggleBase<SelectionToggle>
    {

        #region Statics and Constants

        public const string ID = "PWB/SelectionToggle";

        #endregion

        #region Public Properties

        public override string                id   => ID;
        public override ToolManager.PaintTool tool => ToolManager.PaintTool.SELECTION;

        #endregion

        #region Public Constructors

        public SelectionToggle()
        {
            icon = Resources.Load<Texture2D>( ToggleManager.iconPath + "Selection" );
            tooltip = ToggleManager.GetTooltip( "Selection",
                PWBSettings.shortcuts.toolbarSelectionToggle.combination.ToString() );
        }

        #endregion

    }

    [EditorToolbarElement( ID, typeof(SceneView) )]
    public class ExtrudeToggle : ToolToggleBase<ExtrudeToggle>
    {

        #region Statics and Constants

        public const string ID = "PWB/ExtrudeToggle";

        #endregion

        #region Public Properties

        public override string                id   => ID;
        public override ToolManager.PaintTool tool => ToolManager.PaintTool.EXTRUDE;

        #endregion

        #region Public Constructors

        public ExtrudeToggle()
        {
            icon    = Resources.Load<Texture2D>( ToggleManager.iconPath + "Extrude" );
            tooltip = ToggleManager.GetTooltip( "Extrude", PWBSettings.shortcuts.toolbarExtrudeToggle.combination.ToString() );
        }

        #endregion

    }

    [EditorToolbarElement( ID, typeof(SceneView) )]
    public class MirrorToggle : ToolToggleBase<MirrorToggle>
    {

        #region Statics and Constants

        public const string ID = "PWB/MirrorToggle";

        #endregion

        #region Public Properties

        public override string                id   => ID;
        public override ToolManager.PaintTool tool => ToolManager.PaintTool.MIRROR;

        #endregion

        #region Public Constructors

        public MirrorToggle()
        {
            icon    = Resources.Load<Texture2D>( ToggleManager.iconPath + "Mirror" );
            tooltip = ToggleManager.GetTooltip( "Mirror", PWBSettings.shortcuts.toolbarMirrorToggle.combination.ToString() );
        }

        #endregion

    }

    [Icon( "Assets/PluginMaster/DesignTools/PrefabWorldBuilder/Editor/Resources/Sprites/Selection.png" )]
    [Overlay( typeof(SceneView), "PWB Selection Tools", true )]
    public class PWBSelectionToolbarOverlay : ToolbarOverlay
    {

        #region Statics and Constants

        #endregion

        #region Public Properties

        public static bool IsDisplayed { get; private set; }

        #endregion

        #region Private Constructors

        private PWBSelectionToolbarOverlay() : base( SelectionToggle.ID, ExtrudeToggle.ID, MirrorToggle.ID )
        {
            displayedChanged += OndisplayedChanged;
        }

        #endregion

        #region Private Methods

        private void OndisplayedChanged( bool value )
        {
            IsDisplayed = value;
            ToolbarOverlayManager.OnToolbarDisplayedChanged();
        }

        #endregion

    }

    #endregion

    #region GRID TOOLS

    [EditorToolbarElement( ID, typeof(SceneView) )]
    public class GridTypeToggle : EditorToolbarButton
    {

        #region Statics and Constants

        public const string ID = "PWB/GridTypeToggle";

        #endregion

        #region Public Constructors

        public GridTypeToggle()
        {
            UpdateIcon();
            clicked                            += OnClick;
            SnapManager.settings.OnDataChanged += UpdateIcon;
        }

        #endregion

        #region Private Fields

        private Texture2D _radialGridIcon;
        private Texture2D _rectGridIcon;

        #endregion

        #region Private Methods

        private void OnClick()
        {
            SnapManager.settings.radialGridEnabled = !SnapManager.settings.radialGridEnabled;
            UpdateIcon();
            SnapSettingsWindow.RepaintWindow();
        }

        private void UpdateIcon()
        {
            if ( _radialGridIcon == null )
            {
                _radialGridIcon = Resources.Load<Texture2D>( ToggleManager.iconPath + "RadialGrid" );
            }

            if ( _rectGridIcon == null )
            {
                _rectGridIcon = Resources.Load<Texture2D>( ToggleManager.iconPath + "Grid" );
            }

            icon    = SnapManager.settings.radialGridEnabled ? _rectGridIcon : _radialGridIcon;
            tooltip = SnapManager.settings.radialGridEnabled ? "Grid" : "Radial Grid";
        }

        #endregion

    }

    [EditorToolbarElement( ID, typeof(SceneView) )]
    public class SnapToggle : EditorToolbarDropdownToggle, IAccessContainerWindow
    {

        #region Statics and Constants

        public const string ID = "PWB/SnapToggle";

        #endregion

        #region Public Properties

        public EditorWindow containerWindow { get; set; }

        #endregion

        #region Public Constructors

        public SnapToggle()
        {
            icon            =  Resources.Load<Texture2D>( ToggleManager.iconPath + "SnapOn" );
            tooltip         =  "Enable snapping";
            dropdownClicked += ShowSnapWindow;
            this.RegisterValueChangedCallback( OnValueChange );
            SnapManager.settings.OnDataChanged += () => value = SnapManager.settings.snappingEnabled;
        }

        #endregion

        #region Private Methods

        private void OnValueChange( ChangeEvent<bool> evt )
        {
            SnapManager.settings.snappingEnabled = evt.newValue;
            SnapSettingsWindow.RepaintWindow();
        }

        private void ShowSnapWindow()
        {
            SnapSettings settings = SnapManager.settings;
            GenericMenu  menu     = new GenericMenu();
            if ( settings.radialGridEnabled )
            {
                menu.AddItem( new GUIContent( "Snap To Radius" ), settings.snapToRadius,
                    () => settings.snapToRadius = !settings.snapToRadius );
                menu.AddItem( new GUIContent( "Snap To Circunference" ), settings.snapToCircunference,
                    () => settings.snapToCircunference = !settings.snapToCircunference );
            }
            else
            {
                menu.AddItem( new GUIContent( "X" ), settings.snappingOnX, () => settings.snappingOnX = !settings.snappingOnX );
                menu.AddItem( new GUIContent( "Y" ), settings.snappingOnY, () => settings.snappingOnY = !settings.snappingOnY );
                menu.AddItem( new GUIContent( "Z" ), settings.snappingOnZ, () => settings.snappingOnZ = !settings.snappingOnZ );
            }

            menu.ShowAsContext();
            SnapSettingsWindow.RepaintWindow();
        }

        #endregion

    }

    [EditorToolbarElement( ID, typeof(SceneView) )]
    public class GridToggle : EditorToolbarDropdownToggle, IAccessContainerWindow
    {

        #region Statics and Constants

        public const string ID = "PWB/GridToggle";

        #endregion

        #region Public Properties

        public EditorWindow containerWindow { get; set; }

        #endregion

        #region Public Constructors

        public GridToggle()
        {
            icon            =  Resources.Load<Texture2D>( ToggleManager.iconPath + "ShowGrid" );
            tooltip         =  "Show grid";
            dropdownClicked += ShowGridWindow;
            this.RegisterValueChangedCallback( OnValueChange );
            SnapManager.settings.OnDataChanged += () => value = SnapManager.settings.visibleGrid;
        }

        #endregion

        #region Private Methods

        private void OnValueChange( ChangeEvent<bool> evt )
            => SnapManager.settings.visibleGrid = evt.newValue;

        private void ShowGridWindow()
        {
            SnapSettings settings = SnapManager.settings;
            GenericMenu  menu     = new GenericMenu();
            menu.AddItem( new GUIContent( "X" ), settings.gridOnX, () => settings.gridOnX = !settings.gridOnX );
            menu.AddItem( new GUIContent( "Y" ), settings.gridOnY, () => settings.gridOnY = !settings.gridOnY );
            menu.AddItem( new GUIContent( "Z" ), settings.gridOnZ, () => settings.gridOnZ = !settings.gridOnZ );
            menu.ShowAsContext();
        }

        #endregion

    }

    [EditorToolbarElement( ID, typeof(SceneView) )]
    public class LockGridToggle : EditorToolbarToggle
    {

        #region Statics and Constants

        public const string ID = "PWB/LockGridToggle";

        #endregion

        #region Public Constructors

        public LockGridToggle()
        {
            UpdteIcon();
            this.RegisterValueChangedCallback( OnValueChange );
            SnapManager.settings.OnDataChanged += () => value = SnapManager.settings.lockedGrid;
        }

        #endregion

        #region Private Methods

        private void OnValueChange( ChangeEvent<bool> evt )
        {
            SnapManager.settings.lockedGrid = evt.newValue;
            UpdteIcon();
        }

        private void UpdteIcon()
        {
            icon = Resources.Load<Texture2D>( ToggleManager.iconPath
                                              + ( SnapManager.settings.lockedGrid ? "LockGrid" : "UnlockGrid" ) );
            tooltip = SnapManager.settings.lockedGrid ? "Lock the grid origin in place" : "Unlock the grid origin";
        }

        #endregion

    }

    [EditorToolbarElement( ID, typeof(SceneView) )]
    public class GridSettingsButton : EditorToolbarButton
    {

        #region Statics and Constants

        public const string ID = "PWB/GridSettingsButton";

        #endregion

        #region Public Constructors

        public GridSettingsButton()
        {
            icon    =  Resources.Load<Texture2D>( ToggleManager.iconPath + "SnapSettings" );
            tooltip =  "Grid & Snapping Settings";
            clicked += SnapSettingsWindow.ShowWindow;
        }

        #endregion

    }

    [Icon( "Assets/PluginMaster/DesignTools/PrefabWorldBuilder/Editor/Resources/Sprites/Grid.png" )]
    [Overlay( typeof(SceneView), "PWB Grid Tools", true )]
    public class PWBGridToolbarOverlay : ToolbarOverlay
    {

        #region Statics and Constants

        #endregion

        #region Public Properties

        public static bool IsDisplayed { get; private set; }

        #endregion

        #region Private Constructors

        private PWBGridToolbarOverlay() : base( GridTypeToggle.ID, SnapToggle.ID,
            GridToggle.ID, LockGridToggle.ID, GridSettingsButton.ID )
        {
            displayedChanged += OndisplayedChanged;
        }

        #endregion

        #region Private Methods

        private void OndisplayedChanged( bool value )
        {
            IsDisplayed = value;
            ToolbarOverlayManager.OnToolbarDisplayedChanged();
        }

        #endregion

    }

    #endregion

}
#endif