using PX.CloudServices.DocumentRecognition;
using PX.Common;
using PX.Objects.AP.InvoiceRecognition.Feedback;
using System;
using System.Linq;
using System.Collections.Generic;
using PX.Data;

namespace PX.Objects.AP.InvoiceRecognition.VendorSearch
{
	internal class RankedSearchResultCollection
	{
		private readonly Dictionary<Guid?, RankedSearchResult> _resultsByNoteId = new Dictionary<Guid?, RankedSearchResult>();
		private readonly VendorSearchFeedbackBuilder _feedbackBuilder;

		public int Count => _resultsByNoteId.Count;

		public RankedSearchResultCollection(VendorSearchFeedbackBuilder feedbackBuilder)
		{
			feedbackBuilder.ThrowOnNull(nameof(feedbackBuilder));

			_feedbackBuilder = feedbackBuilder;
		}

		public RankedSearchResult Add(PXGraph graph, Vendor vendor, int? termIndex = null)
		{
			vendor.ThrowOnNull(nameof(vendor));

			if (_resultsByNoteId.TryGetValue(vendor.NoteID, out var existingResult))
			{
				existingResult.IncreaseRank();

				return existingResult;
			}
			else
			{
				var newResult = new RankedSearchResult(vendor, termIndex);
				_resultsByNoteId.Add(vendor.NoteID, newResult);

				_feedbackBuilder.AddCandidate(graph, newResult.Vendor.BAccountID.Value, newResult.Vendor.DefContactID, newResult.Vendor.PrimaryContactID,
					newResult.Vendor.NoteID, newResult.Candidate);

				return newResult;
			}
		}

		public RankedSearchResult GetMaxRankResult()
		{
			if (_resultsByNoteId.Count == 0)
			{
				return null;
			}

			return _resultsByNoteId.Values.Max();
		}
	}
}
