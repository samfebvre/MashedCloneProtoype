using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace PluginMaster
{
    public class PWBToolbar : EditorWindow
    {

        #region WINDOW

        private GUISkin  _skin;
        private GUIStyle _btnStyle;
        private bool     _wasDocked;

        public static PWBToolbar instance { get; private set; }

        private Vector2 _mainScrollPosition = Vector2.zero;

        private const int MIN_SIZE = 32;
        private const int WIDTH    = 556;
        private const int HEIGHT   = 520;

        private Object _documentationPdf;

        [MenuItem( "Tools/Plugin Master/Prefab World Builder/Toolbar...", false, 1100 )]
        public static void ShowWindow()
        {
            #if UNITY_2021_2_OR_NEWER
            if ( !EditorUtility.DisplayDialog( "Toolbar Overlays",
                    "PWB tools are available as overlay panels in the scene view window (in Unity 2021.2 or higher). "
                    + "\nClick anywhere in the Scene view and press the Spacebar to open the overlays menu.",
                    "Open toolbar anyway", "cancel" ) )
            {
                return;
            }
            #endif
            bool isANewInstance = instance == null;
            instance = GetWindow<PWBToolbar>( "Tools" );
            if ( isANewInstance )
            {
                instance.position = new Rect( instance.position.x, instance.position.y, WIDTH, MIN_SIZE );
            }
        }

        public static void RepaintWindow()
        {
            if ( instance == null )
            {
                return;
            }

            instance.Repaint();
        }

        public static void CloseWindow()
        {
            if ( instance != null )
            {
                instance.Close();
            }
        }

        private void OnEnable()
        {
            instance = this;
            _skin    = Resources.Load<GUISkin>( "PWBSkin" );
            if ( _skin == null )
            {
                Close();
                return;
            }

            Assert.IsNotNull( _skin );
            _btnStyle           = _skin.GetStyle( "ToggleButton" );
            _foldoutButtonStyle = new GUIStyle( _btnStyle );
            LoadToolIcons();
            LoadSnapIcons();
            LoadSelectionToolIcons();
            _axisButtonStyle = _skin.GetStyle( "AxisButton" );

            _radialAxisButtonStyle            = new GUIStyle( _axisButtonStyle );
            _radialAxisButtonStyle.fixedWidth = 12;

            _buttonWithAxesStyle              = new GUIStyle( _btnStyle );
            _buttonWithAxesStyle.margin.right = _buttonWithAxesStyle.margin.bottom = 0;

            _simpleBtnStyle          = new GUIStyle( _btnStyle );
            _simpleBtnStyle.onNormal = _simpleBtnStyle.normal;

            minSize         = new Vector2( MIN_SIZE, MIN_SIZE );
            _wasDocked      = !isDocked;
            PWBIO.controlId = GUIUtility.GetControlID( GetHashCode(), FocusType.Passive );
            PWBIO.UpdateOctree();

            _documentationPdf                  =  AssetDatabase.LoadMainAssetAtPath( PWBCore.staticData.documentationPath );
            ToolManager.OnToolChange           += OnToolChange;
            SnapManager.settings.OnDataChanged += Repaint;
        }

        private void OnDisable()
        {
            ToolManager.OnToolChange           -= OnToolChange;
            SnapManager.settings.OnDataChanged -= Repaint;
            ToolManager.DeselectTool();
        }

        private void OnDestroy()
        {
            if ( PWBCore.staticData.closeAllWindowsWhenClosingTheToolbar )
            {
                PWBIO.CloseAllWindows( false );
            }
        }

        private void OnGUI()
        {
            if ( _skin == null )
            {
                Close();
                return;
            }
            #if UNITY_2019_1_OR_NEWER
            UpdateShortcutsTooltips();
            #endif
            bool widthGreaterThanHeight = position.width > position.height;
            UpdateFoldoutButtonStyle();
            using ( EditorGUILayout.ScrollViewScope scrollView = new EditorGUILayout.ScrollViewScope( _mainScrollPosition, false, false,
                       widthGreaterThanHeight ? GUI.skin.horizontalScrollbar : GUIStyle.none,
                       widthGreaterThanHeight ? GUIStyle.none : GUI.skin.verticalScrollbar, GUIStyle.none ) )
            {
                _mainScrollPosition = scrollView.scrollPosition;
                using ( position.width > position.height
                           ? new GUILayout.HorizontalScope( _skin.box )
                           : (GUI.Scope)new GUILayout.VerticalScope( _skin.box ) )
                {
                    _axisButtonStyle.fixedHeight       = widthGreaterThanHeight ? 24 : 12;
                    _radialAxisButtonStyle.fixedHeight = _axisButtonStyle.fixedHeight;
                    ToolsGUI();
                    GUILayout.Space( 5 );
                    SnapGUI();
                    GUILayout.Space( 5 );
                    if ( GUILayout.Button( _helpIcon, _btnStyle ) )
                    {
                        if ( _documentationPdf == null )
                        {
                            _documentationPdf = AssetDatabase
                                .LoadMainAssetAtPath( PWBCore.staticData.documentationPath );
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

                    GUILayout.FlexibleSpace();
                }
            }
        }

        private void Update()
        {
            if ( _wasDocked && !isDocked )
            {
                Vector2 size = position.width >= position.height ? new Vector2( WIDTH, MIN_SIZE ) : new Vector2( MIN_SIZE, HEIGHT );
                position   = new Rect( position.position, size );
                _wasDocked = false;
            }
            else if ( !_wasDocked && isDocked )
            {
                _wasDocked = true;
            }
        }

        private bool isDocked
        {
            get
            {
                MethodInfo isDockedMethod = typeof(EditorWindow).GetProperty( "docked",
                    BindingFlags.Public
                    | BindingFlags.NonPublic
                    | BindingFlags.Instance
                    | BindingFlags.Static ).GetGetMethod( true );
                return (bool)isDockedMethod.Invoke( this, null );
            }
        }

        private void UpdateFoldoutButtonStyle()
        {
            if ( position.width >= position.height )
            {
                _foldoutButtonStyle.fixedWidth  = 16;
                _foldoutButtonStyle.fixedHeight = 24;
            }
            else
            {
                _foldoutButtonStyle.fixedWidth  = 24;
                _foldoutButtonStyle.fixedHeight = 16;
            }
        }

        #if UNITY_2019_1_OR_NEWER
        private void UpdateShortcutsTooltips()
        {
            void UpdateTooltipShortcut( GUIContent button, string tooltip, string keyCombination )
            {
                if ( keyCombination != string.Empty )
                {
                    button.tooltip = tooltip + " ... " + keyCombination;
                }
            }

            UpdateTooltipShortcut( _pinIcon,    "Pin",    PWBSettings.shortcuts.toolbarPinToggle.combination.ToString() );
            UpdateTooltipShortcut( _brushIcon,  "Brush",  PWBSettings.shortcuts.toolbarBrushToggle.combination.ToString() );
            UpdateTooltipShortcut( _eraserIcon, "Eraser", PWBSettings.shortcuts.toolbarEraserToggle.combination.ToString() );
            UpdateTooltipShortcut( _physicsIcon, "Gravity Brush",
                PWBSettings.shortcuts.toolbarGravityToggle.combination.ToString() );
            UpdateTooltipShortcut( _extrudeIcon, "Extrude", PWBSettings.shortcuts.toolbarExtrudeToggle.combination.ToString() );
            UpdateTooltipShortcut( _lineIcon,    "Line",    PWBSettings.shortcuts.toolbarLineToggle.combination.ToString() );
            UpdateTooltipShortcut( _shapeIcon,   "Shape",   PWBSettings.shortcuts.toolbarShapeToggle.combination.ToString() );
            UpdateTooltipShortcut( _tilingIcon,  "Tiling",  PWBSettings.shortcuts.toolbarTilingToggle.combination.ToString() );
            UpdateTooltipShortcut( _selectionIcon, "Selection",
                PWBSettings.shortcuts.toolbarSelectionToggle.combination.ToString() );
            UpdateTooltipShortcut( _mirrorIcon, "Mirror", PWBSettings.shortcuts.toolbarMirrorToggle.combination.ToString() );
            UpdateTooltipShortcut( _lockGridIcon, "Lock the grid origin in place",
                PWBSettings.shortcuts.gridToggleLock.combination.ToString() );
            UpdateTooltipShortcut( _unlockGridIcon, "Unlock the grid origin",
                PWBSettings.shortcuts.gridToggleLock.combination.ToString() );
        }
        #endif

        #endregion

        #region TOOLS

        private GUIContent _pinIcon;
        private GUIContent _brushIcon;
        private GUIContent _eraserIcon;
        private GUIContent _physicsIcon;
        private GUIContent _extrudeIcon;
        private GUIContent _lineIcon;
        private GUIContent _shapeIcon;
        private GUIContent _tilingIcon;
        private GUIContent _selectionIcon;
        private GUIContent _mirrorIcon;
        private GUIContent _replaceIcon;
        private GUIContent _helpIcon;

        private bool _toolChanged;

        private void OnToolChange( ToolManager.PaintTool prevTool )
        {
            _toolChanged = true;
            PWBIO.OnToolChange( prevTool );
        }

        private void LoadToolIcons()
        {
            _pinIcon       = new GUIContent( Resources.Load<Texture2D>( "Sprites/Pin" ),         "Pin" );
            _brushIcon     = new GUIContent( Resources.Load<Texture2D>( "Sprites/Brush" ),       "Brush" );
            _eraserIcon    = new GUIContent( Resources.Load<Texture2D>( "Sprites/Eraser" ),      "Eraser" );
            _physicsIcon   = new GUIContent( Resources.Load<Texture2D>( "Sprites/GravityTool" ), "Gravity Brush" );
            _extrudeIcon   = new GUIContent( Resources.Load<Texture2D>( "Sprites/Extrude" ),     "Extrude" );
            _lineIcon      = new GUIContent( Resources.Load<Texture2D>( "Sprites/Line" ),        "Line" );
            _shapeIcon     = new GUIContent( Resources.Load<Texture2D>( "Sprites/Shape" ),       "Shape" );
            _tilingIcon    = new GUIContent( Resources.Load<Texture2D>( "Sprites/Tiling" ),      "Tiling" );
            _selectionIcon = new GUIContent( Resources.Load<Texture2D>( "Sprites/Selection" ),   "Selection" );
            _mirrorIcon    = new GUIContent( Resources.Load<Texture2D>( "Sprites/Mirror" ),      "Mirror" );
            _replaceIcon   = new GUIContent( Resources.Load<Texture2D>( "Sprites/Replace" ),     "Replacer" );
            _helpIcon      = new GUIContent( Resources.Load<Texture2D>( "Sprites/Help" ),        "Documentation" );
        }

        private void ToolsGUI()
        {
            using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
            {
                ToolManager.PaintTool newtool = ToolManager.tool;

                bool pinSelected = newtool == ToolManager.PaintTool.PIN;
                newtool = GUILayout.Toggle( pinSelected, _pinIcon, _btnStyle )
                    ? ToolManager.PaintTool.PIN
                    : pinSelected
                        ? ToolManager.PaintTool.NONE
                        : newtool;

                bool brushSelected = newtool == ToolManager.PaintTool.BRUSH;
                newtool = GUILayout.Toggle( brushSelected, _brushIcon, _btnStyle )
                    ? ToolManager.PaintTool.BRUSH
                    : brushSelected
                        ? ToolManager.PaintTool.NONE
                        : newtool;

                bool gravitySelected = newtool == ToolManager.PaintTool.GRAVITY;
                newtool = GUILayout.Toggle( gravitySelected, _physicsIcon, _btnStyle )
                    ? ToolManager.PaintTool.GRAVITY
                    : gravitySelected
                        ? ToolManager.PaintTool.NONE
                        : newtool;

                bool lineSelected = newtool == ToolManager.PaintTool.LINE;
                newtool = GUILayout.Toggle( lineSelected, _lineIcon, _btnStyle )
                    ? ToolManager.PaintTool.LINE
                    : lineSelected
                        ? ToolManager.PaintTool.NONE
                        : newtool;

                bool shapeSelected = newtool == ToolManager.PaintTool.SHAPE;
                newtool = GUILayout.Toggle( shapeSelected, _shapeIcon, _btnStyle )
                    ? ToolManager.PaintTool.SHAPE
                    : shapeSelected
                        ? ToolManager.PaintTool.NONE
                        : newtool;

                bool tilingSelected = newtool == ToolManager.PaintTool.TILING;
                newtool = GUILayout.Toggle( tilingSelected, _tilingIcon, _btnStyle )
                    ? ToolManager.PaintTool.TILING
                    : tilingSelected
                        ? ToolManager.PaintTool.NONE
                        : newtool;

                bool replaceSelected = newtool == ToolManager.PaintTool.REPLACER;
                newtool = GUILayout.Toggle( replaceSelected, _replaceIcon, _btnStyle )
                    ? ToolManager.PaintTool.REPLACER
                    : replaceSelected
                        ? ToolManager.PaintTool.NONE
                        : newtool;

                bool eraserSelected = newtool == ToolManager.PaintTool.ERASER;
                newtool = GUILayout.Toggle( eraserSelected, _eraserIcon, _btnStyle )
                    ? ToolManager.PaintTool.ERASER
                    : eraserSelected
                        ? ToolManager.PaintTool.NONE
                        : newtool;

                GUILayout.Space( 5 );

                bool selectionSelected = newtool == ToolManager.PaintTool.SELECTION;
                newtool = GUILayout.Toggle( selectionSelected, _selectionIcon, _buttonWithAxesStyle )
                    ? ToolManager.PaintTool.SELECTION
                    : selectionSelected
                        ? ToolManager.PaintTool.NONE
                        : newtool;
                GUILayout.Space( 1 );
                bool TRSChanged = false;
                using ( new EditorGUI.DisabledGroupScope( !selectionSelected ) )
                {
                    using ( new GUILayout.HorizontalScope() )
                    {
                        using ( EditorGUI.ChangeCheckScope checkTRS = new EditorGUI.ChangeCheckScope() )
                        {
                            SelectionToolManager.settings.move = GUILayout.Toggle( SelectionToolManager.settings.move,
                                _tIcon, _axisButtonStyle );
                            SelectionToolManager.settings.rotate = GUILayout.Toggle( SelectionToolManager.settings.rotate,
                                _rIcon, _axisButtonStyle );
                            SelectionToolManager.settings.scale = GUILayout.Toggle( SelectionToolManager.settings.scale,
                                _sIcon, _axisButtonStyle );
                            if ( checkTRS.changed )
                            {
                                TRSChanged = true;
                                SceneView.RepaintAll();
                                ToolProperties.RepainWindow();
                            }
                        }
                    }
                }

                bool extrudeSelected = newtool == ToolManager.PaintTool.EXTRUDE;
                newtool = GUILayout.Toggle( extrudeSelected, _extrudeIcon, _btnStyle )
                    ? ToolManager.PaintTool.EXTRUDE
                    : extrudeSelected
                        ? ToolManager.PaintTool.NONE
                        : newtool;

                bool mirrorSelected = newtool == ToolManager.PaintTool.MIRROR;
                newtool = GUILayout.Toggle( mirrorSelected, _mirrorIcon, _btnStyle )
                    ? ToolManager.PaintTool.MIRROR
                    : mirrorSelected
                        ? ToolManager.PaintTool.NONE
                        : newtool;

                if ( ( check.changed || _toolChanged )
                     && !TRSChanged )
                {
                    _toolChanged     = false;
                    ToolManager.tool = newtool;
                }
            }
        }

        #endregion

        #region SELECTION TOOL

        private GUIContent _tIcon;
        private GUIContent _rIcon;
        private GUIContent _sIcon;

        private void LoadSelectionToolIcons()
        {
            _tIcon = new GUIContent( Resources.Load<Texture2D>( "Sprites/T" ) );
            _rIcon = new GUIContent( Resources.Load<Texture2D>( "Sprites/R" ) );
            _sIcon = new GUIContent( Resources.Load<Texture2D>( "Sprites/S" ) );
        }

        #endregion

        #region SNAP

        private GUIContent _showGridIcon;
        private GUIContent _enableSnappingIcon;
        private GUIContent _lockGridIcon;
        private GUIContent _unlockGridIcon;
        private GUIContent _snapSettingsIcon;
        private GUIContent _gridIcon;
        private GUIContent _radialGridIcon;
        private GUIContent _xIcon;
        private GUIContent _yIcon;
        private GUIContent _zIcon;
        private GUIStyle   _axisButtonStyle;
        private GUIStyle   _buttonWithAxesStyle;
        private GUIStyle   _simpleBtnStyle;
        private GUIStyle   _radialAxisButtonStyle;
        private GUIContent _cIcon;
        private bool       _showGridTools = true;
        private GUIContent _showGridToolsIcon;
        private GUIContent _hideGridToolsIcon;
        private GUIContent _showGridToolsHIcon;
        private GUIContent _hideGridToolsHIcon;
        private GUIStyle   _foldoutButtonStyle;

        private void LoadSnapIcons()
        {
            _showGridIcon       = new GUIContent( Resources.Load<Texture2D>( "Sprites/ShowGrid" ), "Show grid" );
            _enableSnappingIcon = new GUIContent( Resources.Load<Texture2D>( "Sprites/SnapOn" ),   "Enable snapping" );
            _lockGridIcon = new GUIContent( Resources.Load<Texture2D>( "Sprites/LockGrid" ),
                "Lock the grid origin in place" );
            _unlockGridIcon = new GUIContent( Resources.Load<Texture2D>( "Sprites/UnlockGrid" ),
                "Unlock the grid origin" );
            _snapSettingsIcon = new GUIContent( Resources.Load<Texture2D>( "Sprites/SnapSettings" ),
                "Grid and Snapping Settings" );
            _gridIcon           = new GUIContent( Resources.Load<Texture2D>( "Sprites/Grid" ),       "Grid" );
            _radialGridIcon     = new GUIContent( Resources.Load<Texture2D>( "Sprites/RadialGrid" ), "Radial Grid" );
            _xIcon              = new GUIContent( Resources.Load<Texture2D>( "Sprites/X" ) );
            _yIcon              = new GUIContent( Resources.Load<Texture2D>( "Sprites/Y" ) );
            _zIcon              = new GUIContent( Resources.Load<Texture2D>( "Sprites/Z" ) );
            _cIcon              = new GUIContent( Resources.Load<Texture2D>( "Sprites/C" ) );
            _showGridToolsIcon  = new GUIContent( Resources.Load<Texture2D>( "Sprites/ShowGridTools" ) );
            _hideGridToolsIcon  = new GUIContent( Resources.Load<Texture2D>( "Sprites/HideGridTools" ) );
            _showGridToolsHIcon = new GUIContent( Resources.Load<Texture2D>( "Sprites/ShowGridToolsH" ) );
            _hideGridToolsHIcon = new GUIContent( Resources.Load<Texture2D>( "Sprites/HideGridToolsH" ) );
        }

        private void SnapGUI()
        {
            GUIContent foldoutIcon = position.width > position.height
                ? _showGridTools ? _hideGridToolsHIcon : _showGridToolsHIcon
                : _showGridTools
                    ? _hideGridToolsIcon
                    : _showGridToolsIcon;
            if ( !_showGridTools )
            {
                if ( GUILayout.Button( foldoutIcon, _foldoutButtonStyle ) )
                {
                    _showGridTools = true;
                }
            }

            if ( !_showGridTools )
            {
                return;
            }

            SnapSettings settings = SnapManager.settings;
            using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
            {
                settings.radialGridEnabled = GUILayout.Toggle( settings.radialGridEnabled,
                    settings.radialGridEnabled ? _gridIcon : _radialGridIcon, _simpleBtnStyle );
                if ( check.changed )
                {
                    SnapSettingsWindow.RepaintWindow();
                }
            }

            settings.snappingEnabled = GUILayout.Toggle( settings.snappingEnabled,
                _enableSnappingIcon, _buttonWithAxesStyle );
            GUILayout.Space( 1 );
            using ( new EditorGUI.DisabledGroupScope( !settings.snappingEnabled ) )
            {
                using ( new GUILayout.HorizontalScope() )
                {
                    if ( settings.radialGridEnabled )
                    {
                        settings.snapToRadius = GUILayout.Toggle( settings.snapToRadius,
                            _rIcon, position.width > position.height ? _axisButtonStyle : _radialAxisButtonStyle );
                        settings.snapToCircunference = GUILayout.Toggle( settings.snapToCircunference,
                            _cIcon, position.width > position.height ? _axisButtonStyle : _radialAxisButtonStyle );
                    }
                    else
                    {
                        settings.snappingOnX = GUILayout.Toggle( settings.snappingOnX,
                            _xIcon, _axisButtonStyle );
                        SnapManager.settings.snappingOnY = GUILayout.Toggle( settings.snappingOnY,
                            _yIcon, _axisButtonStyle );
                        settings.snappingOnZ = GUILayout.Toggle( settings.snappingOnZ,
                            _zIcon, _axisButtonStyle );
                    }
                }
            }

            settings.visibleGrid = GUILayout.Toggle( settings.visibleGrid,
                _showGridIcon, _buttonWithAxesStyle );
            GUILayout.Space( 1 );
            using ( new EditorGUI.DisabledGroupScope( !settings.visibleGrid ) )
            {
                using ( new GUILayout.HorizontalScope() )
                {
                    using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                    {
                        bool showGridX = GUILayout.Toggle( settings.gridOnX, _xIcon, _axisButtonStyle );
                        if ( check.changed && showGridX )
                        {
                            settings.gridOnX = showGridX;
                        }
                    }

                    using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                    {
                        bool showGridY = GUILayout.Toggle( settings.gridOnY, _yIcon, _axisButtonStyle );
                        if ( check.changed && showGridY )
                        {
                            settings.gridOnY = showGridY;
                        }
                    }

                    using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                    {
                        bool showGridZ = GUILayout.Toggle( settings.gridOnZ, _zIcon, _axisButtonStyle );
                        if ( check.changed && showGridZ )
                        {
                            settings.gridOnZ = showGridZ;
                        }
                    }
                }
            }

            using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
            {
                settings.lockedGrid = GUILayout.Toggle( settings.lockedGrid,
                    settings.lockedGrid ? _lockGridIcon : _unlockGridIcon, _btnStyle );
                if ( check.changed )
                {
                    SnapSettingsWindow.RepaintWindow();
                }
            }

            if ( GUILayout.Button( _snapSettingsIcon, _btnStyle ) )
            {
                SnapSettingsWindow.ShowWindow();
            }

            if ( GUILayout.Button( foldoutIcon, _foldoutButtonStyle ) )
            {
                _showGridTools = false;
            }
        }

        #endregion

    }
}