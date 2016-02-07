using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace NCrawler.Utils
{
	public class ColorConsoleTraceListener : ConsoleTraceListener
	{
		#region Readonly & Static Fields

		private readonly Dictionary<TraceEventType, ConsoleColor> _eventColor =
			new Dictionary<TraceEventType, ConsoleColor>();

		#endregion

		#region Constructors

		public ColorConsoleTraceListener()
		{
			_eventColor.Add(TraceEventType.Verbose, ConsoleColor.DarkGray);
			_eventColor.Add(TraceEventType.Information, ConsoleColor.Gray);
			_eventColor.Add(TraceEventType.Warning, ConsoleColor.Yellow);
			_eventColor.Add(TraceEventType.Error, ConsoleColor.DarkRed);
			_eventColor.Add(TraceEventType.Critical, ConsoleColor.Red);
			_eventColor.Add(TraceEventType.Start, ConsoleColor.DarkCyan);
			_eventColor.Add(TraceEventType.Stop, ConsoleColor.DarkCyan);
		}

		#endregion

		#region Instance Methods

		public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id,
			string message)
		{
			TraceEvent(eventCache, source, eventType, id, "{0}", message);
		}

		public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id,
			string format, params object[] args)
		{
			ConsoleColor originalColor = Console.ForegroundColor;
			Console.ForegroundColor = GetEventColor(eventType, originalColor);
			base.TraceEvent(eventCache, DateTime.UtcNow.ToString(), eventType, id, format, args);
			Console.ForegroundColor = originalColor;
		}

		private ConsoleColor GetEventColor(TraceEventType eventType, ConsoleColor defaultColor)
		{
			return !_eventColor.ContainsKey(eventType) ? defaultColor : _eventColor[eventType];
		}

		#endregion
	}
}