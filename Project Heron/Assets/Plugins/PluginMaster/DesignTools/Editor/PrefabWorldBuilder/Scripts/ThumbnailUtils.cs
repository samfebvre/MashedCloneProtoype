using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace PluginMaster
{
    [InitializeOnLoad]
    public class HDRPDefine
    {

        #region Private Constructors

        static HDRPDefine()
        {
            RenderPipelineAsset currentRenderPipeline = GraphicsSettings.currentRenderPipeline;
            if ( currentRenderPipeline == null )
            {
                return;
            }

            if ( !currentRenderPipeline.GetType().ToString().Contains( "HighDefinition" ) )
            {
                return;
            }

            BuildTarget      target           = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup buildTargetGroup = BuildPipeline.GetBuildTargetGroup( target );
            #if UNITY_2022_2_OR_NEWER
            NamedBuildTarget namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup( buildTargetGroup );
            string           definesSCSV      = PlayerSettings.GetScriptingDefineSymbols( namedBuildTarget );
            #else
            var definesSCSV = UnityEditor.PlayerSettings.GetScriptingDefineSymbolsForGroup(buildTargetGroup);
            #endif
            const string PWB_HDRP = "PWB_HDRP";
            if ( definesSCSV.Contains( PWB_HDRP ) )
            {
                return;
            }

            definesSCSV += ";" + PWB_HDRP;
            #if UNITY_2022_2_OR_NEWER
            PlayerSettings.SetScriptingDefineSymbols( namedBuildTarget, definesSCSV );
            #else
            UnityEditor.PlayerSettings.SetScriptingDefineSymbolsForGroup(buildTargetGroup, definesSCSV);
            #endif
        }

        #endregion

    }

    public class ThumbnailUtils
    {

        #region Statics and Constants

        public const   int       SIZE     = 256;
        private const  int       MIN_SIZE = 24;
        private static Texture2D _emptyTexture;

        #endregion

        #region Public Properties

        public static bool savingImage { get; private set; }

        #endregion

        #region Public Methods

        public static void DeleteUnusedThumbnails()
        {
            PaletteData[] palettes = PaletteManager.paletteData;

            bool GetBrushIdAndItemIdFromThumbnailPath( string thumbnailPath, out long brushId, out long itemId )
            {
                string   fileName = Path.GetFileNameWithoutExtension( thumbnailPath );
                string[] ids      = fileName.Split( '_' );
                brushId = long.Parse( ids[ 0 ], NumberStyles.HexNumber );
                itemId  = -1;
                MultibrushSettings brush = PaletteManager.GetBrushById( brushId );
                if ( brush == null )
                {
                    return false;
                }

                if ( ids.Length == 1 )
                {
                    return true;
                }

                itemId = long.Parse( ids[ 1 ], NumberStyles.HexNumber );
                return brush.ItemExist( itemId );
            }

            string[] folderPaths = PaletteManager.GetPaletteThumbnailFolderPaths();
            foreach ( string folderPath in folderPaths )
            {
                string[] thumbnailPaths = Directory.GetFiles( folderPath, "*.png" );
                foreach ( string thumbnailPath in thumbnailPaths )
                {
                    if ( !GetBrushIdAndItemIdFromThumbnailPath( thumbnailPath, out long pathBrushId, out long pathItemId ) )
                    {
                        File.Delete( thumbnailPath );
                        string metapath = thumbnailPath + ".meta";
                        if ( File.Exists( metapath ) )
                        {
                            File.Delete( metapath );
                        }

                        PWBCore.refreshDatabase = true;
                    }
                }
            }
        }

        public static void RenderTextureToTexture2D( RenderTexture renderTexture, Texture2D texture )
        {
            RenderTexture prevActive = RenderTexture.active;
            RenderTexture.active = renderTexture;
            texture.ReadPixels( new Rect( 0, 0, SIZE, SIZE ), 0, 0 );
            texture.Apply();
            RenderTexture.active = prevActive;
        }

        public static void UpdateThumbnail( ThumbnailSettings settings,
                                            Texture2D         thumbnailTexture, GameObject prefab, string thumbnailPath, bool savePng )
        {
            float           magnitude       = BoundsUtils.GetMagnitude( prefab.transform );
            ThumbnailEditor thumbnailEditor = new ThumbnailEditor();
            thumbnailEditor.settings = new ThumbnailSettings( settings );

            if ( magnitude == 0 )
            {
                if ( _emptyTexture == null )
                {
                    _emptyTexture = Resources.Load<Texture2D>( "Sprites/Empty" );
                }

                Color32[] pixels = _emptyTexture.GetPixels32();
                for ( int i = 0; i < pixels.Length; ++i )
                {
                    if ( pixels[ i ].a == 0 )
                    {
                        pixels[ i ] = thumbnailEditor.settings.backgroudColor;
                    }
                }

                thumbnailTexture.SetPixels32( pixels );
                thumbnailTexture.Apply();
                return;
            }
            #if UNITY_2022_2_OR_NEWER
            Dictionary<Light, int> sceneLights = Object.FindObjectsByType<Light>( FindObjectsSortMode.None )
                                                       .ToDictionary( comp => comp, light => light.cullingMask );
            #else
            var sceneLights = Object.FindObjectsOfType<Light>().ToDictionary(comp => comp, light => light.cullingMask);
            #endif

            const string rootName = "PWBThumbnailEditor";

            do
            {
                GameObject obj = GameObject.Find( rootName );
                if ( obj == null )
                {
                    break;
                }

                Object.DestroyImmediate( obj );
            }
            while ( true );

            thumbnailEditor.root = new GameObject( rootName );

            GameObject camObj = new GameObject( "PWBThumbnailEditorCam" );
            thumbnailEditor.camera = camObj.AddComponent<Camera>();
            thumbnailEditor.camera.transform.SetParent( thumbnailEditor.root.transform );
            thumbnailEditor.camera.transform.localPosition = new Vector3( 0f, 1.2f, -4f );
            thumbnailEditor.camera.transform.localRotation = Quaternion.Euler( 17.5f, 0f, 0f );
            thumbnailEditor.camera.fieldOfView             = 20f;
            thumbnailEditor.camera.clearFlags              = CameraClearFlags.SolidColor;
            thumbnailEditor.camera.backgroundColor         = thumbnailEditor.settings.backgroudColor;
            thumbnailEditor.camera.cullingMask             = layerMask;
            thumbnailEditor.renderTexture                  = new RenderTexture( SIZE, SIZE, 24 );
            thumbnailEditor.camera.targetTexture           = thumbnailEditor.renderTexture;

            GameObject lightObj = new GameObject( "PWBThumbnailEditorLight" );
            thumbnailEditor.light      = lightObj.AddComponent<Light>();
            thumbnailEditor.light.type = LightType.Directional;
            thumbnailEditor.light.transform.SetParent( thumbnailEditor.root.transform );
            thumbnailEditor.light.transform.localRotation = Quaternion.Euler( thumbnailEditor.settings.lightEuler );
            thumbnailEditor.light.color                   = thumbnailEditor.settings.lightColor;
            thumbnailEditor.light.intensity               = thumbnailEditor.settings.lightIntensity;
            thumbnailEditor.light.cullingMask             = layerMask;

            GameObject pivotObj = new GameObject( "PWBThumbnailEditorPivot" );
            pivotObj.layer        = PWBCore.staticData.thumbnailLayer;
            thumbnailEditor.pivot = pivotObj.transform;
            thumbnailEditor.pivot.transform.SetParent( thumbnailEditor.root.transform );
            thumbnailEditor.pivot.localPosition           = thumbnailEditor.settings.targetOffset;
            thumbnailEditor.pivot.transform.localRotation = Quaternion.identity;
            thumbnailEditor.pivot.transform.localScale    = Vector3.one;

            Transform InstantiateBones( Transform source, Transform parent )
            {
                GameObject obj = new GameObject();
                obj.name = source.name;
                obj.transform.SetParent( parent );
                obj.transform.position   = source.position;
                obj.transform.rotation   = source.rotation;
                obj.transform.localScale = source.localScale;
                foreach ( Transform child in source )
                {
                    InstantiateBones( child, obj.transform );
                }

                return obj.transform;
            }

            bool Requires( Type obj, Type requirement )
            {
                return Attribute.IsDefined( obj, typeof(RequireComponent) )
                       && Attribute.GetCustomAttributes( obj, typeof(RequireComponent) ).OfType<RequireComponent>()
                                   .Any( rc => rc.m_Type0.IsAssignableFrom( requirement ) );
            }

            bool CanDestroy( GameObject go, Type t )
            {
                return !go.GetComponents<Component>().Any( c => Requires( c.GetType(), t ) );
            }

            void CopyComponents( GameObject source, GameObject destination )
            {
                Component[] srcComps = source.GetComponentsInChildren<Component>();
                foreach ( Component srcComp in srcComps )
                {
                    if ( srcComp is MonoBehaviour )
                    {
                        continue;
                    }

                    Component destComp = srcComp is Transform ? destination.transform : destination.AddComponent( srcComp.GetType() );
                    EditorUtility.CopySerialized( srcComp, destComp );
                }

                foreach ( Transform srcChild in source.transform )
                {
                    GameObject destChild = new GameObject();
                    destChild.transform.SetParent( destination.transform );
                    CopyComponents( srcChild.gameObject, destChild );
                }
            }

            GameObject InstantiateAndRemoveMonoBehaviours()
            {
                GameObject      obj           = Object.Instantiate( prefab );
                List<Component> toBeDestroyed = new List<Component>( obj.GetComponentsInChildren<Component>() );

                while ( toBeDestroyed.Count > 0 )
                {
                    Component[] components = toBeDestroyed.ToArray();
                    int         compCount  = components.Length;
                    toBeDestroyed.Clear();
                    foreach ( Component comp in components )
                    {
                        if ( comp is MonoBehaviour )
                        {
                            MonoBehaviour monoBehaviour = comp as MonoBehaviour;
                            monoBehaviour.enabled       = false;
                            monoBehaviour.runInEditMode = false;
                            if ( CanDestroy( obj, comp.GetType() ) )
                            {
                                Object.DestroyImmediate( comp );
                            }
                            else
                            {
                                toBeDestroyed.Add( comp );
                            }
                        }
                    }

                    if ( compCount == toBeDestroyed.Count )
                    {
                        break;
                    }
                }

                if ( toBeDestroyed.Count > 0 )
                {
                    GameObject noMonoBehaviourObj = new GameObject();
                    CopyComponents( noMonoBehaviourObj, obj );
                    Object.DestroyImmediate( obj );
                    obj = noMonoBehaviourObj;
                }

                return obj;
            }

            thumbnailEditor.target = InstantiateAndRemoveMonoBehaviours();

            MonoBehaviour[] monoBehaviours = thumbnailEditor.target.GetComponentsInChildren<MonoBehaviour>();
            foreach ( MonoBehaviour monoBehaviour in monoBehaviours )
            {
                if ( monoBehaviour != null )
                {
                    monoBehaviour.enabled = false;
                }
            }

            magnitude = BoundsUtils.GetMagnitude( thumbnailEditor.target.transform );
            float   targetScale   = magnitude > 0 ? 1f / magnitude : 1f;
            Bounds  targetBounds  = BoundsUtils.GetBoundsRecursive( thumbnailEditor.target.transform );
            Vector3 localPosition = ( thumbnailEditor.target.transform.localPosition - targetBounds.center ) * targetScale;
            thumbnailEditor.target.transform.SetParent( thumbnailEditor.pivot );
            thumbnailEditor.target.transform.localPosition = localPosition;
            thumbnailEditor.target.transform.localRotation = Quaternion.identity;
            thumbnailEditor.target.transform.localScale    = prefab.transform.localScale * targetScale;
            thumbnailEditor.pivot.localScale               = Vector3.one                 * thumbnailEditor.settings.zoom;
            thumbnailEditor.pivot.localRotation            = Quaternion.Euler( thumbnailEditor.settings.targetEuler );

            #if PWB_HDRP
            var HDCamData = camObj.AddComponent<UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData>();
            HDCamData.volumeLayerMask = layerMask | 1;
            HDCamData.probeLayerMask = 0;
            HDCamData.clearColorMode = UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData.ClearColorMode.Color;
            HDCamData.backgroundColorHDR = thumbnailEditor.settings.backgroudColor;
            HDCamData.antialiasing
                = UnityEngine.Rendering.HighDefinition.HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;

            thumbnailEditor.light.intensity *= 100;
            #endif

            Transform[] children = thumbnailEditor.root.GetComponentsInChildren<Transform>();
            foreach ( Transform child in children )
            {
                child.gameObject.layer     = PWBCore.staticData.thumbnailLayer;
                child.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }

            foreach ( Light light in sceneLights.Keys )
            {
                light.cullingMask = light.cullingMask & ~layerMask;
            }

            thumbnailEditor.camera.Render();
            foreach ( Light light in sceneLights.Keys )
            {
                light.cullingMask = sceneLights[ light ];
            }

            RenderTextureToTexture2D( thumbnailEditor.camera.targetTexture, thumbnailTexture );

            Object.DestroyImmediate( thumbnailEditor.root );
            if ( savePng )
            {
                SavePngResource( thumbnailTexture, thumbnailPath );
            }
        }

        public static void UpdateThumbnail( ThumbnailSettings settings,
                                            Texture2D         thumbnailTexture, Texture2D[] subThumbnails, string thumbnailPath, bool savePng )
        {
            if ( subThumbnails.Length == 0 )
            {
                thumbnailTexture.SetPixels( new Color[ SIZE * SIZE ] );
                thumbnailTexture.Apply();
                return;
            }

            float sqrt           = Mathf.Sqrt( subThumbnails.Length );
            int   sideCellsCount = Mathf.FloorToInt( sqrt );
            if ( Mathf.CeilToInt( sqrt ) != sideCellsCount )
            {
                ++sideCellsCount;
            }

            int       spacing    = SIZE * sideCellsCount / MIN_SIZE;
            int       bigSize    = SIZE * sideCellsCount + spacing * ( sideCellsCount - 1 );
            Texture2D texture    = new Texture2D( bigSize, bigSize );
            int       pixelCount = bigSize * bigSize;
            Color32[] pixels     = new Color32[ pixelCount ];
            texture.SetPixels32( pixels );
            int subIdx = 0;
            for ( int i = sideCellsCount - 1; i >= 0; --i )
            {
                for ( int j = 0; j < sideCellsCount; ++j )
                {
                    int x = j * ( SIZE + spacing );
                    int y = i * ( SIZE + spacing );
                    if ( subThumbnails[ subIdx ] == null )
                    {
                        continue;
                    }

                    Color32[] subPixels = subThumbnails[ subIdx ].GetPixels32();
                    texture.SetPixels32( x, y, SIZE, SIZE, subPixels );
                    ++subIdx;
                    if ( subIdx == subThumbnails.Length )
                    {
                        goto Resize;
                    }
                }
            }

            Resize:
            texture.filterMode = FilterMode.Trilinear;
            texture.Apply();
            RenderTexture renderTexture = new RenderTexture( SIZE, SIZE, 24 );
            RenderTexture prevActive    = RenderTexture.active;
            RenderTexture.active = renderTexture;
            Graphics.Blit( texture, renderTexture );
            thumbnailTexture.ReadPixels( new Rect( 0, 0, SIZE, SIZE ), 0, 0 );
            thumbnailTexture.Apply();
            RenderTexture.active = prevActive;
            Object.DestroyImmediate( texture );
            if ( savePng )
            {
                SavePngResource( thumbnailTexture, thumbnailPath );
            }
        }

        public static void UpdateThumbnail( MultibrushItemSettings brushItem, bool savePng )
        {
            if ( brushItem.prefab == null )
            {
                return;
            }

            UpdateThumbnail( brushItem.thumbnailSettings, brushItem.thumbnailTexture,
                brushItem.prefab,                         brushItem.thumbnailPath, savePng );
        }

        public static void UpdateThumbnail( MultibrushSettings brushSettings, bool updateItemThumbnails, bool savePng )
        {
            MultibrushItemSettings[] brushItems    = brushSettings.items;
            List<Texture2D>          subThumbnails = new List<Texture2D>();
            foreach ( MultibrushItemSettings item in brushItems )
            {
                if ( updateItemThumbnails )
                {
                    UpdateThumbnail( item, savePng );
                }

                if ( item.includeInThumbnail )
                {
                    subThumbnails.Add( item.thumbnail );
                }
            }

            UpdateThumbnail( brushSettings.thumbnailSettings, brushSettings.thumbnailTexture,
                subThumbnails.ToArray(),                      brushSettings.thumbnailPath, savePng );
        }

        public static void UpdateThumbnail( BrushSettings brushItem, bool updateItemThumbnails, bool savePng )
        {
            if ( brushItem is MultibrushItemSettings )
            {
                UpdateThumbnail( brushItem as MultibrushItemSettings, savePng );
            }
            else if ( brushItem is MultibrushSettings )
            {
                UpdateThumbnail( brushItem as MultibrushSettings, updateItemThumbnails, savePng );
            }
        }

        #endregion

        #region Private Properties

        private static Texture2D emptyTexture
        {
            get
            {
                if ( _emptyTexture == null )
                {
                    _emptyTexture = Resources.Load<Texture2D>( "Sprites/Empty" );
                }

                return _emptyTexture;
            }
        }

        private static LayerMask layerMask => 1 << PWBCore.staticData.thumbnailLayer;

        #endregion

        #region Private Methods

        private static void SavePngResource( Texture2D texture, string thumbnailPath )
        {
            if ( texture == null
                 || string.IsNullOrEmpty( thumbnailPath ) )
            {
                return;
            }

            savingImage = true;
            byte[] buffer = texture.EncodeToPNG();
            File.WriteAllBytes( thumbnailPath, buffer );
            AssetDatabase.Refresh();
            savingImage = false;
        }

        #endregion

        private class ThumbnailEditor
        {

            #region Public Fields

            public Camera            camera;
            public Light             light;
            public Transform         pivot;
            public RenderTexture     renderTexture;
            public GameObject        root;
            public ThumbnailSettings settings;
            public GameObject        target;

            #endregion

        }
    }
}