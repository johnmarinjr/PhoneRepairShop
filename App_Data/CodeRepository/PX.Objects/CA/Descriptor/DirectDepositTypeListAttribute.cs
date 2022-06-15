using System.Collections.Generic;
using CommonServiceLocator;
using PX.Data;

namespace PX.Objects.CA
{
	public partial class DirectDepositTypeListAttribute : PXStringListAttribute
	{
		[InjectDependency]
		protected DirectDepositTypeService DirectDepositService { get; set; }

		public override void CacheAttached(PXCache sender)
		{
			//workaround for unit tests environment
			var records = DirectDepositService?.GetDirectDepositTypes() ?? new List<DirectDepositType>();
			List<string> codes = new List<string>();
			List<string> descriptions = new List<string>();

			foreach (var record in records)
			{
				codes.Add(record.Code);
				descriptions.Add(record.Description);
			}
			_AllowedValues = codes.ToArray();
			_AllowedLabels = descriptions.ToArray();

			base.CacheAttached(sender);
		}
	}
}
