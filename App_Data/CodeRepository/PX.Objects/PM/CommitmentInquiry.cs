using PX.Data;
using PX.Objects.CM.Extensions;
using PX.Objects.IN;
using PX.Objects.PO;
using System;
using System.Collections;
using PX.Objects.Common;
using PX.Data.BQL.Fluent;
using PX.Data.BQL;

namespace PX.Objects.PM
{
	[GL.TableDashboardType]
	[Serializable]
	public class CommitmentInquiry : PXGraph<CommitmentInquiry>
	{
		[Obsolete]
		protected virtual void _(Events.CacheAttached<POLine.orderType> e) { }

		[PXBool]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		protected virtual void _(Events.CacheAttached<PMCostCode.isProjectOverride> e)
		{
		}

		public PXFilter<ProjectBalanceFilter> Filter;
		public PXCancel<ProjectBalanceFilter> Cancel;

		[PXFilterable]
		[PXViewName(Messages.Commitments)]
		public
			SelectFrom<PMCommitment>.
			LeftJoin<POLineCommitment>.On<PMCommitment.commitmentID.IsEqual<POLineCommitment.commitmentID>>.
			Where<
				Brackets<PMCommitment.projectID.IsEqual<ProjectBalanceFilter.projectID.FromCurrent>.Or<ProjectBalanceFilter.projectID.FromCurrent.IsNull>>.
				And<PMCommitment.accountGroupID.IsEqual<ProjectBalanceFilter.accountGroupID.FromCurrent>.Or<ProjectBalanceFilter.accountGroupID.FromCurrent.IsNull>>.
				And<PMCommitment.projectTaskID.IsEqual<ProjectBalanceFilter.projectTaskID.FromCurrent>.Or<ProjectBalanceFilter.projectTaskID.FromCurrent.IsNull>>.
				And<PMCommitment.costCodeID.IsEqual<ProjectBalanceFilter.costCode.FromCurrent>.Or<ProjectBalanceFilter.costCode.FromCurrent.IsNull>>.
				And<PMCommitment.inventoryID.IsEqual<ProjectBalanceFilter.inventoryID.FromCurrent>.Or<ProjectBalanceFilter.inventoryID.FromCurrent.IsNull>>.
				And<POLineCommitment.relatedDocumentType.IsEqual<ProjectBalanceFilter.relatedDocumentType.FromCurrent>.Or<ProjectBalanceFilter.relatedDocumentType.FromCurrent.IsEqual<PX.Objects.CN.Subcontracts.PM.Descriptor.RelatedDocumentType.all>>>
			>.
			View Items;

		public
			SelectFrom<PMCommitment>.
			LeftJoin<POLineCommitment>.On<PMCommitment.commitmentID.IsEqual<POLineCommitment.commitmentID>>.
			Where<
				Brackets<PMCommitment.projectID.IsEqual<ProjectBalanceFilter.projectID.FromCurrent>.Or<ProjectBalanceFilter.projectID.FromCurrent.IsNull>>.
				And<PMCommitment.accountGroupID.IsEqual<ProjectBalanceFilter.accountGroupID.FromCurrent>.Or<ProjectBalanceFilter.accountGroupID.FromCurrent.IsNull>>.
				And<PMCommitment.projectTaskID.IsEqual<ProjectBalanceFilter.projectTaskID.FromCurrent>.Or<ProjectBalanceFilter.projectTaskID.FromCurrent.IsNull>>.
				And<PMCommitment.costCodeID.IsEqual<ProjectBalanceFilter.costCode.FromCurrent>.Or<ProjectBalanceFilter.costCode.FromCurrent.IsNull>>.
				And<PMCommitment.inventoryID.IsEqual<ProjectBalanceFilter.inventoryID.FromCurrent>.Or<ProjectBalanceFilter.inventoryID.FromCurrent.IsNull>>.
				And<POLineCommitment.relatedDocumentType.IsEqual<ProjectBalanceFilter.relatedDocumentType.FromCurrent>.Or<ProjectBalanceFilter.relatedDocumentType.FromCurrent.IsEqual<PX.Objects.CN.Subcontracts.PM.Descriptor.RelatedDocumentType.all>>>
			>.
			AggregateTo<Sum<PMCommitment.qty>, Sum<PMCommitment.amount>, Sum<PMCommitment.openQty>, Sum<PMCommitment.openAmount>, Sum<PMCommitment.receivedQty>, Sum<PMCommitment.invoicedQty>, Sum<PMCommitment.invoicedAmount>>.
			View Totals;

		[PXCopyPasteHiddenView]
		[PXHidden]
		public PXSelect<PMCostCode> dummyCostCode;

		public PXAction<ProjectBalanceFilter> createCommitment;
		[PXUIField(DisplayName = Messages.CreateCommitment)]
		[PXButton(Tooltip = Messages.CreateCommitment)]
		public virtual IEnumerable CreateCommitment(PXAdapter adapter)
		{
			ExternalCommitmentEntry graph = PXGraph.CreateInstance<ExternalCommitmentEntry>();
			throw new PXPopupRedirectException(graph, Messages.CommitmentEntry + " - " + Messages.CreateCommitment, true);
		}

		public PXAction<ProjectBalanceFilter> viewProject;
		[PXUIField(DisplayName = Messages.ViewProject, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable ViewProject(PXAdapter adapter)
		{
			if (Items.Current != null)
			{
				var service = PXGraph.CreateInstance<PM.ProjectAccountingService>();
				service.NavigateToProjectScreen(Items.Current.ProjectID, PXRedirectHelper.WindowMode.NewWindow);
			}
			return adapter.Get();
		}

		public PXAction<ProjectBalanceFilter> viewExternalCommitment;
		[PXUIField(DisplayName = Messages.ViewExternalCommitment, MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable ViewExternalCommitment(PXAdapter adapter)
		{
			if (Items.Current != null)
			{
				var graph = CreateInstance<ExternalCommitmentEntry>();
				graph.Commitments.Current = Items.Current;
				throw new PXRedirectRequiredException(graph, true, Messages.ViewExternalCommitment) { Mode = PXBaseRedirectException.WindowMode.NewWindow };
			}
			return adapter.Get();
		}

		
		#region Local Types

		[PXHidden]
		[Serializable]
		[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
		public partial class ProjectBalanceFilter : IBqlTable
		{
			#region ProjectID
			public abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID> { }
			protected Int32? _ProjectID;
			[Project(typeof(Where<PMProject.nonProject, Equal<False>, And<PMProject.baseType, Equal<CT.CTPRType.project>>>))]
			public virtual Int32? ProjectID
			{
				get
				{
					return this._ProjectID;
				}
				set
				{
					this._ProjectID = value;
				}
			}
			#endregion

			#region AccountGroupID
			public abstract class accountGroupID : PX.Data.BQL.BqlInt.Field<accountGroupID> { }
			protected Int32? _AccountGroupID;
			[AccountGroupAttribute()]
			public virtual Int32? AccountGroupID
			{
				get
				{
					return this._AccountGroupID;
				}
				set
				{
					this._AccountGroupID = value;
				}
			}
			#endregion
			#region ProjectTaskID
			public abstract class projectTaskID : PX.Data.BQL.BqlInt.Field<projectTaskID> { }
			protected Int32? _ProjectTaskID;
			[ProjectTask(typeof(ProjectBalanceFilter.projectID))]
			public virtual Int32? ProjectTaskID
			{
				get
				{
					return this._ProjectTaskID;
				}
				set
				{
					this._ProjectTaskID = value;
				}
			}
			#endregion
			#region InventoryID
			public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
			protected Int32? _InventoryID;
			[PXDBInt]
			[PXUIField(DisplayName = "Inventory ID", Visibility = PXUIVisibility.Visible)]
			[PMInventorySelector]
			public virtual Int32? InventoryID
			{
				get
				{
					return this._InventoryID;
				}
				set
				{
					this._InventoryID = value;
				}
			}
			#endregion
			#region CostCode
			public abstract class costCode : PX.Data.BQL.BqlInt.Field<costCode> { }
			[CostCode(Filterable = false, SkipVerification = true)]
			public virtual Int32? CostCode
			{
				get;
				set;
			}
			#endregion
			#region RelatedDocumentType
			public abstract class relatedDocumentType : PX.Data.BQL.BqlString.Field<relatedDocumentType> { }
			[PXString]
			[PXUIField(DisplayName = "Related Document Type")]
			[PXUnboundDefault(PX.Objects.CN.Subcontracts.PM.Descriptor.RelatedDocumentType.AllCommitmentsType)]
			[PX.Objects.CN.Subcontracts.PM.Descriptor.RelatedDocumentType.List]
			public String RelatedDocumentType
			{
				get;
				set;
			}
			#endregion
			#region Qty
			public abstract class qty : PX.Data.BQL.BqlDecimal.Field<qty> { }
			protected Decimal? _Qty;
			[PXDecimal]
			[PXUnboundDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Revised Quantity")]
			public virtual Decimal? Qty
			{
				get
				{
					return this._Qty;
				}
				set
				{
					this._Qty = value;
				}
			}
			#endregion
			#region Amount
			public abstract class amount : PX.Data.BQL.BqlDecimal.Field<amount> { }
			protected Decimal? _Amount;
			[PXBaseCury]
			[PXUnboundDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Revised Amount")]
			public virtual Decimal? Amount
			{
				get
				{
					return this._Amount;
				}
				set
				{
					this._Amount = value;
				}
			}
			#endregion
			#region OpenQty
			public abstract class openQty : PX.Data.BQL.BqlDecimal.Field<openQty> { }
			protected Decimal? _OpenQty;
			[PXDecimal]
			[PXUnboundDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Open Quantity")]
			public virtual Decimal? OpenQty
			{
				get
				{
					return this._OpenQty;
				}
				set
				{
					this._OpenQty = value;
				}
			}
			#endregion
			#region OpenAmount
			public abstract class openAmount : PX.Data.BQL.BqlDecimal.Field<openAmount> { }
			protected Decimal? _OpenAmount;
			[PXBaseCury]
			[PXUnboundDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Open Amount")]
			public virtual Decimal? OpenAmount
			{
				get
				{
					return this._OpenAmount;
				}
				set
				{
					this._OpenAmount = value;
				}
			}
			#endregion
			#region ReceivedQty
			public abstract class receivedQty : PX.Data.BQL.BqlDecimal.Field<receivedQty> { }
			protected Decimal? _ReceivedQty;
			[PXDBQuantity]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Received Quantity")]
			public virtual Decimal? ReceivedQty
			{
				get
				{
					return this._ReceivedQty;
				}
				set
				{
					this._ReceivedQty = value;
				}
			}
			#endregion
			#region InvoicedQty
			public abstract class invoicedQty : PX.Data.BQL.BqlDecimal.Field<invoicedQty> { }
			protected Decimal? _InvoicedQty;
			[PXDBQuantity]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Invoiced Quantity")]
			public virtual Decimal? InvoicedQty
			{
				get
				{
					return this._InvoicedQty;
				}
				set
				{
					this._InvoicedQty = value;
				}
			}
			#endregion
			#region InvoicedAmount
			public abstract class invoicedAmount : PX.Data.BQL.BqlDecimal.Field<invoicedAmount> { }
			protected Decimal? _InvoicedAmount;
			[PXDBBaseCury]
			[PXDefault(TypeCode.Decimal, "0.0")]
			[PXUIField(DisplayName = "Invoiced Amount")]
			public virtual Decimal? InvoicedAmount
			{
				get
				{
					return this._InvoicedAmount;
				}
				set
				{
					this._InvoicedAmount = value;
				}
			}
			#endregion
		}

		[PXHidden]
		[Serializable]
		[PXProjection(typeof(Select<POLine>), Persistent = false)]
		public partial class POLineCommitment : PX.Data.IBqlTable
		{
			#region OrderType
			public abstract class orderType : PX.Data.BQL.BqlString.Field<orderType> { }
			[PXDBString(2, IsKey = true, IsFixed = true, BqlField = typeof(POLine.orderType))]
			[PXDBDefault(typeof(POOrder.orderType))]
			[PXUIField(DisplayName = "Order Type", Visibility = PXUIVisibility.Visible, Visible = false)]
			public virtual String OrderType { get; set; }
			#endregion

			#region OrderNbr
			public abstract class orderNbr : PX.Data.BQL.BqlString.Field<orderNbr> { }
			protected String _OrderNbr;

			[PXDBString(15, IsUnicode = true, IsKey = true, InputMask = "", BqlField = typeof(POLine.orderType))]
			[PXUIField(DisplayName = "Order Nbr.", Visibility = PXUIVisibility.Invisible, Visible = false)]
			public virtual String OrderNbr { get; set; }
			#endregion

			#region LineNbr
			public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }
			[PXDBInt(IsKey = true, BqlField = typeof(POLine.lineNbr))]
			[PXUIField(DisplayName = "Line Nbr.", Visibility = PXUIVisibility.Visible, Visible = false)]
			public virtual Int32? LineNbr { get; set; }
			#endregion

			#region CommitmentID
			public abstract class commitmentID : PX.Data.BQL.BqlGuid.Field<commitmentID> { }
			[PXDBGuid(BqlField = typeof(POLine.commitmentID))]
			public virtual Guid? CommitmentID { get; set; }
			#endregion

			#region RelatedDocumentType
			public abstract class relatedDocumentType : PX.Data.BQL.BqlString.Field<relatedDocumentType> { }
			[PXString()]
			[PXUIField(DisplayName = "Related Document Type", Visibility = PXUIVisibility.Visible, Visible = true)]
			[PX.Objects.CN.Subcontracts.PM.Descriptor.RelatedDocumentType.List]
			[PXDBCalced(typeof(
				Switch<
					Case<Where<POLine.orderType, In3<POOrderType.regularOrder, POOrderType.projectDropShip>>, PX.Objects.CN.Subcontracts.PM.Descriptor.RelatedDocumentType.purchaseOrder>,
					Case<Where<POLine.orderType, Equal<POOrderType.regularSubcontract>>, PX.Objects.CN.Subcontracts.PM.Descriptor.RelatedDocumentType.subcontract>>),
				typeof(string))]
			public virtual String RelatedDocumentType { get; set; }
			#endregion
		}

		[Obsolete("Use PX.Objects.CN.Subcontracts.PM.Descriptor.RelatedDocumentType")]
		public class RelatedDocumentType
		{
			public const string AllCommitmentsType = "ALL";
			public const string PurchaseOrderType = "RO";
			public const string SubcontractType = "RS";
			public const string ProjectDropShipType = "PD";

			public const string AllCommitmentsLabel = "All Commitments";
			public const string PurchaseOrderLabel = "Purchase Order";
			public const string SubcontractLabel = "Subcontract";
			public const string ProjectDropShipLabel = "Project Drop-Ship";

			private static readonly string[] RelatedDocumentTypes =
			{
				AllCommitmentsType,
				PurchaseOrderType,
			SubcontractType,
			ProjectDropShipType
			};

			private static readonly string[] RelatedDocumentLabels =
			{
				AllCommitmentsLabel,
				PurchaseOrderLabel,
			SubcontractLabel,
			ProjectDropShipLabel
			};

			public class ListAttribute : PXStringListAttribute
			{
				public ListAttribute()
					: base(RelatedDocumentTypes, RelatedDocumentLabels)
				{
				}
			}

			public class all : PX.Data.BQL.BqlString.Constant<all>
			{
				public all() : base(AllCommitmentsType) {; }
			}
		}
		#endregion
	}
}
