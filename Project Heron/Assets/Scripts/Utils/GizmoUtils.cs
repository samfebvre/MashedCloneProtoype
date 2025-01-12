#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace DefaultNamespace.Utils
{
    public static class GizmoUtils
    {

        #region Public Methods
        
        public static Rect ConvertFromScreenSpaceToGUISpace( Rect screenSpaceRect )
        {
            Vector3 viewportPos = SceneView.currentDrawingSceneView.camera.ScreenToViewportPoint( screenSpaceRect.center );
            viewportPos.z = GetZValJustInFrontOfNearClipPlane();
            Vector3 worldPos = SceneView.currentDrawingSceneView.camera.ViewportToWorldPoint( viewportPos );
            Vector2 guiPos   = HandleUtility.WorldToGUIPoint( worldPos );
            Rect guiRect = new Rect( guiPos.x - screenSpaceRect.width * 0.5f, guiPos.y - screenSpaceRect.height * 0.5f, screenSpaceRect.width,
                screenSpaceRect.height );
            return guiRect;
        }
        
        public static Vector2 ConvertFromGUISpaceToScreenSpace( Vector2 guiSpacePos )
        {
            Vector3 worldPos = HandleUtility.GUIPointToWorldRay( guiSpacePos ).origin;
            Vector3 viewportPos = SceneView.currentDrawingSceneView.camera.WorldToViewportPoint( worldPos );
            Vector2 screenPos = new Vector2( viewportPos.x * Screen.width, viewportPos.y * Screen.height );
            return screenPos;
        }

        public static float GetZValJustInFrontOfNearClipPlane()
        {
            SceneView sceneView = SceneView.currentDrawingSceneView;
            float     nearClipZ = sceneView.camera.nearClipPlane;
            return nearClipZ * 1.1f;
        }
        
        public static void DrawALabelBoxWithInfo( Vector3 position, string labelText, int fontSize = 12, FontStyle fontStyle = FontStyle.Normal,
                                                  Color   boxColor = default )
        {
            LabelRectInfo labelRectInfo = GetLabelRectForLabel( position, labelText, fontSize, fontStyle );
            if ( !labelRectInfo.WasAbleToBeCalculated )
            {
                return;
            }
            
            if (!labelRectInfo.IsWithinScreenBounds)
            {
                return;
            }
            
            if ( labelRectInfo.IsBehindCamera )
            {
                return;
            }


            labelRectInfo.LabelRect = ConvertFromScreenSpaceToGUISpace( labelRectInfo.LabelRect );
            
            // Now draw it in a nice box.
            Handles.BeginGUI();
            GUI.color = boxColor.WithAlpha( 1.0f );
            GUI.DrawTexture( labelRectInfo.LabelRect, Texture2D.whiteTexture );

            // Choose either white or black text color depending on the background color
            Color textColor = ColorUtils.GetWhiteOrBlackContrastColor( boxColor );

            GUI.color = textColor;
            GUI.Label( labelRectInfo.LabelRect, labelRectInfo.Text, labelRectInfo.Style );
            Handles.EndGUI();
        }

        public static void DrawAPointWithALabel( Vector3 position, string label, Color col = default )
        {
            #if UNITY_EDITOR
            // Draw an arrow pointing forwards
            Color prevGizmosColor = Gizmos.color;

            Gizmos.color = col;
            Vector3 dir = Vector3.up;
            Gizmos.DrawRay( position, dir );

            // Add a label
            Handles.Label( position, $"{label}", CustomGUIStyles.CenteredLabel() );

            Gizmos.color = prevGizmosColor;
            #endif // UNITY_EDITOR
        }

        public static void DrawPointsWithLabels( List<Vector3> positions, List<string> labels, Color col = default )
        {
            for ( int i = 0; i < positions.Count(); i++ )
            {
                DrawAPointWithALabel( positions[ i ], labels[ i ], col );
            }
        }

        public static void DrawPointsWithLabels( List<Vector3> positions, string label, Color col = default )
        {
            for ( int i = 0; i < positions.Count; i++ )
            {
                DrawAPointWithALabel( positions[ i ], $"{label} {i}", col );
            }
        }

        public static int GetFontSizeScaledForWorldPosition( int fontSize, Vector3 worldPosition ) =>
            (int)( fontSize / HandleUtility.GetHandleSize( worldPosition ) );

        public static float GetHandleSizeForPosition( Vector3 worldPosition ) => HandleUtility.GetHandleSize( worldPosition );

        public static LabelRectInfo GetLabelRectForLabel( Vector3   position, string labelText, int fontSize = 12,
                                                          FontStyle fontStyle = FontStyle.Normal )
        {
            LabelRectInfo labelRectInfo = new LabelRectInfo();

            // Scene view validity check - without this we cannot calculate things properly
            SceneView sceneView = SceneView.currentDrawingSceneView;
            if ( sceneView           == null
                 || sceneView.camera == null )
            {
                labelRectInfo.WasAbleToBeCalculated = false;
                return labelRectInfo;
            }
            
            labelRectInfo.WasAbleToBeCalculated = true;

            // Viewport position check
            Vector3 viewportPosition = sceneView.camera.WorldToViewportPoint( position );

            //Vector2    pos2D    = HandleUtility.WorldToGUIPoint( position );
            Vector3        pos2D    =            sceneView.camera.WorldToScreenPoint( position );
            GUIContent text     = new GUIContent( labelText );
            GUIStyle   guiStyle = CustomGUIStyles.SceneViewStyles.SceneView_LabelsGUIStyle;
            guiStyle.fontSize  = GetFontSizeScaledForWorldPosition( fontSize, position );
            guiStyle.fontStyle = fontStyle;
            Vector2 size      = guiStyle.CalcSize( text );
            Rect    labelRect = new Rect( pos2D.x - size.x * 0.5f, pos2D.y - size.y * 0.5f, size.x, size.y );
            
            // On screen check - check if the viewport position is within the screen bounds
            bool isWithinScreenBounds = viewportPosition.x is >= 0.0f and <= 1.0f
                                        && viewportPosition.y is >= 0.0f and <= 1.0f;

            labelRectInfo.IsWithinScreenBounds = isWithinScreenBounds;
            labelRectInfo.LabelRect            = labelRect;
            labelRectInfo.Text                 = text;
            labelRectInfo.Style                = guiStyle;
            labelRectInfo.IsBehindCamera       = viewportPosition.z <= 0.0f;

            return labelRectInfo;
        }

        #endregion

        public struct LabelRectInfo
        {

            #region Public Fields

            [FormerlySerializedAs( "IsValid" )] public bool       WasAbleToBeCalculated;
            [FormerlySerializedAs( "Rect" )]    public Rect       LabelRect;
            public                                     GUIStyle   Style;
            public                                     GUIContent Text;
            public                                     bool       IsBehindCamera;
            public                                     bool       IsWithinScreenBounds;
            public bool IsVisible => WasAbleToBeCalculated && IsWithinScreenBounds && !IsBehindCamera;

            #endregion

        }
    }
}
#endif