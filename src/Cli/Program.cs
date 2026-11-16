using Cli;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// Перший аргумент — команда. Якщо це не команда, вважаємо його шляхом до файлу для імпорту.
string command = args.Length > 0 ? args[0] : "import";

return command switch
{
    "env" => EnvironmentPrinter.Print(args.Contains("--json")),
    "demo" => DomainDemo.Run(),
    "import" => ImportCommand.Run(args.Skip(1).ToArray()),
    _ => ImportCommand.Run(args)
};
