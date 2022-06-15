using System;
using PX.Data;
using PX.Objects.IN;
using PX.Objects.IN.GraphExtensions;
using PX.Objects.SO.GraphExtensions;

namespace PX.Objects.PM.MaterialManagement.GraphExtensions.ItemAvailability.Allocated
{
	public abstract class ItemAvailabilityAllocatedProjectExtension<TGraph, TItemAvailExt, TItemAvailAllocExt, TItemAvailProjExt, TLine, TSplit> : PXGraphExtension<TItemAvailAllocExt, TItemAvailExt, TGraph>
		where TGraph : PXGraph
		where TItemAvailExt : ItemAvailabilityExtension<TGraph, TLine, TSplit>
		where TItemAvailAllocExt : ItemAvailabilityAllocatedExtension<TGraph, TItemAvailExt, TLine, TSplit>
		where TItemAvailProjExt : ItemAvailabilityProjectExtension<TGraph, TItemAvailExt, TLine, TSplit>
		where TLine : class, IBqlTable, ILSPrimary, new()
		where TSplit : class, IBqlTable, ILSDetail, new()
	{
		protected static bool UseProjectAvailability => PXAccess.FeatureInstalled<CS.FeaturesSet.materialManagement>();

		protected TItemAvailExt ItemAvailBase => Base1;
		protected TItemAvailAllocExt ItemAvailAllocBase => Base2;
		protected TItemAvailProjExt ItemAvailProjExt => _itemAvailProjExt ?? (_itemAvailProjExt = Base.FindImplementation<TItemAvailProjExt>());
		private TItemAvailProjExt _itemAvailProjExt;

		/// Overrides <see cref="ItemAvailabilityAllocatedExtension{TGraph, TItemAvailExt, TLine, TSplit}.GetStatusWithAllocated(TLine)"/>
		[PXOverride]
		public virtual string GetStatusWithAllocated(TLine line,
			Func<TLine, string> base_GetStatusWithAllocated)
		{
			if (UseProjectAvailability)
				return GetStatusWithAllocatedProject(line) ?? base_GetStatusWithAllocated(line);
			else
				return base_GetStatusWithAllocated(line);
		}

		protected abstract string GetStatusWithAllocatedProject(TLine line);
	}
}
