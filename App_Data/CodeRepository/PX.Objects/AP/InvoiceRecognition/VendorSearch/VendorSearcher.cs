using PX.CloudServices.DocumentRecognition;
using PX.Common;
using PX.Data;
using PX.Data.Search;
using PX.Objects.AP.InvoiceRecognition.Feedback;
using PX.Objects.AP.InvoiceRecognition.Feedback.VendorSearch;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.AP.InvoiceRecognition.VendorSearch
{
	internal class VendorSearcher : IVendorSearchService
	{
		private readonly IVendorRepository _vendorRepository;
		private readonly IEntitySearchService _entitySearchService;
		private readonly VendorSearchFeedbackBuilder _feedbackBuilder;

		public VendorSearcher(IVendorRepository vendorRepository, IEntitySearchService entitySearchService,
			VendorSearchFeedbackBuilder feedbackBuilder)
		{
			vendorRepository.ThrowOnNull(nameof(vendorRepository));
			entitySearchService.ThrowOnNull(nameof(entitySearchService));
			feedbackBuilder.ThrowOnNull(nameof(feedbackBuilder));

			_vendorRepository = vendorRepository;
			_entitySearchService = entitySearchService;
			_feedbackBuilder = feedbackBuilder;
		}

		private IEnumerable<(string FullTextQuery, Guid[] Results)> SearchByTerm(FullTextTerm term)
		{
			if (string.IsNullOrWhiteSpace(term?.Text))
			{
				return Enumerable.Empty<(string, Guid[])>();
			}

			var resultList = new List<(string, Guid[])>();
			var query = EscapeExactSearchQuery(term.Text);
			var (fullTextQuery, results) = FullTextSearch(query);

			resultList.Add((fullTextQuery, results));

			if (results.Length > 0)
			{
				return resultList;
			}

			for (var lastWhitespaceIndex = query.LastIndexOf(' '); lastWhitespaceIndex > 0; lastWhitespaceIndex = query.LastIndexOf(' '))
			{
				query = query.Remove(lastWhitespaceIndex);
				(fullTextQuery, results) = FullTextSearch(query);

				switch (results.Length)
				{
					case 0:
						resultList.Add((fullTextQuery, results));
						break;
					case 1:
						resultList.Add((fullTextQuery, results));
						return resultList;
					default:
						return resultList;
				}
			}

			return resultList;
		}

		private string EscapeExactSearchQuery(string query)
		{
			var isEscaped = false;
			var escapedQuery = query;

			while (!isEscaped)
			{
				if (string.IsNullOrEmpty(escapedQuery))
				{
					return escapedQuery;
				}

				escapedQuery = escapedQuery.Trim();

				// We don't want to search for exact match so we need to remove enclosing double quotes
				if (escapedQuery.StartsWith("\"") && escapedQuery.EndsWith("\"") && escapedQuery.Length > 1)
				{
					escapedQuery = escapedQuery.Substring(1, escapedQuery.Length - 2);
				}
				else
				{
					isEscaped = true;
				}
			}

			return escapedQuery;
		}

		private (string FullTextQuery, Guid[] Results) FullTextSearch(string query)
		{
			List<EntitySearchResult> results = null;

			try
			{
				results = _entitySearchService.Search(query, 0, WebConfig.MaxFullTextSearchResultCount, typeof(Vendor).FullName);
			}
			catch (Exception e)
			{
				PXTrace.WriteError(e);
			}

			if (results == null)
			{
				return (query, Array<Guid>.Empty);
			}

			var vendorRelatedResults = results
				.Select(r => r.NoteID)
				.ToArray();

			return (query, vendorRelatedResults);
		}

		public VendorSearchResult FindVendor(PXGraph graph, string vendorName, IList<FullTextTerm> fullTextTerms, string email)
		{
			graph.ThrowOnNull(nameof(graph));

			if (!string.IsNullOrEmpty(vendorName))
			{
				var vendorId = _vendorRepository.GetActiveVendorIdByVendorName(graph, vendorName);
				if (vendorId != null)
				{
					return new VendorSearchResult(vendorId, null, null);
				}
			}

			_feedbackBuilder.Clear();

			var searchResultCollection = new RankedSearchResultCollection(_feedbackBuilder);

			if (fullTextTerms != null)
			{
				SearchByTerms(graph, searchResultCollection, fullTextTerms);
			}

			if (!string.IsNullOrWhiteSpace(email))
			{
				SearchByEmail(graph, searchResultCollection, email);
			}

			var maxRankResult = searchResultCollection.GetMaxRankResult();
			if (maxRankResult == null)
			{
				return new VendorSearchResult();
			}

			_feedbackBuilder.AddWinner(maxRankResult.Vendor.BAccountID.Value, maxRankResult.Rank);
			var feedback = _feedbackBuilder.ToVendorSearchFeedback();

			return new VendorSearchResult(maxRankResult.Vendor.BAccountID, maxRankResult.TermIndex, feedback);
		}

		private void SearchByTerms(PXGraph graph, RankedSearchResultCollection searchResultCollection, IList<FullTextTerm> fullTextTerms)
		{
			var fullTextTermsDistinct = fullTextTerms.Distinct(new FullTextTermComparer());

			foreach (var term in fullTextTermsDistinct)
			{
				var searchByTermResults = SearchByTerm(term);

				foreach (var (fullTextQuery, results) in searchByTermResults)
				{
					var foundList = new List<Found>();

					foreach (var noteId in results)
					{
						var vendor = _vendorRepository.GetActiveVendorByNoteId(graph, noteId);
						if (vendor == null)
						{
							continue;
						}

						var termIndex = fullTextTerms.IndexOf(term);
						var searchResult = searchResultCollection.Add(graph, vendor, termIndex);

						var found = new Found
						{
							Id = vendor.BAccountID.Value.ToString(),
							Score = searchResult.Rank
						};
						foundList.Add(found);
					}

					_feedbackBuilder.AddFullTextSearch(fullTextQuery, foundList);
				}
			}
		}

		private void SearchByExactEmail(PXGraph graph, RankedSearchResultCollection searchResultCollection, string email, out bool anyFound)
		{
			var vendorList = _vendorRepository.GetVendorsByEmail(graph, email);
			var foundList = new List<Found>();

			foreach (var vendor in vendorList)
			{
				var searchResult = searchResultCollection.Add(graph, vendor);

				var found = new Found
				{
					Id = vendor.BAccountID.Value.ToString(),
					Score = searchResult.Rank
				};
				foundList.Add(found);
			}

			_feedbackBuilder.AddEmailSearch(email, foundList);
			anyFound = foundList.Count > 0;
		}

		private void SearchByEmailDomain(PXGraph graph, RankedSearchResultCollection searchResultCollection, string email)
		{
			var domain = email.Substring(email.IndexOf('@') + 1);

			if (string.IsNullOrEmpty(domain) || _vendorRepository.IsExcludedDomain(domain))
			{
				return;
			}

			var (domainQuery, results) = _vendorRepository.GetVendorsByDomain(graph, domain);
			var foundList = new List<Found>();

			foreach (var vendor in results)
			{
				var searchResult = searchResultCollection.Add(graph, vendor);

				var found = new Found
				{
					Id = vendor.BAccountID.Value.ToString(),
					Score = searchResult.Rank
				};
				foundList.Add(found);
			}

			_feedbackBuilder.AddDomainSearch(domainQuery, foundList);
		}

		private void SearchByEmail(PXGraph graph, RankedSearchResultCollection searchResultCollection, string email)
		{
			SearchByExactEmail(graph, searchResultCollection, email, out var anyFound);
			
			if (anyFound)
			{
				return;
			}

			SearchByEmailDomain(graph, searchResultCollection, email);
		}
	}
}
