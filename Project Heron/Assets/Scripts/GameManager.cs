using System;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace.Utils;
using UnityEngine;
using Random = UnityEngine.Random;

namespace DefaultNamespace
{
    public class GameManager : MonoBehaviour
    {

        #region Statics and Constants

        private static GameManager sm_instance;

        #endregion

        #region Serialized

        [SerializeField] private int m_numberOfHumanPlayers;
        [SerializeField] private int m_numberOfAIPlayers;

        [Header( "Managers" )]
        [SerializeField] private SpawnManager m_spawnManager;

        [SerializeField] private RaceManager       m_raceManager;
        [SerializeField] private CheckpointManager m_checkPointManager;

        [SerializeField]
        private LevelReferenceManager m_levelReferenceManager;

        [SerializeField]
        private List<GameObject> m_vehiclePrefabs;

        [SerializeField] private Material m_vehicleColorRemapMaterial;

        public PlayerDebugSettings PlayerDebugSettings;

        #endregion

        #region Public Properties

        public List<Player_Base>        AllPlayers      { get; } = new List<Player_Base>();
        public IEnumerable<Player_Base> AllValidPlayers => AllPlayers.Where( x => x.PlayerMonoBehaviour != null );

        public CameraController CameraController { get; private set; }

        public CheckpointManager CheckPointManager => m_checkPointManager;

        public static GameManager Instance
        {
            get
            {
                if ( sm_instance != null )
                {
                    return sm_instance;
                }

                sm_instance = FindObjectOfType<GameManager>();
                return sm_instance;
            }

            private set => sm_instance = value;
        }

        public LevelReferenceManager LevelReferenceManager => m_levelReferenceManager;

        public int NumberOfAIPlayers => m_numberOfAIPlayers;

        public int         NumberOfHumanPlayers => m_numberOfHumanPlayers;
        public RaceManager RaceManager          => m_raceManager;

        public SpawnManager SpawnManager => m_spawnManager;
        public UIManager    UIManager    { get; private set; }

        public List<GameObject> VehiclePrefabs => m_vehiclePrefabs;

        #endregion

        #region Public Methods

        public GameObject GetRandomVehiclePrefab()
        {
            int randomIndex = Random.Range( 0, VehiclePrefabs.Count );
            return VehiclePrefabs[ randomIndex ];
        }

        public GameObject GetVehiclePrefab( int i ) => VehiclePrefabs[ i ];

        public void SetPlayerUpWithVehicle( Player_Base player_Base, GameObject vehiclePrefab, Vector3 spawnPosition, Quaternion rotation )
        {
            // instantiate the vehicle prefab
            Player_MonoBehaviour player_Mono = Instantiate( vehiclePrefab, spawnPosition, rotation ).AddComponent<Player_MonoBehaviour>();
            player_Base.Init_MonoBehaviour( player_Mono, m_vehicleColorRemapMaterial );
        }

        #endregion

        #region Unity Functions

        private void Awake()
        {
            Instance = this;

            // call Random.InitState to ensure that the random seed is different each time the game is run
            Random.InitState( (int)DateTime.Now.Ticks );

            UIManager        = new UIManager();
            CameraController = new CameraController();

            RaceManager.Init_ManagerReferences();
            CheckPointManager.Init_ManagerReferences();
            SpawnManager.Init();
            InitAllPlayers();
            CheckPointManager.Init_PostReferencesEstablished();
            CameraController.Init( LevelReferenceManager.FollowCamera );
        }

        private void Start()
        {
            RaceManager.StartRace();
        }

        private void Update()
        {
            RaceManager.Update();
            CameraController.Update();
        }

        private void FixedUpdate()
        {
            foreach ( Player_Base player in AllValidPlayers )
            {
                player.OnFixedUpdate();
            }
        }

        #if  UNITY_EDITOR
        private void OnDrawGizmos()
        {
            foreach ( Player_Base player in AllValidPlayers )
            {
                player.OnDrawGizmos();
            }
        }
        #endif
        
        #endregion

        #region Private Methods

        private void InitAllPlayers()
        {
            for ( int i = 0; i < NumberOfHumanPlayers; i++ )
            {
                InitAPlayer<Player_Human>();
            }

            for ( int i = 0; i < NumberOfAIPlayers; i++ )
            {
                InitAPlayer<Player_AI>();
            }
        }

        private void InitAPlayer<T>() where T : Player_Base, new()
        {
            T     player_Base = new T();
            Color playerCol   = ColorUtils.RandomPleasantColor();
            player_Base.Init_Player( $"Player {AllPlayers.Count}", playerCol );
            AllPlayers.Add( player_Base );
        }

        #endregion

    }
}