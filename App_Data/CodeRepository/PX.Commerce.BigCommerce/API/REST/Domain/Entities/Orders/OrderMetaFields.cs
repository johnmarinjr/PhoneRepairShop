using Newtonsoft.Json;
using PX.Commerce.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Commerce.BigCommerce.API.REST
{
	[Description(BigCommerceCaptions.Metafields)]
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class OrdersMetaFieldData : BCAPIEntity
	{
		[JsonProperty("id")]
		[Description(BigCommerceCaptions.ID)]
		public virtual int? Id { get; set; }
		public bool ShouldSerializeId()
		{
			return false;
		}

		[JsonProperty("key")]
		[Description(BigCommerceCaptions.MetaKeywords)]
		public virtual string Key { get; set; }

		[JsonProperty("namespace", NullValueHandling = NullValueHandling.Ignore)]
		[Description(BigCommerceCaptions.MetaNamespace)]
		public virtual string Namespace { get; set; }

		[JsonProperty("value", NullValueHandling = NullValueHandling.Ignore)]
		[Description(BigCommerceCaptions.Value)]
		public virtual string Value { get; set; }

		[JsonProperty("permission_set", NullValueHandling = NullValueHandling.Ignore)]
		[Description(BigCommerceCaptions.Value)]
		public virtual PermissionSet? PermissionSet { get; set; }

	}

	[JsonObject(Description = "Order MetaFieldList (BigCommerce API v3 response)")]
	public class OrdersMetaFieldList : IEntitiesResponse<OrdersMetaFieldData>
	{
		[JsonProperty("data")]
		public List<OrdersMetaFieldData> Data { get; set; }

		[JsonProperty("meta")]
		public Meta Meta { get; set; }
	}

}
