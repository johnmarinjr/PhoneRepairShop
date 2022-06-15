using System;
using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CM;

namespace PX.Objects.GL.Standalone
{
	[Serializable]
	[PXCacheName(Messages.Ledger)]
	public partial class LedgerAlias : PX.Objects.GL.Ledger
	{
		public new abstract class ledgerID : PX.Data.BQL.BqlInt.Field<ledgerID> { }
		public new abstract class ledgerCD : PX.Data.BQL.BqlInt.Field<ledgerCD> { }
		public new abstract class organizationID : PX.Data.BQL.BqlInt.Field<organizationID> { }
		public new abstract class baseCuryID : PX.Data.BQL.BqlInt.Field<baseCuryID> { }
		public new abstract class descr : PX.Data.BQL.BqlInt.Field<descr> { }
		public new abstract class balanceType : PX.Data.BQL.BqlInt.Field<balanceType> { }
	}
}
