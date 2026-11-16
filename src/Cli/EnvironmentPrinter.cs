using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Core;

namespace Cli;

// Вивід інформації про середовище (ЛР1–ЛР2). Дані збирає Core, тут лише друк.
internal static class EnvironmentPrinter
{
    private const string Domain = "Склад (товари, партії, залишки, переміщення)";

    public static void Print(bool jsonMode)
    {
        EnvironmentReport report = EnvironmentInfo.Collect();

        if (jsonMode)
            PrintJson(report);
        else
            PrintText(report);
    }

    private static void PrintJson(EnvironmentReport report)
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
            Domain,
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

    private static void PrintText(EnvironmentReport report)
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
        Console.WriteLine($"Предметна область: {Domain}");
    }
}
