#if UNITY_EDITOR 

using System;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace.Utils;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Shapes;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace DefaultNamespace
{
    public class RectPacker : MonoBehaviour
    {

        #region Statics and Constants

        private const float CORNER_RADIUS                               = 15f;
        private const float SPAWN_ROPE_DELAY_ON_SCREEN_WIDTH_STABILISED = 0.1f;

        #endregion

        #region Serialized

        [Header( "Rects - General" )]
        [SerializeField]
        private bool m_spreadRectsOut;

        [SerializeField]
        private float m_dragValue = 0.1f;

        [Header( "Rects - Move Towards Target" )]
        [SerializeField]
        private float m_desiredDistanceFromTarget_Base = 200f;

        [SerializeField]
        private float m_restoreToDesiredPositionBaseAcceleration = 0.1f;

        [SerializeField]
        private MoveTowardsDesiredRectMode m_moveTowardsDesiredRectMode;

        [Header( "Rects - Move away from other rects" )]
        [SerializeField]
        private float m_moveAwayFromOtherRectsBaseAcceleration = 0.1f;

        [Header( "Ropes - General" )]
        [SerializeField]
        private int m_ropes_NumberOfPoints;

        [SerializeField]
        private float m_ropes_DesiredStickLength;

        [Header( "Ropes - Debug" )]
        [SerializeField]
        private Vector2 m_ropes_Debug_Start1;

        [SerializeField]
        private Vector2 m_ropes_Debug_End1;

        [SerializeField]
        private Vector2 m_ropes_Debug_Start2;

        [SerializeField]
        private Vector2 m_ropes_Debug_End2;

        [Header( "Ropes - Simulation" )]
        [SerializeField]
        private float m_ropes_GrabDist = 50f;

        [SerializeField]
        private int m_ropes_NumberOfSimulationIterationsPerTimeStep = 1;

        [SerializeField]
        private float m_ropes_DeltaTimeMultiplier = 1f;

        [SerializeField]
        private int m_ropes_NumberOfStickIterations = 10;

        [SerializeField]
        private float m_ropes_Gravity = 9.8f;

        [SerializeField]
        private float m_ropes_Damping = 0.99f;

        [SerializeField]
        private float m_ropes_TangentLength = 2f;

        [Header("Draw Stuff")]
        [SerializeField]
        private bool m_drawStuff;

        [Header( "Drawing - Rects" )]
        [SerializeField] 
        private bool m_rects_Draw_Handle;

        [SerializeField]
        private bool m_rects_Draw_Shape;

        [SerializeField]
        private bool m_rects_Draw_Desired;

        [SerializeField]
        private bool m_rects_Draw_Actual;

        [FormerlySerializedAs( "m_ropes_Drawing_PointRadius" )]
        [Header( "Drawing - Ropes" )]
        [SerializeField]
        private float m_ropes_Draw_PointRadius = 5f;

        [FormerlySerializedAs( "m_ropes_Drawing_LineThickness" )] [SerializeField]
        private float m_ropes_Draw_LineThickness = 5f;

        [FormerlySerializedAs( "m_ropes_Drawing_TangentThickness" )] [SerializeField]
        private float m_ropes_Draw_TangentThickness = 2f;

        [FormerlySerializedAs( "m_ropes_PolylineJoins" )] [SerializeField]
        private PolylineJoins m_ropes_Draw_PolylineJoins;

        [SerializeField]
        private bool m_ropes_Draw_DebugRopes;

        [SerializeField]
        private bool m_ropes_Draw_RopeSticks;

        [SerializeField]
        private bool m_ropes_Draw_RopePolyline;

        [SerializeField]
        private bool m_ropes_Draw_RopePoints;

        [SerializeField]
        private bool m_ropes_Draw_RopeTangents;

        #endregion

        #region Public Enums

        [Serializable]
        public enum MoveTowardsDesiredRectMode
        {
            GoFromEdges,
            GoFromCentres,
        }

        #endregion

        #region Public Methods

        public void Awake()
        {
            m_gameManager = GameManager.Instance;
            m_raceManager = m_gameManager.RaceManager;

            m_miniRingAnimation = DOTween
                                  .To( getter: () => m_miniRingAnimationValue, setter: x => m_miniRingAnimationValue = x, endValue: 1, duration: 1f )
                                  .SetLoops( loops: -1, loopType: LoopType.Yoyo )
                                  .SetEase( Ease.InOutQuad );

            m_currentFrameTime       = DateTime.Now;
            m_lastFrameTime          = m_currentFrameTime;
            m_screenWidthStableState = ScreenWidthStableState.Unstable;

        }

        public Vector3 ConvertScreenToViewport(
            Vector2 screenPos )
        {
            Vector3 viewportPoint = CurrentSceneCam.ScreenToViewportPoint( screenPos );
            viewportPoint.z = GizmoUtils.GetZValJustInFrontOfNearClipPlane();
            return viewportPoint;
        }

        public Vector3 ConvertScreenToWorld(
            Vector2 screenPos )
        {
            Vector3 viewportPos = ConvertScreenToViewport( screenPos );
            Vector3 worldPos    = CurrentSceneCam.ViewportToWorldPoint( viewportPos );
            return worldPos;
        }

        public void OnDrawGizmos()
        {
            // Delta time calculation
            m_currentFrameTime = DateTime.Now;
            m_deltaTime        = (float)( m_currentFrameTime - m_lastFrameTime ).TotalSeconds;

            SpawnDebugRopes_IfScreenIsStable();

            // Drawing logic
            if ( CanDraw() )
            {
                UpdateValidPlayerRectsCollection();
                UpdatePositionsOfActualRects();
                UpdateRopesEndPositions();
                SimulateRopes( ropes: PlayerRectRopes, deltaTime: m_deltaTime );
                DrawRopes( ropes: PlayerRectRopes, colors: PlayerColors );
                //DrawLinesFromActualRectsToDesiredRects();
                ReorderPlayerRectsAndDrawThem();
            }

            // Debug Ropes
            if ( m_ropes_Draw_DebugRopes && EditorApplication.isPlaying )
            {
                UpdateDebugRopes( ropes: m_debugRopes, deltaTime: m_deltaTime );
                DrawRopes( m_debugRopes );
            }

            m_lastFrameTime = m_currentFrameTime;
        }

        public Matrix4x4 WorldMatrixWithRotationToCamera(
            Vector3 worldPos,
            bool    flatten = false )
        {
            Quaternion rot                            = flatten ? GetRotationToFaceCamera_Flattened() : GetRotationToFaceCamera( worldPos );
            Matrix4x4  translateToWorldPosMatrix      = Matrix4x4.Translate( worldPos );
            Matrix4x4  faceCameraRotationMatrix       = Matrix4x4.Rotate( rot );
            Matrix4x4  translateBackToOriginPosMatrix = Matrix4x4.Translate( -worldPos );

            Matrix4x4 combinedMatrix = translateToWorldPosMatrix * faceCameraRotationMatrix * translateBackToOriginPosMatrix;
            return combinedMatrix;
        }

        #endregion

        #region Unity Functions

        private void OnDestroy()
        {
            m_miniRingAnimation.Kill();
        }

        #endregion

        #region Private Enums

        private enum ScreenWidthStableState
        {
            None,
            Unstable,
            PreStable,
            FullyStable,
        }

        #endregion

        #region Private Fields

        private readonly List<RopePhysics.Rope> m_debugRopes = new List<RopePhysics.Rope>();

        private readonly Dictionary<Player_Base, RectsAndRope> m_playerRectsAndRopeDict =
            new Dictionary<Player_Base, RectsAndRope>();

        private DateTime    m_currentFrameTime;
        private float       m_deltaTime;
        private GameManager m_gameManager;
        private Player_Base m_hoveredPlayer;
        private DateTime    m_lastFrameTime;

        private float                                   m_lastScreenWidth;
        private TweenerCore<float, float, FloatOptions> m_miniRingAnimation;

        private float       m_miniRingAnimationValue;
        private Vector2     m_mousePosLastFrame;
        private RaceManager m_raceManager;

        private float m_screenWidthPreStableTimestamp;

        private ScreenWidthStableState m_screenWidthStableState;

        private RopePhysics.RopePoint m_selectedRopePoint;
        private GameObject            m_testRect;

        #endregion

        #region Private Properties

        private Camera CurrentSceneCam => SceneView.currentDrawingSceneView.camera;

        private bool DraggingHoveredRect             { get; set; }
        private bool IsScreenWidthTheSameAsLastFrame => Math.Abs( m_lastScreenWidth - SceneViewScreenWidth ) < 0.01f;

        private float        LineThicknessInCameraSpace => ConvertScreenDistanceToWorldDistance( m_ropes_Draw_LineThickness );
        private Vector2      MouseDelta                 => Event.current.mousePosition - m_mousePosLastFrame;
        private IList<Color> PlayerColors               => m_playerRectsAndRopeDict.Keys.Select( x => x.PlayerColor ).ToList();

        private IList<RopePhysics.Rope> PlayerRectRopes       => m_playerRectsAndRopeDict.Values.Select( x => x.Rope ).ToList();
        private float                   SceneViewScreenHeight => SceneView.currentDrawingSceneView.position.height;

        private float SceneViewScreenWidth => SceneView.currentDrawingSceneView.position.width;

        #endregion

        #region Private Methods

        private void AddPlayerToRectsAndRopeDict(
            Player_Base player )
        {
            ActualRectAndDesiredRect rectsStruct = new ActualRectAndDesiredRect
            {
                DesiredRect = player.GetDebugLabelRectInfo().LabelRect,
                ActualRect  = player.GetDebugLabelRectInfo().LabelRect,
            };

            RopePhysics.Rope rope = SpawnRope( start: rectsStruct.DesiredRect.center,
                                               end: rectsStruct.ActualRect.GetEdgeCenter( Edge.Top )
                                                    + Vector2.up * m_ropes_Draw_LineThickness * 0.5f );
            RectsAndRope rectsAndRope = new RectsAndRope( rects: rectsStruct, rope: rope );

            m_playerRectsAndRopeDict[ player ] = rectsAndRope;
        }

        //private float CalcLineThicknessInCameraSpace() => ConvertScreenDistanceToCameraSpace( LineThickness );

        private bool CanDraw()
        {
            if ( !m_drawStuff )
            {
                return false;
            }

            if ( m_gameManager    == null
                 || m_raceManager == null )
            {
                return false;
            }

            if ( !SceneViewValidityCheck() )
            {
                return false;
            }

            return true;
        }

        private Vector2 CheckOverlapBetweenTwoRects(
            Rect rect1,
            Rect rect2 )
        {
            float xOverlap = Mathf.Min( a: rect1.xMax, b: rect2.xMax ) - Mathf.Max( a: rect1.xMin, b: rect2.xMin );
            float yOverlap = Mathf.Min( a: rect1.yMax, b: rect2.yMax ) - Mathf.Max( a: rect1.yMin, b: rect2.yMin );
            return new Vector2( x: xOverlap, y: yOverlap );
        }

        private void ClampToScreen(
            ref ActualRectAndDesiredRect playerRectsStruct )
        {
            // clamp the rects to the screen and reset velocity in the direction that was clamped
            const float X_MARGIN = 0;
            const float Y_MARGIN = 50;
            if ( playerRectsStruct.ActualRect.xMin < 0 + X_MARGIN )
            {
                playerRectsStruct.ActualRect.x = 0 + X_MARGIN;
            }

            if ( playerRectsStruct.ActualRect.xMax > Screen.width - X_MARGIN )
            {
                playerRectsStruct.ActualRect.x = Screen.width - ( playerRectsStruct.ActualRect.width + X_MARGIN );
            }

            if ( playerRectsStruct.ActualRect.yMin < 0 + Y_MARGIN )
            {
                playerRectsStruct.ActualRect.y = 0 + Y_MARGIN;
            }

            if ( playerRectsStruct.ActualRect.yMax > Screen.height - Y_MARGIN )
            {
                playerRectsStruct.ActualRect.y = Screen.height - ( playerRectsStruct.ActualRect.height + Y_MARGIN );
            }
        }

        private Vector2 ConvertMouseDeltaToScreenSpace(
            Vector2 mouseDelta )
        {
            Vector2 ret = mouseDelta;
            ret.y = -ret.y;
            return ret;
        }

        private float ConvertScreenDistanceToWorldDistance(
            float screenDistance )
        {
            Vector3 p1   = ConvertScreenToWorld( new Vector2( x: 0,              y: 0 ) );
            Vector3 p2   = ConvertScreenToWorld( new Vector2( x: screenDistance, y: 0 ) );
            float   dist = Vector3.Distance( a: p1, b: p2 );
            return dist;
        }

        // private void DrawLineFromActualRectToDesiredRect(
        //     Player_Base player )
        // {
        //     ActualRectAndDesiredRect rectsStruct   = m_validPlayerRectDict[ player ];
        //     Vector2                  actualCentre  = rectsStruct.ActualRect.center;
        //     Vector2                  desiredCentre = rectsStruct.DesiredRect.center;
        //
        //     Vector2     diff       = desiredCentre - actualCentre;
        //     float       diffMag    = diff.magnitude;
        //     const float MULTIPLIER = 1f;
        //
        //     Vector2 tangent1 = actualCentre  + Vector2.down * diffMag * MULTIPLIER;
        //     Vector2 tangent2 = desiredCentre + Vector2.down * diffMag * MULTIPLIER;
        //
        //     PolylinePath polylinePath = new PolylinePath();
        //
        //     Vector3 actualCentreInCameraSpace  = ConvertScreenPointToCameraSpace( actualCentre );
        //     Vector3 desiredCentreInCameraSpace = ConvertScreenPointToCameraSpace( desiredCentre );
        //     Vector3 tangent1InCameraSpace      = ConvertScreenPointToCameraSpace( tangent1 );
        //     Vector3 tangent2InCameraSpace      = ConvertScreenPointToCameraSpace( tangent2 );
        //
        //     Color desiredColor = player.PlayerColor.WithAlpha( 1 );
        //     Color actualColor  = m_hoveredPlayer == player ? GetColorForHoveredPlayer( player ).WithAlpha( 1 ) : desiredColor;
        //
        //     PolylinePoint actualCentrePoint = new PolylinePoint( actualCentreInCameraSpace )
        //     {
        //         color = actualColor,
        //     };
        //
        //     PolylinePoint desiredCentrePoint = new PolylinePoint( desiredCentreInCameraSpace )
        //     {
        //         color = desiredColor,
        //     };
        //
        //     polylinePath.AddPoint( actualCentrePoint );
        //     polylinePath.BezierTo( startTangent: tangent1InCameraSpace, endTangent: tangent2InCameraSpace, end: desiredCentrePoint );
        //
        //     Matrix4x4 matrixBefore = Shapes.Draw.Matrix;
        //
        //     Shapes.Draw.Matrix = GetMatrixToConvertFromCameraSpaceToWorldSpace(Vector2.zero);
        //     Shapes.Draw.Polyline( path: polylinePath, thickness: CalcLineThicknessInCameraSpace() );
        //     polylinePath.Dispose();
        //
        //     Shapes.Draw.Disc( pos: actualCentrePoint.point,  radius: CalcLineThicknessInCameraSpace() / 2f, colors: actualColor );
        //     Shapes.Draw.Disc( pos: desiredCentrePoint.point, radius: CalcLineThicknessInCameraSpace() / 2f, colors: desiredColor );
        //
        //     float ringRadius = CalcLineThicknessInCameraSpace() * 1.0f;
        //     ringRadius *= 1 + m_miniRingAnimationValue * 0.5f;
        //
        //     float ringThickness = CalcLineThicknessInCameraSpace() * 0.75f;
        //     ringThickness *= 1 - m_miniRingAnimationValue * 0.5f;
        //     Shapes.Draw.Ring( pos: desiredCentrePoint.point, radius: ringRadius, thickness: ringThickness, colors: desiredColor );
        //     Shapes.Draw.Matrix = matrixBefore;
        // }
        //
        // private void DrawLinesFromActualRectsToDesiredRects()
        // {
        //     Draw.PolylineGeometry = PolylineGeometry.Flat2D;
        //     // draw lines from the centre of each Actual rect to the centre of the Desired rect
        //     foreach ( Player_Base player in m_validPlayerRectDict.Keys )
        //     {
        //         DrawLineFromActualRectToDesiredRect( player );
        //     }
        // }

        private void DrawPlayerRects(
            Player_Base player,
            bool        hovered )
        {
            RectsAndRope rectsAndRope = m_playerRectsAndRopeDict[ player ];
            Rect         actualRect   = rectsAndRope.Rects.ActualRect;
            Rect         desiredRect  = rectsAndRope.Rects.DesiredRect;

            if ( m_rects_Draw_Actual )
            {
                DrawRect( player: player, rect: actualRect, hovered: hovered );
            }

            if ( m_rects_Draw_Desired )
            {
                DrawRect( player: player, rect: desiredRect, hovered: false );
            }
        }

        private void DrawRect(
            Player_Base player,
            Rect        rect,
            bool        hovered )
        {
            Color playerColor = player.PlayerColor;
            if ( hovered )
            {
                playerColor = GetColorForHoveredPlayer( player );
            }

            if ( m_rects_Draw_Shape )
            {
                DrawRectAsShapesRect( rect: rect, col: playerColor );
            }

            Handles.BeginGUI();
            rect = GizmoUtils.ConvertFromScreenSpaceToGUISpace( rect );
            if ( m_rects_Draw_Handle )
            {
                Handles.DrawSolidRectangleWithOutline( rectangle: rect, faceColor: playerColor, outlineColor: playerColor );
            }

            // draw the player's name in the rect
            string     playerName  = player.PlayerName;
            GUIContent playerLabel = new GUIContent( playerName );
            GUIStyle playerLabelStyle = new GUIStyle
            {
                fontSize = GizmoUtils.GetFontSizeScaledForWorldPosition( fontSize: 16, worldPosition: player.PlayerMonoBehaviour.transform.position ),
                fontStyle = FontStyle.Bold,
                normal =
                {
                    textColor = ColorUtils.GetWhiteOrBlackContrastColor( playerColor ),
                },
                alignment = TextAnchor.MiddleCenter,
            };
            GUI.Label( position: rect, content: playerLabel, style: playerLabelStyle );
            Handles.EndGUI();
        }

        private void DrawRectAsShapesRect(
            Rect  rect,
            Color col )
        {

            Vector2 rectCentre_Screen = rect.center;
            Vector3 rectCentre_World  = ConvertScreenToWorld( rectCentre_Screen );

            float cameraSpaceWidth  = ConvertScreenDistanceToWorldDistance( rect.width );
            float cameraSpaceHeight = ConvertScreenDistanceToWorldDistance( rect.height );
            float cameraSpaceRadius = ConvertScreenDistanceToWorldDistance( CORNER_RADIUS );

            Matrix4x4 matrixBefore = Draw.Matrix;
            Draw.Matrix = WorldMatrixWithRotationToCamera( worldPos: rectCentre_World, flatten: true );
            Draw.Rectangle( pos: rectCentre_World, width: cameraSpaceWidth, height: cameraSpaceHeight, cornerRadius: cameraSpaceRadius,
                            color: col.WithAlpha( 1f ) );
            Draw.Matrix = matrixBefore;
        }

        private void DrawRope(
            RopePhysics.Rope rope,
            Color            color )
        {
            if ( rope.RopePoints.Count <= 1 )
            {
                return;
            }

            float discRadiusCameraSpace           = ConvertScreenDistanceToWorldDistance( m_ropes_Draw_PointRadius );
            float tangentLineThicknessCameraSpace = ConvertScreenDistanceToWorldDistance( m_ropes_Draw_TangentThickness );

            PolylinePath polylinePath = new PolylinePath();

            // I need to calculate tangents for each point in the rope. I will do this by averaging the difference vectors between the points.
            // First, iterate over the list of points and store the difference vectors in a list
            List<Vector2> diffVectors = new List<Vector2>();
            for ( int i = 0; i < rope.RopePoints.Count - 1; i++ )
            {
                Vector2 diff = rope.RopePoints[ i + 1 ].Position - rope.RopePoints[ i ].Position;
                diffVectors.Add( diff );
            }

            // Now, iterate over the list of difference vectors and calculate the average difference vector between each point
            List<Vector2> averageDiffVectors = new List<Vector2>();
            for ( int i = 0; i < diffVectors.Count - 1; i++ )
            {
                Vector2 averageDiff = ( diffVectors[ i ] + diffVectors[ i + 1 ] ) / 2f;
                averageDiffVectors.Add( averageDiff );
            }

            List<RopePointTangentPair> tangentPairs = new List<RopePointTangentPair>();

            // Calculate the tangents for the first point
            RopePointTangentPair firstTangentPair = new RopePointTangentPair
            {
                Point             = rope.RopePoints[ 0 ].Position,
                TangentPointAfter = rope.RopePoints[ 0 ].Position + diffVectors[ 0 ] / m_ropes_TangentLength,
            };

            tangentPairs.Add( firstTangentPair );

            // Calculate the tangent points for the all the points in the middle
            for ( int i = 1; i < rope.RopePoints.Count - 1; i++ )
            {
                RopePointTangentPair tangentPair = new RopePointTangentPair
                {
                    Point              = rope.RopePoints[ i ].Position,
                    TangentPointBefore = rope.RopePoints[ i ].Position - averageDiffVectors[ i - 1 ] / m_ropes_TangentLength,
                    TangentPointAfter  = rope.RopePoints[ i ].Position + averageDiffVectors[ i - 1 ] / m_ropes_TangentLength,
                };

                tangentPairs.Add( tangentPair );
            }

            // Calculate the tangents for the last point
            RopePointTangentPair lastTangentPair = new RopePointTangentPair
            {
                Point              = rope.RopePoints[ ^1 ].Position,
                TangentPointBefore = rope.RopePoints[ ^1 ].Position - diffVectors[ ^1 ] / m_ropes_TangentLength,
            };

            tangentPairs.Add( lastTangentPair );

            // Add all the points to the polyline path
            Vector3 firstPointInWorldSpace = ConvertScreenToWorld( rope.RopePoints[ 0 ].Position );
            polylinePath.AddPoint( firstPointInWorldSpace );

            for ( int index = 0; index < tangentPairs.Count - 1; index++ )
            {
                RopePointTangentPair thisPair = tangentPairs[ index ];
                RopePointTangentPair nextPair = tangentPairs[ index + 1 ];

                Vector3 tangent_This_After_World  = ConvertScreenToWorld( thisPair.TangentPointAfter );
                Vector3 tangent_Next_Before_World = ConvertScreenToWorld( nextPair.TangentPointBefore );
                Vector3 point_Next_World          = ConvertScreenToWorld( nextPair.Point );

                polylinePath.BezierTo( startTangent: tangent_This_After_World, endTangent: tangent_Next_Before_World, end: point_Next_World );
            }

            // Draw Bezier curve
            if ( m_ropes_Draw_RopePolyline )
            {
                Draw.PolylineGeometry = PolylineGeometry.Billboard;
                Draw.Polyline( path: polylinePath, thickness: LineThicknessInCameraSpace, joins: m_ropes_Draw_PolylineJoins, color: color );
            }

            polylinePath.Dispose();

            // Draw all the tangents as lines
            if ( m_ropes_Draw_RopeTangents )
            {

                DrawTangentPair( tangentPair: tangentPairs[ 0 ], before: false );

                for ( int index = 1; index < tangentPairs.Count - 1; index++ )
                {
                    RopePointTangentPair tangentPair = tangentPairs[ index ];
                    DrawTangentPair( tangentPair );
                }

                DrawTangentPair( tangentPair: tangentPairs[ ^1 ], before: true, after: false );

                void DrawTangentPair(
                    RopePointTangentPair tangentPair,
                    bool                 before = true,
                    bool                 after  = true )
                {
                    Vector3 point_World        = ConvertScreenToWorld( tangentPair.Point );
                    Vector3 point_Before_World = ConvertScreenToWorld( tangentPair.TangentPointBefore );
                    Vector3 point_After_World  = ConvertScreenToWorld( tangentPair.TangentPointAfter );

                    if ( before )
                    {
                        Draw.Line( start: point_World,
                                   end: point_Before_World, thickness: tangentLineThicknessCameraSpace, color: Color.blue );
                    }

                    if ( after )
                    {
                        Draw.Line( start: point_World,
                                   end: point_After_World, thickness: tangentLineThicknessCameraSpace, color: Color.blue );
                    }
                }
            }

            // Draw the points as white discs
            if ( m_ropes_Draw_RopePoints )
            {
                Draw.DiscGeometry = DiscGeometry.Billboard;
                foreach ( RopePhysics.RopePoint ropePoint in rope.RopePoints )
                {
                    Vector3 worldPos = ConvertScreenToWorld( ropePoint.Position );
                    Draw.Disc( pos: worldPos, radius: discRadiusCameraSpace, colors: Color.white );
                }
            }

            // Draw sticks as straight lines
            if ( m_ropes_Draw_RopeSticks )
            {
                // Draw all the sticks
                foreach ( RopePhysics.RopeStick ropeStick in rope.RopeSticks )
                {
                    Vector2 pos_A = ropeStick.PointA.Position;
                    Vector2 pos_B = ropeStick.PointB.Position;

                    Vector3 worldPos_A = ConvertScreenToWorld( pos_A );
                    Vector3 worldPos_B = ConvertScreenToWorld( pos_B );

                    Draw.LineGeometry = LineGeometry.Billboard;
                    Draw.Line( start: worldPos_A,
                               end: worldPos_B, thickness: LineThicknessInCameraSpace,
                               color: Color.green );

                }
            }
        }

        private void DrawRopes(
            IList<RopePhysics.Rope> ropes,
            IList<Color>            colors = null )
        {
            if ( colors != null )
            {
                for ( int i = 0; i < ropes.Count; i++ )
                {
                    DrawRope( rope: ropes[ i ], color: colors[ i ] );
                }
            }
            else
            {
                foreach ( RopePhysics.Rope rope in ropes )
                {
                    DrawRope( rope: rope, color: Color.red );
                }
            }
        }

        private Color GetColorForHoveredPlayer(
            Player_Base player )
        {
            Color playerColor = player.PlayerColor;
            Color.RGBToHSV( rgbColor: playerColor, H: out float h, S: out float s, V: out float v );
            //s -= 0.1f;
            v           += 0.3f;
            playerColor =  Color.HSVToRGB( H: h, S: s, V: v );
            return playerColor;
        }

        private float GetHandleSizeScaleFactor(
            Player_Base player ) =>
            HandleUtility.GetHandleSize(
                player.PlayerMonoBehaviour.transform.position );

        private Quaternion GetRotationToFaceCamera(
            Vector3 worldPos )
        {
            Vector3    diff    = worldPos - CurrentSceneCam.transform.position;
            Quaternion lookRot = Quaternion.LookRotation( forward: diff, upwards: CurrentSceneCam.transform.up );
            return lookRot;
        }

        private Quaternion GetRotationToFaceCamera_Flattened()
        {
            Quaternion lookRot = Quaternion.LookRotation( forward: CurrentSceneCam.transform.forward, upwards: CurrentSceneCam.transform.up );
            return lookRot;
        }

        private void InteractWithOtherRects(
            ref ActualRectAndDesiredRect playerRectsStruct,
            float                        handleSizeScaleFactor,
            IEnumerable<Player_Base>     otherPlayersWithValidRects )
        {
            foreach ( Player_Base otherPlayer in otherPlayersWithValidRects )
            {
                // Rects
                RectsAndRope             rectsAndRope     = m_playerRectsAndRopeDict[ otherPlayer ];
                ActualRectAndDesiredRect rects            = rectsAndRope.Rects;
                Rect                     otherActualRect  = rects.ActualRect;
                Rect                     otherDesiredRect = rects.DesiredRect;

                MoveAwayFromRect( playerRectsStruct: ref playerRectsStruct, handleScaleFactor: handleSizeScaleFactor, otherRect: otherActualRect );
                MoveAwayFromRect( playerRectsStruct: ref playerRectsStruct, handleScaleFactor: handleSizeScaleFactor, otherRect: otherDesiredRect );
            }
        }

        private bool IsAnyOfRectVisibleOnScreen(
            Rect rect )
        {
            // Check if ANY of the rect is visible on screen
            bool isVisible = rect.xMin    < Screen.width
                             && rect.xMax > 0
                             && rect.yMin < Screen.height
                             && rect.yMax > 0;

            return isVisible;
        }

        private void MatchSizeOfDesiredRect(
            ref ActualRectAndDesiredRect playerRectsStruct )
        {
            playerRectsStruct.ActualRect.width  = playerRectsStruct.DesiredRect.width;
            playerRectsStruct.ActualRect.height = playerRectsStruct.DesiredRect.height;
        }

        private void MoveAwayFromRect(
            ref ActualRectAndDesiredRect playerRectsStruct,
            float                        handleScaleFactor,
            Rect                         otherRect )
        {
            bool    actuallyOverlapping = playerRectsStruct.ActualRect.Overlaps( otherRect );
            Rect    thisActualRect      = playerRectsStruct.ActualRect;
            Vector2 posDiff             = thisActualRect.position - otherRect.position;
            Vector2 overlap             = CheckOverlapBetweenTwoRects( rect1: thisActualRect, rect2: otherRect );

            // compare distances between minimums and maximums to find the closest point
            float yDist = Mathf.Min( a: Mathf.Abs( thisActualRect.yMin - otherRect.yMax ), b: Mathf.Abs( thisActualRect.yMax - otherRect.yMin ) );
            float xDist = Mathf.Min( a: Mathf.Abs( thisActualRect.xMin - otherRect.xMax ), b: Mathf.Abs( thisActualRect.xMax - otherRect.xMin ) );

            if ( !actuallyOverlapping )
            {
                // clamp the distance to a minimum value
                xDist = Mathf.Clamp( value: xDist, min: 1f, max: float.MaxValue );
                yDist = Mathf.Clamp( value: yDist, min: 1f, max: float.MaxValue );
            }

            // zero out larger value
            if ( yDist > xDist )
            {
                yDist = 0;
            }
            else
            {
                xDist = 0;
            }

            // If the rectangles are not even aligned, then we don't need to move them
            if ( overlap.x < 0 )
            {
                yDist = 0;
            }

            if ( overlap.y < 0 )
            {
                xDist = 0;
            }

            if ( actuallyOverlapping )
            {
                //clamp the distance to a minimum value
                xDist = Mathf.Clamp( value: xDist, min: 0, max: 1f );
                yDist = Mathf.Clamp( value: yDist, min: 0, max: 1f );
            }

            // We lost the sign of the distance when we took the absolute value, so we need to get it back
            float xDir = Mathf.Sign( posDiff.x );
            float yDir = Mathf.Sign( posDiff.y );

            Vector2 vec  = new Vector2( x: xDist * xDir, y: yDist * yDir );
            float   dist = vec.magnitude;
            dist = Mathf.Clamp( value: dist, min: 10, max: float.MaxValue );
            const float POWER = 2;
            dist = Mathf.Pow( f: dist, p: POWER );

            // calculate the acceleration
            const float EASE_OF_USE_MULTIPLIER = 100000f;
            float       accel                  = m_moveAwayFromOtherRectsBaseAcceleration / ( handleScaleFactor * dist );
            accel *= EASE_OF_USE_MULTIPLIER;

            // apply the acceleration
            playerRectsStruct.ActualRectVelocity += vec.normalized * accel * m_deltaTime;
        }

        private void MoveTowardsDesiredPosition(
            ref ActualRectAndDesiredRect playerRectsStruct,
            float                        handleSizeScaleFactor )
        {
            Vector2 difVec = default;

            switch ( m_moveTowardsDesiredRectMode )
            {
                case MoveTowardsDesiredRectMode.GoFromEdges:
                    difVec = MoveTowardsDesiredPosition_FromEdges( playerRectsStruct );
                    break;
                case MoveTowardsDesiredRectMode.GoFromCentres:
                    difVec = MoveTowardsDesiredPosition_FromCentres( playerRectsStruct );
                    break;
            }

            float difVecMag = difVec.magnitude;

            // Scale desired distance based on distance from camera
            float desiredDistanceFromTarget_PostScaling = m_desiredDistanceFromTarget_Base
                                                          / handleSizeScaleFactor;

            // Do some math to determine the acceleration multiplier
            float accelMultiplier;
            if ( difVecMag < desiredDistanceFromTarget_PostScaling )
            {
                float val  = Mathf.InverseLerp( a: 0, b: desiredDistanceFromTarget_PostScaling, value: difVecMag );
                float val2 = Mathf.Lerp( a: 1, b: 0, t: val );
                accelMultiplier = -val2;
            }
            else
            {
                accelMultiplier = difVecMag / desiredDistanceFromTarget_PostScaling;
            }

            difVec = difVec.normalized * accelMultiplier;
            const float MULTIPLIER_FOR_EASE_OF_USE = 10000f;
            difVec *= m_restoreToDesiredPositionBaseAcceleration * MULTIPLIER_FOR_EASE_OF_USE;

            // apply the force by modifying the velocity
            playerRectsStruct.ActualRectVelocity += difVec * m_deltaTime;
        }

        private Vector2 MoveTowardsDesiredPosition_FromCentres(
            ActualRectAndDesiredRect playerRectsStruct ) =>
            playerRectsStruct.DesiredRect.center - playerRectsStruct.ActualRect.center;

        private Vector2 MoveTowardsDesiredPosition_FromEdges(
            ActualRectAndDesiredRect playerRectsStruct )
        {
            // Iterate through each of the Actual Rect edge centres calculating their distance from the DesiredRect position
            // Top
            EdgePair topEdge = new EdgePair(
                pos: new Vector2( x: playerRectsStruct.ActualRect.center.x, y: playerRectsStruct.ActualRect.yMax ),
                otherPos: playerRectsStruct.DesiredRect.center, name: "Top" );

            // Bottom
            EdgePair bottomEdge =
                new EdgePair( pos: new Vector2( x: playerRectsStruct.ActualRect.center.x, y: playerRectsStruct.ActualRect.yMin ),
                              otherPos: playerRectsStruct.DesiredRect.center, name: "Bottom" );

            // Left
            EdgePair leftEdge = new EdgePair(
                pos: new Vector2( x: playerRectsStruct.ActualRect.xMin, y: playerRectsStruct.ActualRect.center.y ),
                otherPos: playerRectsStruct.DesiredRect.center, name: "Left" );

            // Right
            EdgePair rightEdge =
                new EdgePair( pos: new Vector2( x: playerRectsStruct.ActualRect.xMax, y: playerRectsStruct.ActualRect.center.y ),
                              otherPos: playerRectsStruct.DesiredRect.center, name: "Right" );

            // Pick the edge that is closest to the DesiredRect centre
            List<EdgePair> edgePairList = new List<EdgePair> { topEdge, bottomEdge, leftEdge, rightEdge };
            edgePairList = edgePairList.OrderBy( x => x.Dist ).ToList();
            EdgePair actualRectClosestEdgePair   = edgePairList[ 0 ];
            Vector2  actualRectClosestEdgeCentre = actualRectClosestEdgePair.Centre;

            // log the closest edge
            //Debug.Log( $"Closest edge to {player.PlayerName} is {actualRectClosestEdgeName}" );
            // Calculate vector from the closest edge to the DesiredRect position
            Vector2 ret = playerRectsStruct.DesiredRect.center - actualRectClosestEdgeCentre;

            // If we are inside the DesiredRect, flip the vector so that we don't move in the wrong direction.
            Vector2 difVecBetweenCentres = playerRectsStruct.DesiredRect.center - playerRectsStruct.ActualRect.center;
            if ( Vector2.Dot( lhs: ret, rhs: difVecBetweenCentres ) < 0 )
            {
                ret *= -1;
            }

            return ret;
        }

        private void ProcessVelocityForActualRect(
            ref ActualRectAndDesiredRect playerRectsStruct )
        {
            // calc drag value
            float dragVal = m_dragValue * m_deltaTime;
            // clamp drag so it is never greater than 1
            dragVal = Mathf.Clamp( value: dragVal, min: 0, max: 1 );
            // apply drag
            playerRectsStruct.ActualRectVelocity *= 1.0f - dragVal;
            // apply the velocity
            playerRectsStruct.ActualRect.position += playerRectsStruct.ActualRectVelocity * m_deltaTime;
        }

        private void RemovePlayerFromValidPlayerDict(
            Player_Base player )
        {
            m_playerRectsAndRopeDict.Remove( player );
            if ( m_hoveredPlayer == player )
            {
                m_hoveredPlayer = null;
            }
        }

        private void ReorderPlayerRectsAndDrawThem()
        {
            SceneView sceneView = SceneView.currentDrawingSceneView;

            // get an ordered list of players by viewport z position and whether they are hovered
            List<Player_Base> orderedPlayers = m_playerRectsAndRopeDict.Keys.ToList();
            orderedPlayers = orderedPlayers.OrderByDescending( x => sceneView.camera.WorldToViewportPoint( x.Transform.position ).z ).ToList();
            orderedPlayers = orderedPlayers.OrderByDescending( x => x == m_hoveredPlayer ? -1 : 0 ).ToList();

            // figure out which rect is being hovered by the mouse
            Player_Base hoveredPlayer = null;
            for ( int index = orderedPlayers.Count - 1; index >= 0; index-- )
            {
                Player_Base player               = orderedPlayers[ index ];
                Rect        actualRect           = m_playerRectsAndRopeDict[ player ].Rects.ActualRect;
                Rect        actualRectInGUISpace = GizmoUtils.ConvertFromScreenSpaceToGUISpace( actualRect );
                if ( actualRectInGUISpace.Contains( Event.current.mousePosition ) )
                {
                    hoveredPlayer = player;

                    break;
                }
            }

            Debug.Log( $"Hovered player: {hoveredPlayer}" );
            m_hoveredPlayer = hoveredPlayer;

            // Update dragging
            if ( m_hoveredPlayer != null )
            {
                // check if the alt key is being held
                DraggingHoveredRect = Event.current.alt;
            }

            // Reorder the players based on the hovered player
            orderedPlayers = orderedPlayers.OrderByDescending( x => sceneView.camera.WorldToViewportPoint( x.Transform.position ).z ).ToList();
            orderedPlayers = orderedPlayers.OrderByDescending( x => x == m_hoveredPlayer ? -1 : 0 ).ToList();

            // draw the player rects
            foreach ( Player_Base player in orderedPlayers )
            {
                bool hovered = m_hoveredPlayer == player;
                DrawPlayerRects( player: player, hovered: hovered );
            }

            m_mousePosLastFrame = Event.current.mousePosition;
        }

        private void ResetVelocityOfActualRect(
            ref ActualRectAndDesiredRect playerRectsStruct )
        {
            playerRectsStruct.ActualRectVelocity = Vector2.zero;
        }

        private Matrix4x4 RotateToFaceCameraMatrix(
            Vector3 worldPos )
        {
            Quaternion rot                      = GetRotationToFaceCamera( worldPos );
            Matrix4x4  faceCameraRotationMatrix = Matrix4x4.Rotate( q: rot );
            return faceCameraRotationMatrix;
        }

        private bool SceneViewValidityCheck()
        {
            SceneView sceneView = SceneView.currentDrawingSceneView;
            return sceneView           != null
                   && sceneView.camera != null;
        }

        private void SimulateRopes(
            IEnumerable<RopePhysics.Rope> ropes,
            float                         deltaTime )
        {
            foreach ( RopePhysics.Rope rope in ropes )
            {
                rope.Simulate( deltaTime: m_ropes_DeltaTimeMultiplier * deltaTime,
                               stickIterations: m_ropes_NumberOfStickIterations,
                               simulationIterations: m_ropes_NumberOfSimulationIterationsPerTimeStep,
                               gravity: m_ropes_Gravity,
                               damping: m_ropes_Damping,
                               selectedPoint: m_selectedRopePoint );
            }
        }

        private void SpawnDebugRopes()
        {
            m_debugRopes.Clear();
            m_debugRopes.Add( SpawnRope( start: ConvertVec( m_ropes_Debug_Start1 ), end: ConvertVec( m_ropes_Debug_End1 ) ) );
            m_debugRopes.Add( SpawnRope( start: ConvertVec( m_ropes_Debug_Start2 ), end: ConvertVec( m_ropes_Debug_End2 ) ) );

            return;

            Vector2 ConvertVec(
                Vector2 inVec )
            {
                Vector2 outVec = new Vector2( x: SceneViewScreenWidth * inVec.x, y: SceneViewScreenHeight * inVec.y );
                return outVec;
            }
        }

        private void SpawnDebugRopes_IfScreenIsStable()
        {
            switch ( m_screenWidthStableState )
            {
                case ScreenWidthStableState.None:
                case ScreenWidthStableState.Unstable:
                    if ( IsScreenWidthTheSameAsLastFrame )
                    {
                        m_screenWidthStableState        = ScreenWidthStableState.PreStable;
                        m_screenWidthPreStableTimestamp = Time.time;
                    }

                    break;
                case ScreenWidthStableState.PreStable:
                    if ( Time.time - m_screenWidthPreStableTimestamp > SPAWN_ROPE_DELAY_ON_SCREEN_WIDTH_STABILISED )
                    {
                        m_screenWidthStableState = ScreenWidthStableState.FullyStable;
                        SpawnDebugRopes();
                    }

                    break;
                case ScreenWidthStableState.FullyStable:
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if ( !IsScreenWidthTheSameAsLastFrame )
            {
                m_screenWidthStableState = ScreenWidthStableState.Unstable;
            }

            m_lastScreenWidth = SceneViewScreenWidth;
        }

        private RopePhysics.Rope SpawnRope(
            Vector2 start,
            Vector2 end )
        {
            float dist              = Vector2.Distance( a: start, b: end );
            float distBetweenPoints = dist / ( m_ropes_NumberOfPoints - 1 );

            RopePhysics.Rope rope = new RopePhysics.Rope( numberOfPoints: m_ropes_NumberOfPoints,
                                                          distanceBetweenPoints: distBetweenPoints,
                                                          startPosition: start,
                                                          endPosition: end );

            rope.SetAllStickLengths( m_ropes_DesiredStickLength );

            return rope;
        }

        private void SwapRectsIfTheyWantTo(
            Player_Base player,
            Player_Base otherPlayer )
        {
            // get rects
            ActualRectAndDesiredRect playerRects      = m_playerRectsAndRopeDict[ player ].Rects;
            ActualRectAndDesiredRect otherRects       = m_playerRectsAndRopeDict[ otherPlayer ].Rects;
            Rect                     thisActualRect   = playerRects.ActualRect;
            Rect                     otherActualRect  = otherRects.ActualRect;
            Rect                     thisDesiredRect  = playerRects.DesiredRect;
            Rect                     otherDesiredRect = otherRects.DesiredRect;

            if ( !thisActualRect.Overlaps( otherActualRect ) )
            {
                return;
            }

            // Distances
            float thisXDistToOtherDesired = otherDesiredRect.x - thisActualRect.x;
            float otherXDistToOurDesired  = thisDesiredRect.x  - otherActualRect.x;

            float thisXDistToThisDesired   = thisDesiredRect.x  - thisActualRect.x;
            float otherXDistToOtherDesired = otherDesiredRect.x - otherActualRect.x;

            float thisYDistToOtherDesired = otherDesiredRect.y - thisActualRect.y;
            float otherYDistToOurDesired  = thisDesiredRect.y  - otherActualRect.y;

            float thisYDistToThisDesired   = thisDesiredRect.y  - thisActualRect.y;
            float otherYDistToOtherDesired = otherDesiredRect.y - otherActualRect.y;

            // if the x overlap is more than zero and these rects are closer to each others desired position than their own, swap their positions
            if ( Mathf.Abs( thisXDistToOtherDesired )   < Mathf.Abs( thisXDistToThisDesired )
                 && Mathf.Abs( otherXDistToOurDesired ) < Mathf.Abs( otherXDistToOtherDesired ) )
            {
                // swap positions
                ( playerRects.ActualRect.position, otherRects.ActualRect.position ) =
                    ( otherRects.ActualRect.position, playerRects.ActualRect.position );
                // reset velocities
                playerRects.ActualRectVelocity = Vector2.zero;
                otherRects.ActualRectVelocity  = Vector2.zero;

                // update the dictionary
                m_playerRectsAndRopeDict[ player ].Rects      = playerRects;
                m_playerRectsAndRopeDict[ otherPlayer ].Rects = otherRects;
            }
            else if ( Mathf.Abs( thisYDistToOtherDesired )   < Mathf.Abs( thisYDistToThisDesired )
                      && Mathf.Abs( otherYDistToOurDesired ) < Mathf.Abs( otherYDistToOtherDesired ) )
            {
                // swap positions
                ( playerRects.ActualRect.position, otherRects.ActualRect.position ) =
                    ( otherRects.ActualRect.position, playerRects.ActualRect.position );
                // reset velocities
                playerRects.ActualRectVelocity = Vector2.zero;
                otherRects.ActualRectVelocity  = Vector2.zero;

                // update the dictionary
                m_playerRectsAndRopeDict[ player ].Rects      = playerRects;
                m_playerRectsAndRopeDict[ otherPlayer ].Rects = otherRects;
            }
        }

        private void UpdateDebugRopes(
            IEnumerable<RopePhysics.Rope> ropes,
            float                         deltaTime )
        {
            Vector2 mousePos = Event.current.mousePosition;
            // convert gui to screen space
            Vector2 mouseInScreenCoordinates = HandleUtility.GUIPointToScreenPixelCoordinate( mousePos );

            //UpdateRopePointSelection();
            SimulateRopes( ropes: ropes, deltaTime: deltaTime );

            return;

            void UpdateRopePointSelection()
            {
                // Grab
                if ( Event.current.alt )
                {
                    if ( m_selectedRopePoint == null )
                    {
                        // Find the closest rope point to the mouse
                        float                 closestDist      = float.MaxValue;
                        RopePhysics.RopePoint closestRopePoint = null;
                        foreach ( RopePhysics.Rope rope in m_debugRopes )
                        {
                            foreach ( RopePhysics.RopePoint ropePoint in rope.RopePoints )
                            {
                                float dist = Vector2.Distance( a: ropePoint.Position, b: mouseInScreenCoordinates );
                                if ( dist    < m_ropes_GrabDist
                                     && dist < closestDist )
                                {
                                    closestDist      = dist;
                                    closestRopePoint = ropePoint;
                                }
                            }
                        }

                        // Clear previous selected point
                        if ( m_selectedRopePoint    != null
                             && m_selectedRopePoint != closestRopePoint )
                        {
                            m_selectedRopePoint.Selected = false;
                            m_selectedRopePoint          = null;
                        }

                        // Update new selected point
                        m_selectedRopePoint = closestRopePoint;
                    }
                }
                // Ungrab
                else
                {
                    if ( m_selectedRopePoint != null )
                    {
                        m_selectedRopePoint.Selected = false;
                        m_selectedRopePoint          = null;
                    }
                }

                // Set values for selected rope point
                if ( m_selectedRopePoint != null )
                {
                    m_selectedRopePoint.Selected     = true;
                    m_selectedRopePoint.Position     = mouseInScreenCoordinates;
                    m_selectedRopePoint.PrevPosition = mouseInScreenCoordinates;
                }
            }
        }

        private void UpdatePositionsOfActualRects()
        {
            List<Player_Base> playersWithValidRects = m_playerRectsAndRopeDict.Keys.ToList();
            foreach ( Player_Base player in playersWithValidRects )
            {
                IEnumerable<Player_Base> otherPlayersWithValidRects = playersWithValidRects.Where( x => x != player );
                float                    handleSizeScaleFactor      = GetHandleSizeScaleFactor( player );

                // Get a copy of the struct in the dict
                ActualRectAndDesiredRect playerRectsStruct = m_playerRectsAndRopeDict[ player ].Rects;

                if ( DraggingHoveredRect && m_hoveredPlayer == player )
                {
                    ResetVelocityOfActualRect( ref playerRectsStruct );
                    MatchSizeOfDesiredRect( ref playerRectsStruct );
                    playerRectsStruct.ActualRect.position += ConvertMouseDeltaToScreenSpace( MouseDelta );
                }
                else
                {
                    if ( m_spreadRectsOut )
                    {
                        MatchSizeOfDesiredRect( ref playerRectsStruct );
                        MoveTowardsDesiredPosition( playerRectsStruct: ref playerRectsStruct, handleSizeScaleFactor: handleSizeScaleFactor );
                        InteractWithOtherRects( playerRectsStruct: ref playerRectsStruct, handleSizeScaleFactor: handleSizeScaleFactor,
                                                otherPlayersWithValidRects: otherPlayersWithValidRects );
                        ProcessVelocityForActualRect( ref playerRectsStruct );
                        //ClampToScreen( ref playerRectsStruct );
                    }
                    else
                    {
                        playerRectsStruct.ActualRect = playerRectsStruct.DesiredRect;
                    }
                }

                // Put the struct back into the dict
                m_playerRectsAndRopeDict[ player ].Rects = playerRectsStruct;
            }
        }

        private void UpdateRopesEndPositions()
        {
            foreach ( RectsAndRope rectsAndRope in m_playerRectsAndRopeDict.Values )
            {
                RopePhysics.Rope         rope  = rectsAndRope.Rope;
                ActualRectAndDesiredRect rects = rectsAndRope.Rects;

                rope.RopePoints[ 0 ].Position     = rects.DesiredRect.center;
                rope.RopePoints[ 0 ].PrevPosition = rects.DesiredRect.center;

                rope.RopePoints[ ^1 ].Position     = rects.ActualRect.GetEdgeCenter( Edge.Top ) + Vector2.up * m_ropes_Draw_LineThickness * 0.5f;
                rope.RopePoints[ ^1 ].PrevPosition = rope.RopePoints[ ^1 ].Position;
            }
        }

        private void UpdateValidPlayerRectsCollection()
        {
            foreach ( Player_Base player in m_gameManager.AllValidPlayers )
            {
                GizmoUtils.LabelRectInfo labelRectInfo                = player.GetDebugLabelRectInfo();
                bool                     playerRectDictContainsPlayer = m_playerRectsAndRopeDict.ContainsKey( player );

                // If player rect is able to be calculated and is visible, update or add it to our valid player rects dict
                if ( labelRectInfo is { WasAbleToBeCalculated: true, IsVisible: true } )
                {
                    // Update
                    if ( playerRectDictContainsPlayer )
                    {
                        ActualRectAndDesiredRect rectsStruct = m_playerRectsAndRopeDict[ player ].Rects;
                        rectsStruct.DesiredRect                  = labelRectInfo.LabelRect;
                        m_playerRectsAndRopeDict[ player ].Rects = rectsStruct;
                    }
                    // Add
                    else
                    {
                        AddPlayerToRectsAndRopeDict( player );
                    }
                }
                // Otherwise, remove the rect from the dict if it's in there.
                else
                {
                    if ( !playerRectDictContainsPlayer )
                    {
                        continue;
                    }

                    RemovePlayerFromValidPlayerDict( player );
                }
            }
        }

        private Matrix4x4 WorldToCameraSpaceMatrix(
            Vector3 worldPos )
        {
            Quaternion rot = GetRotationToFaceCamera( worldPos );

            Matrix4x4 positionMatrixInFrontOfCamera = Matrix4x4.TRS( pos: worldPos,     q: Quaternion.identity, s: Vector3.one );
            Matrix4x4 faceCameraRotationMatrix      = Matrix4x4.TRS( pos: Vector3.zero, q: rot,                 s: Vector3.one );
            Matrix4x4 combinedMatrix                = positionMatrixInFrontOfCamera * faceCameraRotationMatrix;
            return combinedMatrix;
        }

        #endregion

        private struct RopePointTangentPair
        {

            #region Public Fields

            public Vector2 Point;
            public Vector2 TangentPointAfter;
            public Vector2 TangentPointBefore;

            #endregion

        }

        private struct RopeStickLengthValues
        {

            #region Public Fields

            public float StickLength;
            public float StickLengthVelocity;

            #endregion

        }

        private struct EdgePair
        {

            #region Public Fields

            public Vector2 Centre;
            public float   Dist;
            public string  Name;

            #endregion

            #region Public Constructors

            public EdgePair(
                Vector2 pos,
                Vector2 otherPos,
                string  name )
            {
                Name   = name;
                Centre = pos;
                Dist   = Vector2.Distance( a: Centre, b: otherPos );
            }

            #endregion

        }

        public struct ActualRectAndDesiredRect
        {

            #region Public Fields

            public Rect    ActualRect;
            public Vector2 ActualRectVelocity;
            public Rect    DesiredRect;

            #endregion

        }

        public class RectsAndRope
        {

            #region Public Properties

            public ActualRectAndDesiredRect Rects { get; set; }

            public RopePhysics.Rope Rope { get; set; }

            #endregion

            #region Public Constructors

            public RectsAndRope()
            {
            }

            public RectsAndRope(
                ActualRectAndDesiredRect rects,
                RopePhysics.Rope         rope )
            {
                Rects = rects;
                Rope  = rope;
            }

            #endregion

        }

        public static class RopePhysics
        {
            public class RopePoint
            {

                #region Public Fields

                public readonly bool    OriginallyLocked;
                public          Vector2 Position;
                public          Vector2 PrevPosition;
                public          bool    Selected;

                #endregion

                #region Public Properties

                public bool CurrentlyLocked => Selected || OriginallyLocked;

                #endregion

                #region Public Constructors

                public RopePoint(
                    bool    originallyLocked,
                    Vector2 pos,
                    Vector2 prevPos )
                {
                    OriginallyLocked = originallyLocked;
                    Position         = pos;
                    PrevPosition     = prevPos;
                }

                #endregion

            }

            public class RopeStick
            {

                #region Public Fields

                public float     Length;
                public RopePoint PointA;
                public RopePoint PointB;

                #endregion

            }

            public class Rope
            {

                #region Public Fields

                public List<RopePoint> RopePoints;
                public List<RopeStick> RopeSticks;

                #endregion

                #region Public Constructors

                public Rope(
                    int     numberOfPoints,
                    float   distanceBetweenPoints,
                    Vector2 startPosition,
                    Vector2 endPosition )
                {
                    RopePoints = new List<RopePoint>();
                    RopeSticks = new List<RopeStick>();

                    for ( int i = 0; i < numberOfPoints; i++ )
                    {
                        Vector2   position = Vector2.Lerp( a: startPosition, b: endPosition, t: (float)i / ( numberOfPoints - 1 ) );
                        RopePoint point    = new RopePoint( originallyLocked: i == 0 || i == numberOfPoints - 1, pos: position, prevPos: position );
                        RopePoints.Add( point );
                    }

                    for ( int i = 0; i < RopePoints.Count - 1; i++ )
                    {
                        RopeStick stick = new RopeStick
                        {
                            PointA = RopePoints[ i ],
                            PointB = RopePoints[ i + 1 ],
                            Length = distanceBetweenPoints,
                        };
                        RopeSticks.Add( stick );
                    }
                }

                #endregion

                #region Public Methods

                public int CalculateDesiredNumberOfPoints(
                    float distanceBetweenPoints )
                {
                    // get distance between first and last points
                    Vector2 firstPointPos = RopePoints[ 0 ].Position;
                    Vector2 lastPointPos  = RopePoints[ ^1 ].Position;

                    float distBetweenFirstAndLast = Vector2.Distance( a: firstPointPos, b: lastPointPos );

                    int numberOfRopePoints = Mathf.CeilToInt( distBetweenFirstAndLast / distanceBetweenPoints );
                    return numberOfRopePoints;
                }

                // public float CalculateDesiredStickLength(
                //     float ratio )
                // {
                //     // get distance between first and last points
                //     Vector2 firstPointPos = RopePoints[ 0 ].Position;
                //     Vector2 lastPointPos  = RopePoints[ ^1 ].Position;
                //
                //     float distBetweenFirstAndLast = Vector2.Distance( a: firstPointPos, b: lastPointPos );
                //
                //     // get number of sticks
                //     int numberOfSticks = RopeSticks.Count;
                //
                //     // calculate the desired stick length
                //     float desiredStickLength = ratio * distBetweenFirstAndLast / numberOfSticks;
                //
                //     return desiredStickLength;
                // }

                public void SetAllStickLengths(
                    float length )
                {
                    foreach ( RopeStick stick in RopeSticks )
                    {
                        stick.Length = length;
                    }
                }

                public void Simulate(
                    float     deltaTime,
                    int       stickIterations,
                    int       simulationIterations,
                    float     gravity,
                    float     damping,
                    RopePoint selectedPoint )
                {
                    // iterate for number of simulation steps
                    for ( int simulationIterationIdx = 0; simulationIterationIdx < simulationIterations; simulationIterationIdx++ )
                    {
                        // Points
                        foreach ( RopePoint point in RopePoints )
                        {
                            if ( point == selectedPoint )
                            {
                                continue;
                            }

                            if ( point.CurrentlyLocked )
                            {
                                continue;
                            }

                            Vector2 positionPreUpdate = point.Position;

                            Vector2 prevDelta = point.Position - point.PrevPosition;
                            point.Position     += prevDelta    * damping;
                            point.Position     += Vector2.down * gravity * deltaTime * deltaTime;
                            point.PrevPosition =  positionPreUpdate;
                        }

                        // Sticks
                        for ( int stickIterationIdx = 0; stickIterationIdx < stickIterations; stickIterationIdx++ )
                        {
                            foreach ( RopeStick stick in RopeSticks )
                            {
                                Vector2 stickCentre = ( stick.PointA.Position + stick.PointB.Position ) / 2;
                                Vector2 stickDir    = ( stick.PointA.Position - stick.PointB.Position ).normalized;
                                if ( !stick.PointA.CurrentlyLocked )
                                {
                                    stick.PointA.Position = stickCentre + stickDir * stick.Length / 2;
                                }

                                if ( !stick.PointB.CurrentlyLocked )
                                {
                                    stick.PointB.Position = stickCentre - stickDir * stick.Length / 2;
                                }

                            }
                        }
                    }
                }

                #endregion

            }
        }
    }
}
#endif