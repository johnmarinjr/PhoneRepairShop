using PX.Data;
using PX.Objects.SO;
using System;

namespace PX.Commerce.Objects
{
	[Serializable]
	public sealed class SOOrderTypeExt : PXCacheExtension<SOOrderType>
	{
		#region EncryptAndPseudonymizePII
		public abstract class encryptAndPseudonymizePII : PX.Data.BQL.BqlBool.Field<encryptAndPseudonymizePII> { }
		[PXDBBool]
		[PXUIVisible(typeof(Where<Current<SOOrderType.behavior>, Equal<SOBehavior.iN>, Or<Current<SOOrderType.behavior>,Equal<SOBehavior.sO>>>))]
		[PXUIField(DisplayName = "Protect Personal Data", Visible = false)]
		public  bool? EncryptAndPseudonymizePII { get; set; }
		#endregion

	}
}
