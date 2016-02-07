#r "System.Xml"
#r "System.Xml.Linq"
#r "System.IO.Compression"
#r "System.IO.Compression.FileSystem"

using Nake.FS;
using Nake.Run;
using Nake.Log;
using Nake.Env;
using Nake.App;

using System.Linq;
using System.Net;
using System.Xml.Linq;
using System.Diagnostics;
using System.IO.Compression;

const string CoreProject = "NCrawler";

const string RootPath = "$NakeScriptDirectory$";
const string OutputPath = RootPath + @"\Output";

var PackagePath = @"{OutputPath}\Package";
var ReleasePath = @"{PackagePath}\Release";

var AppVeyor = Var["APPVEYOR"] == "False";

/// Installs dependencies and builds sources in Debug mode
[Task]
void Default()
{
	Install();
	Build();
}

/// Wipeout all build output and temporary build files
[Step]
void Clean(string path = OutputPath)
{
	try
	{
		Delete(@"{path}\*.*|-:*.vshost.exe");
		RemoveDir(@"**\bin|**\obj|{path}\*|-:*.vshost.exe");
	}
	catch
	{
	}
}

/// Builds sources using specified configuration and output path
[Step]
void Build(string config = "Debug", string outDir = OutputPath)
{
	Delete(@"{outDir}\..\*.log");
	Clean(outDir);

	Exec(@"$ProgramFiles(x86)$\MSBuild\14.0\Bin\MSBuild.exe",
		"{CoreProject}.sln /p:Configuration={config}");
}

/// Runs unit tests 
[Step]
void Test(string outDir = OutputPath, bool slow = false)
{
	//Build("Debug", outDir);

	var tests = new FileSet { @"**\bin\*\*.Tests.dll" }.ToString(" ");
	tests = new FileSet { @"**\bin\*\Esc.NetMq.Tests.dll" }.ToString(" ");
	var results = @"{outDir}\nunit-test-results.xml";

	try
	{
		Cmd(@"packages\NUnit.Console.3.0.1\tools\nunit3-console.exe " +
			@"--noresult --framework=net-4.5 {tests} --verbose" +
			(AppVeyor || slow ? "/include:Always,Slow" : ""));
	}
	finally
	{
		if (AppVeyor)
			new WebClient().UploadFile("https://ci.appveyor.com/api/testresults/nunit/$APPVEYOR_JOB_ID$", results);
	}
}

/// Builds official NuGet packages 
[Step]
void Package()
{
	Test(@"{PackagePath}\Debug");
	Build("Package", ReleasePath);

	Pack(CoreProject);
}

void Pack(string project, string properties = null)
{
	Cmd(@"Tools\Nuget.exe pack Build\{project}.nuspec -Version {Version(project)} " +
		 "-OutputDirectory {PackagePath} -BasePath {RootPath} -NoPackageAnalysis " +
		 (properties != null ? "-Properties {properties}" : ""));
}

/// Publishes package to NuGet gallery
[Step]
void Publish(string project)
{
	switch (project)
	{
		case "core":
			Push(CoreProject);
			break;
		default:
			throw new ArgumentException("Available values are: core");
	}
}

void Push(string project)
{
	Cmd(@"Tools\Nuget.exe push {PackagePath}\{project}.{Version(project)}.nupkg $NuGetApiKey$");
}

string Version(string project)
{
	return FileVersionInfo
			.GetVersionInfo(@"{ReleasePath}\{project}.dll")
			.FileVersion;
}

/// Installs dependencies (packages) 
[Task]
void Install()
{
	InstallPackages();
}

void InstallPackages()
{
	Cmd(@"Tools\NuGet.exe restore {CoreProject}.sln -PackagesDirectory .\Packages");

	//Cmd(@"Tools\NuGet.exe restore {CoreProject}.sln");
	Cmd(@"Tools\NuGet.exe install Build/Packages.config -o {RootPath}\Packages");
}
