using PX.Data;
using System;
using System.Collections.Generic;

namespace PX.Objects.CM.Extensions
{
	/// <summary>
	/// Marks the field for processing by MultiCurrencyGraph. When attached to a Field that stores Amount in pair with BaseAmount Field
	/// MultiCurrencyGraph handles conversion and rounding when this field is updated. 
	/// Use this Attribute for Non DB fields. See <see cref="PXDBCurrencyAttribute"/> for DB version.
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Class | AttributeTargets.Method)]
	public class PXCurrencyAttribute : PXDecimalAttribute, ICurrencyAttribute
	{
		#region State
		internal protected Type ResultField;
		internal protected Type KeyField;
		protected Dictionary<long, string> _Matches;
		public virtual bool BaseCalc { get; set; } = true;

		int? ICurrencyAttribute.CustomPrecision => null;

		Type ICurrencyAttribute.ResultField => ResultField;

		Type ICurrencyAttribute.KeyField => KeyField;
		#endregion

		#region Ctor
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="keyField">Field in this table used as a key for CurrencyInfo table.</param>
		/// <param name="resultField">Field in this table to store the result of currency conversion.</param>
		public PXCurrencyAttribute(Type keyField, Type resultField) : this (keyField)
		{
			ResultField = resultField;
		}

		public PXCurrencyAttribute(Type keyField)
		{
			KeyField = keyField;
		}

		#endregion

		#region Implementation

		protected override void _ensurePrecision(PXCache sender, object row) => _Precision = sender.Graph.GetPrecision(sender, row, KeyField?.Name, _Matches);

		#endregion

		#region Initialization
		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			if (KeyField != null)
			{
				_Matches = CurrencyInfo.CuryIDStringAttribute.GetMatchesDictionary(sender);
			}
		}
		#endregion
	}
}
