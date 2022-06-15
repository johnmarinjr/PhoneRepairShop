using PX.Data;
using PX.Objects.CR;
using PX.Objects.CS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.GL
{
	public sealed class JournalWithSubEntryMultipleBaseCurrencies : PXGraphExtension<JournalWithSubEntry>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<FeaturesSet.multipleBaseCurrencies>();
		}

		[PXRestrictor(
					typeof(Where<BAccountR.baseCuryID, EqualBaseCuryID<GLDocBatch.branchID.FromCurrent>>),
					"",
					SuppressVerify = false
				)]
		[PXMergeAttributes(Method = MergeMethod.Append)]
		public void _(Events.CacheAttached<GLTranDoc.bAccountID> e) { }
	}
}
