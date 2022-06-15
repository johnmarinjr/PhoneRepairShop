using PX.Data;
using System;
using PX.Data.BQL;
using PX.Objects.CR.Extensions;
using PX.Objects.CR.Extensions.CRCreateActions;
using PX.Objects.CR.Wizard;

namespace PX.Objects.CR.LeadMaint_Extensions
{
	/// <summary>
	/// An extension that you can use to link <see cref="BAccount"/> with the current <see cref="CRLead"/>.
	/// </summary>
	// Acuminator disable once PX1016 ExtensionDoesNotDeclareIsActiveMethod extension should be constantly active
	public class LeadMaint_LinkAccountExt : PXGraphExtension<LeadMaint>
	{
		#region DACs

		/// <summary>
		/// The filter that is used for linking <see cref="BAccount"/> with the current <see cref="CRLead"/>.
		/// </summary>
		[PXHidden]
		public class LinkAccountFilter : IBqlTable
		{
			public const int NotSetValue = 0;

			#region LinkAccountOption

			/// <summary>
			/// The field that specifies how <see cref="BAccount"/> is linked with the current <see cref="CRLead"/>:
			/// with <see cref="BAccount"/>'s <see cref="Contact"/>, without <see cref="Contact"/>,
			/// or with a new <see cref="Contact"/>.
			/// </summary>
			/// <value>
			/// The field can have one of the values described in <see cref="linkAccountOption.List"/>.
			/// </value>
			[PXInt]
			[PXUIField]
			[PXUnboundDefault(linkAccountOption.LinkAccount)]
			[linkAccountOption.List]
			public int? LinkAccountOption { get; set; }

			public abstract class linkAccountOption : BqlInt.Field<linkAccountOption>
			{
				public const int LinkAccount = 0;
				public const int SelectContact = 1;
				public const int CreateContact = 2;

				public class List : PXIntListAttribute
				{
					public List() : base(
						(LinkAccount,   "Associate the Lead with an Account"),
						(SelectContact, "Associate the Lead with an Account and a Contact"),
						(CreateContact, "Associate the Lead with an Account and a New Contact")
					)
					{ }
				}
			}

			#endregion

			#region WithoutContactOption

			/// <summary>
			/// The field that specifies whether the <see cref="BAccount"/> settings override the <see cref="CRLead"/> settings.
			/// </summary>
			/// <value>
			/// The field can have one of the values described in <see cref="withoutContactOption.List"/>.
			/// </value>
			[PXInt]
			[PXUIField]
			[PXUnboundDefault(withoutContactOption.Link)]
			[withoutContactOption.List]
			public int? WithoutContactOption { get; set; }
			public abstract class withoutContactOption : BqlInt.Field<withoutContactOption>
			{
				public const int Link = 0;
				public const int LinkAndReplace = 1;

				public class List : PXIntListAttribute
				{
					public List() : base(
						(Link,           "Do Not Update the Lead Settings"),
						(LinkAndReplace, "Replace the Lead Settings with the Account Settings")
					)
					{ }
				}
			}

			#endregion


			#region NewContactID

			/// <summary>
			/// The ID of <see cref="Contact"/> that should be linked with the current <see cref="CRLead"/>
			/// along with <see cref="BAccount"/>.
			/// </summary>
			/// <value>
			/// The value of this field corresponds to the <see cref="Contact.ContactID"/>.
			/// </value>
			[PXInt]
			[PXUnboundDefault(NotSetValue)]
			public int? NewContactID { get; set; }
			public abstract class newContactID : BqlInt.Field<newContactID> { }

			#endregion

			#region OldContactID

			/// <summary>
			/// The ID of <see cref="Contact"/> that was linked with the current <see cref="CRLead"/>
			/// before the smart panel was opened.
			/// </summary>
			/// <value>
			/// The value of this field corresponds to the <see cref="Contact.ContactID"/>.
			/// </value>
			[PXInt]
			[PXUnboundDefault(NotSetValue)]
			public int? OldContactID { get; set; }
			public abstract class oldContactID : BqlInt.Field<oldContactID> { }

			#endregion


			#region NewBAccountID

			/// <summary>
			/// The ID of <see cref="BAccount"/> that should be linked with the current <see cref="CRLead"/>.
			/// </summary>
			/// <value>
			/// The value of this field corresponds to the <see cref="BAccount.BAccountID"/>.
			/// </value>
			[PXInt]
			[PXUnboundDefault(NotSetValue)]
			public int? NewBAccountID { get; set; }
			public abstract class newBAccountID : BqlInt.Field<newBAccountID> { }

			#endregion

			#region OldBAccountID

			/// <summary>
			/// The ID of <see cref="BAccount"/> that was linked with the current <see cref="CRLead"/>
			/// before the smart panel was opened.
			/// </summary>
			/// <value>
			/// The value of this field corresponds to the <see cref="BAccount.BAccountID"/>.
			/// </value>
			[PXInt]
			[PXUnboundDefault(NotSetValue)]
			public int? OldBAccountID { get; set; }
			public abstract class oldBAccountID : BqlInt.Field<oldBAccountID> { }

			#endregion

			#region IsWizardOpen

			/// <summary>
			/// The service field that specifies that the link account wizard is currently open.
			/// </summary>
			/// <remarks>
			/// This field is required for proper work of the Back and Cancel buttons.
			/// </remarks>
			[PXBool]
			[PXUnboundDefault(false)]
			public bool? IsWizardOpen { get; set; }

			public abstract class isWizardOpen : BqlBool.Field<isWizardOpen> { }
			#endregion

			#region InsideCreateContact

			/// <summary>
			/// The service field that shows that the Create Contact dialog is currently open.
			/// </summary>
			/// <remarks>
			/// This field is required for proper navigation between the main screen of the wizard
			/// and the Create Contact panel (see <see cref="CRCreateContactAction{TGraph,TMain}"/>).
			/// </remarks>
			[PXBool]
			[PXUnboundDefault(false)]
			public bool? InsideCreateContact { get; set; }

			public abstract class insideCreateContact : BqlBool.Field<insideCreateContact> { }
			#endregion

		}

		#endregion

		#region Filters

		public PXFilter<LinkAccountFilter> LinkAccount;
		public PXFilter<LinkAccountFilter> LinkAccountWithoutContact;

		#endregion

		#region Events

		protected virtual void _(Events.RowUpdated<CRLead> e, PXRowUpdated del)
		{
			PreventRecursionCall.Execute(() =>
			{
				try
				{
					bool shouldProcess = false;
					if (e.Row != null
						&& ShouldProcess(e.Cache, e.Row, e.OldRow)
						&& !Base.UnattendedMode
						&& !Base.IsCopyPasteContext)
					{
						// Acuminator disable once PX1071 PXActionExecutionInEventHandlers
						ProcessFirstStep(e.Cache, e.Row, e.OldRow);
						shouldProcess = true;
					}

					try
					{
						del?.Invoke(e.Cache, e.Args); // -> LinkerExt
					}
					catch (CRWizardAbortException)
					{
						AbortCurrentValueChange();
					}

					if (shouldProcess)
						EnsurePopupPanelWillBeShown();
				}
				catch (PXDialogRequiredException)
				{
					StoreChangedData(
						e.OldRow,
						newContactID: e.Row.RefContactID,
						newBAccountID: e.Row.BAccountID);

					throw;
				}
			});
		}

		protected virtual void _(Events.RowUpdated<LinkAccountFilter> e)
		{
			e.Cache.IsDirty = false;
		}

		protected virtual void _(Events.FieldDefaulting<
			Extensions.CRCreateActions.ContactFilter,
			Extensions.CRCreateActions.ContactFilter.fullName> e,
			PXFieldDefaulting del)
		{
			if (e.Row != null
				&& LinkAccount.Current.NewBAccountID is int baccountId
				&& baccountId != LinkAccountFilter.NotSetValue)
			{
				var baccount = BAccount.PK.Find(Base, LinkAccount.Current.NewBAccountID);
				if (baccount != null)
				{
					e.NewValue = baccount.AcctName;
					return;
				}
			}
			del(e.Cache, e.Args);
		}

		#endregion

		#region Helpers

		/// <summary>
		/// Throws <see cref="PXDialogRequiredException"/>
		/// </summary>
		public virtual void ProcessFirstStep(PXCache cache, CRLead row, CRLead oldRow)
		{
			using (new WizardScope())
			{
				switch (AskOnValueChange())
				{
					case WebDialogResult.None:

						AskOnValueChange();

						break;

					case WebDialogResult.OK:
					case WebDialogResult.Yes:	// Next

						try
						{
							switch (LinkAccount.Current.LinkAccountOption)
							{
								case LinkAccountFilter.linkAccountOption.LinkAccount:

									ProcessSecondStep(cache, row, oldRow);

									return;

								case LinkAccountFilter.linkAccountOption.SelectContact:

									try
									{
										Base.GetExtension<LeadMaint_LinkContactExt>().SelectEntityForLinkAsk();
									}
									catch (CRWizardBackException)
									{
										LinkAccount.View.Answer = WebDialogResult.None;

										throw;
									}

									// just after the successfull Associate, LinkerExt will be executed. Need to stub.
									oldRow.RefContactID = row.RefContactID;

									return;

								case LinkAccountFilter.linkAccountOption.CreateContact:

									var ext = Base.GetExtension<LeadMaint.CreateContactFromLeadGraphExt>();

									// as graph was persisted inside CreateContact, need to rollback changes so it won't go to DB
									if (ext.PopupValidator.Filter.View.Answer == WebDialogResult.None)
									{
										if (LinkAccount.Current.InsideCreateContact is true)
											return;

										Base.Lead.Cache.SetValue<CRLead.refContactID>(row, LinkAccount.Current.OldContactID);
										Base.Lead.Cache.SetValue<CRLead.bAccountID>(row, LinkAccount.Current.OldBAccountID);
										LinkAccount.Current.InsideCreateContact = true;
									}

									try
									{
										// Acuminator disable once PX1071 PXActionExecutionInEventHandlers
										ext.CreateContact.Press();
									}
									// as graph was persisted inside CreateContact, need to re-set the cleared answer back
									catch (PXDialogRequiredException)
									{
										LinkAccount.View.Answer = WebDialogResult.Yes;
										throw;
									}
									catch (CRWizardException)
									{
										LinkAccount.Current.InsideCreateContact = false;
										throw;
									}
									finally
									{
										// revert rollback
										if (ext.PopupValidator.Filter.View.Answer == WebDialogResult.None)
										{
											Base.Lead.Cache.SetValue<CRLead.refContactID>(row, LinkAccount.Current.NewContactID);
											Base.Lead.Cache.SetValue<CRLead.bAccountID>(row, LinkAccount.Current.NewBAccountID);
										}
									}

									LinkAccount.Current.InsideCreateContact = false;
									row.OverrideRefContact = false;

									return;
							}
						}
						catch (CRWizardBackException)
						{
							LinkAccount.View.Answer = WebDialogResult.None;

							goto case WebDialogResult.None;
						}
						catch (CRWizardAbortException)
						{
							goto case WizardResult.Abort;
						}
						catch (PXBaseRedirectException)
						{
							throw;
						}
						catch (Exception ex)
						{
							PXTrace.WriteError(ex);
							// exception have to be wrapped into PXBaseRedirectException
							// to be displayed in the UI in ExecuteUpdate
							throw new CRWrappedRedirectException(ex);
						}

						break;

					case WizardResult.Abort:

						AbortValueChange(row);

						break;
				}
			}
		}

		/// <summary>
		/// Throws <see cref="PXDialogRequiredException"/>, <see cref="CRWizardBackException"/>, <see cref="CRWizardAbortException"/>
		/// </summary>
		public virtual void ProcessSecondStep(PXCache cache, CRLead row, CRLead oldRow)
		{
			var answer = LinkAccountWithoutContact
				.WithActionIfNoAnswerFor(true, () =>
				{
					if (LinkAccountWithoutContact.Current != null)
						LinkAccountWithoutContact.Current.WithoutContactOption = LinkAccountFilter.withoutContactOption.Link;
				})
				.WithAnswerForImport(WebDialogResult.Yes)
				.WithAnswerForMobile(WebDialogResult.Yes)
				.WithAnswerForCbApi(WebDialogResult.Yes)
				.AskExt();

			switch (answer)
			{
				case WebDialogResult.Yes:   // Finish
					ProcessLinkAccountWithoutContact();
					ResetStoredFilterValues();
					break;

				case WizardResult.Back:
					ClearAnswers();
					throw new CRWizardBackException();

				case WizardResult.Abort:
					throw new CRWizardAbortException();
			}
		}

		public virtual void ProcessLinkAccountWithoutContact()
		{
			var sharedExt = Base.GetExtension<LeadMaint.LeadBAccountSharedAddressOverrideGraphExt>();
			var updateExt = Base.GetExtension<LeadMaint.UpdateRelatedContactInfoFromLeadGraphExt>();

			bool overrideRefContact = LinkAccount.Current.WithoutContactOption != LinkAccountFilter.withoutContactOption.LinkAndReplace;

			CRLead lead = Base.Lead.Current;

			lead.OverrideRefContact = lead.OverrideAddress = overrideRefContact;
			sharedExt.UpdateRelatedOnBAccountIDChange(lead, LinkAccount.Current.OldBAccountID);

			if (lead.OverrideRefContact != true)
			{
				var baccount = BAccount.PK.Find(Base, lead.BAccountID);
				var baccountContact = BAccount.FK.ContactInfo.FindParent(Base, baccount);
				var baccountAddress = BAccount.FK.Address.FindParent(Base, baccount);
				updateExt.UpdateFieldsValuesWithoutPersist(
					new PXResult<Contact, Address>(baccountContact, baccountAddress),
					new PXResult<Contact, Address>(lead, Base.AddressCurrent.SelectSingle()));
			}
		}

		protected virtual void ClearAnswers()
		{
			LinkAccount.View.Answer = WebDialogResult.None;

			LinkAccountWithoutContact.View.Answer = WebDialogResult.None;
		}

		protected virtual bool ShouldProcess(PXCache cache, CRLead row, CRLead oldRow)
		{
			var atomicAction = Base.IsImport || Base.IsMobile || Base.IsContractBasedAPI;

			return
				atomicAction && ValueChanged(cache, row, oldRow)
				|| !atomicAction && (LinkAccount.View.Answer != WebDialogResult.None
									|| ValueChanged(cache, row, oldRow));
		}

		protected virtual bool ValueChanged(PXCache cache, CRLead row, CRLead oldRow)
		{
			return
				row.RefContactID == null
				&& row.BAccountID != null
				&& row.BAccountID != oldRow.BAccountID;
		}

		public virtual void StoreChangedData(CRLead lead, int? newContactID, int? newBAccountID)
		{
			if (LinkAccount.Current == null)
			{
				LinkAccount.Cache.Clear();
				LinkAccount.Current = LinkAccount.Insert();
			}

			if (LinkAccount.Current.OldBAccountID == LinkAccountFilter.NotSetValue)
			{
				LinkAccount.Current.OldContactID = lead.RefContactID;
				LinkAccount.Current.NewContactID = newContactID;

				LinkAccount.Current.OldBAccountID = lead.BAccountID;
				LinkAccount.Current.NewBAccountID = newBAccountID;
			}
		}

		public virtual void ResetStoredFilterValues()
		{
			if (LinkAccount.Current == null)
				return;

			var filter = LinkAccount.Current;

			filter.OldContactID = LinkAccountFilter.NotSetValue;
			filter.NewContactID = LinkAccountFilter.NotSetValue;

			filter.OldBAccountID = LinkAccountFilter.NotSetValue;
			filter.NewBAccountID = LinkAccountFilter.NotSetValue;
		}

		public virtual WebDialogResult AskOnValueChange()
		{
			LinkAccount.Current.IsWizardOpen = true;

			return LinkAccount
				.WithActionIfNoAnswerFor(Base.IsImport || Base.IsMobile || Base.IsContractBasedAPI, () =>
				{
					LinkAccount.Current.LinkAccountOption = LinkAccountFilter.linkAccountOption.LinkAccount;
				})
				.WithAnswerForImport(WebDialogResult.Yes)
				.WithAnswerForMobile(WebDialogResult.Yes)
				.WithAnswerForCbApi(WebDialogResult.Yes)
				.AskExt();
		}

		public virtual void AbortCurrentValueChange() => AbortValueChange(Base.Lead.Current);

		public virtual void AbortValueChange(CRLead lead)
		{
			lead.RefContactID = LinkAccount.Current.OldContactID != LinkAccountFilter.NotSetValue
				? LinkAccount.Current.OldContactID
				: null;

			lead.BAccountID = LinkAccount.Current.OldBAccountID != LinkAccountFilter.NotSetValue
				? LinkAccount.Current.OldBAccountID
				: null;

			LinkAccount.Current.IsWizardOpen = false;
			LinkAccount.Current.InsideCreateContact = false;

			LinkAccount.View.Answer = WebDialogResult.None;

			ResetStoredFilterValues();

			LinkAccount.Reset();
		}

		// in normal situation all closes of wizard must trigger clearing filter
		public virtual bool IsWizardOpen() => LinkAccount.Current.IsWizardOpen is true;

		public virtual void EnsurePopupPanelWillBeShown()
		{
			if (string.IsNullOrEmpty(PopupNoteManager.Message))
			{
				object baccountID = Base.Lead.Current.BAccountID;
				Base.Lead.Cache.RaiseFieldVerifying<CRLead.bAccountID>(Base.Lead.Current, ref baccountID);
			}
		}

		#endregion
	}
}
