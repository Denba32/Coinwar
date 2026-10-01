using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace StockGame.Scripts.Base
{
    public class BaseButton : Button
    {
        public override void OnPointerExit(PointerEventData eventData)
        {
            base.OnPointerExit(eventData);
            DoStateTransition(SelectionState.Normal, true);
        }
        
        public override void OnPointerUp(PointerEventData eventData)
        {
            base.OnPointerUp(eventData);
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}