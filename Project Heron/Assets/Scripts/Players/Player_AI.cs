using System.Text;
using UnityEngine;

namespace DefaultNamespace
{
    public class Player_AI : Player_Base
    {

        #region Statics and Constants

        private const float HORIZONTAL_SMOOTH_TIME           = 0.1f;
        private const float VERTICAL_INPUT                   = 1.0f;
        private const float ANGLE_TO_MAXIMISE_STEERING       = 60.0f;
        private const float HORIZONTAL_INPUT_NOISE_MAGNITUDE = 3f;
        private const float HORIZONTAL_INPUT_NOISE_PAN_SPEED = 0.2f;
        private const float RANDOM_SEED_MULTIPLIER           = 100f;

        #endregion

        #region Public Methods

        public override void Init_MonoBehaviour( Player_MonoBehaviour playerMonoBehaviour, Material vehicleMaterial )
        {
            base.Init_MonoBehaviour( playerMonoBehaviour, vehicleMaterial );
            // Randomize the seed based on the game object's instance ID
            Random.InitState( PlayerMonoBehaviour.gameObject.GetInstanceID() );
            m_randomSeed = Random.value * RANDOM_SEED_MULTIPLIER;
            m_stupidity  = Random.value;
        }

        public override void ResetPlayer()
        {
            base.ResetPlayer();
            m_horizontalInput                   = 0;
            m_targetHorizontalInput             = 0;
            m_horizontalInputSmoothdampVelocity = 0;
        }

        #endregion

        #region Protected Methods

        protected override InputStruct GetInputs()
        {
            // Attempt to steer towards the next checkpoint
            GameObject nextCheckpoint = CheckpointManager.GetNextCheckpointForPlayer( this );

            // get the direction to the next checkpoint
            Vector3 dirToNextCheckpoint = ( nextCheckpoint.transform.position - Transform.position ).normalized;

            // get the direction the player is facing
            Vector3 forward = Transform.forward;

            // get the angle between the direction to the next checkpoint and the direction the player is facing
            float angle = Vector3.SignedAngle( forward, dirToNextCheckpoint, Vector3.up );

            // if the angle is greater than 0, turn right
            if ( angle > 0 )
            {
                // interpolate the horizontal input to the right based on the angle magnitude
                m_targetHorizontalInput = Mathf.Lerp( 0.0f, 1.0f, Mathf.Abs( angle ) / ANGLE_TO_MAXIMISE_STEERING );
            }

            // if the angle is less than 0, turn left
            else if ( angle < 0 )
            {
                // interpolate the horizontal input to the left based on the angle magnitude
                m_targetHorizontalInput = Mathf.Lerp( 0.0f, -1.0f, Mathf.Abs( angle ) / ANGLE_TO_MAXIMISE_STEERING );
            }

            // add some perlin noise to the target horizontal input
            m_targetHorizontalInput += ( Mathf.PerlinNoise( m_randomSeed + Time.time * HORIZONTAL_INPUT_NOISE_PAN_SPEED, 0 ) - 0.5f )
                                       * HORIZONTAL_INPUT_NOISE_MAGNITUDE
                                       * m_stupidity;

            // clamp target horizontal input to -1 and 1
            m_targetHorizontalInput = Mathf.Clamp( m_targetHorizontalInput, -1.0f, 1.0f );

            m_horizontalInput = Mathf.SmoothDamp( m_horizontalInput, m_targetHorizontalInput, ref m_horizontalInputSmoothdampVelocity,
                HORIZONTAL_SMOOTH_TIME );

            // draw the target horizontal input
            //Debug.DrawLine( Transform.position, Transform.position + Transform.right * ( m_targetHorizontalInput * 10.0f ), Color.cyan );

            InputStruct ret = new InputStruct
            {
                Horizontal = m_horizontalInput,
                Vertical   = VERTICAL_INPUT,
                // Horizontal = 0,
                // Vertical = 0,
            };
            return ret;
        }

        protected override void PopulateStringBuilderWithDebugText( ref StringBuilder stringBuilder )
        {
            base.PopulateStringBuilderWithDebugText( ref stringBuilder );
            stringBuilder.AppendLine( "Stupidity: " + m_stupidity );
        }

        #endregion

        #region Private Fields

        private float m_horizontalInput;
        private float m_horizontalInputSmoothdampVelocity;

        private float m_randomSeed;
        private float m_stupidity;
        private float m_targetHorizontalInput;

        #endregion

    }
}