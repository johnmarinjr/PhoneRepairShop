using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Data.EP;
using PX.Export.Imc;
using PX.Objects.EP.Imc;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.SM;
using PX.TM;
using PX.Objects.PM;
using ResHandler = PX.TM.PXResourceScheduleAttribute.HandlerAttribute;
using PX.Objects.Common.GraphExtensions.Abstract;
using PX.Data.BQL;
using PX.Api.Models;
using System.Text.RegularExpressions;

namespace PX.Objects.EP
{
	#region SendCardFilter

	[Serializable]
	[PXHidden]
	public partial class SendCardFilter : IBqlTable
	{
		#region Email

		public abstract class email : PX.Data.BQL.BqlString.Field<email> { }
		
		[PXUIField(DisplayName = "Email")]
		[PXDefault]
		[PXDBEmail]
		public virtual string Email { get; set; }

		#endregion
	}

	#endregion

	public class EPEventMaint : CRBaseActivityMaint<EPEventMaint, CRActivity>, PX.Objects.EndpointAdapters.IRelatedActivitiesView, ICaptionable
	{
		#region Extensions

		public class EmbeddedImagesExtractor : EmbeddedImagesExtractorExtension<EPEventMaint, CRActivity, CRActivity.body>
		{
		}

		#endregion

		#region Fields

		internal const string RefNoteType_CbApi_FieldName = "RefNoteID_Type";

		#endregion

		#region Selects

		[PXViewName(Messages.Events)]
		[PXRefNoteSelector(typeof(CRActivity), typeof(CRActivity.refNoteID))]
		[PXCopyPasteHiddenFields(
			typeof(CRActivity.showAsID),
			typeof(CRActivity.uistatus),
			typeof(CRActivity.startDate),
			typeof(CRActivity.endDate),
			typeof(CRActivity.ownerID))]
		public PXSelect<CRActivity,
				Where<CRActivity.classID, Equal<CRActivityClass.events>>>
			Events;


		[PXHidden]
		public PXSelect<CT.Contract>
				BaseContract;

		[PXHidden]
		public PXSelect<PMTimeActivity>
			TimeActivitiesOld;

		[PXCopyPasteHiddenFields(typeof(PMTimeActivity.timeSpent), typeof(PMTimeActivity.timeBillable),
			 typeof(PMTimeActivity.overtimeSpent), typeof(PMTimeActivity.overtimeBillable))]
		public PMTimeActivityList<CRActivity>
			TimeActivity;

		[PXHidden]
		public PXSelect<CRChildActivity>
				ChildAct;

		[PXCopyPasteHiddenView]
		[PXFilterable]
		[CRReference(typeof(CRActivity.bAccountID),typeof(CRActivity.contactID))]
		[PXViewDetailsButton(typeof(CRActivity), OnClosingPopup = PXSpecialButtonType.Refresh)]
		public CRChildActivityList<CRActivity>
			ChildActivities;

		PXSelectBase PX.Objects.EndpointAdapters.IRelatedActivitiesView.Activities => ChildActivities;

		public PXSetup<EPSetup> 
			Setup;

		public PXSelect<Contact,
			Where<Contact.contactID, Equal<Current<CRActivity.ownerID>>>> 
			CurrentOwner;

		public PXSelect<Contact> 
			ContactSearch;

		public PXFilter<SendCardFilter> 
			SendCardSettings;

		public PXSelectJoin<CSCalendar,
			InnerJoin<EPEmployee, 
				On<EPEmployee.calendarID, Equal<CSCalendar.calendarID>>>,
			Where<EPEmployee.userID, Equal<Required<EPEmployee.userID>>>> 
			WorkCalendar;

		public CRReminderList<CRActivity>
			Reminder;

		#endregion

		[InjectDependency]
		public IVCalendarFactory VCalendarFactory { get; private set; }

		#region Ctors

		public EPEventMaint()
			: base()
		{
			PXUIFieldAttribute.SetVisible(Caches[typeof(Users)], typeof(Users.username).Name, true);
			var activitiesCache = Caches[typeof (CRActivity)];
			PXUIFieldAttribute.SetDisplayName<CRActivity.startDate>(activitiesCache, Messages.StartTime);

			PXView relEntityView = new PXView(this, true, new Select<CRSMEmail>(), new PXSelectDelegate(GetRelatedEntity));
			Views.Add("RelatedEntity", relEntityView);
            ActivityStatusAttribute.SetRestictedMode<CRActivity.uistatus>(Events.Cache, true);

			var cache = this.Caches<CRActivity>();
			cache.Fields.Add(RefNoteType_CbApi_FieldName);
			FieldSelecting.AddHandler(typeof(CRActivity), RefNoteType_CbApi_FieldName, RefNoteTypeFieldSelecting);
		}

		public string Caption()
		{
			CRActivity currentItem = this.Events.Current;
			if (currentItem == null) return "";

			if (currentItem.Subject != null)
				return $"{currentItem.Subject}";
			return "";
		}

		#endregion

		#region Data Handlers

		public IEnumerable GetRelatedEntity()
		{
			var current = Events.Current;
			if (current != null && current.RefNoteID != null)
			{
				var row = new EntityHelper(this).GetEntityRow(current.RefNoteID);
				if (row != null) yield return row;
			}
		}

		#endregion

		#region Actions

		public PXCopyPasteAction<CRActivity> CopyPaste;

		public PXDelete<CRActivity> Delete;

		public PXAction<CRActivity> Complete;
		[PXUIField(DisplayName = PX.TM.Messages.CompleteEvent, MapEnableRights = PXCacheRights.Select)]
		[PXButton(Tooltip = PX.Objects.EP.Messages.CompleteEventTooltip,
			IsLockedOnToolbar = true,
			Category = Messages.ManagementCategory,
			ShortcutCtrl = true, ShortcutChar = (char)75)] //Ctrl + K
		protected virtual void complete()
		{
			CompleteRow(Events.Current);
		}

		public PXAction<CRActivity> CompleteAndFollowUp;
		[PXUIField(DisplayName = Messages.CompleteAndFollowUpEvent, MapEnableRights = PXCacheRights.Select, Visible = false)]
		[PXButton(Tooltip = Messages.CompleteAndFollowUpEventTooltip, ShortcutCtrl = true, ShortcutShift = true, ShortcutChar = (char)75)] //Ctrl + Shift + K
		protected virtual void completeAndFollowUp()
		{
			CRActivity row = Events.Current;
			if (row == null) return;

			CompleteRow(row);

			EPEventMaint graph = CreateInstance<EPEventMaint>();

			CRActivity followUpActivity = (CRActivity)graph.Events.Cache.CreateCopy(row);
			followUpActivity.NoteID = null;
			followUpActivity.ParentNoteID = row.ParentNoteID;
			followUpActivity.UIStatus = null;
			followUpActivity.NoteID = null;
			followUpActivity.PercentCompletion = null;

			if (followUpActivity.StartDate != null)
			{
				followUpActivity.StartDate = ((DateTime) followUpActivity.StartDate).AddDays(1D);
			}
			if (followUpActivity.EndDate != null)
				followUpActivity.EndDate = ((DateTime)followUpActivity.EndDate).AddDays(1D);
			
			graph.Events.Cache.Insert(followUpActivity);

			if (!this.IsContractBasedAPI)
				PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.NewWindow);

			graph.Save.Press();
		}

		public PXAction<CRActivity> CancelActivity;

		[PXUIField(DisplayName = PX.TM.Messages.CancelEvent, MapEnableRights = PXCacheRights.Select)]
		[PXButton(Tooltip = PX.TM.Messages.CancelTask, 
				IsLockedOnToolbar = true,
				Category = Messages.ManagementCategory)]
		protected virtual IEnumerable cancelActivity(PXAdapter adapter)
		{
			CancelRow(Events.Current);
			return adapter.Get();
		}

		public PXAction<CRActivity> ExportCard;
		[PXUIField(DisplayName = PX.TM.Messages.ExportCard, MapEnableRights = PXCacheRights.Select)]
		[PXButton(Tooltip = PX.TM.Messages.ExportCardTooltip, Category = Messages.OtherCategory)]
		public virtual void exportCard()
		{
			var row = Events.Current;
			if (row != null)
			{
				var vCard = VCalendarFactory.CreateVEvent(row);
				throw new EPIcsExportRedirectException(vCard);
			}
		}

		public PXAction<CRActivity> sendCard;
		[PXButton(Tooltip = Messages.SendCardTooltip, Category = Messages.OtherCategory)]
		[PXUIField(DisplayName = Messages.SendCard, MapEnableRights = PXCacheRights.Select)]
		public virtual IEnumerable SendCard(PXAdapter adapter)
		{
			if (Events.Current == null) return adapter.Get();

			if (SendCardSettings.AskExtRequired())
			{
				CRActivity _event = Events.Current;
				string newLine = Environment.NewLine + Environment.NewLine;
				string mailTo = SendCardSettings.Current.Email;
				string mailBody = PXLocalizer.Localize(Messages.EventNumber, typeof(Messages).FullName) + ": " + _event.NoteID.Value + newLine +
							PXLocalizer.Localize(Messages.Subject, typeof(Messages).FullName) + ": " + _event.Subject + newLine +
							GetEventStringInfo(_event, newLine, string.Empty);
				string mailSubject = PXLocalizer.Localize(Messages.Event, typeof(Messages).FullName) + ": " + _event.Subject;
				NotificationGenerator sender = new NotificationGenerator
								{
									To = mailTo,
									Subject = mailSubject,
									Body = mailBody,
									Owner = Accessinfo.ContactID
								};
				using (MemoryStream buffer = new MemoryStream())
				{
					CreateVEvent().Write(buffer);
					sender.AddAttachment("event.ics", buffer.ToArray());
				}
				sender.Send();
			}
			return adapter.Get();
		}

		public PXMenuAction<CRActivity> Action;

		#endregion

		#region Event Handlers

		#region CacheAttached

		[EPStartDate(
						TimeZone = typeof(CRActivity.timeZone),
						OwnerID = typeof(CRActivity.ownerID),
						AllDayField = typeof(CRActivity.allDay),
						DisplayName = "Start Date",
						DisplayNameDate = "Date",
						DisplayNameTime = "Start Time",
						IgnoreRequireTimeOnActivity = true)]
		[PXFormula(typeof(Round30Minutes<TimeZoneNow>))]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		protected virtual void _(Events.CacheAttached<CRActivity.startDate> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(EPEndDateAttribute), nameof(EPEndDateAttribute.TimeZone), typeof(CRActivity.timeZone))]
		[PXCustomizeBaseAttribute(typeof(EPEndDateAttribute), nameof(EPEndDateAttribute.OwnerID), typeof(CRActivity.ownerID))]
		protected virtual void _(Events.CacheAttached<CRActivity.endDate> e) { }

		[PXDefault(typeof(CRActivityClass.events))]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		protected virtual void _(Events.CacheAttached<CRActivity.classID> e) { }
				
		[PXFormula(typeof(False))]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		protected virtual void _(Events.CacheAttached<PMTimeActivity.needToBeDeleted> e) { }

		[PXFormula(typeof(Default<CRActivity.allDay>))]
		[PXMergeAttributes(Method = MergeMethod.Append)]
		protected virtual void _(Events.CacheAttached<CRActivity.timeZone> e) { }

		[PXDefault(typeof(Switch<
				Case<Where<
						Current<CRActivity.allDay>, Equal<True>>,
					RemindAtListAttribute.before1day>,
				RemindAtListAttribute.before15minutes>),
			PersistingCheck = PXPersistingCheck.Nothing)]
		[PXCustomizeBaseAttribute(typeof(RemindAtListAttribute), nameof(RemindAtListAttribute.IsAllDay), typeof(CRActivity.allDay))]
		[PXMergeAttributes(Method = MergeMethod.Append)]
		protected virtual void _(Events.CacheAttached<CRReminder.remindAt> e) { }

		#endregion

		protected virtual void _(Events.RowSelected<PMTimeActivity> e)
		{
			var row = e.Row as PMTimeActivity;
			if (row == null) return;

			var cache = e.Cache;

			int timespent = 0;
			int overtimespent = 0;
			int timebillable = 0;
			int overtimebillable = 0;

			foreach (CRPMTimeActivity child in ChildActivities.Select(row.RefNoteID))
			{
				timespent += (child.TimeSpent ?? 0);
				overtimespent += (child.OvertimeSpent ?? 0);
				timebillable += (child.TimeBillable ?? 0);
				overtimebillable += (child.OvertimeBillable ?? 0);
			}

			row.TimeSpent = timespent;
			row.OvertimeSpent = overtimespent;
			row.TimeBillable = timebillable;
			row.OvertimeBillable = overtimebillable;

			PXUIFieldAttribute.SetEnabled<PMTimeActivity.timeSpent>(cache, row, false);
			PXUIFieldAttribute.SetEnabled<PMTimeActivity.overtimeSpent>(cache, row, false);
			PXUIFieldAttribute.SetEnabled<PMTimeActivity.timeBillable>(cache, row, false);
			PXUIFieldAttribute.SetEnabled<PMTimeActivity.overtimeBillable>(cache, row, false);
		}

		protected virtual void _(Events.RowSelected<CRActivity> e)
		{
			CRActivity row = e.Row as CRActivity;
			if (row == null) return;

			var cache = e.Cache;

			var tAct = (PMTimeActivity)TimeActivity.SelectSingle();
			var tActCache = TimeActivity.Cache;

			string origStatus = (string)cache.GetValueOriginal<CRActivity.uistatus>(row) ?? ActivityStatusListAttribute.Open;

			bool isCanEdit = IsCurrentUserCanEditEvent(row);
			bool editable = isCanEdit && origStatus == ActivityStatusListAttribute.Open;
			PXUIFieldAttribute.SetEnabled(tActCache, tAct, editable);

			PXUIFieldAttribute.SetEnabled<CRActivity.noteID>(cache, row);
			PXUIFieldAttribute.SetEnabled<CRActivity.createdByID>(cache, row, false);
			PXUIFieldAttribute.SetEnabled<CRActivity.completedDate>(cache, row, false);
			PXUIFieldAttribute.SetEnabled<CRActivity.source>(cache, row, false);
			PXUIFieldAttribute.SetEnabled<CRActivity.workgroupID>(cache, e.Row, false);

			PXUIFieldAttribute.SetEnabled<PMTimeActivity.timeSpent>(tActCache, tAct, false);
			PXUIFieldAttribute.SetEnabled<PMTimeActivity.overtimeSpent>(tActCache, tAct, false);
			PXUIFieldAttribute.SetEnabled<PMTimeActivity.timeBillable>(tActCache, tAct, false);
			PXUIFieldAttribute.SetEnabled<PMTimeActivity.overtimeBillable>(tActCache, tAct, false);

			GotoParentActivity.SetEnabled(row.ParentNoteID != null);



			this.ChildActivities.View.AllowUpdate = false;
			this.ChildActivities.View.Cache.AllowInsert = editable;
			cache.AllowUpdate = isCanEdit;
			cache.AllowDelete = isCanEdit;
			PXUIFieldAttribute.SetEnabled(cache, row, editable);

			Save.SetEnabled(editable);
			SaveClose.SetEnabled(editable);

			PXUIFieldAttribute.SetVisible<CRActivity.timeZone>(cache, row, editable);
			PXUIFieldAttribute.SetEnabled<CRActivity.ownerID>(cache, row, IsCurrentUserCanEditOwnerField(row));

			Complete.SetVisible(isCanEdit);
			CancelActivity.SetVisible(isCanEdit);

			Complete.SetEnabled(editable && row.UIStatus != ActivityStatusListAttribute.Completed && row.UIStatus != ActivityStatusListAttribute.Canceled);
			CancelActivity.SetEnabled(editable && row.UIStatus != ActivityStatusListAttribute.Completed && row.UIStatus != ActivityStatusListAttribute.Canceled);

			// Acuminator disable once PX1043 SavingChangesInEventHandlers [Persist is called only once on the entity opening by the user]
			// Acuminator disable once PX1044 ChangesInPXCacheInEventHandlers [EPView is placed in cache to Hold the entity]
			MarkAs(cache, row, Accessinfo.ContactID, EPViewStatusAttribute.VIEWED);
		}

		protected virtual void _(Events.RowSelected<CRReminder> e)
		{
			CRReminder row = e.Row as CRReminder;
			if (row == null)
				return;

			string origStatus = (string)this.Events.Cache.GetValueOriginal<CRActivity.uistatus>(this.Events.Current) ?? ActivityStatusListAttribute.Open;

			bool editable = origStatus == ActivityStatusListAttribute.Open;

			PXUIFieldAttribute.SetEnabled<CRReminder.remindAt>(Reminder.Cache, row, editable);

			bool isCanEditEvent = IsCurrentUserCanEditEvent(this.Events.Current) && editable;

			PXUIFieldAttribute.SetEnabled<CRReminder.isReminderOn>(Reminder.Cache, row, isCanEditEvent);
		}

		public virtual void _(Events.FieldSelecting<CRReminder.remindAt> e)
		{
			if (e.Row == null)
			{
				return;
			}
			CRReminder row = (CRReminder)e.Row;

			if (string.IsNullOrEmpty(row.RemindAt) || row.RemindAt == RemindAtListAttribute.DateTimeFromExchange)
			{
				List<string> allowedValues = new List<string>();
				List<string> allowedLabels = new List<string>();

				DateTime dtReminder = (DateTime)e.Cache.GetValue<CRReminder.reminderDate>(e.Row);
				string tzCode = Events.Current?.TimeZone;
				if (string.IsNullOrEmpty(tzCode) == false)
				{
					dtReminder = PXTimeZoneInfo.ConvertTimeToUtc(dtReminder, LocaleInfo.GetTimeZone());
					var tzDest = PXTimeZoneInfo.FindSystemTimeZoneById(tzCode);
					dtReminder = PXTimeZoneInfo.ConvertTimeFromUtc(dtReminder, tzDest);
				}

				allowedValues.Add(RemindAtListAttribute.DateTimeFromExchange);
				allowedLabels.Add(dtReminder.ToString());

				e.ReturnState = PXStringState.CreateInstance(e.ReturnState, null, null, nameof(CRReminder.remindAt), false, null, null, 
									allowedValues.ToArray(), allowedLabels.ToArray(), null, RemindAtListAttribute.DateTimeFromExchange);
				e.ReturnValue = RemindAtListAttribute.DateTimeFromExchange;
				((PXFieldState)e.ReturnState).Enabled = false;
				e.Args.Cancel = true;
			}
		}

		protected virtual void _(Events.FieldUpdated<CRActivity, CRActivity.allDay> e)
		{
			Reminder.Cache.SetDefaultExt<CRReminder.remindAt>(Reminder.Current);
		}

		protected virtual void _(Events.FieldUpdated<CRActivity, CRActivity.startDate> e)
		{
			if (e.Row == null)
				return;

			Reminder.Cache.RaiseFieldUpdated<CRReminder.remindAt>(Reminder.Current, null);
		}

		protected virtual void _(Events.FieldVerifying<CRActivity, CRActivity.timeZone> e)
		{
			if (e.Row == null)
				return;

			if (string.IsNullOrEmpty((string)e.NewValue))
			{
				throw new PXSetPropertyException(Messages.TimeZoneCannotBeEmpty, PXErrorLevel.Warning);
			}
			e.Cache.RaiseFieldUpdated<CRActivity.startDate>(e.Row, null);
		}

		protected virtual void _(Events.FieldUpdated<CRActivity, CRActivity.timeZone> e)
		{
			if (e.Row == null)
				return;

			e.Cache.RaiseFieldUpdated<CRActivity.startDate>(e.Row, null);
		}

		protected virtual void _(Events.FieldUpdated<CRReminder, CRReminder.remindAt> e)
		{
			if (e.Row == null)
				return;

			var activity = this.Events.Current;
			if (activity == null)
				return;

			DateTime? reminderDate = activity.StartDate?.Add(RemindAtListAttribute.GetRemindAtTimeSpan((string)e.NewValue)) ?? e.Row.ReminderDate;
			if (activity.TimeZone != null && activity.TimeZone != GetDefaultTimeZone())
			{
				reminderDate = PXTimeZoneInfo.ConvertTimeToUtc((DateTime)reminderDate, PXTimeZoneInfo.FindSystemTimeZoneById(activity.TimeZone));
				reminderDate = PXTimeZoneInfo.ConvertTimeFromUtc((DateTime)reminderDate, PXTimeZoneInfo.FindSystemTimeZoneById(GetDefaultTimeZone()));
			}
			e.Row.ReminderDate = reminderDate;
			e.Cache.SetStatus(e.Row, PXEntryStatus.Modified);

			if (IsMobile)
			{
				e.Cache.RaiseRowSelected(e.Row);
			}
		}

		protected virtual void _(Events.FieldDefaulting<CRActivity.timeZone> e)
		{
			e.NewValue = GetDefaultTimeZone();
			e.Cancel = true;
		}

		protected virtual void _(Events.FieldUpdated<CRActivity, CRActivity.refNoteID> e)
		{
			CRActivity activity = (CRActivity)e.Row;
			if (activity == null) return;

			if (Caches[typeof(RelatedEntity)].Current != null)
			{
				var related = Views["RelatedEntity"].SelectSingle();
				if (related == null) return;

				var relatedType = related.GetType();

				if (typeof(Contact).IsAssignableFrom(relatedType))
				{
					Contact contact = related as Contact;
					activity.ContactID = contact?.ContactID;
					activity.BAccountID = contact?.BAccountID;
				}
				else if (typeof(BAccount).IsAssignableFrom(relatedType))
				{
					BAccount contact = related as BAccount;
					activity.ContactID = null;
					activity.BAccountID = contact?.BAccountID;
				}
			}
		}

		protected virtual void _(Events.FieldSelecting<CRActivity, CRActivity.source> e)
		{
			if (IsContractBasedAPI && e.Row is CRActivity && e.Row.RefNoteID is Guid refNoteID)
			{
				var helper = new EntityHelper(this);
				var type = helper.GetEntityRowType(refNoteID);
				e.ReturnValue = helper.GetEntityDescription(refNoteID, type);
			}
		}

		internal void RefNoteTypeFieldSelecting(PXCache cache, PXFieldSelectingEventArgs e)
		{
			if (IsContractBasedAPI
				&& e.Row is CRActivity activity
				&& activity.RefNoteID is Guid refNoteID)
			{
				var dacType = new EntityHelper(this).GetEntityRowType(refNoteID, suspendTypeCalculation: true);
				if (dacType != null)
				{
					e.ReturnValue = dacType.GetLongName();
				}
			}
		}

		#endregion

		#region Public Methods

		public override void CompleteRow(CRActivity row)
		{
			if (row == null)
				return;

			string origStatus = (string)Events.Cache.GetValueOriginal<CRActivity.uistatus>(row) ?? ActivityStatusListAttribute.Open;
			if (origStatus == ActivityStatusListAttribute.Completed ||
				origStatus == ActivityStatusListAttribute.Canceled)
			{
				return;
			}

			CRActivity activityCopy = (CRActivity)Events.Cache.CreateCopy(row);
			activityCopy.UIStatus = ActivityStatusListAttribute.Completed;
			Events.Cache.Update(activityCopy);
			Actions.PressSave();
		}

		public override void CancelRow(CRActivity row)
		{
			if (row == null)
				return;

			string origStatus = (string)Events.Cache.GetValueOriginal<CRActivity.uistatus>(row) ?? ActivityStatusListAttribute.Open;
			if (origStatus == ActivityStatusListAttribute.Completed ||
				origStatus == ActivityStatusListAttribute.Canceled)
			{
				return;
			}

			CRActivity activityCopy = (CRActivity)Events.Cache.CreateCopy(row);
			activityCopy.UIStatus = ActivityStatusListAttribute.Canceled;
			Events.Cache.Update(activityCopy);
			Actions.PressSave();
		}

		public override void CopyPasteGetScript(bool isImportSimple, List<Command> script, List<Container> containers)
		{
			var indexes = script.SelectIndexesWhere(f =>
					Regex.IsMatch(f.FieldName,
						$"(?:{nameof(CRActivity.StartDate)}|{nameof(CRActivity.EndDate)})_(?:Date|Time)",
						RegexOptions.IgnoreCase))
				.Reverse();
			foreach (var index in indexes)
			{
				script.RemoveAt(index);
				containers.RemoveAt(index);
			}
		}

		public virtual bool IsCurrentUserAdministrator()
		{
			return PXSiteMapProvider.IsUserInRole(PXAccess.GetAdministratorRole());
		}

		#region Event Status

		public virtual bool IsCurrentUserCanEditOwnerField(CRActivity row)
		{
			int? origOwner = (int?)Events.Cache.GetValueOriginal<CRActivity.ownerID>(row);
			bool isOwnerEdited = (origOwner == null) ? false : (origOwner.Equals(row.OwnerID) == false);

			return (Events.Cache.GetStatus(row) == PXEntryStatus.Inserted)
						|| origOwner == null
						|| isOwnerEdited;
		}

		public virtual bool IsCurrentUserCanEditEvent(CRActivity row)
		{
			return IsCurrentUserOwnerOfEvent(row)
					|| (Events.Cache.GetStatus(row) == PXEntryStatus.Inserted)
					|| Events.Cache.GetValueOriginal<CRActivity.ownerID>(row) == null;
		}

		public virtual bool IsCurrentUserOwnerOfEvent(CRActivity row)
		{
			return PXAccess.GetContactID() == row.OwnerID;
		}

		public virtual bool IsEventEditable(CRActivity row)
		{
			return row?.UIStatus.IsIn(null, ActivityStatusListAttribute.Open) ?? true;
		}

		public virtual bool WasEventOriginallyEditable(CRActivity row)
		{
			return IsEventEditable(Events.Cache.GetOriginal(row) as CRActivity);
		}

		public virtual bool IsEventInThePast(CRActivity row)
		{
			if (row?.EndDate == null)
			{
				return false;
			}
			var utcEndDate = PXTimeZoneInfo.ConvertTimeToUtc(
								(DateTime)row.EndDate,
								string.IsNullOrEmpty(row?.TimeZone) ? LocaleInfo.GetTimeZone() : PXTimeZoneInfo.FindSystemTimeZoneById(row.TimeZone));
			return utcEndDate < PXTimeZoneInfo.UtcNow;
		}

		public virtual bool IsEventPersisted(CRActivity row) => Events.Cache.GetOriginal(row) != null;

		public bool IsCurrentEventEditable() => IsEventEditable(Events.Current);

		public bool IsCurrentEventInThePast() => IsEventInThePast(Events.Current);

		public bool IsCurrentEventPersisted() => IsEventPersisted(Events.Current);

		public bool WasCurrentEventOriginallyEditable() => WasEventOriginallyEditable(Events.Current);

		#endregion

		#endregion

		#region Private/protected Methods

		protected virtual string GetDefaultTimeZone()
		{
			var set = PXSelectJoin<UserPreferences,
				InnerJoin<Users, On<Users.pKID, Equal<UserPreferences.userID>>>,
				Where<Users.pKID, Equal<Current<AccessInfo.userID>>>>.
			Select(this);
			return
				(set == null || set.Count <= 0 || string.IsNullOrEmpty(((UserPreferences)set[0][typeof(UserPreferences)]).TimeZone)
						? LocaleInfo.GetTimeZone().Id
						: ((UserPreferences)set[0][typeof(UserPreferences)]).TimeZone);
		}

		private string GetEventStringInfo(CRActivity _event, string newLineString, string prefix)
		{
			var start = _event.StartDate.Value;
			var end = _event.EndDate.Value;
			var timeZone = LocaleInfo.GetTimeZone().DisplayName;
			string bodyAddInfo = prefix + PXLocalizer.Localize(Messages.StartDate, typeof(Messages).FullName) + ": " + start.ToLongDateString() + " " + start.ToShortTimeString() + " " + timeZone + newLineString +
								 prefix + PXLocalizer.Localize(Messages.EndDate, typeof(Messages).FullName) + ": " + end.ToLongDateString() + " " + end.ToShortTimeString() + " " + timeZone;
			CRActivity gEvent = _event as CRActivity;
			if (gEvent != null)
			{
				PXStringState valueExt = Events.Cache.GetValueExt(gEvent, PXLocalizer.Localize(Messages.Duration, typeof(Messages).FullName)) as PXStringState;
				if (valueExt != null)
				{
					bodyAddInfo += newLineString + prefix + PXLocalizer.Localize(Messages.Duration, typeof(Messages).FullName) + ": ";
					string valueText = valueExt.Value.ToString();
					bodyAddInfo += string.IsNullOrEmpty(valueExt.InputMask) ? valueText :
						PX.Common.Mask.Format(valueExt.InputMask, valueText);
				}
			}
			if (!string.IsNullOrEmpty(_event.Body))
			{
				var description = Tools.ConvertHtmlToSimpleText(_event.Body);
				description = description.Replace(Environment.NewLine, newLineString);
				bodyAddInfo += newLineString + description;
			}
			return bodyAddInfo;
		}

		private vEvent CreateVEvent()
		{
			var vevent = VCalendarFactory.CreateVEvent(Events.Current);
			vevent.Method = "REQUEST";
			return vevent;
		}
		#endregion
	}
}
