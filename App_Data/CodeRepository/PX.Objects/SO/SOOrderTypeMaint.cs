using System;
using System.Collections;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.IN;

namespace PX.Objects.SO
{
	public class SOOrderTypeMaint : PXGraph<SOOrderTypeMaint, SOOrderType>
	{
		public PXSelectJoin<SOOrderType, 
			LeftJoin<SOOrderTypeOperation, 
				On2<SOOrderTypeOperation.FK.OrderType, And<SOOrderTypeOperation.operation, Equal<SOOrderType.defaultOperation>>>>,
			Where2<Where2<Where<SOOrderTypeOperation.iNDocType, NotEqual<INTranType.transfer>, Or<FeatureInstalled<FeaturesSet.warehouse>>>, 
				And<Where<SOOrderType.requireAllocation, NotEqual<True>, Or<AllocationAllowed>>>>, 
				And<Where<SOOrderType.requireShipping, Equal<boolFalse>, Or<FeatureInstalled<FeaturesSet.inventory>>>>>> soordertype;
		public PXSelect<SOOrderType, Where<SOOrderType.orderType, Equal<Current<SOOrderType.orderType>>>> currentordertype;

		[PXCopyPasteHiddenView]
		public PXSelect<SOOrderTypeOperation,
			Where<SOOrderTypeOperation.orderType, Equal<Current<SOOrderType.orderType>>>>
			operations;

		[PXCopyPasteHiddenView]
		public PXSelect<SOOrderTypeOperation,
			Where<SOOrderTypeOperation.orderType, Equal<Optional<SOOrderType.orderType>>,
			And<SOOrderTypeOperation.operation, Equal<Optional<SOOrderType.defaultOperation>>>>>
			defaultOperation;

		[PXCopyPasteHiddenView]
		public PXSelect<SOOrderType,
			Where<SOOrderType.template, Equal<Required<SOOrderType.orderType>>,
				And<SOOrderType.orderType, NotEqual<SOOrderType.template>>>> references;

		[PXCopyPasteHiddenView]
		public PXSelect<SOQuickProcessParameters,
				Where<SOQuickProcessParameters.orderType, Equal<Current<SOOrderType.orderType>>>>
			quickProcessPreset;

		[PXCancelButton]
		[PXUIField(DisplayName = ActionsMessages.Cancel, MapEnableRights = PXCacheRights.Select)]
		protected new virtual IEnumerable Cancel(PXAdapter a)
		{
			if (a.Searches.Length == 1)
			{
				PXResult<SOOrderType> orderType = PXSelectJoin<SOOrderType, 
					LeftJoin<SOOrderTypeOperation, 
						On2<SOOrderTypeOperation.FK.OrderType, And<SOOrderTypeOperation.operation, Equal<SOOrderType.defaultOperation>>>>,
					Where<SOOrderType.requireShipping, Equal<boolFalse>, Or<FeatureInstalled<FeaturesSet.inventory>>>>.Search<SOOrderType.orderType>(this, a.Searches[0]);
				if (orderType != null && (!soordertype.View.BqlSelect.Meet(soordertype.Cache, (SOOrderType)orderType) || !soordertype.View.BqlSelect.Meet(operations.Cache, PXResult.Unwrap<SOOrderTypeOperation>(orderType))))
				{
					a.Searches[0] = null;
				}
			}
			return (new PXCancel<SOOrderType>(this, "Cancel")).Press(a);
		}

		#region Cache Attached

		[PXCustomizeBaseAttribute(typeof(PXDefaultAttribute), nameof(PXDefaultAttribute.Constant), false)]
		[PXFormula(typeof(False))]
		protected void SOQuickProcessParameters_HideWhenNothingToPrint_CacheAttached(PXCache sender) { }

		#endregion

		public SOOrderTypeMaint()
		{
			operations.Cache.AllowInsert = operations.Cache.AllowDelete = false;
			this.FieldVerifying.AddHandler<SOOrderType.salesSubMask>(SOOrderType_Mask_FieldVerifying);
			this.FieldVerifying.AddHandler<SOOrderType.miscSubMask>(SOOrderType_Mask_FieldVerifying);
			this.FieldVerifying.AddHandler<SOOrderType.freightSubMask>(SOOrderType_Mask_FieldVerifying);
			this.FieldVerifying.AddHandler<SOOrderType.discSubMask>(SOOrderType_Mask_FieldVerifying);
			//this.FieldVerifying.AddHandler<SOOrderType.cOGSSubMask>(SOOrderType_Mask_FieldVerifying);
		}

		protected virtual void SOOrderType_Mask_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			SOOrderType row = (SOOrderType)e.Row;
			if(row == null || ((row.Active != true || row.Behavior == SOBehavior.BL) && e.NewValue == null))
				e.Cancel = true;
		}

		protected virtual void SOOrderType_Template_FieldUpdating(PXCache sender, PXFieldUpdatingEventArgs e)
		{
			SOOrderType row = (SOOrderType)e.Row;
			if(row == null) return;
			if(sender.GetStatus(row) == PXEntryStatus.Inserted && row.OrderType == (string)e.NewValue)
				e.NewValue = null;
		}

		[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2022R1)]
		protected virtual void SOOrderType_RowSelecting(PXCache sender, PXRowSelectingEventArgs e)
		{
		}

		protected virtual void SOOrderType_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			if (e.Row == null)
			{
				return;
			}
			SOOrderType ordertype = (SOOrderType)e.Row;
			SOOrderType prevtype = (SOOrderType)e.OldRow;
			if(ordertype.Active == true)
			{
				if (prevtype.SalesSubMask == null && ordertype.SalesSubMask == null) sender.SetDefaultExt<SOOrderType.salesSubMask>(ordertype);
				if (prevtype.MiscSubMask == null && ordertype.MiscSubMask == null) sender.SetDefaultExt<SOOrderType.miscSubMask>(ordertype);
				if (prevtype.FreightSubMask == null && ordertype.FreightSubMask == null) sender.SetDefaultExt<SOOrderType.freightSubMask>(ordertype);
				if (prevtype.DiscSubMask == null && ordertype.DiscSubMask == null) sender.SetDefaultExt<SOOrderType.discSubMask>(ordertype);
				//if (prevtype.COGSSubMask == null && ordertype.COGSSubMask == null) sender.SetDefaultExt<SOOrderType.cOGSSubMask>(ordertype);
			}

			if (ordertype.CustomerOrderIsRequired == false)
			{
				ordertype.CustomerOrderValidation = CustomerOrderValidationType.None;
			}

			if (ordertype.Template != null && ordertype.Template != ordertype.OrderType &&
				ordertype.Template != ((SOOrderType)e.OldRow).Template)
			{
				SOOrderType template =
					PXSelect<SOOrderType, Where<SOOrderType.orderType, Equal<Required<SOOrderType.orderType>>>>
					.SelectWindowed(this, 0,1,ordertype.Template);
				this.soordertype.Current = ordertype;

				if(template == null) return;
				ordertype.Behavior = template.Behavior;
				ordertype.DefaultOperation = template.DefaultOperation;
				ordertype.RequireShipping = template.RequireShipping;
				ordertype.RequireAllocation = template.RequireAllocation;
				ordertype.ARDocType = template.ARDocType;
				ordertype.INDocType = template.INDocType;
				ordertype.ShipmentPlanType = template.ShipmentPlanType;
				ordertype.OrderPlanType = template.OrderPlanType;

				foreach (SOOrderTypeOperation o in this.operations.View.SelectMultiBound( new object[]{ordertype}))
				{
					this.operations.Delete(o);
				}

				foreach (SOOrderTypeOperation o in this.operations.View.SelectMultiBound(new object[] { template}))
				{
					SOOrderTypeOperation upd = new SOOrderTypeOperation();
					upd.OrderType = ordertype.OrderType;
					upd.Operation = o.Operation;
					upd.INDocType = o.INDocType;
					upd.ShipmentPlanType = o.ShipmentPlanType;
					upd.OrderPlanType = o.OrderPlanType;
					upd.AutoCreateIssueLine = o.AutoCreateIssueLine;
					upd.Active = o.Active;
					upd = this.operations.Insert(upd);
				}
			}

			if ((ordertype.Template == null || ordertype.Behavior == SOBehavior.BL) && sender.ObjectsEqual<SOOrderType.behavior, SOOrderType.aRDocType>(e.Row, e.OldRow) == false)
			{
				foreach (SOOrderTypeOperation o in this.operations.View.SelectMultiBound(new object[] { ordertype }))
				{
					this.operations.Delete(o);
				}
				string defaultOp = SOBehavior.DefaultOperation(ordertype.Behavior, ordertype.ARDocType);

				if (defaultOp != null)
				{
					ordertype.DefaultOperation = defaultOp;
					SOOrderTypeOperation def = new SOOrderTypeOperation();
					def.OrderType = ordertype.OrderType;
					def.Operation = ordertype.DefaultOperation;
					if (ordertype.Behavior == SOBehavior.BL)
					{
						def.INDocType = INTranType.NoUpdate;
					}
					this.operations.Insert(def);
					if (ordertype.Behavior == SOBehavior.RM)
					{
						def = new SOOrderTypeOperation();
						def.OrderType = ordertype.OrderType;
						def.Operation = SOOperation.Issue;
						this.operations.Insert(def);
					}
				}
			}

			if (sender.ObjectsEqual<SOOrderType.behavior>(e.Row, e.OldRow) == false && ordertype.Behavior == SOBehavior.BL)
			{
				ordertype.ARDocType = ARDocType.NoUpdate;
				ordertype.INDocType = INTranType.NoUpdate;
				ordertype.AllowQuickProcess = false;
				ordertype.RequireLotSerial = false;
				ordertype.CalculateFreight = false;
				ordertype.ShipFullIfNegQtyAllowed = false;
				ordertype.RecalculateDiscOnPartialShipment = false;
				ordertype.ShipSeparately = false;
				ordertype.CopyNotes = false;
				ordertype.CopyFiles = false;
				ordertype.CopyLineNotesToShipment = false;
				ordertype.CopyLineFilesToShipment = false;
				ordertype.CopyLineNotesToInvoice = false;
				ordertype.CopyLineFilesToInvoice = false;
				ordertype.CopyLineNotesToInvoiceOnlyNS = false;
				ordertype.CopyLineFilesToInvoiceOnlyNS = false;
			}

			if (sender.ObjectsEqual<SOOrderType.aRDocType, SOOrderType.requireShipping, SOOrderType.activeOperationsCntr>(ordertype, prevtype) == false)
			{
				if (ordertype.Behavior == SOBehavior.RM)
				{
					//ShippedNotInvoiced is available for RM when both Issue and Receipt operations are active
					if (ordertype.ActiveOperationsCntr < 2 && ordertype.ARDocType == ARDocType.NoUpdate || ordertype.RequireShipping != true)
						ordertype.UseShippedNotInvoiced = false;
				}
				else if (ordertype.ARDocType == ARDocType.NoUpdate || ordertype.RequireShipping != true)
					ordertype.UseShippedNotInvoiced = false;
			}

			sender.RaiseFieldDefaulting<SOOrderType.allowRefundBeforeReturn>(e.OldRow, out object oldDefaultValue);
			sender.RaiseFieldDefaulting<SOOrderType.allowRefundBeforeReturn>(e.Row, out object newDefaultValue);
			if (!newDefaultValue.Equals(oldDefaultValue))
			{
				ordertype.AllowRefundBeforeReturn = (bool?)newDefaultValue;
			}
			ordertype.CanHavePayments = (bool?)PXFormulaAttribute.Evaluate<SOOrderType.canHavePayments>(sender, ordertype);
			ordertype.CanHaveRefunds = (bool?)PXFormulaAttribute.Evaluate<SOOrderType.canHaveRefunds>(sender, ordertype);
		}

		protected virtual void SOOrderType_DefaultOperation_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			var orderType = e.Row as SOOrderType;
			if (orderType == null)
				return;

			SOOrderTypeOperation operation = defaultOperation.Select(orderType.OrderType, orderType.DefaultOperation);
			if (operation != null)
				sender.SetValueExt<SOOrderType.iNDocType>(orderType, operation.INDocType);
		}

		protected virtual void SOOrderTypeOperation_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			SOOrderTypeOperation row = (SOOrderTypeOperation)e.Row;
			SOOrderType ordertype = this.soordertype.Current;
			if(row == null || ordertype == null || row.Operation != ordertype.DefaultOperation) return;

			ordertype.INDocType = row.INDocType;
			ordertype.OrderPlanType = row.OrderPlanType;
			ordertype.ShipmentPlanType = row.ShipmentPlanType;

			if (row.INDocType == INTranType.NoUpdate)
			{
				row.OrderPlanType = null;
				row.ShipmentPlanType = null;
				ordertype.RequireShipping = false;
				ordertype.RequireAllocation = false;
			}
			if (row.INDocType == INTranType.Transfer)
			{
				ordertype.RequireShipping = true;
				ordertype.ARDocType = ARDocType.NoUpdate;
			}

			if (row.INDocType == INTranType.NoUpdate)
			{
				sender.SetValue<SOOrderTypeOperation.orderPlanType>(e.Row, null);
				sender.SetValue<SOOrderTypeOperation.shipmentPlanType>(e.Row, null);
			}

			if (row.INDocType == null || row.INDocType == INTranType.Transfer)
			{
				soordertype.Cache.SetValue<SOOrderType.customerOrderIsRequired>(ordertype, false);
				soordertype.Cache.SetValue<SOOrderType.customerOrderValidation>(ordertype, CustomerOrderValidationType.None);
			}
		}
		
		protected virtual void SOOrderTypeOperation_INDocType_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			SOOrderTypeOperation row = (SOOrderTypeOperation)e.Row;
			if (row == null)
				return;
			
			short? inInvtMult = INTranType.InvtMult((string)e.NewValue);
			if((row.Operation == SOOperation.Issue && inInvtMult > 0) ||
				(row.Operation == SOOperation.Receipt && inInvtMult < 0))
				throw new PXSetPropertyException(Messages.OrderTypeUnsupportedOperation);

			SOOrderType currentType = currentordertype.Current;
			if ((string)e.NewValue == INTranType.NoUpdate && currentType?.Behavior.IsIn(SOBehavior.SO, SOBehavior.TR, SOBehavior.RM) == true)
				throw new PXSetPropertyException(Messages.NoUpdateInTranTypeIsNotAppropriate);
		}

		protected virtual void SOOrderTypeOperation_InvtMult_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			SOOrderTypeOperation row = e.Row as SOOrderTypeOperation;

			if (row != null)
			{
				e.NewValue = INTranType.InvtMult(row.INDocType);
				e.Cancel = true;
			}
		}

		protected virtual void SOOrderTypeOperation_Active_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			SOOrderTypeOperation row = e.Row as SOOrderTypeOperation;

			if (row != null)
			{
				if (currentordertype.Current.Behavior == SOBehavior.RM || currentordertype.Current.Behavior == SOBehavior.IN)
				{
					if (row.Operation==SOOperation.Issue && row.Active==false)
					{
						foreach (SOOrderTypeOperation var in operations.Cache.Cached)
						{
							if (var.Operation == SOOperation.Receipt)
							{
								var.AutoCreateIssueLine = false;
								operations.Update(var);
								operations.View.RequestRefresh();
							}
						}
					}
				}
			}
		}

		protected virtual void SOOrderTypeOperation_INDocType_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			SOOrderTypeOperation row = e.Row as SOOrderTypeOperation;

			if (row != null && !PXAccess.FeatureInstalled<FeaturesSet.inventory>())
			{
				e.NewValue = INTranType.NoUpdate;
				e.Cancel = true;
			}
		}

		protected virtual void SOOrderType_INDocType_FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			SOOrderType row = e.Row as SOOrderType;

			if (row != null && !PXAccess.FeatureInstalled<FeaturesSet.inventory>())
			{
				e.NewValue = INTranType.NoUpdate;
				e.Cancel = true;
			}
		}

		protected virtual void SOOrderType_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			if (e.Row == null)
			{
				return;
			}
			SOOrderType ordertype = (SOOrderType)e.Row;

			SOOrderType link = references.SelectWindowed(0, 1, ordertype.OrderType);
			bool isTemplateUpdatable = ordertype.IsSystem == false && link == null && ordertype.OrderType != null;
			bool isBlanket = ordertype.Behavior == SOBehavior.BL;

			PXUIFieldAttribute.SetEnabled<SOOrderType.template>(sender, e.Row, isTemplateUpdatable);

			SOOrderTypeOperation def = this.defaultOperation.Select(ordertype.OrderType, ordertype.DefaultOperation);
			if (def == null) def = new SOOrderTypeOperation();
			PXUIFieldAttribute.SetEnabled<SOOrderType.billSeparately>(sender, e.Row, ordertype.ARDocType != ARDocType.NoUpdate);
			PXUIFieldAttribute.SetEnabled<SOOrderType.invoiceNumberingID>(sender, e.Row, ordertype.ARDocType != ARDocType.NoUpdate);


			if (ordertype.ARDocType == ARDocType.NoUpdate)
			{
				INTranType.CustomListAttribute listattr = new INTranType.SONonARListAttribute();
				PXStringListAttribute.SetList<SOOrderTypeOperation.iNDocType>(this.operations.Cache, null, listattr.AllowedValues, listattr.AllowedLabels);
			}
			else
			{
				INTranType.CustomListAttribute listattr = new INTranType.SOListAttribute();
				PXStringListAttribute.SetList<SOOrderTypeOperation.iNDocType>(this.operations.Cache, null, listattr.AllowedValues, listattr.AllowedLabels);
			}

			PXUIFieldAttribute.SetEnabled<SOOrderType.requireShipping>(sender, e.Row, def.INDocType != INTranType.NoUpdate && def.INDocType != INTranType.Transfer && isTemplateUpdatable && !SOBehavior.IsPredefinedBehavior(ordertype.Behavior));
			PXUIFieldAttribute.SetEnabled<SOOrderType.aRDocType>(sender, e.Row, def.INDocType != INTranType.Transfer && !isBlanket && isTemplateUpdatable);
			PXUIFieldAttribute.SetEnabled<SOOrderType.behavior>(sender, e.Row, isTemplateUpdatable);
			PXUIFieldAttribute.SetEnabled<SOOrderType.defaultOperation>(sender, e.Row, isTemplateUpdatable && ordertype.Behavior == SOBehavior.RM);
			PXUIFieldAttribute.SetVisible<SOOrderTypeOperation.active>(operations.Cache, null, ordertype.Behavior == SOBehavior.RM || ordertype.Behavior == SOBehavior.IN);
			PXUIFieldAttribute.SetVisible<SOOrderTypeOperation.autoCreateIssueLine>(operations.Cache, null, ordertype.Behavior == SOBehavior.RM);

			PXUIFieldAttribute.SetEnabled<SOOrderType.copyLineNotesToInvoiceOnlyNS>(sender, ordertype, ordertype.CopyLineNotesToInvoice == true);
			PXUIFieldAttribute.SetEnabled<SOOrderType.copyLineFilesToInvoiceOnlyNS>(sender, ordertype, ordertype.CopyLineFilesToInvoice == true);

			PXUIFieldAttribute.SetEnabled<SOOrderType.requireAllocation>(sender, ordertype, ordertype.RequireShipping == true || isBlanket);
			PXUIFieldAttribute.SetEnabled<SOOrderType.requireLotSerial>(sender, ordertype, ordertype.RequireShipping == true);
			PXUIFieldAttribute.SetEnabled<SOOrderType.copyLotSerialFromShipment>(sender, ordertype, ordertype.RequireShipping == true);

			PXUIFieldAttribute.SetEnabled<SOOrderType.useDiscountSubFromSalesSub>(sender, ordertype, ordertype.PostLineDiscSeparately == true);

			bool customerOrderEnabled = def.INDocType != null && def.INDocType != INTranType.Transfer;
			PXUIFieldAttribute.SetEnabled<SOOrderType.customerOrderIsRequired>(sender, ordertype, customerOrderEnabled);
			PXUIFieldAttribute.SetEnabled<SOOrderType.customerOrderValidation>(sender, ordertype, customerOrderEnabled && ordertype.CustomerOrderIsRequired == true);

			//ShippedNotInvoiced is available for RM when both Issue and Receipt operations are active even when ARDocType = NoUpdate;
			bool isShippedNotInvoicedAllowed = ordertype.Behavior == SOBehavior.RM ?
					(ordertype.ActiveOperationsCntr > 1 || ordertype.ARDocType != ARDocType.NoUpdate) && ordertype.RequireShipping == true :
					ordertype.ARDocType != ARDocType.NoUpdate && ordertype.RequireShipping == true;
			PXUIFieldAttribute.SetEnabled<SOOrderType.useShippedNotInvoiced>(sender, e.Row, isShippedNotInvoicedAllowed);
			PXUIFieldAttribute.SetEnabled<SOOrderType.shippedNotInvoicedAcctID>(sender, null, isShippedNotInvoicedAllowed && ordertype.UseShippedNotInvoiced == true);
			PXUIFieldAttribute.SetEnabled<SOOrderType.shippedNotInvoicedSubID>(sender, null, isShippedNotInvoicedAllowed && ordertype.UseShippedNotInvoiced == true);

			PXUIFieldAttribute.SetVisible<SOOrderType.recalculateDiscOnPartialShipment>(sender, e.Row, PXAccess.FeatureInstalled<FeaturesSet.inventory>() && PXAccess.FeatureInstalled<FeaturesSet.customerDiscounts>());

			bool allowUserDefinedOrderTypes = PXAccess.FeatureInstalled<FeaturesSet.userDefinedOrderTypes>();
			soordertype.AllowInsert = allowUserDefinedOrderTypes;
			currentordertype.AllowInsert = allowUserDefinedOrderTypes;
			bool allowUpdateOrderTypes = allowUserDefinedOrderTypes || sender.GetStatus(e.Row) != PXEntryStatus.Inserted;
			soordertype.AllowUpdate = allowUpdateOrderTypes;
			currentordertype.AllowUpdate = allowUpdateOrderTypes;

			if (!PXAccess.FeatureInstalled<FeaturesSet.inventory>())
			{
				PXDefaultAttribute.SetDefault<SOOrderType.behavior>(sender, SOBehavior.IN);
				PXStringListAttribute.SetList<SOOrderType.behavior>(sender, null, new string[] { SOBehavior.IN, SOBehavior.QT, SOBehavior.CM },
					new string[] { Messages.INName, Messages.QTName, Messages.CMName });
			}

			bool isTransfer = (ordertype.ARDocType == ARDocType.NoUpdate && def.INDocType == INTranType.Transfer);
			bool isQuote = (ordertype.Behavior == SOBehavior.QT);
			sender.Adjust<PXUIFieldAttribute>(e.Row)
				.For<SOOrderType.intercompanySalesAcctDefault>(a =>
				{
					a.Visible = a.Enabled = !isTransfer && !isQuote;
				})
				.SameFor<SOOrderType.intercompanyCOGSAcctDefault>();

			if (isBlanket)
			{
				sender.Adjust<PXUIFieldAttribute>(e.Row)
					//Order settings
					.For<SOOrderType.creditHoldEntry>(a =>
					{
						a.Visible = a.Enabled = false;
					})
					.SameFor<SOOrderType.daysToKeep>()
					.SameFor<SOOrderType.billSeparately>()
					.SameFor<SOOrderType.shipSeparately>()
					.SameFor<SOOrderType.calculateFreight>()
					.SameFor<SOOrderType.shipFullIfNegQtyAllowed>()
					.SameFor<SOOrderType.disableAutomaticDiscountCalculation>()
					.SameFor<SOOrderType.recalculateDiscOnPartialShipment>()
					.SameFor<SOOrderType.allowRefundBeforeReturn>()
					.SameFor<SOOrderType.copyNotes>()
					.SameFor<SOOrderType.copyFiles>()
					.SameFor<SOOrderType.copyLineNotesToShipment>()
					.SameFor<SOOrderType.copyLineFilesToShipment>()
					.SameFor<SOOrderType.copyLineNotesToInvoice>()
					.SameFor<SOOrderType.copyLineNotesToInvoiceOnlyNS>()
					.SameFor<SOOrderType.copyLineFilesToInvoice>()
					.SameFor<SOOrderType.copyLineFilesToInvoiceOnlyNS>()
					//Accounts Receipvable Settings
					.SameFor<SOOrderType.invoiceNumberingID>()
					.SameFor<SOOrderType.markInvoicePrinted>()
					.SameFor<SOOrderType.markInvoiceEmailed>()
					.SameFor<SOOrderType.invoiceHoldEntry>()
					.SameFor<SOOrderType.useCuryRateFromSO>()
					//Posting Settings
					.SameFor<SOOrderType.salesAcctDefault>()
					.SameFor<SOOrderType.salesSubMask>()
					.SameFor<SOOrderType.freightAcctID>()
					.SameFor<SOOrderType.freightAcctDefault>()
					.SameFor<SOOrderType.freightSubID>()
					.SameFor<SOOrderType.freightSubMask>()
					.SameFor<SOOrderType.discountAcctID>()
					.SameFor<SOOrderType.discAcctDefault>()
					.SameFor<SOOrderType.discountSubID>()
					.SameFor<SOOrderType.discSubMask>()
					.SameFor<SOOrderType.postLineDiscSeparately>()
					.SameFor<SOOrderType.useDiscountSubFromSalesSub>()
					.SameFor<SOOrderType.autoWriteOff>()
					.SameFor<SOOrderType.useShippedNotInvoiced>()
					.SameFor<SOOrderType.shippedNotInvoicedAcctID>()
					.SameFor<SOOrderType.shippedNotInvoicedSubID>()
					//Intercompany Posting Settings
					.SameFor<SOOrderType.intercompanySalesAcctDefault>()
					.SameFor<SOOrderType.intercompanyCOGSAcctDefault>();
			}

			sender.Adjust<PXUIFieldAttribute>(e.Row)
				.For<SOOrderType.copyLineNotesToChildOrder>(a =>
				{
					a.Visible = a.Enabled = isBlanket;
				})
				.SameFor<SOOrderType.copyLineFilesToChildOrder>()
				.SameFor<SOOrderType.dfltChildOrderType>();

			sender.RaiseFieldDefaulting<SOOrderType.allowRefundBeforeReturn>(e.Row, out object defaultValue);
			PXUIFieldAttribute.SetEnabled<SOOrderType.allowRefundBeforeReturn>(sender, ordertype, (bool?)defaultValue == true);
		}

		protected virtual void SOOrderType_CopyLineNotesToInvoice_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			SOOrderType row = e.Row as SOOrderType;
			if (row != null)
			{
				if (row.CopyLineNotesToInvoice != true)
					row.CopyLineNotesToInvoiceOnlyNS = false;
			}
		}

		protected virtual void SOOrderType_CopyLineFilesToInvoice_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			SOOrderType row = e.Row as SOOrderType;
			if (row != null)
			{
				if (row.CopyLineFilesToInvoice != true)
					row.CopyLineFilesToInvoiceOnlyNS = false;
			}
		}

		protected virtual void SOOrderType_RequireShipping_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			SOOrderType row = e.Row as SOOrderType;
			if (row == null) return;
			if (row.RequireShipping == true)
			{
				row.RequireLotSerial = false;
			}
			else
			{
				row.RequireAllocation = false;
				row.RequireLotSerial = (row.INDocType != INTranType.NoUpdate);
				row.CopyLotSerialFromShipment = false;
			}
		}

		protected virtual void SOOrderType_PostLineDiscSeparately_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			SOOrderType row = e.Row as SOOrderType;
			if (row != null && row.PostLineDiscSeparately != true)
				row.UseDiscountSubFromSalesSub = false;
		}

		/// <summary><see cref="SOOrderType.AllowQuickProcess"/> Updated</summary>
		protected virtual void SOOrderType_AllowQuickProcess_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			var row = (SOOrderType) e.Row;
			if (row != null && row.AllowQuickProcess == true && quickProcessPreset.Current == null)
				quickProcessPreset.Insert();
		}

		protected virtual void SOOrderTypeOperation_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			SOOrderType ordertype = this.soordertype.Current;
			SOOrderTypeOperation operation = (SOOrderTypeOperation)e.Row;

			if(ordertype == null || operation == null) return;
			bool isBlanket = ordertype.Behavior == SOBehavior.BL;

			SOOrderType link = references.SelectWindowed(0, 1, ordertype.OrderType);
			bool isTemplateUpdatable = ordertype.IsSystem == false && link == null && ordertype.OrderType != null && !isBlanket;

			PXUIFieldAttribute.SetEnabled<SOOrderTypeOperation.active>(sender, e.Row, operation.Operation != ordertype.DefaultOperation);
			PXUIFieldAttribute.SetEnabled<SOOrderTypeOperation.iNDocType>(sender, e.Row, isTemplateUpdatable);
			PXUIFieldAttribute.SetEnabled<SOOrderTypeOperation.shipmentPlanType>(sender, e.Row, operation.INDocType != INTranType.NoUpdate && (bool)ordertype.RequireShipping && isTemplateUpdatable);
			PXUIFieldAttribute.SetEnabled<SOOrderTypeOperation.orderPlanType>(sender, e.Row, operation.INDocType != INTranType.NoUpdate && isTemplateUpdatable);
			PXUIFieldAttribute.SetEnabled<SOOrderTypeOperation.autoCreateIssueLine>(sender, e.Row, 
				ordertype.Behavior == SOBehavior.RM && operation.Operation == SOOperation.Receipt && isTemplateUpdatable);
			PXUIFieldAttribute.SetEnabled<SOOrderTypeOperation.requireReasonCode>(sender, e.Row, !isBlanket);
		}

		protected virtual void Validate(PXCache sender, SOOrderType row)
		{
			SOOrderTypeOperation def = this.defaultOperation.Select(row.OrderType, row.DefaultOperation);
			if(def == null) return;

			short? arInvtMult = 0;
			short? inInvtMult = INTranType.InvtMult(def.INDocType);
			switch (row.ARDocType)
			{
				case ARDocType.Invoice:
				case ARDocType.DebitMemo:
				case ARDocType.CashSale:
					arInvtMult = -1;
					break;
				case ARDocType.CreditMemo:
					arInvtMult = 1;
					break;
			}

			if (row.Behavior != SOBehavior.RM && inInvtMult != arInvtMult && inInvtMult != 0 && arInvtMult != 0)
			{
				PXException ex = new PXSetPropertyException(Messages.OrderTypeUnsupportedCombination);
				sender.RaiseExceptionHandling<SOOrderType.aRDocType>(row, row.ARDocType, ex);
				this.operations.Cache.RaiseExceptionHandling<SOOrderTypeOperation.iNDocType>(def, def.INDocType, ex);
			}

			if (row.INDocType != def.INDocType)
			{
				PXException ex = new PXSetPropertyException(Messages.OrderTypeUnsupportedOperation);
				this.operations.Cache.RaiseExceptionHandling<SOOrderTypeOperation.iNDocType>(def, def.INDocType, ex);
			}
		}

		protected virtual void SOOrderType_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			SOOrderType ordertype = e.Row as SOOrderType;
			if (ordertype == null)
			{
				return;
			}

			bool isBlanketOrderType = ordertype.Behavior == SOBehavior.BL;
			PXDefaultAttribute.SetPersistingCheck<SOOrderType.dfltChildOrderType>(sender, ordertype, isBlanketOrderType ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
			if (ordertype.UseShippedNotInvoiced == true && ordertype.ShippedNotInvoicedAcctID == null)
				throw new PXRowPersistingException(typeof(SOOrderType.shippedNotInvoicedAcctID).Name, null, ErrorMessages.FieldIsEmpty, typeof(SOOrderType.shippedNotInvoicedAcctID).Name);
			if (ordertype.UseShippedNotInvoiced == true && ordertype.ShippedNotInvoicedSubID == null && PXAccess.FeatureInstalled<FeaturesSet.subAccount>())
				throw new PXRowPersistingException(typeof(SOOrderType.shippedNotInvoicedSubID).Name, null, ErrorMessages.FieldIsEmpty, typeof(SOOrderType.shippedNotInvoicedSubID).Name);

			if (e.Operation == PXDBOperation.Insert || e.Operation == PXDBOperation.Update)
			{
				Validate(sender, ordertype);
			}
		}

		protected virtual void SOOrderType_RowDeleting(PXCache sender, PXRowDeletingEventArgs e)
		{
			SOOrderType ordertype = (SOOrderType)e.Row;

			SOOrderType link = references.SelectWindowed(0, 1, ordertype.OrderType);
			if (link != null)
			{
				throw new PXSetPropertyException(Messages.CannotDeleteTemplateOrderType, link.OrderType);
			}

			if (ordertype.Behavior == SOBehavior.SO)
			{
				SOOrderType parentOrderType = PXSelect<SOOrderType,
					Where<SOOrderType.dfltChildOrderType.IsEqual<SOOrderType.orderType.AsOptional>>>.Select(this, ordertype.OrderType);

				if (parentOrderType != null)
				{
					throw new PXSetPropertyException(Messages.TheOrderTypeIsDefaultChildOrderType, ordertype.OrderType, parentOrderType.OrderType);
				}
			}

			SOLine soTran = PXSelectReadonly<SOLine, Where<SOLine.orderType, Equal<Required<SOOrderType.orderType>>>>.SelectWindowed(this, 0, 1, ordertype.OrderType);
			if (soTran != null)
			{
				throw new PXSetPropertyException(Messages.CannotDeleteOrderType);
			}
		}

		public override void Persist()
		{
			var modifiedCurrentTypes = currentordertype.Cache.Inserted.RowCast<SOOrderType>().Union(currentordertype.Cache.Updated.RowCast<SOOrderType>());
			foreach (SOOrderType orderType in modifiedCurrentTypes)
			{
				foreach (SOOrderTypeOperation operation in operations.View.SelectMultiBound(new[] { orderType }))
				{
					if (operation.INDocType == INTranType.NoUpdate && orderType.Behavior.IsIn(SOBehavior.SO, SOBehavior.TR, SOBehavior.RM) == true)
					{
						string behavior = PXStringListAttribute.GetLocalizedLabel<SOOrderType.behavior>(currentordertype.Cache, orderType);
						throw new PXRowPersistingException(typeof(SOOrderTypeOperation.iNDocType).Name, null, Messages.BehaviorChangedAndNoUpdateInTranTypeIsNotAppropriate, behavior);
					}
				}
			}

			base.Persist();
		}
	}
}
