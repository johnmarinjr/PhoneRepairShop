using PX.Commerce.Core.Model;
using System.Collections.Generic;

namespace PX.Commerce.BigCommerce.API.REST
{
	public class ProductImagesDataProvider : RestDataProviderV3, IChildRestDataProvider<ProductsImageData>
	{
		protected override string GetListUrl { get; } = "v3/catalog/products/{parent_id}/images";
		protected override string GetSingleUrl { get; } = "v3/catalog/products/{parent_id}/images/{id}";

		public ProductImagesDataProvider(IBigCommerceRestClient restClient) : base()
		{
			_restClient = restClient;
		}

		public virtual ProductsImageData Create(ProductsImageData productsImageData, string parentId)
		{
			var segments = MakeParentUrlSegments(parentId);
			return Create<ProductsImageData, ProductsImage>(productsImageData, segments)?.Data;
		}

		public virtual ProductsImageData Update(ProductsImageData productsImageData, string id,string parentId)
		{
			var segments = MakeUrlSegments(id, parentId);
			return Update<ProductsImageData, ProductsImage>(productsImageData, segments)?.Data;
		}
		public virtual bool Delete(string id, string parentId)
		{
			var segments = MakeUrlSegments(id, parentId);
			return base.Delete(segments);
		}

		public virtual IEnumerable<ProductsImageData> GetAll(string parentId)
		{
			var segments = MakeParentUrlSegments(parentId);
			return GetAll<ProductsImageData, ProductsImageList>(null, segments);
		}

		public virtual ProductsImageData GetByID(string id, string parentId)
		{
			var segments = MakeUrlSegments(id, parentId);
			return GetByID<ProductsImageData, ProductsImage>(segments).Data;
		}

		#region Not implemented 

		public virtual int Count(string parentId)
		{
			throw new System.NotImplementedException();
		}
		#endregion
	}
}
