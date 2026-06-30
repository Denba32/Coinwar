using TMPro;
using UnityEngine;

public class ClickTeleport : MonoBehaviour
{
    private SpriteRenderer teleportTrigger;
    public string playerTag = "Player";
    private void Awake()
    {
        teleportTrigger = GetComponent<SpriteRenderer>();
    }

    private void OnMouseDown()
    {

    }
}