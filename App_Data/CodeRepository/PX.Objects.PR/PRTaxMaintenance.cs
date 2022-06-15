using CsvHelper;
using Newtonsoft.Json;
using PX.Common;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Objects.AP;
using PX.Objects.CR;
using PX.Objects.CS;
using PX.Objects.EP;
using PX.Payroll;
using PX.Payroll.Data;
using PX.Payroll.Data.Vertex;
using PX.Payroll.Proxy;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using WSTaxType = PX.Payroll.Data.PRTaxType;

namespace PX.Objects.PR
{
	public class PRTaxMaintenance : PXGraph<PRTaxMaintenance>
	{
		public PRTaxMaintenance()
		{
			Taxes.AllowInsert = false;
			Taxes.AllowDelete = false;
			TaxAttributes.AllowInsert = false;
			TaxAttributes.AllowDelete = false;
			CompanyAttributes.AllowInsert = false;
			CompanyAttributes.AllowDelete = false;

			Employees.SetProcessDelegate(list => AssignEmployeeTaxes(list));
		}

		public override bool IsDirty
		{
			get
			{
				PXLongRunStatus status = PXLongOperation.GetStatus(this.UID);
				if (status == PXLongRunStatus.Completed || status == PXLongRunStatus.Aborted)
				{
					foreach (KeyValuePair<Type, PXCache> pair in Caches)
					{
						if (Views.Caches.Contains(pair.Key) && pair.Value.IsDirty)
						{
							return true;
						}
					}
				}
				return base.IsDirty;
			}
		}

		#region Views
		// This dummy view needs to be declared above any view that contains PREmployee so that the Vendor cache, not the cache from derived
		// type PREmployee, is used in Vendor selectors.
		[PXHidden]
		[PXCopyPasteHiddenView]
		public PXSelect<Vendor> DummyVendor;

		public PXFilter<PRTaxMaintenanceFilter> Filter;

		public SelectFrom<PRTaxCode>.View AllTaxes;
		public SelectFrom<PRTaxCode>
			.Where<PRTaxCode.countryID.IsEqual<PRTaxMaintenanceFilter.countryID.FromCurrent>>.View Taxes;
		public SelectFrom<PRTaxCode>
			.Where<PRTaxCode.taxID.IsEqual<PRTaxCode.taxID.FromCurrent>>.View CurrentTax;

		public PRAttributeDefinitionSelect<
			PRTaxCodeAttribute,
			SelectFrom<PRTaxCodeAttribute>
				.Where<PRTaxCodeAttribute.taxID.IsEqual<PRTaxCode.taxID.FromCurrent>>
				.OrderBy<PRTaxCodeAttribute.sortOrder.Asc, PRTaxCodeAttribute.description.Asc>,
			PRTaxCode,
			Payroll.Data.PRTax,
			Payroll.TaxTypeAttribute> TaxAttributes;

		public PREmployeeAttributeDefinitionSelect<
			PRCompanyTaxAttribute,
			SelectFrom<PRCompanyTaxAttribute>
				.Where<PRCompanyTaxAttribute.countryID.IsEqual<PRTaxMaintenanceFilter.countryID.FromCurrent>
					.And<PRTaxMaintenanceFilter.filterStates.FromCurrent.IsNotEqual<True>
						.Or<PRCompanyTaxAttribute.state.IsEqual<BQLLocationConstants.FederalUS>>
						.Or<PRCompanyTaxAttribute.state.IsEqual<BQLLocationConstants.FederalCAN>>
						.Or<PRCompanyTaxAttribute.taxesInState.IsNotNull>>>
				.OrderBy<PRCompanyTaxAttribute.state.Asc, PRCompanyTaxAttribute.sortOrder.Asc, PRCompanyTaxAttribute.description.Asc>,
			PRTaxMaintenanceFilter.filterStates,
			PRTaxMaintenanceFilter.countryID,
			PRTaxCode,
			SelectFrom<PRTaxCode>.Where<PRTaxCode.countryID.IsEqual<PRTaxMaintenanceFilter.countryID.FromCurrent>>,
			PRTaxCode.taxState> CompanyAttributes;

		public SelectFrom<PRCompanyTaxAttribute>
			.LeftJoin<PRTaxCode>.On<PRTaxCode.taxState.IsEqual<PRCompanyTaxAttribute.state>>
			.Where<Brackets<PRTaxMaintenanceFilter.filterStates.FromCurrent.IsNotEqual<True>
					.Or<PRCompanyTaxAttribute.state.IsEqual<BQLLocationConstants.FederalUS>>
					.Or<PRCompanyTaxAttribute.state.IsEqual<BQLLocationConstants.FederalCAN>>
					.Or<PRTaxCode.taxID.IsNotNull>>
				.And<PRCompanyTaxAttribute.countryID.IsEqual<PRTaxMaintenanceFilter.countryID.FromCurrent>>>.View FilteredCompanyAttributes;

		public SelectFrom<Address>
			.LeftJoin<PRLocation>.On<PRLocation.addressID.IsEqual<Address.addressID>>
			.LeftJoin<PREmployee>.On<PREmployee.defAddressID.IsEqual<Address.addressID>>
			.Where<PRLocation.isActive.IsEqual<True>
				.Or<PREmployee.activeInPayroll.IsEqual<True>>>.View Addresses;

		public SelectFrom<Address>
			.InnerJoin<PRLocation>.On<PRLocation.addressID.IsEqual<Address.addressID>>
			.LeftJoin<PREmployeeWorkLocation>.On<PREmployeeWorkLocation.locationID.IsEqual<PRLocation.locationID>>
			.LeftJoin<PREmployeeClassWorkLocation>.On<PREmployeeClassWorkLocation.locationID.IsEqual<PRLocation.locationID>>
			.Where<Address.countryID.IsEqual<BQLLocationConstants.CountryCAN>
				.And<PRLocation.isActive.IsEqual<True>>>
			.AggregateTo<GroupBy<Address.addressID>>.View CanadaTaxableAddresses;

		public InvokablePXProcessing<PREmployee,
			Where<PREmployee.countryID.IsEqual<PRTaxMaintenanceFilter.countryID.FromCurrent>>> Employees;

		public SelectFrom<PREarningTypeDetail>
			.Where<PREarningTypeDetail.taxID.IsEqual<P.AsInt>
				.And<PREarningTypeDetail.countryID.IsEqual<P.AsString>>>.View EarningTypeDetails;

		public SelectFrom<PRDeductCodeDetail>
			.InnerJoin<PRDeductCode>.On<PRDeductCode.codeID.IsEqual<PRDeductCodeDetail.codeID>>
			.Where<PRDeductCodeDetail.taxID.IsEqual<P.AsInt>
				.And<PRDeductCode.countryID.IsEqual<P.AsString>>>.View DeductCodeDetails;

		public PXSetup<PRSetup> Setup;
		public class SetupValidation : PRSetupValidation<PRTaxMaintenance> { }
		#endregion Views

		#region Data view delegates
		public virtual IEnumerable taxes()
		{
			UpdateTaxesCustomInfo customInfo = PXLongOperation.GetCustomInfoPersistent(this.UID) as UpdateTaxesCustomInfo;
			PXLongOperation.RemoveCustomInfoPersistent(this.UID);
			bool taxesCached = Taxes.Cache.Cached.Any_();
			List<object> taxList = new PXView(this, false, Taxes.View.BqlSelect).SelectMulti();
			if (!taxesCached || customInfo?.ValidateTaxesNeeded == true)
			{
				ValidateTaxAttributes(taxList.Select(x => (PRTaxCode)x).ToList());
				if (customInfo != null)
				{
					customInfo.ValidateTaxesNeeded = false;
				}
			}

			if (customInfo?.NewTaxes.Any() == true)
			{
				customInfo.NewTaxes.ForEach(x =>
				{
					AdjustTaxCDForDuplicate(x);
					x = Taxes.Insert(x);
					taxList.Add(x);
				});
				ValidateTaxAttributes(Taxes.Cache.Inserted.Cast<PRTaxCode>().ToList());
			}

			if (customInfo?.UpdatedTaxes.Any() == true)
			{
				customInfo.UpdatedTaxes.ForEach(x => Taxes.Update(x));
				ValidateTaxAttributes(Taxes.Cache.Updated.Cast<PRTaxCode>().ToList());
			}

			return taxList;
		}
		#endregion

		#region Actions
		public PXSave<PRTaxMaintenanceFilter> Save;
		public PXCancel<PRTaxMaintenanceFilter> Cancel;

		public PXAction<PRTaxMaintenanceFilter> UpdateTaxes;
		[PXUIField(DisplayName = "Update Taxes", MapEnableRights = PXCacheRights.Update, MapViewRights = PXCacheRights.Update)]
		[PXButton]
		public virtual IEnumerable updateTaxes(PXAdapter adapter)
		{
			PXLongOperation.StartOperation(this, delegate ()
			{
				List<PRTaxCode> newTaxes = new List<PRTaxCode>();
				List<PRTaxCode> updatedTaxes = new List<PRTaxCode>();
				BackgroundTaxDataUpdate backgroundUpdateGraph = CreateInstance<BackgroundTaxDataUpdate>();

				if (PXAccess.FeatureInstalled<FeaturesSet.payrollCAN>())
				{
					TaxLocationHelpers.UpdateAddressLocationCodes(Addresses.Select().FirstTableItems.ToList());
					Addresses.Cache.Clear();
					PRWebServiceRestClient restClient = new PRWebServiceRestClient();
					List<WSTaxType> applicableTaxes = restClient.GetTaxList(
						LocationConstants.CanadaCountryCode,
						CanadaTaxableAddresses.Select().FirstTableItems.Select(x => new PRLocationCode() { TaxLocationCode = x.TaxLocationCode })).ToList();
					backgroundUpdateGraph.UpdateCanadaBackgroundData(applicableTaxes);
					backgroundUpdateGraph.Actions.PressSave();
					CreateTaxes(applicableTaxes, out newTaxes, out updatedTaxes);
				}
				else
				{
					PXPayrollAssemblyScope.UpdateTaxDefinition();
					CreateTaxesForAllUSLocations(out newTaxes, out updatedTaxes);
					backgroundUpdateGraph.UpdateUSBackgroundData();
					backgroundUpdateGraph.Actions.PressSave();
				}

				UpdateEarningTypeTaxability();
				UpdateDeductionCodeTaxability();

				PXLongOperation.SetCustomInfoPersistent(new UpdateTaxesCustomInfo(newTaxes, updatedTaxes));
			});

			return adapter.Get();
		}

		public PXAction<PRTaxMaintenanceFilter> AssignTaxesToEmployees;
		[PXUIField(DisplayName = "Assign Taxes to Employees", MapEnableRights = PXCacheRights.Delete, MapViewRights = PXCacheRights.Delete)]
		[PXProcessButton]
		public virtual IEnumerable assignTaxesToEmployees(PXAdapter adapter)
		{
			PXLongOperation.ClearStatus(this.UID);
			return Employees.Invoke(adapter);
		}

		public PXAction<PRTaxCode> ViewTaxDetails;
		[PXUIField(DisplayName = "Tax Details", MapEnableRights = PXCacheRights.Select, MapViewRights = PXCacheRights.Select)]
		[PXButton]
		public virtual IEnumerable viewTaxDetails(PXAdapter adapter)
		{
			CurrentTax.AskExt();
			return adapter.Get();
		}
		#endregion Actions

		#region Events
		public virtual void _(Events.RowSelected<PRTaxMaintenanceFilter> e)
		{
			AssignTaxesToEmployees.SetEnabled(!Taxes.Cache.Inserted.Any_() && !TaxAttributes.Cache.IsDirty && !CompanyAttributes.Cache.IsDirty);
		}

		public virtual void _(Events.RowSelected<PRTaxCode> e)
		{
			if (e.Row == null)
			{
				return;
			}

			SetTaxCodeError(e.Cache, e.Row);
		}

		public virtual void _(Events.RowPersisting<PRTaxCodeAttribute> e)
		{
			if (e.Row.ErrorLevel == (int?)PXErrorLevel.RowError)
			{
				e.Cache.RaiseExceptionHandling<PRTaxCodeAttribute.value>(
					e.Row,
					e.Row.Value,
					new PXSetPropertyException(Messages.ValueBlankAndRequired, PXErrorLevel.RowError));
			}
		}

		public virtual void _(Events.RowPersisting<PRCompanyTaxAttribute> e)
		{
			if (e.Row.ErrorLevel == (int?)PXErrorLevel.RowError)
			{
				e.Cache.RaiseExceptionHandling<PRCompanyTaxAttribute.value>(
					e.Row,
					e.Row.Value,
					new PXSetPropertyException(Messages.ValueBlankAndRequiredAndNotOverridable, PXErrorLevel.RowError));
			}
		}

		public virtual void _(Events.RowPersisted<PRTaxCode> e)
		{
			if ((e.Operation & PXDBOperation.Command) != PXDBOperation.Insert || e.TranStatus != PXTranStatus.Open
				|| e.Row.CountryID != LocationConstants.CanadaCountryCode)
			{
				return;
			}

			HashSet<string> earningTypesWithExistingRecord = EarningTypeDetails.Select(e.Row.TaxID, LocationConstants.CanadaCountryCode).FirstTableItems
				.Select(x => x.TypeCD).ToHashSet();

			foreach (EPEarningType earningType in SelectFrom<EPEarningType>.View.Select(this).FirstTableItems.Where(x => !earningTypesWithExistingRecord.Contains(x.TypeCD)))
			{
				PREarningTypeDetail detail = PREarningTypeMaint.CreateEarningTypeDetail<PREarningTypeDetail>(earningType, e.Row);
				if (detail != null)
				{
					detail = EarningTypeDetails.Insert(detail);
					// Acuminator disable once PX1043 SavingChangesInEventHandlers
					// [e.TranStatus == Open is checked in the first line of this method]
					EarningTypeDetails.Cache.PersistInserted(detail);
				}
			}

			HashSet<int?> deductCodesWithExistingRecord = DeductCodeDetails.Select(e.Row.TaxID, LocationConstants.CanadaCountryCode).FirstTableItems
				.Select(x => x.CodeID).ToHashSet();

			foreach (PRDeductCode deductCode in SelectFrom<PRDeductCode>
				.Where<PRDeductCode.countryID.IsEqual<BQLLocationConstants.CountryCAN>
					.And<PRDeductCode.benefitTypeCDCAN.IsNotNull>>
				.View.Select(this).FirstTableItems.Where(x => !deductCodesWithExistingRecord.Contains(x.CodeID)))
			{
				PRDeductCodeDetail detail = new PRDeductCodeDetail()
				{
					CodeID = deductCode.CodeID,
					TaxID = e.Row.TaxID
				};

				PRDedBenCodeMaint.SetDetailTaxability(deductCode.BenefitTypeCDCAN.Value, e.Row, LocationConstants.CanadaCountryCode, detail);
				detail = DeductCodeDetails.Insert(detail);
				// Acuminator disable once PX1043 SavingChangesInEventHandlers
				// [e.TranStatus == Open is checked in the first line of this method]
				DeductCodeDetails.Cache.PersistInserted(detail);
			}
		}
		#endregion Events

		#region Helpers
		protected virtual void CreateTaxesForAllUSLocations(out List<PRTaxCode> newTaxes, out List<PRTaxCode> updatedTaxes)
		{
			newTaxes = new List<PRTaxCode>();
			updatedTaxes = new List<PRTaxCode>();

			List<Address> addresses = Addresses.Select().FirstTableItems.ToList();
			addresses = TaxLocationHelpers.GetUpdatedAddressLocationCodes(addresses);
			List<Address> usAddresses = addresses.Where(x => x.CountryID == LocationConstants.USCountryCode).ToList();

			string includeRailroadTaxesSettingName = GetIncludeRailroadTaxesSettingName();
			bool includeRailroadTaxes =
				SelectFrom<PRCompanyTaxAttribute>
					.Where<PRCompanyTaxAttribute.settingName.IsEqual<P.AsString>>.View
					.Select(this, includeRailroadTaxesSettingName).FirstTableItems
					.Any(x => bool.TryParse(x.Value, out bool boolValue) && boolValue)
				|| SelectFrom<PREmployeeAttribute>
					.InnerJoin<PREmployee>.On<PREmployee.bAccountID.IsEqual<PREmployeeAttribute.bAccountID>>
					.Where<PREmployeeAttribute.settingName.IsEqual<P.AsString>
						.And<PREmployee.activeInPayroll.IsEqual<True>>>.View
					.Select(this, includeRailroadTaxesSettingName).FirstTableItems
					.Any(x => bool.TryParse(x.Value, out bool boolValue) && boolValue);
			List<PRTaxCode> existingTaxes = AllTaxes.Select().FirstTableItems.ToList();
			var payrollService = new PayrollTaxClient(Filter.Current.CountryID);
			List<WSTaxType> webServiceTaxes = payrollService.GetAllLocationTaxTypes(usAddresses, includeRailroadTaxes)
				.Distinct(new TaxTypeEqualityComparer())
				.ToList();
			foreach (WSTaxType taxType in webServiceTaxes)
			{
				PRTaxCode existingTax = existingTaxes.FirstOrDefault(y => y.TaxUniqueCode == taxType.UniqueTaxID);
				if (existingTax == null)
				{
					if (taxType.IsImplemented)
					{
						ParseSymmetryTax(taxType, out string stateAbbr, out string localTaxID, out string municipalCode);
						PRTaxCode newTaxCode = CreateTax(taxType, stateAbbr, localTaxID, municipalCode);
						if (newTaxCode != null)
						{
							newTaxes.Add(newTaxCode);
						}
					}
					else
					{
						PXTrace.WriteWarning(Messages.TaxTypeIsNotImplemented, taxType.TaxID);
					} 
				}
				else
				{
					bool updated = false;
					if (existingTax.TypeName != taxType.TypeName)
				{
					existingTax.TypeName = taxType.TypeName;
						updated = true;
					}
					if (existingTax.JurisdictionLevel != TaxJurisdiction.GetTaxJurisdiction(taxType.TaxJurisdiction))
					{
						existingTax.JurisdictionLevel = TaxJurisdiction.GetTaxJurisdiction(taxType.TaxJurisdiction);
						updated = true;
					}
					if (existingTax.TaxCategory != TaxCategory.GetTaxCategory(taxType.TaxCategory))
					{
						existingTax.TaxCategory = TaxCategory.GetTaxCategory(taxType.TaxCategory);
						updated = true;
					}

					if (updated)
					{
					updatedTaxes.Add(existingTax);
				}
			}
		}
		}

		protected virtual void ParseSymmetryTax(WSTaxType taxType, out string stateAbbr, out string localTaxID, out string municipalCode)
		{
			localTaxID = string.Empty;
			municipalCode = string.Empty;

			string taxJurisdiction = TaxJurisdiction.GetTaxJurisdiction(taxType.TaxJurisdiction);
			if (taxJurisdiction == TaxJurisdiction.Federal)
			{
				stateAbbr = PRSubnationalEntity.GetFederal(Filter.Current.CountryID).Abbr;
			}
			else
			{
				stateAbbr = PRSubnationalEntity.FromLocationCode(Filter.Current.CountryID, int.Parse(taxType.LocationCode.Split('-')[0])).Abbr;
				if (taxJurisdiction == TaxJurisdiction.Local)
				{
					localTaxID = taxType.LocationCode.Split('-')[1];
					if (localTaxID == "000")
					{
						localTaxID = taxType.SchoolDistrictCode;
					}
				}
				else if (taxJurisdiction == TaxJurisdiction.Municipal)
				{
					municipalCode = taxType.LocationCode.Split('-')[2];
				}
			}
		}

		protected virtual PRTaxCode CreateTax(WSTaxType taxType, string stateAbbr, string localTaxID, string municipalCode)
		{
			string taxCD = taxType.TaxID.Replace('_', ' ');
			string taxJurisdiction = TaxJurisdiction.GetTaxJurisdiction(taxType.TaxJurisdiction);
			if (taxJurisdiction != TaxJurisdiction.Federal)
			{
				taxCD = string.Join(" ", stateAbbr, taxCD);
				if (taxJurisdiction == TaxJurisdiction.Local)
				{
					taxCD = string.Join(" ", taxCD, localTaxID);
				}
				else if (taxJurisdiction == TaxJurisdiction.Municipal)
				{
					taxCD = string.Join(" ", taxCD, municipalCode);
				}
				else if (taxJurisdiction == TaxJurisdiction.SchoolDistrict)
				{
					taxCD = string.Join(" ", taxCD, taxType.SchoolDistrictCode);
				}
			}

			PRTaxCode newTaxCode = new PRTaxCode();
			int taxCDFieldLength = Taxes.Cache.GetAttributesOfType<PXDBStringAttribute>(newTaxCode, nameof(PRTaxCode.taxCD)).First().Length;
			newTaxCode.TaxCD = taxCD.Length > taxCDFieldLength ? taxCD.Substring(0, taxCDFieldLength) : taxCD;

			newTaxCode.JurisdictionLevel = taxJurisdiction;
			newTaxCode.TaxCategory = TaxCategory.GetTaxCategory(taxType.TaxCategory);
			newTaxCode.TypeName = taxType.TypeName;
			newTaxCode.TaxUniqueCode = taxType.UniqueTaxID;
			newTaxCode.TaxTypeDescription = taxType.TaxIDDescription;
			if (taxJurisdiction != TaxJurisdiction.Federal)
			{
				newTaxCode.TaxState = stateAbbr;
			}

			int descriptionLength = Taxes.Cache.GetAttributesOfType<PXDBStringAttribute>(newTaxCode, nameof(PRTaxCode.description)).First().Length;
			newTaxCode.Description = taxType.Description.Length > descriptionLength ? taxType.Description.Substring(0, descriptionLength) : taxType.Description;
			return newTaxCode;
		}

		protected virtual void CreateTaxes(List<WSTaxType> applicableTaxes, out List<PRTaxCode> newTaxes, out List<PRTaxCode> updatedTaxes)
		{
			newTaxes = new List<PRTaxCode>();
			updatedTaxes = new List<PRTaxCode>();
			Dictionary<string, PRTaxCode> existingTaxes = AllTaxes.Select().FirstTableItems.ToDictionary(k => k.TaxUniqueCode, v => v);
			foreach (WSTaxType webServiceTax in applicableTaxes)
			{
				if (!existingTaxes.TryGetValue(webServiceTax.UniqueTaxID, out PRTaxCode existingTax))
				{
					ParseVertexTax(webServiceTax, out string stateAbbr, out string localTaxID, out string municipalCode);
					PRTaxCode newTaxCode = CreateTax(webServiceTax, stateAbbr, localTaxID, municipalCode);
					if (newTaxCode != null)
					{
						newTaxes.Add(newTaxCode);
					}
				}
				else if (existingTax.TaxCategory != TaxCategory.GetTaxCategory(webServiceTax.TaxCategory)
					|| existingTax.JurisdictionLevel != TaxJurisdiction.GetTaxJurisdiction(webServiceTax.TaxJurisdiction)
					|| existingTax.TaxTypeDescription != webServiceTax.TaxIDDescription)
				{
					existingTax.TaxCategory = TaxCategory.GetTaxCategory(webServiceTax.TaxCategory);
					existingTax.JurisdictionLevel = TaxJurisdiction.GetTaxJurisdiction(webServiceTax.TaxJurisdiction);
					existingTax.TaxTypeDescription = webServiceTax.TaxIDDescription;
					updatedTaxes.Add(existingTax);
				}
			}
		}

		protected virtual void ParseVertexTax(WSTaxType taxType, out string stateAbbr, out string localTaxID, out string municipalCode)
		{
			localTaxID = string.Empty;
			municipalCode = string.Empty;

			GeoCode geo = new GeoCodeParser(taxType.LocationCode);
			string taxJurisdiction = TaxJurisdiction.GetTaxJurisdiction(taxType.TaxJurisdiction);
			if (taxJurisdiction == TaxJurisdiction.Federal)
			{
				stateAbbr = PRSubnationalEntity.GetFederal(Filter.Current.CountryID).Abbr;
			}
			else
			{
				uint stateCode = Filter.Current.CountryID == LocationConstants.USCountryCode ? geo.StateCode : geo.CountyCode;
				stateAbbr = PRSubnationalEntity.FromLocationCode(Filter.Current.CountryID, (int)stateCode).Abbr;
			}

			if (taxJurisdiction == TaxJurisdiction.Local)
			{
				localTaxID = geo.CountyCode.ToString();
			}
			else if (taxJurisdiction == TaxJurisdiction.Municipal)
			{
				municipalCode = geo.CityCode.ToString();
			}
		}

		protected virtual void ValidateTaxAttributes(List<PRTaxCode> taxes)
		{
			foreach (PRTaxCode taxCodeWithError in GetTaxAttributeErrors(taxes).Where(x => x.ErrorLevel != null && x.ErrorLevel != (int?)PXErrorLevel.Undefined))
			{
				SetTaxCodeError(Taxes.Cache, taxCodeWithError);
			}
		}

		protected virtual void SetTaxCodeError(PXCache cache, PRTaxCode taxCode)
		{
			(string previousErrorMsg, PXErrorLevel previousErrorLevel) = PXUIFieldAttribute.GetErrorWithLevel<PRTaxCode.taxCD>(cache, taxCode);
			bool previousErrorIsRelated = previousErrorMsg == Messages.ValueBlankAndRequired || previousErrorMsg == Messages.NewTaxSetting;

			if (taxCode.ErrorLevel == (int?)PXErrorLevel.RowError)
			{
				PXUIFieldAttribute.SetError(cache, taxCode, nameof(taxCode.TaxCD), Messages.ValueBlankAndRequired, taxCode.TaxCD, false, PXErrorLevel.RowError);
			}
			else if ((taxCode.ErrorLevel == (int?)PXErrorLevel.RowWarning || cache.GetStatus(taxCode) == PXEntryStatus.Inserted) &&
				(previousErrorLevel != PXErrorLevel.RowError || previousErrorIsRelated))
			{
				PXUIFieldAttribute.SetError(cache, taxCode, nameof(taxCode.TaxCD), Messages.NewTaxSetting, taxCode.TaxCD, false, PXErrorLevel.RowWarning);
			}
			else if (taxCode.ErrorLevel == (int?)PXErrorLevel.Undefined && previousErrorIsRelated)
			{
				PXUIFieldAttribute.SetError(cache, taxCode, nameof(taxCode.TaxCD), "", taxCode.TaxCD, false, PXErrorLevel.Undefined);
			}
		}

		protected virtual IEnumerable<PRTaxCode> GetTaxAttributeErrors(List<PRTaxCode> taxes)
		{
			PRTaxCode restoreCurrent = Taxes.Current;
			try
			{
				foreach (PRTaxCode taxCode in taxes)
				{
					Taxes.Current = taxCode;
					foreach (PRTaxCodeAttribute taxAttribute in TaxAttributes.Select().FirstTableItems)
					{
						// Raising FieldSelecting on PRTaxCodeAttribute will set error on the attribute and propagate
						// the error/warning to the tax code
						object value = taxAttribute.Value;
						TaxAttributes.Cache.RaiseFieldSelecting<PRTaxCodeAttribute.value>(taxAttribute, ref value, false);
					}

					yield return taxCode;
				}
			}
			finally
			{
				Taxes.Current = restoreCurrent;
			}
		}

		protected static void AssignEmployeeTaxes(List<PREmployee> list)
		{
			PREmployeePayrollSettingsMaint employeeGraph = PXGraph.CreateInstance<PREmployeePayrollSettingsMaint>();
			foreach (PREmployee employee in list)
			{
				try
				{
					PXProcessing.SetCurrentItem(employee);
					employeeGraph.Clear();
					employeeGraph.CurrentPayrollEmployee.Current = employee;
					employeeGraph.ImportTaxesProc(true);
					employeeGraph.Persist();
					PXProcessing.SetProcessed();
				}
				catch
				{
					PXProcessing.SetError(list.IndexOf(employee), Messages.CantAssignTaxesToEmployee);
				}
			}
		}

		protected virtual void AdjustTaxCDForDuplicate(PRTaxCode row)
		{
			int similarTaxCodes = SelectFrom<PRTaxCode>.View.Select(this).FirstTableItems.Count(x => x.TaxCD.StartsWith(row.TaxCD));
			if (similarTaxCodes > 0)
			{
				int taxCDFieldLength = Taxes.Cache.GetAttributesOfType<PXDBStringAttribute>(row, nameof(PRTaxCode.taxCD)).First().Length;
				row.TaxCD = row.TaxCD.Length >= taxCDFieldLength ? row.TaxCD.Substring(0, taxCDFieldLength - 1) : row.TaxCD;
				row.TaxCD = $"{row.TaxCD}{(char)('a' + similarTaxCodes)}";
			}
		}

		public static string GetIncludeRailroadTaxesSettingName()
		{
			var metaAttr = new EmployeeLocationSettingsAttribute(LocationConstants.USFederalStateCode, string.Empty);
			return MetaDynamicSetting<EmployeeLocationSettingsAttribute>.GetUniqueSettingName(metaAttr, WebserviceContants.IncludeRailroadTaxesSetting);
		}

		public virtual void UpdateEarningTypeTaxability()
		{
			PREarningTypeMaint earningTypeGraph = CreateInstance<PREarningTypeMaint>();
			foreach (EPEarningType earningType in SelectFrom<EPEarningType>.View.Select(this))
			{
				PREarningType prEarningType = PXCache<EPEarningType>.GetExtension<PREarningType>(earningType);
				if (prEarningType == null)
				{
					continue;
				}

				earningTypeGraph.EarningTypes.Current = earningType;
				earningTypeGraph.UpdateTaxabilitySettings(prEarningType);
				earningTypeGraph.InsertMissingTaxDetails(earningType, false);
			}

			earningTypeGraph.Actions.PressSave();
		}

		public virtual void UpdateDeductionCodeTaxability()
		{
			PRDedBenCodeMaint deductCodeGraph = CreateInstance<PRDedBenCodeMaint>();
			foreach (PRDeductCode deductCode in SelectFrom<PRDeductCode>.View.Select(this))
			{
				deductCodeGraph.Document.Current = deductCode;
				deductCodeGraph.UpdateCANTaxabilitySettings(deductCode);
				deductCodeGraph.InsertMissingCANTaxDetails(deductCode, false);
			}

			deductCodeGraph.Actions.PressSave();
		}
		#endregion Helpers

		#region Helper classes
		private class TaxTypeEqualityComparer : IEqualityComparer<WSTaxType>
		{
			public bool Equals(WSTaxType x, WSTaxType y)
			{
				return x.UniqueTaxID == y.UniqueTaxID;
			}

			public int GetHashCode(WSTaxType obj)
			{
				return obj.UniqueTaxID.GetHashCode();
			}
		}

		private class UpdateTaxesCustomInfo
		{
			public List<PRTaxCode> NewTaxes;
			public List<PRTaxCode> UpdatedTaxes;
			public bool ValidateTaxesNeeded = true;

			public UpdateTaxesCustomInfo(List<PRTaxCode> newTaxes, List<PRTaxCode> updatedTaxes)
			{
				NewTaxes = newTaxes;
				UpdatedTaxes = updatedTaxes;
			}
		}
		
		public class InvokablePXProcessing<TTable, TWhere> : PXProcessing<TTable, TWhere>
			where TTable : class, IBqlTable, new()
			where TWhere : IBqlWhere, new()
		{
			public InvokablePXProcessing(PXGraph graph) : base(graph) { }

			public IEnumerable Invoke(PXAdapter adapter)
			{
				return ProcessAll(adapter);
			}
		}

		[PXHidden]
		protected class BackgroundTaxDataUpdate : PXGraph<BackgroundTaxDataUpdate>
		{
			public SelectFrom<PRTaxUpdateHistory>.View TaxUpdateHistory;
			public SelectFrom<PRTaxSettingAdditionalInformation>.View TaxSettingAdditionalInformation;
			public SelectFrom<PRTaxWebServiceData>
				.Where<PRTaxWebServiceData.countryID.IsEqual<P.AsString>>.View TaxWebServiceData;

			public void UpdateUSBackgroundData()
			{
				PayrollUpdateClient updateClient = new PayrollUpdateClient();
				PRTaxUpdateHistory updateHistory = TaxUpdateHistory.SelectSingle() ?? new PRTaxUpdateHistory();
				DateTime utcNow = DateTime.UtcNow;
				updateHistory.LastCheckTime = utcNow;
				updateHistory.ServerTaxDefinitionTimestamp = updateClient.GetTaxDefinitionTimestamp();
				updateHistory.LastUpdateTime = utcNow;
				TaxUpdateHistory.Update(updateHistory);

				Dictionary<TaxSettingAdditionalInformationKey, TaxSettingDescription> additionalDescriptions;
				using (CsvReader reader = new CsvReader(new StreamReader(new MemoryStream(updateClient.GetTaxSettingsAdditionalDescription()), Encoding.UTF8)))
				{
					additionalDescriptions = reader.GetRecords<TaxSettingDescription>().ToDictionary(k => new TaxSettingAdditionalInformationKey(k), v => v);
				}
				UpdateCompanyTaxAttributeDescriptions(additionalDescriptions);
			}

			public void UpdateCanadaBackgroundData(List<WSTaxType> applicableTaxes)
			{
				// Merge list of applicable taxes from addresses with list of existing taxes to get
				// tax settings for old and new taxes
				IEnumerable<string> existingTaxIDs = SelectFrom<PRTaxCode>
					.Where<PRTaxCode.countryID.IsEqual<BQLLocationConstants.CountryCAN>>.View.Select(this).FirstTableItems.Select(x => x.TaxUniqueCode);
				List<string> applicableTaxIDs = applicableTaxes.Select(x => x.UniqueTaxID).Union(existingTaxIDs).ToList();

				PRWebServiceRestClient restClient = new PRWebServiceRestClient();
				List<PRCanadaProvince> provinces = restClient.GetSubnationalEntities<PRCanadaProvince>(LocationConstants.CanadaCountryCode).ToList();
				List<TaxSetting> taxSettings = restClient.GetTaxSettings(LocationConstants.CanadaCountryCode, applicableTaxIDs).ToList();
				List<DeductionType> deductionTypes = restClient.GetDeductionTypes(LocationConstants.CanadaCountryCode, applicableTaxIDs).ToList();
				List<WageType> wageTypes = restClient.GetWageTypes(applicableTaxIDs).ToList();
				List<ReportingType> reportingTypes = restClient.GetReportingTypes(LocationConstants.CanadaCountryCode).ToList();

				PRTaxWebServiceData webServiceDataInDB = TaxWebServiceData.SelectSingle(LocationConstants.CanadaCountryCode)
					?? new PRTaxWebServiceData() { CountryID = LocationConstants.CanadaCountryCode };
				webServiceDataInDB.States = JsonConvert.SerializeObject(provinces);
				webServiceDataInDB.TaxSettings = JsonConvert.SerializeObject(taxSettings);
				webServiceDataInDB.DeductionTypes = JsonConvert.SerializeObject(deductionTypes);
				webServiceDataInDB.WageTypes = JsonConvert.SerializeObject(wageTypes);
				webServiceDataInDB.ReportingTypes = JsonConvert.SerializeObject(reportingTypes);

				Dictionary<TaxSettingAdditionalInformationKey, TaxSettingDescription> additionalDescriptions = taxSettings
					.Select(
						x => new TaxSettingDescription()
						{
							AdditionalInformation = x.AdditionalInformation,
							FormBox = x.FormBox,
							UsedForSymmetry = x.UsedForTaxCalculation ?? true,
							TypeName = x.TypeName,
							SettingName = x.SettingName,
							State = x.State,
							CountryID = LocationConstants.CanadaCountryCode
						})
					.ToDictionary(k => new TaxSettingAdditionalInformationKey(k), v => v);
				UpdateCompanyTaxAttributeDescriptions(additionalDescriptions);

				TaxWebServiceData.Update(webServiceDataInDB);

				PRTaxUpdateHistory updateHistory = TaxUpdateHistory.SelectSingle() ?? new PRTaxUpdateHistory();
				DateTime utcNow = DateTime.UtcNow;
				updateHistory.LastCheckTime = utcNow;
				updateHistory.ServerTaxDefinitionTimestamp = restClient.GetTaxDefinitionTimestamp(LocationConstants.CanadaCountryCode);
				updateHistory.LastUpdateTime = utcNow;
				TaxUpdateHistory.Update(updateHistory);
			}

			protected virtual void UpdateCompanyTaxAttributeDescriptions(Dictionary<TaxSettingAdditionalInformationKey, TaxSettingDescription> additionalDescriptions)
			{
				List<PRTaxSettingAdditionalInformation> settingAdditionalInformation = TaxSettingAdditionalInformation.Select().FirstTableItems.ToList();
				foreach (PRTaxSettingAdditionalInformation setting in settingAdditionalInformation)
				{
					if (additionalDescriptions.TryGetValue(new TaxSettingAdditionalInformationKey(setting), out TaxSettingDescription definition))
					{
						setting.AdditionalInformation = definition.AdditionalInformation;
						setting.UsedForTaxCalculation = definition.UsedForSymmetry;
						setting.FormBox = definition.FormBox;
						TaxSettingAdditionalInformation.Update(setting);
					}
				}

				HashSet<TaxSettingAdditionalInformationKey> settingsDefinedInDB = settingAdditionalInformation.Select(x => new TaxSettingAdditionalInformationKey(x)).ToHashSet();
				foreach (TaxSettingDescription newDefinition in additionalDescriptions.Values.Where(x => !settingsDefinedInDB.Contains(new TaxSettingAdditionalInformationKey(x))))
				{
					TaxSettingAdditionalInformation.Insert(new PRTaxSettingAdditionalInformation()
					{
						TypeName = newDefinition.TypeName,
						SettingName = newDefinition.SettingName,
						AdditionalInformation = newDefinition.AdditionalInformation,
						UsedForTaxCalculation = newDefinition.UsedForSymmetry,
						FormBox = newDefinition.FormBox,
						State = string.IsNullOrEmpty(newDefinition.State) ? null : newDefinition.State,
						CountryID = newDefinition.CountryID
					});
				}

				TaxSettingAdditionalInformation.Cache.Persist(PXDBOperation.Insert);
				TaxSettingAdditionalInformation.Cache.Persist(PXDBOperation.Update);
			}

			public void _(Events.RowPersisting<PRTaxWebServiceData> e)
			{
				PRSubnationalEntity.ClearCachedEntities();
			}
		}
		#endregion
	}

	[PXHidden]
	[Serializable]
	public class PRTaxMaintenanceFilter : IBqlTable
	{
		#region CountryID
		public abstract class countryID : PX.Data.BQL.BqlString.Field<countryID> { }
		[PXString(2, IsFixed = true)]
		[PRCountry]
		[PXUIField(Visible = false)]
		public virtual string CountryID { get; set; }
		#endregion
		#region FilterStates
		[PXBool]
		[PXUnboundDefault(false)]
		[PXUIField(DisplayName = "Show Attributes Only for States That Have Tax Codes Set Up")]
		public bool? FilterStates { get; set; }
		public abstract class filterStates : PX.Data.BQL.BqlBool.Field<filterStates> { }
		#endregion
	}
}
