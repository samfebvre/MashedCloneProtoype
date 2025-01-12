using UnityEditor;
using UnityEngine;

namespace DefaultNamespace.Utils
{
    [CreateAssetMenu( fileName = "GizmoMeshes", menuName = "Heron/Editor/Gizmo Meshes", order = 0 )]
    public class GizmoMeshesSettings : ScriptableObject
    {

        #region Statics and Constants

        private const string PATH = "Assets/Editor/Settings/";

        private static GizmoMeshesSettings sm_instance;

        #endregion

        #region Serialized

        public Mesh ConeMesh;

        #endregion

        #region Public Properties

        public static GizmoMeshesSettings Instance
        {
            get
            {
                if ( sm_instance != null )
                {
                    return sm_instance;
                }

                #if UNITY_EDITOR
                // search the PATH directory for any asset of type GizmoMeshesSettings
                string[] guids = AssetDatabase.FindAssets( "t:GizmoMeshesSettings", new[] { PATH } );
                if ( guids.Length > 0 )
                {
                    string assetPath = AssetDatabase.GUIDToAssetPath( guids[ 0 ] );
                    sm_instance = AssetDatabase.LoadAssetAtPath<GizmoMeshesSettings>( assetPath );
                }
                #endif // UNITY_EDITOR

                return sm_instance;
            }
        }

        #endregion

    }
}