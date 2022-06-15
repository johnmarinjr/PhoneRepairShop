using PX.Commerce.Core.Model;
using System;
using System.Collections.Generic;

namespace PX.Commerce.Shopify.API.REST
{
    public class MetafieldRestDataProvider : RestDataProviderBase, IParentRestDataProvider<MetafieldData>
    {
        protected override string GetListUrl { get; } = "metafields.json";
        protected override string GetSingleUrl { get; } = "metafields/{id}.json";
		protected override string GetSearchUrl => throw new NotImplementedException();

		public MetafieldRestDataProvider(IShopifyRestClient restClient) : base()
		{
            ShopifyRestClient = restClient;
		}

		#region IParentDataRestClient
		public virtual MetafieldData Create(MetafieldData entity)
        {
            var result = base.Create<MetafieldData, MetafieldResponse>(entity);
            return result;
        }

		public virtual MetafieldData Update(MetafieldData entity, string id)
        {
			var segments = MakeUrlSegments(id);
			return base.Update<MetafieldData, MetafieldResponse>(entity, segments);
		}

		public virtual bool Delete(string id)
        {
            var segments = MakeUrlSegments(id);
            return base.Delete(segments);
        }

		public virtual IEnumerable<MetafieldData> GetAll(IFilter filter = null)
        {
            var result = base.GetAll<MetafieldData, MetafieldsResponse>(filter);
            return result;
        }

		public virtual MetafieldData GetByID(string id)
        {
            var segments = MakeUrlSegments(id);
            var result = GetByID<MetafieldData, MetafieldResponse>(segments);
            return result;
        }

		public virtual MetafieldData GetMetafieldBySpecifiedUrl(string url, string id)
		{
			var request = BuildRequest(url, nameof(GetMetafieldBySpecifiedUrl), MakeUrlSegments(id), null);
			return ShopifyRestClient.Get<MetafieldData, MetafieldResponse>(request);
		}

		public virtual IEnumerable<MetafieldData> GetMetafieldsBySpecifiedUrl(string url, string id)
		{
			var request = BuildRequest(url, nameof(GetMetafieldBySpecifiedUrl), MakeUrlSegments(id), null);
			return ShopifyRestClient.GetAll<MetafieldData, MetafieldsResponse>(request);
		}
		#endregion
	}
}
