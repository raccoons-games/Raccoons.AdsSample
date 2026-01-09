using System;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace WordSolitaire.ThirdParty.Ads.Test
{
    [RequireComponent(typeof(Button))]
    public class RewardTestButton: MonoBehaviour
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
            _adsProvider.RunRewarded(RewardedAdPlacement.Default, OnRewardCompleted);
        }

        private void OnRewardCompleted()
        {
            Debug.Log("[RewardTestButton] Reward completed, gained reward");
        }
    }
}