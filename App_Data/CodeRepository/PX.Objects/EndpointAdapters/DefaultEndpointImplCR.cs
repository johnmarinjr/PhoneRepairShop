using PX.Api;
using PX.Api.ContractBased;
using PX.Api.ContractBased.Adapters;
using PX.Api.ContractBased.Models;
using PX.Data;
using PX.Objects.CR;
using System;
using System.Linq;

namespace PX.Objects.EndpointAdapters
{
	[PXVersion("17.200.001", "Default")]
	[PXVersion("18.200.001", "Default")]
	internal class DefaultEndpointImplCR : DefaultEndpointImplCRBase
	{
		public DefaultEndpointImplCR(
			CbApiWorkflowApplicator.CaseApplicator caseApplicator,
			CbApiWorkflowApplicator.OpportunityApplicator opportunityApplicator,
			CbApiWorkflowApplicator.LeadApplicator leadApplicator)
			: base(
				caseApplicator,
				opportunityApplicator,
				leadApplicator)
		{
		}

		[FieldsProcessed(new[] { "Status" })]
		protected virtual void Opportunity_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			if (!(graph is OpportunityMaint))
				return;

			var current = EnsureAndGetCurrentForInsert<CROpportunity>(graph);
			OpportunityApplicator.ApplyStatusChange(graph, MetadataProvider, entity, current);
		}

		[FieldsProcessed(new[] { "Status" })]
		protected virtual void Opportunity_Update(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			if (!(graph is OpportunityMaint))
				return;

			var current = EnsureAndGetCurrentForUpdate<CROpportunity>(graph);
			OpportunityApplicator.ApplyStatusChange(graph, MetadataProvider, entity, current);
		}

		[FieldsProcessed(new[] { "Status" })]
		protected virtual void Case_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			if (!(graph is CRCaseMaint))
				return;

			var current = EnsureAndGetCurrentForInsert<CRCase>(graph);
			CaseApplicator.ApplyStatusChange(graph, MetadataProvider, entity, current);
		}

		[FieldsProcessed(new[] { "Status" })]
		protected virtual void Case_Update(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			if (!(graph is CRCaseMaint))
				return;

			var current = EnsureAndGetCurrentForUpdate<CRCase>(graph);
			CaseApplicator.ApplyStatusChange(graph, MetadataProvider, entity, current);
		}

		[FieldsProcessed(new[] { "Status" })]
		protected virtual void Lead_Insert(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			if (!(graph is LeadMaint))
				return;

			var current = EnsureAndGetCurrentForInsert<CRLead>(graph);
			LeadApplicator.ApplyStatusChange(graph, MetadataProvider, entity, current);
		}

		[FieldsProcessed(new[] { "Status" })]
		protected virtual void Lead_Update(PXGraph graph, EntityImpl entity, EntityImpl targetEntity)
		{
			if (!(graph is LeadMaint))
				return;

			var current = EnsureAndGetCurrentForUpdate<CRLead>(graph);
			LeadApplicator.ApplyStatusChange(graph, MetadataProvider, entity, current);
		}
	}
}
