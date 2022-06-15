using System;
using PX.Common;

namespace PX.Objects.CR.Wizard
{
	/// <summary>
	/// The scope that shows that the current execution stack is inside an action that is triggered from the wizard.
	/// </summary>
	/// <remarks>
	/// The class can be used by actions that can be triggered from the wizard and outside the wizard.
	/// </remarks>
	[PXInternalUseOnly]
	public class WizardScope : IDisposable
	{
		private readonly bool prevState = false;
		private const string _WizardScope_ = nameof(_WizardScope_);

		public static bool IsScoped
		{
			get => PXContext.GetSlot<bool>(_WizardScope_);
		}

		public WizardScope()
		{
			prevState = IsScoped;

			PXContext.SetSlot<bool>(_WizardScope_, true);
		}

		public void Dispose()
		{
			PXContext.SetSlot<bool>(_WizardScope_, prevState);
		}
	}
}
