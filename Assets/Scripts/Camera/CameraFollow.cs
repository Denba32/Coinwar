using StockGame.Scripts.Players;
using UnityEngine;

namespace StockGame.Scripts.Cameras
{
    public class CameraFollow : MonoBehaviour
    {
        public Transform player; // 플레이어의 Transform
        public Vector3 offset; // 카메라와 플레이어 간의 거리
        public float smoothSpeed = 0.125f; // 부드러운 이동 속도

        private void Awake()
        {
            Camera.main.transparencySortMode = TransparencySortMode.CustomAxis;
            Camera.main.transparencySortAxis = new Vector3(0, 1, 0);
            offset = new Vector3(0, 0, -10);

            // 씬에 이미 카메라가 존재하는지 확인
            if (FindObjectsOfType<CameraFollow>().Length > 1)
            {
                Destroy(gameObject); // 중복된 카메라 삭제
                return;
            }

            DontDestroyOnLoad(gameObject);
            PlayerNetwork playerNetwork = FindObjectOfType<PlayerNetwork>();

            if (playerNetwork != null)
            {
                player = playerNetwork.transform; // 현재 씬의 PlayerNetwork를 찾음
            }
            else
            {
                Debug.LogWarning("PlayerNetwork not found in the current scene.");
            }
        }

        private void LateUpdate()
        {
            if (player == null)
            {
                player = FindObjectOfType<PlayerNetwork>()?.transform; // null 조건부 호출
            }

            if (player != null)
            {
                // 플레이어의 현재 위치에 오프셋을 더하여 카메라 위치 계산
                Vector3 desiredPosition = player.position + offset;
                // 카메라의 위치를 부드럽게 이동
                Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
                transform.position = smoothedPosition;

                // 필요한 경우 카메라가 플레이어를 바라보게 설정
                transform.LookAt(player);
            }
        }
    }
}