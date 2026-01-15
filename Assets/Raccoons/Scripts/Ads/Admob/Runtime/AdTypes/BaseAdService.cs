using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Raccoons.Ads.Admob.Data;
using UnityEngine;

namespace Raccoons.Ads.Admob.AdTypes
{
    public abstract class BaseAdService
    {
        protected readonly AdMobConfig config;
        protected DateTime lastLoadTime;
        
        public bool IsLoading { get; protected set; }
    
        private CancellationTokenSource _expirationCheckCancellationTokenSource;

        public bool IsInitialized { get; protected set; }

        protected BaseAdService(AdMobConfig config)
        {
            this.config = config;
            lastLoadTime = DateTime.MinValue;
        }

        public abstract void Load();
        public abstract bool IsAvailable();
        public abstract void Destroy();

        protected void MarkAsLoaded()
        {
            lastLoadTime = DateTime.Now;
            IsLoading = false;
            StartExpirationCheck();
        }

        protected bool IsExpired()
        {
            if (lastLoadTime == DateTime.MinValue)
                return false;
            
            double secondsSinceLoad = (DateTime.Now - lastLoadTime).TotalSeconds;
            return secondsSinceLoad >= config.AdvertisementExpiredDurationSeconds;
        }

        protected void StartExpirationCheck()
        {
            StopExpirationCheck();
            _expirationCheckCancellationTokenSource = new CancellationTokenSource();
            CheckExpirationRoutine().Forget();
        }

        protected void StopExpirationCheck()
        {
            if(_expirationCheckCancellationTokenSource != null)
                _expirationCheckCancellationTokenSource.Cancel();
        }

        private async UniTask CheckExpirationRoutine()
        {
            while (true)
            {
                await UniTask.WaitForSeconds(config.ExpireCheckDelay, cancellationToken: _expirationCheckCancellationTokenSource.Token);
            
                if (IsExpired())
                {
                    Debug.Log($"{GetType().Name}: Ad expired, reloading...");
                    StopExpirationCheck();
                    Destroy();
                    Load();
                    return;
                }
            }
        }

        protected async UniTask DelayCall(Action call, float delay)
        {
            await UniTask.WaitForSeconds(delay, true);
            call.Invoke();
        }

        protected virtual bool CanLoad()
        {
            return !IsLoading;
        }
    }
}

