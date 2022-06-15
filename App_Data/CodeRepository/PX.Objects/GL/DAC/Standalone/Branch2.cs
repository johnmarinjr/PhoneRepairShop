using PX.Data;

namespace PX.Objects.GL.Standalone
{
	[PXHidden]
	public partial class Branch2 : Branch
	{
		public new abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
		public new abstract class bAccountID : PX.Data.BQL.BqlInt.Field<bAccountID> { }
	}
}
