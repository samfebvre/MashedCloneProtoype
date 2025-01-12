using System;
using UnityEngine;

namespace DefaultNamespace.Settings
{
    [CreateAssetMenu( fileName = "VehicleSettings", menuName = "Heron/VehicleSettings", order = 0 )]
    public class VehicleSettings : ScriptableObject
    {

        #region Serialized

        // friction values
        public WheelFrictionCurve ForwardFriction;
        public WheelFrictionCurve SidewaysFriction;
        public Material           BodyMaterial;

        #endregion

    }

    [Serializable]
    public struct WheelFrictionCurve
    {

        #region Serialized

        public float ExtremumSlip;
        public float ExtremumValue;
        public float AsymptoteSlip;
        public float AsymptoteValue;
        public float Stiffness;

        #endregion

        #region Public Methods

        public UnityEngine.WheelFrictionCurve ToUnityWheelFrictionCurve() =>
            new UnityEngine.WheelFrictionCurve
            {
                extremumSlip   = ExtremumSlip,
                extremumValue  = ExtremumValue,
                asymptoteSlip  = AsymptoteSlip,
                asymptoteValue = AsymptoteValue,
                stiffness      = Stiffness,
            };

        #endregion

    }
}