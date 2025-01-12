#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace DefaultNamespace
{
    public class DataWrangler_Manager : MonoBehaviour
    {
        public List<IDataWrangler_Object> DataWrangler_Objects;

        private VisualElement m_cardElement;
        
        [SerializeField]
        private StyleSheet m_styleSheet;

        private void DrawAllObjectsData()
        {
            foreach ( IDataWrangler_Object dataWrangler_Object in DataWrangler_Objects )
            {
                // Draw a box with the data in it
            }
        }

        private const string CARD_NAME = "Card Element";

        private void Start()
        {
            // Remove pre existing card 
            var preExistingElement = SceneViewRootElem.Q( CARD_NAME );
            if ( preExistingElement != null )
            {
                SceneViewRootElem.Remove( preExistingElement );
            }
            
            // Create new card
            m_cardElement = new VisualElement
            {
                name                  = CARD_NAME,
            };
            
            // Add the style sheet
            m_cardElement.styleSheets.Add( m_styleSheet );
            m_cardElement.AddToClassList( "cardPanel" );
            
            // Add the card to the scene view
            SceneViewRootElem.Add( m_cardElement );
        }

        private void OnDestroy()
        {
            // Remove the card from the scene view
            SceneViewRootElem.Remove( m_cardElement );
        }

        private void OnDrawGizmos()
        {

        }

        private SceneView     MainSceneView     => (SceneView)SceneView.sceneViews[ 0 ] ;
        private VisualElement SceneViewRootElem => MainSceneView.rootVisualElement;

    }
}
#endif