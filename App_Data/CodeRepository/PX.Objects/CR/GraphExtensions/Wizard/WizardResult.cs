using System;
using PX.Common;
using PX.Data;

namespace PX.Objects.CR.Wizard
{
	/// <summary>
	/// The aliases for <see cref="WebDialogResult"/> that are used inside the wizard.
	/// </summary>
	[PXInternalUseOnly]
	public static class WizardResult
	{
		/// <summary>
		/// The abort (Cancel) button.
		/// </summary>
		/// <remarks>
		/// Formally it is an abort button, but in the UI, it's name is Cancel.
		/// </remarks>
		public const WebDialogResult Abort = WebDialogResult.Abort;
		/// <summary>
		/// The Back button.
		/// </summary>
		public const WebDialogResult Back = (WebDialogResult)72; // just a random (high) enough number to avoid collision
	}
}
