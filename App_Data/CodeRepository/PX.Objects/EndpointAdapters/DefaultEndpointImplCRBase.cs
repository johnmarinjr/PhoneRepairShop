using PX.Api.ContractBased;
using PX.Api.ContractBased.Adapters;
using PX.Api.ContractBased.Models;
using PX.Data;
using PX.Data.BQL.Fluent;
using PX.Objects.CR;
using PX.Objects.EP;
using PX.Objects.EP.Graphs.EPEventMaint.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.EndpointAdapters
{
	internal class DefaultEndpointImplCRBase : IAdapterWithMetadata
	{



		public IEntityMetadataProvider MetadataProvider { protected get; set; }

		protected CbApiWorkflowApplicator.CaseApplicator CaseApplicator { get; }
		protected CbApiWorkflowApplicator.OpportunityApplicator OpportunityApplicator { get; }
		protected CbApiWorkflowApplicator.LeadApplicator LeadApplicator { get; }

		public DefaultEndpointImplCRBase(
			CbApiWorkflowApplicator.CaseApplicator caseApplicator,
			CbApiWorkflowApplicator.OpportunityApplicator opportunityApplicator,
			CbApiWorkflowApplicator.LeadApplicator leadApplicator)
		{
			CaseApplicator = caseApplicator;
			OpportunityApplicator = opportunityApplicator;
			LeadApplicator = leadApplicator;
		}

		protected T EnsureAndGetCurrentForInsert<T>(PXGraph graph, Func<T, T> initializer = null) where T : class, IBqlTable, new()
		{
			var cache = graph.Caches[typeof(T)];

			T entity = initializer == null
				? cache.Insert() as T
				: cache.Insert(initializer.Invoke(cache.CreateInstance() as T)) as T;

			if (entity == null)
			{
				// this means there is trash in graph from previous session
				graph.Clear();
				entity = initializer == null
					? cache.Insert() as T
					: cache.Insert(initializer.Invoke(cache.CreateInstance() as T)) as T;
			}

			return entity;
		}

		protected T EnsureAndGetCurrentForUpdate<T>(PXGraph graph) where T : class, IBqlTable, new()
		{
			var cache = graph.Caches[typeof(T)];
			if (cache.Current as T == null)
			{
				PXTrace.WriteWarning("No entity in cache for update. Create new entity instead.");

				// just for sure, if there is trash too
				graph.Clear();

				return cache.Insert() as T;
			}
			else
			{
				cache.Current = cache.Update(cache.Current) as T;
			}

			return cache.Current as T;
		}

		protected T GetOrCreateInstance<T>(PXGraph graph) where T : class, IBqlTable, new()
		{
			var cache = graph.Caches<T>();
			if (cache.Current as T == null)
			{
				graph.Clear();
				return graph.Caches<T>().CreateInstance() as T;
			}

			return cache.Current as T;
		}

		protected T Insert<T>(PXGraph graph, T entity) where T : class, IBqlTable, new()
		{
			return graph.Caches<T>().Insert(entity) as T;
		}

		protected T Update<T>(PXGraph graph, T entity) where T : class, IBqlTable, new()
		{
			return graph.Caches<T>().Update(entity) as T;
		}

		protected T GetField<T>(EntityImpl impl, string fieldName) where T : EntityField
		{
			return impl.Fields.OfType<T>().FirstOrDefault(f => string.Equals(f.Name, fieldName, StringComparison.InvariantCultureIgnoreCase));
		}

		protected EntityValueField GetField(EntityImpl impl, string fieldName) => GetField<EntityValueField>(impl, fieldName);

		[FieldsProcessed(new[] { "Type", "Key" })]
		protected virtual void EventAttendee_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			var current = EnsureAndGetCurrentForInsert<EPAttendee>(graph);
			EventAttendeeImpl(graph, targetEntity, current);
		}

		[FieldsProcessed(new[] { "Type", "Key" })]
		protected virtual void EventAttendee_Update(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			var current = EnsureAndGetCurrentForUpdate<EPAttendee>(graph);
			EventAttendeeImpl(graph, targetEntity, current);
		}

		private void EventAttendeeImpl(PXGraph graph, EntityImpl targetEntity, EPAttendee current)
		{
			var typeField = GetField(targetEntity, "Type");
			if (typeField != null && int.TryParse(typeField.Value, out int type)
				&& type == EPEventMaint_AttendeeExt_BackwardCompatibility.ManualAttendeeType)
				return;

			var keyField = GetField(targetEntity, "Key");
			if (keyField?.Value == null
				|| !int.TryParse(keyField.Value, out int key))
				return;

			var contact = Contact.PK.Find(graph, key);
			if(contact != null)
			{
				current.ContactID = contact.ContactID;
				graph.Caches<EPAttendee>().Update(current);
				return;
			}
		}
	}
}
