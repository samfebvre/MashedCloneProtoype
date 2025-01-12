using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace PluginMaster
{
    public class PrefabPalette : EditorWindow, ISerializationCallbackReceiver
    {

        #region COMMON

        private GUISkin _skin;

        [SerializeField] private PaletteManager _paletteManager;
        private                  bool           _loadFromFile;
        private                  bool           _undoRegistered;

        public static PrefabPalette instance { get; private set; }

        [MenuItem( "Tools/Plugin Master/Prefab World Builder/Palette...", false, 1110 )]
        public static void ShowWindow() => instance = GetWindow<PrefabPalette>( "Palette" );

        private static bool _repaint;

        public static void RepainWindow()
        {
            if ( instance != null )
            {
                instance.Repaint();
            }

            _repaint = true;
        }

        public static void OnChangeRepaint()
        {
            if ( instance != null )
            {
                instance.OnPaletteChange();
                RepainWindow();
            }
        }

        public static void CloseWindow()
        {
            if ( instance != null )
            {
                instance.Close();
            }
        }

        private void OnEnable()
        {
            instance        = this;
            _paletteManager = PaletteManager.instance;
            _skin           = Resources.Load<GUISkin>( "PWBSkin" );
            if ( _skin == null )
            {
                CloseWindow();
                return;
            }

            _toggleStyle        = _skin.GetStyle( "PaletteToggle" );
            _loadingIcon        = Resources.Load<Texture2D>( "Sprites/Loading" );
            _toggleStyle.margin = new RectOffset( 4, 4, 4, 4 );
            _dropdownIcon       = new GUIContent( Resources.Load<Texture2D>( "Sprites/DropdownArrow" ) );
            _labelIcon          = new GUIContent( Resources.Load<Texture2D>( "Sprites/Label" ), "Filter by label" );
            _selectionFilterIcon = new GUIContent( Resources.Load<Texture2D>( "Sprites/SelectionFilter" ),
                "Filter by selection" );
            _newBrushIcon            = new GUIContent( Resources.Load<Texture2D>( "Sprites/New" ),    "New Brush" );
            _deleteBrushIcon         = new GUIContent( Resources.Load<Texture2D>( "Sprites/Delete" ), "Delete Brush" );
            _pickerIcon              = new GUIContent( Resources.Load<Texture2D>( "Sprites/Picker" ), "Brush Picker" );
            _clearFilterIcon         = new GUIContent( Resources.Load<Texture2D>( "Sprites/Clear" ) );
            _settingsIcon            = new GUIContent( Resources.Load<Texture2D>( "Sprites/Settings" ) );
            _cursorStyle             = _skin.GetStyle( "Cursor" );
            _visibleTabCount         = PaletteManager.paletteNames.Length;
            autoRepaintOnSceneChange = true;
            UpdateLabelFilter();
            UpdateFilteredList( false );
            PaletteManager.ClearSelection( false );
            Undo.undoRedoPerformed += OnPaletteChange;
            AutoSave.QuickSave();
            string[] paletteFiles = Directory.GetFiles( PWBData.palettesDirectory, "*.txt",
                SearchOption.AllDirectories );
            if ( paletteFiles.Length == 0 )
            {
                PaletteManager.instance.LoadPaletteFiles( true );
            }
        }

        private void OnDisable() => Undo.undoRedoPerformed -= OnPaletteChange;

        private void OnDestroy() => ToolManager.OnPaletteClosed();

        public static void ClearUndo()
        {
            if ( instance == null )
            {
                return;
            }

            Undo.ClearUndo( instance );
        }

        private void OnGUI()
        {
            if ( PWBCore.refreshDatabase )
            {
                PWBCore.AssetDatabaseRefresh();
            }

            if ( _skin == null )
            {
                Close();
                return;
            }

            if ( _loadFromFile && Event.current.type == EventType.Repaint )
            {
                _loadFromFile = false;
                if ( !PWBCore.staticData.saving )
                {
                    PWBCore.LoadFromFile();
                }

                UpdateFilteredList( false );
                return;
            }

            if ( _contextBrushAdded )
            {
                RegisterUndo( "Add Brush" );
                PaletteManager.selectedPalette.AddBrush( _newContextBrush );
                _newContextBrush                = null;
                PaletteManager.selectedBrushIdx = PaletteManager.selectedPalette.brushes.Length - 1;
                _contextBrushAdded              = false;
                OnPaletteChange();
                return;
            }

            try
            {
                TabBar();
                if ( PaletteManager.paletteData.Length == 0 )
                {
                    return;
                }

                SearchBar();
                Palette();
            }
            catch
            {
                RepainWindow();
            }

            EventType eventType = Event.current.rawType;
            if ( eventType    == EventType.MouseMove
                 || eventType == EventType.MouseUp )
            {
                _moveBrush.to = -1;
                draggingBrush = false;
                _showCursor   = false;
            }
            else if ( PWBSettings.shortcuts.paletteDeleteBrush.Check() )
            {
                OnDelete();
            }
        }

        private void Update()
        {
            if ( mouseOverWindow != this )
            {
                _moveBrush.to = -1;
                _showCursor   = false;
            }
            else if ( draggingBrush )
            {
                _showCursor = true;
            }

            if ( _repaint )
            {
                _repaint = false;
                Repaint();
            }

            if ( _frameSelectedBrush && _newSelectedPositionSet )
            {
                DoFrameSelectedBrush();
            }

            if ( PaletteManager.savePending )
            {
                PaletteManager.SaveIfPending();
            }
        }

        private void RegisterUndo( string name )
        {
            _undoRegistered = true;
            if ( PWBCore.staticData.undoPalette )
            {
                Undo.RegisterCompleteObjectUndo( this, name );
            }
        }

        public void OnBeforeSerialize()
        {
        }

        public void OnAfterDeserialize()
        {
            _repaint = true;
            if ( !_undoRegistered )
            {
                _loadFromFile = true;
            }

            PaletteManager.ClearSelection( false );
        }

        public void UpdateAllThumbnails() => PaletteManager.UpdateAllThumbnails();

        #endregion

        #region PALETTE

        private       Vector2  _scrollPosition;
        private       Rect     _scrollViewRect;
        private       Vector2  _prevSize;
        private       int      _columnCount = 1;
        private       GUIStyle _toggleStyle;
        private const int      MIN_ICON_SIZE     = 24;
        private const int      MAX_ICON_SIZE     = 256;
        public const  int      DEFAULT_ICON_SIZE = 64;
        private       int      _prevIconSize     = DEFAULT_ICON_SIZE;

        private GUIContent                       _dropdownIcon;
        private bool                             _draggingBrush;
        private bool                             _showCursor;
        private Rect                             _cursorRect;
        private GUIStyle                         _cursorStyle;
        private (int from, int to, bool perform) _moveBrush = ( 0, 0, false );

        private bool draggingBrush
        {
            get => _draggingBrush;
            set
            {
                _draggingBrush             = value;
                wantsMouseMove             = value;
                wantsMouseEnterLeaveWindow = value;
            }
        }

        private void Palette()
        {
            UpdateColumnCount();

            _prevIconSize = PaletteManager.iconSize;

            if ( _moveBrush.perform )
            {
                RegisterUndo( "Change Brush Order" );
                int[] selection = PaletteManager.idxSelection;
                PaletteManager.selectedPalette.Swap( _moveBrush.from, _moveBrush.to, ref selection );
                PaletteManager.idxSelection = selection;
                if ( selection.Length == 1 )
                {
                    PaletteManager.selectedBrushIdx = selection[ 0 ];
                }

                _moveBrush.perform = false;
                UpdateFilteredList( false );
            }

            BrushInputData toggleData = null;

            using ( EditorGUILayout.ScrollViewScope scrollView = new EditorGUILayout.ScrollViewScope( _scrollPosition, false, false,
                       GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, _skin.box ) )
            {
                _scrollPosition = scrollView.scrollPosition;
                Brushes( ref toggleData );
                if ( _showCursor )
                {
                    GUI.Box( _cursorRect, string.Empty, _cursorStyle );
                }
            }

            _scrollViewRect = GUILayoutUtility.GetLastRect();
            if ( PaletteManager.selectedPalette.brushCount == 0 )
            {
                DropBox();
            }

            Bottom();

            BrushMouseEventHandler( toggleData );
            PaletteContext();
            DropPrefab();
        }

        private void UpdateColumnCount()
        {
            if ( PaletteManager.paletteCount == 0 )
            {
                return;
            }

            PaletteData          paletteData = PaletteManager.selectedPalette;
            MultibrushSettings[] brushes     = paletteData.brushes;
            if ( _scrollViewRect.width > MIN_ICON_SIZE )
            {
                if ( _prevSize        != position.size
                     || _prevIconSize != PaletteManager.iconSize
                     || _repaint )
                {
                    float iconW = (float)( ( PaletteManager.iconSize + 4 ) * brushes.Length + 6 ) / brushes.Length;
                    _columnCount = Mathf.Max( (int)( _scrollViewRect.width / iconW ), 1 );
                    int rowCount = Mathf.CeilToInt( (float)brushes.Length  / _columnCount );
                    int h        = rowCount * ( PaletteManager.iconSize + 4 ) + 42;

                    if ( h > _scrollViewRect.height )
                    {
                        iconW        = (float)( ( PaletteManager.iconSize + 4 ) * brushes.Length + 17 ) / brushes.Length;
                        _columnCount = Mathf.Max( (int)( _scrollViewRect.width / iconW ), 1 );
                    }
                }

                _prevSize = position.size;
            }
        }

        public void OnPaletteChange()
        {
            UpdateLabelFilter();
            UpdateFilteredList( false );
            _repaint = true;
            UpdateColumnCount();
            Repaint();
        }

        #endregion

        #region BOTTOM

        private GUIContent _newBrushIcon;
        private GUIContent _deleteBrushIcon;
        private GUIContent _pickerIcon;
        private GUIContent _settingsIcon;

        private void Bottom()
        {
            using ( new GUILayout.HorizontalScope( EditorStyles.toolbar, GUILayout.Height( 18 ) ) )
            {
                if ( PaletteManager.selectedPalette.brushCount > 0 )
                {
                    GUIStyle sliderStyle = new GUIStyle( GUI.skin.horizontalSlider );
                    sliderStyle.margin.top = 0;
                    PaletteManager.iconSize = (int)GUILayout.HorizontalSlider(
                        PaletteManager.iconSize,
                        MIN_ICON_SIZE,
                        MAX_ICON_SIZE,
                        sliderStyle,
                        GUI.skin.horizontalSliderThumb,
                        GUILayout.MaxWidth( 128 ) );
                }

                GUILayout.FlexibleSpace();
                if ( GUILayout.Button( _newBrushIcon, EditorStyles.toolbarButton ) )
                {
                    PaletteContextMenu();
                }

                using ( new EditorGUI.DisabledGroupScope( PaletteManager.selectionCount == 0 ) )
                {
                    if ( GUILayout.Button( _deleteBrushIcon, EditorStyles.toolbarButton ) )
                    {
                        OnDelete();
                    }
                }

                PaletteManager.pickingBrushes = GUILayout.Toggle( PaletteManager.pickingBrushes,
                    _pickerIcon, EditorStyles.toolbarButton );
                if ( GUILayout.Button( _settingsIcon, EditorStyles.toolbarButton ) )
                {
                    SettingsContextMenu();
                }
            }

            Rect rect = GUILayoutUtility.GetLastRect();
            if ( rect.Contains( Event.current.mousePosition ) )
            {
                if ( Event.current.type    == EventType.MouseDown
                     || Event.current.type == EventType.DragUpdated
                     || Event.current.type == EventType.MouseDrag
                     || Event.current.type == EventType.DragPerform )
                {
                    Event.current.Use();
                }
            }
        }

        private void OnDelete()
        {
            RegisterUndo( "Delete Brush" );
            DeleteBrushSelection();
            PaletteManager.ClearSelection();
            OnPaletteChange();
        }

        public void Reload( bool clearSelection )
        {
            if ( PaletteManager.selectedPaletteIdx >= PaletteManager.paletteCount )
            {
                PaletteManager.selectedPaletteIdx = 0;
            }

            if ( clearSelection )
            {
                PaletteManager.ClearSelection();
                _lastVisibleIdx = PaletteManager.paletteCount - 1;
            }

            _updateTabBar = true;
            OnPaletteChange();
        }

        private void SettingsContextMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem( new GUIContent( PaletteManager.viewList ? "Grid View" : "List View" ), false,
                () => PaletteManager.viewList = !PaletteManager.viewList );
            if ( !PaletteManager.viewList )
            {
                menu.AddItem( new GUIContent( "Show Brush Name" ), PaletteManager.showBrushName,
                    () => PaletteManager.showBrushName = !PaletteManager.showBrushName );
            }

            if ( PaletteManager.selectedPalette.brushCount > 1 )
            {
                menu.AddItem( new GUIContent( "Ascending Sort" ), false,
                    () => { PaletteManager.selectedPalette.AscendingSort(); } );
                menu.AddItem( new GUIContent( "Descending Sort" ), false,
                    () => { PaletteManager.selectedPalette.DescendingSort(); } );
            }

            menu.AddSeparator( string.Empty );
            menu.AddItem( new GUIContent( "Rename palette..." ), false, ShowRenamePaletteWindow,
                new RenameData( PaletteManager.selectedPaletteIdx, PaletteManager.selectedPalette.name,
                    position.position + Event.current.mousePosition ) );
            menu.AddItem( new GUIContent( "Delete palette" ), false, ShowDeleteConfirmation,
                PaletteManager.selectedPaletteIdx );
            menu.AddItem( new GUIContent( "Cleanup palette" ), false, () =>
            {
                PaletteManager.Cleanup();
                OnPaletteChange();
                UpdateTabBar();
                Repaint();
            } );
            menu.AddSeparator( string.Empty );
            menu.AddItem( new GUIContent( "Update all thumbnails" ), false, UpdateAllThumbnails );
            menu.AddSeparator( string.Empty );
            menu.AddItem( new GUIContent( "Brush creation settings..." ), false,
                BrushCreationSettingsWindow.ShowWindow );
            menu.ShowAsContext();
        }

        #endregion

        #region BRUSHES

        private Vector3   _selectedBrushPosition = Vector3.zero;
        private bool      _frameSelectedBrush;
        private bool      _newSelectedPositionSet;
        private Texture2D _loadingIcon;

        public void FrameSelectedBrush()
        {
            _frameSelectedBrush     = true;
            _newSelectedPositionSet = false;
        }

        private void DoFrameSelectedBrush()
        {
            _frameSelectedBrush = false;
            if ( _scrollPosition.y                             > _selectedBrushPosition.y
                 || _scrollPosition.y + _scrollViewRect.height < _selectedBrushPosition.y )
            {
                _scrollPosition.y = _selectedBrushPosition.y - 4;
            }

            RepainWindow();
        }

        private void Brushes( ref BrushInputData toggleData )
        {
            if ( Event.current.control
                 && Event.current.keyCode    == KeyCode.A
                 && _filteredBrushList.Count > 0 )
            {
                PaletteManager.ClearSelection();
                foreach ( FilteredBrush brush in _filteredBrushList )
                {
                    PaletteManager.AddToSelection( brush.index );
                }

                PaletteManager.selectedBrushIdx = _filteredBrushList[ 0 ].index;
                Repaint();
            }

            if ( PaletteManager.selectedPalette.brushCount == 0 )
            {
                return;
            }

            if ( filteredBrushListCount == 0 )
            {
                return;
            }

            FilteredBrush[] filteredBrushes = filteredBrushList.ToArray();
            int             filterBrushIdx  = 0;

            GUIStyle nameStyle = GUIStyle.none;
            nameStyle.margin           = new RectOffset( 2, 2, 0, 1 );
            nameStyle.clipping         = TextClipping.Clip;
            nameStyle.fontSize         = 8;
            nameStyle.normal.textColor = Color.white;

            MultibrushSettings brushSettings = null;
            int                brushIdx      = -1;
            Texture2D          icon          = null;

            void GetBrushSettings( ref GUIStyle style )
            {
                brushSettings = filteredBrushes[ filterBrushIdx ].brush;
                brushIdx      = filteredBrushes[ filterBrushIdx ].index;
                if ( PaletteManager.SelectionContains( brushIdx ) )
                {
                    style.normal = _toggleStyle.onNormal;
                }

                icon = brushSettings.thumbnail;
                if ( icon == null )
                {
                    icon = _loadingIcon;
                }
            }

            void GetInputData( ref BrushInputData inputData )
            {
                Rect rect = GUILayoutUtility.GetLastRect();
                if ( rect.Contains( Event.current.mousePosition ) )
                {
                    inputData = new BrushInputData( brushIdx, rect, Event.current.type,
                        Event.current.control, Event.current.shift, Event.current.mousePosition.x );
                }

                if ( Event.current.type                 != EventType.Layout
                     && PaletteManager.selectedBrushIdx == brushIdx )
                {
                    _selectedBrushPosition  = rect.position;
                    _newSelectedPositionSet = true;
                }
            }

            void GridViewRow( ref BrushInputData inputData )
            {
                using ( new GUILayout.HorizontalScope() )
                {
                    for ( int col = 0; col < _columnCount && filterBrushIdx < filteredBrushes.Length; ++col )
                    {
                        GUIStyle style = new GUIStyle( _toggleStyle );
                        GetBrushSettings( ref style );
                        using ( new GUILayout.VerticalScope( style ) )
                        {
                            if ( PaletteManager.showBrushName )
                            {
                                GUILayout.Box( new GUIContent( brushSettings.name, brushSettings.name ),
                                    nameStyle, GUILayout.Width( PaletteManager.iconSize ) );
                            }

                            GUILayout.Box( new GUIContent( icon, brushSettings.name ), GUIStyle.none,
                                GUILayout.Width( PaletteManager.iconSize ),
                                GUILayout.Height( PaletteManager.iconSize ) );
                        }

                        GetInputData( ref inputData );
                        ++filterBrushIdx;
                    }

                    GUILayout.FlexibleSpace();
                }
            }

            void ListView( ref BrushInputData inputData )
            {
                GUIStyle style = new GUIStyle( _toggleStyle );
                style.padding = new RectOffset( 0, 0, 0, 0 );
                GetBrushSettings( ref style );
                using ( new GUILayout.HorizontalScope( style ) )
                {
                    GUILayout.Box( new GUIContent( icon, brushSettings.name ), GUIStyle.none,
                        GUILayout.Width( PaletteManager.iconSize ),
                        GUILayout.Height( PaletteManager.iconSize ) );
                    GUILayout.Space( 4 );
                    using ( new GUILayout.VerticalScope() )
                    {
                        int span = ( PaletteManager.iconSize - 16 ) / 2;
                        GUILayout.Space( span );
                        GUILayout.Box( new GUIContent( brushSettings.name, brushSettings.name ), nameStyle );
                        GUILayout.Space( span );
                    }
                }

                GetInputData( ref inputData );
                ++filterBrushIdx;
            }

            nameStyle.fontSize = PaletteManager.viewList ? 12 : 8;
            nameStyle.fontSize = Mathf.Max( Mathf.RoundToInt( nameStyle.fontSize
                                                              * ( PaletteManager.iconSize / (float)DEFAULT_ICON_SIZE ) ), nameStyle.fontSize );

            while ( filterBrushIdx < filteredBrushes.Length )
            {
                if ( PaletteManager.viewList )
                {
                    ListView( ref toggleData );
                }
                else
                {
                    GridViewRow( ref toggleData );
                }
            }
        }

        public void DeselectAllButThis( int index )
        {
            if ( PaletteManager.selectedBrushIdx  == index
                 && PaletteManager.selectionCount == 1 )
            {
                return;
            }

            PaletteManager.ClearSelection();
            if ( index < 0 )
            {
                return;
            }

            PaletteManager.AddToSelection( index );
            PaletteManager.selectedBrushIdx = index;
        }

        private void DeleteBrushSelection()
        {
            int[] descendingSelection = PaletteManager.idxSelection;
            Array.Sort( descendingSelection, ( i1, i2 ) => i2.CompareTo( i1 ) );
            foreach ( int i in descendingSelection )
            {
                PaletteManager.selectedPalette.RemoveBrushAt( i );
            }
        }

        private void DeleteBrush( object idx )
        {
            RegisterUndo( "Delete Brush" );
            if ( PaletteManager.SelectionContains( (int)idx ) )
            {
                DeleteBrushSelection();
            }
            else
            {
                PaletteManager.selectedPalette.RemoveBrushAt( (int)idx );
            }

            PaletteManager.ClearSelection();
            OnPaletteChange();
        }

        private void CopyBrushSettings( object idx )
            => PaletteManager.clipboardSetting = PaletteManager.selectedPalette.brushes[ (int)idx ].CloneMainSettings();

        private void PasteBrushSettings( object idx )
        {
            RegisterUndo( "Paste Brush Settings" );
            PaletteManager.selectedPalette.brushes[ (int)idx ].Copy( PaletteManager.clipboardSetting );
            if ( BrushProperties.instance != null )
            {
                BrushProperties.instance.Repaint();
            }

            PaletteManager.selectedPalette.Save();
        }

        private void DuplicateBrush( object idx )
        {
            RegisterUndo( "Duplicate Brush" );
            if ( PaletteManager.SelectionContains( (int)idx ) )
            {
                int[] descendingSelection = PaletteManager.idxSelection;
                Array.Sort( descendingSelection, ( i1, i2 ) => i2.CompareTo( i1 ) );
                for ( int i = 0; i < descendingSelection.Length; ++i )
                {
                    PaletteManager.selectedPalette.DuplicateBrush( descendingSelection[ i ] );
                    descendingSelection[ i ] += descendingSelection.Length - 1 - i;
                }

                PaletteManager.idxSelection = descendingSelection;
            }
            else
            {
                PaletteManager.selectedPalette.DuplicateBrush( (int)idx );
            }

            OnPaletteChange();
        }

        private void MergeBrushes()
        {
            RegisterUndo( "Merge Brushes" );
            List<int> selection = new List<int>( PaletteManager.idxSelection );
            selection.Sort();
            int resultIdx = selection[ 0 ];
            int lastIdx   = selection.Last()                                             + 1;
            PaletteManager.selectedPalette.DuplicateBrushAt( resultIdx, selection.Last() + 1 );
            resultIdx = lastIdx;
            MultibrushSettings     result    = PaletteManager.selectedPalette.GetBrush( resultIdx );
            MultibrushItemSettings firstItem = result.GetItemAt( 0 );
            if ( !firstItem.overwriteSettings )
            {
                firstItem.Copy( result );
            }

            firstItem.overwriteSettings =  true;
            result.name                 += "_merged";

            selection.RemoveAt( 0 );
            for ( int i = 0; i < selection.Count; ++i )
            {
                int                      idx        = selection[ i ];
                MultibrushSettings       other      = PaletteManager.selectedPalette.GetBrush( idx );
                MultibrushItemSettings[] otherItems = other.items;
                foreach ( MultibrushItemSettings item in otherItems )
                {
                    MultibrushItemSettings clone = new MultibrushItemSettings( item.prefab, result );
                    if ( item.overwriteSettings )
                    {
                        clone.Copy( item );
                    }
                    else
                    {
                        clone.Copy( other );
                    }

                    clone.overwriteSettings = true;
                    result.AddItem( clone );
                }
            }

            result.Reset();
            PaletteManager.ClearSelection();
            PaletteManager.AddToSelection( resultIdx );
            PaletteManager.selectedBrushIdx = resultIdx;
            OnPaletteChange();
        }

        private void OnMergeBrushesContext()
        {
            RegisterUndo( "Merge Brushes" );
            List<int> selection = new List<int>( PaletteManager.idxSelection );
            selection.Sort();
            int resultIdx = selection[ 0 ];
            selection.RemoveAt( 0 );
            selection.Reverse();
            MultibrushSettings result = PaletteManager.selectedPalette.GetBrush( resultIdx );
            for ( int i = 0; i < selection.Count; ++i )
            {
                int                      idx        = selection[ i ];
                MultibrushSettings       other      = PaletteManager.selectedPalette.GetBrush( idx );
                MultibrushItemSettings[] otherItems = other.items;
                foreach ( MultibrushItemSettings item in otherItems )
                {
                    MultibrushItemSettings clone = item.Clone() as MultibrushItemSettings;
                    clone.parentSettings = result;
                    result.AddItem( clone );
                }

                PaletteManager.selectedPalette.RemoveBrushAt( idx );
            }

            PaletteManager.ClearSelection();
            PaletteManager.AddToSelection( resultIdx );
            PaletteManager.selectedBrushIdx = resultIdx;
            OnPaletteChange();
        }

        private void SelectPrefabs( object idx )
        {
            List<GameObject> prefabs = new List<GameObject>();
            if ( PaletteManager.SelectionContains( (int)idx ) )
            {
                foreach ( int selectedIdx in PaletteManager.idxSelection )
                {
                    MultibrushSettings brush = PaletteManager.selectedPalette.GetBrush( selectedIdx );
                    foreach ( MultibrushItemSettings item in brush.items )
                    {
                        if ( item.prefab != null )
                        {
                            prefabs.Add( item.prefab );
                        }
                    }
                }
            }
            else
            {
                MultibrushSettings brush = PaletteManager.selectedPalette.GetBrush( (int)idx );
                foreach ( MultibrushItemSettings item in brush.items )
                {
                    if ( item.prefab != null )
                    {
                        prefabs.Add( item.prefab );
                    }
                }
            }

            Selection.objects = prefabs.ToArray();
        }

        private void OpenPrefab( object idx )
            => AssetDatabase.OpenAsset( PaletteManager.selectedPalette.GetBrush( (int)idx ).items[ 0 ].prefab );

        private void SelectReferences( object idx )
        {
            MultibrushItemSettings[] items          = PaletteManager.selectedPalette.GetBrush( (int)idx ).items;
            List<int>                itemsprefabIds = new List<int>();
            foreach ( MultibrushItemSettings item in items )
            {
                if ( item.prefab != null )
                {
                    itemsprefabIds.Add( item.prefab.GetInstanceID() );
                }
            }

            List<GameObject> selection = new List<GameObject>();
            #if UNITY_2022_2_OR_NEWER
            Transform[] objects = FindObjectsByType<Transform>( FindObjectsSortMode.None );
            #else
            var objects = GameObject.FindObjectsOfType<Transform>();
            #endif
            foreach ( Transform obj in objects )
            {
                Transform source = PrefabUtility.GetCorrespondingObjectFromSource( obj );
                if ( source == null )
                {
                    continue;
                }

                int sourceIdx = source.gameObject.GetInstanceID();
                if ( itemsprefabIds.Contains( sourceIdx ) )
                {
                    selection.Add( obj.gameObject );
                }
            }

            Selection.objects = selection.ToArray();
        }

        private void UpdateThumbnail( object idx ) => PaletteManager.UpdateSelectedThumbnails();

        private void EditThumbnail( object idx )
        {
            int                brushIdx = (int)idx;
            MultibrushSettings brush    = PaletteManager.selectedPalette.GetBrush( brushIdx );
            ThumbnailEditorWindow.ShowWindow( brush, brushIdx );
        }

        private void CopyThumbnailSettings( object idx )
        {
            MultibrushSettings brush = PaletteManager.selectedPalette.brushes[ (int)idx ];
            PaletteManager.clipboardThumbnailSettings          = brush.thumbnailSettings.Clone();
            PaletteManager.clipboardOverwriteThumbnailSettings = PaletteManager.Trit.SAME;
        }

        private void PasteThumbnailSettings( object idx )
        {
            if ( PaletteManager.clipboardThumbnailSettings == null )
            {
                return;
            }

            RegisterUndo( "Paste Thumbnail Settings" );

            void Paste( MultibrushSettings brush )
            {
                brush.thumbnailSettings.Copy( PaletteManager.clipboardThumbnailSettings );
                ThumbnailUtils.UpdateThumbnail( brushSettings: brush, updateItemThumbnails: true, savePng: true );
            }

            if ( PaletteManager.SelectionContains( (int)idx ) )
            {
                foreach ( int i in PaletteManager.idxSelection )
                {
                    Paste( PaletteManager.selectedPalette.brushes[ i ] );
                }
            }
            else
            {
                Paste( PaletteManager.selectedPalette.brushes[ (int)idx ] );
            }

            PaletteManager.selectedPalette.Save();
        }

        private void BrushContext( int idx )
        {
            GenericMenu        menu  = new GenericMenu();
            MultibrushSettings brush = PaletteManager.selectedPalette.GetBrush( idx );
            menu.AddItem( new GUIContent( "Select Prefab"
                                          + ( PaletteManager.selectionCount > 1
                                              || brush.itemCount            > 1
                                              ? "s"
                                              : "" ) ), false, SelectPrefabs, idx );
            if ( brush.itemCount == 1 )
            {
                menu.AddItem( new GUIContent( "Open Prefab" ), false, OpenPrefab, idx );
            }

            menu.AddItem( new GUIContent( "Select References In Scene" ), false, SelectReferences, idx );
            menu.AddSeparator( string.Empty );
            menu.AddItem( new GUIContent( "Update Thumbnail" ),        false, UpdateThumbnail,       idx );
            menu.AddItem( new GUIContent( "Edit Thumbnail" ),          false, EditThumbnail,         idx );
            menu.AddItem( new GUIContent( "Copy Thumbnail Settings" ), false, CopyThumbnailSettings, idx );
            if ( PaletteManager.clipboardThumbnailSettings != null )
            {
                menu.AddItem( new GUIContent( "Paste Thumbnail Settings" ), false, PasteThumbnailSettings, idx );
            }

            menu.AddSeparator( string.Empty );
            menu.AddItem( new GUIContent( "Delete" ),    false, DeleteBrush,    idx );
            menu.AddItem( new GUIContent( "Duplicate" ), false, DuplicateBrush, idx );
            if ( PaletteManager.selectionCount > 1 )
            {
                menu.AddItem( new GUIContent( "Merge" ), false, OnMergeBrushesContext );
            }

            if ( PaletteManager.selectionCount == 1 )
            {
                menu.AddItem( new GUIContent( "Copy Brush Settings" ), false, CopyBrushSettings, idx );
            }

            if ( PaletteManager.clipboardSetting != null )
            {
                menu.AddItem( new GUIContent( "Paste Brush Settings" ), false, PasteBrushSettings, idx );
            }

            menu.AddSeparator( string.Empty );
            PaletteContextAddMenuItems( menu );
            menu.ShowAsContext();
        }

        private void BrushMouseEventHandler( BrushInputData data )
        {
            void DeselectAllButCurrent()
            {
                PaletteManager.ClearSelection();
                PaletteManager.selectedBrushIdx = data.index;
                PaletteManager.AddToSelection( data.index );
            }

            if ( data == null )
            {
                return;
            }

            if ( data.eventType == EventType.MouseMove )
            {
                Event.current.Use();
            }

            if ( data.eventType          == EventType.MouseDown
                 && Event.current.button == 0 )
            {
                void DeselectAll()
                {
                    PaletteManager.ClearSelection();
                }

                void ToggleCurrent()
                {
                    if ( PaletteManager.SelectionContains( data.index ) )
                    {
                        PaletteManager.RemoveFromSelection( data.index );
                    }
                    else
                    {
                        PaletteManager.AddToSelection( data.index );
                    }

                    PaletteManager.selectedBrushIdx = PaletteManager.selectionCount == 1
                        ? PaletteManager.idxSelection[ 0 ]
                        : -1;
                }

                if ( data.shift )
                {
                    int selectedIdx = PaletteManager.selectedBrushIdx;
                    int sign        = (int)Mathf.Sign( data.index - selectedIdx );
                    if ( sign != 0 )
                    {
                        PaletteManager.ClearSelection();
                        for ( int i = selectedIdx; i != data.index; i += sign )
                        {
                            if ( FilteredListContains( i ) )
                            {
                                PaletteManager.AddToSelection( i );
                            }
                        }

                        PaletteManager.AddToSelection( data.index );
                        PaletteManager.selectedBrushIdx = selectedIdx;
                    }
                    else
                    {
                        DeselectAllButCurrent();
                    }
                }
                else
                {
                    if ( data.control
                         && PaletteManager.selectionCount < 2 )
                    {
                        if ( PaletteManager.selectedBrushIdx == data.index )
                        {
                            DeselectAll();
                        }
                        else
                        {
                            ToggleCurrent();
                        }
                    }
                    else if ( data.control
                              && PaletteManager.selectionCount > 1 )
                    {
                        ToggleCurrent();
                    }
                    else if ( !data.control
                              && PaletteManager.selectionCount < 2 )
                    {
                        if ( PaletteManager.selectedBrushIdx == data.index )
                        {
                            DeselectAll();
                        }
                        else
                        {
                            DeselectAllButCurrent();
                        }
                    }
                    else if ( !data.control
                              && PaletteManager.selectionCount > 1 )
                    {
                        DeselectAllButCurrent();
                    }
                }

                Event.current.Use();
                Repaint();
            }
            else if ( data.eventType == EventType.ContextClick )
            {
                BrushContext( data.index );
                Event.current.Use();
            }
            else if ( Event.current.type == EventType.MouseDrag )
            {
                if ( !PaletteManager.SelectionContains( data.index ) )
                {
                    DeselectAllButCurrent();
                }

                DragAndDrop.PrepareStartDrag();
                if ( Event.current.control )
                {
                    DragAndDrop.StartDrag( "Dragging brush" );
                    DragAndDrop.objectReferences = new Object[] { PaletteManager.selectedBrush.GetItemAt( 0 ).prefab };
                    DragAndDrop.visualMode       = DragAndDropVisualMode.Move;
                }
                else
                {
                    PWBIO.sceneDragReceiver.brushId = data.index;
                    SceneDragAndDrop.StartDrag( PWBIO.sceneDragReceiver, "Dragging brush" );
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                }

                draggingBrush      = true;
                _moveBrush.from    = data.index;
                _moveBrush.perform = false;
                _moveBrush.to      = -1;
            }
            else if ( data.eventType == EventType.DragUpdated )
            {
                if ( Event.current.control )
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Move;
                }
                else
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    Vector2 size       = new Vector2( 4, PaletteManager.iconSize );
                    Vector2 min        = data.rect.min;
                    bool    toTheRight = data.mouseX - data.rect.center.x > 0;
                    min.x         = toTheRight ? data.rect.max.x : min.x - size.x;
                    _cursorRect   = new Rect( min, size );
                    _showCursor   = true;
                    _moveBrush.to = data.index;
                    if ( toTheRight )
                    {
                        ++_moveBrush.to;
                    }
                }
            }
            else if ( data.eventType == EventType.DragPerform
                      && !Event.current.control )
            {
                _moveBrush.to = data.index;
                bool toTheRight = data.mouseX - data.rect.center.x > 0;
                if ( toTheRight )
                {
                    ++_moveBrush.to;
                }

                if ( draggingBrush )
                {
                    _moveBrush.perform = _moveBrush.from != _moveBrush.to;
                    draggingBrush      = false;
                }

                _showCursor = false;
            }
            else if ( data.eventType == EventType.DragExited
                      && !Event.current.control )
            {
                _showCursor   = false;
                draggingBrush = false;
                _moveBrush.to = -1;
            }
        }

        #endregion

        #region PALETTE CONTEXT

        private int                _currentPickerId = -1;
        private bool               _contextBrushAdded;
        private MultibrushSettings _newContextBrush;

        private void PaletteContext()
        {
            if ( _scrollViewRect.Contains( Event.current.mousePosition ) )
            {
                if ( Event.current.type == EventType.ContextClick )
                {
                    PaletteContextMenu();
                    Event.current.Use();
                }
                else if ( Event.current.type      == EventType.MouseDown
                          && Event.current.button == 0 )
                {
                    PaletteManager.ClearSelection();
                    Repaint();
                }
            }

            if ( Event.current.commandName                      == "ObjectSelectorClosed"
                 && EditorGUIUtility.GetObjectPickerControlID() == _currentPickerId )
            {
                Object obj = EditorGUIUtility.GetObjectPickerObject();
                if ( obj != null )
                {
                    PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType( obj );
                    if ( prefabType    == PrefabAssetType.Regular
                         || prefabType == PrefabAssetType.Variant )
                    {
                        _contextBrushAdded = true;
                        GameObject gameObj = obj as GameObject;
                        AddLabels( gameObj );
                        _newContextBrush = new MultibrushSettings( gameObj, PaletteManager.selectedPalette );
                    }
                }

                _currentPickerId = -1;
            }
        }

        private void PaletteContextAddMenuItems( GenericMenu menu )
        {
            menu.AddItem( new GUIContent( "New Brush From Prefab" ),      false, CreateBrushFromPrefab );
            menu.AddItem( new GUIContent( "New MultiBrush From Folder" ), false, CreateBrushFromFolder );
            menu.AddItem( new GUIContent( "New Brush From Each Prefab In Folder" ), false,
                CreateBrushFromEachPrefabInFolder );
            menu.AddSeparator( string.Empty );
            menu.AddItem( new GUIContent( "New MultiBrush From Selection" ), false, CreateBrushFromSelection );
            menu.AddItem( new GUIContent( "New Brush From Each Prefab Selected" ), false,
                CreateBushFromEachPrefabSelected );
            menu.AddSeparator( string.Empty );
            menu.AddItem( new GUIContent( "Update all thumbnails" ), false, UpdateAllThumbnails );
            menu.AddSeparator( string.Empty );
            menu.AddItem( new GUIContent( "Brush Creation And Drop Settings" ), false,
                BrushCreationSettingsWindow.ShowWindow );
            if ( PaletteManager.selectedBrushIdx > 0
                 || PaletteManager.movingBrushes )
            {
                menu.AddSeparator( string.Empty );
                if ( PaletteManager.selectedBrushIdx > 0 )
                {
                    menu.AddItem( new GUIContent( "Copy Selected brushes" ), false, PaletteManager.SelectBrushesToMove );
                }

                if ( PaletteManager.movingBrushes )
                {
                    menu.AddItem( new GUIContent( "Paste brushes and keep originals" ),
                        false, PasteBrushesToSelectedPalette );
                    menu.AddItem( new GUIContent( "Paste brushes and delete originals" ),
                        false, MoveBrushesToSelectedPalette );
                }
            }
        }

        private void PasteBrushesToSelectedPalette()
        {
            PaletteManager.PasteBrushesToSelectedPalette();
            OnPaletteChange();
        }

        private void MoveBrushesToSelectedPalette()
        {
            PaletteManager.MoveBrushesToSelectedPalette();
            OnPaletteChange();
        }

        private void PaletteContextMenu()
        {
            GenericMenu menu = new GenericMenu();
            PaletteContextAddMenuItems( menu );
            menu.ShowAsContext();
        }

        private void CreateBrushFromPrefab()
        {
            _currentPickerId = GUIUtility.GetControlID( FocusType.Passive ) + 100;
            EditorGUIUtility.ShowObjectPicker<GameObject>( null, false, "t:Prefab", _currentPickerId );
        }

        private void CreateBrushFromFolder()
        {
            DropUtils.DroppedItem[] items = DropUtils.GetFolderItems();
            if ( items == null )
            {
                return;
            }

            RegisterUndo( "Add Brush" );
            MultibrushSettings brush = new MultibrushSettings( items[ 0 ].obj, PaletteManager.selectedPalette );
            AddLabels( items[ 0 ].obj );
            PaletteManager.selectedPalette.AddBrush( brush );
            DeselectAllButThis( PaletteManager.selectedPalette.brushes.Length - 1 );
            for ( int i = 1; i < items.Length; ++i )
            {
                MultibrushItemSettings item = new MultibrushItemSettings( items[ i ].obj, brush );
                AddLabels( items[ i ].obj );
                brush.AddItem( item );
            }

            OnPaletteChange();
        }

        private void CreateBrushFromEachPrefabInFolder()
        {
            DropUtils.DroppedItem[] items = DropUtils.GetFolderItems();
            if ( items == null )
            {
                return;
            }

            foreach ( DropUtils.DroppedItem item in items )
            {
                if ( item.obj == null )
                {
                    continue;
                }

                RegisterUndo( "Add Brush" );
                AddLabels( item.obj );
                MultibrushSettings brush = new MultibrushSettings( item.obj, PaletteManager.selectedPalette );
                PaletteManager.selectedPalette.AddBrush( brush );
            }

            DeselectAllButThis( PaletteManager.selectedPalette.brushes.Length - 1 );
            OnPaletteChange();
        }

        private string GetPrefabFolder( GameObject obj )
        {
            string   path      = AssetDatabase.GetAssetPath( obj );
            string[] folders   = path.Split( '\\', '/' );
            string   subFolder = folders[ folders.Length - 2 ];
            return subFolder;
        }

        public void CreateBrushFromSelection()
        {
            if ( PaletteManager.selectionCount > 1 )
            {
                MergeBrushes();
                return;
            }

            GameObject[] selectionPrefabs = SelectionManager.GetSelectionPrefabs();
            CreateBrushFromSelection( selectionPrefabs );
        }

        public void CreateBrushFromSelection( GameObject[] selectionPrefabs )
        {
            if ( selectionPrefabs.Length == 0 )
            {
                return;
            }

            RegisterUndo( "Add Brush" );
            AddLabels( selectionPrefabs[ 0 ] );
            MultibrushSettings brush = new MultibrushSettings( selectionPrefabs[ 0 ], PaletteManager.selectedPalette );
            PaletteManager.selectedPalette.AddBrush( brush );
            DeselectAllButThis( PaletteManager.selectedPalette.brushes.Length - 1 );
            for ( int i = 1; i < selectionPrefabs.Length; ++i )
            {
                AddLabels( selectionPrefabs[ i ] );
                brush.AddItem( new MultibrushItemSettings( selectionPrefabs[ i ], brush ) );
            }

            OnPaletteChange();
        }

        public void CreateBrushFromSelection( GameObject selectedPrefab )
            => CreateBrushFromSelection( new[] { selectedPrefab } );

        public void CreateBushFromEachPrefabSelected()
        {
            GameObject[] selectionPrefabs = SelectionManager.GetSelectionPrefabs();
            if ( selectionPrefabs.Length == 0 )
            {
                return;
            }

            foreach ( GameObject obj in selectionPrefabs )
            {
                if ( obj == null )
                {
                    continue;
                }

                RegisterUndo( "Add Brush" );
                MultibrushSettings brush = new MultibrushSettings( obj, PaletteManager.selectedPalette );
                AddLabels( obj );
                PaletteManager.selectedPalette.AddBrush( brush );
            }

            DeselectAllButThis( PaletteManager.selectedPalette.brushes.Length - 1 );
            OnPaletteChange();
        }

        #endregion

        #region DROPBOX

        private void DropBox()
        {
            GUIStyle dragAndDropBoxStyle = new GUIStyle();
            dragAndDropBoxStyle.alignment        = TextAnchor.MiddleCenter;
            dragAndDropBoxStyle.fontStyle        = FontStyle.Italic;
            dragAndDropBoxStyle.fontSize         = 12;
            dragAndDropBoxStyle.normal.textColor = Color.white;
            dragAndDropBoxStyle.wordWrap         = true;
            GUI.Box( _scrollViewRect, "Drag and Drop Prefabs Or Folders Here", dragAndDropBoxStyle );
        }

        private void AddLabels( GameObject obj )
        {
            if ( !PaletteManager.selectedPalette.brushCreationSettings.addLabelsToDroppedPrefabs )
            {
                return;
            }

            HashSet<string> labels     = new HashSet<string>( AssetDatabase.GetLabels( obj ) );
            int             labelCount = labels.Count;
            if ( PaletteManager.selectedPalette.brushCreationSettings.addLabelsToDroppedPrefabs )
            {
                labels.UnionWith( PaletteManager.selectedPalette.brushCreationSettings.labels );
            }

            if ( labelCount != labels.Count )
            {
                AssetDatabase.SetLabels( obj, labels.ToArray() );
            }
        }

        private void DropPrefab()
        {
            if ( _scrollViewRect.Contains( Event.current.mousePosition ) )
            {
                if ( Event.current.type == EventType.DragUpdated )
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    Event.current.Use();
                }
                else if ( Event.current.type == EventType.DragPerform )
                {
                    bool                    paletteChanged = false;
                    DropUtils.DroppedItem[] items          = DropUtils.GetDroppedPrefabs();
                    if ( items.Length > 0 )
                    {
                        PaletteManager.ClearSelection();
                    }

                    int i = 0;
                    foreach ( DropUtils.DroppedItem item in items )
                    {
                        AddLabels( item.obj );
                        MultibrushSettings brush = new MultibrushSettings( item.obj, PaletteManager.selectedPalette );
                        RegisterUndo( "Add Brush" );
                        if ( _moveBrush.to < 0 )
                        {
                            PaletteManager.selectedPalette.AddBrush( brush );
                            PaletteManager.selectedBrushIdx = PaletteManager.selectedPalette.brushes.Length - 1;
                        }
                        else
                        {
                            int idx = _moveBrush.to + i++;
                            PaletteManager.selectedPalette.InsertBrushAt( brush, idx );
                            PaletteManager.selectedBrushIdx = _moveBrush.to;
                        }

                        paletteChanged = true;
                    }

                    if ( paletteChanged )
                    {
                        OnPaletteChange();
                    }

                    if ( draggingBrush && _moveBrush.to >= 0 )
                    {
                        _moveBrush.perform = _moveBrush.from != _moveBrush.to;
                        draggingBrush      = false;
                    }

                    _showCursor = false;
                    Event.current.Use();
                }
                else if ( Event.current.type == EventType.DragExited )
                {
                    _showCursor = false;
                }
            }
        }

        #endregion

        #region TAB BAR

        #region RENAME

        private class RenamePaletteWindow : EditorWindow
        {

            #region Public Methods

            public static void ShowWindow( RenameData data, Action<string, int> onDone )
            {
                RenamePaletteWindow window = GetWindow<RenamePaletteWindow>( true, "Rename Palette" );
                window._currentName = data.currentName;
                window._paletteIdx  = data.paletteIdx;
                window._onDone      = onDone;
                window.position     = new Rect( data.mousePosition.x + 50, data.mousePosition.y + 50, 160, 50 );
            }

            #endregion

            #region Unity Functions

            private void OnGUI()
            {
                EditorGUIUtility.labelWidth = 70;
                EditorGUIUtility.fieldWidth = 70;
                using ( new GUILayout.HorizontalScope() )
                {
                    _currentName = EditorGUILayout.TextField( "New Name:", _currentName );
                }

                using ( new GUILayout.HorizontalScope() )
                {
                    GUILayout.FlexibleSpace();
                    if ( GUILayout.Button( "Apply", GUILayout.Width( 50 ) ) )
                    {
                        _onDone( _currentName, _paletteIdx );
                        Close();
                    }
                }
            }

            #endregion

            #region Private Fields

            private string              _currentName = string.Empty;
            private Action<string, int> _onDone;
            private int                 _paletteIdx = -1;

            #endregion

        }

        private struct RenameData
        {

            #region Public Fields

            public readonly string  currentName;
            public readonly Vector2 mousePosition;
            public readonly int     paletteIdx;

            #endregion

            #region Public Constructors

            public RenameData( int paletteIdx, string currentName, Vector2 mousePosition )
            {
                ( this.paletteIdx, this.currentName, this.mousePosition ) = ( paletteIdx, currentName, mousePosition );
            }

            #endregion

        }

        private void ShowRenamePaletteWindow( object obj )
        {
            if ( !( obj is RenameData ) )
            {
                return;
            }

            RenameData data = (RenameData)obj;
            RenamePaletteWindow.ShowWindow( data, RenamePalette );
        }

        private void RenamePalette( string paletteName, int paletteIdx )
        {
            RegisterUndo( "Rename Palette" );
            PaletteManager.paletteData[ paletteIdx ].name = paletteName;
            _updateTabBarWidth                            = true;
            Repaint();
        }

        #endregion

        private void ShowDeleteConfirmation( object obj )
        {
            int         paletteIdx = (int)obj;
            PaletteData palette    = PaletteManager.paletteData[ paletteIdx ];
            if ( EditorUtility.DisplayDialog( "Delete Palette: "      + palette.name,
                    "Are you sure you want to delete this palette?\n" + palette.name, "Delete", "Cancel" ) )
            {
                RegisterUndo( "Remove Palette" );
                PaletteManager.RemovePaletteAt( paletteIdx );
                if ( PaletteManager.paletteCount == 0 )
                {
                    CreatePalette();
                }
                else if ( PaletteManager.selectedPaletteIdx >= PaletteManager.paletteCount )
                {
                    SelectPalette( 0 );
                }

                --_visibleTabCount;
                if ( lastVisibleIdx >= _visibleTabCount )
                {
                    lastVisibleIdx = _visibleTabCount - 1;
                }

                PaletteManager.selectedBrushIdx = -1;
                _updateTabBarWidth              = true;
                _updateTabBar                   = true;
                UpdateFilteredList( false );
                Repaint();
            }
        }

        #region TAB BUTTONS

        private float _prevWidth;
        private bool  _updateTabBarWidth = true;
        private bool  _updateTabBar;
        private int   _lastVisibleIdx;
        private int   _visibleTabCount;
        private Rect  _dropdownRect;

        public static void UpdateTabBar()
        {
            if ( instance == null )
            {
                return;
            }

            instance._updateTabBar      = true;
            instance._updateTabBarWidth = true;
        }

        private int lastVisibleIdx
        {
            get
            {
                if ( _lastVisibleIdx >= PaletteManager.paletteCount )
                {
                    _lastVisibleIdx = 0;
                }

                return _lastVisibleIdx;
            }
            set => _lastVisibleIdx = value;
        }

        public void SelectPalette( int idx )
        {
            if ( PaletteManager.selectedPaletteIdx == idx )
            {
                return;
            }

            PaletteManager.selectedPaletteIdx = idx;
            PaletteManager.selectedBrushIdx   = -1;
            PaletteManager.ClearSelection();
            _updateTabBar = true;
            OnPaletteChange();
        }

        private void SelectPalette( object obj ) => SelectPalette( (int)obj );

        private void CreatePalette()
        {
            _lastVisibleIdx = PaletteManager.paletteCount;
            PaletteManager.AddPalette( new PaletteData( "Palette" + ( PaletteManager.paletteCount + 1 ),
                DateTime.Now.ToBinary() ), save: true );
            SelectPalette( lastVisibleIdx );
            UpdateTabBar();
        }

        private void DuplicatePalette( object obj )
        {
            int paletteIdx = (int)obj;
            PaletteManager.DuplicatePalette( paletteIdx );
            UpdateTabBar();
            RepainWindow();
        }

        private void ToggleMultipleRows()
            => PaletteManager.showTabsInMultipleRows = !PaletteManager.showTabsInMultipleRows;

        private List<Rect> _tabRects = new List<Rect>();

        private Dictionary<long, float> _tabSize
            = new Dictionary<long, float>();

        private void TabBar()
        {
            float visibleW       = 0;
            int   lastVisibleIdx = 0;
            if ( Event.current.type      == EventType.MouseDown
                 && Event.current.button == 1 )
            {
                for ( int i = 0; i < _tabRects.Count; ++i )
                {
                    if ( _tabRects[ i ].Contains( Event.current.mousePosition ) )
                    {
                        string      name = PaletteManager.paletteNames[ i ];
                        GenericMenu menu = new GenericMenu();
                        menu.AddItem( new GUIContent( "Rename" ), false, ShowRenamePaletteWindow,
                            new RenameData( i, name, position.position + Event.current.mousePosition ) );
                        menu.AddItem( new GUIContent( "Delete" ),    false, ShowDeleteConfirmation, i );
                        menu.AddItem( new GUIContent( "Duplicate" ), false, DuplicatePalette,       i );
                        menu.ShowAsContext();
                    }
                }
            }

            string[] names      = PaletteManager.paletteNames;
            long[]   paletteIds = PaletteManager.paletteIds;

            int Tabs( int from, int to )
            {
                int lastVisible = to;
                for ( int i = from; i <= to; ++i )
                {
                    bool   isSelected = PaletteManager.selectedPaletteIdx == i;
                    string name       = names[ i ];

                    if ( GUILayout.Toggle( isSelected, name, EditorStyles.toolbarButton )
                         && Event.current.button == 0 )
                    {
                        if ( !isSelected )
                        {
                            SelectPalette( i );
                        }

                        isSelected = true;
                    }

                    Rect toggleRect = GUILayoutUtility.GetLastRect();
                    long id         = paletteIds[ i ];
                    if ( Event.current.type == EventType.Repaint )
                    {
                        if ( _tabSize.ContainsKey( id ) )
                        {
                            _tabSize[ id ] = toggleRect.width;
                        }
                        else
                        {
                            _tabSize.Add( id, toggleRect.width );
                        }
                    }

                    if ( Event.current.type == EventType.Repaint )
                    {
                        _tabRects.Add( toggleRect );
                    }

                    if ( Event.current.type == EventType.Repaint
                         && toggleRect.xMax < position.width )
                    {
                        lastVisible = i;
                        visibleW    = toggleRect.xMax;
                    }
                }

                GUILayout.FlexibleSpace();
                return lastVisible;
            }

            using ( new GUILayout.HorizontalScope( EditorStyles.toolbar ) )
            {
                if ( GUILayout.Button( _dropdownIcon, EditorStyles.toolbarButton ) )
                {
                    GenericMenu menu = new GenericMenu();
                    menu.AddItem( new GUIContent( "New palette" ), false, CreatePalette );
                    menu.AddSeparator( string.Empty );
                    menu.AddItem( new GUIContent( "Show tabs in multiple rows" ),
                        PaletteManager.showTabsInMultipleRows, ToggleMultipleRows );
                    menu.AddSeparator( string.Empty );
                    Dictionary<int, string> namesDic = PaletteManager.paletteNames.Select( ( name, index ) => new { name, index } )
                                                                     .ToDictionary( item => item.index, item => item.name );
                    Dictionary<int, string> sortedDic = ( from item in namesDic orderby item.Value select item )
                        .ToDictionary( pair => pair.Key, pair => pair.Value );
                    Dictionary<string, int> repeatedNameCount = new Dictionary<string, int>();
                    foreach ( KeyValuePair<int, string> item in sortedDic )
                    {
                        string name = item.Value;
                        if ( repeatedNameCount.ContainsKey( item.Value ) )
                        {
                            name += "(" + repeatedNameCount[ item.Value ] + ")";
                        }

                        menu.AddItem( new GUIContent( name ), PaletteManager.selectedPaletteIdx == item.Key,
                            SelectPalette, item.Key );
                        if ( repeatedNameCount.ContainsKey( item.Value ) )
                        {
                            repeatedNameCount[ item.Value ] += 1;
                        }
                        else
                        {
                            repeatedNameCount.Add( item.Value, 1 );
                        }
                    }

                    menu.ShowAsContext();
                }

                if ( Event.current.type == EventType.Repaint )
                {
                    _tabRects.Clear();
                }

                if ( PaletteManager.paletteCount == 0 )
                {
                    return;
                }

                lastVisibleIdx = Tabs( 0, this.lastVisibleIdx );
                if ( Event.current.type == EventType.Repaint )
                {
                    if ( _updateTabBarWidth && _visibleTabCount == PaletteManager.paletteCount )
                    {
                        _updateTabBarWidth = false;
                        _lastVisibleIdx    = lastVisibleIdx;
                        _updateTabBar      = true;
                    }
                    else if ( _updateTabBarWidth && _visibleTabCount != PaletteManager.paletteCount )
                    {
                        _lastVisibleIdx = PaletteManager.paletteCount - 1;
                        _updateTabBar   = true;
                    }

                    if ( _prevWidth != position.width )
                    {
                        if ( _prevWidth < position.width )
                        {
                            _updateTabBarWidth = true;
                        }

                        _lastVisibleIdx = lastVisibleIdx;
                        _prevWidth      = position.width;
                        _updateTabBar   = true;
                    }
                }
            }

            if ( PaletteManager.showTabsInMultipleRows )
            {
                List<int> rowItemCount = new List<int>();
                float     tabsWidth    = 0;
                int       tabItemCount = 0;
                for ( int i = _visibleTabCount; i < PaletteManager.paletteCount; ++i )
                {
                    long id = paletteIds[ i ];
                    if ( !_tabSize.ContainsKey( id ) )
                    {
                        _updateTabBarWidth = true;
                        _updateTabBar      = true;
                        continue;
                    }

                    float w = _tabSize[ id ];
                    tabsWidth += w;
                    if ( tabsWidth > position.width )
                    {
                        rowItemCount.Add( Mathf.Max( tabItemCount, 1 ) );
                        tabsWidth = tabItemCount > 0 ? w : 0;
                        if ( tabItemCount == 0 )
                        {
                            continue;
                        }

                        tabItemCount = 0;
                    }

                    ++tabItemCount;
                }

                if ( tabItemCount > 0 )
                {
                    rowItemCount.Add( tabItemCount );
                }

                if ( rowItemCount.Count > 0 )
                {
                    if ( _visibleTabCount == PaletteManager.paletteCount )
                    {
                        _updateTabBar = true;
                    }

                    int fromIdx = _visibleTabCount;
                    int toIdx   = _visibleTabCount;
                    foreach ( int itemCount in rowItemCount )
                    {
                        toIdx = fromIdx + itemCount - 1;
                        using ( new GUILayout.HorizontalScope( EditorStyles.toolbar ) )
                        {
                            Tabs( fromIdx, toIdx );
                        }

                        fromIdx = toIdx + 1;
                        if ( fromIdx >= PaletteManager.paletteCount )
                        {
                            break;
                        }
                    }
                }
            }

            if ( _updateTabBar && PaletteManager.paletteCount > 1 )
            {
                if ( !PaletteManager.showTabsInMultipleRows
                     && PaletteManager.selectedPaletteIdx > this.lastVisibleIdx )
                {
                    PaletteManager.SwapPalette( PaletteManager.selectedPaletteIdx, this.lastVisibleIdx );
                    PaletteManager.selectedPaletteIdx = this.lastVisibleIdx;
                }

                _visibleTabCount = this.lastVisibleIdx + 1;
                _updateTabBar    = false;
                Repaint();
            }
        }

        #endregion

        #endregion

        #region SEARCH BAR

        private string     _filterText = string.Empty;
        private GUIContent _labelIcon;
        private GUIContent _selectionFilterIcon;
        private GUIContent _clearFilterIcon;

        private struct FilteredBrush
        {

            #region Public Fields

            public readonly MultibrushSettings brush;
            public readonly int                index;

            #endregion

            #region Public Constructors

            public FilteredBrush( MultibrushSettings brush, int index )
            {
                ( this.brush, this.index ) = ( brush, index );
            }

            #endregion

        }

        private List<FilteredBrush> _filteredBrushList
            = new List<FilteredBrush>();

        private List<FilteredBrush> filteredBrushList
        {
            get
            {
                if ( _filteredBrushList == null )
                {
                    _filteredBrushList = new List<FilteredBrush>();
                }

                return _filteredBrushList;
            }
        }

        public bool FilteredBrushListContains( int index ) => _filteredBrushList.Exists( brush => brush.index == index );

        private Dictionary<string, bool> _labelFilter
            = new Dictionary<string, bool>();

        public Dictionary<string, bool> labelFilter
        {
            get
            {
                if ( _labelFilter == null )
                {
                    _labelFilter = new Dictionary<string, bool>();
                }

                return _labelFilter;
            }
            set => _labelFilter = value;
        }

        private bool _updateLabelFilter = true;
        public  int  filteredBrushListCount => filteredBrushList.Count;

        public string filterText
        {
            get
            {
                if ( _filterText == null )
                {
                    _filterText = string.Empty;
                }

                return _filterText;
            }
            set => _filterText = value;
        }

        private void ClearLabelFilter()
        {
            foreach ( string key in labelFilter.Keys.ToArray() )
            {
                labelFilter[ key ] = false;
            }
        }

        private void SearchBar()
        {
            using ( new GUILayout.HorizontalScope( EditorStyles.toolbar ) )
            {
                GUILayout.FlexibleSpace();

                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    #if UNITY_2019_1_OR_NEWER
                    GUIStyle searchFieldStyle = EditorStyles.toolbarSearchField;
                    #else
                    var searchFieldStyle = EditorStyles.toolbarTextField;
                    #endif
                    GUILayout.Space( 2 );
                    filterText = EditorGUILayout.TextField( filterText, searchFieldStyle ).Trim();
                    if ( check.changed )
                    {
                        UpdateFilteredList( true );
                    }
                }

                if ( filterText != string.Empty )
                {
                    if ( GUILayout.Button( _clearFilterIcon, EditorStyles.toolbarButton ) )
                    {
                        filterText = string.Empty;
                        ClearLabelFilter();
                        UpdateFilteredList( true );
                        GUI.FocusControl( null );
                    }
                }

                if ( GUILayout.Button( _labelIcon, EditorStyles.toolbarButton ) )
                {
                    GUI.FocusControl( null );
                    UpdateLabelFilter();
                    GenericMenu menu = new GenericMenu();
                    if ( labelFilter.Count == 0 )
                    {
                        menu.AddItem( new GUIContent( "No labels Found" ), false, null );
                    }
                    else
                    {
                        foreach ( KeyValuePair<string, bool> labelItem in labelFilter.OrderBy( item => item.Key ) )
                        {
                            menu.AddItem( new GUIContent( labelItem.Key ), labelItem.Value,
                                SelectLabelFilter, labelItem.Key );
                        }
                    }

                    menu.ShowAsContext();
                }

                if ( GUILayout.Button( _selectionFilterIcon, EditorStyles.toolbarButton ) )
                {
                    GUI.FocusControl( null );
                    FilterBySelection();
                }
            }

            if ( _updateLabelFilter )
            {
                _updateLabelFilter = false;
                UpdateLabelFilter();
            }

            if ( Event.current.type      == EventType.MouseDown
                 && Event.current.button == 0 )
            {
                GUI.FocusControl( null );
                Repaint();
            }
        }

        private bool FilteredListContains( int index )
        {
            foreach ( FilteredBrush filtered in filteredBrushList )
            {
                if ( filtered.index == index )
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateFilteredList( bool textCanged )
        {
            filteredBrushList.Clear();

            void RemoveFromSelection( int index )
            {
                PaletteManager.RemoveFromSelection( index );
                if ( PaletteManager.selectedBrushIdx == index )
                {
                    PaletteManager.selectedBrushIdx = -1;
                }

                if ( PaletteManager.selectionCount == 1 )
                {
                    PaletteManager.selectedBrushIdx = PaletteManager.idxSelection[ 0 ];
                }
            }

            //filter by label
            string[]     filterTextArray = filterText.Split( ',' );
            List<string> filterTextList  = new List<string>();
            ClearLabelFilter();
            bool filterByLabel = false;
            for ( int i = 0; i < filterTextArray.Length; ++i )
            {
                string filterText = filterTextArray[ i ].Trim();
                if ( filterText.Length               >= 2
                     && filterText.Substring( 0, 2 ) == "l:" )
                {
                    filterText = filterText.Substring( 2 );
                    if ( labelFilter.ContainsKey( filterText ) )
                    {
                        labelFilter[ filterText ] = true;
                        filterByLabel             = true;
                    }
                    else
                    {
                        return;
                    }

                    continue;
                }

                filterTextList.Add( filterText );
            }

            List<FilteredBrush>  tempFilteredBrushList = new List<FilteredBrush>();
            MultibrushSettings[] brushes               = PaletteManager.selectedPalette.brushes;
            if ( !filterByLabel )
            {
                for ( int i = 0; i < brushes.Length; ++i )
                {
                    if ( brushes[ i ].containMissingPrefabs )
                    {
                        continue;
                    }

                    tempFilteredBrushList.Add( new FilteredBrush( brushes[ i ], i ) );
                }
            }
            else
            {
                for ( int i = 0; i < brushes.Length; ++i )
                {
                    MultibrushSettings brush = brushes[ i ];
                    if ( brush.containMissingPrefabs )
                    {
                        continue;
                    }

                    bool itemContainsFilter = false;
                    foreach ( MultibrushItemSettings item in brush.items )
                    {
                        if ( item.prefab == null )
                        {
                            continue;
                        }

                        string[] labels = AssetDatabase.GetLabels( item.prefab );
                        foreach ( string label in labels )
                        {
                            if ( labelFilter[ label ] )
                            {
                                itemContainsFilter = true;
                                break;
                            }
                        }

                        if ( itemContainsFilter )
                        {
                            break;
                        }
                    }

                    if ( itemContainsFilter )
                    {
                        tempFilteredBrushList.Add( new FilteredBrush( brush, i ) );
                    }
                    else
                    {
                        RemoveFromSelection( i );
                    }
                }
            }

            //filter by name
            bool listIsEmpty = filterTextList.Count == 0;
            if ( !listIsEmpty )
            {
                listIsEmpty = true;
                foreach ( string filter in filterTextList )
                {
                    if ( filter != string.Empty )
                    {
                        listIsEmpty = false;
                        break;
                    }
                }
            }

            if ( listIsEmpty )
            {
                filteredBrushList.AddRange( tempFilteredBrushList );
                return;
            }

            foreach ( FilteredBrush filteredItem in tempFilteredBrushList.ToArray() )
            {
                for ( int i = 0; i < filterTextList.Count; ++i )
                {
                    string filterText    = filterTextList[ i ].Trim();
                    bool   wholeWordOnly = false;
                    if ( filterText == string.Empty )
                    {
                        continue;
                    }

                    if ( filterText.Length               >= 2
                         && filterText.Substring( 0, 2 ) == "w:" )
                    {
                        wholeWordOnly = true;
                        filterText    = filterText.Substring( 2 );
                    }

                    if ( filterText == string.Empty )
                    {
                        continue;
                    }

                    filterText = filterText.ToLower();
                    MultibrushSettings brush = filteredItem.brush;
                    if ( ( !wholeWordOnly   && brush.name.ToLower().Contains( filterText ) )
                         || ( wholeWordOnly && brush.name.ToLower() == filterText ) )
                    {
                        filteredBrushList.Add( filteredItem );
                    }
                    else
                    {
                        RemoveFromSelection( filteredItem.index );
                    }
                }
            }
        }

        private void UpdateLabelFilter()
        {
            foreach ( MultibrushSettings brush in PaletteManager.selectedPalette.brushes )
            {
                foreach ( MultibrushItemSettings item in brush.items )
                {
                    if ( item.prefab == null )
                    {
                        continue;
                    }

                    string[] labels = AssetDatabase.GetLabels( item.prefab );
                    foreach ( string label in labels )
                    {
                        if ( labelFilter.ContainsKey( label ) )
                        {
                            continue;
                        }

                        labelFilter.Add( label, false );
                    }
                }
            }
        }

        private void SelectLabelFilter( object key )
        {
            labelFilter[ (string)key ] = !labelFilter[ (string)key ];
            foreach ( KeyValuePair<string, bool> pair in labelFilter )
            {
                if ( !pair.Value )
                {
                    continue;
                }

                string labelFilter = "l:" + pair.Key;
                if ( filterText.Contains( labelFilter ) )
                {
                    continue;
                }

                if ( filterText.Length > 0 )
                {
                    filterText += ", ";
                }

                filterText += labelFilter;
            }

            string[] filterTextArray = filterText.Split( ',' );
            filterText = string.Empty;
            for ( int i = 0; i < filterTextArray.Length; ++i )
            {
                string filter = filterTextArray[ i ].Trim();
                if ( filter.Length               >= 2
                     && filter.Substring( 0, 2 ) == "l:" )
                {
                    string label = filter.Substring( 2 );
                    if ( !labelFilter.ContainsKey( label ) )
                    {
                        continue;
                    }

                    if ( !labelFilter[ label ] )
                    {
                        continue;
                    }

                    if ( filterText.Contains( filter ) )
                    {
                        continue;
                    }
                }

                if ( filter == string.Empty )
                {
                    continue;
                }

                filterText += filter + ", ";
            }

            if ( filterText != string.Empty )
            {
                filterText = filterText.Substring( 0, filterText.Length - 2 );
            }

            UpdateFilteredList( false );
            Repaint();
        }

        public int FilterBySelection()
        {
            GameObject[] selection = SelectionManager.GetSelectionPrefabs();
            filterText = string.Empty;
            for ( int i = 0; i < selection.Length; ++i )
            {
                filterText += "w:" + selection[ i ].name;
                if ( i < selection.Length - 1 )
                {
                    filterText += ", ";
                }
            }

            UpdateFilteredList( false );
            return filteredBrushListCount;
        }

        public void SelectFirstBrush()
        {
            if ( filteredBrushListCount == 0 )
            {
                return;
            }

            DeselectAllButThis( filteredBrushList[ 0 ].index );
        }

        #endregion

    }
}