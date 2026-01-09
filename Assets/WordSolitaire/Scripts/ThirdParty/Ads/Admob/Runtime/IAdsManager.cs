using System;
using GoogleMobileAds.Api;

namespace WordSolitaire.ThirdParty.Ads
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
        public void ShowInterstitialAd(string placement = null, System.Action OnClose = null);
        public bool IsRewardedVideoAvailable();
        public void ShowRewarded(RewardedAdPlacement sourceId, System.Action OnFinish, System.Action onRewardedVideoNotAvailable = null, System.Action OnCanceled = null);
        bool IsExtraRewardedAvailable();
        void ShowExtraRewardedAd(Action onFinish, Action onNotAvailable = null);
        void LoadExtraRewarded();
        void LoadInterstitial();
        void LoadRewarded();
        event Action<AdValue> OnAdPaid;
    }
}
