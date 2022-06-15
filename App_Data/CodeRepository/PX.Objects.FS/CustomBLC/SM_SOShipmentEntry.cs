using PX.Data;
using PX.Objects.CS;
using PX.Objects.SO;
using System;
using System.Linq;

namespace PX.Objects.FS
{
    public class SM_SOShipmentEntry : PXGraphExtension<SOShipmentEntry>
    {
        public static bool IsActive()
        {
            return PXAccess.FeatureInstalled<FeaturesSet.serviceManagementModule>();
        }

        public delegate void CreateShipmentDelegate(CreateShipmentArgs args);

        /// <summary>
        /// Overrides <see cref="SOShipmentEntry.CreateShipment(CreateShipmentArgs)"/>
        /// </summary>
        [PXOverride]
        public virtual void CreateShipment(CreateShipmentArgs args, CreateShipmentDelegate del)
        {
            ValidatePostBatchStatus(PXDBOperation.Update, ID.Batch_PostTo.SO, args.Order.OrderType, args.Order.RefNbr);
            del(args);
        }

		#region Event handlers

		protected virtual void _(PX.Data.Events.RowSelecting<SOShipment> e)
		{
			SOShipment row = (SOShipment)e.Row;

			if (row == null)
				return;

			FSxSOShipment ext = e.Row.GetExtension<FSxSOShipment>();

			using (new PXConnectionScope())
			{
				ext.IsFSRelated = PXSelectJoin<SOOrderType,
										InnerJoin<SOOrderShipment, On<SOOrderType.orderType, Equal<SOOrderShipment.orderType>>>,
										Where<SOOrderShipment.shipmentType, Equal<Required<SOShipment.shipmentType>>,
											And<SOOrderShipment.shipmentNbr, Equal<Required<SOShipment.shipmentNbr>>,
											And<FSxSOOrderType.enableFSIntegration, Equal<True>>>>>
									.SelectWindowed(Base, 0, 1, row.ShipmentType, row.ShipmentNbr)?
									.RowCast<SOOrderType>()
									.Any();
			}
		}
		#endregion

		#region Validations
		public virtual void ValidatePostBatchStatus(PXDBOperation dbOperation, string postTo, string createdDocType, string createdRefNbr)
        {
            DocGenerationHelper.ValidatePostBatchStatus<SOShipment>(Base, dbOperation, postTo, createdDocType, createdRefNbr);
        }
        #endregion
    }
}
