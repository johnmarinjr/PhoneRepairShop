using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Data.EP;
using PX.Export.Imc;
using PX.Objects.Common;
using PX.Objects.CR;
using PX.Objects.CR.Extensions;
using PX.SM;

namespace PX.Objects.EP.Graphs.EPEventMaint.Extensions
{
	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public class EPEventMaint_AttendeeExt : PXGraphExtension<PX.Objects.EP.EPEventMaint>
	{
		public enum NotificationTypes
		{
			Invitation,
			Reschedule,
			Cancel
		}

		#region Fields

		public static readonly string DoubleNewLine = Environment.NewLine + Environment.NewLine;

		#endregion

		#region Selects

		[PXViewDetailsButton(typeof(EPAttendee.contactID),
			typeof(SelectFrom<Contact>
				.Where<Contact.contactID.IsEqual<EPAttendee.contactID.FromCurrent>>),
			WindowMode = PXRedirectHelper.WindowMode.New)]
		public SelectFrom<EPAttendee>
			.LeftJoin<Contact>
				.On<Contact.contactID.IsEqual<EPAttendee.contactID>>
			.Where<EPAttendee.eventNoteID.IsEqual<CRActivity.noteID.FromCurrent>>
			.OrderBy<EPAttendee.isOwner.Desc>
			.View
			Attendees;

		[PXCopyPasteHiddenView]
		public SelectFrom<EPAttendee>
			.Where<
				EPAttendee.eventNoteID.IsEqual<CRActivity.noteID.FromCurrent>
				.And<EPAttendee.invitation.IsNotIn<PXInvitationStatusAttribute.notinvited, PXInvitationStatusAttribute.rejected>>
				.And<EPAttendee.isOwner.IsNotEqual<True>>
			>
			.View
			InvitedAttendees;

		[PXCopyPasteHiddenView]
		public SelectFrom<EPAttendee>
			.Where<
				EPAttendee.eventNoteID.IsEqual<CRActivity.noteID.FromCurrent>
				.And<EPAttendee.invitation.IsEqual<PXInvitationStatusAttribute.notinvited>>
				.And<EPAttendee.isOwner.IsNotEqual<True>>
			>
			.View
			NotInvitedAttendees;

		[PXHidden]
		[PXCopyPasteHiddenView]
		public SelectFrom<EPAttendee>
			.Where<
				EPAttendee.contactID.IsEqual<AccessInfo.contactID.FromCurrent>
				.And<EPAttendee.eventNoteID.IsEqual<CRActivity.noteID.FromCurrent>>
			>
			.View
			AttendeeForCurrentUser;

		[PXHidden]
		[PXCopyPasteHiddenView]
		public SelectFrom<EPAttendee>
			.Where<
				EPAttendee.eventNoteID.IsEqual<CRActivity.noteID.FromCurrent>
				.And<EPAttendee.isOwner.IsNotEqual<True>>
			>
			.View
			AttendeesExceptOwner;

		[PXHidden]
		public SelectFrom<EPAttendee>
			.Where<
				EPAttendee.eventNoteID.IsEqual<CRActivity.noteID.FromCurrent>
				.And<EPAttendee.isOwner.IsEqual<True>>
			>
			.View
			AttendeeForOwner;

		#region Asks views
		// only for Import Scenario ask
		[PXCopyPasteHiddenView]
		public SelectFrom<EPAttendee>.View
			SendInvitationCancellationsToAttendeesAnswer;

		// only for Import Scenario ask
		[PXCopyPasteHiddenView]
		public SelectFrom<EPAttendee>.View
			SendInvitationsToAttendeesAnswer;

		// only for Import Scenario ask
		[PXCopyPasteHiddenView]
		public SelectFrom<EPAttendee>.View
			SendInvitationsToOnlyNotInvitedAttendeesAnswer;

		// only for Import Scenario ask
		[PXCopyPasteHiddenView]
		public SelectFrom<EPAttendee>.View
			SendInvitationReschdulesToAttendeesAnswer;

		#endregion

		#endregion

		#region Ctors

		[InjectDependency]
		public ICurrentUserInformationProvider CurrentUserInformationProvider { get; private set; }

		#endregion

		#region Actions

		[PXOverride]
		public virtual IEnumerable cancelActivity(PXAdapter adapter, PXButtonDelegate del)
		{
			if (ConfirmToSendInvitationCancellationsToAttendees())
			{
				EnsureIsSavedWithoutSendingEmails();
				var graph = Base.CloneGraphState();
				var ext = graph.GetExtension<EPEventMaint_AttendeeExt>();
				var current = Base.Events.Current;
				PXLongOperation.StartOperation(Base, () =>
				{
					ext.SendInvitationCancellationsToAttendees();
					ext.SetAllPrimaryAnswers(WebDialogResult.No);
					graph.CancelRow(current);
				});
			}
			else
			{
				SetAllPrimaryAnswers(WebDialogResult.No);
				Base.CancelRow(Base.Events.Current);
			}
			return adapter.Get();
		}

		public PXAction<CRActivity> AcceptInvitation;
		[PXButton(Tooltip = AttendeeMessages.AcceptInvitationTooltip,
				IsLockedOnToolbar = true,
				Category = Messages.ManagementCategory)]
		[PXUIField(DisplayName = AttendeeMessages.AcceptInvitation, Visible = false, MapEnableRights = PXCacheRights.Select)]
		public virtual IEnumerable acceptInvitation(PXAdapter adapter)
		{
			AcceptParticipation(true);
			return adapter.Get();
		}

		public PXAction<CRActivity> RejectInvitation;
		[PXButton(Tooltip = AttendeeMessages.RejectInvitationTooltip,
				IsLockedOnToolbar = true,
				Category = Messages.ManagementCategory)]
		[PXUIField(DisplayName = AttendeeMessages.RejectInvitation, Visible = false, MapEnableRights = PXCacheRights.Select)]
		public virtual IEnumerable rejectInvitation(PXAdapter adapter)
		{
			AcceptParticipation(false);
			return adapter.Get();
		}

		protected virtual void AcceptParticipation(bool accept)
		{
			var invitation = accept ? PXInvitationStatusAttribute.ACCEPTED : PXInvitationStatusAttribute.REJECTED;
			foreach (var attendee in AttendeeForCurrentUser.Select().FirstTableItems)
			{
				attendee.Invitation = invitation;
				AttendeeForCurrentUser.Update(attendee);
			}

			Base.Save.PressImpl(internalCall: true);
		}

		public PXAction<CRActivity> SendInvitations;
		[PXButton(Tooltip = AttendeeMessages.SendInvitationsTooltip)]
		[PXUIField(DisplayName = AttendeeMessages.SendInvitations, Visible = false, MapEnableRights = PXCacheRights.Select)]
		public virtual IEnumerable sendInvitations(PXAdapter adapter)
		{
			var attendees = Attendees.Select().FirstTableItems.ToList();
			var notInvitedAttendees = NotInvitedAttendees.Select().FirstTableItems.ToList();

			List<EPAttendee> forSend;
			if (!notInvitedAttendees.Any()) // all invited
			{
				forSend = SendInvitationsToAttendeesAnswer
					.WithAnswerForCbApi(WebDialogResult.Yes)
					.WithAnswerForUnattendedMode(WebDialogResult.No)
					.View
					.Ask(null,
						AttendeeMessages.Ask.Title.Confirmation,
						AttendeeMessages.Ask.Body.NotifyAllInvitedAttendees,
						MessageButtons.YesNo,
						new Dictionary<WebDialogResult, string>()
						{
							{ WebDialogResult.Yes, "Confirm" },
							{ WebDialogResult.No, "Cancel" }
						},
						MessageIcon.None,
						true)
					.IsPositive()
					? attendees
					: null;
			}
			else if (notInvitedAttendees.Count != attendees.Count) // exist not invited
			{
				forSend = SendInvitationsToOnlyNotInvitedAttendeesAnswer
					.WithAnswerForCbApi(WebDialogResult.Yes)
					.WithAnswerForUnattendedMode(WebDialogResult.No)
					.View
					.Ask(null,
						AttendeeMessages.Ask.Title.Confirmation,
						AttendeeMessages.Ask.Body.NotifyNotInvitedAttendees,
						MessageButtons.YesNo,
						new Dictionary<WebDialogResult, string>()
						{
							{ WebDialogResult.Yes, "Confirm" },
							{ WebDialogResult.No, "Cancel" },
						},
						MessageIcon.None,
						true)
					.IsPositive()
					? notInvitedAttendees
					: attendees;

			}
			else // all not invited
			{
				forSend = notInvitedAttendees;
			}

			if (forSend != null && forSend.Any())
			{
				EnsureIsSavedWithoutSendingEmails();

				var graph = Base.CloneGraphState();
				PXLongOperation.StartOperation(Base, () =>
				{
					graph
						.GetExtension<EPEventMaint_AttendeeExt>()
						.SendEmails(NotificationTypes.Invitation, forSend);
					graph.Actions.PressSave();
				});
			}

			SendInvitationsToAttendeesAnswer.View.ClearDialog();
			SendInvitationsToOnlyNotInvitedAttendeesAnswer.View.ClearDialog();
			Attendees.View.RequestRefresh();
			return adapter.Get();
		}

		public PXAction<CRActivity> SendPersonalInvitation;
		[PXButton(Tooltip = AttendeeMessages.SendPersonalInvitationTooltip)]
		[PXUIField(DisplayName = AttendeeMessages.SendPersonalInvitation, Visible = false, MapEnableRights = PXCacheRights.Select)]
		public virtual IEnumerable sendPersonalInvitation(PXAdapter adapter)
		{
			var ownerId = Base.CurrentOwner.SelectSingle()?.ContactID;
			var attendee = Attendees.Current;

			bool confirm = attendee != null
				&& (attendee.Invitation == PXInvitationStatusAttribute.NOTINVITED
					|| InvitedAttendees
						.WithAnswerForCbApi(WebDialogResult.Yes)
						.WithAnswerForUnattendedMode(WebDialogResult.No)
						.View
						.Ask(null,
							AttendeeMessages.Ask.Title.Confirmation,
							AttendeeMessages.Ask.Body.ResendPersonalInvitation,
							MessageButtons.YesNo,
							new Dictionary<WebDialogResult, string>()
							{
								{ WebDialogResult.Yes, "Confirm" },
								{ WebDialogResult.No, "Cancel" }
							},
							MessageIcon.None,
							true)
						.IsPositive());

			if (confirm)
			{
				EnsureIsSavedWithoutSendingEmails();

				var graph = Base.CloneGraphState();
				var ext = graph.GetExtension<EPEventMaint_AttendeeExt>();
				PXLongOperation.StartOperation(Base, () =>
				{
					ext.SendEmail(NotificationTypes.Invitation, attendee);

					ext.SendInvitationsToAttendeesAnswer.View.ClearDialog();
					ext.SendInvitationCancellationsToAttendeesAnswer.View.ClearDialog();
					ext.SendInvitationReschdulesToAttendeesAnswer.View.ClearDialog();
					graph.Actions.PressSave();
				});
			}

			Attendees.View.RequestRefresh();
			return adapter.Get();
		}

		[PXOverride]
		public virtual void Persist(Action del)
		{
			AssertAttendees();

			var (sendCancellation, sendReschedule, sendInvitation) = GetInvitationStates();

			var removedAttendees = GetRemovedAttendeesToSendInvitationCancellations().ToList();

			del();

			if (sendCancellation
				|| sendReschedule
				|| sendInvitation
				|| removedAttendees.Any())
			{

				if(PXLongOperation.IsLongOperationContext())
				{
					Send(this);
				}
				else
				{
					var graph = Base.CloneGraphState();
					var ext = graph.GetExtension<EPEventMaint_AttendeeExt>();

					PXLongOperation.StartOperation(Base, () =>
					{
						Send(ext);
					});
				}
			}

			void Send(EPEventMaint_AttendeeExt ext)
			{
				ext.SendInvitationCancellationsToRemovedAttendees(removedAttendees);

				if (sendCancellation)
					ext.SendInvitationCancellationsToAttendees();
				if (sendReschedule)
					ext.SendInvitationReschdulesToAttendees();
				if (sendInvitation)
					ext.SendInvitationsToAttendees();
			}
		}

		[PXOverride]
		public virtual IEnumerable ExecuteSelect(string viewName, object[] parameters, object[] searches,
			string[] sortcolumns, bool[] descendings, PXFilterRow[] filters, ref int startRow,
			int maximumRows, ref int totalRows, ExecuteSelectDelegate executeSelect)
		{
			if (Base.IsCopyPasteContext && viewName == nameof(Attendees))
			{
				viewName = nameof(AttendeesExceptOwner);
			}
			return executeSelect(viewName, parameters, searches,
				sortcolumns, descendings, filters, ref startRow, maximumRows, ref totalRows);
		}

		#endregion

		#region Event Handlers

		public virtual void _(Events.RowSelected<CRActivity> e)
		{
			if (e.Row == null) return;

			bool isOwner = Base.IsCurrentUserOwnerOfEvent(e.Row);

			bool isActivityEditable = Base.WasEventOriginallyEditable(e.Row);

			SendInvitations.SetVisible(isOwner);
			SendInvitations.SetEnabled(isActivityEditable && isOwner);

			SendPersonalInvitation.SetVisible(isOwner);
			SendPersonalInvitation.SetEnabled(isActivityEditable && isOwner);

			var attendeeCache = Base.Caches<EPAttendee>();
			attendeeCache.AllowDelete =
			attendeeCache.AllowInsert =
			attendeeCache.AllowUpdate = isActivityEditable && isOwner;

			var currentAttendee = AttendeeForCurrentUser.SelectSingle();
			bool enabled = !isOwner
				&& currentAttendee != null
				&& isActivityEditable
				&& currentAttendee.Invitation != PXInvitationStatusAttribute.CANCELED;

			AcceptInvitation.SetVisible(!isOwner);
			RejectInvitation.SetVisible(!isOwner);
			AcceptInvitation.SetEnabled(enabled && currentAttendee.Invitation != PXInvitationStatusAttribute.ACCEPTED);
			RejectInvitation.SetEnabled(enabled && currentAttendee.Invitation != PXInvitationStatusAttribute.REJECTED);
		}

		public virtual void _(Events.RowInserted<CRActivity> e)
		{
			EnsureAttendeeForOwner(e.Row);
		}

		public virtual void _(Events.FieldUpdated<CRActivity, CRActivity.ownerID> e)
		{
			if (bool.Equals(e.NewValue, e.OldValue) is false)
			{
				EnsureAttendeeForOwner(e.Row);
			}
		}

		public virtual void _(Events.RowSelected<EPAttendee> e)
		{
			e.Cache.AdjustUI(e.Row)
				.ForAllFields(a =>
				{
					if (e.Row != null
					 &&(e.Row.Invitation != PXInvitationStatusAttribute.NOTINVITED 
					 || e.Row.IsOwner is true))
						a.Enabled = false;
				});
		}

		public virtual void _(Events.FieldUpdated<EPAttendee, EPAttendee.contactID> e)
		{
			if (e.NewValue != e.OldValue)
			{
				if (e.NewValue is int contactId)
					e.Row.Email = Contact.PK.Find(Base, contactId)?.EMail;
				else
					e.Row.Email = null;
			}
		}

		#endregion

		#region SendEmails

		public void SendEmail(NotificationTypes invite, EPAttendee attendee)
		{
			SendEmails(invite, new[] { attendee });
		}

		public virtual void SendEmails(NotificationTypes invite, IEnumerable<EPAttendee> attendees)
		{
			var @event = Base.Events.Current;
			var (attachmentName, attachmentData) = GetAttachmentForEvent(Base.Events.Current);
			foreach (var attendee in attendees)
			{
				AssertAttendee(attendee);
				// body
				var sender = Base.Setup.Current.IsSimpleNotification == true ?
					GetNotificationGeneratorForSimpleEmail(invite, @event) :
					GetNotificationGeneratorForTemplate(invite, @event);

				// address
				sender.MailAccountId = MailAccountManager.DefaultMailAccountID;
				sender.To = attendee.Email;

				// subject

				sender.Subject = invite switch
				{
					NotificationTypes.Cancel => PXMessages.LocalizeFormatNoPrefixNLA(
						AttendeeMessages.Template.SubjectCancelInvitationTo, @event.Subject),
					NotificationTypes.Reschedule => PXMessages.LocalizeFormatNoPrefixNLA(
						AttendeeMessages.Template.SubjectRescheduleOf, @event.Subject),
					NotificationTypes.Invitation => PXMessages.LocalizeFormatNoPrefixNLA(
						AttendeeMessages.Template.SubjectInvitationTo, @event.Subject),
					_ => sender.Subject,
				};

				// attachments
				sender.AddAttachment(attachmentName, attachmentData);

				sender.ParentNoteID = @event.NoteID;

				try
				{
					sender.Send();
				}
				catch (Exception e)
				{
					throw new PXException(e, AttendeeMessages.ErrorDuringEmailSend, e.Message);
				}

				attendee.Invitation = invite switch
				{
					NotificationTypes.Invitation => PXInvitationStatusAttribute.INVITED,
					NotificationTypes.Reschedule => PXInvitationStatusAttribute.RESCHEDULED,
					NotificationTypes.Cancel => PXInvitationStatusAttribute.CANCELED,
					_ => attendee.Invitation,
				};

				if (Attendees.Cache.GetStatus(attendee) != PXEntryStatus.Deleted)
				{
					Attendees.Update(attendee);
					Attendees.Cache.PersistUpdated(attendee);
				}
			}
		}

		public virtual void AssertAttendee(EPAttendee attendees)
		{
			if (string.IsNullOrEmpty(attendees.Email))
			{
				var ex = new PXSetPropertyException<EPAttendee.email>(AttendeeMessages.AttendeeWithoutEmail);
				Attendees.Cache.RaiseExceptionHandling<EPAttendee.email>(attendees, attendees.Email, ex);
				throw ex;
			}
		}

		public virtual void AssertAttendees()
		{
			bool noEmail = false;
			foreach (EPAttendee attendee in Attendees.Cache.Cached)
			{
				var status = Attendees.Cache.GetStatus(attendee);
				if (status.IsIn(PXEntryStatus.Inserted, PXEntryStatus.Updated, PXEntryStatus.Modified))
				{
					try
					{
						AssertAttendee(attendee);
					}
					catch (PXSetPropertyException<EPAttendee.email>)
					{
						noEmail = true;
					}
				}
			}

			if (noEmail)
				throw new PXInvalidOperationException(AttendeeMessages.OneOfAttendeesWithoutEmail);
		}

		public virtual string GetBodyForSimpleNotification(NotificationTypes invite, CRActivity @event, Contact owner)
		{
			var body = new StringBuilder();
			string bodyStringInfo = string.Empty;
			switch (invite)
			{
				case NotificationTypes.Cancel:

					body.Append(PXMessages.LocalizeNoPrefix(AttendeeMessages.Template.EventWasCanceled));
					break;

				case NotificationTypes.Reschedule:

					body.Append(PXMessages.LocalizeNoPrefix(AttendeeMessages.Template.EventWasRescheduled));

					bodyStringInfo = GetEventStringInfo(
						AttendeeMessages.Template.StartDate,
						AttendeeMessages.Template.EndDate);
					break;

				case NotificationTypes.Invitation:
				default:

					body.Append(owner != null
						? PXMessages.LocalizeFormatNoPrefixNLA(
							AttendeeMessages.Template.OwnerInvitedYouToAnEvent,
							owner.DisplayName)
						: PXMessages.LocalizeNoPrefix(AttendeeMessages.Template.YouAreInvitedToAnEvent));

					bodyStringInfo = GetEventStringInfo(
						AttendeeMessages.Template.RescheduledStartDate,
						AttendeeMessages.Template.RescheduledEndDate);
					break;
			}

			body.Append(DoubleNewLine)
				.Append(PXMessages.LocalizeFormatNoPrefixNLA(
					AttendeeMessages.Template.Subject,
					@event.Subject.Trim()))
				.Append(DoubleNewLine);

			if (!string.IsNullOrWhiteSpace(@event.Location))
				body.Append(PXMessages.LocalizeFormatNoPrefixNLA(
						AttendeeMessages.Template.Location,
						@event.Location.Trim()))
					.Append(DoubleNewLine);

			body.Append(bodyStringInfo);

			if (owner != null && Base.Setup.Current.AddContactInformation == true)
			{
				body.Append(DoubleNewLine)
					.Append(DoubleNewLine)
					.Append(PXMessages.LocalizeNoPrefix(AttendeeMessages.Template.ContactPerson))

					.Append(PXMessages.LocalizeFormatNoPrefixNLA(
						AttendeeMessages.Template.Name, owner.DisplayName))

					.Append(PXMessages.LocalizeFormatNoPrefixNLA(
						AttendeeMessages.Template.Email, owner.EMail))

					.Append(PXMessages.LocalizeFormatNoPrefixNLA(
						AttendeeMessages.Template.Phone, owner.Phone1));
			}

			return body.ToString();

			string GetEventStringInfo(string startDateFormat, string endDateFormat)
			{
				if (@event.StartDate == null || @event.EndDate == null)
					return string.Empty;

				var start = @event.StartDate.Value;
				var end = @event.EndDate.Value;
				var timeZone = string.IsNullOrEmpty(@event.TimeZone) ? LocaleInfo.GetTimeZone().DisplayName : PXTimeZoneInfo.FindSystemTimeZoneById(@event.TimeZone).DisplayName;

				var stringInfo = new StringBuilder();

				stringInfo.Append(PXMessages.LocalizeFormatNoPrefixNLA(startDateFormat, start, timeZone));
				stringInfo.Append(DoubleNewLine);
				stringInfo.Append(PXMessages.LocalizeFormatNoPrefixNLA(endDateFormat, end, timeZone));

				if (!string.IsNullOrEmpty(@event.Body))
				{
					var description = Tools.ConvertHtmlToSimpleText(@event.Body);
					description = description.Replace(Environment.NewLine, DoubleNewLine);
					stringInfo.Append(DoubleNewLine);
					stringInfo.Append(description);
				}
				return stringInfo.ToString();
			}
		}

		public virtual NotificationGenerator GetNotificationGeneratorForSimpleEmail(NotificationTypes invite, CRActivity @event)
		{
			return new NotificationGenerator(Base)
			{
				Body = GetBodyForSimpleNotification(invite, @event, Base.CurrentOwner.SelectSingle())
			};
		}

		public virtual NotificationGenerator GetNotificationGeneratorForTemplate(NotificationTypes invite, CRActivity @event)
		{
			var settings = Base.Setup.Current;

			var field = invite switch
			{
				NotificationTypes.Cancel => nameof(settings.CancelInvitationTemplateID),
				NotificationTypes.Reschedule => nameof(settings.RescheduleTemplateID),
				_ => nameof(settings.InvitationTemplateID),
			};

			if (Base.Setup.Cache.GetValue(settings, field) is int templateId)
			{
				return TemplateNotificationGenerator.Create(Base, @event, (int)templateId);
			}

			var fieldName = Base.Setup.Cache.GetStateExt(null, field) is PXFieldState state
				? state.DisplayName
				: field;
			throw new PXInvalidOperationException(AttendeeMessages.EmailTemplateIsNotConfigured, fieldName);
		}

		public virtual (string name, byte[] data) GetAttachmentForEvent(CRActivity @event)
		{
			byte[] card;
			using (var buffer = new MemoryStream())
			{
				var vevent = Base.VCalendarFactory.CreateVEvent(@event);
				vevent.Method = "REQUEST";
				vevent.Write(buffer);
				card = buffer.ToArray();
			}

			return ("event.ics", card);
		}

		#endregion

		#region  CancelInvitations

		public virtual bool ShouldCancelEvent()
		{
			return Base.WasCurrentEventOriginallyEditable()
				&& Base.Events.Current?.UIStatus == ActivityStatusListAttribute.Canceled;
		}

		public virtual bool ConfirmToSendInvitationCancellationsToAttendees()
		{
			if (Base.IsCurrentEventPersisted() && !Base.IsCurrentEventInThePast()
				&& Base.WasCurrentEventOriginallyEditable())
			{
				var invitedAttendees = InvitedAttendees.Select();
				if (invitedAttendees.Any())
				{
					return SendInvitationCancellationsToAttendeesAnswer
						.WithAnswerForCbApi(WebDialogResult.Yes)
						.WithAnswerForUnattendedMode(WebDialogResult.No)
						.View
						.Ask(null,
							AttendeeMessages.Ask.Title.Confirmation,
							AttendeeMessages.Ask.Body.ConfirmCancelAttendeeInvitations,
							MessageButtons.YesNo,
							new Dictionary<WebDialogResult, string>()
							{
								{ WebDialogResult.Yes, "Confirm" },
								{ WebDialogResult.No, "Cancel" }
							},
							MessageIcon.None,
							true)
						.IsPositive();
				}
			}
			return false;
		}

		public virtual void SendInvitationCancellationsToAttendees()
		{
			// hach for template generator to see current event during delete (when it is already deleted)
			using (new PXReadDeletedScope())
			{
				SendEmails(NotificationTypes.Cancel,
					InvitedAttendees.Select().FirstTableItems);
			}
		}

		public virtual IEnumerable<EPAttendee> GetRemovedAttendeesToSendInvitationCancellations()
		{
			if (Base.IsCurrentEventEditable() && !Base.IsCurrentEventInThePast())
			{
				var deletedAttendees = Attendees
					.Cache
					.Deleted
					.OfType<EPAttendee>()
					.Where(a =>
						a.Email != null
						&& a.Invitation.IsIn(
							PXInvitationStatusAttribute.INVITED,
							PXInvitationStatusAttribute.ACCEPTED,
							PXInvitationStatusAttribute.RESCHEDULED));
				foreach (var attendee in deletedAttendees)
				{
					yield return attendee;
					// hack to update attendees after persist
					Attendees.Cache.Remove(attendee);
				}
			}
		}

		public virtual void SendInvitationCancellationsToRemovedAttendees(IEnumerable<EPAttendee> removedAttendees)
		{
			// hach for template generator to see current event during delete (when it is already deleted)
			using (new PXReadDeletedScope())
			{
				if (removedAttendees.Any())
					SendEmails(NotificationTypes.Cancel, removedAttendees);
			}
			foreach (var attendee in removedAttendees)
			{
				// hack to update attendees after persist
				Attendees.Cache.ResetPersisted(attendee);
				Attendees.Cache.PersistDeleted(attendee);
			}
		}

		#endregion

		#region SendInvitations

		public virtual bool ConfirmToSendInvitationsToAttendees()
		{
			if (!Base.IsCurrentEventInThePast()
				&& Base.IsCurrentEventEditable())
			{
				var attendees = Attendees.Select();
				var notInvitedAttendees = NotInvitedAttendees.Select();
				if (attendees.Count > 0 && notInvitedAttendees.Count > 0)
				{
					return SendInvitationsToAttendeesAnswer
						.WithAnswerForCbApi(WebDialogResult.Yes)
						.WithAnswerForUnattendedMode(WebDialogResult.No)
						.View
						.Ask(null,
							AttendeeMessages.Ask.Title.Confirmation,
							AttendeeMessages.Ask.Body.NotifyAttendees,
							MessageButtons.YesNo,
							new Dictionary<WebDialogResult, string>()
							{
								{ WebDialogResult.Yes, "Confirm" },
								{ WebDialogResult.No, "Cancel" }
							},
							MessageIcon.None,
							true)
						.IsPositive();
				}
			}
			return false;
		}

		public virtual void SendInvitationsToAttendees()
		{
			SendEmails(
				NotificationTypes.Invitation,
				NotInvitedAttendees.Select().FirstTableItems);
		}

		#endregion

		#region RescheduleInvitations

		public virtual bool ConfirmToSendInvitationReschdulesToAttendees()
		{
			if (Base.IsCurrentEventPersisted() && !Base.IsCurrentEventInThePast() && Base.IsCurrentEventEditable())
			{
				if (InvitedAttendees.Select().Any())
				{
					return SendInvitationReschdulesToAttendeesAnswer
						.WithAnswerForCbApi(WebDialogResult.Yes)
						.WithAnswerForUnattendedMode(WebDialogResult.No)
						.View
						.Ask(null,
							AttendeeMessages.Ask.Title.Confirmation,
							AttendeeMessages.Ask.Body.ConfirmRescheduleNotification,
							MessageButtons.YesNo,
							new Dictionary<WebDialogResult, string>()
							{
								{ WebDialogResult.Yes, "Confirm" },
								{ WebDialogResult.No, "Cancel" }
							},
							MessageIcon.None,
							true)
						.IsPositive();
				}
			}

			return false;
		}

		public virtual void SendInvitationReschdulesToAttendees()
		{
			SendEmails(NotificationTypes.Reschedule,
				InvitedAttendees.Select().FirstTableItems);
		}

		public virtual bool ShouldRescheduleEvent()
		{
			var activity = Base.Events.Current;
			var original = Base.Events.Cache.GetOriginal(activity) as CRActivity;
			bool isDateChanged = original == null
			 || !Base.Events.Cache.ObjectsEqualBy<
				 TypeArrayOf<IBqlField>.FilledWith<
					 CRActivity.startDate,
					 CRActivity.endDate,
					 CRActivity.timeZone
				 >>(original, activity);
			return isDateChanged
				&& !Base.IsEventInThePast(activity)
				&& Base.IsEventEditable(activity);
		}

		#endregion

		#region Helpers

		public virtual (bool sendCancellation, bool sendReschedule, bool sendInvitation) GetInvitationStates()
		{
			bool sendCancellation = false,
				 sendReschedule = false,
				 sendInvitation = false;

			if (Base.Events.Current == null)
			{
				// no current while deleting on primary screen ¯\_(ツ)_/¯
				var deleted = Base.Events.Cache.Deleted.OfType<CRActivity>().ToList();
				// just in case check only single object is deleting
				if (deleted.Count != 1)
				{
					return (false, false, false);
				}
				Base.Events.Current = deleted[0];
			}

			switch (Base.Events.Cache.GetStatus(Base.Events.Current))
			{
				case PXEntryStatus.Deleted:
					SendInvitationCancellationsToAttendeesAnswer.WithAnswerForMobile(WebDialogResult.Yes); // hack for AC-211812

					if (ConfirmToSendInvitationCancellationsToAttendees())
						sendCancellation = true;
					break;

				case PXEntryStatus.Updated:
				case PXEntryStatus.Modified:
					if (ShouldCancelEvent())
					{
						if (ConfirmToSendInvitationCancellationsToAttendees())
							sendCancellation = true;
						break;
					}
					if (ShouldRescheduleEvent() && ConfirmToSendInvitationReschdulesToAttendees())
						sendReschedule = true;
					goto case PXEntryStatus.Inserted;

				case PXEntryStatus.Inserted:
					if (ConfirmToSendInvitationsToAttendees())
						sendInvitation = true;
					break;
			}

			return (sendCancellation, sendReschedule, sendInvitation);
		}

		public virtual void EnsureIsSavedWithoutSendingEmails()
		{
			if (Base.IsDirty)
			{
				SetAllPrimaryAnswers(WebDialogResult.No);

				Base.Actions.PressSave();
			}
		}

		public virtual void SetAllPrimaryAnswers(WebDialogResult answer)
		{
			SendInvitationsToAttendeesAnswer.View.Answer =
			SendInvitationCancellationsToAttendeesAnswer.View.Answer =
			SendInvitationReschdulesToAttendeesAnswer.View.Answer = answer;
		}

		public virtual void EnsureAttendeeForOwner(CRActivity row)
		{
			if (row == null)
				return;

			using (new ReadOnlyScope(Base.Caches<EPAttendee>()))
			{
				bool exists = false;
				foreach (EPAttendee attendee in AttendeeForOwner.View.SelectMultiBound(currents: new[] { row }))
				{
					if (!exists && row.OwnerID != null)
					{
						if (attendee.ContactID != row.OwnerID)
						{
							attendee.ContactID = row.OwnerID;
							AttendeeForOwner.Update(attendee);
						}
						exists = true;
					}
					else
						AttendeeForOwner.Delete(attendee);
				}

				if (!exists && row?.OwnerID != null)
				{
					AttendeeForCurrentUser.Insert(
						new EPAttendee
						{
							ContactID = row.OwnerID,
							EventNoteID = row.NoteID,
							IsOwner = true,
							Invitation = PXInvitationStatusAttribute.ACCEPTED,
						});
				}
			}
		}

		#endregion
	}
}
