using PX.Data;
using PX.Objects.AP;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.IN;
using PX.Objects.SO;
using PX.Objects.CM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.PO
{
	/// <summary>
	/// Specialized version of the Projection Attribute. Defines Projection as <br/>
	/// a select of INItemPlan Join INPlanType Join InventoryItem Join INUnit Left Join INItemSite <br/>
	/// filtered by InventoryItem.workgroupID and InventoryItem.productManagerID according to the values <br/>
	/// in the POCreateFilter: <br/>
	/// 1. POCreateFilter.ownerID is null or  POCreateFilter.ownerID = InventoryItem.productManagerID <br/>
	/// 2. POCreateFilter.workGroupID is null or  POCreateFilter.workGroupID = InventoryItem.productWorkgroupID <br/>
	/// 3. POCreateFilter.myWorkGroup = false or  InventoryItem.productWorkgroupID =InMember{POCreateFilter.currentOwnerID} <br/>
	/// 4. InventoryItem.productWorkgroupID is null or  InventoryItem.productWorkgroupID =Owened{POCreateFilter.currentOwnerID}<br/>
	/// </summary>
	public class POCreateProjectionAttribute : TM.OwnedFilter.ProjectionAttribute
	{
		/// <summary>
		/// Default ctor
		/// </summary>
		public POCreateProjectionAttribute()
			: base(typeof(POCreate.POCreateFilter),
			BqlCommand.Compose(
		typeof(Select2<,,>),
			typeof(INItemPlan),
			typeof(InnerJoin<INPlanType,
						  On<INItemPlan.FK.PlanType>,
			InnerJoin<InventoryItem,
				On<INItemPlan.FK.InventoryItem>,
			InnerJoin<INSite,
				On<INSite.siteID, Equal<INItemPlan.siteID>>,
			InnerJoin<INUnit,
				On<INUnit.inventoryID, Equal<InventoryItem.inventoryID>,
				And<INUnit.fromUnit, Equal<InventoryItem.purchaseUnit>,
				And<INUnit.toUnit, Equal<InventoryItem.baseUnit>>>>,
			LeftJoin<SOLineSplit, On<SOLineSplit.planID, Equal<INItemPlan.planID>>,
			LeftJoin<SOLine, On<SOLineSplit.orderType, Equal<SOLine.orderType>, And<SOLineSplit.orderNbr, Equal<SOLine.orderNbr>, And<SOLineSplit.lineNbr, Equal<SOLine.lineNbr>>>>,
			LeftJoin<IN.S.INItemSite, On<IN.S.INItemSite.inventoryID, Equal<INItemPlan.inventoryID>, And<IN.S.INItemSite.siteID, Equal<INItemPlan.siteID>>>,
			LeftJoin<INItemClass,
				On<InventoryItem.FK.ItemClass>>>>>>>>>),
		typeof(Where2<,>),
		typeof(Where<INItemPlan.hold, Equal<False>,
				And<INItemPlan.fixedSource, Equal<INReplenishmentSource.purchased>,
				And<INPlanType.isFixed, Equal<True>, And<INPlanType.isDemand, Equal<True>,
				And<Where<INItemPlan.supplyPlanID, IsNull, And<INItemPlan.planType, NotIn3<INPlanConstants.plan6B, INPlanConstants.plan6E>,
					Or<INItemPlan.supplyPlanID, IsNotNull, And<INItemPlan.planType, In3<INPlanConstants.plan6B, INPlanConstants.plan6E>>>>>>>>>>),
		typeof(And<>),
		TM.OwnedFilter.ProjectionAttribute.ComposeWhere(
		typeof(POCreate.POCreateFilter),
		typeof(INItemSite.productWorkgroupID),
		typeof(INItemSite.productManagerID))))
		{
		}
	}

	[POCreateProjection]
	[Serializable]
	public partial class POFixedDemand : INItemPlan
	{
		#region Selected
		public new abstract class selected : PX.Data.BQL.BqlBool.Field<selected> { }
		#endregion
		#region InventoryID
		public new abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
		#endregion
		#region SiteID
		public new abstract class siteID : PX.Data.BQL.BqlInt.Field<siteID> { }
		#endregion
		#region RequestedDate
		public new abstract class requestedDate : PX.Data.BQL.BqlDateTime.Field<requestedDate> { }
		[PXDate]
		[PXDBCalced(typeof(Switch<Case<Where<INItemPlan.planType, In3<INPlanConstants.plan6D, INPlanConstants.plan6E>>, SOLine.requestDate>, INItemPlan.planDate>), typeof(DateTime?))]
		[PXUIField(DisplayName = "Requested On")]
		public virtual DateTime? RequestedDate
		{
			get;
			set;
		}
		#endregion
		#region PlanID
		public new abstract class planID : PX.Data.BQL.BqlLong.Field<planID> { }
		#endregion
		#region FixedSource
		public new abstract class fixedSource : PX.Data.BQL.BqlString.Field<fixedSource> { }
		[PXDBString(1, IsFixed = true)]
		[PXUIField(DisplayName = "Fixed Source", Enabled = false)]
		[PXDefault(INReplenishmentSource.Purchased, PersistingCheck = PXPersistingCheck.Nothing)]
		[INReplenishmentSource.INPlanList]
		public override String FixedSource
		{
			get
			{
				return this._FixedSource;
			}
			set
			{
				this._FixedSource = value;
			}
		}
		#endregion
		#region PlanType
		public new abstract class planType : PX.Data.BQL.BqlString.Field<planType> { }
		[PXDBString(2, IsFixed = true)]
		[PXDefault()]
		[PXUIField(DisplayName = "Plan Type")]
		[PXSelector(typeof(Search<INPlanType.planType>), CacheGlobal = true, DescriptionField = typeof(INPlanType.descr))]
		public override String PlanType
		{
			get
			{
				return this._PlanType;
			}
			set
			{
				this._PlanType = value;
			}
		}
		#endregion
		#region SubItemID
		public new abstract class subItemID : PX.Data.BQL.BqlInt.Field<subItemID> { }
		#endregion
		#region LocationID
		public new abstract class locationID : PX.Data.BQL.BqlInt.Field<locationID> { }
		#endregion
		#region LotSerialNbr
		public new abstract class lotSerialNbr : PX.Data.BQL.BqlString.Field<lotSerialNbr> { }
		#endregion
		#region SourceSiteID
		public new abstract class sourceSiteID : PX.Data.BQL.BqlInt.Field<sourceSiteID> { }
		[IN.Site(DisplayName = "Demand Warehouse", DescriptionField = typeof(INSite.descr), BqlField = typeof(INItemPlan.sourceSiteID))]
		[PXFormula(typeof(Default<POFixedDemand.fixedSource>))]
		[PXDefault(typeof(Search<INItemSiteSettings.replenishmentSourceSiteID,
			Where<INItemSiteSettings.inventoryID, Equal<Current<POFixedDemand.inventoryID>>,
			And<INItemSiteSettings.siteID, Equal<Current<POFixedDemand.siteID>>,
			And<Where<Current<POFixedDemand.fixedSource>, Equal<INReplenishmentSource.transfer>,
			Or<Current<POFixedDemand.fixedSource>, Equal<INReplenishmentSource.purchased>>>>>>>),
		PersistingCheck = PXPersistingCheck.Nothing)]
		public override Int32? SourceSiteID
		{
			get
			{
				return this._SourceSiteID;
			}
			set
			{
				this._SourceSiteID = value;
			}
		}
		#endregion
		#region SourceSiteDescr
		public abstract class sourceSiteDescr : PX.Data.BQL.BqlString.Field<sourceSiteDescr> { }
		protected String _SourceSiteDescr;
		[PXFormula(typeof(Selector<sourceSiteID, INSite.descr>))]
		[PXString(60, IsUnicode = true)]
		[PXUIField(DisplayName = "Demand Warehouse Description", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual String SourceSiteDescr
		{
			get
			{
				return this._SourceSiteDescr;
			}
			set
			{
				this._SourceSiteDescr = value;
			}
		}
		#endregion
		#region POSiteID
		public abstract class pOSiteID : PX.Data.BQL.BqlInt.Field<pOSiteID> { }
		protected Int32? _POSiteID;
		[PXDBCalced(typeof(IsNull<SOLineSplit.pOSiteID, INItemPlan.siteID>), typeof(int))]
		[PXUIField(DisplayName = "Warehouse", Visibility = PXUIVisibility.Visible, FieldClass = SiteAttribute.DimensionName)]
		[PXDimensionSelector(SiteAttribute.DimensionName, typeof(Search<INSite.siteID>), typeof(INSite.siteCD), DescriptionField = typeof(INSite.descr), CacheGlobal = true)]
		public virtual Int32? POSiteID
		{
			get
			{
				return this._POSiteID;
			}
			set
			{
				this._POSiteID = value;
			}
		}
		#endregion
		#region VendorID
		public new abstract class vendorID : PX.Data.BQL.BqlInt.Field<vendorID> { }
		[Vendor(typeof(Search<BAccountR.bAccountID,
			Where<Vendor.type, NotEqual<BAccountType.employeeType>>>))]
		[PXRestrictor(
			typeof(Where<Vendor.vStatus, IsNull,
				Or<Vendor.vStatus, In3<VendorStatus.active, VendorStatus.oneTime, VendorStatus.holdPayments>>>),
			AP.Messages.VendorIsInStatus, typeof(Vendor.vStatus))]
		[PXFormula(typeof(Default<POFixedDemand.fixedSource>))]
		[PXDefault(typeof(Coalesce<
			Search2<BAccountR.bAccountID,
			InnerJoin<INItemSiteSettings, On<INItemSiteSettings.inventoryID, Equal<Current<POFixedDemand.inventoryID>>, And<INItemSiteSettings.siteID, Equal<Current<POFixedDemand.siteID>>>>,
			LeftJoin<INSite, On<INSite.siteID, Equal<INItemSiteSettings.replenishmentSourceSiteID>>,
			LeftJoin<GL.Branch, On<GL.Branch.branchID, Equal<INSite.branchID>>>>>,
			Where<INItemSiteSettings.preferredVendorID, Equal<BAccountR.bAccountID>, And<Current<POFixedDemand.fixedSource>, NotEqual<INReplenishmentSource.transfer>,
					Or<GL.Branch.bAccountID, Equal<BAccountR.bAccountID>, And<Current<POFixedDemand.fixedSource>, Equal<INReplenishmentSource.transfer>>>>>>,
			Search<InventoryItemCurySettings.preferredVendorID,
				Where<InventoryItemCurySettings.inventoryID, Equal<Current<POFixedDemand.inventoryID>>,
					And<InventoryItemCurySettings.curyID, EqualBaseCuryID<Current<POCreate.POCreateFilter.branchID>>>>>>),
			PersistingCheck = PXPersistingCheck.Nothing)]
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
		#region VendorLocationID
		public new abstract class vendorLocationID : PX.Data.BQL.BqlInt.Field<vendorLocationID> { }
		[LocationID(typeof(Where<Location.bAccountID, Equal<Current<POFixedDemand.vendorID>>>), DescriptionField = typeof(Location.descr), Visibility = PXUIVisibility.SelectorVisible)]
		[PXFormula(typeof(Default<POFixedDemand.vendorID>))]
		public override Int32? VendorLocationID
		{
			get
			{
				return this._VendorLocationID;
			}
			set
			{
				this._VendorLocationID = value;
			}
		}
		#endregion
		#region RecordID
		public abstract class recordID : PX.Data.BQL.BqlInt.Field<recordID> { }
		protected Int32? _RecordID;
		[PXDBScalar(typeof(Search2<POVendorInventory.recordID,
			InnerJoin<BAccountR, On<BAccountR.bAccountID, Equal<POVendorInventory.vendorID>>,
			InnerJoin<InventoryItem,
				On<POVendorInventory.FK.InventoryItem>>>,
			Where<POVendorInventory.vendorID, Equal<INItemPlan.vendorID>,
			  And<POVendorInventory.inventoryID, Equal<INItemPlan.inventoryID>,
				And<POVendorInventory.active, Equal<boolTrue>,
				And2<Where<POVendorInventory.vendorLocationID, Equal<INItemPlan.vendorLocationID>,
						Or<POVendorInventory.vendorLocationID, IsNull>>,
				And<Where<POVendorInventory.subItemID, Equal<INItemPlan.subItemID>,
							 Or<POVendorInventory.subItemID, Equal<InventoryItem.defaultSubItemID>>>>>>>>,
			OrderBy<
			Asc<Switch<Case<Where<POVendorInventory.vendorLocationID, Equal<INItemPlan.vendorLocationID>>, boolFalse>, boolTrue>,
			Asc<Switch<Case<Where<POVendorInventory.subItemID, Equal<INItemPlan.subItemID>>, boolFalse>, boolTrue>>>>>))]
		[PXFormula(typeof(Default<POFixedDemand.vendorLocationID>))]
		public virtual Int32? RecordID
		{
			get
			{
				return this._RecordID;
			}
			set
			{
				this._RecordID = value;
			}
		}
		#endregion
		#region SupplyPlanID
		public new abstract class supplyPlanID : PX.Data.BQL.BqlLong.Field<supplyPlanID> { }
		#endregion
		#region PlanQty
		public new abstract class planQty : PX.Data.BQL.BqlDecimal.Field<planQty> { }
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Requested Qty.")]
		public override Decimal? PlanQty
		{
			get
			{
				return this._PlanQty;
			}
			set
			{
				this._PlanQty = value;
			}
		}
		#endregion
		#region PlanUOM
		public abstract class planUOM : PX.Data.BQL.BqlString.Field<planUOM> { }

		[PXDBString(BqlField = typeof(INItemPlan.uOM))]
		public virtual string PlanUOM
		{
			get;
			set;
		}
		#endregion
		#region UOM
		public new abstract class uOM : PX.Data.BQL.BqlString.Field<uOM> { }

		[PXString]
		[PXDBCalced(typeof(Switch<
			Case<Where<SOLine.orderNbr, IsNotNull,
				And<SOLine.pOSource, Equal<INReplenishmentSource.dropShipToOrder>>>, SOLine.uOM>,
			INUnit.fromUnit>),
			typeof(string))]
		[PXUIField(DisplayName = "UOM")]
		public new virtual String UOM
		{
			get;
			set;
		}
		#endregion
		#region UnitMultDiv
		public abstract class unitMultDiv : PX.Data.BQL.BqlString.Field<unitMultDiv> { }
		protected String _UnitMultDiv;
		[PXDBString(1, IsFixed = true, BqlField = typeof(INUnit.unitMultDiv))]
		public virtual String UnitMultDiv
		{
			get
			{
				return this._UnitMultDiv;
			}
			set
			{
				this._UnitMultDiv = value;
			}
		}
		#endregion
		#region UnitRate
		public abstract class unitRate : PX.Data.BQL.BqlDecimal.Field<unitRate> { }
		protected Decimal? _UnitRate;
		[PXDBDecimal(6, BqlField = typeof(INUnit.unitRate))]
		public virtual Decimal? UnitRate
		{
			get
			{
				return this._UnitRate;
			}
			set
			{
				this._UnitRate = value;
			}
		}
		#endregion
		#region PlanUnitQty
		public abstract class planUnitQty : PX.Data.BQL.BqlDecimal.Field<planUnitQty> { }
		protected Decimal? _PlanUnitQty;
		[PXDBCalced(typeof(Switch<
			Case<Where<SOLine.orderNbr, IsNotNull,
				And<SOLine.pOSource, Equal<INReplenishmentSource.dropShipToOrder>>>, SOLine.openQty,
			Case<Where<INUnit.unitMultDiv, Equal<MultDiv.divide>>, Mult<INItemPlan.planQty, INUnit.unitRate>>>,
			Div<INItemPlan.planQty, INUnit.unitRate>>),
			typeof(decimal))]
		[PXQuantity()]
		public virtual Decimal? PlanUnitQty
		{
			get
			{
				return this._PlanUnitQty;
			}
			set
			{
				this._PlanUnitQty = value;
			}
		}
		#endregion
		#region OrderQty
		public abstract class orderQty : PX.Data.BQL.BqlDecimal.Field<orderQty> { }
		protected Decimal? _OrderQty;
		[PXQuantity()]
		[PXUIField(DisplayName = "Quantity")]
		public virtual Decimal? OrderQty
		{
			[PXDependsOnFields(typeof(planUnitQty))]
			get
			{
				return this._OrderQty ?? this._PlanUnitQty;
			}
			set
			{
				this._OrderQty = value;
			}
		}
		#endregion
		#region RefNoteID
		public new abstract class refNoteID : PX.Data.BQL.BqlGuid.Field<refNoteID> { }
		[PXRefNote()]
		[PXUIField(DisplayName = "Reference Nbr.")]
		public override Guid? RefNoteID
		{
			get
			{
				return this._RefNoteID;
			}
			set
			{
				this._RefNoteID = value;
			}
		}
		#endregion
		#region Hold
		public new abstract class hold : PX.Data.BQL.BqlBool.Field<hold> { }
		#endregion
		#region VendorID_Vendor_acctName
		public abstract class vendorID_Vendor_acctName : PX.Data.BQL.BqlString.Field<vendorID_Vendor_acctName> { }
		#endregion
		#region InventoryID_InventoryItem_descr
		public abstract class inventoryID_InventoryItem_descr : PX.Data.BQL.BqlString.Field<inventoryID_InventoryItem_descr> { }
		#endregion
		#region SiteID_INSite_descr
		public abstract class siteID_INSite_descr : PX.Data.BQL.BqlString.Field<siteID_INSite_descr> { }
		#endregion
		#region AddLeadTimeDays
		public abstract class addLeadTimeDays : PX.Data.BQL.BqlShort.Field<addLeadTimeDays> { }
		protected Int16? _AddLeadTimeDays;
		[PXShort()]
		[PXUIField(DisplayName = "Add. Lead Time (Days)")]
		public virtual Int16? AddLeadTimeDays
		{
			get
			{
				return this._AddLeadTimeDays;
			}
			set
			{
				this._AddLeadTimeDays = value;
			}
		}
		#endregion
		#region EffPrice
		public abstract class effPrice : PX.Data.BQL.BqlDecimal.Field<effPrice> { }
		protected Decimal? _EffPrice;
		[PXPriceCost()]
		[PXUIField(DisplayName = "Vendor Price", Enabled = false)]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? EffPrice
		{
			get
			{
				return this._EffPrice;
			}
			set
			{
				this._EffPrice = value;
			}
		}
		#endregion
		#region ExtWeight
		public abstract class extWeight : PX.Data.BQL.BqlDecimal.Field<extWeight> { }
		protected Decimal? _ExtWeight;
		[PXDecimal(6)]
		[PXUIField(DisplayName = "Weight")]
		[PXFormula(typeof(Mult<POFixedDemand.planQty, Selector<POFixedDemand.inventoryID, InventoryItem.baseWeight>>))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? ExtWeight
		{
			get
			{
				return this._ExtWeight;
			}
			set
			{
				this._ExtWeight = value;
			}
		}
		#endregion
		#region ExtVolume
		public abstract class extVolume : PX.Data.BQL.BqlDecimal.Field<extVolume> { }
		protected Decimal? _ExtVolume;
		[PXDecimal(6)]
		[PXUIField(DisplayName = "Volume")]
		[PXFormula(typeof(Mult<POFixedDemand.planQty, Selector<POFixedDemand.inventoryID, InventoryItem.baseVolume>>))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? ExtVolume
		{
			get
			{
				return this._ExtVolume;
			}
			set
			{
				this._ExtVolume = value;
			}
		}
		#endregion
		#region ExtCost
		public abstract class extCost : PX.Data.BQL.BqlDecimal.Field<extCost> { }
		protected Decimal? _ExtCost;
		[PXDecimal(typeof(Search<Currency.decimalPlaces, Where<Currency.curyID, Equal<Selector<Current<POCreate.POCreateFilter.vendorID>, Vendor.curyID>>>>))]
		[PXUIField(DisplayName = "Extended Amt.", Enabled = false)]
		[PXFormula(typeof(Mult<POFixedDemand.orderQty, POFixedDemand.effPrice>))]
		public virtual Decimal? ExtCost
		{
			get
			{
				return this._ExtCost;
			}
			set
			{
				this._ExtCost = value;
			}
		}
		#endregion
		#region AlternateID
		public abstract class alternateID : PX.Data.BQL.BqlString.Field<alternateID> { }
		protected String _AlternateID;
		[PXUIField(DisplayName = "Alternate ID")]
		[PXDBString(50, IsUnicode = true, InputMask = "", BqlField = typeof(SOLine.alternateID))]
		public virtual String AlternateID
		{
			get
			{
				return this._AlternateID;
			}
			set
			{
				this._AlternateID = value;
			}
		}
		#endregion
		#region PlanProjectID
		public abstract class planProjectID : PX.Data.BQL.BqlInt.Field<planProjectID> { }
		[PXDBInt(BqlField = typeof(INItemPlan.projectID))]
		public virtual int? PlanProjectID
		{
			get;
			set;
		}
		#endregion
		#region ProjectID
		public new abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID> { }
		[PXDBInt(BqlField = typeof(SOLine.projectID))]
		public new virtual int? ProjectID
		{
			get;
			set;
		}
		#endregion
		#region OrderType
		public abstract class orderType : PX.Data.BQL.BqlString.Field<orderType> { }
		protected String _OrderType;
		[PXDBString(2, IsFixed = true, BqlField = typeof(SOLineSplit.orderType))]
		public virtual String OrderType
		{
			get
			{
				return this._OrderType;
			}
			set
			{
				this._OrderType = value;
			}
		}
		#endregion
		#region OrderNbr
		public abstract class orderNbr : PX.Data.BQL.BqlString.Field<orderNbr> { }
		protected String _OrderNbr;
		[PXDBString(15, IsUnicode = true, InputMask = "", BqlField = typeof(SOLineSplit.orderNbr))]
		public virtual String OrderNbr
		{
			get
			{
				return this._OrderNbr;
			}
			set
			{
				this._OrderNbr = value;
			}
		}
		#endregion
		#region LineNbr
		public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }
		protected Int32? _LineNbr;
		[PXDBInt(BqlField = typeof(SOLineSplit.lineNbr))]
		public virtual Int32? LineNbr
		{
			get
			{
				return this._LineNbr;
			}
			set
			{
				this._LineNbr = value;
			}
		}
		#endregion
		#region BaseQty
		public abstract class baseQty : PX.Data.BQL.BqlDecimal.Field<baseQty> { }
		protected Decimal? _BaseQty;
		[PXDBDecimal(6, BqlField = typeof(SOLineSplit.baseQty))]
		public virtual Decimal? BaseQty
		{
			get
			{
				return this._BaseQty;
			}
			set
			{
				this._BaseQty = value;
			}
		}
		#endregion
		#region BaseShippedQty
		public abstract class baseShippedQty : PX.Data.BQL.BqlDecimal.Field<baseShippedQty> { }
		protected Decimal? _BaseShippedQty;
		[PXDBDecimal(6, MinValue = 0, BqlField = typeof(SOLineSplit.baseShippedQty))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		public virtual Decimal? BaseShippedQty
		{
			get
			{
				return this._BaseShippedQty;
			}
			set
			{
				this._BaseShippedQty = value;
			}
		}
		#endregion
		#region Behavior
		public abstract class behavior : Data.BQL.BqlString.Field<behavior> { }
		[PXDBString(2, IsFixed = true, BqlField = typeof(SOLineSplit.behavior))]
		public virtual string Behavior
		{
			get;
			set;
		}
		#endregion
		#region POCreateDate
		public abstract class pOCreateDate : Data.BQL.BqlDateTime.Field<pOCreateDate> { }
		[PXDBDate(BqlField = typeof(SOLineSplit.pOCreateDate))]
		public virtual DateTime? POCreateDate
		{
			get;
			set;
		}
		#endregion

		#region ItemClassCD
		public abstract class itemClassCD : PX.Data.BQL.BqlString.Field<itemClassCD> { }
		protected String _ItemClassCD;
		[PXDefault()]
		[PXDBString(30, IsUnicode = true, InputMask = "", BqlField = typeof(INItemClass.itemClassCD))]
		[PXUIField(DisplayName = "Class ID", Visibility = PXUIVisibility.SelectorVisible)]
		[PXDimensionSelector(INItemClass.Dimension, typeof(Search<INItemClass.itemClassCD, Where<INItemClass.stkItem, Equal<False>, Or<Where<INItemClass.stkItem, Equal<True>, And<FeatureInstalled<FeaturesSet.distributionModule>>>>>>), DescriptionField = typeof(INItemClass.descr))]
		[PX.Data.EP.PXFieldDescription]
		public virtual String ItemClassCD
		{
			get
			{
				return this._ItemClassCD;
			}
			set
			{
				this._ItemClassCD = value;
			}
		}
		#endregion
	}
}
