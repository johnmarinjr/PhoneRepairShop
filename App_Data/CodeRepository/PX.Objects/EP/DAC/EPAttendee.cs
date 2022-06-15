using System;
using System.Diagnostics;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.Objects.CR;

namespace PX.Objects.EP
{
	/// <summary>
	/// Represents an attendee of the event.
	/// </summary>
	/// <remarks>
	/// This is a child entity for the <see cref="CR.CRActivity"/> of the <b>Event</b> type
	/// (<see cref="CR.CRActivity.ClassID"/> is equal to <see cref="CRActivityClass.Event"/>).
	/// </remarks>
	[Serializable]
	[DebuggerDisplay("EventNoteID = {EventNoteID}, ContactID = {ContactID}")]
	[PXCacheName(Messages.Attendee)]
	public class EPAttendee : IBqlTable
	{
		#region Keys

		/// <summary>
		/// Primary Key.
		/// </summary>
		public class PK : PrimaryKeyOf<EPAttendee>.By<eventNoteID, attendeeID>
		{
			public static EPAttendee Find(PXGraph graph, Guid? eventNoteID, Guid? attendeeID)
				=> FindBy(graph, eventNoteID, attendeeID);
		}

		/// <summary>
		/// Foreign Keys.
		/// </summary>
		public static class FK
		{
			/// <summary>
			/// Event.
			/// </summary>
			public class Activity : CR.CRActivity.PK.ForeignKeyOf<EPAttendee>.By<eventNoteID> { }

			/// <summary>
			/// Contact.
			/// </summary>
			public class Contact : CR.Contact.PK.ForeignKeyOf<EPAttendee>.By<contactID> { }
		}
		#endregion

		#region EventNoteID
		public abstract class eventNoteID : PX.Data.BQL.BqlGuid.Field<eventNoteID> { }

		/// <summary>
		/// The identifier of the parent <see cref="CR.CRActivity"/>.
		/// The field is included in <see cref="FK.Activity"/>.
		/// </summary>
		/// <value>
		/// Corresponds to the value of the <see cref="CR.CRActivity.NoteID"/> field.
		/// </value>
		[PXDBGuid(IsKey = true)]
		[PXDBDefault(typeof(CRActivity.noteID))]
		public virtual Guid? EventNoteID { get; set; }
		#endregion

		#region AttendeeID
		public abstract class attendeeID : PX.Data.BQL.BqlGuid.Field<attendeeID> { }

		/// <summary>
		/// The unique identifier of the attendee.
		/// </summary>
		[PXDBGuid(withDefaulting: true, IsKey = true)]
		public virtual Guid? AttendeeID { get; set; }
		#endregion

		#region Email
		public abstract class email : PX.Data.BQL.BqlString.Field<email> { }

		/// <summary>
		/// The email address of the attendee.
		/// </summary>
		[PXDBEmail]
		[PXUIField(DisplayName = "Email")]
		public virtual string Email { get; set; }
		#endregion

		#region Comment
		public abstract class comment : PX.Data.BQL.BqlString.Field<comment> { }

		/// <summary>
		/// The comment of the event owner for the attendee.
		/// </summary>
		[PXDBString(255, IsUnicode = true)]
		[PXUIField(DisplayName = "Comment")]
		public virtual string Comment { get; set; }
		#endregion


		#region ContactID
		public abstract class contactID : PX.Data.BQL.BqlInt.Field<contactID> { }

		/// <summary>
		/// The identifier of the related <see cref="Contact"/>.
		/// The field is included in <see cref="FK.Contact"/>.
		/// </summary>
		/// <value>
		/// Corresponds to the value of the <see cref="Contact.ContactID"/> field.
		/// Can be <see langword="null"/>.
		/// </value>
		/// <remarks>
		/// The related contact's type (<see cref="Contact.ContactType"/>) can have one of the following values:
		/// <see cref="ContactTypesAttribute.Person"/>,
		/// <see cref="ContactTypesAttribute.Lead"/>,
		/// <see cref="ContactTypesAttribute.Employee"/>.
		/// </remarks>
		[PXUIField(DisplayName = "Contact")]
		[ContactRaw(
			contactTypes: new[]
			{
				typeof(ContactTypesAttribute.person),
				typeof(ContactTypesAttribute.employee),
				typeof(ContactTypesAttribute.lead),
			},
			fieldList: new[]
			{
				typeof(Contact.displayName),
				typeof(Contact.contactType),
				typeof(Contact.fullName),
				typeof(Contact.salutation),
				typeof(Contact.eMail),
			})]
		public virtual int? ContactID { get; set; }
		#endregion

		#region Invitation
		public abstract class invitation : PX.Data.BQL.BqlInt.Field<invitation> { }

		/// <summary>
		/// The invitation status of the attendee.
		/// </summary>
		/// <value>
		/// The field can have one of the values described in <see cref="PXInvitationStatusAttribute"/>.
		/// The default value is <see cref="PXInvitationStatusAttribute.NOTINVITED"/>.
		/// </value>
		[PXDBInt]
		[PXUIField(DisplayName = "Invitation", Enabled = false)]
		[PXDefault(PXInvitationStatusAttribute.NOTINVITED)]
		[PXInvitationStatus]
		public virtual int? Invitation { get; set; }
		#endregion

		#region IsOptional
		public abstract class isOptional : PX.Data.BQL.BqlBool.Field<isOptional> { }

		/// <summary>
		/// Specifies (if set to <see langword="true"/>) that the attendee is optional for the event.
		/// </summary>
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Optional")]
		public virtual bool? IsOptional { get; set; }
		#endregion

		#region IsOwner
		public abstract class isOwner : PX.Data.BQL.BqlBool.Field<isOwner> { }

		/// <summary>
		/// Specifies (if set to <see langword="true" />) that this attendee is a system attendee
		/// that corresponds to the event owner (<see cref="CR.CRActivity.OwnerID"/>).
		/// </summary>
		/// <remarks>
		/// It also means that <see cref="ContactID"/> equals to <see cref="PX.Objects.CR.CRActivity.OwnerID"/>
		/// and <see cref="Invitation"/> always equals to <see cref="PXInvitationStatusAttribute.ACCEPTED"/>.
		/// This attendee is exluded from all actions in <see cref="PX.Objects.EP.Graphs.EPEventMaint.Extensions.EPEventMaint_AttendeeExt"/>,
		/// such as <see cref="PX.Objects.EP.Graphs.EPEventMaint.Extensions.EPEventMaint_AttendeeExt.SendInvitations"/>.
		/// </remarks>
		[PXDBBool]
		[PXDefault(false)]
		[PXUIField(DisplayName = "Is Owner", Visible = false)]
		public virtual bool? IsOwner { get; set; }
		#endregion

		#region CreatedByID
		public abstract class createdByID : PX.Data.BQL.BqlGuid.Field<createdByID> { } 

		[PXDBCreatedByID]
		public virtual Guid? CreatedByID { get; set; }
		#endregion

		#region CreatedByScreenID
		public abstract class createdByScreenID : PX.Data.BQL.BqlString.Field<createdByScreenID> { }

		[PXDBCreatedByScreenID]
		public virtual string CreatedByScreenID { get; set; }
		#endregion

		#region CreatedDateTime
		public abstract class createdDateTime : PX.Data.BQL.BqlDateTime.Field<createdDateTime> { }

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
	}
}
