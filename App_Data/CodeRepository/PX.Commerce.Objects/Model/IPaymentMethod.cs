using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Commerce.Objects
{
	/// <summary>
	/// Represents an external payment method that is used to provide mapping to payment method in Acumatica ERP, and the processing center associated with this method.
	/// </summary>	
	public interface IPaymentMethod
	{
		/// <summary>
		/// Payment method name
		/// </summary>	
		string Name { get; set; }
		/// <summary>
		/// Currency used in the payment method
		/// </summary>	
		string Currency { get; set; }
		/// <summary>
		/// Indicator of whether the payment should be created from an order
		/// </summary>	
		bool CreatePaymentfromOrder { get; set; }
	}

}
