using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PluginMaster
{

    #region DATA & SETTINGS

    [Serializable]
    public class SelectionToolSettings : SelectionToolBase, IToolSettings, ISerializationCallbackReceiver
    {

        #region Serialized

        [SerializeField] private bool         _move = true;
        [SerializeField] private bool         _rotate;
        [SerializeField] private bool         _scale;
        [SerializeField] private Space        _handleSpace = Space.Self;
        [SerializeField] private Space        _boxSpace    = Space.Self;
        [SerializeField] private bool         _paletteFilter;
        [SerializeField] private bool         _brushFilter;
        [SerializeField] private LayerMask    _layerFilter = -1;
        [SerializeField] private List<string> _tagFilter;

        #endregion

        #region Public Properties

        public Space boxSpace
        {
            get => _boxSpace;
            set
            {
                if ( _boxSpace == value )
                {
                    return;
                }

                _boxSpace = value;
                DataChanged();
            }
        }

        public bool brushFilter
        {
            get => _brushFilter;
            set
            {
                if ( _brushFilter == value )
                {
                    return;
                }

                _brushFilter = value;
                DataChanged();
            }
        }

        public Space handleSpace
        {
            get => _handleSpace;
            set
            {
                if ( _handleSpace == value )
                {
                    return;
                }

                _handleSpace = value;
                if ( _handleSpace == Space.World )
                {
                    _scale = false;
                }

                DataChanged();
            }
        }

        public LayerMask layerFilter
        {
            get => _layerFilter;
            set
            {
                if ( _layerFilter == value )
                {
                    return;
                }

                _layerFilter = value;
                DataChanged();
            }
        }

        public bool move
        {
            get => _move;
            set
            {
                if ( _move == value )
                {
                    return;
                }

                _move = value;
                DataChanged();
            }
        }

        public bool paletteFilter
        {
            get => _paletteFilter;
            set
            {
                if ( _paletteFilter == value )
                {
                    return;
                }

                _paletteFilter = value;
                DataChanged();
            }
        }

        public bool rotate
        {
            get => _rotate;
            set
            {
                if ( _rotate == value )
                {
                    return;
                }

                _rotate = value;
                DataChanged();
            }
        }

        public bool scale
        {
            get => _scale;
            set
            {
                if ( _scale == value )
                {
                    return;
                }

                _scale = value;
                if ( _scale )
                {
                    _handleSpace = Space.Self;
                }

                DataChanged();
            }
        }

        public List<string> tagFilter
        {
            get
            {
                if ( _tagFilter == null )
                {
                    UpdateTagFilter();
                }

                return _tagFilter;
            }
            set
            {
                if ( _tagFilter == value )
                {
                    return;
                }

                _tagFilter = value;
                DataChanged();
            }
        }

        #endregion

        #region Public Methods

        public override void Copy( IToolSettings other )
        {
            SelectionToolSettings otherSelectionToolSettings = other as SelectionToolSettings;
            if ( otherSelectionToolSettings == null )
            {
                return;
            }

            base.Copy( other );
            _move          = otherSelectionToolSettings._move;
            _rotate        = otherSelectionToolSettings._rotate;
            _scale         = otherSelectionToolSettings._scale;
            _handleSpace   = otherSelectionToolSettings._handleSpace;
            _boxSpace      = otherSelectionToolSettings._boxSpace;
            _paletteFilter = otherSelectionToolSettings._paletteFilter;
            _brushFilter   = otherSelectionToolSettings._brushFilter;
            _layerFilter   = otherSelectionToolSettings._layerFilter;
            _tagFilter = otherSelectionToolSettings._tagFilter == null
                ? null
                : new List<string>( otherSelectionToolSettings._tagFilter );
        }

        public void OnAfterDeserialize() => UpdateTagFilter();
        public void OnBeforeSerialize()  => UpdateTagFilter();

        #endregion

        #region Private Methods

        private void UpdateTagFilter()
        {
            if ( _tagFilter != null )
            {
                return;
            }

            _tagFilter = new List<string>( InternalEditorUtility.tags );
        }

        #endregion

    }

    [Serializable]
    public class SelectionToolManager : ToolManagerBase<SelectionToolSettings>
    {
    }

    #endregion

    #region PWBIO

    public static partial class PWBIO
    {

        #region Statics and Constants

        private static int                                        _selectedBoxPointIdx = -1;
        private static Quaternion                                 _selectionRotation   = Quaternion.identity;
        private static Vector3                                    _selectionScale      = Vector3.one;
        private static Vector3                                    _snappedPoint;
        private static bool                                       _snappedPointIsVisible;
        private static bool                                       _snappedPointIsSelected;
        private static (Vector3 position, GameObject[] selection) _selectionMoveFrom;
        private static bool                                       _selectionMoving;
        private static bool                                       _editingSelectionHandlePosition;
        private static Vector3                                    _tempSelectionHandle = Vector3.zero;
        private static bool                                       _selectionChanged;
        private static Bounds                                     _selectionBounds;
        private static bool                                       _setSelectionOriginPosition;

        #endregion

        #region Public Methods

        public static void ApplySelectionFilters()
        {
            GameObject[] selection = SelectionManager.topLevelSelection;
            if ( selection == null )
            {
                SelectionManager.UpdateSelection();
            }

            if ( SelectionToolManager.settings.paletteFilter )
            {
                if ( PaletteManager.selectedPalette == null )
                {
                    selection = new GameObject[ 0 ];
                }
                else
                {
                    selection = selection.Where( obj => PaletteManager.selectedPalette.ContainsSceneObject( obj ) ).ToArray();
                }
            }

            if ( SelectionToolManager.settings.brushFilter )
            {
                if ( PaletteManager.selectedBrush == null )
                {
                    selection = new GameObject[ 0 ];
                }
                else
                {
                    selection = selection.Where( obj => PaletteManager.selectedBrush.ContainsSceneObject( obj ) ).ToArray();
                }
            }

            if ( SelectionToolManager.settings.layerFilter != -1 )
            {
                LayerMask layerMask = SelectionToolManager.settings.layerFilter;
                selection = selection.Where( obj => ( layerMask & ( 1 << obj.layer ) ) != 0 ).ToArray();
            }

            if ( SelectionToolManager.settings.tagFilter.Count > 0 )
            {
                selection = selection.Where( obj => SelectionToolManager.settings.tagFilter.Contains( obj.tag ) ).ToArray();
            }
            else
            {
                selection = new GameObject[ 0 ];
            }

            Selection.objects = selection;
        }

        public static void EmbedSelectionInSurface() => EmbedSelectionInSurface( _selectionRotation );

        public static void ResetSelectionRotation()
        {
            _selectionRotation = Quaternion.identity;
            UpdateSelection();
        }

        public static void SetSelectionOriginPosition() => _setSelectionOriginPosition = true;

        #endregion

        #region Private Methods

        private static void EmbedSelectionInSurface( Quaternion rotation )
        {
            PWBCore.SetActiveTempColliders( SelectionManager.topLevelSelection, false );
            PlaceOnSurfaceUtils.PlaceOnSurfaceData placeOnSurfaceData = new PlaceOnSurfaceUtils.PlaceOnSurfaceData();
            placeOnSurfaceData.projectionDirectionSpace = Space.World;
            placeOnSurfaceData.rotateToSurface          = false;
            float[] objHeight = new float[ SelectionManager.topLevelSelection.Length ];
            for ( int i = 0; i < SelectionManager.topLevelSelection.Length; ++i )
            {
                GameObject obj = SelectionManager.topLevelSelection[ i ];
                objHeight[ i ] = BoundsUtils.GetMagnitude( obj.transform );
                obj.SetActive( false );
            }

            for ( int i = 0; i < SelectionManager.topLevelSelection.Length; ++i )
            {
                GameObject obj             = SelectionManager.topLevelSelection[ i ];
                Vector3[]  bottomVertices  = BoundsUtils.GetBottomVertices( obj.transform );
                float      bottomMagnitude = Mathf.Abs( BoundsUtils.GetBottomMagnitude( obj.transform ) );
                Matrix4x4  TRS             = obj.transform.localToWorldMatrix;
                float surfceDistance = SelectionToolManager.settings.embedAtPivotHeight
                    ? GetPivotDistanceToSurfaceSigned( obj.transform.position, bottomMagnitude, true, true )
                    : GetBottomDistanceToSurface( bottomVertices, TRS, bottomMagnitude, true, true );
                surfceDistance -= SelectionToolManager.settings.surfaceDistance;
                if ( surfceDistance != 0f )
                {
                    Vector3 euler = obj.transform.rotation.eulerAngles;
                    Vector3 delta = obj.transform.rotation * new Vector3( 0f, -surfceDistance, 0f );
                    obj.transform.position += obj.transform.rotation * new Vector3( 0f, -surfceDistance, 0f );
                }

                if ( SelectionToolManager.settings.rotateToTheSurface )
                {
                    Vector3 down = obj.transform.rotation * Vector3.down;
                    Ray     ray  = new Ray( obj.transform.position - down * objHeight[ i ], down );
                    if ( MouseRaycast( ray, out RaycastHit hitInfo, out GameObject collider,
                            float.MaxValue, -1,                     true, true ) )
                    {
                        Vector3 tangent = Vector3.Cross( hitInfo.normal, Vector3.left );
                        if ( tangent.sqrMagnitude < 0.000001 )
                        {
                            tangent = Vector3.Cross( hitInfo.normal, Vector3.back );
                        }

                        tangent                = tangent.normalized;
                        obj.transform.rotation = Quaternion.LookRotation( tangent, hitInfo.normal );
                    }
                }
            }

            foreach ( GameObject obj in SelectionManager.topLevelSelection )
            {
                obj.SetActive( true );
            }

            _selectionBounds = BoundsUtils.GetSelectionBounds( SelectionManager.topLevelSelection, rotation );
            PWBCore.SetActiveTempColliders( SelectionManager.topLevelSelection, true );
        }

        private static Quaternion GetSelectionRotation()
        {
            Quaternion rotation = _selectionRotation;
            if ( SelectionManager.topLevelSelection.Length == 1 )
            {
                if ( SelectionManager.topLevelSelection[ 0 ] == null )
                {
                    SelectionManager.UpdateSelection();
                }
                else if ( SelectionToolManager.settings.boxSpace == Space.Self )
                {
                    rotation = SelectionManager.topLevelSelection[ 0 ].transform.rotation;
                }
            }
            else if ( SelectionToolManager.settings.handleSpace == Space.Self )
            {
                int     count      = 0;
                Vector3 avgForward = Vector3.forward;
                Vector3 avgUp      = Vector3.up;
                if ( SelectionManager.topLevelSelection.Length > 0 )
                {
                    avgForward = Vector3.zero;
                    avgUp      = Vector3.zero;
                }

                foreach ( GameObject obj in SelectionManager.topLevelSelection )
                {
                    if ( obj == null )
                    {
                        continue;
                    }

                    ++count;
                    avgForward += obj.transform.rotation * Vector3.forward;
                    avgUp      += obj.transform.rotation * Vector3.up;
                }

                avgForward /= count;
                avgUp      /= count;
                rotation   =  Quaternion.LookRotation( avgForward, avgUp );
            }

            return rotation;
        }

        private static void MoveSelection( Quaternion    rotation,
                                           List<Vector3> points, SceneView sceneView )
        {
            if ( !SelectionToolManager.settings.move )
            {
                return;
            }

            if ( SelectionToolManager.settings.handleSpace == Space.World )
            {
                rotation = Quaternion.identity;
            }
            else if ( SelectionManager.topLevelSelection.Length == 1 )
            {
                rotation = SelectionManager.topLevelSelection[ 0 ].transform.rotation;
            }

            void SetSetectedPoint( Vector3 value )
            {
                points[ _selectedBoxPointIdx ] = value;
            }

            Vector3 prevPosition = points[ _selectedBoxPointIdx ];
            SetSetectedPoint( Handles.PositionHandle( points[ _selectedBoxPointIdx ], rotation ) );
            if ( prevPosition == points[ _selectedBoxPointIdx ] )
            {
                return;
            }

            SetSetectedPoint( SnapAndUpdateGridOrigin( points[ _selectedBoxPointIdx ],
                SnapManager.settings.snappingEnabled, true, true, true, Vector3.down ) );
            if ( _snappedPointIsSelected )
            {
                _snappedPoint = points[ _selectedBoxPointIdx ];
            }

            if ( prevPosition == points[ _selectedBoxPointIdx ] )
            {
                return;
            }

            if ( _snapToVertex )
            {
                if ( SnapToVertex( HandleUtility.GUIPointToWorldRay( Event.current.mousePosition ),
                        out RaycastHit closestVertexInfo, sceneView.in2DMode ) )
                {
                    SetSetectedPoint( closestVertexInfo.point );
                }
            }
            else
            {
                points[ _selectedBoxPointIdx ] = SnapAndUpdateGridOrigin( points[ _selectedBoxPointIdx ],
                    SnapManager.settings.snappingEnabled,          true, true,
                    !SelectionToolManager.settings.embedInSurface, Vector3.down );
            }

            if ( prevPosition == points[ _selectedBoxPointIdx ] )
            {
                return;
            }

            Vector3 delta = points[ _selectedBoxPointIdx ] - prevPosition;
            foreach ( GameObject obj in SelectionManager.topLevelSelection )
            {
                if ( obj == null )
                {
                    SelectionManager.UpdateSelection();
                    return;
                }

                Undo.RecordObject( obj.transform, "Move Selection" );
                obj.transform.position += delta;
            }

            _selectionBounds.center += delta;
            if ( SelectionToolManager.settings.embedInSurface )
            {
                EmbedSelectionInSurface();
            }

            PWBCore.UpdateTempCollidersTransforms( SelectionManager.topLevelSelection );
        }

        private static void RotateSelection( Quaternion rotation, List<Vector3> points )
        {
            if ( !SelectionToolManager.settings.rotate )
            {
                return;
            }

            if ( SelectionToolManager.settings.handleSpace    == Space.Self
                 && SelectionManager.topLevelSelection.Length == 1 )
            {
                rotation = SelectionManager.topLevelSelection[ 0 ].transform.rotation;
            }

            Quaternion prevRotation = rotation;
            Quaternion newRotation  = Handles.RotationHandle( prevRotation, points[ _selectedBoxPointIdx ] );
            if ( prevRotation == newRotation )
            {
                return;
            }

            _selectionRotation = newRotation;
            float   angle = Quaternion.Angle( prevRotation, newRotation );
            Vector3 axis  = Vector3.Cross( prevRotation * Vector3.forward, newRotation * Vector3.forward );
            if ( axis == Vector3.zero )
            {
                axis = Vector3.Cross( prevRotation * Vector3.up, newRotation * Vector3.up );
            }

            axis.Normalize();
            foreach ( GameObject obj in SelectionManager.topLevelSelection )
            {
                if ( obj == null )
                {
                    SelectionManager.UpdateSelection();
                    return;
                }

                Undo.RecordObject( obj.transform, "Rotate Selection" );
                obj.transform.RotateAround( points[ _selectedBoxPointIdx ], axis, angle );
            }

            Vector3 localCenter = _selectionBounds.center - points[ _selectedBoxPointIdx ];
            _selectionBounds.center = Quaternion.AngleAxis( angle, axis ) * localCenter
                                      + points[ _selectedBoxPointIdx ];
            if ( SelectionToolManager.settings.embedInSurface )
            {
                EmbedSelectionInSurface( _selectionRotation );
            }

            PWBCore.UpdateTempCollidersTransforms( SelectionManager.topLevelSelection );
        }

        private static void RotateSelection90Deg( Vector3 axis, List<Vector3> points )
        {
            Quaternion rotation = _selectionRotation;
            foreach ( GameObject obj in SelectionManager.topLevelSelection )
            {
                if ( obj == null )
                {
                    SelectionManager.UpdateSelection();
                    return;
                }

                Undo.RecordObject( obj.transform, "Rotate Selection" );
                obj.transform.RotateAround( points[ _selectedBoxPointIdx < 0 ? 10 : _selectedBoxPointIdx ],
                    rotation * axis, 90 );
            }

            _selectionRotation = rotation * Quaternion.AngleAxis( 90, axis );
            Vector3 localCenter = _selectionBounds.center - points[ _selectedBoxPointIdx ];
            _selectionBounds.center = Quaternion.AngleAxis( 90, axis ) * localCenter + points[ _selectedBoxPointIdx ];
            if ( SelectionToolManager.settings.embedInSurface )
            {
                EmbedSelectionInSurface();
            }

            PWBCore.UpdateTempCollidersTransforms( SelectionManager.topLevelSelection );
        }

        private static void ScaleSelection( Quaternion rotation, List<Vector3> points )
        {
            if ( !SelectionToolManager.settings.scale )
            {
                return;
            }

            Vector3 prevScale = _selectionScale;
            Vector3 newScale = Handles.ScaleHandle( prevScale, points[ _selectedBoxPointIdx ],
                rotation, HandleUtility.GetHandleSize( points[ _selectedBoxPointIdx ] ) * 1.4f );
            if ( prevScale == newScale )
            {
                return;
            }

            _selectionScale = newScale;
            Vector3 scaleFactor = new Vector3(
                prevScale.x == 0 ? newScale.x : newScale.x / prevScale.x,
                prevScale.y == 0 ? newScale.y : newScale.y / prevScale.y,
                prevScale.z == 0 ? newScale.z : newScale.z / prevScale.z );
            GameObject pivot = new GameObject();
            pivot.hideFlags          = HideFlags.HideAndDontSave;
            pivot.transform.position = points[ _selectedBoxPointIdx ];
            pivot.transform.rotation = rotation;
            foreach ( GameObject obj in SelectionManager.topLevelSelection )
            {
                if ( obj == null )
                {
                    SelectionManager.UpdateSelection();
                    break;
                }

                Undo.RecordObject( obj.transform, "Scale Selection" );
                pivot.transform.localScale = Vector3.one;
                Vector3 localPosition = pivot.transform.InverseTransformPoint( obj.transform.position );
                pivot.transform.localScale = scaleFactor;
                obj.transform.position     = pivot.transform.TransformPoint( localPosition );
                obj.transform.localScale   = Vector3.Scale( obj.transform.localScale, scaleFactor );
            }

            Object.DestroyImmediate( pivot );
            Vector3 pivotToCenter = _selectionBounds.center - points[ _selectedBoxPointIdx ];
            _selectionBounds.center = points[ _selectedBoxPointIdx ] + Vector3.Scale( pivotToCenter, scaleFactor );
            _selectionBounds.size   = Vector3.Scale( _selectionBounds.size, scaleFactor );
            if ( SelectionToolManager.settings.embedInSurface )
            {
                EmbedSelectionInSurface();
            }

            PWBCore.UpdateTempCollidersTransforms( SelectionManager.topLevelSelection );
        }

        private static void SelectionDuringSceneGUI( SceneView sceneView )
        {
            if ( SelectionToolManager.settings.createTempColliders )
            {
                PWBCore.CreateTempCollidersWithinFrustum( sceneView.camera );
            }

            if ( Event.current.type       == EventType.KeyDown
                 && Event.current.keyCode == KeyCode.Escape )
            {
                _snappedPointIsSelected = false;
                _selectionMoving        = false;
                if ( _selectedBoxPointIdx    >= 0
                     && _selectedBoxPointIdx != 10 )
                {
                    _selectedBoxPointIdx = 10;
                }
                else
                {
                    ResetUnityCurrentTool();
                    ToolManager.DeselectTool();
                    return;
                }
            }

            if ( Tools.current    != Tool.View
                 && Tools.current != Tool.None )
            {
                Tools.current = Tool.None;
            }

            if ( SelectionManager.topLevelSelection.Length == 0 )
            {
                return;
            }

            List<Vector3> points = SelectionPoints( sceneView.camera );

            if ( _setSelectionOriginPosition
                 && SnapManager.settings.snappingEnabled
                 && !SnapManager.settings.lockedGrid )
            {
                _setSelectionOriginPosition = false;
                SnapManager.settings.SetOriginHeight( points[ _selectedBoxPointIdx ], SnapManager.settings.gridAxis );
            }

            SelectionInput( points, sceneView.in2DMode );
            if ( _selectionMoving )
            {
                Handles.CircleHandleCap( 0, _selectionMoveFrom.position, sceneView.camera.transform.rotation,
                    HandleUtility.GetHandleSize( _selectionMoveFrom.position ) * 0.06f, EventType.Repaint );
                if ( _selectedBoxPointIdx >= 0 )
                {
                    Handles.DrawLine( _selectionMoveFrom.position, points[ _selectedBoxPointIdx ] );
                }
            }

            bool mouseDown    = Event.current.button == 0 && Event.current.type == EventType.MouseDown;
            bool clickOnPoint = false;

            bool SelectPoint( Vector3 point, int i )
            {
                if ( _editingSelectionHandlePosition )
                {
                    return false;
                }

                if ( clickOnPoint )
                {
                    return false;
                }

                int   controlId     = GUIUtility.GetControlID( FocusType.Passive );
                float distFromMouse = HandleUtility.DistanceToRectangle( point, Quaternion.identity, 0f );
                HandleUtility.AddControl( controlId, distFromMouse );
                if ( HandleUtility.nearestControl != controlId )
                {
                    return false;
                }

                DrawDotHandleCap( point, 1f, 1.2f );
                if ( !mouseDown )
                {
                    return false;
                }

                _selectedBoxPointIdx = i;
                clickOnPoint         = true;
                Event.current.Use();
                return true;
            }

            for ( int i = 0; i < points.Count; ++i )
            {
                if ( SelectPoint( points[ i ], i ) )
                {
                    _snappedPointIsSelected = false;
                }

                if ( clickOnPoint )
                {
                    break;
                }
            }

            if ( _snappedPointIsVisible || _snappedPointIsSelected )
            {
                points.Add( _snappedPoint );
                if ( SelectPoint( _snappedPoint, points.Count - 1 ) )
                {
                    _snappedPointIsSelected = true;
                }
            }

            if ( _selectionChanged )
            {
                _tempSelectionHandle = Vector3.zero;
                _selectionChanged    = false;
                ApplySelectionFilters();
            }

            if ( _editingSelectionHandlePosition )
            {
                _selectedBoxPointIdx = 11;
                Handles.CircleHandleCap( 0, points[ 11 ], sceneView.camera.transform.rotation,
                    HandleUtility.GetHandleSize( points[ 11 ] ) * 0.06f, EventType.Repaint );
            }

            if ( _selectedBoxPointIdx >= 0 )
            {
                Quaternion rotation = GetSelectionRotation();
                if ( _editingSelectionHandlePosition )
                {
                    Vector3 delta = points[ _selectedBoxPointIdx ];
                    points[ _selectedBoxPointIdx ] =  Handles.PositionHandle( points[ _selectedBoxPointIdx ], rotation );
                    delta                          =  points[ _selectedBoxPointIdx ] - delta;
                    _tempSelectionHandle           += delta;
                }
                else
                {
                    MoveSelection( rotation, points, sceneView );
                    RotateSelection( rotation, points );
                    ScaleSelection( rotation, points );
                }
            }
            else
            {
                _editingSelectionHandlePosition = false;
            }
        }

        private static void SelectionInput( List<Vector3> points, bool in2DMode )
        {
            if ( Tools.current == Tool.Move )
            {
                return;
            }

            KeyCode keyCode = Event.current.keyCode;
            if ( PWBSettings.shortcuts.selectionTogglePositionHandle.Check() )
            {
                SelectionToolManager.settings.move = !SelectionToolManager.settings.move;
                PWBToolbar.RepaintWindow();
            }
            else if ( PWBSettings.shortcuts.selectionToggleRotationHandle.Check() )
            {
                SelectionToolManager.settings.rotate = !SelectionToolManager.settings.rotate;
                PWBToolbar.RepaintWindow();
            }
            else if ( PWBSettings.shortcuts.selectionToggleScaleHandle.Check() )
            {
                SelectionToolManager.settings.scale = !SelectionToolManager.settings.scale;
                PWBToolbar.RepaintWindow();
            }
            else if ( Event.current.type == EventType.KeyDown
                      && ( PWBSettings.shortcuts.selectionEditCustomHandle.Check()
                           || ( _editingSelectionHandlePosition
                                && ( Event.current.keyCode    == KeyCode.Escape
                                     || Event.current.keyCode == KeyCode.Return ) ) ) )
            {
                _editingSelectionHandlePosition = !_editingSelectionHandlePosition;
            }
            else if ( _snappedToVertex && _selectedBoxPointIdx < 0 )
            {
                _snappedPointIsVisible = false;
                Ray mouseRay = HandleUtility.GUIPointToWorldRay( Event.current.mousePosition );
                if ( SnapToVertex( mouseRay, out RaycastHit snappedHit, in2DMode, SelectionManager.topLevelSelection ) )
                {
                    _snappedPoint          = snappedHit.point;
                    _snappedPointIsVisible = true;
                }
            }
            else if ( Event.current.type       == EventType.KeyDown
                      && Event.current.keyCode == KeyCode.Return
                      && _selectedBoxPointIdx  >= 0 )
            {
                _editingSelectionHandlePosition = false;
                if ( _selectionMoving )
                {
                    Vector3 delta = points[ _selectedBoxPointIdx ] - _selectionMoveFrom.position;
                    foreach ( GameObject obj in _selectionMoveFrom.selection )
                    {
                        if ( obj == null )
                        {
                            continue;
                        }

                        Undo.RecordObject( obj.transform, "Move Selection" );
                        obj.transform.position += delta;
                    }

                    _selectionMoving = false;
                    SelectionManager.UpdateSelection();
                    _selectedBoxPointIdx = -1;
                }
                else
                {
                    _selectionMoveFrom = ( points[ _selectedBoxPointIdx ], SelectionManager.topLevelSelection );
                    _selectionMoving   = true;
                }
            }
            else if ( PWBSettings.shortcuts.selectionRotate90XCCW.Check() )
            {
                RotateSelection90Deg( Vector3.left, points );
            }
            else if ( PWBSettings.shortcuts.selectionRotate90XCW.Check() )
            {
                RotateSelection90Deg( Vector3.right, points );
            }
            else if ( PWBSettings.shortcuts.selectionRotate90YCCW.Check() )
            {
                RotateSelection90Deg( Vector3.down, points );
            }
            else if ( PWBSettings.shortcuts.selectionRotate90YCW.Check() )
            {
                RotateSelection90Deg( Vector3.up, points );
            }
            else if ( PWBSettings.shortcuts.selectionRotate90ZCCW.Check() )
            {
                RotateSelection90Deg( Vector3.back, points );
            }
            else if ( PWBSettings.shortcuts.selectionRotate90ZCW.Check() )
            {
                RotateSelection90Deg( Vector3.forward, points );
            }
            else if ( Event.current.keyCode == KeyCode.X
                      && Event.current.type == EventType.KeyDown
                      && Event.current.control
                      && Event.current.shift )
            {
                SelectionToolManager.settings.handleSpace = SelectionToolManager.settings.handleSpace == Space.Self
                    ? Space.World
                    : Space.Self;
                if ( SelectionToolManager.settings.handleSpace == Space.World )
                {
                    ResetSelectionRotation();
                }

                SceneView.RepaintAll();
                ToolProperties.RepainWindow();
                Event.current.Use();
            }
        }

        private static List<Vector3> SelectionPoints( Camera camera )
        {
            Quaternion rotation        = GetSelectionRotation();
            Bounds     bounds          = _selectionBounds;
            Vector3    halfSizeRotated = rotation * bounds.size / 2;
            Vector3    min             = bounds.center - halfSizeRotated;
            Vector3    max             = bounds.center + halfSizeRotated;
            List<Vector3> points = new List<Vector3>
            {
                min,
                min + rotation * new Vector3( bounds.size.x, 0f,            0f ),
                min + rotation * new Vector3( bounds.size.x, 0f,            bounds.size.z ),
                min + rotation * new Vector3( 0f,            0f,            bounds.size.z ),
                min + rotation * new Vector3( 0f,            bounds.size.y, 0f ),
                min + rotation * new Vector3( bounds.size.x, bounds.size.y, 0f ),
                max,
                min + rotation                                                   * new Vector3( 0f, bounds.size.y, bounds.size.z ),
                min + rotation * new Vector3( bounds.size.x, 0f, bounds.size.z ) / 2,
                max - rotation * new Vector3( bounds.size.x, 0f, bounds.size.z ) / 2,
            };

            int[] visibleIdx = GetVisiblePoints( points.ToArray(), camera );

            points.Add( bounds.center );
            points.Add( bounds.center + _selectionRotation * _tempSelectionHandle );

            void DrawLine( Vector3[] line, float alpha )
            {
                Handles.color = new Color( 0f, 0f, 0f, 0.5f );
                Handles.DrawAAPolyLine( 10, line );
                Handles.color = new Color( 1f, 1f, 1f, 0.3f * alpha );
                Handles.DrawAAPolyLine( 4, line );
            }

            List<Vector3[]> visibleLines = new List<Vector3[]>();
            float           ocludedAlpha = 0.5f;
            for ( int i = 0; i < 8; ++i )
            {
                bool visibleLine = visibleIdx.Contains( i ) && visibleIdx.Contains( i + 4 );
                if ( i < 4 )
                {
                    Vector3[] vLine =
                    {
                        points[ i ],
                        points[ i ] + rotation * new Vector3( 0f, bounds.size.y, 0f ),
                    };
                    if ( visibleLine )
                    {
                        visibleLines.Add( vLine );
                    }
                    else
                    {
                        DrawLine( vLine, ocludedAlpha );
                    }

                    points.Add( vLine[ 0 ] + ( vLine[ 1 ] - vLine[ 0 ] ) / 2 );
                }

                int nextI = ( i + 1 ) % 4 + 4 * ( i / 4 );
                visibleLine = visibleIdx.Contains( i ) && visibleIdx.Contains( nextI );
                Vector3[] hLine = { points[ i ], points[ nextI ] };
                if ( visibleLine )
                {
                    visibleLines.Add( hLine );
                }
                else
                {
                    DrawLine( hLine, ocludedAlpha );
                }

                Vector3 midpoint = hLine[ 0 ] + ( hLine[ 1 ] - hLine[ 0 ] ) / 2;
                points.Add( midpoint );
                if ( i < 4 )
                {
                    points.Add( midpoint + rotation * new Vector3( 0f, bounds.size.y / 2, 0f ) );
                }
            }

            foreach ( Vector3[] line in visibleLines )
            {
                DrawLine( line, 1f );
            }

            for ( int i = 0; i < 8; ++i )
            {
                float alpha = visibleIdx.Contains( i ) ? 1f : 0.3f;
                DrawDotHandleCap( points[ i ], alpha );
            }

            DrawDotHandleCap( points[ 11 ] );
            return points;
        }

        #endregion

        #region VISIBLE POINTS

        private static int[] GetVisiblePoints( Vector3[] points, Camera camera )
        {
            HashSet<int> resultSet = new HashSet<int>( GrahamScan( points ) );
            if ( resultSet.Count == 6 )
            {
                List<int> ocluded = new List<int>();
                for ( int i = 0; i < points.Length; ++i )
                {
                    if ( resultSet.Contains( i ) )
                    {
                        continue;
                    }

                    ocluded.Add( i );
                }

                if ( ocluded[ 0 ] / 4 == ocluded[ 1 ] / 4
                     || ocluded[ 1 ]  == ocluded[ 0 ] + 4 )
                {
                    return resultSet.ToArray();
                }

                int nearestIdx = camera.transform.InverseTransformPoint( points[ ocluded[ 0 ] ] ).z
                                 < camera.transform.InverseTransformPoint( points[ ocluded[ 1 ] ] ).z
                    ? ocluded[ 0 ]
                    : ocluded[ 1 ];
                resultSet.Add( nearestIdx );
            }

            return resultSet.ToArray();
        }

        private static int[] GrahamScan( Vector3[] points )
        {
            List<BoxPoint> screenPoints = new List<BoxPoint>();
            for ( int i = 0; i < points.Length; ++i )
            {
                screenPoints.Add( new BoxPoint( i, HandleUtility.WorldToGUIPoint( points[ i ] ) ) );
            }

            BoxPoint p0 = screenPoints[ 0 ];
            foreach ( BoxPoint value in screenPoints )
            {
                if ( p0.point.y > value.point.y )
                {
                    p0 = value;
                }
            }

            List<BoxPoint> order = new List<BoxPoint>();
            foreach ( BoxPoint point in screenPoints )
            {
                if ( p0 != point )
                {
                    order.Add( point );
                }
            }

            order = MergeSort( p0, order );
            List<BoxPoint> result = new List<BoxPoint>();
            result.Add( p0 );
            result.Add( order[ 0 ] );
            result.Add( order[ 1 ] );
            order.RemoveAt( 0 );
            order.RemoveAt( 0 );
            foreach ( BoxPoint value in order )
            {
                KeepLeft( result, value );
            }

            int[] resultIdx = new int[ result.Count ];
            for ( int i = 0; i < result.Count; ++i )
            {
                resultIdx[ i ] = result[ i ];
            }

            return resultIdx;
        }

        private class BoxPoint
        {

            #region Public Fields

            public int     idx   = -1;
            public Vector2 point = Vector2.zero;

            #endregion

            #region Public Constructors

            public BoxPoint( int idx, Vector2 point )
            {
                ( this.idx, this.point ) = ( idx, point );
            }

            #endregion

            #region Public Methods

            public          bool Equals( BoxPoint other ) => GetHashCode() == other.GetHashCode();
            public override bool Equals( object   obj )   => Equals( obj as BoxPoint );

            public override int GetHashCode()
            {
                int hashCode = 386348313;
                hashCode = hashCode * -1521134295 + idx.GetHashCode();
                hashCode = hashCode * -1521134295 + point.GetHashCode();
                return hashCode;
            }

            public static                   bool operator ==( BoxPoint l, BoxPoint r ) => l.Equals( r );
            public static implicit operator Vector2( BoxPoint          value )         => value.point;
            public static implicit operator int( BoxPoint              value )         => value.idx;
            public static                   bool operator !=( BoxPoint l, BoxPoint r ) => !l.Equals( r );

            #endregion

        }

        private static List<BoxPoint> MergeSort( BoxPoint       p0,
                                                 List<BoxPoint> pointList )
        {
            if ( pointList.Count == 1 )
            {
                return pointList;
            }

            List<BoxPoint> sortedList = new List<BoxPoint>();
            int            middle     = pointList.Count / 2;
            List<BoxPoint> leftArray  = pointList.GetRange( 0,      middle );
            List<BoxPoint> rightArray = pointList.GetRange( middle, pointList.Count - middle );
            leftArray  = MergeSort( p0, leftArray );
            rightArray = MergeSort( p0, rightArray );
            int leftptr  = 0;
            int rightptr = 0;
            for ( int i = 0; i < leftArray.Count + rightArray.Count; i++ )
            {
                if ( leftptr == leftArray.Count )
                {
                    sortedList.Add( rightArray[ rightptr ] );
                    rightptr++;
                }
                else if ( rightptr == rightArray.Count )
                {
                    sortedList.Add( leftArray[ leftptr ] );
                    leftptr++;
                }
                else if ( GetAngle( p0, leftArray[ leftptr ] ) < GetAngle( p0, rightArray[ rightptr ] ) )
                {
                    sortedList.Add( leftArray[ leftptr ] );
                    leftptr++;
                }
                else
                {
                    sortedList.Add( rightArray[ rightptr ] );
                    rightptr++;
                }
            }

            return sortedList;
        }

        private static double GetAngle( Vector2 p1, Vector2 p2 )
        {
            float xDiff = p2.x - p1.x;
            float yDiff = p2.y - p1.y;
            return Mathf.Atan2( yDiff, xDiff ) * 180f / Mathf.PI;
        }

        private static void KeepLeft( List<BoxPoint> hull, BoxPoint point )
        {
            int turn( Vector2 p, Vector2 q, Vector2 r )
            {
                return ( ( q.x - p.x ) * ( r.y - p.y ) - ( r.x - p.x ) * ( q.y - p.y ) ).CompareTo( 0 );
            }

            while ( hull.Count                                                       > 1
                    && turn( hull[ hull.Count - 2 ], hull[ hull.Count - 1 ], point ) != 1 )
            {
                hull.RemoveAt( hull.Count - 1 );
            }

            if ( hull.Count                == 0
                 || hull[ hull.Count - 1 ] != point )
            {
                hull.Add( point );
            }
        }

        #endregion

    }

    #endregion

}