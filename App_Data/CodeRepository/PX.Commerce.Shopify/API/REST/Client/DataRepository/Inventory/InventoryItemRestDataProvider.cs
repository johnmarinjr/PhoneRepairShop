using Newtonsoft.Json;
using PX.Commerce.Core;
using PX.Commerce.Core.Model;
using RestSharp.Extensions;
using System;
using System.Collections.Generic;

namespace PX.Commerce.Shopify.API.REST
{
    public class InventoryItemRestDataProvider : RestDataProviderBase,  IParentRestDataProvider<InventoryItemData>
    {
        protected override string GetListUrl   { get; } = "inventory_items.json";
        protected override string GetSingleUrl { get; } = "inventory_items/{id}.json";
		protected override string GetSearchUrl => throw new NotImplementedException();

		public InventoryItemRestDataProvider(IShopifyRestClient restClient) : base()
		{
            ShopifyRestClient = restClient;
		}

		public virtual InventoryItemData Create(InventoryItemData entity) => throw new NotImplementedException();

		public virtual InventoryItemData Update(InventoryItemData entity) => Update(entity, entity.Id.ToString());
		public virtual InventoryItemData Update(InventoryItemData entity, string id)
		{
			var segments = MakeUrlSegments(id);
			return base.Update<InventoryItemData, InventoryItemResponse>(entity, segments);
		}

		public virtual bool Delete(InventoryItemData entity, string id) => Delete(id);

		public virtual bool Delete(string id)
		{
			var segments = MakeUrlSegments(id);
			return Delete(segments);
		}

		public virtual IEnumerable<InventoryItemData> GetAll(IFilter filter = null)
		{
			return GetAll<InventoryItemData, InventoryItemsResponse>(filter);
		}

		public virtual InventoryItemData GetByID(string id)
		{
			var segments = MakeUrlSegments(id);
			var entity = base.GetByID<InventoryItemData, InventoryItemResponse>(segments);
			return entity;
		}
	}
}
