using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Autofac;
using IdentityModel;
using IdentityModel.Client;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using PX.CloudServices.Auth;
using PX.CloudServices.Discovery;
using PX.Data;
using PX.OAuthClient.DAC;
using PX.OAuthClient.Handlers;
using PX.OAuthClient.Processors;
using PX.Objects.Localizations.GB.HMRC.Model;
using Module = Autofac.Module;

namespace PX.Objects.Localizations.GB.HMRC
{
	public class MTDCloudApplicationProcessor : IExternalApplicationProcessor
	{
		const string serviceKey = "hmrc-making-tax-digital";
		private readonly IDiscoveryService _discoveryService;
		private readonly ITokenService _tokenService;
		private readonly IHttpClientFactory<MTDCloudApplicationProcessor> _httpClientFactory;


		public MTDCloudApplicationProcessor(IDiscoveryService discoveryService, ITokenService tokenService,
			IHttpClientFactory<MTDCloudApplicationProcessor> httpClientFactory)
		{
			_discoveryService = discoveryService;
			_tokenService = tokenService;
			_httpClientFactory = httpClientFactory;
		}

		public void SignIn(OAuthApplication oAuthApplication, ref OAuthToken token)
		{
			var redirectUri = AuthenticationHandler.ReturnUrl;
			object key = Guid.NewGuid();

			PXLongOperation.StartOperation(key, () =>
				{
					Uri url;
					try
					{
						url = GetServiceUrlAsync(serviceKey, oAuthApplication.ApplicationID.ToString(), redirectUri).Result;
					}
					catch (Exception e)
					{
						PXLongOperation.SetCustomInfo(e.ToString(), "urlerror");
						throw;
					}

					PXLongOperation.SetCustomInfo(url.ToString(), "url");
				}
			);
			PXLongOperation.WaitCompletion(key);

			string redirectUrl = PXLongOperation.GetCustomInfo(key, "url") as string;

			if (!string.IsNullOrEmpty(redirectUrl))
			{
				throw new PXRedirectToUrlException(redirectUrl, PXBaseRedirectException.WindowMode.InlineWindow,
				"Authenticate");
			}
			string UrlError = PXLongOperation.GetCustomInfo(key, "urlerror") as string;
			PXTrace.WriteError(UrlError);
			throw new PXException(Messages.UnretrievableRedirectURL);
		}

		public void ProcessAuthorizationCode(string code, OAuthApplication application, OAuthToken token)
		{
			var redirectUri = AuthenticationHandler.ReturnUrl;
			object key = Guid.NewGuid();
			PXLongOperation.StartOperation(key, () =>
				{
					ProcessAuthorizationCodeImpl(code, application, token, redirectUri).Wait();
				}
			);
			PXLongOperation.WaitCompletion(key);
		}

		public void RefreshAccessToken(OAuthToken token, OAuthApplication oAuthApplication)
		{
			object key = Guid.NewGuid();
			PXLongOperation.StartOperation(key, () =>
				{
					RefreshAccessTokenImpl(token, oAuthApplication).Wait();
				}
			);
			PXLongOperation.WaitCompletion(key);
		}

		private async Task ProcessAuthorizationCodeImpl(string code, OAuthApplication application, OAuthToken token,
			string redirectUri)
		{
			var baseUrl = await _discoveryService.GetServiceUrlAsync(serviceKey);
			var authorization = await _tokenService.GetAuthorizationHeader();
			using (var httpClient = _httpClientFactory.CreateClient())
			{
				var tokenResponse = await httpClient.RequestAuthorizationCodeTokenAsync(
					new AuthorizationCodeTokenRequest
					{
						Address = new Uri(baseUrl, "oauth2/token").ToString(),
						Code = code,
						RedirectUri = redirectUri,
						Headers = { Authorization = authorization },
					});
				FillTokenFromTokenResponse(tokenResponse, token);
			}
		}

		private async Task RefreshAccessTokenImpl(OAuthToken token, OAuthApplication oAuthApplication)
		{
			var baseUrl = await _discoveryService.GetServiceUrlAsync(serviceKey);
			var authorization = await _tokenService.GetAuthorizationHeader();
			using (var httpClient = _httpClientFactory.CreateClient())
			{
				var refreshToken = token.RefreshToken;
				var tokenResult = await httpClient.RequestRefreshTokenAsync(new RefreshTokenRequest
				{
					Address = new Uri(baseUrl, "oauth2/token").ToString(),
					RefreshToken = refreshToken,
					Headers = { Authorization = authorization },
				});
				FillTokenFromTokenResponse(tokenResult, token);
			}
		}

		public Dictionary<string, string> GetAPIUrls()
		{
			Guid key = Guid.NewGuid();
			PXLongOperation.StartOperation(key, () =>
			{
				try
				{
					var urls = GetAPIUrlsImpl().GetAwaiter().GetResult();
					PXLongOperation.SetCustomInfo(urls, $"Urls");
				}
				catch (Exception e)
				{
					PXLongOperation.SetCustomInfo(e, $"Error");
				}
			}
			);
			PXLongOperation.WaitCompletion(key);

			object customInfo = PXLongOperation.GetCustomInfo(key, $"Urls");
			if (customInfo is Dictionary<string, string>)
			{
				return customInfo as Dictionary<string, string>;
			}

			object error = PXLongOperation.GetCustomInfo(key, $"Error");
			if (error is Exception)
			{
				PXTrace.WriteError((Exception)error);
			}

			return new Dictionary<string, string>();
		}

		private async Task<Dictionary<string, string>> GetAPIUrlsImpl()
		{
			var baseUrl = await _discoveryService.GetServiceUrlAsync(serviceKey);
			var authorization = await _tokenService.GetAuthorizationHeader();
			Dictionary<string, string> result = new Dictionary<string, string>();
			using (var httpClient = _httpClientFactory.CreateClient())
			{
				httpClient.DefaultRequestHeaders.Authorization = authorization;
				var response = await httpClient.GetAsync(baseUrl);
				if (response.IsSuccessStatusCode
					&& response.Content.Headers.ContentType.ToString().ToLower().Contains("application/json"))
				{
					string urlsStr = await response.Content.ReadAsStringAsync();
					try
					{
						result = JsonConvert.DeserializeObject<Dictionary<string, string>>(urlsStr);
					}
					catch (JsonException e)
					{
						PXTrace.WriteError(e);
					}
				}
				else if (!response.Content.Headers.ContentType.ToString().ToLower().Contains("application/json"))
				{
					throw new PXException(Messages.NonJSONContentType);
				}
			}
			return result;
		}


		public string GetApplicationRestrictedToken(OAuthApplication oAuthApplication)
		{
			object key = Guid.NewGuid();

			PXLongOperation.StartOperation(key, () =>
				{
					var token = GetApplicationRestrictedTokenImpl().Result;
					PXLongOperation.SetCustomInfo(token, "Token");
				}
			);
			PXLongOperation.WaitCompletion(key);

			object customInfo = PXLongOperation.GetCustomInfo(key, "Token");
			if (customInfo is string)
			{
				return customInfo as string;
			}

			throw new PXException(Messages.UnretrievableToken);
		}

		private async Task<string> GetApplicationRestrictedTokenImpl()
		{
			var baseUrl = await _discoveryService.GetServiceUrlAsync(serviceKey);
			var authorization = await _tokenService.GetAuthorizationHeader();
			string result = String.Empty;
			using (var httpClient = _httpClientFactory.CreateClient())
			{
				var tokenResult = await httpClient.RequestClientCredentialsTokenAsync(new ClientCredentialsTokenRequest
				{
					Address = new Uri(baseUrl, "oauth2/token").ToString(),
					Headers = { Authorization = authorization },
				}
				);
				if (tokenResult.IsError) throw new PXException(tokenResult.Error);
				result = tokenResult.AccessToken;
			}
			return result;
		}

		private void FillTokenFromTokenResponse(TokenResponse tokenResponse, OAuthToken token)
		{
			token.AccessToken = tokenResponse.AccessToken;
			token.RefreshToken = tokenResponse.RefreshToken;
			token.UtcExpiredOn = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
			token.Bearer = string.Empty;
		}

		public string ResourceHtml(OAuthToken token, OAuthResource resource, OAuthApplication application)
		{
			return string.Empty;
		}

		public async Task<IEnumerable<Resource>> GetResources(OAuthToken token, OAuthApplication application)
		{
			return await Task.FromResult(Enumerable.Empty<Resource>());
		}

		public bool IsSignedIn(OAuthApplication application, OAuthToken Token)
		{
			return !string.IsNullOrEmpty(Token?.AccessToken);
		}

		public const string Type = "RCMTD";
		public string TypeCode => Type;
		public string TypeName => "HMRC Making Tax Digital";
		public bool HasRefreshToken => true;
		public string SignInFailedMessage => "Authentication failed. Sign in to the selected application.";

		private const string Scope = "read:vat write:vat";

		private async Task<Uri> GetServiceUrlAsync(string serviceName, string state, string redirectUri)
		{
			using (var httpClient = _httpClientFactory.CreateClient())
			{
				var baseUrl = await _discoveryService.GetServiceUrlAsync(serviceName);
				var authorization = await _tokenService.GetAuthorizationHeader();

				var serviceResponse = await httpClient.SendAsync(new HttpRequestMessage
				{
					Method = HttpMethod.Get,
					RequestUri = new Uri(new RequestUrl(new Uri(baseUrl, "oauth2/authorize").ToString())
						.CreateAuthorizeUrl(
							clientId: null, //doesn't matter
							responseType: OidcConstants.ResponseTypes.Code,
							scope: Scope,
							redirectUri: redirectUri,
							state: state
						)),
					Headers = { Authorization = authorization },
				});

				if (serviceResponse.StatusCode != HttpStatusCode.Redirect)
				{
					var response = await serviceResponse.Content.ReadAsStringAsync();

					throw new InvalidOperationException($"Unexpected status code {serviceResponse.StatusCode}, Content: {response}");
				}

				return new Uri(serviceResponse.Headers.Location.ToString());
			}
		}
	}

	internal class ServiceConfiguration : IConfigureServices
	{
		public void ConfigureServices(IServiceCollection services)
		{
			services
				.AddHttpClientFactory<MTDCloudApplicationProcessor>()
				.ConfigurePrimaryHttpMessageHandlerAsNoAutoRedirect();

			services
				.AddHttpClientFactory<VATMaint>();
		}
	}

	public class RegisterModule : Module
	{
		protected override void Load(ContainerBuilder builder)
		{
			builder.BindFromConfiguration<MtdOptions>("hmrc-mtd");
		}
	}
}
