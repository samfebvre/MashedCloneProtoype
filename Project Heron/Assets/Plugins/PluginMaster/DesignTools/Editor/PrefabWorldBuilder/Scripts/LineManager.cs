using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace PluginMaster
{

    #region DATA & SETTINGS

    [Serializable]
    public class LineSettings : PaintOnSurfaceToolSettings, IPaintToolSettings
    {

        #region Serialized

        [SerializeField] private Vector3        _projectionDirection         = Vector3.down;
        [SerializeField] private bool           _objectsOrientedAlongTheLine = true;
        [SerializeField] private AxesUtils.Axis _axisOrientedAlongTheLine    = AxesUtils.Axis.X;
        [SerializeField] private SpacingType    _spacingType                 = SpacingType.BOUNDS;
        [SerializeField] private float          _gapSize;
        [SerializeField] private float          _spacing = 10f;

        [SerializeField] private PaintToolSettings _paintTool = new PaintToolSettings();

        #endregion

        #region Public Enums

        public enum SpacingType
        {
            BOUNDS,
            CONSTANT,
        }

        #endregion

        #region Public Properties

        public bool autoCreateParent
        {
            get => _paintTool.autoCreateParent;
            set => _paintTool.autoCreateParent = value;
        }

        public AxesUtils.Axis axisOrientedAlongTheLine
        {
            get => _axisOrientedAlongTheLine;
            set
            {
                if ( _axisOrientedAlongTheLine == value )
                {
                    return;
                }

                _axisOrientedAlongTheLine = value;
                OnDataChanged();
            }
        }

        public BrushSettings brushSettings => _paintTool.brushSettings;

        public bool createSubparentPerBrush
        {
            get => _paintTool.createSubparentPerBrush;
            set => _paintTool.createSubparentPerBrush = value;
        }

        public bool createSubparentPerPalette
        {
            get => _paintTool.createSubparentPerPalette;
            set => _paintTool.createSubparentPerPalette = value;
        }

        public bool createSubparentPerPrefab
        {
            get => _paintTool.createSubparentPerPrefab;
            set => _paintTool.createSubparentPerPrefab = value;
        }

        public bool createSubparentPerTool
        {
            get => _paintTool.createSubparentPerTool;
            set => _paintTool.createSubparentPerTool = value;
        }

        public float gapSize
        {
            get => _gapSize;
            set
            {
                if ( _gapSize == value )
                {
                    return;
                }

                _gapSize = value;
                OnDataChanged();
            }
        }

        public int layer
        {
            get => _paintTool.layer;
            set => _paintTool.layer = value;
        }

        public bool objectsOrientedAlongTheLine
        {
            get => _objectsOrientedAlongTheLine;
            set
            {
                if ( _objectsOrientedAlongTheLine == value )
                {
                    return;
                }

                _objectsOrientedAlongTheLine = value;
                OnDataChanged();
            }
        }

        public bool overwriteBrushProperties
        {
            get => _paintTool.overwriteBrushProperties;
            set => _paintTool.overwriteBrushProperties = value;
        }

        public bool overwritePrefabLayer
        {
            get => _paintTool.overwritePrefabLayer;
            set => _paintTool.overwritePrefabLayer = value;
        }

        public Transform parent
        {
            get => _paintTool.parent;
            set => _paintTool.parent = value;
        }

        public Vector3 projectionDirection
        {
            get => _projectionDirection;
            set
            {
                if ( _projectionDirection == value )
                {
                    return;
                }

                _projectionDirection = value;
                OnDataChanged();
            }
        }

        public bool setSurfaceAsParent
        {
            get => _paintTool.setSurfaceAsParent;
            set => _paintTool.setSurfaceAsParent = value;
        }

        public float spacing
        {
            get => _spacing;
            set
            {
                value = Mathf.Max( value, 0.01f );
                if ( _spacing == value )
                {
                    return;
                }

                _spacing = value;
                OnDataChanged();
            }
        }

        public SpacingType spacingType
        {
            get => _spacingType;
            set
            {
                if ( _spacingType == value )
                {
                    return;
                }

                _spacingType = value;
                OnDataChanged();
            }
        }

        #endregion

        #region Public Constructors

        public LineSettings()
        {
            _paintTool.OnDataChanged += DataChanged;
        }

        #endregion

        #region Public Methods

        public override void Clone( ICloneableToolSettings clone )
        {
            if ( clone == null
                 || !( clone is LineSettings ) )
            {
                clone = new LineSettings();
            }

            clone.Copy( this );
        }

        public override void Copy( IToolSettings other )
        {
            LineSettings otherLineSettings = other as LineSettings;
            if ( otherLineSettings == null )
            {
                return;
            }

            base.Copy( other );
            _projectionDirection         = otherLineSettings._projectionDirection;
            _objectsOrientedAlongTheLine = otherLineSettings._objectsOrientedAlongTheLine;
            _axisOrientedAlongTheLine    = otherLineSettings._axisOrientedAlongTheLine;
            _spacingType                 = otherLineSettings._spacingType;
            _spacing                     = otherLineSettings._spacing;
            _paintTool.Copy( otherLineSettings._paintTool );
            _gapSize = otherLineSettings._gapSize;
        }

        public override void DataChanged()
        {
            base.DataChanged();
            UpdateStroke();
            SceneView.RepaintAll();
        }

        public void UpdateProjectDirection( Vector3 value ) => _projectionDirection = value;

        #endregion

        #region Protected Methods

        protected virtual void UpdateStroke() => PWBIO.UpdateStroke();

        #endregion

    }

    [Serializable]
    public class LineSegment
    {

        #region Serialized

        public SegmentType type = SegmentType.CURVE;

        [SerializeField]
        private List<LinePoint> _linePoints = new List<LinePoint>();

        #endregion

        #region Public Enums

        public enum SegmentType
        {
            STRAIGHT,
            CURVE,
        }

        #endregion

        #region Public Properties

        public Vector3[] points => _linePoints.Select( p => p.position ).ToArray();
        public float[]   scales => _linePoints.Select( p => p.scale ).ToArray();

        #endregion

        #region Public Methods

        public void AddPoint( Vector3 position, float scale = 0.25f ) => _linePoints.Add( new LinePoint( position, scale ) );

        #endregion

    }

    [Serializable]
    public class LinePoint : ControlPoint
    {

        #region Serialized

        public LineSegment.SegmentType type  = LineSegment.SegmentType.CURVE;
        public float                   scale = 0.25f;

        #endregion

        #region Public Constructors

        public LinePoint()
        {
        }

        public LinePoint( Vector3                 position = new Vector3(), float scale = 0.25f,
                          LineSegment.SegmentType type     = LineSegment.SegmentType.CURVE )
            : base( position )
        {
            ( this.type, this.scale ) = ( type, scale );
        }

        public LinePoint( LinePoint other ) : base( (ControlPoint)other )
        {
            Copy( other );
        }

        #endregion

        #region Public Methods

        public override void Copy( ControlPoint other )
        {
            base.Copy( other );
            LinePoint otherLinePoint = other as LinePoint;
            if ( otherLinePoint == null )
            {
                return;
            }

            type  = otherLinePoint.type;
            scale = otherLinePoint.scale;
        }

        #endregion

    }

    [Serializable]
    public class LineData : PersistentData<LineToolName, LineSettings, LinePoint>
    {

        #region Statics and Constants

        private static LineData _instance;

        #endregion

        #region Serialized

        [SerializeField] private bool _closed;

        #endregion

        #region Public Properties

        public bool closed => _closed;

        public static LineData instance
        {
            get
            {
                if ( _instance == null )
                {
                    _instance = new LineData();
                }

                if ( _instance.points           == null
                     || _instance.points.Length == 0 )
                {
                    _instance.Initialize();
                    _instance._settings = LineManager.settings;
                }

                return _instance;
            }
        }

        public Vector3 lastPathPoint  => _pathPoints.Last();
        public Vector3 lastTangentPos { get; set; }

        public float lenght { get; private set; }

        public Vector3[] midpoints           => _midpoints.ToArray();
        public Vector3[] onSurfacePathPoints => _onSurfacePathPoints.ToArray();
        public Vector3[] pathPoints          => _pathPoints.ToArray();

        public bool showHandles { get; set; }

        public override ToolManager.ToolState state
        {
            get => base.state;
            set
            {
                if ( state == value )
                {
                    return;
                }

                base.state = value;
                UpdatePath();
            }
        }

        #endregion

        #region Public Constructors

        public LineData()
        {
        }

        public LineData( GameObject[] objects, long initialBrushId, LineData lineData )
            : base( objects, initialBrushId, lineData )
        {
        }

        //for compatibility with version 1.9
        public LineData( long id,             LinePoint[] controlPoints, ObjectPose[] objectPoses,
                         long initialBrushId, bool        closed,        LineSettings settings )
        {
            _id             = id;
            _controlPoints  = new List<LinePoint>( controlPoints );
            _initialBrushId = initialBrushId;
            _closed         = closed;
            _settings       = settings;
            base.UpdatePoints();
            UpdatePath( true );
            if ( objectPoses           == null
                 || objectPoses.Length == 0 )
            {
                return;
            }

            _objectPoses = new List<ObjectPose>( objectPoses );
        }

        #endregion

        #region Public Methods

        public void AddPoint( Vector3 point, bool registerUndo = true )
        {
            LinePoint linePoint = new LinePoint( point );
            base.AddPoint( linePoint, registerUndo );
            UpdatePath();
        }

        public LineData Clone()
        {
            LineData clone = new LineData();
            base.Clone( clone );
            clone.CopyLineData( this );
            return clone;
        }

        public override void Copy( PersistentData<LineToolName, LineSettings, LinePoint> other )
        {
            base.Copy( other );
            LineData otherLineData = other as LineData;
            if ( otherLineData == null )
            {
                return;
            }

            CopyLineData( otherLineData );
        }

        public LineSegment[] GetSegments()
        {
            List<LineSegment> segments = new List<LineSegment>();
            if ( _controlPoints          == null
                 || _controlPoints.Count == 0 )
            {
                return segments.ToArray();
            }

            LineSegment.SegmentType type = _controlPoints[ 0 ].type;
            for ( int i = 0; i < pointsCount; ++i )
            {
                LineSegment segment = new LineSegment();
                segments.Add( segment );
                segment.type = type;
                segment.AddPoint( _controlPoints[ i ].position );

                do
                {
                    ++i;
                    if ( i >= pointsCount )
                    {
                        break;
                    }

                    type = _controlPoints[ i ].type;
                    if ( type == segment.type )
                    {
                        segment.AddPoint( _controlPoints[ i ].position );
                    }
                }
                while ( type == segment.type );

                if ( i >= pointsCount )
                {
                    break;
                }

                i -= 2;
            }

            if ( _closed )
            {
                if ( _controlPoints[ 0 ].type == _controlPoints.Last().type )
                {
                    segments.Last().AddPoint( _controlPoints[ 0 ].position );
                }
                else
                {
                    LineSegment segment = new LineSegment();
                    segment.type = _controlPoints[ 0 ].type;
                    segment.AddPoint( _controlPoints.Last().position );
                    segment.AddPoint( _controlPoints[ 0 ].position );
                    segments.Add( segment );
                }
            }

            return segments.ToArray();
        }

        public static Vector3 NearestPathPoint( Vector3   startPoint, float   minPathLenght,
                                                Vector3[] pathPoints, out int nearestPointIdx )
        {
            nearestPointIdx = pathPoints.Length - 1;
            Vector3 result = pathPoints.Last();
            for ( int i = 1; i < pathPoints.Length; ++i )
            {
                Vector3 start = pathPoints[ i - 1 ];
                Vector3 end   = pathPoints[ i ];
                if ( SphereSegmentIntersection( start, end, startPoint, minPathLenght, out Vector3 intersection ) )
                {
                    result          = intersection;
                    nearestPointIdx = i - 1;
                    return result;
                }
            }

            return result;
        }

        public override void SetPoint( int idx, Vector3 value, bool registerUndo, bool selectAll, bool moveSelection = true )
        {
            base.SetPoint( idx, value, registerUndo, selectAll, moveSelection );
            UpdatePath();
        }

        public void SetRotatedPoint( int idx, Vector3 value, bool registerUndo )
            => base.SetPoint( idx, value, registerUndo, selectAll: false, moveSelection: false );

        public static bool SphereSegmentIntersection( Vector3 segmentStart, Vector3 segmentEnd,
                                                      Vector3 sphereCenter, float   sphereRadius, out Vector3 intersection )
        {
            float   r            = sphereRadius;
            Vector3 d            = segmentEnd   - segmentStart;
            Vector3 f            = segmentStart - sphereCenter;
            float   a            = Vector3.Dot( d, d );
            float   b            = 2 * Vector3.Dot( f, d );
            float   c            = Vector3.Dot( f,     f ) - r * r;
            float   discriminant = b                           * b - 4 * a * c;
            float   t            = -1;
            intersection = segmentStart;
            if ( discriminant < 0 )
            {
                return false;
            }

            discriminant = Mathf.Sqrt( discriminant );
            float t1 = ( -b - discriminant ) / ( 2 * a );
            float t2 = ( -b + discriminant ) / ( 2 * a );
            if ( t1    >= 0
                 && t1 <= 1
                 && t1 > t2 )
            {
                t = t1;
            }
            else if ( t2    >= 0
                      && t2 <= 1
                      && t2 > t1 )
            {
                t = t2;
            }

            if ( t == -1 )
            {
                return false;
            }

            intersection += d * t;
            return true;
        }

        public void ToggleClosed()
        {
            ToolProperties.RegisterUndo( COMMAND_NAME );
            _closed = !_closed;
        }

        public void ToggleSegmentType()
        {
            ToolProperties.RegisterUndo( COMMAND_NAME );
            for ( int i = 0; i < _selection.Count; ++i )
            {
                int idx = _selection[ i ];
                _controlPoints[ idx ].type = _controlPoints[ idx ].type == LineSegment.SegmentType.CURVE
                    ? LineSegment.SegmentType.STRAIGHT
                    : LineSegment.SegmentType.CURVE;
            }
        }

        public void UpdatePath( bool forceUpdate = false )
        {
            if ( !forceUpdate
                 && !ToolManager.editMode
                 && state != ToolManager.ToolState.EDIT )
            {
                return;
            }

            lenght = 0;
            _pathPoints.Clear();
            _midpoints.Clear();
            _onSurfacePathPoints.Clear();
            LineSegment[] segments = GetSegments();

            void AddSegmentPoints( List<Vector3> pointList, Vector3[] newPoints )
            {
                if ( pointList.Count     > 0
                     && pointList.Last() == newPoints[ 0 ]
                     && newPoints.Length > 1 )
                {
                    for ( int i = 1; i < newPoints.Length; ++i )
                    {
                        pointList.Add( newPoints[ i ] );
                    }
                }
                else
                {
                    pointList.AddRange( newPoints );
                }
            }

            foreach ( LineSegment segment in segments )
            {
                Vector3[] segmentPoints = { };
                if ( segment.type == LineSegment.SegmentType.STRAIGHT )
                {
                    segmentPoints = segment.points.ToArray();
                }
                else
                {
                    segmentPoints = BezierPath.GetBezierPoints( segment.points, segment.scales ).ToArray();
                }

                AddSegmentPoints( _pathPoints, segmentPoints );
                if ( segmentPoints.Length == 0 )
                {
                    continue;
                }

                AddSegmentPoints( _midpoints, GetLineMidpoints( segmentPoints ) );
            }

            for ( int i = 0; i < _pathPoints.Count; ++i )
            {
                float distance = 10000f;
                if ( ToolManager.tool == ToolManager.PaintTool.LINE
                     && !deserializing )
                {
                    Ray     ray            = new Ray( _pathPoints[ i ] - settings.projectionDirection * distance, settings.projectionDirection );
                    Vector3 onSurfacePoint = _pathPoints[ i ];
                    if ( PWBIO.MouseRaycast( ray, out RaycastHit hit, out GameObject collider, distance * 2, -1,
                            paintOnPalettePrefabs: false, castOnMeshesWithoutCollider: true,
                            tags: null, terrainLayers: null, exceptions: objects ) )
                    {
                        onSurfacePoint = hit.point;
                    }

                    _onSurfacePathPoints.Add( onSurfacePoint );
                }

                if ( i == 0 )
                {
                    continue;
                }

                lenght += ( _pathPoints[ i ] - _pathPoints[ i - 1 ] ).magnitude;
            }
        }

        #endregion

        #region Protected Methods

        protected override void Initialize()
        {
            base.Initialize();
            for ( int i = 0; i < 2; ++i )
            {
                _controlPoints.Add( new LinePoint( Vector3.zero ) );
            }

            deserializing = true;
            UpdatePoints();
            deserializing = false;
        }

        protected override void UpdatePoints()
        {
            base.UpdatePoints();
            UpdatePath();
        }

        #endregion

        #region Private Fields

        private List<Vector3> _midpoints           = new List<Vector3>();
        private List<Vector3> _onSurfacePathPoints = new List<Vector3>();
        private List<Vector3> _pathPoints          = new List<Vector3>();

        #endregion

        #region Private Methods

        private void CopyLineData( LineData other )
        {
            _closed     = other._closed;
            lenght      = other.lenght;
            _midpoints  = other._midpoints.ToList();
            _pathPoints = other._pathPoints.ToList();
        }

        private float GetLineLength( Vector3[] points, out float[] lengthFromFirstPoint )
        {
            float lineLength = 0f;
            lengthFromFirstPoint = new float[ points.Length ];
            float[] segmentLength = new float[ points.Length ];
            lengthFromFirstPoint[ 0 ] = 0f;
            for ( int i = 1; i < points.Length; ++i )
            {
                segmentLength[ i - 1 ]    =  ( points[ i ] - points[ i - 1 ] ).magnitude;
                lineLength                += segmentLength[ i - 1 ];
                lengthFromFirstPoint[ i ] =  lineLength;
            }

            return lineLength;
        }

        private Vector3[] GetLineMidpoints( Vector3[] points )
        {
            if ( points.Length == 0 )
            {
                return new Vector3[ 0 ];
            }

            List<Vector3>       midpoints   = new List<Vector3>();
            List<List<Vector3>> subSegments = new List<List<Vector3>>();
            Vector3[]           pathPoints  = _pointPositions;

            bool IsAPathPoint( Vector3 point )
            {
                return pathPoints.Contains( point );
            }

            subSegments.Add( new List<Vector3>() );
            subSegments.Last().Add( points[ 0 ] );
            for ( int i = 1; i < points.Length - 1; ++i )
            {
                Vector3 point = points[ i ];
                subSegments.Last().Add( point );
                if ( IsAPathPoint( point ) )
                {
                    subSegments.Add( new List<Vector3>() );
                    subSegments.Last().Add( point );
                }
            }

            subSegments.Last().Add( points.Last() );

            Vector3 GetLineMidpoint( Vector3[] subSegmentPoints )
            {
                Vector3 midpoint             = subSegmentPoints[ 0 ];
                float[] lengthFromFirstPoint = null;
                float   halfLineLength       = GetLineLength( subSegmentPoints, out lengthFromFirstPoint ) / 2f;
                for ( int i = 1; i < subSegmentPoints.Length; ++i )
                {
                    if ( lengthFromFirstPoint[ i ] < halfLineLength )
                    {
                        continue;
                    }

                    Vector3 dir         = ( subSegmentPoints[ i ] - subSegmentPoints[ i - 1 ] ).normalized;
                    float   localLength = halfLineLength - lengthFromFirstPoint[ i - 1 ];
                    midpoint = subSegmentPoints[ i - 1 ] + dir * localLength;
                    break;
                }

                return midpoint;
            }

            foreach ( List<Vector3> subSegment in subSegments )
            {
                midpoints.Add( GetLineMidpoint( subSegment.ToArray() ) );
            }

            return midpoints.ToArray();
        }

        #endregion

    }

    public class LineToolName : IToolName
    {

        #region Public Properties

        public string value => "Line";

        #endregion

    }

    [Serializable]
    public class LineSceneData : SceneData<LineToolName, LineSettings, LinePoint, LineData>
    {

        #region Public Constructors

        public LineSceneData()
        {
        }

        public LineSceneData( string sceneGUID ) : base( sceneGUID )
        {
        }

        #endregion

    }

    [Serializable]
    public class LineManager : PersistentToolManagerBase<LineToolName, LineSettings, LinePoint, LineData, LineSceneData>
    {

        #region Public Enums

        public enum EditModeType
        {
            NODES,
            LINE_POSE,
        }

        #endregion

        #region Public Properties

        public static EditModeType editModeType { get; set; }

        #endregion

        #region Public Methods

        public static void ToggleEditModeType()
        {
            editModeType = editModeType == EditModeType.NODES ? EditModeType.LINE_POSE : EditModeType.NODES;
            ToolProperties.RepainWindow();
        }

        #endregion

    }

    #endregion

    #region PWBIO

    public static partial class PWBIO
    {

        #region Statics and Constants

        private static LineData _lineData = LineData.instance;
        private static bool     _selectingLinePoints;
        private static Rect     _selectionRect;

        private static List<GameObject> _disabledObjects
            = new List<GameObject>();

        private static bool     _editingPersistentLine;
        private static LineData _initialPersistentLineData;
        private static LineData _selectedPersistentLineData;
        private static string   _createProfileName = ToolProfile.DEFAULT;

        private static Quaternion _lineRotation = Quaternion.identity;

        #endregion

        #region Public Properties

        public static bool selectingLinePoints
        {
            get => _selectingLinePoints;
            set
            {
                if ( value == _selectingLinePoints )
                {
                    return;
                }

                _selectingLinePoints = value;
            }
        }

        #endregion

        #region Public Methods

        public static void ResetLineRotation() => _lineRotation = Quaternion.identity;

        public static void ResetLineState( bool askIfWantToSave = true )
        {
            if ( _lineData.state == ToolManager.ToolState.NONE )
            {
                return;
            }

            if ( askIfWantToSave )
            {
                void Save()
                {
                    if ( SceneView.lastActiveSceneView != null )
                    {
                        LineStrokePreview( SceneView.lastActiveSceneView, _lineData, false, true );
                    }

                    CreateLine();
                }

                AskIfWantToSave( _lineData.state, Save );
            }

            _snappedToVertex    = false;
            selectingLinePoints = false;
            _lineData.Reset();
        }

        #endregion

        #region Private Methods

        private static void ApplySelectedPersistentLine( bool deselectPoint )
        {
            if ( !ApplySelectedPersistentObject( deselectPoint, ref _editingPersistentLine, ref _initialPersistentLineData,
                    ref _selectedPersistentLineData,            LineManager.instance ) )
            {
                return;
            }

            if ( _initialPersistentLineData == null )
            {
                return;
            }

            LineData selected = LineManager.instance.GetItem( _initialPersistentLineData.id );
            _initialPersistentLineData = selected.Clone();
        }

        private static void ClearLineStroke()
        {
            _paintStroke.Clear();
            BrushstrokeManager.ClearBrushstroke();
            if ( ToolManager.editMode
                 && _selectedPersistentLineData != null )
            {
                _selectedPersistentLineData.UpdatePath( true );
                PreviewPersistentLine( _selectedPersistentLineData );
                SceneView.RepaintAll();
                repaint = true;
            }
        }

        private static void CreateLine()
        {
            string                               nextLineId = LineData.nextHexId;
            Dictionary<string, List<GameObject>> objDic     = Paint( LineManager.settings, PAINT_CMD, true, false, nextLineId );
            if ( objDic.Count != 1 )
            {
                return;
            }

            string       scenePath      = SceneManager.GetActiveScene().path;
            string       sceneGUID      = AssetDatabase.AssetPathToGUID( scenePath );
            long         initialBrushId = PaletteManager.selectedBrush != null ? PaletteManager.selectedBrush.id : -1;
            GameObject[] objs           = objDic[ nextLineId ].ToArray();
            LineData     persistentData = new LineData( objs, initialBrushId, _lineData );
            LineManager.instance.AddPersistentItem( sceneGUID, persistentData );
        }

        private static void DeselectPersistentLines()
        {
            LineData[] persistentLines = LineManager.instance.GetPersistentItems();
            foreach ( LineData l in persistentLines )
            {
                l.selectedPointIdx = -1;
                l.ClearSelection();
            }
        }

        private static void DrawLine( LineData lineData )
        {
            Vector3[] pathPoints = lineData.pathPoints;
            if ( pathPoints.Length == 0 )
            {
                lineData.UpdatePath( true );
            }

            Handles.zTest = CompareFunction.Always;
            Vector3[] surfacePathPoints = lineData.onSurfacePathPoints;
            Handles.color = new Color( 0f, 0f, 0f, 0.7f );
            Handles.DrawAAPolyLine( 8, surfacePathPoints );
            Handles.color = new Color( 0f, 1f, 1f, 0.5f );
            Handles.DrawAAPolyLine( 4, surfacePathPoints );

            Handles.color = new Color( 0f, 0f, 0f, 0.7f );
            Handles.DrawAAPolyLine( 8, pathPoints );
            Handles.color = new Color( 1f, 1f, 1f, 0.7f );
            Handles.DrawAAPolyLine( 4, pathPoints );
        }

        private static bool DrawLineControlPoints( LineData lineData,             bool     showHandles,
                                                   out bool clickOnPoint,         out bool multiSelection, out bool    addToSelection,
                                                   out bool removedFromSelection, out bool wasEdited,      out Vector3 delta )
        {
            delta                = Vector3.zero;
            clickOnPoint         = false;
            wasEdited            = false;
            multiSelection       = false;
            addToSelection       = false;
            removedFromSelection = false;
            bool leftMouseDown    = Event.current.button == 0 && Event.current.type       == EventType.MouseDown;
            bool selectAll        = ToolManager.editMode      && LineManager.editModeType == LineManager.EditModeType.LINE_POSE;
            bool selectionChanged = false;
            for ( int i = 0; i < lineData.pointsCount; ++i )
            {
                int controlId = GUIUtility.GetControlID( FocusType.Passive );
                if ( selectingLinePoints )
                {
                    Vector2 GUIPos = HandleUtility.WorldToGUIPoint( lineData.GetPoint( i ) );
                    Rect    rect   = _selectionRect;
                    if ( _selectionRect.size.x    < 0
                         || _selectionRect.size.y < 0 )
                    {
                        Vector2 max  = Vector2.Max( _selectionRect.min, _selectionRect.max );
                        Vector2 min  = Vector2.Min( _selectionRect.min, _selectionRect.max );
                        Vector2 size = max - min;
                        rect = new Rect( min, size );
                    }

                    if ( rect.Contains( GUIPos ) )
                    {
                        if ( !Event.current.control
                             && lineData.selectedPointIdx < 0 )
                        {
                            lineData.selectedPointIdx = i;
                        }

                        lineData.AddToSelection( i );
                        clickOnPoint     = true;
                        multiSelection   = true;
                        selectionChanged = true;
                    }
                }
                else if ( !clickOnPoint )
                {
                    if ( showHandles )
                    {
                        float distFromMouse
                            = HandleUtility.DistanceToRectangle( lineData.GetPoint( i ), Quaternion.identity, 0f );
                        HandleUtility.AddControl( controlId, distFromMouse );
                        if ( leftMouseDown && HandleUtility.nearestControl == controlId )
                        {
                            if ( !Event.current.control )
                            {
                                lineData.selectedPointIdx = i;
                                lineData.ClearSelection();
                                selectionChanged = true;
                            }

                            if ( ( !ToolManager.editMode
                                   || ( ToolManager.editMode && LineManager.editModeType == LineManager.EditModeType.NODES ) )
                                 && ( Event.current.control || lineData.selectionCount == 0 ) )
                            {
                                if ( lineData.IsSelected( i ) )
                                {
                                    lineData.RemoveFromSelection( i );
                                    lineData.selectedPointIdx = -1;
                                    removedFromSelection      = true;
                                }
                                else
                                {
                                    lineData.AddToSelection( i );
                                    lineData.showHandles      = true;
                                    lineData.selectedPointIdx = i;
                                    if ( Event.current.control )
                                    {
                                        addToSelection = true;
                                    }
                                }

                                selectionChanged = true;
                            }

                            clickOnPoint = true;
                            Event.current.Use();
                        }
                    }
                }

                if ( Event.current.type != EventType.Repaint )
                {
                    continue;
                }

                DrawDotHandleCap( lineData.GetPoint( i ), 1, 1, lineData.IsSelected( i ) );
            }

            if ( selectionChanged )
            {
                ResetLineRotation();
            }

            Vector3[] midpoints = lineData.midpoints;
            for ( int i = 0; i < midpoints.Length; ++i )
            {
                Vector3 point = midpoints[ i ];

                int controlId = GUIUtility.GetControlID( FocusType.Passive );
                if ( showHandles )
                {
                    float distFromMouse
                        = HandleUtility.DistanceToRectangle( point, Quaternion.identity, 0f );
                    HandleUtility.AddControl( controlId, distFromMouse );
                }

                DrawDotHandleCap( point, 0.4f );
                if ( showHandles && HandleUtility.nearestControl == controlId )
                {
                    DrawDotHandleCap( point );
                    if ( leftMouseDown )
                    {
                        lineData.InsertPoint( i + 1, new LinePoint( point ) );
                        lineData.selectedPointIdx = i + 1;
                        lineData.ClearSelection();
                        updateStroke = true;
                        clickOnPoint = true;
                        Event.current.Use();
                    }
                }
            }

            if ( showHandles
                 && lineData.showHandles
                 && lineData.selectedPointIdx >= 0 )
            {
                Vector3 selectedPoint = lineData.selectedPoint;
                if ( _updateHandlePosition )
                {
                    selectedPoint         = handlePosition;
                    _updateHandlePosition = false;
                }

                Vector3 prevPosition = lineData.selectedPoint;
                lineData.SetPoint( lineData.selectedPointIdx,
                    Handles.PositionHandle( selectedPoint, Quaternion.identity ),
                    registerUndo: true, selectAll );
                Vector3 point = _snapToVertex
                    ? LinePointSnapping( lineData.selectedPoint )
                    : SnapAndUpdateGridOrigin( lineData.selectedPoint, SnapManager.settings.snappingEnabled,
                        LineManager.settings.paintOnPalettePrefabs,    LineManager.settings.paintOnMeshesWithoutCollider,
                        false,                                         Vector3.down );
                lineData.SetPoint( lineData.selectedPointIdx, point, registerUndo: false, selectAll );
                handlePosition = lineData.selectedPoint;
                if ( prevPosition != lineData.selectedPoint )
                {
                    wasEdited    = true;
                    updateStroke = true;
                    delta        = lineData.selectedPoint - prevPosition;
                    ToolProperties.RepainWindow();
                }

                if ( LineManager.editModeType == LineManager.EditModeType.LINE_POSE )
                {
                    Quaternion prevRotation   = _lineRotation;
                    Quaternion handleRotation = Handles.RotationHandle( _lineRotation, lineData.selectedPoint );
                    if ( prevRotation != handleRotation )
                    {
                        RotateLineAround( lineData.selectedPointIdx, handleRotation, lineData );
                        wasEdited    = true;
                        updateStroke = true;
                        ToolProperties.RepainWindow();
                    }
                }
            }

            if ( !showHandles )
            {
                return false;
            }

            return clickOnPoint || wasEdited;
        }

        private static void DrawSelectionRectangle()
        {
            if ( !selectingLinePoints )
            {
                return;
            }

            Ray[] rays =
            {
                HandleUtility.GUIPointToWorldRay( _selectionRect.min ),
                HandleUtility.GUIPointToWorldRay( new Vector2( _selectionRect.xMax, _selectionRect.yMin ) ),
                HandleUtility.GUIPointToWorldRay( _selectionRect.max ),
                HandleUtility.GUIPointToWorldRay( new Vector2( _selectionRect.xMin, _selectionRect.yMax ) ),
            };
            Vector3[] verts = new Vector3[ 4 ];
            for ( int i = 0; i < 4; ++i )
            {
                verts[ i ] = rays[ i ].origin + rays[ i ].direction;
            }

            Handles.DrawSolidRectangleWithOutline( verts,
                new Color( 0f, 0.5f, 0.5f, 0.3f ), new Color( 0f, 0.5f, 0.5f, 1f ) );
        }

        private static void LineDuringSceneGUI( SceneView sceneView )
        {
            if ( LineManager.settings.paintOnMeshesWithoutCollider )
            {
                PWBCore.CreateTempCollidersWithinFrustum( sceneView.camera );
            }

            if ( Event.current.type       == EventType.KeyDown
                 && Event.current.keyCode == KeyCode.Escape )
            {
                if ( _lineData.state               == ToolManager.ToolState.EDIT
                     && _lineData.selectedPointIdx > 0 )
                {
                    _lineData.selectedPointIdx = -1;
                    _lineData.ClearSelection();
                }
                else if ( _lineData.state == ToolManager.ToolState.NONE
                          && !ToolManager.editMode )
                {
                    ToolManager.DeselectTool();
                }
                else if ( ToolManager.editMode )
                {
                    if ( _editingPersistentLine )
                    {
                        ResetSelectedPersistentLine();
                    }
                    else
                    {
                        ToolManager.DeselectTool();
                    }

                    DeselectPersistentLines();
                    _initialPersistentLineData  = null;
                    _selectedPersistentLineData = null;
                    ToolProperties.SetProfile( new ToolProperties.ProfileData( LineManager.instance, _createProfileName ) );
                    ToolProperties.RepainWindow();
                    ToolManager.editMode = false;
                }
                else
                {
                    ResetLineState( false );
                }

                OnUndoLine();
                UpdateStroke();
                BrushstrokeManager.ClearBrushstroke();
            }

            if ( ToolManager.editMode
                 || LineManager.instance.showPreexistingElements )
            {
                LineToolEditMode( sceneView );
            }

            if ( ToolManager.editMode )
            {
                return;
            }

            switch ( _lineData.state )
            {
                case ToolManager.ToolState.NONE:
                    LineStateNone( sceneView.in2DMode );
                    break;
                case ToolManager.ToolState.PREVIEW:
                    LineStateStraightLine( sceneView.in2DMode );
                    break;
                case ToolManager.ToolState.EDIT:
                    LineStateBezier( sceneView );
                    break;
            }
        }

        private static void LineInitializeOnLoad()
        {
            LineManager.settings.OnDataChanged += OnLineSettingsChanged;
        }

        private static void LineInput( bool persistent, SceneView sceneView )
        {
            LineData lineData = persistent ? _selectedPersistentLineData : _lineData;
            if ( lineData == null )
            {
                return;
            }

            if ( Event.current.keyCode == KeyCode.Return
                 && Event.current.type == EventType.KeyDown )
            {
                if ( persistent )
                {
                    DeleteDisabledObjects();
                    ApplySelectedPersistentLine( true );
                    ToolProperties.SetProfile( new ToolProperties.ProfileData( LineManager.instance, _createProfileName ) );
                    DeleteDisabledObjects();
                    ToolProperties.RepainWindow();
                }
                else
                {
                    CreateLine();
                    ResetLineState( false );
                }
            }
            else if ( Event.current.type       == EventType.KeyDown
                      && Event.current.keyCode == KeyCode.Delete
                      && !Event.current.control
                      && !Event.current.alt
                      && !Event.current.shift )
            {
                lineData.RemoveSelectedPoints();
                if ( persistent )
                {
                    PreviewPersistentLine( _selectedPersistentLineData );
                }
                else
                {
                    updateStroke = true;
                }
            }
            else if ( Event.current.type      == EventType.MouseDown
                      && Event.current.button == 1
                      && Event.current.control
                      && !Event.current.alt
                      && !Event.current.shift
                      && LineManager.editModeType == LineManager.EditModeType.NODES )
            {
                if ( MouseDot( out Vector3 point,                out Vector3 normal,                             lineData.settings.mode, sceneView.in2DMode,
                        lineData.settings.paintOnPalettePrefabs, lineData.settings.paintOnMeshesWithoutCollider, false ) )
                {
                    point = _snapToVertex
                        ? LinePointSnapping( point )
                        : SnapAndUpdateGridOrigin( point,            SnapManager.settings.snappingEnabled,
                            lineData.settings.paintOnPalettePrefabs, lineData.settings.paintOnMeshesWithoutCollider,
                            false,                                   Vector3.down );
                    lineData.AddPoint( point, false );
                    if ( persistent )
                    {
                        PreviewPersistentLine( _selectedPersistentLineData );
                        LineStrokePreview( sceneView, lineData, true, true );
                    }
                    else
                    {
                        updateStroke = true;
                    }
                }
            }
            else if ( PWBSettings.shortcuts.lineSelectAllPoints.Check()
                      && LineManager.editModeType == LineManager.EditModeType.NODES )
            {
                lineData.SelectAll();
            }
            else if ( PWBSettings.shortcuts.lineDeselectAllPoints.Check() )
            {
                lineData.selectedPointIdx = -1;
                lineData.ClearSelection();
            }
            else if ( PWBSettings.shortcuts.lineToggleCurve.Check() )
            {
                lineData.ToggleSegmentType();
                updateStroke = true;
            }
            else if ( PWBSettings.shortcuts.lineToggleClosed.Check() )
            {
                lineData.ToggleClosed();
                updateStroke = true;
            }
            else if ( PWBSettings.shortcuts.lineEditGap.Check() )
            {
                float deltaSign = Mathf.Sign( PWBSettings.shortcuts.lineEditGap.combination.delta );
                lineData.settings.gapSize += lineData.lenght * deltaSign * 0.001f;
                ToolProperties.RepainWindow();
            }

            if ( !persistent )
            {
                return;
            }

            if ( PWBSettings.shortcuts.editModeSelectParent.Check()
                 && lineData != null )
            {
                GameObject parent = lineData.GetParent();
                if ( parent != null )
                {
                    Selection.activeGameObject = parent;
                }
            }
            else if ( PWBSettings.shortcuts.editModeDeleteItemButNotItsChildren.Check() )
            {
                LineManager.instance.DeletePersistentItem( lineData.id, false );
            }
            else if ( PWBSettings.shortcuts.editModeDeleteItemAndItsChildren.Check() )
            {
                LineManager.instance.DeletePersistentItem( lineData.id, true );
            }
            else if ( PWBSettings.shortcuts.lineEditModeTypeToggle.Check() )
            {
                LineManager.ToggleEditModeType();
            }
        }

        private static Vector3 LinePointSnapping( Vector3 point )
        {
            const float snapSqrDistance = 400f;
            Ray         mouseRay        = HandleUtility.GUIPointToWorldRay( Event.current.mousePosition );
            LineData[]  persistentLines = LineManager.instance.GetPersistentItems();
            Vector3     result          = point;
            float       minSqrDistance  = snapSqrDistance;
            foreach ( LineData lineData in persistentLines )
            {
                Vector3[] controlPoints = lineData.points;
                foreach ( Vector3 controlPoint in controlPoints )
                {
                    Vector3 intersection         = mouseRay.origin + Vector3.Project( controlPoint - mouseRay.origin, mouseRay.direction );
                    Vector2 GUIControlPoint      = HandleUtility.WorldToGUIPoint( controlPoint );
                    Vector2 intersectionGUIPoint = HandleUtility.WorldToGUIPoint( intersection );
                    float   sqrDistance          = ( GUIControlPoint - intersectionGUIPoint ).sqrMagnitude;
                    if ( sqrDistance    > 0
                         && sqrDistance < snapSqrDistance
                         && sqrDistance < minSqrDistance )
                    {
                        minSqrDistance = sqrDistance;
                        result         = controlPoint;
                    }
                }
            }

            return result;
        }

        private static void LineStateBezier( SceneView sceneView )
        {
            Vector3[] pathPoints        = _lineData.pathPoints;
            bool      forceStrokeUpdate = updateStroke;
            if ( updateStroke )
            {
                _lineData.UpdatePath();
                pathPoints = _lineData.pathPoints;
                BrushstrokeManager.UpdateLineBrushstroke( pathPoints );
                updateStroke = false;
            }

            LineStrokePreview( sceneView, _lineData, false, forceStrokeUpdate );
            DrawLine( _lineData );
            DrawSelectionRectangle();
            LineInput( false, sceneView );

            if ( selectingLinePoints && !Event.current.control )
            {
                _lineData.selectedPointIdx = -1;
                _lineData.ClearSelection();
            }

            bool clickOnPoint, wasEdited;
            DrawLineControlPoints( _lineData, true,          out clickOnPoint, out bool multiSelection, out bool addToselection,
                out bool removeFromSelection, out wasEdited, out Vector3 delta );
            if ( wasEdited )
            {
                updateStroke = true;
            }

            SelectionRectangleInput( clickOnPoint );
        }

        private static void LineStateNone( bool in2DMode )
        {
            if ( Event.current.button  == 0
                 && Event.current.type == EventType.MouseDown
                 && !Event.current.alt )
            {
                _lineData.state = ToolManager.ToolState.PREVIEW;
                Event.current.Use();
            }

            if ( MouseDot( out Vector3 point,                   out Vector3 normal,                                LineManager.settings.mode, in2DMode,
                    LineManager.settings.paintOnPalettePrefabs, LineManager.settings.paintOnMeshesWithoutCollider, false ) )
            {
                point = _snapToVertex
                    ? LinePointSnapping( point )
                    : SnapAndUpdateGridOrigin( point,               SnapManager.settings.snappingEnabled,
                        LineManager.settings.paintOnPalettePrefabs, LineManager.settings.paintOnMeshesWithoutCollider,
                        false,                                      Vector3.down );
                _lineData.SetPoint( 0, point, registerUndo: false, selectAll: false );
                _lineData.SetPoint( 1, point, registerUndo: false, selectAll: false );
            }

            DrawDotHandleCap( _lineData.GetPoint( 0 ) );
        }

        private static void LineStateStraightLine( bool in2DMode )
        {
            if ( Event.current.button  == 0
                 && Event.current.type == EventType.MouseDown
                 && !Event.current.alt )
            {
                _lineData.state = ToolManager.ToolState.EDIT;
                updateStroke    = true;
            }

            if ( MouseDot( out Vector3 point,                   out Vector3 normal,                                LineManager.settings.mode, in2DMode,
                    LineManager.settings.paintOnPalettePrefabs, LineManager.settings.paintOnMeshesWithoutCollider, false ) )
            {
                point = _snapToVertex
                    ? LinePointSnapping( point )
                    : SnapAndUpdateGridOrigin( point,               SnapManager.settings.snappingEnabled,
                        LineManager.settings.paintOnPalettePrefabs, LineManager.settings.paintOnMeshesWithoutCollider,
                        false,                                      Vector3.down );
                _lineData.SetPoint( 1, point, registerUndo: false, selectAll: false );
            }

            Handles.color = new Color( 0f, 0f, 0f, 0.7f );
            Handles.DrawAAPolyLine( 8, _lineData.GetPoint( 0 ), _lineData.GetPoint( 1 ) );
            Handles.color = new Color( 1f, 1f, 1f, 0.7f );
            Handles.DrawAAPolyLine( 4, _lineData.GetPoint( 0 ), _lineData.GetPoint( 1 ) );
            DrawDotHandleCap( _lineData.GetPoint( 0 ) );
            DrawDotHandleCap( _lineData.GetPoint( 1 ) );
        }

        private static void LineStrokePreview( SceneView sceneView,
                                               LineData  lineData, bool persistent, bool forceUpdate )
        {
            LineSettings settings                  = lineData.settings;
            Vector3      lastPoint                 = lineData.lastPathPoint;
            int          objectCount               = lineData.objectCount;
            Vector3      lastObjectTangentPosition = lineData.lastTangentPos;

            BrushstrokeItem[] brushstroke = null;

            if ( PreviewIfBrushtrokestaysTheSame( out brushstroke, sceneView.camera, forceUpdate ) )
            {
                return;
            }

            PWBCore.UpdateTempCollidersIfHierarchyChanged();

            if ( !persistent )
            {
                _paintStroke.Clear();
            }

            for ( int i = 0; i < brushstroke.Length; ++i )
            {
                BrushstrokeItem strokeItem = brushstroke[ i ];
                GameObject      prefab     = strokeItem.settings.prefab;
                if ( prefab == null )
                {
                    continue;
                }

                Bounds        bounds        = BoundsUtils.GetBoundsRecursive( prefab.transform, prefab.transform.rotation );
                BrushSettings brushSettings = strokeItem.settings;
                if ( LineManager.settings.overwriteBrushProperties )
                {
                    brushSettings = LineManager.settings.brushSettings;
                }

                Vector3 size = Vector3.Scale( bounds.size, brushSettings.scaleMultiplier );

                Vector3 pivotToCenter = Vector3.Scale(
                    prefab.transform.InverseTransformDirection( bounds.center - prefab.transform.position ),
                    brushSettings.scaleMultiplier );
                float   height     = Mathf.Max( size.x, size.y, size.z ) * 2;
                Vector3 segmentDir = Vector3.zero;

                if ( settings.objectsOrientedAlongTheLine
                     && brushstroke.Length > 1 )
                {
                    segmentDir = i < brushstroke.Length - 1
                        ? strokeItem.nextTangentPosition - strokeItem.tangentPosition
                        : lastPoint                      - strokeItem.tangentPosition;
                }

                if ( brushstroke.Length == 1 )
                {
                    segmentDir = lastPoint - brushstroke[ 0 ].tangentPosition;
                    if ( persistent && objectCount > 0 )
                    {
                        segmentDir = lastPoint - lastObjectTangentPosition;
                    }
                }

                if ( i == brushstroke.Length - 1 )
                {
                    float onLineSize = AxesUtils.GetAxisValue( size, settings.axisOrientedAlongTheLine )
                                       + settings.gapSize;
                    float segmentSize = segmentDir.magnitude;
                    if ( segmentSize > onLineSize )
                    {
                        segmentDir = segmentDir.normalized
                                     * ( settings.spacingType == LineSettings.SpacingType.BOUNDS ? onLineSize : settings.spacing );
                    }
                }

                if ( settings.objectsOrientedAlongTheLine
                     && !settings.perpendicularToTheSurface )
                {
                    AxesUtils.Axis projectionAxis = ( (AxesUtils.SignedAxis)settings.projectionDirection ).axis;
                    segmentDir -= AxesUtils.GetVector( AxesUtils.GetAxisValue( segmentDir, projectionAxis ), projectionAxis );
                }

                Vector3          normal       = -settings.projectionDirection;
                AxesUtils.Axis[] otherAxes    = AxesUtils.GetOtherAxes( (AxesUtils.SignedAxis)( -settings.projectionDirection ) );
                AxesUtils.Axis   tangetAxis   = otherAxes[ settings.objectsOrientedAlongTheLine ? 0 : 1 ];
                Vector3          itemTangent  = (AxesUtils.SignedAxis)tangetAxis;
                Quaternion       itemRotation = Quaternion.LookRotation( itemTangent, normal );
                Quaternion lookAt = Quaternion.LookRotation( (Vector3)(AxesUtils.SignedAxis)
                    settings.axisOrientedAlongTheLine, Vector3.up );

                Vector3 itemPosition = strokeItem.tangentPosition + segmentDir / 2;

                Ray       ray     = new Ray( itemPosition + normal * height, -normal );
                Transform surface = null;
                if ( settings.mode != PaintOnSurfaceToolSettingsBase.PaintMode.ON_SHAPE )
                {
                    if ( MouseRaycast( ray,                 out RaycastHit itemHit,
                            out GameObject collider,        float.MaxValue, -1,
                            settings.paintOnPalettePrefabs, settings.paintOnMeshesWithoutCollider ) )
                    {
                        itemPosition = itemHit.point;
                        if ( settings.perpendicularToTheSurface )
                        {
                            normal = itemHit.normal;
                        }

                        GameObject colObj = PWBCore.GetGameObjectFromTempCollider( collider );
                        if ( colObj != null )
                        {
                            surface = colObj.transform;
                        }
                    }
                    else if ( settings.mode == PaintOnSurfaceToolSettingsBase.PaintMode.ON_SURFACE )
                    {
                        continue;
                    }
                }

                if ( settings.perpendicularToTheSurface
                     && segmentDir != Vector3.zero )
                {
                    if ( settings.mode == PaintOnSurfaceToolSettingsBase.PaintMode.ON_SHAPE )
                    {
                        Vector3 bitangent  = Vector3.Cross( segmentDir, normal );
                        Vector3 lineNormal = Vector3.Cross( bitangent,  segmentDir );
                        itemRotation = Quaternion.LookRotation( segmentDir, lineNormal ) * lookAt;
                    }
                    else
                    {
                        Plane   plane   = new Plane( normal, itemPosition );
                        Vector3 tangent = plane.ClosestPointOnPlane( segmentDir + itemPosition ) - itemPosition;
                        itemRotation = Quaternion.LookRotation( tangent, normal ) * lookAt;
                    }
                }
                else if ( !settings.perpendicularToTheSurface
                          && segmentDir != Vector3.zero )
                {
                    itemRotation = Quaternion.LookRotation( segmentDir, normal ) * lookAt;
                }

                itemPosition += normal * strokeItem.surfaceDistance;
                itemRotation *= Quaternion.Euler( strokeItem.additionalAngle );

                itemPosition += itemRotation * brushSettings.localPositionOffset;
                itemPosition -= itemRotation * ( pivotToCenter - Vector3.up * ( size.y / 2 ) );
                if ( brushSettings.embedInSurface
                     && settings.mode != PaintOnSurfaceToolSettingsBase.PaintMode.ON_SHAPE )
                {
                    if ( brushSettings.embedAtPivotHeight )
                    {
                        itemPosition += itemRotation * new Vector3( 0f, strokeItem.settings.bottomMagnitude, 0f );
                    }
                    else
                    {
                        Matrix4x4 TRS = Matrix4x4.TRS( itemPosition, itemRotation,
                            Vector3.Scale( prefab.transform.localScale, strokeItem.scaleMultiplier ) );
                        float bottomDistanceToSurfce = GetBottomDistanceToSurface( strokeItem.settings.bottomVertices,
                            TRS, Mathf.Abs( strokeItem.settings.bottomMagnitude ), settings.paintOnPalettePrefabs,
                            settings.paintOnMeshesWithoutCollider );
                        itemPosition += itemRotation * new Vector3( 0f, -bottomDistanceToSurfce, 0f );
                    }
                }

                Matrix4x4 rootToWorld = Matrix4x4.TRS( itemPosition, itemRotation, strokeItem.scaleMultiplier )
                                        * Matrix4x4.Rotate( Quaternion.Inverse( prefab.transform.rotation ) )
                                        * Matrix4x4.Translate( -prefab.transform.position );
                Vector3 itemScale = Vector3.Scale( prefab.transform.localScale, strokeItem.scaleMultiplier );
                int     layer     = settings.overwritePrefabLayer ? settings.layer : prefab.layer;

                Transform parentTransform = settings.parent;
                PaintStrokeItem paintItem = new PaintStrokeItem( prefab, itemPosition, itemRotation,
                    itemScale, layer, parentTransform, surface, strokeItem.flipX, strokeItem.flipY );
                paintItem.persistentParentId = persistent ? lineData.hexId : LineData.nextHexId;
                _paintStroke.Add( paintItem );
                PreviewBrushItem( prefab, rootToWorld, layer,            sceneView.camera,
                    false,                false,       strokeItem.flipX, strokeItem.flipY );
                PreviewData prevData = new PreviewData( prefab, rootToWorld, layer, strokeItem.flipX, strokeItem.flipY );
                _previewData.Add( prevData );
            }

            if ( _persistentPreviewData.ContainsKey( lineData.id ) )
            {
                _persistentPreviewData[ lineData.id ] = _previewData.ToArray();
            }
            else
            {
                _persistentPreviewData.Add( lineData.id, _previewData.ToArray() );
            }
        }

        private static void LineToolEditMode( SceneView sceneView )
        {
            LineData[]     persistentLines     = LineManager.instance.GetPersistentItems();
            long           selectedLineId      = _initialPersistentLineData == null ? -1 : _initialPersistentLineData.id;
            bool           clickOnAnyPoint     = false;
            bool           someLinesWereEdited = false;
            Vector3        delta               = Vector3.zero;
            LineData       editedData          = _selectedPersistentLineData;
            List<LineData> deselectedLines     = new List<LineData>( persistentLines );
            DrawSelectionRectangle();
            foreach ( LineData lineData in persistentLines )
            {
                lineData.UpdateObjects();
                if ( lineData.objectCount == 0 )
                {
                    LineManager.instance.RemovePersistentItem( lineData.id );
                    continue;
                }

                DrawLine( lineData );

                if ( DrawLineControlPoints( lineData, ToolManager.editMode,          out bool clickOnPoint, out bool multiSelection,
                        out bool addToselection,      out bool removedFromSelection, out bool wasEdited,    out Vector3 localDelta ) )
                {
                    if ( clickOnPoint )
                    {
                        clickOnAnyPoint        = true;
                        _editingPersistentLine = true;
                        if ( selectedLineId != lineData.id )
                        {
                            ApplySelectedPersistentLine( false );
                            if ( selectedLineId == -1 )
                            {
                                _createProfileName = LineManager.instance.selectedProfileName;
                            }

                            LineManager.instance.CopyToolSettings( lineData.settings );
                            ToolProperties.RepainWindow();
                            PWBCore.SetActiveTempColliders( lineData.objects, false );
                        }

                        _selectedPersistentLineData = lineData;
                        if ( _initialPersistentLineData == null )
                        {
                            _initialPersistentLineData = lineData.Clone();
                        }
                        else if ( _initialPersistentLineData.id != lineData.id )
                        {
                            _initialPersistentLineData = lineData.Clone();
                        }

                        if ( !removedFromSelection )
                        {
                            foreach ( LineData l in persistentLines )
                            {
                                l.showHandles = l == lineData;
                            }
                        }

                        deselectedLines.Remove( lineData );
                    }

                    if ( addToselection )
                    {
                        deselectedLines.Clear();
                        lineData.showHandles = true;
                    }

                    if ( removedFromSelection )
                    {
                        deselectedLines.Clear();
                    }

                    if ( wasEdited )
                    {
                        _editingPersistentLine = true;
                        someLinesWereEdited    = true;
                        delta                  = localDelta;
                        editedData             = lineData;
                    }
                }
            }

            if ( clickOnAnyPoint )
            {
                foreach ( LineData lineData in deselectedLines )
                {
                    lineData.showHandles      = false;
                    lineData.selectedPointIdx = -1;
                    lineData.ClearSelection();
                    PWBCore.SetActiveTempColliders( lineData.objects, true );
                }
            }

            LineData[] linesEdited = persistentLines.Where( i => i.selectionCount > 0 ).ToArray();

            if ( someLinesWereEdited && linesEdited.Length > 0 )
            {
                _disabledObjects.Clear();
            }

            if ( someLinesWereEdited && linesEdited.Length > 1 )
            {
                _paintStroke.Clear();
                foreach ( LineData lineData in linesEdited )
                {
                    if ( lineData != editedData )
                    {
                        lineData.AddDeltaToSelection( delta );
                    }

                    lineData.UpdatePath();
                    PreviewPersistentLine( lineData );
                    LineStrokePreview( sceneView, lineData, true, true );
                }

                PWBCore.SetSavePending();
                return;
            }

            if ( linesEdited.Length > 1 )
            {
                PreviewPersistent( sceneView.camera );
            }

            if ( !ToolManager.editMode )
            {
                return;
            }

            if ( LineManager.editModeType == LineManager.EditModeType.NODES )
            {
                SelectionRectangleInput( clickOnAnyPoint );
            }

            if ( !someLinesWereEdited
                 && linesEdited.Length <= 1
                 && _editingPersistentLine
                 && _selectedPersistentLineData != null )
            {
                bool forceStrokeUpdate = updateStroke;
                if ( updateStroke )
                {
                    _selectedPersistentLineData.UpdatePath();
                    PreviewPersistentLine( _selectedPersistentLineData );
                    updateStroke = false;
                    PWBCore.SetSavePending();
                }

                if ( _brushstroke != null
                     && !BrushstrokeManager.BrushstrokeEqual( BrushstrokeManager.brushstroke, _brushstroke ) )
                {
                    _paintStroke.Clear();
                }

                LineStrokePreview( sceneView, _selectedPersistentLineData, true, forceStrokeUpdate );
            }

            LineInput( true, sceneView );
        }

        private static void OnLineSettingsChanged()
        {
            repaint = true;
            if ( !ToolManager.editMode )
            {
                _lineData.settings = LineManager.settings;
                updateStroke       = true;
                return;
            }

            if ( _selectedPersistentLineData == null )
            {
                return;
            }

            _selectedPersistentLineData.settings.Copy( LineManager.settings );
            PreviewPersistentLine( _selectedPersistentLineData );
        }

        private static void OnLineToolModeChanged()
        {
            DeselectPersistentLines();
            if ( !ToolManager.editMode )
            {
                if ( _createProfileName != null )
                {
                    ToolProperties.SetProfile( new ToolProperties.ProfileData( LineManager.instance, _createProfileName ) );
                }

                ToolProperties.RepainWindow();
                return;
            }

            ResetLineState();
            ResetSelectedPersistentLine();
            LineManager.editModeType = LineManager.EditModeType.NODES;
        }

        private static void OnUndoLine() => ClearLineStroke();

        private static void PreviewPersistentLine( LineData lineData )
        {
            PWBCore.UpdateTempCollidersIfHierarchyChanged();

            Vector3[]        objPos    = null;
            List<GameObject> objList   = lineData.objectList;
            Vector3[]        strokePos = null;
            LineSettings     settings  = lineData.settings;
            BrushstrokeManager.UpdatePersistentLineBrushstroke( lineData.pathPoints,
                settings, objList, out objPos, out strokePos );
            _disabledObjects.AddRange( lineData.objects.ToList() );
            float   pathLength     = 0;
            Vector3 prevSegmentDir = Vector3.zero;

            BrushSettings brushSettings = PaletteManager.GetBrushById( lineData.initialBrushId );
            if ( brushSettings                   == null
                 && PaletteManager.selectedBrush != null )
            {
                brushSettings = PaletteManager.selectedBrush;
                lineData.SetInitialBrushId( brushSettings.id );
            }

            if ( settings.overwriteBrushProperties )
            {
                brushSettings = settings.brushSettings;
            }

            if ( brushSettings == null )
            {
                brushSettings = new BrushSettings();
            }

            GameObject[] objArray = objList.ToArray();
            for ( int objIdx = 0; objIdx < objPos.Length; ++objIdx )
            {
                GameObject obj = objList[ objIdx ];

                obj.SetActive( true );
                if ( objIdx > 0 )
                {
                    pathLength += ( objPos[ objIdx ] - objPos[ objIdx - 1 ] ).magnitude;
                }

                Bounds bounds = BoundsUtils.GetBoundsRecursive( obj.transform, obj.transform.rotation );

                Vector3 size = bounds.size;
                Vector3 pivotToCenter = Vector3.Scale( obj.transform.InverseTransformPoint( bounds.center ),
                    obj.transform.lossyScale );
                float   height        = Mathf.Max( size.x, size.y, size.z ) * 2;
                Vector3 segmentDir    = Vector3.zero;
                float   objOnLineSize = AxesUtils.GetAxisValue( size, settings.axisOrientedAlongTheLine );
                if ( settings.objectsOrientedAlongTheLine
                     && objPos.Length > 1 )
                {
                    if ( objIdx < objPos.Length - 1 )
                    {
                        if ( objIdx + 1 < objPos.Length )
                        {
                            segmentDir = objPos[ objIdx + 1 ] - objPos[ objIdx ];
                        }
                        else if ( strokePos.Length > 0 )
                        {
                            segmentDir = strokePos[ 0 ] - objPos[ objIdx ];
                        }

                        prevSegmentDir = segmentDir;
                    }
                    else
                    {
                        Vector3 nearestPathPoint = LineData.NearestPathPoint( objPos[ objIdx ],
                            objOnLineSize, lineData.pathPoints, out int nearestPointIdx );
                        segmentDir = nearestPathPoint - objPos[ objIdx ];
                        segmentDir = segmentDir.normalized * prevSegmentDir.magnitude;
                    }
                }

                if ( objPos.Length == 1 )
                {
                    segmentDir = lineData.lastPathPoint - objPos[ 0 ];
                }
                else if ( objIdx == objPos.Length - 1 )
                {
                    float onLineSize  = objOnLineSize + settings.gapSize;
                    float segmentSize = segmentDir.magnitude;
                    if ( segmentSize > onLineSize )
                    {
                        segmentDir = segmentDir.normalized
                                     * ( settings.spacingType == LineSettings.SpacingType.BOUNDS ? onLineSize : settings.spacing );
                    }
                }

                if ( settings.objectsOrientedAlongTheLine
                     && !settings.perpendicularToTheSurface )
                {
                    AxesUtils.Axis projectionAxis = ( (AxesUtils.SignedAxis)settings.projectionDirection ).axis;
                    segmentDir -= AxesUtils.GetVector( AxesUtils.GetAxisValue( segmentDir, projectionAxis ), projectionAxis );
                }

                Vector3          normal       = -settings.projectionDirection;
                AxesUtils.Axis[] otherAxes    = AxesUtils.GetOtherAxes( (AxesUtils.SignedAxis)( -settings.projectionDirection ) );
                AxesUtils.Axis   tangetAxis   = otherAxes[ settings.objectsOrientedAlongTheLine ? 0 : 1 ];
                Vector3          itemTangent  = (AxesUtils.SignedAxis)tangetAxis;
                Quaternion       itemRotation = Quaternion.LookRotation( itemTangent, normal );
                Quaternion lookAt = Quaternion.LookRotation( (Vector3)(AxesUtils.SignedAxis)
                    settings.axisOrientedAlongTheLine, Vector3.up );
                if ( segmentDir != Vector3.zero )
                {
                    itemRotation = Quaternion.LookRotation( segmentDir, normal ) * lookAt;
                }

                Vector3 itemPosition = objPos[ objIdx ];
                Ray     ray          = new Ray( itemPosition + normal * height, -normal );
                if ( settings.mode != PaintOnSurfaceToolSettingsBase.PaintMode.ON_SHAPE )
                {
                    if ( MouseRaycast( ray,                 out RaycastHit itemHit, out GameObject collider, float.MaxValue, -1,
                            settings.paintOnPalettePrefabs, settings.paintOnMeshesWithoutCollider,
                            tags: null,                     terrainLayers: null, exceptions: objArray ) )
                    {
                        itemPosition = itemHit.point;
                        if ( settings.perpendicularToTheSurface )
                        {
                            normal = itemHit.normal;
                        }
                    }
                    else if ( settings.mode == PaintOnSurfaceToolSettingsBase.PaintMode.ON_SURFACE )
                    {
                        continue;
                    }
                }

                if ( settings.perpendicularToTheSurface
                     && segmentDir != Vector3.zero )
                {
                    if ( settings.mode == PaintOnSurfaceToolSettingsBase.PaintMode.ON_SHAPE )
                    {
                        Vector3 bitangent  = Vector3.Cross( segmentDir, normal );
                        Vector3 lineNormal = Vector3.Cross( bitangent,  segmentDir );
                        itemRotation = Quaternion.LookRotation( segmentDir, lineNormal ) * lookAt;
                    }
                    else
                    {
                        Plane   plane   = new Plane( normal, itemPosition );
                        Vector3 tangent = plane.ClosestPointOnPlane( segmentDir + itemPosition ) - itemPosition;
                        itemRotation = Quaternion.LookRotation( tangent, normal ) * lookAt;
                    }
                }
                else if ( !settings.perpendicularToTheSurface
                          && segmentDir != Vector3.zero )
                {
                    itemRotation = Quaternion.LookRotation( segmentDir, normal ) * lookAt;
                }

                itemPosition += normal       * brushSettings.surfaceDistance;
                itemPosition += itemRotation * brushSettings.localPositionOffset;
                Vector3 sizeOffset = itemRotation
                                     * ( settings.axisOrientedAlongTheLine == AxesUtils.Axis.X
                                         ? Vector3.left    * ( size.x / 2 )
                                         : Vector3.forward * ( size.z / 2 ) );
                itemPosition += sizeOffset;
                itemPosition += itemRotation * ( Vector3.up * ( size.y / 2 ) - pivotToCenter );

                if ( brushSettings.embedInSurface
                     && settings.mode != PaintOnSurfaceToolSettingsBase.PaintMode.ON_SHAPE )
                {
                    float bottomMagnitude = BoundsUtils.GetBottomMagnitude( obj.transform );
                    if ( brushSettings.embedAtPivotHeight )
                    {
                        itemPosition += itemRotation * new Vector3( 0f, bottomMagnitude, 0f );
                    }
                    else
                    {
                        Matrix4x4 TRS            = Matrix4x4.TRS( itemPosition, itemRotation, obj.transform.lossyScale );
                        Vector3[] bottomVertices = BoundsUtils.GetBottomVertices( obj.transform );
                        float bottomDistanceToSurfce = GetBottomDistanceToSurface( bottomVertices, TRS,
                            Mathf.Abs( bottomMagnitude ),                                          settings.paintOnPalettePrefabs, settings.paintOnMeshesWithoutCollider );
                        itemPosition += itemRotation * new Vector3( 0f, -bottomDistanceToSurfce, 0f );
                    }
                }

                Undo.RecordObject( obj.transform, LineData.COMMAND_NAME );
                obj.transform.SetPositionAndRotation( itemPosition, itemRotation );
                _disabledObjects.Remove( obj );
                lineData.lastTangentPos = objPos[ objIdx ];
            }

            _disabledObjects = _disabledObjects.Where( i => i != null ).ToList();
            foreach ( GameObject obj in _disabledObjects )
            {
                obj.SetActive( false );
            }
        }

        private static void ResetSelectedPersistentLine()
        {
            _editingPersistentLine = false;
            if ( _initialPersistentLineData == null )
            {
                return;
            }

            LineData selectedLine = LineManager.instance.GetItem( _initialPersistentLineData.id );
            if ( selectedLine == null )
            {
                return;
            }

            selectedLine.ResetPoses( _initialPersistentLineData );
            selectedLine.selectedPointIdx = -1;
            selectedLine.ClearSelection();
        }

        private static void RotateLineAround( int idx, Quaternion rotation, LineData lineData )
        {
            Vector3 pivotPosition = lineData.GetPoint( idx );
            for ( int i = 0; i < lineData.pointsCount; ++i )
            {
                if ( i == idx )
                {
                    continue;
                }

                Vector3 localPositionUnrotated = Quaternion.Inverse( _lineRotation ) * ( lineData.GetPoint( i ) - pivotPosition );
                Vector3 localPosition          = rotation                            * localPositionUnrotated;
                lineData.SetRotatedPoint( i, pivotPosition + localPosition, true );
            }

            _lineRotation = rotation;
            lineData.UpdatePath();
        }

        private static void SelectionRectangleInput( bool clickOnPoint )
        {
            bool leftMouseDown = Event.current.button == 1 && Event.current.type == EventType.MouseDown;
            if ( !selectingLinePoints
                 && Event.current.shift
                 && leftMouseDown
                 && !clickOnPoint )
            {
                selectingLinePoints = true;
                _selectionRect      = new Rect( Event.current.mousePosition, Vector2.zero );
                Event.current.Use();
            }

            if ( ( Event.current.type == EventType.MouseDrag || Event.current.type == EventType.MouseMove )
                 && selectingLinePoints )
            {
                _selectionRect.size = Event.current.mousePosition - _selectionRect.position;
            }

            if ( Event.current.button == 0
                 && ( Event.current.type    == EventType.MouseUp
                      || Event.current.type == EventType.Ignore
                      || Event.current.type == EventType.KeyUp ) )
            {
                selectingLinePoints = false;
            }
        }

        #endregion

    }

    #endregion

}