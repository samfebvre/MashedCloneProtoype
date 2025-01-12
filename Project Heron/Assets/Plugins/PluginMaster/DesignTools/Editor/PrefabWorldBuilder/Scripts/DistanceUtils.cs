#if !UNITY_2020_2_OR_NEWER
using System.Linq;
#endif
using System;
using UnityEditor;
using UnityEngine;

namespace PluginMaster
{
    public static class DistanceUtils
    {
        #if !UNITY_2020_2_OR_NEWER
        private static System.Reflection.MethodInfo findNearestVertex = null;
        #endif
        public static bool FindNearestVertexToMouse( out RaycastHit hit, Transform transform = null )
        {
            Ray mouseRay = HandleUtility.GUIPointToWorldRay( Event.current.mousePosition );
            hit          = new RaycastHit();
            hit.point    = mouseRay.origin;
            hit.normal   = Vector3.up;
            hit.distance = 0f;
            bool    result = false;
            Vector3 vertex = Vector3.zero;

            void SetHit( ref RaycastHit resultHit )
            {
                Ray vertexRay = new Ray( vertex - mouseRay.direction, mouseRay.direction );
                if ( Physics.Raycast( vertexRay, out RaycastHit rayHit ) )
                {
                    if ( transform == null )
                    {
                        transform = rayHit.collider.transform;
                    }

                    resultHit.normal = rayHit.normal;
                }
                else if ( transform != null )
                {
                    Vector3 normal = GetNormal( transform, vertex, Vector3.zero );
                    if ( normal != Vector3.zero )
                    {
                        resultHit.normal = normal;
                    }
                }

                resultHit.point = vertex;
            }

            if ( transform != null )
            {
                Terrain terrain = transform.GetComponent<Terrain>();
                if ( terrain != null )
                {
                    Vector3[] corners = TerrainUtils.GetCorners( terrain, Space.World );
                    float     minDist = float.MaxValue;
                    vertex = corners[ 0 ];
                    foreach ( Vector3 corner in corners )
                    {
                        float dist = Vector3.Cross( mouseRay.direction, corner - mouseRay.origin ).magnitude;
                        if ( dist < minDist )
                        {
                            minDist = dist;
                            vertex  = corner;
                        }
                    }

                    result = true;
                    SetHit( ref hit );
                    return result;
                }
            }

            #if UNITY_2020_2_OR_NEWER
            if ( transform == null )
            {
                result = HandleUtility.FindNearestVertex( Event.current.mousePosition, out vertex );
            }
            else
            {
                result = HandleUtility.FindNearestVertex( Event.current.mousePosition,
                    new[] { transform }, out vertex );
            }
            #else
            Transform[] selection = { transform };
            if (findNearestVertex == null)
            {
                var editorTypes = typeof(UnityEditor.Editor).Assembly.GetTypes();
                var type_HandleUtility = editorTypes.FirstOrDefault(t => t.Name == "HandleUtility");
                findNearestVertex = type_HandleUtility.GetMethod("FindNearestVertex",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
            }
            var parameters = new object[] { Event.current.mousePosition, selection, null };
            result = (bool)findNearestVertex.Invoke(null, parameters);
            if (result) vertex = (Vector3)parameters[2];
            #endif
            if ( !result )
            {
                return false;
            }

            SetHit( ref hit );
            return result;
        }

        private static Vector3 GetNormal( Transform transform, Vector3 vertex, Vector3 defaultValue )
        {
            Vector3    result    = defaultValue;
            Collider[] colliders = transform.GetComponentsInChildren<Collider>();

            int GetVertexIdx( Mesh mesh, Vector3 v )
            {
                if ( mesh == null )
                {
                    return -1;
                }

                int idx = Array.IndexOf( mesh.vertices, v );
                if ( idx < 0 )
                {
                    return idx;
                }

                return idx;
            }

            foreach ( Collider collider in colliders )
            {
                if ( collider is MeshCollider )
                {
                    MeshCollider meshCollider = collider as MeshCollider;
                    int          idx          = GetVertexIdx( meshCollider.sharedMesh, vertex );
                    if ( idx < 0 )
                    {
                        continue;
                    }

                    return meshCollider.sharedMesh.normals[ idx ];
                }

                Vector3 center = BoundsUtils.GetBoundsRecursive( transform ).center;
                result = ( vertex - center ).normalized;
                return result;
            }

            MeshFilter[] meshFilters = transform.GetComponentsInChildren<MeshFilter>();
            foreach ( MeshFilter filter in meshFilters )
            {
                Mesh mesh = filter.sharedMesh;
                int  idx  = GetVertexIdx( mesh, vertex );
                if ( idx < 0 )
                {
                    continue;
                }

                return mesh.normals[ idx ];
            }

            SkinnedMeshRenderer[] renderers = transform.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach ( SkinnedMeshRenderer renderer in renderers )
            {
                Mesh mesh = renderer.sharedMesh;
                int  idx  = GetVertexIdx( mesh, vertex );
                if ( idx < 0 )
                {
                    continue;
                }

                return mesh.normals[ idx ];
            }

            return result;
        }

    }
}