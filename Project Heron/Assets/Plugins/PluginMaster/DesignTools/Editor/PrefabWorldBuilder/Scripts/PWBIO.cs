using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace PluginMaster
{
    [InitializeOnLoad]
    public static partial class PWBIO
    {

        #region Public Methods

        #region PWB WINDOWS

        public static void CloseAllWindows( bool closeToolbar = true )
        {
            BrushProperties.CloseWindow();
            ToolProperties.CloseWindow();
            PrefabPalette.CloseWindow();
            if ( closeToolbar )
            {
                PWBToolbar.CloseWindow();
            }
        }

        #endregion

        #region TOOLBAR

        public static void ToogleTool( ToolManager.PaintTool tool )
        {
            #if UNITY_2021_2_OR_NEWER
            #else
            if (PWBToolbar.instance == null) PWBToolbar.ShowWindow();
            #endif
            ToolManager.tool = ToolManager.tool == tool ? ToolManager.PaintTool.NONE : tool;
            PWBToolbar.RepaintWindow();
        }

        /*private static void ToolbarInput()
        {
            if(PWBSettings.shortcuts.toolbarPinToggle.combination.Check())
                ToogleTool(ToolManager.PaintTool.PIN);
            else if (PWBSettings.shortcuts.toolbarBrushToggle.combination.Check())
                ToogleTool(ToolManager.PaintTool.BRUSH);
            else if (PWBSettings.shortcuts.toolbarGravityToggle.combination.Check())
                ToogleTool(ToolManager.PaintTool.GRAVITY);
            else if (PWBSettings.shortcuts.toolbarLineToggle.combination.Check())
                ToogleTool(ToolManager.PaintTool.LINE);
            else if (PWBSettings.shortcuts.toolbarShapeToggle.combination.Check())
                ToogleTool(ToolManager.PaintTool.SHAPE);
            else if (PWBSettings.shortcuts.toolbarTilingToggle.combination.Check())
                ToogleTool(ToolManager.PaintTool.TILING);
            else if (PWBSettings.shortcuts.toolbarReplacerToggle.combination.Check())
                ToogleTool(ToolManager.PaintTool.REPLACER);
            else if (PWBSettings.shortcuts.toolbarEraserToggle.combination.Check())
                ToogleTool(ToolManager.PaintTool.ERASER);
            else if (PWBSettings.shortcuts.toolbarSelectionToggle.combination.Check())
                ToogleTool(ToolManager.PaintTool.SELECTION);
            else if (PWBSettings.shortcuts.toolbarExtrudeToggle.combination.Check())
                ToogleTool(ToolManager.PaintTool.EXTRUDE);
            else if (PWBSettings.shortcuts.toolbarMirrorToggle.combination.Check())
                ToogleTool(ToolManager.PaintTool.MIRROR);
        }*/

        #endregion

        #region SELECTION

        public static void UpdateSelection()
        {
            if ( SelectionManager.topLevelSelection.Length == 0 )
            {
                if ( tool == ToolManager.PaintTool.EXTRUDE )
                {
                    _initialExtrudePosition = _extrudeHandlePosition = _selectionSize = Vector3.zero;
                    _extrudeDirection       = Vector3Int.zero;
                }

                return;
            }

            if ( tool == ToolManager.PaintTool.EXTRUDE )
            {
                Bounds selectionBounds = ExtrudeManager.settings.space == Space.World
                    ? BoundsUtils.GetSelectionBounds( SelectionManager.topLevelSelection )
                    : BoundsUtils.GetSelectionBounds( SelectionManager.topLevelSelection,
                        ExtrudeManager.settings.rotationAccordingTo == ExtrudeSettings.RotationAccordingTo.FRIST_SELECTED
                            ? SelectionManager.topLevelSelection.First().transform.rotation
                            : SelectionManager.topLevelSelection.Last().transform.rotation );
                _initialExtrudePosition = _extrudeHandlePosition = selectionBounds.center;
                _selectionSize          = selectionBounds.size;
                _extrudeDirection       = Vector3Int.zero;
            }
            else if ( tool == ToolManager.PaintTool.SELECTION )
            {
                _selectedBoxPointIdx            = 10;
                _selectionRotation              = Quaternion.identity;
                _selectionChanged               = true;
                _editingSelectionHandlePosition = false;
                Quaternion rotation = GetSelectionRotation();
                _selectionBounds   = BoundsUtils.GetSelectionBounds( SelectionManager.topLevelSelection, rotation );
                _selectionRotation = rotation;
            }
        }

        #endregion

        #endregion

        #region Private Methods

        #region HANDLES

        private static void DrawDotHandleCap( Vector3 point,      float alpha    = 1f,
                                              float   scale = 1f, bool  selected = false )
        {
            Handles.color = new Color( 0f, 0f, 0f, 0.7f * alpha );
            float handleSize = HandleUtility.GetHandleSize( point );
            float sizeDelta  = handleSize * 0.0125f;
            Handles.DotHandleCap( 0, point, Quaternion.identity,
                handleSize * 0.0325f * scale * PWBCore.staticData.controPointSize, EventType.Repaint );
            Color fillColor = selected ? Color.cyan : Handles.preselectionColor;
            fillColor.a   *= alpha;
            Handles.color =  fillColor;
            Handles.DotHandleCap( 0, point, Quaternion.identity,
                ( handleSize * 0.0325f * scale - sizeDelta ) * PWBCore.staticData.controPointSize, EventType.Repaint );
        }

        #endregion

        #endregion

        #region UNSAVED CHANGES

        private const string UNSAVED_CHANGES_TITLE   = "Unsaved Changes";
        private const string UNSAVED_CHANGES_MESSAGE = "There are unsaved changes.\nWhat would you like to do?";
        private const string UNSAVED_CHANGES_OK      = "Save";
        private const string UNSAVED_CHANGES_CANCEL  = "Don't Save";

        private static void DisplaySaveDialog( Action Save )
        {
            if ( EditorUtility.DisplayDialog( UNSAVED_CHANGES_TITLE,
                    UNSAVED_CHANGES_MESSAGE, UNSAVED_CHANGES_OK, UNSAVED_CHANGES_CANCEL ) )
            {
                Save();
            }
            else
            {
                repaint = true;
            }
        }

        private static void AskIfWantToSave( ToolManager.ToolState state, Action Save )
        {
            switch ( PWBCore.staticData.unsavedChangesAction )
            {
                case PWBData.UnsavedChangesAction.ASK:
                    if ( state == ToolManager.ToolState.EDIT )
                    {
                        DisplaySaveDialog( Save );
                    }

                    break;
                case PWBData.UnsavedChangesAction.SAVE:
                    if ( state == ToolManager.ToolState.EDIT )
                    {
                        Save();
                    }

                    BrushstrokeManager.ClearBrushstroke();
                    break;
                case PWBData.UnsavedChangesAction.DISCARD:
                    repaint = true;
                    return;
            }
        }

        #endregion

        #region COMMON

        private const  float TAU = Mathf.PI * 2;
        private static int   _controlId;

        public static int controlId
        {
            set => _controlId = value;
        }

        private static ToolManager.PaintTool tool => ToolManager.tool;

        private static Tool _unityCurrentTool = Tool.None;

        private static Camera _sceneViewCamera;

        public static bool repaint { get; set; }

        static PWBIO()
        {
            LineData.SetNextId();
            SelectionManager.selectionChanged += UpdateSelection;
            Undo.undoRedoPerformed            += OnUndoPerformed;
            SceneView.duringSceneGui          += DuringSceneGUI;
            PaletteManager.OnPaletteChanged   += OnPaletteChanged;
            PaletteManager.OnBrushChanged     += OnBrushChanged;
            ToolManager.OnToolModeChanged     += OnEditModeChanged;
            LineInitializeOnLoad();
            ShapeInitializeOnLoad();
            TilingInitializeOnLoad();
        }

        private static void OnPaletteChanged() => ApplySelectionFilters();

        private static void OnBrushChanged()
        {
            switch ( ToolManager.tool )
            {
                case ToolManager.PaintTool.LINE:

                    ClearLineStroke();
                    break;
                case ToolManager.PaintTool.SHAPE:
                    ClearShapeStroke();
                    break;
                case ToolManager.PaintTool.TILING:
                    ClearTilingStroke();
                    break;
                case ToolManager.PaintTool.SELECTION:
                    ApplySelectionFilters();
                    break;
            }
        }

        public static void SaveUnityCurrentTool() => _unityCurrentTool = Tools.current;
        public static bool _wasPickingBrushes;

        public static void DuringSceneGUI( SceneView sceneView )
        {
            if ( sceneView.in2DMode )
            {
                SnapManager.settings.gridOnZ = true;
                PWBToolbar.RepaintWindow();
            }

            if ( repaint )
            {
                if ( tool == ToolManager.PaintTool.SHAPE )
                {
                    BrushstrokeManager.UpdateShapeBrushstroke();
                }

                sceneView.Repaint();
                repaint = false;
            }

            PaletteInput( sceneView );
            _sceneViewCamera = sceneView.camera;

            if ( Event.current.type       == EventType.KeyDown
                 && Event.current.keyCode == KeyCode.Escape
                 && ( tool    == ToolManager.PaintTool.PIN
                      || tool == ToolManager.PaintTool.BRUSH
                      || tool == ToolManager.PaintTool.GRAVITY
                      || tool == ToolManager.PaintTool.ERASER
                      || tool == ToolManager.PaintTool.REPLACER ) )
            {
                ToolManager.DeselectTool();
            }

            bool repaintScene = _wasPickingBrushes == PaletteManager.pickingBrushes;
            _wasPickingBrushes = PaletteManager.pickingBrushes;
            if ( PaletteManager.pickingBrushes )
            {
                HandleUtility.AddDefaultControl( _controlId );
                if ( repaintScene )
                {
                    SceneView.RepaintAll();
                }

                if ( Event.current.button  == 0
                     && Event.current.type == EventType.MouseDown )
                {
                    Event.current.Use();
                }

                return;
            }

            if ( ToolManager.tool != ToolManager.PaintTool.NONE )
            {
                if ( PWBSettings.shortcuts.editModeToggle.Check() )
                {
                    switch ( tool )
                    {
                        case ToolManager.PaintTool.LINE:
                        case ToolManager.PaintTool.SHAPE:
                        case ToolManager.PaintTool.TILING:
                            ToolManager.editMode = !ToolManager.editMode;
                            ToolProperties.RepainWindow();
                            break;
                    }
                }

                if ( PaletteManager.selectedBrushIdx == -1
                     && ( tool    == ToolManager.PaintTool.PIN
                          || tool == ToolManager.PaintTool.BRUSH
                          || tool == ToolManager.PaintTool.GRAVITY
                          || ( ( tool    == ToolManager.PaintTool.LINE
                                 || tool == ToolManager.PaintTool.SHAPE
                                 || tool == ToolManager.PaintTool.TILING )
                               && !ToolManager.editMode ) ) )
                {
                    if ( tool               == ToolManager.PaintTool.LINE
                         && _lineData       != null
                         && _lineData.state != ToolManager.ToolState.NONE )
                    {
                        ResetLineState();
                    }
                    else if ( tool                == ToolManager.PaintTool.SHAPE
                              && _shapeData       != null
                              && _shapeData.state != ToolManager.ToolState.NONE )
                    {
                        ResetShapeState();
                    }
                    else if ( tool                 == ToolManager.PaintTool.TILING
                              && _tilingData       != null
                              && _tilingData.state != ToolManager.ToolState.NONE )
                    {
                        ResetTilingState();
                    }

                    return;
                }

                if ( Event.current.type == EventType.MouseEnterWindow )
                {
                    _pinned = false;
                }

                if ( Event.current.type    == EventType.MouseMove
                     || Event.current.type == EventType.MouseDrag )
                {
                    sceneView.Focus();
                }
                else if ( Event.current.type       == EventType.KeyDown
                          && Event.current.keyCode == KeyCode.V )
                {
                    _snapToVertex = true;
                }
                else if ( Event.current.type       == EventType.KeyUp
                          && Event.current.keyCode == KeyCode.V )
                {
                    _snapToVertex = false;
                }

                if ( tool    == ToolManager.PaintTool.BRUSH
                     || tool == ToolManager.PaintTool.GRAVITY
                     || tool == ToolManager.PaintTool.ERASER
                     || tool == ToolManager.PaintTool.REPLACER )
                {
                    IToolSettings settings = ToolManager.GetSettingsFromTool( tool );
                    BrushRadiusShortcuts( settings as CircleToolBase );
                }

                if ( PWBCore.staticData.tempCollidersAction == PWBData.TempCollidersAction.CREATE_WITHIN_FRUSTRUM )
                {
                    PWBCore.CreateTempCollidersWithinFrustum( sceneView.camera );
                }

                switch ( tool )
                {
                    case ToolManager.PaintTool.PIN:
                        PinDuringSceneGUI( sceneView );
                        break;
                    case ToolManager.PaintTool.BRUSH:
                        BrushDuringSceneGUI( sceneView );
                        break;
                    case ToolManager.PaintTool.GRAVITY:
                        GravityToolDuringSceneGUI( sceneView );
                        break;
                    case ToolManager.PaintTool.LINE:
                        LineDuringSceneGUI( sceneView );
                        break;
                    case ToolManager.PaintTool.SHAPE:
                        ShapeDuringSceneGUI( sceneView );
                        break;
                    case ToolManager.PaintTool.TILING:
                        TilingDuringSceneGUI( sceneView );
                        break;
                    case ToolManager.PaintTool.ERASER:
                        EraserDuringSceneGUI( sceneView );
                        break;
                    case ToolManager.PaintTool.REPLACER:
                        ReplacerDuringSceneGUI( sceneView );
                        break;
                    case ToolManager.PaintTool.SELECTION:
                        SelectionDuringSceneGUI( sceneView );
                        break;
                    case ToolManager.PaintTool.EXTRUDE:
                        ExtrudeDuringSceneGUI( sceneView );
                        break;
                    case ToolManager.PaintTool.MIRROR:
                        MirrorDuringSceneGUI( sceneView );
                        break;
                }

                if ( tool                  != ToolManager.PaintTool.EXTRUDE
                     && tool               != ToolManager.PaintTool.SELECTION
                     && tool               != ToolManager.PaintTool.MIRROR
                     && Event.current.type == EventType.Layout
                     && !ToolManager.editMode )
                {
                    Tools.current = Tool.None;
                    HandleUtility.AddDefaultControl( _controlId );
                }
            }

            GridDuringSceneGui( sceneView );
        }

        private static float UpdateRadius( float radius )
            => Mathf.Max( radius * ( 1f + Mathf.Sign( Event.current.delta.y ) * 0.05f ), 0.05f );

        private static Vector3 TangentSpaceToWorld( Vector3 tangent, Vector3 bitangent, Vector2 tangentSpacePos )
            => tangent * tangentSpacePos.x + bitangent * tangentSpacePos.y;

        private static void UpdateStrokeDirection( Vector3 hitPoint )
        {
            Vector3 dir = hitPoint - _prevMousePos;
            if ( dir.sqrMagnitude > 0.3f )
            {
                _strokeDirection = hitPoint - _prevMousePos;
                _prevMousePos    = hitPoint;
            }
        }

        public static void ResetUnityCurrentTool() => Tools.current = _unityCurrentTool;

        private static bool MouseDot( out Vector3                              point,                 out Vector3 normal,
                                      PaintOnSurfaceToolSettingsBase.PaintMode mode,                  bool        in2DMode,
                                      bool                                     paintOnPalettePrefabs, bool        castOnMeshesWithoutCollider, bool snapOnGrid )
        {
            point  = Vector3.zero;
            normal = Vector3.up;
            Vector2 mousePos = Event.current.mousePosition;
            if ( mousePos.x    < 0
                 || mousePos.x >= Screen.width
                 || mousePos.y < 0
                 || mousePos.y >= Screen.height )
            {
                return false;
            }

            Ray mouseRay = HandleUtility.GUIPointToWorldRay( mousePos );

            Vector3 SnapPoint( Vector3 hitPoint, ref Vector3 snapNormal )
            {
                if ( _snapToVertex )
                {
                    if ( SnapToVertex( mouseRay, out RaycastHit snappedHit, in2DMode ) )
                    {
                        _snappedToVertex = true;
                        hitPoint         = snappedHit.point;
                        snapNormal       = snappedHit.normal;
                    }
                }
                else if ( SnapManager.settings.snappingEnabled )
                {
                    hitPoint        = SnapPosition( hitPoint, snapOnGrid, true );
                    mouseRay.origin = hitPoint - mouseRay.direction;
                    if ( Physics.Raycast( mouseRay, out RaycastHit hitInfo ) )
                    {
                        snapNormal = hitInfo.normal;
                    }
                    else if ( MeshUtils.Raycast( mouseRay, out RaycastHit meshHitInfo, out GameObject c,
                                 octree.GetNearby( mouseRay, 1 ).Where( o => o != null ).ToArray(),
                                 float.MaxValue ) )
                    {
                        snapNormal = meshHitInfo.normal;
                    }
                }

                return hitPoint;
            }

            RaycastHit surfaceHit;
            bool surfaceFound = MouseRaycast( mouseRay, out surfaceHit, out GameObject collider,
                float.MaxValue,                         -1,             paintOnPalettePrefabs, castOnMeshesWithoutCollider );
            if ( mode != PaintOnSurfaceToolSettingsBase.PaintMode.ON_SHAPE && surfaceFound )
            {
                normal = surfaceHit.normal;
                point  = SnapPoint( surfaceHit.point, ref normal );
                return true;
            }

            if ( mode != PaintOnSurfaceToolSettingsBase.PaintMode.ON_SURFACE )
            {
                if ( surfaceFound )
                {
                    point = SnapPoint( surfaceHit.point, ref normal );
                    Vector3 direction = SnapManager.settings.rotation * Vector3.down;
                    Ray     ray       = new Ray( point - direction, direction );
                    if ( MouseRaycast( ray,        out RaycastHit hitInfo, out collider, float.MaxValue, -1,
                            paintOnPalettePrefabs, castOnMeshesWithoutCollider ) )
                    {
                        point = hitInfo.point;
                    }

                    UpdateGridOrigin( point );
                    return true;
                }

                if ( GridRaycast( mouseRay, out RaycastHit gridHit ) )
                {
                    point = SnapPoint( gridHit.point, ref normal );
                    return true;
                }
            }

            return false;
        }

        public static bool updateStroke { get; set; }

        public static void UpdateStroke()
        {
            updateStroke = true;
            SceneView.RepaintAll();
        }

        public static void UpdateSelectedPersistentObject()
        {
            BrushstrokeManager.UpdateBrushstroke();
            switch ( tool )
            {
                case ToolManager.PaintTool.LINE:
                    if ( _selectedPersistentLineData != null )
                    {
                        _editingPersistentLine = true;
                    }

                    break;
                case ToolManager.PaintTool.SHAPE:
                    if ( _selectedPersistentShapeData != null )
                    {
                        _editingPersistentShape = true;
                    }

                    break;
                case ToolManager.PaintTool.TILING:
                    if ( _selectedPersistentTilingData != null )
                    {
                        _editingPersistentTiling = true;
                    }

                    break;
            }

            repaint = true;
        }

        public static int selectedPointIdx
        {
            get
            {
                switch ( ToolManager.tool )
                {
                    case ToolManager.PaintTool.TILING:
                        if ( ToolManager.editMode )
                        {
                            if ( _selectedPersistentTilingData == null )
                            {
                                return -1;
                            }

                            return _selectedPersistentTilingData.selectedPointIdx;
                        }

                        if ( _tilingData.state == ToolManager.ToolState.EDIT )
                        {
                            return _tilingData.selectedPointIdx;
                        }

                        break;
                    case ToolManager.PaintTool.LINE:
                        if ( ToolManager.editMode )
                        {
                            if ( _selectedPersistentLineData == null )
                            {
                                return -1;
                            }

                            return _selectedPersistentLineData.selectedPointIdx;
                        }

                        if ( _lineData.state == ToolManager.ToolState.EDIT )
                        {
                            return _lineData.selectedPointIdx;
                        }

                        break;
                    case ToolManager.PaintTool.SHAPE:
                        if ( ToolManager.editMode )
                        {
                            if ( _selectedPersistentShapeData == null )
                            {
                                return -1;
                            }

                            return _selectedPersistentShapeData.selectedPointIdx;
                        }

                        if ( _shapeData.state == ToolManager.ToolState.EDIT )
                        {
                            return _shapeData.selectedPointIdx;
                        }

                        break;
                }

                return -1;
            }
        }

        private static bool _updateHandlePosition;

        public static void UpdateHandlePosition()
        {
            _updateHandlePosition = true;
            if ( tool          == ToolManager.PaintTool.TILING
                 && tilingData != null )
            {
                ApplyTilingHandlePosition( tilingData );
            }

            BrushstrokeManager.UpdateBrushstroke();

        }

        public static Vector3 handlePosition { get; set; }

        private static bool _updateHandleRotation;

        public static void UpdateHandleRotation()
        {
            _updateHandleRotation = true;
            BrushstrokeManager.UpdateBrushstroke();
        }

        public static Quaternion handleRotation { get; set; }

        #endregion

        #region PERSISTENT OBJECTS

        public static void OnUndoPerformed()
        {
            _octree = null;
            if ( tool                          == ToolManager.PaintTool.LINE
                 && Undo.GetCurrentGroupName() == LineData.COMMAND_NAME )
            {
                OnUndoLine();
                UpdateStroke();
            }
            else if ( tool                          == ToolManager.PaintTool.SHAPE
                      && Undo.GetCurrentGroupName() == ShapeData.COMMAND_NAME )
            {
                OnUndoShape();
                UpdateStroke();
            }
            else if ( tool                          == ToolManager.PaintTool.TILING
                      && Undo.GetCurrentGroupName() == TilingData.COMMAND_NAME )
            {
                OnUndoTiling();
                UpdateStroke();
            }

            if ( ToolManager.tool    != ToolManager.PaintTool.LINE
                 && ToolManager.tool != ToolManager.PaintTool.SHAPE
                 && ToolManager.tool != ToolManager.PaintTool.TILING )
            {
                BrushstrokeManager.UpdateBrushstroke();
            }

            SceneView.RepaintAll();
        }

        public static void OnToolChange( ToolManager.PaintTool prevTool )
        {
            switch ( prevTool )
            {
                case ToolManager.PaintTool.LINE:
                    ResetLineState();
                    break;
                case ToolManager.PaintTool.SHAPE:
                    ResetShapeState();
                    break;
                case ToolManager.PaintTool.TILING:
                    ResetTilingState();
                    break;
                case ToolManager.PaintTool.EXTRUDE:
                    ResetExtrudeState();
                    break;
                case ToolManager.PaintTool.MIRROR:
                    ResetMirrorState();
                    break;
            }

            _meshesAndRenderers.Clear();
            SceneView.RepaintAll();
        }

        private static void OnEditModeChanged()
        {
            switch ( tool )
            {
                case ToolManager.PaintTool.LINE:
                    OnLineToolModeChanged();
                    break;
                case ToolManager.PaintTool.SHAPE:
                    OnShapeToolModeChanged();
                    break;
                case ToolManager.PaintTool.TILING:
                    OnTilingToolModeChanged();
                    break;
            }
        }

        private static void DeleteDisabledObjects()
        {
            if ( _disabledObjects == null )
            {
                return;
            }

            foreach ( GameObject obj in _disabledObjects )
            {
                if ( obj == null )
                {
                    continue;
                }

                obj.SetActive( true );
                Undo.DestroyObjectImmediate( obj );
            }
        }

        private static void ResetSelectedPersistentObject<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT, TOOL_DATA, SCENE_DATA>
        ( PersistentToolManagerBase<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT, TOOL_DATA, SCENE_DATA> manager,
          ref bool                                                                                  editingPersistentObject, TOOL_DATA initialPersistentData )
            where TOOL_NAME : IToolName, new()
            where TOOL_SETTINGS : ICloneableToolSettings, new()
            where CONTROL_POINT : ControlPoint, new()
            where TOOL_DATA : PersistentData<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT>, new()
            where SCENE_DATA : SceneData<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT, TOOL_DATA>, new()
        {
            editingPersistentObject = false;
            if ( initialPersistentData == null )
            {
                return;
            }

            TOOL_DATA selectedItem = manager.GetItem( initialPersistentData.id );
            if ( selectedItem == null )
            {
                return;
            }

            selectedItem.ResetPoses( initialPersistentData );
            selectedItem.selectedPointIdx = -1;
            selectedItem.ClearSelection();
        }

        private static void DeselectPersistentItems<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT, TOOL_DATA, SCENE_DATA>
            ( PersistentToolManagerBase<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT, TOOL_DATA, SCENE_DATA> manager )
            where TOOL_NAME : IToolName, new()
            where TOOL_SETTINGS : ICloneableToolSettings, new()
            where CONTROL_POINT : ControlPoint, new()
            where TOOL_DATA : PersistentData<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT>, new()
            where SCENE_DATA : SceneData<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT, TOOL_DATA>, new()
        {
            TOOL_DATA[] persitentTilings = manager.GetPersistentItems();
            foreach ( TOOL_DATA i in persitentTilings )
            {
                i.selectedPointIdx = -1;
                i.ClearSelection();
            }
        }

        private static bool ApplySelectedPersistentObject<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT, TOOL_DATA, SCENE_DATA>
        ( bool                                                                                      deselectPoint, ref bool editingPersistentObject, ref TOOL_DATA initialPersistentData,
          ref TOOL_DATA                                                                             selectedPersistentData,
          PersistentToolManagerBase<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT, TOOL_DATA, SCENE_DATA> manager )
            where TOOL_NAME : IToolName, new()
            where TOOL_SETTINGS : ICloneableToolSettings, new()
            where CONTROL_POINT : ControlPoint, new()
            where TOOL_DATA : PersistentData<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT>, new()
            where SCENE_DATA : SceneData<TOOL_NAME, TOOL_SETTINGS, CONTROL_POINT, TOOL_DATA>, new()
        {
            editingPersistentObject = false;
            if ( initialPersistentData == null )
            {
                return false;
            }

            TOOL_DATA selected = manager.GetItem( initialPersistentData.id );
            if ( selected == null )
            {
                initialPersistentData  = null;
                selectedPersistentData = null;
                return false;
            }

            selected.UpdatePoses();
            if ( _paintStroke.Count > 0 )
            {
                Dictionary<string, List<GameObject>> objDic = Paint( selected.settings as IPaintToolSettings, PAINT_CMD, true, true );
                foreach ( KeyValuePair<string, List<GameObject>> paintedItem in objDic )
                {
                    TOOL_DATA persistentItem = manager.GetItem( paintedItem.Key );
                    if ( persistentItem == null )
                    {
                        continue;
                    }

                    persistentItem.AddObjects( paintedItem.Value.ToArray() );
                }
            }

            if ( deselectPoint )
            {
                DeselectPersistentItems( manager );
            }

            DeleteDisabledObjects();

            _persistentPreviewData.Clear();
            if ( !deselectPoint )
            {
                return true;
            }

            TOOL_DATA[] persistentObjects = manager.GetPersistentItems();
            foreach ( TOOL_DATA item in persistentObjects )
            {
                item.selectedPointIdx = -1;
                item.ClearSelection();
            }

            return true;
        }

        #endregion

        #region OCTREE

        private const  float                   MIN_OCTREE_NODE_SIZE = 0.5f;
        private static PointOctree<GameObject> _octree              = new PointOctree<GameObject>( 10, Vector3.zero, MIN_OCTREE_NODE_SIZE );

        private static List<GameObject> _paintedObjects
            = new List<GameObject>();

        public static PointOctree<GameObject> octree
        {
            get
            {
                if ( _octree == null )
                {
                    UpdateOctree();
                }

                return _octree;
            }
            set => _octree = value;
        }

        public static void UpdateOctree()
        {
            if ( PaletteManager.paletteCount == 0 )
            {
                return;
            }

            if ( ( tool    == ToolManager.PaintTool.PIN
                   || tool == ToolManager.PaintTool.BRUSH
                   || tool == ToolManager.PaintTool.GRAVITY
                   || tool == ToolManager.PaintTool.LINE
                   || tool == ToolManager.PaintTool.SHAPE
                   || tool == ToolManager.PaintTool.TILING )
                 && PaletteManager.selectedBrushIdx < 0 )
            {
                return;
            }
            #if UNITY_2022_2_OR_NEWER
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>( FindObjectsSortMode.None );
            #else
            var allObjects = GameObject.FindObjectsOfType<GameObject>();
            #endif
            _octree = null;
            _paintedObjects.Clear();
            List<string> allPrefabsPaths = new List<string>();

            bool AddPrefabPath( MultibrushItemSettings item )
            {
                if ( item.prefab == null )
                {
                    return false;
                }

                string path = AssetDatabase.GetAssetPath( item.prefab );
                if ( allPrefabsPaths.Contains( path ) )
                {
                    return false;
                }

                allPrefabsPaths.Add( path );
                return true;
            }

            if ( tool    == ToolManager.PaintTool.ERASER
                 || tool == ToolManager.PaintTool.REPLACER )
            {
                IModifierTool settings = EraserManager.settings;
                if ( tool == ToolManager.PaintTool.REPLACER )
                {
                    settings = ReplacerManager.settings;
                }

                if ( settings.command == ModifierToolSettings.Command.MODIFY_PALETTE_PREFABS )
                {
                    foreach ( MultibrushSettings brush in PaletteManager.selectedPalette.brushes )
                    foreach ( MultibrushItemSettings item in brush.items )
                    {
                        AddPrefabPath( item );
                    }
                }
                else if ( PaletteManager.selectedBrush != null
                          && settings.command          == ModifierToolSettings.Command.MODIFY_BRUSH_PREFABS )
                {
                    foreach ( MultibrushItemSettings item in PaletteManager.selectedBrush.items )
                    {
                        AddPrefabPath( item );
                    }
                }

                SelectionManager.UpdateSelection();
                bool modifyAll            = settings.command == ModifierToolSettings.Command.MODIFY_ALL;
                bool modifyAllButSelected = settings.modifyAllButSelected;
                foreach ( GameObject obj in allObjects )
                {
                    if ( !obj.activeInHierarchy )
                    {
                        continue;
                    }

                    if ( !modifyAll
                         && !PrefabUtility.IsAnyPrefabInstanceRoot( obj ) )
                    {
                        continue;
                    }

                    string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot( obj );
                    bool   isBrush    = allPrefabsPaths.Contains( prefabPath );
                    if ( !isBrush
                         && !modifyAll )
                    {
                        continue;
                    }

                    if ( modifyAllButSelected && SelectionManager.selection.Contains( obj ) )
                    {
                        continue;
                    }

                    AddPaintedObject( obj );
                }
            }
            else
            {
                foreach ( MultibrushSettings brush in PaletteManager.selectedPalette.brushes )
                foreach ( MultibrushItemSettings item in brush.items )
                {
                    AddPrefabPath( item );
                }

                foreach ( GameObject obj in allObjects )
                {
                    if ( !obj.activeInHierarchy )
                    {
                        continue;
                    }

                    if ( !PrefabUtility.IsAnyPrefabInstanceRoot( obj ) )
                    {
                        continue;
                    }

                    string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot( obj );
                    bool   isBrush    = allPrefabsPaths.Contains( prefabPath );
                    if ( isBrush )
                    {
                        AddPaintedObject( obj );
                    }
                }
            }

            if ( _octree == null )
            {
                _octree = new PointOctree<GameObject>( 10, Vector3.zero, MIN_OCTREE_NODE_SIZE );
            }
        }

        private static void AddPaintedObject( GameObject obj )
        {
            if ( _octree == null )
            {
                _octree = new PointOctree<GameObject>( 10, obj.transform.position, MIN_OCTREE_NODE_SIZE );
            }

            _octree.Add( obj, obj.transform.position );
            _paintedObjects.Add( obj );
        }

        public static bool OctreeContains( int objId ) => octree.Contains( objId );

        #endregion

        #region STROKE & PAINT

        private const  string    PWB_OBJ_NAME     = "Prefab World Builder";
        private static Vector3   _prevMousePos    = Vector3.zero;
        private static Vector3   _strokeDirection = Vector3.forward;
        private static Transform _autoParent;

        private static Dictionary<string, Transform> _subParents
            = new Dictionary<string, Transform>();

        private static Mesh quadMesh;

        private class PaintStrokeItem
        {

            #region Public Fields

            public readonly bool       flipX;
            public readonly bool       flipY;
            public readonly int        layer;
            public readonly Vector3    position = Vector3.zero;
            public readonly GameObject prefab;
            public readonly Quaternion rotation = Quaternion.identity;
            public readonly Vector3    scale    = Vector3.one;

            #endregion

            #region Public Properties

            public Transform parent             { get; set; }
            public string    persistentParentId { get; set; } = string.Empty;
            public Transform surface            { get; }

            #endregion

            #region Public Constructors

            public PaintStrokeItem( GameObject prefab, Vector3 position, Quaternion rotation,
                                    Vector3    scale,  int     layer,    Transform  parent, Transform surface, bool flipX, bool flipY )
            {
                this.prefab   = prefab;
                this.position = position;
                this.rotation = rotation;
                this.scale    = scale;
                this.layer    = layer;
                this.flipX    = flipX;
                this.flipY    = flipY;
                this.parent   = parent;
                this.surface  = surface;
            }

            #endregion

            #region Private Fields

            #endregion

        }

        private static List<PaintStrokeItem> _paintStroke
            = new List<PaintStrokeItem>();

        private static void BrushRadiusShortcuts( CircleToolBase settings )
        {
            if ( PWBSettings.shortcuts.brushRadius.Check() )
            {
                PWBMouseCombination combi = PWBSettings.shortcuts.brushRadius.combination;
                float               delta = Mathf.Sign( combi.delta );
                settings.radius = Mathf.Max( settings.radius * ( 1f + delta * 0.03f ), 0.05f );
                if ( settings is BrushToolSettings )
                {
                    if ( BrushManager.settings.heightType == BrushToolSettings.HeightType.RADIUS )
                    {
                        BrushManager.settings.maxHeightFromCenter = BrushManager.settings.radius;
                    }
                }

                ToolProperties.RepainWindow();
            }
        }

        private static void BrushstrokeMouseEvents( BrushToolBase settings )
        {
            if ( PaletteManager.selectedBrush == null )
            {
                return;
            }

            if ( Event.current.button == 0
                 && !Event.current.alt
                 && Event.current.type                          == EventType.MouseUp
                 && PaletteManager.selectedBrush.patternMachine != null
                 && PaletteManager.selectedBrush.restartPatternForEachStroke )
            {
                PaletteManager.selectedBrush.patternMachine.Reset();
                BrushstrokeManager.UpdateBrushstroke();
            }
            else if ( PWBSettings.shortcuts.brushUpdatebrushstroke.Check() )
            {
                BrushstrokeManager.UpdateBrushstroke();
                repaint = true;
            }
            else if ( PWBSettings.shortcuts.brushResetRotation.Check() )
            {
                _brushAngle = 0;
            }
            else if ( PWBSettings.shortcuts.brushDensity.Check()
                      && settings.brushShape != BrushToolBase.BrushShape.POINT )
            {
                settings.density += (int)Mathf.Sign( PWBSettings.shortcuts.brushDensity.combination.delta );
                ToolProperties.RepainWindow();
            }
            else if ( PWBSettings.shortcuts.brushRotate.Check() )
            {
                _brushAngle -= PWBSettings.shortcuts.brushRotate.combination.delta * 1.8f; //180deg/100px
            }

            if ( Event.current.button == 1 )
            {
                if ( Event.current.type == EventType.MouseDown
                     && ( Event.current.control || Event.current.shift ) )
                {
                    _pinned   = true;
                    _pinMouse = Event.current.mousePosition;
                    Event.current.Use();
                }
                else if ( Event.current.type == EventType.MouseUp
                          && !Event.current.control
                          && !Event.current.shift )
                {
                    _pinned = false;
                }
            }

            if ( ( Event.current.keyCode    == KeyCode.LeftControl
                   || Event.current.keyCode == KeyCode.RightControl
                   || Event.current.keyCode == KeyCode.RightShift
                   || Event.current.keyCode == KeyCode.LeftShift )
                 && Event.current.type == EventType.KeyUp )
            {
                _pinned = false;
            }
        }

        private struct MeshAndRenderer
        {

            #region Public Fields

            public Mesh     mesh;
            public Renderer renderer;

            #endregion

            #region Public Constructors

            public MeshAndRenderer( Mesh mesh, Renderer renderer )
            {
                this.mesh     = mesh;
                this.renderer = renderer;
            }

            #endregion

        }

        private static Dictionary<int, MeshAndRenderer[]> _meshesAndRenderers
            = new Dictionary<int, MeshAndRenderer[]>();

        private static void PreviewBrushItem( GameObject prefab, Matrix4x4 rootToWorld, int  layer,
                                              Camera     camera, bool      redMaterial, bool reverseTriangles, bool flipX, bool flipY )
        {
            int id = prefab.GetInstanceID();
            if ( !_meshesAndRenderers.ContainsKey( id ) )
            {
                List<MeshAndRenderer> meshesAndRenderers = new List<MeshAndRenderer>();
                MeshFilter[]          filters            = prefab.GetComponentsInChildren<MeshFilter>();
                foreach ( MeshFilter filter in filters )
                {
                    Mesh mesh = filter.sharedMesh;
                    if ( mesh == null )
                    {
                        continue;
                    }

                    MeshRenderer renderer = filter.GetComponent<MeshRenderer>();
                    if ( renderer == null )
                    {
                        continue;
                    }

                    meshesAndRenderers.Add( new MeshAndRenderer( mesh, renderer ) );
                }

                SkinnedMeshRenderer[] skinedMeshRenderers = prefab.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach ( SkinnedMeshRenderer renderer in skinedMeshRenderers )
                {
                    Mesh mesh = renderer.sharedMesh;
                    if ( mesh == null )
                    {
                        continue;
                    }

                    meshesAndRenderers.Add( new MeshAndRenderer( mesh, renderer ) );
                }

                _meshesAndRenderers.Add( id, meshesAndRenderers.ToArray() );
            }

            foreach ( MeshAndRenderer item in _meshesAndRenderers[ id ] )
            {

                Mesh        mesh         = item.mesh;
                Matrix4x4   childToWorld = rootToWorld * item.renderer.transform.localToWorldMatrix;
                Matrix4x4[] matrices     = { childToWorld };

                if ( !redMaterial )
                {
                    if ( item.renderer is SkinnedMeshRenderer )
                    {
                        SkinnedMeshRenderer smr      = (SkinnedMeshRenderer)item.renderer;
                        Transform           rootBone = smr.rootBone;
                        if ( rootBone != null )
                        {
                            while ( rootBone.parent    != null
                                    && rootBone.parent != prefab.transform )
                            {
                                rootBone = rootBone.parent;
                            }

                            Quaternion rotation = rootBone.rotation;
                            Vector3    position = rootBone.position;
                            position.y = 0f;
                            Vector3 scale = rootBone.localScale;
                            childToWorld = rootToWorld * Matrix4x4.TRS( position, rotation, scale );
                        }
                    }

                    Material[] materials = item.renderer.sharedMaterials;

                    if ( materials           == null
                         && materials.Length > 0
                         && materials.Length >= mesh.subMeshCount )
                    {
                        continue;
                    }

                    for ( int subMeshIdx = 0; subMeshIdx < Mathf.Min( mesh.subMeshCount, materials.Length ); ++subMeshIdx )
                    {
                        Material material = materials[ subMeshIdx ];
                        if ( reverseTriangles )
                        {
                            Mesh tempMesh = Object.Instantiate( mesh );
                            tempMesh.SetTriangles( mesh.triangles.Reverse().ToArray(), subMeshIdx );
                            tempMesh.subMeshCount = mesh.subMeshCount;
                            int vCount = 0;
                            for ( int i = 0; i < mesh.subMeshCount; ++i )
                            {
                                SubMeshDescriptor desc = mesh.GetSubMesh( mesh.subMeshCount - i - 1 );
                                desc.indexStart = vCount;
                                tempMesh.SetSubMesh( i, desc );
                                vCount += desc.indexCount;
                            }

                            material = materials[ mesh.subMeshCount - subMeshIdx - 1 ];
                            Graphics.DrawMesh( tempMesh, childToWorld, material, layer, camera, subMeshIdx );

                            tempMesh = null;
                        }
                        else
                        {
                            Graphics.DrawMesh( mesh, childToWorld, material, layer, camera, subMeshIdx );
                        }
                    }
                }
                else
                {
                    for ( int subMeshIdx = 0; subMeshIdx < mesh.subMeshCount; ++subMeshIdx )
                    {
                        Graphics.DrawMesh( mesh, childToWorld, transparentRedMaterial, layer, camera, subMeshIdx );
                    }
                }
            }

            SpriteRenderer[] SpriteRenderers = prefab.GetComponentsInChildren<SpriteRenderer>()
                                                     .Where( r => r.enabled && r.sprite != null && r.gameObject.activeSelf ).ToArray();
            if ( SpriteRenderers.Length > 0 )
            {
                Bounds bounds = BoundsUtils.GetBoundsRecursive( prefab.transform );

                foreach ( SpriteRenderer spriteRenderer in SpriteRenderers )
                {
                    DrawSprite( spriteRenderer, rootToWorld, camera, bounds, flipX, flipY );
                }
            }
        }

        private static void DrawSprite( SpriteRenderer renderer, Matrix4x4 matrix,
                                        Camera         camera,   Bounds    objectBounds, bool flipX, bool flipY )
        {
            if ( quadMesh == null )
            {
                quadMesh = new Mesh
                {
                    vertices = new[]
                    {
                        new Vector3( -.5f, .5f,  0 ), new Vector3( .5f, .5f,  0 ),
                        new Vector3( -.5f, -.5f, 0 ), new Vector3( .5f, -.5f, 0 ),
                    },
                    normals   = new[] { Vector3.forward, Vector3.forward, Vector3.forward, Vector3.forward },
                    triangles = new[] { 0, 2, 3, 3, 1, 0 },
                };
            }

            Vector2 minUV = new Vector2( float.MaxValue, float.MaxValue );
            Vector2 maxUV = new Vector2( float.MinValue, float.MinValue );
            foreach ( Vector2 uv in renderer.sprite.uv )
            {
                minUV = Vector2.Min( minUV, uv );
                maxUV = Vector2.Max( maxUV, uv );
            }

            Vector2[] uvs =
            {
                new Vector2( minUV.x, maxUV.y ), new Vector2( maxUV.x, maxUV.y ),
                new Vector2( minUV.x, minUV.y ), new Vector2( maxUV.x, minUV.y ),
            };

            void ToggleFlip( ref bool flip )
            {
                flip = !flip;
            }

            if ( renderer.flipX )
            {
                ToggleFlip( ref flipX );
            }

            if ( renderer.flipY )
            {
                ToggleFlip( ref flipY );
            }

            if ( flipX )
            {
                uvs[ 0 ].x = maxUV.x;
                uvs[ 1 ].x = minUV.x;
                uvs[ 2 ].x = maxUV.x;
                uvs[ 3 ].x = minUV.x;
            }

            if ( flipY )
            {
                uvs[ 0 ].y = minUV.y;
                uvs[ 1 ].y = minUV.y;
                uvs[ 2 ].y = maxUV.y;
                uvs[ 3 ].y = maxUV.y;
            }

            quadMesh.uv = uvs;
            Vector2 pivotToCenter = ( renderer.sprite.rect.size / 2 - renderer.sprite.pivot ) / renderer.sprite.pixelsPerUnit;
            if ( renderer.flipX )
            {
                pivotToCenter.x = -pivotToCenter.x;
            }

            if ( renderer.flipY )
            {
                pivotToCenter.y = -pivotToCenter.y;
            }

            MaterialPropertyBlock mpb = new MaterialPropertyBlock();
            mpb.SetTexture( "_MainTex", renderer.sprite.texture );
            mpb.SetColor( "_Color", renderer.color );
            matrix *= Matrix4x4.Translate( pivotToCenter );
            matrix *= renderer.transform.localToWorldMatrix;
            matrix *= Matrix4x4.Scale( new Vector3(
                renderer.sprite.textureRect.width  / renderer.sprite.pixelsPerUnit,
                renderer.sprite.textureRect.height / renderer.sprite.pixelsPerUnit, 1 ) );
            Graphics.DrawMesh( quadMesh, matrix, renderer.sharedMaterial, 0, camera, 0, mpb );
        }

        public static bool   painting { get; set; }
        private const string PAINT_CMD = "Paint";

        private static Dictionary<string, List<GameObject>>
            Paint( IPaintToolSettings settings,               string commandName = PAINT_CMD,
                   bool               addTempCollider = true, bool   persistent  = false, string toolObjectId = "" )
        {
            painting = true;
            Dictionary<string, List<GameObject>> paintedObjects = new Dictionary<string,
                List<GameObject>>();
            if ( _paintStroke.Count == 0 )
            {
                if ( BrushstrokeManager.brushstroke.Length == 0 )
                {
                    BrushstrokeManager.UpdateBrushstroke();
                }

                return paintedObjects;
            }

            foreach ( PaintStrokeItem item in _paintStroke )
            {
                if ( item.prefab == null )
                {
                    continue;
                }

                string          persistentParentId = persistent ? item.persistentParentId : toolObjectId;
                PrefabAssetType type               = PrefabUtility.GetPrefabAssetType( item.prefab );
                GameObject obj = type == PrefabAssetType.NotAPrefab
                    ? Object.Instantiate( item.prefab )
                    : (GameObject)PrefabUtility.InstantiatePrefab
                    ( PrefabUtility.IsPartOfPrefabAsset( item.prefab )
                        ? item.prefab
                        : PrefabUtility.GetCorrespondingObjectFromSource( item.prefab ) );
                if ( settings.overwritePrefabLayer )
                {
                    obj.layer = settings.layer;
                }

                obj.transform.SetPositionAndRotation( item.position, item.rotation );
                obj.transform.localScale = item.scale;
                GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot( obj );
                item.parent = GetParent( settings, item.prefab.name, true, item.surface, persistentParentId );
                if ( addTempCollider )
                {
                    PWBCore.AddTempCollider( obj );
                }

                if ( !paintedObjects.ContainsKey( persistentParentId ) )
                {
                    paintedObjects.Add( persistentParentId, new List<GameObject>() );
                }

                paintedObjects[ persistentParentId ].Add( obj );
                SpriteRenderer[] spriteRenderers = obj.GetComponentsInChildren<SpriteRenderer>();

                foreach ( SpriteRenderer spriteRenderer in spriteRenderers )
                {
                    bool flipX = spriteRenderer.flipX;
                    bool flipY = spriteRenderer.flipY;
                    if ( item.flipX )
                    {
                        flipX = !flipX;
                    }

                    if ( item.flipY )
                    {
                        flipY = !flipY;
                    }

                    spriteRenderer.flipX = flipX;
                    spriteRenderer.flipY = flipY;
                    Vector3 center = BoundsUtils.GetBoundsRecursive( spriteRenderer.transform,
                        spriteRenderer.transform.rotation ).center;
                    Vector3 pivotToCenter = center - spriteRenderer.transform.position;
                    Vector3 delta         = Vector3.zero;
                    if ( item.flipX )
                    {
                        delta.x = pivotToCenter.x * -2;
                    }

                    if ( item.flipY )
                    {
                        delta.y = pivotToCenter.y * -2;
                    }

                    spriteRenderer.transform.position += delta;
                }

                AddPaintedObject( obj );
                Undo.RegisterCreatedObjectUndo( obj, commandName );
                if ( root != null )
                {
                    Undo.SetTransformParent( root.transform, item.parent, commandName );
                }
                else
                {
                    Undo.SetTransformParent( obj.transform, item.parent, commandName );
                }
            }

            if ( _paintStroke.Count > 0 )
            {
                BrushstrokeManager.UpdateBrushstroke();
            }

            _paintStroke.Clear();
            return paintedObjects;
        }

        public static void ResetAutoParent() => _autoParent = null;

        private const string NO_PALETTE_NAME      = "<#PALETTE@>";
        private const string NO_TOOL_NAME         = "<#TOOL@>";
        private const string NO_OBJ_ID            = "<#ID@>";
        private const string NO_BRUSH_NAME        = "<#BRUSH@>";
        private const string NO_PREFAB_NAME       = "<#PREFAB@>";
        private const string PARENT_KEY_SEPARATOR = "<#@>";

        private static Transform GetParent( IPaintToolSettings settings, string    prefabName,
                                            bool               create,   Transform surface, string toolObjectId = "" )
        {
            if ( !create )
            {
                return settings.parent;
            }

            if ( settings.autoCreateParent )
            {
                GameObject pwbObj = GameObject.Find( PWB_OBJ_NAME );
                if ( pwbObj == null )
                {
                    _autoParent = new GameObject( PWB_OBJ_NAME ).transform;
                }
                else
                {
                    _autoParent = pwbObj.transform;
                }
            }
            else
            {
                _autoParent = settings.setSurfaceAsParent ? surface : settings.parent;
            }

            if ( !settings.createSubparentPerPalette
                 && !settings.createSubparentPerTool
                 && !settings.createSubparentPerBrush
                 && !settings.createSubparentPerPrefab )
            {
                return _autoParent;
            }

            int _autoParentId = _autoParent == null ? -1 : _autoParent.gameObject.GetInstanceID();

            string GetSubParentKey( int    parentId = -1,        string palette = NO_PALETTE_NAME, string tool   = NO_TOOL_NAME,
                                    string id       = NO_OBJ_ID, string brush   = NO_BRUSH_NAME,   string prefab = NO_PREFAB_NAME )
            {
                return parentId
                       + PARENT_KEY_SEPARATOR
                       + palette
                       + PARENT_KEY_SEPARATOR
                       + tool
                       + PARENT_KEY_SEPARATOR
                       + id
                       + PARENT_KEY_SEPARATOR
                       + brush
                       + PARENT_KEY_SEPARATOR
                       + prefab;
            }

            string subParentKey = GetSubParentKey( _autoParentId,
                settings.createSubparentPerPalette ? PaletteManager.selectedPalette.name : NO_PALETTE_NAME,
                settings.createSubparentPerTool ? ToolManager.tool.ToString() : NO_TOOL_NAME,
                string.IsNullOrEmpty( toolObjectId ) ? NO_OBJ_ID : toolObjectId,
                settings.createSubparentPerBrush ? PaletteManager.selectedBrush.name : NO_BRUSH_NAME,
                settings.createSubparentPerPrefab ? prefabName : NO_PREFAB_NAME );

            create = !_subParents.ContainsKey( subParentKey );
            if ( !create
                 && _subParents[ subParentKey ] == null )
            {
                create = true;
            }

            if ( !create )
            {
                return _subParents[ subParentKey ];
            }

            Transform CreateSubParent( string key, string name, Transform transformParent )
            {
                Transform subParentTransform = null;
                bool      subParentIsEmpty   = true;
                if ( transformParent != null )
                {
                    subParentTransform = transformParent.Find( name );
                    if ( subParentTransform != null )
                    {
                        subParentIsEmpty = subParentTransform.GetComponents<Component>().Length == 1;
                    }
                }

                if ( subParentTransform == null
                     || !subParentIsEmpty )
                {
                    GameObject obj       = new GameObject( name );
                    Transform  subParent = obj.transform;
                    subParent.SetParent( transformParent );
                    subParent.localPosition = Vector3.zero;
                    subParent.localRotation = Quaternion.identity;
                    subParent.localScale    = Vector3.one;
                    if ( _subParents.ContainsKey( key ) )
                    {
                        _subParents[ key ] = subParent;
                    }
                    else
                    {
                        _subParents.Add( key, subParent );
                    }

                    return subParent;
                }

                return subParentTransform;
            }

            Transform parent = _autoParent;

            void CreateSubParentIfDoesntExist( string name,                  string palette = NO_PALETTE_NAME,
                                               string tool   = NO_TOOL_NAME, string id      = NO_OBJ_ID, string brush = NO_BRUSH_NAME,
                                               string prefab = NO_PREFAB_NAME )
            {
                string    key       = GetSubParentKey( _autoParentId, palette, tool, id, brush, prefab );
                bool      keyExist  = _subParents.ContainsKey( key );
                Transform subParent = keyExist ? _subParents[ key ] : null;
                if ( subParent != null )
                {
                    parent = subParent;
                }

                if ( !keyExist
                     || subParent == null )
                {
                    parent = CreateSubParent( key, name, parent );
                }
            }

            string[] keySplitted = subParentKey.Split( new[] { PARENT_KEY_SEPARATOR },
                StringSplitOptions.None );
            string keyPlaletteName = keySplitted[ 1 ];
            string keyToolName     = keySplitted[ 2 ];
            string keyToolObjId    = keySplitted[ 3 ];
            string keyBrushName    = keySplitted[ 4 ];
            string keyPrefabName   = keySplitted[ 5 ];

            if ( keyPlaletteName != NO_PALETTE_NAME )
            {
                CreateSubParentIfDoesntExist( keyPlaletteName, keyPlaletteName );
            }

            if ( keyToolName != NO_TOOL_NAME )
            {
                CreateSubParentIfDoesntExist( keyToolName, keyPlaletteName, keyToolName );
                if ( keyToolObjId != NO_OBJ_ID )
                {
                    CreateSubParentIfDoesntExist( keyToolObjId, keyPlaletteName, keyToolName, keyToolObjId );
                }
            }

            if ( keyBrushName != NO_BRUSH_NAME )
            {
                CreateSubParentIfDoesntExist( keyBrushName, keyPlaletteName, keyToolName, keyToolObjId, keyBrushName );
            }

            if ( keyPrefabName != NO_PREFAB_NAME )
            {
                CreateSubParentIfDoesntExist( keyPrefabName, keyPlaletteName,
                    keyToolName,                             keyToolObjId, keyBrushName, keyPrefabName );
            }

            return parent;
        }

        private static bool IsVisible( ref GameObject obj )
        {
            if ( obj == null )
            {
                return false;
            }

            Renderer parentRenderer = obj.GetComponentInParent<Renderer>();
            Terrain  parentTerrain  = obj.GetComponentInParent<Terrain>();
            if ( parentRenderer != null )
            {
                obj = parentRenderer.gameObject;
            }
            else if ( parentTerrain != null )
            {
                obj = parentTerrain.gameObject;
            }
            else
            {
                Transform parent = obj.transform.parent;
                if ( parent != null )
                {
                    Renderer siblingRenderer = parent.GetComponentInChildren<Renderer>();
                    Terrain  siblingTerrain  = parent.GetComponentInChildren<Terrain>();
                    if ( siblingRenderer != null )
                    {
                        obj = parent.gameObject;
                    }
                    else if ( siblingTerrain != null )
                    {
                        obj = parent.gameObject;
                    }

                }
            }

            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            if ( renderers.Length > 0 )
            {
                foreach ( Renderer renderer in renderers )
                {
                    if ( renderer.enabled )
                    {
                        return true;
                    }
                }
            }

            Terrain[] terrains = obj.GetComponentsInChildren<Terrain>();
            if ( terrains.Length > 0 )
            {
                foreach ( Terrain terrain in terrains )
                {
                    if ( terrain.enabled )
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsVisible( GameObject obj )
        {
            obj = PWBCore.GetGameObjectFromTempCollider( obj );
            return IsVisible( ref obj );
        }

        private struct TerrainDataSimple
        {

            #region Public Fields

            public float[ ,, ]    alphamaps;
            public TerrainLayer[] layers;
            public Vector3        size;

            #endregion

            #region Public Constructors

            public TerrainDataSimple( float[ ,, ] alphamaps, Vector3 size, TerrainLayer[] layers )
            {
                ( this.alphamaps, this.size, this.layers ) = ( alphamaps, size, layers );
            }

            #endregion

        }

        private static Dictionary<int, TerrainDataSimple> _terrainAlphamaps
            = new Dictionary<int, TerrainDataSimple>();

        public static bool MouseRaycast( Ray            mouseRay,              out RaycastHit mouseHit,
                                         out GameObject collider,              float          maxDistance,                 LayerMask layerMask,
                                         bool           paintOnPalettePrefabs, bool           castOnMeshesWithoutCollider, string[]  tags = null,
                                         TerrainLayer[] terrainLayers = null,  GameObject[]   exceptions = null )
        {
            bool IsTempCollider( GameObject obj )
            {
                Transform hitParent = obj.transform.parent;
                return hitParent != null && hitParent.gameObject.GetInstanceID() == PWBCore.parentColliderId;
            }

            GameObject GetOriginalCollider( GameObject obj )
            {
                if ( IsTempCollider( obj ) )
                {
                    return PWBCore.GetGameObjectFromTempColliderId( obj.GetInstanceID() );
                }

                return obj;
            }

            bool TagFilterPassed( GameObject obj )
            {
                if ( tags == null )
                {
                    return true;
                }

                if ( tags.Length == 0 )
                {
                    return true;
                }

                if ( obj.tag == "untagged" )
                {
                    return true;
                }

                return tags.Contains( obj.tag );
            }

            bool ExceptionFilterPassed( GameObject obj )
            {
                if ( exceptions == null )
                {
                    return true;
                }

                if ( exceptions.Length == 0 )
                {
                    return true;
                }

                return !exceptions.Contains( obj );
            }

            bool PaletteFilterPassed( GameObject obj )
            {
                if ( paintOnPalettePrefabs )
                {
                    return true;
                }

                return !PaletteManager.selectedPalette.ContainsSceneObject( obj );
            }

            bool TerrainFilterPassed( GameObject obj, Vector3 mouseHitPoint )
            {
                if ( terrainLayers == null )
                {
                    return true;
                }

                if ( terrainLayers.Length == 0 )
                {
                    return true;
                }

                Terrain terrain = obj.GetComponent<Terrain>();
                if ( terrain == null )
                {
                    return true;
                }

                int            instanceId = terrain.GetInstanceID();
                int            alphamapW  = 0;
                int            alphamapH  = 0;
                float[ ,, ]    alphamaps;
                Vector3        terrainSize;
                TerrainLayer[] layers;
                if ( _terrainAlphamaps.ContainsKey( instanceId ) )
                {
                    alphamaps   = _terrainAlphamaps[ instanceId ].alphamaps;
                    alphamapW   = alphamaps.GetLength( 1 );
                    alphamapH   = alphamaps.GetLength( 0 );
                    terrainSize = _terrainAlphamaps[ instanceId ].size;
                    layers      = _terrainAlphamaps[ instanceId ].layers;
                }
                else
                {
                    TerrainData terrainData = terrain.terrainData;
                    alphamapW   = terrainData.alphamapWidth;
                    alphamapH   = terrainData.alphamapHeight;
                    alphamaps   = terrainData.GetAlphamaps( 0, 0, alphamapW, alphamapH );
                    terrainSize = terrainData.size;
                    layers      = terrainData.terrainLayers;
                    _terrainAlphamaps.Add( instanceId, new TerrainDataSimple( alphamaps, terrainSize, layers ) );

                }

                int numLayers = alphamaps.GetLength( 2 );

                Vector3 localHit  = terrain.transform.InverseTransformPoint( mouseHitPoint );
                int     alphaHitX = Mathf.Clamp( Mathf.RoundToInt( localHit.x / terrainSize.x * alphamapW ), 0, alphamapW - 1 );
                int     alphaHitZ = Mathf.Clamp( Mathf.RoundToInt( localHit.z / terrainSize.z * alphamapH ), 0, alphamapH - 1 );

                int layerUnderCursorIdx = 0;
                for ( int k = 1; k < numLayers; k++ )
                {
                    if ( alphamaps[ alphaHitZ, alphaHitX, k ] > 0.5 )
                    {
                        layerUnderCursorIdx = k;
                        break;
                    }
                }

                TerrainLayer layerUnderCursor = layers[ layerUnderCursorIdx ];
                return terrainLayers.Contains( layerUnderCursor );
            }

            bool AllFiltersPassed( ref GameObject obj, Vector3 mouseHitPoint )
            {
                if ( obj == null )
                {
                    return false;
                }

                if ( !IsVisible( ref obj ) )
                {
                    return false;
                }

                if ( !TagFilterPassed( obj ) )
                {
                    return false;
                }

                if ( !ExceptionFilterPassed( obj ) )
                {
                    return false;
                }

                if ( !PaletteFilterPassed( obj ) )
                {
                    return false;
                }

                if ( !TerrainFilterPassed( obj, mouseHitPoint ) )
                {
                    return false;
                }

                return true;
            }

            mouseHit = new RaycastHit();
            collider = null;
            bool validHit = Physics.Raycast( mouseRay, out mouseHit, maxDistance, layerMask, QueryTriggerInteraction.Ignore );
            if ( validHit )
            {
                collider = mouseHit.collider.gameObject;
            }

            GameObject[] nearbyObjects = null;
            if ( castOnMeshesWithoutCollider )
            {
                nearbyObjects = octree.GetNearby( mouseRay, 1f ).Where( o => o != null ).ToArray();
                if ( MeshUtils.Raycast( mouseRay, out RaycastHit meshHit, out GameObject meshCollider,
                        nearbyObjects, maxDistance ) )
                {
                    if ( !validHit
                         || meshHit.distance < mouseHit.distance )
                    {
                        mouseHit = meshHit;
                        collider = meshCollider;
                        validHit = true;
                    }
                }
            }

            if ( validHit && collider != null )
            {
                GameObject obj = GetOriginalCollider( collider );
                if ( AllFiltersPassed( ref obj, mouseHit.point ) )
                {
                    collider = obj;
                    return true;
                }
            }

            Dictionary<GameObject, RaycastHit> hitDictionary = Physics.RaycastAll( mouseRay, maxDistance, layerMask, QueryTriggerInteraction.Ignore )
                                                                      .ToDictionary( hit => hit.collider.gameObject, hit => hit );
            if ( castOnMeshesWithoutCollider )
            {
                if ( MeshUtils.RaycastAll( mouseRay, out RaycastHit[] hitArray, out GameObject[] colliders,
                        nearbyObjects, maxDistance ) )
                {
                    for ( int i = 0; i < hitArray.Length; ++i )
                    {
                        if ( !hitDictionary.ContainsKey( colliders[ i ] ) )
                        {
                            hitDictionary.Add( colliders[ i ], hitArray[ i ] );
                        }
                    }
                }
            }

            if ( collider != null
                 && hitDictionary.ContainsKey( collider ) )
            {
                hitDictionary.Remove( collider );
            }

            float minDistance = float.MaxValue;
            collider = null;
            validHit = false;
            foreach ( KeyValuePair<GameObject, RaycastHit> hitPair in hitDictionary )
            {
                float      hitDistance = hitPair.Value.distance;
                GameObject hitCollider = GetOriginalCollider( hitPair.Key );
                if ( !AllFiltersPassed( ref hitCollider, mouseHit.point ) )
                {
                    continue;
                }

                if ( hitDistance < minDistance )
                {
                    minDistance = hitDistance;
                    mouseHit    = hitPair.Value;
                    collider    = hitCollider;
                    validHit    = true;
                }
            }

            return validHit;
        }

        public static float GetBottomDistanceToSurface( Vector3[] bottomVertices,  Matrix4x4 TRS,
                                                        float     bottomMagnitude, bool      paintOnPalettePrefabs, bool castOnMeshesWithoutCollider )
        {
            float   positiveDistance = float.MaxValue;
            float   negativeDistance = float.MinValue;
            Vector3 down             = ( TRS.rotation * Vector3.down ).normalized;
            bool    noSurfaceFound   = true;

            void GetDistance( float height, Vector3 direction )
            {
                foreach ( Vector3 vertex in bottomVertices )
                {
                    Vector3 origin = TRS.MultiplyPoint( vertex );

                    Ray ray = new Ray( origin - direction * height, direction );
                    if ( MouseRaycast( ray, out RaycastHit hitInfo, out GameObject collider,
                            float.MaxValue, -1,                     paintOnPalettePrefabs, castOnMeshesWithoutCollider ) )
                    {

                        if ( hitInfo.distance >= height )
                        {
                            positiveDistance = Mathf.Min( hitInfo.distance - height, positiveDistance );
                        }
                        else
                        {
                            negativeDistance = Mathf.Max( height - hitInfo.distance, negativeDistance );
                        }

                        noSurfaceFound = false;
                    }
                }
            }

            float hMult = 100f;
            GetDistance( Mathf.Max( bottomMagnitude * hMult, hMult ), down );
            if ( noSurfaceFound )
            {
                return 0f;
            }

            if ( positiveDistance == float.MaxValue )
            {
                positiveDistance = 0f;
            }

            if ( negativeDistance == float.MinValue )
            {
                negativeDistance = 0f;
            }

            float distance = positiveDistance >= negativeDistance ? positiveDistance : -negativeDistance;
            return distance;
        }

        public static float GetBottomDistanceToSurfaceSigned( Vector3[] bottomVertices, Matrix4x4 TRS,
                                                              float     maxDistance,    bool      paintOnPalettePrefabs, bool castOnMeshesWithoutCollider )
        {
            float   distance = 0f;
            Vector3 down     = Vector3.down;
            foreach ( Vector3 vertex in bottomVertices )
            {
                Vector3 origin = TRS.MultiplyPoint( vertex );
                Ray     ray    = new Ray( origin - down * maxDistance, down );
                if ( MouseRaycast( ray, out RaycastHit hitInfo, out GameObject collider,
                        float.MaxValue, -1,                     paintOnPalettePrefabs, castOnMeshesWithoutCollider ) )
                {
                    float d = hitInfo.distance - maxDistance;
                    if ( Mathf.Abs( d ) > Mathf.Abs( distance ) )
                    {
                        distance = d;
                    }
                }
            }

            return distance;
        }

        public static float GetPivotDistanceToSurfaceSigned( Vector3 pivot,
                                                             float   maxDistance, bool paintOnPalettePrefabs, bool castOnMeshesWithoutCollider )
        {
            Ray ray = new Ray( pivot + Vector3.up * maxDistance, Vector3.down );
            if ( MouseRaycast( ray, out RaycastHit hitInfo, out GameObject collider,
                    float.MaxValue, -1,                     paintOnPalettePrefabs, castOnMeshesWithoutCollider ) )
            {
                return hitInfo.distance - maxDistance;
            }

            return 0;
        }

        private static BrushstrokeItem[] _brushstroke;

        private struct PreviewData
        {

            #region Public Fields

            public readonly bool       flipX;
            public readonly bool       flipY;
            public readonly int        layer;
            public readonly GameObject prefab;
            public readonly Matrix4x4  rootToWorld;

            #endregion

            #region Public Constructors

            public PreviewData( GameObject prefab, Matrix4x4 rootToWorld, int layer, bool flipX, bool flipY )
            {
                this.prefab      = prefab;
                this.rootToWorld = rootToWorld;
                this.layer       = layer;
                this.flipX       = flipX;
                this.flipY       = flipY;
            }

            #endregion

        }

        private static List<PreviewData> _previewData
            = new List<PreviewData>();

        private static bool PreviewIfBrushtrokestaysTheSame( out BrushstrokeItem[] brushstroke,
                                                             Camera                camera, bool forceUpdate )
        {
            brushstroke = BrushstrokeManager.brushstroke;
            if ( !forceUpdate
                 && _brushstroke != null
                 && BrushstrokeManager.BrushstrokeEqual( brushstroke, _brushstroke ) )
            {
                foreach ( PreviewData previewItemData in _previewData )
                {
                    PreviewBrushItem( previewItemData.prefab, previewItemData.rootToWorld,
                        previewItemData.layer,                camera, false, false, previewItemData.flipX, previewItemData.flipY );
                }

                return true;
            }

            _brushstroke = BrushstrokeManager.brushstrokeClone;
            _previewData.Clear();
            return false;
        }

        private static Dictionary<long, PreviewData[]> _persistentPreviewData
            = new Dictionary<long, PreviewData[]>();

        private static Dictionary<long, BrushstrokeItem[]> _persistentLineBrushstrokes
            = new Dictionary<long, BrushstrokeItem[]>();

        private static void PreviewPersistent( Camera camera )
        {
            foreach ( PreviewData[] previewDataArray in _persistentPreviewData.Values )
            foreach ( PreviewData previewItemData in previewDataArray )
            {
                PreviewBrushItem( previewItemData.prefab, previewItemData.rootToWorld,
                    previewItemData.layer,                camera, false, false, previewItemData.flipX, previewItemData.flipY );
            }
        }

        #endregion

        #region BRUSH SHAPE INDICATOR

        private static void DrawCricleIndicator( Vector3 hitPoint,       Vector3  hitNormal,
                                                 float   radius,         float    height,                Vector3 tangent, Vector3 bitangent,
                                                 Vector3 normal,         bool     paintOnPalettePrefabs, bool    castOnMeshesWithoutCollider,
                                                 int     layerMask = -1, string[] tags = null )
        {
            Handles.zTest = CompareFunction.Always;
            const float normalOffset    = 0.01f;
            const float polygonSideSize = 0.3f;
            const int   minPolygonSides = 12;
            const int   maxPolygonSides = 36;
            int         polygonSides    = Mathf.Clamp( (int)( TAU * radius / polygonSideSize ), minPolygonSides, maxPolygonSides );

            Handles.color = new Color( 0f, 0f, 0f, 0.5f );
            List<Vector3> periPoints       = new List<Vector3>();
            List<Vector3> periPointsShadow = new List<Vector3>();
            for ( int i = 0; i < polygonSides; ++i )
            {
                float   radians    = TAU * i / ( polygonSides - 1f );
                Vector2 tangentDir = new Vector2( Mathf.Cos( radians ), Mathf.Sin( radians ) );
                Vector3 worldDir   = TangentSpaceToWorld( tangent, bitangent, tangentDir );
                Vector3 periPoint  = hitPoint + worldDir * radius;
                Ray     periRay    = new Ray( periPoint + normal * height, -normal );
                if ( MouseRaycast( periRay, out RaycastHit periHit, out GameObject collider,
                        height * 2,         layerMask,              paintOnPalettePrefabs, castOnMeshesWithoutCollider, tags ) )
                {
                    Vector3 periHitPoint = periHit.point + hitNormal * normalOffset;
                    Vector3 shadowPoint  = periHitPoint  + worldDir  * 0.2f;
                    periPoints.Add( periHitPoint );
                    periPointsShadow.Add( shadowPoint );
                }
                else
                {
                    if ( periPoints.Count > 0
                         && i             == polygonSides - 1 )
                    {
                        periPoints.Add( periPoints[ 0 ] );
                        periPointsShadow.Add( periPointsShadow[ 0 ] );
                    }
                    else
                    {
                        float binSearchRadius = radius;
                        float delta           = -binSearchRadius / 2;

                        for ( int j = 0; j < 8; ++j )
                        {
                            binSearchRadius += delta;
                            periPoint       =  hitPoint + worldDir * binSearchRadius;
                            periRay         =  new Ray( periPoint + normal * height, -normal );
                            if ( MouseRaycast( periRay,               out RaycastHit binSearchPeriHit,
                                    out GameObject binSearchCollider, height * 2,                  layerMask,
                                    paintOnPalettePrefabs,            castOnMeshesWithoutCollider, tags ) )
                            {
                                delta   = Mathf.Abs( delta ) / 2;
                                periHit = binSearchPeriHit;
                            }
                            else
                            {
                                delta = -Mathf.Abs( delta ) / 2;
                            }

                            if ( Mathf.Abs( delta ) < 0.01 )
                            {
                                break;
                            }
                        }

                        if ( periHit.point == Vector3.zero )
                        {
                            continue;
                        }

                        Vector3 periHitPoint = periHit.point + hitNormal * normalOffset;
                        Vector3 shadowPoint  = periHitPoint  + worldDir  * 0.2f;
                        periPoints.Add( periHitPoint );
                        periPointsShadow.Add( shadowPoint );
                    }
                }
            }

            if ( periPoints.Count > 0 )
            {
                Handles.color = new Color( 1f, 1f, 1f, 0.5f );
                Handles.DrawAAPolyLine( 3, periPoints.ToArray() );
                Handles.color = new Color( 0f, 0f, 0f, 0.5f );
                Handles.DrawAAPolyLine( 6, periPointsShadow.ToArray() );
            }
            else
            {
                Handles.color = new Color( 1f, 1f, 1f, 0.5f );
                Handles.DrawWireDisc( hitPoint + hitNormal * normalOffset, hitNormal, radius );
                Handles.color = new Color( 0f, 0f, 0f, 0.5f );
                Handles.DrawWireDisc( hitPoint + hitNormal * normalOffset, hitNormal, radius + 0.2f );
            }
        }

        private static void DrawSquareIndicator( Vector3 hitPoint,       Vector3  hitNormal,
                                                 float   radius,         float    height,                Vector3 tangent, Vector3 bitangent,
                                                 Vector3 normal,         bool     paintOnPalettePrefabs, bool    castOnMeshesWithoutCollider,
                                                 int     layerMask = -1, string[] tags = null )
        {
            Handles.zTest = CompareFunction.Always;
            const float normalOffset = 0.01f;

            const int minSideSegments = 4;
            const int maxSideSegments = 15;
            int       segmentsPerSide = Mathf.Clamp( (int)( radius * 2 / 0.3f ), minSideSegments, maxSideSegments );
            int       segmentCount    = segmentsPerSide * 4;
            float     segmentSize     = radius * 2f     / segmentsPerSide;
            float     SQRT2           = Mathf.Sqrt( 2f );
            Handles.color = new Color( 0f, 0f, 0f, 0.5f );
            List<Vector3> periPoints = new List<Vector3>();

            for ( int i = 0; i < segmentCount; ++i )
            {
                int     sideIdx    = i / segmentsPerSide;
                int     segmentIdx = i % segmentsPerSide;
                Vector3 periPoint  = hitPoint;
                if ( sideIdx == 0 )
                {
                    periPoint += tangent * ( segmentSize * segmentIdx - radius ) + bitangent * radius;
                }
                else if ( sideIdx == 1 )
                {
                    periPoint += bitangent * ( radius - segmentSize * segmentIdx ) + tangent * radius;
                }
                else if ( sideIdx == 2 )
                {
                    periPoint += tangent * ( radius - segmentSize * segmentIdx ) - bitangent * radius;
                }
                else
                {
                    periPoint += bitangent * ( segmentSize * segmentIdx - radius ) - tangent * radius;
                }

                Vector3 worldDir = ( periPoint - hitPoint ).normalized;
                Ray     periRay  = new Ray( periPoint + normal * height, -normal );
                if ( MouseRaycast( periRay, out RaycastHit periHit, out GameObject collider,
                        height * 2,         layerMask,              paintOnPalettePrefabs, castOnMeshesWithoutCollider, tags ) )
                {
                    Vector3 periHitPoint = periHit.point + hitNormal * normalOffset;
                    periPoints.Add( periHitPoint );
                }
                else
                {
                    float binSearchRadius = radius           * SQRT2;
                    float delta           = -binSearchRadius / 2;

                    for ( int j = 0; j < 8; ++j )
                    {
                        binSearchRadius += delta;
                        periPoint       =  hitPoint + worldDir * binSearchRadius;
                        periRay         =  new Ray( periPoint + normal * height, -normal );
                        if ( MouseRaycast( periRay,               out RaycastHit binSearchPeriHit,
                                out GameObject binSearchCollider, height * 2,                  layerMask,
                                paintOnPalettePrefabs,            castOnMeshesWithoutCollider, tags ) )
                        {
                            delta   = Mathf.Abs( delta ) / 2;
                            periHit = binSearchPeriHit;
                        }
                        else
                        {
                            delta = -Mathf.Abs( delta ) / 2;
                        }

                        if ( Mathf.Abs( delta ) < 0.01 )
                        {
                            break;
                        }
                    }

                    if ( periHit.point == Vector3.zero )
                    {
                        continue;
                    }

                    Vector3 periHitPoint = periHit.point + hitNormal * normalOffset;
                    Vector3 shadowPoint  = periHitPoint  + worldDir  * 0.2f;
                    periPoints.Add( periHitPoint );

                }
            }

            if ( periPoints.Count > 0 )
            {
                periPoints.Add( periPoints[ 0 ] );
                Handles.color = new Color( 0f, 0f, 0f, 0.7f );
                Handles.DrawAAPolyLine( 8, periPoints.ToArray() );

                Handles.color = new Color( 1f, 1f, 1f, 0.7f );
                Handles.DrawAAPolyLine( 4, periPoints.ToArray() );
            }
        }

        #endregion

        #region DRAG AND DROP

        public class SceneDragReceiver : ISceneDragReceiver
        {

            #region Public Properties

            public int brushId { get; set; } = -1;

            #endregion

            #region Public Methods

            public void PerformDrag( Event evt )
            {
            }

            public void StartDrag()
            {
            }

            public void StopDrag()
            {
            }

            public DragAndDropVisualMode UpdateDrag( Event evt, EventType eventType )
            {
                PrefabPalette.instance.DeselectAllButThis( brushId );
                ToolManager.tool = ToolManager.PaintTool.PIN;
                return DragAndDropVisualMode.Generic;
            }

            #endregion

            #region Private Fields

            #endregion

        }

        public static SceneDragReceiver sceneDragReceiver { get; } = new SceneDragReceiver();

        #endregion

        #region PALETTE

        private static void PaletteInput( SceneView sceneView )
        {
            void Repaint()
            {
                PrefabPalette.RepainWindow();
                sceneView.Repaint();
                repaint = true;
                AsyncRepaint();
            }

            if ( PWBSettings.shortcuts.palettePreviousBrush.Check() )
            {
                PaletteManager.SelectPreviousBrush();
                Repaint();
            }
            else if ( PWBSettings.shortcuts.paletteNextBrush.Check() )
            {
                PaletteManager.SelectNextBrush();
                Repaint();
            }

            if ( PWBSettings.shortcuts.paletteNextBrushScroll.Check() )
            {
                Event.current.Use();
                if ( PWBSettings.shortcuts.paletteNextBrushScroll.combination.delta > 0 )
                {
                    PaletteManager.SelectNextBrush();
                }
                else
                {
                    PaletteManager.SelectPreviousBrush();
                }

                Repaint();
            }

            if ( PWBSettings.shortcuts.paletteNextPaletteScroll.Check() )
            {
                Event.current.Use();
                if ( Event.current.delta.y > 0 )
                {
                    PaletteManager.SelectNextPalette();
                }
                else
                {
                    PaletteManager.SelectPreviousPalette();
                }

                Repaint();
            }

            if ( PWBSettings.shortcuts.palettePreviousPalette.Check() )
            {
                PaletteManager.SelectPreviousPalette();
                Repaint();
            }
            else if ( PWBSettings.shortcuts.paletteNextPalette.Check() )
            {
                PaletteManager.SelectNextPalette();
                Repaint();
            }

            bool pickShortcutOn = PWBSettings.shortcuts.palettePickBrush.Check();
            bool pickBrush = PaletteManager.pickingBrushes
                             && Event.current.button == 0
                             && Event.current.type   == EventType.MouseDown;
            if ( pickShortcutOn || pickBrush )
            {
                Ray mouseRay = HandleUtility.GUIPointToWorldRay( Event.current.mousePosition );
                if ( MouseRaycast( mouseRay, out RaycastHit mouseHit, out GameObject collider,
                        float.MaxValue,      -1,                      true, true ) )
                {
                    GameObject target          = collider.gameObject;
                    GameObject outermostPrefab = PrefabUtility.GetOutermostPrefabInstanceRoot( target );
                    if ( outermostPrefab != null )
                    {
                        target = outermostPrefab;
                    }

                    int brushIdx = PaletteManager.selectedPalette.FindBrushIdx( target );
                    if ( brushIdx >= 0 )
                    {
                        PaletteManager.SelectBrush( brushIdx );
                    }
                    else if ( outermostPrefab != null )
                    {
                        GameObject prefabAsset = PrefabUtility.GetCorrespondingObjectFromSource( outermostPrefab );
                        PrefabPalette.instance.CreateBrushFromSelection( prefabAsset );
                    }
                }

                Event.current.Use();
                if ( !pickShortcutOn && pickBrush )
                {
                    PaletteManager.pickingBrushes = false;
                }
            }

            if ( PWBSettings.shortcuts.palettePickBrush.holdKeysAndClickCombination.holdingChanged )
            {
                PaletteManager.pickingBrushes = PWBSettings.shortcuts.palettePickBrush.holdKeysAndClickCombination.holdingKeys;
            }
        }

        private static async void AsyncRepaint()
        {
            await Task.Delay( 500 );
            repaint = true;
        }

        #endregion

    }
}