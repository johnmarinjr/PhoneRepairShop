using System;
using PX.Data;
using PX.Objects.SO;
using System.Collections.Generic;
using PX.Commerce.Core;
using PX.Data.WorkflowAPI;
using PX.Data.EP;

namespace PX.Commerce.Objects
{
	[Serializable]
	public sealed class BCSOLineExt : PXCacheExtension<SOLine>
	{
		public static bool IsActive() { return CommerceFeaturesHelper.CommerceEdition; }

		#region OrderType
		public abstract class orderType : PX.Data.BQL.BqlString.Field<orderType> { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXFieldDescription]
		public String OrderType { get; set; }

		#endregion
		#region OrderNbr
		public abstract class orderNbr : PX.Data.BQL.BqlString.Field<orderNbr> { }
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		[PXFieldDescription]
		public String OrderNbr { get; set; }
		#endregion

		#region ExternalRef
		public abstract class externalRef : PX.Data.BQL.BqlString.Field<externalRef> { }
		[PXDBString(64, IsUnicode = true)]
		[PXUIField(DisplayName = "External Ref.")]
		public string ExternalRef { get; set; }
		#endregion

		#region AssociatedOrderLineNbr
		public abstract class associatedOrderLineNbr : PX.Data.BQL.BqlInt.Field<associatedOrderLineNbr> { }
		[PXDBInt]
		[PXUIField(DisplayName = "Associated Order Line Nbr.", Visible = false, IsReadOnly = true)]
		public int? AssociatedOrderLineNbr { get; set; }
		#endregion

		#region GiftMessage
		public abstract class giftMessage : PX.Data.BQL.BqlString.Field<giftMessage> { }
		[PXDBString(200, IsUnicode = true)]
		[PXUIField(DisplayName = "Gift Message", Visible = false)]
		public string GiftMessage { get; set; }
		#endregion
	}
}