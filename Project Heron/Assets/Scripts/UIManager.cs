using TMPro;

namespace DefaultNamespace
{
    public class UIManager
    {

        #region Public Methods

        public void SetStatusText( string status )
        {
            StatusText.text = status;
        }

        #endregion

        #region Private Properties

        private TextMeshProUGUI StatusText => GameManager.Instance.LevelReferenceManager.StatusText;

        #endregion

    }
}