using System;
using System.Collections.Generic;
using System.Linq;
using PX.Data;
using PX.Data.BQL;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.SM;
using PX.Objects.Common.Attributes;
using PX.Objects.IN;

namespace PX.Objects.SO
{
	[PXTable]
	[PXCacheName(Messages.SOPickingJob, PXDacType.Details)]
	public class SOPickingJob : WMSJob
	{
		#region Keys
		public new class PK : PrimaryKeyOf<SOPickingJob>.By<jobID>
		{
			public static SOPickingJob Find(PXGraph graph, int? jobID) => FindBy(graph, jobID);
		}

		public new static class FK
		{
			public class Job : WMSJob.PK.ForeignKeyOf<SOPickingJob>.By<jobID> { }
			public class PreferredAssignee : Users.PK.ForeignKeyOf<SOPickingJob>.By<preferredAssigneeID> { }
			public class ActualAssignee : Users.PK.ForeignKeyOf<SOPickingJob>.By<actualAssigneeID> { }
			public class Worksheet : SOPickingWorksheet.PK.ForeignKeyOf<SOPickingJob>.By<worksheetNbr> { }
			public class Picker : SOPicker.PK.ForeignKeyOf<SOPickingJob>.By<worksheetNbr, pickerNbr> { }
		}
		#endregion

		#region JobID
		public new abstract class jobID : BqlInt.Field<jobID> { }
		#endregion
		#region WorksheetNbr
		[PXDBString(15, IsUnicode = true, InputMask = ">CCCCCCCCCCCCCCC")]
		[PXUIField(DisplayName = "Worksheet Nbr.", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		[PXDBDefault(typeof(SOPickingWorksheet.worksheetNbr))]
		[PXParent(typeof(FK.Worksheet))]
		public virtual String WorksheetNbr { get; set; }
		public abstract class worksheetNbr : BqlString.Field<worksheetNbr> { }
		#endregion
		#region PickerNbr
		[PXDBInt]
		[PXDBDefault(typeof(SOPicker.pickerNbr))]
		[PXUIField(DisplayName = "Picker Nbr.", Enabled = false)]
		[PXParent(typeof(FK.Picker))]
		public virtual Int32? PickerNbr { get; set; }
		public abstract class pickerNbr : BqlInt.Field<pickerNbr> { }
		#endregion
		#region JobType
		[PXDBString(4, IsFixed = true, IsUnicode = false)]
		[PXDefault(jobType.Picking)]
		public override string JobType { get; set; }
		public new abstract class jobType : BqlString.Field<jobType>
		{
			public const string Picking = "PICK";
		}
		#endregion
		#region Status
		[PXDBString(3, IsFixed = true, IsUnicode = false)]
		[status.List]
		[PXDefault(status.OnHold)]
		[PXUIField(DisplayName = "Status", Enabled = false)]
		public override string Status { get; set; }
		public new abstract class status : BqlString.Field<status>
		{
			public const string OnHold = WMSJob.status.OnHold;
			public const string Enqueued = WMSJob.status.Enqueued;
			public const string Assigned = WMSJob.status.Assigned;
			public const string Picking = "PNG";
			public const string Reenqueued = WMSJob.status.Reenqueued;
			public const string Picked = "PED";
			public const string Completed = WMSJob.status.Completed;

			[PX.Common.PXLocalizable]
			public static class DisplayNames
			{
				public const string OnHold = WMSJob.status.DisplayNames.OnHold;
				public const string Enqueued = WMSJob.status.DisplayNames.Enqueued;
				public const string Assigned = WMSJob.status.DisplayNames.Assigned;
				public const string Picking = "Being Picked";
				public const string Reenqueued = WMSJob.status.DisplayNames.Reenqueued;
				public const string Picked = "Picked";
				public const string Completed = WMSJob.status.DisplayNames.Completed;
			}

			public class onHold : WMSJob.status.onHold { }
			public class enqueued : WMSJob.status.enqueued { }
			public class assigned : WMSJob.status.assigned { }
			public class picking : BqlString.Constant<picking> { public picking() : base(Picking) { } }
			public class reenqueued : WMSJob.status.reenqueued { }
			public class picked : BqlString.Constant<picked> { public picked() : base(Picked) { } }
			public class completed : WMSJob.status.completed { }

			public new class ListAttribute : WMSJob.status.ListAttribute
			{
				public ListAttribute() : base(GetPairs().ToArray()) { }
				protected ListAttribute(params Tuple<string, string>[] valuesToLabels) : base(valuesToLabels) { }

				protected new static IEnumerable<Tuple<string, string>> GetPairs()
				{
					foreach (var pair in WMSJob.status.ListAttribute.GetPairs())
					{
						if (pair.Item1 == Reenqueued)
						{
							yield return Pair(Picking, DisplayNames.Picking);
							yield return pair;
							yield return Pair(Picked, DisplayNames.Picked);
						}
						else
							yield return pair;
					}
				}
			}
		}
		#endregion
		#region Priority
		public new abstract class priority : BqlInt.Field<priority> { }
		#endregion
		#region PreferredAssigneeID
		[PXDBGuid]
		[PXUIField(DisplayName = "Assigned Picker")]
		[PXSelector(typeof(Search<Users.pKID, Where<Users.isHidden.IsEqual<False>>>), SubstituteKey = typeof(Users.username))]
		[PXUIEnabled(typeof(actualAssigneeID.IsNull))]
		[PXForeignReference(typeof(FK.PreferredAssignee))]
		public override Guid? PreferredAssigneeID { get; set; }
		public new abstract class preferredAssigneeID : BqlGuid.Field<preferredAssigneeID> { }
		#endregion
		#region ActualAssigneeID
		[PXDBGuid]
		[PXUIField(DisplayName = "Actual Picker", Enabled = false)]
		[PXSelector(typeof(Search<Users.pKID, Where<Users.isHidden.IsEqual<False>>>), SubstituteKey = typeof(Users.username))]
		[PXForeignReference(typeof(FK.ActualAssignee))]
		public override Guid? ActualAssigneeID { get; set; }
		public new abstract class actualAssigneeID : BqlGuid.Field<actualAssigneeID> { }
		#endregion
		#region EnqueuedAt
		public new abstract class enqueuedAt : BqlDateTime.Field<enqueuedAt> { }
		#endregion
		#region PickingStartedAt
		[DBConditionalModifiedDateTime(typeof(status), status.Picking, KeepValue = true)]
		[PXFormula(typeof(Null.When<status.IsIn<status.reenqueued, status.assigned>>.Else<pickingStartedAt>))]
		[PXUIField(DisplayName = "Picking Started", Enabled = false)]
		public virtual DateTime? PickingStartedAt { get; set; }
		public abstract class pickingStartedAt : BqlDateTime.Field<pickingStartedAt> { }
		#endregion
		#region ReenqueuedAt
		public new abstract class reenqueuedAt : BqlDateTime.Field<reenqueuedAt> { }
		#endregion
		#region PickedAt
		[DBConditionalModifiedDateTime(typeof(status), status.Picked, KeepValue = true)]
		[PXFormula(typeof(Null.When<status.IsEqual<status.picking>>.Else<pickedAt>))]
		[PXUIField(DisplayName = "Picking Finished", Enabled = false)]
		public virtual DateTime? PickedAt { get; set; }
		public abstract class pickedAt : BqlDateTime.Field<pickedAt> { }
		#endregion
		#region CompletedAt
		public new abstract class completedAt : BqlDateTime.Field<completedAt> { }
		#endregion
		#region PickListNbr
		[PXString]
		[PXUIField(DisplayName = "Pick List Nbr.", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		[PXFormula(typeof(IsNull<Parent<SOPickingWorksheet.singleShipmentNbr>, Parent<SOPicker.pickListNbr>>))]
		public virtual String PickListNbr { get; set; }
		public abstract class pickListNbr : BqlString.Field<pickListNbr> { }
		#endregion
		#region AutomaticShipmentConfirmation
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Automatic Shipment Confirmation")]
		[PXUIEnabled(typeof(
			status.IsNotIn<status.picked, status.completed>.
			And<Where<Parent<SOPickingWorksheet.worksheetType>, Equal<SOPickingWorksheet.worksheetType.single>>>))]
		public virtual bool? AutomaticShipmentConfirmation { get; set; }
		public abstract class automaticShipmentConfirmation : BqlBool.Field<automaticShipmentConfirmation> { }
		#endregion
	}
}