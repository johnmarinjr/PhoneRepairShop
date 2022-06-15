using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CM.Extensions;
using PX.Objects.IN;
using System;

namespace PX.Objects.PM
{
	/// <summary>Is the base class for <see cref="PMChangeOrderRevenueBudget" /> and <see cref="PMChangeOrderCostBudget" /> types. The DAC provides fields common to these types.</summary>
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	[PXCacheName(Messages.Budget)]
	[Serializable]
	public class PMChangeOrderBudget : IBqlTable, IProjectFilter, IQuantify
	{
		#region Keys
		/// <summary>
		/// Primary Key
		/// </summary>
		/// <exclude />
		public class PK : PrimaryKeyOf<PMChangeOrderBudget>.By<refNbr, projectID, projectTaskID, accountGroupID, costCodeID, inventoryID>
		{
			public static PMChangeOrderBudget Find(PXGraph graph, string refNbr, int? projectID, int? projectTaskID, int? accountGroupID, int? costCodeID, int? inventoryID) => FindBy(graph, refNbr, projectID, projectTaskID, accountGroupID, costCodeID, inventoryID);
		}

		/// <summary>
		/// Foreign Keys
		/// </summary>
		/// <exclude />
		public static class FK
		{
			/// <summary>
			/// Change Order
			/// </summary>
			/// <exclude />
			public class ChangeOrder : PMProject.PK.ForeignKeyOf<PMChangeOrderBudget>.By<refNbr> { }

			/// <summary>
			/// Project
			/// </summary>
			/// <exclude />
			public class Project : PMProject.PK.ForeignKeyOf<PMChangeOrderBudget>.By<projectID> { }

			/// <summary>
			/// Project Task
			/// </summary>
			/// <exclude />
			public class ProjectTask : PMTask.PK.ForeignKeyOf<PMChangeOrderBudget>.By<projectTaskID> { }

			/// <summary>
			/// Account Group
			/// </summary>
			/// <exclude />
			public class AccountGroup : PMAccountGroup.PK.ForeignKeyOf<PMChangeOrderBudget>.By<accountGroupID> { }

			/// <summary>
			/// Cost Code
			/// </summary>
			/// <exclude />
			public class CostCode : PMCostCode.PK.ForeignKeyOf<PMChangeOrderBudget>.By<costCodeID> { }

			/// <summary>
			/// Inventory Item
			/// </summary>
			/// <exclude />
			public class Item : IN.InventoryItem.PK.ForeignKeyOf<PMChangeOrderBudget>.By<inventoryID> { }
		}

		#endregion

		#region RefNbr
		public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr>
		{
			public const int Length = 15;
		}

		/// <summary>
		/// The reference number of the parent <see cref="PMChangeOrder">change order</see>.
		/// </summary>
		[PXDBString(refNbr.Length, IsUnicode = true, IsKey = true, InputMask = "")]
		[PXDBDefault(typeof(PMChangeOrder.refNbr))]
		[PXUIField(DisplayName = "Ref. Nbr.", Enabled = false)]
		public virtual String RefNbr
		{
			get;
			set;
		}
		#endregion
		#region ProjectID
		public abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID> { }
		protected Int32? _ProjectID;

		/// <summary>The identifier of the <see cref="PMProject">project</see> associated with the change order line.</summary>
		/// <value>
		/// Defaults to the <see cref="PMChangeOrder.ProjectID">project</see> of the parent change order.
		/// The value of this field corresponds to the value of the <see cref="PMProject.ContractID" /> field.
		/// </value>
		[PXDefault(typeof(PMChangeOrder.projectID))]
		[PXForeignReference(typeof(Field<projectID>.IsRelatedTo<PMProject.contractID>))]
		[PXDBInt(IsKey = true)]
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
		#region ProjectTaskID
		public abstract class projectTaskID : PX.Data.BQL.BqlInt.Field<projectTaskID> { }

		/// <summary>The identifier of the <see cref="PMTask">task</see> associated with the change order line.</summary>
		/// <value>
		/// The value of this field corresponds to the value of the <see cref="PMTask.TaskID" /> field.
		/// </value>
		public int? TaskID => ProjectTaskID;

		[PXDefault(typeof(Search<PMTask.taskID, Where<PMTask.projectID, Equal<Current<projectID>>, And<PMTask.isDefault, Equal<True>>>>))]
		[PXDBInt(IsKey = true)]
		[PXForeignReference(typeof(Field<projectTaskID>.IsRelatedTo<PMTask.taskID>))]
		public virtual Int32? ProjectTaskID
		{
			get;
			set;
		}
		#endregion
		#region CostCodeID
		public abstract class costCodeID : PX.Data.BQL.BqlInt.Field<costCodeID> { }
		protected Int32? _CostCodeID;

		/// <summary>The identifier of the <see cref="PMCostCode">cost code</see> associated with the change order line.</summary>
		/// <value>
		/// The value of this field corresponds to the value of the <see cref="PMCostCode.costCodeID" /> field.
		/// </value>
		[CostCode(IsKey = true, ReleasedField = typeof(released))]
		public virtual Int32? CostCodeID
		{
			get
			{
				return this._CostCodeID;
			}
			set
			{
				this._CostCodeID = value;
			}
		}
		#endregion
		#region AccountGroupID
		public abstract class accountGroupID : PX.Data.BQL.BqlInt.Field<accountGroupID> { }
		protected Int32? _AccountGroupID;

		/// <summary>The identifier of the <see cref="PMAccountGroup">account group</see> associated with the change order line.</summary>
		/// <value>
		/// The value of this field corresponds to the value of the <see cref="PMAccountGroup.GroupID" /> field.
		/// </value>
		[PXRestrictor(typeof(Where<PMAccountGroup.isActive, Equal<True>>), PM.Messages.InactiveAccountGroup, typeof(PMAccountGroup.groupCD))]
		[PXDefault]
		[AccountGroup(IsKey = true)]
		[PXForeignReference(typeof(Field<accountGroupID>.IsRelatedTo<PMAccountGroup.groupID>))]
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
		#region InventoryID
		public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
		protected Int32? _InventoryID;

		/// <summary>
		/// The identifier of the <see cref="InventoryItem">inventory item</see> associated with the change order line.
		/// </summary>
		/// <value>
		/// The value of this field corresponds to the value of the <see cref="InventoryItem.InventoryID"/> field.
		/// </value>
		[PXDBInt(IsKey = true)]
		[PXUIField(DisplayName = "Inventory ID", Visibility = PXUIVisibility.Visible)]
		[PMInventorySelector]
		[PXDefault]
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

		#region Type
		public abstract class type : PX.Data.BQL.BqlString.Field<type> { }
		protected string _Type;

		/// <summary>
		/// The type of the change order line.
		/// </summary>
		/// <value>
		/// The field can have one of the following values:
		/// <c>"A"</c>: Asset,
		/// <c>"L"</c>: Liability,
		/// <c>"I"</c>: Income,
		/// <c>"E"</c>: Expense,
		/// <c>"O"</c>: Off-Balance
		/// </value>
		[PXDBString(1)]
		[PXDefault]
		[PMAccountType.List]
		[PXUIField(DisplayName = "Type", Enabled = false)]
		public virtual string Type
		{
			get
			{
				return this._Type;
			}
			set
			{
				this._Type = value;
			}
		}
		#endregion
		#region Rate
		public abstract class rate : PX.Data.BQL.BqlDecimal.Field<rate> { }
		protected Decimal? _Rate;

		/// <summary>The rate of the specified unit of the change order line. The value can be manually modified.</summary>
		[PXDBPriceCost]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Unit Rate")]
		public virtual Decimal? Rate
		{
			get
			{
				return this._Rate;
			}
			set
			{
				this._Rate = value;
			}
		}
		#endregion
		#region Description
		public abstract class description : PX.Data.BQL.BqlString.Field<description> { }
		protected String _Description;

		/// <summary>
		/// The description of the change order line.
		/// </summary>
		[PXDBString(Common.Constants.TranDescLength, IsUnicode = true)]
		[PXUIField(DisplayName = "Description", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual String Description
		{
			get
			{
				return this._Description;
			}
			set
			{
				this._Description = value;
			}
		}
		#endregion
		#region Qty
		public abstract class qty : PX.Data.BQL.BqlDecimal.Field<qty> { }
		protected Decimal? _Qty;

		/// <summary>
		/// The quantity of the change order line.
		/// </summary>
		[PXDBQuantity]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Quantity")]
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
		#region UOM
		public abstract class uOM : PX.Data.BQL.BqlString.Field<uOM> { }
		protected String _UOM;

		/// <summary>
		/// The unit of measure of the change order line.
		/// </summary>
		[PXDefault(typeof(Search<InventoryItem.baseUnit, Where<InventoryItem.inventoryID, Equal<Current<PMBudget.inventoryID>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
		[PMUnit(typeof(inventoryID))]
		public virtual String UOM
		{
			get
			{
				return this._UOM;
			}
			set
			{
				this._UOM = value;
			}
		}
		#endregion
		#region Amount
		public abstract class amount : PX.Data.BQL.BqlDecimal.Field<amount> { }

		/// <summary>The amount of the change order line in the base currency. The value can be manually modified.</summary>
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Amount")]
		public virtual Decimal? Amount
		{
			get;
			set;
		}
		#endregion
		#region RevisedQty
		public abstract class revisedQty : PX.Data.BQL.BqlDecimal.Field<revisedQty> { }

		/// <summary>
		/// The sum of the <see cref="PMBudget.Qty">Original Budgeted Quantity</see>,
		/// <see cref="PreviouslyApprovedQty">Previously Approved CO Quantity</see>,
		/// and <see cref="Qty">Quantity</see> values.
		/// </summary>
		[PXQuantity]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Revised Budgeted Quantity", Enabled = false)]
		public virtual Decimal? RevisedQty
		{
			get;
			set;
		}
		#endregion
		#region RevisedAmount
		public abstract class revisedAmount : PX.Data.BQL.BqlDecimal.Field<revisedAmount> { }

		/// <summary>
		/// The sum of the <see cref="PMBudget.CuryAmount">Original Budgeted Amount</see>,
		/// <see cref="PreviouslyApprovedAmount">Previously Approved CO Amount</see>,
		/// and <see cref="Amount">Amount</see> values. The amount is displayed in the base currency.
		/// </summary>
		[PXBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Revised Budgeted Amount", Enabled = false)]
		public virtual Decimal? RevisedAmount
		{
			get;
			set;
		}
		#endregion

		#region ChangeRequestQty
		public abstract class changeRequestQty : PX.Data.BQL.BqlDecimal.Field<changeRequestQty> { }

		/// <summary>
		/// The total quantity of all the estimation lines of linked change requests
		/// with the same project task, account group, and cost code or inventory item.
		/// </summary>
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Change Request Total Quantity", Enabled = false, FieldClass = nameof(CS.FeaturesSet.ChangeRequest))]
		public virtual Decimal? ChangeRequestQty
		{
			get;
			set;
		}
		#endregion

		#region ChangeRequestAmount
		public abstract class changeRequestAmount : PX.Data.BQL.BqlDecimal.Field<changeRequestAmount> { }

		/// <summary>
		/// The total amount of all the estimation lines of linked change requests
		/// with the same project task, account group, and cost code or inventory item.
		/// </summary>
		[PXDBQuantity()]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Change Request Total Amount", Enabled = false, FieldClass = nameof(CS.FeaturesSet.ChangeRequest))]
		public virtual Decimal? ChangeRequestAmount
		{
			get;
			set;
		}
		#endregion

		#region IsDisabled
		public abstract class isDisabled : PX.Data.BQL.BqlBool.Field<isDisabled> { }

		/// <summary>Specifies (if set to <see langword="true"></see>) that the following change order line fields are not available for editing: <see cref="ProjectTaskID" />, <see cref="AccountGroupID" />,
		/// <see cref="CostCodeID" />, <see cref="InventoryID" />, <see cref="UOM" />.</summary>
		[PXDBBool()]
		[PXDefault(false)]
		public virtual Boolean? IsDisabled
		{
			get;
			set;
		}
		#endregion

		#region PreviouslyApprovedQty
		public abstract class previouslyApprovedQty : PX.Data.BQL.BqlDecimal.Field<previouslyApprovedQty> { }

		/// <summary>
		/// The total quantity of the released change orders that were created before the current one
		/// and that are associated with the same project, project task, account group, and cost code or inventory item.
		/// </summary>
		[PXQuantity]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual Decimal? PreviouslyApprovedQty
		{
			get;
			set;
		}
		#endregion
		#region PreviouslyApprovedAmount
		public abstract class previouslyApprovedAmount : PX.Data.BQL.BqlDecimal.Field<previouslyApprovedAmount> { }

		/// <summary>
		/// The total amount of the released change orders that were created before the current one
		/// and that are associated with the same project, project task, account group, and cost code or inventory item.
		/// The amount is displayed in the base currency.
		/// </summary>
		[PXBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual Decimal? PreviouslyApprovedAmount
		{
			get;
			set;
		}
		#endregion
		#region CommittedCOQty
		public abstract class committedCOQty : PX.Data.BQL.BqlDecimal.Field<committedCOQty> { }

		/// <summary>
		/// The total quantity of the commitment lines of the currently selected change order
		/// that are associated with the same project, project task, account group, and cost code or inventory item.
		/// </summary>
		[PXQuantity]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual Decimal? CommittedCOQty
		{
			get;
			set;
		}
		#endregion
		#region CommittedCOAmount
		public abstract class committedCOAmount : PX.Data.BQL.BqlDecimal.Field<committedCOAmount> { }

		/// <summary>
		/// The total amount of the commitment lines of the currently selected change order
		/// that are associated with the same project, project task, account group, and cost code or inventory item.
		/// The amount is displayed in the base currency.
		/// </summary>
		[PXBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual Decimal? CommittedCOAmount
		{
			get;
			set;
		}
		#endregion
		#region OtherDraftRevisedAmount
		public abstract class otherDraftRevisedAmount : PX.Data.BQL.BqlDecimal.Field<otherDraftRevisedAmount> { }

		/// <summary>The total amount of lines of the unreleased change orders (except for the current one) that refer to the cost budget line with the same project, project task,
		/// account group, and cost code or inventory item. The amount is displayed in the base currency.</summary>
		[PXBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual Decimal? OtherDraftRevisedAmount
		{
			get;
			set;
		}
		#endregion
		#region TotalPotentialRevisedAmount
		public abstract class totalPotentialRevisedAmount : PX.Data.BQL.BqlDecimal.Field<totalPotentialRevisedAmount> { }

		/// <summary>
		/// The sum of the <see cref="RevisedAmount">Revised Budgeted Amount</see> and <see cref="OtherDraftRevisedAmount">Other Draft CO Amount</see> values. 
		/// The amount is displayed in the base currency.
		/// </summary>
		[PXBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual Decimal? TotalPotentialRevisedAmount
		{
			get;
			set;
		}
		#endregion
		#region Released
		public abstract class released : PX.Data.BQL.BqlBool.Field<released> { }
		protected Boolean? _Released;

		/// <summary>
		/// Specifies (if set to <see langword="true" />) that the parent <see cref="PMChangeOrder">change order</see> has been released.
		/// </summary>
		[PXDBBool()]
		[PXUIField(DisplayName = "Released", Enabled = false)]
		[PXDefault(false)]
		public virtual Boolean? Released
		{
			get
			{
				return this._Released;
			}
			set
			{
				this._Released = value;
			}
		}
		#endregion
		#region System Columns
		#region NoteID
		public abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }
		protected Guid? _NoteID;
		[PXNote]
		public virtual Guid? NoteID
		{
			get
			{
				return this._NoteID;
			}
			set
			{
				this._NoteID = value;
			}
		}
		#endregion
		#region tstamp
		public abstract class Tstamp : PX.Data.BQL.BqlByteArray.Field<Tstamp> { }
		protected Byte[] _tstamp;
		[PXDBTimestamp()]
		public virtual Byte[] tstamp
		{
			get
			{
				return this._tstamp;
			}
			set
			{
				this._tstamp = value;
			}
		}
		#endregion
		#region CreatedByID
		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }
		protected Guid? _CreatedByID;
		[PXDBCreatedByID]
		public virtual Guid? CreatedByID
		{
			get
			{
				return this._CreatedByID;
			}
			set
			{
				this._CreatedByID = value;
			}
		}
		#endregion
		#region CreatedByScreenID
		public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }
		protected String _CreatedByScreenID;
		[PXDBCreatedByScreenID()]
		public virtual String CreatedByScreenID
		{
			get
			{
				return this._CreatedByScreenID;
			}
			set
			{
				this._CreatedByScreenID = value;
			}
		}
		#endregion
		#region CreatedDateTime
		public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }
		protected DateTime? _CreatedDateTime;
		[PXUIField(DisplayName = PXDBLastModifiedByIDAttribute.DisplayFieldNames.CreatedDateTime, Enabled = false, IsReadOnly = true)]
		[PXDBCreatedDateTime]
		public virtual DateTime? CreatedDateTime
		{
			get
			{
				return this._CreatedDateTime;
			}
			set
			{
				this._CreatedDateTime = value;
			}
		}
		#endregion
		#region LastModifiedByID
		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }
		protected Guid? _LastModifiedByID;
		[PXDBLastModifiedByID]
		public virtual Guid? LastModifiedByID
		{
			get
			{
				return this._LastModifiedByID;
			}
			set
			{
				this._LastModifiedByID = value;
			}
		}
		#endregion
		#region LastModifiedByScreenID
		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }
		protected String _LastModifiedByScreenID;
		[PXDBLastModifiedByScreenID()]
		public virtual String LastModifiedByScreenID
		{
			get
			{
				return this._LastModifiedByScreenID;
			}
			set
			{
				this._LastModifiedByScreenID = value;
			}
		}
		#endregion
		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }
		protected DateTime? _LastModifiedDateTime;
		[PXUIField(DisplayName = PXDBLastModifiedByIDAttribute.DisplayFieldNames.LastModifiedDateTime, Enabled = false, IsReadOnly = true)]
		[PXDBLastModifiedDateTime]
		public virtual DateTime? LastModifiedDateTime
		{
			get
			{
				return this._LastModifiedDateTime;
			}
			set
			{
				this._LastModifiedDateTime = value;
			}
		}
		#endregion
		#endregion
	}

	/// <summary>The DAC, which is used in reports to calculate the previously invoiced amount.</summary>
	/// <exclude />
	[PXHidden]
	[Serializable]
	[PXProjection(typeof(Select4<PMChangeOrderBudget,
		Where<PMChangeOrderBudget.refNbr, Less<Current<PMChangeOrderPrevioslyAmount.refNbr>>,
			And<PMChangeOrderBudget.type, Equal<GL.AccountType.income>,
			And<PMChangeOrderBudget.released, Equal<True>>>>,
		Aggregate<GroupBy<PMChangeOrderBudget.projectID,
			GroupBy<PMChangeOrderBudget.projectTaskID,
			GroupBy<PMChangeOrderBudget.accountGroupID,
			GroupBy<PMChangeOrderBudget.inventoryID,
			GroupBy<PMChangeOrderBudget.costCodeID,
			Sum<PMChangeOrderBudget.amount>>>>>>>>), Persistent = false)]
	public class PMChangeOrderPrevioslyAmount : IBqlTable
	{
		#region RefNbr
		public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }
		[PXDBString(PMChangeOrderBudget.refNbr.Length, IsUnicode = true, IsKey = true, BqlField = typeof(PMChangeOrderBudget.refNbr))]
		public virtual String RefNbr
		{
			get;
			set;
		}
		#endregion
		#region ProjectID
		public abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID> { }
		protected Int32? _ProjectID;
		[PXDBInt(IsKey = true, BqlField = typeof(PMChangeOrderBudget.projectID))]
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
		#region TaskID
		public abstract class taskID : PX.Data.BQL.BqlInt.Field<taskID> { }
		protected Int32? _TaskID;
		[PXDBInt(IsKey = true, BqlField = typeof(PMChangeOrderBudget.projectTaskID))]
		public virtual Int32? TaskID
		{
			get
			{
				return this._TaskID;
			}
			set
			{
				this._TaskID = value;
			}
		}
		#endregion
		#region InventoryID
		public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }
		protected Int32? _InventoryID;
		[PXDBInt(IsKey = true, BqlField = typeof(PMChangeOrderBudget.inventoryID))]
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
		#region CostCodeID
		public abstract class costCodeID : PX.Data.BQL.BqlInt.Field<costCodeID> { }
		protected Int32? _CostCodeID;
		[PXDBInt(IsKey = true, BqlField = typeof(PMChangeOrderBudget.costCodeID))]
		public virtual Int32? CostCodeID
		{
			get
			{
				return this._CostCodeID;
			}
			set
			{
				this._CostCodeID = value;
			}
		}
		#endregion
		#region AccountGroupID
		public abstract class accountGroupID : PX.Data.BQL.BqlInt.Field<accountGroupID> { }
		protected Int32? _AccountGroupID;
		[PXDBInt(IsKey = true, BqlField = typeof(PMChangeOrderBudget.accountGroupID))]
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
		#region LineTotal
		public abstract class amount : PX.Data.BQL.BqlDecimal.Field<amount> { }
		[PXDBBaseCury(BqlField = typeof(PMChangeOrderBudget.amount))]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Total")]
		public virtual Decimal? Amount
		{
			get; set;
		}
		#endregion
	}

	/// <summary>Represents a change order line with the Income type. The records of this type are created and edited through the <strong>Revenue Budget</strong> tab of the Change Orders
	/// (PM308000) form. The DAC is based on the <see cref="PMChangeOrderBudget" /> DAC.</summary>
	[PXCacheName(Messages.Budget)]
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	[PXBreakInheritance]
	[Serializable]
	public class PMChangeOrderRevenueBudget : PMChangeOrderBudget
	{
		#region Keys
		/// <summary>
		/// Primary Key
		/// </summary>
		/// <exclude />
		public new class PK : PrimaryKeyOf<PMChangeOrderRevenueBudget>.By<refNbr, projectID, projectTaskID, accountGroupID, costCodeID, inventoryID>
		{
			public static PMChangeOrderRevenueBudget Find(PXGraph graph, string refNbr, int? projectID, int? projectTaskID, int? accountGroupID, int? costCodeID, int? inventoryID) => FindBy(graph, refNbr, projectID, projectTaskID, accountGroupID, costCodeID, inventoryID);
		}

		/// <summary>
		/// Foreign Keys
		/// </summary>
		/// <exclude />
		public new static class FK
		{
			/// <summary>
			/// Change Order
			/// </summary>
			/// <exclude />
			public class ChangeOrder : PMProject.PK.ForeignKeyOf<PMChangeOrderRevenueBudget>.By<refNbr> { }

			/// <summary>
			/// Project
			/// </summary>
			/// <exclude />
			public class Project : PMProject.PK.ForeignKeyOf<PMChangeOrderRevenueBudget>.By<projectID> { }

			/// <summary>
			/// Project Task
			/// </summary>
			/// <exclude />
			public class ProjectTask : PMTask.PK.ForeignKeyOf<PMChangeOrderRevenueBudget>.By<projectTaskID> { }

			/// <summary>
			/// Account Group
			/// </summary>
			/// <exclude />
			public class AccountGroup : PMAccountGroup.PK.ForeignKeyOf<PMChangeOrderRevenueBudget>.By<accountGroupID> { }

			/// <summary>
			/// Cost Code
			/// </summary>
			/// <exclude />
			public class CostCode : PMCostCode.PK.ForeignKeyOf<PMChangeOrderRevenueBudget>.By<costCodeID> { }

			/// <summary>
			/// Inventory Item
			/// </summary>
			/// <exclude />
			public class Item : IN.InventoryItem.PK.ForeignKeyOf<PMChangeOrderRevenueBudget>.By<inventoryID> { }
		}

		#endregion

		#region RefNbr
		public new abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }

		/// <inheritdoc/>
		[PXParent(typeof(Select<PMChangeOrder, Where<PMChangeOrder.refNbr, Equal<Current<refNbr>>, And<Current<type>, Equal<GL.AccountType.income>>>>))]
		[PXDBString(PMChangeOrderBudget.refNbr.Length, IsUnicode = true, IsKey = true, InputMask = "")]
		[PXDBDefault(typeof(PMChangeOrder.refNbr))]
		[PXUIField(DisplayName = "Ref. Nbr.", Enabled = false)]
		public override String RefNbr
		{
			get;
			set;
		}
		#endregion
		#region ProjectID
		public new abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID> { }

		/// <inheritdoc/>
		[PXDefault(typeof(PMChangeOrder.projectID))]
		[PXDBInt(IsKey = true)]
		public override Int32? ProjectID
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
		#region ProjectTaskID
		public new abstract class projectTaskID : PX.Data.BQL.BqlInt.Field<projectTaskID> { }

		/// <inheritdoc/>
		[PXDefault(typeof(Search<PMTask.taskID, Where<PMTask.projectID, Equal<Current<projectID>>, And<PMTask.isDefault, Equal<True>>>>))]
		[ProjectTask(typeof(PMChangeOrderRevenueBudget.projectID), IsKey = true, AlwaysEnabled = true)]
		public override Int32? ProjectTaskID
		{
			get;
			set;
		}
		#endregion
		#region CostCodeID
		public new abstract class costCodeID : PX.Data.BQL.BqlInt.Field<costCodeID> { }

		/// <inheritdoc/>
		[CostCode(null, typeof(projectTaskID), GL.AccountType.Income, typeof(accountGroupID), IsKey = true, ReleasedField = typeof(released), SkipVerificationForDefault = true)]
		public override Int32? CostCodeID
		{
			get
			{
				return this._CostCodeID;
			}
			set
			{
				this._CostCodeID = value;
			}
		}
		#endregion
		#region AccountGroupID
		public new abstract class accountGroupID : PX.Data.BQL.BqlInt.Field<accountGroupID> { }

		/// <inheritdoc/>
		[PXRestrictor(typeof(Where<PMAccountGroup.isActive, Equal<True>>), PM.Messages.InactiveAccountGroup, typeof(PMAccountGroup.groupCD))]
		[PXDefault]
		[AccountGroup(typeof(Where<PMAccountGroup.type, Equal<GL.AccountType.income>>), IsKey = true)]
		public override Int32? AccountGroupID
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
		#region InventoryID
		public new abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }

		/// <inheritdoc/>
		[PXDBInt(IsKey = true)]
		[PXUIField(DisplayName = "Inventory ID")]
		[PXDefault]
		[PMInventorySelector]
		public override Int32? InventoryID
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
		#region Type
		public new abstract class type : PX.Data.BQL.BqlString.Field<type> { }

		/// <summary>The type of the change order line.</summary>
		/// <value>
		/// Defaults to the <see cref="GL.AccountType.Income">Income</see> type.
		/// </value>
		[PXDBString(1)]
		[PXDefault(GL.AccountType.Income)]
		[PMAccountType.List()]
		[PXUIField(DisplayName = "Budget Type", Visible = false, Enabled = false)]
		public override string Type
		{
			get
			{
				return this._Type;
			}
			set
			{
				this._Type = value;
			}
		}
		#endregion

		#region UOM
		public new abstract class uOM : PX.Data.BQL.BqlString.Field<uOM> { }

		/// <inheritdoc/>
		[PXDefault(typeof(Search<InventoryItem.baseUnit, Where<InventoryItem.inventoryID, Equal<Current<PMChangeOrderRevenueBudget.inventoryID>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
		[PMUnit(typeof(inventoryID))]
		public override String UOM
		{
			get
			{
				return this._UOM;
			}
			set
			{
				this._UOM = value;
			}
		}
		#endregion
		#region Rate
		public new abstract class rate : PX.Data.BQL.BqlDecimal.Field<rate> { }

		/// <summary>The price of the specified unit of the change order line. The value can be manually modified.</summary>
		[PXDBPriceCost]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Unit Rate")]
		public override Decimal? Rate
		{
			get
			{
				return this._Rate;
			}
			set
			{
				this._Rate = value;
			}
		}
		#endregion
		#region Description
		public new abstract class description : PX.Data.BQL.BqlString.Field<description> { }

		/// <inheritdoc/>
		[PXDBString(255, IsUnicode = true)]
		[PXUIField(DisplayName = "Description", Visibility = PXUIVisibility.SelectorVisible)]
		public override String Description
		{
			get
			{
				return this._Description;
			}
			set
			{
				this._Description = value;
			}
		}
		#endregion

		#region Amount
		public new abstract class amount : PX.Data.BQL.BqlDecimal.Field<amount> { }

		/// <inheritdoc/>
		[PXFormula(typeof(Mult<qty, rate>), typeof(SumCalc<PMChangeOrder.revenueTotal>))]
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Amount")]
		public override Decimal? Amount
		{
			get;
			set;
		}
		#endregion

		public new abstract class previouslyApprovedQty : PX.Data.BQL.BqlDecimal.Field<previouslyApprovedQty> { }
		public new abstract class previouslyApprovedAmount : PX.Data.BQL.BqlDecimal.Field<previouslyApprovedAmount> { }
		public new abstract class committedCOQty : PX.Data.BQL.BqlDecimal.Field<committedCOQty> { }
		public new abstract class committedCOAmount : PX.Data.BQL.BqlDecimal.Field<committedCOAmount> { }
		public new abstract class otherDraftRevisedAmount : PX.Data.BQL.BqlDecimal.Field<otherDraftRevisedAmount> { }
		public new abstract class totalPotentialRevisedAmount : PX.Data.BQL.BqlDecimal.Field<totalPotentialRevisedAmount> { }
	}

	/// <summary>Represents a change order line with the Expense type. The records of this type are created and edited through the <b>Cost Budget</b> tab of the Change Orders (PM308000)
	/// form. The DAC is based on the <see cref="PMChangeOrderBudget" /> DAC.</summary>
	[PXCacheName(Messages.Budget)]
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	[PXBreakInheritance]
	[Serializable]
	public class PMChangeOrderCostBudget : PMChangeOrderBudget
	{
		#region Keys
		/// <summary>
		/// Primary Key
		/// </summary>
		/// <exclude />
		public new class PK : PrimaryKeyOf<PMChangeOrderCostBudget>.By<refNbr, projectID, projectTaskID, accountGroupID, costCodeID, inventoryID>
		{
			public static PMChangeOrderCostBudget Find(PXGraph graph, string refNbr, int? projectID, int? projectTaskID, int? accountGroupID, int? costCodeID, int? inventoryID) => FindBy(graph, refNbr, projectID, projectTaskID, accountGroupID, costCodeID, inventoryID);
		}

		/// <summary>
		/// Foreign Keys
		/// </summary>
		/// <exclude />
		public new static class FK
		{
			/// <summary>
			/// Change Order
			/// </summary>
			/// <exclude />
			public class ChangeOrder : PMProject.PK.ForeignKeyOf<PMChangeOrderCostBudget>.By<refNbr> { }

			/// <summary>
			/// Project
			/// </summary>
			/// <exclude />
			public class Project : PMProject.PK.ForeignKeyOf<PMChangeOrderCostBudget>.By<projectID> { }

			/// <summary>
			/// Project Task
			/// </summary>
			/// <exclude />
			public class ProjectTask : PMTask.PK.ForeignKeyOf<PMChangeOrderCostBudget>.By<projectTaskID> { }

			/// <summary>
			/// Account Group
			/// </summary>
			/// <exclude />
			public class AccountGroup : PMAccountGroup.PK.ForeignKeyOf<PMChangeOrderCostBudget>.By<accountGroupID> { }

			/// <summary>
			/// Cost Code
			/// </summary>
			/// <exclude />
			public class CostCode : PMCostCode.PK.ForeignKeyOf<PMChangeOrderCostBudget>.By<costCodeID> { }

			/// <summary>
			/// Inventory Item
			/// </summary>
			/// <exclude />
			public class Item : IN.InventoryItem.PK.ForeignKeyOf<PMChangeOrderCostBudget>.By<inventoryID> { }
		}

		#endregion

		#region RefNbr
		public new abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }

		/// <inheritdoc/>
		[PXParent(typeof(Select<PMChangeOrder, Where<PMChangeOrder.refNbr, Equal<Current<refNbr>>, And<Current<type>, Equal<GL.AccountType.expense>>>>))]
		[PXDBString(PMChangeOrderBudget.refNbr.Length, IsUnicode = true, IsKey = true, InputMask = "")]
		[PXDBDefault(typeof(PMChangeOrder.refNbr))]
		[PXUIField(DisplayName = "Ref. Nbr.", Enabled = false)]
		public override String RefNbr
		{
			get;
			set;
		}
		#endregion
		#region ProjectID
		public new abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID> { }

		/// <inheritdoc/>
		[PXDefault(typeof(PMChangeOrder.projectID))]
		[PXDBInt(IsKey = true)]
		public override Int32? ProjectID
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
		#region ProjectTaskID
		public new abstract class projectTaskID : PX.Data.BQL.BqlInt.Field<projectTaskID> { }

		/// <inheritdoc/>
		[PXDefault(typeof(Search<PMTask.taskID, Where<PMTask.projectID, Equal<Current<projectID>>, And<PMTask.isDefault, Equal<True>>>>))]
		[ProjectTask(typeof(PMChangeOrderCostBudget.projectID), IsKey = true, AlwaysEnabled = true, DirtyRead = true)]
		public override Int32? ProjectTaskID
		{
			get;
			set;
		}
		#endregion
		#region CostCodeID
		public new abstract class costCodeID : PX.Data.BQL.BqlInt.Field<costCodeID> { }

		/// <inheritdoc/>
		[CostCode(null, typeof(projectTaskID), GL.AccountType.Expense, typeof(accountGroupID), IsKey = true, ReleasedField = typeof(released))]
		public override Int32? CostCodeID
		{
			get
			{
				return this._CostCodeID;
			}
			set
			{
				this._CostCodeID = value;
			}
		}
		#endregion
		#region AccountGroupID
		public new abstract class accountGroupID : PX.Data.BQL.BqlInt.Field<accountGroupID> { }

		/// <inheritdoc/>
		[PXRestrictor(typeof(Where<PMAccountGroup.isActive, Equal<True>>), PM.Messages.InactiveAccountGroup, typeof(PMAccountGroup.groupCD))]
		[PXDefault]
		[AccountGroup(typeof(Where<PMAccountGroup.isExpense, Equal<True>>), IsKey = true)]
		public override Int32? AccountGroupID
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
		#region InventoryID
		public new abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }

		/// <inheritdoc/>
		[PXDBInt(IsKey = true)]
		[PXUIField(DisplayName = "Inventory ID")]
		[PXDefault]
		[PMInventorySelector]
		public override Int32? InventoryID
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
		#region Type
		public new abstract class type : PX.Data.BQL.BqlString.Field<type> { }

		/// <summary>The type of the change order line.</summary>
		/// <value>
		/// Defaults to the <see cref="GL.AccountType.Expense">Expense</see> type.
		/// </value>
		[PXDBString(1)]
		[PXDefault(GL.AccountType.Expense)]
		[PMAccountType.List()]
		[PXUIField(DisplayName = "Budget Type", Visible = false, Enabled = false)]
		public override string Type
		{
			get
			{
				return this._Type;
			}
			set
			{
				this._Type = value;
			}
		}
		#endregion

		#region UOM
		public new abstract class uOM : PX.Data.BQL.BqlString.Field<uOM> { }

		/// <inheritdoc/>
		[PXDefault(typeof(Search<InventoryItem.baseUnit, Where<InventoryItem.inventoryID, Equal<Current<PMChangeOrderCostBudget.inventoryID>>>>), PersistingCheck = PXPersistingCheck.Nothing)]
		[PMUnit(typeof(inventoryID))]
		public override String UOM
		{
			get
			{
				return this._UOM;
			}
			set
			{
				this._UOM = value;
			}
		}
		#endregion
		#region Rate
		public new abstract class rate : PX.Data.BQL.BqlDecimal.Field<rate> { }

		/// <summary>The cost of the specified unit of the change order line. The value can be manually modified.</summary>
		[PXDBPriceCost]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Unit Rate")]
		public override Decimal? Rate
		{
			get
			{
				return this._Rate;
			}
			set
			{
				this._Rate = value;
			}
		}
		#endregion
		#region Description
		public new abstract class description : PX.Data.BQL.BqlString.Field<description> { }

		/// <inheritdoc/>
		[PXDBString(255, IsUnicode = true)]
		[PXUIField(DisplayName = "Description", Visibility = PXUIVisibility.SelectorVisible)]
		public override String Description
		{
			get
			{
				return this._Description;
			}
			set
			{
				this._Description = value;
			}
		}
		#endregion

		#region Amount
		public new abstract class amount : PX.Data.BQL.BqlDecimal.Field<amount> { }

		/// <inheritdoc/>
		[PXFormula(typeof(Mult<qty, rate>), typeof(SumCalc<PMChangeOrder.costTotal>))]
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Amount")]
		public override Decimal? Amount
		{
			get;
			set;
		}
		#endregion

		public new abstract class previouslyApprovedQty : PX.Data.BQL.BqlDecimal.Field<previouslyApprovedQty> { }
		public new abstract class previouslyApprovedAmount : PX.Data.BQL.BqlDecimal.Field<previouslyApprovedAmount> { }
		public new abstract class committedCOQty : PX.Data.BQL.BqlDecimal.Field<committedCOQty> { }
		public new abstract class committedCOAmount : PX.Data.BQL.BqlDecimal.Field<committedCOAmount> { }
		public new abstract class otherDraftRevisedAmount : PX.Data.BQL.BqlDecimal.Field<otherDraftRevisedAmount> { }
		public new abstract class totalPotentialRevisedAmount : PX.Data.BQL.BqlDecimal.Field<totalPotentialRevisedAmount> { }
	}
}
