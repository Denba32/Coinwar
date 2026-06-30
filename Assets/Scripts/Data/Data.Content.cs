using MemoryPack;
using StockGame.Scripts.Define;
using StockGame.Scripts.Manager;
using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.U2D.Animation;
using static StockGame.Scripts.Define.GameDefine.GraphicsDefine;

namespace StockGame.Scripts.Datas
{
    [MemoryPackable]
    public partial class ConfigData
    {
        // ── Audio ──
        public float MasterVolume { get; set; } = 1.0f;
        public float BgmVolume { get; set; } = 1.0f;
        public float SfxVolume { get; set; } = 1.0f;

        // ── Graphics ──
        public int ResolutionWidth { get; set; } = 1920;
        public int ResolutionHeight { get; set; } = 1080;
        public int RefreshRate { get; set; } = 60;
        public DisplayModeOption WindowMode { get; set; } = DisplayModeOption.Fullscreen;
    }

    [Serializable]
    public struct RelayServerData
    {
        public string JoinCode;
        public string IPv4Address;
        public ushort Port;
        public Guid AllocationId;
        public byte[] AllocationIdBytes;
        public byte[] ConnectionData;
        public byte[] Key;
        public byte[] HostConnectionData;
    }

    public enum CharacterType
    {
        Penguin = 1,
    }
    /// <summary>
    /// 서버에 연결되었을 때 정보를 기입하는 용도.
    /// LobbyManager에서 사용중
    /// </summary>
    public struct NetworkPlayerData : INetworkSerializable, IEquatable<NetworkPlayerData>
    {
        public int Index;
        public ulong ClientId;
        public CharacterType CharacterType;
        public FixedString32Bytes NickName;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref Index);
            serializer.SerializeValue(ref ClientId);
            serializer.SerializeValue(ref CharacterType);
            serializer.SerializeValue(ref NickName);
        }
        public bool Equals(NetworkPlayerData other)
        {
            return ClientId == other.ClientId &&
                   NickName.Equals(other.NickName);
        }
        public override bool Equals(object obj)
        {
            return obj is NetworkPlayerData other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Index, ClientId, NickName);
        }

        public string GetNickname() => NickName.ToString();
        public Sprite GetProfileImage()
        {
            return ResourceManager.Instance.Load<Sprite>(string.Format(GameDefine.CharacterProfilePath, CharacterType.ToString(), (int)CharacterType, Index.ToString("D2")),
                ResourceDirectory.Images);
        }
        public SpriteLibraryAsset GetSpriteLibraryAsset()
        {
            return ResourceManager.Instance.Load<SpriteLibraryAsset>(
                string.Format(GameDefine.CharacterLibraryAssetPath, CharacterType.ToString(), (int)CharacterType, Index.ToString("D2")),
                ResourceDirectory.Prefabs);
        }
    }
}