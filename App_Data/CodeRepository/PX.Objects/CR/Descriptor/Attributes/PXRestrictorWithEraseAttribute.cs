using System;
using PX.Data;
using PX.Objects.IN.Attributes;

namespace PX.Objects.CR
{
	public class PXRestrictorWithEraseAttribute : RestrictorWithParametersAttribute
	{
		#region ctor

		public PXRestrictorWithEraseAttribute(Type where, string message, params Type[] pars)
			: base(where, message, pars)
		{
			this.ShowWarning = true;
		}

		#endregion

		#region Events

		protected override PXException TryVerify(PXCache sender, PXFieldVerifyingEventArgs e, bool IsErrorValueRequired)
		{
			var ex = base.TryVerify(sender, e, IsErrorValueRequired);

			sender.AdjustUI(e.Row)
				.For(this.FieldName, attribute =>
				{
					// in case no error is still apllied to the field - try to add new error. If the error exists already - just skip
					if (attribute.ErrorLevel == PXErrorLevel.Undefined)
					{
						attribute.ExceptionHandling(sender, new PXExceptionHandlingEventArgs(e.Row, null, ex != null
							? new PXSetPropertyException(ex.MessageNoPrefix, PXErrorLevel.Error)
							: null));
					}
				});

			return ex;
		}

		#endregion
	}
}
