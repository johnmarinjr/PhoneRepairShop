using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Data;
using PX.Commerce.Core;
using PX.Objects.AR;
using PX.Objects.CS;

namespace PX.Commerce.Shopify
{
	public class GeneralValidator : BCBaseValidator, ISettingsValidator, IExternValidator
	{
		public int Priority { get { return int.MaxValue; } }

		public virtual void Validate(IProcessor processor)
		{
			if (PXAccess.FeatureInstalled<FeaturesSet.subItem>() == true)
				throw new PXException(BCMessages.FeatureNotSupported, PXMessages.LocalizeNoPrefix(BCCaptions.InventorySubitems));
			if (PXAccess.FeatureInstalled<FeaturesSet.financialStandard>() == false)
				throw new PXException(BCMessages.FeatureRequired, PXMessages.LocalizeNoPrefix(BCCaptions.StandardFinancials));
			if (PXAccess.FeatureInstalled<FeaturesSet.accountLocations>() == false)
				throw new PXException(BCMessages.FeatureRequired, PXMessages.LocalizeNoPrefix(BCCaptions.BusinessAccountsLocation));
			if (PXAccess.FeatureInstalled<FeaturesSet.distributionModule>() == false)
				throw new PXException(BCMessages.FeatureRequired, PXMessages.LocalizeNoPrefix(BCCaptions.Distribution));
		}

		public virtual void Validate(IProcessor processor, IExternEntity entity)
		{
			RunAttributesValidation(processor, entity);
		}
	}
}
