using Doozy.Runtime.Reactor;
using Doozy.Runtime.Reactor.Animators;
using UnityEngine;

namespace DefaultNamespace
{
    public class AnimatorUpdaterFromProgressor : MonoBehaviour
    {

        #region Serialized

        public float MinValue;
        public float MaxValue;

        #endregion

        #region Public Methods

        public void Awake()
        {
            m_animator   = GetComponent<FloatAnimator>();
            m_progressor = GetComponentInParent<Progressor>();
            m_progressor.OnProgressChanged.AddListener( OnProgressChanged );
        }

        #endregion

        #region Private Fields

        private FloatAnimator m_animator;

        private Progressor m_progressor;

        #endregion

        #region Private Methods

        private void OnProgressChanged( float arg0 )
        {
            m_animator.SetProgressAt( Mathf.InverseLerp( MinValue, MaxValue, arg0 ) );
        }

        #endregion

    }
}