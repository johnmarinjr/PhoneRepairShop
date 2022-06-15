using PX.Data;
using PX.Data.EP;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CM.Extensions;
using PX.Objects.IN;
using System;

namespace PX.Objects.PM
{
	/// <summary>
	/// Contains a markup of a <see cref="PMChangeRequest">change request</see>.
	/// The records of this type are created and edited through the <strong>Markups</strong> tab
	/// of the Change Requests (PM308500) form (which corresponds to the <see cref="ChangeRequestEntry"/> graph).
	/// </summary>
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	[PXCacheName(Messages.Markup)]
	[Serializable]
	public class PMChangeRequestMarkup : PX.Data.IBqlTable
	{
		#region RefNbr
		public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr> { }

		/// <summary>
		/// The reference number of the parent <see cref="PMChangeRequest">change request</see>.
		/// </summary>
		[PXDBString(PMChangeRequest.refNbr.Length, IsUnicode = true, IsKey = true, InputMask = "")]
		[PXDBDefault(typeof(PMChangeRequest.refNbr))]
		[PXUIField(DisplayName = "Reference Nbr.", Enabled = false)]
		[PXParent(typeof(Select<PMChangeRequest, Where<PMChangeRequest.refNbr, Equal<Current<PMChangeRequestMarkup.refNbr>>>>))]
		public virtual String RefNbr
		{
			get;
			set;
		}
		#endregion
		#region LineNbr
		public abstract class lineNbr : PX.Data.BQL.BqlInt.Field<lineNbr> { }

		/// <summary>
		/// The original sequence number of the line.
		/// </summary>
		/// <remarks>
		/// The sequence of line numbers that belongs to a single document can include gaps.
		/// </remarks>
		[PXDBInt(IsKey = true)]
		[PXLineNbr(typeof(PMChangeRequest.markupLineCntr))]
		[PXUIField(DisplayName = "Line Nbr.", Visible = false)]
		public virtual Int32? LineNbr
		{
			get;
			set;
		}
		#endregion
		#region Type
		public abstract class type : PX.Data.BQL.BqlString.Field<type> { }
		
		/// <summary>
		/// The type of the document markup.
		/// </summary>
		/// <value>
		/// The field can have one of the following values:
		/// <c>"P"</c>: %,
		/// <c>"F"</c>: Flat Fee,
		/// <c>"C"</c>: Cumulative %
		/// </value>
		[PXUIField(DisplayName = "Type")]
		[PXDefault(PMMarkupLineType.Percentage)]
		[PMMarkupLineType.List]
		[PXDBString(1)]
		public virtual string Type
		{
			get;
			set;
		}
		#endregion
		#region Description
		public abstract class description : PX.Data.BQL.BqlString.Field<description> { }

		/// <summary>
		/// The description of the markup.
		/// </summary>
		[PXDBString(256, IsUnicode = true)]
		[PXUIField(DisplayName = "Description")]
		[PXFieldDescription]
		public virtual String Description
		{
			get;
			set;
		}
		#endregion
		#region Value
		public abstract class value : PX.Data.BQL.BqlDecimal.Field<value> { }

		/// <summary>
		/// The percentage or amount of the markup, depending on the markup type.
		/// </summary>
		[PXDBQuantity]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Value")]
		public virtual Decimal? Value
		{
			get;
			set;
			
		}
		#endregion
		#region Amount
		public abstract class amount : PX.Data.BQL.BqlDecimal.Field<amount> { }

		/// <summary>
		/// The amount to which the markup is applied.
		/// </summary>
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Amount Subject to Markup", Enabled = false)]
		public virtual Decimal? Amount
		{
			get;
			set;

		}
		#endregion
		#region MarkupAmount
		public abstract class markupAmount : PX.Data.BQL.BqlDecimal.Field<markupAmount> { }

		/// <summary>
		/// The markup amount.
		/// </summary>
		[PXFormula(null,
			typeof(SumCalc<PMChangeRequest.markupTotal>))]
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Markup Amount", Enabled = false)]
		public virtual Decimal? MarkupAmount
		{
			get;
			set;

		}
		#endregion		
		#region TaskID
		public abstract class taskID : PX.Data.BQL.BqlInt.Field<taskID> { }

		/// <summary>
		/// The identifier of the <see cref="PMTask">task</see> associated with the markup.
		/// </summary>
		/// <value>
		/// The value of this field corresponds to the value of the <see cref="PMTask.TaskID" /> field.
		/// </value>
		[ProjectTask(typeof(PMChangeRequest.projectID), AlwaysEnabled = true, AllowNull = true)]
		[PXForeignReference(typeof(Field<taskID>.IsRelatedTo<PMTask.taskID>))]
		public virtual Int32? TaskID
		{
			get;
			set;
		}
		#endregion
		#region AccountGroupID
		public abstract class accountGroupID : PX.Data.BQL.BqlInt.Field<accountGroupID> { }

		/// <summary>
		/// The identifier of the <see cref="PMAccountGroup">expense account group</see> associated with the markup.
		/// </summary>
		/// <value>
		/// The value of this field corresponds to the value of the <see cref="PMAccountGroup.GroupID" /> field.
		/// </value>
		[PXRestrictor(typeof(Where<PMAccountGroup.isActive, Equal<True>>), PM.Messages.InactiveAccountGroup, typeof(PMAccountGroup.groupCD))]
		[AccountGroup(typeof(Where<PMAccountGroup.type, Equal<PX.Objects.GL.AccountType.income>>))]
		[PXForeignReference(typeof(Field<accountGroupID>.IsRelatedTo<PMAccountGroup.groupID>))]
		public virtual Int32? AccountGroupID
		{
			get;
			set;
		}
		#endregion
		#region CostCodeID
		public abstract class costCodeID : PX.Data.BQL.BqlInt.Field<costCodeID> { }

		/// <summary>
		/// The identifier of the <see cref="PMCostCode">cost code</see> associated with the markup.
		/// </summary>
		[CostCode(null, typeof(taskID), PX.Objects.GL.AccountType.Income, typeof(accountGroupID), AllowNullValue = true)]
		public virtual Int32? CostCodeID
		{
			get;
			set;
		}
		#endregion
		#region InventoryID
		public abstract class inventoryID : PX.Data.BQL.BqlInt.Field<inventoryID> { }

		/// <summary>
		/// The identifier of the <see cref="InventoryItem">inventory item</see> associated with the markup.
		/// </summary>
		/// <value>
		/// The value of this field corresponds to the value of the <see cref="InventoryItem.InventoryID"/> field.
		/// </value>
		[PXDBInt]
		[PXUIField(DisplayName = "Inventory ID")]
		[PMInventorySelector]
		public virtual Int32? InventoryID
		{
			get;
			set;
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
}
