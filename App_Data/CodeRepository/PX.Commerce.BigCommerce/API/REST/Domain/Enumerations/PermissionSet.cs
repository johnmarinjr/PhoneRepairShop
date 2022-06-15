using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace PX.Commerce.BigCommerce.API.REST
{
	[JsonConverter(typeof(StringEnumConverter))]
	public enum PermissionSet
	{
		[EnumMember(Value = "app_only")]
		App_Only,
		[EnumMember(Value = "read")]
		Read,
		[EnumMember(Value = "write")]
		Write,
		[EnumMember(Value = "read_and_sf_access")]
		ReadAndSFAccess,
		[EnumMember(Value = "write_and_sf_access")]
		WriteAndSfAccess,
	}
}
		