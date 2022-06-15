using System;
using System.Collections.Generic;

namespace PX.Objects.EP
{
	/// <summary>
	/// Interface for email processors provider.
	/// </summary>
	public interface IEmailProcessorsProvider
	{
		/// <summary>
		/// Gets the collection of email processors.
		/// </summary>
		/// <returns/>
		IEnumerable<IEmailProcessor> GetEmailProcessors();
	}
}
