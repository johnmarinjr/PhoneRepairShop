using System;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.IN;
using PX.Objects.SO;
using PX.Objects.TX;
using PX.Objects.GL;
using PX.Objects.CS;
using System.Collections.Generic;
using PX.Commerce.Core;
using PX.Objects.CA;
using PX.Objects.CR;

namespace PX.Commerce.Objects
{
	[Serializable]
	[PXNonInstantiatedExtension]
	public sealed class BCBindingCommerce : PXCacheExtension<BCBinding>
	{
		public static bool IsActive() { return true; }

		#region Keys
		public static class FK
		{
			public class BindingsBranch : Branch.PK.ForeignKeyOf<BCBinding>.By<branchID> { }
		}
		#endregion

		#region BranchID
		// Acuminator disable once PX1030 PXDefaultIncorrectUse [Need to check if it empty while Persisting]
		[PXMergeAttributes(Method = MergeMethod.Replace)]
		[Branch(typeof(AccessInfo.branchID))]
		[PXDefault(typeof(AccessInfo.branchID))]
		public int? BranchID { get; set; }
		public abstract class branchID : PX.Data.BQL.BqlInt.Field<branchID> { }
		#endregion
	}
}
