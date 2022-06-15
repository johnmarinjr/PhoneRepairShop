using PX.Data;
using PX.Objects.CS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.Common.Exceptions
{
	public class FeatureIsDisabledException<TFeature> : PXException
		where TFeature : IBqlField
	{
		public FeatureIsDisabledException()
			: base(Messages.TheFeatureIsDisabled, GetFeatureName(typeof(TFeature)))
		{
		}

		public FeatureIsDisabledException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
		}

		protected static string GetFeatureName(Type featureField)
		{
			var featureProperty = BqlCommand.GetItemType(featureField)?.GetProperties()
				.Where(p => p.Name.Equals(featureField.Name, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();

			var featureAttribute = featureProperty?.GetCustomAttributes(typeof(FeatureAttribute), true)
				.FirstOrDefault() as FeatureAttribute;

			return featureAttribute?.DisplayName ?? featureProperty?.Name ?? featureField.Name;
		}
	}
}
