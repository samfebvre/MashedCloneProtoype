using UnityEditor;
using UnityEngine;

namespace PluginMaster
{
    public class BrushCreationSettingsWindow : EditorWindow
    {

        #region Statics and Constants

        private static string UNDO_MSG = "Brush Creation Settings";

        #endregion

        #region Serialized

        [SerializeField] private PWBData _data;

        #endregion

        #region Public Methods

        [MenuItem( "Tools/Plugin Master/Prefab World Builder/Brush Creation Settings...", false, 1140 )]
        public static void ShowWindow() => GetWindow<BrushCreationSettingsWindow>();

        #endregion

        #region Unity Functions

        private void OnEnable()
        {
            _data                  =  PWBCore.staticData;
            Undo.undoRedoPerformed += Repaint;
            titleContent           =  new GUIContent( PaletteManager.selectedPalette.name + " - Brush Creation Settings" );

        }

        private void OnDisable() => Undo.undoRedoPerformed -= Repaint;

        private void OnGUI()
        {
            if ( PaletteManager.selectedPalette == null )
            {
                return;
            }

            EditorGUIUtility.labelWidth = 60;
            BrushCreationSettings settings = PaletteManager.selectedPalette.brushCreationSettings.Clone();
            using ( EditorGUILayout.ScrollViewScope scrollView = new EditorGUILayout.ScrollViewScope( _mainScrollPosition,
                       false, false, GUI.skin.horizontalScrollbar, GUI.skin.verticalScrollbar, GUIStyle.none ) )
            {
                using ( EditorGUI.ChangeCheckScope check = new EditorGUI.ChangeCheckScope() )
                {
                    _mainScrollPosition = scrollView.scrollPosition;
                    settings.includeSubfolders = EditorGUILayout.ToggleLeft( "Include subfolders",
                        settings.includeSubfolders );
                    using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
                    {
                        settings.addLabelsToDroppedPrefabs = EditorGUILayout.ToggleLeft( "Add labels to prefabs",
                            settings.addLabelsToDroppedPrefabs );
                        using ( new EditorGUI.DisabledGroupScope( !settings.addLabelsToDroppedPrefabs ) )
                        {
                            EditorGUIUtility.labelWidth = 40;
                            settings.labelsCSV          = EditorGUILayout.TextField( "Labels:", settings.labelsCSV );
                        }
                    }

                    #if UNITY_2019_1_OR_NEWER
                    _defaultBrushSettingsGroupOpen
                        = EditorGUILayout.BeginFoldoutHeaderGroup( _defaultBrushSettingsGroupOpen,
                            "Default Brush Settings" );
                    #else
                    _defaultBrushSettingsGroupOpen = EditorGUILayout.Foldout(_defaultBrushSettingsGroupOpen,
                    "Default Brush Settings");
                    #endif
                    if ( _defaultBrushSettingsGroupOpen )
                    {
                        using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
                        {
                            BrushProperties.BrushFields( settings.defaultBrushSettings, ref _brushPosGroupOpen,
                                ref _brushRotGroupOpen, ref _brushScaleGroupOpen, ref _brushFlipGroupOpen, this, UNDO_MSG );
                            GUILayout.Space( 10 );
                            if ( GUILayout.Button( "Reset to factory settings" ) )
                            {
                                settings.FactoryResetDefaultBrushSettings();
                                GUI.FocusControl( null );
                                Repaint();
                            }
                        }
                    }
                    #if UNITY_2019_1_OR_NEWER
                    EditorGUILayout.EndFoldoutHeaderGroup();
                    #endif
                    _defaultThumbnailSettingsGroupOpen
                        = EditorGUILayout.BeginFoldoutHeaderGroup( _defaultThumbnailSettingsGroupOpen,
                            "Default Thumbnail Settings" );
                    if ( _defaultThumbnailSettingsGroupOpen )
                    {
                        using ( new GUILayout.VerticalScope( EditorStyles.helpBox ) )
                        {
                            ThumbnailEditorWindow.ThumbnailSettingsGUI( settings.defaultThumbnailSettings );
                            GUILayout.Space( 10 );
                            if ( GUILayout.Button( "Reset to factory settings" ) )
                            {
                                settings.FactoryResetDefaultThumbnailSettings();
                                GUI.FocusControl( null );
                                Repaint();
                            }
                        }
                    }

                    EditorGUILayout.EndFoldoutHeaderGroup();

                    if ( Event.current.type      == EventType.MouseDown
                         && Event.current.button == 0 )
                    {
                        GUI.FocusControl( null );
                        Repaint();
                    }

                    if ( check.changed )
                    {
                        Undo.RegisterCompleteObjectUndo( this, UNDO_MSG );
                        PaletteManager.selectedPalette.brushCreationSettings.Copy( settings );
                        PWBCore.SetSavePending();
                    }
                }
            }
        }

        #endregion

        #region Private Fields

        private bool _brushFlipGroupOpen;
        private bool _brushPosGroupOpen;
        private bool _brushRotGroupOpen;
        private bool _brushScaleGroupOpen;

        private bool _defaultBrushSettingsGroupOpen;
        private bool _defaultThumbnailSettingsGroupOpen;

        private Vector2 _mainScrollPosition = Vector2.zero;

        #endregion

        #region Private Methods

        [MenuItem( "Assets/Clear Labels", false, 2000 )]
        private static void ClearLabels()
        {
            Object[] selection = Selection.GetFiltered<Object>( SelectionMode.Assets );
            foreach ( Object asset in selection )
            {
                AssetDatabase.ClearLabels( asset );
            }
        }

        #endregion

    }
}