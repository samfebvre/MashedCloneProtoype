using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;

namespace PluginMaster
{

    #region DATA & SETTINGS

    [Serializable]
    public class BrushToolSettings : BrushToolBase, IPaintOnSurfaceToolSettings, ISerializationCallbackReceiver
    {

        #region Serialized

        [SerializeField] private PaintOnSurfaceToolSettings _paintOnSurfaceToolSettings = new PaintOnSurfaceToolSettings();

        [SerializeField] private float                _maxHeightFromCenter = 2f;
        [SerializeField] private HeightType           _heightType          = HeightType.RADIUS;
        [SerializeField] private AvoidOverlappingType _avoidOverlapping    = AvoidOverlappingType.WITH_ALL_OBJECTS;

        [SerializeField] private LayerMask         _layerFilter = -1;
        [SerializeField] private List<string>      _tagFilter;
        [SerializeField] private RandomUtils.Range _slopeFilter = new RandomUtils.Range( 0, 60 );
        [SerializeField] private string[]          _terrainLayerIds;
        [SerializeField] private bool              _showPreview;

        #endregion

        #region Public Enums

        public enum AvoidOverlappingType
        {
            DISABLED,
            WITH_PALETTE_PREFABS,
            WITH_BRUSH_PREFABS,
            WITH_SAME_PREFABS,
            WITH_ALL_OBJECTS,
        }

        public enum HeightType
        {
            CUSTOM,
            RADIUS,
        }

        #endregion

        #region Public Properties

        public AvoidOverlappingType avoidOverlapping
        {
            get => _avoidOverlapping;
            set
            {
                if ( _avoidOverlapping == value )
                {
                    return;
                }

                _avoidOverlapping = value;
                DataChanged();
            }
        }

        public HeightType heightType
        {
            get => _heightType;
            set
            {
                if ( _heightType == value )
                {
                    return;
                }

                _heightType = value;
                DataChanged();
            }
        }

        public virtual LayerMask layerFilter
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

        public float maxHeightFromCenter
        {
            get => _maxHeightFromCenter;
            set
            {
                if ( _maxHeightFromCenter == value )
                {
                    return;
                }

                _maxHeightFromCenter = value;
                DataChanged();
            }
        }

        public bool paintOnMeshesWithoutCollider
        {
            get => _paintOnSurfaceToolSettings.paintOnMeshesWithoutCollider;
            set => _paintOnSurfaceToolSettings.paintOnMeshesWithoutCollider = value;
        }

        public bool paintOnPalettePrefabs
        {
            get => _paintOnSurfaceToolSettings.paintOnPalettePrefabs;
            set => _paintOnSurfaceToolSettings.paintOnPalettePrefabs = value;
        }

        public bool paintOnSelectedOnly
        {
            get => _paintOnSurfaceToolSettings.paintOnSelectedOnly;
            set => _paintOnSurfaceToolSettings.paintOnSelectedOnly = value;
        }

        public bool showPreview
        {
            get => _showPreview;
            set
            {
                if ( _showPreview == value )
                {
                    return;
                }

                _showPreview = value;
                DataChanged();
            }
        }

        public virtual RandomUtils.Range slopeFilter
        {
            get => _slopeFilter;
            set
            {
                if ( _slopeFilter == value )
                {
                    return;
                }

                _slopeFilter = value;
                DataChanged();
            }
        }

        public virtual List<string> tagFilter
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

        public TerrainLayer[] terrainLayerFilter
        {
            get
            {
                if ( ( _terrainLayerFilter == null && _terrainLayerIds != null ) || _updateTerrainFilter )
                {
                    UpdateTerrainFilter();
                }

                return _terrainLayerFilter;
            }
            set
            {
                if ( Equals( _terrainLayerFilter, value ) )
                {
                    return;
                }

                if ( value == null )
                {
                    _terrainLayerFilter = null;
                    _terrainLayerIds    = null;
                    return;
                }

                List<TerrainLayer> layerList       = new List<TerrainLayer>();
                List<string>       terrainLayerIds = new List<string>();
                foreach ( TerrainLayer layer in value )
                {
                    layerList.Add( layer );
                    if ( layer == null )
                    {
                        continue;
                    }

                    terrainLayerIds.Add( GlobalObjectId.GetGlobalObjectIdSlow( layer ).ToString() );
                }

                _terrainLayerFilter = layerList.ToArray();
                _terrainLayerIds    = terrainLayerIds.ToArray();
            }
        }

        #endregion

        #region Public Constructors

        public BrushToolSettings()
        {
            id                                        =  DateTime.Now.Ticks;
            _paintOnSurfaceToolSettings.OnDataChanged += DataChanged;
        }

        #endregion

        #region Public Methods

        public override void Copy( IToolSettings other )
        {
            BrushToolSettings otherBrushToolSettings = other as BrushToolSettings;
            if ( otherBrushToolSettings == null )
            {
                return;
            }

            base.Copy( other );
            _paintOnSurfaceToolSettings.Copy( otherBrushToolSettings._paintOnSurfaceToolSettings );
            _maxHeightFromCenter = otherBrushToolSettings._maxHeightFromCenter;
            _heightType          = otherBrushToolSettings._heightType;
            _avoidOverlapping    = otherBrushToolSettings._avoidOverlapping;
            _layerFilter         = otherBrushToolSettings._layerFilter;
            _tagFilter = otherBrushToolSettings._tagFilter == null
                ? null
                : new List<string>( otherBrushToolSettings._tagFilter );
            _slopeFilter = new RandomUtils.Range( otherBrushToolSettings._slopeFilter );
            _terrainLayerFilter = otherBrushToolSettings._terrainLayerFilter == null
                ? null
                : otherBrushToolSettings._terrainLayerFilter.ToArray();
            _terrainLayerIds = otherBrushToolSettings._terrainLayerIds == null
                ? null
                : otherBrushToolSettings._terrainLayerIds.ToArray();
        }

        public void OnAfterDeserialize()
        {
            UpdateTagFilter();
            _updateTerrainFilter = true;
        }

        public void OnBeforeSerialize()
        {
            UpdateTagFilter();
            UpdateTerrainFilter();
        }

        #endregion

        #region Private Fields

        private TerrainLayer[] _terrainLayerFilter;
        private bool           _updateTerrainFilter;
        private long           id;

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

        private void UpdateTerrainFilter()
        {
            _updateTerrainFilter = false;
            if ( _terrainLayerIds == null )
            {
                return;
            }

            List<TerrainLayer> terrainLayerList = new List<TerrainLayer>();
            foreach ( string globalId in _terrainLayerIds )
            {
                if ( GlobalObjectId.TryParse( globalId, out GlobalObjectId id ) )
                {
                    TerrainLayer layer = GlobalObjectId.GlobalObjectIdentifierToObjectSlow( id ) as TerrainLayer;
                    if ( layer == null )
                    {
                        continue;
                    }

                    terrainLayerList.Add( layer );
                }
            }

            _terrainLayerFilter = terrainLayerList.ToArray();
        }

        #endregion

    }

    [Serializable]
    public class BrushManager : ToolManagerBase<BrushToolSettings>
    {
    }

    #endregion

    #region PWBIO

    public static partial class PWBIO
    {

        #region Statics and Constants

        private static float _brushAngle;

        #endregion

        #region Private Methods

        private static void BrushDuringSceneGUI( SceneView sceneView )
        {
            if ( BrushManager.settings.paintOnMeshesWithoutCollider )
            {
                PWBCore.CreateTempCollidersWithinFrustum( sceneView.camera );
            }

            BrushstrokeMouseEvents( BrushManager.settings );
            Vector2 mousePos = Event.current.mousePosition;
            if ( _pinned )
            {
                mousePos = _pinMouse;
            }

            Ray        mouseRay          = HandleUtility.GUIPointToWorldRay( mousePos );
            bool       snappedToVertex   = false;
            RaycastHit closestVertexInfo = new RaycastHit();
            if ( _snapToVertex )
            {
                snappedToVertex = SnapToVertex( mouseRay, out closestVertexInfo, sceneView.in2DMode );
            }

            if ( snappedToVertex )
            {
                mouseRay.origin = closestVertexInfo.point - mouseRay.direction;
            }

            bool in2DMode = PaletteManager.selectedBrush != null
                            && PaletteManager.selectedBrush.isAsset2D
                            && sceneView.in2DMode;
            if ( BrushRaycast( mouseRay, out RaycastHit hit, float.MaxValue, -1, BrushManager.settings, null ) || in2DMode )
            {
                if ( in2DMode )
                {
                    hit.point  = new Vector3( mouseRay.origin.x, mouseRay.origin.y, 0f );
                    hit.normal = Vector3.back;
                }

                DrawBrush( sceneView, hit, BrushManager.settings.showPreview );
            }
            else
            {
                _paintStroke.Clear();
            }

            if ( Event.current.button == 0
                 && !Event.current.alt
                 && ( Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag ) )
            {
                if ( !BrushManager.settings.showPreview )
                {
                    DrawBrush( sceneView, hit, true );
                }

                Paint( BrushManager.settings );
                Event.current.Use();
            }
        }

        private static bool BrushRaycast( Ray       ray,       out RaycastHit    hit,      float          maxDistance,
                                          LayerMask layerMask, BrushToolSettings settings, TerrainLayer[] terrainLayers )
        {
            hit = new RaycastHit();
            bool  result             = false;
            float noColliderDistance = float.MaxValue;
            if ( MouseRaycast( ray, out RaycastHit hitInfo,         out GameObject collider,               maxDistance,
                    layerMask,      settings.paintOnPalettePrefabs, settings.paintOnMeshesWithoutCollider, settings.tagFilter.ToArray(), terrainLayers ) )
            {
                GameObject nearestRoot      = PrefabUtility.GetNearestPrefabInstanceRoot( collider );
                bool       isAPaintedObject = false;

                while ( nearestRoot != null )
                {
                    isAPaintedObject = isAPaintedObject || _paintedObjects.Contains( nearestRoot );
                    GameObject parent = nearestRoot.transform.parent == null
                        ? null
                        : nearestRoot.transform.parent.gameObject;
                    nearestRoot = parent == null ? null : PrefabUtility.GetNearestPrefabInstanceRoot( parent );
                }

                bool selectedOnlyFilter = !settings.paintOnSelectedOnly
                                          || SelectionManager.selection.Contains( collider )
                                          || PWBCore.CollidersContains( SelectionManager.selection, collider.name );

                bool paletteFilter = !isAPaintedObject || settings.paintOnPalettePrefabs;
                bool filterResult  = selectedOnlyFilter && paletteFilter;

                result = filterResult;
                if ( filterResult && hitInfo.distance < noColliderDistance )
                {
                    hit = hitInfo;
                }

            }

            return result;
        }

        private static void BrushstrokePreview( Vector3 hitPoint, Vector3 normal,
                                                Vector3 tangent,  Vector3 bitangent, SceneView sceneView )
        {
            Camera            camera   = sceneView.camera;
            BrushToolSettings settings = BrushManager.settings;
            _paintStroke.Clear();
            List<GameObject> nearbyObjectsAtDensitySpacing = new List<GameObject>();
            foreach ( BrushstrokeItem strokeItem in BrushstrokeManager.brushstroke )
            {
                Vector3 worldPos = hitPoint
                                   + TangentSpaceToWorld( tangent, bitangent,
                                       new Vector2( strokeItem.tangentPosition.x, strokeItem.tangentPosition.y ) );
                float height = settings.heightType == BrushToolSettings.HeightType.CUSTOM
                    ? settings.maxHeightFromCenter
                    : settings.radius;
                Ray  ray      = new Ray( worldPos + normal * height, -normal );
                bool in2DMode = strokeItem.settings.isAsset2D && sceneView.in2DMode;

                if ( BrushRaycast( ray, out RaycastHit itemHit, height * 2f, settings.layerFilter,
                         settings,      settings.terrainLayerFilter )
                     || in2DMode )
                {
                    if ( in2DMode )
                    {
                        itemHit.point  = new Vector3( worldPos.x, worldPos.y, 0f );
                        itemHit.normal = Vector3.forward;
                    }
                    else
                    {
                        float slope = Mathf.Abs( Vector3.Angle( Vector3.up, itemHit.normal ) );
                        if ( slope > 90f )
                        {
                            slope = 180f - slope;
                        }

                        if ( slope    < settings.slopeFilter.min
                             || slope > settings.slopeFilter.max )
                        {
                            continue;
                        }
                    }

                    GameObject prefab = strokeItem.settings.prefab;
                    if ( prefab == null )
                    {
                        continue;
                    }

                    BrushSettings brushSettings = strokeItem.settings;
                    if ( settings.overwriteBrushProperties )
                    {
                        brushSettings = settings.brushSettings;
                    }

                    Quaternion itemRotation = Quaternion.AngleAxis( _brushAngle, Vector3.up );
                    Vector3    itemPosition = itemHit.point;
                    if ( brushSettings.rotateToTheSurface )
                    {
                        Vector3 itemTangent = GetTangent( itemHit.normal );
                        itemRotation =  Quaternion.LookRotation( itemTangent, itemHit.normal );
                        itemPosition += itemHit.normal * brushSettings.surfaceDistance;
                    }
                    else
                    {
                        itemPosition += normal * brushSettings.surfaceDistance;
                    }

                    if ( settings.avoidOverlapping    != BrushToolSettings.AvoidOverlappingType.DISABLED
                         && settings.avoidOverlapping != BrushToolSettings.AvoidOverlappingType.WITH_ALL_OBJECTS )
                    {
                        float rSqr           = settings.minSpacing * settings.minSpacing;
                        float d              = settings.density    / 100f;
                        float densitySpacing = Mathf.Sqrt( rSqr / d );
                        octree.GetNearbyNonAlloc( itemPosition, densitySpacing, nearbyObjectsAtDensitySpacing );
                        if ( nearbyObjectsAtDensitySpacing.Count > 0 )
                        {
                            bool brushObjectsNearby = false;
                            foreach ( GameObject obj in nearbyObjectsAtDensitySpacing )
                            {
                                if ( settings.avoidOverlapping
                                     == BrushToolSettings.AvoidOverlappingType.WITH_BRUSH_PREFABS
                                     && PaletteManager.selectedBrush.ContainsSceneObject( obj ) )
                                {
                                    brushObjectsNearby = true;
                                    break;
                                }

                                if ( settings.avoidOverlapping
                                     == BrushToolSettings.AvoidOverlappingType.WITH_PALETTE_PREFABS
                                     && PaletteManager.selectedPalette.ContainsSceneObject( obj ) )
                                {
                                    brushObjectsNearby = true;
                                    break;
                                }

                                if ( settings.avoidOverlapping
                                     == BrushToolSettings.AvoidOverlappingType.WITH_SAME_PREFABS )
                                {
                                    GameObject outermostPrefab = PrefabUtility.GetOutermostPrefabInstanceRoot( obj );
                                    if ( outermostPrefab == null )
                                    {
                                        continue;
                                    }

                                    GameObject source = PrefabUtility.GetCorrespondingObjectFromSource( outermostPrefab );
                                    if ( source == null )
                                    {
                                        continue;
                                    }

                                    if ( prefab == source )
                                    {
                                        brushObjectsNearby = true;
                                        break;
                                    }
                                }
                            }

                            if ( brushObjectsNearby )
                            {
                                continue;
                            }
                        }
                    }

                    if ( settings.orientAlongBrushstroke )
                    {
                        itemRotation = Quaternion.Euler( settings.additionalOrientationAngle )
                                       * Quaternion.LookRotation( _strokeDirection, itemRotation * Vector3.up );
                        itemPosition = hitPoint + itemRotation * ( itemPosition - hitPoint );
                    }

                    itemRotation *= Quaternion.Euler( strokeItem.additionalAngle );
                    if ( brushSettings.alwaysOrientUp )
                    {
                        Vector3 fw = Quaternion.Euler( strokeItem.additionalAngle ) * itemHit.normal;
                        fw.y = 0;
                        if ( fw.magnitude > 0.000001f )
                        {
                            itemRotation = Quaternion.LookRotation( fw, Vector3.up );
                        }
                    }

                    itemPosition += itemRotation * brushSettings.localPositionOffset;

                    if ( brushSettings.embedInSurface
                         && !brushSettings.embedAtPivotHeight )
                    {
                        Matrix4x4 TRS = Matrix4x4.TRS( itemPosition, itemRotation,
                            Vector3.Scale( prefab.transform.localScale, strokeItem.scaleMultiplier ) );
                        float bottomDistanceToSurfce = GetBottomDistanceToSurface( strokeItem.settings.bottomVertices,
                            TRS, Mathf.Abs( strokeItem.settings.bottomMagnitude ), PinManager.settings.paintOnPalettePrefabs,
                            PinManager.settings.paintOnMeshesWithoutCollider );
                        itemPosition += itemRotation * new Vector3( 0f, -bottomDistanceToSurfce, 0f );
                    }

                    Vector3 itemScale = Vector3.Scale( prefab.transform.localScale, strokeItem.scaleMultiplier );

                    if ( settings.avoidOverlapping == BrushToolSettings.AvoidOverlappingType.WITH_ALL_OBJECTS )
                    {
                        Bounds  itemBounds    = BoundsUtils.GetBoundsRecursive( prefab.transform, Quaternion.identity );
                        Vector3 pivotToCenter = itemBounds.center - prefab.transform.position;
                        pivotToCenter = Vector3.Scale( pivotToCenter, strokeItem.scaleMultiplier );
                        pivotToCenter = itemRotation * pivotToCenter;
                        Vector3 itemCenter      = itemPosition + pivotToCenter;
                        Vector3 itemHalfExtends = Vector3.Scale( itemBounds.size / 2, strokeItem.scaleMultiplier );
                        Collider[] overlaped = Physics.OverlapBox( itemCenter, itemHalfExtends,
                                                          itemRotation, -1, QueryTriggerInteraction.Ignore )
                                                      .Where( c => c != itemHit.collider && IsVisible( c.gameObject ) ).ToArray();
                        if ( overlaped.Length > 0 )
                        {
                            continue;
                        }
                    }

                    Transform surface = null;

                    GameObject colObj = null;
                    if ( itemHit.collider != null )
                    {
                        colObj = PWBCore.GetGameObjectFromTempCollider( itemHit.collider.gameObject );
                    }

                    if ( colObj != null )
                    {
                        surface = colObj.transform;
                    }

                    int       layer           = settings.overwritePrefabLayer ? settings.layer : prefab.layer;
                    Transform parentTransform = GetParent( settings, prefab.name, false, surface );
                    _paintStroke.Add( new PaintStrokeItem( prefab, itemPosition,
                        itemRotation * Quaternion.Euler( prefab.transform.eulerAngles ),
                        itemScale, layer, parentTransform, surface, strokeItem.flipX, strokeItem.flipY ) );
                    if ( settings.showPreview )
                    {
                        Matrix4x4 rootToWorld = Matrix4x4.TRS( itemPosition, itemRotation, strokeItem.scaleMultiplier )
                                                * Matrix4x4.Translate( -prefab.transform.position );
                        PreviewBrushItem( prefab, rootToWorld, layer, camera, false, false, strokeItem.flipX, strokeItem.flipY );
                    }
                }
            }
        }

        private static void DrawBrush( SceneView sceneView, RaycastHit hit, bool preview )
        {
            BrushToolSettings settings = BrushManager.settings;
            UpdateStrokeDirection( hit.point );
            if ( PaletteManager.selectedBrush == null )
            {
                return;
            }

            PWBCore.UpdateTempCollidersIfHierarchyChanged();
            hit.point = SnapAndUpdateGridOrigin( hit.point, SnapManager.settings.snappingEnabled,
                settings.paintOnPalettePrefabs,             settings.paintOnMeshesWithoutCollider, false, Vector3.down );

            Vector3 tangent   = GetTangent( hit.normal );
            Vector3 bitangent = Vector3.Cross( hit.normal, tangent );

            if ( settings.brushShape == BrushToolBase.BrushShape.POINT )
            {
                DrawCricleIndicator( hit.point, hit.normal, 0.1f,       settings.maxHeightFromCenter,
                    tangent,                    bitangent,  hit.normal, settings.paintOnPalettePrefabs, true,
                    settings.layerFilter,       settings.tagFilter.ToArray() );
            }
            else
            {
                Handles.zTest = CompareFunction.Always;
                Handles.color = Color.green;
                Handles.DrawAAPolyLine( 3, hit.point, hit.point + hit.normal * settings.maxHeightFromCenter );
                if ( settings.brushShape == BrushToolBase.BrushShape.CIRCLE )
                {
                    DrawCricleIndicator( hit.point, hit.normal, settings.radius,                settings.maxHeightFromCenter, tangent,
                        bitangent,                  hit.normal, settings.paintOnPalettePrefabs, true,
                        settings.layerFilter,       settings.tagFilter.ToArray() );
                }
                else if ( settings.brushShape == BrushToolBase.BrushShape.SQUARE )
                {
                    DrawSquareIndicator( hit.point, hit.normal, settings.radius,                settings.maxHeightFromCenter, tangent,
                        bitangent,                  hit.normal, settings.paintOnPalettePrefabs, true,
                        settings.layerFilter,       settings.tagFilter.ToArray() );
                }
            }

            if ( preview )
            {
                BrushstrokePreview( hit.point, hit.normal, tangent, bitangent, sceneView );
            }
        }

        private static Vector3 GetTangent( Vector3 normal )
        {
            Quaternion rotation = Quaternion.AngleAxis( _brushAngle, Vector3.up );
            Vector3    tangent  = Vector3.Cross( normal, rotation * Vector3.right );
            if ( tangent.sqrMagnitude < 0.000001 )
            {
                tangent = Vector3.Cross( normal, rotation * Vector3.forward );
            }

            tangent.Normalize();
            return tangent;
        }

        #endregion

    }

    #endregion

}