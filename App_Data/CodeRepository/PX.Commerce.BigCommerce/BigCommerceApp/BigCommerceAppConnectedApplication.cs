using IdentityServer4;
using IdentityServer4.Models;
using System.Collections.Generic;

namespace PX.Commerce.BigCommerce
{
    internal static class BigCommerceAppConnectedApplication
    {
        //Here until platform allows storing in DB
        internal const string BigCommerceAppClientId = "D5A7B70A-2040-2F3E-5F2D-4E3DAD993758";
        private static readonly string BigCommerceAppClientSecret = "jYJJoRp7qXC2/2Risri98Va1w5mQqKifAIR4//Uj/tc=";

        private static readonly List<string> BigCommerceAppRedirectUris = new List<string>
        {
			"https://bigcommerceconnect.acumatica.com/auth/callback",
			"https://commerceconnect.acumatica.com/bcc/auth/callback",
			"https://getpostman.com/oauth2/callback",
			"https://oauth.pstmn.io/v1/callback"
		};

        internal static Client BigCommerceAppClientForRegistration()
        {
            var bigCommerceAppClient = new Client
			{
                ClientName = "BigCommerce Public App",
                Enabled = true,
                AllowedGrantTypes = GrantTypes.Code,
                RedirectUris = BigCommerceAppRedirectUris,
                ClientSecrets = new List<Secret> { new Secret(BigCommerceAppClientSecret.Sha256()) },
                AccessTokenType = AccessTokenType.Reference,
                RequireConsent = false,
                AllowedScopes = new List<string>
                {
					"api"
                }
            };
            return bigCommerceAppClient;
        }
    }
}
