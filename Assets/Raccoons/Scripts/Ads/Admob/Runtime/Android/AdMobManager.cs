using System;
using Cysharp.Threading.Tasks;
using GoogleMobileAds.Api;
using GoogleMobileAds.Api.Mediation.UnityAds;
using GoogleMobileAds.Ump.Api;
using Raccoons.Ads.Admob.AdTypes;
using Raccoons.Ads.Admob.Data;
using UnityEngine;

namespace Raccoons.Ads.Admob.Android
{
    public class AdMobManager : MonoBehaviour, IAdsManager
    {
        [SerializeField] 
        private AdMobConfig config;

        private BannerAdService _bannerService;
        private InterstitialAdService _interstitialService;
        private RewardedAdService _rewardedService;

        public bool AdEnabled { get; private set; } = true;
    
        public event Action OnRewardedVideoAvailableToShow;
        public event Action OnRewardedShown;
        public event Action OnRewardedClose;
        public event Action<AdValue> OnAdPaid;
    
        public bool IsInitialized { get; private set; } = false;
        public string Name { get; } = "Admob";
    
        public bool ShownBanner => _bannerService?.IsShown ?? false;

        public void Initialize(Action initializedCallback)
        {
            Initialize();
            initializedCallback?.Invoke();
        }
    
        public void Initialize()
        {
            Debug.Log("START INIT admob");
            InitializeGoogleAdsConsent();
        }

        private void InitializeGoogleAdsConsent()
        {
             Consent.ConsentData(error =>
             {
                 if (error != null)
                 {
                     Debug.Log($"[AdMobManager] Failed initialize ConsentData: {error}");
                 }
                 else
                 {
                     Debug.Log("[AdmobManager] Google Ads consent updated: " + ConsentInformation.ConsentStatus);
                 }

                 if (Consent.CanRequestAds)
                 {
                     InitDelayed().Forget();
                 }
                 
                 bool gdprConsent =
                     ConsentInformation.ConsentStatus == ConsentStatus.Obtained ||
                     ConsentInformation.ConsentStatus == ConsentStatus.NotRequired;
                 UnityAds.SetConsentMetaData("gdpr.consent", gdprConsent);
             });
        }
    
        private void InitSdk()
        {
            Debug.Log("*** ADMOB AD MANAGER InitSdk ***");

            if (IsInitialized)
            {
                Debug.Log("AdMob SDK already initialized");
                return;       
            }
        
            if (config == null)
            {
                Debug.LogError("AdMobConfig is not assigned!");
                return;
            }

            _bannerService = new BannerAdService(config);
            _interstitialService = new InterstitialAdService(config);
            _rewardedService = new RewardedAdService(config);
            IsInitialized = true;

            SubscribeToServiceEvents();

            MobileAds.RaiseAdEventsOnUnityMainThread = true;
        
            MobileAds.Initialize((InitializationStatus initStatus) =>
            {
                foreach (var adapter in initStatus.getAdapterStatusMap())
                {
                    string className = adapter.Key;
                    AdapterStatus status = adapter.Value;
                
                    switch (status.InitializationState)
                    {
                        case AdapterState.NotReady:
                            Debug.Log($"Adapter: {className} not ready.");
                            break;
                        case AdapterState.Ready:
                            Debug.Log($"Adapter: {className} is initialized.");
                            break;
                    }
                }
            
                _interstitialService.Load();
                _rewardedService.LoadRewardedAd();
                _rewardedService.LoadExtraRewardedAd();

                int interDelay = config.DefaultInterSecondsDelay;
                _interstitialService.SetInterDelay(interDelay);

                Debug.Log("AdMob SDK Initialized");
            });
        }

        private async UniTaskVoid InitDelayed()
        {
            await UniTask.Delay(500);
            InitSdk();
        }

        private void SubscribeToServiceEvents()
        {
            _rewardedService.OnRewardedVideoAvailableToShow += HandleRewardedAvailable;
            _rewardedService.OnRewardedShown += HandleRewardedShown;
            _rewardedService.OnRewardedClosed += HandleRewardedClosed;
            _rewardedService.OnAdPaid += OnAdPaid;
            _interstitialService.OnAdPaid += OnAdPaid;
            _bannerService.OnAdPaid += OnAdPaid;
        }

        private void UnsubscribeFromServiceEvents()
        {
            if (_rewardedService != null)
            {
                _rewardedService.OnRewardedVideoAvailableToShow -= HandleRewardedAvailable;
                _rewardedService.OnRewardedShown -= HandleRewardedShown;
                _rewardedService.OnRewardedClosed -= HandleRewardedClosed;
                _rewardedService.OnAdPaid -= OnAdPaid;
                _interstitialService.OnAdPaid -= OnAdPaid;
                _bannerService.OnAdPaid -= OnAdPaid;
                
            }
        }

        private void HandleRewardedAvailable()
        {
            OnRewardedVideoAvailableToShow?.Invoke();
        }

        private void HandleRewardedShown()
        {
            OnRewardedShown?.Invoke();
        }

        private void HandleRewardedClosed()
        {
            OnRewardedClose?.Invoke();
        }

        private void OnDestroy()
        {
            UnsubscribeFromServiceEvents();
        
            _bannerService?.Destroy();
            _interstitialService?.Destroy();
            _rewardedService?.Destroy();
        }

        public void DeactivateAds(bool deactivate)
        {
            AdEnabled = !deactivate;
      
            if (deactivate)
            {
                HideBanner();
            }
            else
            {
                ShowBanner();
            }
        }

        #region BANNER

        public void ShowBanner()
        {
            if (AdEnabled && (!_bannerService?.IsShown ?? false))
            {
                _bannerService.Show();
            }
        }
        
        public void HideBanner()
        {
            _bannerService?.Hide();
        }
    
        public void DestroyBannerView()
        {
            _bannerService?.Destroy();
        }

        #endregion BANNER

        #region INTERSTITIAL

        public void ShowInterstitialAd(string placement = null, Action OnClose = null)
        {
            if (!AdEnabled)
            {
                OnClose?.Invoke();
                return;
            }

            _interstitialService.Show(OnClose);
        }
    
        public void LoadInterstitial()
        {
            _interstitialService.Load();   
        }

        public bool IsInterstitialAvailable()
        {
            return _interstitialService.IsAvailable();
        }
    
        #endregion INTERSTITIAL

        #region REWARDED

        public bool IsRewardedVideoAvailable()
        {
            if(_rewardedService == null) 
                return false;

            return _rewardedService.IsAvailable();
        }

        public bool IsExtraRewardedAvailable()
        {
            if(_rewardedService == null) 
                return false;
        
            return _rewardedService.IsExtraRewardedAvailable();
        }

        public void ShowRewarded(RewardedAdPlacement sourceId, Action onFinish, Action onRewardedVideoNotAvailable = null, Action onCanceled = null)
        {
            _rewardedService.ShowRewarded(sourceId, onFinish, onRewardedVideoNotAvailable);
        }

        public void ShowExtraRewardedAd(Action onFinish, Action onNotAvailable = null)
        {
            _rewardedService.ShowExtraRewardedAd(onFinish, onNotAvailable);
        }
    
        public void LoadExtraRewarded()
        {
            _rewardedService?.LoadExtraRewardedAd();
        }
        public void LoadRewarded()
        {
            _rewardedService.LoadRewardedAd();   
        }

        #endregion REWARDED
    }
}
