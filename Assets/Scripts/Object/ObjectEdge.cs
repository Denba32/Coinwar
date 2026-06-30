using UnityEngine;

public class ObjectEdge : MonoBehaviour
{
    [Header("���׸����� �ٲ� ���")] public SpriteRenderer targetRenderer;

    [Header("������ ���׸��� (�׵θ� ȿ����)")]
    public Material outlineMaterial;

    private Material originalMaterial;

    private void Start()
    {
        // ���� ���׸��� ����
        if (targetRenderer != null)
            originalMaterial = targetRenderer.material;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && targetRenderer != null)
        {
            targetRenderer.material = outlineMaterial; // �׵θ� ���׸��� ����
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player") && targetRenderer != null)
        {
            targetRenderer.material = originalMaterial; // ���� ���׸��� ����
        }
    }
}