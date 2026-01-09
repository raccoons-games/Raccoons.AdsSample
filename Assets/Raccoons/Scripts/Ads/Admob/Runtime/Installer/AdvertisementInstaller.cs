using Raccoons.Ads.Admob.Android;
using UnityEngine;
using Zenject;

namespace Raccoons.Ads.Admob.Installer
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