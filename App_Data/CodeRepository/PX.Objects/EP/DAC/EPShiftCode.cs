using PX.Data;
using PX.Data.BQL;
using PX.Data.ReferentialIntegrity.Attributes;
using System;
using System.Diagnostics;

namespace PX.Objects.EP
{
	[Serializable]
	[PXCacheName(Messages.EPShiftCode)]
	[DebuggerDisplay("{GetType().Name,nq}: {ShiftCD,nq}")]
	public class EPShiftCode : IBqlTable
	{
		public class PK : PrimaryKeyOf<EPShiftCode>.By<shiftID>
		{
			public static EPShiftCode Find(PXGraph graph, int? shiftID) => FindBy(graph, shiftID);
		}

		public class UK : PrimaryKeyOf<EPShiftCode>.By<shiftCD>
		{
			public static EPShiftCode Find(PXGraph graph, string shiftCD) => FindBy(graph, shiftCD);
		}

		#region ShiftID
		public abstract class shiftID : BqlInt.Field<shiftID> { }
		[PXDBIdentity]
		public int? ShiftID { get; set; }
		#endregion

		#region ShiftCD
		[PXDBString(IsKey = true, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
		[PXUIField(DisplayName = "Code")]
		[PXDefault]
		[PXUIEnabled(typeof(Where<shiftCD.IsNull>))]
		public virtual string ShiftCD { get; set; }
		public abstract class shiftCD : BqlString.Field<shiftCD> { }
		#endregion

		#region IsActive
		[PXDBBool]
		[PXUIField(DisplayName = "Active")]
		[PXDefault(true)]
		public virtual bool? IsActive { get; set; }
		public abstract class isActive : BqlBool.Field<isActive> { }
		#endregion

		#region Description
		[PXDBString(IsUnicode = true)]
		[PXUIField(DisplayName = "Description")]
		[PXDefault("")]
		public virtual string Description { get; set; }
		public abstract class description : BqlString.Field<description> { }
		#endregion

		#region IsManufacturingShift
		[PXDBBool]
		[PXDefault(false)]
		public virtual bool? IsManufacturingShift { get; set; }
		public abstract class isManufacturingShift : BqlBool.Field<isManufacturingShift> { }
		#endregion

		#region NoteID
		public abstract class noteID : BqlGuid.Field<noteID> { }
		[PXNote]
		public virtual Guid? NoteID { get; set; }
		#endregion

		#region System Columns
		#region CreatedByID
		[PXDBCreatedByID()]
		public virtual Guid? CreatedByID { get; set; }
		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }
		#endregion

		#region CreatedByScreenID
		[PXDBCreatedByScreenID()]
		public virtual string CreatedByScreenID { get; set; }
		public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }
		#endregion

		#region CreatedDateTime
		[PXDBCreatedDateTime()]
		public virtual DateTime? CreatedDateTime { get; set; }
		public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }
		#endregion

		#region LastModifiedByID
		[PXDBLastModifiedByID()]
		public virtual Guid? LastModifiedByID { get; set; }
		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }
		#endregion

		#region LastModifiedByScreenID
		[PXDBLastModifiedByScreenID()]
		public virtual string LastModifiedByScreenID { get; set; }
		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }
		#endregion

		#region LastModifiedDateTime
		[PXDBLastModifiedDateTime()]
		public virtual DateTime? LastModifiedDateTime { get; set; }
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }
		#endregion
		#endregion System Columns
	}
}
