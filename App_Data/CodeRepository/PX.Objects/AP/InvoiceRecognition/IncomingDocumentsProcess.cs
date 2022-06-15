using PX.CloudServices.DAC;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AP.InvoiceRecognition.DAC;
using PX.SM;
using PX.TM;
using PX.Web.UI;
using System;
using System.Collections;
using System.Collections.Generic;

namespace PX.Objects.AP.InvoiceRecognition
{
	[PXInternalUseOnly]
	public class IncomingDocumentsProcess : PXGraph<IncomingDocumentsProcess>
	{
		private const string _processButtonName = "Process";

		[PXFilterable]
		public PXProcessingJoin<RecognizedRecordForProcessing,
			   LeftJoin<RecognizedRecordDetail,
			   On<RecognizedRecordForProcessing.entityType, Equal<RecognizedRecordDetail.entityType>, And<
				  RecognizedRecordForProcessing.refNbr, Equal<RecognizedRecordDetail.refNbr>>>>,
			   Where<RecognizedRecordForProcessing.entityType, Equal<RecognizedRecordEntityTypeListAttribute.aPDocument>>,
			   OrderBy<Desc<RecognizedRecordForProcessing.createdDateTime>>> Records;

		public SelectFrom<RecognizedRecordErrorHistory>.
			   Where<RecognizedRecordErrorHistory.refNbr.IsEqual<RecognizedRecordForProcessing.refNbr.FromCurrent>.And<
					  RecognizedRecordErrorHistory.entityType.IsEqual<RecognizedRecordForProcessing.entityType.FromCurrent>>>.
			   OrderBy<RecognizedRecordErrorHistory.createdDateTime.Desc>.
			   View.ReadOnly ErrorHistory;

		public PXCancel<RecognizedRecordForProcessing> Cancel;
		public PXAction<RecognizedRecordForProcessing> Insert;
		public PXAction<RecognizedRecordForProcessing> Delete;
		public PXAction<RecognizedRecordForProcessing> EditRecord;
		public PXAction<RecognizedRecordForProcessing> ViewDocument;
		public PXAction<RecognizedRecordForProcessing> ViewErrorHistory;
		public PXAction<RecognizedRecordForProcessing> SearchVendor;
		public PXAction<RecognizedRecordForProcessing> UploadFiles;

		public IncomingDocumentsProcess()
		{
			Records.SetProcessCaption(Messages.Recognize);
			Records.SetProcessAllCaption(Messages.RecognizeAll);
			Records.SetProcessDelegate(Recognize);

			Actions.Move(_processButtonName, nameof(Insert));
			Actions.Move(_processButtonName, nameof(Delete));
			Actions.Move(nameof(Insert), nameof(Cancel));

			PXUIFieldAttribute.SetDisplayName<RecognizedRecordDetail.vendorID>(Caches[typeof(RecognizedRecordDetail)], Messages.RecognizedVendor);
		}

		[PXEntryScreenRights(typeof(APRecognizedInvoice), nameof(APInvoiceRecognitionEntry.Insert))]
		[PXInsertButton]
		[PXUIField]
		protected virtual void insert()
		{
			var graph = CreateInstance<APInvoiceRecognitionEntry>();

			throw new PXRedirectRequiredException(graph, null);
		}

		[PXEntryScreenRights(typeof(APRecognizedInvoice), nameof(APInvoiceRecognitionEntry.Delete))]
		[PXButton(ImageKey = Sprite.Main.Remove, ConfirmationMessage = ActionsMessages.ConfirmDeleteMultiple)]
		[PXUIField]
		protected virtual void delete()
		{
			Records.SetProcessDelegate(RecognizedRecordProcess.DeleteRecognizedRecord);
			Actions[_processButtonName].PressButton();
		}

		[PXButton]
		[PXUIField(Visible = false)]
		protected virtual void editRecord()
		{
			var refNbr = Records.Current?.RefNbr;
			if (refNbr == null)
			{
				return;
			}

			var select = new
				SelectFrom<APRecognizedInvoice>.
				Where<APRecognizedInvoice.recognizedRecordRefNbr.IsEqual<@P.AsGuid>>.
				View.ReadOnly(this);
			select.View.Clear();

			// Acuminator disable once PX1015 IncorrectNumberOfSelectParameters Diagnostic bug
			var record = select.SelectSingle(refNbr);
			if (record == null)
			{
				return;
			}

			var graph = CreateInstance<APInvoiceRecognitionEntry>();
			graph.Document.Current = record;

			throw new PXRedirectRequiredException(graph, null);
		}

		[PXButton]
		[PXUIField(Visible = false)]
		protected virtual void viewDocument()
		{
			var noteId = Records.Current?.DocumentLink;
			if (noteId == null)
			{
				return;
			}

			var document = (APInvoice)
				SelectFrom<APInvoice>.
				Where<APInvoice.noteID.IsEqual<@P.AsGuid>>.
				View.ReadOnly.SelectSingleBound(this, null, noteId);
			if (document == null)
			{
				return;
			}

			var graph = CreateInstance<APInvoiceEntry>();
			graph.Document.Current = document;

			throw new PXRedirectRequiredException(graph, null);
		}

		[PXEntryScreenRights(typeof(APRecognizedInvoice), nameof(APInvoiceRecognitionEntry.ViewErrorHistory))]
		[PXButton(Category = ActionsMessages.Actions)]
		[PXUIField(DisplayName = "View History", Enabled = false)]
		public virtual void viewErrorHistory()
		{
			ErrorHistory.AskExt();
		}

		[PXEntryScreenRights(typeof(APRecognizedInvoice), nameof(APInvoiceRecognitionEntry.SearchVendor))]
		[PXButton(Category = ActionsMessages.Actions)]
		[PXUIField(DisplayName = "Search Vendor", Enabled = false)]
		protected virtual void searchVendor()
		{
			Records.SetProcessDelegate(SearchForVendor);
			Actions[_processButtonName].PressButton();
		}

		[PXButton]
		[PXUIField(DisplayName = "Upload Files", Visible = false)]
		public virtual IEnumerable uploadFiles(PXAdapter adapter)
		{
			var recognitionGraph = CreateInstance<APInvoiceRecognitionEntry>();
			var uploadFileGraph = CreateInstance<UploadFileMaintenance>();

			foreach (var pair in adapter.Arguments)
			{
				if (!(pair.Value is byte[] fileData))
				{
					continue;
				}

				var fileName = pair.Key;
				var uid = Guid.NewGuid();
				var name = $"{uid}//{fileName}";
				var fileInfo = new FileInfo(uid, name, null, fileData);

				if (!uploadFileGraph.SaveFile(fileInfo))
				{
					throw new PXException(Messages.FileCannotBeSaved, fileName);
				}

				var recognizedRecord = recognitionGraph.CreateRecognizedRecord(fileName, fileData, fileInfo.UID.Value);

				PXNoteAttribute.ForcePassThrow<RecognizedRecord.noteID>(recognitionGraph.RecognizedRecords.Cache);
				PXNoteAttribute.SetFileNotes(recognitionGraph.RecognizedRecords.Cache, recognizedRecord, fileInfo.UID.Value);

				recognitionGraph.RecognizedRecords.Cache.PersistUpdated(recognizedRecord);
			}

			return adapter.Get();
		}


		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXUIField(DisplayName = "Created Date")]
		protected virtual void _(Events.CacheAttached<RecognizedRecordForProcessing.createdDateTime> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[Owner]
		protected virtual void _(Events.CacheAttached<RecognizedRecordForProcessing.owner> e)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIField(DisplayName = "Document Link", Visible = true)]
		protected virtual void _(Events.CacheAttached<RecognizedRecordForProcessing.documentLink> e)
		{
		}

		protected virtual void _(Events.RowSelected<RecognizedRecordForProcessing> e)
		{
			if (e.Row == null)
			{
				return;
			}

			var enableViewErrorHistory = e.Row.Status == RecognizedRecordStatusListAttribute.Error;
			ViewErrorHistory.SetEnabled(enableViewErrorHistory);
		}

		protected virtual void _(Events.FieldSelecting<RecognizedRecordForProcessing.documentLink> e)
		{
			if (!(e.Row is RecognizedRecordForProcessing row) || row.DocumentLink == null)
			{
				return;
			}

			var document = (APRegister)
				SelectFrom<APRegister>.
				Where<APRegister.noteID.IsEqual<@P.AsGuid>>.
				View.ReadOnly.SelectSingleBound(this, null, row.DocumentLink);

			e.ReturnValue = document != null ?
				$"{document.DocType} {document.RefNbr}" :
				string.Empty;
		}

		public static void SearchForVendor(List<RecognizedRecordForProcessing> records)
		{
			var graph = CreateInstance<APInvoiceRecognitionEntry>();

			foreach (var rec in records)
			{
				PXProcessing.SetCurrentItem(rec);

				if (rec.Status != RecognizedRecordStatusListAttribute.Recognized)
				{
					PXProcessing.SetProcessed();

					continue;
				}

				var detail = (RecognizedRecordDetail)
					SelectFrom<RecognizedRecordDetail>.
					Where<RecognizedRecordDetail.entityType.IsEqual<@P.AsString>.And<
						  RecognizedRecordDetail.refNbr.IsEqual<@P.AsGuid>>>.
						  View.ReadOnly.Select(graph, rec.EntityType, rec.RefNbr);
				if (detail?.VendorID != null)
				{
					PXProcessing.SetProcessed();

					continue;
				}

				graph.PopulateVendorId(rec, detail).Wait();

				PXProcessing.SetProcessed();
			}
		}

		public static void Recognize(List<RecognizedRecordForProcessing> records)
		{
			var graph = CreateInstance<PXGraph>();
			var cache = graph.Caches[typeof(RecognizedRecordForProcessing)];
			var batch = new List<RecognizedRecordFileInfo>();

			foreach (var rec in records)
			{
				if (!APInvoiceRecognitionEntry.StatusValidForRecognitionSet.Contains(rec.Status))
				{
					PXProcessing.SetCurrentItem(rec);
					PXProcessing.SetProcessed();

					continue;
				}

				var files = APInvoiceRecognitionEntry.GetFilesToRecognize(cache, rec);
				if (files == null || files.Length == 0)
				{
					PXProcessing.SetCurrentItem(rec);
					PXProcessing.SetProcessed();

					continue;
				}

				var file = files[0];
				var fileInfo = new RecognizedRecordFileInfo(file.Name, file.Data, file.FileID.Value, rec);

				batch.Add(fileInfo);
			}

			if (batch.Count == 0)
			{
				return;
			}

			APInvoiceRecognitionEntry.RecognizeRecordsBatch(batch).GetAwaiter().GetResult();
		}
	}
}
