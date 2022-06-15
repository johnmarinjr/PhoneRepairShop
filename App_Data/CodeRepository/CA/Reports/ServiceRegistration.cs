using Autofac;

namespace PX.Objects.Localizations.CA.Reports
{
	public class ServiceRegistration : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.
                RunOnApplicationStart(RegisterCustomFunctions.RegisterReportFunctions);
        }
    }

    public class RegisterCustomFunctions
    {
        public static void RegisterReportFunctions()
        {
            System.Type commonReportFunctionsType = typeof(CanadaCustomReportFunctions);
            PX.Common.Parser.ExpressionContext.RegisterExternalObject("CACustomFunctions", System.Activator.CreateInstance(commonReportFunctionsType));
        }
    }
}
