using PX.Data;
using PX.Data.EP;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.AR;
using PX.Objects.CM.Extensions;
using PX.Objects.CS;
using PX.TM;
using System;

namespace PX.Objects.PM
{
	/// <summary>Contains the main properties of a change order. The records of this type are created and edited through the Change Orders (PM308000) form (which corresponds to
	/// the <see cref="ChangeOrderEntry" /> graph).</summary>
	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	[PXCacheName(Messages.ChangeOrder)]
	[PXPrimaryGraph(typeof(ChangeOrderEntry))]
	[Serializable]
	[PXEMailSource]
	public class PMChangeOrder : PX.Data.IBqlTable, IAssign
	{
		#region Keys
		/// <summary>
		/// Primary Key
		/// </summary>
		/// <exclude />
		public class PK : PrimaryKeyOf<PMChangeOrder>.By<PMChangeOrder.refNbr>
		{
			public static PMChangeOrder Find(PXGraph graph, string refNbr) => FindBy(graph, refNbr);
		}

		/// <summary>
		/// Foreign Keys
		/// </summary>
		/// <exclude />
		public static class FK
		{
			/// <summary>
			/// Project
			/// </summary>
			/// <exclude />
			public class Project : PMProject.PK.ForeignKeyOf<PMChangeOrder>.By<projectID> { }

			/// <summary>
			/// Change Order Class
			/// </summary>
			/// <exclude />
			public class ChangeOrderClass : PMChangeOrderClass.PK.ForeignKeyOf<PMChangeOrder>.By<classID> { }

			/// <summary>
			/// Customer
			/// </summary>
			/// <exclude />
			public class Customer : AR.Customer.PK.ForeignKeyOf<PMChangeOrder>.By<customerID> { }


		}
		#endregion

		public const string FieldClass = "CHANGEORDER";
		#region RefNbr
		public abstract class refNbr : PX.Data.BQL.BqlString.Field<refNbr>
		{
			public const int Length = 15;
		}

		/// <summary>
		/// The reference number of the change order.
		/// </summary>
		/// <value>The number is generated from the <see cref="Numbering">numbering sequence</see>, which is specified on the <see cref="PMSetup">Projects Preferences</see> (PM101000) form.</value>
		[PXDBString(refNbr.Length, IsUnicode = true, IsKey = true, InputMask = "")]
		[PXDefault()]
		[PXUIField(DisplayName = "Reference Nbr.", Visibility = PXUIVisibility.SelectorVisible)]
		[PXSelector(typeof(PMChangeOrder.refNbr), DescriptionField = typeof(PMChangeOrder.description))]
		[AutoNumber(typeof(Search<PMSetup.changeOrderNumbering>), typeof(AccessInfo.businessDate))]
		public virtual String RefNbr
		{
			get;
			set;
		}
		#endregion
		#region ProjectNbr
		public abstract class projectNbr : PX.Data.BQL.BqlString.Field<projectNbr>
		{
			public const int Length = 15;
		}
		/// <summary>The change number.</summary>
		[PXDBString(projectNbr.Length, IsUnicode = true)]
		[PXUIField(DisplayName = "Revenue Change Nbr.", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual String ProjectNbr
		{
			get;
			set;
		}
		#endregion
		#region ClassID
		public abstract class classID : PX.Data.BQL.BqlString.Field<classID> { }

		/// <summary>The identifier of the GL <see cref="PMChangeOrderClass">change order class</see> that provides default settings for the change order.</summary>
		/// <value>
		/// The value of this field corresponds to the value of the <see cref="PMChangeOrderClass.ClassID" /> field.
		/// </value>
		[PXForeignReference(typeof(Field<classID>.IsRelatedTo<PMChangeOrderClass.classID>))]
		[PXDBString(PMChangeOrderClass.classID.Length, IsUnicode = true, InputMask = "")]
		[PXDefault(typeof(Search<PMSetup.defaultChangeOrderClassID>))]
		[PXUIField(DisplayName = "Class", Visibility = PXUIVisibility.SelectorVisible)]
		[PXSelector(typeof(Search<PMChangeOrderClass.classID, Where<PMChangeOrderClass.isActive, Equal<True>>>), DescriptionField = typeof(PMChangeOrderClass.description))]
		public virtual String ClassID
		{
			get;
			set;
		}
		#endregion
		#region Description
		public abstract class description : PX.Data.BQL.BqlString.Field<description> { }
		protected String _Description;
		/// <summary>The description of the change order.</summary>
		[PXDBString(Common.Constants.TranDescLength, IsUnicode = true)]
		[PXDefault()]
		[PXUIField(DisplayName = "Description", Visibility = PXUIVisibility.SelectorVisible)]
		[PXFieldDescription]
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
		#region Text
		public abstract class text : PX.Data.BQL.BqlString.Field<text> { }
		protected String _Text;

		/// <summary>
		/// A detailed description of the change order.
		/// </summary>
		[PXDBText(IsUnicode = true)]
		[PXUIField(DisplayName = "Details")]
		public virtual String Text
		{
			get
			{
				return this._Text;
			}
			set
			{
				this._Text = value;
				_plainText = null;
			}
		}
		#endregion
		#region DescriptionAsPlainText
		public abstract class descriptionAsPlainText : PX.Data.BQL.BqlString.Field<descriptionAsPlainText> { }

		/// <summary>
		/// A detailed description of the change order as plain text.
		/// </summary>
		private string _plainText = null;
		[PXString(IsUnicode = true)]
		[PXUIField(Visible = false)]
		public virtual String DescriptionAsPlainText
		{
			get
			{
				return _plainText ?? (_plainText = PX.Data.Search.SearchService.Html2PlainText(this.Text));
			}
		}
		#endregion
		#region Status
		public abstract class status : PX.Data.BQL.BqlString.Field<status> { }
		protected String _Status;

		/// <summary>
		/// The status of the change order.
		/// </summary>
		/// <value>
		/// The field can have one of the following values:
		/// <c>"H"</c>: On Hold,
		/// <c>"A"</c>: Pending Approval,
		/// <c>"O"</c>: Open,
		/// <c>"C"</c>: Closed,
		/// <c>"R"</c>: Rejected
		/// </value>
		[PXDBString(1, IsFixed = true)]
		[ChangeOrderStatus.List()]
		[PXDefault(ChangeOrderStatus.OnHold)]
		[PXUIField(DisplayName = "Status", Required = true, Enabled = false, Visibility = PXUIVisibility.SelectorVisible)]
		public virtual String Status
		{
			get
			{
				return this._Status;
			}
			set
			{
				this._Status = value;
			}
		}
		#endregion
		#region Hold
		public abstract class hold : PX.Data.BQL.BqlBool.Field<hold> { }
		protected Boolean? _Hold;

		/// <summary>
		/// Specifies (if set to <see langword="true" />) that the document is on hold.
		/// </summary>
		[PXDBBool]
		[PXUIField(DisplayName = "Hold")]
		[PXDefault(true)]
		public virtual Boolean? Hold
		{
			get
			{
				return this._Hold;
			}
			set
			{
				this._Hold = value;
			}
		}
		#endregion
		#region Approved
		public abstract class approved : PX.Data.BQL.BqlBool.Field<approved> { }
		protected Boolean? _Approved;

		/// <summary>
		/// Specifies (if set to <see langword="true" />) that the document has been approved.
		/// </summary>
		[PXDBBool]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual Boolean? Approved
		{
			get
			{
				return this._Approved;
			}
			set
			{
				this._Approved = value;
			}
		}
		#endregion
		#region Rejected
		public abstract class rejected : PX.Data.BQL.BqlBool.Field<rejected> { }
		protected bool? _Rejected = false;

		/// <summary>
		/// Specifies (if set to <see langword="true" />) that the document has been rejected.
		/// </summary>
		[PXDBBool]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		public bool? Rejected
		{
			get
			{
				return _Rejected;
			}
			set
			{
				_Rejected = value;
			}
		}
		#endregion
		#region ProjectID
		public abstract class projectID : PX.Data.BQL.BqlInt.Field<projectID> { }
		protected Int32? _ProjectID;

		/// <summary>The identifier of the <see cref="PMProject">project</see> associated with the change order.</summary>
		/// <value>
		/// The value of this field corresponds to the value of the <see cref="PMProject.contractID" /> field.
		/// </value>
		[PXDefault]
		[PXForeignReference(typeof(Field<projectID>.IsRelatedTo<PMProject.contractID>))]
		[Project(typeof(Where<PMProject.changeOrderWorkflow, Equal<True>, And<PMProject.baseType, Equal<CT.CTPRType.project>>>), Visibility = PXUIVisibility.SelectorVisible)]
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
		#region CustomerID
		public abstract class customerID : PX.Data.BQL.BqlInt.Field<customerID> { }
		protected Int32? _CustomerID;

		/// <summary>
		/// The identifier of the customer associated with the project.
		/// </summary>
		/// <value>
		/// The value of this field corresponds to the value of the <see cref="CR.BAccount.BAccountID"/> field.
		/// </value>
		[PXFormula(typeof(Selector<projectID, PMProject.customerID>))]
		[Customer(DescriptionField = typeof(Customer.acctName), Enabled = false)]
		public virtual Int32? CustomerID
		{
			get
			{
				return this._CustomerID;
			}
			set
			{
				this._CustomerID = value;
			}
		}
		#endregion
		#region Date
		public abstract class date : PX.Data.BQL.BqlDateTime.Field<date> { }

		/// <summary>
		/// The date on which the changes made with the change order should be recorded in the project balances.
		/// </summary>
		/// <value>Defaults to the current <see cref="AccessInfo.BusinessDate">business date</see>.</value>
		[PXDBDate()]
		[PXDefault(typeof(AccessInfo.businessDate))]
		[PXUIField(DisplayName = "Change Date", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual DateTime? Date
		{
			get;
			set;
		}
		#endregion
		#region CompletionDate
		public abstract class completionDate : PX.Data.BQL.BqlDateTime.Field<completionDate> { }

		/// <summary>
		/// The date that has been communicated to the customer as the approval date of the agreed-upon changes.
		/// </summary>
		/// <value>Defaults to the current <see cref="AccessInfo.BusinessDate">business date</see>.</value>
		[PXDBDate()]
		[PXDefault(typeof(AccessInfo.businessDate))]
		[PXUIField(DisplayName = "Approval Date", Visibility = PXUIVisibility.SelectorVisible)]
		public virtual DateTime? CompletionDate
		{
			get;
			set;
		}
		#endregion
		#region ExtRefNbr
		public abstract class extRefNbr : PX.Data.BQL.BqlString.Field<extRefNbr> { }
		protected String _ExtRefNbr;

		/// <summary>The external reference number (such as an identifier required by the customer or a number from an external system integrated with Acumatica ERP) entered
		/// manually.</summary>
		[PXDBString(30, IsUnicode = true)]
		[PXUIField(DisplayName = "External Reference Nbr.")]
		public virtual String ExtRefNbr
		{
			get
			{
				return this._ExtRefNbr;
			}
			set
			{
				this._ExtRefNbr = value;
			}
		}
		#endregion
		#region OrigRefNbr
		public abstract class origRefNbr : PX.Data.BQL.BqlString.Field<origRefNbr> { }

		/// <summary>
		/// The <see cref="RefNbr">reference number</see> of the original change order
		/// whose changes the currently selected change order reverses.
		/// </summary>
		[PXDBString(refNbr.Length, IsUnicode = true, InputMask = "")]
		[PXUIField(DisplayName = "Orig. CO Ref. Nbr.")]
		public virtual String OrigRefNbr
		{
			get;
			set;
		}
		#endregion
		#region ReverseStatus
		public abstract class reverseStatus : PX.Data.BQL.BqlString.Field<reverseStatus> { }

		/// <summary>
		/// The reverse status of the change order.
		/// </summary>
		/// <value>
		/// The field can have one of the following values:
		/// <c>"N"</c>: None,
		/// <c>"X"</c>: Reversed,
		/// <c>"R"</c>: Reversing
		/// </value>
		[PXDBString(1, IsFixed = true)]
		[ChangeOrderReverseStatus.List()]
		[PXDefault(ChangeOrderReverseStatus.None)]
		[PXUIField(DisplayName = "Reverse Status", Enabled = false)]
		public virtual String ReverseStatus
		{
			get;
			set;
		}
		#endregion

		#region CostTotal
		public abstract class costTotal : PX.Data.BQL.BqlDecimal.Field<costTotal> { }

		/// <summary>
		/// The total <see cref="PMChangeOrderCostBudget.Amount">amount</see> of the
		/// <see cref="PMChangeOrderCostBudget">cost budget lines</see> of the document.
		/// </summary>
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Cost Budget Change Total")]
		public virtual Decimal? CostTotal
		{
			get;
			set;
		}
		#endregion
		#region RevenueTotal
		public abstract class revenueTotal : PX.Data.BQL.BqlDecimal.Field<revenueTotal> { }

		/// <summary>
		/// The total <see cref="PMChangeOrderRevenueBudget.Amount">amount</see> of the
		/// <see cref="PMChangeOrderRevenueBudget">revenue budget lines</see> of the document.
		/// </summary>
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Revenue Budget Change Total")]
		public virtual Decimal? RevenueTotal
		{
			get;
			set;
		}
		#endregion
		#region CommitmentTotal
		public abstract class commitmentTotal : PX.Data.BQL.BqlDecimal.Field<commitmentTotal> { }

		/// <summary>The total <see cref="PMChangeOrderLine.AmountInProjectCury">amount in project currency</see> of the <see cref="PMChangeOrderLine">commitments lines</see> of the document.</summary>
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Commitment Change Total")]
		public virtual Decimal? CommitmentTotal
		{
			get;
			set;
		}
		#endregion
		#region GrossMarginAmount
		public abstract class grossMarginAmount : PX.Data.BQL.BqlDecimal.Field<grossMarginAmount> { }

		/// <summary>
		/// The difference between the <see cref="RevenueTotal">Revenue Budget Change Total</see>
		/// and the <see cref="CostTotal">Cost Budget Change Total</see> values.
		/// </summary>
		[PXBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Gross Margin Amount")]
		public virtual Decimal? GrossMarginAmount
		{
			[PXDependsOnFields(typeof(revenueTotal), typeof(costTotal))]
			get
			{
				return RevenueTotal - CostTotal;
			}

		}
		#endregion
		#region GrossMarginPct
		public abstract class grossMarginPct : PX.Data.BQL.BqlDecimal.Field<grossMarginPct> { }

		/// <summary>The gross margin percent.</summary>
		[PXBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Gross Margin %")]
		public virtual Decimal? GrossMarginPct
		{
			[PXDependsOnFields(typeof(revenueTotal), typeof(costTotal))]
			get
			{
				if (RevenueTotal != 0)
				{
					return 100 * (RevenueTotal - CostTotal) / RevenueTotal;
				}
				else
					return 0;
			}
		}
		#endregion


		#region ChangeRequestCostTotal
		public abstract class changeRequestCostTotal : PX.Data.BQL.BqlDecimal.Field<changeRequestCostTotal> { }

		/// <summary>
		/// The <see cref="PMChangeRequest.CostTotal">cost total</see> of all the change requests linked to the change order.
		/// </summary>
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Change Request Cost Total", Enabled = false, FieldClass = nameof(CS.FeaturesSet.ChangeRequest))]
		public virtual Decimal? ChangeRequestCostTotal
		{
			get;
			set;
		}
		#endregion
		#region ChangeRequestLineTotal
		public abstract class changeRequestLineTotal : PX.Data.BQL.BqlDecimal.Field<changeRequestLineTotal> { }

		/// <summary>
		/// The <see cref="PMChangeRequest.LineTotal">line total</see> of all the change requests linked to the change order.
		/// </summary>
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Change Request Line Total", Enabled = false, FieldClass = nameof(CS.FeaturesSet.ChangeRequest))]
		public virtual Decimal? ChangeRequestLineTotal
		{
			get;
			set;
		}
		#endregion
		#region ChangeRequestMarkupTotal
		public abstract class changeRequestMarkupTotal : PX.Data.BQL.BqlDecimal.Field<changeRequestMarkupTotal> { }

		/// <summary>
		/// The <see cref="PMChangeRequest.MarkupTotal">markup total</see> of all the change requests linked to the change order.
		/// </summary>
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Change Request Markup Total", Enabled = false, FieldClass = nameof(CS.FeaturesSet.ChangeRequest))]
		public virtual Decimal? ChangeRequestMarkupTotal
		{
			get;
			set;
		}
		#endregion
		#region ChangeRequestPriceTotal
		public abstract class changeRequestPriceTotal : PX.Data.BQL.BqlDecimal.Field<changeRequestPriceTotal> { }

		/// <summary>
		/// The <see cref="PMChangeRequest.PriceTotal">price total</see> of all the change requests linked to the change order.
		/// </summary>
		[PXDBBaseCury]
		[PXDefault(TypeCode.Decimal, "0.0")]
		[PXUIField(DisplayName = "Change Request Price Total", Enabled = false, FieldClass = nameof(CS.FeaturesSet.ChangeRequest))]
		public virtual Decimal? ChangeRequestPriceTotal
		{
			get;
			set;
		}
		#endregion


		#region WorkgroupID
		public abstract class workgroupID : PX.Data.BQL.BqlInt.Field<workgroupID> { }
		protected int? _WorkgroupID;

		/// <summary>The workgroup that is responsible for the document.</summary>
		/// <value>
		/// Corresponds to the <see cref="PX.TM.EPCompanyTree.WorkGroupID">EPCompanyTree.WorkGroupID</see> field.
		/// </value>
		[PXDBInt]
		[PXDefault(typeof(Customer.workgroupID), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXCompanyTreeSelector]
		[PXUIField(DisplayName = "Workgroup", Visibility = PXUIVisibility.Visible)]
		public virtual int? WorkgroupID
		{
			get
			{
				return this._WorkgroupID;
			}
			set
			{
				this._WorkgroupID = value;
			}
		}
		#endregion
		#region OwnerID
		public abstract class ownerID : PX.Data.BQL.BqlInt.Field<ownerID> { }
		protected int? _OwnerID;

		/// <summary>The <see cref="Contact">contact</see> responsible for the document.</summary>
		/// <value>
		/// Corresponds to the <see cref="Contact.ContactID" /> field.
		/// </value>
		[PXDefault(typeof(Customer.ownerID), PersistingCheck = PXPersistingCheck.Nothing)]
		[Owner(typeof(PMChangeOrder.workgroupID))]
		public virtual int? OwnerID
		{
			get
			{
				return this._OwnerID;
			}
			set
			{
				this._OwnerID = value;
			}
		}
		#endregion
		#region LineCntr
		public abstract class lineCntr : PX.Data.BQL.BqlInt.Field<lineCntr> { }
		protected Int32? _LineCntr;

		/// <summary>A counter of the document lines, which is used internally to assign <see cref="PMChangeOrderLine.LineNbr">numbers</see> to newly created lines. We do not recommend
		/// that you rely on this field to determine the exact number of lines because it might not reflect this number under various conditions.</summary>
		[PXDBInt()]
		[PXDefault(0)]
		public virtual Int32? LineCntr
		{
			get
			{
				return this._LineCntr;
			}
			set
			{
				this._LineCntr = value;
			}
		}
		#endregion
		#region DelayDays
		public abstract class delayDays : PX.Data.BQL.BqlInt.Field<delayDays> { }

		/// <summary>
		/// A positive or negative number of days that represents the delay of the contract.
		/// </summary>
		[PXDBInt()]
		[PXUIField(DisplayName = "Contract Time Change, Days")]
		public virtual Int32? DelayDays
		{
			get;
			set;
		}
		#endregion
		#region Released
		public abstract class released : PX.Data.BQL.BqlBool.Field<released> { }
		protected Boolean? _Released;

		/// <summary>
		/// Specifies (if set to <see langword="true" />) that the document has been released.
		/// </summary>
		[PXDBBool()]
		[PXUIField(DisplayName = "Released")]
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

		#region IsCostVisible
		public abstract class isCostVisible : PX.Data.BQL.BqlBool.Field<isCostVisible> { }

		/// <summary>Specifies (if set to <see langword="true"></see>) that the <strong>Cost Budget</strong> tab is visible in the change order.</summary>
		[PXBool()]
		[PXUIField(DisplayName = "Visible Cost", Enabled = false)]
		[PXUnboundDefault(true)]
		public virtual Boolean? IsCostVisible
		{
			get;
			set;
		}
		#endregion
		#region IsRevenueVisible
		public abstract class isRevenueVisible : PX.Data.BQL.BqlBool.Field<isRevenueVisible> { }

		/// <summary>Specifies (if set to <see langword="true"></see>) that the <strong>Revenue <span>Budget</span></strong> tab is visible in the change order.</summary>
		[PXBool()]
		[PXUIField(DisplayName = "Visible Revenue", Enabled = false)]
		[PXUnboundDefault(true)]
		public virtual Boolean? IsRevenueVisible
		{
			get;
			set;
		}
		#endregion
		#region IsDetailsVisible
		public abstract class isDetailsVisible : PX.Data.BQL.BqlBool.Field<isDetailsVisible> { }

		/// <summary>Specifies (if set to <see langword="true"></see>) that the <strong>Commitments</strong> tab is visible in the change order.</summary>
		[PXBool()]
		[PXUIField(DisplayName = "Visible Details", Enabled = false)]
		[PXUnboundDefault(true)]
		public virtual Boolean? IsDetailsVisible
		{
			get;
			set;
		}
		#endregion
		#region IsChangeRequestVisible
		public abstract class isChangeRequestVisible : PX.Data.BQL.BqlBool.Field<isChangeRequestVisible> { }

		/// <summary>Specifies (if set to <see langword="true"></see>) that the <strong>Change Requests</strong> tab is visible in the change order.</summary>
		[PXBool()]
		[PXUIField(DisplayName = "2-Tier Change Management", Enabled = false)]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual Boolean? IsChangeRequestVisible
		{
			get;
			set;
		}
		#endregion

		#region FormCaptionDescription
		[PXString]
		[PXFormula(typeof(Selector<projectID, PMProject.description>))]
		public string FormCaptionDescription { get; set; }
		#endregion

		#region Attributes
		public abstract class attributes : CR.BqlAttributes.Field<attributes> { }

		/// <summary>
		/// A service field, which is necessary for the <see cref="CSAnswers">dynamically 
		/// added attributes</see> defined at the <see cref="PMChangeOrderClass">change order 
		/// class</see> level to function correctly.
		/// </summary>
		[CR.CRAttributesField(typeof(PMChangeOrder.classID))]
		public virtual string[] Attributes { get; set; }
		#endregion

		#region System Columns
		#region NoteID
		public abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }
		protected Guid? _NoteID;
		[ChangeOrderSearchable]
		[PXNote(DescriptionField = typeof(PMChangeOrderClass.description))]
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

	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public static class ChangeOrderStatus
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute()
				: base(
				new string[] { OnHold, PendingApproval, Open, Closed, Rejected },
				new string[] { Messages.OnHold, Messages.PendingApproval, Messages.Open, Messages.Closed, Messages.Rejected })
			{; }
		}
		public const string OnHold = "H";
		public const string PendingApproval = "A";
		public const string Open = "O";
		public const string Closed = "C";
		public const string Rejected = "R";

		public class onHold : PX.Data.BQL.BqlString.Constant<onHold>
		{
			public onHold() : base(OnHold) {; }
		}

		public class pendingApproval : PX.Data.BQL.BqlString.Constant<pendingApproval>
		{
			public pendingApproval() : base(PendingApproval) {; }
		}

		public class open : PX.Data.BQL.BqlString.Constant<open>
		{
			public open() : base(Open) {; }
		}

		public class closed : PX.Data.BQL.BqlString.Constant<closed>
		{
			public closed() : base(Closed) {; }
		}

		public class rejected : PX.Data.BQL.BqlString.Constant<rejected>
		{
			public rejected() : base(Rejected) {; }
		}
	}

	[System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
	public static class ChangeOrderReverseStatus
	{
		public class ListAttribute : PXStringListAttribute
		{
			public ListAttribute()
				: base(
				new string[] { None, Reversed, Reversal },
				new string[] { Messages.None, Messages.Reversed, Messages.Reversing })
			{; }
		}
		public const string None = "N";
		public const string Reversed = "X";
		public const string Reversal = "R";

		public class reversed : PX.Data.BQL.BqlString.Constant<reversed>
		{
			public reversed() : base(Reversed) {; }
		}

		public class reversal : PX.Data.BQL.BqlString.Constant<reversal>
		{
			public reversal() : base(Reversal) {; }
		}

		public class none : PX.Data.BQL.BqlString.Constant<none>
		{
			public none() : base(None) {; }
		}
	}
}
