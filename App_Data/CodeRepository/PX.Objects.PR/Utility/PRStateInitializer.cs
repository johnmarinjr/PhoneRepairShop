using PX.Payroll.Proxy;
using PX.Payroll.Data;
using System;
using System.Collections.Generic;
using System.Reflection;
using PX.Data;
using Newtonsoft.Json;
using System.Collections;

namespace PX.Objects.PR
{
	[Serializable]
	public class PRStateInitializer : PXPayrollAssemblyScope<PayrollMetaProxy>, IStateDefinitionInitializer
	{
		public List<PRSubnationalEntity> InitializeStateDefinitions(Func<List<Assembly>, List<PRSubnationalEntity>> stateDefinitionDelegate)
		{
			return CreateProxy().InitializeStateDefinitions(stateDefinitionDelegate);
		}
	}

	public class DatabaseSubnationalEntityDefinitionInitializer : IDatabaseSubnationalEntityDefinitionInitializer
	{
		public List<PRSubnationalEntity> InitializeSubnationalEntityDefinitionsFromDatabase(string countryID)
		{
			List<PRSubnationalEntity> entities = new List<PRSubnationalEntity>();

			string jsonStateList;
			using (PXDataRecord row = PXDatabase.SelectSingle<PRTaxWebServiceData>(
					new PXDataField(typeof(PRTaxWebServiceData.states).Name),
					new PXDataFieldValue<PRTaxWebServiceData.countryID>(countryID)))
			{
				jsonStateList = row?.GetString(0);
			}

			if (!string.IsNullOrEmpty(jsonStateList))
			{
				Type entityType = PRSubnationalEntity.GetCountryDataType(countryID);
				if (entityType != null)
				{
					Type deserializeType = typeof(IEnumerable<>).MakeGenericType(entityType);
					object deserialized = JsonConvert.DeserializeObject(jsonStateList, deserializeType);
					if (deserialized is IEnumerable deserializedList)
					{
						foreach (object item in deserializedList)
						{
							if (item is PRSubnationalEntity entity)
							{
								entities.Add(entity);
							}
						}
					}
				}
			}

			return entities;
		}
	}
}
