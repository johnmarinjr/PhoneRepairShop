using PX.Commerce.Core;
using PX.Commerce.Core.API;
using PX.Commerce.Objects;
using PX.Data;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Objects.GL;
using PX.Objects.SO;
using System.Linq;

namespace PX.Commerce.BigCommerce
{
	public class OrderValidator : BCBaseValidator, ISettingsValidator, ILocalValidator
	{
		public int Priority { get { return 0; } }

		public virtual void Validate(IProcessor iproc)
		{
			Validate<BCSalesOrderProcessor>(iproc, (processor) =>
			{
				BCEntity entity = processor.GetEntity();
				BCBinding store = processor.GetBinding();
				BCBindingExt storeExt = processor.GetBindingExt<BCBindingExt>();

				//Branch
				if (store.BranchID == null)
					throw new PXException(BigCommerceMessages.NoBranch);

				if (storeExt.DefaultStoreCurrency != null)
				{
					var branch = PX.Objects.GL.Branch.PK.Find(processor, store.BranchID);
					if (branch.BaseCuryID != storeExt.DefaultStoreCurrency)
						throw new PXException(BCMessages.BranchWithIncorrectCurrency, branch.BaseCuryID, storeExt.DefaultStoreCurrency);
				}
				ARSetup arSetup = PXSelect<ARSetup>.Select(processor);
				if (arSetup?.MigrationMode == true)
					throw new PXException(BCMessages.MigrationModeOnSO);
				//Integrated CC Porcessing
				if (arSetup?.IntegratedCCProcessing != true)
				{
					foreach (BCPaymentMethods method in PXSelect<BCPaymentMethods,
						Where<BCPaymentMethods.bindingID, Equal<Required<BCPaymentMethods.bindingID>>,
							And<BCPaymentMethods.active, Equal<True>,
							And<BCPaymentMethods.processingCenterID, IsNotNull>>>>.Select(processor, store.BindingID))
					{
						throw new PXException(BCObjectsMessages.IntegratedCCProcessingSync, method.ProcessingCenterID, method.StorePaymentMethod);
					}
				}

				SOOrderType type = PXSelect<SOOrderType, Where<SOOrderType.orderType, Equal<Required<SOOrderType.orderType>>>>.Select(processor, storeExt.OrderType);
				//OrderType
				if (type == null || type.Active != true)
					throw new PXException(BigCommerceMessages.NoSalesOrderType);
				//Order Numberings
				BCAutoNumberAttribute.CheckAutoNumbering(processor, type.OrderNumberingID);
			});
			Validate<BCPaymentProcessor>(iproc, (processor) =>
			{
				BCEntity entity = processor.GetEntity();
				BCBinding store = processor.GetBinding();
				BCBindingExt storeExt = processor.GetBindingExt<BCBindingExt>();

				//Branch
				if (store.BranchID == null)
					throw new PXException(BigCommerceMessages.NoBranch);

				ARSetup arSetup = PXSelect<ARSetup>.Select(processor);
				if (arSetup?.MigrationMode == true)
					throw new PXException(BCMessages.MigrationModeOn);
				//Integrated CC Porcessing
				if (arSetup?.IntegratedCCProcessing != true)
				{
					foreach (BCPaymentMethods method in PXSelect<BCPaymentMethods,
						Where<BCPaymentMethods.bindingID, Equal<Required<BCPaymentMethods.bindingID>>,
							And<BCPaymentMethods.active, Equal<True>,
							And<BCPaymentMethods.processingCenterID, IsNotNull>>>>.Select(processor, store.BindingID))
					{
						throw new PXException(BCObjectsMessages.IntegratedCCProcessingSync,  method.ProcessingCenterID, method.StorePaymentMethod);
					}
				}

				//Numberings
				BCAutoNumberAttribute.CheckAutoNumbering(processor, arSetup.PaymentNumberingID);
			});
			Validate<BCRefundsProcessor>(iproc, (processor) =>
			{
				BCEntity entity = processor.GetEntity();
				BCBinding store = processor.GetBinding();
				BCBindingExt storeExt = processor.GetBindingExt<BCBindingExt>();

				//Branch
				if (store.BranchID == null)
					throw new PXException(BigCommerceMessages.NoBranch);

				ARSetup arSetup = PXSelect<ARSetup>.Select(processor);
				if (arSetup?.MigrationMode == true)
					throw new PXException(BCMessages.MigrationModeOn);
				//Integrated CC Porcessing
				if (arSetup?.IntegratedCCProcessing != true)
				{
					foreach (BCPaymentMethods method in PXSelect<BCPaymentMethods,
						Where<BCPaymentMethods.bindingID, Equal<Required<BCPaymentMethods.bindingID>>,
							And<BCPaymentMethods.active, Equal<True>,
							And<BCPaymentMethods.processingCenterID, IsNotNull>>>>.Select(processor, store.BindingID))
					{
						throw new PXException(BCObjectsMessages.IntegratedCCProcessingSync, method.ProcessingCenterID, method.StorePaymentMethod);
					}
				}

				//Numberings
				BCAutoNumberAttribute.CheckAutoNumbering(processor, arSetup.PaymentNumberingID);
			});
		}

		public void Validate(IProcessor iproc, ILocalEntity ilocal)
		{
			Validate<BCSalesOrderProcessor, SalesOrder>(iproc, ilocal, (processor, entity) =>
			{
				BCBinding store = processor.GetBinding();
				BCBindingExt storeExt = processor.GetBindingExt<BCBindingExt>();
				if (storeExt.PostDiscounts == BCPostDiscountAttribute.DocumentDiscount && entity.DiscountDetails?.Count > 0 && PXAccess.FeatureInstalled<FeaturesSet.customerDiscounts>() == false)
					throw new PXException(BCMessages.DocumentDiscountSOMsg);
			});
			Validate<BCRefundsProcessor, SalesOrder>(iproc, ilocal, (processor, entity) =>
			{
				BCBinding store = processor.GetBinding();
				BCBindingExt storeExt = processor.GetBindingExt<BCBindingExt>();
				if (storeExt.PostDiscounts == BCPostDiscountAttribute.DocumentDiscount && (entity.RefundOrders != null && entity.RefundOrders.Any(x => x.DiscountDetails?.Count > 0)) && PXAccess.FeatureInstalled<FeaturesSet.customerDiscounts>() == false)
					throw new PXException(BCMessages.DocumentDiscountSOMsg);
			});
		}
	}
}
