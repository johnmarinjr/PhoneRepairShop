using System;
using PX.Data;
using CommonServiceLocator;

namespace PX.Objects.CM.Extensions
{
	/// <summary>
	/// Extends <see cref="PXDecimalAttribute"/> by defaulting the precision property.
	/// Precision is taken from Base Currency that is configured on the Company level.
	/// </summary>
	/// <remarks>This is a NON-DB attribute. Use it for calculated fields that are not storred in database.</remarks>
	public class PXBaseCuryAttribute : PXDecimalAttribute
	{
		protected override void _ensurePrecision(PXCache sender, object row)
		{
			_Precision = ServiceLocator.Current.GetInstance<Func<PXGraph, IPXCurrencyService>>()(sender.Graph).BaseDecimalPlaces();
		}
		public override void CacheAttached(PXCache sender)
		{
			sender.SetAltered(_FieldName, true);
			base.CacheAttached(sender);
		}
	}
}
