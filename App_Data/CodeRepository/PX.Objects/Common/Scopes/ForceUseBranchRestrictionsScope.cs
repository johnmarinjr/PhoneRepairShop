using PX.Common;
using PX.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PX.Objects.Common.Scopes
{
	public sealed class ForceUseBranchRestrictionsScope : IDisposable
	{
		private class BoolWrapper
		{
			public bool Value { get; set; }
			public BoolWrapper() { this.Value = false; }
		}

		private static string _SLOT_KEY = "ForceUseBranchRestrictionsScope_Running";

		public ForceUseBranchRestrictionsScope()
		{
			BoolWrapper val = PXDatabase.GetSlot<BoolWrapper>(_SLOT_KEY);
			val.Value = true;
		}

		public void Dispose()
		{
			PXDatabase.ResetSlot<BoolWrapper>(_SLOT_KEY);
		}

		public static bool IsRunning
		{
			get
			{
				return PXDatabase.GetSlot<BoolWrapper>(_SLOT_KEY).Value;
			}
		}
	}
}
