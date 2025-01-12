using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PluginMaster
{
    public static class DropUtils
    {

        #region Public Methods

        public static DroppedItem[] GetDirPrefabs( string dirPath )
        {
            string[]          filePaths   = Directory.GetFiles( dirPath, "*.prefab" );
            List<DroppedItem> subItemList = new List<DroppedItem>();
            string            dirName     = dirPath.Substring( Mathf.Max( dirPath.LastIndexOf( '/' ), dirPath.LastIndexOf( '\\' ) ) + 1 );
            foreach ( string filePath in filePaths )
            {
                DroppedItem item;
                item.obj = AssetDatabase.LoadAssetAtPath<GameObject>( filePath );
                PrefabAssetType prefabType = PrefabUtility.GetPrefabAssetType( item.obj );
                if ( prefabType    != PrefabAssetType.Regular
                     && prefabType != PrefabAssetType.Variant )
                {
                    continue;
                }

                subItemList.Add( item );
            }

            if ( PaletteManager.selectedPalette.brushCreationSettings.includeSubfolders )
            {
                string[] subdirPaths = Directory.GetDirectories( dirPath );
                foreach ( string subdirPath in subdirPaths )
                {
                    subItemList.AddRange( GetDirPrefabs( subdirPath ) );
                }
            }

            return subItemList.ToArray();
        }

        public static DroppedItem[] GetDroppedPrefabs()
        {
            List<DroppedItem> itemList = new List<DroppedItem>();
            for ( int i = 0; i < DragAndDrop.objectReferences.Length; ++i )
            {
                Object objRef = DragAndDrop.objectReferences[ i ];
                if ( objRef is GameObject )
                {
                    if ( objRef == null )
                    {
                        continue;
                    }

                    if ( PrefabUtility.GetPrefabAssetType( objRef ) == PrefabAssetType.NotAPrefab )
                    {
                        continue;
                    }

                    string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot( objRef );
                    if ( path == string.Empty )
                    {
                        continue;
                    }

                    GameObject prefab         = objRef as GameObject;
                    GameObject prefabInstance = PrefabUtility.GetNearestPrefabInstanceRoot( objRef );
                    if ( prefabInstance != null )
                    {
                        PrefabAssetType assetType = PrefabUtility.GetPrefabAssetType( prefabInstance );
                        if ( assetType    == PrefabAssetType.NotAPrefab
                             || assetType == PrefabAssetType.NotAPrefab )
                        {
                            continue;
                        }

                        if ( assetType == PrefabAssetType.Variant )
                        {
                            prefab = prefabInstance;
                        }
                        else
                        {
                            prefab = PrefabUtility.GetCorrespondingObjectFromSource( prefabInstance );
                        }
                    }

                    itemList.Add( new DroppedItem( prefab ) );
                }
                else
                {
                    string path = DragAndDrop.paths[ i ];
                    if ( objRef is DefaultAsset
                         && AssetDatabase.IsValidFolder( path ) )
                    {
                        itemList.AddRange( GetDirPrefabs( path ) );
                    }
                }
            }

            return itemList.ToArray();
        }

        public static DroppedItem[] GetFolderItems()
        {
            DroppedItem[] items  = null;
            string        folder = EditorUtility.OpenFolderPanel( "Add Prefabs in folder:", Application.dataPath, "Assets" );
            if ( folder.Contains( Application.dataPath ) )
            {
                folder = folder.Replace( Application.dataPath, "Assets" );
                items  = GetDirPrefabs( folder );
                if ( items.Length == 0 )
                {
                    EditorUtility.DisplayDialog( "No Prefabs found", "No prefabs found in folder", "Ok" );
                }
            }
            else if ( folder != string.Empty )
            {
                EditorUtility.DisplayDialog( "Folder Error", "Folder must be under Assets folder", "Ok" );
            }

            return items;
        }

        #endregion

        public struct DroppedItem
        {

            #region Public Fields

            public GameObject obj;

            #endregion

            #region Public Constructors

            public DroppedItem( GameObject obj )
            {
                this.obj = obj;
            }

            #endregion

        }
    }
}