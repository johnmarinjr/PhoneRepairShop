using PX.Data;
using System;
using System.Collections.Generic;

namespace PX.Objects.CM.Extensions
{
	/// <summary>
	/// Marks the field for processing by MultiCurrencyGraph. When attached to a Field that stores Amount in pair with BaseAmount Field
	/// MultiCurrencyGraph handles conversion and rounding when this field is updated.
	/// Use this Attribute for DB fields. See <see cref="PXCurrencyAttribute"/> for Non-DB version.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Class | AttributeTargets.Method)]
	public class PXDBCurrencyAttribute : PXDBCurrencyAttributeBase
	{
		protected Dictionary<long, string> _matches;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="keyField">Field in this table used as a key for CurrencyInfo table.</param>
		/// <param name="resultField">Field in this table to store the result of currency conversion.</param>
		public PXDBCurrencyAttribute(Type keyField, Type resultField)
			: base(keyField, resultField)
		{
		}

		protected override void _ensurePrecision(PXCache sender, object row) => _Precision = sender.Graph.GetPrecision(sender, row, KeyField?.Name, _matches);

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			if (KeyField != null)
			{
				_matches = CurrencyInfo.CuryIDStringAttribute.GetMatchesDictionary(sender);
			}
		}
	}
}
