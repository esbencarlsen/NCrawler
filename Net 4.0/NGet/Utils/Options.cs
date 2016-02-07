//
// Options.cs
//
// Authors:
//  Jonathan Pryor <jpryor@novell.com>
//
// Copyright (C) 2008 Novell (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

// Compile With:
//   gmcs -debug+ -r:System.Core Options.cs -o:NDesk.Options.dll
//   gmcs -debug+ -d:LINQ -r:System.Core Options.cs -o:NDesk.Options.dll
//
// The LINQ version just changes the implementation of
// OptionSet.Parse(IEnumerable<string>), and confers no semantic changes.

//
// A Getopt::Long-inspired option parsing library for C#.
//
// NDesk.Options.OptionSet is built upon a key/value table, where the
// key is a option format string and the value is a delegate that is 
// invoked when the format string is matched.
//
// Option format strings:
//  Regex-like BNF Grammar: 
//    name: .+
//    type: [=:]
//    sep: ( [^{}]+ | '{' .+ '}' )?
//    aliases: ( name type sep ) ( '|' name type sep )*
// 
// Each '|'-delimited name is an alias for the associated action.  If the
// format string ends in a '=', it has a required value.  If the format
// string ends in a ':', it has an optional value.  If neither '=' or ':'
// is present, no value is supported.  `=' or `:' need only be defined on one
// alias, but if they are provided on more than one they must be consistent.
//
// Each alias portion may also end with a "key/value separator", which is used
// to split option values if the option accepts > 1 value.  If not specified,
// it defaults to '=' and ':'.  If specified, it can be any character except
// '{' and '}' OR the *string* between '{' and '}'.  If no separator should be
// used (i.e. the separate values should be distinct arguments), then "{}"
// should be used as the separator.
//
// Options are extracted either from the current option by looking for
// the option name followed by an '=' or ':', or is taken from the
// following option IFF:
//  - The current option does not contain a '=' or a ':'
//  - The current option requires a value (i.e. not a Option type of ':')
//
// The `name' used in the option format string does NOT include any leading
// option indicator, such as '-', '--', or '/'.  All three of these are
// permitted/required on any named option.
//
// Option bundling is permitted so long as:
//   - '-' is used to start the option group
//   - all of the bundled options are a single character
//   - at most one of the bundled options accepts a value, and the value
//     provided starts from the next character to the end of the string.
//
// This allows specifying '-a -b -c' as '-abc', and specifying '-D name=value'
// as '-Dname=value'.
//
// Option processing is disabled by specifying "--".  All options after "--"
// are returned by OptionSet.Parse() unchanged and unprocessed.
//
// Unprocessed options are returned from OptionSet.Parse().
//
// Examples:
//  int verbose = 0;
//  OptionSet p = new OptionSet ()
//    .Add ("v", v => ++verbose)
//    .Add ("name=|value=", v => Console.WriteLine (v));
//  p.Parse (new string[]{"-v", "--v", "/v", "-name=A", "/name", "B", "extra"});
//
// The above would parse the argument string array, and would invoke the
// lambda expression three times, setting `verbose' to 3 when complete.  
// It would also print out "A" and "B" to standard output.
// The returned array would contain the string "extra".
//
// C# 3.0 collection initializers are supported and encouraged:
//  var p = new OptionSet () {
//    { "h|?|help", v => ShowHelp () },
//  };
//
// System.ComponentModel.TypeConverter is also supported, allowing the use of
// custom data types in the callback type; TypeConverter.ConvertFromString()
// is used to convert the value option to an instance of the specified
// type:
//
//  var p = new OptionSet () {
//    { "foo=", (Foo f) => Console.WriteLine (f.ToString ()) },
//  };
//
// Random other tidbits:
//  - Boolean options (those w/o '=' or ':' in the option format string)
//    are explicitly enabled if they are followed with '+', and explicitly
//    disabled if they are followed with '-':
//      string a = null;
//      var p = new OptionSet () {
//        { "a", s => a = s },
//      };
//      p.Parse (new string[]{"-a"});   // sets v != null
//      p.Parse (new string[]{"-a+"});  // sets v != null
//      p.Parse (new string[]{"-a-"});  // sets v == null
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Text;
using System.Text.RegularExpressions;

using NCrawler.HtmlProcessor.Properties;

namespace NGet.Utils
{
	public class OptionValueCollection : IList, IList<string>
	{
		#region Readonly & Static Fields

		private readonly OptionContext m_C;
		private readonly List<string> m_Values = new List<string>();

		#endregion

		#region Constructors

		internal OptionValueCollection(OptionContext c)
		{
			m_C = c;
		}

		#endregion

		#region Instance Methods

		public override string ToString()
		{
			return string.Join(", ", m_Values.ToArray());
		}

		public string[] ToArray()
		{
			return m_Values.ToArray();
		}

		public List<string> ToList()
		{
			return new List<string>(m_Values);
		}

		/// <exception cref="InvalidOperationException">OptionContext.Option is null.</exception>
		/// <exception cref="ArgumentOutOfRangeException"><c>index</c> is out of range.</exception>
		/// <exception cref="OptionException"><c>OptionException</c>.</exception>
		private void AssertValid(int index)
		{
			if (m_C.Option == null)
			{
				throw new InvalidOperationException("OptionContext.Option is null.");
			}

			if (index >= m_C.Option.MaxValueCount)
			{
				throw new ArgumentOutOfRangeException("index");
			}

			if (m_C.Option.OptionValueType == OptionValueType.Required && index >= m_Values.Count)
			{
				throw new OptionException(string.Format(
					m_C.OptionSet.MessageLocalizer("Missing required value for option '{0}'."), m_C.OptionName),
					m_C.OptionName);
			}
		}

		#endregion

		#region IList Members

		void ICollection.CopyTo(Array array, int index)
		{
			(m_Values as ICollection).CopyTo(array, index);
		}

		bool ICollection.IsSynchronized
		{
			get { return (m_Values as ICollection).IsSynchronized; }
		}

		object ICollection.SyncRoot
		{
			get { return (m_Values as ICollection).SyncRoot; }
		}

		public void Clear()
		{
			m_Values.Clear();
		}

		public int Count
		{
			get { return m_Values.Count; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return m_Values.GetEnumerator();
		}

		int IList.Add(object value)
		{
			return (m_Values as IList).Add(value);
		}

		bool IList.Contains(object value)
		{
			return (m_Values as IList).Contains(value);
		}

		int IList.IndexOf(object value)
		{
			return (m_Values as IList).IndexOf(value);
		}

		void IList.Insert(int index, object value)
		{
			(m_Values as IList).Insert(index, value);
		}

		void IList.Remove(object value)
		{
			(m_Values as IList).Remove(value);
		}

		void IList.RemoveAt(int index)
		{
			(m_Values as IList).RemoveAt(index);
		}

		bool IList.IsFixedSize
		{
			get { return false; }
		}

		object IList.this[int index]
		{
			get { return this[index]; }
			set { (m_Values as IList)[index] = value; }
		}

		#endregion

		#region IList<string> Members

		public void Add(string item)
		{
			m_Values.Add(item);
		}

		public bool Contains(string item)
		{
			return m_Values.Contains(item);
		}

		public void CopyTo(string[] array, int arrayIndex)
		{
			m_Values.CopyTo(array, arrayIndex);
		}

		public bool Remove(string item)
		{
			return m_Values.Remove(item);
		}

		public IEnumerator<string> GetEnumerator()
		{
			return m_Values.GetEnumerator();
		}

		public int IndexOf(string item)
		{
			return m_Values.IndexOf(item);
		}

		public void Insert(int index, string item)
		{
			m_Values.Insert(index, item);
		}

		public void RemoveAt(int index)
		{
			m_Values.RemoveAt(index);
		}

		public string this[int index]
		{
			get
			{
				AssertValid(index);
				return index >= m_Values.Count ? null : m_Values[index];
			}
			set { m_Values[index] = value; }
		}

		#endregion
	}

	public class OptionContext
	{
		#region Readonly & Static Fields

		#endregion

		#region Constructors

		public OptionContext(OptionSet set)
		{
			OptionSet = set;
			OptionValues = new OptionValueCollection(this);
		}

		#endregion

		#region Instance Properties

		public Option Option { get; set; }
		public int OptionIndex { get; set; }
		public string OptionName { get; set; }
		public OptionSet OptionSet { get; private set; }
		public OptionValueCollection OptionValues { get; private set; }

		#endregion
	}

	public enum OptionValueType
	{
		None,
		Optional,
		Required,
	}

	public abstract class Option
	{
		#region Readonly & Static Fields

		private static readonly char[] s_nameTerminator = new[] {'=', ':'};

		#endregion

		#region Fields

		#endregion

		#region Constructors

		protected Option(string prototype, string description)
			: this(prototype, description, 1)
		{
		}

		/// <exception cref="ArgumentNullException"><paramref name="prototype" /> is <c>null</c>.</exception>
		/// <exception cref="ArgumentException">Cannot be the empty string.</exception>
		/// <exception cref="ArgumentOutOfRangeException"><c>maxValueCount</c> is out of range.</exception>
		protected Option(string prototype, string description, int maxValueCount)
		{
			if (prototype == null)
			{
				throw new ArgumentNullException(@"prototype");
			}

			if (prototype.Length == 0)
			{
				throw new ArgumentException(Resources.Option_Option_Cannot_be_the_empty_string_, @"prototype");
			}

			if (maxValueCount < 0)
			{
				throw new ArgumentOutOfRangeException("maxValueCount");
			}

			Prototype = prototype;
			Names = prototype.Split('|');
			Description = description;
			MaxValueCount = maxValueCount;
			OptionValueType = ParsePrototype();

			if (MaxValueCount == 0 && OptionValueType != OptionValueType.None)
			{
				throw new ArgumentException(
					Resources.Option_Option_Cannot_provide_maxValueCount_of_0_for_OptionValueType_Required_or_OptionValueType_Optional_,
					"maxValueCount");
			}

			if (OptionValueType == OptionValueType.None && maxValueCount > 1)
			{
				throw new ArgumentException(
					string.Format("Cannot provide maxValueCount of {0} for OptionValueType.None.", maxValueCount),
					"maxValueCount");
			}

			if (Array.IndexOf(Names, "<>") >= 0 &&
				((Names.Length == 1 && OptionValueType != OptionValueType.None) ||
					(Names.Length > 1 && MaxValueCount > 1)))
			{
				throw new ArgumentException(
					Resources.Option_Option_The_default_option_handler______cannot_require_values_,
					@"prototype");
			}
		}

		#endregion

		#region Instance Properties

		public string Description { get; private set; }
		public int MaxValueCount { get; private set; }
		public OptionValueType OptionValueType { get; private set; }
		public static string Prototype { get; private set; }
		internal string[] Names { get; private set; }
		internal string[] ValueSeparators { get; private set; }

		#endregion

		#region Instance Methods

		public override string ToString()
		{
			return Prototype;
		}

		public string[] GetNames()
		{
			return (string[]) Names.Clone();
		}

		public string[] GetValueSeparators()
		{
			if (ValueSeparators == null)
			{
				return new string[0];
			}

			return (string[]) ValueSeparators.Clone();
		}

		public void Invoke(OptionContext c)
		{
			OnParseComplete(c);
			c.OptionName = null;
			c.Option = null;
			c.OptionValues.Clear();
		}

		protected abstract void OnParseComplete(OptionContext c);

		/// <exception cref="ArgumentException">prototype</exception>
		private OptionValueType ParsePrototype()
		{
			char type = '\0';
			List<string> seps = new List<string>();
			for (int i = 0; i < Names.Length; ++i)
			{
				string name = Names[i];
				if (name.Length == 0)
				{
					throw new ArgumentException(Resources.Option_ParsePrototype_Empty_option_names_are_not_supported_, Prototype);
				}

				int end = name.IndexOfAny(s_nameTerminator);
				if (end == -1)
				{
					continue;
				}

				Names[i] = name.Substring(0, end);
				if (type == '\0' || type == name[end])
				{
					type = name[end];
				}
				else
				{
					throw new ArgumentException(
						string.Format("Conflicting option types: '{0}' vs. '{1}'.", type, name[end]), Prototype);
				}

				AddSeparators(name, end, seps);
			}

			if (type == '\0')
			{
				return OptionValueType.None;
			}

			if (MaxValueCount <= 1 && seps.Count != 0)
			{
				throw new ArgumentException(
					string.Format("Cannot provide key/value separators for Options taking {0} value(s).", MaxValueCount), Prototype);
			}

			if (MaxValueCount > 1)
			{
				if (seps.Count == 0)
				{
					ValueSeparators = new[] {":", "="};
				}
				else if (seps.Count == 1 && seps[0].Length == 0)
				{
					ValueSeparators = null;
				}
				else
				{
					ValueSeparators = seps.ToArray();
				}
			}

			return type == '=' ? OptionValueType.Required : OptionValueType.Optional;
		}

		#endregion

		#region Class Methods

		/// <exception cref="OptionException"><c>OptionException</c>.</exception>
		protected static T Parse<T>(string value, OptionContext c)
		{
			TypeConverter conv = TypeDescriptor.GetConverter(typeof (T));
			T t = default (T);
			try
			{
				if (value != null)
				{
					if (conv != null)
					{
						t = (T) conv.ConvertFromString(value);
					}
				}
			}
			catch (Exception e)
			{
				throw new OptionException(
					string.Format(
						c.OptionSet.MessageLocalizer("Could not convert string `{0}' to type {1} for option `{2}'."),
						value, typeof (T).Name, c.OptionName),
					c.OptionName, e);
			}

			return t;
		}

		/// <exception cref="ArgumentException">prototype</exception>
		private static void AddSeparators(string name, int end, ICollection<string> seps)
		{
			int start = -1;
			for (int i = end + 1; i < name.Length; ++i)
			{
				switch (name[i])
				{
					case '{':
						if (start != -1)
						{
							throw new ArgumentException(
								string.Format("Ill-formed name/value separator found in \"{0}\".", name), Prototype);
						}

						start = i + 1;
						break;
					case '}':
						if (start == -1)
						{
							throw new ArgumentException(
								string.Format("Ill-formed name/value separator found in \"{0}\".", name), Prototype);
						}

						seps.Add(name.Substring(start, i - start));
						start = -1;
						break;
					default:
						if (start == -1)
						{
							seps.Add(name[i].ToString());
						}

						break;
				}
			}
			if (start != -1)
			{
				throw new ArgumentException(
					string.Format("Ill-formed name/value separator found in \"{0}\".", name), Prototype);
			}
		}

		#endregion
	}

	[Serializable]
	public class OptionException : Exception
	{
		#region Constructors

		public OptionException()
		{
		}

		public OptionException(string message, string optionName)
			: base(message)
		{
			OptionName = optionName;
		}

		public OptionException(string message, string optionName, Exception innerException)
			: base(message, innerException)
		{
			OptionName = optionName;
		}

		protected OptionException(SerializationInfo info, StreamingContext context)
			: base(info, context)
		{
			OptionName = info.GetString("OptionName");
		}

		#endregion

		#region Instance Properties

		public string OptionName { get; private set; }

		#endregion

		#region Instance Methods

		[SecurityPermission(SecurityAction.LinkDemand, SerializationFormatter = true)]
		public override void GetObjectData(SerializationInfo info, StreamingContext context)
		{
			base.GetObjectData(info, context);
			info.AddValue("OptionName", OptionName);
		}

		#endregion
	}

	public delegate void OptionAction<in TKey, in TValue>(TKey key, TValue value);

	public class OptionSet : KeyedCollection<string, Option>
	{
		#region Constants

		private const int OptionWidth = 29;

		#endregion

		#region Readonly & Static Fields

		private readonly Regex m_ValueOption = new Regex(
			@"^(?<flag>--|-|/)(?<name>[^:=]+)((?<sep>[:=])(?<value>.*))?$");

		#endregion

		#region Constructors

		public OptionSet()
			: this(f => f)
		{
		}

		public OptionSet(Converter<string, string> localizer)
		{
			MessageLocalizer = localizer;
		}

		#endregion

		#region Instance Properties

		public Converter<string, string> MessageLocalizer { get; private set; }

		#endregion

		#region Instance Methods

		public new OptionSet Add(Option option)
		{
			base.Add(option);
			return this;
		}

		public OptionSet Add(string prototype, Action<string> action)
		{
			return Add(prototype, null, action);
		}

		/// <exception cref="ArgumentNullException"><paramref name="action" /> is <c>null</c>.</exception>
		public OptionSet Add(string prototype, string description, Action<string> action)
		{
			if (action == null)
			{
				throw new ArgumentNullException("action");
			}

			Option p = new ActionOption(prototype, description, 1, v => action(v[0]));
			base.Add(p);
			return this;
		}

		public OptionSet Add(string prototype, OptionAction<string, string> action)
		{
			return Add(prototype, null, action);
		}

		/// <exception cref="ArgumentNullException"><paramref name="action" /> is <c>null</c>.</exception>
		public OptionSet Add(string prototype, string description, OptionAction<string, string> action)
		{
			if (action == null)
			{
				throw new ArgumentNullException("action");
			}

			Option p = new ActionOption(prototype, description, 2, v => action(v[0], v[1]));
			base.Add(p);
			return this;
		}

		public OptionSet Add<T>(string prototype, Action<T> action)
		{
			return Add(prototype, null, action);
		}

		public OptionSet Add<T>(string prototype, string description, Action<T> action)
		{
			return Add(new ActionOption<T>(prototype, description, action));
		}

		public OptionSet Add<TKey, TValue>(string prototype, OptionAction<TKey, TValue> action)
		{
			return Add(prototype, null, action);
		}

		public OptionSet Add<TKey, TValue>(string prototype, string description, OptionAction<TKey, TValue> action)
		{
			return Add(new ActionOption<TKey, TValue>(prototype, description, action));
		}

		public string[] Parse(IEnumerable<string> arguments)
		{
			bool process = true;
			OptionContext c = CreateOptionContext();
			c.OptionIndex = -1;
			Option def = this["<>"];
			IEnumerable<string> unprocessed =
				from argument in arguments
				where !(++c.OptionIndex >= 0 && (process || def != null)) || (process
					? argument == "--"
						? (process = false)
						: !Parse(argument, c) && (def == null || Unprocessed(null, def, c, argument))
					: def == null || Unprocessed(null, def, c, argument))
				select argument;

			if (c.Option != null)
			{
				c.Option.Invoke(c);
			}

			return unprocessed.ToArray();
		}

		public void WriteOptionDescriptions(TextWriter o)
		{
			foreach (Option p in this)
			{
				int written = 0;
				if (!WriteOptionPrototype(o, p, ref written))
				{
					continue;
				}

				if (written < OptionWidth)
				{
					o.Write(new string(' ', OptionWidth - written));
				}
				else
				{
					o.WriteLine();
					o.Write(new string(' ', OptionWidth));
				}

				List<string> lines = GetLines(MessageLocalizer(GetDescription(p.Description)));
				o.WriteLine(lines[0]);
				string prefix = new string(' ', OptionWidth + 2);
				for (int i = 1; i < lines.Count; ++i)
				{
					o.Write(prefix);
					o.WriteLine(lines[i]);
				}
			}
		}

		protected virtual OptionContext CreateOptionContext()
		{
			return new OptionContext(this);
		}

		protected virtual bool Parse(string argument, OptionContext c)
		{
			if (c.Option != null)
			{
				ParseValue(argument, c);
				return true;
			}

			string f, n, s, v;
			if (!GetOptionParts(argument, out f, out n, out s, out v))
			{
				return false;
			}

			if (Contains(n))
			{
				Option p = this[n];
				c.OptionName = f + n;
				c.Option = p;
				switch (p.OptionValueType)
				{
					case OptionValueType.None:
						c.OptionValues.Add(n);
						c.Option.Invoke(c);
						break;
					case OptionValueType.Optional:
					case OptionValueType.Required:
						ParseValue(v, c);
						break;
				}
				return true;
			}
			// no match; is it a bool option?
			if (ParseBool(argument, n, c))
			{
				return true;
			}

			// is it a bundled option?
			if (ParseBundledValue(f, string.Concat(n + s + v), c))
			{
				return true;
			}

			return false;
		}

		/// <exception cref="ArgumentNullException"><paramref name="item" /> is <c>null</c>.</exception>
		/// <exception cref="InvalidOperationException">Option has no names!</exception>
		protected override string GetKeyForItem(Option item)
		{
			if (item == null)
			{
				throw new ArgumentNullException("item");
			}

			if (item.Names != null && item.Names.Length > 0)
			{
				return item.Names[0];
			}

			// This should never happen, as it's invalid for Option to be
			// constructed w/o any names.
			throw new InvalidOperationException("Option has no names!");
		}

		protected override void InsertItem(int index, Option item)
		{
			base.InsertItem(index, item);
			AddImpl(item);
		}

		protected override void RemoveItem(int index)
		{
			base.RemoveItem(index);
			Option p = Items[index];
			// KeyedCollection.RemoveItem() handles the 0th item
			for (int i = 1; i < p.Names.Length; ++i)
			{
				Dictionary.Remove(p.Names[i]);
			}
		}

		protected override void SetItem(int index, Option item)
		{
			base.SetItem(index, item);
			RemoveItem(index);
			AddImpl(item);
		}

		/// <exception cref="ArgumentNullException"><paramref name="option" /> is <c>null</c>.</exception>
		[Obsolete("Use KeyedCollection.this[string]")]
		protected Option GetOptionForName(string option)
		{
			if (option == null)
			{
				throw new ArgumentNullException("option");
			}

			try
			{
				return base[option];
			}
			catch (KeyNotFoundException)
			{
				return null;
			}
		}

		/// <exception cref="ArgumentNullException"><paramref name="argument" /> is <c>null</c>.</exception>
		protected bool GetOptionParts(string argument, out string flag, out string name, out string sep, out string value)
		{
			if (argument == null)
			{
				throw new ArgumentNullException("argument");
			}

			flag = name = sep = value = null;
			Match m = m_ValueOption.Match(argument);
			if (!m.Success)
			{
				return false;
			}

			flag = m.Groups["flag"].Value;
			name = m.Groups["name"].Value;
			if (m.Groups["sep"].Success && m.Groups["value"].Success)
			{
				sep = m.Groups["sep"].Value;
				value = m.Groups["value"].Value;
			}

			return true;
		}

		/// <exception cref="ArgumentNullException"><paramref name="option" /> is <c>null</c>.</exception>
		private void AddImpl(Option option)
		{
			if (option == null)
			{
				throw new ArgumentNullException("option");
			}

			List<string> added = new List<string>(option.Names.Length);
			try
			{
				// KeyedCollection.InsertItem/SetItem handle the 0th name.
				for (int i = 1; i < option.Names.Length; ++i)
				{
					Dictionary.Add(option.Names[i], option);
					added.Add(option.Names[i]);
				}
			}
			catch (Exception)
			{
				foreach (string name in added)
				{
					Dictionary.Remove(name);
				}

				throw;
			}
		}

		private bool ParseBool(string option, string n, OptionContext c)
		{
			string rn;
			if (n.Length >= 1 && (n[n.Length - 1] == '+' || n[n.Length - 1] == '-') &&
				Contains((rn = n.Substring(0, n.Length - 1))))
			{
				Option p = this[rn];
				string v = n[n.Length - 1] == '+' ? option : null;
				c.OptionName = option;
				c.Option = p;
				c.OptionValues.Add(v);
				p.Invoke(c);
				return true;
			}
			
			return false;
		}

		/// <exception cref="OptionException"><c>OptionException</c>.</exception>
		/// <exception cref="InvalidOperationException"><c>InvalidOperationException</c>.</exception>
		private bool ParseBundledValue(string f, string n, OptionContext c)
		{
			if (f != "-")
			{
				return false;
			}

			for (int i = 0; i < n.Length; ++i)
			{
				string opt = f + n[i];
				string rn = n[i].ToString();
				if (!Contains(rn))
				{
					if (i == 0)
					{
						return false;
					}
					
					throw new OptionException(string.Format(MessageLocalizer(
						"Cannot bundle unregistered option '{0}'."), opt), opt);
				}
				Option p = this[rn];
				switch (p.OptionValueType)
				{
					case OptionValueType.None:
						Invoke(c, opt, n, p);
						break;
					case OptionValueType.Optional:
					case OptionValueType.Required:
						{
							string v = n.Substring(i + 1);
							c.Option = p;
							c.OptionName = opt;
							ParseValue(v.Length != 0 ? v : null, c);
							return true;
						}
					default:
						throw new InvalidOperationException("Unknown OptionValueType: " + p.OptionValueType);
				}
			}

			return true;
		}

		/// <exception cref="OptionException"><c>OptionException</c>.</exception>
		private void ParseValue(string option, OptionContext c)
		{
			if (option != null)
			{
				foreach (string o in c.Option.ValueSeparators != null
					? option.Split(c.Option.ValueSeparators, StringSplitOptions.None)
					: new[] {option})
				{
					c.OptionValues.Add(o);
				}
			}
			
			if (c.OptionValues.Count == c.Option.MaxValueCount ||
				c.Option.OptionValueType == OptionValueType.Optional)
			{
				c.Option.Invoke(c);
			}
			else if (c.OptionValues.Count > c.Option.MaxValueCount)
			{
				throw new OptionException(MessageLocalizer(string.Format(
					"Error: Found {0} option values when expecting {1}.",
					c.OptionValues.Count, c.Option.MaxValueCount)),
					c.OptionName);
			}
		}

		private bool WriteOptionPrototype(TextWriter o, Option p, ref int written)
		{
			string[] names = p.Names;

			int i = GetNextOptionIndex(names, 0);
			if (i == names.Length)
			{
				return false;
			}

			if (names[i].Length == 1)
			{
				Write(o, ref written, "  -");
				Write(o, ref written, names[0]);
			}
			else
			{
				Write(o, ref written, "      --");
				Write(o, ref written, names[0]);
			}

			for (i = GetNextOptionIndex(names, i + 1);
				i < names.Length;
				i = GetNextOptionIndex(names, i + 1))
			{
				Write(o, ref written, ", ");
				Write(o, ref written, names[i].Length == 1 ? "-" : "--");
				Write(o, ref written, names[i]);
			}

			if (p.OptionValueType == OptionValueType.Optional ||
				p.OptionValueType == OptionValueType.Required)
			{
				if (p.OptionValueType == OptionValueType.Optional)
				{
					Write(o, ref written, MessageLocalizer("["));
				}
				
				Write(o, ref written, MessageLocalizer("=" + GetArgumentName(0, p.MaxValueCount, p.Description)));
				string sep = p.ValueSeparators != null && p.ValueSeparators.Length > 0
					? p.ValueSeparators[0]
					: " ";
				
				for (int c = 1; c < p.MaxValueCount; ++c)
				{
					Write(o, ref written, MessageLocalizer(sep + GetArgumentName(c, p.MaxValueCount, p.Description)));
				}
				
				if (p.OptionValueType == OptionValueType.Optional)
				{
					Write(o, ref written, MessageLocalizer("]"));
				}
			}
			return true;
		}

		#endregion

		#region Class Methods

		private static string GetArgumentName(int index, int maxIndex, string description)
		{
			if (description == null)
			{
				return maxIndex == 1 ? "VALUE" : "VALUE" + (index + 1);
			}

			string[] nameStart = maxIndex == 1 ? new[] {"{0:", "{"} : new[] {"{" + index + ":"};
			foreach (string t in nameStart)
			{
				int start, j = 0;
				do
				{
					start = description.IndexOf(t, j);
				} while (start >= 0 && j != 0 && description[j++ - 1] == '{');
				if (start == -1)
				{
					continue;
				}

				int end = description.IndexOf("}", start);
				if (end == -1)
				{
					continue;
				}

				return description.Substring(start + t.Length, end - start - t.Length);
			}

			return maxIndex == 1 ? "VALUE" : "VALUE" + (index + 1);
		}

		/// <exception cref="InvalidOperationException"><c>InvalidOperationException</c>.</exception>
		private static string GetDescription(string description)
		{
			if (description == null)
				return string.Empty;
			StringBuilder sb = new StringBuilder(description.Length);
			int start = -1;
			for (int i = 0; i < description.Length; ++i)
			{
				switch (description[i])
				{
					case '{':
						if (i == start)
						{
							sb.Append('{');
							start = -1;
						}
						else if (start < 0)
						{
							start = i + 1;
						}

						break;
					case '}':
						if (start < 0)
						{
							if ((i + 1) == description.Length || description[i + 1] != '}')
							{
								throw new InvalidOperationException("Invalid option description: " + description);
							}

							++i;
							sb.Append("}");
						}
						else
						{
							sb.Append(description.Substring(start, i - start));
							start = -1;
						}
						break;
					case ':':
						if (start < 0)
						{
							goto default;
						}

						start = i + 1;
						break;
					default:
						if (start < 0)
						{
							sb.Append(description[i]);
						}

						break;
				}
			}
			return sb.ToString();
		}

		private static int GetLineEnd(int start, int length, string description)
		{
			int end = Math.Min(start + length, description.Length);
			int sep = -1;
			for (int i = start; i < end; ++i)
			{
				switch (description[i])
				{
					case ' ':
					case '\t':
					case '\v':
					case '-':
					case ',':
					case '.':
					case ';':
						sep = i;
						break;
					case '\n':
						return i;
				}
			}
			if (sep == -1 || end == description.Length)
			{
				return end;
			}

			return sep;
		}

		private static List<string> GetLines(string description)
		{
			List<string> lines = new List<string>();
			if (string.IsNullOrEmpty(description))
			{
				lines.Add(string.Empty);
				return lines;
			}

			const int length = 80 - OptionWidth - 2;
			int start = 0, end;
			do
			{
				end = GetLineEnd(start, length, description);
				bool cont = false;
				if (end < description.Length)
				{
					char c = description[end];
					if (c == '-' || (char.IsWhiteSpace(c) && c != '\n'))
					{
						++end;
					}
					else if (c != '\n')
					{
						cont = true;
						--end;
					}
				}
				lines.Add(description.Substring(start, end - start));
				if (cont)
				{
					lines[lines.Count - 1] += "-";
				}

				start = end;
				if (start < description.Length && description[start] == '\n')
				{
					++start;
				}
			} while (end < description.Length);

			return lines;
		}

		private static int GetNextOptionIndex(string[] names, int i)
		{
			while (i < names.Length && names[i] == "<>")
			{
				++i;
			}
			
			return i;
		}

		private static void Invoke(OptionContext c, string name, string value, Option option)
		{
			c.OptionName = name;
			c.Option = option;
			c.OptionValues.Add(value);
			option.Invoke(c);
		}

		private static bool Unprocessed(ICollection<string> extra, Option def, OptionContext c, string argument)
		{
			if (def == null)
			{
				extra.Add(argument);
				return false;
			}
			
			c.OptionValues.Add(argument);
			c.Option = def;
			c.Option.Invoke(c);
			return false;
		}

		private static void Write(TextWriter o, ref int n, string s)
		{
			n += s.Length;
			o.Write(s);
		}

		#endregion

		#region Nested type: ActionOption

		private sealed class ActionOption : Option
		{
			#region Readonly & Static Fields

			private readonly Action<OptionValueCollection> m_Action;

			#endregion

			#region Constructors

			/// <exception cref="ArgumentNullException"><paramref name="action" /> is <c>null</c>.</exception>
			public ActionOption(string prototype, string description, int count, Action<OptionValueCollection> action)
				: base(prototype, description, count)
			{
				if (action == null)
				{
					throw new ArgumentNullException("action");
				}

				m_Action = action;
			}

			#endregion

			#region Instance Methods

			protected override void OnParseComplete(OptionContext c)
			{
				m_Action(c.OptionValues);
			}

			#endregion
		}

		private sealed class ActionOption<T> : Option
		{
			#region Readonly & Static Fields

			private readonly Action<T> m_Action;

			#endregion

			#region Constructors

			/// <exception cref="ArgumentNullException"><paramref name="action" /> is <c>null</c>.</exception>
			public ActionOption(string prototype, string description, Action<T> action)
				: base(prototype, description, 1)
			{
				if (action == null)
				{
					throw new ArgumentNullException("action");
				}

				m_Action = action;
			}

			#endregion

			#region Instance Methods

			protected override void OnParseComplete(OptionContext c)
			{
				m_Action(Parse<T>(c.OptionValues[0], c));
			}

			#endregion
		}

		private sealed class ActionOption<TKey, TValue> : Option
		{
			#region Readonly & Static Fields

			private readonly OptionAction<TKey, TValue> m_Action;

			#endregion

			#region Constructors

			/// <exception cref="ArgumentNullException"><paramref name="action" /> is <c>null</c>.</exception>
			public ActionOption(string prototype, string description, OptionAction<TKey, TValue> action)
				: base(prototype, description, 2)
			{
				if (action == null)
				{
					throw new ArgumentNullException("action");
				}

				m_Action = action;
			}

			#endregion

			#region Instance Methods

			protected override void OnParseComplete(OptionContext c)
			{
				m_Action(Parse<TKey>(c.OptionValues[0], c), Parse<TValue>(c.OptionValues[1], c));
			}

			#endregion
		}

		#endregion
	}
}