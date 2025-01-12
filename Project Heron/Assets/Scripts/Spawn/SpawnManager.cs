using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DefaultNamespace.Utils;
using FluffyUnderware.Curvy;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

namespace DefaultNamespace
{
    [Serializable]
    public class SpawnManager : MonoBehaviour
    {

        #region Statics and Constants

        private const float MARGIN = 0.1f;

        private const int MAX_SPAWN_ATTEMPT_ITERATIONS = 10000;

        private const string SPAWN_LAYER_NAME = "Spawn";

        private const float BOX_DEPTH                 = 1.0f;
        private const float ITERATION_STEP_HORIZONTAL = 2f;
        private const float ITERATION_STEP_VERTICAL   = 2f;

        private const float TRACK_WIDTH = 10f;

        #endregion

        #region Serialized

        [Header( "Debug" )]
        [SerializeField]
        private SpawnDebugSettings m_spawnDebugSettings;

        [Header( "Spawn placement settings" )]
        [SerializeField]
        private float m_verticalStagger = 1.0f;

        [SerializeField]
        private float m_gap_Between_First_Checkpoint_And_Spawn = 3.0f;

        [SerializeField]
        private float m_horizontalPaddingForVehicleSize = 0.2f;

        [SerializeField]
        private float m_verticalPaddingForVehicleSize = 0.2f;

        [Header( "Box Spawn Settings" )]
        [FormerlySerializedAs( "BoxSpawnParentTransform" )] [SerializeField]
        private Transform m_boxSpawnParentTransform;

        #endregion

        #region Public Properties

        public float HorizontalPaddingForVehicleSize => m_horizontalPaddingForVehicleSize;

        public float VerticalPaddingForVehicleSize => m_verticalPaddingForVehicleSize;

        #endregion

        #region Public Methods

        public void AdjustTrackersIfWidthWillNotFit( ref float lateralTrackerValue, ref float verticalTrackerValue, float boxWidth )
        {
            // if the lateral position tracker is greater than the track width, reset it to 0 and increment the vertical position tracker
            if ( lateralTrackerValue + boxWidth > TRACK_WIDTH )
            {
                lateralTrackerValue  =  0;
                verticalTrackerValue += ITERATION_STEP_VERTICAL;
            }
        }

        public void ClearBoxes()
        {
            foreach ( GameObject box in m_boxInfos.Select( x => x.BoxGameObject ) )
            {
                // If in editor mode, destroy the gameobject
                if ( Application.isEditor )
                {
                    DestroyImmediate( box );
                }
                else
                {

                    Destroy( box );
                }
            }

            m_boxInfos.Clear();
            ResetTrackers();

            for ( int i = 0; i < m_boxSpawnParentTransform.childCount; i++ )
            {
                Transform child = m_boxSpawnParentTransform.GetChild( i );
                // If in editor mode, destroy the gameobject
                if ( Application.isEditor )
                {
                    DestroyImmediate( child.gameObject );
                }
                else
                {

                    Destroy( child.gameObject );
                }
            }
        }

        public void Init()
        {
            // init managers
            Init_ManagerReferences();
            Reset();
        }

        public void Init_ManagerReferences()
        {
            m_gameManager       = GameManager.Instance;
            m_checkpointManager = m_gameManager.CheckPointManager;
        }

        public void Reset()
        {
            ClearBoxes();
        }

        public void TryCreateRandomlySizedSpawnBox()
        {
            // come up with some random width and height
            float boxWidth = Random.Range( 1.0f, 3.0f );
            // height should be at least as big as the width
            float boxHeight = Random.Range( boxWidth, 10.0f );

            TryGetSpawnPositionAndRotationForBoxOfSize( boxWidth, boxHeight, out Vector3 pos, out Quaternion rot );
        }

        public void TryCreateSpawnBoxFromVehiclePrefab( int vehiclePrefabIdx )
        {
            // clamp the vehicle prefab index
            vehiclePrefabIdx = Mathf.Clamp( vehiclePrefabIdx, 0, m_gameManager.VehiclePrefabs.Count - 1 );
            GameObject vehiclePrefab = m_gameManager.GetVehiclePrefab( vehiclePrefabIdx );
            // get vehicle controller from the prefab
            VehicleController vehicleController = vehiclePrefab.GetComponent<VehicleController>();
            // get the size of the vehicle collider
            Vector3 vehicleColliderSize = vehicleController.GetVehicleColliderSize();
            TryGetSpawnPositionAndRotationForVehicleOfSize( vehicleColliderSize.x, vehicleColliderSize.z, out Vector3 spawnPosition,
                out Quaternion rotation );
        }

        public bool TryGetSpawnPositionAndRotationForVehicleOfSize( float vehicleWidth, float vehicleHeight, out Vector3 pos, out Quaternion rot )
        {
            float boxWidth  = vehicleWidth  + m_horizontalPaddingForVehicleSize;
            float boxHeight = vehicleHeight + m_verticalPaddingForVehicleSize;
            return TryGetSpawnPositionAndRotationForBoxOfSize( boxWidth, boxHeight, out pos, out rot );
        }

        #endregion

        #region Private Fields

        [SerializeField] [HideInInspector]
        private List<BoxInfo> m_boxInfos = new List<BoxInfo>();

        private CheckpointManager m_checkpointManager;

        private GameManager m_gameManager;

        private float m_lateralPositionTracker;

        private Collider[] m_results = new Collider[ 10 ];

        private float m_verticalPositionTracker;

        #endregion

        #region Private Methods

        private float CalculateInterpolationValue( float verticalPositionTrackerValue, float boxHeight )
        {
            float       boxHeightContribution = boxHeight * 0.5f;
            float       sumOfContributions    = verticalPositionTrackerValue + boxHeightContribution;
            CurvySpline trackSpline           = m_checkpointManager.TrackSpline;
            return trackSpline.DistanceToTF( sumOfContributions );
        }

        private bool CalculateSpawnPositionAndRotation( float          boxWidth, float boxHeight, out float newLateralPositionTrackerValue,
                                                        out float      newVerticalPositionTrackerValue, out Vector3 pos,
                                                        out Quaternion rot )
        {
            newVerticalPositionTrackerValue = m_verticalPositionTracker;
            newLateralPositionTrackerValue  = m_lateralPositionTracker;

            AdjustTrackersIfWidthWillNotFit( ref newLateralPositionTrackerValue, ref newVerticalPositionTrackerValue, boxWidth );

            pos = default;
            rot = default;
            int iterationCount = 0;
            while ( iterationCount < MAX_SPAWN_ATTEMPT_ITERATIONS )
            {
                if ( CalculateSpawnPositionAndRotation_OneStep( boxWidth, boxHeight, ref newLateralPositionTrackerValue,
                        ref newVerticalPositionTrackerValue,              out pos,   out rot ) )
                {
                    SpawnBox( boxWidth, boxHeight, newLateralPositionTrackerValue, newVerticalPositionTrackerValue, pos, rot, true );
                    IncreaseLateralTracker_Reactively( ref newLateralPositionTrackerValue, boxWidth );
                    return true;
                }

                SpawnBox( boxWidth, boxHeight, newLateralPositionTrackerValue, newVerticalPositionTrackerValue, pos, rot, false );
                IncreaseLateralTracker_Proactively( ref newLateralPositionTrackerValue, ref newVerticalPositionTrackerValue, boxWidth,
                    ITERATION_STEP_HORIZONTAL );

                // while loop exit condition
                iterationCount++;
            }

            return false;
        }

        private bool CalculateSpawnPositionAndRotation_OneStep( float       boxWidth, float boxHeight, ref float newLateralPositionTrackerValue,
                                                                ref float   newVerticalPositionTrackerValue,
                                                                out Vector3 spawnPos, out Quaternion rot )
        {
            CurvySpline trackSpline = m_checkpointManager.TrackSpline;

            float   interpolationVal = CalculateInterpolationValue( newVerticalPositionTrackerValue, boxHeight );
            Vector3 interpolatedPos  = trackSpline.Interpolate( interpolationVal );
            // add the vertical offset to the interpolated position
            interpolatedPos -= Vector3.up * m_checkpointManager.VerticalSplineDisplayOffset;

            rot = CalculateSpawnRotation( newVerticalPositionTrackerValue, boxHeight );
            // calculate the offset from the interpolated position to reach the edge of the track
            Vector3 reachEdgeOfTrackOffset = rot * Vector3.right * -TRACK_WIDTH / 2.0f;
            // Calculate the offset from the edge of the track using the lateral position tracker
            Vector3 lateralPositionTrackerOffset = rot * Vector3.right * newLateralPositionTrackerValue;
            // Calculate the offset from the edge of the track using the new box width halved
            Vector3 boxWidthOffset = rot * Vector3.right * boxWidth / 2.0f;

            // calculate the spawn position
            spawnPos = interpolatedPos + reachEdgeOfTrackOffset + lateralPositionTrackerOffset + boxWidthOffset;

            // check if the box is colliding with anything
            int spawnLayerMask = LayerMask.GetMask( SPAWN_LAYER_NAME );
            int size = Physics.OverlapBoxNonAlloc( spawnPos, new Vector3( boxWidth / 2.0f, BOX_DEPTH, boxHeight / 2.0f ), m_results, rot,
                spawnLayerMask );
            return size <= 0;
        }

        private Quaternion CalculateSpawnRotation( float verticalPositionTrackerValue, float boxHeight )
        {
            CurvySpline trackSpline      = m_checkpointManager.TrackSpline;
            float       interpolationVal = CalculateInterpolationValue( verticalPositionTrackerValue, boxHeight );
            Quaternion  orientation      = trackSpline.GetOrientationFast( interpolationVal );
            // rotate the orientation by 180 degrees around the up axis using AngleAxis
            return orientation * Quaternion.AngleAxis( 180, Vector3.up );
        }

        private void IncreaseLateralTracker_Proactively( ref float lateralTrackerValue, ref float verticalTrackerValue, float boxWidth, float amount )
        {
            lateralTrackerValue += amount;
            AdjustTrackersIfWidthWillNotFit( ref lateralTrackerValue, ref verticalTrackerValue, boxWidth );
        }

        private void IncreaseLateralTracker_Reactively( ref float lateralTrackerValue, float amount )
        {
            lateralTrackerValue += amount;
        }

        private void ResetTrackers()
        {
            m_lateralPositionTracker  = 0;
            m_verticalPositionTracker = m_gap_Between_First_Checkpoint_And_Spawn;
        }

        private void SpawnBox( float width, float height, float lateralPositionTracker, float verticalPositionTracker, Vector3 pos, Quaternion rot,
                               bool  valid )
        {
            pos += Vector3.up * BOX_DEPTH / 2.0f;

            // spawn a gameobject for the box
            GameObject box = new GameObject
            {
                transform =
                {
                    position = pos,
                    rotation = rot,
                    parent   = m_boxSpawnParentTransform,
                },
            };

            // add a box collider to the box for drawing and collision purposes
            BoxCollider boxCollider = box.AddComponent<BoxCollider>();
            boxCollider.isTrigger = true;
            boxCollider.size      = new Vector3( width, BOX_DEPTH, height );

            // add a volume drawing component
            DrawCollider colliderDrawer = box.AddComponent<DrawCollider>();

            if ( valid )
            {
                // set the layer of the box collider so that it will be considered for overlaps
                box.layer = LayerMask.NameToLayer( SPAWN_LAYER_NAME );
            }

            // determine if the box should be drawn by checking if it is valid and if the debug setting is enabled
            bool shouldDraw = valid ? m_spawnDebugSettings.Debug_DrawValidSpawnBoxes : m_spawnDebugSettings.Debug_DrawInvalidSpawnBoxes;
            colliderDrawer.Draw = shouldDraw;
            Color drawColor =
                valid ? ColorUtils.RandomPleasantColor() : ColorUtils.ConvertSystemDrawingColorToUnityColor( System.Drawing.Color.Gray );
            drawColor.a              = 0.5f;
            colliderDrawer.DrawColor = drawColor;

            string boxValidityString = valid ? "(Valid) Spawn Box" : "(Invalid) Spawn Box";
            string boxName           = boxValidityString + $" {m_boxInfos.Count}";

            // set the name of the box
            box.name = boxName;

            BoxInfo boxInfo = new BoxInfo
            {
                Position                = pos,
                Rotation                = rot,
                LateralPositionTracker  = lateralPositionTracker,
                VerticalPositionTracker = verticalPositionTracker,
                BoxWidth                = width,
                BoxHeight               = height,
                Valid                   = valid,
                BoxGameObject           = box,
                BoxName                 = boxName,
                ColliderDrawer          = colliderDrawer,
            };

            m_boxInfos.Add( boxInfo );
        }

        private bool TryGetSpawnPositionAndRotationForBoxOfSize( float boxWidth, float boxHeight, out Vector3 pos, out Quaternion rot )
        {
            if ( !CalculateSpawnPositionAndRotation( boxWidth, boxHeight, out float newLateralPositionTrackerValue,
                    out float newVerticalPositionTrackerValue, out pos,   out rot ) )
            {
                return false;
            }

            m_verticalPositionTracker = newVerticalPositionTrackerValue;
            m_lateralPositionTracker  = newLateralPositionTrackerValue;

            // Add some offsets to the trackers
            m_verticalPositionTracker += m_verticalStagger;
            IncreaseLateralTracker_Proactively( ref m_lateralPositionTracker, ref m_verticalPositionTracker, boxWidth, MARGIN );
            return true;
        }

        #endregion

        public struct BoxInfo
        {

            #region Public Fields

            public GameObject   BoxGameObject;
            public float        BoxHeight;
            public string       BoxName;
            public float        BoxWidth;
            public DrawCollider ColliderDrawer;
            public float        LateralPositionTracker;

            public Vector3    Position;
            public Quaternion Rotation;
            public bool       Valid;
            public float      VerticalPositionTracker;

            #endregion

            #region Public Methods

            public void DrawBoxLabel()
            {
                #if UNITY_EDITOR
                StringBuilder boxInfoStringBuilder = new StringBuilder();
                boxInfoStringBuilder.AppendLine( $"Vertical Position Tracker: {VerticalPositionTracker}" );
                boxInfoStringBuilder.AppendLine( $"Lateral Position Tracker: {LateralPositionTracker}" );
                boxInfoStringBuilder.AppendLine( $"Position: {Position}" );
                boxInfoStringBuilder.AppendLine( $"Rotation: {Rotation.eulerAngles}" );
                boxInfoStringBuilder.AppendLine( $"Box Width: {BoxWidth}" );
                boxInfoStringBuilder.AppendLine( $"Box Height: {BoxHeight}" );
                boxInfoStringBuilder.AppendLine( $"Valid: {Valid}" );
                string boxInfoStringBuilderString = boxInfoStringBuilder.ToString();
                boxInfoStringBuilderString = boxInfoStringBuilderString.TrimEnd( '\n' );
                GizmoUtils.DrawALabelBoxWithInfo( Position, boxInfoStringBuilderString, fontSize: 8 );
                #endif
            }

            #endregion

        }

        [Serializable]
        private struct SpawnDebugSettings
        {

            #region Serialized

            [Header( "Valid Spawn Boxes" )]
            public bool Debug_DrawValidSpawnBoxes;

            public bool Debug_AddLabelsToValidSpawnBoxes;

            [Header( "Invalid Spawn Boxes" )]
            public bool Debug_DrawInvalidSpawnBoxes;

            public bool Debug_AddLabelsToInvalidSpawnBoxes;

            #endregion

        }

        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            foreach ( BoxInfo boxInfo in m_boxInfos.Where( ShouldDrawBoxLabels ) )
            {
                boxInfo.DrawBoxLabel();
            }
        }

        private void OnValidate()
        {
            foreach ( BoxInfo boxInfo in m_boxInfos )
            {
                bool shouldDraw = ShouldDrawBox( boxInfo );
                boxInfo.ColliderDrawer.Draw = shouldDraw;
            }
        }

        private bool ShouldDrawBox( BoxInfo boxInfo ) =>
            boxInfo.Valid ? m_spawnDebugSettings.Debug_DrawValidSpawnBoxes : m_spawnDebugSettings.Debug_DrawInvalidSpawnBoxes;

        private bool ShouldDrawBoxLabels( BoxInfo boxInfo ) => boxInfo.Valid
            ? m_spawnDebugSettings.Debug_AddLabelsToValidSpawnBoxes
            : m_spawnDebugSettings.Debug_AddLabelsToInvalidSpawnBoxes;

        #endif // UNITY_EDITOR
    }
}