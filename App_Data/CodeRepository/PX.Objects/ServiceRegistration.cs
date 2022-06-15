using System;
using System.Linq;
using System.Collections.Generic;
using System.Web.Compilation;

using Autofac;
using PX.Data;
using PX.Data.Process;
using PX.Data.EP;
using PX.Data.RelativeDates;
using PX.Objects.GL.FinPeriods;
using PX.Objects.SM;
using PX.Objects.CM.Extensions;
using PX.Objects.EndpointAdapters;
using PX.Objects.EndpointAdapters.WorkflowAdapters.AR;
using PX.Objects.EndpointAdapters.WorkflowAdapters.AP;
using PX.Objects.EndpointAdapters.WorkflowAdapters.IN;
using PX.Objects.EndpointAdapters.WorkflowAdapters.PO;
using PX.Objects.EndpointAdapters.WorkflowAdapters.SO;
using PX.Objects.FA;
using PX.Objects.PM;
using PX.Objects.CS;
using PX.Objects.AP;
using PX.Objects.AU;
using PX.Objects.EP;
using PX.Data.Search;
using PX.Objects.AP.InvoiceRecognition;
using PX.Objects.IN.Services;
using PX.Objects.CA;
using Assembly = System.Reflection.Assembly;
using PX.Objects.CA.Repositories;

namespace PX.Objects
{
	public class ServiceRegistration : Module
	{
		protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<FinancialPeriodManager>()
                .As<IFinancialPeriodManager>();

            // Acuminator disable once PX1003 NonSpecificPXGraphCreateInstance [Justification]
            builder
                .RegisterType<FinPeriodScheduleAdjustmentRule>()
                .WithParameter(TypedParameter.From<Func<PXGraph>>(() => new PXGraph())) // TODO: Use single PXGraph instance to cache queries
                .As<IScheduleAdjustmentRule>()
                .SingleInstance();

			builder
				.RegisterType<TodayBusinessDate>()
				.As<ITodayUtc>();

			RegisterNotificationServices(builder);

			builder
				.RegisterType<FinPeriodRepository>()
				.As<IFinPeriodRepository>();

			builder
				.RegisterType<FinPeriodUtils>()
				.As<IFinPeriodUtils>();

			builder
				.Register<Func<PXGraph, IPXCurrencyService>>(context
					=>
					{
						return (graph)
						=>
						{
							return new DatabaseCurrencyService(graph);
						};
					});

			builder
				.RegisterType<FABookPeriodRepository>()
				.As<IFABookPeriodRepository>();

			builder
				.RegisterType<FABookPeriodUtils>()
				.As<IFABookPeriodUtils>();

			builder
				.RegisterType<BudgetService>()
				.As<IBudgetService>();

			builder
				.RegisterType<UnitRateService>()
				.As<IUnitRateService>();

			builder
				.RegisterType<PM.ProjectSettingsManager>()
				.As<PM.IProjectSettingsManager>();

			builder
				.RegisterType<PM.CostCodeManager>()
				.As<PM.ICostCodeManager>();
			builder
				.RegisterType<PM.ProjectMultiCurrency>()
				.As<PM.IProjectMultiCurrency>();

			builder.RegisterType<DefaultEndpointImplCR>().AsSelf();
			builder.RegisterType<DefaultEndpointImplCR20>().AsSelf();
			builder.RegisterType<DefaultEndpointImplPM>().AsSelf();
			builder.RegisterType<CbApiWorkflowApplicator.CaseApplicator>().SingleInstance().AsSelf();
			builder.RegisterType<CbApiWorkflowApplicator.OpportunityApplicator>().SingleInstance().AsSelf();
			builder.RegisterType<CbApiWorkflowApplicator.LeadApplicator>().SingleInstance().AsSelf();
			builder.RegisterType<CbApiWorkflowApplicator.ProjectTemplateApplicator>().SingleInstance().AsSelf();
			builder.RegisterType<CbApiWorkflowApplicator.ProjectTaskApplicator>().SingleInstance().AsSelf();

			builder.RegisterType<BillAdapter>().AsSelf();
			builder.RegisterType<CheckAdapter>().AsSelf();

			builder.RegisterType<InvoiceAdapter>().AsSelf();
			builder.RegisterType<PaymentAdapter>().AsSelf();

			builder.RegisterType<InventoryReceiptAdapter>().AsSelf();
			builder.RegisterType<AdjustmentAdapter>().AsSelf();
			builder.RegisterType<InventoryAdjustmentAdapter>().AsSelf();
			builder.RegisterType<TransferOrderAdapter>().AsSelf();
			builder.RegisterType<KitAssemblyAdapter>().AsSelf();

			builder.RegisterType<PurchaseOrderAdapter>().AsSelf();
			builder.RegisterType<PurchaseReceiptAdapter>().AsSelf();

			builder.RegisterType<SalesOrderAdapter>().AsSelf();
			builder.RegisterType<ShipmentAdapter>().AsSelf();
			builder.RegisterType<SalesInvoiceAdapter>().AsSelf();

			builder
				.RegisterType<CN.Common.Services.NumberingSequenceUsage>()
				.As<CN.Common.Services.INumberingSequenceUsage>();

			builder
				.RegisterType<AdvancedAuthenticationRestrictor>()
				.As<IAdvancedAuthenticationRestrictor>()
				.SingleInstance();

			builder
				.RegisterType<PXEntitySearchEnriched>()
				.As<IEntitySearchService>();

			builder
				.RegisterType<InventoryAccountService>()
				.As<IInventoryAccountService>();


			builder
				.RegisterType<PXEntitySearchEnriched>()
				.As<IEntitySearchService>();

			builder
				.RegisterType<CustomTimeRegionProvider>()
				.As<PX.Common.ITimeRegionProvider>()
				.SingleInstance();
			builder
				.RegisterType<DirectDepositTypeService>()
				.AsSelf();

			builder
				.RegisterType<CABankTransactionsRepository>()
				.As<ICABankTransactionsRepository>();

			builder
				.RegisterType<CABankTransactionsRepository>()
				.As<ICABankTransactionsRepository>();

			RegisterEPModuleServices(builder);
			RegisterMailServices(builder);
		}

		private void RegisterNotificationServices(ContainerBuilder builder)
		{
			builder.RegisterType<EP.NotificationProvider>()
				.As<INotificationSender>()
				.SingleInstance()
				.PreserveExistingDefaults();

			builder.RegisterType<EP.NotificationProvider>()
				.As<INotificationSenderWithActivityLink>()
				.SingleInstance()
				.PreserveExistingDefaults();

			builder
				.RegisterType<PX.Objects.SM.NotificationService>()
				.As<PX.SM.INotificationService>()
				.SingleInstance();
		}

		private void RegisterEPModuleServices(ContainerBuilder builder)
		{
			builder
				.RegisterType<EPEventVCalendarProcessor>()
				.As<PX.Objects.EP.Imc.IVCalendarProcessor>()
				.SingleInstance();

			builder
				.RegisterType<PX.Objects.EP.Imc.VCalendarFactory>()
				.As<PX.Objects.EP.Imc.IVCalendarFactory>()
				.SingleInstance();

			builder
				.RegisterType<PX.Objects.EP.ActivityService>()
				.As<IActivityService>()
				.SingleInstance();

			builder
				.RegisterType<EP.ReportNotificationGenerator>()
				.AsSelf()
				.InstancePerDependency();
		}

		private void RegisterMailServices(ContainerBuilder builder)
		{
			builder
				.RegisterType<PX.Objects.EP.CommonMailSendProvider>()
				.As<IMailSendProvider>()
				.SingleInstance();

			builder
				.RegisterType<PX.Objects.EP.CommonMailReceiveProvider>()
				.UsingConstructor(typeof(IEmailProcessorsProvider))
				.As<IMailReceiveProvider>()
				.As<IMessageProccessor>()
				.As<IOriginalMailProvider>()
				.SingleInstance();

			var assemblies = BuildManager.GetReferencedAssemblies().Cast<Assembly>().ToArray();
			builder
				.RegisterAssemblyTypes(assemblies)
				.AssignableTo<IEmailProcessor>()
				.As<IEmailProcessor>()
				.SingleInstance();

			builder
				.RegisterType<OrderedEmailProcessorsProvider>()
				.As<IEmailProcessorsProvider>()
				.SingleInstance()
				.PreserveExistingDefaults();
		}
	}
}
