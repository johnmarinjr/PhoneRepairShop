using PX.Common;
using PX.Data;
using PX.Objects.Common.Exceptions;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.SO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using IQtyAllocated = PX.Objects.IN.Overrides.INDocumentRelease.IQtyAllocated;
using SiteLotSerial = PX.Objects.IN.Overrides.INDocumentRelease.SiteLotSerial;
using SiteStatus = PX.Objects.IN.Overrides.INDocumentRelease.SiteStatus;

namespace PX.Objects.FS
{
	public class FSSODetSplitPlanIDAttribute : INItemPlanIDAttribute
	{
		#region State
		protected Type _ParentOrderDate;
		#endregion
		#region Ctor
		public FSSODetSplitPlanIDAttribute(Type parentNoteID, Type parentHoldEntry, Type parentOrderDate)
			: base(parentNoteID, parentHoldEntry)
		{
			_ParentOrderDate = parentOrderDate;
		}
		#endregion
		#region Implementation
		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);

			sender.Graph.FieldDefaulting.AddHandler<SiteStatus.negAvailQty>(SiteStatus_NegAvailQty_FieldDefaulting);
		}

		protected virtual void SiteStatus_NegAvailQty_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			SOOrderType ordertype = PXSetup<SOOrderType>.Select(sender.Graph);

			if (e.Cancel == false && ordertype != null && ordertype.RequireAllocation == true)
			{
				e.NewValue = false;
				e.Cancel = true;
			}
		}

		public override void Parent_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			base.Parent_RowUpdated(sender, e);

			PXView view;
			//WebDialogResult answer = sender.Graph.Views.TryGetValue("Document", out view) ? view.Answer : WebDialogResult.None;
			WebDialogResult answer = WebDialogResult.Yes;

			bool DatesUpdated = !sender.ObjectsEqual<FSServiceOrder.orderDate>(e.Row, e.OldRow) && (answer == WebDialogResult.Yes /*|| ((FSServiceOrder)e.Row).ShipComplete != SOShipComplete.BackOrderAllowed*/);
			bool RequestOnUpdated = !sender.ObjectsEqual<FSServiceOrder.orderDate>(e.Row, e.OldRow) && (answer == WebDialogResult.Yes /*|| ((FSServiceOrder)e.Row).ShipComplete != SOShipComplete.BackOrderAllowed*/);
			//bool CreditHoldApprovedUpdated = !sender.ObjectsEqual<FSServiceOrder.creditHold>(e.Row, e.OldRow) || !sender.ObjectsEqual<FSServiceOrder.approved>(e.Row, e.OldRow);
			bool CustomerUpdated = !sender.ObjectsEqual<FSServiceOrder.billCustomerID>(e.Row, e.OldRow);
			FSBillingCycle billingCycleRow = ((ServiceOrderEntry)sender.Graph).BillingCycleRelated.Current;

			var serviceOrder = (FSServiceOrder)e.Row;

			if (CustomerUpdated || DatesUpdated || RequestOnUpdated
				|| !sender.ObjectsEqual<FSServiceOrder.hold, FSServiceOrder.status>(e.Row, e.OldRow))
			{
				//DatesUpdated |= !sender.ObjectsEqual<FSServiceOrder.shipComplete>(e.Row, e.OldRow) && ((FSServiceOrder)e.Row).ShipComplete != SOShipComplete.BackOrderAllowed;
				//RequestOnUpdated |= !sender.ObjectsEqual<FSServiceOrder.shipComplete>(e.Row, e.OldRow) && ((FSServiceOrder)e.Row).ShipComplete != SOShipComplete.BackOrderAllowed;

				bool cancelled = (bool)sender.GetValue<FSServiceOrder.canceled>(e.Row);
				//bool? BackOrdered = (bool?)sender.GetValue<FSServiceOrder.backOrdered>(e.Row);

				PXCache plancache = sender.Graph.Caches[typeof(INItemPlan)];
				PXCache fsSODetCache = sender.Graph.Caches[typeof(FSSODet)];
				PXCache splitcache = sender.Graph.Caches[typeof(FSSODetSplit)];

				SOOrderType ordertype = PXSetup<SOOrderType>.Select(sender.Graph);

				var splitsByPlan = new Dictionary<long?, FSSODetSplit>();

				foreach (FSSODetSplit split in PXSelect<FSSODetSplit,
											   Where<
												   FSSODetSplit.srvOrdType, Equal<Current<FSServiceOrder.srvOrdType>>,
											   And<
												   FSSODetSplit.refNbr, Equal<Current<FSServiceOrder.refNbr>>>>>
											   .SelectMultiBound(sender.Graph, new[] { e.Row }))
				{

					FSSODet soDet = PXSelect<FSSODet,
											   Where<
												   FSSODet.srvOrdType, Equal<Required<FSSODet.srvOrdType>>,
											   And<
												   FSSODet.refNbr, Equal<Required<FSSODet.refNbr>>,
												And<
													FSSODet.lineNbr, Equal<Required<FSSODet.refNbr>>>>>>
											   .Select(sender.Graph, split.SrvOrdType, split.RefNbr, split.LineNbr);

					if (cancelled)
					{
						plancache.Inserted.RowCast<INItemPlan>()
										  .Where(_ => _.PlanID == split.PlanID)
										  .ForEach(_ => plancache.Delete(_));

						split.PlanID = null;
						split.Completed = true;

						splitcache.MarkUpdated(split);
					}
					else
					{
						if ((bool?)sender.GetValue<FSServiceOrder.canceled>(e.OldRow) == true)
						{
							if (string.IsNullOrEmpty(split.ShipmentNbr)
									&& split.POCompleted == false)
							{
								split.Completed = false;
							}

							INItemPlan planl = DefaultValues(splitcache, split);

							if (planl != null)
							{
								planl = (INItemPlan)sender.Graph.Caches[typeof(INItemPlan)].Insert(planl);
								split.PlanID = planl.PlanID;
							}

							splitcache.MarkUpdated(split);
						}

						if (DatesUpdated)
						{
							split.ShipDate = (DateTime?)sender.GetValue<FSServiceOrder.orderDate>(e.Row);
							splitcache.MarkUpdated(split);
						}

						if (split.PlanID != null)
						{
							splitsByPlan[split.PlanID] = split;
						}

						if ((bool?)sender.GetValue<FSServiceOrder.closed>(e.OldRow) == true)
						{
							if (string.IsNullOrEmpty(split.ShipmentNbr)
									&& split.POCompleted == false
									&& split.Completed == true
									&& split.LastModifiedByScreenID == ID.ScreenID.SERVICE_ORDER
									&& serviceOrder?.BillingBy == ID.Billing_By.APPOINTMENT)
							{
								soDet.BaseShippedQty -= split.BaseShippedQty;
								soDet.ShippedQty -= split.ShippedQty;
								soDet.OpenQty = soDet.OrderQty - soDet.ShippedQty;
								soDet.BaseOpenQty = soDet.BaseOrderQty - soDet.BaseShippedQty;
								soDet.ClosedQty = soDet.ShippedQty;
								soDet.BaseClosedQty = soDet.BaseShippedQty;

								fsSODetCache.MarkUpdated(soDet);

								split.Completed = false;
								split.ShippedQty = 0;

								INItemPlan plan = DefaultValues(splitcache, split);

								if (plan != null)
								{
									plan = (INItemPlan)sender.Graph.Caches[typeof(INItemPlan)].Insert(plan);
									split.PlanID = plan.PlanID;
								}

								splitcache.MarkUpdated(split);
							}
						}
					}
				}

				PXCache linecache = sender.Graph.Caches[typeof(FSSODet)];

				foreach (FSSODet line in PXSelect<FSSODet,
										 Where<
											 FSSODet.srvOrdType, Equal<Current<FSServiceOrder.srvOrdType>>,
										 And<
											 FSSODet.refNbr, Equal<Current<FSServiceOrder.refNbr>>>>>
										 .SelectMultiBound(sender.Graph, new[] { e.Row }))
				{
					if (cancelled)
					{
						FSSODet old_row = PXCache<FSSODet>.CreateCopy(line);
						//line.UnbilledQty -= line.OpenQty;
						line.OpenQty = 0m;
						//linecache.RaiseFieldUpdated<FSSODet.unbilledQty>(line, 0m);
						linecache.RaiseFieldUpdated<FSSODet.openQty>(line, 0m);

						line.Completed = true;
						this.ResetAvailabilityCounters(line);

						//SOOrderEntry_SOOrder_RowUpdated should execute later to correctly update balances
						//+++//TaxAttribute.Calculate<FSSODet.taxCategoryID>(linecache, new PXRowUpdatedEventArgs(line, old_row, false));

						linecache.MarkUpdated(line);
					}
					else
					{
						if ((bool?)sender.GetValue<FSServiceOrder.canceled>(e.OldRow) == true)
						{
							FSSODet old_row = PXCache<FSSODet>.CreateCopy(line);
							line.OpenQty = line.OrderQty;
							/*line.UnbilledQty += line.OpenQty;
                            object value = line.UnbilledQty;
                            linecache.RaiseFieldVerifying<FSSODet.unbilledQty>(line, ref value);
                            linecache.RaiseFieldUpdated<FSSODet.unbilledQty>(line, value);*/

							object value = line.OpenQty;
							linecache.RaiseFieldVerifying<FSSODet.openQty>(line, ref value);
							linecache.RaiseFieldUpdated<FSSODet.openQty>(line, value);

							line.Completed = false;

							//+++++//
							//TaxAttribute.Calculate<FSSODet.taxCategoryID>(linecache, new PXRowUpdatedEventArgs(line, old_row, false));

							linecache.MarkUpdated(line);
						}
						if (DatesUpdated)
						{
							line.ShipDate = (DateTime?)sender.GetValue<FSServiceOrder.orderDate>(e.Row);
							linecache.MarkUpdated(line);
						}
						/*if (RequestOnUpdated)
                        {
                            line.RequestDate = (DateTime?)sender.GetValue<FSServiceOrder.requestDate>(e.Row);
                            linecache.MarkUpdated(line);
                        }*/
						if (/*CreditHoldApprovedUpdated ||*/ !sender.ObjectsEqual<FSServiceOrder.hold>(e.Row, e.OldRow))
						{
							this.ResetAvailabilityCounters(line);
						}
					}
				}

				if (cancelled)
				{
					//PXFormulaAttribute.CalcAggregate<FSSODet.unbilledQty>(linecache, e.Row);
					PXFormulaAttribute.CalcAggregate<FSSODet.openQty>(linecache, e.Row);
				}

				PXSelectBase<INItemPlan> cmd = new PXSelect<INItemPlan, Where<INItemPlan.refNoteID, Equal<Current<FSServiceOrder.noteID>>>>(sender.Graph);

				//BackOrdered is tri-state
				/*if (BackOrdered == true && sender.GetValue<FSServiceOrder.lastSiteID>(e.Row) != null && sender.GetValue<FSServiceOrder.lastShipDate>(e.Row) != null)
                {
                    cmd.WhereAnd<Where<INItemPlan.siteID, Equal<Current<FSServiceOrder.lastSiteID>>, And<INItemPlan.planDate, LessEqual<Current<FSServiceOrder.lastShipDate>>>>>();
                }

                if (BackOrdered == false)
                {
                    sender.SetValue<FSServiceOrder.lastSiteID>(e.Row, null);
                    sender.SetValue<FSServiceOrder.lastShipDate>(e.Row, null);
                }*/

				foreach (INItemPlan plan in cmd.View.SelectMultiBound(new[] { e.Row }))
				{
					if (cancelled)
					{
						plancache.Delete(plan);
					}
					else
					{
						INItemPlan copy = PXCache<INItemPlan>.CreateCopy(plan);

						if (DatesUpdated)
						{
							plan.PlanDate = (DateTime?)sender.GetValue<FSServiceOrder.orderDate>(e.Row);
						}
						if (CustomerUpdated)
						{
							plan.BAccountID = (int?)sender.GetValue<FSServiceOrder.customerID>(e.Row);
						}
						plan.Hold = IsOrderOnHold((FSServiceOrder)e.Row);

						FSSODetSplit split;

						if (splitsByPlan.TryGetValue(plan.PlanID, out split))
						{
							plan.PlanType = CalcPlanType(sender, plan, (FSServiceOrder)e.Row, split/*, BackOrdered*/);

							if (!string.Equals(copy.PlanType, plan.PlanType))
							{
								plancache.RaiseRowUpdated(plan, copy);
							}
						}

						if (plancache.GetStatus(plan).IsIn(PXEntryStatus.Notchanged, PXEntryStatus.Held))
						{
							plancache.SetStatus(plan, PXEntryStatus.Updated);
						}
					}
				}
				// FSServiceOrder.BackOrdered value should be handled only single time and only in this method
				// sender.SetValue<FSServiceOrder.backOrdered>(e.Row, null);
			}
		}

		bool initPlan = false;
		bool initVendor = false;
		bool resetSupplyPlanID = false;

		public override void RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			//respond only to GUI operations
			var isLinked = IsLineLinked((FSSODetSplit)e.Row);

			initPlan = InitPlanRequired(sender, e) && !isLinked;

			FSSODet parent = (FSSODet)PXParentAttribute.SelectParent(sender, e.Row, typeof(FSSODet));

			initVendor = !sender.ObjectsEqual<FSSODetSplit.siteID, FSSODetSplit.subItemID, FSSODetSplit.vendorID, FSSODetSplit.pOCreate>(e.Row, e.OldRow) && !isLinked;

			initVendor = initVendor || parent.POVendorLocationID != null;

			resetSupplyPlanID = !isLinked;

			try
			{
				base.RowUpdated(sender, e);
			}
			finally
			{
				initPlan = false;
				resetSupplyPlanID = false;
			}
		}

		protected virtual bool InitPlanRequired(PXCache cache, PXRowUpdatedEventArgs e)
		{
			return !cache
				.ObjectsEqual<FSSODetSplit.isAllocated,
					FSSODetSplit.siteID,
					FSSODetSplit.pOCreate,
					FSSODetSplit.pOSource,
					FSSODetSplit.operation>(e.Row, e.OldRow);
		}

		protected virtual bool IsLineLinked(FSSODetSplit soLineSplit)
		{
			return soLineSplit != null && (soLineSplit.PONbr != null || soLineSplit.SOOrderNbr != null && soLineSplit.IsAllocated == true);
		}

		public override INItemPlan DefaultValues(PXCache sender, INItemPlan planRow, object origRow)
		{
			if (((FSSODetSplit)origRow).Completed == true || ((FSSODetSplit)origRow).POCompleted == true || ((FSSODetSplit)origRow).LineType == SOLineType.MiscCharge || ((FSSODetSplit)origRow).LineType == SOLineType.NonInventory && ((FSSODetSplit)origRow).RequireShipping == false)
			{
				return null;
			}

			FSSODet parent = (FSSODet)PXParentAttribute.SelectParent(sender, origRow, typeof(FSSODet));
			FSServiceOrder order = (FSServiceOrder)PXParentAttribute.SelectParent(sender, origRow, typeof(FSServiceOrder));

			FSSODetSplit split_Row = (FSSODetSplit)origRow;

			if (string.IsNullOrEmpty(planRow.PlanType) || initPlan)
			{
				planRow.PlanType = CalcPlanType(sender, planRow, order, split_Row);

				if (split_Row.POCreate == true)
				{
					planRow.FixedSource = INReplenishmentSource.Purchased;

					if (split_Row.POType != PO.POOrderType.Blanket && split_Row.POType != PO.POOrderType.DropShip && split_Row.POSource == INReplenishmentSource.PurchaseToOrder)
						planRow.SourceSiteID = split_Row.SiteID;
					else
						planRow.SourceSiteID = split_Row.SiteID;
				}
				else
				{
					planRow.Reverse = (split_Row.Operation == SOOperation.Receipt);
					planRow.FixedSource = (split_Row.SiteID != split_Row.ToSiteID ? INReplenishmentSource.Transfer : INReplenishmentSource.None);
					planRow.SourceSiteID = split_Row.SiteID;
				}
			}

			if (resetSupplyPlanID)
			{
				planRow.SupplyPlanID = null;
			}

			planRow.VendorID = split_Row.VendorID;

			if (initVendor || split_Row.POCreate == true && planRow.VendorID != null && planRow.VendorLocationID == null)
			{
				planRow.VendorLocationID = parent?.POVendorLocationID;

				if (planRow.VendorLocationID == null)
				{
					planRow.VendorLocationID = PO.POItemCostManager.FetchLocation(sender.Graph,
																				  split_Row.VendorID,
																				  split_Row.InventoryID,
																				  split_Row.SubItemID,
																				  split_Row.SiteID);
				}
			}

			planRow.BAccountID = parent == null ? null : parent.BillCustomerID;
			planRow.InventoryID = split_Row.InventoryID;
			planRow.SubItemID = split_Row.SubItemID;
			planRow.SiteID = split_Row.SiteID;
			planRow.LocationID = split_Row.LocationID;
			planRow.LotSerialNbr = split_Row.LotSerialNbr;

			if (string.IsNullOrEmpty(split_Row.AssignedNbr) == false && INLotSerialNbrAttribute.StringsEqual(split_Row.AssignedNbr, split_Row.LotSerialNbr))
			{
				planRow.LotSerialNbr = null;
			}

			planRow.PlanDate = split_Row.ShipDate;
			planRow.UOM = parent?.UOM;
			planRow.PlanQty = (split_Row.POCreate == true ? split_Row.BaseUnreceivedQty - split_Row.BaseShippedQty : split_Row.BaseQty);

			PXCache cache = sender.Graph.Caches[BqlCommand.GetItemType(_ParentNoteID)];
			planRow.RefNoteID = (Guid?)cache.GetValue(cache.Current, _ParentNoteID.Name);
			planRow.Hold = IsOrderOnHold(order);

			if (string.IsNullOrEmpty(planRow.PlanType))
			{
				return null;
			}

			return planRow;
		}

		protected virtual bool IsOrderOnHold(FSServiceOrder order)
		{
			return (order != null) && ((order.Hold ?? false)) /*|| (order.CreditHold ?? false) || (!order.Approved ?? false))*/;
		}

		protected virtual string CalcPlanType(PXCache sender, INItemPlan plan, FSServiceOrder order, FSSODetSplit split, bool? backOrdered = null)
		{
			if (split.POCreate == true)
			{
				return INPlanConstants.PlanF6;
			}

			SOOrderType ordertype = PXSetup<SOOrderType>.Select(sender.Graph);
			bool isAllocation = (split.IsAllocated == true) || INPlanConstants.IsAllocated(plan.PlanType) || INPlanConstants.IsFixed(plan.PlanType);
			bool isOrderOnHold = IsOrderOnHold(order) && ordertype.RequireAllocation != true;

			string calcedPlanType = CalcPlanType(plan, split, ordertype, isOrderOnHold);
			bool putOnSOPrepared = (calcedPlanType == INPlanConstants.PlanF0);

			if (!initPlan && !putOnSOPrepared && !isAllocation)
			{
				if (backOrdered == true || backOrdered == null && plan.PlanType == INPlanConstants.Plan68)
				{
					return INPlanConstants.Plan68;
				}
			}

			return calcedPlanType;
		}

		protected virtual string CalcPlanType(INItemPlan plan, FSSODetSplit split, SOOrderType ordertype, bool isOrderOnHold)
		{
			if (ordertype == null || ordertype.RequireShipping == true)
			{
				return (split.IsAllocated == true) ? split.AllocatedPlanType
					: isOrderOnHold ? INPlanConstants.PlanF0
					: (split.RequireAllocation != true || split.IsStockItem != true) ? split.PlanType : split.BackOrderPlanType;
			}
			else
			{
				return (isOrderOnHold != true || split.IsStockItem != true) ? split.PlanType : INPlanConstants.PlanF0;
			}
		}

		public virtual void ResetAvailabilityCounters(FSSODet row)
		{
			row.LineQtyAvail = null;
			row.LineQtyHardAvail = null;
		}
		#endregion
	}
}
