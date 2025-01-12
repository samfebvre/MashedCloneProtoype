using Doozy.Runtime.Reactor;
using Doozy.Runtime.Reactor.Animators;
using Shapes;
using UnityEngine;

namespace DefaultNamespace
{
    public class LineMover : MonoBehaviour
    {

        #region Serialized

        public float LineValue;
        public Line  Line;

        public FloatAnimator LineValueAnimator;
        public Progressor    LineValueProgressor;
        public ColorAnimator LineColorAnimator;

        #endregion

        #region Public Methods

        public void Awake()
        {
            //LineValueProgressor.OnProgressChanged.AddListener( OnProgressChanged );
        }

        public void OnProgressChanged( float arg0 )
        {
            Debug.Log( $"Progress progress: {arg0}" );
        }

        #endregion

        #region Private Fields

        private float _progressVal;

        #endregion

    }
}