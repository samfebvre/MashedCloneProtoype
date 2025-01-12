using System;
using System.Collections.Generic;
using DefaultNamespace.Settings;
using DefaultNamespace.Utils;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DefaultNamespace.Editor
{
    public class VehicleSetupWindow : EditorWindow
    {

        #region Statics and Constants

        private const string VEHICLES_PATH               = "Assets/Prefabs/Vehicles";
        private const string BASIC_VEHICLE_SETTINGS_PATH = "Assets/Settings/VehicleSettings.asset";

        #endregion

        #region Public Properties

        public VehicleSettings VehicleSettings_Prop =>
            m_vehicleSettings ??= AssetDatabase.LoadAssetAtPath<VehicleSettings>( BASIC_VEHICLE_SETTINGS_PATH );

        #endregion

        #region Public Methods

        public void CreateGUI()
        {
            // Use UI toolkit to create the UI
            VisualElement root = rootVisualElement;

            // add a button to load the prefabs
            m_loadButton = new Button( LoadAssetsInPathFolder )
            {
                text = "Load",
            };

            // add a button to setup the vehicles
            m_setupVehiclesButton = new Button( SetupVehicles )
            {
                text = "Setup Vehicles",
            };

            m_loadedVehiclesListView = CreateVehicleListView();

            root.Add( m_loadButton );
            root.Add( m_setupVehiclesButton );
            root.Add( m_loadedVehiclesListView );

            LoadAssetsInPathFolder();
        }

        #endregion

        #region Private Fields

        private readonly List<GameObject> m_loadedVehiclesAsGameObjects = new List<GameObject>();
        private          Button           m_loadButton;
        private          List<string>     m_loadedVehiclesAsAssetPaths = new List<string>();

        private string[] m_loadedVehiclesAsGuids;
        private ListView m_loadedVehiclesListView;
        private Button   m_setupVehiclesButton;

        private VehicleSettings m_vehicleSettings;

        #endregion

        #region Private Methods

        private ListView CreateVehicleListView()
        {
            Func<VisualElement> makeItem = GenerateVisualElementForVehiclePrefab;

            Action<VisualElement, int> bindItem = ( e, i ) =>
            {
                Label nameLabel = e.Q<Label>( "Object Name" );
                Image thumbnail = e.Q<Image>( "Thumbnail" );

                nameLabel.text  = m_loadedVehiclesAsGameObjects[ i ].name;
                thumbnail.image = AssetPreview.GetAssetPreview( m_loadedVehiclesAsGameObjects[ i ] );
            };

            const int ITEM_HEIGHT = 128;
            ListView listView = new ListView( m_loadedVehiclesAsGameObjects, ITEM_HEIGHT, makeItem, bindItem )
            {
                selectionType = SelectionType.Multiple,
                style =
                {
                    //m_loadedVehiclesListView.showAddRemoveFooter = true;
                    flexGrow = 1.0f,
                    // give the list view some padding
                    paddingTop    = 10,
                    paddingBottom = 10,
                    paddingLeft   = 10,
                    paddingRight  = 10,
                },
                showAddRemoveFooter = true,
            };

            return listView;
        }

        // private VisualElement GenerateVisualElementForVehiclePrefab( GameObject vehicle )
        // {
        //     VisualElement root      = new VisualElement();
        //     Label         label     = new Label( vehicle.name );
        //     Texture2D     thumbnail = AssetPreview.GetAssetPreview( vehicle );
        //     root.Add( label );
        //     root.Add( new Image { image = thumbnail } );
        //     return root;
        // }

        private VisualElement GenerateVisualElementForVehiclePrefab()
        {
            VisualElement root = new VisualElement();

            root.Add( new Label
            {
                name = "Object Name",
                style =
                {
                    alignSelf = Align.Center,
                },

            } );
            root.Add( new Image
            {
                name = "Thumbnail",
                style =
                {
                    alignSelf = Align.Center,
                    // add padding at bottom
                    marginBottom = 10,
                },
            } );
            return root;
        }

        private void LoadAssetsInPathFolder()
        {
            m_loadedVehiclesAsGuids = AssetDatabase.FindAssets( "t:GameObject", new[] { VEHICLES_PATH } );
            foreach ( string assetGuid in m_loadedVehiclesAsGuids )
            {
                string     assetPath = AssetDatabase.GUIDToAssetPath( assetGuid );
                GameObject asset     = AssetDatabase.LoadAssetAtPath<GameObject>( assetPath );
                m_loadedVehiclesAsGameObjects.Add( asset );
                m_loadedVehiclesAsAssetPaths.Add( assetPath );
            }

            RefreshVehicleListView();
        }

        private void RefreshVehicleListView()
        {
            m_loadedVehiclesListView.RefreshItems();
        }

        /// <summary>
        ///     I will likely replace a lot of this with a different system - it doesnt seem right to do it this way. Instead, the vast majority of this stuff should be set when the object is
        ///     instantiated/created.
        /// </summary>
        private void SetupVehicles()
        {
            foreach ( GameObject vehicleAsGameObject in m_loadedVehiclesAsGameObjects )
            {
                // Early outs for things that should already be setup.

                // get the rigidbody
                if ( !vehicleAsGameObject.TryGetComponent( out Rigidbody rigidbody ) )
                {
                    return;
                }

                // get the child called 'Colliders'
                if ( !vehicleAsGameObject.transform.TraverseHierarchyLookingForTransformWithName( "Colliders", out Transform collidersObj ) )
                {
                    return;
                }

                // get the child called 'Meshes'
                if ( !vehicleAsGameObject.transform.TraverseHierarchyLookingForTransformWithName( "Meshes", out Transform meshesObj ) )
                {
                    return;
                }

                // get the child called 'Body'
                if ( !vehicleAsGameObject.transform.TraverseHierarchyLookingForTransformWithName( "Body", out Transform bodyObj ) )
                {
                    return;
                }

                // Get the body mesh renderer 
                if ( !bodyObj.TryGetComponent( out MeshRenderer bodyMeshRenderer ) )
                {
                    return;
                }

                // NOTE - Sam - 28/03/2024 - This is commented out because I actually don't want to set the material here. I want to set it when the object is created.
                // set the body mesh renderer to have the desired material
                //bodyMeshRenderer.material = VehicleSettings_Prop.BodyMaterial;

                // TODO - Sam - 28/03/2024 - This is gross, definitely need a system to avoid doing this.
                // Set every Transform in the hierarchy to have the 'Player' tag
                vehicleAsGameObject.transform.tag = "Player";
                List<Transform> children = new List<Transform>();
                vehicleAsGameObject.transform.GetAllChildrenOfTransform( ref children );
                foreach ( Transform child in children )
                {
                    child.tag = "Player";
                }

                // set the rigidbody interpolation to interpolate
                rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
                // set the rigidbody collision detection to continuous
                rigidbody.collisionDetectionMode = CollisionDetectionMode.Continuous;

                // add the VehicleController script to the vehicle
                if ( !vehicleAsGameObject.GetComponent<VehicleController>() )
                {
                    vehicleAsGameObject.AddComponent<VehicleController>();
                }

                // add the WheelController script to all children of children of the colliders object
                for ( int i = 0; i < collidersObj.childCount; i++ )
                {
                    Transform child = collidersObj.GetChild( i );

                    // add the WheelController script if it doesn't already exist
                    if ( !child.GetComponent<WheelController>() )
                    {
                        child.gameObject.AddComponent<WheelController>();
                    }

                    WheelController wheelController = child.GetComponent<WheelController>();

                    // set the wheel controller properties
                    wheelController.Motorized = true;
                    if ( child.name    == "FLW"
                         || child.name == "FRW" )
                    {
                        wheelController.Steerable = true;
                    }

                    wheelController.WheelModel = meshesObj.Find( child.name );

                    // get the wheel collider
                    if ( !child.TryGetComponent( out WheelCollider wheelCollider ) )
                    {
                        return;
                    }

                    // set the wheel collider properties
                    wheelCollider.forwardFriction  = VehicleSettings_Prop.ForwardFriction.ToUnityWheelFrictionCurve();
                    wheelCollider.sidewaysFriction = VehicleSettings_Prop.SidewaysFriction.ToUnityWheelFrictionCurve();
                }
            }

            AssetDatabase.ForceReserializeAssets( m_loadedVehiclesAsAssetPaths );
            AssetDatabase.SaveAssets();
        }

        [MenuItem( "Heron/VehicleSetup" )]
        private static void ShowWindow()
        {
            VehicleSetupWindow window = GetWindow<VehicleSetupWindow>();
            window.titleContent = new GUIContent( "VehicleSetup" );
            window.Show();
        }

        #endregion

    }
}