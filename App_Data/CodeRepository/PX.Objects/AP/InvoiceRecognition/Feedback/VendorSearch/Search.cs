using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;

namespace PX.Objects.AP.InvoiceRecognition.Feedback.VendorSearch
{
	internal class Search
	{
		[JsonConverter(typeof(StringEnumConverter), typeof(CamelCaseNamingStrategy))]
		public SearchType Type { get; set; }
		public string Input { get; set; }
		public List<Found> Found { get; set; }
	}
}
