using UnityEditor;
using UnityEngine;

namespace PluginMaster
{
    public static class SceneDragAndDrop
    {

        #region Statics and Constants

        private const           string DRAG_ID        = "SceneDragAndDrop";
        private static readonly int    _sceneDragHint = "SceneDragAndDrop".GetHashCode();

        #endregion

        #region Public Methods

        public static void StartDrag( ISceneDragReceiver receiver, string title )
        {
            StopDrag();
            if ( receiver == null )
            {
                return;
            }

            GUIUtility.hotControl = 0;
            DragAndDrop.PrepareStartDrag();
            DragAndDrop.objectReferences = new Object[ 0 ];
            DragAndDrop.paths            = new string[ 0 ];
            DragAndDrop.SetGenericData( DRAG_ID, receiver );
            receiver.StartDrag();
            DragAndDrop.StartDrag( title );
            #if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui += OnSceneGUI;
            #else
            UnityEditor.SceneView.onSceneGUIDelegate += OnSceneGUI;
            #endif
        }

        public static void StopDrag()
        {
            #if UNITY_2019_1_OR_NEWER
            SceneView.duringSceneGui -= OnSceneGUI;
            #else
            UnityEditor.SceneView.onSceneGUIDelegate -= OnSceneGUI;
            #endif
        }

        #endregion

        #region Private Methods

        private static void OnSceneGUI( SceneView sceneView )
        {
            int                controlId = GUIUtility.GetControlID( _sceneDragHint, FocusType.Passive );
            Event              evt       = Event.current;
            EventType          eventType = evt.GetTypeForControl( controlId );
            ISceneDragReceiver receiver;
            if ( eventType    == EventType.DragPerform
                 || eventType == EventType.DragUpdated )
            {
                receiver = DragAndDrop.GetGenericData( DRAG_ID ) as ISceneDragReceiver;
                if ( receiver == null )
                {
                    return;
                }

                DragAndDrop.visualMode = receiver.UpdateDrag( evt, eventType );
                if ( eventType                 == EventType.DragPerform
                     && DragAndDrop.visualMode != DragAndDropVisualMode.None )
                {
                    receiver.PerformDrag( evt );
                    DragAndDrop.AcceptDrag();
                    DragAndDrop.SetGenericData( DRAG_ID, default(ISceneDragReceiver) );
                    StopDrag();
                }

                evt.Use();
            }
            else if ( eventType == EventType.DragExited )
            {
                receiver = DragAndDrop.GetGenericData( DRAG_ID ) as ISceneDragReceiver;
                if ( receiver == null )
                {
                    return;
                }

                receiver.StopDrag();
                evt.Use();
            }
        }

        #endregion

    }

    public interface ISceneDragReceiver
    {

        #region Public Methods

        void                  PerformDrag( Event evt );
        void                  StartDrag();
        void                  StopDrag();
        DragAndDropVisualMode UpdateDrag( Event evt, EventType eventType );

        #endregion

    }
}