using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PluginMaster
{
    public static class BoundsUtils
    {

        #region Statics and Constants

        public static readonly Vector3 MIN_VECTOR3 = new Vector3( float.MinValue, float.MinValue, float.MinValue );
        public static readonly Vector3 MAX_VECTOR3 = new Vector3( float.MaxValue, float.MaxValue, float.MaxValue );

        private static Dictionary<(int, ObjectProperty), Bounds> _boundsDictionary
            = new Dictionary<(int, ObjectProperty), Bounds>();

        private static Dictionary<(int, ObjectProperty, Vector2), Bounds>
            _boundsRecursiveDictionary = new Dictionary<(int, ObjectProperty, Vector2), Bounds>();

        private static Dictionary<BoundsRotKey, Bounds> _boundsRotDictionary
            = new Dictionary<BoundsRotKey, Bounds>();

        #endregion

        #region Public Enums

        public enum ObjectProperty
        {
            BOUNDING_BOX,
            CENTER,
            PIVOT,
        }

        #endregion

        #region Public Methods

        public static void ClearBoundsDictionaries()
        {
            _boundsDictionary.Clear();
            _boundsRecursiveDictionary.Clear();
            _boundsRotDictionary.Clear();
        }

        public static float GetAverageMagnitude( Transform transform )
        {
            Vector3 size = GetBoundsRecursive( transform ).size;
            return ( size.x + size.y + size.z ) / 3;
        }

        public static float GetBottomMagnitude( Transform transform )
        {
            Vector3[] vertices  = GetBottomVertices( transform );
            float     magnitude = float.MinValue;
            foreach ( Vector3 vertex in vertices )
            {
                magnitude = Mathf.Max( magnitude, vertex.y );
            }

            return magnitude * transform.localScale.y;
        }

        public static Vector3[] GetBottomVertices( Transform transform, Space space = Space.Self )
        {
            HashSet<Vector3> vertices         = new HashSet<Vector3>();
            HashSet<Vector3> allLocalVertices = new HashSet<Vector3>();
            float            minY             = float.MaxValue;
            MeshFilter[]     meshFilters      = transform.GetComponentsInChildren<MeshFilter>();

            void UpdateMinVertex( Vector3 vertex, Transform child )
            {
                Vector3 worldVertex = child.TransformPoint( vertex );
                Vector3 localVertex = space == Space.Self ? transform.InverseTransformPoint( worldVertex ) : worldVertex;
                allLocalVertices.Add( localVertex );
                minY = Mathf.Min( localVertex.y, minY );
            }

            foreach ( MeshFilter filter in meshFilters )
            {
                if ( filter.sharedMesh == null )
                {
                    continue;
                }

                foreach ( Vector3 vertex in filter.sharedMesh.vertices )
                {
                    UpdateMinVertex( vertex, filter.transform );
                }
            }

            SkinnedMeshRenderer[] skinnedMeshRenderers = transform.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach ( SkinnedMeshRenderer renderer in skinnedMeshRenderers )
            {
                if ( renderer.sharedMesh == null )
                {
                    continue;
                }

                foreach ( Vector3 vertex in renderer.sharedMesh.vertices )
                {
                    UpdateMinVertex( vertex, renderer.transform );
                }
            }

            float threshold = 0.01f;
            foreach ( Vector3 vertex in allLocalVertices )
            {
                if ( vertex.y < minY + threshold )
                {
                    Vector3 localVertex = space == Space.Self ? vertex : transform.InverseTransformPoint( vertex );
                    vertices.Add( localVertex );
                }
            }

            return vertices.ToArray();
        }

        public static Bounds GetBounds( Transform transform, ObjectProperty property = ObjectProperty.BOUNDING_BOX,
                                        bool      useDictionary = true )
        {
            (int, ObjectProperty property) key = ( transform.gameObject.GetInstanceID(), property );
            if ( useDictionary && _boundsDictionary.ContainsKey( key ) )
            {
                return _boundsDictionary[ key ];
            }

            Terrain       terrain       = transform.GetComponent<Terrain>();
            Renderer      renderer      = transform.GetComponent<Renderer>();
            RectTransform rectTransform = transform.GetComponent<RectTransform>();
            LODGroup      lodGroup      = renderer == null ? transform.GetComponent<LODGroup>() : null;

            Bounds DoGetBounds()
            {
                if ( lodGroup    != null
                     && property == ObjectProperty.BOUNDING_BOX )
                {
                    LOD[] lods = lodGroup.GetLODs();
                    if ( lods           != null
                         && lods.Length > 0 )
                    {
                        lods = lods.Where( l => l.renderers != null ).ToArray();
                        if ( lods.Length > 0 )
                        {
                            GameObject[] lodGameObjects = lods[ 0 ].renderers.Where( r => r                != null ).Select( r => r.gameObject )
                                                                   .Where( o => o.GetComponent<LODGroup>() == null ).ToArray();
                            return GetSelectionBounds( lodGameObjects, false );
                        }
                    }
                }

                if ( rectTransform == null
                     && terrain    == null )
                {
                    if ( renderer == null
                         || !renderer.enabled
                         || property == ObjectProperty.PIVOT )
                    {
                        return new Bounds( transform.position, Vector3.zero );
                    }

                    if ( property == ObjectProperty.CENTER )
                    {
                        return new Bounds( renderer.bounds.center, Vector3.zero );
                    }

                    return renderer.bounds;
                }

                if ( property == ObjectProperty.PIVOT )
                {
                    return new Bounds( transform.position, Vector3.zero );
                }

                if ( terrain != null )
                {
                    Bounds bounds = terrain.terrainData.bounds;
                    bounds.center += transform.position;
                    return bounds;
                }

                return new Bounds( rectTransform.TransformPoint( rectTransform.rect.center ),
                    rectTransform.TransformVector( rectTransform.rect.size ) );
            }

            Bounds result = DoGetBounds();
            if ( useDictionary )
            {
                _boundsDictionary.Add( key, result );
            }

            return result;
        }

        public static Bounds GetBounds( Transform transform, Quaternion rotation, bool useDictionary = true )
        {
            RectTransform rectTransform = transform.GetComponent<RectTransform>();
            if ( rectTransform != null )
            {
                return new Bounds( rectTransform.TransformPoint( rectTransform.rect.center ),
                    rectTransform.TransformVector( rectTransform.rect.size ) );
            }

            Renderer   renderer   = transform.GetComponent<Renderer>();
            MeshFilter meshFilter = transform.GetComponent<MeshFilter>();
            if ( renderer                 == null
                 || meshFilter            == null
                 || meshFilter.sharedMesh == null
                 || !renderer.enabled )
            {
                return new Bounds( transform.position, Vector3.zero );
            }

            Vector3 maxSqrDistance = MIN_VECTOR3;
            Vector3 minSqrDistance = MAX_VECTOR3;
            Vector3 center         = GetBounds( transform ).center;
            Vector3 right          = rotation * Vector3.right;
            Vector3 up             = rotation * Vector3.up;
            Vector3 forward        = rotation * Vector3.forward;
            foreach ( Vector3 vertex in meshFilter.sharedMesh.vertices )
            {
                Vector3 centerToVertex    = transform.TransformPoint( vertex ) - center;
                Vector3 rightProjection   = Vector3.Project( centerToVertex, right );
                Vector3 upProjection      = Vector3.Project( centerToVertex, up );
                Vector3 forwardProjection = Vector3.Project( centerToVertex, forward );
                float   rightSqrDistance  = rightProjection.sqrMagnitude * ( rightProjection.normalized != right ? -1 : 1 );
                float   upSqrDistance     = upProjection.sqrMagnitude    * ( upProjection.normalized    != up ? -1 : 1 );
                float forwardSqrDistance = forwardProjection.sqrMagnitude
                                           * ( forwardProjection.normalized != forward ? -1 : 1 );
                maxSqrDistance.x = Mathf.Max( maxSqrDistance.x, rightSqrDistance );
                maxSqrDistance.y = Mathf.Max( maxSqrDistance.y, upSqrDistance );
                maxSqrDistance.z = Mathf.Max( maxSqrDistance.z, forwardSqrDistance );
                minSqrDistance.x = Mathf.Min( minSqrDistance.x, rightSqrDistance );
                minSqrDistance.y = Mathf.Min( minSqrDistance.y, upSqrDistance );
                minSqrDistance.z = Mathf.Min( minSqrDistance.z, forwardSqrDistance );
            }

            Vector3 size = new Vector3(
                Mathf.Sqrt( Mathf.Abs( maxSqrDistance.x ) )   * Mathf.Sign( maxSqrDistance.x )
                - Mathf.Sqrt( Mathf.Abs( minSqrDistance.x ) ) * Mathf.Sign( minSqrDistance.x ),
                Mathf.Sqrt( Mathf.Abs( maxSqrDistance.y ) )   * Mathf.Sign( maxSqrDistance.y )
                - Mathf.Sqrt( Mathf.Abs( minSqrDistance.y ) ) * Mathf.Sign( minSqrDistance.y ),
                Mathf.Sqrt( Mathf.Abs( maxSqrDistance.z ) )   * Mathf.Sign( maxSqrDistance.z )
                - Mathf.Sqrt( Mathf.Abs( minSqrDistance.z ) ) * Mathf.Sign( minSqrDistance.z ) );
            return new Bounds( center, size );
        }

        public static Bounds GetBoundsRecursive( Transform      transform,                              bool recursive     = true,
                                                 ObjectProperty property = ObjectProperty.BOUNDING_BOX, bool useDictionary = true )
        {
            if ( !recursive )
            {
                return GetBounds( transform, property, useDictionary );
            }

            Vector2        pivot2D        = Vector2.zero;
            SpriteRenderer spriteRenderer = transform.GetComponent<SpriteRenderer>();
            if ( spriteRenderer != null
                 && spriteRenderer.enabled
                 && spriteRenderer.sprite != null )
            {
                pivot2D = spriteRenderer.sprite.pivot;
            }

            (int, ObjectProperty property, Vector2 pivot2D) key = ( transform.gameObject.GetInstanceID(), property, pivot2D );
            if ( useDictionary && _boundsRecursiveDictionary.ContainsKey( key ) )
            {
                return _boundsRecursiveDictionary[ key ];
            }

            Transform[] children       = transform.GetComponentsInChildren<Transform>( true );
            Vector3     min            = MAX_VECTOR3;
            Vector3     max            = MIN_VECTOR3;
            bool        emptyHierarchy = true;

            bool IsActiveInHierarchy( Transform obj )
            {
                Transform parent = obj;
                do
                {
                    if ( !parent.gameObject.activeSelf )
                    {
                        return false;
                    }

                    parent = parent.parent;
                }
                while ( parent != null );

                return true;
            }

            foreach ( Transform child in children )
            {
                bool notActive = !IsActiveInHierarchy( child );
                if ( notActive )
                {
                    continue;
                }

                Renderer      renderer      = child.GetComponent<Renderer>();
                RectTransform rectTransform = child.GetComponent<RectTransform>();
                Terrain       terrain       = child.GetComponent<Terrain>();
                LODGroup      lodGroup      = child.GetComponent<LODGroup>();
                if ( ( renderer == null || !renderer.enabled )
                     && rectTransform == null
                     && terrain       == null
                     && lodGroup      == null )
                {
                    continue;
                }

                Bounds bounds = GetBounds( child, property, useDictionary );
                if ( bounds.size == Vector3.zero )
                {
                    continue;
                }

                emptyHierarchy = false;
                min            = Vector3.Min( bounds.min, min );
                max            = Vector3.Max( bounds.max, max );
            }

            if ( emptyHierarchy )
            {
                return new Bounds( transform.position, Vector3.zero );
            }

            Vector3 size   = max - min;
            Vector3 center = min + size / 2f;
            Bounds  result = new Bounds( center, size );
            if ( useDictionary )
            {
                _boundsRecursiveDictionary.Add( key, result );
            }

            return result;
        }

        public static Bounds GetBoundsRecursive( Transform      transform,                              Quaternion rotation,         bool ignoreDissabled = true,
                                                 ObjectProperty property = ObjectProperty.BOUNDING_BOX, bool       recursive = true, bool useDictionary   = true )
        {
            if ( property == ObjectProperty.PIVOT )
            {
                return new Bounds( transform.position, Vector3.zero );
            }

            Vector2        pivot2D        = Vector2.zero;
            SpriteRenderer spriteRenderer = transform.GetComponent<SpriteRenderer>();
            if ( spriteRenderer != null
                 && spriteRenderer.enabled
                 && spriteRenderer.sprite != null )
            {
                pivot2D = spriteRenderer.sprite.pivot;
            }

            BoundsRotKey key = new BoundsRotKey( transform.gameObject.GetInstanceID(), transform.position,
                transform.rotation, transform.lossyScale, rotation, pivot2D );
            useDictionary = false;
            if ( useDictionary && _boundsRotDictionary.ContainsKey( key ) )
            {
                return _boundsRotDictionary[ key ];
            }

            Vector3 center = GetBoundsRecursive( transform, recursive, property, useDictionary ).center;
            if ( property == ObjectProperty.CENTER )
            {
                return new Bounds( center, Vector3.zero );
            }

            Vector3 maxDistance, minDistance;
            GetDistanceFromCenterRecursive( transform, rotation,        center,
                out minDistance,                       out maxDistance, ignoreDissabled, recursive );
            Vector3 size = maxDistance         - minDistance;
            center += rotation * ( minDistance + size / 2 );
            Bounds bounds = new Bounds( center, size );
            if ( useDictionary )
            {
                _boundsRotDictionary.Add( key, bounds );
            }

            return new Bounds( center, size );
        }

        public static Bounds GetBoundsRecursive( Transform transform, Quaternion rotation, Vector3 scale,
                                                 bool      ignoreDissabled = true )
        {
            GameObject obj = Object.Instantiate( transform.gameObject );
            obj.transform.localScale = Vector3.Scale( obj.transform.localScale, scale );
            Bounds bounds = GetBoundsRecursive( obj.transform, rotation, ignoreDissabled );
            Object.DestroyImmediate( obj );
            return bounds;
        }

        public static float GetMagnitude( Transform transform )
        {
            Vector3 size = GetBoundsRecursive( transform ).size;
            return Mathf.Max( size.x, size.y, size.z );
        }

        public static Vector3 GetMaxSize( GameObject[] objs )
        {
            Vector3 max = MIN_VECTOR3;
            foreach ( GameObject obj in objs )
            {
                Vector3 size = Vector3.zero;
                if ( obj != null )
                {
                    size = GetBoundsRecursive( obj.transform ).size;
                }

                max = Vector3.Max( max, size );
            }

            return max;
        }

        public static Vector3 GetMaxVector( Vector3[] values )
        {
            Vector3 max = MIN_VECTOR3;
            foreach ( Vector3 value in values )
            {
                max = Vector3.Max( max, value );
            }

            return max;
        }

        public static Bounds GetSelectionBounds( GameObject[]   selection, bool recursive = true,
                                                 ObjectProperty property = ObjectProperty.BOUNDING_BOX )
        {
            Vector3 max = MIN_VECTOR3;
            Vector3 min = MAX_VECTOR3;
            if ( selection.Length == 0 )
            {
                return new Bounds();
            }

            foreach ( GameObject obj in selection )
            {
                if ( obj == null )
                {
                    continue;
                }

                Bounds bounds = GetBoundsRecursive( obj.transform, recursive, property );
                max = Vector3.Max( bounds.max, max );
                min = Vector3.Min( bounds.min, min );
            }

            Vector3 size   = max - min;
            Vector3 center = min + size / 2f;
            return new Bounds( center, size );
        }

        public static Bounds GetSelectionBounds( GameObject[] selection, Quaternion rotation, bool ignoreDissabled = true )
        {
            Vector3 max    = MIN_VECTOR3;
            Vector3 min    = MAX_VECTOR3;
            Vector3 center = GetSelectionBounds( selection ).center;
            bool    empty  = true;
            foreach ( GameObject obj in selection )
            {
                if ( obj == null )
                {
                    continue;
                }

                float objMagnitude = GetMagnitude( obj.transform );
                if ( objMagnitude == 0 )
                {
                    continue;
                }

                Vector3 minDistance, maxDistance;
                GetDistanceFromCenterRecursive( obj.transform, rotation,        center,
                    out minDistance,                           out maxDistance, ignoreDissabled );
                max   = Vector3.Max( maxDistance, max );
                min   = Vector3.Min( minDistance, min );
                empty = false;
            }

            if ( empty )
            {
                return new Bounds( center, Vector3.zero );
            }

            Vector3 size = max         - min;
            center += rotation * ( min + size / 2 );
            return new Bounds( center, size );
        }

        public static Vector3[] GetVertices( Transform transform )
        {
            List<Vector3> vertices    = new List<Vector3>();
            MeshFilter[]  meshFilters = transform.GetComponentsInChildren<MeshFilter>();
            foreach ( MeshFilter filter in meshFilters )
            {
                if ( filter.sharedMesh == null )
                {
                    continue;
                }

                vertices.AddRange( filter.sharedMesh.vertices );
            }

            SkinnedMeshRenderer[] skinnedMeshRenderers = transform.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach ( SkinnedMeshRenderer renderer in skinnedMeshRenderers )
            {
                if ( renderer.sharedMesh == null )
                {
                    continue;
                }

                vertices.AddRange( renderer.sharedMesh.vertices );
            }

            return vertices.ToArray();
        }

        #endregion

        #region Private Methods

        private static void GetDistanceFromCenter( Transform transform, Quaternion  rotation,
                                                   Vector3   center,    out Vector3 min, out Vector3 max, bool ignoreDisabled = true )
        {
            min = max = Vector3.zero;
            if ( ignoreDisabled && !transform.gameObject.activeSelf )
            {
                return;
            }

            List<Vector3> vertices      = new List<Vector3>();
            RectTransform rectTransform = transform.GetComponent<RectTransform>();
            Terrain       terrain       = transform.GetComponent<Terrain>();
            if ( rectTransform != null )
            {
                vertices.Add( rectTransform.rect.min );
                vertices.Add( rectTransform.rect.max );
                vertices.Add( new Vector2( rectTransform.rect.min.x, rectTransform.rect.max.y ) );
                vertices.Add( new Vector2( rectTransform.rect.max.x, rectTransform.rect.min.y ) );
            }
            else if ( terrain != null )
            {
                vertices.AddRange( TerrainUtils.GetCorners( terrain, Space.Self ) );
            }
            else
            {
                Renderer renderer = transform.GetComponent<Renderer>();
                if ( renderer == null
                     || !renderer.enabled )
                {
                    return;
                }

                if ( renderer is SpriteRenderer )
                {
                    Sprite sprite = ( renderer as SpriteRenderer ).sprite;
                    if ( sprite == null )
                    {
                        return;
                    }

                    Quaternion rot = renderer.transform.rotation;
                    renderer.transform.rotation = Quaternion.Euler( 0, 0, 0 );
                    Vector3 spriteMin = renderer.transform.InverseTransformPoint( renderer.bounds.min );
                    Vector3 spriteMax = renderer.transform.InverseTransformPoint( renderer.bounds.max );
                    renderer.transform.rotation = rot;

                    vertices.Add( new Vector3( spriteMin.x, spriteMin.y, 0 ) );
                    vertices.Add( new Vector3( spriteMin.x, spriteMax.y, 0 ) );
                    vertices.Add( new Vector3( spriteMax.x, spriteMin.y, 0 ) );
                    vertices.Add( new Vector3( spriteMax.x, spriteMax.y, 0 ) );
                }
                else if ( renderer is MeshRenderer )
                {
                    MeshFilter meshFilter = transform.GetComponent<MeshFilter>();
                    if ( meshFilter               == null
                         || meshFilter.sharedMesh == null )
                    {
                        return;
                    }

                    vertices.AddRange( meshFilter.sharedMesh.vertices );
                }
                else if ( renderer is SkinnedMeshRenderer )
                {
                    Mesh mesh = ( renderer as SkinnedMeshRenderer ).sharedMesh;
                    if ( mesh == null )
                    {
                        return;
                    }

                    vertices.AddRange( mesh.vertices );
                }
            }

            if ( vertices.Count == 0 )
            {
                min = max = Vector3.zero;
                return;
            }

            Vector3 maxSqrDistance = MIN_VECTOR3;
            Vector3 minSqrDistance = MAX_VECTOR3;
            Vector3 right          = rotation * Vector3.right;
            Vector3 up             = rotation * Vector3.up;
            Vector3 forward        = rotation * Vector3.forward;

            foreach ( Vector3 vertex in vertices )
            {
                Vector3 centerToVertex    = transform.TransformPoint( vertex ) - center;
                Vector3 rightProjection   = Vector3.Project( centerToVertex, right );
                Vector3 upProjection      = Vector3.Project( centerToVertex, up );
                Vector3 forwardProjection = Vector3.Project( centerToVertex, forward );
                float   rightSqrDistance  = rightProjection.sqrMagnitude * ( rightProjection.normalized != right ? -1 : 1 );
                float   upSqrDistance     = upProjection.sqrMagnitude    * ( upProjection.normalized    != up ? -1 : 1 );
                float forwardSqrDistance = forwardProjection.sqrMagnitude
                                           * ( forwardProjection.normalized != forward ? -1 : 1 );

                maxSqrDistance.x = Mathf.Max( maxSqrDistance.x, rightSqrDistance );
                maxSqrDistance.y = Mathf.Max( maxSqrDistance.y, upSqrDistance );
                maxSqrDistance.z = Mathf.Max( maxSqrDistance.z, forwardSqrDistance );

                minSqrDistance.x = Mathf.Min( minSqrDistance.x, rightSqrDistance );
                minSqrDistance.y = Mathf.Min( minSqrDistance.y, upSqrDistance );
                minSqrDistance.z = Mathf.Min( minSqrDistance.z, forwardSqrDistance );
            }

            min = new Vector3(
                Mathf.Sqrt( Mathf.Abs( minSqrDistance.x ) ) * Mathf.Sign( minSqrDistance.x ),
                Mathf.Sqrt( Mathf.Abs( minSqrDistance.y ) ) * Mathf.Sign( minSqrDistance.y ),
                Mathf.Sqrt( Mathf.Abs( minSqrDistance.z ) ) * Mathf.Sign( minSqrDistance.z ) );
            max = new Vector3(
                Mathf.Sqrt( Mathf.Abs( maxSqrDistance.x ) ) * Mathf.Sign( maxSqrDistance.x ),
                Mathf.Sqrt( Mathf.Abs( maxSqrDistance.y ) ) * Mathf.Sign( maxSqrDistance.y ),
                Mathf.Sqrt( Mathf.Abs( maxSqrDistance.z ) ) * Mathf.Sign( maxSqrDistance.z ) );
        }

        private static void GetDistanceFromCenterRecursive( Transform transform, Quaternion  rotation,
                                                            Vector3   center,    out Vector3 minDistance, out Vector3 maxDistance, bool ignoreDissabled = true, bool recursive = true )
        {
            Transform[] children       = recursive ? transform.GetComponentsInChildren<Transform>( true ) : new[] { transform };
            bool        emptyHierarchy = true;
            maxDistance = MIN_VECTOR3;
            minDistance = MAX_VECTOR3;
            foreach ( Transform child in children )
            {
                Renderer      renderer      = child.GetComponent<Renderer>();
                RectTransform rectTransform = child.GetComponent<RectTransform>();
                Terrain       terrain       = child.GetComponent<Terrain>();
                if ( ( renderer == null || !renderer.enabled )
                     && rectTransform == null
                     && terrain       == null )
                {
                    continue;
                }

                emptyHierarchy = false;

                Vector3 min, max;
                GetDistanceFromCenter( child, rotation, center, out min, out max, ignoreDissabled );
                minDistance = Vector3.Min( min, minDistance );
                maxDistance = Vector3.Max( max, maxDistance );
            }

            if ( emptyHierarchy )
            {
                minDistance = maxDistance = Vector3.zero;
            }
        }

        #endregion

        private struct BoundsRotKey
        {

            #region Public Constructors

            public BoundsRotKey( int     id,    Vector3    position,       Quaternion rotation,
                                 Vector3 scale, Quaternion boundsRotation, Vector2    pivot2D )
            {
                this.id             = id;
                this.position       = position;
                this.rotation       = rotation;
                this.scale          = scale;
                this.boundsRotation = boundsRotation;
                this.pivot2D        = pivot2D;
            }

            #endregion

            #region Private Fields

            private Quaternion boundsRotation;
            private int        id;
            private Vector2    pivot2D;
            private Vector3    position;
            private Quaternion rotation;
            private Vector3    scale;

            #endregion

        }
    }
}