using System;

namespace PX.Objects.Common
{
	public class SimpleScope : IDisposable
	{
		private readonly Action _onClose;

		public SimpleScope(Action onOpen, Action onClose)
		{
			onOpen();
			_onClose = onClose;
		}

		public void Dispose() => _onClose();
	}
}
