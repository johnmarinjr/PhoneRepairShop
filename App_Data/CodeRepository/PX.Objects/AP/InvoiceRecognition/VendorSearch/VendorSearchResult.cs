using PX.Common;
using PX.Objects.AP.InvoiceRecognition.Feedback;

namespace PX.Objects.AP.InvoiceRecognition.VendorSearch
{
	internal readonly struct VendorSearchResult
	{
		public int? VendorId { get; }
		public int? TermIndex { get; }
		public VendorSearchFeedback Feedback { get; }

		public VendorSearchResult(int? vendorId, int? termIndex, VendorSearchFeedback feedback)
		{
			Feedback = feedback;
			VendorId = vendorId;
			TermIndex = termIndex;
		}
	}
}
