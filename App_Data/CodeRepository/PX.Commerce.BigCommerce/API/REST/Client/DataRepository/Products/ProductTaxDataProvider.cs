using PX.Commerce.Core.Model;
using PX.Commerce.Objects;
using System;
using System.Collections.Generic;

namespace PX.Commerce.BigCommerce.API.REST
{
	public class TaxDataProvider : RestDataProviderV3 
	{
		protected override string GetListUrl { get; } = "v2/tax_classes";

		protected override string GetSingleUrl { get; } = "v2/tax_classes/{id}";

		public TaxDataProvider(IBigCommerceRestClient restClient) : base()
		{
			_restClient = restClient;
		}
		public virtual ProductsTax GetByID(int id)
		{
			var segments = MakeUrlSegments(id.ToString());
			var result = base.GetByID<ProductsTaxData, ProductsTax>(segments);
			return result;
		}
		public virtual List<ProductsTaxData> GetAll()
		{
			var request = _restClient.MakeRequest(GetListUrl);
			var result = _restClient.Get<List<ProductsTaxData>>(request);
			result.Add(new ProductsTaxData() { Id = 0, Name = BCObjectsConstants.DefaultTaxClass });
			return result;
		}

		#region Not Implemented
		public virtual ProductsVideo Update(ProductsVideo productsVideo, string id, string parentId)
		{
			throw new NotImplementedException();
		}

		public virtual int Count(string parentId)
		{
			throw new NotImplementedException();
		}

		public virtual bool Delete(string id, string parentId)
		{
			throw new NotImplementedException();
		}

		public virtual List<ProductsVideo> Get(string parentId)
		{
			throw new NotImplementedException();
		}

		public virtual ProductsVideo GetByID(string id, string parentId)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
