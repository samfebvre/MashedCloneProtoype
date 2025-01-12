using Doozy.Runtime.Reactor;
using UnityEngine;

namespace DefaultNamespace
{
    public class ProgressorPrinter : MonoBehaviour
    {

        #region Serialized

        public bool PrintProgress;

        #endregion

        #region Public Methods

        public void Awake()
        {
            m_progressor = GetComponent<Progressor>();
            m_progressor.OnValueChanged.AddListener( OnProgressChanged );
        }

        #endregion

        #region Private Fields

        private Progressor m_progressor;

        #endregion

        #region Private Methods

        private void OnProgressChanged( float arg0 )
        {
            if ( !PrintProgress )
            {
                return;
            }

            Debug.Log( $"Progress progress: {arg0}" );
        }

        #endregion

    }
}