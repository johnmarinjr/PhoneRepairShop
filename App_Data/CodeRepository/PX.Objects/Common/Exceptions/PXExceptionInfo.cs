using PX.Data;
using System;

namespace PX.Objects.Common.Exceptions
{
	public class PXExceptionInfo
	{
		public string MessageFormat { get; }

		public object[] MessageArguments { get; set; }

		public PXErrorLevel? ErrorLevel { get; set; }

		public string Css { get; set; }

		public PXExceptionInfo(string messageFormat, params object[] messageArgs)
		{
			MessageFormat = messageFormat;
			MessageArguments = messageArgs ?? Array.Empty<object>();
		}

		public PXExceptionInfo(PXErrorLevel errorLevel, string messageFormat, params object[] messageArgs)
			: this(messageFormat, messageArgs)
		{
			ErrorLevel = errorLevel;
		}

		public PXSetPropertyException ToSetPropertyException()
        {
			var errorLevel = ErrorLevel ?? PXErrorLevel.Warning;
			if (string.IsNullOrEmpty(Css))
				return new PXSetPropertyException(MessageFormat, errorLevel, MessageArguments);
			return new PXSetPropertyException($"|css={Css}|{PXMessages.LocalizeFormatNoPrefix(MessageFormat, MessageArguments)}", errorLevel);
		}
	}
}
