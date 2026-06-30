using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleportTriggerChild : MonoBehaviour
{
    public Sprite enterSprite;
    public Sprite exitSprite;

    private SpriteRenderer targetRenderer;

    [Header("�̵��� ��ǥ ��ġ")]
    public Transform targetPosition;

    [Header("�÷��̾� �±� �̸� (�⺻��: Player)")]
    public string playerTag = "Player";

    private Transform player;
    public bool canClick { get; private set; }

    private void Awake()
    {
        targetRenderer = GetComponentInParent<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            targetRenderer.sprite = enterSprite;
            player = other.transform;
            canClick = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            targetRenderer.sprite = exitSprite;
            canClick = false;
            player = null;
        }
    }

}
