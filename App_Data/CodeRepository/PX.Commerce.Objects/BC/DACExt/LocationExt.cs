using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Objects.CR;
using PX.Data.EP;
using PX.Objects.IN;
using PX.SM;
using PX.Commerce.Core;

namespace PX.Commerce.Objects
{
	#region LocationExt
	public sealed class BCLocationExt : PXCacheExtension<Location>
	{
		public static bool IsActive() { return CommerceFeaturesHelper.CommerceEdition; }

		#region LocationCD
		public abstract class locationCD : PX.Data.BQL.BqlString.Field<locationCD> { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXFieldDescription]
		public String LocationCD { get; set; }
		#endregion
		#region Descr
		public abstract class descr : PX.Data.BQL.BqlString.Field<descr> { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXFieldDescription]
		public String Descr { get; set; }
		#endregion
	}
	#endregion
}