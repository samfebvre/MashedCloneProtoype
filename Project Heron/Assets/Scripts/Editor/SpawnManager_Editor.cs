using System.Collections;
using DefaultNamespace;
using DefaultNamespace.Utils;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace Editor
{
    [CustomEditor( typeof(SpawnManager) )]
    public class SpawnManager_Editor : UnityEditor.Editor
    {

        #region Serialized

        [FormerlySerializedAs( "numberOfBoxesToSpawn" )]
        public int NumberOfBoxesToSpawn = 1;

        public float TimeBetweenSpawns = 0.01f;

        public int VehicleIndex;

        #endregion

        #region Public Methods

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            SpawnManager spawnManager = (SpawnManager)target;

            // Vehicle preview bits
            {
                // add a horizontal line
                CustomEditorUtils.DrawSeparator();

                // Add a label that says 'Vehicle Preview'
                EditorGUILayout.LabelField( "Vehicle Preview", CustomGUIStyles.InspectorViewStyles.Inspector_LargeHeadingStyle );

                GUIStyle helpBoxStyle = GUI.skin.GetStyle( "HelpBox" );

                // start horizontal scope
                EditorGUILayout.BeginHorizontal( helpBoxStyle );
                {
                    EditorGUILayout.BeginVertical();
                    {
                        GUILayout.FlexibleSpace();
                        // add a label that says 'Vehicle Prefab Index'
                        EditorGUILayout.LabelField( "Vehicle Prefab Index" );
                        // add a slider to control the vehicle index
                        VehicleIndex = Mathf.Clamp( VehicleIndex, 0, GameManager.Instance.VehiclePrefabs.Count               - 1 );
                        VehicleIndex = EditorGUILayout.IntSlider( VehicleIndex, 0, GameManager.Instance.VehiclePrefabs.Count - 1 );
                        GUILayout.FlexibleSpace();
                        EditorGUILayout.EndVertical();
                    }

                    EditorGUILayout.BeginVertical( GUILayout.Width( 128 ) );
                    {
                        // display the vehicle prefab field
                        m_vehiclePrefab = GameManager.Instance.VehiclePrefabs[ VehicleIndex ];
                        m_vehiclePrefab = (GameObject)EditorGUILayout.ObjectField( m_vehiclePrefab, typeof(GameObject), false );
                        if ( m_vehiclePrefab != null
                             && GameManager.Instance.VehiclePrefabs.Contains( m_vehiclePrefab ) )
                        {
                            VehicleIndex = GameManager.Instance.VehiclePrefabs.IndexOf( m_vehiclePrefab );
                        }

                        // add an image preview of the vehicle prefab
                        Texture2D previewTexture = AssetPreview.GetAssetPreview( m_vehiclePrefab );
                        GUILayout.Label( previewTexture, GUILayout.Width( 128 ), GUILayout.Height( 128 ) );

                        EditorGUILayout.EndVertical();
                    }

                    // end horizontal scope
                    EditorGUILayout.EndHorizontal();
                }
            }

            // add a slider to control the number of boxes to spawn
            NumberOfBoxesToSpawn = EditorGUILayout.IntSlider( "Number of Boxes to Spawn", NumberOfBoxesToSpawn, 1, 64 );

            // add a slider to control the time between spawns
            TimeBetweenSpawns = EditorGUILayout.Slider( "Time Between Spawns", TimeBetweenSpawns, 0.01f, 5.0f );

            // if the user clicks the button, spawn boxes for the previewed vehicle
            if ( GUILayout.Button( "Spawn Boxes For This Vehicle" ) )
            {
                m_spawnCoroutine = EditorCoroutineUtility.StartCoroutine( SpawnABunchOfBoxesForSpecifiedVehicle(), this );
            }

            // if the user clicks the button, spawn boxes for random vehicles
            if ( GUILayout.Button( "Spawn Boxes For Random Vehicles" ) )
            {
                m_spawnCoroutine = EditorCoroutineUtility.StartCoroutine( SpawnABunchOfBoxesForRandomVehicles(), this );
            }

            // if the user clicks the button, clear all boxes
            if ( GUILayout.Button( "Clear Boxes" ) )
            {
                spawnManager.ClearBoxes();
                if ( m_spawnCoroutine != null )
                {
                    EditorCoroutineUtility.StopCoroutine( m_spawnCoroutine );
                }
            }
        }

        #endregion

        #region Private Fields

        private EditorCoroutine m_spawnCoroutine;

        private GameObject m_vehiclePrefab;

        #endregion

        #region Private Methods

        private IEnumerator SpawnABunchOfBoxesForRandomVehicles()
        {
            SpawnManager spawnManager = (SpawnManager)target;
            spawnManager.Init_ManagerReferences();
            for ( int i = 0; i < NumberOfBoxesToSpawn; i++ )
            {
                // get random vehicle index
                int vehicleIndex = Random.Range( 0, GameManager.Instance.VehiclePrefabs.Count );
                spawnManager.TryCreateSpawnBoxFromVehiclePrefab( vehicleIndex );
                yield return new WaitForSecondsRealtime( TimeBetweenSpawns );
            }
        }

        private IEnumerator SpawnABunchOfBoxesForSpecifiedVehicle()
        {
            SpawnManager spawnManager = (SpawnManager)target;
            spawnManager.Init_ManagerReferences();
            for ( int i = 0; i < NumberOfBoxesToSpawn; i++ )
            {
                spawnManager.TryCreateSpawnBoxFromVehiclePrefab( VehicleIndex );
                yield return new WaitForSecondsRealtime( TimeBetweenSpawns );
            }
        }

        #endregion

    }
}