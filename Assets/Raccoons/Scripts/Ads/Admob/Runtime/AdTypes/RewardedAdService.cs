using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using GoogleMobileAds.Api;
using Raccoons.Ads.Admob.Data;
using UnityEngine;

namespace Raccoons.Ads.Admob.AdTypes
{
    public enum RewardedAdPlacement
    {
        Default = 0,
    }

    public class RewardedAdService : BaseAdService
    {
        private RewardedAd _rewardedAd;
        private RewardedAd _extraRewardedAd;
    
        private DateTime _extraRewardedAdLoadTime;
    
        private CancellationTokenSource _rewardedExpirationTokenSource;
        private CancellationTokenSource _extraRewardedExpirationTokenSource;

        private bool IsHighRewardedLoading { get; set; } = false;

        public event Action OnRewardedVideoAvailableToShow;
        public event Action OnRewardedShown;
        public event Action OnRewardedClosed;
        public event Action<AdValue> OnAdPaid;

        public RewardedAdService(AdMobConfig config) : base(config)
        {
            _extraRewardedAdLoadTime = DateTime.MinValue;
        }

        public override void Load()
        {
            LoadRewardedAd();
        }

        private void LoadRewardedAd()
        {
            Debug.Log("[RewardedAdService] LoadRewardedAd():Loading the rewarded ad.");
            if (IsRewardedAvailable())
            {
                Debug.Log("[RewardedAdService] LoadRewardedAd(): rewarded ad is already loaded and can be shown");
                return;
            }

            if (!CanLoad())
            {
                Debug.Log("[RewardedAdService] LoadRewardedAd(): Can't start load reward, it's already loading");
                return;
            }

            IsLoading = true;
            var adRequest = new AdRequest();
            RewardedAd.Load(config.RewardedAdUnitId, adRequest, OnRewardedLoadCallback);
        }

        public void LoadExtraRewardedAd()
        {
            Debug.Log("[RewardedAdService] LoadExtraRewardedAd(): Loading the extra rewarded ad.");
            if (IsExtraRewardedAvailable())
            {
                Debug.Log("LoadExtraRewardedAd(): extra rewarded ad is already loaded and can be shown");
                return;
            }
            
            if(IsHighRewardedLoading)
                Debug.Log("[RewardedAdService] LoadExtraRewardedAd(): Can't start load reward, it's already loading");

            IsHighRewardedLoading = true;
            var request = new AdRequest();
            RewardedAd.Load(config.HighRewardAdUnitId, request, OnExtraRewardedLoadCallback);
        }

        public override bool IsAvailable()
        {
            return IsRewardedAvailable() || IsExtraRewardedAvailable();
        }

        public bool IsRewardedAvailable()
        {
            if (_rewardedAd == null || !_rewardedAd.CanShowAd())
                return false;

            return !IsExpired();
        }

        public bool IsExtraRewardedAvailable()
        {
            if (_extraRewardedAd == null || !_extraRewardedAd.CanShowAd())
                return false;

            return !IsExtraRewardedExpired();
        }

        private bool IsExtraRewardedExpired()
        {
            if (_extraRewardedAdLoadTime == DateTime.MinValue)
                return false;

            double secondsSinceLoad = (DateTime.Now - _extraRewardedAdLoadTime).TotalSeconds;
            return secondsSinceLoad >= config.AdvertisementExpiredDurationSeconds;
        }
    
        private void StartExtraRewardedExpirationCheck()
        {
            StopExtraRewardedExpirationCheck();

            _extraRewardedExpirationTokenSource = new CancellationTokenSource();
            CheckExtraRewardedExpirationRoutine().Forget();
        }

        private void StopExtraRewardedExpirationCheck()
        {
            if (_extraRewardedExpirationTokenSource != null)
            {
                _extraRewardedExpirationTokenSource.Cancel();
                _extraRewardedExpirationTokenSource.Dispose();
                _extraRewardedExpirationTokenSource = null;;
            }
        }

        private async UniTask CheckExtraRewardedExpirationRoutine()
        {
            while (true)
            {
                await UniTask.WaitForSeconds(config.ExpireCheckDelay, true);
            
                if (IsExtraRewardedExpired())
                {
                    Debug.Log("RewardedAdService: Extra rewarded ad expired, reloading...");
                    StopExtraRewardedExpirationCheck();
                    _extraRewardedAd.Destroy();
                    _extraRewardedAd = null;
                    LoadExtraRewardedAd();
                    return;
                }
            }
        }

        public override void Destroy()
        {
            StopExpirationCheck();
            StopExtraRewardedExpirationCheck();
        
            if (_rewardedAd != null)
            {
                _rewardedAd.Destroy();
                _rewardedAd = null;
            }

            if (_extraRewardedAd != null)
            {
                _extraRewardedAd.Destroy();
                _extraRewardedAd = null;
            }
        }

        public void ShowRewarded(RewardedAdPlacement placement, Action onFinish, Action onNotAvailable = null)
        {
            RewardedAd rewardedAd;
        
            if (IsRewardedAvailable())
            {
                rewardedAd = _rewardedAd;
            }
            else if (IsExtraRewardedAvailable())
            {
                rewardedAd = _extraRewardedAd;
                Debug.Log("[RewardedAdService] ShowRewardedAd(): extra reward video will be shown instead: no reward video available");
            }
            else
            {
                Debug.Log("[RewardedAdService] ShowRewardedAd(): reward is not ready");
                onNotAvailable?.Invoke();
                LoadRewardedAd();
                LoadExtraRewardedAd();
                return;
            }

            ShowRewardedAd(rewardedAd, onFinish);
        }

        public void ShowExtraRewardedAd(Action onFinish, Action onNotAvailable = null)
        {
            if (IsExtraRewardedAvailable())
            {
                ShowRewardedAd(_extraRewardedAd, onFinish);
            }
            else
            {
                Debug.Log("[RewardedAdService] Extra rewarded not ready");
                onNotAvailable?.Invoke();
                LoadExtraRewardedAd();
            }
        }

        private void ShowRewardedAd(RewardedAd ad, Action onFinish)
        {
            Action previousHandler = null;
            previousHandler = () =>
            {
                OnRewardedClosed -= previousHandler;
                onFinish?.Invoke();
            };
            OnRewardedClosed += previousHandler;

            ad.Show(reward =>
            {
                OnRewardedShown?.Invoke();
                Debug.Log($"[RewardedAdService] Rewarded ad rewarded the user. Type: {reward.Type}, amount: {reward.Amount}.");
            });
        }

        private void OnRewardedLoadCallback(RewardedAd ad, LoadAdError error)
        {
            if (error != null || ad == null)
            {
                Debug.LogError("[RewardedAdService] Rewarded ad failed to load an ad with error : " + error);
                DelayCall(LoadRewardedAd, config.RetryLoadDelay);
                return;
            }

            if (_rewardedAd != null)
            {
                _rewardedAd.Destroy();
                _rewardedAd = null;
            }
        
            _rewardedAd = ad;
            MarkAsLoaded();
            Debug.Log("[RewardedAdService] Rewarded ad loaded with response : " + _rewardedAd.GetResponseInfo());
        
            RegisterRewardedEventHandlers(_rewardedAd);
            RegisterRewardedReloadEventHandlers(_rewardedAd);
        
            OnRewardedVideoAvailableToShow?.Invoke();
        }

        private void OnExtraRewardedLoadCallback(RewardedAd ad, LoadAdError error)
        {
            if (error != null || ad == null)
            {
                Debug.LogError($"[RewardedAdService] Extra rewarded load failed: {error}");
                DelayCall(LoadExtraRewardedAd, config.RetryLoadDelay).Forget();
                return;
            }

            if (_extraRewardedAd != null)
            {
                _extraRewardedAd.Destroy();
                _extraRewardedAd = null;
            }

            MarkExtraRewardAsLoaded(ad);
            Debug.Log("[RewardedAdService] Extra Rewarded ad loaded with response : " + _extraRewardedAd.GetResponseInfo());
        
            RegisterRewardedEventHandlers(_extraRewardedAd);
            RegisterExtraRewardedReloadEventHandlers(_extraRewardedAd);
        
            OnRewardedVideoAvailableToShow?.Invoke();
        }

        private void MarkExtraRewardAsLoaded(RewardedAd ad)
        {
            _extraRewardedAd = ad;
            _extraRewardedAdLoadTime = DateTime.Now;
            IsHighRewardedLoading = false;
            StartExtraRewardedExpirationCheck();
        }

        private void RegisterRewardedEventHandlers(RewardedAd ad)
        {
            ad.OnAdPaid += (AdValue adValue) =>
            {
                Debug.Log(String.Format("[RewardedAdService] Rewarded ad paid {0} {1}.", adValue.Value, adValue.CurrencyCode));
                OnAdPaid?.Invoke(adValue);
            };

            ad.OnAdImpressionRecorded += () =>
            {
                Debug.Log("[RewardedAdService] Rewarded ad recorded an impression.");
            };

            ad.OnAdClicked += () =>
            {
                Debug.Log("[RewardedAdService] Rewarded ad was clicked.");
            };

            ad.OnAdFullScreenContentOpened += () =>
            {
                Debug.Log("[RewardedAdService] Rewarded ad full screen content opened.");
            };
        }

        private void RegisterRewardedReloadEventHandlers(RewardedAd ad)
        {
            ad.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("[RewardedAdService] Rewarded Ad full screen content closed.");
                OnRewardedClosed?.Invoke();
                LoadRewardedAd();
            };

            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                Debug.LogError("[RewardedAdService] Rewarded ad failed to open full screen content with error : " + error);
                LoadRewardedAd();
            };
        }

        private void RegisterExtraRewardedReloadEventHandlers(RewardedAd ad)
        {
            ad.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("[RewardedAdService] Rewarded Ad full screen content closed.");
                OnRewardedClosed?.Invoke();
                LoadExtraRewardedAd();
            };

            ad.OnAdFullScreenContentFailed += (AdError error) =>
            {
                Debug.LogError("[RewardedAdService] Rewarded ad failed to open full screen content with error : " + error);
                LoadExtraRewardedAd();
            };
        }
    }
}

