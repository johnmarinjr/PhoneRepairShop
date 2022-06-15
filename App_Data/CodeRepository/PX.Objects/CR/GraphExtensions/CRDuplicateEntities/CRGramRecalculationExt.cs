using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.CS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.CR.Extensions.CRDuplicateEntities
{
	public class CRGramRecalculationExt<TGraph> : PXGraphExtension<TGraph> where TGraph : PXGraph
	{
		protected static bool IsFeatureActive() => PXAccess.FeatureInstalled<FeaturesSet.contactDuplicate>();

		public SelectFrom<CRSetup>.View Setup;

		[PXOverride]
		public virtual void Persist(Action del)
		{
			// already asked
			if (Setup.View.Answer.IsPositive())
			{
				PXRedirectHelper.TryRedirect(PXGraph.CreateInstance<CRGrammProcess>(), PXRedirectHelper.WindowMode.Same);
			}

			var requiresGrammCalculation = RequiresGramRecalculation();

			del();

			if (!Base.IsImport && !Base.IsExport && requiresGrammCalculation)
			{
				Setup.View.Ask(
					row: null,
					header: "Warning",
					message: PXMessages.Localize(MessagesNoPrefix.WouldYouLikeToRecalculateRecords),
					buttons: MessageButtons.YesNo,
					icon: MessageIcon.Warning);
			}
		}

		protected virtual bool RequiresGramRecalculation()
		{
			return Base
				.Caches<CRValidation>()
				.Updated
				.OfType<CRValidation>()
				.Any(v => v.GramValidationDateTime is null);
		}
	}
}
