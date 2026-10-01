using UnityEngine;
using TMPro;
using StockGame.Scripts.Manager;

namespace StockGame.Scripts.UI
{
    public class LobbyRoomInformationPanel : MonoBehaviour
    {
        [Space(10)]
        [Header("Room")]
        [SerializeField] private RectTransform roomRect;
        [SerializeField] private TMP_InputField roomIdInputField;

        public TMP_InputField RootIdInputField => roomIdInputField;

        public void Initialize()
        {
            var joinCode = MultiplayManager.Instance.JoinCode;
            SetRootId(joinCode);
        }

        private void SetRootId(string joinId)
        {
            roomIdInputField.text = joinId.ToUpper();
            roomIdInputField.readOnly = true;
        }
    }

}