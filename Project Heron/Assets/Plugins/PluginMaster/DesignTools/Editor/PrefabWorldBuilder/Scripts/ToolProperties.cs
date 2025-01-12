using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace PluginMaster
{
    public class ToolProperties : EditorWindow
    {

        #region Private Methods

        #region ERASER

        private void EraserGroup()
        {
            EditorGUIUtility.labelWidth = 60;
            EraserSettings settings = EraserManager.settings;
            using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
            {
                RadiusSlider( settings );
            }

            ModifierGroup( settings );

            using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
            {
                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    bool outermostFilter = EditorGUILayout.ToggleLeft( "Outermost prefab filter",
                        settings.outermostPrefabFilter );
                    if ( check.changed )
                    {
                        settings.outermostPrefabFilter = outermostFilter;
                    }
                }

                if ( !settings.outermostPrefabFilter )
                {
                    GUILayout.Label( "When you delete a child of a prefab, the prefab will be unpacked.",
                        EditorStyles.helpBox );
                }
            }
        }

        #endregion

        #endregion

        #region COMMON

        private const  string         UNDO_MSG            = "Tool properties";
        private        Vector2        _mainScrollPosition = Vector2.zero;
        private        GUIContent     _updateButtonContent;
        private static ToolProperties _instance;

        [MenuItem( "Tools/Plugin Master/Prefab World Builder/Tool Properties...", false, 1130 )]
        public static void ShowWindow() => _instance = GetWindow<ToolProperties>( "Tool Properties" );

        public static void RepainWindow()
        {
            if ( _instance != null )
            {
                _instance.Repaint();
            }
        }

        public static void CloseWindow()
        {
            if ( _instance != null )
            {
                _instance.Close();
            }
        }

        private void OnEnable()
        {
            if ( BrushManager.settings.paintOnMeshesWithoutCollider )
            {
                PWBCore.UpdateTempColliders();
            }

            _updateButtonContent
                = new GUIContent( Resources.Load<Texture2D>( "Sprites/Update" ), "Update Temp Colliders" );
            Undo.undoRedoPerformed += Repaint;
        }

        private void OnDisable()
        {
            PWBCore.DestroyTempColliders();
            Undo.undoRedoPerformed -= Repaint;
        }

        private void OnGUI()
        {
            if ( _instance == null )
            {
                _instance = this;
            }

            using ( EditorGUILayout.ScrollViewScope scrollView = new EditorGUILayout.ScrollViewScope( _mainScrollPosition,
                       false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUIStyle.none ) )
            {
                _mainScrollPosition = scrollView.scrollPosition;
                #if UNITY_2021_2_OR_NEWER
                #else
                if (PWBToolbar.instance == null) PWBToolbar.ShowWindow();
                #endif
                if ( ToolManager.tool == ToolManager.PaintTool.PIN )
                {
                    PinGroup();
                }
                else if ( ToolManager.tool == ToolManager.PaintTool.BRUSH )
                {
                    BrushGroup();
                }
                else if ( ToolManager.tool == ToolManager.PaintTool.ERASER )
                {
                    EraserGroup();
                }
                else if ( ToolManager.tool == ToolManager.PaintTool.GRAVITY )
                {
                    GravityGroup();
                }
                else if ( ToolManager.tool == ToolManager.PaintTool.EXTRUDE )
                {
                    ExtrudeGroup();
                }
                else if ( ToolManager.tool == ToolManager.PaintTool.LINE )
                {
                    LineGroup();
                }
                else if ( ToolManager.tool == ToolManager.PaintTool.SHAPE )
                {
                    ShapeGroup();
                }
                else if ( ToolManager.tool == ToolManager.PaintTool.TILING )
                {
                    TilingGroup();
                }
                else if ( ToolManager.tool == ToolManager.PaintTool.SELECTION )
                {
                    SelectionGroup();
                }
                else if ( ToolManager.tool == ToolManager.PaintTool.MIRROR )
                {
                    MirrorGroup();
                }
                else if ( ToolManager.tool == ToolManager.PaintTool.REPLACER )
                {
                    ReplacerGroup();
                }
            }

            if ( Event.current.type      == EventType.MouseDown
                 && Event.current.button == 0 )
            {
                GUI.FocusControl( null );
                Repaint();
            }
        }

        public static void ClearUndo()
        {
            if ( _instance == null )
            {
                return;
            }

            Undo.ClearUndo( _instance );
        }

        #endregion

        #region UNDO

        [SerializeField] private LineData       _lineData       = LineData.instance;
        [SerializeField] private TilingData     _tilingData     = TilingData.instance;
        [SerializeField] private MirrorSettings _mirrorSettings = MirrorManager.settings;
        [SerializeField] private ShapeData      _shapeData      = ShapeData.instance;
        [SerializeField] private TilingManager  _tilingManager  = TilingManager.instance as TilingManager;
        [SerializeField] private ShapeManager   _shapeManager   = ShapeManager.instance as ShapeManager;
        [SerializeField] private LineManager    _lineManager    = LineManager.instance as LineManager;

        public static void RegisterUndo( string commandName )
        {
            if ( _instance != null )
            {
                Undo.RegisterCompleteObjectUndo( _instance, commandName );
            }
        }

        #endregion

        #region TOOL PROFILE

        public class ProfileData
        {

            #region Public Fields

            public readonly string       profileName = string.Empty;
            public readonly IToolManager toolManager;

            #endregion

            #region Public Constructors

            public ProfileData( IToolManager toolManager, string profileName )
            {
                ( this.toolManager, this.profileName ) = ( toolManager, profileName );
            }

            #endregion

        }

        private void ToolProfileGUI( IToolManager toolManager )
        {
            using ( new GUILayout.HorizontalScope( EditorStyles.helpBox ) )
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label( "Tool Profile:" );
                if ( GUILayout.Button( toolManager.selectedProfileName,
                        EditorStyles.popup, GUILayout.MinWidth( 100 ) ) )
                {
                    GUI.FocusControl( null );
                    GenericMenu menu = new GenericMenu();
                    foreach ( string profileName in toolManager.profileNames )
                    {
                        menu.AddItem( new GUIContent( profileName ), profileName == toolManager.selectedProfileName,
                            SelectProfileItem, new ProfileData( toolManager, profileName ) );
                    }

                    menu.AddSeparator( string.Empty );
                    if ( toolManager.selectedProfileName != ToolProfile.DEFAULT )
                    {
                        menu.AddItem( new GUIContent( "Save" ),
                            false, SaveProfile, toolManager );
                    }

                    menu.AddItem( new GUIContent( "Save As..." ), false, SaveProfileAs,
                        new ProfileData( toolManager, toolManager.selectedProfileName ) );
                    if ( toolManager.selectedProfileName != ToolProfile.DEFAULT )
                    {
                        menu.AddItem( new GUIContent( "Delete Selected Profile" ), false, DeleteProfile,
                            new ProfileData( toolManager, toolManager.selectedProfileName ) );
                    }

                    menu.AddItem( new GUIContent( "Revert Selected Profile" ), false, RevertProfile, toolManager );
                    menu.AddItem( new GUIContent( "Factory Reset Selected Profile" ), false,
                        FactoryResetProfile, toolManager );
                    menu.ShowAsContext();
                }
            }
        }

        private void SelectProfile( ProfileData profileData )
        {

            GUI.FocusControl( null );
            profileData.toolManager.selectedProfileName = profileData.profileName;
            Repaint();
            if ( ToolManager.tool == ToolManager.PaintTool.MIRROR )
            {
                SceneView.lastActiveSceneView.LookAt( MirrorManager.settings.mirrorPosition );
            }
            else if ( ToolManager.tool == ToolManager.PaintTool.LINE )
            {
                LineManager.settings.OnDataChanged();
            }

            SceneView.RepaintAll();
        }

        private void SelectProfileItem( object value ) => SelectProfile( value as ProfileData );

        public static void SetProfile( ProfileData profileData )
        {
            if ( _instance != null )
            {
                _instance.SelectProfile( profileData );
            }
        }

        private void SaveProfile( object value )
        {

            IToolManager manager = value as IToolManager;
            manager.SaveProfile();
        }

        private void SaveProfileAs( object value )
        {
            ProfileData profiledata = value as ProfileData;
            SaveProfileWindow.ShowWindow( profiledata, OnSaveProfileDone );
        }

        private void OnSaveProfileDone( IToolManager toolManager, string profileName )
        {

            toolManager.SaveProfileAs( profileName );
            Repaint();
        }

        private class SaveProfileWindow : EditorWindow
        {

            #region Public Methods

            public static void ShowWindow( ProfileData data, Action<IToolManager, string> OnDone )
            {
                SaveProfileWindow window = GetWindow<SaveProfileWindow>( true, "Save Profile" );
                window._toolManager         = data.toolManager;
                window._profileName         = data.profileName;
                window.OnDone               = OnDone;
                window.minSize              = window.maxSize = new Vector2( 160, 50 );
                EditorGUIUtility.labelWidth = 70;
                EditorGUIUtility.fieldWidth = 70;
            }

            #endregion

            #region Unity Functions

            private void OnGUI()
            {
                const string textFieldName = "NewProfileName";
                GUI.SetNextControlName( textFieldName );
                _profileName = EditorGUILayout.TextField( _profileName ).Trim();
                GUI.FocusControl( textFieldName );
                using ( new EditorGUI.DisabledGroupScope( _profileName == string.Empty ) )
                {
                    if ( GUILayout.Button( "Save" ) )
                    {
                        OnDone( _toolManager, _profileName );
                        Close();
                    }
                }
            }

            #endregion

            #region Private Fields

            private string                       _profileName = string.Empty;
            private IToolManager                 _toolManager;
            private Action<IToolManager, string> OnDone;

            #endregion

        }

        private void DeleteProfile( object value )
        {

            ProfileData profiledata = value as ProfileData;
            profiledata.toolManager.DeleteProfile();
            if ( ToolManager.tool == ToolManager.PaintTool.MIRROR )
            {
                SceneView.lastActiveSceneView.LookAt( MirrorManager.settings.mirrorPosition );
            }
        }

        private void RevertProfile( object value )
        {

            IToolManager manager = value as IToolManager;
            manager.Revert();
            if ( ToolManager.tool == ToolManager.PaintTool.MIRROR )
            {
                SceneView.lastActiveSceneView.LookAt( MirrorManager.settings.mirrorPosition );
            }
        }

        private void FactoryResetProfile( object value )
        {

            IToolManager manager = value as IToolManager;
            manager.FactoryReset();
            if ( ToolManager.tool == ToolManager.PaintTool.MIRROR )
            {
                SceneView.lastActiveSceneView.LookAt( MirrorManager.settings.mirrorPosition );
            }
        }

        #endregion

        #region COMMON PAINT SETTINGS

        private static float _maxRadius = 50f;

        private static Vector3[] _dir =
        {
            Vector3.right, Vector3.left,
            Vector3.up, Vector3.down,
            Vector3.forward, Vector3.back,
        };

        private static string[] _dirNames = { "+X", "-X", "+Y", "-Y", "+Z", "-Z" };

        private static readonly string[] _brushShapeOptions = { "Point", "Circle", "Square" };
        private static readonly string[] _spacingOptions    = { "Auto", "Custom" };

        private void PaintSettingsGUI( IPaintOnSurfaceToolSettings paintOnSurfaceSettings,
                                       IPaintToolSettings          paintSettings )
        {
            using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
            {
                void UpdateTempColliders()
                {
                    if ( paintOnSurfaceSettings.paintOnMeshesWithoutCollider )
                    {
                        PWBCore.UpdateTempColliders();
                    }
                    else
                    {
                        PWBCore.DestroyTempColliders();
                    }
                }

                using ( new EditorGUI.DisabledGroupScope( PWBCore.staticData.tempCollidersAction == PWBData.TempCollidersAction.NEVER_CREATE ) )
                {
                    using ( new GUILayout.HorizontalScope() )
                    {
                        EditorGUIUtility.labelWidth = 150;
                        using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                        {
                            bool paintOnMeshesWithoutCollider
                                = EditorGUILayout.ToggleLeft( "Paint on meshes without collider",
                                    paintOnSurfaceSettings.paintOnMeshesWithoutCollider );
                            if ( check.changed )
                            {
                                paintOnSurfaceSettings.paintOnMeshesWithoutCollider = paintOnMeshesWithoutCollider;
                                UpdateTempColliders();
                                SceneView.RepaintAll();
                            }
                        }

                        using ( new EditorGUI.DisabledGroupScope( !paintOnSurfaceSettings.paintOnMeshesWithoutCollider ) )
                        {
                            if ( GUILayout.Button( _updateButtonContent, GUILayout.Width( 21 ), GUILayout.Height( 21 ) ) )
                            {
                                PWBCore.UpdateTempColliders();
                            }
                        }
                    }
                }

                EditorGUIUtility.labelWidth = 110;
                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    bool paintOnPalettePrefabs = EditorGUILayout.ToggleLeft( "Paint on palette prefabs",
                        paintOnSurfaceSettings.paintOnPalettePrefabs );
                    bool paintOnSelectedOnly = EditorGUILayout.ToggleLeft( "Paint on selected only",
                        paintOnSurfaceSettings.paintOnSelectedOnly );
                    if ( check.changed )
                    {
                        paintOnSurfaceSettings.paintOnPalettePrefabs = paintOnPalettePrefabs;
                        paintOnSurfaceSettings.paintOnSelectedOnly   = paintOnSelectedOnly;
                        UpdateTempColliders();
                        SceneView.RepaintAll();
                    }
                }
            }

            PaintToolSettingsGUI( paintSettings );
        }

        private void PaintToolSettingsGUI( IPaintToolSettings paintSettings )
        {
            using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
            {
                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    bool autoCreateParent
                        = EditorGUILayout.ToggleLeft( "Create parent", paintSettings.autoCreateParent );
                    if ( check.changed )
                    {
                        paintSettings.autoCreateParent = autoCreateParent;
                    }
                }

                if ( !paintSettings.autoCreateParent )
                {
                    paintSettings.setSurfaceAsParent = EditorGUILayout.ToggleLeft( "Set surface as parent",
                        paintSettings.setSurfaceAsParent );
                    if ( !paintSettings.setSurfaceAsParent )
                    {
                        using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                        {
                            Transform parent = (Transform)EditorGUILayout.ObjectField( "Parent Transform:",
                                paintSettings.parent, typeof(Transform), true );
                            if ( check.changed )
                            {
                                paintSettings.parent = parent;
                            }
                        }
                    }
                }

                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    bool createSubparent = EditorGUILayout.ToggleLeft( "Create sub-parents per palette",
                        paintSettings.createSubparentPerPalette );
                    if ( check.changed )
                    {
                        paintSettings.createSubparentPerPalette = createSubparent;
                    }
                }

                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    bool createSubparent = EditorGUILayout.ToggleLeft( "Create sub-parents per tool",
                        paintSettings.createSubparentPerTool );
                    if ( check.changed )
                    {
                        paintSettings.createSubparentPerTool = createSubparent;
                    }
                }

                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    bool createSubparent = EditorGUILayout.ToggleLeft( "Create sub-parents per brush",
                        paintSettings.createSubparentPerBrush );
                    if ( check.changed )
                    {
                        paintSettings.createSubparentPerBrush = createSubparent;
                    }
                }

                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    bool createSubparent = EditorGUILayout.ToggleLeft( "Create sub-parents per prefab",
                        paintSettings.createSubparentPerPrefab );
                    if ( check.changed )
                    {

                        paintSettings.createSubparentPerPrefab = createSubparent;
                    }
                }

            }

            using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
            {
                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    bool overwritePrefabLayer = EditorGUILayout.ToggleLeft( "Overwrite prefab layer",
                        paintSettings.overwritePrefabLayer );
                    int layer = paintSettings.layer;
                    if ( paintSettings.overwritePrefabLayer )
                    {
                        layer = EditorGUILayout.LayerField( "Layer:",
                            paintSettings.layer );
                    }

                    if ( check.changed )
                    {
                        paintSettings.overwritePrefabLayer = overwritePrefabLayer;
                        paintSettings.layer                = layer;
                        SceneView.RepaintAll();
                    }
                }
            }
        }

        private void RadiusSlider( CircleToolBase settings )
        {
            using ( new GUILayout.HorizontalScope() )
            {
                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    if ( settings.radius > _maxRadius )
                    {
                        _maxRadius = Mathf.Max( Mathf.Floor( settings.radius / 10 ) * 20f, 10f );
                    }

                    EditorGUIUtility.labelWidth = 60;
                    float radius = EditorGUILayout.Slider( "Radius:", settings.radius, 0.05f, _maxRadius );
                    if ( check.changed )
                    {
                        settings.radius = radius;
                        SceneView.RepaintAll();
                    }
                }

                if ( GUILayout.Button( "|>", GUILayout.Width( 20 ) ) )
                {
                    _maxRadius *= 2f;
                }

                if ( GUILayout.Button( "|<", GUILayout.Width( 20 ) ) )
                {
                    _maxRadius = Mathf.Min( Mathf.Floor( settings.radius / 10f ) * 10f + 10f, _maxRadius );
                }
            }
        }

        private void BrushToolBaseSettingsGUI( BrushToolBase settings )
        {
            using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
            {
                EditorGUIUtility.labelWidth = 60;
                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    BrushToolBase.BrushShape brushShape = (BrushToolBase.BrushShape)EditorGUILayout.Popup( "Shape:",
                        (int)settings.brushShape, _brushShapeOptions );
                    if ( check.changed )
                    {
                        settings.brushShape = brushShape;
                        SceneView.RepaintAll();
                    }
                }

                if ( settings.brushShape != BrushToolBase.BrushShape.POINT )
                {
                    using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
                    {
                        using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                        {
                            bool randomize
                                = EditorGUILayout.ToggleLeft( "Randomize positions", settings.randomizePositions );
                            if ( check.changed )
                            {
                                settings.randomizePositions = randomize;
                                SceneView.RepaintAll();
                            }
                        }

                        using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                        {
                            EditorGUIUtility.labelWidth = 80;
                            float randomness = EditorGUILayout.Slider( "Randomness:", settings.randomness, 0f, 1f );
                            if ( check.changed )
                            {
                                settings.randomness = randomness;
                                SceneView.RepaintAll();
                            }

                            EditorGUIUtility.labelWidth = 60;
                        }
                    }

                    RadiusSlider( settings );
                }

                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    int density = EditorGUILayout.IntSlider( "Density:", settings.density, 0, 100 );
                    if ( check.changed )
                    {
                        settings.density = density;
                        SceneView.RepaintAll();
                    }
                }

                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
                    {
                        EditorGUIUtility.labelWidth = 90;
                        BrushToolBase.SpacingType spacingType = (BrushToolBase.SpacingType)EditorGUILayout.Popup( "Min Spacing:",
                            (int)settings.spacingType, _spacingOptions );
                        float spacing = settings.minSpacing;
                        using ( new EditorGUI.DisabledGroupScope( spacingType != BrushToolBase.SpacingType.CUSTOM ) )
                        {
                            spacing = EditorGUILayout.FloatField( "Value:", settings.minSpacing );
                        }

                        if ( check.changed )
                        {
                            settings.spacingType = spacingType;
                            settings.minSpacing  = spacing;
                            SceneView.RepaintAll();
                        }
                    }
                }

                using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
                {
                    using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                    {
                        bool orientAlongBrushstroke = EditorGUILayout.ToggleLeft( "Orient Along the Brushstroke",
                            settings.orientAlongBrushstroke );
                        Vector3 additionalAngle = settings.additionalOrientationAngle;
                        if ( orientAlongBrushstroke )
                        {
                            additionalAngle = EditorGUILayout.Vector3Field( "Additonal angle:", additionalAngle );
                        }

                        if ( check.changed )
                        {
                            settings.orientAlongBrushstroke     = orientAlongBrushstroke;
                            settings.additionalOrientationAngle = additionalAngle;
                            SceneView.RepaintAll();
                        }
                    }
                }
            }
        }

        private void EmbedInSurfaceSettingsGUI( SelectionToolBaseBasic settings )
        {
            using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
            {
                using ( new EditorGUI.DisabledGroupScope( PWBCore.staticData.tempCollidersAction == PWBData.TempCollidersAction.NEVER_CREATE ) )
                {
                    using ( new GUILayout.HorizontalScope() )
                    {
                        using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                        {
                            bool createTempColliders = EditorGUILayout.ToggleLeft( "Create Temp Colliders",
                                settings.createTempColliders );
                            if ( check.changed )
                            {
                                settings.createTempColliders = createTempColliders;
                                PWBCore.UpdateTempColliders();
                                SceneView.RepaintAll();
                            }
                        }

                        using ( new EditorGUI.DisabledGroupScope( !settings.createTempColliders ) )
                        {
                            if ( GUILayout.Button( _updateButtonContent, GUILayout.Width( 21 ), GUILayout.Height( 21 ) ) )
                            {
                                PWBCore.UpdateTempColliders();
                            }
                        }
                    }
                }

                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    EditorGUIUtility.labelWidth = 60;
                    bool embedInSurface = EditorGUILayout.ToggleLeft( "Embed On the Surface",
                        settings.embedInSurface );
                    if ( check.changed )
                    {
                        settings.embedInSurface = embedInSurface;
                        if ( embedInSurface && settings is SelectionToolSettings )
                        {
                            PWBIO.EmbedSelectionInSurface();
                        }

                        SceneView.RepaintAll();
                    }
                }

                if ( settings.embedInSurface )
                {
                    using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                    {
                        bool embedAtPivotHeight = EditorGUILayout.ToggleLeft( "Embed At Pivot Height",
                            settings.embedAtPivotHeight );
                        if ( check.changed )
                        {
                            settings.embedAtPivotHeight = embedAtPivotHeight;
                            if ( settings.embedInSurface
                                 && settings is SelectionToolSettings )
                            {
                                PWBIO.EmbedSelectionInSurface();
                            }

                            SceneView.RepaintAll();
                        }
                    }

                    using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                    {
                        EditorGUIUtility.labelWidth = 110;
                        float surfaceDistance = EditorGUILayout.FloatField( "Surface Distance:",
                            settings.surfaceDistance );
                        if ( check.changed )
                        {
                            settings.surfaceDistance = surfaceDistance;
                            if ( settings is SelectionToolSettings )
                            {
                                PWBIO.EmbedSelectionInSurface();
                            }

                            SceneView.RepaintAll();
                        }
                    }

                    if ( settings is SelectionToolBase )
                    {
                        using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                        {
                            SelectionToolBase selectionSettings = settings as SelectionToolBase;
                            bool rotateToTheSurface = EditorGUILayout.ToggleLeft( "Rotate To the Surface",
                                selectionSettings.rotateToTheSurface );
                            if ( check.changed )
                            {
                                selectionSettings.rotateToTheSurface = rotateToTheSurface;
                                if ( settings.embedInSurface
                                     && settings is SelectionToolSettings )
                                {
                                    PWBIO.EmbedSelectionInSurface();
                                }

                                SceneView.RepaintAll();
                            }
                        }

                    }
                }
            }
        }

        private struct BrushPropertiesGroupState
        {

            #region Public Fields

            public bool brushFlipGroupOpen;
            public bool brushPosGroupOpen;
            public bool brushRotGroupOpen;
            public bool brushScaleGroupOpen;

            #endregion

        }

        private void OverwriteBrushPropertiesGUI( IPaintToolSettings            settings,
                                                  ref BrushPropertiesGroupState state )
        {
            using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
            {
                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    bool overwriteBrushProperties = EditorGUILayout.ToggleLeft( "Overwrite Brush Properties",
                        settings.overwriteBrushProperties );
                    if ( check.changed )
                    {
                        settings.overwriteBrushProperties = overwriteBrushProperties;
                        SceneView.RepaintAll();
                    }
                }

                if ( PaletteManager.selectedBrush != null )
                {
                    settings.brushSettings.isAsset2D = PaletteManager.selectedBrush.isAsset2D;
                }
                else
                {
                    settings.brushSettings.isAsset2D = false;
                }

                if ( settings.overwriteBrushProperties )
                {
                    BrushProperties.BrushFields( settings.brushSettings,
                        ref state.brushPosGroupOpen, ref state.brushRotGroupOpen,
                        ref state.brushScaleGroupOpen, ref state.brushFlipGroupOpen, this, UNDO_MSG );
                }
            }
        }

        private static readonly string[] _editModeTypeOptions = { "Line nodes", "Line position and rotation" };

        private void EditModeToggle( IPersistentToolManager persistentToolManager )
        {
            using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
            {
                using ( new GUILayout.HorizontalScope() )
                {
                    using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                    {
                        bool editMode = EditorGUILayout.ToggleLeft( "Edit Mode", ToolManager.editMode );
                        if ( check.changed )
                        {
                            ToolManager.editMode = editMode;
                            PWBIO.ResetLineRotation();
                            PWBIO.repaint = true;
                            SceneView.RepaintAll();
                        }
                    }

                    if ( persistentToolManager == LineManager.instance
                         && ToolManager.editMode )
                    {
                        using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                        {
                            LineManager.EditModeType editModeType = (LineManager.EditModeType)EditorGUILayout
                                .Popup( (int)LineManager.editModeType, _editModeTypeOptions );
                            if ( check.changed )
                            {
                                LineManager.editModeType = editModeType;
                                PWBIO.ResetLineRotation();
                                PWBIO.repaint = true;
                                SceneView.RepaintAll();
                            }
                        }
                    }
                }

                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    bool showPreexistingElements = EditorGUILayout.ToggleLeft( "Show Pre-existing elements",
                        persistentToolManager.showPreexistingElements );
                    if ( check.changed )
                    {
                        persistentToolManager.showPreexistingElements = showPreexistingElements;
                        PWBIO.repaint                                 = true;
                        SceneView.RepaintAll();
                    }
                }
            }
        }

        private void HandlePosition()
        {
            if ( PWBIO.selectedPointIdx < 0 )
            {
                return;
            }

            using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
            {
                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    PWBIO.handlePosition = EditorGUILayout.Vector3Field( "Handle position:", PWBIO.handlePosition );
                    if ( check.changed )
                    {
                        PWBIO.UpdateHandlePosition();
                    }
                }
            }
        }

        private void HandleRotation()
        {
            if ( PWBIO.selectedPointIdx < 0 )
            {
                return;
            }

            using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
            {
                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    Vector3 eulerAngles = PWBIO.handleRotation.eulerAngles;
                    eulerAngles = EditorGUILayout.Vector3Field( "Handle rotation:", eulerAngles );
                    if ( check.changed )
                    {
                        Quaternion newRotation = Quaternion.Euler( eulerAngles );
                        PWBIO.handleRotation = newRotation;
                        PWBIO.UpdateHandleRotation();
                    }
                }
            }
        }

        #endregion

        #region MODIFIER SETTINGS

        private static readonly string[] _modifierCommandOptions = { "All", "Palette Prefabs", "Brush Prefabs" };

        private void ModifierGroup( IModifierTool settings )
        {
            string actionLabel = settings is EraserSettings ? "Erase" : "Replace";
            using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
            {
                EditorGUIUtility.labelWidth = 60;
                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    ModifierToolSettings.Command command = (ModifierToolSettings.Command)EditorGUILayout.Popup( actionLabel + ":",
                        (int)settings.command, _modifierCommandOptions );
                    if ( check.changed )
                    {
                        settings.command = command;
                        PWBIO.UpdateOctree();
                    }
                }

                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    bool allButSelected = EditorGUILayout.ToggleLeft( actionLabel + " all but selected",
                        settings.modifyAllButSelected );
                    if ( check.changed )
                    {
                        settings.modifyAllButSelected = allButSelected;
                        PWBIO.UpdateOctree();
                    }
                }

                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    bool onlyTheClosest = EditorGUILayout.ToggleLeft( actionLabel + " only the closest",
                        settings.onlyTheClosest );
                    if ( check.changed )
                    {
                        settings.onlyTheClosest = onlyTheClosest;
                    }
                }
            }
        }

        #endregion

        #region PIN

        private static readonly string[]                  _pinModeNames = { "Auto", "Paint on surface", "Paint on grid" };
        private static          BrushPropertiesGroupState _pinOverwriteGroupState;

        private void PinGroup()
        {
            ToolProfileGUI( PinManager.instance );
            using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
            {
                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    PaintOnSurfaceToolSettingsBase.PaintMode mode = (PaintOnSurfaceToolSettingsBase.PaintMode)EditorGUILayout.Popup( "Paint mode:",
                        (int)PinManager.settings.mode, _pinModeNames );
                    if ( check.changed )
                    {
                        PinManager.settings.mode = mode;
                        SceneView.RepaintAll();
                    }
                }

                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    bool repeat = EditorGUILayout.ToggleLeft( "Repeat multi-brush item", PinManager.settings.repeat );
                    if ( check.changed )
                    {
                        PinManager.settings.repeat = repeat;
                        SceneView.RepaintAll();
                    }
                }

                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    bool avoidOverlapping = EditorGUILayout.ToggleLeft( "Avoid overlapping",
                        PinManager.settings.avoidOverlapping );
                    if ( check.changed )
                    {
                        PinManager.settings.avoidOverlapping = avoidOverlapping;
                        SceneView.RepaintAll();
                    }
                }
            }

            using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
            {
                EditorGUIUtility.labelWidth = 60;
                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    bool flattenTerrain
                        = EditorGUILayout.ToggleLeft( "Flatten the terrain", PinManager.settings.flattenTerrain );
                    if ( check.changed )
                    {
                        PinManager.settings.flattenTerrain = flattenTerrain;
                    }
                }

                using ( new EditorGUI.DisabledGroupScope( !PinManager.settings.flattenTerrain ) )
                {
                    TerrainFlatteningSettings flatteningSettings = PinManager.settings.flatteningSettings;
                    using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                    {
                        float hardness = EditorGUILayout.Slider( "Hardness:", flatteningSettings.hardness, 0, 1 );
                        if ( check.changed )
                        {
                            flatteningSettings.hardness = hardness;
                        }
                    }

                    using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                    {
                        float padding = EditorGUILayout.FloatField( "Padding:", flatteningSettings.padding );
                        if ( check.changed )
                        {
                            flatteningSettings.padding = padding;
                        }
                    }

                    using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                    {
                        bool clearTrees = EditorGUILayout.ToggleLeft( "Clear trees", flatteningSettings.clearTrees );
                        if ( check.changed )
                        {
                            flatteningSettings.clearTrees = clearTrees;
                        }
                    }

                    using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                    {
                        bool clearDetails
                            = EditorGUILayout.ToggleLeft( "Clear details", flatteningSettings.clearDetails );
                        if ( check.changed )
                        {
                            flatteningSettings.clearDetails = clearDetails;
                        }
                    }
                }
            }

            PaintSettingsGUI( PinManager.settings, PinManager.settings );
            OverwriteBrushPropertiesGUI( PinManager.settings, ref _pinOverwriteGroupState );
        }

        #endregion

        #region BRUSH

        private static readonly string[] _heightTypeNames = { "Custom", "Radius" };

        private static readonly string[] _avoidOverlappingTypeNames =
        {
            "Disabled", "With Palette Prefabs",
            "With Brush Prefabs", "With Same Prefabs", "With All Objects",
        };

        private static BrushPropertiesGroupState _brushOverwriteGroupState;

        private void BrushGroup()
        {
            ToolProfileGUI( BrushManager.instance );
            using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
            {
                BrushManager.settings.showPreview = EditorGUILayout.ToggleLeft( "Show Brushstroke Preview",
                    BrushManager.settings.showPreview );
                if ( BrushManager.settings.showPreview )
                {
                    EditorGUILayout.HelpBox( "The brushstroke preview can cause slowdown issues.",
                        MessageType.Info );
                }

                EditorGUILayout.LabelField( "Brushstroke object count:", BrushstrokeManager.itemCount.ToString() );
            }

            using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
            {
                BrushToolBaseSettingsGUI( BrushManager.settings );
                EditorGUIUtility.labelWidth = 150;
                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    BrushToolSettings.AvoidOverlappingType avoidOverlapping = (BrushToolSettings.AvoidOverlappingType)
                        EditorGUILayout.Popup( "Avoid Overlapping:",
                            (int)BrushManager.settings.avoidOverlapping, _avoidOverlappingTypeNames );
                    if ( check.changed )
                    {
                        BrushManager.settings.avoidOverlapping = avoidOverlapping;
                    }
                }

                if ( BrushManager.settings.brushShape != BrushToolBase.BrushShape.POINT )
                {
                    using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
                    {
                        using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                        {
                            BrushToolSettings.HeightType heightType = (BrushToolSettings.HeightType)
                                EditorGUILayout.Popup( "Max Height From center:",
                                    (int)BrushManager.settings.heightType, _heightTypeNames );
                            if ( check.changed )
                            {
                                BrushManager.settings.heightType = heightType;
                                if ( heightType == BrushToolSettings.HeightType.RADIUS )
                                {
                                    BrushManager.settings.maxHeightFromCenter = BrushManager.settings.radius;
                                }

                                SceneView.RepaintAll();
                            }
                        }

                        using ( new EditorGUI.DisabledGroupScope(
                                   BrushManager.settings.heightType == BrushToolSettings.HeightType.RADIUS ) )
                        {
                            using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                            {
                                float maxHeightFromCenter = Mathf.Abs( EditorGUILayout.FloatField( "Value:",
                                    BrushManager.settings.maxHeightFromCenter ) );
                                if ( check.changed )
                                {
                                    BrushManager.settings.maxHeightFromCenter = maxHeightFromCenter;
                                    SceneView.RepaintAll();
                                }
                            }
                        }
                    }
                }
            }

            using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
            {
                GUILayout.Label( "Surface Filters", EditorStyles.boldLabel );
                EditorGUIUtility.labelWidth = 110;
                using ( new GUILayout.HorizontalScope() )
                {
                    using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                    {
                        float minSlope = BrushManager.settings.slopeFilter.min;
                        float maxSlope = BrushManager.settings.slopeFilter.max;
                        EditorGUILayout.MinMaxSlider( "Slope Angle:", ref minSlope, ref maxSlope, 0, 90 );
                        minSlope = Mathf.Round( minSlope );
                        maxSlope = Mathf.Round( maxSlope );
                        GUILayout.Label( "[" + minSlope.ToString( "00" ) + "°," + maxSlope.ToString( "00" ) + "°]" );
                        if ( check.changed )
                        {
                            BrushManager.settings.slopeFilter.v1 = minSlope;
                            BrushManager.settings.slopeFilter.v2 = maxSlope;
                            SceneView.RepaintAll();
                        }
                    }
                }

                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    int mask = EditorGUILayout.MaskField( "Layers:",
                        EditorGUIUtils.LayerMaskToField( BrushManager.settings.layerFilter ),
                        InternalEditorUtility.layers );
                    if ( check.changed )
                    {
                        BrushManager.settings.layerFilter = EditorGUIUtils.FieldToLayerMask( mask );
                        SceneView.RepaintAll();
                    }
                }

                EditorGUIUtility.labelWidth = 108;
                EditorGUIUtils.MultiTagField field = EditorGUIUtils.MultiTagField.Instantiate( "Tags:", BrushManager.settings.tagFilter, null );
                field.OnChange += OnBrushTagFilterChanged;

                bool terrainFilterChanged = false;
                TerrainLayer[] terrainFilter = EditorGUIUtils.ObjectArrayFieldWithButtons( "Terrain Layers:",
                    BrushManager.settings.terrainLayerFilter, ref _terrainLayerFilterFoldout, out terrainFilterChanged );
                if ( terrainFilterChanged )
                {
                    BrushManager.settings.terrainLayerFilter = terrainFilter.ToArray();
                    SceneView.RepaintAll();
                }
            }

            PaintSettingsGUI( BrushManager.settings, BrushManager.settings );
            OverwriteBrushPropertiesGUI( BrushManager.settings, ref _brushOverwriteGroupState );
        }

        private bool _terrainLayerFilterFoldout;

        private void OnBrushTagFilterChanged( List<string> prevFilter,
                                              List<string> newFilter, string key )
        {

            BrushManager.settings.tagFilter = newFilter;
        }

        #endregion

        #region GRAVITY

        private static BrushPropertiesGroupState _gravityOverwriteGroupState;

        private void GravityGroup()
        {
            ToolProfileGUI( GravityToolManager.instance );
            BrushToolBaseSettingsGUI( GravityToolManager.settings );
            EditorGUIUtility.labelWidth = 120;
            using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
            {
                GravityToolSettings settings = GravityToolManager.settings.Clone();
                SimulateGravityData data     = settings.simData;
                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    settings.height      = EditorGUILayout.FloatField( "Height:", settings.height );
                    data.maxIterations   = EditorGUILayout.IntField( "Max Iterations:", data.maxIterations );
                    data.maxSpeed        = EditorGUILayout.FloatField( "Max Speed:",         data.maxSpeed );
                    data.maxAngularSpeed = EditorGUILayout.FloatField( "Max Angular Speed:", data.maxAngularSpeed );
                    data.mass            = EditorGUILayout.FloatField( "Mass:",              data.mass );
                    data.drag            = EditorGUILayout.FloatField( "Drag:",              data.drag );
                    data.angularDrag     = EditorGUILayout.FloatField( "Angular Drag:",      data.angularDrag );
                    if ( check.changed )
                    {
                        GravityToolManager.settings.Copy( settings );
                        SceneView.RepaintAll();
                    }
                }

                using ( new EditorGUI.DisabledGroupScope( PWBCore.staticData.tempCollidersAction == PWBData.TempCollidersAction.NEVER_CREATE ) )
                {
                    using ( new GUILayout.HorizontalScope() )
                    {
                        using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                        {
                            bool createTempColliders = EditorGUILayout.ToggleLeft( "Create Temp Colliders",
                                GravityToolManager.settings.createTempColliders );
                            if ( check.changed )
                            {
                                GravityToolManager.settings.createTempColliders = createTempColliders;
                                PWBCore.UpdateTempColliders();
                                SceneView.RepaintAll();
                            }
                        }

                        using ( new EditorGUI.DisabledGroupScope( !GravityToolManager.settings.createTempColliders ) )
                        {
                            if ( GUILayout.Button( _updateButtonContent, GUILayout.Width( 21 ), GUILayout.Height( 21 ) ) )
                            {
                                PWBCore.UpdateTempColliders();
                            }
                        }

                    }
                }

                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    data.ignoreSceneColliders = EditorGUILayout.ToggleLeft( "Ignore Scene Colliders",
                        data.ignoreSceneColliders );
                    using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
                    {
                        data.changeLayer
                            = EditorGUILayout.ToggleLeft( "Change Layer Temporarily", data.changeLayer );
                        if ( data.changeLayer )
                        {
                            data.tempLayer = EditorGUILayout.LayerField( "Temp layer:", data.tempLayer );
                        }
                    }

                    if ( check.changed )
                    {
                        GravityToolManager.settings.Copy( settings );
                        SceneView.RepaintAll();
                    }
                }
            }

            PaintToolSettingsGUI( GravityToolManager.settings );
            OverwriteBrushPropertiesGUI( GravityToolManager.settings, ref _gravityOverwriteGroupState );
        }

        #endregion

        #region LINE

        private static readonly string[] _lineModeNames             = { "Auto", "Paint on surface", "Paint on the line" };
        private static readonly string[] _lineSpacingNames          = { "Bounds", "Constant" };
        private static readonly string[] _lineAxesAlongTheLineNames = { "X", "Z" };
        private static          string[] _shapeProjDirNames         = { "+X", "-X", "+Y", "-Y", "+Z", "-Z", "Plane Axis" };

        private static int                       _lineProjDirIdx = 6;
        private static BrushPropertiesGroupState _lineOverwriteGroupState;

        private void LineBaseGUI<SETTINGS>( SETTINGS lineSettings ) where SETTINGS : LineSettings
        {
            void OnValueChanged()
            {
                PWBIO.UpdateStroke();
                PWBIO.repaint = true;
            }

            using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
            {
                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    PaintOnSurfaceToolSettingsBase.PaintMode mode = (PaintOnSurfaceToolSettingsBase.PaintMode)
                        EditorGUILayout.Popup( "Paint Mode:", (int)lineSettings.mode, _lineModeNames );
                    if ( check.changed )
                    {
                        lineSettings.mode = mode;
                        OnValueChanged();
                    }
                }

                if ( lineSettings is ShapeSettings )
                {
                    using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                    {
                        bool parallelToTheSurface = EditorGUILayout.ToggleLeft(
                            lineSettings.mode == PaintOnSurfaceToolSettingsBase.PaintMode.ON_SHAPE
                                ? "Place objects perpendicular to the plane"
                                : "Place objects perpendicular to the surface",
                            lineSettings.perpendicularToTheSurface );
                        if ( check.changed )
                        {
                            lineSettings.perpendicularToTheSurface = parallelToTheSurface;
                            OnValueChanged();
                        }
                    }
                }
                else
                {
                    using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                    {
                        bool parallelToTheSurface
                            = EditorGUILayout.ToggleLeft( "Place objects perpendicular to the " + ( lineSettings.mode == PaintOnSurfaceToolSettingsBase.PaintMode.ON_SHAPE ? "line" : "surface" ),
                                lineSettings.perpendicularToTheSurface );
                        if ( check.changed )
                        {
                            lineSettings.perpendicularToTheSurface = parallelToTheSurface;
                            OnValueChanged();
                        }
                    }
                }

                string[]      dirNames      = lineSettings is ShapeSettings ? _shapeProjDirNames : _dirNames;
                ShapeSettings shapeSettings = lineSettings as ShapeSettings;
                if ( shapeSettings != null )
                {
                    _lineProjDirIdx = shapeSettings.projectInNormalDir
                        ? _lineProjDirIdx = 6
                        : Array.IndexOf( _dir, lineSettings.projectionDirection );
                }
                else
                {
                    _lineProjDirIdx = Array.IndexOf( _dir, lineSettings.projectionDirection );
                }

                if ( _lineProjDirIdx == -1 )
                {
                    _lineProjDirIdx = 3;
                }

                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    _lineProjDirIdx = EditorGUILayout.Popup( "Pojection Direction:", _lineProjDirIdx, dirNames );
                    if ( check.changed )
                    {
                        if ( shapeSettings != null )
                        {
                            shapeSettings.projectInNormalDir = _lineProjDirIdx == 6;
                        }

                        lineSettings.projectionDirection = _lineProjDirIdx == 6
                            ? PWBIO.GetShapePlaneNormal()
                            : _dir[ _lineProjDirIdx ];
                        OnValueChanged();
                    }
                }
            }

            using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
            {
                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    bool objectsOrientedAlongTheLine
                        = EditorGUILayout.ToggleLeft( "Orient Along the Line",
                            lineSettings.objectsOrientedAlongTheLine );
                    if ( check.changed )
                    {
                        lineSettings.objectsOrientedAlongTheLine = objectsOrientedAlongTheLine;
                        OnValueChanged();
                    }
                }

                if ( lineSettings.objectsOrientedAlongTheLine )
                {
                    EditorGUIUtility.labelWidth = 170;
                    using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                    {
                        AxesUtils.Axis axisOrientedAlongTheLine = EditorGUILayout.Popup( "Axis Oriented Along the Line:",
                                                                      lineSettings.axisOrientedAlongTheLine == AxesUtils.Axis.X ? 0 : 1,
                                                                      _lineAxesAlongTheLineNames )
                                                                  == 0
                            ? AxesUtils.Axis.X
                            : AxesUtils.Axis.Z;
                        if ( check.changed )
                        {
                            lineSettings.axisOrientedAlongTheLine = axisOrientedAlongTheLine;
                            OnValueChanged();
                        }
                    }
                }
            }

            using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
            {
                EditorGUIUtility.labelWidth = 120;
                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    LineSettings.SpacingType spacingType = (LineSettings.SpacingType)
                        EditorGUILayout.Popup( "Spacing:", (int)lineSettings.spacingType, _lineSpacingNames );
                    if ( check.changed )
                    {
                        lineSettings.spacingType = spacingType;
                        OnValueChanged();
                    }
                }

                if ( lineSettings.spacingType == LineSettings.SpacingType.CONSTANT )
                {
                    using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                    {
                        float spacing = EditorGUILayout.FloatField( "Value:", lineSettings.spacing );
                        if ( check.changed )
                        {
                            lineSettings.spacing = spacing;
                            OnValueChanged();
                        }
                    }
                }

                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    float gapSize = EditorGUILayout.FloatField( "Gap Size:", lineSettings.gapSize );
                    if ( check.changed )
                    {
                        if ( PaletteManager.selectedBrushIdx >= 0
                             && PaletteManager.selectedBrush != null )
                        {
                            float spacing = lineSettings.spacingType == LineSettings.SpacingType.CONSTANT
                                ? lineSettings.spacing
                                : PaletteManager.selectedBrush.minBrushMagnitude;
                            float min = Mathf.Min( 0, 0.05f - spacing );
                            gapSize = Mathf.Max( min, gapSize );
                        }

                        lineSettings.gapSize = gapSize;
                        OnValueChanged();
                    }
                }
            }
        }

        private void LineGroup()
        {
            ToolProfileGUI( LineManager.instance );
            EditModeToggle( LineManager.instance );
            HandlePosition();
            EditorGUIUtility.labelWidth = 120;
            LineBaseGUI( LineManager.settings );
            PaintSettingsGUI( LineManager.settings, LineManager.settings );
            OverwriteBrushPropertiesGUI( LineManager.settings, ref _lineOverwriteGroupState );
        }

        #endregion

        #region SHAPE

        private static readonly string[]                  _shapeTypeNames = { "Circle", "Polygon" };
        private static          BrushPropertiesGroupState _shapeOverwriteGroupState;
        private static          string[]                  _shapeDirNames = { "+X", "-X", "+Y", "-Y", "+Z", "-Z", "Normal to surface" };

        private void ShapeGroup()
        {
            EditorGUIUtility.labelWidth = 100;
            ToolProfileGUI( ShapeManager.instance );
            EditModeToggle( ShapeManager.instance );
            HandlePosition();
            HandleRotation();
            using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
            {
                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    ShapeSettings.ShapeType shapeType = (ShapeSettings.ShapeType)EditorGUILayout.Popup( "Shape:",
                        (int)ShapeManager.settings.shapeType, _shapeTypeNames );
                    if ( check.changed )
                    {
                        ShapeManager.settings.shapeType = shapeType;
                        if ( shapeType == ShapeSettings.ShapeType.CIRCLE )
                        {
                            ShapeData.instance.UpdateCircleSideCount();
                        }

                        ShapeData.instance.Update( true );
                        PWBIO.UpdateStroke();
                        PWBIO.repaint = true;
                    }
                }

                if ( ShapeManager.settings.shapeType == ShapeSettings.ShapeType.POLYGON )
                {
                    using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                    {
                        int sideCount = EditorGUILayout.IntSlider( "Number of sides:",
                            ShapeManager.settings.sidesCount, 3, 12 );
                        if ( check.changed )
                        {
                            ShapeManager.settings.sidesCount = sideCount;
                            ShapeData.instance.UpdateIntersections();
                            PWBIO.UpdateStroke();
                            PWBIO.repaint = true;
                        }
                    }
                }

                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    int normalDirIdx = ShapeManager.settings.axisNormalToSurface
                        ? 6
                        : Array.IndexOf( _dir, ShapeManager.settings.normal );
                    EditorGUIUtility.labelWidth = 120;
                    normalDirIdx                = EditorGUILayout.Popup( "Initial axis direction:", normalDirIdx, _shapeDirNames );
                    bool axisNormalToSurface = normalDirIdx == 6;
                    if ( check.changed )
                    {
                        ShapeManager.settings.axisNormalToSurface = axisNormalToSurface;
                        ShapeManager.settings.normal              = normalDirIdx == 6 ? Vector3.up : _dir[ normalDirIdx ];
                        PWBIO.UpdateStroke();
                        PWBIO.repaint = true;
                    }
                }
            }

            EditorGUIUtility.labelWidth = 120;
            LineBaseGUI( ShapeManager.settings );
            PaintSettingsGUI( ShapeManager.settings, ShapeManager.settings );
            OverwriteBrushPropertiesGUI( ShapeManager.settings, ref _shapeOverwriteGroupState );
        }

        #endregion

        #region TILING

        private static readonly string[]                  _tilingModeNames     = { "Auto", "Paint on surface", "Paint on the plane" };
        private static readonly string[]                  _tilingCellTypeNames = { "Smallest object", "Biggest object", "Custom" };
        private static          BrushPropertiesGroupState _tilingOverwriteGroupState;

        private void TilingGroup()
        {
            ToolProfileGUI( TilingManager.instance );
            EditModeToggle( TilingManager.instance );
            HandlePosition();
            if ( !ToolManager.editMode )
            {
                using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
                {
                    TilingManager.settings.showPreview = EditorGUILayout.ToggleLeft( "Show Preview",
                        TilingManager.settings.showPreview );
                    if ( TilingManager.settings.showPreview )
                    {
                        EditorGUILayout.HelpBox( "If you experience slowdown issues, disable preview.",
                            MessageType.Info );
                    }

                    EditorGUILayout.LabelField( "Object count:", BrushstrokeManager.itemCount.ToString() );
                }
            }

            EditorGUIUtility.labelWidth = 180;
            using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
            {
                TilingSettings settings = TilingManager.settings;
                using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
                {
                    settings.mode = (PaintOnSurfaceToolSettingsBase.PaintMode)EditorGUILayout.Popup( "Paint mode:",
                        (int)settings.mode, _tilingModeNames );
                    using ( EditorGUI.ChangeCheckScope angleCheck = new EditorGUI.ChangeCheckScope() )
                    {
                        Vector3 eulerAngles = settings.rotation.eulerAngles;
                        eulerAngles = EditorGUILayout.Vector3Field( "Plane Rotation:", eulerAngles );
                        if ( angleCheck.changed )
                        {
                            Quaternion newRotation = Quaternion.Euler( eulerAngles );
                            PWBIO.UpdateTilingRotation( newRotation );
                            settings.rotation = newRotation;
                        }
                    }

                    int axisIdx = EditorGUILayout.Popup( "Axis aligned with plane normal: ",
                        settings.axisAlignedWithNormal, _dirNames );
                    settings.axisAlignedWithNormal = axisIdx;
                }

                using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
                {
                    EditorGUIUtility.labelWidth = 76;
                    settings.cellSizeType = (TilingSettings.CellSizeType)
                        EditorGUILayout.Popup( "Cell size:", (int)settings.cellSizeType, _tilingCellTypeNames );
                    using ( new EditorGUI.DisabledGroupScope(
                               settings.cellSizeType != TilingSettings.CellSizeType.CUSTOM ) )
                    {
                        settings.cellSize = EditorGUILayout.Vector2Field( "", settings.cellSize );
                    }
                }

                using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
                {
                    settings.spacing = EditorGUILayout.Vector2Field( "Spacing", settings.spacing );
                }

                if ( check.changed )
                {
                    PWBIO.UpdateStroke();
                    SceneView.RepaintAll();
                }
            }

            PaintSettingsGUI( TilingManager.settings, TilingManager.settings );
            OverwriteBrushPropertiesGUI( TilingManager.settings, ref _tilingOverwriteGroupState );
        }

        #endregion

        #region EXTRUDE

        private static readonly string[] _spaceOptions          = { "Global", "Local" };
        private static readonly string[] _rotationOptions       = { "First Object Selected", "Last Object Selected" };
        private static readonly string[] _extrudeSpacingOptions = { "Box Size", "Custom" };
        private static readonly string[] _addRotationOptions    = { "Constant", "Random" };

        private void ExtrudeGroup()
        {
            ToolProfileGUI( ExtrudeManager.instance );
            EditorGUIUtility.labelWidth = 60;
            using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
            {
                ExtrudeSettings extrudeSettings = ExtrudeManager.settings.Clone();
                using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
                {
                    extrudeSettings.space = (Space)EditorGUILayout.Popup( "Space:",
                        (int)extrudeSettings.space, _spaceOptions );
                    if ( extrudeSettings.space == Space.Self )
                    {
                        EditorGUIUtility.labelWidth = 150;
                        extrudeSettings.rotationAccordingTo = (ExtrudeSettings.RotationAccordingTo)EditorGUILayout.Popup( "Set rotation according to:",
                            (int)extrudeSettings.rotationAccordingTo, _rotationOptions );
                    }
                }

                EditorGUIUtility.labelWidth = 60;
                using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
                {
                    extrudeSettings.spacingType = (ExtrudeSettings.SpacingType)EditorGUILayout.Popup( "Spacing:",
                        (int)extrudeSettings.spacingType, _extrudeSpacingOptions );
                    if ( extrudeSettings.spacingType == ExtrudeSettings.SpacingType.BOX_SIZE )
                    {
                        extrudeSettings.multiplier
                            = EditorGUILayout.Vector3Field( "Multiplier:", extrudeSettings.multiplier );
                    }
                    else
                    {
                        extrudeSettings.spacing
                            = EditorGUILayout.Vector3Field( "Value:", extrudeSettings.spacing );
                    }
                }

                if ( extrudeSettings.space == Space.World )
                {
                    using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
                    {
                        EditorGUIUtility.labelWidth = 80;
                        extrudeSettings.addRandomRotation = EditorGUILayout.Popup( "Add Rotation:",
                                                                extrudeSettings.addRandomRotation ? 1 : 0, _addRotationOptions )
                                                            == 1;
                        if ( extrudeSettings.addRandomRotation )
                        {
                            extrudeSettings.randomEulerOffset = EditorGUIUtils.Range3Field( string.Empty,
                                extrudeSettings.randomEulerOffset );
                            using ( new GUILayout.HorizontalScope() )
                            {
                                extrudeSettings.rotateInMultiples = EditorGUILayout.ToggleLeft
                                    ( "Only in multiples of:", extrudeSettings.rotateInMultiples );
                                using ( new EditorGUI.DisabledGroupScope( !extrudeSettings.rotateInMultiples ) )
                                {
                                    extrudeSettings.rotationFactor
                                        = EditorGUILayout.FloatField( extrudeSettings.rotationFactor );
                                }
                            }
                        }
                        else
                        {
                            extrudeSettings.eulerOffset = EditorGUILayout.Vector3Field( string.Empty,
                                extrudeSettings.eulerOffset );
                        }
                    }
                }

                using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
                {
                    extrudeSettings.sameParentAsSource
                        = EditorGUILayout.ToggleLeft( "Same parent as source", extrudeSettings.sameParentAsSource );
                    if ( !extrudeSettings.sameParentAsSource )
                    {
                        extrudeSettings.autoCreateParent
                            = EditorGUILayout.ToggleLeft( "Create parent", extrudeSettings.autoCreateParent );
                        if ( extrudeSettings.autoCreateParent )
                        {
                            extrudeSettings.createSubparentPerPrefab
                                = EditorGUILayout.ToggleLeft( "Create sub-parent per prefab",
                                    extrudeSettings.createSubparentPerPrefab );
                        }
                        else
                        {
                            extrudeSettings.parent = (Transform)EditorGUILayout.ObjectField( "Parent Transform:",
                                extrudeSettings.parent, typeof(Transform), true );
                        }
                    }
                }

                using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
                {
                    extrudeSettings.overwritePrefabLayer
                        = EditorGUILayout.ToggleLeft( "Overwrite prefab layer",
                            extrudeSettings.overwritePrefabLayer );
                    if ( extrudeSettings.overwritePrefabLayer )
                    {
                        extrudeSettings.layer = EditorGUILayout.LayerField( "Layer:", extrudeSettings.layer );
                    }
                }

                if ( check.changed )
                {
                    ExtrudeManager.settings.Copy( extrudeSettings );
                    SceneView.RepaintAll();
                    PWBIO.ClearExtrudeAngles();
                }
            }

            EmbedInSurfaceSettingsGUI( ExtrudeManager.settings );
        }

        #endregion

        #region SELECTION TOOL

        private void SelectionGroup()
        {
            ToolProfileGUI( SelectionToolManager.instance );
            using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
            {
                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    EditorGUIUtility.labelWidth = 90;
                    Space handleSpace = (Space)EditorGUILayout.Popup( "Handle Space:",
                        (int)SelectionToolManager.settings.handleSpace, _spaceOptions );
                    if ( SelectionManager.topLevelSelection.Length > 1 )
                    {
                        SelectionToolManager.settings.boxSpace = Space.World;
                    }

                    Space boxSpace = SelectionToolManager.settings.boxSpace;
                    using ( new EditorGUI.DisabledGroupScope( SelectionManager.topLevelSelection.Length > 1 ) )
                    {
                        boxSpace = (Space)EditorGUILayout.Popup( "Box Space:",
                            (int)SelectionToolManager.settings.boxSpace, _spaceOptions );
                    }

                    if ( check.changed )
                    {
                        SelectionToolManager.settings.handleSpace = handleSpace;
                        SelectionToolManager.settings.boxSpace    = boxSpace;
                        PWBIO.ResetSelectionRotation();
                        SceneView.RepaintAll();
                    }
                }
            }

            using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
            {
                GUILayout.Label( "Selection Filters", EditorStyles.boldLabel );
                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    EditorGUIUtility.labelWidth = 90;
                    bool paletteFilter = EditorGUILayout.ToggleLeft( "Prefabs from selected palette only",
                        SelectionToolManager.settings.paletteFilter );
                    bool brushFilter = EditorGUILayout.ToggleLeft( "Prefabs from selected brush only",
                        SelectionToolManager.settings.brushFilter );
                    int layerMask = EditorGUILayout.MaskField( "Layers:",
                        EditorGUIUtils.LayerMaskToField( SelectionToolManager.settings.layerFilter ),
                        InternalEditorUtility.layers );
                    EditorGUIUtils.MultiTagField tagField = EditorGUIUtils.MultiTagField.Instantiate( "Tags:",
                        SelectionToolManager.settings.tagFilter, null );
                    tagField.OnChange += OnSelectionTagFilterChanged;
                    if ( check.changed )
                    {
                        SelectionToolManager.settings.paletteFilter = paletteFilter;
                        SelectionToolManager.settings.brushFilter   = brushFilter;
                        SelectionToolManager.settings.layerFilter   = EditorGUIUtils.FieldToLayerMask( layerMask );
                        PWBIO.ApplySelectionFilters();
                        SceneView.RepaintAll();
                    }
                }
            }

            EmbedInSurfaceSettingsGUI( SelectionToolManager.settings );
        }

        private void OnSelectionTagFilterChanged( List<string> prevFilter,
                                                  List<string> newFilter, string key )
        {

            SelectionToolManager.settings.tagFilter = newFilter;
            PWBIO.ApplySelectionFilters();
            SceneView.RepaintAll();
        }

        #endregion

        #region MIRROR

        private static readonly string[] _mirrorActionNames = { "Transform", "Create" };

        private void MirrorGroup()
        {
            ToolProfileGUI( MirrorManager.instance );
            using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
            {
                MirrorSettings mirrorSettings = new MirrorSettings();
                MirrorManager.settings.Clone( mirrorSettings );
                using ( EditorGUI.ChangeCheckScope mirrorCheck = new EditorGUI.ChangeCheckScope() )
                {
                    using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
                    {
                        EditorGUIUtility.labelWidth = 80;
                        mirrorSettings.mirrorPosition = EditorGUILayout.Vector3Field( "Position:",
                            mirrorSettings.mirrorPosition );
                        mirrorSettings.mirrorRotation = Quaternion.Euler( EditorGUILayout.Vector3Field( "Rotation:",
                            mirrorSettings.mirrorRotation.eulerAngles ) );
                    }

                    using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
                    {
                        EditorGUIUtility.labelWidth = 110;
                        mirrorSettings.invertScale
                            = EditorGUILayout.ToggleLeft( "Invert scale", mirrorSettings.invertScale );
                        mirrorSettings.reflectRotation
                            = EditorGUILayout.ToggleLeft( "Reflect rotation", mirrorSettings.reflectRotation );
                        mirrorSettings.action = (MirrorSettings.MirrorAction)EditorGUILayout.Popup( "Action:",
                            (int)mirrorSettings.action, _mirrorActionNames );
                    }

                    if ( mirrorCheck.changed )
                    {
                        SceneView.RepaintAll();
                    }
                }

                if ( mirrorSettings.action == MirrorSettings.MirrorAction.CREATE )
                {
                    using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
                    {
                        mirrorSettings.sameParentAsSource
                            = EditorGUILayout.ToggleLeft( "Same parent as source", mirrorSettings.sameParentAsSource );
                        if ( !mirrorSettings.sameParentAsSource )
                        {
                            mirrorSettings.autoCreateParent
                                = EditorGUILayout.ToggleLeft( "Create parent", mirrorSettings.autoCreateParent );
                            if ( mirrorSettings.autoCreateParent )
                            {
                                mirrorSettings.createSubparentPerPrefab
                                    = EditorGUILayout.ToggleLeft( "Create sub-parent per prefab",
                                        mirrorSettings.createSubparentPerPrefab );
                            }
                            else
                            {
                                mirrorSettings.parent
                                    = (Transform)EditorGUILayout.ObjectField( "Parent Transform:",
                                        mirrorSettings.parent, typeof(Transform), true );
                            }
                        }
                    }

                    using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
                    {
                        mirrorSettings.overwritePrefabLayer = EditorGUILayout.ToggleLeft( "Overwrite prefab layer",
                            mirrorSettings.overwritePrefabLayer );
                        if ( mirrorSettings.overwritePrefabLayer )
                        {
                            mirrorSettings.layer = EditorGUILayout.LayerField( "Layer:", mirrorSettings.layer );
                        }
                    }
                }

                if ( check.changed )
                {
                    MirrorManager.settings.Copy( mirrorSettings );
                    SceneView.RepaintAll();
                }
            }

            EmbedInSurfaceSettingsGUI( MirrorManager.settings );
        }

        #endregion

        #region REPLACER

        private static BrushPropertiesGroupState _replacerOverwriteGroupState;

        private void ReplacerGroup()
        {
            EditorGUIUtility.labelWidth = 60;
            ReplacerSettings settings = ReplacerManager.settings;
            using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
            {
                RadiusSlider( settings );
            }

            ModifierGroup( settings );
            using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
            {
                bool keepTargetSize = settings.keepTargetSize;
                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    keepTargetSize = EditorGUILayout.ToggleLeft( "Keep target size", settings.keepTargetSize );

                    if ( check.changed )
                    {
                        settings.keepTargetSize = keepTargetSize;
                    }
                }

                if ( keepTargetSize )
                {
                    using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                    {
                        bool maintainProportions = EditorGUILayout.ToggleLeft( "Maintain proportions",
                            settings.maintainProportions );
                        if ( check.changed )
                        {
                            settings.maintainProportions = maintainProportions;
                        }
                    }
                }
            }

            using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
            {
                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    bool outermostFilter = EditorGUILayout.ToggleLeft( "Outermost prefab filter",
                        settings.outermostPrefabFilter );
                    if ( check.changed )
                    {

                        settings.outermostPrefabFilter = outermostFilter;
                    }
                }

                if ( !settings.outermostPrefabFilter )
                {
                    GUILayout.Label( "When you replace a child of a prefab, the prefab will be unpacked.",
                        EditorStyles.helpBox );
                }
            }

            using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
            {
                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    bool overwriteBrushProperties = EditorGUILayout.ToggleLeft( "Overwrite Brush Properties",
                        settings.overwriteBrushProperties );
                    if ( check.changed )
                    {
                        settings.overwriteBrushProperties = overwriteBrushProperties;
                        SceneView.RepaintAll();
                    }
                }

                if ( settings.overwriteBrushProperties )
                {
                    BrushProperties.BrushFields( settings.brushProperties,
                        ref _replacerOverwriteGroupState.brushPosGroupOpen, ref _replacerOverwriteGroupState.brushRotGroupOpen,
                        ref _replacerOverwriteGroupState.brushScaleGroupOpen,
                        ref _replacerOverwriteGroupState.brushFlipGroupOpen, this, UNDO_MSG );
                }
            }

            using ( new GUILayout.HorizontalScope( EditorStyles.helpBox ) )
            {
                GUILayout.FlexibleSpace();
                if ( GUILayout.Button( "Replace all selected" ) )
                {
                    PWBIO.ReplaceAllSelected();
                    SceneView.RepaintAll();
                }

                GUILayout.FlexibleSpace();
            }
        }

        #endregion

    }
}