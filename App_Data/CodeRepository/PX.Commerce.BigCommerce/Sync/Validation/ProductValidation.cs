using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Data;
using PX.Commerce.Core;
using PX.Commerce.Objects;
using PX.Objects.AR;
using PX.Objects.CS;
using PX.Commerce.BigCommerce.API.REST;
using PX.Objects.IN;
using PX.Commerce.BigCommerce.API.WebDAV;

namespace PX.Commerce.BigCommerce
{
	public class ProductValidator : BCBaseValidator, ISettingsValidator
	{
		public int Priority { get { return 0; } }

		public virtual void Validate(IProcessor iproc)
		{
			Validate<BCImageProcessor>(iproc, (processor) =>
			{
				BCBindingBigCommerce binding = processor.GetBindingExt<BCBindingBigCommerce>();
				BCWebDavClient webDavClient = BCConnector.GetWebDavClient(binding);
				var folder = webDavClient.GetFolder();
				if (folder == null) throw new PXException(BigCommerceMessages.TestConnectionFolderNotFound);
			});
			Validate<BCStockItemProcessor>(iproc, (processor) =>
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

				if (entity.Direction != BCSyncDirectionAttribute.Export)
				{
					if (storeExt.StockItemClassID == null)
					{
						INSetup inSetup = PXSelect<INSetup>.Select(processor);
						if (inSetup.DfltStkItemClassID == null)
							throw new PXException(BigCommerceMessages.NoStockItemClass);
					}
				}
			});
			Validate<BCNonStockItemProcessor>(iproc, (processor) =>
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
				if (entity.Direction != BCSyncDirectionAttribute.Export)
				{
					if (storeExt.NonStockItemClassID == null)
					{
						INSetup inSetup = PXSelect<INSetup>.Select(processor);
						if (inSetup.DfltNonStkItemClassID == null)
							throw new PXException(BigCommerceMessages.NoNonStockItemClass);
					}
				}
			});

			Validate<BCTemplateItemProcessor>(iproc, (processor) =>
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
