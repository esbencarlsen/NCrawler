//
// NConsoler 0.9.3
// http://nconsoler.csharpus.com
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace NConsoler
{
	/// <summary>
	/// Entry point for NConsoler applications
	/// </summary>
	public sealed class Consolery
	{
		#region Readonly & Static Fields

		private readonly List<MethodInfo> _actionMethods = new List<MethodInfo>();
		private readonly string[] _args;
		private readonly IMessenger _messenger;
		private readonly Type _targetType;

		#endregion

		#region Constructors

		private Consolery(Type targetType, string[] args, IMessenger messenger)
		{
			#region Parameter Validation

			if (targetType == null)
			{
				throw new ArgumentNullException("targetType");
			}
			if (args == null)
			{
				throw new ArgumentNullException("args");
			}
			if (messenger == null)
			{
				throw new ArgumentNullException("messenger");
			}

			#endregion

			_targetType = targetType;
			_args = args;
			_messenger = messenger;
			MethodInfo[] methods = _targetType.GetMethods(BindingFlags.Public | BindingFlags.Static);
			foreach (MethodInfo method in methods)
			{
				object[] attributes = method.GetCustomAttributes(false);
				if (attributes.OfType<ActionAttribute>().Any())
				{
					_actionMethods.Add(method);
				}
			}
		}

		#endregion

		#region Instance Properties

		private bool IsMulticommand => _actionMethods.Count > 1;

		#endregion

		#region Instance Methods

		private object[] BuildParameterArray(MethodInfo method)
		{
			int argumentIndex = IsMulticommand ? 1 : 0;
			List<object> parameterValues = new List<object>();
			Dictionary<string, ParameterData> aliases = new Dictionary<string, ParameterData>();
			foreach (ParameterInfo info in method.GetParameters())
			{
				if (IsRequired(info))
				{
					parameterValues.Add(ConvertValue(_args[argumentIndex], info.ParameterType));
				}
				else
				{
					OptionalAttribute optional = GetOptional(info);

					foreach (string altName in optional.AltNames)
					{
						aliases.Add(altName.ToLower(),
							new ParameterData(parameterValues.Count, info.ParameterType));
					}
					aliases.Add(info.Name.ToLower(),
						new ParameterData(parameterValues.Count, info.ParameterType));
					parameterValues.Add(optional.Default);
				}
				argumentIndex++;
			}
			foreach (string optionalParameter in OptionalParameters(method))
			{
				string name = ParameterName(optionalParameter);
				string value = ParameterValue(optionalParameter);
				parameterValues[aliases[name].Position] = ConvertValue(value, aliases[name].Type);
			}
			return parameterValues.ToArray();
		}

		private void CheckActionMethodNamesAreNotReserved()
		{
			foreach (MethodInfo method in _actionMethods)
			{
				if (method.Name.ToLower() == "help")
				{
					throw new NConsolerException("Method name \"{0}\" is reserved. Please, choose another name", method.Name);
				}
			}
		}

		private void CheckAllRequiredParametersAreSet(MethodInfo method)
		{
			int minimumArgsLengh = RequiredParameterCount(method);
			if (IsMulticommand)
			{
				minimumArgsLengh++;
			}
			if (_args.Length < minimumArgsLengh)
			{
				throw new NConsolerException("Not all required parameters are set");
			}
		}

		private void CheckAnyActionMethodExists()
		{
			if (_actionMethods.Count == 0)
			{
				throw new NConsolerException(
					"Can not find any public static method marked with [Action] attribute in type \"{0}\"", _targetType.Name);
			}
		}

		private void CheckOptionalParametersAreNotDuplicated(MethodInfo method)
		{
			List<string> passedParameters = new List<string>();
			foreach (string optionalParameter in OptionalParameters(method))
			{
				if (!optionalParameter.StartsWith("/"))
				{
					throw new NConsolerException("Unknown parameter {0}", optionalParameter);
				}
				string name = ParameterName(optionalParameter);
				if (passedParameters.Contains(name))
				{
					throw new NConsolerException("Parameter with name {0} passed two times", name);
				}
				passedParameters.Add(name);
			}
		}

		private void CheckUnknownParametersAreNotPassed(MethodInfo method)
		{
			List<string> parameterNames = new List<string>();
			foreach (ParameterInfo parameter in method.GetParameters())
			{
				if (IsRequired(parameter))
				{
					continue;
				}
				parameterNames.Add(parameter.Name.ToLower());
				OptionalAttribute optional = GetOptional(parameter);
				parameterNames.AddRange(optional.AltNames.Select(altName => altName.ToLower()));
			}
			foreach (string optionalParameter in OptionalParameters(method))
			{
				string name = ParameterName(optionalParameter);
				if (!parameterNames.Contains(name.ToLower()))
				{
					throw new NConsolerException("Unknown parameter name {0}", optionalParameter);
				}
			}
		}

		private MethodInfo GetCurrentMethod()
		{
			if (!IsMulticommand)
			{
				return _actionMethods[0];
			}
			return GetMethodByName(_args[0].ToLower());
		}

		private MethodInfo GetMethodByName(string name)
		{
			return _actionMethods.FirstOrDefault(method => method.Name.ToLower() == name);
		}

		private void IfActionMethodIsSingleCheckMethodHasParameters()
		{
			if (_actionMethods.Count == 1 && _actionMethods[0].GetParameters().Length == 0)
			{
				throw new NConsolerException(
					"[Action] attribute applied once to the method \"{0}\" without parameters. In this case NConsoler should not be used",
					_actionMethods[0].Name);
			}
		}

		private void InvokeMethod(MethodInfo method)
		{
			try
			{
				method.Invoke(null, BuildParameterArray(method));
			}
			catch (TargetInvocationException e)
			{
				if (e.InnerException != null)
				{
					throw new NConsolerException(e.InnerException.Message, e);
				}
				throw;
			}
		}

		private bool IsHelpRequested()
		{
			return (_args.Length == 0 && !SingleActionWithOnlyOptionalParametersSpecified())
				|| (_args.Length > 0 && (_args[0] == "/?"
				|| _args[0] == "/help"
				|| _args[0] == "/h"
				|| _args[0] == "help"));
		}

		private bool IsSubcommandHelpRequested()
		{
			return _args.Length > 0
				&& _args[0].ToLower() == "help"
					&& _args.Length == 2;
		}

		private IEnumerable<string> OptionalParameters(MethodInfo method)
		{
			int firstOptionalParameterIndex = RequiredParameterCount(method);
			if (IsMulticommand)
			{
				firstOptionalParameterIndex++;
			}
			for (int i = firstOptionalParameterIndex; i < _args.Length; i++)
			{
				yield return _args[i];
			}
		}

		private void PrintGeneralMulticommandUsage()
		{
			_messenger.Write(
				String.Format("usage: {0} <subcommand> [args]", ProgramName()));
			_messenger.Write(
				String.Format("Type '{0} help <subcommand>' for help on a specific subcommand.", ProgramName()));
			_messenger.Write(String.Empty);
			_messenger.Write("Available subcommands:");
			foreach (MethodInfo method in _actionMethods)
			{
				_messenger.Write(method.Name.ToLower() + " " + GetMethodDescription(method));
			}
		}

		private void PrintMethodDescription(MethodInfo method)
		{
			string description = GetMethodDescription(method);
			if (description == String.Empty) return;
			_messenger.Write(description);
		}

		private void PrintParametersDescriptions(IEnumerable<KeyValuePair<string, string>> parameters)
		{
			int maxParameterNameLength = MaxKeyLength(parameters);
			foreach (KeyValuePair<string, string> pair in parameters)
			{
				if (pair.Value != String.Empty)
				{
					int difference = maxParameterNameLength - pair.Key.Length + 2;
					_messenger.Write("    " + pair.Key + new String(' ', difference) + pair.Value);
				}
			}
		}

		private void PrintSubcommandUsage()
		{
			MethodInfo method = GetMethodByName(_args[1].ToLower());
			if (method == null)
			{
				PrintGeneralMulticommandUsage();
				throw new NConsolerException("Unknown subcommand \"{0}\"", _args[0].ToLower());
			}
			PrintUsage(method);
		}

		private void PrintUsage(MethodInfo method)
		{
			PrintMethodDescription(method);
			Dictionary<string, string> parameters = GetParametersDescriptions(method);
			PrintUsageExample(method, parameters);
			PrintParametersDescriptions(parameters);
		}

		private void PrintUsage()
		{
			if (IsMulticommand && !IsSubcommandHelpRequested())
			{
				PrintGeneralMulticommandUsage();
			}
			else if (IsMulticommand && IsSubcommandHelpRequested())
			{
				PrintSubcommandUsage();
			}
			else
			{
				PrintUsage(_actionMethods[0]);
			}
		}

		private void PrintUsageExample(MethodInfo method, IDictionary<string, string> parameterList)
		{
			string subcommand = IsMulticommand ? method.Name.ToLower() + " " : String.Empty;
			string parameters = String.Join(" ", new List<string>(parameterList.Keys).ToArray());
			_messenger.Write("usage: " + ProgramName() + " " + subcommand + parameters);
		}

		private string ProgramName()
		{
			Assembly entryAssembly = Assembly.GetEntryAssembly();
			if (entryAssembly == null)
			{
				return _targetType.Name.ToLower();
			}
			return new AssemblyName(entryAssembly.FullName).Name;
		}

		private void RunAction()
		{
			ValidateMetadata();
			if (IsHelpRequested())
			{
				PrintUsage();
				return;
			}

			MethodInfo currentMethod = GetCurrentMethod();
			if (currentMethod == null)
			{
				PrintUsage();
				throw new NConsolerException("Unknown subcommand \"{0}\"", _args[0]);
			}

			ValidateInput(currentMethod);
			InvokeMethod(currentMethod);
		}

		private bool SingleActionWithOnlyOptionalParametersSpecified()
		{
			if (IsMulticommand)
			{
				return false;
			}

			MethodInfo method = _actionMethods[0];
			return OnlyOptionalParametersSpecified(method);
		}

		private void ValidateInput(MethodInfo method)
		{
			CheckAllRequiredParametersAreSet(method);
			CheckOptionalParametersAreNotDuplicated(method);
			CheckUnknownParametersAreNotPassed(method);
		}

		private void ValidateMetadata()
		{
			CheckAnyActionMethodExists();
			IfActionMethodIsSingleCheckMethodHasParameters();
			foreach (MethodInfo method in _actionMethods)
			{
				CheckActionMethodNamesAreNotReserved();
				CheckRequiredAndOptionalAreNotAppliedAtTheSameTime(method);
				CheckOptionalParametersAreAfterRequiredOnes(method);
				CheckOptionalParametersDefaultValuesAreAssignableToRealParameterTypes(method);
				CheckOptionalParametersAltNamesAreNotDuplicated(method);
			}
		}

		#endregion

		#region Class Methods

		/// <summary>
		/// Runs an appropriate Action method.
		/// Uses the class this call lives in as target type and command line arguments from Environment
		/// </summary>
		public static void Run()
		{
			Type declaringType = new StackTrace().GetFrame(1).GetMethod().DeclaringType;
			string[] args = new string[Environment.GetCommandLineArgs().Length - 1];
			new List<string>(Environment.GetCommandLineArgs()).CopyTo(1, args, 0, Environment.GetCommandLineArgs().Length - 1);
			Run(declaringType, args);
		}

		/// <summary>
		/// Runs an appropriate Action method
		/// </summary>
		/// <param name="targetType">Type where to search for Action methods</param>
		/// <param name="args">Arguments that will be converted to Action method arguments</param>
		public static void Run(Type targetType, string[] args)
		{
			Run(targetType, args, new ConsoleMessenger());
		}

		/// <summary>
		/// Runs an appropriate Action method
		/// </summary>
		/// <param name="targetType">Type where to search for Action methods</param>
		/// <param name="args">Arguments that will be converted to Action method arguments</param>
		/// <param name="messenger">Uses for writing messages instead of Console class methods</param>
		public static void Run(Type targetType, string[] args, IMessenger messenger)
		{
			try
			{
				new Consolery(targetType, args, messenger).RunAction();
			}
			catch (NConsolerException e)
			{
				messenger.Write(e.Message);
			}
		}

		/// <summary>
		/// Validates specified type and throws NConsolerException if an error
		/// </summary>
		/// <param name="targetType">Type where to search for Action methods</param>
		public static void Validate(Type targetType)
		{
			new Consolery(targetType, new string[] {}, new ConsoleMessenger()).ValidateMetadata();
		}

		private static bool CanBeConvertedToDate(string parameter)
		{
			try
			{
				ConvertToDateTime(parameter);
				return true;
			}
			catch (NConsolerException)
			{
				return false;
			}
		}

		private static bool CanBeNull(Type type)
		{
			return type == typeof (string)
				|| type == typeof (string[])
				|| type == typeof (int[]);
		}

		private static void CheckOptionalParametersAltNamesAreNotDuplicated(MethodBase method)
		{
			List<string> parameterNames = new List<string>();
			foreach (ParameterInfo parameter in method.GetParameters())
			{
				if (IsRequired(parameter))
				{
					parameterNames.Add(parameter.Name.ToLower());
				}
				else
				{
					if (parameterNames.Contains(parameter.Name.ToLower()))
					{
						throw new NConsolerException(
							"Found duplicated parameter name \"{0}\" in method \"{1}\". Please check alt names for optional parameters",
							parameter.Name, method.Name);
					}

					parameterNames.Add(parameter.Name.ToLower());
					OptionalAttribute optional = GetOptional(parameter);
					foreach (string altName in optional.AltNames)
					{
						if (parameterNames.Contains(altName.ToLower()))
						{
							throw new NConsolerException(
								"Found duplicated parameter name \"{0}\" in method \"{1}\". Please check alt names for optional parameters",
								altName, method.Name);
						}

						parameterNames.Add(altName.ToLower());
					}
				}
			}
		}

		private static void CheckOptionalParametersAreAfterRequiredOnes(MethodBase method)
		{
			bool optionalFound = false;
			foreach (ParameterInfo parameter in method.GetParameters())
			{
				if (IsOptional(parameter))
				{
					optionalFound = true;
				}
				else if (optionalFound)
				{
					throw new NConsolerException(
						"It is not allowed to write a parameter with a Required attribute after a parameter with an Optional one. See method \"{0}\" parameter \"{1}\"",
						method.Name, parameter.Name);
				}
			}
		}

		private static void CheckOptionalParametersDefaultValuesAreAssignableToRealParameterTypes(MethodBase method)
		{
			foreach (ParameterInfo parameter in method.GetParameters())
			{
				if (IsRequired(parameter))
				{
					continue;
				}
				
				OptionalAttribute optional = GetOptional(parameter);
				if (optional.Default != null && optional.Default.GetType() == typeof (string) &&
					CanBeConvertedToDate(optional.Default.ToString()))
				{
					return;
				}
				
				if ((optional.Default == null && !CanBeNull(parameter.ParameterType))
					|| (optional.Default != null && !optional.Default.GetType().IsAssignableFrom(parameter.ParameterType)))
				{
					throw new NConsolerException(
						"Default value for an optional parameter \"{0}\" in method \"{1}\" can not be assigned to the parameter",
						parameter.Name, method.Name);
				}
			}
		}

		private static void CheckRequiredAndOptionalAreNotAppliedAtTheSameTime(MethodBase method)
		{
			foreach (ParameterInfo parameter in method.GetParameters())
			{
				object[] attributes = parameter.GetCustomAttributes(typeof (ParameterAttribute), false);
				if (attributes.Length > 1)
				{
					throw new NConsolerException("More than one attribute is applied to the parameter \"{0}\" in the method \"{1}\"",
						parameter.Name, method.Name);
				}
			}
		}

		private static DateTime ConvertToDateTime(string parameter)
		{
			string[] parts = parameter.Split('-');
			if (parts.Length != 3)
			{
				throw new NConsolerException("Could not convert {0} to Date", parameter);
			}
			
			int day = (int) ConvertValue(parts[0], typeof (int));
			int month = (int) ConvertValue(parts[1], typeof (int));
			int year = (int) ConvertValue(parts[2], typeof (int));
			try
			{
				return new DateTime(year, month, day);
			}
			catch (ArgumentException)
			{
				throw new NConsolerException("Could not convert {0} to Date", parameter);
			}
		}

		private static object ConvertValue(string value, Type argumentType)
		{
			if (argumentType == typeof (int))
			{
				try
				{
					return Convert.ToInt32(value);
				}
				catch (FormatException)
				{
					throw new NConsolerException("Could not convert \"{0}\" to integer", value);
				}
				catch (OverflowException)
				{
					throw new NConsolerException("Value \"{0}\" is too big or too small", value);
				}
			}
			
			if (argumentType == typeof (string))
			{
				return value;
			}
			
			if (argumentType == typeof (bool))
			{
				try
				{
					return Convert.ToBoolean(value);
				}
				catch (FormatException)
				{
					throw new NConsolerException("Could not convert \"{0}\" to boolean", value);
				}
			}
			
			if (argumentType == typeof (string[]))
			{
				return value.Split('+');
			}
			
			if (argumentType == typeof (int[]))
			{
				string[] values = value.Split('+');
				int[] valuesArray = new int[values.Length];
				for (int i = 0; i < values.Length; i++)
				{
					valuesArray[i] = (int) ConvertValue(values[i], typeof (int));
				}
				return valuesArray;
			}
			
			if (argumentType == typeof (DateTime))
			{
				return ConvertToDateTime(value);
			}
			
			throw new NConsolerException("Unknown type is used in your method {0}", argumentType.FullName);
		}

		private static string GetDisplayName(ParameterInfo parameter)
		{
			if (IsRequired(parameter))
			{
				return parameter.Name;
			}
			
			OptionalAttribute optional = GetOptional(parameter);
			string parameterName =
				(optional.AltNames.Length > 0) ? optional.AltNames[0] : parameter.Name;
			
			if (parameter.ParameterType != typeof (bool))
			{
				parameterName += ":" + ValueDescription(parameter.ParameterType);
			}
			
			return "[/" + parameterName + "]";
		}

		private static string GetMethodDescription(MethodInfo method)
		{
			object[] attributes = method.GetCustomAttributes(true);
			foreach (object attribute in attributes)
			{
				if (attribute is ActionAttribute)
				{
					return ((ActionAttribute) attribute).Description;
				}
			}
			
			throw new NConsolerException("Method is not marked with an Action attribute");
		}

		private static OptionalAttribute GetOptional(ICustomAttributeProvider info)
		{
			object[] attributes = info.GetCustomAttributes(typeof (OptionalAttribute), false);
			return (OptionalAttribute) attributes[0];
		}

		private static Dictionary<string, string> GetParametersDescriptions(MethodInfo method)
		{
			Dictionary<string, string> parameters = new Dictionary<string, string>();
			foreach (ParameterInfo parameter in method.GetParameters())
			{
				object[] parameterAttributes =
					parameter.GetCustomAttributes(typeof (ParameterAttribute), false);
				if (parameterAttributes.Length > 0)
				{
					string name = GetDisplayName(parameter);
					ParameterAttribute attribute = (ParameterAttribute) parameterAttributes[0];
					parameters.Add(name, attribute.Description);
				}
				else
				{
					parameters.Add(parameter.Name, String.Empty);
				}
			}
			
			return parameters;
		}

		private static bool IsOptional(ICustomAttributeProvider info)
		{
			return !IsRequired(info);
		}

		private static bool IsRequired(ICustomAttributeProvider info)
		{
			object[] attributes = info.GetCustomAttributes(typeof (ParameterAttribute), false);
			return attributes.Length == 0 || attributes[0].GetType() == typeof (RequiredAttribute);
		}

		private static int MaxKeyLength(IEnumerable<KeyValuePair<string, string>> parameters)
		{
			int maxLength = 0;
			foreach (KeyValuePair<string, string> pair in parameters)
			{
				if (pair.Key.Length > maxLength)
				{
					maxLength = pair.Key.Length;
				}
			}
			
			return maxLength;
		}

		private static bool OnlyOptionalParametersSpecified(MethodBase method)
		{
			return method.GetParameters().All(parameter => !IsRequired(parameter));
		}

		private static string ParameterName(string parameter)
		{
			if (parameter.StartsWith("/-"))
			{
				return parameter.Substring(2).ToLower();
			}
			
			if (parameter.Contains(":"))
			{
				return parameter.Substring(1, parameter.IndexOf(":") - 1).ToLower();
			}
			
			return parameter.Substring(1).ToLower();
		}

		private static string ParameterValue(string parameter)
		{
			if (parameter.StartsWith("/-"))
			{
				return "false";
			}
			
			if (parameter.Contains(":"))
			{
				return parameter.Substring(parameter.IndexOf(":") + 1);
			}
			
			return "true";
		}

		private static int RequiredParameterCount(MethodInfo method)
		{
			return method.GetParameters().Count(IsRequired);
		}

		private static string ValueDescription(Type type)
		{
			if (type == typeof (int))
			{
				return "number";
			}
			
			if (type == typeof (string))
			{
				return "value";
			}
			
			if (type == typeof (int[]))
			{
				return "number[+number]";
			}
			
			if (type == typeof (string[]))
			{
				return "value[+value]";
			}
			
			if (type == typeof (DateTime))
			{
				return "dd-mm-yyyy";
			}
			
			throw new ArgumentOutOfRangeException(String.Format("Type {0} is unknown", type.Name));
		}

		#endregion

		#region Nested type: ParameterData

		private struct ParameterData
		{
			#region Readonly & Static Fields

			public readonly int Position;
			public readonly Type Type;

			#endregion

			#region Constructors

			public ParameterData(int position, Type type)
			{
				Position = position;
				Type = type;
			}

			#endregion
		}

		#endregion
	}

	/// <summary>
	/// Used for getting messages from NConsoler
	/// </summary>
	public interface IMessenger
	{
		#region Instance Methods

		void Write(string message);

		#endregion
	}

	/// <summary>
	/// Uses Console class for message output
	/// </summary>
	public class ConsoleMessenger : IMessenger
	{
		#region IMessenger Members

		public void Write(string message)
		{
			Console.WriteLine(message);
		}

		#endregion
	}

	/// <summary>
	/// Every action method should be marked with this attribute
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
	public sealed class ActionAttribute : Attribute
	{
		#region Fields

		private string _description = String.Empty;

		#endregion

		#region Constructors

		public ActionAttribute()
		{
		}

		public ActionAttribute(string description)
		{
			_description = description;
		}

		#endregion

		#region Instance Properties

		/// <summary>
		/// Description is used for help messages
		/// </summary>
		public string Description
		{
			get { return _description; }

			set { _description = value; }
		}

		#endregion
	}

	/// <summary>
	/// Should not be used directly
	/// </summary>
	[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
	public class ParameterAttribute : Attribute
	{
		#region Fields

		private string _description = String.Empty;

		#endregion

		#region Constructors

		protected ParameterAttribute()
		{
		}

		#endregion

		#region Instance Properties

		/// <summary>
		/// Description is used in help message
		/// </summary>
		public string Description
		{
			get { return _description; }

			set { _description = value; }
		}

		#endregion
	}

	/// <summary>
	/// Marks an Action method parameter as optional
	/// </summary>
	public sealed class OptionalAttribute : ParameterAttribute
	{
		#region Constructors

		/// <param name="defaultValue">Default value if client doesn't pass this value</param>
		public OptionalAttribute(object defaultValue)
		{
			Default = defaultValue;
			AltNames = new string[0];
		}

		#endregion

		#region Instance Properties

		public string[] AltNames { get; set; }
		public object Default { get; private set; }

		#endregion
	}

	/// <summary>
	/// Marks an Action method parameter as required
	/// </summary>
	public sealed class RequiredAttribute : ParameterAttribute
	{
	}

	/// <summary>
	/// Can be used for safe exception throwing - NConsoler will catch the exception
	/// </summary>
	public sealed class NConsolerException : Exception
	{
		#region Constructors

		public NConsolerException()
		{
		}

		public NConsolerException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		public NConsolerException(string message, params string[] arguments)
			: base(String.Format(message, arguments))
		{
		}

		#endregion
	}
}