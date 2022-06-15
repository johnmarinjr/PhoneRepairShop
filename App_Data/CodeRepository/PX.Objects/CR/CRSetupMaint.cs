using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using PX.Common;
using PX.Data;
using PX.Objects.CR.MassProcess;
using PX.Objects.CS;
using PX.Objects.CR.DAC;
using PX.Data.BQL.Fluent;

namespace PX.Objects.CR
{
	public class CRSetupMaint : PXGraph<CRSetupMaint>
	{
		public PXSave<CRSetup> Save;
		public PXCancel<CRSetup> Cancel;
		public PXSelect<CRSetup> CRSetupRecord;

        public CRNotificationSetupList<CRNotification> Notifications;
        public PXSelect<NotificationSetupRecipient,
            Where<NotificationSetupRecipient.setupID, Equal<Current<CRNotification.setupID>>>> Recipients;

		public SelectFrom<CRValidation>.View Validations;

        public PXSelect<CRCampaignType> CampaignType;

        #region CacheAttached
        [PXDBString(10)]
        [PXDefault]
        [CRMContactType.List]
        [PXUIField(DisplayName = "Contact Type")]
        [PXCheckUnique(typeof(NotificationSetupRecipient.contactID),
            Where = typeof(Where<NotificationSetupRecipient.setupID, Equal<Current<NotificationSetupRecipient.setupID>>>))]
        public virtual void NotificationSetupRecipient_ContactType_CacheAttached(PXCache sender)
        {
        }
        [PXDBInt]
        [PXUIField(DisplayName = "Contact ID")]
        [PXNotificationContactSelector(typeof(NotificationSetupRecipient.contactType))]
        public virtual void NotificationSetupRecipient_ContactID_CacheAttached(PXCache sender)
        {
        }

        #endregion

        #region Event Handlers

        protected virtual void _(Events.RowSelected<CRSetup> e)
		{
			bool multicurrencyFeatureInstalled = PXAccess.FeatureInstalled<FeaturesSet.multicurrency>();

			PXUIFieldAttribute.SetVisible<CRSetup.defaultCuryID>(e.Cache, null, multicurrencyFeatureInstalled);
			PXUIFieldAttribute.SetVisible<CRSetup.defaultRateTypeID>(e.Cache, null, multicurrencyFeatureInstalled);
			PXUIFieldAttribute.SetVisible<CRSetup.allowOverrideCury>(e.Cache, null, multicurrencyFeatureInstalled);
			PXUIFieldAttribute.SetVisible<CRSetup.allowOverrideRate>(e.Cache, null, multicurrencyFeatureInstalled);
		}

		protected virtual void _(Events.RowPersisting<CRValidation> e)
		{
			if (e.Row == null)
				return;

			if (e.Row.GramValidationDateTime == null)
			{
				e.Row.GramValidationDateTime = PXTimeZoneInfo.Now;
			}
		}

		protected virtual void _(Events.FieldUpdated<CRSetup.duplicateScoresNormalization> e)
		{
			if (e.NewValue is bool newValue && !newValue.Equals(e.OldValue))
			{
				UpdateGramValidationDate();
			}
		}

		#endregion

		#region Methods

		private void UpdateGramValidationDate()
		{
			foreach (CRValidation validation in Validations.Select())
			{
				validation.GramValidationDateTime = null;
				Validations.Update(validation);
			}
		}

		#endregion

		#region Extensions

		public class GramRecalculationExt : Extensions.CRDuplicateEntities.CRGramRecalculationExt<CRSetupMaint>
		{
			public static bool IsActive() => IsFeatureActive();
		}

		#endregion
	}
}
