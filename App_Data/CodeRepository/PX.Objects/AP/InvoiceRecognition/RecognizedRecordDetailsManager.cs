using Newtonsoft.Json;
using PX.CloudServices.DAC;
using PX.CloudServices.DocumentRecognition;
using PX.Common;
using PX.Data;
using PX.Objects.AP.InvoiceRecognition.DAC;
using PX.Objects.AP.InvoiceRecognition.Feedback;
using PX.Objects.AP.InvoiceRecognition.VendorSearch;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Threading.Tasks;

namespace PX.Objects.AP.InvoiceRecognition
{
	internal class RecognizedRecordDetailsManager
	{
		private const string FEEDBACK_VENDOR_SEARCH = "feedback:entity-resolution";
		private const string VENDOR_NAME_ENTITY = "name";

		private static readonly Dictionary<string, string> _fieldsToPopulate = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ nameof(APRecognizedInvoice.CuryOrigDocAmt), nameof(RecognizedRecordDetail.Amount) },
			{ nameof(APRecognizedInvoice.DocDate), nameof(RecognizedRecordDetail.Date) },
			{ nameof(APRecognizedInvoice.DueDate), nameof(RecognizedRecordDetail.DueDate) },
			{ nameof(APRecognizedInvoice.InvoiceNbr), nameof(RecognizedRecordDetail.VendorRef) }
		};

		private readonly PXGraph _graph;
		private readonly IVendorSearchService _vendorSearchService;
		private readonly IInvoiceRecognitionService _invoiceRecognitionClient;

		public RecognizedRecordDetailsManager(PXGraph graph, IVendorSearchService vendorSearchService, IInvoiceRecognitionService invoiceRecognitionClient)
		{
			graph.ThrowOnNull(nameof(graph));
			vendorSearchService.ThrowOnNull(nameof(vendorSearchService));
			invoiceRecognitionClient.ThrowOnNull(nameof(invoiceRecognitionClient));

			_graph = graph;
			_vendorSearchService = vendorSearchService;
			_invoiceRecognitionClient = invoiceRecognitionClient;
		}

		public async Task FillRecognizedFields(RecognizedRecord record, DocumentRecognitionResult recognitionResult)
		{
			record.ThrowOnNull(nameof(record));

			var detail = new RecognizedRecordDetail
			{
				EntityType = record.EntityType,
				RefNbr = record.RefNbr
			};
			var detailCache = _graph.Caches[typeof(RecognizedRecordDetail)];

			detail = detailCache.Insert(detail) as RecognizedRecordDetail;

			var fields = recognitionResult?.Documents?[0]?.Fields;

			if (fields != null)
			{
				foreach (var fieldPair in fields)
				{
					var viewPlusFieldName = fieldPair.Key;
					if (string.IsNullOrWhiteSpace(viewPlusFieldName))
					{
						continue;
					}

					var (viewName, sourceFieldName) = InvoiceDataLoader.GetFieldInfo(fieldPair.Key);
					if (string.IsNullOrWhiteSpace(viewName) || string.IsNullOrWhiteSpace(sourceFieldName))
					{
						continue;
					}

					if (!viewName.Equals(nameof(APInvoiceRecognitionEntry.Document), StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}

					if (!_fieldsToPopulate.TryGetValue(sourceFieldName, out var destinationFieldName))
					{
						continue;
					}

					InvoiceDataLoader.SetFieldExtValue(detailCache, detail, destinationFieldName, fieldPair.Value);
				}
			}

			var feedback = FillVendorId(detail, fields, record.MailFrom);

			detail = detailCache.Update(detail) as RecognizedRecordDetail;
			detailCache.PersistInserted(detail);

			await SendVendorSearchFeedback(feedback, recognitionResult?.Links);
		}

		private VendorSearchFeedback FillVendorId(RecognizedRecordDetail detail, Dictionary<string, Field> fields, string mailFrom)
		{
			List<FullTextTerm> fullTextTerms = null;
			string vendorName = null;

			if (fields != null)
			{
				var vendorFieldKey = $"{nameof(Document)}.{nameof(APRecognizedInvoice.VendorID)}";

				if (fields.TryGetValue(vendorFieldKey, out var vendorField))
				{
					fullTextTerms = vendorField?.FullTextTerms;

					if (vendorField.Entity != null && vendorField.Entity.TryGetValue(VENDOR_NAME_ENTITY, out var vendorNameField))
					{
						var value = vendorNameField.Value as string;

						if (!string.IsNullOrEmpty(value))
						{
							vendorName = value;
						}
					}
				}

			}

			var email = ExtractEmail(mailFrom);
			var searchResult = _vendorSearchService.FindVendor(_graph, vendorName, fullTextTerms, email);

			detail.VendorID = searchResult.VendorId;
			detail.VendorTermIndex = searchResult.TermIndex;
			detail.VendorName = vendorName;

			return searchResult.Feedback;
		}

		private static string ExtractEmail(string mailFrom)
		{
			if (string.IsNullOrEmpty(mailFrom))
			{
				return null;
			}

			var startIndex = mailFrom.IndexOf('<');
			var endIndex = mailFrom.IndexOf('>');

			if (startIndex != -1 && endIndex != -1)
			{
				var length = endIndex - startIndex - 1;

				return mailFrom.Substring(startIndex + 1, length);
			}

			return mailFrom;
		}

		private async Task SendVendorSearchFeedback(VendorSearchFeedback feedback, Dictionary<string, Uri> links)
		{
			if (!(_invoiceRecognitionClient is IInvoiceRecognitionFeedback feedbackService))
			{
				return;
			}

			if (feedback == null || links == null)
			{
				return;
			}

			try
			{
				await SendVendorSearchFeedbackAsync(links, feedbackService, feedback);
			}
			catch (Exception e)
			{
				PXTrace.WriteError(e);
			}
		}

		private static async Task SendVendorSearchFeedbackAsync(Dictionary<string, Uri> links, IInvoiceRecognitionFeedback feedbackService,
			VendorSearchFeedback feedback)
		{
			if (!links.TryGetValue(FEEDBACK_VENDOR_SEARCH, out var link))
			{
				PXTrace.WriteError($"{nameof(IInvoiceRecognitionFeedback)}: Unable to send feedback - link is not found:{{LinkKey}}", FEEDBACK_VENDOR_SEARCH);
				return;
			}

			var formatter = new JsonMediaTypeFormatter { SerializerSettings = VendorSearchFeedback.Settings };
			var content = new ObjectContent(feedback.GetType(), feedback, formatter);

			await feedbackService.Send(link, content);
		}

		public async Task FillVendorId(RecognizedRecord record, RecognizedRecordDetail detail)
		{
			record.ThrowOnNull(nameof(record));

			var detailCache = _graph.Caches[typeof(RecognizedRecordDetail)];
			var recognitionResult = string.IsNullOrWhiteSpace(record.RecognitionResult) ?
				null :
				JsonConvert.DeserializeObject<DocumentRecognitionResult>(record.RecognitionResult);

			var needCreateDetail = detail?.RefNbr == null;
			if (needCreateDetail)
			{
				detail = new RecognizedRecordDetail
				{
					EntityType = record.EntityType,
					RefNbr = record.RefNbr
				};
			}

			var fields = recognitionResult?.Documents?[0]?.Fields;
			var feedback = FillVendorId(detail, fields, record.MailFrom);

			if (needCreateDetail)
			{
				detail = detailCache.Insert(detail) as RecognizedRecordDetail;
				detailCache.PersistInserted(detail);
			}
			else
			{
				detail = detailCache.Update(detail) as RecognizedRecordDetail;
				detailCache.PersistUpdated(detail);
			}

			await SendVendorSearchFeedback(feedback, recognitionResult?.Links);
		}
	}
}
