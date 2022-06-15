using PX.Data;
using System;
using System.Collections.Generic;

namespace PX.Objects.CM.Extensions
{
	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Class | AttributeTargets.Method)]
	public class PXDBCurrencyFixedPrecisionAttribute : PXDBCurrencyAttributeBase
	{
		public override int? CustomPrecision => _Precision;

		public PXDBCurrencyFixedPrecisionAttribute(int precision, Type keyField, Type resultField)
			: base(precision, keyField, resultField)
		{
		}

		protected override void _ensurePrecision(PXCache sender, object row) { }
	}
}
