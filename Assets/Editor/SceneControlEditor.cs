using UnityEditor;
using UnityEditor.SceneManagement;

public class SceneControlEditor : Editor
{
    [MenuItem("Tools/SceneControl/GoToBootScene &#1")]
    private static void GoToBootScene()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/BootScene.unity");
    }
    
    [MenuItem("Tools/SceneControl/GoToTitleScene &#2")]
    private static void GoToTitleScene()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/TitleScene.unity");
    }
    
    [MenuItem("Tools/SceneControl/GoToLobbyScene &#3")]
    private static void GoToLobbyScene()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/LobbyScene.unity");
    }
    
    [MenuItem("Tools/SceneControl/GoToMainScene &#4")]
    private static void GoToMainScene()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainScene.unity");
    }

    [MenuItem("Tools/SceneControl/GoToTestScene &#5")]
    private static void GoToTestScene()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/TestScene.unity");
    }

    [MenuItem("Tools/SceneControl/GoToTestBootScene &#6")]
    private static void GoToTestBootScene()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/TestBootScene.unity");
    }

    [MenuItem("Tools/SceneControl/GoToMission &#7")]
    private static void GoToMission()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Mission.unity");
    }
}