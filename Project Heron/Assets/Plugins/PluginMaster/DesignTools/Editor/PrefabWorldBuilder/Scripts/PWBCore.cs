using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.ShortcutManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace PluginMaster
{

    #region CORE

    public static class PWBCore
    {

        #region Public Properties

        public static bool refreshDatabase { get; set; }

        #endregion

        #region Public Methods

        public static void AssetDatabaseRefresh()
        {
            if ( !ApplicationEventHandler.importingPackage )
            {
                if ( !DataReimportHandler.importingAssets
                     && !ApplicationEventHandler.sceneOpening )
                {
                    AssetDatabase.Refresh();
                }
            }
            else
            {
                ApplicationEventHandler.RefreshOnImportingCancelled();
            }

            refreshDatabase = false;
        }

        #endregion

        #region DATA

        private static PWBData _staticData;
        public static  bool    staticDataWasInitialized => _staticData != null;

        public static PWBData staticData
        {
            get
            {
                if ( _staticData != null )
                {
                    return _staticData;
                }

                _staticData = new PWBData();
                return _staticData;
            }
        }

        public static void LoadFromFile()
        {
            string text = PWBData.ReadDataText();

            void CreateFile()
            {
                _staticData = new PWBData();
                _staticData.SaveAndUpdateVersion();
            }

            if ( text == null )
            {
                CreateFile();
            }
            else
            {
                _staticData = null;
                try
                {
                    _staticData = JsonUtility.FromJson<PWBData>( text );
                }
                catch ( Exception e )
                {
                    Debug.LogException( e );
                }

                if ( _staticData == null )
                {
                    CreateFile();
                    return;
                }

                foreach ( PaletteData palette in PaletteManager.paletteData )
                foreach ( MultibrushSettings brush in palette.brushes )
                foreach ( MultibrushItemSettings item in brush.items )
                {
                    item.InitializeParentSettings( brush );
                }
            }
        }

        public static void SetSavePending()
        {
            AutoSave.QuickSave();
            staticData.SetSavePending();
        }

        public static string GetRelativePath( string fullPath )
        {
            Uri fullUri = new Uri( fullPath );
            Uri dataUri = new Uri( Application.dataPath );
            return Uri.UnescapeDataString( dataUri.MakeRelativeUri( fullUri ).ToString() );
        }

        public static string GetFullPath( string retalivePath )
            => Application.dataPath.Substring( 0, Application.dataPath.Length - 6 ) + retalivePath;

        public static bool IsFullPath( string path )
            => !string.IsNullOrWhiteSpace( path )
               && path.IndexOfAny( Path.GetInvalidPathChars().ToArray() ) == -1
               && Path.IsPathRooted( path )
               && !Path.GetPathRoot( path ).Equals( Path.DirectorySeparatorChar.ToString(),
                   StringComparison.Ordinal );

        #endregion

        #region TEMP COLLIDERS

        public const   string     PARENT_COLLIDER_NAME = "PluginMasterPrefabPaintTempMeshColliders";
        private static GameObject _parentCollider;

        private static GameObject parentCollider
        {
            get
            {
                if ( _parentCollider == null )
                {
                    _parentCollider           = new GameObject( PARENT_COLLIDER_NAME );
                    parentColliderId          = _parentCollider.GetInstanceID();
                    _parentCollider.hideFlags = HideFlags.HideAndDontSave;
                }

                return _parentCollider;
            }
        }

        public static int parentColliderId { get; private set; } = -1;

        private static Dictionary<int, GameObject> _tempCollidersIds
            = new Dictionary<int, GameObject>();

        private static Dictionary<int, GameObject> _tempCollidersTargets
            = new Dictionary<int, GameObject>();

        private static Dictionary<int, List<int>>
            _tempCollidersTargetParentsIds
                = new Dictionary<int, List<int>>();

        private static Dictionary<int, List<int>>
            _tempCollidersTargetChildrenIds
                = new Dictionary<int, List<int>>();

        private static BoundsOctree<MeshFilter> _meshFilterOctree      = new BoundsOctree<MeshFilter>( 10, Vector3.zero, 0.5f, 0.5f );
        private static PointOctree<MeshFilter>  _meshFilterPointOctree = new PointOctree<MeshFilter>( 10, Vector3.zero, 0.5f );

        public static bool CollidersContains( GameObject[] selection, string colliderName )
        {
            int objId;
            if ( !int.TryParse( colliderName, out objId ) )
            {
                return false;
            }

            foreach ( GameObject obj in selection )
            {
                if ( obj.GetInstanceID() == objId )
                {
                    return true;
                }
            }

            return false;
        }

        public static bool IsTempCollider( int instanceId ) => _tempCollidersIds.ContainsKey( instanceId );

        public static GameObject GetGameObjectFromTempColliderId( int instanceId )
        {
            if ( !_tempCollidersIds.ContainsKey( instanceId ) )
            {
                return null;
            }

            if ( _tempCollidersIds[ instanceId ] == null )
            {
                _tempCollidersIds.Remove( instanceId );
                Object tempCol = EditorUtility.InstanceIDToObject( instanceId );
                if ( tempCol != null )
                {
                    Object.DestroyImmediate( tempCol );
                }

                return null;
            }

            return _tempCollidersIds[ instanceId ];
        }

        public static GameObject GetGameObjectFromTempCollider( GameObject source )
        {
            if ( source == null )
            {
                return null;
            }

            if ( IsTempCollider( source.GetInstanceID() ) )
            {
                return GetGameObjectFromTempColliderId( source.GetInstanceID() );
            }

            return source;
        }

        public static bool updatingTempColliders { get; set; }

        public static void UpdateTempColliders()
        {
            updatingTempColliders = true;
            DestroyTempColliders();
            if ( staticData.tempCollidersAction == PWBData.TempCollidersAction.NEVER_CREATE )
            {
                return;
            }

            if ( ToolManager.tool == ToolManager.PaintTool.PIN
                 && !PinManager.settings.paintOnMeshesWithoutCollider )
            {
                return;
            }

            if ( ToolManager.tool == ToolManager.PaintTool.BRUSH
                 && !BrushManager.settings.paintOnMeshesWithoutCollider )
            {
                return;
            }

            if ( ToolManager.tool == ToolManager.PaintTool.GRAVITY
                 && !GravityToolManager.settings.createTempColliders )
            {
                return;
            }

            if ( ToolManager.tool == ToolManager.PaintTool.LINE
                 && !LineManager.settings.paintOnMeshesWithoutCollider )
            {
                return;
            }

            if ( ToolManager.tool == ToolManager.PaintTool.SHAPE
                 && !ShapeManager.settings.paintOnMeshesWithoutCollider )
            {
                return;
            }

            if ( ToolManager.tool == ToolManager.PaintTool.TILING
                 && !TilingManager.settings.paintOnMeshesWithoutCollider )
            {
                return;
            }

            if ( ToolManager.tool == ToolManager.PaintTool.SELECTION
                 && !SelectionToolManager.settings.createTempColliders )
            {
                return;
            }

            if ( ToolManager.tool == ToolManager.PaintTool.EXTRUDE
                 && !ExtrudeManager.settings.createTempColliders )
            {
                return;
            }

            if ( ToolManager.tool == ToolManager.PaintTool.MIRROR
                 && !MirrorManager.settings.createTempColliders )
            {
                return;
            }

            PWBIO.UpdateOctree();
            _meshFilterOctree = new BoundsOctree<MeshFilter>( 10, Vector3.zero, 0.5f, 0.5f );
            int sceneCount = SceneManager.sceneCount;
            for ( int i = 0; i < sceneCount; ++i )
            {
                Scene scene = SceneManager.GetSceneAt( i );
                if ( scene == null
                     || !scene.IsValid()
                     || !scene.isLoaded )
                {
                    continue;
                }

                GameObject[] rootObjs = scene.GetRootGameObjects();
                foreach ( GameObject rootObj in rootObjs )
                {
                    if ( !rootObj.activeInHierarchy )
                    {
                        continue;
                    }

                    AddTempCollider( rootObj );
                }
            }
        }

        public static void UpdateTempCollidersIfHierarchyChanged()
        {
            if ( !ApplicationEventHandler.hierarchyChangedWhileUsingTools )
            {
                return;
            }

            UpdateTempColliders();
            ApplicationEventHandler.hierarchyChangedWhileUsingTools = false;
        }

        public static void AddTempCollider( GameObject obj, Pose pose )
        {
            Pose currentPose = new Pose( obj.transform.position, obj.transform.rotation );
            obj.transform.SetPositionAndRotation( pose.position, pose.rotation );
            AddTempCollider( obj );
            obj.transform.SetPositionAndRotation( currentPose.position, currentPose.rotation );
        }

        private static void AddParentsIds( GameObject target )
        {
            Transform[] parents = target.GetComponentsInParent<Transform>();
            foreach ( Transform parent in parents )
            {
                if ( !_tempCollidersTargetParentsIds.ContainsKey( target.GetInstanceID() ) )
                {
                    _tempCollidersTargetParentsIds.Add( target.GetInstanceID(), new List<int>() );
                }

                _tempCollidersTargetParentsIds[ target.GetInstanceID() ].Add( parent.gameObject.GetInstanceID() );
                if ( !_tempCollidersTargetChildrenIds.ContainsKey( parent.gameObject.GetInstanceID() ) )
                {
                    _tempCollidersTargetChildrenIds.Add( parent.gameObject.GetInstanceID(),
                        new List<int>() );
                }

                _tempCollidersTargetChildrenIds[ parent.gameObject.GetInstanceID() ].Add( target.GetInstanceID() );
            }
        }

        private static GameObject CreateTempCollider( GameObject target, Mesh mesh )
        {
            if ( target  == null
                 || mesh == null )
            {
                return null;
            }

            List<Vector3> differentVertices = new List<Vector3>();
            foreach ( Vector3 vertex in mesh.vertices )
            {
                if ( !differentVertices.Contains( vertex ) )
                {
                    differentVertices.Add( vertex );
                }

                if ( differentVertices.Count >= 3 )
                {
                    break;
                }
            }

            if ( differentVertices.Count < 3 )
            {
                return null;
            }

            if ( _tempCollidersTargets.ContainsKey( target.GetInstanceID() ) )
            {
                if ( _tempCollidersTargets[ target.GetInstanceID() ] != null )
                {
                    return _tempCollidersTargets[ target.GetInstanceID() ];
                }

                _tempCollidersTargets.Remove( target.GetInstanceID() );
            }

            string     name    = target.GetInstanceID().ToString();
            GameObject tempObj = new GameObject( name );
            tempObj.hideFlags = HideFlags.HideAndDontSave;
            _tempCollidersIds.Add( tempObj.GetInstanceID(), target );
            tempObj.transform.SetParent( parentCollider.transform );
            tempObj.transform.position   = target.transform.position;
            tempObj.transform.rotation   = target.transform.rotation;
            tempObj.transform.localScale = target.transform.lossyScale;
            _tempCollidersTargets.Add( target.GetInstanceID(), tempObj );
            AddParentsIds( target );

            MeshUtils.AddCollider( mesh, tempObj );
            return tempObj;
        }

        public static void AddTempCollider( GameObject obj )
        {

            bool ObjectIsActiveAndWithoutCollider( GameObject go )
            {
                if ( !go.activeInHierarchy )
                {
                    return false;
                }

                Collider collider = go.GetComponent<Collider>();
                if ( collider == null )
                {
                    return true;
                }

                if ( collider is MeshCollider )
                {
                    MeshCollider meshCollider = collider as MeshCollider;
                    if ( meshCollider.sharedMesh == null )
                    {
                        return true;
                    }
                }

                return collider.isTrigger;
            }

            MeshFilter[] meshFilters = obj.GetComponentsInChildren<MeshFilter>()
                                          .Where( mf => ObjectIsActiveAndWithoutCollider( mf.gameObject ) ).ToArray();
            foreach ( MeshFilter meshFilter in meshFilters )
            {
                if ( staticData.tempCollidersAction == PWBData.TempCollidersAction.CREATE_ALL_AT_ONCE )
                {
                    CreateTempCollider( meshFilter.gameObject, meshFilter.sharedMesh );
                }
                else
                {
                    _meshFilterOctree.Add( meshFilter, BoundsUtils.GetBounds( meshFilter.transform ) );
                    _meshFilterPointOctree.Add( meshFilter, meshFilter.transform.position );
                }
            }

            SkinnedMeshRenderer[] skinnedMeshRenderers = obj.GetComponentsInChildren<SkinnedMeshRenderer>()
                                                            .Where( smr => ObjectIsActiveAndWithoutCollider( smr.gameObject ) ).ToArray();
            foreach ( SkinnedMeshRenderer renderer in skinnedMeshRenderers )
            {
                CreateTempCollider( renderer.gameObject, renderer.sharedMesh );
            }

            SpriteRenderer[] spriteRenderers = obj.GetComponentsInChildren<SpriteRenderer>();
            foreach ( SpriteRenderer spriteRenderer in spriteRenderers )
            {
                GameObject target = spriteRenderer.gameObject;
                if ( !target.activeInHierarchy )
                {
                    continue;
                }

                if ( spriteRenderer.sprite == null )
                {
                    continue;
                }

                if ( _tempCollidersTargets.ContainsKey( target.GetInstanceID() ) )
                {
                    if ( _tempCollidersTargets[ target.GetInstanceID() ] != null )
                    {
                        return;
                    }

                    _tempCollidersTargets.Remove( target.GetInstanceID() );
                }

                string     name    = spriteRenderer.gameObject.GetInstanceID().ToString();
                GameObject tempObj = new GameObject( name );
                tempObj.hideFlags = HideFlags.HideAndDontSave;
                _tempCollidersIds.Add( tempObj.GetInstanceID(), spriteRenderer.gameObject );
                tempObj.transform.SetParent( parentCollider.transform );
                tempObj.transform.position   = spriteRenderer.transform.position;
                tempObj.transform.rotation   = spriteRenderer.transform.rotation;
                tempObj.transform.localScale = spriteRenderer.transform.lossyScale;
                _tempCollidersTargets.Add( target.GetInstanceID(), tempObj );
                AddParentsIds( target );
                BoxCollider boxCollider = tempObj.AddComponent<BoxCollider>();
                boxCollider.size = (Vector3)( spriteRenderer.sprite.rect.size / spriteRenderer.sprite.pixelsPerUnit )
                                   + new Vector3( 0f, 0f, 0.01f );
                Collider2D collider = spriteRenderer.GetComponent<Collider2D>();
                if ( collider != null
                     && !collider.isTrigger )
                {
                    continue;
                }

                tempObj           = new GameObject( name );
                tempObj.hideFlags = HideFlags.HideAndDontSave;
                _tempCollidersIds.Add( tempObj.GetInstanceID(), spriteRenderer.gameObject );
                tempObj.transform.SetParent( parentCollider.transform );
                tempObj.transform.position   = spriteRenderer.transform.position;
                tempObj.transform.rotation   = spriteRenderer.transform.rotation;
                tempObj.transform.localScale = spriteRenderer.transform.lossyScale;
                BoxCollider2D boxCollider2D = tempObj.AddComponent<BoxCollider2D>();
                boxCollider2D.size = spriteRenderer.sprite.rect.size / spriteRenderer.sprite.pixelsPerUnit;
            }
        }

        public static void CreateTempCollidersWithinFrustum( Camera cam )
        {
            if ( _meshFilterOctree.Count == 0 )
            {
                return;
            }

            List<MeshFilter> filters = _meshFilterOctree.GetWithinFrustum( cam );
            updatingTempColliders = true;
            foreach ( MeshFilter filter in filters )
            {
                CreateTempCollider( filter.gameObject, filter.sharedMesh );
                _meshFilterOctree.Remove( filter );
                _meshFilterPointOctree.Remove( filter );
            }

        }

        private static readonly Vector2 INVALID_MOUSE_POS = new Vector2( 999999, 999999 );
        private static          Vector2 _prevMousePos     = INVALID_MOUSE_POS;

        public static void CreateTempCollidersNearMouseHit( float radius )
        {
            if ( _prevMousePos == Event.current.mousePosition )
            {
                return;
            }

            if ( _prevMousePos == INVALID_MOUSE_POS )
            {
                _prevMousePos = Event.current.mousePosition;
                return;
            }

            void CreateColliders( Vector2 GUIPoint )
            {
                Ray                     mouseRay = HandleUtility.GUIPointToWorldRay( GUIPoint );
                IEnumerable<MeshFilter> filters  = _meshFilterPointOctree.GetNearby( mouseRay, radius ).Where( o => o != null );
                updatingTempColliders = true;
                foreach ( MeshFilter filter in filters )
                {
                    CreateTempCollider( filter.gameObject, filter.sharedMesh );
                    _meshFilterOctree.Remove( filter );
                    _meshFilterPointOctree.Remove( filter );
                }

                List<MeshFilter> FilterList = new List<MeshFilter>();
                _meshFilterOctree.GetColliding( FilterList, mouseRay );
                foreach ( MeshFilter filter in filters )
                {
                    CreateTempCollider( filter.gameObject, filter.sharedMesh );
                    _meshFilterOctree.Remove( filter );
                    _meshFilterPointOctree.Remove( filter );
                }
            }

            Vector2 delta     = Event.current.mousePosition - _prevMousePos;
            float   distance  = delta.magnitude;
            Vector2 direction = delta.normalized;
            if ( distance < radius )
            {
                CreateColliders( Event.current.mousePosition );
            }
            else
            {
                for ( float d = radius; d < distance; d += radius )
                {
                    CreateColliders( _prevMousePos + direction * d );
                }
            }

            _prevMousePos = Event.current.mousePosition;
        }

        public static void DestroyTempCollider( int objId )
        {
            if ( !_tempCollidersTargets.ContainsKey( objId ) )
            {
                return;
            }

            GameObject temCollider = _tempCollidersTargets[ objId ];
            if ( temCollider == null )
            {
                _tempCollidersTargets.Remove( objId );
                return;
            }

            int tempId = temCollider.GetInstanceID();
            _tempCollidersIds.Remove( tempId );
            _tempCollidersTargets.Remove( objId );
            _tempCollidersTargetParentsIds.Remove( objId );
            Object.DestroyImmediate( temCollider );
        }

        public static void DestroyTempColliders()
        {
            _tempCollidersIds.Clear();
            _tempCollidersTargets.Clear();
            _tempCollidersTargetParentsIds.Clear();
            _tempCollidersTargetChildrenIds.Clear();
            GameObject parentObj = GameObject.Find( PARENT_COLLIDER_NAME );
            if ( parentObj != null )
            {
                Object.DestroyImmediate( parentObj );
            }

            parentColliderId = -1;
        }

        public static void UpdateTempCollidersTransforms( GameObject[] objects )
        {
            foreach ( GameObject obj in objects )
            {
                int  parentId = obj.GetInstanceID();
                bool isParent = false;
                foreach ( int childId in _tempCollidersTargetParentsIds.Keys )
                {
                    List<int> parentsIds = _tempCollidersTargetParentsIds[ childId ];
                    if ( parentsIds.Contains( parentId ) )
                    {
                        isParent = true;
                        break;
                    }
                }

                if ( !isParent )
                {
                    continue;
                }

                foreach ( int id in _tempCollidersTargetChildrenIds[ parentId ].ToArray() )
                {
                    if ( !_tempCollidersTargets.ContainsKey( id ) )
                    {
                        _tempCollidersTargetChildrenIds[ parentId ].Remove( id );
                        continue;
                    }

                    GameObject tempCollider = _tempCollidersTargets[ id ];
                    if ( tempCollider == null )
                    {
                        _tempCollidersTargetChildrenIds[ parentId ].Remove( id );
                        _tempCollidersTargets.Remove( id );
                        continue;
                    }

                    GameObject childObj = (GameObject)EditorUtility.InstanceIDToObject( id );
                    if ( childObj == null )
                    {
                        continue;
                    }

                    tempCollider.transform.position   = childObj.transform.position;
                    tempCollider.transform.rotation   = childObj.transform.rotation;
                    tempCollider.transform.localScale = childObj.transform.lossyScale;
                }
            }
        }

        public static void SetActiveTempColliders( GameObject[] objects, bool value )
        {
            foreach ( GameObject obj in objects )
            {
                if ( obj == null )
                {
                    continue;
                }

                if ( !obj.activeInHierarchy )
                {
                    continue;
                }

                int  parentId = obj.GetInstanceID();
                bool isParent = false;
                foreach ( int childId in _tempCollidersTargetParentsIds.Keys )
                {
                    List<int> parentsIds = _tempCollidersTargetParentsIds[ childId ];
                    if ( parentsIds.Contains( parentId ) )
                    {
                        isParent = true;
                        break;
                    }
                }

                if ( !isParent )
                {
                    continue;
                }

                foreach ( int id in _tempCollidersTargetChildrenIds[ parentId ].ToArray() )
                {
                    if ( !_tempCollidersTargets.ContainsKey( id ) )
                    {
                        _tempCollidersTargetChildrenIds[ parentId ].Remove( id );
                        continue;
                    }

                    GameObject tempCollider = _tempCollidersTargets[ id ];
                    if ( tempCollider == null )
                    {
                        _tempCollidersTargetChildrenIds[ parentId ].Remove( id );
                        _tempCollidersTargets.Remove( id );
                        continue;
                    }

                    GameObject childObj = (GameObject)EditorUtility.InstanceIDToObject( id );
                    if ( childObj == null )
                    {
                        continue;
                    }

                    tempCollider.SetActive( value );
                    tempCollider.transform.position   = childObj.transform.position;
                    tempCollider.transform.rotation   = childObj.transform.rotation;
                    tempCollider.transform.localScale = childObj.transform.lossyScale;
                }
            }
        }

        public static GameObject[] GetTempColliders( GameObject obj )
        {
            int  parentId = obj.GetInstanceID();
            bool isParent = false;
            foreach ( int childId in _tempCollidersTargetParentsIds.Keys )
            {
                List<int> parentsIds = _tempCollidersTargetParentsIds[ childId ];
                if ( parentsIds.Contains( parentId ) )
                {
                    isParent = true;
                    break;
                }
            }

            if ( !isParent )
            {
                return null;
            }

            List<GameObject> tempColliders = new List<GameObject>();
            foreach ( int id in _tempCollidersTargetChildrenIds[ parentId ].ToArray() )
            {
                if ( !_tempCollidersTargets.ContainsKey( id ) )
                {
                    _tempCollidersTargetChildrenIds[ parentId ].Remove( id );
                    continue;
                }

                GameObject tempCollider = _tempCollidersTargets[ id ];
                if ( tempCollider == null )
                {
                    _tempCollidersTargetChildrenIds[ parentId ].Remove( id );
                    _tempCollidersTargets.Remove( id );
                    continue;
                }

                tempColliders.Add( tempCollider );
            }

            return tempColliders.ToArray();
        }

        #endregion

    }

    [Serializable]
    public class PWBData
    {

        #region Statics and Constants

        public const string DATA_DIR               = "Data";
        public const string FILE_NAME              = "PWBData";
        public const string FULL_FILE_NAME         = FILE_NAME + ".txt";
        public const string RELATIVE_TOOL_DIR      = "PluginMaster/DesignTools/Editor/PrefabWorldBuilder";
        public const string RELATIVE_RESOURCES_DIR = RELATIVE_TOOL_DIR      + "/Resources";
        public const string RELATIVE_DATA_DIR      = RELATIVE_RESOURCES_DIR + "/" + DATA_DIR;
        public const string PALETTES_DIR           = "Palettes";
        public const string VERSION                = "3.9";

        #endregion

        #region Serialized

        [SerializeField] private string               _version = VERSION;
        [SerializeField] private string               _rootDirectory;
        [SerializeField] private int                  _autoSavePeriodMinutes = 1;
        [SerializeField] private bool                 _undoBrushProperties   = true;
        [SerializeField] private bool                 _undoPalette           = true;
        [SerializeField] private int                  _controlPointSize      = 1;
        [SerializeField] private bool                 _closeAllWindowsWhenClosingTheToolbar;
        [SerializeField] private bool                 _selectTheNextPaletteInAlphabeticalOrder = true;
        [SerializeField] private int                  _thumbnailLayer                          = 7;
        [SerializeField] private UnsavedChangesAction _unsavedChangesAction                    = UnsavedChangesAction.ASK;
        [SerializeField] private TempCollidersAction  _tempCollidersAction                     = TempCollidersAction.CREATE_ALL_AT_ONCE;

        [SerializeField] private PaletteManager _paletteManager = PaletteManager.instance;

        [SerializeField] private PinManager         pinManager          = PinManager.instance as PinManager;
        [SerializeField] private BrushManager       _brushManager       = BrushManager.instance as BrushManager;
        [SerializeField] private GravityToolManager _gravityToolManager = GravityToolManager.instance as GravityToolManager;
        [SerializeField] private LineManager        _lineManager        = LineManager.instance as LineManager;
        [SerializeField] private ShapeManager       _shapeManager       = ShapeManager.instance as ShapeManager;
        [SerializeField] private TilingManager      _tilingManager      = TilingManager.instance as TilingManager;
        [SerializeField] private ReplacerManager    _replacerManager    = ReplacerManager.instance as ReplacerManager;
        [SerializeField] private EraserManager      _eraserManager      = EraserManager.instance as EraserManager;

        [SerializeField]
        private SelectionToolManager _selectionToolManager = SelectionToolManager.instance as SelectionToolManager;

        [SerializeField] private ExtrudeManager _extrudeSettings = ExtrudeManager.instance as ExtrudeManager;
        [SerializeField] private MirrorManager  _mirrorManager   = MirrorManager.instance as MirrorManager;

        [SerializeField] private SnapManager _snapManager = new SnapManager();

        #endregion

        #region Public Enums

        public enum TempCollidersAction
        {
            NEVER_CREATE,
            CREATE_ALL_AT_ONCE,
            CREATE_WITHIN_FRUSTRUM,
        }

        public enum UnsavedChangesAction
        {
            ASK,
            SAVE,
            DISCARD,
        }

        #endregion

        #region Public Properties

        public int autoSavePeriodMinutes
        {
            get => _autoSavePeriodMinutes;
            set
            {
                value = Mathf.Clamp( value, 1, 10 );
                if ( _autoSavePeriodMinutes == value )
                {
                    return;
                }

                _autoSavePeriodMinutes = value;
                SaveAndUpdateVersion();
            }
        }

        public bool closeAllWindowsWhenClosingTheToolbar
        {
            get => _closeAllWindowsWhenClosingTheToolbar;
            set
            {
                if ( _closeAllWindowsWhenClosingTheToolbar == value )
                {
                    return;
                }

                _closeAllWindowsWhenClosingTheToolbar = value;
                SaveAndUpdateVersion();
            }
        }

        public int controPointSize
        {
            get => _controlPointSize;
            set
            {
                if ( _controlPointSize == value )
                {
                    return;
                }

                _controlPointSize = value;
                SaveAndUpdateVersion();
            }
        }

        public static string dataPath => PWBSettings.fullDataDir + "/" + FULL_FILE_NAME;

        public string documentationPath => rootDirectory + "/Documentation/Prefab World Builder Documentation.pdf";

        public static string palettesDirectory
        {
            get
            {
                string dir = PWBSettings.fullDataDir + "/" + PALETTES_DIR;
                if ( !Directory.Exists( dir ) )
                {
                    Directory.CreateDirectory( dir );
                }

                return dir;
            }
        }

        public bool saving { get; private set; }

        public bool selectTheNextPaletteInAlphabeticalOrder
        {
            get => _selectTheNextPaletteInAlphabeticalOrder;
            set
            {
                if ( _selectTheNextPaletteInAlphabeticalOrder == value )
                {
                    return;
                }

                _selectTheNextPaletteInAlphabeticalOrder = value;
                SaveAndUpdateVersion();
            }
        }

        public TempCollidersAction tempCollidersAction
        {
            get => _tempCollidersAction;
            set
            {
                if ( _tempCollidersAction == value )
                {
                    return;
                }

                _tempCollidersAction = value;
                SaveAndUpdateVersion();
            }
        }

        public int thumbnailLayer
        {
            get => _thumbnailLayer;
            set
            {
                value = Mathf.Clamp( value, 0, 31 );
                if ( _thumbnailLayer == value )
                {
                    return;
                }

                _thumbnailLayer = value;
                SaveAndUpdateVersion();
            }
        }

        public bool undoBrushProperties
        {
            get => _undoBrushProperties;
            set
            {
                if ( _undoBrushProperties == value )
                {
                    return;
                }

                _undoBrushProperties = value;
                SaveAndUpdateVersion();
            }
        }

        public bool undoPalette
        {
            get => _undoPalette;
            set
            {
                if ( _undoPalette == value )
                {
                    return;
                }

                _undoPalette = value;
                SaveAndUpdateVersion();
            }
        }

        public UnsavedChangesAction unsavedChangesAction
        {
            get => _unsavedChangesAction;
            set
            {
                if ( _unsavedChangesAction == value )
                {
                    return;
                }

                _unsavedChangesAction = value;
                SaveAndUpdateVersion();
            }
        }

        public string version => _version;

        #endregion

        #region Public Methods

        public static void DeleteFile()
        {
            string fullFilePath = dataPath;
            if ( File.Exists( fullFilePath ) )
            {
                File.Delete( fullFilePath );
            }

            string metaPath = fullFilePath += ".meta";
            if ( File.Exists( metaPath ) )
            {
                File.Delete( metaPath );
            }
        }

        public static string ReadDataText()
        {
            string fullFilePath = dataPath;
            if ( !File.Exists( fullFilePath ) )
            {
                PWBCore.staticData.Save( false );
            }

            return File.ReadAllText( fullFilePath );
        }

        public void Save() => Save( false );

        public void Save( bool updateVersion )
        {
            saving = true;
            if ( updateVersion )
            {
                VersionUpdate();
            }

            _version = VERSION;
            string jsonString = JsonUtility.ToJson( this, true );
            bool   fileExist  = File.Exists( dataPath );
            File.WriteAllText( dataPath, jsonString );
            if ( !fileExist )
            {
                PWBCore.AssetDatabaseRefresh();
            }

            _savePending = false;
            saving       = false;
        }

        public void SaveAndUpdateVersion() => Save( true );

        public void SaveIfPending()
        {
            if ( _savePending )
            {
                SaveAndUpdateVersion();
            }
        }

        public void SetSavePending() => _savePending = true;

        public void UpdateRootDirectory()
        {
            string[] directories = Directory.GetDirectories( Application.dataPath, "PrefabWorldBuilder",
                SearchOption.AllDirectories ).Where( d => d.Replace( "\\", "/" ).Contains( RELATIVE_TOOL_DIR ) ).ToArray();
            if ( directories.Length == 0 )
            {
                _rootDirectory = Application.dataPath + "/" + RELATIVE_TOOL_DIR;
                _rootDirectory = _rootDirectory.Replace( "\\", "/" );
                Directory.CreateDirectory( _rootDirectory );
            }
            else
            {
                _rootDirectory = directories[ 0 ];
            }

            _rootDirectory = PWBCore.GetRelativePath( _rootDirectory );
        }

        public bool VersionUpdate()
        {
            string currentText = ReadDataText();
            if ( currentText == null )
            {
                return false;
            }

            PWBDataVersion dataVersion = null;
            try
            {
                dataVersion = JsonUtility.FromJson<PWBDataVersion>( currentText );
            }
            catch ( Exception e )
            {
                Debug.LogException( e );
            }

            if ( dataVersion == null )
            {
                DeleteFile();
                return false;
            }

            bool V1_9()
            {
                if ( dataVersion.IsOlderThan( "1.10" ) )
                {
                    V1_9_PWBData      v1_9_data       = JsonUtility.FromJson<V1_9_PWBData>( currentText );
                    V1_9_SceneLines[] v1_9_sceneItems = v1_9_data._lineManager._unsavedProfile._sceneLines;
                    if ( v1_9_sceneItems           == null
                         || v1_9_sceneItems.Length == 0 )
                    {
                        return false;
                    }

                    foreach ( V1_9_SceneLines v1_9_sceneData in v1_9_sceneItems )
                    {
                        V1_9_PersistentLineData[] v1_9_sceneLines = v1_9_sceneData._lines;
                        if ( v1_9_sceneItems           == null
                             || v1_9_sceneItems.Length == 0 )
                        {
                            return false;
                        }

                        foreach ( V1_9_PersistentLineData v1_9_sceneLine in v1_9_sceneLines )
                        {
                            if ( v1_9_sceneLines           == null
                                 || v1_9_sceneLines.Length == 0 )
                            {
                                return false;
                            }

                            LineData lineData = new LineData( v1_9_sceneLine._id, v1_9_sceneLine._data._controlPoints,
                                v1_9_sceneLine._objectPoses, v1_9_sceneLine._initialBrushId,
                                v1_9_sceneLine._data._closed, v1_9_sceneLine._settings );
                            LineManager.instance.AddPersistentItem( v1_9_sceneData._sceneGUID, lineData );
                        }
                    }

                    return true;
                }

                return false;
            }

            bool updated = V1_9();

            if ( dataVersion.IsOlderThan( "2.9" ) )
            {
                V2_8_PWBData v2_8_data = JsonUtility.FromJson<V2_8_PWBData>( currentText );
                if ( v2_8_data._paletteManager._paletteData.Length > 0 )
                {
                    PaletteManager.ClearPaletteList();
                }

                foreach ( PaletteData paletteData in v2_8_data._paletteManager._paletteData )
                {
                    paletteData.version = VERSION;
                    PaletteManager.AddPalette( paletteData, save: true );
                }

                TextAsset[] textAssets = Resources.LoadAll<TextAsset>( FILE_NAME );
                for ( int i = 0; i < textAssets.Length; ++i )
                {
                    string assetPath = AssetDatabase.GetAssetPath( textAssets[ i ] );
                    AssetDatabase.DeleteAsset( assetPath );
                }

                PWBCore.staticData.Save( false );

                PrefabPalette.RepainWindow();
                updated = true;
            }

            return updated;
        }

        #endregion

        #region Private Fields

        private bool _savePending;

        #endregion

        #region Private Properties

        private string rootDirectory
        {
            get
            {
                if ( string.IsNullOrEmpty( _rootDirectory ) )
                {
                    UpdateRootDirectory();
                }
                else
                {
                    string fullPath = PWBCore.GetFullPath( _rootDirectory );
                    if ( !Directory.Exists( fullPath ) )
                    {
                        UpdateRootDirectory();
                    }
                }

                return _rootDirectory;
            }
        }

        #endregion

    }

    #endregion

    #region SHORTCUTS

    #region COMBINATION CLASSES

    [Serializable]
    public class PWBShortcutCombination : IEquatable<PWBShortcutCombination>
    {

        #region Serialized

        [SerializeField] protected EventModifiers _modifiers = EventModifiers.None;

        #endregion

        #region Public Properties

        public         bool           alt       => ( modifiers & EventModifiers.Alt )     != 0;
        public         bool           control   => ( modifiers & EventModifiers.Control ) != 0;
        public virtual EventModifiers modifiers => _modifiers;
        public         bool           shift     => ( modifiers & EventModifiers.Shift ) != 0;

        #endregion

        #region Public Constructors

        public PWBShortcutCombination( EventModifiers modifiers )
        {
            _modifiers = FilterModifiers( modifiers );
        }

        #endregion

        #region Public Methods

        public virtual bool Check( PWBShortcut.Group group = PWBShortcut.Group.NONE )
        {
            if ( Event.current == null )
            {
                return false;
            }

            EventModifiers currentModifiers = FilterModifiers( Event.current.modifiers );
            return currentModifiers == modifiers;
        }

        public bool Equals( PWBShortcutCombination other )
        {
            if ( other == null )
            {
                return false;
            }

            return modifiers == other.modifiers;
        }

        public override bool Equals( object other )
        {
            if ( other == null )
            {
                return false;
            }

            if ( !( other is PWBShortcutCombination otherCombination ) )
            {
                return false;
            }

            return Equals( otherCombination );
        }

        public static EventModifiers FilterModifiers( EventModifiers modifiers )
            => modifiers & ( EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift );

        public override int GetHashCode()
        {
            int hashCode = 822824530;
            hashCode = hashCode * -1521134295 + modifiers.GetHashCode();
            return hashCode;
        }

        public static bool operator ==( PWBShortcutCombination lhs, PWBShortcutCombination rhs )
        {
            if ( (object)lhs    == null
                 && (object)rhs == null )
            {
                return true;
            }

            if ( (object)lhs    == null
                 || (object)rhs == null )
            {
                return false;
            }

            return lhs.Equals( rhs );
        }

        public static bool operator !=( PWBShortcutCombination lhs, PWBShortcutCombination rhs ) => !( lhs == rhs );

        #endregion

    }

    [Serializable]
    public class PWBKeyCombination : PWBShortcutCombination, IEquatable<PWBKeyCombination>
    {

        #region Serialized

        [SerializeField] private KeyCode _keyCode = KeyCode.None;

        #endregion

        #region Public Properties

        public virtual KeyCode keyCode => _keyCode;

        #endregion

        #region Public Constructors

        public PWBKeyCombination( KeyCode keyCode, EventModifiers modifiers = EventModifiers.None ) : base( modifiers )
        {
            _keyCode = keyCode;
        }

        public PWBKeyCombination() : base( EventModifiers.None )
        {
        }

        #endregion

        #region Public Methods

        public override bool Check( PWBShortcut.Group group = PWBShortcut.Group.NONE )
        {
            if ( keyCode == KeyCode.None )
            {
                return false;
            }

            if ( Event.current.type       != EventType.KeyDown
                 || Event.current.keyCode != keyCode )
            {
                return false;
            }

            return base.Check();
        }

        public bool Equals( PWBKeyCombination other )
        {
            if ( other == null )
            {
                return false;
            }

            return keyCode == other.keyCode && _modifiers == other._modifiers;
        }

        public override bool Equals( object other )
        {
            if ( other == null )
            {
                return false;
            }

            if ( !( other is PWBKeyCombination otherCombination ) )
            {
                return false;
            }

            return Equals( otherCombination );
        }

        public override int GetHashCode()
        {
            int hashCode = 822824530;
            hashCode = hashCode * -1521134295 + _modifiers.GetHashCode();
            hashCode = hashCode * -1521134295 + keyCode.GetHashCode();
            return hashCode;
        }

        public static bool operator ==( PWBKeyCombination lhs, PWBKeyCombination rhs )
        {
            if ( (object)lhs    == null
                 && (object)rhs == null )
            {
                return true;
            }

            if ( (object)lhs    == null
                 || (object)rhs == null )
            {
                return false;
            }

            return lhs.Equals( rhs );
        }

        public static bool operator !=( PWBKeyCombination lhs, PWBKeyCombination rhs ) => !( lhs == rhs );

        public void Set( KeyCode keyCode, EventModifiers modifiers = EventModifiers.None )
        {
            _keyCode   = keyCode;
            _modifiers = FilterModifiers( modifiers );
        }

        public override string ToString()
        {
            string result = string.Empty;
            if ( keyCode == KeyCode.None )
            {
                return "Disabled";
            }

            if ( control )
            {
                result = "Ctrl";
            }

            if ( alt )
            {
                result += result == string.Empty ? "Alt" : "+Alt";
            }

            if ( shift )
            {
                result += result == string.Empty ? "Shift" : "+Shift";
            }

            if ( result != string.Empty )
            {
                result += "+";
            }

            result += keyCode;
            return result;
        }

        #endregion

    }

    [Serializable]
    public class PWBKeyCombinationUSM : PWBKeyCombination
    {

        #region Public Properties

        public override KeyCode keyCode
        {
            get
            {
                IEnumerable<KeyCombination> keyCombinationSequence = ShortcutManager.instance
                                                                                    .GetShortcutBinding( _shortcutId ).keyCombinationSequence;
                if ( keyCombinationSequence.Count() == 0 )
                {
                    return KeyCode.None;
                }

                return keyCombinationSequence.First().keyCode;
            }
        }

        public override EventModifiers modifiers
        {
            get
            {
                ShortcutModifiers mods = ShortcutManager.instance
                                                        .GetShortcutBinding( _shortcutId ).keyCombinationSequence.First().modifiers;
                EventModifiers result = EventModifiers.None;
                if ( ( mods & ShortcutModifiers.Action )
                     == ShortcutModifiers.Action )
                {
                    result |= EventModifiers.Control;
                }

                if ( ( mods & ShortcutModifiers.Alt )
                     == ShortcutModifiers.Alt )
                {
                    result |= EventModifiers.Alt;
                }

                if ( ( mods & ShortcutModifiers.Shift )
                     == ShortcutModifiers.Shift )
                {
                    result |= EventModifiers.Shift;
                }

                return result;
            }
        }

        #endregion

        #region Public Constructors

        public PWBKeyCombinationUSM( string shortcutId )
            : base( KeyCode.None )
        {
            _shortcutId = shortcutId;
        }

        #endregion

        #region Public Methods

        public void Rebind( KeyCode keyCode, EventModifiers modifiers )
        {
            ShortcutModifiers mods = ShortcutModifiers.None;
            if ( ( modifiers & EventModifiers.Control ) == EventModifiers.Control )
            {
                mods |= ShortcutModifiers.Action;
            }

            if ( ( modifiers & EventModifiers.Alt ) == EventModifiers.Alt )
            {
                mods |= ShortcutModifiers.Alt;
            }

            if ( ( modifiers & EventModifiers.Shift ) == EventModifiers.Shift )
            {
                mods |= ShortcutModifiers.Shift;
            }

            ShortcutManager.instance.RebindShortcut( _shortcutId,
                new ShortcutBinding(
                    new KeyCombination( keyCode, mods ) ) );
        }

        public void Reset()
        {
            ShortcutManager.instance.ClearShortcutOverride( _shortcutId );
        }

        #endregion

        #region Private Fields

        private string _shortcutId;

        #endregion

    }

    [Serializable]
    public class PWBHoldKeysAndClickCombination : PWBKeyCombination
    {

        #region Public Properties

        public bool holdingChanged { get; private set; }

        public bool holdingKeys { get; private set; }

        #endregion

        #region Public Constructors

        public PWBHoldKeysAndClickCombination( KeyCode keyCode, EventModifiers modifiers = EventModifiers.None )
            : base( keyCode, modifiers )
        {
        }

        public PWBHoldKeysAndClickCombination()
        {
        }

        #endregion

        #region Public Methods

        public override bool Check( PWBShortcut.Group group = PWBShortcut.Group.NONE )
        {
            holdingChanged = false;
            if ( Event.current.keyCode == keyCode )
            {
                bool prevHolding = holdingKeys;
                if ( Event.current.type == EventType.KeyDown
                     && base.Check() )
                {
                    holdingKeys = true;
                }
                else if ( Event.current.type == EventType.KeyUp )
                {
                    holdingKeys = false;
                }

                holdingChanged = prevHolding != holdingKeys;
            }

            return holdingKeys && Event.current.button == 0 && Event.current.type == EventType.MouseDown;
        }

        public override string ToString()
        {
            string result = base.ToString();
            if ( keyCode != KeyCode.None )
            {
                result = "Hold " + result + " + Click";
            }

            return result;
        }

        #endregion

        #region Private Fields

        #endregion

    }

    [Serializable]
    public class PWBMouseCombination : PWBShortcutCombination, IEquatable<PWBMouseCombination>
    {

        #region Serialized

        [SerializeField] private MouseEvents _mouseEvent = MouseEvents.NONE;

        #endregion

        #region Public Enums

        public enum MouseEvents
        {
            NONE,
            SCROLL_WHEEL,
            DRAG_R_H,
            DRAG_R_V,
            DRAG_M_H,
            DRAG_M_V,
        }

        #endregion

        #region Public Properties

        public float delta => mouseEvent == MouseEvents.SCROLL_WHEEL ? Event.current.delta.y
            : isHorizontalDragEvent                                  ? Event.current.delta.x : -Event.current.delta.y;

        public bool isHorizontalDragEvent => mouseEvent == MouseEvents.DRAG_R_H || mouseEvent == MouseEvents.DRAG_M_H;
        public bool isMDrag               => mouseEvent == MouseEvents.DRAG_M_H || mouseEvent == MouseEvents.DRAG_M_V;

        public bool isMouseDragEvent => mouseEvent    == MouseEvents.DRAG_R_H
                                        || mouseEvent == MouseEvents.DRAG_R_V
                                        || mouseEvent == MouseEvents.DRAG_M_H
                                        || mouseEvent == MouseEvents.DRAG_M_V;

        public bool isRDrag => mouseEvent == MouseEvents.DRAG_R_H || mouseEvent == MouseEvents.DRAG_R_V;

        public MouseEvents mouseEvent => _mouseEvent;

        #endregion

        #region Public Constructors

        public PWBMouseCombination( EventModifiers modifiers, MouseEvents mouseEvent ) : base( modifiers )
        {
            _mouseEvent = mouseEvent;
        }

        #endregion

        #region Public Methods

        public override bool Check( PWBShortcut.Group group = PWBShortcut.Group.NONE )
        {
            if ( mouseEvent == MouseEvents.NONE )
            {
                return false;
            }

            if ( FilterModifiers( Event.current.modifiers ) == EventModifiers.None )
            {
                return false;
            }

            if ( !base.Check() )
            {
                return false;
            }

            if ( isMouseDragEvent )
            {
                if ( Event.current.type != EventType.MouseDrag )
                {
                    return false;
                }

                if ( isRDrag && Event.current.button != 1 )
                {
                    return false;
                }

                if ( isMDrag && Event.current.button != 2 )
                {
                    return false;
                }

                bool xIsGreaterThanY = Mathf.Abs( Event.current.delta.x ) > Mathf.Abs( Event.current.delta.y );
                if ( isHorizontalDragEvent && !xIsGreaterThanY )
                {
                    PWBMouseCombination other = new PWBMouseCombination( base.modifiers,
                        mouseEvent == MouseEvents.DRAG_R_H ? MouseEvents.DRAG_R_V : MouseEvents.DRAG_M_V );
                    if ( !PWBSettings.shortcuts.CombinationExist( other, group ) )
                    {
                        Event.current.Use();
                    }

                    return false;
                }

                if ( !isHorizontalDragEvent && xIsGreaterThanY )
                {
                    PWBMouseCombination other = new PWBMouseCombination( base.modifiers,
                        mouseEvent == MouseEvents.DRAG_R_V ? MouseEvents.DRAG_R_H : MouseEvents.DRAG_M_H );
                    if ( !PWBSettings.shortcuts.CombinationExist( other, group ) )
                    {
                        Event.current.Use();
                    }

                    return false;
                }
            }

            if ( mouseEvent == MouseEvents.SCROLL_WHEEL
                 && !Event.current.isScrollWheel )
            {
                return false;
            }

            Event.current.Use();
            return true;
        }

        public bool Equals( PWBMouseCombination other )
        {
            if ( other == null )
            {
                return false;
            }

            return _mouseEvent == other._mouseEvent && _modifiers == other._modifiers;
        }

        public override bool Equals( object other )
        {
            if ( other == null )
            {
                return false;
            }

            if ( !( other is PWBMouseCombination otherCombination ) )
            {
                return false;
            }

            return Equals( otherCombination );
        }

        public override int GetHashCode()
        {
            int hashCode = 1068782991;
            hashCode = hashCode * -1521134295 + _modifiers.GetHashCode();
            hashCode = hashCode * -1521134295 + _mouseEvent.GetHashCode();
            return hashCode;
        }

        public static bool operator ==( PWBMouseCombination lhs, PWBMouseCombination rhs )
        {
            if ( (object)lhs    == null
                 && (object)rhs == null )
            {
                return true;
            }

            if ( (object)lhs    == null
                 || (object)rhs == null )
            {
                return false;
            }

            return lhs.Equals( rhs );
        }

        public static bool operator !=( PWBMouseCombination lhs, PWBMouseCombination rhs ) => !( lhs == rhs );

        public void Set( EventModifiers modifiers, MouseEvents mouseEvent )
        {
            _modifiers  = FilterModifiers( modifiers );
            _mouseEvent = mouseEvent;
        }

        #endregion

    }

    #endregion

    #region SHORTCUT CLASSES

    [Serializable]
    public class PWBShortcut
    {

        #region Serialized

        [SerializeField] private string _name;
        [SerializeField] private Group  _group = Group.NONE;
        [SerializeField] private bool   _conflicted;

        #endregion

        #region Public Enums

        public enum Group
        {
            NONE      = 0,
            GLOBAL    = 1,
            GRID      = 2,
            PIN       = 4,
            BRUSH     = 8,
            GRAVITY   = 16,
            LINE      = 32,
            SHAPE     = 64,
            TILING    = 128,
            ERASER    = 256,
            REPLACER  = 512,
            SELECTION = 1024,
            PALETTE   = 2048,
        }

        #endregion

        #region Public Properties

        public bool conflicted
        {
            get => _conflicted;
            set => _conflicted = value;
        }

        public Group group => _group;

        public string name => _name;

        #endregion

        #region Public Constructors

        public PWBShortcut( string name, Group group )
        {
            _name  = name;
            _group = group;
        }

        #endregion

    }

    [Serializable]
    public class PWBKeyShortcut : PWBShortcut
    {

        #region Serialized

        [SerializeField]
        protected PWBKeyCombination _keyCombination;

        #endregion

        #region Public Properties

        public virtual PWBKeyCombination combination
        {
            get
            {
                if ( _keyCombination == null )
                {
                    _keyCombination = new PWBKeyCombination();
                }

                return _keyCombination;
            }
        }

        #endregion

        #region Public Constructors

        public PWBKeyShortcut( string name, Group group, KeyCode keyCode, EventModifiers modifiers = EventModifiers.None )
            : base( name, group )
        {
            combination.Set( keyCode, modifiers );
        }

        public PWBKeyShortcut( string name, Group group, PWBKeyCombination keyCombination ) : base( name, group )
        {
            _keyCombination = keyCombination;
        }

        #endregion

        #region Public Methods

        public bool Check()
        {
            if ( PWBIO.gridShorcutEnabled
                 && group != Group.GRID )
            {
                return false;
            }

            return combination.Check( group );
        }

        #endregion

    }

    [Serializable]
    public class PWBHoldKeysAndClickShortcut : PWBKeyShortcut
    {

        #region Public Properties

        public override PWBKeyCombination combination
        {
            get
            {
                if ( _keyCombination == null )
                {
                    _keyCombination = new PWBHoldKeysAndClickCombination();
                }

                return _keyCombination;
            }
        }

        public PWBHoldKeysAndClickCombination holdKeysAndClickCombination => _keyCombination as PWBHoldKeysAndClickCombination;

        #endregion

        #region Public Constructors

        public PWBHoldKeysAndClickShortcut( string         name, Group group, KeyCode keyCode,
                                            EventModifiers modifiers = EventModifiers.None ) : base( name, group, keyCode, modifiers )
        {
        }

        #endregion

    }

    [Serializable]
    public class PWBMouseShortcut : PWBShortcut
    {

        #region Serialized

        [SerializeField]
        private PWBMouseCombination _combination
            = new PWBMouseCombination( EventModifiers.None, PWBMouseCombination.MouseEvents.NONE );

        #endregion

        #region Public Properties

        public PWBMouseCombination combination => _combination;

        #endregion

        #region Public Constructors

        public PWBMouseShortcut( string         name,      Group                           group,
                                 EventModifiers modifiers, PWBMouseCombination.MouseEvents mouseEvent )
            : base( name, group )
        {
            _combination.Set( modifiers, mouseEvent );
        }

        #endregion

        #region Public Methods

        public bool Check() => combination.Check( group );

        #endregion

    }

    #endregion

    [Serializable]
    public class PWBShortcuts
    {

        #region PROFILE

        [SerializeField] private string _profileName = string.Empty;

        public string profileName
        {
            get => _profileName;
            set => _profileName = value;
        }

        public PWBShortcuts( string name )
        {
            _profileName = name;
        }

        public static PWBShortcuts GetDefault( int i )
        {
            if ( i == 0 )
            {
                return new PWBShortcuts( "Default 1" );
            }

            if ( i == 1 )
            {
                PWBShortcuts d2 = new PWBShortcuts( "Default 2" );
                d2.pinMoveHandlesUp.combination.Set( KeyCode.PageUp );
                d2.pinMoveHandlesDown.combination.Set( KeyCode.PageDown );
                d2.pinSelectPivotHandle.combination.Set( KeyCode.Home );
                d2.pinSelectNextHandle.combination.Set( KeyCode.End );
                d2.pinResetScale.combination.Set( KeyCode.Home, EventModifiers.Control | EventModifiers.Shift );

                d2.pinRotate90YCW.combination.Set( KeyCode.LeftArrow, EventModifiers.Control );
                d2._pinRotate90YCCW.combination.Set( KeyCode.RightArrow, EventModifiers.Control );
                d2.pinRotateAStepYCW.combination.Set( KeyCode.LeftArrow,
                    EventModifiers.Control | EventModifiers.Shift );
                d2.pinRotateAStepYCCW.combination.Set( KeyCode.RightArrow,
                    EventModifiers.Control | EventModifiers.Shift );

                d2.pinRotate90XCW.combination.Set( KeyCode.LeftArrow,
                    EventModifiers.Control | EventModifiers.Alt );
                d2.pinRotate90XCCW.combination.Set( KeyCode.RightArrow,
                    EventModifiers.Control | EventModifiers.Alt );
                d2.pinRotateAStepXCW.combination.Set( KeyCode.LeftArrow,
                    EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift );
                d2.pinRotateAStepXCCW.combination.Set( KeyCode.RightArrow,
                    EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift );

                d2.pinResetRotation.combination.Set( KeyCode.Home, EventModifiers.Control );

                d2.pinAdd1UnitToSurfDist.combination.Set( KeyCode.UpArrow,
                    EventModifiers.Control | EventModifiers.Alt );
                d2.pinSubtract1UnitFromSurfDist.combination.Set( KeyCode.DownArrow,
                    EventModifiers.Control | EventModifiers.Alt );
                d2.pinAdd01UnitToSurfDist.combination.Set( KeyCode.UpArrow,
                    EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift );
                d2.pinSubtract01UnitFromSurfDist.combination.Set( KeyCode.DownArrow,
                    EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift );

                d2.lineToggleCurve.combination.Set( KeyCode.PageDown );
                d2.lineToggleClosed.combination.Set( KeyCode.End );

                d2.selectionRotate90XCW.combination.Set( KeyCode.PageUp,
                    EventModifiers.Control | EventModifiers.Shift );
                d2.selectionRotate90XCCW.combination.Set( KeyCode.PageDown,
                    EventModifiers.Control | EventModifiers.Shift );
                d2.selectionRotate90YCW.combination.Set( KeyCode.LeftArrow,
                    EventModifiers.Control | EventModifiers.Alt );
                d2.selectionRotate90YCCW.combination.Set( KeyCode.RightArrow,
                    EventModifiers.Control | EventModifiers.Alt );
                d2.selectionRotate90ZCW.combination.Set( KeyCode.UpArrow,
                    EventModifiers.Control | EventModifiers.Alt );
                d2.selectionRotate90ZCCW.combination.Set( KeyCode.DownArrow,
                    EventModifiers.Control | EventModifiers.Alt );

                d2.brushRadius.combination.Set( EventModifiers.Shift, PWBMouseCombination.MouseEvents.DRAG_R_H );
                return d2;
            }

            return null;
        }

        #endregion

        #region KEY COMBINATIONS

        #region GRID

        [SerializeField]
        private PWBKeyShortcut _gridEnableShortcuts = new PWBKeyShortcut( "Enable grid shorcuts",
            PWBShortcut.Group.GLOBAL | PWBShortcut.Group.GRID, KeyCode.G, EventModifiers.Control );

        [SerializeField]
        private PWBKeyShortcut _gridToggle = new PWBKeyShortcut( "Toggle grid",
            PWBShortcut.Group.GRID, KeyCode.G, EventModifiers.Control );

        [SerializeField]
        private PWBKeyShortcut _gridToggleSnapping = new PWBKeyShortcut( "Toggle snapping",
            PWBShortcut.Group.GRID, KeyCode.H, EventModifiers.Control );

        [SerializeField]
        private PWBKeyShortcut _gridToggleLock = new PWBKeyShortcut( "Toggle grid lock",
            PWBShortcut.Group.GRID, KeyCode.L, EventModifiers.Control );

        [SerializeField]
        private PWBKeyShortcut _gridSetOriginPosition = new PWBKeyShortcut( "Set the origin to the active gameobject position",
            PWBShortcut.Group.GRID, KeyCode.W, EventModifiers.Control );

        [SerializeField]
        private PWBKeyShortcut _gridSetOriginRotation
            = new PWBKeyShortcut( "Set the grid rotation to the active gameobject rotation",
                PWBShortcut.Group.GRID, KeyCode.E, EventModifiers.Control );

        [SerializeField]
        private PWBKeyShortcut _gridSetSize = new PWBKeyShortcut( "Set the snap value to the size of the active gameobject",
            PWBShortcut.Group.GRID, KeyCode.R, EventModifiers.Control );

        [SerializeField]
        private PWBKeyShortcut _gridFrameOrigin = new PWBKeyShortcut( "Frame grid origin",
            PWBShortcut.Group.GRID, KeyCode.Q, EventModifiers.Control );

        [SerializeField]
        private PWBKeyShortcut _gridTogglePositionHandle = new PWBKeyShortcut( "Toggle Postion Handle",
            PWBShortcut.Group.GRID, KeyCode.W, EventModifiers.Control | EventModifiers.Alt );

        [SerializeField]
        private PWBKeyShortcut _gridToggleRotationHandle = new PWBKeyShortcut( "Toggle Rotation Handle",
            PWBShortcut.Group.GRID, KeyCode.E, EventModifiers.Control | EventModifiers.Alt );

        [SerializeField]
        private PWBKeyShortcut _gridToggleSpacingHandle = new PWBKeyShortcut( "Toggle Spacing Handle",
            PWBShortcut.Group.GRID, KeyCode.R, EventModifiers.Control | EventModifiers.Alt );

        [SerializeField]
        private PWBKeyShortcut _gridMoveOriginUp = new PWBKeyShortcut( "Move the origin one step up",
            PWBShortcut.Group.GRID, KeyCode.J, EventModifiers.Control | EventModifiers.Alt );

        [SerializeField]
        private PWBKeyShortcut _gridMoveOriginDown = new PWBKeyShortcut( "Move the origin one step down",
            PWBShortcut.Group.GRID, KeyCode.M, EventModifiers.Control | EventModifiers.Alt );

        public PWBKeyShortcut gridEnableShortcuts      => _gridEnableShortcuts;
        public PWBKeyShortcut gridToggle               => _gridToggle;
        public PWBKeyShortcut gridToggleSnaping        => _gridToggleSnapping;
        public PWBKeyShortcut gridToggleLock           => _gridToggleLock;
        public PWBKeyShortcut gridSetOriginPosition    => _gridSetOriginPosition;
        public PWBKeyShortcut gridSetOriginRotation    => _gridSetOriginRotation;
        public PWBKeyShortcut gridSetSize              => _gridSetSize;
        public PWBKeyShortcut gridFrameOrigin          => _gridFrameOrigin;
        public PWBKeyShortcut gridTogglePositionHandle => _gridTogglePositionHandle;
        public PWBKeyShortcut gridToggleRotationHandle => _gridToggleRotationHandle;
        public PWBKeyShortcut gridToggleSpacingHandle  => _gridToggleSpacingHandle;
        public PWBKeyShortcut gridMoveOriginUp         => _gridMoveOriginUp;
        public PWBKeyShortcut gridMoveOriginDown       => _gridMoveOriginDown;

        #endregion

        #region PIN

        [SerializeField]
        private PWBKeyShortcut _pinMoveHandlesUp = new PWBKeyShortcut( "Move handles up",
            PWBShortcut.Group.PIN, KeyCode.U, EventModifiers.Control | EventModifiers.Shift );

        [SerializeField]
        private PWBKeyShortcut _pinMoveHandlesDown = new PWBKeyShortcut( "Move handles down",
            PWBShortcut.Group.PIN, KeyCode.J, EventModifiers.Control | EventModifiers.Shift );

        [SerializeField]
        private PWBKeyShortcut _pinSelectNextHandle = new PWBKeyShortcut( "Select the next handle as active",
            PWBShortcut.Group.PIN, KeyCode.Y, EventModifiers.Control | EventModifiers.Shift );

        [SerializeField]
        private PWBKeyShortcut _pinSelectPrevHandle = new PWBKeyShortcut( "Select the Previous handle as active",
            PWBShortcut.Group.PIN, KeyCode.H, EventModifiers.Control | EventModifiers.Shift );

        [SerializeField]
        private PWBKeyShortcut _pinSelectPivotHandle = new PWBKeyShortcut( "Set the pivot as the active handle",
            PWBShortcut.Group.PIN, KeyCode.T, EventModifiers.Control | EventModifiers.Shift );

        [SerializeField]
        private PWBKeyShortcut _pinToggleRepeatItem = new PWBKeyShortcut( "Toggle repeat item option",
            PWBShortcut.Group.PIN, KeyCode.T, EventModifiers.Control );

        [SerializeField]
        private PWBKeyShortcut _pinResetScale = new PWBKeyShortcut( "Reset scale",
            PWBShortcut.Group.PIN, KeyCode.Period, EventModifiers.Control | EventModifiers.Shift );

        [SerializeField]
        private PWBKeyShortcut _pinRotate90YCW = new PWBKeyShortcut( "Rotate 90º clockwise around Y axis",
            PWBShortcut.Group.PIN, KeyCode.Q, EventModifiers.Control );

        [SerializeField]
        private PWBKeyShortcut _pinRotate90YCCW = new PWBKeyShortcut( "Rotate 90º counterclockwise around Y axis",
            PWBShortcut.Group.PIN, KeyCode.W, EventModifiers.Control );

        [SerializeField]
        private PWBKeyShortcut _pinRotateAStepYCW = new PWBKeyShortcut( "Rotate clockwise in small steps around the Y axis",
            PWBShortcut.Group.PIN, KeyCode.Q, EventModifiers.Control | EventModifiers.Shift );

        [SerializeField]
        private PWBKeyShortcut _pinRotateAStepYCCW
            = new PWBKeyShortcut( "Rotate counterclockwise in small steps around the Y axis",
                PWBShortcut.Group.PIN, KeyCode.W, EventModifiers.Control | EventModifiers.Shift );

        [SerializeField]
        private PWBKeyShortcut _pinRotate90XCW = new PWBKeyShortcut( "Rotate 90º clockwise around X axis",
            PWBShortcut.Group.PIN, KeyCode.K, EventModifiers.Control | EventModifiers.Alt );

        [SerializeField]
        private PWBKeyShortcut _pinRotate90XCCW = new PWBKeyShortcut( "Rotate 90º counterclockwise around X axis",
            PWBShortcut.Group.PIN, KeyCode.L, EventModifiers.Control | EventModifiers.Alt );

        [SerializeField]
        private PWBKeyShortcut _pinRotateAStepXCW = new PWBKeyShortcut( "Rotate clockwise in small steps around the X axis",
            PWBShortcut.Group.PIN, KeyCode.K, EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift );

        [SerializeField]
        private PWBKeyShortcut _pinRotateAStepXCCW
            = new PWBKeyShortcut( "Rotate counterclockwise in small steps around the X axis",
                PWBShortcut.Group.PIN, KeyCode.L, EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift );

        [SerializeField]
        private PWBKeyShortcut _pinRotate90ZCW = new PWBKeyShortcut( "Rotate 90º clockwise around Z axis",
            PWBShortcut.Group.PIN, KeyCode.Period, EventModifiers.Control | EventModifiers.Alt );

        [SerializeField]
        private PWBKeyShortcut _pinRotate90ZCCW = new PWBKeyShortcut( "Rotate 90º counterclockwise around Z axis",
            PWBShortcut.Group.PIN, KeyCode.Comma, EventModifiers.Control | EventModifiers.Alt );

        [SerializeField]
        private PWBKeyShortcut _pinRotateAStepZCW = new PWBKeyShortcut( "Rotate clockwise in small steps around the Z axis",
            PWBShortcut.Group.PIN, KeyCode.Period, EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift );

        [SerializeField]
        private PWBKeyShortcut _pinRotateAStepZCCW
            = new PWBKeyShortcut( "Rotate counterclockwise in small steps around the Z axis",
                PWBShortcut.Group.PIN, KeyCode.Comma, EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift );

        [SerializeField]
        private PWBKeyShortcut _pinResetRotation = new PWBKeyShortcut( "Reset rotation to zero",
            PWBShortcut.Group.PIN, KeyCode.M, EventModifiers.Control | EventModifiers.Shift );

        [SerializeField]
        private PWBKeyShortcut _pinAdd1UnitToSurfDist = new PWBKeyShortcut( "Increase the distance from the surface by 1 unit",
            PWBShortcut.Group.PIN, KeyCode.U, EventModifiers.Control | EventModifiers.Alt );

        [SerializeField]
        private PWBKeyShortcut _pinSubtract1UnitFromSurfDist
            = new PWBKeyShortcut( "Decrease the distance from the surface by 1 unit",
                PWBShortcut.Group.PIN, KeyCode.J, EventModifiers.Control | EventModifiers.Alt );

        [SerializeField]
        private PWBKeyShortcut _pinAdd01UnitToSurfDist
            = new PWBKeyShortcut( "Increase the distance from the surface by 0.1 units",
                PWBShortcut.Group.PIN, KeyCode.U, EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift );

        [SerializeField]
        private PWBKeyShortcut _pinSubtract01UnitFromSurfDist
            = new PWBKeyShortcut( "Decrease the distance from the surface by 0.1 units",
                PWBShortcut.Group.PIN, KeyCode.J,
                EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift );

        [SerializeField]
        private PWBKeyShortcut _pinResetSurfDist = new PWBKeyShortcut( "Reset the distance from the surface to zero",
            PWBShortcut.Group.PIN, KeyCode.G, EventModifiers.Control | EventModifiers.Shift );

        [SerializeField]
        private PWBKeyShortcut _pinSelectPreviousItem = new PWBKeyShortcut( "Select previous item in the multi-brush",
            PWBShortcut.Group.PIN, KeyCode.O, EventModifiers.Control | EventModifiers.Alt );

        [SerializeField]
        private PWBKeyShortcut _pinSelectNextItem = new PWBKeyShortcut( "Select next item in the multi-brush",
            PWBShortcut.Group.PIN, KeyCode.N, EventModifiers.Control | EventModifiers.Alt );

        public PWBKeyShortcut pinMoveHandlesUp     => _pinMoveHandlesUp;
        public PWBKeyShortcut pinMoveHandlesDown   => _pinMoveHandlesDown;
        public PWBKeyShortcut pinSelectNextHandle  => _pinSelectNextHandle;
        public PWBKeyShortcut pinSelectPrevHandle  => _pinSelectPrevHandle;
        public PWBKeyShortcut pinSelectPivotHandle => _pinSelectPivotHandle;
        public PWBKeyShortcut pinToggleRepeatItem  => _pinToggleRepeatItem;
        public PWBKeyShortcut pinResetScale        => _pinResetScale;

        public PWBKeyShortcut pinRotate90YCW     => _pinRotate90YCW;
        public PWBKeyShortcut pinRotate90YCCW    => _pinRotate90YCCW;
        public PWBKeyShortcut pinRotateAStepYCW  => _pinRotateAStepYCW;
        public PWBKeyShortcut pinRotateAStepYCCW => _pinRotateAStepYCCW;

        public PWBKeyShortcut pinRotate90XCW     => _pinRotate90XCW;
        public PWBKeyShortcut pinRotate90XCCW    => _pinRotate90XCCW;
        public PWBKeyShortcut pinRotateAStepXCW  => _pinRotateAStepXCW;
        public PWBKeyShortcut pinRotateAStepXCCW => _pinRotateAStepXCCW;

        public PWBKeyShortcut pinRotate90ZCW     => _pinRotate90ZCW;
        public PWBKeyShortcut pinRotate90ZCCW    => _pinRotate90ZCCW;
        public PWBKeyShortcut pinRotateAStepZCW  => _pinRotateAStepZCW;
        public PWBKeyShortcut pinRotateAStepZCCW => _pinRotateAStepZCCW;

        public PWBKeyShortcut pinResetRotation => _pinResetRotation;

        public PWBKeyShortcut pinAdd1UnitToSurfDist         => _pinAdd1UnitToSurfDist;
        public PWBKeyShortcut pinSubtract1UnitFromSurfDist  => _pinSubtract1UnitFromSurfDist;
        public PWBKeyShortcut pinAdd01UnitToSurfDist        => _pinAdd01UnitToSurfDist;
        public PWBKeyShortcut pinSubtract01UnitFromSurfDist => _pinSubtract01UnitFromSurfDist;

        public PWBKeyShortcut pinResetSurfDist => _pinResetSurfDist;

        public PWBKeyShortcut pinSelectPreviousItem => _pinSelectPreviousItem;
        public PWBKeyShortcut pinSelectNextItem     => _pinSelectNextItem;

        #endregion

        #region BRUSH & GRAVITY

        [SerializeField]
        private PWBKeyShortcut _brushUpdatebrushstroke = new PWBKeyShortcut( "Update brushstroke",
            PWBShortcut.Group.BRUSH | PWBShortcut.Group.GRAVITY, KeyCode.Period, EventModifiers.Control | EventModifiers.Shift );

        [SerializeField]
        private PWBKeyShortcut _brushResetRotation = new PWBKeyShortcut( "Reset brush rotation",
            PWBShortcut.Group.BRUSH | PWBShortcut.Group.GRAVITY, KeyCode.M, EventModifiers.Control );

        public PWBKeyShortcut brushUpdatebrushstroke => _brushUpdatebrushstroke;
        public PWBKeyShortcut brushResetRotation     => _brushResetRotation;

        #endregion

        #region GRAVITY

        [SerializeField]
        private PWBKeyShortcut _gravityAdd1UnitToSurfDist = new PWBKeyShortcut( "Increase the distance from the surface by 1 unit",
            PWBShortcut.Group.GRAVITY, KeyCode.U, EventModifiers.Control | EventModifiers.Alt );

        [SerializeField]
        private PWBKeyShortcut _gravitySubtract1UnitFromSurfDist = new PWBKeyShortcut( "Decrease the distance from the surface by 1 unit",
            PWBShortcut.Group.GRAVITY, KeyCode.J, EventModifiers.Control | EventModifiers.Alt );

        [SerializeField]
        private PWBKeyShortcut _gravityAdd01UnitToSurfDist = new PWBKeyShortcut( "Increase the distance from the surface by 0.1 units",
            PWBShortcut.Group.GRAVITY, KeyCode.U,
            EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift );

        [SerializeField]
        private PWBKeyShortcut _gravitySubtract01UnitFromSurfDist
            = new PWBKeyShortcut( "Decrease the distance from the surface by 0.1 units",
                PWBShortcut.Group.GRAVITY, KeyCode.J,
                EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift );

        public PWBKeyShortcut gravityAdd1UnitToSurfDist         => _gravityAdd1UnitToSurfDist;
        public PWBKeyShortcut gravitySubtract1UnitFromSurfDist  => _gravitySubtract1UnitFromSurfDist;
        public PWBKeyShortcut gravityAdd01UnitToSurfDist        => _gravityAdd01UnitToSurfDist;
        public PWBKeyShortcut gravitySubtract01UnitFromSurfDist => _gravitySubtract01UnitFromSurfDist;

        #endregion

        #region EDIT MODE

        [SerializeField]
        private PWBKeyShortcut _editModeDeleteItemAndItsChildren
            = new PWBKeyShortcut( "Delete selected persistent item and its children",
                PWBShortcut.Group.LINE | PWBShortcut.Group.SHAPE | PWBShortcut.Group.TILING,
                KeyCode.Delete, EventModifiers.Alt );

        [SerializeField]
        private PWBKeyShortcut _editModeDeleteItemButNotItsChildren
            = new PWBKeyShortcut( "Delete selected persistent item but not its children",
                PWBShortcut.Group.LINE             | PWBShortcut.Group.SHAPE | PWBShortcut.Group.TILING,
                KeyCode.Delete, EventModifiers.Alt | EventModifiers.Shift );

        [SerializeField]
        private PWBKeyShortcut _editModeSelectParent = new PWBKeyShortcut( "Select parent object",
            PWBShortcut.Group.LINE            | PWBShortcut.Group.SHAPE | PWBShortcut.Group.TILING,
            KeyCode.T, EventModifiers.Control | EventModifiers.Shift );

        [SerializeField]
        private PWBKeyShortcut _editModeToggle = new PWBKeyShortcut( "Toggle edit mode",
            PWBShortcut.Group.LINE             | PWBShortcut.Group.SHAPE | PWBShortcut.Group.TILING,
            KeyCode.Period, EventModifiers.Alt | EventModifiers.Shift );

        public PWBKeyShortcut editModeDeleteItemAndItsChildren    => _editModeDeleteItemAndItsChildren;
        public PWBKeyShortcut editModeDeleteItemButNotItsChildren => _editModeDeleteItemButNotItsChildren;
        public PWBKeyShortcut editModeSelectParent                => _editModeSelectParent;
        public PWBKeyShortcut editModeToggle                      => _editModeToggle;

        #endregion

        #region LINE

        [SerializeField]
        private PWBKeyShortcut _lineSelectAllPoints = new PWBKeyShortcut( "Select all points",
            PWBShortcut.Group.LINE, KeyCode.A, EventModifiers.Control | EventModifiers.Shift );

        [SerializeField]
        private PWBKeyShortcut _lineDeselectAllPoints = new PWBKeyShortcut( "Deselect all points",
            PWBShortcut.Group.LINE, KeyCode.D, EventModifiers.Control | EventModifiers.Shift );

        [SerializeField]
        private PWBKeyShortcut _lineToggleCurve = new PWBKeyShortcut( "Set the previous segment as a Curved or Straight Line",
            PWBShortcut.Group.LINE, KeyCode.Y, EventModifiers.Control | EventModifiers.Shift );

        [SerializeField]
        private PWBKeyShortcut _lineToggleClosed = new PWBKeyShortcut( "Close or open the line",
            PWBShortcut.Group.LINE, KeyCode.O, EventModifiers.Control | EventModifiers.Shift );

        [SerializeField]
        private PWBKeyShortcut _lineEditModeTypeToggle = new PWBKeyShortcut( "Toggle edit mode type",
            PWBShortcut.Group.LINE, KeyCode.Comma, EventModifiers.Alt | EventModifiers.Shift );

        public PWBKeyShortcut lineSelectAllPoints    => _lineSelectAllPoints;
        public PWBKeyShortcut lineDeselectAllPoints  => _lineDeselectAllPoints;
        public PWBKeyShortcut lineToggleCurve        => _lineToggleCurve;
        public PWBKeyShortcut lineToggleClosed       => _lineToggleClosed;
        public PWBKeyShortcut lineEditModeTypeToggle => _lineEditModeTypeToggle;

        #endregion

        #region TILING & SELECTION

        [SerializeField]
        private PWBKeyShortcut _selectionRotate90XCW = new PWBKeyShortcut( "Rotate 90º clockwise around X axis",
            PWBShortcut.Group.TILING | PWBShortcut.Group.SELECTION, KeyCode.U, EventModifiers.Control | EventModifiers.Shift );

        [SerializeField]
        private PWBKeyShortcut _selectionRotate90XCCW = new PWBKeyShortcut( "Rotate 90º counterclockwise around X axis",
            PWBShortcut.Group.TILING | PWBShortcut.Group.SELECTION, KeyCode.J, EventModifiers.Control | EventModifiers.Shift );

        [SerializeField]
        private PWBKeyShortcut _selectionRotate90YCW = new PWBKeyShortcut( "Rotate 90º clockwise around Y axis",
            PWBShortcut.Group.TILING | PWBShortcut.Group.SELECTION, KeyCode.K, EventModifiers.Control | EventModifiers.Alt );

        [SerializeField]
        private PWBKeyShortcut _selectionRotate90YCCW = new PWBKeyShortcut( "Rotate 90º counterclockwise around Y axis",
            PWBShortcut.Group.TILING | PWBShortcut.Group.SELECTION, KeyCode.L, EventModifiers.Control | EventModifiers.Alt );

        [SerializeField]
        private PWBKeyShortcut _selectionRotate90ZCW = new PWBKeyShortcut( "Rotate 90º clockwise around Z axis",
            PWBShortcut.Group.TILING | PWBShortcut.Group.SELECTION, KeyCode.U, EventModifiers.Control | EventModifiers.Alt );

        [SerializeField]
        private PWBKeyShortcut _selectionRotate90ZCCW = new PWBKeyShortcut( "Rotate 90º counterclockwise around Z axis",
            PWBShortcut.Group.TILING | PWBShortcut.Group.SELECTION, KeyCode.J, EventModifiers.Control | EventModifiers.Alt );

        public PWBKeyShortcut selectionRotate90XCW  => _selectionRotate90XCW;
        public PWBKeyShortcut selectionRotate90XCCW => _selectionRotate90XCCW;
        public PWBKeyShortcut selectionRotate90YCW  => _selectionRotate90YCW;
        public PWBKeyShortcut selectionRotate90YCCW => _selectionRotate90YCCW;
        public PWBKeyShortcut selectionRotate90ZCW  => _selectionRotate90ZCW;
        public PWBKeyShortcut selectionRotate90ZCCW => _selectionRotate90ZCCW;

        #endregion

        #region SELECTION

        [SerializeField]
        private PWBKeyShortcut _selectionTogglePositionHandle = new PWBKeyShortcut( "Toggle position handle",
            PWBShortcut.Group.SELECTION, KeyCode.W );

        [SerializeField]
        private PWBKeyShortcut _selectionToggleRotationHandle = new PWBKeyShortcut( "Toggle rotation handle",
            PWBShortcut.Group.SELECTION, KeyCode.E );

        [SerializeField]
        private PWBKeyShortcut _selectionToggleScaleHandle = new PWBKeyShortcut( "Toggle scale handle",
            PWBShortcut.Group.SELECTION, KeyCode.R );

        [SerializeField]
        private PWBKeyShortcut _selectionEditCustomHandle = new PWBKeyShortcut( "Edit custom handle",
            PWBShortcut.Group.SELECTION, KeyCode.U );

        public PWBKeyShortcut selectionTogglePositionHandle => _selectionTogglePositionHandle;
        public PWBKeyShortcut selectionToggleRotationHandle => _selectionToggleRotationHandle;
        public PWBKeyShortcut selectionToggleScaleHandle    => _selectionToggleScaleHandle;
        public PWBKeyShortcut selectionEditCustomHandle     => _selectionEditCustomHandle;

        #endregion

        #region TOOLBAR

        public PWBKeyShortcut toolbarPinToggle { get; } = new PWBKeyShortcut( "Toggle Pin Tool",
            PWBShortcut.Group.GLOBAL, new PWBKeyCombinationUSM( Shortcuts.PWB_TOGGLE_PIN_SHORTCUT_ID ) );

        public PWBKeyShortcut toolbarBrushToggle { get; } = new PWBKeyShortcut( "Toggle Brush Tool",
            PWBShortcut.Group.GLOBAL, new PWBKeyCombinationUSM( Shortcuts.PWB_TOGGLE_BRUSH_SHORTCUT_ID ) );

        public PWBKeyShortcut toolbarGravityToggle { get; } = new PWBKeyShortcut( "Toggle Gravity Tool",
            PWBShortcut.Group.GLOBAL, new PWBKeyCombinationUSM( Shortcuts.PWB_TOGGLE_GRAVITY_SHORTCUT_ID ) );

        public PWBKeyShortcut toolbarLineToggle { get; } = new PWBKeyShortcut( "Toggle Line Tool",
            PWBShortcut.Group.GLOBAL, new PWBKeyCombinationUSM( Shortcuts.PWB_TOGGLE_LINE_SHORTCUT_ID ) );

        public PWBKeyShortcut toolbarShapeToggle { get; } = new PWBKeyShortcut( "Toggle Shape Tool",
            PWBShortcut.Group.GLOBAL, new PWBKeyCombinationUSM( Shortcuts.PWB_TOGGLE_SHAPE_SHORTCUT_ID ) );

        public PWBKeyShortcut toolbarTilingToggle { get; } = new PWBKeyShortcut( "Toggle Tiling Tool",
            PWBShortcut.Group.GLOBAL, new PWBKeyCombinationUSM( Shortcuts.PWB_TOGGLE_TILING_SHORTCUT_ID ) );

        public PWBKeyShortcut toolbarReplacerToggle { get; } = new PWBKeyShortcut( "Toggle Replacer Tool",
            PWBShortcut.Group.GLOBAL, new PWBKeyCombinationUSM( Shortcuts.PWB_TOGGLE_REPLACER_SHORTCUT_ID ) );

        public PWBKeyShortcut toolbarEraserToggle { get; } = new PWBKeyShortcut( "Toggle Eraser Tool",
            PWBShortcut.Group.GLOBAL, new PWBKeyCombinationUSM( Shortcuts.PWB_TOGGLE_ERASER_SHORTCUT_ID ) );

        public PWBKeyShortcut toolbarSelectionToggle { get; } = new PWBKeyShortcut( "Toggle Selection Tool",
            PWBShortcut.Group.GLOBAL, new PWBKeyCombinationUSM( Shortcuts.PWB_TOGGLE_SELECTION_SHORTCUT_ID ) );

        public PWBKeyShortcut toolbarExtrudeToggle { get; } = new PWBKeyShortcut( "Toggle Extrude Tool",
            PWBShortcut.Group.GLOBAL, new PWBKeyCombinationUSM( Shortcuts.PWB_TOGGLE_EXTRUDE_SHORTCUT_ID ) );

        public PWBKeyShortcut toolbarMirrorToggle { get; } = new PWBKeyShortcut( "Toggle Mirror Tool",
            PWBShortcut.Group.GLOBAL, new PWBKeyCombinationUSM( Shortcuts.PWB_TOGGLE_MIRROR_SHORTCUT_ID ) );

        #endregion

        #region PALETTE

        [SerializeField]
        private PWBKeyShortcut _paletteDeleteBrush = new PWBKeyShortcut( "Delete selected brushes",
            PWBShortcut.Group.PALETTE              | PWBShortcut.Group.GLOBAL,
            KeyCode.Delete, EventModifiers.Control | EventModifiers.Shift );

        [SerializeField]
        private PWBKeyShortcut _palettePreviousBrush = new PWBKeyShortcut( "Select previous brush",
            PWBShortcut.Group.PALETTE         | PWBShortcut.Group.GLOBAL,
            KeyCode.Z, EventModifiers.Control | EventModifiers.Shift );

        [SerializeField]
        private PWBKeyShortcut _paletteNextBrush = new PWBKeyShortcut( "Select next brush",
            PWBShortcut.Group.PALETTE         | PWBShortcut.Group.GLOBAL,
            KeyCode.X, EventModifiers.Control | EventModifiers.Shift );

        [SerializeField]
        private PWBKeyShortcut _palettePreviousPalette = new PWBKeyShortcut( "Select previous palette",
            PWBShortcut.Group.PALETTE         | PWBShortcut.Group.GLOBAL,
            KeyCode.Z, EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift );

        [SerializeField]
        private PWBKeyShortcut _paletteNextPalette = new PWBKeyShortcut( "Select next palette",
            PWBShortcut.Group.PALETTE         | PWBShortcut.Group.GLOBAL,
            KeyCode.X, EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift );

        [SerializeField]
        private PWBHoldKeysAndClickShortcut _palettePickBrush = new PWBHoldKeysAndClickShortcut( "Pick or add a brush",
            PWBShortcut.Group.PALETTE | PWBShortcut.Group.GLOBAL,
            KeyCode.Alpha1, EventModifiers.Shift );

        public PWBKeyShortcut              paletteDeleteBrush     => _paletteDeleteBrush;
        public PWBKeyShortcut              palettePreviousBrush   => _palettePreviousBrush;
        public PWBKeyShortcut              paletteNextBrush       => _paletteNextBrush;
        public PWBKeyShortcut              palettePreviousPalette => _palettePreviousPalette;
        public PWBKeyShortcut              paletteNextPalette     => _paletteNextPalette;
        public PWBHoldKeysAndClickShortcut palettePickBrush       => _palettePickBrush;

        #endregion

        #region CONFLICTS

        private PWBKeyShortcut[] _keyShortcuts;

        public PWBKeyShortcut[] keyShortcuts
        {
            get
            {
                if ( _keyShortcuts == null )
                {
                    _keyShortcuts = new[]
                    {
                        /*/// GRID ///*/
                        _gridEnableShortcuts,
                        _gridToggle,
                        _gridToggleSnapping,
                        _gridToggleLock,
                        _gridSetOriginPosition,
                        _gridSetOriginRotation,
                        _gridSetSize,
                        _gridFrameOrigin,
                        _gridTogglePositionHandle,
                        _gridToggleRotationHandle,
                        _gridToggleSpacingHandle,
                        _gridMoveOriginUp,
                        _gridMoveOriginDown,
                        /*/// PIN ///*/
                        _pinMoveHandlesUp,
                        _pinMoveHandlesDown,
                        _pinSelectNextHandle,
                        _pinSelectPivotHandle,
                        _pinToggleRepeatItem,
                        _pinResetScale,

                        _pinRotate90YCW,
                        _pinRotate90YCCW,
                        _pinRotateAStepYCW,
                        _pinRotateAStepYCCW,

                        _pinRotate90XCW,
                        _pinRotate90XCCW,
                        _pinRotateAStepXCW,
                        _pinRotateAStepXCCW,

                        _pinRotate90ZCW,
                        _pinRotate90ZCCW,
                        _pinRotateAStepZCW,
                        _pinRotateAStepZCCW,

                        _pinResetRotation,

                        _pinAdd1UnitToSurfDist,
                        _pinSubtract1UnitFromSurfDist,
                        _pinAdd01UnitToSurfDist,
                        _pinSubtract01UnitFromSurfDist,

                        _pinResetSurfDist,

                        _pinSelectPreviousItem,
                        _pinSelectNextItem,
                        /*/// BRUSH & GRAVITY ///*/
                        _brushUpdatebrushstroke,
                        _brushResetRotation,
                        /*/// GRAVITY ///*/
                        _gravityAdd1UnitToSurfDist,
                        _gravitySubtract1UnitFromSurfDist,
                        _gravityAdd01UnitToSurfDist,
                        _gravitySubtract01UnitFromSurfDist,
                        /*/// EDIT MODE ///*/
                        _editModeDeleteItemAndItsChildren,
                        _editModeDeleteItemButNotItsChildren,
                        _editModeSelectParent,
                        editModeToggle,
                        /*/// LINE ///*/
                        _lineSelectAllPoints,
                        _lineDeselectAllPoints,
                        _lineToggleCurve,
                        _lineToggleClosed,
                        _lineEditModeTypeToggle,
                        /*/// TILING & SELECTION ///*/
                        _selectionRotate90XCW,
                        _selectionRotate90XCCW,
                        _selectionRotate90YCW,
                        _selectionRotate90YCCW,
                        _selectionRotate90ZCW,
                        _selectionRotate90ZCCW,
                        /*/// SELECTION ///*/
                        _selectionTogglePositionHandle,
                        _selectionToggleRotationHandle,
                        _selectionToggleScaleHandle,
                        _selectionEditCustomHandle,
                        /*/// PALETTE ///*/
                        _paletteDeleteBrush,
                        _palettePreviousBrush,
                        _paletteNextBrush,
                        _palettePreviousPalette,
                        _paletteNextPalette,
                        _palettePickBrush,
                        /*/// TOOLBAR ///*/
                        toolbarPinToggle,
                        toolbarBrushToggle,
                        toolbarGravityToggle,
                        toolbarLineToggle,
                        toolbarShapeToggle,
                        toolbarTilingToggle,
                        toolbarReplacerToggle,
                        toolbarEraserToggle,
                        toolbarSelectionToggle,
                        toolbarExtrudeToggle,
                        toolbarMirrorToggle,
                    };
                }

                return _keyShortcuts;
            }
        }

        public void UpdateConficts()
        {
            foreach ( PWBKeyShortcut shortcut in keyShortcuts )
            {
                shortcut.conflicted = false;
            }

            for ( int i = 0; i < keyShortcuts.Length; ++i )
            {
                PWBKeyShortcut shortcut1 = keyShortcuts[ i ];
                if ( shortcut1.conflicted )
                {
                    continue;
                }

                if ( shortcut1.combination.keyCode == KeyCode.None )
                {
                    continue;
                }

                for ( int j = i + 1; j < keyShortcuts.Length; ++j )
                {
                    PWBKeyShortcut shortcut2 = keyShortcuts[ j ];
                    if ( shortcut2.conflicted )
                    {
                        continue;
                    }

                    if ( shortcut2.combination.keyCode == KeyCode.None )
                    {
                        continue;
                    }

                    if ( ( shortcut1.group    & shortcut2.group )          == 0
                         && ( shortcut1.group & PWBShortcut.Group.GLOBAL ) == 0
                         && ( shortcut1.group & PWBShortcut.Group.GLOBAL ) == 0 )
                    {
                        continue;
                    }

                    if ( shortcut1                                       == gridEnableShortcuts
                         && ( shortcut2.group & PWBShortcut.Group.GRID ) != 0 )
                    {
                        continue;
                    }

                    if ( shortcut1.combination == shortcut2.combination )
                    {
                        shortcut1.conflicted = true;
                        shortcut2.conflicted = true;
                    }
                }
            }
        }

        public bool CheckConflicts( PWBKeyCombination combi, PWBKeyShortcut target, out string conflicts )
        {
            conflicts = string.Empty;
            foreach ( PWBKeyShortcut shortcut in keyShortcuts )
            {
                if ( target == shortcut )
                {
                    continue;
                }

                if ( target.combination.keyCode      == KeyCode.None
                     || shortcut.combination.keyCode == KeyCode.None )
                {
                    continue;
                }

                if ( combi == shortcut.combination
                     && ( ( target.group      & shortcut.group )           != 0
                          || ( shortcut.group & PWBShortcut.Group.GLOBAL ) != 0
                          || ( target.group   & PWBShortcut.Group.GLOBAL ) != 0 ) )
                {
                    if ( shortcut                                     == gridEnableShortcuts
                         && ( target.group & PWBShortcut.Group.GRID ) != 0 )
                    {
                        continue;
                    }

                    if ( target                                         == gridEnableShortcuts
                         && ( shortcut.group & PWBShortcut.Group.GRID ) != 0 )
                    {
                        continue;
                    }

                    if ( conflicts != string.Empty )
                    {
                        conflicts += "\n";
                    }

                    conflicts += shortcut.name;
                }
            }

            return conflicts != string.Empty;
        }

        #endregion

        #endregion

        #region MOUSE COMBINATIONS

        #region PIN

        [SerializeField]
        private PWBMouseShortcut _pinScale = new PWBMouseShortcut( "Edit Scale",
            PWBShortcut.Group.PIN, EventModifiers.Control, PWBMouseCombination.MouseEvents.SCROLL_WHEEL );

        [SerializeField]
        private PWBMouseShortcut _pinSelectNextItemScroll
            = new PWBMouseShortcut( "Select previous/next item in the multi-brush",
                PWBShortcut.Group.PIN, EventModifiers.Control | EventModifiers.Alt,
                PWBMouseCombination.MouseEvents.SCROLL_WHEEL );

        [SerializeField]
        private PWBMouseShortcut _pinRotateAroundY = new PWBMouseShortcut( "Rotate freely around local Y axis",
            PWBShortcut.Group.PIN, EventModifiers.Control, PWBMouseCombination.MouseEvents.DRAG_R_H );

        [SerializeField]
        private PWBMouseShortcut _pinRotateAroundYSnaped
            = new PWBMouseShortcut( "Rotate freely around the local Y axis in steps",
                PWBShortcut.Group.PIN, EventModifiers.Control | EventModifiers.Alt, PWBMouseCombination.MouseEvents.DRAG_R_H );

        [SerializeField]
        private PWBMouseShortcut _pinRotateAroundX = new PWBMouseShortcut( "Rotate freely around local X axis",
            PWBShortcut.Group.PIN, EventModifiers.Control, PWBMouseCombination.MouseEvents.DRAG_M_V );

        [SerializeField]
        private PWBMouseShortcut _pinRotateAroundXSnaped
            = new PWBMouseShortcut( "Rotate freely around the local X axis in steps",
                PWBShortcut.Group.PIN, EventModifiers.Control | EventModifiers.Alt, PWBMouseCombination.MouseEvents.DRAG_M_V );

        [SerializeField]
        private PWBMouseShortcut _pinRotateAroundZ = new PWBMouseShortcut( "Rotate freely around local Z axis",
            PWBShortcut.Group.PIN, EventModifiers.Control | EventModifiers.Shift, PWBMouseCombination.MouseEvents.DRAG_M_V );

        [SerializeField]
        private PWBMouseShortcut _pinRotateAroundZSnaped
            = new PWBMouseShortcut( "Rotate freely around the local Z axis in steps", PWBShortcut.Group.PIN,
                EventModifiers.Control | EventModifiers.Alt | EventModifiers.Shift, PWBMouseCombination.MouseEvents.DRAG_M_V );

        [SerializeField]
        private PWBMouseShortcut _pinSurfDist = new PWBMouseShortcut( "Edit distance to the surface",
            PWBShortcut.Group.PIN,
            EventModifiers.Control | EventModifiers.Shift, PWBMouseCombination.MouseEvents.DRAG_R_V );

        public PWBMouseShortcut pinScale                => _pinScale;
        public PWBMouseShortcut pinSelectNextItemScroll => _pinSelectNextItemScroll;

        public PWBMouseShortcut pinRotateAroundY       => _pinRotateAroundY;
        public PWBMouseShortcut pinRotateAroundYSnaped => _pinRotateAroundYSnaped;
        public PWBMouseShortcut pinRotateAroundX       => _pinRotateAroundX;
        public PWBMouseShortcut pinRotateAroundXSnaped => _pinRotateAroundXSnaped;
        public PWBMouseShortcut pinRotateAroundZ       => _pinRotateAroundZ;
        public PWBMouseShortcut pinRotateAroundZSnaped => _pinRotateAroundZSnaped;

        public PWBMouseShortcut pinSurfDist => _pinSurfDist;

        #endregion

        #region RADIUS

        [SerializeField]
        private PWBMouseShortcut _brushRadius = new PWBMouseShortcut( "Change radius",
            PWBShortcut.Group.BRUSH | PWBShortcut.Group.GRAVITY | PWBShortcut.Group.ERASER | PWBShortcut.Group.REPLACER,
            EventModifiers.Control, PWBMouseCombination.MouseEvents.SCROLL_WHEEL );

        public PWBMouseShortcut brushRadius => _brushRadius;

        #endregion

        #region BRUSH & GRAVITY

        [SerializeField]
        private PWBMouseShortcut _brushDensity = new PWBMouseShortcut( "Edit density",
            PWBShortcut.Group.BRUSH | PWBShortcut.Group.GRAVITY,
            EventModifiers.Control  | EventModifiers.Alt, PWBMouseCombination.MouseEvents.SCROLL_WHEEL );

        [SerializeField]
        private PWBMouseShortcut _brushRotate = new PWBMouseShortcut( "Rotate brush",
            PWBShortcut.Group.BRUSH | PWBShortcut.Group.GRAVITY,
            EventModifiers.Control, PWBMouseCombination.MouseEvents.DRAG_R_H );

        public PWBMouseShortcut brushDensity => _brushDensity;
        public PWBMouseShortcut brushRotate  => _brushRotate;

        #endregion

        #region GRAVITY

        [SerializeField]
        private PWBMouseShortcut _gravitySurfDist
            = new PWBMouseShortcut( "Edit distance to the surface", PWBShortcut.Group.GRAVITY,
                EventModifiers.Control | EventModifiers.Shift, PWBMouseCombination.MouseEvents.DRAG_R_V );

        public PWBMouseShortcut gravitySurfDist => _gravitySurfDist;

        #endregion

        #region LINE & SHAPE

        [SerializeField]
        private PWBMouseShortcut _lineEditGap
            = new PWBMouseShortcut( "Edit gap size", PWBShortcut.Group.LINE | PWBShortcut.Group.SHAPE,
                EventModifiers.Control                                      | EventModifiers.Shift, PWBMouseCombination.MouseEvents.DRAG_R_H );

        public PWBMouseShortcut lineEditGap => _lineEditGap;

        #endregion

        #region TILING

        [SerializeField]
        private PWBMouseShortcut _tilingEditSpacing1 = new PWBMouseShortcut( "Edit spacing on axis 1", PWBShortcut.Group.TILING,
            EventModifiers.Control, PWBMouseCombination.MouseEvents.DRAG_R_H );

        [SerializeField]
        private PWBMouseShortcut _tilingEditSpacing2 = new PWBMouseShortcut( "Edit spacing on axis 2", PWBShortcut.Group.TILING,
            EventModifiers.Control | EventModifiers.Shift, PWBMouseCombination.MouseEvents.DRAG_R_H );

        public PWBMouseShortcut tilingEditSpacing1 => _tilingEditSpacing1;
        public PWBMouseShortcut tilingEditSpacing2 => _tilingEditSpacing2;

        #endregion

        #region PALETTE

        [SerializeField]
        private PWBMouseShortcut _paletteNextBrushScroll = new PWBMouseShortcut( "Select previous/next brush",
            PWBShortcut.Group.PALETTE | PWBShortcut.Group.GLOBAL,
            EventModifiers.Control    | EventModifiers.Shift, PWBMouseCombination.MouseEvents.SCROLL_WHEEL );

        [SerializeField]
        private PWBMouseShortcut _paletteNextPaletteScroll = new PWBMouseShortcut( "Select previous/next palette",
            PWBShortcut.Group.PALETTE | PWBShortcut.Group.GLOBAL,
            EventModifiers.Control    | EventModifiers.Alt | EventModifiers.Shift, PWBMouseCombination.MouseEvents.SCROLL_WHEEL );

        public PWBMouseShortcut paletteNextBrushScroll   => _paletteNextBrushScroll;
        public PWBMouseShortcut paletteNextPaletteScroll => _paletteNextPaletteScroll;

        #endregion

        #region CONFLICTS

        private PWBMouseShortcut[] _mouseShortcuts;

        public PWBMouseShortcut[] mouseShortcuts
        {
            get
            {
                if ( _mouseShortcuts == null )
                {
                    _mouseShortcuts = new[]
                    {
                        /*/// PIN ///*/
                        _pinScale,
                        _pinSelectNextItemScroll,

                        _pinRotateAroundY,
                        _pinRotateAroundYSnaped,
                        _pinRotateAroundX,
                        _pinRotateAroundXSnaped,
                        _pinRotateAroundZ,
                        _pinRotateAroundZSnaped,

                        _pinSurfDist,
                        /*/// RADIUS ///*/
                        _brushRadius,
                        /*/// BRUSH & GRAVITY ///*/
                        _brushDensity,
                        _brushRotate,
                        /*/// BRUSH & GRAVITY ///*/
                        _gravitySurfDist,
                        /*/// LINE & SHAPE ///*/
                        _lineEditGap,
                        /*/// LINE ///*/

                        /*/// TILING ///*/
                        tilingEditSpacing1,
                        tilingEditSpacing2,
                        /*/// PALETTE ///*/
                        _paletteNextBrushScroll,
                        _paletteNextPaletteScroll,

                    };
                }

                return _mouseShortcuts;
            }
        }

        public void UpdateMouseConficts()
        {
            foreach ( PWBMouseShortcut scrollShortcut in mouseShortcuts )
            {
                scrollShortcut.conflicted = false;
            }

            for ( int i = 0; i < mouseShortcuts.Length; ++i )
            {
                PWBMouseShortcut shortcut1 = mouseShortcuts[ i ];
                if ( shortcut1.conflicted )
                {
                    continue;
                }

                if ( shortcut1.combination.modifiers == EventModifiers.None )
                {
                    continue;
                }

                for ( int j = i + 1; j < mouseShortcuts.Length; ++j )
                {
                    PWBMouseShortcut shortcut2 = mouseShortcuts[ j ];
                    if ( shortcut2.conflicted )
                    {
                        continue;
                    }

                    if ( shortcut2.combination.modifiers == EventModifiers.None )
                    {
                        continue;
                    }

                    if ( ( shortcut1.group    & shortcut2.group )          == 0
                         && ( shortcut1.group & PWBShortcut.Group.GLOBAL ) == 0
                         && ( shortcut1.group & PWBShortcut.Group.GLOBAL ) == 0 )
                    {
                        continue;
                    }

                    if ( shortcut1.combination == shortcut2.combination )
                    {
                        shortcut1.conflicted = true;
                        shortcut2.conflicted = true;
                    }
                }
            }
        }

        public bool CheckMouseConflicts( PWBMouseCombination combi, PWBMouseShortcut target, out string conflicts )
        {
            conflicts = string.Empty;
            foreach ( PWBMouseShortcut shortcut in mouseShortcuts )
            {
                if ( target == shortcut )
                {
                    continue;
                }

                if ( target.combination.modifiers      == EventModifiers.None
                     || shortcut.combination.modifiers == EventModifiers.None )
                {
                    continue;
                }

                if ( combi == shortcut.combination
                     && ( ( target.group      & shortcut.group )           != 0
                          || ( shortcut.group & PWBShortcut.Group.GLOBAL ) != 0
                          || ( target.group   & PWBShortcut.Group.GLOBAL ) != 0 ) )
                {
                    if ( conflicts != string.Empty )
                    {
                        conflicts += "\n";
                    }

                    conflicts += shortcut.name;
                }
            }

            return conflicts != string.Empty;
        }

        public bool CombinationExist( PWBMouseCombination combi, PWBShortcut.Group group )
        {
            foreach ( PWBMouseShortcut shortcut in mouseShortcuts )
            {
                if ( combi == shortcut.combination
                     && ( ( group             & shortcut.group )           != 0
                          || ( shortcut.group & PWBShortcut.Group.GLOBAL ) != 0
                          || ( group          & PWBShortcut.Group.GLOBAL ) != 0 ) )
                {
                    return true;
                }
            }

            return false;
        }

        #endregion

        #endregion

    }

    #endregion

    #region SETTINGS

    [Serializable]
    public class PWBSettings
    {

        #region COMMON

        private static string      _settingsPath;
        private static PWBSettings _instance;

        private PWBSettings()
        {
        }

        private static PWBSettings instance
        {
            get
            {
                if ( _instance == null )
                {
                    _instance = new PWBSettings();
                }

                return _instance;
            }
        }

        private static string settingsPath
        {
            get
            {
                if ( _settingsPath == null )
                {
                    _settingsPath = Directory.GetParent( Application.dataPath ) + "/ProjectSettings/PWBSettings.txt";
                }

                return _settingsPath;
            }
        }

        private void LoadFromFile()
        {
            if ( !File.Exists( settingsPath ) )
            {
                string[] files = Directory.GetFiles( Application.dataPath,
                    PWBData.FULL_FILE_NAME, SearchOption.AllDirectories );
                if ( files.Length > 0 )
                {
                    _dataDir = Path.GetDirectoryName( files[ 0 ] );
                }
                else
                {
                    _dataDir = Application.dataPath + "/" + PWBData.RELATIVE_DATA_DIR;
                    Directory.CreateDirectory( _dataDir );
                    _dataDir = PWBCore.GetRelativePath( _dataDir );
                }

                Save();
            }
            else
            {
                PWBSettings settings = JsonUtility.FromJson<PWBSettings>( File.ReadAllText( settingsPath ) );
                _dataDir = settings._dataDir;
                if ( PWBCore.IsFullPath( _dataDir ) )
                {
                    _dataDir = PWBCore.GetRelativePath( _dataDir );
                }

                _shortcutProfiles   = settings._shortcutProfiles;
                _selectedProfileIdx = settings._selectedProfileIdx;
            }
        }

        private void Save()
        {
            string jsonString = JsonUtility.ToJson( this, true );
            File.WriteAllText( settingsPath, jsonString );
        }

        #endregion

        #region DATA DIR

        [SerializeField] private string _dataDir;
        public static            bool   movingDir { get; private set; }

        private static void CheckDataDir()
        {
            if ( instance._dataDir == null )
            {
                instance.LoadFromFile();
            }

            if ( PWBCore.IsFullPath( instance._dataDir ) )
            {
                instance._dataDir = PWBCore.GetRelativePath( instance._dataDir );
            }

        }

        public static string relativeDataDir
        {
            get
            {
                CheckDataDir();
                string currentDir = PWBCore.GetFullPath( instance._dataDir );
                if ( !Directory.Exists( currentDir ) )
                {
                    if ( currentDir.Replace( "\\", "/" ).Contains( PWBData.RELATIVE_DATA_DIR ) )
                    {
                        string[] directories = Directory.GetDirectories( Application.dataPath, PWBData.DATA_DIR,
                                                            SearchOption.AllDirectories )
                                                        .Where( d => d.Replace( "\\", "/" ).Contains( PWBData.RELATIVE_DATA_DIR ) ).ToArray();
                        if ( directories.Length > 0 )
                        {
                            instance._dataDir = PWBCore.GetRelativePath( directories[ 0 ].Replace( "\\", "/" ) );
                            instance.Save();
                            PaletteManager.instance.LoadPaletteFiles( false );
                            PrefabPalette.UpdateTabBar();
                        }
                    }
                }

                return instance._dataDir;
            }
        }

        public static void SetDataDir( string fullPath )
        {
            string newDirRelative = PWBCore.GetRelativePath( fullPath );
            if ( instance._dataDir == newDirRelative )
            {
                return;
            }

            string currentFullDir = PWBCore.GetFullPath( instance._dataDir );

            void DeleteMeta( string path )
            {
                string metapath = path + ".meta";
                if ( File.Exists( metapath ) )
                {
                    File.Delete( metapath );
                }
            }

            bool DeleteIfEmpty( string dirPath )
            {
                if ( Directory.GetFiles( dirPath ).Length != 0 )
                {
                    return false;
                }

                Directory.Delete( dirPath );
                DeleteMeta( dirPath );
                return true;
            }

            if ( Directory.Exists( currentFullDir ) )
            {
                movingDir = true;
                string currentDataPath = currentFullDir + "/" + PWBData.FULL_FILE_NAME;
                if ( File.Exists( currentDataPath ) )
                {
                    string newDataPath = fullPath + "/" + PWBData.FULL_FILE_NAME;
                    if ( File.Exists( newDataPath ) )
                    {
                        File.Delete( newDataPath );
                    }

                    DeleteMeta( currentDataPath );
                    File.Move( currentDataPath, newDataPath );

                    string currentPalettesDir = currentFullDir + "/" + PWBData.PALETTES_DIR;
                    if ( Directory.Exists( currentPalettesDir ) )
                    {
                        string newPalettesDir = fullPath + "/" + PWBData.PALETTES_DIR;
                        if ( !Directory.Exists( newPalettesDir ) )
                        {
                            Directory.CreateDirectory( newPalettesDir );
                        }

                        string[] palettesPaths = Directory.GetFiles( currentPalettesDir, "*.txt" );
                        foreach ( string currentPalettePath in palettesPaths )
                        {
                            string fileName       = Path.GetFileName( currentPalettePath );
                            string newPalettePath = newPalettesDir + "/" + fileName;
                            if ( File.Exists( newPalettePath ) )
                            {
                                File.Delete( newPalettePath );
                            }

                            DeleteMeta( currentPalettePath );

                            string      paletteText = File.ReadAllText( currentPalettePath );
                            PaletteData palette     = JsonUtility.FromJson<PaletteData>( paletteText );
                            palette.filePath = newPalettePath;

                            File.Move( currentPalettePath, newPalettePath );
                            File.Delete( currentPalettePath );

                            string currentThumbnailsPath = currentPalettePath.Substring( 0, currentPalettePath.Length - 4 );
                            if ( !Directory.Exists( currentThumbnailsPath ) )
                            {
                                continue;
                            }

                            string thumbnailsDirName = fileName.Substring( 0, fileName.Length - 4 );
                            string newThumbnailPath  = newPalettesDir + "/" + thumbnailsDirName;
                            if ( Directory.Exists( newThumbnailPath ) )
                            {
                                Directory.Delete( newThumbnailPath );
                            }

                            DeleteMeta( currentThumbnailsPath );
                            Directory.Move( currentThumbnailsPath, newThumbnailPath );
                        }
                    }

                    if ( DeleteIfEmpty( currentPalettesDir ) )
                    {
                        DeleteIfEmpty( currentFullDir );
                    }

                    PWBCore.AssetDatabaseRefresh();
                }

                movingDir = false;
            }

            instance._dataDir = PWBCore.GetRelativePath( fullPath );
            instance.Save();
            PaletteManager.instance.LoadPaletteFiles( true );
            PrefabPalette.UpdateTabBar();
        }

        public static string fullDataDir => PWBCore.GetFullPath( relativeDataDir );

        #endregion

        #region SHORTCUTS

        [SerializeField]
        private List<PWBShortcuts> _shortcutProfiles
            = new List<PWBShortcuts>
            {
                PWBShortcuts.GetDefault( 0 ),
                PWBShortcuts.GetDefault( 1 ),
            };

        [SerializeField] private int _selectedProfileIdx;

        private PWBShortcuts selectedProfile
        {
            get
            {
                if ( _selectedProfileIdx    < 0
                     || _selectedProfileIdx > _shortcutProfiles.Count )
                {
                    _selectedProfileIdx = 0;
                }

                return _shortcutProfiles[ _selectedProfileIdx ];
            }
        }

        public static PWBShortcuts shortcuts
        {
            get
            {
                CheckDataDir();
                return instance.selectedProfile;
            }
        }

        public static string[] shotcutProfileNames
        {
            get
            {
                CheckDataDir();
                return instance._shortcutProfiles.Select( p => p.profileName ).ToArray();
            }
        }

        public static int selectedProfileIdx
        {
            get
            {
                CheckDataDir();
                return instance._selectedProfileIdx;
            }
            set
            {
                CheckDataDir();
                instance._selectedProfileIdx = value;
            }
        }

        public static void UpdateShrotcutsConflictsAndSaveFile()
        {
            CheckDataDir();
            shortcuts.UpdateConficts();
            shortcuts.UpdateMouseConficts();
            instance.Save();
        }

        public static void SetDefaultShortcut( int shortcutIdx, int defaultIdx )
        {
            CheckDataDir();
            if ( shortcutIdx    < 0
                 || shortcutIdx > instance._shortcutProfiles.Count )
            {
                return;
            }

            instance._shortcutProfiles[ shortcutIdx ] = PWBShortcuts.GetDefault( defaultIdx );
        }

        public static void ResetSelectedProfile()
        {
            CheckDataDir();
            if ( selectedProfileIdx == 1 )
            {
                instance._shortcutProfiles[ 1 ] = PWBShortcuts.GetDefault( 1 );
            }
            else
            {
                instance._shortcutProfiles[ instance._selectedProfileIdx ] = PWBShortcuts.GetDefault( 0 );
            }
        }

        public static void ResetShortcutToDefault( PWBKeyShortcut shortcut )
        {
            PWBShortcuts defaultProfile = selectedProfileIdx == 1 ? PWBShortcuts.GetDefault( 1 ) : PWBShortcuts.GetDefault( 0 );
            foreach ( PWBKeyShortcut ds in defaultProfile.keyShortcuts )
            {
                if ( ds.group   == shortcut.group
                     && ds.name == shortcut.name )
                {
                    shortcut.combination.Set( ds.combination.keyCode, ds.combination.modifiers );
                    return;
                }
            }
        }

        public static void ResetShortcutToDefault( PWBMouseShortcut shortcut )
        {
            PWBShortcuts defaultProfile = selectedProfileIdx == 1 ? PWBShortcuts.GetDefault( 1 ) : PWBShortcuts.GetDefault( 0 );
            foreach ( PWBMouseShortcut ds in defaultProfile.mouseShortcuts )
            {
                if ( ds.group   == shortcut.group
                     && ds.name == shortcut.name )
                {
                    shortcut.combination.Set( ds.combination.modifiers, ds.combination.mouseEvent );
                    return;
                }
            }
        }

        public static void ResetShortcutToDefault( PWBShortcut shortcut )
        {
            if ( shortcut is PWBKeyShortcut )
            {
                ResetShortcutToDefault( shortcut as PWBKeyShortcut );
            }
            else if ( shortcut is PWBMouseShortcut )
            {
                ResetShortcutToDefault( shortcut as PWBMouseShortcut );
            }
        }

        #endregion

    }

    #endregion

    #region HANDLERS

    [InitializeOnLoad]
    public static class ApplicationEventHandler
    {

        #region Statics and Constants

        private static bool _refreshOnImportingCancelled;

        private static bool _hierarchyLoaded;

        #endregion

        #region Public Properties

        public static bool hierarchyChangedWhileUsingTools { get; set; }

        public static bool importingPackage { get; private set; }

        public static bool sceneOpening { get; private set; }

        #endregion

        #region Public Methods

        public static bool RefreshOnImportingCancelled() => _refreshOnImportingCancelled = true;

        #endregion

        #region Private Constructors

        static ApplicationEventHandler()
        {
            EditorApplication.playModeStateChanged += OnStateChanged;
            EditorApplication.hierarchyChanged     += OnHierarchyChanged;
            AssetDatabase.importPackageStarted     += OnImportPackageStarted;
            AssetDatabase.importPackageCompleted   += OnImportPackageCompleted;
            AssetDatabase.importPackageCancelled   += OnImportPackageCancelled;
            AssetDatabase.importPackageFailed      += OnImportPackageFailed;
            EditorSceneManager.sceneOpening        += OnSceneOpening;
            EditorSceneManager.sceneOpened         += OnSceneOpened;
            PaletteManager.instance.LoadPaletteFiles( true );
        }

        #endregion

        #region Private Methods

        private static void OnHierarchyChanged()
        {
            if ( !_hierarchyLoaded )
            {
                _hierarchyLoaded = true;
                if ( !PWBCore.staticData.saving )
                {
                    PWBCore.LoadFromFile();
                }

                return;
            }

            if ( PWBCore.updatingTempColliders
                 || PWBIO.painting )
            {
                if ( PWBCore.updatingTempColliders )
                {
                    PWBCore.updatingTempColliders = false;
                }

                if ( PWBIO.painting )
                {
                    PWBIO.painting = false;
                }

                return;
            }

            if ( ToolManager.tool != ToolManager.PaintTool.NONE )
            {
                hierarchyChangedWhileUsingTools = true;
            }
        }

        private static void OnImportPackageCancelled( string packageName )
        {
            if ( _refreshOnImportingCancelled )
            {
                AssetDatabase.Refresh();
                _refreshOnImportingCancelled = false;
            }

            importingPackage = false;
        }

        private static void OnImportPackageCompleted( string packageName )                      => importingPackage = false;
        private static void OnImportPackageFailed( string    packageName, string errorMessage ) => importingPackage = false;

        private static void OnImportPackageStarted( string packageName ) => importingPackage = true;

        private static void OnSceneOpened( Scene         scene,
                                           OpenSceneMode mode )
            => sceneOpening = false;

        private static void OnSceneOpening( string path, OpenSceneMode mode )
            => sceneOpening = true;

        private static void OnStateChanged( PlayModeStateChange state )
        {
            if ( state    == PlayModeStateChange.ExitingEditMode
                 || state == PlayModeStateChange.ExitingPlayMode )
            {
                PWBCore.staticData.SaveIfPending();
            }
        }

        #endregion

    }

    public class DataReimportHandler : AssetPostprocessor
    {

        #region Statics and Constants

        #endregion

        #region Public Properties

        public static bool importingAssets { get; private set; }

        #endregion

        #region Unity Functions

        private static void OnPostprocessAllAssets( string[] importedAssets, string[] deletedAssets,
                                                    string[] movedAssets,    string[] movedFromAssetPaths )
        {
            importingAssets = false;
            if ( PWBSettings.movingDir )
            {
                return;
            }

            if ( PWBCore.staticData.saving )
            {
                return;
            }

            if ( !PWBData.palettesDirectory.Contains( Application.dataPath ) )
            {
                return;
            }

            if ( PaletteManager.addingPalettes )
            {
                PaletteManager.addingPalettes = false;
                return;
            }

            List<string> paths = new List<string>( importedAssets );
            paths.AddRange( deletedAssets );
            paths.AddRange( movedAssets );
            paths.AddRange( movedFromAssetPaths );

            string relativeDataPath = PWBSettings.relativeDataDir.Replace( Application.dataPath, string.Empty );

            if ( paths.Exists( p => p.Contains( relativeDataPath ) && Path.GetExtension( p ) == ".txt" ) )
            {
                if ( PaletteManager.selectedPalette != null
                     && PaletteManager.selectedPalette.saving )
                {
                    PaletteManager.selectedPalette.StopSaving();
                    return;
                }

                PaletteManager.instance.LoadPaletteFiles( false );
                if ( PrefabPalette.instance != null )
                {
                    PrefabPalette.instance.Reload( !ThumbnailUtils.savingImage );
                }
            }
        }

        private void OnPreprocessAsset() => importingAssets = true;

        #endregion

    }

    #endregion

    #region AUTOSAVE

    [InitializeOnLoad]
    public static class AutoSave
    {

        #region Statics and Constants

        private static int _quickSaveCount = 3;

        #endregion

        #region Public Methods

        public static void QuickSave() => _quickSaveCount = 0;

        #endregion

        #region Private Constructors

        static AutoSave()
        {
            PWBCore.staticData.UpdateRootDirectory();
            PeriodicSave();
            PeriodicQuickSave();
        }

        #endregion

        #region Private Methods

        private static async void PeriodicQuickSave()
        {
            await Task.Delay( 300 );
            ++_quickSaveCount;
            if ( _quickSaveCount == 3
                 && PWBCore.staticDataWasInitialized )
            {
                PWBCore.staticData.SaveAndUpdateVersion();
            }

            PeriodicQuickSave();
        }

        private static async void PeriodicSave()
        {
            if ( PWBCore.staticDataWasInitialized )
            {
                await Task.Delay( PWBCore.staticData.autoSavePeriodMinutes * 60000 );
                PWBCore.staticData.SaveIfPending();
            }
            else
            {
                await Task.Delay( 60000 );
            }

            PeriodicSave();
        }

        #endregion

    }

    #endregion

    #region VERSION

    [Serializable]
    public class PWBDataVersion
    {

        #region Serialized

        [SerializeField] public string _version;

        #endregion

        #region Public Methods

        public bool IsOlderThan( string value ) => IsOlderThan( value, _version );

        public static bool IsOlderThan( string value, string referenceValue )
        {
            int[] intArray      = GetIntArray( referenceValue );
            int[] otherIntArray = GetIntArray( value );
            int   minLength     = Mathf.Min( intArray.Length, otherIntArray.Length );
            for ( int i = 0; i < minLength; ++i )
            {
                if ( intArray[ i ] < otherIntArray[ i ] )
                {
                    return true;
                }

                if ( intArray[ i ] > otherIntArray[ i ] )
                {
                    return false;
                }
            }

            return false;
        }

        #endregion

        #region Private Methods

        private static int[] GetIntArray( string value )
        {
            string[] stringArray = value.Split( '.' );
            if ( stringArray.Length == 0 )
            {
                return new[] { 1, 0 };
            }

            int[] intArray = new int[ stringArray.Length ];
            for ( int i = 0; i < intArray.Length; ++i )
            {
                intArray[ i ] = int.Parse( stringArray[ i ] );
            }

            return intArray;
        }

        #endregion

    }

    #endregion

    #region DATA 1.9

    [Serializable]
    public class V1_9_LineData
    {

        #region Serialized

        [SerializeField] public LinePoint[] _controlPoints;
        [SerializeField] public bool        _closed;

        #endregion

    }

    [Serializable]
    public class V1_9_PersistentLineData
    {

        #region Serialized

        [SerializeField] public long          _id;
        [SerializeField] public long          _initialBrushId;
        [SerializeField] public V1_9_LineData _data;
        [SerializeField] public LineSettings  _settings;
        [SerializeField] public ObjectPose[]  _objectPoses;

        #endregion

    }

    [Serializable]
    public class V1_9_SceneLines
    {

        #region Serialized

        [SerializeField] public string                    _sceneGUID;
        [SerializeField] public V1_9_PersistentLineData[] _lines;

        #endregion

    }

    [Serializable]
    public class V1_9_Profile
    {

        #region Serialized

        [SerializeField] public V1_9_SceneLines[] _sceneLines;

        #endregion

    }

    [Serializable]
    public class V1_9_LineManager
    {

        #region Serialized

        [SerializeField] public V1_9_Profile _unsavedProfile;

        #endregion

    }

    [Serializable]
    public class V1_9_PWBData
    {

        #region Serialized

        [SerializeField] public V1_9_LineManager _lineManager;

        #endregion

    }

    #endregion

    #region DATA 2.8

    [Serializable]
    public class V2_8_PaletteManager
    {

        #region Serialized

        [SerializeField] public PaletteData[] _paletteData;

        #endregion

    }

    [Serializable]
    public class V2_8_PWBData
    {

        #region Serialized

        [SerializeField] public V2_8_PaletteManager _paletteManager;

        #endregion

    }

    #endregion

}