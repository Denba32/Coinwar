using StockGame.Scripts.Define;
using UnityEngine;

namespace StockGame.Scripts.Datas
{
    [CreateAssetMenu(fileName = "SupplyDataSO", menuName = "ScriptableObject/SupplyDataSO")]
    public class SupplyDataSO : ScriptableObject
    {
        public GameDefine.SupplyBoxEvent SupplyEvent;
        public Sprite SupplyCardImage;
    }
}
