using PX.Data;
using System;
using System.Linq;

namespace PX.Objects.CM.Extensions
{
	public class SlaveCuryIDAttribute : PXAggregateAttribute, IPXRowPersistingSubscriber
	{
		private readonly Type SourceField;

		public SlaveCuryIDAttribute(Type sourceField)
		{
			SourceField = sourceField;
			_Attributes.Add(new PXDBStringAttribute(5) { IsUnicode = true, InputMask = ">LLLLL" });
			_Attributes.Add(new PXUIFieldAttribute { DisplayName = "Currency" });
			_Attributes.Add(new PXSelectorAttribute(typeof(Currency.curyID)));
		}

		public void RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			long? newkey = CurrencyInfoAttribute.GetPersistedCuryInfoID(sender, (long?)sender.GetValue(e.Row, SourceField.Name));
			CurrencyInfo currencyInfo = (CurrencyInfo)PXSelect<CurrencyInfo, Where<CurrencyInfo.curyInfoID, Equal<Required<CurrencyInfo.curyInfoID>>>>.Select(sender.Graph, newkey);
			if (currencyInfo?.CuryID != null)
				sender.SetValue(e.Row, FieldName, currencyInfo.CuryID);
		}
	}
}
