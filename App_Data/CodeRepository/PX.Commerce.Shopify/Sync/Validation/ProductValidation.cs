using System.Text;
using System.Threading.Tasks;
using PX.Data;
using PX.Commerce.Core;
using PX.Commerce.Objects;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Commerce.Shopify.API.REST;
using PX.Api;

namespace PX.Commerce.Shopify
{
	public class ProductValidator : BCBaseValidator, ISettingsValidator
	{
		public int Priority { get { return 0; } }

		public virtual void Validate(IProcessor iproc)
		{
			Validate<SPStockItemProcessor>(iproc, (processor) =>
			{
				BCEntity entity = processor.GetEntity();
				BCBinding store = processor.GetBinding();
				BCBindingExt storeExt = processor.GetBindingExt<BCBindingExt>();
				if (storeExt.DefaultStoreCurrency != null)
				{
					var branch = PX.Objects.GL.Branch.PK.Find(processor, store.BranchID);
					if (branch.BaseCuryID != storeExt.DefaultStoreCurrency)
						throw new PXException(BCMessages.BranchWithIncorrectCurrency, branch.BaseCuryID, storeExt.DefaultStoreCurrency);
				}
			});
			Validate<SPNonStockItemProcessor>(iproc, (processor) =>
			{
				BCEntity entity = processor.GetEntity();
				BCBindingExt storeExt = processor.GetBindingExt<BCBindingExt>();
				BCBinding store = processor.GetBinding();
				if (storeExt.DefaultStoreCurrency != null)
				{
					var branch = PX.Objects.GL.Branch.PK.Find(processor, store.BranchID);
					if (branch.BaseCuryID != storeExt.DefaultStoreCurrency)
						throw new PXException(BCMessages.BranchWithIncorrectCurrency, branch.BaseCuryID, storeExt.DefaultStoreCurrency);
				}
			});

			Validate<SPTemplateItemProcessor>(iproc, (processor) =>
			{
				BCEntity entity = processor.GetEntity();
				BCBindingExt storeExt = processor.GetBindingExt<BCBindingExt>();
				BCBinding store = processor.GetBinding();
				if (storeExt.DefaultStoreCurrency != null)
				{
					var branch = PX.Objects.GL.Branch.PK.Find(processor, store.BranchID);
					if (branch.BaseCuryID != storeExt.DefaultStoreCurrency)
						throw new PXException(BCMessages.BranchWithIncorrectCurrency, branch.BaseCuryID, storeExt.DefaultStoreCurrency);
				}

				if (PXAccess.FeatureInstalled<FeaturesSet.matrixItem>() == false)
					throw new PXException(BCMessages.MatrixFeatureRequired);
			});
		}
	}
}
