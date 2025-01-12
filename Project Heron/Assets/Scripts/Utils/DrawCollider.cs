using UnityEngine;

namespace DefaultNamespace.Utils
{
    public class DrawCollider : MonoBehaviour
    {

        #region Serialized

        [SerializeField] private Color m_drawColor;
        [SerializeField] private bool  m_draw;

        #endregion

        #region Public Properties

        public bool Draw
        {
            get => m_draw;
            set => m_draw = value;
        }

        public Color DrawColor
        {
            get => m_drawColor;
            set => m_drawColor = value;
        }

        #endregion

        #region Unity Functions

        #if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if ( !Draw )
            {
                return;
            }

            m_collider ??= GetComponent<Collider>();

            if ( m_collider == null )
            {
                return;
            }

            Color     prevGizmosColor = Gizmos.color;
            Matrix4x4 prevMatrix      = Gizmos.matrix;

            Gizmos.color  = DrawColor;
            Gizmos.matrix = gameObject.transform.localToWorldMatrix;

            switch ( m_collider )
            {
                case BoxCollider boxCollider:
                    Gizmos.DrawCube( boxCollider.center, boxCollider.size );
                    return;
                case CapsuleCollider capsuleCollider:
                {
                    Mesh nativeCapsuleMesh = Resources.GetBuiltinResource<Mesh>( "Capsule.fbx" );

                    Gizmos.matrix = Matrix4x4.TRS( capsuleCollider.transform.position, capsuleCollider.transform.rotation,
                        capsuleCollider.transform.lossyScale );
                    Gizmos.color = DrawColor;
                    Gizmos.DrawMesh( nativeCapsuleMesh, capsuleCollider.center, Quaternion.identity,
                        new Vector3( capsuleCollider.radius, capsuleCollider.height * 0.25f, capsuleCollider.radius ) );

                    return;
                }
                case MeshCollider meshCollider:
                    Gizmos.color = DrawColor;

                    Gizmos.DrawMesh( meshCollider.sharedMesh, meshCollider.transform.position, meshCollider.transform.rotation,
                        meshCollider.transform.lossyScale );
                    break;
            }

            Gizmos.color  = prevGizmosColor;
            Gizmos.matrix = prevMatrix;
        }
        #endif // UNITY_EDITOR

        #endregion

        #region Private Fields

        private Collider m_collider;

        #endregion

    }
}