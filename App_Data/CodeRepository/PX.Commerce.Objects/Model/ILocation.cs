using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Commerce.Objects
{
	/// <summary>
	/// Represents an external location for the inventory import and export mappings between two systems.
	/// </summary>
	public interface ILocation
	{
		/// <summary>
		/// Location ID
		/// </summary>
		long? Id { get; set; }
		/// <summary>
		/// Location name
		/// </summary>
		string Name { get; set; }
		/// <summary>
		/// Indicator of whether the location is activated
		/// </summary>
		bool? Active { get; set; }
	}
}
