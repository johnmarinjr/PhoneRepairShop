using PX.Commerce.Core.Model;
using System;
using System.Collections.Generic;

namespace PX.Commerce.BigCommerce.API.REST
{
    public class WebHookRestDataProvider : RestDataProviderV2, IParentRestDataProvider<WebHookData>
    {
        protected override string GetListUrl { get; } = "v2/hooks";
        protected override string GetSingleUrl { get; } = "v2/hooks/{id}";

        public WebHookRestDataProvider(IBigCommerceRestClient restClient) : base()
		{
            _restClient = restClient;
		}

        #region IParentDataRestClient
        public virtual WebHookData Create(WebHookData webhook)
        {
            var newwebhook = base.Create(webhook);
            return newwebhook;
        }

		public virtual WebHookData Update(WebHookData customer, int id)
        {
			throw new NotImplementedException();
		}

		public virtual bool Delete(WebHookData order, int id)
        {
            return Delete(id);
        }

		public virtual bool Delete(int id)
        {
            var segments = MakeUrlSegments(id.ToString());
            return Delete(segments);
        }

        public virtual IEnumerable<WebHookData> GetAll(IFilter filter = null)
        {
			return base.GetAll<WebHookData>(filter);
        }

		public virtual WebHookData GetByID(string webhookId)
        {
            var segments = MakeUrlSegments(webhookId);
            var customer = GetByID<WebHookData>(segments);
            return customer;
        }
        #endregion
    }
}
