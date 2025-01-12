using UnityEngine;
using UnityEngine.UIElements;

namespace Editor
{
    public class LinearGradientElement : VisualElement
    {
         #region Public Constructors
        
         private readonly Gradient m_gradient;
         public LinearGradientElement(Gradient gradient)
         {
             m_gradient = gradient;
             generateVisualContent += OnGenerateVisualContent;
         }

         const int WIDTH = 256;
         const int HEIGHT = 256;
        
         #endregion
        
         #region Private Methods
        
         private void OnGenerateVisualContent(
             MeshGenerationContext mgc )
         {
             var generatedTex = GradientTextureGenerator.GradientToTexture(
                 WIDTH, HEIGHT, m_gradient);
             
             var newBackground = new StyleBackground
             {
                 value = new Background
                 {
                     texture       = generatedTex,
                     sprite        = null,
                     renderTexture = null,
                     vectorImage   = null
                 },
                 keyword = StyleKeyword.Undefined
             };

             mgc.visualElement.style.backgroundImage = newBackground;
         }

        #endregion

    }
}