using Cysharp.Threading.Tasks;
using Denba.Common;
using StockGame.Scripts.Utility;
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
        private NetworkVariable<float> currentTax;

        public NetworkList<NetworkStockInfo> StockInfos => stockInfos;
        public NetworkList<NetworkPurchasedStock> PurchasedStocks => purchasedStocks;
        public NetworkVariable<float> CurrentTax => currentTax;
        #endregion Global Field

        private Subject<NetworkPurchasedStock> onPurchase = new();
        private Subject<NetworkPurchasedStock> onSell = new();
        public IObservable<NetworkPurchasedStock> OnPurchase => onPurchase;
        public IObservable<NetworkPurchasedStock> OnSell => onSell;

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
            currentTax = new NetworkVariable<float>(default);

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
            if (!IsServer || !IsSpawned)
            {
                Debug.LogWarning($"[StockManager] InitializeByGameStart 스킵 (IsServer={IsServer}, IsSpawned={IsSpawned})");
                return;
            }

            _disposables?.Dispose();
            _disposables = new();
            stockInfos?.Initialize(this);
            purchasedStocks?.Initialize(this);

            // 새 게임 시작이므로 이전 게임의 보유 주식 정보가 남아있지 않도록 방어적으로 정리.
            purchasedStocks?.Clear();

            if (GameManager.Instance != null)
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
                    // DefaultStockPrice 범위 초과 방지
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
                        MinGainRate = 0,
                        MaxGainRate = gainRates,
                        MinLossRate = 0,
                        MaxLossRate = lossRates
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

        /// <summary>
        /// 상승/하락 확률과 금액 변동 비율을 통한 주가 변동 계산
        /// </summary>
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

            // 다음 게임을 위해 데이터 초기화
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
                    return price >= range[0] && price < range[1];
                });

            if (match == null)
            {
                Debug.LogWarning($"[StockManager] price({price})에 해당하는 확률 구간을 찾지 못했습니다. 0으로 대체합니다.");
                return 0f;
            }

            var probability = direction == StockDirection.Increase ? match.GainProbability : match.LossProbability;
            return probability;
        }

        public float GetGainRate(float price)
        {
            var match = stockPriceInfoList
                .FirstOrDefault(info =>
                {
                    var range = info.StockPrice;
                    return price >= range[0] && price < range[1];
                });

            return match?.GainRate ?? 0;
        }

        public float GetLossRate(float price)
        {
            var match = stockPriceInfoList
                .FirstOrDefault(info =>
                {
                    var range = info.StockPrice;
                    return price >= range[0] && price < range[1];
                });
            return match?.LossRate ?? 0;
        }

        public bool Purchase(int stockId, int purchaseCount)
        {
            if (purchaseCount <= 0) return false;
            var purchaseTargetStock = GetStockInfoByIndex(stockId);
            PurchaseStockRpc(purchaseTargetStock, purchaseCount);
            return true;
        }

        public bool PurchaseAll(int stockId)
        {
            if (stockId < 0) return false;
            var purchaseTargetStock = GetStockInfoByIndex(stockId);
            PurchaseAllStockRpc(purchaseTargetStock);
            return true;
        }

        public bool Sell(int stockIndex, int sellCount)
        {
            if (stockIndex < 0 || sellCount <= 0) return false;
            SellStockRpc(stockIndex, sellCount);
            return true;
        }

        public bool SellAll(int stockIndex)
        {
            if (stockIndex < 0) return false;
            SellAllStockRpc(stockIndex);
            return true;
        }

        public List<NetworkPurchasedStock> GetPurchasedStockListByOwnerId(ulong ownerId)
        {
            List<NetworkPurchasedStock> list = new();
            foreach (var stock in purchasedStocks)
            {
                if (stock.OwnerId == ownerId)
                {
                    list?.Add(stock);
                }
            }
            return list;
        }

        /// <summary>
        /// 주식 구매 요청
        /// </summary>
        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void PurchaseStockRpc(NetworkStockInfo stockInfo, int count, RpcParams rpcParams = default)
        {
            var senderId = rpcParams.Receive.SenderClientId;

            var playerInfo = GameManager.Instance.GetPlayerInfoByClientId(senderId);
            var totalPrice = stockInfo.AfterPrice * count;
            if (playerInfo.Money < totalPrice) return;
            UpdateStockPurchasedCount(stockInfo, count);
            GameManager.Instance.UpdateMoneyByClientId(senderId, playerInfo.Money - totalPrice);

            var currentPurchasedStock = GetPurchaseStock(senderId, stockInfo.StockId);

            // 해당 주식을 구매한 적이 없을 경우
            if (!currentPurchasedStock.IsValid)
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

            Debug.Log($"본인이 구매한 주식수 : {currentPurchasedStock.PurchaseCount}");

            NotifyPurchaseStockRpc(currentPurchasedStock, RpcTarget.Single(senderId, RpcTargetUse.Temp));
        }

        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void PurchaseAllStockRpc(NetworkStockInfo stockInfo, RpcParams rpcParams = default)
        {
            var senderId = rpcParams.Receive.SenderClientId;
            var playerInfo = GameManager.Instance.GetPlayerInfoByClientId(senderId);
            if (playerInfo.Money <= 0) return;
            var currentPrice = stockInfo.AfterPrice;
            int purchasableCount = (int)(playerInfo.Money / currentPrice);
            var totalPrice = stockInfo.AfterPrice * purchasableCount;
            if (playerInfo.Money < totalPrice) return;
            UpdateStockPurchasedCount(stockInfo, purchasableCount);
            GameManager.Instance.UpdateMoneyByClientId(senderId, playerInfo.Money - totalPrice);

            var currentPurchasedStock = GetPurchaseStock(senderId, stockInfo.StockId);

            // 해당 주식을 구매한 적이 없을 경우
            if (!currentPurchasedStock.IsValid)
            {
                currentPurchasedStock = new NetworkPurchasedStock
                {
                    StockId = stockInfo.StockId,
                    OwnerId = senderId,
                    StockName = stockInfo.StockName,
                    PurchasePrice = stockInfo.AfterPrice,
                    PurchaseCount = purchasableCount,
                };
                purchasedStocks?.Add(currentPurchasedStock);
            }
            else
            {
                var index = purchasedStocks.IndexOf(currentPurchasedStock);
                currentPurchasedStock.PurchaseCount += purchasableCount;
                purchasedStocks[index] = currentPurchasedStock;
            }

            Debug.Log($"본인이 구매한 주식수 : {currentPurchasedStock.PurchaseCount}");

            NotifyPurchaseStockRpc(currentPurchasedStock, RpcTarget.Single(senderId, RpcTargetUse.Temp));
        }

        /// <summary>
        /// NetworkStockInfo 구매된 주식 개수 갱신
        /// </summary>
        private void UpdateStockPurchasedCount(NetworkStockInfo stockInfo, int count)
        {
            var index = stockInfos.IndexOf(stockInfo);
            if (index < 0) return;
            stockInfo.PurchasedCount += count;
            Debug.Log($"{stockInfo.StockName} 주식 발행량 : {stockInfo.PurchasedCount - count} | 구매 주식 수 : {count}");
            stockInfos[index] = stockInfo;
        }

        /// <summary>
        /// StockId로 구매된 주식 개수 갱신
        /// </summary>
        private void UpdateStockPurchasedCount(int stockId, int count)
        {
            for (int i = 0; i < stockInfos.Count; i++)
            {
                if (stockInfos[i].StockId != stockId) continue;
                var info = stockInfos[i];
                info.PurchasedCount -= count;
                stockInfos[i] = info;
                break;
            }
        }

        /// <summary>
        /// 주식 판매 요청
        /// </summary>
        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void SellStockRpc(int stockIndex, int sellCount, RpcParams rpcParams = default)
        {
            Debug.Log($"SellStockRpc 호출한 사람 : {rpcParams.Receive.SenderClientId}");
            var holdingStock = GetPurchaseStock(rpcParams.Receive.SenderClientId, stockIndex);

            if (!holdingStock.IsValid)
            {
                Debug.Log("판매하려는 주식을 구매한 기록이 없어서 Return");
                return;
            }

            var count = sellCount > holdingStock.PurchaseCount ? holdingStock.PurchaseCount : sellCount;
            Debug.Log($"판매하려는 주식 수 {count}");
            var index = purchasedStocks.IndexOf(holdingStock);

            // 모든 주식을 다 팔 경우
            if (holdingStock.PurchaseCount - count <= 0)
                purchasedStocks.Remove(holdingStock);

            holdingStock.PurchaseCount -= count;
            if (holdingStock.PurchaseCount > 0)
                purchasedStocks[index] = holdingStock;

            Debug.Log($"{holdingStock.StockName}의 남은 주식 수 {holdingStock.PurchaseCount}");
            var currentStockInfo = GetStockInfoByIndex(stockIndex);
            var price = currentStockInfo.AfterPrice * count;
            UpdateStockPurchasedCount(stockIndex, count);
            var localInfo = Managers.Game.GetPlayerInfoByClientId(rpcParams.Receive.SenderClientId);
            Managers.Game.UpdateMoneyByClientId(rpcParams.Receive.SenderClientId, price + localInfo.Money);
            NotifySellStockRpc(holdingStock, RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
        }

        /// <summary>
        /// 소유 주식 전체 판매
        /// </summary>
        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void SellAllStockRpc(int stockIndex, RpcParams rpcParams = default)
        {
            Debug.Log($"SellAllStockRpc 호출한 사람 : {rpcParams.Receive.SenderClientId}");
            var holdingStock = GetPurchaseStock(rpcParams.Receive.SenderClientId, stockIndex);
            var currentCount = holdingStock.PurchaseCount;
            Debug.Log($"{holdingStock.StockName} 주식 전체 판매 : {holdingStock.PurchaseCount}");
            holdingStock.PurchaseCount = 0;
            purchasedStocks.Remove(holdingStock);
            var currentStockInfo = GetStockInfoByIndex(stockIndex);
            currentStockInfo.PurchasedCount = Mathf.Max(currentStockInfo.PurchasedCount - currentCount, 0);
            var price = currentStockInfo.AfterPrice * currentCount;
            var localInfo = Managers.Game.GetPlayerInfoByClientId(rpcParams.Receive.SenderClientId);
            Managers.Game.UpdateMoneyByClientId(rpcParams.Receive.SenderClientId, price + localInfo.Money);
            NotifySellStockRpc(holdingStock, RpcTarget.Single(rpcParams.Receive.SenderClientId, RpcTargetUse.Temp));
        }

        /// <summary>
        /// 구매 후 이벤트 스트림 발행
        /// </summary>
        [Rpc(SendTo.SpecifiedInParams)]
        private void NotifyPurchaseStockRpc(NetworkPurchasedStock purchaseStock, RpcParams rpcParams)
        {
            Debug.Log($"NotifyPurchaseStockRpc {rpcParams.Receive.SenderClientId}에게 이벤트 스트림 발행 ");
            onPurchase?.OnNext(purchaseStock);
        }

        /// <summary>
        /// 판매 후 이벤트 스트림 발행
        /// </summary>
        [Rpc(SendTo.SpecifiedInParams)]
        private void NotifySellStockRpc(NetworkPurchasedStock purchasedStock, RpcParams rpcParams)
        {
            Debug.Log($"NotifySellStockRpc {rpcParams.Receive.SenderClientId}에게 이벤트 스트림 발행 ");
            onSell?.OnNext(purchasedStock);
        }

        /// <summary>
        /// 구매한 주식 정보 가져오기
        /// </summary>
        public NetworkPurchasedStock GetPurchaseStock(ulong ownerId, int stockId)
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

        /// <summary>
        /// 주식 증권 정보 가져오기
        /// </summary>
        private NetworkStockInfo GetStockInfoByIndex(int stockIndex)
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

        /// <summary>
        /// 주식 정보 갱신
        /// </summary>
        [Rpc(SendTo.Server, RequireOwnership = false)]
        private void UpdateStockRpc(NetworkStockInfo stockInfo)
        {
            var index = stockInfos.IndexOf(stockInfo);
            var updatedStockInfo = UpdatePrice(stockInfo);
            var applyRate = UpdateRateByPrice(updatedStockInfo);

            Debug.Log($"CurrentPrice : {stockInfo.AfterPrice}" +
                $"MinGainRate : {applyRate.MinGainRate} | MaxGainRate : {applyRate.MaxGainRate}" +
                $"MinLossRate : {applyRate.MinLossRate} | MaxLossRate : {applyRate.MaxLossRate} | Penalty : {Mathf.Min((stockInfo.PurchasedCount / 10) * 0.01f, 0.1f)} | " +
                $"GainProbability : {applyRate.GainProbability} | LossProbability : {applyRate.LossProbability}");
            stockInfos[index] = applyRate;
        }

        /// <summary>
        /// 가격 변동
        /// </summary>
        private NetworkStockInfo UpdatePrice(NetworkStockInfo stockInfo)
        {
            long currentPrice = stockInfo.AfterPrice;

            float total = stockInfo.GainProbability + stockInfo.LossProbability;
            float random = Random.Range(0f, total);

            random -= stockInfo.GainProbability;
            bool isGain = random <= 0;

            float maxRate = isGain ? stockInfo.MaxGainRate : stockInfo.MaxLossRate;
            float minRate = isGain ? stockInfo.MinGainRate : stockInfo.MinLossRate;
            float appliedRate = Random.Range(minRate, maxRate);

            if (!isGain)
            {
                float penalty = Mathf.Min((stockInfo.PurchasedCount / 10) * 0.01f, 0.1f);
                appliedRate += penalty;
            }

            long priceDelta = (long)(currentPrice * appliedRate);
            long nextPrice = isGain ? (currentPrice + priceDelta) : (currentPrice - priceDelta);
            nextPrice = (nextPrice / 100) * 100;

            if (nextPrice < 1000) nextPrice = 1000;

            stockInfo.BeforePrice = currentPrice;
            stockInfo.AfterPrice = nextPrice;

            return stockInfo;
        }

        /// <summary>
        /// 가격에 따른 주가 변동 표시 갱신
        /// </summary>
        private NetworkStockInfo UpdateRateByPrice(NetworkStockInfo stockInfo)
        {
            var currentPrice = stockInfo.AfterPrice;
            var gainRates = GetGainRate(currentPrice);
            var lossRates = GetLossRate(currentPrice);
            var increaseProbability = GetProbabilityByPrice(currentPrice, StockDirection.Increase);
            var decreaseProbability = GetProbabilityByPrice(currentPrice, StockDirection.Decrease);

            stockInfo.GainProbability = increaseProbability;
            stockInfo.LossProbability = decreaseProbability;
            stockInfo.MinGainRate = 0;
            stockInfo.MaxGainRate = gainRates;
            stockInfo.MinLossRate = 0;
            stockInfo.MaxLossRate = lossRates;
            return stockInfo;
        }

        /// <summary>
        /// 전체 주가 계산
        /// </summary>
        public long CalculateTotalStock(ulong localClientId)
        {
            long totalMoney = 0;

            foreach (var stock in purchasedStocks)
            {
                if (stock.OwnerId == localClientId)
                {
                    var currentStockInfo = GetStockInfoByIndex(stock.StockId);
                    totalMoney += currentStockInfo.AfterPrice * stock.PurchaseCount;
                }
            }
            return totalMoney;
        }

        /// <summary>
        /// 세금 랜덤 변경
        /// </summary>
        public float ChangeTaxByRandom()
        {
            if (!IsServer) return 0f;
            var random = Random.Range(0.1f, 0.3f);
            random = Mathf.Floor(random * 100f) * 0.01f;
            currentTax.Value = random;
            Debug.Log($"현재 세금 : {currentTax.Value}");
            return random;
        }

        public void RemovePlayerStocks(ulong clientId)
        {
            if (!IsServer) return;
            if (purchasedStocks == null || stockInfos == null) return;

            for (int i = purchasedStocks.Count - 1; i >= 0; i--)
            {
                if (purchasedStocks[i].OwnerId != clientId) continue;

                var stock = purchasedStocks[i];

                var stockIndex = -1;
                for (int j = 0; j < stockInfos.Count; j++)
                {
                    if (stockInfos[j].StockId != stock.StockId) continue;
                    stockIndex = j;
                    break;
                }

                if (stockIndex >= 0)
                {
                    var stockInfo = stockInfos[stockIndex];
                    stockInfo.PurchasedCount = Mathf.Max(stockInfo.PurchasedCount - stock.PurchaseCount, 0);
                    stockInfos[stockIndex] = stockInfo;
                }

                purchasedStocks.RemoveAt(i);
            }
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