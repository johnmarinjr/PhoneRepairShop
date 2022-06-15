using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.GL;

namespace PX.Objects.CA.Alias
{
	[PXHidden]
	public partial class CashAccount : IBqlTable
	{
		#region Keys
		public class PK : PrimaryKeyOf<CashAccount>.By<cashAccountID>
		{
			public static CashAccount Find(PXGraph graph, int? cashAccountID) => FindBy(graph, cashAccountID);
		}

		public static class FK
		{
			public class Branch : GL.Branch.PK.ForeignKeyOf<CashAccount>.By<branchID> { }
		}

		#endregion
		#region CashAccountID
		public abstract class cashAccountID : PX.Data.BQL.BqlInt.Field<cashAccountID> { }

		[PXDBIdentity]
		[PXUIField(Enabled = false)]
		[PXReferentialIntegrityCheck]
		public virtual int? CashAccountID
		{
			get;
			set;
		}
		#endregion
		#region CashAccountCD
		public abstract class cashAccountCD : PX.Data.BQL.BqlString.Field<cashAccountCD> { }

		[CashAccountRaw(IsKey = true, Visibility = PXUIVisibility.SelectorVisible)]
		[PXDefault]
		public virtual string CashAccountCD
		{
			get;
			set;
		}
		#endregion
		#region BranchID
		public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }

		[Branch(Visibility = PXUIVisibility.SelectorVisible)]
		public virtual int? BranchID
		{
			get;
			set;
		}
		#endregion
	}

	[PXHidden]
	public class CashAccountAlias : CashAccount
	{
		#region CashAccountID
		public abstract new class cashAccountID : PX.Data.BQL.BqlInt.Field<cashAccountID> { }
		#endregion
	}
}
