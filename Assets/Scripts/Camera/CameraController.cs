using Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CameraController : MonoBehaviour
{
    public CinemachineVirtualCamera cinemachineCam;
    private CinemachineTransposer transposer;
    private CinemachineConfiner2D confiner;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        if (transposer == null)
            transposer = cinemachineCam.GetCinemachineComponent<CinemachineTransposer>();
        if (confiner == null)
            confiner = cinemachineCam.GetComponent<CinemachineConfiner2D>();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public void SetFollow(Transform target)
    {
        cinemachineCam.Follow = target;
    }

    public void SetFollow(Transform target, Vector3 offset)
    {
        cinemachineCam.Follow = target;
        if (transposer == null)
            transposer = cinemachineCam.GetCinemachineComponent<CinemachineTransposer>();
        transposer.m_FollowOffset = offset;
    }

    public void SetFollowDamping(float x, float y, float z = 0)
    {
        transposer.m_XDamping = x;
        transposer.m_YDamping = y;
        transposer.m_ZDamping = z;
    }

    public void SetLookAt(Transform target)
    {
        cinemachineCam.LookAt = target;
    }
    private void OnSceneLoaded(Scene loadedScene, LoadSceneMode sceneMode)
    {
        if (loadedScene.isLoaded)
        {
            if (confiner == null)
                confiner = cinemachineCam.GetComponent<CinemachineConfiner2D>();

            var collider = FindFirstObjectByType<PolygonCollider2D>();
            if (collider != null)
            {
                confiner.m_BoundingShape2D = collider;
            }
        }

    }
}
