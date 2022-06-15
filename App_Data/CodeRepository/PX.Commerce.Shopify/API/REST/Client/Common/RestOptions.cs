using System;

namespace PX.Commerce.Shopify.API.REST
{
    public interface IRestOptions
    {
        string BaseUri { get; set; }
        string XAuthClient { get; set; }
        string XAuthTocken { get; set; }
        string AccessToekn { get; set; }
        string SharedSecret { get; set; }
		int ApiCallLimit { get; set; }
	}

    public class RestOptions : IRestOptions
    {
        public string BaseUri { get; set; }
        public string XAuthClient { get; set; }
        public string XAuthTocken { get; set; }
        public string AccessToekn { get; set; }
        public string SharedSecret { get; set; }
		public int ApiCallLimit { get; set; }
    }
}
