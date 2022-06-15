using System;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Objects.AM.Attributes;
using PX.Objects.Common;

namespace PX.Objects.AM
{
    public class ConfigurationCopyEngine
    {
        protected readonly PXGraph _graph;

        private ConfigurationCopyEngine(PXGraph graph)
        {
            _graph = graph ?? throw new PXArgumentException(nameof(graph));
        }

        public static void UpdateConfigurationFromKey(PXGraph graph, AMConfigurationResults currentConfigResult, string configKeyFrom)
        {
            if (graph == null || currentConfigResult == null || string.IsNullOrWhiteSpace(configKeyFrom))
            {
                return;
            }

            var fromConfiguration = GetConfigResultByKey(graph, configKeyFrom, currentConfigResult.ConfigurationID, currentConfigResult.ConfigResultsID);
            if (fromConfiguration == null)
            {
                return;
            }

            UpdateConfigurationFromConfiguration(graph, currentConfigResult, fromConfiguration);
        }

        public static void UpdateConfigurationFromConfiguration(PXGraph graph, AMConfigurationResults currentConfigResult, AMConfigurationResults fromConfigResult)
        {
            UpdateConfigurationFromConfiguration(graph, currentConfigResult, fromConfigResult, true, true);
        }

        // ASSUMES CURRENT CONFIGURATION RESULTS HAS ALL RECORDS ALREADY IN PLACE...
        // Allows for copy of configuration across revisions
        public static void UpdateConfigurationFromConfiguration(PXGraph graph, AMConfigurationResults currentConfigResult, AMConfigurationResults fromConfigResult, bool updateAttributes, bool updateOptions)
        {
            if (currentConfigResult == null || fromConfigResult == null)
            {
                return;
            }

            // We only need to sync the records the users would change such as attributes and options. All others are calculated or not updated by the user.
            var cce = new ConfigurationCopyEngine(graph);

            using(new RemoveMultiFormulaAggregrateScope<PXUnboundFormulaAttribute>(graph.Caches[typeof(AMConfigResultsOption)], typeof(AMConfigResultsOption.curyExtPrice)))
            {

                cce.UpdateConfigResult(currentConfigResult, fromConfigResult);

                // Set attributes first so copy can pickup qty enabled options where qty also has a formula from attributes
                if (updateAttributes)
                {
                    cce.UpdateAttriburtes(currentConfigResult, fromConfigResult);
                }

                if (updateOptions)
                {
                    cce.UpdateOptions(currentConfigResult, fromConfigResult);
                }
            }

            cce.RecalculateOptionPrice(currentConfigResult);
        }

        private void RecalculateOptionPrice(AMConfigurationResults currentConfigResult)
        {
            currentConfigResult.CuryOptionPriceTotal = 0m;
            currentConfigResult.CurySupplementalPriceTotal = 0m;
            PXUnboundFormulaAttribute.CalcAggregate<AMConfigResultsOption.curyExtPrice>(_graph.Caches[typeof(AMConfigResultsOption)], currentConfigResult);
            currentConfigResult = (AMConfigurationResults)_graph.Caches[typeof(AMConfigurationResults)].Update(currentConfigResult);
        }

        protected virtual void UpdateConfigResult(AMConfigurationResults toConfigResult, AMConfigurationResults fromConfigResult)
        {
            if (toConfigResult == null)
            {
                throw new PXArgumentException(nameof(toConfigResult));
            }
            if (fromConfigResult == null)
            {
                throw new PXArgumentException(nameof(fromConfigResult));
            }

            toConfigResult.KeyID = fromConfigResult.KeyID;
            toConfigResult.KeyDescription = fromConfigResult.KeyDescription;
            toConfigResult.TranDescription = fromConfigResult.TranDescription;
            toConfigResult = (AMConfigurationResults)_graph.Caches[typeof(AMConfigurationResults)].Update(toConfigResult);
        }

		protected virtual PXResultset<AMConfigResultsAttribute> GetConfigResultsAttribute(AMConfigurationResults configResults)
		{
			var configResultID = configResults?.ConfigResultsID;
			if(configResultID < 0)
			{
				var rtn = new PXResultset<AMConfigResultsAttribute>();
				// this will help avoid sub query to each record as its cached values anyhow
				foreach (var attributeResult in _graph.Caches[typeof(AMConfigResultsAttribute)].Cached.RowCast<AMConfigResultsAttribute>()
					.Where(r => r.ConfigResultsID == configResults.ConfigResultsID
					&& !_graph.Caches[typeof(AMConfigResultsAttribute)].GetStatus(r).IsIn(PXEntryStatus.Deleted, PXEntryStatus.InsertedDeleted)))
				{
					var locatedAttriubte = (AMConfigurationAttribute)_graph.Caches[typeof(AMConfigurationAttribute)].Locate(new AMConfigurationAttribute
						{
							ConfigurationID = attributeResult.ConfigurationID,
							Revision = attributeResult.Revision,
							LineNbr = attributeResult.AttributeLineNbr
						});

					if(locatedAttriubte == null)
					{
						locatedAttriubte = AMConfigurationAttribute.PK.Find(_graph, attributeResult.ConfigurationID, attributeResult.Revision, attributeResult.AttributeLineNbr);
						if(locatedAttriubte == null)
						{
							continue;
						}
					}

					rtn.Add(new PXResult<AMConfigResultsAttribute, AMConfigurationAttribute>(attributeResult, locatedAttriubte));
				}

				if(rtn.Count > 0)
				{
					return rtn;
				}
			}

			return PXSelectJoin<AMConfigResultsAttribute,
				InnerJoin<AMConfigurationAttribute,
					On<AMConfigResultsAttribute.configurationID, Equal<AMConfigurationAttribute.configurationID>,
						And<AMConfigResultsAttribute.revision, Equal<AMConfigurationAttribute.revision>,
							And<AMConfigResultsAttribute.attributeLineNbr, Equal<AMConfigurationAttribute.lineNbr>>>>>,
				Where<AMConfigResultsAttribute.configResultsID, Equal<Required<AMConfigurationResults.configResultsID>>>>.Select(_graph, configResultID);
		}

        protected virtual void UpdateAttriburtes(AMConfigurationResults toConfigResult, AMConfigurationResults fromConfigResult)
        {
            var fromAttributes = GetAttributes(fromConfigResult);
            foreach (PXResult<AMConfigResultsAttribute, AMConfigurationAttribute> result in GetConfigResultsAttribute(toConfigResult))
            {
                var attribute = (AMConfigurationAttribute) result;
                var attributeResult = (AMConfigResultsAttribute) result;
                if (attribute == null || string.IsNullOrWhiteSpace(attribute.Label) ||
                    attributeResult == null || attributeResult.ConfigResultsID == null)
                {
                    continue;
                }

				PXResult<AMConfigResultsAttribute, AMConfigurationAttribute> fromAttributeResult;
                if (!fromAttributes.TryGetValue(attribute.Label, out fromAttributeResult))
                {
                    attributeResult.Value = attribute.Value;
                    _graph.Caches[typeof(AMConfigResultsAttribute)].Update(attributeResult);
                    continue;
                }
                var attributeResultFrom = (AMConfigResultsAttribute)fromAttributeResult;
                if (attributeResultFrom == null || attributeResultFrom.ConfigResultsID == null || attributeResult.Value == attributeResultFrom.Value)
                {
                    continue;
                }

				attributeResult.Value = attributeResultFrom.Value;

                _graph.Caches[typeof(AMConfigResultsAttribute)].Update(attributeResult);
            }
        }

		
		protected virtual PXResultset<AMConfigResultsOption> GetConfigResultsOption(AMConfigurationResults configResults)
		{
			var configResultID = configResults?.ConfigResultsID;
			if(configResultID < 0)
			{
				var rtn = new PXResultset<AMConfigResultsOption>();
				// this will help avoid sub query to each record as its cached values anyhow
				foreach (var optionResult in _graph.Caches[typeof(AMConfigResultsOption)].Cached.RowCast<AMConfigResultsOption>()
					.Where(r => r.ConfigResultsID == configResults.ConfigResultsID
					&& !_graph.Caches[typeof(AMConfigResultsOption)].GetStatus(r).IsIn(PXEntryStatus.Deleted, PXEntryStatus.InsertedDeleted)))
				{
					var locatedFeature = (AMConfigurationFeature)_graph.Caches[typeof(AMConfigurationFeature)].Locate(new AMConfigurationFeature
						{
							ConfigurationID = optionResult.ConfigurationID,
							Revision = optionResult.Revision,
							LineNbr = optionResult.FeatureLineNbr
						});

					if(locatedFeature == null)
					{
						locatedFeature = AMConfigurationFeature.PK.Find(_graph, optionResult.ConfigurationID, optionResult.Revision, optionResult.FeatureLineNbr);
						if (locatedFeature == null)
						{
							continue;
						}
					}

					var locatedOption = (AMConfigurationOption)_graph.Caches[typeof(AMConfigurationOption)].Locate(new AMConfigurationOption
						{
							ConfigurationID = optionResult.ConfigurationID,
							Revision = optionResult.Revision,
							ConfigFeatureLineNbr = optionResult.FeatureLineNbr,
							LineNbr = optionResult.OptionLineNbr
						});

					if(locatedOption == null)
					{
						locatedOption = AMConfigurationOption.PK.Find(_graph, optionResult.ConfigurationID, optionResult.Revision, optionResult.FeatureLineNbr, optionResult.OptionLineNbr);
						if (locatedOption == null)
						{
							continue;
						}
					}

					rtn.Add(new PXResult<AMConfigResultsOption, AMConfigurationOption, AMConfigurationFeature>(optionResult, locatedOption, locatedFeature));
				}

				if(rtn.Count > 0)
				{
					return rtn;
				}
			}

			return PXSelectJoin<AMConfigResultsOption,
				InnerJoin<AMConfigurationOption,
					On<AMConfigResultsOption.configurationID, Equal<AMConfigurationOption.configurationID>,
						And<AMConfigResultsOption.revision, Equal<AMConfigurationOption.revision>,
						And<AMConfigResultsOption.featureLineNbr, Equal<AMConfigurationOption.configFeatureLineNbr>,
						And<AMConfigResultsOption.optionLineNbr, Equal<AMConfigurationOption.lineNbr>>>>>,
				InnerJoin<AMConfigurationFeature,
					On<AMConfigResultsOption.configurationID, Equal<AMConfigurationFeature.configurationID>,
						And<AMConfigResultsOption.revision, Equal<AMConfigurationFeature.revision>,
						And<AMConfigResultsOption.featureLineNbr, Equal<AMConfigurationFeature.lineNbr>>>>>>,
				Where<AMConfigResultsOption.configResultsID, Equal<Required<AMConfigResultsOption.configResultsID>>>>.Select(_graph, configResultID);
		}

		[Obsolete]
        protected virtual void UpdateOptions(AMConfigurationResults toConfigResult, AMConfigurationResults fromConfigResult, bool includeQtyEnabledOnly)
        {
            UpdateOptions(toConfigResult, fromConfigResult);
        }

        protected virtual void UpdateOptions(AMConfigurationResults toConfigResult, AMConfigurationResults fromConfigResult)
        {
            var fromOptions = GetOptions(fromConfigResult);
			var useCurrentParent = toConfigResult?.ConfigResultsID < 0; //for performance - avoid query to AMConfigResultsFeature
			using (new UseCurrentParentScope(_graph.Caches[typeof(AMConfigResultsOption)], useCurrentParent, typeof(AMConfigResultsOption.featureLineNbr)))
			{
				foreach (PXResult<AMConfigResultsOption, AMConfigurationOption, AMConfigurationFeature> result in GetConfigResultsOption(toConfigResult))
				{
					var optionResult = _graph.Caches[typeof(AMConfigResultsOption)].LocateElseCopy((AMConfigResultsOption)result);
					var configFeature = (AMConfigurationFeature)result;
					var configOption = (AMConfigurationOption)result;
					if (optionResult?.ConfigResultsID == null || string.IsNullOrWhiteSpace(configOption?.Label) ||
						!configOption.ResultsCopy.GetValueOrDefault() || string.IsNullOrWhiteSpace(configFeature?.Label))
					{
						continue;
					}

					if (useCurrentParent)
					{
						_graph.SetLocatedCacheCurrent(new AMConfigResultsFeature
						{
							ConfigResultsID = optionResult.ConfigResultsID,
							FeatureLineNbr = optionResult.FeatureLineNbr
						});
					}

					PXResult<AMConfigResultsOption, AMConfigurationOption, AMConfigurationFeature> fromOptionResult = null;
					if (!fromOptions.TryGetValue(MergeLabelsForKey(configFeature, configOption), out fromOptionResult))
					{
						optionResult.Included = false;
						optionResult.ManualInclude = false;
						optionResult.Available = true;
						optionResult.Required = false;
						var optionResultNotFound = (AMConfigResultsOption)_graph.Caches[typeof(AMConfigResultsOption)].Update(optionResult);

						if (optionResultNotFound != null)
						{
							// Potential need to exclude set of qty is due to attribute formulas calculating the qty when not qty enabled
							optionResultNotFound.Qty = optionResultNotFound.QtyRequired;
							_graph.Caches[typeof(AMConfigResultsOption)].Update(optionResultNotFound);
						}
						continue;
					}
					var optionResultFrom = (AMConfigResultsOption)fromOptionResult;
					if (optionResultFrom?.ConfigResultsID == null)
					{
						continue;
					}

					var noChanges = _graph.Caches[typeof(AMConfigResultsOption)]
							.ObjectsEqual<AMConfigResultsOption.included, AMConfigResultsOption.manualInclude, AMConfigResultsOption.available, AMConfigResultsOption.required>(optionResultFrom, optionResult);
					var qtyUpdated = configOption.QtyEnabled.GetValueOrDefault() && optionResultFrom.Included.GetValueOrDefault() && optionResult.Qty != optionResultFrom.Qty;

					if(noChanges && !qtyUpdated)
					{
						continue;
					}

					optionResult.Included = optionResultFrom.Included;
					optionResult.ManualInclude = optionResultFrom.ManualInclude;
					optionResult.Available = optionResultFrom.Available;
					optionResult.Required = optionResultFrom.Required;
					if (qtyUpdated)
					{
						optionResult.Qty = optionResultFrom.Qty; 
					}
					_graph.Caches[typeof(AMConfigResultsOption)].Update(optionResult);
				}
			}
        }

        protected virtual Dictionary<string, PXResult<AMConfigResultsAttribute, AMConfigurationAttribute>> GetAttributes(AMConfigurationResults configResult)
        {
            var dic = new Dictionary<string, PXResult<AMConfigResultsAttribute, AMConfigurationAttribute>>();
            foreach (PXResult<AMConfigResultsAttribute, AMConfigurationAttribute> result in PXSelectJoin<AMConfigResultsAttribute,
                InnerJoin<AMConfigurationAttribute,
                    On<AMConfigResultsAttribute.configurationID, Equal<AMConfigurationAttribute.configurationID>,
                        And<AMConfigResultsAttribute.revision, Equal<AMConfigurationAttribute.revision>,
                            And<AMConfigResultsAttribute.attributeLineNbr, Equal<AMConfigurationAttribute.lineNbr>>>>>,
                Where<AMConfigResultsAttribute.configResultsID, Equal<Required<AMConfigurationResults.configResultsID>>>>.Select(_graph, configResult.ConfigResultsID))
            {
                var attribute = (AMConfigurationAttribute) result;
                if (attribute == null || string.IsNullOrWhiteSpace(attribute.Label))
                {
                    continue;
                }

                dic.Add(attribute.Label, result);
            }
            return dic;
        }

        protected virtual Dictionary<string, PXResult<AMConfigResultsOption, AMConfigurationOption, AMConfigurationFeature>> GetOptions(AMConfigurationResults configResult)
        {
            var dic = new Dictionary<string, PXResult<AMConfigResultsOption, AMConfigurationOption, AMConfigurationFeature>>();
            foreach (PXResult<AMConfigResultsOption, AMConfigurationOption, AMConfigurationFeature> result in PXSelectJoin<AMConfigResultsOption,
                InnerJoin<AMConfigurationOption,
                    On<AMConfigResultsOption.configurationID, Equal<AMConfigurationOption.configurationID>,
                        And<AMConfigResultsOption.revision, Equal<AMConfigurationOption.revision>,
                            And<AMConfigResultsOption.featureLineNbr, Equal<AMConfigurationOption.configFeatureLineNbr>,
                                And<AMConfigResultsOption.optionLineNbr, Equal<AMConfigurationOption.lineNbr>>>>>,
                InnerJoin<AMConfigurationFeature,
                        On<AMConfigResultsOption.configurationID, Equal<AMConfigurationFeature.configurationID>,
                            And<AMConfigResultsOption.revision, Equal<AMConfigurationFeature.revision>,
                                And<AMConfigResultsOption.featureLineNbr, Equal<AMConfigurationFeature.lineNbr>>>>>>,
                Where<AMConfigResultsOption.configResultsID, Equal<Required<AMConfigResultsOption.configResultsID>>>>.Select(_graph, configResult?.ConfigResultsID))
            {
                var configFeature = (AMConfigurationFeature)result;
                var configOption = (AMConfigurationOption)result;
                if (configOption == null || string.IsNullOrWhiteSpace(configOption.Label) ||
                    configFeature == null || string.IsNullOrWhiteSpace(configFeature.Label))
                {
                    continue;
                }

                dic.Add(MergeLabelsForKey(configFeature, configOption), result);
            }
            return dic;
        }

        protected static string MergeLabelsForKey(AMConfigurationFeature feature, AMConfigurationOption option)
        {
            if (option == null || feature == null)
            {
                return string.Empty;
            }
            return $"{feature.Label.TrimIfNotNullEmpty()}{feature.LineNbr}{option.Label.TrimIfNotNullEmpty()}";
        }

        /// <summary>
        /// Get the latest configuration result by a config key
        /// </summary>
        /// <param name="graph">Calling graph</param>
        /// <param name="configKey">Configuration KEYID to lookup</param>
        /// <param name="configurationID">ConfigurationID the ConfigKey is related to</param>
        /// <param name="excludingConfigResultsID">Excluding a specific config results ID (optional)</param>
        public static AMConfigurationResults GetConfigResultByKey(PXGraph graph, string configKey, string configurationID, int? excludingConfigResultsID)
        {
            if (string.IsNullOrWhiteSpace(configKey))
            {
                return null;
            }

            if (string.IsNullOrWhiteSpace(configurationID))
            {
                throw new PXArgumentException(nameof(configurationID));
            }

            // Configurations not yet saved will be a negative number - no need to look those up
            if (excludingConfigResultsID.GetValueOrDefault() <= 0)
            {
                return PXSelect<AMConfigurationResults,
                    Where<AMConfigurationResults.keyID, Equal<Required<AMConfigurationResults.keyID>>,
                        And<AMConfigurationResults.configurationID, Equal<Required<AMConfigurationResults.configurationID>>,
                        And<AMConfigurationResults.completed, Equal<True>>>>,
                    OrderBy<Desc<AMConfigurationResults.createdDateTime>>>.SelectWindowed(graph, 0, 1, configKey, configurationID);
            }

            return PXSelect<AMConfigurationResults,
                Where<AMConfigurationResults.keyID, Equal<Required<AMConfigurationResults.keyID>>,
                    And<AMConfigurationResults.configurationID, Equal<Required<AMConfigurationResults.configurationID>>,
                    And<AMConfigurationResults.completed, Equal<True>,
                    And<AMConfigurationResults.configResultsID, NotEqual<Required<AMConfigurationResults.configResultsID>>>>>>,
                OrderBy<Desc<AMConfigurationResults.createdDateTime>>>.SelectWindowed(graph, 0, 1, configKey, configurationID, excludingConfigResultsID);
        }

        /// <summary>
        /// Similar to OverrideAttributePropertyScope however by a single field and allows for multiple of TAttribute while the property can have different values
        /// </summary>
        protected class RemoveMultiFormulaAggregrateScope <TAttribute> : IDisposable
		    where TAttribute : PXFormulaAttribute
        {
		    public PXCache Cache { get; }

		    public Type Field { get; }

            public IEnumerable<Type> Aggregates { get; }

		    protected Dictionary<Type, Type> _oldAggregateValues;

            public RemoveMultiFormulaAggregrateScope(PXCache cache, Type field)
            {
                Cache = cache ?? throw new ArgumentNullException(nameof(cache));
                Field = field ?? throw new ArgumentNullException(nameof(field));

                if (!typeof(IBqlField).IsAssignableFrom(field))
				{
					throw new PXException($"The type {field.FullName} is not a BQL field.");
				}

				IEnumerable<TAttribute> attributesOfType = this.Cache
					.GetAttributesReadonly(field.Name)
					.OfType<TAttribute>();

				if (!attributesOfType.Any())
				{
                    return;
				}

                _oldAggregateValues = new Dictionary<Type, Type>();

				attributesOfType.ForEach(attribute => 
				{
                    _oldAggregateValues[attribute.Formula] = attribute.Aggregate;
                    attribute.Aggregate = null;
				});
            }

            public void Dispose()
            {
				IEnumerable<TAttribute> attributesOfType = Cache
					.GetAttributes(Field.Name)
					.OfType<TAttribute>();

				foreach (TAttribute attribute in attributesOfType)
				{
                    var oldValue = _oldAggregateValues[attribute.Formula];
                    attribute.Aggregate = oldValue;
				}
            }
        }
    }
}
