using PX.Commerce.Core.Model;
using System;
using System.Collections.Generic;

namespace PX.Commerce.BigCommerce.API.REST
{
	public class ProductVideoDataProvider : RestDataProviderV3, IChildRestDataProvider<ProductsVideo>
	{
		protected override string GetListUrl { get; } = "v3/catalog/products/{parent_id}/videos";

		protected override string GetSingleUrl { get; } = "v3/catalog/products/{parent_id}/videos/{id}";

		public ProductVideoDataProvider(IBigCommerceRestClient restClient) : base()
		{
			_restClient = restClient;
		}

		public virtual ProductsVideo Create(ProductsVideo productsVideo, string parentId)
		{
			var segments = MakeParentUrlSegments(parentId);
			var productVideo = new ProductVideoData { Data = productsVideo };
			return Create<ProductsVideo, ProductVideoData>(productVideo, segments).Data;
		}

		#region Not Implemented
		[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		public virtual ProductsVideo Update(ProductsVideo productsVideo, string id, string parentId)
		{
			throw new NotImplementedException();
		}

		[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		public virtual int Count(string parentId)
		{
			throw new NotImplementedException();
		}

		[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		public virtual bool Delete(string id, string parentId)
		{
			throw new NotImplementedException();
		}

		[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		public virtual ProductsVideo GetByID(string id, string parentId)
		{
			throw new NotImplementedException();
		}

		[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		public virtual IEnumerable<ProductsVideo> GetAll(string externID)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}
