using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Raccoons.Ads.Admob.Test
{
    [RequireComponent(typeof(Button))]
    public class InterstitialTestButton: MonoBehaviour
    {
        [SerializeField] private Button button;
        private IAdsProvider _adsProvider;
        
        [Inject]
        private void Construct(IAdsProvider adsProvider)
        {
            _adsProvider = adsProvider;
        }

        private void Awake()
        {
            button.onClick.AddListener(Button_OnClicked);
        }

        private void OnDestroy()
        {
            button.onClick.RemoveListener(Button_OnClicked);
        }

        private void Button_OnClicked()
        {
            _adsProvider.RunInterstitial("test_placement",OnInterstitialCompleted);
        }

        private void OnInterstitialCompleted()
        {
            Debug.Log("[InterstitialTestButton] Interstitial completed");
        }
    }
}