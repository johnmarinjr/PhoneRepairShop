using PX.Data;
using PX.Data.BQL;
using PX.Objects.AM.Attributes;
using PX.Objects.EP;
using PX.Objects.IN;
using System;

namespace PX.Objects.AM.CacheExtensions
{
	public sealed class EPShiftCodeExt : PXCacheExtension<EPShiftCode>
	{
		public static bool IsActive()
		{
			return PXAccess.FeatureInstalled<CS.FeaturesSet.manufacturing>();
		}

		#region DiffType
		[PXString(1, IsFixed = true)]
		[PXUIField(DisplayName = "Diff Type", Required = true)]
		[ShiftDiffType.List]
		public string DiffType { get; set; }
		public abstract class diffType : BqlString.Field<diffType> { }
		#endregion
		#region ShftDiff
		[PXPriceCost]
		[PXUIField(DisplayName = "Shift Diff", Required = true)]
		public decimal? ShftDiff { get; set; }
		public abstract class shftDiff : BqlDecimal.Field<shftDiff> { }
		#endregion
		#region AMCrewSize
		[PXDBDecimal(6)]
		[PXUIField(DisplayName = "Crew Size", Required = true, FieldClass = "MFGADVANCEDPLANNING")]
		[PXDefault(TypeCode.Decimal, "0.0", PersistingCheck = PXPersistingCheck.Nothing)]
		public decimal? AMCrewSize { get; set; }
		public abstract class amCrewSize : BqlDecimal.Field<amCrewSize> { }
		#endregion
	}
}
