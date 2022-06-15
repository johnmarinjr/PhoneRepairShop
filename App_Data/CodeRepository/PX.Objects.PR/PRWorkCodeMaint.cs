using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CS;
using PX.Objects.PM;
using System.Collections;

namespace PX.Objects.PR
{
	public class PRWorkCodeMaint : PXGraph<PRWorkCodeMaint>
	{
		public class WorkCodeSourcesImpl : WorkCodeSources<PRWorkCodeMaint> { }

		public PXFilter<PRWorkCodeFilter> Filter;
		public PXSave<PRWorkCodeFilter> Save;
		public PXCancel<PRWorkCodeFilter> Cancel;

		public SelectFrom<PMWorkCode>
			.Where<PRxPMWorkCode.countryID.IsEqual<PRWorkCodeFilter.countryID.FromCurrent>>.View WorkCompensationCodes;

		public SelectFrom<PRWorkCompensationBenefitRate>
			.InnerJoin<PRDeductCode>.On<PRDeductCode.codeID.IsEqual<PRWorkCompensationBenefitRate.deductCodeID>>
			.Where<PRWorkCompensationBenefitRate.workCodeID.IsEqual<PMWorkCode.workCodeID.FromCurrent>
				.And<PRDeductCode.countryID.IsEqual<PRxPMWorkCode.countryID.FromCurrent>>
				.And<MatchWithBranch<PRWorkCompensationBenefitRate.branchID>>>.View WorkCompensationRates;

		public SelectFrom<PRDeductCode>
			.Where<PRDeductCode.isWorkersCompensation.IsEqual<True>
				.And<PRDeductCode.isActive.IsEqual<True>>
				.And<PRDeductCode.countryID.IsEqual<P.AsString>>>.View WCDeductions;

		public SelectFrom<PRWorkCompensationMaximumInsurableWage>
			.InnerJoin<PRDeductCode>.On<PRWorkCompensationMaximumInsurableWage.FK.DeductionCode>
			.InnerJoin<State>.On<PRDeductCode.FK.State>
			.Where<PRWorkCompensationMaximumInsurableWage.FK.WorkCode.SameAsCurrent>.View MaximumInsurableWages;

		#region Data View Delegates
		public IEnumerable workCompensationRates()
		{
			PXView bqlSelect = new PXView(this, false, WorkCompensationRates.View.BqlSelect);

			foreach (object objResult in bqlSelect.SelectMulti())
			{
				PXResult<PRWorkCompensationBenefitRate, PRDeductCode> result = objResult as PXResult<PRWorkCompensationBenefitRate, PRDeductCode>;
				if (result != null)
				{
					PRWorkCompensationBenefitRate packageDeduct = (PRWorkCompensationBenefitRate)result;
					PRDeductCode deductCode = (PRDeductCode)result;

					if (packageDeduct.DeductCodeID != null && deductCode.IsActive != true)
					{
						packageDeduct.IsActive = false;
						PXUIFieldAttribute.SetEnabled(WorkCompensationRates.Cache, packageDeduct, false);
						WorkCompensationRates.Cache.RaiseExceptionHandling<PRWorkCompensationBenefitRate.deductCodeID>(
							packageDeduct,
							packageDeduct.DeductCodeID,
							new PXSetPropertyException(Messages.DeductCodeInactive, PXErrorLevel.Warning));
					}

					yield return result;
				}
			}
		}
		#endregion Data View Delegates

		#region CacheAttached
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "State")]
		public virtual void _(Events.CacheAttached<State.stateID> _) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(PRCountryAttribute), nameof(PRCountryAttribute.UseDefault), false)]
		[PXDefault(typeof(PRWorkCodeFilter.countryID))]
		public virtual void _(Events.CacheAttached<PRxPMWorkCode.countryID> _) { }
		#endregion CacheAttached

		#region Actions
		public PXAction<PRWorkCodeFilter> ViewMaximumInsurableWages;
		[PXUIField(DisplayName = "Maximum Insurable Wages", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual void viewMaximumInsurableWages()
		{
			MaximumInsurableWages.AskExt();
		}
		#endregion Actions

		#region Events
		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRemoveBaseAttribute(typeof(PXDefaultAttribute))]
		[PXRemoveBaseAttribute(typeof(PMWorkCodeAttribute))]
		[PXDBString(PMWorkCode.workCodeID.Length, IsUnicode = true, IsKey = true)]
		[PXDefault(typeof(PMWorkCode.workCodeID.FromCurrent))]
		public void _(Events.CacheAttached<PRWorkCompensationBenefitRate.workCodeID> e) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXCustomizeBaseAttribute(typeof(PXUIFieldAttribute), nameof(PXUIFieldAttribute.DisplayName), "Benefit Calculation Method")]
		public void _(Events.CacheAttached<PRDeductCode.cntCalcType> e) { }

		public void _(Events.RowSelected<PRWorkCompensationBenefitRate> e)
		{
			if (e.Row == null)
			{
				return;
			}

			PXUIFieldAttribute.SetEnabled<PRWorkCompensationBenefitRate.deductCodeID>(e.Cache, e.Row, e.Row.DeductCodeID == null);
		}

		public void _(Events.RowInserted<PMWorkCode> e)
		{
			PRxPMWorkCode workCodeExt = PXCache<PMWorkCode>.GetExtension<PRxPMWorkCode>(e.Row);

			foreach (PRDeductCode wcDeduction in WCDeductions.Select(workCodeExt?.CountryID))
			{
				PRWorkCompensationBenefitRate workCompensationRate = new PRWorkCompensationBenefitRate()
				{
					DeductCodeID = wcDeduction.CodeID
				};
				WorkCompensationRates.Insert(workCompensationRate);
			}
		}

		public void _(Events.RowDeleted<PMWorkCode> e)
		{
			SelectFrom<PRWorkCompensationBenefitRate>.Where<PRWorkCompensationBenefitRate.workCodeID.IsEqual<P.AsString>>.View
				.Select(this, e.Row.WorkCodeID)
				.ForEach(x => WorkCompensationRates.Delete(x));
		}

		public void _(Events.FieldUpdated<PMWorkCode.isActive> e)
		{
			PMWorkCode row = e.Row as PMWorkCode;
			if (row == null)
			{
				return;
			}

			PRxPMWorkCode workCodeExt = PXCache<PMWorkCode>.GetExtension<PRxPMWorkCode>(row);

			if (e.NewValue.Equals(true))
			{
				foreach (PRDeductCode result in SelectFrom<PRDeductCode>
					.LeftJoin<PRWorkCompensationBenefitRate>.On<PRWorkCompensationBenefitRate.workCodeID.IsEqual<P.AsString>
						.And<PRWorkCompensationBenefitRate.deductCodeID.IsEqual<PRDeductCode.codeID>>>
					.Where<PRWorkCompensationBenefitRate.deductCodeID.IsNull
						.And<PRDeductCode.isWorkersCompensation.IsEqual<True>>
						.And<PRDeductCode.isActive.IsEqual<True>>
						.And<PRDeductCode.countryID.IsEqual<P.AsString>>>.View.Select(this, row.WorkCodeID, workCodeExt?.CountryID))
				{
					PRWorkCompensationBenefitRate newRate = new PRWorkCompensationBenefitRate()
					{
						WorkCodeID = row.WorkCodeID,
						DeductCodeID = result.CodeID
					};

					WorkCompensationRates.Insert(newRate);
				}
			}
		}
		#endregion Events

		[PXHidden]
		public class PRWorkCodeFilter : IBqlTable
		{
			#region CountryID
			public abstract class countryID : BqlString.Field<countryID> { }
			[PXString(2)]
			[PRCountry]
			[PXUIField(DisplayName = "Country")]
			[PXUIVisible(typeof(Where<FeatureInstalled<FeaturesSet.payrollUS>.And<FeatureInstalled<FeaturesSet.payrollCAN>>>))]
			public virtual string CountryID { get; set; }
			#endregion
		}
	}
}
