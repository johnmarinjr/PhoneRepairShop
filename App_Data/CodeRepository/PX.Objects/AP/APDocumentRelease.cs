using PX.Common;
using PX.Data;
using PX.Data.EP;
using PX.Data.BQL.Fluent;
using PX.Data.BQL;
using PX.Objects.AP.BQL;
using PX.Objects.AP.Overrides.APDocumentRelease;
using PX.Objects.AP.Standalone;
using PX.Objects.AR;
using PX.Objects.CA;
using PX.Objects.CM.Extensions;
using PX.Objects.Common;
using PX.Objects.Common.DataIntegrity;
using PX.Objects.Common.EntityInUse;
using PX.Objects.Common.Exceptions;
using PX.Objects.Common.Extensions;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.DR;
using PX.Objects.Extensions.MultiCurrency;
using PX.Objects.Extensions.MultiCurrency.AP;
using PX.Objects.GL;
using PX.Objects.GL.FinPeriods;
using PX.Objects.GL.FinPeriods.TableDefinition;
using PX.Objects.IN;
using PX.Objects.PM;
using PX.Objects.PO;
using PX.Objects.PO.LandedCosts;
using PX.Objects.TX;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Amount = PX.Objects.AR.ARReleaseProcess.Amount;
using PX.Data.BQL.Fluent;
using PX.Data.BQL;

namespace PX.Objects.AP
{
	[Serializable]
	[PXProjection(typeof(Select2<APRegister,
		LeftJoin<APInvoice,
			On<APInvoice.docType, Equal<APRegister.docType>,
				And<APInvoice.refNbr, Equal<APRegister.refNbr>>>,
		LeftJoin<APPayment,
			On<APPayment.docType, Equal<APRegister.docType>,
				And<APPayment.refNbr, Equal<APRegister.refNbr>>>>>>))]
	public partial class BalancedAPDocument : APRegister
	{
		#region InvoiceNbr
		public abstract class invoiceNbr : PX.Data.BQL.BqlString.Field<invoiceNbr> { }
		[PXDBString(40, IsUnicode = true, BqlField = typeof(APInvoice.invoiceNbr))]
		public virtual string InvoiceNbr { get; set; }
		#endregion
		#region ExtRefNbr
		public abstract class extRefNbr : PX.Data.BQL.BqlString.Field<extRefNbr> { }
		[PXDBString(40, IsUnicode = true, BqlField = typeof(APPayment.extRefNbr))]
		public virtual string ExtRefNbr { get; set; }
		#endregion
		#region VendorRefNbr
		public abstract class vendorRefNbr : PX.Data.BQL.BqlString.Field<vendorRefNbr> { }

		[PXString(40, IsUnicode = true)]
		[PXUIField(DisplayName = "Vendor Ref.")]
		[PXFormula(typeof(IsNull<BalancedAPDocument.invoiceNbr, BalancedAPDocument.extRefNbr>))]
		public string VendorRefNbr { get; set; }
		#endregion
		#region DocType
		public new abstract class docType : PX.Data.BQL.BqlString.Field<docType> { }
		[PXDBString(3, IsKey = true, IsFixed = true, BqlField = typeof(APRegister.docType))]
		[PXDefault]
		[APDocType.DocumentReleaseList]
		[PXUIField(DisplayName = "Type", Visibility = PXUIVisibility.SelectorVisible, Enabled = true, TabOrder = 0)]
		[PXFieldDescription]
		public override string DocType { get; set; }
		#endregion
		#region PrintCheck
		public abstract class printCheck : PX.Data.BQL.BqlBool.Field<printCheck> { }
		/// <summary>
		/// When set to <c>true</c> indicates that a check must be printed for the payment represented by this record.
		/// </summary>
		[PXDBBool(BqlField = typeof(APPayment.printCheck))]
		public virtual bool? PrintCheck
		{
			get;
			set;
		}
		#endregion
		#region CuryOrigDocAmt
		public new abstract class curyOrigDocAmt : PX.Data.BQL.BqlDecimal.Field<curyOrigDocAmt> { }
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXDBCurrency(typeof(APRegister.curyInfoID), typeof(APRegister.origDocAmt))]
		[PXUIField(DisplayName = "Currency Amount", Visibility = PXUIVisibility.SelectorVisible)]
		public override Decimal? CuryOrigDocAmt { get; set; }
		#endregion

		public new abstract class selected : PX.Data.BQL.BqlBool.Field<selected> { }
		public new abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }
        public new abstract class origModule : PX.Data.BQL.BqlString.Field<origModule> { }
        public new abstract class openDoc : PX.Data.BQL.BqlBool.Field<openDoc> { }
		public new abstract class released : PX.Data.BQL.BqlBool.Field<released> { }
		public new abstract class hold : PX.Data.BQL.BqlBool.Field<hold> { }
		public new abstract class scheduled : PX.Data.BQL.BqlBool.Field<scheduled> { }
		public new abstract class voided : PX.Data.BQL.BqlBool.Field<voided> { }
		public new abstract class printed : PX.Data.BQL.BqlBool.Field<printed> { }
		public new abstract class prebooked : PX.Data.BQL.BqlBool.Field<prebooked> { }
		public new abstract class approved : PX.Data.BQL.BqlBool.Field<approved> { }
		public new abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }
		public new abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }
		public new abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }
	}

	[Obsolete(Common.InternalMessages.ClassIsObsoleteRemoveInAcumatica2019R1)]
	public class PXMassProcessException : Common.PXMassProcessException
	{
		public PXMassProcessException(int ListIndex, Exception InnerException)
			: base(ListIndex, InnerException)
		{ }

		public PXMassProcessException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{ }
	}

	[PX.Objects.GL.TableAndChartDashboardType]
	public class APDocumentRelease : PXGraph<APDocumentRelease>
	{
		public PXCancel<BalancedAPDocument> Cancel;
		[PXFilterable]
		public PXProcessingJoin<
			BalancedAPDocument,
				LeftJoin<APInvoice,
					On<APInvoice.docType, Equal<BalancedAPDocument.docType>,
					And<APInvoice.refNbr, Equal<BalancedAPDocument.refNbr>>>,
				LeftJoin<APPayment,
					On<APPayment.docType, Equal<BalancedAPDocument.docType>,
					And<APPayment.refNbr, Equal<BalancedAPDocument.refNbr>>>,
				InnerJoinSingleTable<Vendor,
					On<Vendor.bAccountID, Equal<BalancedAPDocument.vendorID>>>>>,
				Where<Match<Vendor, Current<AccessInfo.userName>>>>
			APDocumentList;

		public static string[] TransClassesWithoutZeroPost = { GLTran.tranClass.Discount, GLTran.tranClass.RealizedAndRoundingGOL };

		public APDocumentRelease()
		{
			APSetup setup = APSetup.Current;
			APDocumentList.SetProcessDelegate(
				delegate (List<BalancedAPDocument> list)
				{
					List<APRegister> newlist = new List<APRegister>(list.Count);
					foreach (BalancedAPDocument doc in list)
					{
						newlist.Add(doc);
					}
					ReleaseDoc(newlist, true);
				}
			);
			APDocumentList.SetProcessCaption(ActionsMessages.Release);
			APDocumentList.SetProcessAllCaption(ActionsMessages.ReleaseAll);
			//APDocumentList.SetProcessAllVisible(false);
			PXNoteAttribute.ForcePassThrow<BalancedAPDocument.noteID>(APDocumentList.Cache);
		}

		public PXAction<BalancedAPDocument> ViewDocument;
		[PXUIField(DisplayName = "", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable viewDocument(PXAdapter adapter)
		{
			if (this.APDocumentList.Current != null)
			{
				PXRedirectHelper.TryRedirect(APDocumentList.Cache, APDocumentList.Current, "Document", PXRedirectHelper.WindowMode.NewWindow);
			}
			return adapter.Get();
		}

		public static void ReleaseDoc(List<APRegister> list, bool isMassProcess)
		{
			ReleaseDoc(list, isMassProcess, false);
		}

        public static void ReleaseDoc(List<APRegister> list, bool isMassProcess, List<Batch> externalPostList)
        {
            ReleaseDoc(list, isMassProcess, false, externalPostList);
        }
		public static void ReleaseDoc(List<APRegister> list, bool isMassProcess, bool isPrebooking)
		{
			ReleaseDoc(list, isMassProcess, isPrebooking, null);
		}

		/// <summary>
		/// Static function for release of AP documents and posting of the released batch.
		/// Released batches will be posted if the corresponded flag in APSetup is set to true.
		/// SkipPost parameter is used to override this flag.
		/// This function can not be called from inside of the covering DB transaction scope, unless skipPost is set to true.
		/// </summary>
		/// <param name="list">List of the documents to be released</param>
		/// <param name="isMassProcess">Flag specifing if the function is called from mass process - affects error handling</param>
		/// <param name="skipPost"> Prevent Posting of the released batch(es). This parameter must be set to true if this function is called from "covering" DB transaction</param>
		public static void ReleaseDoc(List<APRegister> list, bool isMassProcess, bool isPrebooking, List<Batch> externalPostList)
		{
			bool failed = false;
			bool skipPost = (externalPostList != null);
			APReleaseProcess rg = PXGraph.CreateInstance<APReleaseProcess>();
			JournalEntry je = CreateJournalEntry();

			PostGraph pg = PXGraph.CreateInstance<PostGraph>();
			Dictionary<int, int> batchbind = new Dictionary<int, int>();
			bool ppvProcessFailed = false;
			bool taxItemCostProcessFailed = false;

			for (int i = 0; i < list.Count; i++)
			{
				APRegister doc = list[i];
				List<INRegister> inDocs = null;

				if (doc == null)
				{
					continue;
				}

				try
				{
					rg.Clear();
					rg.VerifyStockItemLineHasReceipt(doc);
					rg.VerifyInterBranchTransactions(doc);

					if (doc.Passed == true)
					{
						rg.TimeStamp = doc.tstamp;
					}

					doc = rg.OnBeforeRelease(doc);

					doc.ReleasedToVerify = (doc.Status == ARDocStatus.Open && doc.OpenDoc == true && doc.Released == true) ? (bool?)null : false;
					List<APRegister> childs = rg.ReleaseDocProc(je, doc, isPrebooking, out inDocs);

					int k;

					if ((k = je.created.IndexOf(je.BatchModule.Current)) >= 0 && batchbind.ContainsKey(k) == false)
					{
						batchbind.Add(k, i);
					}

					if (childs != null)
					{
						for (int j = 0; j < childs.Count; j++)
						{
							var isSelfAppliedAdr = childs[j].DocType == APDocType.DebitAdj && childs[j].DocType == doc.DocType && childs[j].RefNbr == doc.RefNbr;
							var isOpenDoc = childs[j].Status == ARDocStatus.Open && childs[j].OpenDoc == true && childs[j].Released == true;
							childs[j].ReleasedToVerify = (isSelfAppliedAdr || isOpenDoc) ? (bool?)null : false;

							doc = childs[j];
							rg.Clear();
							rg.ReleaseDocProc(je, doc, isPrebooking, out List<INRegister> childINDocs);

							if (childINDocs?.Count > 0)
							{
								if (inDocs == null)
									inDocs = new List<INRegister>(childINDocs);
								else
									inDocs.Add(childINDocs);
							}
						}
					}

					if (string.IsNullOrEmpty(doc.WarningMessage))
						PXProcessing<APRegister>.SetInfo(i, ActionsMessages.RecordProcessed);
					else
					{
						PXProcessing<APRegister>.SetWarning(i, doc.WarningMessage);
					}
				}
				catch (Exception e)
				{
					je.Clear();
                    je.CleanupCreated(batchbind.Keys);

					if (isMassProcess)
					{
						PXProcessing<APRegister>.SetError(i, e);
						failed = true;
					}
					else
					{
						throw new PXMassProcessException(i, e);
					}
				}

				try
				{
					var taxAdjDocs = inDocs?.Where(d => d.IsTaxAdjustmentTran == true).ToList();
					if (taxAdjDocs?.Count > 0 && rg.posetup?.AutoReleaseIN == true)
					{
						INDocumentRelease.ReleaseDoc(taxAdjDocs, false);
					}
				}
				catch (Exception e)
				{
					taxItemCostProcessFailed = true;

					if (isMassProcess)
						PXProcessing<APRegister>.SetError(i, e);
					else
						PXTrace.WriteError(e);
				}

				try
				{
					var nonTaxAdjDocs = inDocs?.Where(d => d.IsTaxAdjustmentTran != true).ToList();
					if (nonTaxAdjDocs?.Count > 0 && rg.posetup?.AutoReleaseIN == true)
					{
						INDocumentRelease.ReleaseDoc(nonTaxAdjDocs, false);
					}
				}
				catch (Exception e)
				{
					ppvProcessFailed = true;

					if (isMassProcess)
						PXProcessing<APRegister>.SetError(i, e);
					else
						PXTrace.WriteError(e);
				}
			}

			if (skipPost)
			{
				if (rg.AutoPost)
					externalPostList.AddRange(je.created);
			}
			else
			{
				for (int i = 0; i < je.created.Count; i++)
				{
					Batch batch = je.created[i];
					try
					{
						if (rg.AutoPost)
						{
							pg.Clear();
							pg.PostBatchProc(batch);
						}
					}
					catch (Exception e)
					{
						if (isMassProcess)
						{
							failed = true;
							PXProcessing<APRegister>.SetError(batchbind[i], e);
						}
						else
						{
							throw new PXMassProcessException(batchbind[i], e);
						}
					}
				}
			}
			if (failed || ppvProcessFailed || taxItemCostProcessFailed)
			{
				//It is necessary that the platform did not set a general error message to the Item
				PXProcessing<APPayment>.SetCurrentItem(null);
			}

			if (failed)
				throw new PXException(GL.Messages.DocumentsNotReleased);
			else if (ppvProcessFailed)
				throw new PXException(Messages.ProcessingOfPPVTransactionForAPDocFailed);
			else if (taxItemCostProcessFailed)
				throw new PXException(Messages.ProcessingOfTaxAdjustmentTransactionForAPDocFailed);
		}

		public static JournalEntry CreateJournalEntry()
		{
			JournalEntry je = PXGraph.CreateInstance<JournalEntry>();
			je.PrepareForDocumentRelease();
			je.RowInserting.AddHandler<GLTran>((sender, e) => { je.SetZeroPostIfUndefined((GLTran)e.Row, TransClassesWithoutZeroPost); });
			return je;
		}

		public static void VoidDoc(List<APRegister> list)
		{
			bool failed = false;
			APReleaseProcess rg = PXGraph.CreateInstance<APReleaseProcess>();
			JournalEntry je = CreateJournalEntry();

			PostGraph pg = PXGraph.CreateInstance<PostGraph>();
			Dictionary<int, int> batchbind = new Dictionary<int, int>();
			for (int i = 0; i < list.Count; i++)
			{
				APRegister doc = list[i];
				if (doc == null)
				{
					continue;
				}
				try
				{
					rg.Clear();
					if (doc.Passed == true)
					{
						rg.TimeStamp = doc.tstamp;
					}
					rg.VoidDocProc(je, doc);
					PXProcessing<APRegister>.SetInfo(i, ActionsMessages.RecordProcessed);
					int k;
					if ((k = je.created.IndexOf(je.BatchModule.Current)) >= 0 && batchbind.ContainsKey(k) == false)
					{
						batchbind.Add(k, i);
					}
				}
				catch (Exception e)
				{
					throw new PXMassProcessException(i, e);
				}
			}

			for (int i = 0; i < je.created.Count; i++)
			{
				Batch batch = je.created[i];
				try
				{
					if (rg.AutoPost)
					{
						pg.Clear();
						pg.PostBatchProc(batch);
					}
				}
				catch (Exception e)
				{
					throw new PXMassProcessException(batchbind[i], e);
				}
			}
			if (failed)
			{
				throw new PXException(GL.Messages.DocumentsNotReleased);
			}
		}

		protected virtual IEnumerable apdocumentlist()
		{
			PXResultset<BalancedAPDocument, APInvoice, APPayment, Vendor, APAdjust> ret = new PXResultset<BalancedAPDocument, APInvoice, APPayment, Vendor, APAdjust>();

			PXSelectBase<BalancedAPDocument> cmd = new PXSelectJoinGroupBy<
				BalancedAPDocument,
				InnerJoinSingleTable<Vendor, On<Vendor.bAccountID, Equal<BalancedAPDocument.vendorID>>,
				LeftJoin<APAdjust, On<APAdjust.adjgDocType, Equal<BalancedAPDocument.docType>,
					And<APAdjust.adjgRefNbr, Equal<BalancedAPDocument.refNbr>,
					And<APAdjust.released, NotEqual<True>,
					And<APAdjust.hold, Equal<boolFalse>>>>>,
				LeftJoin<APInvoice, On<APInvoice.docType, Equal<BalancedAPDocument.docType>,
					And<APInvoice.refNbr, Equal<BalancedAPDocument.refNbr>>>,
				LeftJoin<APPayment, On<APPayment.docType, Equal<BalancedAPDocument.docType>,
					And<APPayment.refNbr, Equal<BalancedAPDocument.refNbr>>>>>>>,
					Where2<Match<Vendor, Current<AccessInfo.userName>>,
							 And<APRegister.hold, Equal<boolFalse>,
							 And<APRegister.voided, Equal<boolFalse>,
							 And<APRegister.scheduled, Equal<boolFalse>,
						And<APRegister.approved, Equal<boolTrue>,
							 And<APRegister.docType, NotEqual<APDocType.check>,
							 And<APRegister.docType, NotEqual<APDocType.quickCheck>,
							 And2<Where<APInvoice.refNbr, IsNotNull, Or<APPayment.refNbr, IsNotNull>>,
						And2<Where<
							BalancedAPDocument.released, Equal<boolFalse>,
							Or<
								BalancedAPDocument.openDoc, Equal<boolTrue>,
								And<APAdjust.adjdRefNbr, IsNotNull,
								And<APAdjust.isInitialApplication, NotEqual<True>>>>>,
						And<APRegister.isMigratedRecord, Equal<Current<APSetup.migrationMode>>,
						And<Where<BalancedAPDocument.docType, NotEqual<APDocType.prepayment>,
							Or<BalancedAPDocument.printed, Equal<True>,
							Or<BalancedAPDocument.printCheck, IsNull,
							Or<BalancedAPDocument.printCheck, NotEqual<True>>>>>>>>>>>>>>>>,
					Aggregate<
					GroupBy<BalancedAPDocument.docType,
					GroupBy<BalancedAPDocument.refNbr,
					GroupBy<BalancedAPDocument.released,
					GroupBy<BalancedAPDocument.prebooked,
					GroupBy<BalancedAPDocument.openDoc,
					GroupBy<BalancedAPDocument.hold,
					GroupBy<BalancedAPDocument.scheduled,
					GroupBy<BalancedAPDocument.voided,
					GroupBy<BalancedAPDocument.printed,
					GroupBy<BalancedAPDocument.approved,
					GroupBy<BalancedAPDocument.noteID,
					GroupBy<BalancedAPDocument.createdByID,
						GroupBy<BalancedAPDocument.lastModifiedByID>>>>>>>>>>>>>>,
					OrderBy<Asc<BalancedAPDocument.docType,
							Asc<BalancedAPDocument.refNbr>>>>(this);

			int startRow = PXView.StartRow;
			int totalRows = 0;

			List<PXView.PXSearchColumn> searchColumns = APDocumentList.View.GetContextualExternalSearchColumns();
			foreach (PXResult<BalancedAPDocument, Vendor, APAdjust, APInvoice, APPayment> res in
					cmd.View.Select(null, null,
									searchColumns.GetSearches(),
									searchColumns.GetSortColumns(),
									searchColumns.GetDescendings(),
									APDocumentList.View.GetExternalFilters(),
									ref startRow,
									PXView.MaximumRows,
									ref totalRows))
			{
				BalancedAPDocument apdoc = (BalancedAPDocument)res;
				apdoc = APDocumentList.Locate(apdoc) ?? apdoc;

				APAdjust adj = (APAdjust)res;
					if (adj.AdjdRefNbr != null)
					{
						apdoc.DocDate = adj.AdjgDocDate;
					    FinPeriodIDAttribute.SetPeriodsByMaster<APRegister.finPeriodID>(APDocumentList.Cache, apdoc, adj.AdjgTranPeriodID);
					}
					ret.Add(new PXResult<BalancedAPDocument, APInvoice, APPayment, Vendor, APAdjust>(apdoc, res, res, res, res));
				}

			PXView.StartRow = 0;

			return ret;
		}

		public PXSetup<APSetup> APSetup;
	}

	public class APPayment_CurrencyInfo_Currency_Vendor : PXSelectJoin<APPayment,
		InnerJoin<CurrencyInfo, On<CurrencyInfo.curyInfoID, Equal<APPayment.curyInfoID>>,
		InnerJoin<Currency, On<Currency.curyID, Equal<CurrencyInfo.curyID>>,
		LeftJoin<Vendor, On<Vendor.bAccountID, Equal<APPayment.vendorID>>,
		LeftJoin<CashAccount, On<CashAccount.cashAccountID, Equal<APPayment.cashAccountID>>>>>>,
		Where<APPayment.docType, Equal<Required<APPayment.docType>>,
			And<APPayment.refNbr, Equal<Required<APPayment.refNbr>>>>>
	{
		public APPayment_CurrencyInfo_Currency_Vendor(PXGraph graph)
			: base(graph)
		{
		}
	}

	public class APInvoice_CurrencyInfo_Terms_Vendor : PXSelectJoin<APInvoice, InnerJoin<CurrencyInfo, On<CurrencyInfo.curyInfoID, Equal<APInvoice.curyInfoID>>, LeftJoin<Terms, On<Terms.termsID, Equal<APInvoice.termsID>>, LeftJoin<Vendor, On<Vendor.bAccountID, Equal<APInvoice.vendorID>>>>>, Where<APInvoice.docType, Equal<Required<APInvoice.docType>>, And<APInvoice.refNbr, Equal<Required<APInvoice.refNbr>>>>>
	{
		public APInvoice_CurrencyInfo_Terms_Vendor(PXGraph graph)
			: base(graph)
		{
		}
	}

	[PXHidden]
	public class APReleaseProcess : PXGraph<APReleaseProcess>
	{
		public class MultiCurrency : APMultiCurrencyGraph<APReleaseProcess, APRegister>
		{
			protected override string DocumentStatus => Base.APDocument.Current?.Status;

			protected override CurySource CurrentSourceSelect() => null;

			protected override DocumentMapping GetDocumentMapping()
			{
				return new DocumentMapping(typeof(APRegister))
				{
					DocumentDate = typeof(APRegister.docDate),
					BAccountID = typeof(APRegister.vendorID)
				};
			}

			protected override IEnumerable<Type> FieldWhichShouldBeRecalculatedAnyway
			{
				get
				{
					yield return typeof(APInvoice.curyDiscBal);
				}
			}

			protected override PXSelectBase[] GetChildren()
			{
				return new PXSelectBase[]
				{
					Base.APTaxTran_TranType_RefNbr,
					Base.APInvoice_DocType_RefNbr,
					Base.APPayment_DocType_RefNbr,
					Base.APDocument,
				};
			}

			public void UpdateCurrencyInfoForPrepayment(APPayment prepayment, CurrencyInfo origCuryInfoToUse)
			{
				TrackedItems[Base.APPayment_DocType_RefNbr.Cache.GetItemType()]
					.Single(f => f.CuryName.Equals(nameof(APPayment.curyDocBal), StringComparison.OrdinalIgnoreCase))
					.BaseCalc = true;

				CurrencyInfo curyInfoToUse = PXCache<CurrencyInfo>.CreateCopy(origCuryInfoToUse);
				curyInfoToUse.CuryInfoID = prepayment.CuryInfoID;
				curyInfoToUse.IsReadOnly = false;
				currencyinfo.Cache.Update(curyInfoToUse);
			}
		}

		public PXSelect<APRegister> APDocument;

		public PXSelectJoin<
			APTran,
				LeftJoin<APTax,
					On<APTax.tranType, Equal<APTran.tranType>,
					And<APTax.refNbr, Equal<APTran.refNbr>,
					And<APTax.lineNbr, Equal<APTran.lineNbr>>>>,
				LeftJoin<Tax,
					On<Tax.taxID, Equal<APTax.taxID>>,
				LeftJoin<DRDeferredCode,
					On<DRDeferredCode.deferredCodeID, Equal<APTran.deferredCode>>,
				LeftJoin<LandedCostCode,
					On<LandedCostCode.landedCostCodeID, Equal<APTran.landedCostCodeID>>,
				LeftJoin<InventoryItem,
					On<InventoryItem.inventoryID, Equal<APTran.inventoryID>>,
				LeftJoin<APTaxTran,
					On<APTaxTran.module, Equal<BatchModule.moduleAP>,
					And<APTaxTran.tranType, Equal<APTax.tranType>,
					And<APTaxTran.refNbr, Equal<APTax.refNbr>,
					And<APTaxTran.taxID, Equal<APTax.taxID>>>>>>>>>>>,
			Where<
				APTran.tranType, Equal<Required<APTran.tranType>>,
				And<APTran.refNbr, Equal<Required<APTran.refNbr>>>>,
			OrderBy<
				Asc<APTran.lineNbr,
				Asc<Tax.taxCalcLevel,
				Asc<Tax.taxType>>>>>
			APTran_TranType_RefNbr;

		public PXSelectJoin<
			APTaxTran,
			InnerJoin<Tax,
				On<Tax.taxID, Equal<APTaxTran.taxID>>,
			LeftJoin<APInvoice,
				On<APInvoice.docType,
				Equal<APTaxTran.origTranType>,
				And<APInvoice.refNbr, Equal<APTaxTran.origRefNbr>>>>>,
			Where<APTaxTran.module,
				Equal<BatchModule.moduleAP>,
				And<APTaxTran.tranType, Equal<Required<APTaxTran.tranType>>,
				And<APTaxTran.refNbr, Equal<Required<APTaxTran.refNbr>>>>>,
			OrderBy<
				Asc<Tax.taxCalcLevel>>>
			APTaxTran_TranType_RefNbr;
		public PXSelect<SVATConversionHist> SVATConversionHistory;
        public PXSelect<Batch> Batch;

		public APInvoice_CurrencyInfo_Terms_Vendor APInvoice_DocType_RefNbr;
		public APPayment_CurrencyInfo_Currency_Vendor APPayment_DocType_RefNbr;

		public PXSelectJoin<
			APAdjust,
				InnerJoin<CurrencyInfo,
					On<CurrencyInfo.curyInfoID, Equal<APAdjust.adjdCuryInfoID>>,
				InnerJoin<Currency,
					On<Currency.curyID, Equal<CurrencyInfo.curyID>>,
				LeftJoinSingleTable<APInvoice,
					On<APInvoice.docType, Equal<APAdjust.adjdDocType>,
					And<APInvoice.refNbr, Equal<APAdjust.adjdRefNbr>>>,
				LeftJoinSingleTable<APPayment,
					On<APPayment.docType, Equal<APAdjust.adjdDocType>,
					And<APPayment.refNbr, Equal<APAdjust.adjdRefNbr>>>,
				InnerJoin<Standalone.APRegisterAlias,
					On<Standalone.APRegisterAlias.docType, Equal<APAdjust.adjdDocType>,
					And<Standalone.APRegisterAlias.refNbr, Equal<APAdjust.adjdRefNbr>>>,
				LeftJoin<APTran, On<Standalone.APRegisterAlias.paymentsByLinesAllowed, Equal<True>,
					And<APTran.tranType, Equal<APAdjust.adjdDocType>,
					And<APTran.refNbr, Equal<APAdjust.adjdRefNbr>,
					And<APTran.lineNbr, Equal<APAdjust.adjdLineNbr>>>>>>>>>>>,
			Where<
				APAdjust.adjgDocType, Equal<Required<APAdjust.adjgDocType>>,
				And<APAdjust.adjgRefNbr, Equal<Required<APAdjust.adjgRefNbr>>,
				And<Where<
					Switch<
						Case<Where<Required<APAdjust.released>, Equal<True>>,
							IIf<Where<APAdjust.adjNbr, Equal<Required<APAdjust.adjNbr>>>, True, False>>,
						IIf<Where<APAdjust.released, NotEqual<True>>, True, False>>, Equal<True>
					>>>>,
			OrderBy<
				Asc<APAdjust.adjgDocType,
				Asc<APAdjust.adjgRefNbr,
				Asc<APAdjust.adjdDocType,
				Asc<APAdjust.adjdRefNbr,
				Asc<APAdjust.adjNbr>>>>>>>
			APAdjust_AdjgDocType_RefNbr_VendorID;

		public PXSelect<APPaymentChargeTran, Where<APPaymentChargeTran.docType, Equal<Required<APPaymentChargeTran.docType>>, And<APPaymentChargeTran.refNbr, Equal<Required<APPaymentChargeTran.refNbr>>>>> APPaymentChargeTran_DocType_RefNbr;

		public PXSelect<APTran, Where<APTran.tranType, Equal<Required<APTran.tranType>>, And<APTran.refNbr, Equal<Required<APTran.refNbr>>, And<APTran.box1099, IsNotNull>>>> AP1099Tran_Select;
		public PXSelect<AP1099Hist> AP1099History_Select;
		public PXSelect<AP1099Yr> AP1099Year_Select;

		public PXSelectJoin<
			APTaxTran,
			InnerJoin<Tax,
				On<Tax.taxID, Equal<APTaxTran.taxID>>>,
			Where<APTaxTran.module, Equal<BatchModule.moduleAP>,
				And<APTaxTran.tranType, Equal<Required<APTaxTran.tranType>>,
				And<APTaxTran.refNbr, Equal<Required<APTaxTran.refNbr>>,
				And<Tax.taxType, Equal<CSTaxType.withholding>>>>>>
			WHTax_TranType_RefNbr;

		public PXSelect<APTranPost,
			Where<APTranPost.docType, Equal<Required<APRegister.docType>>,
				And<APTranPost.refNbr, Equal<Required<APRegister.refNbr>>>>> TranPost;

		public PXSelect<CATran> CashTran;
		public PXSelect<CurrencyInfo, Where<CurrencyInfo.curyInfoID, Equal<Required<CurrencyInfo.curyInfoID>>>> CurrencyInfo_CuryInfoID;

		public PXSetup<GLSetup> glsetup;
		public PXSelect<Tax> taxes;

		private APSetup _apsetup;

		public APSetup apsetup
		{
			get
			{
				return _apsetup ?? (_apsetup = PXSelect<APSetup>.Select(this));
				}
				}

		private POSetup _posetup;

		public POSetup posetup
		{
			get
			{
				return _posetup ?? (_posetup = PXSelect<POSetup>.Select(this));
			}
		}

		public bool AutoPost => apsetup.AutoPost == true;

		public bool SummPost => apsetup.TransactionPosting == AccountPostOption.Summary;

		public string InvoiceRounding => apsetup.InvoiceRounding;

		public decimal? InvoicePrecision => apsetup.InvoicePrecision;

		public bool? IsMigrationMode => apsetup.MigrationMode;

		public bool IsMigratedDocumentForProcessing(APRegister doc)
        {
			// QuickCheck and VoidQuickCheck documents
			// will be processed the same way as for normal mode,
			// but GL transactions will not be created.
			//
			bool isQuickCheckOrVoidQuickCheckDocument = doc.DocType == APDocType.QuickCheck ||
				doc.DocType == APDocType.VoidQuickCheck;

			return
				doc.IsMigratedRecord == true &&
				doc.Released != true &&
				doc.CuryInitDocBal != doc.CuryOrigDocAmt &&
				!isQuickCheckOrVoidQuickCheckDocument;
        }

		public bool? RequireControlTaxTotal =>
			apsetup.RequireControlTaxTotal == true
			&& PXAccess.FeatureInstalled<FeaturesSet.netGrossEntryMode>();


		protected APInvoiceEntry _ie;
		public APInvoiceEntry InvoiceEntryGraph
		{
			get { return _ie ?? (_ie = CreateInstance<APInvoiceEntry>()); }
		}

		[InjectDependency]
		public IFinPeriodRepository FinPeriodRepository { get; set; }

		[InjectDependency]
		public IFinPeriodUtils FinPeriodUtils { get; set; }

		#region Cache Attached

		/// <summary>
		/// The formula that calculates <see cref="APPayment.CuryApplAmt"/> needs to be removed
		/// to prevent premature updates of the documents by the <see cref="PXUnboundFormulaAttribute"/>
		/// upon applications' delete, thus avoiding the lock violation exceptions. This does no harm
		/// as the application amounts are not visible in the context of release process, the <see
		/// cref="APPayment.CuryApplAmt"/> is neither DB-bound or visible during release.
		/// </summary>
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRemoveBaseAttribute(typeof(PXUnboundFormulaAttribute))]
		protected virtual void APAdjust_CuryAdjgAmt_CacheAttached(PXCache sender) { }

		[PXDBString(1, IsFixed = true)]
		public virtual void Tax_TaxType_CacheAttached(PXCache sender) { }

		[PXDBString(1, IsFixed = true)]
		public virtual void Tax_TaxCalcLevel_CacheAttached(PXCache sender) { }
		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXDBInt]
		protected virtual void APTranPost_AccountID_CacheAttached(PXCache sender) { }
		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXDBInt]
		protected virtual void APTranPost_SubID_CacheAttached(PXCache sender) { }
		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXDBInt]
		protected virtual void APTranPost_VendorID_CacheAttached(PXCache sender) { }
		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXDBInt]
		protected virtual void APTranPost_BranchID_CacheAttached(PXCache sender) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXParent(typeof(POOrderPrepayment.FK.Order))]
		protected virtual void _(Events.CacheAttached<POOrderPrepayment.orderNbr> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRemoveBaseAttribute(typeof(PXDefaultAttribute))]
		[CurrencyInfo(typeof(POOrder.curyInfoID) )]
		protected virtual void _(Events.CacheAttached<POOrderPrepayment.curyInfoID> e) { }

		#endregion

		public APReleaseProcess()
		{
			OpenPeriodAttribute.SetValidatePeriod<APRegister.finPeriodID>(APDocument.Cache, null, PeriodValidation.Nothing);
			OpenPeriodAttribute.SetValidatePeriod<APPayment.adjFinPeriodID>(APPayment_DocType_RefNbr.Cache, null, PeriodValidation.Nothing);

			PXCache cacheAPAdjust = Caches[typeof(APAdjust)];

		    cacheAPAdjust.Adjust<FinPeriodIDAttribute>()
		        .For<APAdjust.adjgFinPeriodID>(attr =>
		        {
		            attr.AutoCalculateMasterPeriod = false;
		            attr.CalculatePeriodByHeader = false;
		            attr.HeaderFindingMode = FinPeriodIDAttribute.HeaderFindingModes.Parent;
		        })
		        .SameFor<APAdjust.adjdFinPeriodID>();
			PXDBDefaultAttribute.SetDefaultForUpdate<APAdjust.vendorID>(cacheAPAdjust, null, false);
			PXDBDefaultAttribute.SetDefaultForUpdate<APAdjust.adjgDocType>(cacheAPAdjust, null, false);
			PXDBDefaultAttribute.SetDefaultForUpdate<APAdjust.adjgRefNbr>(cacheAPAdjust, null, false);
			PXDBDefaultAttribute.SetDefaultForUpdate<APAdjust.adjgCuryInfoID>(cacheAPAdjust, null, false);
			PXDBDefaultAttribute.SetDefaultForUpdate<APAdjust.adjgDocDate>(cacheAPAdjust, null, false);

			PXCache cacheAPTran = Caches[typeof(APTran)];

		    cacheAPTran.Adjust<FinPeriodIDAttribute>()
		        .For<APTran.finPeriodID>(attr =>
		        {
		            attr.HeaderFindingMode = FinPeriodIDAttribute.HeaderFindingModes.Parent;
		        });
            PXDBDefaultAttribute.SetDefaultForUpdate<APTran.tranType>(cacheAPTran, null, false);
			PXDBDefaultAttribute.SetDefaultForUpdate<APTran.refNbr>(cacheAPTran, null, false);
			PXDBDefaultAttribute.SetDefaultForUpdate<APTran.curyInfoID>(cacheAPTran, null, false);
			PXDBDefaultAttribute.SetDefaultForUpdate<APTran.tranDate>(cacheAPTran, null, false);
            PXDBDefaultAttribute.SetDefaultForUpdate<APTran.vendorID>(cacheAPTran, null, false);

			PXCache cacheAPTaxTran = Caches[typeof(APTaxTran)];

		    cacheAPTaxTran.Adjust<FinPeriodIDAttribute>()
		        .For<APTaxTran.finPeriodID>(attr =>
		        {
		            attr.HeaderFindingMode = FinPeriodIDAttribute.HeaderFindingModes.Parent;
		        });
			PXDBDefaultAttribute.SetDefaultForInsert<APTaxTran.tranType>(cacheAPTaxTran, null, false);
			PXDBDefaultAttribute.SetDefaultForInsert<APTaxTran.refNbr>(cacheAPTaxTran, null, false);
			PXDBDefaultAttribute.SetDefaultForInsert<APTaxTran.curyInfoID>(cacheAPTaxTran, null, false);
			PXDBDefaultAttribute.SetDefaultForInsert<APTaxTran.tranDate>(cacheAPTaxTran, null, false);
			PXDBDefaultAttribute.SetDefaultForInsert<APTaxTran.taxZoneID>(cacheAPTaxTran, null, false);

			PXDBDefaultAttribute.SetDefaultForUpdate<APTaxTran.tranType>(cacheAPTaxTran, null, false);
			PXDBDefaultAttribute.SetDefaultForUpdate<APTaxTran.refNbr>(cacheAPTaxTran, null, false);
			PXDBDefaultAttribute.SetDefaultForUpdate<APTaxTran.curyInfoID>(cacheAPTaxTran, null, false);
			PXDBDefaultAttribute.SetDefaultForUpdate<APTaxTran.tranDate>(cacheAPTaxTran, null, false);
			PXDBDefaultAttribute.SetDefaultForUpdate<APTaxTran.taxZoneID>(cacheAPTaxTran, null, false);

			if (IsMigrationMode == true)
			{
				PXDBDefaultAttribute.SetDefaultForInsert<APAdjust.vendorID>(cacheAPAdjust, null, false);
				PXDBDefaultAttribute.SetDefaultForInsert<APAdjust.adjgDocDate>(cacheAPAdjust, null, false);
			}
		}

        protected virtual void APPayment_CashAccountID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
        {
            e.Cancel = true;
        }

        protected virtual void APPayment_PaymentMethodID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
        {
            e.Cancel = true;
        }

        protected virtual void APPayment_ExtRefNbr_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
        {
            e.Cancel = true;
        }

		protected virtual void APPayment_FinPeriodID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)//Pank PID ?
		{
			e.Cancel = true;
		}

        protected virtual void APRegister_FinPeriodID_CommandPreparing(PXCache sender, PXCommandPreparingEventArgs e)//Pank PID ?
		{
			if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Update)
			{
			    e.ExcludeFromInsertUpdate();
			}
		}

		protected virtual void APRegister_TranPeriodID_CommandPreparing(PXCache sender, PXCommandPreparingEventArgs e)//Pank PID ?
		{
			if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Update)
			{
			    e.ExcludeFromInsertUpdate();
			}
		}

		protected virtual void APRegister_DocDate_CommandPreparing(PXCache sender, PXCommandPreparingEventArgs e)
		{
			if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Update)
			{
			    e.ExcludeFromInsertUpdate();
			}
		}

		protected virtual void APPayment_FinPeriodID_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)//Pank PID ?
		{
			e.Cancel = true;
		}

		protected virtual void CATran_ReferenceID_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			e.Cancel = true;
		}

		protected virtual void APAdjust_AdjdRefNbr_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			e.Cancel = true;
		}

		protected virtual void APTran_RowUpdating(PXCache sender, PXRowUpdatingEventArgs e)
		{
		}

		protected void _(Events.RowPersisting<APTranPost> e)
		{
			if (e.Operation == PXDBOperation.Insert &&
			    e.Row.Type == APTranPost.type.RGOL &&
			    e.Row.CuryAmt == 0 && e.Row.Amt == 0 &&
			    e.Row.RGOLAmt == 0 && e.Row.WhTaxAmt == 0 &&
			    e.Row.PPDAmt == 0 && e.Row.DiscAmt == 0)
			{
				//Suppress inserting zero RGOL & Rounding transactions
				e.Cancel = true;
			}
		}
		private APHist CreateHistory(int? BranchID, int? AccountID, int? SubID, int? VendorID, string PeriodID)
		{
			APHist accthist = new APHist();
			accthist.BranchID = BranchID;
			accthist.AccountID = AccountID;
			accthist.SubID = SubID;
			accthist.VendorID = VendorID;
			accthist.FinPeriodID = PeriodID;
			return (APHist)Caches[typeof(APHist)].Insert(accthist);
		}

		private CuryAPHist CreateHistory(int? BranchID, int? AccountID, int? SubID, int? VendorID, string CuryID, string PeriodID)
		{
			CuryAPHist accthist = new CuryAPHist();
			accthist.BranchID = BranchID;
			accthist.AccountID = AccountID;
			accthist.SubID = SubID;
			accthist.VendorID = VendorID;
			accthist.CuryID = CuryID;
			accthist.FinPeriodID = PeriodID;
			return (CuryAPHist)Caches[typeof(CuryAPHist)].Insert(accthist);
		}

		private class APHistBucket
		{
			public int? apAccountID = null;
			public int? apSubID = null;
			public decimal SignPayments = 0m;
			public decimal SignDeposits = 0m;
			public decimal SignPurchases = 0m;
			public decimal SignDrAdjustments = 0m;
			public decimal SignCrAdjustments = 0m;
			public decimal SignDiscTaken = 0m;
			public decimal SignWhTax = 0m;
			public decimal SignRGOL = 0m;
			public decimal SignPtd = 0m;
			public decimal SignRetainageWithheld = 0m;
			public decimal SignRetainageReleased = 0m;

			public APHistBucket(GLTran tran, string TranType)
			{
				apAccountID = tran.AccountID;
				apSubID = tran.SubID;

				switch (TranType + tran.TranClass)
				{
					case "QCKN":
						SignPurchases = 1m;
						SignPayments = 1m;
						SignPtd = 0m;
						break;
					case "VQCN":
						SignPurchases = 1m;
						SignPayments = 1m;
						SignPtd = 0m;
						break;
					case "INVN":
						SignPurchases = -1m;
						SignPtd = -1m;
						break;
					case "INVE":
						apAccountID = tran.OrigAccountID;
						apSubID = tran.OrigSubID;
						SignRetainageWithheld = -1m;
						break;
					case "INVF":
						SignPurchases = -1m;
						SignRetainageReleased = -1m;
						SignPtd = -1m;
						break;
					case "ACRN":
						SignCrAdjustments = -1m;
						SignPtd = -1m;
						break;
					case "ADRP":
					case "ADRN":
						SignDrAdjustments = 1m;
						SignPtd = -1m;
						break;
					case "ADRR":
						apAccountID = tran.OrigAccountID;
						apSubID = tran.OrigSubID;
						SignDrAdjustments = 1m;
						SignRGOL = -1m;
						break;
					case "ADRD":
						apAccountID = tran.OrigAccountID;
						apSubID = tran.OrigSubID;
						SignDrAdjustments = 1m;
						SignDiscTaken = -1m;
						break;
					case "ADRE":
						apAccountID = tran.OrigAccountID;
						apSubID = tran.OrigSubID;
						SignRetainageWithheld = -1m;
						break;
					case "ADRF":
						SignDrAdjustments = 1m;
						SignRetainageReleased = -1m;
						SignPtd = -1m;
						break;
					case "VCKP":
					case "VCKN":
					case "CHKP":
					case "CHKN":
					case "PPMN":
					case "REFP":
					case "VRFP":
					case "REFN":
					case "VRFN":
						SignPayments = 1m;
						SignPtd = -1m;
						break;
					case "VCKR":
					case "CHKR":
					case "PPMR":
					case "REFR":
					case "VRFR":
					case "QCKR":
					case "VQCR":
						apAccountID = tran.OrigAccountID;
						apSubID = tran.OrigSubID;
						SignPayments = 1m;
						SignRGOL = -1m;
						break;
					case "VCKD":
					case "CHKD":
					case "PPMD":
					case "REFD": //not really happens
					case "VRFD":
					case "QCKD":
					case "VQCD":
						apAccountID = tran.OrigAccountID;
						apSubID = tran.OrigSubID;
						SignPayments = 1m;
						SignDiscTaken = -1m;
						break;
					case "PPMP":
						SignDeposits = 1m;
						break;
					case "PPMU":
						SignDeposits = 1m;
						break;
					case "CHKU":
					case "VCKU":
						SignDeposits = 1m;
						break;
					case "REFU":
					case "VRFU":
						SignDeposits = 1m;
						break;
					case "VCKW":
					case "PPMW":
					case "CHKW":
					case "QCKW":
					case "VQCW":
						apAccountID = tran.OrigAccountID;
						apSubID = tran.OrigSubID;
						SignPayments = 1m;
						SignWhTax = -1m;
						break;
				}
			}

			public APHistBucket()
			{
			}
		}

		private void UpdateHist<History>(History accthist, APHistBucket bucket, bool FinFlag, GLTran tran)
			where History : class, IBaseAPHist
		{
			if ((!_IsIntegrityCheck && !IsInvoiceReclassification) || accthist.DetDeleted == false)
			{
				decimal? amount = tran.DebitAmt - tran.CreditAmt;

				accthist.FinFlag = FinFlag;
				accthist.PtdPayments += bucket.SignPayments * amount;
				accthist.PtdPurchases += bucket.SignPurchases * amount;
				accthist.PtdCrAdjustments += bucket.SignCrAdjustments * amount;
				accthist.PtdDrAdjustments += bucket.SignDrAdjustments * amount;
				accthist.PtdDiscTaken += bucket.SignDiscTaken * amount;
				accthist.PtdWhTax += bucket.SignWhTax * amount;
				accthist.PtdRGOL += bucket.SignRGOL * amount;
				accthist.YtdBalance += bucket.SignPtd * amount;
				accthist.PtdDeposits += bucket.SignDeposits * amount;
				accthist.YtdDeposits += bucket.SignDeposits * amount;
				accthist.YtdRetainageReleased += bucket.SignRetainageReleased * amount;
				accthist.PtdRetainageReleased += bucket.SignRetainageReleased * amount;
				accthist.YtdRetainageWithheld += bucket.SignRetainageWithheld * amount;
				accthist.PtdRetainageWithheld += bucket.SignRetainageWithheld * amount;
			}
		}

		private void UpdateFinHist<History>(History accthist, APHistBucket bucket, GLTran tran)
			where History : class, IBaseAPHist
		{
			UpdateHist<History>(accthist, bucket, true, tran);
		}

		private void UpdateTranHist<History>(History accthist, APHistBucket bucket, GLTran tran)
			where History : class, IBaseAPHist
		{
			UpdateHist<History>(accthist, bucket, false, tran);
		}

		private void CuryUpdateHist<History>(History accthist, APHistBucket bucket, bool FinFlag, GLTran tran)
			where History : class, ICuryAPHist, IBaseAPHist
		{
			if ((!_IsIntegrityCheck && !IsInvoiceReclassification) || accthist.DetDeleted == false)
			{
				UpdateHist<History>(accthist, bucket, FinFlag, tran);

				decimal? amount = tran.CuryDebitAmt - tran.CuryCreditAmt;

				accthist.FinFlag = FinFlag;
				accthist.CuryPtdPayments += bucket.SignPayments * amount;
				accthist.CuryPtdPurchases += bucket.SignPurchases * amount;
				accthist.CuryPtdCrAdjustments += bucket.SignCrAdjustments * amount;
				accthist.CuryPtdDrAdjustments += bucket.SignDrAdjustments * amount;
				accthist.CuryPtdDiscTaken += bucket.SignDiscTaken * amount;
				accthist.CuryPtdWhTax += bucket.SignWhTax * amount;
				accthist.CuryYtdBalance += bucket.SignPtd * amount;
				accthist.CuryPtdDeposits += bucket.SignDeposits * amount;
				accthist.CuryYtdDeposits += bucket.SignDeposits * amount;
				accthist.CuryYtdRetainageReleased += bucket.SignRetainageReleased * amount;
				accthist.CuryPtdRetainageReleased += bucket.SignRetainageReleased * amount;
				accthist.CuryYtdRetainageWithheld += bucket.SignRetainageWithheld * amount;
				accthist.CuryPtdRetainageWithheld += bucket.SignRetainageWithheld * amount;
			}
		}

		private void CuryUpdateFinHist<History>(History accthist, APHistBucket bucket, GLTran tran)
			where History : class, ICuryAPHist, IBaseAPHist
		{
			CuryUpdateHist<History>(accthist, bucket, true, tran);
		}

		private void CuryUpdateTranHist<History>(History accthist, APHistBucket bucket, GLTran tran)
			where History : class, ICuryAPHist, IBaseAPHist
		{
			CuryUpdateHist<History>(accthist, bucket, false, tran);
		}

		private bool IsNeedUpdateHistoryForTransaction(string TranPeriodID)
		{
			if (IsInvoiceReclassification) return false;
			if (!_IsIntegrityCheck) return true;

			return string.Compare(TranPeriodID, _IntegrityCheckStartingPeriod) >= 0;
		}

		private void UpdateHistory(GLTran tran, Vendor vend)
		{
			UpdateHistory(tran, vend.BAccountID);
		}

		private void UpdateHistory(GLTran tran, int? vendorID)
		{
			APHistBucket bucket = new APHistBucket(tran, GetHistTranType(tran.TranType, tran.RefNbr));
			UpdateHistory(tran, vendorID, bucket);
		}

		private void UpdateHistory(GLTran tran, int? vendorID, APHistBucket bucket)
			{
			if (!IsNeedUpdateHistoryForTransaction(tran.TranPeriodID))
			{
				return;
			}

			{
				APHist accthist = CreateHistory(tran.BranchID, bucket.apAccountID, bucket.apSubID, vendorID, tran.FinPeriodID);
				if (accthist != null)
				{
					UpdateFinHist<APHist>(accthist, bucket, tran);
				}
			}

			{
				APHist accthist = CreateHistory(tran.BranchID, bucket.apAccountID, bucket.apSubID, vendorID, tran.TranPeriodID);
				if (accthist != null)
				{
					UpdateTranHist<APHist>(accthist, bucket, tran);
				}
			}
		}

		private void UpdateHistory(GLTran tran, Vendor vend, CurrencyInfo info)
		{
			UpdateHistory(tran, vend.BAccountID, info.CuryID);
		}

		private void UpdateHistory(GLTran tran, int? vendorID, string aCuryID)
		{
			APHistBucket bucket = new APHistBucket(tran, GetHistTranType(tran.TranType, tran.RefNbr));
			UpdateHistory(tran, vendorID, aCuryID, bucket);
			}

		private void UpdateHistory(GLTran tran, int? vendorID, string aCuryID, APHistBucket bucket)
		{
			if (!IsNeedUpdateHistoryForTransaction(tran.TranPeriodID))
			{
				return;
			}

			{
				CuryAPHist accthist = CreateHistory(tran.BranchID, bucket.apAccountID, bucket.apSubID, vendorID, aCuryID, tran.FinPeriodID);
				if (accthist != null)
				{
					CuryUpdateFinHist<CuryAPHist>(accthist, bucket, tran);
				}
			}

			{
				CuryAPHist accthist = CreateHistory(tran.BranchID, bucket.apAccountID, bucket.apSubID, vendorID, aCuryID, tran.TranPeriodID);
				if (accthist != null)
				{
					CuryUpdateTranHist<CuryAPHist>(accthist, bucket, tran);
				}
			}
		}

		private string GetHistTranType(string tranType, string refNbr)
		{
			string HistTranType = tranType;
			if (tranType == APDocType.VoidCheck)
			{
				APRegister doc = PXSelect<APRegister,
					Where<APRegister.refNbr, Equal<Required<APRegister.refNbr>>,
						And<Where<APRegister.docType, Equal<APDocType.check>,
							Or<APRegister.docType, Equal<APDocType.prepayment>>>>>,
					OrderBy<Asc<Switch<Case<Where<APRegister.docType, Equal<APDocType.check>>, int0>, int1>,
						Asc<APRegister.docType,
						Asc<APRegister.refNbr>>>>>.Select(this, refNbr);
				if (doc != null)
				{
					HistTranType = doc.DocType;
				}
			}

			return HistTranType;
		}

		private List<APRegister> CreateInstallments(PXResult<APInvoice, CurrencyInfo, Terms, Vendor> res)
		{
			APInvoice apdoc = (APInvoice)res;
			CurrencyInfo info = (CurrencyInfo)res;
			Terms terms = (Terms)res;
			Vendor vendor = (Vendor)res;
			List<APRegister> ret = new List<APRegister>();

			decimal CuryTotalInstallments = 0m;

			APInvoiceEntry docgraph = PXGraph.CreateInstance<APInvoiceEntry>();

			PXResultset<TermsInstallments> installments = TermsAttribute.SelectInstallments(this, terms, (DateTime)apdoc.DueDate);
			foreach (TermsInstallments inst in installments)
			{
				docgraph.vendor.Current = vendor;
				PXCache sender = APInvoice_DocType_RefNbr.Cache;
				//force precision population
				object CuryOrigDocAmt = sender.GetValueExt(apdoc, "CuryOrigDocAmt");

				CurrencyInfo new_info = PXCache<CurrencyInfo>.CreateCopy(info);
				new_info.CuryInfoID = null;
				new_info = docgraph.currencyinfo.Insert(new_info);

				APInvoice new_apdoc = PXCache<APInvoice>.CreateCopy(apdoc);
				new_apdoc.CuryInfoID = new_info.CuryInfoID;
				new_apdoc.DueDate = ((DateTime)new_apdoc.DueDate).AddDays((double)inst.InstDays);
				new_apdoc.DiscDate = new_apdoc.DueDate;
				new_apdoc.InstallmentNbr = inst.InstallmentNbr;
				new_apdoc.MasterRefNbr = new_apdoc.RefNbr;
				new_apdoc.RefNbr = null;
				new_apdoc.NoteID = null;
				new_apdoc.PayDate = null;
				new_apdoc.IntercompanyInvoiceNoteID = null;
				TaxAttribute.SetTaxCalc<APTran.taxCategoryID>(docgraph.Transactions.Cache, null, TaxCalc.NoCalc);

				if (inst.InstallmentNbr == installments.Count)
				{
					new_apdoc.CuryOrigDocAmt = new_apdoc.CuryOrigDocAmt - CuryTotalInstallments;
				}
				else
				{
					if (terms.InstallmentMthd == TermsInstallmentMethod.AllTaxInFirst)
					{
						new_apdoc.CuryOrigDocAmt = new_info
							.RoundCury((decimal)((apdoc.CuryOrigDocAmt - apdoc.CuryTaxTotal) * inst.InstPercent / 100m));
						if (inst.InstallmentNbr == 1)
						{
							new_apdoc.CuryOrigDocAmt += (decimal)apdoc.CuryTaxTotal;
						}
					}
					else
					{
						new_apdoc.CuryOrigDocAmt = new_info
							.RoundCury((decimal)(apdoc.CuryOrigDocAmt * inst.InstPercent / 100m));
					}
				}
				new_apdoc.CuryDocBal = new_apdoc.CuryOrigDocAmt;
				new_apdoc.CuryLineTotal = new_apdoc.CuryOrigDocAmt;
				new_apdoc.CuryTaxTotal = 0m;
				new_apdoc.CuryOrigDiscAmt = 0m;
				new_apdoc.CuryVatTaxableTotal = 0m;
				new_apdoc.CuryDiscTot = 0m;
				new_apdoc.OrigModule = BatchModule.AP;
				new_apdoc = docgraph.Document.Insert(new_apdoc);
				CuryTotalInstallments += (decimal)new_apdoc.CuryOrigDocAmt;
				TaxAttribute.SetTaxCalc<APTran.taxCategoryID>(docgraph.Transactions.Cache, null, TaxCalc.NoCalc);

				APTran new_aptran = new APTran();
				new_aptran.AccountID = new_apdoc.APAccountID;
				new_aptran.SubID = new_apdoc.APSubID;
				new_aptran.CuryTranAmt = new_apdoc.CuryOrigDocAmt;
				using (new PXLocaleScope(vendor.LocaleName))
				{
					new_aptran.TranDesc = PXMessages.LocalizeNoPrefix(Messages.MultiplyInstallmentsTranDesc);
				}

				docgraph.Transactions.Insert(new_aptran);

				docgraph.Save.Press();

				ret.Add((APRegister)docgraph.Document.Current);

				docgraph.Clear();
			}


			if (installments.Count > 0)
			{
				docgraph.Document.Search<APInvoice.refNbr>(apdoc.RefNbr, apdoc.DocType);
				docgraph.Document.Current.InstallmentCntr = Convert.ToInt16(installments.Count);
				docgraph.Document.Cache.SetStatus(docgraph.Document.Current, PXEntryStatus.Updated);

				docgraph.Save.Press();
				docgraph.Clear();
			}

			return ret;
		}

        public static decimal? RoundAmount(decimal? amount, string RoundType, decimal? precision)
        {
            decimal? toround = amount / precision;

            switch (RoundType)
            {
				case RoundingType.Floor:
                    return Math.Floor((decimal)toround) * precision;
				case RoundingType.Ceil:
                    return Math.Ceiling((decimal)toround) * precision;
				case RoundingType.Mathematical:
                    return Math.Round((decimal)toround, 0, MidpointRounding.AwayFromZero) * precision;
                default:
                    return amount;
            }
        }

        public virtual decimal? RoundAmount(decimal? amount)
        {
            return RoundAmount(amount, this.InvoiceRounding, this.InvoicePrecision);
        }

		/// <summary>
		/// The method to create a self document application (the same adjusted and adjusting documents)
		/// with amount equal to <see cref="APRegister.CuryOrigDocAmt"> value.
		/// </summary>
		public virtual APAdjust CreateSelfApplicationForDocument(APRegister doc)
		{
			APAdjust adj = new APAdjust();

			adj.AdjgDocType = doc.DocType;
			adj.AdjgRefNbr = doc.RefNbr;
			adj.AdjdDocType = doc.DocType;
			adj.AdjdRefNbr = doc.RefNbr;
			adj.AdjNbr = doc.AdjCntr;

			adj.AdjgBranchID = doc.BranchID;
			adj.AdjdBranchID = doc.BranchID;
			adj.VendorID = doc.VendorID;
			adj.AdjdAPAcct = doc.APAccountID;
			adj.AdjdAPSub = doc.APSubID;
			adj.AdjgCuryInfoID = doc.CuryInfoID;
			adj.AdjdCuryInfoID = doc.CuryInfoID;
			adj.AdjdOrigCuryInfoID = doc.CuryInfoID;

			adj.AdjgDocDate = doc.DocDate;
			adj.AdjdDocDate = doc.DocDate;
			adj.AdjgFinPeriodID = doc.FinPeriodID;
		    adj.AdjgTranPeriodID = doc.TranPeriodID;
			adj.AdjdFinPeriodID = doc.FinPeriodID;
			adj.AdjdTranPeriodID = doc.TranPeriodID;

			adj.CuryAdjgAmt = doc.CuryOrigDocAmt;
			adj.CuryAdjdAmt = doc.CuryOrigDocAmt;
			adj.AdjAmt = doc.OrigDocAmt;

			adj.RGOLAmt = 0m;
			adj.CuryAdjgDiscAmt = doc.CuryOrigDiscAmt;
			adj.CuryAdjdDiscAmt = doc.CuryOrigDiscAmt;
			adj.AdjDiscAmt = doc.OrigDiscAmt;

			adj.CuryAdjgWhTaxAmt = doc.CuryOrigWhTaxAmt;
			adj.CuryAdjdWhTaxAmt = doc.CuryOrigWhTaxAmt;
			adj.AdjWhTaxAmt = doc.OrigWhTaxAmt;

			adj.Released = false;
			adj = (APAdjust)APAdjust_AdjgDocType_RefNbr_VendorID.Cache.Insert(adj);

			return adj;
		}

		/// <summary>
		/// A method to process migrated documents. A special self application with amount
		/// equal to difference between <see cref="APRegister.CuryOrigDocAmt">
		/// and <see cref="APRegister.CuryInitDocBal"> will be created for the document.
		/// Note, that all logic around <see cref="ARBalances">, <see cref="ARHistory"> and
		/// document balances is implemented inside this method, so we don't need to update any balances somewhere else.
		/// This is the reason why all special applications should be excluded from the adjustments processing.
		/// </summary>
		protected virtual void ProcessMigratedDocument(
			JournalEntry je,
			GLTran tran,
			APRegister doc,
			bool isDebit,
			Vendor vendor,
			CurrencyInfo currencyinfo)
		{
			// Create special application to update balances with proper amounts.
			//
			APAdjust initAdj = CreateSelfApplicationForDocument(doc);

			initAdj.RGOLAmt = 0m;
			initAdj.CuryAdjgDiscAmt = 0m;
			initAdj.CuryAdjdDiscAmt = 0m;
			initAdj.AdjDiscAmt = 0m;

			initAdj.CuryAdjgWhTaxAmt = 0m;
			initAdj.CuryAdjdWhTaxAmt = 0m;
			initAdj.AdjWhTaxAmt = 0m;

			initAdj.CuryAdjgAmt -= doc.CuryInitDocBal;
			initAdj.CuryAdjdAmt -= doc.CuryInitDocBal;
			initAdj.AdjAmt -= doc.InitDocBal;

			initAdj.Released = true;
			initAdj.IsInitialApplication = true;
			initAdj = (APAdjust)APAdjust_AdjgDocType_RefNbr_VendorID.Cache.Update(initAdj);

			// We don't need to update balances for VoidPayment document,
			// because it will be closed anyway further in the code.
			//
			if (initAdj.VoidAppl != true)
			{
				UpdateBalances(initAdj, doc, vendor);
				UpdateRetainageBalances(initAdj, doc, null);
				VerifyDocumentBalanceAndClose(doc);
			}

			// Create special GL transaction to update history with proper bucket.
			//
			GLTran initTran = (GLTran)je.GLTranModuleBatNbr.Cache.CreateCopy(tran);

			initTran.TranClass = GLTran.tranClass.Normal;
			initTran.TranType = APDocType.DebitAdj;
			initTran.DebitAmt = isDebit ? initAdj.AdjAmt : 0m;
			initTran.CuryDebitAmt = isDebit ? initAdj.CuryAdjgAmt : 0m;
			initTran.CreditAmt = isDebit ? 0m : initAdj.AdjAmt;
			initTran.CuryCreditAmt = isDebit ? 0m : initAdj.CuryAdjgAmt;

			UpdateHistory(initTran, vendor);
			UpdateHistory(initTran, vendor, currencyinfo);
			ProcessAdjustmentTranPost(initAdj, doc, doc, true);


			// All deposits should be moved to the Payments bucket,
			// to prevent amounts stack on the Deposits bucket.
			//
			APHistBucket origBucket = new APHistBucket(tran, GetHistTranType(tran.TranType, tran.RefNbr));

			if (origBucket.SignDeposits != 0m)
			{
				APHistBucket initBucket = new APHistBucket();
				decimal sign = origBucket.SignDeposits;

				initBucket.apAccountID = tran.AccountID;
				initBucket.apSubID = tran.SubID;
				initBucket.SignDeposits = sign;
				initBucket.SignPayments = -sign;
				initBucket.SignPtd = sign;

				UpdateHistory(initTran, vendor.BAccountID, initBucket);
				UpdateHistory(initTran, vendor.BAccountID, currencyinfo.CuryID, initBucket);
			}
		}

		public virtual void VerifyStockItemLineHasReceipt(APRegister doc)
		{
			if (IsMigrationMode == true || doc.IsMigratedRecord == true || doc.IsRetainageDocument == true) return;

			APTran tran = PXSelectJoin<APTran,
				InnerJoin<InventoryItem, On<APTran.inventoryID, Equal<InventoryItem.inventoryID>>>,
				Where<APTran.refNbr, Equal<Required<APInvoice.refNbr>>,
					And<APTran.tranType, Equal<Required<APInvoice.docType>>,
					And2<Where<APTran.pOAccrualType, IsNull, Or<APTran.pOAccrualType, NotEqual<POAccrualType.order>>>,
					And<APTran.receiptNbr, IsNull, And<InventoryItem.stkItem, Equal<True>,
					And<APTran.tranType, NotEqual<APDocType.prepayment>>>>>>>>
				.SelectSingleBound(this, null, doc.RefNbr, doc.DocType);
			if (tran != null)
			{
				throw new PXException(Messages.HasNoLinkedtoReceipt);
			}
		}

		public virtual void VerifyInterBranchTransactions(APRegister doc)
		{
			if (IsMigrationMode == true || PXAccess.FeatureInstalled<FeaturesSet.interBranch>()) return;

			var branch = (Branch)PXSelect<Branch, Where<Branch.branchID, Equal<Required<Branch.branchID>>>>.SelectSingleBound(this, null, doc.BranchID);

			var adjdsToDifferentOrganization = PXSelectJoin<APAdjust,
				InnerJoin<Branch, On<Branch.branchID, Equal<APAdjust.adjdBranchID>>>,
				Where<APAdjust.adjgDocType, Equal<Required<APAdjust.adjgDocType>>,
					And<APAdjust.adjgRefNbr, Equal<Required<APAdjust.adjgRefNbr>>,
					And<APAdjust.adjgBranchID, NotEqual<APAdjust.adjdBranchID>,
					And<Branch.organizationID, NotEqual<Required<Branch.organizationID>>>>>>>
				.SelectSingleBound(this, null, doc.DocType, doc.RefNbr, branch.OrganizationID);

			if (adjdsToDifferentOrganization.Any())
			{
				throw new PXException(GL.Messages.InterBranchTransAreNotAllowed);
			}

			var adjgsToDifferentOrganization = PXSelectJoin<APAdjust,
				InnerJoin<Branch, On<Branch.branchID, Equal<APAdjust.adjgBranchID>>>,
				Where<APAdjust.adjdDocType, Equal<Required<APAdjust.adjdDocType>>,
					And<APAdjust.adjdRefNbr, Equal<Required<APAdjust.adjdRefNbr>>,
					And<APAdjust.adjgBranchID, NotEqual<APAdjust.adjdBranchID>,
					And<Branch.organizationID, NotEqual<Required<Branch.organizationID>>>>>>>
				.SelectSingleBound(this, null, doc.DocType, doc.RefNbr, branch.OrganizationID);

			if (adjgsToDifferentOrganization.Any())
			{
				throw new PXException(GL.Messages.InterBranchTransAreNotAllowed);
			}

			Ledger docLedger = null;
			VerifyInterBranchTransactions<APAdjust.adjgDocType, APAdjust.adjgRefNbr, APAdjust.adjdBranchID>(doc, ref docLedger);
			VerifyInterBranchTransactions<APAdjust.adjdDocType, APAdjust.adjdRefNbr, APAdjust.adjgBranchID>(doc, ref docLedger);
		}
		[Obsolete(Common.Messages.MethodIsObsoleteAndWillBeRemoved2020R1)]
		public virtual void VerifyInterBranchTransactions<docTypeField, docNbrField, branchIDField>(APRegister doc, ref Ledger docLedger)
			where docTypeField : IBqlField
			where docNbrField : IBqlField
			where branchIDField : IBqlField
		{
		}

		/// <summary>
		/// The method to release invoices.
		/// The maintenance screen is "Bills And Adjustments" (AP301000).
		/// </summary>
		public virtual List<APRegister> ReleaseInvoice(
			JournalEntry je,
			ref APRegister doc,
			PXResult<APInvoice, CurrencyInfo, Terms, Vendor> res,
			bool isPrebooking,
			out List<INRegister> inDocs)
		{
			APInvoice apdoc = res;
			CurrencyInfo info = res;
			Terms terms = res;
			Vendor vend = res;

			APInvoice_DocType_RefNbr.Current = apdoc;
			APDocument.Cache.Current = doc;

			List<APRegister> ret = null;
			inDocs = new List<INRegister>();

			if (doc.Released != true && (!isPrebooking || doc.Prebooked != true))
			{
				string _InstallmentType = terms.InstallmentType;
				bool masterInstallment = apdoc.InstallmentCntr != null;
				if (_IsIntegrityCheck && apdoc.InstallmentNbr == null)
				{
					_InstallmentType = apdoc.InstallmentCntr != null ? TermsInstallmentType.Multiple : TermsInstallmentType.Single;
				}

				bool isPrebookCompletion = (doc.Prebooked == true);
				if (doc.DocType == APDocType.Invoice && doc.Voided == true)
					isPrebookCompletion = true;
				bool isPrebookVoiding = doc.DocType == APDocType.VoidQuickCheck && !string.IsNullOrEmpty(apdoc.PrebookBatchNbr);
				if (isPrebookCompletion && string.IsNullOrEmpty(apdoc.PrebookBatchNbr))
				{
					throw new PXException(Messages.LinkToThePrebookingBatchIsMissing, doc.DocType, doc.RefNbr);
				}

				if (_InstallmentType == TermsInstallmentType.Multiple && isPrebooking)
				{
					throw new PXException(Messages.InvoicesWithMultipleInstallmentTermsMayNotBePrebooked);
				}

				if (_InstallmentType == TermsInstallmentType.Multiple && (apdoc.DocType == APDocType.QuickCheck || apdoc.DocType == APDocType.VoidQuickCheck))
				{
					throw new PXException(Messages.Quick_Check_Cannot_Have_Multiply_Installments);
				}

				if (_InstallmentType == TermsInstallmentType.Multiple && apdoc.InstallmentNbr == null)
				{
					if (!_IsIntegrityCheck && !IsInvoiceReclassification)
					{
						ret = CreateInstallments(res);
					}
					masterInstallment = true;
					doc.CuryDocBal = 0m;
					doc.DocBal = 0m;
					doc.CuryDiscBal = 0m;
					doc.DiscBal = 0m;
					doc.CuryDiscTaken = 0m;
					doc.DiscTaken = 0m;
					doc.CuryWhTaxBal = 0m;
					doc.WhTaxBal = 0m;
					doc.CuryTaxWheld = 0m;
					doc.TaxWheld = 0m;

					doc.OpenDoc = false;
					doc.ClosedDate = doc.DocDate;
					doc.ClosedFinPeriodID = doc.FinPeriodID;
					RaiseInvoiceEvent(doc, APInvoice.Events.Select(ev => ev.CloseDocument));
				}
				else if (!isPrebookCompletion)
				{
						doc.CuryDocBal = doc.CuryOrigDocAmt;
						doc.DocBal = doc.OrigDocAmt;
						doc.CuryRetainageUnreleasedAmt = doc.CuryRetainageTotal;
						doc.RetainageUnreleasedAmt = doc.RetainageTotal;
						doc.CuryDiscBal = doc.CuryOrigDiscAmt;
						doc.DiscBal = doc.OrigDiscAmt;
						doc.CuryWhTaxBal = doc.CuryOrigWhTaxAmt;
						doc.WhTaxBal = doc.OrigWhTaxAmt;
						doc.CuryDiscTaken = 0m;
						doc.DiscTaken = 0m;
						doc.CuryTaxWheld = 0m;
						doc.TaxWheld = 0m;
						doc.RGOLAmt = 0m;

						doc.OpenDoc = true;
						doc.ClosedDate = null;
						doc.ClosedFinPeriodID = null;
						doc.ClosedTranPeriodID = null;
						RaiseInvoiceEvent(doc, APInvoice.Events.Select(ev => ev.OpenDocument));
					}

				//should always restore APRegister to APInvoice after above assignments
				PXCache<APRegister>.RestoreCopy(apdoc, doc);

				CurrencyInfo new_info = CurrencyInfo.GetEX(GetCurrencyInfoCopyForGL(je, info));

				bool isDebit = (apdoc.DrCr == DrCr.Debit);
				bool isQuickCheckOrVoidQuickCheckDocument =
					apdoc.DocType == APDocType.QuickCheck ||
					apdoc.DocType == APDocType.VoidQuickCheck;

				if (isPrebookCompletion == false)
				{
					if (isQuickCheckOrVoidQuickCheckDocument)
					{
						GLTran tran = new GLTran();
						tran.SummPost = true;
						tran.ZeroPost = false;
						tran.BranchID = apdoc.BranchID;

						tran.AccountID = apdoc.APAccountID;
						tran.SubID = apdoc.APSubID;
						tran.ReclassificationProhibited = true;
						tran.CuryDebitAmt = isDebit ? 0m : apdoc.CuryOrigDocAmt + apdoc.CuryOrigDiscAmt + apdoc.CuryOrigWhTaxAmt;
						tran.DebitAmt = isDebit ? 0m : apdoc.OrigDocAmt + apdoc.OrigDiscAmt + apdoc.OrigWhTaxAmt;
						tran.CuryCreditAmt = isDebit ? apdoc.CuryOrigDocAmt + apdoc.CuryOrigDiscAmt + apdoc.CuryOrigWhTaxAmt : 0m;
						tran.CreditAmt = isDebit ? apdoc.OrigDocAmt + apdoc.OrigDiscAmt + apdoc.OrigWhTaxAmt : 0m;

						tran.TranType = apdoc.DocType;
						tran.TranClass = apdoc.DocClass;
						tran.RefNbr = apdoc.RefNbr;
						tran.TranDesc = apdoc.DocDesc;
						FinPeriodIDAttribute.SetPeriodsByMaster<GLTran.finPeriodID>(je.GLTranModuleBatNbr.Cache, tran, apdoc.TranPeriodID);
						tran.TranDate = apdoc.DocDate;
						tran.CuryInfoID = new_info.CuryInfoID;
						tran.Released = true;
						tran.ReferenceID = apdoc.VendorID;

						//no history update should take place
						InsertInvoiceTransaction(je, tran,
							new GLTranInsertionContext { APRegisterRecord = doc });
					}
					else if (apdoc.DocType != APDocType.Prepayment)
					{
						GLTran tran = new GLTran();
						tran.SummPost = true;
						tran.BranchID = apdoc.BranchID;

						tran.AccountID = apdoc.APAccountID;
						tran.SubID = apdoc.APSubID;
						tran.ReclassificationProhibited = true;
						tran.CuryDebitAmt = isDebit ? 0m : apdoc.CuryOrigDocAmt;
						tran.DebitAmt = isDebit ? 0m : apdoc.OrigDocAmt - apdoc.RGOLAmt;
						tran.CuryCreditAmt = isDebit ? apdoc.CuryOrigDocAmt : 0m;
						tran.CreditAmt = isDebit ? apdoc.OrigDocAmt - apdoc.RGOLAmt : 0m;

						tran.TranType = apdoc.DocType;
						tran.TranClass =
							apdoc.IsChildRetainageDocument()
								? GLTran.tranClass.RetainageReleased
								: apdoc.DocClass;
						tran.RefNbr = apdoc.RefNbr;
						tran.TranDesc = apdoc.DocDesc;
						FinPeriodIDAttribute.SetPeriodsByMaster<GLTran.finPeriodID>(je.GLTranModuleBatNbr.Cache, tran, apdoc.TranPeriodID);
						tran.TranDate = apdoc.DocDate;
						tran.CuryInfoID = new_info.CuryInfoID;
						tran.Released = true;
						tran.ReferenceID = apdoc.VendorID;
                        tran.ProjectID = PM.ProjectDefaultAttribute.NonProject();
						tran.NonBillable = true;

						if (doc.OpenDoc == true)
						{
							UpdateHistory(tran, vend);
							UpdateHistory(tran, vend, info);
						}

						InsertInvoiceTransaction(je, tran,
							new GLTranInsertionContext { APRegisterRecord = doc });

						if (IsMigratedDocumentForProcessing(doc))
						{
							ProcessMigratedDocument(je, tran, doc, isDebit, vend, info);
						}

						#region Retainage part

						if (apdoc.IsOriginalRetainageDocument())
						{
							GLTran retainageTran = (GLTran)je.GLTranModuleBatNbr.Cache.CreateCopy(tran);

							retainageTran.ReclassificationProhibited = true;
							retainageTran.AccountID = apdoc.RetainageAcctID;
							retainageTran.SubID = apdoc.RetainageSubID;

							retainageTran.CuryDebitAmt = isDebit ? 0m : apdoc.CuryRetainageTotal;
							retainageTran.DebitAmt = isDebit ? 0m : apdoc.RetainageTotal;
							retainageTran.CuryCreditAmt = isDebit ? apdoc.CuryRetainageTotal : 0m;
							retainageTran.CreditAmt = isDebit ? apdoc.RetainageTotal : 0m;

							retainageTran.OrigAccountID = tran.AccountID;
							retainageTran.OrigSubID = tran.SubID;

							retainageTran.TranClass = GLTran.tranClass.RetainageWithheld;

							UpdateHistory(retainageTran, vend);
							UpdateHistory(retainageTran, vend, info);

							je.GLTranModuleBatNbr.Insert(retainageTran);
						}

						if (apdoc.IsOriginalRetainageDocument() &&
							apdoc.DocType == APDocType.DebitAdj)
						{
							APRegister origRetainageDoc = GetOriginalRetainageDocument(apdoc);

							// We should clear unreleased retainage amount
							// for the original retainage bill in the case
							// when it is a reversing DebitAdj document.
							//
							if (origRetainageDoc != null)
							{
								doc.CuryRetainageUnreleasedAmt = 0m;
								doc.CuryRetainageReleased = 0m;
								origRetainageDoc.CuryRetainageUnreleasedAmt = 0m;
								origRetainageDoc.CuryRetainageReleased = 0m;

								using (new DisableFormulaCalculationScope(
									APDocument.Cache,
									typeof(APRegister.curyRetainageReleased)))
								{
									APDocument.Update(origRetainageDoc);
								}
							}
						}

						if (apdoc.IsChildRetainageDocument())
						{
							APRegister origRetainageDoc = GetOriginalRetainageDocument(apdoc);

							// We should update unreleased retainage amount
							// for the original retainage bill in the case
							// when it is a child retainage document.
							//
							if (origRetainageDoc != null)
							{
								origRetainageDoc.CuryRetainageUnreleasedAmt -= (apdoc.CuryOrigDocAmt + apdoc.CuryRoundDiff) * apdoc.SignAmount;
								origRetainageDoc = APDocument.Update(origRetainageDoc);

								if (!_IsIntegrityCheck && origRetainageDoc.CuryRetainageUnreleasedAmt < 0m)
								{
									throw new PXException(Messages.RetainageUnreleasedBalanceNegative);
								}
							}
						}

						#endregion
					}
				}

				var docInclTaxDiscrepancy = 0.0m;

				if (apdoc.DocType == APDocType.Prepayment)
				{
					APTran prevTran = null;
					foreach (PXResult<APTran, APTax, Tax> r in APTran_TranType_RefNbr.Select(apdoc.DocType, apdoc.RefNbr))
					{
						APTran n = r;
						if (!APTran_TranType_RefNbr.Cache.ObjectsEqual(n, prevTran))
						{
							var releasingArgs = new InvoiceTransactionReleasingArgs
							{
								Invoice = apdoc,
								Transaction = n,

								IsPrebooking = isPrebooking
							};

							InvoiceTransactionReleasing(releasingArgs);
						}
						prevTran = n;
					}

					var transReleasedArgs = new InvoiceTransactionsReleasedArgs
					{
						Invoice = apdoc,
						IsPrebooking = isPrebooking
					};
					InvoiceTransactionsReleased(transReleasedArgs);
				}
				else
				{
					GLTran summaryTran = null;
					if (isPrebooking || isPrebookCompletion || isPrebookVoiding)
					{
						summaryTran = new GLTran
						{
							SummPost = true,
							ZeroPost = false,
							CuryCreditAmt = 0,
							CuryDebitAmt = 0,
							CreditAmt = 0,
							DebitAmt = 0,
							BranchID = apdoc.BranchID,
							AccountID = apdoc.PrebookAcctID,
							SubID = apdoc.PrebookSubID,
							ReclassificationProhibited = true,
							TranType = apdoc.DocType,
							TranClass = apdoc.DocClass,
							RefNbr = apdoc.RefNbr,
							TranDesc = isPrebookCompletion
							? PXMessages.LocalizeNoPrefix(Messages.PreliminaryAPExpenceBookingAdjustment)
							: PXMessages.LocalizeNoPrefix(Messages.PreliminaryAPExpenceBooking),
							TranDate = apdoc.DocDate,
							CuryInfoID = new_info.CuryInfoID,
							Released = true,
							ReferenceID = apdoc.VendorID
						};
						FinPeriodIDAttribute.SetPeriodsByMaster<GLTran.finPeriodID>(
							je.GLTranModuleBatNbr.Cache, summaryTran, apdoc.TranPeriodID);
					}

					bool updateDeferred = !isPrebooking;

					PXResultset<APTran> apTranTaxes = APTran_TranType_RefNbr.Select(apdoc.DocType, apdoc.RefNbr);

					IEqualityComparer<APTaxTran> apTaxTranComparer =
						new FieldSubsetEqualityComparer<APTaxTran>(
						Caches[typeof(APTaxTran)],
						typeof(APTaxTran.recordID));

					bool isPayByLineRetainageDebitAdj = IsPayByLineRetainageDebitAdj(doc);
					bool calculateLineBalances = doc.PaymentsByLinesAllowed == true || isPayByLineRetainageDebitAdj;

					foreach (var group in apTranTaxes.AsEnumerable()
						.Cast<PXResult<APTran, APTax, Tax, DRDeferredCode, LandedCostCode, InventoryItem, APTaxTran>>()
						.GroupBy(row => (APTaxTran)row, apTaxTranComparer))
					{
						APTaxTran apTaxTran = group.Key;
						List<APTax> apTaxes = new List<APTax>();
						Tax prev_tax = null;

						foreach (PXResult<APTran, APTax, Tax, DRDeferredCode, LandedCostCode, InventoryItem, APTaxTran> row in group)
						{
							APTran tran = (APTran)row;
							APTax aptax = (APTax)row;
							prev_tax = (Tax)row;

							tran.TranDate = apdoc.DocDate;
							APTran_TranType_RefNbr.Cache.SetDefaultExt<APTran.finPeriodID>(tran);

							if (aptax.TranType != null && aptax.RefNbr != null && aptax.LineNbr != null)
							{
								apTaxes.Add(aptax);
							}
						}

						if (apTaxes.Count() > 0 && calculateLineBalances)
						{
							APTaxAttribute apTaxAttr = new APTaxAttribute(typeof(APRegister), typeof(APTax), typeof(APTaxTran))
							{
								Inventory = typeof(APTran.inventoryID),
								UOM = typeof(APTran.uOM),
								LineQty = typeof(APTran.qty)
							};

							apTaxAttr.DistributeTaxDiscrepancy<APTax, APTax.curyTaxAmt, APTax.taxAmt>(this, apTaxes, apTaxTran.CuryTaxAmt.Value, true);

							if (apTaxTran.CuryRetainedTaxAmt != 0m)
							{
								APRetainedTaxAttribute apRetainedTaxAttr = new APRetainedTaxAttribute(typeof(APRegister), typeof(APTax), typeof(APTaxTran));
								apRetainedTaxAttr.DistributeTaxDiscrepancy<APTax, APTax.curyRetainedTaxAmt, APTax.retainedTaxAmt>(this, apTaxes, apTaxTran.CuryRetainedTaxAmt.Value, true);
							}

							if (prev_tax?.DeductibleVAT == true)
							{
								apTaxAttr.DistributeTaxDiscrepancy<APTax, APTax.curyExpenseAmt, APTax.expenseAmt>(this, apTaxes, apTaxTran.CuryExpenseAmt.Value, true);
							}
						}
					}

					FinPeriodUtils.AllowPostToUnlockedPeriodAnyway = _IsIntegrityCheck;

					if (!_IsIntegrityCheck)
						FinPeriodUtils.ValidateFinPeriod(apTranTaxes.Select(line => (APTran)line), typeof(OrganizationFinPeriod.aPClosed));

					FinPeriodUtils.AllowPostToUnlockedPeriodAnyway = false;
					ValidateLandedCostTran(doc, apTranTaxes);
					CheckVoidQuickCheckAmountDiscrepancies(doc);

					//sorting on joined tables' fields does not work!
					IComparer<Tax> taxComparer = GetTaxComparer();
					taxComparer.ThrowOnNull(nameof(taxComparer));

					apTranTaxes.Sort((PXResult<APTran> x, PXResult<APTran> y) =>
					{
						APTran tranX = (APTran)x;
						APTran tranY = (APTran)y;
						Tax taxX = x.GetItem<Tax>();
						Tax taxY = y.GetItem<Tax>();

						return tranX.LineNbr == tranY.LineNbr
							? taxComparer.Compare(taxX, taxY)
							: tranX.LineNbr.Value - tranY.LineNbr.Value;
					});

					LineBalances validateBalances = new LineBalances(0m);
					APTran maxRetainageTran = null;
					APTran maxBalanceTran = null;

					IEqualityComparer<APTran> apTranComparer =
						new FieldSubsetEqualityComparer<APTran>(
						APTran_TranType_RefNbr.Cache,
						typeof(APTran.tranType),
						typeof(APTran.refNbr),
						typeof(APTran.lineNbr));

					HashSet<int> tranProjectIDs = new HashSet<int>();
					foreach (var group in apTranTaxes.AsEnumerable().GroupBy(row => (APTran)row, apTranComparer))
					{
						APTran n = group.Key;
						tranProjectIDs.Add((int)n.ProjectID);

						if (!_IsIntegrityCheck && !IsInvoiceReclassification)
						{
							n.ClearInvoiceDetailsBalance();
						}

						if (!_IsIntegrityCheck && !IsInvoiceReclassification && n.Released == true)
						{
							throw new PXException(Messages.Document_Status_Invalid);
						}

						bool isFirstAPTaxRow = true;
						bool isMultipleInstallmentInvoice = _InstallmentType == TermsInstallmentType.Multiple && apdoc.InstallmentNbr == null;

						foreach (PXResult<APTran, APTax, Tax, DRDeferredCode, LandedCostCode, InventoryItem, APTaxTran> r in group)
						{
							APTax x = r;
							Tax salestax = r;
							DRDeferredCode defcode = r;
							InventoryItem inventoryItem = r;
							LandedCostCode landedCostCode = r;

							if (x != null && x.TaxID != null)
							{
								AdjustTaxCalculationLevelForNetGrossEntryMode(apdoc, n, ref salestax);
							}

							if (isFirstAPTaxRow)
							{
								GLTran tran = new GLTran();
								tran.ReclassificationProhibited = doc.IsChildRetainageDocument();
								tran.SummPost = this.SummPost;
								tran.BranchID = n.BranchID;
								tran.CuryInfoID = new_info.CuryInfoID;
								tran.TranType = n.TranType;
								tran.TranClass = apdoc.DocClass;
								tran.InventoryID = n.InventoryID;
								tran.UOM = n.UOM;
								tran.Qty = (n.DrCr == DrCr.Debit) ? n.Qty : -1 * n.Qty;
								tran.RefNbr = n.RefNbr;
								tran.TranDate = n.TranDate;
								tran.ProjectID = ProjectDefaultAttribute.NonProject();
								tran.CostCodeID = CostCodeAttribute.DefaultCostCode;
								tran.AccountID = n.AccountID;
								tran.SubID = n.SubID;
								tran.TranDesc = n.TranDesc;
								tran.Released = true;
								tran.ReferenceID = apdoc.VendorID;
								tran.TranLineNbr = (tran.SummPost == true) ? null : n.LineNbr;
								tran.NonBillable = n.NonBillable;

								Amount postedAmount = GetExpensePostingAmount(this,
									n,
									x,
									salestax,
									apdoc,
									amount => CM.PXDBCurrencyAttribute.Round(je.GLTranModuleBatNbr.Cache, tran, amount, CM.CMPrecision.TRANCURY),
									amount => CM.PXDBCurrencyAttribute.Round(je.GLTranModuleBatNbr.Cache, tran, amount, CM.CMPrecision.BASECURY));

								tran.CuryDebitAmt = (n.DrCr == DrCr.Debit) ? postedAmount.Cury : 0m;
								tran.DebitAmt = (n.DrCr == DrCr.Debit) ? postedAmount.Base : 0m;
								tran.CuryCreditAmt = (n.DrCr == DrCr.Debit) ? 0m : postedAmount.Cury;
								tran.CreditAmt = (n.DrCr == DrCr.Debit) ? 0m : postedAmount.Base;

								ReleaseInvoiceTransactionPostProcessing(je, apdoc, r, tran);

								var releasingArgs = new InvoiceTransactionReleasingArgs
								{
									Invoice = apdoc,
									Register = doc,
									TransactionResult = r,
									GLTransaction = tran,
									PostedAmount = postedAmount,
									JournalEntry = je,

									CurrencyInfo = new_info,
									IsPrebooking = isPrebooking
								};

								InvoiceTransactionReleasing(releasingArgs);

								IEnumerable<GLTran> transactions = null;
								if (!_IsIntegrityCheck && !IsInvoiceReclassification && defcode != null && defcode.DeferredCodeID != null && updateDeferred)
								{
									DRProcess dr = PXGraph.CreateInstance<DRProcess>();
									dr.CreateSchedule(n, defcode, apdoc, postedAmount.Base.Value, isDraft: false);
									dr.Actions.PressSave();

									transactions = je.CreateTransBySchedule(dr, tran);
									je.CorrectCuryAmountsDueToRounding(transactions, tran, postedAmount.Cury.Value);
								}

								if (transactions == null || transactions.Any() == false)
								{
									transactions = new GLTran[] { tran };
								}

								if (isPrebooking == true || isPrebookVoiding == true)
								{
									foreach (var item in transactions)
									{
										Append(summaryTran, item);
									}
								}
								else
								{
									foreach (var item in transactions)
									{
										InsertInvoiceDetailsScheduleTransaction(je, item,
											new GLTranInsertionContext { APRegisterRecord = doc, APTranRecord = n });
										if (isPrebookCompletion)
										{
											Append(summaryTran, item);
										}
									}
									n.Released = true;
								}

								isFirstAPTaxRow = false;
							}

							if (!_IsIntegrityCheck &&
								!IsInvoiceReclassification &&
								calculateLineBalances &&
								n.LineType != SO.SOLineType.Discount &&
								!isMultipleInstallmentInvoice)
							{
								validateBalances += AdjustInvoiceDetailsBalanceByTax(doc, n, x, salestax);
							}
						}

						if (calculateLineBalances &&
							n.LineType != SO.SOLineType.Discount &&
							!isMultipleInstallmentInvoice)
						{
							if (!_IsIntegrityCheck && !IsInvoiceReclassification)
							{
								validateBalances += AdjustInvoiceDetailsBalanceByLine(doc, n);
							}

							n.RecoverInvoiceDetailsBalance();

							maxRetainageTran = maxRetainageTran == null || maxRetainageTran.CuryOrigRetainageAmt < n.CuryOrigRetainageAmt ? n : maxRetainageTran;
							maxBalanceTran = maxBalanceTran == null || maxBalanceTran.CuryOrigTranAmt < n.CuryOrigTranAmt ? n : maxBalanceTran;

							if (apdoc.IsOriginalRetainageDocument() &&
								apdoc.DocType == APDocType.DebitAdj)
							{
								APTran origRetainageTran = GetOriginalRetainageLine(apdoc, n);

								if (origRetainageTran != null)
								{
									n.CuryRetainageBal = 0m;
									n.RetainageBal = 0m;
									origRetainageTran.CuryRetainageBal = 0m;
									origRetainageTran.RetainageBal = 0m;
									APTran_TranType_RefNbr.Update(origRetainageTran);
								}
							}

							if (apdoc.IsChildRetainageDocument())
							{
								AdjustOriginalRetainageLineBalance(apdoc, n, n.CuryOrigTranAmt, n.OrigTranAmt);
							}

							if (isPayByLineRetainageDebitAdj)
							{
								n.CuryTranBal = 0m;
								n.TranBal = 0m;
								n.CuryRetainageBal = 0m;
								n.RetainageBal = 0m;
							}
						}

						APTran_TranType_RefNbr.Update(n);
					}

					if (PXAccess.FeatureInstalled<FeaturesSet.projectModule>() && !_IsIntegrityCheck && !IsInvoiceReclassification)
					{
						doc.ProjectID = tranProjectIDs.Count == 1
							? tranProjectIDs.First()
							: (int)ProjectDefaultAttribute.NonProject();
						APDocument.Update(doc);
					}

					if (!_IsIntegrityCheck &&
						!IsInvoiceReclassification &&
						calculateLineBalances &&

						// Calculate line balances only once during the
						// prebook or release process.
						//
						doc.Released != true &&
						doc.Prebooked != true)
					{
						if ((validateBalances.CashDiscountBalance.Cury != doc.CuryOrigDiscAmt ||
							validateBalances.CashDiscountBalance.Base != doc.OrigDiscAmt) &&
							maxBalanceTran != null)
						{
							maxBalanceTran.CuryCashDiscBal -= validateBalances.CashDiscountBalance.Cury - doc.CuryOrigDiscAmt;
							maxBalanceTran.CashDiscBal -= validateBalances.CashDiscountBalance.Base - doc.OrigDiscAmt;
							APTran_TranType_RefNbr.Update(maxBalanceTran);
						}

						if (validateBalances.RetainageBalance.Cury != doc.CuryRetainageTotal)
						{
							throw new PXException(Messages.SumLineRetainageBalancesNotEqualRetainageTotal);
						}
						else if (validateBalances.RetainageBalance.Base != doc.RetainageTotal &&
							maxRetainageTran != null)
						{
							decimal? retainageDelta = validateBalances.RetainageBalance.Base - doc.RetainageTotal;
							maxBalanceTran.OrigRetainageAmt -= retainageDelta;
							if (maxBalanceTran.RetainageBal != 0m)
							{
								maxBalanceTran.RetainageBal -= retainageDelta;
							}
							APTran_TranType_RefNbr.Update(maxBalanceTran);
						}

						if (validateBalances.TranBalance.Cury != doc.CuryDocBal)
						{
							throw new PXException(Messages.SumLineBalancesNotEqualDocBalance);
						}
						else if (validateBalances.TranBalance.Base != doc.DocBal &&
							maxBalanceTran != null)
						{
							decimal? balanceDelta = validateBalances.TranBalance.Base - doc.DocBal;
							maxBalanceTran.OrigTranAmt -= balanceDelta;
							if (maxBalanceTran.TranBal != 0m)
							{
								maxBalanceTran.TranBal -= balanceDelta;
							}
							APTran_TranType_RefNbr.Update(maxBalanceTran);

							if (apdoc.IsChildRetainageDocument())
							{
								AdjustOriginalRetainageLineBalance(apdoc, maxBalanceTran, 0m, balanceDelta);
							}
						}
					}

					var transReleasedArgs = new InvoiceTransactionsReleasedArgs
					{
						Invoice = apdoc,
						IsPrebooking = isPrebooking
					};
					InvoiceTransactionsReleased(transReleasedArgs);
					if (transReleasedArgs.INDocuments.Any() == true)
						inDocs.AddRange(transReleasedArgs.INDocuments);

					if (isPrebookCompletion)
					{
						foreach (PXResult<APTaxTran, Tax, APInvoice> r in APTaxTran_TranType_RefNbr.Select(apdoc.DocType, apdoc.RefNbr))
						{
							APTaxTran x = r;
							Tax salestax = r;
							APInvoice orig_doc = r;
							if (salestax.DeductibleVAT == true && salestax.ReportExpenseToSingleAccount != true && salestax.TaxCalcType == CSTaxCalcType.Item)
							{
								IEnumerable<GLTran> newTrans = PostTaxExpenseToItemAccounts(je, apdoc, new_info, x, salestax, true);
								foreach (var item in newTrans)
								{
									Append(summaryTran, item);
								}
							}

							if ((salestax.TaxType == CSTaxType.Use || salestax.TaxType == CSTaxType.Sales) &&
								IsPostUseAndSalesTaxesByProjectKey(this, salestax))
							{
								IEnumerable<GLTran> newTrans = PostTaxAmountByProjectKey(je, apdoc, new_info, x, salestax, true, true);
								foreach (var item in newTrans)
								{
									Append(summaryTran, item);
								}
							}
						}
						Invert(summaryTran);
						PostGraph.NormalizeAmounts(summaryTran);
						InsertInvoiceTaxTransaction(je, summaryTran,
							new GLTranInsertionContext { APRegisterRecord = doc });
					}
					else
					{
						foreach (PXResult<APTaxTran, Tax, APInvoice> r in APTaxTran_TranType_RefNbr.Select(apdoc.DocType, apdoc.RefNbr))
						{
							APTaxTran x = r;
							Tax salestax = r;
							APInvoice orig_doc = r;

							if (apdoc.TaxCalcMode == TaxCalculationMode.Gross || salestax.TaxCalcLevel == CSTaxCalcLevel.Inclusive && apdoc.TaxCalcMode != TaxCalculationMode.Net)
								docInclTaxDiscrepancy += CalcDocInclTaxDiscrepancyForTran(x, salestax);

							if (salestax.TaxType == CSTaxType.Withholding)
							{
								continue;
							}

							if (salestax.DirectTax == true && string.IsNullOrEmpty(x.OrigRefNbr) == false)
							{
								if (_IsIntegrityCheck || IsInvoiceReclassification)
								{
									continue;
								}

								if (orig_doc.CuryInfoID == null)
								{
									throw new PXException(ErrorMessages.ElementDoesntExist, x.OrigRefNbr);
								}

								PostDirectTax(info, x, orig_doc);
							}

							if (salestax.TaxType == CSTaxType.Use || salestax.TaxType == CSTaxType.Sales)
							{
								bool isChildRetainageDocumentWithTax = apdoc.IsChildRetainageDocument() && x.CuryTaxAmt != 0m;

								// Use and Sales taxes should not be posted to
								// Expense account for the child retainage document
								// See AC-135457 and AC-135562 for details.
								//
								if (!isChildRetainageDocumentWithTax)
								{
									if (IsPostUseAndSalesTaxesByProjectKey(this, salestax))
									{
										IEnumerable<GLTran> newTrans = PostTaxAmountByProjectKey(je, apdoc, new_info, x, salestax, !isPrebooking && !isPrebookVoiding, true);
										if (isPrebooking || isPrebookVoiding)
										{
											foreach (var item in newTrans)
											{
												Append(summaryTran, item);
											}
										}
									}
									else
									{
										PostGeneralTax(je, apdoc, new_info, x, salestax, salestax.ExpenseAccountID, salestax.ExpenseSubID, true);
									}
								}
								else if (salestax.TaxType == CSTaxType.Sales && (x.TaxAmt ?? 0m) != 0m)
								{
									PostGeneralTax(je, apdoc, new_info, x, salestax, doc.RetainageAcctID, doc.RetainageSubID);
								}

								if (salestax.TaxType == CSTaxType.Use)
								{
									PostReverseTax(je, apdoc, new_info, x, salestax);
								}
							}
							else if (salestax.TaxType == CSTaxType.PerUnit)
							{
								bool isDebitTaxTran = IsDebitTaxTran(x);
								PostPerUnitTaxAmounts(je, apdoc, new_info, perUnitAggregatedTax: x, perUnitTax: salestax, isDebitTaxTran: isDebitTaxTran);
							}
							else
							{
								if (salestax.ReverseTax != true)
								{
									PostGeneralTax(je, apdoc, new_info, x, salestax);
								}
								else
								{
									PostReverseTax(je, apdoc, new_info, x, salestax);
								}

								if (salestax.DeductibleVAT == true)
								{
									if (salestax.ReportExpenseToSingleAccount == true)
									{
										PostTaxExpenseToSingleAccount(je, apdoc, new_info, x, salestax);
									}
									else if (salestax.TaxCalcType == CSTaxCalcType.Item)
									{
										IEnumerable<GLTran> newTrans = PostTaxExpenseToItemAccounts(je, apdoc, new_info, x, salestax, !isPrebooking && !isPrebookVoiding);
										if (isPrebooking || isPrebookVoiding)
										{
											foreach (var item in newTrans)
											{
												Append(summaryTran, item);
											}
										}
									}
								}
							}

							if (apdoc.DocType == APDocType.QuickCheck || apdoc.DocType == APDocType.VoidQuickCheck)
							{
								bool isCredit = !IsDebitTaxTran(x);
								PostReduceOnEarlyPaymentTran(je, apdoc, vend, new_info, isCredit, x.CuryTaxDiscountAmt ?? 0m, x.TaxDiscountAmt ?? 0m);
							}

							x.Released = true;
							x = APTaxTran_TranType_RefNbr.Update(x);

							if (PXAccess.FeatureInstalled<FeaturesSet.vATReporting>() && !_IsIntegrityCheck && !IsInvoiceReclassification &&
								(x.TaxType == TX.TaxType.PendingPurchase || x.TaxType == TX.TaxType.PendingSales))
							{
								Vendor vendor = PXSelect<Vendor, Where<Vendor.bAccountID,
									Equal<Required<Vendor.bAccountID>>>>.SelectSingleBound(this, null, x.VendorID);

								decimal mult = ReportTaxProcess.GetMultByTranType(BatchModule.AP, x.TranType);
								string reversalMethod = x.TranType == APDocType.QuickCheck || x.TranType == APDocType.VoidQuickCheck
									? SVATTaxReversalMethods.OnDocuments
									: vendor?.SVATReversalMethod;

								var pendingVatDocs = new List<APRegister>() { doc };
								if (_InstallmentType == TermsInstallmentType.Multiple)
								{
									pendingVatDocs = ret;
								}

								decimal taxableInstallmentsTotal = 0;
								decimal taxInstallmentsTotal = 0;
								decimal curyTaxableInstallmentsTotal = 0;
								decimal curyTaxInstallmentsTotal = 0;

								SVATConversionHist biggestSVAT = null;
								for (int i = 0; i < pendingVatDocs.Count; i++)
								{
									var document = pendingVatDocs[i];
									SVATConversionHist histSVAT = new SVATConversionHist
									{
										Module = BatchModule.AP,
										AdjdBranchID = x.BranchID,
										AdjdDocType = x.TranType,
										AdjdRefNbr = document.RefNbr,
										AdjgDocType = x.TranType,
										AdjgRefNbr = document.RefNbr,
										AdjdDocDate = document.DocDate,
										TaxID = x.TaxID,
										TaxType = x.TaxType,
										TaxRate = x.TaxRate,
										VendorID = x.VendorID,
										ReversalMethod = reversalMethod,
										CuryInfoID = x.CuryInfoID,
									};

									decimal installmentPct = (doc.CuryOrigDocAmt != 0m ? document.CuryOrigDocAmt / doc.CuryOrigDocAmt : 0) ?? 0m;
									histSVAT.FillAmounts(GetExtension<MultiCurrency>().GetCurrencyInfo(x.CuryInfoID), x.CuryTaxableAmt, x.CuryTaxAmt, installmentPct * mult);

									FinPeriodIDAttribute.SetPeriodsByMaster<SVATConversionHist.adjdFinPeriodID>(SVATConversionHistory.Cache, histSVAT, doc.TranPeriodID);

									taxableInstallmentsTotal += histSVAT.TaxableAmt.Value;
									taxInstallmentsTotal += histSVAT.TaxAmt.Value;
									curyTaxableInstallmentsTotal += histSVAT.CuryTaxableAmt.Value;
									curyTaxInstallmentsTotal += histSVAT.CuryTaxAmt.Value;

									histSVAT = SVATConversionHistory.Insert(histSVAT);
									biggestSVAT = biggestSVAT == null || (histSVAT.CuryTaxAmt > biggestSVAT.CuryTaxAmt) ? histSVAT : biggestSVAT;
								}

								var taxableAmtDiff = (x.TaxableAmt * mult) - taxableInstallmentsTotal;
								var taxAmtDiff = (x.TaxAmt * mult) - taxInstallmentsTotal;
								//Set base currency leftovers
								if (taxableAmtDiff != 0 || taxAmtDiff != 0)
								{
									biggestSVAT.TaxableAmt += taxableAmtDiff;
									biggestSVAT.TaxAmt += taxAmtDiff;
									biggestSVAT.UnrecognizedTaxAmt = biggestSVAT.TaxAmt;
									biggestSVAT = SVATConversionHistory.Update(biggestSVAT);
								}

								var curyTaxableAmtDiff = (x.CuryTaxableAmt * mult) - curyTaxableInstallmentsTotal;
								var curyTaxAmtDiff = (x.CuryTaxAmt * mult) - curyTaxInstallmentsTotal;
								//Set currency leftovers
								if (curyTaxableAmtDiff != 0 || curyTaxAmtDiff != 0)
								{
									biggestSVAT.CuryTaxableAmt += curyTaxableAmtDiff;
									biggestSVAT.CuryTaxAmt += curyTaxAmtDiff;
									biggestSVAT.CuryUnrecognizedTaxAmt = biggestSVAT.CuryTaxAmt;
									biggestSVAT = SVATConversionHistory.Update(biggestSVAT);
								}
							}
						}

						if (isPrebooking || isPrebookVoiding)
						{
							PostGraph.NormalizeAmounts(summaryTran);
							InsertInvoiceTaxTransaction(je, summaryTran,
								new GLTranInsertionContext { APRegisterRecord = doc });
						}
					}
				}

				//Process APTranPost
				ProcessOriginTranPost(apdoc,masterInstallment == true);
				if (apdoc.IsChildRetainageDocument())
					ProcessRetainageTranPost(apdoc);

				if (!_IsIntegrityCheck && !IsInvoiceReclassification)
				{
					foreach (PXResult<APAdjust, APPayment> appres in PXSelectJoin<APAdjust, InnerJoin<APPayment, On<APPayment.docType, Equal<APAdjust.adjgDocType>, And<APPayment.refNbr, Equal<APAdjust.adjgRefNbr>>>>, Where<APAdjust.adjdDocType, Equal<Required<APAdjust.adjdDocType>>, And<APAdjust.adjdRefNbr, Equal<Required<APAdjust.adjdRefNbr>>, And<APAdjust.released, Equal<False>, And<APPayment.released, Equal<True>>>>>>.Select(this, doc.DocType, doc.RefNbr))
					{
						APAdjust adj = (APAdjust)appres;
						APPayment payment = (APPayment)appres;

						if (((APAdjust)appres).CuryAdjdAmt > 0m)
						{
							if (_InstallmentType != TermsInstallmentType.Single)
							{
								throw new PXException(Messages.PrepaymentAppliedToMultiplyInstallments);
							}

							if (ret == null)
							{
								ret = new List<APRegister>();
							}

							//are always greater then payments period
							payment.AdjDate = adj.AdjdDocDate;
						    FinPeriodIDAttribute.SetPeriodsByMaster<APPayment.adjFinPeriodID>(APPayment_DocType_RefNbr.Cache, payment, adj.AdjdTranPeriodID);

							ret.Add(payment);

							APPayment_DocType_RefNbr.Cache.Update(payment);

							adj.AdjAmt += adj.RGOLAmt;
							adj.RGOLAmt = -adj.RGOLAmt;
							adj.Hold = false;

							APAdjust_AdjgDocType_RefNbr_VendorID.Cache.SetStatus(adj, PXEntryStatus.Updated);
						}
					}

					// We should add a DebitAdj document to the list
					// to release further its payment part and increment
					// an adjustments counter.
					//
					if (doc.DocType == APDocType.DebitAdj)
					{
						if (ret == null)
						{
							ret = new List<APRegister>();
						}

						ret.Add(doc);
					}
				}

				Batch apbatch = je.BatchModule.Current;

				ReleaseInvoiceBatchPostProcessing(je, apdoc, apbatch);

				decimal curyCreditDiff = Math.Round((decimal)(apbatch.CuryDebitTotal - apbatch.CuryCreditTotal), 4);
				decimal creditDiff = Math.Round((decimal)(apbatch.DebitTotal - apbatch.CreditTotal), 4, MidpointRounding.AwayFromZero);

				if (docInclTaxDiscrepancy != 0)
				{
					ProcessTaxDiscrepancy(je, apbatch, apdoc, new_info, docInclTaxDiscrepancy);
					curyCreditDiff = Math.Round((decimal)(apbatch.CuryDebitTotal - apbatch.CuryCreditTotal), 4);
					creditDiff = Math.Round((decimal)(apbatch.DebitTotal - apbatch.CreditTotal), 4, MidpointRounding.AwayFromZero);
				}

				if (Math.Abs(curyCreditDiff) >= 0.00005m)
				{
					VerifyRoundingAllowed(apdoc, apbatch, je.currencyInfo.BaseCuryID);
				}

				if (Math.Abs(curyCreditDiff) >= 0.00005m || Math.Abs(creditDiff) >= 0.00005m)
				{
					ProcessInvoiceRounding(je, apbatch, apdoc, curyCreditDiff, creditDiff);
				}

				if (doc.HasZeroBalance<APRegister.curyDocBal, APTran.curyTranBal>(this) &&
					(!doc.IsOriginalRetainageDocument() || doc.HasZeroBalance<APRegister.curyRetainageUnreleasedAmt, APTran.curyRetainageBal>(this)) &&
					(!doc.IsOriginalRetainageDocument() || doc.DocType != APDocType.DebitAdj))
				{
					doc.DocBal = 0m;
					doc.CuryDiscBal = 0m;
					doc.DiscBal = 0m;

					doc.OpenDoc = false;
					doc.ClosedDate = doc.DocDate;
					doc.ClosedFinPeriodID = doc.FinPeriodID;
					doc.ClosedTranPeriodID = doc.TranPeriodID;
					RaiseInvoiceEvent(doc, APInvoice.Events.Select(ev => ev.CloseDocument));
				}
			}
			return ret;
		}

		private static decimal CalcDocInclTaxDiscrepancyForTran(APTaxTran x, Tax salestax)
		{
			decimal? amount = x.CuryTaxAmt - x.CuryTaxAmtSumm + x.CuryRetainedTaxAmt - x.CuryRetainedTaxAmtSumm;

			if (salestax.ReverseTax == true) return -amount.Value;
			else return amount.Value;
		}

		private void AdjustOriginalRetainageLineBalance(APRegister document, APTran tran, decimal? curyAmount, decimal? baseAmount)
		{
			APTran origRetainageTran = GetOriginalRetainageLine(document, tran);

			if (origRetainageTran != null)
			{
				origRetainageTran.CuryRetainageBal -= (curyAmount ?? 0m) * document.SignAmount;
				origRetainageTran.RetainageBal -= (baseAmount ?? 0m) * document.SignAmount;
				origRetainageTran = APTran_TranType_RefNbr.Update(origRetainageTran);

				if (!_IsIntegrityCheck &&
					(origRetainageTran.CuryRetainageBal < 0m || origRetainageTran.CuryRetainageBal > origRetainageTran.CuryOrigRetainageAmt))
				{
					throw new PXException(Messages.RetainageUnreleasedBalanceNegative);
				}
			}
		}

		protected virtual void AdjustmentProcessingOnApplication(APRegister paymentRegister, APAdjust adj)
		{
			if (paymentRegister.PaymentsByLinesAllowed == true)
			{
				// It is not possible to create Pay by Line DebitAdj
				// any more after the AC-141326 issue.
				ProcessPayByLineDebitAdjAdjustment(paymentRegister, adj);
			}
		}

		protected virtual IComparer<Tax> GetTaxComparer() => TaxByCalculationLevelAndTypeComparer.Instance;

		public class LineBalances : Tuple<Amount, Amount, Amount>
		{
			public Amount CashDiscountBalance { get { return Item1; } }
			public Amount RetainageBalance { get { return Item2; } }
			public Amount TranBalance { get { return Item3; } }

			public LineBalances(decimal? initValue)
				: base(new Amount(initValue, initValue),
					new Amount(initValue, initValue),
					new Amount(initValue, initValue))
			{
			}

			public LineBalances(Amount cashDiscountBalance, Amount retainageBalance, Amount tranBalance)
				: base(cashDiscountBalance, retainageBalance, tranBalance)
			{
			}

			public static LineBalances operator +(LineBalances a, LineBalances b)
			{
				return new LineBalances(
					a.CashDiscountBalance + b.CashDiscountBalance,
					a.RetainageBalance + b.RetainageBalance,
					a.TranBalance + b.TranBalance);
			}

			public static LineBalances operator -(LineBalances a, LineBalances b)
			{
				return new LineBalances(
					a.CashDiscountBalance - b.CashDiscountBalance,
					a.RetainageBalance - b.RetainageBalance,
					a.TranBalance - b.TranBalance);
			}
		}

		protected virtual LineBalances AdjustInvoiceDetailsBalanceByLine(APRegister doc, APTran tran)
		{
			// Retainage balance
			//
			tran.CuryOrigRetainageAmt += tran.CuryRetainageAmt;
			tran.OrigRetainageAmt += tran.RetainageAmt;

			// Transaction balance
			//
			tran.CuryOrigTranAmt += tran.CuryTranAmt;
			tran.OrigTranAmt += tran.TranAmt;

			// Cash Discount balance
			//
			decimal discountPercent = (doc.CuryOrigDocAmt ?? 0m) != 0m
				? (tran.CuryOrigTranAmt ?? 0m) / (doc.CuryOrigDocAmt ?? 0m)
				: 0m;
			tran.CuryCashDiscBal = CM.PXCurrencyAttribute.RoundCury(APTran_TranType_RefNbr.Cache, tran, (doc.CuryOrigDiscAmt ?? 0m) * discountPercent);
			tran.CashDiscBal = CM.PXCurrencyAttribute.RoundCury(APTran_TranType_RefNbr.Cache, tran, (doc.OrigDiscAmt ?? 0m) * discountPercent);

			return new LineBalances(
				new Amount(tran.CuryCashDiscBal ?? 0m, tran.CashDiscBal ?? 0m),
				new Amount(tran.CuryRetainageAmt ?? 0m, tran.RetainageAmt ?? 0m),
				new Amount(tran.CuryTranAmt ?? 0m, tran.TranAmt ?? 0m));
		}

		public static bool IncludeTaxInLineBalance(Tax tax)
		{
			return
				tax != null &&
				tax.TaxType != CSTaxType.Use &&
				tax.TaxType != CSTaxType.Withholding &&
				tax.TaxCalcLevel != CSTaxCalcLevel.Inclusive;
		}

		protected virtual LineBalances AdjustInvoiceDetailsBalanceByTax(
			APRegister doc,
			APTran tran,
			APTax aptax,
			Tax tax)
		{
			bool includeBalance =
				aptax?.TaxID != null &&
				IncludeTaxInLineBalance(tax);

			bool includeTax =
				aptax?.TaxID != null &&
				tax != null &&
				tax.TaxType != CSTaxType.Use;

			decimal sign = tax.ReverseTax == true ? -1m : 1m;

			decimal curyTaxAmt = (aptax.CuryTaxAmt ?? 0m) + (aptax.CuryExpenseAmt ?? 0m);
			decimal baseTaxAmt = (aptax.TaxAmt ?? 0m) + (aptax.ExpenseAmt ?? 0m);
			curyTaxAmt *= sign;
			baseTaxAmt *= sign;

			decimal curyRetainedTaxAmt = aptax.CuryRetainedTaxAmt ?? 0m;
			decimal baseRetainedTaxAmt = aptax.RetainedTaxAmt ?? 0m;
			curyRetainedTaxAmt *= sign;
			baseRetainedTaxAmt *= sign;

			LineBalances balances = includeBalance
				? new LineBalances(
					new Amount(0m, 0m),
					new Amount(curyRetainedTaxAmt, baseRetainedTaxAmt),
					new Amount(curyTaxAmt, baseTaxAmt))
				: new LineBalances(0m);

			// Retainage balance
			//
			tran.CuryRetainedTaxableAmt += includeTax ? aptax.CuryRetainedTaxableAmt ?? 0m : 0m;
			tran.RetainedTaxableAmt += includeTax ? aptax.RetainedTaxableAmt ?? 0m : 0m;
			tran.CuryRetainedTaxAmt += includeTax ? curyRetainedTaxAmt : 0m;
			tran.RetainedTaxAmt += includeTax ? baseRetainedTaxAmt : 0m;

			tran.CuryOrigRetainageAmt += balances.RetainageBalance.Cury;
			tran.OrigRetainageAmt += balances.RetainageBalance.Base;

			// Transaction balance
			//
			tran.CuryOrigTaxableAmt += includeTax ? aptax.CuryTaxableAmt ?? 0m : 0m;
			tran.OrigTaxableAmt += includeTax ? aptax.TaxableAmt ?? 0m : 0m;
			tran.CuryOrigTaxAmt += includeTax ? curyTaxAmt : 0m;
			tran.OrigTaxAmt += includeTax ? baseTaxAmt : 0m;

			tran.CuryOrigTranAmt += balances.TranBalance.Cury;
			tran.OrigTranAmt += balances.TranBalance.Base;

			return balances;
		}

		[Obsolete("This validation is needed for landed cost upgrade case, when AP Bill with LC trans is not released (see AC-111467, remove in 2020R2).")]
		protected virtual void ValidateLandedCostTran(APRegister doc, PXResultset<APTran> apTranResultSet)
		{
			var hasLC = apTranResultSet.RowCast<APTran>().Any(t => !String.IsNullOrEmpty(t.LCRefNbr));

			if (hasLC)
			{
				var lcCheckQuery = new PXSelectJoin<POLandedCostDoc, InnerJoin<APTran, On<APTran.lCDocType, Equal<POLandedCostDoc.docType>, And<APTran.lCRefNbr, Equal<POLandedCostDoc.refNbr>>>>,
					Where<APTran.tranType, Equal<Required<APTran.tranType>>, And<APTran.refNbr, Equal<Required<APTran.refNbr>>, And<POLandedCostDoc.released, Equal<False>>>>>(this);

				var lc = lcCheckQuery.SelectWindowed(0, 1, doc.DocType, doc.RefNbr);

				if (lc.Count > 0)
				{
					throw new PXException(Messages.LandedCostDocNotReleased);
				}
			}
		}

		protected virtual void CheckVoidQuickCheckAmountDiscrepancies(APRegister document)
		{
			if (document.DocType == APDocType.VoidQuickCheck)
			{
				APRegister origDoc = APRegister.PK.Find(this, APDocType.QuickCheck, document.RefNbr);

				if (document.OrigDocAmt != origDoc.OrigDocAmt)
				{
					throw new ReleaseException(Common.Messages.AmountDifferFromDocumentBeingVoided, document.RefNbr, APDocType.GetDisplayName(document.DocType));
				}
			}
		}

		private void ProcessInvoiceRounding(
			JournalEntry je,
			Batch apbatch,
			APInvoice apdoc,
			decimal curyCreditDiff,
			decimal creditDiff)
                {
			Currency currency = PXSelect<Currency, Where<Currency.curyID, Equal<Required<CurrencyInfo.curyID>>>>.Select(this, apdoc.CuryID);

			if (currency.RoundingGainAcctID == null || currency.RoundingGainSubID == null)
			{
				throw new PXException(Messages.NoRoundingGainLossAccSub, currency.CuryID);
			}

			if (curyCreditDiff != 0m)
			{
                    GLTran tran = new GLTran();
                    tran.SummPost = true;
                    tran.BranchID = apdoc.BranchID;

				if (Math.Sign(curyCreditDiff) == 1)
	                {
					tran.AccountID = currency.RoundingGainAcctID;
					tran.SubID = CM.GainLossSubAccountMaskAttribute.GetSubID<Currency.roundingGainSubID>(je, tran.BranchID, currency);
                        tran.CuryDebitAmt = 0m;
					tran.CuryCreditAmt = Math.Abs(curyCreditDiff);
                    }
                    else
                    {
					tran.AccountID = currency.RoundingLossAcctID;
					tran.SubID = CM.GainLossSubAccountMaskAttribute.GetSubID<Currency.roundingLossSubID>(je, tran.BranchID, currency);
					tran.CuryDebitAmt = Math.Abs(curyCreditDiff);
                        tran.CuryCreditAmt = 0m;
                    }

                    tran.CreditAmt = 0m;
                    tran.DebitAmt = 0m;
				tran.TranType = apdoc.DocType;
				tran.RefNbr = apdoc.RefNbr;
                    tran.TranClass = GLTran.tranClass.Normal;
                    tran.TranDesc = GL.Messages.RoundingDiff;
                    tran.LedgerID = apbatch.LedgerID;
			    FinPeriodIDAttribute.SetPeriodsByMaster<GLTran.finPeriodID>(je.GLTranModuleBatNbr.Cache, tran, apbatch.TranPeriodID);
                    tran.TranDate = apdoc.DocDate;
                    tran.ReferenceID = apdoc.VendorID;
				tran.Released = true;

				CM.CurrencyInfo infocopy = new CM.CurrencyInfo();
                    infocopy = je.currencyinfo.Insert(infocopy) ?? infocopy;

                    tran.CuryInfoID = infocopy.CuryInfoID;
					InsertInvoiceRoundingTransaction(je, tran,
						new GLTranInsertionContext { APRegisterRecord = apdoc });
                }

			if (creditDiff != 0m)
				{
					GLTran tran = new GLTran();
					tran.SummPost = true;
					tran.BranchID = apdoc.BranchID;

				if (Math.Sign(creditDiff) == 1)
					{
					tran.AccountID = currency.RoundingGainAcctID;
					tran.SubID = CM.GainLossSubAccountMaskAttribute.GetSubID<Currency.roundingGainSubID>(je, tran.BranchID, currency);
					tran.CreditAmt = Math.Abs(creditDiff);
						tran.DebitAmt = 0m;
					}
					else
					{
					tran.AccountID = currency.RoundingLossAcctID;
					tran.SubID = CM.GainLossSubAccountMaskAttribute.GetSubID<Currency.roundingLossSubID>(je, tran.BranchID, currency);
                        tran.CreditAmt = 0m;
					tran.DebitAmt = Math.Abs(creditDiff);
					}

					tran.CuryCreditAmt = 0m;
					tran.CuryDebitAmt = 0m;
				tran.TranType = apdoc.DocType;
				tran.RefNbr = apdoc.RefNbr;
					tran.TranClass = GLTran.tranClass.Normal;
					tran.TranDesc = GL.Messages.RoundingDiff;
					tran.LedgerID = apbatch.LedgerID;
				    FinPeriodIDAttribute.SetPeriodsByMaster<GLTran.finPeriodID>(je.GLTranModuleBatNbr.Cache, tran, apbatch.TranPeriodID);
					tran.TranDate = apdoc.DocDate;
					tran.ReferenceID = apdoc.VendorID;
				tran.Released = true;

				CM.CurrencyInfo infocopy = new CM.CurrencyInfo();
					infocopy = je.currencyinfo.Insert(infocopy) ?? infocopy;

					tran.CuryInfoID = infocopy.CuryInfoID;
					InsertInvoiceRoundingTransaction(je, tran,
						new GLTranInsertionContext { APRegisterRecord = apdoc });
				}
				}

		protected virtual void ProcessTaxDiscrepancy(
			JournalEntry je,
			Batch arbatch,
			APInvoice apdoc,
			CurrencyInfo currencyInfo,
			decimal docInclTaxDiscrepancy)
		{
			if (docInclTaxDiscrepancy == 0) return;
			decimal? roundinglimit = CM.CurrencyCollection.GetCurrency(currencyInfo.BaseCuryID).RoundingLimit;

			if (Math.Abs(docInclTaxDiscrepancy) > roundinglimit &&
					(PXAccess.FeatureInstalled<FeaturesSet.netGrossEntryMode>() || PXAccess.FeatureInstalled<FeaturesSet.invoiceRounding>())
				)
				{
					throw new PXException(AP.Messages.RoundingAmountTooBig,
						je.currencyinfo.Current.BaseCuryID,
						Math.Abs(Math.Round(docInclTaxDiscrepancy, currencyInfo.CuryPrecision ?? 4)),
					IN.PXDBQuantityAttribute.Round(roundinglimit));
				}

				TXSetup txsetup = PXSetup<TXSetup>.Select(this);
				if (txsetup?.TaxRoundingGainAcctID == null || txsetup?.TaxRoundingLossAcctID == null)
				{
					throw new PXException(TX.Messages.TaxRoundingGainLossAccountsRequired);
				}

				var roundAcctID = docInclTaxDiscrepancy > 0 ? txsetup.TaxRoundingGainAcctID : txsetup.TaxRoundingLossAcctID;
				var roundSubID = docInclTaxDiscrepancy > 0 ? txsetup.TaxRoundingGainSubID : txsetup.TaxRoundingLossSubID;
				var isDebit = (apdoc.DrCr == DrCr.Debit);

			GLTran diffTran = new GLTran
			{
				SummPost = this.SummPost,
				BranchID = apdoc.BranchID,
				CuryInfoID = currencyInfo.CuryInfoID,
				TranType = apdoc.DocType,
				TranClass = GLTran.tranClass.RealizedAndRoundingGOL,
				RefNbr = apdoc.RefNbr,
				TranDate = apdoc.DocDate,
				AccountID = roundAcctID,
				SubID = roundSubID,
				TranDesc = TX.Messages.DocumentInclusiveTaxDiscrepancy,
				CuryDebitAmt = !isDebit ? docInclTaxDiscrepancy : 0m,
				DebitAmt = !isDebit ? docInclTaxDiscrepancy : 0m,
				CuryCreditAmt = !isDebit ? 0m : docInclTaxDiscrepancy,
				CreditAmt = !isDebit ? 0m : docInclTaxDiscrepancy,
				Released = true,
				ReferenceID = apdoc.VendorID
			};

				InsertInvoiceTransaction(je, diffTran, new GLTranInsertionContext { APRegisterRecord = apdoc });
			}

		/// <summary>
		/// Extension point for AP Release Invoice process. This method is called after GL Batch was created and all main GL Transactions have been
		/// inserted, but before Invoice rounding transaction, or RGOL transaction has been inserted.
		/// </summary>
		/// <param name="je">Journal Entry graph used for posting</param>
		/// <param name="apdoc">Orginal AP Invoice</param>
		/// <param name="apbatch">GL Batch that was created for Invoice</param>
		public virtual void ReleaseInvoiceBatchPostProcessing(JournalEntry je, APInvoice apdoc, Batch apbatch)
		{

		}

		[Obsolete(Common.Messages.MethodIsObsoleteRemoveInLaterAcumaticaVersions)]
		public virtual void ReleaseInvoiceTransactionPostProcessing(JournalEntry je, APInvoice apdoc, PXResult<APTran, APTax, Tax, DRDeferredCode, LandedCostCode, InventoryItem> r, GLTran tran)
		{
		}

		/// <summary>
		/// Extension point for AP Release Invoice process. This method is called after transaction amounts have been calculated, but before it was inserted.
		/// </summary>
		/// <param name="je">Journal Entry graph used for posting</param>
		/// <param name="apdoc">Orginal AP Invoice</param>
		/// <param name="r">Document line with joined supporting entities</param>
		/// <param name="tran">Transaction that was created for APTran. This transaction has not been inserted yet.</param>
		public virtual void ReleaseInvoiceTransactionPostProcessing(JournalEntry je, APInvoice apdoc, PXResult<APTran, APTax, Tax, DRDeferredCode, LandedCostCode, InventoryItem, APTaxTran> r, GLTran tran)
		{
			var oldResult = new PXResult<APTran, APTax, Tax, DRDeferredCode, LandedCostCode, InventoryItem>(r, r, r, r, r, r);
			ReleaseInvoiceTransactionPostProcessing(je, apdoc, oldResult, tran);
		}

		/// <summary>
		/// Extension point for AP Release Invoice process. This method is called after transaction amounts have been calculated, but before it was inserted.
		/// </summary>
		public virtual void InvoiceTransactionReleasing(InvoiceTransactionReleasingArgs invoiceTransactionReleasing)
		{

		}

		/// <summary>
		/// Extension point for AP Release Invoice process. This method is called after transactions have been processed for release
		/// </summary>
		public virtual void InvoiceTransactionsReleased(InvoiceTransactionsReleasedArgs doc)
		{

		}

		protected virtual IEnumerable<GLTran> PostTaxExpenseToItemAccounts(JournalEntry je, APInvoice apdoc, CurrencyInfo new_info, APTaxTran x, Tax salestax, bool doInsert)
		{
			if (apdoc.IsOriginalRetainageDocument() &&
				apdoc.CuryRetainedTaxTotal != 0m)
			{
				throw new PXException(TX.Messages.CannotPostRetainedTaxExpenseToItemAccounts);
			}

			PXResultset<APTax> deductibleLines = GetDeductibleLines(salestax, x);

			if (apdoc.PaymentsByLinesAllowed != true)
			{
				APTaxAttribute apTaxAttr = new APTaxAttribute(typeof(APRegister), typeof(APTax), typeof(APTaxTran))
				{
					Inventory = typeof(APTran.inventoryID),
					UOM = typeof(APTran.uOM),
					LineQty = typeof(APTran.qty)
				};

				apTaxAttr.DistributeTaxDiscrepancy<APTax, APTax.curyExpenseAmt, APTax.expenseAmt>(this, deductibleLines.FirstTableItems, x.CuryExpenseAmt.Value);
			}

			List<GLTran> newTrans = new List<GLTran>();
			bool isDebit = IsDebitTaxTran(x);

			foreach (PXResult<APTax, APTran> item in deductibleLines)
			{
				APTax taxLine = (APTax)item;
				APTran apTran = (APTran)item;

				GLTran tran = new GLTran();
				tran.SummPost = this.SummPost;
				tran.BranchID = apTran.BranchID;
				tran.CuryInfoID = new_info.CuryInfoID;
				tran.TranType = x.TranType;
				tran.TranClass = GLTran.tranClass.Tax;
				tran.RefNbr = x.RefNbr;
				tran.TranDate = x.TranDate;
				GetItemCostTaxAccount(apdoc, salestax, apTran, x, out int? accountID, out int? subID);
				tran.AccountID = accountID;
				tran.SubID = subID;
				tran.TranDesc = salestax.TaxID;
				tran.TranLineNbr = apTran.LineNbr;
				tran.CuryDebitAmt = isDebit ? taxLine.CuryExpenseAmt : 0m;
				tran.DebitAmt = isDebit ? taxLine.ExpenseAmt : 0m;
				tran.CuryCreditAmt = isDebit ? 0m : taxLine.CuryExpenseAmt;
				tran.CreditAmt = isDebit ? 0m : taxLine.ExpenseAmt;
				tran.Released = true;
				tran.ReferenceID = apdoc.VendorID;
				tran.ProjectID = apTran.ProjectID;
				tran.TaskID = apTran.TaskID;
				tran.CostCodeID = apTran.CostCodeID;

				newTrans.Add(tran);
				if (doInsert)
				{
					tran = InsertInvoiceTaxExpenseItemAccountsTransaction(je, tran,
						new GLTranInsertionContext { APRegisterRecord = apdoc, APTranRecord = apTran, APTaxTranRecord = x });

					//SelectJoin doesn't merge caches for joined tables
					apTran = APTran_TranType_RefNbr.Locate(apTran) ?? apTran;

					apTran.ExpenseAmt += taxLine.ExpenseAmt;
					apTran.CuryExpenseAmt += taxLine.CuryExpenseAmt;

					APTran_TranType_RefNbr.Update(apTran);
				}
			}

			return newTrans;
        }

		private bool IsPostUseAndSalesTaxesByProjectKey(PXGraph graph, Tax tax)
		{
			bool postByProjectKey = true;

			if (tax.ReportExpenseToSingleAccount == true)
			{
				Account account = PXSelect<Account, Where<Account.accountID, Equal<Required<Account.accountID>>>>.SelectSingleBound(graph, null, tax.ExpenseAccountID);
				postByProjectKey = account?.AccountGroupID != null;
			}

			return postByProjectKey;
		}

		protected virtual IEnumerable<GLTran> PostTaxAmountByProjectKey(
			JournalEntry je,
			APInvoice apDoc,
			CurrencyInfo curyInfo,
			APTaxTran apTaxTran,
			Tax tax,
			bool doInsert,
			bool addRetTaxAmt = false)
		{
			bool isDebit = IsDebitTaxTran(apTaxTran);
			PXResultset<APTax> deductibleLines = GetDeductibleLines(tax, apTaxTran);

			if (apDoc.PaymentsByLinesAllowed != true)
			{
				APTaxAttribute apTaxAttr = new APTaxAttribute(typeof(APRegister), typeof(APTax), typeof(APTaxTran))
				{
					Inventory = typeof(APTran.inventoryID),
					UOM = typeof(APTran.uOM),
					LineQty = typeof(APTran.qty)
				};

				apTaxAttr.DistributeTaxDiscrepancy<APTax, APTax.curyTaxAmt, APTax.taxAmt>(this, deductibleLines.FirstTableItems, apTaxTran.CuryTaxAmt.Value);
			}

			if (addRetTaxAmt && apDoc.PaymentsByLinesAllowed != true)
			{
				var apRetainedTaxAttr = new APRetainedTaxAttribute(typeof(APRegister), typeof(APTax), typeof(APTaxTran));
				apRetainedTaxAttr.DistributeTaxDiscrepancy<APTax, APTax.curyRetainedTaxAmt, APTax.retainedTaxAmt>(this, deductibleLines.FirstTableItems, apTaxTran.CuryRetainedTaxAmt.Value, true);
			}

			var newTrans = new Dictionary<ProjectKey, GLTran>();
			var apTranByLineNbr = new Dictionary<int?, APTran>();

			foreach (PXResult<APTax, APTran> item in deductibleLines)
			{
				APTax taxLine = (APTax)item;
				APTran apTran = (APTran)item;

				int? accountID = tax.ExpenseAccountID;
				int? subID = tax.ExpenseSubID;
				if (tax.ReportExpenseToSingleAccount != true)
					GetItemCostTaxAccount(apDoc, tax, apTran, apTaxTran, out accountID, out subID);

				var projectKey = new ProjectKey(
					apTran.BranchID,
					accountID,
					subID,
					apTran.ProjectID,
					apTran.TaskID,
					apTran.CostCodeID,
					apTran.InventoryID);

				var curyTaxAmt = (taxLine.CuryTaxAmt ?? 0) + (addRetTaxAmt ? (taxLine.CuryRetainedTaxAmt ?? 0) : 0m);
				var taxAmt = (taxLine.TaxAmt ?? 0) + (addRetTaxAmt ? (taxLine.RetainedTaxAmt ?? 0) : 0m);

				if (newTrans.TryGetValue(projectKey, out GLTran tran))
				{
					tran.TranLineNbr = null;
					tran.Qty = (tran.Qty ?? 0) + (apTran.Qty ?? 0);
					tran.CuryDebitAmt = (tran.CuryDebitAmt ?? 0) + (isDebit ? curyTaxAmt : 0m);
					tran.DebitAmt = (tran.DebitAmt ?? 0) + (isDebit ? taxAmt : 0m);
					tran.CuryCreditAmt = (tran.CuryCreditAmt ?? 0) + (isDebit ? 0m : curyTaxAmt);
					tran.CreditAmt = (tran.CreditAmt ?? 0) +  (isDebit ? 0m : taxAmt);
				}
				else
				{
					tran = new GLTran();
					tran.SummPost = this.SummPost;
					tran.BranchID = apTran.BranchID;
					tran.CuryInfoID = curyInfo.CuryInfoID;
					tran.TranType = apTaxTran.TranType;
					tran.TranClass = GLTran.tranClass.Tax;
					tran.RefNbr = apTaxTran.RefNbr;
					tran.TranDate = apTaxTran.TranDate;
					tran.AccountID = accountID;
					tran.SubID = subID;
					tran.TranDesc = tax.TaxID;
					tran.TranLineNbr = apTran.LineNbr;
					tran.CuryDebitAmt = isDebit ? curyTaxAmt : 0m;
					tran.DebitAmt = isDebit ? taxAmt : 0m;
					tran.CuryCreditAmt = isDebit ? 0m : curyTaxAmt;
					tran.CreditAmt = isDebit ? 0m : taxAmt;
					tran.Released = true;
					tran.ReferenceID = apDoc.VendorID;
					tran.ProjectID = apTran.ProjectID;
					tran.TaskID = apTran.TaskID;
					tran.CostCodeID = apTran.CostCodeID;

					tran.InventoryID = apTran.InventoryID;
					tran.Qty = apTran.Qty;
					tran.UOM = apTran.UOM;

					newTrans.Add(projectKey, tran);
					apTranByLineNbr.Add(apTran.LineNbr, apTran);
				}
			}

			if (doInsert)
			{
				foreach (var key in newTrans.Keys.ToList())
				{
					GLTran tran = newTrans[key];

					apTranByLineNbr.TryGetValue(tran.TranLineNbr ?? -1, out APTran apTran);
					newTrans[key] = InsertInvoiceTaxByProjectKeyTransaction(je, tran,
						new GLTranInsertionContext { APRegisterRecord = apDoc, APTaxTranRecord = apTaxTran, APTranRecord = apTran });
				}
			}

			return newTrans.Values;
		}

		protected virtual PXResultset<APTax> GetDeductibleLines(Tax salestax, APTaxTran x)
		{
			return PXSelectJoin<APTax,
				InnerJoin<APTran, On<APTax.tranType, Equal<APTran.tranType>,
					And<APTax.refNbr, Equal<APTran.refNbr>,
					And<APTax.lineNbr, Equal<APTran.lineNbr>>>>>,
				Where<APTax.taxID, Equal<Required<APTax.taxID>>,
					And<APTran.tranType, Equal<Required<APTran.tranType>>,
					And<APTran.refNbr, Equal<Required<APTran.refNbr>>>>>,
				OrderBy<Desc<APTax.curyTaxAmt>>>
				.Select(this, salestax.TaxID, x.TranType, x.RefNbr);
		}

		protected virtual void PostTaxExpenseToSingleAccount(JournalEntry je, APInvoice apdoc, CurrencyInfo new_info, APTaxTran x, Tax salestax)
		{
			bool isDebit = IsDebitTaxTran(x);

			GLTran tran = new GLTran
			{
				SummPost = this.SummPost,
				BranchID = x.BranchID,
				CuryInfoID = new_info.CuryInfoID,
				TranType = x.TranType,
				TranClass = GLTran.tranClass.Tax,
				RefNbr = x.RefNbr,
				TranDate = x.TranDate,
				AccountID = salestax.ExpenseAccountID,
				SubID = salestax.ExpenseSubID,
				TranDesc = salestax.TaxID,
				CuryDebitAmt = isDebit ? x.CuryExpenseAmt : 0m,
				DebitAmt = isDebit ? x.ExpenseAmt : 0m,
				CuryCreditAmt = isDebit ? 0m : x.CuryExpenseAmt,
				CreditAmt = isDebit ? 0m : x.ExpenseAmt,
				Released = true,
				ReferenceID = apdoc.VendorID,
				ProjectID = ProjectDefaultAttribute.NonProject(),
				CostCodeID = CostCodeAttribute.DefaultCostCode
			};

			InsertInvoiceTaxExpenseSingeAccountTransaction(je, tran,
				new GLTranInsertionContext { APRegisterRecord = apdoc, APTaxTranRecord = x });

			if (apdoc.IsChildRetainageDocument())
			{
				PostRetainedTax(je, apdoc, tran, x, salestax);
			}
		}

		protected virtual void PostReverseTax(JournalEntry je, APInvoice apdoc, CurrencyInfo new_info, APTaxTran x, Tax salestax)
		{
			bool isDebit = IsDebitTaxTran(x);

			GLTran tran = new GLTran
			{
				SummPost = this.SummPost,
				BranchID = x.BranchID,
				CuryInfoID = new_info.CuryInfoID,
				TranType = x.TranType,
				TranClass = GLTran.tranClass.Tax,
				RefNbr = x.RefNbr,
				TranDate = x.TranDate,
				AccountID = x.AccountID,
				SubID = x.SubID,
				TranDesc = salestax.TaxID,
				CuryDebitAmt = isDebit ? 0m : x.CuryTaxAmt,
				DebitAmt = isDebit ? 0m : x.TaxAmt,
				CuryCreditAmt = isDebit ? x.CuryTaxAmt : 0m,
				CreditAmt = isDebit ? x.TaxAmt : 0m,
				Released = true,
				ReferenceID = apdoc.VendorID,
				ProjectID = ProjectDefaultAttribute.NonProject(),
				CostCodeID = CostCodeAttribute.DefaultCostCode
			};

			InsertInvoiceReverseTaxTransaction(je, tran,
				new GLTranInsertionContext { APRegisterRecord = apdoc, APTaxTranRecord = x });

			PostRetainedTax(je, apdoc, tran, x, salestax, true);
		}

		protected virtual void PostGeneralTax(JournalEntry je, APInvoice apdoc, CurrencyInfo new_info, APTaxTran x, Tax salestax)
		{
			PostGeneralTax(je, apdoc, new_info, x, salestax, x.AccountID, x.SubID, false);
		}

		protected virtual void PostGeneralTax(
			JournalEntry je,
			APInvoice apdoc,
			CurrencyInfo new_info,
			APTaxTran x,
			Tax salestax,
			int? accountID = null,
			int? subID = null,
			bool addRetTaxAmount = false)
		{
			bool isDebit = IsDebitTaxTran(x);
			var curyTaxAmt = (x.CuryTaxAmt ?? 0) + (addRetTaxAmount ? (x.CuryRetainedTaxAmt ?? 0) : 0);
			var taxAmt = (x.TaxAmt ?? 0) + (addRetTaxAmount ? (x.RetainedTaxAmt ?? 0) : 0);

			GLTran tran = new GLTran
			{
				SummPost = this.SummPost,
				BranchID = x.BranchID,
				CuryInfoID = new_info.CuryInfoID,
				TranType = x.TranType,
				TranClass = GLTran.tranClass.Tax,
				RefNbr = x.RefNbr,
				TranDate = x.TranDate,
				AccountID = accountID ?? x.AccountID,
				SubID = subID ?? x.SubID,
				TranDesc = salestax.TaxID,
				CuryDebitAmt = isDebit ? curyTaxAmt : 0m,
				DebitAmt = isDebit ? taxAmt : 0m,
				CuryCreditAmt = isDebit ? 0m : curyTaxAmt,
				CreditAmt = isDebit ? 0m : taxAmt,
				Released = true,
				ReferenceID = apdoc.VendorID,
				ProjectID = ProjectDefaultAttribute.NonProject(),
				CostCodeID = CostCodeAttribute.DefaultCostCode
			};

			InsertInvoiceGeneralTaxTransaction(je, tran,
				new GLTranInsertionContext { APRegisterRecord = apdoc, APTaxTranRecord = x });

			PostRetainedTax(je, apdoc, tran, x, salestax);
		}

		public virtual bool IsDebitTaxTran(APTaxTran x)
		{
			bool isDebit = GetTaxDrCr(x.OrigTranType, x.TranType) == DrCr.Debit;
			return isDebit;
		}

		protected virtual void PostRetainedTax(
			JournalEntry je,
			APInvoice apdoc,
			GLTran origTran,
			APTaxTran x,
			Tax salestax,
			bool isReversedTran = false)
		{
			bool isReversedTax = salestax.ReverseTax == true;
			bool isUseTax = salestax.TaxType == CSTaxType.Use;
			bool isSalesTax = salestax.TaxType == CSTaxType.Sales;

			bool isOriginalRetainageDocumentWithTax = apdoc.IsOriginalRetainageDocument() && x.CuryRetainedTaxAmt != 0m && !isSalesTax;
			bool isChildRetainageDocumentWithTax = apdoc.IsChildRetainageDocument() && x.CuryTaxAmt != 0m && !isSalesTax;

			if (isOriginalRetainageDocumentWithTax || isChildRetainageDocumentWithTax)
			{
				RetainageTaxCheck(salestax);
			}

			if (isOriginalRetainageDocumentWithTax)
			{
				if (isUseTax && isReversedTran)
				{
					bool needCreditRetainageTaxPayableAcct = IsDebitTaxTran(x) && !isReversedTax;

					GLTran retainedTaxTran = PXCache<GLTran>.CreateCopy(origTran);
					retainedTaxTran.ReclassificationProhibited = true;
					retainedTaxTran.AccountID = salestax.RetainageTaxPayableAcctID;
					retainedTaxTran.SubID = salestax.RetainageTaxPayableSubID;
					retainedTaxTran.CuryDebitAmt = needCreditRetainageTaxPayableAcct ? 0m : x.CuryRetainedTaxAmt;
					retainedTaxTran.DebitAmt = needCreditRetainageTaxPayableAcct ? 0m : x.RetainedTaxAmt;
					retainedTaxTran.CuryCreditAmt = needCreditRetainageTaxPayableAcct ? x.CuryRetainedTaxAmt : 0m;
					retainedTaxTran.CreditAmt = needCreditRetainageTaxPayableAcct ? x.RetainedTaxAmt : 0m;
					je.GLTranModuleBatNbr.Insert(retainedTaxTran);
				}
				else if (!isUseTax)
				{
					bool isDebit = IsDebitTaxTran(x) && !isReversedTax;

					GLTran retainedTaxTran = PXCache<GLTran>.CreateCopy(origTran);
					retainedTaxTran.ReclassificationProhibited = true;
					retainedTaxTran.AccountID = salestax.RetainageTaxClaimableAcctID;
					retainedTaxTran.SubID = salestax.RetainageTaxClaimableSubID;
					retainedTaxTran.CuryDebitAmt = isDebit ? x.CuryRetainedTaxAmt : 0m;
					retainedTaxTran.DebitAmt = isDebit ? x.RetainedTaxAmt : 0m;
					retainedTaxTran.CuryCreditAmt = isDebit ? 0m : x.CuryRetainedTaxAmt;
					retainedTaxTran.CreditAmt = isDebit ? 0m : x.RetainedTaxAmt;
					je.GLTranModuleBatNbr.Insert(retainedTaxTran);
				}
			}
			else if (isChildRetainageDocumentWithTax)
			{
				if (isUseTax && isReversedTran)
				{
					GLTran retainedTaxTran = PXCache<GLTran>.CreateCopy(origTran);
					retainedTaxTran.ReclassificationProhibited = true;
					retainedTaxTran.AccountID = salestax.RetainageTaxPayableAcctID;
					retainedTaxTran.SubID = salestax.RetainageTaxPayableSubID;
					retainedTaxTran.CuryDebitAmt = origTran.CuryCreditAmt;
					retainedTaxTran.DebitAmt = origTran.CreditAmt;
					retainedTaxTran.CuryCreditAmt = origTran.CuryDebitAmt;
					retainedTaxTran.CreditAmt = origTran.DebitAmt;
					je.GLTranModuleBatNbr.Insert(retainedTaxTran);
				}
				else
				{
					GLTran retainedTaxTran = PXCache<GLTran>.CreateCopy(origTran);
					retainedTaxTran.ReclassificationProhibited = true;
					retainedTaxTran.AccountID = salestax.RetainageTaxClaimableAcctID;
					retainedTaxTran.SubID = salestax.RetainageTaxClaimableSubID;
					retainedTaxTran.CuryDebitAmt = origTran.CuryCreditAmt;
					retainedTaxTran.DebitAmt = origTran.CreditAmt;
					retainedTaxTran.CuryCreditAmt = origTran.CuryDebitAmt;
					retainedTaxTran.CreditAmt = origTran.DebitAmt;
					je.GLTranModuleBatNbr.Insert(retainedTaxTran);

					GLTran retainageTran = PXCache<GLTran>.CreateCopy(origTran);
					retainageTran.ReclassificationProhibited = true;
					retainageTran.SummPost = true;
					retainageTran.AccountID = apdoc.RetainageAcctID;
					retainageTran.SubID = apdoc.RetainageSubID;
					retainageTran.CuryDebitAmt = origTran.CuryDebitAmt;
					retainageTran.DebitAmt = origTran.DebitAmt;
					retainageTran.CuryCreditAmt = origTran.CuryCreditAmt;
					retainageTran.CreditAmt = origTran.CreditAmt;
					je.GLTranModuleBatNbr.Insert(retainageTran);
				}
			}
		}

		protected virtual void PostDirectTax(CurrencyInfo info, APTaxTran x, APInvoice orig_doc)
		{
			APTaxTran tran = PXSelect<APTaxTran,
				Where<APTaxTran.tranType, Equal<Required<APTaxTran.tranType>>,
					And<APTaxTran.refNbr, Equal<Required<APTaxTran.refNbr>>,
					And<APTaxTran.taxID, Equal<Required<APTaxTran.taxID>>,
					And<APTaxTran.module, Equal<BatchModule.moduleAP>>>>>>
				.Select(this, x.OrigTranType, x.OrigRefNbr, x.TaxID);

			if (tran == null)
			{
				tran = PXCache<APTaxTran>.CreateCopy(x);
				tran.TranType = x.OrigTranType;
				tran.RefNbr = x.OrigRefNbr;
				tran.OrigTranType = null;
				tran.OrigRefNbr = null;
				tran.CuryInfoID = orig_doc.CuryInfoID;
				tran.TaxableAmt = 0m;
				tran.CuryTaxableAmt = 0m;
				tran.TaxAmt = 0m;
				tran.CuryTaxAmt = 0m;
				tran.Released = true;
				tran.TranDate = orig_doc.DocDate;
			    FinPeriodIDAttribute.SetPeriodsByMaster<APTaxTran.finPeriodID>(APTaxTran_TranType_RefNbr.Cache, tran, orig_doc.TranPeriodID);

				tran = PXCache<APTaxTran>.CreateCopy(APTaxTran_TranType_RefNbr.Insert(tran));
			}

			if (string.IsNullOrEmpty(tran.TaxPeriodID) == false)
			{
				throw new PXException(TX.Messages.CannotAdjustTaxForClosedOrPreparedPeriod, APTaxTran_TranType_RefNbr.Cache.GetValueExt<APTaxTran.taxPeriodID>(tran));
			}

			decimal sign =
				tran.TranType == APDocType.DebitAdj && x.TranType != APDocType.DebitAdj ||
				tran.TranType != APDocType.DebitAdj && x.TranType == APDocType.DebitAdj
					? -1m
					: 1m;

				tran.TaxZoneID = x.TaxZoneID;
			tran.CuryTaxableAmt += x.CuryTaxableAmt * sign;
			tran.CuryTaxAmt += x.CuryTaxAmt * sign;
			tran.TaxableAmt += x.TaxableAmt * sign;
			tran.TaxAmt += x.TaxAmt * sign;

			#region Retainage part

			tran.CuryRetainedTaxableAmt += x.CuryRetainedTaxableAmt * sign;
			tran.CuryRetainedTaxAmt += x.CuryRetainedTaxAmt * sign;
			tran.RetainedTaxableAmt += x.RetainedTaxableAmt * sign;
			tran.RetainedTaxAmt += x.RetainedTaxAmt * sign;

			#endregion

			CurrencyInfo orig_info = CurrencyInfo_CuryInfoID.Select(tran.CuryInfoID);

			if (orig_info != null && string.Equals(orig_info.CuryID, info.CuryID) == false)
			{
				CM.PXCurrencyAttribute.CuryConvCury<APTaxTran.curyTaxableAmt>(APTaxTran_TranType_RefNbr.Cache, tran);
				CM.PXCurrencyAttribute.CuryConvCury<APTaxTran.curyTaxAmt>(APTaxTran_TranType_RefNbr.Cache, tran);
			}

			APTaxTran_TranType_RefNbr.Update(tran);
		}

		protected virtual void RetainageTaxCheck(Tax tax)
		{
			if (tax.TaxType == CSTaxType.Use)
			{
				TaxAccountCheck<Tax.retainageTaxPayableAcctID>(tax);
				TaxAccountCheck<Tax.retainageTaxPayableSubID>(tax);
			}
			else
			{
				TaxAccountCheck<Tax.retainageTaxClaimableAcctID>(tax);
				TaxAccountCheck<Tax.retainageTaxClaimableSubID>(tax);
			}
		}

		private void TaxAccountCheck<Field>(Tax tax)
			where Field : IBqlField
		{
			Type table = BqlCommand.GetItemType(typeof(Field));
			var account = Caches[table].GetValue(tax, typeof(Field).Name);
			if (account == null)
			{
				throw new ReleaseException(
					AP.Messages.TaxAccountNotFound,
					PXUIFieldAttribute.GetDisplayName<Field>(Caches[typeof(Tax)]), tax.TaxID);
			}
		}

		public virtual void ProcessOriginTranPost(APInvoice doc, bool masterInstallment)
		{
			if (doc.DocType == APDocType.Prepayment) return;
			APTranPost post = CreateTranPost(doc);
			post.Type = APTranPost.type.Origin;
			post.CuryAmt = doc.CuryOrigDocAmt;
			post.Amt = doc.OrigDocAmt;
			post.CuryRetainageAmt = doc.CuryRetainageTotal;
			post.RetainageAmt = doc.RetainageTotal;
			post.CuryWhTaxAmt = doc.CuryOrigWhTaxAmt;
			post.CuryDiscAmt = doc.CuryOrigDiscAmt;
			post.WhTaxAmt = doc.OrigWhTaxAmt;
			post.DiscAmt = doc.OrigDiscAmt;
			post.RGOLAmt = doc.RGOLAmt;
			post.TranRefNbr = doc.DocType == ARDocType.Prepayment
				? null
				: doc.MasterRefNbr ?? post.TranRefNbr;
			if(IsNeedUpdateHistoryForTransaction(post.FinPeriodID))
				post = TranPost.Insert(post);

			if (masterInstallment == true)
			{
				post.AccountID = null;
				post.SubID = null;
				ProcessInstallmentTranPost(doc);
			}
			//Process void invoices
			if(doc.DocType == APDocType.Invoice &&  doc.Voided == true)
				ProcessVoidTranPost(doc);
			
			if (doc.IsOriginalRetainageDocument() &&
			    doc.DocType == APDocType.DebitAdj)
			{
				var postR = CreateTranPost(doc);
				postR.SourceDocType = doc.OrigDocType;
				postR.SourceRefNbr = doc.OrigRefNbr;
				postR.Type = APTranPost.type.RetainageReverse;
				postR.CuryRetainageAmt = doc.CuryRetainageTotal;
				postR.RetainageAmt = doc.RetainageTotal;
				TranPost.Insert(postR);	
				postR.DocType = doc.OrigDocType;
				postR.RefNbr = doc.OrigRefNbr;
				postR.SourceDocType = doc.DocType;
				postR.SourceRefNbr = doc.RefNbr;
				TranPost.Insert(postR);
			}
		}

		public virtual void ProcessOriginTranPost(APPayment doc)
		{
			APTranPost post = CreateTranPost(doc);
			post.Type = APTranPost.type.Origin;
			post.CuryAmt = doc.CuryOrigDocAmt;
			post.Amt = doc.OrigDocAmt;
			if(IsNeedUpdateHistoryForTransaction(post.FinPeriodID))
				TranPost.Insert(post);
		}

		public virtual void ProcessRetainageTranPost(APInvoice doc)
		{
			if (doc.DocType == APDocType.Prepayment) return;
			APTranPost post = CreateTranPost(doc);
			post.DocType = doc.OrigDocType;
			post.RefNbr = doc.OrigRefNbr;
			post.Type = APTranPost.type.Retainage;
			post.CuryRetainageAmt = -doc.CuryOrigDocAmt;
			post.RetainageAmt = -doc.OrigDocAmt;
			if(IsNeedUpdateHistoryForTransaction(post.FinPeriodID))
				TranPost.Insert(post);
		}

		public virtual void ProcessInstallmentTranPost(APInvoice doc)
		{
			APTranPost post = CreateTranPost(doc);
			post.Type = APTranPost.type.Installment;
			post.CuryAmt = -doc.CuryOrigDocAmt;
			post.Amt = -doc.OrigDocAmt;
			if(IsNeedUpdateHistoryForTransaction(post.FinPeriodID))
				TranPost.Insert(post);
		}
		public virtual void ProcessVoidPaymentTranPost(APRegister doc, Amount docBal)
		{
			APTranPost docPost = CreateTranPost(doc);
			APTranPost revPost = CreateTranPost(doc);
			docPost.DocType = doc.OrigDocType ?? GetHistTranType(doc.DocType, doc.RefNbr);
			docPost.RefNbr = doc.OrigRefNbr ?? doc.RefNbr;
			revPost.SourceDocType = docPost.DocType;
			revPost.SourceRefNbr = docPost.RefNbr;
			docPost.Type = revPost.Type = APTranPost.type.Voided;
			docPost.CuryAmt = -docBal.Cury;
			docPost.Amt = -docBal.Base;
			revPost.CuryAmt = docBal.Cury;
			revPost.Amt = docBal.Base;
			if(docPost.DocType != null &&
			   IsNeedUpdateHistoryForTransaction(docPost.FinPeriodID))
				TranPost.Insert(docPost);
			if(revPost.DocType != null &&
			   IsNeedUpdateHistoryForTransaction(docPost.FinPeriodID))
				TranPost.Insert(revPost);
		}

		public virtual void ProcessVoidTranPost(APRegister doc)
		{
			APTranPost post = CreateTranPost(doc);
			post.Type = APTranPost.type.Voided;
			post.CuryAmt = -doc.CuryOrigDocAmt;
			post.Amt = -doc.OrigDocAmt;
			post.CuryRetainageAmt = -doc.CuryRetainageTotal;
			post.RetainageAmt = -doc.RetainageTotal;
			post.CuryWhTaxAmt = -doc.CuryOrigWhTaxAmt;
			post.CuryDiscAmt = -doc.CuryOrigDiscAmt;
			post.WhTaxAmt = -doc.OrigWhTaxAmt;
			post.DiscAmt = -doc.OrigDiscAmt;
			post.RGOLAmt = -doc.RGOLAmt;
			if(IsNeedUpdateHistoryForTransaction(post.FinPeriodID))
				post = TranPost.Insert(post);
		}

		public virtual void ProcessAdjustmentTranPost(APAdjust adj, APRegister doc, APRegister pmt, bool adjustedOnly = false)
		{
			APTranPost adjd = new APTranPost();
			APTranPost adjg = new APTranPost();
			adjd.Type = APTranPost.type.Application;
			adjg.Type = APTranPost.type.Adjustment;
			adjd.LineNbr = adjg.LineNbr = adj.AdjdLineNbr;
			adjd.IsMigratedRecord = adjg.IsMigratedRecord = adj.IsMigratedRecord;
			adjd.DocType = adjg.SourceDocType = adj.AdjdDocType;
			adjd.RefNbr = adjg.SourceRefNbr = adj.AdjdRefNbr;
			adjd.SourceDocType = adjg.DocType = adj.AdjgDocType;
			adjd.SourceRefNbr = adjg.RefNbr = adj.AdjgRefNbr;
			adjd.BatchNbr = adjg.BatchNbr = adj.AdjBatchNbr;
			adjd.RefNoteID = adjg.RefNoteID = adj.NoteID;
			adjd.TranType = adjg.TranType = adj.AdjgDocType;
			adjd.TranRefNbr = adjg.TranRefNbr = adj.AdjgRefNbr;
			adjd.IsVoidPrepayment = adjg.IsVoidPrepayment =
				GetHistTranType(adjg.TranType, adjg.TranRefNbr) == APDocType.Prepayment;
			adjd.VendorID = adjg.VendorID = doc.VendorID ?? pmt.VendorID;
			adjd.FinPeriodID = adjg.FinPeriodID = adj.AdjgFinPeriodID;
			adjd.TranPeriodID = adjg.TranPeriodID = adj.AdjgTranPeriodID;
			
			adjd.DocDate = adjg.DocDate = adj.AdjgDocDate;

			//AdjD
			adjd.AccountID = doc.APAccountID ?? adj.AdjdAPAcct;
			adjd.SubID = doc.APSubID ?? adj.AdjdAPSub;
			adjd.BranchID = adj.AdjdBranchID;
			adjd.CuryInfoID = adj.AdjdCuryInfoID;
			adjd.CuryAmt = adj.CuryAdjdAmt;
			adjd.CuryPPDAmt = adj.CuryAdjdPPDAmt;
			adjd.CuryDiscAmt = adj.CuryAdjdDiscAmt;
			adjd.CuryWhTaxAmt = adj.CuryAdjdWhTaxAmt;
			adjd.Amt = adj.AdjAmt;
			adjd.PPDAmt = adj.AdjPPDAmt;
			adjd.DiscAmt = adj.AdjDiscAmt;
			adjd.WhTaxAmt = adj.AdjWhTaxAmt;
			adjd.RGOLAmt = adj.RGOLAmt;
			//Adjg
			adjg.AccountID = pmt.APAccountID;
			adjg.SubID = pmt.APSubID;
			adjg.BranchID = adj.AdjgBranchID;
			adjg.CuryInfoID = adj.AdjgCuryInfoID;
			adjg.CuryAmt = adj.CuryAdjgAmt;
			adjg.CuryPPDAmt = adj.CuryAdjgPPDAmt;
			adjg.CuryDiscAmt = adj.CuryAdjgDiscAmt;
			adjg.CuryWhTaxAmt = adj.CuryAdjgWhTaxAmt;
			adjg.Amt = adj.AdjAmt;
			adjg.PPDAmt = adj.AdjPPDAmt;
			adjg.DiscAmt = adj.AdjDiscAmt;
			adjg.WhTaxAmt = adj.AdjWhTaxAmt;
			adjg.RGOLAmt = adj.RGOLAmt;

			if (doc.IsMigratedRecord == true &&
			    pmt.IsMigratedRecord == true)
			{
				adjd.IsMigratedRecord = true;
			}

			if (IsNeedUpdateHistoryForTransaction(adjd.FinPeriodID))
				TranPost.Insert(adjd);

			if (IsNeedUpdateHistoryForTransaction(adjg.FinPeriodID) && !adjustedOnly)
			{
				if(!adjd.DocType.IsIn(APDocType.QuickCheck, APDocType.VoidQuickCheck))
					TranPost.Insert(adjg);

				APTranPost rgol = (APTranPost)TranPost.Cache.CreateCopy(adjd);
				rgol.Type = APTranPost.type.RGOL;
				rgol.CuryInfoID = adj.AdjdCuryInfoID;
				rgol.CuryAmt = 0;
				rgol.Amt = 0;
				rgol.CuryPPDAmt = adj.CuryAdjdPPDAmt;
				rgol.PPDAmt = adj.AdjPPDAmt;
				rgol.CuryDiscAmt = adj.CuryAdjdDiscAmt;
				rgol.DiscAmt = adj.AdjDiscAmt;
				rgol.CuryWhTaxAmt = adj.CuryAdjdWhTaxAmt;
				rgol.WhTaxAmt = adj.AdjWhTaxAmt;
				rgol.TranType = adj.AdjgDocType;
				rgol.TranRefNbr = adj.AdjgRefNbr;
				TranPost.Insert(rgol);
			}
		}

		protected virtual APTranPost CreateTranPost(APRegister doc)
		{
			APTranPost post = new APTranPost();
			post.CuryInfoID = doc.CuryInfoID;
			post.DocType = post.SourceDocType = doc.DocType;
			post.RefNbr = post.SourceRefNbr = doc.RefNbr;
			post.VendorID = doc.VendorID;
			post.FinPeriodID = doc.FinPeriodID;
			post.TranPeriodID = doc.TranPeriodID;
			post.AccountID = doc.APAccountID;
			post.SubID = doc.APSubID;
			post.BranchID = doc.BranchID;
			post.BatchNbr = doc.BatchNbr;
			post.DocDate = doc.DocDate;
			post.CuryInfoID = doc.CuryInfoID;
			post.RefNoteID = doc.NoteID;
			post.TranType = doc.DocType;
			post.TranRefNbr = doc.RefNbr;
			post.IsMigratedRecord = doc.IsMigratedRecord;
			post.IsVoidPrepayment = GetHistTranType(post.TranType, post.TranRefNbr) == APDocType.Prepayment;
			return post;
		}

		public static void GetPPVAccountSub(ref int? aAccountID, ref int? aSubID, PXGraph aGraph, POReceiptLine aRow, ReasonCode reasonCode, bool getPOAccrual = false)
		{
			if (aRow.InventoryID.HasValue)
			{
				PXResult<InventoryItem, INPostClass> res = (PXResult<InventoryItem, INPostClass>)PXSelectJoin<InventoryItem,
									LeftJoin<INPostClass, On<INPostClass.postClassID, Equal<InventoryItem.postClassID>>>,
									Where<InventoryItem.inventoryID, Equal<Required<POLine.inventoryID>>>>.Select(aGraph, aRow.InventoryID);
				if (res != null)
				{
					InventoryItem invItem = (InventoryItem)res;
					INPostClass postClass = (INPostClass)res;
					if (postClass == null)
						throw new PXException(PO.Messages.PostingClassIsNotDefinedForTheItemInReceiptRow, invItem.InventoryCD, aRow.ReceiptNbr, aRow.LineNbr);
					INSite invSite = PXSelect<INSite, Where<INSite.siteID, Equal<Required<POReceiptLine.siteID>>>>.Select(aGraph, aRow.SiteID);

					if (getPOAccrual)
					{
						aAccountID = INReleaseProcess.GetAcctID<INPostClass.pOAccrualAcctID>(aGraph, postClass.POAccrualAcctDefault, invItem, invSite, postClass);
						try
						{
							aSubID = INReleaseProcess.GetSubID<INPostClass.pOAccrualSubID>(aGraph, postClass.POAccrualAcctDefault, postClass.POAccrualSubMask, invItem, invSite, postClass);
						}
						catch (PXException ex)
						{
							if (postClass.POAccrualSubID == null
								|| string.IsNullOrEmpty(postClass.POAccrualSubMask)
									|| invItem.POAccrualSubID == null || invSite == null || invSite.POAccrualSubID == null)
								throw new PXException(PO.Messages.POAccrualSubAccountCanNotBeFoundForItemInReceiptRow, invItem.InventoryCD, aRow.ReceiptNbr, aRow.LineNbr, postClass.PostClassID, invSite != null ? invSite.SiteCD : String.Empty);
							else
								throw ex;
						}
					return;
					}

					if ((bool)invItem.StkItem)
					{
						if (aRow.LineType == POLineType.GoodsForDropShip)
						{
							aAccountID = INReleaseProcess.GetAcctID<INPostClass.cOGSAcctID>(aGraph, postClass.COGSAcctDefault, invItem, invSite, postClass);
							if (aAccountID == null)
								throw new PXException(PO.Messages.COGSAccountCanNotBeFoundForItemInReceiptRow, invItem.InventoryCD, aRow.ReceiptNbr, aRow.LineNbr, postClass.PostClassID, invSite != null ? invSite.SiteCD : String.Empty);
							try
							{
								aSubID = INReleaseProcess.GetSubID<INPostClass.cOGSSubID>(aGraph, postClass.COGSAcctDefault, postClass.COGSSubMask, invItem, invSite, postClass);
							}
							catch (PXException ex)
							{
								if (postClass.COGSSubID == null
									|| string.IsNullOrEmpty(postClass.COGSSubMask)
										|| invItem.COGSSubID == null || invSite == null || invSite.COGSSubID == null)
									throw new PXException(PO.Messages.COGSSubAccountCanNotBeFoundForItemInReceiptRow, invItem.InventoryCD, aRow.ReceiptNbr, aRow.LineNbr, postClass.PostClassID, invSite != null ? invSite.SiteCD : String.Empty);
								else
									throw ex;
							}
							if (aSubID == null)
								throw new PXException(PO.Messages.COGSSubAccountCanNotBeFoundForItemInReceiptRow, invItem.InventoryCD, aRow.ReceiptNbr, aRow.LineNbr, postClass.PostClassID, invSite != null ? invSite.SiteCD : String.Empty);
						}
						else
						{
							aAccountID = reasonCode.AccountID;
							if (aAccountID == null)
							{
								throw new PXException(PO.Messages.PPVAccountCanNotBeFoundForItemInReceiptRow, invItem.InventoryCD, aRow.ReceiptNbr, aRow.LineNbr, reasonCode.ReasonCodeID);
							}
							try
							{
								aSubID = INReleaseProcess.GetReasonCodeSubID(aGraph, reasonCode, invItem, invSite, postClass);
							}
							catch (PXException ex)
							{
								if (reasonCode.SubID == null
									|| string.IsNullOrEmpty(reasonCode.SubMaskInventory)
										|| invItem == null || invSite == null)
								{
									throw new PXException(PO.Messages.PPVSubAccountCanNotBeFoundForItemInReceiptRow, invItem.InventoryCD, aRow.ReceiptNbr, aRow.LineNbr, reasonCode.ReasonCodeID, invSite != null ? invSite.SiteCD : String.Empty);
								}
								else
								{
									throw ex;
								}
							}
							if (aSubID == null)
								throw new PXException(PO.Messages.PPVSubAccountCanNotBeFoundForItemInReceiptRow, invItem.InventoryCD, aRow.ReceiptNbr, aRow.LineNbr, reasonCode.ReasonCodeID, invSite != null ? invSite.SiteCD : String.Empty);
						}

					}
					else
					{
						aAccountID = INReleaseProcess.GetAcctID<INPostClass.cOGSAcctID>(aGraph, postClass.COGSAcctDefault, invItem, invSite, postClass);
						try
						{
							aSubID = INReleaseProcess.GetSubID<INPostClass.cOGSSubID>(aGraph, postClass.COGSAcctDefault, postClass.COGSSubMask, invItem, invSite, postClass);
						}
						catch (PXException)
						{
							throw new PXException(Messages.ExpSubAccountCanNotBeAssembled);
						}
					}
				}
				else
				{
					throw new PXException(PO.Messages.PPVInventoryItemInReceiptRowIsNotFound, aRow.InventoryID, aRow.ReceiptNbr, aRow.LineNbr);
				}
			}
			else
			{
				aAccountID = aRow.ExpenseAcctID;
				aSubID = aRow.ExpenseSubID;
			}
		}

		public virtual bool IsPPVCalcNeeded(POReceiptLineR1 rctLine, APTran tran)
		{
			return
				rctLine.LineType == PO.POLineType.GoodsForInventory ||
			       rctLine.LineType == PO.POLineType.GoodsForDropShip ||
			       rctLine.LineType == PO.POLineType.NonStockForSalesOrder ||
                   rctLine.LineType == PO.POLineType.NonStockForServiceOrder ||
                   rctLine.LineType == PO.POLineType.GoodsForSalesOrder ||
                   rctLine.LineType == PO.POLineType.GoodsForServiceOrder ||
                   rctLine.LineType == PO.POLineType.NonStockForDropShip ||
			       rctLine.LineType == PO.POLineType.GoodsForReplenishment ||
			       rctLine.LineType == PO.POLineType.NonStock ||
			       rctLine.LineType == PO.POLineType.GoodsForManufacturing ||
			       rctLine.LineType == PO.POLineType.NonStockForManufacturing;
		}

		/// <summary>
		/// Gets the amount to be posted to the expense account for the given document line.
		/// </summary>
		public static Amount GetExpensePostingAmount(PXGraph graph, POLine documentLine)
		{
			var documentLineWithTaxes = new PXSelectJoin<
				POLine,
					LeftJoin<POTax,
						On<POTax.orderType, Equal<POLine.orderType>,
						And<POTax.orderNbr, Equal<POLine.orderNbr>,
						And<POTax.lineNbr, Equal<POLine.lineNbr>>>>,
					LeftJoin<Tax,
						On<Tax.taxID, Equal<POTax.taxID>>>>,
				Where<
					POLine.orderType, Equal<Required<POLine.orderType>>,
					And<POLine.orderNbr, Equal<Required<POLine.orderNbr>>,
					And<POLine.lineNbr, Equal<Required<POLine.lineNbr>>>>>>
				(graph);

			CurrencyInfo currencyInfo = graph.FindImplementation<IPXCurrencyHelper>().GetDefaultCurrencyInfo();
			Func<decimal, decimal> roundingFunction = amount => currencyInfo.RoundCury(amount);

			PXResult<POLine, POTax, Tax> queryResult =
				documentLineWithTaxes
					.Select(documentLine.OrderType, documentLine.OrderNbr, documentLine.LineNbr).AsEnumerable()
					.Cast<PXResult<POLine, POTax, Tax>>()
					.First();

			return GetExpensePostingAmountBase(graph, (POLine)queryResult, (POTax)queryResult, queryResult, null, roundingFunction);
		}

		/// <summary>
		/// Gets the amount to be posted to the expense account
		/// for the given document line.
		/// </summary>
		public static Amount GetExpensePostingAmount(PXGraph graph, APTran documentLine)
		{
			var documentLineWithTaxes = new PXSelectJoin<
				APTran,
					LeftJoin<APTax,
						On<APTax.tranType, Equal<APTran.tranType>,
						And<APTax.refNbr, Equal<APTran.refNbr>,
						And<APTax.lineNbr, Equal<APTran.lineNbr>>>>,
					LeftJoin<Tax,
						On<Tax.taxID, Equal<APTax.taxID>>,
					LeftJoin<APInvoice,
						On<APInvoice.docType, Equal<APTran.tranType>,
						And<APInvoice.refNbr, Equal<APTran.refNbr>>>>>>,
				Where<
					APTran.tranType, Equal<Required<APTran.tranType>>,
					And<APTran.refNbr, Equal<Required<APTran.refNbr>>,
					And<APTran.lineNbr, Equal<Required<APTran.lineNbr>>>>>>
				(graph);

			PXResult<APTran, APTax, Tax, APInvoice> queryResult =
				documentLineWithTaxes
					.Select(documentLine.TranType, documentLine.RefNbr, documentLine.LineNbr).AsEnumerable()
					.Cast<PXResult<APTran, APTax, Tax, APInvoice>>()
					.First();

			Func<decimal, decimal> roundingFunction = amount =>
				CM.PXDBCurrencyAttribute.Round(
					graph.Caches[typeof(APTran)],
					documentLine,
					amount,
					CM.CMPrecision.TRANCURY);

			return GetExpensePostingAmount(graph, documentLine, queryResult, queryResult, queryResult, roundingFunction);
		}

		/// <summary>
		/// If <see cref="FeaturesSet.netGrossEntryMode"/> is enabled, overrides the tax
		/// calculation level of the specified sales tax based on the document-level settings, e.g. to
		/// correctly calculate the expense posting amount (<see cref="GetExpensePostingAmount(PXGraph, APTran)"/>).
		/// </summary>
		/// <returns>A copy of the <see cref="Tax"/> with potentially adjusted calculation level.</returns>
		public static void AdjustTaxCalculationLevelForNetGrossEntryMode(APInvoice document, APTran documentLine, ref Tax taxCorrespondingToLine)
		{
			if (taxCorrespondingToLine?.TaxCalcType == CSTaxCalcType.Item
				&& PXAccess.FeatureInstalled<FeaturesSet.netGrossEntryMode>())
			{
				string documentTaxCalculationMode = document.TaxCalcMode;

				switch (documentTaxCalculationMode)
				{
					case TaxCalculationMode.Gross:
						taxCorrespondingToLine.TaxCalcLevel = CSTaxCalcLevel.Inclusive;
						break;
					case TaxCalculationMode.Net:
						taxCorrespondingToLine.TaxCalcLevel = CSTaxCalcLevel.CalcOnItemAmt;
						break;
					case TaxCalculationMode.TaxSetting:
					default:
						break;
				}
			}
		}

		[Obsolete(Common.Messages.MethodIsObsoleteRemoveInLaterAcumaticaVersions)]
		public static Amount GetExpensePostingAmount(PXGraph graph, APTran documentLine, APTax lineTax, Tax salesTax, APInvoice document, Func<decimal, decimal> round)
		{
			return GetExpensePostingAmount(graph, documentLine, lineTax, salesTax, document, round, round);
		}

		public static Amount GetExpensePostingAmount(PXGraph graph,
			APTran documentLine,
			APTax lineTax,
			Tax salesTax,
			APInvoice document,
			Func<decimal, decimal> roundCury,
			Func<decimal, decimal> roundBase)
		{
			AdjustTaxCalculationLevelForNetGrossEntryMode(document, documentLine, ref salesTax);

			bool postedPPD = document != null && document.PendingPPD == true && document.DocType == APDocType.DebitAdj;
			if (postedPPD == false &&
				lineTax != null &&
				lineTax.TaxID == null &&
				document != null &&
				document.OrigDocType == APDocType.DebitAdj &&
				document.OrigRefNbr != null)
			{
				postedPPD = PXSelect<
					APRegister,
					Where<
						APRegister.refNbr, Equal<Required<APRegister.refNbr>>,
						And<APRegister.docType, Equal<Required<APRegister.docType>>,
						And<APRegister.pendingPPD, Equal<True>>>>>
					.SelectSingleBound(graph, null, document.OrigRefNbr, document.OrigDocType).Count > 0;
			}

			bool isInclusivePerUnitTax = salesTax?.TaxType == CSTaxType.PerUnit &&
										 salesTax.TaxCalcLevel == CSTaxCalcLevel.CalcOnItemQtyInclusively;

			if (!postedPPD && !isInclusivePerUnitTax)
			{
				Amount postingAmount = GetExpensePostingAmountBase(graph, documentLine, lineTax, salesTax, document, roundCury, roundBase);
				if (lineTax?.TaxID != null && (salesTax?.IsRegularInclusiveTax() == true || document?.TaxCalcMode == TX.TaxCalculationMode.Gross))
				{
					postingAmount = postingAmount + new Amount(lineTax.CuryTaxableDiscountAmt ?? 0m, lineTax.TaxableDiscountAmt);
				}

				return postingAmount;
			}

			return new Amount(documentLine.CuryTaxableAmt, documentLine.TaxableAmt);
		}

		[Obsolete(Common.Messages.MethodIsObsoleteRemoveInLaterAcumaticaVersions)]
		public static Amount GetExpensePostingAmountBase(PXGraph graph, ITaxableDetail documentLine, ITaxDetailWithAmounts lineTax, Tax salesTax, APInvoice document, Func<decimal, decimal> round)
		{
			return GetExpensePostingAmountBase(graph, documentLine, lineTax, salesTax, document, round, round);
		}

		public static Amount GetExpensePostingAmountBase(PXGraph graph,
			ITaxableDetail documentLine,
			ITaxDetailWithAmounts lineTax,
			Tax salesTax,
			APInvoice document,
			Func<decimal, decimal> roundCury,
			Func<decimal, decimal> roundBase)
		{
			if (lineTax?.TaxID != null && (salesTax?.IsRegularInclusiveTax() == true || document?.TaxCalcMode == TX.TaxCalculationMode.Gross))
			{
				decimal? curyAddUp = documentLine.CuryTranAmt
					- roundCury((decimal)(documentLine.CuryTranAmt * documentLine.GroupDiscountRate * documentLine.DocumentDiscountRate));

				decimal? addUp = documentLine.TranAmt
					- roundBase((decimal)(documentLine.TranAmt * documentLine.GroupDiscountRate * documentLine.DocumentDiscountRate));

				return new Amount(
					lineTax.CuryTaxableAmt + (lineTax.CuryRetainedTaxableAmt ?? 0m) + curyAddUp,
					lineTax.TaxableAmt + (lineTax.RetainedTaxableAmt ?? 0m) + addUp);
			}

			return new Amount(
				documentLine.CuryTranAmt + (documentLine.CuryRetainageAmt ?? 0m),
				documentLine.TranAmt + (documentLine.RetainageAmt ?? 0m));
		}

		protected virtual void VerifyRoundingAllowed(APInvoice document, Batch batch, string baseCuryID)
		{
			bool useCurrencyPrecision = false;
			CM.Currency currency = PXSelect<CM.Currency, Where<Currency.curyID, Equal<Required<APInvoice.curyID>>>>.Select(this, document.CuryID);

			decimal diff = (decimal)(batch.DebitTotal - batch.CreditTotal);

			if (currency.UseAPPreferencesSettings == true)
			{
				useCurrencyPrecision = this.InvoiceRounding == RoundingType.Currency;
			}
			else
			{
				useCurrencyPrecision = currency.APInvoiceRounding == RoundingType.Currency;
			}

			if (useCurrencyPrecision && PXAccess.FeatureInstalled<FeaturesSet.invoiceRounding>() && Math.Abs(Math.Round((decimal)(document.CuryTaxRoundDiff) - diff, 4)) >= 0.00005m)
			{
				throw new PXException(Messages.DocumentOutOfBalance);
			}

			decimal roundDiff = Math.Abs(Math.Round(diff, 4));

			if (roundDiff > CM.CurrencyCollection.GetCurrency(baseCuryID).RoundingLimit && PXAccess.FeatureInstalled<FeaturesSet.invoiceRounding>())
			{
				throw new PXException(Messages.RoundingAmountTooBig, baseCuryID, roundDiff,
					PXDBQuantityAttribute.Round(CM.CurrencyCollection.GetCurrency(baseCuryID).RoundingLimit));
			}
		}

		private static string GetTaxDrCr(string origTranType, string tranType)
		{
			string ret = null;
			if (!string.IsNullOrWhiteSpace(origTranType) && !string.IsNullOrWhiteSpace(tranType))
			{
				if (origTranType == APDocType.Invoice && tranType == APDocType.DebitAdj)
				{
					ret = APInvoiceType.DrCr(tranType);
				}
			}
			if (ret == null)
			{
				if (!string.IsNullOrWhiteSpace(origTranType))
				{
					ret = APInvoiceType.DrCr(origTranType);
				}
				else
				{
					ret = APInvoiceType.DrCr(tranType);
				}
			}
			return ret;
		}

		private static void Append(GLTran aDest, GLTran aSrc)
		{
			aDest.CuryCreditAmt += aSrc.CuryCreditAmt ?? Decimal.Zero;
			aDest.CreditAmt += aSrc.CreditAmt ?? Decimal.Zero;
			aDest.CuryDebitAmt += aSrc.CuryDebitAmt ?? Decimal.Zero;
			aDest.DebitAmt += aSrc.DebitAmt ?? Decimal.Zero;
		}

		private static void Invert(GLTran aRow)
		{
			Decimal? swap1 = aRow.CuryDebitAmt;
			Decimal? swap2 = aRow.DebitAmt;
			aRow.CuryDebitAmt = aRow.CuryCreditAmt;
			aRow.DebitAmt = aRow.CreditAmt;
			aRow.CuryCreditAmt = swap1;
			aRow.CreditAmt = swap2;
		}

		private void UpdateWithholding(JournalEntry je, APAdjust adj, APRegister adjddoc, APPayment adjgdoc, Vendor vend, CurrencyInfo vouch_info)
		{
			APRegister apdoc = (APRegister)adjddoc;
			APRegister cached = (APRegister)APDocument.Cache.Locate(apdoc);
			if (cached != null)
			{
				apdoc = cached;
			}

			if (adjgdoc.DocType == APDocType.DebitAdj)
			{
				return;
			}

			if (CM.PXCurrencyAttribute.IsNullOrEmpty(apdoc.CuryOrigWhTaxAmt))
			{
				return;
			}

			if (je.currencyinfo.Current == null)
			{
				throw new PXException();
			}

			PXResultset<APTaxTran> whtaxtrans = (PXResultset<APTaxTran>)WHTax_TranType_RefNbr.Select(apdoc.DocType, apdoc.RefNbr);

			int i = 0;
			decimal CuryAdjgWhTaxAmt = (decimal)adj.CuryAdjgWhTaxAmt;
			decimal AdjWhTaxAmt = (decimal)adj.AdjWhTaxAmt;

			foreach (PXResult<APTaxTran, Tax> whres in whtaxtrans)
			{
				Tax salesTax = (Tax)whres;
				APTaxTran taxtran = (APTaxTran)whres;

				if (apdoc.DocType == APDocType.QuickCheck || apdoc.DocType == APDocType.VoidQuickCheck)
				{
					taxtran.Released = true;
					WHTax_TranType_RefNbr.Update(taxtran);
					CreateGLTranForWhTaxTran(je, adj, adjgdoc, taxtran, vend, vouch_info, i == whtaxtrans.Count - 1);
				}
				else
				{
					CurrencyInfo currencyInfo = GetExtension<MultiCurrency>().GetCurrencyInfo(adj.AdjgCuryInfoID);

					APTaxTran whtran = new APTaxTran
					{
						Module = taxtran.Module,
						BranchID = adj.AdjgBranchID,
						TranType = adj.AdjgDocType,
						RefNbr = adj.AdjgRefNbr,
						AdjdDocType = adj.AdjdDocType,
						AdjdRefNbr = adj.AdjdRefNbr,
						AdjNbr = adj.AdjNbr,
						VendorID = taxtran.VendorID,
						TaxZoneID = taxtran.TaxZoneID,
						TaxID = taxtran.TaxID,
						TaxRate = taxtran.TaxRate,
						AccountID = taxtran.AccountID,
						SubID = taxtran.SubID,
						TaxType = taxtran.TaxType,
						TaxBucketID = taxtran.TaxBucketID,
						TranDate = adj.AdjgDocDate,
						FinPeriodID = adj.AdjgFinPeriodID,
						CuryInfoID = adj.AdjgCuryInfoID,
						Released = true,
						CuryID = currencyInfo.CuryID,
						CuryTaxableAmt = currencyInfo.RoundCury(((decimal)adj.CuryAdjgAmt + (decimal)adj.CuryAdjgWhTaxAmt) * (decimal)taxtran.CuryTaxableAmt / (decimal)apdoc.CuryOrigDocAmt)
					};

					if (i < whtaxtrans.Count - 1)
					{
						whtran.CuryTaxAmt = currencyInfo.RoundCury((decimal)adj.CuryAdjgWhTaxAmt * (decimal)taxtran.CuryTaxAmt / (decimal)apdoc.CuryOrigWhTaxAmt);
						//insert, get back with base currency
						if (APTaxTran_TranType_RefNbr.Cache.ObjectsEqual(whtran, taxtran))
						{
							whtran.CreatedByID = taxtran.CreatedByID;
							whtran.CreatedByScreenID = taxtran.CreatedByScreenID;
							whtran.CreatedDateTime = taxtran.CreatedDateTime;
							whtran = (APTaxTran)APTaxTran_TranType_RefNbr.Cache.Update(whtran);
						}
						else
						{
							whtran = (APTaxTran)APTaxTran_TranType_RefNbr.Cache.Insert(whtran);
						}

						CuryAdjgWhTaxAmt -= (decimal)whtran.CuryTaxAmt;
						AdjWhTaxAmt -= (decimal)whtran.TaxAmt;
					}
					else
					{
						whtran.CuryTaxAmt = CuryAdjgWhTaxAmt;
						whtran.TaxAmt = AdjWhTaxAmt;

						//insert, do not get back not to recalc base cury
						if (APTaxTran_TranType_RefNbr.Cache.ObjectsEqual(whtran, taxtran))
						{
							whtran.CreatedByID = taxtran.CreatedByID;
							whtran.CreatedByScreenID = taxtran.CreatedByScreenID;
							whtran.CreatedDateTime = taxtran.CreatedDateTime;
							APTaxTran_TranType_RefNbr.Cache.Update(whtran);
						}
						else
						{
							APTaxTran_TranType_RefNbr.Cache.Insert(whtran);
						}

						CuryAdjgWhTaxAmt = 0m;
						AdjWhTaxAmt = 0m;
					}

					CreateGLTranForWhTaxTran(je, adj, adjgdoc, whtran, vend, vouch_info, i == whtaxtrans.Count - 1);
				}

				i++;
			}
		}

		protected virtual void CreateGLTranForWhTaxTran(JournalEntry je, APAdjust adj, APPayment adjgdoc, APTaxTran whtran, Vendor vend, CurrencyInfo vouch_info, bool updateHistory)
		{
			GLTran tran = new GLTran();
			tran.SummPost = this.SummPost;
			tran.BranchID = whtran.BranchID;
			tran.AccountID = whtran.AccountID;
			tran.SubID = whtran.SubID;
			tran.OrigAccountID = adj.AdjdAPAcct;
			tran.OrigSubID = adj.AdjdAPSub;
			tran.DebitAmt = (adj.AdjgGLSign == 1m) ? 0m : whtran.TaxAmt;
			tran.CuryDebitAmt = (adj.AdjgGLSign == 1m) ? 0m : whtran.CuryTaxAmt;
			tran.CreditAmt = (adj.AdjgGLSign == 1m) ? whtran.TaxAmt : 0m;
			tran.CuryCreditAmt = (adj.AdjgGLSign == 1m) ? whtran.CuryTaxAmt : 0m;
			tran.TranType = adj.AdjgDocType;
			tran.TranClass = GLTran.tranClass.WithholdingTax;
			tran.RefNbr = adj.AdjgRefNbr;
			tran.TranDesc = whtran.TaxID;
			tran.TranDate = adj.AdjgDocDate;
			FinPeriodIDAttribute.SetPeriodsByMaster<GLTran.finPeriodID>(je.GLTranModuleBatNbr.Cache, tran, adj.AdjgTranPeriodID);
			tran.CuryInfoID = je.currencyinfo.Current.CuryInfoID;
			tran.Released = true;
			tran.ReferenceID = adjgdoc.VendorID;
			InsertAdjustmentsWhTaxTransaction(je, tran,
				new GLTranInsertionContext { APRegisterRecord = adjgdoc, APAdjustRecord = adj, APTaxTranRecord = whtran });

			if (updateHistory)
			{
				tran.DebitAmt = (adj.AdjgGLSign == 1m) ? 0m : adj.AdjWhTaxAmt;
				tran.CreditAmt = (adj.AdjgGLSign == 1m) ? adj.AdjWhTaxAmt : 0m;
				UpdateHistory(tran, vend);

				tran.CuryDebitAmt = (adj.AdjgGLSign == 1m) ? 0m : adj.CuryAdjdWhTaxAmt;
				tran.CuryCreditAmt = (adj.AdjgGLSign == 1m) ? adj.CuryAdjdWhTaxAmt : 0m;
				UpdateHistory(tran, vend, vouch_info);
			}
		}

		protected virtual void AP1099Hist_FinYear_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			if (_IsIntegrityCheck || IsInvoiceReclassification)
			{
				e.Cancel = true;
			}
		}

		protected virtual void AP1099Hist_BoxNbr_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			if (_IsIntegrityCheck || IsInvoiceReclassification)
			{
				e.Cancel = true;
			}
		}

		protected virtual void AP1099Hist_RowInserting(PXCache sender, PXRowInsertingEventArgs e)
		{
			if (((AP1099Hist)e.Row).BoxNbr == null)
			{
				e.Cancel = true;
			}
		}

		public static void Update1099Hist(PXGraph graph, decimal histMult, APAdjust adj, APTran tran, APRegister apdoc)
		{
            if (adj.AdjdDocType == APDocType.Prepayment || adj.AdjgDocType == APDocType.DebitAdj)
            {
                return;
            }

			if (adj.AdjgDocType == APDocType.VoidQuickCheck || adj.AdjgDocType == APDocType.Refund || adj.AdjgDocType == APDocType.VoidRefund || adj.AdjdDocType == APDocType.DebitAdj)
            {
                histMult = -histMult;
            }

            PXCache cache = graph.Caches[typeof(AP1099Hist)];
			string Year1099 = ((DateTime)adj.AdjgDocDate).Year.ToString();

			if (apdoc != null && apdoc.OrigDocAmt != 0m)
			{
				AP1099Yr year = new AP1099Yr
				{
					FinYear = Year1099,
					OrganizationID = PXAccess.GetParentOrganizationID(adj.AdjgBranchID)
				};

				year = (AP1099Yr)graph.Caches[typeof(AP1099Yr)].Insert(year);

				AP1099Hist hist = new AP1099Hist();
				hist.BranchID = adj.AdjgBranchID;
				hist.VendorID = apdoc.VendorID;
				hist.FinYear = Year1099;
				hist.BoxNbr = tran.Box1099;

				hist = (AP1099Hist)cache.Insert(hist);

				if (hist != null)
				{
					decimal whTaxAmount = GetLineWhTaxAmount(graph, tran);
					decimal groupAndDocumentDiscountRate = (decimal)tran.GroupDiscountRate * (decimal)tran.DocumentDiscountRate;
					decimal tranAmountAfterDiscount = (decimal)tran.TranAmt * groupAndDocumentDiscountRate;
					decimal tranAmountPortion = (tranAmountAfterDiscount - whTaxAmount) * (decimal)adj.AdjAmt / ((decimal)apdoc.OrigDocAmt - (decimal)apdoc.OrigWhTaxAmt);

					hist.HistAmt += CM.PXCurrencyAttribute.BaseRound(graph, histMult * tranAmountPortion);
				}
			}
		}

		private static decimal GetLineWhTaxAmount(PXGraph graph, APTran tran)
		{
			return SelectFrom<APTax>
							.InnerJoin<Tax>
								.On<APTax.taxID.IsEqual<Tax.taxID>>
							.Where<APTax.refNbr.IsEqual<P.AsString>
								.And<APTax.lineNbr.IsEqual<P.AsInt>>
								.And<APTax.tranType.IsEqual<P.AsString>>
								.And<Tax.taxType.IsEqual<CSTaxType.withholding>>>
							.View.ReadOnly
							.Select(graph, tran.RefNbr, tran.LineNbr, tran.TranType)
							.RowCast<APTax>().Sum(a => (decimal)a.TaxAmt);
		}

		private void Update1099(APAdjust adj, APRegister apdoc)
		{
			string year1099 = ((DateTime)adj.AdjgDocDate).Year.ToString();

			int? organizationID = PXAccess.GetParentOrganizationID(adj.AdjgBranchID);

			AP1099Year year = PXSelect<AP1099Year,
											Where<AP1099Year.finYear, Equal<Required<AP1099Year.finYear>>,
													And<AP1099Year.organizationID, Equal<Required<AP1099Year.organizationID>>>>>
											.Select(this, year1099, organizationID);
			if (year == null)
			{
				year = new AP1099Yr
				{
					FinYear = year1099,
					Status = AP1099Year.status.Open,
					OrganizationID = organizationID
				};

				year = (AP1099Year)AP1099Year_Select.Cache.Insert(year);
				}
			else if (_IsIntegrityCheck == false && year.Status != AP1099Year.status.Open)
				{
					throw new PXException(Messages.AP1099_PaymentDate_NotIn_OpenYear, PXUIFieldAttribute.GetDisplayName<APPayment.adjDate>(APPayment_DocType_RefNbr.Cache));
				}

			foreach (APTran tran in AP1099Tran_Select.Select(apdoc.DocType, apdoc.RefNbr))
			{
				Update1099Hist(this, 1, adj, tran, apdoc);
			}
		}

		public virtual void UpdateBalances(APAdjust adj, APRegister adjddoc, Vendor vendor)
		{
			UpdateBalances(adj, adjddoc, vendor, null);
		}

		private bool ShouldProcessAsPrepaymentRequestApplication(APRegister apdoc, APAdjust adj)
		{
			if (apdoc.DocType != APDocType.Prepayment
				|| adj.AdjgDocType == APDocType.Refund
				|| adj.AdjgDocType == APDocType.VoidRefund) return false;

			bool appliedToVoidCheck = adj.AdjgDocType == APDocType.VoidCheck || adj.VoidAdjNbr != null;
			bool appliedToCheck = adj.AdjgDocType == APDocType.Check;
			bool appliedToPrepayment = adj.AdjgDocType == APDocType.Prepayment
				&& !string.Equals(adj.AdjgRefNbr, apdoc.RefNbr, StringComparison.OrdinalIgnoreCase);

			return (appliedToVoidCheck || appliedToCheck || appliedToPrepayment)
								&& string.Equals(adj.AdjdDocType, apdoc.DocType, StringComparison.Ordinal)
								&& string.Equals(adj.AdjdRefNbr, apdoc.RefNbr, StringComparison.OrdinalIgnoreCase);
		}


		public virtual void UpdateBalances(APAdjust adj, APRegister adjddoc, Vendor vendor, APTran adjdtran)
		{
			APRegister apdoc = (APRegister)adjddoc;
			APRegister cachedDoc = (APRegister)APDocument.Cache.Locate(apdoc);

			if (cachedDoc != null)
			{
				APDocument.Cache.RestoreCopy(apdoc, cachedDoc);
			}
			else if (_IsIntegrityCheck == true)
			{
				return;
			}

			if (_IsIntegrityCheck == false && adj.VoidAdjNbr != null)
			{
				VoidOrigAdjustment(adj);
			}

			if (ShouldProcessAsPrepaymentRequestApplication(apdoc, adj))
			{
				ProcessPrepaymentRequestApplication(apdoc, adj);
				return;
			}


			decimal? curyAdjdAmt = adj.CuryAdjdAmt + adj.CuryAdjdDiscAmt + adj.CuryAdjdWhTaxAmt;
			decimal? adjAmt = adj.AdjAmt + adj.AdjDiscAmt + adj.AdjWhTaxAmt + (adj.ReverseGainLoss == false ? adj.RGOLAmt : -adj.RGOLAmt);

			apdoc.CuryDocBal -= curyAdjdAmt;
			apdoc.DocBal -= adjAmt;
			apdoc.CuryDiscBal -= adj.CuryAdjdDiscAmt;
			apdoc.DiscBal -= adj.AdjDiscAmt;
			apdoc.CuryWhTaxBal -= adj.CuryAdjdWhTaxAmt;
			apdoc.WhTaxBal -= adj.AdjWhTaxAmt;
			apdoc.CuryDiscTaken += adj.CuryAdjdDiscAmt;
			apdoc.DiscTaken += adj.AdjDiscAmt;
			apdoc.CuryTaxWheld += adj.CuryAdjdWhTaxAmt;
			apdoc.TaxWheld += adj.AdjWhTaxAmt;

			apdoc.RGOLAmt += adj.RGOLAmt;

			if (apdoc.CuryDiscBal == 0m)
			{
				apdoc.DiscBal = 0m;
			}

			if (apdoc.CuryWhTaxBal == 0m)
			{
				apdoc.WhTaxBal = 0m;
			}

			if (apdoc.CuryDocBal == 0m && apdoc.DocBal != 0)
			{
				adj.RGOLAmt += apdoc.DocBal;
				apdoc.RGOLAmt += apdoc.DocBal;
			}

			if (_IsIntegrityCheck == false && apdoc.CuryDocBal < 0m)
			{
				throw new PXException(Messages.DocumentBalanceNegative);
			}

			if (_IsIntegrityCheck == false && adj.AdjgDocDate < adjddoc.DocDate)
			{
				throw new PXException(Messages.ApplDate_Less_DocDate, PXUIFieldAttribute.GetDisplayName<APPayment.adjDate>(APPayment_DocType_RefNbr.Cache));
			}

			if (_IsIntegrityCheck == false && string.Compare(adj.AdjgTranPeriodID, adjddoc.TranPeriodID) < 0)
			{
				throw new PXException(Messages.ApplPeriod_Less_DocPeriod, PXUIFieldAttribute.GetDisplayName<APPayment.adjFinPeriodID>(APPayment_DocType_RefNbr.Cache));
			}
			
			if (adjdtran != null && adjdtran.AreAllKeysFilled(APTran_TranType_RefNbr.Cache))
			{
				APTran tran = adjdtran;
				APTran cachedTran = (APTran)APTran_TranType_RefNbr.Cache.Locate(tran);

				if (cachedTran != null)
				{
					tran = cachedTran;
				}
				else if (_IsIntegrityCheck) return;

				tran.CuryTranBal -= curyAdjdAmt;
				tran.TranBal -= adjAmt;
				tran.CuryCashDiscBal -= adj.CuryAdjdDiscAmt;
				tran.CashDiscBal -= adj.AdjDiscAmt;

				if (tran.CuryCashDiscBal == 0m)
				{
					tran.CashDiscBal = 0m;
				}

				if (tran.CuryTranBal == 0m)
				{
					tran.TranBal = 0m;
				}

				if (!_IsIntegrityCheck &&
					(tran.CuryTranBal < 0m || tran.CuryCashDiscBal < 0m))
				{
					throw new PXException(Messages.DocumentBalanceNegative);
				}

				APTran_TranType_RefNbr.Update(tran);
			}
		}

		public virtual void UpdateRetainageBalances(APAdjust adj, APRegister adjddoc, APRegister adjgdoc)
		{
			APRegister apdoc = adjddoc;
			APRegister cached = (APRegister)APDocument.Cache.Locate(apdoc);

			if (cached != null)
			{
				apdoc = cached;
			}

			APRegister origRetainageDoc =
				apdoc.DocType == APDocType.Invoice
					? apdoc.IsOriginalRetainageDocument()
						? apdoc
						: apdoc.IsChildRetainageDocument()
							? GetOriginalRetainageDocument(apdoc)
							: null
					: null;

			// We should close original retainage Bill
			// only when all its retainage balances will be
			// equal to 0.
			//
			if (origRetainageDoc != null)
			{
				VerifyDocumentBalanceAndClose(origRetainageDoc);
			}
		}

		public virtual void CloseInvoiceAndClearBalances(APRegister apdoc)
		{
				apdoc.CuryDiscBal = 0m;
				apdoc.DiscBal = 0m;
				apdoc.CuryWhTaxBal = 0m;
				apdoc.WhTaxBal = 0m;
				apdoc.DocBal = 0m;

				apdoc.OpenDoc = false;
				SetClosedPeriodsFromLatestApplication(apdoc);
				APDocument.Cache.Update(apdoc);
				RaiseInvoiceEvent(apdoc, APInvoice.Events.Select(ev => ev.CloseDocument));
				RaisePaymentEvent(apdoc, APPayment.Events.Select(ev => ev.CloseDocument));
		}

		public virtual void OpenInvoiceAndRecoverBalances(APRegister apdoc)
			{
				if (apdoc.CuryDocBal == apdoc.CuryOrigDocAmt)
				{
					apdoc.CuryDiscBal = apdoc.CuryOrigDiscAmt;
					apdoc.DiscBal = apdoc.OrigDiscAmt;
					apdoc.CuryWhTaxBal = apdoc.CuryOrigWhTaxAmt;
					apdoc.WhTaxBal = apdoc.OrigWhTaxAmt;
					apdoc.CuryDiscTaken = 0m;
					apdoc.DiscTaken = 0m;
					apdoc.CuryTaxWheld = 0m;
					apdoc.TaxWheld = 0m;
				}

				apdoc.OpenDoc = true;
				apdoc.ClosedDate = null;
                apdoc.ClosedFinPeriodID = null;
				apdoc.ClosedTranPeriodID = null;
			APDocument.Cache.Update(apdoc);
			RaiseInvoiceEvent(apdoc, APInvoice.Events.Select(ev => ev.OpenDocument));
			RaisePaymentEvent(apdoc, APPayment.Events.Select(ev => ev.OpenDocument));
		}

		public virtual void VoidOrigAdjustment(APAdjust adj)
		{
			string[] docTypes = APPaymentType.GetVoidedAPDocType(adj.AdjgDocType);
			if (docTypes.Length == 0)
			{
				docTypes = new string[] { adj.AdjgDocType };
			}

			APAdjust voidadj = PXSelect<APAdjust,
					  Where<APAdjust.adjgDocType, In<Required<APAdjust.adjgDocType>>,
						And<APAdjust.adjgRefNbr, Equal<Required<APAdjust.adjgRefNbr>>,
						And<APAdjust.adjdDocType, Equal<Required<APAdjust.adjdDocType>>,
						And<APAdjust.adjdRefNbr, Equal<Required<APAdjust.adjdRefNbr>>,
						And<APAdjust.adjNbr, Equal<Required<APAdjust.adjNbr>>,
						And<APAdjust.adjdLineNbr, Equal<Required<APAdjust.adjdLineNbr>>>>>>>>>.
					Select(this, docTypes, adj.AdjgRefNbr, adj.AdjdDocType, adj.AdjdRefNbr, adj.VoidAdjNbr, adj.AdjdLineNbr);


			if (voidadj != null)
			{
				if ((bool)voidadj.Voided)
				{
					throw new PXException(Messages.DocumentApplicationAlreadyVoided);
				}

				voidadj.Voided = true;
				Caches[typeof(APAdjust)].Update(voidadj);

				adj.AdjAmt = -voidadj.AdjAmt;
				adj.RGOLAmt = -voidadj.RGOLAmt;

				Caches[typeof(APAdjust)].Update(adj);
				if (voidadj.AdjgDocType == APDocType.DebitAdj && voidadj.AdjdHasPPDTaxes == true)
				{
					APRegister debitAdj = PXSelect<APRegister, Where<APRegister.docType, Equal<APDocType.debitAdj>,
						And<APRegister.refNbr, Equal<Required<APRegister.refNbr>>>>>.SelectSingleBound(this, null, voidadj.AdjgRefNbr);
					if (debitAdj != null && debitAdj.PendingPPD == true)
					{
						PXUpdate<Set<APAdjust.pPDDebitAdjRefNbr, Null>, APAdjust,
						Where<APAdjust.pendingPPD, Equal<True>,
							And<APAdjust.adjdDocType, Equal<Required<APAdjust.adjdDocType>>,
							 And<APAdjust.adjdRefNbr, Equal<Required<APAdjust.adjdRefNbr>>,
							And<APAdjust.pPDDebitAdjRefNbr, Equal<Required<APAdjust.pPDDebitAdjRefNbr>>>>>>>
						.Update(this, voidadj.AdjdDocType, voidadj.AdjdRefNbr, voidadj.AdjgRefNbr);
					}
				}
			}
		}
		/// <summary>
		/// Processes the prepayment request applied to Check or Void Check.
		/// </summary>
		/// <param name="prepaymentRequest">The prepayment request.</param>
		/// <param name="prepaymentAdj">The prepayment application.</param>
		protected virtual void ProcessPrepaymentRequestApplication(APRegister prepaymentRequest, APAdjust prepaymentAdj)
		{
            if (prepaymentAdj.AdjgDocType == APDocType.VoidCheck ||
                prepaymentAdj.VoidAdjNbr != null && prepaymentAdj.AdjgDocType != APDocType.VoidRefund)
			{
				if (Math.Abs((decimal)(prepaymentRequest.CuryOrigDocAmt - prepaymentRequest.CuryDocBal)) > 0m)
				{
					throw new PXException(Messages.PrepaymentCheckCannotBeVoided, prepaymentAdj.AdjgRefNbr, prepaymentRequest.RefNbr);
				}
				else
				{
					foreach (APAdjust oldadj in
						APAdjust_AdjgDocType_RefNbr_VendorID.Select(prepaymentRequest.DocType, prepaymentRequest.RefNbr, _IsIntegrityCheck, prepaymentRequest.AdjCntr))
					{
						throw new PXException(Messages.PrepaymentCheckCannotBeVoided, prepaymentAdj.AdjgRefNbr, prepaymentRequest.RefNbr);
					}
				}

				prepaymentRequest.OpenDoc = false;
				prepaymentRequest.Voided = true;
			    FinPeriodIDAttribute.SetPeriodsByMaster<APRegister.closedFinPeriodID>(APDocument.Cache, prepaymentRequest, prepaymentAdj.AdjgTranPeriodID);
				prepaymentRequest.ClosedDate = prepaymentAdj.AdjgDocDate;
				prepaymentRequest.CuryDocBal = 0m;
				prepaymentRequest.DocBal = 0m;

				prepaymentRequest = (APRegister)APDocument.Cache.Update(prepaymentRequest);
				RaiseInvoiceEvent(prepaymentRequest, APInvoice.Events.Select(ev => ev.VoidDocument));
            }
			else if (prepaymentAdj.AdjgDocType == APDocType.VoidRefund)
			{
				prepaymentRequest.OpenDoc = true;
				prepaymentRequest.CuryDocBal -= prepaymentAdj.CuryAdjdAmt;
				prepaymentRequest.DocBal -= prepaymentAdj.AdjAmt;
				APDocument.Cache.Update(prepaymentRequest);
				RaiseInvoiceEvent(prepaymentRequest, APInvoice.Events.Select(ev => ev.OpenDocument));

			}
			else if (prepaymentAdj.AdjgDocType.IsIn(APDocType.Check, APDocType.Prepayment))
			{
				if (_IsIntegrityCheck)
				{
					//check for prepayment request will be processed last
					BalanceCalculation.AdjustBalance(prepaymentRequest, prepaymentAdj, -1m);
					prepaymentRequest.DocBal -= prepaymentAdj.RGOLAmt;
					VerifyDocumentBalanceAndClose(prepaymentRequest);
					return;
				}

				ProcessPrepaymentRequestAppliedToCheck(prepaymentRequest, prepaymentAdj);
			}
		}

		/// <summary>
		/// Processes the prepayment request applied to check. Verifies that prepayment is paid in full, if neccessary checks for discrepancy and moves it to RGOL account.
		/// Then creates the Payment part of prepayment request which is shown on the "Checks and Payments" screen.
		/// </summary>
		/// <param name="prepaymentRequest">The prepayment request.</param>
		/// <param name="prepaymentAdj">The prepayment adjustment.</param>
		protected virtual void ProcessPrepaymentRequestAppliedToCheck(APRegister prepaymentRequest, APAdjust prepaymentAdj)
		{
			CurrencyInfo ppmRequestCuryinfo = CurrencyInfo_CuryInfoID.Select(prepaymentRequest.CuryInfoID);
			CurrencyInfo checkCuryinfo = CurrencyInfo_CuryInfoID.Select(prepaymentAdj.AdjgCuryInfoID);
			CurrencyInfo curyInfoToUse;

			FullBalanceDelta balanceAdjustment = prepaymentAdj.GetFullBalanceDelta();
			decimal curyAdjdAmountCorrected = balanceAdjustment.CurrencyAdjustedBalanceDelta;

			//Check that prepayment is fully paid in currency
			if (Math.Abs(prepaymentRequest.CuryOrigDocAmt.Value - curyAdjdAmountCorrected) != 0m)
			{
				throw new PXException(Messages.PrepaymentNotPayedFull, prepaymentRequest.RefNbr);
			}

			//If check and prepayment request are in the same currency, use currency info from check to account for possible currency rate overrides in check
			if (checkCuryinfo.CuryID == ppmRequestCuryinfo.CuryID)
			{
				curyInfoToUse = checkCuryinfo;
			}
			else
			{
				//If check and prepayment request are in different currencies, use currency info from application to check
				curyInfoToUse = CurrencyInfo_CuryInfoID.Select(prepaymentAdj.AdjdCuryInfoID);

				if (ppmRequestCuryinfo.CuryID == ppmRequestCuryinfo.BaseCuryID)
				{
					//Check prepayment for rounding discrepancy, if there is one, then add the difference to RGOL
					decimal adjAmountCorrected = balanceAdjustment.BaseAdjustedBalanceDelta;
					decimal baseCuryDiff = prepaymentRequest.OrigDocAmt.Value - adjAmountCorrected;

					if (Math.Abs(baseCuryDiff) != 0m)
					{
						decimal? amountToRGOL = prepaymentAdj.ReverseGainLoss == false ? baseCuryDiff : -baseCuryDiff;
						prepaymentAdj.RGOLAmt += amountToRGOL;
					}
				}
			}

			APPayment prepayment = (APPayment)APPayment_DocType_RefNbr.Cache.Extend<APRegister>(prepaymentRequest);
			prepayment.CreatedByID = prepaymentRequest.CreatedByID;
			prepayment.CreatedByScreenID = prepaymentRequest.CreatedByScreenID;
			prepayment.CreatedDateTime = prepaymentRequest.CreatedDateTime;
			prepayment.CashAccountID = null;
			prepayment.PaymentMethodID = null;
			prepayment.ExtRefNbr = null;

			prepayment.DocDate = prepaymentAdj.AdjgDocDate;
			FinPeriodIDAttribute.SetPeriodsByMaster<APPayment.finPeriodID>(APPayment_DocType_RefNbr.Cache, prepayment, prepaymentAdj.AdjgTranPeriodID);

			prepayment.AdjDate = prepayment.DocDate;
			prepayment.AdjFinPeriodID = prepayment.FinPeriodID;
			prepayment.AdjTranPeriodID = prepayment.TranPeriodID;
			prepayment.Printed = true;

			APAddressAttribute.DefaultRecord<APPayment.remitAddressID>(APPayment_DocType_RefNbr.Cache, prepayment);
			APContactAttribute.DefaultRecord<APPayment.remitContactID>(APPayment_DocType_RefNbr.Cache, prepayment);

			APPayment_DocType_RefNbr.Cache.Update(prepayment);

			TaxAttribute.SetTaxCalc<APTran.taxCategoryID>(APTran_TranType_RefNbr.Cache, null, TaxCalc.NoCalc);

			GetExtension<MultiCurrency>().UpdateCurrencyInfoForPrepayment(prepayment, curyInfoToUse);
			APDocument.Cache.SetStatus(prepaymentRequest, PXEntryStatus.Notchanged);
			//Prepayment with prepayment should not generetate any RGOL.
			if (prepaymentAdj.AdjgDocType == APDocType.Prepayment)
				prepaymentAdj.RGOLAmt = 0;
		}

		private void UpdateVoidedCheck(APRegister voidcheck)
		{
			foreach (string origDocType in voidcheck.PossibleOriginalDocumentTypes())
			{
				foreach (PXResult<APPayment, CurrencyInfo, Currency, Vendor> res in APPayment_DocType_RefNbr
					.Search<APPayment.vendorID>(voidcheck.VendorID, origDocType, voidcheck.RefNbr))
				{
					APRegister apdoc = res;
					APRegister cached = (APRegister)APDocument.Cache.Locate(apdoc);

					if (cached != null)
					{
						APDocument.Cache.RestoreCopy(apdoc, cached);
					}

					apdoc.Voided = true;
					apdoc.OpenDoc = false;
					apdoc.Hold = false;
					apdoc.CuryDocBal = 0m;
					apdoc.DocBal = 0m;
					SetClosedPeriodsFromLatestApplication(apdoc);
					APDocument.Cache.Update(apdoc);
					RaisePaymentEvent(apdoc, APPayment.Events.Select(g=>g.VoidDocument));
					RaiseInvoiceEvent(apdoc, APInvoice.Events.Select(g => g.VoidDocument));

					PXCache applicationCache = Caches[typeof(APAdjust)];

					if (!_IsIntegrityCheck)
					{
						// For the voided document, we must remove all unreleased applications.
						// -
						foreach (APAdjust application in PXSelect<APAdjust,
							Where<
								APAdjust.adjgDocType, Equal<Required<APAdjust.adjgDocType>>,
								And<APAdjust.adjgRefNbr, Equal<Required<APAdjust.adjgRefNbr>>,
								And<APAdjust.released, NotEqual<True>>>>>
							.Select(this, apdoc.DocType, apdoc.RefNbr))
						{
							applicationCache.Delete(application);
						}
					}
				}
			}
		}

		private void VerifyVoidCheckNumberMatchesOriginalPayment(APPayment voidcheck)
		{
			if (_IsIntegrityCheck) return;
			bool hasVoidDoc = false;
			foreach (string origDocType in voidcheck.PossibleOriginalDocumentTypes())
			{
				foreach (PXResult<APPayment, CurrencyInfo, Currency, Vendor> res in APPayment_DocType_RefNbr
				.Search<APPayment.vendorID>(voidcheck.VendorID, origDocType, voidcheck.RefNbr))
				{
					APPayment payment = res;
					if (string.Equals(voidcheck.ExtRefNbr, payment.ExtRefNbr, StringComparison.OrdinalIgnoreCase))
					{
						hasVoidDoc = true;
						break;
					}
				}
			}
			if (!hasVoidDoc)
				throw new PXException(Messages.VoidAppl_CheckNbr_NotMatchOrigPayment);
		}

		protected void DeactivateOneTimeVendorIfAllDocsIsClosed(Vendor vendor)
		{
			if (vendor.VStatus != VendorStatus.OneTime)
				return;

			APRegister apRegister = PXSelect<APRegister,
												Where<APRegister.vendorID, Equal<Required<APRegister.vendorID>>,
														And<APRegister.released, Equal<True>,
														And<APRegister.openDoc, Equal<True>>>>>
												.SelectWindowed(this, 0, 1, vendor.BAccountID);

			if (apRegister != null)
				return;

			vendor.VStatus = VendorStatus.Inactive;
			Caches[typeof(Vendor)].Update(vendor);
			Caches[typeof(Vendor)].Persist(PXDBOperation.Update);
			Caches[typeof(Vendor)].Persisted(false);
		}

		/// <summary>
		/// Ensures that no unreleased voiding document exists for the specified payment.
		/// (If the applications of the voided and the voiding document are not
		/// synchronized, it can lead to a balance discrepancy, see AC-78131).
		/// </summary>
		public static void EnsureNoUnreleasedVoidPaymentExists(PXGraph selectGraph, APRegister doc, APRegister payment, string actionDescription)
		{
			APRegister unreleasedVoidPayment =
				HasUnreleasedVoidPayment<APRegister.docType, APRegister.refNbr>.Select(selectGraph, payment);

			if (unreleasedVoidPayment != null)
			{
				if (!(unreleasedVoidPayment.DocType == doc?.DocType && unreleasedVoidPayment.RefNbr == doc?.RefNbr))
				throw new PXException(
					Common.Messages.CannotPerformActionOnDocumentUnreleasedVoidPaymentExists,
					PXLocalizer.Localize(GetLabel.For<APDocType>(payment.DocType)),
					payment.RefNbr,
					PXLocalizer.Localize(actionDescription),
					PXLocalizer.Localize(GetLabel.For<APDocType>(unreleasedVoidPayment.DocType)),
					PXLocalizer.Localize(GetLabel.For<APDocType>(payment.DocType)),
					PXLocalizer.Localize(GetLabel.For<APDocType>(unreleasedVoidPayment.DocType)),
					unreleasedVoidPayment.RefNbr);
			}
		}

		[Obsolete(Common.Messages.MethodIsObsoleteRemoveInLaterAcumaticaVersions)]
		public static void EnsureNoUnreleasedVoidPaymentExists(PXGraph selectGraph, APRegister payment, string actionDescription)
		{
			EnsureNoUnreleasedVoidPaymentExists(selectGraph, null, payment, actionDescription);
		}

		/// <summary>
		/// The method to release payment part.
		/// The maintenance screen is "Checks And Payments" (AP302000).
		/// </summary>
		public virtual void ProcessPayment(
			JournalEntry je,
			APRegister doc,
			PXResult<APPayment, CurrencyInfo, Currency, Vendor, CashAccount> res)
		{
			APPayment apdoc = res;
			CurrencyInfo new_info = res;
			Currency paycury = res;
			Vendor vend = res;
			CashAccount cashacct = res;

			EnsureNoUnreleasedVoidPaymentExists(this, doc, apdoc, Common.Messages.ActionReleased);

			VendorClass vendclass = (VendorClass)PXSelectJoin<VendorClass, InnerJoin<APSetup, On<APSetup.dfltVendorClassID, Equal<VendorClass.vendorClassID>>>>.Select(this, null);

			bool isQuickCheckOrVoidQuickCheckDocument =
				apdoc.DocType == APDocType.QuickCheck ||
				apdoc.DocType == APDocType.VoidQuickCheck;

			if (doc.Released != true)
			{
				// Should always restore APRegister to ARPayment after invoice part release of cash sale
                PXCache<APRegister>.RestoreCopy(apdoc, doc);

				doc.CuryDocBal = doc.CuryOrigDocAmt;
				doc.DocBal = doc.OrigDocAmt;

				bool isDebit = (apdoc.DrCr == DrCr.Debit);

				GLTran tran = new GLTran();
				tran.SummPost = this.SummPost;
				tran.BranchID = cashacct.BranchID;
                tran.AccountID = cashacct.AccountID;
                tran.SubID = cashacct.SubID;
				tran.CuryDebitAmt = isDebit ? apdoc.CuryOrigDocAmt : 0m;
				tran.DebitAmt = isDebit ? apdoc.OrigDocAmt : 0m;
				tran.CuryCreditAmt = isDebit ? 0m : apdoc.CuryOrigDocAmt;
				tran.CreditAmt = isDebit ? 0m : apdoc.OrigDocAmt;
				tran.TranType = apdoc.DocType;
				tran.TranClass = apdoc.DocClass;
				tran.RefNbr = apdoc.RefNbr;
				tran.TranDesc = apdoc.DocDesc;
				tran.TranDate = apdoc.DocDate;
				FinPeriodIDAttribute.SetPeriodsByMaster<GLTran.finPeriodID>(je.GLTranModuleBatNbr.Cache, tran, apdoc.TranPeriodID);
				tran.CuryInfoID = new_info.CuryInfoID;
				tran.Released = true;
				tran.CATranID = apdoc.CATranID;
				tran.ReferenceID = apdoc.VendorID;
                tran.ProjectID = PM.ProjectDefaultAttribute.NonProject();
				tran.NonBillable = true;

				InsertPaymentTransaction(je, tran,
					new GLTranInsertionContext { APRegisterRecord = doc });

				/*Debit Payment AP Account*/
				tran = new GLTran();
				tran.SummPost = true;
                if (!APPaymentType.CanHaveBalance(apdoc.DocType))
				{
					tran.ZeroPost = false;
				}
				tran.BranchID = apdoc.BranchID;
				tran.AccountID = apdoc.APAccountID;
				tran.ReclassificationProhibited = true;
				tran.SubID = apdoc.APSubID;
				tran.CuryDebitAmt = isDebit ? 0m : apdoc.CuryOrigDocAmt;
				tran.DebitAmt = isDebit ? 0m : apdoc.OrigDocAmt;
				tran.CuryCreditAmt = isDebit ? apdoc.CuryOrigDocAmt : 0m;
				tran.CreditAmt = isDebit ? apdoc.OrigDocAmt : 0m;
				tran.TranType = apdoc.DocType;
				tran.TranClass = GLTran.tranClass.Payment;
				tran.RefNbr = apdoc.RefNbr;
				tran.TranDesc = apdoc.DocDesc;
				tran.TranDate = apdoc.DocDate;
				FinPeriodIDAttribute.SetPeriodsByMaster<GLTran.finPeriodID>(je.GLTranModuleBatNbr.Cache, tran, apdoc.TranPeriodID);
				tran.CuryInfoID = new_info.CuryInfoID;
				tran.Released = true;
				tran.ReferenceID = apdoc.VendorID;
                tran.ProjectID = PM.ProjectDefaultAttribute.NonProject();
				tran.NonBillable = true;

				UpdateHistory(tran, vend);
				UpdateHistory(tran, vend, new_info);

				InsertPaymentTransaction(je, tran,
					new GLTranInsertionContext { APRegisterRecord = doc });

				if (IsMigratedDocumentForProcessing(doc))
				{
					ProcessMigratedDocument(je, tran, apdoc, isDebit, vend, new_info);
				}

                foreach (APPaymentChargeTran charge in APPaymentChargeTran_DocType_RefNbr.Select(doc.DocType, doc.RefNbr))
                {
					bool isCADebit = charge.GetCASign() == 1;

					tran = new GLTran();
					tran.SummPost = this.SummPost;
					tran.BranchID = cashacct.BranchID;
                    tran.AccountID = cashacct.AccountID;
                    tran.SubID = cashacct.SubID;
					tran.CuryDebitAmt = isCADebit ? charge.CuryTranAmt : 0m;
					tran.DebitAmt = isCADebit ? charge.TranAmt : 0m;
					tran.CuryCreditAmt = isCADebit ? 0m : charge.CuryTranAmt;
					tran.CreditAmt = isCADebit ? 0m : charge.TranAmt;
                    tran.TranType = charge.DocType;
                    tran.TranClass = apdoc.DocClass;
                    tran.RefNbr = charge.RefNbr;
                    tran.TranDesc = charge.TranDesc;
                    tran.TranDate = charge.TranDate;
                    FinPeriodIDAttribute.SetPeriodsByMaster<GLTran.finPeriodID>(je.GLTranModuleBatNbr.Cache, tran, charge.TranPeriodID);
                    tran.CuryInfoID = new_info.CuryInfoID;
                    tran.Released = true;
                    tran.CATranID = charge.CashTranID;
                    tran.ReferenceID = apdoc.VendorID;

					InsertPaymentChargeTransaction(je, tran,
						new GLTranInsertionContext { APRegisterRecord = doc, APPaymentChargeTranRecord = charge });

                    tran = new GLTran();
                    tran.SummPost = true;
                    tran.ZeroPost = false;
                    tran.BranchID = apdoc.BranchID;
                    tran.AccountID = charge.AccountID;
                    tran.SubID = charge.SubID;
					tran.CuryDebitAmt = isCADebit ? 0m : charge.CuryTranAmt;
					tran.DebitAmt = isCADebit ? 0m : charge.TranAmt;
					tran.CuryCreditAmt = isCADebit ? charge.CuryTranAmt : 0m;
					tran.CreditAmt = isCADebit ? charge.TranAmt : 0m;
                    tran.TranType = charge.DocType;
                    tran.TranClass = GLTran.tranClass.Charge;
                    tran.RefNbr = charge.RefNbr;
                    tran.TranDesc = charge.TranDesc;
                    tran.TranDate = charge.TranDate;
                    FinPeriodIDAttribute.SetPeriodsByMaster<GLTran.finPeriodID>(je.GLTranModuleBatNbr.Cache, tran, charge.TranPeriodID);
                    tran.CuryInfoID = new_info.CuryInfoID;
                    tran.Released = true;
                    tran.ReferenceID = apdoc.VendorID;

					InsertPaymentChargeTransaction(je, tran,
						new GLTranInsertionContext { APRegisterRecord = doc, APPaymentChargeTranRecord = charge });

					charge.Released = true;
					APPaymentChargeTran_DocType_RefNbr.Update(charge);

				}
                if(!isQuickCheckOrVoidQuickCheckDocument)
					ProcessOriginTranPost(apdoc);
                doc.Voided = false;
				doc.OpenDoc = true;
				doc.ClosedDate = null;
				doc.ClosedFinPeriodID = null;
				doc.ClosedTranPeriodID = null;

				if (apdoc.VoidAppl == true)
				{
					VerifyVoidCheckNumberMatchesOriginalPayment(apdoc);
				}
				else
				{
                    PaymentMethod paytype = PXSelect<PaymentMethod, Where<PaymentMethod.paymentMethodID, Equal<Required<PaymentMethod.paymentMethodID>>>>.Select(this, apdoc.PaymentMethodID);
					if (_IsIntegrityCheck == false && APPaymentEntry.MustPrintCheck(apdoc, paytype))
					{
						throw new PXException(Messages.Check_NotPrinted_CannotRelease);
					}
				}
			}
			else if (_IsIntegrityCheck && doc.DocType == APDocType.Prepayment && doc.AdjCntr == 0)
			{
				// This is the only good place to reset prepayment request balance
				doc.CuryDocBal = 0m;
				doc.DocBal = 0m;
			}

			if (isQuickCheckOrVoidQuickCheckDocument)
			{
				if (_IsIntegrityCheck == false)
				{
					foreach (Type extension in this.Caches<APAdjust>().GetExtensionTables() ?? new List<Type> { })
					{
						PXDatabase.ForceDelete(extension,
							new PXDataFieldRestrict(nameof(APAdjust.adjgDocType), doc.DocType),
							new PXDataFieldRestrict(nameof(APAdjust.adjgRefNbr), doc.RefNbr));
					}

					PXDatabase.Delete<APAdjust>(
						new PXDataFieldRestrict<APAdjust.adjgDocType>(PXDbType.Char, 3, doc.DocType, PXComp.EQ),
						new PXDataFieldRestrict<APAdjust.adjgRefNbr>(PXDbType.VarChar, 15, doc.RefNbr, PXComp.EQ));

					CreateSelfApplicationForDocument(doc);
				}

				if (doc.DocType == APDocType.VoidQuickCheck)
				{
					VerifyVoidCheckNumberMatchesOriginalPayment(apdoc);
				}

				doc.CuryDocBal += doc.CuryOrigDiscAmt + doc.CuryOrigWhTaxAmt;
				doc.DocBal += doc.OrigDiscAmt + doc.OrigWhTaxAmt;
				doc.ClosedDate = doc.DocDate;
				doc.ClosedFinPeriodID = doc.FinPeriodID;
				doc.ClosedTranPeriodID = doc.TranPeriodID;
			}

			doc.Released = true;
		}

		/// <summary>
		/// The method to verify invoice balances and close it if needed.
		/// This verification should be called after
		/// release process of payment and applications.
		/// </summary>
		public virtual void VerifyDocumentBalanceAndClose(APRegister apdoc)
		{
			if (apdoc.IsOriginalRetainageDocument()
				? IsFullyProcessedOriginalRetainageDocument(apdoc)
				: apdoc.HasZeroBalance<APRegister.curyDocBal, APTran.curyTranBal>(this))
			{
				CloseInvoiceAndClearBalances(apdoc);
			}
			else
			{
				OpenInvoiceAndRecoverBalances(apdoc);
			}
		}
		/// <summary>
		/// The method to verify payment balances and close it if needed.
		/// This verification should be called after
		/// release process of payment and applications.
		/// </summary>
		public virtual void VerifyPaymentRoundAndClose(
			JournalEntry je,
			APRegister paymentRegister,
			APPayment payment,
			Vendor paymentVendor,
			CurrencyInfo new_info,
			Currency paycury,
			Tuple<APAdjust, CurrencyInfo> lastAdjustment)
		{
			APAdjust prev_adj = lastAdjustment.Item1;
			CurrencyInfo prev_info = lastAdjustment.Item2;

			if (_IsIntegrityCheck == false &&
				(payment.VoidAppl == true ? paymentRegister.CuryDocBal > 0m : paymentRegister.CuryDocBal < 0m))
			{
				throw new PXException(Messages.DocumentBalanceNegative);
			}

			if (paymentRegister.CuryDocBal == 0m &&
				paymentRegister.DocBal != 0m &&
				prev_adj.AdjdRefNbr != null)
			{
				if (prev_adj.VoidAppl == true || Equals(new_info.CuryID, new_info.BaseCuryID))
				{
					throw new PXException(Messages.BugAlertBalanceWasNotRecalculatedInBase);
				}

				// BaseCalc should be false
				//
				prev_adj.AdjAmt += paymentRegister.DocBal;

				decimal? roundingLoss = prev_adj.ReverseGainLoss == false
					? paymentRegister.DocBal
					: -paymentRegister.DocBal;
				prev_adj.RGOLAmt -= roundingLoss;

				prev_adj = (APAdjust)Caches[typeof(APAdjust)].Update(prev_adj);
				foreach (APTranPost post in
					this.Caches<APTranPost>()
						.Inserted
						.Cast<APTranPost>()
						.Where(d =>d.RefNoteID == prev_adj.NoteID))
				{
					post.Amt = post.Type == APTranPost.type.RGOL
						? 0
						: prev_adj.AdjAmt;
					post.RGOLAmt = prev_adj.RGOLAmt;
				}

				// Signs are reversed to RGOL
				//
				GLTran tran = new GLTran();
				tran.SummPost = this.SummPost;
				tran.BranchID = payment.BranchID;
				tran.AccountID = (roundingLoss < 0m)
					? paycury.RoundingGainAcctID
					: paycury.RoundingLossAcctID;
				tran.SubID = (roundingLoss < 0m)
					? CM.GainLossSubAccountMaskAttribute.GetSubID<Currency.roundingGainSubID>(je, tran.BranchID, paycury)
					: CM.GainLossSubAccountMaskAttribute.GetSubID<Currency.roundingLossSubID>(je, tran.BranchID, paycury);
				tran.OrigAccountID = prev_adj.AdjdAPAcct;
				tran.OrigSubID = prev_adj.AdjdAPSub;
				tran.DebitAmt = (roundingLoss > 0m) ? roundingLoss : 0m;
				tran.CuryDebitAmt = 0m;
				tran.CreditAmt = (roundingLoss < 0m) ? -roundingLoss : 0m;
				tran.CuryCreditAmt = 0m;
				tran.TranType = prev_adj.AdjgDocType;
				tran.TranClass = GLTran.tranClass.RealizedAndRoundingGOL;
				tran.RefNbr = prev_adj.AdjgRefNbr;
				tran.TranDesc = payment.DocDesc;
				tran.TranDate = prev_adj.AdjgDocDate;
				FinPeriodIDAttribute.SetPeriodsByMaster<GLTran.finPeriodID>(je.GLTranModuleBatNbr.Cache, tran, prev_adj.AdjgTranPeriodID);
				tran.CuryInfoID = new_info.CuryInfoID;
				tran.Released = true;
				tran.ReferenceID = payment.VendorID;
				tran.ProjectID = PM.ProjectDefaultAttribute.NonProject();

				UpdateHistory(tran, paymentVendor);
				UpdateHistory(tran, paymentVendor, prev_info);

				// AC-96772: The modified document can be in one of two caches.
				// If the document is changed in two caches there will be an error:
				// "Another process has updated the 'APRegister' record. Your changes will be lost."
				// -
				var adjdPayment = (APPayment)APPayment_DocType_RefNbr.Cache.Locate(new APPayment { DocType = prev_adj.AdjdDocType, RefNbr = prev_adj.AdjdRefNbr });
				if (adjdPayment != null)
				{
					adjdPayment.RGOLAmt -= roundingLoss;
					APPayment_DocType_RefNbr.Cache.Update(adjdPayment);
				}
				else
				{
				var adjdDoc = (APRegister)APDocument.Cache.Locate(new APRegister { DocType = prev_adj.AdjdDocType, RefNbr = prev_adj.AdjdRefNbr });
				if (adjdDoc != null)
				{
					adjdDoc.RGOLAmt -= roundingLoss;
					APDocument.Cache.Update(adjdDoc);
				}
				}

				InsertAdjustmentsRoundingTransaction(je, tran,
					new GLTranInsertionContext { APRegisterRecord = paymentRegister, APAdjustRecord = prev_adj });

				// Credit Payment AR Account
				//
				tran = new GLTran();
				tran.SummPost = true;
				tran.ZeroPost = false;
				tran.BranchID = payment.BranchID;
				tran.AccountID = payment.APAccountID;
				tran.ReclassificationProhibited = true;
				tran.SubID = payment.APSubID;
				tran.CreditAmt = (roundingLoss > 0m) ? roundingLoss : 0m;
				tran.CuryCreditAmt = 0m;
				tran.DebitAmt = (roundingLoss < 0m) ? -roundingLoss : 0m;
				tran.CuryDebitAmt = 0m;
				tran.TranType = prev_adj.AdjgDocType;
				tran.TranClass = GLTran.tranClass.Payment;
				tran.RefNbr = prev_adj.AdjgRefNbr;
				tran.TranDesc = payment.DocDesc;
				tran.TranDate = prev_adj.AdjgDocDate;
				FinPeriodIDAttribute.SetPeriodsByMaster<GLTran.finPeriodID>(je.GLTranModuleBatNbr.Cache, tran, prev_adj.AdjgTranPeriodID);
				tran.CuryInfoID = new_info.CuryInfoID;
				tran.Released = true;
				tran.ReferenceID = payment.VendorID;
				tran.OrigAccountID = prev_adj.AdjdAPAcct;
				tran.OrigSubID = prev_adj.AdjdAPSub;
				tran.ProjectID = PM.ProjectDefaultAttribute.NonProject();

				UpdateHistory(tran, paymentVendor);
				UpdateHistory(tran, paymentVendor, new_info);

				InsertAdjustmentsRoundingTransaction(je, tran,
					new GLTranInsertionContext { APRegisterRecord = paymentRegister, APAdjustRecord = prev_adj });
			}

			bool hasAnyApplications = prev_adj.AdjdRefNbr != null;

			if (!paymentRegister.IsOriginalRetainageDocument() || paymentRegister.DocType != APDocType.DebitAdj || hasAnyApplications)
			{
			ClosePayment(paymentRegister, payment, paymentVendor);
		}
		}

		protected virtual CM.CurrencyInfo GetCurrencyInfoCopyForGL(JournalEntry je, CurrencyInfo info)
		{
			CM.CurrencyInfo new_info = info.GetCM();
			new_info.CuryInfoID = null;
			new_info.ModuleCode = "GL";
			new_info = je.currencyinfo.Insert(new_info) ?? new_info;
			return new_info;
		}

		/// <summary>
		/// The method to release applications only
		/// without payment part.
		/// </summary>
		protected virtual Tuple<APAdjust, CurrencyInfo> ProcessAdjustments(
			JournalEntry je,
			PXResultset<APAdjust> adjustments,
			APRegister paymentRegister,
			APPayment payment,
			Vendor paymentVendor,
			CM.CurrencyInfo new_info,
			Currency paycury)
		{
			APAdjust prev_adj = new APAdjust();
			CurrencyInfo prev_info = new CurrencyInfo();

			// All special applications, which have been created in migration mode
			// for migrated document - should be excluded from the processing
			//
			foreach (PXResult<APAdjust, CurrencyInfo, Currency, APInvoice, APPayment, Standalone.APRegisterAlias, APTran> adjres in
				adjustments.AsEnumerable().Where(row => ((APAdjust)row).IsInitialApplication != true))
			{
				APAdjust adj = adjres;
				CurrencyInfo vouch_info = adjres;
				Currency cury = adjres;
				APInvoice adjddoc = adjres;
				APPayment adjgdoc = adjres;
				APTran line = adjres;

				// Restore full invoice / payment from the "single table" stripped version.
				//
				if (adjddoc?.RefNbr != null)
				{
					PXCache<APRegister>.RestoreCopy(adjddoc, (Standalone.APRegisterAlias)adjres);
				}
				else if (adjgdoc?.RefNbr != null)
				{
					PXCache<APRegister>.RestoreCopy(adjgdoc, (Standalone.APRegisterAlias)adjres);
				}

				if (adjddoc?.PaymentsByLinesAllowed == true && adj.AdjdLineNbr == 0)
				{
					continue;
				}

				if (_IsIntegrityCheck == false && adj.PendingPPD == true)
				{
					adjddoc.PendingPPD = !adj.Voided;
					APDocument.Cache.Update(adjddoc);
				}
				EnsureNoUnreleasedVoidPaymentExists(
					this,
					paymentRegister,
					adjgdoc,
					paymentRegister.DocType == APDocType.Refund
						? Common.Messages.ActionRefunded
						: Common.Messages.ActionAdjusted);

				if (adj.CuryAdjgAmt == 0m && adj.CuryAdjgDiscAmt == 0m && adj.CuryAdjgWhTaxAmt == 0m
					&& (!adjddoc.IsOriginalRetainageDocument() || adjddoc.RetainageUnreleasedAmt != 0 || adjddoc.RetainageReleased != 0))
				{
					APAdjust_AdjgDocType_RefNbr_VendorID.Cache.Delete(adj);
					continue;
				}

				if (adj.Hold == true)
				{
					throw new PXException(Messages.Document_OnHold_CannotRelease);
				}

				if (adjddoc.RefNbr != null)
				{
					UpdateBalances(adj, adjddoc, paymentVendor, line);
					if (_IsIntegrityCheck == false && paymentVendor.Vendor1099 == true
						|| AP1099Tran_Select.Any(adjddoc.DocType, adjddoc.RefNbr))
					{
						Update1099(adj, adjddoc);
					}
					UpdateWithholding(je, adj, adjddoc, payment, paymentVendor, vouch_info);
				}
				else
				{
					UpdateBalances(adj, adjgdoc, paymentVendor);
				}

				ProcessAdjustmentAdjusting(je, adj, payment, paymentVendor, CurrencyInfo.GetEX(new_info));
				ProcessAdjustmentAdjusted(je, adj, payment, paymentVendor, vouch_info, CurrencyInfo.GetEX(new_info));
				ProcessAdjustmentCashDiscount(je, adj, payment, paymentVendor, vouch_info, CurrencyInfo.GetEX(new_info));
				ProcessAdjustmentGOL(je, adj, payment, paymentVendor, paycury, cury, CurrencyInfo.GetEX(new_info), vouch_info);

				// true for Cash Sale and Reverse Cash Sale
				if (adj.AdjgDocType != adj.AdjdDocType || adj.AdjgRefNbr != adj.AdjdRefNbr)
				{
					paymentRegister.CuryDocBal -= adj.AdjgBalSign * adj.CuryAdjgAmt;
					paymentRegister.DocBal -= adj.AdjgBalSign * adj.AdjAmt;

					AdjustmentProcessingOnApplication(paymentRegister, adj);
				}

				if (_IsIntegrityCheck == false)
				{
					if (je.GLTranModuleBatNbr.Cache.IsInsertedUpdatedDeleted)
					{
						je.Save.Press();
					}

					if (!je.BatchModule.Cache.IsDirty)
					{
						adj.AdjBatchNbr = (je.BatchModule.Current).BatchNbr;
					}

					adj.Released = true;
					adj = (APAdjust)Caches[typeof(APAdjust)].Update(adj);
				}

				prev_adj = adj;
				prev_info = adjres;

				ProcessSVATAdjustments(adj, adjddoc, paymentRegister);
				ProcessAdjustmentTranPost(adj, adjddoc, paymentRegister);

				var adjpayment = this.APPayment_DocType_RefNbr.Cache.Locate(
					new APPayment()
					{
						DocType = adjddoc.DocType ?? adjgdoc.DocType,
						RefNbr = adjddoc.RefNbr ?? adjgdoc.RefNbr
					});
				bool isPaymentProcessed = 
					payment != null &&
					this.APPayment_DocType_RefNbr.Cache.GetStatus(adjpayment) == PXEntryStatus.Inserted ||
					this.APPayment_DocType_RefNbr.Cache.GetStatus(adjpayment) == PXEntryStatus.Updated;
				
				if (!isPaymentProcessed)
				{
					if (adjddoc.RefNbr != null)
					{
						VerifyDocumentBalanceAndClose(adjddoc);
						UpdateRetainageBalances(adj, adjddoc, paymentRegister);
					}
					else if (adjgdoc.RefNbr != null)
					{
						VerifyDocumentBalanceAndClose(adjgdoc);
					}
				}
			}
			
			return new Tuple<APAdjust, CurrencyInfo>(prev_adj, prev_info);
		}

		[Obsolete(Common.Messages.MethodIsObsoleteRemoveInLaterAcumaticaVersions)]
		private void ProcessPayByLineDebitAdjAdjustment(APRegister paymentRegister, APAdjust adj)
		{
			var curyAdjBal = adj.CuryAdjgAmt;
			var adjBal = adj.AdjAmt;

			IEnumerable<PXResult<APTran>> transactions = PXSelect<APTran,
				Where<APTran.tranType, Equal<Required<APPayment.docType>>,
					And<APTran.refNbr, Equal<Required<APPayment.refNbr>>>>,
				OrderBy<Asc<APTran.lineNbr>>>
					.Select(this, paymentRegister.DocType, paymentRegister.RefNbr);
			if (curyAdjBal < 0)
				transactions = transactions.OrderByDescending(_ => PXResult.Unwrap<APTran>(_).LineNbr);

			foreach (APTran tran in transactions)
			{
				APTran t = PXCache<APTran>.CreateCopy(tran);

				Decimal? curyDelta =
					adj.Voided != true && t.CuryTranBal <= curyAdjBal ? t.CuryTranBal :
					adj.Voided == true && t.CuryTranBal - t.CuryOrigTranAmt > curyAdjBal ? t.CuryTranBal - t.CuryOrigTranAmt :
					curyAdjBal;

				Decimal? delta =
					adj.Voided != true && t.TranBal <= adjBal ? t.TranBal :
					adj.Voided == true && t.TranBal - t.OrigTranAmt > adjBal ? t.TranBal - t.OrigTranAmt :
					adjBal;

				curyAdjBal -= curyDelta;
				adjBal -= delta;
				t.CuryTranBal -= curyDelta;
				t.TranBal -= delta;
				this.APTran_TranType_RefNbr.Update(t);
				if (curyAdjBal == 0m) break;
			}
		}

		private void ProcessAdjustmentAdjusting(
			JournalEntry je,
			APAdjust adj,
			APPayment payment,
			Vendor paymentVendor,
			CurrencyInfo new_info)
		{
				/*Credit Payment AP Account*/
				GLTran tran = new GLTran();
				tran.SummPost = true;
				tran.ZeroPost = false;
				tran.BranchID = adj.AdjgBranchID;
			tran.AccountID = payment.APAccountID;
				tran.ReclassificationProhibited = true;
			tran.SubID = payment.APSubID;
				tran.DebitAmt = (adj.AdjgGLSign == 1m) ? 0m : adj.AdjAmt;
				tran.CuryDebitAmt = (adj.AdjgGLSign == 1m) ? 0m : adj.CuryAdjgAmt;
				tran.CreditAmt = (adj.AdjgGLSign == 1m) ? adj.AdjAmt : 0m;
				tran.CuryCreditAmt = (adj.AdjgGLSign == 1m) ? adj.CuryAdjgAmt : 0m;
				tran.TranType = adj.AdjgDocType;
				tran.TranClass = GLTran.tranClass.Payment;
				tran.RefNbr = adj.AdjgRefNbr;
			tran.TranDesc = payment.DocDesc;
				tran.TranDate = adj.AdjgDocDate;
				FinPeriodIDAttribute.SetPeriodsByMaster<GLTran.finPeriodID>(je.GLTranModuleBatNbr.Cache, tran, adj.AdjgTranPeriodID);
				tran.CuryInfoID = new_info.CuryInfoID;
				tran.Released = true;
			tran.ReferenceID = payment.VendorID;
				tran.ProjectID = PM.ProjectDefaultAttribute.NonProject();

			UpdateHistory(tran, paymentVendor);
			UpdateHistory(tran, paymentVendor, new_info);

				InsertAdjustmentsAdjustingTransaction(je, tran,
				new GLTranInsertionContext { APRegisterRecord = payment, APAdjustRecord = adj });
		}

		private void ProcessAdjustmentAdjusted(
			JournalEntry je,
			APAdjust adj,
			APPayment payment,
			Vendor paymentVendor,
			CurrencyInfo vouch_info,
			CurrencyInfo new_info)
		{
				/*Debit Voucher AP Account/minus RGOL for refund*/
			GLTran tran = new GLTran();
				tran.SummPost = true;
				tran.ZeroPost = false;
				tran.BranchID = adj.AdjdBranchID;
				tran.AccountID = adj.AdjdAPAcct;
				tran.ReclassificationProhibited = true;
				tran.SubID = adj.AdjdAPSub;
				tran.CreditAmt = (adj.AdjgGLSign == 1m) ? 0m : adj.AdjAmt + adj.AdjDiscAmt + adj.AdjWhTaxAmt - adj.RGOLAmt;
				tran.CuryCreditAmt = (adj.AdjgGLSign == 1m) ? 0m : (object.Equals(new_info.CuryID, new_info.BaseCuryID) ? tran.CreditAmt : adj.CuryAdjgAmt + adj.CuryAdjgDiscAmt + adj.CuryAdjgWhTaxAmt);
				tran.DebitAmt = (adj.AdjgGLSign == 1m) ? adj.AdjAmt + adj.AdjDiscAmt + adj.AdjWhTaxAmt + adj.RGOLAmt : 0m;
				tran.CuryDebitAmt = (adj.AdjgGLSign == 1m) ? (object.Equals(new_info.CuryID, new_info.BaseCuryID) ? tran.DebitAmt : adj.CuryAdjgAmt + adj.CuryAdjgDiscAmt + adj.CuryAdjgWhTaxAmt) : 0m;
				tran.TranType = adj.AdjgDocType;
				//always N for AdjdDocs except Prepayment
				tran.TranClass = APDocType.DocClass(adj.AdjdDocType);
				tran.RefNbr = adj.AdjgRefNbr;
			tran.TranDesc = payment.DocDesc;
				tran.TranDate = adj.AdjgDocDate;
				FinPeriodIDAttribute.SetPeriodsByMaster<GLTran.finPeriodID>(je.GLTranModuleBatNbr.Cache, tran, adj.AdjgTranPeriodID);
				tran.CuryInfoID = new_info.CuryInfoID;
				tran.Released = true;
			tran.ReferenceID = payment.VendorID;
				tran.ProjectID = PM.ProjectDefaultAttribute.NonProject();

			UpdateHistory(tran, paymentVendor);

				InsertAdjustmentsAdjustedTransaction(je, tran,
				new GLTranInsertionContext { APRegisterRecord = payment, APAdjustRecord = adj });

				/*Update CuryHistory in Voucher currency*/
				tran.CuryCreditAmt = (adj.AdjgGLSign == 1m) ? 0m : (object.Equals(vouch_info.CuryID, vouch_info.BaseCuryID) ? tran.CreditAmt : adj.CuryAdjdAmt + adj.CuryAdjdDiscAmt + adj.CuryAdjdWhTaxAmt);
				tran.CuryDebitAmt = (adj.AdjgGLSign == 1m) ? (object.Equals(vouch_info.CuryID, vouch_info.BaseCuryID) ? tran.DebitAmt : adj.CuryAdjdAmt + adj.CuryAdjdDiscAmt + adj.CuryAdjdWhTaxAmt) : 0m;
			UpdateHistory(tran, paymentVendor, vouch_info);
				}

		private void PostReduceOnEarlyPaymentTran(JournalEntry je, APInvoice doc,
			 Vendor vend, CurrencyInfo currencyInfo,
			bool isCredit, decimal curyAmount, decimal amount)
		{
			GLTran tran = new GLTran();
			tran.SummPost = this.SummPost;
			tran.BranchID = doc.BranchID;
			tran.AccountID = vend.DiscTakenAcctID;
			tran.SubID = vend.DiscTakenSubID;
			tran.OrigAccountID = doc.APAccountID;
			tran.OrigSubID = doc.APSubID;
			tran.DebitAmt = isCredit ? 0m : amount;
			tran.CuryDebitAmt = isCredit ? 0m : curyAmount;
			tran.CreditAmt = isCredit ? amount : 0m;
			tran.CuryCreditAmt = isCredit ? curyAmount : 0m;
			tran.TranType = doc.DocType;
			tran.TranClass = GLTran.tranClass.Discount;
			tran.RefNbr = doc.RefNbr;
			tran.TranDesc = doc.DocDesc;
			tran.TranDate = doc.DocDate;
			FinPeriodIDAttribute.SetPeriodsByMaster<GLTran.finPeriodID>(je.GLTranModuleBatNbr.Cache, tran, doc.TranPeriodID);
			tran.CuryInfoID = currencyInfo.CuryInfoID;
			tran.Released = true;
			tran.ReferenceID = doc.VendorID;
			tran.ProjectID = PM.ProjectDefaultAttribute.NonProject();

			UpdateHistory(tran, vend);

			InsertAdjustmentsCashDiscountTransaction(je, tran,
				new GLTranInsertionContext { APRegisterRecord = doc });

			tran.CuryDebitAmt = isCredit ? 0m : curyAmount;
			tran.CuryCreditAmt = isCredit ? curyAmount : 0m;
			UpdateHistory(tran, vend, currencyInfo);
		}

		private void ProcessAdjustmentCashDiscount(
			JournalEntry je,
			APAdjust adj,
			APPayment payment,
			Vendor paymentVendor,
			CurrencyInfo vouch_info,
			CurrencyInfo new_info)
		{
			/*Credit Discount Taken/does not apply to refund, since no disc in AD*/
			GLTran tran = new GLTran();
			tran.SummPost = this.SummPost;
			tran.BranchID = adj.AdjdBranchID;
			tran.AccountID = paymentVendor.DiscTakenAcctID;
			tran.SubID = paymentVendor.DiscTakenSubID;
			tran.OrigAccountID = adj.AdjdAPAcct;
			tran.OrigSubID = adj.AdjdAPSub;
			tran.DebitAmt = (adj.AdjgGLSign == 1m) ? 0m : adj.AdjDiscAmt;
			tran.CuryDebitAmt = (adj.AdjgGLSign == 1m) ? 0m : adj.CuryAdjgDiscAmt;
			tran.CreditAmt = (adj.AdjgGLSign == 1m) ? adj.AdjDiscAmt : 0m;
			tran.CuryCreditAmt = (adj.AdjgGLSign == 1m) ? adj.CuryAdjgDiscAmt : 0m;
			tran.TranType = adj.AdjgDocType;
			tran.TranClass = GLTran.tranClass.Discount;
			tran.RefNbr = adj.AdjgRefNbr;
			tran.TranDesc = payment.DocDesc;
			tran.TranDate = adj.AdjgDocDate;
			FinPeriodIDAttribute.SetPeriodsByMaster<GLTran.finPeriodID>(je.GLTranModuleBatNbr.Cache, tran, adj.AdjgTranPeriodID);
			tran.CuryInfoID = new_info.CuryInfoID;
			tran.Released = true;
			tran.ReferenceID = payment.VendorID;
			tran.ProjectID = PM.ProjectDefaultAttribute.NonProject();

			UpdateHistory(tran, paymentVendor);

			InsertAdjustmentsCashDiscountTransaction(je, tran,
				new GLTranInsertionContext { APRegisterRecord = payment, APAdjustRecord = adj });

			/*Update CuryHistory in Voucher currency*/
			tran.CuryDebitAmt = (adj.AdjgGLSign == 1m) ? 0m : adj.CuryAdjdDiscAmt;
			tran.CuryCreditAmt = (adj.AdjgGLSign == 1m) ? adj.CuryAdjdDiscAmt : 0m;
			UpdateHistory(tran, paymentVendor, vouch_info);
		}

		private void ProcessAdjustmentGOL(
			JournalEntry je,
			APAdjust adj,
			APPayment payment,
			Vendor vendor,
			Currency paycury,
			Currency cury,
			CurrencyInfo new_info,
			CurrencyInfo vouch_info)
		{
			if ((cury.RealGainAcctID == null || cury.RealLossAcctID == null) &&
				(paycury.RoundingGainAcctID == null || paycury.RoundingLossAcctID == null))
			{
				return;
			}

			bool useGainAccount =
				(adj.RGOLAmt > 0m && !adj.VoidAppl.Value) ||
								  (adj.RGOLAmt < 0m && adj.VoidAppl.Value);

			decimal? debitAmount = adj.RGOLAmt < 0m ? -1m * adj.RGOLAmt : 0m;
			decimal? creditAmount = adj.RGOLAmt > 0m ? adj.RGOLAmt : 0m;

			bool areNewInfoCurryEqualAndVounchInfoCurryUnequal =
				Equals(new_info.CuryID, new_info.BaseCuryID) &&
				!Equals(vouch_info.CuryID, vouch_info.BaseCuryID);

			GLTran tran = new GLTran
			{
				SummPost = SummPost,
				BranchID = adj.AdjdBranchID,
				OrigAccountID = adj.AdjdAPAcct,
				OrigSubID = adj.AdjdAPSub,
				DebitAmt = debitAmount,
				CuryDebitAmt = areNewInfoCurryEqualAndVounchInfoCurryUnequal ? debitAmount : 0m,
				CreditAmt = creditAmount,
				CuryCreditAmt = areNewInfoCurryEqualAndVounchInfoCurryUnequal ? creditAmount : 0m,
				TranType = adj.AdjgDocType,
				TranClass = GLTran.tranClass.RealizedAndRoundingGOL,
				RefNbr = adj.AdjgRefNbr,
				TranDesc = payment.DocDesc,
				TranDate = adj.AdjgDocDate,
				CuryInfoID = new_info.CuryInfoID,
				Released = true,
				ReferenceID = payment.VendorID,
				ProjectID = PM.ProjectDefaultAttribute.NonProject()
			};
		    FinPeriodIDAttribute.SetPeriodsByMaster<GLTran.finPeriodID>(je.GLTranModuleBatNbr.Cache, tran, adj.AdjgTranPeriodID);

			if (cury.RealGainAcctID != null && cury.RealLossAcctID != null)
			{
				/*Debit/Credit RGOL Account*/
				tran.AccountID = useGainAccount ? cury.RealGainAcctID : cury.RealLossAcctID;
				tran.SubID = useGainAccount
					? CM.GainLossSubAccountMaskAttribute.GetSubID<Currency.realGainSubID>(je, adj.AdjdBranchID, cury)
					: CM.GainLossSubAccountMaskAttribute.GetSubID<Currency.realLossSubID>(je, adj.AdjdBranchID, cury);
			}
			else
			{
				//Debit/Credit Rounding Gain-Loss Account
				tran.AccountID = useGainAccount ? paycury.RoundingGainAcctID : paycury.RoundingLossAcctID;
				tran.SubID = useGainAccount
					? CM.GainLossSubAccountMaskAttribute.GetSubID<Currency.roundingGainSubID>(je, adj.AdjdBranchID, paycury)
					: CM.GainLossSubAccountMaskAttribute.GetSubID<Currency.roundingLossSubID>(je, adj.AdjdBranchID, paycury);
			}

			UpdateHistory(tran, vendor);
			InsertAdjustmentsGOLTransaction(je, tran,
				new GLTranInsertionContext { APRegisterRecord = payment, APAdjustRecord = adj });

			/*Update CuryHistory in Voucher currency*/
			tran.CuryDebitAmt = 0m;
			tran.CuryCreditAmt = 0m;
			UpdateHistory(tran, vendor, vouch_info);
		}

		protected virtual void ProcessSVATAdjustments(APAdjust adj, APRegister adjddoc, APRegister adjgdoc)
		{
			if (PXAccess.FeatureInstalled<FeaturesSet.vATReporting>() && _IsIntegrityCheck == false)
			{
				foreach (SVATConversionHist docSVAT in PXSelect<SVATConversionHist, Where<
					SVATConversionHist.module, Equal<BatchModule.moduleAP>,
					And2<Where<SVATConversionHist.adjdDocType, Equal<Current<APAdjust.adjdDocType>>,
						And<SVATConversionHist.adjdRefNbr, Equal<Current<APAdjust.adjdRefNbr>>,
						Or<SVATConversionHist.adjdDocType, Equal<Current<APAdjust.adjgDocType>>,
						And<SVATConversionHist.adjdRefNbr, Equal<Current<APAdjust.adjgRefNbr>>>>>>,
					And<SVATConversionHist.reversalMethod, Equal<SVATTaxReversalMethods.onPayments>,
					And<Where<SVATConversionHist.adjdDocType, Equal<SVATConversionHist.adjgDocType>,
						And<SVATConversionHist.adjdRefNbr, Equal<SVATConversionHist.adjgRefNbr>>>>>>>>
					.SelectMultiBound(this, new object[] { adj }))
				{
					bool isPayment = adj.AdjgDocType == docSVAT.AdjdDocType && adj.AdjgRefNbr == docSVAT.AdjdRefNbr;
					decimal percent = isPayment
						? ((adj.CuryAdjgAmt ?? 0m) + (adj.CuryAdjgDiscAmt ?? 0m) + (adj.CuryAdjgWhTaxAmt ?? 0m)) / (adjgdoc.CuryOrigDocAmt ?? 0m)
						: ((adj.CuryAdjdAmt ?? 0m) + (adj.CuryAdjdDiscAmt ?? 0m) + (adj.CuryAdjdWhTaxAmt ?? 0m)) / (adjddoc.CuryOrigDocAmt ?? 0m);

					SVATConversionHist adjSVAT = new SVATConversionHist
					{
						Module = BatchModule.AP,
						AdjdBranchID = adj.AdjdBranchID,
						AdjdDocType = isPayment ? adj.AdjgDocType : adj.AdjdDocType,
						AdjdRefNbr = isPayment ? adj.AdjgRefNbr : adj.AdjdRefNbr,
						AdjdLineNbr = adj.AdjdLineNbr,
						AdjgDocType = isPayment ? adj.AdjdDocType : adj.AdjgDocType,
						AdjgRefNbr = isPayment ? adj.AdjdRefNbr : adj.AdjgRefNbr,
						AdjNbr = adj.AdjNbr,
						AdjdDocDate = adj.AdjgDocDate,

						TaxID = docSVAT.TaxID,
						TaxType = docSVAT.TaxType,
						TaxRate = docSVAT.TaxRate,
						VendorID = docSVAT.VendorID,
						ReversalMethod = SVATTaxReversalMethods.OnPayments,

						CuryInfoID = docSVAT.CuryInfoID,
					};

					adjSVAT.FillAmounts(GetExtension<MultiCurrency>().GetCurrencyInfo(docSVAT.CuryInfoID), docSVAT.CuryTaxableAmt, docSVAT.CuryTaxAmt, percent);

					FinPeriodIDAttribute.SetPeriodsByMaster<SVATConversionHist.adjdFinPeriodID>(
						SVATConversionHistory.Cache, adjSVAT, adj.AdjdTranPeriodID);


					APRegister adjdoc = isPayment ? adjgdoc : adjddoc;
					APRegister cachedDoc = (APRegister)APDocument.Cache.Locate(adjdoc);
					if (cachedDoc != null)
					{
						adjdoc = cachedDoc;
					}

					if (adjdoc.CuryDocBal == 0m)
					{
						bool isPartialApplication = percent != 1m;

						adjSVAT.CuryTaxableAmt = docSVAT.CuryTaxableAmt;
						adjSVAT.TaxableAmt = docSVAT.TaxableAmt;
						adjSVAT.CuryTaxAmt = docSVAT.CuryTaxAmt;
						adjSVAT.TaxAmt = docSVAT.TaxAmt;

						if (isPartialApplication)
						{
							var rows = PXSelect<SVATConversionHist, Where<
								SVATConversionHist.module, Equal<BatchModule.moduleAP>,
								And<SVATConversionHist.adjdDocType, Equal<Current<SVATConversionHist.adjdDocType>>,
								And<SVATConversionHist.adjdRefNbr, Equal<Current<SVATConversionHist.adjdRefNbr>>,
								And<SVATConversionHist.taxID, Equal<Current<SVATConversionHist.taxID>>,
								And<Where<SVATConversionHist.adjdDocType, NotEqual<SVATConversionHist.adjgDocType>,
									Or<SVATConversionHist.adjdRefNbr, NotEqual<SVATConversionHist.adjgRefNbr>>>>>>>>>
								.SelectMultiBound(this, new object[] { docSVAT }).AsEnumerable();
							if (rows.Any())
							{
								foreach (SVATConversionHist row in rows)
								{
									adjSVAT.CuryTaxableAmt -= (row.CuryTaxableAmt ?? 0m);
									adjSVAT.TaxableAmt -= (row.TaxableAmt ?? 0m);
									adjSVAT.CuryTaxAmt -= (row.CuryTaxAmt ?? 0m);
									adjSVAT.TaxAmt -= (row.TaxAmt ?? 0m);
								}
							}
						}

						adjSVAT.CuryUnrecognizedTaxAmt = adjSVAT.CuryTaxAmt;
						adjSVAT.UnrecognizedTaxAmt = adjSVAT.TaxAmt;
					}

					adjSVAT = (SVATConversionHist)SVATConversionHistory.Cache.Insert(adjSVAT);

					docSVAT.Processed = false;
					docSVAT.AdjgFinPeriodID = null;
				    docSVAT.AdjgTranPeriodID = null;
					PXTimeStampScope.PutPersisted(SVATConversionHistory.Cache, docSVAT, PXDatabase.SelectTimeStamp());
					SVATConversionHistory.Cache.Update(docSVAT);
				}
			}
		}

        private void SegregateBatch(JournalEntry je, int? branchID, string curyID, DateTime? docDate, string finPeriodID, string description, CurrencyInfo curyInfo)
		{
			JournalEntry.SegregateBatch(je, BatchModule.AP, branchID, curyID, docDate, finPeriodID, description, curyInfo.GetCM(), null);
		}

		protected virtual void PerformBasicReleaseChecks(APRegister document)
		{
			if (document == null) throw new ArgumentNullException(nameof(document));

			if (document.Hold == true)
			{
				throw new ReleaseException(Messages.Document_OnHold_CannotRelease);
			}

			if (document.Status == APDocStatus.PendingApproval || document.Status == APDocStatus.Rejected)
			{
				throw new ReleaseException(Messages.DocumentNotApproved);
			}

			if (document.IsMigratedRecord == true &&
				document.Released != true &&
				IsMigrationMode != true)
			{
				throw new ReleaseException(Messages.CannotReleaseMigratedDocumentInNormalMode);
			}

			if (document.IsMigratedRecord != true &&
				IsMigrationMode == true)
			{
				throw new ReleaseException(Messages.CannotReleaseNormalDocumentInMigrationMode);
			}

			if (document.RetainageApply == true && !PXAccess.FeatureInstalled<FeaturesSet.retainage>())
			{
				throw new ReleaseException(GL.Messages.CannotReleaseRetainageDocumentIfFeatureOff);
			}

			Account acc = AccountAttribute.GetAccount(this, document.APAccountID);
			if (acc.IsCashAccount.GetValueOrDefault() == true)
			{
				throw new ReleaseException(GL.Messages.NotValidAccount, GL.Messages.ModuleAP);
			}
		}

		public virtual APRegister OnBeforeRelease(APRegister apdoc)
		{
			//TODO: This block should be removed after partially deductible taxes' support.
			if (apdoc.DocType == APDocType.Invoice)
			{
				foreach (PXResult<APTaxTran, Tax> res in
						PXSelectJoin<APTaxTran, LeftJoin<Tax, On<Tax.taxID, Equal<APTaxTran.taxID>>>,
						Where<APTaxTran.module, Equal<BatchModule.moduleAP>,
						And<APTaxTran.tranType, Equal<Required<APInvoice.docType>>,
							And<APTaxTran.refNbr, Equal<Required<APInvoice.refNbr>>>>>>.Select(this, apdoc.DocType, apdoc.RefNbr))
				{
					Tax tax = res;
					APTaxTran apTaxTran = res;
					if (tax.DeductibleVAT == true && tax.TaxApplyTermsDisc == CSTaxTermsDiscount.ToPromtPayment)
					{
						throw new PXException(Messages.DeductiblePPDTaxProhibitedForReleasing);
					}
				}
			}
			RefillAPPrintCheckDetail(apdoc);

			return apdoc;
		}

		protected virtual void RefillAPPrintCheckDetail(APRegister apdoc)
		{
			APPayment payment = apdoc as APPayment;
			if (payment == null)
				return;

			if (payment.DocType.IsNotIn(APDocType.Check, APDocType.Prepayment, APDocType.DebitAdj, APDocType.Refund, APDocType.VoidRefund, APDocType.VoidCheck)
				|| string.IsNullOrEmpty(payment.ExtRefNbr))
			{
				return;
			}

			APPaymentEntry pe = PXGraph.CreateInstance<APPaymentEntry>();
			pe.SelectTimeStamp();
			pe.RefillAPPrintCheckDetail(payment.RefNbr, payment.DocType);
			pe.Save.Press();
		}

		private class APDocTypePeriodSorting : IComparer<Tuple<string, string>>
		{
			public int Compare(Tuple<string, string> x, Tuple<string, string> y)
			{
				int result = Math.Sign((short)APDocType.SortOrder(y.Item1) - (short)APDocType.SortOrder(x.Item1));
				if (result == 0)
				{
					result = string.CompareOrdinal(x.Item2, y.Item2);
				}
				return result;
			}
		}

		/// <summary>
		/// Common entry point.
		/// The method to release both types of documents - invoices and payments.
		/// </summary>
		public virtual List<APRegister> ReleaseDocProc(JournalEntry je, APRegister doc, bool isPrebooking, out List<INRegister> inDocs)
		{
			List<APRegister> ret = null;
			inDocs = null;

			if (isPrebooking)
			{
				foreach (APTran tran in APTran_TranType_RefNbr.Select(doc.DocType, doc.RefNbr))
				{
					if (tran.PONbr != null)
					{
						throw new PXException(Messages.PrebookingIsNotAllowedForPO);
					}
				}
			}

			PerformBasicReleaseChecks(doc);

			if (doc.DocType.IsIn(APDocType.Invoice, APDocType.CreditAdj, APDocType.DebitAdj) && doc.OrigDocAmt < 0)
			{
				throw new PXException(Messages.DocAmtMustBeGreaterZero);
			}

			//TODO: This block should be removed after partially deductible taxes' support.
			if (doc.DocType == APDocType.Invoice)
			{
				var select = PXSelectJoin<Tax,
					InnerJoin<APTaxTran, On<Tax.taxID, Equal<APTaxTran.taxID>>>,
						Where<APTaxTran.module, Equal<BatchModule.moduleAP>,
							And<Tax.deductibleVAT, Equal<True>,
							And<Tax.taxApplyTermsDisc, Equal<CSTaxTermsDiscount.toPromtPayment>,
							And<APTaxTran.tranType, Equal<Required<APInvoice.docType>>,
							And<APTaxTran.refNbr, Equal<Required<APInvoice.refNbr>>>>>>>>
							.Select(this, doc.DocType, doc.RefNbr);
				if (select.Any())
				{
					throw new PXException(Messages.DeductiblePPDTaxProhibitedForReleasing);
				}
			}

			// Finding some known data inconsistency problems,
			// if any, the process will be stopped.
			//
			if (_IsIntegrityCheck != true)
			{
				bool? isReleasedOrPrebooked = doc.Prebooked | doc.Released;

				new DataIntegrityValidator<APRegister>(
					je, APDocument.Cache, doc, BatchModule.AP, doc.VendorID, isReleasedOrPrebooked, apsetup.DataInconsistencyHandlingMode)
					.CheckTransactionsExistenceForUnreleasedDocument()
					.Commit();
			}

			if (IsMigrationMode == true)
			{
				je.SetOffline();
			}

			using (PXTransactionScope ts = new PXTransactionScope())
			{
				//mark as updated so that doc will not expire from cache and update with Released = 1 will not override balances/amount in document
				APDocument.Cache.SetStatus(doc, PXEntryStatus.Updated);

				foreach (PXResult<APInvoice, CurrencyInfo, Terms, Vendor> res in APInvoice_DocType_RefNbr.Select((object)doc.DocType, doc.RefNbr))
				{
					Vendor v = res;
					switch (v.VStatus)
					{
						case VendorStatus.Inactive:
						case VendorStatus.Hold:
							throw new PXSetPropertyException(Messages.VendorIsInStatus, new VendorStatus.ListAttribute().ValueLabelDic[v.VStatus]);
					}

					//must check for AD application in different period
					if ((bool)doc.Released == false)
					{
						SegregateBatch(je, doc.BranchID, doc.CuryID, doc.DocDate, doc.FinPeriodID, doc.DocDesc, (CurrencyInfo)res);
					}

					ret = ReleaseInvoice(je, ref doc, res, isPrebooking, out inDocs);
					//ensure correct PXDBDefault behaviour on APTran persisting
					APInvoice_DocType_RefNbr.Current = (APInvoice)res;
				}
				Amount docBal = new Amount();
				foreach (PXResult<APPayment, CurrencyInfo, Currency, Vendor, CashAccount> res in APPayment_DocType_RefNbr.Select(doc.DocType, doc.RefNbr))
				{
					APPayment payment = res;
					CurrencyInfo info = res;
					Currency paycury = res;
					Vendor vendor = res;
					CashAccount cashacct = res;

					APPayment_DocType_RefNbr.Current = payment;

					Tuple<APAdjust, CurrencyInfo> lastAdjustment = new Tuple<APAdjust, CurrencyInfo>(new APAdjust(), new CurrencyInfo());

					switch (vendor.VStatus)
					{
						case VendorStatus.Inactive:
						case VendorStatus.Hold:
						case VendorStatus.HoldPayments:
							throw new PXSetPropertyException(Messages.VendorIsInStatus, new VendorStatus.ListAttribute().ValueLabelDic[vendor.VStatus]);
					}

					if (doc.Prebooked == true && doc.Released != true &&
						(doc.DocType == APDocType.QuickCheck || doc.DocType == APDocType.VoidQuickCheck || doc.DocType == APDocType.DebitAdj))
					{
						// We don't need payment part processing on release;
						//
						continue;
					}

					CM.CurrencyInfo last_info = null;

					if (doc.Released != true &&
						(doc.DocType == APDocType.Check || doc.DocType == APDocType.VoidCheck || doc.DocType == APDocType.Prepayment))
					{
						SegregateBatch(je, doc.BranchID, doc.CuryID, payment.DocDate, payment.FinPeriodID, doc.DocDesc, info);

						// We should use the same CurrencyInfo for Payment
						// and its applications to save proper consolidation
						// for generated GL transactions
						//
						last_info = GetCurrencyInfoCopyForGL(je, info);
						ProcessPayment(je, doc, new PXResult<APPayment, CurrencyInfo, Currency, Vendor, CashAccount>(payment, CurrencyInfo.GetEX(last_info), paycury, vendor, cashacct));

						if (je.GLTranModuleBatNbr.Cache.IsInsertedUpdatedDeleted)
						{
							je.Save.Press();
						}

						if (!je.BatchModule.Cache.IsDirty && string.IsNullOrEmpty(doc.BatchNbr))
						{
							doc.BatchNbr = je.BatchModule.Current.BatchNbr;
							foreach (APTranPost post in
								TranPost.Cache.Inserted.Cast<APTranPost>().Where(d => 
									d.TranType == doc.DocType && d.TranRefNbr == doc.RefNbr && d.BatchNbr == null))
								post.BatchNbr = doc.BatchNbr;
						}

						IComparer<Tuple<string, string>> comparer = new APDocTypePeriodSorting();
						SortedDictionary<Tuple<string, string>, List<PXResult<APAdjust>>> appsByDocTypeAndPeriod = new SortedDictionary<Tuple<string, string>, List<PXResult<APAdjust>>>(comparer);
						SortedDictionary<Tuple<string, string>, DateTime?> datesByDocTypeAndPeriod = new SortedDictionary<Tuple<string, string>, DateTime?>(comparer);

						InsertCurrencyInfoIntoCache(doc, info);

						foreach (PXResult<APAdjust> adjres in APAdjust_AdjgDocType_RefNbr_VendorID.Select(doc.DocType, doc.RefNbr, _IsIntegrityCheck, doc.AdjCntr))
						{
							APAdjust adj = adjres;
							SetAdjgPeriodsFromLatestApplication(doc, adj);

							List<PXResult<APAdjust>> apps;
							var appsKey = new Tuple<string, string>(adj.AdjdDocType, adj.AdjgTranPeriodID);

							if (!appsByDocTypeAndPeriod.TryGetValue(appsKey, out apps))
							{
								appsByDocTypeAndPeriod[appsKey] = apps = new List<PXResult<APAdjust>>();
							}
							apps.Add(adjres);

							DateTime? maxdate;
							if (!datesByDocTypeAndPeriod.TryGetValue(appsKey, out maxdate))
							{
								datesByDocTypeAndPeriod[appsKey] = maxdate = adj.AdjgDocDate;
							}

							if (DateTime.Compare((DateTime)adj.AdjgDocDate, (DateTime)maxdate) > 0)
							{
								datesByDocTypeAndPeriod[appsKey] = adj.AdjgDocDate;
							}

							if (doc.OpenDoc == false &&
								doc.DocType == APDocType.VoidCheck)
							{
								doc.OpenDoc = true;
								doc.CuryDocBal = doc.CuryOrigDocAmt;
								doc.DocBal = doc.OrigDocAmt;
							}
						}

						Batch paymentBatch = je.BatchModule.Current;

						foreach (KeyValuePair<Tuple<string, string>, List<PXResult<APAdjust>>> pair in appsByDocTypeAndPeriod)
						{
							Tuple<string, string> appsKey = pair.Key;

							FinPeriod postPeriod = FinPeriodRepository.GetFinPeriodByMasterPeriodID(PXAccess.GetParentOrganizationID(doc.BranchID), appsKey.Item2).GetValueOrRaiseError();

							JournalEntry.SegregateBatch(je, BatchModule.AP, doc.BranchID, doc.CuryID, datesByDocTypeAndPeriod[appsKey], postPeriod.FinPeriodID, doc.DocDesc, info.GetCM(), paymentBatch);

							var adjustments = new PXResultset<APAdjust>();
							adjustments.AddRange(pair.Value);

							last_info = GetCurrencyInfoCopyForGL(je, info);
							GetExtension<MultiCurrency>().currencyinfo.Insert(info); //was required for BankTran Maint
							lastAdjustment = ProcessAdjustments(je, adjustments, doc, payment, vendor, last_info, paycury);
						}
					}
					else
					{
						if (doc.DocType != APDocType.QuickCheck && doc.DocType != APDocType.VoidQuickCheck)
						{
							SegregateBatch(je, doc.BranchID, doc.CuryID, payment.AdjDate, payment.AdjFinPeriodID, doc.DocDesc, info);
						}

						// We should use the same CurrencyInfo for Payment
						// and its applications to save proper consolidation
						// for generated GL transactions.
						//
						last_info = GetCurrencyInfoCopyForGL(je, info);

						ProcessPayment(je, doc, new PXResult<APPayment, CurrencyInfo, Currency, Vendor, CashAccount>(payment, CurrencyInfo.GetEX(last_info), paycury, vendor, cashacct));

						PXResultset<APAdjust> adjustments = APAdjust_AdjgDocType_RefNbr_VendorID.Select(doc.DocType, doc.RefNbr, _IsIntegrityCheck, doc.AdjCntr);
						lastAdjustment = ProcessAdjustments(je, adjustments, doc, payment, vendor, last_info, paycury);
					}
					docBal = new Amount(doc.CuryDocBal, doc.DocBal);
					if (doc.DocType == APDocType.VoidCheck && docBal.Base != 0)
					{
						if (doc.IsMigratedRecord == true)
							docBal = new Amount(doc.CuryInitDocBal, doc.InitDocBal);
						ProcessVoidPaymentTranPost(doc, docBal);
					}
					
					VerifyPaymentRoundAndClose(je, doc, payment, vendor, CurrencyInfo.GetEX(last_info), paycury, lastAdjustment);
					doc.AdjCntr++;
					

					// Ensure correct PXDBDefault behaviour on APAdjust persisting
					//
					APPayment_DocType_RefNbr.Current = payment;
				}

				if (_IsIntegrityCheck == false)
				{
					if (je.GLTranModuleBatNbr.Cache.IsInsertedUpdatedDeleted)
					{
						je.Save.Press();
					}

					// Leave BatchNbr empty for Prepayment Requests
					//
					if (!je.BatchModule.Cache.IsDirty &&
						string.IsNullOrEmpty(doc.BatchNbr) &&
						(APInvoice_DocType_RefNbr.Current == null || APInvoice_DocType_RefNbr.Current.DocType != APDocType.Prepayment))
					{
						if (!isPrebooking)
						{
							doc.BatchNbr = je.BatchModule.Current?.BatchNbr;
							foreach (APTranPost post in
								TranPost.Cache.Inserted.Cast<APTranPost>()
									.Where(d =>d.TranType == doc.DocType && d.TranRefNbr == doc.RefNbr && d.BatchNbr == null))
								post.BatchNbr = doc.BatchNbr;

							if (doc.DocType == APDocType.VoidQuickCheck)
							{
								// Void Quick check is not prebooked by itself, but may contain
								// a reference on the prebook batch of the original Quick Check.
								//
								doc.PrebookBatchNbr = null;
							}
						}
						else
						{
							doc.PrebookBatchNbr = je.BatchModule.Current?.BatchNbr;
						}
					}
				}

				#region Auto Commit/Post document to external tax provider.

				APInvoice apDoc = doc as APInvoice;
				if (apDoc != null)
					{
					apDoc = CommitExternalTax(apDoc);

					doc.IsTaxPosted = apDoc.IsTaxPosted == true;
				}
				#endregion

				#region Setting Document Release Flag
				bool alreadyReleased = doc.Released.Value;

				if (doc.DocType == APDocType.QuickCheck && isPrebooking)
				{
					//For a Quick Check the Released flag is set in the middle of Pre-Release process, so we clear it manually here
					alreadyReleased = false;
				}

				bool isPrebookingAllowed = !alreadyReleased && APDocType.IsPrebookingAllowedForType(doc.DocType);

				if (isPrebookingAllowed && isPrebooking)
				{
					doc.Released = false;
					doc.Prebooked = true;
				}
				else
				{
					doc.Released = true;
				}

				doc = (APRegister)APDocument.Cache.Update(doc);


				#endregion

				//Apply automation.
				if (doc.Released == true)
				{
					RaiseReleaseEvent(doc);
				}

				PXTimeStampScope.DuplicatePersisted(APDocument.Cache, doc, typeof(APInvoice));
				PXTimeStampScope.DuplicatePersisted(APDocument.Cache, doc, typeof(APPayment));

				if (doc.DocType == APDocType.DebitAdj)
				{
					if (alreadyReleased)
					{
						APPayment_DocType_RefNbr.Cache.SetStatus(APPayment_DocType_RefNbr.Current, PXEntryStatus.Notchanged);
					}
					else
					{
						APPayment debitadj = (APPayment)APPayment_DocType_RefNbr.Cache.Extend<APRegister>(doc);
						debitadj.AdjTranPeriodID = null;
						debitadj.AdjFinPeriodID = null;
						APPayment_DocType_RefNbr.Cache.Update(debitadj);

						debitadj.CreatedByID = doc.CreatedByID;
						debitadj.CreatedByScreenID = doc.CreatedByScreenID;
						debitadj.CreatedDateTime = doc.CreatedDateTime;
						debitadj.CashAccountID = null;
						debitadj.PaymentMethodID = null;
						debitadj.DepositAsBatch = false;
						debitadj.ExtRefNbr = null;
						debitadj.AdjDate = debitadj.DocDate;
						debitadj.AdjFinPeriodID = debitadj.FinPeriodID;
						debitadj.AdjTranPeriodID = debitadj.TranPeriodID;
						debitadj.Printed = true;
						APAddressAttribute.DefaultRecord<APPayment.remitAddressID>(APPayment_DocType_RefNbr.Cache, debitadj);
						APContactAttribute.DefaultRecord<APPayment.remitContactID>(APPayment_DocType_RefNbr.Cache, debitadj);
						OpenPeriodAttribute.SetValidatePeriod<APPayment.adjFinPeriodID>(APPayment_DocType_RefNbr.Cache, debitadj, PeriodValidation.DefaultSelectUpdate);
						APPayment_DocType_RefNbr.Cache.Update(debitadj);
						PXTimeStampScope.DuplicatePersisted(APPayment_DocType_RefNbr.Cache, debitadj, typeof(APInvoice));
						APDocument.Cache.SetStatus(doc, PXEntryStatus.Notchanged);
					}
				}
				else
				{
					if (APDocument.Cache.ObjectsEqual(doc, APPayment_DocType_RefNbr.Current))
					{
						APPayment_DocType_RefNbr.Cache.SetStatus(APPayment_DocType_RefNbr.Current, PXEntryStatus.Notchanged);
					}
				}


				this.Actions.PressSave();

				// Finding some known data inconsistency problems,
				// if any, the process will be stopped.
				//
				if (_IsIntegrityCheck != true)
				{
					EntityInUseHelper.MarkEntityAsInUse<CurrencyInUse>(doc.CuryID);

					bool? isReleasedOrPrebooked = (isPrebookingAllowed && isPrebooking) ? doc.Prebooked : doc.Released;

					// We need this condition to prevent applications verification,
					// until the APPayment part will not be created.
					bool isUnreleasedDebitAdj = doc.DocType == APDocType.DebitAdj && !alreadyReleased;

					// GLBatch will not be created for the Prepayment request,
					// so we should disable such validation for it
					bool isPrepaymentRequest = APInvoice_DocType_RefNbr.Current?.DocType == APDocType.Prepayment;

					new DataIntegrityValidator<APRegister>(
						je, APDocument.Cache, doc, BatchModule.AP, doc.VendorID, isReleasedOrPrebooked, apsetup.DataInconsistencyHandlingMode)
						.CheckTransactionsExistenceForUnreleasedDocument()
						.CheckTransactionsExistenceForReleasedDocument(disableCheck: isPrepaymentRequest || doc.IsMigratedRecord == true)
						.CheckBatchAndTransactionsSumsForDocument()
						.CheckApplicationsReleasedForDocument<APAdjust, APAdjust.adjgDocType, APAdjust.adjgRefNbr, APAdjust.released>(disableCheck: isUnreleasedDebitAdj)
						.CheckDocumentHasNonNegativeBalance()
						.CheckDocumentTotalsConformToCurrencyPrecision()
						.Commit();
				}

				ts.Complete(this);
			}

			return ret;
		}

	    public virtual APInvoice CommitExternalTax(APInvoice doc)
	    {
	        return doc;
	    }

		/// <summary>
		/// Workaround for AC-167924. To prevent selection of outdated currencyinfo record from DB:
		/// 1. When we generate ap doc through the voucher from, we create new currencyinfo in the voucher graph.
		/// 2. We are persisting changes but they are not committed in the db
		/// 3. When we are in the ap release graph, we select the currencyinfo from db and get outdated commited one.
		/// 4. This workaround is that to put the currencyinfo to the cache to avoid quieting the db
		/// </summary>
		/// <param name="doc"></param>
		/// <param name="info"></param>
		protected virtual void InsertCurrencyInfoIntoCache(APRegister doc, CurrencyInfo info)
		{
			if (doc.OrigModule == BatchModule.GL)
			{
				this.CurrencyInfo_CuryInfoID.Insert(info);
			}
		}

	    protected virtual void RaiseInvoiceEvent(APRegister doc, PX.Data.WorkflowAPI.SelectedEntityEvent<APInvoice> invEvent)
	    {
		    if (doc is APInvoice)
		    {
			    invEvent.FireOn(this, (APInvoice) doc);
			    APDocument.Cache.Update(doc);
			    APDocument.Cache.RestoreCopy(doc, APDocument.Cache.Locate(doc));
		    }
	    }
	    protected virtual void RaisePaymentEvent(APRegister doc, PX.Data.WorkflowAPI.SelectedEntityEvent<APPayment> pntEvent)
	    {
		    if (doc is APPayment)
		    {
			    pntEvent.FireOn(this, (APPayment)doc);
			    APDocument.Cache.Update(doc);
			    APDocument.Cache.RestoreCopy(doc, APDocument.Cache.Locate(doc));
		    }
	    }

		protected virtual void RaiseReleaseEvent(APRegister doc)
	    {
		    if (APDocument.Cache.ObjectsEqual(doc, APInvoice_DocType_RefNbr.Current))
		    {
			    APInvoice invoice = PXCache<APInvoice>.CreateCopy(APInvoice_DocType_RefNbr.Current);
			    APDocument.Cache.RestoreCopy(invoice, doc);
			    APDocument.Cache.Remove(doc);
			    APInvoice.Events
				    .Select(e => e.ReleaseDocument)
				    .FireOn(this, invoice);
			    if (APDocument.Cache.GetStatus(invoice) != PXEntryStatus.Updated)
				    APDocument.Cache.SetStatus(invoice, PXEntryStatus.Updated);
			    APDocument.Cache.RestoreCopy(doc, APDocument.Cache.Locate(doc));
		    }
		    else if (APDocument.Cache.ObjectsEqual(doc, APPayment_DocType_RefNbr.Current))
		    {
			    APPayment payment = PXCache<APPayment>.CreateCopy(APPayment_DocType_RefNbr.Current);
			    APDocument.Cache.RestoreCopy(payment, doc);
			    APDocument.Cache.Remove(doc);
			    APPayment.Events
				    .Select(e => e.ReleaseDocument)
				    .FireOn(this, payment);
			    if (APDocument.Cache.GetStatus(payment) != PXEntryStatus.Updated)
				    APDocument.Cache.SetStatus(payment, PXEntryStatus.Updated);

			    APDocument.Cache.RestoreCopy(doc, APDocument.Cache.Locate(doc));
		    }
		}

		private void SetClosedPeriodsFromLatestApplication(APRegister doc)
		{
			APTranPost lastPeriod = PXSelect<APTranPost,
					Where<APTranPost.docType, Equal<Required<APTranPost.docType>>,
						And<APTranPost.refNbr, Equal<Required<APTranPost.refNbr>>>>,
					OrderBy<Desc<APTranPost.tranPeriodID,
						Desc<APTranPost.iD>>>>
				.SelectSingleBound(this, new object[]{}, doc.DocType, doc.RefNbr);

			APTranPost lastDate = PXSelect<APTranPost,
					Where<APTranPost.docType, Equal<Required<APTranPost.docType>>,
						And<APTranPost.refNbr, Equal<Required<APTranPost.refNbr>>>>,
					OrderBy<Desc<APTranPost.docDate,
						Desc<APTranPost.iD>>>>
				.SelectSingleBound(this, new object[]{}, doc.DocType, doc.RefNbr);
			doc.ClosedTranPeriodID = GL.FinPeriods.FinPeriodUtils.Max(lastPeriod?.TranPeriodID , doc.TranPeriodID);
			FinPeriodIDAttribute.SetPeriodsByMaster<APRegister.closedFinPeriodID>(
				APDocument.Cache,
				doc,
				doc.ClosedTranPeriodID);
			
			doc.ClosedDate = GL.FinPeriods.FinPeriodUtils.Max(lastDate?.DocDate,doc.DocDate);
		}

		private void SetAdjgPeriodsFromLatestApplication(APRegister doc, APAdjust adj)
		{
			if (adj.VoidAppl == true)
			{
				// We should collect original applications to find max periods and dates,
				// because in some cases their values can be greater than values from voiding application
				//
				foreach (string adjgDocType in doc.PossibleOriginalDocumentTypes())
				{
					APAdjust orig = PXSelect<APAdjust,
						Where<APAdjust.adjdDocType, Equal<Required<APAdjust.adjdDocType>>,
							And<APAdjust.adjdRefNbr, Equal<Required<APAdjust.adjdRefNbr>>,
							And<APAdjust.adjgDocType, Equal<Required<APAdjust.adjgDocType>>,
						And<APAdjust.adjgRefNbr, Equal<Required<APAdjust.adjgRefNbr>>,
							And<APAdjust.adjNbr, Equal<Required<APAdjust.voidAdjNbr>>,
							And<APAdjust.released, Equal<True>>>>>>>>
						.SelectSingleBound(this, null, adj.AdjdDocType, adj.AdjdRefNbr, adjgDocType, adj.AdjgRefNbr, adj.VoidAdjNbr);
					if (orig != null)
				{
						FinPeriodIDAttribute.SetPeriodsByMaster<APAdjust.adjgFinPeriodID>(
                            APAdjust_AdjgDocType_RefNbr_VendorID.Cache, adj,
						    GL.FinPeriods.FinPeriodUtils.Max(orig.AdjgTranPeriodID, adj.AdjgTranPeriodID));

						adj.AdjgDocDate = GL.FinPeriods.FinPeriodUtils.Max((DateTime)orig.AdjgDocDate, (DateTime)adj.AdjgDocDate);

						break;
					}
				}
			}

			FinPeriodIDAttribute.SetPeriodsByMaster<APAdjust.adjgFinPeriodID>(
			    APAdjust_AdjgDocType_RefNbr_VendorID.Cache, adj,
			    GL.FinPeriods.FinPeriodUtils.Max(adj.AdjdTranPeriodID, adj.AdjgTranPeriodID));

			adj.AdjgDocDate = GL.FinPeriods.FinPeriodUtils.Max((DateTime)adj.AdjdDocDate, (DateTime)adj.AdjgDocDate);
		}

		private void ClosePayment(APRegister doc, APPayment apdoc, Vendor vendor)
		{
			if (apdoc.VoidAppl == true || doc.CuryDocBal == 0m)
			{
				doc.CuryDocBal = 0m;
				doc.DocBal = 0m;
				doc.OpenDoc = false;

				SetClosedPeriodsFromLatestApplication(doc);

				if (apdoc.VoidAppl == true || apdoc.DocType == APDocType.VoidQuickCheck)
				{
					UpdateVoidedCheck(doc);
				}
				else
				{
					RaisePaymentEvent(doc, APPayment.Events.Select(ev=>ev.CloseDocument));
					RaiseInvoiceEvent(doc, APInvoice.Events.Select(ev=>ev.CloseDocument));
				}

				if (apdoc.VoidAppl != true)
				{
					DeactivateOneTimeVendorIfAllDocsIsClosed(vendor);
				}
			}
			else if (apdoc.VoidAppl == false)
			{
				// Do not reset ClosedPeriod for VoidCheck.
				doc.OpenDoc = true;
				doc.ClosedDate = null;
				doc.ClosedFinPeriodID = null;
				doc.ClosedTranPeriodID = null;
				RaisePaymentEvent(doc, APPayment.Events.Select(ev=>ev.OpenDocument));	
				RaiseInvoiceEvent(doc, APInvoice.Events.Select(ev=>ev.OpenDocument));
			}
		}

		public virtual void ExtensionsPersist()
		{
			// Extension point used in customizations.
		}

		public virtual void ExtensionsPersisted()
		{
			// Extension point used in customizations.
		}

        public override void Persist()
        {
            using (PXTransactionScope ts = new PXTransactionScope())
            {
                APPayment_DocType_RefNbr.Cache.Persist(PXDBOperation.Insert);
                APPayment_DocType_RefNbr.Cache.Persist(PXDBOperation.Update);

                APDocument.Cache.Persist(PXDBOperation.Update);

                APTran_TranType_RefNbr.Cache.Persist(PXDBOperation.Update);

				APPaymentChargeTran_DocType_RefNbr.Cache.Persist(PXDBOperation.Update);

				APTaxTran_TranType_RefNbr.Cache.Persist(PXDBOperation.Insert);
                APTaxTran_TranType_RefNbr.Cache.Persist(PXDBOperation.Update);
				SVATConversionHistory.Cache.Persist(PXDBOperation.Insert);
				SVATConversionHistory.Cache.Persist(PXDBOperation.Update);

                APAdjust_AdjgDocType_RefNbr_VendorID.Cache.Persist(PXDBOperation.Insert);
                APAdjust_AdjgDocType_RefNbr_VendorID.Cache.Persist(PXDBOperation.Update);
                APAdjust_AdjgDocType_RefNbr_VendorID.Cache.Persist(PXDBOperation.Delete);

                Caches[typeof(APHist)].Persist(PXDBOperation.Insert);
                Caches[typeof(CuryAPHist)].Persist(PXDBOperation.Insert);
                Caches[typeof(APTranPost)].Persist(PXDBOperation.Insert);

                AP1099Year_Select.Cache.Persist(PXDBOperation.Insert);
                AP1099History_Select.Cache.Persist(PXDBOperation.Insert);

                CurrencyInfo_CuryInfoID.Cache.Persist(PXDBOperation.Update);

				this.Caches[typeof(CADailySummary)].Persist(PXDBOperation.Insert);
				this.Caches[typeof(PMCommitment)].Persist(PXDBOperation.Insert);
				this.Caches[typeof(PMCommitment)].Persist(PXDBOperation.Update);
				this.Caches[typeof(PMCommitment)].Persist(PXDBOperation.Delete);
				this.Caches[typeof(PMHistoryAccum)].Persist(PXDBOperation.Insert);
				this.Caches[typeof(PMBudgetAccum)].Persist(PXDBOperation.Insert);
				this.Caches[typeof(PMForecastHistoryAccum)].Persist(PXDBOperation.Insert);

				Caches[typeof(APTax)].Persist(PXDBOperation.Update);

				ExtensionsPersist();

                ts.Complete(this);
            }

            APPayment_DocType_RefNbr.Cache.Persisted(false);
            APDocument.Cache.Persisted(false);
            APTran_TranType_RefNbr.Cache.Persisted(false);
            APTaxTran_TranType_RefNbr.Cache.Persisted(false);
            APAdjust_AdjgDocType_RefNbr_VendorID.Cache.Persisted(false);

            Caches[typeof(APHist)].Persisted(false);
            Caches[typeof(CuryAPHist)].Persisted(false);
            Caches[typeof(APTranPost)].Persisted(false);

            AP1099Year_Select.Cache.Persisted(false);
            AP1099History_Select.Cache.Persisted(false);

            CurrencyInfo_CuryInfoID.Cache.Persisted(false);

			this.Caches[typeof(CADailySummary)].Persisted(false);
			this.Caches[typeof(PMCommitment)].Persisted(false);
			this.Caches[typeof(PMHistoryAccum)].Persisted(false);
			this.Caches[typeof(PMBudgetAccum)].Persisted(false);
			this.Caches[typeof(PMForecastHistoryAccum)].Persisted(false);

			Caches[typeof(APTax)].Persisted(false);

			ExtensionsPersisted();
		}

		/// <summary>
		/// Is turned on when releasing of invoice runs secondary after reclassifying action. Need to create only new gl batch in this case.
		/// </summary>
        public bool IsInvoiceReclassification { get; set; }

        protected bool _IsIntegrityCheck = false;
		protected string _IntegrityCheckStartingPeriod = null;

		public bool IsIntegrityCheck => _IsIntegrityCheck;

		protected virtual int SortVendDocs(APRegister docA, APRegister docB)
		{
			int result = ((IComparable)(docA.SortOrder)).CompareTo(docB.SortOrder);
			if (result == 0)
			{
				//Compare prepayments.
				if (docA is APInvoice && docB is APPayment)
					return -1;
				if (docA is APPayment && docB is APInvoice)
					return 1;
			}

			if (docA.DocType == APDocType.Prepayment && docB.DocType == APDocType.Prepayment)
			{
				if (docA.LineCntr > 0 || docB.LineCntr > 0)
				{
					result = (docA.LineCntr > 0 && docB.LineCntr == 0 ) ? -1 : 1;
				}
			}

			// Sort order for the Retainage documents validation:
			// Original retainage Bill,
			// Child retainage Bill,
			// Child retainage DebitAdj,
			// Original retainage DebitAdj.
			//
			return
				result == 0
					? docA.DocType == APDocType.DebitAdj
					? ((IComparable)(docA.RetainageApply) ?? false).CompareTo(docB.RetainageApply ?? false)
						: ((IComparable)(docA.IsRetainageDocument) ?? false).CompareTo(docB.IsRetainageDocument ?? false)
					: result;
		}

		public virtual void IntegrityCheckProc(Vendor vend, string startPeriod)
		{
			_IsIntegrityCheck = true;
			_IntegrityCheckStartingPeriod = startPeriod;
			JournalEntry je = PXGraph.CreateInstance<JournalEntry>();
			je.SetOffline();
			DocumentList<Batch> created = new DocumentList<Batch>(je);

			Caches[typeof(Vendor)].Current = vend;

			using (new PXConnectionScope())
			{
				using (PXTransactionScope ts = new PXTransactionScope())
				{
					string minPeriod = "190001";

					APHistory maxHist = (APHistory)PXSelectGroupBy<
						APHistory,
						Where<APHistory.vendorID, Equal<Current<Vendor.bAccountID>>,
							And<APHistory.detDeleted, Equal<True>>>,
						Aggregate<
							Max<APHistory.finPeriodID>>>
						.Select(this);

					if (maxHist != null && maxHist.FinPeriodID != null)
					{
						minPeriod = FinPeriodRepository.GetOffsetPeriodId(maxHist.FinPeriodID, 1, FinPeriod.organizationID.MasterValue);
					}

					if (string.IsNullOrEmpty(startPeriod) == false && string.Compare(startPeriod, minPeriod) > 0)
					{
						minPeriod = startPeriod;
					}
					FinPeriod prevPeriod = FinPeriodRepository.FindPrevPeriod(FinPeriod.organizationID.MasterValue, minPeriod);

                    PXDatabase.Delete<AP1099History>(
						new PXDataFieldRestrict("VendorID", PXDbType.Int, 4, vend.BAccountID, PXComp.EQ),
						new PXDataFieldRestrict("FinYear", PXDbType.Char, 4, minPeriod.Substring(0, 4), PXComp.GE)
						);

					PXUpdateJoin<
							Set<APHistory.finBegBalance, IsNull<APHistory2.finYtdBalance, Zero>,
							Set<APHistory.finPtdPayments, Zero,
							Set<APHistory.finPtdPurchases, Zero,
							Set<APHistory.finPtdCrAdjustments, Zero,
							Set<APHistory.finPtdDrAdjustments, Zero,
							Set<APHistory.finPtdDiscTaken, Zero,
							Set<APHistory.finPtdWhTax, Zero,
							Set<APHistory.finPtdRGOL, Zero,
							Set<APHistory.finYtdBalance, IsNull<APHistory2.finYtdBalance, Zero>,
							Set<APHistory.finPtdDeposits, Zero,
							Set<APHistory.finYtdDeposits, IsNull<APHistory2.finYtdDeposits, Zero>,
							Set<APHistory.finYtdRetainageReleased, IsNull<APHistory2.finYtdRetainageReleased, Zero>,
							Set<APHistory.finPtdRetainageReleased, Zero,
							Set<APHistory.finYtdRetainageWithheld, IsNull<APHistory2.finYtdRetainageWithheld, Zero>,
							Set<APHistory.finPtdRetainageWithheld, Zero,
							Set<APHistory.finPtdRevalued, APHistory.finPtdRevalued>>>>>>>>>>>>>>>>,
						APHistory,
						LeftJoin<Branch,
							On<APHistory.branchID, Equal<Branch.branchID>>,
						LeftJoin<FinPeriod,
							On<APHistory.finPeriodID, Equal<FinPeriod.finPeriodID>,
							And<Branch.organizationID, Equal<FinPeriod.organizationID>>>,
						LeftJoin<OrganizationFinPeriodExt,
							  On<OrganizationFinPeriodExt.masterFinPeriodID, Equal<Required<OrganizationFinPeriodExt.masterFinPeriodID>>,
							  And<Branch.organizationID, Equal<OrganizationFinPeriodExt.organizationID>>>,
						LeftJoin<APHistory2ByPeriod,
							On<APHistory2ByPeriod.branchID, Equal<APHistory.branchID>,
							And<APHistory2ByPeriod.accountID, Equal<APHistory.accountID>,
							And<APHistory2ByPeriod.subID, Equal<APHistory.subID>,
							And<APHistory2ByPeriod.vendorID, Equal<APHistory.vendorID>,
							And<APHistory2ByPeriod.finPeriodID, Equal<OrganizationFinPeriodExt.prevFinPeriodID>>>>>>,
						LeftJoin<APHistory2,
							  On<APHistory2.branchID, Equal<APHistory.branchID>,
							 And<APHistory2.accountID, Equal<APHistory.accountID>,
							 And<APHistory2.subID, Equal<APHistory.subID>,
							 And<APHistory2.vendorID, Equal<APHistory.vendorID>,
							 And<APHistory2.finPeriodID, Equal<APHistory2ByPeriod.lastActivityPeriod>>>>>>>>>>>,
						Where<APHistory.vendorID, Equal<Required<APHist.vendorID>>,
							And<FinPeriod.masterFinPeriodID, GreaterEqual<Required<FinPeriod.masterFinPeriodID>>>>>
						.Update(this, minPeriod, vend.BAccountID, minPeriod);

					PXUpdateJoin<
							Set<APHistory.tranBegBalance, IsNull<APHistory2.tranYtdBalance, Zero>,
							Set<APHistory.tranPtdPayments, Zero,
							Set<APHistory.tranPtdPurchases, Zero,
							Set<APHistory.tranPtdCrAdjustments, Zero,
							Set<APHistory.tranPtdDrAdjustments, Zero,
							Set<APHistory.tranPtdDiscTaken, Zero,
							Set<APHistory.tranPtdWhTax, Zero,
							Set<APHistory.tranPtdRGOL, Zero,
							Set<APHistory.tranYtdBalance, IsNull<APHistory2.tranYtdBalance, Zero>,
							Set<APHistory.tranPtdDeposits, Zero,
							Set<APHistory.tranYtdDeposits, IsNull<APHistory2.tranYtdDeposits, Zero>,
							Set<APHistory.tranYtdRetainageReleased, IsNull<APHistory2.tranYtdRetainageReleased, Zero>,
							Set<APHistory.tranPtdRetainageReleased, Zero,
							Set<APHistory.tranYtdRetainageWithheld, IsNull<APHistory2.tranYtdRetainageWithheld, Zero>,
							Set<APHistory.tranPtdRetainageWithheld, Zero>>>>>>>>>>>>>>>,
						APHistory,
						LeftJoin<APHistory2ByPeriod,
							On<APHistory2ByPeriod.branchID, Equal<APHistory.branchID>,
							And<APHistory2ByPeriod.accountID, Equal<APHistory.accountID>,
							And<APHistory2ByPeriod.subID, Equal<APHistory.subID>,
							And<APHistory2ByPeriod.vendorID, Equal<APHistory.vendorID>,
							And<APHistory2ByPeriod.finPeriodID, Equal<Required<FinPeriod.masterFinPeriodID>>>>>>>,
						LeftJoin<APHistory2,
							On<APHistory2.branchID, Equal<APHistory.branchID>,
								And<APHistory2.accountID, Equal<APHistory.accountID>,
								And<APHistory2.subID, Equal<APHistory.subID>,
								And<APHistory2.vendorID, Equal<APHistory.vendorID>,
								And<APHistory2.finPeriodID, Equal<APHistory2ByPeriod.lastActivityPeriod>>>>>>>>,
						Where<APHistory.vendorID, Equal<Required<APHist.vendorID>>,
							And<APHistory.finPeriodID, GreaterEqual<Required<APHistory.finPeriodID>>>>>
						.Update(this, prevPeriod?.FinPeriodID, vend.BAccountID, minPeriod);

					PXUpdateJoin<
							Set<CuryAPHistory.curyFinBegBalance, IsNull<CuryAPHistory2.curyFinYtdBalance, Zero>,
							Set<CuryAPHistory.curyFinPtdCrAdjustments, Zero,
							Set<CuryAPHistory.curyFinPtdDeposits, Zero,
							Set<CuryAPHistory.curyFinPtdDiscTaken, Zero,
							Set<CuryAPHistory.curyFinPtdDrAdjustments, Zero,
							Set<CuryAPHistory.curyFinPtdPayments, Zero,
							Set<CuryAPHistory.curyFinPtdPurchases, Zero,
							Set<CuryAPHistory.curyFinPtdRetainageReleased, Zero,
							Set<CuryAPHistory.curyFinPtdRetainageWithheld, Zero,
							Set<CuryAPHistory.curyFinPtdWhTax, Zero,
							Set<CuryAPHistory.curyFinYtdBalance, IsNull<CuryAPHistory2.curyFinYtdBalance, Zero>,
							Set<CuryAPHistory.curyFinYtdDeposits, IsNull<CuryAPHistory2.curyFinYtdDeposits, Zero>,
							Set<CuryAPHistory.curyFinYtdRetainageReleased, IsNull<CuryAPHistory2.curyFinYtdRetainageReleased, Zero>,
							Set<CuryAPHistory.curyFinYtdRetainageWithheld, IsNull<CuryAPHistory2.curyFinYtdRetainageWithheld, Zero>,
							Set<CuryAPHistory.finBegBalance, IsNull<CuryAPHistory2.finYtdBalance, Zero>,
							Set<CuryAPHistory.finPtdCrAdjustments, Zero,
							Set<CuryAPHistory.finPtdDeposits, Zero,
							Set<CuryAPHistory.finPtdDiscTaken, Zero,
							Set<CuryAPHistory.finPtdDrAdjustments, Zero,
							Set<CuryAPHistory.finPtdPayments, Zero,
							Set<CuryAPHistory.finPtdPurchases, Zero,
							Set<CuryAPHistory.finPtdRetainageReleased, Zero,
							Set<CuryAPHistory.finPtdRetainageWithheld, Zero,
							Set<CuryAPHistory.finPtdRGOL, Zero,
							Set<CuryAPHistory.finPtdWhTax, Zero,
							Set<CuryAPHistory.finYtdBalance, IsNull<CuryAPHistory2.finYtdBalance, Zero>,
							Set<CuryAPHistory.finYtdDeposits, IsNull<CuryAPHistory2.finYtdDeposits, Zero>,
							Set<CuryAPHistory.finYtdRetainageReleased, IsNull<CuryAPHistory2.finYtdRetainageReleased, Zero>,
							Set<CuryAPHistory.finYtdRetainageWithheld, IsNull<CuryAPHistory2.finYtdRetainageWithheld, Zero>,
							Set<CuryAPHistory.finPtdRevalued, CuryAPHistory.finPtdRevalued
								>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>,
						CuryAPHistory,
						LeftJoin<Branch,
							On<CuryAPHistory.branchID, Equal<Branch.branchID>>,
						LeftJoin<FinPeriod,
							On<CuryAPHistory.finPeriodID, Equal<FinPeriod.finPeriodID>,
							And<Branch.organizationID, Equal<FinPeriod.organizationID>>>,
						LeftJoin<OrganizationFinPeriodExt,
							  On<OrganizationFinPeriodExt.masterFinPeriodID, Equal<Required<OrganizationFinPeriodExt.masterFinPeriodID>>,
							  And<Branch.organizationID, Equal<OrganizationFinPeriodExt.organizationID>>>,
						LeftJoin<APHistoryByPeriod,
							On<APHistoryByPeriod.branchID, Equal<CuryAPHistory.branchID>,
							And<APHistoryByPeriod.accountID, Equal<CuryAPHistory.accountID>,
							And<APHistoryByPeriod.subID, Equal<CuryAPHistory.subID>,
							And<APHistoryByPeriod.vendorID, Equal<CuryAPHistory.vendorID>,
							And<APHistoryByPeriod.curyID, Equal<CuryAPHistory.curyID>,
							And<APHistoryByPeriod.finPeriodID, Equal<OrganizationFinPeriodExt.prevFinPeriodID>>>>>>>,
						LeftJoin<CuryAPHistory2,
							On<CuryAPHistory2.branchID, Equal<CuryAPHistory.branchID>,
							And<CuryAPHistory2.accountID, Equal<CuryAPHistory.accountID>,
							And<CuryAPHistory2.subID, Equal<CuryAPHistory.subID>,
							And<CuryAPHistory2.vendorID, Equal<CuryAPHistory.vendorID>,
							And<CuryAPHistory2.curyID, Equal<CuryAPHistory.curyID>,
							And<CuryAPHistory2.finPeriodID, Equal<APHistoryByPeriod.lastActivityPeriod>>>>>>>>>>>>,
						Where<CuryAPHistory.vendorID, Equal<Required<CuryAPHist.vendorID>>,
						  And<FinPeriod.masterFinPeriodID, GreaterEqual<Required<FinPeriod.finPeriodID>>>>>
						.Update(this, minPeriod, vend.BAccountID, minPeriod);

					PXUpdateJoin<
							Set<CuryAPHistory.curyTranBegBalance, IsNull<CuryAPHistory2.curyTranYtdBalance, Zero>,
							Set<CuryAPHistory.curyTranPtdCrAdjustments, Zero,
							Set<CuryAPHistory.curyTranPtdDeposits, Zero,
							Set<CuryAPHistory.curyTranPtdDiscTaken, Zero,
							Set<CuryAPHistory.curyTranPtdDrAdjustments, Zero,
							Set<CuryAPHistory.curyTranPtdPayments, Zero,
							Set<CuryAPHistory.curyTranPtdPurchases, Zero,
							Set<CuryAPHistory.curyTranPtdRetainageReleased, Zero,
							Set<CuryAPHistory.curyTranPtdRetainageWithheld, Zero,
							Set<CuryAPHistory.curyTranPtdWhTax, Zero,
							Set<CuryAPHistory.curyTranYtdBalance, IsNull<CuryAPHistory2.curyTranYtdBalance, Zero>,
							Set<CuryAPHistory.curyTranYtdDeposits, IsNull<CuryAPHistory2.curyTranYtdDeposits, Zero>,
							Set<CuryAPHistory.curyTranYtdRetainageReleased, IsNull<CuryAPHistory2.curyTranYtdRetainageReleased, Zero>,
							Set<CuryAPHistory.curyTranYtdRetainageWithheld, IsNull<CuryAPHistory2.curyTranYtdRetainageWithheld, Zero>,
							Set<CuryAPHistory.tranBegBalance, IsNull<CuryAPHistory2.tranYtdBalance, Zero>,
							Set<CuryAPHistory.tranPtdCrAdjustments, Zero,
							Set<CuryAPHistory.tranPtdDeposits, Zero,
							Set<CuryAPHistory.tranPtdDiscTaken, Zero,
							Set<CuryAPHistory.tranPtdDrAdjustments, Zero,
							Set<CuryAPHistory.tranPtdPayments, Zero,
							Set<CuryAPHistory.tranPtdPurchases, Zero,
							Set<CuryAPHistory.tranPtdRetainageReleased, Zero,
							Set<CuryAPHistory.tranPtdRetainageWithheld, Zero,
							Set<CuryAPHistory.tranPtdRGOL, Zero,
							Set<CuryAPHistory.tranPtdWhTax, Zero,
							Set<CuryAPHistory.tranYtdBalance, IsNull<CuryAPHistory2.tranYtdBalance, Zero>,
							Set<CuryAPHistory.tranYtdDeposits, IsNull<CuryAPHistory2.tranYtdDeposits, Zero>,
							Set<CuryAPHistory.tranYtdRetainageReleased, IsNull<CuryAPHistory2.tranYtdRetainageReleased, Zero>,
							Set<CuryAPHistory.tranYtdRetainageWithheld, IsNull<CuryAPHistory2.tranYtdRetainageWithheld, Zero>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>,
						CuryAPHistory,
						LeftJoin<APHistoryByPeriod,
							On<APHistoryByPeriod.branchID, Equal<CuryAPHistory.branchID>,
							And<APHistoryByPeriod.accountID, Equal<CuryAPHistory.accountID>,
							And<APHistoryByPeriod.subID, Equal<CuryAPHistory.subID>,
							And<APHistoryByPeriod.vendorID, Equal<CuryAPHistory.vendorID>,
							And<APHistoryByPeriod.curyID, Equal<CuryAPHistory.curyID>,
							And<APHistoryByPeriod.finPeriodID, Equal<Required<CuryAPHistory.finPeriodID>>>>>>>>,
						LeftJoin<CuryAPHistory2,
							On<CuryAPHistory2.branchID, Equal<CuryAPHistory.branchID>,
								And<CuryAPHistory2.accountID, Equal<CuryAPHistory.accountID>,
								And<CuryAPHistory2.subID, Equal<CuryAPHistory.subID>,
								And<CuryAPHistory2.vendorID, Equal<CuryAPHistory.vendorID>,
								And<CuryAPHistory2.curyID, Equal<CuryAPHistory.curyID>,
								And<CuryAPHistory2.finPeriodID, Equal<APHistoryByPeriod.lastActivityPeriod>>>>>>>>>,
						Where<CuryAPHistory.vendorID, Equal<Required<CuryAPHist.vendorID>>,
							And<CuryAPHistory.finPeriodID, GreaterEqual<Required<CuryAPHistory.finPeriodID>>>>>
						.Update(this, prevPeriod?.FinPeriodID, vend.BAccountID, minPeriod);

					PXDatabase.Delete<APTranPost>(
						new PXDataFieldRestrict<APTranPost.vendorID>(PXDbType.Int, 4, vend.BAccountID, PXComp.EQ),
						new PXDataFieldRestrict<APTranPost.finPeriodID>(PXDbType.VarChar, minPeriod.Length, minPeriod, PXComp.GE)
					);

					HashedList<APRegister> venddocs = GetDocumentsForIntegrityCheckProc(minPeriod);
					venddocs.Sort(SortVendDocs);
					APDocument.Cache.Clear();

					foreach (APRegister venddoc in venddocs)
					{
						je.Clear();

						APRegister doc = venddoc;

						//mark as updated so that doc will not expire from cache and update with Released = 1 will not override balances/amount in document
						APDocument.Cache.SetStatus(doc, PXEntryStatus.Updated);

						bool prebooked = (doc.Prebooked == true);
						bool released = (doc.Released == true); //Save state of the document - prebooked & released flags will be altered during release process
						if (prebooked)
						{
							doc.Prebooked = false;
						}

						doc.Released = false;

						foreach (PXResult<APInvoice, CurrencyInfo, Terms, Vendor> res in APInvoice_DocType_RefNbr.Select(doc.DocType, doc.RefNbr))
						{
							if (doc.PaymentsByLinesAllowed != true && !IsPayByLineRetainageDebitAdj(doc))
							{
								APTran_TranType_RefNbr.StoreResult(new List<object>(), PXQueryParameters.ExplicitParameters(doc.DocType, doc.RefNbr));
							}

							if (doc.Released == false || doc.Prebooked == false)
							{
								SegregateBatch(je, doc.BranchID, doc.CuryID, doc.DocDate, doc.FinPeriodID, doc.DocDesc, (CurrencyInfo)res);
							}
							List<INRegister> inDocs;
							ReleaseInvoice(je, ref doc, res, prebooked, out inDocs);

							doc.Released = released; //Restore flag
							doc.Prebooked = prebooked;
						}

						foreach (PXResult<APPayment, CurrencyInfo, Currency, Vendor, CashAccount> res in APPayment_DocType_RefNbr.Select(doc.DocType, doc.RefNbr, doc.VendorID))
						{
							APPayment payment = res;
							CurrencyInfo info = res;
							Currency paycury = res;
							Vendor vendor = res;
							CashAccount cashacct = res;

							if (doc.DocType == APDocType.DebitAdj || doc.DocType == APDocType.CreditAdj)
							{
								//We don't need payment part processing on release;
								//
								if (doc.Prebooked == true) continue;
							}

							SegregateBatch(je, doc.BranchID, doc.CuryID, payment.AdjDate, payment.AdjFinPeriodID, doc.DocDesc, info);

							int OrigAdjCntr = (int)doc.AdjCntr;
							Amount docBal = new Amount();
							doc.AdjCntr = 0;

							while (doc.AdjCntr < OrigAdjCntr)
							{
								// We should use the same CurrencyInfo for Payment
								// and its applications to save proper consolidation
								// for generated GL transactions
								//
								CM.CurrencyInfo new_info = GetCurrencyInfoCopyForGL(je, info);

								if (doc.AdjCntr == 0 || (doc.DocType != APDocType.QuickCheck && doc.DocType != APDocType.VoidQuickCheck))
								{
									ProcessPayment(je, doc, new PXResult<APPayment, CurrencyInfo, Currency, Vendor, CashAccount>(payment, CurrencyInfo.GetEX(new_info), paycury, vendor, cashacct));
								}

								PXResultset<APAdjust> adjustments = APAdjust_AdjgDocType_RefNbr_VendorID.Select(doc.DocType, doc.RefNbr, _IsIntegrityCheck, doc.AdjCntr);
								foreach (PXResult<APAdjust, CurrencyInfo, Currency, APInvoice, APPayment, Standalone.APRegisterAlias> result in adjustments)
								{
									Standalone.APRegisterAlias adjddoc = (Standalone.APRegisterAlias)result;
									APInvoice invDoc = (APInvoice)result;
									APAdjust adjust = (APAdjust)result;

									bool appliedToVoid = adjust.AdjgDocType == APDocType.VoidCheck ||
										adjust.VoidAdjNbr != null && adjust.AdjgDocType != APDocType.VoidRefund;

									if (adjddoc.DocType == APDocType.Prepayment
										&& adjddoc.AdjCntr == 0
										&& !appliedToVoid
										&& adjust.AdjgDocType.IsNotIn(APDocType.Refund, APDocType.VoidRefund)
										)
									{
										adjddoc.CuryDocBal = 0m;
										adjddoc.DocBal = 0m;

										// APRegisterAlias used for performance optimization on MySQL (see AC-72888)
										//
										Caches[typeof(Standalone.APRegisterAlias)].Update(adjddoc);

										APRegister cached = (APRegister)APDocument.Cache.Locate(invDoc);
										if (cached != null && cached.DocType == APDocType.Prepayment && cached.AdjCntr == 0)
										{
											cached.CuryDocBal = 0m;
											cached.DocBal = 0m;

											// In other places used Cache.Locate() in APRegister cache
											//
											Caches[typeof(APRegister)].Update(cached);
										}
									}
								}

								Tuple<APAdjust, CurrencyInfo> lastAdjustment = ProcessAdjustments(je, adjustments, doc, payment, vendor, new_info, paycury);

								if(doc.IsMigratedRecord != true)
									docBal = new Amount(doc.CuryDocBal, doc.DocBal);
								else
									docBal = new Amount(doc.CuryInitDocBal, doc.InitDocBal);

								doc.AdjCntr++;
								VerifyPaymentRoundAndClose(je, doc, payment, vendor, CurrencyInfo.GetEX(new_info), paycury, lastAdjustment);

								doc.Prebooked = prebooked;
								doc.Released = released;
							}

							if (docBal.Base != 0 &&
							    doc.DocType == APDocType.VoidCheck)
							{
								ProcessVoidPaymentTranPost(doc, docBal);
							}

							APAdjust reversal = APAdjust_AdjgDocType_RefNbr_VendorID.Select(doc.DocType, doc.RefNbr, _IsIntegrityCheck, OrigAdjCntr);
							if (reversal != null && reversal.IsInitialApplication != true)
							{
								doc.OpenDoc = true;
							}
						}

						if (doc.DocType == APDocType.Prepayment && doc.Status == APDocStatus.Voided && doc.OpenDoc == true)
						{
							// Restore Open flag for voided Prepayment without application
							doc.OpenDoc = false;
						}

						APDocument.Cache.Update(doc);
					}

                    Caches[typeof(AP1099Hist)].Clear();

                    foreach (PXResult<APAdjust, APTran, APInvoice> res in PXSelectReadonly2<APAdjust,
                        InnerJoin<APTran, On<APTran.tranType, Equal<APAdjust.adjdDocType>, And<APTran.refNbr, Equal<APAdjust.adjdRefNbr>>>,
                        InnerJoin<APInvoice, On<APInvoice.docType, Equal<APAdjust.adjdDocType>, And<APInvoice.refNbr, Equal<APAdjust.adjdRefNbr>>>>>,
					Where<APAdjust.vendorID, Equal<Required<APAdjust.vendorID>>, And<APAdjust.adjgDocDate, GreaterEqual<Required<APAdjust.adjgDocDate>>, And<APAdjust.released, Equal<True>, And<APAdjust.voided, Equal<False>, And<APTran.box1099, IsNotNull>>>>>>.Select(this, vend.BAccountID, new DateTime(Convert.ToInt32(minPeriod.Substring(0, 4)), 1, 1)))
                    {
                        APAdjust adj = res;
                        APTran tran = res;
                        APInvoice doc = res;

                        Update1099Hist(this, 1, adj, tran, doc);
                    }

					foreach (APRegister apdoc in APDocument.Cache.Updated)
					{
						APDocument.Cache.PersistUpdated(apdoc);
						}

					APTran_TranType_RefNbr.Cache.Persist(PXDBOperation.Update);

					TranPost.Cache.Persist(PXDBOperation.Insert);

					Caches[typeof(APHist)].Persist(PXDBOperation.Insert);

					Caches[typeof(CuryAPHist)].Persist(PXDBOperation.Insert);

                    Caches[typeof(AP1099Hist)].Persist(PXDBOperation.Insert);

                    Caches[typeof(APAdjust)].Persist(PXDBOperation.Update);


					ts.Complete(this);
				}
				APDocument.Cache.Persisted(false);

				Caches[typeof(APHist)].Persisted(false);

				Caches[typeof(CuryAPHist)].Persisted(false);

                Caches[typeof(AP1099Hist)].Persisted(false);

                TranPost.Cache.Persisted(false);

				APTran_TranType_RefNbr.Cache.Persisted(false);

				Caches[typeof(APAdjust)].Persisted(false);
			}
		}

		protected virtual HashedList<APRegister> GetDocumentsForIntegrityCheckProc(string minPeriod)
		{
			// Vendor released documents, that are created or closed ofter MinPeriod
			HashedList<APRegister> venddocs = new HashedList<APRegister>(APDocument.Cache.GetComparer());

			foreach (PXResult<APRegister, APInvoice, APPayment> rec in
				PXSelectJoin<APRegister,
				LeftJoinSingleTable<APInvoice,
					On<APInvoice.docType, Equal<APRegister.docType>,
					And<APInvoice.refNbr, Equal<APRegister.refNbr>>>,
				LeftJoinSingleTable<APPayment,
					On<APPayment.docType, Equal<APRegister.docType>,
					And<APPayment.refNbr, Equal<APRegister.refNbr>>>>>,
					Where<APRegister.vendorID, Equal<Current<Vendor.bAccountID>>,
						And2<Where<APRegister.released, Equal<True>,
							Or<APRegister.prebooked, Equal<True>>>,
						And<Where<APRegister.tranPeriodID, GreaterEqual<Required<APRegister.tranPeriodID>>,
							Or<APRegister.closedTranPeriodID, GreaterEqual<Required<APRegister.closedTranPeriodID>>>>>>>>
				.Select(this, minPeriod, minPeriod))
			{
				APRegister doc = GetFullDocument(rec);
				if (doc != null)
					venddocs.Add(doc);
			}

			// Original retainage documents
			if (PXAccess.FeatureInstalled<FeaturesSet.retainage>())
			{
				PXResultset<APRegister> retainage =
					PXSelectJoin<APRegister,
					LeftJoinSingleTable<APInvoice,
						On<APInvoice.docType, Equal<APRegister.docType>,
						And<APInvoice.refNbr, Equal<APRegister.refNbr>>>,
					InnerJoin<APRegisterAlias,
						On<APRegister.docType, Equal<APRegisterAlias.origDocType>,
							And<APRegister.refNbr, Equal<APRegisterAlias.origRefNbr>>>>>,
					Where<
						APRegisterAlias.vendorID, Equal<Current<Vendor.bAccountID>>,
						And<APRegisterAlias.isRetainageDocument, Equal<True>,
						And2<Where<APRegisterAlias.released, Equal<True>,
								Or<APRegisterAlias.prebooked, Equal<True>>>,
							And<Where<APRegisterAlias.tranPeriodID, GreaterEqual<Required<APRegisterAlias.tranPeriodID>>,
								Or<APRegisterAlias.closedTranPeriodID, GreaterEqual<Required<APRegisterAlias.closedTranPeriodID>>>>>>>>>
						.Select(this, minPeriod, minPeriod);

				var origdocs = venddocs.RowCast<APRegister>().ToHashSet(APDocument.Cache.GetComparer());
				foreach (var retdoc in retainage)
				{
					if (!venddocs.Contains((APRegister)retdoc))
					{
						APInvoice doc = PXResult.Unwrap<APInvoice>(retdoc);
						PXCache<APRegister>.RestoreCopy(doc, PXResult.Unwrap<APRegister>(retdoc));
						venddocs.Add(doc);

						foreach (PXResult<APAdjust, APRegister> docadj in SelectFrom<APAdjust>
								.InnerJoin<APRegister>.On<APAdjust.adjgDocType.IsEqual<APRegister.docType>
									.And<APAdjust.adjgRefNbr.IsEqual<APRegister.refNbr>>>
								.Where<APAdjust.adjdDocType.IsEqual<@P.AsString>
									.And<APAdjust.adjdRefNbr.IsEqual<@P.AsString>>
									.And<APRegister.released.IsEqual<True>>>
									.View.SelectMultiBound(this, null, new object[] { doc.DocType, doc.RefNbr }))
						{
							if (!venddocs.Contains((APRegister)docadj))
							{
								venddocs.Add(new PXResult<APRegister>(docadj));
							}
						}
					}
				}
			}

			// Direct adjustments for vendor documents
			PXResultset<APRegister> adjustments = GetDirectAdjustmentsForIntegrityCheckProc(minPeriod);
			venddocs.AddRange(adjustments.RowCast<APRegister>());

			// Infinite-level adjustments
			GetAllReleasedAdjustments(venddocs, adjustments, minPeriod);

			return venddocs;
		}

		private APRegister GetFullDocument(PXResult<APRegister, APInvoice, APPayment> rec)
		{
			APInvoice invoice = rec;
			APPayment payment = rec;
			APRegister result = null;

			// Restore full invoice / payment from the "single table" stripped version.
			//
			if (invoice?.RefNbr != null)
			{
				PXCache<APRegister>.RestoreCopy(invoice, (APRegister)rec);
				result = invoice;
			}
			else if (payment?.RefNbr != null)
			{
				PXCache<APRegister>.RestoreCopy(payment, (APRegister)rec);
				result = payment;
			}
			return result;
		}

		private IEnumerable<PXResult<APRegister>> GetFullDocuments(IEnumerable<PXResult<APRegister>> list)
		{
			foreach (PXResult<APRegister, APInvoice, APPayment> rec in list)
			{
				APRegister doc = GetFullDocument(rec);
				if (doc != null)
					yield return new PXResult<APRegister>(doc);
			}
		}
		protected virtual PXResultset<APRegister> GetDirectAdjustmentsForIntegrityCheckProc(string minPeriod)
		{
			var adjgs =
				PXSelectJoin<APRegister,
				LeftJoinSingleTable<APInvoice,
					On<APInvoice.docType, Equal<APRegister.docType>,
					And<APInvoice.refNbr, Equal<APRegister.refNbr>>>,
				LeftJoinSingleTable<APPayment,
					On<APPayment.docType, Equal<APRegister.docType>,
					And<APPayment.refNbr, Equal<APRegister.refNbr>>>>>,
				Where<APRegister.vendorID, Equal<Current<Vendor.bAccountID>>,
					And<APRegister.tranPeriodID, Less<Required<APRegister.tranPeriodID>>,
					And2<Where<
						APRegister.released, Equal<True>,
						Or<APRegister.prebooked, Equal<True>>>,
					And2<Where<
						APRegister.closedTranPeriodID, Less<Required<APRegister.closedTranPeriodID>>,
						Or<APRegister.closedTranPeriodID, IsNull>>,
					And<Exists<Select2<APAdjust,
						InnerJoin<Standalone.APRegister2,
							On<
								Standalone.APRegister2.docType, Equal<APAdjust.adjdDocType>,
								And<Standalone.APRegister2.refNbr, Equal<APAdjust.adjdRefNbr>>>>,
						Where<APAdjust.adjgDocType, Equal<APRegister.docType>,
							And<APAdjust.adjgRefNbr, Equal<APRegister.refNbr>,
								And2<Where<Standalone.APRegister2.closedTranPeriodID, GreaterEqual<Required<Standalone.APRegister2.closedTranPeriodID>>,
										Or<APAdjust.adjdTranPeriodID, GreaterEqual<Required<APAdjust.adjdTranPeriodID>>>>,
									And<APAdjust.released, Equal<True>>>>>>>>>>>>>
					.Select(this, minPeriod, minPeriod, minPeriod, minPeriod);

			var adjds =
				PXSelectJoin<APRegister,
				LeftJoinSingleTable<APInvoice,
					On<APInvoice.docType, Equal<APRegister.docType>,
					And<APInvoice.refNbr, Equal<APRegister.refNbr>>>,
				LeftJoinSingleTable<APPayment,
					On<APPayment.docType, Equal<APRegister.docType>,
					And<APPayment.refNbr, Equal<APRegister.refNbr>>>>>,
				Where<APRegister.vendorID, Equal<Current<Vendor.bAccountID>>,
					And<APRegister.tranPeriodID, Less<Required<APRegister.tranPeriodID>>,
					And2<Where<
						APRegister.released, Equal<True>,
						Or<APRegister.prebooked, Equal<True>>>,
					And2<Where<
						APRegister.closedTranPeriodID, Less<Required<APRegister.closedTranPeriodID>>,
						Or<APRegister.closedTranPeriodID, IsNull>>,
					And<Exists<Select2<APAdjust,
						InnerJoin<Standalone.APRegister2,
							On<Standalone.APRegister2.docType, Equal<APAdjust.adjgDocType>,
								And<Standalone.APRegister2.refNbr, Equal<APAdjust.adjgRefNbr>>>>,
						Where<APAdjust.adjdDocType, Equal<APRegister.docType>,
							And<APAdjust.adjdRefNbr, Equal<APRegister.refNbr>,
							And<APAdjust.released, Equal<True>,
							And<Where<Standalone.APRegister2.closedTranPeriodID, GreaterEqual<Required<Standalone.APRegister2.closedTranPeriodID>>,
								Or<APAdjust.adjdTranPeriodID, GreaterEqual<Required<APAdjust.adjdTranPeriodID>>>>>>>>>>>>>>>>
					.Select(this, minPeriod, minPeriod, minPeriod, minPeriod);

			var result = new PXResultset<APRegister>();
			result.AddRange(GetFullDocuments(adjgs));
			result.AddRange(GetFullDocuments(adjds));

			return result;
		}

		protected virtual void GetAllReleasedAdjustments(HashedList<APRegister> documents, PXResultset<APRegister> startFrom, string minPeriod)
		{
			var newlyFoundDocsKeys = startFrom.RowCast<APRegister>().Select(_ => new { DocType = _.DocType, RefNbr = _.RefNbr }).ToList();
			while (newlyFoundDocsKeys.Any())
			{
				var currentList = newlyFoundDocsKeys.ToList();
				newlyFoundDocsKeys.Clear();
				foreach (var source in currentList)
				{
					var adjustments =
						PXSelectJoin<APRegister,
						LeftJoinSingleTable<APInvoice,
							On<APInvoice.docType, Equal<APRegister.docType>,
							And<APInvoice.refNbr, Equal<APRegister.refNbr>>>,
						LeftJoinSingleTable<APPayment,
							On<APPayment.docType, Equal<APRegister.docType>,
							And<APPayment.refNbr, Equal<APRegister.refNbr>>>>>,
						Where<APRegister.vendorID, Equal<Current<Vendor.bAccountID>>,
							And2<Exists<Select<APAdjust,
									Where<APAdjust.adjgDocType, Equal<APRegister.docType>,
								And<APAdjust.adjgRefNbr, Equal<APRegister.refNbr>,
								And<APAdjust.released, Equal<True>,
							And<APAdjust.adjdDocType, Equal<Required<APAdjust.adjdDocType>>,
								And<APAdjust.adjdRefNbr, Equal<Required<APAdjust.adjdRefNbr>>>>>>>>>,
							And<APRegister.tranPeriodID, Less<Required<APRegister.tranPeriodID>>,
							And2<Where<
								APRegister.released, Equal<True>,
								Or<APRegister.prebooked, Equal<True>>>,
							And<Where<
								APRegister.closedTranPeriodID, Less<Required<APRegister.closedTranPeriodID>>,
								Or<APRegister.closedTranPeriodID, IsNull>>>>>>>>
							.Select(this, source.DocType, source.RefNbr, minPeriod, minPeriod);

					// do not add already found document
					foreach (PXResult<APRegister> rec in GetFullDocuments(adjustments))
					{
						APRegister doc = rec;
						if (doc == null) continue;
						if (!documents.Contains(doc))
						{
							newlyFoundDocsKeys.Add(new { doc.DocType, doc.RefNbr });
							documents.Add(rec);
						}
					}

					var adjds =
						PXSelectJoin<APRegister,
						LeftJoinSingleTable<APInvoice,
							On<APInvoice.docType, Equal<APRegister.docType>,
								And<APInvoice.refNbr, Equal<APRegister.refNbr>>>,
						LeftJoinSingleTable<APPayment,
							On<APPayment.docType, Equal<APRegister.docType>,
								And<APPayment.refNbr, Equal<APRegister.refNbr>>>>>,
						Where<APRegister.vendorID, Equal<Current<Vendor.bAccountID>>,
							And2<Exists<Select<APAdjust,
										Where<APAdjust.adjdDocType, Equal<APRegister.docType>,
											And<APAdjust.adjdRefNbr, Equal<APRegister.refNbr>,
							And<APAdjust.released, Equal<True>,
											And<APAdjust.adjgDocType, Equal<Required<APAdjust.adjdDocType>>,
											And<APAdjust.adjgRefNbr, Equal<Required<APAdjust.adjdRefNbr>>>>>>>>>,
							And<APRegister.tranPeriodID, Less<Required<APRegister.tranPeriodID>>,
							And2<Where<
								APRegister.released, Equal<True>,
								Or<APRegister.prebooked, Equal<True>>>,
							And<Where<
								APRegister.closedTranPeriodID, Less<Required<APRegister.closedTranPeriodID>>,
								Or<APRegister.closedTranPeriodID, IsNull>>>>>>>>
							.Select(this, source.DocType, source.RefNbr, minPeriod, minPeriod);


					// do not add already found document
					foreach (PXResult<APRegister> rec in GetFullDocuments(adjds))
					{
						APRegister doc = rec;
						if (doc == null) continue;
						if (!documents.Contains(doc))
						{
							newlyFoundDocsKeys.Add(new { doc.DocType, doc.RefNbr });
							documents.Add(rec);
						}
					}
				}
			}
		}

		public virtual void RetrievePPVAccount(PXGraph aOpGraph, POReceiptLineR1 aLine, ref int? aPPVAcctID, ref int? aPPVSubID) 
		{
			aPPVAcctID = null;
			aPPVSubID = null;
			PXResult<PO.POReceiptLine, IN.InventoryItem, IN.INPostClass, INSite> res = (PXResult<PO.POReceiptLine, IN.InventoryItem, IN.INPostClass, INSite>)
							PXSelectJoin<PO.POReceiptLine, InnerJoin<InventoryItem, On<PO.POReceiptLine.inventoryID, Equal<InventoryItem.inventoryID>>,
													InnerJoin<IN.INPostClass, On<IN.INPostClass.postClassID, Equal<IN.InventoryItem.postClassID>>,
													InnerJoin<IN.INSite, On<IN.INSite.siteID, Equal<PO.POReceiptLine.siteID>>>>>,
							Where<
								PO.POReceiptLine.receiptType, Equal<Required<PO.POReceiptLine.receiptType>>,
								And<PO.POReceiptLine.receiptNbr, Equal<Required<PO.POReceiptLine.receiptNbr>>,
								And<PO.POReceiptLine.lineNbr, Equal<Required<PO.POReceiptLine.lineNbr>>>>>>.Select(this, aLine.ReceiptType, aLine.ReceiptNbr, aLine.LineNbr);
			if (res != null)
			{
				InventoryItem  invItem = (InventoryItem)res;
				INPostClass postClass = (INPostClass)res;
				INSite invSite = (INSite)res;
				aPPVAcctID = INReleaseProcess.GetAcctID<INPostClass.pPVAcctID>(aOpGraph, postClass.PPVAcctDefault, invItem, invSite, postClass);
				try
				{
					aPPVSubID = INReleaseProcess.GetSubID<INPostClass.pPVSubID>(aOpGraph, postClass.PPVAcctDefault, postClass.PPVSubMask, invItem, invSite, postClass);
				}
				catch (PXException)
				{
					throw new PXException(Messages.PPVSubAccountMaskCanNotBeAssembled);
				}
			}
		}

		public virtual void VoidDocProc(JournalEntry je, APRegister doc)
		{
			if (doc.Released == true && doc.Prebooked == true)
			{
				throw new PXException(Messages.PrebookedDocumentsMayNotBeVoidedAfterTheyAreReleased);
			}

			if (doc.Prebooked == true && string.IsNullOrEmpty(doc.PrebookBatchNbr))
			{
				throw new PXException(Messages.LinkToThePrebookingBatchIsMissingVoidImpossible, doc.DocType, doc.RefNbr);
			}

			APAdjust adjustment = PXSelectReadonly<APAdjust, Where<APAdjust.adjdDocType, Equal<Required<APAdjust.adjdDocType>>,
								And<APAdjust.adjdRefNbr, Equal<Required<APAdjust.adjdRefNbr>>>>>.Select(this, doc.DocType, doc.RefNbr);
			if (adjustment != null && string.IsNullOrEmpty(adjustment.AdjgRefNbr) == false)
			{
				throw new PXException(Messages.PrebookedDocumentMayNotBeVoidedIfPaymentsWereAppliedToIt);
			}


			APTran tran = PXSelectReadonly<APTran, Where<APTran.tranType, Equal<Required<APTran.tranType>>, And<APTran.refNbr, Equal<Required<APTran.refNbr>>,
											And<Where<APTran.pONbr, IsNotNull, Or<APTran.receiptNbr, IsNotNull>>>>>>.Select(this, doc.DocType, doc.RefNbr);
			if (tran != null && !string.IsNullOrEmpty(tran.RefNbr))
			{
				throw new PXException(Messages.ThisDocumentConatinsTransactionsLinkToPOVoidIsNotPossible);
			}

			APTaxTran reportedTaxTran = PXSelectReadonly<APTaxTran, Where<APTaxTran.tranType, Equal<Required<APTaxTran.tranType>>, And<APTaxTran.refNbr, Equal<Required<APTaxTran.refNbr>>, And<APTaxTran.taxPeriodID, IsNotNull>>>>.Select(this, doc.DocType, doc.RefNbr);
			if (reportedTaxTran != null && string.IsNullOrEmpty(reportedTaxTran.TaxID) == false)
			{
				throw new PXException(Messages.TaxesForThisDocumentHaveBeenReportedVoidIsNotPossible);
			}
			//using (new PXConnectionScope())
			{
				using (PXTransactionScope ts = new PXTransactionScope())
				{
					//mark as updated so that doc will not expire from cache and update with Released = 1 will not override balances/amount in document
					APDocument.Cache.SetStatus(doc, PXEntryStatus.Updated);
					string batchNbr = doc.Prebooked == true ? doc.PrebookBatchNbr : doc.BatchNbr;
					GL.Batch batch = PXSelectReadonly<GL.Batch,
								Where<GL.Batch.module, Equal<GL.BatchModule.moduleAP>, And<GL.Batch.batchNbr, Equal<Required<GL.Batch.batchNbr>>>>>.Select(this, batchNbr);
					if (batch == null && string.IsNullOrEmpty(batch.BatchNbr))
					{
						throw new PXException(Messages.PrebookingBatchDoesNotExistsInTheSystemVoidImpossible, GL.BatchModule.AP, doc.PrebookBatchNbr);
					}

					je.ReverseDocumentBatch(batch);
					Batch newBatch = (Batch)je.BatchModule.Cache.CreateCopy(je.BatchModule.Current);
					newBatch.Hold = false;
					je.BatchModule.Update(newBatch);
					if (doc.OpenDoc == true)
					{
						GLTran apTran = CreateGLTranAP(je, doc, true);
						GLTran apTranActual = null;
						foreach (GLTran iTran in je.GLTranModuleBatNbr.Select())
						{
							if (apTranActual == null
									&& iTran.AccountID == apTran.AccountID && iTran.SubID == apTran.SubID && iTran.ReferenceID == apTran.ReferenceID
									&& iTran.TranType == apTran.TranType && iTran.RefNbr == apTran.RefNbr
									&& iTran.ReferenceID == apTran.ReferenceID
									&& iTran.BranchID == apTran.BranchID && iTran.CuryCreditAmt == iTran.CuryCreditAmt && iTran.CuryDebitAmt == iTran.CuryDebitAmt) //Detect AP Tran
							{
								apTranActual = iTran;
							}
							iTran.Released = true;
						}
						if (apTranActual != null)
						{
							UpdateHistory(apTranActual, doc.VendorID);
							UpdateHistory(apTranActual, doc.VendorID, doc.CuryID);
						}
						else
						{
							throw new PXException(Messages.APTransactionIsNotFoundInTheReversingBatch);
						}
					}

					foreach (APTaxTran iTaxTran in this.APTaxTran_TranType_RefNbr.Select(doc.DocType, doc.RefNbr))
					{
						PXCache taxCache = this.APTaxTran_TranType_RefNbr.Cache;
						APTaxTran copy = (APTaxTran)taxCache.CreateCopy(iTaxTran);
						copy.Voided = true;
						this.APTaxTran_TranType_RefNbr.Update(copy);
					}

					if (je.GLTranModuleBatNbr.Cache.IsInsertedUpdatedDeleted)
					{
						je.Persist();
					}

					//leave BatchNbr empty for Prepayment Requests
					if (!je.BatchModule.Cache.IsDirty && string.IsNullOrEmpty(doc.VoidBatchNbr) && (APInvoice_DocType_RefNbr.Current == null || APInvoice_DocType_RefNbr.Current.DocType != APDocType.Prepayment))
					{
						doc.VoidBatchNbr = ((Batch)je.BatchModule.Current).BatchNbr;
					}
					doc.OpenDoc = false;
					doc.Voided = true;
					doc.CuryDocBal = Decimal.Zero;
					doc.DocBal = Decimal.Zero;
					doc = (APRegister)APDocument.Cache.Update(doc);
					ProcessVoidTranPost(doc);

					PXTimeStampScope.DuplicatePersisted(APDocument.Cache, doc, typeof(APInvoice));
					PXTimeStampScope.DuplicatePersisted(APDocument.Cache, doc, typeof(APPayment));

					if (doc.DocType != APDocType.DebitAdj)
					{
						if (APDocument.Cache.ObjectsEqual(doc, APPayment_DocType_RefNbr.Current))
						{
							APPayment_DocType_RefNbr.Cache.SetStatus(APPayment_DocType_RefNbr.Current, PXEntryStatus.Notchanged);
						}
					}
					//this.Persist();
					//APPayment_DocType_RefNbr.Cache.Persist(PXDBOperation.Insert);
					//APPayment_DocType_RefNbr.Cache.Persist(PXDBOperation.Update);
					APDocument.Cache.Persist(PXDBOperation.Update);
					APTran_TranType_RefNbr.Cache.Persist(PXDBOperation.Update);
					APTaxTran_TranType_RefNbr.Cache.Persist(PXDBOperation.Update);

					Caches[typeof(APHist)].Persist(PXDBOperation.Insert);
					Caches[typeof(CuryAPHist)].Persist(PXDBOperation.Insert);
					Caches[typeof(APTranPost)].Persist(PXDBOperation.Insert);

					AP1099Year_Select.Cache.Persist(PXDBOperation.Insert);
					AP1099History_Select.Cache.Persist(PXDBOperation.Insert);

					CurrencyInfo_CuryInfoID.Cache.Persist(PXDBOperation.Update);
					ts.Complete(this);
				}

				APPayment_DocType_RefNbr.Cache.Persisted(false);
				APDocument.Cache.Persisted(false);
				APTran_TranType_RefNbr.Cache.Persisted(false);
				APTaxTran_TranType_RefNbr.Cache.Persisted(false);
				APAdjust_AdjgDocType_RefNbr_VendorID.Cache.Persisted(false);

				Caches[typeof(APHist)].Persisted(false);
				Caches[typeof(CuryAPHist)].Persisted(false);

				AP1099Year_Select.Cache.Persisted(false);
				AP1099History_Select.Cache.Persisted(false);

				CurrencyInfo_CuryInfoID.Cache.Persisted(false);
			}
		}

		protected static GLTran CreateGLTranAP(JournalEntry journalEntry, APRegister doc, bool aReversed)
		{
			GLTran tran = new GLTran();
			tran.SummPost = true;
			tran.BranchID = doc.BranchID;
			tran.AccountID = doc.APAccountID;
			tran.SubID = doc.APSubID;
			tran.ReclassificationProhibited = true;
			bool isDebit = APInvoiceType.DrCr(doc.DocType) == DrCr.Debit;
			tran.CuryDebitAmt = (isDebit && !aReversed) ? 0m : doc.CuryOrigDocAmt;
			tran.DebitAmt = (isDebit && !aReversed) ? 0m : doc.OrigDocAmt - doc.RGOLAmt;
			tran.CuryCreditAmt = (isDebit && !aReversed) ? doc.CuryOrigDocAmt : 0m;
			tran.CreditAmt = (isDebit && !aReversed) ? doc.OrigDocAmt - doc.RGOLAmt : 0m;

			tran.TranType = doc.DocType;
			tran.TranClass = doc.DocClass;
			tran.RefNbr = doc.RefNbr;
			tran.TranDesc = doc.DocDesc;
			FinPeriodIDAttribute.SetPeriodsByMaster<GLTran.finPeriodID>(
                journalEntry.GLTranModuleBatNbr.Cache, tran, doc.FinPeriodID);
			tran.TranDate = doc.DocDate;
			tran.ReferenceID = doc.VendorID;
			return tran;
		}

		private bool IsPayByLineRetainageDebitAdj(APRegister doc)
		{
			return
				doc.PaymentsByLinesAllowed != true &&
				(doc.IsChildRetainageDocument() || doc.IsOriginalRetainageDocument()) &&
				doc.DocType == APDocType.DebitAdj &&
				GetOriginalRetainageDocument(doc)?.PaymentsByLinesAllowed == true;
		}

		public virtual APRegister GetOriginalRetainageDocument(APRegister childRetainageDoc)
		{
			APRegister origRetainageDoc = PXSelect<APInvoice,
				Where<APInvoice.docType, Equal<Required<APInvoice.docType>>,
					And<APInvoice.refNbr, Equal<Required<APInvoice.refNbr>>,
					And<APInvoice.retainageApply, Equal<True>>>>>
				.SelectSingleBound(this, null, childRetainageDoc.OrigDocType, childRetainageDoc.OrigRefNbr);
			APRegister cached = APDocument.Cache.Locate(origRetainageDoc) as APRegister;
			if (cached != null && origRetainageDoc != null)
				APDocument.Cache.RestoreCopy(origRetainageDoc, cached);
			return origRetainageDoc;
		}

		public virtual APTran GetOriginalRetainageLine(APRegister childRetainageDoc, APTran childRetainageTran)
		{
			APTran origRetainageLine = PXSelect<APTran,
				Where<APTran.tranType, Equal<Required<APTran.tranType>>,
					And<APTran.refNbr, Equal<Required<APTran.refNbr>>,
					And<APTran.lineNbr, Equal<Required<APTran.lineNbr>>,
					And<APTran.curyRetainageAmt, NotEqual<decimal0>>>>>>
				.SelectSingleBound(this, null,
					childRetainageDoc.OrigDocType,
					childRetainageDoc.OrigRefNbr,
					childRetainageTran.OrigLineNbr);

			return origRetainageLine;
		}

		public virtual bool IsFullyProcessedOriginalRetainageDocument(APRegister origRetainageInvoice)
		{
			decimal curyRetainageUnpaidTotal = 0m;

			// APRegister class should be used here,
			// otherwise you will get not updated records
			// with incorrect balances.
			//
			foreach (APRegister childRetainageBill in PXSelect<APRegister,
				Where<APRegister.isRetainageDocument, Equal<True>,
					And<APRegister.origDocType, Equal<Required<APRegister.docType>>,
					And<APRegister.origRefNbr, Equal<Required<APRegister.refNbr>>,
					And<APRegister.released, Equal<True>>>>>>
				.Select(this, origRetainageInvoice.DocType, origRetainageInvoice.RefNbr))
			{
				curyRetainageUnpaidTotal += (childRetainageBill.CuryDocBal ?? 0m) * (childRetainageBill.SignAmount ?? 0m);
			}

			return
				origRetainageInvoice.CuryDocBal == 0m &&
				origRetainageInvoice.CuryRetainageUnreleasedAmt == 0m &&
				curyRetainageUnpaidTotal == 0m;
		}

		#region Customizable virtual methods for GL transactions insertion

		public class GLTranInsertionContext
		{
			public virtual APRegister APRegisterRecord { get; set; }
			public virtual APTran APTranRecord { get; set; }
			public virtual APTaxTran APTaxTranRecord { get; set; }

			public virtual APPaymentChargeTran APPaymentChargeTranRecord { get; set; }
			public virtual APAdjust APAdjustRecord { get; set; }

			public virtual POReceiptLineR1 POReceiptLineRecord { get; set; }
		}

		#region Invoice
		/// <summary>
		/// Posts per-unit tax amounts to document lines' accounts.
		/// This is an extension point, actual posting is done by graph extension <see cref="APReleaseProcessPerUnitTaxPoster"/>
		/// which overrides this method if the festure "Per-unit Tax Support" is turned on.
		/// </summary>
		protected virtual void PostPerUnitTaxAmounts(JournalEntry journalEntry, APInvoice invoice, CurrencyInfo newCurrencyInfo,
													 APTaxTran perUnitAggregatedTax, Tax perUnitTax, bool isDebitTaxTran)
		{
		}

		/// <summary>
		/// The method to insert invoice GL transactions
		/// for the <see cref="APInvoice"/> entity inside the
		/// <see cref="ReleaseInvoice(JournalEntry, ref APRegister, PXResult{APInvoice, CurrencyInfo, Terms, Vendor}, bool, out List{INRegister})"/> method.
		/// <see cref="GLTranInsertionContext"/> class content:
		/// <see cref="GLTranInsertionContext.APRegisterRecord"/>.
		/// </summary>
		public virtual GLTran InsertInvoiceTransaction(
			JournalEntry je,
			GLTran tran,
			GLTranInsertionContext context)
		{
			return je.GLTranModuleBatNbr.Insert(tran);
		}

		/// <summary>
		/// The method to insert invoice tax GL transactions
		/// for the <see cref="APTaxTran"/> entity inside the
		/// <see cref="ReleaseInvoice(JournalEntry, ref APRegister, PXResult{APInvoice, CurrencyInfo, Terms, Vendor}, bool, out List{INRegister})"/> method.
		/// <see cref="GLTranInsertionContext"/> class content:
		/// <see cref="GLTranInsertionContext.APRegisterRecord"/>.
		/// </summary>
		public virtual GLTran InsertInvoiceTaxTransaction(
			JournalEntry je,
			GLTran tran,
			GLTranInsertionContext context)
		{
			return je.GLTranModuleBatNbr.Insert(tran);
		}

		/// <summary>
		/// The method to insert tax expense GL transactions
		/// for the <see cref="APTaxTran"/> entity inside the
		/// <see cref="PostTaxExpenseToItemAccounts(JournalEntry, APInvoice, CurrencyInfo, APTaxTran, Tax, bool)"/> method.
		/// <see cref="GLTranInsertionContext"/> class content:
		/// <see cref="GLTranInsertionContext.APRegisterRecord"/>,
		/// <see cref="GLTranInsertionContext.APTranRecord"/>,
		/// <see cref="GLTranInsertionContext.APTaxTranRecord"/>.
		/// </summary>
		public virtual GLTran InsertInvoiceTaxExpenseItemAccountsTransaction(
			JournalEntry je,
			GLTran tran,
			GLTranInsertionContext context)
		{
			return je.GLTranModuleBatNbr.Insert(tran);
		}

		/// <summary>
		/// The method to insert tax expense GL transactions for the <see cref="APTaxTran"/> entity inside the
		/// <see cref="PerUnitTaxesPostOnRelease.PostPerUnitTaxAmountsToItemAccounts(APInvoice, CurrencyInfo, APTaxTran, Tax, bool, bool)"/> helper method.
		/// <see cref="GLTranInsertionContext"/> class content:
		/// <see cref="GLTranInsertionContext.APRegisterRecord"/>,
		/// <see cref="GLTranInsertionContext.APTranRecord"/>,
		/// <see cref="GLTranInsertionContext.APTaxTranRecord"/>.
		/// </summary>
		public virtual GLTran InsertInvoicePerUnitTaxAmountsToItemAccountsTransaction(JournalEntry journalEntryGraph, GLTran tran,
																					  GLTranInsertionContext context)
		{
			journalEntryGraph.ThrowOnNull(nameof(journalEntryGraph));
			return journalEntryGraph.GLTranModuleBatNbr.Insert(tran);
		}

		/// <summary>
		/// The method to insert grouped by project key tax GL transactions
		/// for the <see cref="APTaxTran"/> entity inside the
		/// <see cref="PostTaxAmountByProjectKey(JournalEntry, APInvoice, CurrencyInfo, APTaxTran, Tax, bool)"/> method.
		/// <see cref="GLTranInsertionContext"/> class content:
		/// <see cref="GLTranInsertionContext.APRegisterRecord"/>,
		/// <see cref="GLTranInsertionContext.APTranRecord"/>,
		/// <see cref="GLTranInsertionContext.APTaxTranRecord"/>.
		/// </summary>
		public virtual GLTran InsertInvoiceTaxByProjectKeyTransaction(
			JournalEntry je,
			GLTran tran,
			GLTranInsertionContext context)
		{
			return je.GLTranModuleBatNbr.Insert(tran);
		}

		/// <summary>
		/// The method to insert invoice tax expense GL transactions
		/// for the <see cref="APTaxTran"/> entity inside the
		/// <see cref="PostTaxExpenseToSingleAccount(JournalEntry, APInvoice, CurrencyInfo, APTaxTran, Tax)"/> method.
		/// <see cref="GLTranInsertionContext"/> class content:
		/// <see cref="GLTranInsertionContext.APRegisterRecord"/>,
		/// <see cref="GLTranInsertionContext.APTaxTranRecord"/>.
		/// </summary>
		public virtual GLTran InsertInvoiceTaxExpenseSingeAccountTransaction(
			JournalEntry je,
			GLTran tran,
			GLTranInsertionContext context)
		{
			return je.GLTranModuleBatNbr.Insert(tran);
		}

		/// <summary>
		/// The method to insert invoice reverse tax GL transactions
		/// for the <see cref="APTaxTran"/> entity inside the
		/// <see cref="PostReverseTax(JournalEntry, APInvoice, CurrencyInfo, APTaxTran, Tax)"/> method.
		/// <see cref="GLTranInsertionContext"/> class content:
		/// <see cref="GLTranInsertionContext.APRegisterRecord"/>,
		/// <see cref="GLTranInsertionContext.APTaxTranRecord"/>.
		/// </summary>
		public virtual GLTran InsertInvoiceReverseTaxTransaction(
			JournalEntry je,
			GLTran tran,
			GLTranInsertionContext context)
		{
			return je.GLTranModuleBatNbr.Insert(tran);
		}

		/// <summary>
		/// The method to insert invoice general tax GL transactions
		/// for the <see cref="APTaxTran"/> entity inside the
		/// <see cref="PostGeneralTax(JournalEntry, APInvoice, CurrencyInfo, APTaxTran, Tax)"/> method.
		/// <see cref="GLTranInsertionContext"/> class content:
		/// <see cref="GLTranInsertionContext.APRegisterRecord"/>,
		/// <see cref="GLTranInsertionContext.APTaxTranRecord"/>.
		/// </summary>
		public virtual GLTran InsertInvoiceGeneralTaxTransaction(
			JournalEntry je,
			GLTran tran,
			GLTranInsertionContext context)
		{
			return je.GLTranModuleBatNbr.Insert(tran);
		}

		/// <summary>
		/// The method to insert invoice rounding GL transactions
		/// for the <see cref="APInvoice"/> entity inside the
		/// <see cref="ReleaseInvoice(JournalEntry, ref APRegister, PXResult{APInvoice, CurrencyInfo, Terms, Vendor}, bool, out List{INRegister})"/> method.
		/// <see cref="GLTranInsertionContext"/> class content:
		/// <see cref="GLTranInsertionContext.APRegisterRecord"/>.
		/// </summary>
		public virtual GLTran InsertInvoiceRoundingTransaction(
			JournalEntry je,
			GLTran tran,
			GLTranInsertionContext context)
		{
			return je.GLTranModuleBatNbr.Insert(tran);
		}

		/// <summary>
		/// The method to insert invoice details GL transactions
		/// for the <see cref="APTran"/> entity inside the
		/// <see cref="ReleaseInvoice(JournalEntry, ref APRegister, PXResult{APInvoice, CurrencyInfo, Terms, Vendor}, bool, out List{INRegister})"/> method.
		/// <see cref="GLTranInsertionContext"/> class content:
		/// <see cref="GLTranInsertionContext.APRegisterRecord"/>,
		/// <see cref="GLTranInsertionContext.APTranRecord"/>.
		/// </summary>
		public virtual GLTran InsertInvoiceDetailsTransaction(
			JournalEntry je,
			GLTran tran,
			GLTranInsertionContext context)
		{
			return je.GLTranModuleBatNbr.Insert(tran);
		}

		/// <summary>
		/// The method to insert invoice details schedule GL transactions
		/// for the <see cref="APTran"/> entity inside the
		/// <see cref="ReleaseInvoice(JournalEntry, ref APRegister, PXResult{APInvoice, CurrencyInfo, Terms, Vendor}, bool, out List{INRegister})"/> method.
		/// <see cref="GLTranInsertionContext"/> class content:
		/// <see cref="GLTranInsertionContext.APRegisterRecord"/>,
		/// <see cref="GLTranInsertionContext.APTranRecord"/>.
		/// </summary>
		public virtual GLTran InsertInvoiceDetailsScheduleTransaction(
			JournalEntry je,
			GLTran tran,
			GLTranInsertionContext context)
		{
			return je.GLTranModuleBatNbr.Insert(tran);
		}

		/// <summary>
		/// The method to insert invoice details POReceiptLine GL transactions
		/// for the <see cref="POReceiptLine"/> entity inside the
		/// <see cref="ReleaseInvoice(JournalEntry, ref APRegister, PXResult{APInvoice, CurrencyInfo, Terms, Vendor}, bool, out List{INRegister})"/> method.
		/// <see cref="GLTranInsertionContext"/> class content:
		/// <see cref="GLTranInsertionContext.APRegisterRecord"/>,
		/// <see cref="GLTranInsertionContext.APTranRecord"/>,
		/// <see cref="GLTranInsertionContext.POReceiptLineRecord"/>.
		/// </summary>
		public virtual GLTran InsertInvoiceDetailsPOReceiptLineTransaction(
			JournalEntry je,
			GLTran tran,
			GLTranInsertionContext context)
		{
			return je.GLTranModuleBatNbr.Insert(tran);
		}

		/// <summary>
		/// The method to return PO Accrual Account for GL Tran by taxes
		/// </summary>
		protected virtual void GetItemCostTaxAccount(APRegister apdoc, Tax tax, APTran apTran, APTaxTran apTaxTran, out int? accountID, out int? subID)
		{
			accountID = apTran.AccountID;
			subID = apTran.SubID;
		}

		#endregion

		#region Payment

		/// <summary>
		/// The method to insert payment GL transactions
		/// for the <see cref="APPayment"/> entity inside the
		/// <see cref="ProcessPayment(JournalEntry, APRegister, PXResult{APPayment, CurrencyInfo, CM.Currency, Vendor, CashAccount})"/> method.
		/// <see cref="GLTranInsertionContext"/> class content:
		/// <see cref="GLTranInsertionContext.APRegisterRecord"/>.
		/// </summary>
		public virtual GLTran InsertPaymentTransaction(
			JournalEntry je,
			GLTran tran,
			GLTranInsertionContext context)
		{
			return je.GLTranModuleBatNbr.Insert(tran);
		}

		/// <summary>
		/// The method to insert payment charge GL transactions
		/// for the <see cref="APPaymentChargeTran"/> entity inside the
		/// <see cref="ProcessPayment(JournalEntry, APRegister, PXResult{APPayment, CurrencyInfo, CM.Currency, Vendor, CashAccount})"/> method.
		/// <see cref="GLTranInsertionContext"/> class content:
		/// <see cref="GLTranInsertionContext.APRegisterRecord"/>,
		/// <see cref="GLTranInsertionContext.APPaymentChargeTranRecord"/>.
		/// </summary>
		public virtual GLTran InsertPaymentChargeTransaction(
			JournalEntry je,
			GLTran tran,
			GLTranInsertionContext context)
		{
			return je.GLTranModuleBatNbr.Insert(tran);
		}

		/// <summary>
		/// The method to insert adjustments adjusting GL transactions
		/// for the <see cref="APAdjust"/> entity inside the
		/// <see cref="ProcessAdjustmentAdjusting(JournalEntry, APAdjust, APPayment, Vendor, CurrencyInfo)"/> method.
		/// <see cref="GLTranInsertionContext"/> class content:
		/// <see cref="GLTranInsertionContext.APRegisterRecord"/>,
		/// <see cref="GLTranInsertionContext.APAdjustRecord"/>.
		/// </summary>
		public virtual GLTran InsertAdjustmentsAdjustingTransaction(
			JournalEntry je,
			GLTran tran,
			GLTranInsertionContext context)
		{
			return je.GLTranModuleBatNbr.Insert(tran);
		}

		/// <summary>
		/// The method to insert adjustments adjusted GL transactions
		/// for the <see cref="APAdjust"/> entity inside the
		/// <see cref="ProcessAdjustmentAdjusted(JournalEntry, APAdjust, APPayment, Vendor, CurrencyInfo, CurrencyInfo)"/> method.
		/// <see cref="GLTranInsertionContext"/> class content:
		/// <see cref="GLTranInsertionContext.APRegisterRecord"/>,
		/// <see cref="GLTranInsertionContext.APAdjustRecord"/>.
		/// </summary>
		public virtual GLTran InsertAdjustmentsAdjustedTransaction(
			JournalEntry je,
			GLTran tran,
			GLTranInsertionContext context)
		{
			return je.GLTranModuleBatNbr.Insert(tran);
		}

		/// <summary>
		/// The method to insert adjustments GOL GL transactions
		/// for the <see cref="APAdjust"/> entity inside the
		/// <see cref="ProcessAdjustmentGOL(JournalEntry, APAdjust, APPayment, Vendor, CM.Currency, CM.Currency, CurrencyInfo, CurrencyInfo)"/> method.
		/// <see cref="GLTranInsertionContext"/> class content:
		/// <see cref="GLTranInsertionContext.APRegisterRecord"/>,
		/// <see cref="GLTranInsertionContext.APAdjustRecord"/>.
		/// </summary>
		public virtual GLTran InsertAdjustmentsGOLTransaction(
			JournalEntry je,
			GLTran tran,
			GLTranInsertionContext context)
		{
			return je.GLTranModuleBatNbr.Insert(tran);
		}

		/// <summary>
		/// The method to insert adjustments cash discount GL transactions
		/// for the <see cref="APAdjust"/> entity inside the
		/// <see cref="ProcessAdjustmentCashDiscount(JournalEntry, APAdjust, APPayment, Vendor, CurrencyInfo, CurrencyInfo)"/> method.
		/// <see cref="GLTranInsertionContext"/> class content:
		/// <see cref="GLTranInsertionContext.APRegisterRecord"/>,
		/// <see cref="GLTranInsertionContext.APAdjustRecord"/>.
		/// </summary>
		public virtual GLTran InsertAdjustmentsCashDiscountTransaction(
			JournalEntry je,
			GLTran tran,
			GLTranInsertionContext context)
		{
			return je.GLTranModuleBatNbr.Insert(tran);
		}

		/// <summary>
		/// The method to insert adjustments withholding tax GL transactions
		/// for the <see cref="APAdjust"/> entity inside the
		/// <see cref="CreateGLTranForWhTaxTran(JournalEntry, APAdjust, APPayment, APTaxTran, Vendor, CurrencyInfo, bool)"/> method.
		/// <see cref="GLTranInsertionContext"/> class content:
		/// <see cref="GLTranInsertionContext.APRegisterRecord"/>,
		/// <see cref="GLTranInsertionContext.APTaxTranRecord"/>,
		/// <see cref="GLTranInsertionContext.APAdjustRecord"/>.
		/// </summary>
		public virtual GLTran InsertAdjustmentsWhTaxTransaction(
			JournalEntry je,
			GLTran tran,
			GLTranInsertionContext context)
		{
			return je.GLTranModuleBatNbr.Insert(tran);
		}

		/// <summary>
		/// The method to insert adjustments rounding GL transactions
		/// for the <see cref="APAdjust"/> entity inside the
		/// <see cref="VerifyPaymentRoundAndClose(JournalEntry, APRegister, APPayment, Vendor, CurrencyInfo, CM.Currency, Tuple{APAdjust, CurrencyInfo})"/> method.
		/// <see cref="GLTranInsertionContext"/> class content:
		/// <see cref="GLTranInsertionContext.APRegisterRecord"/>,
		/// <see cref="GLTranInsertionContext.APAdjustRecord"/>.
		/// </summary>
		public virtual GLTran InsertAdjustmentsRoundingTransaction(
			JournalEntry je,
			GLTran tran,
			GLTranInsertionContext context)
		{
			return je.GLTranModuleBatNbr.Insert(tran);
		}

		#endregion

		#endregion

		/// <exclude/>
		public class ExtensionSort
			: SortExtensionsBy<ExtensionOrderFor<APReleaseProcess>
				.FilledWith<
					MultiCurrency,
					PO.GraphExtensions.APReleaseProcessExt.UpdatePOOnRelease,
					AffectedPOOrdersByAPRelease
				>>
		{ }
	}
}

namespace PX.Objects.AP.Overrides.APDocumentRelease
{
	[PXAccumulator(SingleRecord = true)]
    [Serializable]
	public partial class AP1099Yr : AP1099Year
	{
		#region FinYear
		public new abstract class finYear : PX.Data.BQL.BqlString.Field<finYear> { }
		[PXDBString(4, IsKey = true, IsFixed = true)]
		[PXDefault()]
		public override String FinYear
		{
			get
			{
				return this._FinYear;
			}
			set
			{
				this._FinYear = value;
			}
		}
		#endregion
	}

	[PXAccumulator(SingleRecord = true)]
    [Serializable]
	public partial class AP1099Hist : AP1099History
	{
		#region BranchID
		public new abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
		[PXDBInt(IsKey = true)]
		[PXDefault()]
		public override Int32? BranchID
		{
			get
			{
				return this._BranchID;
			}
			set
			{
				this._BranchID = value;
			}
		}
		#endregion
		#region VendorID
		public new abstract class vendorID : PX.Data.BQL.BqlInt.Field<vendorID> { }
		[PXDBInt(IsKey = true)]
		[PXDefault()]
		public override Int32? VendorID
		{
			get
			{
				return this._VendorID;
			}
			set
			{
				this._VendorID = value;
			}
		}
		#endregion
		#region FinYear
		public new abstract class finYear : PX.Data.BQL.BqlString.Field<finYear> { }
		[PXDBString(4, IsKey = true, IsFixed = true)]
		[PXSelector(typeof(Search2<AP1099Year.finYear,
									InnerJoin<Branch,
										On<Branch.organizationID, Equal<AP1099Year.organizationID>>>,
									Where<AP1099Year.status, Equal<AP1099Year.status.open>,
											And<Branch.branchID, Equal<Current<AP1099Hist.branchID>>>>>),
					DirtyRead = true)]
		[PXDefault()]
		public override String FinYear
		{
			get
			{
				return this._FinYear;
			}
			set
			{
				this._FinYear = value;
			}
		}
		#endregion
		#region BoxNbr
		public new abstract class boxNbr : PX.Data.BQL.BqlShort.Field<boxNbr> { }
		[PXDBShort(IsKey = true)]
		[PXSelector(typeof(Search<AP1099Box.boxNbr>))]
		[PXDefault()]
		public override Int16? BoxNbr
		{
			get
			{
				return this._BoxNbr;
			}
			set
			{
				this._BoxNbr = value;
			}
		}
		#endregion
	}

	public interface IBaseAPHist
	{
		Boolean? DetDeleted
		{
			get;
			set;
		}
        Boolean? FinFlag
		{
			get;
			set;
		}
		Decimal? PtdCrAdjustments
		{
			get;
			set;
		}
		Decimal? PtdDrAdjustments
		{
			get;
			set;
		}
		Decimal? PtdPurchases
		{
			get;
			set;
		}
		Decimal? PtdPayments
		{
			get;
			set;
		}
		Decimal? PtdDiscTaken
		{
			get;
			set;
		}
		Decimal? PtdWhTax
		{
			get;
			set;
		}
		Decimal? PtdRGOL
		{
			get;
			set;
		}
		Decimal? YtdBalance
		{
			get;
			set;
		}
		Decimal? BegBalance
		{
			get;
			set;
		}
		Decimal? PtdDeposits
		{
			get;
			set;
		}
		Decimal? YtdDeposits
		{
			get;
			set;
		}
		Decimal? YtdRetainageReleased
		{
			get;
			set;
		}
		Decimal? PtdRetainageReleased
		{
			get;
			set;
		}
		Decimal? YtdRetainageWithheld
		{
			get;
			set;
		}
		Decimal? PtdRetainageWithheld
		{
			get;
			set;
		}
	}

	public interface ICuryAPHist
	{
		Decimal? CuryPtdCrAdjustments
		{
			get;
			set;
		}
		Decimal? CuryPtdDrAdjustments
		{
			get;
			set;
		}
		Decimal? CuryPtdPurchases
		{
			get;
			set;
		}
		Decimal? CuryPtdPayments
		{
			get;
			set;
		}
		Decimal? CuryPtdDiscTaken
		{
			get;
			set;
		}
		Decimal? CuryPtdWhTax
		{
			get;
			set;
		}
		Decimal? CuryYtdBalance
		{
			get;
			set;
		}
		Decimal? CuryBegBalance
		{
			get;
			set;
		}
		Decimal? CuryPtdDeposits
		{
			get;
			set;
		}
		Decimal? CuryYtdDeposits
		{
			get;
			set;
		}
		Decimal? CuryYtdRetainageReleased
		{
			get;
			set;
		}
		Decimal? CuryPtdRetainageReleased
		{
			get;
			set;
		}
		Decimal? CuryYtdRetainageWithheld
		{
			get;
			set;
		}
		Decimal? CuryPtdRetainageWithheld
		{
			get;
			set;
		}
	}

	[PXAccumulator(new Type[] {
				typeof(CuryAPHistory.finYtdBalance),
				typeof(CuryAPHistory.tranYtdBalance),
				typeof(CuryAPHistory.curyFinYtdBalance),
				typeof(CuryAPHistory.curyTranYtdBalance),
				typeof(CuryAPHistory.finYtdBalance),
				typeof(CuryAPHistory.tranYtdBalance),
				typeof(CuryAPHistory.curyFinYtdBalance),
				typeof(CuryAPHistory.curyTranYtdBalance),
				typeof(CuryAPHistory.finYtdDeposits),
				typeof(CuryAPHistory.tranYtdDeposits),
				typeof(CuryAPHistory.curyFinYtdDeposits),
				typeof(CuryAPHistory.curyTranYtdDeposits),
				typeof(CuryAPHistory.finYtdRetainageReleased),
				typeof(CuryAPHistory.tranYtdRetainageReleased),
				typeof(CuryAPHistory.finYtdRetainageWithheld),
				typeof(CuryAPHistory.tranYtdRetainageWithheld),
				typeof(CuryAPHistory.curyFinYtdRetainageReleased),
				typeof(CuryAPHistory.curyTranYtdRetainageReleased),
				typeof(CuryAPHistory.curyFinYtdRetainageWithheld),
				typeof(CuryAPHistory.curyTranYtdRetainageWithheld)
				},
					new Type[] {
				typeof(CuryAPHistory.finBegBalance),
				typeof(CuryAPHistory.tranBegBalance),
				typeof(CuryAPHistory.curyFinBegBalance),
				typeof(CuryAPHistory.curyTranBegBalance),
				typeof(CuryAPHistory.finYtdBalance),
				typeof(CuryAPHistory.tranYtdBalance),
				typeof(CuryAPHistory.curyFinYtdBalance),
				typeof(CuryAPHistory.curyTranYtdBalance),
				typeof(CuryAPHistory.finYtdDeposits),
				typeof(CuryAPHistory.tranYtdDeposits),
				typeof(CuryAPHistory.curyFinYtdDeposits),
				typeof(CuryAPHistory.curyTranYtdDeposits),
				typeof(CuryAPHistory.finYtdRetainageReleased),
				typeof(CuryAPHistory.tranYtdRetainageReleased),
				typeof(CuryAPHistory.finYtdRetainageWithheld),
				typeof(CuryAPHistory.tranYtdRetainageWithheld),
				typeof(CuryAPHistory.curyFinYtdRetainageReleased),
				typeof(CuryAPHistory.curyTranYtdRetainageReleased),
				typeof(CuryAPHistory.curyFinYtdRetainageWithheld),
				typeof(CuryAPHistory.curyTranYtdRetainageWithheld)
				}
			)]
    [Serializable]
	[PXHidden]
	public partial class CuryAPHist : CuryAPHistory, ICuryAPHist, IBaseAPHist
	{
		#region BranchID
		public new abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
		[PXDBInt(IsKey = true)]
		public override Int32? BranchID
		{
			get
			{
				return this._BranchID;
			}
			set
			{
				this._BranchID = value;
			}
		}
		#endregion
		#region AccountID
		public new abstract class accountID : PX.Data.BQL.BqlInt.Field<accountID> { }
		[PXDBInt(IsKey = true)]
		[PXDefault()]
		public override Int32? AccountID
		{
			get
			{
				return this._AccountID;
			}
			set
			{
				this._AccountID = value;
			}
		}
		#endregion
		#region SubID
		public new abstract class subID : PX.Data.BQL.BqlInt.Field<subID> { }
		[PXDBInt(IsKey = true)]
		[PXDefault()]
		public override Int32? SubID
		{
			get
			{
				return this._SubID;
			}
			set
			{
				this._SubID = value;
			}
		}
		#endregion
		#region VendorID
		public new abstract class vendorID : PX.Data.BQL.BqlInt.Field<vendorID> { }
		[PXDBInt(IsKey = true)]
		[PXDefault()]
		public override Int32? VendorID
		{
			get
			{
				return this._VendorID;
			}
			set
			{
				this._VendorID = value;
			}
		}
		#endregion
		#region CuryID
		public new abstract class curyID : PX.Data.BQL.BqlString.Field<curyID> { }
		[PXDBString(5, IsUnicode = true, IsKey = true, InputMask = ">LLLLL")]
		[PXDefault()]
		public override String CuryID
		{
			get
			{
				return this._CuryID;
			}
			set
			{
				this._CuryID = value;
			}
		}
		#endregion
		#region FinPeriodID
		public new abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }
		[PXDBString(6, IsKey = true, IsFixed = true)]
		[PXDefault()]
		public override String FinPeriodID
		{
			get
			{
				return this._FinPeriodID;
			}
			set
			{
				this._FinPeriodID = value;
			}
		}
		#endregion
	}

	[PXAccumulator(new Type[] {
				typeof(APHistory.finYtdBalance),
				typeof(APHistory.tranYtdBalance),
				typeof(APHistory.finYtdBalance),
				typeof(APHistory.tranYtdBalance),
				typeof(APHistory.finYtdDeposits),
				typeof(APHistory.tranYtdDeposits),
				typeof(APHistory.finYtdRetainageReleased),
				typeof(APHistory.tranYtdRetainageReleased),
				typeof(APHistory.finYtdRetainageWithheld),
				typeof(APHistory.tranYtdRetainageWithheld)
				},
					new Type[] {
				typeof(APHistory.finBegBalance),
				typeof(APHistory.tranBegBalance),
				typeof(APHistory.finYtdBalance),
				typeof(APHistory.tranYtdBalance),
				typeof(APHistory.finYtdDeposits),
				typeof(APHistory.tranYtdDeposits),
				typeof(APHistory.finYtdRetainageReleased),
				typeof(APHistory.tranYtdRetainageReleased),
				typeof(APHistory.finYtdRetainageWithheld),
				typeof(APHistory.tranYtdRetainageWithheld)
				}
			)]
    [Serializable]
    [PXHidden]
	public partial class APHist : APHistory, IBaseAPHist
	{
		#region BranchID
		public new abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
		[PXDBInt(IsKey = true)]
		public override Int32? BranchID
		{
			get
			{
				return this._BranchID;
			}
			set
			{
				this._BranchID = value;
			}
		}
		#endregion
		#region AccountID
		public new abstract class accountID : PX.Data.BQL.BqlInt.Field<accountID> { }
		[PXDBInt(IsKey = true)]
		[PXDefault()]
		public override Int32? AccountID
		{
			get
			{
				return this._AccountID;
			}
			set
			{
				this._AccountID = value;
			}
		}
		#endregion
		#region SubID
		public new abstract class subID : PX.Data.BQL.BqlInt.Field<subID> { }
		[PXDBInt(IsKey = true)]
		[PXDefault()]
		public override Int32? SubID
		{
			get
			{
				return this._SubID;
			}
			set
			{
				this._SubID = value;
			}
		}
		#endregion
		#region VendorID
		public new abstract class vendorID : PX.Data.BQL.BqlInt.Field<vendorID> { }
		[PXDBInt(IsKey = true)]
		[PXDefault()]
		public override Int32? VendorID
		{
			get
			{
				return this._VendorID;
			}
			set
			{
				this._VendorID = value;
			}
		}
		#endregion
		#region FinPeriodID
		public new abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }
		[PXDBString(6, IsKey = true, IsFixed = true)]
		[PXDefault()]
		public override String FinPeriodID
		{
			get
			{
				return this._FinPeriodID;
			}
			set
			{
				this._FinPeriodID = value;
			}
		}
		#endregion
	}

	public class APHistory2 : APHistory
	{
		#region BranchID
		public new abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
		#endregion
		#region AccountID
		public new abstract class accountID : PX.Data.BQL.BqlInt.Field<accountID> { }
		#endregion
		#region SubID
		public new abstract class subID : PX.Data.BQL.BqlInt.Field<subID> { }
		#endregion
		#region VendorID
		public new abstract class vendorID : PX.Data.BQL.BqlInt.Field<vendorID> { }
		#endregion
		#region FinBegBalance
		public new abstract class finBegBalance : PX.Data.BQL.BqlDecimal.Field<finBegBalance> { }
		#endregion
		#region FinPtdPurchases
		public new abstract class finPtdPurchases : PX.Data.BQL.BqlDecimal.Field<finPtdPurchases> { }
		#endregion
		#region FinPtdPayments
		public new abstract class finPtdPayments : PX.Data.BQL.BqlDecimal.Field<finPtdPayments> { }
		#endregion
		#region FinPtdDrAdjustments
		public new abstract class finPtdDrAdjustments : PX.Data.BQL.BqlDecimal.Field<finPtdDrAdjustments> { }
		#endregion
		#region FinPtdCrAdjustments
		public new abstract class finPtdCrAdjustments : PX.Data.BQL.BqlDecimal.Field<finPtdCrAdjustments> { }
		#endregion
		#region FinPtdDiscTaken
		public new abstract class finPtdDiscTaken : PX.Data.BQL.BqlDecimal.Field<finPtdDiscTaken> { }
		#endregion
		#region FinPtdRGOL
		public new abstract class finPtdRGOL : PX.Data.BQL.BqlDecimal.Field<finPtdRGOL> { }
		#endregion
		#region FinYtdBalance
		public new abstract class finYtdBalance : PX.Data.BQL.BqlDecimal.Field<finYtdBalance> { }
		#endregion
		#region TranBegBalance
		public new abstract class tranBegBalance : PX.Data.BQL.BqlDecimal.Field<tranBegBalance> { }
		#endregion
		#region TranPtdPurchases
		public new abstract class tranPtdPurchases : PX.Data.BQL.BqlDecimal.Field<tranPtdPurchases> { }
		#endregion
		#region TranPtdPayments
		public new abstract class tranPtdPayments : PX.Data.BQL.BqlDecimal.Field<tranPtdPayments> { }
		#endregion
		#region TranPtdDrAdjustments
		public new abstract class tranPtdDrAdjustments : PX.Data.BQL.BqlDecimal.Field<tranPtdDrAdjustments> { }
		#endregion
		#region TranPtdCrAdjustments
		public new abstract class tranPtdCrAdjustments : PX.Data.BQL.BqlDecimal.Field<tranPtdCrAdjustments> { }
		#endregion
		#region TranPtdDiscTaken
		public new abstract class tranPtdDiscTaken : PX.Data.BQL.BqlDecimal.Field<tranPtdDiscTaken> { }
		#endregion
		#region TranPtdRGOL
		public new abstract class tranPtdRGOL : PX.Data.BQL.BqlDecimal.Field<tranPtdRGOL> { }
		#endregion
		#region TranYtdBalance
		public new abstract class tranYtdBalance : PX.Data.BQL.BqlDecimal.Field<tranYtdBalance> { }
		#endregion
		#region tstamp
		public new abstract class tstamp : IBqlField { }
		#endregion
		#region TranPtdDeposits
		public new abstract class tranPtdDeposits : PX.Data.BQL.BqlDecimal.Field<tranPtdDeposits> { }
		#endregion
		#region TranYtdDeposits
		public new abstract class tranYtdDeposits : PX.Data.BQL.BqlDecimal.Field<tranYtdDeposits> { }
		#endregion
		#region FinPtdDeposits
		public new abstract class finPtdDeposits : PX.Data.BQL.BqlDecimal.Field<finPtdDeposits> { }
		#endregion
		#region FinYtdDeposits
		public new abstract class finYtdDeposits : PX.Data.BQL.BqlDecimal.Field<finYtdDeposits> { }
		#endregion
		#region DetDeleted
		public new abstract class detDeleted : PX.Data.BQL.BqlBool.Field<detDeleted> { }
		#endregion
		#region FinPeriodID
		public new abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }
		#endregion
		#region FinPtdWhTax
		public new abstract class finPtdWhTax : PX.Data.BQL.BqlDecimal.Field<finPtdWhTax> { }
		#endregion
		#region TranPtdWhTax
		public new abstract class tranPtdWhTax : PX.Data.BQL.BqlDecimal.Field<tranPtdWhTax> { }
		#endregion
		#region FinPtdRetainageWithheld
		public new abstract class finPtdRetainageWithheld : PX.Data.BQL.BqlDecimal.Field<finPtdRetainageWithheld> { }
		#endregion
		#region TranPtdRetainageWithheld
		public new abstract class tranPtdRetainageWithheld : PX.Data.BQL.BqlDecimal.Field<tranPtdRetainageWithheld> { }
		#endregion
		#region FinYtdRetainageWithheld
		public new abstract class finYtdRetainageWithheld : PX.Data.BQL.BqlDecimal.Field<finYtdRetainageWithheld> { }
		#endregion
		#region TranYtdRetainageWithheld
		public new abstract class tranYtdRetainageWithheld : PX.Data.BQL.BqlDecimal.Field<tranYtdRetainageWithheld> { }
		#endregion
		#region FinPtdRetainageReleased
		public new abstract class finPtdRetainageReleased : PX.Data.BQL.BqlDecimal.Field<finPtdRetainageReleased> { }
		#endregion
		#region TranPtdRetainageReleased
		public new abstract class tranPtdRetainageReleased : PX.Data.BQL.BqlDecimal.Field<tranPtdRetainageReleased> { }
		#endregion
		#region FinYtdRetainageReleased
		public new abstract class finYtdRetainageReleased : PX.Data.BQL.BqlDecimal.Field<finYtdRetainageReleased> { }
		#endregion
		#region TranYtdRetainageReleased
		public new abstract class tranYtdRetainageReleased : PX.Data.BQL.BqlDecimal.Field<tranYtdRetainageReleased> { }
		#endregion
		#region FinPtdRevalued
		public new abstract class finPtdRevalued : PX.Data.BQL.BqlDecimal.Field<finPtdRevalued> { }
		#endregion
	}

	public class CuryAPHistory2 : CuryAPHistory
	{
		#region BranchID
		public new abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
		#endregion
		#region AccountID
		public new abstract class accountID : PX.Data.BQL.BqlInt.Field<accountID> { }
		#endregion
		#region SubID
		public new abstract class subID : PX.Data.BQL.BqlInt.Field<subID> { }
		#endregion
		#region VendorID
		public new abstract class vendorID : PX.Data.BQL.BqlInt.Field<vendorID> { }
		#endregion
		#region CuryID
		public new abstract class curyID : IBqlField { }
		#endregion
		#region FinBegBalance
		public new abstract class finBegBalance : PX.Data.BQL.BqlDecimal.Field<finBegBalance> { }
		#endregion
		#region FinPtdPurchases
		public new abstract class finPtdPurchases : PX.Data.BQL.BqlDecimal.Field<finPtdPurchases> { }
		#endregion
		#region FinPtdPayments
		public new abstract class finPtdPayments : PX.Data.BQL.BqlDecimal.Field<finPtdPayments> { }
		#endregion
		#region FinPtdDrAdjustments
		public new abstract class finPtdDrAdjustments : PX.Data.BQL.BqlDecimal.Field<finPtdDrAdjustments> { }
		#endregion
		#region FinPtdCrAdjustments
		public new abstract class finPtdCrAdjustments : PX.Data.BQL.BqlDecimal.Field<finPtdCrAdjustments> { }
		#endregion
		#region FinPtdDiscTaken
		public new abstract class finPtdDiscTaken : PX.Data.BQL.BqlDecimal.Field<finPtdDiscTaken> { }
		#endregion
		#region FinPtdRGOL
		public new abstract class finPtdRGOL : PX.Data.BQL.BqlDecimal.Field<finPtdRGOL> { }
		#endregion
		#region FinYtdBalance
		public new abstract class finYtdBalance : PX.Data.BQL.BqlDecimal.Field<finYtdBalance> { }
		#endregion
		#region FinPtdDeposits
		public new abstract class finPtdDeposits : PX.Data.BQL.BqlDecimal.Field<finPtdDeposits> { }
		#endregion
		#region FinYtdDeposits
		public new abstract class finYtdDeposits : PX.Data.BQL.BqlDecimal.Field<finYtdDeposits> { }
		#endregion
		#region TranBegBalance
		public new abstract class tranBegBalance : PX.Data.BQL.BqlDecimal.Field<tranBegBalance> { }
		#endregion
		#region TranPtdPurchases
		public new abstract class tranPtdPurchases : PX.Data.BQL.BqlDecimal.Field<tranPtdPurchases> { }
		#endregion
		#region TranPtdPayments
		public new abstract class tranPtdPayments : PX.Data.BQL.BqlDecimal.Field<tranPtdPayments> { }
		#endregion
		#region TranPtdDrAdjustments
		public new abstract class tranPtdDrAdjustments : PX.Data.BQL.BqlDecimal.Field<tranPtdDrAdjustments> { }
		#endregion
		#region TranPtdCrAdjustments
		public new abstract class tranPtdCrAdjustments : PX.Data.BQL.BqlDecimal.Field<tranPtdCrAdjustments> { }
		#endregion
		#region TranPtdDiscTaken
		public new abstract class tranPtdDiscTaken : PX.Data.BQL.BqlDecimal.Field<tranPtdDiscTaken> { }
		#endregion
		#region TranPtdRGOL
		public new abstract class tranPtdRGOL : PX.Data.BQL.BqlDecimal.Field<tranPtdRGOL> { }
		#endregion
		#region TranYtdBalance
		public new abstract class tranYtdBalance : PX.Data.BQL.BqlDecimal.Field<tranYtdBalance> { }
		#endregion
		#region TranPtdDeposits
		public new abstract class tranPtdDeposits : PX.Data.BQL.BqlDecimal.Field<tranPtdDeposits> { }
		#endregion
		#region TranYtdDeposits
		public new abstract class tranYtdDeposits : PX.Data.BQL.BqlDecimal.Field<tranYtdDeposits> { }
		#endregion
		#region CuryFinBegBalance
		public new abstract class curyFinBegBalance : PX.Data.BQL.BqlDecimal.Field<curyFinBegBalance> { }
		#endregion
		#region CuryFinPtdPurchases
		public new abstract class curyFinPtdPurchases : PX.Data.BQL.BqlDecimal.Field<curyFinPtdPurchases> { }
		#endregion
		#region CuryFinPtdPayments
		public new abstract class curyFinPtdPayments : PX.Data.BQL.BqlDecimal.Field<curyFinPtdPayments> { }
		#endregion
		#region CuryFinPtdDrAdjustments
		public new abstract class curyFinPtdDrAdjustments : PX.Data.BQL.BqlDecimal.Field<curyFinPtdDrAdjustments> { }
		#endregion
		#region CuryFinPtdCrAdjustments
		public new abstract class curyFinPtdCrAdjustments : PX.Data.BQL.BqlDecimal.Field<curyFinPtdCrAdjustments> { }
		#endregion
		#region CuryFinPtdDiscTaken
		public new abstract class curyFinPtdDiscTaken : PX.Data.BQL.BqlDecimal.Field<curyFinPtdDiscTaken> { }
		#endregion
		#region CuryFinYtdBalance
		public new abstract class curyFinYtdBalance : PX.Data.BQL.BqlDecimal.Field<curyFinYtdBalance> { }
		#endregion
		#region CuryFinPtdDeposits
		public new abstract class curyFinPtdDeposits : PX.Data.BQL.BqlDecimal.Field<curyFinPtdDeposits> { }
		#endregion
		#region CuryFinYtdDeposits
		public new abstract class curyFinYtdDeposits : PX.Data.BQL.BqlDecimal.Field<curyFinYtdDeposits> { }
		#endregion
		#region CuryTranBegBalance
		public new abstract class curyTranBegBalance : PX.Data.BQL.BqlDecimal.Field<curyTranBegBalance> { }
		#endregion
		#region CuryTranPtdPurchases
		public new abstract class curyTranPtdPurchases : PX.Data.BQL.BqlDecimal.Field<curyTranPtdPurchases> { }
		#endregion
		#region CuryTranPtdPayments
		public new abstract class curyTranPtdPayments : PX.Data.BQL.BqlDecimal.Field<curyTranPtdPayments> { }
		#endregion
		#region CuryTranPtdDrAdjustments
		public new abstract class curyTranPtdDrAdjustments : PX.Data.BQL.BqlDecimal.Field<curyTranPtdDrAdjustments> { }
		#endregion
		#region CuryTranPtdCrAdjustments
		public new abstract class curyTranPtdCrAdjustments : PX.Data.BQL.BqlDecimal.Field<curyTranPtdCrAdjustments> { }
		#endregion
		#region CuryTranPtdDiscTaken
		public new abstract class curyTranPtdDiscTaken : PX.Data.BQL.BqlDecimal.Field<curyTranPtdDiscTaken> { }
		#endregion
		#region CuryTranYtdBalance
		public new abstract class curyTranYtdBalance : PX.Data.BQL.BqlDecimal.Field<curyTranYtdBalance> { }
		#endregion
		#region CuryTranPtdDeposits
		public new abstract class curyTranPtdDeposits : PX.Data.BQL.BqlDecimal.Field<curyTranPtdDeposits> { }
		#endregion
		#region CuryTranYtdDeposits
		public new abstract class curyTranYtdDeposits : PX.Data.BQL.BqlDecimal.Field<curyTranYtdDeposits> { }
		#endregion
		#region tstamp
		public new abstract class tstamp : IBqlField { }
		#endregion
		#region DetDeleted
		public new abstract class detDeleted : PX.Data.BQL.BqlBool.Field<detDeleted> { }
		#endregion
		#region FinPeriodID
		public new abstract class finPeriodID : PX.Data.BQL.BqlString.Field<finPeriodID> { }
		#endregion
		#region CuryFinPtdWhTax
		public new abstract class curyFinPtdWhTax : PX.Data.BQL.BqlDecimal.Field<curyFinPtdWhTax> { }
		#endregion
		#region CuryTranPtdWhTax
		public new abstract class curyTranPtdWhTax : PX.Data.BQL.BqlDecimal.Field<curyTranPtdWhTax> { }
		#endregion
		#region FinPtdWhTax
		public new abstract class finPtdWhTax : PX.Data.BQL.BqlDecimal.Field<finPtdWhTax> { }
		#endregion
		#region TranPtdWhTax
		public new abstract class tranPtdWhTax : PX.Data.BQL.BqlDecimal.Field<tranPtdWhTax> { }
		#endregion
		#region CuryFinPtdRetainageWithheld
		public new abstract class curyFinPtdRetainageWithheld : PX.Data.BQL.BqlDecimal.Field<curyFinPtdRetainageWithheld> { }
		#endregion
		#region FinPtdRetainageWithheld
		public new abstract class finPtdRetainageWithheld : PX.Data.BQL.BqlDecimal.Field<finPtdRetainageWithheld> { }
		#endregion
		#region CuryTranPtdRetainageWithheld
		public new abstract class curyTranPtdRetainageWithheld : PX.Data.BQL.BqlDecimal.Field<curyTranPtdRetainageWithheld> { }
		#endregion
		#region TranPtdRetainageWithheld
		public new abstract class tranPtdRetainageWithheld : PX.Data.BQL.BqlDecimal.Field<tranPtdRetainageWithheld> { }
		#endregion
		#region CuryFinYtdRetainageWithheld
		public new abstract class curyFinYtdRetainageWithheld : PX.Data.BQL.BqlDecimal.Field<curyFinYtdRetainageWithheld> { }
		#endregion
		#region FinYtdRetainageWithheld
		public new abstract class finYtdRetainageWithheld : PX.Data.BQL.BqlDecimal.Field<finYtdRetainageWithheld> { }
		#endregion
		#region CuryTranYtdRetainageWithheld
		public new abstract class curyTranYtdRetainageWithheld : PX.Data.BQL.BqlDecimal.Field<curyTranYtdRetainageWithheld> { }
		#endregion
		#region TranYtdRetainageWithheld
		public new abstract class tranYtdRetainageWithheld : PX.Data.BQL.BqlDecimal.Field<tranYtdRetainageWithheld> { }
		#endregion
		#region CuryFinPtdRetainageReleased
		public new abstract class curyFinPtdRetainageReleased : PX.Data.BQL.BqlDecimal.Field<curyFinPtdRetainageReleased> { }
		#endregion
		#region FinPtdRetainageReleased
		public new abstract class finPtdRetainageReleased : PX.Data.BQL.BqlDecimal.Field<finPtdRetainageReleased> { }
		#endregion
		#region CuryTranPtdRetainageReleased
		public new abstract class curyTranPtdRetainageReleased : PX.Data.BQL.BqlDecimal.Field<curyTranPtdRetainageReleased> { }
		#endregion
		#region TranPtdRetainageReleased
		public new abstract class tranPtdRetainageReleased : PX.Data.BQL.BqlDecimal.Field<tranPtdRetainageReleased> { }
		#endregion
		#region CuryFinYtdRetainageReleased
		public new abstract class curyFinYtdRetainageReleased : PX.Data.BQL.BqlDecimal.Field<curyFinYtdRetainageReleased> { }
		#endregion
		#region FinYtdRetainageReleased
		public new abstract class finYtdRetainageReleased : PX.Data.BQL.BqlDecimal.Field<finYtdRetainageReleased> { }
		#endregion
		#region CuryTranYtdRetainageReleased
		public new abstract class curyTranYtdRetainageReleased : PX.Data.BQL.BqlDecimal.Field<curyTranYtdRetainageReleased> { }
		#endregion
		#region TranYtdRetainageReleased
		public new abstract class tranYtdRetainageReleased : PX.Data.BQL.BqlDecimal.Field<tranYtdRetainageReleased> { }
		#endregion
		#region FinPtdRevalued
		public new abstract class finPtdRevalued : PX.Data.BQL.BqlDecimal.Field<finPtdRevalued> { }
		#endregion
	}
}
