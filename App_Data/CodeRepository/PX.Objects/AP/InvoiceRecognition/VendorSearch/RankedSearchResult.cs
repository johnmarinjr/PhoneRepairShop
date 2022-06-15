using PX.CloudServices.DocumentRecognition;
using PX.Common;
using PX.Objects.AP.InvoiceRecognition.Feedback.VendorSearch;
using System;

namespace PX.Objects.AP.InvoiceRecognition.VendorSearch
{
	internal class RankedSearchResult : IComparable<RankedSearchResult>
	{
		public Candidate Candidate { get; }
		public float Rank => Candidate.Score.Value;
		public int? TermIndex { get; }
		public Vendor Vendor { get; }

		public RankedSearchResult(Vendor vendor, int? termIndex)
		{
			vendor.ThrowOnNull(nameof(vendor));

			Vendor = vendor;
			TermIndex = termIndex;

			Candidate = new Candidate { Score = 0 };
		}

		public void IncreaseRank()
		{
			Candidate.Score++;
		}

		public int CompareTo(RankedSearchResult other)
		{
			if (other == null)
			{
				return 1;
			}

			if (Rank > other.Rank)
			{
				return 1;
			}

			if (Rank < other.Rank)
			{
				return -1;
			}

			return 0;
		}
	}
}
