using System.IO;
using UnityEngine;

[System.Serializable]
public class PrivateData
{
    public string privateCode;
}

public class HostNickName
{
    public string nickNameHost;
}

public class ClientNickName
{
    public string nickNameClient;
}

public class HostNumber
{
    public string hostNumber;
}

public class ClientNumber
{
    public string clientNumber;
}

public class DataController : MonoBehaviour
{
    public PrivateData privateData = new PrivateData();
    public HostNickName nicknameHost = new HostNickName();
    public ClientNickName nicknameClient = new ClientNickName();
    public HostNumber hostNumber = new HostNumber();
    public ClientNumber clientNumber = new ClientNumber();

    public bool sceneControl;

    public void Awake()
    {
        DontDestroyOnLoad(gameObject);

        LoadData();
    }

    public void SaveData(string st)
    {
        privateData.privateCode = st;

        string json = JsonUtility.ToJson(privateData);

        File.WriteAllText(Path.Combine(Application.persistentDataPath, "PrivateData.json"), json);
    }

    public void SaveHostNickName(string nickname)
    {
        nicknameHost.nickNameHost = nickname;
        hostNumber.hostNumber = "0";


        string json = JsonUtility.ToJson(nicknameHost);
        string json_ = JsonUtility.ToJson(hostNumber);

        File.WriteAllText(Path.Combine(Application.persistentDataPath, "HostNickNameData.json"), json);
        File.WriteAllText(Path.Combine(Application.persistentDataPath, "HostNumber.json"), json_);
    }

    public void SaveClientNickName(string nickname)
    {
        nicknameClient.nickNameClient = nickname;
        clientNumber.clientNumber = "1";

        string json = JsonUtility.ToJson(nicknameClient);
        string json_ = JsonUtility.ToJson(clientNumber);

        File.WriteAllText(Path.Combine(Application.persistentDataPath, "ClientNickNameData.json"), json);
        File.WriteAllText(Path.Combine(Application.persistentDataPath, "ClientNumber.json"), json_);
    }

    public void LoadData()
    {

        string path = Path.Combine(Application.persistentDataPath, "PrivateData.json");


        if (File.Exists(path))
        {

            string json = File.ReadAllText(path);
            privateData = JsonUtility.FromJson<PrivateData>(json);
            //Debug.Log(path +     privateData.privateCode);
        }
    }

    public void LoadHostNickName()
    {

        string path = Path.Combine(Application.persistentDataPath, "HostNickNameData.json");
        string path_ = Path.Combine(Application.persistentDataPath, "HostNumber.json");


        if (File.Exists(path) && File.Exists(path_))
        {

            string json = File.ReadAllText(path);
            string json_ = File.ReadAllText(path_);
            nicknameHost = JsonUtility.FromJson<HostNickName>(json);
            hostNumber = JsonUtility.FromJson<HostNumber>(json_);

            Debug.Log(path + nicknameHost.nickNameHost + "       " + hostNumber.hostNumber);
        }
    }

    public void LoadClientNickName()
    {

        string path = Path.Combine(Application.persistentDataPath, "ClientNickNameData.json");
        string path_ = Path.Combine(Application.persistentDataPath, "ClientNumber.json");


        if (File.Exists(path) && File.Exists(path_))
        {

            string json = File.ReadAllText(path);
            string json_ = File.ReadAllText(path_);
            nicknameClient = JsonUtility.FromJson<ClientNickName>(json);
            clientNumber = JsonUtility.FromJson<ClientNumber>(json_);

            Debug.Log(path + nicknameClient.nickNameClient + "       " + clientNumber.clientNumber);
        }
    }
}