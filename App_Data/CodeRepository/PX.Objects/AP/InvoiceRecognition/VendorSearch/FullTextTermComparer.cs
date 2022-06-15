using PX.CloudServices.DocumentRecognition;
using System;
using System.Collections.Generic;

namespace PX.Objects.AP.InvoiceRecognition.VendorSearch
{
	internal class FullTextTermComparer : IEqualityComparer<FullTextTerm>
	{
		private readonly StringComparer _stringComparer = StringComparer.CurrentCultureIgnoreCase;

		public bool Equals(FullTextTerm x, FullTextTerm y)
		{
			return _stringComparer.Equals(x?.Text, y?.Text);
		}

		public int GetHashCode(FullTextTerm obj)
		{
			return _stringComparer.GetHashCode(obj?.Text);
		}
	}
}
