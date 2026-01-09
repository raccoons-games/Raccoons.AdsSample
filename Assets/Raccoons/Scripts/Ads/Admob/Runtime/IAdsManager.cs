using System;
using GoogleMobileAds.Api;
using Raccoons.Ads.Admob.AdTypes;

namespace Raccoons.Ads.Admob
{
    public interface IAdsManager 
    {
        public bool ShownBanner { get; }
        public event Action OnRewardedVideoAvailableToShow;
        public event Action OnRewardedShown;
        public event Action OnRewardedClose;
        public void Initialize(Action initializedCallback);
        public void ShowBanner();
        public void HideBanner();
        public bool IsInterstitialAvailable();
        public void ShowInterstitialAd(string placement = null, Action OnClose = null);
        public bool IsRewardedVideoAvailable();
        public void ShowRewarded(RewardedAdPlacement sourceId, Action OnFinish,
            Action onRewardedVideoNotAvailable = null, Action OnCanceled = null);
        bool IsExtraRewardedAvailable();
        void ShowExtraRewardedAd(Action onFinish, Action onNotAvailable = null);
        void LoadExtraRewarded();
        void LoadInterstitial();
        void LoadRewarded();
        event Action<AdValue> OnAdPaid;
    }
}
