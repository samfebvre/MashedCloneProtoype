using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

namespace PluginMaster
{

    #region BRUSH SETTINGS

    [Serializable]
    public class BrushSettings : ISerializationCallbackReceiver
    {

        #region Statics and Constants

        private static long _prevId;

        #endregion

        #region Serialized

        [SerializeField] private   long              _id = -1;
        [SerializeField] private   float             _surfaceDistance;
        [SerializeField] private   bool              _randomSurfaceDistance;
        [SerializeField] private   RandomUtils.Range _randomSurfaceDistanceRange = new RandomUtils.Range( -0.005f, 0.005f );
        [SerializeField] protected bool              _embedInSurface;
        [SerializeField] protected bool              _embedAtPivotHeight  = true;
        [SerializeField] protected Vector3           _localPositionOffset = Vector3.zero;
        [SerializeField] private   bool              _rotateToTheSurface  = true;
        [SerializeField] private   Vector3           _eulerOffset         = Vector3.zero;
        [SerializeField] private   bool              _addRandomRotation;
        [SerializeField] private   float             _rotationFactor = 90;
        [SerializeField] private   bool              _rotateInMultiples;

        [SerializeField]
        private RandomUtils.Range3 _randomEulerOffset = new RandomUtils.Range3( Vector3.zero, Vector3.zero );

        [SerializeField] private bool    _alwaysOrientUp;
        [SerializeField] private bool    _separateScaleAxes;
        [SerializeField] private Vector3 _scaleMultiplier = Vector3.one;
        [SerializeField] private bool    _randomScaleMultiplier;

        [SerializeField]
        private RandomUtils.Range3 _randomScaleMultiplierRange = new RandomUtils.Range3( Vector3.one, Vector3.one );

        [SerializeField] private FlipAction _flipX = FlipAction.NONE;
        [SerializeField] private FlipAction _flipY = FlipAction.NONE;

        [SerializeField] private ThumbnailSettings _thumbnailSettings = new ThumbnailSettings();

        #endregion

        #region Public Enums

        public enum FlipAction
        {
            NONE,
            FLIP,
            RANDOM,
        }

        #endregion

        #region Public Fields

        public Action OnDataChangedAction;

        #endregion

        #region Public Properties

        public virtual bool addRandomRotation
        {
            get => _addRandomRotation;
            set
            {
                if ( _addRandomRotation == value )
                {
                    return;
                }

                _addRandomRotation = value;
                OnDataChanged();
            }
        }

        public virtual bool alwaysOrientUp
        {
            get => _alwaysOrientUp;
            set
            {
                if ( _alwaysOrientUp == value )
                {
                    return;
                }

                _alwaysOrientUp = value;
                OnDataChanged();
            }
        }

        public virtual bool embedAtPivotHeight
        {
            get => _embedAtPivotHeight;
            set
            {
                if ( _embedAtPivotHeight == value )
                {
                    return;
                }

                _embedAtPivotHeight = value;
                OnDataChanged();
            }
        }

        public virtual bool embedInSurface
        {
            get => _embedInSurface;
            set
            {
                if ( _embedInSurface == value )
                {
                    return;
                }

                _embedInSurface = value;
                OnDataChanged();
            }
        }

        public virtual Vector3 eulerOffset
        {
            get => _eulerOffset;
            set
            {
                if ( _eulerOffset == value )
                {
                    return;
                }

                _eulerOffset          = value;
                _randomEulerOffset.v1 = _randomEulerOffset.v2 = Vector3.zero;
                OnDataChanged();
            }
        }

        public virtual FlipAction flipX
        {
            get => _flipX;
            set
            {
                if ( _flipX == value )
                {
                    return;
                }

                _flipX = value;
                OnDataChanged();
            }
        }

        public virtual FlipAction flipY
        {
            get => _flipY;
            set
            {
                if ( _flipY == value )
                {
                    return;
                }

                _flipY = value;
                OnDataChanged();
            }
        }

        public long id => _id;

        public virtual bool isAsset2D { get; set; }

        public virtual Vector3 localPositionOffset
        {
            get => _localPositionOffset;
            set
            {
                if ( _localPositionOffset == value )
                {
                    return;
                }

                _localPositionOffset = value;
                OnDataChanged();
            }
        }

        public virtual RandomUtils.Range3 randomEulerOffset
        {
            get => _randomEulerOffset;
            set
            {
                if ( _randomEulerOffset == value )
                {
                    return;
                }

                _randomEulerOffset = value;
                _eulerOffset       = Vector3.zero;
                OnDataChanged();
            }
        }

        public virtual bool randomScaleMultiplier
        {
            get => _randomScaleMultiplier;
            set
            {
                if ( _randomScaleMultiplier == value )
                {
                    return;
                }

                _randomScaleMultiplier         = value;
                _randomScaleMultiplierRange.v1 = _randomScaleMultiplierRange.v2 = _scaleMultiplier = Vector3.one;
                OnDataChanged();
            }
        }

        public virtual RandomUtils.Range3 randomScaleMultiplierRange
        {
            get => _randomScaleMultiplierRange;
            set
            {
                if ( _randomScaleMultiplierRange == value )
                {
                    return;
                }

                _randomScaleMultiplierRange = value;
                _scaleMultiplier            = Vector3.one;
                OnDataChanged();
            }
        }

        public virtual bool randomSurfaceDistance
        {
            get => _randomSurfaceDistance;
            set
            {
                if ( _randomSurfaceDistance == value )
                {
                    return;
                }

                _randomSurfaceDistance = value;
                OnDataChanged();
            }
        }

        public virtual RandomUtils.Range randomSurfaceDistanceRange
        {
            get => _randomSurfaceDistanceRange;
            set
            {
                if ( _randomSurfaceDistanceRange == value )
                {
                    return;
                }

                _randomSurfaceDistanceRange = value;
                OnDataChanged();
            }
        }

        public virtual bool rotateInMultiples
        {
            get => _rotateInMultiples;
            set
            {
                if ( _rotateInMultiples == value )
                {
                    return;
                }

                _rotateInMultiples = value;
                OnDataChanged();
            }
        }

        public virtual bool rotateToTheSurface
        {
            get => _rotateToTheSurface;
            set
            {
                if ( _rotateToTheSurface == value )
                {
                    return;
                }

                _rotateToTheSurface = value;
                OnDataChanged();
            }
        }

        public virtual float rotationFactor
        {
            get => _rotationFactor;
            set
            {
                value = Mathf.Max( value, 0f );
                if ( _rotationFactor == value )
                {
                    return;
                }

                _rotationFactor = value;
                OnDataChanged();
            }
        }

        public virtual Vector3 scaleMultiplier
        {
            get => _scaleMultiplier;
            set
            {
                if ( _scaleMultiplier == value )
                {
                    return;
                }

                _scaleMultiplier               = value;
                _randomScaleMultiplierRange.v1 = _randomScaleMultiplierRange.v2 = Vector3.one;
                OnDataChanged();
            }
        }

        public virtual bool separateScaleAxes
        {
            get => _separateScaleAxes;
            set
            {
                if ( _separateScaleAxes == value )
                {
                    return;
                }

                _separateScaleAxes = value;
                OnDataChanged();
            }
        }

        public virtual float surfaceDistance
        {
            get => _surfaceDistance;
            set
            {
                if ( _surfaceDistance == value )
                {
                    return;
                }

                _surfaceDistance = value;
                OnDataChanged();
            }
        }

        public Texture2D thumbnail
        {
            get
            {
                if ( _thumbnail == null )
                {
                    string filePath = thumbnailPath;
                    if ( filePath != null )
                    {
                        if ( File.Exists( filePath ) )
                        {
                            byte[] fileData = File.ReadAllBytes( filePath );
                            _thumbnail = new Texture2D( ThumbnailUtils.SIZE, ThumbnailUtils.SIZE );
                            _thumbnail.LoadImage( fileData );
                        }
                        else
                        {
                            UpdateThumbnail( updateItemThumbnails: true, savePng: true );
                        }
                    }
                }

                if ( _thumbnail == null )
                {
                    UpdateThumbnail( updateItemThumbnails: true, savePng: true );
                }

                return _thumbnail;
            }
        }

        public virtual string thumbnailPath { get; }

        public virtual ThumbnailSettings thumbnailSettings
        {
            get => _thumbnailSettings;
            set => _thumbnailSettings.Copy( value );
        }

        public Texture2D thumbnailTexture
        {
            get
            {
                if ( _thumbnail == null )
                {
                    _thumbnail = new Texture2D( ThumbnailUtils.SIZE, ThumbnailUtils.SIZE );
                }

                return _thumbnail;
            }
        }

        #endregion

        #region Public Constructors

        public BrushSettings()
        {
        }

        public BrushSettings( BrushSettings other )
        {
            Copy( other );
        }

        #endregion

        #region Public Methods

        public virtual BrushSettings Clone()
        {
            BrushSettings clone = new BrushSettings();
            clone.Copy( this );
            clone._thumbnail = _thumbnail;
            return clone;
        }

        public virtual void Copy( BrushSettings other )
        {
            _surfaceDistance            = other._surfaceDistance;
            _randomSurfaceDistance      = other._randomSurfaceDistance;
            _randomSurfaceDistanceRange = other._randomSurfaceDistanceRange;
            _embedInSurface             = other._embedInSurface;
            _embedAtPivotHeight         = other._embedAtPivotHeight;
            _localPositionOffset        = other._localPositionOffset;
            _rotateToTheSurface         = other._rotateToTheSurface;
            _addRandomRotation          = other._addRandomRotation;
            _eulerOffset                = other._eulerOffset;
            _randomEulerOffset          = new RandomUtils.Range3( other._randomEulerOffset );
            _randomScaleMultiplier      = other._randomScaleMultiplier;
            _alwaysOrientUp             = other._alwaysOrientUp;
            _separateScaleAxes          = other._separateScaleAxes;
            _scaleMultiplier            = other._scaleMultiplier;
            _randomScaleMultiplierRange = new RandomUtils.Range3( other._randomScaleMultiplierRange );
            _thumbnailSettings.Copy( other._thumbnailSettings );
            _rotationFactor    = other._rotationFactor;
            _rotateInMultiples = other._rotateInMultiples;
            _flipX             = other._flipX;
            _flipY             = other._flipY;
        }

        public virtual void OnAfterDeserialize()
        {
        }

        public virtual void OnBeforeSerialize()
        {
        }

        public void Reset()
        {
            _surfaceDistance            = 0f;
            _randomSurfaceDistance      = false;
            _randomSurfaceDistanceRange = new RandomUtils.Range( -0.005f, 0.005f );
            _embedInSurface             = false;
            _embedAtPivotHeight         = true;
            _localPositionOffset        = Vector3.zero;
            _rotateToTheSurface         = true;
            _addRandomRotation          = false;
            _eulerOffset                = Vector3.zero;
            _randomEulerOffset          = new RandomUtils.Range3( Vector3.zero, Vector3.zero );
            _randomScaleMultiplier      = false;
            _alwaysOrientUp             = false;
            _separateScaleAxes          = false;
            _scaleMultiplier            = Vector3.one;
            _randomScaleMultiplierRange = new RandomUtils.Range3( Vector3.one, Vector3.one );
            _thumbnailSettings          = new ThumbnailSettings();
            _rotationFactor             = 90;
            _rotateInMultiples          = false;
            _flipX                      = FlipAction.NONE;
            _flipY                      = FlipAction.NONE;
        }

        public virtual void UpdateBottomVertices()
        {
        }

        public void UpdateThumbnail( bool updateItemThumbnails, bool savePng )
            => ThumbnailUtils.UpdateThumbnail( brushItem: this, updateItemThumbnails, savePng );

        #endregion

        #region Protected Methods

        protected void SetId()
        {
            _id = DateTime.Now.Ticks;
            if ( _id <= _prevId )
            {
                _id = _prevId + 1;
            }

            _prevId = _id;
        }

        #endregion

        #region Private Fields

        [field: NonSerialized] private Texture2D _thumbnail;

        #endregion

        #region Private Methods

        private void OnDataChanged()
        {
            if ( OnDataChangedAction != null )
            {
                OnDataChangedAction();
            }
        }

        #endregion

    }

    public static class SelectionUtils
    {

        #region Public Methods

        public static void Swap<T>( int fromIdx, int toIdx, ref int[] selection, List<T> list )
        {
            if ( fromIdx == toIdx )
            {
                return;
            }

            List<T> newOrder     = new List<T>();
            int[]   newSelection = selection.ToArray();
            for ( int idx = 0; idx <= list.Count; ++idx )
            {
                if ( idx == toIdx )
                {
                    Array.Sort( selection );
                    int newSelectionIdx = 0;
                    foreach ( int selectionIdx in selection )
                    {
                        newOrder.Add( list[ selectionIdx ] );
                        newSelection[ newSelectionIdx++ ] = newOrder.Count - 1;
                    }

                    if ( idx < list.Count
                         && !selection.Contains( idx ) )
                    {
                        newOrder.Add( list[ idx ] );
                    }
                }
                else if ( selection.Contains( idx ) )
                {
                }
                else if ( idx < list.Count )
                {
                    newOrder.Add( list[ idx ] );
                }
            }

            selection = newSelection;
            list.Clear();
            list.AddRange( newOrder );
            PWBCore.staticData.Save();
        }

        #endregion

    }

    [Serializable]
    public class ThumbnailSettings
    {

        #region Serialized

        [SerializeField] private Color   _backgroudColor = Color.gray;
        [SerializeField] private Vector2 _lightEuler     = new Vector2( 130, -165 );
        [SerializeField] private Color   _lightColor     = Color.white;
        [SerializeField] private float   _lightIntensity = 1;
        [SerializeField] private float   _zoom           = 1;
        [SerializeField] private Vector3 _targetEuler    = new Vector3( 0, 125, 0 );
        [SerializeField] private Vector2 _targetOffset   = Vector2.zero;

        #endregion

        #region Public Properties

        public Color backgroudColor
        {
            get => _backgroudColor;
            set => _backgroudColor = value;
        }

        public Color lightColor
        {
            get => _lightColor;
            set => _lightColor = value;
        }

        public Vector2 lightEuler
        {
            get => _lightEuler;
            set => _lightEuler = value;
        }

        public float lightIntensity
        {
            get => _lightIntensity;
            set => _lightIntensity = value;
        }

        public Vector3 targetEuler
        {
            get => _targetEuler;
            set => _targetEuler = value;
        }

        public Vector2 targetOffset
        {
            get => _targetOffset;
            set => _targetOffset = value;
        }

        public float zoom
        {
            get => _zoom;
            set => _zoom = value;
        }

        #endregion

        #region Public Constructors

        public ThumbnailSettings()
        {
        }

        public ThumbnailSettings( Color backgroudColor, Vector3 lightEuler,  Color   lightColor, float lightIntensity,
                                  float zoom,           Vector3 targetEuler, Vector2 targetOffset )
        {
            _backgroudColor = backgroudColor;
            _lightEuler     = lightEuler;
            _lightColor     = lightColor;
            _lightIntensity = lightIntensity;
            _zoom           = zoom;
            _targetEuler    = targetEuler;
            _targetOffset   = targetOffset;
        }

        public ThumbnailSettings( ThumbnailSettings other )
        {
            Copy( other );
        }

        #endregion

        #region Public Methods

        public ThumbnailSettings Clone()
        {
            ThumbnailSettings clone = new ThumbnailSettings();
            clone.Copy( this );
            return clone;
        }

        public void Copy( ThumbnailSettings other )
        {
            _backgroudColor = other._backgroudColor;
            _lightEuler     = other._lightEuler;
            _lightColor     = other._lightColor;
            _lightIntensity = other._lightIntensity;
            _zoom           = other._zoom;
            _targetEuler    = other._targetEuler;
            _targetOffset   = other._targetOffset;
        }

        #endregion

    }

    [Serializable]
    public class MultibrushItemSettings : BrushSettings
    {

        #region Serialized

        [SerializeField] private bool   _overwriteSettings;
        [SerializeField] private string _guid       = string.Empty;
        [SerializeField] private string _prefabPath = string.Empty;
        [SerializeField] private float  _frequency  = 1;
        [SerializeField] private long   _parentId   = -1;
        [SerializeField] private bool   _overwriteThumbnailSettings;
        [SerializeField] private bool   _includeInThumbnail = true;
        [SerializeField] private bool   _isAsset2D;

        #endregion

        #region Public Properties

        public override bool addRandomRotation
            => _overwriteSettings || parentSettings == null ? base.addRandomRotation : parentSettings.addRandomRotation;

        public override bool alwaysOrientUp
            => _overwriteSettings || parentSettings == null ? base.alwaysOrientUp : parentSettings.alwaysOrientUp;

        public float bottomMagnitude
        {
            get
            {
                if ( prefab == null )
                {
                    return 0f;
                }

                if ( _bottomMagnitude == 0 )
                {
                    _bottomMagnitude = BoundsUtils.GetBottomMagnitude( prefab.transform );
                }

                return _bottomMagnitude;
            }
        }

        public Vector3[] bottomVertices
        {
            get
            {
                if ( _bottomVertices == null )
                {
                    UpdateBottomVertices();
                }

                return _bottomVertices;
            }
        }

        public override bool embedAtPivotHeight
        {
            get => _overwriteSettings || parentSettings == null ? base.embedAtPivotHeight : parentSettings.embedAtPivotHeight;
            set
            {
                if ( _embedAtPivotHeight == value )
                {
                    return;
                }

                _embedAtPivotHeight = value;
            }
        }

        public override bool embedInSurface
        {
            get => _overwriteSettings || parentSettings == null
                ? base.embedInSurface
                : parentSettings.embedInSurface;
            set
            {
                if ( _embedInSurface == value )
                {
                    return;
                }

                _embedInSurface = value;
                if ( _embedInSurface )
                {
                    UpdateBottomVertices();
                }
            }
        }

        public override Vector3 eulerOffset
            => _overwriteSettings || parentSettings == null ? base.eulerOffset : parentSettings.eulerOffset;

        public override FlipAction flipX
            => _overwriteSettings || parentSettings == null ? base.flipX : parentSettings.flipX;

        public override FlipAction flipY
            => _overwriteSettings || parentSettings == null ? base.flipY : parentSettings.flipY;

        public float frequency
        {
            get => _frequency;
            set
            {
                value = Mathf.Max( value, 0 );
                if ( _frequency == value )
                {
                    return;
                }

                _frequency = value;
                if ( parentSettings != null )
                {
                    parentSettings.UpdateTotalFrequency();
                }
            }
        }

        public float height { get; private set; } = 1f;

        public bool includeInThumbnail
        {
            get => _includeInThumbnail;
            set
            {
                if ( _includeInThumbnail == value )
                {
                    return;
                }

                _includeInThumbnail = value;
            }
        }

        public override bool isAsset2D
        {
            get => _isAsset2D;
            set => _isAsset2D = value;
        }

        public override Vector3 localPositionOffset
            => _overwriteSettings || parentSettings == null ? base.localPositionOffset : parentSettings.localPositionOffset;

        public Vector3 maxScaleMultiplier
            => randomScaleMultiplier ? randomScaleMultiplierRange.max : scaleMultiplier;

        public Vector3 minScaleMultiplier
            => randomScaleMultiplier ? randomScaleMultiplierRange.min : scaleMultiplier;

        public bool overwriteSettings
        {
            get => _overwriteSettings;
            set
            {
                if ( _overwriteSettings == value )
                {
                    return;
                }

                _overwriteSettings = value;
                SavePalette();
            }
        }

        public virtual bool overwriteThumbnailSettings
        {
            get => _overwriteThumbnailSettings;
            set
            {
                if ( _overwriteThumbnailSettings == value )
                {
                    return;
                }

                _overwriteThumbnailSettings = value;
            }
        }

        public MultibrushSettings parentSettings
        {
            get
            {
                if ( _parentSettings == null )
                {
                    _parentSettings = PaletteManager.GetBrushById( _parentId );
                }

                return _parentSettings;
            }
            set
            {
                if ( value == null )
                {
                    _parentId       = -1;
                    _parentSettings = null;
                    return;
                }

                _parentSettings = value;
                _parentId       = value.id;
            }
        }

        public GameObject prefab
        {
            get
            {
                if ( _prefab == null )
                {
                    _prefab = AssetDatabase.LoadAssetAtPath<GameObject>
                        ( AssetDatabase.GUIDToAssetPath( _guid ) );
                }

                if ( _prefab == null )
                {
                    _prefab = AssetDatabase.LoadAssetAtPath<GameObject>( _prefabPath );
                    if ( _prefab != null )
                    {
                        AssetDatabase.TryGetGUIDAndLocalFileIdentifier( _prefab, out _guid, out long localId );
                    }
                }
                else
                {
                    _prefabPath = AssetDatabase.GetAssetPath( _prefab );
                }

                return _prefab;
            }
        }

        public string prefabPath => _prefabPath;

        public override RandomUtils.Range3 randomEulerOffset
            => _overwriteSettings || parentSettings == null ? base.randomEulerOffset : parentSettings.randomEulerOffset;

        public override bool randomScaleMultiplier
            => _overwriteSettings || parentSettings == null
                ? base.randomScaleMultiplier
                : parentSettings.randomScaleMultiplier;

        public override RandomUtils.Range3 randomScaleMultiplierRange
            => _overwriteSettings || parentSettings == null
                ? base.randomScaleMultiplierRange
                : parentSettings.randomScaleMultiplierRange;

        public override bool randomSurfaceDistance
            => _overwriteSettings || parentSettings == null
                ? base.randomSurfaceDistance
                : parentSettings.randomSurfaceDistance;

        public override RandomUtils.Range randomSurfaceDistanceRange
            => _overwriteSettings || parentSettings == null
                ? base.randomSurfaceDistanceRange
                : parentSettings.randomSurfaceDistanceRange;

        public override bool rotateInMultiples
            => _overwriteSettings || parentSettings == null ? base.rotateInMultiples : parentSettings.rotateInMultiples;

        public override bool rotateToTheSurface
            => _overwriteSettings || parentSettings == null ? base.rotateToTheSurface : parentSettings.rotateToTheSurface;

        public override float rotationFactor
            => _overwriteSettings || parentSettings == null ? base.rotationFactor : parentSettings.rotationFactor;

        public override Vector3 scaleMultiplier
            => _overwriteSettings || parentSettings == null ? base.scaleMultiplier : parentSettings.scaleMultiplier;

        public override bool separateScaleAxes
            => _overwriteSettings || parentSettings == null ? base.separateScaleAxes : parentSettings.separateScaleAxes;

        public Vector3 size
        {
            get
            {
                if ( prefab == null )
                {
                    return Vector3.zero;
                }

                if ( _size == Vector3.zero )
                {
                    _size = BoundsUtils.GetBoundsRecursive( prefab.transform ).size;
                }

                return _size;
            }
        }

        public override float surfaceDistance
            => _overwriteSettings || parentSettings == null ? base.surfaceDistance : parentSettings.surfaceDistance;

        public override string thumbnailPath
        {
            get
            {
                if ( parentSettings == null )
                {
                    return null;
                }

                string parentPath = parentSettings.thumbnailPath;
                if ( parentPath == null )
                {
                    return null;
                }

                string path = parentPath.Insert( parentPath.Length - 4, "_" + id.ToString( "X" ) );
                return path;
            }
        }

        public override ThumbnailSettings thumbnailSettings
        {
            get => _overwriteThumbnailSettings || parentSettings == null
                ? base.thumbnailSettings
                : parentSettings.thumbnailSettings;
            set => base.thumbnailSettings = value;
        }

        #endregion

        #region Public Constructors

        public MultibrushItemSettings( GameObject prefab, MultibrushSettings parentSettings )
        {
            SetId();
            _prefab         = prefab;
            _parentId       = parentSettings.id;
            _parentSettings = parentSettings;
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier( _prefab, out _guid, out long localId );
            if ( _prefab == null )
            {
                return;
            }

            _prefabPath      = AssetDatabase.GetAssetPath( _prefab );
            _bottomVertices  = BoundsUtils.GetBottomVertices( prefab.transform );
            height           = BoundsUtils.GetBoundsRecursive( prefab.transform, prefab.transform.rotation ).size.y;
            _size            = BoundsUtils.GetBoundsRecursive( prefab.transform ).size;
            _bottomMagnitude = BoundsUtils.GetBottomMagnitude( prefab.transform );
            UpdateAssetType();
            UpdateThumbnail( updateItemThumbnails: false, savePng: true );
        }

        public MultibrushItemSettings()
        {
        }

        public MultibrushItemSettings( MultibrushItemSettings other )
        {
            Copy( other );
        }

        #endregion

        #region Public Methods

        public override BrushSettings Clone()
        {
            MultibrushItemSettings clone = new MultibrushItemSettings();
            clone._prefab = _prefab;
            clone._guid   = _guid;
            if ( parentSettings != null )
            {
                clone._parentId       = parentSettings.id;
                clone._parentSettings = parentSettings;
            }

            clone._bottomVertices  = bottomVertices == null ? null : bottomVertices.ToArray();
            clone._bottomMagnitude = bottomMagnitude;
            clone.height           = height;
            clone.Copy( this );
            clone.SetId();
            return clone;
        }

        public override void Copy( BrushSettings other )
        {
            if ( other is MultibrushItemSettings )
            {
                MultibrushItemSettings otherItemSettings = other as MultibrushItemSettings;
                _overwriteSettings          = otherItemSettings._overwriteSettings;
                _frequency                  = otherItemSettings._frequency;
                _overwriteThumbnailSettings = otherItemSettings._overwriteThumbnailSettings;
                _includeInThumbnail         = otherItemSettings._includeInThumbnail;
                _isAsset2D                  = otherItemSettings._isAsset2D;
            }

            base.Copy( other );
        }

        public void InitializeParentSettings( MultibrushSettings parentSettings )
        {
            _parentId       = parentSettings.id;
            _parentSettings = parentSettings;
            this.parentSettings.UpdateTotalFrequency();
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            _prefab = null;
        }

        public override void OnBeforeSerialize() => base.OnBeforeSerialize();

        public void UpdateAssetType() => _isAsset2D = Utils2D.Is2DAsset( prefab );

        public override void UpdateBottomVertices()
        {
            if ( prefab == null )
            {
                return;
            }

            _bottomVertices  = BoundsUtils.GetBottomVertices( prefab.transform );
            height           = BoundsUtils.GetBoundsRecursive( prefab.transform, prefab.transform.rotation ).size.y;
            _size            = BoundsUtils.GetBoundsRecursive( prefab.transform ).size;
            _bottomMagnitude = BoundsUtils.GetBottomMagnitude( prefab.transform );
        }

        #endregion

        #region Private Fields

        private float     _bottomMagnitude;
        private Vector3[] _bottomVertices;

        [NonSerialized] private MultibrushSettings _parentSettings;
        private                 GameObject         _prefab;
        private                 Vector3            _size = Vector3.zero;

        #endregion

        #region Private Methods

        private void SavePalette()
        {
            if ( parentSettings == null )
            {
                return;
            }

            parentSettings.SavePalette();
        }

        #endregion

    }

    [Serializable]
    public class MultibrushSettings : BrushSettings
    {

        #region Serialized

        [SerializeField] private string _name;

        [SerializeField]
        private List<MultibrushItemSettings> _items
            = new List<MultibrushItemSettings>();

        [SerializeField] private FrecuencyMode _frequencyMode               = FrecuencyMode.RANDOM;
        [SerializeField] private string        _pattern                     = "1...";
        [SerializeField] private bool          _restartPatternForEachStroke = true;

        #endregion

        #region Public Enums

        public enum FrecuencyMode
        {
            RANDOM,
            PATTERN,
        }

        #endregion

        #region Public Properties

        public bool allPrefabMissing
        {
            get
            {
                foreach ( MultibrushItemSettings item in _items )
                {
                    if ( item.prefab != null )
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        public bool containMissingPrefabs
        {
            get
            {
                foreach ( MultibrushItemSettings item in _items )
                {
                    if ( item.prefab == null )
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public override bool embedInSurface
        {
            get => _embedInSurface;
            set
            {
                if ( _embedInSurface == value )
                {
                    return;
                }

                _embedInSurface = value;
                if ( _embedInSurface )
                {
                    UpdateBottomVertices();
                }
            }
        }

        public FrecuencyMode frequencyMode
        {
            get => _frequencyMode;
            set
            {
                if ( _frequencyMode == value )
                {
                    return;
                }

                _frequencyMode = value;
            }
        }

        public override bool isAsset2D
        {
            get => _items.Exists( i => i.isAsset2D );
            set
            {
                foreach ( MultibrushItemSettings item in _items )
                {
                    item.isAsset2D = value;
                }
            }
        }

        public int itemCount => _items.Count;

        public MultibrushItemSettings[] items => _items.ToArray();

        public float maxBrushMagnitude
        {
            get
            {
                Vector3 max = maxBrushSize;
                return Mathf.Min( max.x, max.y, max.z );
            }
        }

        public Vector3 maxBrushSize
        {
            get
            {
                Vector3 max = Vector3.one * float.MinValue;
                foreach ( MultibrushItemSettings item in _items )
                {
                    max = Vector3.Max( max, item.size );
                }

                return max;
            }
        }

        public float minBrushMagnitude
        {
            get
            {
                Vector3 min = minBrushSize;
                return Mathf.Min( min.x, min.y, min.z );
            }
        }

        public Vector3 minBrushSize
        {
            get
            {
                Vector3 min = Vector3.one * float.MaxValue;
                foreach ( MultibrushItemSettings item in _items )
                {
                    min = Vector3.Min( min, item.size );
                }

                return min;
            }
        }

        public string name
        {
            get => _name;
            set
            {
                if ( _name == value )
                {
                    return;
                }

                _name = value;
            }
        }

        public int nextItemIndex
        {
            get
            {
                if ( frequencyMode == FrecuencyMode.RANDOM )
                {
                    if ( _items.Count == 1 )
                    {
                        return 0;
                    }

                    float rand = Random.Range( 0f, totalFrecuency );
                    float sum  = 0;
                    for ( int i = 0; i < _items.Count; ++i )
                    {
                        sum += _items[ i ].frequency;
                        if ( rand <= sum )
                        {
                            return i;
                        }
                    }

                    return -1;
                }

                if ( _patternMachine == null )
                {
                    if ( PatternMachine.Validate( _pattern, _items.Count, out PatternMachine.Token[] tokens )
                         == PatternMachine.ValidationResult.VALID )
                    {
                        _patternMachine = new PatternMachine( tokens );
                    }
                }

                return _patternMachine == null ? -2 : _patternMachine.nextIndex - 1;
            }
        }

        public int notNullItemCount => _items.Where( i => i.prefab != null ).Count();

        public PaletteData palette
        {
            get
            {
                if ( _palette == null )
                {
                    _palette = PaletteManager.GetPalette( this );
                }

                return _palette;
            }
            set => _palette = value;
        }

        public string pattern
        {
            get => _pattern;
            set
            {
                if ( _pattern == value )
                {
                    return;
                }

                _pattern = value;
            }
        }

        public PatternMachine patternMachine
        {
            get => _patternMachine;
            set => _patternMachine = value;
        }

        public bool restartPatternForEachStroke
        {
            get => _restartPatternForEachStroke;
            set
            {
                if ( _restartPatternForEachStroke == value )
                {
                    return;
                }

                _restartPatternForEachStroke = value;
            }
        }

        public override string thumbnailPath
            => palette == null ? null : palette.thumbnailsPath + "/" + id.ToString( "X" ) + ".png";

        public float totalFrecuency
        {
            get
            {
                if ( _totalFrequency == -1 )
                {
                    UpdateTotalFrequency();
                }

                return _totalFrequency;
            }
        }

        #endregion

        #region Public Constructors

        public MultibrushSettings( GameObject prefab, PaletteData palette )
        {
            SetId();
            this.palette = palette;
            _items.Add( new MultibrushItemSettings( prefab, this ) );
            _name = prefab.name;
            Copy( palette.brushCreationSettings.defaultBrushSettings );
            thumbnailSettings.Copy( palette.brushCreationSettings.defaultThumbnailSettings );
            UpdateThumbnail( updateItemThumbnails: false, savePng: true );
        }

        #endregion

        #region Public Methods

        public void AddItem( MultibrushItemSettings item )
        {
            _items.Add( item );
            OnItemCountChange();
        }

        public void Cleanup()
        {
            foreach ( MultibrushItemSettings item in items )
            {
                if ( item.prefab == null )
                {
                    RemoveItem( item );
                }
            }
        }

        public override BrushSettings Clone()
        {
            MultibrushSettings clone = new MultibrushSettings();
            clone.Copy( this );
            clone.SetId();
            clone.palette = _palette;
            return clone;
        }

        public MultibrushSettings CloneAndChangePalette( PaletteData palette )
        {
            MultibrushSettings clone = new MultibrushSettings();
            clone.Copy( this );
            clone.SetId();
            clone.palette = palette;
            return clone;
        }

        public BrushSettings CloneMainSettings()
        {
            BrushSettings clone = new BrushSettings();
            clone.Copy( this );
            return clone;
        }

        public bool ContainsPrefab( int prefabId )
            => _items.Exists( item => item.prefab != null && item.prefab.GetInstanceID() == prefabId );

        public bool ContainsPrefabPath( string path ) => _items.Exists( item => item.prefabPath == path );

        public bool ContainsSceneObject( GameObject obj )
        {
            if ( obj == null )
            {
                return false;
            }

            GameObject outermostPrefab = PrefabUtility.GetOutermostPrefabInstanceRoot( obj );
            if ( outermostPrefab == null )
            {
                return false;
            }

            GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource( outermostPrefab );
            if ( prefab == null )
            {
                return false;
            }

            return ContainsPrefab( prefab.GetInstanceID() );
        }

        public override void Copy( BrushSettings other )
        {

            if ( other is MultibrushSettings )
            {
                MultibrushSettings otherMulti = other as MultibrushSettings;
                _items.Clear();
                foreach ( MultibrushItemSettings item in otherMulti._items )
                {
                    MultibrushItemSettings clone = item.Clone() as MultibrushItemSettings;
                    clone.parentSettings = this;
                    _items.Add( clone );
                }

                _name                        = otherMulti._name;
                _frequencyMode               = otherMulti._frequencyMode;
                _pattern                     = otherMulti._pattern;
                _restartPatternForEachStroke = otherMulti._restartPatternForEachStroke;
                _totalFrequency              = otherMulti._totalFrequency;
            }

            base.Copy( other );
        }

        public void Duplicate( int index )
        {
            BrushSettings clone = _items[ index ].Clone();
            _items.Insert( index, clone as MultibrushItemSettings );
            OnItemCountChange();
        }

        public void DuplicateAt( int indexToDuplicate, int at )
        {
            BrushSettings clone = _items[ indexToDuplicate ].Clone();
            _items.Insert( at, clone as MultibrushItemSettings );
            OnItemCountChange();
        }

        public MultibrushItemSettings GetItemAt( int index )
        {
            if ( index >= _items.Count )
            {
                return null;
            }

            return _items[ index ];
        }

        public MultibrushItemSettings GetItemById( long itemId )
        {
            MultibrushItemSettings[] items = _items.Where( i => i.id == itemId ).ToArray();
            if ( items.Length == 0 )
            {
                return null;
            }

            return items[ 0 ];
        }

        public void InsertItemAt( MultibrushItemSettings item, int index )
        {
            _items.Insert( index, item );
            OnItemCountChange();
        }

        public bool ItemExist( long itemId ) => _items.Exists( i => i.id == itemId );

        public void RemoveItem( MultibrushItemSettings item )
        {
            if ( !_items.Contains( item ) )
            {
                return;
            }

            _items.Remove( item );
            OnItemCountChange();
            if ( _items.Count == 0 )
            {
                RemoveFromPalette();
            }
        }

        public void RemoveItemAt( int index )
        {
            _items.RemoveAt( index );
            OnItemCountChange();
            if ( _items.Count == 0 )
            {
                RemoveFromPalette();
            }
        }

        public void SavePalette()
        {
            if ( palette != null )
            {
                palette.Save();
            }
        }

        public void Swap( int fromIdx, int toIdx, ref int[] selection )
            => SelectionUtils.Swap( fromIdx, toIdx, ref selection, _items );

        public void UpdateAssetTypes()
        {
            foreach ( MultibrushItemSettings item in _items )
            {
                item.UpdateAssetType();
            }
        }

        public override void UpdateBottomVertices()
        {
            foreach ( MultibrushItemSettings item in _items )
            {
                item.UpdateBottomVertices();
            }
        }

        public void UpdateTotalFrequency()
        {
            _totalFrequency = 0;
            foreach ( MultibrushItemSettings item in _items )
            {
                _totalFrequency += item.frequency;
            }
        }

        #endregion

        #region Private Fields

        [field: NonSerialized] private PaletteData    _palette;
        [field: NonSerialized] private PatternMachine _patternMachine;

        [field: NonSerialized] private float _totalFrequency = -1;

        #endregion

        #region Private Constructors

        private MultibrushSettings()
        {
        }

        #endregion

        #region Private Methods

        private void OnItemCountChange()
        {
            UpdateTotalFrequency();
            UpdatePatternMachine();
            PWBCore.staticData.SaveAndUpdateVersion();
            BrushstrokeManager.UpdateBrushstroke();
            SavePalette();
            UpdateThumbnail( updateItemThumbnails: false, savePng: true );
            if ( _palette != null )
            {
                _palette.ClearObjectQuery();
            }
        }

        private void RemoveFromPalette()
        {
            if ( palette != null )
            {
                palette.RemoveBrush( this );
            }
        }

        private void UpdatePatternMachine()
        {
            if ( PatternMachine.Validate( _pattern, _items.Count, out PatternMachine.Token[] tokens )
                 != PatternMachine.ValidationResult.VALID )
            {
                _patternMachine = null;
            }
        }

        #endregion

    }

    [Serializable]
    public class BrushCreationSettings
    {

        #region Serialized

        [SerializeField] private bool              _includeSubfolders = true;
        [SerializeField] private bool              _addLabelsToDroppedPrefabs;
        [SerializeField] private string            _labelsCSV;
        [SerializeField] private BrushSettings     _defaultBrushSettings     = new BrushSettings();
        [SerializeField] private ThumbnailSettings _defaultThumbnailSettings = new ThumbnailSettings();

        #endregion

        #region Public Properties

        public bool addLabelsToDroppedPrefabs
        {
            get => _addLabelsToDroppedPrefabs;
            set
            {
                if ( _addLabelsToDroppedPrefabs == value )
                {
                    return;
                }

                _addLabelsToDroppedPrefabs = value;
            }
        }

        public BrushSettings defaultBrushSettings => _defaultBrushSettings;

        public ThumbnailSettings defaultThumbnailSettings => _defaultThumbnailSettings;

        public bool includeSubfolders
        {
            get => _includeSubfolders;
            set
            {
                if ( _includeSubfolders == value )
                {
                    return;
                }

                _includeSubfolders = value;
            }
        }

        public string[] labels
        {
            get
            {
                if ( _labels == null
                     || ( _labels.Length == 0 && _labelsCSV != null && _labelsCSV != string.Empty ) )
                {
                    SplitCSV();
                }

                return _labels;
            }
        }

        public string labelsCSV
        {
            get => _labelsCSV;
            set
            {
                if ( _labelsCSV == value )
                {
                    return;
                }

                if ( value == string.Empty )
                {
                    _labelsCSV = string.Empty;
                    _labels    = new string[ 0 ];
                    return;
                }

                string trimmed = Regex.Replace( value.Trim(), "[( *, +)]+", ", " );
                if ( trimmed.Last() == ' ' )
                {
                    trimmed = trimmed.Substring( 0, trimmed.Length - 2 );
                }

                if ( trimmed.First() == ',' )
                {
                    trimmed = trimmed.Substring( 1 );
                }

                if ( _labelsCSV == trimmed )
                {
                    return;
                }

                _labelsCSV = trimmed;
                SplitCSV();
            }
        }

        #endregion

        #region Public Methods

        public BrushCreationSettings Clone()
        {
            BrushCreationSettings clone = new BrushCreationSettings();
            clone.Copy( this );
            return clone;
        }

        public void Copy( BrushCreationSettings other )
        {
            _includeSubfolders         = other._includeSubfolders;
            _addLabelsToDroppedPrefabs = other._addLabelsToDroppedPrefabs;
            _labelsCSV                 = other._labelsCSV;
            if ( other._labels != null )
            {
                _labels = new string[ other._labels.Length ];
                Array.Copy( other._labels, _labels, other._labels.Length );
            }

            _defaultBrushSettings.Copy( other._defaultBrushSettings );
            _defaultThumbnailSettings.Copy( other._defaultThumbnailSettings );
        }

        public void FactoryResetDefaultBrushSettings()     => _defaultBrushSettings = new BrushSettings();
        public void FactoryResetDefaultThumbnailSettings() => _defaultThumbnailSettings = new ThumbnailSettings();

        #endregion

        #region Private Fields

        private string[] _labels;

        #endregion

        #region Private Methods

        private void SplitCSV() => _labels = _labelsCSV.Replace( ", ", "," ).Split( ',' );

        #endregion

    }

    public class BrushInputData
    {

        #region Public Fields

        public readonly bool      control;
        public readonly EventType eventType;
        public readonly int       index;
        public readonly float     mouseX;
        public readonly Rect      rect;
        public readonly bool      shift;

        #endregion

        #region Public Constructors

        public BrushInputData( int index, Rect rect, EventType eventType, bool control, bool shift, float mouseX )
        {
            this.index     = index;
            this.rect      = rect;
            this.eventType = eventType;
            this.control   = !shift && control;
            this.shift     = shift;
            this.mouseX    = mouseX;
        }

        #endregion

    }

    #endregion

    [Serializable]
    public class PaletteData
    {

        #region Serialized

        [SerializeField] private string _version = PWBData.VERSION;
        [SerializeField] private string _name;
        [SerializeField] private long   _id = -1;

        [SerializeField]
        private List<MultibrushSettings> _brushes
            = new List<MultibrushSettings>();

        [SerializeField] private BrushCreationSettings _brushCreationSettings = new BrushCreationSettings();

        #endregion

        #region Public Fields

        public SortedDictionary<int, bool> _objectQuery
            = new SortedDictionary<int, bool>();

        #endregion

        #region Public Properties

        public int brushCount => _brushes.Count;

        public BrushCreationSettings brushCreationSettings => _brushCreationSettings;

        public MultibrushSettings[] brushes => _brushes.Where( b => !b.allPrefabMissing ).ToArray();

        public string filePath
        {
            get
            {
                void SetFilePath()
                {
                    _filePath = PWBData.palettesDirectory + "/" + GetFileNameFromData( this );
                }

                if ( _filePath == null )
                {
                    SetFilePath();
                }
                else if ( !File.Exists( _filePath ) )
                {
                    SetFilePath();
                }

                return _filePath;
            }
            set => _filePath = value;
        }

        public long id => _id;

        public string name
        {
            get => _name;
            set
            {
                if ( _name == value )
                {
                    return;
                }

                _name = value;
                Save();
            }
        }

        public bool saving { get; private set; }

        public string thumbnailsPath
        {
            get
            {
                string path = filePath.Substring( 0, filePath.Length - 4 );
                if ( !Directory.Exists( path ) )
                {
                    Directory.CreateDirectory( path );
                }

                return path;
            }
        }

        public string version
        {
            get => _version;
            set => _version = value;
        }

        #endregion

        #region Public Constructors

        public PaletteData( string name, long id )
        {
            ( _name, _id ) = ( name, id );
        }

        #endregion

        #region Public Methods

        public void AddBrush( MultibrushSettings brush )
        {
            _brushes.Add( brush );
            SetSpritesThumbnailSettings( brush );
            brush.palette = this;
            PWBCore.staticData.SaveAndUpdateVersion();
            Save();
            ClearObjectQuery();
        }

        public void AscendingSort()
        {
            _brushes.Sort( delegate( MultibrushSettings x, MultibrushSettings y ) { return x.name.CompareTo( y.name ); } );
            PaletteManager.ClearSelection();
            PWBCore.staticData.SaveAndUpdateVersion();
            PrefabPalette.OnChangeRepaint();
        }

        public void Cleanup()
        {
            foreach ( MultibrushSettings brush in _brushes.ToArray() )
            {
                brush.Cleanup();
            }

            Save();
            ClearObjectQuery();
        }

        public void ClearObjectQuery()
        {
            if ( _objectQuery != null )
            {
                _objectQuery.Clear();
            }
            else
            {
                _objectQuery = new SortedDictionary<int, bool>();
            }
        }

        public bool ContainsBrush( MultibrushSettings brush )
            => _brushes.Contains( brush ) || _brushes.Exists( b => b.id == brush.id );

        public bool ContainsPrefab( GameObject prefab )
        {
            if ( prefab == null )
            {
                return false;
            }

            return _brushes.Exists( brush => brush.ContainsPrefab( prefab.GetInstanceID() ) );
        }

        public bool ContainsPrefabPath( string path ) => _brushes.Exists( brush => brush.ContainsPrefabPath( path ) );

        public bool ContainsSceneObject( GameObject obj )
        {
            if ( obj == null )
            {
                return false;
            }

            int objId = obj.GetInstanceID();
            if ( _objectQuery == null )
            {
                _objectQuery = new SortedDictionary<int, bool>();
            }

            if ( _objectQuery.ContainsKey( objId ) )
            {
                return _objectQuery[ objId ];
            }

            _objectQuery.Add( objId, false );
            GameObject outermostPrefab = PrefabUtility.GetOutermostPrefabInstanceRoot( obj );
            if ( outermostPrefab == null )
            {
                return false;
            }

            GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource( outermostPrefab );
            if ( prefab == null )
            {
                return false;
            }

            _objectQuery[ objId ] = _brushes.Exists( brush => brush.ContainsPrefab( prefab.GetInstanceID() ) );
            return _objectQuery[ objId ];
        }

        public void Copy( PaletteData other )
        {
            _brushes.Clear();
            MultibrushSettings[] otherBrushes = other.brushes.ToArray();
            MultibrushSettings[] cloneBrushes = otherBrushes.Select( b => b.CloneAndChangePalette( this ) ).ToArray();
            _brushes.AddRange( cloneBrushes );
            _name = other.name;
            _brushCreationSettings.Copy( other._brushCreationSettings );
        }

        public void DescendingSort()
        {
            _brushes.Sort( delegate( MultibrushSettings x, MultibrushSettings y ) { return y.name.CompareTo( x.name ); } );
            PaletteManager.ClearSelection();
            PWBCore.staticData.SaveAndUpdateVersion();
            PrefabPalette.OnChangeRepaint();
        }

        public void DuplicateBrush( int index ) => DuplicateBrushAt( index, index );

        public void DuplicateBrushAt( int indexToDuplicate, int at )
        {
            BrushSettings clone = _brushes[ indexToDuplicate ].Clone();
            clone.UpdateThumbnail( updateItemThumbnails: true, savePng: true );
            _brushes.Insert( at, clone as MultibrushSettings );
            PWBCore.staticData.SaveAndUpdateVersion();
            Save();
        }

        public int FindBrushIdx( GameObject obj )
        {
            if ( obj == null )
            {
                return -1;
            }

            GameObject outermostPrefab = PrefabUtility.GetOutermostPrefabInstanceRoot( obj );
            if ( outermostPrefab == null )
            {
                return -1;
            }

            GameObject prefab = PrefabUtility.GetCorrespondingObjectFromSource( outermostPrefab );
            if ( prefab == null )
            {
                return -1;
            }

            int idx = _brushes.FindIndex( brush => brush.ContainsPrefab( prefab.GetInstanceID() ) && brush.itemCount == 1 );
            if ( idx == -1 )
            {
                idx = _brushes.FindIndex( brush => brush.ContainsPrefab( prefab.GetInstanceID() ) );
            }

            return idx;
        }

        public MultibrushSettings GetBrush( int idx )
        {
            if ( idx    < 0
                 || idx >= _brushes.Count )
            {
                return null;
            }

            if ( _brushes[ idx ].allPrefabMissing )
            {
                return null;
            }

            return _brushes[ idx ];
        }

        public static string GetFileNameFromData( PaletteData data ) => "PWB_" + data._id.ToString( "X" ) + ".txt";

        public void InsertBrushAt( MultibrushSettings brush, int idx )
        {
            _brushes.Insert( idx, brush );
            SetSpritesThumbnailSettings( brush );
            brush.palette = this;
            PWBCore.staticData.SaveAndUpdateVersion();
            Save();
            ClearObjectQuery();
        }

        public void ReloadFromFile()
        {
            string fileText = File.ReadAllText( _filePath );
            if ( string.IsNullOrEmpty( fileText ) )
            {
                return;
            }

            PaletteData paletteData = JsonUtility.FromJson<PaletteData>( fileText );
            if ( paletteData == null )
            {
                return;
            }

            Copy( paletteData );
            ClearObjectQuery();
        }

        public void RemoveBrush( MultibrushSettings brush )
        {
            _brushes.Remove( brush );
            PWBCore.staticData.SaveAndUpdateVersion();
            BrushstrokeManager.UpdateBrushstroke();
            PrefabPalette.OnChangeRepaint();
            Save();
            ClearObjectQuery();
        }

        public void RemoveBrushAt( int idx )
        {
            _brushes.RemoveAt( idx );
            PWBCore.staticData.SaveAndUpdateVersion();
            BrushstrokeManager.UpdateBrushstroke();
            Save();
            ClearObjectQuery();
        }

        public string Save()
        {
            saving = true;
            string jsonString = JsonUtility.ToJson( this, true );
            bool   fileExist  = File.Exists( filePath );
            File.WriteAllText( filePath, jsonString );
            if ( !fileExist )
            {
                PWBCore.AssetDatabaseRefresh();
            }

            return filePath;
        }

        public void StopSaving() => saving = false;

        public void Swap( int fromIdx, int toIdx, ref int[] selection )
            => SelectionUtils.Swap( fromIdx, toIdx, ref selection, _brushes );

        public void UpdateAllThumbnails()
        {
            foreach ( MultibrushSettings brush in _brushes )
            {
                brush.UpdateThumbnail( updateItemThumbnails: true, savePng: true );
            }
        }

        #endregion

        #region Private Fields

        private string _filePath;

        #endregion

        #region Private Methods

        private void SetSpritesThumbnailSettings( MultibrushSettings brush )
        {
            foreach ( MultibrushItemSettings item in brush.items )
            {
                if ( item.isAsset2D )
                {
                    item.thumbnailSettings.targetEuler  = new Vector3( 17.5f, 0f, 0f );
                    item.thumbnailSettings.zoom         = 1.47f;
                    item.thumbnailSettings.targetOffset = new Vector2( 0f, -0.06f );
                }
            }

            brush.rotateToTheSurface = false;
        }

        #endregion

    }

    [Serializable]
    public class PaletteManager : ISerializationCallbackReceiver
    {

        #region Statics and Constants

        public static Action OnBrushChanged;
        public static Action OnSelectionChanged;
        public static Action OnPaletteChanged;

        private static PaletteManager _instance;

        #endregion

        #region Serialized

        [SerializeField] private int  _selectedPaletteIdx;
        [SerializeField] private int  _selectedBrushIdx = -1;
        [SerializeField] private bool _showBrushName;
        [SerializeField] private bool _viewList;
        [SerializeField] private bool _showTabsInMultipleRows;

        [SerializeField] private int _iconSize = PrefabPalette.DEFAULT_ICON_SIZE;

        #endregion

        #region Public Properties

        public static bool addingPalettes { get; set; }

        public static int iconSize
        {
            get => instance._iconSize;
            set
            {
                if ( instance._iconSize == value )
                {
                    return;
                }

                instance._iconSize = value;
                PWBCore.staticData.SaveAndUpdateVersion();
            }
        }

        public static int[] idxSelection
        {
            get => instance._idxSelection.ToArray();
            set
            {
                instance._idxSelection = new HashSet<int>( value );
                if ( OnSelectionChanged != null )
                {
                    OnSelectionChanged();
                }
            }
        }

        public static PaletteManager instance
        {
            get
            {
                if ( _instance == null )
                {
                    _instance = new PaletteManager();
                }

                return _instance;
            }
        }

        public static bool movingBrushes => instance._movingBrushesFromIdx >= 0;

        public static int           paletteCount => instance.paletteDataList.Count;
        public static PaletteData[] paletteData  => instance.paletteDataList.ToArray();

        public List<PaletteData> paletteDataList
        {
            get
            {
                if ( _paletteDataList.Count == 0 )
                {
                    AddPalette( new PaletteData( "Palette", DateTime.Now.ToBinary() ), save: true );
                    _selectedPaletteIdx = 0;
                    _selectedBrushIdx   = -1;
                }

                return _paletteDataList;
            }
        }

        public static long[] paletteIds => instance.paletteDataList.Select( p => p.id ).ToArray();

        public static string[] paletteNames => instance.paletteDataList.Select( p => p.name ).ToArray();

        public static bool pickingBrushes
        {
            get => instance._pickingBrushes;
            set
            {
                if ( instance._pickingBrushes == value )
                {
                    return;
                }

                instance._pickingBrushes = value;
                if ( instance._pickingBrushes )
                {
                    PWBCore.UpdateTempColliders();
                    PWBIO.repaint = true;
                    SceneView.RepaintAll();
                }

                PrefabPalette.RepainWindow();
            }
        }

        public static bool savePending { get; private set; }

        public static MultibrushSettings selectedBrush
            => instance._selectedBrushIdx < 0 ? null : selectedPalette.GetBrush( instance._selectedBrushIdx );

        public static int selectedBrushIdx
        {
            get => instance._selectedBrushIdx;
            set
            {
                if ( instance._selectedBrushIdx == value )
                {
                    return;
                }

                instance._selectedBrushIdx = value;
                if ( selectedBrush != null )
                {
                    selectedBrush.UpdateBottomVertices();
                    selectedBrush.UpdateAssetTypes();
                }
                else
                {
                    instance._selectedBrushIdx = -1;
                }

                BrushstrokeManager.UpdateBrushstroke( true );
                if ( ToolManager.tool == ToolManager.PaintTool.PIN )
                {
                    PWBIO.ResetPinValues();
                }

                if ( OnBrushChanged != null )
                {
                    OnBrushChanged();
                }
            }
        }

        public static PaletteData selectedPalette => instance.paletteDataList[ selectedPaletteIdx ];

        public static int selectedPaletteIdx
        {
            get
            {
                instance._selectedPaletteIdx = Mathf.Clamp( instance._selectedPaletteIdx, 0,
                    Mathf.Max( instance.paletteDataList.Count - 1, 0 ) );
                return instance._selectedPaletteIdx;
            }
            set
            {
                value = Mathf.Max( value, 0 );
                if ( instance._selectedPaletteIdx == value )
                {
                    return;
                }

                instance._selectedPaletteIdx = value;
                if ( OnPaletteChanged != null )
                {
                    OnPaletteChanged();
                }
            }
        }

        public static int selectionCount
        {
            get
            {
                if ( instance._idxSelection.Count  == 0
                     && instance._selectedBrushIdx > 0
                     && selectedBrush              != null )
                {
                    instance._idxSelection.Add( instance._selectedBrushIdx );
                    if ( OnSelectionChanged != null )
                    {
                        OnSelectionChanged();
                    }
                }

                return instance._idxSelection.Count;
            }
        }

        public static bool showBrushName
        {
            get => instance._showBrushName;
            set
            {
                if ( instance._showBrushName == value )
                {
                    return;
                }

                instance._showBrushName = value;
                PWBCore.staticData.SaveAndUpdateVersion();
            }
        }

        public static bool showTabsInMultipleRows
        {
            get => instance._showTabsInMultipleRows;
            set
            {
                if ( instance._showTabsInMultipleRows == value )
                {
                    return;
                }

                instance._showTabsInMultipleRows = value;
                PWBCore.staticData.SaveAndUpdateVersion();
            }
        }

        public static bool viewList
        {
            get => instance._viewList;
            set
            {
                if ( instance._viewList == value )
                {
                    return;
                }

                instance._viewList = value;
                PWBCore.staticData.SaveAndUpdateVersion();
            }
        }

        #endregion

        #region Public Methods

        public static void AddPalette( PaletteData palette, bool save )
        {
            addingPalettes = true;
            if ( instance._paletteDataList.Exists( p => p.id == palette.id ) )
            {
                return;
            }

            instance._paletteDataList.Add( palette );
            if ( save )
            {
                palette.filePath = PWBData.palettesDirectory + "/" + PaletteData.GetFileNameFromData( palette );
                palette.Save();
            }
        }

        public static void AddToSelection( int index )
        {
            instance._idxSelection.Add( index );
            if ( OnSelectionChanged != null )
            {
                OnSelectionChanged();
            }
        }

        public static bool BrushExist( long id ) => instance.paletteDataList.Exists( b => b.id == id );

        public static void Cleanup()
        {
            foreach ( PaletteData palette in instance.paletteDataList.ToArray() )
            {
                palette.Cleanup();
            }
        }

        public static void Clear()
        {
            ClearPaletteList();
            instance.paletteDataList.Add( new PaletteData( "Palette", DateTime.Now.ToBinary() ) );
            instance._selectedPaletteIdx = 0;
            instance._selectedBrushIdx   = -1;
            instance._idxSelection.Clear();
            instance._pickingBrushes = false;
        }

        public static void ClearPaletteList() => instance._paletteDataList.Clear();

        public static void ClearSelection( bool updateBrushProperties = true )
        {
            selectedBrushIdx = -1;
            instance._idxSelection.Clear();
            if ( !updateBrushProperties )
            {
                return;
            }

            if ( OnSelectionChanged != null )
            {
                OnSelectionChanged();
            }

            BrushProperties.RepaintWindow();
        }

        public static void DuplicatePalette( int paletteIdx )
        {
            PaletteData palette   = instance._paletteDataList[ paletteIdx ];
            long        cloneId   = DateTime.Now.ToBinary();
            string      cloneName = palette.name + " Copy";
            PaletteData clone     = new PaletteData( cloneName, cloneId );
            clone.Copy( palette );
            clone.name = cloneName;
            AddPalette( clone, save: true );
            clone.UpdateAllThumbnails();
        }

        public static MultibrushSettings GetBrushById( long id )
        {
            foreach ( PaletteData palette in instance.paletteDataList )
            foreach ( MultibrushSettings brush in palette.brushes )
            {
                if ( brush.id == id )
                {
                    return brush;
                }
            }

            return null;
        }

        public static MultibrushSettings GetBrushByItemId( long id )
        {
            foreach ( PaletteData palette in instance.paletteDataList )
            foreach ( MultibrushSettings brush in palette.brushes )
            foreach ( MultibrushItemSettings item in brush.items )
            {
                if ( item.id == id )
                {
                    return brush;
                }
            }

            return null;
        }

        public static MultibrushSettings GetBrushByThumbnail( string thumbnailPath )
        {
            foreach ( PaletteData palette in instance.paletteDataList )
            foreach ( MultibrushSettings brush in palette.brushes )
            {
                if ( brush.thumbnailPath == thumbnailPath )
                {
                    return brush;
                }
            }

            return null;
        }

        public static int GetBrushIdx( long id )
        {
            PaletteData          palette = selectedPalette;
            MultibrushSettings[] brushes = palette.brushes;
            for ( int i = 0; i < brushes.Length; ++i )
            {
                if ( brushes[ i ].id == id )
                {
                    return i;
                }
            }

            return -1;
        }

        public static PaletteData GetPalette( MultibrushSettings brush )
        {
            foreach ( PaletteData palette in instance.paletteDataList )
            {
                if ( palette.ContainsBrush( brush ) )
                {
                    return palette;
                }
            }

            return null;
        }

        public static PaletteData GetPalette( long id )
        {
            foreach ( PaletteData palette in instance.paletteDataList )
            {
                if ( palette.id == id )
                {
                    return palette;
                }
            }

            return null;
        }

        public static string[] GetPaletteThumbnailFolderPaths()
        {
            string[] paths = new string[ instance.paletteDataList.Count ];
            for ( int i = 0; i < paths.Length; ++i )
            {
                paths[ i ] = instance.paletteDataList[ i ].thumbnailsPath;
            }

            return paths;
        }

        public void LoadPaletteFiles( bool deleteUnusedThumbnails )
        {
            string[] txtPaths = Directory.GetFiles( PWBData.palettesDirectory, "*.txt" );
            if ( txtPaths.Length == 0 )
            {
                if ( _paletteDataList.Count == 0 )
                {
                    _paletteDataList = new List<PaletteData> { new PaletteData( "Palette", DateTime.Now.ToBinary() ) };
                }

                _paletteDataList[ 0 ].filePath = _paletteDataList[ 0 ].Save();
            }

            bool clearList = true;
            foreach ( string path in txtPaths )
            {
                string fileText = File.ReadAllText( path );
                if ( string.IsNullOrEmpty( fileText ) )
                {
                    continue;
                }

                try
                {
                    PaletteData paletteData = JsonUtility.FromJson<PaletteData>( fileText );
                    if ( paletteData == null )
                    {
                        continue;
                    }

                    if ( clearList )
                    {
                        _paletteDataList.Clear();
                        clearList = false;
                    }

                    paletteData.filePath = path;
                    AddPalette( paletteData, save: false );
                }
                catch
                {
                    Debug.LogWarning( "PWB found a corrupted palette file at: " + path );
                }

            }

            if ( deleteUnusedThumbnails )
            {
                ThumbnailUtils.DeleteUnusedThumbnails();
            }
        }

        public static void MoveBrushesToAnotherPalette( int paletteIdx, bool removeFromSource )
        {
            if ( instance._movingBrushesFromIdx    < 0
                 || instance._movingBrushesFromIdx >= paletteCount )
            {
                return;
            }

            if ( paletteIdx    < 0
                 || paletteIdx >= paletteCount )
            {
                return;
            }

            if ( instance._movingBrushesFromIdx != paletteIdx )
            {
                PaletteData sourcePalette      = paletteData[ instance._movingBrushesFromIdx ];
                PaletteData destinationPalette = paletteData[ paletteIdx ];
                foreach ( MultibrushSettings brush in instance._brushesToMove )
                {
                    destinationPalette.AddBrush( brush );
                    if ( removeFromSource )
                    {
                        sourcePalette.RemoveBrush( brush );
                    }
                }
            }

            instance._brushesToMove.Clear();
            instance._movingBrushesFromIdx = -1;
        }

        public static void MoveBrushesToSelectedPalette() => MoveBrushesToAnotherPalette( selectedPaletteIdx, true );

        public void OnAfterDeserialize()
        {
        }

        public void OnBeforeSerialize()
        {
        }

        public static void PasteBrushesToSelectedPalette() => MoveBrushesToAnotherPalette( selectedPaletteIdx, false );

        public static void RemoveFromSelection( int index )
        {
            instance._idxSelection.Remove( index );
            if ( OnSelectionChanged != null )
            {
                OnSelectionChanged();
            }
        }

        public static void RemovePaletteAt( int paletteIdx )
        {
            string filePath            = instance._paletteDataList[ paletteIdx ].filePath;
            string thumbnailFolderPath = instance._paletteDataList[ paletteIdx ].thumbnailsPath;
            instance._paletteDataList.RemoveAt( paletteIdx );
            string metapath = filePath + ".meta";
            if ( File.Exists( metapath ) )
            {
                File.Delete( metapath );
            }

            if ( File.Exists( filePath ) )
            {
                File.Delete( filePath );
            }

            metapath = thumbnailFolderPath + ".meta";
            if ( File.Exists( metapath ) )
            {
                File.Delete( metapath );
            }

            if ( Directory.Exists( thumbnailFolderPath ) )
            {
                Directory.Delete( thumbnailFolderPath, true );
            }

            PWBCore.AssetDatabaseRefresh();
        }

        public static void SaveIfPending()
        {
            if ( savePending )
            {
                SavePalettes();
            }

            savePending = false;
        }

        public static void SelectBrush( int idx )
        {
            if ( PrefabPalette.instance == null )
            {
                return;
            }

            if ( selectedPalette.brushCount == 0 )
            {
                return;
            }

            if ( !PrefabPalette.instance.FilteredBrushListContains( idx ) )
            {
                return;
            }

            instance._idxSelection.Clear();
            selectedBrushIdx = idx;
            if ( selectedBrush != null )
            {
                selectedBrush.UpdateBottomVertices();
                selectedBrush.UpdateAssetTypes();
            }

            AddToSelection( selectedBrushIdx );
            PrefabPalette.instance.FrameSelectedBrush();
            PrefabPalette.RepainWindow();
        }

        public static void SelectBrushesToMove()
        {
            instance._movingBrushesFromIdx = selectedPaletteIdx;
            instance._brushesToMove.Clear();
            foreach ( int idx in instance._idxSelection )
            {
                instance._brushesToMove.Add( selectedPalette.GetBrush( idx ) );
            }
        }

        public static bool SelectionContains( int index ) => instance._idxSelection.Contains( index );

        public static void SelectNextBrush()
        {
            if ( PrefabPalette.instance == null )
            {
                return;
            }

            if ( selectedPalette.brushCount <= 1 )
            {
                return;
            }

            instance._idxSelection.Clear();
            int selectedIdx = instance._selectedBrushIdx;
            int count       = 0;
            do
            {
                selectedIdx = ( selectedIdx + 1 ) % selectedPalette.brushCount;
                if ( ++count > selectedPalette.brushCount )
                {
                    return;
                }
            }
            while ( !PrefabPalette.instance.FilteredBrushListContains( selectedIdx ) );

            selectedBrushIdx = selectedIdx;
            if ( selectedBrush != null )
            {
                selectedBrush.UpdateBottomVertices();
                selectedBrush.UpdateAssetTypes();
            }

            AddToSelection( selectedBrushIdx );
            PrefabPalette.instance.FrameSelectedBrush();
        }

        public static void SelectNextPalette()
        {
            if ( PrefabPalette.instance == null )
            {
                return;
            }

            if ( paletteCount <= 1 )
            {
                return;
            }

            instance._idxSelection.Clear();

            Dictionary<int, PaletteData> idsDic = paletteData.Select( ( palette, index ) => new { palette, index } )
                                                             .ToDictionary( item => item.index, item => item.palette );
            Dictionary<int, long> sortedDic = PWBCore.staticData.selectTheNextPaletteInAlphabeticalOrder
                ? ( from item in idsDic orderby item.Value.name select item )
                .ToDictionary( pair => pair.Key, pair => pair.Value.id )
                : idsDic.ToDictionary( pair => pair.Key, pair => pair.Value.id );

            long selectedId = selectedPalette.id;
            int  sortedIdx  = -1;
            bool stop       = false;

            foreach ( int idx in sortedDic.Keys )
            {
                if ( sortedIdx == -1 )
                {
                    sortedIdx = idx;
                }

                if ( stop )
                {
                    sortedIdx = idx;
                    break;
                }

                stop = sortedDic[ idx ] == selectedId;
            }

            PrefabPalette.instance.SelectPalette( sortedIdx );
            selectedBrushIdx = 0;
            AddToSelection( selectedBrushIdx );
            PrefabPalette.instance.FrameSelectedBrush();
            PrefabPalette.RepainWindow();
        }

        public static void SelectPreviousBrush()
        {
            if ( PrefabPalette.instance == null )
            {
                return;
            }

            if ( selectedPalette.brushCount <= 1 )
            {
                return;
            }

            instance._idxSelection.Clear();
            int selectedIdx = instance._selectedBrushIdx;
            int count       = 0;
            do
            {
                selectedIdx = ( selectedIdx == 0 ? selectedPalette.brushCount : selectedIdx ) - 1;
                if ( ++count > selectedPalette.brushCount )
                {
                    return;
                }
            }
            while ( !PrefabPalette.instance.FilteredBrushListContains( selectedIdx ) );

            selectedBrushIdx = selectedIdx;
            if ( selectedBrush != null )
            {
                selectedBrush.UpdateBottomVertices();
                selectedBrush.UpdateAssetTypes();
            }

            AddToSelection( selectedBrushIdx );
            PrefabPalette.instance.FrameSelectedBrush();
        }

        public static void SelectPreviousPalette()
        {
            if ( PrefabPalette.instance == null )
            {
                return;
            }

            if ( paletteCount <= 1 )
            {
                return;
            }

            instance._idxSelection.Clear();

            Dictionary<int, PaletteData> idsDic = paletteData.Select( ( palette, index ) => new { palette, index } )
                                                             .ToDictionary( item => item.index, item => item.palette );
            Dictionary<int, long> sortedDic = PWBCore.staticData.selectTheNextPaletteInAlphabeticalOrder
                ? ( from item in idsDic orderby item.Value.name descending select item )
                .ToDictionary( pair => pair.Key, pair => pair.Value.id )
                : ( from item in idsDic orderby item.Key descending select item )
                .ToDictionary( pair => pair.Key, pair => pair.Value.id );

            long selectedId = selectedPalette.id;
            int  sortedIdx  = -1;
            bool stop       = false;

            foreach ( int idx in sortedDic.Keys )
            {
                if ( sortedIdx == -1 )
                {
                    sortedIdx = idx;
                }

                if ( stop )
                {
                    sortedIdx = idx;
                    break;
                }

                stop = sortedDic[ idx ] == selectedId;
            }

            PrefabPalette.instance.SelectPalette( sortedIdx );
            selectedBrushIdx = 0;
            AddToSelection( selectedBrushIdx );
            PrefabPalette.instance.FrameSelectedBrush();
            PrefabPalette.RepainWindow();
        }

        public static void SetSavePending() => savePending = true;

        public static void SwapPalette( int from, int to )
        {
            if ( from == to )
            {
                return;
            }

            instance.paletteDataList.Insert( to, instance.paletteDataList[ from ] );
            int removeIdx = from;
            if ( from > to )
            {
                ++removeIdx;
            }

            instance.paletteDataList.RemoveAt( removeIdx );
        }

        public static void UpdateAllThumbnails()
        {
            PaletteData[] palettes = instance.paletteDataList.ToArray();
            foreach ( PaletteData palette in palettes )
            {
                palette.UpdateAllThumbnails();
            }
        }

        public static void UpdateSelectedThumbnails()
        {
            foreach ( int idx in instance._idxSelection )
            {
                selectedPalette.GetBrush( idx ).UpdateThumbnail( updateItemThumbnails: true, savePng: true );
            }
        }

        #endregion

        #region Private Fields

        private List<MultibrushSettings> _brushesToMove
            = new List<MultibrushSettings>();

        private HashSet<int> _idxSelection = new HashSet<int>();

        private int _movingBrushesFromIdx = -1;

        private List<PaletteData> _paletteDataList
            = new List<PaletteData> { new PaletteData( "Palette", DateTime.Now.ToBinary() ) };

        private bool _pickingBrushes;

        #endregion

        #region Private Constructors

        private PaletteManager()
        {
        }

        #endregion

        #region Private Methods

        private static void SavePalettes()
        {
            foreach ( PaletteData palette in instance.paletteDataList )
            {
                palette.Save();
            }
        }

        #endregion

        #region CLIPBOARD

        private static BrushSettings     _clipboardSettings;
        private static ThumbnailSettings _clipboardThumbnailSettings;

        public enum Trit
        {
            FALSE,
            TRUE,
            SAME,
        }

        private static Trit _clipboardOverwriteThumbnailSettings = Trit.FALSE;

        public static BrushSettings clipboardSetting
        {
            get => _clipboardSettings;
            set => _clipboardSettings = value;
        }

        public static ThumbnailSettings clipboardThumbnailSettings
        {
            get => _clipboardThumbnailSettings;
            set => _clipboardThumbnailSettings = value;
        }

        public static Trit clipboardOverwriteThumbnailSettings
        {
            get => _clipboardOverwriteThumbnailSettings;
            set => _clipboardOverwriteThumbnailSettings = value;
        }

        #endregion

    }

    public class PaletteAssetPostprocessor : AssetPostprocessor
    {

        #region Unity Functions

        private static void OnPostprocessAllAssets( string[] importedAssets, string[] deletedAssets,
                                                    string[] movedAssets,    string[] movedFromAssetPaths )
        {
            bool repaintPalette = false;
            foreach ( string path in importedAssets )
            {
                if ( PaletteManager.selectedPalette.ContainsPrefabPath( path ) )
                {
                    repaintPalette = true;
                    break;
                }
            }

            foreach ( string path in deletedAssets )
            {
                if ( PaletteManager.selectedPalette.ContainsPrefabPath( path ) )
                {
                    PaletteManager.Cleanup();
                    PaletteManager.ClearSelection();
                    repaintPalette = true;
                    break;
                }
            }

            if ( repaintPalette )
            {
                PrefabPalette.OnChangeRepaint();
            }
        }

        #endregion

    }
}