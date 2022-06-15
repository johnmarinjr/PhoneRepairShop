using System;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Objects.IN;

namespace PX.Objects.SO
{
	public class PXDBBaseQtyWithOrigQtyAttribute : PXDBBaseQuantityAttribute
	{
		private Type _origUomField;
		private Type _baseOrigQtyField;
		private Type _origQtyField;

		public PXDBBaseQtyWithOrigQtyAttribute(Type uomField, Type qtyField,
			Type origUomField, Type baseOrigQtyField, Type origQtyField)
			: base(uomField, qtyField)
		{
			_origUomField = origUomField ?? throw new ArgumentException(nameof(origUomField));
			_baseOrigQtyField = baseOrigQtyField ?? throw new ArgumentException(nameof(baseOrigQtyField));
			_origQtyField = origQtyField ?? throw new ArgumentException(nameof(origQtyField));
		}

		protected override decimal? CalcResultValue(PXCache sender, QtyConversionArgs e)
		{
			object uom = sender.GetValue(e.Row, KeyField.Name),
				origUom = sender.GetValue(e.Row, _origUomField.Name),
				baseQty = e.NewValue,
				baseOrigQty = sender.GetValue(e.Row, _baseOrigQtyField.Name);
			if (Equals(uom, origUom) && Equals(baseQty, baseOrigQty))
			{
				return (decimal?)sender.GetValue(e.Row, _origQtyField.Name);
			}
			else
			{
				return base.CalcResultValue(sender, e);
			}
		}
	}
}
