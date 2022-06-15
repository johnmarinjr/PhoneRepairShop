using System;
using System.Collections.Generic;
using System.Linq;

using PX.Common;
using PX.Data;
using PX.Data.BQL;

using PX.Objects.AR;
using PX.Objects.CA;
using PX.Objects.CM;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.GL.FinPeriods.TableDefinition;
using PX.Objects.IN;
using PX.Objects.TX;

using SiteStatus = PX.Objects.IN.Overrides.INDocumentRelease.SiteStatus;

namespace PX.Objects.SO
{
	public class Round<Field, CuryKeyField> : BqlFormulaEvaluator<Field, CuryKeyField>, IBqlOperand
		where Field : IBqlOperand
		where CuryKeyField : IBqlField
	{
		public override object Evaluate(PXCache cache, object item, Dictionary<Type, object> pars)
		{
			decimal? val = (decimal?)pars[typeof(Field)];
			return val != null ? (object) PXDBCurrencyAttribute.RoundCury<CuryKeyField>(cache, item, (decimal) val) : null;
		}
	}

	public static class Constants
	{
		public const string NoShipmentNbr = "<NEW>";

		public class noShipmentNbr : PX.Data.BQL.BqlString.Constant<noShipmentNbr>
		{
			public noShipmentNbr() : base(NoShipmentNbr) { ;}
		}

		public const int MaxNumberOfPaymentsAndMemos = 200;
	}

	public class SOSetupSelect : PXSetupSelect<SOSetup>
	{
		public SOSetupSelect(PXGraph graph)
			:base(graph)
		{
		}

		protected override void FillDefaultValues(SOSetup record)
		{
			record.MinGrossProfitValidation = MinGrossProfitValidationType.Warning;
		}
	}

	public class OrderSiteSelectorAttribute : PXSelectorAttribute
	{
		protected string _InputMask = null;

		public OrderSiteSelectorAttribute()
			: base(typeof(Search2<SOOrderSite.siteID, 
				InnerJoin<INSite, 
					On<SOOrderSite.FK.Site>>,
				Where<SOOrderSite.orderType, Equal<Current<SOOrder.orderType>>, And<SOOrderSite.orderNbr, Equal<Current<SOOrder.orderNbr>>, 
				And<Match<INSite, Current<AccessInfo.userName>>>>>>),
				typeof(INSite.siteCD), typeof(INSite.descr), typeof(INSite.replenishmentClassID)
			)
		{
			this.DirtyRead = true;
			this.SubstituteKey = typeof(INSite.siteCD);
			this.DescriptionField = typeof(INSite.descr);
			this._UnconditionalSelect = BqlCommand.CreateInstance(typeof(Search<INSite.siteID, Where<INSite.siteID, Equal<Required<INSite.siteID>>>>));
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);

			PXDimensionAttribute attr = new PXDimensionAttribute(SiteAttribute.DimensionName);
			attr.CacheAttached(sender);
			attr.FieldName = _FieldName;
			PXFieldSelectingEventArgs e = new PXFieldSelectingEventArgs(null, null, true, false);
			attr.FieldSelecting(sender, e);

			_InputMask = ((PXSegmentedState)e.ReturnState).InputMask;
		}

		public override void SubstituteKeyFieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			base.SubstituteKeyFieldSelecting(sender, e);
			if (_AttributeLevel == PXAttributeLevel.Item || e.IsAltered)
			{
				e.ReturnState = PXStringState.CreateInstance(e.ReturnState, null, null, null, null, null, _InputMask, null, null, null, null);
			}
		}
	}

	/// <summary>
	/// Specialized for SOInvoice version of the InvoiceNbrAttribute.<br/>
	/// The main purpose of the attribute is poviding of the uniqueness of the RefNbr <br/>
	/// amoung  a set of  documents of the specifyed types (for example, each RefNbr of the ARInvoice <br/>
	/// the ARInvoices must be unique across all ARInvoices and AR Debit memos)<br/>
	/// This may be useful, if user has configured a manual numberin for SOInvoices  <br/>
	/// or needs  to create SOInvoice from another document (like SOOrder) allowing to type RefNbr <br/>
	/// for the to-be-created Invoice manually. To store the numbers, system using ARInvoiceNbr table, <br/>
	/// keyed uniquelly by DocType and RefNbr. A source document is linked to a number by NoteID.<br/>
	/// Attributes checks a number for uniqueness on FieldVerifying and RowPersisting events.<br/>
	/// </summary>
	public class SOInvoiceNbrAttribute : InvoiceNbrAttribute
	{
		public SOInvoiceNbrAttribute()
			: base(typeof(SOOrder.aRDocType), typeof(SOOrder.noteID))
		{
		}

		protected override bool DeleteOnUpdate(PXCache sender, PXRowPersistedEventArgs e)
		{
			return base.DeleteOnUpdate(sender, e) || (bool?)sender.GetValue<SOOrder.cancelled>(e.Row) == true;
		}
	}

	/// <summary>
	/// Automatically tracks and Updates Cash discounts in accordance with Customer's Credit Terms. 
	/// </summary>
	public class SOInvoiceTermsAttribute : TermsAttribute
	{
		public SOInvoiceTermsAttribute()
			: base(typeof(ARInvoice.docDate), typeof(ARInvoice.dueDate), typeof(ARInvoice.discDate), null, null)
		{ 
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);

			SubscribeCalcDisc(sender);
			sender.Graph.FieldVerifying.AddHandler(typeof(ARInvoice), typeof(ARInvoice.curyOrigDiscAmt).Name, VerifyDiscount<ARInvoice.curyOrigDocAmt>);
			sender.Graph.FieldVerifying.AddHandler(typeof(ARInvoice), typeof(ARInvoice.curyOrigDiscAmt).Name, VerifyDiscount<ARInvoice.curyDocBal>);

			_CuryDiscBal = typeof(ARInvoice.curyOrigDiscAmt);
		}

		public override void FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			base.FieldUpdated(sender, e);
			CalcDisc_CuryOrigDocAmt(sender, e);
			CalcDisc_CuryDocBal(sender, e);
		}

		protected override void UnsubscribeCalcDisc(PXCache sender)
		{
			sender.Graph.FieldUpdated.RemoveHandler(typeof(ARInvoice), typeof(ARInvoice.curyOrigDocAmt).Name, CalcDisc_CuryOrigDocAmt);
			sender.Graph.FieldUpdated.RemoveHandler(typeof(ARInvoice), typeof(ARInvoice.curyDocBal).Name, CalcDisc_CuryDocBal);
		}

		protected override void SubscribeCalcDisc(PXCache sender)
			{
			sender.Graph.FieldUpdated.AddHandler(typeof(ARInvoice), typeof(ARInvoice.curyOrigDocAmt).Name, CalcDisc_CuryOrigDocAmt);
			sender.Graph.FieldUpdated.AddHandler(typeof(ARInvoice), typeof(ARInvoice.curyDocBal).Name, CalcDisc_CuryDocBal);
		}

		public void CalcDisc_CuryOrigDocAmt(PXCache sender, PXFieldUpdatedEventArgs e)
		{
            if (((ARInvoice)e.Row).DocType != ARDocType.CashSale && ((ARInvoice)e.Row).DocType != ARDocType.CashReturn)
			{
			_CuryDocBal = typeof(ARInvoice.curyOrigDocAmt);
			}

			try
			{
				base.CalcDisc(sender, e);
			}
			finally
			{
				_CuryDocBal = null;
			}
			}

		public void CalcDisc_CuryDocBal(PXCache sender, PXFieldUpdatedEventArgs e)
		{
            if (((ARInvoice)e.Row).DocType == ARDocType.CashSale || ((ARInvoice)e.Row).DocType == ARDocType.CashReturn)
			{
			_CuryDocBal = typeof(ARInvoice.curyDocBal);
			}

			try
			{
				base.CalcDisc(sender, e);
			}
			finally
			{
				_CuryDocBal = null;
			}
		}

		public void VerifyDiscount<Field>(PXCache sender, PXFieldVerifyingEventArgs e)
			where Field : IBqlField
		{
			if (((ARInvoice)e.Row).DocType == ARDocType.CashSale && typeof(Field) == typeof(ARInvoice.curyDocBal) ||
				((ARInvoice)e.Row).DocType != ARDocType.CashSale && typeof(Field) == typeof(ARInvoice.curyOrigDocAmt))
			{
				_CuryDocBal = typeof(Field);
			}

			try
			{
				base.VerifyDiscount(sender, e);
			}
			finally
			{
				_CuryDocBal = null;
			}
		}
	}


	public class SOShipExpireDateAttribute : INExpireDateAttribute
	{
		public SOShipExpireDateAttribute(Type InventoryType)
			:base(InventoryType)
		{ 
		}

		public override void RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			PXResult<InventoryItem, INLotSerClass> item = ReadInventoryItem(sender, ((ILSMaster)e.Row).InventoryID);
			if (item == null) return;

			if (((INLotSerClass) item).LotSerTrack != INLotSerTrack.NotNumbered)
			{
				bool? isConfirmed = (bool?) sender.GetValue<SOShipLineSplit.confirmed>(e.Row);
				if (((INLotSerClass)item).LotSerAssign != INLotSerAssign.WhenUsed || isConfirmed == true)
			{
				base.RowPersisting(sender, e);
				}
			}
		}
	}

	public class DirtyFormulaAttribute : PXAggregateAttribute, IPXRowInsertedSubscriber, IPXRowUpdatedSubscriber
	{
		protected Dictionary<object, object> inserted = null;
		protected Dictionary<object, object> updated = null;

		public DirtyFormulaAttribute(Type formulaType, Type aggregateType)
		{
			this._Attributes.Add(new PXFormulaAttribute(formulaType, aggregateType));
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			inserted = new Dictionary<object, object>();
			updated = new Dictionary<object, object>();
		}

		public virtual void RowInserted(PXCache sender, PXRowInsertedEventArgs e)
		{
			object copy;
			if (!inserted.TryGetValue(e.Row, out copy))
			{
				inserted[e.Row] = sender.CreateCopy(e.Row);
			} 
		}

		public virtual void RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			object copy;
			if (!updated.TryGetValue(e.Row, out copy))
			{
				updated[e.Row] = sender.CreateCopy(e.Row);
			}
		}

		public static void RaiseRowInserted<Field>(PXCache sender, PXRowInsertedEventArgs e)
			where Field : IBqlField
		{
			foreach (PXEventSubscriberAttribute attr in sender.GetAttributes<Field>(e.Row))
			{
				if (attr is DirtyFormulaAttribute)
				{
					object copy;
					if (((DirtyFormulaAttribute)attr).inserted.TryGetValue(e.Row, out copy))
					{
						List<IPXRowUpdatedSubscriber> subs = new List<IPXRowUpdatedSubscriber>();
						((PXAggregateAttribute)attr).GetSubscriber<IPXRowUpdatedSubscriber>(subs);
						foreach (IPXRowUpdatedSubscriber ru in subs)
						{
							ru.RowUpdated(sender, new PXRowUpdatedEventArgs(e.Row, copy, false));
						}
						((DirtyFormulaAttribute)attr).inserted.Remove(e.Row);
					}
				}
			}
		}

		public static void RaiseRowUpdated<Field>(PXCache sender, PXRowUpdatedEventArgs e)
			where Field : IBqlField
		{
			foreach (PXEventSubscriberAttribute attr in sender.GetAttributes<Field>(e.Row))
			{
				if (attr is DirtyFormulaAttribute)
				{
					object copy;
					if (((DirtyFormulaAttribute)attr).updated.TryGetValue(e.Row, out copy))
					{
						List<IPXRowUpdatedSubscriber> subs = new List<IPXRowUpdatedSubscriber>();
						((PXAggregateAttribute)attr).GetSubscriber<IPXRowUpdatedSubscriber>(subs);
						foreach (IPXRowUpdatedSubscriber ru in subs)
						{
							ru.RowUpdated(sender, new PXRowUpdatedEventArgs(e.Row, copy, false));
						}
						((DirtyFormulaAttribute)attr).updated.Remove(e.Row);
					}
				}
			}
		}
	}

	public sealed class OpenLineCalc<Field> : IBqlAggregateCalculator
		where Field : IBqlField
	{
		#region IBqlAggregateCalculator Members

		public object Calculate(PXCache cache, object row, object oldrow, int fieldordinal, int digit)
		{
			if (object.ReferenceEquals(row, oldrow))
			{
				return null;
			}

			bool? newOpenLine = (bool?)cache.GetValue<SOLine.openLine>(row);
			bool? oldOpenLine = (bool?)cache.GetValue<SOLine.openLine>(oldrow);
			bool? newIsFree = (bool?)cache.GetValue<SOLine.isFree>(row);
			bool? oldIsFree = (bool?)cache.GetValue<SOLine.isFree>(oldrow);
			bool? newManualDisc = (bool?)cache.GetValue<SOLine.manualDisc>(row);
			bool? oldManualDisc = (bool?)cache.GetValue<SOLine.manualDisc>(oldrow);

			if (row != null && newOpenLine == true && (newIsFree != true || newManualDisc == true) 
				&& (oldrow == null || oldOpenLine != true || oldIsFree == true && oldManualDisc == false))
			{
				return 1;
			}

			if (oldrow != null && oldOpenLine == true && (oldIsFree != true || oldManualDisc == true) 
				&& (row == null || newOpenLine != true || newIsFree == true && newManualDisc == false))
			{
				return -1;
			}

			return 0;
		}

		public object Calculate(PXCache cache, object row, int fieldordinal, object[] records, int digit)
		{
			short count = (short)records.Count(r
				=> (bool?)cache.GetValue<SOLine.openLine>(r) == true
				&& ((bool?)cache.GetValue<SOLine.isFree>(r) != true
					|| (bool?)cache.GetValue<SOLine.manualDisc>(r) == true));
			return count;
		}

		#endregion
	}

	public class DateMinusDaysNotLessThenDate<V1, V2, V3> : IBqlCreator
		where V1 : IBqlOperand
		where V2 : IBqlOperand
		where V3 : IBqlOperand
	{
		IBqlCreator _formula = new Switch<Case<Where<Sub<V1,V2>, Less<V3>, Or<Sub<V1,V2>, IsNull>>, V3>, Sub<V1,V2>>();

		public bool AppendExpression(ref PX.Data.SQLTree.SQLExpression exp, PXGraph graph, BqlCommandInfo info, BqlCommand.Selection selection)
			=> _formula.AppendExpression(ref exp, graph, info, selection);

		public void Verify(PXCache cache, object item, List<object> pars, ref bool? result, ref object value)
		{
			_formula.Verify(cache, item, pars, ref result, ref value);
		}	
	}

	public interface IFreightBase
	{
		string ShipTermsID { get; }
		string ShipVia { get; }
		string ShipZoneID { get; }
		decimal? LineTotal { get; }
		decimal? OrderWeight { get; }
		decimal? PackageWeight { get; }
		decimal? OrderVolume { get; }
		decimal? FreightAmt { get; set;}
		decimal? FreightCost { get; set; }
	}

	/// <summary>
	/// Calculates Freight Cost and Freight Terms
	/// </summary>
	public class FreightCalculator
	{
		protected PXGraph graph;

		public FreightCalculator(PXGraph graph)
		{
			if (graph == null)
				throw new ArgumentNullException("graph");

			this.graph = graph;
		}

		/*
		Freight Calculation

		1. Per Unit

		FreightInBase = BaseRate + Rate * Weight

		Select FreightRate.Rate 
		FreightRate.Volume <= Order.Volume && 
		FreightRate.Weight <= Order.Weight && 
		FreightRate.Zone = Order.ShippingZone 
		ORDER BY FreightRate.Volume Asc, FreightRate.Weight Asc, FreightRate.Rate Asc

		IF not found, then

		Select ShipTerms.Rate 
		FreightRate.Volume <= Order.Volume && 
		FreightRate.Weight <= Order.Weight 
		ORDER BY FreightRate.Volume Asc, FreightRate.Weight Asc, FreightRate.Rate Desc
		(MAX Rate for search criteria)

		2. Net

		FreightInBase = BaseRate + Rate
		--------------------------------------------------------------------------------
											 FreightCost%			   InvoiceAmount%
		FreightFinalInBase = FreightInBase * ------------ + LineTotal * --------------
												 100						  100
		*/

		/// <summary>
		/// First calculates and sets CuryFreightCost, then applies the Terms and updates CuryFreightAmt.
		/// </summary>
		public virtual void CalcFreight<T, CuryFreightCostField, CuryFreightAmtField>(PXCache sender, T data, int? linesCount)
			where T : class, IFreightBase, new()
			where CuryFreightAmtField : IBqlField
			where CuryFreightCostField : IBqlField
		{
			CalcFreightCost<T, CuryFreightCostField>(sender, data);
			ApplyFreightTerms<T, CuryFreightAmtField>(sender, data, linesCount);
		}

		/// <summary>
		/// Calculates and sets CuryFreightCost
		/// </summary>
		public virtual void CalcFreightCost<T, CuryFreightCostField>(PXCache sender, T data)
			where T : class, IFreightBase, new()
			where CuryFreightCostField : IBqlField
		{
			data.FreightCost = CalculateFreightCost<T>(data);
			CM.PXCurrencyAttribute.CuryConvCury<CuryFreightCostField>(sender, data);
		}

		/// <summary>
		/// Applies the Terms and updates CuryFreightAmt.
		/// </summary>
		public virtual void ApplyFreightTerms<T, CuryFreightAmtField>(PXCache sender, T data, int? linesCount)
			where T : class, IFreightBase, new()
			where CuryFreightAmtField : IBqlField
		{
			ShipTermsDetail shipTermsDetail = GetFreightTerms(data.ShipTermsID, data.LineTotal);

			if (shipTermsDetail != null)
			{
				data.FreightAmt = ((data.FreightCost * (shipTermsDetail.FreightCostPercent) / 100) ?? 0) +
						(((data.LineTotal) * (shipTermsDetail.InvoiceAmountPercent) / 100) ?? 0) + (shipTermsDetail.ShippingHandling ?? 0) + ((linesCount * shipTermsDetail.LineHandling) ?? 0);
			}
			else
			{
				data.FreightAmt = data.FreightCost;
			}

			CM.PXCurrencyAttribute.CuryConvCury<CuryFreightAmtField>(sender, data);
		}

		/// <summary>
		/// Returns true if it is "flat rate shipping" ("Freight Cost %" is zero), otherwise, false.
		/// </summary>
		public virtual bool IsFlatRate<T>(PXCache sender, T data)
			where T : class, IFreightBase, new()
		{
			if (data.ShipTermsID == null)
				return false;

			ShipTermsDetail shipTermsDetail = GetFreightTerms(data.ShipTermsID, data.LineTotal);
			return shipTermsDetail?.FreightCostPercent == 0m;
		}

		protected virtual decimal CalculateFreightCost<T>(T data)
			where T : class, IFreightBase, new()
		{
			Carrier carrier = Carrier.PK.Find(graph, data.ShipVia);

			if (carrier == null)
				return 0;

            if (carrier.CalcMethod != CarrierCalcMethod.Manual)
            {
                decimal freightCostAmt = 0;
                if (data.OrderVolume == null || data.OrderVolume == 0)
                {
                    //Get Freight Rate based only on weight.
                    FreightRate freightRateOnWeight = GetFreightRateBasedOnWeight(carrier.CarrierID, data.ShipZoneID, (data.PackageWeight == null || data.PackageWeight == 0) ? data.OrderWeight : data.PackageWeight);

                    if (carrier.CalcMethod == CarrierCalcMethod.Net)
                        freightCostAmt = freightRateOnWeight.Rate ?? 0;
                    else
                        if (data.PackageWeight == null || data.PackageWeight == 0)
                            freightCostAmt = (freightRateOnWeight.Rate ?? 0m) * (data.OrderWeight ?? 0m);
                        else
                            freightCostAmt = (freightRateOnWeight.Rate ?? 0m) * (data.PackageWeight ?? 0m);
                }
                else if (data.PackageWeight == null || data.PackageWeight == 0)
                {
                    //Get Freight Rate based only on Volume
                    FreightRate freightRateOnVolume = GetFreightRateBasedOnVolume(carrier.CarrierID, data.ShipZoneID, data.OrderVolume);

                    if (carrier.CalcMethod == CarrierCalcMethod.Net)
                        freightCostAmt = freightRateOnVolume.Rate ?? 0;
                    else
                        freightCostAmt = (freightRateOnVolume.Rate ?? 0m) * (data.OrderVolume ?? 0m);
                }
                else
                {
                    FreightRate freightRateOnWeight = GetFreightRateBasedOnWeight(carrier.CarrierID, data.ShipZoneID, (data.PackageWeight == null || data.PackageWeight == 0) ? data.OrderWeight : data.PackageWeight);
                    FreightRate freightRateOnVolume = GetFreightRateBasedOnVolume(carrier.CarrierID, data.ShipZoneID, data.OrderVolume);

                    decimal freightCostByWeight = 0;
                    decimal freightCostByVolume = 0;


                    if (carrier.CalcMethod == CarrierCalcMethod.Net)
                    {
                        freightCostByWeight = freightRateOnWeight.Rate ?? 0;
                        freightCostByVolume = freightRateOnVolume.Rate ?? 0;
                    }
                    else
                    {
                        freightCostByWeight = (freightRateOnWeight.Rate ?? 0m) * (data.PackageWeight ?? 0m);
                        freightCostByVolume = (freightRateOnVolume.Rate ?? 0m) * (data.OrderVolume ?? 0m);
                    }

                    freightCostAmt = Math.Max(freightCostByWeight, freightCostByVolume);
                }

                return (carrier.BaseRate ?? 0m) + freightCostAmt;
            }
            else
            {
                return data.FreightCost ?? 0m;
            }
		}

		protected virtual ShipTermsDetail GetFreightTerms(string shipTermsID, decimal? lineTotal)
		{
			return PXSelect<ShipTermsDetail,
				Where<ShipTermsDetail.shipTermsID, Equal<Required<SOOrder.shipTermsID>>,
				And<ShipTermsDetail.breakAmount, LessEqual<Required<SOOrder.lineTotal>>>>,
				OrderBy<Desc<ShipTermsDetail.breakAmount>>>.Select(graph, shipTermsID, lineTotal);

		}

		protected virtual FreightRate GetFreightRateBasedOnWeight(string carrierID, string shipZoneID, decimal? weight)
		{
			FreightRate freightRate = PXSelect<FreightRate,
				Where<FreightRate.carrierID, Equal<Required<FreightRate.carrierID>>,
				And<FreightRate.weight, LessEqual<Required<SOOrder.orderWeight>>,
				And<FreightRate.zoneID, Equal<Required<SOOrder.shipZoneID>>>>>,
				OrderBy<Desc<FreightRate.volume, Desc<FreightRate.weight, Asc<FreightRate.rate>>>>>.
				Select(graph, carrierID, weight, shipZoneID);

			if (freightRate == null)
			{
				freightRate = PXSelect<FreightRate,
					Where<FreightRate.carrierID, Equal<Required<FreightRate.carrierID>>,
					And<FreightRate.weight, LessEqual<Required<FreightRate.weight>>>>,
					OrderBy<Desc<FreightRate.volume, Desc<FreightRate.weight, Desc<FreightRate.rate>>>>>.
					Select(graph, weight);
			}

			return freightRate ?? new FreightRate();
		}

		protected virtual FreightRate GetFreightRateBasedOnVolume(string carrierID, string shipZoneID, decimal? volume)
		{
			FreightRate freightRate = PXSelect<FreightRate,
				Where<FreightRate.carrierID, Equal<Required<FreightRate.carrierID>>,
				And<FreightRate.volume, LessEqual<Required<FreightRate.volume>>,
				And<FreightRate.zoneID, Equal<Required<FreightRate.zoneID>>>>>,
				OrderBy<Desc<FreightRate.volume, Desc<FreightRate.weight, Asc<FreightRate.rate>>>>>.
				Select(graph, carrierID, volume, shipZoneID);

			if (freightRate == null)
			{
				freightRate = PXSelect<FreightRate,
					Where<FreightRate.carrierID, Equal<Required<FreightRate.carrierID>>,
					And<FreightRate.weight, LessEqual<Required<FreightRate.weight>>>>,
					OrderBy<Desc<FreightRate.volume, Desc<FreightRate.weight, Desc<FreightRate.rate>>>>>.
					Select(graph, volume);
			}

			return freightRate ?? new FreightRate();
		}

	}

	public class SOLineSplitPlanIDAttribute : INItemPlanIDAttribute
	{
		#region State
		protected Type _ParentOrderDate;
		protected Lazy<Dictionary<object[], HashSet<Guid?>>> _processingSets;
		#endregion
		#region Ctor
		public SOLineSplitPlanIDAttribute(Type ParentNoteID, Type ParentHoldEntry, Type ParentOrderDate)
			: base(ParentNoteID, ParentHoldEntry)
		{
			_ParentOrderDate = ParentOrderDate;
		}
		#endregion
		#region Implementation
		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);

			_processingSets = Lazy.By(() => new Dictionary<object[], HashSet<Guid?>>());
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
			WebDialogResult answer = sender.Graph.Views.TryGetValue("Document", out view) ? view.Answer : WebDialogResult.None;

			bool DatesUpdated = !sender.ObjectsEqual<SOOrder.shipDate>(e.Row, e.OldRow) && (answer == WebDialogResult.Yes || ((SOOrder)e.Row).ShipComplete != SOShipComplete.BackOrderAllowed);
			bool RequestOnUpdated = !sender.ObjectsEqual<SOOrder.requestDate>(e.Row, e.OldRow) && (answer == WebDialogResult.Yes || ((SOOrder)e.Row).ShipComplete != SOShipComplete.BackOrderAllowed);
			bool CreditHoldApprovedUpdated = !sender.ObjectsEqual<SOOrder.creditHold, SOOrder.approved>(e.Row, e.OldRow);
			bool CustomerUpdated = !sender.ObjectsEqual<SOOrder.customerID>(e.Row, e.OldRow);

			if (CustomerUpdated || DatesUpdated || RequestOnUpdated || CreditHoldApprovedUpdated
				|| !sender.ObjectsEqual<SOOrder.hold,
										SOOrder.cancelled,
										SOOrder.completed,
										SOOrder.backOrdered,
										SOOrder.shipComplete,
										SOOrder.prepaymentReqSatisfied,
										SOOrder.isExpired>(e.Row, e.OldRow))
			{
				DatesUpdated |= !sender.ObjectsEqual<SOOrder.shipComplete>(e.Row, e.OldRow) && ((SOOrder)e.Row).ShipComplete != SOShipComplete.BackOrderAllowed;
				RequestOnUpdated |= !sender.ObjectsEqual<SOOrder.shipComplete>(e.Row, e.OldRow) && ((SOOrder)e.Row).ShipComplete != SOShipComplete.BackOrderAllowed;

				bool Cancelled = (bool?)sender.GetValue<SOOrder.cancelled>(e.Row) == true;
				bool Completed = (bool?)sender.GetValue<SOOrder.completed>(e.Row) == true;
                bool? BackOrdered = (bool?)sender.GetValue<SOOrder.backOrdered>(e.Row);

				PXCache plancache = sender.Graph.Caches[typeof(INItemPlan)];
				PXCache splitcache = sender.Graph.Caches[typeof(SOLineSplit)];
				PXCache linecache = sender.Graph.Caches[typeof(SOLine)];

				SOOrderType ordertype = PXSetup<SOOrderType>.Select(sender.Graph);
				var unlinkedLines = new Dictionary<int?, bool>();
				var splitsByPlan = new Dictionary<long?, SOLineSplit>();
				var plansToDelete = new HashSet<long?>();
				foreach (IEnumerable<SOLineSplit> lineSplits in PXSelect<SOLineSplit,
					Where<SOLineSplit.orderType, Equal<Current<SOOrder.orderType>>, 
					And<SOLineSplit.orderNbr, Equal<Current<SOOrder.orderNbr>>>>>
					.SelectMultiBound(sender.Graph, new[] { e.Row }).RowCast<SOLineSplit>().GroupBy(x => x.LineNbr).ToDictionary(x => x.Key, x => x.ToList()).Values)
                {
					bool clearPOLinks = false;
					bool newDropShipLine = IsDropShipNotLegacy(lineSplits.FirstOrDefault(), splitcache);
					if (!newDropShipLine && Cancelled && lineSplits.Any(x => x.Completed != true && !string.IsNullOrEmpty(x.PONbr)))
					{
						clearPOLinks = true;
						unlinkedLines.Add(lineSplits.First().LineNbr, !lineSplits.Any(x => x.Completed == true && !string.IsNullOrEmpty(x.PONbr)));
					}

					foreach (SOLineSplit split in lineSplits)
					{
						if (Cancelled || Completed)
						{
							if (split.Completed != true)
							{
								plancache.Inserted.RowCast<INItemPlan>()
									.Where(_ => _.PlanID == split.PlanID)
									.ForEach(_ => plancache.Delete(_));
								if(clearPOLinks)
								{
									split.POCreate = false;
									if (!string.IsNullOrEmpty(split.PONbr))
									{
										split.ClearPOReferences();
										split.RefNoteID = null;
									}
								}
								split.PlanID = null;
								split.Completed = true;

								splitcache.MarkUpdated(split);
							}
						}
						else
						{
							if ((bool?)sender.GetValue<SOOrder.cancelled>(e.OldRow) == true)
							{
								if (string.IsNullOrEmpty(split.ShipmentNbr) && split.POCompleted == false)
								{
									split.Completed = false;
								}

								INItemPlan plan = DefaultValues(splitcache, split);
								if (plan != null)
								{
									plan = (INItemPlan)sender.Graph.Caches[typeof(INItemPlan)].Insert(plan);
									split.PlanID = plan.PlanID;
								}

								splitcache.MarkUpdated(split);
							}

							if (!sender.ObjectsEqual<SOOrder.isExpired>(e.OldRow, e.Row)
								&& split.POCreate == true && string.IsNullOrEmpty(split.PONbr))
							{
								bool? newIsExpired = (bool?)sender.GetValue<SOOrder.isExpired>(e.Row);
								if (newIsExpired == false && split.PlanID == null)
								{
									INItemPlan plan = DefaultValues(splitcache, split);
									if (plan != null)
									{
										plan = (INItemPlan)sender.Graph.Caches[typeof(INItemPlan)].Insert(plan);
										split.PlanID = plan.PlanID;
										splitcache.MarkUpdated(split);
									}
								}
								else if (newIsExpired == true && split.PlanID != null)
								{
									plansToDelete.Add(split.PlanID);
									split.PlanID = null;
									splitcache.MarkUpdated(split);
								}
							}

							if (DatesUpdated)
							{
								split.ShipDate = (DateTime?)sender.GetValue<SOOrder.shipDate>(e.Row);
								splitcache.MarkUpdated(split);
							}

							if (split.PlanID != null)
							{
								splitsByPlan[split.PlanID] = split;
							}
						}
					}
				}

				foreach (SOLine line in PXSelect<SOLine,
					Where<SOLine.orderType, Equal<Current<SOOrder.orderType>>, And<SOLine.orderNbr, Equal<Current<SOOrder.orderNbr>>, And<SOLine.lineType, NotEqual<SOLineType.miscCharge>>>>>
					.SelectMultiBound(sender.Graph, new[] { e.Row }))
				{
					if (Cancelled || Completed)
					{
						if (line.Completed != true)
						{
							SOLine old_row = PXCache<SOLine>.CreateCopy(line);
							line.UnbilledQty -= line.OpenQty;
							line.OpenQty = 0m;
							linecache.RaiseFieldUpdated<SOLine.unbilledQty>(line, 0m);
							linecache.RaiseFieldUpdated<SOLine.openQty>(line, 0m);

							bool clearPOCreated;
							if (unlinkedLines.TryGetValue(line.LineNbr, out clearPOCreated))
							{
								if (clearPOCreated)
									line.POCreated = false;
								if (line.POCreate == true)
								{
									line.POCreate = false;
									linecache.RaiseFieldUpdated<SOLine.pOCreate>(line, true);
								}
							}
							line.Completed = true;
							LSSOLine.ResetAvailabilityCounters(line);

							//SOOrderEntry_SOOrder_RowUpdated should execute later to correctly update balances
							TaxAttribute.Calculate<SOLine.taxCategoryID>(linecache, new PXRowUpdatedEventArgs(line, old_row, false));

							linecache.MarkUpdated(line);
						}
					}
					else
					{
						if ((bool?)sender.GetValue<SOOrder.cancelled>(e.OldRow) == true)
						{
							SOLine old_row = PXCache<SOLine>.CreateCopy(line);
							line.OpenQty = line.OrderQty - line.ShippedQty;
							line.UnbilledQty += line.OpenQty;
							object value = line.UnbilledQty;
							linecache.RaiseFieldVerifying<SOLine.unbilledQty>(line, ref value);
							linecache.RaiseFieldUpdated<SOLine.unbilledQty>(line, value);

							value = line.OpenQty;
							linecache.RaiseFieldVerifying<SOLine.openQty>(line, ref value);
							linecache.RaiseFieldUpdated<SOLine.openQty>(line, value);

							linecache.SetValueExt<SOLine.completed>(line, false);

							TaxAttribute.Calculate<SOLine.taxCategoryID>(linecache, new PXRowUpdatedEventArgs(line, old_row, false));

							linecache.MarkUpdated(line);
						}
						if (DatesUpdated)
						{
							line.ShipDate = (DateTime?)sender.GetValue<SOOrder.shipDate>(e.Row);
							linecache.MarkUpdated(line);
						}
						if (RequestOnUpdated)
						{
							line.RequestDate = (DateTime?)sender.GetValue<SOOrder.requestDate>(e.Row);
							linecache.MarkUpdated(line);
						}
						if (CreditHoldApprovedUpdated || !sender.ObjectsEqual<SOOrder.hold>(e.Row, e.OldRow))
						{
							LSSOLine.ResetAvailabilityCounters(line);
						}
					}
				}

				PXFormulaAttribute.CalcAggregate<SOLine.unbilledQty>(linecache, e.Row);
				PXFormulaAttribute.CalcAggregate<SOLine.openQty>(linecache, e.Row);

				PXSelectBase<INItemPlan> cmd = new PXSelect<INItemPlan, Where<INItemPlan.refNoteID, Equal<Current<SOOrder.noteID>>>>(sender.Graph);

				//BackOrdered is tri-state
				if (BackOrdered == true && sender.GetValue<SOOrder.lastSiteID>(e.Row) != null && sender.GetValue<SOOrder.lastShipDate>(e.Row) != null)
				{
					cmd.WhereAnd<Where<INItemPlan.siteID, Equal<Current<SOOrder.lastSiteID>>, And<INItemPlan.planDate, LessEqual<Current<SOOrder.lastShipDate>>>>>();
				}

				if (BackOrdered == false)
				{
					sender.SetValue<SOOrder.lastSiteID>(e.Row, null);
					sender.SetValue<SOOrder.lastShipDate>(e.Row, null);
				}

				foreach (INItemPlan plan in cmd.View.SelectMultiBound(new [] { e.Row }))
				{
					if (Cancelled || Completed || plansToDelete.Contains(plan.PlanID))
					{
						plancache.Delete(plan);
					}
					else
					{
						INItemPlan copy = PXCache<INItemPlan>.CreateCopy(plan);

						if (DatesUpdated)
						{
							plan.PlanDate = (DateTime?)sender.GetValue<SOOrder.shipDate>(e.Row);
						}
						if (CustomerUpdated)
						{
							plan.BAccountID = (int?)sender.GetValue<SOOrder.customerID>(e.Row);
						}
						plan.Hold = IsOrderOnHold((SOOrder)e.Row);

						// We should skip allocated plans. In general we should process only "normal" plans.
						bool initPlanType = (ordertype.RequireAllocation != true)
							&& plan.PlanType.IsIn(INPlanConstants.Plan60, INPlanConstants.Plan62, INPlanConstants.Plan68, INPlanConstants.Plan69);
						if (initPlanType)
						{
							SOLineSplit split;
							if (splitsByPlan.TryGetValue(plan.PlanID, out split))
							{
								plan.PlanType = CalcPlanType(sender, plan, (SOOrder)e.Row, split, BackOrdered);

								if (!string.Equals(copy.PlanType, plan.PlanType))
								{
									plancache.RaiseRowUpdated(plan, copy);
								}
							}
						}

						plancache.MarkUpdated(plan);
					}
				}
				// SOOrder.BackOrdered value should be handled only single time and only in this method
				sender.SetValue<SOOrder.backOrdered>(e.Row, null);
			}

			RecalcOpenLineCounters(sender.Graph, e.Row as SOOrder, e.OldRow as SOOrder);
		}

		public virtual bool IsDropShipNotLegacy(SOLineSplit split, PXCache sender)
		{
			SOLine soLine = split != null ? PXParentAttribute.SelectParent<SOLine>(sender, split) : null;
			return soLine != null && soLine.POCreate == true && soLine.POSource == INReplenishmentSource.DropShipToOrder && soLine.IsLegacyDropShip != true;
		}

		protected virtual void RecalcOpenLineCounters(PXGraph graph, SOOrder newRow, SOOrder oldRow)
		{
			var orderCache = graph.Caches[typeof(SOOrder)];

			if (!orderCache.ObjectsEqual<SOOrder.cancelled, SOOrder.completed>(newRow, oldRow))
			{
				PXCache orderSiteCache = graph.Caches[typeof(SOOrderSite)];
				PXCache lineCache = graph.Caches[typeof(SOLine)];

				var orderSites = PXParentAttribute.SelectChildren(orderSiteCache, newRow, typeof(SOOrder));

				foreach (SOOrderSite orderSite in orderSites)
				{
					PXFormulaAttribute.CalcAggregate<SOLine.siteID>(lineCache, orderSite);
					orderSiteCache.MarkUpdated(orderSite);
				}

				PXFormulaAttribute.CalcAggregate<SOOrderSite.openShipmentCntr>(orderSiteCache, newRow);
				PXFormulaAttribute.CalcAggregate<SOLine.openLine>(lineCache, newRow);
			}
		}

		public override void Plan_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			// crutch for mass processing CreateShipment & PXProcessing<Table>.TryPersistPerRow
			if (e.Operation == PXDBOperation.Update)
			{
				INItemPlan row = e.Row as INItemPlan;
				PXCache cache = sender.Graph.Caches[typeof(SOOrder)];
				SOOrder order = cache.Current as SOOrder;

				if (row != null && order != null
					&& row.RefNoteID != order.NoteID
					&& PXLongOperation.GetCustomInfo(sender.Graph.UID, PXProcessing.ProcessingKey, out object[] processingList) != null
					&& processingList != null)
				{
					if (!_processingSets.Value.TryGetValue(processingList, out HashSet<Guid?> processingSet))
					{
						processingSet = processingList.Select(x => ((SOOrder)x).NoteID).ToHashSet();
						_processingSets.Value[processingList] = processingSet;
					}

					if (processingSet.Contains(row.RefNoteID))
					e.Cancel = true;
				}
			}
			base.Plan_RowPersisting(sender, e);
		}

		bool InitPlan = false;
		bool InitVendor = false;
		bool ResetSupplyPlanID = false;
		public override void RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			//respond only to GUI operations
		    var IsLinked = IsLineLinked((SOLineSplit)e.Row);

		    InitPlan = InitPlanRequired(sender, e) && !IsLinked;
			InitVendor = !sender.ObjectsEqual<SOLineSplit.siteID, SOLineSplit.subItemID, SOLineSplit.vendorID, SOLineSplit.pOCreate>(e.Row, e.OldRow) &&
				!IsLinked;
			ResetSupplyPlanID = !IsLinked;

			try
			{
				base.RowUpdated(sender, e);
			}
			finally
			{
				InitPlan = false;
				InitVendor = false;
				ResetSupplyPlanID = false;
			}
		}

	    protected virtual bool InitPlanRequired(PXCache cache, PXRowUpdatedEventArgs e)
	    {
	        return !cache
	            .ObjectsEqual<SOLineSplit.isAllocated, 
	                SOLineSplit.siteID, 
	                SOLineSplit.pOCreate, 
	                SOLineSplit.pOSource,
	                SOLineSplit.operation>(e.Row, e.OldRow);
	    }

	    protected virtual bool IsLineLinked(SOLineSplit soLineSplit)
	    {
	        return soLineSplit != null && (soLineSplit.PONbr != null || soLineSplit.SOOrderNbr != null && soLineSplit.IsAllocated == true);
	    }

		public override INItemPlan DefaultValues(PXCache sender, INItemPlan plan_Row, object orig_Row)
		{
			var split_Row = (SOLineSplit)orig_Row;
			if (split_Row.Completed == true
				|| split_Row.POCompleted == true
				|| split_Row.LineType == SOLineType.MiscCharge
				|| split_Row.LineType == SOLineType.NonInventory && split_Row.RequireShipping == false && split_Row.Behavior != SOBehavior.BL)
			{
				return null;
			}
			SOLine parent = (SOLine)PXParentAttribute.SelectParent(sender, split_Row, typeof(SOLine));
			SOOrder order = (SOOrder)PXParentAttribute.SelectParent(sender, split_Row, typeof(SOOrder));

			if (string.IsNullOrEmpty(plan_Row.PlanType) || InitPlan || ResetSupplyPlanID && order.IsExpired == true)
			{
				plan_Row.PlanType = CalcPlanType(sender, plan_Row, order, split_Row);
				if (split_Row.POCreate == true)
				{
					plan_Row.FixedSource = INReplenishmentSource.Purchased;
					if (split_Row.POType != PO.POOrderType.Blanket && split_Row.POType != PO.POOrderType.DropShip && split_Row.POSource == INReplenishmentSource.PurchaseToOrder)
						plan_Row.SourceSiteID = order.DestinationSiteID ?? split_Row.SiteID;
					else
						plan_Row.SourceSiteID = split_Row.SiteID;
				}
				else
				{
                    plan_Row.Reverse = (split_Row.Operation == SOOperation.Receipt);

					plan_Row.FixedSource = (split_Row.SiteID != split_Row.ToSiteID ? INReplenishmentSource.Transfer : INReplenishmentSource.None);
                    plan_Row.SourceSiteID = split_Row.SiteID;
				}
			}

			if (ResetSupplyPlanID)
			{
				plan_Row.SupplyPlanID = null;
			}

			plan_Row.VendorID = split_Row.VendorID;

			if (InitVendor || split_Row.POCreate == true && plan_Row.VendorID != null && plan_Row.VendorLocationID == null)
			{
				plan_Row.VendorLocationID =
					PX.Objects.PO.POItemCostManager.FetchLocation(
					sender.Graph,
					split_Row.VendorID,
					split_Row.InventoryID,
					split_Row.SubItemID,
					split_Row.SiteID);
			}

			plan_Row.BAccountID = parent == null ? null : parent.CustomerID;
			plan_Row.InventoryID = split_Row.InventoryID;
			plan_Row.SubItemID = split_Row.SubItemID;
			plan_Row.SiteID = split_Row.SiteID;
			plan_Row.LocationID = split_Row.LocationID;
			plan_Row.LotSerialNbr = split_Row.LotSerialNbr;
			plan_Row.IsTempLotSerial = string.IsNullOrEmpty(split_Row.AssignedNbr) == false &&
				INLotSerialNbrAttribute.StringsEqual(split_Row.AssignedNbr, split_Row.LotSerialNbr);
			plan_Row.ProjectID = parent?.ProjectID;
			plan_Row.TaskID = parent?.TaskID;

			if (plan_Row.IsTempLotSerial == true)
			{
				plan_Row.LotSerialNbr = null;
			}
			plan_Row.PlanDate = split_Row.ShipDate;
			plan_Row.UOM = parent?.UOM;
			plan_Row.PlanQty =
				(split_Row.POCreate == true
					? split_Row.BaseUnreceivedQty - ((split_Row.Behavior == SOBehavior.BL) ? 0m : split_Row.BaseShippedQty)
					: split_Row.BaseQty)
				- split_Row.BaseQtyOnOrders;

			PXCache cache = sender.Graph.Caches[BqlCommand.GetItemType(_ParentNoteID)];
			plan_Row.RefNoteID = (Guid?)cache.GetValue(cache.Current, _ParentNoteID.Name);
			plan_Row.Hold = IsOrderOnHold(order);

			if (string.IsNullOrEmpty(plan_Row.PlanType))
			{
				return null;
			}
			return plan_Row;
		}

		protected virtual bool IsOrderOnHold(SOOrder order)
	    {
			return (order != null) && ((order.Hold ?? false) || (order.CreditHold ?? false) || (!order.Approved ?? false) || (!order.PrepaymentReqSatisfied ?? false));
		}

		protected virtual string CalcPlanType(PXCache sender, INItemPlan plan, SOOrder order, SOLineSplit split, bool? backOrdered = null)
		{
			if (split.POCreate == true && split.Operation == SOOperation.Receipt)
				return null;

			SOLine soLine = PXParentAttribute.SelectParent<SOLine>(sender, split);
			if (split.POCreate == true && soLine?.IsLegacyDropShip == true)
			{
				if (split.POType == PO.POOrderType.Blanket)
				{
					return (split.POSource == INReplenishmentSource.DropShipToOrder)
						? INPlanConstants.Plan6E
						: INPlanConstants.Plan6B;
				}
				else
				{
					return (split.POSource == INReplenishmentSource.DropShipToOrder)
						? INPlanConstants.Plan6D
						: INPlanConstants.Plan66;
				}
			}

			if (split.POSource == INReplenishmentSource.DropShipToOrder && (split.POCreate == true || split.PONbr != null))
				return INPlanConstants.Plan6D;

			if (split.POCreate == true && split.POSource == INReplenishmentSource.BlanketDropShipToOrder)
				return INPlanConstants.Plan6E;

			if (split.POCreate == true && split.POSource == INReplenishmentSource.BlanketPurchaseToOrder)
				return order.IsExpired == true && string.IsNullOrEmpty(split.PONbr) ? null : INPlanConstants.Plan6B;

			if (split.POCreate == true && split.POSource == INReplenishmentSource.PurchaseToOrder)
				return order.IsExpired == true && string.IsNullOrEmpty(split.PONbr) ? null : INPlanConstants.Plan66;

			SOOrderType ordertype = PXSetup<SOOrderType>.Select(sender.Graph);
			bool isAllocation = (split.IsAllocated == true) || INPlanConstants.IsAllocated(plan.PlanType) || INPlanConstants.IsFixed(plan.PlanType);
			bool isOrderOnHold = IsOrderOnHold(order) && ordertype.RequireAllocation != true;

			string calcedPlanType = CalcPlanType(plan, split, ordertype, isOrderOnHold);
			bool putOnSOPrepared = (calcedPlanType == INPlanConstants.Plan69);

			if (!InitPlan && !putOnSOPrepared && !isAllocation)
			{
				if (backOrdered == true || backOrdered == null && plan.PlanType == INPlanConstants.Plan68)
				{
					return INPlanConstants.Plan68;
				}
	        }

			return calcedPlanType;
	    }

		protected virtual string CalcPlanType(INItemPlan plan, SOLineSplit split, SOOrderType ordertype, bool isOrderOnHold)
		{
			if (split.Behavior == SOBehavior.BL)
			{
				return (split.IsAllocated == true) ? split.AllocatedPlanType : null;
			}
			else if (ordertype == null || ordertype.RequireShipping == true)
			{
				return (split.IsAllocated == true) ? split.AllocatedPlanType
					: isOrderOnHold ? INPlanConstants.Plan69
					: (split.RequireAllocation != true || split.IsStockItem != true) ? split.PlanType : split.BackOrderPlanType;
			}
			else
			{
				return (isOrderOnHold != true || split.IsStockItem != true) ? split.PlanType : INPlanConstants.Plan69;
			}
		}
		#endregion
    }

	public abstract class SOShipLineSplitPlanIDBaseAttribute : INItemPlanIDAttribute
	{
		#region State
		protected Type _ParentOrderDate;
		#endregion
		#region Ctor
		public SOShipLineSplitPlanIDBaseAttribute(Type ParentNoteID, Type ParentHoldEntry, Type ParentOrderDate)
			: base(ParentNoteID, ParentHoldEntry)
		{
			_ParentOrderDate = ParentOrderDate;
		}
		#endregion
		#region Implementation

		public override void Parent_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			base.Parent_RowUpdated(sender, e);

			if (!sender.ObjectsEqual<SOShipment.shipDate, SOShipment.hold>(e.Row, e.OldRow))
			{
				PXCache plancache = sender.Graph.Caches[typeof(INItemPlan)];
				HashSet<long?> shipmentPlans = CollectShipmentPlans(sender);
				foreach (INItemPlan plan in plancache.Inserted)
				{
					if (shipmentPlans.Contains(plan.PlanID))
					{
						plan.Hold = (bool?)sender.GetValue<SOShipment.hold>(e.Row);
						plan.PlanDate = (DateTime?)sender.GetValue<SOShipment.shipDate>(e.Row);
					}
				}

				UpdatePlansOnParentUpdated(sender, e.Row);
			}
		}

		protected virtual void UpdatePlansOnParentUpdated(PXCache sender, object parent)
		{
		}

		protected abstract HashSet<long?> CollectShipmentPlans(PXCache sender, string shipmentNbr = null);

		public override INItemPlan DefaultValues(PXCache sender, INItemPlan plan_Row, object orig_Row)
		{
			if ((bool?)sender.GetValue<SOShipLineSplit.released>(orig_Row) == true
				|| (bool?)sender.GetValue<SOShipLineSplit.isStockItem>(orig_Row) == false)
			{
				return null;
			}
			else if (plan_Row.IsTemporary != true && (bool?)sender.GetValue<SOShipLineSplit.confirmed>(orig_Row) == true)
			{
				return plan_Row;
			}
			SOShipLine parent = plan_Row.IsTemporary == true ? null : (SOShipLine)PXParentAttribute.SelectParent(sender, orig_Row, typeof(SOShipLine));

			if (plan_Row.IsTemporary != true && parent == null) return null;

			plan_Row.BAccountID = parent?.CustomerID;
			plan_Row.PlanType = (string)sender.GetValue<SOShipLineSplit.planType>(orig_Row);
			plan_Row.OrigPlanType = (string)sender.GetValue<SOShipLineSplit.origPlanType>(orig_Row);
			plan_Row.IgnoreOrigPlan = (bool?)sender.GetValue<SOShipLineSplit.isComponentItem>(orig_Row) == true;
			plan_Row.InventoryID = (int?)sender.GetValue<SOShipLineSplit.inventoryID>(orig_Row);
			plan_Row.Reverse = (string)sender.GetValue<SOShipLineSplit.operation>(orig_Row) == SOOperation.Receipt;
			plan_Row.SubItemID = (int?)sender.GetValue<SOShipLineSplit.subItemID>(orig_Row);
			plan_Row.SiteID = (int?)sender.GetValue<SOShipLineSplit.siteID>(orig_Row);
			plan_Row.LocationID = (int?)sender.GetValue<SOShipLineSplit.locationID>(orig_Row);
			plan_Row.LotSerialNbr = (string)sender.GetValue<SOShipLineSplit.lotSerialNbr>(orig_Row);
			plan_Row.ProjectID = parent?.ProjectID;
			plan_Row.TaskID = parent?.TaskID;

			string assignedNbr = (string)sender.GetValue<SOShipLineSplit.assignedNbr>(orig_Row);
			plan_Row.IsTempLotSerial = string.IsNullOrEmpty(assignedNbr) == false &&
				INLotSerialNbrAttribute.StringsEqual(assignedNbr, plan_Row.LotSerialNbr);

			if (plan_Row.IsTempLotSerial == true)
			{
				plan_Row.LotSerialNbr = null;
			}
			plan_Row.PlanDate = (DateTime?)sender.GetValue<SOShipLineSplit.shipDate>(orig_Row);
			plan_Row.OrigUOM = parent?.OrderUOM;
			plan_Row.UOM = parent?.UOM;
			plan_Row.PlanQty = (decimal?)sender.GetValue<SOShipLineSplit.baseQty>(orig_Row);

			PXCache cache = sender.Graph.Caches[BqlCommand.GetItemType(_ParentNoteID)];
			plan_Row.RefNoteID = (Guid?)cache.GetValue(cache.Current, _ParentNoteID.Name);
			plan_Row.Hold = (bool?)cache.GetValue(cache.Current, _ParentHoldEntry.Name);

			if (string.IsNullOrEmpty(plan_Row.PlanType))
			{
				return null;
			}

            if (parent != null)
            {
				if (plan_Row.OrigNoteID == null)
				{
					SOOrder doc = SOOrder.PK.Find(sender.Graph, parent.OrigOrderType, parent.OrigOrderNbr);
					plan_Row.OrigNoteID = doc?.NoteID;
				}

				if (parent.OrigSplitLineNbr != null)
				{
					SOLineSplit split = SOLineSplit.PK.Find(sender.Graph, parent.OrigOrderType, parent.OrigOrderNbr, parent.OrigLineNbr, parent.OrigSplitLineNbr);
					plan_Row.OrigPlanLevel = (!string.IsNullOrEmpty(split.LotSerialNbr) ? INPlanLevel.LotSerial : INPlanLevel.Site);
					plan_Row.OrigPlanID = split.PlanID;
					plan_Row.IgnoreOrigPlan |= !string.IsNullOrEmpty(split.LotSerialNbr) &&
						!string.Equals(plan_Row.LotSerialNbr, split.LotSerialNbr, StringComparison.InvariantCultureIgnoreCase);
				}
			}

			return plan_Row;
		}

		#endregion
	}

	public class SOShipLineSplitPlanIDAttribute : SOShipLineSplitPlanIDBaseAttribute
	{
		#region State
		protected Lazy<Dictionary<object[], HashSet<Guid?>>> _processingSets;
		#endregion
		#region Ctor
		public SOShipLineSplitPlanIDAttribute(Type ParentNoteID, Type ParentHoldEntry, Type ParentOrderDate)
			: base(ParentNoteID, ParentHoldEntry, ParentOrderDate)
		{
		}
		#endregion
		#region Implementation

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			_processingSets = Lazy.By(() => new Dictionary<object[], HashSet<Guid?>>());
		}

		protected override void UpdatePlansOnParentUpdated(PXCache sender, object parent)
		{
			PXCache plancache = sender.Graph.Caches[typeof(INItemPlan)];
			foreach (INItemPlan plan in PXSelect<INItemPlan, Where<INItemPlan.refNoteID, Equal<Current<SOShipment.noteID>>>>.Select(sender.Graph))
			{
				plan.Hold = (bool?)sender.GetValue<SOShipment.hold>(parent);
				plan.PlanDate = (DateTime?)sender.GetValue<SOShipment.shipDate>(parent);

				plancache.MarkUpdated(plan);
			}
		}

		protected override HashSet<long?> CollectShipmentPlans(PXCache sender, string shipmentNbr = null)
		{
			object[] pars = (shipmentNbr == null) ? new object[] { } : new object[] { shipmentNbr };
			return new HashSet<long?>(
				PXSelect<SOShipLineSplit, Where<SOShipLineSplit.shipmentNbr, Equal<Optional<SOShipment.shipmentNbr>>>>
				.Select(sender.Graph, pars)
				.Select(s => ((SOShipLineSplit)s).PlanID));
		}

		public override void Plan_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			// crutch for mass processing CreateShipment & PXProcessing<Table>.TryPersistPerRow
			if (e.Operation == PXDBOperation.Update)
			{
				INItemPlan row = e.Row as INItemPlan;
				PXCache cache = sender.Graph.Caches[typeof(SOShipment)];
				SOShipment shipment = cache.Current as SOShipment;

				if (row != null && shipment != null
					&& row.RefNoteID != shipment.NoteID
					&& PXLongOperation.GetCustomInfo(sender.Graph.UID, PXProcessing.ProcessingKey, out object[] processingList) != null
					&& processingList != null)
				{
					if (!_processingSets.Value.TryGetValue(processingList, out HashSet<Guid?> processingSet))
					{
						processingSet = processingList.Select(x => ((SOShipment)x).NoteID).ToHashSet();
						_processingSets.Value[processingList] = processingSet;
					}

					if (processingSet.Contains(row.RefNoteID))
					e.Cancel = true;
				}
			}
			base.Plan_RowPersisting(sender, e);
		}
		#endregion
	}

	public class SOUnassignedShipLineSplitPlanIDAttribute : SOShipLineSplitPlanIDBaseAttribute
	{
		#region Ctor
		public SOUnassignedShipLineSplitPlanIDAttribute(Type ParentNoteID, Type ParentHoldEntry, Type ParentOrderDate)
			: base(ParentNoteID, ParentHoldEntry, ParentOrderDate)
		{
		}
		#endregion
		#region Implementation
		protected override HashSet<long?> CollectShipmentPlans(PXCache sender, string shipmentNbr = null)
		{
			object[] pars = (shipmentNbr == null) ? new object[] { } : new object[] { shipmentNbr };
			return new HashSet<long?>(
				PXSelect<Unassigned.SOShipLineSplit, Where<Unassigned.SOShipLineSplit.shipmentNbr, Equal<Optional<SOShipment.shipmentNbr>>>>
				.Select(sender.Graph, pars)
				.Select(s => ((Unassigned.SOShipLineSplit)s).PlanID));
		}
		#endregion
	}

	#region SOLotSerialNbrAttribute

	public class SOLotSerialNbrAttribute : INLotSerialNbrAttribute
	{
		private SOLotSerialNbrAttribute() { }

		public SOLotSerialNbrAttribute(Type InventoryType, Type SubItemType, Type LocationType)
		{
			var itemType = BqlCommand.GetItemType(InventoryType);
			if (!typeof(ILSMaster).IsAssignableFrom(itemType))
			{
				throw new PXArgumentException(IN.Messages.TypeMustImplementInterface, itemType.GetLongName(), typeof(ILSMaster).GetLongName());
			}

			_InventoryType = InventoryType;
			_SubItemType = SubItemType;
			_LocationType = LocationType;

			Type SearchType;
			PXSelectorAttribute attr;

			SearchType = BqlTemplate.OfCommand<Search2<INLotSerialStatus.lotSerialNbr,
				InnerJoin<INSiteLotSerial, On<INLotSerialStatus.inventoryID, Equal<INSiteLotSerial.inventoryID>, And<INLotSerialStatus.siteID, Equal<INSiteLotSerial.siteID>,
				 And<INLotSerialStatus.lotSerialNbr, Equal<INSiteLotSerial.lotSerialNbr>>>>>,
			Where<INLotSerialStatus.inventoryID, Equal<Optional<BqlPlaceholder.A>>,
				And<INLotSerialStatus.subItemID, Equal<Optional<BqlPlaceholder.B>>,
				And2<Where<INLotSerialStatus.locationID, Equal<Optional<BqlPlaceholder.C>>,
					Or<Optional<BqlPlaceholder.C>, IsNull>>, 
				And<INLotSerialStatus.qtyOnHand, Greater<decimal0>>>>>>>
				.Replace<BqlPlaceholder.A>(InventoryType)
				.Replace<BqlPlaceholder.B>(SubItemType)
				.Replace<BqlPlaceholder.C>(LocationType).ToType();


				attr = new PXSelectorAttribute(SearchType,
																	 typeof(INLotSerialStatus.lotSerialNbr),
																	 typeof(INLotSerialStatus.siteID),
																	 typeof(INLotSerialStatus.qtyOnHand),
																	 typeof(INSiteLotSerial.qtyAvail),
																	 typeof(INLotSerialStatus.expireDate));

			_Attributes.Add(attr);
			_SelAttrIndex = _Attributes.Count - 1;
		}

		public SOLotSerialNbrAttribute(Type InventoryType, Type SubItemType, Type LocationType, Type ParentLotSerialNbrType)
			: this(InventoryType, SubItemType, LocationType)
		{
			_Attributes[_DefAttrIndex] = new PXDefaultAttribute(ParentLotSerialNbrType) { PersistingCheck = PXPersistingCheck.NullOrBlank };
		}

		public override void GetSubscriber<ISubscriber>(List<ISubscriber> subscribers)
		{
			if (typeof(ISubscriber) == typeof(IPXFieldVerifyingSubscriber) || typeof(ISubscriber) == typeof(IPXFieldDefaultingSubscriber) || typeof(ISubscriber) == typeof(IPXRowPersistingSubscriber))
			{
				subscribers.Add(this as ISubscriber);
			}
			else if (typeof(ISubscriber) == typeof(IPXFieldSelectingSubscriber))
			{
				base.GetSubscriber<ISubscriber>(subscribers);

				subscribers.Remove(this as ISubscriber);
				subscribers.Add(this as ISubscriber);
				subscribers.Reverse();
			}
			else
			{
				base.GetSubscriber<ISubscriber>(subscribers);
			}
		}

		public class SOAllocationLotSerialNbrAttribute : SOLotSerialNbrAttribute
		{
			public SOAllocationLotSerialNbrAttribute(Type InventoryType, Type SubItemType, Type SiteType, Type LocationType)
			{
				var itemType = BqlCommand.GetItemType(InventoryType);
				if (!typeof(ILSMaster).IsAssignableFrom(itemType))
				{
					throw new PXArgumentException(nameof(itemType), IN.Messages.TypeMustImplementInterface, itemType.GetLongName(), typeof(ILSMaster).GetLongName());
				}

				_InventoryType = InventoryType;
				_SubItemType = SubItemType;
				_LocationType = LocationType;

				Type SearchType;
				PXSelectorAttribute attr;

				SearchType = BqlTemplate.OfCommand<Search2<INLotSerialStatus.lotSerialNbr,
					InnerJoin<INSiteLotSerial, On<INLotSerialStatus.inventoryID, Equal<INSiteLotSerial.inventoryID>, And<INLotSerialStatus.siteID, Equal<INSiteLotSerial.siteID>,
					 And<INLotSerialStatus.lotSerialNbr, Equal<INSiteLotSerial.lotSerialNbr>>>>>,
				Where<INLotSerialStatus.inventoryID, Equal<Optional<BqlPlaceholder.A>>,
					And<INLotSerialStatus.subItemID, Equal<Optional<BqlPlaceholder.B>>,
					And2<Where<INLotSerialStatus.siteID, Equal<Optional<BqlPlaceholder.C>>,
						Or<Optional<BqlPlaceholder.C>, IsNull>>,
					And2 <Where<INLotSerialStatus.locationID, Equal<Optional<BqlPlaceholder.D>>,
						Or<Optional<BqlPlaceholder.D>, IsNull>>, 
					And<INLotSerialStatus.qtyOnHand, Greater<decimal0>>>>>>>>
					.Replace<BqlPlaceholder.A>(InventoryType)
					.Replace<BqlPlaceholder.B>(SubItemType)
					.Replace<BqlPlaceholder.C>(SiteType)
					.Replace<BqlPlaceholder.D>(LocationType).ToType();


				attr = new PXSelectorAttribute(SearchType,
																	 typeof(INLotSerialStatus.lotSerialNbr),
																	 typeof(INLotSerialStatus.siteID),
																	 typeof(INLotSerialStatus.qtyOnHand),
																	 typeof(INSiteLotSerial.qtyAvail),
																	 typeof(INLotSerialStatus.expireDate));

				_Attributes.Add(attr);
				_SelAttrIndex = _Attributes.Count - 1;
			}

			public SOAllocationLotSerialNbrAttribute(Type InventoryType, Type SubItemType, Type SiteType, Type LocationType, Type ParentLotSerialNbrType)
				: this(InventoryType, SubItemType, SiteType, LocationType)
			{
				_Attributes[_DefAttrIndex] = new PXDefaultAttribute(ParentLotSerialNbrType) { PersistingCheck = PXPersistingCheck.NullOrBlank };
			}
		}

	}

	#endregion

	#region SOShipLotSerialNbrAttribute

	public class SOShipLotSerialNbrAttribute : INLotSerialNbrAttribute
	{
        public SOShipLotSerialNbrAttribute(Type SiteID, Type InventoryType, Type SubItemType, Type LocationType)
		{
			var itemType = BqlCommand.GetItemType(InventoryType);
			if (!typeof(ILSMaster).IsAssignableFrom(itemType))
			{
				throw new PXArgumentException(nameof(itemType), IN.Messages.TypeMustImplementInterface, itemType.GetLongName(), typeof(ILSMaster).GetLongName());
			}

			_InventoryType = InventoryType;
			_SubItemType = SubItemType;
			_LocationType = LocationType;

			Type SearchType = BqlCommand.Compose(
				typeof(Search2<,,>),
				typeof(INLotSerialStatus.lotSerialNbr),
				typeof(InnerJoin<INSiteLotSerial, On<INLotSerialStatus.inventoryID, Equal<INSiteLotSerial.inventoryID>, And<INLotSerialStatus.siteID, Equal<INSiteLotSerial.siteID>,
				   And<INLotSerialStatus.lotSerialNbr, Equal<INSiteLotSerial.lotSerialNbr>>>>>),
				typeof(Where<,,>),
				typeof(INLotSerialStatus.inventoryID),
				typeof(Equal<>),
				typeof(Optional<>),
				InventoryType,
				typeof(And<,,>),
				typeof(INLotSerialStatus.siteID),
				typeof(Equal<>),
				typeof(Optional<>),
				SiteID,
				typeof(And<,,>),
				typeof(INLotSerialStatus.subItemID),
				typeof(Equal<>),
				typeof(Optional<>),
				SubItemType,
				typeof(And2<,>),
				typeof(Where<,,>),
				typeof(Optional<>),
				LocationType,
				typeof(IsNotNull),
				typeof(And<,,>),
				typeof(INLotSerialStatus.locationID),
				typeof(Equal<>),
				typeof(Optional<>),
				LocationType,
				typeof(Or<,>),
				typeof(Optional<>),
				LocationType,
				typeof(IsNull),
				typeof(And<,>),
				typeof(INLotSerialStatus.qtyOnHand),
				typeof(Greater<>),
				typeof(decimal0)
				);

			{
				PXSelectorAttribute attr = new PXSelectorAttribute(SearchType,
																	 typeof(INLotSerialStatus.lotSerialNbr),
																	 typeof(INLotSerialStatus.siteID),
																	 typeof(INLotSerialStatus.locationID),
																	 typeof(INLotSerialStatus.qtyOnHand),
																	 typeof(INSiteLotSerial.qtyAvail),
																	 typeof(INLotSerialStatus.expireDate));
				_Attributes.Add(attr);
				_SelAttrIndex = _Attributes.Count - 1;
			}
		}

        public SOShipLotSerialNbrAttribute(Type SiteID, Type InventoryType, Type SubItemType, Type LocationType, Type ParentLotSerialNbrType)
            : this(SiteID, InventoryType, SubItemType, LocationType)
		{
			_Attributes[_DefAttrIndex] = new PXDefaultAttribute(ParentLotSerialNbrType) { PersistingCheck = PXPersistingCheck.NullOrBlank };
		}

		public override void GetSubscriber<ISubscriber>(List<ISubscriber> subscribers)
		{
			if (typeof(ISubscriber) == typeof(IPXFieldVerifyingSubscriber) || typeof(ISubscriber) == typeof(IPXFieldDefaultingSubscriber) || typeof(ISubscriber) == typeof(IPXRowPersistingSubscriber))
			{
				subscribers.Add(this as ISubscriber);
			}
			else if (typeof(ISubscriber) == typeof(IPXFieldSelectingSubscriber))
			{
				base.GetSubscriber<ISubscriber>(subscribers);

				subscribers.Remove(this as ISubscriber);
				subscribers.Add(this as ISubscriber);
				subscribers.Reverse();
			}
			else
			{
				base.GetSubscriber<ISubscriber>(subscribers);
			}
		}
		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			sender.Graph.FieldUpdated.AddHandler<SOShipLineSplit.lotSerialNbr>(LotSerialNumberUpdated);
		}
		protected virtual void LotSerialNumberUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			SOShipLineSplit row = e.Row as SOShipLineSplit;
			if (row == null) return;
			if (string.IsNullOrEmpty(row.LotSerialNbr)) return;
			SOShipLine parentLine = PXParentAttribute.SelectParent(sender, e.Row, typeof(SOShipLine)) as SOShipLine;
			if (parentLine == null || parentLine.IsUnassigned != true)
				return;

			if (row.LocationID != null)
				return;

			PXResultset<INLotSerialStatus> res = PXSelect<INLotSerialStatus, Where<INLotSerialStatus.inventoryID, Equal<Required<INLotSerialStatus.inventoryID>>,
				 And<INLotSerialStatus.subItemID, Equal<Required<INLotSerialStatus.subItemID>>,
				 And<INLotSerialStatus.siteID, Equal<Required<INLotSerialStatus.siteID>>,
				 And<INLotSerialStatus.lotSerialNbr, Equal<Required<INLotSerialStatus.lotSerialNbr>>,
				 And<INLotSerialStatus.qtyHardAvail, Greater<Zero>>>>>>>.SelectWindowed(sender.Graph, 0, 1, row.InventoryID, row.SubItemID, row.SiteID, row.LotSerialNbr);
			if (res.Count == 1)
			{
				sender.SetValueExt<SOShipLineSplit.locationID>(row, ((INLotSerialStatus)res).LocationID);
			}
		}
	}

	#endregion

	public abstract class SOContactAttribute : ContactAttribute
	{
		#region State
		BqlCommand _DuplicateSelect = BqlCommand.CreateInstance(typeof(Select<SOContact, Where<SOContact.customerID, Equal<Required<SOContact.customerID>>, And<SOContact.customerContactID, Equal<Required<SOContact.customerContactID>>, And<SOContact.revisionID, Equal<Required<SOContact.revisionID>>, And<SOContact.isDefaultContact, Equal<boolTrue>>>>>>));
		#endregion
		#region Ctor
		public SOContactAttribute(Type AddressIDType, Type IsDefaultAddressType, Type SelectType)
			: base(AddressIDType, IsDefaultAddressType, SelectType)
		{
		}
		#endregion
		#region Implementation
		public override void Record_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert && ((SOContact)e.Row).IsDefaultContact == true)
			{
				PXView view = sender.Graph.TypedViews.GetView(_DuplicateSelect, true);
				view.Clear();

				SOContact prev_address = (SOContact)view.SelectSingle(((SOContact)e.Row).CustomerID, ((SOContact)e.Row).CustomerContactID, ((SOContact)e.Row).RevisionID);
				if (prev_address != null)
				{
					_KeyToAbort = sender.GetValue(e.Row, _RecordID);
					object newkey = sender.Graph.Caches[typeof(SOContact)].GetValue(prev_address, _RecordID);

					PXCache cache = sender.Graph.Caches[_ItemType];

					foreach (object data in cache.Updated)
					{
						object datakey = cache.GetValue(data, _FieldOrdinal);
						if (Equals(_KeyToAbort, datakey))
						{
							cache.SetValue(data, _FieldOrdinal, newkey);
						}
					}

					_KeyToAbort = null;
					e.Cancel = true;
					return;
				}
			}
			base.Record_RowPersisting(sender, e);
		}

		public override void RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			object key = sender.GetValue(e.Row, _FieldOrdinal);
			if (key != null)
			{
				PXCache cache = sender.Graph.Caches[_RecordType];
				if (Convert.ToInt32(key) < 0)
				{
					foreach (object data in cache.Inserted)
					{
						object datakey = cache.GetValue(data, _RecordID);
						if (Equals(key, datakey))
						{
							if (((SOContact)data).IsDefaultContact == true)
							{
								PXView view = sender.Graph.TypedViews.GetView(_DuplicateSelect, true);
								view.Clear();

								SOContact prev_address = (SOContact)view.SelectSingle(((SOContact)data).CustomerID, ((SOContact)data).CustomerContactID, ((SOContact)data).RevisionID);

								if (prev_address != null)
								{
									_KeyToAbort = sender.GetValue(e.Row, _FieldOrdinal);
									object id = sender.Graph.Caches[typeof(SOContact)].GetValue(prev_address, _RecordID);
									sender.SetValue(e.Row, _FieldOrdinal, id);
								}
							}
							break;
						}
					}
				}
			}
			base.RowPersisting(sender, e);
		}
		#endregion
	}

	public class SOBillingContactAttribute : SOContactAttribute
	{
		public SOBillingContactAttribute(Type SelectType)
			: base(typeof(SOBillingContact.contactID), typeof(SOBillingContact.isDefaultContact), SelectType)
		{ 
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			sender.Graph.FieldVerifying.AddHandler<SOBillingContact.overrideContact>(Record_Override_FieldVerifying);
		}

		public override void DefaultRecord(PXCache sender, object DocumentRow, object Row)
		{
			DefaultContact<SOBillingContact, SOBillingContact.contactID>(sender, DocumentRow, Row);
		}
		public override void CopyRecord(PXCache sender, object DocumentRow, object SourceRow, bool clone)
		{
			CopyContact<SOBillingContact, SOBillingContact.contactID>(sender, DocumentRow, SourceRow, clone);
		}
		public override void Record_IsDefault_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
		}

		public virtual void Record_Override_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			e.NewValue = (e.NewValue == null ? e.NewValue : (bool?)e.NewValue == false);
			try
			{
				Contact_IsDefaultContact_FieldVerifying<SOBillingContact>(sender, e);
			}
			finally
			{
				e.NewValue = (e.NewValue == null ? e.NewValue : (bool?)e.NewValue == false);
			}
		}

		protected override void Record_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			base.Record_RowSelected(sender, e);

			if (e.Row != null)
			{
				PXUIFieldAttribute.SetEnabled<SOBillingContact.overrideContact>(sender, e.Row, true);
			}
		}
	}

	/// <summary>
	/// Shipping contact for the Sales Order document.
	/// </summary>
	public class SOShippingContactAttribute : SOContactAttribute
	{
		public SOShippingContactAttribute(Type SelectType)
			: base(typeof(SOShippingContact.contactID), typeof(SOShippingContact.isDefaultContact), SelectType)
		{
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			sender.Graph.FieldVerifying.AddHandler<SOShippingContact.overrideContact>(Record_Override_FieldVerifying);
		}

		public override void DefaultRecord(PXCache sender, object DocumentRow, object Row)
		{
			DefaultContact<SOShippingContact, SOShippingContact.contactID>(sender, DocumentRow, Row);
		}
		public override void CopyRecord(PXCache sender, object DocumentRow, object SourceRow, bool clone)
		{
			CopyContact<SOShippingContact, SOShippingContact.contactID>(sender, DocumentRow, SourceRow, clone);
		}
		public override void Record_IsDefault_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
		}

		public virtual void Record_Override_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			e.NewValue = (e.NewValue == null ? e.NewValue : (bool?)e.NewValue == false);
			try
			{
				Contact_IsDefaultContact_FieldVerifying<SOShippingContact>(sender, e);
			}
			finally
			{
				e.NewValue = (e.NewValue == null ? e.NewValue : (bool?)e.NewValue == false);
			}
		}

		protected override void Record_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			base.Record_RowSelected(sender, e);

			if (e.Row != null)
			{
				PXUIFieldAttribute.SetEnabled<SOShippingContact.overrideContact>(sender, e.Row, true);
			}
		}

        public override void DefaultContact<TContact, TContactID>(PXCache sender, object DocumentRow, object AddressRow)
		{
            int? destinationSiteID = (int?)sender.GetValue<SOOrder.destinationSiteID>(DocumentRow);
            PXView view;
            bool issitebranch = false;
            if (destinationSiteID != null)
            {
                issitebranch = true;
                BqlCommand altSelect = BqlCommand.CreateInstance(typeof(
                    Select2<Contact,
                        InnerJoin<INSite,
                            On2<INSite.FK.Contact,
                            And<INSite.siteID, Equal<Current<SOOrder.destinationSiteID>>>>,
                        LeftJoin<SOShippingContact,
                            On<SOShippingContact.customerID, Equal<Contact.bAccountID>,
                            And<SOShippingContact.customerContactID, Equal<Contact.contactID>,
                            And<SOShippingContact.revisionID, Equal<Contact.revisionID>,
                            And<SOShippingContact.isDefaultContact, Equal<True>>>>>>>,
                        Where<True, Equal<True>>>));
                view = sender.Graph.TypedViews.GetView(altSelect, false);
            }
            else
			{
                view = sender.Graph.TypedViews.GetView(_Select, false);
            }
            int startRow = -1;
            int totalRows = 0;
            bool contactFound = false;

            foreach (PXResult res in view.Select(new object[] { DocumentRow }, null, null, null, null, null, ref startRow, 1, ref totalRows))
			{
                contactFound = DefaultContact<TContact, TContactID>(sender, FieldName, DocumentRow, AddressRow, res);
                break;
			}

            if (!contactFound && !_Required)
                this.ClearRecord(sender, DocumentRow);

            if(!contactFound && _Required && issitebranch)
                throw new SharedRecordMissingException();
		}
	}

	public class SOShipmentContactAttribute : ContactAttribute
	{
		public SOShipmentContactAttribute(Type SelectType)
			: base(typeof(SOShipmentContact.contactID), typeof(SOShipmentContact.isDefaultContact), SelectType)
		{
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			sender.Graph.FieldVerifying.AddHandler<SOShipmentContact.overrideContact>(Record_Override_FieldVerifying);
		}

		public override void DefaultRecord(PXCache sender, object DocumentRow, object Row)
		{
            SOShipment shipment = DocumentRow as SOShipment;
            if (shipment != null && shipment.ShipmentType == SOShipmentType.Transfer)
            {
	            PXResult contactRecord = null;
				using(new PXReadBranchRestrictedScope())
	            {
	            contactRecord = PXSelectJoin<Contact,
                    InnerJoin<INSite,
                          On<INSite.FK.Contact>,
                    LeftJoin<SOShipmentContact, On<SOShipmentContact.customerID, Equal<Contact.bAccountID>,
                        And<SOShipmentContact.customerContactID, Equal<Contact.contactID>,
                        And<SOShipmentContact.revisionID, Equal<Contact.revisionID>,
                        And<SOShipmentContact.isDefaultContact, Equal<True>>>>>>>,
                    Where<INSite.siteID, Equal<Current<SOShipment.destinationSiteID>>>>.SelectMultiBound(sender.Graph, new object[] { DocumentRow });
				}
                DefaultContact<SOShipmentContact, SOShipmentContact.contactID>(sender, FieldName, DocumentRow, Row, contactRecord);

            }
            else
            {
                DefaultContact<SOShipmentContact, SOShipmentContact.contactID>(sender, DocumentRow, Row);
            }
		}
		public override void CopyRecord(PXCache sender, object DocumentRow, object SourceRow, bool clone)
		{
			CopyContact<SOShipmentContact, SOShipmentContact.contactID>(sender, DocumentRow, SourceRow, clone);
		}
		public override void Record_IsDefault_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
		}

		public virtual void Record_Override_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			e.NewValue = (e.NewValue == null ? e.NewValue : (bool?)e.NewValue == false);
			try
			{
				Contact_IsDefaultContact_FieldVerifying<SOShipmentContact>(sender, e);
			}
			finally
			{
				e.NewValue = (e.NewValue == null ? e.NewValue : (bool?)e.NewValue == false);
			}
		}

		protected override void Record_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			base.Record_RowSelected(sender, e);

			if (e.Row != null)
			{
				PXUIFieldAttribute.SetEnabled<SOShipmentContact.overrideContact>(sender, e.Row, true);
			}
		}
	}

	public abstract class SOAddressAttribute : AddressAttribute
	{
		#region State
		BqlCommand _DuplicateSelect = BqlCommand.CreateInstance(typeof(Select<SOAddress, Where<SOAddress.customerID, Equal<Required<SOAddress.customerID>>, And<SOAddress.customerAddressID, Equal<Required<SOAddress.customerAddressID>>, And<SOAddress.revisionID, Equal<Required<SOAddress.revisionID>>, And<SOAddress.isDefaultAddress, Equal<boolTrue>>>>>>));
		#endregion
		#region Ctor
		public SOAddressAttribute(Type AddressIDType, Type IsDefaultAddressType, Type SelectType)
			: base(AddressIDType, IsDefaultAddressType, SelectType)
		{
		}
		#endregion
		#region Implementation
		public override void Record_RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Insert && ((SOAddress)e.Row).IsDefaultAddress == true)
			{
				PXView view = sender.Graph.TypedViews.GetView(_DuplicateSelect, true);
				view.Clear();

				SOAddress prev_address = (SOAddress)view.SelectSingle(((SOAddress)e.Row).CustomerID, ((SOAddress)e.Row).CustomerAddressID, ((SOAddress)e.Row).RevisionID);
				if (prev_address != null)
				{
					_KeyToAbort = sender.GetValue(e.Row, _RecordID);
					object newkey = sender.Graph.Caches[typeof(SOAddress)].GetValue(prev_address, _RecordID);

					PXCache cache = sender.Graph.Caches[_ItemType];

					foreach (object data in cache.Updated)
					{
						object datakey = cache.GetValue(data, _FieldOrdinal);
						if (Equals(_KeyToAbort, datakey))
						{
							cache.SetValue(data, _FieldOrdinal, newkey);
						}
					}

					_KeyToAbort = null;
					e.Cancel = true;
					return;
				}
			}
			base.Record_RowPersisting(sender, e);
		}

		public override void RowPersisting(PXCache sender, PXRowPersistingEventArgs e)
		{
			object key = sender.GetValue(e.Row, _FieldOrdinal);
			if (key != null)
			{
				PXCache cache = sender.Graph.Caches[_RecordType];
				if (Convert.ToInt32(key) < 0)
				{
					foreach (object data in cache.Inserted)
					{
						object datakey = cache.GetValue(data, _RecordID);
						if (Equals(key, datakey))
						{
							if (((SOAddress)data).IsDefaultAddress == true)
							{
								PXView view = sender.Graph.TypedViews.GetView(_DuplicateSelect, true);
								view.Clear();

								SOAddress prev_address = (SOAddress)view.SelectSingle(((SOAddress)data).CustomerID, ((SOAddress)data).CustomerAddressID, ((SOAddress)data).RevisionID);

								if (prev_address != null)
								{
									_KeyToAbort = sender.GetValue(e.Row, _FieldOrdinal);
									object id = sender.Graph.Caches[typeof(SOAddress)].GetValue(prev_address, _RecordID);
									sender.SetValue(e.Row, _FieldOrdinal, id);
								}
							}
							break;
						}
					}
				}
			}
			base.RowPersisting(sender, e);
		}
		#endregion
	}

	public class SOBillingAddressAttribute : SOAddressAttribute
	{
		public SOBillingAddressAttribute(Type SelectType)
			: base(typeof(SOBillingAddress.addressID), typeof(SOBillingAddress.isDefaultAddress), SelectType)
		{
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			sender.Graph.FieldVerifying.AddHandler<SOBillingAddress.overrideAddress>(Record_Override_FieldVerifying);
		}

		public override void DefaultRecord(PXCache sender, object DocumentRow, object Row)
		{
			DefaultAddress<SOBillingAddress, SOBillingAddress.addressID>(sender, DocumentRow, Row);
		}

		public override void CopyRecord(PXCache sender, object DocumentRow, object SourceRow, bool clone)
		{
			CopyAddress<SOBillingAddress, SOBillingAddress.addressID>(sender, DocumentRow, SourceRow, clone);
		}

		public override void Record_IsDefault_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
		}

		public virtual void Record_Override_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			e.NewValue = (e.NewValue == null ? e.NewValue : (bool?)e.NewValue == false);
			try
			{
				Address_IsDefaultAddress_FieldVerifying<SOBillingAddress>(sender, e);
			}
			finally
			{
				e.NewValue = (e.NewValue == null ? e.NewValue : (bool?)e.NewValue == false);
			}
		}

		protected override void Record_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			base.Record_RowSelected(sender, e);

			if (e.Row != null)
			{
				PXUIFieldAttribute.SetEnabled<SOBillingAddress.overrideAddress>(sender, e.Row, true);
				PXUIFieldAttribute.SetEnabled<SOBillingAddress.isValidated>(sender, e.Row, false);
			}
		}
	}

	public class SOShippingAddressAttribute : SOAddressAttribute
	{
		public SOShippingAddressAttribute(Type SelectType)
			: base(typeof(SOShippingAddress.addressID), typeof(SOShippingAddress.isDefaultAddress), SelectType)
		{
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			sender.Graph.FieldVerifying.AddHandler<SOShippingAddress.overrideAddress>(Record_Override_FieldVerifying);
		}

		public override void DefaultRecord(PXCache sender, object DocumentRow, object Row)
		{
			DefaultAddress<SOShippingAddress, SOShippingAddress.addressID>(sender, DocumentRow, Row);
		}
		public override void CopyRecord(PXCache sender, object DocumentRow, object SourceRow, bool clone)
		{
			CopyAddress<SOShippingAddress, SOShippingAddress.addressID>(sender, DocumentRow, SourceRow, clone);
		}
		public override void Record_IsDefault_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
		}

		public virtual void Record_Override_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			e.NewValue = (e.NewValue == null ? e.NewValue : (bool?)e.NewValue == false);
			try
			{
				Address_IsDefaultAddress_FieldVerifying<SOShippingAddress>(sender, e);
			}
			finally
			{
				e.NewValue = (e.NewValue == null ? e.NewValue : (bool?)e.NewValue == false);
			}
		}

		protected override void Record_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			base.Record_RowSelected(sender, e);

			if (e.Row != null)
			{
				PXUIFieldAttribute.SetEnabled<SOShippingAddress.overrideAddress>(sender, e.Row, true);
			}
		}

		public override void DefaultAddress<TAddress, TAddressID>(PXCache sender, object DocumentRow, object AddressRow)
		{
            int? destinationSiteID = (int?)sender.GetValue<SOOrder.destinationSiteID>(DocumentRow);
            PXView view;
            bool issitebranch = false;
            if (destinationSiteID != null)
			{
                issitebranch = true;
                BqlCommand altSelect = BqlCommand.CreateInstance(
                    typeof(Select2<Address,
                                    InnerJoin<INSite,
                                        On2<INSite.FK.Address,
                                        And<INSite.siteID, Equal<Current<SOOrder.destinationSiteID>>>>,
                                    LeftJoin<SOShippingAddress,
                                        On<SOShippingAddress.customerID, Equal<Address.bAccountID>,
                                        And<SOShippingAddress.customerAddressID, Equal<Address.addressID>,
                                        And<SOShippingAddress.revisionID, Equal<Address.revisionID>,
                                        And<SOShippingAddress.isDefaultAddress, Equal<True>>>>>>>,
                                    Where<True, Equal<True>>>));
                view = sender.Graph.TypedViews.GetView(altSelect, false);
			}
            else
            {
                view = sender.Graph.TypedViews.GetView(_Select, false);
		}

            int startRow = -1;
            int totalRows = 0;
            bool addressFind = false;
            foreach (PXResult res in view.Select(new object[] { DocumentRow }, null, null, null, null, null, ref startRow, 1, ref totalRows))
            {
                addressFind = DefaultAddress<TAddress, TAddressID>(sender, FieldName, DocumentRow, AddressRow, res);
                break;
            }

            if (!addressFind && !_Required)
                this.ClearRecord(sender, DocumentRow);

            if (!addressFind && _Required && issitebranch)
                throw new SharedRecordMissingException();
        }
	}

	public class SOShipmentAddressAttribute : AddressAttribute
	{
		public SOShipmentAddressAttribute(Type SelectType)
			: base(typeof(SOShipmentAddress.addressID), typeof(SOShipmentAddress.isDefaultAddress), SelectType)
		{
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			sender.Graph.FieldVerifying.AddHandler<SOShipmentAddress.overrideAddress>(Record_Override_FieldVerifying);
		}

		public override void DefaultRecord(PXCache sender, object DocumentRow, object Row)
		{
            SOShipment shipment = DocumentRow as SOShipment;
            if (shipment != null && shipment.ShipmentType == SOShipmentType.Transfer)
            {
	            PXResult addressRecord;
	            using (new PXReadBranchRestrictedScope())
	            {
	            addressRecord = PXSelectJoin<Address,
                    InnerJoin<INSite,
                                On<INSite.FK.Address>,
                    LeftJoin<SOShipmentAddress, On<SOShipmentAddress.customerID, Equal<Address.bAccountID>,
                            And<SOShipmentAddress.customerAddressID, Equal<Address.addressID>,
                            And<SOShipmentAddress.revisionID, Equal<Address.revisionID>,
                            And<SOShipmentAddress.isDefaultAddress, Equal<True>>>>>>>,
                    Where<INSite.siteID, Equal<Current<SOShipment.destinationSiteID>>>>.SelectMultiBound(sender.Graph, new object[] { DocumentRow });

                AddressAttribute.DefaultAddress<SOShipmentAddress, SOShipmentAddress.addressID>(sender, FieldName, DocumentRow, Row, addressRecord);
            }
			}
            else
            {
                DefaultAddress<SOShipmentAddress, SOShipmentAddress.addressID>(sender, DocumentRow, Row);
            }
		}
		public override void CopyRecord(PXCache sender, object DocumentRow, object SourceRow, bool clone)
		{
			CopyAddress<SOShipmentAddress, SOShipmentAddress.addressID>(sender, DocumentRow, SourceRow, clone);
		}
		public override void Record_IsDefault_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
		}

		public virtual void Record_Override_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			e.NewValue = (e.NewValue == null ? e.NewValue : (bool?)e.NewValue == false);
			try
			{
				Address_IsDefaultAddress_FieldVerifying<SOShipmentAddress>(sender, e);
			}
			finally
			{
				e.NewValue = (e.NewValue == null ? e.NewValue : (bool?)e.NewValue == false);
			}
		}

		protected override void Record_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			base.Record_RowSelected(sender, e);

			if (e.Row != null)
			{
				PXUIFieldAttribute.SetEnabled<SOShipmentAddress.overrideAddress>(sender, e.Row, true);
				PXUIFieldAttribute.SetEnabled<SOShipmentAddress.isValidated>(sender, e.Row, false);
			}
		}
	}

	public class SOUnbilledTax4Attribute : SOUnbilledTax2Attribute
	{
		public SOUnbilledTax4Attribute(Type ParentType, Type TaxType, Type TaxSumType)
			: base(ParentType, TaxType, TaxSumType)
		{
			this._CuryTaxableAmt = typeof(SOTax.curyUnbilledTaxableAmt).Name;
			this._CuryExemptedAmt = typeof(SOTax.curyUnbilledTaxableAmt).Name;
			this._CuryTaxAmt = typeof(SOTax.curyUnbilledTaxAmt).Name;

			this.CuryTranAmt = typeof(SOLine4.curyUnbilledAmt);

			this._Attributes.Clear();
			this._Attributes.Add(new PXUnboundFormulaAttribute(
				typeof(Switch<Case<Where<SOLine4.operation, Equal<Parent<SOOrder.defaultOperation>>>, SOLine4.curyUnbilledAmt>, Data.Minus<SOLine4.curyUnbilledAmt>>),
				typeof(SumCalc<SOOrder.curyUnbilledLineTotal>)));
			this._Attributes.Add(new PXUnboundFormulaAttribute(typeof(Mult<
				Mult<Switch<Case<Where<SOLine4.operation, Equal<Parent<SOOrder.defaultOperation>>>, decimal1>, Data.Minus<decimal1>>, SOLine4.curyUnbilledAmt>,
				Sub<decimal1, Mult<SOLine4.groupDiscountRate, SOLine4.documentDiscountRate>>>),
				typeof(SumCalc<SOOrder.curyUnbilledDiscTotal>)));
			
		}

		protected override List<object> SelectTaxes<Where>(PXGraph graph, object row, PXTaxCheck taxchk, params object[] parameters)
		{
			return SelectTaxes<Where, Where<True, Equal<True>>, Current<SOLine4.lineNbr>>(graph, new object[] { row, graph.Caches[_ParentType].Current }, taxchk, parameters);
		}
	}

	public class SOUnbilledTax2Attribute : SOUnbilledTaxAttribute
	{
		public SOUnbilledTax2Attribute(Type ParentType, Type TaxType, Type TaxSumType)
			: base(ParentType, TaxType, TaxSumType)
		{
			this._CuryTaxableAmt = typeof(SOTax.curyUnbilledTaxableAmt).Name;
			this._CuryExemptedAmt = typeof(SOTax.curyUnbilledTaxableAmt).Name;
			this._CuryTaxAmt = typeof(SOTax.curyUnbilledTaxAmt).Name;

			this.CuryTranAmt = typeof(SOLine2.curyUnbilledAmt);

			this._Attributes.Clear();
			this._Attributes.Add(new PXUnboundFormulaAttribute(
				typeof(Switch<Case<Where<SOLine2.operation, Equal<Parent<SOOrder.defaultOperation>>>, SOLine2.curyUnbilledAmt>, Data.Minus<SOLine2.curyUnbilledAmt>>),
				typeof(SumCalc<SOOrder.curyUnbilledLineTotal>)));
			this._Attributes.Add(new PXUnboundFormulaAttribute(typeof(Mult<
				Mult<Switch<Case<Where<SOLine2.operation, Equal<Parent<SOOrder.defaultOperation>>>, decimal1>, Data.Minus<decimal1>>, SOLine2.curyUnbilledAmt>,
				Sub<decimal1, Mult<SOLine2.groupDiscountRate, SOLine2.documentDiscountRate>>>),
				typeof(SumCalc<SOOrder.curyUnbilledDiscTotal>)));
		}

		protected override List<object> SelectTaxes<Where>(PXGraph graph, object row, PXTaxCheck taxchk, params object[] parameters)
		{
			return SelectTaxes<Where, Where<True, Equal<True>>, Current<SOLine2.lineNbr>>(graph, new object[] { row, graph.Caches[_ParentType].Current }, taxchk, parameters);
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			_ChildType = sender.GetItemType();
			TaxCalc = TaxCalc.Calc;
			//sender.Graph.FieldUpdated.AddHandler(sender.GetItemType(), _CuryTranAmt, CuryUnbilledAmt_FieldUpdated);
		}

		//public virtual void CuryUnbilledAmt_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		//{
		//	sender.Graph.Caches[_ParentType].Current = PXParentAttribute.SelectParent(sender, e.Row);
		//	CalcTaxes(sender, e.Row, PXTaxCheck.Line);
		//}

		public override void RowInserted(PXCache sender, PXRowInsertedEventArgs e)
		{
			sender.Graph.Caches[_ParentType].Current = PXParentAttribute.SelectParent(sender, e.Row);
			base.RowInserted(sender, e);
		}

		public override void RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			sender.Graph.Caches[_ParentType].Current = PXParentAttribute.SelectParent(sender, e.Row);
			base.RowUpdated(sender, e);
		}

		public override void RowDeleted(PXCache sender, PXRowDeletedEventArgs e)
		{
			sender.Graph.Caches[_ParentType].Current = PXParentAttribute.SelectParent(sender, e.Row);
			base.RowDeleted(sender, e);
		}
	}

	/// <summary>
	/// Extends <see cref="SOTaxAttribute"/> and calculates CuryUnbilledOrderTotal and OpenDoc for the Parent(Header) SOOrder.
	/// </summary>
	/// <example>
	/// [SOUnbilledTax(typeof(SOOrder), typeof(SOTax), typeof(SOTaxTran), TaxCalc = TaxCalc.ManualLineCalc)]
	/// </example>
	public class SOUnbilledTaxAttribute : SOTaxAttribute
	{
		protected override short SortOrder
		{
			get
			{
				return 2;
			}
		}

		public SOUnbilledTaxAttribute(Type ParentType, Type TaxType, Type TaxSumType)
			: base(ParentType, TaxType, TaxSumType)
		{
			this._CuryTaxableAmt = typeof(SOTax.curyUnbilledTaxableAmt).Name;
			this._CuryExemptedAmt = typeof(SOTax.curyUnbilledTaxableAmt).Name;
			this._CuryTaxAmt = typeof(SOTax.curyUnbilledTaxAmt).Name;

			this.CuryDocBal = null;
			this.CuryLineTotal = typeof(SOOrder.curyUnbilledLineTotal);
			this.CuryTaxTotal = typeof(SOOrder.curyUnbilledTaxTotal);
			this.DocDate = typeof(SOOrder.orderDate);
			this.CuryTranAmt = typeof(SOLine.curyUnbilledAmt);

            this._Attributes.Clear();
			this._Attributes.Add(new PXUnboundFormulaAttribute(
				typeof(Switch<Case<Where<SOLine.lineType, NotEqual<SOLineType.miscCharge>>, Switch<Case<Where<SOLine.operation, Equal<Current<SOOrder.defaultOperation>>>, SOLine.curyUnbilledAmt>, Data.Minus<SOLine.curyUnbilledAmt>>>, decimal0>),
				typeof(SumCalc<SOOrder.curyUnbilledLineTotal>)));
			this._Attributes.Add(new PXUnboundFormulaAttribute(
				typeof(Switch<Case<Where<SOLine.lineType, Equal<SOLineType.miscCharge>>, Switch<Case<Where<SOLine.operation, Equal<Current<SOOrder.defaultOperation>>>, SOLine.curyUnbilledAmt>, Data.Minus<SOLine.curyUnbilledAmt>>>, decimal0>),
				typeof(SumCalc<SOOrder.curyUnbilledMiscTot>)));
			this._Attributes.Add(new PXUnboundFormulaAttribute(typeof(Mult<
				Mult<Switch<Case<Where<SOLine.operation, Equal<Parent<SOOrder.defaultOperation>>>, decimal1>, Data.Minus<decimal1>>, SOLine.curyUnbilledAmt>,
				Sub<decimal1, Mult<SOLine.groupDiscountRate, SOLine.documentDiscountRate>>>),
				typeof(SumCalc<SOOrder.curyUnbilledDiscTotal>)));
		}

		#region Per Unit Taxes
		protected override string TaxableQtyFieldNameForTaxDetail => nameof(SOTax.UnbilledTaxableQty);
		#endregion

		protected override void CalcDocTotals(
			PXCache sender, 
			object row, 
			decimal CuryTaxTotal, 
			decimal CuryInclTaxTotal, 
			decimal CuryWhTaxTotal,
			decimal CuryTaxDiscountTotal)
		{
			_CalcDocTotals(sender, row, CuryTaxTotal, CuryInclTaxTotal, CuryWhTaxTotal, CuryTaxDiscountTotal);

			decimal CuryLineTotal = (decimal)(ParentGetValue<SOOrder.curyLineTotal>(sender.Graph) ?? 0m);
			decimal CuryMiscTotal = (decimal)(ParentGetValue<SOOrder.curyMiscTot>(sender.Graph) ?? 0m);
			decimal CuryFreightTotal = (decimal)(ParentGetValue<SOOrder.curyFreightTot>(sender.Graph) ?? 0m);

			decimal CuryUnbilledLineTotal = (decimal)(ParentGetValue<SOOrder.curyUnbilledLineTotal>(sender.Graph) ?? 0m);
			decimal CuryUnbilledMiscTotal = (decimal)(ParentGetValue<SOOrder.curyUnbilledMiscTot>(sender.Graph) ?? 0m);
			decimal CuryUnbilledFreightTotal = CalcUnbilledFreightTotal(sender, CuryFreightTotal, CuryLineTotal, CuryUnbilledLineTotal);
			decimal CuryUnbilledDiscTotal = (decimal)(ParentGetValue<SOOrder.curyUnbilledDiscTotal>(sender.Graph) ?? 0m);

			CuryLineTotal += CuryMiscTotal;
			CuryUnbilledLineTotal += CuryUnbilledMiscTotal;

			decimal CuryUnbilledDocTotal = CuryUnbilledLineTotal + CuryUnbilledFreightTotal + CuryTaxTotal - CuryInclTaxTotal - CuryUnbilledDiscTotal;

			if (object.Equals(CuryUnbilledDocTotal, (decimal)(ParentGetValue<SOOrder.curyUnbilledOrderTotal>(sender.Graph) ?? 0m)) == false)
			{
				ParentSetValue<SOOrder.curyUnbilledOrderTotal>(sender.Graph, CuryUnbilledDocTotal);
				ParentSetValue<SOOrder.openDoc>(sender.Graph, (CuryUnbilledDocTotal > 0m)); 
			}
		}

		protected virtual decimal CalcUnbilledFreightTotal(PXCache sender, decimal curyFreightTot, decimal curyLineTot, decimal curyUnbilledLineTot)
		{
			int? releasedCntr = (int?)ParentGetValue<SOOrder.releasedCntr>(sender.Graph);
			int? billedCntr = (int?)ParentGetValue<SOOrder.billedCntr>(sender.Graph);
			if (releasedCntr + billedCntr > 0)
			{
				SOSetup sosetup = PXSetup<SOSetup>.Select(sender.Graph);
				return (sosetup.FreightAllocation == FreightAllocationList.FullAmount || curyLineTot == 0m) ? 0m
					: PXCurrencyAttribute.RoundCury(ParentCache(sender.Graph), ParentRow(sender.Graph), curyFreightTot * curyUnbilledLineTot / curyLineTot);
			}
			else
			{
				return curyFreightTot;
			}
		}

		protected override bool IsFreightTaxable(PXCache sender, List<object> taxitems)
		{
			if (taxitems.Count > 0)
			{
				List<object> items = base.SelectTaxes<Where<Tax.taxID, Equal<Required<Tax.taxID>>>>(sender.Graph, null, PXTaxCheck.RecalcLine, ((SOTax)(PXResult<SOTax>)taxitems[0]).TaxID);

				return base.IsFreightTaxable(sender, items);
			}
			else
			{
				return false;
			}
		}

		protected override List<object> SelectTaxes<Where>(PXGraph graph, object row, PXTaxCheck taxchk, params object[] parameters)
		{
			return SelectTaxes<Where, Where<True, Equal<True>>, Current<SOLine.lineNbr>>(graph, new object[] { row, graph.Caches[_ParentType].Current }, taxchk, parameters);
		}

		protected override decimal? GetDocLineFinalAmtNoRounding(PXCache sender, object row, string TaxCalcType = "I")
		{
			var extPrice = (decimal?)sender.GetValue(row, typeof(SOLine.curyUnbilledAmt).Name);
			var docDiscount = (decimal?)sender.GetValue(row, typeof(SOLine.documentDiscountRate).Name);
			var groupDiscount = (decimal?)sender.GetValue(row, typeof(SOLine.groupDiscountRate).Name);
			var value = extPrice * docDiscount * groupDiscount;
			return value;
		}

		//Only base attribute should re-default taxes
		protected override void ReDefaultTaxes(PXCache cache, List<object> details)
		{
		}
		protected override void ReDefaultTaxes(PXCache cache, object clearDet, object defaultDet, bool defaultExisting = true)
		{
		}
	}

	public class SOOpenTax4Attribute : SOOpenTaxAttribute
	{
		public SOOpenTax4Attribute(Type ParentType, Type TaxType, Type TaxSumType)
			: base(ParentType, TaxType, TaxSumType)
		{
			this._CuryTaxableAmt = typeof(SOTax.curyUnshippedTaxableAmt).Name;
			this._CuryTaxAmt = typeof(SOTax.curyUnshippedTaxAmt).Name;

			this.CuryTranAmt = typeof(SOLine4.curyOpenAmt);

			this._Attributes.Clear();
			this._Attributes.Add(new PXUnboundFormulaAttribute(
				typeof(Switch<Case<Where<SOLine4.operation, Equal<Parent<SOOrder.defaultOperation>>>, SOLine4.curyOpenAmt>, Data.Minus<SOLine4.curyOpenAmt>>),
				typeof(SumCalc<SOOrder.curyOpenLineTotal>)));
			this._Attributes.Add(new PXUnboundFormulaAttribute(typeof(Mult<
				Mult<Switch<Case<Where<SOLine4.operation, Equal<Parent<SOOrder.defaultOperation>>>, decimal1>, Data.Minus<decimal1>>, SOLine4.curyOpenAmt>,
				Sub<decimal1, Mult<SOLine4.groupDiscountRate, SOLine4.documentDiscountRate>>>),
				typeof(SumCalc<SOOrder.curyOpenDiscTotal>)));
		}

		protected override List<object> SelectTaxes<Where>(PXGraph graph, object row, PXTaxCheck taxchk, params object[] parameters)
		{
			return SelectTaxes<Where, Where<True, Equal<True>>, Current<SOLine4.lineNbr>>(graph, new object[] { row, graph.Caches[_ParentType].Current }, taxchk, parameters);
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			_ChildType = sender.GetItemType();
			TaxCalc = TaxCalc.Calc;
			sender.Graph.FieldUpdated.AddHandler(sender.GetItemType(), _CuryTranAmt, CuryUnbilledAmt_FieldUpdated);
		}

		public virtual void CuryUnbilledAmt_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			sender.Graph.Caches[_ParentType].Current = PXParentAttribute.SelectParent(sender, e.Row);
			CalcTaxes(sender, e.Row, PXTaxCheck.Line);
		}

		public override void RowInserted(PXCache sender, PXRowInsertedEventArgs e)
		{
			sender.Graph.Caches[_ParentType].Current = PXParentAttribute.SelectParent(sender, e.Row);
			base.RowInserted(sender, e);
		}

		public override void RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			sender.Graph.Caches[_ParentType].Current = PXParentAttribute.SelectParent(sender, e.Row);
			base.RowUpdated(sender, e);
		}

		public override void RowDeleted(PXCache sender, PXRowDeletedEventArgs e)
		{
			sender.Graph.Caches[_ParentType].Current = PXParentAttribute.SelectParent(sender, e.Row);
			base.RowDeleted(sender, e);
		}
	}


	/// <summary>
	/// Extends <see cref="SOTaxAttribute"/> and calculates CuryOpenOrderTotal for the Parent(Header) SOOrder.
	/// </summary>
	/// <example>
	/// [SOOpenTax(typeof(SOOrder), typeof(SOTax), typeof(SOTaxTran), TaxCalc = TaxCalc.ManualLineCalc)]
	/// </example>
	public class SOOpenTaxAttribute : SOTaxAttribute
	{
		protected override short SortOrder
		{
			get
			{
				return 1;
			}
		}

		public SOOpenTaxAttribute(Type ParentType, Type TaxType, Type TaxSumType)
			: base(ParentType, TaxType, TaxSumType)
		{
			this._CuryTaxableAmt = typeof(SOTax.curyUnshippedTaxableAmt).Name;
			this._CuryExemptedAmt = typeof(SOTax.curyUnshippedTaxableAmt).Name;
			this._CuryTaxAmt = typeof(SOTax.curyUnshippedTaxAmt).Name;

			this.CuryDocBal = typeof(SOOrder.curyOpenOrderTotal);
			this.CuryLineTotal = typeof(SOOrder.curyOpenLineTotal);
			this.CuryTaxTotal = typeof(SOOrder.curyOpenTaxTotal);
			this.DocDate = typeof(SOOrder.orderDate);
			this.CuryTranAmt = typeof(SOLine.curyOpenAmt);

			this._Attributes.Clear();
			this._Attributes.Add(new PXUnboundFormulaAttribute(
				typeof(Switch<Case<Where<SOLine.operation, Equal<Current<SOOrder.defaultOperation>>>, SOLine.curyOpenAmt>, Data.Minus<SOLine.curyOpenAmt>>),
				typeof(SumCalc<SOOrder.curyOpenLineTotal>)));
			this._Attributes.Add(new PXUnboundFormulaAttribute(typeof(Mult<
				Mult<Switch<Case<Where<SOLine.operation, Equal<Parent<SOOrder.defaultOperation>>>, decimal1>, Data.Minus<decimal1>>, SOLine.curyOpenAmt>,
				Sub<decimal1, Mult<SOLine.groupDiscountRate, SOLine.documentDiscountRate>>>),
				typeof(SumCalc<SOOrder.curyOpenDiscTotal>)));
		}

		#region Per Unit Taxes
		protected override string TaxableQtyFieldNameForTaxDetail => nameof(SOTax.UnshippedTaxableQty);
		#endregion

		protected override List<object> SelectTaxes<Where>(PXGraph graph, object row, PXTaxCheck taxchk, params object[] parameters)
		{
			return SelectTaxes<Where, Where<True, Equal<True>>, Current<SOLine.lineNbr>>(graph, new object[] { row, graph.Caches[_ParentType].Current }, taxchk, parameters);
		}

		protected override decimal? GetDocLineFinalAmtNoRounding(PXCache sender, object row, string TaxCalcType = "I")
		{
			var extPrice = (decimal?)sender.GetValue(row, typeof(SOLine.curyOpenAmt).Name);
			var docDiscount = (decimal?)sender.GetValue(row, typeof(SOLine.documentDiscountRate).Name);
			var groupDiscount = (decimal?)sender.GetValue(row, typeof(SOLine.groupDiscountRate).Name);
			var value = extPrice * docDiscount * groupDiscount;
			return value;
		}

		protected override void CalcDocTotals(
			PXCache sender, 
			object row, 
			decimal CuryTaxTotal, 
			decimal CuryInclTaxTotal, 
			decimal CuryWhTaxTotal,
			decimal CuryTaxDiscountTotal)
		{
			_CalcDocTotals(sender, row, CuryTaxTotal, CuryInclTaxTotal, CuryWhTaxTotal, CuryTaxDiscountTotal);

			decimal CuryLineTotal = (decimal)(ParentGetValue<SOOrder.curyLineTotal>(sender.Graph) ?? 0m);
			decimal CuryMiscTotal = (decimal)(ParentGetValue<SOOrder.curyMiscTot>(sender.Graph) ?? 0m);
			decimal CuryFreightTotal = (decimal)(ParentGetValue<SOOrder.curyFreightTot>(sender.Graph) ?? 0m);
			decimal CuryOpenDiscTotal = (decimal)(ParentGetValue<SOOrder.curyOpenDiscTotal>(sender.Graph) ?? 0m);

			CuryLineTotal += CuryMiscTotal + CuryFreightTotal;

			decimal CuryOpenLineTotal = (decimal)(ParentGetValue<SOOrder.curyOpenLineTotal>(sender.Graph) ?? 0m);
			decimal CuryOpenDocTotal = CuryOpenLineTotal + CuryTaxTotal - CuryInclTaxTotal - CuryOpenDiscTotal;

			if (object.Equals(CuryOpenDocTotal, (decimal)(ParentGetValue<SOOrder.curyOpenOrderTotal>(sender.Graph) ?? 0m)) == false)
			{
				ParentSetValue<SOOrder.curyOpenOrderTotal>(sender.Graph, CuryOpenDocTotal);
			}
		}

		protected override bool IsFreightTaxable(PXCache sender, List<object> taxitems)
		{
			if (taxitems.Count > 0)
			{
				List<object> items = base.SelectTaxes<Where<Tax.taxID, Equal<Required<Tax.taxID>>>>(sender.Graph, null, PXTaxCheck.RecalcLine, ((SOTax)(PXResult<SOTax>)taxitems[0]).TaxID);

				return base.IsFreightTaxable(sender, items);
			}
			else
			{
				return false;
			}
		}

		//Only base attribute should re-default taxes
		protected override void ReDefaultTaxes(PXCache cache, List<object> details)
		{
		}
		protected override void ReDefaultTaxes(PXCache cache, object clearDet, object defaultDet, bool defaultExisting = true)
		{
		}
	}

	/// <summary>
	/// Extends <see cref="SOTaxAttribute"/> and calculates CuryOrderTotal and CuryTaxTotal for the Parent(Header) SOOrder.
	/// This Attribute overrides some of functionality of <see cref="SOTaxAttribute"/>. 
	/// This Attribute is applied to the TaxCategoryField of SOOrder instead of SO Line.
	/// </summary>
	/// <example>
	/// [SOOrderTax(typeof(SOOrder), typeof(SOTax), typeof(SOTaxTran), TaxCalc = TaxCalc.ManualLineCalc)]
	/// </example>
	public class SOOrderTaxAttribute : SOTaxAttribute
	{
		public SOOrderTaxAttribute(Type ParentType, Type TaxType, Type TaxSumType)
			: this(ParentType, TaxType, TaxSumType, null)
		{
		}

		public SOOrderTaxAttribute(Type ParentType, Type TaxType, Type TaxSumType, Type TaxCalculationMode)
			: base(ParentType, TaxType, TaxSumType, TaxCalculationMode)
		{
			CuryTranAmt = typeof(SOOrder.curyFreightTot);
			TaxCategoryID = typeof(SOOrder.freightTaxCategoryID);

			this._Attributes.Clear();
		}

		protected override object InitializeTaxDet(object data)
		{
			object new_data =  base.InitializeTaxDet(data);
			if (new_data.GetType() == _TaxType)
			{
				((SOTax)new_data).LineNbr = 32000;
			}

			return new_data;
		}

        protected override decimal? GetCuryTranAmt(PXCache sender, object row, string TaxCalcType)
		{
			return (decimal?)sender.GetValue(row, _CuryTranAmt);
		}

		protected override List<object> SelectTaxes<Where>(PXGraph graph, object row, PXTaxCheck taxchk, params object[] parameters)
		{
			return SelectTaxes<Where, Where<True, Equal<True>>, int32000>(graph, new object[] { row, graph.Caches[_ParentType].Current }, taxchk, parameters);
		}

		protected override void CalcDocTotals(
			PXCache sender, 
			object row, 
			decimal CuryTaxTotal, 
			decimal CuryInclTaxTotal, 
			decimal CuryWhTaxTotal,
			decimal CuryTaxDiscountTotal)
		{
			decimal CuryLineTotal = (decimal)(ParentGetValue<SOOrder.curyLineTotal>(sender.Graph) ?? 0m);
			decimal CuryMiscTotal = (decimal)(ParentGetValue<SOOrder.curyMiscTot>(sender.Graph) ?? 0m);
			decimal CuryFreightTotal = (decimal)(ParentGetValue<SOOrder.curyFreightTot>(sender.Graph) ?? 0m);
			decimal CuryDiscountTotal = (decimal)(ParentGetValue<SOOrder.curyDiscTot>(sender.Graph) ?? 0m);

			decimal CuryDocTotal = CuryLineTotal + CuryMiscTotal + CuryFreightTotal + CuryTaxTotal - CuryInclTaxTotal - CuryDiscountTotal;

			if (object.Equals(CuryDocTotal, (decimal)(ParentGetValue<SOOrder.curyOrderTotal>(sender.Graph) ?? 0m)) == false ||
				object.Equals(CuryTaxTotal, (decimal)(ParentGetValue<SOOrder.curyTaxTotal>(sender.Graph) ?? 0m)) == false)
			{
				ParentSetValue<SOOrder.curyOrderTotal>(sender.Graph, CuryDocTotal);
				ParentSetValue<SOOrder.curyTaxTotal>(sender.Graph, CuryTaxTotal);
			}
		}

		protected override void Tax_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
		}

		protected override void Tax_RowInserted(PXCache sender, PXRowInsertedEventArgs e)
		{
			if ((_TaxCalc == TaxCalc.Calc || _TaxCalc == TaxCalc.ManualLineCalc) && e.ExternalCall || _TaxCalc == TaxCalc.ManualCalc)
			{
				PXCache cache = sender.Graph.Caches[_ChildType];
				PXCache taxcache = sender.Graph.Caches[_TaxType];

				object det = ParentRow(sender.Graph);
				{
					ITaxDetail taxzonedet = MatchesCategory(cache, det, (ITaxDetail)e.Row);
					AddOneTax(taxcache, det, taxzonedet);
				}
				_NoSumTotals = (_TaxCalc == TaxCalc.ManualCalc && e.ExternalCall == false);

				PXRowDeleting del = delegate(PXCache _sender, PXRowDeletingEventArgs _e) { _e.Cancel |= object.ReferenceEquals(e.Row, _e.Row); };
				sender.Graph.RowDeleting.AddHandler(_TaxSumType, del);
				try
				{
					CalcTaxes(cache, null);
				}
				finally
				{
					sender.Graph.RowDeleting.RemoveHandler(_TaxSumType, del);
				}
				_NoSumTotals = false;
			}
		}

		protected override void Tax_RowDeleted(PXCache sender, PXRowDeletedEventArgs e)
		{
			if ((_TaxCalc == TaxCalc.Calc || _TaxCalc == TaxCalc.ManualLineCalc) && e.ExternalCall || _TaxCalc == TaxCalc.ManualCalc)
			{
				PXCache cache = sender.Graph.Caches[_ChildType];
				PXCache taxcache = sender.Graph.Caches[_TaxType];

				object det = ParentRow(sender.Graph);
				{
					DelOneTax(taxcache, det, e.Row);
				}
				CalcTaxes(cache, null);
			}
		}
		
		protected override void ZoneUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			if (_TaxCalc == TaxCalc.Calc || _TaxCalc == TaxCalc.ManualLineCalc)
			{
				if (!CompareZone(sender.Graph, (string)e.OldValue, (string)sender.GetValue(e.Row, _TaxZoneID)))
				{
					Preload(sender);

					ReDefaultTaxes(sender, e.Row, e.Row, false);

					_ParentRow = e.Row;
					CalcTaxes(sender, e.Row);
					_ParentRow = null;
				}
			}
		}

		protected override void DateUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			if (_TaxCalc == TaxCalc.Calc || _TaxCalc == TaxCalc.ManualLineCalc)
			{
				Preload(sender);

				ReDefaultTaxes(sender, e.Row, e.Row, false);

				_ParentRow = e.Row;
				CalcTaxes(sender, e.Row);
				_ParentRow = null;
			}
		}

	    protected override bool ShouldUpdateFinPeriodID(PXCache sender, object oldRow, object newRow)
	    {
	        return false;
	    }

        public override void RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			_ParentRow = e.Row;
			base.RowUpdated(sender, e);
			_ParentRow = null;
		}

		#region Per Unit Taxes Override Stubs	
		/// <summary>
		/// Fill tax details for line for per unit taxes. Do nothing for retained tax.
		/// </summary>
		protected override void TaxSetLineDefaultForPerUnitTaxes(PXCache rowCache, object row, Tax tax, TaxRev taxRevision, TaxDetail taxDetail)
		{
		}

		/// <summary>
		/// Fill aggregated tax detail for per unit tax. Do nothing for retained tax.
		/// </summary>
		protected override TaxDetail FillAggregatedTaxDetailForPerUnitTax(PXCache rowCache, object row, Tax tax, TaxRev taxRevision,
																		  TaxDetail aggrTaxDetail, List<object> taxItems)
		{
			return aggrTaxDetail;
		}
		#endregion
	}

	public class SOUnbilledMiscTax2Attribute : SOUnbilledTaxAttribute
	{
		public SOUnbilledMiscTax2Attribute(Type ParentType, Type TaxType, Type TaxSumType)
			: base(ParentType, TaxType, TaxSumType)
		{
			this._CuryTaxableAmt = typeof(SOTax.curyUnbilledTaxableAmt).Name;
			this._CuryExemptedAmt = typeof(SOTax.curyUnbilledTaxableAmt).Name;
			this._CuryTaxAmt = typeof(SOTax.curyUnbilledTaxAmt).Name;

			this.CuryDocBal = null;
			this.CuryLineTotal = typeof(SOOrder.curyUnbilledMiscTot);
			this.CuryTaxTotal = typeof(SOOrder.curyUnbilledTaxTotal);
			this.DocDate = typeof(SOOrder.orderDate);
			this.CuryTranAmt = typeof(SOMiscLine2.curyUnbilledAmt);

			this._Attributes.Clear();
			this._Attributes.Add(new PXUnboundFormulaAttribute(
				typeof(Switch<Case<Where<SOMiscLine2.operation, Equal<Parent<SOOrder.defaultOperation>>>, SOMiscLine2.curyUnbilledAmt>, Data.Minus<SOMiscLine2.curyUnbilledAmt>>),
				typeof(SumCalc<SOOrder.curyUnbilledMiscTot>)));
			this._Attributes.Add(new PXUnboundFormulaAttribute(typeof(Mult<
				Mult<Switch<Case<Where<SOMiscLine2.operation, Equal<Parent<SOOrder.defaultOperation>>>, decimal1>, Data.Minus<decimal1>>, SOMiscLine2.curyUnbilledAmt>,
				Sub<decimal1, Mult<SOMiscLine2.groupDiscountRate, SOMiscLine2.documentDiscountRate>>>),
				typeof(SumCalc<SOOrder.curyUnbilledDiscTotal>)));
		}

		protected override decimal? GetCuryTranAmt(PXCache sender, object row, string TaxCalcType)
		{
			return (decimal?)sender.GetValue(row, _CuryTranAmt);
		}

		protected override List<object> SelectTaxes<Where>(PXGraph graph, object row, PXTaxCheck taxchk, params object[] parameters)
		{
			return SelectTaxes<Where, Where<True, Equal<True>>, Current<SOMiscLine2.lineNbr>>(graph, new object[] { row, graph.Caches[_ParentType].Current }, taxchk, parameters);
		}

		public override void CacheAttached(PXCache sender)
		{
			base.CacheAttached(sender);
			_ChildType = sender.GetItemType();
			TaxCalc = TaxCalc.Calc;
			//sender.Graph.FieldUpdated.AddHandler(sender.GetItemType(), _CuryTranAmt, CuryUnbilledAmt_FieldUpdated);
		}

		//public virtual void CuryUnbilledAmt_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		//{
		//	sender.Graph.Caches[_ParentType].Current = PXParentAttribute.SelectParent(sender, e.Row);
		//	CalcTaxes(sender, e.Row, PXTaxCheck.Line);
		//}

		public override void RowInserted(PXCache sender, PXRowInsertedEventArgs e)
		{
			sender.Graph.Caches[_ParentType].Current = PXParentAttribute.SelectParent(sender, e.Row);
			base.RowInserted(sender, e);
		}

		public override void RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
		{
			sender.Graph.Caches[_ParentType].Current = PXParentAttribute.SelectParent(sender, e.Row);
			base.RowUpdated(sender, e);
		}

		public override void RowDeleted(PXCache sender, PXRowDeletedEventArgs e)
		{
			sender.Graph.Caches[_ParentType].Current = PXParentAttribute.SelectParent(sender, e.Row);
			base.RowDeleted(sender, e);
		}

	}

	/// <summary>
	/// Specialized for SO version of the TaxAttribute. <br/>
	/// Provides Tax calculation for SOLine, by default is attached to SOLine (details) and SOOrder (header) <br/>
	/// Normally, should be placed on the TaxCategoryID field. <br/>
	/// Internally, it uses SOOrder graphs, otherwise taxes are not calculated (tax calc Level is set to NoCalc).<br/>
	/// As a result of this attribute work a set of SOTax tran related to each SOLine  and to their parent will created <br/>
	/// May be combined with other attrbibutes with similar type - for example, SOOpenTaxAttribute <br/>
	/// </summary>
	/// <example>
	/// [SOTax(typeof(SOOrder), typeof(SOTax), typeof(SOTaxTran), TaxCalc = TaxCalc.ManualLineCalc)]
	/// </example>    
	public class SOTaxAttribute : TaxAttribute
	{
		protected virtual short SortOrder
		{
			get
			{
				return 0;
			}
		}
		
		protected override bool CalcGrossOnDocumentLevel { get => true; set => base.CalcGrossOnDocumentLevel = value; }

		public SOTaxAttribute(Type ParentType, Type TaxType, Type TaxSumType)
			: this(ParentType, TaxType, TaxSumType, null)
		{
		}

		public SOTaxAttribute(Type ParentType, Type TaxType, Type TaxSumType, Type TaxCalculationMode)
			: base(ParentType, TaxType, TaxSumType, TaxCalculationMode)
		{
			this.CuryDocBal = null;
			this.CuryLineTotal = typeof(SOOrder.curyLineTotal);
			this.CuryTaxTotal = typeof(SOOrder.curyTaxTotal);
			this.DocDate = typeof(SOOrder.orderDate);
			this.CuryTranAmt = typeof(SOLine.curyLineAmt);
            this.GroupDiscountRate = typeof(SOLine.groupDiscountRate);
			TaxCalcMode = typeof(SOOrder.taxCalcMode);

			this._Attributes.Add(new PXUnboundFormulaAttribute(
				typeof(Switch<Case<Where<SOLine.lineType, NotEqual<SOLineType.miscCharge>>, Switch<Case<Where<SOLine.operation, Equal<Current<SOOrder.defaultOperation>>>, SOLine.curyLineAmt>, Data.Minus<SOLine.curyLineAmt>>>, decimal0>), 
				typeof(SumCalc<SOOrder.curyLineTotal>)));
			this._Attributes.Add(new PXUnboundFormulaAttribute(
				typeof(Switch<Case<Where<SOLine.lineType, Equal<SOLineType.miscCharge>>, Switch<Case<Where<SOLine.operation, Equal<Current<SOOrder.defaultOperation>>>, SOLine.curyLineAmt>, Data.Minus<SOLine.curyLineAmt>>>, decimal0>),
				typeof(SumCalc<SOOrder.curyMiscTot>)));
			this._Attributes.Add(new PXUnboundFormulaAttribute(
				typeof(Switch<Case<Where<SOLine.commissionable, Equal<True>>, Mult<Mult<SOLine.curyLineAmt, SOLine.groupDiscountRate>, SOLine.documentDiscountRate>>, decimal0>),
				typeof(SumCalc<SOSalesPerTran.curyCommnblAmt>)));
		}

		public override int CompareTo(object other)
		{
 			 return this.SortOrder.CompareTo(((SOTaxAttribute)other).SortOrder);
		}

		protected override decimal CalcLineTotal(PXCache sender, object row)
		{
			return (decimal?)ParentGetValue(sender.Graph, _CuryLineTotal) ?? 0m;
		}
		protected override string GetTaxZone(PXCache sender, object row)
		{
			return row == null ? base.GetTaxZone(sender, row)
				: (string)sender.GetValue<SOLine.taxZoneID>(row);
		}

		protected override decimal? GetCuryTranAmt(PXCache sender, object row, string TaxCalcType)
		{
            decimal val = 0m;
            val = (base.GetCuryTranAmt(sender, row) ?? 0m) * 
				  ((decimal?)sender.GetValue(row, _GroupDiscountRate) ?? 1m) * 
				  ((decimal?)sender.GetValue(row, _DocumentDiscountRate) ?? 1m);

			PXCache cache = sender.Graph.Caches[typeof(SOOrder)];
			
			if (row != null && !object.Equals(sender.GetValue<SOLine.operation>(row), cache.GetValue<SOOrder.defaultOperation>(cache.Current)))
			{
                return -PXDBCurrencyAttribute.Round(sender, row, val, CMPrecision.TRANCURY);
			}

            return PXDBCurrencyAttribute.Round(sender, row, val, CMPrecision.TRANCURY);
		}

		/// <summary>
		/// Check if per-unit tax amount sign should be inverted. For <see cref="SOTaxAttribute"/> checks the same conditions for sign invertion
		/// as the <see cref="SOTaxAttribute.GetCuryTranAmt(PXCache, object, string)"/> method override.
		/// </summary>
		/// <param name="rowCache">The row cache.</param>
		/// <param name="row">The row.</param>
		/// <param name="tax">The tax.</param>
		/// <param name="taxRevison">The tax revison.</param>
		/// <param name="taxDetailCache">The tax detail cache.</param>
		/// <param name="taxDetail">The tax detail.</param>
		/// <returns/>
		protected override bool InvertPerUnitTaxAmountSign(PXCache rowCache, object row, Tax tax, TaxRev taxRevison,
														   PXCache taxDetailCache, TaxDetail taxDetail)
		{
			if (row == null)
				return base.InvertPerUnitTaxAmountSign(rowCache, row, tax, taxRevison, taxDetailCache, taxDetail);

			PXCache ordersCache = rowCache.Graph.Caches[typeof(SOOrder)];
			string lineOperation = rowCache.GetValue<SOLine.operation>(row) as string;
			string defaultOperation = ordersCache.GetValue<SOOrder.defaultOperation>(ordersCache.Current) as string;

			return defaultOperation != lineOperation
				? true
				: base.InvertPerUnitTaxAmountSign(rowCache, row, tax, taxRevison, taxDetailCache, taxDetail);
		}

		protected override void CalcDocTotals(
			PXCache sender, 
			object row, 
			decimal CuryTaxTotal, 
			decimal CuryInclTaxTotal, 
			decimal CuryWhTaxTotal,
			decimal CuryTaxDiscountTotal)
		{
			base.CalcDocTotals(sender, row, CuryTaxTotal, CuryInclTaxTotal, CuryWhTaxTotal, CuryTaxDiscountTotal);

			decimal CuryLineTotal = (decimal)(ParentGetValue<SOOrder.curyLineTotal>(sender.Graph) ?? 0m);
			decimal CuryMiscTotal = (decimal)(ParentGetValue<SOOrder.curyMiscTot>(sender.Graph) ?? 0m);
			decimal CuryFreightTotal = (decimal)(ParentGetValue<SOOrder.curyFreightTot>(sender.Graph) ?? 0m);
			decimal CuryDiscountTotal = (decimal)(ParentGetValue<SOOrder.curyDiscTot>(sender.Graph) ?? 0m);
			
			//if (this.GetType() == typeof(SOTaxAttribute) && CuryLineTotal < 0m)
			//{
			//	CuryLineTotal = -CuryLineTotal;
			//	CuryTaxTotal = -CuryTaxTotal;
			//	CuryDiscountTotal = -CuryDiscountTotal;
			//}

			decimal CuryDocTotal = CuryLineTotal + CuryMiscTotal + CuryFreightTotal + CuryTaxTotal - CuryInclTaxTotal - CuryDiscountTotal;

			if (object.Equals(CuryDocTotal, (decimal)(ParentGetValue<SOOrder.curyOrderTotal>(sender.Graph) ?? 0m)) == false)
			{
				ParentSetValue<SOOrder.curyOrderTotal>(sender.Graph, CuryDocTotal);
			}
		}

		protected virtual bool IsFreightTaxable(PXCache sender, List<object> taxitems)
		{
			for (int i = 0; i < taxitems.Count; i++)
			{
				if (((SOTax)(PXResult<SOTax>)taxitems[i]).LineNbr == 32000)
				{
					return true;
				}
			}
			return false;
		}

		//PXParent attributes pointing on SOLine in SOTax DAC don't work because SOTax cache is initialized before SOLine cache
 		protected override object SelectParent(PXCache sender, object row)
		{
			if (row.GetType() == typeof(SOTax) && ((SOTax)row).LineNbr == 32000) //freight lines
			{
				if (_ChildType != typeof(SOOrder))
			{
					return null; 
				}
				return (SOOrder)PXSelect<SOOrder, Where<SOOrder.orderType, Equal<Current<SOTax.orderType>>, And<SOOrder.orderNbr, Equal<Current<SOTax.orderNbr>>>>>.Select(sender.Graph, row);
			}
			else
			{
				return base.SelectParent(sender, row);
			}
		}

        protected override void AdjustTaxableAmount(PXCache sender, object row, List<object> taxitems, ref decimal CuryTaxableAmt, string TaxCalcType)
		{
			decimal CuryLineTotal = (decimal?)ParentGetValue<SOOrder.curyLineTotal>(sender.Graph) ?? 0m;
			decimal CuryMiscTotal = (decimal)(ParentGetValue<SOOrder.curyMiscTot>(sender.Graph) ?? 0m);
			decimal CuryFreightTotal = (decimal)(ParentGetValue<SOOrder.curyFreightTot>(sender.Graph) ?? 0m);
            decimal CuryDiscountTotal = (decimal)(ParentGetValue<SOOrder.curyDiscTot>(sender.Graph) ?? 0m);

			CuryLineTotal += CuryMiscTotal;

            //24565 do not protate discount if lineamt+freightamt = taxableamt
            //27214 do not prorate discount on freight separated from document lines, i.e. taxable by different tax than document lines
            decimal CuryTaxableFreight = IsFreightTaxable(sender, taxitems) ? CuryFreightTotal : 0m;

			if (CuryLineTotal != 0m && CuryTaxableAmt != 0m)
			{
                if (Math.Abs(CuryTaxableAmt - CuryTaxableFreight - CuryLineTotal) < 0.00005m)
                {
                    CuryTaxableAmt -= CuryDiscountTotal;
                }
			}
		}

		protected override List<object> SelectTaxesToCalculateTaxSum(PXCache sender, object row, TaxDetail taxdet)
		{
			SOTaxTran taxtran = (SOTaxTran)taxdet;

			return SelectTaxes<
				Where<Tax.taxID, Equal<Required<Tax.taxID>>>,
				Where<SOTax.taxID, Equal<Required<SOTaxTran.taxID>>, And<SOTax.taxZoneID, Equal<Required<SOTaxTran.taxZoneID>>>>,
				Current<SOLine.lineNbr>>(sender.Graph, new object[] { row, sender.Graph.Caches[_ParentType].Current }, PXTaxCheck.RecalcLine, taxtran.TaxID, taxtran.TaxZoneID);
		}

		protected override List<object> SelectTaxes<Where>(PXGraph graph, object row, PXTaxCheck taxchk, params object[] parameters)
		{
			return SelectTaxes<Where, Where<True, Equal<True>>, Current<SOLine.lineNbr>>(graph, new object[] { row, graph.Caches[_ParentType].Current }, taxchk, parameters);
		}

		public override object Insert(PXCache sender, object item)
		{
			return InsertCached(sender, item);
		}

		public override object Update(PXCache sender, object item)
		{
			return UpdateCached(sender, item);
		}

		public override object Delete(PXCache sender, object item)
		{
			return DeleteCached(sender, item);
		}

		protected virtual object InsertCached(PXCache sender, object item)
		{
			List<object> recordsList = getRecordsList(sender);

			PXCache pcache = sender.Graph.Caches[typeof(SOOrder)];
			string OrderType = (string)pcache.GetValue<SOOrder.orderType>(pcache.Current);
			string OrderNbr = (string)pcache.GetValue<SOOrder.orderNbr>(pcache.Current);

			List<PXRowInserted> insertedHandlersList = storeCachedInsertList(sender, recordsList, OrderType, OrderNbr);
			List<PXRowDeleted> deletedHandlersList = storeCachedDeleteList(sender, recordsList, OrderType, OrderNbr);

			try
			{
				return sender.Insert(item);
			}
			finally
			{
				foreach (PXRowInserted handler in insertedHandlersList)
					sender.Graph.RowInserted.RemoveHandler<SOTax>(handler);
				foreach (PXRowDeleted handler in deletedHandlersList)
					sender.Graph.RowDeleted.RemoveHandler<SOTax>(handler);
			}
		}

		protected virtual object UpdateCached(PXCache sender, object item)
		{
			List<object> recordsList = getRecordsList(sender);

			PXCache pcache = sender.Graph.Caches[typeof(SOOrder)];
			string OrderType = (string)pcache.GetValue<SOOrder.orderType>(pcache.Current);
			string OrderNbr = (string)pcache.GetValue<SOOrder.orderNbr>(pcache.Current);

			List<PXRowUpdated> updatedHandlersList = storeCachedUpdateList(sender, recordsList, OrderType, OrderNbr);

			try
			{
				return sender.Update(item);
			}
			finally
			{
				foreach (PXRowUpdated handler in updatedHandlersList)
					sender.Graph.RowUpdated.RemoveHandler<SOTax>(handler);
			}
		}

		protected virtual object DeleteCached(PXCache sender, object item)
		{
			List<object> recordsList = getRecordsList(sender);

			PXCache pcache = sender.Graph.Caches[typeof(SOOrder)];
			string OrderType = (string)pcache.GetValue<SOOrder.orderType>(pcache.Current);
			string OrderNbr = (string)pcache.GetValue<SOOrder.orderNbr>(pcache.Current);

			List<PXRowInserted> insertedHandlersList = storeCachedInsertList(sender, recordsList, OrderType, OrderNbr);
			List<PXRowDeleted> deletedHandlersList = storeCachedDeleteList(sender, recordsList, OrderType, OrderNbr);

			try
			{
				return sender.Delete(item);
			}
			finally
			{
				foreach (PXRowInserted handler in insertedHandlersList)
					sender.Graph.RowInserted.RemoveHandler<SOTax>(handler);
				foreach (PXRowDeleted handler in deletedHandlersList)
					sender.Graph.RowDeleted.RemoveHandler<SOTax>(handler);
			}
		}

		protected List<Object> getRecordsList(PXCache sender)
		{
			var recordsList = new List<object>(PXSelect<SOTax,
				Where<SOTax.orderType, Equal<Current<SOOrder.orderType>>,
					And<SOTax.orderNbr, Equal<Current<SOOrder.orderNbr>>>>>
				.SelectMultiBound(sender.Graph, new object[] { sender.Graph.Caches[typeof(SOOrder).Name].Current })
				.RowCast<SOTax>());

			return recordsList;
		}

		protected List<PXRowInserted> storeCachedInsertList(PXCache sender, List<Object> recordsList, string OrderType, string OrderNbr)
		{
			List<PXRowInserted> handlersList = new List<PXRowInserted>();

			PXRowInserted inserted = delegate(PXCache cache, PXRowInsertedEventArgs e)
			{
				recordsList.Add(e.Row);

				PXSelect<SOTax,
					Where<SOTax.orderType, Equal<Current<SOOrder.orderType>>,
						And<SOTax.orderNbr, Equal<Current<SOOrder.orderNbr>>>>>
					.StoreCached(sender.Graph, new PXCommandKey(new object[] { OrderType, OrderNbr }), recordsList);
					//.StoreResult(sender.Graph, recordsList, PXQueryParameters.ExplicitParameters(OrderType, OrderNbr));
			};
			sender.Graph.RowInserted.AddHandler<SOTax>(inserted);
			handlersList.Add(inserted);

			return handlersList;
		}

		protected List<PXRowUpdated> storeCachedUpdateList(PXCache sender, List<Object> recordsList, string OrderType, string OrderNbr)
		{
			var handlersList = new List<PXRowUpdated>();

			PXRowUpdated updated = delegate (PXCache cache, PXRowUpdatedEventArgs e)
			{
				var comparer = cache.GetComparer();
				int ndx = recordsList.FindIndex(t => comparer.Equals(t, e.OldRow));
				if (ndx >= 0)
				{
					recordsList[ndx] = e.Row;
				}
				PXSelect<SOTax,
					Where<SOTax.orderType, Equal<Current<SOOrder.orderType>>,
						And<SOTax.orderNbr, Equal<Current<SOOrder.orderNbr>>>>>
					.StoreCached(sender.Graph, new PXCommandKey(new object[] { OrderType, OrderNbr }), recordsList);
					//.StoreResult(sender.Graph, recordsList, PXQueryParameters.ExplicitParameters(OrderType, OrderNbr));
			};
			sender.Graph.RowUpdated.AddHandler<SOTax>(updated);
			handlersList.Add(updated);

			return handlersList;
		}

		protected List<PXRowDeleted> storeCachedDeleteList(PXCache sender, List<Object> recordsList, string OrderType, string OrderNbr)
		{
			List<PXRowDeleted> handlersList = new List<PXRowDeleted>();

			PXRowDeleted deleted = delegate(PXCache cache, PXRowDeletedEventArgs e)
			{
				recordsList.Remove(e.Row);

				PXSelect<SOTax,
					Where<SOTax.orderType, Equal<Current<SOOrder.orderType>>,
						And<SOTax.orderNbr, Equal<Current<SOOrder.orderNbr>>>>>
					.StoreCached(sender.Graph, new PXCommandKey(new object[] { OrderType, OrderNbr }), recordsList);
					//.StoreResult(sender.Graph, recordsList, PXQueryParameters.ExplicitParameters(OrderType, OrderNbr));
			};

			sender.Graph.RowDeleted.AddHandler<SOTax>(deleted);
			handlersList.Add(deleted);

			return handlersList;
		}

		protected List<object> SelectTaxes<Where, Where2, LineNbr>(PXGraph graph, object[] currents, PXTaxCheck taxchk, params object[] parameters)
			where Where : IBqlWhere, new()
			where Where2 : IBqlWhere, new()
			where LineNbr : IBqlOperand
		{
			List<ITaxDetail> taxDetails;
			IDictionary<string, PXResult<Tax, TaxRev>> tails;

			List<object> ret = new List<object>();
			Type fieldLineNbr;

			BqlCommand selectTaxes = new Select2<Tax,
				LeftJoin< TaxRev, On<TaxRev.taxID, Equal<Tax.taxID>,
					 And<TaxRev.outdated, Equal<False>,
					 And<TaxRev.taxType, Equal<TaxType.sales>,
					 And<Tax.taxType, NotEqual<CSTaxType.withholding>,
					 And<Tax.taxType, NotEqual<CSTaxType.use>,
					 And<Tax.reverseTax, Equal<False>,
					 And<Current<SOOrder.orderDate>, Between<TaxRev.startDate, TaxRev.endDate>>>>>>>>>>();

			switch (taxchk)
			{
				case PXTaxCheck.Line:
					taxDetails = PXSelect<SOTax,
						Where<SOTax.orderType, Equal<Current<SOOrder.orderType>>,
							And<SOTax.orderNbr, Equal<Current<SOOrder.orderNbr>>>>>
						.SelectMultiBound(graph, currents)
						.RowCast<SOTax>()
						.ToList<ITaxDetail>();

					if (taxDetails == null || taxDetails.Count == 0) return ret;

					tails = CollectInvolvedTaxes<Where>(graph, taxDetails, selectTaxes, currents, parameters);

					int? linenbr = int.MinValue;

					if (currents.Length > 0
						&& currents[0] != null
						&& typeof(LineNbr).IsGenericType
						&& typeof(LineNbr).GetGenericTypeDefinition() == typeof(Current<>)
						&& (fieldLineNbr = typeof(LineNbr).GetGenericArguments()[0]).IsNested
						&& currents[0].GetType() == BqlCommand.GetItemType(fieldLineNbr)
						)
					{
						linenbr = (int?)graph.Caches[BqlCommand.GetItemType(fieldLineNbr)].GetValue(currents[0], fieldLineNbr.Name);
					}

					if (typeof(IConstant<int>).IsAssignableFrom(typeof(LineNbr)))
					{
						linenbr = ((IConstant<int>)Activator.CreateInstance(typeof(LineNbr))).Value;
					}

					foreach (SOTax record in taxDetails)
					{
						if (record.LineNbr == linenbr)
						{
							InsertTax(graph, taxchk, record, tails, ret);
						}

						//resultset is always sorted by LineNbr
						//if (record.LineNbr > linenbr) break;
					}
					return ret;

				case PXTaxCheck.RecalcLine:
					taxDetails = PXSelect<SOTax,
						Where<SOTax.orderType, Equal<Current<SOOrder.orderType>>,
							And<SOTax.orderNbr, Equal<Current<SOOrder.orderNbr>>>>>
						.SelectMultiBound(graph, currents)
						.RowCast<SOTax>()
						.ToList<ITaxDetail>();

					if (taxDetails == null || taxDetails.Count == 0) return ret;

					tails = CollectInvolvedTaxes<Where>(graph, taxDetails, selectTaxes, currents, parameters);

					foreach (SOTax record in taxDetails)
					{
						if (!Meet<Where2>(graph, record, parameters.ToList())) continue;

						//resultset is always sorted by LineNbr
						if (record.LineNbr == int.MaxValue) break;

						InsertTax(graph, taxchk, record, tails, ret);
					}
					return ret;

				case PXTaxCheck.RecalcTotals:
					taxDetails = PXSelect<SOTaxTran,
						Where<SOTaxTran.orderType, Equal<Current<SOOrder.orderType>>,
							And<SOTaxTran.orderNbr, Equal<Current<SOOrder.orderNbr>>>>>
						.SelectMultiBound(graph, currents)
						.RowCast<SOTaxTran>()
						.ToList<ITaxDetail>();

					if (taxDetails == null || taxDetails.Count == 0) return ret;

					tails = CollectInvolvedTaxes<Where>(graph, taxDetails, selectTaxes, currents, parameters);

					foreach (SOTaxTran record in taxDetails)
					{
						InsertTax(graph, taxchk, record, tails, ret);
					}
					return ret;

				default:
					return ret;
			}
		}

		protected bool Meet<Where2>(PXGraph graph, SOTax record, List<object> parameters)
			where Where2 : IBqlWhere, new()
		{
			IBqlWhere where = (IBqlWhere)Activator.CreateInstance(typeof(Where2));
			object value = null;
			bool? ret = null;

			where.Verify(graph.Caches[_TaxType], record, parameters, ref ret, ref value);

			return ret == true;
		}

		protected override List<object> SelectDocumentLines(PXGraph graph, object row)
		{
			List<object> ret = PXSelect<SOLine,
								Where<SOLine.orderType, Equal<Current<SOOrder.orderType>>,
									And<SOLine.orderNbr, Equal<Current<SOOrder.orderNbr>>>>>
									.SelectMultiBound(graph, new object[] { row })
									.RowCast<SOLine>()
									.Select(_ => (object)_)
									.ToList();
			return ret;
		}

		protected override decimal? GetDocLineFinalAmtNoRounding(PXCache sender, object row, string TaxCalcType = "I")
		{
			var extPrice = (decimal?)sender.GetValue(row, typeof(SOLine.curyExtPrice).Name);
			var discAmount = (decimal?)sender.GetValue(row, typeof(SOLine.curyDiscAmt).Name);
			var docDiscount = (decimal?)sender.GetValue(row, typeof(SOLine.documentDiscountRate).Name);
			var groupDiscount = (decimal?)sender.GetValue(row, typeof(SOLine.groupDiscountRate).Name);
			var value = (extPrice - discAmount ) * docDiscount * groupDiscount;
			return value;
		}



		protected override void DefaultTaxes(PXCache sender, object row, bool DefaultExisting)
		{
			if (SortOrder == 0)
				base.DefaultTaxes(sender, row, DefaultExisting);
		}

		protected override void ClearTaxes(PXCache sender, object row)
		{
			if (SortOrder == 0)
				base.ClearTaxes(sender, row);
		}

		public override void CacheAttached(PXCache sender)
		{
			_ChildType = sender.GetItemType();

            inserted = new Dictionary<object, object>();
            updated = new Dictionary<object, object>();

			if (sender.Graph is SOOrderEntry)
			{
                base.CacheAttached(sender);

				sender.Graph.FieldUpdated.AddHandler(typeof(SOOrder), typeof(SOOrder.curyDiscTot).Name, SOOrder_CuryDiscTot_FieldUpdated);
				sender.Graph.FieldUpdated.AddHandler(typeof(SOOrder), typeof(SOOrder.curyFreightTot).Name, SOOrder_CuryFreightTot_FieldUpdated);
				sender.Graph.FieldUpdated.AddHandler(typeof(SOOrder), _CuryTaxTotal, SOOrder_CuryTaxTot_FieldUpdated);
			}
			else
			{
				this.TaxCalc = TaxCalc.NoCalc;
			}
		}

		protected override void OnZoneUpdated(ZoneUpdatedArgs e)
		{
			foreach (object detail in e.Details)
			{
				if ((string)e.Cache.GetValue<SOLine.behavior>(detail) != SOBehavior.BL) //TaxZoneID updates in blanket orders are handled in PX.Objects.SO.GraphExtensions.SOOrderEntryExt.Blanket extension
				{
					e.Cache.SetValue<SOLine.taxZoneID>(detail, e.NewValue);
					e.Cache.MarkUpdated(detail);
				}
			}

			base.OnZoneUpdated(e);
		}

		//CalcTaxes will be called on every tax zone change, even if taxes in both tax zones are the same
		protected override bool CompareZone(PXGraph graph, string zoneA, string zoneB)
		{
			return string.Equals(zoneA, zoneB, StringComparison.OrdinalIgnoreCase);
		}

		protected override void AddOneTax(PXCache sender, object detrow, ITaxDetail taxitem)
		{
			PXCache childcache = sender.Graph.Caches[_ChildType];

			PXRowInserting inserting = (cache, e) => { ((SOTax)e.Row).TaxZoneID = (string)childcache.GetValue<SOLine.taxZoneID>(detrow); };

			sender.Graph.RowInserting.AddHandler(_TaxType, inserting);
			try
			{
				base.AddOneTax(sender, detrow, taxitem);
			}
			finally
			{
				sender.Graph.RowInserting.RemoveHandler(_TaxType, inserting);
			}
		}

		protected override void AddTaxTotals(PXCache sender, string taxID, object row)
		{
			PXCache childcache = sender.Graph.Caches[_ChildType];

			PXRowInserting inserting = (cache, e) => { ((SOTaxTran)e.Row).TaxZoneID = (string)childcache.GetValue<SOLine.taxZoneID>(row); };

			sender.Graph.RowInserting.AddHandler(_TaxSumType, inserting);
			try
			{
				base.AddTaxTotals(sender, taxID, row);
			}
			finally
			{
				sender.Graph.RowInserting.RemoveHandler(_TaxSumType, inserting);
			}

		}

		protected override IEnumerable<ITaxDetail> ManualTaxes(PXCache sender, object row)
		{
			List<ITaxDetail> ret = new List<ITaxDetail>();

			foreach (PXResult res in SelectTaxes(sender, row, PXTaxCheck.RecalcTotals))
			{
				if (row is SOLine detrow && res[0] is SOTaxTran taxdetail && detrow.TaxZoneID != null && detrow.TaxZoneID == taxdetail.TaxZoneID)
				{
					ret.Add((ITaxDetail)res[0]);
				}
			}
			return ret;
		}

		protected virtual void SOOrder_CuryDiscTot_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			bool calc = true;
			if (IsExternalTax(sender.Graph, (string)sender.GetValue(e.Row, _TaxZoneID)) || (e.Row != null && ((SOOrder)e.Row).ExternalTaxesImportInProgress == true))
				calc = false;
			
			PXCache cache = sender.Graph.Caches[typeof(SOLine)];
			
			this._ParentRow = e.Row;
			CalcTotals(sender, e.Row, calc);
			this._ParentRow = null;
		}

		protected virtual void SOOrder_CuryFreightTot_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			bool calc = true;
			if (IsExternalTax(sender.Graph, (string)sender.GetValue(e.Row, _TaxZoneID)) || (e.Row != null && ((SOOrder)e.Row).ExternalTaxesImportInProgress == true))
				calc = false;

			this._ParentRow = e.Row;
			CalcTotals(sender, e.Row, calc);
			this._ParentRow = null;
		}

        protected virtual void SOOrder_CuryTaxTot_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
        {
            if (IsExternalTax(sender.Graph, (string)sender.GetValue(e.Row, _TaxZoneID)) || (e.Row != null && ((SOOrder)e.Row).ExternalTaxesImportInProgress == true))
            {
                decimal? curyTaxTotal = (decimal?)sender.GetValue(e.Row, _CuryTaxTotal);
                decimal? curyWhTaxTotal = (decimal?)sender.GetValue(e.Row, _CuryWhTaxTotal);
                CalcDocTotals(sender, e.Row, curyTaxTotal.GetValueOrDefault(), 0, curyWhTaxTotal.GetValueOrDefault(), 0m);
            }
        }
    }

	public class SOInvoiceTaxAttribute : ARTaxAttribute
	{
        public SOInvoiceTaxAttribute()
            : base(typeof(PX.Objects.AR.ARInvoice), typeof(ARTax), typeof(ARTaxTran))
        {
            _Attributes.Clear();
            _Attributes.Add(new PXUnboundFormulaAttribute(typeof(Switch<Case<Where<ARTran.lineType, NotEqual<SOLineType.miscCharge>, And<ARTran.lineType, NotEqual<SOLineType.freight>, And<ARTran.lineType, NotEqual<SOLineType.discount>>>>, ARTran.curyTranAmt>, decimal0>), typeof(SumCalc<ARInvoice.curyGoodsTotal>)));
            _Attributes.Add(new PXUnboundFormulaAttribute(typeof(Switch<Case<Where<ARTran.lineType, Equal<SOLineType.miscCharge>>, ARTran.curyTranAmt>, decimal0>), typeof(SumCalc<ARInvoice.curyMiscTot>)));
            _Attributes.Add(new PXUnboundFormulaAttribute(typeof(Switch<Case<Where<ARTran.lineType, Equal<SOLineType.freight>>, ARTran.curyTranAmt>, decimal0>), typeof(SumCalc<ARInvoice.curyFreightTot>)));
			_Attributes.Add(new PXUnboundFormulaAttribute(typeof(Switch<
				Case<Where<ARTran.commissionable, Equal<True>, And<Where<ARTran.lineType, NotEqual<SOLineType.discount>,
					And<Where<ARTran.tranType, Equal<ARDocType.creditMemo>, Or<ARTran.tranType, Equal<ARDocType.cashReturn>>>>>>>,
						Data.Minus<Sub<Sub<ARTran.curyTranAmt, Sub<ARTran.curyTranAmt, Mult<Mult<ARTran.curyTranAmt, ARTran.origGroupDiscountRate>, ARTran.origDocumentDiscountRate>>>,
						Sub<ARTran.curyTranAmt, Mult<Mult<ARTran.curyTranAmt, ARTran.groupDiscountRate>, ARTran.documentDiscountRate>>>>,
				Case<Where<ARTran.commissionable, Equal<True>, And<Where<ARTran.lineType, NotEqual<SOLineType.discount>>>>,
						Sub<Sub<ARTran.curyTranAmt, Sub<ARTran.curyTranAmt, Mult<Mult<ARTran.curyTranAmt, ARTran.origGroupDiscountRate>, ARTran.origDocumentDiscountRate>>>,
						Sub<ARTran.curyTranAmt, Mult<Mult<ARTran.curyTranAmt, ARTran.groupDiscountRate>, ARTran.documentDiscountRate>>>>>,
				decimal0>),
				typeof(SumCalc<ARSalesPerTran.curyCommnblAmt>)));
        }

		protected override void Tax_RowDeleted(PXCache sender, PXRowDeletedEventArgs e)
		{
			if (_TaxCalc != TaxCalc.NoCalc && e.ExternalCall)
			{
				base.Tax_RowDeleted(sender, e);
			}

			if (_TaxCalc == TaxCalc.ManualCalc)
			{
				PXCache cache = sender.Graph.Caches[_ChildType];
				PXCache taxcache = sender.Graph.Caches[_TaxType];

				//Preload detail taxes into PXCache
				SelectTaxes(sender, null, PXTaxCheck.RecalcLine);
			}
		}
        protected override bool ConsiderEarlyPaymentDiscount(PXCache sender, object parent, Tax tax)
        {
			var doc = (ARInvoice)parent;
			if (doc.DocType != ARDocType.CashSale && doc.DocType != ARDocType.CashReturn)
                return base.ConsiderEarlyPaymentDiscount(sender, parent, tax);
            else
                return
                    (tax.TaxCalcLevel == CSTaxCalcLevel.CalcOnItemAmt
                    || tax.TaxCalcLevel == CSTaxCalcLevel.CalcOnItemAmtPlusTaxAmt)
                                &&
                    tax.TaxApplyTermsDisc == CSTaxTermsDiscount.ToPromtPayment;
        }
        protected override bool ConsiderInclusiveDiscount(PXCache sender, object parent, Tax tax)
        {
			var doc = (ARInvoice)parent;
			if (doc.DocType != ARDocType.CashSale && doc.DocType != ARDocType.CashReturn)
				return base.ConsiderInclusiveDiscount(sender, parent, tax);
            else
                return (tax.TaxCalcLevel == CSTaxCalcLevel.Inclusive && tax.TaxApplyTermsDisc == CSTaxTermsDiscount.ToPromtPayment);
        }

		protected override object SelectParent(PXCache cache, object row)
		{
			if (_TaxCalc == TaxCalc.ManualCalc)
			{
				//locate parent in cache
				object detrow = TaxParentAttribute.LocateParent(cache, row, _ChildType);

				if (FilterParent(cache, detrow))
				{
					return null;
				}
				return detrow;
			}
			else
			{
				return base.SelectParent(cache, row);
			}
			}

		protected virtual bool FilterParent(PXCache cache, object detrow)
		{
			if (detrow == null || cache.Graph.Caches[_ChildType].GetStatus(detrow) == PXEntryStatus.Notchanged)
				return true;

			if (_ChildType == typeof(ARTran))
			{
				ARTran tran = (ARTran)detrow;
				PXCache orderShipmentCache = cache.Graph.Caches[typeof(SOOrderShipment)];
				SOOrderShipment orderShipment = (SOOrderShipment)orderShipmentCache?.Current;
				return orderShipment != null && (orderShipment.OrderType != tran.SOOrderType || orderShipment.OrderNbr != tran.SOOrderNbr
					|| orderShipment.ShipmentType != tran.SOShipmentType || orderShipment.ShipmentNbr != tran.SOShipmentNbr);
			}
			return false;
		}

		protected override decimal CalcLineTotal(PXCache sender, object row)
		{
			return (decimal?)ParentGetValue(sender.Graph, _CuryLineTotal) ?? 0m;
		}

		protected override void AdjustTaxableAmount(PXCache sender, object row, List<object> taxitems, ref decimal CuryTaxableAmt, string TaxCalcType)
		{
			PXCache cache = sender.Graph.Caches[typeof(SOInvoice)];
			SOInvoice current = (SOInvoice)PXParentAttribute.SelectParent(sender, row, typeof(SOInvoice)); 

			decimal CuryLineTotal = (decimal?)cache.GetValue<ARInvoice.curyGoodsTotal>(current) ?? 0m;
			decimal CuryMiscTotal = (decimal?)cache.GetValue<ARInvoice.curyMiscTot>(current) ?? 0m;
			decimal CuryFreightTotal = (decimal?)cache.GetValue<ARInvoice.curyFreightTot>(current) ?? 0m;
            decimal CuryDiscountTotal = (decimal?)cache.GetValue<ARInvoice.curyDiscTot>(current) ?? 0m;

			if (CuryLineTotal + CuryMiscTotal + CuryFreightTotal != 0m && CuryTaxableAmt != 0m)
			{
                if (Math.Abs(CuryTaxableAmt - CuryLineTotal - CuryMiscTotal) < 0.00005m)
                {
                    CuryTaxableAmt -= CuryDiscountTotal;
                }
			}
		}

		protected override void CalcDocTotals(
			PXCache sender, 
			object row, 
			decimal CuryTaxTotal, 
			decimal CuryInclTaxTotal, 
			decimal CuryWhTaxTotal,
			decimal CuryTaxDiscountTotal)
		{
			PXCache cache = sender.Graph.Caches[typeof(ARInvoice)];

			ARInvoice doc = null;
			if ( row is ARInvoice )
			{
				doc = (ARInvoice) row;
			}
			else
			{
				doc = (ARInvoice)PXParentAttribute.SelectParent(sender, row, typeof(ARInvoice));	
			}
			

			decimal CuryLineTotal = ((decimal?)cache.GetValue<ARInvoice.curyGoodsTotal>(doc) ?? 0m)
									+((decimal?)cache.GetValue<ARInvoice.curyMiscTot>(doc) ?? 0m)
									+ ((decimal?)cache.GetValue<ARInvoice.curyFreightTot>(doc) ?? 0m);

            decimal CuryDiscTotal = ((decimal?)cache.GetValue<ARInvoice.curyDiscTot>(doc) ?? 0m);

            decimal CuryDocTotal = CuryLineTotal + CuryTaxTotal + CuryTaxDiscountTotal - CuryInclTaxTotal - CuryDiscTotal;

			decimal doc_CuryLineTotal = (decimal)(ParentGetValue(sender.Graph, _CuryLineTotal) ?? 0m);
			decimal doc_CuryTaxTotal = (decimal)(ParentGetValue(sender.Graph, _CuryTaxTotal) ?? 0m);
            decimal doc_CuryDiscTotal = (decimal)(ParentGetValue(sender.Graph, _CuryDiscTot) ?? 0m);

			if (object.Equals(CuryLineTotal, doc_CuryLineTotal) == false ||
				object.Equals(CuryTaxTotal, doc_CuryTaxTotal) == false ||
                object.Equals(CuryDiscTotal, doc_CuryDiscTotal) == false)
			{
				ParentSetValue(sender.Graph, _CuryLineTotal, CuryLineTotal);
				ParentSetValue(sender.Graph, _CuryTaxTotal, CuryTaxTotal);
                ParentSetValue(sender.Graph, _CuryDiscTot, CuryDiscTotal);
				if (!string.IsNullOrEmpty(_CuryDocBal))
				{
					ParentSetValue(sender.Graph, _CuryDocBal, CuryDocTotal);
					return;
				}
			}

            if (!string.IsNullOrEmpty(_CuryTaxDiscountTotal))
            {
                ParentSetValue(sender.Graph, _CuryTaxDiscountTotal, CuryTaxDiscountTotal);
            }

			if (!string.IsNullOrEmpty(_CuryDocBal))
			{
				decimal doc_CuryDocBal = (decimal)(ParentGetValue(sender.Graph, _CuryDocBal) ?? 0m);

				if (object.Equals(CuryDocTotal, doc_CuryDocBal) == false)
				{
					ParentSetValue(sender.Graph, _CuryDocBal, CuryDocTotal);
				}
			}
		}

		protected override void IsTaxSavedFieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			ARInvoice invoice = e.Row as ARInvoice;

			if (invoice != null)
			{
				decimal? curyTaxTotal = (decimal?)sender.GetValue(e.Row, _CuryTaxTotal);
				decimal? curyWhTaxTotal = (decimal?)sender.GetValue(e.Row, _CuryWhTaxTotal);
				CalcDocTotals(sender, invoice, curyTaxTotal.GetValueOrDefault(), 0, curyWhTaxTotal.GetValueOrDefault(), 0m);
			}
		}

		protected override DateTime? GetDocDate(PXCache sender, object row)
		{
			bool isCancellationInvoice = (bool?)ParentGetValue<ARRegister.isCancellation>(sender.Graph) == true;
			DateTime? origDocDate = (DateTime?)ParentGetValue<ARRegister.origDocDate>(sender.Graph);
			if (isCancellationInvoice && origDocDate != null)
			{
				return origDocDate;
			}
			else
			{
				return base.GetDocDate(sender, row);
			}
		}
	}


	public abstract class SOCustomListAttribute : PXStringListAttribute
			{
		public string[] AllowedValues => _AllowedValues;
		public string[] AllowedLabels => _AllowedLabels;

		protected SOCustomListAttribute(Tuple<string, string>[] valuesToLabels) : base(valuesToLabels) { }

		protected abstract Tuple<string, string>[] GetPairs();

            public override void CacheAttached(PXCache sender)
            {
			var pairs = GetPairs();
			_AllowedValues = pairs.Select(t => t.Item1).ToArray();
			_AllowedLabels = pairs.Select(t => t.Item2).ToArray();
                _NeutralAllowedLabels = _AllowedLabels;

                base.CacheAttached(sender);
            }

		protected static string MaskLocationLabel
			=> !PXAccess.FeatureInstalled<FeaturesSet.accountLocations>()
				? AR.Messages.MaskCustomer
				: AR.Messages.MaskLocation;
		}

	public class SOSalesAcctSubDefault
	{
		public class AcctListAttribute : SOCustomListAttribute
		{
			private static Tuple<string, string>[] Pairs =>
				new[]
			{
					Pair(MaskItem, IN.Messages.MaskItem),
					Pair(MaskSite, IN.Messages.MaskSite),
					Pair(MaskClass, IN.Messages.MaskClass),
					Pair(MaskLocation, MaskLocationLabel),
					Pair(MaskReasonCode, IN.Messages.MaskReasonCode),
				};

			public AcctListAttribute() : base(Pairs) { }
			protected override Tuple<string, string>[] GetPairs() => Pairs;
			}

		public class SubListAttribute : SOCustomListAttribute
            {
			private static Tuple<string, string>[] Pairs =>
				new[]
				{
					Pair(MaskItem, IN.Messages.MaskItem),
					Pair(MaskSite, IN.Messages.MaskSite),
					Pair(MaskClass, IN.Messages.MaskClass),
					Pair(MaskLocation, MaskLocationLabel),
					Pair(MaskEmployee, AR.Messages.MaskEmployee),
					Pair(MaskCompany, AR.Messages.MaskCompany),
					Pair(MaskSalesPerson, AR.Messages.MaskSalesPerson),
					Pair(MaskReasonCode, IN.Messages.MaskReasonCode),
				};

			public SubListAttribute() : base(Pairs) { }
			protected override Tuple<string, string>[] GetPairs() => Pairs;
		}

		public const string MaskItem = "I";
		public const string MaskSite = "W";
		public const string MaskClass = "P";
		public const string MaskReasonCode = "R";

		public const string MaskLocation = "L";
		public const string MaskEmployee = "E";
		public const string MaskCompany = "C";
		public const string MaskSalesPerson = "S";
	}

	[PXDBString(30, IsUnicode = true, InputMask = "")]
	[PXUIField(DisplayName = "Subaccount Mask", Visibility = PXUIVisibility.Visible, FieldClass = _DimensionName)]
    [SOSalesAcctSubDefault.SubList]
	public sealed class SOSalesSubAccountMaskAttribute : AcctSubAttribute
	{
		private const string _DimensionName = "SUBACCOUNT";
		private const string _MaskName = "ZZZZZZZZZA";
		public SOSalesSubAccountMaskAttribute()
			: base()
		{
			PXDimensionMaskAttribute attr = new PXDimensionMaskAttribute(_DimensionName, _MaskName, SOSalesAcctSubDefault.MaskItem, new SOSalesAcctSubDefault.SubListAttribute().AllowedValues, new SOSalesAcctSubDefault.SubListAttribute().AllowedLabels);
			attr.ValidComboRequired = false;
			_Attributes.Add(attr);
			_SelAttrIndex = _Attributes.Count - 1;
		}

        public override void GetSubscriber<ISubscriber>(List<ISubscriber> subscribers)
        {
            base.GetSubscriber<ISubscriber>(subscribers);
            subscribers.Remove(_Attributes.OfType<SOSalesAcctSubDefault.SubListAttribute>().FirstOrDefault() as ISubscriber);
        }

        public override void CacheAttached(PXCache sender)
        {
            base.CacheAttached(sender);
            var stringlist = (SOSalesAcctSubDefault.SubListAttribute)_Attributes.First(x => x.GetType() == typeof(SOSalesAcctSubDefault.SubListAttribute));
            var dimensionmaskattribute = (PXDimensionMaskAttribute)_Attributes.First(x => x.GetType() == typeof(PXDimensionMaskAttribute));
            dimensionmaskattribute.SynchronizeLabels(stringlist.AllowedValues, stringlist.AllowedLabels);
        }

		public static string MakeSub<Field>(PXGraph graph, string mask, object[] sources, Type[] fields)
			where Field : IBqlField
		{
			try
			{
				return PXDimensionMaskAttribute.MakeSub<Field>(graph, mask, new SOSalesAcctSubDefault.SubListAttribute().AllowedValues, 3, sources);
			}
			catch (PXMaskArgumentException ex)
			{
				PXCache cache = graph.Caches[BqlCommand.GetItemType(fields[ex.SourceIdx])];
				string fieldName = fields[ex.SourceIdx].Name;
				throw new PXMaskArgumentException(new SOSalesAcctSubDefault.SubListAttribute().AllowedLabels[ex.SourceIdx], PXUIFieldAttribute.GetDisplayName(cache, fieldName));
			}
		}
	}


	public class SOMiscAcctSubDefault
	{
		public class AcctListAttribute : SOCustomListAttribute
		{
			private static Tuple<string, string>[] Pairs =>
				new[]
			{
					Pair(MaskLocation, MaskLocationLabel),
					Pair(MaskItem, AR.Messages.MaskNonStockItem),
				};

			public AcctListAttribute() : base(Pairs) { }
			protected override Tuple<string, string>[] GetPairs() => Pairs;
		}

		public class SubListAttribute : SOCustomListAttribute
			{
			private static Tuple<string, string>[] Pairs =>
				new[]
            {
					Pair(MaskLocation, MaskLocationLabel),
					Pair(MaskItem, AR.Messages.MaskNonStockItem),
					Pair(MaskEmployee, AR.Messages.MaskEmployee),
					Pair(MaskCompany, AR.Messages.MaskCompany),
				};

			public SubListAttribute() : base(Pairs) { }
			protected override Tuple<string, string>[] GetPairs() => Pairs;
		}

		public const string MaskItem = "I";
		public const string MaskLocation = "L";
		public const string MaskEmployee = "E";
		public const string MaskCompany = "C";
	}

	[PXDBString(30, IsUnicode = true, InputMask = "")]
	[PXUIField(DisplayName = "Subaccount Mask", Visibility = PXUIVisibility.Visible, FieldClass = _DimensionName)]
    [SOMiscAcctSubDefault.SubList]
	public sealed class SOMiscSubAccountMaskAttribute : AcctSubAttribute
	{
		private const string _DimensionName = "SUBACCOUNT";
		private const string _MaskName = "ZZZZZZZZZB";
		public SOMiscSubAccountMaskAttribute()
			: base()
		{
			PXDimensionMaskAttribute attr = new PXDimensionMaskAttribute(_DimensionName, _MaskName, SOMiscAcctSubDefault.MaskItem, new SOMiscAcctSubDefault.SubListAttribute().AllowedValues, new SOMiscAcctSubDefault.SubListAttribute().AllowedLabels);
			attr.ValidComboRequired = false;
			_Attributes.Add(attr);
			_SelAttrIndex = _Attributes.Count - 1;
		}

        public override void GetSubscriber<ISubscriber>(List<ISubscriber> subscribers)
        {
            base.GetSubscriber<ISubscriber>(subscribers);
            subscribers.Remove(_Attributes.OfType<SOMiscAcctSubDefault.SubListAttribute>().FirstOrDefault() as ISubscriber);
        }

        public override void CacheAttached(PXCache sender)
        {
            base.CacheAttached(sender);
            var stringlist = (SOMiscAcctSubDefault.SubListAttribute)_Attributes.First(x => x.GetType() == typeof(SOMiscAcctSubDefault.SubListAttribute));
            var dimensionmaskattribute = (PXDimensionMaskAttribute)_Attributes.First(x => x.GetType() == typeof(PXDimensionMaskAttribute));
            dimensionmaskattribute.SynchronizeLabels(stringlist.AllowedValues, stringlist.AllowedLabels);
        }

		public static string MakeSub<Field>(PXGraph graph, string mask, object[] sources, Type[] fields)
			where Field : IBqlField
		{
			try
			{
				return PXDimensionMaskAttribute.MakeSub<Field>(graph, mask, new SOMiscAcctSubDefault.SubListAttribute().AllowedValues, 0, sources);
			}
			catch (PXMaskArgumentException ex)
			{
				PXCache cache = graph.Caches[BqlCommand.GetItemType(fields[ex.SourceIdx])];
				string fieldName = fields[ex.SourceIdx].Name;
				throw new PXMaskArgumentException(new SOMiscAcctSubDefault.SubListAttribute().AllowedLabels[ex.SourceIdx], PXUIFieldAttribute.GetDisplayName(cache, fieldName));
			}
		}
	}


	public class SOFreightAcctSubDefault
	{
		public class AcctListAttribute : SOCustomListAttribute
		{
			private static Tuple<string, string>[] Pairs =>
				new[]
			{
					Pair(OrderType, Messages.OrderType),
					Pair(MaskLocation, MaskLocationLabel),
					Pair(MaskShipVia, Messages.ShipVia),
				};

			public AcctListAttribute() : base(Pairs) {}
			protected override Tuple<string, string>[] GetPairs() => Pairs;
		}

		public class SubListAttribute : SOCustomListAttribute
		{
			private static Tuple<string, string>[] Pairs =>
				new[]
			{
					Pair(OrderType, Messages.OrderType),
					Pair(MaskLocation, MaskLocationLabel),
					Pair(MaskShipVia, Messages.ShipVia),
					Pair(MaskCompany, AR.Messages.MaskCompany),
					Pair(MaskEmployee, AR.Messages.MaskEmployee),
				};

			public SubListAttribute() : base(Pairs) { }
			protected override Tuple<string, string>[] GetPairs() => Pairs;
		}

		public const string MaskShipVia = "V";
		public const string MaskLocation = "L";
		public const string OrderType = "T";
        public const string MaskCompany = "C";
		public const string MaskEmployee = "E";
	}

	[PXDBString(30, IsUnicode = true, InputMask = "")]
	[PXUIField(DisplayName = "Subaccount Mask", Visibility = PXUIVisibility.Visible, FieldClass = _DimensionName)]
    [SOFreightAcctSubDefault.SubList]
	public sealed class SOFreightSubAccountMaskAttribute : AcctSubAttribute
	{
		private const string _DimensionName = "SUBACCOUNT";
		private const string _MaskName = "ZZZZZZZZZC";
		public SOFreightSubAccountMaskAttribute()
			: base()
		{
			PXDimensionMaskAttribute attr = new PXDimensionMaskAttribute(_DimensionName, _MaskName, SOFreightAcctSubDefault.MaskLocation, new SOFreightAcctSubDefault.SubListAttribute().AllowedValues, new SOFreightAcctSubDefault.SubListAttribute().AllowedLabels);
			attr.ValidComboRequired = false;
			_Attributes.Add(attr);
			_SelAttrIndex = _Attributes.Count - 1;
		}

        public override void GetSubscriber<ISubscriber>(List<ISubscriber> subscribers)
        {
            base.GetSubscriber<ISubscriber>(subscribers);
            subscribers.Remove(_Attributes.OfType<SOFreightAcctSubDefault.SubListAttribute>().FirstOrDefault() as ISubscriber);
        }

        public override void CacheAttached(PXCache sender)
        {
            base.CacheAttached(sender);
            var stringlist = (SOFreightAcctSubDefault.SubListAttribute)_Attributes.First(x => x.GetType() == typeof(SOFreightAcctSubDefault.SubListAttribute));
            var dimensionmaskattribute = (PXDimensionMaskAttribute)_Attributes.First(x => x.GetType() == typeof(PXDimensionMaskAttribute));
            dimensionmaskattribute.SynchronizeLabels(stringlist.AllowedValues, stringlist.AllowedLabels);
        }

		public static string MakeSub<Field>(PXGraph graph, string mask, object[] sources, Type[] fields)
			where Field : IBqlField 
		{
			try
			{
				return PXDimensionMaskAttribute.MakeSub<Field>(graph, mask, new SOFreightAcctSubDefault.SubListAttribute().AllowedValues, 0, sources);
			}
			catch (PXMaskArgumentException ex)
			{
				PXCache cache = graph.Caches[BqlCommand.GetItemType(fields[ex.SourceIdx])];
				string fieldName = fields[ex.SourceIdx].Name;
				throw new PXMaskArgumentException(new SOFreightAcctSubDefault.SubListAttribute().AllowedLabels[ex.SourceIdx], PXUIFieldAttribute.GetDisplayName(cache, fieldName));
			}
		}
	}




	public class SODiscAcctSubDefault
	{
		public class AcctListAttribute : SOCustomListAttribute
			{
			private static Tuple<string, string>[] Pairs =>
				new[]
				{
					Pair(OrderType, Messages.OrderType),
					Pair(MaskLocation, MaskLocationLabel),
				};

			public AcctListAttribute() : base(Pairs) {}
			protected override Tuple<string, string>[] GetPairs() => Pairs;
		}

		public class SubListAttribute : SOCustomListAttribute
		{
			private static Tuple<string, string>[] Pairs =>
				new[]
			{
					Pair(OrderType, Messages.OrderType),
					Pair(MaskLocation, MaskLocationLabel),
					Pair(MaskCompany, AR.Messages.MaskCompany),
				};

			public SubListAttribute() : base(Pairs) {}
			protected override Tuple<string, string>[] GetPairs() => Pairs;
		}

		public const string OrderType = "T";
		public const string MaskLocation = "L";
        public const string MaskCompany = "C";
	}

	[PXDBString(30, IsUnicode = true, InputMask = "")]
	[PXUIField(DisplayName = "Subaccount Mask", Visibility = PXUIVisibility.Visible, FieldClass = _DimensionName)]
    [SODiscAcctSubDefault.SubList]
	public sealed class SODiscSubAccountMaskAttribute : AcctSubAttribute
	{
		private const string _DimensionName = "SUBACCOUNT";
		private const string _MaskName = "ZZZZZZZZZD";
		public SODiscSubAccountMaskAttribute()
			: base()
		{
			PXDimensionMaskAttribute attr = new PXDimensionMaskAttribute(_DimensionName, _MaskName, SODiscAcctSubDefault.MaskLocation, new SODiscAcctSubDefault.SubListAttribute().AllowedValues, new SODiscAcctSubDefault.SubListAttribute().AllowedLabels);
			attr.ValidComboRequired = false;
			_Attributes.Add(attr);
			_SelAttrIndex = _Attributes.Count - 1;
		}

        public override void GetSubscriber<ISubscriber>(List<ISubscriber> subscribers)
        {
            base.GetSubscriber<ISubscriber>(subscribers);
            subscribers.Remove(_Attributes.OfType<SODiscAcctSubDefault.SubListAttribute>().FirstOrDefault() as ISubscriber);
        }

        public override void CacheAttached(PXCache sender)
        {
            base.CacheAttached(sender);
            var stringlist = (SODiscAcctSubDefault.SubListAttribute)_Attributes.First(x => x.GetType() == typeof(SODiscAcctSubDefault.SubListAttribute));
            var dimensionmaskattribute = (PXDimensionMaskAttribute)_Attributes.First(x => x.GetType() == typeof(PXDimensionMaskAttribute));
            dimensionmaskattribute.SynchronizeLabels(stringlist.AllowedValues, stringlist.AllowedLabels);
        }

		public static string MakeSub<Field>(PXGraph graph, string mask, object[] sources, Type[] fields)
			where Field : IBqlField 
		{
			try
			{
				return PXDimensionMaskAttribute.MakeSub<Field>(graph, mask, new SODiscAcctSubDefault.SubListAttribute().AllowedValues, 0, sources);
			}
			catch (PXMaskArgumentException ex)
			{
				PXCache cache = graph.Caches[BqlCommand.GetItemType(fields[ex.SourceIdx])];
				string fieldName = fields[ex.SourceIdx].Name;
				throw new PXMaskArgumentException(new SODiscAcctSubDefault.SubListAttribute().AllowedLabels[ex.SourceIdx], PXUIFieldAttribute.GetDisplayName(cache, fieldName));
			}
		}
	}

	public class SOCOGSAcctSubDefault
	{
		public class AcctListAttribute : SOCustomListAttribute
		{
			private static Tuple<string, string>[] Pairs =>
				new[]
			{
					Pair(MaskItem, IN.Messages.MaskItem),
					Pair(MaskSite, IN.Messages.MaskSite),
					Pair(MaskClass, IN.Messages.MaskClass),
					Pair(MaskLocation, MaskLocationLabel),
				};

			public AcctListAttribute() : base(Pairs) {}
			protected override Tuple<string, string>[] GetPairs() => Pairs;
		}

		public class SubListAttribute : SOCustomListAttribute
			{
			private static Tuple<string, string>[] Pairs =>
				new[]
            {
					Pair(MaskItem, IN.Messages.MaskItem),
					Pair(MaskSite, IN.Messages.MaskSite),
					Pair(MaskClass, IN.Messages.MaskClass),
					Pair(MaskLocation, MaskLocationLabel),
					Pair(MaskEmployee, AR.Messages.MaskEmployee),
					Pair(MaskCompany, AR.Messages.MaskCompany),
					Pair(MaskSalesPerson, AR.Messages.MaskSalesPerson),
				};

			public SubListAttribute() : base(Pairs) {}
			protected override Tuple<string, string>[] GetPairs() => Pairs;
		}

		public const string MaskItem = "I";
		public const string MaskSite = "W";
		public const string MaskClass = "P";

		public const string MaskLocation = "L";
		public const string MaskEmployee = "E";
		public const string MaskCompany = "C";
		public const string MaskSalesPerson = "S";
	}

	[PXDBString(30, IsUnicode = true, InputMask = "")]
	[PXUIField(DisplayName = "Subaccount Mask", Visibility = PXUIVisibility.Visible, FieldClass = _DimensionName)]
    [SOCOGSAcctSubDefault.SubList]
	public sealed class SOCOGSSubAccountMaskAttribute : AcctSubAttribute
	{
		private const string _DimensionName = "SUBACCOUNT";
		private const string _MaskName = "ZZZZZZZZZE";
		public SOCOGSSubAccountMaskAttribute()
			: base()
		{
			PXDimensionMaskAttribute attr = new PXDimensionMaskAttribute(_DimensionName, _MaskName, SOCOGSAcctSubDefault.MaskItem, new SOCOGSAcctSubDefault.SubListAttribute().AllowedValues, new SOCOGSAcctSubDefault.SubListAttribute().AllowedLabels);
			attr.ValidComboRequired = false;
			_Attributes.Add(attr);
			_SelAttrIndex = _Attributes.Count - 1;
		}

        public override void GetSubscriber<ISubscriber>(List<ISubscriber> subscribers)
        {
            base.GetSubscriber<ISubscriber>(subscribers);
            subscribers.Remove(_Attributes.OfType<SOCOGSAcctSubDefault.SubListAttribute>().FirstOrDefault() as ISubscriber);
        }

        public override void CacheAttached(PXCache sender)
        {
            base.CacheAttached(sender);
            var stringlist = (SOCOGSAcctSubDefault.SubListAttribute)_Attributes.First(x => x.GetType() == typeof(SOCOGSAcctSubDefault.SubListAttribute));
            var dimensionmaskattribute = (PXDimensionMaskAttribute)_Attributes.First(x => x.GetType() == typeof(PXDimensionMaskAttribute));
            dimensionmaskattribute.SynchronizeLabels(stringlist.AllowedValues, stringlist.AllowedLabels);
        }

		public static string MakeSub<Field>(PXGraph graph, string mask, object[] sources, Type[] fields)
			where Field : IBqlField
		{
			try
			{
				return PXDimensionMaskAttribute.MakeSub<Field>(graph, mask, new SOCOGSAcctSubDefault.SubListAttribute().AllowedValues, 3, sources);
			}
			catch (PXMaskArgumentException ex)
			{
				PXCache cache = graph.Caches[BqlCommand.GetItemType(fields[ex.SourceIdx])];
				string fieldName = fields[ex.SourceIdx].Name;
				throw new PXMaskArgumentException(new SOCOGSAcctSubDefault.SubListAttribute().AllowedLabels[ex.SourceIdx], PXUIFieldAttribute.GetDisplayName(cache, fieldName));
			}
		}
	}

	public class SOSiteStatusLookup<Status, StatusFilter> : INSiteStatusLookup<Status, StatusFilter>
		where Status : class, IBqlTable, new()
		where StatusFilter:  SOSiteStatusFilter, new()
	{
		#region Ctor
		public SOSiteStatusLookup(PXGraph graph)
			:base(graph)
		{
			graph.RowSelecting.AddHandler(typeof(SOSiteStatusSelected), OnRowSelecting);
		}

		public SOSiteStatusLookup(PXGraph graph, Delegate handler)
			:base(graph, handler)
		{
			graph.RowSelecting.AddHandler(typeof(SOSiteStatusSelected), OnRowSelecting);
		}
		#endregion
		protected virtual void OnRowSelecting(PXCache sender, PXRowSelectingEventArgs e)
		{
			if (sender.Fields.Contains(typeof(SOSiteStatusSelected.curyID).Name) &&
					sender.GetValue<SOSiteStatusSelected.curyID>(e.Row) == null)
			{
				PXCache orderCache = sender.Graph.Caches[typeof(SOOrder)];
				sender.SetValue<SOSiteStatusSelected.curyID>(e.Row,
					orderCache.GetValue<SOOrder.curyID>(orderCache.Current));
				sender.SetValue<SOSiteStatusSelected.curyInfoID>(e.Row,
					orderCache.GetValue<SOOrder.curyInfoID>(orderCache.Current));				
			}
		}

		protected override void OnFilterSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			base.OnFilterSelected(sender, e);
			SOSiteStatusFilter filter = (SOSiteStatusFilter)e.Row;
			PXUIFieldAttribute.SetVisible<SOSiteStatusFilter.historyDate>(sender, null, filter.Mode == SOAddItemMode.ByCustomer);
			PXUIFieldAttribute.SetVisible<SOSiteStatusFilter.dropShipSales>(sender, null, filter.Mode == SOAddItemMode.ByCustomer);
			sender.Adjust<PXUIFieldAttribute>().For<SOSiteStatusFilter.customerLocationID>(a =>
				a.Visible = a.Enabled = (filter.Behavior == SOBehavior.BL));

			PXCache status = sender.Graph.Caches[typeof (SOSiteStatusSelected)];
			PXUIFieldAttribute.SetVisible<SOSiteStatusSelected.qtyLastSale>(status, null, filter.Mode == SOAddItemMode.ByCustomer);
			PXUIFieldAttribute.SetVisible<SOSiteStatusSelected.curyID>(status, null, filter.Mode == SOAddItemMode.ByCustomer);
			PXUIFieldAttribute.SetVisible<SOSiteStatusSelected.curyUnitPrice>(status, null, filter.Mode == SOAddItemMode.ByCustomer);
			PXUIFieldAttribute.SetVisible<SOSiteStatusSelected.lastSalesDate>(status, null, filter.Mode == SOAddItemMode.ByCustomer);

			status.Adjust<PXUIFieldAttribute>()
				.For<SOSiteStatusSelected.dropShipLastBaseQty>(x => { x.Visible = filter.DropShipSales == true; })
				.SameFor<SOSiteStatusSelected.dropShipLastQty>()
				.SameFor<SOSiteStatusSelected.dropShipLastUnitPrice>()
				.SameFor<SOSiteStatusSelected.dropShipCuryUnitPrice>()
				.SameFor<SOSiteStatusSelected.dropShipLastDate>();

			if (filter.HistoryDate == null)
				filter.HistoryDate =  sender.Graph.Accessinfo.BusinessDate.GetValueOrDefault().AddMonths(-3);
		}
	}
	#region SOOpenPeriod
	/// <summary>
	/// Specialized version of the selector for SO Open Financial Periods.<br/>
	/// Displays a list of FinPeriods, having flags Active = true and  ARClosed = false and INClosed = false.<br/>
	/// </summary>
	public class SOOpenPeriodAttribute : OpenPeriodAttribute
	{
		#region Ctor
		public SOOpenPeriodAttribute(Type sourceType,
			Type branchSourceType,
			Type branchSourceFormulaType = null,
			Type organizationSourceType = null,
			Type useMasterCalendarSourceType = null,
			Type defaultType = null,
			bool redefaultOrRevalidateOnOrganizationSourceUpdated = true,
			bool useMasterOrganizationIDByDefault = false,
			Type masterFinPeriodIDType = null)
			: base(
				typeof(Search<FinPeriod.finPeriodID, 
							Where2<Where<Current<SOInvoice.createINDoc>, Equal<False>, 
											Or<FinPeriod.iNClosed, Equal<False>>>,
									And<FinPeriod.aRClosed, Equal<False>, 
									And<Where<FinPeriod.status, Equal<FinPeriod.status.open>>>>>>),
				sourceType,
				branchSourceType: branchSourceType,
				branchSourceFormulaType: branchSourceFormulaType,
				organizationSourceType: organizationSourceType,
				useMasterCalendarSourceType: useMasterCalendarSourceType,
				defaultType: defaultType,
				redefaultOrRevalidateOnOrganizationSourceUpdated: redefaultOrRevalidateOnOrganizationSourceUpdated,
				useMasterOrganizationIDByDefault: useMasterOrganizationIDByDefault,
				masterFinPeriodIDType: masterFinPeriodIDType)
		{
		}

		public SOOpenPeriodAttribute(Type SourceType)
			: this(SourceType, null)
		{
		}

		public SOOpenPeriodAttribute()
			: this(null)
		{
		}
		#endregion

		#region Implementation

		public override void FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
		{
			if (_ValidatePeriod != PeriodValidation.Nothing)
			{
				OpenPeriodVerifying(sender, e);
			}
		}

		protected override PeriodValidationResult ValidateOrganizationFinPeriodStatus(PXCache sender, object row, FinPeriod finPeriod)
		{
			PeriodValidationResult result = base.ValidateOrganizationFinPeriodStatus(sender, row, finPeriod);

			if (!result.HasWarningOrError)
			{
				if (finPeriod.ARClosed == true)
				{
					result.Aggregate(HandleErrorThatPeriodIsClosed(sender, finPeriod, errorMessage: AR.Messages.FinancialPeriodClosedInAR));
			}

				if (finPeriod.INClosed == true)
			{
					bool? createINDoc = ((SOInvoice)sender.Graph.Caches<SOInvoice>().Current)?.CreateINDoc;

				if (createINDoc == true)
				{
						result.Aggregate(HandleErrorThatPeriodIsClosed(sender, finPeriod, errorMessage: IN.Messages.FinancialPeriodClosedInIN));
				}
			}
		}
			
			return result;
		}

		#endregion
	}
	#endregion

	#region SOFinPeriodAttribute
	public class SOFinPeriodAttribute : OpenPeriodAttribute
	{
		public SOFinPeriodAttribute()
			: this(null)
		{
		}

		public SOFinPeriodAttribute(Type SourceType)
			: this(SourceType, null)
		{
		}

		public SOFinPeriodAttribute(Type sourceType,
			Type branchSourceType,
			Type branchSourceFormulaType = null,
			Type organizationSourceType = null,
			Type useMasterCalendarSourceType = null,
			Type defaultType = null,
			bool redefaultOrRevalidateOnOrganizationSourceUpdated = true,
			bool useMasterOrganizationIDByDefault = false)
			: base(
				typeof(Search<FinPeriod.finPeriodID, Where<FinPeriod.aRClosed, Equal<False>>>),
				sourceType,
				branchSourceType: branchSourceType,
				branchSourceFormulaType: branchSourceFormulaType,
				organizationSourceType: organizationSourceType,
				useMasterCalendarSourceType: useMasterCalendarSourceType,
				defaultType: defaultType,
				redefaultOrRevalidateOnOrganizationSourceUpdated: redefaultOrRevalidateOnOrganizationSourceUpdated,
				useMasterOrganizationIDByDefault: useMasterOrganizationIDByDefault)
		{
		}

		public static void DefaultFirstOpenPeriod(PXCache sender, string FieldName)
		{
			AROpenPeriodAttribute.DefaultFirstOpenPeriod(sender, FieldName);
		}

		public static void DefaultFirstOpenPeriod<Field>(PXCache sender)
			where Field : IBqlField
		{
			DefaultFirstOpenPeriod(sender, typeof(Field).Name);
		}

		protected override PeriodValidationResult ValidateOrganizationFinPeriodStatus(PXCache sender, object row, FinPeriod finPeriod)
		{
			PeriodValidationResult validationResult = new PeriodValidationResult();

			if (finPeriod.Status == FinPeriod.status.Closed || finPeriod.ARClosed == true)
			{
				validationResult = HandleErrorThatPeriodIsClosed(sender, finPeriod);

				return validationResult;
			}

			if (finPeriod.Status == FinPeriod.status.Locked)
			{
				validationResult.AddMessage(
					PXErrorLevel.Warning,
					ExceptionType.Locked,
					GL.Messages.FinPeriodIsLockedInCompany,
					FormatForError(finPeriod.FinPeriodID),
					PXAccess.GetOrganizationCD(finPeriod.OrganizationID));
				
				return validationResult;
			}

			if (finPeriod.Status == FinPeriod.status.Inactive)
			{
				validationResult.AddMessage(
					PXErrorLevel.Warning,
					ExceptionType.Inactive,
					GL.Messages.FinPeriodIsInactiveInCompany,
					FormatForError(finPeriod.FinPeriodID),
					PXAccess.GetOrganizationCD(finPeriod.OrganizationID));

				return validationResult;
			}

			return validationResult;
		}
	}
	#endregion

	public class SOCashSaleCashTranIDAttribute : CashTranIDAttribute
	{
		public override CATran DefaultValues(PXCache sender, CATran catran_Row, object orig_Row)
		{
			SOInvoice soinvoice = (SOInvoice)orig_Row;
			if (soinvoice.Released == true || soinvoice.CuryPaymentAmt == null || soinvoice.CuryPaymentAmt == 0m)
			{
				return null;
			}

			catran_Row.OrigModule = BatchModule.AR;
			catran_Row.OrigTranType = soinvoice.DocType;
			catran_Row.OrigRefNbr = soinvoice.RefNbr;
			catran_Row.ExtRefNbr = soinvoice.ExtRefNbr;
			catran_Row.CashAccountID = soinvoice.CashAccountID;
			catran_Row.CuryInfoID = soinvoice.CuryInfoID;
			catran_Row.CuryID = soinvoice.CuryID;

			switch (soinvoice.DocType)
			{
				case ARDocType.CashSale:
					catran_Row.CuryTranAmt = soinvoice.CuryPaymentAmt;
					catran_Row.DrCr = DrCr.Debit;
					break;
				case ARDocType.CashReturn:
					catran_Row.CuryTranAmt = -soinvoice.CuryPaymentAmt;
					catran_Row.DrCr = DrCr.Credit;
					break;
				default:
					return null;
			}

			catran_Row.TranDate = soinvoice.AdjDate;
			catran_Row.TranDesc = soinvoice.DocDesc;
			SetPeriodsByMaster(sender, catran_Row, soinvoice.AdjTranPeriodID);
            catran_Row.ReferenceID = soinvoice.CustomerID;
			catran_Row.Released = false;
			catran_Row.Hold = soinvoice.Hold;
			catran_Row.Cleared = soinvoice.Cleared;
			catran_Row.ClearDate = soinvoice.ClearDate;

			return catran_Row;
		}
	}

	/// <summary>
	/// Extends <see cref="LocationAvailAttribute"/> and shows the availability of Inventory Item for the given location.
	/// </summary>
	/// <example>
	/// [SOLocationAvail(typeof(SOLine.inventoryID), typeof(SOLine.subItemID), typeof(SOLine.siteID), typeof(SOLine.tranType), typeof(SOLine.invtMult))]
	/// </example>
	public class SOLocationAvailAttribute : LocationAvailAttribute
	{ 
		public SOLocationAvailAttribute(Type InventoryType, Type SubItemType, Type SiteIDType, Type TranType, Type InvtMultType)
			: base(InventoryType, SubItemType, SiteIDType, null, null, null)
		{
			_IsSalesType = BqlCommand.Compose(
				typeof(Where<,,>),
				TranType,
				typeof(Equal<INTranType.issue>),
				typeof(Or<,,>),
				TranType,
				typeof(Equal<INTranType.invoice>),
				typeof(Or<,>),
				TranType,
				typeof(Equal<INTranType.debitMemo>)
				);

			_IsReceiptType = BqlCommand.Compose(
				typeof(Where<,,>),
				TranType,
				typeof(Equal<INTranType.receipt>),
				typeof(Or<,,>),
				TranType,
				typeof(Equal<INTranType.return_>),
				typeof(Or<,>),
				TranType,
				typeof(Equal<INTranType.creditMemo>)
				);

			_IsTransferType = BqlCommand.Compose(
				typeof(Where<,,>),
				TranType,
				typeof(Equal<INTranType.transfer>),
				typeof(And<,,>),
				InvtMultType,
				typeof(Equal<short1>),
				typeof(Or<,,>),
				TranType,
				typeof(Equal<INTranType.transfer>),
				typeof(And<,>),
				InvtMultType,
				typeof(Equal<shortMinus1>)
				);

			_IsStandardCostAdjType = BqlCommand.Compose(
				typeof(Where<,,>),
				TranType,
				typeof(Equal<INTranType.standardCostAdjustment>),
				typeof(Or<,>),
				TranType,
				typeof(Equal<INTranType.negativeCostAdjustment>)
				);

			this._Attributes.Add(new PrimaryItemRestrictorAttribute(InventoryType, _IsReceiptType, _IsSalesType, _IsTransferType));
			this._Attributes.Add(new LocationRestrictorAttribute(_IsReceiptType, _IsSalesType, _IsTransferType));
		}
		public override void FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			SOShipLine parentLine = PXParentAttribute.SelectParent(sender, e.Row, typeof (SOShipLine)) as SOShipLine;
			if (parentLine != null && parentLine.IsUnassigned == true)
				return;
			base.FieldDefaulting(sender, e);
		}
	}

	/// <summary>
	/// Specialized for SOLine version of the CrossItemAttribute.<br/> 
	/// Providing an Inventory ID selector for the field, it allows also user <br/>
	/// to select both InventoryID and SubItemID by typing AlternateID in the control<br/>
	/// As a result, if user type a correct Alternate id, values for InventoryID, SubItemID, <br/>
	/// and AlternateID fields in the row will be set.<br/>
	/// In this attribute, InventoryItems with a status inactive, markedForDeletion,<br/>
	/// noSale and noRequest are filtered out. It also fixes  INPrimaryAlternateType parameter to CPN <br/>    
	/// This attribute may be used in combination with AlternativeItemAttribute on the AlternateID field of the row <br/>
	/// <example>
	/// [SOLineInventoryItem(Filterable = true)]
	/// </example>
	/// </summary>
	[PXDBInt()]
	[PXUIField(DisplayName = "Inventory ID", Visibility = PXUIVisibility.Visible)]
	[PXRestrictor(typeof(
		Where<InventoryItem.itemStatus, NotEqual<InventoryItemStatus.noSales>,
			Or<Current<SOOrderType.behavior>, Equal<SOBehavior.tR>,
			Or<Current<SOLine.operation>, Equal<SOOperation.receipt>>>>),
		IN.Messages.ItemCannotSale)]
	public class SOLineInventoryItemAttribute : CrossItemAttribute
	{

		/// <summary>
		/// Default ctor
		/// </summary>
		public SOLineInventoryItemAttribute()
			: base(typeof(Search<InventoryItem.inventoryID, Where<Match<Current<AccessInfo.userName>>>>), typeof(InventoryItem.inventoryCD), typeof(InventoryItem.descr), INPrimaryAlternateType.CPN)
		{
		}
	}

	[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter | AttributeTargets.Method , AllowMultiple = false)]
	public class SOShipmentEntryActionsAttribute : PXIntListAttribute
	{
		public const int ConfirmShipment = 1;
		public const int CreateInvoice = 2;
		public const int PostInvoiceToIN = 3;
		public const int ApplyAssignmentRules = 4;
		public const int CorrectShipment = 5;
		public const int CreateDropshipInvoice = 6;
		public const int PrintLabels = 7;
		public const int GetReturnLabels = 8;
		public const int CancelReturn = 9;
		public const int PrintPickList = 10;
		public const int PrintShipmentConfirmation = 11;

		public SOShipmentEntryActionsAttribute() : base(
			new[]
			{
				Pair(ConfirmShipment, Messages.ConfirmShipment),
				Pair(CreateInvoice, Messages.CreateInvoice),
				Pair(PostInvoiceToIN, Messages.PostInvoiceToIN),
				Pair(ApplyAssignmentRules, Messages.ApplyAssignmentRules),
				Pair(CorrectShipment, Messages.CorrectShipment),
				Pair(CreateDropshipInvoice, Messages.CreateDropshipInvoice),
				Pair(PrintLabels, Messages.PrintLabels),
				Pair(GetReturnLabels, Messages.GetReturnLabels),
				Pair(CancelReturn, Messages.CancelReturn),
				Pair(PrintPickList, Messages.PrintPickList),
				Pair(PrintShipmentConfirmation, Messages.PrintShipmentConfirmation),
			}) {}
		
		
		[PXLocalizable]
		public class Messages
		{
			public const string ConfirmShipment = "Confirm Shipment";
			public const string CreateInvoice = "Prepare Invoice";
			public const string PostInvoiceToIN = "Update IN";
			public const string ApplyAssignmentRules = "Apply Assignment Rules";
			public const string CorrectShipment = "Correct Shipment";
			public const string CreateDropshipInvoice = "Prepare Drop-Ship Invoice";
			public const string PrintLabels = "Print Labels";
			public const string GetReturnLabels = "Get Return Labels";
			public const string CancelReturn = "Cancel Return";
			public const string PrintPickList = "Print Pick List";
			public const string PrintShipmentConfirmation = "Print Shipment Confirmation";
		}
	}

	public class SOReports : PX.SM.ReportUtils
	{
		public const string PrintLabels = "SO645000";
		public const string PrintPickList = "SO644000";
		public const string PrintShipmentConfirmation = "SO642000";
		public const string PrintInvoiceReport = "SO643000";
		public const string PrintSalesOrder = "SO641010";
		public const string PrintPackSlipBatch = "SO644005";
		public const string PrintPackSlipWave = "SO644007";
		public const string PrintPickerPickList = "SO644006";

		public static string GetReportID(int? actionID, string actionName)
		{
			if (actionID != null)
			{
				switch (actionID)
				{
					case SOShipmentEntryActionsAttribute.PrintLabels:
						return PrintLabels;
					case SOShipmentEntryActionsAttribute.PrintPickList:
						return PrintPickList;
					case SOShipmentEntryActionsAttribute.PrintShipmentConfirmation:
						return PrintShipmentConfirmation;
				}
			}
			if (actionName != null)
			{
				switch (actionName)
				{
					case SOShipmentEntryActionsAttribute.Messages.PrintLabels:
					case SOInvoiceShipment.WellKnownActions.SOShipmentScreen.PrintLabels:
						return PrintLabels;
					case SOShipmentEntryActionsAttribute.Messages.PrintPickList:
					case SOInvoiceShipment.WellKnownActions.SOShipmentScreen.PrintPickList:
						return PrintPickList;
					case SOShipmentEntryActionsAttribute.Messages.PrintShipmentConfirmation:
					case SOInvoiceShipment.WellKnownActions.SOShipmentScreen.PrintShipmentConfirmation:
						return PrintShipmentConfirmation;
				}
			}
			return null;
		}

	}

	/// <summary>
	/// PXSelector Marker to be used in PXFormula when a field cannot be decorated with PXSelector Attribute (Example: A field is already defined with StringList attribute to render a Combo box).
	/// </summary>
	public class PXSelectorMarkerAttribute : PXSelectorAttribute
	{
		public PXSelectorMarkerAttribute(Type type) : base(type)
		{
		}

		public override void GetSubscriber<ISubscriber>(List<ISubscriber> subscribers)
		{
			if (typeof(ISubscriber) == typeof(IPXFieldSelectingSubscriber) || typeof(ISubscriber) == typeof(IPXFieldVerifyingSubscriber))
			{
				//do nothing.
			}
			else
			{
				base.GetSubscriber<ISubscriber>(subscribers);
			}
		}
	}
}
