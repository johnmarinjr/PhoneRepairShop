using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.WorkflowAPI;
using PX.Objects.AR.CCPaymentProcessing;
using PX.Objects.AR.CCPaymentProcessing.Common;
using PX.Objects.AR.CCPaymentProcessing.Helpers;
using PX.Objects.AR.CCPaymentProcessing.Interfaces;
using PX.Objects.AR.CCPaymentProcessing.Repositories;
using PX.Objects.AR.Repositories;
using PX.Objects.AR.Standalone;
using PX.Objects.CA;
using PX.Objects.CM.Extensions;
using PX.Objects.Common;
using PX.Objects.Common.Bql;
using PX.Objects.Common.Extensions;
using PX.Objects.Common.Utility;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.EP;
using PX.Objects.Extensions.MultiCurrency;
using PX.Objects.Extensions.MultiCurrency.AR;
using PX.Objects.Extensions.PaymentTransaction;
using PX.Objects.GL;
using PX.Objects.GL.Attributes;
using PX.Objects.GL.FinPeriods;
using PX.Objects.GL.FinPeriods.TableDefinition;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using SOAdjust = PX.Objects.SO.SOAdjust;
using SOOrder = PX.Objects.SO.SOOrder;
using SOOrderEntry = PX.Objects.SO.SOOrderEntry;
using SOOrderStatus = PX.Objects.SO.SOOrderStatus;
using SOOrderType = PX.Objects.SO.SOOrderType;
using OrdersToApplyTab = PX.Objects.SO.GraphExtensions.ARPaymentEntryExt.OrdersToApplyTab;

namespace PX.Objects.AR
{
	[Serializable]
	public partial class ARPaymentEntry : ARDataEntryGraph<ARPaymentEntry, ARPayment>
	    {
		#region Entity Event Handlers
		public PXWorkflowEventHandler<ARPayment> OnReleaseDocument;
		public PXWorkflowEventHandler<ARPayment> OnOpenDocument;
		public PXWorkflowEventHandler<ARPayment> OnCloseDocument;
		public PXWorkflowEventHandler<ARPayment> OnVoidDocument;
		public PXWorkflowEventHandler<ARPayment> OnUpdateStatus;
		#endregion

        #region Internal Type Definition
		[Serializable]
        [PXHidden]
		public partial class LoadOptions : IBqlTable
		{
			#region OrganizationID
			public abstract class organizationID : PX.Data.BQL.BqlInt.Field<organizationID> { }

			[Organization(true, typeof(Null),
				Required = false,
				PersistingCheck = PXPersistingCheck.Nothing)]
			public int? OrganizationID { get; set; }
			#endregion
			#region BranchID
			public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }

			[BranchOfOrganization(typeof(organizationID), true, true,
				sourceType: typeof(Null),
				Required = false,
				PersistingCheck = PXPersistingCheck.Nothing)]
			public int? BranchID { get; set; }
			#endregion

			#region OrgBAccountID
			public abstract class orgBAccountID : IBqlField { }

			[OrganizationTree(typeof(organizationID), typeof(branchID), onlyActive: true)]
			public int? OrgBAccountID { get; set; }
			#endregion

			#region FromDate
			public abstract class fromDate : PX.Data.BQL.BqlDateTime.Field<fromDate> { }
			protected DateTime? _FromDate;
			[PXDBDate]
			[PXUIField(DisplayName = "From Date")]
			public virtual DateTime? FromDate
			{
				get
				{
					return _FromDate;
				}
				set
				{
					_FromDate = value;
				}
			}
			#endregion
			#region TillDate
			public abstract class tillDate : PX.Data.BQL.BqlDateTime.Field<tillDate> { }
			protected DateTime? _TillDate;
			[PXDBDate]
			[PXUIField(DisplayName = "To Date")]
			[PXDefault(typeof(ARPayment.adjDate), PersistingCheck = PXPersistingCheck.Nothing)]
			public virtual DateTime? TillDate
			{
				get
				{
					return _TillDate;
				}
				set
				{
					_TillDate = value;
				}
			}
			#endregion
			#region StartRefNbr
			public abstract class startRefNbr : PX.Data.BQL.BqlString.Field<startRefNbr> { }
			protected string _StartRefNbr;
			[PXDBString(15, IsUnicode = true)]
			[PXUIField(DisplayName = "From Ref. Nbr.")]
			[PXSelector(typeof(Search2<ARInvoice.refNbr,
				InnerJoin<Customer, On<Customer.bAccountID, Equal<ARInvoice.customerID>>,
				LeftJoin<ARAdjust, On<ARAdjust.adjdDocType, Equal<ARInvoice.docType>,
					And<ARAdjust.adjdRefNbr, Equal<ARInvoice.refNbr>,
					And<ARAdjust.released, NotEqual<True>,
					And<ARAdjust.voided, NotEqual<True>,
					And<Where<ARAdjust.adjgDocType, NotEqual<Current<ARRegister.docType>>,
						Or<ARAdjust.adjgRefNbr, NotEqual<Current<ARRegister.refNbr>>>>>>>>>,
				LeftJoin<ARAdjust2, On<ARAdjust2.adjgDocType, Equal<ARInvoice.docType>,
					And<ARAdjust2.adjgRefNbr, Equal<ARInvoice.refNbr>,
					And<ARAdjust2.released, NotEqual<True>,
					And<ARAdjust2.voided, NotEqual<True>>>>>>>>,
				Where<ARInvoice.released, Equal<True>,
					And<ARInvoice.openDoc, Equal<True>,
					And<ARInvoice.hold, Equal<False>,
					And<ARAdjust.adjgRefNbr, IsNull,
					And<ARAdjust2.adjdRefNbr, IsNull,
					And<ARInvoice.docDate, LessEqual<Current<ARPayment.adjDate>>,
					And<ARInvoice.tranPeriodID, LessEqual<Current<ARPayment.adjTranPeriodID>>,
					And<ARInvoice.pendingPPD, NotEqual<True>,
					And<Where<ARInvoice.customerID, Equal<Optional<ARPayment.customerID>>,
						Or<Customer.consolidatingBAccountID, Equal<Optional<ARRegister.customerID>>>>>>>>>>>>>>))]
			public virtual string StartRefNbr
			{
				get
				{
					return _StartRefNbr;
				}
				set
				{
					_StartRefNbr = value;
				}
			}
			#endregion
			#region EndRefNbr
			public abstract class endRefNbr : PX.Data.BQL.BqlString.Field<endRefNbr> { }
			protected string _EndRefNbr;
			[PXDBString(15, IsUnicode = true)]
			[PXUIField(DisplayName = "To Ref. Nbr.")]
			[PXSelector(typeof(Search2<ARInvoice.refNbr,
				InnerJoin<Customer, On<Customer.bAccountID, Equal<ARInvoice.customerID>>,
				LeftJoin<ARAdjust, On<ARAdjust.adjdDocType, Equal<ARInvoice.docType>,
					And<ARAdjust.adjdRefNbr, Equal<ARInvoice.refNbr>,
					And<ARAdjust.released, NotEqual<True>,
					And<ARAdjust.voided, NotEqual<True>,
					And<Where<ARAdjust.adjgDocType, NotEqual<Current<ARRegister.docType>>,
						Or<ARAdjust.adjgRefNbr, NotEqual<Current<ARRegister.refNbr>>>>>>>>>,
				LeftJoin<ARAdjust2, On<ARAdjust2.adjgDocType, Equal<ARInvoice.docType>,
					And<ARAdjust2.adjgRefNbr, Equal<ARInvoice.refNbr>,
					And<ARAdjust2.released, NotEqual<True>,
					And<ARAdjust2.voided, NotEqual<True>>>>>>>>,
				Where<ARInvoice.released, Equal<True>,
					And<ARInvoice.openDoc, Equal<True>,
					And<ARInvoice.hold, Equal<False>,
					And<ARAdjust.adjgRefNbr, IsNull,
					And<ARAdjust2.adjdRefNbr, IsNull,
					And<ARInvoice.docDate, LessEqual<Current<ARPayment.adjDate>>,
					And<ARInvoice.tranPeriodID, LessEqual<Current<ARPayment.adjTranPeriodID>>,
					And<ARInvoice.pendingPPD, NotEqual<True>,
					And<Where<ARInvoice.customerID, Equal<Optional<ARPayment.customerID>>,
						Or<Customer.consolidatingBAccountID, Equal<Optional<ARRegister.customerID>>>>>>>>>>>>>>))]
			public virtual string EndRefNbr
			{
				get
				{
					return _EndRefNbr;
				}
				set
				{
					_EndRefNbr = value;
				}
			}
			#endregion
			#region StartOrderNbr
			public abstract class startOrderNbr : PX.Data.BQL.BqlString.Field<startOrderNbr> { }
			protected String _StartOrderNbr;
			[PXDBString(15, IsUnicode = true)]
			[PXUIField(DisplayName = "Start Order Nbr.")]
			[PXSelector(typeof(Search2<SOOrder.orderNbr, LeftJoin<SOOrderType, On<SOOrderType.orderType, Equal<SOOrder.orderType>>>,
					 Where<SOOrder.customerID, Equal<Optional<ARPayment.customerID>>,
					   And<SOOrder.openDoc, Equal<True>,					   
					   And<Where<SOOrderType.aRDocType, Equal<ARDocType.invoice>, Or<SOOrderType.aRDocType, Equal<ARDocType.debitMemo>>>>>>>))]
			public virtual String StartOrderNbr
			{
				get
				{
					return _StartOrderNbr;
				}
				set
				{
					_StartOrderNbr = value;
				}
			}
			#endregion
			#region EndOrderNbr
			public abstract class endOrderNbr : PX.Data.BQL.BqlString.Field<endOrderNbr> { }
			protected String _EndOrderNbr;
			[PXDBString(15, IsUnicode = true)]
			[PXUIField(DisplayName = "End Order Nbr.")]
			[PXSelector(typeof(Search2<SOOrder.orderNbr, LeftJoin<SOOrderType, On<SOOrderType.orderType, Equal<SOOrder.orderType>>>,
					 Where<SOOrder.customerID, Equal<Optional<ARPayment.customerID>>,
					   And<SOOrder.openDoc, Equal<True>,					   
					   And<Where<SOOrderType.aRDocType, Equal<ARDocType.invoice>, Or<SOOrderType.aRDocType, Equal<ARDocType.debitMemo>>>>>>>))]
			public virtual String EndOrderNbr
			{
				get
				{
					return _EndOrderNbr;
				}
				set
				{
					_EndOrderNbr = value;
				}
			}
			#endregion
			#region MaxDocs
			public abstract class maxDocs : PX.Data.BQL.BqlInt.Field<maxDocs> { }
			protected int? _MaxDocs;
			[PXDBInt(MinValue = 0)]
			[PXUIField(DisplayName = "Max. Number of Rows")]
			[PXDefault(100, PersistingCheck = PXPersistingCheck.Nothing)]
			public virtual int? MaxDocs
			{
				get
				{
					return _MaxDocs;
				}
				set
				{
					_MaxDocs = value;
				}
			}

			#endregion
			#region LoadChildDocuments
			public abstract class loadChildDocuments : PX.Data.BQL.BqlString.Field<loadChildDocuments>
			{
				public const string None = "NOONE";
				public const string ExcludeCRM = "EXCRM";
				public const string IncludeCRM = "INCRM";

				public class none : PX.Data.BQL.BqlString.Constant<none> { public none() : base(None) { } }
				public class excludeCRM : PX.Data.BQL.BqlString.Constant<excludeCRM> { public excludeCRM() : base(ExcludeCRM) { } }
				public class includeCRM : PX.Data.BQL.BqlString.Constant<includeCRM> { public includeCRM() : base(IncludeCRM) { } }

				public class ListAttribute : PXStringListAttribute
				{
					public ListAttribute() : base(
						new[] { None, ExcludeCRM, IncludeCRM },
						new[] { Messages.None, Messages.ExcludeCRM, Messages.IncludeCRM })
					{ }
				}

				public class NoCRMListAttribute : PXStringListAttribute
				{
					public NoCRMListAttribute()
						: base(
							new[] { None, ExcludeCRM },
							new[] { Messages.None, Messages.ExcludeCRM })
					{ }
				}
			}

			[PXDBString(5, IsFixed = true)]
			[PXUIField(DisplayName = "Include Child Documents")]
			[loadChildDocuments.List]
			[PXDefault(loadChildDocuments.None)]
			public virtual string LoadChildDocuments { get; set; }
			#endregion
			#region OrderBy
			public abstract class orderBy : PX.Data.BQL.BqlString.Field<orderBy>
			{
				#region List
				public class ListAttribute : PXStringListAttribute
				{
					public ListAttribute()
						: base(
						new[] { DueDateRefNbr, DocDateRefNbr, RefNbr },
						new[] { Messages.DueDateRefNbr, Messages.DocDateRefNbr, Messages.RefNbr })
					{ }
				}

				public const string DueDateRefNbr = "DUE";
				public const string DocDateRefNbr = "DOC";
				public const string RefNbr = "REF";
				#endregion
			}
			protected string _OrderBy;
			[PXDBString(3, IsFixed = true)]
			[PXUIField(DisplayName = "Sort Order")]
			[orderBy.List]
			[PXDefault(orderBy.DueDateRefNbr)]
			public virtual string OrderBy
			{
				get
				{
					return _OrderBy;
				}
				set
				{
					_OrderBy = value;
				}
			}
			#endregion
			#region LoadingMode
			public abstract class loadingMode : PX.Data.BQL.BqlString.Field<loadingMode>
			{
				#region List
				public class ListAttribute : PXStringListAttribute
				{
					public ListAttribute() : base(
						 new[] { Load, Reload },
						 new[] { Messages.Load, Messages.Reload })
					{ }
				}

				public const string Load = "L";
				public const string Reload = "R";
				#endregion
			}
			[PXDBString(1, IsFixed = true)]
			[PXUIField(DisplayName = "Loading mode")]
			[loadingMode.List]
			[PXDefault(loadingMode.Load)]
			public virtual string LoadingMode { get; set; }
			#endregion
			#region LoadAndApply
			public abstract class apply : PX.Data.BQL.BqlBool.Field<apply> { }
			[PXDBBool()]
			[PXDefault(true)]
			[PXUIField(DisplayName = "Automatically Apply Amount Paid")]
			public virtual bool? Apply { get; set; }
			#endregion

			#region SOOrderBy
			public abstract class sOOrderBy : PX.Data.BQL.BqlString.Field<sOOrderBy>
			{
				#region List
				public class ListAttribute : PXStringListAttribute
				{
					public ListAttribute()
						: base(
						new[] { OrderDateOrderNbr, OrderNbr },
						new[] { Messages.OrderDateOrderNbr, Messages.OrderNbr })
					{ }
				}

				public const string OrderDateOrderNbr = "DAT";
				public const string OrderNbr = "ORD";
				#endregion
			}
			protected string _SOOrderBy;
			[PXDBString(3, IsFixed = true)]
			[PXUIField(DisplayName = "Order by")]
			[sOOrderBy.List]
			[PXDefault(sOOrderBy.OrderDateOrderNbr)]
			public virtual string SOOrderBy
			{
				get
				{
					return _SOOrderBy;
				}
				set
				{
					_SOOrderBy = value;
				}
			}
			#endregion
			#region IsInvoice
			public abstract class isInvoice : PX.Data.BQL.BqlBool.Field<isInvoice> { }
			protected bool? _IsInvoice;
			[PXDBBool]
			public virtual bool? IsInvoice
			{
				get
				{
					return _IsInvoice;
				}
				set
				{
					_IsInvoice = value;
				}
			}
			#endregion
		}

		public class MultiCurrency : ARMultiCurrencyGraph<ARPaymentEntry, ARPayment>
		{
			protected override string DocumentStatus => Base.Document.Current?.Status;

			protected override CurySourceMapping GetCurySourceMapping()
			{
				return new CurySourceMapping(typeof(CashAccount))
				{
					CuryID = typeof(CashAccount.curyID),
					CuryRateTypeID = typeof(CashAccount.curyRateTypeID)
				};
			}

			protected override CurySource CurrentSourceSelect()
			{
				CurySource CurySource = base.CurrentSourceSelect();
				if (CurySource != null) CurySource.AllowOverrideRate = Base.customer?.Current?.AllowOverrideRate;
				return CurySource;
			}

			protected override DocumentMapping GetDocumentMapping()
			{
				return new DocumentMapping(typeof(ARPayment))
				{
					DocumentDate = typeof(ARPayment.adjDate),
					BAccountID = typeof(ARPayment.customerID)
				};
			}

			protected override PXSelectBase[] GetChildren()
			{
				return new PXSelectBase[]
				{
					Base.Document,
					Base.Adjustments,
					Base.Adjustments_Balance,
					Base.Adjustments_History,
					Base.Adjustments_Invoices,
					Base.Adjustments_Payments,
					Base.dummy_CATran,
					Base.PaymentCharges,
					Base.ARPost
				};
			}

			protected override bool ShouldBeDisabledDueToDocStatus()
			{
				switch (Base.Document.Current?.DocType)
				{
					case ARDocType.VoidPayment:
					case ARDocType.VoidRefund:
						return true;
					default:
						return base.ShouldBeDisabledDueToDocStatus();
				}
			}

			protected virtual void _(Events.FieldUpdated<ARPayment, ARPayment.cashAccountID> e)
			{
				if (Base._IsVoidCheckInProgress || !PXAccess.FeatureInstalled<FeaturesSet.multicurrency>()) return;
				else SourceFieldUpdated<ARPayment.curyInfoID, ARPayment.curyID, ARPayment.adjDate>(e.Cache, e.Row);
			}

			protected override void _(Events.FieldUpdated<Document, Document.documentDate> e)
			{
				base._(e);
				if (ShouldBeDisabledDueToDocStatus() || Base.HasUnreleasedSOInvoice) return;
				else Base.LoadInvoicesExtProc(true, null);
			}

			protected virtual void _(Events.FieldSelecting<ARAdjust, ARAdjust.adjdCuryID> e)
			{
				e.ReturnValue = CuryIDFieldSelecting<ARAdjust.adjdCuryInfoID>(e.Cache, e.Row);
			}
		}


		#endregion

		#region PXSelect views
		/// <summary>
		/// Necessary for proper cache resolution inside selector
		/// on <see cref="ARAdjust.DisplayRefNbr"/>.
		/// </summary>
		public PXSelect<Standalone.ARRegister> dummy_register;

		/// <summary>
		/// Necessary for proper cache resolution inside selector (Compliance Tab)
		/// </summary>
		public PXSelect<AP.Vendor> dummyVendor;

		[PXViewName(Messages.ARPayment)]
		[PXCopyPasteHiddenFields(typeof(ARPayment.extRefNbr), typeof(ARPayment.clearDate), typeof(ARPayment.cleared), 
			typeof(ARPayment.cCTransactionRefund), typeof(ARPayment.refTranExtNbr), typeof(ARPayment.pMInstanceID))]
		public PXSelectJoin<
			ARPayment,
			LeftJoinSingleTable<Customer,
				On<Customer.bAccountID, Equal<ARPayment.customerID>>>,
			Where<
				ARPayment.docType, Equal<Optional<ARPayment.docType>>,
				And<Where<
					Customer.bAccountID, IsNull,
					Or<Match<Customer, Current<AccessInfo.userName>>>>>>>
			Document;

		public PXSelect<ARPayment,
			Where<ARPayment.docType, Equal<Current<ARPayment.docType>>,
				And<ARPayment.refNbr, Equal<Current<ARPayment.refNbr>>>>> CurrentDocument;

		[PXViewName(Messages.DocumentsToApply)]
		[PXCopyPasteHiddenView]
		public PXSelectJoin<ARAdjust,
			LeftJoin<ARInvoice, On<ARInvoice.docType, Equal<ARAdjust.adjdDocType>,
				And<ARInvoice.refNbr, Equal<ARAdjust.adjdRefNbr>>>,
			InnerJoin<Standalone.ARRegisterAlias,
				On<Standalone.ARRegisterAlias.docType, Equal<ARAdjust.adjdDocType>,
				And<Standalone.ARRegisterAlias.refNbr, Equal<ARAdjust.adjdRefNbr>>>,
			LeftJoin<ARTran, On<ARInvoice.paymentsByLinesAllowed, Equal<True>,
				And<ARTran.tranType, Equal<ARAdjust.adjdDocType>,
				And<ARTran.refNbr, Equal<ARAdjust.adjdRefNbr>,
				And<ARTran.lineNbr, Equal<ARAdjust.adjdLineNbr>>>>>>>>,
			Where<ARAdjust.adjgDocType, Equal<Current<ARPayment.docType>>,
				And<ARAdjust.adjgRefNbr, Equal<Current<ARPayment.refNbr>>,
				And<ARAdjust.released, NotEqual<True>>>>> Adjustments;

		[PXCopyPasteHiddenView] 
		public PXSelectJoin<Standalone.ARAdjust,
					LeftJoin<ARInvoice, On<ARInvoice.docType, Equal<Standalone.ARAdjust.adjdDocType>,
						And<ARInvoice.refNbr, Equal<Standalone.ARAdjust.adjdRefNbr>>>,
					InnerJoin<Standalone.ARRegisterAlias,
						On<Standalone.ARRegisterAlias.docType, Equal<Standalone.ARAdjust.adjdDocType>, 
							And<Standalone.ARRegisterAlias.refNbr, Equal<Standalone.ARAdjust.adjdRefNbr>>>,
					LeftJoin<ARTran,
						On<ARTran.tranType, Equal<Standalone.ARAdjust.adjdDocType>, 
						And<ARTran.refNbr, Equal<Standalone.ARAdjust.adjdRefNbr>,
						And<ARTran.lineNbr, Equal<Standalone.ARAdjust.adjdLineNbr>>>>, 
					LeftJoin<CurrencyInfo,
						On<CurrencyInfo.curyInfoID, Equal<ARAdjust.adjdCuryInfoID>>>>>>,
				Where<Standalone.ARAdjust.adjgDocType, Equal<Current<ARPayment.docType>>,
					And<Standalone.ARAdjust.adjgRefNbr, Equal<Current<ARPayment.refNbr>>,
					And<ARAdjust.released, Equal<Required<ARAdjust.released>>>>>>
				Adjustments_Balance;

		public PXSelect<ARAdjust,
			Where<ARAdjust.adjgDocType, Equal<Optional<ARPayment.docType>>,
				And<ARAdjust.adjgRefNbr, Equal<Optional<ARPayment.refNbr>>,
				And<ARAdjust.released, NotEqual<True>>>>> Adjustments_Raw;

		
		public PXSelectJoin<
			ARAdjust,
				InnerJoinSingleTable<ARInvoice,
					On<ARInvoice.docType, Equal<ARAdjust.adjdDocType>,
					And<ARInvoice.refNbr, Equal<ARAdjust.adjdRefNbr>>>,
				InnerJoin<Standalone.ARRegisterAlias,
					On<Standalone.ARRegisterAlias.docType, Equal<ARAdjust.adjdDocType>,
					And<Standalone.ARRegisterAlias.refNbr, Equal<ARAdjust.adjdRefNbr>>>,
				LeftJoin<ARTran,
					On<ARTran.tranType, Equal<ARAdjust.adjdDocType>,
					And<ARTran.refNbr, Equal<ARAdjust.adjdRefNbr>,
					And<ARTran.lineNbr, Equal<ARAdjust.adjdLineNbr>>>>>>>,
			Where<
				ARAdjust.adjgDocType, Equal<Current<ARPayment.docType>>,
				And<ARAdjust.adjgRefNbr, Equal<Current<ARPayment.refNbr>>,
				And<ARAdjust.released, NotEqual<True>,
				And<Standalone.ARRegisterAlias.hold, NotEqual<True>>>>>>
			Adjustments_Invoices;

		private bool AppendAdjustmentsInvoicesTail(ARAdjust adj, PXResult<ARInvoice, CurrencyInfo, Customer, ARTran> invoicesResult)
		{
			PXResult<ARInvoice, ARRegisterAlias, ARTran> result = new PXResult<ARInvoice, ARRegisterAlias, ARTran>(
				invoicesResult,
				Common.Utilities.Clone<ARInvoice, ARRegisterAlias>(this, invoicesResult),
				invoicesResult
				);

			Adjustments_Invoices.StoreTailResult(
				result,
				new[] { adj },
				Document.Current.DocType, Document.Current.RefNbr
				);

			ARInvoice invoice = invoicesResult;
			ARInvoice_DocType_RefNbr.StoreResult(new List<object> { invoicesResult },
				PXQueryParameters.ExplicitParameters(adj.AdjdLineNbr ?? 0, invoice.DocType, invoice.RefNbr));
			ARInvoice_DocType_RefNbr.StoreTailResult(
				new PXResult<CurrencyInfo, Customer, ARTran>(invoicesResult, invoicesResult, invoicesResult),
				new object[] { invoice },
				adj.AdjdLineNbr ?? 0, invoice.DocType, invoice.RefNbr
				);
			return true;
		}

		private void Adjustments_Invoices_BeforeSelect()
		{
			if (Adjustments_Raw.Cache.Cached.Empty_()) return;

			Adjustments_Raw.Cache.Cached.OfType<ARAdjust>().Join(
				GetCustDocs(null, Document.Current, arsetup.Current, this).AsEnumerable().Cast<PXResult<ARInvoice, CurrencyInfo, Customer, ARTran>>(),
				_ => _.AdjdDocType + _.AdjdRefNbr + _.AdjdLineNbr,
				 _ => _.GetItem<ARInvoice>().DocType + _.GetItem<ARInvoice>().RefNbr + (_.GetItem<ARTran>().LineNbr ?? 0),
				AppendAdjustmentsInvoicesTail
			).ToArray();
		}

		public PXSelectJoin<
			ARAdjust,
				InnerJoinSingleTable<ARPayment,
					On<ARPayment.docType, Equal<ARAdjust.adjdDocType>,
					And<ARPayment.refNbr, Equal<ARAdjust.adjdRefNbr>>>,
				InnerJoin<Standalone.ARRegisterAlias,
					On<Standalone.ARRegisterAlias.docType, Equal<ARAdjust.adjdDocType>,
					And<Standalone.ARRegisterAlias.refNbr, Equal<ARAdjust.adjdRefNbr>>>>>,
			Where<
				ARAdjust.adjgDocType, Equal<Current<ARPayment.docType>>,
				And<ARAdjust.adjgRefNbr, Equal<Current<ARPayment.refNbr>>,
				And<ARAdjust.released, NotEqual<True>>>>>
			Adjustments_Payments;

		[PXViewName(Messages.ApplicationHistory)]
		[PXCopyPasteHiddenView]
		public PXSelectJoin<ARTranPostBal,
				LeftJoinSingleTable<ARInvoice,
					On<ARInvoice.docType, Equal<ARTranPostBal.sourceDocType>,
					And<ARInvoice.refNbr, Equal<ARTranPostBal.sourceRefNbr>>>,
				LeftJoin<ARRegisterAlias,
					On<ARRegisterAlias.docType, Equal<ARTranPostBal.sourceDocType>,
					And<ARRegisterAlias.refNbr, Equal<ARTranPostBal.sourceRefNbr>>>,
				LeftJoin<CM.CurrencyInfo,
					On<CM.CurrencyInfo.curyInfoID, Equal<ARRegisterAlias.curyInfoID>>,	
				LeftJoin<CM.CurrencyInfo2,
					On<CM.CurrencyInfo2.curyInfoID, Equal<Current<ARPayment.curyInfoID>>>,	
				LeftJoin<ARTran,
					On<ARTran.tranType, Equal<ARTranPostBal.sourceDocType>,
					And<ARTran.refNbr, Equal<ARTranPostBal.sourceRefNbr>,
					And<ARTran.lineNbr, Equal<ARTranPostBal.lineNbr>>>>,
				LeftJoin<ARAdjust2, On<ARAdjust2.noteID, Equal<ARTranPostBal.refNoteID>>>>>>>>,
				Where<ARTranPostBal.docType, Equal<Current<ARPayment.docType>>,
					And<ARTranPostBal.refNbr, Equal<Current<ARPayment.refNbr>>,
					And<ARTranPostBal.type, NotEqual<ARTranPost.type.origin>,
					And<ARTranPostBal.type, NotEqual<ARTranPost.type.rgol>,	
					And<ARTranPostBal.type, NotEqual<ARTranPost.type.retainageReverse>,
					And<ARTranPostBal.type, NotEqual<ARTranPost.type.retainage>>>>>>>,
				OrderBy<Asc<ARTranPostBal.iD>>>			
				ARPost;
		
		[Obsolete(InternalMessages.MethodIsObsoleteAndWillBeRemoved2022R2)]
		[PXCopyPasteHiddenView]
		public PXSelectReadonly2<
			ARAdjust,
				LeftJoin<ARInvoice,
					On<ARInvoice.docType, Equal<ARAdjust.adjdDocType>,
					And<ARInvoice.refNbr, Equal<ARAdjust.adjdRefNbr>>>,
				LeftJoin<ARTran, On<ARInvoice.paymentsByLinesAllowed, Equal<True>,
					And<ARTran.tranType, Equal<ARInvoice.docType>,
					And<ARTran.refNbr, Equal<ARInvoice.refNbr>,
					And<ARTran.lineNbr, Equal<ARAdjust.adjdLineNbr>>>>>>>,
			Where<
				ARAdjust.adjgDocType, Equal<Optional<ARPayment.docType>>,
				And<ARAdjust.adjgRefNbr, Equal<Optional<ARPayment.refNbr>>,
				And<ARAdjust.released, Equal<True>>>>>
			Adjustments_History;

		public PXSetup<ARSetup> arsetup;
		public PXSelect<CurrencyInfo, Where<CurrencyInfo.curyInfoID, Equal<Current<ARPayment.curyInfoID>>>> currencyinfo;

		[PXReadOnlyView]
		public PXSelect<CATran, Where<CATran.tranID, Equal<Current<ARPayment.cATranID>>>> dummy_CATran;
		public PXSelect<GL.Branch, Where<GL.Branch.branchID, Equal<Required<GL.Branch.branchID>>>> CurrentBranch;


		public PXSelect<CurrencyInfo, Where<CurrencyInfo.curyInfoID, Equal<Optional<CurrencyInfo.curyInfoID>>>> CurrencyInfo_CuryInfoID;

		public PXSelectJoin<ARInvoice,
			InnerJoin<CurrencyInfo,
				On<CurrencyInfo.curyInfoID, Equal<ARInvoice.curyInfoID>>,
			InnerJoin<Customer, On<Customer.bAccountID, Equal<ARInvoice.customerID>>,
			LeftJoin<ARTran, On<ARInvoice.paymentsByLinesAllowed, Equal<True>,
				And<ARTran.tranType, Equal<ARInvoice.docType>,
				And<ARTran.refNbr, Equal<ARInvoice.refNbr>,
				And<ARTran.lineNbr, Equal<Required<ARAdjust.adjdLineNbr>>>>>>>>>,
			Where<ARInvoice.docType, Equal<Required<ARInvoice.docType>>,
				And<ARInvoice.refNbr, Equal<Required<ARInvoice.refNbr>>>>>
			ARInvoice_DocType_RefNbr;

		public PXSelectJoin<ARPayment,
			InnerJoin<CurrencyInfo,
				On<CurrencyInfo.curyInfoID, Equal<ARPayment.curyInfoID>>>,
			Where<ARPayment.docType, Equal<Required<ARPayment.docType>>,
				And<ARPayment.refNbr, Equal<Required<ARPayment.refNbr>>>>>
			ARPayment_DocType_RefNbr;

		public PXSelect<
				Standalone.ARRegister,	
			Where<
				Standalone.ARRegister.released, NotEqual<True>,
				And<
				Exists<Select<ARAdjust,
					Where<Standalone.ARRegister.docType, Equal<ARAdjust.adjdDocType>,
						And<Standalone.ARRegister.refNbr, Equal<ARAdjust.adjdRefNbr>,
						And<ARAdjust.adjgDocType, Equal<Current<ARPayment.docType>>,
						And<ARAdjust.adjgRefNbr, Equal<Current<ARPayment.refNbr>>,
						And<ARAdjust.released, NotEqual<True>>>>>>>>>>>
			Adjustments_Invoices_Unreleased;

		[PXViewName(Messages.Customer)]
		public PXSetup<
			Customer,
			Where<Customer.bAccountID, Equal<Optional<ARPayment.customerID>>>> customer;

		[PXViewName(Messages.CustomerLocation)]
		public PXSetup<
			Location,
			Where<
				Location.bAccountID, Equal<Current<ARPayment.customerID>>,
				And<Location.locationID, Equal<Optional<ARPayment.customerLocationID>>>>>
			location;

		public PXSetup<CustomerClass, Where<CustomerClass.customerClassID, Equal<Current<Customer.customerClassID>>>> customerclass;

		public PXSelect<ARBalances> arbalances;

		[PXViewName(Messages.CashAccount)]
		public PXSetup<
			CashAccount,
			Where<CashAccount.cashAccountID, Equal<Current<ARPayment.cashAccountID>>>> cashaccount;

		public PXSetup<OrganizationFinPeriod, Where<OrganizationFinPeriod.finPeriodID, Equal<Optional<ARPayment.adjFinPeriodID>>,
													And<EqualToOrganizationOfBranch<OrganizationFinPeriod.organizationID, Current<ARPayment.branchID>>>>> finperiod;
		public PXSetup<PaymentMethod, Where<PaymentMethod.paymentMethodID, Equal<Current<ARPayment.paymentMethodID>>>> paymentmethod;

		public PXSetup<CCProcessingCenter>.Where<CCProcessingCenter.processingCenterID.IsEqual<ARPayment.processingCenterID.FromCurrent>> processingCenter;

		public PXSetup<GLSetup> glsetup;

		public ARPaymentChargeSelect<ARPayment, ARPayment.paymentMethodID, ARPayment.cashAccountID, ARPayment.docDate, ARPayment.tranPeriodID, ARPayment.pMInstanceID,
			Where<ARPaymentChargeTran.docType, Equal<Current<ARPayment.docType>>,
				And<ARPaymentChargeTran.refNbr, Equal<Current<ARPayment.refNbr>>>>> PaymentCharges;

		public PXFilter<LoadOptions> loadOpts;

		public PXSelect<GLVoucher, Where<True, Equal<False>>> Voucher;
		public PXSelect<ARSetupApproval,
			Where<Current<ARPayment.docType>, Equal<ARDocType.refund>,
				And<ARSetupApproval.docType, Equal<ARDocType.refund>>>> SetupApproval;

		// CC Transactions are available for both the main and voiding document.
		public PXSelect<ExternalTransaction,
			Where2<Where<ExternalTransaction.refNbr, Equal<Current<ARPayment.refNbr>>,
				And<Where<ExternalTransaction.docType, Equal<Current<ARPayment.docType>>,
					Or2<Where<Current<ARPayment.docType>, In3<ARDocType.payment, ARDocType.voidPayment>,
						And<ExternalTransaction.docType, In3<ARDocType.payment, ARDocType.voidPayment>>>,
					Or2<Where<Current<ARPayment.docType>, In3<ARDocType.prepayment, ARDocType.voidPayment>>,
						And<ExternalTransaction.docType, In3<ARDocType.prepayment, ARDocType.voidPayment>>>>>>>,
			Or<Where<Current<ARPayment.docType>, Equal<ARDocType.refund>, 
				And<ExternalTransaction.voidDocType, Equal<Current<ARPayment.docType>>,
				And<ExternalTransaction.voidRefNbr, Equal<Current<ARPayment.refNbr>>>>>>>,
			OrderBy<Desc<ExternalTransaction.transactionID>>> ExternalTran;

		public PXSelect<CCBatchTransaction,
			Where<CCBatchTransaction.refNbr, Equal<Current<ARPayment.refNbr>>,
				And<CCBatchTransaction.docType, Equal<Current<ARPayment.docType>>>>> BatchTran;

		public PXSelect<CCProcessingCenterPmntMethod,
				  Where<CCProcessingCenterPmntMethod.paymentMethodID, Equal<Current<CCProcessingCenterPmntMethod.paymentMethodID>>,
					And<CCProcessingCenterPmntMethod.processingCenterID, Equal<Current<CCProcessingCenterPmntMethod.processingCenterID>>>>> ProcessingCenterPmntMethod;

		[PXViewName(Messages.CreditCardProcessingInfo)]
		public PXSelectOrderBy<CCProcTran, OrderBy<Desc<CCProcTran.tranNbr>>> ccProcTran;
		public IEnumerable CcProcTran()
		{
			var externalTrans = ExternalTran.Select();
			var query = new PXSelect<CCProcTran, 
				Where<CCProcTran.transactionID, Equal<Required<CCProcTran.transactionID>>>>(this);
			foreach (ExternalTransaction extTran in externalTrans)
			{
				foreach (CCProcTran procTran in query.Select(extTran.TransactionID))
				{
					yield return procTran;
				}
			}
		}

		[PXCopyPasteHiddenView]
		public PXSelect<ARPaymentTotals,
			Where<ARPaymentTotals.docType, Equal<Current<ARPayment.docType>>,
				And<ARPaymentTotals.refNbr, Equal<Current<ARPayment.refNbr>>>>> PaymentTotals;
		#endregion

		#region Well-known extension

		public OrdersToApplyTab GetOrdersToApplyTabExtension(bool throwException = false)
		{
			var extension = FindImplementation<OrdersToApplyTab>();

			if (extension == null && throwException)
			{
				throw new PXException(ErrorMessages.ElementDoesntExist, nameof(OrdersToApplyTab));
			}

			return extension;
		}

		#endregion // Well-known extension

		public static string[] AdjgDocTypesToValidateFinPeriod = new string[]
		{
			ARDocType.Payment,
			ARDocType.CreditMemo,
			ARDocType.Prepayment,
			ARDocType.Refund
		};

		[PXViewName(EP.Messages.Approval)]
		public EPApprovalAutomationWithoutHoldDefaulting<ARPayment, ARPayment.approved, ARPayment.rejected, ARPayment.hold, ARSetupApproval> Approval;

		#region Cache Attached
		#region ARPayment
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRemoveBaseAttribute(typeof(ARPaymentType.ListExAttribute))]
		[ARPaymentType.List]
		protected virtual void ARPayment_DocType_CacheAttached(PXCache sender)
		{

		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Original Document")]
		protected virtual void ARPayment_OrigRefNbr_CacheAttached(PXCache sender) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXUnboundDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		protected virtual void _(Events.CacheAttached<ARPayment.curySOApplAmt> e) { }
		#endregion

		#region CATran
		[PXDBTimestamp(RecordComesFirst = true)]
		protected virtual void CATran_tstamp_CacheAttached(PXCache sender)
		{ }
		#endregion

		#region ARAdjust
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[BatchNbrExt(typeof(Search<Batch.batchNbr, Where<Batch.module, Equal<BatchModule.moduleAP>>>),
			IsMigratedRecordField = typeof(ARAdjust.isMigratedRecord))]
		protected virtual void ARAdjust_AdjBatchNbr_CacheAttached(PXCache sender) { }
		[PXFormula(typeof(Switch<
			Case<Where<
				ARAdjust.adjgDocType, Equal<Current<ARPayment.docType>>,
				And<ARAdjust.adjgRefNbr, Equal<Current<ARPayment.refNbr>>>>,
				ARAdjust.adjType.adjusted>,
			ARAdjust.adjType.adjusting>))]
		protected virtual void ARAdjust_AdjType_CacheAttached(PXCache sender) { }
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRemoveBaseAttribute(typeof(PXSelectorAttribute))]
		protected virtual void ARAdjust_DisplayRefNbr_CacheAttached(PXCache sender) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXParent(typeof(Select<ARInvoice,
			Where<ARInvoice.docType, Equal<Current<ARAdjust.adjdDocType>>,
				And<ARInvoice.refNbr, Equal<Current<ARAdjust.adjdRefNbr>>>>>))]
		[PXUnboundFormula(
			typeof(Switch<Case<Where<ARAdjust.curyAdjdAmt, NotEqual<decimal0>,
				And<ARAdjust.paymentPendingProcessing, Equal<True>,
				And<ARAdjust.paymentCaptureFailed, NotEqual<True>>>>, int1>, int0>),
			typeof(SumCalc<ARInvoice.pendingProcessingCntr>),
			ForceAggregateRecalculation = true)]
		[PXUnboundFormula(
			typeof(Switch<Case<Where<ARAdjust.curyAdjdAmt, NotEqual<decimal0>,
				And<ARAdjust.paymentCaptureFailed, Equal<True>>>, int1>, int0>),
			typeof(SumCalc<ARInvoice.captureFailedCntr>),
			ForceAggregateRecalculation = true)]
		protected virtual void ARAdjust_AdjdRefNbr_CacheAttached(PXCache sender)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[ARDocType.List]
		[PXRemoveBaseAttribute(typeof(PO.PXStringListExtAttribute))]
		protected virtual void ARAdjust_DisplayDocType_CacheAttached(PXCache sender)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXUnboundFormula(
			typeof(Switch<Case<Where<ARAdjust.voided, Equal<False>>, ARAdjust.curyAdjdAmt>, decimal0>),
			typeof(SumCalc<ARInvoice.curyPaymentTotal>))]
		protected virtual void ARAdjust_CuryAdjdAmt_CacheAttached(PXCache sender)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXUnboundFormula(typeof(Mult<ARAdjust.adjgBalSign, ARAdjust.curyAdjgAmt>), typeof(SumCalc<ARPayment.curyApplAmt>))]
		protected virtual void ARAdjust_CuryAdjgAmt_CacheAttached(PXCache sender)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXUnboundFormula(
			typeof(Switch<Case<Where<ARAdjust.voided, Equal<False>>, ARAdjust.curyAdjdDiscAmt>, decimal0>),
			typeof(SumCalc<ARInvoice.curyDiscAppliedAmt>))]
		protected virtual void ARAdjust_CuryAdjdDiscAmt_CacheAttached(PXCache sender)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXUnboundFormula(
			typeof(Switch<Case<Where<ARAdjust.voided, Equal<False>>, ARAdjust.curyAdjdWOAmt>, decimal0>),
			typeof(SumCalc<ARInvoice.curyBalanceWOTotal>))]
		protected virtual void ARAdjust_CuryAdjdWOAmt_CacheAttached(PXCache sender)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXCurrency(typeof(ARAdjust.adjgCuryInfoID), typeof(ARAdjust.adjAmt), BaseCalc = false)]
		protected void _(Events.CacheAttached<ARAdjust.displayCuryAmt> e) {}
		
		#endregion

		#region ARInvoice - Do not Check Control Accounts
		[PXRemoveBaseAttribute(typeof(ARInvoiceNbrAttribute))]
		protected virtual void ARInvoice_RefNbr_CacheAttached(PXCache sender)
		{
		}
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXRemoveBaseAttribute(typeof(PXSearchableAttribute))]
		[PXNote]
		protected virtual void ARInvoice_NoteID_CacheAttached(PXCache sender)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[Account(Visibility = PXUIVisibility.Invisible)]
		protected virtual void ARInvoice_ARAccountID_CacheAttached(PXCache sender)
		{
		}

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[Account(Visibility = PXUIVisibility.Invisible)]
		protected virtual void ARInvoice_RetainageAcctID_CacheAttached(PXCache sender)
		{
		}
		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[PXCurrency(typeof(ARInvoice.curyInfoID), typeof(ARInvoice.applicationBalance), BaseCalc = false)]
		protected virtual void ARInvoice_CuryApplicationBalance_CacheAttached(PXCache sender)
		{
		}

		[PXDBTimestamp(RecordComesFirst = true)]
		protected virtual void ARInvoice_tstamp_CacheAttached(PXCache sender)
		{
		}

		#endregion

		#region EPApproval
		[PXDefault]
		[PXDBInt]
		[PXSelector(typeof(Search<EPAssignmentMap.assignmentMapID, Where<EPAssignmentMap.entityType, Equal<AssignmentMapType.AssignmentMapTypeARPayment>>>),
				DescriptionField = typeof(EPAssignmentMap.name))]
		[PXUIField(DisplayName = "Approval Map")]
		protected virtual void EPApproval_AssignmentMapID_CacheAttached(PXCache sender)
		{
		}
		[PXDBDate]
		[PXDefault(typeof(ARPayment.docDate), PersistingCheck = PXPersistingCheck.Nothing)]
		protected virtual void EPApproval_DocDate_CacheAttached(PXCache sender)
		{
		}

		[PXDBInt]
		[PXDefault(typeof(ARPayment.customerID), PersistingCheck = PXPersistingCheck.Nothing)]
		protected virtual void EPApproval_BAccountID_CacheAttached(PXCache sender)
		{
		}

		[PXDBString(60, IsUnicode = true)]
		[PXDefault(typeof(ARPayment.docDesc), PersistingCheck = PXPersistingCheck.Nothing)]
		protected virtual void EPApproval_Descr_CacheAttached(PXCache sender)
		{
		}

		[PXDBLong]
		[CurrencyInfo(typeof(ARPayment.curyInfoID))]
		protected virtual void EPApproval_CuryInfoID_CacheAttached(PXCache sender)
		{
		}

		[PXDBDecimal]
		[PXDefault(typeof(ARPayment.curyOrigDocAmt), PersistingCheck = PXPersistingCheck.Nothing)]
		protected virtual void EPApproval_CuryTotalAmount_CacheAttached(PXCache sender)
		{
		}

		[PXDBDecimal]
		[PXDefault(typeof(ARPayment.origDocAmt), PersistingCheck = PXPersistingCheck.Nothing)]
		protected virtual void EPApproval_TotalAmount_CacheAttached(PXCache sender)
		{
		}
		#endregion

		#region ARPaymentTotals
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXSelector(typeof(Search<ARAdjust.ARInvoice.refNbr, Where<ARAdjust.ARInvoice.docType, Equal<Current<ARPaymentTotals.adjdDocType>>>>))]
		protected virtual void _(Events.CacheAttached<ARPaymentTotals.adjdRefNbr> e)
		{
		}
		#endregion
		
		#region ARTranPostBal
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIFieldAttribute(DisplayName = "Doc. Type")]
		protected virtual void ARTranPostBal_SourceDocType_CacheAttached(PXCache sender) { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIFieldAttribute(DisplayName = "Reference Nbr.")]
		protected virtual void ARTranPostBal_SourceRefNbr_CacheAttached(PXCache sender) { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXUIFieldAttribute(DisplayName = "Amount Paid")]
		protected virtual void ARTranPostBal_CuryAmt_CacheAttached(PXCache sender) { }
		#endregion
		#endregion

		protected virtual void EPApproval_SourceItemType_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			if (Document.Current != null)
			{
				e.NewValue = new ARDocType.ListAttribute()
					.ValueLabelDic[Document.Current.DocType];

				e.Cancel = true;
			}
		}

		protected virtual void EPApproval_Details_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			if (Document.Current != null)
			{
				e.NewValue = EPApprovalHelper.BuildEPApprovalDetailsString(sender, Document.Current);
			}
		}
				
		#region Properties
		public bool AutoPaymentApp
		{
			get;
			set;
		}

		public bool IsReverseProc
		{
			get;
			set;
		}

		public bool HasUnreleasedSOInvoice
		{
			get;
			set;
		}

		public bool ForcePaymentApp
		{
			get;
			set;
		}

		public bool IgnoreNegativeOrderBal // TODO: SOCreatePayment: Temporary fix ARPayment bug (AC-159389), after fix we should remove this property.
		{
			get;
			set;
		}
		#endregion

		[InjectDependency]
		public IFinPeriodRepository FinPeriodRepository { get; set; }

		[InjectDependency]
		public IFinPeriodUtils FinPeriodUtils { get; set; }

		[InjectDependency]
		internal ICurrentUserInformationProvider CurrentUserInformationProvider { get; set; }

		#region CallBack Handlers
		public PXAction<ARPayment> newCustomer;
		[PXUIField(DisplayName = "New Customer", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXLookupButton]
		public virtual IEnumerable NewCustomer(PXAdapter adapter)
		{
			CustomerMaint graph = PXGraph.CreateInstance<CustomerMaint>();
			throw new PXRedirectRequiredException(graph, "New Customer");
		}

		public PXAction<ARPayment> editCustomer;
		[PXUIField(DisplayName = "Edit Customer", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXLookupButton]
		public virtual IEnumerable EditCustomer(PXAdapter adapter)
		{
			if (customer.Current != null)
			{
				CustomerMaint graph = PXGraph.CreateInstance<CustomerMaint>();
				graph.BAccount.Current = customer.Current;
				throw new PXRedirectRequiredException(graph, "Edit Customer");
			}
			return adapter.Get();
		}

		public PXAction<ARPayment> customerDocuments;
		[PXUIField(DisplayName = "Customer Details", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable CustomerDocuments(PXAdapter adapter)
		{
			if (customer.Current != null)
			{
				ARDocumentEnq graph = PXGraph.CreateInstance<ARDocumentEnq>();
				graph.Filter.Current.CustomerID = customer.Current.BAccountID;
				graph.Filter.Select();
				throw new PXRedirectRequiredException(graph, "Customer Details");
			}
			return adapter.Get();
		}

		[PXUIField(DisplayName = "Release", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXProcessButton]
		[ARMigrationModeDependentActionRestriction(
			restrictInMigrationMode: false,
			restrictForRegularDocumentInMigrationMode: true,
			restrictForUnreleasedMigratedDocumentInNormalMode: true)]
		public override IEnumerable Release(PXAdapter adapter)
		{
			PXCache cache = Document.Cache;
			List<ARRegister> list = new List<ARRegister>();
			foreach (ARPayment ardoc in adapter.Get<ARPayment>())
			{
				if (!(bool)ardoc.Hold)
				{
					cache.Update(ardoc);
					list.Add(ardoc);
				}
			}
			if (list.Count == 0)
			{
				throw new PXException(Messages.Document_Status_Invalid);
			}
			Save.Press();
			PXLongOperation.StartOperation(this, delegate () { ARDocumentRelease.ReleaseDoc(list, false); });
			return list;
		}
		[PXUIField(DisplayName = "Void", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXProcessButton]
		[ARMigrationModeDependentActionRestriction(
			restrictInMigrationMode: false,
			restrictForRegularDocumentInMigrationMode: true,
			restrictForUnreleasedMigratedDocumentInNormalMode: true)]
		public override IEnumerable VoidCheck(PXAdapter adapter)
		{
			ARPayment payment = Document.Current;

			if (payment == null) return adapter.Get();

			if (payment != null &&
				payment.Released == true &&
				payment.Voided == false
				&& ARPaymentType.VoidEnabled(payment))
			{
				ARAdjust refundApplication = PXSelect<ARAdjust,
					Where<ARAdjust.adjdDocType, Equal<Required<ARAdjust.adjdDocType>>,
						And<ARAdjust.adjdRefNbr, Equal<Required<ARAdjust.adjdRefNbr>>,
						And<ARAdjust.adjgDocType, Equal<ARDocType.refund>,
						And<ARAdjust.voided, NotEqual<True>>>>>>
					.SelectWindowed(this, 0, 1, payment.DocType, payment.RefNbr);

				if (refundApplication != null && refundApplication.IsSelfAdjustment() != true)
				{
					throw new PXException(
						Common.Messages.DocumentHasBeenRefunded,
						GetLabel.For<ARDocType>(payment.DocType),
						Document.Current.RefNbr,
						GetLabel.For<ARDocType>(refundApplication.AdjgDocType),
						refundApplication.AdjgRefNbr);
				}

				if (arsetup.Current.MigrationMode != true &&
					payment.IsMigratedRecord == true &&
					payment.CuryInitDocBal != payment.CuryOrigDocAmt)
				{
					throw new PXException(Messages.MigrationModeIsDeactivatedForMigratedDocument);
				}

				if (arsetup.Current.MigrationMode == true && payment.IsMigratedRecord == true)
				{
					if (PXSelect<ARAdjust,
						Where<ARAdjust.adjgDocType, Equal<Current<ARPayment.docType>>,
						And<ARAdjust.adjgRefNbr, Equal<Current<ARPayment.refNbr>>,
						And<ARAdjust.released, Equal<True>,
						And<ARAdjust.voided, NotEqual<True>,
						And<ARAdjust.isMigratedRecord, NotEqual<True>,
						And<ARAdjust.isInitialApplication, NotEqual<True>>>>>>>>.Select(this)
						.RowCast<ARAdjust>()
						.Any(_ => _.VoidAppl != true))
					throw new PXException(Common.Messages.CannotVoidPaymentRegularUnreversedApplications);
				}

				if (!ARPaymentType.IsSelfVoiding(payment.DocType))
				{
					ARPayment voidPayment = Document.Search<ARPayment.refNbr>(payment.RefNbr, ARPaymentType.GetVoidingARDocType(payment.DocType));

					if (voidPayment != null)
					{
						this.Document.Current = voidPayment;
						if (IsContractBasedAPI || IsImport)
						{
							return new[] { this.Document.Current };
						}
						else
						{
							// Redirect to itself for UI
						throw new PXRedirectRequiredException(this, Messages.Voided);
					}
				}
				}

				// Delete unreleased applications
				// -
				bool anyApplicationDeleted = false;

				foreach (ARAdjust application in Adjustments_Raw.Select())
				{
					Adjustments.Cache.Delete(application);
					anyApplicationDeleted = true;
				}

				if (!anyApplicationDeleted
					&& payment.OpenDoc == true
					&& PXUIFieldAttribute.GetError<ARPayment.adjFinPeriodID>(Document.Cache, payment) != null)
				{
					Document.Cache.SetStatus(payment, PXEntryStatus.Notchanged);
				}

				Save.Press();

				ARPayment document = PXCache<ARPayment>.CreateCopy(payment);
                document.NoteID = null;

				if (document.SelfVoidingDoc != true
					&& (
						paymentmethod.Current == null
						|| paymentmethod.Current.ARDefaultVoidDateToDocumentDate != true))
				{
					document.DocDate = Accessinfo.BusinessDate > document.DocDate
						? Accessinfo.BusinessDate
						: document.DocDate;
					
					string businessPeriodID = FinPeriodRepository.GetPeriodIDFromDate(document.DocDate, FinPeriod.organizationID.MasterValue);
					FinPeriodIDAttribute.SetPeriodsByMaster<ARPayment.adjFinPeriodID>(Document.Cache, document, businessPeriodID);
					
					finperiod.Cache.Current = finperiod.View.SelectSingleBound(new object[] { document });
				}

				if (paymentmethod.Current?.ARDefaultVoidDateToDocumentDate == true)
				{
					document.AdjFinPeriodID = document.FinPeriodID;
					document.AdjTranPeriodID = document.TranPeriodID;
				}
				else
				{
					FinPeriodUtils.VerifyAndSetFirstOpenedFinPeriod<ARPayment.adjFinPeriodID, ARPayment.branchID>(
						Document.Cache,
						document,
						finperiod,
						typeof(OrganizationFinPeriod.aRClosed));
				}

				

				if (document.DepositAsBatch == true
					&& !string.IsNullOrEmpty(document.DepositNbr)
					&& document.Deposited != true)
				{
					throw new PXException(Messages.ARPaymentIsIncludedIntoCADepositAndCannotBeVoided);
				}

				CheckCreditCardTranStateBeforeVoiding();
				try
				{
					_IsVoidCheckInProgress = true;

					if (payment.SelfVoidingDoc == true)
					{
						SelfVoidingProc(document);
					}
					else
					{
						VoidCheckProc(document);
					}
				}
				finally
				{
					_IsVoidCheckInProgress = false;
				}

				Document.Cache.RaiseExceptionHandling<ARPayment.finPeriodID>(Document.Current, Document.Current.FinPeriodID, null);
				if (IsContractBasedAPI || IsImport)
				{
					return new[] { this.Document.Current };
				}
				else
				{
					// Redirect to itself for UI
				throw new PXRedirectRequiredException(this, Messages.Voided);
			}
			}
			else if (
				payment.Released != true
				&& payment.Voided == false
				&& (payment.DocType == ARDocType.Payment || payment.DocType == ARDocType.Prepayment))
			{
				if (ExternalTranHelper.HasTransactions(ExternalTran))
				{
					Save.Press();
					if (arsetup.Current.RequireExtRef == true
						&& string.IsNullOrWhiteSpace(payment.ExtRefNbr))
					{
						string displayName = PXUIFieldAttribute.GetDisplayName(Document.Cache, nameof(ARPayment.ExtRefNbr));
						throw new PXException(ErrorMessages.FieldIsEmpty, displayName);
					}

					ARPayment document = payment;

					document.Voided = true;
					document.OpenDoc = false;
					document.PendingProcessing = false;
					document = Document.Update(document);
					Caches[typeof(ARAdjust)].ClearQueryCache();
					foreach (ARAdjust application in Adjustments_Raw.Select())
					{
						if (application.Voided == true) continue;

						ARAdjust applicationCopy = (ARAdjust)Caches[typeof(ARAdjust)].CreateCopy(application);
						applicationCopy.Voided = true;

						Caches[typeof(ARAdjust)].Update(applicationCopy);
					}

					if (document.CATranID != null && document.CashAccountID != null)
					{
						CATran cashTransaction = PXSelect<CATran, Where<CATran.tranID, Equal<Required<CATran.tranID>>>>.Select(this, document.CATranID);

						if (cashTransaction != null)
						{
							Caches[typeof(CATran)].Delete(cashTransaction);
						}

						document.CATranID = null;
					}

					document.CCReauthTriesLeft = 0;
					document.CCReauthDate = null;
					document.IsCCUserAttention = false;

					document = Document.Update(document);
					ARPayment.Events
						.Select(ev=>ev.VoidDocument)
						.FireOn(this, document);
					Save.Press();
				}
			}
			return adapter.Get();
		}

		public PXAction<ARPayment> refund;

		[PXUIField(DisplayName = "Refund", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXProcessButton]
		[ARMigrationModeDependentActionRestriction(
			restrictInMigrationMode: false,
			restrictForRegularDocumentInMigrationMode: true,
			restrictForUnreleasedMigratedDocumentInNormalMode: true)]
		public virtual IEnumerable Refund(PXAdapter adapter)
		{
			ARPayment payment = Document.Current;

			if (payment == null) return adapter.Get();

			if (payment != null &&
				payment.Released == true &&
				payment.Voided == false)
			{
				ValidatePaymentForRefund(payment);
				
				Save.Press();

				if (payment.DocType == ARPaymentType.CreditMemo)
				{
					ARInvoiceEntry invoiceGraph = CreateInstance<ARInvoiceEntry>();
					invoiceGraph.Document.Current = invoiceGraph.Document.Search<ARInvoice.refNbr>(payment.RefNbr, payment.DocType);
					invoiceGraph.customerRefund.Press();
				}
				else
				{
					return CreateRefundFromPayment(payment);
				}
			}

			return adapter.Get();
		}

		private IEnumerable CreateRefundFromPayment(ARPayment payment)
		{
			ARPayment document = PXCache<ARPayment>.CreateCopy(payment);
			document.NoteID = null;

			document.DocDate = Accessinfo.BusinessDate;

			string businessPeriodID =
				FinPeriodRepository.GetPeriodIDFromDate(document.DocDate, FinPeriod.organizationID.MasterValue);
			FinPeriodIDAttribute.SetPeriodsByMaster<ARPayment.adjFinPeriodID>(Document.Cache, document,
				businessPeriodID);

			finperiod.Cache.Current = finperiod.View.SelectSingleBound(new object[] {document});

			FinPeriodUtils.VerifyAndSetFirstOpenedFinPeriod<ARPayment.adjFinPeriodID, ARPayment.branchID>(
				Document.Cache,
				document,
				finperiod,
				typeof(OrganizationFinPeriod.aRClosed));

			RefundCheckProc(document);

			Document.Cache.RaiseExceptionHandling<ARPayment.finPeriodID>(Document.Current,
				Document.Current.FinPeriodID, null);
			if (IsContractBasedAPI) return new[] {this.Document.Current};
			throw new PXRedirectRequiredException(this, Messages.Refund);
		}

		public class PXLoadInvoiceException : Exception
		{
			public PXLoadInvoiceException() { }

			public PXLoadInvoiceException(SerializationInfo info, StreamingContext context)
				: base(info, context) { }
		}

		private ARAdjust AddAdjustment(ARAdjust adj)
		{
			if (Document.Current.CuryUnappliedBal == 0m && Document.Current.CuryOrigDocAmt > 0m)
			{
				throw new PXLoadInvoiceException();
			}
			return this.Adjustments.Insert(adj);
		}

		private ARAdjust AddAdjustmentExt(ARAdjust adj)
		{
			return this.Adjustments.Insert(adj); // call - ARAdjust_AdjdRefNbr_FieldUpdated
		}

		/// <summary>
		/// This particular overload of LoadInvoicesProc is being used in ARReleaseProcess.ReleaseDocProc to create applications for available documents when AutoApplyPayments is True
		/// Can be overloaded in extension to pass LoadOptions to use in that particular case
		/// </summary>
		public virtual void LoadInvoicesProc(bool LoadExistingOnly)
		{
			LoadInvoicesProc(LoadExistingOnly, null);
		}

		private static string ToKeyString(ARAdjust adj) => string.Join("_", adj.AdjdDocType, adj.AdjdRefNbr, adj.AdjdLineNbr);
		private static string ToKeyString(ARInvoice invoice, ARTran tran) => string.Join("_", invoice.DocType, invoice.RefNbr, tran?.LineNbr ?? 0);

		public virtual void LoadInvoicesProc(bool LoadExistingOnly, LoadOptions opts)
		{
			Dictionary<string, ARAdjust> existing = new Dictionary<string, ARAdjust>();
			ARPayment currentDoc = Document.Current;
			InternalCall = true;
			try
			{
				if (currentDoc == null ||
					currentDoc.CustomerID == null ||
					currentDoc.OpenDoc == false ||
					currentDoc.DocType != ARDocType.Payment &&
					currentDoc.DocType != ARDocType.Prepayment &&
					currentDoc.DocType != ARDocType.CreditMemo)
				{
					throw new PXLoadInvoiceException();
				}

				CalcApplAmounts(Document.Cache, currentDoc);

				Adjustments_Invoices_BeforeSelect();

				foreach (PXResult<ARAdjust> res in Adjustments_Raw.Select())
				{
					ARAdjust old_adj = res;

					if (LoadExistingOnly == false)
					{
						old_adj = PXCache<ARAdjust>.CreateCopy(old_adj);
						old_adj.CuryAdjgAmt = null;
						old_adj.CuryAdjgDiscAmt = null;
						old_adj.CuryAdjgPPDAmt = null;
					}

					existing.Add(ToKeyString(old_adj), old_adj);
					Adjustments.Cache.Delete((ARAdjust)res);
				}

				currentDoc.AdjCntr++;
				Document.Cache.MarkUpdated(currentDoc);
				Document.Cache.IsDirty = true;
				AutoPaymentApp = true;

				foreach (ARAdjust res in existing.Values.OrderBy(o=>o.AdjgBalSign))
				{
					ARAdjust adj = new ARAdjust
					{
						AdjdDocType = res.AdjdDocType,
						AdjdRefNbr = res.AdjdRefNbr,
						AdjdLineNbr = res.AdjdLineNbr
					};

					try
					{
						adj = PXCache<ARAdjust>.CreateCopy(AddAdjustment(adj));
						if (res.CuryAdjgDiscAmt != null && res.CuryAdjgDiscAmt < adj.CuryAdjgDiscAmt)
						{
							adj.CuryAdjgDiscAmt = res.CuryAdjgDiscAmt;
							adj.CuryAdjgPPDAmt = res.CuryAdjgDiscAmt;
							adj = PXCache<ARAdjust>.CreateCopy((ARAdjust)this.Adjustments.Cache.Update(adj));
						}

						if (res.CuryAdjgAmt != null && res.CuryAdjgAmt < adj.CuryAdjgAmt)
						{
							adj.CuryAdjgAmt = res.CuryAdjgAmt;
							adj = PXCache<ARAdjust>.CreateCopy((ARAdjust)this.Adjustments.Cache.Update(adj));
						}
						if (res.WriteOffReasonCode != null)
						{
							adj.WriteOffReasonCode = res.WriteOffReasonCode;
							adj.CuryAdjgWOAmt = res.CuryAdjgWOAmt;
							this.Adjustments.Cache.Update(adj);
						}
					}
					catch (PXSetPropertyException) { }
				}

				if (LoadExistingOnly)
				{
					return;
				}

				PXResultset<ARInvoice> custdocs = GetCustDocs(opts, currentDoc, arsetup.Current, this);

				foreach (PXResult<ARInvoice, CurrencyInfo, Customer, ARTran> res in custdocs)
				{
					ARInvoice invoice = res;
					CurrencyInfo info = res;
					ARTran tran = res;

					if (existing.ContainsKey(ToKeyString(invoice, tran)) == false)
					{
						ARAdjust adj = new ARAdjust
						{
							AdjdDocType = invoice.DocType,
							AdjdRefNbr = invoice.RefNbr,
							AdjdLineNbr = tran?.LineNbr ?? 0,
							AdjgDocType = currentDoc.DocType,
							AdjgRefNbr = currentDoc.RefNbr,
							AdjNbr = currentDoc.AdjCntr
						};
						AddBalanceCache(adj, res);

						PXSelectorAttribute.StoreCached<ARAdjust.adjdRefNbr>(Adjustments.Cache, adj, new ARAdjust.ARInvoice
						{
							DocType = adj.AdjdDocType,
							RefNbr = adj.AdjdRefNbr,
							PaymentsByLinesAllowed = invoice.PaymentsByLinesAllowed
						}, true);
						ARInvoice_DocType_RefNbr.View.Clear();
						ARInvoice_DocType_RefNbr.StoreResult(new List<object> { res },
							PXQueryParameters.ExplicitParameters(adj.AdjdLineNbr, invoice.DocType, invoice.RefNbr));

						AddAdjustment(adj);
					}
				}
				AutoPaymentApp = false;

				if (currentDoc.CuryApplAmt < 0m)
				{
					List<ARAdjust> credits = new List<ARAdjust>();

					foreach (ARAdjust adj in Adjustments_Raw.Select())
					{
						if (adj.AdjdDocType == ARDocType.CreditMemo)
						{
							credits.Add(adj);
						}
					}

					credits.Sort((a, b) =>
					{
						return ((IComparable)a.CuryAdjgAmt).CompareTo(b.CuryAdjgAmt);
					});

					foreach (ARAdjust adj in credits)
					{
						if (adj.CuryAdjgAmt <= -currentDoc.CuryApplAmt)
						{
							Adjustments.Delete(adj);
						}
						else
						{
							ARAdjust copy = PXCache<ARAdjust>.CreateCopy(adj);
							copy.CuryAdjgAmt += currentDoc.CuryApplAmt;
							Adjustments.Update(copy);
						}
					}
				}

			}
			catch (PXLoadInvoiceException)
			{
			}
			finally
			{
				InternalCall = false;
			}
		}

		public virtual void LoadInvoicesExtProc(bool LoadExistingOnly, LoadOptions opts)
		{
			ARPayment currentDoc = Document.Current;

			InternalCall = true;    // exclude ARPayment_RowSelected 

			this.ARInvoice_DocType_RefNbr.Cache.DisableReadItem = true;
			this.Adjustments.Cache.DisableReadItem = true;

			bool reload = (opts?.LoadingMode == LoadOptions.loadingMode.Reload);

			Dictionary<string, ARAdjust> existing = new Dictionary<string, ARAdjust>();
			try
			{
				#region PXLoadInvoiceException
				if (currentDoc == null || currentDoc.CustomerID == null || currentDoc.OpenDoc == false ||
					currentDoc.DocType != ARDocType.Payment &&
					currentDoc.DocType != ARDocType.Prepayment &&
					currentDoc.DocType != ARDocType.CreditMemo &&
					currentDoc.DocType != ARDocType.Refund)
				{
					throw new PXLoadInvoiceException();
				}
				#endregion
				#region Recalc Balances
				CalcApplAmounts(Document.Cache, currentDoc);
				#endregion

				Adjustments_Invoices_BeforeSelect();
				Dictionary<string, ARInvoice> invoices = Adjustments_Invoices.Select().ToDictionary(_ => ToKeyString(_), _ => _.GetItem<ARInvoice>());
				#region Save existed adjustment and Delete if isReload
				foreach (PXResult<ARAdjust> res in Adjustments_Raw.Select())
				{
					ARAdjust old_adj = res;
					if (reload == false) // if not "reload" save row as existed
					{
						if (LoadExistingOnly == false)
						{
							old_adj = PXCache<ARAdjust>.CreateCopy(old_adj);
						}
						existing.Add(ToKeyString(old_adj), old_adj);
					}
					Adjustments.Cache.Delete((ARAdjust)res);
				}
				#endregion

				currentDoc.AdjCntr++;
				Document.Cache.MarkUpdated(currentDoc);
				Document.Cache.IsDirty = true;

				Adjustments_Invoices_BeforeSelect();
				#region restoring existing rows
				if (reload == false || LoadExistingOnly)
					foreach (KeyValuePair<string, ARAdjust> res in existing)
					{
						ARAdjust old_adj = res.Value;
						ARAdjust adj = new ARAdjust
						{
							AdjdDocType = old_adj.AdjdDocType,
							AdjdRefNbr = old_adj.AdjdRefNbr,
							AdjdLineNbr = old_adj.AdjdLineNbr,

							CuryAdjgAmt = old_adj.CuryAdjgAmt,
							CuryAdjgDiscAmt = (old_adj.CuryAdjgPPDAmt ?? 0) == 0 ? old_adj.CuryAdjgDiscAmt : old_adj.CuryAdjgPPDAmt
						};

						if (invoices.TryGetValue(res.Key, out ARInvoice linkedInvoice))
							PXSelectorAttribute.StoreResult<ARAdjust.adjdRefNbr>(this.Adjustments.Cache, adj, linkedInvoice);

						try
						{
							adj = PXCache<ARAdjust>.CreateCopy(AddAdjustmentExt(adj)); // call - ARAdjust_AdjdRefNbr_FieldUpdated
							if (old_adj.CuryAdjgDiscAmt != null && old_adj.CuryAdjgDiscAmt < 0m
								? old_adj.CuryAdjgDiscAmt > adj.CuryAdjgDiscAmt
								: old_adj.CuryAdjgDiscAmt < adj.CuryAdjgDiscAmt)
							{
								adj.CuryAdjgDiscAmt = old_adj.CuryAdjgDiscAmt;
								adj.CuryAdjgPPDAmt = old_adj.CuryAdjgDiscAmt;
								adj = PXCache<ARAdjust>.CreateCopy((ARAdjust)this.Adjustments.Cache.Update(adj));
							}
							if (old_adj.CuryAdjgAmt != null && old_adj.CuryAdjgAmt < 0m
								? old_adj.CuryAdjgAmt > adj.CuryAdjgAmt
								: old_adj.CuryAdjgAmt < adj.CuryAdjgAmt)
							{
								adj.CuryAdjgAmt = old_adj.CuryAdjgAmt;
								adj = PXCache<ARAdjust>.CreateCopy((ARAdjust)this.Adjustments.Cache.Update(adj));
							}
							if (old_adj.WriteOffReasonCode != null)
							{
								adj.WriteOffReasonCode = old_adj.WriteOffReasonCode;
								adj.CuryAdjgWOAmt = old_adj.CuryAdjgWOAmt;
								this.Adjustments.Cache.Update(adj);
							}
						}
						catch (PXSetPropertyException) { }
					}
				#endregion

				if (LoadExistingOnly)
				{
					return;
				}

				#region Load new documents and merge with existing adjustments
				if (!(opts?.MaxDocs >= 0)) return;

				PXResultset<ARInvoice> custdocs = GetCustDocs(opts, currentDoc, arsetup.Current, this);
				AutoPaymentApp = true;
				foreach (PXResult<ARInvoice, CurrencyInfo, Customer, ARTran> res in custdocs)
				{
					ARInvoice invoice = res;
					ARRegisterAlias reg = Common.Utilities.Clone<ARInvoice, ARRegisterAlias>(this, invoice);
					CurrencyInfo info = res;
					ARTran tran = (ARTran)res;

					if (existing.ContainsKey(ToKeyString(invoice, tran)) == false)
					{
						ARAdjust adj = new ARAdjust
						{
							AdjdDocType = invoice.DocType,
							AdjdRefNbr = invoice.RefNbr,
							AdjdLineNbr = tran?.LineNbr ?? 0,
							AdjgDocType = currentDoc.DocType,
							AdjgRefNbr = currentDoc.RefNbr,
							AdjNbr = currentDoc.AdjCntr,
							CuryAdjgAmt = 0, // not apply
							CuryAdjgDiscAmt = 0
						};

						Adjustments_Invoices.StoreTailResult(
							new List<object>() { new PXResult<ARInvoice, ARRegisterAlias, ARTran>(invoice, reg, tran) },
							new object[] { invoice, reg, tran, adj },
							adj.AdjgDocType, adj.AdjgRefNbr);

						//TODO: try to remove after merge to 2022 (with MC in PO)/try to remove Adjustments_Payments from GetChildren
						Adjustments_Payments.StoreTailResult(
							new List<object>() { new PXResult<ARPayment, ARRegisterAlias, ARTran>(Document.Current, reg, tran) },
							new object[] { invoice, reg, tran, adj },
							adj.AdjgDocType, adj.AdjgRefNbr);

						AddBalanceCache(adj, res);
						StoreInvoiceForAdjdRefNbrSelector(invoice, adj);
						ARTran cachedTran = tran;
						if (cachedTran.LineNbr == null)
							cachedTran = new ARTran()
							{
								TranType = invoice.DocType,
								RefNbr = invoice.RefNbr,
								LineNbr = 0,
								SortOrder = 0
							};
						PXSelectorAttribute.StoreResult<ARAdjust.adjdLineNbr>(Adjustments.Cache, adj,
								new List<object>() { new PXResult<ARTran, ARInvoice>(cachedTran, invoice) });

						ARInvoice_DocType_RefNbr.StoreResult(new List<object> { res },
							PXQueryParameters.ExplicitParameters(adj.AdjdLineNbr, invoice.DocType, invoice.RefNbr));

						PXParentAttribute.SetParent(this.Adjustments.Cache, adj, typeof(ARInvoice), invoice);
						PXParentAttribute.SetParent(this.Adjustments.Cache, adj, typeof(ARPayment), currentDoc);

						PXParentAttribute.SetParent(this.Adjustments.Cache, adj, typeof(ARInvoice), invoice);
						PXParentAttribute.SetParent(this.Adjustments.Cache, adj, typeof(ARPayment), currentDoc);

						adj = AddAdjustmentExt(adj); // call - ARAdjust_AdjdRefNbr_FieldUpdated
					}
				}
				AutoPaymentApp = false;

				#endregion
			}
			catch (PXLoadInvoiceException)
			{
			}
			finally
			{
				InternalCall = false;
			}
		}

		private void StoreInvoiceForAdjdRefNbrSelector(ARInvoice invoice, ARAdjust adj)
		{
			PXSelectorAttribute.StoreResult<ARAdjust.adjdRefNbr>(Adjustments.Cache, adj, new ARAdjust.ARInvoice
			{
				DocType = adj.AdjdDocType,
				RefNbr = adj.AdjdRefNbr,
				PaymentsByLinesAllowed = invoice.PaymentsByLinesAllowed,
				PendingPPD = invoice.PendingPPD,
				HasPPDTaxes = invoice.HasPPDTaxes
			});
		}

		public static int GetCustDocsCount(LoadOptions opts, ARPayment currentARPayment, ARSetup currentARSetup, PXGraph graph)
		{
			#region CMD
			PXSelectBase<ARInvoice> cmd = new PXSelectJoinGroupBy<ARInvoice,
				InnerJoin<CurrencyInfo, On<CurrencyInfo.curyInfoID, Equal<ARInvoice.curyInfoID>>,
				InnerJoin<Customer, On<Customer.bAccountID, Equal<ARInvoice.customerID>>,
				LeftJoin<ARTran, On<ARInvoice.paymentsByLinesAllowed, Equal<True>,
					And<ARTran.tranType, Equal<ARInvoice.docType>,
					And<ARTran.refNbr, Equal<ARInvoice.refNbr>,
					And<ARTran.curyTranBal, NotEqual<decimal0>>>>>,
				LeftJoin<ARAdjust, On<ARAdjust.adjdDocType, Equal<ARInvoice.docType>,
					And<ARAdjust.adjdRefNbr, Equal<ARInvoice.refNbr>,
					And<ARAdjust.released, NotEqual<True>,
					And<ARAdjust.voided, NotEqual<True>,
					And<Where<ARAdjust.adjgDocType, NotEqual<Required<ARRegister.docType>>,
						Or<ARAdjust.adjgRefNbr, NotEqual<Required<ARRegister.refNbr>>>>>>>>>,
				LeftJoin<ARAdjust2, On<ARAdjust2.adjgDocType, Equal<ARInvoice.docType>,
					And<ARAdjust2.adjgRefNbr, Equal<ARInvoice.refNbr>,
					And<ARAdjust2.released, NotEqual<True>,
					And<ARAdjust2.voided, NotEqual<True>>>>>>>>>>,
				Where<ARInvoice.docType, NotEqual<Required<ARPayment.docType>>,
					And<ARInvoice.released, Equal<True>,
					And<ARInvoice.openDoc, Equal<True>,
					And<ARAdjust.adjgRefNbr, IsNull,
					And<ARAdjust2.adjgRefNbr, IsNull,
					And<ARInvoice.pendingPPD, NotEqual<True>,
					And<ARInvoice.isUnderCorrection, NotEqual<True>>>>>>>>,
				Aggregate<Count>>(graph);

			if (opts != null)
			{
				if (opts.FromDate != null)
				{
					cmd.WhereAnd<Where<ARInvoice.docDate, GreaterEqual<Required<LoadOptions.fromDate>>>>();
				}
				if (opts.TillDate != null)
				{
					cmd.WhereAnd<Where<ARInvoice.docDate, LessEqual<Required<LoadOptions.tillDate>>>>();
				}
				if (!string.IsNullOrEmpty(opts.StartRefNbr))
				{
					cmd.WhereAnd<Where<ARInvoice.refNbr, GreaterEqual<Required<LoadOptions.startRefNbr>>>>();
				}
				if (!string.IsNullOrEmpty(opts.EndRefNbr))
				{
					cmd.WhereAnd<Where<ARInvoice.refNbr, LessEqual<Required<LoadOptions.endRefNbr>>>>();
				}
				if (opts.BranchID != null)
				{
					cmd.WhereAnd<Where<ARInvoice.branchID, Equal<Required<LoadOptions.branchID>>>>();
				}
				else if (opts.OrganizationID != null)
				{
					cmd.WhereAnd<Where<ARInvoice.branchID, In<Required<LoadOptions.branchID>>>>();
				}
			}

			var loadChildDocs = opts == null ? LoadOptions.loadChildDocuments.None : opts.LoadChildDocuments;
			switch (loadChildDocs)
			{
				case LoadOptions.loadChildDocuments.IncludeCRM:
					cmd.WhereAnd<Where<ARInvoice.customerID, Equal<Required<ARRegister.customerID>>,
									Or<Customer.consolidatingBAccountID, Equal<Required<ARRegister.customerID>>>>>();
					break;
				case LoadOptions.loadChildDocuments.ExcludeCRM:
					cmd.WhereAnd<Where<ARInvoice.customerID, Equal<Required<ARRegister.customerID>>,
									Or<Customer.consolidatingBAccountID, Equal<Required<ARRegister.customerID>>,
										And<ARInvoice.docType, NotEqual<ARDocType.creditMemo>>>>>();
					break;
				default:
					cmd.WhereAnd<Where<ARInvoice.customerID, Equal<Required<ARRegister.customerID>>>>();
					break;
			}

			switch (currentARPayment.DocType)
			{
				case ARDocType.Payment:
					cmd.WhereAnd<Where<ARInvoice.docType, Equal<ARDocType.invoice>,
						Or<ARInvoice.docType, Equal<ARDocType.debitMemo>,
						Or<ARInvoice.docType, Equal<ARDocType.creditMemo>,
						Or<ARInvoice.docType, Equal<ARDocType.finCharge>>>>>>();
					break;
				case ARDocType.Prepayment:
					cmd.WhereAnd<Where<ARInvoice.docType, Equal<ARDocType.invoice>,
						Or<ARInvoice.docType, Equal<ARDocType.creditMemo>,
						Or<ARInvoice.docType, Equal<ARDocType.debitMemo>,
						Or<ARInvoice.docType, Equal<ARDocType.finCharge>>>>>>();
					break;
				case ARDocType.CreditMemo:
					cmd.WhereAnd<Where<ARInvoice.docType, Equal<ARDocType.invoice>,
						Or<ARInvoice.docType, Equal<ARDocType.debitMemo>,
						Or<ARInvoice.docType, Equal<ARDocType.finCharge>>>>>();
					break;
				default:
					cmd.WhereAnd<Where<True, Equal<False>>>();
					break;
			}

			if (!PXAccess.FeatureInstalled<FeaturesSet.interBranch>() && currentARPayment.BranchID != null)
			{
				cmd.WhereAnd<Where<ARInvoice.branchID, In<Required<ARInvoice.branchID>>>>();
			}
			#endregion

			#region Parametrs
			List<object> parametrs = new List<object>();
			parametrs.Add(currentARPayment.DocType);
			parametrs.Add(currentARPayment.RefNbr);
			parametrs.Add(currentARPayment.DocType);
			if (opts != null)
			{
				if (opts.FromDate != null)
				{
					parametrs.Add(opts.FromDate);
				}
				if (opts.TillDate != null)
				{
					parametrs.Add(opts.TillDate);
				}
				if (!string.IsNullOrEmpty(opts.StartRefNbr))
				{
					parametrs.Add(opts.StartRefNbr);
				}
				if (!string.IsNullOrEmpty(opts.EndRefNbr))
				{
					parametrs.Add(opts.EndRefNbr);
				}
				if (opts.BranchID != null)
				{
					parametrs.Add(opts.BranchID);
				}
				else if (opts.OrganizationID != null)
				{
					int[] branchIDs = BranchMaint.GetChildBranches(graph, opts.OrganizationID).Select(o => o.BranchID.Value).ToArray();
					parametrs.Add(branchIDs);
				}
			}

			switch (loadChildDocs)
			{
				case LoadOptions.loadChildDocuments.IncludeCRM:
				case LoadOptions.loadChildDocuments.ExcludeCRM:
					parametrs.Add(currentARPayment.CustomerID);
					parametrs.Add(currentARPayment.CustomerID);
					break;
				default:
					parametrs.Add(currentARPayment.CustomerID);
					break;
			}

			if (!PXAccess.FeatureInstalled<FeaturesSet.interBranch>() && currentARPayment.BranchID!=null)
			{
				parametrs.Add(GL.Helpers.BranchHelper.GetBranchesToApplyDocuments(graph, currentARPayment.BranchID));
			}
			#endregion

			int count = 0;
			using (new PXFieldScope(cmd.View, typeof(ARInvoice.docType), typeof(ARInvoice.refNbr), typeof(ARTran.lineNbr)))
			{
				count = cmd.Select(parametrs.ToArray()).RowCount ?? 0;
			}
			return count;
		}

		private static int CompareCustDocs(
			ARPayment currentARPayment, 
			ARSetup currentARSetup, 
			LoadOptions opts,
			ARInvoice aInvoice, ARInvoice bInvoice,
			ARTran aTran, ARTran bTran)
		{
			int aSortOrder = 0;
			int bSortOrder = 0;

			bool isACredit = aInvoice.DocType == ARDocType.CreditMemo
				? aTran.LineNbr != null && aTran.CuryOrigTranAmt > 0m
				: aTran.LineNbr != null && aTran.CuryOrigTranAmt < 0m;
			bool isBCredit = bInvoice.DocType == ARDocType.CreditMemo
				? bTran.LineNbr != null && bTran.CuryOrigTranAmt > 0m
				: bTran.LineNbr != null && bTran.CuryOrigTranAmt < 0m;

			aSortOrder += currentARPayment.CuryOrigDocAmt > 0m && isACredit ? 0 : 10000;
			bSortOrder += currentARPayment.CuryOrigDocAmt > 0m && isBCredit ? 0 : 10000;
			
			if (currentARSetup.FinChargeFirst == true)
			{
				aSortOrder += aInvoice.DocType == ARDocType.FinCharge ? 0 : 1000;
				bSortOrder += bInvoice.DocType == ARDocType.FinCharge ? 0 : 1000;
			}

			DateTime aDueDate = aInvoice.DueDate ?? DateTime.MinValue;
			DateTime bDueDate = bInvoice.DueDate ?? DateTime.MinValue;

			object aObj;
			object bObj;

			string orderBy = opts?.OrderBy ?? LoadOptions.orderBy.DueDateRefNbr;

			switch (orderBy)
			{
				case LoadOptions.orderBy.RefNbr:

					aObj = aInvoice.RefNbr;
					bObj = bInvoice.RefNbr;
					aSortOrder += (1 + ((IComparable)aObj).CompareTo(bObj)) / 2;
					bSortOrder += (1 - ((IComparable)aObj).CompareTo(bObj)) / 2;
					break;

				case LoadOptions.orderBy.DocDateRefNbr:

					aObj = aInvoice.DocDate;
					bObj = bInvoice.DocDate;
					aSortOrder += (1 + ((IComparable)aObj).CompareTo(bObj)) / 2 * 10;
					bSortOrder += (1 - ((IComparable)aObj).CompareTo(bObj)) / 2 * 10;

					aObj = aInvoice.RefNbr;
					bObj = bInvoice.RefNbr;
					aSortOrder += (1 + ((IComparable)aObj).CompareTo(bObj)) / 2;
					bSortOrder += (1 - ((IComparable)aObj).CompareTo(bObj)) / 2;
					break;

				default:
					aSortOrder += (1 + aDueDate.CompareTo(bDueDate)) / 2 * 100;
					bSortOrder += (1 - aDueDate.CompareTo(bDueDate)) / 2 * 100;

					aObj = aInvoice.RefNbr;
					bObj = bInvoice.RefNbr;
					aSortOrder += (1 + ((IComparable)aObj).CompareTo(bObj)) / 2 * 10;
					bSortOrder += (1 - ((IComparable)aObj).CompareTo(bObj)) / 2 * 10;

					aObj = aTran.LineNbr ?? 0;
					bObj = bTran.LineNbr ?? 0;
					aSortOrder += (1 + ((IComparable)aObj).CompareTo(bObj)) / 2;
					bSortOrder += (1 - ((IComparable)aObj).CompareTo(bObj)) / 2;

					break;
			}

			return aSortOrder.CompareTo(bSortOrder);
		}

		public static PXResultset<ARInvoice> GetCustDocs(LoadOptions opts, ARPayment currentARPayment, ARSetup currentARSetup, PXGraph graph)
		{
			if (opts?.MaxDocs == 0 || currentARPayment == null) return new PXResultset<ARInvoice>();

			#region CMD
			var cmd = new PXSelectReadonly2<ARInvoice,
				InnerJoin<CurrencyInfo, On<CurrencyInfo.curyInfoID, Equal<ARInvoice.curyInfoID>>,
				InnerJoin<Customer, On<Customer.bAccountID, Equal<ARInvoice.customerID>>,
				LeftJoin<ARTran, On<ARInvoice.paymentsByLinesAllowed, Equal<True>,
					And<ARTran.tranType, Equal<ARInvoice.docType>,
					And<ARTran.refNbr, Equal<ARInvoice.refNbr>,
					And<ARTran.curyTranBal, NotEqual<decimal0>>>>>,
				LeftJoin<ARAdjust, On<ARAdjust.adjdDocType, Equal<ARInvoice.docType>,
					And<ARAdjust.adjdRefNbr, Equal<ARInvoice.refNbr>,
					And<ARAdjust.released, NotEqual<True>,
					And<ARAdjust.voided, NotEqual<True>,
					And<Where<ARAdjust.adjgDocType, NotEqual<Required<ARRegister.docType>>,
						Or<ARAdjust.adjgRefNbr, NotEqual<Required<ARRegister.refNbr>>>>>>>>>,
				LeftJoin<ARAdjust2, On<ARAdjust2.adjgDocType, Equal<ARInvoice.docType>,
					And<ARAdjust2.adjgRefNbr, Equal<ARInvoice.refNbr>,
					And<ARAdjust2.released, NotEqual<True>,
					And<ARAdjust2.voided, NotEqual<True>>>>>>>>>>,
				Where<ARInvoice.docType, NotEqual<Required<ARPayment.docType>>,
					And<ARInvoice.released, Equal<True>,
					And<ARInvoice.openDoc, Equal<True>,
					And<ARAdjust.adjgRefNbr, IsNull,
					And<ARAdjust2.adjgRefNbr, IsNull,
					And<ARInvoice.pendingPPD, NotEqual<True>,
					And<ARInvoice.isUnderCorrection, NotEqual<True>,
					And<Where<ARInvoice.paymentsByLinesAllowed, NotEqual<True>, 
						Or<ARTran.refNbr, IsNotNull>>>>>>>>>>,
					OrderBy<Asc<ARInvoice.dueDate, Asc<ARInvoice.refNbr, Asc<ARTran.refNbr>>>>>(graph);

			if (opts != null)
			{
				if (opts.FromDate != null)
				{
					cmd.WhereAnd<Where<ARInvoice.docDate, GreaterEqual<Required<LoadOptions.fromDate>>>>();
				}
				if (opts.TillDate != null)
				{
					cmd.WhereAnd<Where<ARInvoice.docDate, LessEqual<Required<LoadOptions.tillDate>>>>();
				}
				if (!string.IsNullOrEmpty(opts.StartRefNbr))
				{
					cmd.WhereAnd<Where<ARInvoice.refNbr, GreaterEqual<Required<LoadOptions.startRefNbr>>>>();
				}
				if (!string.IsNullOrEmpty(opts.EndRefNbr))
				{
					cmd.WhereAnd<Where<ARInvoice.refNbr, LessEqual<Required<LoadOptions.endRefNbr>>>>();
				}
				if (opts.BranchID != null)
				{
					cmd.WhereAnd<Where<ARInvoice.branchID, Equal<Required<LoadOptions.branchID>>>>();
				}
				else if (opts.OrganizationID != null)
				{
					cmd.WhereAnd<Where<ARInvoice.branchID, In<Required<LoadOptions.branchID>>>>();
				}
			}

			var loadChildDocs = opts == null ? LoadOptions.loadChildDocuments.None : opts.LoadChildDocuments;
			switch (loadChildDocs)
			{
				case LoadOptions.loadChildDocuments.IncludeCRM:
					cmd.WhereAnd<Where<ARInvoice.customerID, Equal<Required<ARRegister.customerID>>,
									Or<Customer.consolidatingBAccountID, Equal<Required<ARRegister.customerID>>>>>();
					break;
				case LoadOptions.loadChildDocuments.ExcludeCRM:
					cmd.WhereAnd<Where<ARInvoice.customerID, Equal<Required<ARRegister.customerID>>,
									Or<Customer.consolidatingBAccountID, Equal<Required<ARRegister.customerID>>,
										And<ARInvoice.docType, NotEqual<ARDocType.creditMemo>>>>>();
					break;
				default:
					cmd.WhereAnd<Where<ARInvoice.customerID, Equal<Required<ARRegister.customerID>>>>();
					break;
			}

			switch (currentARPayment.DocType)
			{
				case ARDocType.Payment:
					cmd.WhereAnd<Where<ARInvoice.docType, Equal<ARDocType.invoice>,
						Or<ARInvoice.docType, Equal<ARDocType.debitMemo>,
						Or<ARInvoice.docType, Equal<ARDocType.creditMemo>,
						Or<ARInvoice.docType, Equal<ARDocType.finCharge>>>>>>();
					break;
				case ARDocType.Prepayment:
					cmd.WhereAnd<Where<ARInvoice.docType, Equal<ARDocType.invoice>,
						Or<ARInvoice.docType, Equal<ARDocType.creditMemo>,
						Or<ARInvoice.docType, Equal<ARDocType.debitMemo>,
						Or<ARInvoice.docType, Equal<ARDocType.finCharge>>>>>>();
					break;
				case ARDocType.CreditMemo:
					cmd.WhereAnd<Where<ARInvoice.docType, Equal<ARDocType.invoice>,
						Or<ARInvoice.docType, Equal<ARDocType.debitMemo>,
						Or<ARInvoice.docType, Equal<ARDocType.finCharge>>>>>();
					break;
				default:
					cmd.WhereAnd<Where<True, Equal<False>>>();
					break;
			}

			if (!PXAccess.FeatureInstalled<FeaturesSet.interBranch>() && currentARPayment.BranchID != null)
			{
				cmd.WhereAnd<Where<ARInvoice.branchID, In<Required<ARInvoice.branchID>>>>();
			}
			#endregion

			#region Parametrs
			List<object> parametrs = new List<object>();
			parametrs.Add(currentARPayment.DocType);
			parametrs.Add(currentARPayment.RefNbr);
			parametrs.Add(currentARPayment.DocType);
			if (opts != null)
			{
				if (opts.FromDate != null)
				{
					parametrs.Add(opts.FromDate);
				}
				if (opts.TillDate != null)
				{
					parametrs.Add(opts.TillDate);
				}
				if (!string.IsNullOrEmpty(opts.StartRefNbr))
				{
					parametrs.Add(opts.StartRefNbr);
				}
				if (!string.IsNullOrEmpty(opts.EndRefNbr))
				{
					parametrs.Add(opts.EndRefNbr);
				}
				if (opts.BranchID != null)
				{
					parametrs.Add(opts.BranchID);
				}
				else if (opts.OrganizationID != null)
				{
					int[] branchIDs = BranchMaint.GetChildBranches(graph, opts.OrganizationID).Select(o => o.BranchID.Value).ToArray();
					parametrs.Add(branchIDs);
				}
			}

			switch (loadChildDocs)
			{
				case LoadOptions.loadChildDocuments.IncludeCRM:
				case LoadOptions.loadChildDocuments.ExcludeCRM:
					parametrs.Add(currentARPayment.CustomerID);
					parametrs.Add(currentARPayment.CustomerID);
					break;
				default:
					parametrs.Add(currentARPayment.CustomerID);
					break;
			}

			if (!PXAccess.FeatureInstalled<FeaturesSet.interBranch>() && currentARPayment.BranchID != null)
			{
				parametrs.Add(GL.Helpers.BranchHelper.GetBranchesToApplyDocuments(graph, currentARPayment.BranchID));
			}
			#endregion

			PXResultset<ARInvoice> custdocs = opts?.MaxDocs == null 
				? cmd.Select(parametrs.ToArray()) 
				: cmd.SelectWindowed(0, (int)opts.MaxDocs, parametrs.ToArray());

			#region Sort
			custdocs.Sort(new Comparison<PXResult<ARInvoice>>((a, b) =>
			{
				return CompareCustDocs(currentARPayment, currentARSetup, opts,
					PXResult.Unwrap<ARInvoice>(a),
					PXResult.Unwrap<ARInvoice>(b),
					PXResult.Unwrap<ARTran>(a),
					PXResult.Unwrap<ARTran>(b));
			}));
			#endregion
			return custdocs;
		}

		public virtual void LoadOptions_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			LoadOptions filter = (LoadOptions)e.Row;
			if (filter != null)
			{
				PXUIFieldAttribute.SetVisible<LoadOptions.startRefNbr>(sender, filter, filter.IsInvoice == true);
				PXUIFieldAttribute.SetVisible<LoadOptions.endRefNbr>(sender, filter, filter.IsInvoice == true);
				PXUIFieldAttribute.SetVisible<LoadOptions.orderBy>(sender, filter, filter.IsInvoice == true);
				PXUIFieldAttribute.SetVisible<LoadOptions.loadingMode>(sender, filter, filter.IsInvoice == true);
				PXUIFieldAttribute.SetVisible<LoadOptions.apply>(sender, filter, filter.IsInvoice == true);

				bool currentCustomerHasChildren =
					PXAccess.FeatureInstalled<FeaturesSet.parentChildAccount>() &&
					Document.Current != null && Document.Current.CustomerID != null &&
					((Customer)PXSelect<Customer, Where<Customer.parentBAccountID, Equal<Required<Customer.parentBAccountID>>,
													And<Customer.consolidateToParent, Equal<True>>>>
						.Select(this, Document.Current.CustomerID) != null);

				PXUIFieldAttribute.SetVisible<LoadOptions.loadChildDocuments>(sender, filter, filter.IsInvoice == true && currentCustomerHasChildren);
				PXUIFieldAttribute.SetVisible<LoadOptions.startOrderNbr>(sender, filter, filter.IsInvoice == false);
				PXUIFieldAttribute.SetVisible<LoadOptions.endOrderNbr>(sender, filter, filter.IsInvoice == false);
				PXUIFieldAttribute.SetVisible<LoadOptions.sOOrderBy>(sender, filter, filter.IsInvoice == false);
			}
		}

		#region LoadInvoices
		public PXAction<ARPayment> loadInvoices;
		[PXUIField(DisplayName = "Load Documents", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.Refresh)]
		[ARMigrationModeDependentActionRestriction(
			restrictInMigrationMode: true,
			restrictForRegularDocumentInMigrationMode: true,
			restrictForUnreleasedMigratedDocumentInNormalMode: true)]
		public virtual IEnumerable LoadInvoices(PXAdapter adapter)
		{
			if (loadOpts != null && loadOpts.Current != null)
			{
				loadOpts.Current.IsInvoice = true;
			}

			string errmsg = PXUIFieldAttribute.GetError<LoadOptions.branchID>(loadOpts.Cache, loadOpts.Current);

			if (string.IsNullOrEmpty(errmsg) == false)
				throw new PXException(errmsg);

			var res = loadOpts.AskExt();
			if (res == WebDialogResult.OK || res == WebDialogResult.Yes)
			{
				switch (res)
				{
					case WebDialogResult.OK:
						loadOpts.Current.LoadingMode = LoadOptions.loadingMode.Load;
						break;
					case WebDialogResult.Yes:
						loadOpts.Current.LoadingMode = LoadOptions.loadingMode.Reload;
						break;
				}
				LoadInvoicesExtProc(false, loadOpts.Current);
				if (loadOpts.Current?.Apply == true)
					AutoApplyProc(((ARPayment)Document.Current).CuryOrigDocAmt != 0m); // CuryUnappliedBal
			}
			return adapter.Get();
		}
		#endregion

		#region AutoApply
		public PXAction<ARPayment> autoApply;
		[PXUIField(DisplayName = "Auto Apply", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.Refresh)]
		[ARMigrationModeDependentActionRestriction(
			restrictInMigrationMode: true,
			restrictForRegularDocumentInMigrationMode: true,
			restrictForUnreleasedMigratedDocumentInNormalMode: true)]
		public virtual IEnumerable AutoApply(PXAdapter adapter)
		{
			AutoApplyProc(Document.Current.CuryOrigDocAmt != 0m); // CuryUnappliedBal
			return adapter.Get();
		}

		public virtual void AutoApplyProc(bool checkBalance = true)
		{
			PXCache cachePayment = Document.Cache;
			ARPayment payment = Document.Current;

			decimal curyUnappliedBal = payment.CuryUnappliedBal.Value;

			try
			{
				#region unapply documents before apply again
				InternalCall = true;    // exclude ARPayment_RowSelected 
				if (curyUnappliedBal < 0m)   // unapply documents before apply again
				{
					foreach (ARAdjust adj in Adjustments.View.SelectExternal().RowCast<ARAdjust>()
						.Where(adj => (adj.CuryAdjgAmt != 0) && (adj.Released != true && adj.Voided != true)))
			{
						if (adj.CuryAdjgPPDAmt != 0)
						{
							adj.CuryAdjgPPDAmt = 0; // Adjustments.Cache.SetValueExt<ARAdjust.curyAdjgPPDAmt>(adj, 0); //  set Amt and Selected
							adj.FillDiscAmts();
						}
						Adjustments.Cache.SetValueExt<ARAdjust.curyAdjgAmt>(adj, 0m); //  set Amt and Selected
						Adjustments.Cache.Update(adj);
					}
				}
				#endregion

				foreach (PXResult<ARAdjust> adjResult in Adjustments.View.SelectExternal())
					PXSelectorAttribute.StoreResult<ARAdjust.adjdRefNbr>(Adjustments.Cache, adjResult, adjResult.GetItem<ARInvoice>());
				RecalcApplAmounts(cachePayment, payment);
				curyUnappliedBal = payment.CuryUnappliedBal.Value;

				decimal difCuryUnappliedBal = 0m;

				var adjustments = Adjustments.View.SelectExternal()
					.Cast<PXResult<ARAdjust>>()
					.Where(adj => ((ARAdjust)adj).Released != true && ((ARAdjust)adj).Voided != true)
					.ToList();
				adjustments.Sort(new Comparison<PXResult<ARAdjust>>((a, b) =>
				{
					return CompareCustDocs(payment, arsetup.Current, null,
						PXResult.Unwrap<ARInvoice>(a),
						PXResult.Unwrap<ARInvoice>(b),
						PXResult.Unwrap<ARTran>(a),
						PXResult.Unwrap<ARTran>(b));
				}));

				foreach (ARAdjust adj in adjustments)
				{
					if (adj.Selected == true && adj.CuryDocBal == 0m)
						continue;

					difCuryUnappliedBal = applyARAdjust(adj, curyUnappliedBal, checkBalance, true);
					if (adj.Selected == false)
						break;

					curyUnappliedBal += difCuryUnappliedBal;

					Adjustments.Cache.Update(adj);
			}

				RecalcApplAmounts(cachePayment, payment);
				IsPaymentUnbalancedException(cachePayment, payment);
			}
			finally
			{
				InternalCall = false;    // include ARPayment_RowSelected 
			}
		}
		#endregion

		#region AdjustDocAmt
		public PXAction<ARPayment> adjustDocAmt;
		[PXUIField(DisplayName = "Set Payment Amount to Applied to Documents amount", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.Refresh)]
		[ARMigrationModeDependentActionRestriction(
			restrictInMigrationMode: true,
			restrictForRegularDocumentInMigrationMode: true,
			restrictForUnreleasedMigratedDocumentInNormalMode: true)]
		public virtual IEnumerable AdjustDocAmt(PXAdapter adapter)
		{
			ARPayment payment = (ARPayment)Document.Cache.CreateCopy(Document.Current);
			if (payment.CuryUnappliedBal != 0)
			{
				payment.CuryOrigDocAmt = payment.CuryOrigDocAmt - payment.CuryUnappliedBal;
				Document.Cache.Update(payment);
			}
			return adapter.Get();
		}
		#endregion

		public PXAction<ARPayment> reverseApplication;
		[PXUIField(DisplayName = "Reverse Application", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.Refresh)]
		[ARMigrationModeDependentActionRestriction(
			restrictInMigrationMode: false,
			restrictForRegularDocumentInMigrationMode: true,
			restrictForUnreleasedMigratedDocumentInNormalMode: true)]
		public virtual IEnumerable ReverseApplication(PXAdapter adapter)
		{
			ARPayment payment = Document.Current;
			ARAdjust application = PXSelect<ARAdjust, 
				Where<ARAdjust.noteID, Equal<Current<ARTranPostBal.refNoteID>>>>
				.SelectSingleBound(this, new object[]{});

			ReverseApplicationProc(application, payment);

			return adapter.Get();
		}

		protected virtual void ReverseApplicationProc(ARAdjust application, ARPayment payment)
		{
			if (application == null) return;

			if (application.AdjType == ARAdjust.adjType.Adjusting)
			{
				throw new PXException(
					Common.Messages.IncomingApplicationCannotBeReversed,
					GetLabel.For<ARDocType>(payment.DocType),
					GetLabel.For<ARDocType>(application.AdjgDocType),
					application.AdjgRefNbr);
			}

			if (application.IsInitialApplication == true)
			{
				throw new PXException(Common.Messages.InitialApplicationCannotBeReversed);
			}
			else if (application.IsMigratedRecord != true &&
				arsetup.Current.MigrationMode == true)
			{
				throw new PXException(Messages.CannotReverseRegularApplicationInMigrationMode);
			}

			if (application.Voided != true
				&& (
					ARPaymentType.CanHaveBalance(application.AdjgDocType)
					|| application.AdjgDocType == ARDocType.Refund))
			{
				if (payment != null
					&& (payment.DocType != ARDocType.CreditMemo || payment.PendingPPD != true)
					&& application.AdjdHasPPDTaxes == true
					&& application.PendingPPD != true)
				{
					ARAdjust adjPPD = GetPPDApplication(this, application.AdjdDocType, application.AdjdRefNbr);

					if (adjPPD != null)
					{
						throw new PXSetPropertyException(Messages.PPDApplicationExists, adjPPD.AdjgRefNbr);
					}
				}

				if (Document.Current.OpenDoc == false)
				{
					Document.Current.OpenDoc = true;
					DateTime? tmpDateTime = payment.AdjDate;
					Document.Cache.RaiseRowSelected(payment);
					Document.Cache.SetValueExt<ARPayment.adjDate>(payment, tmpDateTime);
				}

				CreateReversingApp(application, payment);
			}
		}

		public static ARAdjust GetPPDApplication(PXGraph graph, string DocType, string RefNbr)
		{
			return PXSelect<ARAdjust, Where<ARAdjust.adjdDocType, Equal<Required<ARAdjust.adjdDocType>>,
				And<ARAdjust.adjdRefNbr, Equal<Required<ARAdjust.adjdRefNbr>>,
				And<ARAdjust.released, Equal<True>,
				And<ARAdjust.voided, NotEqual<True>,
				And<ARAdjust.pendingPPD, Equal<True>>>>>>>.SelectSingleBound(graph, null, DocType, RefNbr);
		}

		public PXAction<ARPayment> viewDocumentToApply;
		[PXUIField(DisplayName = "View Document", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXLookupButton(OnClosingPopup = PXSpecialButtonType.Refresh)]
		public virtual IEnumerable ViewDocumentToApply(PXAdapter adapter)
		{
			ARAdjust row = Adjustments.Current;
			if (!string.IsNullOrEmpty(row?.AdjdDocType) && !string.IsNullOrEmpty(row?.AdjdRefNbr))
			{
				if (row.AdjdDocType == ARDocType.Payment || (row.AdjgDocType == ARDocType.Refund && row.AdjdDocType == ARDocType.Prepayment))
				{
					ARPaymentEntry iegraph = PXGraph.CreateInstance<ARPaymentEntry>();
					iegraph.Document.Current = iegraph.Document.Search<ARPayment.refNbr>(row.AdjdRefNbr, row.AdjdDocType);
					throw new PXRedirectRequiredException(iegraph, true, "View Document") { Mode = PXBaseRedirectException.WindowMode.NewWindow };
				}
				else
				{
					SO.SOInvoice soInvoice = SO.SOInvoice.PK.Find(this, row.AdjdDocType, row.AdjdRefNbr);

					ARInvoiceEntry iegraph = soInvoice != null ? CreateInstance<SO.SOInvoiceEntry>() : CreateInstance<ARInvoiceEntry>();
					iegraph.Document.Current = iegraph.Document.Search<ARInvoice.refNbr>(row.AdjdRefNbr, row.AdjdDocType);
					throw new PXRedirectRequiredException(iegraph, true, "View Document") { Mode = PXBaseRedirectException.WindowMode.NewWindow };
				}
			}

			return adapter.Get();
		}

		public PXAction<ARPayment> viewApplicationDocument;
		[PXUIField(DisplayName = "View Application Document", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXLookupButton()]
		public virtual IEnumerable ViewApplicationDocument(PXAdapter adapter)
		{
			ARTranPostBal post = this.ARPost.Current;

			if (post == null ||
				string.IsNullOrEmpty(post.SourceDocType) ||
				string.IsNullOrEmpty(post.SourceRefNbr))
			{
				return adapter.Get();
			}

			PXGraph targetGraph;

			switch (post.SourceDocType)
			{
				case ARDocType.Payment:
				case ARDocType.Prepayment:
				case ARDocType.Refund:
				case ARDocType.VoidRefund:
				case ARDocType.VoidPayment:
				case ARDocType.SmallBalanceWO:
					{
						ARPaymentEntry documentGraph = CreateInstance<ARPaymentEntry>();
						documentGraph.Document.Current = documentGraph.Document.Search<ARPayment.refNbr>(
							post.SourceRefNbr,
							post.SourceDocType);

						targetGraph = documentGraph;
						break;
					}
				default:
					{
						SO.SOInvoice soInvoice = SO.SOInvoice.PK.Find(this, post.SourceDocType, post.SourceRefNbr);

						ARInvoiceEntry documentGraph = soInvoice != null ? CreateInstance<SO.SOInvoiceEntry>() : CreateInstance<ARInvoiceEntry>();
						documentGraph.Document.Current = documentGraph.Document.Search<ARInvoice.refNbr>(
							post.SourceRefNbr,
							post.SourceDocType);

						targetGraph = documentGraph;
						break;
					}
			}

			throw new PXRedirectRequiredException(
				targetGraph,
				true,
				"View Application Document")
			{
				Mode = PXBaseRedirectException.WindowMode.NewWindow
			};
		}

		public PXAction<ARPayment> viewCurrentBatch;
		[PXUIField(DisplayName = "View Batch", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXLookupButton]
		public virtual IEnumerable ViewCurrentBatch(PXAdapter adapter)
		{
			ARTranPostBal row = ARPost.Current;
			if (row != null && !String.IsNullOrEmpty(row.BatchNbr))
			{
				JournalEntry graph = PXGraph.CreateInstance<JournalEntry>();
				graph.BatchModule.Current = PXSelect<Batch,
										Where<Batch.module, Equal<BatchModule.moduleAR>,
										And<Batch.batchNbr, Equal<Required<Batch.batchNbr>>>>>
										.Select(this, row.BatchNbr);
				if (graph.BatchModule.Current != null)
				{
					throw new PXRedirectRequiredException(graph, true, "View Batch") { Mode = PXBaseRedirectException.WindowMode.NewWindow };
				}
			}
			return adapter.Get();
		}

		public PXAction<ARPayment> ViewOriginalDocument;

		[PXUIField(Visible = false, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXLookupButton]
		protected virtual IEnumerable viewOriginalDocument(PXAdapter adapter)
		{
			RedirectionToOrigDoc.TryRedirect(Document.Current?.OrigDocType, Document.Current?.OrigRefNbr, Document.Current?.OrigModule);
			return adapter.Get();
		}

		public static void CheckValidPeriodForCCTran(PXGraph graph, ARPayment payment)
		{
			DateTime trandate = PXTimeZoneInfo.Today;

			FinPeriod finPeriod = graph.GetService<IFinPeriodRepository>().FindFinPeriodByDate(trandate, PXAccess.GetParentOrganizationID(payment.BranchID));
			if (finPeriod == null)
			{
				throw new PXException(Messages.CannotCaptureInInvalidPeriod, trandate.ToString("d", graph.Culture), GL.Messages.FinPeriodForBranchOrCompanyDoesNotExist);
			}
			try
			{
				graph.GetService<IFinPeriodUtils>().CanPostToPeriod(finPeriod, typeof(FinPeriod.aRClosed));
			}
			catch (PXException e)
			{
				throw new PXException(Messages.CannotCaptureInInvalidPeriod, trandate.ToString("d", graph.Culture), e.MessageNoPrefix);
			}
		}
		#endregion

		protected virtual void CATran_CashAccountID_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			e.Cancel = true;
		}

		protected virtual void CATran_FinPeriodID_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			e.Cancel = true;
		}

		protected virtual void CATran_TranPeriodID_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			e.Cancel = true;
		}

		protected virtual void CATran_ReferenceID_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			e.Cancel = true;
		}

		protected virtual void CATran_CuryID_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			e.Cancel = true;
		}

		protected virtual void ARPayment_DocType_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			e.NewValue = ARDocType.Payment;
		}

		protected virtual Dictionary<Type, Type>  CreateApplicationMap(bool invoiceSide)
		{
			if (invoiceSide)
				return new Dictionary<Type, Type>
				{
					{ typeof(ARAdjust.displayDocType), typeof(ARAdjust.adjdDocType) },
					{ typeof(ARAdjust.displayRefNbr), typeof(ARAdjust.adjdRefNbr) },
					{ typeof(ARAdjust.displayCustomerID), typeof(ARInvoice.customerID) },
					{ typeof(ARAdjust.displayDocDate), typeof(ARInvoice.docDate) },
					{ typeof(ARAdjust.displayDocDesc), typeof(ARInvoice.docDesc) },
					{ typeof(ARAdjust.displayCuryID), typeof(ARInvoice.curyID) },
					{ typeof(ARAdjust.displayFinPeriodID), typeof(ARInvoice.finPeriodID) },
					{ typeof(ARAdjust.displayStatus), typeof(ARInvoice.status) },
					{ typeof(ARAdjust.displayCuryInfoID), typeof(ARInvoice.curyInfoID) },
					{ typeof(ARAdjust.displayCuryAmt), typeof(ARAdjust.curyAdjgAmt) },
					{ typeof(ARAdjust.displayCuryWOAmt), typeof(ARAdjust.curyAdjgWOAmt) },
					{ typeof(ARAdjust.displayCuryPPDAmt), typeof(ARAdjust.curyAdjgPPDAmt) },
				};
			else
			{
				return new Dictionary<Type, Type>
				{
					{ typeof(ARAdjust.displayDocType), typeof(ARAdjust.adjgDocType)},
					{ typeof(ARAdjust.displayRefNbr), typeof(ARAdjust.adjgRefNbr)},
					{ typeof(ARAdjust.displayCustomerID), typeof(ARPayment.customerID) },
					{ typeof(ARAdjust.displayDocDate), typeof(ARPayment.docDate) },
					{ typeof(ARAdjust.displayDocDesc), typeof(ARPayment.docDesc) },
					{ typeof(ARAdjust.displayCuryID), typeof(ARPayment.curyID) },
					{ typeof(ARAdjust.displayFinPeriodID), typeof(ARPayment.finPeriodID) },
					{ typeof(ARAdjust.displayStatus), typeof(ARPayment.status) },
					{ typeof(ARAdjust.displayCuryInfoID), typeof(ARPayment.curyInfoID) },
					{ typeof(ARAdjust.displayCuryAmt), typeof(ARAdjust.curyAdjdAmt) },
					{ typeof(ARAdjust.displayCuryWOAmt), typeof(ARAdjust.curyAdjdWOAmt) },
					{ typeof(ARAdjust.displayCuryPPDAmt), typeof(ARAdjust.curyAdjdPPDAmt) },
				};
			}

		}

		[Api.Export.PXOptimizationBehavior(IgnoreBqlDelegate = true)]
		protected virtual IEnumerable adjustments()
		{
			FillBalanceCache(this.Document.Current);

			int startRow = PXView.StartRow;
			int totalRows = 0;

			Adjustments_Invoices_BeforeSelect();

			if (Document.Current == null || (Document.Current.DocType != ARDocType.Refund && Document.Current.DocType != ARDocType.VoidRefund))
			{
				PXResultMapper mapper = new PXResultMapper(this,
					CreateApplicationMap(true),
					typeof(ARAdjust), typeof(ARInvoice), 
					typeof(Standalone.ARRegisterAlias), typeof(ARTran));
				var result = mapper.CreateDelegateResult(true);
				foreach (PXResult<ARAdjust, ARInvoice, Standalone.ARRegisterAlias, ARTran> res in
					Adjustments_Invoices.View.Select(
						PXView.Currents,
						null,
						mapper.Searches,
						mapper.SortColumns,
						mapper.Descendings,
						mapper.Filters,
						ref startRow,
						PXView.MaximumRows,
						ref totalRows))
				{
					ARInvoice fullInvoice = res;
					ARTran tran = res;
					PXCache<ARRegister>.RestoreCopy(fullInvoice, (Standalone.ARRegisterAlias)res);

					// allow to update ARInvoice in popup window
					// and keep DueDate and Discount date - the only values, that can be changed
					// in popup for released invoice
					ARInvoice located = (ARInvoice)Caches[typeof(ARInvoice)].Locate(fullInvoice);
					if (located != null
						&& Caches[typeof(ARInvoice)].GetStatus(located) == PXEntryStatus.Updated
						&& !Caches[typeof(ARInvoice)].ObjectsEqual<ARInvoice.Tstamp, ARInvoice.dueDate, ARInvoice.discDate>(fullInvoice, located)
						)
					{
						Caches[typeof(ARInvoice)].SetValue<ARInvoice.Tstamp>(located, fullInvoice.tstamp);
						Caches[typeof(ARInvoice)].SetValue<ARInvoice.dueDate>(located, fullInvoice.DueDate);
						Caches[typeof(ARInvoice)].SetValue<ARInvoice.discDate>(located, fullInvoice.DiscDate);
					}

					if (Adjustments.Cache.GetStatus((ARAdjust)res) == PXEntryStatus.Notchanged)
					{
						GetExtension<ARPaymentEntryDocumentExtension>().CalcBalances(res, fullInvoice, true, false, tran);
					}
					result.Add(
						mapper.CreateResult(new PXResult<ARAdjust, ARInvoice, Standalone.ARRegisterAlias, ARTran>
						(res, fullInvoice, res, tran))
						);
				}
				PXView.StartRow = 0;
				return result;
			}
			else
			{
				PXResultMapper mapper = new PXResultMapper(this, 
					CreateApplicationMap(false), 
					typeof(ARAdjust), typeof(ARPayment), typeof(Standalone.ARRegisterAlias));
				var result = mapper.CreateDelegateResult(true);
				foreach (PXResult<ARAdjust, ARPayment, Standalone.ARRegisterAlias> res in
					Adjustments_Payments.View.Select(
						PXView.Currents,
						null,
						mapper.Searches,
						mapper.SortColumns,
						mapper.Descendings,
						mapper.Filters,
						ref startRow,
						PXView.MaximumRows,
						ref totalRows))
				{
					ARPayment fullPayment = res;
					PXCache<ARRegister>.RestoreCopy(fullPayment, (Standalone.ARRegisterAlias)res);

					if (Adjustments.Cache.GetStatus((ARAdjust)res) == PXEntryStatus.Notchanged)
					{
						GetExtension<ARPaymentEntryDocumentExtension>().CalcBalances(res, fullPayment, true, false, null);
					}

					result.Add(
						mapper.CreateResult(new PXResult<ARAdjust, ARPayment, Standalone.ARRegisterAlias>
						(res, fullPayment, res))
						);
			}
			PXView.StartRow = 0;
				return result;
			}
		}

		[Obsolete(InternalMessages.MethodIsObsoleteAndWillBeRemoved2022R2)]
		protected virtual IEnumerable adjustments_history()
		{
			PXResultset<ARAdjust> resultSet = new PXResultset<ARAdjust>();
			if (IsImport) return resultSet; //no place for output on Import

			FillBalanceCache(this.Document.Current, true);


			BqlCommand outgoingApplications = new Select2<
				ARAdjust,
					LeftJoinSingleTable<ARInvoice,
						On<ARInvoice.docType, Equal<ARAdjust.adjdDocType>,
						And<ARInvoice.refNbr, Equal<ARAdjust.adjdRefNbr>>>,
					LeftJoin<ARRegisterAlias,
						On<ARRegisterAlias.docType, Equal<ARAdjust.adjdDocType>,
						And<ARRegisterAlias.refNbr, Equal<ARAdjust.adjdRefNbr>>>,
					LeftJoin<ARTran,
						On<ARTran.tranType, Equal<ARAdjust.adjdDocType>,
						And<ARTran.refNbr, Equal<ARAdjust.adjdRefNbr>,
						And<ARTran.lineNbr, Equal<ARAdjust.adjdLineNbr>>>>,
					LeftJoin<BAccountR, On<BAccountR.bAccountID, Equal<ARRegisterAlias.customerID>>>>>>,									
				Where<
					ARAdjust.adjgDocType, Equal<Optional<ARPayment.docType>>,
					And<ARAdjust.adjgRefNbr, Equal<Optional<ARPayment.refNbr>>,
					And<ARAdjust.released, Equal<True>,
					And<ARAdjust.isInitialApplication, NotEqual<True>>>>>>();

			outgoingApplications.EnsureParametersEqual(Adjustments_History.View.BqlSelect);
			PXResultMapper mapper = new PXResultMapper(this,
					CreateApplicationMap(true),
					typeof(ARAdjust), 
					typeof(ARInvoice),
					typeof(ARTran));
			PXView outgoingApplicationsView = new PXView(this, true, outgoingApplications);

			foreach (PXResult<ARAdjust, ARInvoice, ARRegisterAlias, ARTran, BAccountR> result in outgoingApplicationsView.SelectMulti(PXView.Parameters))
			{
				ARInvoice fullInvoice = result;
				ARTran tran = result;
				BAccountR customer = result;
				PXCache<ARRegister>.RestoreCopy(fullInvoice, (ARRegisterAlias)result);

				resultSet.Add(
					(PXResult<ARAdjust>)mapper.CreateResult(
					new PXResult<ARAdjust, ARInvoice, ARTran, BAccountR>(result, fullInvoice, tran,customer)));
			}

			BqlCommand incomingApplications = new Select2<
				ARAdjust,
					InnerJoinSingleTable<ARPayment,
						On<ARPayment.docType, Equal<ARAdjust.adjgDocType>,
						And<ARPayment.refNbr, Equal<ARAdjust.adjgRefNbr>>>,
					InnerJoin<ARRegisterAlias,
						On<ARRegisterAlias.docType, Equal<ARAdjust.adjgDocType>,
						And<ARRegisterAlias.refNbr, Equal<ARAdjust.adjgRefNbr>>>,
					LeftJoin<CurrencyInfo,
						On<ARRegisterAlias.curyInfoID, Equal<CurrencyInfo.curyInfoID>>,
					LeftJoin<BAccountR,
							On<BAccountR.bAccountID, Equal<ARRegisterAlias.customerID>>>>>>,
				Where<
					ARAdjust.adjdDocType, Equal<Optional<ARPayment.docType>>,
					And<ARAdjust.adjdRefNbr, Equal<Optional<ARPayment.refNbr>>,
					And<ARAdjust.released, Equal<True>>>>>();
			mapper = new PXResultMapper(this,
				CreateApplicationMap(false), typeof(ARAdjust), typeof(ARPayment), typeof(Standalone.ARRegisterAlias), typeof(ARTran));
			PXView incomingApplicationsView = new PXView(this, true, incomingApplications);

			foreach (PXResult<ARAdjust, ARPayment, ARRegisterAlias, CurrencyInfo, BAccountR> result in incomingApplicationsView.SelectMulti(PXView.Parameters))
			{
				ARAdjust incomingApplication = result;
				ARPayment appliedPayment = result;
				CurrencyInfo paymentCurrencyInfo = result;
				BAccountR customer = result;
				PXCache<ARRegister>.RestoreCopy(appliedPayment, (ARRegisterAlias)result);

				BalanceCalculation.CalculateApplicationDocumentBalance(
					appliedPayment,
					incomingApplication,
					paymentCurrencyInfo,
					GetExtension<MultiCurrency>().GetDefaultCurrencyInfo());

				resultSet.Add(
					(PXResult<ARAdjust>)mapper.CreateResult(
					new PXResult<ARAdjust, ARPayment, ARTran, BAccountR>(incomingApplication, appliedPayment, null, customer)));
			}

			return resultSet;
		}

		public virtual IEnumerable arpost()
		{
			using (var scope = new PXReadBranchRestrictedScope())
			{
				return this.ARPost.View.QuickSelect();
			}
		}

		public ARPaymentEntry()
			: base()
		{
			LoadOptions opt = loadOpts.Current;
			ARSetup setup = arsetup.Current;
			OpenPeriodAttribute.SetValidatePeriod<ARPayment.adjFinPeriodID>(Document.Cache, null, PeriodValidation.DefaultSelectUpdate);

			setup.CreditCheckError = false;
			ForcePaymentApp = ForcePaymentAppScope.IsActive;
		}

		public override int ExecuteUpdate(string viewName, IDictionary keys, IDictionary values, params object[] parameters)
		{
			if (viewName.Equals(nameof(Document), StringComparison.OrdinalIgnoreCase)
				&& values != null)
			{
				values[nameof(ARPayment.CurySOApplAmt)] = PXCache.NotSetValue;
				values[nameof(ARPayment.CuryApplAmt)] = PXCache.NotSetValue;
				values[nameof(ARPayment.CuryWOAmt)] = PXCache.NotSetValue;
				values[nameof(ARPayment.CuryUnappliedBal)] = PXCache.NotSetValue;
			}

			return base.ExecuteUpdate(viewName, keys, values, parameters);
		}

		public override void Persist()
		{
			// condition of deleting ARAdjust like in ARReleaseProcess.ProcessAdjustments
			foreach (var adjres in Adjustments.Select())
			{
				ARAdjust adj = adjres;
				ARRegisterAlias adjustedInvoice = PXResult.Unwrap<ARRegisterAlias>(adjres);
				if (adj.CuryAdjgAmt == 0m && adj.CuryAdjgDiscAmt == 0m
						&& (!adjustedInvoice.IsOriginalRetainageDocument() || adjustedInvoice.RetainageUnreleasedAmt != 0 || adjustedInvoice.RetainageReleased != 0))
				{
					Adjustments.Cache.Delete(adj);
				}
			}

			foreach (ARAdjust adj in Adjustments_History.Select())
			{
				PXDBDefaultAttribute.SetDefaultForUpdate<ARAdjust.adjgDocDate>(Adjustments_History.Cache, adj, false);
			}

			foreach (ARPayment doc in Document.Cache.Updated)
			{
				if (doc.SelfVoidingDoc == true)
				{
					int countAdjVoided = Adjustments_Raw.Select(doc.DocType, doc.RefNbr).Count(adj => ((ARAdjust)adj).Voided == true);
					int countAdjHistory = ARPost.Select().Count();
					if (countAdjVoided > 0 && countAdjVoided != countAdjHistory)
					{
						throw new PXException(Messages.SelfVoidingDocPartialReverse);
					}
				}

				if (doc.OpenDoc == true && 
					((bool?)Document.Cache.GetValueOriginal<ARPayment.openDoc>(doc) == false || doc.DocBal == 0 && doc.UnappliedBal == 0 && doc.Released == true && doc.Hold == false) && 
					Adjustments_Raw.SelectSingle(doc.DocType, doc.RefNbr) == null)
				{
					doc.OpenDoc = false;
					Document.Cache.RaiseRowSelected(doc);
				}
			}

			var contextExt = this.GetExtension<ARPaymentEntry.ARPaymentContextExtention>();
			contextExt.GraphContext = ARPaymentEntry.ARPaymentContextExtention.Context.Persist;

			base.Persist();

			contextExt.GraphContext = ARPaymentEntry.ARPaymentContextExtention.Context.None;

			using (PXTransactionScope ts = new PXTransactionScope())
			{
				Caches[typeof(CADailySummary)].Persist(PXDBOperation.Insert);
				ts.Complete(this);
			}

			Caches[typeof(CADailySummary)].Persisted(false);

		}

		protected virtual void CurrencyInfo_CuryID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			if (PXAccess.FeatureInstalled<FeaturesSet.multicurrency>())
			{
				if (cashaccount.Current != null && !string.IsNullOrEmpty(cashaccount.Current.CuryID))
				{
					e.NewValue = cashaccount.Current.CuryID;
					e.Cancel = true;
				}
			}
		}

		protected virtual void CurrencyInfo_CuryRateTypeID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			if (PXAccess.FeatureInstalled<FeaturesSet.multicurrency>())
			{
				if (cashaccount.Current != null && !string.IsNullOrEmpty(cashaccount.Current.CuryRateTypeID))
				{
					e.NewValue = cashaccount.Current.CuryRateTypeID;
					e.Cancel = true;
				}
			}
		}

		protected virtual void CurrencyInfo_CuryEffDate_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			if (Document.Cache.Current != null)
			{
				e.NewValue = ((ARPayment)Document.Cache.Current).DocDate;
				e.Cancel = true;
			}
		}

		#region ARPayment Events
		protected virtual void ARPayment_CustomerID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			customer.RaiseFieldUpdated(sender, e.Row);
			const string WARNING_DIALOG_KEY = "RowCountWarningDialog";

			sender.SetDefaultExt<ARInvoice.customerLocationID>(e.Row);
			sender.SetDefaultExt<ARPayment.paymentMethodID>(e.Row);       //Payment method should be defaulted first to correctly set CashAccount and pass account checks for AR Account

			ARPayment payment = (ARPayment)e.Row;

			if (e.ExternalCall
				 && payment.IsMigratedRecord != true
				 && customer.Current?.AutoApplyPayments == false
				 && !this.IsContractBasedAPI
				 && !this.IsMobile
				 && !this.IsProcessing
				 && !this.IsImport
				 && !this.UnattendedMode
				 && arsetup.Current.AutoLoadMaxDocs > 0)
			{
				if (Document.View.GetAnswer(WARNING_DIALOG_KEY) != WebDialogResult.None)
				{
					Document.ClearDialog();
					return;
				}

				var opt = new LoadOptions() { Apply = false, MaxDocs = arsetup.Current.AutoLoadMaxDocs };
				int count = GetCustDocsCount(opt, payment, arsetup.Current, this);
				if (count > opt.MaxDocs)
				{
					string message = PXAccess.FeatureInstalled<FeaturesSet.paymentsByLines>() 
						? PXLocalizer.LocalizeFormat(Messages.CustomerHasXOpenRowsToApply, customer.Current?.AcctCD, count)
						: PXLocalizer.LocalizeFormat(Messages.CustomerHasXOpenDocumentsToApply, customer.Current?.AcctCD, count);
					sender.RaiseExceptionHandling<ARPayment.customerID>(e.Row, payment.CustomerID, new PXSetPropertyException(message, PXErrorLevel.Warning));
					Document.Ask(WARNING_DIALOG_KEY, Messages.Warning, message, MessageButtons.OK, MessageIcon.Warning);
				}
				else
				{
					sender.RaiseExceptionHandling<ARPayment.customerID>(e.Row, payment.CustomerID, null);
					if (count > 0)
					{
						LoadInvoicesExtProc(false, opt); // load existing and invoices of new customers                    
					}
				}
			}
		}

		protected virtual void ARPayment_CustomerLocationID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			location.RaiseFieldUpdated(sender, e.Row);

			sender.SetDefaultExt<ARPayment.aRAccountID>(e.Row);
			sender.SetDefaultExt<ARPayment.aRSubID>(e.Row);
		}

		protected virtual void ARPayment_Hold_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			ARPayment doc = e.Row as ARPayment;
			if (PXAccess.FeatureInstalled<FeaturesSet.approvalWorkflow>())
			{
				if (this.Approval.GetAssignedMaps(doc, sender).Any())
				{
					sender.SetValue<ARPayment.hold>(doc, true);
				}
			}
		}
		protected virtual void ARPayment_ExtRefNbr_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			ARPayment row = (ARPayment)e.Row;
			if (row == null) return;

			if (row.DocType == ARDocType.VoidPayment || row.DocType == ARDocType.VoidRefund)
			{
				//avoid webdialog in PaymentRef attribute
				e.Cancel = true;
			}
			else
			{
				if (string.IsNullOrEmpty((string)e.NewValue) == false && string.IsNullOrEmpty(row.PaymentMethodID) == false)
				{
					PaymentMethod pm = this.paymentmethod.Current;
					ARPayment dup = null;
					if (pm.IsAccountNumberRequired == true)
					{
						dup = PXSelectReadonly<ARPayment, Where<ARPayment.customerID, Equal<Current<ARPayment.customerID>>,
											And<ARPayment.pMInstanceID, Equal<Current<ARPayment.pMInstanceID>>,
											And<ARPayment.extRefNbr, Equal<Required<ARPayment.extRefNbr>>,
											And<ARPayment.voided, Equal<False>,
											And<Where<ARPayment.docType, NotEqual<Current<ARPayment.docType>>,
											Or<ARPayment.refNbr, NotEqual<Current<ARPayment.refNbr>>>>>>>>>>.Select(this, e.NewValue);
					}
					else
					{
						dup = PXSelectReadonly<ARPayment, Where<ARPayment.customerID, Equal<Current<ARPayment.customerID>>,
											And<ARPayment.paymentMethodID, Equal<Current<ARPayment.paymentMethodID>>,
											And<ARPayment.extRefNbr, Equal<Required<ARPayment.extRefNbr>>,
											And<ARPayment.voided, Equal<False>,
											And<Where<ARPayment.docType, NotEqual<Current<ARPayment.docType>>,
										 Or<ARPayment.refNbr, NotEqual<Current<ARPayment.refNbr>>>>>>>>>>.Select(this, e.NewValue);
					}
					if (dup != null)
					{
						sender.RaiseExceptionHandling<ARPayment.extRefNbr>(e.Row, e.NewValue, new PXSetPropertyException(Messages.DuplicateCustomerPayment, PXErrorLevel.Warning, dup.ExtRefNbr, dup.DocDate, dup.DocType, dup.RefNbr));
					}
				}
			}
		}

		private object GetAcctSub<Field>(PXCache cache, object data)
			where Field : IBqlField
		{
			object NewValue = cache.GetValueExt<Field>(data);
			if (NewValue is PXFieldState)
			{
				return ((PXFieldState)NewValue).Value;
			}
			else
			{
				return NewValue;
			}
		}

		protected virtual void ARPayment_ARAccountID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			if (customer.Current != null && e.Row != null)
			{
				if (((ARPayment)e.Row).DocType == ARDocType.Prepayment)
				{
					e.NewValue = GetAcctSub<Customer.prepaymentAcctID>(customer.Cache, customer.Current);
				}
				if (string.IsNullOrEmpty((string)e.NewValue))
				{
					e.NewValue = GetAcctSub<CR.Location.aRAccountID>(location.Cache, location.Current);
				}
			}
		}

		protected virtual void ARPayment_ARSubID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			if (customer.Current != null && e.Row != null)
			{
				if (((ARPayment)e.Row).DocType == ARDocType.Prepayment)
				{
					e.NewValue = GetAcctSub<Customer.prepaymentSubID>(customer.Cache, customer.Current);
				}
				if (string.IsNullOrEmpty((string)e.NewValue))
				{
					e.NewValue = GetAcctSub<Location.aRSubID>(location.Cache, location.Current);
				}
			}
		}

		protected virtual void ARPayment_PaymentMethodID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			ARPayment payment = e.Row as ARPayment;
			sender.SetDefaultExt<ARPayment.pMInstanceID>(e.Row);
			sender.SetDefaultExt<ARPayment.cashAccountID>(e.Row);
			object newCashAccountID;
			sender.RaiseFieldDefaulting<ARPayment.cashAccountID>(payment, out newCashAccountID);
			sender.SetValue<ARPayment.cashAccountID>(payment, newCashAccountID);
			sender.SetDefaultExt<ARPayment.aRAccountID>(payment);
		}

		protected virtual void ARPayment_PMInstanceID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			if (e.OldValue == null)
			{
				sender.SetDefaultExt<ARPayment.cashAccountID>(e.Row);
			}
		}

		protected virtual void PMInstanceIDFieldDefaulting(Events.FieldDefaulting<ARPayment.pMInstanceID> e)
		{
			ARPayment payment = e.Row as ARPayment;
			if (payment == null) return;
			if (_IsVoidCheckInProgress)
			{
				e.Cancel = true;
			}
		}

		protected virtual void ARPayment_CashAccountID_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			ARPayment payment = (ARPayment)e.Row;
			if (this._IsVoidCheckInProgress)
			{
				e.Cancel = true;
			}
		}

		protected virtual void ARPayment_CashAccountID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			ARPayment payment = (ARPayment)e.Row;
			if (cashaccount.Current == null || cashaccount.Current.CashAccountID != payment.CashAccountID)
			{
				cashaccount.Current = (CashAccount)PXSelectorAttribute.Select<ARPayment.cashAccountID>(sender, e.Row);
			}

			sender.SetDefaultExt<ARPayment.depositAsBatch>(e.Row);
			sender.SetDefaultExt<ARPayment.depositAfter>(e.Row);

			payment.Cleared = false;
			payment.ClearDate = null;

			PaymentMethod pm = paymentmethod.Select();
			if (pm?.PaymentType != PaymentMethodType.CreditCard && cashaccount.Current?.Reconcile == false)
			{
				payment.Cleared = true;
				payment.ClearDate = payment.DocDate;
			}
		}

		protected virtual void ARPayment_Cleared_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			ARPayment payment = (ARPayment)e.Row;
			if (payment.Cleared == true)
			{
				if (payment.ClearDate == null)
				{
					payment.ClearDate = payment.DocDate;
				}
			}
			else
			{
				payment.ClearDate = null;
			}
		}
		protected virtual void ARPayment_AdjDate_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			ARPayment payment = (ARPayment)e.Row;
			if (payment.Released == false && payment.VoidAppl == false)
			{
				sender.SetDefaultExt<ARPayment.depositAfter>(e.Row);
		}
		}

		protected virtual void ARPayment_AdjDate_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			ARPayment doc = (ARPayment)e.Row;
			if (doc == null) return;

			if (doc.DocType == ARDocType.VoidPayment || doc.DocType == ARDocType.VoidRefund)
			{
				ARPayment orig_payment = PXSelect<ARPayment, Where2<
						Where<ARPayment.docType, Equal<ARDocType.payment>,
							Or<ARPayment.docType, Equal<ARDocType.prepayment>,
							Or<ARPayment.docType, Equal<ARDocType.refund>>>>,
						And<ARPayment.refNbr, Equal<Current<ARPayment.refNbr>>>>>.SelectSingleBound(this, new object[] { e.Row });
				if (orig_payment != null && orig_payment.DocDate > (DateTime)e.NewValue)
				{
					throw new PXSetPropertyException(CS.Messages.Entry_GE, orig_payment.DocDate.Value.ToString("d"));
				}
			}
		}

		protected virtual void ARPayment_CuryOrigDocAmt_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			if (!(bool)((ARPayment)e.Row).Released)
			{
				sender.SetValueExt<ARPayment.curyDocBal>(e.Row, ((ARPayment)e.Row).CuryOrigDocAmt);
			}
		}

		protected virtual void ARPayment_RefTranExtNbr_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			e.Cancel = true;
		}

		private bool IsApprovalRequired(ARPayment doc, PXCache cache)
		{
			var isApprovalInstalled = PXAccess.FeatureInstalled<FeaturesSet.approvalWorkflow>();
			var areMapsAssigned = Approval.GetAssignedMaps(doc, cache).Any();
			return doc.DocType == ARDocType.Refund && isApprovalInstalled && areMapsAssigned;
		}
		#endregion
		#region ARAdjust handlers
		protected virtual void ARAdjust_RowSelecting(PXCache cache, PXRowSelectingEventArgs e)
		{
			ARAdjust adj = e.Row as ARAdjust;
			if (adj == null)
				return;
			adj.Selected = (adj.CuryAdjgAmt != 0m || adj.CuryAdjgPPDAmt != 0m);
		}
		protected virtual void ARAdjust_RowSelected(PXCache cache, PXRowSelectedEventArgs e)
		{
			ARAdjust adj = e.Row as ARAdjust;
			if (adj == null)
				return;
	
			bool adjNotReleased = adj.Released != true;
			bool adjNotVoided = adj.Voided != true;
			bool isRefNbr = adj.AdjdRefNbr != null;

			PXUIFieldAttribute.SetEnabled<ARAdjust.adjdDocType>(cache, adj, adj.AdjdRefNbr == null);
			PXUIFieldAttribute.SetEnabled<ARAdjust.adjdRefNbr>(cache, adj, adj.AdjdRefNbr == null);
			PXUIFieldAttribute.SetEnabled<ARAdjust.adjdLineNbr>(cache, adj, adj.AdjdLineNbr == null);
			PXUIFieldAttribute.SetEnabled<ARAdjust.selected>(cache, adj, isRefNbr && adjNotReleased && adjNotVoided);
			PXUIFieldAttribute.SetEnabled<ARAdjust.curyAdjgAmt>(cache, adj, adjNotReleased && adjNotVoided);
			PXUIFieldAttribute.SetEnabled<ARAdjust.curyAdjgPPDAmt>(cache, adj, adjNotReleased && adjNotVoided);
			PXUIFieldAttribute.SetEnabled<ARAdjust.adjBatchNbr>(cache, adj, false);
			
			PXUIFieldAttribute.SetVisible<ARAdjust.adjBatchNbr>(cache, adj, !adjNotReleased);

			if (Document.Current != null)
			{
				Customer adjdCustomer = PXSelect<Customer, Where<Customer.bAccountID, Equal<Required<Customer.bAccountID>>>>.Select(this, adj.AdjdCustomerID);
				if (adjdCustomer != null)
				{
					bool smallBalanceAllow = adjdCustomer.SmallBalanceAllow == true && adjdCustomer.SmallBalanceLimit > 0 && adj.AdjdDocType != ARDocType.CreditMemo;
					Sign balanceSign = adj.CuryOrigDocAmt < 0m ? Sign.Minus : Sign.Plus;
					PXUIFieldAttribute.SetEnabled<ARAdjust.curyAdjgWOAmt>(cache, adj, adjNotReleased && adjNotVoided && smallBalanceAllow && balanceSign == Sign.Plus);
					PXUIFieldAttribute.SetEnabled<ARAdjust.writeOffReasonCode>(cache, adj, smallBalanceAllow && Document.Current.SelfVoidingDoc != true);
				}
			}

			bool EnableCrossRate = false;
			if (adj.Released != true)
			{
				CurrencyInfo pay_info = GetExtension<MultiCurrency>().GetCurrencyInfo(adj.AdjgCuryInfoID);
				CurrencyInfo vouch_info = GetExtension<MultiCurrency>().GetCurrencyInfo(adj.AdjdCuryInfoID);
				EnableCrossRate = vouch_info != null && !string.Equals(pay_info.CuryID, vouch_info.CuryID) && !string.Equals(vouch_info.CuryID, vouch_info.BaseCuryID);
			}
			PXUIFieldAttribute.SetEnabled<ARAdjust.adjdCuryRate>(cache, adj, EnableCrossRate);

			if (adj.AdjdLineNbr == 0)
			{
				ARRegister invoice = GetAdjdInvoiceToVerifyArePaymentsByLineAllowed(Adjustments.Cache, adj);
				if (invoice?.PaymentsByLinesAllowed == true)
				{
					Adjustments.Cache.RaiseExceptionHandling<ARAdjust.adjdRefNbr>(adj, adj.AdjdRefNbr,
						new PXSetPropertyException(Messages.NotDistributedApplicationCannotBeReleased, PXErrorLevel.RowWarning));
				}
			}
		}

		private ARRegister GetAdjdInvoiceToVerifyArePaymentsByLineAllowed(PXCache cache, ARAdjust adj)
		{
			if (balanceCache == null) FillBalanceCache(Document.Current);

			if (balanceCache != null && balanceCache.TryGetValue(adj, out var source))
				return PXResult.Unwrap<ARInvoice>(source[0]);

			return (ARRegister)PXSelectorAttribute.Select<ARAdjust.adjdRefNbr>(cache, adj);
		}

		protected virtual void ARAdjust_RowInserting(PXCache sender, PXRowInsertingEventArgs e)
		{
			ARAdjust adjust = (ARAdjust)e.Row;

			string adjdRefNbrErrMsg = PXUIFieldAttribute.GetError<ARAdjust.adjdRefNbr>(sender, adjust);
			string lineNbrErrMsg = PXUIFieldAttribute.GetError<ARAdjust.adjdLineNbr>(sender, e.Row);

			e.Cancel =
				adjust.AdjdRefNbr == null ||
				adjust.AdjdLineNbr == null ||
				!string.IsNullOrEmpty(adjdRefNbrErrMsg) ||
				!string.IsNullOrEmpty(lineNbrErrMsg) ||
				Document.Current?.PaymentsByLinesAllowed == true;
		}

		protected virtual void ARAdjust_RowUpdating(PXCache sender, PXRowUpdatingEventArgs e)
		{
			ARAdjust adj = (ARAdjust)e.Row;
			if (adj.Voided == true && !_IsVoidCheckInProgress && !IsReverseProc
				&& !sender.ObjectsEqualExceptFields<
					ARAdjust.isCCPayment,
					ARAdjust.paymentReleased,
					ARAdjust.isCCAuthorized,
					ARAdjust.isCCCaptured>(e.Row, e.NewRow))
			{
				throw new PXSetPropertyException(ErrorMessages.CantUpdateRecord);
			}
		}

		protected virtual void ARAdjust_RowDeleted(PXCache sender, PXRowDeletedEventArgs e)
		{
			if (((ARAdjust)e.Row).AdjdCuryInfoID != ((ARAdjust)e.Row).AdjgCuryInfoID && ((ARAdjust)e.Row).AdjdCuryInfoID != ((ARAdjust)e.Row).AdjdOrigCuryInfoID && ((ARAdjust)e.Row).VoidAdjNbr == null)
			{
				foreach (CurrencyInfo info in CurrencyInfo_CuryInfoID.Select(((ARAdjust)e.Row).AdjdCuryInfoID))
				{
					currencyinfo.Delete(info);
				}
			}
		}

		protected virtual void ARAdjust_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			ARAdjust adjustment = (ARAdjust)e.Row;

			if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Delete)
			{
				return;
			}

			if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert
			    && !string.IsNullOrEmpty(adjustment.AdjgFinPeriodID)
			    && AdjgDocTypesToValidateFinPeriod.Contains(adjustment.AdjgDocType))
			{
				IFinPeriod organizationFinPeriod = FinPeriodRepository.GetByID(adjustment.AdjgFinPeriodID, PXAccess.GetParentOrganizationID(adjustment.AdjgBranchID));

				ProcessingResult result = FinPeriodUtils.CanPostToPeriod(organizationFinPeriod, typeof(FinPeriod.aRClosed));

				if (!result.IsSuccess)
				{
					throw new PXRowPersistingException(
						PXDataUtils.FieldName<ARAdjust.adjgFinPeriodID>(),
						adjustment.AdjgFinPeriodID,
						result.GetGeneralMessage(),
						PXUIFieldAttribute.GetDisplayName<ARAdjust.adjgFinPeriodID>(sender));
				}
			}

			Sign balanceSign = adjustment.CuryOrigDocAmt < 0m ? Sign.Minus : Sign.Plus;

			if (adjustment.Released != true && adjustment.AdjNbr < Document.Current?.AdjCntr)
			{
				sender.RaiseExceptionHandling<ARAdjust.adjdRefNbr>(adjustment, adjustment.AdjdRefNbr, new PXSetPropertyException(Messages.ApplicationStateInvalid));
			}

			if (adjustment.CuryDocBal * balanceSign < 0m)
			{
				sender.RaiseExceptionHandling<ARAdjust.curyAdjgAmt>(e.Row, adjustment.CuryAdjgAmt, new PXSetPropertyException(Messages.DocumentBalanceNegative));
			}

			if (adjustment.CuryDiscBal * balanceSign < 0m)
			{
				sender.RaiseExceptionHandling<ARAdjust.curyAdjgPPDAmt>(e.Row, adjustment.CuryAdjgPPDAmt, new PXSetPropertyException(Messages.DocumentBalanceNegative));
			}

			if (adjustment.CuryWOBal * balanceSign < 0m)
			{
				sender.RaiseExceptionHandling<ARAdjust.curyAdjgWOAmt>(e.Row, adjustment.CuryAdjgWOAmt, new PXSetPropertyException(Messages.DocumentBalanceNegative));
			}

			adjustment.PendingPPD = adjustment.CuryAdjgPPDAmt != 0m && adjustment.AdjdHasPPDTaxes == true;
			if (adjustment.PendingPPD == true && adjustment.CuryDocBal != 0m && adjustment.Voided != true)
			{
				sender.RaiseExceptionHandling<ARAdjust.curyAdjgPPDAmt>(e.Row, adjustment.CuryAdjgPPDAmt, new PXSetPropertyException(Messages.PartialPPD));
			}

			if (adjustment.CuryAdjgWOAmt != 0m && string.IsNullOrEmpty(adjustment.WriteOffReasonCode))
			{
				if (sender.RaiseExceptionHandling<ARAdjust.writeOffReasonCode>(e.Row, null, 
					new PXSetPropertyException(ErrorMessages.FieldIsEmpty, PXUIFieldAttribute.GetDisplayName<ARAdjust.writeOffReasonCode>(sender))))
				{
					throw new PXRowPersistingException(PXDataUtils.FieldName<ARAdjust.writeOffReasonCode>(), null, ErrorMessages.FieldIsEmpty, PXUIFieldAttribute.GetDisplayName<ARAdjust.writeOffReasonCode>(sender));
				}
			}

			decimal currencyAdjustedBalanceDelta = BalanceCalculation.GetFullBalanceDelta(adjustment).CurrencyAdjustedBalanceDelta;

			if (adjustment.VoidAdjNbr == null && currencyAdjustedBalanceDelta * balanceSign < 0m)
			{
				sender.RaiseExceptionHandling<ARAdjust.curyAdjgAmt>(adjustment, adjustment.CuryAdjdAmt, new PXSetPropertyException(balanceSign == Sign.Plus 
					? Messages.RegularApplicationTotalAmountNegative
					: Messages.RegularApplicationTotalAmountPositive));
			}

			if (adjustment.VoidAdjNbr != null && currencyAdjustedBalanceDelta * balanceSign > 0m)
			{
				sender.RaiseExceptionHandling<ARAdjust.curyAdjgAmt>(adjustment, adjustment.CuryAdjdAmt, new PXSetPropertyException(balanceSign == Sign.Plus 
					? Messages.ReversedApplicationTotalAmountPositive
					: Messages.ReversedApplicationTotalAmountNegative));
			}

			if (adjustment.VoidAdjNbr == null && adjustment.CuryAdjgAmt * balanceSign < 0m)
			{
				throw new PXSetPropertyException(balanceSign == Sign.Plus ? 
					CS.Messages.Entry_GE : 
					CS.Messages.Entry_LE, 0.ToString());
			}

			if (adjustment.VoidAdjNbr != null && adjustment.CuryAdjgAmt * balanceSign > 0m)
			{
				throw new PXSetPropertyException(balanceSign == Sign.Plus ? 
					CS.Messages.Entry_LE : 
					CS.Messages.Entry_GE, 0.ToString());
			}

			if ((adjustment.VoidAdjNbr == null && balanceSign * Math.Sign((decimal)adjustment.CuryAdjgPPDAmt) < 0)
				|| (adjustment.VoidAdjNbr != null && balanceSign * Math.Sign((decimal)adjustment.CuryAdjgPPDAmt) > 0))
			{
				throw new PXRowPersistingException(PXDataUtils.FieldName<ARAdjust.curyAdjgPPDAmt>(), null, (decimal)adjustment.CuryAdjgPPDAmt < 0m ? CS.Messages.Entry_GE : CS.Messages.Entry_LE, new object[] { 0 });
			}
		}

		protected virtual void _(Events.RowPersisted<ARAdjust> e)
		{
			/* !!! Please note here is a specific case, don't use it as a template and think before implementing the same approach. 
			 * Verification on RowPersisted event will be done on the locked record to guarantee consistent data during the verification.
			 * Otherwise it is possible to incorrectly pass verifications with dirty data without any errors.*/

			// We raising verifying event here to prevent 
			// situations when it is possible to apply the same
			// invoice twice due to read only invoice view.
			// For more details see AC-85468.
			//
			if (!UnattendedMode && e.TranStatus == PXTranStatus.Open && !IsAdjdRefNbrFieldVerifyingDisabled(e.Row))
			{
				// Acuminator disable once PX1073 ExceptionsInRowPersisted. Justification: see comments above
				// Acuminator disable once PX1075 RaiseExceptionHandlingInEventHandlers. Justification: see comments above
				e.Cache.VerifyFieldAndRaiseException<ARAdjust.adjdRefNbr>(e.Row);
			}
		}

		#region ARAdjust_Selected

		/// <summary>
		/// Calc CuryAdjgPPDAmt and CuryAdjgAmt
		/// </summary>
		/// <param name="adj"></param>
		/// <param name="curyUnappliedBal"></param>
		/// <param name="checkBalance">Check that amount of UnappliedBal is greater than 0</param>
		/// <param name="trySelect">Try to alllay this ARAdjust</param>
		/// <returns>Changing amount of payment.CuryUnappliedBal</returns>
		public virtual decimal applyARAdjust(ARAdjust adj, decimal curyUnappliedBal, bool checkBalance, bool trySelect)
		{
			decimal oldCuryUnappliedBal = curyUnappliedBal;
			decimal curyDiscBal = GetDiscAmountForAdjustment(adj);
			decimal oldCuryAdjgPPDAmt = adj.CuryAdjgPPDAmt.Value;
			decimal newCuryAdjgPPDAmt = oldCuryAdjgPPDAmt;

			decimal curyDocBal = adj.CuryDocBal.Value;
			decimal oldCuryAdjgAmt = adj.CuryAdjgAmt.Value;
			decimal newCuryAdjgAmt = oldCuryAdjgAmt;
			decimal balSign = adj.AdjgBalSign.Value;
			bool calcBalances = false;

			#region Calculation adjgAmt
			if (trySelect)
			{
				if (curyDocBal != 0m)
				{
					decimal maxAvailableAmt = (Math.Abs(curyDiscBal) <= Math.Abs(curyDocBal)) ? curyDiscBal : curyDocBal;

					if (!checkBalance)
					{						
						newCuryAdjgPPDAmt += maxAvailableAmt;
						newCuryAdjgAmt += curyDocBal - maxAvailableAmt;
					}
					else
					{
						if ((curyDocBal * balSign - maxAvailableAmt) <= curyUnappliedBal)
						{
							newCuryAdjgPPDAmt += maxAvailableAmt;
							newCuryAdjgAmt += curyDocBal - maxAvailableAmt;
						}
						else
						{
							newCuryAdjgAmt += curyUnappliedBal > 0m ? curyUnappliedBal : 0m;
						}
					}
				}
			}
			else
			{
				newCuryAdjgAmt = 0m;
				newCuryAdjgPPDAmt = 0m;
			}
			curyUnappliedBal -= (newCuryAdjgAmt - oldCuryAdjgAmt) * balSign;
			#endregion

			#region Set adjgAmt and AdjgPPDAmt
			if (oldCuryAdjgPPDAmt != newCuryAdjgPPDAmt)
			{
				adj.CuryAdjgPPDAmt = newCuryAdjgPPDAmt;
				adj.FillDiscAmts();
				calcBalances = true;
			}
			if (oldCuryAdjgAmt != newCuryAdjgAmt)
			{
				adj.CuryAdjgAmt = newCuryAdjgAmt;
				calcBalances = true;
			}
			if (calcBalances)
			{
				GetExtension<ARPaymentEntryDocumentExtension>().CalcBalancesFromAdjustedDocument(adj, true, true);
			}
			#endregion
			adj.Selected = newCuryAdjgAmt != 0m || newCuryAdjgPPDAmt != 0m;


			return (curyUnappliedBal - oldCuryUnappliedBal);
		}

		protected virtual decimal GetDiscAmountForAdjustment(ARAdjust adj)
		{
			return (adj.AdjgDocType == ARDocType.CreditMemo) ? 0m : adj.CuryDiscBal.Value;
		}

		protected virtual void ARAdjust_Selected_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			#region Comment
			/* For On Hold and Balanced where payment amount editable: Once a user set the checkbox to true, fill Amount Paid by the rules:
					If Payment Amount = 0, Amount Paid = Document Balance
					If Payment Amount <> 0, Amount Paid = Document Balance
				For Open, Reserved document where payment amount disabled: Once a user set the checkbox to true, fill Amount Paid by the rules:
					If Payment Amount <> 0, fill Amount Paid as auto apply action - do not exceed Available Amount. 
					When Available Amount becomes 0, manually setting the checkbox for a new line should fill Amount Paid with full balance 
					and keep the current validation and warning on the Payment Amount field when Applied to Document > Payment Amount: "The document is out of the balance."
			*/
			#endregion

			ARAdjust adj = (ARAdjust)e.Row;
			if (adj.Released == true 
				|| adj.Voided == true
				|| _IsVoidCheckInProgress
				|| IsReverseProc)
			{
				return;
			}

			ARPayment payment = (ARPayment)Document.Current;
			bool checkBalance = !(payment.Status == ARDocStatus.Hold || payment.Status == ARDocStatus.Balanced);   // Payment Amount is editable
			bool isSelected = adj.Selected == true;
			applyARAdjust(adj, payment.CuryUnappliedBal.Value, checkBalance, isSelected);
		}
		#endregion

		public bool IsAdjdRefNbrFieldVerifyingDisabled(IAdjustment adj)
		{
			return
				Document?.Current?.VoidAppl == true ||
				adj?.Voided == true ||
				adj?.Released == true ||
				AutoPaymentApp ||
				ForcePaymentApp ||
				IsReverseProc ||
				HasUnreleasedSOInvoice;
		}

		protected virtual void ARAdjust_AdjdRefNbr_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			ARAdjust adj = e.Row as ARAdjust;
			e.Cancel = IsAdjdRefNbrFieldVerifyingDisabled(adj);
		}

		protected virtual void ARAdjust_AdjdRefNbr_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			if (e.Row == null) return;
			var adj = (ARAdjust)e.Row;

			bool initialize = true;
			if (PXAccess.FeatureInstalled<FeaturesSet.paymentsByLines>())
			{
				ARRegister invoice = GetAdjdInvoiceToVerifyArePaymentsByLineAllowed(sender, adj);
				initialize = invoice?.PaymentsByLinesAllowed != true;
			}

			if (initialize)
			{
				InitApplicationData(adj);
			}
		}

		protected virtual void ARAdjust_AdjdLineNbr_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
			{
			if (e.Row == null) return;
			var adj = (ARAdjust)e.Row;

			if (!(e.Cancel = IsAdjdRefNbrFieldVerifyingDisabled(adj)) && e.NewValue == null && !PXAccess.FeatureInstalled<FeaturesSet.paymentsByLines>())
				{
				ARRegister invoice = GetAdjdInvoiceToVerifyArePaymentsByLineAllowed(sender, adj);
				if (invoice?.PaymentsByLinesAllowed == true)
					{
					throw new PXSetPropertyException(Messages.PaymentsByLinesCanBePaidOnlyByLines);
				}
			}
					}

		protected virtual void ARAdjust_AdjdLineNbr_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			if (e.Row == null) return;
			var adj = (ARAdjust)e.Row;
			if (adj.AdjdLineNbr != 0 && adj.AdjdLineNbr != null)
					{
				InitApplicationData(adj);
					}
				}

		private void InitApplicationData(ARAdjust adj)
				{
			try
			{
				foreach (PXResult<ARInvoice, CurrencyInfo, Customer, ARTran> res in ARInvoice_DocType_RefNbr.Select(adj.AdjdLineNbr ?? 0, adj.AdjdDocType, adj.AdjdRefNbr))
				{
					ARInvoice invoice = res;
					CurrencyInfo info = res;
					ARTran tran = res;
					ARRegisterAlias reg = Common.Utilities.Clone<ARInvoice, ARRegisterAlias>(this, invoice);
					ARAdjust key = adj;

					foreach (string f in this.Adjustments.Cache.Keys)
					{
						if (this.Adjustments.Cache.GetValue(key, f ) == null)
						{
							this.Adjustments.Cache.SetDefaultExt(key, f);
						}
					}
					PXParentAttribute.SetParent(this.Adjustments.Cache,  key, typeof(ARInvoice), invoice );
					PXSelectorAttribute.StoreResult<ARAdjust.adjdRefNbr>(this.Adjustments.Cache, adj, invoice);
					this.Adjustments.StoreTailResult( new List<object>()
						{
							new PXResult<ARInvoice, ARRegisterAlias, ARTran>(
								invoice,
								reg,
								tran)
						}, 
						new object[]{ invoice, reg, tran, key }, 
						 key.AdjgDocType, key.AdjgRefNbr
						);
					
					ARAdjust_KeyUpdated<ARInvoice>(adj, invoice, info, tran);
					return;
				}

				foreach (PXResult<ARPayment, CurrencyInfo> res in ARPayment_DocType_RefNbr.Select(adj.AdjdDocType, adj.AdjdRefNbr))
				{
					ARPayment payment = res;
					CurrencyInfo info = res;

					ARAdjust_KeyUpdated<ARPayment>(adj, payment, info);
				}
			}
			catch (PXSetPropertyException ex)
			{
				throw new PXException(ex.Message);
			}
		}

		private void ARAdjust_KeyUpdated<T>(ARAdjust adj, T invoice, CurrencyInfo currencyInfo, ARTran tran = null)
			where T : ARRegister, CM.IInvoice, new()
		{
			GetExtension<MultiCurrency>().StoreResult(currencyInfo);
			CurrencyInfo info_copy = GetExtension<MultiCurrency>().CloneCurrencyInfo(currencyInfo, Document.Current.DocDate);

			adj.CustomerID = Document.Current.CustomerID;
			adj.AdjgDocDate = Document.Current.AdjDate;
			adj.AdjgCuryInfoID = Document.Current.CuryInfoID;
			adj.AdjdCustomerID = invoice.CustomerID;
			adj.AdjdCuryInfoID = info_copy.CuryInfoID;
			adj.AdjdOrigCuryInfoID = invoice.CuryInfoID;

			if(Document.Current.DocType == ARDocType.CreditMemo)
			{
				adj.InvoiceID = invoice.NoteID;
				adj.PaymentID = null;
				adj.MemoID = Document.Current.NoteID;
			}
			else if (invoice.DocType == ARDocType.CreditMemo)
			{
				adj.InvoiceID = null;
				adj.PaymentID = Document.Current.NoteID;
				adj.MemoID = invoice.NoteID;
			}
			else
			{
				adj.InvoiceID = invoice.NoteID;
				adj.PaymentID = Document.Current.NoteID;
				adj.MemoID = null;
			}

			Adjustments.Cache.SetValue<ARAdjust.adjdBranchID>(adj, invoice.BranchID);
			adj.AdjdARAcct = invoice.ARAccountID;
			adj.AdjdARSub = invoice.ARSubID;
			adj.AdjdDocDate = invoice.DocDate;
			FinPeriodIDAttribute.SetPeriodsByMaster<ARAdjust.adjdFinPeriodID>(Adjustments.Cache, adj, invoice.TranPeriodID);
			adj.AdjdHasPPDTaxes = invoice.HasPPDTaxes;
			adj.Released = false;
			adj.PendingPPD = false;

			adj.CuryAdjgAmt = 0m;
			adj.CuryAdjgDiscAmt = 0m;
			adj.CuryAdjgPPDAmt = 0m;
			adj.CuryAdjgWhTaxAmt = 0m;
			adj.AdjdCuryID = currencyInfo.CuryID;
			GetExtension<ARPaymentEntryDocumentExtension>().CalcBalances(adj, invoice, false, true, tran);
			decimal? CuryUnappliedBal = Document.Current.CuryUnappliedBal;
			// save CuryAdjgAmt and CuryAdjgDiscAmt and clear them to exclude events calling

			decimal? _CuryApplAmt = Adjustments.Cache.GetValuePending<ARAdjust.curyAdjgAmt>(adj) as decimal?;
			decimal? _CuryApplDiscAmt = Adjustments.Cache.GetValuePending<ARAdjust.curyAdjgDiscAmt>(adj) as decimal?;
			if (!IsContractBasedAPI)
			{
				Adjustments.Cache.SetValuePending<ARAdjust.curyAdjgAmt>(adj, null);
				Adjustments.Cache.SetValuePending<ARAdjust.curyAdjgDiscAmt>(adj, null);
			}

			if (_CuryApplAmt == 0m && _CuryApplDiscAmt == 0m)
			{
				return; // not apply
			}

			#region apply ARAdjust to Document

			Sign balanceSign = adj.CuryOrigDocAmt < 0m ? Sign.Minus : Sign.Plus;

			decimal? CuryApplDiscAmt = GetDiscAmountForAdjustment(adj);
			decimal? CuryApplAmt = adj.CuryDocBal - CuryApplDiscAmt;

			if (adj.CuryDiscBal * balanceSign >= 0m && (adj.CuryDocBal - adj.CuryDiscBal) * balanceSign <= 0m)
			{
				return; //no amount suggestion is possible
			}

			if (Document.Current != null && (adj.AdjgBalSign < 0m || balanceSign == Sign.Minus))
			{
				if (CuryUnappliedBal < 0m)
				{
					CuryApplAmt = Math.Min((decimal)CuryApplAmt, Math.Abs((decimal)CuryUnappliedBal));
				}
			}
			else if (Document.Current != null && CuryUnappliedBal > 0m && adj.AdjgBalSign > 0m)
			{
				CuryApplAmt = Math.Min((decimal)CuryApplAmt, (decimal)CuryUnappliedBal);

				if ((CuryApplAmt + CuryApplDiscAmt) * balanceSign < adj.CuryDocBal * balanceSign)
				{
					CuryApplDiscAmt = 0m;
				}
			}
			else if (Document.Current != null && CuryUnappliedBal <= 0m && Document.Current.CuryOrigDocAmt > 0)
			{
				CuryApplAmt = 0m;
				CuryApplDiscAmt = 0m;
			}
			
			if (_CuryApplAmt != null && _CuryApplAmt * balanceSign < CuryApplAmt * balanceSign)
			{
				CuryApplAmt = _CuryApplAmt;
			}
			if (_CuryApplDiscAmt != null && _CuryApplDiscAmt * balanceSign < CuryApplDiscAmt * balanceSign)
			{
				CuryApplDiscAmt = _CuryApplDiscAmt;
			}

			adj.CuryAdjgAmt = CuryApplAmt;
			adj.CuryAdjgDiscAmt = CuryApplDiscAmt;
			adj.CuryAdjgPPDAmt = CuryApplDiscAmt;
			adj.CuryAdjgWOAmt = 0m;
			adj.Selected = (adj.CuryAdjgAmt != 0m || adj.CuryAdjgPPDAmt != 0m);

			GetExtension<ARPaymentEntryDocumentExtension>().CalcBalances(adj, invoice, true, true, tran);

            PXCache<ARAdjust>.SyncModel(adj);

			#endregion
        }

		public bool internalCall;


		protected virtual void ARAdjust_AdjdCuryRate_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			if ((decimal?)e.NewValue <= 0m)
			{
				throw new PXSetPropertyException(CS.Messages.Entry_GT, ((int)0).ToString());
			}
		}

		#endregion

		#region CalcBalances ARAdjust

		#endregion
		#region CurrencyInfo handleds

		protected virtual void CurrencyInfo_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			if (!sender.ObjectsEqual<CurrencyInfo.curyID, CurrencyInfo.curyRate, CurrencyInfo.curyMultDiv>(e.Row, e.OldRow))
			{
				CurrencyInfo info = e.Row as CurrencyInfo;
				if (info?.CuryRate == null)
				{
					Document.Cache.RaiseExceptionHandling<ARPayment.adjDate>(Document.Current, Document.Current.AdjDate,
						new PXSetPropertyException(CM.Messages.RateNotFound, PXErrorLevel.RowWarning));
					return;
				}

				foreach (ARAdjust adj in PXSelect<ARAdjust, Where<ARAdjust.adjgCuryInfoID, Equal<Required<ARAdjust.adjgCuryInfoID>>>>.Select(sender.Graph, ((CurrencyInfo)e.Row).CuryInfoID))
				{
					Adjustments.Cache.MarkUpdated(adj);

					GetExtension<ARPaymentEntryDocumentExtension>().CalcBalancesFromAdjustedDocument(adj, true, true);

					if (adj.CuryDocBal < 0m)
					{
						Adjustments.Cache.RaiseExceptionHandling<ARAdjust.curyAdjgAmt>(adj, adj.CuryAdjgAmt, new PXSetPropertyException(Messages.DocumentBalanceNegative));
					}

					if (adj.CuryDiscBal < 0m)
					{
						Adjustments.Cache.RaiseExceptionHandling<ARAdjust.curyAdjgPPDAmt>(adj, adj.CuryAdjgPPDAmt, new PXSetPropertyException(Messages.DocumentBalanceNegative));
					}

					if (adj.CuryWOBal < 0m)
					{
						Adjustments.Cache.RaiseExceptionHandling<ARAdjust.curyAdjgWOAmt>(adj, adj.CuryAdjgWOAmt, new PXSetPropertyException(Messages.DocumentBalanceNegative));
					}
				}
			}
		}
		#endregion

		protected virtual void ARPayment_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			ARPayment doc = (ARPayment)e.Row;
			bool docIsMemoOrBalanceWO = doc.DocType == ARDocType.CreditMemo || doc.DocType == ARDocType.SmallBalanceWO;

			if (!docIsMemoOrBalanceWO && doc.CashAccountID == null)
			{
				if (sender.RaiseExceptionHandling<ARPayment.cashAccountID>(e.Row, null, new PXSetPropertyException(ErrorMessages.FieldIsEmpty, $"[{nameof(ARPayment.cashAccountID)}]")))
				{
					throw new PXRowPersistingException(typeof(ARPayment.cashAccountID).Name, null, ErrorMessages.FieldIsEmpty, nameof(ARPayment.cashAccountID));
				}
			}

			if (!docIsMemoOrBalanceWO && String.IsNullOrEmpty(doc.PaymentMethodID))
			{
				if (sender.RaiseExceptionHandling<ARPayment.paymentMethodID>(e.Row, null, new PXSetPropertyException(ErrorMessages.FieldIsEmpty, $"[{nameof(ARPayment.paymentMethodID)}]")))
				{
					throw new PXRowPersistingException(typeof(ARPayment.paymentMethodID).Name, null, ErrorMessages.FieldIsEmpty, nameof(ARPayment.paymentMethodID));
				}
			}

			if (doc.OpenDoc == true && doc.Hold != true && IsPaymentUnbalanced(doc))
			{
				throw new PXRowPersistingException(typeof(ARPayment.curyOrigDocAmt).Name, doc.CuryOrigDocAmt, Messages.DocumentOutOfBalance);
			}

			PaymentRefAttribute.SetUpdateCashManager<ARPayment.extRefNbr>(sender, e.Row, ((ARPayment)e.Row).DocType != ARDocType.VoidPayment);

			string errMsg;
			// VerifyAdjFinPeriodID() compares Payment "Application Period" only with applications, that have been released. Sometimes, this may cause an erorr 
			// during the action, while document is saved and closed (because of Persist() for each action) - this why doc.OpenDoc flag has been used as a criteria.
			if (doc.OpenDoc == true && !VerifyAdjFinPeriodID(doc, doc.AdjFinPeriodID, out errMsg))
			{
				if (sender.RaiseExceptionHandling<ARPayment.adjFinPeriodID>(e.Row,
					FinPeriodIDAttribute.FormatForDisplay(doc.AdjFinPeriodID), new PXSetPropertyException(errMsg)))
				{
					throw new PXRowPersistingException(typeof(ARPayment.adjFinPeriodID).Name, FinPeriodIDAttribute.FormatForError(doc.AdjFinPeriodID), errMsg);
				}
			}

			if (ARPaymentType.IsSelfVoiding(doc.DocType) && 
				doc.OpenDoc == true &&
				doc.CuryApplAmt != null &&
				Math.Abs(doc.CuryApplAmt ?? 0m) != Math.Abs(doc.CuryOrigDocAmt ?? 0m))
			{
				throw new PXRowPersistingException(typeof(ARPayment.curyOrigDocAmt).Name, doc.CuryOrigDocAmt, Messages.DocumentOutOfBalance);
			}

			if(!PXAccess.FeatureInstalled<FeaturesSet.integratedCardProcessing>())
				{
				bool isPMInstanceRequired = paymentmethod.Current?.IsAccountNumberRequired ?? false;

				PXDefaultAttribute.SetPersistingCheck<ARPayment.pMInstanceID>(sender, doc, !docIsMemoOrBalanceWO && isPMInstanceRequired ? 
					PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
				}
			}

		protected internal bool InternalCall = false;

		// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
		public class ARPaymentContextExtention: GraphContextExtention<ARPaymentEntry>
		{
			
		}
			

		protected virtual void ARPayment_RowSelected(PXCache cache, PXRowSelectedEventArgs e)
		{
			ARPayment doc = e.Row as ARPayment;

			if (doc == null || InternalCall)
			{
				return;
			}
			release.SetEnabled(true);

			this.Caches[typeof(CurrencyInfo)].AllowUpdate = true;
			this.Caches[typeof(ARTranPostBal)].AllowInsert = 
			this.Caches[typeof(ARTranPostBal)].AllowUpdate = 
			this.Caches[typeof(ARTranPostBal)].AllowDelete = false;
			this.Caches[typeof(ARAdjust2)].AllowInsert = 
			this.Caches[typeof(ARAdjust2)].AllowUpdate = 
			this.Caches[typeof(ARAdjust2)].AllowDelete = false;
			bool dontApprove = !IsApprovalRequired(doc, cache);
			if (doc.DontApprove != dontApprove)
			{
				cache.SetValueExt<ARPayment.dontApprove>(doc, dontApprove);
			}

			bool docIsMemoOrBalanceWO = doc.DocType == ARDocType.CreditMemo || doc.DocType == ARDocType.SmallBalanceWO;
			bool isPMInstanceRequired = false;

			if (!string.IsNullOrEmpty(doc.PaymentMethodID))
			{
				isPMInstanceRequired = paymentmethod.Current?.IsAccountNumberRequired ?? false;
			}

			PXUIFieldAttribute.SetVisible<ARPayment.curyID>(cache, doc, PXAccess.FeatureInstalled<FeaturesSet.multicurrency>());
			PXUIFieldAttribute.SetVisible<ARPayment.cashAccountID>(cache, doc, !docIsMemoOrBalanceWO);
			PXUIFieldAttribute.SetVisible<ARPayment.cleared>(cache, doc, !docIsMemoOrBalanceWO);
			PXUIFieldAttribute.SetVisible<ARPayment.clearDate>(cache, doc, !docIsMemoOrBalanceWO);
			PXUIFieldAttribute.SetVisible<ARPayment.paymentMethodID>(cache, doc, !docIsMemoOrBalanceWO);
			PXUIFieldAttribute.SetVisible<ARPayment.pMInstanceID>(cache, doc, !docIsMemoOrBalanceWO);
			PXUIFieldAttribute.SetVisible<ARPayment.extRefNbr>(cache, doc, !docIsMemoOrBalanceWO);

			PXUIFieldAttribute.SetRequired<ARPayment.cashAccountID>(cache, !docIsMemoOrBalanceWO);
			PXUIFieldAttribute.SetRequired<ARPayment.pMInstanceID>(cache, !docIsMemoOrBalanceWO && isPMInstanceRequired);
			PXUIFieldAttribute.SetEnabled<ARPayment.pMInstanceID>(cache, e.Row, !docIsMemoOrBalanceWO && isPMInstanceRequired);

			bool isDepositAfterEditable = doc.DocType == ARDocType.Payment ||
										  doc.DocType == ARDocType.Prepayment ||
										  doc.DocType == ARDocType.Refund ||
										  doc.DocType == ARDocType.CashSale;

			PXUIFieldAttribute.SetVisible<ARPayment.depositAfter>(cache, doc, isDepositAfterEditable && doc.DepositAsBatch == true);
			PXUIFieldAttribute.SetEnabled<ARPayment.depositAfter>(cache, doc, false);
			PXUIFieldAttribute.SetRequired<ARPayment.depositAfter>(cache, isDepositAfterEditable && doc.DepositAsBatch == true);

			PXPersistingCheck depositAfterPersistCheck = (isDepositAfterEditable && doc.DepositAsBatch == true) ? PXPersistingCheck.NullOrBlank
																												: PXPersistingCheck.Nothing;
			PXDefaultAttribute.SetPersistingCheck<ARPayment.depositAfter>(cache, doc, depositAfterPersistCheck);

			PXUIFieldAttribute.SetVisible<ARAdjust.adjdCustomerID>(Adjustments.Cache, null, PXAccess.FeatureInstalled<FeaturesSet.parentChildAccount>());
			PXUIFieldAttribute.SetVisible<ARAdjust.adjdCustomerID>(Adjustments_History.Cache, null, PXAccess.FeatureInstalled<FeaturesSet.parentChildAccount>());

			OpenPeriodAttribute.SetValidatePeriod<ARPayment.adjFinPeriodID>(cache, doc, doc.OpenDoc == true ? PeriodValidation.DefaultSelectUpdate
																											: PeriodValidation.DefaultUpdate);

			CashAccount cashAccount = this.cashaccount.Current;
			bool isClearingAccount = (cashAccount != null && cashAccount.CashAccountID == doc.CashAccountID && cashAccount.ClearingAccount == true);

			bool curyenabled = false;
			bool docNotOnHold = doc.Hold == false;
			bool docOnHold = doc.Hold == true;
			bool docNotReleased = doc.Released == false;
			bool docReleased = doc.Released == true;
			bool docOpen = doc.OpenDoc == true;
			bool docNotVoided = doc.Voided == false;
			bool clearEnabled = docOnHold && cashaccount.Current?.Reconcile == true;
			bool holdAdj = false;
			bool docClosed = doc.Status == ARDocStatus.Closed;

			#region Credit Card Processing

			bool docTypePayment = doc.DocType == ARDocType.Payment || doc.DocType == ARDocType.Prepayment;
			bool enableCCProcess = (doc.IsCCPayment == true) && (docTypePayment || doc.DocType.IsIn(ARDocType.Refund, ARDocType.VoidPayment));

			ExternalTransactionState currState = ExternalTranHelper.GetActiveTransactionState(this, ExternalTran);
			var extTrans = ExternalTran.Select().RowCast<ExternalTransaction>();
			ExternalTranHelper.SharedTranStatus sharedTranStatus = ExternalTranHelper.GetSharedTranStatus(this, extTrans.FirstOrDefault());
			bool sharedVoidForRefClearState = doc.DocType == ARDocType.Refund
				&& sharedTranStatus == ExternalTranHelper.SharedTranStatus.ClearState;
			bool sharedVoidForRef = doc.DocType == ARDocType.Refund
				&& sharedTranStatus == ExternalTranHelper.SharedTranStatus.Synchronized;

			bool existsOpenCCTran = false;
			if (doc.CCActualExternalTransactionID != null)
			{
				CCProcTran tranDetail = ccProcTran
					.Select().RowCast<CCProcTran>()
					.FirstOrDefault(i => i.TransactionID == doc.CCActualExternalTransactionID);
				existsOpenCCTran = tranDetail?.ProcStatus == CCProcStatus.Opened;
			}

			bool useNewCard = paymentmethod.Current?.PaymentType == PaymentMethodType.CreditCard &&
							  doc.DocType != ARDocType.Refund &&
							  !existsOpenCCTran;

			bool newCardVisible = PXAccess.FeatureInstalled<FeaturesSet.integratedCardProcessing>()
				&& useNewCard && ShowCardChck(doc)
				&& (!currState.IsActive || currState.ProcessingStatus == ProcessingStatus.AuthorizeExpired
					|| currState.ProcessingStatus == ProcessingStatus.CaptureExpired)
				&& paymentmethod.Current?.ARIsProcessingRequired == true;

			PXUIFieldAttribute.SetEnabled<ARPayment.newCard>(cache, doc, useNewCard);
			PXUIFieldAttribute.SetVisible<ARPayment.newCard>(cache, doc, newCardVisible);

			CustomerClass custClass = customerclass.Current;
			string saveCustOpt = custClass?.SavePaymentProfiles;
			bool saveCardVisible = false;
			bool useSaveCard = false;
			if (saveCustOpt == SavePaymentProfileCode.Allow)
			{
				CCProcessingCenter procCenter = processingCenter.Current;
				saveCardVisible = doc.NewCard == true && newCardVisible && procCenter?.AllowSaveProfile == true;
				useSaveCard = useNewCard && procCenter?.AllowSaveProfile == true;
			}
			PXUIFieldAttribute.SetEnabled<ARPayment.saveCard>(cache, doc, useSaveCard);
			PXUIFieldAttribute.SetVisible<ARPayment.saveCard>(cache, doc, saveCardVisible);

			enableCCProcess = enableCCProcess && !doc.Voided.Value;
			CheckCashAccount(cache, doc);
			#endregion

			bool isReclassified = false;
			bool isViewOnlyRecord = AutoNumberAttribute.IsViewOnlyRecord<ARPayment.refNbr>(cache, doc);
			bool hasSOAdjustments = GetOrdersToApplyTabExtension()?.SOAdjustments_Raw.View.SelectSingleBound(new object[] { e.Row }) != null;
			HasUnreleasedSOInvoice = docNotReleased
				&& hasSOAdjustments
				&& Adjustments_Invoices_Unreleased.Select().Any();

			if (isViewOnlyRecord)
			{
				PXUIFieldAttribute.SetEnabled(cache, doc, false);
				cache.AllowUpdate = false;
				cache.AllowDelete = false;
				Adjustments.Cache.AllowDelete = false;
				Adjustments.Cache.AllowUpdate = false;
				Adjustments.Cache.AllowInsert = false;
				release.SetEnabled(false);
			}
			else if ((bool)doc.Voided)
			{
				//Document is voided
				PXUIFieldAttribute.SetEnabled(cache, doc, false);
				cache.AllowDelete = false;
				cache.AllowUpdate = false;
				Adjustments.Cache.AllowDelete = false;
				Adjustments.Cache.AllowUpdate = false;
				Adjustments.Cache.AllowInsert = false;
				release.SetEnabled(false);
			}
			else if ((bool)doc.VoidAppl && docNotReleased)
			{
				PXUIFieldAttribute.SetEnabled(cache, doc, false);
				PXUIFieldAttribute.SetEnabled<ARPayment.adjDate>(cache, doc, true);
				PXUIFieldAttribute.SetEnabled<ARPayment.adjFinPeriodID>(cache, doc, true);
				PXUIFieldAttribute.SetEnabled<ARPayment.docDesc>(cache, doc, true);
				PXUIFieldAttribute.SetEnabled<ARPayment.hold>(cache, doc, true);
				PXUIFieldAttribute.SetEnabled<ARPayment.depositAfter>(cache, doc, isDepositAfterEditable && doc.DepositAsBatch == true);
				cache.AllowUpdate = true;
				cache.AllowDelete = true;
				Adjustments.Cache.AllowDelete = false;
				Adjustments.Cache.AllowUpdate = false;
				Adjustments.Cache.AllowInsert = false;
				release.SetEnabled(docNotOnHold);
			}
			else if (docReleased && docOpen)
			{
				int AdjCount = Adjustments_Raw.Select().Count;

				foreach (ARAdjust adj in Adjustments_Raw.Select())
				{
					if ((bool)adj.Voided)
					{
						break;
					}

					if (adj.Hold == true)
					{
						holdAdj = true;
						break;
					}
				}

				PXUIFieldAttribute.SetEnabled(cache, doc, false);
				PXUIFieldAttribute.SetEnabled<ARPayment.adjDate>(cache, doc, !holdAdj);
				PXUIFieldAttribute.SetEnabled<ARPayment.adjFinPeriodID>(cache, doc, !holdAdj);
				PXUIFieldAttribute.SetEnabled<ARPayment.hold>(cache, doc, !holdAdj);

				cache.AllowDelete = false;
				cache.AllowUpdate = !holdAdj;
				Adjustments.Cache.AllowDelete = !holdAdj && doc.PaymentsByLinesAllowed != true;
				Adjustments.Cache.AllowInsert = !holdAdj && doc.SelfVoidingDoc != true && doc.PaymentsByLinesAllowed != true;
				Adjustments.Cache.AllowUpdate = !holdAdj && doc.PaymentsByLinesAllowed != true;

				release.SetEnabled(docNotOnHold && !holdAdj && AdjCount != 0);
			}
			else if (HasUnreleasedSOInvoice)
			{
				PXUIFieldAttribute.SetEnabled(cache, doc, false);
				PXUIFieldAttribute.SetEnabled<ARPayment.newCard>(cache, doc, false);
				PXUIFieldAttribute.SetEnabled<ARPayment.hold>(cache, doc, true);
				PXUIFieldAttribute.SetEnabled<ARPayment.docDesc>(cache, doc, true);
				PXUIFieldAttribute.SetEnabled<ARPayment.aRAccountID>(cache, doc, true);
				PXUIFieldAttribute.SetEnabled<ARPayment.aRSubID>(cache, doc, true);
				PXUIFieldAttribute.SetEnabled<ARPayment.extRefNbr>(cache, doc, true);
				PXUIFieldAttribute.SetEnabled<ARPayment.adjDate>(cache, doc, true);
				PXUIFieldAttribute.SetEnabled<ARPayment.adjFinPeriodID>(cache, doc, true);

				cache.AllowDelete = !(enableCCProcess && (currState.IsPreAuthorized || currState.IsSettlementDue));
				cache.AllowUpdate = true;
				Adjustments.Cache.AllowDelete = true;
				Adjustments.Cache.AllowUpdate = false;
				Adjustments.Cache.AllowInsert = false;
				release.SetEnabled(docNotOnHold);
			}
			else if (docReleased && !docOpen)
			{
				PXUIFieldAttribute.SetEnabled(cache, doc, false);
				cache.AllowDelete = false;
				cache.AllowUpdate = isClearingAccount;
				Adjustments.Cache.AllowDelete = false;
				Adjustments.Cache.AllowUpdate = false;
				Adjustments.Cache.AllowInsert = false;
				release.SetEnabled(false);
			}
			else if ((bool)doc.VoidAppl)
			{
				PXUIFieldAttribute.SetEnabled(cache, doc, false);
				cache.AllowDelete = false;
				cache.AllowUpdate = false;
				Adjustments.Cache.AllowDelete = false;
				Adjustments.Cache.AllowUpdate = false;
				Adjustments.Cache.AllowInsert = false;
				release.SetEnabled(docNotOnHold);
			}
			else if (enableCCProcess && !sharedVoidForRefClearState
				&& (currState.IsPreAuthorized || currState.IsSettlementDue || currState.NeedSync
				|| currState.IsImportedUnknown || sharedVoidForRef))
			{
				PXUIFieldAttribute.SetEnabled(cache, doc, false);
				PXUIFieldAttribute.SetEnabled<ARPayment.adjDate>(cache, doc, true);
				PXUIFieldAttribute.SetEnabled<ARPayment.adjFinPeriodID>(cache, doc, true);
				PXUIFieldAttribute.SetEnabled<ARPayment.hold>(cache, doc, true);
				PXUIFieldAttribute.SetEnabled<ARPayment.docDate>(cache, doc, true);
				cache.AllowDelete = false;
				cache.AllowUpdate = true;
				Adjustments.Cache.AllowDelete = true;
				Adjustments.Cache.AllowUpdate = true;
				Adjustments.Cache.AllowInsert = true;
				release.SetEnabled(docNotOnHold);
			}
			else
			{
				CATran tran = PXSelect<CATran, Where<CATran.tranID, Equal<Required<CATran.tranID>>>>.Select(this, doc.CATranID);
				isReclassified = tran?.RefTranID != null;
				PXUIFieldAttribute.SetEnabled(cache, doc, true);
				PXUIFieldAttribute.SetEnabled<ARPayment.status>(cache, doc, false);
				PXUIFieldAttribute.SetEnabled<ARPayment.curyID>(cache, doc, curyenabled);
				cache.AllowDelete = !ExternalTranHelper.HasSuccessfulTrans(ExternalTran) || sharedVoidForRefClearState;
				cache.AllowUpdate = true;
				Adjustments.Cache.AllowDelete = true;
				Adjustments.Cache.AllowUpdate = true;
				Adjustments.Cache.AllowInsert = true;
				release.SetEnabled(docNotOnHold);
				PXUIFieldAttribute.SetEnabled<ARPayment.curyOrigDocAmt>(cache, doc, !isReclassified);
				PXUIFieldAttribute.SetEnabled<ARPayment.cashAccountID>(cache, doc, !isReclassified);
				PXUIFieldAttribute.SetEnabled<ARPayment.pMInstanceID>(cache, doc, !isReclassified && isPMInstanceRequired);
				PXUIFieldAttribute.SetEnabled<ARPayment.paymentMethodID>(cache, doc, !isReclassified);
				PXUIFieldAttribute.SetEnabled<ARPayment.extRefNbr>(cache, doc, !isReclassified);
				PXUIFieldAttribute.SetEnabled<ARPayment.customerID>(cache, doc, !isReclassified);
			}

			if(doc.DocType == ARDocType.VoidRefund)
			{
				PXUIFieldAttribute.SetEnabled<ARPayment.extRefNbr>(cache, doc, false);
			}

			PXUIFieldAttribute.SetEnabled<ARPayment.docType>(cache, doc);
			PXUIFieldAttribute.SetEnabled<ARPayment.refNbr>(cache, doc);
			PXUIFieldAttribute.SetEnabled<ARPayment.curyUnappliedBal>(cache, doc, false);
			PXUIFieldAttribute.SetEnabled<ARPayment.curyApplAmt>(cache, doc, false);
			PXUIFieldAttribute.SetEnabled<ARPayment.curyWOAmt>(cache, doc, false);
			PXUIFieldAttribute.SetEnabled<ARPayment.batchNbr>(cache, doc, false);
			PXUIFieldAttribute.SetEnabled<ARPayment.cleared>(cache, doc, clearEnabled);
			PXUIFieldAttribute.SetEnabled<ARPayment.clearDate>(cache, doc, clearEnabled && doc.Cleared == true);

			bool enableVoidCheck = false;
			if (docReleased && docNotVoided && ARPaymentType.VoidEnabled(doc))
			{
				enableVoidCheck = true;
			}
			if (!docReleased && docTypePayment && docNotVoided && ExternalTranHelper.HasTransactions(ExternalTran))
			{
				bool isCCStateClear = !(currState.IsCaptured || currState.IsPreAuthorized || currState.IsImportedUnknown);
				bool isVoidableIfFeatureIsOff = !PXAccess.FeatureInstalled<FeaturesSet.integratedCardProcessing>() && doc.Status == ARDocStatus.CCHold;
				if(isCCStateClear || isVoidableIfFeatureIsOff)
				{
					enableVoidCheck = true;
				}
			}

			bool validImportedTran = !(sharedVoidForRef 
				|| (ExternalTranHelper.HasImportedNeedSyncTran(this, ExternalTran) && docTypePayment));
			voidCheck.SetEnabled(enableVoidCheck && !holdAdj && validImportedTran);

			bool loadActionsEnabled = 
				doc.CustomerID != null && 
				(bool)doc.OpenDoc && !holdAdj &&
				(doc.DocType == ARDocType.Payment ||
					doc.DocType == ARDocType.Prepayment || 
					doc.DocType == ARDocType.CreditMemo) &&
				doc.PaymentsByLinesAllowed != true;

			loadInvoices.SetEnabled(loadActionsEnabled);
			adjustDocAmt.SetEnabled(
				((PXFieldState)cache.GetStateExt<ARPayment.curyOrigDocAmt>(doc)).Enabled
				&& !(doc.CuryApplAmt == 0 && doc.CurySOApplAmt == 0)
			);

			SetDocTypeList(e.Row);
			editCustomer.SetEnabled(customer?.Current != null);

			bool hasAdjustments = false;
			if (doc.CustomerID != null)
			{
				hasAdjustments = Adjustments_Raw.Select().Any();
				if (hasAdjustments || hasSOAdjustments)
				{
					PXUIFieldAttribute.SetEnabled<ARPayment.customerID>(cache, doc, false);
				}
			}

			autoApply.SetEnabled(loadActionsEnabled && doc.CuryOrigDocAmt > 0 && hasAdjustments);

			PXUIFieldAttribute.SetEnabled<ARPayment.cCPaymentStateDescr>(cache, null, false);
			PXUIFieldAttribute.SetEnabled<ARPayment.depositDate>(cache, null, false);
			PXUIFieldAttribute.SetEnabled<ARPayment.depositAsBatch>(cache, null, false);
			PXUIFieldAttribute.SetEnabled<ARPayment.deposited>(cache, null, false);
			PXUIFieldAttribute.SetEnabled<ARPayment.depositNbr>(cache, null, false);

			CalcApplAmounts(cache, doc);

			bool isDeposited = string.IsNullOrEmpty(doc.DepositNbr) == false && string.IsNullOrEmpty(doc.DepositType) == false;
			bool enableDepositEdit = !isDeposited && cashAccount != null && (isClearingAccount || doc.DepositAsBatch != isClearingAccount);

			if (enableDepositEdit)
			{
				var exc = doc.DepositAsBatch != isClearingAccount ? new PXSetPropertyException(Messages.DocsDepositAsBatchSettingDoesNotMatchClearingAccountFlag, PXErrorLevel.Warning)
																  : null;

				cache.RaiseExceptionHandling<ARPayment.depositAsBatch>(doc, doc.DepositAsBatch, exc);
			}

			PXUIFieldAttribute.SetEnabled<ARPayment.depositAsBatch>(cache, doc, enableDepositEdit);
			PXUIFieldAttribute.SetEnabled<ARPayment.depositAfter>(cache, doc, !isDeposited && isClearingAccount && doc.DepositAsBatch == true);

			bool allowPaymentChargesEdit = doc.Released != true && (doc.DocType == ARDocType.Payment || doc.DocType == ARDocType.VoidPayment || doc.DocType == ARDocType.Prepayment);
			this.PaymentCharges.Cache.AllowInsert = allowPaymentChargesEdit;
			this.PaymentCharges.Cache.AllowUpdate = allowPaymentChargesEdit;
			this.PaymentCharges.Cache.AllowDelete = allowPaymentChargesEdit;

			bool reversalActionEnabled =
				doc.DocType != ARDocType.SmallBalanceWO
				&& doc.DocType != ARDocType.VoidPayment
				&& doc.DocType != ARDocType.VoidRefund
				&& doc.Voided != true;

			reverseApplication.SetEnabled(reversalActionEnabled);

			#region Migration Mode Settings

			bool isMigratedDocument = doc.IsMigratedRecord == true;
			bool isUnreleasedMigratedDocument = isMigratedDocument && !docReleased;
			bool isReleasedMigratedDocument = isMigratedDocument && docReleased;
			bool isCuryInitDocBalEnabled = isUnreleasedMigratedDocument &&
				(doc.DocType == ARDocType.Payment || doc.DocType == ARDocType.Prepayment);

			PXUIFieldAttribute.SetVisible<ARPayment.curyUnappliedBal>(cache, doc, !isUnreleasedMigratedDocument);
			PXUIFieldAttribute.SetVisible<ARPayment.curyInitDocBal>(cache, doc, isUnreleasedMigratedDocument);
			PXUIFieldAttribute.SetVisible<ARPayment.displayCuryInitDocBal>(cache, doc, isReleasedMigratedDocument);
			PXUIFieldAttribute.SetEnabled<ARPayment.curyInitDocBal>(cache, doc, isCuryInitDocBalEnabled);

			Adjustments.Cache.AllowSelect = !isUnreleasedMigratedDocument;
			ARPost.Cache.AllowSelect = !isUnreleasedMigratedDocument;
			PaymentCharges.Cache.AllowSelect = !isUnreleasedMigratedDocument;

			bool disableCaches = arsetup.Current?.MigrationMode == true
				? !isMigratedDocument
				: isUnreleasedMigratedDocument;
			if (disableCaches)
			{
				bool primaryCacheAllowInsert = Document.Cache.AllowInsert;
				bool primaryCacheAllowDelete = Document.Cache.AllowDelete;
				this.DisableCaches();
				Document.Cache.AllowInsert = primaryCacheAllowInsert;
				Document.Cache.AllowDelete = primaryCacheAllowDelete;
			}

			// We should notify the user that initial balance can be entered,
			// if there are now any errors on this box.
			// 
			if (isCuryInitDocBalEnabled)
			{
				if (string.IsNullOrEmpty(PXUIFieldAttribute.GetError<ARPayment.curyInitDocBal>(cache, doc)))
				{
					cache.RaiseExceptionHandling<ARPayment.curyInitDocBal>(doc, doc.CuryInitDocBal,
						new PXSetPropertyException(Messages.EnterInitialBalanceForUnreleasedMigratedDocument, PXErrorLevel.Warning));
				}
			}
			else
			{
				cache.RaiseExceptionHandling<ARPayment.curyInitDocBal>(doc, doc.CuryInitDocBal, null);
			}
			#endregion

			CheckForUnreleasedIncomingApplications(cache, doc);
			#region Approval
			if (IsApprovalRequired(doc, cache))
			{
				if (doc.Status == ARDocStatus.PendingApproval || doc.Status == ARDocStatus.Rejected)
				{
					release.SetEnabled(false);
				}

				if (doc.DocType == ARDocType.Refund && (doc.Status == ARDocStatus.PendingApproval || doc.Status == ARDocStatus.Rejected 
					|| doc.Status == ARDocStatus.Balanced) && doc.DontApprove == false)
				{
					DisableViewsOnUnapprovedRefund();
				}

				if (doc.DocType == ARDocType.Refund)
				{
					if ((doc.Status == ARDocStatus.PendingApproval || doc.Status == ARDocStatus.Rejected ||
						 doc.Status == ARDocStatus.Closed || doc.Status == ARDocStatus.Balanced) && doc.DontApprove == false)
					{
						PXUIFieldAttribute.SetEnabled(cache, doc, false);
					}
					if (doc.Status == ARDocStatus.PendingApproval || doc.Status == ARDocStatus.Rejected ||
						doc.Status == ARDocStatus.Balanced)
					{
						PXUIFieldAttribute.SetEnabled<ARPayment.hold>(cache, doc, true);
						cache.AllowDelete = true;
					}
				}
			}
			PXUIFieldAttribute.SetEnabled<ARPayment.docType>(cache, doc, true);
			PXUIFieldAttribute.SetEnabled<ARPayment.refNbr>(cache, doc, true);
			#endregion

			if (docReleased && (docOnHold || (bool?)cache.GetValueOriginal<ARPayment.hold>(doc) == true))
			{
				PXUIFieldAttribute.SetEnabled<ARPayment.hold>(cache, doc, true);
				cache.AllowUpdate = true;
			}

			if (doc.DocType == ARDocType.CreditMemo && doc.PaymentsByLinesAllowed == true)
			{
				cache.RaiseExceptionHandling<ARPayment.refNbr>(doc, doc.RefNbr, 
					new PXSetPropertyException(Messages.PayByLineCreditMemoCannotBeUsedAsPayment, PXErrorLevel.Warning, doc.RefNbr));
			}

			#region CC Settings
			bool isCreditCardProcInfoTabVisible = doc.IsCCPayment == true &&
				(PXAccess.FeatureInstalled<FeaturesSet.integratedCardProcessing>() == true ||
				PXAccess.FeatureInstalled<FeaturesSet.integratedCardProcessing>() == false && currState.IsActive);
			this.ccProcTran.Cache.AllowSelect = isCreditCardProcInfoTabVisible;
			this.ccProcTran.Cache.AllowUpdate = false;
			this.ccProcTran.Cache.AllowDelete = false;
			this.ccProcTran.Cache.AllowInsert = false;

			bool CCActionsNoAvailable = doc.Status == ARDocStatus.CCHold && !PXAccess.FeatureInstalled<FeaturesSet.integratedCardProcessing>();
			UIState.RaiseOrHideErrorByErrorLevelPriority<ARPayment.status>(cache, e.Row, CCActionsNoAvailable,
				Messages.CardProcessingActionsNotAvailable, PXErrorLevel.Warning);
			#endregion
		}

		protected virtual void CheckForUnreleasedIncomingApplications(PXCache sender, ARPayment document)
		{
			if (document.Released != true || document.OpenDoc != true)
			{
				return;
			}

			ARAdjust unreleasedIncomingApplication = PXSelect<
				ARAdjust,
				Where<
					ARAdjust.adjdDocType, Equal<Required<ARAdjust.adjdDocType>>,
					And<ARAdjust.adjdRefNbr, Equal<Required<ARAdjust.adjdRefNbr>>,
					And<ARAdjust.released, NotEqual<True>>>>>
				.Select(this, document.DocType, document.RefNbr);

			sender.ClearFieldErrors<ARPayment.refNbr>(document);
			string warningMessage = null;

			if (unreleasedIncomingApplication != null)
			{
				warningMessage = PXLocalizer.LocalizeFormat(
					Common.Messages.CannotApplyDocumentUnreleasedIncomingApplicationsExist,
					GetLabel.For<ARDocType>(unreleasedIncomingApplication.AdjgDocType),
					unreleasedIncomingApplication.AdjgRefNbr);
			}
			else
			{
				unreleasedIncomingApplication = PXSelectJoin<
				ARAdjust,
				InnerJoin<ARRegisterAlias,
					On<ARAdjust.adjdDocType, Equal<ARRegisterAlias.docType>,
						And<ARAdjust.adjdRefNbr, Equal<ARRegisterAlias.refNbr>>>>,
				Where<
					ARAdjust.adjgDocType, Equal<Required<ARAdjust.adjgDocType>>,
					And<ARAdjust.adjgRefNbr, Equal<Required<ARAdjust.adjgRefNbr>>,
					And<ARAdjust.adjdDocType, Equal<ARDocType.creditMemo>,
					And<ARRegisterAlias.released, NotEqual<True>>>>>>
				.Select(this, document.DocType, document.RefNbr);

			if (unreleasedIncomingApplication != null)
			{
					warningMessage = PXLocalizer.LocalizeFormat(
						Common.Messages.UnreleasedApplicationToCreditMemo,
						unreleasedIncomingApplication.AdjdRefNbr);
				}
			}

			if (warningMessage != null)
				SetUnreleasedIncomingApplicationWarning(sender, document, warningMessage);
		}

		protected virtual void SetUnreleasedIncomingApplicationWarning(PXCache sender, ARPayment document, string warningMessage)
		{
			sender.DisplayFieldWarning<ARPayment.refNbr>(
				document,
				null,
				warningMessage);

			Adjustments.Cache.AllowInsert =
			Adjustments.Cache.AllowUpdate =
			Adjustments.Cache.AllowDelete = false;
			loadInvoices.SetEnabled(false);
			autoApply.SetEnabled(false);
			adjustDocAmt.SetEnabled(false);
		}

		protected virtual void DisableViewsOnUnapprovedRefund()
		{
			Adjustments.Cache.AllowInsert = false;
			Adjustments_History.Cache.AllowInsert = false;
			PaymentCharges.Cache.AllowInsert = false;
			Approval.Cache.AllowInsert = false;
			Adjustments.Cache.AllowUpdate = false;
			Adjustments_History.Cache.AllowUpdate = false;
			PaymentCharges.Cache.AllowUpdate = false;
			Approval.Cache.AllowUpdate = false;
			Adjustments.Cache.AllowDelete = false;
			Adjustments_History.Cache.AllowDelete = false;
			CurrentDocument.Cache.AllowDelete = false;
			PaymentCharges.Cache.AllowDelete = false;
			Approval.Cache.AllowDelete = false;
		}


		private void CheckCreditCardTranStateBeforeVoiding()
		{
			ARPayment doc = Document.Current;
			if (doc == null) return;

			if (doc.DocType == ARDocType.Payment || doc.DocType == ARDocType.Prepayment)
			{
				var docLabel = TranValidationHelper.GetDocumentName(doc.DocType);
				if (ExternalTranHelper.HasImportedNeedSyncTran(this, ExternalTran))
				{
					throw new PXException(Messages.ERR_TranIsNotValidatedDocCanNotBeVoided, doc.RefNbr, docLabel);
				}

				var tran = ExternalTran.Select().RowCast<ExternalTransaction>().FirstOrDefault();
				if (tran != null)
				{
					bool refundHasValidSharedTran = ExternalTranHelper.GetSharedTranStatus(this, tran)
						== ExternalTranHelper.SharedTranStatus.Synchronized;
					if (refundHasValidSharedTran)
					{
						string refundLabel = TranValidationHelper.GetDocumentName(tran.VoidDocType);
						throw new PXException(Messages.ERR_DocCannotBeVoidedWithSharedTran, doc.RefNbr, docLabel,
							tran.TranNumber, tran.VoidRefNbr, refundLabel);
					}
				}
			}
		}

		protected virtual void CheckCashAccount(PXCache cache, ARPayment doc)
		{
			CCProcessingCenter procCenter = processingCenter.SelectSingle();
			if (procCenter?.ImportSettlementBatches != true)
				return;

			PXSelectBase<CashAccountDeposit> cashAccountDepositSelect = new PXSelect<
				CashAccountDeposit, Where<CashAccountDeposit.accountID, Equal<Required<CCProcessingCenter.depositAccountID>>,
						And<CashAccountDeposit.depositAcctID, Equal<Required<ARPayment.cashAccountID>>,
							And<Where<CashAccountDeposit.paymentMethodID, Equal<Required<ARPayment.paymentMethodID>>,
									Or<CashAccountDeposit.paymentMethodID, Equal<BQLConstants.EmptyString>>>>>>>(this);

			bool cashAccountIsClearingForDeposit = cashAccountDepositSelect.Select(procCenter.DepositAccountID, doc.CashAccountID, doc.PaymentMethodID).Any();
			CashAccount procCenterDepositAccount = CashAccount.PK.Find(this, procCenter.DepositAccountID);
			UIState.RaiseOrHideErrorByErrorLevelPriority<ARPayment.cashAccountID>(cache, doc, !cashAccountIsClearingForDeposit && doc.CashAccountID != null,
				Messages.CashAccountIsNotClearingPaymentWontBeIncludedInDeposit, PXErrorLevel.Warning, procCenterDepositAccount.CashAccountCD);
		}

		protected Dictionary<ARAdjust, PXResultset<ARInvoice>> balanceCache;
		protected virtual void FillBalanceCache(ARPayment row, bool released = false)
		{
			if (row?.DocType == null || row.RefNbr == null) return;
			if (balanceCache == null)
			{
				balanceCache = new Dictionary<ARAdjust, PXResultset<ARInvoice>>(
					new RecordKeyComparer<ARAdjust>(Adjustments.Cache));
			}
			if (balanceCache.Keys.Any(_ => _.Released == released)) return;

			foreach (PXResult<Standalone.ARAdjust, ARInvoice, ARRegisterAlias, ARTran, CurrencyInfo> res
				in Adjustments_Balance.View.SelectMultiBound(new object[] { row }, released))
			{
				Standalone.ARAdjust key = res;
				ARAdjust adj = new ARAdjust
				{
					AdjdDocType = key.AdjdDocType,
					AdjdRefNbr = key.AdjdRefNbr,
					AdjgDocType = key.AdjgDocType,
					AdjgRefNbr = key.AdjgRefNbr,
					AdjdLineNbr = key.AdjdLineNbr,
					AdjNbr = key.AdjNbr
				};
				AddBalanceCache(adj, res);
			}
		}

		protected virtual void AddBalanceCache(ARAdjust adj, PXResult res)
		{
			if (balanceCache == null)
			{
				balanceCache = new Dictionary<ARAdjust, PXResultset<ARInvoice>>(new RecordKeyComparer<ARAdjust>(Adjustments.Cache));
			}
			ARInvoice fullInvoice = PXResult.Unwrap<ARInvoice>(res);
			ARTran tran = PXResult.Unwrap<ARTran>(res);
			ARRegisterAlias register = PXResult.Unwrap<Standalone.ARRegisterAlias>(res);
			CurrencyInfo info = PXResult.Unwrap<CurrencyInfo>(res);

			if(register != null)
				PXCache<ARRegister>.RestoreCopy(fullInvoice, register);
			PXSelectorAttribute.StoreResult<ARAdjust.displayRefNbr>(this.Adjustments.Cache, adj, fullInvoice);
			CurrencyInfo_CuryInfoID.StoreResult(info);
			GetExtension<MultiCurrency>().StoreResult(info);
			balanceCache[adj] = new PXResultset<ARInvoice, ARTran>()
			{
				new PXResult<ARInvoice, ARTran>(fullInvoice, tran)
			};
		}
		public virtual void ARPayment_RowSelecting(PXCache sender, PXRowSelectingEventArgs e)
		{
			//TODO: move everything this method contains into FieldSelecting events
			ARPayment row = e.Row as ARPayment;
			if (row != null && !e.IsReadOnly)
			{
				using (new PXConnectionScope())
				{
					if (sender.GetStatus(e.Row) == PXEntryStatus.Notchanged && row.Status == ARDocStatus.Open && row.VoidAppl == false &&
						row.AdjDate != null && ((DateTime)row.AdjDate).CompareTo((DateTime)Accessinfo.BusinessDate) < 0
						   && IsImport != true)
					{
						if (Adjustments_Raw.View.SelectSingleBound(new object[] { e.Row }) == null)
						{
							FinPeriod finPeriod = FinPeriodRepository.FindFinPeriodByDate((DateTime)Accessinfo.BusinessDate, PXAccess.GetParentOrganizationID(row.BranchID));

							if (finPeriod != null)
							{
								row.AdjDate = Accessinfo.BusinessDate;
								row.AdjTranPeriodID = finPeriod.MasterFinPeriodID;
								row.AdjFinPeriodID = finPeriod.FinPeriodID;
								sender.SetStatus(e.Row, PXEntryStatus.Held);
							}
						}
					}
					FillBalanceCache(row);
					CalcApplAmounts(sender, row);
				}
			}
		}
		public virtual void CalcApplAmounts(PXCache sender, ARPayment row)
		{
			if (row.CuryApplAmt == null)
				RecalcApplAmounts(sender, row);
		}
		public virtual void RecalcApplAmounts(PXCache sender, ARPayment row)
		{
			bool IsReadOnly = false;

			PXFormulaAttribute.CalcAggregate<ARAdjust.curyAdjgAmt>(Adjustments.Cache, row, IsReadOnly);
			if (row.CuryApplAmt == null)
			{
				row.CuryApplAmt = 0m;
			}
			sender.RaiseFieldUpdated<ARPayment.curyApplAmt>(row, null);
		}
		public static void SetDocTypeList(PXCache cache, string docType)
		{
			string defValue = ARDocType.Invoice;
			List<string> values = new List<string>();
			List<string> labels = new List<string>();

			if (docType == ARDocType.Refund || docType == ARDocType.VoidRefund)
			{
				defValue = ARDocType.CreditMemo;
				values.AddRange(new string[] { ARDocType.CreditMemo, ARDocType.Payment, ARDocType.Prepayment });
				labels.AddRange(new string[] { Messages.CreditMemo, Messages.Payment, Messages.Prepayment });
			}
			else if (docType == ARDocType.Payment || docType == ARDocType.VoidPayment || docType == ARDocType.Prepayment)
			{
				values.AddRange(new string[] { ARDocType.Invoice, ARDocType.DebitMemo, ARDocType.CreditMemo, ARDocType.FinCharge });
				labels.AddRange(new string[] { Messages.Invoice, Messages.DebitMemo, Messages.CreditMemo, Messages.FinCharge });
			}
			else if (docType == ARDocType.CreditMemo)
			{
				values.AddRange(new string[] { ARDocType.Invoice, ARDocType.DebitMemo, ARDocType.FinCharge });
				labels.AddRange(new string[] { Messages.Invoice, Messages.DebitMemo, Messages.FinCharge });
			}
			else
			{
				values.AddRange(new string[] { ARDocType.Invoice, ARDocType.DebitMemo, ARDocType.FinCharge });
				labels.AddRange(new string[] { Messages.Invoice, Messages.DebitMemo, Messages.FinCharge });
			}

			if (!PXAccess.FeatureInstalled<FeaturesSet.overdueFinCharges>() && values.Contains(ARDocType.FinCharge) && labels.Contains(Messages.FinCharge))
			{
				values.Remove(ARDocType.FinCharge);
				labels.Remove(Messages.FinCharge);
			}

			PXDefaultAttribute.SetDefault<ARAdjust.adjdDocType>(cache, defValue);
			PXStringListAttribute.SetList<ARAdjust.adjdDocType>(cache, null, values.ToArray(), labels.ToArray());
		}

		public static PXAdapter CreateAdapterWithDummyView(ARPaymentEntry graph, ARPayment doc)
		{
			var dummyView = new PXView.Dummy(graph, graph.Document.View.BqlSelect,
				new List<object>() { doc });
			return new PXAdapter(dummyView);
		}

		private void SetDocTypeList(object Row)
		{
			ARPayment row = Row as ARPayment;
			if (row != null)
			{
				SetDocTypeList(Adjustments.Cache, row.DocType);
			}
		}

		protected virtual void ARPayment_DocDate_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			if (((ARPayment)e.Row).Released == false)
			{
				e.NewValue = ((ARPayment)e.Row).AdjDate;
				e.Cancel = true;
			}
		}

		protected virtual void ARPayment_DocDate_FieldUpdating(PXCache sender, PXFieldUpdatingEventArgs e)
		{
			ARPayment row = e.Row as ARPayment;
			if (row == null) return;
			ExternalTransactionState currState = ExternalTranHelper.GetActiveTransactionState(this, ExternalTran);
			DateTime? activityDate = currState.ExternalTransaction?.LastActivityDate;
			if (activityDate == null)
				return;

			if (row.VoidAppl == false)
			{
				if (currState.IsSettlementDue && DateTime.Compare(((DateTime?)e.NewValue).Value, activityDate.Value.Date) != 0)
				{
					sender.RaiseExceptionHandling<ARPayment.docDate>(row, null, new PXSetPropertyException(Messages.PaymentAndCaptureDatesDifferent, PXErrorLevel.Warning));
				}
			}
			else
			{
				if (currState.IsRefunded && DateTime.Compare(((DateTime?)e.NewValue).Value, activityDate.Value.Date) != 0)
				{
					sender.RaiseExceptionHandling<ARPayment.docDate>(row, null, new PXSetPropertyException(Messages.PaymentAndCaptureDatesDifferent, PXErrorLevel.Warning));
				}
			}
		}

		protected virtual void ARPayment_FinPeriodID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			if ((bool)((ARPayment)e.Row).Released == false)
			{
				e.NewValue = ((ARPayment)e.Row).AdjFinPeriodID;
				e.Cancel = true;
			}
		}

		protected virtual void ARPayment_TranPeriodID_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			if ((bool)((ARPayment)e.Row).Released == false)
			{
				e.NewValue = ((ARPayment)e.Row).AdjTranPeriodID;
				e.Cancel = true;
			}
		}

		protected virtual void ARPayment_DepositAfter_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			ARPayment row = (ARPayment)e.Row;
			if ((row.DocType == ARDocType.Payment || row.DocType == ARDocType.Prepayment || row.DocType == ARDocType.CashSale || row.DocType == ARDocType.Refund)
				&& row.DepositAsBatch == true)
			{
				e.NewValue = row.AdjDate;
				e.Cancel = true;
			}
		}

		protected virtual void ARPayment_DepositAsBatch_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			ARPayment row = (ARPayment)e.Row;
			if ((row.DocType == ARDocType.Payment || row.DocType == ARDocType.Prepayment || row.DocType == ARDocType.CashSale || row.DocType == ARDocType.Refund))
			{
				sender.SetDefaultExt<ARPayment.depositAfter>(e.Row);
			}
		}

		protected virtual void ARPayment_RowDeleting(PXCache sender, PXRowDeletingEventArgs e)
		{
			foreach (ARAdjust adj in Adjustments.Cache.Inserted)
			{
				Adjustments.Delete(adj);
			}

			ExternalTransactionState state = ExternalTranHelper.GetActiveTransactionState(this, ExternalTran);
			PaymentMethod pm = this.paymentmethod.Current;
			if (pm?.PaymentType == CA.PaymentMethodType.CreditCard && pm?.ARIsProcessingRequired == true && state?.IsActive == true)
			{
				throw new PXException(AR.Messages.CannotDeletedBecauseOfTransactions);
			}
		}

		protected virtual void ARPayment_RowDeleted(PXCache sender, PXRowDeletedEventArgs e)
		{
		}

		protected virtual void ARPayment_RowInserted(PXCache sender, PXRowInsertedEventArgs e)
		{
			ARPayment payment = (ARPayment)e.Row;
			if (payment.Released == false)
			{
				payment.DocDate = payment.AdjDate;
				payment.FinPeriodID = payment.AdjFinPeriodID;
				payment.TranPeriodID = payment.AdjTranPeriodID;

				sender.RaiseExceptionHandling<ARPayment.finPeriodID>(e.Row, payment.FinPeriodID, null);
			}
		}

		protected virtual void ARPayment_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			ARPayment payment = (ARPayment)e.Row;
			ExternalTransactionState state = ExternalTranHelper.GetActiveTransactionState(this, ExternalTran);
			if (payment.Released != true
				&& (((ARSetup)arsetup.Select()).IntegratedCCProcessing != true
					|| !state.IsSettlementDue))
			{
				payment.DocDate = payment.AdjDate;
				payment.FinPeriodID = payment.AdjFinPeriodID;
				payment.TranPeriodID = payment.AdjTranPeriodID;

				sender.RaiseExceptionHandling<ARPayment.finPeriodID>(e.Row, payment.FinPeriodID, null);

				PaymentCharges.UpdateChangesFromPayment(sender, e);
			}

			IsPaymentUnbalancedException(sender, payment);
		}

		public virtual void IsPaymentUnbalancedException(PXCache sender, ARPayment payment)
		{
			if (payment.OpenDoc == true && payment.Hold != true)
			{
				if (IsPaymentUnbalanced(payment))
				{
					sender.RaiseExceptionHandling<ARPayment.curyOrigDocAmt>(payment, payment.CuryOrigDocAmt, new PXSetPropertyException(Messages.DocumentOutOfBalance, PXErrorLevel.Warning));
				}
				else
				{
					sender.RaiseExceptionHandling<ARPayment.curyOrigDocAmt>(payment, null, null);
				}
			}
		}

		public virtual bool IsPaymentUnbalanced(ARPayment payment)
		{
			// It's should be allowed to enter a CustomerRefund 
			// document without any required applications, 
			// when migration mode is activated.
			// 
			bool canHaveBalance = payment.CanHaveBalance == true ||
				payment.IsMigratedRecord == true && payment.CuryInitDocBal == 0m;

			return
				canHaveBalance && payment.VoidAppl != true
			&& (payment.CuryUnappliedBal < 0m
				|| payment.CuryApplAmt < 0m && payment.CuryUnappliedBal > payment.CuryOrigDocAmt
				|| payment.CuryOrigDocAmt < 0m)
				|| !canHaveBalance && payment.CuryUnappliedBal != 0m && payment.SelfVoidingDoc != true;
		}

		public virtual void CreatePayment(ARInvoice ardoc)
		{
			CreatePayment(ardoc, null, null, null, true);
		}

		public virtual void CreatePayment(ARInvoice ardoc, string paymentType)
		{
			CreatePayment(ardoc, null, null, null, true, paymentType);
		}

		public virtual void CreatePayment(ARInvoice ardoc, CurrencyInfo info, DateTime? paymentDate, string aFinPeriod, bool overrideDesc)
		{
			string paymentType = ardoc.DocType == ARDocType.CreditMemo
				? ARDocType.Refund
				: ARDocType.Payment;
			CreatePayment(ardoc, null, null, null, true, paymentType);
		}

		public virtual void CreatePayment(
			ARInvoice ardoc, 
			CurrencyInfo info, 
			DateTime? paymentDate, 
			string aFinPeriod, 
			bool overrideDesc,
			string paymentType)
		{
			ARPayment payment = this.Document.Current;
			if (payment == null || object.Equals(payment.CustomerID, ardoc.CustomerID) == false
				|| (ardoc.PMInstanceID != null && payment.PMInstanceID != ardoc.PMInstanceID))
			{
				this.Clear();
				if (info != null)
				{
					info = GetExtension<MultiCurrency>().CloneCurrencyInfo(info);
				}

				payment = PXCache<ARPayment>.CreateCopy(this.Document.Insert(new ARPayment
				{
					DocType = paymentType
				}));

				if (info != null)
					payment.CuryInfoID = info.CuryInfoID;

				payment.BranchID = null;
				payment.CustomerID = ardoc.CustomerID;
				payment.CustomerLocationID = ardoc.CustomerLocationID;
				if (overrideDesc)
				{
					payment.DocDesc = ardoc.DocDesc;
				}

				if (paymentDate.HasValue)
				{
					payment.AdjDate = paymentDate;
				}
				else
				{
					payment.AdjDate = (DateTime.Compare((DateTime)this.Accessinfo.BusinessDate, (DateTime)ardoc.DocDate) < 0 ? ardoc.DocDate : this.Accessinfo.BusinessDate);
				}
				if (!String.IsNullOrEmpty(aFinPeriod))
					payment.AdjFinPeriodID = aFinPeriod;

				if (string.IsNullOrEmpty(ardoc.PaymentMethodID) == false)
				{
					payment.PaymentMethodID = ardoc.PaymentMethodID;
				}

				if (ardoc.PMInstanceID != null)
				{
					payment.PMInstanceID = ardoc.PMInstanceID;
				}

				if (ardoc.CashAccountID != null)
				{
					payment.CashAccountID = ardoc.CashAccountID;
				}

				payment = this.Document.Update(payment);

				if (info != null)
				{
					CurrencyInfo b_info = (CurrencyInfo)PXSelect<CurrencyInfo, Where<CurrencyInfo.curyInfoID, Equal<Current<ARPayment.curyInfoID>>>>.Select(this, null);
					b_info.CuryID = info.CuryID;
					b_info.CuryEffDate = info.CuryEffDate;
					b_info.CuryRateTypeID = info.CuryRateTypeID;
					b_info.CuryRate = info.CuryRate;
					b_info.RecipRate = info.RecipRate;
					b_info.CuryMultDiv = info.CuryMultDiv;
					this.currencyinfo.Update(b_info);
				}
			}

			ARAdjust adj = new ARAdjust();
			adj.AdjdDocType = ardoc.DocType;
			adj.AdjdRefNbr = ardoc.RefNbr;

			//set origamt to zero to apply "full" amounts to invoices.
			Document.Current.CuryOrigDocAmt = 0m;
			Document.Update(Document.Current);

			try
			{
				if (ardoc.PaymentsByLinesAllowed == true)
				{
					foreach (ARTran tran in
						PXSelect<ARTran,
							Where<ARTran.tranType, Equal<Current<ARInvoice.docType>>,
								And<ARTran.refNbr, Equal<Current<ARInvoice.refNbr>>,
								And<ARTran.curyTranBal, NotEqual<Zero>>>>,
							OrderBy<Desc<ARTran.curyTranBal>>>
							.SelectMultiBound(this, new object[] { ardoc }))
					{
						ARAdjust lineAdj = PXCache<ARAdjust>.CreateCopy(adj);
						lineAdj.AdjdLineNbr = tran.LineNbr;
						Adjustments.Insert(lineAdj);
					}
				}
				else
				{
					adj.AdjdLineNbr = 0;
					Adjustments.Insert(adj);
				}
			}
			catch (PXSetPropertyException)
			{
				throw new AP.AdjustedNotFoundException();
			}

			decimal? CuryApplAmt = Document.Current.CuryApplAmt;
			if (CuryApplAmt > 0m)
			{
			Document.Current.CuryOrigDocAmt = CuryApplAmt;
			Document.Update(Document.Current);
		}
		}
		#region BusinessProcs

		private bool _IsVoidCheckInProgress = false;

		protected virtual void ARPayment_RefNbr_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			if (_IsVoidCheckInProgress)
			{
				e.Cancel = true;
			}
		}

		protected virtual void ARPayment_FinPeriodID_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			ARPayment doc;
			if (_IsVoidCheckInProgress)
			{
				e.Cancel = true;
			}
			else
			if ((doc = e.Row as ARPayment) != null)
			{
				ExternalTransactionState state = ExternalTranHelper.GetActiveTransactionState(this, ExternalTran);
				if ((doc.Released == true || state.IsCaptured) && doc.AdjFinPeriodID.CompareTo((string)e.NewValue) < 0)
				{
					e.NewValue = FinPeriodIDAttribute.FormatForDisplay((string)e.NewValue);
					throw new PXSetPropertyException(CS.Messages.Entry_LE, FinPeriodIDAttribute.FormatForError(doc.AdjFinPeriodID));
				}
			}
		}

		protected virtual void ARPayment_AdjFinPeriodID_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			if (_IsVoidCheckInProgress)
			{
				e.Cancel = true;
			}

			ARPayment doc = (ARPayment)e.Row;

			string errMsg;
			if (!VerifyAdjFinPeriodID(doc, (string)e.NewValue, out errMsg))
			{
				e.NewValue = FinPeriodIDAttribute.FormatForDisplay((string)e.NewValue);
				throw new PXSetPropertyException(errMsg);
			}
		}

		protected virtual bool VerifyAdjFinPeriodID(ARPayment doc, string newValue, out string errMsg)
		{
			errMsg = null;
			ExternalTransactionState state = ExternalTranHelper.GetActiveTransactionState(this, ExternalTran);
			if ((doc.Released == true) &&
				doc.FinPeriodID.CompareTo(newValue) > 0)
			{
				errMsg = string.Format(CS.Messages.Entry_GE, FinPeriodIDAttribute.FormatForError(doc.FinPeriodID));
				return false;
			}

			if (doc.DocType == ARDocType.VoidPayment)
			{
				ARPayment orig_payment = PXSelect<ARPayment,
					Where2<Where<ARPayment.docType, Equal<ARDocType.payment>,
							Or<ARPayment.docType, Equal<ARDocType.prepayment>>>,
						And<ARPayment.refNbr, Equal<Required<ARPayment.refNbr>>>>>
					.SelectSingleBound(this, null, doc.RefNbr);

				if (orig_payment != null && orig_payment.FinPeriodID.CompareTo(newValue) > 0)
				{
					errMsg = string.Format(CS.Messages.Entry_GE, FinPeriodIDAttribute.FormatForError(orig_payment.FinPeriodID));
					return false;
				}
			}
			else
			{
				try
				{
					internalCall = true;

					/// We should find maximal adjusting period of adjusted applications 
					/// (excluding applications, that have been reversed in the same period)
					/// for the document, because applications in earlier period are not allowed.
					/// 
					ARAdjust adjdmax = PXSelectJoin<ARAdjust,
						LeftJoin<ARAdjust2, On<
							ARAdjust2.adjdDocType, Equal<ARAdjust.adjdDocType>,
							And<ARAdjust2.adjdRefNbr, Equal<ARAdjust.adjdRefNbr>,
							And<ARAdjust2.adjdLineNbr, Equal<ARAdjust.adjdLineNbr>,
							And<ARAdjust2.adjgDocType, Equal<ARAdjust.adjgDocType>,
							And<ARAdjust2.adjgRefNbr, Equal<ARAdjust.adjgRefNbr>,
							And<ARAdjust2.adjNbr, NotEqual<ARAdjust.adjNbr>,
							And<Switch<Case<Where<ARAdjust.voidAdjNbr, IsNotNull>, ARAdjust.voidAdjNbr>, ARAdjust.adjNbr>,
								Equal<Switch<Case<Where<ARAdjust.voidAdjNbr, IsNotNull>, ARAdjust2.adjNbr>, ARAdjust2.voidAdjNbr>>,
							And<ARAdjust2.adjgTranPeriodID, Equal<ARAdjust.adjgTranPeriodID>,
							And<ARAdjust2.released, Equal<True>,
							And<ARAdjust2.voided, Equal<True>,
							And<ARAdjust.voided, Equal<True>>>>>>>>>>>>>,
					Where<ARAdjust.adjgDocType, Equal<Required<ARAdjust.adjgDocType>>,
						And<ARAdjust.adjgRefNbr, Equal<Required<ARAdjust.adjgRefNbr>>,
							And<ARAdjust.released, Equal<True>,
							And<ARAdjust2.adjdRefNbr, IsNull>>>>,
						OrderBy<Desc<ARAdjust.adjgTranPeriodID>>>
						.SelectSingleBound(this, null, doc.DocType, doc.RefNbr);

					if (adjdmax?.AdjgFinPeriodID.CompareTo(newValue) > 0)
					{
						errMsg = string.Format(CS.Messages.Entry_GE, FinPeriodIDAttribute.FormatForError(adjdmax.AdjgFinPeriodID));
						return false;
					}
				}
				finally
				{
					internalCall = false;
				}
			}

			return true;
		}

		public virtual bool ShowCardChck(ARPayment doc)
		{
			return (doc.DocType == ARDocType.Payment || doc.DocType == ARDocType.Prepayment || doc.DocType == ARDocType.Refund) 
				&& doc.Released == false && doc.Voided == false;
		}

		public virtual void VoidCheckProcExt(ARPayment doc)
		{
			try
			{
				_IsVoidCheckInProgress = true;
				this.VoidCheckProc(doc);
			}
			finally
			{
				_IsVoidCheckInProgress = false;
			}
		}

		public virtual void SelfVoidingProc(ARPayment doc)
		{
			ARPayment payment = PXCache<ARPayment>.CreateCopy(doc);

			if (payment.OpenDoc == false)
			{
				payment.OpenDoc = true;
				Document.Cache.RaiseRowSelected(payment);
			}

			foreach (PXResult<ARAdjust> adjres in PXSelectJoin<ARAdjust,
				InnerJoin<CurrencyInfo, On<CurrencyInfo.curyInfoID, Equal<ARAdjust.adjdCuryInfoID>>>,
				Where<ARAdjust.adjgDocType, Equal<Required<ARAdjust.adjgDocType>>,
					And<ARAdjust.adjgRefNbr, Equal<Required<ARAdjust.adjgRefNbr>>,
					And<ARAdjust.voided, NotEqual<True>>>>>.Select(this, payment.DocType, payment.RefNbr))
			{
				CreateReversingApp(adjres, payment);
			}

			payment.CuryApplAmt = null;
			payment.CuryUnappliedBal = null;
			payment.CuryWOAmt = null;
			payment.CCReauthDate = null;
			payment.CCReauthTriesLeft = 0;
			payment.IsCCUserAttention = false;

			payment = Document.Update(payment);
			ARPayment.Events
				.Select(ev=>ev.OpenDocument)
				.FireOn(this,payment);
		}

		public virtual ARAdjust CreateReversingApp(ARAdjust adj, ARPayment payment)
		{
			ARAdjust adjold = PXCache<ARAdjust>.CreateCopy(adj);

			adjold.Voided = true;
			adjold.VoidAdjNbr = adjold.AdjNbr;
			adjold.Released = false;
			Adjustments.Cache.SetDefaultExt<ARAdjust.isMigratedRecord>(adjold);
			adjold.AdjNbr = payment.AdjCntr;
			adjold.AdjBatchNbr = null;
			adjold.StatementDate = null;
			adjold.AdjgDocDate = payment.AdjDate;

			ARAdjust adjnew = new ARAdjust();
			adjnew.AdjgDocType = adjold.AdjgDocType;
			adjnew.AdjgRefNbr = adjold.AdjgRefNbr;
			adjnew.AdjgBranchID = adjold.AdjgBranchID;
			adjnew.AdjdDocType = adjold.AdjdDocType;
			adjnew.AdjdRefNbr = adjold.AdjdRefNbr;
			adjnew.AdjdLineNbr = adjold.AdjdLineNbr;
			adjnew.AdjdBranchID = adjold.AdjdBranchID;
			adjnew.CustomerID = adjold.CustomerID;
			adjnew.AdjdCustomerID = adjold.AdjdCustomerID;
			adjnew.AdjNbr = adjold.AdjNbr;
			adjnew.AdjdCuryInfoID = adjold.AdjdCuryInfoID;
			adjnew.AdjdHasPPDTaxes = adjold.AdjdHasPPDTaxes;
			adjnew.InvoiceID = adjold.InvoiceID;
			adjnew.PaymentID = adjold.PaymentID;
			adjnew.MemoID = adjold.MemoID;

			try
			{
				AutoPaymentApp = true;
				IsReverseProc = true;

				adjnew = Adjustments.Insert(adjnew);

				if (adjnew != null)
				{
					adjold.AdjdOrderType = null;
					adjold.AdjdOrderNbr = null;
					adjold.CuryAdjgAmt = -1 * adjold.CuryAdjgAmt;
					adjold.CuryAdjgDiscAmt = -1 * adjold.CuryAdjgDiscAmt;
					adjold.CuryAdjgPPDAmt = -1 * adjold.CuryAdjgPPDAmt;
					adjold.CuryAdjgWOAmt = -1 * adjold.CuryAdjgWOAmt;
					adjold.AdjAmt = -1 * adjold.AdjAmt;
					adjold.AdjDiscAmt = -1 * adjold.AdjDiscAmt;
					adjold.AdjPPDAmt = -1 * adjold.AdjPPDAmt;
					adjold.AdjWOAmt = -1 * adjold.AdjWOAmt;
					adjold.CuryAdjdAmt = -1 * adjold.CuryAdjdAmt;
					adjold.CuryAdjdDiscAmt = -1 * adjold.CuryAdjdDiscAmt;
					adjold.CuryAdjdPPDAmt = -1 * adjold.CuryAdjdPPDAmt;
					adjold.CuryAdjdWOAmt = -1 * adjold.CuryAdjdWOAmt;
					adjold.RGOLAmt = -1 * adjold.RGOLAmt;
					adjold.AdjgCuryInfoID = payment.CuryInfoID;
				}
				Adjustments.Cache.SetDefaultExt<ARAdjust.noteID>(adjold);

				Adjustments.Update(adjold);
				FinPeriodIDAttribute.SetPeriodsByMaster<ARAdjust.adjgFinPeriodID>(this.Adjustments.Cache, adjnew, this.Document.Current.AdjTranPeriodID);
			}
			finally
			{
				AutoPaymentApp = false;
				IsReverseProc = false;
			}

			return adjnew;
		}

		public virtual void VoidCheckProc(ARPayment doc)
		{
			this.Clear(PXClearOption.PreserveTimeStamp);

			foreach (PXResult<ARPayment, CurrencyInfo, Currency, Customer> res in ARPayment_CurrencyInfo_Currency_Customer.Select(this, (object)doc.DocType, doc.RefNbr))
			{
				ARPayment origDocument = res;
				//Set IsReadOnly false? Copy it?
				CurrencyInfo info = GetExtension<MultiCurrency>().CloneCurrencyInfo(res);
				ARPayment payment = Document.Insert(new ARPayment
				{
					DocType = origDocument.DocType,
					RefNbr = origDocument.RefNbr,
					CuryInfoID = info.CuryInfoID,
					VoidAppl = true
				});
				payment = PXCache<ARPayment>.CreateCopy(origDocument);
				Document.Cache.SetDefaultExt<ARPayment.noteID>(payment);

				payment.CuryInfoID = info.CuryInfoID;
				payment.VoidAppl = true;
				payment.CATranID = null;

				//Set original document reference
				payment.OrigDocType = origDocument.DocType;
				payment.OrigRefNbr = origDocument.RefNbr;
				payment.OrigModule = BatchModule.AR;

				// Must be set for RowSelected event handler
				payment.OpenDoc = true;
				payment.Released = false;
				Document.Cache.SetDefaultExt<ARPayment.hold>(payment);
				Document.Cache.SetDefaultExt<ARPayment.isMigratedRecord>(payment);
				Document.Cache.SetDefaultExt<ARPayment.status>(payment);
				payment.LineCntr = 0;
				payment.AdjCntr = 0;
				payment.BatchNbr = null;
				payment.CuryOrigDocAmt = -1 * payment.CuryOrigDocAmt;
				payment.OrigDocAmt = -1 * payment.OrigDocAmt;
				payment.CuryInitDocBal = -1 * payment.CuryInitDocBal;
				payment.InitDocBal = -1 * payment.InitDocBal;
				payment.CuryChargeAmt = 0;
				payment.CuryConsolidateChargeTotal = 0;
				payment.CuryApplAmt = null;
				payment.CuryUnappliedBal = null;
				payment.CuryWOAmt = null;
				payment.DocDate = doc.DocDate;
				payment.AdjDate = doc.DocDate;
				FinPeriodIDAttribute.SetPeriodsByMaster<ARPayment.adjFinPeriodID>(Document.Cache, payment, doc.AdjTranPeriodID);
				payment.StatementDate = null;
				payment.CCReauthDate = null;
				payment.CCReauthTriesLeft = 0;
				payment.IsCCUserAttention = false;

				if (payment.Cleared == true)
				{
					payment.ClearDate = payment.DocDate;
				}
				else
				{
					payment.ClearDate = null;
				}

				string paymentMethod = payment.PaymentMethodID;
				if (payment.DepositAsBatch == true)
				{
					if (!String.IsNullOrEmpty(payment.DepositNbr))
					{
						PaymentMethod pm = PXSelectReadonly<PaymentMethod,
									Where<PaymentMethod.paymentMethodID, Equal<Required<PaymentMethod.paymentMethodID>>>>.Select(this, payment.PaymentMethodID);
						bool voidOnDepositAccount = pm.ARVoidOnDepositAccount ?? false;
						if (!voidOnDepositAccount)
						{
							if (payment.Deposited == false)
							{
								throw new PXException(Messages.ARPaymentIsIncludedIntoCADepositAndCannotBeVoided);
							}
							PXResult<CADeposit, CashAccount> depositRes = (PXResult<CADeposit, CashAccount>)PXSelectJoin<CADeposit, InnerJoin<CashAccount, On<CashAccount.cashAccountID, Equal<CADeposit.cashAccountID>>>,
												Where<CADeposit.tranType, Equal<Required<CADeposit.tranType>>,
													And<CADeposit.refNbr, Equal<Required<CADeposit.refNbr>>>>>.Select(this, payment.DepositType, payment.DepositNbr);

							if (depositRes == null)
							{
								throw new PXException(Messages.PaymentRefersToInvalidDeposit, GetLabel.For<DepositType>(payment.DepositType), payment.DepositNbr);
							}
							CADeposit deposit = depositRes;
							payment.CashAccountID = deposit.CashAccountID;
						}
						else
						{
							payment.DepositType = null;
							payment.DepositNbr = null;
							payment.Deposited = false;
						}
					}
				}

				this.Document.Update(payment);

				using (new PX.SM.SuppressWorkflowAutoPersistScope(this))
				{
					this.initializeState.Press();
				}

				payment = this.Document.Current;

				if (payment.PaymentMethodID != paymentMethod)
				{
					payment.PaymentMethodID = paymentMethod;
					payment = this.Document.Update(payment);
				}

				this.Document.Cache.SetValueExt<ARPayment.adjFinPeriodID>(payment, FinPeriodIDAttribute.FormatForDisplay(doc.AdjFinPeriodID));

				if (info != null)
				{
					CurrencyInfo b_info = (CurrencyInfo)PXSelect<CurrencyInfo, Where<CurrencyInfo.curyInfoID, Equal<Current<ARPayment.curyInfoID>>>>.Select(this, null);
					b_info.CuryID = info.CuryID;
					b_info.CuryEffDate = info.CuryEffDate;
					b_info.CuryRateTypeID = info.CuryRateTypeID;
					b_info.CuryRate = info.CuryRate;
					b_info.RecipRate = info.RecipRate;
					b_info.CuryMultDiv = info.CuryMultDiv;
					this.currencyinfo.Update(b_info);
				}
			}

			foreach (PXResult<ARAdjust, CurrencyInfo> adjres in PXSelectJoin<ARAdjust,
				InnerJoin<CurrencyInfo, On<CurrencyInfo.curyInfoID, Equal<ARAdjust.adjdCuryInfoID>>>,
				Where<ARAdjust.adjgDocType, Equal<Required<ARAdjust.adjgDocType>>,
					And<ARAdjust.adjgRefNbr, Equal<Required<ARAdjust.adjgRefNbr>>,
					And<ARAdjust.voided, NotEqual<True>,
					And<ARAdjust.isInitialApplication, NotEqual<True>>>>>>.Select(this, doc.DocType, doc.RefNbr))
			{
				ARAdjust adj = PXCache<ARAdjust>.CreateCopy((ARAdjust)adjres);
				Adjustments.Cache.SetDefaultExt<ARAdjust.noteID>(adj);

				if ((doc.DocType != ARDocType.CreditMemo || doc.PendingPPD != true) &&
					adj.AdjdHasPPDTaxes == true &&
					adj.PendingPPD != true)
				{
					ARAdjust adjPPD = GetPPDApplication(this, adj.AdjdDocType, adj.AdjdRefNbr);
					if (adjPPD != null && (adjPPD.AdjgDocType != adj.AdjgDocType || adjPPD.AdjgRefNbr != adj.AdjgRefNbr))
					{
						adj = adjres;
						this.Clear();
						adj = (ARAdjust)Adjustments.Cache.Update(adj);
						Document.Current = Document.Search<ARPayment.refNbr>(adj.AdjgRefNbr, adj.AdjgDocType);
						Adjustments.Cache.RaiseExceptionHandling<ARAdjust.adjdRefNbr>(adj, adj.AdjdRefNbr,
							new PXSetPropertyException(Messages.PPDApplicationExists, PXErrorLevel.RowError, adjPPD.AdjgRefNbr));

						throw new PXSetPropertyException(Messages.PPDApplicationExists, adjPPD.AdjgRefNbr);
					}
				}

				adj.VoidAppl = true;
				adj.Released = false;
				Adjustments.Cache.SetDefaultExt<ARAdjust.isMigratedRecord>(adj);
				adj.VoidAdjNbr = adj.AdjNbr;
				adj.AdjNbr = 0;
				adj.AdjBatchNbr = null;
				adj.StatementDate = null;

				ARAdjust adjnew = new ARAdjust();
				adjnew.AdjgDocType = adj.AdjgDocType;
				adjnew.AdjgRefNbr = adj.AdjgRefNbr;
				adjnew.AdjgBranchID = adj.AdjgBranchID;
				adjnew.AdjdDocType = adj.AdjdDocType;
				adjnew.AdjdRefNbr = adj.AdjdRefNbr;
				adjnew.AdjdLineNbr = adj.AdjdLineNbr;
				adjnew.AdjdBranchID = adj.AdjdBranchID;
				adjnew.CustomerID = adj.CustomerID;
				adjnew.AdjdCustomerID = adj.AdjdCustomerID;
				adjnew.AdjdCuryInfoID = adj.AdjdCuryInfoID;
				adjnew.AdjdHasPPDTaxes = adj.AdjdHasPPDTaxes;

				if (this.Adjustments.Insert(adjnew) == null)
				{
					adj = (ARAdjust)adjres;
					this.Clear();
					adj = (ARAdjust)this.Adjustments.Cache.Update(adj);
					this.Document.Current = this.Document.Search<ARPayment.refNbr>(adj.AdjgRefNbr, adj.AdjgDocType);
					this.Adjustments.Cache.RaiseExceptionHandling<ARAdjust.adjdRefNbr>(adj, adj.AdjdRefNbr, new PXSetPropertyException(Messages.MultipleApplicationError, PXErrorLevel.RowError));

					throw new PXException(Messages.MultipleApplicationError);
				}
				adj.PaymentID = Document.Current.NoteID;
				adj.CuryAdjgAmt = -1 * adj.CuryAdjgAmt;
				adj.CuryAdjgDiscAmt = -1 * adj.CuryAdjgDiscAmt;
				adj.CuryAdjgPPDAmt = -1 * adj.CuryAdjgPPDAmt;
				adj.CuryAdjgWOAmt = -1 * adj.CuryAdjgWOAmt;
				adj.AdjAmt = -1 * adj.AdjAmt;
				adj.AdjDiscAmt = -1 * adj.AdjDiscAmt;
				adj.AdjPPDAmt = -1 * adj.AdjPPDAmt;
				adj.AdjWOAmt = -1 * adj.AdjWOAmt;
				adj.CuryAdjdAmt = -1 * adj.CuryAdjdAmt;
				adj.CuryAdjdDiscAmt = -1 * adj.CuryAdjdDiscAmt;
				adj.CuryAdjdPPDAmt = -1 * adj.CuryAdjdPPDAmt;
				adj.CuryAdjdWOAmt = -1 * adj.CuryAdjdWOAmt;
				adj.RGOLAmt = -1 * adj.RGOLAmt;
				adj.AdjgCuryInfoID = this.Document.Current.CuryInfoID;

			    FinPeriodIDAttribute.SetPeriodsByMaster<ARAdjust.adjgFinPeriodID>(this.Adjustments.Cache, adj, this.Document.Current.AdjTranPeriodID);

				this.Adjustments.Update(adj);
			}
			PaymentCharges.ReverseCharges(doc, Document.Current);
		}

		public virtual void RefundCheckProc(ARPayment doc)
		{
			this.Clear(PXClearOption.PreserveTimeStamp);

			foreach (PXResult<ARPayment, CurrencyInfo, Currency, Customer> res in ARPayment_CurrencyInfo_Currency_Customer.Select(this, (object)doc.DocType, doc.RefNbr))
			{
				ARPayment origDocument = (ARPayment)res;
				CurrencyInfo origCurrencyInfo = (CurrencyInfo)res;
				
				var refund = CopyRefundFromPayment(doc, origCurrencyInfo, origDocument);
				CreateApplicationOnRefundToOriginalPayment(refund, origDocument);
			}
		}

		private ARPayment CopyRefundFromPayment(ARPayment doc, CurrencyInfo origCurrencyInfo, ARPayment origPayment)
		{
			CurrencyInfo info = PXCache<CurrencyInfo>.CreateCopy(origCurrencyInfo);
			info.CuryInfoID = null;
			info.IsReadOnly = false;
			info = PXCache<CurrencyInfo>.CreateCopy(this.currencyinfo.Insert(info));

			ARPayment payment = new ARPayment();
			payment.DocType = ARDocType.Refund;
			payment.RefNbr = null;
			payment.CuryInfoID = info.CuryInfoID;

			payment = Document.Insert(payment);
			string newRefNbr = payment.RefNbr;
			payment = PXCache<ARPayment>.CreateCopy(origPayment);
			payment.DocType = ARDocType.Refund;
			payment.RefNbr = newRefNbr;
			payment.ExtRefNbr = "";
			Document.Cache.SetDefaultExt<ARPayment.noteID>(payment);

			ICCPaymentProcessingRepository repository = CCPaymentProcessingRepository.GetCCPaymentProcessingRepository();
			ExternalTransaction actualExtTran = repository.GetExternalTransaction(origPayment.CCActualExternalTransactionID);

			payment.RefTranExtNbr = actualExtTran?.TranNumber;

			payment.CuryInfoID = info.CuryInfoID;
			payment.CATranID = null;

			// Must be set for RowSelected event handler
			payment.OpenDoc = true;
			payment.Released = false;
			Document.Cache.SetDefaultExt<ARPayment.hold>(payment);
			Document.Cache.SetDefaultExt<ARPayment.isMigratedRecord>(payment);
			Document.Cache.SetDefaultExt<ARPayment.status>(payment);
			payment.LineCntr = 0;
			payment.AdjCntr = 0;
			payment.BatchNbr = null;
			payment.CuryOrigDocAmt = origPayment.CuryUnappliedBal;
			payment.CuryChargeAmt = 0;
			payment.CurySOApplAmt = 0;
			payment.CuryConsolidateChargeTotal = 0;
			payment.CuryApplAmt = null;
			payment.CuryUnappliedBal = null;
			payment.CuryWOAmt = null;
			payment.DocDate = doc.DocDate;
			payment.AdjDate = doc.DocDate;
			FinPeriodIDAttribute.SetPeriodsByMaster<ARPayment.adjFinPeriodID>(Document.Cache, payment, doc.AdjTranPeriodID);
			payment.StatementDate = null;
			payment.CCReauthDate = null;
			payment.CCReauthTriesLeft = 0;
			payment.IsCCUserAttention = false;
			payment.Deposited = false;
			payment.DepositDate = null;
			payment.DepositNbr = null;

			if (payment.Cleared == true)
			{
				payment.ClearDate = payment.DocDate;
			}
			else
			{
				payment.ClearDate = null;
			}

			string paymentMethod = payment.PaymentMethodID;

			object cashAccountId = payment.CashAccountID;
			try
			{
				this.Document.Cache.RaiseFieldVerifying<ARPayment.cashAccountID>(payment, ref cashAccountId);
			}
			catch (PXSetPropertyException)
			{
				this.Document.Cache.SetDefaultExt<ARPayment.cashAccountID>(payment);
			}

			this.Document.Update(payment);
			this.initializeState.Press();
			payment = this.Document.Current;

			if (payment.PaymentMethodID != paymentMethod)
			{
				payment.PaymentMethodID = paymentMethod;
				payment = this.Document.Update(payment);
			}

			this.Document.Cache.SetValueExt<ARPayment.adjFinPeriodID>(payment,
				FinPeriodIDAttribute.FormatForDisplay(doc.AdjFinPeriodID));

			if (info != null)
			{
				var b_info = (CurrencyInfo) PXSelect<CurrencyInfo,
											Where<CurrencyInfo.curyInfoID, Equal<Current<ARPayment.curyInfoID>>>>
											.Select(this, null);
				b_info.CuryID = info.CuryID;
				b_info.CuryRateTypeID = info.CuryRateTypeID;
				this.currencyinfo.Update(b_info);
			}

			return payment;
		}

		private ARAdjust CreateApplicationOnRefundToOriginalPayment(ARPayment refund, ARPayment origDocument)
		{
			var adjnew = (ARAdjust) Adjustments.Cache.CreateInstance();
			adjnew.AdjgDocType = refund.DocType;
			adjnew.AdjgRefNbr = refund.RefNbr;
			adjnew.AdjdDocType = origDocument.DocType;
			adjnew.AdjdRefNbr = origDocument.RefNbr;
			adjnew = Adjustments.Insert(adjnew);
			return adjnew;
		}

		protected virtual void ValidatePaymentForRefund(ARPayment document)
		{
			if (document.Released != true || document.OpenDoc != true)
			{
				return;
			}

			ARAdjust unreleasedIncomingApplication =
				SelectFrom<ARAdjust>
					.Where<ARAdjust.adjdDocType.IsEqual<@P.AsString>
						.And<ARAdjust.adjdRefNbr.IsEqual<@P.AsString>>
						.And<ARAdjust.released.IsNotEqual<True>>>
					.View.Select(this, document.DocType, document.RefNbr);
				
			if (unreleasedIncomingApplication != null)
			{
				var errorMsg = PXLocalizer.LocalizeFormat(
					Common.Messages.CannotApplyDocumentUnreleasedIncomingApplicationsExist,
					GetLabel.For<ARDocType>(unreleasedIncomingApplication.AdjgDocType),
					unreleasedIncomingApplication.AdjgRefNbr);
				throw new PXException(errorMsg);
			}
		}
		
		#endregion

		public class CurrencyInfoSelect : PXSelect<CurrencyInfo, Where<CurrencyInfo.curyInfoID, Equal<Required<CurrencyInfo.curyInfoID>>>>
		{
			public class PXView : Data.PXView
			{
				protected Dictionary<long?, object> _cached = new Dictionary<long?, object>();

				public PXView(PXGraph graph, bool isReadOnly, BqlCommand select)
					: base(graph, isReadOnly, select)
				{
				}

				public PXView(PXGraph graph, bool isReadOnly, BqlCommand select, Delegate handler)
					: base(graph, isReadOnly, select, handler)
				{
				}

				public override object SelectSingle(params object[] parameters)
				{
					object result = null;
					if (!_cached.TryGetValue((long?)parameters[0], out result))
					{
						result = base.SelectSingle(parameters);

						if (result != null)
						{
							_cached.Add((long?)parameters[0], result);
						}
					}

					return result;
				}

				public override List<object> SelectMulti(params object[] parameters)
				{
					List<object> ret = new List<object>();

					object item;
					if ((item = SelectSingle(parameters)) != null)
					{
						ret.Add(item);
					}

					return ret;
				}

				public override void Clear()
				{
					_cached.Clear();
					base.Clear();
				}
			}


			public CurrencyInfoSelect(PXGraph graph)
				: base(graph)
			{
				View = new PXView(graph, false, new Select<CurrencyInfo, Where<CurrencyInfo.curyInfoID, Equal<Required<CurrencyInfo.curyInfoID>>>>());

				graph.RowDeleted.AddHandler<CurrencyInfo>(CurrencyInfo_RowDeleted);
			}

			public virtual void CurrencyInfo_RowDeleted(PXCache sender, PXRowDeletedEventArgs e)
			{
				View.Clear();
			}
		}
	}
}
