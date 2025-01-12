using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PluginMaster
{

    #region DATA & SETTINGS

    [Serializable]
    public class GravityToolSettings : BrushToolBase
    {

        #region Serialized

        [SerializeField] private SimulateGravityData _simData             = new SimulateGravityData();
        [SerializeField] private float               _height              = 10f;
        [SerializeField] private bool                _createTempColliders = true;

        #endregion

        #region Public Properties

        public bool createTempColliders
        {
            get
            {
                if ( PWBCore.staticData.tempCollidersAction == PWBData.TempCollidersAction.NEVER_CREATE )
                {
                    return false;
                }

                return _createTempColliders;
            }
            set
            {
                if ( _createTempColliders == value )
                {
                    return;
                }

                _createTempColliders = value;
                DataChanged();
            }
        }

        public float height
        {
            get => _height;
            set
            {
                value = Mathf.Max( value, 0f );
                if ( _height == value )
                {
                    return;
                }

                _height = value;
            }
        }

        public SimulateGravityData simData => _simData;

        #endregion

        #region Public Constructors

        public GravityToolSettings()
        {
            _brushShape = BrushShape.POINT;
        }

        #endregion

        #region Public Methods

        public GravityToolSettings Clone()
        {
            GravityToolSettings clone = new GravityToolSettings();
            clone.Copy( this );
            return clone;
        }

        public override void Copy( IToolSettings other )
        {
            GravityToolSettings otherGravityToolSettings = other as GravityToolSettings;
            if ( otherGravityToolSettings == null )
            {
                return;
            }

            base.Copy( other );
            _simData.Copy( otherGravityToolSettings._simData );
            _height              = otherGravityToolSettings.height;
            _createTempColliders = otherGravityToolSettings.createTempColliders;
        }

        #endregion

    }

    [Serializable]
    public class GravityToolManager : ToolManagerBase<GravityToolSettings>
    {

        #region Statics and Constants

        private static float _surfaceDistanceSensitivityStatic = 1.0f;

        #endregion

        #region Serialized

        [SerializeField] private float _surfaceDistanceSensitivity = _surfaceDistanceSensitivityStatic;

        #endregion

        #region Public Properties

        public static float surfaceDistanceSensitivity
        {
            get => _surfaceDistanceSensitivityStatic;
            set
            {
                value = Mathf.Clamp( value, 0f, 1f );
                if ( _surfaceDistanceSensitivityStatic == value )
                {
                    return;
                }

                _surfaceDistanceSensitivityStatic = value;
                PWBCore.staticData.Save();
            }
        }

        #endregion

        #region Public Methods

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            _surfaceDistanceSensitivityStatic = _surfaceDistanceSensitivity;
        }

        public override void OnBeforeSerialize()
        {
            base.OnBeforeSerialize();
            _surfaceDistanceSensitivity = _surfaceDistanceSensitivityStatic;
        }

        #endregion

    }

    #endregion

    #region PWBIO

    public static partial class PWBIO
    {

        #region Statics and Constants

        private static          Mesh     _gravityLinesMesh;
        private static          Material _gravityLinesMaterial;
        private static readonly int      OPACITY_PROP_ID = Shader.PropertyToID( "_opacity" );

        #endregion

        #region Private Methods

        private static void DrawGravityBrush( RaycastHit hit, Camera camera )
        {
            GravityToolSettings settings = GravityToolManager.settings;

            PWBCore.UpdateTempCollidersIfHierarchyChanged();

            hit.point = SnapAndUpdateGridOrigin( hit.point, SnapManager.settings.snappingEnabled,
                true,                                       true, false, Vector3.down );
            Vector3 tangent   = GetTangent( Vector3.up );
            Vector3 bitangent = Vector3.Cross( hit.normal, tangent );

            if ( settings.brushShape == BrushToolBase.BrushShape.SQUARE )
            {
                DrawSquareIndicator( hit.point, hit.normal, settings.radius,
                    settings.height,            tangent,    bitangent, Vector3.up, true, true );
            }
            else
            {
                DrawCricleIndicator( hit.point, hit.normal,
                    settings.brushShape == BrushToolBase.BrushShape.POINT ? 0.1f : settings.radius,
                    settings.height, tangent, bitangent, Vector3.up, true, true );
            }

            if ( _gravityLinesMesh == null )
            {
                _gravityLinesMesh = new Mesh();
                _gravityLinesMesh.SetVertices( new[]
                {
                    new Vector3( -1, -1, 0 ), new Vector3( 1,  -1, 0 ),
                    new Vector3( 1,  1,  0 ), new Vector3( -1, 1,  0 ),
                } );
                _gravityLinesMesh.uv = new[]
                {
                    new Vector2( 1, 0 ), new Vector2( 0, 0 ),
                    new Vector2( 0, 1 ), new Vector2( 1, 1 ),
                };
                _gravityLinesMesh.SetTriangles( new[] { 0, 1, 2, 0, 2, 3 }, 0 );
                _gravityLinesMesh.RecalculateNormals();
            }

            if ( _gravityLinesMaterial == null )
            {
                _gravityLinesMaterial = new Material( Resources.Load<Material>( "Materials/GravityLines" ) );
            }

            float camEulerY           = Mathf.Abs( Vector3.SignedAngle( Vector3.up, camera.transform.up, camera.transform.forward ) );
            float gravityLinesOpacity = 1f - Mathf.Min( ( camEulerY > 90f ? 180f - camEulerY : camEulerY ) / 60f, 1f );
            _gravityLinesMaterial.SetFloat( OPACITY_PROP_ID, gravityLinesOpacity );

            Vector3 hitToCamXZ = camera.transform.position - hit.point;
            hitToCamXZ.y = 0f;
            Quaternion gravityLinesRotation = Quaternion.AngleAxis( camera.transform.eulerAngles.y, Vector3.up );
            float radius = settings.brushShape == BrushToolBase.BrushShape.POINT
                ? 0.5F
                : settings.radius;
            Matrix4x4 gravityLinesMatrix = Matrix4x4.TRS( hit.point + new Vector3( 0f, 3f * radius, 0f ),
                gravityLinesRotation, new Vector3( 0.5f,                               2f,          1f ) * radius );
            Graphics.DrawMesh( _gravityLinesMesh, gravityLinesMatrix, _gravityLinesMaterial, 0, camera );

            Transform surface = null;
            if ( hit.collider != null )
            {
                surface = hit.collider.transform;
            }

            GravityStrokePreview( hit.point + new Vector3( 0f, settings.height, 0f ), tangent,
                bitangent,                                                            camera, surface );
        }

        private static Vector3 GetObjectHalfSize( Transform transform )
        {
            Vector3    size             = new Vector3( 0.1f, 0.1f, 0.1f );
            Renderer[] childrenRenderer = transform.GetComponentsInChildren<Renderer>();
            foreach ( Renderer child in childrenRenderer )
            {
                size = Vector3.Max( size, child.bounds.size );
            }

            return size / 2f;
        }

        private static void GravityStrokePreview( Vector3 center,    Vector3 tangent,
                                                  Vector3 bitangent, Camera  camera, Transform surface )
        {
            _paintStroke.Clear();

            foreach ( BrushstrokeItem strokeItem in BrushstrokeManager.brushstroke )
            {
                GameObject prefab = strokeItem.settings.prefab;
                if ( prefab == null )
                {
                    continue;
                }

                float         h             = strokeItem.settings.bottomMagnitude;
                BrushSettings brushSettings = strokeItem.settings;
                if ( GravityToolManager.settings.overwriteBrushProperties )
                {
                    brushSettings = GravityToolManager.settings.brushSettings;
                }

                Vector3 strokePosition = TangentSpaceToWorld( tangent, bitangent,
                    new Vector2( strokeItem.tangentPosition.x, strokeItem.tangentPosition.y ) );
                Vector3 itemPosition = strokePosition + center + new Vector3( 0f, h * strokeItem.scaleMultiplier.y, 0f );

                Quaternion itemRotation = Quaternion.AngleAxis( _brushAngle, Vector3.up )
                                          * Quaternion.Euler( strokeItem.additionalAngle );
                if ( GravityToolManager.settings.orientAlongBrushstroke )
                {
                    itemRotation = Quaternion.LookRotation( _strokeDirection, Vector3.up );
                    itemPosition = center + itemRotation * strokePosition;
                }

                itemPosition += itemRotation * brushSettings.localPositionOffset;

                Matrix4x4 rootToWorld = Matrix4x4.TRS( itemPosition, itemRotation, strokeItem.scaleMultiplier )
                                        * Matrix4x4.Translate( -prefab.transform.position );
                Vector3 itemScale = Vector3.Scale( prefab.transform.localScale, strokeItem.scaleMultiplier );
                int layer = GravityToolManager.settings.overwritePrefabLayer
                    ? GravityToolManager.settings.layer
                    : prefab.layer;
                Transform parentTransform = GetParent( GravityToolManager.settings, prefab.name, false, surface );
                _paintStroke.Add( new PaintStrokeItem( prefab, itemPosition,
                    itemRotation * Quaternion.Euler( prefab.transform.eulerAngles ),
                    itemScale, layer, parentTransform, surface, strokeItem.flipX, strokeItem.flipY ) );
                PreviewBrushItem( prefab, rootToWorld, layer, camera, false, false, strokeItem.flipX, strokeItem.flipY );
            }
        }

        private static void GravityToolDuringSceneGUI( SceneView sceneView )
        {
            if ( GravityToolManager.settings.createTempColliders )
            {
                PWBCore.CreateTempCollidersWithinFrustum( sceneView.camera );
            }

            BrushstrokeMouseEvents( GravityToolManager.settings );

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

            if ( MouseRaycast( mouseRay, out RaycastHit hit, out GameObject c, float.MaxValue, -1, paintOnPalettePrefabs: true, castOnMeshesWithoutCollider: true ) )
            {
                DrawGravityBrush( hit, sceneView.camera );
            }
            else
            {
                return;
            }

            void AddHeight( float value )
            {
                GravityToolManager.settings.height += value;
                ToolProperties.RepainWindow();
            }

            if ( Event.current.button == 0
                 && !Event.current.alt
                 && ( Event.current.type == EventType.MouseDown || Event.current.type == EventType.MouseDrag ) )
            {
                Dictionary<string, List<GameObject>> paintedObjectsDic = Paint( GravityToolManager.settings, PAINT_CMD, false );
                if ( !paintedObjectsDic.ContainsKey( string.Empty ) )
                {
                    return;
                }

                GameObject[] paintedObjects = paintedObjectsDic[ string.Empty ].ToArray();

                Pose[] finalPoses = GravityUtils.SimulateGravity( paintedObjects, GravityToolManager.settings.simData, false );

                for ( int i = 0; i < paintedObjects.Length; ++i )
                {
                    GameObject obj        = paintedObjects[ i ];
                    Transform  parent     = obj.transform.parent;
                    Vector3    position   = obj.transform.position;
                    Quaternion rotation   = obj.transform.rotation;
                    Vector3    localScale = obj.transform.localScale;

                    MeshCollider[] colliders = obj.GetComponentsInChildren<MeshCollider>();
                    foreach ( MeshCollider collider in colliders )
                    {
                        if ( collider == null )
                        {
                            continue;
                        }

                        if ( PrefabUtility.IsAddedComponentOverride( collider ) )
                        {
                            PrefabUtility.RevertAddedComponent( collider,
                                InteractionMode.AutomatedAction );
                        }
                    }

                    obj.transform.SetParent( parent );
                    obj.transform.position   = position;
                    obj.transform.rotation   = rotation;
                    obj.transform.localScale = localScale;
                    PWBCore.AddTempCollider( obj, finalPoses[ i ] );
                }
            }

            if ( PWBSettings.shortcuts.gravitySubtract1UnitFromSurfDist.Check() )
            {
                AddHeight( -1f );
            }
            else if ( PWBSettings.shortcuts.gravityAdd1UnitToSurfDist.Check() )
            {
                AddHeight( 1f );
            }
            else if ( PWBSettings.shortcuts.gravitySubtract01UnitFromSurfDist.Check() )
            {
                AddHeight( -0.1f );
            }
            else if ( PWBSettings.shortcuts.gravityAdd01UnitToSurfDist.Check() )
            {
                AddHeight( 0.1f );
            }
            else if ( PWBSettings.shortcuts.gravitySurfDist.Check() )
            {
                float delta = Mathf.Sign( PWBSettings.shortcuts.gravitySurfDist.combination.delta )
                              * GravityToolManager.surfaceDistanceSensitivity;
                GravityToolManager.settings.height = Mathf.Max( ( GravityToolManager.settings.height + delta * 0.5f )
                                                                * ( 1f                               + delta * 0.02f ), 0.05f );
                ToolProperties.RepainWindow();
            }

            if ( Event.current.button == 1 )
            {
                if ( Event.current.type == EventType.MouseDown
                     && Event.current.control )
                {
                    _pinned   = true;
                    _pinMouse = Event.current.mousePosition;
                    Event.current.Use();
                }
            }
        }

        #endregion

    }

    #endregion

}