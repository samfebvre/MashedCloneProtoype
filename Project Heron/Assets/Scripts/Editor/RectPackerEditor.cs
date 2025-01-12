using System;
using System.Collections;
using DefaultNamespace;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Editor
{
    [CustomEditor( typeof(RectPacker) )]
    public class RectPackerEditor : UnityEditor.Editor
    {

        #region Public Methods

        public override VisualElement CreateInspectorGUI()
        {
            m_coroutine ??= EditorCoroutineUtility.StartCoroutineOwnerless( UpdateGizmoGimmick() );
            return base.CreateInspectorGUI();
        }

        #endregion

        #region Private Fields

        private EditorCoroutine m_coroutine;

        private DateTime m_lastFrameTime;

        #endregion

        #region Private Methods

        private IEnumerator UpdateGizmoGimmick()
        {
            while ( true )
            {
                yield return null;
                //double deltaTime = ( DateTime.Now - m_lastFrameTime ).TotalSeconds;
                //Debug.Log( $"Time since last frame: {deltaTime}" );

                //m_lastFrameTime = DateTime.Now;
                SceneView.RepaintAll();
                if ( !Application.isPlaying )
                {
                    break;
                }
            }
        }

        #endregion

    }
}