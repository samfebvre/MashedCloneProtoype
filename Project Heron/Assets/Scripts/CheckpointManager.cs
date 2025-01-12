using System;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace.Utils;
using FluffyUnderware.Curvy;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using static RaceManager.ERaceState;

namespace DefaultNamespace
{
    public class CheckpointManager : MonoBehaviour
    {

        #region Serialized

        [FormerlySerializedAs( "CheckpointContainer" )] [SerializeField]
        private Transform m_checkpointContainer;

        [FormerlySerializedAs( "m_lastCheckpoint" )] [SerializeField]
        private GameObject m_firstCheckpoint;

        [SerializeField]
        private List<GameObject> m_checkpoints = new List<GameObject>();

        [SerializeField]
        private CheckpointDebugSettings m_debugSettings;

        [SerializeField] [HideInInspector] private CurvySpline m_trackSpline;

        public float VerticalSplineDisplayOffset = 2.0f;

        #endregion

        #region Public Properties

        public List<Player_Base> AllPlayersSortedByRacePosition { get; } = new List<Player_Base>();

        public IEnumerable<Player_Base> AllValidPlayersSortedByRacePosition =>
            AllPlayersSortedByRacePosition.Where( x => x.PlayerMonoBehaviour != null );

        public GameObject FirstCheckpoint => m_firstCheckpoint;

        public CurvySpline TrackSpline
        {
            get => m_trackSpline;
            private set => m_trackSpline = value;
        }

        #endregion

        #region Public Methods

        public float CalculateDistanceFromPlayerToCheckpoint( Player_Base player_Base, GameObject checkpoint, out Vector3 closestPointOnCheckpoint )
        {
            Vector3 lineStartPnt = checkpoint.transform.position;
            lineStartPnt.y = player_Base.Transform.position.y;

            Vector3 lineDirection = checkpoint.transform.right;
            Vector3 pnt           = player_Base.Transform.position;
            float   lineLength    = checkpoint.transform.localScale.x;

            //Debug.LogWarning($"checkpoint {checkpoint.name} lineLength: " + lineLength);
            //Debug.DrawLine( lineStartPnt - lineDirection * lineLength / 2.0f, lineStartPnt + lineDirection * lineLength / 2.0f, Color.blue );

            Vector3 nearestPointOnLine = MathsUtils.NearestPointOnLineToPoint( lineStartPnt, lineDirection, pnt, lineLength );
            float   distance           = Vector3.Distance( nearestPointOnLine, pnt );
            closestPointOnCheckpoint = nearestPointOnLine;
            return distance;
        }

        public void CreateSplineFromCheckpoints()
        {
            if ( TrackSpline == null )
            {
                TrackSpline = gameObject.AddComponent<CurvySpline>();
            }

            TrackSpline.Clear();

            TrackSpline.Closed             = true;
            TrackSpline.AutoHandleDistance = 0.4f;
            TrackSpline.Interpolation      = CurvyInterpolation.Bezier;
            TrackSpline.Add( m_checkpoints[ 0 ].transform.position + Vector3.up * VerticalSplineDisplayOffset );
            for ( int index = m_checkpoints.Count - 1; index >= 1; index-- )
            {
                GameObject checkpoint = m_checkpoints[ index ];
                TrackSpline.Add( checkpoint.transform.position + Vector3.up * VerticalSplineDisplayOffset );
            }
        }

        public void EstablishCheckpoints()
        {
            // Clear the list of checkpoints
            m_checkpoints.Clear();

            // Loop through all children of the checkpoint container
            foreach ( Transform child in m_checkpointContainer )
            {
                // Add the child to the list of checkpoints
                m_checkpoints.Add( child.gameObject );
            }

            int idxOfFirstCheckpoint = m_checkpoints.IndexOf( FirstCheckpoint );

            // move all of the checkpoints before the first checkpoint to the end of the list
            for ( int i = 0; i < idxOfFirstCheckpoint; i++ )
            {
                GameObject checkpoint = m_checkpoints[ 0 ];
                m_checkpoints.RemoveAt( 0 );
                m_checkpoints.Add( checkpoint );
            }
        }

        public GameObject GetCurrentCheckpointForPlayer( Player_Base player_Base ) => m_playerCurrentCheckpointDict[ player_Base ];

        public int GetCurrentCheckpointIndexForPlayer( Player_Base player_Base ) =>
            m_checkpoints.IndexOf( m_playerCurrentCheckpointDict[ player_Base ] );

        public int GetCurrentLapForPlayer( Player_Base      player_Base ) => m_playerCurrentLapDict[ player_Base ];
        public int GetCurrentPositionForPlayer( Player_Base player_Base ) => m_playerRacePositionDict[ player_Base ];

        public float GetDistanceBetweenCheckpoints( GameObject nextCheckpoint, GameObject getNextCheckpoint ) =>
            Vector3.Distance( nextCheckpoint.transform.position, getNextCheckpoint.transform.position );

        public MathsUtils.Line GetLineDefiningCheckpoint( GameObject checkpoint )
        {
            Vector3 dir       = checkpoint.transform.right;
            float   length    = checkpoint.transform.localScale.x;
            Vector3 lineStart = checkpoint.transform.position - dir * length / 2.0f;
            Vector3 lineEnd   = checkpoint.transform.position + dir * length / 2.0f;

            MathsUtils.Line ret = new MathsUtils.Line( lineStart, lineEnd );
            return ret;
        }

        public GameObject GetNextCheckpoint( GameObject currentCheckpoint )
        {
            int idxOfNextCheckpoint = m_checkpoints.IndexOf( currentCheckpoint ) + 1;
            if ( idxOfNextCheckpoint >= m_checkpoints.Count )
            {
                idxOfNextCheckpoint = 0;
            }

            return m_checkpoints[ idxOfNextCheckpoint ];
        }

        public GameObject GetNextCheckpointForPlayer( Player_Base player_Base )
        {
            GameObject playerCurrentCheckpoint = m_playerCurrentCheckpointDict[ player_Base ];
            return GetNextCheckpoint( playerCurrentCheckpoint );
        }

        public GameObject GetPreviousCheckpoint( GameObject currentCheckpoint )
        {
            int idxOfNextCheckpoint = m_checkpoints.IndexOf( currentCheckpoint ) - 1;
            if ( idxOfNextCheckpoint < 0 )
            {
                idxOfNextCheckpoint = m_checkpoints.Count - 1;
            }

            return m_checkpoints[ idxOfNextCheckpoint ];
        }

        public GameObject GetPreviousCheckpointForPlayer( Player_Base player_Base )
        {
            GameObject playerCurrentCheckpoint = m_playerCurrentCheckpointDict[ player_Base ];
            return GetPreviousCheckpoint( playerCurrentCheckpoint );
        }

        public void Init_ManagerReferences()
        {
            m_gameManager = GameManager.Instance;
            m_raceManager = m_gameManager.RaceManager;
        }

        public void Init_PostReferencesEstablished()
        {
            EstablishCheckpoints();
            ResetPlayerInformation();
        }

        public void OnPlayerEnteredCheckpoint( Player_Base player_Base, GameObject checkpoint )
        {
            // if ( !IsCheckpointValidForPlayer( player_Base, checkpoint ) )
            // {
            //     return;
            // }
            //
            // // Check what the next desired checkpoint should be for the player
            // GameObject nextCheckpoint  = GetNextCheckpointForPlayer( player_Base);
            //
            // // Check whether the checkpoint entered is the same as the next checkpoint
            // if ( checkpoint != nextCheckpoint )
            // {
            //     return;
            // }
            //
            // m_playerCurrentCheckpointDict[ player_Base ] = checkpoint;
            // if ( checkpoint == m_firstCheckpoint )
            // {
            //     //Debug.Log( $"{player_Base.GetGameObject().name} completed lap {m_playerCurrentLapDict[ player_Base ]}" );
            //     m_playerCurrentLapDict[ player_Base ]++;
            // }
        }

        public void OnPlayerExitedCheckpoint( Player_Base player_Base, GameObject checkpoint )
        {
            if ( !IsCheckpointValidForPlayer( player_Base, checkpoint ) )
            {
                return;
            }

            // Get the players position in the checkpoints local space
            Vector3 playerPosInCheckpointSpace = checkpoint.transform.InverseTransformPoint( player_Base.Transform.position );

            // Check if we exited behind the checkpoint
            if ( playerPosInCheckpointSpace.z < 0 )
            {
                // We only permit travelling backwards ONE checkpoint at a time, so if the checkpoint exited is not the current checkpoint, return
                GameObject previousCheckpoint = GetPreviousCheckpointForPlayer( player_Base );
                GameObject currentCheckpoint  = m_playerCurrentCheckpointDict[ player_Base ];
                if ( checkpoint != currentCheckpoint )
                {
                    return;
                }

                // revert to previous checkpoint and decrement lap counter
                m_playerCurrentCheckpointDict[ player_Base ] = previousCheckpoint;
                if ( checkpoint == FirstCheckpoint )
                {
                    m_playerCurrentLapDict[ player_Base ]--;
                }
            }
            // player is ahead of the checkpoint
            else
            {
                // Check what the next desired checkpoint should be for the player
                GameObject nextCheckpoint = GetNextCheckpointForPlayer( player_Base );

                // Check whether the checkpoint exited is the same as the next checkpoint
                if ( checkpoint != nextCheckpoint )
                {
                    return;
                }

                m_playerCurrentCheckpointDict[ player_Base ] = checkpoint;
                if ( checkpoint == FirstCheckpoint )
                {
                    //Debug.Log( $"{player_Base.GetGameObject().name} completed lap {m_playerCurrentLapDict[ player_Base ]}" );
                    m_playerCurrentLapDict[ player_Base ]++;
                }
            }
        }

        public void RecalculateRacePositions()
        {
            AllPlayersSortedByRacePosition.Sort( ComparePlayersByRacePosition );

            for ( int i = 0; i < AllPlayersSortedByRacePosition.Count; i++ )
            {
                Player_Base playerBase = AllPlayersSortedByRacePosition[ i ];
                m_playerRacePositionDict[ playerBase ] = i + 1;
            }
        }

        public void ResetPlayerInformation()
        {
            AllPlayersSortedByRacePosition.Clear();
            AllPlayersSortedByRacePosition.AddRange( m_gameManager.AllPlayers );

            m_playerCurrentCheckpointDict.Clear();
            m_playerCurrentLapDict.Clear();
            m_playerRacePositionDict.Clear();

            foreach ( Player_Base player_Base in AllPlayersSortedByRacePosition )
            {
                m_playerCurrentCheckpointDict.Add( player_Base, m_checkpoints[ ^1 ] );
                m_playerCurrentLapDict.Add( player_Base, 0 );
                m_playerRacePositionDict.Add( player_Base, -1 );
            }
        }

        #endregion

        #region Unity Functions

        private void Update()
        {
            if ( m_raceManager.RaceState is Racing or Countdown or WaitingForRoundCountdownStart or WaitingForNextRound )
            {
                RecalculateRacePositions();
            }
        }

        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if ( !m_debugSettings.Debug_DrawInfoLabels )
            {
                return;
            }

            // for each player, add a handle label above them with their name, checkpoint and lap
            SceneView sceneView = SceneView.currentDrawingSceneView;
            if ( sceneView           == null
                 || sceneView.camera == null )
            {
                return;
            }

            for ( int i = 0; i < m_trackSpline.Length; i++ )
            {
                // // get the distance along the spline for the current point
                float   distance = m_trackSpline.DistanceToTF( i );
                Vector3 point    = m_trackSpline.Interpolate( distance );
                GizmoUtils.DrawALabelBoxWithInfo( point, $"Spline Point {i}" );
            }
        }
        #endif // UNITY_EDITOR

        private void OnValidate()
        {
            EstablishCheckpoints();
        }

        #endregion

        #region Private Fields

        private readonly Dictionary<Player_Base, GameObject> m_playerCurrentCheckpointDict = new Dictionary<Player_Base, GameObject>();
        private readonly Dictionary<Player_Base, int>        m_playerCurrentLapDict        = new Dictionary<Player_Base, int>();
        private readonly Dictionary<Player_Base, int>        m_playerRacePositionDict      = new Dictionary<Player_Base, int>();

        private GameManager m_gameManager;
        private RaceManager m_raceManager;

        #endregion

        #region Private Methods

        private int ComparePlayersByRacePosition( Player_Base x, Player_Base y )
        {
            // handle null cases
            if ( x    == null
                 && y == null )
            {
                return 0;
            }

            if ( x == null )
            {
                return 1;
            }

            if ( y == null )
            {
                return -1;
            }

            // Dead vehicles should be sorted to the end of the list
            if ( x.RaceState    == Player_Base.PlayerRaceState.Dead
                 && y.RaceState == Player_Base.PlayerRaceState.Dead )
            {
                return 0;
            }

            if ( x.RaceState == Player_Base.PlayerRaceState.Dead )
            {
                return 1;
            }

            if ( y.RaceState == Player_Base.PlayerRaceState.Dead )
            {
                return -1;
            }

            // handle the case where the bikes have completed different numbers of laps by checking the bikeLapsCompletedDict
            int xCurrentLap = m_playerCurrentLapDict[ x ];
            int yCurrentLap = m_playerCurrentLapDict[ y ];
            if ( xCurrentLap > yCurrentLap )
            {
                return -1;
            }

            if ( xCurrentLap < yCurrentLap )
            {
                return 1;
            }

            // get the next checkpoint for each bike
            GameObject xNextCheckpoint = GetNextCheckpointForPlayer( x );
            GameObject yNextCheckpoint = GetNextCheckpointForPlayer( y );

            // get the indices of the next checkpoints for each player
            int xNextCheckpointIdx = m_checkpoints.IndexOf( xNextCheckpoint );
            int yNextCheckpointIdx = m_checkpoints.IndexOf( yNextCheckpoint );

            // use the indices to determine which player is further around the track
            if ( xNextCheckpointIdx > yNextCheckpointIdx )
            {
                return -1;
            }

            if ( xNextCheckpointIdx < yNextCheckpointIdx )
            {
                return 1;
            }

            // if the player are at the same checkpoint, use the players distance to the checkpoint to determine which bike is further around the track
            float xDistanceToCheckpoint = CalculateDistanceFromPlayerToCheckpoint( x, xNextCheckpoint, out _ );
            float yDistanceToCheckpoint = CalculateDistanceFromPlayerToCheckpoint( y, yNextCheckpoint, out _ );
            if ( xDistanceToCheckpoint < yDistanceToCheckpoint )
            {
                return -1;
            }

            if ( xDistanceToCheckpoint > yDistanceToCheckpoint )
            {
                return 1;
            }

            // if the bikes are at the same checkpoint and the same distance from the checkpoint, return 0
            return 0;
        }

        private bool IsCheckpointValidForPlayer( Player_Base player_Base, GameObject checkpoint )
        {
            // check if the player is in the dictionary
            // if it isn't, return false
            if ( !m_playerCurrentCheckpointDict.ContainsKey( player_Base ) )
            {
                return false;
            }

            // check if the checkpoint is in m_checkpoints
            // if it isn't, return false
            if ( !m_checkpoints.Contains( checkpoint ) )
            {
                return false;
            }

            // if the current checkpoint for the player is null, return true
            if ( m_playerCurrentCheckpointDict[ player_Base ] == null )
            {
                return true;
            }

            // // check if the checkpoint is the next checkpoint in m_checkpoints
            // // if it is, return true, and if not return false
            // TryGetNextCheckpointForPlayer( player_Base, out GameObject nextCheckpoint );
            // return checkpoint == nextCheckpoint;
            return true;
        }

        #endregion

        [Serializable]
        private struct CheckpointDebugSettings
        {

            #region Serialized

            public bool Debug_DrawInfoLabels;

            #endregion

        }
    }
}