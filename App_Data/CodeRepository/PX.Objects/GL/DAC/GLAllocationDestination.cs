namespace PX.Objects.GL
{
	using System;
	using PX.Data;
	using PX.Data.ReferentialIntegrity.Attributes;
	using PX.Objects.GL.DAC;

	[System.SerializableAttribute()]
	[PXCacheName(Messages.GLAllocationDestination)]
	public partial class GLAllocationDestination : PX.Data.IBqlTable
    {
		#region Keys
		public class PK : PrimaryKeyOf<GLAllocationDestination>.By<gLAllocationID, lineID>
		{
			public static GLAllocationDestination Find(PXGraph graph, String gLAllocationID, Int32? lineID) => FindBy(graph, gLAllocationID, lineID);
		}
		public static class FK
		{
			public class Branch : GL.Branch.PK.ForeignKeyOf<GLAllocationDestination>.By<branchID> { }
			public class Allocation : GL.GLAllocation.PK.ForeignKeyOf<GLAllocationDestination>.By<gLAllocationID> { }
			public class Account : GL.Account.UK.ForeignKeyOf<GLAllocationDestination>.By<accountCD> { }
			public class Subaccount : GL.Sub.UK.ForeignKeyOf<GLAllocationDestination>.By<subCD> { }
			public class BaseBranch : GL.Branch.PK.ForeignKeyOf<GLAllocationDestination>.By<basisBranchID> { }
			public class BaseAccount : GL.Account.UK.ForeignKeyOf<GLAllocationDestination>.By<basisAccountCD> { }
			public class BaseSubaccount : GL.Sub.UK.ForeignKeyOf<GLAllocationDestination>.By<basisSubCD> { }
		}
		#endregion

		#region BranchID
		public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
        protected Int32? _BranchID;
		[PXDefault(typeof(Search<Branch.branchID, Where<Branch.branchID, Equal<Current<GLAllocation.branchID>>>>))]
		[Branch(null, typeof(Search2<Branch.branchID,
					 InnerJoin<Organization,
						On<Organization.organizationID, Equal<Branch.organizationID>>,
					 InnerJoin<OrganizationLedgerLink,
						  On<Branch.organizationID, Equal<OrganizationLedgerLink.organizationID>, And<OrganizationLedgerLink.ledgerID, Equal<Current<GLAllocation.allocLedgerID>>>>>>,
					 Where<Match<Current<AccessInfo.userName>>>>),
			useDefaulting: false)]
		public virtual Int32? BranchID
        {
            get
            {
                return this._BranchID;
            }
            set
            {
                this._BranchID = value;
            }
        }
        #endregion
		#region GLAllocationID
		public abstract class gLAllocationID : PX.Data.BQL.BqlString.Field<gLAllocationID> { }
		protected String _GLAllocationID;
		[PXDBString(15, IsUnicode = true, IsKey = true)]
		[PXDBDefault(typeof(GLAllocation.gLAllocationID))]
		[PXUIField(DisplayName = "Allocation ID", Visible = false)]
		[PXParent(typeof(Select<GLAllocation,Where<GLAllocation.gLAllocationID,Equal<Current<GLAllocationDestination.gLAllocationID>>>>))]
		public virtual String GLAllocationID
		{
			get
			{
				return this._GLAllocationID;
			}
			set
			{
				this._GLAllocationID = value;
			}
		}
		#endregion
		#region LineID
		public abstract class lineID : PX.Data.BQL.BqlInt.Field<lineID> { }
		protected Int32? _LineID;
		[PXDBIdentity(IsKey = true)]
		[PXUIField(DisplayName = "Line Nbr.", Visible =false)]
		public virtual Int32? LineID
		{
			get
			{
				return this._LineID;
			}
			set
			{
				this._LineID = value;
			}
		}
		#endregion
		#region AccountCD
		public abstract class accountCD : PX.Data.BQL.BqlString.Field<accountCD> { }
		protected String _AccountCD;
		[PXDefault(PersistingCheck = PXPersistingCheck.NullOrBlank)]
		[AccountCDWildcard(typeof(Search2<Account.accountCD, LeftJoin<GLSetup, On<GLSetup.ytdNetIncAccountID, Equal<Account.accountID>>,
							LeftJoin<Ledger, On<Ledger.ledgerID, Equal<Current<GLAllocation.allocLedgerID>>>>>,
						 Where2<Match<Current<AccessInfo.userName>>,
						And<Account.active, Equal<True>,
						And<Where<Account.curyID, IsNull, Or<Account.curyID, Equal<Ledger.baseCuryID>,
						And<GLSetup.ytdNetIncAccountID, IsNull>>>>>>>),
			DescriptionField = typeof(Account.description))]
		public virtual String AccountCD
		{
			get
			{
				return this._AccountCD;
			}
			set
			{
				this._AccountCD = value;
			}
		}
		#endregion
		#region SubCD
		public abstract class subCD : PX.Data.BQL.BqlString.Field<subCD> { }
		protected String _SubCD;
		[SubCDWildcard()]
		[PXDefault(PersistingCheck = PXPersistingCheck.NullOrBlank)]
		public virtual String SubCD
		{
			get
			{
				return this._SubCD;
			}
			set
			{
				this._SubCD = value;
			}
		}
		#endregion
		#region BasisBranchID
		public abstract class basisBranchID : PX.Data.BQL.BqlInt.Field<basisBranchID> { }
		protected Int32? _BasisBranchID;
		[Branch(null, typeof(Search2<Branch.branchID,
					 InnerJoin<Organization,
							On<Organization.organizationID, Equal<Branch.organizationID>>,
					 InnerJoin<OrganizationLedgerLink,
						  On<Branch.organizationID, Equal<OrganizationLedgerLink.organizationID>, And<OrganizationLedgerLink.ledgerID, Equal<Current<GLAllocation.basisLederID>>>>>>,
					 Where<Match<Current<AccessInfo.userName>>>>),
			useDefaulting: false, 
			DisplayName = "Base Branch", 
			PersistingCheck = PXPersistingCheck.Nothing)]
		public virtual Int32? BasisBranchID
		{
			get
			{
				return this._BasisBranchID;
			}
			set
			{
				this._BasisBranchID = value;
			}
		}
		#endregion
        #region BasisAccountCD
        public abstract class basisAccountCD : PX.Data.BQL.BqlString.Field<basisAccountCD> { }
        protected String _BasisAccountCD;
        [AccountCDWildcard(typeof(Search<Account.accountCD, Where<Account.active, Equal<True>>>), DescriptionField = typeof(Account.description), DisplayName="Base Account")]        
        public virtual String BasisAccountCD
        {
            get
            {
                return this._BasisAccountCD;
            }
            set
            {
                this._BasisAccountCD = value;
            }
        }
        #endregion
        #region BasisSubCD
        public abstract class basisSubCD : PX.Data.BQL.BqlString.Field<basisSubCD> { }
        protected String _BasisSubCD;
        [SubCDWildcard(DisplayName="Base Subaccount")]
        public virtual String BasisSubCD
        {
            get
            {
                return this._BasisSubCD;
            }
            set
            {
                this._BasisSubCD = value;
            }
        }
        #endregion
		#region Weight
		public abstract class weight : PX.Data.BQL.BqlDecimal.Field<weight> { }
		protected Decimal? _Weight;
		[PXDBDecimal(6)]
		[PXUIField(DisplayName = "Weight/Percent")]
		public virtual Decimal? Weight
		{
			get
			{
				return this._Weight;
			}
			set
			{
				this._Weight = value;
			}
		}
		#endregion
		#region CreatedByID
		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }
		protected Guid? _CreatedByID;
		[PXDBCreatedByID()]
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
		[PXDBCreatedDateTime()]
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
		[PXDBLastModifiedByID()]
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
		[PXDBLastModifiedDateTime()]
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
	}
}
