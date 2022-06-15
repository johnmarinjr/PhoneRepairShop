using Newtonsoft.Json;
using PX.Data;
using PX.Payroll.Data;
using PX.Payroll.Data.Vertex;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PX.Objects.PR
{
	public class PRTaxWebServiceDataSlot : IPrefetchable<string>
	{
		public enum DataType
		{
			WageTypes,
			DeductionTypes,
			ReportingTypes
		}

		public class DeductionTypeKey : IEquatable<DeductionTypeKey>
		{
			public int DeductionTypeID;
			public string TaxID;

			public DeductionTypeKey(int deductionTypeID, string taxID) => (DeductionTypeID, TaxID) = (deductionTypeID, taxID);

			public bool Equals(DeductionTypeKey other)
			{
				return other.DeductionTypeID == DeductionTypeID && other.TaxID == TaxID;
			}

			public override int GetHashCode()
			{
				unchecked
				{
					int hash = 17;
					hash = hash * 23 + DeductionTypeID.GetHashCode();
					hash = hash * 23 + (TaxID?.GetHashCode() ?? 0);
					return hash;
				}
			}
		}

		private CachedData _Data;

		public static CachedData GetData(string countryID) => 
			PXDatabase.GetSlot<PRTaxWebServiceDataSlot, string>(nameof(PRTaxWebServiceDataSlot), countryID, typeof(PRTaxWebServiceData))._Data;

		public static IEnumerable<IDynamicType> GetDynamicTypeData(string countryID, DataType dataType)
		{
			CachedData fullData = GetData(countryID);
			if (fullData == null)
			{
				return new List<IDynamicType>();
			}

			switch (dataType)
			{
				case DataType.WageTypes:
					return fullData.WageTypes.Values;
				case DataType.DeductionTypes:
					return fullData.DeductionTypes.Values;
				case DataType.ReportingTypes:
					return fullData.ReportingTypes.Values;
				default:
					throw new PXException(Messages.WebServiceDataTypeNotRecognized, dataType);
			}
		}

		public void Prefetch(string countryID)
		{
			PXDataRecord rec = PXDatabase.SelectSingle<PRTaxWebServiceData>(
				new PXDataField<PRTaxWebServiceData.wageTypes>(),
				new PXDataField<PRTaxWebServiceData.deductionTypes>(),
				new PXDataField<PRTaxWebServiceData.reportingTypes>(),
				new PXDataFieldValue<PRTaxWebServiceData.countryID>(countryID));
			
			if (rec == null)
			{
				_Data = null;
			}
			else
			{
				_Data = new CachedData()
				{
					WageTypes = JsonConvert.DeserializeObject<IEnumerable<WageType>>(rec.GetString(0)).ToDictionary(k => k.TypeID, v => v),
					DeductionTypes = JsonConvert.DeserializeObject<IEnumerable<DeductionType>>(rec.GetString(1)).ToDictionary(k => new DeductionTypeKey(k.TypeID, k.TaxID), v => v),
					ReportingTypes = JsonConvert.DeserializeObject<IEnumerable<ReportingType>>(rec.GetString(2)).ToDictionary(k => k.TypeID, v => v),
				};
			}
		}

		public class CachedData
		{
			public Dictionary<int, WageType> WageTypes { get; set; }
			public Dictionary<DeductionTypeKey, DeductionType> DeductionTypes { get; set; }
			public Dictionary<int, ReportingType> ReportingTypes { get; set; }
		}
	}
}
