using Cysharp.Threading.Tasks;
using Denba.Common;
using StockGame.Scripts.Define;
using StockGame.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UniRx;
using Unity.Netcode;
using UnityEngine;
using static StockGame.Scripts.Define.GameDefine.StockDefine;
using Random = UnityEngine.Random;

namespace StockGame.Scripts.Manager
{
    public class StockManager : NetworkSingleton<StockManager>
    {
        private CompositeDisposable _disposables = new();

        #region Local Field
        private List<StockInfo> stockInfoList = new();
        private List<StockPriceInfo> stockPriceInfoList = new();
        public List<StockInfo> StockInfoList => stockInfoList;
        public List<StockPriceInfo> StockPriceInfoList => stockPriceInfoList;
        #endregion Local Field

        #region Global Field
        private NetworkList<NetworkStockInfo> stockInfos;
        private NetworkList<NetworkPurchasedStock> purchasedStocks;

        public NetworkList<NetworkStockInfo> StockInfos => stockInfos;
        public NetworkList<NetworkPurchasedStock> PurchasedStocks => purchasedStocks;
        #endregion Global Field

        private Subject<NetworkPurchasedStock> onPurchase = new();
        public IObservable<NetworkPurchasedStock> OnPurchase => onPurchase;

        public override UniTask Initialize()
        {
            var stockNameTable = Managers.Master.GetTable("StockNameTable");
            var stockPriceTable = Managers.Master.GetTable("StockTable");
            if (stockNameTable.RowCount <= 0 || stockPriceTable.RowCount <= 0)
            {
                Debug.LogWarning("데이터를 찾을 수 없습니다.");
                return UniTask.CompletedTask;
            }

            var stockNames = stockNameTable.GetAllData();
            var stockPrices = stockPriceTable.GetAllData();

            foreach (var stockName in stockNames)
                stockInfoList?.Add(new StockInfo(stockName));

            foreach (var stockPrice in stockPrices)
                stockPriceInfoList?.Add(new StockPriceInfo(stockPrice));

            stockInfos = new NetworkList<NetworkStockInfo>(writePerm: NetworkVariableWritePermission.Server);
            purchasedStocks = new NetworkList<NetworkPurchasedStock>(writePerm: NetworkVariableWritePermission.Server);

            return base.Initialize();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsServer) return;
            stockInfos?.Clear();
            purchasedStocks?.Clear();
        }

        /// <summary>
        /// 게임이 시작되었을 때의 초기화
        /// </summary>
        public void InitializeByGameStart()
        {
            _disposables?.Dispose();
            _disposables = new();
            stockInfos?.Initialize(this);
            purchasedStocks?.Initialize(this);
            GameManager.Instance.CurrentRound.ObserveEveryValueChanged(round => round.Value).Subscribe(OnRoundChanged).AddTo(_disposables);
            GameManager.Instance.OnGameStart.Subscribe(_ => InitializeStock()).AddTo(_disposables);
            GameManager.Instance.OnGameFinish.Subscribe(_ => FinishGame()).AddTo(_disposables);

            InitializeStock();
        }

        private void InitializeStock()
        {
            if (!IsServer || !IsSpawned) return;

            stockInfos?.Clear();

            var copyList = stockInfoList.ToList();
            copyList.Shuffle();

            int index = 0;
            try
            {
                foreach (var stockInfo in copyList)
                {
                    if (index >= DefaultStockPrice.Count)
                    {
                        Debug.LogWarning($"[StockManager] DefaultStockPrice 범위 초과: stockInfoList.Count={copyList.Count}, DefaultStockPrice.Length={DefaultStockPrice.Count}");
                        break;
                    }

                    var price = DefaultStockPrice[index];
                    var gainRates = GetGainRate(price);
                    var lossRates = GetLossRate(price);
                    var stock = new NetworkStockInfo
                    {
                        StockId = stockInfo.StockNameId,
                        StockName = stockInfo.StockKrName,
                        BeforePrice = price,
                        AfterPrice = price,
                        GainProbability = GetProbabilityByPrice(price, StockDirection.Increase),
                        LossProbability = GetProbabilityByPrice(price, StockDirection.Decrease),
                        MinGainRate = gainRates[0],
                        MaxGainRate = gainRates[1],
                        MinLossRate = lossRates[0],
                        MaxLossRate = lossRates[1]
                    };

                    stockInfos?.Add(stock);
                    index++;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
        }

        public async UniTask UpdateStockPrices(int interval, CancellationToken token)
        {
            if (token.IsCancellationRequested) return;
            while (!token.IsCancellationRequested)
            {
                await UniTask.WaitForSeconds(interval * 60, cancellationToken: token);
                UpdateStock();
            }
        }

        public void UpdateStock()
        {
            if (!IsServer) return;
            for (int i = 0; i < stockInfos.Count; i++)
            {
                UpdateStockRpc(stockInfos[i]);
            }
        }

        private void FinishGame()
        {
            _disposables?.Dispose();
            _disposables = new();

            if (IsServer)
            {
                stockInfos?.Clear();
                purchasedStocks?.Clear();
            }
        }

        public float GetProbabilityByPrice(float price, StockDirection direction)
        {
            var match = stockPriceInfoList
                .FirstOrDefault(info =>
                {
                    var range = info.StockPrice;
                    return price >= range[0] && price < range[1] && direction == info.StockUpDown;
                });
            return match != null ? match.StockUpDownRate : 0f;
        }

        public float[] GetGainRate(float price)
        {
            var match = stockPriceInfoList
                .FirstOrDefault(info =>
                {
                    var range = info.StockPrice;
                    return price >= range[0] && price < range[1] && info.StockUpDown == StockDirection.Increase;
                });
            return match.UpPriceRate;
        }

        public float[] GetLossRate(float price)
        {
            var match = stockPriceInfoList
                .FirstOrDefault(info =>
                {
                    var range = info.StockPrice;
                    return price >= range[0] && price < range[1] && info.StockUpDown == StockDirection.Decrease;
                });
            return match.DownPriceRate;
        }

        private void OnRoundChanged(GameDefine.RoundDefine.RoundInfo info)
        {
            switch (info.RoundPhase)
            {
                case GameDefine.RoundDefine.RoundPhase.StockInfo:
                    if (IsServer)
                        purchasedStocks?.Clear();
                    break;
                case GameDefine.RoundDefine.RoundPhase.Explore:
                    break;
                case GameDefine.RoundDefine.RoundPhase.StockPurchase:
                    var localPlayer = GameManager.Instance.GetLocalPlayerInfo();
                    var convertedMoney = (long)GameDefine.StockDefine.DefaultMoney * localPlayer.Coin;
                    localPlayer.Money = convertedMoney;
                    localPlayer.Coin = 0;
                    break;
                case GameDefine.RoundDefine.RoundPhase.ReleaseRanking:
                    break;
            }
        }

        public bool Purchase(int stockIndex, int purchaseCount)
        {
            if (purchaseCount <= 0) return false;
            var purchaseTargetStock = GetStockInfoByIndes(stockIndex);
            PurchaseStockRpc(purchaseTargetStock, purchaseCount);
            return true;
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void PurchaseStockRpc(NetworkStockInfo stockInfo, int count, RpcParams rpcParams = default)
        {
            var senderId = rpcParams.Receive.SenderClientId;

            var playerInfo = GameManager.Instance.GetPlayerInfoByClientId(senderId);
            var totalPrice = stockInfo.AfterPrice * count;
            if (playerInfo.Money < totalPrice) return;

            GameManager.Instance.UpdateMoneyByClientId(senderId, playerInfo.Money - totalPrice);

            var currentPurchasedStock = GetPurchaseStock(senderId, stockInfo.StockId);
            if (currentPurchasedStock == default)
            {
                currentPurchasedStock = new NetworkPurchasedStock
                {
                    StockId = stockInfo.StockId,
                    OwnerId = senderId,
                    StockName = stockInfo.StockName,
                    PurchasePrice = stockInfo.AfterPrice,
                    PurchaseCount = count,
                };
                purchasedStocks?.Add(currentPurchasedStock);
            }
            else
            {
                var index = purchasedStocks.IndexOf(currentPurchasedStock);
                currentPurchasedStock.PurchaseCount += count;
                purchasedStocks[index] = currentPurchasedStock;
            }

            NotifyPurchaseStockRpc(currentPurchasedStock, RpcTarget.Single(senderId, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.SpecifiedInParams)]
        private void NotifyPurchaseStockRpc(NetworkPurchasedStock purchaseStock, RpcParams rpcParams)
        {
            onPurchase?.OnNext(purchaseStock);
        }

        private NetworkPurchasedStock GetPurchaseStock(ulong ownerId, int stockId)
        {
            NetworkPurchasedStock purchased = default;
            foreach (var stock in purchasedStocks)
            {
                if (stock.OwnerId == ownerId && stock.StockId == stockId)
                {
                    purchased = stock;
                    break;
                }
            }
            return purchased;
        }

        private NetworkStockInfo GetStockInfoByIndes(int stockIndex)
        {
            NetworkStockInfo stockInfo = default;
            foreach (var stock in stockInfos)
            {
                if (stock.StockId == stockIndex)
                {
                    stockInfo = stock;
                }
            }
            return stockInfo;
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void UpdateStockRpc(NetworkStockInfo stockInfo)
        {
            var index = stockInfos.IndexOf(stockInfo);
            var updatedStockInfo = UpdatePrice(stockInfo);
            var applyRate = UpdateRateByPrice(updatedStockInfo);

            Debug.Log($"CurrentPrice : {stockInfo.AfterPrice}" +
                $"MinGainRate : {applyRate.MinGainRate} | MaxGainRate : {applyRate.MaxGainRate}" +
                $"MinLossRate : {applyRate.MinLossRate} | MaxLossRate : {applyRate.MaxLossRate}" +
                $"GainProbability : {applyRate.GainProbability} | LossProbability : {applyRate.LossProbability}");
            stockInfos[index] = applyRate;
        }

        private NetworkStockInfo UpdatePrice(NetworkStockInfo stockInfo)
        {
            long currentPrice = stockInfo.AfterPrice;

            bool isGain = Random.value <= stockInfo.GainProbability;

            float maxRate = isGain ? stockInfo.MaxGainRate : stockInfo.MaxLossRate;
            float minRate = isGain ? stockInfo.MinGainRate : stockInfo.MinLossRate;
            float appliedRate = Random.Range(minRate, maxRate);

            long priceDelta = (long)(currentPrice * appliedRate);
            long nextPrice = isGain ? (currentPrice + priceDelta) : (currentPrice - priceDelta);
            nextPrice = (nextPrice / 100) * 100;

            if (nextPrice < 100) nextPrice = 100;

            stockInfo.BeforePrice = currentPrice;
            stockInfo.AfterPrice = nextPrice;

            return stockInfo;
        }

        private NetworkStockInfo UpdateRateByPrice(NetworkStockInfo stockInfo)
        {
            var currentPrice = stockInfo.AfterPrice;
            var gainRates = GetGainRate(currentPrice);
            var lossRates = GetLossRate(currentPrice);
            var increaseProbability = GetProbabilityByPrice(currentPrice, StockDirection.Increase);
            var decreaseProbability = GetProbabilityByPrice(currentPrice, StockDirection.Decrease);

            stockInfo.GainProbability = increaseProbability;
            stockInfo.LossProbability = decreaseProbability;
            stockInfo.MinGainRate = gainRates[0];
            stockInfo.MaxGainRate = gainRates[1];
            stockInfo.MinLossRate = lossRates[0];
            stockInfo.MaxLossRate = lossRates[1];
            return stockInfo;
        }

        public void UpdateDiff()
        {
            foreach (var stock in stockInfos)
            {
                stock.UpdateDiff();
            }
        }

        public long CalculateTotalStock(ulong localClientId)
        {
            long totalMoney = 0;

            foreach (var stock in purchasedStocks)
            {
                if (stock.OwnerId == localClientId)
                {
                    var currentStockInfo = GetStockInfoByIndes(stock.StockId);
                    totalMoney += currentStockInfo.AfterPrice * stock.PurchaseCount;
                }
            }
            return totalMoney;
        }

        [Rpc(SendTo.Server)]
        private void CalculateResultRpc(long totalCash)
        {
            if (IsServer) return;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();

            stockInfos?.Dispose();
            onPurchase?.Dispose();

            onPurchase = null;
            stockInfos = null;
        }
    }
}