using System;
using System.Runtime.Serialization;
using PX.Common;
using PX.Data;

namespace PX.Objects.CR.Wizard
{
	/// <summary>
	/// The base exception that is used for the wizard-specific navigation.
	/// </summary>
	[PXInternalUseOnly]
	public abstract class CRWizardException : PXClosePopupException
	{
		protected CRWizardException(string message) : base(message) { }
		protected CRWizardException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}

	/// <summary>
	/// The exception that shows that the <see cref="WizardResult.Abort"/> button is clicked in the wizard.
	/// </summary>
	[PXInternalUseOnly]
	public class CRWizardAbortException : CRWizardException
	{
		public CRWizardAbortException() : base("Wizard Abort") { }
		public CRWizardAbortException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}

	/// <summary>
	/// The exception that shows that the <see cref="WizardResult.Back"/> button is clicked in the wizard.
	/// </summary>
	[PXInternalUseOnly]
	public class CRWizardBackException : CRWizardException
	{
		public CRWizardBackException() : base("Wizard Back") { }
		public CRWizardBackException(SerializationInfo info, StreamingContext context) : base(info, context) { }
	}

	/// <summary>
	/// The service exception that is used to show the exception in the UI during <see cref="PXGraph.ExecuteUpdate"/>.
	/// </summary>
	/// <remarks>
	/// All exceptions that are not inherited from <see cref="PXBaseRedirectException"/>
	/// are hidden by <see cref="PXGraph.ExecuteUpdate"/>.
	/// This exception is a wrapper for a normal exception that for some reason (such as, because it is thrown in the wizard),
	/// cannot be thrown or shown the usual way.
	/// </remarks>
	[PXInternalUseOnly]
	public class CRWrappedRedirectException : PXBaseRedirectException
	{
		public CRWrappedRedirectException(Exception exception) : this(exception.Message)
		{
			PXTrace.WriteError(exception);
		}

		public CRWrappedRedirectException(string message) : base(message) { }

		public CRWrappedRedirectException(SerializationInfo info, StreamingContext context)
			: base(info, context) { }
	}
}
