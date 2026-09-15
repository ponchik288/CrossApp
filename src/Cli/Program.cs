using System.Runtime.InteropServices;
using System.Text.Json;

Console.OutputEncoding = System.Text.Encoding.UTF8;

bool jsonMode = args.Contains("--json");

if (jsonMode)
{
    var info = new
    {
        OsDescription = RuntimeInformation.OSDescription,
        OsVersion = Environment.OSVersion.ToString(),
        ProcessArchitecture = RuntimeInformation.ProcessArchitecture.ToString(),
        DotNetVersion = Environment.Version.ToString(),
        Runtime = RuntimeInformation.FrameworkDescription,
        AppBaseDirectory = AppContext.BaseDirectory,
        CurrentDirectory = Environment.CurrentDirectory,
        Domain = "Склад (товари, партії, залишки, переміщення)"
    };

    Console.WriteLine(JsonSerializer.Serialize(info));
}
else
{
    Console.WriteLine("CrossApp – практикум з крос-платформного програмування");
    Console.WriteLine("Студент: Кирик Денис Володимирович, група ФЕІ-35с");
    Console.WriteLine(new string('-', 52));

    Console.WriteLine($"ОС (OSDescription) : {RuntimeInformation.OSDescription}");
    Console.WriteLine($"ОС (Environment) : {Environment.OSVersion}");
    Console.WriteLine($"Архітектура процесу : {RuntimeInformation.ProcessArchitecture}");
    Console.WriteLine($"Версія .NET (CLR) : {Environment.Version}");
    Console.WriteLine($"Runtime : {RuntimeInformation.FrameworkDescription}");
    Console.WriteLine($"Каталог застосунку : {AppContext.BaseDirectory}");
    Console.WriteLine($"Поточний каталог : {Environment.CurrentDirectory}");

    Console.WriteLine(new string('-', 52));
    Console.WriteLine("Предметна область: Склад (товари, партії, залишки, переміщення)");
}