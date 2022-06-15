using Newtonsoft.Json;
using PX.Commerce.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Commerce.BigCommerce.API.REST
{
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public class PaymentMethod : IPaymentMethod
	{
		public string Name { get; set; }

		public string Currency { get; set; }

		public bool CreatePaymentfromOrder { get; set; }
	}
}
