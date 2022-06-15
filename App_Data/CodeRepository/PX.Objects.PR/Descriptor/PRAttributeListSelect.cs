using PX.Common;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Payroll.Proxy;
using PX.Payroll;
using PX.Payroll.Data;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Objects.CS;
using PX.Payroll.Data.Vertex;
using Newtonsoft.Json;

namespace PX.Objects.PR
{
	public class MetaTypeSettingCache<TDynamicType, TTypeMeta> : Dictionary<string, MetaTypeSettingCollection<TDynamicType, TTypeMeta>>
		where TDynamicType : DynamicEntity<TDynamicType, TTypeMeta>
		where TTypeMeta : SettingDefinitionListAttribute
	{ }

	public abstract class PRAttributeSelectBase<TEntity, TSelect, TDynamicType, TTypeMeta> : PXSelectBase<TEntity>
		where TEntity : class, IBqlTable, IPRSetting, new()
		where TSelect : class, IBqlSelect, new()
		where TDynamicType : DynamicEntity<TDynamicType, TTypeMeta>
		where TTypeMeta : SettingDefinitionListAttribute
	{
		public const int ParameterIdLength = 10;

		protected virtual MetaTypeSettingCollection<TDynamicType, TTypeMeta> DynamicSettingDefinitions
		{
			get
			{
				MetaTypeSettingCache<TDynamicType, TTypeMeta> metaCache = PXContext.GetSlot<MetaTypeSettingCache<TDynamicType, TTypeMeta>>() ?? CacheMetaSettings();

				var settingDictionary = new MetaTypeSettingCollection<TDynamicType, TTypeMeta>();
				foreach (IPRReflectiveSettingMapper<TTypeMeta> settingMapper in GetMetaTypes().Where(x => !string.IsNullOrEmpty(x.ReflectiveUniqueCode)))
				{
					if (!metaCache.ContainsKey(settingMapper.ReflectiveUniqueCode))
					{
						metaCache = CacheMetaSettings();
					}
					settingDictionary.AddRange(metaCache[settingMapper.ReflectiveUniqueCode]);
				}
				return settingDictionary;
			}
		}

		protected virtual Dictionary<string, TaxSetting> DatabaseSettingDefinitions
		{
			get
			{
				List<TaxSetting> cachedSettings = PXContext.GetSlot<List<TaxSetting>>();
				if (cachedSettings == null)
				{
					cachedSettings = new List<TaxSetting>();
					foreach (PRTaxWebServiceData dbData in SelectFrom<PRTaxWebServiceData>.View.Select(_Graph).FirstTableItems.Where(x => !string.IsNullOrEmpty(x.TaxSettings)))
					{
						cachedSettings.AddRange(JsonConvert.DeserializeObject<IEnumerable<TaxSetting>>(dbData.TaxSettings)
							.Where(x => !string.IsNullOrEmpty(x.SettingName)));
					}

					PXContext.SetSlot(cachedSettings);
				}

				return FilterTaxSettingsInView(cachedSettings).ToDictionary(k => k.SettingName, v => v);
			}
		}

		public PRAttributeSelectBase(PXGraph graph)
		{
			_Graph = graph;

			var command = BqlCommand.CreateInstance(typeof(TSelect));
			View = new PXView(graph, false, command, new PXSelectDelegate(viewHandler));
			_Graph.FieldSelecting.AddHandler(typeof(TEntity), "Value", ValueFieldSelecting);
			_Graph.FieldSelecting.AddHandler(typeof(TEntity), "Description", DescriptionFieldSelecting);
			Cache.AllowInsert = _Graph.IsImport;
			Cache.AllowDelete = false;
		}

		protected IEnumerable viewHandler()
		{
			List<TEntity> savedValuesList = GetSavedValues();
			IEnumerable<ISettingDefinition> definitionList = GetAttributeDefinitionList().ToList();
			Dictionary<TaxSettingAdditionalInformationKey, PRTaxSettingAdditionalInformation> additionalInformation =
				SelectFrom<PRTaxSettingAdditionalInformation>.View.Select(_Graph).FirstTableItems
					.ToDictionary(k => new TaxSettingAdditionalInformationKey(k), v => v);

			//Delete Deprecated records
			foreach (TEntity savedValue in savedValuesList)
			{
				bool found = false;

				foreach (ISettingDefinition definition in definitionList)
				{
					if (savedValue.TypeName == definition.TypeName && savedValue.SettingName == definition.SettingName)
					{
						found = true;
						break;
					}
				}

				if (!found)
				{
					this.Delete(savedValue);
				}
			}

			foreach (ISettingDefinition definition in definitionList)
			{
				//First look for this attribute in the cache - we can't blindly return all
				//values from Cache.Cached since the PRAttributeListSelect can be used
				//in master-detail-detail scenarios (ex: Employee Payroll Setting -> Tax Codes -> Attributes)
				TEntity foundRecord = (TEntity)Cache.CreateInstance();
				foreach (string key in Cache.Keys)
				{
					//This will take care of setting the values for other keys of this TEntity (ex: BAccountID, TaxID for PREmployeeTaxAttribute)
					Cache.SetDefaultExt(foundRecord, key);
				}
				foundRecord.TypeName = definition.TypeName;
				foundRecord.SettingName = definition.SettingName;
				foundRecord = (TEntity)Cache.Locate(foundRecord);
				if (foundRecord != null)
				{
					// Item was found in cache; make sure to set it to Held status again (this will happen after we persist changes)
					if (Cache.GetStatus(foundRecord) == PXEntryStatus.Notchanged || Cache.GetStatus(foundRecord) == PXEntryStatus.Deleted)
					{
						Cache.SetStatus(foundRecord, PXEntryStatus.Held);
					}
				}

				else
				{
					//If not found in cache, look in saved values
					foreach (TEntity savedValue in savedValuesList)
					{
						if (savedValue.TypeName == definition.TypeName && savedValue.SettingName == definition.SettingName)
						{
							//Return from DB saved values
							foundRecord = savedValue;
							Cache.SetStatus(foundRecord, PXEntryStatus.Held);
							break;
						}
					}
				}

				if (foundRecord != null)
				{
					foundRecord.Required = foundRecord.Required ?? definition.Required;
					foundRecord.AllowOverride = foundRecord.AllowOverride ?? definition.AllowOverride;
					foundRecord.SortOrder = foundRecord.SortOrder ?? definition.SortOrder;

					bool setUpdatedStatus = AssignDefault(foundRecord, null) || AssignAdditionalInformation(Cache, foundRecord, additionalInformation);
					if (foundRecord.AatrixMapping != definition.AatrixMapping)
					{
						foundRecord.AatrixMapping = definition.AatrixMapping;
						setUpdatedStatus = true;
					}

					if (setUpdatedStatus)
					{
						Cache.SetStatus(foundRecord, Cache.GetStatus(foundRecord) == PXEntryStatus.Inserted ? PXEntryStatus.Inserted : PXEntryStatus.Updated);
						Cache.IsDirty = true;
					}

					yield return foundRecord;
				}
				else
				{
					// Nothing was found, just insert new row based on current attribute definition
					TEntity newRecord = this.Insert(CreateDacRecord(Cache, definition, additionalInformation));
					yield return newRecord;
				}
			}
		}

		protected virtual List<TEntity> GetSavedValues()
		{
			List<TEntity> cachedRecords = PXContext.GetSlot<List<TEntity>>();
			if (cachedRecords == null || !cachedRecords.Any())
			{
				cachedRecords = PXContext.SetSlot(SelectFrom<TEntity>.View.Select(_Graph).FirstTableItems.ToList());
			}

			PXView view = new PXView(_Graph, true, BqlCommand.CreateInstance(typeof(TSelect)));
			object[] queryParameters = view.PrepareParameters(null, null);
			return cachedRecords.Where(x => view.BqlSelect.Meet(_Graph.Caches[typeof(TEntity)], x, queryParameters)).ToList();
		}

		private TEntity CreateDacRecord(PXCache cache, ISettingDefinition definition, Dictionary<TaxSettingAdditionalInformationKey, PRTaxSettingAdditionalInformation> infoDefinitionDict)
		{
			var attr = (TEntity)cache.CreateInstance();
			attr.TypeName = definition.TypeName;
			attr.SettingName = definition.SettingName;
			attr.AllowOverride = definition.AllowOverride;
			attr.Required = definition.Required;
			attr.UseDefault = (definition.AllowOverride == false);
			attr.SortOrder = definition.SortOrder;
			attr.AatrixMapping = definition.AatrixMapping;
			attr.Value = definition.Value;

			if (attr.Value == null && definition is ISettingFieldDefinition fieldDefinition && fieldDefinition.ControlType == SettingControlType.CheckBox)
			{
				attr.Value = false.ToString();
			}

			if (definition is IStateSpecific)
			{
				cache.SetValue(attr, "State", ((IStateSpecific)definition).State);
			}

			AssignAdditionalInformation(cache, attr, infoDefinitionDict);
			return attr;
		}

		protected virtual void ValueFieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			var row = e.Row as TEntity;
			if (e.Row == null || string.IsNullOrEmpty(row.SettingName))
				return;

			const int answerValueLength = 60;

			ISettingFieldDefinition settingDefinition = GetSettingFieldDefinition(row.SettingName);
			if (settingDefinition != null)
			{
				bool required = AttributeValueIsRequired(row, settingDefinition.ControlType);
				string errorMsg = null;
				PXErrorLevel errorLevel = PXErrorLevel.Undefined;
				if (required && e.ReturnValue == null)
				{
					errorMsg = InvalidAttributeErrorMessage;
					errorLevel = PXErrorLevel.RowError;
				}
				else if (sender.GetStatus(row) == PXEntryStatus.Inserted 
					|| !Equals(Cache.GetValueOriginal(row, nameof(IPRSetting.AdditionalInformation)), row.AdditionalInformation)
					|| !Equals(Cache.GetValueOriginal(row, nameof(IPRSetting.FormBox)), row.FormBox)
					|| sender.Fields.Contains(nameof(PRCompanyTaxAttribute.UsedForTaxCalculation)) && !Equals(sender.GetValueOriginal(row, nameof(PRCompanyTaxAttribute.UsedForTaxCalculation)), sender.GetValue(row, nameof(PRCompanyTaxAttribute.UsedForTaxCalculation))))
				{
					errorMsg = Messages.NewTaxSetting;
					errorLevel = PXErrorLevel.RowWarning;
				}

				if (settingDefinition.ControlType == SettingControlType.Combo)
				{
					List<string> allowedValues = new List<string>();
					List<string> allowedLabels = new List<string>();

					foreach (var option in settingDefinition.ComboListItems)
					{
						allowedValues.Add(option.Value);
						allowedLabels.Add(option.DisplayName);
					}

					e.ReturnState = PXStringState.CreateInstance(e.ReturnValue, ParameterIdLength,
						true, nameof(row.Value), false, required ? 1 : -1, settingDefinition.EntryMask, allowedValues.ToArray(), allowedLabels.ToArray(),
						true, null);
					var stringState = e.ReturnState as PXStringState;
					stringState.Error = errorMsg;
					stringState.ErrorLevel = errorLevel;
				}
				else if (settingDefinition.ControlType == SettingControlType.CheckBox)
				{
					e.ReturnValue = e.ReturnValue ?? false;
					e.ReturnState = PXFieldState.CreateInstance(e.ReturnValue, typeof(bool), false, false, required ? 1 : -1,
						null, null, false, nameof(row.Value), null, null, errorMsg, errorLevel, true, true,
						null, PXUIVisibility.Visible, null, null, null);
				}
				else if (settingDefinition.ControlType == SettingControlType.Datetime)
				{
					e.ReturnState = PXDateState.CreateInstance(e.ReturnValue, nameof(row.Value), false, required ? 1 : -1,
						settingDefinition.EntryMask, settingDefinition.EntryMask, null, null);
					var dateState = e.ReturnState as PXDateState;
					dateState.Error = errorMsg;
					dateState.ErrorLevel = errorLevel;
				}
				else if (settingDefinition.ControlType == SettingControlType.Decimal)
				{
					decimal currentValue;
					if (decimal.TryParse(e.ReturnState as string, out currentValue))
						e.ReturnState = (decimal?)currentValue;

					e.ReturnState = PXFieldState.CreateInstance(e.ReturnValue, typeof(decimal), false, false, required ? 1 : -1,
						settingDefinition.Precision, null, false, nameof(row.Value), null, null, errorMsg, errorLevel, true, true,
						null, PXUIVisibility.Visible, null, null, null);
				}
				else
				{
					//TextBox
					e.ReturnState = PXStringState.CreateInstance(e.ReturnValue, answerValueLength, null,
						nameof(row.Value), false, required ? 1 : -1, settingDefinition.EntryMask, null, null, true, null);
					var stringState = e.ReturnState as PXStringState;
					stringState.Error = errorMsg;
					stringState.ErrorLevel = errorLevel;
				}

				SetErrorLevel(sender, row, errorLevel);
			}
		}

		protected virtual void DescriptionFieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			TEntity row = e.Row as TEntity;
			if (string.IsNullOrEmpty(row?.SettingName))
			{
				return;
			}

			ISettingDefinition settingDefinition = GetSettingDefinition(row.SettingName);
			if (settingDefinition != null)
			{
				e.ReturnValue = settingDefinition.Description;
			}
		}

		protected virtual void SetErrorLevel(PXCache cache, TEntity row, PXErrorLevel errorLevel)
		{
			if (_Graph.IsImport)
			{
				return;
			}

			TEntity originalRow = (TEntity)Cache.CreateCopy(row);
			cache.SetValue(row, nameof(row.ErrorLevel), (int?)errorLevel);
			// To update parent through PXFormula
			if (row.ErrorLevel != originalRow.ErrorLevel)
			{
				cache.RaiseRowUpdated(row, originalRow); 
			}
		}

		protected abstract IEnumerable<ISettingDefinition> GetAttributeDefinitionList();

		protected abstract IEnumerable<IPRReflectiveSettingMapper<TTypeMeta>> GetMetaTypes();
		protected abstract IEnumerable<IPRReflectiveSettingMapper<TTypeMeta>> GetAllMetaTypes();
		protected abstract IEnumerable<TaxSetting> FilterTaxSettingsInView(IEnumerable<TaxSetting> unfiltered);

		protected virtual MetaTypeSettingCache<TDynamicType, TTypeMeta> CacheMetaSettings()
		{
			List<IPRReflectiveSettingMapper<TTypeMeta>> allMetaTypes = GetAllMetaTypes().ToList();
			MetaTypeSettingCache<TDynamicType, TTypeMeta> metaCache = new MetaTypeSettingCache<TDynamicType, TTypeMeta>();
			foreach (KeyValuePair<IPRReflectiveSettingMapper<TTypeMeta>, DynamicEntity<TDynamicType, TTypeMeta>.MetaWithSetting> settingMeta in
				GetMetaSettings(allMetaTypes))
			{
				metaCache[settingMeta.Key.ReflectiveUniqueCode] = PRSettingDefinition<TDynamicType, TTypeMeta>.Convert(settingMeta.Value);
			}

			PXContext.SetSlot(metaCache);
			return metaCache;
		}

		protected virtual bool AssignDefault(TEntity record, IPRSetting _)
		{
			ISettingFieldDefinition settingDefinition = GetSettingFieldDefinition(record.SettingName);
			if (settingDefinition != null && settingDefinition.ControlType == SettingControlType.CheckBox
				&& string.IsNullOrEmpty(record.Value) && record.UseDefault == false)
			{
				record.Value = false.ToString();
				return true;
			}
			return false;
		}

		protected virtual bool AssignAdditionalInformation(PXCache cache, IPRSetting record, Dictionary<TaxSettingAdditionalInformationKey, PRTaxSettingAdditionalInformation> infoDefinitionDict)
		{
			if (record is IStateSpecific)
			{
				object _ = null;
				cache.RaiseFieldSelecting(nameof(IStateSpecific.State), record, ref _, false);
			}

			if (!infoDefinitionDict.TryGetValue(new TaxSettingAdditionalInformationKey(record, true), out PRTaxSettingAdditionalInformation infoDefinition))
			{
				if (!infoDefinitionDict.TryGetValue(new TaxSettingAdditionalInformationKey(record, false), out infoDefinition))
				{
					return false;
				}
			}

			bool updated = false;

			if (record.AdditionalInformation != infoDefinition.AdditionalInformation)
			{
				record.AdditionalInformation = infoDefinition.AdditionalInformation;
				updated = true;
			}

			if (record.FormBox != infoDefinition.FormBox)
			{
				record.FormBox = infoDefinition.FormBox;
				updated = true;
			}

			if (cache.Fields.Contains(nameof(PRCompanyTaxAttribute.UsedForTaxCalculation))
				&& !Equals(cache.GetValue(record, nameof(PRCompanyTaxAttribute.UsedForTaxCalculation)), infoDefinition.UsedForTaxCalculation))
			{
				cache.SetValue(record, nameof(PRCompanyTaxAttribute.UsedForTaxCalculation), infoDefinition.UsedForTaxCalculation);
				updated = true;
			}

			return updated;
		}

		protected virtual bool AttributeValueIsRequired(TEntity attribute)
		{
			if (DynamicSettingDefinitions.TryGetValue(attribute.SettingName, out PRSettingDefinition<TDynamicType, TTypeMeta> settingDefinition))
			{
				return AttributeValueIsRequired(attribute, settingDefinition.ControlType);
			}
			return false;
		}

		protected abstract bool AttributeValueIsRequired(TEntity attribute, SettingControlType controlType);

		protected abstract string InvalidAttributeErrorMessage { get; }

		protected virtual Dictionary<IPRReflectiveSettingMapper<TTypeMeta>, DynamicEntity<TDynamicType, TTypeMeta>.MetaWithSetting> GetMetaSettings(IEnumerable<IPRReflectiveSettingMapper<TTypeMeta>> typeSettings)
		{
			using (var payrollAssemblyScope = new PXPayrollAssemblyScope<PayrollMetaProxy>())
			{
				return payrollAssemblyScope.Proxy.GetMetaWithSettings<TDynamicType, TTypeMeta>(typeSettings.ToArray());
			}
		}

		protected ISettingDefinition GetSettingDefinition(string settingName)
		{
			ISettingDefinition settingDefinition = null;
			if (DynamicSettingDefinitions.ContainsKey(settingName))
			{
				settingDefinition = DynamicSettingDefinitions[settingName];
			}
			else if (DatabaseSettingDefinitions.ContainsKey(settingName))
			{
				settingDefinition = DatabaseSettingDefinitions[settingName];
			}

			return settingDefinition;
		}

		protected ISettingFieldDefinition GetSettingFieldDefinition(string settingName)
		{
			return GetSettingDefinition(settingName) as ISettingFieldDefinition;
		}
	}

	public abstract class PRAttributeValuesSelectBase<TEntity, TSelect, TDefinitionEntity, TDefinitionSelect, TDynamicType, TTypeMeta> : PRAttributeSelectBase<TEntity, TSelect, TDynamicType, TTypeMeta>
		where TEntity : class, IBqlTable, IPRSetting, new()
		where TSelect : class, IBqlSelect, new()
		where TDefinitionEntity : class, IBqlTable, IPRSetting, new()
		where TDefinitionSelect : class, IBqlSelect, new()
		where TDynamicType : DynamicEntity<TDynamicType, TTypeMeta>
		where TTypeMeta : SettingDefinitionListAttribute
	{
		public PRAttributeValuesSelectBase(PXGraph graph) : base(graph)
		{
			_Graph.FieldUpdated.AddHandler(typeof(TEntity), "UseDefault", UseDefaultFieldUpdated);
			_Graph.RowSelected.AddHandler(typeof(TEntity), RowSelected);
		}

		protected override void ValueFieldSelecting(PXCache sender, PXFieldSelectingEventArgs e)
		{
			base.ValueFieldSelecting(sender, e);
			var fieldState = e.ReturnState as PXFieldState;
			if (fieldState != null)
			{
				bool? useDefault = (bool?)sender.GetValue(e.Row, "UseDefault");
				if (useDefault == true)
				{
					var settingInfo = (TDefinitionEntity)PXSelectorAttribute.Select(sender, e.Row, "SettingName");
					if (settingInfo == null) throw new PXException(Messages.TaxSettingNotFound, sender.GetValue(e.Row, "SettingName"));
					e.ReturnValue = settingInfo.Value;
					if (settingInfo.Value != null)
					{
						fieldState.Error = null;
						fieldState.ErrorLevel = PXErrorLevel.Undefined;
						SetErrorLevel(sender, e.Row as TEntity, PXErrorLevel.Undefined);
					}
				}
			}
		}

		protected virtual void UseDefaultFieldUpdated(PXCache sender, PXFieldUpdatedEventArgs e)
		{
			bool? useDefault = (bool?)sender.GetValue(e.Row, nameof(IPRSetting.UseDefault));

			if (useDefault == true)
			{
				//We'll fetch and show default value from ValueFieldSelecting handler
				sender.SetValue(e.Row, nameof(IPRSetting.Value), null);
			}
			else
			{
				var definition = (TDefinitionEntity)PXSelectorAttribute.Select(sender, e.Row, nameof(IPRSetting.SettingName));
				sender.SetValue(e.Row, nameof(IPRSetting.Value), definition?.Value);
			}
		}

		protected virtual void RowSelected(PXCache sender, PXRowSelectedEventArgs e)
		{
			var row = e.Row as TEntity;
			if (row == null || string.IsNullOrEmpty(row.SettingName))
			{
				return;
			}

			var parentInfo = PXSelectorAttribute.Select(sender, row, "SettingName") as TDefinitionEntity;
			if (parentInfo == null)
			{
				throw new PXException(Messages.TaxSettingNotFound, row.SettingName);
			}
			PXUIFieldAttribute.SetEnabled(sender, e.Row, nameof(row.UseDefault), parentInfo.Value != null || row.UseDefault == true);
		}

		protected override IEnumerable<ISettingDefinition> GetAttributeDefinitionList()
		{
			PXView attributeDefinitionView = new PXView(_Graph, true, BqlCommand.CreateInstance(typeof(TDefinitionSelect)));
			foreach (IPRSetting attribute in attributeDefinitionView.SelectMulti().Where(x => ((IPRSetting)x).AllowOverride == true))
			{
				yield return attribute;
			}
		}

		protected override bool AttributeValueIsRequired(TEntity attribute, SettingControlType controlType)
		{
			return attribute.Required == true && controlType != SettingControlType.CheckBox;
		}

		protected override List<TEntity> GetSavedValues()
		{
			return new PXView(_Graph, true, BqlCommand.CreateInstance(typeof(TSelect))).SelectMulti().Select(x =>
			{
				if (x is TEntity)
				{
					return x as TEntity;
				}
				else if (x is PXResult<TEntity>)
				{
					return ((PXResult<TEntity>)x)[0] as TEntity;
				}
				return null;
			}).Where(x => x != null).ToList();
		}

		protected override string InvalidAttributeErrorMessage => Messages.ValueBlankAndRequired;
	}

	public abstract class PRAttributeDefinitionSelectBase<TEntity, TSelect, TDynamicType, TTypeMeta> : PRAttributeSelectBase<TEntity, TSelect, TDynamicType, TTypeMeta>
		where TEntity : class, IBqlTable, IPRSetting, new()
		where TSelect : class, IBqlSelect, new()
		where TDynamicType : DynamicEntity<TDynamicType, TTypeMeta>
		where TTypeMeta : SettingDefinitionListAttribute
	{

		public PRAttributeDefinitionSelectBase(PXGraph graph) : base(graph) { }

		protected override IEnumerable<ISettingDefinition> GetAttributeDefinitionList()
		{
			List<ISettingDefinition> definitionList = new List<ISettingDefinition>();
			
			if (PXAccess.FeatureInstalled<FeaturesSet.payrollUS>())
			{
				definitionList.AddRange(DynamicSettingDefinitions.Values);
			}

			if (PXAccess.FeatureInstalled<FeaturesSet.payrollCAN>())
			{
				definitionList.AddRange(DatabaseSettingDefinitions.Values);
			}

			return definitionList;
		}

		protected override bool AttributeValueIsRequired(TEntity attribute, SettingControlType controlType)
		{
			return attribute.Required == true && controlType != SettingControlType.CheckBox;
		}

		protected override string InvalidAttributeErrorMessage => Messages.ValueBlankAndRequired;
	}

	public class PRAttributeValuesSelect<TEntity, TSelect, TDefinitionEntity, TDefinitionSelect, TTypeSettingEntity, TTypeSettingSelect, TDynamicType, TTypeMeta> : PRAttributeValuesSelectBase<TEntity, TSelect, TDefinitionEntity, TDefinitionSelect, TDynamicType, TTypeMeta>
		where TEntity : class, IBqlTable, IPRSetting, new()
		where TSelect : class, IBqlSelect, new()
		where TDefinitionEntity : class, IBqlTable, IPRSetting, new()
		where TDefinitionSelect : class, IBqlSelect, new()
		where TTypeSettingEntity : class, IBqlTable, ITaxCode, IPRReflectiveSettingMapper<TTypeMeta>, new()
		where TTypeSettingSelect : class, IBqlSelect, new()
		where TDynamicType : DynamicEntity<TDynamicType, TTypeMeta>
		where TTypeMeta : SettingDefinitionListAttribute
	{
		public PRAttributeValuesSelect(PXGraph graph) : base(graph) { }

		protected override IEnumerable<IPRReflectiveSettingMapper<TTypeMeta>> GetMetaTypes()
		{
			PXView settingView = new PXView(_Graph, true, BqlCommand.CreateInstance(typeof(TTypeSettingSelect)));
			return settingView.SelectMulti().Select(x => x as IPRReflectiveSettingMapper<TTypeMeta>);
		}

		protected override IEnumerable<IPRReflectiveSettingMapper<TTypeMeta>> GetAllMetaTypes()
		{
			return SelectFrom<TTypeSettingEntity>.View.Select(_Graph).FirstTableItems.Select(x => x as IPRReflectiveSettingMapper<TTypeMeta>);
		}

		protected override IEnumerable<TaxSetting> FilterTaxSettingsInView(IEnumerable<TaxSetting> unfiltered)
		{
			PXView typeSettingView = new PXView(_Graph, true, BqlCommand.CreateInstance(typeof(TTypeSettingSelect)));
			ITaxCode typeSetting = typeSettingView.SelectSingle() as ITaxCode;
			return unfiltered.Where(setting => !setting.IsEmployeeSetting
				&& !string.IsNullOrWhiteSpace(setting.ApplicableTaxUniqueID)
				&& typeSetting?.TaxUniqueCode == setting.ApplicableTaxUniqueID);
		}
	}

	public class PRAttributeDefinitionSelect<TEntity, TSelect, TTypeSetting, TDynamicType, TTypeMeta> : PRAttributeDefinitionSelectBase<TEntity, TSelect, TDynamicType, TTypeMeta>
		where TEntity : class, IBqlTable, IPRSetting, new()
		where TSelect : class, IBqlSelect, new()
		where TTypeSetting : class, IBqlTable, IPRReflectiveSettingMapper<TTypeMeta>, ITaxCode, new()
		where TDynamicType : DynamicEntity<TDynamicType, TTypeMeta>
		where TTypeMeta : SettingDefinitionListAttribute
	{

		public PRAttributeDefinitionSelect(PXGraph graph) : base(graph) { }

		protected override IEnumerable<IPRReflectiveSettingMapper<TTypeMeta>> GetMetaTypes()
		{
			object current = _Graph.Caches[typeof(TTypeSetting)].Current;
			if (current != null)
			{
				yield return current as IPRReflectiveSettingMapper<TTypeMeta>;
			}
			else
			{
				yield break;
			}
		}

		protected override IEnumerable<IPRReflectiveSettingMapper<TTypeMeta>> GetAllMetaTypes()
		{
			return SelectFrom<TTypeSetting>.View.Select(_Graph).FirstTableItems.Select(x => x as IPRReflectiveSettingMapper<TTypeMeta>);
		}

		protected override IEnumerable<TaxSetting> FilterTaxSettingsInView(IEnumerable<TaxSetting> unfiltered)
		{
			ITaxCode current = _Graph.Caches[typeof(TTypeSetting)].Current as ITaxCode;
			return unfiltered.Where(setting => !setting.IsEmployeeSetting
				&& !string.IsNullOrWhiteSpace(setting.ApplicableTaxUniqueID)
				&& current?.TaxUniqueCode == setting.ApplicableTaxUniqueID);
		}
	}

	public class PREmployeeAttributeDefinitionSelect<TEntity, TSelect, TFilterField, TCountryField, TSearchEntity, TSearchFilter, TSearchField> : PRAttributeDefinitionSelectBase<TEntity, TSelect, PX.Payroll.Data.PREmployee, EmployeeLocationSettingsAttribute>
		where TEntity : class, IBqlTable, IPRSetting, new()
		where TSelect : class, IBqlSelect, new()
		where TFilterField : class, IBqlField
		where TCountryField : class, IBqlField
		where TSearchEntity : class, IBqlTable, new()
		where TSearchFilter : class, IBqlSelect, new()
		where TSearchField : class, IBqlField
	{
		private Type _FilterEntityType = BqlCommand.GetItemType(typeof(TFilterField));

		public PREmployeeAttributeDefinitionSelect(PXGraph graph) : base(graph) { }

		protected override IEnumerable<IPRReflectiveSettingMapper<EmployeeLocationSettingsAttribute>> GetMetaTypes()
		{
			return GetDisplayStates().Select(x => new PREmployeeSettingMapper(x));
		}

		protected override IEnumerable<IPRReflectiveSettingMapper<EmployeeLocationSettingsAttribute>> GetAllMetaTypes()
		{
			return PRSubnationalEntity.GetAll(GetPayrollCountryCode()).Select(x => new PREmployeeSettingMapper(x));
		}

		protected override IEnumerable<TaxSetting> FilterTaxSettingsInView(IEnumerable<TaxSetting> unfiltered)
		{
			HashSet<string> displayStates = GetDisplayStates().Select(x => x.Abbr).ToHashSet();
			return unfiltered.Where(setting => setting.IsEmployeeSetting
				&& displayStates.Contains(setting.State));
		}

		protected override bool AttributeValueIsRequired(TEntity attribute, SettingControlType controlType)
		{
			return attribute.Required == true && attribute.AllowOverride == false && controlType != SettingControlType.CheckBox;
		}

		protected override string InvalidAttributeErrorMessage => Messages.ValueBlankAndRequiredAndNotOverridable;

		private HashSet<PRSubnationalEntity> GetDisplayStates()
		{
			HashSet<PRSubnationalEntity> displayStates;
			if (_Graph.Caches[_FilterEntityType].GetValue<TFilterField>(_Graph.Caches[_FilterEntityType].Current).Equals(true))
			{
				displayStates = new HashSet<PRSubnationalEntity>();
				PXView searchFilterView = new PXView(_Graph, false, BqlCommand.CreateInstance(typeof(TSearchFilter)));
				foreach (TSearchEntity row in searchFilterView.SelectMulti())
				{
					var value = _Graph.Caches[typeof(TSearchEntity)].GetValue<TSearchField>(row);
					if (value != null)
					{
						var stateAbbr = value as string;
						PRSubnationalEntity state = null;

						if (stateAbbr != null)
						{
							state = PRSubnationalEntity.FromAbbr(GetPayrollCountryCode(), stateAbbr);
						}

						if (state == null)
						{
							throw new PXException(Messages.CantUseFieldAsState, _Graph.Caches[typeof(TSearchEntity)].GetField(typeof(TSearchField)));
						}

						displayStates.Add(state);
					}
				}
				displayStates.Add(PRSubnationalEntity.GetFederal(GetPayrollCountryCode()));
			}
			else
			{
				displayStates = new HashSet<PRSubnationalEntity>(PRSubnationalEntity.GetAll(GetPayrollCountryCode()));
			}

			return displayStates;
		}

		private string GetPayrollCountryCode()
		{
			Type countryIDDac = BqlCommand.GetItemType(typeof(TCountryField));
			return (_Graph.Caches[countryIDDac].GetValue<TCountryField>(_Graph.Caches[countryIDDac].Current) as string) ?? LocationConstants.USCountryCode;
		}
	}

	/// <typeparam name="TRequiredInSearch"> BQL Search to retrieve values to fill the Required parameter in <typeparamref name="TDefinitionSelect"/> BQL. </typeparam>
	public class PREmployeeAttributeValueSelect<TEntity, TSelect, TDefinitionEntity, TDefinitionSelect, TRequiredInSearch, TSearchField, TPrimaryViewType> : PRAttributeValuesSelectBase<TEntity, TSelect, TDefinitionEntity, TDefinitionSelect, PX.Payroll.Data.PREmployee, EmployeeLocationSettingsAttribute>
		where TEntity : class, IBqlTable, IPRSetting, new()
		where TSelect : class, IBqlSelect, new()
		where TDefinitionEntity : class, IBqlTable, IPRSetting, new()
		where TDefinitionSelect : class, IBqlSelect, new()
		where TRequiredInSearch : IBqlSelect
		where TSearchField : IBqlField
		where TPrimaryViewType : class, IBqlTable
	{
		public PREmployeeAttributeValueSelect(PXGraph graph) : base(graph) { }

		public void SetSettingNameForDescription(TEntity row)
		{
			if (!string.IsNullOrEmpty(row.Description) && row is IStateSpecific stateSpecificRow && !string.IsNullOrEmpty(stateSpecificRow.State))
			{
				ISettingDefinition definition = 
					(ISettingDefinition)DynamicSettingDefinitions.Values.FirstOrDefault(x => x.State == stateSpecificRow.State && x.Description == row.Description)
					?? DatabaseSettingDefinitions.Values.FirstOrDefault(x => x.State == stateSpecificRow.State && x.Description == row.Description);
				row.SettingName = definition.SettingName;
				row.TypeName = definition.TypeName;
				row.AatrixMapping = definition.AatrixMapping;
				row.AllowOverride = definition.AllowOverride;
				row.UseDefault = definition.AllowOverride != true;
			}
		}

		protected override IEnumerable<IPRReflectiveSettingMapper<EmployeeLocationSettingsAttribute>> GetMetaTypes()
		{
			List<TDefinitionEntity> fullDefinitionList = SelectFrom<TDefinitionEntity>.View.Select(_Graph).FirstTableItems.ToList();
			if (_Graph.IsImport && fullDefinitionList.FirstOrDefault() is IStateSpecific)
			{
				foreach (string state in PRSubnationalEntity.GetAll(GetPayrollCountryCode()).Select(x => x.Abbr))
				{
					TDefinitionEntity definition = fullDefinitionList.FirstOrDefault(x => ((IStateSpecific)x).State == state);
					if (definition != null)
					{
						yield return new PREmployeeSettingMapper(definition);
					}
				}

				yield break;
			}

			object[] parameters = GetBqlParameters();
			PXView settingsView = GetAttributDefinitionView(parameters);
			var statesAdded = new HashSet<string>();
			foreach (object row in settingsView.SelectMulti(parameters))
			{
				if (row is IStateSpecific)
				{
					var state = ((IStateSpecific)row).State;
					if (statesAdded.Add(state))
					{
						yield return new PREmployeeSettingMapper((TDefinitionEntity)row);
					}
				}
			}

			if (fullDefinitionList.FirstOrDefault() is IStateSpecific)
			{
				PXCache employeeTaxCache = _Graph.Caches[typeof(PREmployeeTax)];
				foreach (PREmployeeTax newEmployeeTax in employeeTaxCache.Inserted)
				{
					var state = (PXSelectorAttribute.Select<PREmployeeTax.taxID>(employeeTaxCache, newEmployeeTax) as PRTaxCode)?.TaxState;
					if (!string.IsNullOrEmpty(state) && statesAdded.Add(state))
					{
						TDefinitionEntity definition = fullDefinitionList.FirstOrDefault(x => ((IStateSpecific)x).State == state);
						if (definition != null)
						{
							yield return new PREmployeeSettingMapper(definition);
						}
					}
				}
			}
		}

		protected override IEnumerable<IPRReflectiveSettingMapper<EmployeeLocationSettingsAttribute>> GetAllMetaTypes()
		{
			return SelectFrom<TDefinitionEntity>.View.Select(_Graph).FirstTableItems
				.Where(x => x is IStateSpecific)
				.Select(x => new PREmployeeSettingMapper(x));
		}

		protected override IEnumerable<TaxSetting> FilterTaxSettingsInView(IEnumerable<TaxSetting> unfiltered)
		{
			IEnumerable<TaxSetting> filtered = unfiltered.Where(x => x.IsEmployeeSetting);
			if (_Graph.IsImport)
			{
				HashSet<string> allStatesInCountry = PRSubnationalEntity.GetAll(GetPayrollCountryCode()).Select(x => x.Abbr).ToHashSet();
				return filtered.Where(x => allStatesInCountry.Contains(x.State));
			}

			object[] parameters = GetBqlParameters();
			PXView settingsView = GetAttributDefinitionView(parameters);
			HashSet<string> statesInView = settingsView.SelectMulti(parameters).Where(x => !string.IsNullOrWhiteSpace((x as IStateSpecific)?.State))
				.Select(x => (x as IStateSpecific).State).ToHashSet();
			return filtered.Where(setting => statesInView.Contains(setting.State));
		}

		protected override IEnumerable<ISettingDefinition> GetAttributeDefinitionList()
		{			
			var list = new List<IPRSetting>();
			PXCache cache = _Graph.Caches[typeof(TPrimaryViewType)];
			if (cache.GetStatus(cache.Current) == PXEntryStatus.Inserted)
			{
				return list;
			}

			object[] parameters = GetBqlParameters();
			PXView attributeDefinitionView = GetAttributDefinitionView(parameters);
			foreach (IPRSetting attribute in attributeDefinitionView.SelectMulti(parameters).Where(x => ((IPRSetting)x).AllowOverride == true))
			{
				list.Add(attribute);
			}

			return list;
		}

		private object[] GetBqlParameters()
		{
			var searchView = new PXView(_Graph, false, BqlCommand.CreateInstance(typeof(TRequiredInSearch)));
			var fieldType = typeof(TSearchField).DeclaringType;
			var fieldName = typeof(TSearchField).Name;
			return searchView.SelectMulti().GroupBy(x => searchView.Cache.GetValue(PXResult.Unwrap(x, fieldType), fieldName)).Select(x => x.Key).Where(x => x != null).ToArray();
		}

		private PXView GetAttributDefinitionView(IEnumerable<object> parameters)
		{
			PXView attributeDefinitionView = new PXView(_Graph, false, BqlCommand.CreateInstance(typeof(TDefinitionSelect)));
			for (int i = 0; i < parameters.Count(); i++)
			{
				attributeDefinitionView.WhereOr(BqlCommand.Compose(typeof(Where<,>), typeof(PRCompanyTaxAttribute.state), typeof(Equal<>), typeof(Required<>), typeof(PRCompanyTaxAttribute.state)));
			}
			return attributeDefinitionView;
		}

		private string GetPayrollCountryCode()
		{
			return PRCountryAttribute.GetPayrollCountry();
		}
	}
}
