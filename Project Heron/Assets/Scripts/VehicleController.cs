using DefaultNamespace;
using UnityEngine;

public class VehicleController : MonoBehaviour
{

    #region Serialized

    public float MotorTorque             = 2000;
    public float BrakeTorque             = 2000;
    public float MaxSpeed                = 20;
    public float SteeringRange           = 30;
    public float SteeringRangeAtMaxSpeed = 10;
    public float CentreOfGravityOffset   = -1f;

    #endregion

    #region Public Properties

    public BoxCollider BodyCollider
    {
        get
        {
            if ( m_bodyCollider == null )
            {
                m_bodyCollider = BodyTransform.GetComponent<BoxCollider>();
            }

            return m_bodyCollider;
        }
    }

    public Transform BodyTransform
    {
        get
        {
            if ( m_bodyTransform == null )
            {
                m_bodyTransform = transform.Find( "Body" );
            }

            return m_bodyTransform;
        }
    }

    #endregion

    #region Public Methods

    public Vector3 GetVehicleColliderCentre() =>
        // return the center of the box collider
        BodyCollider.center;

    public Vector3 GetVehicleColliderSize() => BodyCollider.size;

    // Start is called before the first frame update
    public void Init( Player_MonoBehaviour playerMonoBehaviour )
    {
        PlayerMonoBehaviour = playerMonoBehaviour;
        RaceManager         = GameManager.Instance.RaceManager;

        m_rigidBody = GetComponent<Rigidbody>();

        // Adjust center of mass vertically, to help prevent the car from rolling
        m_rigidBody.centerOfMass += Vector3.up * CentreOfGravityOffset;

        // Find all child GameObjects that have the WheelControl script attached
        m_wheels = GetComponentsInChildren<WheelController>();
        foreach ( WheelController wheelController in m_wheels )
        {
            wheelController.Init();
        }
    }

    public void OnFixedUpdate()
    {
        // I now need to stop doing any updates if not currently racing.
        if ( PlayerMonoBehaviour.PlayerBase.CanUpdateWheelForces )
        {
            UpdateWheelForces();
        }
    }

    public void ResetWheelForces()
    {
        foreach ( WheelController wheel in m_wheels )
        {
            // Apply steering to Wheel colliders that have "Steerable" enabled
            if ( wheel.Steerable )
            {
                wheel.WheelCollider.steerAngle = 0;
            }

            wheel.WheelCollider.motorTorque = 0;
        }
    }

    public void SetInputs( InputStruct inputs )
    {
        m_verticalInput   = inputs.Vertical;
        m_horizontalInput = inputs.Horizontal;
    }

    #endregion

    #region Private Fields

    private BoxCollider m_bodyCollider;

    private Transform m_bodyTransform;

    private float m_horizontalInput;

    private Rigidbody m_rigidBody;

    private float m_verticalInput;

    private WheelController[] m_wheels;

    #endregion

    #region Private Properties

    private Player_MonoBehaviour PlayerMonoBehaviour { get; set; }

    private RaceManager RaceManager { get; set; }

    #endregion

    #region Private Methods

    private void UpdateWheelForces()
    {
        // Calculate current speed in relation to the forward direction of the car
        // (this returns a negative number when traveling backwards)
        float forwardSpeed = Vector3.Dot( transform.forward, m_rigidBody.velocity );

        // Calculate how close the car is to top speed
        // as a number from zero to one
        float speedFactor = Mathf.InverseLerp( 0, MaxSpeed, forwardSpeed );

        // Use that to calculate how much torque is available 
        // (zero torque at top speed)
        float currentMotorTorque = Mathf.Lerp( MotorTorque, 0, speedFactor );

        // …and to calculate how much to steer 
        // (the car steers more gently at top speed)
        float currentSteerRange = Mathf.Lerp( SteeringRange, SteeringRangeAtMaxSpeed, speedFactor );

        // Check whether the user input is in the same direction 
        // as the car's velocity
        bool isAccelerating = Mathf.Sign( m_verticalInput ).Equals( Mathf.Sign( forwardSpeed ) );

        foreach ( WheelController wheel in m_wheels )
        {
            // Apply steering to Wheel colliders that have "Steerable" enabled
            if ( wheel.Steerable )
            {
                wheel.WheelCollider.steerAngle = m_horizontalInput * currentSteerRange;
            }

            if ( isAccelerating )
            {
                // Apply torque to Wheel colliders that have "Motorized" enabled
                if ( wheel.Motorized )
                {
                    wheel.WheelCollider.motorTorque = m_verticalInput * currentMotorTorque;
                }

                wheel.WheelCollider.brakeTorque = 0;
            }
            else
            {
                // If the user is trying to go in the opposite direction
                // apply brakes to all wheels
                wheel.WheelCollider.brakeTorque = Mathf.Abs( m_verticalInput ) * BrakeTorque;
                wheel.WheelCollider.motorTorque = 0;
            }
        }
    }

    #endregion

}