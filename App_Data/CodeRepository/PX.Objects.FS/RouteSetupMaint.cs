using PX.Common;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.EP;
using System;

namespace PX.Objects.FS
{
    public class RouteSetupMaint : PXGraph<RouteSetupMaint>
    {
        [PXHidden]
        public PXSelect<FSSetup> SetupRecord;
        public PXSelect<FSRouteSetup> RouteSetupRecord;

        public PXSave<FSRouteSetup> Save;
        public PXCancel<FSRouteSetup> Cancel;

		public CRNotificationSetupList<FSCTNotification> Notifications;
		public PXSelect<NotificationSetupRecipient,
			   Where<
				   NotificationSetupRecipient.setupID, Equal<Current<FSCTNotification.setupID>>>> Recipients;

		public RouteSetupMaint()
            : base()
        {
            FSSetup setup = PXSelectReadonly<FSSetup>.Select(this);
            if (setup == null)
            {
                throw new PXSetupNotEnteredException(ErrorMessages.SetupNotEntered, typeof(FSSetup), DACHelper.GetDisplayName(typeof(FSSetup)));
            }
        }

		#region CacheAttached
		#region NotificationSetupRecipient_ContactType
		[PXDBString(10)]
		[PXDefault]
		[ContractContactType.ClassList]
		[PXUIField(DisplayName = "Contact Type")]
		[PXCheckUnique(typeof(NotificationSetupRecipient.contactID),
			Where = typeof(Where<NotificationSetupRecipient.setupID, Equal<Current<NotificationSetupRecipient.setupID>>>))]
		public virtual void NotificationSetupRecipient_ContactType_CacheAttached(PXCache sender)
		{
		}
		#endregion
		#region NotificationSetupRecipient_ContactID
		[PXDBInt]
		[PXUIField(DisplayName = "Contact ID")]
		[PXNotificationContactSelector(typeof(NotificationSetupRecipient.contactType),
			typeof(
			Search2<Contact.contactID,
			LeftJoin<EPEmployee,
				On<EPEmployee.parentBAccountID, Equal<Contact.bAccountID>,
				And<EPEmployee.defContactID, Equal<Contact.contactID>>>>,
			Where<
				Current<NotificationSetupRecipient.contactType>, Equal<NotificationContactType.employee>,
				And<EPEmployee.acctCD, IsNotNull>>>))]
		public virtual void NotificationSetupRecipient_ContactID_CacheAttached(PXCache sender)
		{
		}
		#endregion
		#endregion

		#region Event Handlers

		#region FSRouteSetup

		#region FieldSelecting
		#endregion
		#region FieldDefaulting
		#endregion
		#region FieldUpdating
		#endregion
		#region FieldVerifying
		#endregion
		#region FieldUpdated
		#endregion

		protected virtual void _(Events.RowSelecting<FSRouteSetup> e)
        {
        }

        protected virtual void _(Events.RowSelected<FSRouteSetup> e)
        {
            if (e.Row == null)
            {
                return;
            }

            if (SetupRecord.Current == null)
            {
                SetupRecord.Current = SetupRecord.Select();
            }
        }

        protected virtual void _(Events.RowInserting<FSRouteSetup> e)
        {
        }

        protected virtual void _(Events.RowInserted<FSRouteSetup> e)
        {
        }

        protected virtual void _(Events.RowUpdating<FSRouteSetup> e)
        {
        }

        protected virtual void _(Events.RowUpdated<FSRouteSetup> e)
        {
        }

        protected virtual void _(Events.RowDeleting<FSRouteSetup> e)
        {
        }

        protected virtual void _(Events.RowDeleted<FSRouteSetup> e)
        {
        }

        protected virtual void _(Events.RowPersisting<FSRouteSetup> e)
        {
        }

        protected virtual void _(Events.RowPersisted<FSRouteSetup> e)
        {
        }

        #endregion

        #region FSSetup

        #region FieldSelecting
        #endregion
        #region FieldDefaulting
        #endregion
        #region FieldUpdating
        #endregion
        #region FieldVerifying
        #endregion
        #region FieldUpdated

        protected virtual void _(Events.FieldUpdated<FSSetup, FSSetup.contractPostTo> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSSetup fsSetupRow = (FSSetup)e.Row;

            if (fsSetupRow.ContractPostTo != (string)e.OldValue)
            {
                SharedFunctions.ValidatePostToByFeatures<FSSetup.contractPostTo>(e.Cache, fsSetupRow, fsSetupRow.ContractPostTo);
            }
        }

        #endregion

        protected virtual void _(Events.RowSelecting<FSSetup> e)
        {
        }

        protected virtual void _(Events.RowSelected<FSSetup> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSSetup fsSetupRow = (FSSetup)e.Row;
            PXCache cache = e.Cache;

            EquipmentSetupMaint.EnableDisable_Document(cache, fsSetupRow);
            SharedFunctions.ValidatePostToByFeatures<FSSetup.contractPostTo>(cache, fsSetupRow, fsSetupRow.ContractPostTo);
            FSPostTo.SetLineTypeList<FSSetup.contractPostTo>(e.Cache, e.Row);
        }

        protected virtual void _(Events.RowInserting<FSSetup> e)
        {
        }

        protected virtual void _(Events.RowInserted<FSSetup> e)
        {
        }

        protected virtual void _(Events.RowUpdating<FSSetup> e)
        {
        }

        protected virtual void _(Events.RowUpdated<FSSetup> e)
        {
        }

        protected virtual void _(Events.RowDeleting<FSSetup> e)
        {
        }

        protected virtual void _(Events.RowDeleted<FSSetup> e)
        {
        }

        protected virtual void _(Events.RowPersisting<FSSetup> e)
        {
            if (e.Row == null)
            {
                return;
            }

            FSSetup fsSetupRow = (FSSetup)e.Row;

            if (fsSetupRow.ContractPostTo == ID.Contract_PostTo.SALES_ORDER_INVOICE)
            {
                e.Cache.RaiseExceptionHandling<FSSetup.contractPostTo>(fsSetupRow, null, new PXSetPropertyException(TX.Error.SOINVOICE_FROM_CONTRACT_NOT_IMPLEMENTED, PXErrorLevel.Error));
            }

            SharedFunctions.ValidatePostToByFeatures<FSSetup.contractPostTo>(e.Cache, fsSetupRow, fsSetupRow.ContractPostTo);
        }

        protected virtual void _(Events.RowPersisted<FSSetup> e)
        {
        }
        #endregion

        #endregion
    }
}
