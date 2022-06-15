using PX.Data;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.EP;
using PX.SM;
using System.Linq;

namespace PX.Objects.AP.InvoiceRecognition
{
	internal class APInvoiceEmailProcessor : BasicEmailProcessor
	{
		private readonly IInvoiceRecognitionService _invoiceRecognitionService;

		public APInvoiceEmailProcessor(IInvoiceRecognitionService invoiceRecognitionService)
		{
			_invoiceRecognitionService = invoiceRecognitionService;
		}

		protected override bool Process(Package package)
		{
			var isRecognitionEnabled = PXAccess.FeatureInstalled<FeaturesSet.apDocumentRecognition>() && _invoiceRecognitionService.IsConfigured();
			if (!isRecognitionEnabled)
			{
				return false;
			}

			var account = package.Account;
			if (account == null)
			{
				return false;
			}

			var accountCache = package.Graph.Caches[typeof(EMailAccount)];
			var accountExt = accountCache.GetExtension<DAC.EMailAccountExt>(account);

			var isProcessorEnabled = account.IncomingProcessing == true && accountExt.SubmitToIncomingAPDocuments == true;
			if (!isProcessorEnabled)
			{
				return false;
			}

			var message = package.Message;

			var processMessage = message?.Incoming == true;
			if (!processMessage)
			{
				return false;
			}

			var graph = package.Graph;
			if (graph == null)
			{
				return false;
			}

			var cache = graph.Caches[typeof(CRSMEmail)];

			var filesToProcess = APInvoiceRecognitionEntry.GetFilesToRecognize(cache, message);
			if (filesToProcess == null || filesToProcess.Length == 0)
			{
				return false;
			}

			var batch = filesToProcess.Select(file => new RecognizedRecordFileInfo(file.Name, file.Data, file.FileID.Value));

			PXLongOperation.StartOperation(graph, () =>
			{
				APInvoiceRecognitionEntry.RecognizeRecordsBatch(batch, message.Subject, message.MailFrom, message.MessageId, message.OwnerID).Wait();
			});

			return true;
		}
	}
}
