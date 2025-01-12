using UnityEngine;

namespace DefaultNamespace
{
    public class CheckpointVolume : MonoBehaviour
    {

        #region Public Methods

        public void OnTriggerEnter( Collider other )
        {
            if ( other.CompareTag( "Player" ) )
            {
                Player_MonoBehaviour playerMonoBehaviour = other.GetComponentInParent<Player_MonoBehaviour>();
                playerMonoBehaviour.PlayerBase.OnEnteredCheckpointVolume( gameObject );
            }
        }

        public void OnTriggerExit( Collider other )
        {
            if ( other.CompareTag( "Player" ) )
            {
                Player_MonoBehaviour playerMonoBehaviour = other.GetComponentInParent<Player_MonoBehaviour>();
                playerMonoBehaviour.PlayerBase.OnExitedCheckpointVolume( gameObject );
            }
        }

        #endregion

    }
}