using System;
using System.Collections.Generic;
using System.Linq;
using PX.Common;

using PX.Data;

namespace PX.Objects.Common.Extensions
{
	public abstract class GraphContextExtention<TGraph>: PXGraphExtension<TGraph>
		where TGraph : PXGraph
	{
		public enum Context
		{
			None,
			Release,
			Persist
		}

		public Context GraphContext { get; set; } = Context.None;
	}
}
