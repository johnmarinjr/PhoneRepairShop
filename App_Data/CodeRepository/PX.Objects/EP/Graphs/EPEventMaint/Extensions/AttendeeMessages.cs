using PX.Common;

namespace PX.Objects.EP.Graphs.EPEventMaint.Extensions
{
	[PXLocalizable]
	public static class AttendeeMessages
	{
		#region Actions
		public const string AcceptInvitation = "Accept";
		public const string AcceptInvitationTooltip = "Accept the invitation to the event";
		public const string RejectInvitation = "Decline";
		public const string RejectInvitationTooltip = "Decline the invitation to the event";
		public const string SendInvitations = "Invite All";
		public const string SendInvitationsTooltip = "Send the invitations to all attendees by email";
		public const string SendPersonalInvitation = "Invite";
		public const string SendPersonalInvitationTooltip = "Send the invitations to the selected attendees by email";

		#endregion

		#region Errors
		public const string EmailTemplateIsNotConfigured = "{0} is not configured on the Event Setup (EP204070) form.";
		public const string ErrorDuringEmailSend = "The following error has occurred during the sending of the emails: {0}";
		public const string OneOfAttendeesWithoutEmail = "At least one email address of an attendee has not been found. The email address cannot be empty.";
		public const string AttendeeWithoutEmail = "The email address cannot be empty.";
		#endregion

		public static class Ask
		{
			[PXLocalizable]
			public static class Title
			{
				public const string Confirmation = "Confirmation";
			}

			[PXLocalizable]
			public static class Body
			{
				public const string ResendPersonalInvitation = "The invitation that has already been sent will be sent once again.";
				public const string ConfirmCancelAttendeeInvitations = "The invitations that have already been sent will be canceled.";
				public const string NotifyNotInvitedAttendees = "The invitations will be sent to only the potential attendees who have not been invited.";
				public const string NotifyAllInvitedAttendees = "The invitations will be sent to all potential attendees, including those that were previously invited.";
				public const string NotifyAttendees = "At least one potential attendee has been selected. The invitations will be sent to all selected potential attendees.";
				public const string ConfirmRescheduleNotification = "The invited potential attendees will be notified about the new start time of the event.";
			}
		}

		[PXLocalizable]
		public static class Template
		{
			public const string SubjectCancelInvitationTo = "Cancel the invitation to {0}";
			public const string SubjectRescheduleOf = "Rescheduling of {0}";
			public const string SubjectInvitationTo = "Invitation to {0}";

			public const string OwnerInvitedYouToAnEvent = "{0} invited you to the event.";
			public const string YouAreInvitedToAnEvent = "You are invited to the event.";
			public const string Subject = "Subject: {0}";
			public const string Location = "Location: {0}";
			public const string StartDate = "Start Date: {0} {1}";
			public const string EndDate = "Due Date: {0} {1}";
			public const string RescheduledStartDate = "New Start Date: {0} {1}";
			public const string RescheduledEndDate = "New Due Date: {0} {1}";
			public const string Duration = "Duration: {0}";
			public const string EventWasCanceled = "The event was canceled.";
			public const string EventWasRescheduled = "The event was rescheduled.";
			public const string ContactPerson = "Contact Person:";
			public const string Name = "Name: {0}";
			public const string Email = "Email: {0}";
			public const string Phone = "Phone: {0}";
		}
	}
}
