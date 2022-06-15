using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Commerce.Objects
{

	/// <summary>
	/// Represents an external shipping zone that is used for the shipping settings (ship via) mappings for order import.
	/// </summary>		
	public interface IShippingZone
	{
		string Name { get; set; }
		string Type { get; set; }
		bool? Enabled { get; set; }

		List<IShippingMethod> ShippingMethods { get; set; }
	}

	/// <summary>
	/// Represents an external shipping method that is used for the shipping settings (ship via) mappings for order import. 
	/// </summary>	
	public interface IShippingMethod
	{
		string Name { get; set; }
		string Type { get; set; }
		bool? Enabled { get; set; }

		List<String> ShippingServices { get; set; }
	}
}
