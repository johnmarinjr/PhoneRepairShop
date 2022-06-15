using PX.Data;
using PX.Data.ReferentialIntegrity.Attributes;
using PX.OAuthClient.DAC;

namespace PX.Objects.Localizations.GB.HMRC.DAC
{
	[PXHidden]
	public class HMRCOAuthApplication : OAuthApplication
	{
		public class PK : PrimaryKeyOf<HMRCOAuthApplication>.By<applicationID>
		{
			public static HMRCOAuthApplication Find(PXGraph graph, int? applicationID) => FindBy(graph, applicationID);
		}

		#region OAuthApplication
		public new abstract class applicationID : PX.Data.BQL.BqlInt.Field<applicationID> { }
		public new abstract class type : PX.Data.BQL.BqlString.Field<type> { }
		public new abstract class applicationName : PX.Data.BQL.BqlString.Field<applicationName> { }
		public new abstract class clientID : PX.Data.BQL.BqlString.Field<clientID> { }
		public new abstract class clientSecret : PX.Data.BQL.BqlString.Field<clientSecret> { }
		public class HMRCApplicationType : PX.Data.BQL.BqlString.Constant<HMRCApplicationType>
		{
			public HMRCApplicationType()
				: base(MTDCloudApplicationProcessor.Type)
			{

			}
		}
		#endregion
	}
}
