using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace PluginMaster
{

    #region DATA & SETTINGS

    [Serializable]
    public class SnapSettings
    {

        #region Serialized

        [SerializeField] private bool       _snappingEnabled;
        [SerializeField] private Bool3      _snappingOn;
        [SerializeField] private bool       _visibleGrid;
        [SerializeField] private Bool3      _gridOn = new Bool3( false, true, false );
        [SerializeField] private bool       _lockedGrid;
        [SerializeField] private Vector3    _step               = Vector3.one;
        [SerializeField] private Vector3    _origin             = Vector3.zero;
        [SerializeField] private Quaternion _rotation           = Quaternion.identity;
        [SerializeField] private bool       _showPositionHandle = true;
        [SerializeField] private bool       _showRotationHandle;
        [SerializeField] private bool       _showScaleHandle;
        [SerializeField] private bool       _radialGridEnabled;
        [SerializeField] private float      _radialStep          = 1f;
        [SerializeField] private int        _radialSectors       = 8;
        [SerializeField] private bool       _snapToRadius        = true;
        [SerializeField] private bool       _snapToCircunference = true;
        [SerializeField] private Vector3Int _majorLinesGap       = Vector3Int.one * 10;
        [SerializeField] private bool       _midpointSnapping;

        #endregion

        #region Public Fields

        public Action OnDataChanged;

        #endregion

        #region Public Properties

        public AxesUtils.Axis gridAxis => gridOnX ? AxesUtils.Axis.X : gridOnY ? AxesUtils.Axis.Y : AxesUtils.Axis.Z;

        public bool gridOnX
        {
            get => _gridOn.x;
            set
            {
                if ( _gridOn.x == value )
                {
                    return;
                }

                _gridOn.x = value;
                if ( value )
                {
                    _gridOn.y     = _gridOn.z = false;
                    _snappingOn.x = false;
                    _snappingOn.y = _snappingOn.z = true;
                }

                DataChanged();
            }
        }

        public bool gridOnY
        {
            get => _gridOn.y;
            set
            {
                if ( _gridOn.y == value )
                {
                    return;
                }

                _gridOn.y = value;
                if ( value )
                {
                    _gridOn.x     = _gridOn.z = false;
                    _snappingOn.y = false;
                    _snappingOn.x = _snappingOn.z = true;
                }

                DataChanged();
            }
        }

        public bool gridOnZ
        {
            get => _gridOn.z;
            set
            {
                if ( _gridOn.z == value )
                {
                    return;
                }

                _gridOn.z = value;
                if ( value )
                {
                    _gridOn.x     = _gridOn.y = false;
                    _snappingOn.z = false;
                    _snappingOn.y = _snappingOn.x = true;
                }

                DataChanged();
            }
        }

        public bool lockedGrid
        {
            get => _lockedGrid;
            set
            {
                if ( _lockedGrid == value )
                {
                    return;
                }

                _lockedGrid = value;
                DataChanged();
            }
        }

        public Vector3Int majorLinesGap
        {
            get => _majorLinesGap;
            set
            {
                value = Vector3Int.Max( value, Vector3Int.one );
                if ( _majorLinesGap == value )
                {
                    return;
                }

                _majorLinesGap = value;
                DataChanged();
            }
        }

        public bool midpointSnapping
        {
            get => _midpointSnapping;
            set
            {
                if ( _midpointSnapping == value )
                {
                    return;
                }

                _midpointSnapping = value;
                DataChanged();
            }
        }

        public Vector3 origin
        {
            get => _origin;
            set
            {
                if ( _origin == value )
                {
                    return;
                }

                _origin = value;
            }
        }

        public bool radialGridEnabled
        {
            get => _radialGridEnabled;
            set
            {
                if ( _radialGridEnabled == value )
                {
                    return;
                }

                _radialGridEnabled = value;
                DataChanged();
            }
        }

        public int radialSectors
        {
            get => _radialSectors;
            set
            {
                value = Mathf.Max( value, 3 );
                if ( _radialSectors == value )
                {
                    return;
                }

                _radialSectors = value;
                DataChanged();
            }
        }

        public float radialStep
        {
            get => _radialStep;
            set
            {
                value = Mathf.Max( value, 0.1f );
                if ( _radialStep == value )
                {
                    return;
                }

                _radialStep = value;
                DataChanged();
            }
        }

        public Quaternion rotation
        {
            get => _rotation;
            set
            {
                if ( _rotation == value )
                {
                    return;
                }

                _rotation = value;
                DataChanged( false );
            }
        }

        public bool showPositionHandle
        {
            get => _showPositionHandle;
            set
            {
                if ( _showPositionHandle == value )
                {
                    return;
                }

                _showPositionHandle = value;
                if ( _showPositionHandle )
                {
                    _showRotationHandle = false;
                    _showScaleHandle    = false;
                }

                SnapManager.FrameGridOrigin();
                DataChanged();
            }
        }

        public bool showRotationHandle
        {
            get => _showRotationHandle;
            set
            {
                if ( _showRotationHandle == value )
                {
                    return;
                }

                _showRotationHandle = value;
                if ( _showRotationHandle )
                {
                    _showPositionHandle = false;
                    _showScaleHandle    = false;
                    SnapManager.FrameGridOrigin();
                }

                DataChanged();
            }
        }

        public bool showScaleHandle
        {
            get => _showScaleHandle;
            set
            {
                if ( _showScaleHandle == value )
                {
                    return;
                }

                _showScaleHandle = value;
                if ( _showScaleHandle )
                {
                    _showPositionHandle = false;
                    _showRotationHandle = false;
                    SnapManager.FrameGridOrigin();
                }

                DataChanged();
            }
        }

        public bool snappingEnabled
        {
            get => _snappingEnabled;
            set
            {
                if ( _snappingEnabled == value )
                {
                    return;
                }

                _snappingEnabled = value;
                DataChanged();
            }
        }

        public bool snappingOnX
        {
            get => _snappingOn.x;
            set
            {
                if ( _snappingOn.x == value )
                {
                    return;
                }

                _snappingOn.x = value;
                DataChanged();
            }
        }

        public bool snappingOnY
        {
            get => _snappingOn.y;
            set
            {
                if ( _snappingOn.y == value )
                {
                    return;
                }

                _snappingOn.y = value;
                DataChanged();
            }
        }

        public bool snappingOnZ
        {
            get => _snappingOn.z;
            set
            {
                if ( _snappingOn.z == value )
                {
                    return;
                }

                _snappingOn.z = value;
                DataChanged();
            }
        }

        public bool snapToCircunference
        {
            get => _snapToCircunference;
            set
            {
                if ( _snapToCircunference == value )
                {
                    return;
                }

                _snapToCircunference = value;
            }
        }

        public bool snapToRadius
        {
            get => _snapToRadius;
            set
            {
                if ( _snapToRadius == value )
                {
                    return;
                }

                _snapToRadius = value;
                DataChanged();
            }
        }

        public Vector3 step
        {
            get => _step;
            set
            {
                value = Vector3.Max( value, Vector3.one * 0.1f );
                if ( _step == value )
                {
                    return;
                }

                _step = value;
                DataChanged( false );
            }
        }

        public bool visibleGrid
        {
            get => _visibleGrid;
            set
            {
                if ( _visibleGrid == value )
                {
                    return;
                }

                _visibleGrid = value;
                DataChanged();
            }
        }

        #endregion

        #region Public Methods

        public bool IsSnappingEnabledInThisDirection( Vector3 direction )
        {
            bool isParallel( Vector3 other )
            {
                return Vector3.Cross( direction, other ).magnitude < 0.0000001;
            }

            if ( isParallel( _rotation * Vector3.up )
                 && _snappingOn.y )
            {
                return true;
            }

            if ( isParallel( _rotation * Vector3.right )
                 && _snappingOn.x )
            {
                return true;
            }

            if ( isParallel( _rotation * Vector3.forward )
                 && _snappingOn.z )
            {
                return true;
            }

            return false;
        }

        public void SetOriginHeight( Vector3 point, AxesUtils.Axis axis )
        {
            Vector3 originPos = origin;
            AxesUtils.SetAxisValue( ref originPos, axis, AxesUtils.GetAxisValue( point, axis ) );
            origin = originPos;
        }

        public Vector3 TransformToGridDirection( Vector3 direction )
        {
            if ( direction == Vector3.zero )
            {
                return _rotation * Vector3.up;
            }

            Vector3 xProjection          = Vector3.Project( direction, _rotation * Vector3.right );
            Vector3 yProjection          = Vector3.Project( direction, _rotation * Vector3.up );
            Vector3 zProjection          = Vector3.Project( direction, _rotation * Vector3.forward );
            float   xProjectionMagnitude = xProjection.magnitude;
            float   yProjectionMagnitude = yProjection.magnitude;
            float   zProjectionMagnitude = zProjection.magnitude;
            float   max                  = Mathf.Max( xProjectionMagnitude, yProjectionMagnitude, zProjectionMagnitude );
            if ( xProjectionMagnitude == max )
            {
                return xProjection.normalized;
            }

            if ( yProjectionMagnitude == max )
            {
                return yProjection.normalized;
            }

            return zProjection.normalized;
        }

        #endregion

        #region Private Methods

        private void DataChanged( bool repaint = true )
        {
            if ( !repaint )
            {
                PWBCore.staticData.SetSavePending();
                return;
            }

            PWBCore.SetSavePending();
            if ( OnDataChanged != null )
            {
                OnDataChanged();
            }

            SceneView.RepaintAll();
        }

        #endregion

        [Serializable]
        private struct Bool3
        {

            #region Serialized

            public bool x, y, z;

            #endregion

            #region Public Constructors

            public Bool3( bool x = true, bool y = false, bool z = true )
            {
                ( this.x, this.y, this.z ) = ( x, y, z );
            }

            #endregion

        }
    }

    [Serializable]
    public class SnapManager
    {

        #region Statics and Constants

        #endregion

        #region Serialized

        [SerializeField] private SnapSettings _settings = settings;

        #endregion

        #region Public Properties

        public static SnapSettings settings { get; } = new SnapSettings();

        #endregion

        #region Public Methods

        public static void FrameGridOrigin()
        {
            SceneView sceneView = (SceneView)SceneView.sceneViews[ 0 ];
            if ( sceneView == null )
            {
                return;
            }

            Vector3 viewportPoint = sceneView.camera.WorldToViewportPoint( settings.origin );
            bool originOnScreen = viewportPoint.x    > 0
                                  && viewportPoint.y > 0
                                  && viewportPoint.x < 1
                                  && viewportPoint.y < 1;
            if ( originOnScreen )
            {
                return;
            }

            GameObject activeGO = Selection.activeGameObject;
            GameObject tempGO   = new GameObject();
            tempGO.transform.position = settings.origin;
            Selection.activeObject    = tempGO;
            SceneView.FrameLastActiveSceneView();
            Selection.activeGameObject = activeGO;
            Object.DestroyImmediate( tempGO );
        }

        public static void ToggleGridPositionHandle()
        {
            if ( !settings.lockedGrid )
            {
                settings.lockedGrid = true;
            }

            settings.showPositionHandle = !settings.showPositionHandle;
            SnapSettingsWindow.RepaintWindow();
        }

        public static void ToggleGridRotationHandle()
        {
            if ( !settings.lockedGrid )
            {
                settings.lockedGrid = true;
            }

            settings.showRotationHandle = !settings.showRotationHandle;
            SnapSettingsWindow.RepaintWindow();
        }

        public static void ToggleGridScaleHandle()
        {
            if ( !settings.lockedGrid )
            {
                settings.lockedGrid = true;
            }

            settings.showScaleHandle = !settings.showScaleHandle;
            SnapSettingsWindow.RepaintWindow();
        }

        #endregion

    }

    #endregion

    #region PWBIO

    public static partial class PWBIO
    {

        #region Statics and Constants

        private static bool _snappedToVertex;

        #endregion

        #region Public Properties

        public static bool gridShorcutEnabled { get; private set; }

        #endregion

        #region Private Methods

        private static void DrawGrid( AxesUtils.Axis axis, Vector3 focusPoint, int maxCells, Vector3 snapSize )
        {
            Quaternion rotation = SnapManager.settings.rotation;
            Handles.zTest = CompareFunction.Always;
            Vector3 focusOffset = Quaternion.Inverse( SnapManager.settings.rotation ) * ( focusPoint - SnapManager.settings.origin );
            Vector3Int focusOffsetInt = new Vector3Int( Mathf.RoundToInt( focusOffset.x / snapSize.x ),
                Mathf.RoundToInt( focusOffset.y                                         / snapSize.y ), Mathf.RoundToInt( focusOffset.z / snapSize.z ) );

            float GetAlpha( float cell, int majorLinesGap )
            {
                return cell % majorLinesGap == 0 ? 0.5f : 0.2f;
            }

            for ( int i = 0; i < maxCells; ++i )
            {
                for ( int j = 1; j < maxCells; ++j )
                {
                    Vector3 p1 = Vector3.zero;
                    Vector3 p2 = Vector3.zero;
                    Vector3 p3 = Vector3.zero;
                    Vector3 p4 = Vector3.zero;

                    float alpha1  = ( maxCells - Mathf.Max( i, j - 1 ) ) / (float)maxCells;
                    float alpha2  = alpha1;
                    float alpha3  = alpha1;
                    float alpha4  = alpha1;
                    float alpha1R = alpha1;
                    float alpha2R = alpha1;
                    float alpha4R = alpha1;
                    float alpha3R = alpha1;

                    Color color = new Color( 0.5f, 1f, 0.5f, 0f );
                    switch ( axis )
                    {
                        case AxesUtils.Axis.X:
                            color   =  new Color( 1f, 0.5f, 0.5f, 0f );
                            alpha1  *= GetAlpha( i + focusOffsetInt.y, SnapManager.settings.majorLinesGap.y );
                            alpha2  *= GetAlpha( i - focusOffsetInt.y, SnapManager.settings.majorLinesGap.y );
                            alpha3  *= GetAlpha( i + focusOffsetInt.z, SnapManager.settings.majorLinesGap.z );
                            alpha4  *= GetAlpha( i - focusOffsetInt.z, SnapManager.settings.majorLinesGap.z );
                            alpha1R =  alpha1;
                            alpha2R =  alpha2;
                            alpha3R =  alpha4;
                            alpha4R =  alpha3;
                            p1      += rotation * Vector3.Scale( new Vector3( 0f, i,     j - 1 ), snapSize );
                            p2      += rotation * Vector3.Scale( new Vector3( 0f, i,     j ),     snapSize );
                            p3      += rotation * Vector3.Scale( new Vector3( 0f, j - 1, i ),     snapSize );
                            p4      += rotation * Vector3.Scale( new Vector3( 0f, j,     i ),     snapSize );
                            break;
                        case AxesUtils.Axis.Y:
                            alpha1  *= GetAlpha( i + focusOffsetInt.x, SnapManager.settings.majorLinesGap.x );
                            alpha2  *= GetAlpha( i - focusOffsetInt.x, SnapManager.settings.majorLinesGap.x );
                            alpha3  *= GetAlpha( i + focusOffsetInt.z, SnapManager.settings.majorLinesGap.z );
                            alpha4  *= GetAlpha( i - focusOffsetInt.z, SnapManager.settings.majorLinesGap.z );
                            alpha1R =  alpha2;
                            alpha2R =  alpha1;
                            alpha3R =  alpha3;
                            alpha4R =  alpha4;
                            p1      += rotation * Vector3.Scale( new Vector3( i,     0f, j - 1 ), snapSize );
                            p2      += rotation * Vector3.Scale( new Vector3( i,     0f, j ),     snapSize );
                            p3      += rotation * Vector3.Scale( new Vector3( j - 1, 0f, i ),     snapSize );
                            p4      += rotation * Vector3.Scale( new Vector3( j,     0f, i ),     snapSize );
                            break;
                        case AxesUtils.Axis.Z:
                            color   =  new Color( 0.5f, 0.5f, 1f, 0f );
                            alpha1  *= GetAlpha( i + focusOffsetInt.x, SnapManager.settings.majorLinesGap.x );
                            alpha2  *= GetAlpha( i - focusOffsetInt.x, SnapManager.settings.majorLinesGap.x );
                            alpha3  *= GetAlpha( i + focusOffsetInt.y, SnapManager.settings.majorLinesGap.y );
                            alpha4  *= GetAlpha( i - focusOffsetInt.y, SnapManager.settings.majorLinesGap.y );
                            alpha1R =  alpha1;
                            alpha2R =  alpha2;
                            alpha3R =  alpha4;
                            alpha4R =  alpha3;
                            p1      += rotation * Vector3.Scale( new Vector3( i,     j - 1, 0f ), snapSize );
                            p2      += rotation * Vector3.Scale( new Vector3( i,     j,     0f ), snapSize );
                            p3      += rotation * Vector3.Scale( new Vector3( j - 1, i,     0f ), snapSize );
                            p4      += rotation * Vector3.Scale( new Vector3( j,     i,     0f ), snapSize );
                            break;
                    }

                    Handles.color = color + new Color( 0f, 0f, 0f, alpha1 );
                    Handles.DrawLine( focusPoint + p1, focusPoint + p2 );
                    Handles.color = color + new Color( 0f, 0f, 0f, alpha2 );
                    Handles.DrawLine( focusPoint - p1, focusPoint - p2 );
                    Handles.color = color + new Color( 0f, 0f, 0f, alpha3 );
                    Handles.DrawLine( focusPoint + p3, focusPoint + p4 );
                    Handles.color = color + new Color( 0f, 0f, 0f, alpha4 );
                    Handles.DrawLine( focusPoint - p3, focusPoint - p4 );
                    if ( i == 0 )
                    {
                        continue;
                    }

                    Quaternion r180 = Quaternion.AngleAxis( 180, rotation
                                                                 * ( axis == AxesUtils.Axis.X ? Vector3.up :
                                                                     axis == AxesUtils.Axis.Y ? Vector3.forward : Vector3.right ) );
                    Handles.color = color + new Color( 0f, 0f, 0f, alpha1R );
                    Handles.DrawLine( focusPoint + r180 * p1, focusPoint + r180 * p2 );
                    Handles.color = color + new Color( 0f, 0f, 0f, alpha2R );
                    Handles.DrawLine( focusPoint - r180 * p1, focusPoint - r180 * p2 );
                    Handles.color = color + new Color( 0f, 0f, 0f, alpha3R );
                    Handles.DrawLine( focusPoint + r180 * p3, focusPoint + r180 * p4 );
                    Handles.color = color + new Color( 0f, 0f, 0f, alpha4R );
                    Handles.DrawLine( focusPoint - r180 * p3, focusPoint - r180 * p4 );
                }
            }
        }

        private static void DrawGridCricle( Vector3 center,  Vector3 normal,
                                            Vector3 tangent, Vector3 bitangent, float radius )
        {
            Handles.zTest = CompareFunction.Always;
            const float polygonSideSize = 0.3f;
            const int   minPolygonSides = 12;
            const int   maxPolygonSides = 60;
            int         polygonSides    = Mathf.Clamp( (int)( TAU * radius / polygonSideSize ), minPolygonSides, maxPolygonSides );

            List<Vector3> periPoints = new List<Vector3>();
            for ( int i = 0; i < polygonSides; ++i )
            {
                float   radians    = TAU * i / ( polygonSides - 1f );
                Vector2 tangentDir = new Vector2( Mathf.Cos( radians ), Mathf.Sin( radians ) );
                Vector3 worldDir   = TangentSpaceToWorld( tangent, bitangent, tangentDir );
                Vector3 periPoint  = center + worldDir * radius;
                periPoints.Add( periPoint );
            }

            Handles.DrawAAPolyLine( 4 * Handles.color.a, periPoints.ToArray() );
        }

        private static void DrawRadialGrid( AxesUtils.Axis axis, SceneView sceneView, int maxCells, float snapSize )
        {
            Quaternion       rotation  = SnapManager.settings.rotation;
            AxesUtils.Axis[] otherAxes = AxesUtils.GetOtherAxes( axis );
            Vector3          normal    = rotation * AxesUtils.GetVector( 1, axis );
            Vector3          tangent   = rotation * AxesUtils.GetVector( 1, otherAxes[ 0 ] );
            Vector3          bitangent = rotation * AxesUtils.GetVector( 1, otherAxes[ 1 ] );
            float            radius    = 0f;
            for ( int i = 1; i < maxCells; ++i )
            {
                radius += snapSize;
                float alpha = ( maxCells - i ) * 0.5f / maxCells;
                switch ( axis )
                {
                    case AxesUtils.Axis.X:
                        Handles.color = new Color( 1f, 0.5f, 0.5f, alpha );
                        break;
                    case AxesUtils.Axis.Y:
                        Handles.color = new Color( 0.5f, 1f, 0.5f, alpha );
                        break;
                    case AxesUtils.Axis.Z:
                        Handles.color = new Color( 0.5f, 0.5f, 1f, alpha );
                        break;
                }

                DrawGridCricle( SnapManager.settings.origin, normal, tangent, bitangent, radius );

                for ( int j = 0; j < SnapManager.settings.radialSectors; ++j )
                {
                    float   radians    = TAU * j / SnapManager.settings.radialSectors;
                    Vector2 tangentDir = new Vector2( Mathf.Cos( radians ), Mathf.Sin( radians ) );
                    Vector3 worldDir   = TangentSpaceToWorld( tangent, bitangent, tangentDir );
                    Vector3[] points = new[]
                    {
                        SnapManager.settings.origin + worldDir * ( radius - snapSize ),
                        SnapManager.settings.origin + worldDir * radius,
                    };
                    Handles.DrawAAPolyLine( 1, points );
                }
            }
        }

        private static int GetMaxCells( AxesUtils.Axis axis, Vector3 focusPoint, SceneView sceneView,
                                        out Vector3    snapSize )
        {
            snapSize = SnapManager.settings.radialGridEnabled
                ? Vector3.one * SnapManager.settings.radialStep
                : SnapManager.settings.step;
            Quaternion rotation = SnapManager.settings.rotation;

            float guiDistance = ( HandleUtility.WorldToGUIPoint( focusPoint )
                                  - HandleUtility.WorldToGUIPoint( focusPoint + rotation * snapSize ) ).magnitude;

            const int minGuidistance = 30;
            if ( guiDistance < minGuidistance )
            {
                snapSize *= Mathf.Round( minGuidistance / guiDistance );
            }

            int maxCells = 10;

            Vector3 halfSize = new Vector3(
                axis == AxesUtils.Axis.X ? 0f : maxCells * snapSize.x,
                axis == AxesUtils.Axis.Y ? 0f : maxCells * snapSize.y,
                axis == AxesUtils.Axis.Z ? 0f : maxCells * snapSize.z );

            Vector3 axis1Vector = rotation
                                  * ( axis   == AxesUtils.Axis.X ? Vector3.forward
                                      : axis == AxesUtils.Axis.Y ? Vector3.right : Vector3.up );
            Vector3 axis2Vector = rotation
                                  * ( axis   == AxesUtils.Axis.X ? Vector3.up
                                      : axis == AxesUtils.Axis.Y ? Vector3.forward : Vector3.right );

            Vector2[] gridAxes = new[]
            {
                HandleUtility.WorldToGUIPoint( focusPoint - Vector3.Scale( halfSize, axis1Vector ) ),
                HandleUtility.WorldToGUIPoint( focusPoint + Vector3.Scale( halfSize, axis1Vector ) ),
                HandleUtility.WorldToGUIPoint( focusPoint - Vector3.Scale( halfSize, axis2Vector ) ),
                HandleUtility.WorldToGUIPoint( focusPoint + Vector3.Scale( halfSize, axis2Vector ) ),
            };

            Vector2 gridMax = new Vector2(
                Mathf.Max( gridAxes[ 0 ].x, gridAxes[ 1 ].x, gridAxes[ 2 ].x, gridAxes[ 3 ].x ),
                Mathf.Max( gridAxes[ 0 ].y, gridAxes[ 1 ].y, gridAxes[ 2 ].y, gridAxes[ 3 ].y ) );
            Vector2 gridMin = new Vector2(
                Mathf.Min( gridAxes[ 0 ].x, gridAxes[ 1 ].x, gridAxes[ 2 ].x, gridAxes[ 3 ].x ),
                Mathf.Min( gridAxes[ 0 ].y, gridAxes[ 1 ].y, gridAxes[ 2 ].y, gridAxes[ 3 ].y ) );

            Vector2 gridSizeOnGUI = gridMax                 - gridMin;
            Vector2 diff          = sceneView.position.size - gridSizeOnGUI;

            if ( diff.x    > 0
                 || diff.y > 0 )
            {
                float maxRatio = float.MinValue;
                if ( diff.x > 0 )
                {
                    maxRatio = sceneView.position.size.x / gridSizeOnGUI.x;
                }

                if ( diff.y > 0 )
                {
                    float ratio = sceneView.position.size.y / gridSizeOnGUI.y;
                    if ( ratio > maxRatio )
                    {
                        maxRatio = ratio;
                    }
                }

                maxCells = Mathf.CeilToInt( maxCells * maxRatio );
                if ( maxCells > 30 )
                {
                    int maxCellsRatio = Mathf.CeilToInt( maxCells / 30f );
                    snapSize = snapSize * maxCellsRatio;
                    maxCells = 30;
                }
            }

            return maxCells;
        }

        private static void GridDuringSceneGui( SceneView sceneView )
        {
            if ( PWBSettings.shortcuts.gridEnableShortcuts.Check() )
            {
                if ( !gridShorcutEnabled )
                {
                    gridShorcutEnabled = true;
                    Event.current.Use();
                }
            }

            if ( gridShorcutEnabled )
            {
                void MoveGridOrigin( AxesUtils.SignedAxis forwardAxis )
                {
                    Vector3 fw = SnapManager.settings.rotation * forwardAxis;
                    float stepSize = SnapManager.settings.radialGridEnabled
                        ? SnapManager.settings.radialStep
                        : AxesUtils.GetAxisValue( SnapManager.settings.step, forwardAxis );
                    SnapManager.settings.origin += fw * stepSize;
                    gridShorcutEnabled          =  false;
                }

                if ( PWBSettings.shortcuts.gridToggle.Check() )
                {
                    SnapManager.settings.visibleGrid = !SnapManager.settings.visibleGrid;
                    gridShorcutEnabled               = false;
                }
                else if ( PWBSettings.shortcuts.gridToggleSnaping.Check() )
                {
                    SnapManager.settings.snappingEnabled = !SnapManager.settings.snappingEnabled;
                    gridShorcutEnabled                   = false;
                }
                else if ( PWBSettings.shortcuts.gridToggleLock.Check() )
                {
                    SnapManager.settings.lockedGrid = !SnapManager.settings.lockedGrid;
                    gridShorcutEnabled              = false;
                }
                else if ( PWBSettings.shortcuts.gridSetOriginPosition.Check()
                          && Selection.activeTransform != null )
                {
                    SnapManager.settings.origin             = Selection.activeTransform.position;
                    SnapManager.settings.showPositionHandle = true;
                    gridShorcutEnabled                      = false;
                }
                else if ( PWBSettings.shortcuts.gridSetOriginRotation.Check()
                          && Selection.activeTransform != null )
                {
                    SnapManager.settings.rotation           = Selection.activeTransform.rotation;
                    SnapManager.settings.showRotationHandle = true;
                    gridShorcutEnabled                      = false;
                }
                else if ( PWBSettings.shortcuts.gridSetSize.Check()
                          && Selection.activeTransform != null )
                {
                    SnapManager.settings.step = BoundsUtils.GetBounds( Selection.activeTransform,
                        Selection.activeTransform.rotation ).size;
                    SnapManager.settings.showScaleHandle = true;
                    gridShorcutEnabled                   = false;
                }
                else if ( PWBSettings.shortcuts.gridFrameOrigin.Check() )
                {
                    SnapManager.FrameGridOrigin();
                    gridShorcutEnabled = false;
                }
                else if ( PWBSettings.shortcuts.gridTogglePositionHandle.Check() )
                {
                    SnapManager.ToggleGridPositionHandle();
                    gridShorcutEnabled = false;
                }
                else if ( PWBSettings.shortcuts.gridToggleRotationHandle.Check() )
                {
                    SnapManager.ToggleGridRotationHandle();
                    gridShorcutEnabled = false;
                }
                else if ( PWBSettings.shortcuts.gridToggleSpacingHandle.Check() )
                {
                    SnapManager.ToggleGridScaleHandle();
                    gridShorcutEnabled = false;
                }
                else if ( PWBSettings.shortcuts.gridMoveOriginUp.Check() )
                {
                    MoveGridOrigin( AxesUtils.SignedAxis.UP );
                }
                else if ( PWBSettings.shortcuts.gridMoveOriginDown.Check() )
                {
                    MoveGridOrigin( AxesUtils.SignedAxis.DOWN );
                }
            }

            if ( !SnapManager.settings.visibleGrid )
            {
                return;
            }

            Vector3    originOffset = SnapManager.settings.origin;
            Quaternion rotation     = SnapManager.settings.rotation;
            AxesUtils.Axis axis = SnapManager.settings.gridOnX ? AxesUtils.Axis.X
                : SnapManager.settings.gridOnY                 ? AxesUtils.Axis.Y : AxesUtils.Axis.Z;
            Ray camRay = new Ray( sceneView.camera.transform.position, sceneView.camera.transform.forward );
            Plane plane = new Plane( rotation
                                     * ( axis   == AxesUtils.Axis.X ? Vector3.right
                                         : axis == AxesUtils.Axis.Y ? Vector3.up : Vector3.forward ), originOffset );
            Vector3 focusPoint;
            if ( plane.Raycast( camRay, out float distance ) )
            {
                focusPoint = camRay.GetPoint( distance );
            }
            else
            {
                return;
            }

            Vector3 snapSize       = SnapManager.settings.step;
            int     maxCells       = GetMaxCells( axis, focusPoint, sceneView, out snapSize );
            float   snapStepFactor = snapSize.x / SnapManager.settings.step.x;
            focusPoint = SnapPosition( focusPoint, SnapManager.settings.snappingEnabled, false, snapStepFactor, true );
            GridHandles();
            if ( SnapManager.settings.radialGridEnabled )
            {
                DrawRadialGrid( axis, sceneView, maxCells, snapSize.x );
            }
            else
            {
                DrawGrid( axis, focusPoint, maxCells, snapSize );
            }
        }

        private static void GridHandles()
        {
            if ( !SnapManager.settings.lockedGrid )
            {
                return;
            }

            Vector3    originOffset = SnapManager.settings.origin;
            Quaternion rotation     = SnapManager.settings.rotation;
            Vector3    snapSize     = SnapManager.settings.step;
            Handles.zTest = CompareFunction.Always;
            float handleSize = HandleUtility.GetHandleSize( originOffset );

            void DrawSnapGizmos( AxesUtils.Axis forwardAxis, AxesUtils.Axis upwardAxis )
            {
                Vector3 fw       = rotation   * AxesUtils.GetVector( 1, forwardAxis );
                Vector3 uw       = rotation   * AxesUtils.GetVector( 1, upwardAxis );
                float   coneSize = handleSize * 0.15f;
                float stepSize = SnapManager.settings.radialGridEnabled
                    ? SnapManager.settings.radialStep
                    : AxesUtils.GetAxisValue( snapSize, forwardAxis );
                Vector3 conePosFw       = originOffset + fw * ( handleSize * 1.6f );
                Vector3 originScreenPos = _sceneViewCamera.WorldToScreenPoint( SnapManager.settings.origin );
                Vector3 fwScreenPos     = _sceneViewCamera.WorldToScreenPoint( conePosFw );
                float   alpha           = Mathf.Clamp01( ( fwScreenPos - originScreenPos ).magnitude / 90 - 0.5f );

                int   controlId     = GUIUtility.GetControlID( FocusType.Passive );
                float distFromMouse = HandleUtility.DistanceToCircle( conePosFw, coneSize / 2 );
                HandleUtility.AddControl( controlId, distFromMouse );
                bool mouseOver = HandleUtility.nearestControl == controlId;

                Handles.color = new Color( 1f, 1f, mouseOver ? 1 : 0, alpha );
                Handles.ConeHandleCap( controlId, conePosFw,
                    Quaternion.LookRotation( fw, uw ), coneSize, EventType.Repaint );
                if ( Event.current.button  == 0
                     && Event.current.type == EventType.MouseDown
                     && mouseOver )
                {
                    SnapManager.settings.origin += fw * stepSize;
                }

                Vector3 conePosBw = originOffset + fw * ( handleSize * 1.3f );
                controlId     = GUIUtility.GetControlID( FocusType.Passive );
                distFromMouse = HandleUtility.DistanceToCircle( conePosBw, coneSize / 2 );
                HandleUtility.AddControl( controlId, distFromMouse );
                mouseOver = HandleUtility.nearestControl == controlId;

                Handles.color = new Color( 1f, 1f, mouseOver ? 1 : 0, alpha );
                Handles.ConeHandleCap( controlId, conePosBw,
                    Quaternion.LookRotation( -fw, -uw ), coneSize, EventType.Repaint );
                if ( Event.current.button  == 0
                     && Event.current.type == EventType.MouseDown
                     && mouseOver )
                {
                    SnapManager.settings.origin -= fw * stepSize;
                }
            }

            if ( SnapManager.settings.showPositionHandle )
            {
                SnapManager.settings.origin = Handles.PositionHandle( originOffset, rotation );
                Handles.zTest               = CompareFunction.LessEqual;
                Handles.color               = Color.yellow;
                Handles.SphereHandleCap( 0, originOffset, rotation,
                    HandleUtility.GetHandleSize( originOffset ) * 0.2f, EventType.Repaint );
                Handles.zTest = CompareFunction.Always;
                DrawSnapGizmos( AxesUtils.Axis.X, AxesUtils.Axis.Y );
                DrawSnapGizmos( AxesUtils.Axis.Y, AxesUtils.Axis.Z );
                DrawSnapGizmos( AxesUtils.Axis.Z, AxesUtils.Axis.X );
            }
            else if ( SnapManager.settings.showRotationHandle )
            {
                SnapManager.settings.rotation = Handles.RotationHandle( rotation, originOffset );
            }
            else if ( SnapManager.settings.showScaleHandle )
            {
                if ( SnapManager.settings.radialGridEnabled )
                {
                    Vector3 step0 = Vector3.one * SnapManager.settings.radialStep;
                    Vector3 step = Handles.ScaleHandle( step0, originOffset,
                        rotation, handleSize );
                    if ( step0 != step )
                    {
                        if ( step0.x != step.x )
                        {
                            SnapManager.settings.radialStep = step.x;
                        }
                        else if ( step0.y != step.y )
                        {
                            SnapManager.settings.radialStep = step.y;
                        }
                        else
                        {
                            SnapManager.settings.radialStep = step.z;
                        }
                    }
                }
                else
                {
                    SnapManager.settings.step = Handles.ScaleHandle( SnapManager.settings.step,
                        originOffset, rotation, handleSize );
                }
            }

            if ( SnapManager.settings.origin      != originOffset
                 || SnapManager.settings.rotation != rotation
                 || SnapManager.settings.step     != snapSize )
            {
                SnapSettingsWindow.RepaintWindow();
            }
        }

        private static bool GridRaycast( Ray ray, out RaycastHit hitInfo )
        {
            hitInfo = new RaycastHit();
            Plane plane = new Plane( SnapManager.settings.rotation
                                     * ( SnapManager.settings.gridOnX   ? Vector3.right
                                         : SnapManager.settings.gridOnY ? Vector3.up : Vector3.forward ), SnapManager.settings.origin );
            if ( Vector3.Cross( ray.direction, plane.normal ).magnitude < 0.000001 )
            {
                plane = new Plane( ray.direction, SnapManager.settings.origin );
            }

            if ( plane.Raycast( ray, out float distance ) )
            {
                hitInfo.normal = plane.normal;
                hitInfo.point  = ray.GetPoint( distance );
                return true;
            }

            return false;
        }

        private static Vector3 SnapAndUpdateGridOrigin( Vector3 point,                 bool snapToGrid,
                                                        bool    paintOnPalettePrefabs, bool paintOnMeshesWithoutCollider, bool paintOnTheGrid,
                                                        Vector3 projectionDirection )
        {
            if ( snapToGrid )
            {
                point = SnapPosition( point, paintOnTheGrid, true );
                Vector3 direction = SnapManager.settings.TransformToGridDirection( SnapManager.settings.rotation
                                                                                   * projectionDirection );
                if ( !paintOnTheGrid
                     && !SnapManager.settings.IsSnappingEnabledInThisDirection( direction ) )
                {
                    Ray ray = new Ray( point - direction, direction );
                    if ( MouseRaycast( ray,        out RaycastHit hit, out GameObject collider, float.MaxValue, -1,
                            paintOnPalettePrefabs, paintOnMeshesWithoutCollider ) )
                    {
                        point = hit.point;
                    }
                }
            }

            UpdateGridOrigin( point );
            return point;
        }

        private static Vector3 SnapPosition( Vector3 position,            bool onGrid, bool applySettings,
                                             float   snapStepFactor = 1f, bool ignoreMidpoints = false )
        {
            Vector3 result = position;
            if ( SnapManager.settings.radialGridEnabled )
            {
                Quaternion rotation = SnapManager.settings.rotation;
                if ( SnapManager.settings.gridOnX )
                {
                    rotation *= Quaternion.AngleAxis( -90, Vector3.forward );
                }
                else if ( SnapManager.settings.gridOnZ )
                {
                    rotation *= Quaternion.AngleAxis( -90, Vector3.right );
                }

                Vector3 localPosition     = Quaternion.Inverse( rotation ) * ( position - SnapManager.settings.origin );
                Vector3 snappedDirOnPlane = new Vector3( localPosition.x, 0, localPosition.z ).normalized;
                if ( SnapManager.settings.snapToRadius )
                {
                    float sectorAngleRad  = TAU / SnapManager.settings.radialSectors;
                    float angleRad        = Mathf.Atan2( localPosition.z, localPosition.x );
                    float snappedAngleRad = Mathf.Round( angleRad / sectorAngleRad ) * sectorAngleRad;
                    snappedDirOnPlane = new Vector3( Mathf.Cos( snappedAngleRad ), 0, Mathf.Sin( snappedAngleRad ) );
                    float sizeOnplane = Mathf.Sqrt( localPosition.x   * localPosition.x
                                                    + localPosition.z * localPosition.z );
                    Vector3 snappedOnPlane      = snappedDirOnPlane * sizeOnplane;
                    Vector3 localSnapedPosition = new Vector3( snappedOnPlane.x, localPosition.y, snappedOnPlane.z );
                    result = rotation * localSnapedPosition + SnapManager.settings.origin;
                }

                if ( SnapManager.settings.snapToCircunference )
                {
                    float sizeOnplane = Mathf.Sqrt( localPosition.x   * localPosition.x
                                                    + localPosition.z * localPosition.z );
                    float sizeOnPlaneSnapped = Mathf.Round( sizeOnplane / SnapManager.settings.radialStep )
                                               * SnapManager.settings.radialStep;
                    Vector3 localSnapedPosition = snappedDirOnPlane * sizeOnPlaneSnapped
                                                  + new Vector3( 0, localPosition.y, 0 );
                    result = rotation * localSnapedPosition + SnapManager.settings.origin;
                }
            }
            else
            {
                Vector3 localPosition = Quaternion.Inverse( SnapManager.settings.rotation )
                                        * ( position - SnapManager.settings.origin );

                float Snap( float step, float value )
                {
                    if ( !ignoreMidpoints
                         && SnapManager.settings.midpointSnapping )
                    {
                        step *= 0.5f;
                    }

                    return Mathf.Round( value / step ) * step;
                }

                Vector3 localSnappedPosition = new Vector3(
                    Snap( SnapManager.settings.step.x * snapStepFactor, localPosition.x ),
                    Snap( SnapManager.settings.step.y * snapStepFactor, localPosition.y ),
                    Snap( SnapManager.settings.step.z * snapStepFactor, localPosition.z ) );
                result = SnapManager.settings.rotation
                         * ( applySettings
                             ? new Vector3(
                                 SnapManager.settings.snappingOnX ? localSnappedPosition.x : onGrid ? 0 : localPosition.x,
                                 SnapManager.settings.snappingOnY ? localSnappedPosition.y : onGrid ? 0 : localPosition.y,
                                 SnapManager.settings.snappingOnZ ? localSnappedPosition.z : onGrid ? 0 : localPosition.z )
                             : localSnappedPosition )
                         + SnapManager.settings.origin;
            }

            return result;
        }

        private static bool SnapToVertex( Ray  ray,      out RaycastHit closestVertexInfo,
                                          bool in2DMode, GameObject[]   selection = null )
        {
            Vector2 origin2D        = ray.origin;
            bool    snappedToVertex = false;

            float        radius          = 1f;
            RaycastHit[] hitArray        = null;
            Collider2D[] collider2DArray = null;
            do
            {
                if ( selection == null )
                {
                    hitArray = new RaycastHit[ 0 ];
                    if ( Physics.SphereCast( ray, radius, out RaycastHit hitInfo ) )
                    {
                        hitArray = new[] { hitInfo };
                    }
                }
                else
                {
                    hitArray = Physics.SphereCastAll( ray, radius );
                    if ( hitArray.Length > 0 )
                    {
                        List<RaycastHit> filtered = new List<RaycastHit>();
                        foreach ( RaycastHit hit in hitArray )
                        {
                            GameObject colliderObj = hit.collider.gameObject;
                            int        hitID       = colliderObj.GetInstanceID();
                            if ( PWBCore.IsTempCollider( hitID ) )
                            {
                                colliderObj = PWBCore.GetGameObjectFromTempColliderId( hitID );
                                hitID       = colliderObj.GetInstanceID();
                            }

                            foreach ( GameObject filter in selection )
                            {
                                if ( hitID == filter.GetInstanceID() )
                                {
                                    filtered.Add( hit );
                                }
                            }
                        }

                        hitArray = filtered.ToArray();
                    }
                }

                if ( hitArray.Length > 0 )
                {
                    List<RaycastHit> filtered = new List<RaycastHit>();
                    foreach ( RaycastHit hit in hitArray )
                    {
                        GameObject obj = hit.collider.gameObject;
                        if ( PWBCore.IsTempCollider( obj.GetInstanceID() ) )
                        {
                            obj = PWBCore.GetGameObjectFromTempColliderId( obj.GetInstanceID() );
                        }

                        if ( IsVisible( ref obj ) )
                        {
                            filtered.Add( hit );
                        }
                    }

                    hitArray = filtered.ToArray();
                    if ( hitArray.Length > 0 )
                    {
                        break;
                    }
                }

                if ( in2DMode )
                {
                    collider2DArray = Physics2D.OverlapCircleAll( origin2D, radius );
                    List<Collider2D> filtered = new List<Collider2D>();
                    foreach ( Collider2D collider in collider2DArray )
                    {
                        GameObject colliderObj = collider.gameObject;
                        int        hitID       = colliderObj.GetInstanceID();
                        if ( PWBCore.IsTempCollider( hitID ) )
                        {
                            colliderObj = PWBCore.GetGameObjectFromTempColliderId( hitID );
                            hitID       = colliderObj.GetInstanceID();
                        }

                        foreach ( GameObject filter in selection )
                        {
                            if ( hitID == filter.GetInstanceID() )
                            {
                                filtered.Add( collider );
                            }
                        }
                    }

                    collider2DArray = filtered.ToArray();
                    if ( collider2DArray.Length > 0 )
                    {
                        break;
                    }
                }

                radius *= 2;
            }
            while ( radius <= 1024f );

            if ( hitArray.Length > 0 )
            {
                float      minDist         = float.MaxValue;
                GameObject closestObj      = null;
                Vector3    closestHitPoint = Vector3.zero;
                foreach ( RaycastHit sphereCastHit in hitArray )
                {
                    if ( sphereCastHit.distance < minDist )
                    {
                        minDist    = sphereCastHit.distance;
                        closestObj = sphereCastHit.collider.gameObject;
                        if ( PWBCore.IsTempCollider( closestObj.GetInstanceID() ) )
                        {
                            closestObj = PWBCore.GetGameObjectFromTempColliderId( closestObj.GetInstanceID() );
                        }
                    }
                }

                if ( DistanceUtils.FindNearestVertexToMouse( out closestVertexInfo, closestObj.transform ) )
                {
                    return true;
                }
            }

            snappedToVertex   = false;
            closestVertexInfo = new RaycastHit();
            if ( in2DMode && collider2DArray.Length > 0 )
            {
                float minSqrDistance = float.MaxValue;
                if ( snappedToVertex )
                {
                    minSqrDistance = ( (Vector2)closestVertexInfo.point - origin2D ).sqrMagnitude;
                }

                foreach ( Collider2D collider in collider2DArray )
                {
                    GameObject obj = collider.gameObject;
                    if ( PWBCore.IsTempCollider( obj.GetInstanceID() ) )
                    {
                        obj = PWBCore.GetGameObjectFromTempColliderId( obj.GetInstanceID() );
                    }

                    if ( DistanceUtils.FindNearestVertexToMouse( out RaycastHit closestVertexInfo2D, obj.transform ) )
                    {
                        float sqrDistance = ( (Vector2)closestVertexInfo2D.point - origin2D ).sqrMagnitude;
                        if ( sqrDistance < minSqrDistance )
                        {
                            minSqrDistance    = sqrDistance;
                            closestVertexInfo = closestVertexInfo2D;
                            snappedToVertex   = true;
                        }
                    }
                }
            }
            #if UNITY_2020_2_OR_NEWER
            if ( !snappedToVertex )
            {
                return DistanceUtils.FindNearestVertexToMouse( out closestVertexInfo );
            }
            #endif
            return snappedToVertex;
        }

        private static void UpdateGridOrigin( Vector3 hitPoint )
        {
            Vector3 snapOrigin = SnapManager.settings.origin;
            if ( !SnapManager.settings.lockedGrid )
            {
                if ( SnapManager.settings.gridOnX )
                {
                    snapOrigin.x = hitPoint.x;
                }
                else if ( SnapManager.settings.gridOnY )
                {
                    snapOrigin.y = hitPoint.y;
                }
                else if ( SnapManager.settings.gridOnZ )
                {
                    snapOrigin.z = hitPoint.z;
                }
            }

            SnapManager.settings.origin = snapOrigin;
        }

        #endregion

    }

    #endregion

}