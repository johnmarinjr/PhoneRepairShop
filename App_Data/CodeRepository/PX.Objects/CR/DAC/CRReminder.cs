using System;
using PX.Data;
using PX.Data.EP;
using PX.SM;
using PX.TM;
using PX.Web.UI;
using PX.Data.ReferentialIntegrity.Attributes;

namespace PX.Objects.CR
{
	/// <summary>
	/// The reminder for the <b>task</b> or <b>event</b> (<see cref="CRActivity"/>).
	/// </summary>
	[Serializable]
	[PXCacheName(Messages.Reminder)]
	public class CRReminder : IBqlTable
	{
		#region Keys

		/// <summary>
		/// Primary Key
		/// </summary>
		public class PK : PrimaryKeyOf<CRReminder>.By<noteID>
		{
			public static CRReminder Find(PXGraph graph, Guid? noteID)
				=> FindBy(graph, noteID);
		}

		/// <summary>
		/// Foreign Keys.
		/// </summary>
		public static class FK
		{
			/// <summary>
			/// Owner of the reminder.
			/// </summary>
			public class Owner : CR.Contact.PK.ForeignKeyOf<CRReminder>.By<owner> { }

			/// <summary>
			/// Event or task related to the reminder.
			/// </summary>
			public class Activity : CR.CRActivity.PK.ForeignKeyOf<CRReminder>.By<refNoteID> { }
		}

		#endregion

		#region Selected
		public abstract class selected : PX.Data.BQL.BqlBool.Field<selected> { }

		[PXBool]
		[PXDefault(false, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIField(DisplayName = "Selected")]
		public virtual bool? Selected { get; set; }
		#endregion

		#region IsReminderOn
		public abstract class isReminderOn : PX.Data.BQL.BqlBool.Field<isReminderOn> { }

		/// <summary>
		/// Specifies (if set to <see langword="true"/>) that the reminder is enabled.
		/// </summary>
		/// <value>
		/// The value of this field is calculated by formula.
		/// </value>
		[PXBool]
		[PXFormula(typeof(
			Switch<
				Case<Where<reminderDate, IsNotNull>, True>
			, False>))]
		[PXUIField(DisplayName = "Reminder")]
		public virtual bool? IsReminderOn { get; set; }
		#endregion

		#region ReminderIcon
		public abstract class reminderIcon : PX.Data.BQL.BqlString.Field<reminderIcon>
		{
			public class reminder : PX.Data.BQL.BqlString.Constant<reminder>
			{
				public reminder() : base(Sprite.Control.GetFullUrl(Sprite.Control.Reminder)) { }
			}
		}

		/// <summary>
		/// The alias for the icon used by the reminder.
		/// </summary>
		/// <value>
		/// The value is a full path to the icon which is calculated by formula.
		/// This value is used by the related <see cref="CRActivity"/> to display the reminder icon in generic inquiries.
		/// </value>
		[PXUIField(DisplayName = "Reminder Icon", IsReadOnly = true)]
		[PXImage(HeaderImage = (Sprite.AliasControl + "@" + Sprite.Control.ReminderHead))]
		[PXFormula(typeof(Switch<Case<Where<reminderDate, IsNotNull>, CRReminder.reminderIcon.reminder>>))]
		public virtual String ReminderIcon { get; set; }
		#endregion

		#region NoteID
		public abstract class noteID : PX.Data.BQL.BqlGuid.Field<noteID> { }

		/// <summary>
		/// The unique identifier of the reminder.
		/// </summary>
		[PXSequentialNote(SuppressActivitiesCount = true, IsKey = true)]
		[PXTimeTag(typeof(noteID))]
		public virtual Guid? NoteID { get; set; }
		#endregion

		#region RefNoteID
		public abstract class refNoteID : PX.Data.BQL.BqlGuid.Field<refNoteID> { }

		/// <summary>
		/// The identifier of the related <see cref="CRActivity"/>.
		/// This field is included in <see cref="FK.Activity"/>.
		/// </summary>
		/// <value>
		/// Corresponds to the value of the <see cref="CRActivity.NoteID"/> field.
		/// </value>
		[PXDBGuid]
		[PXDBDefault(null, PersistingCheck = PXPersistingCheck.Nothing, DefaultForUpdate = false)]
		public virtual Guid? RefNoteID { get; set; }
		#endregion

		#region ReminderDate
		public abstract class reminderDate : PX.Data.BQL.BqlDateTime.Field<reminderDate> { }

		/// <summary>
		/// The date and time of the reminder.
		/// </summary>
		/// <value>
		/// The value of this field is stored in the Coordinated Universal Time (UTC)
		/// time zone and shown in the UI in the user's time zone.
		/// </value>
		[PXDBDateAndTime(InputMask = "g", PreserveTime = true, UseTimeZone = true)]
		//[PXRemindDate(typeof(isReminderOn), typeof(startDate), InputMask = "g", PreserveTime = true)]
		[PXUIField(DisplayName = "Remind at")]
		public virtual DateTime? ReminderDate { get; set; }
		#endregion

		#region RemindAt
		public abstract class remindAt : PX.Data.BQL.BqlString.Field<remindAt> { }

		/// <summary>
		/// The value that shows the relative time before the start of the reminder.
		/// Allows to select <see cref="ReminderDate"/> relatively to <see cref="CRActivity.StartDate"/>.
		/// </summary>
		/// <value>
		/// The field can have one of the values described in the <see cref="RemindAtListAttribute"/>.
		/// </value>
		[PXDBString]
		[PXUIField(DisplayName = "Remind At", Visible = false)]
		[RemindAtList]
		[PXUIVisible(typeof(Where<CRReminder.isReminderOn, Equal<True>>))]
		public virtual string RemindAt { get; set; }
		#endregion

		#region Owner
		public abstract class owner : PX.Data.BQL.BqlInt.Field<owner> { }

		/// <summary>
		/// The identifier of the <see cref="Contact">owner</see> of the reminder.
		/// The field is included in <see cref="FK.Owner"/>.
		/// </summary>
		/// <value>
		/// Corresponds to the value of the <see cref="Contact.ContactID"/> field.
		/// </value>
		[PXChildUpdatable(AutoRefresh = true)]
		//[PXOwnerSelector(typeof(groupID))]
		// cutted done
		[SubordinateOwner]
		public virtual int? Owner { get; set; }
		#endregion

		#region Dismiss
		public abstract class dismiss : PX.Data.BQL.BqlBool.Field<dismiss> { }

		/// <summary>
		/// Specifies (if set to <see langword="true"/>) that the reminder is dismissed.
		/// </summary>
		[PXDBBool]
		[PXDefault(false)]
		public virtual bool? Dismiss { get; set; }
		#endregion


		#region CreatedByID
		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { }

		[PXDBCreatedByID(DontOverrideValue = true)]
		[PXUIField(Enabled = false)]
		public virtual Guid? CreatedByID { get; set; }
		#endregion

		#region CreatedByScreenID
		public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }

		[PXDBCreatedByScreenID]
		public virtual string CreatedByScreenID { get; set; }
		#endregion

		#region CreatedDateTime
		public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }

		[PXUIField(DisplayName = "Created At", Enabled = false)]
		[PXDBCreatedDateTime]
		public virtual DateTime? CreatedDateTime { get; set; }
		#endregion

		#region LastModifiedByID
		public abstract class lastModifiedByID : PX.Data.BQL.BqlGuid.Field<lastModifiedByID> { }

		[PXDBLastModifiedByID]
		public virtual Guid? LastModifiedByID { get; set; }
		#endregion

		#region LastModifiedByScreenID
		public abstract class lastModifiedByScreenID : PX.Data.BQL.BqlString.Field<lastModifiedByScreenID> { }

		[PXDBLastModifiedByScreenID]
		public virtual string LastModifiedByScreenID { get; set; }
		#endregion

		#region LastModifiedDateTime
		public abstract class lastModifiedDateTime : PX.Data.BQL.BqlDateTime.Field<lastModifiedDateTime> { }

		[PXDBLastModifiedDateTime]
		public virtual DateTime? LastModifiedDateTime { get; set; }
		#endregion

		#region tstamp
		public abstract class Tstamp : PX.Data.BQL.BqlByteArray.Field<Tstamp> { }

		[PXDBTimestamp]
		public virtual byte[] tstamp { get; set; }
		#endregion
	}
}
