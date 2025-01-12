using DefaultNamespace;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomEditor( typeof(CheckpointManager) )]
    public class CheckpointManager_Editor : UnityEditor.Editor
    {

        #region Public Methods

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            CheckpointManager checkpointManager = (CheckpointManager)target;

            if ( GUILayout.Button( "Generate Spline" ) )
            {
                checkpointManager.CreateSplineFromCheckpoints();
            }
        }

        #endregion

    }
}