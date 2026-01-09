using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace WordSolitaire.ThirdParty.Ads
{
    public class AdsProvider : IDisposable, IAdsProvider
    {
        private IAdsManager _currentAdsManager;

        private float _interCooldownSeconds = 30;
        private float _rewardWatchedResetInterWhenCooldownLessThan = 30;
        private DateTime _timeFromLastInter = DateTime.Now;
        private bool _isSkipAds = false;
        
        private CancellationTokenSource _bannerLoadingTokenSource;
        
        public bool IsBannerDisplayed { get; set; } = false;
        public bool IsInit { get; private set; }
        public bool IsNoAds { get; private set; } 
        
        public event Action OnAppProviderInit;
        public Action OnRewardedCompleted;
        public event Action OnRewardedLoaded;
        public event Action<bool> OnBannerShown;

        [Inject]
        private void Construct(IAdsManager adsManager)
        {
            _currentAdsManager = adsManager;
        }

        public void Dispose()
        {
            _currentAdsManager.OnRewardedVideoAvailableToShow -= OnRewardAvailableToShow;
        }

        public void Initialize()
        {
            _currentAdsManager.OnRewardedVideoAvailableToShow += OnRewardAvailableToShow;
            if (IsInit == false)
            {
                IsInit = true;
                _currentAdsManager.Initialize(InitializedCallback);
            }
        }

        public void LevelInit()
        {
            if (!IsNoAds && CanShowBanner())
            {
                LoadingBannerLoop().Forget();
            }
        }

        private void InitializedCallback()
        {
            IsInit = true;
            OnAppProviderInit?.Invoke();
        }

        private void OnRewardAvailableToShow()
        {
            OnRewardedLoaded?.Invoke();
        }

        public void DeactivateAds(bool deactivate)
        {
            if (deactivate)
            {
                HideBanner();
            }
            else
            {
                ShowBanner();
            }
        }

        public void RunRewarded(RewardedAdPlacement sourceID, Action onCompleted = null)
        {
            if (_isSkipAds)
            {
                Debug.Log("[AdsProvider] Skipping reward ad: IsNoAds or IsSkipAds is true");
                onCompleted?.Invoke();
                return;
            }
            
            if (_currentAdsManager.IsRewardedVideoAvailable())
            {
                OnRewardedCompleted = onCompleted;
                StartRun();
                _currentAdsManager.ShowRewarded(sourceID, EndRunRewarded);
            }
            else
            {
                _currentAdsManager.LoadRewarded();
            }
        }

        public bool IsExtraRewardedReady()
        {
            if (_isSkipAds) return true;
            return _currentAdsManager.IsExtraRewardedAvailable();
        }

        public bool IsRewardedReady()
        {
            return _currentAdsManager.IsRewardedVideoAvailable() || _isSkipAds;
        }

        public bool IsInterstitialReady()
        {
            bool isCooldownElapsed = (DateTime.Now - _timeFromLastInter).TotalSeconds > _interCooldownSeconds;
            return (_currentAdsManager.IsInterstitialAvailable() && isCooldownElapsed && !IsNoAds) || _isSkipAds;
        }

        public void RunExtraRewarded(Action onCompleted = null)
        {
            if (_isSkipAds)
            {
                onCompleted?.Invoke();
                return;
            }

            if (_currentAdsManager != null && _currentAdsManager.IsExtraRewardedAvailable())
            {
                OnRewardedCompleted = onCompleted;
                StartRun();
                _currentAdsManager.ShowExtraRewardedAd(EndRunRewarded);
            }
            else
            {
                _currentAdsManager?.LoadExtraRewarded();
            }
        }
    
        public void RunInterstitial(string placement,Action onCompleted)
        {
            if (IsNoAds || _isSkipAds)
            {
                Debug.Log("[AdsProvider] Skipping interstitial: IsNoAds or IsSkipAds is true");
                onCompleted?.Invoke();
                return;
            }

            bool isInterstitialReady = _currentAdsManager.IsInterstitialAvailable();
            double secondsRemaining = (DateTime.Now - _timeFromLastInter).TotalSeconds;
            bool isCooldown = secondsRemaining > _interCooldownSeconds;
            
            if (isInterstitialReady && isCooldown)
            {
                StartRun();
                _currentAdsManager.ShowInterstitialAd(placement,  () => EndRunInter(onCompleted));
            }
            else
            {
                Debug.Log("[AdsProvider] Interstitial not started: isInterstitialReady = " + isInterstitialReady + " isCooldown = " + isCooldown);
                if (!isCooldown)
                {
                    Debug.Log($"[AdsProvider] Inter cooldown remaining = {secondsRemaining}");

                }
                onCompleted?.Invoke();
                _currentAdsManager.LoadInterstitial();
            }
        }

        private void StartRun()
        {
            HideBanner();
        }

        private void EndRunInter(Action onCompleted)
        {
            _timeFromLastInter = DateTime.Now;
            onCompleted?.Invoke();
            ShowBanner();
        }
        private void EndRunRewarded()
        {
            OnRewardedCompleted?.Invoke();
            ResetInterstitialCooldown();
            ShowBanner();
        }

        private void ResetInterstitialCooldown()
        {
            TimeSpan timeFromLastInter = DateTime.Now - _timeFromLastInter;
            double timeToInterRemaining = _interCooldownSeconds - timeFromLastInter.TotalSeconds;

            if (timeToInterRemaining < _rewardWatchedResetInterWhenCooldownLessThan)
            {
                Debug.Log($"[AdsProvider] After watching reward, remaining time to inter: {timeToInterRemaining}." +
                          $" Minimum cooldown after inter for reset cooldown: {_rewardWatchedResetInterWhenCooldownLessThan}. Reset cooldown...");
                _timeFromLastInter = DateTime.Now;
            }
            else
            {
                Debug.Log($"[AdsProvider] After watching reward, remaining time to inter: {timeToInterRemaining}." +
                          $" Minimum cooldown after inter for reset cooldown: {_rewardWatchedResetInterWhenCooldownLessThan}. Reset cooldown didn't proceed");
            }
        }

        public void HideBanner()
        {
            if (_currentAdsManager != null && _currentAdsManager.ShownBanner)
            {
                if (_bannerLoadingTokenSource != null)
                {
                    _bannerLoadingTokenSource.Cancel();
                    _bannerLoadingTokenSource.Dispose();
                    _bannerLoadingTokenSource = null;
                }
                _currentAdsManager.HideBanner();
            }
        }

        public void SetNoAdsState(bool state)
        {
            Debug.Log($"[AdsProvider] SetNoAdsState called. New value : {state}]");
            IsNoAds = state;
            DeactivateAds(state);
        }

        public void ShowBanner()
        {
            if(IsNoAds)
                return;

            if (!CanShowBanner())
                return;
            
            if (_currentAdsManager.ShownBanner == false)
            {
                _currentAdsManager.ShowBanner();
            }
        }

        private bool CanShowBanner() => true;

        private async UniTask LoadingBannerLoop()
        {
            if (_bannerLoadingTokenSource != null)
            {
                _bannerLoadingTokenSource.Cancel();
                _bannerLoadingTokenSource.Dispose();
                _bannerLoadingTokenSource = null;
            }
            _bannerLoadingTokenSource = new CancellationTokenSource();
            while (true)
            {
                if (_currentAdsManager.ShownBanner)
                    await UniTask.Yield(cancellationToken: _bannerLoadingTokenSource.Token);

                ShowBanner();

                await UniTask.WaitForSeconds(5f, cancellationToken: _bannerLoadingTokenSource.Token);
            }
        }
    }
}