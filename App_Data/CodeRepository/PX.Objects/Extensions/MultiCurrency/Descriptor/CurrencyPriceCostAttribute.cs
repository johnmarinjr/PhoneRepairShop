using CommonServiceLocator;
using PX.Data;
using System;

namespace PX.Objects.CM.Extensions
{
	/// <summary>
	/// Marks the field for processing by MultiCurrencyGraph. When attached to a Field that stores Amount in pair with BaseAmount Field
	/// MultiCurrencyGraph handles conversion and rounding when this field is updated. 
	/// This Attribute forces the system to use precision specified for Price/Cost instead one comming from Currency
	/// Use this Attribute for Non DB fields. See <see cref="PXDBCurrencyAttribute"/> for DB version.
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Class | AttributeTargets.Method)]
	public class PXCurrencyPriceCostAttribute : PXDecimalAttribute, ICurrencyAttribute
	{
		#region State
		internal protected Type ResultField;
		internal protected Type KeyField;
		public virtual bool BaseCalc { get; set; } = true;

		int? ICurrencyAttribute.CustomPrecision => _Precision;

		Type ICurrencyAttribute.ResultField => ResultField;

		Type ICurrencyAttribute.KeyField => KeyField;
		#endregion

		#region Ctor
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="keyField">Field in this table used as a key for CurrencyInfo table.</param>
		/// <param name="resultField">Field in this table to store the result of currency conversion.</param>
		public PXCurrencyPriceCostAttribute(Type keyField, Type resultField)
			: base(typeof(Search<CS.CommonSetup.decPlPrcCst>))
		{
			ResultField = resultField;
			KeyField = keyField;
		}

		#endregion

		#region Implementation
		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			_ensurePrecision(sender, null);
		}

		protected override void _ensurePrecision(PXCache sender, object row) =>
			_Precision = ServiceLocator.Current.GetInstance<Func<PXGraph, IPXCurrencyService>>()(sender.Graph).PriceCostDecimalPlaces();
		#endregion
	}
}
