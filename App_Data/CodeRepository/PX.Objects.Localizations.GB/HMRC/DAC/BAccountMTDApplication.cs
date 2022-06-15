using PX.Data;
using PX.Data.BQL;
using System;

namespace PX.Objects.Localizations.GB.HMRC.DAC
{
	[Serializable()]
	[PXCacheName("MTD External Application")]
	public partial class BAccountMTDApplication : IBqlTable
	{

		#region ApplicationID
		[PXDBInt]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "MTD External Application")]
		public int? ApplicationID { get; set; }
		public abstract class applicationID : PX.Data.BQL.BqlInt.Field<applicationID> { }
		#endregion

		#region BAccountID
		[PXDBInt(IsKey = true)]
		[PXDefault(PersistingCheck = PXPersistingCheck.Null)]		
		public int? BAccountID { get; set; }
		public abstract class bAccountID : BqlInt.Field<bAccountID> { }
		#endregion
	}
}
