using PX.Common;
using PX.Common.Extensions;
using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.CR.Extensions.CRDuplicateEntities
{
	[PXInternalUseOnly]
	public class CRGramProcessorBase
	{
		private readonly PXGraph.GetDefaultDelegate _defaultDelegate;
		private long processedItems;
		private DateTime? track;

		protected readonly PXGraph graph;
		protected CRSetup Setup => _defaultDelegate() as CRSetup;

		public CRGramProcessorBase()
			: this(PXGraph.CreateInstance<PXGraph>())
		{
		}
		public CRGramProcessorBase(PXGraph graph)
		{
			this.graph = graph;
			processedItems = 0;
			track = null;
			if(!graph.Defaults.TryGetValue(typeof(CRSetup), out _defaultDelegate))
			{
				// adds default delegate to graph
				new IN.PXSetupOptional<CRSetup>(graph);
				_defaultDelegate = graph.Defaults[typeof(CRSetup)];
			}
		}

		public void CreateGrams(Contact contact)
		{
			PersistGrams(contact, true);
		}

		public bool PersistGrams(Contact contact, bool requireRecreate = false)
		{
			try
			{
				if (track == null)
					track = DateTime.Now;

				if (graph.Caches[contact.GetType()].GetStatus(contact) == PXEntryStatus.Deleted)
				{
					PXDatabase.Delete<CRGrams>(new PXDataFieldRestrict("EntityID", PXDbType.Int, 4, contact.ContactID, PXComp.EQ));

					return false;
				}

				if (!requireRecreate && GramSourceUpdated(contact))
					return false;

				PXDatabase.Delete<CRGrams>(new PXDataFieldRestrict("EntityID", PXDbType.Int, 4, contact.ContactID, PXComp.EQ));

				foreach (CRGrams gram in DoCreateGrams(contact))
				{
					PXDatabase.Insert<CRGrams>(
						new PXDataFieldAssign(nameof(CRGrams.entityID), PXDbType.Int, 4, contact.ContactID),
						new PXDataFieldAssign(nameof(CRGrams.fieldName), PXDbType.NVarChar, 60, gram.FieldName),
						new PXDataFieldAssign(nameof(CRGrams.fieldValue), PXDbType.NVarChar, 60, gram.FieldValue),
						new PXDataFieldAssign(nameof(CRGrams.score), PXDbType.Decimal, 8, gram.Score),
						new PXDataFieldAssign(nameof(CRGrams.validationType), PXDbType.NVarChar, 2, gram.ValidationType)
						);
				}

				contact.GrammValidationDateTime = PXTimeZoneInfo.Now;

				PXDatabase.Update<Contact>
					(
						new PXDataFieldAssign(nameof(Contact.grammValidationDateTime), PXTimeZoneInfo.ConvertTimeToUtc(contact.GrammValidationDateTime.Value, LocaleInfo.GetTimeZone())),
						new PXDataFieldRestrict(nameof(Contact.contactID), contact.ContactID)
					);

				processedItems += 1;

				return true;
			}
			finally
			{
				if (processedItems % 100 == 0)
				{
					TimeSpan taken = DateTime.Now - (DateTime)track;
					System.Diagnostics.Debug.WriteLine("Items count:{0}, increment taken {1}", processedItems, taken);
					track = DateTime.Now;
				}
			}
		}

		public bool GramSourceUpdated(Contact contact)
		{
			PXCache cache = graph.Caches[contact.GetType()];

			if (cache.GetStatus(contact) == PXEntryStatus.Inserted || cache.GetStatus(contact) == PXEntryStatus.Notchanged)
				return false;

			if (Definition.ValidationRules(contact.ContactType)
				.Any(rule => !String.Equals(
					 cache.GetValue(contact, rule.MatchingField)?.ToString(),
					 cache.GetValueOriginal(contact, rule.MatchingField)?.ToString(),
					 StringComparison.InvariantCultureIgnoreCase)))
			{
				return false;
			}

			return true;
		}

		public bool IsRulesDefined
		{
			get { return Definition.IsRulesDefined; }
		}

		public bool IsAnyBlockingRulesConfigured(string contactType)
		{
			return Definition.IsAnyBlockingRulesConfigured(contactType);
		}

		public virtual bool IsValidationOnEntryActive(string contactType)
		{
			return Definition.IsValidationOnEntryActive(contactType);
		}

		protected class ValidationDefinition : IPrefetchable
		{
			public List<CRValidationRules> Rules;
			public Dictionary<string, List<CRValidationRules>> TypeRules;

			private List<CRValidationRules> Leads;
			private List<CRValidationRules> Contacts;
			private List<CRValidationRules> Accounts;

			private List<CRValidation> Validations;

			public void Prefetch()
			{
				var graph = PXGraph.CreateInstance<PXGraph>();

				Rules = new List<CRValidationRules>();
				Leads = new List<CRValidationRules>();
				Contacts = new List<CRValidationRules>();
				Accounts = new List<CRValidationRules>();
				TypeRules = new Dictionary<string, List<CRValidationRules>>();

				// Acuminator disable once PX1072 PXGraphCreationForBqlQueries [prefetch]
				foreach (CRValidationRules rule in PXSelect<CRValidationRules>.Select(graph))
				{
					Rules.Add(rule);

					if (!TypeRules.ContainsKey(rule.ValidationType))
					{
						TypeRules[rule.ValidationType] = new List<CRValidationRules>();
					}

					TypeRules[rule.ValidationType].Add(rule);

					switch (rule.ValidationType)
					{
						case ValidationTypesAttribute.LeadToLead:
							Leads.Add(rule);
							break;

						case ValidationTypesAttribute.LeadToContact:
						case ValidationTypesAttribute.ContactToLead:
							Leads.Add(rule);
							Contacts.Add(rule);
							break;

						case ValidationTypesAttribute.ContactToContact:
							Contacts.Add(rule);
							break;

						case ValidationTypesAttribute.LeadToAccount:
							Leads.Add(rule);
							Accounts.Add(rule);
							break;
						case ValidationTypesAttribute.ContactToAccount:
							Contacts.Add(rule);
							Accounts.Add(rule);
							break;

						case ValidationTypesAttribute.AccountToAccount:
							Accounts.Add(rule);
							break;
					}
				}

				Validations = new List<CRValidation>();

				// Acuminator disable once PX1072 PXGraphCreationForBqlQueries [prefetch]
				foreach (CRValidation validation in PXSelect<CRValidation>.Select(graph))
				{
					Validations.Add(validation);
				}
			}

			public List<CRValidationRules> ValidationRules(string contactType)
			{
				switch (contactType)
				{
					case ContactTypesAttribute.Lead:
						return Leads;

					case ContactTypesAttribute.Person:
						return Contacts;

					case ContactTypesAttribute.BAccountProperty:
						return Accounts;

					default:
						return new List<CRValidationRules>();
				}
			}

			public string[] ValidationTypes(string contactType)
			{
				switch (contactType)
				{
					case ContactTypesAttribute.Lead:
						return new[] { ValidationTypesAttribute.LeadToLead, ValidationTypesAttribute.LeadToContact, ValidationTypesAttribute.LeadToAccount, ValidationTypesAttribute.ContactToLead };

					case ContactTypesAttribute.Person:
						return new[] { ValidationTypesAttribute.ContactToLead, ValidationTypesAttribute.ContactToContact, ValidationTypesAttribute.ContactToAccount, ValidationTypesAttribute.LeadToContact };

					case ContactTypesAttribute.BAccountProperty:
						return new[] { ValidationTypesAttribute.LeadToAccount, ValidationTypesAttribute.ContactToAccount, ValidationTypesAttribute.AccountToAccount };

					default:
						return new string[0];
				}
			}

			public bool IsRulesDefined
			{
				get { return Contacts.Count > 0 && Accounts.Count > 0; }
			}

			public bool IsAnyBlockingRulesConfigured(string contactType)
			{
				switch (contactType)
				{
					case ContactTypesAttribute.Lead:
						return
							TypeRules[ValidationTypesAttribute.LeadToLead].Count > 0 && TypeRules[ValidationTypesAttribute.LeadToLead].Any(_ => _.CreateOnEntry != CreateOnEntryAttribute.Allow)
							|| TypeRules[ValidationTypesAttribute.LeadToContact].Count > 0 && TypeRules[ValidationTypesAttribute.LeadToContact].Any(_ => _.CreateOnEntry != CreateOnEntryAttribute.Allow)
							|| TypeRules[ValidationTypesAttribute.LeadToAccount].Count > 0 && TypeRules[ValidationTypesAttribute.LeadToAccount].Any(_ => _.CreateOnEntry != CreateOnEntryAttribute.Allow);

					case ContactTypesAttribute.Person:
						return
							TypeRules[ValidationTypesAttribute.ContactToLead].Count > 0 && TypeRules[ValidationTypesAttribute.ContactToLead].Any(_ => _.CreateOnEntry != CreateOnEntryAttribute.Allow)
							|| TypeRules[ValidationTypesAttribute.ContactToContact].Count > 0 && TypeRules[ValidationTypesAttribute.ContactToContact].Any(_ => _.CreateOnEntry != CreateOnEntryAttribute.Allow)
							|| TypeRules[ValidationTypesAttribute.ContactToAccount].Count > 0 && TypeRules[ValidationTypesAttribute.ContactToAccount].Any(_ => _.CreateOnEntry != CreateOnEntryAttribute.Allow);

					case ContactTypesAttribute.BAccountProperty:
						return
							TypeRules[ValidationTypesAttribute.AccountToAccount].Count > 0 && TypeRules[ValidationTypesAttribute.AccountToAccount].Any(_ => _.CreateOnEntry != CreateOnEntryAttribute.Allow);

					default:
						return false;
				}
			}

			public virtual bool IsValidationOnEntryActive(string contactType)
			{
				switch (contactType)
				{
					case ContactTypesAttribute.Lead:
						return
							Validations
								.Where(_ => _.Type.IsIn(ValidationTypesAttribute.LeadToLead, ValidationTypesAttribute.LeadToContact, ValidationTypesAttribute.LeadToAccount))
								.Any(_ => _.ValidateOnEntry == true);

					case ContactTypesAttribute.Person:
						return
							Validations
								.Where(_ => _.Type.IsIn(ValidationTypesAttribute.ContactToLead, ValidationTypesAttribute.ContactToContact, ValidationTypesAttribute.ContactToAccount))
								.Any(_ => _.ValidateOnEntry == true);

					case ContactTypesAttribute.BAccountProperty:
						return
							Validations
								.Where(_ => _.Type == ValidationTypesAttribute.AccountToAccount)
								.Any(_ => _.ValidateOnEntry == true);

					default:
						return false;
				}
			}
		}

		protected ValidationDefinition Definition
		{
			get
			{
				return PXDatabase.GetSlot<ValidationDefinition>("ValidationRules", typeof(CRValidation), typeof(CRValidationRules));
			}
		}

		protected virtual (decimal total, decimal totalZero) GetTotals(
			PXCache documentCache, object document,
			PXCache locationCache, object location,
			string validationType)
		{
			decimal total = 0, totalZero = 0;
			foreach (var rule in Definition.TypeRules[validationType])
			{
				if (validationType == ValidationTypesAttribute.AccountToAccount && location != null)
				{
					if (locationCache.GetValue(location, rule.MatchingField) == null
						&& documentCache.GetValue(document, rule.MatchingField) == null)
						totalZero += rule.ScoreWeight.GetValueOrDefault();
					else
						total += rule.ScoreWeight.GetValueOrDefault();
				}
				else
				{
					if (documentCache.GetValue(document, rule.MatchingField) == null)
						totalZero += rule.ScoreWeight.GetValueOrDefault();
					else
						total += rule.ScoreWeight.GetValueOrDefault();
				}
			}

			if (Setup?.DuplicateScoresNormalization is false)
			{
				return (total, 0m);
			}

			return (total, totalZero);
		}

		protected virtual IEnumerable<CRGrams> DoCreateGrams(object document)
		{
			var types = GetValidationTypes(document);

			foreach (string validationType in types)
			{
				PXCache contactCache = graph.Caches[document.GetType()];
				PXCache locationCache = graph.Caches[typeof(Location)];

				Location defLocation = GetDefLocation(document);

				if (!Definition.TypeRules.ContainsKey(validationType)) continue;

				foreach (var gram in CreateGramsForType(contactCache, document, locationCache, defLocation, validationType))
				{
					yield return gram;
				}
			}
		}

		protected virtual IEnumerable<CRGrams> CreateGramsForType(PXCache documentCache, object document, PXCache locationCache, Location defLocation, string validationType)
		{
			var (total, totalZero) = GetTotals(documentCache, document, locationCache, defLocation, validationType);

			if (total == 0) 
				yield break;

			foreach (CRValidationRules rule in Definition.TypeRules[validationType])
			{
				string fieldName = rule.MatchingField;
				string transformRule = rule.TransformationRule;
				Decimal sw = rule.ScoreWeight ?? 0;
				if (sw == 0) continue;

				if (sw > 0 && totalZero > 0)
					sw += totalZero * (sw / total);

				var value = documentCache.GetValue(document, fieldName);
				if (validationType == ValidationTypesAttribute.AccountToAccount && value == null)
				{
					value = locationCache.GetValue(defLocation, fieldName);
				}

				if (value == null) continue;

				string stringValue = value.ToString().ToLower();

				if (transformRule.Equals(TransformationRulesAttribute.SplitWords))
				{
					foreach (var gram in GetSplitWordGrams(document, rule, stringValue, sw, fieldName))
					{
						yield return gram;
					}
				}
				else
				{
					if (transformRule.Equals(TransformationRulesAttribute.DomainName))
					{
						foreach (var gram in GetDomainNameGrams(document, rule, stringValue, sw, fieldName))
						{
							yield return gram;
						}
					}

					else
					{
						yield return GetGrams(document, rule, fieldName, stringValue, Decimal.Round(sw, 4));
					}
				}
			}
		}

		protected virtual string[] GetValidationTypes(object document)
		{
			if (document is Contact contact)
				return Definition.ValidationTypes(contact.ContactType);
			throw new NotSupportedException();
		}

		protected virtual Location GetDefLocation(object document)
		{
			if (document is Contact contact)
				return GetDefLocation(contact.BAccountID);
			return null;
		}

		protected virtual Location GetDefLocation(int? baccountId)
		{
			if (baccountId == null)
				return null;

			BAccount bAccount = PXSelect<BAccount, Where<BAccount.bAccountID, Equal<Required<Contact.bAccountID>>>>.Select(graph, baccountId);
			if (bAccount != null && bAccount.DefLocationID != null)
				return PXSelect<Location, Where<Location.bAccountID, Equal<Required<Location.bAccountID>>, And<Location.locationID, Equal<Required<BAccount.defLocationID>>>>>
						.Select(graph, bAccount.BAccountID, bAccount.DefLocationID);

			return null;
		}

		protected virtual CRGrams GetGrams(object document, CRValidationRules rule, string fieldName, string fieldValue, decimal? score)
		{
			if(document is Contact contact)
			{
				return new CRGrams
				{
					EntityID = contact.ContactID,
					ValidationType = rule.ValidationType,
					FieldName = fieldName,
					FieldValue = fieldValue,
					Score = score,
				};
			}

			throw new NotSupportedException();
		}

		protected virtual IEnumerable<CRGrams> GetSplitWordGrams(object document, CRValidationRules rule, string stringValue, decimal scodeWeight, string fieldName)
		{
			var charsDelimiters = Setup?.DuplicateCharsDelimiters?.ToCharArray();

			string[] words = stringValue.Split(charsDelimiters, StringSplitOptions.RemoveEmptyEntries);

			foreach (string word in words)
			{
				Decimal score = Decimal.Round(scodeWeight / words.Length, 4);

				if (score <= 0)
					continue;

				yield return GetGrams(document, rule, fieldName, word, score);
			}
		}

		protected virtual IEnumerable<CRGrams> GetDomainNameGrams(object document, CRValidationRules rule, string stringValue, decimal scodeWeight, string fieldName)
		{
			if (stringValue.Contains('@'))
			{
				stringValue = stringValue.Segment('@', 1);
			}
			else
			{
				try
				{
					stringValue = new UriBuilder(stringValue).Host;
					int index = stringValue.IndexOf('.');
					string firstSegment = index < 0 ? stringValue : stringValue.Substring(0, index);
					if (firstSegment.Equals("www"))
					{
						stringValue = stringValue.Substring(index + 1);
					}
				}
				catch (UriFormatException)
				{
					//Do nothing
				}
			}

			yield return GetGrams(document, rule, fieldName, stringValue, Decimal.Round(scodeWeight, 4));
		}
	}
}
