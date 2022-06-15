using PX.Common;
using PX.Data;
using PX.Data.Search;
using PX.Objects.AP.InvoiceRecognition.Feedback.VendorSearch;
using PX.Objects.AP.InvoiceRecognition.VendorSearch;
using System;
using System.Collections.Generic;

namespace PX.Objects.AP.InvoiceRecognition.Feedback
{
	internal class VendorSearchFeedbackBuilder
	{
		private readonly IContactRepository _contactRepository;
		private readonly IEntitySearchService _entitySearchService;

		private readonly List<Search> _searches = new List<Search>();
		private readonly Dictionary<string, Candidate> _candidates = new Dictionary<string, Candidate>();
		private Found _winner;

		public VendorSearchFeedbackBuilder(IContactRepository contactRepository, IEntitySearchService entitySearchService)
		{
			contactRepository.ThrowOnNull(nameof(contactRepository));
			entitySearchService.ThrowOnNull(nameof(entitySearchService));

			_contactRepository = contactRepository;
			_entitySearchService = entitySearchService;
		}

		public void Clear()
		{
			_searches.Clear();
			_candidates.Clear();
			_winner = null;
		}

		public void AddFullTextSearch(string query, List<Found> results)
		{
			AddSearch(SearchType.FullText, query, results);
		}

		public void AddEmailSearch(string email, List<Found> results)
		{
			AddSearch(SearchType.Email, email, results);
		}

		public void AddDomainSearch(string domain, List<Found> results)
		{
			AddSearch(SearchType.EmailDomain, domain, results);
		}

		private void AddSearch(SearchType type, string input, List<Found> found)
		{
			input.ThrowOnNull(nameof(input));

			var search = new Search
			{
				Type = type,
				Input = input,
				Found = found
			};

			_searches.Add(search);
		}

		public void AddWinner(int baccountId, float score)
		{
			if (_winner != null)
			{
				throw new PXInvalidOperationException();
			}

			var idStr = baccountId.ToString();
			if (!_candidates.ContainsKey(idStr))
			{
				throw new PXArgumentException(nameof(baccountId));
			}

			_winner = new Found
			{
				Id = idStr,
				Score = score
			};
		}

		public void AddCandidate(PXGraph graph, int baccountId, int? defContactId, int? primaryContactID, Guid? noteId, Candidate candidate)
		{
			candidate.ThrowOnNull(nameof(candidate));

			var key = baccountId.ToString();

			if (_candidates.ContainsKey(key))
			{
				return;
			}

			var accountContact = defContactId.HasValue ?
				_contactRepository.GetAccountContact(graph, baccountId, defContactId.Value) :
				null;
			var primaryContact = primaryContactID.HasValue ?
				_contactRepository.GetPrimaryContact(graph, baccountId, primaryContactID.Value) :
				null;
			var otherContactEmails = _contactRepository.GetOtherContactEmails(graph, baccountId, accountContact, primaryContact);

			candidate.Term = noteId.HasValue ? _entitySearchService.GetSearchIndexContent(noteId.Value) : null;
			candidate.Emails = new Emails
			{
				Account = accountContact?.EMail,
				Primary = primaryContact?.EMail,
				Contacts = otherContactEmails
			};

			_candidates.Add(key, candidate);
		}

		public VendorSearchFeedback ToVendorSearchFeedback()
		{
			var feedback = new VendorSearchFeedback
			{
				Searches = new List<Search>(_searches),
				Candidates = new Dictionary<string, Candidate>(_candidates),
				Winner = _winner
			};

			return feedback;
		}
	}
}
