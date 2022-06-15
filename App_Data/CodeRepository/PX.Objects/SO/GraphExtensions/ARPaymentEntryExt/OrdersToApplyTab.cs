using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.AR;
using PX.Objects.CS;
using PXLoadInvoiceException = PX.Objects.AR.ARPaymentEntry.PXLoadInvoiceException;
using LoadOptions = PX.Objects.AR.ARPaymentEntry.LoadOptions;
using PX.Objects.CM.Extensions;

namespace PX.Objects.SO.GraphExtensions.ARPaymentEntryExt
{
	public class OrdersToApplyTab : PXGraphExtension<ARPaymentEntry.MultiCurrency, ARPaymentEntry>
	{
		#region DataMembers

		[PXViewName(AR.Messages.OrdersToApply)]
		[PXCopyPasteHiddenView]
		public PXSelectJoin<
			SOAdjust,
				LeftJoin<SOOrder,
					On<SOOrder.orderType, Equal<SOAdjust.adjdOrderType>,
					And<SOOrder.orderNbr, Equal<SOAdjust.adjdOrderNbr>>>>,
			Where<
				SOAdjust.adjgDocType, Equal<Current<ARPayment.docType>>,
				And<SOAdjust.adjgRefNbr, Equal<Current<ARPayment.refNbr>>>>>
			SOAdjustments;

		public PXSelectJoin<SOAdjust,
			InnerJoin<SOOrder, On<SOOrder.orderType, Equal<SOAdjust.adjdOrderType>, And<SOOrder.orderNbr, Equal<SOAdjust.adjdOrderNbr>>>,
			InnerJoin<CurrencyInfo, On<CurrencyInfo.curyInfoID, Equal<SOOrder.curyInfoID>>>>,
			Where<SOAdjust.adjgDocType, Equal<Current<ARPayment.docType>>,
				And<SOAdjust.adjgRefNbr, Equal<Current<ARPayment.refNbr>>>>> SOAdjustments_Orders;

		public PXSelect<SOAdjust,
			Where<SOAdjust.adjgDocType, Equal<Current<ARPayment.docType>>,
				And<SOAdjust.adjgRefNbr, Equal<Current<ARPayment.refNbr>>>>> SOAdjustments_Raw;

		public PXSelect<SOOrder,
			Where<SOOrder.customerID, Equal<Required<SOOrder.customerID>>,
				And<SOOrder.orderType, Equal<Required<SOOrder.orderType>>,
				And<SOOrder.orderNbr, Equal<Required<SOOrder.orderNbr>>>>>> SOOrder_CustomerID_OrderType_RefNbr;

		#endregion

		#region Well-known extension

		public ARPaymentEntry.ARPaymentSOBalanceCalculator SOBalanceCalculator
			=> Base.FindImplementation<ARPaymentEntry.ARPaymentSOBalanceCalculator>();

		#endregion // Well-known extension


		#region Buttons

		public PXAction<ARPayment> loadOrders;
		[PXUIField(DisplayName = "Load Orders", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.Refresh)]
		[ARMigrationModeDependentActionRestriction(
			restrictInMigrationMode: true,
			restrictForRegularDocumentInMigrationMode: true,
			restrictForUnreleasedMigratedDocumentInNormalMode: true)]
		public virtual IEnumerable LoadOrders(PXAdapter adapter)
		{
			if (Base.loadOpts != null && Base.loadOpts.Current != null)
			{
				Base.loadOpts.Current.IsInvoice = false;
			}
			var res = Base.loadOpts.AskExt();
			if (res == WebDialogResult.OK || res == WebDialogResult.Yes)
			{
				LoadOrdersProc(false, Base.loadOpts.Current);
			}
			return adapter.Get();
		}

		public PXAction<ARPayment> viewSODocumentToApply;
		[PXUIField(
			DisplayName = "View Order",
			MapEnableRights = PXCacheRights.Select,
			MapViewRights = PXCacheRights.Select,
			Visible = false)]
		[PXLookupButton]
		public virtual IEnumerable ViewSODocumentToApply(PXAdapter adapter)
		{
			SOAdjust row = SOAdjustments.Current;
			if (row != null && !(String.IsNullOrEmpty(row.AdjdOrderType) || String.IsNullOrEmpty(row.AdjdOrderNbr)))
			{
				SOOrderEntry iegraph = PXGraph.CreateInstance<SOOrderEntry>();
				iegraph.Document.Current = iegraph.Document.Search<SOOrder.orderNbr>(row.AdjdOrderNbr, row.AdjdOrderType);
				if (iegraph.Document.Current != null)
				{
					throw new PXRedirectRequiredException(iegraph, true, "View Document") { Mode = PXBaseRedirectException.WindowMode.NewWindow };
				}
			}
			return adapter.Get();
		}

		#endregion // Buttons

		#region Event Handlers

		#region SOOrder Event Handlers
		#region CacheAttached
		// We need this code to disable CurrencyInfo attribute on the SOOrder.CuryInfoID field, 
		// which is may set an incorrect value to the current CurrencyInfo.IsReadOnly flag.
		[PXDBLong]
		protected virtual void SOOrder_CuryInfoID_CacheAttached(PXCache sender) { }
		#endregion // CacheAttached
		#endregion // SOOrder Event Handlers

		#region ARPayment Event Handlers

		#region CacheAttached
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRemoveBaseAttribute(typeof(PXUnboundDefaultAttribute))]
		protected virtual void _(Events.CacheAttached<ARPayment.curySOApplAmt> e) { }
		#endregion // CacheAttached

		protected virtual void _(Events.RowUpdated<ARPayment> eventArgs)
		{
			if (!eventArgs.Cache.ObjectsEqual<ARPayment.refTranExtNbr>(eventArgs.Row, eventArgs.OldRow))
			{
				foreach (SOAdjust adj in SOAdjustments.Select())
				{
					SOAdjustments.Cache.MarkUpdated(adj);
				}
			}
		}

		#endregion // ARPayemnt Event Handlers
		#region SOAdjust Event Handlers

		protected virtual void _(Events.RowPersisting<SOAdjust> eventArgs)
		{
			if (eventArgs.Operation.Command().IsIn(PXDBOperation.Insert, PXDBOperation.Update))
			{
				if (!string.IsNullOrEmpty(Base.Document.Current.RefTranExtNbr) && eventArgs.Row?.ValidateCCRefundOrigTransaction != false)
				{
					SOLine returnLine = SelectFrom<SOLine>
						.InnerJoin<ARAdjust>.On<ARAdjust.adjdDocType.IsEqual<SOLine.invoiceType>
							.And<ARAdjust.adjdRefNbr.IsEqual<SOLine.invoiceNbr>>>
						.InnerJoin<ExternalTransaction>.On<ExternalTransaction.docType.IsEqual<ARAdjust.adjgDocType>
							.And<ExternalTransaction.refNbr.IsEqual<ARAdjust.adjgRefNbr>>>
						.Where<SOLine.orderType.IsEqual<SOAdjust.adjdOrderType.FromCurrent>
							.And<SOLine.orderNbr.IsEqual<SOAdjust.adjdOrderNbr.FromCurrent>>
							.And<ExternalTransaction.processingCenterID.IsEqual<ARPayment.processingCenterID.FromCurrent>>
							.And<ExternalTransaction.tranNumber.IsEqual<ARPayment.refTranExtNbr.FromCurrent>>
							.And<ARAdjust.voided.IsNotEqual<True>>.And<ARAdjust.curyAdjdAmt.IsNotEqual<decimal0>>
							.And<SOLine.curyLineAmt.IsNotEqual<decimal0>>>
						.View.SelectSingleBound(Base, new[] { eventArgs.Row });
					if (returnLine == null)
					{
						eventArgs.Cache.RaiseExceptionHandling<SOAdjust.adjdOrderNbr>(
							eventArgs.Row, eventArgs.Row.AdjdOrderNbr,
							new PXSetPropertyException(Messages.SOHasNoItemsToReturnPaidWithTransaction,
							eventArgs.Row.AdjdOrderNbr, Base.Document.Current.RefTranExtNbr));
					}
				}

				if (eventArgs.Row.CuryDocBal < 0m &&
					!IsApplicationToBlanketOrderWithChild(eventArgs.Row) &&
					!Base.IgnoreNegativeOrderBal) // TODO: SOCreatePayment: Temporary fix ARPayment bug (AC-159389), after fix we should remove this code.
				{
					bool isUpdate = (eventArgs.Operation & PXDBOperation.Command) == PXDBOperation.Update;
					bool balanceChanged = false;

					if (isUpdate)
					{
						var original = eventArgs.Cache.GetOriginal(eventArgs.Row);
						balanceChanged = original == null ||
							!eventArgs.Cache.ObjectsEqual<SOAdjust.curyAdjgAmt, SOAdjust.adjAmt, SOAdjust.curyAdjdAmt>(eventArgs.Row, original);
					}

					if (!isUpdate || balanceChanged)
						eventArgs.Cache.RaiseExceptionHandling<SOAdjust.curyAdjgAmt>(eventArgs.Row, eventArgs.Row.CuryAdjgAmt,
							new PXSetPropertyException(AR.Messages.DocumentBalanceNegative));
				}
			}
		}

		protected virtual void _(Events.RowInserting<SOAdjust> e)
		{
			string errmsg = PXUIFieldAttribute.GetError<SOAdjust.adjdOrderNbr>(e.Cache, e.Row);

			e.Cancel = e.Row.AdjdOrderNbr == null || string.IsNullOrEmpty(errmsg) == false;
		}

		protected virtual void _(Events.RowDeleting<SOAdjust> e)
		{
			if (e.Row.CuryAdjdBilledAmt > 0m)
			{
				throw new PXSetPropertyException(ErrorMessages.CantDeleteRecord);
			}
		}

		protected virtual void _(Events.RowDeleted<SOAdjust> e)
		{
			if (e.Row.AdjdCuryInfoID != e.Row.AdjgCuryInfoID && e.Row.AdjdCuryInfoID != e.Row.AdjdOrigCuryInfoID)
			{
				foreach (CurrencyInfo info in Base.CurrencyInfo_CuryInfoID.Select(e.Row.AdjdCuryInfoID))
				{
					Base.currencyinfo.Delete(info);
				}
			}
		}

		protected virtual void _(Events.FieldVerifying<SOAdjust, SOAdjust.adjdOrderNbr> e)
		{
			if (e.Row != null && !e.Cancel)
			{
				if (e.NewValue != null)
				{
					var ord = PXSelectorAttribute.Select<SOAdjust.adjdOrderNbr>(e.Cache, e.Row, e.NewValue);
					if (ord == null)
					{
						throw new PXSetPropertyException<SOAdjust.adjdOrderNbr>(AR.Messages.WrongOrderNbr, PXErrorLevel.Error);
					}
				}

				if (e.ExternalCall)
				{
					if (PXSelectJoin<SOOrder,
							 InnerJoin<Terms, On<Terms.termsID, Equal<SOOrder.termsID>>>,
							 Where<SOOrder.orderType, Equal<Required<SOAdjust.adjdOrderType>>,
							 And<SOOrder.orderNbr, Equal<Required<SOAdjust.adjdOrderNbr>>,
							 And<Terms.installmentType, NotEqual<TermsInstallmentType.single>>>>>.Select(Base, e.Row.AdjdOrderType, e.NewValue).Count() > 0)
					{
						throw new PXSetPropertyException(AR.Messages.PrepaymentAppliedToMultiplyInstallments);
					}
				}

				e.Cancel = Base.IsAdjdRefNbrFieldVerifyingDisabled(e.Row);
			}
		}

		protected virtual void _(Events.FieldUpdated<SOAdjust, SOAdjust.adjdOrderNbr> e)
		{
			try
			{
				if (e.Row.AdjdCuryInfoID == null)
				{
					foreach (PXResult<SOOrder, CurrencyInfo> res in
						PXSelectJoin<SOOrder,
							InnerJoin<CurrencyInfo, On<CurrencyInfo.curyInfoID, Equal<SOOrder.curyInfoID>>>,
							Where<SOOrder.orderType, Equal<Required<SOOrder.orderType>>,
								And<SOOrder.orderNbr, Equal<Required<SOOrder.orderNbr>>>>>
							.Select(Base, e.Row.AdjdOrderType, e.Row.AdjdOrderNbr))
					{
						UpdateAppliedToOrderAmount(res, res, e.Row);
						return;
					}
				}
			}
			catch (PXSetPropertyException ex)
			{
				throw new PXException(ex.Message);
			}
		}

		protected virtual void _(Events.FieldUpdated<SOAdjust, SOAdjust.adjdOrderType> e)
		{
			if (e.Row != null && (string)e.OldValue != e.Row.AdjdOrderType)
			{
				var value = e.Cache.GetValue<SOAdjust.adjdOrderNbr>(e.Row);

				try
				{
					e.Cache.RaiseFieldVerifying<SOAdjust.adjdOrderNbr>(e.Row, ref value);
				}
				catch (PXSetPropertyException<SOAdjust.adjdOrderNbr>)
				{
					e.Cache.SetValue<SOAdjust.adjdOrderNbr>(e.Row, null);
				}
			}
		}

		#endregion // SOAdjust Event Handlers

		#endregion // Event Handlers

		#region Overrides

		/// <summary>
		/// Overrides <see cref="ARPaymentEntry.ARPayment_RowSelected(PXCache, PXRowSelectedEventArgs)" />
		/// </summary>
		protected virtual void _(Events.RowSelected<ARPayment> e, PXRowSelected baseMethod)
		{
			if (e.Row == null || Base.InternalCall)
			{
				baseMethod?.Invoke(e.Cache, e.Args);
				return;
			}

			bool allowEditSOAdjustments = (e.Row.Status != ARDocStatus.Closed && e.Row.VoidAppl != true &&
				e.Row.Voided != true && e.Row.PaymentsByLinesAllowed != true);

			SOAdjustments.Cache.AllowUpdate = allowEditSOAdjustments;
			SOAdjustments.Cache.AllowDelete = allowEditSOAdjustments;
			SOAdjustments.Cache.AllowInsert = allowEditSOAdjustments;
			SOAdjustments.Cache.AllowSelect = e.Row.IsMigratedRecord != true || e.Row.Released == true;

			baseMethod?.Invoke(e.Cache, e.Args);

			loadOrders.SetEnabled(Base.loadInvoices.GetEnabled());
		}

		/// <summary>
		/// Overrides <see cref="ARPaymentEntry.SetUnreleasedIncomingApplicationWarning(PXCache, ARPayment, string)" />
		/// </summary>
		[PXOverride]
		public virtual void SetUnreleasedIncomingApplicationWarning(PXCache sender, ARPayment document, string warningMessage,
			Action<PXCache, ARPayment, string> baseMethod)
		{
			baseMethod(sender, document, warningMessage);
			SOAdjustments.Cache.AllowInsert = false;
			SOAdjustments.Cache.AllowUpdate = false;
			SOAdjustments.Cache.AllowDelete = false;
		}

		/// <summary>
		/// Overrides <see cref="ARPaymentEntry.DisableViewsOnUnapprovedRefund()" />
		/// </summary>
		[PXOverride]
		public virtual void DisableViewsOnUnapprovedRefund(Action baseMethod)
		{
			baseMethod();
			SOAdjustments.Cache.AllowInsert = false;
			SOAdjustments.Cache.AllowUpdate = false;
			SOAdjustments.Cache.AllowDelete = false;
		}

		protected virtual void _(Events.RowUpdated<CurrencyInfo> e, PXRowUpdated baseMethod)
		{
			baseMethod?.Invoke(e.Cache, e.Args);

			if (!e.Cache.ObjectsEqual<CurrencyInfo.curyID, CurrencyInfo.curyRate, CurrencyInfo.curyMultDiv>(e.Row, e.OldRow))
			{
				foreach (SOAdjust soAdj in
					PXSelect<SOAdjust, Where<SOAdjust.adjgCuryInfoID, Equal<Required<SOAdjust.adjgCuryInfoID>>>>
					.Select(Base, e.Row.CuryInfoID))
				{
					SOAdjustments.Cache.MarkUpdated(soAdj);
					SOBalanceCalculator.CalcBalances(soAdj, true, true);

					if (soAdj.CuryDocBal < 0m && !IsApplicationToBlanketOrderWithChild(soAdj))
					{
						SOAdjustments.Cache.RaiseExceptionHandling<SOAdjust.curyAdjgAmt>(
							soAdj, soAdj.CuryAdjgAmt, new PXSetPropertyException(Messages.DocumentBalanceNegative));
					}
				}
			}
		}

		/// <summary>
		/// Overrides <see cref="ARPaymentEntry.MultiCurrency.GetChildren()"/>
		/// </summary>
		[PXOverride]
		public virtual PXSelectBase[] GetChildren(Func<PXSelectBase[]> baseMethod)
		{
			return baseMethod().Append(new PXSelectBase[]
			{
				SOAdjustments,
				SOAdjustments_Orders,
				SOAdjustments_Raw,
			});
		}

		/// <summary>
		/// Overrides <see cref="ARPaymentEntry.CalcApplAmounts(PXCache, ARPayment)" />
		/// </summary>
		[PXOverride]
		public virtual void CalcApplAmounts(PXCache sender, ARPayment row,
			Action<PXCache, ARPayment> baseMethod)
		{
			baseMethod(sender, row);

			if (row.CurySOApplAmt == null)
				RecalcSOApplAmounts(sender, row);
		}

		#endregion // Overrides

		#region Methods

		public virtual void LoadOrdersProc(bool LoadExistingOnly, LoadOptions opts)
		{
			Dictionary<string, SOAdjust> existing = new Dictionary<string, SOAdjust>();

			Base.InternalCall = true;
			try
			{
				if (Base.Document.Current == null || Base.Document.Current.CustomerID == null || Base.Document.Current.OpenDoc == false ||
					Base.Document.Current.DocType.IsNotIn(ARDocType.Payment, ARDocType.Prepayment))
				{
					throw new PXLoadInvoiceException();
				}

				foreach (PXResult<SOAdjust> res in SOAdjustments_Raw.Select())
				{
					SOAdjust old_adj = (SOAdjust)res;

					if (LoadExistingOnly == false)
					{
						old_adj = PXCache<SOAdjust>.CreateCopy(old_adj);
						old_adj.CuryAdjgAmt = null;
						old_adj.CuryAdjgDiscAmt = null;
					}

					string s = string.Format("{0}_{1}", old_adj.AdjdOrderType, old_adj.AdjdOrderNbr);
					existing.Add(s, old_adj);
					SOAdjustments.Delete(res);
				}

				Base.Document.Cache.MarkUpdated(Base.Document.Current);
				Base.Document.Cache.IsDirty = true;

				foreach (KeyValuePair<string, SOAdjust> res in existing)
				{
					SOAdjust adj = new SOAdjust();
					adj.RecordID = res.Value.RecordID;
					adj.AdjdOrderType = res.Value.AdjdOrderType;
					adj.AdjdOrderNbr = res.Value.AdjdOrderNbr;

					try
					{
						adj = PXCache<SOAdjust>.CreateCopy(AddSOAdjustment(adj) ?? adj);
						if (res.Value.CuryAdjgDiscAmt != null && res.Value.CuryAdjgDiscAmt < adj.CuryAdjgDiscAmt)
						{
							adj.CuryAdjgDiscAmt = res.Value.CuryAdjgDiscAmt;
							adj = PXCache<SOAdjust>.CreateCopy((SOAdjust)Base.Adjustments.Cache.Update(adj));
						}

						if (res.Value.CuryAdjgAmt != null && res.Value.CuryAdjgAmt < adj.CuryAdjgAmt)
						{
							adj.CuryAdjgAmt = res.Value.CuryAdjgAmt;
							Base.Adjustments.Cache.Update(adj);
						}
					}
					catch (PXSetPropertyException) { }
				}

				if (LoadExistingOnly)
				{
					return;
				}

				PXSelectBase<SOOrder> cmd = new PXSelectJoin<SOOrder,
					 InnerJoin<SOOrderType, On<SOOrderType.orderType, Equal<SOOrder.orderType>>,
					 InnerJoin<Terms, On<Terms.termsID, Equal<SOOrder.termsID>>>>,
					 Where<SOOrder.customerID, Equal<Optional<ARPayment.customerID>>,
					   And<SOOrder.openDoc, Equal<True>,
					   And<SOOrder.orderDate, LessEqual<Current<ARPayment.adjDate>>,
					   And<SOOrderType.aRDocType, In3<ARDocType.invoice, ARDocType.debitMemo>,
					   And<SOOrder.status, NotIn3<SOOrderStatus.cancelled, SOOrderStatus.pendingApproval, SOOrderStatus.voided>,
					   And<Terms.installmentType, NotEqual<TermsInstallmentType.multiple>>>>>>>,
				 OrderBy<Asc<SOOrder.orderDate,
						 Asc<SOOrder.orderNbr>>>>(Base);

				if (opts != null)
				{
					if (opts.FromDate != null)
					{
						cmd.WhereAnd<Where<SOOrder.orderDate, GreaterEqual<Current<LoadOptions.fromDate>>>>();
					}
					if (opts.TillDate != null)
					{
						cmd.WhereAnd<Where<SOOrder.orderDate, LessEqual<Current<LoadOptions.tillDate>>>>();
					}
					if (!string.IsNullOrEmpty(opts.StartOrderNbr))
					{
						cmd.WhereAnd<Where<SOOrder.orderNbr, GreaterEqual<Current<LoadOptions.startOrderNbr>>>>();
					}
					if (!string.IsNullOrEmpty(opts.EndOrderNbr))
					{
						cmd.WhereAnd<Where<SOOrder.orderNbr, LessEqual<Current<LoadOptions.endOrderNbr>>>>();
					}
				}

				PXResultset<SOOrder> custdocs = opts == null || opts.MaxDocs == null ? cmd.Select() : cmd.SelectWindowed(0, (int)opts.MaxDocs);

				custdocs.Sort(new Comparison<PXResult<SOOrder>>(delegate (PXResult<SOOrder> a, PXResult<SOOrder> b)
				{
					if (Base.arsetup.Current.FinChargeFirst == true)
					{
						int aSortOrder = (((SOOrder)a).DocType == ARDocType.FinCharge ? 0 : 1);
						int bSortOrder = (((SOOrder)b).DocType == ARDocType.FinCharge ? 0 : 1);
						int ret = ((IComparable)aSortOrder).CompareTo(bSortOrder);
						if (ret != 0) return ret;
					}

					if (opts == null)
					{
						object aOrderDate = ((SOOrder)a).OrderDate ?? DateTime.MinValue;
						object bOrderDate = ((SOOrder)b).OrderDate ?? DateTime.MinValue;
						return ((IComparable)aOrderDate).CompareTo(bOrderDate);
					}
					else
					{
						object aObj;
						object bObj;
						int ret;
						switch (opts.OrderBy)
						{
							case LoadOptions.sOOrderBy.OrderNbr:

								aObj = ((SOOrder)a).OrderNbr;
								bObj = ((SOOrder)b).OrderNbr;
								return ((IComparable)aObj).CompareTo(bObj);

							case LoadOptions.sOOrderBy.OrderDateOrderNbr:
							default:
								aObj = ((SOOrder)a).OrderDate ?? DateTime.MinValue;
								bObj = ((SOOrder)b).OrderDate ?? DateTime.MinValue;
								ret = ((IComparable)aObj).CompareTo(bObj);
								if (ret != 0) return ret;

								aObj = ((SOOrder)a).OrderNbr;
								bObj = ((SOOrder)b).OrderNbr;
								return ((IComparable)aObj).CompareTo(bObj);
						}
					}
				}));

				foreach (SOOrder invoice in custdocs)
				{
					string s = string.Format("{0}_{1}", invoice.OrderType, invoice.OrderNbr);
					if (existing.ContainsKey(s) == false)
					{
						SOAdjust adj = new SOAdjust();
						adj.AdjdOrderType = invoice.OrderType;
						adj.AdjdOrderNbr = invoice.OrderNbr;
						AddSOAdjustment(adj);
					}
				}
			}
			catch (PXLoadInvoiceException)
			{
			}
			finally
			{
				Base.InternalCall = false;
			}
		}

		protected virtual SOAdjust AddSOAdjustment(SOAdjust adj)
		{
			if (Base.Document.Current.CuryUnappliedBal == 0m && Base.Document.Current.CuryOrigDocAmt > 0m)
			{
				throw new PXLoadInvoiceException();
			}
			return SOAdjustments.Insert(adj);
		}

		public virtual void RecalcSOApplAmounts(PXCache sender, ARPayment row)
		{
			bool IsReadOnly = (sender.GetStatus(row) == PXEntryStatus.Notchanged);

			PXFormulaAttribute.CalcAggregate<SOAdjust.curyAdjgAmt>(SOAdjustments.Cache, row, IsReadOnly);
			if (row.CurySOApplAmt == null)
			{
				row.CurySOApplAmt = 0m;
			}
			sender.RaiseFieldUpdated<ARPayment.curySOApplAmt>(row, null);
		}

		protected virtual void UpdateAppliedToOrderAmount(SOOrder order, CurrencyInfo curyInfo, SOAdjust adj)
		{
			SOOrder orderCopy = PXCache<SOOrder>.CreateCopy(order);

			adj.CustomerID = Base.Document.Current.CustomerID;
			adj.AdjgDocDate = Base.Document.Current.AdjDate;
			adj.AdjgCuryInfoID = Base.Document.Current.CuryInfoID;
			adj.AdjdCuryInfoID = curyInfo.CuryInfoID;
			adj.AdjdOrigCuryInfoID = orderCopy.CuryInfoID;
			adj.AdjdOrderDate = orderCopy.OrderDate > Base.Document.Current.AdjDate
				? Base.Document.Current.AdjDate
				: orderCopy.OrderDate;
			adj.Released = false;

			SOAdjust other = PXSelectGroupBy<SOAdjust,
				Where<SOAdjust.voided, Equal<False>,
				And<SOAdjust.adjdOrderType, Equal<Required<SOAdjust.adjdOrderType>>,
				And<SOAdjust.adjdOrderNbr, Equal<Required<SOAdjust.adjdOrderNbr>>,
				And<Where<SOAdjust.adjgDocType, NotEqual<Required<SOAdjust.adjgDocType>>, Or<SOAdjust.adjgRefNbr, NotEqual<Required<SOAdjust.adjgRefNbr>>>>>>>>,
				Aggregate<GroupBy<SOAdjust.adjdOrderType,
				GroupBy<SOAdjust.adjdOrderNbr, Sum<SOAdjust.curyAdjdAmt, Sum<SOAdjust.adjAmt>>>>>>
				.Select(Base, adj.AdjdOrderType, adj.AdjdOrderNbr, adj.AdjgDocType, adj.AdjgRefNbr);
			if (other != null && other.AdjdOrderNbr != null)
			{
				orderCopy.CuryDocBal -= other.CuryAdjdAmt;
				orderCopy.DocBal -= other.AdjAmt;
			}

			SOBalanceCalculator.CalcBalances(adj, orderCopy, false, true);

			decimal? CuryApplAmt = adj.CuryDocBal - adj.CuryDiscBal;
			decimal? CuryApplDiscAmt = adj.CuryDiscBal;
			decimal? CuryUnappliedBal = Base.Document.Current.CuryUnappliedBal;

			if (adj.CuryDiscBal >= 0m && adj.CuryDocBal - adj.CuryDiscBal <= 0m)
			{
				//no amount suggestion is possible
				return;
			}

			if (Base.Document.Current != null && string.IsNullOrEmpty(Base.Document.Current.DocDesc))
			{
				Base.Document.Current.DocDesc = orderCopy.OrderDesc;
			}
			if (Base.Document.Current != null && CuryUnappliedBal > 0m)
			{
				CuryApplAmt = Math.Min((decimal)CuryApplAmt, (decimal)CuryUnappliedBal);

				if (CuryApplAmt + CuryApplDiscAmt < adj.CuryDocBal)
				{
					CuryApplDiscAmt = 0m;
				}
			}
			else if (Base.Document.Current != null && CuryUnappliedBal <= 0m && ((ARPayment)Base.Document.Current).CuryOrigDocAmt > 0)
			{
				CuryApplAmt = 0m;
				CuryApplDiscAmt = 0m;
			}

			var actual = (SOAdjust)SOAdjustments.Cache.Locate(adj);
			if (actual == null)
			{
				actual = SOAdjust.PK.Find(Base, adj);
			}
			else if (SOAdjustments.Cache.GetStatus(actual).IsIn(PXEntryStatus.Deleted, PXEntryStatus.InsertedDeleted))
			{
				actual = null;
			}
			SOAdjustments.Cache.SetValue<SOAdjust.curyAdjgAmt>(adj, actual?.CuryAdjgAmt ?? 0m);
			SOAdjustments.Cache.SetValue<SOAdjust.curyAdjgDiscAmt>(adj, actual?.CuryAdjgDiscAmt ?? 0m);
			SOAdjustments.Cache.SetValueExt<SOAdjust.curyAdjgAmt>(adj, CuryApplAmt);
			SOAdjustments.Cache.SetValueExt<SOAdjust.curyAdjgDiscAmt>(adj, CuryApplDiscAmt);

			SOBalanceCalculator.CalcBalances(adj, orderCopy, true, true);
		}

		public virtual bool IsApplicationToBlanketOrderWithChild(SOAdjust adjustment)
		{
			return adjustment != null && SOOrderType.PK.Find(Base, adjustment.AdjdOrderType)?.Behavior == SOBehavior.BL &&
						(adjustment.AdjTransferredToChildrenAmt > 0 ||
						PXParentAttribute.SelectParent<SOOrder>(SOAdjustments.Cache, adjustment)?.ChildLineCntr != 0);
		}

		#endregion
	}
}
