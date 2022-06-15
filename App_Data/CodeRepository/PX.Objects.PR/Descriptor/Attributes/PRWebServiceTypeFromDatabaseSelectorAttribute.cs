using Newtonsoft.Json;
using PX.Data;
using PX.Data.BQL;
using PX.Data.BQL.Fluent;
using PX.Payroll.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.PR
{
	public class PRWebServiceTypeFromDatabaseSelectorAttribute : PXCustomSelectorAttribute, IPXFieldDefaultingSubscriber
	{
		private PRTaxWebServiceDataSlot.DataType _WebServiceType;
		private string _CountryID;
		private bool _UseDefault;

		public PRWebServiceTypeFromDatabaseSelectorAttribute(PRTaxWebServiceDataSlot.DataType webServiceType, string countryID, bool useDefault)
			: base(typeof(PRDynType.id))
		{
			_WebServiceType = webServiceType;
			_CountryID = countryID;
			_UseDefault = useDefault;
			SubstituteKey = typeof(PRDynType.name);

			if (PRTaxWebServiceDataSlot.GetDynamicTypeData(_CountryID, _WebServiceType).Any(x => !string.IsNullOrEmpty(x.Description)))
			{
				DescriptionField = typeof(PRDynType.description);
				_FieldList = new[] { nameof(PRDynType.Name), nameof(PRDynType.Description) };
			}
			else
			{
				DescriptionField = null;
				_FieldList = new[] { nameof(PRDynType.Name) };
			}
		}

		public void FieldDefaulting(PXCache sender, PXFieldDefaultingEventArgs e)
		{
			if (!_UseDefault)
			{
				return;
			}

			IEnumerable<IDynamicType> cachedData = PRTaxWebServiceDataSlot.GetDynamicTypeData(_CountryID, _WebServiceType);
			if (!cachedData.Any())
			{
				return;
			}

			e.NewValue = cachedData.Min(x => x.TypeID);
		}

		protected virtual IEnumerable GetRecords()
		{
			IEnumerable<IDynamicType> cachedData = PRTaxWebServiceDataSlot.GetDynamicTypeData(_CountryID, _WebServiceType);
			if (!cachedData.Any())
			{
				return new object[] { };
			}

			return cachedData.GroupBy(x => x.TypeID).Select(x => new PRDynType()
			{
				ID = x.Key,
				Name = x.First().TypeName,
				Description = x.First().Description
			});
		}

		public override void SubstituteKeyCommandPreparing(PXCache sender, PXCommandPreparingEventArgs e) { }
	}
}
