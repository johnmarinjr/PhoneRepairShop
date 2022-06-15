using Newtonsoft.Json;
using PX.Commerce.Core;
using PX.Commerce.Core.Model;
using RestSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Commerce.Shopify.API.REST
{
    public class InventoryLocationRestDataProvider : RestDataProviderBase,  IParentRestDataProvider<InventoryLocationData>
    {
        protected override string GetListUrl   { get; } = "locations.json";
        protected override string GetSingleUrl { get; } = "locations/{id}.json";
		protected override string GetSearchUrl => throw new NotImplementedException();
		protected string GetLevelsUrl { get; } = "locations/{id}/inventory_levels.json";

		public InventoryLocationRestDataProvider(IShopifyRestClient restClient) : base()
		{
            ShopifyRestClient = restClient;
		}

		public virtual InventoryLocationData Create(InventoryLocationData entity) => throw new NotImplementedException();

		public virtual InventoryLocationData Update(InventoryLocationData entity) => throw new NotImplementedException();
		public virtual InventoryLocationData Update(InventoryLocationData entity, string id) => throw new NotImplementedException();

		public virtual bool Delete(InventoryLocationData entity, string id) => throw new NotImplementedException();

		public virtual bool Delete(string id) => throw new NotImplementedException();

		public virtual IEnumerable<InventoryLocationData> GetAll(IFilter filter = null)
		{
			return GetAll<InventoryLocationData, InventoryLocationsResponse>(filter);
		}

		public virtual InventoryLocationData GetByID(string id)
		{
			var segments = MakeUrlSegments(id);
			var entity = base.GetByID<InventoryLocationData, InventoryLocationResponse>(segments);
			return entity;
		}

		public virtual List<InventoryLevelData> GetInventoryLevelsByLocation(string locationId)
		{
			var request = BuildRequest(GetLevelsUrl, nameof(GetInventoryLevelsByLocation), MakeUrlSegments(locationId), null);
			return ShopifyRestClient.GetAll<InventoryLevelData, InventoryLevelsResponse>(request).ToList();
		}
	}
}
