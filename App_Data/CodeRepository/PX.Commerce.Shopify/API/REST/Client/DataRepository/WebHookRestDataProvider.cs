using PX.Commerce.Core.Model;
using System;
using System.Collections.Generic;

namespace PX.Commerce.Shopify.API.REST
{
    public class WebHookRestDataProvider : RestDataProviderBase, IParentRestDataProvider<WebHookData>
    {
        protected override string GetListUrl { get; } = "/webhooks.json";
        protected override string GetSingleUrl { get; } = "/webhooks/{id}.json";
		protected override string GetSearchUrl => throw new NotImplementedException();

		public WebHookRestDataProvider(IShopifyRestClient restClient) : base()
		{
            ShopifyRestClient = restClient;
		}

        #region IParentDataRestClient
        public virtual WebHookData Create(WebHookData entity)
        {
            var newwebhook = base.Create<WebHookData, WebHookResponse>(entity);
            return newwebhook;
        }

		public virtual WebHookData Update(WebHookData entity, string id)
        {
			var segments = MakeUrlSegments(id);
			return base.Update<WebHookData, WebHookResponse>(entity, segments);
		}

		public virtual bool Delete(string id)
        {
            var segments = MakeUrlSegments(id);
            return base.Delete(segments);
        }

		public virtual IEnumerable<WebHookData> GetAll(IFilter filter = null)
        {
            var allWebHooks = base.GetAll<WebHookData, WebHooksResponse>(filter);
            return allWebHooks;
        }

		public virtual WebHookData GetByID(string id)
        {
            var segments = MakeUrlSegments(id);
            var webhook = GetByID<WebHookData, WebHookResponse>(segments);
            return webhook;
        }
		#endregion
	}
}
