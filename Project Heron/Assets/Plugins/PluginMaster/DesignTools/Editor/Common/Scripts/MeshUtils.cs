using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PluginMaster
{
    public static class MeshUtils
    {

        #region Statics and Constants

        private static MethodInfo intersectRayMesh;

        #endregion

        #region Public Methods

        public static Collider AddCollider( Mesh mesh, GameObject target )
        {
            Collider collider = null;

            void AddMeshCollider()
            {
                MeshCollider meshCollider = target.AddComponent<MeshCollider>();
                meshCollider.sharedMesh = mesh;
                collider                = meshCollider;
            }

            if ( IsPrimitive( mesh ) )
            {
                if ( mesh.name == "Sphere" )
                {
                    collider = target.AddComponent<SphereCollider>();
                }
                else if ( mesh.name == "Capsule" )
                {
                    collider = target.AddComponent<CapsuleCollider>();
                }
                else if ( mesh.name == "Cube" )
                {
                    collider = target.AddComponent<BoxCollider>();
                }
                else if ( mesh.name == "Plane" )
                {
                    AddMeshCollider();
                }
            }
            else
            {
                AddMeshCollider();
            }

            return collider;
        }

        public static GameObject[] FindFilters( LayerMask mask, GameObject[] exclude = null, bool excludeColliders = true )
        {
            HashSet<GameObject> objects = new HashSet<GameObject>();
            #if UNITY_2022_2_OR_NEWER
            MeshFilter[]          meshFilters   = Object.FindObjectsByType<MeshFilter>( FindObjectsSortMode.None );
            SkinnedMeshRenderer[] skinnedMeshes = Object.FindObjectsByType<SkinnedMeshRenderer>( FindObjectsSortMode.None );
            #else
            var meshFilters = GameObject.FindObjectsOfType<MeshFilter>();
            var skinnedMeshes = GameObject.FindObjectsOfType<SkinnedMeshRenderer>();
            #endif
            objects.UnionWith( meshFilters.Select( comp => comp.gameObject ) );
            objects.UnionWith( skinnedMeshes.Select( comp => comp.gameObject ) );

            List<GameObject> filterList = new List<GameObject>( objects );
            if ( exclude != null )
            {
                filterList = new List<GameObject>( objects.Except( exclude ) );
                objects    = new HashSet<GameObject>( filterList );
            }

            if ( excludeColliders )
            {
                #if UNITY_2022_2_OR_NEWER
                Collider[] colliders = Object.FindObjectsByType<Collider>( FindObjectsSortMode.None );
                #else
                var colliders = GameObject.FindObjectsOfType<Collider>();
                #endif
                HashSet<GameObject> collidersSet
                    = new HashSet<GameObject>( colliders.Select( comp => comp.gameObject ) );
                filterList = new List<GameObject>( objects.Except( collidersSet ) );
            }

            filterList = filterList.Where( obj => ( mask.value & ( 1 << obj.layer ) ) != 0 ).ToList();
            return filterList.ToArray();
        }

        public static bool IsPrimitive( Mesh mesh )
        {
            string assetPath = AssetDatabase.GetAssetPath( mesh );
            return assetPath.Length < 6 ? false : assetPath.Substring( 0, 6 ) != "Assets";
        }

        public static bool Raycast( Ray            ray,      out RaycastHit hitInfo,
                                    out GameObject collider, GameObject[]   filters, float maxDistance )
        {
            collider = null;
            hitInfo  = new RaycastHit();
            if ( intersectRayMesh == null )
            {
                Type[] editorTypes        = typeof(UnityEditor.Editor).Assembly.GetTypes();
                Type   type_HandleUtility = editorTypes.FirstOrDefault( t => t.Name == "HandleUtility" );
                intersectRayMesh = type_HandleUtility.GetMethod( "IntersectRayMesh",
                    BindingFlags.Static | BindingFlags.NonPublic );
            }

            float minDistance = float.MaxValue;
            bool  result      = false;
            foreach ( GameObject filter in filters )
            {
                if ( filter == null )
                {
                    continue;
                }

                MeshFilter meshFilter = filter.GetComponent<MeshFilter>();
                Mesh       mesh;
                if ( meshFilter == null )
                {
                    SkinnedMeshRenderer skinnedMeshRenderer = filter.GetComponent<SkinnedMeshRenderer>();
                    if ( skinnedMeshRenderer == null )
                    {
                        continue;
                    }

                    mesh = skinnedMeshRenderer.sharedMesh;
                }
                else if ( meshFilter.sharedMesh == null )
                {
                    continue;
                }
                else
                {
                    mesh = meshFilter.sharedMesh;
                }

                object[] parameters = { ray, mesh, filter.transform.localToWorldMatrix, null };
                if ( (bool)intersectRayMesh.Invoke( null, parameters ) )
                {
                    if ( hitInfo.distance > maxDistance )
                    {
                        continue;
                    }

                    result = true;
                    RaycastHit hit = (RaycastHit)parameters[ 3 ];
                    if ( hit.distance < minDistance )
                    {
                        collider    = filter;
                        minDistance = hit.distance;
                        hitInfo     = hit;
                    }
                }
            }

            if ( result )
            {
                hitInfo.normal = hitInfo.normal.normalized;
            }

            return result;
        }

        public static bool Raycast( Vector3        origin,  Vector3        direction,
                                    out RaycastHit hitInfo, out GameObject collider, GameObject[] filters, float maxDistance )
        {
            Ray ray = new Ray( origin, direction );
            return Raycast( ray, out hitInfo, out collider, filters, maxDistance );
        }

        public static bool RaycastAll( Ray              ray,       out RaycastHit[] hitInfo,
                                       out GameObject[] colliders, GameObject[]     filters, float maxDistance )
        {
            List<RaycastHit> hitInfoList  = new List<RaycastHit>();
            List<GameObject> colliderList = new List<GameObject>();
            if ( intersectRayMesh == null )
            {
                Type[] editorTypes        = typeof(UnityEditor.Editor).Assembly.GetTypes();
                Type   type_HandleUtility = editorTypes.FirstOrDefault( t => t.Name == "HandleUtility" );
                intersectRayMesh = type_HandleUtility.GetMethod( "IntersectRayMesh",
                    BindingFlags.Static | BindingFlags.NonPublic );
            }

            foreach ( GameObject filter in filters )
            {
                if ( filter == null )
                {
                    continue;
                }

                MeshFilter meshFilter = filter.GetComponent<MeshFilter>();
                Mesh       mesh;
                if ( meshFilter == null )
                {
                    SkinnedMeshRenderer skinnedMeshRenderer = filter.GetComponent<SkinnedMeshRenderer>();
                    if ( skinnedMeshRenderer == null )
                    {
                        continue;
                    }

                    mesh = skinnedMeshRenderer.sharedMesh;
                }
                else if ( meshFilter.sharedMesh == null )
                {
                    continue;
                }
                else
                {
                    mesh = meshFilter.sharedMesh;
                }

                object[] parameters = { ray, mesh, filter.transform.localToWorldMatrix, null };
                if ( (bool)intersectRayMesh.Invoke( null, parameters ) )
                {
                    RaycastHit hit = (RaycastHit)parameters[ 3 ];
                    if ( hit.distance > maxDistance )
                    {
                        continue;
                    }

                    hitInfoList.Add( hit );
                    colliderList.Add( filter );
                }
            }

            hitInfo   = hitInfoList.ToArray();
            colliders = colliderList.ToArray();
            return hitInfoList.Count > 0;
        }

        #endregion

    }
}