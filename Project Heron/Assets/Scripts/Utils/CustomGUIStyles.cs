using UnityEngine;

namespace DefaultNamespace.Utils
{
    public static class CustomGUIStyles
    {

        #region Public Methods

        public static GUIStyle CenteredLabel()
        {
            GUIStyle style = new GUIStyle( GUI.skin.label )
            {
                alignment = TextAnchor.MiddleCenter,
            };
            return style;
        }

        #endregion

        public static class SceneViewStyles
        {

            #region Statics and Constants

            private static GUIStyle sm_sceneView_LabelsGUIStyle;

            #endregion

            #region Public Properties

            public static GUIStyle SceneView_LabelsGUIStyle
            {
                get
                {
                    if ( sm_sceneView_LabelsGUIStyle != null )
                    {
                        return sm_sceneView_LabelsGUIStyle;
                    }

                    sm_sceneView_LabelsGUIStyle = new GUIStyle( GUI.skin.label )
                    {
                        fontSize  = 12,
                        fontStyle = FontStyle.Bold,
                        richText  = true,
                        padding   = new RectOffset( 14, 4, 1, 1 ),
                        normal =
                        {
                            textColor = Color.white,
                        },
                    };

                    return sm_sceneView_LabelsGUIStyle;
                }
            }

            #endregion

        }

        public static class InspectorViewStyles
        {

            #region Statics and Constants

            private static GUIStyle sm_inspector_LargeHeadingStyle;

            #endregion

            #region Public Properties

            public static GUIStyle Inspector_LargeHeadingStyle
            {
                get
                {
                    return sm_inspector_LargeHeadingStyle ??= new GUIStyle( GUI.skin.label )
                    {
                        fontSize  = 14,
                        fontStyle = FontStyle.Bold,
                        alignment = TextAnchor.MiddleCenter,
                    };
                }
            }

            #endregion

        }
    }
}