using System;
using System.Collections.Generic;
using Doozy.Runtime.Reactor;
using UnityEngine;

namespace DefaultNamespace
{
    public class StartProgressorsOnProgressionReached : MonoBehaviour
    {

        #region Serialized

        public List<ProgressorData> Progressors;

        #endregion

        #region Public Methods

        public void Awake()
        {
            SourceProgressor = GetComponent<Progressor>();
            SourceProgressor.OnValueChanged.AddListener( OnSourceProgressorReachedValue );
        }

        public void OnSourceProgressorReachedValue( float value )
        {
            // Start all progressors that have a value greater than the last value
            foreach ( ProgressorData progressorData in Progressors )
            {
                if ( value          >= progressorData.Value
                     && m_lastValue < progressorData.Value )
                {
                    progressorData.Progressor.Play();
                }
            }

            m_lastValue = value;
        }

        public void Reset()
        {
            // Reset all progressors
            foreach ( ProgressorData progressorData in Progressors )
            {
                progressorData.Progressor.Stop();
                progressorData.Progressor.ResetToStartValues();
            }
        }

        #endregion

        #region Private Fields

        private float m_lastValue;

        private Progressor SourceProgressor;

        #endregion

        [Serializable]
        public struct ProgressorData
        {

            #region Serialized

            public Progressor Progressor;
            public float      Value;

            #endregion

        }
    }
}