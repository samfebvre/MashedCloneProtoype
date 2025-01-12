using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PluginMaster
{
    public abstract class ThumbnailEditorWindow : EditorWindow
    {

        #region Statics and Constants

        public const string UNDO_CMD = "Edit Thumbnail";

        protected static BrushSettings _brush;
        public static    int           _settingsIdx;

        private static ThumbnailEditorWindow _instance;

        #endregion

        #region Serialized

        [SerializeField] private   PaletteManager    _paletteManager;
        [SerializeField] protected ThumbnailSettings _settings;

        #endregion

        #region Public Methods

        public static void ShowWindow( BrushSettings brush, int brushIdx )
        {
            _brush       = brush;
            _settingsIdx = brushIdx;
            if ( _instance == null )
            {
                _instance = brush is MultibrushSettings
                    ? GetWindow<ThumbnailEditorCommon>( true, "Thumbnail Editor" )
                    : GetWindow<SubThumbnailEditor>( true, "Thumbnail Editor" );
            }
            else
            {
                _instance.Initialize( brush );
            }

            _instance.Repaint();
        }

        public static void ThumbnailSettingsGUI( ThumbnailSettings settings )
        {
            EditorGUIUtility.labelWidth = 110;
            using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
            {
                settings.backgroudColor = EditorGUILayout.ColorField( "Background color:", settings.backgroudColor );
            }

            using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
            {
                settings.lightColor = EditorGUILayout.ColorField( "Light color:", settings.lightColor );
                settings.lightIntensity
                    = EditorGUILayout.Slider( "Light intensity:", settings.lightIntensity, 0.1f, 2 );
            }

            using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
            {
                settings.zoom        = EditorGUILayout.Slider( "Zoom:", settings.zoom, 0.5f, 10 );
                settings.targetEuler = EditorGUILayout.Vector3Field( "Rotation:", settings.targetEuler );
            }
        }

        #endregion

        #region Unity Functions

        protected virtual void OnEnable()
        {
            if ( _brush == null )
            {
                return;
            }

            _paletteManager = PaletteManager.instance;
            _thumbnail      = new Texture2D( ThumbnailUtils.SIZE, ThumbnailUtils.SIZE );
            _settings       = new ThumbnailSettings( _brush.thumbnailSettings );
            Initialize( _brush );
            Undo.undoRedoPerformed          += OnUndoPerformed;
            _nextBtnStyle                   =  new GUIStyle();
            _nextBtnStyle.normal.background =  Resources.Load<Texture2D>( "Sprites/Next" );
            _nextBtnStyle.fixedWidth        =  10;
            _nextBtnStyle.fixedHeight       =  38;
            _prevBtnStyle                   =  new GUIStyle( _nextBtnStyle );
            _prevBtnStyle.normal.background =  Resources.Load<Texture2D>( "Sprites/Prev" );
        }

        private void OnDisable() => Undo.undoRedoPerformed -= OnUndoPerformed;

        private void OnGUI()
        {
            if ( _brush == null )
            {
                Close();
                return;
            }

            EditorGUIUtility.wideMode = true;
            using ( new GUILayout.HorizontalScope( EditorStyles.helpBox ) )
            {
                GUILayout.FlexibleSpace();
                using ( new GUILayout.VerticalScope() )
                {
                    GUILayout.FlexibleSpace();
                    if ( GUILayout.Button( GUIContent.none, _prevBtnStyle ) )
                    {
                        ShowPrev();
                    }

                    GUILayout.FlexibleSpace();
                }

                GUILayout.Label( new GUIContent( _thumbnail ) );
                Rect rect = GUILayoutUtility.GetLastRect();
                using ( new GUILayout.VerticalScope() )
                {
                    GUILayout.FlexibleSpace();
                    if ( GUILayout.Button( GUIContent.none, _nextBtnStyle ) )
                    {
                        ShowNext();
                    }

                    GUILayout.FlexibleSpace();
                }

                PreviewMouseEvents( rect );
                GUILayout.FlexibleSpace();
            }

            SettingsGUI( _settings );
            Buttons();
        }

        #endregion

        #region Protected Fields

        protected Texture2D _thumbnail;

        #endregion

        #region Protected Methods

        protected virtual void Initialize( BrushSettings brush )
        {
            if ( _thumbnail == null )
            {
                _thumbnail = new Texture2D( ThumbnailUtils.SIZE, ThumbnailUtils.SIZE );
            }

            _settings = new ThumbnailSettings( brush.thumbnailSettings );
        }

        protected abstract void OnApply();
        protected abstract void OnSettingsChange();

        protected virtual void PreviewMouseEvents( Rect previeRect )
        {
            if ( !previeRect.Contains( Event.current.mousePosition ) )
            {
                return;
            }

            if ( Event.current.type      == EventType.MouseDrag
                 && Event.current.button == 1
                 && Event.current.delta  != Vector2.zero )
            {
                if ( !Event.current.control
                     && !Event.current.shift )
                {
                    Quaternion rot = Quaternion.Euler( _settings.targetEuler );
                    _settings.targetEuler = ( Quaternion.AngleAxis( Event.current.delta.y,   Vector3.left )
                                              * Quaternion.AngleAxis( Event.current.delta.x, Vector3.down )
                                              * rot ).eulerAngles;
                    OnSettingsChange();
                    Event.current.Use();
                }
                else if ( Event.current.control
                          && !Event.current.shift )
                {
                    Vector2 delta = Event.current.delta / 128;
                    delta.y = -delta.y;
                    _settings.targetOffset = Vector2.Min( Vector2.one,
                        Vector2.Max( _settings.targetOffset + delta, -Vector2.one ) );
                    OnSettingsChange();
                    Event.current.Use();
                }
                else if ( !Event.current.control
                          && Event.current.shift )
                {
                    Vector3 centerDir = ( previeRect.center - Event.current.mousePosition ) * 5 / 128;
                    centerDir.z = 5;
                    Quaternion rot   = Quaternion.LookRotation( centerDir );
                    Vector3    euler = rot.eulerAngles;
                    _settings.lightEuler = new Vector2( -euler.x, euler.y );
                    OnSettingsChange();
                    Event.current.Use();
                }
            }

            if ( Event.current.isScrollWheel )
            {
                float scrollSign = Mathf.Sign( Event.current.delta.y );
                _settings.zoom += scrollSign * 0.1f;
                OnSettingsChange();
                Event.current.Use();
            }
        }

        protected virtual void SettingsGUI( ThumbnailSettings settings )
        {
            using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
            {
                ThumbnailSettingsGUI( settings );
                if ( check.changed )
                {
                    OnSettingsChange();
                }
            }
        }

        protected abstract void ShowNext();
        protected abstract void ShowPrev();

        #endregion

        #region Private Fields

        private GUIStyle _nextBtnStyle;
        private GUIStyle _prevBtnStyle;

        #endregion

        #region Private Methods

        private void Buttons()
        {
            using ( new GUILayout.HorizontalScope( EditorStyles.helpBox ) )
            {
                GUILayout.FlexibleSpace();
                if ( GUILayout.Button( "Factory Reset" ) )
                {
                    _settings = new ThumbnailSettings();
                    OnSettingsChange();
                }

                if ( GUILayout.Button( "Cancel" ) )
                {
                    Close();
                }

                if ( GUILayout.Button( "Appy" ) )
                {
                    OnApply();
                }
            }
        }

        private void OnUndoPerformed()
        {
            Initialize( _brush );
            Repaint();
        }

        #endregion

    }

    public class ThumbnailEditorCommon : ThumbnailEditorWindow
    {

        #region Serialized

        [SerializeField]
        private List<SubThumbnailData> _subThumbnails
            = new List<SubThumbnailData>();

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            base.OnEnable();
            maxSize = minSize = new Vector2( 300, 414 );
        }

        #endregion

        #region Protected Methods

        protected override void Initialize( BrushSettings brush )
        {
            _brush = brush;
            MultibrushSettings brushSettings = brush as MultibrushSettings;
            if ( brushSettings == null )
            {
                Close();
                ShowWindow( brush, _settingsIdx );
                return;
            }

            base.Initialize( brush );
            _subThumbnails.Clear();
            MultibrushItemSettings[] brushItems = ( brush as MultibrushSettings ).items;
            foreach ( MultibrushItemSettings item in brushItems )
            {
                if ( item.prefab == null )
                {
                    continue;
                }

                SubThumbnailData subThumbnail = new SubThumbnailData();
                subThumbnail.multibrushItem = item;
                subThumbnail.settings       = new ThumbnailSettings( item.thumbnailSettings );
                subThumbnail.texture        = new Texture2D( ThumbnailUtils.SIZE, ThumbnailUtils.SIZE );
                subThumbnail.include        = item.includeInThumbnail;
                subThumbnail.prefab         = item.prefab;
                subThumbnail.overwrite      = item.overwriteSettings;
                _subThumbnails.Add( subThumbnail );

                ThumbnailUtils.UpdateThumbnail( subThumbnail.overwrite ? subThumbnail.settings : _settings,
                    subThumbnail.texture, subThumbnail.prefab, subThumbnail.multibrushItem.thumbnailPath, false );
            }

            Texture2D[] included = GetIncluded();
            ThumbnailUtils.UpdateThumbnail( _settings, _thumbnail, included, brushSettings.thumbnailPath, false );
        }

        protected override void OnApply()
        {
            Undo.RegisterCompleteObjectUndo( this, UNDO_CMD );
            foreach ( SubThumbnailData subThumbnail in _subThumbnails )
            {
                subThumbnail.multibrushItem.thumbnailSettings = subThumbnail.settings;
            }

            _brush.thumbnailSettings = _settings;
            ThumbnailUtils.UpdateThumbnail( _brush as MultibrushSettings, updateItemThumbnails: true, savePng: true );
            PaletteManager.selectedPalette.Save();
            PrefabPalette.instance.OnPaletteChange();
        }

        protected override void OnSettingsChange()
        {
            foreach ( SubThumbnailData subThumbnail in _subThumbnails )
            {
                ThumbnailUtils.UpdateThumbnail( subThumbnail.overwrite ? subThumbnail.settings : _settings,
                    subThumbnail.texture, subThumbnail.prefab, subThumbnail.multibrushItem.thumbnailPath, savePng: false );
            }

            Texture2D[] included = GetIncluded();
            ThumbnailUtils.UpdateThumbnail( _settings, _thumbnail, included, _brush.thumbnailPath, savePng: false );
        }

        protected override void ShowNext()
        {
            int brushCount = PaletteManager.selectedPalette.brushCount;
            _settingsIdx = ( _settingsIdx + 1 ) % brushCount;
            Initialize( PaletteManager.selectedPalette.GetBrush( _settingsIdx ) );
        }

        protected override void ShowPrev()
        {
            int brushCount = PaletteManager.selectedPalette.brushCount;
            _settingsIdx = ( _settingsIdx + brushCount - 1 ) % brushCount;
            Initialize( PaletteManager.selectedPalette.GetBrush( _settingsIdx ) );
        }

        #endregion

        #region Private Methods

        private Texture2D[] GetIncluded()
        {
            List<Texture2D> included = new List<Texture2D>();
            foreach ( SubThumbnailData item in _subThumbnails )
            {
                if ( !item.include )
                {
                    continue;
                }

                included.Add( item.texture );
            }

            return included.ToArray();
        }

        #endregion

        [Serializable]
        private class SubThumbnailData
        {

            #region Serialized

            public MultibrushItemSettings multibrushItem;
            public ThumbnailSettings      settings;
            public Texture2D              texture;
            public bool                   include = true;
            public GameObject             prefab;
            public bool                   overwrite;

            #endregion

        }
    }

    public class SubThumbnailEditor : ThumbnailEditorWindow
    {

        #region Serialized

        [SerializeField] private MultibrushItemSettings _itemClone;

        #endregion

        #region Unity Functions

        protected override void OnEnable()
        {
            base.OnEnable();
            maxSize = minSize = new Vector2( 300, 440 );
        }

        #endregion

        #region Protected Methods

        protected override void Initialize( BrushSettings brush )
        {
            _brush = brush;
            MultibrushItemSettings item     = brush as MultibrushItemSettings;
            bool                   nullItem = item == null;
            if ( !nullItem )
            {
                nullItem = item.prefab == null;
            }

            if ( nullItem )
            {
                Close();
                ShowWindow( brush, _settingsIdx );
                return;
            }

            _itemClone = item.Clone() as MultibrushItemSettings;
            base.Initialize( brush );
            ThumbnailUtils.UpdateThumbnail( _settings, _thumbnail, item.prefab, item.thumbnailPath, savePng: false );
        }

        protected override void OnApply()
        {
            Undo.RegisterCompleteObjectUndo( this, UNDO_CMD );
            _brush.thumbnailSettings = _settings;
            MultibrushItemSettings item = _brush as MultibrushItemSettings;
            item.overwriteThumbnailSettings = _itemClone.overwriteThumbnailSettings;
            ThumbnailUtils.UpdateThumbnail( item,                savePng: true );
            ThumbnailUtils.UpdateThumbnail( item.parentSettings, updateItemThumbnails: false, savePng: true );
            PaletteManager.selectedPalette.Save();
        }

        protected override void OnSettingsChange()
        {
            if ( _itemClone.prefab == null )
            {
                return;
            }

            ThumbnailUtils.UpdateThumbnail( _settings, _thumbnail,
                _itemClone.prefab, _itemClone.thumbnailPath, savePng: false );
        }

        protected override void PreviewMouseEvents( Rect previewRect )
        {
            if ( _itemClone.overwriteThumbnailSettings )
            {
                base.PreviewMouseEvents( previewRect );
            }
        }

        protected override void SettingsGUI( ThumbnailSettings settings )
        {
            using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
            {
                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    bool overwrite = EditorGUILayout.ToggleLeft( "Overwrite common settings",
                        _itemClone.overwriteThumbnailSettings );
                    if ( check.changed )
                    {
                        Undo.RegisterCompleteObjectUndo( this, UNDO_CMD );
                        _itemClone.overwriteThumbnailSettings = overwrite;
                        _settings.Copy( _itemClone.thumbnailSettings );
                        ThumbnailUtils.UpdateThumbnail( _settings, _thumbnail, _itemClone.prefab,
                            thumbnailPath: null, savePng: false );
                    }
                }
            }

            using ( new EditorGUI.DisabledGroupScope( !_itemClone.overwriteThumbnailSettings ) )
            {
                base.SettingsGUI( settings );
            }
        }

        protected override void ShowNext()
        {
            int itemCount = PaletteManager.selectedBrush.itemCount;
            _settingsIdx = ( _settingsIdx + 1 ) % itemCount;
            Initialize( PaletteManager.selectedBrush.GetItemAt( _settingsIdx ) );
        }

        protected override void ShowPrev()
        {
            int itemCount = PaletteManager.selectedBrush.itemCount;
            _settingsIdx = ( _settingsIdx + itemCount - 1 ) % itemCount;
            Initialize( PaletteManager.selectedBrush.GetItemAt( _settingsIdx ) );
        }

        #endregion

    }
}