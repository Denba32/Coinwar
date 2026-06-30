namespace StockGame.Scripts.Define
{
    public enum PlayerState
    {
        Idle,
        Move
    }

    public enum ResourceDirectory
    {
        None,
        Images,
        Prefabs,
        Sounds,
        Datas
    }

    public enum UIType
    {
        Scene,
        Popup,
        Quick
    }

    public enum SoundType
    {
        Master,
        BGM,
        SFX,
        SFX_3D
    }

    public enum SoundEffectType
    {
        None,
        FootStep,
        Skill,
        UI,
        Mission
    }

    public enum SceneEnum
    {
        BootScene,
        TitleScene,
        LobbyScene,
        MainScene,
        TestBootScene,
        Mission
    }
    
    public enum MissionType : int
    {
        Normal,
        Job
    }
}