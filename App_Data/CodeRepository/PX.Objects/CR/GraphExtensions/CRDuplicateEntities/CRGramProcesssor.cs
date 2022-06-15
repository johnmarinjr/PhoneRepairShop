using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PX.Common;
using PX.Data;
using PX.Data.Update.ExchangeService;

namespace PX.Objects.CR.Extensions.CRDuplicateEntities
{
	[PXInternalUseOnly]
	public class CRGramProcessor : CRGramProcessorBase
	{
		public CRGramProcessor(PXGraph graph)
			: base(graph) { }

		public IEnumerable<(bool IsBlocked, string BlockType)> CheckIsBlocked(DuplicateDocument document, IEnumerable duplicates)
		{
			var currentGrams = DoCreateGrams(document).Where(gram => gram.CreateOnEntry != CreateOnEntryAttribute.Allow).ToList();
			if (currentGrams.Count == 0)
				return null;

			var duplicateRecords = duplicates.Cast<CRDuplicateResult>().ToList();

			if (duplicateRecords.Count == 0)
				return null;

			return duplicateRecords
					.Select(duplicate =>
					{
						var duplicateRecord = duplicate.GetItem<CRDuplicateRecord>();
						var duplicateContact = duplicate.GetItem<DuplicateContact>();

						var isBlocked = false;
						var blockType = CreateOnEntryAttribute.Warn;

						foreach (var blockingGram in currentGrams.Where(_ => _.ValidationType == duplicateRecord?.ValidationType))
						{
							var value = graph.Caches[typeof(DuplicateContact)].GetValue(duplicateContact, blockingGram.FieldName)?.ToString();

							if (String.IsNullOrWhiteSpace(value))
								continue;

							if (value.Equals(blockingGram.FieldValue, StringComparison.InvariantCultureIgnoreCase))
							{
								isBlocked = true;

								blockType = blockType == CreateOnEntryAttribute.Warn
									? blockingGram.CreateOnEntry    // try to increase from Warn to Deny
									: blockType;                    // leave Deny
							}
						}

						return (IsBlocked: isBlocked, BlockType: blockType);
					});
		}

		public bool PersistGrams(DuplicateDocument document, bool requireRecreate = false)
		{
			if (graph.Caches[document.GetType()].GetStatus(document) == PXEntryStatus.Deleted)
			{
				PXDatabase.Delete<CRGrams>(new PXDataFieldRestrict<CRGrams.entityID>(PXDbType.Int, 4, document.ContactID, PXComp.EQ));

				return false;
			}

			if (!requireRecreate && GrammSourceUpdated(document))
				return false;

			PXDatabase.Delete<CRGrams>(new PXDataFieldRestrict<CRGrams.entityID>(PXDbType.Int, 4, document.ContactID, PXComp.EQ));

			foreach (CRGrams gram in DoCreateGrams(document))
			{
				PXDatabase.Insert<CRGrams>(
					new PXDataFieldAssign(nameof(CRGrams.entityID), PXDbType.Int, 4, document.ContactID),
					new PXDataFieldAssign(nameof(CRGrams.fieldName), PXDbType.NVarChar, 60, gram.FieldName),
					new PXDataFieldAssign(nameof(CRGrams.fieldValue), PXDbType.NVarChar, 60, gram.FieldValue),
					new PXDataFieldAssign(nameof(CRGrams.score), PXDbType.Decimal, 8, gram.Score),
					new PXDataFieldAssign(nameof(CRGrams.validationType), PXDbType.NVarChar, 2, gram.ValidationType)
				);
			}

			document.DuplicateStatus = DuplicateStatusAttribute.NotValidated;
			document.GrammValidationDateTime = PXTimeZoneInfo.Now;

			PXDatabase.Update<Contact>(
				new PXDataFieldAssign<Contact.duplicateStatus>(PXDbType.NVarChar, document.DuplicateStatus),
				new PXDataFieldAssign<Contact.grammValidationDateTime>(PXDbType.DateTime, PXTimeZoneInfo.ConvertTimeToUtc(document.GrammValidationDateTime.Value, LocaleInfo.GetTimeZone())),
				new PXDataFieldRestrict<Contact.contactID>(PXDbType.Int, document.ContactID)
			);

			return true;
		}

		public bool GrammSourceUpdated(DuplicateDocument document)
		{
			var main = graph.Caches[typeof(DuplicateDocument)].GetMain(document);

			PXCache cache = graph.Caches[main.GetType()];

			if (cache.GetStatus(main) == PXEntryStatus.Inserted || cache.GetStatus(main) == PXEntryStatus.Notchanged)
				return false;

			if (Definition.ValidationRules(document.ContactType)
				.Any(rule => !String.Equals(
					cache.GetValue(main, rule.MatchingField)?.ToString(),
					cache.GetValueOriginal(main, rule.MatchingField)?.ToString(),
					StringComparison.InvariantCultureIgnoreCase)))
			{
				return false;
			}

			return true;
		}

		protected override CRGrams GetGrams(object document, CRValidationRules rule, string fieldName, string fieldValue, decimal? score)
		{
			if (document is DuplicateDocument dup)
			{
				return new CRGrams
				{
					EntityID = dup.ContactID,
					ValidationType = rule.ValidationType,
					FieldName = fieldName,
					FieldValue = fieldValue,
					Score = score,
					CreateOnEntry = rule.CreateOnEntry,
				};
			}

			throw new NotSupportedException();
		}

		protected override string[] GetValidationTypes(object document)
		{
			if (document is DuplicateDocument dup)
				return Definition.ValidationTypes(dup.ContactType);
			throw new NotSupportedException();
		}

		protected override Location GetDefLocation(object document)
		{
			if (document is DuplicateDocument dup)
				return GetDefLocation(dup.BAccountID);
			return null;
		}
	}
}
