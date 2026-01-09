using System;
using GoogleMobileAds.Ump.Api;

namespace WordSolitaire.ThirdParty.Ads
{
    public static class Consent
    {
        public static bool CanRequestAds => ConsentInformation.CanRequestAds();

        public static void ConsentData(Action<string> onComplete)
        {
            var requestParameters = new ConsentRequestParameters()
            {
                ConsentDebugSettings = new ConsentDebugSettings()
                {
                    DebugGeography = DebugGeography.Disabled
                }
            };
            ConsentInformation.Update(requestParameters, (FormError updateError) =>
            {
                 if (updateError != null)
                     onComplete(updateError.Message);

                 if (CanRequestAds)
                 {
                     onComplete(null);
                     return;
                 }

                 ConsentForm.LoadAndShowConsentFormIfRequired(showError =>
                 {
                     if (showError != null)
                     {
                         if (onComplete != null)
                         {
                             onComplete(showError.Message);
                         }
                     }
                     else if (onComplete != null)
                     {
                         onComplete(null);
                     }
                 });
            });
        }
        
        public static void ResetConsent()
        {
            ConsentInformation.Reset();
        }
    }
}