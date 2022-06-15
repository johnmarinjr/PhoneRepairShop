using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.CS;
using PX.Objects.EP;
using PX.Payroll.Data;
using PX.Payroll.Data.Vertex;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.PR
{
	public class PREarningTypeMaint : EPEarningTypesSetup
	{
		#region Views
		public SelectFrom<PREarningTypeDetailUS>
			.InnerJoin<PRTaxCode>.On<PREarningTypeDetail.taxID.IsEqual<PRTaxCode.taxID>>
			.Where<PREarningTypeDetail.typecd.IsEqual<EPEarningType.typeCD.FromCurrent>
				.And<PREarningTypeDetail.countryID.IsEqual<BQLLocationConstants.CountryUS>>>.View EarningTypeTaxesUS;

		public SelectFrom<PREarningTypeDetailCAN>
			.InnerJoin<PRTaxCode>.On<PREarningTypeDetail.taxID.IsEqual<PRTaxCode.taxID>>
			.Where<PREarningTypeDetail.typecd.IsEqual<EPEarningType.typeCD.FromCurrent>
				.And<PREarningTypeDetail.countryID.IsEqual<BQLLocationConstants.CountryCAN>>>.View EarningTypeTaxesCAN;

		public PXSelect<EPEarningType, Where<EPEarningType.typeCD, Equal<Current<EPEarningType.typeCD>>>> EarningSettings;

		public PXSetup<PRSetup> Preferences;
		public class SetupValidation : PRSetupValidation<PREarningTypeMaint> { }
		#endregion

		#region Actions
		//Save and Cancel are already defined by the base class
		public PXInsert<EPEarningType> Insert;
		public PXDelete<EPEarningType> Delete;
		public PXCopyPasteAction<EPEarningType> CopyPaste;
		public PXFirst<EPEarningType> First;
		public PXPrevious<EPEarningType> Previous;
		public PXNext<EPEarningType> Next;
		public PXLast<EPEarningType> Last;
		#endregion

		#region Cache Attached
		//Standard earning screen doesn't require a selector, so we override it for payroll only.
		[PXDefault]
		[PXDBString(EPEarningType.typeCD.Length, IsUnicode = true, IsKey = true, InputMask = EPEarningType.typeCD.InputMask)]
		[PXUIField(DisplayName = "Code", Visibility = PXUIVisibility.SelectorVisible)]
		[PXSelector(typeof(EPEarningType.typeCD))]
		public virtual void EPEarningType_TypeCD_CacheAttached(PXCache sender) { }

		[PXMergeAttributes(Method = MergeMethod.Append)]
		[PXRemoveBaseAttribute(typeof(PXDefaultAttribute))]
		[PXDefault(true)]
		protected virtual void _(Events.CacheAttached<EPEarningType.isActive> e) { }
		#endregion

		#region Row Event Handlers
		protected virtual void EPEarningType_RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			EPEarningType row = (EPEarningType)e.Row;
			if (row == null)
				return;

			EarningTypeTaxesUS.Cache.AllowSelect = PXAccess.FeatureInstalled<FeaturesSet.payrollUS>();
			EarningTypeTaxesCAN.Cache.AllowSelect = PXAccess.FeatureInstalled<FeaturesSet.payrollCAN>();
			EarningTypeTaxesCAN.Cache.AllowInsert = false;
			EarningTypeTaxesCAN.Cache.AllowDelete = false;

			PREarningType prRow = sender.GetExtension<PREarningType>(row);
			EarningTypeTaxesUS.Cache.AllowInsert = SubjectToTaxes.IsFromList(prRow.IncludeType);

			EarningTypeTaxesCAN.Cache.AllowUpdate = prRow.WageTypeCDCAN == WageType.CustomWageType;
			bool allowRegularAndSupplementalCanada = GetAvailablePayType(prRow, LocationConstants.CanadaCountryCode) == PayType.RegularAndSupplementalAllowed;
			PXUIFieldAttribute.SetEnabled<PREarningType.isSupplementalCAN>(sender, row, allowRegularAndSupplementalCanada);

			bool requireRegularTypeCD = row.IsOvertime == true || prRow.IsPTO == true;
			PXDefaultAttribute.SetPersistingCheck<PREarningType.regularTypeCD>(sender, row, requireRegularTypeCD ? PXPersistingCheck.NullOrBlank : PXPersistingCheck.Nothing);
			PXUIFieldAttribute.SetRequired<PREarningType.regularTypeCD>(sender, requireRegularTypeCD);
		}
		#endregion

		#region Field Event Handlers
		protected override void EPEarningType_IsActive_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			try
			{
				base.EPEarningType_IsActive_FieldUpdated(sender, e);
			}
			catch (PXException exception)
			{
				sender.SetValue<EPEarningType.isActive>(e.Row, e.OldValue);
				throw new PXSetPropertyException<EPEarningType.isActive>(exception, PXErrorLevel.Error, exception.Message);
			}
		}

		protected virtual void EPEarningType_IncludeType_FieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			EPEarningType row = (EPEarningType)e.Row;
			if (e.Row == null)
				return;

			PREarningType prRow = PXCache<EPEarningType>.GetExtension<PREarningType>(row);

			if (prRow.IncludeType == SubjectToTaxes.All || prRow.IncludeType == SubjectToTaxes.None || prRow.IncludeType == SubjectToTaxes.PerTaxEngine)
			{
				foreach (PREarningTypeDetailUS item in EarningTypeTaxesUS.Select())
				{
					EarningTypeTaxesUS.Delete(item);
				}
			}
		}

		protected virtual void _(Events.FieldVerifying<EPEarningType.isActive> e)
		{
			if (e.NewValue as bool? == true)
				return;

			EPEarningType currentEarningType = e.Row as EPEarningType;
			if (string.IsNullOrWhiteSpace(currentEarningType?.TypeCD))
				return;

			string displayName = PXUIFieldAttribute.GetDisplayName(e.Cache, nameof(EPEarningType.isActive));
			CheckEarningTypeUsage(displayName, currentEarningType.TypeCD, false);
		}

		protected virtual void _(Events.FieldVerifying<PREarningType.earningTypeCategory> e)
		{
			if (Equals(e.OldValue, e.NewValue))
				return;

			EPEarningType currentEarningType = e.Row as EPEarningType;
			if (string.IsNullOrWhiteSpace(currentEarningType?.TypeCD))
				return;

			string displayName = PXUIFieldAttribute.GetDisplayName(e.Cache, nameof(PREarningType.earningTypeCategory));
			CheckEarningTypeUsage(displayName, currentEarningType.TypeCD, true);
		}

		protected virtual void _(Events.FieldDefaulting<PREarningTypeDetailUS.countryID> e)
		{
			if (e.Row == null)
			{
				return;
			}	

			e.NewValue = LocationConstants.USCountryCode;
			e.Cancel = true;
		}

		protected virtual void _(Events.FieldDefaulting<PREarningTypeDetailCAN.countryID> e)
		{
			if (e.Row == null)
			{
				return;
			}

			e.NewValue = LocationConstants.CanadaCountryCode;
			e.Cancel = true;
		}

		protected virtual void _(Events.FieldUpdated<EPEarningType, PREarningType.wageTypeCDCAN> e)
		{
			PREarningType prRow = PXCache<EPEarningType>.GetExtension<PREarningType>(e.Row);
			if (prRow == null)
			{
				return;
			}

			UpdateTaxabilitySettings(prRow);

			PayType availablePayType = GetAvailablePayType(prRow, LocationConstants.CanadaCountryCode);
			if (availablePayType == PayType.Regular)
			{
				prRow.IsSupplementalCAN = false;
			}
			else if (availablePayType == PayType.Supplemental)
			{
				prRow.IsSupplementalCAN = true;
			}
		}

		protected virtual void _(Events.RowInserting<EPEarningType> e)
		{
			if (string.IsNullOrEmpty(e.Row?.TypeCD))
			{
				return;
			}

			// Acuminator disable once PX1043 SavingChangesInEventHandlers
			// [Changes are not persisted when InsertMissingTaxDetails is called with persistChanges=false]
			InsertMissingTaxDetails(e.Row, false);
		}

		protected virtual void _(Events.RowPersisting<EPEarningType> e)
		{
			if ((e.Operation & PXDBOperation.Command) == PXDBOperation.Delete)
			{
				return;
			}

			PREarningType prRow = PXCache<EPEarningType>.GetExtension<PREarningType>(e.Row);
			if (PXAccess.FeatureInstalled<FeaturesSet.payrollUS>())
			{
				if (prRow.WageTypeCD == null)
				{
					e.Cache.RaiseExceptionHandling<PREarningType.wageTypeCD>(
						e.Row,
						null,
						new PXSetPropertyException(ErrorMessages.FieldIsEmpty, PXUIFieldAttribute.GetDisplayName(e.Cache, nameof(prRow.WageTypeCD))));
				}

				if (prRow.ReportType == null)
				{
					e.Cache.RaiseExceptionHandling<PREarningType.reportType>(
						e.Row,
						null,
						new PXSetPropertyException(ErrorMessages.FieldIsEmpty, PXUIFieldAttribute.GetDisplayName(e.Cache, nameof(prRow.ReportType))));
				}
			}

			if (PXAccess.FeatureInstalled<FeaturesSet.payrollCAN>())
			{
				if (prRow.WageTypeCDCAN == null)
				{
					e.Cache.RaiseExceptionHandling<PREarningType.wageTypeCDCAN>(
						e.Row,
						null,
						new PXSetPropertyException(ErrorMessages.FieldIsEmpty, PXUIFieldAttribute.GetDisplayName(e.Cache, nameof(prRow.WageTypeCDCAN))));
				}

				if (prRow.ReportTypeCAN == null)
				{
					e.Cache.RaiseExceptionHandling<PREarningType.reportTypeCAN>(
						e.Row,
						null,
						new PXSetPropertyException(ErrorMessages.FieldIsEmpty, PXUIFieldAttribute.GetDisplayName(e.Cache, nameof(prRow.ReportTypeCAN))));
				}

				InsertMissingTaxDetails(e.Row, true);
			}
		}
		#endregion

		private void CheckEarningTypeUsage(string displayName, string typeCD, bool checkExistingEarningDetails)
		{
			CheckEarningTypesWithCurrentRegularEarningType(displayName, typeCD);
			CheckEmployeesWithCurrentEarningType(displayName, typeCD);
			if (checkExistingEarningDetails)
			{
				CheckPaymentsWithCurrentEarningType(displayName, typeCD);
				CheckPayrollBatchesWithCurrentEarningType(displayName, typeCD);
			}
			CheckPreferencesWithCurrentEarningType(displayName, typeCD);
			CheckOvertimeRulesWithCurrentEarningType(displayName, typeCD);
			CheckPTOBanksWithCurrentEarningType(displayName, typeCD);
		}

		private void CheckEarningTypesWithCurrentRegularEarningType(string displayName, string typeCD)
		{
			EPEarningType earningTypeWithCurrentRegularEarningType =
				SelectFrom<EPEarningType>.
				Where<EPEarningType.isActive.IsEqual<True>.
					And<PREarningType.regularTypeCD.IsEqual<P.AsString>>>.View.
				SelectSingleBound(this, null, typeCD);

			if (earningTypeWithCurrentRegularEarningType == null)
				return;

			throw new PXSetPropertyException(Messages.CannotChangeFieldStateEarningType, PXErrorLevel.Error, displayName, typeCD, earningTypeWithCurrentRegularEarningType.TypeCD);
		}

		private void CheckEmployeesWithCurrentEarningType(string displayName, string typeCD)
		{
			EPEmployee employeeWithCurrentEarningType =
				SelectFrom<EPEmployee>.InnerJoin<PREmployeeEarning>.
					On<EPEmployee.bAccountID.IsEqual<PREmployeeEarning.bAccountID>>.
				Where<PREmployeeEarning.typeCD.IsEqual<P.AsString>.
					And<PREmployeeEarning.isActive.IsEqual<True>>>.View.
				SelectSingleBound(this, null, typeCD);

			if (employeeWithCurrentEarningType == null)
				return;

			throw new PXSetPropertyException(Messages.CannotChangeFieldStateEmployee, PXErrorLevel.Error, displayName, typeCD, employeeWithCurrentEarningType.AcctName);
		}

		private void CheckPaymentsWithCurrentEarningType(string displayName, string typeCD)
		{
			PRPayment paymentsWithCurrentEarningType =
				SelectFrom<PRPayment>.InnerJoin<PREarningDetail>.
					On<PRPayment.refNbr.IsEqual<PREarningDetail.paymentRefNbr>.
						And<PRPayment.docType.IsEqual<PREarningDetail.paymentDocType>>>.
				Where<PREarningDetail.typeCD.IsEqual<P.AsString>.
					And<PRPayment.paid.IsNotEqual<True>>.
					And<PRPayment.released.IsNotEqual<True>>>.View.SelectSingleBound(this, null, typeCD);

			if (paymentsWithCurrentEarningType == null)
				return;

			throw new PXSetPropertyException(Messages.CannotChangeFieldStatePayment, PXErrorLevel.Error, displayName, typeCD, paymentsWithCurrentEarningType.PaymentDocAndRef);
		}

		private void CheckPayrollBatchesWithCurrentEarningType(string displayName, string typeCD)
		{
			PRBatch payrollBatchWithCurrentEarningType =
				SelectFrom<PRBatch>.InnerJoin<PREarningDetail>.
					On<PRBatch.batchNbr.IsEqual<PREarningDetail.batchNbr>>.
				Where<PREarningDetail.typeCD.IsEqual<P.AsString>.
					And<PRBatch.open.IsNotEqual<True>>.
					And<PRBatch.closed.IsNotEqual<True>>>.View.SelectSingleBound(this, null, typeCD);

			if (payrollBatchWithCurrentEarningType == null)
				return;

			throw new PXSetPropertyException(Messages.CannotChangeFieldStatePayrollBatch, PXErrorLevel.Error, displayName, typeCD, payrollBatchWithCurrentEarningType.BatchNbr);
		}

		private void CheckPreferencesWithCurrentEarningType(string displayName, string typeCD)
		{
			string settingName = null;

			if (typeCD == PRSetupMaint.GetEarningTypeFromSetup<PRSetup.regularHoursType>(this))
				settingName = PXUIFieldAttribute.GetDisplayName(Preferences.Cache, nameof(PRSetup.regularHoursType));
			else if (typeCD == PRSetupMaint.GetEarningTypeFromSetup<PRSetup.holidaysType>(this))
				settingName = PXUIFieldAttribute.GetDisplayName(Preferences.Cache, nameof(PRSetup.holidaysType));
			else if (typeCD == PRSetupMaint.GetEarningTypeFromSetup<PRSetup.commissionType>(this))
				settingName = PXUIFieldAttribute.GetDisplayName(Preferences.Cache, nameof(PRSetup.commissionType));

			if (settingName == null)
				return;

			throw new PXSetPropertyException(Messages.CannotChangeFieldStateSetup, PXErrorLevel.Error, displayName, typeCD, settingName);
		}

		private void CheckOvertimeRulesWithCurrentEarningType(string displayName, string typeCD)
		{
			PROvertimeRule overtimeRule = SelectFrom<PROvertimeRule>.
				Where<PROvertimeRule.disbursingTypeCD.IsEqual<P.AsString>.
					And<PROvertimeRule.isActive.IsEqual<True>>>.
				View.SelectSingleBound(this, null, typeCD);

			if (overtimeRule == null)
				return;

			throw new PXSetPropertyException(Messages.CannotChangeFieldStateOvertime, PXErrorLevel.Error, displayName, typeCD, overtimeRule.OvertimeRuleID);
		}

		private void CheckPTOBanksWithCurrentEarningType(string displayName, string typeCD)
		{
			PRPTOBank ptoBank = SelectFrom<PRPTOBank>.
				Where<PRPTOBank.earningTypeCD.IsEqual<P.AsString>.
					And<PRPTOBank.isActive.IsEqual<True>>>.
				View.SelectSingleBound(this, null, typeCD);

			if (ptoBank == null)
				return;

			throw new PXSetPropertyException(Messages.CannotChangeFieldStatePTOBanks, PXErrorLevel.Error, displayName, typeCD, ptoBank.BankID);
		}

		protected virtual PayType GetAvailablePayType(PREarningType row, string countryID)
		{
			if (row.WageTypeCDCAN != null
				&& PRTaxWebServiceDataSlot.GetData(LocationConstants.CanadaCountryCode).WageTypes.TryGetValue(row.WageTypeCDCAN.Value, out WageType wageTypeDefinition))
			{
				return wageTypeDefinition.PayType;
			}
			else
			{
				return PayType.RegularAndSupplementalAllowed;
			}
		}

		public virtual void UpdateTaxabilitySettings(PREarningType row)
		{
			if (row.WageTypeCDCAN == null || row.WageTypeCDCAN == WageType.CustomWageType)
			{
				return;
			}

			if (row.WageTypeCDCAN == null
				|| !PRTaxWebServiceDataSlot.GetData(LocationConstants.CanadaCountryCode).WageTypes.TryGetValue(row.WageTypeCDCAN.Value, out WageType wageTypeDefinition))
			{
				return;
			}

			foreach (PXResult<PREarningTypeDetailCAN, PRTaxCode> result in EarningTypeTaxesCAN.Select())
			{
				PREarningTypeDetailCAN detail = result;
				PRTaxCode taxCode = result;

				WageTypeTaxability wageTypeOverride = wageTypeDefinition.Overrides.FirstOrDefault(x => x.TaxUniqueCode == taxCode.TaxUniqueCode);
				detail.Taxability = (int)(wageTypeOverride?.CompensationType ?? wageTypeDefinition.CompensationType);
				EarningTypeTaxesCAN.Update(detail);
			}
		}

		public virtual void InsertMissingTaxDetails(EPEarningType row, bool persistChanges)
		{
			HashSet<int?> alreadyInsertedTaxIDs = EarningTypeTaxesCAN.Select().FirstTableItems.Select(x => x.TaxID).ToHashSet();

			foreach (PRTaxCode taxCode in SelectFrom<PRTaxCode>
				.Where<PRTaxCode.countryID.IsEqual<BQLLocationConstants.CountryCAN>>.View.Select(this).FirstTableItems
				.Where(x => !alreadyInsertedTaxIDs.Contains(x.TaxID)))
			{
				PREarningTypeDetailCAN detail = CreateEarningTypeDetail<PREarningTypeDetailCAN>(row, taxCode);
				if (detail != null)
				{
					detail = EarningTypeTaxesCAN.Insert(detail);
					if (persistChanges)
					{
						EarningTypeTaxesCAN.Cache.PersistInserted(detail);
					}
				}
			}
		}

		public static T CreateEarningTypeDetail<T>(EPEarningType earningType, PRTaxCode taxCode)
			where T : PREarningTypeDetail, new()
		{
			PREarningType prEarningType = PXCache<EPEarningType>.GetExtension<PREarningType>(earningType);
			if (prEarningType == null)
			{
				return null;
			}

			CompensationType defaultType = CompensationType.CashSubjectTaxable;
			WageType wageTypeDefinition = null;
			if (prEarningType.WageTypeCDCAN != null
				&& PRTaxWebServiceDataSlot.GetData(LocationConstants.CanadaCountryCode).WageTypes.TryGetValue(prEarningType.WageTypeCDCAN.Value, out wageTypeDefinition))
			{
				defaultType = wageTypeDefinition.CompensationType;
			}

			WageTypeTaxability wageTypeOverride = wageTypeDefinition?.Overrides.FirstOrDefault(x => x.TaxUniqueCode == taxCode.TaxUniqueCode);
			T detail = new T()
			{
				TypeCD = earningType.TypeCD,
				TaxID = taxCode.TaxID,
				Taxability = (int)(wageTypeOverride?.CompensationType ?? defaultType),
				CountryID = taxCode.CountryID
			};

			return detail;
		}

		[PXHidden]
		public class PREarningTypeDetailUS : PREarningTypeDetail
		{
			public abstract new class countryID : PX.Data.BQL.BqlString.Field<countryID> { }
		}

		[PXHidden]
		public class PREarningTypeDetailCAN : PREarningTypeDetail
		{
			public abstract new class countryID : PX.Data.BQL.BqlString.Field<countryID> { }
		}
	}
}