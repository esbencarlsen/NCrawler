using System;
using System.IO;

using NGet.Utils;

namespace NGet
{
	public class Arguments
	{
		#region Fields

		public TextWriter DefaultOutput = Console.Out;
		public OptionSet m_LoggingAndInputFileArgumentOptionSet;
		public OptionSet m_StartupArgumentOptionSet;

		#endregion

		#region Constructors

		public Arguments()
		{
			m_StartupArgumentOptionSet = new OptionSet
				{
					{"V|version", "display the version of Wget and exit.", v => ShowVersionInformation()},
					{"h|help|?", "print this help.", v => ShowUsageFull()},
					{"b|background", "go to background after startup.", v => SetBackGroundFlag()},
				};
			m_LoggingAndInputFileArgumentOptionSet = new OptionSet
				{
					{"o|output-file=", "log messages to {FILE}.", v => SetOutputOutputToFileOption()},
					{"a|append-output=", "append messages to {FILE}.", v => SetOutputAppendToFileOption()},
					{"d|debug", "print lots of debugging information.", v => SetShowDebugInformation()},
					{"q|quiet", "quiet (no output).", v => SetQuietOption()},
					{"v|verbose", "be verbose (this is the default).", v => SetVerboseOption()},
					{"nv|no-verbose", "turn off verboseness, without being quiet.", v => SetNoVerboseOption()},
					{"i|input-file=", "download URLs found in {FILE}.", file => SetInputFileOption(file)},
					{"F|force-html", "treat input file as HTML.", v => SetInputFileIsHtmlOption()},
					{"B|base=", "prepends {URL} to relative links in -F -i file.", v => SetPrependUrlOption()},
				};
		}

		#endregion

		#region Instance Properties

		public bool Verbose { get; private set; }

		#endregion

		#region Instance Methods

		public void ShowUsageFull()
		{
			DefaultOutput.WriteLine("nget 1.0.0.0, a non-interactive network retriever.");
			DefaultOutput.WriteLine("Usage: nget [OPTION]... [URL]...");
			DefaultOutput.WriteLine();
			DefaultOutput.WriteLine("Mandatory arguments to long options are mandatory for short options too.");
			DefaultOutput.WriteLine();
			DefaultOutput.WriteLine("Startup:");
			m_StartupArgumentOptionSet.WriteOptionDescriptions(DefaultOutput);
			DefaultOutput.WriteLine();

			DefaultOutput.WriteLine("Logging and input file:");
			m_LoggingAndInputFileArgumentOptionSet.WriteOptionDescriptions(DefaultOutput);
		}

		public void ShowUsageShort()
		{
			DefaultOutput.WriteLine("nget: missing URL");
			DefaultOutput.WriteLine("Usage: nget [OPTION]... [URL]...");
			DefaultOutput.WriteLine();
			DefaultOutput.WriteLine("Try 'nget --help' for more options.");
		}

		private void SetBackGroundFlag()
		{
		}

		private void SetInputFileIsHtmlOption()
		{
		}

		private void SetInputFileOption(string value)
		{
		}

		private void SetNoVerboseOption()
		{
		}

		private void SetOutputAppendToFileOption()
		{
		}

		private void SetOutputOutputToFileOption()
		{
		}

		private void SetPrependUrlOption()
		{
		}

		private void SetQuietOption()
		{
		}

		private void SetShowDebugInformation()
		{
		}

		private void SetVerboseOption()
		{
			Verbose = true;
		}

		private void ShowVersionInformation()
		{
		}

		#endregion
	}
}