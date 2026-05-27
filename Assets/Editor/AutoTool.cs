using System.IO;
using UnityEditor;
using UnityEngine;
public class AutoTool
{
    private static void RemoveMissingScripts(GameObject obj)
    {
        // 현재 오브젝트 검사
        if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(obj) > 0)
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
        }

        // 자식 오브젝트 검사
        foreach (Transform child in obj.transform)
        {
            if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) > 0)
            {
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
            }
        }
    }

    [MenuItem("Denba/AutoTool/RemoveMissingScriptSelectedObject")]
    public static void RemoveMissingScriptSelectedObject()
    {
        foreach (var obj in Selection.gameObjects)
        {
            RemoveMissingScripts(obj);
        }
    }


    [MenuItem("Tools/Settings/Delete Settings JSON")]
    public static void DeleteSettings()
    {
        string path = Path.Combine(Application.persistentDataPath, "Config/config.json");

        if (File.Exists(path))
        {
            File.Delete(path);
            Debug.Log($"Settings file deleted: {path}");
        }
        else
        {
            Debug.LogWarning("Settings file not found.");
        }
    }

}
