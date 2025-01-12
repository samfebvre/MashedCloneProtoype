using System;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace;
using UnityEngine;

public class RaceManager : MonoBehaviour
{

    #region Statics and Constants

    private const float START_ROUND_COUNTDOWN_DURATION = 3;

    private const float WAITING_FOR_NEXT_ROUND_DURATION = 3;

    private const float PRE_COUNTDOWN_START_DURATION = 2;

    private const float SCORE_TO_WIN = 3;

    private const float WAITING_FOR_NEXT_RACE_DURATION = 5;

    #endregion

    #region Serialized

    [SerializeField]
    private RaceDebugSettings m_debugSettings;

    #endregion

    #region Public Enums

    public enum ERaceState
    {
        None,
        WaitingForRoundCountdownStart,
        Countdown,
        Racing,
        WaitingForNextRound,
        Finished,
    }

    #endregion

    #region Public Properties

    public List<Player_Base>        AllPlayers      => m_gameManager.AllPlayers;
    public IEnumerable<Player_Base> AllValidPlayers => m_gameManager.AllValidPlayers;

    public ERaceState RaceState { get; private set; } = ERaceState.None;

    public IEnumerable<Player_Base> ValidCurrentlyRacingPlayers => AllValidPlayers.Where( x => x.RaceState == Player_Base.PlayerRaceState.Racing );
    public IEnumerable<Player_Base> ValidNonDeadPlayers         => AllValidPlayers.Where( x => x.RaceState != Player_Base.PlayerRaceState.Dead );

    #endregion

    #region Public Methods

    public void FinishRace()
    {
        RaceState = ERaceState.Finished;
        UIManager.SetStatusText( "Race Finished!" );

        // foreach ( Player_Base player_Base in AllPlayers )
        // {
        //     player_Base.RaceState = Player_Base.PlayerRaceState.Finished;
        // }

        m_waitingForNextRaceTimeRemaining = m_debugSettings.Debug_QuickTimers ? 0.1f : WAITING_FOR_NEXT_RACE_DURATION;
    }

    public void Init_ManagerReferences()
    {
        m_gameManager       = GameManager.Instance;
        m_spawnManager      = m_gameManager.SpawnManager;
        m_checkpointManager = m_gameManager.CheckPointManager;
        m_cameraController  = m_gameManager.CameraController;
    }

    public void OnPlayerDied()
    {
        if ( RaceState != ERaceState.Racing )
        {
            return;
        }

        // If there are no players left, start waiting for the next round
        if ( !ValidCurrentlyRacingPlayers.Any() )
        {
            StartWaitingForNextRound();
            return;
        }

        // If we have only one player left, they win the round
        if ( ValidCurrentlyRacingPlayers.Count() != 1 )
        {
            return;
        }

        Player_Base winner = ValidCurrentlyRacingPlayers.First();
        winner.RaceState = Player_Base.PlayerRaceState.WonRound;
        OnPlayerWonRound( winner );
    }

    public void OnPlayerWonRound( Player_Base playerBase )
    {
        playerBase.Score++;
        if ( playerBase.Score >= SCORE_TO_WIN )
        {
            FinishRace();
        }
        else
        {
            StartWaitingForNextRound();
        }
    }

    public void StartCountdown()
    {
        RaceState                      = ERaceState.Countdown;
        m_startRoundCountdownRemaining = m_debugSettings.Debug_QuickTimers ? 0.1f : START_ROUND_COUNTDOWN_DURATION;
    }

    public void StartRace()
    {
        // Reset player score
        foreach ( Player_Base player_Base in AllPlayers )
        {
            player_Base.Score = 0;
        }

        GoToNextRound();
    }

    public void Update()
    {
        switch ( RaceState )
        {
            case ERaceState.None:
                break;
            case ERaceState.WaitingForRoundCountdownStart:
                DoWaitingForRoundCountdownStart();
                break;
            case ERaceState.Countdown:
                DoCountdown();
                break;
            case ERaceState.Racing:
                break;
            case ERaceState.WaitingForNextRound:
                DoWaitingForNextRound();
                break;
            case ERaceState.Finished:
                DoWaitForNextRace();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    #endregion

    #region Private Fields

    private CameraController m_cameraController;

    private CheckpointManager m_checkpointManager;

    private GameManager m_gameManager;

    private float m_preCountdownStartTimeRemaining;

    private SpawnManager m_spawnManager;
    private float        m_startRoundCountdownRemaining;
    private float        m_waitingForNextRaceTimeRemaining;
    private float        m_waitingForNextRoundRemaining;

    #endregion

    #region Private Properties

    private UIManager UIManager => GameManager.Instance.UIManager;

    #endregion

    #region Private Methods

    private void DoCountdown()
    {
        m_startRoundCountdownRemaining -= Time.deltaTime;
        UIManager.SetStatusText( $"Starting in: {m_startRoundCountdownRemaining:F1}" );
        if ( m_startRoundCountdownRemaining <= 0 )
        {
            StartRound();
        }
    }

    private void DoWaitForNextRace()
    {
        m_waitingForNextRaceTimeRemaining -= Time.deltaTime;
        UIManager.SetStatusText( $"Waiting for next race to start {m_waitingForNextRaceTimeRemaining:F1}" );

        if ( m_waitingForNextRaceTimeRemaining <= 0 )
        {
            StartRace();
        }
    }

    private void DoWaitingForNextRound()
    {
        m_waitingForNextRoundRemaining -= Time.deltaTime;
        UIManager.SetStatusText( $"Waiting for next round to start: {m_waitingForNextRoundRemaining:F1}" );
        if ( m_waitingForNextRoundRemaining <= 0 )
        {
            GoToNextRound();
        }
    }

    private void DoWaitingForRoundCountdownStart()
    {
        m_preCountdownStartTimeRemaining -= Time.deltaTime;
        UIManager.SetStatusText( $"Waiting for round countdown to start: {m_preCountdownStartTimeRemaining:F1}" );
        if ( m_preCountdownStartTimeRemaining <= 0 )
        {
            StartCountdown();
        }
    }

    private void GoToNextRound()
    {
        RaceState                        = ERaceState.WaitingForRoundCountdownStart;
        m_preCountdownStartTimeRemaining = m_debugSettings.Debug_QuickTimers ? 0.1f : PRE_COUNTDOWN_START_DURATION;

        m_spawnManager.Reset();
        foreach ( Player_Base player_Base in AllPlayers )
        {
            player_Base.ResetPlayer();
            player_Base.DestroyVehicleObject();

            GameObject        vehiclePrefab         = m_gameManager.GetRandomVehiclePrefab();
            VehicleController vehicleController     = vehiclePrefab.GetComponent<VehicleController>();
            Vector3           vehicleColliderSize   = vehicleController.GetVehicleColliderSize();
            Vector3           vehicleColliderCentre = vehicleController.GetVehicleColliderCentre();

            if ( m_spawnManager.TryGetSpawnPositionAndRotationForVehicleOfSize( vehicleColliderSize.x, vehicleColliderSize.z, out Vector3 position,
                    out Quaternion rotation ) )
            {
                // Offset the position using the vehicleColliderCentre to get the correct position.
                // Make sure to rotate the position by the rotation to get it into the correct space.
                // Do not add any vertical offset.
                position -= rotation * new Vector3( vehicleColliderCentre.x, 0, vehicleColliderCentre.z );

                m_gameManager.SetPlayerUpWithVehicle( player_Base, vehiclePrefab, position, rotation );
                player_Base.RaceState = Player_Base.PlayerRaceState.WaitingToStartRound;
            }
            else
            {
                Debug.LogError( "Failed to get spawn position and rotation for player" );
                return;
            }
        }

        // Reset player positions
        // TeleportPlayersToStartPositions();

        // Reset checkpoint information
        m_checkpointManager.ResetPlayerInformation();
        m_checkpointManager.RecalculateRacePositions();

        // Reset camera
        m_cameraController.SnapCamera();
    }

    private void ResetAllPlayersVelocityAndForces()
    {
        foreach ( Player_Base player_Base in AllPlayers )
        {
            player_Base.PlayerMonoBehaviour.Rigidbody.velocity = Vector3.zero;
            player_Base.PlayerMonoBehaviour.ResetWheelForces();
        }
    }

    private void StartRound()
    {
        RaceState = ERaceState.Racing;
        UIManager.SetStatusText( "Racing!" );

        foreach ( Player_Base player_Base in AllPlayers )
        {
            player_Base.RaceState = Player_Base.PlayerRaceState.Racing;
        }
    }

    private void StartWaitingForNextRound()
    {
        RaceState                      = ERaceState.WaitingForNextRound;
        m_waitingForNextRoundRemaining = m_debugSettings.Debug_QuickTimers ? 0.1f : WAITING_FOR_NEXT_ROUND_DURATION;
    }

    #endregion

    [Serializable]
    private struct RaceDebugSettings
    {

        #region Serialized

        public bool Debug_QuickTimers;

        #endregion

    }
}