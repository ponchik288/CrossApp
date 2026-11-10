using Core;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

Console.OutputEncoding = System.Text.Encoding.UTF8;

EnvironmentReport report = EnvironmentInfo.Collect();
bool jsonMode = args.Contains("--json");

if (jsonMode)
{
    var info = new
    {
        report.OsDescription,
        report.OsVersion,
        report.ProcessArchitecture,
        report.DotNetVersion,
        Runtime = report.FrameworkDescription,
        AppBaseDirectory = report.BaseDirectory,
        report.CurrentDirectory,
        Domain = "Склад (товари, партії, залишки, переміщення)",
        report.DetectedRid,
        report.ReportedRid,
        report.BuildNote
    };

    var options = new JsonSerializerOptions
    {
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    Console.WriteLine(JsonSerializer.Serialize(info, options));
}
else
{
    Console.WriteLine("CrossApp – практикум з крос-платформного програмування");
    Console.WriteLine("Студент: Кирик Денис Володимирович, група ФЕІ-35с");
    Console.WriteLine(new string('-', 52));

    Console.WriteLine($"ОС (OSDescription) : {report.OsDescription}");
    Console.WriteLine($"ОС (Environment) : {report.OsVersion}");
    Console.WriteLine($"Архітектура процесу : {report.ProcessArchitecture}");
    Console.WriteLine($"Версія .NET (CLR) : {report.DotNetVersion}");
    Console.WriteLine($"Runtime : {report.FrameworkDescription}");
    Console.WriteLine($"RID (визначено) : {report.DetectedRid}");
    Console.WriteLine($"RID (від .NET) : {report.ReportedRid}");
    Console.WriteLine($"Каталог застосунку : {report.BaseDirectory}");
    Console.WriteLine($"Поточний каталог : {report.CurrentDirectory}");
    Console.WriteLine($"Збірка Core : {report.BuildNote}");

    Console.WriteLine(new string('-', 52));
    Console.WriteLine("Предметна область: Склад (товари, партії, залишки, переміщення)");
}
