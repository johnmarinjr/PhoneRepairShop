using System;
using PX.Data;

namespace PX.Objects.CA
{
	[PXHidden]
	public class CAMatchProcess : IBqlTable
	{
		#region Keys
		public class PK : Data.ReferentialIntegrity.Attributes.PrimaryKeyOf<CAMatchProcess>.By<cashAccountID>
		{
			public static CAMatchProcess Find(PXGraph graph, int? cashAccount) => FindBy(graph, cashAccount);
		}

		public static class FK
		{
			public class CashAcccount : CA.CashAccount.PK.ForeignKeyOf<CABankTran>.By<cashAccountID> { }
		}

		#endregion
		#region CashAccountID
		public abstract class cashAccountID : PX.Data.BQL.BqlInt.Field<cashAccountID> { }

		[PXDBInt(IsKey = true)]
		[PXDefault]
		public virtual int? CashAccountID
		{
			get;
			set;
		}
		#endregion
		#region ProcessUID
		public abstract class processUID : PX.Data.BQL.BqlGuid.Field<processUID> { }

		[PXDBGuid]
		[PXDefault]
		public virtual Guid? ProcessUID
		{
			get;
			set;
		}
		#endregion
		#region OperationStartDate
		public abstract class operationStartDate : PX.Data.BQL.BqlDateTime.Field<operationStartDate> { }

		[PXDBDate(PreserveTime = true)]
		public virtual DateTime? OperationStartDate
		{
			get;
			set;
		}
		#endregion
		#region StartedByID
		public abstract class startedByID : PX.Data.BQL.BqlGuid.Field<startedByID> { }

		[PXDBGuid]
		public virtual Guid? StartedByID
		{
			get;
			set;
		}
		#endregion
	}
}
