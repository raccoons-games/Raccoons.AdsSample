using UnityEngine;
using Zenject;

namespace WordSolitaire.ThirdParty.Ads
{
    public class AdvertisementInstaller: MonoInstaller
    {
        [SerializeField]
        private AdMobManager admobManager;
        
        public override void InstallBindings()
        {
            Container.BindInterfacesAndSelfTo<AdMobManager>().FromInstance(admobManager).AsSingle();
            
            Container.BindInterfacesAndSelfTo<AdsProvider>().AsSingle();
        }
    }
}