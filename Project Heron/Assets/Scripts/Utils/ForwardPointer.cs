using DefaultNamespace.Utils;
using UnityEditor;
using UnityEngine;

public class ForwardPointer : MonoBehaviour
{

    #region Statics and Constants

    private const float ARROW_TIP_SIZE = 3f;

    #endregion

    #region Serialized

    public bool Draw;

    #endregion

    #region Public Methods

    #if UNITY_EDITOR
    public void OnDrawGizmos()
    {
        if ( !Draw )
        {
            return;
        }

        // Draw an arrow pointing forwards
        Color prevGizmosColor = Gizmos.color;

        Gizmos.color = Color.red;
        Transform tform    = transform;
        Vector3   position = tform.position;
        Vector3   forward  = tform.forward;
        Gizmos.DrawRay( position, forward );

        // Draw the arrow tip
        Vector3 arrowTip = position + forward;
        Gizmos.DrawMesh( GizmoMeshesSettings.Instance.ConeMesh, arrowTip, tform.rotation, Vector3.one * ARROW_TIP_SIZE );

        // Add a label
        Handles.Label( position, $"{gameObject.name} Forward", CustomGUIStyles.CenteredLabel() );

        Gizmos.color = prevGizmosColor;
    }

    #endif // UNITY_EDITOR

    #endregion

}