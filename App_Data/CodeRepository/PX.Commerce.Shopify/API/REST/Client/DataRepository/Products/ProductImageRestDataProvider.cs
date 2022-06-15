using PX.Commerce.Core.Model;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Commerce.Shopify.API.REST
{
	public class ProductImageRestDataProvider : RestDataProviderBase, IChildRestDataProvider<ProductImageData>
	{
		protected override string GetListUrl { get; } = "products/{parent_id}/images.json";
		protected override string GetSingleUrl { get; } = "products/{parent_id}/images/{id}.json";
		protected override string GetSearchUrl => throw new NotImplementedException();
		private string GetMetafieldsUrl { get; } = "metafields.json?metafield[owner_id]={0}&metafield[owner_resource]=product_image";

		public ProductImageRestDataProvider(IShopifyRestClient restClient) : base()
		{
			ShopifyRestClient = restClient;
		}

		public virtual ProductImageData Create(ProductImageData entity, string productId)
		{
			var segments = MakeParentUrlSegments(productId);
			return base.Create<ProductImageData, ProductImageResponse>(entity, segments);
		}

		public virtual ProductImageData Update(ProductImageData entity, string productId, string imageId)
		{
			var segments = MakeUrlSegments(imageId, productId);
			return Update<ProductImageData, ProductImageResponse>(entity, segments);
		}

		public virtual bool Delete(string productId, string imageId)
		{
			var segments = MakeUrlSegments(imageId, productId);
			return Delete(segments);
		}

		public virtual IEnumerable<ProductImageData> GetAll(string productId, IFilter filter = null)
		{
			var segments = MakeParentUrlSegments(productId);
			var imageList = GetAll<ProductImageData, ProductImagesResponse>(filter, segments);
			if (imageList != null && imageList.Count() > 0)
			{
                foreach (var oneItem in imageList)
                {
                    oneItem.Metafields = GetMetafieldsByImageId(oneItem.Id.ToString());
                    yield return oneItem;
                }
			}
			yield break;
		}

		public virtual ProductImageData GetByID(string productId, string imageId)
		{
			var segments = MakeUrlSegments(imageId, productId);
			var image = GetByID<ProductImageData, ProductImageResponse>(segments);
			if (image != null) image.Metafields = GetMetafieldsByImageId(imageId);
			return image;
		}

		public virtual IEnumerable<ProductImageData> GetAllWithoutParent(IFilter filter = null)
		{
			throw new NotImplementedException();
		}

		public virtual List<MetafieldData> GetMetafieldsByImageId(string imageId)
		{
			var request = BuildRequest(string.Format(GetMetafieldsUrl, imageId), nameof(GetMetafieldsByImageId), null, null);
			return ShopifyRestClient.GetAll<MetafieldData, MetafieldsResponse>(request).ToList();
		}
	}
}
