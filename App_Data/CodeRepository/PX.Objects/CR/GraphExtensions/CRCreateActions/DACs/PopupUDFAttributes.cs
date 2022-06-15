using System;
using PX.Data;
using PX.Data.MassProcess;

namespace PX.Objects.CR.Extensions.CRCreateActions
{
	/// <exclude/>
	[PXHidden]
	[Serializable]
	[PXBreakInheritance]
	public class PopupUDFAttributes : FieldValue
	{
		#region CacheName
		public new abstract class cacheName : PX.Data.BQL.BqlString.Field<cacheName> { }

		[PXString]
		public override string CacheName { get; set; }
		#endregion

		#region ScreenID
		public new abstract class screenID : PX.Data.BQL.BqlString.Field<screenID> { }

		[PXString(IsKey = true)]
		public virtual string ScreenID { get; set; }
		#endregion
	}
}