using UnityEngine;

public class WheelController : MonoBehaviour
{

    #region Serialized

    public Transform WheelModel;

    [HideInInspector] public WheelCollider WheelCollider;

    // Create properties for the CarControl script
    // (You should enable/disable these via the 
    // Editor Inspector window)
    public bool Steerable;
    public bool Motorized;

    #endregion

    #region Public Methods

    // Start is called before the first frame update
    public void Init()
    {
        WheelCollider = GetComponent<WheelCollider>();
    }

    #endregion

    #region Unity Functions

    // Update is called once per frame
    private void Update()
    {
        // Get the Wheel colliders world pose values and
        // use them to set the wheel model's position and rotation
        WheelCollider.GetWorldPose( out m_position, out m_rotation );
        WheelModel.transform.position = m_position;
        WheelModel.transform.rotation = m_rotation;
    }

    #endregion

    #region Private Fields

    private Vector3    m_position;
    private Quaternion m_rotation;

    #endregion

}