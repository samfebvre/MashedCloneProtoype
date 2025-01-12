using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PluginMaster
{
    [Serializable]
    public class SimulateGravityData
    {

        #region Serialized

        [SerializeField] private int     _maxIterations = 1000;
        [SerializeField] private Vector3 _gravity       = Physics.gravity;
        [SerializeField] private float   _drag;
        [SerializeField] private float   _angularDrag     = 0.05f;
        [SerializeField] private float   _maxSpeed        = 100;
        [SerializeField] private float   _maxAngularSpeed = 10;
        [SerializeField] private float   _mass            = 1f;
        [SerializeField] private bool    _changeLayer;
        [SerializeField] private int     _tempLayer;
        [SerializeField] private bool    _ignoreSceneColliders;

        #endregion

        #region Public Properties

        public float angularDrag
        {
            get => _angularDrag;
            set
            {
                value = Mathf.Max( value, 0f );
                if ( _angularDrag == value )
                {
                    return;
                }

                _angularDrag = value;
            }
        }

        public bool changeLayer
        {
            get => _changeLayer;
            set
            {
                if ( _changeLayer == value )
                {
                    return;
                }

                _changeLayer = value;
            }
        }

        public float drag
        {
            get => _drag;
            set
            {
                value = Mathf.Max( value, 0f );
                if ( _drag == value )
                {
                    return;
                }

                _drag = value;
            }
        }

        public Vector3 gravity
        {
            get => _gravity;
            set
            {
                if ( _gravity == value )
                {
                    return;
                }

                _gravity = value;
            }
        }

        public bool ignoreSceneColliders
        {
            get => _ignoreSceneColliders;
            set
            {
                if ( _ignoreSceneColliders == value )
                {
                    return;
                }

                _ignoreSceneColliders = value;
            }
        }

        public float mass
        {
            get => _mass;
            set
            {
                value = Mathf.Max( value, 1e-7f );
                if ( _mass == value )
                {
                    return;
                }

                _mass = value;
            }
        }

        public float maxAngularSpeed
        {
            get => _maxAngularSpeed;
            set
            {
                value = Mathf.Max( value, 0f );
                if ( _maxAngularSpeed == value )
                {
                    return;
                }

                _maxAngularSpeed       = value;
                maxAngularSpeedSquared = _maxAngularSpeed * _maxAngularSpeed;
            }
        }

        public float maxAngularSpeedSquared { get; private set; } = 100;

        public int maxIterations
        {
            get => _maxIterations;
            set
            {
                value = Mathf.Clamp( value, 1, 100000 );
                if ( _maxIterations == value )
                {
                    return;
                }

                _maxIterations = value;
            }
        }

        public float maxSpeed
        {
            get => _maxSpeed;
            set
            {
                value = Mathf.Max( value, 0f );
                if ( _maxSpeed == value )
                {
                    return;
                }

                _maxSpeed       = value;
                maxSpeedSquared = _maxSpeed * _maxSpeed;
            }
        }

        public float maxSpeedSquared { get; private set; } = 10000;

        public int tempLayer
        {
            get => _tempLayer;
            set
            {
                if ( _tempLayer == value )
                {
                    return;
                }

                _tempLayer = value;
            }
        }

        #endregion

        #region Public Methods

        public void Copy( SimulateGravityData other )
        {
            _maxIterations         = other._maxIterations;
            _gravity               = other._gravity;
            _drag                  = other._drag;
            _angularDrag           = other._angularDrag;
            _maxSpeed              = other._maxSpeed;
            maxSpeedSquared        = other.maxSpeedSquared;
            _maxAngularSpeed       = other._maxAngularSpeed;
            maxAngularSpeedSquared = other.maxAngularSpeedSquared;
            _mass                  = other._mass;
            _changeLayer           = other._changeLayer;
            _tempLayer             = other._tempLayer;
            _ignoreSceneColliders  = other._ignoreSceneColliders;
        }

        #endregion

        #region Private Fields

        #endregion

    }

    public static class GravityUtils
    {

        #region Statics and Constants

        private static bool _isPlaying;
        private static bool _stop;

        #endregion

        #region Public Methods

        public static Pose[] SimulateGravity( GameObject[] selection, SimulateGravityData simData, bool recordAction )
        {

            if ( _isPlaying && recordAction )
            {
                return null;
            }

            List<Collider> tempColliders = null;

            Collider[] sceneColliders     = null;
            Collider[] invisibleColliders = null;

            if ( simData.ignoreSceneColliders )
            {
                #if UNITY_2022_2_OR_NEWER
                sceneColliders = Object.FindObjectsByType<Collider>( FindObjectsSortMode.None )
                                       .Where( sc => sc.enabled && sc.gameObject.activeInHierarchy && !sc.isTrigger ).ToArray();
                #else
                sceneColliders = Object.FindObjectsOfType<Collider>()
                    .Where(sc => sc.enabled && sc.gameObject.activeInHierarchy && !sc.isTrigger).ToArray();
                #endif
                foreach ( Collider sceneCollider in sceneColliders )
                {
                    sceneCollider.enabled = false;
                }
                #if UNITY_2022_2_OR_NEWER
                MeshFilter[] sceneMeshFilters = Object.FindObjectsByType<MeshFilter>( FindObjectsSortMode.None )
                                                      .Where( mf => mf.gameObject.activeInHierarchy && mf.sharedMesh != null ).ToArray();
                #else
                var sceneMeshFilters = Object.FindObjectsOfType<MeshFilter>()
                    .Where(mf => mf.gameObject.activeInHierarchy && mf.sharedMesh != null).ToArray();
                #endif
                tempColliders = new List<Collider>();
                foreach ( MeshFilter meshFilter in sceneMeshFilters )
                {
                    Mesh     mesh         = meshFilter.sharedMesh;
                    Collider tempCollider = MeshUtils.AddCollider( meshFilter.sharedMesh, meshFilter.gameObject );
                    if ( tempCollider != null )
                    {
                        tempColliders.Add( tempCollider );
                    }
                }
            }
            else
            {
                #if UNITY_2022_2_OR_NEWER
                invisibleColliders = Object.FindObjectsByType<Collider>( FindObjectsSortMode.None )
                                           .Where( sc => sc.enabled && sc.gameObject.activeInHierarchy && !IsVisible( sc.gameObject ) ).ToArray();
                #else
                invisibleColliders = Object.FindObjectsOfType<Collider>()
                  .Where(sc => sc.enabled && sc.gameObject.activeInHierarchy && !IsVisible(sc.gameObject)).ToArray();
                #endif
                foreach ( Collider invisibleCollider in invisibleColliders )
                {
                    invisibleCollider.enabled = false;
                }
            }

            Vector3 originalGravity = Physics.gravity;
            Physics.gravity = simData.gravity;
            #if UNITY_2022_2_OR_NEWER
            Rigidbody[] allBodies = Object.FindObjectsByType<Rigidbody>( FindObjectsSortMode.None );
            #else
            var allBodies = Object.FindObjectsOfType<Rigidbody>();
            #endif
            List<(Rigidbody body, bool useGravity, bool isKinematic, float drag, float angularDrag, float mass, RigidbodyConstraints constraints, RigidbodyInterpolation interpolation,
                CollisionDetectionMode collisionDetectionMode)> originalBodies = new List<(Rigidbody body, bool useGravity, bool isKinematic,
                float drag, float angularDrag, float mass, RigidbodyConstraints constraints,
                RigidbodyInterpolation interpolation, CollisionDetectionMode collisionDetectionMode)>();
            foreach ( Rigidbody rigidBody in allBodies )
            {
                originalBodies.Add( ( rigidBody, rigidBody.useGravity, rigidBody.isKinematic,
                    rigidBody.drag, rigidBody.angularDrag, rigidBody.mass, rigidBody.constraints,
                    rigidBody.interpolation, rigidBody.collisionDetectionMode ) );
                rigidBody.useGravity             = false;
                rigidBody.isKinematic            = true;
                rigidBody.drag                   = simData.drag;
                rigidBody.angularDrag            = simData.angularDrag;
                rigidBody.mass                   = simData.mass;
                rigidBody.constraints            = RigidbodyConstraints.None;
                rigidBody.interpolation          = RigidbodyInterpolation.None;
                rigidBody.collisionDetectionMode = CollisionDetectionMode.Discrete;
            }

            List<Rigidbody> simBodies = new List<Rigidbody>();

            GameObject[] clones = new GameObject[ selection.Length ];

            List<AnimData> animData = new List<AnimData>();

            void AddColliders( GameObject source, GameObject dest )
            {
                MeshFilter[] meshFilters = source.GetComponents<MeshFilter>();
                foreach ( MeshFilter meshFilter in meshFilters )
                {
                    Mesh mesh = meshFilter.sharedMesh;
                    if ( mesh == null )
                    {
                        continue;
                    }

                    AddCollider( dest, mesh, simData );
                }

                SkinnedMeshRenderer[] skinnedMeshRenderers = source.GetComponents<SkinnedMeshRenderer>();
                foreach ( SkinnedMeshRenderer renderer in skinnedMeshRenderers )
                {
                    Mesh mesh = renderer.sharedMesh;
                    if ( mesh == null )
                    {
                        continue;
                    }

                    AddCollider( dest, mesh, simData );
                }

                foreach ( Transform child in source.transform )
                {
                    GameObject destChild = new GameObject();
                    destChild.transform.SetParent( dest.transform );
                    destChild.transform.localPosition = child.localPosition;
                    destChild.transform.localRotation = child.localRotation;
                    destChild.transform.localScale    = child.localScale;
                    AddColliders( child.gameObject, destChild );
                }
            }

            for ( int i = 0; i < selection.Length; ++i )
            {
                animData.Add( new AnimData( selection[ i ] ) );
                GameObject obj = new GameObject();
                obj.layer                = selection[ i ].layer;
                obj.transform.position   = selection[ i ].transform.position;
                obj.transform.rotation   = selection[ i ].transform.rotation;
                obj.transform.localScale = selection[ i ].transform.lossyScale;
                AddColliders( selection[ i ], obj );
                float magnitude = BoundsUtils.GetMagnitude( selection[ i ].transform );
                selection[ i ].transform.position += Vector3.up * ( 100 * magnitude );
                Rigidbody rigidBody = obj.AddComponent<Rigidbody>();
                if ( simData.changeLayer )
                {
                    obj.layer = simData.tempLayer;
                }

                simBodies.Add( rigidBody );
                rigidBody.useGravity  = true;
                rigidBody.isKinematic = false;
                clones[ i ]           = obj;
            }

            #if UNITY_2022_2_OR_NEWER
            SimulationMode prevSimMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
            #else
            Physics.autoSimulation = false;
            #endif
            for ( int i = 0; i < simData.maxIterations; ++i )
            {
                Physics.Simulate( Time.fixedDeltaTime );
                for ( int objIdx = 0; objIdx < selection.Length; ++objIdx )
                {
                    Rigidbody body = simBodies[ objIdx ];
                    if ( body.velocity.sqrMagnitude > simData.maxSpeedSquared )
                    {
                        body.velocity = body.velocity.normalized * simData.maxSpeed;
                    }

                    if ( body.angularVelocity.sqrMagnitude > simData.maxAngularSpeedSquared )
                    {
                        body.angularVelocity = body.angularVelocity.normalized * simData.maxAngularSpeed;
                    }

                    if ( i % 10 == 0 )
                    {
                        animData[ objIdx ].poses.Add( new Pose( body.position, body.rotation ) );
                    }
                }

                if ( simBodies.All( rb => rb.IsSleeping() ) )
                {
                    break;
                }
            }
            #if UNITY_2022_2_OR_NEWER
            Physics.simulationMode = prevSimMode;
            #else
            Physics.autoSimulation = true;
            #endif

            for ( int i = 0; i < selection.Length; ++i )
            {
                selection[ i ].transform.position = clones[ i ].transform.position;
                selection[ i ].transform.rotation = clones[ i ].transform.rotation;
                animData[ i ].poses.Add( new Pose( selection[ i ].transform.position, selection[ i ].transform.rotation ) );
                Object.DestroyImmediate( clones[ i ] );
            }

            foreach ( (Rigidbody body, bool useGravity, bool isKinematic, float drag, float angularDrag, float mass, RigidbodyConstraints constraints, RigidbodyInterpolation interpolation,
                     CollisionDetectionMode collisionDetectionMode) item in originalBodies )
            {
                if ( item.body == null )
                {
                    continue;
                }

                item.body.useGravity             = item.useGravity;
                item.body.isKinematic            = item.isKinematic;
                item.body.drag                   = item.drag;
                item.body.angularDrag            = item.angularDrag;
                item.body.mass                   = item.mass;
                item.body.constraints            = item.constraints;
                item.body.interpolation          = item.interpolation;
                item.body.collisionDetectionMode = item.collisionDetectionMode;
            }

            Physics.gravity = originalGravity;

            if ( simData.ignoreSceneColliders )
            {
                foreach ( Collider sceneCollider in sceneColliders )
                {
                    sceneCollider.enabled = true;
                }

                foreach ( Collider tempCollider in tempColliders )
                {
                    Object.DestroyImmediate( tempCollider );
                }
            }
            else
            {
                foreach ( Collider invisibleCollider in invisibleColliders )
                {
                    invisibleCollider.enabled = true;
                }
            }

            Animate( animData, simData, recordAction );
            List<Pose> finalPoses = new List<Pose>();
            foreach ( AnimData d in animData )
            {
                finalPoses.Add( d.poses.Last() );
            }

            return finalPoses.ToArray();
        }

        #endregion

        #region Private Methods

        private static void AddCollider( GameObject obj, Mesh mesh, SimulateGravityData data )
        {
            Collider collider = MeshUtils.AddCollider( mesh, obj );
            if ( collider is MeshCollider )
            {
                ( collider as MeshCollider ).convex = true;
            }

            if ( data.changeLayer )
            {
                obj.layer = data.tempLayer;
            }
        }

        private static void Animate( List<AnimData>      animData,
                                     SimulateGravityData simData, bool recordAction )
        {
            _stop      = false;
            _isPlaying = true;
            foreach ( AnimData item in animData )
            {
                item.enabledColliders = item.obj.GetComponentsInChildren<Collider>()
                                            .Where( collider => collider.enabled ).ToList();
                item.tempColliders.Clear();
                GameObject temp = new GameObject();
                temp.hideFlags = HideFlags.HideAndDontSave;
                Pose lastPose = item.poses.Last();
                temp.transform.position   = lastPose.position;
                temp.transform.rotation   = lastPose.rotation;
                temp.transform.localScale = item.obj.transform.lossyScale;

                MeshFilter[] meshFilters = item.obj.GetComponentsInChildren<MeshFilter>();
                foreach ( MeshFilter meshFilter in meshFilters )
                {
                    Mesh mesh = meshFilter.sharedMesh;
                    if ( mesh == null )
                    {
                        continue;
                    }

                    AddCollider( temp, mesh, simData );
                }

                SkinnedMeshRenderer[] skinnedMeshRenderers = item.obj.GetComponentsInChildren<SkinnedMeshRenderer>();
                foreach ( SkinnedMeshRenderer renderer in skinnedMeshRenderers )
                {
                    Mesh mesh = renderer.sharedMesh;
                    if ( mesh == null )
                    {
                        continue;
                    }

                    MeshFilter meshFilter = renderer.gameObject.AddComponent<MeshFilter>();
                    meshFilter.sharedMesh = mesh;
                    AddCollider( temp, mesh, simData );
                }

                item.tempColliders.Add( temp );

                foreach ( Collider collider in item.enabledColliders )
                {
                    collider.enabled = false;
                }
            }

            Animate( animData, 0, recordAction );
        }

        private static async void Animate( List<AnimData> data, int frame, bool recordAction )
        {
            void EnableColliders( AnimData item )
            {
                foreach ( Collider collider in item.enabledColliders )
                {
                    if ( collider == null )
                    {
                        continue;
                    }

                    collider.enabled = true;
                }
            }

            void DestroyTempColliders( AnimData item )
            {
                foreach ( GameObject temp in item.tempColliders )
                {
                    Object.DestroyImmediate( temp );
                }
            }

            void Record()
            {
                foreach ( AnimData item in data )
                {
                    if ( item.obj == null )
                    {
                        continue;
                    }

                    EnableColliders( item );
                    DestroyTempColliders( item );
                    item.obj.transform.position = item.poses.First().position;
                    item.obj.transform.rotation = item.poses.First().rotation;
                    Undo.RecordObject( item.obj.transform, "Simulate Gravity" );
                    item.obj.transform.position = item.poses.Last().position;
                    item.obj.transform.rotation = item.poses.Last().rotation;
                }
            }

            bool isPlaying = false;
            if ( _stop )
            {
                if ( recordAction )
                {
                    Record();
                }

                return;
            }

            foreach ( AnimData item in data )
            {
                if ( item.obj == null )
                {
                    break;
                }

                if ( frame >= item.poses.Count )
                {
                    item.obj.transform.position = item.poses.Last().position;
                    item.obj.transform.rotation = item.poses.Last().rotation;
                    continue;
                }

                isPlaying                   = true;
                item.obj.transform.position = item.poses[ frame ].position;
                item.obj.transform.rotation = item.poses[ frame ].rotation;
            }

            if ( isPlaying )
            {
                await Task.Delay( (int)( Time.fixedDeltaTime * 1000 ) );
                Animate( data, frame + 1, recordAction );
            }
            else
            {
                if ( recordAction )
                {
                    Record();
                }
                else
                {
                    foreach ( AnimData item in data )
                    {
                        if ( item.obj == null )
                        {
                            continue;
                        }

                        EnableColliders( item );
                        DestroyTempColliders( item );
                    }
                }

                _isPlaying = false;
            }
        }

        private static bool IsVisible( GameObject obj )
        {
            if ( obj == null )
            {
                return false;
            }

            GameObject target         = obj;
            Renderer   parentRenderer = target.GetComponentInParent<Renderer>();
            Terrain    parentTerrain  = target.GetComponentInParent<Terrain>();
            if ( parentRenderer != null )
            {
                target = parentRenderer.gameObject;
            }
            else if ( parentTerrain != null )
            {
                target = parentTerrain.gameObject;
            }
            else
            {
                Transform parent = target.transform.parent;
                if ( parent != null )
                {
                    Renderer siblingRenderer = parent.GetComponentInChildren<Renderer>();
                    Terrain  siblingTerrain  = parent.GetComponentInChildren<Terrain>();
                    if ( siblingRenderer != null )
                    {
                        target = parent.gameObject;
                    }
                    else if ( siblingTerrain != null )
                    {
                        target = parent.gameObject;
                    }

                }
            }

            Renderer[] renderers = target.GetComponentsInChildren<Renderer>();
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

            Terrain[] terrains = target.GetComponentsInChildren<Terrain>();
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

        #endregion

        private class AnimData
        {

            #region Public Fields

            public List<Collider> enabledColliders
                = new List<Collider>();

            public GameObject obj;
            public List<Pose> poses = new List<Pose>();

            public List<GameObject> tempColliders
                = new List<GameObject>();

            #endregion

            #region Public Constructors

            public AnimData( GameObject obj )
            {
                this.obj = obj;
            }

            #endregion

        }

        [InitializeOnLoad]
        private static class UndoEventHandler
        {

            #region Private Constructors

            static UndoEventHandler()
            {
                Undo.undoRedoPerformed += OnUndoRedoPerformed;
            }

            #endregion

            #region Private Methods

            private static void OnUndoRedoPerformed()
            {
                _isPlaying = false;
                _stop      = true;
            }

            #endregion

        }
    }
}