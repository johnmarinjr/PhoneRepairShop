using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PX.Objects.CS;

namespace PX.Objects.RUTROT
{
	[Obsolete(Common.Messages.ItemIsObsoleteAndWillBeRemoved2021R2)]
	public class BranchMaintRUTROT : ConfigurationMaintRUTROTBase<BranchMaint>
	{
		protected override RUTROTConfigurationHolderMapping GetDocumentMapping()
		{
			return new RUTROTConfigurationHolderMapping(typeof(BranchMaint.BranchBAccount));
		}
	}
}