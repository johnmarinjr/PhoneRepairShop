using System.Runtime.CompilerServices;
using PX.Common;

namespace PX.Objects.CR
{
	/// <summary>
	/// The service scope that allows you to execute a method without implicit recursion.
	/// For instance, it can be used when a field event triggers an update that triggers this event once again, which leads to infinite recursion.
	/// </summary>
	/// <remarks>
	/// The class uses slots in <see cref="PXContext"/>. It gives a name of the caller member according to the source code caller info,
	/// however it could be specified manually.
	/// </remarks>
	[PXInternalUseOnly]
	public static class PreventRecursionCall
	{
		/// <summary>
		/// Executes the action and prevents subsequent calls of this method inside this action.
		/// </summary>
		/// <param name="action">The action.</param>
		/// <param name="memberName">The caller member name, which can be specified manually, but is usually filled by the compiler.</param>
		/// <param name="sourceFilePath">The source file path.</param>
		/// <param name="sourceLineNumber">The source line number.</param>
		public static void Execute(System.Action action,
			[CallerMemberName] string memberName = "",
			[CallerFilePath] string sourceFilePath = "",
			[CallerLineNumber] int sourceLineNumber = 0)
		{
			var name = memberName + sourceFilePath + sourceLineNumber;
			if (PXContext.GetSlot<bool>(name) is true)
				return;

			try
			{
				PXContext.SetSlot(name, true);
				action();
			}
			finally
			{
				PXContext.SetSlot(name, false);
			}
		}
	}
}
