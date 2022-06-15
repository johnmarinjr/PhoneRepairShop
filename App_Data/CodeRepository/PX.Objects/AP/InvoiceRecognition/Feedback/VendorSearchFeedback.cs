using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PX.Objects.AP.InvoiceRecognition.Feedback.VendorSearch;
using System.Collections.Generic;

namespace PX.Objects.AP.InvoiceRecognition.Feedback
{
	internal class VendorSearchFeedback
	{
		internal static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
		{
			NullValueHandling = NullValueHandling.Ignore,
			ContractResolver = new DefaultContractResolver
			{
				NamingStrategy = new CamelCaseNamingStrategy()
			}
		};

		[JsonProperty("$version")]
		public byte Version { get; set; } = 1;

		public List<Search> Searches { get; set; }

		public Dictionary<string, Candidate> Candidates { get; set; }

		public Found Winner { get; set; }

		public override string ToString()
		{
			return JsonConvert.SerializeObject(this, Settings);
		}
	}
}
