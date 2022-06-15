using PX.Data;

namespace PX.Objects.CA
{
	[PXHidden]
	public class CashAccountAlias : CashAccount
	{
		#region CashAccountID
		public abstract new class cashAccountID : PX.Data.BQL.BqlInt.Field<cashAccountID> { }
		#endregion
	}
}
