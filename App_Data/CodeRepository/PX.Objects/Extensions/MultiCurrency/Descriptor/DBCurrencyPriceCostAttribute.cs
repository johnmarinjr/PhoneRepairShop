using CommonServiceLocator;
using PX.Data;
using System;

namespace PX.Objects.CM.Extensions
{
	/// <summary>
	/// Marks the field for processing by MultiCurrencyGraph. When attached to a Field that stores Amount in pair with BaseAmount Field
	/// MultiCurrencyGraph handles conversion and rounding when this field is updated.
	/// This Attribute forces the system to use precision specified for Price/Cost instead one comming from Currency
	/// Use this Attribute for DB fields. See <see cref="PXCurrencyAttribute"/> for Non-DB version.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Class | AttributeTargets.Method)]
	public class PXDBCurrencyPriceCostAttribute : PXDBCurrencyAttributeBase
	{
		public override int? CustomPrecision => _Precision;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="keyField">Field in this table used as a key for CurrencyInfo table.</param>
		/// <param name="resultField">Field in this table to store the result of currency conversion.</param>
		public PXDBCurrencyPriceCostAttribute(Type keyField, Type resultField)
			: base(typeof(Search<CS.CommonSetup.decPlPrcCst>), keyField, resultField)
		{
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			_ensurePrecision(sender, null);
		}

		protected override void _ensurePrecision(PXCache sender, object row) =>
			_Precision = ServiceLocator.Current.GetInstance<Func<PXGraph, IPXCurrencyService>>()(sender.Graph).PriceCostDecimalPlaces();
	}
}
