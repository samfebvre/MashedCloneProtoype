using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace PluginMaster
{

    #region DATA & SETTINGS

    [Serializable]
    public class TerrainFlatteningSettings
    {

        #region Serialized

        [SerializeField] private float _hardness;
        [SerializeField] private float _padding;
        [SerializeField] private bool  _clearTrees   = true;
        [SerializeField] private bool  _clearDetails = true;

        #endregion

        #region Public Properties

        public float angle
        {
            get => _angle;
            set
            {
                if ( _angle == value )
                {
                    return;
                }

                _angle           = value;
                _updateHeightmap = true;
            }
        }

        public bool clearDetails
        {
            get => _clearDetails;
            set
            {
                if ( _clearDetails == value )
                {
                    return;
                }

                _clearDetails = value;
                PWBCore.SetSavePending();
            }
        }

        public bool clearTrees
        {
            get => _clearTrees;
            set
            {
                if ( _clearTrees == value )
                {
                    return;
                }

                _clearTrees = value;
                PWBCore.SetSavePending();
            }
        }

        public Vector2 density
        {
            set
            {
                if ( _density == value )
                {
                    return;
                }

                _density         = value;
                _updateHeightmap = true;
            }
        }

        public float hardness
        {
            get => _hardness;
            set
            {
                if ( _hardness == value )
                {
                    return;
                }

                _hardness        = value;
                _updateHeightmap = true;
                PWBCore.SetSavePending();
            }
        }

        public float[ , ] heightmap
        {
            get
            {
                if ( _updateHeightmap || _heightmap == null )
                {
                    UpdateHeightmap();
                }

                return _heightmap;
            }
        }

        public float padding
        {
            get => _padding;
            set
            {
                value = Mathf.Max( value, 0 );
                if ( _padding == value )
                {
                    return;
                }

                _padding         = value;
                _updateHeightmap = true;
                PWBCore.SetSavePending();
            }
        }

        public Vector2 size
        {
            get => _coreSize;
            set
            {
                if ( _coreSize == value )
                {
                    return;
                }

                _coreSize        = value;
                _updateHeightmap = true;
            }
        }

        #endregion

        #region Public Constructors

        #endregion

        #region Private Fields

        private float      _angle;
        private Vector2    _coreSize = Vector2.one;
        private Vector2    _density  = Vector2.zero;
        private float[ , ] _heightmap;
        private bool       _updateHeightmap = true;

        #endregion

        #region Private Methods

        private void UpdateHeightmap()
        {
            _updateHeightmap = false;
            float cornerSize = ( _coreSize.x + _coreSize.y ) / 2 * ( 1 - _hardness );
            if ( _hardness < 0.8 )
            {
                cornerSize = Mathf.Max( cornerSize, 10f / Mathf.Min( _density.x, _density.y ) );
            }

            Vector2 coreWithPaddingSize = _coreSize           + Vector2.one * _padding * 2;
            Vector2 size                = coreWithPaddingSize + Vector2.one * ( cornerSize * 2 );
            Vector2Int unrotatedSize = new Vector2Int( Mathf.RoundToInt( size.x * _density.x ),
                Mathf.RoundToInt( size.y                                        * _density.y ) );

            Vector2Int coreMapSize = new Vector2Int( Mathf.RoundToInt( coreWithPaddingSize.x * _density.x ),
                Mathf.RoundToInt( coreWithPaddingSize.y                                      * _density.y ) );
            Vector2Int cornerMapSize = new Vector2Int( Mathf.RoundToInt( _density.x * cornerSize ),
                Mathf.RoundToInt( _density.y                                        * cornerSize ) );
            float[ , ] unrotatedMap = new float[ unrotatedSize.x, unrotatedSize.y ];

            Vector2Int minCore  = new Vector2Int( Mathf.Max( cornerMapSize.x - 1, 0 ), Mathf.Max( cornerMapSize.y - 1, 0 ) );
            int        maxCoreI = Mathf.Min( coreMapSize.y + cornerMapSize.y + 1, unrotatedSize.y );
            int        maxCoreJ = Mathf.Min( coreMapSize.x + cornerMapSize.x + 1, unrotatedSize.x );
            for ( int i = minCore.y; i < maxCoreI; ++i )
            for ( int j = minCore.x; j < maxCoreJ; ++j )
            {
                unrotatedMap[ j, i ] = 1f;
            }

            float ParametricBlend( float t )
            {
                if ( t > 1 )
                {
                    return 1;
                }

                if ( t < 0 )
                {
                    return 0;
                }

                float tSquared = t       * t;
                return tSquared / ( 2.0f * ( tSquared - t ) + 1.0f );
            }

            for ( int i = 0; i < cornerMapSize.x; ++i )
            {
                int   i1        = unrotatedSize.x - 1 - i;
                float iDistance = ( cornerMapSize.x - i ) / _density.x;
                for ( int j = 0; j < cornerMapSize.y; ++j )
                {
                    int   j1                                     = unrotatedSize.y - 1 - j;
                    float jDistance                              = ( cornerMapSize.y - j ) / _density.y;
                    float distance                               = 1 - Mathf.Sqrt( iDistance * iDistance + jDistance * jDistance ) / cornerSize;
                    float h                                      = ParametricBlend( distance );
                    unrotatedMap[ i, j ] = unrotatedMap[ i1, j ] = unrotatedMap[ i, j1 ] = unrotatedMap[ i1, j1 ] = h;

                }

                float distanceNorm = 1 - iDistance / cornerSize;
                float iH           = ParametricBlend( distanceNorm );
                for ( int j = minCore.y; j < maxCoreI; ++j )
                {
                    unrotatedMap[ i, j ] = unrotatedMap[ i1, j ] = iH;
                }
            }

            for ( int j = 0; j < cornerMapSize.y; ++j )
            {
                int   j1           = unrotatedSize.y - 1 - j;
                float jDistance    = ( cornerMapSize.y - j ) / _density.y;
                float distanceNorm = 1 - jDistance / cornerSize;
                float jH           = ParametricBlend( distanceNorm );
                for ( int i = minCore.x; i < maxCoreJ; ++i )
                {
                    unrotatedMap[ i, j ] = unrotatedMap[ i, j1 ] = jH;
                }
            }

            if ( _angle == 0 )
            {
                _heightmap = unrotatedMap;
                return;
            }

            float angleRad = _angle * Mathf.Deg2Rad;
            float cos      = Mathf.Cos( angleRad );
            float sin      = Mathf.Sin( angleRad );
            float aspect   = _density.x / _density.y;

            Vector2Int RotatePoint( Vector2 centerToPoint )
            {
                if ( _angle == 0 )
                {
                    return new Vector2Int( Mathf.RoundToInt( centerToPoint.x ), Mathf.RoundToInt( centerToPoint.y ) );
                }

                Vector2Int result = Vector2Int.zero;
                centerToPoint.y = centerToPoint.y * aspect;
                result.x        = Mathf.RoundToInt( centerToPoint.x * cos - centerToPoint.y * sin );
                result.y        = Mathf.RoundToInt( ( centerToPoint.x * sin + centerToPoint.y * cos ) / aspect );
                return result;
            }

            Vector2Int centerToCorner1 = new Vector2Int( Mathf.CeilToInt( unrotatedSize.x / 2f ), Mathf.CeilToInt( unrotatedSize.y / 2f ) );
            Vector2Int rotatedCorner1  = RotatePoint( centerToCorner1 );
            rotatedCorner1 = new Vector2Int( Mathf.Abs( rotatedCorner1.x ), Mathf.Abs( rotatedCorner1.y ) );
            Vector2Int centerToCorner2 = new Vector2Int( -Mathf.CeilToInt( unrotatedSize.x / 2f ),
                Mathf.CeilToInt( unrotatedSize.y / 2f ) );
            Vector2Int rotatedCorner2 = RotatePoint( centerToCorner2 );
            rotatedCorner2 = new Vector2Int( Mathf.Abs( rotatedCorner2.x ), Mathf.Abs( rotatedCorner2.y ) );
            Vector2Int rotatedCorner = Vector2Int.Max( rotatedCorner1, rotatedCorner2 );

            Vector2Int rotationPadding = Vector2Int.Max( rotatedCorner - centerToCorner1, Vector2Int.zero );

            Vector2Int rotatedHeightmapSize = unrotatedSize + rotationPadding * 2;
            _heightmap = new float[ rotatedHeightmapSize.x, rotatedHeightmapSize.y ];

            Vector2Int ClampPoint( Vector2Int point )
            {
                return new Vector2Int( Mathf.Clamp( point.x, 0, rotatedHeightmapSize.x - 1 ),
                    Mathf.Clamp( point.y,                    0, rotatedHeightmapSize.y - 1 ) );
            }

            void SetHeight( Vector2Int point, float value )
            {
                Vector2Int clampPoint = ClampPoint( point );
                _heightmap[ clampPoint.x, clampPoint.y ] = value;
                Vector2Int[] points = new[]
                {
                    ClampPoint( point + Vector2Int.up ), ClampPoint( point   + Vector2Int.down ),
                    ClampPoint( point + Vector2Int.left ), ClampPoint( point + Vector2Int.right ),
                };
                foreach ( Vector2Int p in points )
                {
                    _heightmap[ p.x, p.y ] = _heightmap[ p.x, p.y ] < 0.0001 ? value : ( _heightmap[ p.x, p.y ] * 6 + value ) / 7;
                }
            }

            Vector2Int unrotatedCenter = new Vector2Int( Mathf.FloorToInt( unrotatedSize.x / 2f ),
                Mathf.FloorToInt( unrotatedSize.y                                          / 2f ) );
            Vector2Int center = new Vector2Int( Mathf.FloorToInt( rotatedHeightmapSize.x / 2f ),
                Mathf.FloorToInt( rotatedHeightmapSize.y                                 / 2f ) );
            for ( int i = 0; i < unrotatedSize.y; ++i )
            {
                for ( int j = 0; j < unrotatedSize.x; ++j )
                {
                    float      h             = unrotatedMap[ j, i ];
                    Vector2    point         = new Vector2( j, i );
                    Vector2    centerToPoint = point                        - unrotatedCenter;
                    Vector2Int rotatedPoint  = RotatePoint( centerToPoint ) + center;
                    SetHeight( rotatedPoint, h );
                }
            }

            float[ , ] smoothMap = new float[ rotatedHeightmapSize.x, rotatedHeightmapSize.y ];
            for ( int i = 0; i < rotatedHeightmapSize.x; ++i )
            {
                for ( int j = 0; j < rotatedHeightmapSize.y; ++j )
                {
                    int   count = 0;
                    float sum   = 0f;
                    float[] corners = new[]
                    {
                        i == 0                          || j == 0 ? 0 : _heightmap[ i                                                             - 1, j - 1 ],
                        i == rotatedHeightmapSize.x - 1 || j == 0 ? 0 : _heightmap[ i                                                             + 1, j - 1 ],
                        i == 0                          || j == rotatedHeightmapSize.y - 1 ? 0 : _heightmap[ i                                    - 1, j + 1 ],
                        i == rotatedHeightmapSize.x                                    - 1 || j == rotatedHeightmapSize.y - 1 ? 0 : _heightmap[ i + 1, j + 1 ],
                    };
                    for ( int n = 0; n < 4; ++n )
                    {
                        if ( corners[ n ] < 0.0001 )
                        {
                            continue;
                        }

                        ++count;
                        sum += corners[ n ];
                    }

                    float[] neighbors = new[]
                    {
                        i == 0 ? 0 : _heightmap[ i                          - 1, j ],
                        i == rotatedHeightmapSize.x - 1 ? 0 : _heightmap[ i + 1, j ],
                        j == 0 ? 0 : _heightmap[ i, j                       - 1 ], j == rotatedHeightmapSize.y - 1 ? 0 : _heightmap[ i, j + 1 ],
                    };
                    for ( int n = 0; n < 4; ++n )
                    {
                        if ( neighbors[ n ] < 0.0001 )
                        {
                            continue;
                        }

                        count += 2;
                        sum   += neighbors[ n ] * 2;
                    }

                    if ( count == 0 )
                    {
                        smoothMap[ i, j ] = _heightmap[ i, j ];
                        continue;
                    }

                    if ( !( _heightmap[ i, j ] < 0.0001
                            && ( ( neighbors[ 0 ]    > 0.0001 && neighbors[ 1 ] > 0.0001 )
                                 || ( neighbors[ 2 ] > 0.0001 && neighbors[ 3 ] > 0.0001 ) ) ) )
                    {
                        sum   += _heightmap[ i, j ] * 3;
                        count += 3;
                    }

                    float avg = sum / count;
                    smoothMap[ i, j ] = avg;
                }
            }

            _heightmap = smoothMap;
        }

        #endregion

    }

    [Serializable]
    public class PinSettings : PaintOnSurfaceToolSettings, IPaintToolSettings
    {

        #region Serialized

        [SerializeField] private bool                      _repeat;
        [SerializeField] private TerrainFlatteningSettings _flatteningSettings = new TerrainFlatteningSettings();
        [SerializeField] private bool                      _flattenTerrain;

        [SerializeField] private bool _avoidOverlapping;

        #endregion

        #region Public Properties

        public bool avoidOverlapping
        {
            get => _avoidOverlapping;
            set
            {
                if ( _avoidOverlapping == value )
                {
                    return;
                }

                _avoidOverlapping = value;
                OnDataChanged();
            }
        }

        public TerrainFlatteningSettings flatteningSettings => _flatteningSettings;

        public bool flattenTerrain
        {
            get => _flattenTerrain;
            set
            {
                if ( _flattenTerrain == value )
                {
                    return;
                }

                _flattenTerrain = value;
                PWBCore.SetSavePending();
            }
        }

        public bool repeat
        {
            get => _repeat;
            set
            {
                if ( _repeat == value )
                {
                    return;
                }

                _repeat = value;
                OnDataChanged();
            }
        }

        #endregion

        #region Public Methods

        public override void Copy( IToolSettings other )
        {
            PinSettings otherPinSettings = other as PinSettings;
            if ( otherPinSettings == null )
            {
                return;
            }

            base.Copy( other );
            _repeat = otherPinSettings._repeat;
            _paintTool.Copy( otherPinSettings._paintTool );
            _flattenTerrain = otherPinSettings._flattenTerrain;
        }

        public override void DataChanged()
        {
            base.DataChanged();
            BrushstrokeManager.UpdateBrushstroke();
        }

        #endregion

        #region PAINT TOOL

        [SerializeField] private PaintToolSettings _paintTool = new PaintToolSettings();

        public Transform parent
        {
            get => _paintTool.parent;
            set => _paintTool.parent = value;
        }

        public bool overwritePrefabLayer
        {
            get => _paintTool.overwritePrefabLayer;
            set => _paintTool.overwritePrefabLayer = value;
        }

        public int layer
        {
            get => _paintTool.layer;
            set => _paintTool.layer = value;
        }

        public bool autoCreateParent
        {
            get => _paintTool.autoCreateParent;
            set => _paintTool.autoCreateParent = value;
        }

        public bool setSurfaceAsParent
        {
            get => _paintTool.setSurfaceAsParent;
            set => _paintTool.setSurfaceAsParent = value;
        }

        public bool createSubparentPerPalette
        {
            get => _paintTool.createSubparentPerPalette;
            set => _paintTool.createSubparentPerPalette = value;
        }

        public bool createSubparentPerTool
        {
            get => _paintTool.createSubparentPerTool;
            set => _paintTool.createSubparentPerTool = value;
        }

        public bool createSubparentPerBrush
        {
            get => _paintTool.createSubparentPerBrush;
            set => _paintTool.createSubparentPerBrush = value;
        }

        public bool createSubparentPerPrefab
        {
            get => _paintTool.createSubparentPerPrefab;
            set => _paintTool.createSubparentPerPrefab = value;
        }

        public bool overwriteBrushProperties
        {
            get => _paintTool.overwriteBrushProperties;
            set => _paintTool.overwriteBrushProperties = value;
        }

        public BrushSettings brushSettings => _paintTool.brushSettings;

        public PinSettings()
        {
            _paintTool.OnDataChanged += DataChanged;
        }

        #endregion

    }

    [Serializable]
    public class PinManager : ToolManagerBase<PinSettings>
    {

        #region Statics and Constants

        private static float _rotationSnapValueStatic = 5f;

        #endregion

        #region Serialized

        [SerializeField] private float _rotationSnapValue = _rotationSnapValueStatic;

        #endregion

        #region Public Properties

        public static float rotationSnapValue
        {
            get => _rotationSnapValueStatic;
            set
            {
                if ( _rotationSnapValueStatic == value )
                {
                    return;
                }

                _rotationSnapValueStatic = value;
                PWBCore.staticData.SaveAndUpdateVersion();
            }
        }

        #endregion

        #region Public Methods

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            _rotationSnapValueStatic = _rotationSnapValue;
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            _rotationSnapValue = _rotationSnapValueStatic;
        }

        #endregion

    }

    #endregion

    #region PWBIO

    public static partial class PWBIO
    {

        #region Statics and Constants

        private static bool       _pinned;
        private static Vector3    _pinMouse = Vector3.zero;
        private static RaycastHit _pinHit;
        private static Vector3    _pinAngle         = Vector3.zero;
        private static Vector3    _previousPinAngle = Vector3.zero;
        private static float      _pinScale         = 1f;
        private static Vector3    _pinOffset        = Vector3.zero;

        private static List<List<Vector3>> _initialPinBoundPoints
            = new List<List<Vector3>>();

        private static List<List<Vector3>> _pinBoundPoints
            = new List<List<Vector3>>();

        private static int   _pinBoundPointIdx;
        private static int   _pinBoundLayerIdx;
        private static bool  _snapToVertex;
        private static float _pinDistanceFromSurface;

        private static Vector2 _flatteningSize = Vector2.zero;
        private static bool    _globalFlattening;
        private static Vector3 _flatteningCenter = Vector3.zero;

        private static Transform _pinSurface;

        private static Vector3 _prevPinHitNormal = Vector3.zero;

        #endregion

        #region Public Methods

        public static void ResetPinValues()
        {
            _pinned                 = false;
            _pinMouse               = Vector3.zero;
            _pinHit                 = new RaycastHit();
            _pinAngle               = Vector3.zero;
            _pinScale               = 1f;
            _pinDistanceFromSurface = 0f;
            if ( BrushstrokeManager.brushstroke.Length == 0 )
            {
                return;
            }

            BrushstrokeItem strokeItem = BrushstrokeManager.brushstroke[ 0 ];
            SetPinValues( Quaternion.Euler( strokeItem.additionalAngle ) );
            BrushSettings brushSettings = strokeItem.settings;
            if ( PinManager.settings.overwriteBrushProperties )
            {
                brushSettings = PinManager.settings.brushSettings;
            }

            repaint    = true;
            _pinOffset = _pinBoundPoints[ _pinBoundLayerIdx ][ _pinBoundPointIdx ];
            SceneView.RepaintAll();
        }

        public static void UpdatePinValues()
        {
            BrushstrokeItem strokeItem = BrushstrokeManager.brushstroke[ 0 ];
            GameObject      prefab     = strokeItem.settings.prefab;
            if ( prefab == null )
            {
                return;
            }

            bool isSprite = prefab.GetComponentsInChildren<SpriteRenderer>()
                                  .Where( r => r.enabled && r.sprite != null && r.gameObject.activeSelf ).ToArray().Length
                            > 0;
            Quaternion additionalRotation = Quaternion.Euler( strokeItem.additionalAngle );

            float RoundToStraightAngle( float angle )
            {
                return Mathf.Round( angle / 90f ) * 90f;
            }

            Quaternion fromUpToNormalRotation = Quaternion.FromToRotation( additionalRotation * Vector3.up, _pinHit.normal );

            Vector3 RoundEulerToStraightAngles( Vector3 euler )
            {
                return new Vector3( RoundToStraightAngle( euler.x ), RoundToStraightAngle( euler.y ), RoundToStraightAngle( euler.z ) );
            }

            Vector3 fromUpToNormalEulerRounded = RoundEulerToStraightAngles( fromUpToNormalRotation.eulerAngles );
            fromUpToNormalRotation =  Quaternion.Euler( fromUpToNormalEulerRounded );
            additionalRotation     *= fromUpToNormalRotation;
            SetPinValues( additionalRotation );
            if ( _pinBoundLayerIdx != 1 )
            {
                if ( fromUpToNormalEulerRounded.y == 180 )
                {
                    _pinBoundLayerIdx = 2;
                }
                else
                {
                    _pinBoundLayerIdx = 0;
                }
            }

            int layerIdx = Mathf.Clamp( _pinBoundLayerIdx, 0, _pinBoundPoints.Count             - 1 );
            int pointIdx = Mathf.Clamp( _pinBoundPointIdx, 0, _pinBoundPoints[ layerIdx ].Count - 1 );
            _pinOffset = _pinBoundPoints[ layerIdx ][ pointIdx ];
            UpdatePinScale();
            repaint = true;
            SceneView.RepaintAll();
        }

        #endregion

        #region Private Properties

        private static Vector3 nextBoundLayer
        {
            get
            {
                ++_pinBoundLayerIdx;
                if ( _pinBoundLayerIdx >= _pinBoundPoints.Count )
                {
                    _pinBoundLayerIdx = 0;
                }

                return _pinBoundPoints[ _pinBoundLayerIdx ][ _pinBoundPointIdx ];
            }
        }

        private static Vector3 nextBoundPoint
        {
            get
            {
                ++_pinBoundPointIdx;
                if ( _pinBoundPointIdx >= _pinBoundPoints[ _pinBoundLayerIdx ].Count )
                {
                    _pinBoundPointIdx = 1;
                }

                return _pinBoundPoints[ _pinBoundLayerIdx ][ _pinBoundPointIdx ];
            }
        }

        private static Vector3 pivotBoundPoint
        {
            get
            {
                _pinBoundPointIdx = 0;
                return _pinBoundPoints[ _pinBoundLayerIdx ][ _pinBoundPointIdx ];
            }
        }

        private static Vector3 prevBoundLayer
        {
            get
            {
                --_pinBoundLayerIdx;
                if ( _pinBoundLayerIdx < 0 )
                {
                    _pinBoundLayerIdx = _pinBoundPoints.Count - 1;
                }

                return _pinBoundPoints[ _pinBoundLayerIdx ][ _pinBoundPointIdx ];
            }
        }

        private static Vector3 prevBoundPoint
        {
            get
            {
                --_pinBoundPointIdx;
                if ( _pinBoundPointIdx < 0 )
                {
                    _pinBoundPointIdx = _pinBoundPoints[ _pinBoundLayerIdx ].Count - 1;
                }

                return _pinBoundPoints[ _pinBoundLayerIdx ][ _pinBoundPointIdx ];
            }
        }

        #endregion

        #region Private Methods

        private static void DrawPin( SceneView sceneView, RaycastHit hit,
                                     bool      snapToGrid )
        {
            if ( PaletteManager.selectedBrush == null )
            {
                return;
            }

            PWBCore.UpdateTempCollidersIfHierarchyChanged();
            if ( !_pinned )
            {
                hit.point = SnapAndUpdateGridOrigin( hit.point,                                    snapToGrid,
                    PinManager.settings.paintOnPalettePrefabs,                                     PinManager.settings.paintOnMeshesWithoutCollider,
                    PinManager.settings.mode == PaintOnSurfaceToolSettingsBase.PaintMode.ON_SHAPE, -hit.normal );
                _pinHit = hit;
            }

            PinPreview( sceneView.camera );
        }

        private static void DrawPinHandles( Matrix4x4 rootToWorld, Color color )
        {
            if ( BrushstrokeManager.brushstroke.Length == 0 )
            {
                return;
            }

            BrushstrokeItem strokeItem = BrushstrokeManager.brushstroke[ 0 ];
            GameObject      prefab     = strokeItem.settings.prefab;
            if ( prefab == null )
            {
                return;
            }

            Vector3       pos          = Vector3.zero;
            Vector3       prevPos      = Vector3.zero;
            Vector3       pos0         = Vector3.zero;
            List<Vector3> handlePoints = new List<Vector3>();
            Handles.zTest = CompareFunction.Always;
            if ( _pinBoundPoints.Count == 0 )
            {
                ResetPinValues();
            }

            List<Vector3> flatteningPoints = new List<Vector3>();
            int           layerIdx         = Mathf.Clamp( _pinBoundLayerIdx, 0, _pinBoundPoints.Count - 1 );
            for ( int i = 0; i < _pinBoundPoints[ layerIdx ].Count; ++i )
            {
                prevPos = pos;

                pos = Vector3.Scale( rootToWorld * ( _pinOffset - _pinBoundPoints[ layerIdx ][ i ] ),
                          Vector3.one            / _pinScale )
                      + _pinHit.point;
                if ( i > _pinBoundPoints[ layerIdx ].Count - 5 )
                {
                    if ( i == _pinBoundPoints[ layerIdx ].Count - 4 )
                    {
                        pos0 = pos;
                    }
                    else if ( i < _pinBoundPoints[ layerIdx ].Count )
                    {
                        Handles.color = new Color( 0f, 0f, 0f, 0.7f );
                        Handles.DrawAAPolyLine( 6, prevPos, pos );
                        Handles.color = color;
                        Handles.DrawAAPolyLine( 2, prevPos, pos );
                    }
                }

                flatteningPoints.Add( pos );
                if ( _pinBoundPointIdx == i )
                {
                    continue;
                }

                handlePoints.Add( pos );
            }

            Handles.color = new Color( 0f, 0f, 0f, 0.7f );
            Handles.DrawAAPolyLine( 6, pos, pos0 );
            Handles.color = color;
            Handles.DrawAAPolyLine( 2, pos, pos0 );

            if ( PinManager.settings.flattenTerrain
                 && _pinHit.collider                         != null
                 && _pinHit.collider.GetComponent<Terrain>() != null )
            {
                Vector3    scale    = rootToWorld.lossyScale;
                Quaternion rotation = rootToWorld.rotation;
                Vector3    angle    = rotation.eulerAngles;
                angle.x = Mathf.Abs( Mathf.Round( angle.x ) ) % 360;
                angle.z = Mathf.Abs( Mathf.Round( angle.z ) ) % 360;
                Vector3 p0, p1, p2, p3;
                _globalFlattening = angle.x > 0 || angle.z > 0;
                int n = flatteningPoints.Count;
                _flatteningCenter = flatteningPoints[ n - 5 ];
                if ( _globalFlattening )
                {
                    Bounds bounds = BoundsUtils.GetBoundsRecursive( prefab.transform, rotation, scale );
                    _flatteningSize.x = bounds.size.x;
                    _flatteningSize.y = bounds.size.z;
                    Vector2 halfSize = _flatteningSize / 2;
                    p0 = _flatteningCenter + new Vector3( -halfSize.x, 0, -halfSize.y );
                    p1 = _flatteningCenter + new Vector3( -halfSize.x, 0, halfSize.y );
                    p2 = _flatteningCenter + new Vector3( halfSize.x,  0, halfSize.y );
                    p3 = _flatteningCenter + new Vector3( halfSize.x,  0, -halfSize.y );
                }
                else
                {
                    Vector3 side1_2 = flatteningPoints[ n - 3 ] - flatteningPoints[ n - 4 ];
                    Vector3 side2_3 = flatteningPoints[ n - 2 ] - flatteningPoints[ n - 3 ];
                    Vector3 dir1_2  = side1_2.normalized;
                    Vector3 dir2_3  = side2_3.normalized;
                    p0 = flatteningPoints[ n - 4 ] + ( -dir1_2 - dir2_3 ) * PinManager.settings.flatteningSettings.padding;
                    p1 = flatteningPoints[ n - 3 ] + ( dir1_2  - dir2_3 ) * PinManager.settings.flatteningSettings.padding;
                    p2 = flatteningPoints[ n - 2 ] + ( dir1_2  + dir2_3 ) * PinManager.settings.flatteningSettings.padding;
                    p3 = flatteningPoints[ n - 1 ] + ( -dir1_2 + dir2_3 ) * PinManager.settings.flatteningSettings.padding;
                }

                Handles.color = new Color( 0.5f, 0f, 1f, 0.7f );
                Handles.DrawAAPolyLine( 6, p0, p1, p2, p3, p0 );
                Handles.color = new Color( 0f, 0.5f, 1f, 0.7f );
                Handles.DrawAAPolyLine( 2, p0, p1, p2, p3, p0 );
            }

            foreach ( Vector3 handlePoint in handlePoints )
            {
                Handles.color = new Color( 0f, 0f, 0f, 0.7f );
                Handles.DotHandleCap( 795, handlePoint, Quaternion.identity,
                    HandleUtility.GetHandleSize( pos ) * 0.0325f * PWBCore.staticData.controPointSize,
                    EventType.Repaint );
                Handles.color = Handles.preselectionColor;
                Handles.DotHandleCap( 795, handlePoint, Quaternion.identity,
                    HandleUtility.GetHandleSize( pos ) * 0.02f * PWBCore.staticData.controPointSize,
                    EventType.Repaint );
            }

            Vector3 pinHitPoint = _pinHit.point;
            Handles.color = new Color( 0f, 0f, 0f, 0.7f );
            Handles.DotHandleCap( 418, pinHitPoint, Quaternion.identity,
                HandleUtility.GetHandleSize( pinHitPoint ) * 0.0425f * PWBCore.staticData.controPointSize,
                EventType.Repaint );
            Handles.color = Handles.selectedColor;
            Handles.DotHandleCap( 418, pinHitPoint, Quaternion.identity,
                HandleUtility.GetHandleSize( pinHitPoint ) * 0.03f * PWBCore.staticData.controPointSize,
                EventType.Repaint );
        }

        private static void FlatenTerrain()
        {
            Terrain terrain = _pinHit.collider.GetComponent<Terrain>();
            if ( terrain == null )
            {
                return;
            }

            TerrainData terrainData = terrain.terrainData;

            terrainData.SetTerrainLayersRegisterUndo( terrainData.terrainLayers, "Paint" );
            int resolution = terrainData.heightmapResolution;

            float[ , ] heighMap       = terrainData.GetHeights( 0, 0, resolution, resolution );
            Vector3    transformScale = terrain.transform.localScale;
            terrain.transform.localScale = Vector3.one;
            Vector3 localCenter = terrain.transform.InverseTransformPoint( _flatteningCenter );
            Vector3 localHit    = terrain.transform.InverseTransformPoint( _pinHit.point );
            terrain.transform.localScale = transformScale;

            Vector2 density    = new Vector2( 1                  / terrainData.heightmapScale.x, 1 / terrainData.heightmapScale.z );
            int     mapCenterX = Mathf.RoundToInt( localCenter.x * density.x );
            int     mapCenterZ = Mathf.RoundToInt( localCenter.z * density.y );
            int     mapHitX    = Mathf.RoundToInt( localHit.x    * density.x );
            int     mapHitZ    = Mathf.RoundToInt( localHit.z    * density.y );

            float                     hitHmapVal      = heighMap[ mapHitZ, mapHitX ];
            TerrainFlatteningSettings flattenSettings = PinManager.settings.flatteningSettings;
            flattenSettings.density = density;
            flattenSettings.angle   = _globalFlattening ? 0 : -_pinAngle.y;
            if ( _globalFlattening )
            {
                flattenSettings.size = _flatteningSize;
            }
            else
            {
                PaintStrokeItem paintItem = _paintStroke[ 0 ];
                Vector3         itemSize  = BoundsUtils.GetBoundsRecursive( paintItem.prefab.transform ).size * _pinScale;
                flattenSettings.size = new Vector2( itemSize.x, itemSize.z );
            }

            float[ , ] itemHeighmap  = flattenSettings.heightmap;
            int        itemHeighmapH = itemHeighmap.GetLength( 0 );
            int        itemHeighmapW = itemHeighmap.GetLength( 1 );
            int        itemMinX      = Mathf.Max( itemHeighmapH / 2 - mapCenterX, 0 );
            int        itemMinZ      = Mathf.Max( itemHeighmapW / 2 - mapCenterZ, 0 );
            int        itemMaxX      = itemHeighmapH;
            if ( Mathf.CeilToInt( itemHeighmapH / 2 ) + mapCenterX > resolution )
            {
                itemMaxX -= Mathf.CeilToInt( itemHeighmapH / 2 ) + mapCenterX - resolution + 1;
            }

            int itemMaxZ = itemHeighmapW;
            if ( Mathf.CeilToInt( itemHeighmapW / 2 ) + mapCenterZ > resolution )
            {
                itemMaxZ -= Mathf.CeilToInt( itemHeighmapW / 2 ) + mapCenterZ - resolution + 1;
            }

            int        w       = itemMaxZ - itemMinZ;
            int        h       = itemMaxX - itemMinX;
            float[ , ] heights = new float[ w, h ];

            int terrHmapMinX = Mathf.Max( mapCenterX - itemHeighmapH / 2, 0 );
            int terrHmapMinZ = Mathf.Max( mapCenterZ - itemHeighmapW / 2, 0 );

            for ( int x = itemMinX; x < itemMaxX; ++x )
            {
                for ( int z = itemMinZ; z < itemMaxZ; ++z )
                {
                    int   terrHmapI   = Mathf.Clamp( mapCenterZ - Mathf.CeilToInt( itemHeighmapW / 2 ) + z, 0, resolution - 1 );
                    int   terrHmapJ   = Mathf.Clamp( mapCenterX - Mathf.CeilToInt( itemHeighmapH / 2 ) + x, 0, resolution - 1 );
                    float terrHmapVal = heighMap[ terrHmapI, terrHmapJ ];

                    int   itemI       = z - itemMinZ;
                    int   itemJ       = x - itemMinX;
                    float itemHmapVal = itemHeighmap[ x, z ];
                    heights[ itemI, itemJ ] = terrHmapVal * ( 1 - itemHmapVal ) + hitHmapVal * itemHmapVal;
                }
            }

            terrainData.SetHeights( terrHmapMinX, terrHmapMinZ, heights );

            ////////////////////
            if ( flattenSettings.clearDetails )
            {
                float          heightToDetail      = (float)terrainData.detailResolution / terrainData.heightmapResolution;
                int            heightToDetailInt   = Mathf.CeilToInt( heightToDetail ) + 1;
                List<int[ , ]> terrainDetailLayers = new List<int[ , ]>();
                List<int[ , ]> detailLayers        = new List<int[ , ]>();
                Vector2Int     densityInt          = new Vector2Int( Mathf.CeilToInt( density.x ), Mathf.CeilToInt( density.y ) );
                int            detailsW            = Mathf.CeilToInt( w * heightToDetail ) + 4 * densityInt.y;
                int            detailsH            = Mathf.CeilToInt( h * heightToDetail ) + 4 * densityInt.x;
                int terrDetailMinX = Mathf.RoundToInt( ( localCenter.x * density.x - itemHeighmapH / 2f ) * heightToDetail )
                                     - 2 * densityInt.x;
                int terrDetailMinY = Mathf.RoundToInt( ( localCenter.z * density.y - itemHeighmapW / 2f ) * heightToDetail )
                                     - 2 * densityInt.y;

                void SetDetailToZero( int layer, int i, int j )
                {
                    detailLayers[ layer ][ i, j ] = 0;
                    for ( int k = 1; k <= heightToDetailInt; ++k )
                    {
                        if ( i - k >= 0 )
                        {
                            detailLayers[ layer ][ i - k, j ] = 0;
                            if ( j - k >= 0 )
                            {
                                detailLayers[ layer ][ i - k, j - k ] = 0;
                            }
                            else if ( j + k < detailsH - 1 )
                            {
                                detailLayers[ layer ][ i - k, j + k ] = 0;
                            }
                        }
                        else if ( i + k < detailsW - 1 )
                        {
                            detailLayers[ layer ][ i + k, j ] = 0;
                            if ( j - k >= 0 )
                            {
                                detailLayers[ layer ][ i + k, j - k ] = 0;
                            }
                            else if ( j + k < detailsH - 1 )
                            {
                                detailLayers[ layer ][ i + k, j + k ] = 0;
                            }
                        }
                        else
                        {
                            if ( j - k >= 0 )
                            {
                                detailLayers[ layer ][ i, j - k ] = 0;
                            }
                            else if ( j + k < detailsH - 1 )
                            {
                                detailLayers[ layer ][ i, j + k ] = 0;
                            }
                        }
                    }
                }

                for ( int k = 0; k < terrainData.detailPrototypes.Length; ++k )
                {

                    terrainDetailLayers.Add( terrainData.GetDetailLayer( 0, 0,
                        terrainData.detailWidth, terrainData.detailHeight, k ) );
                    detailLayers.Add( new int[ detailsW, detailsH ] );
                    for ( int itemDetailI = 0; itemDetailI < detailsW; ++itemDetailI )
                    {
                        for ( int itemDetailJ = 0; itemDetailJ < detailsH; ++itemDetailJ )
                        {
                            int terrDetailI = Mathf.Clamp( terrDetailMinY + itemDetailI, 0, terrainData.detailWidth  - 1 );
                            int terrDetailJ = Mathf.Clamp( terrDetailMinX + itemDetailJ, 0, terrainData.detailHeight - 1 );
                            int layerValue  = terrainDetailLayers[ k ][ terrDetailI, terrDetailJ ];
                            detailLayers[ k ][ itemDetailI, itemDetailJ ] = layerValue;

                            int itemHmapX = Mathf.Clamp( Mathf.RoundToInt( ( itemDetailJ - 2 * densityInt.y )
                                                                           / heightToDetail ), 0, itemHeighmapH - 1 );
                            int itemHmapZ = Mathf.Clamp( Mathf.RoundToInt( ( itemDetailI - 2 * densityInt.x )
                                                                           / heightToDetail ), 0, itemHeighmapW - 1 );
                            float itemHmapVal = itemHeighmap[ itemHmapX, itemHmapZ ];
                            if ( itemHmapVal > 0.9 )
                            {
                                SetDetailToZero( k, itemDetailI, itemDetailJ );
                            }
                        }
                    }

                    terrainData.SetDetailLayer( terrDetailMinX, terrDetailMinY, k, detailLayers[ k ] );
                }
            }

            if ( flattenSettings.clearTrees )
            {
                for ( int k = 0; k < terrainData.detailPrototypes.Length; ++k )
                {
                    List<TreeInstance> treeInstances = new List<TreeInstance>();
                    foreach ( TreeInstance treeInstance in terrainData.treeInstances )
                    {
                        int hmapX     = Mathf.RoundToInt( treeInstance.position.x * resolution );
                        int hmapZ     = Mathf.RoundToInt( treeInstance.position.z * resolution );
                        int itemHmapX = hmapX - terrHmapMinX;
                        int itemHmapZ = hmapZ - terrHmapMinZ;
                        if ( itemHmapX    < 0
                             || itemHmapX >= itemHeighmapH
                             || itemHmapZ < 0
                             || itemHmapZ >= itemHeighmapW )
                        {
                            treeInstances.Add( treeInstance );
                            continue;
                        }

                        float itemHmapVal = itemHeighmap[ itemHmapX, itemHmapZ ];
                        if ( itemHmapVal < 0.9 )
                        {
                            treeInstances.Add( treeInstance );
                        }
                    }

                    terrainData.treeInstances = treeInstances.ToArray();
                }
            }
            //////////////////

        }

        private static void PinDuringSceneGUI( SceneView sceneView )
        {
            if ( PinManager.settings.paintOnMeshesWithoutCollider )
            {
                PWBCore.CreateTempCollidersWithinFrustum( sceneView.camera );
            }

            PinInput( sceneView );
            if ( Event.current.type    != EventType.Repaint
                 && Event.current.type != EventType.Layout )
            {
                return;
            }

            Ray         mouseRay          = HandleUtility.GUIPointToWorldRay( Event.current.mousePosition );
            bool        snappedToVertex   = false;
            RaycastHit  closestVertexInfo = new RaycastHit();
            PinSettings settings          = PinManager.settings;
            if ( _snapToVertex )
            {
                snappedToVertex = SnapToVertex( mouseRay, out closestVertexInfo, sceneView.in2DMode );
            }

            if ( snappedToVertex )
            {
                DrawPin( sceneView, closestVertexInfo, false );
            }
            else
            {
                if ( settings.mode == PaintOnSurfaceToolSettingsBase.PaintMode.ON_SHAPE )
                {
                    if ( GridRaycast( mouseRay, out RaycastHit planeHit ) )
                    {
                        DrawPin( sceneView, planeHit, SnapManager.settings.snappingEnabled );
                    }
                    else
                    {
                        _paintStroke.Clear();
                    }
                }
                else
                {
                    if ( MouseRaycast( mouseRay, out RaycastHit mouseHit,        out GameObject collider, float.MaxValue,
                            -1,                  settings.paintOnPalettePrefabs, settings.paintOnMeshesWithoutCollider ) )
                    {
                        DrawPin( sceneView, mouseHit, SnapManager.settings.snappingEnabled );
                        _pinSurface = collider.transform;
                    }
                    else if ( _pinned )
                    {
                        DrawPin( sceneView, _pinHit, SnapManager.settings.snappingEnabled );
                    }
                    else if ( settings.mode == PaintOnSurfaceToolSettingsBase.PaintMode.AUTO )
                    {
                        if ( GridRaycast( mouseRay, out RaycastHit planeHit ) )
                        {
                            DrawPin( sceneView, planeHit, SnapManager.settings.snappingEnabled );
                        }
                    }
                    else
                    {
                        _paintStroke.Clear();
                    }
                }
            }
        }

        private static void PinInput( SceneView sceneView )
        {
            if ( PaletteManager.selectedBrush == null )
            {
                return;
            }

            KeyCode keyCode = Event.current.keyCode;
            if ( Event.current.button == 0 )
            {
                if ( Event.current.type == EventType.MouseUp
                     && !Event.current.alt )
                {
                    if ( PinManager.settings.flattenTerrain )
                    {
                        FlatenTerrain();
                    }

                    Paint( PinManager.settings );
                    _pinned = false;
                    Event.current.Use();
                }

                if ( Event.current.type == EventType.KeyDown )
                {
                    if ( PWBSettings.shortcuts.pinMoveHandlesUp.Check() )
                    {
                        _pinOffset = nextBoundLayer;
                    }
                    else if ( PWBSettings.shortcuts.pinMoveHandlesDown.Check() )
                    {
                        _pinOffset = prevBoundLayer;
                    }
                    else if ( PWBSettings.shortcuts.pinSelectNextHandle.Check() )
                    {
                        _pinOffset = nextBoundPoint;
                    }
                    else if ( PWBSettings.shortcuts.pinSelectPrevHandle.Check() )
                    {
                        _pinOffset = prevBoundPoint;
                    }
                    else if ( PWBSettings.shortcuts.pinSelectPivotHandle.Check() )
                    {
                        _pinOffset = pivotBoundPoint;
                    }
                    //add rotation around Y
                    else if ( PWBSettings.shortcuts.pinRotate90YCW.Check() )
                    {
                        _pinAngle.y = ( _pinAngle.y + 90 ) % 360;
                    }
                    else if ( PWBSettings.shortcuts.pinRotate90YCCW.Check() )
                    {
                        _pinAngle.y = ( _pinAngle.y - 90 ) % 360;
                    }
                    else if ( PWBSettings.shortcuts.pinRotateAStepYCW.Check() )
                    {
                        _pinAngle.y -= PinManager.rotationSnapValue;
                    }
                    else if ( PWBSettings.shortcuts.pinRotateAStepYCCW.Check() )
                    {
                        _pinAngle.y += PinManager.rotationSnapValue;
                    }
                    //add rotation around X
                    else if ( PWBSettings.shortcuts.pinRotate90XCW.Check() )
                    {
                        _pinAngle.x = ( _pinAngle.x + 90 ) % 360;
                    }
                    else if ( PWBSettings.shortcuts.pinRotate90XCCW.Check() )
                    {
                        _pinAngle.x = ( _pinAngle.x - 90 ) % 360;
                    }
                    else if ( PWBSettings.shortcuts.pinRotateAStepXCW.Check() )
                    {
                        _pinAngle.x -= PinManager.rotationSnapValue;
                    }
                    else if ( PWBSettings.shortcuts.pinRotateAStepXCCW.Check() )
                    {
                        _pinAngle.x += PinManager.rotationSnapValue;
                    }
                    //add rotation around Z
                    else if ( PWBSettings.shortcuts.pinRotate90ZCW.Check() )
                    {
                        _pinAngle.z = ( _pinAngle.z + 90 ) % 360;
                    }
                    else if ( PWBSettings.shortcuts.pinRotate90ZCCW.Check() )
                    {
                        _pinAngle.z = ( _pinAngle.z - 90 ) % 360;
                    }
                    else if ( PWBSettings.shortcuts.pinRotateAStepZCW.Check() )
                    {
                        _pinAngle.z -= PinManager.rotationSnapValue;
                    }
                    else if ( PWBSettings.shortcuts.pinRotateAStepZCCW.Check() )
                    {
                        _pinAngle.z += PinManager.rotationSnapValue;
                    }
                    //reset rotation
                    else if ( PWBSettings.shortcuts.pinResetRotation.Check() )
                    {
                        _pinAngle = Vector3.zero;
                    }
                    else if ( PWBSettings.shortcuts.pinSubtract1UnitFromSurfDist.Check() )
                    {
                        _pinDistanceFromSurface -= 1f;
                    }
                    else if ( PWBSettings.shortcuts.pinAdd1UnitToSurfDist.Check() )
                    {
                        _pinDistanceFromSurface += 1f;
                    }
                    else if ( PWBSettings.shortcuts.pinSubtract01UnitFromSurfDist.Check() )
                    {
                        _pinDistanceFromSurface -= 0.1f;
                    }
                    else if ( PWBSettings.shortcuts.pinAdd01UnitToSurfDist.Check() )
                    {
                        _pinDistanceFromSurface += 0.1f;
                    }
                    else if ( PWBSettings.shortcuts.pinResetSurfDist.Check() )
                    {
                        _pinDistanceFromSurface = 0;
                    }
                    else if ( PWBSettings.shortcuts.pinResetScale.Check() )
                    {
                        UpdatePinScale( 1f );
                    }
                    else if ( PWBSettings.shortcuts.pinToggleRepeatItem.Check() )
                    {
                        PinManager.settings.repeat = !PinManager.settings.repeat;
                        ToolProperties.RepainWindow();
                    }
                    else if ( PWBSettings.shortcuts.pinSelectPreviousItem.Check() )
                    {
                        BrushstrokeManager.SetNextPinBrushstroke( -1 );
                        sceneView.Repaint();
                        repaint = true;
                    }
                    else if ( PWBSettings.shortcuts.pinSelectNextItem.Check() )
                    {
                        BrushstrokeManager.SetNextPinBrushstroke( 1 );
                        sceneView.Repaint();
                        repaint = true;
                    }
                }
            }
            else
            {
                if ( Event.current.type == EventType.MouseDown
                     && Event.current.control )
                {
                    _pinned           = true;
                    _pinMouse         = Event.current.mousePosition;
                    _previousPinAngle = _pinAngle;
                    Event.current.Use();
                }
                else if ( Event.current.type == EventType.MouseUp
                          && !Event.current.control )
                {
                    _pinned = false;
                }
            }

            const float DEG_PER_PIXEL = 1.8f; //180deg/100px

            if ( PWBSettings.shortcuts.pinSelectNextItemScroll.Check() )
            {
                float scrollSign = Mathf.Sign( Event.current.delta.y );
                Event.current.Use();
                BrushstrokeManager.SetNextPinBrushstroke( (int)scrollSign );
                sceneView.Repaint();
                repaint = true;
            }
            else if ( PWBSettings.shortcuts.pinRotateAroundY.Check() )
            {
                PWBMouseCombination combi = PWBSettings.shortcuts.pinRotateAroundY.combination;
                if ( combi.mouseEvent == PWBMouseCombination.MouseEvents.SCROLL_WHEEL )
                {
                    _pinAngle.y += combi.delta;
                }
                else if ( combi.isMouseDragEvent )
                {
                    _pinAngle.y -= combi.delta * DEG_PER_PIXEL;
                }

                _previousPinAngle = _pinAngle;
            }
            else if ( PWBSettings.shortcuts.pinRotateAroundYSnaped.Check() )
            {
                PWBMouseCombination combi = PWBSettings.shortcuts.pinRotateAroundYSnaped.combination;
                if ( combi.mouseEvent == PWBMouseCombination.MouseEvents.SCROLL_WHEEL )
                {
                    float scrollSign = Mathf.Sign( Event.current.delta.y );
                    _pinAngle.y += scrollSign * PinManager.rotationSnapValue;
                }
                else if ( combi.isMouseDragEvent )
                {
                    _pinAngle.y         = _previousPinAngle.y - combi.delta * DEG_PER_PIXEL;
                    _previousPinAngle.y = _pinAngle.y;
                    if ( PinManager.rotationSnapValue > 0 )
                    {
                        _pinAngle.y = Mathf.Round( _pinAngle.y / PinManager.rotationSnapValue ) * PinManager.rotationSnapValue;
                    }
                }
            }
            else if ( PWBSettings.shortcuts.pinRotateAroundX.Check() )
            {
                PWBMouseCombination combi = PWBSettings.shortcuts.pinRotateAroundX.combination;
                if ( combi.mouseEvent == PWBMouseCombination.MouseEvents.SCROLL_WHEEL )
                {
                    _pinAngle.x += Event.current.delta.y;
                }
                else if ( combi.isMouseDragEvent )
                {
                    _pinAngle.x -= combi.delta * DEG_PER_PIXEL;
                }

                _previousPinAngle = _pinAngle;
            }
            else if ( PWBSettings.shortcuts.pinRotateAroundXSnaped.Check() )
            {
                PWBMouseCombination combi = PWBSettings.shortcuts.pinRotateAroundXSnaped.combination;
                if ( combi.mouseEvent == PWBMouseCombination.MouseEvents.SCROLL_WHEEL )
                {
                    float scrollSign = Mathf.Sign( Event.current.delta.y );
                    _pinAngle.x += scrollSign * PinManager.rotationSnapValue;
                }
                else if ( combi.isMouseDragEvent )
                {
                    _pinAngle.x         = _previousPinAngle.x + combi.delta * DEG_PER_PIXEL;
                    _previousPinAngle.x = _pinAngle.x;
                    if ( PinManager.rotationSnapValue > 0 )
                    {
                        _pinAngle.x = Mathf.Round( _pinAngle.x / PinManager.rotationSnapValue ) * PinManager.rotationSnapValue;
                    }
                }
            }
            else if ( PWBSettings.shortcuts.pinRotateAroundZ.Check() )
            {
                PWBMouseCombination combi = PWBSettings.shortcuts.pinRotateAroundZ.combination;
                if ( combi.mouseEvent == PWBMouseCombination.MouseEvents.SCROLL_WHEEL )
                {
                    _pinAngle.z += Event.current.delta.y;
                }
                else if ( combi.isMouseDragEvent )
                {
                    _pinAngle.z -= combi.delta * DEG_PER_PIXEL;
                }

                _previousPinAngle = _pinAngle;
            }
            else if ( PWBSettings.shortcuts.pinRotateAroundZSnaped.Check() )
            {
                PWBMouseCombination combi = PWBSettings.shortcuts.pinRotateAroundZSnaped.combination;
                if ( combi.mouseEvent == PWBMouseCombination.MouseEvents.SCROLL_WHEEL )
                {
                    float scrollSign = Mathf.Sign( Event.current.delta.y );
                    _pinAngle.z += scrollSign * PinManager.rotationSnapValue;
                }
                else if ( combi.isMouseDragEvent )
                {
                    _pinAngle.z         = _previousPinAngle.z + combi.delta * DEG_PER_PIXEL;
                    _previousPinAngle.z = _pinAngle.z;
                    if ( PinManager.rotationSnapValue > 0 )
                    {
                        _pinAngle.z = Mathf.Round( _pinAngle.z / PinManager.rotationSnapValue ) * PinManager.rotationSnapValue;
                    }
                }
            }
            else if ( PWBSettings.shortcuts.pinSurfDist.Check() )
            {
                PWBMouseCombination combi = PWBSettings.shortcuts.pinSurfDist.combination;
                if ( combi.mouseEvent == PWBMouseCombination.MouseEvents.SCROLL_WHEEL )
                {
                    _pinDistanceFromSurface += Event.current.delta.y * 0.04f;
                }
                else if ( combi.isMouseDragEvent )
                {
                    _pinDistanceFromSurface += combi.delta * 0.04f;
                }
            }
            else if ( PWBSettings.shortcuts.pinScale.Check() )
            {

                if ( PWBSettings.shortcuts.pinScale.combination.mouseEvent == PWBMouseCombination.MouseEvents.SCROLL_WHEEL )
                {
                    float scrollSign = Mathf.Sign( Event.current.delta.y );
                    UpdatePinScale( Mathf.Max( _pinScale * ( 1f + scrollSign * 0.05f ), 0.01f ) );
                    sceneView.Repaint();
                    repaint = true;
                }
                else if ( PWBSettings.shortcuts.pinScale.combination.isMouseDragEvent )
                {
                    UpdatePinScale( Mathf.Max( _pinScale * ( 1f + PWBSettings.shortcuts.pinScale.combination.delta * 0.003f ),
                        0.01f ) );
                    sceneView.Repaint();
                    repaint = true;
                }
            }

            if ( ( keyCode == KeyCode.LeftControl || keyCode == KeyCode.RightControl )
                 && Event.current.type == EventType.KeyUp )
            {
                _pinned = false;
            }
        }

        private static void PinPreview( Camera camera )
        {
            _paintStroke.Clear();
            if ( BrushstrokeManager.brushstroke.Length == 0 )
            {
                return;
            }

            BrushstrokeItem strokeItem = BrushstrokeManager.brushstroke[ 0 ].Clone();
            GameObject      prefab     = strokeItem.settings.prefab;
            if ( prefab == null )
            {
                return;
            }

            BrushSettings brushSettings = strokeItem.settings;
            if ( PinManager.settings.overwriteBrushProperties )
            {
                brushSettings = PinManager.settings.brushSettings;
            }

            if ( !brushSettings.isAsset2D
                 && !brushSettings.rotateToTheSurface
                 && _prevPinHitNormal != _pinHit.normal )
            {
                _prevPinHitNormal = _pinHit.normal;
                UpdatePinValues();
            }

            Quaternion itemRotation = Quaternion.identity;
            Vector3    itemPosition = _pinHit.point;
            if ( brushSettings.rotateToTheSurface
                 && !PinManager.settings.flattenTerrain )
            {
                if ( _pinHit.normal == Vector3.zero )
                {
                    _pinHit.normal = Vector3.up;
                }

                Vector3 itemTangent = Vector3.Cross( _pinHit.normal, Vector3.left );
                if ( itemTangent.sqrMagnitude < 0.000001 )
                {
                    itemTangent = Vector3.Cross( _pinHit.normal, Vector3.back );
                }

                itemTangent = itemTangent.normalized;
                if ( _pinHit.collider == null
                     && strokeItem.settings.isAsset2D )
                {
                    itemRotation = Quaternion.LookRotation( Vector3.forward, Vector3.up );
                }
                else
                {
                    itemRotation = Quaternion.LookRotation( itemTangent, _pinHit.normal );
                }
            }

            if ( _pinHit.collider != null )
            {
                GameObject obj       = _pinHit.collider.gameObject;
                Transform  hitParent = _pinHit.collider.transform.parent;
                if ( hitParent                               != null
                     && hitParent.gameObject.GetInstanceID() == PWBCore.parentColliderId )
                {
                    obj = PWBCore.GetGameObjectFromTempColliderId( obj.GetInstanceID() );
                }
            }

            GameObject objUnderMouse = null;
            if ( _pinHit.collider != null )
            {
                Transform parentUnderMouse = _pinHit.collider.transform.parent;
                if ( parentUnderMouse                               != null
                     && parentUnderMouse.gameObject.GetInstanceID() == PWBCore.parentColliderId )
                {
                    objUnderMouse = PWBCore.GetGameObjectFromTempColliderId(
                        _pinHit.collider.gameObject.GetInstanceID() );
                }
                else
                {
                    objUnderMouse = _pinHit.collider.gameObject;
                }
            }

            if ( PinManager.settings.paintOnSelectedOnly
                 && objUnderMouse != null
                 && !SelectionManager.selection.Contains( objUnderMouse ) )
            {
                return;
            }

            itemRotation *= Quaternion.Euler( strokeItem.additionalAngle );
            itemRotation *= Quaternion.Euler( _pinAngle );
            if ( brushSettings.alwaysOrientUp )
            {
                Vector3 fw = Quaternion.Euler( strokeItem.additionalAngle ) * Quaternion.Euler( _pinAngle ) * _pinHit.normal;
                fw.y = 0;
                if ( fw != Vector3.zero )
                {
                    itemRotation = Quaternion.LookRotation( fw, Vector3.up );
                }
            }

            itemPosition += itemRotation * _pinOffset;
            itemPosition += itemRotation * brushSettings.localPositionOffset;

            Vector3 scaleMult = strokeItem.scaleMultiplier * _pinScale;
            Vector3 itemScale = Vector3.Scale( prefab.transform.localScale, scaleMult );

            itemPosition += _pinHit.normal * ( strokeItem.surfaceDistance + _pinDistanceFromSurface );

            if ( brushSettings.embedInSurface
                 && !brushSettings.embedAtPivotHeight
                 && PinManager.settings.mode != PaintOnSurfaceToolSettingsBase.PaintMode.ON_SHAPE )
            {
                Matrix4x4 TRS = Matrix4x4.TRS( itemPosition, itemRotation,
                    Vector3.Scale( prefab.transform.localScale, strokeItem.scaleMultiplier ) );
                float bottomDistanceToSurfce = GetBottomDistanceToSurface( strokeItem.settings.bottomVertices,
                    TRS, Mathf.Abs( strokeItem.settings.bottomMagnitude ), PinManager.settings.paintOnPalettePrefabs,
                    PinManager.settings.paintOnMeshesWithoutCollider );

                itemPosition += itemRotation * new Vector3( 0f, -bottomDistanceToSurfce, 0f );
            }

            int       layer           = PinManager.settings.overwritePrefabLayer ? PinManager.settings.layer : prefab.layer;
            Transform parentTransform = GetParent( PinManager.settings, prefab.name, false, _pinSurface );

            Matrix4x4 translateMatrix = Matrix4x4.Translate( -prefab.transform.position );

            if ( PinManager.settings.avoidOverlapping )
            {
                Bounds  itemBounds    = BoundsUtils.GetBoundsRecursive( prefab.transform, Quaternion.identity );
                Vector3 pivotToCenter = itemBounds.center - prefab.transform.position;
                pivotToCenter = Vector3.Scale( pivotToCenter, scaleMult );
                pivotToCenter = itemRotation * pivotToCenter;
                Vector3 itemCenter      = itemPosition + pivotToCenter;
                Vector3 itemHalfExtends = Vector3.Scale( itemBounds.size * 0.499f, strokeItem.scaleMultiplier );
                Collider[] overlaped = Physics.OverlapBox( itemCenter, itemHalfExtends,
                    itemRotation, -1, QueryTriggerInteraction.Ignore ).Where( c => c != _pinHit.collider && IsVisible( c.gameObject ) ).ToArray();
                if ( overlaped.Length > 0 )
                {
                    DrawPinHandles( Matrix4x4.TRS( itemPosition, itemRotation, scaleMult ) * translateMatrix,
                        new Color( 1f, 0f, 0f, 0.7f ) );
                    return;
                }
            }

            _paintStroke.Add( new PaintStrokeItem( prefab, itemPosition,
                itemRotation * Quaternion.Euler( prefab.transform.eulerAngles ),
                itemScale, layer, parentTransform, _pinSurface, strokeItem.flipX, strokeItem.flipY ) );

            Matrix4x4 rootToWorld = Matrix4x4.TRS( itemPosition, itemRotation, scaleMult ) * translateMatrix;
            PreviewBrushItem( prefab, rootToWorld, layer, camera, false, false, strokeItem.flipX, strokeItem.flipY );

            DrawPinHandles( Matrix4x4.TRS( itemPosition, itemRotation, scaleMult ) * translateMatrix,
                new Color( 1f, 1f, 1f, 0.7f ) );

            _pinSurface = null;
        }

        private static void SetPinValues( Quaternion additionRotation )
        {
            BrushstrokeItem strokeItem = BrushstrokeManager.brushstroke[ 0 ];
            GameObject      prefab     = strokeItem.settings.prefab;
            if ( prefab == null )
            {
                return;
            }

            bool isSprite = prefab.GetComponentsInChildren<SpriteRenderer>()
                                  .Where( r => r.enabled && r.sprite != null && r.gameObject.activeSelf ).ToArray().Length
                            > 0;

            Bounds bounds = BoundsUtils.GetBoundsRecursive( prefab.transform, additionRotation );

            Vector3 halfSize = bounds.size * 0.5f;
            _pinBoundPoints.Clear();
            _initialPinBoundPoints.Clear();

            Vector3 centerToPivot = prefab.transform.position - bounds.center;
            Vector3 centerToPivotOnPlane = Vector3.ProjectOnPlane( centerToPivot,
                additionRotation * Vector3.up );

            Quaternion pointRotation = Quaternion.Inverse( additionRotation );

            Vector3 pivotTocenter = Vector3.zero;

            int l = 0;
            Vector2[] pointsNormalized = new[]
            {
                new Vector2( 0,  0 ),
                new Vector2( -1, 0 ), new Vector2( 0,   1 ), new Vector2( 1, 0 ), new Vector2( 0, -1 ),
                new Vector2( -1, -1 ), new Vector2( -1, 1 ), new Vector2( 1, 1 ), new Vector2( 1, -1 ),
            };
            int ySign = ( additionRotation * Vector3.up ).y > 0.0001 ? 1 : -1;
            for ( int y = -1; y <= 1; y += 1 )
            {

                float newY = -y * ySign * halfSize.y + centerToPivot.y;
                _pinBoundPoints.Add( new List<Vector3>() );
                _initialPinBoundPoints.Add( new List<Vector3>() );
                Vector3 center = pointRotation * new Vector3( 0f, -y * ySign * halfSize.y, 0f ) + centerToPivot;

                _pinBoundPoints[ l ].Add( pivotTocenter );
                _initialPinBoundPoints[ l ].Add( pivotTocenter );

                foreach ( Vector2 n in pointsNormalized )
                {
                    Vector3 point = pointRotation
                                    * Vector3.Scale( !isSprite
                                        ? new Vector3( n.x, -y * ySign, n.y )
                                        : new Vector3( n.x, n.y,        -y * ySign ), halfSize )
                                    + centerToPivot;
                    _pinBoundPoints[ l ].Add( point );
                    _initialPinBoundPoints[ l ].Add( point );
                }

                ++l;
            }
        }

        private static void UpdatePinScale()
        {
            for ( int l = 0; l < _pinBoundPoints.Count; ++l )
            for ( int p = 0; p < _pinBoundPoints[ l ].Count; ++p )
            {
                _pinBoundPoints[ l ][ p ] = _initialPinBoundPoints[ l ][ p ] * _pinScale;
            }

            _pinOffset = _pinBoundPoints[ _pinBoundLayerIdx ][ _pinBoundPointIdx ];
        }

        private static void UpdatePinScale( float value )
        {
            if ( _pinScale == value )
            {
                return;
            }

            _pinScale = value;
            UpdatePinScale();
        }

        #endregion

    }

    #endregion

}