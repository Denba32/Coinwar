using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneFunc : MonoBehaviour
{
    public void CallNextScene()
    {
        SceneManager.LoadScene("LobbyScene");
    }

    public void CallMainScene()
    {
        SceneManager.LoadScene("MainScene");
    }

    public void CallGameScene()
    {
        SceneManager.LoadScene("GamePlayScene");
    }
}
