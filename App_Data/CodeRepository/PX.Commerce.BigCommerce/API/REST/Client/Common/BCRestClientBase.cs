using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using PX.Commerce.Core;
using PX.Commerce.Core.Model;
using PX.Common;
using PX.Data;
using RestSharp;
using RestSharp.Deserializers;
using RestSharp.Serializers;
using Serilog.Context;

namespace PX.Commerce.BigCommerce.API.REST
{
    public abstract class BCRestClientBase : RestClient
	{
		protected ISerializer _serializer;
		protected IDeserializer _deserializer;
		public Serilog.ILogger Logger { get; set; } = null;
        protected readonly int commerceRetryCount = WebConfig.GetInt(BCConstants.COMMERCE_RETRY_COUNT, 3);
        protected BCRestClientBase(IDeserializer deserializer, ISerializer serializer, IRestOptions options, Serilog.ILogger logger)
        {
			_serializer = serializer;
			_deserializer = deserializer;
			AddHandler("application/json", deserializer);
            AddHandler("text/json", deserializer);
            AddHandler("text/x-json", deserializer);
            Authenticator = new Autentificator(options.XAuthClient, options.XAuthTocken);
            try
            {
                BaseUrl = new Uri(options.BaseUri);
            }
            catch (UriFormatException e)
            {
                throw new UriFormatException("Invalid URL: The format of the URL could not be determined.", e);
			}
			Logger = logger;
		}

        public override IRestResponse<T> Execute<T>(IRestRequest request)
        {
            int retryCount = 0;
            while (true)
            {
                var response = base.Execute<T>(request);
                if (response.StatusCode != default(HttpStatusCode))
                    return response;
                else if (retryCount < commerceRetryCount)
                {
                    this.Logger?.ForContext("Scope", new BCLogTypeScope(GetType()))
                        .Error("{CommerceCaption}: Operation '{OperationType}' for { failed, RetryCount {RetryCount}, Exception {ExceptionMessage}",
                        BCCaptions.CommerceLogCaption, request.Method, retryCount, response.ErrorException.ToString());

                    retryCount++;
                    Thread.Sleep(1000 * retryCount);
                }
				else throw new PXException(BCMessages.RetryLimitIsExceeded, response.ErrorException);
            }
        }
        public override IRestResponse Execute(IRestRequest request)
        {
            int retryCount = 0;
            while (true)
            {
                var response = base.Execute(request);
                if (response.StatusCode != default(HttpStatusCode))
                    return response;
                else if (retryCount < commerceRetryCount)
                {
                    this.Logger?.ForContext("Scope", new BCLogTypeScope(GetType()))
                        .Error("{CommerceCaption}: Operation '{OperationType}' for { failed, RetryCount {RetryCount}, Exception {ExceptionMessage}",
                        BCCaptions.CommerceLogCaption, request.Method, retryCount, response.ErrorException.ToString());

                    retryCount++;
                    Thread.Sleep(1000 * retryCount);
                }
                else throw new PXException(BCMessages.RetryLimitIsExceeded, response.ErrorException);
            }
        }

        public RestRequest MakeRequest(string url, Dictionary<string, string> urlSegments = null)
        {
            var request = new RestRequest(url) { JsonSerializer = _serializer, RequestFormat = DataFormat.Json };

            if (urlSegments != null)
            {
                foreach (var urlSegment in urlSegments)
                {
                    request.AddUrlSegment(urlSegment.Key, urlSegment.Value);
                }
            }

            return request;
        }

        protected void LogError(Uri baseUrl, IRestRequest request, IRestResponse response)
        {
            //Get the values of the parameters passed to the API
            var parameters = string.Join(", ", request.Parameters.Select(x => x.Name.ToString() + "=" + (x.Value ?? "NULL")).ToArray());

            //Set up the information message with the URL, the status code, and the parameters.
            var info = "Request to " + baseUrl.AbsoluteUri + request.Resource + " failed with status code " + response.StatusCode + ", parameters: " + parameters;
            var description = "Response content: " + response.Content;

            //Acquire the actual exception
            var ex = (response.ErrorException?.Message) ?? info;

			//Log the exception and info message
			Logger.ForContext("Scope", new BCLogTypeScope(GetType()))
				.ForContext("Exception", response.ErrorException?.Message)
				.Error("{CommerceCaption}: {ResponseError}, Status Code: {StatusCode}", BCCaptions.CommerceLogCaption, description, response.StatusCode);
        }
    }
}