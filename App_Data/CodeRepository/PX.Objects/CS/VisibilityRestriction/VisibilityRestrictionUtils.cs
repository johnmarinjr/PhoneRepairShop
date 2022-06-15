using PX.Data;
using PX.Objects.AR;

namespace PX.Objects.CS
{
	/// <summary>
	/// Visibility Restriction utilits
	/// </summary>
	public static class VisibilityRestriction
	{
		/// <summary>
		/// Default BAccountID, that means, that visibility restriction is not set.
		/// </summary>
		public const int EmptyBAccountID = 0;

		/// <summary>
		/// Checks, if BAccountID, that is set as 'Restrict Visibility To' for some entity is empty (or default).
		/// </summary>
		/// <param name="restrictVisibilityToBAccountID"></param>
		/// <returns></returns>
		public static bool IsEmpty(int? restrictVisibilityToBAccountID)
		{
			return restrictVisibilityToBAccountID == null || restrictVisibilityToBAccountID == EmptyBAccountID;
		}

		/// <summary>
		/// Checks, if BAccountID, that is set as 'Restrict Visibility To' for some entity is not empty (or default).
		/// </summary>
		/// <param name="restrictVisibilityToBAccountID"></param>
		/// <returns></returns>
		public static bool IsNotEmpty(int? restrictVisibilityToBAccountID)
		{
			return !IsEmpty(restrictVisibilityToBAccountID);
		}
	}
}