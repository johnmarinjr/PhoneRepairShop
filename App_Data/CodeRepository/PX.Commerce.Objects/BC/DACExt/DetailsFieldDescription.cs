using PX.Commerce.Core;
using PX.CS;
using PX.Data;
using PX.Data.EP;
using PX.Objects.AR;
using PX.Objects.SO;
using System;

namespace PX.Commerce.Objects
{
	public sealed class BCAttributeExt : PXCacheExtension<CSAttribute>
	{
		public static bool IsActive() { return CommerceFeaturesHelper.CommerceEdition; }
		#region Attribute ID
		public abstract class attributeID : PX.Data.BQL.BqlString.Field<attributeID> { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXFieldDescription]
		public string AttributeID { get; set; }
		#endregion		
	}

	public sealed class BCAttributeValueExt : PXCacheExtension<CSAttributeDetail>
	{
		public static bool IsActive() { return CommerceFeaturesHelper.CommerceEdition; }
		#region Value ID
		public abstract class valueID : PX.Data.BQL.BqlString.Field<valueID> { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXFieldDescription]
		public string ValueID { get; set; }
		#endregion
	}
}
