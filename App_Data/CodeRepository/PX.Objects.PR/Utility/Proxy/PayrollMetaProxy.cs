using PX.Payroll.Proxy;
using PX.Payroll;
using PX.Payroll.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace PX.Objects.PR
{
	[Serializable]
	public class PayrollMetaProxy : PayrollBaseProxy<PayrollMetaProxy>
	{
		public DynamicEntity<TDynamicType, TTypeMeta>.Meta GetMeta<TDynamicType, TTypeMeta>(IPRReflectiveSettingMapper<TTypeMeta> typeSetting)
			where TDynamicType : DynamicEntity<TDynamicType, TTypeMeta>
			where TTypeMeta : SettingDefinitionListAttribute
		{
			return DynamicEntity<TDynamicType, TTypeMeta>.Meta.Get(DynamicAssemblies, typeSetting);
		}

		public Dictionary<IPRReflectiveSettingMapper<TTypeMeta>, DynamicEntity<TDynamicType, TTypeMeta>.MetaWithSetting> GetMetaWithSettings<TDynamicType, TTypeMeta>(IPRReflectiveSettingMapper<TTypeMeta>[] typeSettings)
			where TDynamicType : DynamicEntity<TDynamicType, TTypeMeta>
			where TTypeMeta : SettingDefinitionListAttribute
		{
			var settings = new Dictionary<IPRReflectiveSettingMapper<TTypeMeta>, DynamicEntity<TDynamicType, TTypeMeta>.MetaWithSetting>();
			foreach (IPRReflectiveSettingMapper<TTypeMeta> settingMapper in typeSettings)
			{
				if (settingMapper.ReflectiveUniqueCode != null)
				{
					settings.Add(settingMapper, GetMetaWithSetting<TDynamicType, TTypeMeta>(settingMapper));
				}
			}

			return settings;
		}

		public DynamicEntity<TDynamicType, TTypeMeta>.MetaWithSetting GetMetaWithSetting<TDynamicType, TTypeMeta>(IPRReflectiveSettingMapper<TTypeMeta> typeSetting)
			where TDynamicType : DynamicEntity<TDynamicType, TTypeMeta>
			where TTypeMeta : SettingDefinitionListAttribute
		{
			return DynamicEntity<TDynamicType, TTypeMeta>.MetaWithSetting.Get(DynamicAssemblies, typeSetting);
		}

			public MetaDynamicEntityDictionary<DynamicEntity<TDynamicType, TTypeMeta>.Meta> GetAllMeta<TDynamicType, TTypeMeta>()
			where TDynamicType : DynamicEntity<TDynamicType, TTypeMeta>
			where TTypeMeta : SettingDefinitionListAttribute
		{
			return DynamicEntity<TDynamicType, TTypeMeta>.Meta.GetAll(DynamicAssemblies);
		}

		public MetaDynamicEntityDictionary<DynamicEntity<TDynamicType, TTypeMeta>.MetaWithSetting> GetAllMetaWithSettings<TDynamicType, TTypeMeta>()
			where TDynamicType : DynamicEntity<TDynamicType, TTypeMeta>
			where TTypeMeta : SettingDefinitionListAttribute
		{
			return DynamicEntity<TDynamicType, TTypeMeta>.MetaWithSetting.GetAll(DynamicAssemblies);
		}

		public List<PRTypeMeta> GetPRTypeMeta<TTypeClass>(bool useReportingType)
			where TTypeClass : IPRTypeClass
		{
			if (useReportingType)
			{
				return PRReportingType.GetList<TTypeClass>(DynamicAssemblies).ToList();
			}
			else
			{
				return PRType.GetList<TTypeClass>(DynamicAssemblies).ToList();
			}
		}

		public List<PRTypeMeta> GetPRTypeMeta(Type typeClass, bool useReportingType)
		{
			if (useReportingType)
			{
				return PRReportingType.GetList(typeClass, DynamicAssemblies).ToList();
			}
			else
			{
				return PRType.GetList(typeClass, DynamicAssemblies).ToList();
			}
		}

		public List<PRSubnationalEntity> InitializeStateDefinitions(Func<List<Assembly>, List<PRSubnationalEntity>> stateDefinitionDelegate)
		{
			List<Assembly> pxAssemblies = DynamicAssemblies.Where(assembly => assembly.GetName().Name.StartsWith("PX")).ToList();
			return stateDefinitionDelegate(pxAssemblies);
		}
	}
}
