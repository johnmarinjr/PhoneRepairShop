using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.Common.Discount;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.IN.GraphExtensions.SOOrderEntryExt;
using PX.Objects.SO.DAC.Unbound;
using PX.Objects.SO.DAC.Projections;
using PX.Objects.TX;
using PX.Objects.AR;
using PX.Objects.CM;

namespace PX.Objects.SO.GraphExtensions.SOOrderEntryExt
{
	public class AffectedBlanketOrderByChildOrders : AffectedBlanketOrderByChildOrders<AffectedBlanketOrderByChildOrders, SOOrderEntry>
	{
		public static bool IsActive()
			=> PXAccess.FeatureInstalled<FeaturesSet.distributionModule>();
	}

	public class Blanket : PXGraphExtension<SOOrderEntry>
	{
		public class CreateChildrenResult
		{
			public List<SOOrder> Created { get; set; } = new List<SOOrder>();
			public Exception LastError { get; set; }
			public int ErrorCount { get; set; }
		}

		public class CreateChildParameter
		{
			public SOOrder BlanketOrder { get; set; }
			public SOOrderType BlanketOrderType { get; set; }
			public CurrencyInfo BlanketCurrency { get; set; }

			public DateTime? SchedOrderDate { get; set; }
			public string CustomerOrderNbr { get; set; }
			public int? CustomerLocationID { get; set; }
			public string TaxZoneID { get; set; }
			public string ShipVia { get; set; }
			public string FOBPoint { get; set; }
			public string ShipTermsID { get; set; }
			public string ShipZoneID { get; set; }

			public IEnumerable<PXResult<BlanketSOLineSplit, BlanketSOLine>> Lines { get; set; }
		}

		public static bool IsActive()
			=> PXAccess.FeatureInstalled<FeaturesSet.distributionModule>();

		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXDefault(typeof(IIf<Where<SOLine.behavior, Equal<SOBehavior.bL>>, True, False>))]
		protected virtual void _(Events.CacheAttached<SOLine.automaticDiscountsDisabled> e)
		{
		}

		public SelectFrom<SOBlanketOrderLink>
				.Where<SOBlanketOrderLink.blanketType.IsEqual<SOOrder.orderType.FromCurrent>
					.And<SOBlanketOrderLink.blanketNbr.IsEqual<SOOrder.orderNbr.FromCurrent>>>
				.View BlanketOrderChildrenList;

		public SelectFrom<SOBlanketOrderDisplayLink>
				.Where<SOBlanketOrderDisplayLink.blanketType.IsEqual<SOOrder.orderType.FromCurrent>
					.And<SOBlanketOrderDisplayLink.blanketNbr.IsEqual<SOOrder.orderNbr.FromCurrent>>>
				.View.ReadOnly BlanketOrderChildrenDisplayList;

		public SelectFrom<BlanketSOAdjust>
				.Where<BlanketSOAdjust.adjdOrderType.IsEqual<@P.AsString.ASCII>.And<BlanketSOAdjust.adjdOrderNbr.IsEqual<@P.AsString>>
					.And<BlanketSOAdjust.curyAdjdAmt.IsGreater<decimal0>>.And<BlanketSOAdjust.voided.IsEqual<False>>>
				.View BlanketAdjustments;

		[PXCopyPasteHiddenView]
		[PXVirtualDAC]
		public PXSelect<OpenBlanketSOLineSplit> BlanketSplits;
		public PXFilter<BlanketSOOverrideTaxZoneFilter> BlanketTaxZoneOverrideFilter;

		protected virtual IEnumerable blanketOrderChildrenDisplayList()
		{
			var internalView = new PXView(Base, true, PXView.View.BqlSelect);

			var miscOrderSelect = new SelectFrom<SOBlanketOrderMiscLink>
				.Where<SOBlanketOrderMiscLink.blanketType.IsEqual<SOOrder.orderType.FromCurrent>
					.And<SOBlanketOrderMiscLink.blanketNbr.IsEqual<SOOrder.orderNbr.FromCurrent>>>
				.View.ReadOnly(Base);

			int maximumRows = 0;
			int startRow = 0;
			int totalRows = 0;

			var orders = internalView.Select(new object[] { Base.Document.Current },
				PXView.Parameters, PXView.Searches, PXView.SortColumns, PXView.Descendings,
				PXView.Filters, ref startRow, maximumRows, ref totalRows).RowCast<SOBlanketOrderDisplayLink>().ToList();

			startRow = 0;
			totalRows = 0;

			var miscOrders = miscOrderSelect.View.Select(new object[] { Base.Document.Current },
				PXView.Parameters, PXView.Searches, PXView.SortColumns, PXView.Descendings,
				PXView.Filters, ref startRow, maximumRows, ref totalRows).RowCast<SOBlanketOrderDisplayLink>().ToList();

			var result = new PXDelegateResult { IsResultFiltered = true };

			result.AddRange(
				orders.Where(o => o.ShipmentNbr != null ||
					!miscOrders.Any(mo => mo.OrderType == o.OrderType && mo.OrderNbr == o.OrderNbr)));

			result.AddRange(miscOrders);

			return result;
		}

		public PXAction<SOOrder> createChildOrders;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Create Child Orders", MapViewRights = PXCacheRights.Select)]
		protected virtual IEnumerable CreateChildOrders(PXAdapter adapter,
			[PXDate] DateTime? schedOrderDate)
		{
			List<SOOrder> list = adapter.Get<SOOrder>().ToList();
			Base.Save.Press();

			PXLongOperation.StartOperation(Base, delegate ()
			{
				var graph = PXGraph.CreateInstance<SOOrderEntry>();
				var ext = graph.GetExtension<Blanket>();
				schedOrderDate ??= graph.Accessinfo.BusinessDate;
				var result = new CreateChildrenResult();

				foreach (SOOrder blanket in list)
				{
					if (blanket.ExpireDate < schedOrderDate)
					{
						throw new PXException(Messages.OrderExpiredChangeDate);
					}
					ext.CreateChildren(blanket, schedOrderDate, result);
				}

				if (result.ErrorCount > 1 || (result.ErrorCount == 1 && result.Created.Count > 0))
				{
					throw new PXException(Messages.SomeChildOrdersHaveNotBeenCreated);
				}
				else if (result.ErrorCount == 1 && result.Created.Count == 0)
				{
					throw result.LastError;
				}
				else if (result.Created.Count == 0)
				{
					throw new PXException(Messages.NoLinesForCreatingChild);
				}
				else if (adapter.MassProcess == false)
				{
					if (result.Created.Count == 1)
					{
						using (new PXTimeStampScope(null))
						{
							graph.Clear();
							graph.Document.Current = graph.Document.Search<SOOrder.orderNbr>(result.Created[0].OrderNbr, result.Created[0].OrderType);
							throw new PXRedirectRequiredException(graph, string.Empty);
						}
					}
					else
					{
						throw new PXOperationCompletedException(Messages.FollowingChildrenCreated,
							string.Join(", ", result.Created.Select(o => o.OrderNbr)));
					}
				}
			});

			return list;
		}

		public PXAction<SOOrder> processExpiredOrder;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Process Expired Order", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Select)]
		public virtual IEnumerable ProcessExpiredOrder(PXAdapter adapter) => adapter.Get();

		public PXAction<SOOrder> viewChildOrder;
		[PXUIField(DisplayName = "View Child Order", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXLookupButton(DisplayOnMainToolbar = false)]
		public virtual IEnumerable ViewChildOrder(PXAdapter adapter)
		{
			if (BlanketOrderChildrenDisplayList.Current != null)
			{
				SOOrderEntry graph = PXGraph.CreateInstance<SOOrderEntry>();
				graph.Document.Current = graph.Document.Search<SOOrder.orderNbr>(BlanketOrderChildrenDisplayList.Current.OrderNbr, BlanketOrderChildrenDisplayList.Current.OrderType);
				throw new PXRedirectRequiredException(graph, true, "View Child Order") { Mode = PXBaseRedirectException.WindowMode.NewWindow };
			}
			return adapter.Get();
		}

		public PXAction<SOOrder> printBlanket;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Print Blanket Sales Order", MapEnableRights = PXCacheRights.Select)]
		public virtual IEnumerable PrintBlanket(PXAdapter adapter, string reportID = null) => Base.Report(adapter.Apply(it => it.Menu = "Print Blanket Sales Order"), reportID ?? "SO641040");

		public PXAction<SOOrder> emailBlanket;
		[PXButton(CommitChanges = true), PXUIField(DisplayName = "Email Blanket Sales Order", MapEnableRights = PXCacheRights.Select)]
		public virtual IEnumerable EmailBlanket(
			PXAdapter adapter,
			[PXString]
			string notificationCD = null) => Base.Notification(adapter, notificationCD ?? "BLANKET SO");

		public PXAction<SOOrder> addBlanketLine;
		[PXUIField(DisplayName = "Add Blanket SO Line", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Select, Visible = true)]
		[PXLookupButton()]
		public virtual IEnumerable AddBlanketLine(PXAdapter adapter)
		{
			if (Base.Document.Current != null && BlanketSplits.AskExt() == WebDialogResult.OK)
			{
				bool hasLines = Base.Transactions.Select().Any();

				foreach (OpenBlanketSOLineSplit res in BlanketSplits.Cache.Cached.RowCast<OpenBlanketSOLineSplit>().Where(res => res.Selected == true))
				{
					var openBlanketSplits = SelectFrom<BlanketSOLineSplit>
						.InnerJoin<BlanketSOLine>.On<BlanketSOLineSplit.FK.BlanketOrderLine>
						.LeftJoin<SOBlanketOrderLink>.On<SOBlanketOrderLink.blanketType.IsEqual<BlanketSOLineSplit.orderType>
							.And<SOBlanketOrderLink.blanketType.IsEqual<BlanketSOLineSplit.orderNbr>>
							.And<SOBlanketOrderLink.orderType.IsEqual<SOOrder.orderType.FromCurrent>>
							.And<SOBlanketOrderLink.orderNbr.IsEqual<SOOrder.orderNbr.FromCurrent>>>
						.Where<BlanketSOLineSplit.orderType.IsEqual<OpenBlanketSOLineSplit.orderType.FromCurrent>
							.And<BlanketSOLineSplit.orderNbr.IsEqual<OpenBlanketSOLineSplit.orderNbr.FromCurrent>>
							.And<BlanketSOLineSplit.lineNbr.IsEqual<OpenBlanketSOLineSplit.lineNbr.FromCurrent>>
							.And<BlanketSOLineSplit.splitLineNbr.IsEqual<OpenBlanketSOLineSplit.splitLineNbr.FromCurrent>>>
						.View.SelectMultiBound(Base, new object[] { Base.Document.Current, res })
						.AsEnumerable()
						.Cast<PXResult<BlanketSOLineSplit, BlanketSOLine, SOBlanketOrderLink>>().ToList();

					foreach (PXResult<BlanketSOLineSplit, BlanketSOLine, SOBlanketOrderLink> openBlanketSplit in openBlanketSplits)
					{
						BlanketSOLine blanketLine = (BlanketSOLine)openBlanketSplit;
						BlanketSOLineSplit blanketSplit = (BlanketSOLineSplit)openBlanketSplit;
						SOBlanketOrderLink existingLink = (SOBlanketOrderLink)openBlanketSplit;
						if (existingLink.BlanketNbr == null
							|| BlanketOrderChildrenList.Cache.GetStatus(existingLink).IsIn(PXEntryStatus.Deleted, PXEntryStatus.InsertedDeleted))
						{
							CreateChildToBlanketLink(blanketLine.OrderType, blanketLine.OrderNbr, Base.Document.Current);
						}

						if (!hasLines)
						{
							SOOrder order = PXCache<SOOrder>.CreateCopy(Base.Document.Current);
							order.CustomerOrderNbr = blanketSplit.CustomerOrderNbr;
							if (order.TaxZoneID != blanketLine.TaxZoneID)
							{
								order.OverrideTaxZone = true;
								order.TaxZoneID = blanketLine.TaxZoneID;
							}
							Base.Document.Update(order);
						}

						//all lines with different CustomerOrderNbr/TaxZoneID will be silently skipped 
						if (blanketSplit.CustomerOrderNbr == Base.Document.Current.CustomerOrderNbr && blanketLine.TaxZoneID == Base.Document.Current.TaxZoneID)
						{
							SOLine line = AddChildLine(blanketSplit, blanketLine);
							hasLines = true;
						}
					}
				}
				BlanketSplits.Cache.Clear();
				BlanketSplits.View.Clear();
			}

			return adapter.Get();
		}

		public PXAction<SOOrder> addBlanketLineOK;
		[PXUIField(DisplayName = "", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select, Visible = false)]
		[PXLookupButton()]
		public virtual IEnumerable AddBlanketLineOK(PXAdapter adapter)
		{
			BlanketSplits.View.Answer = WebDialogResult.OK;
			return AddBlanketLine(adapter);
		}

		public virtual IEnumerable blanketSplits()
		{
			List<OpenBlanketSOLineSplit> list = new List<OpenBlanketSOLineSplit>();
			if (Base.Document.Current?.Behavior != SOBehavior.SO)
				return list;

			bool hasLines = Base.Transactions.Select().Any();

			var openBlanketSplits = SelectFrom<BlanketSOLineSplit>.
				InnerJoin<BlanketSOLine>.On<BlanketSOLineSplit.FK.BlanketOrderLine>.
				InnerJoin<BlanketSOOrder>.On<BlanketSOLineSplit.FK.BlanketOrder>.
				Where<BlanketSOLineSplit.completed.IsEqual<False>.
					And<BlanketSOLine.customerID.IsEqual<SOOrder.customerID.FromCurrent>>.
					And<BlanketSOLine.customerLocationID.IsEqual<SOOrder.customerLocationID.FromCurrent>>.
					And<BlanketSOLineSplit.schedOrderDate.IsLessEqual<SOOrder.orderDate.FromCurrent>>.
					And<BlanketSOLineSplit.qty.IsGreater<BlanketSOLineSplit.qtyOnOrders.Add<BlanketSOLineSplit.receivedQty>>>.
					And<BlanketSOOrder.curyID.IsEqual<SOOrder.curyID.FromCurrent>>.
					And<BlanketSOOrder.taxCalcMode.IsEqual<SOOrder.taxCalcMode.FromCurrent>>.
					And<BlanketSOOrder.isExpired.IsEqual<False>>.
					And<BlanketSOOrder.hold.IsEqual<False>>.
					And<BlanketSOLine.taxZoneID.IsEqual<SOOrder.taxZoneID.FromCurrent>.
						Or<BlanketSOLine.taxZoneID.IsNull.And<SOOrder.taxZoneID.FromCurrent.IsNull>>.
						Or<True.IsEqual<@P.AsBool>>>.
					And<BlanketSOLineSplit.customerOrderNbr.IsEqual<SOOrder.customerOrderNbr.FromCurrent>.
						Or<BlanketSOLineSplit.customerOrderNbr.IsNull.And<SOOrder.customerOrderNbr.FromCurrent.IsNull>>.
						Or<True.IsEqual<@P.AsBool>>>>.View.SelectMultiBound(Base, new object[] { Base.Document.Current }, !hasLines, !hasLines).ToList();

			foreach (PXResult<BlanketSOLineSplit, BlanketSOLine> openBlanketLine in openBlanketSplits)
			{
				var split = (BlanketSOLineSplit)openBlanketLine;
				var line = (BlanketSOLine)openBlanketLine;
				if (IsExpectingReturnAllocationsToBlanket(split)) continue;

				var openBlanketSplit = new OpenBlanketSOLineSplit
				{
					OrderType = split.OrderType,
					OrderNbr = split.OrderNbr,
					LineNbr = split.LineNbr,
					SplitLineNbr = split.SplitLineNbr,
					InventoryID = split.InventoryID,
					SubItemID = split.SubItemID,
					TranDesc = line.TranDesc,
					SiteID = split.SiteID,
					CustomerID = line.CustomerID,
					CustomerLocationID = line.CustomerLocationID,
					CustomerOrderNbr = split.CustomerOrderNbr,
					SchedOrderDate = split.SchedOrderDate,
					UOM = line.UOM,
					BlanketOpenQty = split.BlanketOpenQty,
					TaxZoneID = line.TaxZoneID,
				};
				var cachedOpenBlanketSplit = (OpenBlanketSOLineSplit)BlanketSplits.Cache.Locate(openBlanketSplit);
				if (cachedOpenBlanketSplit != null)
				{
					list.Add(cachedOpenBlanketSplit);
				}
				else
				{
					BlanketSplits.Cache.SetStatus(openBlanketSplit, PXEntryStatus.Held);
					list.Add(openBlanketSplit);
				}
			}

			return list;
		}

		protected virtual void CreateChildren(SOOrder blanket, DateTime? schedOrderDate, CreateChildrenResult result)
		{
			if (blanket.Behavior != SOBehavior.BL) throw new InvalidOperationException();

			var blanketSplits = SelectFrom<BlanketSOLineSplit>
				.InnerJoin<BlanketSOLine>.On<BlanketSOLineSplit.FK.BlanketOrderLine>
				.Where<BlanketSOLineSplit.FK.Order.SameAsCurrent
					.And<BlanketSOLineSplit.completed.IsEqual<False>>
					.And<BlanketSOLineSplit.qty.IsGreater<BlanketSOLineSplit.qtyOnOrders.Add<BlanketSOLineSplit.receivedQty>>>
					.And<BlanketSOLineSplit.schedOrderDate.IsLessEqual<@P.AsDateTime>>>
				.View.ReadOnly.SelectMultiBound(Base, new[] { blanket }, schedOrderDate)
				.AsEnumerable()
				.Cast<PXResult<BlanketSOLineSplit, BlanketSOLine>>().ToList();
			if (!blanketSplits.Any()) return;

			var orderType = SOOrderType.PK.Find(Base, blanket.OrderType);

			CurrencyInfo blanketCurrency = Base.currencyinfo.View.SelectSingleBound(new object[] { blanket }) as CurrencyInfo;

			foreach (var group in blanketSplits
				.GroupBy(r => new
				{
					((BlanketSOLineSplit)r).SchedOrderDate,
					((BlanketSOLineSplit)r).CustomerOrderNbr,
					((BlanketSOLine)r).CustomerLocationID,
					((BlanketSOLine)r).TaxZoneID,
					((BlanketSOLine)r).ShipVia,
					((BlanketSOLine)r).FOBPoint,
					((BlanketSOLine)r).ShipTermsID,
					((BlanketSOLine)r).ShipZoneID
				})
				.OrderBy(g => g.Key.SchedOrderDate)
				.ThenBy(g => g.Key.CustomerOrderNbr)
				.ThenBy(g => g.Key.CustomerLocationID)
				.ThenBy(g => g.Key.TaxZoneID))
			{
				try
				{
					var order = CreateChild(new CreateChildParameter()
					{
						BlanketOrder = blanket,
						BlanketOrderType = orderType,
						BlanketCurrency = blanketCurrency,
						SchedOrderDate = group.Key.SchedOrderDate,
						CustomerOrderNbr = group.Key.CustomerOrderNbr,
						CustomerLocationID = group.Key.CustomerLocationID,
						TaxZoneID = group.Key.TaxZoneID,
						ShipVia = group.Key.ShipVia,
						FOBPoint = group.Key.FOBPoint,
						ShipTermsID = group.Key.ShipTermsID,
						ShipZoneID = group.Key.ShipZoneID,
						Lines = group
					});

					result.Created.Add(order);
				}
				catch(Exception exception)
				{
					BlanketSOLineSplit firstErrorSplit = group.First();

					var processingEntityException = Common.Exceptions.ErrorProcessingEntityException.Create(
						Base.Caches[typeof(BlanketSOLineSplit)], firstErrorSplit, exception);

					PXTrace.WriteError(processingEntityException);
					result.LastError = exception;
					result.ErrorCount++;
				}

				Base.Clear();
			}
		}

		protected virtual SOOrder CreateChild(CreateChildParameter parameter)
		{
			var doc = new SOOrder
			{
				OrderType = parameter.BlanketOrderType.DfltChildOrderType,
				BranchID = parameter.BlanketOrder.BranchID,
			};
			doc = PXCache<SOOrder>.CreateCopy(Base.Document.Insert(doc));
			doc.OrderDate = parameter.SchedOrderDate;
			doc.CustomerID = parameter.BlanketOrder.CustomerID;
			doc.CustomerLocationID = parameter.CustomerLocationID;
			doc = PXCache<SOOrder>.CreateCopy(Base.Document.Update(doc));

			Base.ReloadCustomerCreditRule();

			doc.BranchID = parameter.BlanketOrder.BranchID;
			doc.ProjectID = parameter.BlanketOrder.ProjectID;
			doc.TaxCalcMode = parameter.BlanketOrder.TaxCalcMode;
			doc.TermsID = parameter.BlanketOrder.TermsID;
			doc.PaymentMethodID = parameter.BlanketOrder.PaymentMethodID;
			doc.PMInstanceID = parameter.BlanketOrder.PMInstanceID;
			doc.CustomerOrderNbr = parameter.CustomerOrderNbr;
			if (parameter.TaxZoneID != GetDefaultLocationTaxZone(doc.CustomerID, doc.CustomerLocationID, doc.BranchID))
				doc.OverrideTaxZone = true;
			doc.TaxZoneID = parameter.TaxZoneID;
			doc.ShipVia = parameter.ShipVia;
			doc.FOBPoint = parameter.FOBPoint;
			doc.ShipTermsID = parameter.ShipTermsID;
			doc.ShipZoneID = parameter.ShipZoneID;
			doc = PXCache<SOOrder>.CreateCopy(Base.Document.Update(doc));
			doc.CuryID = parameter.BlanketOrder.CuryID;
			doc = Base.Document.Update(doc);

			if (parameter.BlanketOrderType.UseCuryRateFromBL == true &&
				parameter.BlanketCurrency.BaseCuryID != parameter.BlanketCurrency.CuryID)
			{
				CurrencyInfo childOrderCurrency = Base.currencyinfo.Select();
				PXCache<CurrencyInfo>.RestoreCopy(childOrderCurrency, parameter.BlanketCurrency);
				childOrderCurrency.CuryInfoID = doc.CuryInfoID;
				Base.currencyinfo.Update(childOrderCurrency);
			}

			CreateChildToBlanketLink(parameter.BlanketOrder, doc);

			using (Base.FindImplementation<AffectedBlanketOrderByChildOrders>().SuppressedModeScope(parameter.BlanketOrder))
			{
				foreach (PXResult<BlanketSOLineSplit, BlanketSOLine> r in parameter.Lines)
				{
					SOLine line = AddChildLine(r, r);
					PXNoteAttribute.CopyNoteAndFiles(Base.Caches[typeof(BlanketSOLine)], (BlanketSOLine)r, Base.Caches[typeof(SOLine)], line,
						parameter.BlanketOrderType.CopyLineNotesToChildOrder, parameter.BlanketOrderType.CopyLineFilesToChildOrder);
				}
			}

			doc = PXCache<SOOrder>.CreateCopy(Base.Document.Locate(doc));
			doc.CuryControlTotal = doc.CuryOrderTotal;
			doc = Base.Document.Update(doc);

			using (var ts = new PXTransactionScope())
			{
				TransferPayments(parameter.BlanketOrder, doc);
				Base.Save.Press();
				ts.Complete();
			}

			return Base.Document.Current;
		}

		private void CreateChildToBlanketLink(SOOrder blanket, SOOrder doc)
		{
			CreateChildToBlanketLink(blanket.OrderType, blanket.OrderNbr, doc);
		}
		private void CreateChildToBlanketLink(string blanketOrderType, string blanketOrderNbr, SOOrder doc)
		{
			SOBlanketOrderLink blanketLink = new SOBlanketOrderLink
			{
				BlanketType = blanketOrderType,
				BlanketNbr = blanketOrderNbr,
				OrderType = doc.OrderType,
				OrderNbr = doc.OrderNbr,
				CuryInfoID = doc.CuryInfoID
			};
			var located = BlanketOrderChildrenList.Locate(blanketLink) ?? SOBlanketOrderLink.PK.Find(Base, blanketLink);
			if (located == null
				|| BlanketOrderChildrenList.Cache.GetStatus(located).IsIn(PXEntryStatus.Deleted, PXEntryStatus.InsertedDeleted))
			{
				PXParentAttribute.SetParent(BlanketOrderChildrenList.Cache, blanketLink, typeof(SOOrder), doc);
				BlanketOrderChildrenList.Insert(blanketLink);
			}
		}

		protected virtual SOLine AddChildLine(BlanketSOLineSplit blanketSplit, BlanketSOLine blanketLine)
		{
			var soLine = new SOLine
			{
				BranchID = blanketLine.BranchID,
			};
			soLine = PXCache<SOLine>.CreateCopy(Base.Transactions.Insert(soLine));
			soLine.InventoryID = blanketSplit.InventoryID;
			soLine.SubItemID = blanketSplit.SubItemID;
			soLine.SiteID = blanketSplit.SiteID;
			soLine.TaxCategoryID = blanketLine.TaxCategoryID;
			soLine.TaskID = blanketLine.TaskID;
			soLine.CostCodeID = blanketLine.CostCodeID;
			soLine.AutomaticDiscountsDisabled = true;
			if (blanketSplit.SchedShipDate != null)
				soLine.ShipDate = blanketSplit.SchedShipDate;
			soLine.ShipComplete = blanketLine.ShipComplete;
			soLine = PXCache<SOLine>.CreateCopy(Base.Transactions.Update(soLine));
			soLine.UOM = blanketLine.UOM;
			soLine.ManualPrice = true;
			soLine.LineType = blanketSplit.LineType;
			soLine.SalesPersonID = blanketLine.SalesPersonID;
			soLine = PXCache<SOLine>.CreateCopy(Base.Transactions.Update(soLine));
			decimal? childLineQty = blanketSplit.Qty - blanketSplit.QtyOnOrders - blanketSplit.ReceivedQty;
			soLine.OrderQty = childLineQty;
			soLine.CuryUnitPrice = blanketLine.CuryUnitPrice;
			soLine = PXCache<SOLine>.CreateCopy(Base.Transactions.Update(soLine));
			soLine.CuryExtPrice = childLineQty * blanketLine.CuryExtPrice / blanketLine.OrderQty;
			soLine.BlanketType = blanketSplit.OrderType;
			soLine.BlanketNbr = blanketSplit.OrderNbr;
			soLine.BlanketLineNbr = blanketSplit.LineNbr;
			soLine.BlanketSplitLineNbr = blanketSplit.SplitLineNbr;
			soLine = PXCache<SOLine>.CreateCopy(Base.Transactions.Update(soLine));
			soLine.DiscountID = blanketLine.DiscountID;
			soLine.DiscPct = blanketLine.DiscPct;
			if (childLineQty == blanketLine.OrderQty)
			{
				soLine = PXCache<SOLine>.CreateCopy(Base.Transactions.Update(soLine));
				soLine.CuryDiscAmt = blanketLine.CuryDiscAmt;
			}
			soLine = Base.Transactions.Update(soLine);

			if (blanketSplit.POCreate == true)
			{
				soLine = PXCache<SOLine>.CreateCopy(soLine);
				soLine.POCreate = true;
				soLine.POSource = blanketLine.POSource;
				soLine.VendorID = blanketLine.VendorID;
				soLine = Base.Transactions.Update(soLine);
				if (blanketLine.POCreated == true)
				{
					foreach (SOLineSplit split in Base.splits.Cache.Inserted.Cast<SOLineSplit>()
						.Where(split => split.LineNbr == soLine.LineNbr))
					{
						split.POType = blanketSplit.POType;
						split.PONbr = blanketSplit.PONbr;
						split.POLineNbr = blanketSplit.POLineNbr;
						split.RefNoteID = blanketSplit.RefNoteID;
						split.Completed = blanketSplit.Completed;
						split.POCompleted = blanketSplit.POCompleted;
						split.POCancelled = blanketSplit.POCancelled;

						Base.splits.Update(split);
						var plan = (INItemPlan)Base.Caches<INItemPlan>().Locate(new INItemPlan { PlanID = split.PlanID });
						var blanketPlan = INItemPlan.PK.Find(Base, blanketSplit.PlanID);
						if (plan != null && blanketPlan != null)
						{
							plan.SupplyPlanID = blanketPlan.SupplyPlanID;
							Base.Caches<INItemPlan>().MarkUpdated(plan);
						}
						break;
					}
				}
			}
			else if (blanketSplit.IsAllocated == true || !string.IsNullOrEmpty(blanketSplit.POReceiptNbr))
			{
				foreach (SOLineSplit split in Base.splits.Cache.Inserted.Cast<SOLineSplit>()
					.Where(split => split.LineNbr == soLine.LineNbr))
				{
					split.IsAllocated = blanketSplit.IsAllocated;
					if (!string.IsNullOrEmpty(blanketSplit.LotSerialNbr))
						split.LotSerialNbr = blanketSplit.LotSerialNbr;
					split.POReceiptType = blanketSplit.POReceiptType;
					split.POReceiptNbr = blanketSplit.POReceiptNbr;
					Base.splits.Update(split);
				}
			}

			//set this flag in the end to override any changes made by ManualDiscMode attribute
			soLine.ManualDisc = blanketLine.ManualDisc;

			return soLine;
		}

		protected virtual void TransferPayments(SOOrder blanketOrder, SOOrder destinationOrder)
		{
			if (Base.IsExternalTax(blanketOrder.TaxZoneID))
			{
				Base.Save.Press();

				//clear caches before save in second time
				Base.Document.Cache.ClearQueryCache();
				Base.Document.Cache.Clear();

				destinationOrder = Base.Document.Current =
					Base.Document.Search<SOOrder.orderNbr>(destinationOrder.OrderNbr, destinationOrder.OrderType);
			}

			decimal curyUnpaidBalance = destinationOrder.CuryUnpaidBalance ?? 0m;

			foreach (BlanketSOAdjust blanketAdjustment in BlanketAdjustments.Select(blanketOrder.OrderType, blanketOrder.OrderNbr))
			{
				decimal amount = Math.Min(blanketAdjustment.CuryAdjdAmt ?? 0m, curyUnpaidBalance);
				if (amount > 0m)
					TransferPayment(blanketOrder, blanketAdjustment, destinationOrder, amount);

				curyUnpaidBalance -= amount;
				if (curyUnpaidBalance <= 0)
					break;
			}
		}

		protected virtual void TransferPayment(SOOrder blanketOrder, BlanketSOAdjust blanketAdjustment, SOOrder destinationOrder, decimal amount)
		{
			var newAdjust = new SOAdjust()
			{
				AdjgDocType = blanketAdjustment.AdjgDocType,
				AdjgRefNbr = blanketAdjustment.AdjgRefNbr,
				BlanketRecordID = blanketAdjustment.RecordID,
				BlanketType = blanketAdjustment.AdjdOrderType,
				BlanketNbr = blanketAdjustment.AdjdOrderNbr,
				AdjdOrderType = destinationOrder.OrderType,
				AdjdOrderNbr = destinationOrder.OrderNbr,
				CuryAdjdAmt = 0m
			};

			var payment = ARPayment.PK.Find(Base, newAdjust.AdjgDocType, newAdjust.AdjgRefNbr);
			if (payment == null)
			{
				throw new Common.Exceptions.RowNotFoundException(
					Base.Adjustments.Cache, newAdjust.AdjgDocType, newAdjust.AdjgRefNbr);
			}

			Base.CalculateApplicationBalance(Base.currencyinfo.Current, payment, newAdjust);

			newAdjust = (SOAdjust)Base.Adjustments.Cache.CreateCopy(Base.Adjustments.Insert(newAdjust));
			newAdjust.CuryAdjdAmt = amount;
			newAdjust = Base.Adjustments.Update(newAdjust);
		}

		/// <summary>
		/// Overrides <see cref="SOOrderEntry.CalculatePaymentBalance"/>
		/// </summary>
		[PXOverride]
		public virtual void CalculatePaymentBalance(ARPayment payment, SOAdjust adj,
			Action<ARPayment, SOAdjust> baseMethod)
		{
			baseMethod(payment, adj);

			if (adj.BlanketNbr != null)
			{
				var parent = BlanketSOAdjust.PK.Find(Base,
						(int)adj.BlanketRecordID, adj.BlanketType, adj.BlanketNbr, adj.AdjgDocType, adj.AdjgRefNbr);

				payment.CuryDocBal += parent?.CuryAdjgAmt ?? 0m;
				payment.DocBal += parent?.AdjAmt ?? 0m;
			}
		}

		/// <summary>
		/// Overrides <see cref="SOOrderEntry.GetDefaultSODiscountCalculationOptions"/>
		/// </summary>
		[PXOverride]
		public virtual DiscountEngine.DiscountCalculationOptions GetDefaultSODiscountCalculationOptions(SOOrder doc, Func<SOOrder, DiscountEngine.DiscountCalculationOptions> baseFunc)
		{
			DiscountEngine.DiscountCalculationOptions calculationOptions = baseFunc(doc);
			if (doc.Behavior == SOBehavior.BL)
			{
				calculationOptions = calculationOptions | DiscountEngine.DiscountCalculationOptions.ExplicitlyAllowToCalculateAutomaticLineDiscounts;
			}
			return calculationOptions;
		}

		/// <summary>
		/// Overrides <see cref="SOOrderEntry.IsCurrencyEnabled"/>
		/// </summary>
		[PXOverride]
		public virtual bool IsCurrencyEnabled(SOOrder order, Func<SOOrder, bool> baseMethod)
		{
			return baseMethod(order) && order.ChildLineCntr == 0 && order.BlanketLineCntr == 0;
		}

		/// <summary>
		/// Overrides <see cref="SOOrderEntry.InvoiceOrders(List{SOOrder}, Dictionary{string, object}, bool, PXQuickProcess.ActionFlow)"/>
		/// </summary>
		[PXOverride]
		public virtual void InvoiceOrders(List<SOOrder> list, Dictionary<string, object> arguments,
			bool massProcess, PXQuickProcess.ActionFlow quickProcessFlow,
			Action<List<SOOrder>, Dictionary<string, object>, bool, PXQuickProcess.ActionFlow> baseMethod)
		{
			if (massProcess || list.Count != 1 || list.First().Behavior != SOBehavior.BL)
			{
				baseMethod(list, arguments, massProcess, quickProcessFlow);
				return;
			}

			var order = list.First();

			var orderShipments = SelectFrom<SOOrderShipment>
				.InnerJoin<SOOrder>.On<SOOrderShipment.FK.Order>
				.InnerJoin<CurrencyInfo>.On<SOOrder.FK.CurrencyInfo>
				.InnerJoin<SOAddress>.On<SOAddress.addressID.IsEqual<SOOrder.billAddressID>>
				.InnerJoin<SOContact>.On<SOContact.contactID.IsEqual<SOOrder.billContactID>>
				.Where<Exists<
					Select<SOShipLine,
					Where<SOShipLine.FK.OrderShipment
						.And<SOShipLine.blanketType.IsEqual<SOOrder.orderType.FromCurrent>>
						.And<SOShipLine.blanketNbr.IsEqual<SOOrder.orderNbr.FromCurrent>>>>>
					.And<SOOrderShipment.confirmed.IsEqual<True>>
					.And<SOOrderShipment.createARDoc.IsEqual<True>>
					.And<SOOrderShipment.invoiceNbr.IsNull>>
				.View.ReadOnly.SelectMultiBound(Base, new object[] { order });

			var invoiceEntry = PXGraph.CreateInstance<SOInvoiceEntry>();
			var shipmentEntry = PXGraph.CreateInstance<SOShipmentEntry>();
			var createdDocuments = new InvoiceList(shipmentEntry);

			Customer customer = Customer.PK.Find(Base, order.CustomerID);
			if (customer == null)
				throw new Common.Exceptions.RowNotFoundException(Base.customer.Cache, order.CustomerID);

			foreach (PXResult<SOOrderShipment, SOOrder, CurrencyInfo, SOAddress, SOContact> orderShipment in orderShipments)
			{
				invoiceEntry.InvoiceOrder(new InvoiceOrderArgs(orderShipment)
				{
					InvoiceDate = Base.Accessinfo.BusinessDate.Value,
					Customer = customer,
					List = createdDocuments,
					QuickProcessFlow = quickProcessFlow,
					GroupByCustomerOrderNumber = true
				});
			}

			var miscOrders = SelectFrom<SOOrder>
				.Where<SOOrder.orderQty.IsEqual<decimal0>
						.And<SOOrder.hold.IsEqual<False>>
						.And<SOOrder.cancelled.IsEqual<False>>
						.And<SOOrder.unbilledOrderQty.IsGreater<decimal0>.Or<SOOrder.unbilledMiscTot.IsGreater<decimal0>>>
						.And<Exists<
							Select<SOBlanketOrderLink,
							Where<SOBlanketOrderLink.FK.ChildOrder.
								And<SOBlanketOrderLink.FK.BlanketOrder.SameAsCurrent>>>>>>
				.View.ReadOnly.SelectMultiBound(Base, new object[] { order }).RowCast<SOOrder>().ToList();

			if (miscOrders.Any())
				Base.InvoiceOrder(arguments, miscOrders, createdDocuments, massProcess, quickProcessFlow, true);

			if (!createdDocuments.Any())
			{
				throw new PXException(Messages.NoShipmentsForInvoicing);
			}
			else if (createdDocuments.Count == 1 && !miscOrders.Any())
			{
				throw new PXRedirectRequiredException(invoiceEntry, "Invoice");
			}
			else if (createdDocuments.Count == 1)
			{
				var invoice = (ARInvoice)createdDocuments.First();
				invoiceEntry.Clear();
				invoiceEntry.SelectTimeStamp();
				invoiceEntry.Document.Current = invoiceEntry.Document.Search<ARInvoice.refNbr>(invoice.RefNbr, invoice.DocType);
				throw new PXRedirectRequiredException(invoiceEntry, "Invoice");
			}
			else
			{
				throw new PXOperationCompletedException(Messages.FollowingInvoicesCreated,
					string.Join(", ", createdDocuments.Select(o => ((ARInvoice)o).RefNbr)));
			}
		}

		protected virtual void _(Events.FieldDefaulting<SOOrder, SOOrder.projectID> e)
		{
			if (e.Row.Behavior == SOBehavior.BL)
			{
				e.NewValue = PM.ProjectDefaultAttribute.NonProject();
				e.Cancel = true;
			}
		}

		protected virtual void _(Events.FieldVerifying<SOOrder, SOOrder.expireDate> e)
		{
			VerifyExpireDate(e.Cache, e.Row, (DateTime?)e.NewValue, false);
		}

		protected virtual void _(Events.RowSelected<SOOrder> e)
		{
			bool isBlanket = (e.Row?.Behavior == SOBehavior.BL);

			addBlanketLine.SetVisible(e.Row?.Behavior == SOBehavior.SO);

			if (isBlanket)
			{
				e.Cache.Adjust<PXUIFieldAttribute>(e.Row)
					.For<SOOrder.requestDate>(a =>
						a.Enabled = a.Visible = false)
					.SameFor<SOOrder.projectID>()
					.SameFor<SOOrder.curyBilledPaymentTotal>()
					.SameFor<SOOrder.curyDiscTot>()
					.For<SOOrder.customerID>(a =>
						a.Enabled = (e.Row.ChildLineCntr == 0));
			}

			e.Cache.Adjust<PXUIFieldAttribute>(e.Row)
				.For<SOOrder.expireDate>(a =>
					a.Visible = isBlanket)
				.SameFor<SOOrder.curyTransferredToChildrenPaymentTotal>()
				.For<SOOrder.blanketOpenQty>(a =>
				{
					a.Visible = isBlanket;
					a.Enabled = false;
				});

			Base.Transactions.Cache.Adjust<PXUIFieldAttribute>()
				.For<SOLine.customerOrderNbr>(a =>
					a.Visible = isBlanket)
				.SameFor<SOLine.schedOrderDate>()
				.SameFor<SOLine.schedShipDate>()
				.SameFor<SOLine.pOCreateDate>()
				.SameFor<SOLine.taxZoneID>()
				.SameFor<SOLine.customerLocationID>()
				.SameFor<SOLine.shipVia>()
				.SameFor<SOLine.fOBPoint>()
				.SameFor<SOLine.shipTermsID>()
				.SameFor<SOLine.shipZoneID>()
				.For<SOLine.qtyOnOrders>(a =>
					a.Visible = isBlanket)
				.SameFor<SOLine.blanketOpenQty>()
				.SameFor<SOLine.unshippedQty>()
				.For<SOLine.blanketNbr>(a =>
					a.Visible = !isBlanket)
				.SameFor<SOLine.shippedQty>()
				.SameFor<SOLine.openQty>()
				.SameFor<SOLine.dRTermStartDate>()
				.SameFor<SOLine.dRTermEndDate>()
				.SameFor<SOLine.requestDate>()
				.SameFor<SOLine.shipDate>()
				.SameFor<SOLine.completeQtyMin>()
				.SameFor<SOLine.completeQtyMax>()
				.SameFor<SOLine.reasonCode>()
				.SameFor<SOLine.avalaraCustomerUsageType>();
			Base.Transactions.Cache.Adjust<PXUIFieldAttribute>()
				.For<SOLine.taskID>(a =>
					a.Visible &= !isBlanket)
				.SameFor<SOLine.costCodeID>();

			Base.splits.Cache.Adjust<PXUIFieldAttribute>()
				.For<SOLineSplit.customerOrderNbr>(a =>
					a.Enabled = a.Visible = isBlanket)
				.SameFor<SOLineSplit.schedOrderDate>()
				.SameFor<SOLineSplit.schedShipDate>()
				.SameFor<SOLineSplit.pOCreateDate>()
				.For<SOLineSplit.qtyOnOrders>(a =>
					a.Visible = isBlanket)
				.SameFor<SOLineSplit.blanketOpenQty>();

			Base.recalcdiscountsfilter.Cache.Adjust<PXUIFieldAttribute>()
				.For<RecalcDiscountsParamFilter.calcDiscountsOnLinesWithDisabledAutomaticDiscounts>(a =>
					a.Visible = !isBlanket &&
					e.Row?.BlanketLineCntr > 0) // TODO: BlanketSO: revise this condition - the checkbox itself is universal and can be used not only for orders with blaket lines
				.For<RecalcDiscountsParamFilter.overrideManualDocGroupDiscounts>(a =>
					a.Visible = !isBlanket);

			string expireDateError = PXUIFieldAttribute.GetErrorOnly<SOOrder.expireDate>(e.Cache, e.Row);
			if (string.IsNullOrEmpty(expireDateError) && isBlanket)
			{
				bool showExpireDateWarning =
					e.Row.ExpireDate < Base.Accessinfo.BusinessDate
					&& e.Row.Hold == false && e.Row.Cancelled == false && e.Row.Completed == false && e.Row.IsExpired == false
					&& e.Row.ExpireDate >= e.Row.OrderDate && e.Cache.GetStatus(e.Row) != PXEntryStatus.Inserted;
				e.Cache.RaiseExceptionHandling<SOOrder.expireDate>(e.Row, e.Row.ExpireDate,
					showExpireDateWarning ? new PXSetPropertyException(Messages.OrderExpiredChangeDate, PXErrorLevel.Warning) : null);
			}

			Base.addInvoice.SetVisible(!isBlanket);

			Base.Adjustments.Cache.AdjustUI()
				.For<SOAdjust.curyAdjdBilledAmt>(a =>
					a.Visible = !isBlanket)
				.For<SOAdjust.curyAdjdTransferredToChildrenAmt>(a =>
					a.Visible = isBlanket);

			Base.Taxes.Cache.Adjust<PXUIFieldAttribute>()
				.For<SOTaxTran.taxZoneID>(a =>
					a.Visible = isBlanket)
				.For<SOTaxTran.taxID>(a =>
					a.Enabled = !isBlanket)
				.SameFor<SOTaxTran.curyTaxableAmt>()
				.SameFor<SOTaxTran.curyTaxAmt>();

			if (isBlanket)
			{
				Base.Taxes.Cache.AllowInsert = false;
			}

			if (e.Row?.IsExpired == true)
			{
				Base.Document.Cache.AllowDelete = false;
				Base.Transactions.AllowDelete = false;
				Base.Transactions.AllowInsert = false;
				Base.addInvBySite.SetEnabled(false);
				Base.addInvoice.SetEnabled(false);
				if (Base.Actions.Contains(nameof(MatrixEntryExt.showMatrixPanel)))
				{
					Base.Actions[nameof(MatrixEntryExt.showMatrixPanel)].SetEnabled(false);
				}
			}
		}

		protected virtual void _(Events.RowDeleting<SOOrder> e)
		{
			if (e.Row.Behavior == SOBehavior.BL)
			{
				if (BlanketOrderChildrenList.SelectSingle() != null)
				{
					throw new PXException(Messages.CannotDeleteBlanketWithChild, Base.Document.Current.OrderNbr);
				}

				if (Base.Adjustments.Select().Any())
				{
					throw new PXException(Messages.CannotDeleteBlanketWithPayment, Base.Document.Current.OrderNbr);
				}
			}
		}

		protected virtual void _(Events.RowPersisting<SOOrder> e)
		{
			if (e.Operation.IsNotIn(PXDBOperation.Insert, PXDBOperation.Update))
				return;
			VerifyExpireDate(e.Cache, e.Row, e.Row.ExpireDate, true);
		}

		private void VerifyExpireDate(PXCache cache, SOOrder row, DateTime? val, bool persist)
		{
			if (row.Behavior != SOBehavior.BL) return;
			if (val < row.OrderDate)
			{
				string msg = Messages.ExpireDateLessOrderDate;
				if (cache.RaiseExceptionHandling<SOOrder.expireDate>(row, val, new PXSetPropertyException(msg, PXErrorLevel.Error))
					&& persist)
				{
					throw new PXRowPersistingException(typeof(SOOrder.expireDate).Name, val, msg);
				}
			}
			else if (val < Base.Accessinfo.BusinessDate
				&& row.Hold == false && row.Cancelled == false && row.Completed == false && row.IsExpired == false
				&& !Base.UnattendedMode)
			{
				string msg = Messages.CantSaveExpiredOrder;
				if (cache.RaiseExceptionHandling<SOOrder.expireDate>(row, val, new PXSetPropertyException(msg, PXErrorLevel.Error))
					&& persist)
				{
					throw new PXRowPersistingException(typeof(SOOrder.expireDate).Name, val, msg);
				}
			}
		}

		protected virtual void _(Events.FieldVerifying<SOOrder, SOOrder.cancelled> e)
		{
			if (e.Row.Behavior == SOBehavior.BL && (bool?)e.NewValue == true)
			{
				if (BlanketOrderChildrenList.SelectSingle() != null)
				{
					throw new PXException(Messages.CannotCancelBlanketWithChild, Base.Document.Current.OrderNbr);
				}

				if (Base.Adjustments.Select().Any())
				{
					throw new PXException(Messages.CannotCancelBlanketWithPayment, Base.Document.Current.OrderNbr);
				}
			}
		}

		protected virtual void _(Events.FieldVerifying<SOOrder, SOOrder.customerID> e)
		{
			if (e.Row.BlanketLineCntr > 0 && !object.Equals(e.OldValue, e.NewValue))
			{
				Base.RaiseCustomerIDSetPropertyException(e.Cache, e.Row, e.NewValue, Messages.CustomerChangedOnChildSalesOrder);
			}
		}

		private bool _updateLineCustomerLocation = false;

		protected virtual void _(Events.FieldVerifying<SOOrder, SOOrder.customerLocationID> e)
		{
			_updateLineCustomerLocation = true;
			if (e.ExternalCall && e.Row.Behavior == SOBehavior.BL && !Base.CustomerChanged)
			{
				bool hasLines = Base.Transactions.Select().Any();
				if (hasLines && Base.Document.Ask(AR.Messages.CustomerLocation, Messages.ConfirmCustomerLocChange, MessageButtons.YesNo) == WebDialogResult.No)
					_updateLineCustomerLocation = false;
			}
		}

		protected virtual void _(Events.FieldVerifying<SOLine, SOLine.taxZoneID> e)
		{
			if (e.Row.Behavior == SOBehavior.BL && Base.Document.Current != null)
			{
				if (Base.Document.Current.TaxZoneID == (string)e.NewValue && ExternalTax.IsExternalTax(Base, Base.Document.Current.TaxZoneID))
					e.Cancel = true;
			}
		}

		protected virtual void _(Events.FieldVerifying<SOLine, SOLine.siteID> e)
		{
			if (object.Equals(e.NewValue, e.OldValue))
				return;

			if (e.Row.Behavior == SOBehavior.BL && e.Row.ChildLineCntr > 0)
			{
				e.NewValue = INSite.PK.Find(Base, e.OldValue as int?)?.SiteCD;
				throw new PXSetPropertyException(Messages.CannotChangeWarehouseOnBlanketLine);
			}

			if (e.Row.BlanketNbr != null)
			{
				e.NewValue = INSite.PK.Find(Base, e.OldValue as int?)?.SiteCD;
				throw new PXSetPropertyException(Messages.CannotChangeWarehouseOnChildLine, e.Row.BlanketNbr);
			}
		}

		protected virtual void _(Events.RowUpdated<SOOrder> e)
		{
			bool custLocChanged = !e.Cache.ObjectsEqual<SOOrder.customerLocationID>(e.Row, e.OldRow);
			bool taxZoneChanged = !e.Cache.ObjectsEqual<SOOrder.taxZoneID>(e.Row, e.OldRow);
			bool externalTaxZone = ExternalTax.IsExternalTax(Base, e.Row?.TaxZoneID);
			bool freightChanged = !e.Cache.ObjectsEqual<SOOrder.shipVia, SOOrder.fOBPoint, SOOrder.shipTermsID, SOOrder.shipZoneID>(e.Row, e.OldRow);

			if (e.Row.Behavior == SOBehavior.BL)
			{
				bool overrideTaxZoneChanged = !e.Cache.ObjectsEqual<SOOrder.overrideTaxZone>(e.Row, e.OldRow);
				bool dateUpdated =
					(e.Row.OrderDate != e.OldRow.OrderDate && (e.Row.OrderDate > e.OldRow.OrderDate || e.OldRow.OrderDate == null))
					|| (e.Row.ExpireDate != e.OldRow.ExpireDate && (e.Row.ExpireDate < e.OldRow.ExpireDate || e.OldRow.ExpireDate == null));

				if (taxZoneChanged)
				{
					foreach (SOLine soline in Base.Transactions.Select())
					{
						if (externalTaxZone) //no need to call any additional logic while setting external tax zone
						{
							Base.Transactions.Cache.SetValue<SOLine.taxZoneID>(soline, e.Row?.TaxZoneID);
							Base.Transactions.Cache.MarkUpdated(soline);
						}
						else
						{
							if (e.Row.OverrideTaxZone != true)
							{
								ReDefaultLineTaxZone(soline);
							}
							else
							{
								soline.TaxZoneID = e.Row.TaxZoneID;
								Base.Transactions.Update(soline);
							}
						}
					}
				}
				else if (overrideTaxZoneChanged && e.Row.TaxZoneID == null && e.Row.OverrideTaxZone != true)
				{
					foreach (SOLine soline in Base.Transactions.Select())
					{
						ReDefaultLineTaxZone(soline);
					}
				}

				if (custLocChanged && _updateLineCustomerLocation)
				{
					foreach (SOLine line in Base.Transactions.Select())
					{
						line.CustomerLocationID = e.Row.CustomerLocationID;
						Base.Transactions.Update(line);
					}
				}

				if (dateUpdated)
				{
					// need to revalidate blanket schedule dates in lines and splits
					foreach (SOLine line in Base.Transactions.Select())
					{
						Base.Transactions.Cache.MarkUpdated(line);
					}
					foreach (SOLineSplit split in SelectFrom<SOLineSplit>
						.Where<SOLineSplit.FK.Order.SameAsCurrent>
						.View.Select(Base).RowCast<SOLineSplit>())
					{
						Base.splits.Cache.MarkUpdated(split);
					}
				}
			}
			else
			{
				if (taxZoneChanged && externalTaxZone)
				{
					foreach (SOLine soline in Base.Transactions.Select())
					{
						Base.Transactions.Cache.SetValue<SOLine.taxZoneID>(soline, e.Row?.TaxZoneID);
						Base.Transactions.Cache.MarkUpdated(soline);
					}
				}

				if (custLocChanged || freightChanged)
				{
					foreach (SOLine line in Base.Transactions.Select())
					{
						line.CustomerLocationID = e.Row.CustomerLocationID;
						line.ShipVia = e.Row.ShipVia;
						line.FOBPoint = e.Row.FOBPoint;
						line.ShipTermsID = e.Row.ShipTermsID;
						line.ShipZoneID = e.Row.ShipZoneID;
						Base.Transactions.Cache.MarkUpdated(line);
					}
				}
			}

			if (!e.Cache.ObjectsEqual<SOOrder.cancelled>(e.Row, e.OldRow))
			{
				foreach (SOLine line in PXParentAttribute.SelectSiblings(Base.Transactions.Cache, null, typeof(SOOrder)))
				{
					var oldLine = PXCache<SOLine>.CreateCopy(line);
					line.Cancelled = e.Row.Cancelled;
					Base.Transactions.Cache.MarkUpdated(line);
					if (!string.IsNullOrEmpty(line.BlanketNbr))
					{
						OnChildSOLineUpdated(oldLine, line);
					}
				}
			}
		}

		private void ReDefaultLineTaxZone(SOLine soline)
		{
			object newTaxZone;
			Base.Transactions.Cache.RaiseFieldDefaulting<SOLine.taxZoneID>(soline, out newTaxZone);
			if (newTaxZone != null && ExternalTax.IsExternalTax(Base, (string)newTaxZone)) //external tax zones cannot coexist with non-external tax zone in the document header
				newTaxZone = null;
			soline.TaxZoneID = (string)newTaxZone;
			Base.Transactions.Update(soline);
		}

		protected virtual void _(Events.RowSelected<SOAdjust> e)
		{
			SOOrder order = Base.Document.Current;
			if (order?.Behavior == SOBehavior.BL && order.IsExpired == true
				&& e.Row?.CuryAdjdAmt > 0m && e.Row.Voided == false)
			{
				string curyAdjdAmtError = PXUIFieldAttribute.GetErrorOnly<SOAdjust.curyAdjdAmt>(e.Cache, e.Row);
				if (string.IsNullOrEmpty(curyAdjdAmtError))
					e.Cache.RaiseExceptionHandling<SOAdjust.curyAdjdAmt>(e.Row, e.Row.CuryAdjdAmt,
						new PXSetPropertyException(Messages.ExpiredBlanketWithPayment, PXErrorLevel.RowWarning));
			}
		}

		protected virtual void _(Events.FieldUpdated<SOLine, SOLine.customerLocationID> e)
		{
			if (e.Row.Behavior == SOBehavior.BL)
			{
				if (Base.Document.Current?.OverrideTaxZone != true &&
					!ExternalTax.IsExternalTax(Base, Base.Document.Current?.TaxZoneID) &&
					Base.Document.Current?.ExternalTaxesImportInProgress != true)
				{
					e.Cache.SetDefaultExt<SOLine.taxZoneID>(e.Row);
				}
				e.Cache.SetDefaultExt<SOLine.shipVia>(e.Row);
				e.Cache.SetDefaultExt<SOLine.fOBPoint>(e.Row);
				e.Cache.SetDefaultExt<SOLine.shipTermsID>(e.Row);
				e.Cache.SetDefaultExt<SOLine.shipZoneID>(e.Row);
				e.Cache.SetDefaultExt<SOLine.shipComplete>(e.Row);
			}
		}

		protected virtual void _(Events.FieldDefaulting<SOLine, SOLine.shipComplete> e)
		{
			if (e.Row?.Behavior == SOBehavior.BL && e.Cancel != true)
			{
				e.NewValue = Location.PK.Find(Base, e.Row.CustomerID, e.Row.CustomerLocationID)?.CShipComplete;
				e.Cancel = (e.NewValue != null);
			}
		}

		protected virtual void _(Events.FieldDefaulting<SOLine, SOLine.taxZoneID> e)
		{
			if (e.Row.Behavior == SOBehavior.BL && Base.Document.Current?.OverrideTaxZone != true)
			{
				e.NewValue = GetDefaultLocationTaxZone(e.Row.CustomerID, e.Row.CustomerLocationID, Base.Document.Current?.BranchID);
			}
			else
			{
				e.NewValue = Base.Document.Current?.TaxZoneID;
			}
		}

		public virtual string GetDefaultLocationTaxZone(int? customerID, int? customerLocationID, int? branchID)
		{
			Location customerLocation = SelectFrom<Location>
					.Where<Location.bAccountID.IsEqual<@P.AsInt>
						.And<Location.locationID.IsEqual<@P.AsInt>>>
					.View.Select(Base, customerID, customerLocationID);

			return Base.GetDefaultTaxZone(customerLocation, false, customerLocation?.CCarrierID, branchID);
		}

		protected virtual void _(Events.RowSelected<SOLine> e, PXRowSelected baseHandler)
		{
			baseHandler?.Invoke(e.Cache, e.Args);

			if (e.Row == null)
				return;

			bool isBlanket = (e.Row?.Behavior == SOBehavior.BL);
			bool isChild = !string.IsNullOrEmpty(e.Row?.BlanketNbr);
			bool isBlanketWithChild = (isBlanket && e.Row.ChildLineCntr > 0);
			if (isChild || isBlanketWithChild)
			{
				e.Cache.Adjust<PXUIFieldAttribute>(e.Row)
					.For<SOLine.uOM>(a =>
						a.Enabled = false)
					.SameFor<SOLine.inventoryID>();
			}
			if (isBlanketWithChild)
			{
				e.Cache.Adjust<PXUIFieldAttribute>(e.Row)
					.For<SOLine.curyUnitPrice>(a =>
						a.Enabled = false)
					.SameFor<SOLine.curyExtPrice>()
					.SameFor<SOLine.manualPrice>();
			}
			if (isBlanket)
			{
				e.Cache.Adjust<PXUIFieldAttribute>(e.Row)
					.For<SOLine.pOSiteID>(a => a.Enabled = false);

				if (Base.Document.Current?.IsExpired == true)
				{
					PXSetPropertyException exc = null;
					if (e.Row.LineQtyHardAvail == null)
					{
						object newValue = null;
						e.Cache.RaiseFieldSelecting(Base.ItemAvailabilityExt.StatusField.Name, e.Row, ref newValue, false);
					}

					if (e.Row.LineQtyHardAvail > 0m)
					{
						exc = new PXSetPropertyException(Messages.AllocatedInExpiredOrder, PXErrorLevel.RowWarning);
					}
					else if (e.Row.POCreated == true)
					{
						foreach (SOLineSplit split in Base.splits.View.SelectMultiBound(new[] { e.Row }))
						{
							if (split.Completed != true && !string.IsNullOrEmpty(split.PONbr))
							{
								exc = new PXSetPropertyException(Messages.LineLinkedToPOInExpiredOrder, PXErrorLevel.RowWarning, split.PONbr);
								break;
							}
						}
					}
					e.Cache.RaiseExceptionHandling<SOLine.pOCreateDate>(e.Row, e.Row.ExpireDate, exc);
				}
				else
				{
					string poCreateDateError = PXUIFieldAttribute.GetErrorOnly<SOLine.pOCreateDate>(e.Cache, e.Row);
					if (string.IsNullOrEmpty(poCreateDateError))
					{
						PXSetPropertyException exc = null;
						if (e.Row.POCreate == true && e.Row.POCreateDate > e.Row.SchedOrderDate)
						{
							exc = new PXSetPropertyException(Messages.POCreateDateGreaterSchedOrder, PXErrorLevel.Warning,
								e.Row.SchedOrderDate.Value.ToShortDateString());
						}
						e.Cache.RaiseExceptionHandling<SOLine.pOCreateDate>(e.Row, e.Row.ExpireDate, exc);
					}
				}
			}

			bool isBlanketOpenLine = isBlanket && e.Row.Completed != true &&
				(e.Row.BlanketOpenQty > 0 || e.Row.LineType == SOLineType.MiscCharge);

			Base.Transactions.Cache.Adjust<PXUIFieldAttribute>(e.Row)
				.For<SOLine.customerOrderNbr>(a =>
					a.Enabled = isBlanketOpenLine)
				.SameFor<SOLine.schedOrderDate>()
				.SameFor<SOLine.schedShipDate>()
				.SameFor<SOLine.pOCreateDate>()
				.SameFor<SOLine.customerLocationID>()
				.SameFor<SOLine.shipVia>()
				.SameFor<SOLine.fOBPoint>()
				.SameFor<SOLine.shipTermsID>()
				.SameFor<SOLine.shipZoneID>();

			Base.Transactions.Cache.Adjust<PXUIFieldAttribute>(e.Row)
				.For<SOLine.taxZoneID>(a =>
					a.Enabled = (Base.Document.Current?.OverrideTaxZone != true) && isBlanketOpenLine);

		}

		protected virtual void _(Events.FieldVerifying<SOLine, SOLine.orderQty> e)
		{
			if (e.Row.Behavior == SOBehavior.BL)
			{
				if ((decimal?)e.NewValue < e.Row.QtyOnOrders)
					throw new PXSetPropertyException(CS.Messages.Entry_GE, e.Row.QtyOnOrders.Value.ToString("0.####"));
			}
			else if (!string.IsNullOrEmpty(e.Row.BlanketNbr) && e.Row.LineType != SOLineType.MiscCharge)
			{
				decimal? diff = (decimal?)e.NewValue - e.Row.OrderQty;

				if (diff != 0m)
				{
					var splits = PXParentAttribute.SelectChildren(Base.splits.Cache, e.Row, typeof(SOLine));
					var origChildSplit = GetOrigChildSplit(e.Row, splits.Cast<SOLineSplit>());
					if (origChildSplit != null)
					{
						throw new PXSetPropertyException(Messages.CannotChangeQtyChildLineWithPOReceipt, e.Row.BlanketNbr);
					}
				}

				if (diff > 0m && e.ExternalCall)
				{
					BlanketSOLineSplit blanketSplit = SelectParentSplit(e.Row);
					if (diff > blanketSplit.BlanketOpenQty)
					{
						e.NewValue = e.Row.OrderQty + blanketSplit.BlanketOpenQty;
						Base.Transactions.Cache.RaiseExceptionHandling<SOLine.orderQty>(e.Row, e.NewValue,
							new PXSetPropertyException(Messages.QtyExceedsBlanketOpenQty, blanketSplit.OrderNbr));
					}
				}
			}
		}

		protected virtual void _(Events.RowPersisting<SOLine> e)
		{
			if (e.Row.Behavior == SOBehavior.BL && e.Operation.IsIn(PXDBOperation.Insert, PXDBOperation.Update))
			{
				SOOrder order = Base.Document.Current;
				string errorMsg = (e.Row.SchedOrderDate < order?.OrderDate) ? Messages.ChildDateCantBeEarlierBlanket
					: (e.Row.SchedOrderDate > order?.ExpireDate) ? Messages.ChildDateCantBeLaterExpiration : null;
				if (!string.IsNullOrEmpty(errorMsg))
				{
					if (e.Cache.RaiseExceptionHandling<SOLine.schedOrderDate>(e.Row, e.Row.SchedOrderDate, new PXSetPropertyException(errorMsg, PXErrorLevel.Error)))
					{
						throw new PXRowPersistingException(typeof(SOLine.schedOrderDate).Name, e.Row.SchedOrderDate, errorMsg);
					}
				}

				errorMsg = (e.Row.SchedShipDate < e.Row.SchedOrderDate) ? Messages.SchedShipDateCantBeEarlierChildDate
					: (e.Row.SchedOrderDate == null && e.Row.SchedShipDate < order?.OrderDate) ? Messages.SchedShipDateCantBeEarlierBlanket
					: (e.Row.SchedShipDate > order?.ExpireDate) ? Messages.SchedShipDateCantBeLaterExpiration : null;
				if (!string.IsNullOrEmpty(errorMsg))
				{
					if (e.Cache.RaiseExceptionHandling<SOLine.schedShipDate>(e.Row, e.Row.SchedShipDate, new PXSetPropertyException(errorMsg, PXErrorLevel.Error)))
					{
						throw new PXRowPersistingException(typeof(SOLine.schedShipDate).Name, e.Row.SchedShipDate, errorMsg);
					}
				}
			}
		}

		protected virtual void _(Events.RowPersisted<SOLine> e)
		{
			if (!string.IsNullOrEmpty(e.Row.BlanketNbr) && e.Operation == PXDBOperation.Delete)
			{
				switch (e.TranStatus)
				{
					case PXTranStatus.Open:
						// Acuminator disable once PX1045 PXGraphCreateInstanceInEventHandlers
						// [we must have the business logic from the graph here and also isolate these changes to be able to clear them]
						// Acuminator disable once PX1073 ExceptionsInRowPersisted
						// [exceptions with Open status are OK, they will be handled in the Aborted case handler]
						ReturnReceivedAllocationsToBlanket(e.Row);
						break;
					case PXTranStatus.Aborted:
						ClearReturnReceivedAllocationsToBlanket();
						break;
				}
			}
		}

		protected virtual void _(Events.RowSelected<SOLineSplit> e)
		{
			if (e.Row?.Behavior == SOBehavior.BL)
			{
				SOOrder order = Base.Document.Current;

				PXSetPropertyException poCreateDateExc = null;
				if (e.Row.POCreate == true && e.Row.POCreateDate > e.Row.SchedOrderDate)
				{
					poCreateDateExc = new PXSetPropertyException(Messages.POCreateDateGreaterSchedOrder, PXErrorLevel.Warning,
						e.Row.SchedOrderDate.Value.ToShortDateString());
				}
				e.Cache.RaiseExceptionHandling<SOLine.pOCreateDate>(e.Row, e.Row.ExpireDate, poCreateDateExc);
				e.Cache.RaiseExceptionHandling<SOLineSplit.schedOrderDate>(e.Row, e.Row.SchedOrderDate, GetSchedOrderDateException(e.Row, order));
				e.Cache.RaiseExceptionHandling<SOLineSplit.schedShipDate>(e.Row, e.Row.SchedShipDate, GetSchedShipDateException(e.Row, order));

				Base.splits.Cache.Adjust<PXUIFieldAttribute>(e.Row)
					.For<SOLineSplit.customerOrderNbr>(a =>
						a.Enabled = (e.Row.Completed != true))
					.SameFor<SOLineSplit.schedOrderDate>()
					.SameFor<SOLineSplit.schedShipDate>()
					.SameFor<SOLineSplit.pOCreateDate>();
			}
		}

		private PXException GetSchedOrderDateException(SOLineSplit s, SOOrder order)
		{
			string errorMsgSchedOrderDate = (s.SchedOrderDate < order?.OrderDate) ? Messages.ChildDateCantBeEarlierBlanket
				: (s.SchedOrderDate > order?.ExpireDate) ? Messages.ChildDateCantBeLaterExpiration : null;
			return string.IsNullOrEmpty(errorMsgSchedOrderDate) ? null
				: new PXSetPropertyException(errorMsgSchedOrderDate, PXErrorLevel.Error);
		}

		private PXException GetSchedShipDateException(SOLineSplit s, SOOrder order)
		{
			string errorMsgSchedShipDate = (s.SchedShipDate < s.SchedOrderDate) ? Messages.SchedShipDateCantBeEarlierChildDate
				: (s.SchedOrderDate == null && s.SchedShipDate < order?.OrderDate) ? Messages.SchedShipDateCantBeEarlierBlanket
				: (s.SchedShipDate > order?.ExpireDate) ? Messages.SchedShipDateCantBeLaterExpiration : null;
			return string.IsNullOrEmpty(errorMsgSchedShipDate) ? null
				: new PXSetPropertyException(errorMsgSchedShipDate, PXErrorLevel.Error);
		}

		private DateTime? CalcSchedOrderDate(SOLineSplit s)
			=> (s.Behavior == SOBehavior.BL && s.Completed == false && s.Qty > s.QtyOnOrders + s.ReceivedQty) ? s.SchedOrderDate : null;

		protected virtual void _(Events.RowInserted<SOLineSplit> e)
		{
			OnSchedOrderDateUpdated(null, CalcSchedOrderDate(e.Row));
		}

		protected virtual void _(Events.RowUpdated<SOLineSplit> e)
		{
			OnSchedOrderDateUpdated(CalcSchedOrderDate(e.OldRow), CalcSchedOrderDate(e.Row));
		}

		protected virtual void _(Events.RowDeleted<SOLineSplit> e)
		{
			OnSchedOrderDateUpdated(CalcSchedOrderDate(e.Row), null);
		}

		protected virtual void _(Events.RowPersisting<SOLineSplit> e)
		{
			if (e.Row.Behavior != SOBehavior.BL || e.Operation.IsNotIn(PXDBOperation.Insert, PXDBOperation.Update))
				return;

			SOOrder order = Base.Document.Current;
			var schedOrderDateExc = GetSchedOrderDateException(e.Row, order);
			if (schedOrderDateExc != null)
			{
				if (e.Cache.RaiseExceptionHandling<SOLineSplit.schedOrderDate>(e.Row, e.Row.SchedOrderDate, schedOrderDateExc))
				{
					throw new PXRowPersistingException(typeof(SOLineSplit.schedOrderDate).Name, e.Row.SchedOrderDate, schedOrderDateExc.MessageNoPrefix);
				}
			}

			var schedShipDateExc = GetSchedShipDateException(e.Row, order);
			if (schedShipDateExc != null)
			{
				if (e.Cache.RaiseExceptionHandling<SOLineSplit.schedShipDate>(e.Row, e.Row.SchedShipDate, schedShipDateExc))
				{
					throw new PXRowPersistingException(typeof(SOLineSplit.schedShipDate).Name, e.Row.SchedShipDate, schedShipDateExc.MessageNoPrefix);
				}
			}
		}

		protected virtual void _(Events.FieldVerifying<SOLineSplit, SOLineSplit.qty> e)
		{
			if (e.Row.Behavior == SOBehavior.BL
				&& (decimal?)e.NewValue < e.Row.QtyOnOrders + e.Row.ReceivedQty)
			{
				throw new PXSetPropertyException(CS.Messages.Entry_GE, (e.Row.QtyOnOrders + e.Row.ReceivedQty).Value.ToString("0.####"));
			}
		}

		protected virtual void _(Events.RowPersisting<BlanketSOLineSplit> e)
		{
			if (e.Row.BlanketOpenQty >= 0m
				&& (e.Row.LineType == SOLineType.MiscCharge || e.Row.Qty >= e.Row.QtyOnOrders + e.Row.ReceivedQty)) return;

			foreach (SOLine line in Base.Transactions.Cache.Updated
				.Concat_(Base.Transactions.Cache.Inserted)
				.RowCast<SOLine>().Where(l
					=> l.BlanketType == e.Row.OrderType
					&& l.BlanketNbr == e.Row.OrderNbr
					&& l.BlanketLineNbr == e.Row.LineNbr
					&& l.BlanketSplitLineNbr == e.Row.SplitLineNbr))
			{
				Base.Transactions.Cache.RaiseExceptionHandling<SOLine.orderQty>(line, line.OrderQty,
					new PXException(Messages.QtyExceedsBlanketOpenQty, e.Row.OrderNbr));
			}

			throw new PXRowPersistingException(typeof(BlanketSOLine.blanketOpenQty).Name,
				e.Row.BlanketOpenQty, Messages.QtyExceedsBlanketOpenQty, e.Row.OrderNbr);
		}

		protected virtual void _(Events.RowUpdated<SOLine> e)
		{
			if (e.Row.Behavior == SOBehavior.BL && e.Row.SchedShipDate != e.OldRow.SchedShipDate
				&& (e.Row.SchedShipDate < e.OldRow.SchedShipDate || e.OldRow.SchedShipDate == null))
			{
				// need to revalidate blanket schedule dates in splits
				foreach (SOLineSplit split in Base.splits.View.SelectMultiBound(new[] { e.Row }))
				{
					Base.splits.Cache.MarkUpdated(split);
				}
			}
			else if (!string.IsNullOrEmpty(e.Row.BlanketNbr))
			{
				OnChildSOLineUpdated(e.OldRow, e.Row);
			}
		}

		protected virtual void _(Events.FieldUpdated<BlanketSOLineSplit, BlanketSOLineSplit.effectiveChildLineCntr> e)
		{
			if (e.Row.LineType == SOLineType.MiscCharge)
			{
				int? oldValue = (int?)e.OldValue,
					newValue = (int?)e.NewValue;
				if (oldValue != newValue && (oldValue == 0 || newValue == 0))
				{
					e.Cache.SetValueExt<BlanketSOLineSplit.completed>(e.Row, newValue > 0);
				}
			}
		}

		protected virtual void _(Events.RowUpdated<BlanketSOLineSplit> e)
		{
			if (e.Row.LineType == SOLineType.MiscCharge
				&& !e.Cache.ObjectsEqual<BlanketSOLineSplit.completed>(e.OldRow, e.Row))
			{
				var blanketLine = PXParentAttribute.SelectParent<BlanketSOLine>(e.Cache, e.Row);
				if (blanketLine == null)
				{
					throw new Common.Exceptions.RowNotFoundException(Base.Caches<BlanketSOLine>(),
						e.Row.OrderType, e.Row.OrderNbr, e.Row.LineNbr);
				}
				if (e.Row.Completed == true)
				{
					bool allSplitsCompleted = PXParentAttribute.SelectChildren(e.Cache, blanketLine, typeof(BlanketSOLine))
						.RowCast<BlanketSOLineSplit>().All(s => s.Completed == true);
					if (allSplitsCompleted)
					{
						blanketLine.Completed = true;
					}
					blanketLine.ClosedQty += e.Row.Qty;
				}
				else
				{
					blanketLine.Completed = false;
					blanketLine.ClosedQty -= e.Row.Qty;
				}
				blanketLine = (BlanketSOLine)Base.Caches<BlanketSOLine>().Update(blanketLine);
			}
		}

		protected virtual void _(Events.FieldDefaulting<SOOrder, SOOrder.taxZoneID> e, PXFieldDefaulting baseMethod)
		{
			baseMethod(e.Cache, e.Args);

			if (e.Row != null && e.Row.Behavior == SOBehavior.BL)
			{
				if (e.Row.OverrideTaxZone != true && e.NewValue != null && !ExternalTax.IsExternalTax(Base, (string)e.NewValue))
				{
					e.NewValue = null;
					return;
				}

				if (e.Row.OverrideTaxZone == true)
				{
					e.NewValue = e.Row.TaxZoneID;
					return;
				}
			}
		}

		public PXAction<SOOrder> overrideBlanketTaxZone;
		[PXButton(CommitChanges = true), PXUIField(MapEnableRights = PXCacheRights.Select, Visible = false)]
		protected virtual IEnumerable OverrideBlanketTaxZone(PXAdapter adapter)
		{
			if (Base.Document.Current != null && Base.Document.Current.Behavior == SOBehavior.BL)
			{
				if (Base.Document.Current.OverrideTaxZone == true && (SOLine)Base.Transactions.Select() != null &&
					!Base.IsMobile && BlanketTaxZoneOverrideFilter.Current != null &&
					BlanketTaxZoneOverrideFilter.AskExt() == WebDialogResult.Yes)
				{
					if (Base.Document.Current.TaxZoneID != BlanketTaxZoneOverrideFilter.Current.TaxZoneID)
					{
						SOOrder order = (SOOrder)Base.Document.Cache.CreateCopy(Base.Document.Current);
						order.TaxZoneID = BlanketTaxZoneOverrideFilter.Current?.TaxZoneID;
						Base.Document.Update(order);
					}

					if (BlanketTaxZoneOverrideFilter.Current.TaxZoneID == null)
					{
						foreach (SOLine soline in Base.Transactions.Select())
						{
							soline.TaxZoneID = null;
							Base.Transactions.Update(soline);
						}
					}
				}
				else
				{
					Base.Document.Cache.SetValue<SOOrder.overrideTaxZone>(Base.Document.Current, false);
				}
			}

			return adapter.Get();
		}

		protected virtual void _(Events.RowDeleted<SOLine> e)
		{
			SOLine row = e.Row;

			if (row != null && row.BlanketNbr != null && Base.Document.Current != null &&
				Base.Document.Cache.GetStatus(Base.Document.Current).IsNotIn(PXEntryStatus.Deleted, PXEntryStatus.InsertedDeleted))
			{
				SOLine blanketLine = Base.Transactions.Select().Where(x => ((SOLine)x).BlanketType == row.BlanketType && ((SOLine)x).BlanketNbr == row.BlanketNbr).FirstOrDefault();
				if (blanketLine == null)
				{
					SOBlanketOrderLink linkToDelete = SelectFrom<SOBlanketOrderLink>.
						Where<SOBlanketOrderLink.blanketType.IsEqual<@P.AsString.ASCII>.
							And<SOBlanketOrderLink.blanketNbr.IsEqual<@P.AsString>>.
							And<SOBlanketOrderLink.FK.ChildOrder.SameAsCurrent>>.View.SelectSingleBound(Base, new[] { Base.Document.Current }, row.BlanketType, row.BlanketNbr);
					if (linkToDelete != null)
					{
						BlanketOrderChildrenList.Delete(linkToDelete);
					}
				}
			}
		}

		protected virtual void _(Events.RowDeleting<SOLine> e)
		{
			if (e.Row.Behavior == SOBehavior.BL && e.Row.ChildLineCntr > 0)
			{
				ThrowExceptionCannotDeleteBlanket(e.Row.OrderType, e.Row.OrderNbr, e.Row.LineNbr);
			}
		}

		protected virtual void _(Events.RowDeleting<SOLineSplit> e)
		{
			if (e.Row.Behavior == SOBehavior.BL && e.Row.ChildLineCntr > 0)
			{
				ThrowExceptionCannotDeleteBlanket(e.Row.OrderType, e.Row.OrderNbr, e.Row.LineNbr, e.Row.SplitLineNbr);
			}
		}

		protected virtual void _(Events.RowInserted<SOOrderDiscountDetail> e)
		{
			CheckForLinesWithDiscountCalculationDisabled(e.Row);
		}

		protected virtual void _(Events.RowUpdated<SOOrderDiscountDetail> e)
		{
			CheckForLinesWithDiscountCalculationDisabled(e.Row);
		}

		protected virtual void CheckForLinesWithDiscountCalculationDisabled(SOOrderDiscountDetail discountDetail)
		{
			if (Base.Document.Current?.BlanketLineCntr > 0)
			{
				bool hasLinesWithDiscountsDisabled = false;
				foreach (SOLine line in Base.Transactions.Select())
				{
					if (line.AutomaticDiscountsDisabled == true)
					{
						hasLinesWithDiscountsDisabled = true;
						break;
					}
				}
				if (hasLinesWithDiscountsDisabled)
				{
					Base.DiscountDetails.Cache.RaiseExceptionHandling<SOOrderDiscountDetail.discountID>(discountDetail, discountDetail.DiscountID,
						new PXSetPropertyException(Messages.ChildOrderHasLinesWithDisabledDiscounts, PXErrorLevel.Warning));
				}
			}
		}

		protected virtual void ThrowExceptionCannotDeleteBlanket(string orderType, string orderNbr, int? lineNbr, int? splitNbr = null)
		{
			SOLine childLine = SelectFrom<SOLine>
				.Where<SOLine.blanketType.IsEqual<@P.AsString.ASCII>
					.And<SOLine.blanketNbr.IsEqual<@P.AsString>>
					.And<SOLine.blanketLineNbr.IsEqual<@P.AsInt>>
					.And<SOLine.blanketSplitLineNbr.IsEqual<@P.AsInt>.Or<@P.AsInt.IsNull>>>
				.View.ReadOnly.Select(Base, orderType, orderNbr, lineNbr, splitNbr, splitNbr);
			throw new PXException(Messages.CannotDeleteBlanketLineWithChild, childLine?.OrderNbr);
		}

		protected virtual void OnChildSOLineUpdated(SOLine oldLine, SOLine line)
		{
			PXCache cache = Base.Transactions.Cache;
			bool miscLine = line.LineType == SOLineType.MiscCharge;
			bool cancelledChanged = !cache.ObjectsEqual<SOLine.cancelled>(oldLine, line);
			bool completedChanged = !cache.ObjectsEqual<SOLine.closedQty>(oldLine, line);
			if (cancelledChanged || completedChanged && !miscLine)
			{
				// supposed that this method handles several cases:
				// 1. during Confirm/Correct Shipment
				// 2. when child line is Completed/Uncompleted manually
				// 3. during Cancel Order / Re-open Order
				int effectiveChildLineCntrDiff = (line.Cancelled != true ? 1 : 0) - (oldLine.Cancelled != true ? 1 : 0);
				decimal? qtyOnOrdersDiff =
					(line.Cancelled != true ? line.OrderQty : 0m)
					- (oldLine.Cancelled != true ? oldLine.OrderQty : 0m);
				decimal? shippedQtyDiff =
					(line.Cancelled != true ? line.ClosedQty : 0m)
					- (oldLine.Cancelled != true ? oldLine.ClosedQty : 0m);

				BlanketSOLineSplit blanketSplit = SelectParentSplit(line);
				blanketSplit.EffectiveChildLineCntr += effectiveChildLineCntrDiff;
				blanketSplit.QtyOnOrders += qtyOnOrdersDiff;
				if (!miscLine)
				{
					blanketSplit.ShippedQty += shippedQtyDiff;
					if (shippedQtyDiff > 0m && blanketSplit.Completed != true
						|| shippedQtyDiff < 0m && blanketSplit.Completed == true)
					{
						blanketSplit.Completed = (blanketSplit.ShippedQty + blanketSplit.ReceivedQty >= blanketSplit.Qty);
					}
				}
				blanketSplit = (BlanketSOLineSplit)Base.Caches<BlanketSOLineSplit>().Update(blanketSplit);

				var blanketLine = PXParentAttribute.SelectParent<BlanketSOLine>(cache, line);
				if (blanketLine == null)
				{
					throw new Common.Exceptions.RowNotFoundException(Base.Caches<BlanketSOLine>(),
						line.BlanketType, line.BlanketNbr, line.BlanketLineNbr);
				}
				blanketLine.QtyOnOrders += qtyOnOrdersDiff;
				if (!miscLine)
				{
					blanketLine.ShippedQty += shippedQtyDiff;
					blanketLine.ClosedQty += shippedQtyDiff;
					if (blanketLine.ClosedQty > blanketLine.OrderQty)
						blanketLine.ClosedQty = blanketLine.OrderQty;
					if (shippedQtyDiff > 0m && blanketLine.Completed != true
						|| shippedQtyDiff < 0m && blanketLine.Completed == true)
					{
						blanketLine.Completed = (blanketLine.ShippedQty >= blanketLine.OrderQty);
					}
				}
				blanketLine = (BlanketSOLine)Base.Caches<BlanketSOLine>().Update(blanketLine);
			}
		}

		protected virtual void OnSchedOrderDateUpdated(DateTime? oldDate, DateTime? newDate)
		{
			if (oldDate == newDate || Base.Document.Current == null
				|| Base.Document.Current.Behavior != SOBehavior.BL
				|| Base.Document.Cache.GetStatus(Base.Document.Current).IsIn(PXEntryStatus.Deleted, PXEntryStatus.InsertedDeleted))
				return;

			bool decreased = (newDate != null && (oldDate == null || newDate < oldDate));
			if (decreased)
			{
				if (Base.Document.Current.MinSchedOrderDate == null
					|| Base.Document.Current.MinSchedOrderDate > newDate)
				{
					Base.Document.Current.MinSchedOrderDate = newDate;
					Base.Document.UpdateCurrent();
				}
			}
			else
			{
				if (Base.Document.Current.MinSchedOrderDate == null
					|| Base.Document.Current.MinSchedOrderDate >= oldDate)
				{
					Base.Document.Current.MinSchedOrderDate =
						SelectFrom<SOLineSplit>
							.Where<SOLineSplit.FK.Order.SameAsCurrent
								.And<SOLineSplit.completed.IsEqual<False>>
								.And<SOLineSplit.qty.IsGreater<SOLineSplit.qtyOnOrders.Add<SOLineSplit.receivedQty>>>>
							.View.Select(Base).RowCast<SOLineSplit>()
							.Min(s => s.SchedOrderDate);
					Base.Document.UpdateCurrent();
				}
			}
		}

		public delegate void ConfirmSingleLineDelegate(SOLine line, SOShipLine shipline, string lineShippingRule, ref bool backorderExists);
		/// <summary>
		/// Overrides <see cref="SOOrderEntry.ConfirmSingleLine"/>
		/// </summary>
		[PXOverride]
		public virtual void ConfirmSingleLine(SOLine line, SOShipLine shipline, string lineShippingRule, ref bool backorderExists,
			ConfirmSingleLineDelegate base_ConfirmSingleLine)
		{
			base_ConfirmSingleLine(line, shipline, lineShippingRule, ref backorderExists);
			UpdateBlanketOrderShipmentCntr(line, 1);
		}

		public delegate SOLine CorrectSingleLineDelegate(SOLine line, SOShipLine shipLine, bool lineSwitched,
			Dictionary<int?, (SOLine, decimal?, decimal?)> lineOpenQuantities);
		/// <summary>
		/// Overrides <see cref="SOOrderEntry.CorrectSingleLine"/>
		/// </summary>
		[PXOverride]
		public virtual SOLine CorrectSingleLine(SOLine line, SOShipLine shipLine, bool lineSwitched,
			Dictionary<int?, (SOLine, decimal?, decimal?)> lineOpenQuantities, CorrectSingleLineDelegate base_CorrectSingleLine)
		{
			var ret = base_CorrectSingleLine(line, shipLine, lineSwitched, lineOpenQuantities);
			UpdateBlanketOrderShipmentCntr(line, -1);

			return ret;
		}

		private void UpdateBlanketOrderShipmentCntr(SOLine line, int diff)
		{
			if (!string.IsNullOrEmpty(line.BlanketNbr))
			{
				var blanketOrder = PXParentAttribute.SelectParent<BlanketSOOrder>(Base.Transactions.Cache, line);
				if (blanketOrder == null)
				{
					throw new Common.Exceptions.RowNotFoundException(Base.Caches<BlanketSOOrder>(), line.BlanketType, line.BlanketNbr);
				}
				if (blanketOrder.ShipmentCntrUpdated != true)
				{
					blanketOrder.ShipmentCntr += diff;
					blanketOrder.ShipmentCntrUpdated = true;
					blanketOrder = (BlanketSOOrder)Base.Caches<BlanketSOOrder>().Update(blanketOrder);
				}
			}
		}

		/// <summary>
		/// Overrides <see cref="SOOrderEntry.IsAddingPaymentsAllowed"/>
		/// </summary>
		[PXOverride]
		public virtual bool IsAddingPaymentsAllowed(SOOrder order, SOOrderType orderType,
			Func<SOOrder, SOOrderType, bool> baseMethod)
		{
			if (order?.Behavior == SOBehavior.BL &&
				(order.IsExpired == true || order.Hold == true || order.ChildLineCntr != 0))
			{
				return false;
			}

			return baseMethod(order, orderType);
		}

		protected virtual void _(Events.FieldVerifying<SOAdjust, SOAdjust.curyAdjdAmt> e)
		{
			if (object.Equals(e.NewValue, e.OldValue))
				return;

			if (Base.Document.Current?.Behavior == SOBehavior.BL && Base.Document.Current.ChildLineCntr != 0)
			{
				e.NewValue = e.OldValue;
				throw new PXSetPropertyException(Messages.CannotChangeAppliedToOrderAmountOnBlnaket, PXErrorLevel.Warning);
			}
		}

		/// <summary>
		/// Overrides <see cref="SOOrderEntry.VerifyAppliedToOrderAmount"/>
		/// </summary>
		[PXOverride]
		public virtual void VerifyAppliedToOrderAmount(SOOrder doc, Action<SOOrder> baseMethod)
		{
			if (doc?.Behavior == SOBehavior.BL && doc.ChildLineCntr != 0)
				return;

			baseMethod(doc);
		}

		protected virtual void _(Events.RowPersisted<SOAdjust> e)
		{
			if (e.Operation.Command() == PXDBOperation.Delete && e.TranStatus == PXTranStatus.Open)
			{
				SOOrder order = null;

				if (Base.Document.Current?.OrderType == e.Row.AdjdOrderType &&
					Base.Document.Current.OrderNbr == e.Row.AdjdOrderNbr)
				{
					order = Base.Document.Current;
				}
				else
				{
					order = SOAdjust.FK.Order.FindParent(Base, e.Row);
				}

				if (order?.Behavior == SOBehavior.BL && order.ChildLineCntr > 0)
					// Acuminator disable once PX1043 SavingChangesInEventHandlers There is a verification that the TranStatus is Open in the parent if block.
					ClearLinksToBlanketSOAdjust(e.Row);
			}
		}

		protected virtual void ClearLinksToBlanketSOAdjust(SOAdjust adjustment)
		{
			PXDatabase.Update<SOAdjust>(
				new PXDataFieldAssign<SOAdjust.blanketRecordID>(null),
				new PXDataFieldAssign<SOAdjust.blanketNbr>(null),
				new PXDataFieldAssign<SOAdjust.blanketType>(null),
				new PXDataFieldRestrict<SOAdjust.blanketRecordID>(adjustment.RecordID),
				new PXDataFieldRestrict<SOAdjust.blanketType>(adjustment.AdjdOrderType),
				new PXDataFieldRestrict<SOAdjust.blanketNbr>(adjustment.AdjdOrderNbr),
				new PXDataFieldRestrict<SOAdjust.adjgDocType>(adjustment.AdjgDocType),
				new PXDataFieldRestrict<SOAdjust.adjgRefNbr>(adjustment.AdjgRefNbr));
		}

		#region Returning Received Allocations to Blanket when the line is deleted

		private BlanketSOLineSplit SelectParentSplit(SOLine row)
		{
			var blanketSplit = PXParentAttribute.SelectParent<BlanketSOLineSplit>(Base.Transactions.Cache, row);
			if (blanketSplit == null)
			{
				throw new Common.Exceptions.RowNotFoundException(Base.Caches<BlanketSOLineSplit>(),
					row.BlanketType, row.BlanketNbr, row.BlanketLineNbr, row.BlanketSplitLineNbr);
			}
			return blanketSplit;
		}

		private SOLineSplit GetOrigChildSplit(SOLine line, IEnumerable<SOLineSplit> splits)
			=> splits.Where(s => s.POCreate == true && s.ReceivedQty > 0m)
				.SingleOrDefault(s =>
				{
					BlanketSOLineSplit blanketSplit = SelectParentSplit(line);
					return blanketSplit.POType == s.POType && blanketSplit.PONbr == s.PONbr && blanketSplit.POLineNbr == s.POLineNbr;
				});

		private SOOrderEntry _graphForReturnReceivedAllocationsToBlanket = null;

		private SOOrderEntry GetGraphForReturnReceivedAllocationsToBlanket()
		{
			if (_graphForReturnReceivedAllocationsToBlanket == null)
			{
				_graphForReturnReceivedAllocationsToBlanket = PXGraph.CreateInstance<SOOrderEntry>();
				Base.OnBeforeCommit += PersistReturnReceivedAllocationsToBlanket;
				Base.OnAfterPersist += ClearAffectedCaches;
			}
			return _graphForReturnReceivedAllocationsToBlanket;
		}

		private void ClearReturnReceivedAllocationsToBlanket()
		{
			if (_graphForReturnReceivedAllocationsToBlanket != null)
			{
				Base.OnAfterPersist -= ClearAffectedCaches;
				Base.OnBeforeCommit -= PersistReturnReceivedAllocationsToBlanket;
				_graphForReturnReceivedAllocationsToBlanket = null;
			}
		}

		private void PersistReturnReceivedAllocationsToBlanket(PXGraph graph)
		{
			_graphForReturnReceivedAllocationsToBlanket.Save.Press();
			Base.SelectTimeStamp();
		}

		private void ClearAffectedCaches(PXGraph graph)
		{
			BlanketSplits.Cache.Clear();
			Base.splits.Cache.Clear();
			Base.Caches<INItemPlan>().Clear();
			Base.Caches<BlanketSOOrder>().Clear();
			Base.Caches<BlanketSOLine>().Clear();
			Base.Caches<BlanketSOLineSplit>().Clear();
			Base.Clear(PXClearOption.ClearQueriesOnly);
			ClearReturnReceivedAllocationsToBlanket();
		}

		protected virtual void ReturnReceivedAllocationsToBlanket(SOLine row)
		{
			var splits = Base.splits.Cache.Deleted.Cast<SOLineSplit>()
				.Where(s => s.LineNbr == row.LineNbr && s.OrderNbr == row.OrderNbr && s.OrderType == row.OrderType)
				.ToList();
			var origChildSplit = GetOrigChildSplit(row, splits);
			if (origChildSplit != null)
			{
				var graph = GetGraphForReturnReceivedAllocationsToBlanket();

				graph.Document.Current = graph.Document.Search<SOOrder.orderNbr>(row.BlanketNbr, row.BlanketType);
				graph.Transactions.Current = graph.Transactions.Search<SOLine.lineNbr>(row.BlanketLineNbr);
				using (graph.LineSplittingExt.SuppressedModeScope(true))
				{
					SOLineSplit origBlanketSplit = graph.splits.Search<SOLineSplit.splitLineNbr>(row.BlanketSplitLineNbr);
					if (origBlanketSplit == null)
					{
						throw new Common.Exceptions.RowNotFoundException(graph.splits.Cache,
							row.BlanketType, row.BlanketNbr, row.BlanketLineNbr, row.BlanketSplitLineNbr);
					}

					decimal? baseTotalReceivedQty = 0m;
					foreach (SOLineSplit splitReceivedOnChild in splits.Where(s => s.ParentSplitLineNbr == origChildSplit.SplitLineNbr))
					{
						SOLineSplit newSplit =  PXCache<SOLineSplit>.CreateCopy(splitReceivedOnChild);
						newSplit.OrderType = null;
						newSplit.OrderNbr = null;
						newSplit.LineNbr = null;
						newSplit.SplitLineNbr = null;
						newSplit.Behavior = null;
						newSplit.InvtMult = null;
						newSplit.ParentSplitLineNbr = origBlanketSplit.SplitLineNbr;
						newSplit.PlanID = null;
						newSplit.OrderDate = null;
						newSplit.SchedOrderDate = origBlanketSplit.SchedOrderDate;
						newSplit.SchedShipDate = origBlanketSplit.SchedShipDate;
						newSplit.POCreateDate = origBlanketSplit.POCreateDate;
						newSplit.CustomerOrderNbr = origBlanketSplit.CustomerOrderNbr;
						baseTotalReceivedQty += newSplit.BaseQty;
						newSplit = graph.splits.Insert(newSplit);
					}

					graph.splits.Current = origBlanketSplit;
					origBlanketSplit = PXCache<SOLineSplit>.CreateCopy(graph.splits.Current);
					origBlanketSplit.BaseReceivedQty += baseTotalReceivedQty;
					origBlanketSplit.ReceivedQty = INUnitAttribute.ConvertFromBase(graph.splits.Cache,
						origBlanketSplit.InventoryID,
						origBlanketSplit.UOM,
						(decimal)origBlanketSplit.BaseReceivedQty,
						INPrecision.QUANTITY);
					origBlanketSplit.Completed =
						(origBlanketSplit.ReceivedQty + origBlanketSplit.ShippedQty >= origBlanketSplit.Qty);
					origBlanketSplit = graph.splits.Update(origBlanketSplit);
				}
			}
		}

		private bool IsExpectingReturnAllocationsToBlanket(BlanketSOLineSplit split)
		{
			if (Base.Caches<BlanketSOLineSplit>().GetStatus(split) == PXEntryStatus.Updated)
			{
				foreach (SOLine deletedLine in Base.Transactions.Cache.Deleted.RowCast<SOLine>()
					.Where(l => l.BlanketType == split.OrderType && l.BlanketNbr == split.OrderNbr
						&& l.BlanketLineNbr == split.LineNbr && l.BlanketSplitLineNbr == split.SplitLineNbr))
				{
					var splits = Base.splits.Cache.Deleted.Cast<SOLineSplit>()
						.Where(s => s.LineNbr == deletedLine.LineNbr && s.OrderNbr == deletedLine.OrderNbr && s.OrderType == deletedLine.OrderType)
						.ToList();
					var origChildSplit = GetOrigChildSplit(deletedLine, splits);
					if (origChildSplit != null)
					{
						return true;
					}
				}
			}
			return false;
		}

		#endregion
	}
}
