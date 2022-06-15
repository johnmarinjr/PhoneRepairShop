using System;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.SM;
using PX.Objects.Common.Attributes;
using PX.Objects.Common.Extensions;

namespace PX.Objects.IN
{
	[PXCacheName(Messages.WMSJob, PXDacType.Details)]
	public class WMSJob : IBqlTable, IPXSelectable
	{
		#region Keys
		public class PK : PrimaryKeyOf<WMSJob>.By<jobID>
		{
			public static WMSJob Find(PXGraph graph, int? jobID) => FindBy(graph, jobID);
		}

		public static class FK
		{
			public class PreferredAssignee : Users.PK.ForeignKeyOf<WMSJob>.By<preferredAssigneeID> { }
			public class ActualAssignee : Users.PK.ForeignKeyOf<WMSJob>.By<actualAssigneeID> { }
		}
		#endregion

		#region JobID
		[PXDBIdentity(IsKey = true)]
		public virtual int? JobID { get; set; }
		public abstract class jobID : BqlInt.Field<jobID> { }
		#endregion
		#region JobType
		[PXDBString(4, IsFixed = true, IsUnicode = false)]
		[PXDefault]
		public virtual string JobType { get; set; }
		public abstract class jobType : BqlString.Field<jobType> { }
		#endregion
		#region Status
		[PXDBString(3, IsFixed = true, IsUnicode = false)]
		[status.List]
		[PXDefault(status.OnHold)]
		[PXUIField(DisplayName = "Status", Enabled = false)]
		public virtual string Status { get; set; }
		public abstract class status : BqlString.Field<status>
		{
			public const string OnHold = "HLD";
			public const string Enqueued = "ENQ";
			public const string Assigned = "ASG";
			public const string Reenqueued = "RNQ";
			public const string Completed = "CMP";

			[PX.Common.PXLocalizable]
			public static class DisplayNames
			{
				public const string OnHold = "On Hold";
				public const string Enqueued = "Added to Queue";
				public const string Assigned = "Assigned";
				public const string Reenqueued = "Returned to Queue";
				public const string Completed = "Completed";
			}

			public class onHold : BqlString.Constant<onHold> { public onHold() : base(OnHold) { } }
			public class enqueued : BqlString.Constant<enqueued> { public enqueued() : base(Enqueued) { } }
			public class assigned : BqlString.Constant<assigned> { public assigned() : base(Assigned) { } }
			public class reenqueued : BqlString.Constant<reenqueued> { public reenqueued() : base(Reenqueued) { } }
			public class completed : BqlString.Constant<completed> { public completed() : base(Completed) { } }

			public class ListAttribute : PXStringListAttribute, IPXRowUpdatedSubscriber
			{
				public ListAttribute() : base(GetPairs().ToArray()) { }
				protected ListAttribute(params Tuple<string, string>[] valuesToLabels) : base(valuesToLabels) { }

				protected static IEnumerable<Tuple<string, string>> GetPairs()
				{
					yield return Pair(OnHold, DisplayNames.OnHold);
					yield return Pair(Enqueued, DisplayNames.Enqueued);
					yield return Pair(Assigned, DisplayNames.Assigned);
					yield return Pair(Reenqueued, DisplayNames.Reenqueued);
					yield return Pair(Completed, DisplayNames.Completed);
				}

				void IPXRowUpdatedSubscriber.RowUpdated(PXCache cache, PXRowUpdatedEventArgs e)
				{
					if (!cache.ObjectsEqual<WMSJob.status, WMSJob.preferredAssigneeID>(e.OldRow, e.Row) && e.Row is WMSJob newJob)
					{
						if (newJob.PreferredAssigneeID != null && newJob.Status.IsIn(Enqueued, Reenqueued))
							cache.LiteUpdate(newJob, set => set.Set(j => j.Status, Assigned));
						else if (newJob.PreferredAssigneeID == null && newJob.Status == Assigned)
							cache.LiteUpdate(newJob, set => set.Set(j => j.Status, Reenqueued));
					}
				}
			}
		}
		#endregion
		#region Priority
		[PXDBInt]
		[priority.List]
		[PXDefault(priority.Medium)]
		[PXUIField(DisplayName = "Priority")]
		public virtual int? Priority { get; set; }
		public abstract class priority : BqlInt.Field<priority>
		{
			public const int Low = 1;
			public const int Medium = 2;
			public const int High = 3;
			public const int Urgent = 4;

			[PX.Common.PXLocalizable]
			public static class DisplayNames
			{
				public const string Low = "Low";
				public const string Medium = "Medium";
				public const string High = "High";
				public const string Urgent = "Urgent";
			}

			public class ListAttribute : PXIntListAttribute
			{
				public ListAttribute() : base
				(
					Pair(Urgent, DisplayNames.Urgent),
					Pair(High, DisplayNames.High),
					Pair(Medium, DisplayNames.Medium),
					Pair(Low, DisplayNames.Low)
				) { }
			}

			public class low : BqlInt.Constant<low> { public low() : base(Low) { } }
			public class medium : BqlInt.Constant<medium> { public medium() : base(Medium) { } }
			public class high : BqlInt.Constant<high> { public high() : base(High) { } }
			public class urgent : BqlInt.Constant<urgent> { public urgent() : base(Urgent) { } }
		}
		#endregion
		#region PreferredAssigneeID
		[PXDBGuid]
		[PXUIField(DisplayName = "Preferred Assignee")]
		[PXSelector(typeof(Search<Users.pKID, Where<Users.isHidden.IsEqual<False>>>), SubstituteKey = typeof(Users.username))]
		[PXUIEnabled(typeof(actualAssigneeID.IsNull))]
		[PXForeignReference(typeof(FK.PreferredAssignee))]
		public virtual Guid? PreferredAssigneeID { get; set; }
		public abstract class preferredAssigneeID : BqlGuid.Field<preferredAssigneeID> { }
		#endregion
		#region ActualAssigneeID
		[PXDBGuid]
		[PXUIField(DisplayName = "Actual Assignee", Enabled = false)]
		[PXSelector(typeof(Search<Users.pKID, Where<Users.isHidden.IsEqual<False>>>), SubstituteKey = typeof(Users.username))]
		[PXForeignReference(typeof(FK.ActualAssignee))]
		public virtual Guid? ActualAssigneeID { get; set; }
		public abstract class actualAssigneeID : BqlGuid.Field<actualAssigneeID> { }
		#endregion
		#region EnqueuedAt
		[DBConditionalModifiedDateTime(typeof(status), status.OnHold, InvertLogic = true)]
		[PXFormula(typeof(Null.When<status.IsEqual<status.onHold>>.Else<enqueuedAt>))]
		[PXUIField(DisplayName = "Added to Queue at", Enabled = false)]
		public virtual DateTime? EnqueuedAt { get; set; }
		public abstract class enqueuedAt : BqlDateTime.Field<enqueuedAt> { }
		#endregion
		#region ReenqueuedAt
		[DBConditionalModifiedDateTime(typeof(status), status.Reenqueued, KeepValue = true)]
		[PXFormula(typeof(Null.When<status.IsIn<status.onHold, status.assigned>>.Else<reenqueuedAt>))]
		[PXUIField(DisplayName = "Returned to Queue at", Enabled = false)]
		public virtual DateTime? ReenqueuedAt { get; set; }
		public abstract class reenqueuedAt : BqlDateTime.Field<reenqueuedAt> { }
		#endregion
		#region CompletedAt
		[DBConditionalModifiedDateTime(typeof(status), status.Completed)]
		[PXUIField(DisplayName = "Completed at", Enabled = false)]
		public virtual DateTime? CompletedAt { get; set; }
		public abstract class completedAt : BqlDateTime.Field<completedAt> { }
		#endregion
		#region IsAbandoned
		[PXInt] // overflow in 4085+ years
		[PXDBCalced(typeof(lastModifiedDateTime.Diff<Now>.Minutes), typeof(int), Persistent = false)]
		public virtual int? MinutesSinceLastModification { get; set; }
		public abstract class minutesSinceLastModification : BqlInt.Field<minutesSinceLastModification> { }
		#endregion

		#region NoteID
		[PXNote]
		public virtual Guid? NoteID { get; set; }
		public abstract class noteID : BqlGuid.Field<noteID> { }
		#endregion
		#region Audit Fields
		#region CreatedByID
		[PXDBCreatedByID]
		public virtual Guid? CreatedByID { get; set; }
		public abstract class createdByID : BqlGuid.Field<createdByID> { }
		#endregion
		#region CreatedByScreenID
		[PXDBCreatedByScreenID]
		public virtual String CreatedByScreenID { get; set; }
		public abstract class createdByScreenID : BqlString.Field<createdByScreenID> { }
		#endregion
		#region CreatedDateTime
		[PXDBCreatedDateTime]
		public virtual DateTime? CreatedDateTime { get; set; }
		public abstract class createdDateTime : BqlDateTime.Field<createdDateTime> { }
		#endregion
		#region LastModifiedByID
		[PXDBLastModifiedByID]
		public virtual Guid? LastModifiedByID { get; set; }
		public abstract class lastModifiedByID : BqlGuid.Field<lastModifiedByID> { }
		#endregion
		#region LastModifiedByScreenID
		[PXDBLastModifiedByScreenID]
		public virtual String LastModifiedByScreenID { get; set; }
		public abstract class lastModifiedByScreenID : BqlString.Field<lastModifiedByScreenID> { }
		#endregion
		#region LastModifiedDateTime
		[PXDBLastModifiedDateTime]
		public virtual DateTime? LastModifiedDateTime { get; set; }
		public abstract class lastModifiedDateTime : BqlDateTime.Field<lastModifiedDateTime> { }
		#endregion
		#region tstamp
		[PXDBTimestamp]
		public virtual Byte[] tstamp { get; set; }
		public abstract class Tstamp : BqlByteArray.Field<Tstamp> { }
		#endregion
		#endregion

		#region Selected
		[PXBool]
		[PXUnboundDefault(false)]
		[PXUIField(DisplayName = "Selected")]
		public virtual bool? Selected { get; set; }
		public abstract class selected : PX.Data.BQL.BqlBool.Field<selected> { }
		#endregion
	}
}