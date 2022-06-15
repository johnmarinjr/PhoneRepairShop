using PX.Commerce.Core;
using PX.Commerce.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Commerce.BigCommerce.API.REST
{
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class ShippingZoneData : BCAPIEntity, IShippingZone
	{
		public ShippingZoneData()
		{

		}

		/// <summary>
		/// The unique numeric identifier for the shipping zone.
		/// </summary>
		[ShouldNotSerialize]
		public long? Id { get; set; }

		/// <summary>
		/// The name of the shipping zone, specified by the user.
		/// </summary>
		[ShouldNotSerialize]
		public string Name { get; set; }

		/// <summary>
		/// The ID of the shipping zone's delivery profile. Shipping profiles allow merchants to create product-based or location-based shipping rates.
		/// </summary>
		[ShouldNotSerialize]
		public string ProfileId { get; set; }

		/// <summary>
		/// The ID of the shipping zone's location group. 
		/// Location groups allow merchants to create shipping rates that apply only to the specific locations in the group.
		/// </summary>
		public string LocationGroupId { get; set; }

		public string Type { get; set; }
		public bool? Enabled { get; set; } = true;
		public List<IShippingMethod> ShippingMethods { get; set; }
	}

	public class ShippingMethod : IShippingMethod
	{
		public long? Id { get; set; }
		public string Name { get; set; }
		public string Type { get; set; }
		public bool? Enabled { get; set; }
		public List<string> ShippingServices { get; set; }
	}
}
