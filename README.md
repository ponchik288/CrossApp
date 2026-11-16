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
  data/                      тестові дані (sample.csv, sample.json, mixed.csv)
  src/
    Core/                    бібліотека класів (спільна логіка)
      Core.csproj
      EnvironmentReport.cs   record з даними про середовище
      EnvironmentInfo.cs     збирає ці дані
      Dto/                   record-типи формату даних (ЛР3)
      Import/                імпортери CSV і JSON (ЛР3)
    Cli/                     консольний застосунок
      Cli.csproj             містить ProjectReference на Core
      Program.cs             аргументи, виклик Core, вивід
      EnvironmentPrinter.cs  вивід інформації про середовище
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

Усі команди виконуються з кореня репозиторію.

```powershell
dotnet build
dotnet run --project src/Cli                             # імпорт data/sample.csv
dotnet run --project src/Cli -- data/sample.json         # імпорт JSON
dotnet run --project src/Cli -- data/mixed.csv --mixed   # товари і склади в одному файлі
dotnet run --project src/Cli -- env                      # інформація про середовище
dotnet run --project src/Cli -- env --json               # те саме одним JSON-рядком
```

## ЛР1. Середовище і старт проєкту

Створено solution, консольний проєкт Cli, `.gitignore`, репозиторій на GitHub.

Програма виводить ОС, архітектуру, версію .NET і каталоги (тепер це команда `env`).

Додаткове завдання: режим `--json` (тепер `env --json`) і self-contained публікація для двох RID:

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
(Збірки в каталозі `lab02` зроблені на момент ЛР2. Після ЛР3 інформацію про середовище
виводить команда `Cli.exe env`.)

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

## ЛР3. Records, pattern matching, імпорт CSV/JSON

У Core з'явилися перші дані предметної області «Склад» і імпорт із файлу.
Пошкоджені рядки не зупиняють імпорт: програма повертає і прочитані записи, і перелік помилок.

### Типи (Core/Dto)

| Тип | Поля | Nullable |
|---|---|---|
| `ProductDto` | Id, Sku, Name, Unit, Quantity, Note | лише `Note` – примітки може не бути |
| `WarehouseDto` | Id, Name, Address | лише `Address` – адреса може бути невідома |
| `ImportResult<T>` | Items, Errors | немає: порожній список замість null |
| `MixedImportResult` | Products, Warehouses, Errors | немає |

Ідентифікатор – рядок (`"P-001"`, `"W-01"`).

### Формат файлу data/sample.csv

- роздільник – крапка з комою `;` (не конфліктує з комою в назвах);
- перший рядок – заголовок `id;sku;name;unit;quantity` (файл без заголовка теж читається);
- кодування UTF-8;
- порожні рядки і рядки, що починаються з `#`, пропускаються;
- кількість – ціле невід'ємне число, розбирається з `CultureInfo.InvariantCulture`.

У файлі 10 коректних рядків і 3 навмисно пошкоджені (мало колонок, нечислова кількість,
порожній SKU) – це тестові дані для перевірки імпорту.

### Розбір рядка

`ProductCsvImporter.ParseLine` – один switch expression із патернами: властивостей
`{ Length: < 5 }`, списків `[_, "", _, _, _]`, константним `""`, логічним `or`,
охоронною умовою `when` і патерном списку з іменами `[var id, var sku, ...]`.
У `Program.cs` немає ні `Split`, ні `int.Parse` – увесь розбір у Core.

### Вивід

```text
Завантажено записів: 10
  P-001  SKU-001    Цемент М400 25кг                   120 шт
  P-002  SKU-002    Пісок будівельний                   18 т
  P-003  SKU-003    Цегла червона                     4200 шт
  P-004  SKU-004    Фарба водоемульсійна 10л            36 шт
  P-005  SKU-005    Шпаклівка фінішна                  250 кг
Пропущено рядків: 3
  ! рядок 12: очікую 5 колонок, отримав 4
  ! рядок 13: кількість 'багато' не є невід'ємним числом
  ! рядок 14: SKU або назва порожні
Статистика: усього 13 / прийнято 10 / пропущено 3 / помилок 23,1%
```

Неіснуючий файл: повідомлення `Файл не знайдено: <повний шлях>` і код виходу 1.
Невідоме розширення: код виходу 2.

### Додаткові завдання

1. **JSON-імпортер** `ProductJsonImporter` на тих самих типах; Cli обирає імпортер за
   розширенням файлу через switch expression.
2. **Змішаний файл** `data/mixed.csv`: рядки `P;...` – товари, `W;...` – склади;
   один switch у `MixedCsvImporter` повертає два різні типи результату.
3. **Статистика імпорту** одним рядком: усього / прийнято / пропущено / % помилок.
