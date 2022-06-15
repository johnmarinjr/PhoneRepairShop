using System;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.CS;
using PX.Data;
using PX.Data.EP;
using PX.Objects.CR.MassProcess;

namespace PX.Objects.CR.Extensions.Cache
{
	[PXInternalUseOnly]
	public static class CacheExtensions
	{
		public static IEnumerable<string> GetFields_ContactInfo(this PXCache cache)
		{
			return GetFields_WithAttribute<PXContactInfoFieldAttribute>(cache);
		}

		public static IEnumerable<string> GetFields_MassUpdatable(this PXCache cache)
		{
			return GetFields_WithAttribute<PXMassUpdatableFieldAttribute>(cache);
		}

		public static IEnumerable<string> GetFields_MassMergable(this PXCache cache)
		{
			return GetFields_WithAttribute<PXMassMergableFieldAttribute>(cache);
		}

		public static IEnumerable<string> GetFields_Udf(this PXCache cache)
		{
			return UDFHelper
				.GetUDFFields(cache.Graph)
				.Select(attr => KeyValueHelper.AttributePrefix + attr.AttributeID);
		}

		public static IEnumerable<string> GetFields_WithAttribute<TAttribute>(this PXCache cache)
		{
			return cache
				.Fields
				.Where(field => cache
					.GetAttributesReadonly(field)
					.OfType<TAttribute>()
					.Any());
		}
	}
}
