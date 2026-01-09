using System;
using Cysharp.Threading.Tasks;
using GoogleMobileAds.Api;
using UnityEngine;
using WordSolitaire.ThirdParty.Ads.Data;

namespace WordSolitaire.ThirdParty.Ads
{
    public class InterstitialAdService : BaseAdService
    {
        private InterstitialAd _interstitialAd;
        private InterstitialAd _highInterstitialAd;
        private DateTime _lastShowTime;
        private int _interSecondsDelay;

        public event Action<InterstitialAd> OnInterstitialLoaded;
        public event Action<LoadAdError> OnInterstitialLoadFailed;
        public event Action OnInterstitialClosed;
        public event Action<AdValue> OnAdPaid;

        public InterstitialAdService(AdMobConfig config) : base(config)
        {
            _lastShowTime = DateTime.Now;
            _interSecondsDelay = config.DefaultInterSecondsDelay;
        }

        public void SetInterDelay(int delaySeconds)
        {
            _interSecondsDelay = delaySeconds;
        }

        public override void Load()
        {
            LoadHighInterstitial();
        }

        private void LoadDefaultInterstitial()
        {
            Debug.Log("Loading the default interstitial ad.");
        
            var adRequest = new AdRequest();
            InterstitialAd.Load(config.InterstitialAdUnitId, adRequest, OnLoadCallback);
        }

        private void LoadHighInterstitial()
        {
            if (IsAvailable())
            {
                Debug.Log("LoadHighInterstitialAd(): ad is already loaded and can be shown");
                return;
            }

            Debug.Log("Loading the high interstitial ad.");
        
            var adRequest = new AdRequest();
            InterstitialAd.Load(config.HighInterstitialAdUnitId, adRequest, OnHighLoadCallback);
        }

        public override bool IsAvailable()
        {
            return IsHighInterstitialAvailable() || IsDefaultInterstitialAvailable();
        }

        private bool IsDefaultInterstitialAvailable()
        {
            return _interstitialAd != null && _interstitialAd.CanShowAd() && !IsExpired();
        }

        private bool IsHighInterstitialAvailable()
        {
            return _highInterstitialAd != null && _highInterstitialAd.CanShowAd() && !IsExpired();
        }

        public override void Destroy()
        {
            StopExpirationCheck();
            if (_interstitialAd != null)
            {
                _interstitialAd.Destroy();
                _interstitialAd = null;
            }

            if (_highInterstitialAd != null)
            {
                _highInterstitialAd.Destroy();
                _highInterstitialAd = null;
            }
        }

        public bool CanShowNow()
        {
            if (!IsAvailable())
                return false;

            return (DateTime.Now - _lastShowTime).TotalSeconds > _interSecondsDelay;
        }

        public void Show(Action onClose = null)
        {
            if (!IsAvailable())
            {
                Debug.LogError("Interstitial ad is not ready yet.");
                Load();
                onClose?.Invoke();
                return;
            }

            if (!CanShowNow())
            {
                Debug.Log("Too soon: interstitial ad attempt.");
                onClose?.Invoke();
                return;
            }

            Debug.Log("Showing interstitial ad.");
        
            if (onClose != null)
            {
                Action previousHandler = null;
                previousHandler = () =>
                {
                    OnInterstitialClosed -= previousHandler;
                    onClose();
                };
                OnInterstitialClosed += previousHandler;
            }

            _lastShowTime = DateTime.Now;

            if (IsHighInterstitialAvailable())
            {
                ShowHighInterstitial();
            }
            else if (IsDefaultInterstitialAvailable())
            {
                ShowInterstitial();
            }
        }

        private void ShowInterstitial()
        {
            Debug.Log("Showing default interstitial ad.");
            _interstitialAd.Show();
        }

        private void ShowHighInterstitial()
        {
            Debug.Log("Showing high interstitial ad.");
            _highInterstitialAd.Show();
        }

        private void OnHighLoadCallback(InterstitialAd ad, LoadAdError error)
        {
            if (error != null || ad == null)
            {
                Debug.LogError($"[AdMobService] HighInterstitial ad failed to load: {error}");
                HandleHighInterstitialLoadFailed(error);
                return;
            }

            if (_highInterstitialAd != null)
            {
                _highInterstitialAd.Destroy();
                _highInterstitialAd = null;
            }

            _highInterstitialAd = ad;
            MarkAsLoaded();
            Debug.Log("HighInterstitial ad loaded with response : " + _highInterstitialAd.GetResponseInfo());
        
            RegisterHighInterstitialEventHandlers(_highInterstitialAd);
            OnInterstitialLoaded?.Invoke(_highInterstitialAd);
        }

        private void OnLoadCallback(InterstitialAd ad, LoadAdError error)
        {
            if (error != null || ad == null)
            {
                Debug.LogError($"[AdMobService] Interstitial ad failed to load: {error}");
                HandleDefaultInterstitialLoadFailed(error);
                return;
            }

            if (_interstitialAd != null)
            {
                _interstitialAd.Destroy();
                _interstitialAd = null;
            }

            _interstitialAd = ad;
            MarkAsLoaded();
            Debug.Log("Interstitial ad loaded with response : " + _interstitialAd.GetResponseInfo());
        
            RegisterDefaultInterstitialEventHandlers(_interstitialAd);
            OnInterstitialLoaded?.Invoke(_interstitialAd);
        }

        private void HandleDefaultInterstitialLoadFailed(LoadAdError error)
        {
            Debug.Log($"Default interstitial failed. Retrying with high interstitial after {config.RetryLoadDelay} seconds.");
            OnInterstitialLoadFailed?.Invoke(error);
            DelayCall(LoadHighInterstitial, config.RetryLoadDelay).Forget();
        }
    
        private void HandleHighInterstitialLoadFailed(LoadAdError error)
        {
            Debug.Log("High interstitial failed. Trying default interstitial immediately.");
            OnInterstitialLoadFailed?.Invoke(error);
            LoadDefaultInterstitial();
        }

        private void RegisterDefaultInterstitialEventHandlers(InterstitialAd interstitialAd)
        {
            interstitialAd.OnAdPaid += (AdValue adValue) =>
            {
                Debug.Log(String.Format("Interstitial ad paid {0} {1}.", adValue.Value, adValue.CurrencyCode));
                OnAdPaid?.Invoke(adValue);
            };

            interstitialAd.OnAdImpressionRecorded += () =>
            {
                Debug.Log("Interstitial ad recorded an impression.");
            };

            interstitialAd.OnAdClicked += () =>
            {
                Debug.Log("Interstitial ad was clicked.");
            };

            interstitialAd.OnAdFullScreenContentOpened += () =>
            {
                Debug.Log("Interstitial ad full screen content opened.");
            };

            interstitialAd.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("Interstitial Ad full screen content closed.");
                OnInterstitialClosed?.Invoke();
                Load();
            };

            interstitialAd.OnAdFullScreenContentFailed += (AdError error) =>
            {
                Debug.LogError("Interstitial ad failed to open full screen content with error : " + error);
                Load();
            };
        }
    
        private void RegisterHighInterstitialEventHandlers(InterstitialAd interstitialAd)
        {
            interstitialAd.OnAdPaid += (AdValue adValue) =>
            {
                Debug.Log(String.Format("HighInterstitial ad paid {0} {1}.", adValue.Value, adValue.CurrencyCode));
                OnAdPaid?.Invoke(adValue);
            };

            interstitialAd.OnAdImpressionRecorded += () =>
            {
                Debug.Log("HighInterstitial ad recorded an impression.");
            };

            interstitialAd.OnAdClicked += () =>
            {
                Debug.Log("HighInterstitial ad was clicked.");
            };

            interstitialAd.OnAdFullScreenContentOpened += () =>
            {
                Debug.Log("HighInterstitial ad full screen content opened.");
            };

            interstitialAd.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("HighInterstitial Ad full screen content closed.");
                OnInterstitialClosed?.Invoke();
                Load();
            };

            interstitialAd.OnAdFullScreenContentFailed += (AdError error) =>
            {
                Debug.LogError("Interstitial ad failed to open full screen content with error : " + error);
                Load();
            };
        }
    }
}

