using UnityEngine;

namespace DefaultNamespace
{
    public class KillVolume : MonoBehaviour
    {

        #region Public Methods

        public void OnTriggerEnter( Collider other )
        {
            if ( other.CompareTag( "Player" ) )
            {
                Player_MonoBehaviour playerMonoBehaviour = other.GetComponentInParent<Player_MonoBehaviour>();
                playerMonoBehaviour.PlayerBase.OnEnteredKillVolume();
            }
        }

        #endregion

    }
}