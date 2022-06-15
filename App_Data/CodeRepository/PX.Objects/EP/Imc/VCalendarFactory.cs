using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using PX.Common;
using PX.Common.Service;
using PX.Export.Imc;

namespace PX.Objects.EP.Imc
{
	[PXInternalUseOnly]
	public interface IVCalendarProcessor
	{
		void Process(vEvent card, object item);
	}

	[PXInternalUseOnly]
	public interface IVCalendarFactory
	{
		vCalendar CreateVCalendar(IEnumerable events);

		vEvent CreateVEvent(object item);
	}

	public class VCalendarFactory : IVCalendarFactory
	{
		private const string OneOfRecordsIsNull = "One of records is null";

		private readonly IEnumerable<IVCalendarProcessor> _calendarProcessors;

		public VCalendarFactory(IEnumerable<IVCalendarProcessor> calendarProcessors)
		{
			_calendarProcessors = calendarProcessors;
		}

		public vCalendar CreateVCalendar(IEnumerable events)
		{
			events.ThrowOnNull(nameof(events));

			var calendar = new vCalendar();

			foreach (object item in events)
			{
				item.ThrowOnNull(nameof(events), message: OneOfRecordsIsNull);
				var @event = CreateVEvent(item);

				if (@event != vEvent.Empty) 
					calendar.AddEvent(@event);
			}
			return calendar;
		}

		public vEvent CreateVEvent(object item)
		{
			var card = new vEvent();

			foreach (IVCalendarProcessor handler in _calendarProcessors)
				handler.Process(card, item);

			return card;
		}
	}
}
