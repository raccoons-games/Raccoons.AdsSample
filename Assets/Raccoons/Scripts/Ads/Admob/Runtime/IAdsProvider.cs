using System;
using Raccoons.Ads.Admob.AdTypes;
using Zenject;

namespace Raccoons.Ads.Admob
{
    public interface IAdsProvider: IInitializable
    {
        public event Action OnRewardedLoaded;
        bool IsNoAds { get; }
        bool IsRewardedReady();
        bool IsInterstitialReady();
        void RunRewarded(RewardedAdPlacement sourceID, Action onCompleted = null);
        void RunInterstitial(string placement, Action onCompleted = null);
        void Initialize();
        void ShowBanner();
        void HideBanner();
        void SetNoAdsState(bool b);
        void LevelInit();
    }
}