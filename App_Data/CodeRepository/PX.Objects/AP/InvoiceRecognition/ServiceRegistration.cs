using Autofac;
using PX.Objects.AP.InvoiceRecognition.Feedback;
using PX.Objects.AP.InvoiceRecognition.VendorSearch;

namespace PX.Objects.AP.InvoiceRecognition
{
	internal class ServiceRegistration : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder
                .RegisterType<InvoiceRecognitionService>()
                .As<IInvoiceRecognitionService>()
                .PreserveExistingDefaults();

            builder
                .RegisterType<ContactRepository>()
                .As<IContactRepository>()
                .SingleInstance();

            builder
                .RegisterType<VendorRepository>()
                .As<IVendorRepository>()
                .SingleInstance();

            builder
                .RegisterType<VendorSearchFeedbackBuilder>()
                .AsSelf();

            builder
                .RegisterType<VendorSearcher>()
                .As<IVendorSearchService>();

			builder
				.RegisterType<RecognizedRecordDetailsManager>()
				.AsSelf();
        }
    }
}
