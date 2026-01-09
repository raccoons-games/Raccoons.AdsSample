using System;
using GoogleMobileAds.Api;
using Raccoons.Ads.Admob.Data;
using UnityEngine;

namespace Raccoons.Ads.Admob.AdTypes
{
    public class BannerAdService : BaseAdService
    {
        private BannerView _bannerView;
        private bool _isShown;
        private bool _isLoading;

        public event Action OnBannerLoaded;
        public event Action<LoadAdError> OnBannerLoadFailed;
        public event Action<AdValue> OnAdPaid;

        public bool IsShown => _isShown;

        public BannerAdService(AdMobConfig config) : base(config) { }

        public override void Load()
        {
            if (_bannerView == null)
            {
                CreateBannerView();
            }
        
            if (_isLoading)
            {
                Debug.Log("Banner ad is already loading, skipping...");
                return;
            }

            var adRequest = new AdRequest();
            Debug.Log("Loading banner ad.");
            _isLoading = true;
            _bannerView.LoadAd(adRequest);
        
        }

        public override bool IsAvailable()
        {
            return _bannerView != null;
        }

        public override void Destroy()
        {
            if (_bannerView != null)
            {
                Debug.Log("Destroying banner view.");
                _bannerView.Hide();
                _bannerView.Destroy();
                _bannerView = null;
                _isShown = false;
            }
        }

        public void Show()
        {
            Debug.Log($"[BannerAdService] Show called. _bannerView != null: {_bannerView != null}, _isShown: {_isShown}, _isLoading: {_isLoading}");

            if (_isLoading)
            {
                Debug.Log("[BannerAdService] Skipping show, banner ad is loading");
                return;       
            }
        
            if (_bannerView != null)
            {
                Debug.Log($"[BannerAdService] Showing banner ad. Was banner shown before : {_isShown}");
                _bannerView.Show();
                _isShown = true;
            }
            else
            {
                Debug.Log("[BannerAdService] Creating new banner view and loading ad");
                _isShown = true;
                CreateBannerView();
                Load();
            }
        }

        public void Hide()
        {
            _bannerView?.Hide();
            _isShown = false;
            Debug.Log("[BannerAdService] Hide banner. Is banner shown : " + _isShown + "");
        }

        private void CreateBannerView()
        {
            Debug.Log("Creating banner view");

            if (_bannerView != null)
            {
                Destroy();
            }

            _bannerView = new BannerView(
                config.BannerAdUnitId, 
                AdSize.GetCurrentOrientationAnchoredAdaptiveBannerAdSizeWithWidth(AdSize.FullWidth), 
                AdPosition.Bottom
            );
        
            RegisterEventHandlers();
        }

        private void RegisterEventHandlers()
        {
            _bannerView.OnBannerAdLoaded += () =>
            {
                Debug.Log("Banner view loaded an ad with response : " + _bannerView.GetResponseInfo());
                _isLoading = false;
                _isShown = true;
                OnBannerLoaded?.Invoke();
            };

            _bannerView.OnBannerAdLoadFailed += (LoadAdError error) =>
            {
                _isLoading = false;
                Debug.LogError("Banner view failed to load an ad with error : " + error.GetMessage());
                OnBannerLoadFailed?.Invoke(error);
            };

            _bannerView.OnAdPaid += (AdValue adValue) =>
            {
                Debug.Log(String.Format("Banner view paid {0} {1}.", adValue.Value, adValue.CurrencyCode));
                OnAdPaid?.Invoke(adValue);
            };

            _bannerView.OnAdImpressionRecorded += () =>
            {
                Debug.Log("Banner view recorded an impression.");
            };

            _bannerView.OnAdClicked += () =>
            {
                Debug.Log("Banner view was clicked.");
            };

            _bannerView.OnAdFullScreenContentOpened += () =>
            {
                Debug.Log("Banner view full screen content opened.");
            };

            _bannerView.OnAdFullScreenContentClosed += () =>
            {
                Debug.Log("Banner view full screen content closed.");
            };
        }
    }
}

