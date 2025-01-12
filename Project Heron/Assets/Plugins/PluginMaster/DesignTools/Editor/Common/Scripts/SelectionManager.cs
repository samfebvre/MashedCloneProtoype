using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PluginMaster
{
    [InitializeOnLoad]
    public static class SelectionManager
    {

        #region Statics and Constants

        public static Action selectionChanged;

        #endregion

        #region Public Properties

        public static GameObject[] selection { get; private set; } = new GameObject[ 0 ];

        public static GameObject[] topLevelSelection { get; private set; } = new GameObject[ 0 ];

        public static GameObject[] topLevelSelectionWithPrefabs { get; private set; } = new GameObject[ 0 ];

        #endregion

        #region Public Methods

        public static GameObject[] GetSelection( bool filteredByTopLevel )
            => filteredByTopLevel ? topLevelSelection : selection;

        public static GameObject[] GetSelectionPrefabs()
        {
            List<GameObject> result = new List<GameObject>();
            foreach ( GameObject obj in topLevelSelectionWithPrefabs )
            {
                if ( obj == null )
                {
                    continue;
                }

                PrefabAssetType assetType = PrefabUtility.GetPrefabAssetType( obj );
                if ( assetType == PrefabAssetType.NotAPrefab )
                {
                    continue;
                }

                GameObject prefab = obj;
                if ( PrefabUtility.IsAnyPrefabInstanceRoot( obj ) )
                {
                    prefab = assetType == PrefabAssetType.Variant
                        ? obj
                        : PrefabUtility.GetCorrespondingObjectFromSource( obj );
                }

                if ( result.Contains( prefab ) )
                {
                    continue;
                }

                result.Add( prefab );
            }

            return result.ToArray();
        }

        public static void UpdateSelection()
        {
            List<GameObject> selectionOrderedTopLevel = new List<GameObject>( topLevelSelection );
            List<GameObject> selectionOrdered         = new List<GameObject>( selection );
            List<GameObject> selectionOrderedTopLevelWithPrefabs
                = new List<GameObject>( topLevelSelectionWithPrefabs );
            UpdateSelection( selectionOrderedTopLevel,            true,  true );
            UpdateSelection( selectionOrdered,                    false, true );
            UpdateSelection( selectionOrderedTopLevelWithPrefabs, true,  false );
            selection                    = selectionOrdered.ToArray();
            topLevelSelection            = selectionOrderedTopLevel.ToArray();
            topLevelSelectionWithPrefabs = selectionOrderedTopLevelWithPrefabs.ToArray();
            if ( selectionChanged != null )
            {
                selectionChanged();
            }
        }

        #endregion

        #region Private Constructors

        static SelectionManager()
        {
            Selection.selectionChanged += UpdateSelection;
        }

        #endregion

        #region Private Methods

        private static void UpdateSelection( List<GameObject> list,
                                             bool             filteredByTopLevel, bool excludePrefabs )
        {
            HashSet<GameObject> newSet = new HashSet<GameObject>(
                Selection.GetFiltered<GameObject>( SelectionMode.Editable
                                                   | ( excludePrefabs ? SelectionMode.ExcludePrefab : SelectionMode.Unfiltered )
                                                   | ( filteredByTopLevel ? SelectionMode.TopLevel : SelectionMode.Unfiltered ) ) );
            if ( newSet.Count == 0 )
            {
                list.Clear();
                return;
            }

            HashSet<GameObject> unselectedSet = new HashSet<GameObject>( list );
            unselectedSet.ExceptWith( newSet );
            foreach ( GameObject obj in unselectedSet )
            {
                list.Remove( obj );
            }

            newSet.ExceptWith( list );
            foreach ( GameObject obj in newSet )
            {
                list.Add( obj );
            }
        }

        #endregion

    }
}