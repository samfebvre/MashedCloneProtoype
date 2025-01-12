using Cinemachine;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace DefaultNamespace
{
    public class LevelReferenceManager : MonoBehaviour
    {

        #region Serialized

        [SerializeField] private TextMeshProUGUI m_statusText;

        [FormerlySerializedAs( "_FollowCamera" )] [SerializeField]
        private CinemachineVirtualCamera m_followCamera;

        #endregion

        #region Public Properties

        public CinemachineVirtualCamera FollowCamera => m_followCamera;

        public TextMeshProUGUI StatusText => m_statusText;

        #endregion

    }
}