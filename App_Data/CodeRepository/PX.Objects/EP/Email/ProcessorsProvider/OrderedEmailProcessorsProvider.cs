using System;
using System.Linq;
using System.Collections.Generic;

namespace PX.Objects.EP
{
	/// <summary>
	/// Email processors provider. Accepts all <see cref="IEmailProcessor"/>s registered with Autofac, orders them and returns ordered collection.
	/// </summary>
	internal class OrderedEmailProcessorsProvider : IEmailProcessorsProvider
	{
		private static readonly Dictionary<Type, double> _executionOrderByProcessorType = new Dictionary<Type, double>
		{
			[typeof(ConversationEmailProcessor)]                    = 1,
			[typeof(ConfirmReceiptEmailProcessor)]                  = 2,
			[typeof(DefaultEmailProcessor)]                         = 3,
			[typeof(ExchangeEmailProcessor)]                        = 4,
			[typeof(PX.Objects.CR.CaseCommonEmailProcessor)]        = 5,
			[typeof(RouterEmailProcessor)]                          = 6,
			[typeof(PX.Objects.CR.NewCaseEmailProcessor)]           = 7,
			[typeof(PX.Objects.CR.ContactBAccountEmailProcessor)]   = 8,
			[typeof(NotificationEmailProcessor)]                    = 9,
			[typeof(UnassignedEmailProcessor)]                      = 10,
			[typeof(CleanerEmailProcessor)]                         = 11,
			[typeof(AssignmentEmailProcessor)]                      = 12,
			[typeof(PX.Objects.CR.NewLeadEmailProcessor)]           = 13,
			[typeof(ImageExtractorEmailProcessor)]                  = 14,
			[typeof(AP.InvoiceRecognition.APInvoiceEmailProcessor)] = 15
		};


		private readonly List<IEmailProcessor> _orderedEmailProcessors;

		public OrderedEmailProcessorsProvider(IEnumerable<IEmailProcessor> emailProcessors)
		{
			_orderedEmailProcessors = emailProcessors.OrderBy(GetProcessorOrder).ToList();
		}

		/// <summary>
		/// Gets <see cref="IEmailProcessor"/> execution order. A processor with a higher order will be executed after a processor with lower order.
		/// <see cref="double.MaxValue"/> is the highest order assigned to custom email processors not dependent on the order of execution.
		/// </summary>
		/// <param name="emailProcessor">The email processor.</param>
		/// <returns>
		/// The processor's order.
		/// </returns>
		private double GetProcessorOrder(IEmailProcessor emailProcessor)
		{
			return _executionOrderByProcessorType.TryGetValue(emailProcessor.GetType(), out double order)
				? order
				: double.MaxValue;
		}

		/// <summary>
		/// Gets the collection of email processors. Order-dependent processors would be returned first followed by custom email processors.
		/// </summary>
		/// <returns/>
		public IEnumerable<IEmailProcessor> GetEmailProcessors() => _orderedEmailProcessors;
	}
}
