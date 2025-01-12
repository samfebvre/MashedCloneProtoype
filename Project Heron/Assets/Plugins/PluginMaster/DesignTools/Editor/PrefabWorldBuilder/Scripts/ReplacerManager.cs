using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace PluginMaster
{

    #region DATA & SETTINGS

    [Serializable]
    public class ReplacerSettings : CircleToolBase, IModifierTool
    {

        #region Serialized

        [SerializeField] private ModifierToolSettings _modifierTool = new ModifierToolSettings();

        [SerializeField] private bool          _keepTargetSize;
        [SerializeField] private bool          _maintainProportions;
        [SerializeField] private bool          _outermostPrefabFilter = true;
        [SerializeField] private bool          _overwriteBrushProperties;
        [SerializeField] private BrushSettings _brushProperties = new BrushSettings();

        #endregion

        #region Public Properties

        public BrushSettings brushProperties => _brushProperties;

        public ModifierToolSettings.Command command
        {
            get => _modifierTool.command;
            set => _modifierTool.command = value;
        }

        public bool keepTargetSize
        {
            get => _keepTargetSize;
            set
            {
                if ( _keepTargetSize == value )
                {
                    return;
                }

                _keepTargetSize = value;
                DataChanged();
            }
        }

        public bool maintainProportions
        {
            get => _maintainProportions;
            set
            {
                if ( _maintainProportions == value )
                {
                    return;
                }

                _maintainProportions = value;
                DataChanged();
            }
        }

        public bool modifyAllButSelected
        {
            get => _modifierTool.modifyAllButSelected;
            set => _modifierTool.modifyAllButSelected = value;
        }

        public bool onlyTheClosest
        {
            get => _modifierTool.onlyTheClosest;
            set => _modifierTool.onlyTheClosest = value;
        }

        public bool outermostPrefabFilter
        {
            get => _outermostPrefabFilter;
            set
            {
                if ( _outermostPrefabFilter == value )
                {
                    return;
                }

                _outermostPrefabFilter = value;
                DataChanged();
            }
        }

        public bool overwriteBrushProperties
        {
            get => _overwriteBrushProperties;
            set
            {
                if ( _overwriteBrushProperties == value )
                {
                    return;
                }

                _overwriteBrushProperties = value;
                DataChanged();
            }
        }

        #endregion

        #region Public Constructors

        public ReplacerSettings()
        {
            _modifierTool.OnDataChanged += DataChanged;
        }

        #endregion

        #region Public Methods

        public override void Copy( IToolSettings other )
        {
            ReplacerSettings otherReplacer = other as ReplacerSettings;
            if ( otherReplacer == null )
            {
                return;
            }

            base.Copy( other );
            _modifierTool.Copy( otherReplacer );
            _keepTargetSize           = otherReplacer._keepTargetSize;
            _maintainProportions      = otherReplacer._maintainProportions;
            _outermostPrefabFilter    = otherReplacer._outermostPrefabFilter;
            _overwriteBrushProperties = otherReplacer._overwriteBrushProperties;
            _brushProperties.Copy( otherReplacer._brushProperties );
        }

        #endregion

    }

    [Serializable]
    public class ReplacerManager : ToolManagerBase<ReplacerSettings>
    {
    }

    #endregion

    #region PWBIO

    public static partial class PWBIO
    {

        #region Statics and Constants

        private static List<GameObject> _toReplace
            = new List<GameObject>();

        private static List<Renderer> _replaceRenderers
            = new List<Renderer>();

        private static bool _replaceAllSelected;

        #endregion

        #region Public Methods

        public static void ReplaceAllSelected()
        {
            _replaceAllSelected = true;
        }

        public static void ResetReplacer()
        {
            foreach ( Renderer renderer in _replaceRenderers )
            {
                if ( renderer == null )
                {
                    continue;
                }

                renderer.enabled = true;
            }

            _toReplace.Clear();
            _replaceRenderers.Clear();
            _paintStroke.Clear();
        }

        #endregion

        #region Private Methods

        private static void DrawReplacerCircle( Vector3 center, Ray mouseRay, Camera camera )
        {
            ReplacerSettings settings        = ReplacerManager.settings;
            const float      polygonSideSize = 0.3f;
            const int        minPolygonSides = 8;
            const int        maxPolygonSides = 60;
            int polygonSides = Mathf.Clamp( (int)( TAU * settings.radius / polygonSideSize ),
                minPolygonSides, maxPolygonSides );

            List<Vector3> periPoints = new List<Vector3>();
            for ( int i = 0; i < polygonSides; ++i )
            {
                float   radians    = TAU * i / ( polygonSides - 1f );
                Vector2 tangentDir = new Vector2( Mathf.Cos( radians ), Mathf.Sin( radians ) );
                Vector3 worldDir   = TangentSpaceToWorld( camera.transform.right, camera.transform.up, tangentDir );
                periPoints.Add( center + worldDir * settings.radius );
            }

            Handles.zTest = CompareFunction.Always;
            Handles.color = new Color( 0f, 0f, 0f, 1f );
            Handles.DrawAAPolyLine( 6, periPoints.ToArray() );
            Handles.color = new Color( 1f, 1f, 1f, 0.6f );
            Handles.DrawAAPolyLine( 4, periPoints.ToArray() );

            IEnumerable<GameObject> nearbyObjects = octree.GetNearby( mouseRay, settings.radius ).Where( o => o != null );

            _toReplace.Clear();
            _paintStroke.Clear();
            if ( settings.outermostPrefabFilter )
            {
                foreach ( GameObject nearby in nearbyObjects )
                {
                    if ( nearby == null )
                    {
                        continue;
                    }

                    GameObject outermost = PrefabUtility.GetOutermostPrefabInstanceRoot( nearby );
                    if ( outermost == null )
                    {
                        Component[] components = nearby.GetComponents<Component>();
                        if ( components.Length > 1 )
                        {
                            _toReplace.Add( nearby );
                        }
                    }
                    else if ( !_toReplace.Contains( outermost ) )
                    {
                        _toReplace.Add( outermost );
                    }
                }
            }
            else
            {
                _toReplace.AddRange( nearbyObjects );
            }

            GameObject[] toReplace = _toReplace.ToArray();
            _toReplace.Clear();
            float closestDistSqr = float.MaxValue;
            for ( int i = 0; i < toReplace.Length; ++i )
            {
                GameObject obj = toReplace[ i ];
                if ( obj == null )
                {
                    continue;
                }

                float magnitude = BoundsUtils.GetAverageMagnitude( obj.transform );
                if ( settings.radius < magnitude / 2 )
                {
                    continue;
                }

                if ( ReplacerManager.settings.onlyTheClosest )
                {
                    Vector3 pos     = obj.transform.position;
                    float   distSqr = ( pos - camera.transform.position ).sqrMagnitude;
                    if ( distSqr < closestDistSqr )
                    {
                        closestDistSqr = distSqr;
                        _toReplace.Clear();
                        _toReplace.Add( obj );
                    }

                    continue;
                }

                _toReplace.Add( obj );
            }

            foreach ( Renderer renderer in _replaceRenderers )
            {
                if ( renderer == null )
                {
                    continue;
                }

                renderer.enabled = true;
            }

            _replaceRenderers.Clear();
            toReplace = _toReplace.ToArray();
            _toReplace.Clear();
            for ( int i = 0; i < toReplace.Length; ++i )
            {
                GameObject obj     = toReplace[ i ];
                bool       isChild = false;
                foreach ( GameObject listed in toReplace )
                {
                    if ( obj.transform.IsChildOf( listed.transform )
                         && listed != obj )
                    {
                        isChild = true;
                        break;
                    }
                }

                if ( isChild )
                {
                    continue;
                }

                _toReplace.Add( obj );
                _replaceRenderers.AddRange( obj.GetComponentsInChildren<Renderer>().Where( r => r.enabled ) );
                ReplacePreview( camera, obj.transform );
                foreach ( Renderer renderer in _replaceRenderers )
                {
                    renderer.enabled = false;
                }

                BrushstrokeManager.UpdateBrushstroke();
            }
        }

        private static void Replace()
        {
            if ( _toReplace.Count == 0 )
            {
                return;
            }

            if ( _paintStroke.Count != _toReplace.Count )
            {
                return;
            }

            const string COMMAND_NAME = "Replace";
            foreach ( Renderer renderer in _replaceRenderers )
            {
                renderer.enabled = true;
            }

            _replaceRenderers.Clear();
            for ( int i = 0; i < _toReplace.Count; ++i )
            {
                GameObject target = _toReplace[ i ];
                if ( target == null )
                {
                    continue;
                }

                PaintStrokeItem item = _paintStroke[ i ];
                if ( item.prefab == null )
                {
                    continue;
                }

                if ( ReplacerManager.settings.outermostPrefabFilter )
                {
                    GameObject nearestRoot = PrefabUtility.GetNearestPrefabInstanceRoot( target );
                    if ( nearestRoot != null )
                    {
                        target = nearestRoot;
                    }
                }
                else
                {
                    GameObject parent = target.transform.parent.gameObject;
                    if ( parent != null )
                    {
                        GameObject outermost = null;
                        do
                        {
                            outermost = PrefabUtility.GetOutermostPrefabInstanceRoot( target );
                            if ( outermost == null )
                            {
                                break;
                            }

                            if ( outermost == target )
                            {
                                break;
                            }

                            PrefabUtility.UnpackPrefabInstance( outermost,
                                PrefabUnpackMode.OutermostRoot, InteractionMode.UserAction );
                        }
                        while ( outermost != parent );
                    }
                }

                PrefabAssetType type = PrefabUtility.GetPrefabAssetType( item.prefab );
                GameObject obj = type == PrefabAssetType.NotAPrefab
                    ? Object.Instantiate( item.prefab )
                    : (GameObject)PrefabUtility.InstantiatePrefab
                    ( PrefabUtility.IsPartOfPrefabAsset( item.prefab )
                        ? item.prefab
                        : PrefabUtility.GetCorrespondingObjectFromSource( item.prefab ) );
                obj.transform.SetPositionAndRotation( item.position, item.rotation );
                obj.transform.localScale = item.scale;
                GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot( obj );
                PWBCore.AddTempCollider( obj );
                AddPaintedObject( obj );

                if ( !LineManager.instance.ReplaceObject( target, obj ) )
                {
                    if ( !ShapeManager.instance.ReplaceObject( target, obj ) )
                    {
                        TilingManager.instance.ReplaceObject( target, obj );
                    }
                }

                BrushstrokeManager.UpdateBrushstroke();
                GameObject[] tempColliders = PWBCore.GetTempColliders( target );
                if ( tempColliders != null )
                {
                    foreach ( GameObject tempCollider in tempColliders )
                    {
                        Undo.DestroyObjectImmediate( tempCollider );
                    }
                }

                Undo.DestroyObjectImmediate( target );
                Undo.RegisterCreatedObjectUndo( obj, COMMAND_NAME );
                if ( root != null )
                {
                    Undo.SetTransformParent( root.transform, item.parent, COMMAND_NAME );
                }
                else
                {
                    Undo.SetTransformParent( obj.transform, item.parent, COMMAND_NAME );
                }
            }

            _paintStroke.Clear();
            _toReplace.Clear();
        }

        private static void ReplacePreview( Camera camera, Transform target )
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

            Quaternion itemRotation   = target.rotation;
            Bounds     targetBounds   = BoundsUtils.GetBoundsRecursive( target, target.rotation );
            Quaternion strokeRotation = Quaternion.Euler( strokeItem.additionalAngle );
            Vector3    scaleMult      = strokeItem.scaleMultiplier;
            if ( ReplacerManager.settings.overwriteBrushProperties )
            {
                BrushSettings brushSettings = ReplacerManager.settings.brushProperties;
                Vector3 additonalAngle = brushSettings.addRandomRotation
                    ? brushSettings.randomEulerOffset.randomVector
                    : brushSettings.eulerOffset;
                strokeRotation *= Quaternion.Euler( additonalAngle );
                scaleMult = brushSettings.randomScaleMultiplier
                    ? brushSettings.randomScaleMultiplierRange.randomVector
                    : brushSettings.scaleMultiplier;
            }

            Quaternion inverseStrokeRotation = Quaternion.Inverse( strokeRotation );
            itemRotation *= strokeRotation;
            Bounds itemBounds = BoundsUtils.GetBoundsRecursive( prefab.transform, prefab.transform.rotation * strokeRotation );

            if ( ReplacerManager.settings.keepTargetSize )
            {
                Vector3 targetSize = targetBounds.size;
                Vector3 itemSize   = itemBounds.size;

                if ( ReplacerManager.settings.maintainProportions )
                {
                    float targetMagnitude = Mathf.Max( targetSize.x, targetSize.y, targetSize.z );
                    float itemMagnitude   = Mathf.Max( itemSize.x,   itemSize.y,   itemSize.z );
                    scaleMult = inverseStrokeRotation * ( Vector3.one * ( targetMagnitude / itemMagnitude ) );
                }
                else
                {
                    scaleMult = inverseStrokeRotation
                                * new Vector3( targetSize.x / itemSize.x, targetSize.y / itemSize.y, targetSize.z / itemSize.z );
                }

                scaleMult = new Vector3( Mathf.Abs( scaleMult.x ), Mathf.Abs( scaleMult.y ), Mathf.Abs( scaleMult.z ) );
            }

            Vector3 itemPosition = targetBounds.center
                                   - itemRotation * Vector3.Scale( itemBounds.center - prefab.transform.position, scaleMult );

            Vector3 itemScale = Vector3.Scale( prefab.transform.localScale, scaleMult );

            int       layer           = target.gameObject.layer;
            Transform parentTransform = target.parent;
            _paintStroke.Add( new PaintStrokeItem( prefab, itemPosition,
                itemRotation * prefab.transform.rotation,
                itemScale, layer, parentTransform, null, false, false ) );
            Matrix4x4 rootToWorld = Matrix4x4.TRS( itemPosition, itemRotation, scaleMult )
                                    * Matrix4x4.Translate( -prefab.transform.position );
            PreviewBrushItem( prefab, rootToWorld, layer, camera, false, false, strokeItem.flipX, strokeItem.flipY );
        }

        private static void ReplacerDuringSceneGUI( SceneView sceneView )
        {
            if ( PaletteManager.selectedBrushIdx < 0 )
            {
                return;
            }

            if ( _replaceAllSelected )
            {
                BrushstrokeManager.UpdateBrushstroke();
                _paintStroke.Clear();
                _toReplace.Clear();
                _replaceAllSelected = false;
                _toReplace.AddRange( SelectionManager.topLevelSelection );
                foreach ( GameObject selected in _toReplace )
                {
                    ReplacePreview( sceneView.camera, selected.transform );
                }

                Replace();
                return;
            }

            ReplacerMouseEvents();

            Vector2 mousePos = Event.current.mousePosition;
            if ( _pinned )
            {
                mousePos = _pinMouse;
            }

            Ray mouseRay = HandleUtility.GUIPointToWorldRay( mousePos );

            Vector3 center = mouseRay.GetPoint( _lastHitDistance );
            if ( MouseRaycast( mouseRay, out RaycastHit mouseHit, out GameObject collider,
                    float.MaxValue,      -1,                      true, true ) )
            {
                _lastHitDistance = mouseHit.distance;
                center           = mouseHit.point;
                PWBCore.UpdateTempCollidersIfHierarchyChanged();
            }

            DrawReplacerCircle( center, mouseRay, sceneView.camera );

        }

        private static void ReplacerMouseEvents()
        {
            ReplacerSettings settings = ReplacerManager.settings;
            if ( Event.current.button == 0
                 && !Event.current.alt
                 && ( Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag ) )
            {
                Replace();
                Event.current.Use();
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
                else if ( Event.current.type == EventType.MouseUp )
                {
                    _pinned = false;
                }
            }
        }

        #endregion

    }

    #endregion

}