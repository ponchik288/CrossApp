# CrossApp

Наскрізний проєкт з крос-платформного програмування.

Предметна область: Склад (товари, партії, залишки, переміщення).

Сутності: Product, StockBatch, Warehouse, Movement.

Призначення: облік залишків товарів по партіях.

Середовище: Windows 10 x64, .NET SDK 10.0.401.

## Структура solution

```text
CrossApp/
  CrossApp.sln
  README.md
  .gitignore
  src/
    Core/                    бібліотека класів (спільна логіка)
      Core.csproj
      EnvironmentReport.cs   record з даними про середовище
      EnvironmentInfo.cs     збирає ці дані
    Cli/                     консольний застосунок
      Cli.csproj             містить ProjectReference на Core
      Program.cs             викликає Core і друкує результат
  scripts/
    Show-CoreTargets.ps1     показує Core, зібраний під net8.0 і net10.0
```

Залежність одностороння: `Cli → Core`. Core про Cli нічого не знає, тому пізніше
його зможуть використовувати також Api та Blazor-клієнт.

Домовленість про каталоги в Core на семестр (створюються разом із першим типом):

- `Core/Dto/` – record-типи формату даних (ЛР3)
- `Core/Domain/` – сутності з поведінкою та інваріантами (ЛР4)
- `Core/Storage/` – реалізації сховищ (ЛР5)

## Збірка і запуск

```powershell
dotnet build
dotnet run --project src/Cli
dotnet run --project src/Cli -- --json
```

Параметр `--json` виводить ту саму інформацію одним JSON-рядком (System.Text.Json).

Приклад виводу:

```text
CrossApp – практикум з крос-платформного програмування
Студент: Кирик Денис Володимирович, група ФЕІ-35с
----------------------------------------------------
ОС (OSDescription) : Microsoft Windows 10.0.19045
ОС (Environment) : Microsoft Windows NT 10.0.19045.0
Архітектура процесу : X64
Версія .NET (CLR) : 10.0.12
Runtime : .NET 10.0.12
RID (визначено) : win-x64
RID (від .NET) : win-x64
Каталог застосунку : C:\Users\admin\Desktop\KPP\CrossApp\src\Cli\bin\Debug\net10.0\
Поточний каталог : C:\Users\admin\Desktop\KPP\CrossApp
Збірка Core : збірка під net10.0
----------------------------------------------------
Предметна область: Склад (товари, партії, залишки, переміщення)
```

## ЛР1. Середовище і старт проєкту

Створено solution, консольний проєкт Cli, `.gitignore`, репозиторій на GitHub.

Додаткове завдання: режим `--json` і self-contained публікація для двох RID:

- win-x64 – 76,84 MB
- win-arm64 – 86,34 MB

## ЛР2. Core + Cli, multi-targeting, публікація

Збирання інформації про середовище перенесено з `Program.cs` у бібліотеку Core.
`EnvironmentInfo.Collect()` повертає `EnvironmentReport` і нічого не друкує.
У `Program.cs` залишився тільки виклик Core і вивід.

Команди, якими додано бібліотеку і посилання:

```powershell
dotnet new classlib -n Core -o src/Core -f net10.0
dotnet sln add src/Core/Core.csproj
dotnet add src/Cli/Cli.csproj reference src/Core/Core.csproj
```

### Multi-targeting

У `Core.csproj` вказано `<TargetFrameworks>net8.0;net10.0</TargetFrameworks>`,
тому `dotnet build` створює дві бібліотеки: `src/Core/bin/Debug/net8.0/Core.dll`
і `src/Core/bin/Debug/net10.0/Core.dll`. Cli збирається тільки під `net10.0`.

У `EnvironmentInfo` є рядок, який залежить від TFM (`#if NET10_0_OR_GREATER`):
«збірка під net10.0» або «збірка під net8.0». Обидва варіанти показує скрипт:

```powershell
.\scripts\Show-CoreTargets.ps1
```

### Публікація

```powershell
dotnet publish src/Cli -c Release -f net10.0 -r win-x64 --self-contained true -o src/Cli/bin/Release/net10.0/win-x64/lab02/self-contained
dotnet publish src/Cli -c Release -f net10.0 -r win-x64 --self-contained false -o src/Cli/bin/Release/net10.0/win-x64/lab02/framework-dependent
```

Запуск із каталогу publish, без `dotnet run`:

```powershell
cd src/Cli/bin/Release/net10.0/win-x64/lab02/self-contained
.\Cli.exe
```

Вивід такий самий, як при `dotnet run`; відрізняються тільки рядки з каталогами.

| RID | Режим | Розмір publish | Файлів | Потрібен встановлений runtime |
|---|---|---:|---:|---|
| win-x64 | self-contained | 76,86 МБ | 194 | ні |
| win-x64 | framework-dependent | 0,20 МБ | 7 | так (.NET 10) |
| win-x64 | self-contained + single-file | 70,16 МБ | 3 | ні |
| win-x64 | self-contained + trimmed | 19,55 МБ | 35 | ні |

Розмір виміряно командою
`(Get-ChildItem -Recurse <шлях publish> | Measure-Object Length -Sum).Sum/1MB`.

**Self-contained** – у каталозі publish лежить наш код і копія .NET Runtime.
Застосунок працює на комп'ютері без встановленого .NET, але каталог великий
і зібраний під одну RID (win-x64).

**Framework-dependent** – у каталозі тільки наш код. Каталог малий, але на
комп'ютері користувача має бути встановлений .NET 10.

### Додаткові завдання

**Single-file** (`-p:PublishSingleFile=true`): замість 194 файлів – 3
(`Cli.exe` і два файли символів `.pdb`). Розмір майже не змінився, бо runtime
нікуди не зник – його запаковано всередину `Cli.exe`. Застосунок запускається.

**Trimming** (`-p:PublishTrimmed=true`): розмір зменшився з 76,86 до 19,55 МБ,
але збірка видала попередження `IL2026` для `JsonSerializer.Serialize`.
Звичайний вивід працює, а `Cli.exe --json` завершується винятком
`InvalidOperationException: Reflection-based serialization has been disabled`.
Trimmer видаляє код, на який немає прямих посилань, а серіалізатор JSON шукає
властивості типу через рефлексію вже під час виконання. Тому trimming
небезпечний для коду з рефлексією, і в основній публікації його не використано.

**Умовна компіляція** – описано вище в розділі Multi-targeting.
