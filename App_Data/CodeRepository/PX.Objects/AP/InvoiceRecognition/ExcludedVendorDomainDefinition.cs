using PX.Data;
using PX.Objects.AP.InvoiceRecognition.DAC;
using System.Collections.Generic;

namespace PX.Objects.AP.InvoiceRecognition
{
	internal class ExcludedVendorDomainDefinition : IPrefetchable
	{
		private readonly HashSet<string> _names = new HashSet<string>();

		public bool Contains(string name) => _names.Contains(name);

		public void Prefetch()
		{
			_names.Clear();

			foreach (PXDataRecord record in PXDatabase.SelectMulti<ExcludedVendorDomain>(new PXDataField<ExcludedVendorDomain.name>()))
			{
				_names.Add(record.GetString(0));
			}
		}

		public static ExcludedVendorDomainDefinition GetSlot() => 
			PXDatabase.GetSlot<ExcludedVendorDomainDefinition>(typeof(ExcludedVendorDomainDefinition).FullName, typeof(ExcludedVendorDomain));
	}
}
