using Cysharp.Threading.Tasks;
using StockGame.Scripts.Define;
using StockGame.Scripts.Scenes;
using System;
using System.Threading;

namespace StockGame.Common.Interfaces
{
    // 초기 랜덤 주식 : 정해진 값이지만 이를 랜덤으로하여 데이터를 전달함
    /* Scene */
    public interface ISceneController : IDisposable
    {
        UniTask Enter(CancellationToken token); // Scene 시작
        UniTask Initialize(CancellationToken token); // Scene 초기화
        UniTask Exit(CancellationToken token); // Scene 종료
        void SetView(SceneBaseView view);
    }
}