using Newtonsoft.Json;
using PX.Commerce.Core;
using PX.Commerce.Core.Model;
using RestSharp.Extensions;
using System;
using System.Collections.Generic;

namespace PX.Commerce.Shopify.API.REST
{
    public class InventoryLevelRestDataProvider : RestDataProviderBase,  IParentRestDataProvider<InventoryLevelData>
    {
        protected override string GetListUrl   { get; } = "inventory_levels.json";
        protected override string GetSingleUrl => throw new NotImplementedException();
		protected override string GetSearchUrl => throw new NotImplementedException();
		private string GetDeleteUrl { get; } = "inventory_levels.json?inventory_item_id={0}&location_id={1}";
		private string GetPostSetUrl { get; } = "inventory_levels/set.json";
		private string GetPostAdjustUrl { get; } = "inventory_levels/adjust.json";
		private string GetPostConnectUrl { get; } = "inventory_levels/connect.json";

		public InventoryLevelRestDataProvider(IShopifyRestClient restClient) : base()
		{
            ShopifyRestClient = restClient;
		}

		public virtual InventoryLevelData Create(InventoryLevelData entity) => throw new NotImplementedException();

		public virtual InventoryLevelData Update(InventoryLevelData entity) => throw new NotImplementedException();
		public virtual InventoryLevelData Update(InventoryLevelData entity, string id) => throw new NotImplementedException();

		public virtual bool Delete(InventoryLevelData entity, string id) => throw new NotImplementedException();

		public virtual bool Delete(string id) => throw new NotImplementedException();

		public virtual bool Delete(string inventoryItemId, string inventoryLocationId)
		{
			var request = BuildRequest(string.Format(GetDeleteUrl, inventoryItemId, inventoryLocationId), nameof(Delete), null, null);
			return ShopifyRestClient.Delete(request);
		}

		public virtual IEnumerable<InventoryLevelData> GetAll(IFilter filter = null)
		{
			if (filter == null) throw new Exception("You must include inventory_item_ids, location_ids, or both as filter parameters");
			return GetAll<InventoryLevelData, InventoryLevelsResponse>(filter);
		}

		public virtual InventoryLevelData GetByID(string id) => throw new NotImplementedException();

		public virtual InventoryLevelData AdjustInventory(InventoryLevelData entity)
		{
			ShopifyRestClient.Logger?.ForContext("Scope", new BCLogTypeScope(GetType()))
				.ForContext("Object", entity)
				.Verbose("{CommerceCaption}: adjusting {EntityType} entry", BCCaptions.CommerceLogCaption, entity.GetType().ToString());
			var request = BuildRequest(GetPostAdjustUrl, nameof(AdjustInventory), null, null);
			return ShopifyRestClient.Post<InventoryLevelData, InventoryLevelResponse>(request, entity, false);
		}

		public virtual InventoryLevelData SetInventory(InventoryLevelData entity)
		{
			ShopifyRestClient.Logger?.ForContext("Scope", new BCLogTypeScope(GetType()))
				.ForContext("Object", entity)
				.Verbose("{CommerceCaption}: setting {EntityType} entry", BCCaptions.CommerceLogCaption, entity.GetType().ToString());
			var request = BuildRequest(GetPostSetUrl, nameof(SetInventory), null, null);
			return ShopifyRestClient.Post<InventoryLevelData, InventoryLevelResponse>(request, entity, false);
		}

		public virtual InventoryLevelData ConnectInventory(InventoryLevelData entity)
		{
			ShopifyRestClient.Logger?.ForContext("Scope", new BCLogTypeScope(GetType()))
				.ForContext("Object", entity)
				.Verbose("{CommerceCaption}: connecting {EntityType} entry", BCCaptions.CommerceLogCaption, entity.GetType().ToString());
			var request = BuildRequest(GetPostConnectUrl, nameof(ConnectInventory), null, null);
			return ShopifyRestClient.Post<InventoryLevelData, InventoryLevelResponse>(request, entity, false);
		}
	}
}
