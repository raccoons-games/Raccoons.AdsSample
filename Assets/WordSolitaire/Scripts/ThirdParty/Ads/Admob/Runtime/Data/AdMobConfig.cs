using UnityEngine;

namespace WordSolitaire.ThirdParty.Ads.Data
{
    [CreateAssetMenu(fileName = "AdMobConfig", menuName = "Config/AdMob Config", order = 1)]
    public class AdMobConfig : ScriptableObject
    {
        [Header("Development Mode")]
        [SerializeField]
        private bool isDev = false;
    
        [Header("Production Ad Unit IDs")]
        [SerializeField] 
        private string bannerAdUnitId;
        [SerializeField]
        private string interstitialAdUnitId;
        [SerializeField]
        private string highInterstitialAdUnitId;
        [SerializeField]
        private string rewardedAdUnitId;
        [SerializeField]
        private string highRewardAdUnitId;
        
        [Header("Production Ad Unit IDs - iOS")]
        [SerializeField]
        private string iosBannerAdUnitId;
        [SerializeField]
        private string iosInterstitialAdUnitId;
        [SerializeField]
        private string iosRewardedAdUnitId;
        [SerializeField]
        private string iosHighRewardAdUnitId;
        [SerializeField]
        private string iosHighInterstitialAdUnitId;
    
        [Header("Test Ad Unit IDs")]
        [SerializeField] 
        private string testBannerAdUnitId;
        [SerializeField]
        private string testInterstitialAdUnitId;
        [SerializeField]
        private string testHighInterstitialAdUnitId;
        [SerializeField]
        private string testRewardedAdUnitId;
        [SerializeField]
        private string testHighRewardAdUnitId;

        [Header("Settings")]
        [SerializeField]
        private float retryLoadDelay = 30f;
        [SerializeField]
        private float advertisementExpiredDurationSeconds = 2700f;
        [SerializeField]
        private int defaultInterSecondsDelay = 60;

        [SerializeField]
        private float expireCheckDelay = 1f;

    

        public bool IsDev => isDev;
        public string BannerAdUnitId => isDev ? testBannerAdUnitId : bannerAdUnitId;
        public string InterstitialAdUnitId => isDev ? testInterstitialAdUnitId : interstitialAdUnitId;
        public string HighInterstitialAdUnitId => isDev ? testHighInterstitialAdUnitId : highInterstitialAdUnitId;
        public string RewardedAdUnitId => isDev ? testRewardedAdUnitId : rewardedAdUnitId;
        public string HighRewardAdUnitId => isDev ? testHighRewardAdUnitId : highRewardAdUnitId;
        public float RetryLoadDelay => retryLoadDelay;
        public float AdvertisementExpiredDurationSeconds => advertisementExpiredDurationSeconds;
        public int DefaultInterSecondsDelay => defaultInterSecondsDelay;
        public float ExpireCheckDelay => expireCheckDelay;
    }
}