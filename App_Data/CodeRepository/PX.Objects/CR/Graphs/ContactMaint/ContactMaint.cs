using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.CR.MassProcess;
using PX.Objects.CR.Workflows;
using PX.Objects.CR.Extensions.CRCreateActions;
using PX.Objects.GL;
using PX.Objects.CS;
using PX.Objects.EP;
using PX.SM;
using PX.EP;
using PX.Objects.Extensions.ContactAddress;
using PX.Data.BQL;
using PX.Data.MassProcess;
using PX.Objects.CR.Extensions;
using PX.Objects.CR.Extensions.Cache;
using PX.Objects.CR.Extensions.Relational;
using PX.Objects.CR.Extensions.CRDuplicateEntities;
using PX.Objects.CR.Extensions.CRContactAccountDataSync;
using PX.Data.BQL.Fluent;
using PX.Data.WorkflowAPI;
using PX.Objects.CR.Extensions.SideBySideComparison;
using PX.Objects.CR.Extensions.SideBySideComparison.Link;

namespace PX.Objects.CR
{
	public class ContactMaint : PXGraph<ContactMaint, Contact, Contact.displayName>, ICaptionable
	{
		#region Inner Types
		[Serializable]
        [PXHidden]
		public class CurrentUser : Users
		{
			public new abstract class pKID : PX.Data.BQL.BqlGuid.Field<pKID> { }
			public new abstract class guest : PX.Data.BQL.BqlBool.Field<guest> { }
		}
		#endregion

		#region Selects

		//TODO: need review
		[PXHidden]
		public PXSelect<BAccount>
			bAccountBasic;

		[PXHidden]
		public PXSetup<Company>
			company;

		[PXHidden]
        public IN.PXSetupOptional<CRSetup>
			Setup;

		[PXViewName(Messages.Contact)]
		[PXCopyPasteHiddenFields(typeof(Contact.duplicateStatus), typeof(Contact.duplicateFound))]
		public SelectContactEmailSync<Where<Contact.contactType, Equal<ContactTypesAttribute.person>>>
			Contact;

		public PXSelect<Contact,
				Where<Contact.contactID, Equal<Current<Contact.contactID>>>>
			ContactCurrent;
		
		public PXSelect<Contact,
				Where<Contact.contactID, Equal<Current<Contact.contactID>>>>
			ContactCurrent2;

		[PXViewName(Messages.Leads)]
		[PXFilterable]
		[PXViewDetailsButton(typeof(Contact))]
		public PXSelectJoin<
				CRLead,
				InnerJoin<Address,
					On<Address.addressID, Equal<Contact.defAddressID>>,
					LeftJoin<CRActivityStatistics,
						On<CRActivityStatistics.noteID, Equal<CRLead.noteID>>>>,
				Where<
					CRLead.refContactID, Equal<Current<Contact.contactID>>>,
				OrderBy<
					Desc<CRLead.createdDateTime>>>
			Leads;

		[PXCopyPasteHiddenView]
		public PXSelect<CRActivityStatistics,
				Where<CRActivityStatistics.noteID, Equal<Current<Contact.noteID>>>>
			ContactActivityStatistics;

		[PXViewName(Messages.Address)]
		public SelectFrom<Address>
			.Where<
				Address.addressID.IsEqual<Contact.defAddressID.FromCurrent>>
			.View
			AddressCurrent;

	    [PXCopyPasteHiddenView()]
		public PXSelectUsers<Contact, Where<Users.pKID, Equal<Current<Contact.userID>>>> User;
        [PXCopyPasteHiddenView()]
		public PXSelectUsersInRoles UserRoles;
        [PXCopyPasteHiddenView()]
		public PXSelectAllowedRoles Roles;

		[PXViewName(Messages.Answers)]
		public CRAttributeList<Contact>
			Answers;

		[PXViewName(Messages.Activities)]
		[PXFilterable]
		[CRDefaultMailTo]
		[CRReference(typeof(Contact.bAccountID),typeof(Contact.contactID))]
		public CRActivityList<Contact>
			Activities;
		
		public PXSelectJoin<EMailSyncAccount,
			InnerJoin<BAccount,
				On<BAccount.bAccountID, Equal<EMailSyncAccount.employeeID>>>,
			Where<BAccount.defContactID, Equal<Optional<Contact.contactID>>>> SyncAccount;

		public PXSelect<EMailAccount,
			Where<EMailAccount.emailAccountID, Equal<Optional<EMailSyncAccount.emailAccountID>>>> EMailAccounts;

		[PXCopyPasteHiddenView]
		[PXViewName(Messages.Relations)]
		[PXFilterable]
		public CRRelationsList<Contact.noteID>
			Relations;

		[PXHidden]
		public PXSelect<CROpportunityClass>
			CROpportunityClass;

		[PXViewName(Messages.Opportunities)]
		[PXFilterable]
		[PXViewDetailsButton(typeof(Contact))]
		public PXSelectReadonly2<CROpportunity,
			LeftJoin<CROpportunityProbability, On<CROpportunityProbability.stageCode, Equal<CROpportunity.stageID>>,
			LeftJoin<CROpportunityClass, On<CROpportunityClass.cROpportunityClassID, Equal<CROpportunity.classID>>>>,
			Where<CROpportunity.contactID, Equal<Current<Contact.contactID>>>>
			Opportunities;

		[PXFilterable]
		[PXViewDetailsButton(typeof(Contact))]
		public PXSelectReadonly<CRCase,
			Where<CRCase.contactID, Equal<Current<Contact.contactID>>,
            And<Where<Current<Contact.bAccountID>, IsNull,
                   Or<CRCase.customerID, Equal<Current<Contact.bAccountID>>>>>>>
			Cases;

        [PXCopyPasteHiddenView]
		[PXViewName(Messages.CampaignMember)]
		[PXFilterable]
		[PXViewDetailsButton(typeof(Contact), 
			typeof(Select<CRCampaign, 
				Where<CRCampaign.campaignID, Equal<Current<CRCampaignMembers.campaignID>>>>))]
		public PXSelectJoin<CRCampaignMembers,
			InnerJoin<CRCampaign, On<CRCampaignMembers.campaignID, Equal<CRCampaign.campaignID>>>,
			Where<CRCampaignMembers.contactID, Equal<Current<Contact.contactID>>>>
			Members;

		[PXHidden]
		public PXSelect<CRMarketingListMember>
			Subscriptions_stub;

		[PXCopyPasteHiddenView]
		[PXViewName(Messages.Subscriptions)]
		[PXFilterable]
		[PXViewDetailsButton(typeof(Contact),
			typeof(Select<CRMarketingList,
				Where<CRMarketingList.marketingListID, Equal<Current<CRMarketingListMember.marketingListID>>>>))]
		public CRMMarketingContactSubscriptions<Contact, Contact.contactID>
			Subscriptions;
		
		[PXViewName(Messages.Notifications)]
		public PXSelectJoin<ContactNotification,
			InnerJoin<NotificationSetup,
				On<NotificationSetup.setupID, Equal<ContactNotification.setupID>>>,
			Where<ContactNotification.contactID, Equal<Optional<Contact.contactID>>>>
			NWatchers;

		#endregion

		#region Ctors

		public ContactMaint()
		{
			PXUIFieldAttribute.SetRequired<Contact.lastName>(Contact.Cache, true);

		    // HACK graph can contain separate caches for BAccount and BAccountR, so force display names for BAccount cache
		    PXUIFieldAttribute.SetDisplayName<BAccount.acctCD>(Caches[typeof(BAccount)], Messages.BAccountCD);
		    PXUIFieldAttribute.SetDisplayName<BAccount.acctName>(Caches[typeof(BAccount)], Messages.BAccountName);

			PXUIFieldAttribute.SetDisplayName<BAccountR.acctCD>(Caches[typeof(BAccountR)], Messages.BAccountCD);
			PXUIFieldAttribute.SetDisplayName<BAccountR.acctName>(Caches[typeof(BAccountR)], Messages.BAccountName);

            Activities.GetNewEmailAddress =
				() =>
				{
					var contact = Contact.Current;
					return contact != null && !string.IsNullOrWhiteSpace(contact.EMail)
						? PXDBEmailAttribute.FormatAddressesWithSingleDisplayName(contact.EMail, contact.DisplayName)
						: String.Empty;
				};

			PXUIFieldAttribute.SetEnabled<EPLoginTypeAllowsRole.rolename>(Roles.Cache, null, false);
			Roles.Cache.AllowInsert = false;
			Roles.Cache.AllowDelete = false;
		    PXUIFieldAttribute.SetVisible<CRMarketingListMember.format>(Subscriptions.Cache, null, false);

			PXUIFieldAttribute.SetVisible<Contact.languageID>(ContactCurrent.Cache, null, PXDBLocalizableStringAttribute.HasMultipleLocales);

			// for cb api
			Opportunities.Cache.Fields.Add("_Contact_DisplayName");
			FieldSelecting.AddHandler(typeof(CROpportunity), "_Contact_DisplayName", (s, e) =>
			{
				e.ReturnValue = Contact.Current?.DisplayName;
			});
		}

	    public override void InitCacheMapping(Dictionary<Type, Type> map)
	    {
	        base.InitCacheMapping(map);
            Caches.AddCacheMappingsWithInheritance(this, typeof(BAccount));
	    }

		public string Caption()
		{
			Contact currentItem = this.Contact.Current;
			if (currentItem == null) return "";

			if (!String.IsNullOrEmpty(currentItem.FullName))
			{
				return $"{currentItem.DisplayName} - {currentItem.FullName}";
			}
			else
			{
				return $"{currentItem.DisplayName}";
			}
		}

	    #endregion

		#region Actions

		public PXMenuAction<Contact> Action;

		public PXDBAction<Contact> addOpportunity;
        [PXUIField(DisplayName = Messages.CreateNewOpportunity, FieldClass = FeaturesSet.customerModule.FieldClass)]
		[PXButton]
		public virtual void AddOpportunity()
		{
			var row = ContactCurrent.Current;
			if (row == null || row.ContactID == null) return;
		
			var graph = PXGraph.CreateInstance<OpportunityMaint>();
			var newOpportunity = graph.Opportunity.Insert();
            newOpportunity.BAccountID = row.BAccountID;

            newOpportunity.Source = row.Source;

			CRContactClass cls = PXSelect<
					CRContactClass,
				Where<
					CRContactClass.classID, Equal<Required<Contact.classID>>>>
				.Select(this, row.ClassID);

			if (cls?.TargetOpportunityClassID != null)
			{
				newOpportunity.ClassID = cls.TargetOpportunityClassID;
			}
			else
			{
				newOpportunity.ClassID = this.Setup.Current?.DefaultOpportunityClassID;
			}

			CROpportunityClass ocls = PXSelect<CROpportunityClass, Where<CROpportunityClass.cROpportunityClassID, Equal<Current<CROpportunity.classID>>>>
				.SelectSingleBound(this, new object[] { newOpportunity });
			if (ocls?.DefaultOwner == CRDefaultOwnerAttribute.Source)
			{
				newOpportunity.WorkgroupID = row.WorkgroupID;
				newOpportunity.OwnerID = row.OwnerID;
			}

			newOpportunity.ContactID = row.ContactID;
			UDFHelper.CopyAttributes(ContactCurrent.Cache, row, graph.Opportunity.Cache, graph.Opportunity.Current, newOpportunity.ClassID);
			graph.Opportunity.Update(newOpportunity);

			if (!this.IsContractBasedAPI)
				PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.NewWindow);

			graph.Save.Press();
		}

		public PXDBAction<Contact> addCase;
        [PXUIField(DisplayName = Messages.CreateNewCase, FieldClass = FeaturesSet.customerModule.FieldClass)]
		[PXButton]
		public virtual void AddCase()
		{
		    var row = ContactCurrent.Current;
		    if (row == null || row.ContactID == null) return;

			var graph = PXGraph.CreateInstance<CRCaseMaint>();
			var newCase = (CRCase)graph.Case.Cache.CreateInstance();
			newCase = PXCache < CRCase >.CreateCopy(graph.Case.Insert(newCase));
			UDFHelper.CopyAttributes(ContactCurrent.Cache, row, graph.Case.Cache, graph.Case.Current, newCase.CaseClassID);
			newCase.CustomerID = row.BAccountID;
			newCase.ContactID = row.ContactID;
			try
            {
                graph.Case.Update(newCase);
            }
            catch{}
			
			if (!this.IsContractBasedAPI)
				PXRedirectHelper.TryRedirect(graph, PXRedirectHelper.WindowMode.NewWindow);

			graph.Save.Press();
		}

		public PXAction<Contact> copyBAccountContactInfo;
		[PXButton(ImageKey = PX.Web.UI.Sprite.Main.ArrowDown, Tooltip = Messages.CopyFromCompany, DisplayOnMainToolbar = false)]
		[PXUIField(DisplayName = Messages.CopyFromCompany)]
		public virtual void CopyBAccountContactInfo()
		{
			var row = ContactCurrent.Current as Contact;
			if (row == null || row.BAccountID == null) return;

			var acct = (BAccount)PXSelect<BAccount,
				Where<BAccount.bAccountID, Equal<Required<BAccount.bAccountID>>>>.
				Select(this, row.BAccountID);
			if (acct != null && acct.DefContactID != null)
			{
				var defContact = (Contact)PXSelect<Contact,
					Where<Contact.contactID, Equal<Required<Contact.contactID>>>>.
					Select(this, acct.DefContactID);
				if (defContact != null)
					CopyContactInfo(row, defContact);
				ContactCurrent.Update(row);
			}

			if (this.IsContractBasedAPI)
				this.Save.Press();
		}

		public PXAction<Contact> deleteMarketingList;
        [PXUIField(DisplayName = Messages.Delete)]
        [PXButton(ImageKey = PX.Web.UI.Sprite.Main.Remove)]
        public virtual void DeleteMarketingList()
        {
            CRMarketingListMember marketingListMember = Subscriptions.Cache.Current as CRMarketingListMember;
            if (marketingListMember == null) return;
            CRMarketingList marketingList = PXSelect<CRMarketingList, Where<CRMarketingList.marketingListID,
                Equal<Required<CRMarketingList.marketingListID>>>>.Select(this, marketingListMember.MarketingListID);

            if (marketingList == null) return;

            if (marketingList.IsDynamic == true)
                return;

                Subscriptions.Cache.Delete(marketingListMember);              
        }

		#endregion

        #region Event Handlers

        #region Contact

		[PXUIField(DisplayName = "Contact ID")]
		[ContactSelector(true, typeof(ContactTypesAttribute.person))]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		protected virtual void _(Events.CacheAttached<Contact.contactID> e) { }

		[PXUIField(DisplayName = "Contact Class")]
		[PXDefault(typeof(Search<CRSetup.defaultContactClassID>), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		protected virtual void _(Events.CacheAttached<Contact.classID> e) { }

		[ContactSynchronize]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		protected virtual void _(Events.CacheAttached<Contact.synchronize> e) { }

		[CRMBAccount(bAccountTypes: new[]
		{
			typeof(BAccountType.prospectType),
			typeof(BAccountType.customerType),
			typeof(BAccountType.combinedType),
			typeof(BAccountType.vendorType),
		})]
		[PopupMessage]
		[PXMergeAttributes(Method = MergeMethod.Replace)]
		protected virtual void _(Events.CacheAttached<Contact.bAccountID> e) { }

		[PXUIField(DisplayName = "Account Name", Visibility = PXUIVisibility.SelectorVisible)]
		[CRLeadFullName(typeof(Contact.bAccountID))]
		[PXMassMergableField]
		[PXPersonalDataField]
		[PXMergeAttributes(Method = MergeMethod.Replace)]
		protected virtual void _(Events.CacheAttached<Contact.fullName> e) { }

		[CRMParentBAccount(typeof(Contact.bAccountID))]
		[PXFormula(typeof(Selector<Contact.bAccountID, BAccount.parentBAccountID>))]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		protected virtual void _(Events.CacheAttached<Contact.parentBAccountID> e) { }

		[PXDefault(ContactStatus.Active, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXUIRequired(typeof(Where<Contact.contactType, Equal<ContactTypesAttribute.person>>))]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		protected virtual void _(Events.CacheAttached<Contact.status> e) { }
		
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.Enabled), true)]
		protected virtual void _(Events.CacheAttached<Contact.isActive> e) { }

		#region Users logic

		[PXDBGuid(IsKey = true)]
		[PXDefault]
		[PXUIField(Visibility = PXUIVisibility.Invisible)]
		[PXParent(typeof(Select<Contact, Where<Contact.userID, Equal<Current<Users.pKID>>>>))]
		[PXMergeAttributes(Method = MergeMethod.Replace)]
		public virtual void Users_PKID_CacheAttached(PXCache sender){}

		[PXDBString(64, IsUnicode = true, InputMask = "" /*"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA||.'@-_"*/)]
		[PXUIField(DisplayName = "Login")]
		[PXUIRequired(typeof(Where<Users.loginTypeID, IsNotNull, And<EntryStatus, Equal<EntryStatus.inserted>>>))]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		public virtual void Users_Username_CacheAttached(PXCache sender) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXSelector(typeof(Search2<Contact.contactID,
				LeftJoin<Users, On<Contact.userID, Equal<Users.pKID>>,
				LeftJoin<BAccount, On<BAccount.defContactID, Equal<Contact.contactID>>>>,
					Where<Current<Users.guest>, Equal<True>, And<Contact.contactType, Equal<ContactTypesAttribute.person>,
						Or<Current<Users.guest>, NotEqual<True>, And<Contact.contactType, Equal<ContactTypesAttribute.employee>, And<BAccount.bAccountID, IsNotNull>>>>>>),
			typeof(Contact.displayName),
			typeof(Contact.salutation),
			typeof(Contact.fullName),
			typeof(Contact.eMail),
			typeof(Users.username),
			DescriptionField = typeof(Contact.displayName))]
		[PXRestrictor(typeof(Where<Contact.userID, IsNull, Or<Contact.userID, Equal<Current<Users.pKID>>>>), PX.Objects.CR.Messages.ContactWithUser, typeof(Contact.displayName))]
		[PXDBScalar(typeof(Search<Contact.contactID, Where<Contact.userID, Equal<Users.pKID>>>))]
		protected virtual void Users_ContactID_CacheAttached(PXCache sender)
		{
		}

		//DONE: need to duplicate in User Maint
		[PXDBInt]
		[PXUIField(DisplayName = "User Type")]
		[PXRestrictor(typeof(Where<EPLoginType.entity, Equal<EPLoginType.entity.contact>>), Messages.NonContactLoginType, typeof(EPLoginType.loginTypeName))]
		[PXSelector(typeof(Search5<EPLoginType.loginTypeID, LeftJoin<EPManagedLoginType, On<EPLoginType.loginTypeID, Equal<EPManagedLoginType.loginTypeID>>,
								LeftJoin<Users, On<EPManagedLoginType.parentLoginTypeID, Equal<Users.loginTypeID>>,
								LeftJoin<CurrentUser, On<CurrentUser.pKID, Equal<Current<AccessInfo.userID>>>>>>,
								Where<Users.pKID, Equal<CurrentUser.pKID>, And<CurrentUser.guest, Equal<True>,
									Or<CurrentUser.guest, NotEqual<True>>>>, 
								Aggregate<GroupBy<EPLoginType.loginTypeID, GroupBy<EPLoginType.loginTypeName, GroupBy<EPLoginType.requireLoginActivation, GroupBy<EPLoginType.resetPasswordOnLogin>>>>>>), 
			SubstituteKey = typeof(EPLoginType.loginTypeName))]
		[PXDefault(PersistingCheck = PXPersistingCheck.Nothing)]
		protected virtual void Users_LoginTypeID_CacheAttached(PXCache sender) { }

		[PXUIField(DisplayName = "Guest Account")]
		[PXFormula(typeof(Switch<Case<Where<Selector<Users.loginTypeID, EPLoginType.entity>, Equal<EPLoginType.entity.contact>>, True>, False>))]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		protected virtual void Users_Guest_CacheAttached(PXCache sender) { }

		[PXFormula(typeof(Selector<Users.loginTypeID, EPLoginType.requireLoginActivation>))]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		protected virtual void Users_IsPendingActivation_CacheAttached(PXCache sender) { }

		[PXFormula(typeof(Switch<Case<Where<Selector<Users.loginTypeID, EPLoginType.resetPasswordOnLogin>, Equal<True>>, True>, False>))]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		protected virtual void Users_PasswordChangeOnNextLogin_CacheAttached(PXCache sender) { }

		[PXDefault(true, PersistingCheck = PXPersistingCheck.Nothing)]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		protected virtual void Users_GeneratePassword_CacheAttached(PXCache sender) { }

		[PXDBString(256, IsKey = true, IsUnicode = true, InputMask = "")]
		[PXDefault(typeof(Users.username))]
		[PXParent(typeof(Select<Users, Where<Users.username, Equal<Current<UsersInRoles.username>>>>))]
		[PXMergeAttributes(Method = MergeMethod.Replace)]	// delete PXSelector from DAC
		protected virtual void UsersInRoles_Username_CacheAttached(PXCache sender) { }
		protected virtual void Users_State_FieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			if (e.ReturnValue == null && (e.Row == null || sender.GetStatus(e.Row) == PXEntryStatus.Inserted))
			{
				e.ReturnValue = Users.state.NotCreated;
			}
		}

		protected virtual void Users_LoginTypeID_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			UserRoles.Cache.Clear();
			if (((Users) e.Row).LoginTypeID == null)
			{
				User.Cache.Clear();
				Contact.Current.UserID = null;
			}
		}
		
		protected virtual void Users_Username_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			Users user = (Users)e.Row;

			if (e.OldValue != null && user.Username != null && e.OldValue.ToString() != user.Username)
				User.Cache.RaiseExceptionHandling<Users.username>(User.Current, User.Current.Username, new PXSetPropertyException(Messages.LoginChangedError));
		}

        protected virtual void Users_Username_FieldVerifying(PXCache sender, PXFieldVerifyingEventArgs e)
        {
            Guid? restoredGuid = Access.GetGuidFromDeletedUser((string)e.NewValue);
            if (restoredGuid != null)
            {
                ((Users)e.Row).PKID = restoredGuid;
            }
        }
        protected virtual void Users_RowUpdated(PXCache sender, PXRowUpdatedEventArgs e)
        {
            Users user = (Users)e.Row;
            if (user == null || Contact.Current == null || ((Users)e.Row).LoginTypeID == null) return;				

			if (Contact.Current == null)
				Contact.Current = Contact.Select();
				Contact.Current.UserID = user.PKID;
			Contact.Cache.MarkUpdated(Contact.Current);

			if (this.IsContractBasedAPI) Roles.Select();
        }
		protected virtual void Users_RowInserted(PXCache sender, PXRowInsertedEventArgs e)
		{
			Users user = (Users) e.Row;

			EPLoginType ltype = PXSelect<EPLoginType, Where<EPLoginType.loginTypeID, Equal<Current<Users.loginTypeID>>>>.SelectSingleBound(this, new object[]{user});
			user.Username = ltype != null && ltype.EmailAsLogin == true ? Contact.Current.EMail : null;
			Guid? restoredGuid = Access.GetGuidFromDeletedUser(user.Username);
			if (restoredGuid != null)
			{
				user.PKID = restoredGuid;
			}

			if (Contact.Current.UserID == null)
			{
				Contact.Current.UserID = user.PKID;
			}
			else
			{
				User.Cache.Clear();
				UserRoles.Cache.Clear();
			}

		}

		#endregion

		protected virtual void _(Events.RowSelected<Contact> e)
		{
			Contact row = e.Row as Contact;
			if (row == null) return;

			var isNotInserted = e.Cache.GetStatus(row) != PXEntryStatus.Inserted;
            Contact.AllowDelete = row.ContactType == ContactTypesAttribute.Person;
            PXUIFieldAttribute.SetEnabled<Contact.classID>(Contact.Cache, row, row.ContactType == ContactTypesAttribute.Person);

            copyBAccountContactInfo.SetEnabled(row.ContactType == ContactTypesAttribute.Person);
			Answers.Cache.AllowInsert = row.ContactType == ContactTypesAttribute.Person;
			Answers.Cache.AllowUpdate = row.ContactType == ContactTypesAttribute.Person;
			Answers.Cache.AllowDelete = row.ContactType == ContactTypesAttribute.Person;

			Activities.Cache.AllowInsert = row.ContactType == ContactTypesAttribute.Person;
			Activities.Cache.AllowUpdate = row.ContactType == ContactTypesAttribute.Person;
			Activities.Cache.AllowDelete = row.ContactType == ContactTypesAttribute.Person;

			Relations.Cache.AllowInsert = row.ContactType == ContactTypesAttribute.Person && isNotInserted;
			Relations.Cache.AllowDelete = row.ContactType == ContactTypesAttribute.Person;

			Opportunities.Cache.AllowInsert = row.ContactType == ContactTypesAttribute.Person && isNotInserted;
			Opportunities.Cache.AllowUpdate = row.ContactType == ContactTypesAttribute.Person;
			Opportunities.Cache.AllowDelete = row.ContactType == ContactTypesAttribute.Person;

			Cases.Cache.AllowInsert = row.ContactType == ContactTypesAttribute.Person && isNotInserted;
			Cases.Cache.AllowUpdate = row.ContactType == ContactTypesAttribute.Person;
			Cases.Cache.AllowDelete = row.ContactType == ContactTypesAttribute.Person;

			Members.Cache.AllowInsert = row.ContactType == ContactTypesAttribute.Person && isNotInserted;
			Members.Cache.AllowUpdate = row.ContactType == ContactTypesAttribute.Person;
			Members.Cache.AllowDelete = row.ContactType == ContactTypesAttribute.Person;

			Subscriptions.Cache.AllowInsert = row.ContactType == ContactTypesAttribute.Person && isNotInserted;
			Subscriptions.Cache.AllowUpdate = row.ContactType == ContactTypesAttribute.Person;
			Subscriptions.Cache.AllowDelete = row.ContactType == ContactTypesAttribute.Person;

			NWatchers.Cache.AllowInsert = row.ContactType == ContactTypesAttribute.Person && isNotInserted;
			NWatchers.Cache.AllowUpdate = row.ContactType == ContactTypesAttribute.Person;
			NWatchers.Cache.AllowDelete = row.ContactType == ContactTypesAttribute.Person;

			User.Cache.AllowInsert = row.ContactType == ContactTypesAttribute.Person;
			User.Cache.AllowUpdate = row.ContactType == ContactTypesAttribute.Person;
			User.Cache.AllowDelete = row.ContactType == ContactTypesAttribute.Person;
			User.Cache.AllowSelect = row.ContactType == ContactTypesAttribute.Person;
			User.Cache.ClearQueryCacheObsolete();

			Roles.Cache.AllowInsert = row.ContactType == ContactTypesAttribute.Person;
			Roles.Cache.AllowUpdate = row.ContactType == ContactTypesAttribute.Person;
			Roles.Cache.AllowDelete = row.ContactType == ContactTypesAttribute.Person;
			Roles.Cache.AllowSelect = row.ContactType == ContactTypesAttribute.Person;
			Roles.Cache.ClearQueryCacheObsolete();

			UserRoles.Cache.AllowInsert = row.ContactType == ContactTypesAttribute.Person;
			UserRoles.Cache.AllowUpdate = row.ContactType == ContactTypesAttribute.Person;
			UserRoles.Cache.AllowDelete = row.ContactType == ContactTypesAttribute.Person;
			UserRoles.Cache.AllowSelect = row.ContactType == ContactTypesAttribute.Person;
			UserRoles.Cache.ClearQueryCacheObsolete();

			var bAccount = row.BAccountID.
				With<int?, BAccount>(_ => (BAccount)PXSelect<BAccount,
					Where<BAccount.bAccountID, Equal<Required<BAccount.bAccountID>>>>.
				Select(this, _));
			var isCustomerOrProspect = bAccount == null || 
				bAccount.Type == BAccountType.CustomerType ||
				bAccount.Type == BAccountType.ProspectType || 
				bAccount.Type == BAccountType.CombinedType;
			addOpportunity.SetEnabled(isNotInserted && isCustomerOrProspect);
			addCase.SetEnabled(isNotInserted && isCustomerOrProspect);

			PXUIFieldAttribute.SetEnabled<Contact.contactID>(e.Cache, row, true);
			PXUIFieldAttribute.SetEnabled<Contact.bAccountID>(e.Cache, row, row.ContactType == ContactTypesAttribute.Person);

			CRContactClass contactClass = row.ClassID.
				With(_ => (CRContactClass)PXSelectReadonly<CRContactClass,
					Where<CRContactClass.classID, Equal<Required<CRContactClass.classID>>>>.
					SelectSingleBound(this, null, _));
			if (contactClass != null)
			{
				Activities.DefaultEMailAccountId = contactClass.DefaultEMailAccountID;
			}

			bool isUserInserted = row.UserID == null || User.Cache.GetStatus(User.Current) == PXEntryStatus.Inserted;
			bool hasLoginType = isUserInserted && User.Current != null && User.Current.LoginTypeID != null;
			PXUIFieldAttribute.SetEnabled<Users.loginTypeID>(User.Cache, User.Current, isUserInserted && row.IsActive == true);
			PXUIFieldAttribute.SetEnabled<Users.username>(User.Cache, User.Current, this.IsContractBasedAPI || hasLoginType);
			PXUIFieldAttribute.SetEnabled<Users.generatePassword>(User.Cache, User.Current, this.IsContractBasedAPI || hasLoginType);
			PXUIFieldAttribute.SetEnabled<Users.password>(User.Cache, User.Current, this.IsContractBasedAPI || (hasLoginType && User.Current.GeneratePassword != true));

			var employeeHasUserAttached = row.ContactType == ContactTypesAttribute.Employee && User.Current != null;

			PXDefaultAttribute.SetPersistingCheck<Contact.eMail>(e.Cache, row, 
                employeeHasUserAttached || (hasLoginType && User.Current.Username != null )
                ? PXPersistingCheck.NullOrBlank 
                : PXPersistingCheck.Nothing);
			PXUIFieldAttribute.SetRequired<Contact.eMail>(e.Cache, employeeHasUserAttached || (hasLoginType && User.Current.Username != null));

			User.Current = (Users)User.View.SelectSingleBound(new[] { e.Row });

			PXUIFieldAttribute.SetEnabled<Address.isValidated>(e.Cache, row, false);

			PXUIFieldAttribute.SetEnabled<Contact.duplicateStatus>(e.Cache, row, false);

			PXUIFieldAttribute.SetEnabled<CRActivityStatistics.lastIncomingActivityDate>(e.Cache, row, false);
			PXUIFieldAttribute.SetEnabled<CRActivityStatistics.lastOutgoingActivityDate>(e.Cache, row, false);
		}

		protected virtual void _(Events.RowDeleting<Contact> e)
		{
			Contact contact = (Contact) e.Row;
			if(contact != null && contact.ContactType == ContactTypesAttribute.Employee)
				throw new PXSetPropertyException(Messages.CantDeleteEmployeeContact);
		}

        protected virtual void _(Events.RowUpdated<Contact> e)
        {
            Contact cont = (Contact)e.Row;
            if (cont != null && cont.ContactType == ContactTypesAttribute.Employee && !e.Cache.ObjectsEqual<Contact.displayName>(e.Row, e.OldRow))
            {
                BAccount emp =
                PXSelect<BAccount,
                    Where<BAccount.parentBAccountID, Equal<Current<Contact.bAccountID>>,
                    And<BAccount.defContactID, Equal<Current<CR.Contact.contactID>>>>>.SelectSingleBound(this, new object[]{cont});
                if (emp != null)
                {
                    emp = (BAccount)this.bAccountBasic.Cache.CreateCopy(emp);
                    this.bAccountBasic.Cache.SetValueExt<EPEmployee.acctName>(emp, cont.DisplayName);
                    this.bAccountBasic.Update(emp);
                }
            }
        }

		protected virtual void _(Events.FieldUpdated<Contact, Contact.eMail> e)
		{
			Contact contact = (Contact)e.Row;
			
			foreach (EMailSyncAccount syncAccount in SyncAccount.Select(contact.ContactID)
					   .RowCast<EMailSyncAccount>()
					   .Select(account => (EMailSyncAccount)SyncAccount.Cache.CreateCopy(account)))
			{
				syncAccount.Address = contact.EMail;

				syncAccount.ContactsExportDate = null;
				syncAccount.ContactsImportDate = null;
				syncAccount.EmailsExportDate = null;
				syncAccount.EmailsImportDate = null;
				syncAccount.TasksExportDate = null;
				syncAccount.TasksImportDate = null;
				syncAccount.EventsExportDate = null;
				syncAccount.EventsImportDate = null;

				EMailAccount mailAccount = EMailAccounts.Select(syncAccount.EmailAccountID);
				mailAccount.Address = syncAccount.Address;

                EMailAccounts.Update(mailAccount);
                SyncAccount.Update(syncAccount);

			}
		}


		protected virtual void _(Events.FieldUpdated<Contact, Contact.isActive> e)
		{
			if (e.Row == null)
				return;

			e.Row.Status = e.Row.IsActive is true
				? ContactStatus.Active
				: ContactStatus.Inactive;

			SetLinkedUserStatus(e.Row);
		}

		protected virtual void _(Events.FieldUpdated<Contact, Contact.status> e)
		{
			if (e.Row == null || (string)e.OldValue == (string)e.NewValue)
				return;

			e.Row.IsActive = e.Row.Status is ContactStatus.Inactive
				? false
				: true;

			SetLinkedUserStatus(e.Row);
		}

		private void SetLinkedUserStatus(Contact contact)
		{
			if (contact is null)
				return;

			if (Users.PK.Find(this, contact.UserID) is Users user)
			{
				user.IsApproved = contact.IsActive is true;
				User.Update(user);
			}
		}

		public override void Persist()
		{
			if (Subscriptions?.Cache?.Inserted != null && Subscriptions.Cache.Inserted.Any_())
			{
				foreach (CRMarketingListMember insserted in Subscriptions.Cache.Inserted)
				{
					if (insserted.MarketingListID == 0 && insserted.CreatedByScreenID == "SM206036")
					{
						Subscriptions.Cache.SetStatus(insserted, PXEntryStatus.InsertedDeleted);
					}
				}
			}

			AccessUsers access = null;
			var user = User.SelectSingle();
			if (user != null)
			{
				var copy = PXCache<Users>.CreateCopy(user);
				access = PXGraph.CreateInstance<AccessUsers>();
				access.UserList.Current = copy;
				if (User.Cache.GetStatus(user) == PXEntryStatus.Inserted)
				{
					copy.OldPassword = User.Current.Password;
					copy.NewPassword = User.Current.Password;
					copy.ConfirmPassword = User.Current.Password;

					copy.FirstName = Contact.Current.FirstName;
					copy.LastName = Contact.Current.LastName;
					copy.Email = Contact.Current.EMail;

					copy.IsAssigned = true;

					copy = User.Update(copy);
					copy = access.UserList.Insert(copy);
				}
				else
				{
					copy = access.UserList.Update(copy);
				}

				access.UserList.Current = copy;
			}

			base.Persist();

			if (User.Current != null && User.Current.ContactID == null && Contact.Current != null) // for correct redirection to user after inserting
			{
				User.Current.ContactID = Contact.Current.ContactID;
			}

			if (access != null)
			{
				var data =
				(
					user.OldPassword,
					user.NewPassword,
					user.ConfirmPassword,
					user.GeneratePassword
				);
				access.Cancel.Press();
				access.UserList.UpdateCurrent();
				access.Save.Press();
				Cancel.Press();
				user = User.SelectSingle();
				(
					user.OldPassword,
					user.NewPassword,
					user.ConfirmPassword,
					user.GeneratePassword
				) = data;
			}
		}

		#endregion

		#region Lead

		[PXUIField(DisplayName = "Display Name", Visibility = PXUIVisibility.SelectorVisible, Enabled = false)]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		protected virtual void _(Events.CacheAttached<CRLead.memberName> e) { }

		#endregion

		#region CRCampaignMembers

		[PXDBDefault(typeof(Contact.contactID))]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		protected virtual void CRCampaignMembers_ContactID_CacheAttached(PXCache sender)
		{

		}

		#endregion

		#region CRMarketingListMember

		[PXSelector(typeof(Search<CRMarketingList.marketingListID,
			Where<CRMarketingList.isDynamic, IsNull, Or<CRMarketingList.isDynamic, NotEqual<True>>>>),
			DescriptionField = typeof(CRMarketingList.mailListCode))]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		protected virtual void CRMarketingListMember_MarketingListID_CacheAttached(PXCache sender)
		{

		}

		[PXDBDefault(typeof(Contact.contactID))]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		protected virtual void CRMarketingListMember_ContactID_CacheAttached(PXCache sender)
		{

		}

        protected virtual void CRMarketingListMember_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
        {
            CRMarketingListMember row = e.Row as CRMarketingListMember;

            if (row == null) return;

            CRMarketingList _CRMarketingList = PXSelect<CRMarketingList, Where<CRMarketingList.marketingListID,
                Equal<Required<CRMarketingList.marketingListID>>>>.Select(this, row.MarketingListID);
            if(_CRMarketingList != null)
            {
                PXUIFieldAttribute.SetEnabled<CRMarketingList.marketingListID>(sender, row, _CRMarketingList.IsDynamic == false);
            }
        }
        #endregion

        #region CRPMTimeActivity

        [PXDBChildIdentity(typeof(Contact.contactID))]
		[PXSelector(typeof(Contact.contactID), DescriptionField = typeof(Contact.memberName), DirtyRead = true)]
        [PXMergeAttributes(Method = MergeMethod.Merge)]
        protected virtual void CRPMTimeActivity_ContactID_CacheAttached(PXCache sender) { }

        [PXDBDefault(typeof(Contact.bAccountID), PersistingCheck = PXPersistingCheck.Nothing)]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		protected virtual void CRPMTimeActivity_BAccountID_CacheAttached(PXCache sender) { }

		#endregion

		#region CROpportunityClass
		[PXUIField(DisplayName = "Class Description")]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		protected virtual void CROpportunityClass_Description_CacheAttached(PXCache sender)
		{
		}
		#endregion

		#region CRRelation

		[PXDBChildIdentity(typeof(Contact.contactID))]
		[PXMergeAttributes(Method = MergeMethod.Merge)]
		protected virtual void _(Events.CacheAttached<CRRelation.contactID> e) { }

		#endregion

		#endregion

		#region Private Methods

		protected void CopyContactInfo(Contact dest, Contact src)
		{
			if (!string.IsNullOrEmpty(src.FaxType)) dest.FaxType = src.FaxType;
			if (!string.IsNullOrEmpty(src.Phone1Type)) dest.Phone1Type = src.Phone1Type;
			if (!string.IsNullOrEmpty(src.Phone2Type)) dest.Phone2Type = src.Phone2Type;
			if (!string.IsNullOrEmpty(src.Phone3Type)) dest.Phone3Type = src.Phone3Type;

			dest.Fax = src.Fax;
			dest.Phone1 = src.Phone1;
			dest.Phone2 = src.Phone2;
			dest.Phone3 = src.Phone3;
			dest.WebSite = src.WebSite;
			dest.EMail = src.EMail;
		}

		#endregion

		#region Extensions

		/// <exclude/>
		// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
		public class PrimaryContactGraphExt : PXGraphExtension<ContactMaint>
		{
			#region Events

			protected virtual void _(Events.RowUpdated<Contact> e)
			{
				var row = e.Row as Contact;
				if (row == null) return;

				BAccount acct = PXSelectorAttribute.Select<Contact.bAccountID>(e.Cache, row, row.BAccountID) as BAccount;
				if (acct == null) return;

				if (acct.PrimaryContactID == row.ContactID)
				{
					Base.Caches<BAccount>().MarkUpdated(acct);
				}
			}

			protected virtual void _(Events.FieldUpdated<Contact.bAccountID> e)
			{
				var row = e.Row as Contact;
				if (row == null) return;

				BAccount acct = PXSelectorAttribute.Select<Contact.bAccountID>(e.Cache, row, e.OldValue) as BAccount;
				if (acct == null) return;

				if (acct.PrimaryContactID == row.ContactID)
				{
					PXUIFieldAttribute.SetWarning<Contact.bAccountID>(e.Cache, row, Messages.PrimaryContactReassignment);
				}
				else
				{
					PXUIFieldAttribute.SetWarning<Contact.bAccountID>(e.Cache, row, null);
				}
			}

			protected virtual void _(Events.RowPersisted<Contact> e)
			{
				if (!(e.Row is Contact contact)
					|| e.TranStatus != PXTranStatus.Open
					|| !e.Operation.IsIn(PXDBOperation.Update, PXDBOperation.Delete))
					return;

				if (!(e.Cache.GetValueOriginal<Contact.bAccountID>(contact) is int oldBAID))
					return;

				if (e.Operation == PXDBOperation.Update && contact.BAccountID == oldBAID)
					return;

				// Acuminator disable once PX1043 SavingChangesInEventHandlers [Changes can be saved inside the open transaction]
				PXDatabase.Update<BAccount>(
					new PXDataFieldAssign<BAccount.primaryContactID>(null),
					new PXDataFieldRestrict<BAccount.bAccountID>(oldBAID)
				);
			}

			#endregion
		}

		/// <exclude/>
		// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
		public class DefaultContactOwnerGraphExt : CRDefaultDocumentOwner<
			ContactMaint, Contact,
			Contact.classID, Contact.ownerID, Contact.workgroupID>
		{ }

		/// <exclude/>
		public class CRDuplicateEntitiesForContactGraphExt : CRDuplicateEntities<ContactMaint, Contact>
		{
			#region Workflow
			
			public class Workflow : PXGraphExtension<CRDuplicateEntitiesForContactGraphExt, ContactWorkflow, ContactMaint>
			{
				public static bool IsActive()
				{
					return IsExtensionActive();
				}

				public override void Configure(PXScreenConfiguration configuration)
				{
					var context = configuration.GetScreenConfigurationContext<ContactMaint, Contact>();
					var categoryValidation = context.Categories.Get(ContactWorkflow.CategoryNames.Validation);

					configuration
						.GetScreenConfigurationContext<ContactMaint, Contact>()
						.UpdateScreenConfigurationFor(screen =>
						{
							return screen
								.WithActions(actions =>
								{
									// New Toolbar "Validation" folder
									actions.Add<CRDuplicateEntitiesForContactGraphExt>(e => e.CheckForDuplicates, a => a.WithCategory(categoryValidation));
									actions.Add<CRDuplicateEntitiesForContactGraphExt>(e => e.MarkAsValidated, a => a.WithCategory(categoryValidation));
									actions.Add<CRDuplicateEntitiesForContactGraphExt>(e => e.CloseAsDuplicate, a => a.WithCategory(categoryValidation));
									actions.Add<ContactAddressActions>(e => e.ValidateAddress, a => a.WithCategory(categoryValidation));
								});
						});
				}
			}

			#endregion

			#region Initialization

			public override Type AdditionalConditions => typeof(

				Brackets<
					// do not show BA, that is currently attached to the Contact
					DuplicateDocument.bAccountID.FromCurrent.IsNotNull
					.And<Brackets<
						DuplicateContact.contactType.IsEqual<ContactTypesAttribute.bAccountProperty>
						.And<BAccountR.bAccountID.IsNotEqual<DuplicateDocument.bAccountID.FromCurrent>>
						.Or<DuplicateContact.contactType.IsNotEqual<ContactTypesAttribute.bAccountProperty>>
					>>
					.Or<DuplicateDocument.bAccountID.FromCurrent.IsNull>
				>
				.And<Brackets<
					// all Contact inside the single related BA or without related BA
					DuplicateDocument.bAccountID.FromCurrent.IsNotNull
					.And<Brackets<
						// it's a Lead with same BA
						DuplicateContact.bAccountID.IsEqual<DuplicateDocument.bAccountID.FromCurrent>
						.And<DuplicateContact.contactType.IsEqual<ContactTypesAttribute.lead>>

						// it's a Lead with no BA
						.Or<DuplicateContact.bAccountID.IsNull
							.And<DuplicateContact.contactType.IsEqual<ContactTypesAttribute.lead>>>

						// it's a Contact or BA
						.Or<DuplicateContact.contactType.IsIn<ContactTypesAttribute.person, ContactTypesAttribute.bAccountProperty>>
					>>
					.Or<DuplicateDocument.bAccountID.FromCurrent.IsNull>
				>>
				.And<Brackets<
					// Leads that are not linked to the current Contact
					Standalone.CRLead.refContactID.IsNotEqual<DuplicateDocument.contactID.FromCurrent>
					.Or<Standalone.CRLead.refContactID.IsNull>
				>>
				.And<
					DuplicateContact.isActive.IsEqual<True>.And<DuplicateContact.contactType.IsNotEqual<ContactTypesAttribute.bAccountProperty>>
					.Or<BAccountR.status.IsNotEqual<CustomerStatus.inactive>>
				>
			);

			public override string WarningMessage => Messages.ContactHavePossibleDuplicates;

			public static bool IsActive()
			{
				return IsExtensionActive();
			}

			public override void Initialize()
			{
				base.Initialize();

				DuplicateDocuments = new PXSelectExtension<DuplicateDocument>(Base.ContactCurrent);
			}

			protected override DocumentMapping GetDocumentMapping()
			{
				return new DocumentMapping(typeof(Contact)) { Key = typeof(Contact.contactID) };
			}

			protected override DuplicateDocumentMapping GetDuplicateDocumentMapping()
			{
				return new DuplicateDocumentMapping(typeof(Contact)) { Email = typeof(Contact.eMail) };
			}

			#endregion

			#region Events

			protected virtual void _(Events.FieldUpdated<Contact, Contact.duplicateStatus> e)
			{
				Contact row = e.Row as Contact;
				if (e.Row == null || (string)e.OldValue == (string)e.NewValue)
					return;

				if (row.DuplicateStatus == DuplicateStatusAttribute.Duplicated)
				{
					row.Status = ContactStatus.Inactive;
				}
			}

			protected virtual void _(Events.FieldUpdated<Contact, Contact.isActive> e)
			{
				Contact row = e.Row as Contact;
				if (e.Row == null)
					return;

				if (row.IsActive == true && row.IsActive != (bool?)e.OldValue)
				{
					row.DuplicateStatus = DuplicateStatusAttribute.NotValidated;
				}
			}

			protected virtual void _(Events.RowSelected<CRDuplicateRecord> e)
			{
				var rec = e.Row;
				if (rec == null) return;

				if (rec.CanBeMerged != true)
				{
					DuplicatesForMerging.Cache.RaiseExceptionHandling<CRDuplicateRecord.canBeMerged>(rec, rec.CanBeMerged,
							new PXSetPropertyException(Messages.ContactAssocRecordDiff, PXErrorLevel.RowWarning));
				}
			}

			#endregion

			#region Overrides

			public override Contact GetTargetEntity(int targetID)
			{
				return PXSelect<Contact, Where<Contact.contactID, Equal<Required<Contact.contactID>>>>.Select(Base, targetID);
			}

			public override Contact GetTargetContact(Contact targetEntity)
			{
				return targetEntity as Contact;
			}

			public override Address GetTargetAddress(Contact targetEntity)
			{
				return PXSelect<Address, Where<Address.addressID, Equal<Required<Address.addressID>>>>.Select(Base, targetEntity.DefAddressID);
			}

			public override void GetAllProperties(List<FieldValue> values, HashSet<string> fieldNames)
			{
				int order = 0;

				values.AddRange(GetMarkedPropertiesOf<Contact>(Base, ref order).Where(fld => fieldNames.Add(fld.Name)));

				base.GetAllProperties(values, fieldNames);
			}

			protected override bool WhereMergingMet(CRDuplicateResult result)
			{
				var doc = DuplicateDocuments.Current;
				var duplicate = result.GetItem<CRDuplicateRecord>();

				if (duplicate == null)
					return false;

				bool isOfSameType = duplicate.DuplicateContactType == ContactTypesAttribute.Person;
				return isOfSameType;
			}

			protected override bool CanBeMerged(CRDuplicateResult result)
			{
				var doc = DuplicateDocuments.Current;
				var duplicate = result.GetItem<CRDuplicateRecord>();

				bool isOfSameParentOrRefContact = 
					duplicate != null
					&& (doc.RefContactID == null || duplicate.DuplicateRefContactID == null || duplicate.DuplicateRefContactID == doc.RefContactID)
					&& (doc.BAccountID == null || duplicate.DuplicateBAccountID == null || duplicate.DuplicateBAccountID == doc.BAccountID);
				return isOfSameParentOrRefContact;
			}

			public override void DoDuplicateAttach(DuplicateDocument duplicateDocument)
			{
				var duplicateRecord = DuplicatesForLinking.Cache.Current as CRDuplicateRecord;

				if (duplicateRecord == null)
					return;

				Contact duplicate = PXSelect<Contact,
						Where<Contact.contactID, Equal<Current<CRDuplicateRecord.duplicateContactID>>>>
					.SelectSingleBound(Base, new object[] { duplicateRecord });

				if (duplicate == null)
					return;

				if (duplicate.ContactType == ContactTypesAttribute.Lead)
				{
					var lead = CRLead.PK.Find(Base, duplicateRecord.DuplicateContactID);

					lead.BAccountID = null;
					Base.Leads.Cache.SetValueExt<CRLead.refContactID>(lead, duplicateDocument.ContactID);
					// need to update through leads cache (DuplicateDocuments is contacts cache)
					Base.Leads.Cache.Update(lead);
				}
				else if (duplicate.ContactType == ContactTypesAttribute.BAccountProperty)
				{
					duplicateDocument.BAccountID = duplicate.BAccountID;
					DuplicateDocuments.Update(duplicateDocument);
				}
				else
				{
					throw new PXException(Messages.AttachToAccountNotFound);
				}

			}

			#endregion
		}

		/// <exclude/>
		// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
		public class ContactBAccountSharedAddressOverrideGraphExt : SharedChildOverrideGraphExt<ContactMaint, ContactBAccountSharedAddressOverrideGraphExt>
		{
			#region Initialization

			protected override DocumentMapping GetDocumentMapping()
			{
				return new DocumentMapping(typeof(Contact))
				{
					RelatedID = typeof(Contact.bAccountID),
					ChildID = typeof(Contact.defAddressID),
					IsOverrideRelated = typeof(Contact.overrideAddress)
				};
			}

			protected override RelatedMapping GetRelatedMapping()
			{
				return new RelatedMapping(typeof(BAccount))
				{
					RelatedID = typeof(BAccount.bAccountID),
					ChildID = typeof(BAccount.defAddressID)
				};
			}

			protected override ChildMapping GetChildMapping()
			{
				return new ChildMapping(typeof(Address))
				{
					ChildID = typeof(Address.addressID),
					RelatedID = typeof(Address.bAccountID),
				};
			}

			#endregion

			#region Events

			protected void _(Events.RowSelected<Contact> e)
			{
				Contact row = e.Row as Contact;
				if (row == null)
					return;

				PXUIFieldAttribute.SetEnabled<Contact.overrideAddress>(e.Cache, row, row.ContactType == ContactTypesAttribute.Person);
			}

			protected override void _(Events.RowSelected<Child> e)
			{
				if (e.Row == null)
					return;

				var contact = Base.Contact.Current;
				if (contact == null)
					return;

				var account = PXSelectorAttribute.Select<Contact.bAccountID>(Base.Contact.Cache, contact) as BAccount;

				PXUIFieldAttribute.SetEnabled(e.Cache, e.Row, 
						contact.OverrideAddress == true
						|| account == null
						|| account.Type == BAccountType.ProspectType
					);
			}

			#endregion
		}

		/// <exclude/>
		// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
		public class ContactAddressActions : CRAddressActions<ContactMaint, Contact>
		{
			#region Initialization

			protected override ChildMapping GetChildMapping()
			{
				return new ChildMapping(typeof(Address))
				{
					ChildID = typeof(Address.addressID),
					RelatedID = typeof(Address.bAccountID)
				};
			}

			protected override DocumentMapping GetDocumentMapping()
			{
				return new DocumentMapping(typeof(Contact))
				{
					RelatedID = typeof(Contact.bAccountID),
					ChildID = typeof(Contact.defAddressID)
				};
			}

			#endregion
		}

		// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
		public class LinkLeadFromContactExt : LinkEntitiesExt_EventBased<ContactMaint, Contact, LinkFilter, CRLead, CRLead.refContactID>
		{
			#region Initialization

			public override string LeftValueDescription => Messages.Lead;
			public override string RightValueDescription => Messages.Contact;

			public PXFilter<LinkFilter> SelectContactForLink; // just dummy for link contact ext panel

			public override CRLead UpdatingEntityCurrent
			{
				get
				{
					if (Base.Caches<CRDuplicateRecordForLinking>().Current is CRDuplicateRecordForLinking rec
						&& rec.DuplicateContactID is int leadId)
					{
						if (UpdatingEntityCache.Current is CRLead lead && lead.ContactID == leadId)
							return lead;
						return CRLead.PK.Find(Base, leadId);
					}

					return null;
				}
			}

			#endregion

			#region Events

			[PXMergeAttributes(Method = MergeMethod.Append)]
			[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Sync with Lead")]
			protected virtual void _(Events.CacheAttached<LinkFilter.processLink> e) { }

			#endregion

			#region Overrides

			public override EntitiesContext GetLeftEntitiesContext()
			{
				var lead = UpdatingEntityCurrent;
				var address = Address.PK.Find(Base, lead.DefAddressID);
				return new EntitiesContext(Base,
					new EntityEntry(typeof(Contact), UpdatingEntityCache, lead),
					new EntityEntry(typeof(Address), Base.AddressCurrent.Cache, address));
			}

			public override EntitiesContext GetRightEntitiesContext()
			{
				return new EntitiesContext(Base,
					new EntityEntry(Base.Contact.Cache, Base.Contact.Current),
					new EntityEntry(Base.AddressCurrent.Cache, Base.AddressCurrent.SelectSingle()));
			}

			public override void UpdateMainAfterProcess()
			{
				UpdatingEntityCurrent.OverrideRefContact = Filter.Current.ProcessLink != true;
				base.UpdateMainAfterProcess();
			}

			public override void UpdateRightEntitiesContext(EntitiesContext context, IEnumerable<LinkComparisonRow> result) { }

			protected override object GetSelectedEntityID()
			{
				return Base.Contact.Current.ContactID;
			}

			#endregion
		}

		/// <exclude/>
		// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
		public class UpdateRelatedContactInfoFromContactGraphExt : CRUpdateRelatedContactInfoGraphExt<ContactMaint>
		{
			#region Events

			protected virtual void _(Events.RowPersisting<Contact> e)
			{
				if (e.Row == null)
					return;

				SetUpdateRelatedInfo(e, e.Cache.GetFields_ContactInfo().Union(new[] { nameof(CR.Contact.DefAddressID) }));
			}

			protected virtual void _(Events.RowPersisting<Address> e)
			{
				if (e.Row == null)
					return;

				SetUpdateRelatedInfo(e, e.Cache.GetFields_ContactInfo());
			}

			protected virtual void _(Events.RowPersisting<CRLead> e)
			{
				if (e.Row == null)
					return;

				SetUpdateRelatedInfo<CRLead, CRLead.refContactID>(e);
			}

			protected virtual void _(Events.RowPersisted<Contact> e)
			{
				var row = e.Row;
				if (row == null
					|| UpdateRelatedInfo != true
					|| e.TranStatus != PXTranStatus.Open
					|| e.Operation.Command() != PXDBOperation.Update)
					return;

				// Acuminator disable once PX1043 SavingChangesInEventHandlers [ISV]
				UpdateContact(e.Cache, row,
					new SelectFrom<Contact>
						.LeftJoin<Standalone.CRLead>
							.On<Standalone.CRLead.contactID.IsEqual<Contact.contactID>>
						.Where<
							// Leads that are linked to the same Contact
							Standalone.CRLead.refContactID.IsEqual<@P.AsInt>
							.And<Standalone.CRLead.overrideRefContact.IsEqual<False>>>
					.View(Base),
					row.ContactID);
			}

			protected virtual void _(Events.RowPersisted<Address> e)
			{
				var row = e.Row;
				if (row == null
					|| UpdateRelatedInfo != true
					|| e.TranStatus != PXTranStatus.Open
					|| e.Operation.Command().IsNotIn(PXDBOperation.Update, PXDBOperation.Insert))
					return;

				Contact contact = Base.Contact.Current ?? PXSelect<
							Contact,
						Where<
							Contact.defAddressID, Equal<@P.AsInt>,
							And<Contact.contactType, Equal<ContactTypesAttribute.person>>>>
					.Select(Base, row.AddressID);

				if (contact == null)
					return;

				// Acuminator disable once PX1043 SavingChangesInEventHandlers [ISV]
				UpdateAddress(e.Cache, row,
					new SelectFrom<Address>
						.InnerJoin<Contact>
							.On<Contact.defAddressID.IsEqual<Address.addressID>>
						.LeftJoin<Standalone.CRLead>
							.On<Standalone.CRLead.contactID.IsEqual<Contact.contactID>>
						.Where<
							// Leads that are linked to the same Contact
							Standalone.CRLead.refContactID.IsEqual<@P.AsInt>
							.And<Standalone.CRLead.overrideRefContact.IsEqual<False>>>
					.View(Base),
					contact.ContactID);
			}

			#endregion
		}

		/// <exclude/>
		// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
		public class CreateLeadFromContactGraphExt : CRCreateLeadAction<ContactMaint, Contact>
		{
			#region Initialization

			public override void Initialize()
			{
				base.Initialize();

				Addresses = new PXSelectExtension<CR.Extensions.CRCreateActions.DocumentAddress>(Base.AddressCurrent);
				Contacts = new PXSelectExtension<CR.Extensions.CRCreateActions.DocumentContact>(Base.ContactCurrent);
			}

			protected override DocumentContactMapping GetDocumentContactMapping()
			{
				return new DocumentContactMapping(typeof(Contact)) { Email = typeof(Contact.eMail) };
			}
			protected override DocumentAddressMapping GetDocumentAddressMapping()
			{
				return new DocumentAddressMapping(typeof(Address));
			}

			#endregion

			#region Overrides

			public override Contact GetCurrentMain(params object[] pars)
			{
				Contact contact = PXSelect<
						Contact,
					Where<
						Contact.contactID, Equal<Required<Contact.contactID>>>>
					.SelectSingleBound(Base, null, pars);

				return contact ?? Base.ContactCurrent.Current;
			}

			#endregion
		}

		/// <exclude/>
		// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
		public class CreateAccountFromContactGraphExt : CRCreateAccountAction<ContactMaint, Contact>
		{
			#region State

			protected override string TargetType => CRTargetEntityType.Contact;

			protected override PXSelectBase<CRPMTimeActivity> Activities => Base.Activities;

			#endregion

			#region Initialization

			public override void Initialize()
			{
				base.Initialize();

				Addresses = new PXSelectExtension<CR.Extensions.CRCreateActions.DocumentAddress>(Base.AddressCurrent);
				Contacts = new PXSelectExtension<CR.Extensions.CRCreateActions.DocumentContact>(Base.ContactCurrent);
			}

			protected override DocumentContactMapping GetDocumentContactMapping()
			{
				return new DocumentContactMapping(typeof(Contact)) { Email = typeof(Contact.eMail) };
			}
			protected override DocumentAddressMapping GetDocumentAddressMapping()
			{
				return new DocumentAddressMapping(typeof(Address));
			}

			#endregion

			#region Events

			protected virtual void _(Events.FieldDefaulting<AccountsFilter, AccountsFilter.accountClass> e)
			{
				if (ExistingAccount.SelectSingle() is BAccount existingAccount)
				{
					e.NewValue = existingAccount.ClassID;
					e.Cancel = true;
					return;
				}

				Contact contact = Base.Contact.Current;
				if (contact == null) return;

				CRContactClass cls = PXSelect<
						CRContactClass,
					Where<
						CRContactClass.classID, Equal<Required<Contact.classID>>>>
					.Select(Base, contact.ClassID);

				if (cls?.TargetBAccountClassID != null)
				{
					e.NewValue = cls.TargetBAccountClassID;
				}
				else
				{
					e.NewValue = Base.Setup.Current?.DefaultCustomerClassID;
				}

				e.Cancel = true;
			}

			protected override void _(Events.RowSelected<AccountsFilter> e)
			{
				base._(e);

				e.Cache.AdjustUI(e.Row)
					.For<AccountsFilter.linkContactToAccount>(_ => _.Visible = false);
			}

			#endregion

			#region Methods

			protected override void MapAddress(CR.Extensions.CRCreateActions.DocumentAddress docAddress, BAccount account, ref Address address)
			{
				// set address to account as is from contact
				// no need to check in release, should work properly, just to ensure
				System.Diagnostics.Debug.Assert(Base.Caches<Contact>().Current != null,
					"Random address will be used, there is no contact in currents");
				address = Base.AddressCurrent.View.SelectSingle() as Address ?? address;

				base.MapAddress(docAddress, account, ref address);
				account.DefAddressID = address.AddressID;
				address.BAccountID = account.BAccountID;
			}

			#endregion
		}

		/// <exclude/>
		// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
		public class ContactMaintAddressLookupExtension : CR.Extensions.AddressLookupExtension<ContactMaint, Contact, Address>
		{
			protected override string AddressView => nameof(Base.AddressCurrent);
		}

		#endregion
	}
}
