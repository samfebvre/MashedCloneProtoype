using System;
using System.Linq;
using Cinemachine;
using DefaultNamespace.Utils;
using UnityEngine;

namespace DefaultNamespace
{
    public class CameraController
    {

        #region Statics and Constants

        private const float TARGET_TRACKER_POSITION_SMOOTH_TIME = 0.2f;
        private const float TARGET_TRACKER_ROTATION_SMOOTH_TIME = 0.2f;
        private const float TARGET_TRACKER_POSITION_MAX_SPEED   = 100;
        private const float TARGET_TRACKER_ROTATION_MAX_SPEED   = 100;

        private const float CAMERA_KILL_ZONE_BOTTOM = 0.1f;
        private const float CAMERA_KILL_ZONE_SIDES  = 0.1f;

        private const float CAMERA_ZOOM_DIST_AT_MIN_ZOOM = 5.0f;
        private const float CAMERA_ZOOM_DIST_AT_MAX_ZOOM = 60.0f;
        private const float CAMERA_ZOOM_MIN              = 0.7f;
        private const float CAMERA_ZOOM_MAX              = 2.2f;
        private const float CAMERA_ZOOM_SMOOTH_TIME      = 0.2f;
        private const float CAMERA_ZOOM_MAX_SPEED        = 100;

        #endregion

        #region Public Methods

        public void Init( CinemachineVirtualCamera virtualCamera )
        {
            m_gameManager       = GameManager.Instance;
            m_raceManager       = m_gameManager.RaceManager;
            m_checkpointManager = m_gameManager.CheckPointManager;
            m_camera            = Camera.main;

            m_virtualCamera     = virtualCamera;
            m_transposer        = m_virtualCamera.GetCinemachineComponent<CinemachineTransposer>();
            m_startFollowOffset = m_transposer.m_FollowOffset;

            m_tracker            = new GameObject( "Camera Controller Target" ).transform;
            virtualCamera.Follow = m_tracker;
            virtualCamera.LookAt = m_tracker;
        }

        public void SetTargetPositionToFocusBetweenFirstAndLastPlayer()
        {
            TryGetPlayerInFirstPlace( out Player_Base firstPlacePlayer );
            if ( !TryGetNonDeadPlayerInLastPlace( out Player_Base lastPlacePlayer ) )
            {
                m_targetTrackerPosition = firstPlacePlayer.Transform.position;
                return;
            }

            m_targetTrackerPosition = ( lastPlacePlayer.Transform.position + firstPlacePlayer.Transform.position ) / 2;
        }

        public void SnapCamera()
        {
            SetTargetPositionToFocusBetweenFirstAndLastPlayer();
            UpdateTrackerTargetRotation();
            UpdateTargetZoom();

            m_tracker.position        = m_targetTrackerPosition;
            m_tracker.rotation        = m_targetTrackerRotation;
            m_trackerPositionVelocity = Vector3.zero;
            m_trackerRotationVelocity = Quaternion.identity;
            m_zoomVal                 = m_targetZoom;
            m_zoomVel                 = 0;
        }

        public void Update()
        {
            switch ( m_raceManager.RaceState )
            {
                case RaceManager.ERaceState.None:
                    break;
                case RaceManager.ERaceState.WaitingForRoundCountdownStart:
                case RaceManager.ERaceState.Countdown:
                case RaceManager.ERaceState.Racing:
                case RaceManager.ERaceState.WaitingForNextRound:
                case RaceManager.ERaceState.Finished:
                    DoPositionAndRotationUpdates();
                    DoZoomUpdates();
                    DoKillPlayers();
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void UpdateTargetZoom()
        {
            // get the distance between the first and last player
            TryGetPlayerInFirstPlace( out Player_Base playerInFirstPlace );
            if ( !TryGetNonDeadPlayerInLastPlace( out Player_Base playerInLastPlace ) )
            {
                m_targetZoom                              = CAMERA_ZOOM_MIN;
                m_atMaxDistanceBetweenFirstAndLastPlayers = false;
                return;
            }

            float dist = Vector3.Distance( playerInFirstPlace.Transform.position, playerInLastPlace.Transform.position );
            float t    = Mathf.InverseLerp( CAMERA_ZOOM_DIST_AT_MIN_ZOOM, CAMERA_ZOOM_DIST_AT_MAX_ZOOM, dist );
            m_targetZoom = Mathf.Lerp( CAMERA_ZOOM_MIN, CAMERA_ZOOM_MAX, t );

            m_atMaxDistanceBetweenFirstAndLastPlayers = dist >= CAMERA_ZOOM_DIST_AT_MAX_ZOOM;
        }

        public void UpdateTrackerTargetRotation()
        {
            // Get the player in first
            TryGetPlayerInFirstPlace( out Player_Base firstPlacePlayer );

            // Get all their relevant checkpoints
            GameObject nextCheckpoint    = m_checkpointManager.GetNextCheckpointForPlayer( firstPlacePlayer );
            GameObject prevCheckpoint    = m_checkpointManager.GetPreviousCheckpointForPlayer( firstPlacePlayer );
            GameObject currentCheckpoint = m_checkpointManager.GetCurrentCheckpointForPlayer( firstPlacePlayer );

            // first check if the player is actually BEHIND their current checkpoint - in which case we use the current checkpoint and the previous checkpoint
            Vector3 playerPosInCheckpointSpace = currentCheckpoint.transform.InverseTransformPoint( firstPlacePlayer.Transform.position );

            GameObject checkpointInFront;
            GameObject checkpointBehind;

            if ( playerPosInCheckpointSpace.z < 0 )
            {
                checkpointInFront = currentCheckpoint;
                checkpointBehind  = prevCheckpoint;
            }
            else
            {
                checkpointInFront = nextCheckpoint;
                checkpointBehind  = currentCheckpoint;
            }

            float distToCheckpointInFront = m_checkpointManager.CalculateDistanceFromPlayerToCheckpoint( firstPlacePlayer, checkpointInFront, out _ );
            float distToCheckpointBehind  = m_checkpointManager.CalculateDistanceFromPlayerToCheckpoint( firstPlacePlayer, checkpointBehind,  out _ );

            float sumOfDistances   = distToCheckpointInFront + distToCheckpointBehind;
            float progressionValue = distToCheckpointBehind / sumOfDistances;

            Quaternion rotationOfCheckpointInFront = checkpointInFront.transform.rotation;
            Quaternion rotationOfCheckpointBehind  = checkpointBehind.transform.rotation;

            Quaternion interpolatedRotation = Quaternion.Slerp( rotationOfCheckpointBehind, rotationOfCheckpointInFront, progressionValue );

            m_targetTrackerRotation = interpolatedRotation;
        }

        #endregion

        #region Private Fields

        private bool m_atMaxDistanceBetweenFirstAndLastPlayers;

        private Camera m_camera;

        private CheckpointManager m_checkpointManager;

        private GameManager m_gameManager;

        private float       m_originalXDamping;
        private float       m_originalYawDamping;
        private float       m_originalYDamping;
        private float       m_originalZDamping;
        private RaceManager m_raceManager;

        private Coroutine m_revertCameraToPreviousDampingSettingsCoroutine;

        private Vector3 m_startFollowOffset;

        private Vector3    m_targetTrackerPosition;
        private Quaternion m_targetTrackerRotation;

        private float m_targetZoom;

        private Transform  m_tracker;
        private Vector3    m_trackerPositionVelocity;
        private Quaternion m_trackerRotationVelocity;

        private CinemachineTransposer    m_transposer;
        private CinemachineVirtualCamera m_virtualCamera;
        private float                    m_zoomVal;

        private float m_zoomVel;

        #endregion

        #region Private Methods

        private void DoKillPlayers()
        {
            if ( !m_atMaxDistanceBetweenFirstAndLastPlayers )
            {
                return;
            }

            KillPlayersTooCloseToTheEdgesOfTheScreen();
        }

        private void DoPositionAndRotationUpdates()
        {
            SetTargetPositionToFocusBetweenFirstAndLastPlayer();
            UpdateTrackerTargetRotation();

            SmoothDampTrackerPosition();
            SmoothDampTrackerRotation();
        }

        private void DoZoomUpdates()
        {
            UpdateTargetZoom();

            SmoothDampZoomAndUpdateCamera();
        }

        private void KillPlayersTooCloseToTheEdgesOfTheScreen()
        {
            foreach ( Player_Base nonDeadPlayer in m_raceManager.ValidNonDeadPlayers )
            {
                Vector3 viewportPoint = m_camera.WorldToViewportPoint( nonDeadPlayer.Transform.position );
                if ( viewportPoint.y < CAMERA_KILL_ZONE_BOTTOM )
                {
                    nonDeadPlayer.KillPlayer();
                }

                if ( viewportPoint.x is < CAMERA_KILL_ZONE_SIDES or > 1 - CAMERA_KILL_ZONE_SIDES )
                {
                    nonDeadPlayer.KillPlayer();
                }
            }
        }

        private void SmoothDampTrackerPosition()
        {
            m_tracker.position = Vector3.SmoothDamp( m_tracker.position, m_targetTrackerPosition, ref m_trackerPositionVelocity,
                TARGET_TRACKER_POSITION_SMOOTH_TIME,
                TARGET_TRACKER_POSITION_MAX_SPEED );
        }

        private void SmoothDampTrackerRotation()
        {
            m_tracker.rotation = MathsUtils.QuaternionSmoothDamp( m_tracker.rotation, m_targetTrackerRotation, ref m_trackerRotationVelocity,
                TARGET_TRACKER_ROTATION_SMOOTH_TIME,
                TARGET_TRACKER_ROTATION_MAX_SPEED );
        }

        private void SmoothDampZoomAndUpdateCamera()
        {
            m_zoomVal                   = Mathf.SmoothDamp( m_zoomVal, m_targetZoom, ref m_zoomVel, CAMERA_ZOOM_SMOOTH_TIME, CAMERA_ZOOM_MAX_SPEED );
            m_transposer.m_FollowOffset = m_startFollowOffset * m_zoomVal;
        }

        private bool TryGetNonDeadPlayerInLastPlace( out Player_Base last )
        {
            last = m_checkpointManager.AllValidPlayersSortedByRacePosition.LastOrDefault( x => x.RaceState != Player_Base.PlayerRaceState.Dead );
            return last != null;
        }

        private bool TryGetPlayerInFirstPlace( out Player_Base first )
        {
            first = m_checkpointManager.AllValidPlayersSortedByRacePosition.FirstOrDefault();
            return first != null;
        }

        #endregion

    }
}