using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace PluginMaster
{

    #region DATA & SETTIGNS

    [Serializable]
    public class EraserSettings : CircleToolBase, IModifierTool
    {

        #region Serialized

        [SerializeField] private ModifierToolSettings _modifierTool = new ModifierToolSettings();

        [SerializeField] private bool _outermostPrefabFilter = true;

        #endregion

        #region Public Properties

        public ModifierToolSettings.Command command
        {
            get => _modifierTool.command;
            set => _modifierTool.command = value;
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

        #endregion

        #region Public Constructors

        public EraserSettings()
        {
            _modifierTool.OnDataChanged += DataChanged;
        }

        #endregion

        #region Public Methods

        public override void Copy( IToolSettings other )
        {
            EraserSettings otherEraserSettings = other as EraserSettings;
            if ( otherEraserSettings == null )
            {
                return;
            }

            base.Copy( other );
            _modifierTool.Copy( otherEraserSettings );
            _outermostPrefabFilter = otherEraserSettings._outermostPrefabFilter;
        }

        #endregion

    }

    [Serializable]
    public class EraserManager : ToolManagerBase<EraserSettings>
    {
    }

    #endregion

    #region PWBIO

    public static partial class PWBIO
    {

        #region Statics and Constants

        private static float _lastHitDistance = 20f;

        private static Material _transparentRedMaterial;

        private static List<GameObject> _toErase
            = new List<GameObject>();

        #endregion

        #region Public Properties

        public static Material transparentRedMaterial
        {
            get
            {
                if ( _transparentRedMaterial == null )
                {
                    _transparentRedMaterial = new Material( Shader.Find( "PluginMaster/TransparentRed" ) );
                }

                return _transparentRedMaterial;
            }
        }

        #endregion

        #region Private Methods

        private static void DrawEraserCircle( Vector3 center, Ray mouseRay, Camera camera )
        {
            Handles.zTest = CompareFunction.Always;
            Handles.color = new Color( 1f, 0f, 0f, 0.6f );

            const float polygonSideSize = 0.3f;
            const int   minPolygonSides = 8;
            const int   maxPolygonSides = 60;
            int polygonSides = Mathf.Clamp( (int)( TAU * EraserManager.settings.radius / polygonSideSize ),
                minPolygonSides, maxPolygonSides );

            List<Vector3> periPoints = new List<Vector3>();
            for ( int i = 0; i < polygonSides; ++i )
            {
                float   radians    = TAU * i / ( polygonSides - 1f );
                Vector2 tangentDir = new Vector2( Mathf.Cos( radians ), Mathf.Sin( radians ) );
                Vector3 worldDir   = TangentSpaceToWorld( camera.transform.right, camera.transform.up, tangentDir );
                periPoints.Add( center + worldDir * EraserManager.settings.radius );
            }

            Handles.DrawAAPolyLine( 6, periPoints.ToArray() );

            IEnumerable<GameObject> nearbyObjects = octree.GetNearby( mouseRay, EraserManager.settings.radius ).Where( o => o != null );

            _toErase.Clear();
            if ( EraserManager.settings.outermostPrefabFilter )
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
                        _toErase.Add( nearby );
                    }
                    else if ( !_toErase.Contains( outermost ) )
                    {
                        _toErase.Add( outermost );
                    }
                }
            }
            else
            {
                _toErase.AddRange( nearbyObjects );
            }

            GameObject[] toErase = _toErase.ToArray();
            _toErase.Clear();

            float closestDistSqr = float.MaxValue;
            for ( int i = 0; i < toErase.Length; ++i )
            {
                GameObject obj = toErase[ i ];
                if ( obj == null )
                {
                    continue;
                }

                float magnitude = BoundsUtils.GetAverageMagnitude( obj.transform );
                if ( EraserManager.settings.radius < magnitude / 2 )
                {
                    continue;
                }

                if ( EraserManager.settings.onlyTheClosest )
                {
                    Vector3 pos     = obj.transform.position;
                    float   distSqr = ( pos - camera.transform.position ).sqrMagnitude;
                    if ( distSqr < closestDistSqr )
                    {
                        closestDistSqr = distSqr;
                        _toErase.Clear();
                        _toErase.Add( obj );
                    }

                    continue;
                }

                _toErase.Add( obj );
            }

            foreach ( GameObject obj in _toErase )
            {
                MeshFilter[] filters = obj.GetComponentsInChildren<MeshFilter>();
                foreach ( MeshFilter filter in filters )
                {
                    Mesh mesh = filter.sharedMesh;
                    if ( mesh == null )
                    {
                        continue;
                    }

                    for ( int subMeshIdx = 0; subMeshIdx < mesh.subMeshCount; ++subMeshIdx )
                    {
                        Graphics.DrawMesh( mesh, filter.transform.localToWorldMatrix,
                            transparentRedMaterial, 0, camera, subMeshIdx );
                    }
                }
            }
        }

        private static void Erase()
        {
            void EraseObject( GameObject obj )
            {
                if ( EraserManager.settings.outermostPrefabFilter )
                {
                    GameObject root = PrefabUtility.GetNearestPrefabInstanceRoot( obj );
                    if ( root != null )
                    {
                        obj = root;
                    }
                }
                else
                {
                    GameObject parent = obj.transform.parent.gameObject;
                    if ( parent != null )
                    {
                        GameObject outermost = null;
                        do
                        {
                            outermost = PrefabUtility.GetOutermostPrefabInstanceRoot( obj );
                            if ( outermost == null )
                            {
                                break;
                            }

                            if ( outermost == obj )
                            {
                                break;
                            }

                            PrefabUtility.UnpackPrefabInstance( outermost,
                                PrefabUnpackMode.OutermostRoot, InteractionMode.UserAction );
                        }
                        while ( outermost != parent );
                    }
                }

                PWBCore.DestroyTempCollider( obj.GetInstanceID() );
                Undo.DestroyObjectImmediate( obj );
            }

            for ( int i = 0; i < _toErase.Count; ++i )
            {
                GameObject obj = _toErase[ i ];
                if ( obj == null )
                {
                    continue;
                }

                EraseObject( obj );
            }

            _toErase.Clear();
        }

        private static void EraserDuringSceneGUI( SceneView sceneView )
        {
            EraserMouseEvents();
            Vector2 mousePos = Event.current.mousePosition;
            if ( _pinned )
            {
                mousePos = _pinMouse;
            }

            Ray mouseRay = HandleUtility.GUIPointToWorldRay( mousePos );

            Vector3 center = mouseRay.GetPoint( _lastHitDistance );
            if ( MouseRaycast( mouseRay, out RaycastHit mouseHit, out GameObject collider,
                    float.MaxValue,      -1,                      paintOnPalettePrefabs: true, castOnMeshesWithoutCollider: true ) )
            {
                _lastHitDistance = mouseHit.distance;
                center           = mouseHit.point;
                PWBCore.UpdateTempCollidersIfHierarchyChanged();
            }

            DrawEraserCircle( center, mouseRay, sceneView.camera );
        }

        private static void EraserMouseEvents()
        {
            if ( Event.current.button == 0
                 && !Event.current.alt
                 && ( Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag ) )
            {
                Erase();
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