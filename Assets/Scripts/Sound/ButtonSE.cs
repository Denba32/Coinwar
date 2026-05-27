using StockGame.Scripts.Manager;
using UnityEngine.EventSystems;
using UniRx.Triggers;
using UnityEngine.UI;
using UnityEngine;
using UniRx;

namespace StockGame.Scripts.Sounds
{
    public class ButtonSE : MonoBehaviour
    {
        private Button button;
        [SerializeField] private bool isPositive;

        [SerializeField] private bool isPointerDown;

        private void Start()
        {
            button = GetComponent<Button>();
            if (button == null) return;
            if(isPointerDown)
            {
                button.OnPointerDownAsObservable().Subscribe(PlayButtonSE).AddTo(this);
            }
            else
            {
                button.OnClickAsObservable().Subscribe(PlayButtonSE).AddTo(this);
            }
        }

        private void PlayButtonSE(Unit _)
        {
            string fileName = isPositive ? "SFX220" : "SFX221";
            string path = $"SFX/{fileName}";
            Managers.Sound.PlaySound(path, Define.SoundType.SFX, Define.SoundEffectType.UI);
        }

        private void PlayButtonSE(BaseEventData eventData)
        {
            string fileName = isPositive ? "SFX220" : "SFX221";
            string path = $"SFX/{fileName}";
            Managers.Sound.PlaySound(path, Define.SoundType.SFX, Define.SoundEffectType.UI);
        }
    }
}