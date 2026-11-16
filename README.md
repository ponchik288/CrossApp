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
  data/                      зразкові вхідні файли (sample.csv, import.json тощо)
  src/
    Core/                    бібліотека класів (спільна логіка)
      Core.csproj
      EnvironmentReport.cs   record з даними про середовище
      EnvironmentInfo.cs     збирає ці дані
      Dto/                   record-типи формату даних (ЛР3)
      Import/                імпортери CSV і JSON (ЛР3)
      Domain/                сутності з інваріантами (ЛР4)
      Abstractions/          інтерфейс сховища ICatalogStore (ЛР5)
      Storage/               сховища: у пам'яті, файлове, кешуюче (ЛР5)
      Services/              CatalogService – бізнес-операції (ЛР5)
      Events/                дані події зміни каталогу (ЛР6)
      Reports/               LINQ-звіти і їхні DTO (ЛР7)
      SampleData.cs          зразкові дані (18 товарів)
      DomainException.cs     виняток бізнес-правил (ЛР8)
      Configuration/         AppOptions – налаштування застосунку (ЛР9)
    Infrastructure/          робота з базою даних (EF Core + SQLite, ЛР11)
      Persistence/           ProductRow, BatchRow, AppDbContext
      EfCatalogStore.cs      реалізація ICatalogStore на EF Core
      EfBatchJournal.cs      журнал надходжень (таблиця Batches)
      Migrations/            міграції (у Git)
    Contracts/               DTO контракту API, спільні для Api і Web (ЛР12)
    Web/                     Blazor-клієнт до Api (Interactive Server, ЛР12)
      Program.cs             типізований HttpClient з адресою з конфігурації
      Services/              ProductsApiClient – усі виклики Api
      Contracts/             модель форми і результат виклику
      Components/Pages/      Products.razor, ProductEdit.razor
    Api/                     HTTP API (Minimal API, ЛР10)
      Program.cs             DI, CORS, обробка помилок, OpenAPI/Scalar
      Endpoints/             маршрути /api/products
      Validation/            правила FluentValidation і фільтр маршруту
      Errors/                перетворення винятків на ProblemDetails
      Api.http               готові запити для перевірки
    Cli/                     консольний застосунок
      Cli.csproj             містить ProjectReference на Core
      Program.cs             конфігурація, composition root і вибір команди
      appsettings.json       налаштування (+ .Development.json, .Production.json)
      CliSettings.cs         збирання конфігурації і шляхів
      DataCommands.cs        config, export, add
      FileLog.cs             лог у файл з мінімальним рівнем
      CatalogCommand.cs      сценарій роботи з каталогом
      EventsCommand.cs       підписка на події, лог у файл
      ReportCommand.cs       підкоманди звітів
      ImportAsyncCommand.cs  асинхронний імпорт зі скасуванням
      Sinks.cs               одержувачі сповіщень (консоль, файл)
      StoreFactory.cs        вибір сховища через фабрику
      ImportCommand.cs       імпорт із файлу і вивід результату
      DomainDemo.cs          демонстрація доменної моделі
      EnvironmentPrinter.cs  вивід інформації про середовище
  tests/
    Core.Tests/              unit-тести Core (xUnit), посилання лише на Core
    Api.Tests/               інтеграційні тести Api (WebApplicationFactory)
  scripts/
    Show-CoreTargets.ps1     показує Core, зібраний під net8.0 і net10.0
```

Залежності односторонні: `Cli → Core`, `Infrastructure → Core`, `Api → Core + Infrastructure + Contracts`,
`Web → Contracts`. Core не знає ні про консоль, ні про HTTP, ні про базу даних; Web не знає ні про Core,
ні про базу – лише адресу Api і формат JSON.

Домовленість про каталоги в Core на семестр (створюються разом із першим типом):

- `Core/Dto/` – record-типи формату даних (ЛР3)
- `Core/Domain/` – сутності з поведінкою та інваріантами (ЛР4)
- `Core/Storage/` – реалізації сховищ (ЛР5)

## Збірка і запуск

Команди наведено для кореня репозиторію, але з ЛР9 застосунок можна запускати з будь-якого каталогу:
дані, лог і зразкові файли шукаються відносно каталогу застосунку, а не поточного каталогу.

```powershell
dotnet build
dotnet run --project src/Cli                             # каталог, сховище в пам'яті
dotnet run --project src/Cli -- --file                   # каталог, файлове сховище
dotnet run --project src/Cli -- events                   # події сервісу і лог logs/app.log
dotnet run --project src/Cli -- report-top               # звіти: report-low, report-groups, report-sum
dotnet run --project src/Cli -- import-async             # асинхронний імпорт data/import.json
dotnet run --project src/Cli -- config                   # показати діючу конфігурацію
dotnet run --project src/Cli -- export                   # експорт колекції в JSON
dotnet test                                              # unit-тести та інтеграційні тести
dotnet run --project src/Api                             # HTTP API на http://localhost:5080
dotnet run --project src/Web                             # Blazor-клієнт на http://localhost:5090 (Api має бути запущений)
dotnet run --project src/Cli -- data/sample.csv          # імпорт CSV
dotnet run --project src/Cli -- data/sample.json         # імпорт JSON
dotnet run --project src/Cli -- data/mixed.csv --mixed   # товари і склади в одному файлі
dotnet run --project src/Cli -- demo                     # доменна модель: успіх і порушення правил
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

## ЛР4. Доменна модель: сутності, інваріанти, інкапсуляція

У `Core/Domain` з'явилися сутності з поведінкою. Records з ЛР3 залишилися як DTO (формат даних),
а сутність захищає правила: створити об'єкт у некоректному стані або перевести в нього неможливо.

| Сутність | Що робить |
|---|---|
| `Product` | товар із залишком: `Create`, `RegisterArrival`, `Issue`, `Rename`, `ToDto` / `FromDto` |
| `Warehouse` | склад зі списком товарів: `Create`, `AddProduct`, `FindBySku`; список назовні лише для читання |
| `Movement` | документ переміщення зі станом `Draft` / `Confirmed` / `Cancelled` |

Стан інкапсульовано: залишок лежить у приватному полі `_quantity`, публічних сетерів немає,
конструктори приватні, об'єкти створюються лише через фабричний метод `Create`.

### Інваріанти

| № | Правило | Виняток | Де перевіряється |
|---|---|---|---|
| 1 | Id, SKU, назва й одиниця виміру товару не порожні | `ArgumentException` | `Product.Create` |
| 2 | Початковий залишок не від'ємний | `ArgumentOutOfRangeException` | `Product.Create` |
| 3 | Кількість приходу і видачі більша за нуль | `ArgumentOutOfRangeException` | `Product.RegisterArrival`, `Product.Issue` |
| 4 | Не можна видати більше, ніж є на залишку | `DomainException` | `Product.Issue` |
| 5 | Нова назва товару не порожня | `ArgumentException` | `Product.Rename` |
| 6 | На одному складі немає двох товарів з однаковим SKU | `DomainException` | `Warehouse.AddProduct` |
| 7 | Переміщати можна лише той самий товар (однаковий SKU) | `DomainException` | `StockTransfer.Move` |
| 8 | Підтверджене або скасоване переміщення змінити не можна | `DomainException` | `Movement.Confirm`, `Movement.Cancel` |

`Argument*` – некоректний сам аргумент; `DomainException` – аргумент нормальний, але операція
порушує бізнес-правило в поточному стані (до ЛР8 тут був `InvalidOperationException`). `FromDto` викликає `Create`, тому дані з файлу проходять ті самі перевірки.
У `Core/Domain` немає звернень ні до `Console`, ні до `File`.

### Запуск

```powershell
dotnet run --project src/Cli -- demo
```

```text
=== Сценарій 1: успіх ===
P-001 [SKU-001] Цемент М400 25кг — 100 шт
P-001 [SKU-001] Цемент М400 25кг — 120 шт
W-01 Головний склад — товарів: 1

=== Сценарій 2: порушення інваріантів ===
  видача більша за залишок: DomainException — Не можна видати 1000: залишок SKU-001 = 120
  порожній SKU: ArgumentException — SKU не може бути порожнім (Parameter 'sku')
  від'ємний залишок: ArgumentOutOfRangeException — Початковий залишок не може бути від'ємним (Parameter 'quantity') Actual value was -5.
  ...
  Після всіх відмов стан не змінився: P-001 [SKU-001] Цемент М400 25кг — 120 шт
```

### Додаткові завдання

1. **Сутності з результату імпорту**: `ProductFactory.FromImport` бере `ImportResult<ProductDto>` і
   повертає сутності разом із переліком записів, що не пройшли інваріанти (сценарій 5, `data/stock.csv`).
2. **Інваріант на дві сутності**: `StockTransfer.Move(from, to, amount)` – переміщення лише між
   записами одного товару. Правило винесене з сутностей, бо жодна з них не знає про іншу;
   з ЛР5 такі правила викликатиме сервіс.
3. **Стан-перелічення**: `MovementStatus` і перевірка дозволених переходів через switch expression у `Movement`.

## ЛР5. Сервісний шар: інтерфейс, два сховища, ручний DI

Доменна логіка більше не залежить від того, де лежать дані. Сервіс працює з інтерфейсом,
а конкретне сховище обирається в одному місці – у `Program.cs`.

| Що | Назва | Де |
|---|---|---|
| Інтерфейс сховища | `ICatalogStore` (List, GetById, Add, Update, Remove) | `Core/Abstractions` |
| Реалізація в пам'яті | `InMemoryCatalogStore` | `Core/Storage` |
| Файлова реалізація | `FileCatalogStore` (JSON) | `Core/Storage` |
| Сервіс | `CatalogService` (Add, Receive, Issue, Remove, All, Find, FindAll) | `Core/Services` |

Схема залежностей:

```text
Cli (Program.cs) → CatalogService → ICatalogStore ← InMemoryCatalogStore | FileCatalogStore
```

`CatalogService` отримує `ICatalogStore` через конструктор і не знає жодного конкретного класу.
Пакет `Microsoft.Extensions.DependencyInjection` не підключено: залежності передаються вручну через `new`.

### Запуск

```powershell
dotnet run --project src/Cli              # Сховище: InMemoryCatalogStore – після виходу дані зникають
dotnet run --project src/Cli -- --file    # Сховище: FileCatalogStore – дані накопичуються між запусками
```

Файлове сховище зберігає дані у файлі з конфігурації (з ЛР9 – `data/items.json` поруч із зібраним
застосунком, `src/Cli/bin/Debug/net10.0/data/items.json`); повний шлях програма друкує на початку.
На диск записуються DTO (`ProductDto`), а сутності відновлюються через `Product.FromDto`.

Сценарій: додати товар, прихід 25, видача 10, знайти за id, показати список, пошук з умовою,
три відмови (прихід для неіснуючого id, видача понад залишок, дубль id) – із зрозумілим текстом.

### Додаткові завдання

1. **Декоратор** `CachingCatalogStore`: приймає інше `ICatalogStore` і кешує список. Прапорець `--cache`.
2. **Пошук з умовою**: `CatalogService.FindAll(Func<Product, bool>)`, наприклад `p => p.Quantity < 20`.
3. **Фабрика** `StoreFactory.Create(args, dataPath)` – прапорець `--factory`. Порівняння:
   у варіанті «все в Program.cs» видно одразу, що створюється, і це зручно, поки варіантів два.
   Фабрика прибирає розгалуження з точки входу і стане в пригоді, коли сховищ побільшає,
   але конкретні класи тепер згадуються у двох місцях Cli.

## ЛР6. Доменні події, делегати Action/Func, лямбди

Сервіс повідомляє про зміни подією, а не друкує сам. Core не знає ні про консоль, ні про лог-файл:
що робити зі сповіщенням, вирішує той, хто підписався.

- `Core/Events/CatalogChangeKind.cs` – вид зміни: `Added`, `Received`, `Issued`, `Removed`.
- `Core/Events/CatalogChangedEventArgs.cs` – дані події: Id, вид, дельта, залишок після, час.
- `CatalogService.Changed` – `event EventHandler<CatalogChangedEventArgs>?`; піднімається в одному
  місці (`OnChanged`) і лише після успішної зміни. Якщо інваріант кинув виняток, події немає.

Подіями вважаються зміни стану каталогу: додавання товару, прихід, видача, видалення.

Підписники в Cli (`EventsCommand.cs`):

1. локальний метод `OnCatalogChanged` – друкує рядок у консоль і дописує його в `logs/app.log`;
2. лямбда `(_, _) => changes++` – лічильник подій (замикання над локальною змінною).

Після `service.Changed -= OnCatalogChanged` операція виконується, але рядка в консолі й у лозі немає –
лічильник при цьому зростає, бо лямбду не відписували.

Лог лежить у `logs/app.log` поруч із застосунком; шлях будується через `Path.Combine` від
`AppContext.BaseDirectory`, каталог створюється кодом.

```powershell
dotnet run --project src/Cli -- events
```

```text
21:39:29 Added    13eef464  +50 → залишок 50
21:39:29 Received 13eef464  +25 → залишок 75
21:39:29 Issued   13eef464  -10 → залишок 65
Подій отримано: 3

Відмови (події не виникають):
  прихід з від'ємною кількістю: Кількість приходу має бути більшою за нуль (Parameter 'amount') Actual value was -5.
  прихід для неіснуючого id: Немає запису з id=нема.
Подій отримано: 3

Після відписки OnCatalogChanged:
Подій отримано: 4
```

`Func` використано там, де потрібне значення у відповідь: `CatalogService.FindAll(Func<Product, bool>)`,
наприклад `service.FindAll(p => p.Quantity < 10)`.

### Додаткові завдання

1. **INotificationSink** (`Core/Abstractions`) з реалізаціями `ConsoleSink` і `FileSink` у Cli; сервіс
   отримує його через конструктор. Прапорці `--sink` і `--sink-file`. Порівняно з подією: одержувач
   один і задається під час створення сервісу, його не можна додати чи зняти під час роботи;
   зате залежність видно в конструкторі. Подія дозволяє будь-яку кількість підписників і відписку.
2. **Action<string> в імпорті**: `ProductCsvImporter.Load(path, onSkipped)` викликає делегат одразу
   для кожного пропущеного рядка. Прапорець `--trace`: `dotnet run --project src/Cli -- data/sample.csv --trace`.

## ЛР7. Звіти через LINQ

Усі запити лежать у `Core/Reports/ReportService.cs`. Методи приймають `IEnumerable<Product>`
(а не сховище) і повертають `IReadOnlyList` з DTO, а не сутності. У `Program.cs` і в Cli-командах
немає жодного `Where` чи `GroupBy` – лише виклик звіту і вивід. Використано тільки LINQ to Objects.

| Команда | Метод | Питання, на яке відповідає | Оператори |
|---|---|---|---|
| `report-top` | `TopByQuantity(source, 5)` | яких товарів найбільше | OrderByDescending, ThenBy, Take, Select |
| `report-low` | `LowStock(source, 10)` | які товари закінчуються | Where, OrderBy, ThenBy, Select |
| `report-groups` | `ByUnit(source)` | як залишки розподілені за одиницями виміру | GroupBy, Select, Count, Sum, OrderByDescending |
| `report-sum` | `Totals(source)` | скільки позицій, сума, середнє, максимум | Sum, Average, Max |
| `report-deferred` | – | демонстрація відкладеного виконання | Where, Count, ToList |

DTO звітів: `StockLine`, `GroupSummary`, `MovementSummary` (records у `Core/Reports`).
У топ-N другий ключ сортування – SKU, тому порядок однаковий за кожного запуску.
Порожнє джерело не спричиняє винятку (перевірка: `--empty`).

```powershell
dotnet run --project src/Cli -- report-groups
dotnet run --project src/Cli -- report-sum
dotnet run --project src/Cli -- report-low --empty
```

```text
  шт     позицій:   4   сумарно:   4431
  м      позицій:   2   сумарно:   1600
  кг     позицій:   3   сумарно:    790
  м2     позицій:   3   сумарно:    716
  уп     позицій:   2   сумарно:    149
  л      позицій:   2   сумарно:     72
  т      позицій:   2   сумарно:     25

  позицій 18, всього 7783, середнє 432,39, макс 4200
```

### Відкладене виконання

`Where`, `Select`, `OrderBy`, `GroupBy` лише описують запит. Виконується він тоді, коли результат
перебирають: у `foreach` або завершальним оператором (`ToList`, `Count()`, `Sum`). У звітах
`ReportService` кожен метод закінчується `ToList()`, тому запит виконується один раз усередині
методу, і Cli отримує готовий список. У демо `report-deferred` запит `query` збережено без `ToList()`:
перший `query.Count()` дав 3, потім у джерело додали товар із залишком 3, і другий `query.Count()`
перерахував запит по зміненому джерелу й дав 4. Після `query.ToList()` результат зафіксовано:
джерело очистили, а у знімку лишилося 4.

```text
Дефіцитних до змін: 3
Дефіцитних після змін: 4
У знімку лишилось: 4
Запит по очищеному джерелу: 0
```

### Додаткові завдання

1. **GroupJoin двох колекцій** (`report-moves`): товари × переміщення зі зведенням у `MovementSummary`;
   товари без переміщень теж потрапляють у результат.
2. **Власний метод розширення** `TopBy<T>(this IEnumerable<T>, Func<T, int>, int)` – використано у
   двох звітах: `TopGroups` (`report-top-groups`) і `MostMoved` (`report-most-moved`).
3. **Синтаксис запитів** (`report-groups-query`): `ByUnitQuerySyntax` записано як
   `from ... group ... by ... into g orderby ... select ...`. Результат той самий. Для групування
   такий запис читається як речення; у методному синтаксисі зручніше дописувати `Where`, `ThenBy`, `Take`
   ланцюжком, тому основні звіти написані методами.
4. **Stopwatch на 100 000 записів** (`report-bench`, конфігурація Debug, середнє за 20 прогонів):
   без проміжних `ToList` – близько 32 мс, з `ToList` після кожного кроку – близько 28 мс.
   Різниця мала: зайві списки не прискорюють запит суттєво, зате кожен `ToList` створює ще одну
   копію даних у пам'яті.

## ЛР8. async/await, скасування, доменні винятки, unit-тести

### Асинхронний імпорт

Асинхронним став імпорт товарів із файлу – це ввід-вивід, який може тривати довго. Операції над
сховищем у пам'яті (`Add`, `Receive`, `Issue`) і звіти залишилися синхронними: там немає очікування.

- `Core/Abstractions/ICatalogImporter.cs` – контракт `Task<IReadOnlyList<Product>> LoadAsync(path, ct)`.
- `Core/Storage/JsonCatalogImporter.cs` – `FileStream(useAsync: true)` + `JsonSerializer.DeserializeAsync`,
  без `Task.Run`. Токен перевіряється в циклі (`ThrowIfCancellationRequested`) і передається далі.
- `CatalogService.ImportAsync(importer, path, ct)` – додає записи, піднімає подію для кожного;
  запис з id, який уже є у сховищі, пропускається, тому повторний імпорт нічого не дублює.
- У розв'язку немає `async void`, `.Result` і `.Wait()`.

```powershell
dotnet run --project src/Cli -- import-async                        # Імпортовано записів: 17
dotnet run --project src/Cli -- import-async data/import-bad.json   # Дані відхилено: Запис 3: ...
dotnet run --project src/Cli -- import-async --slow=5 --timeout=1   # Імпорт скасовано (спрацював таймаут).
dotnet run --project src/Cli -- import-async --slow=20              # натиснути Ctrl+C під час очікування
```

`--slow=N` імітує великий файл (затримка N секунд перед читанням), щоб скасування можна було показати.
Скасування – це не помилка: `OperationCanceledException` обробляється окремим `catch`, який стоїть першим.

### Винятки

| Виняток | Коли | Приклад |
|---|---|---|
| `ArgumentException`, `ArgumentOutOfRangeException` | некоректний аргумент | порожній SKU, прихід −5 |
| `DomainException` | порушено бізнес-правило | видати більше залишку, немає запису з таким id, зіпсований запис у файлі імпорту |
| `OperationCanceledException` | операцію скасовано (Ctrl+C або таймаут) | – |
| `FileNotFoundException`, `IOException` | технічна помилка, передається як є | файлу немає |

Коли імпортер обгортає `ArgumentException` або `JsonException` у `DomainException`, початковий виняток
зберігається в `InnerException`.

### Тести

```powershell
dotnet test
```

```text
Passed!  - Failed:     0, Passed:    39, Skipped:     0, Total:    39
```

Проєкт `tests/Core.Tests` посилається лише на Core. Сховище в тестах – `InMemoryCatalogStore`;
тести не читають `data/` з репозиторію, а кілька тестів із файлом пишуть у системний temp і видаляють файл.

| Файл | Що перевіряє |
|---|---|
| `ProductTests` | інваріанти товару, нормалізація SKU, `ToDto` / `FromDto` |
| `DomainRulesTests` | дубль SKU на складі, переміщення, переходи стану документа |
| `CatalogServiceTests` | відмова не змінює стан, подія рівно один раз, події немає після відмови |
| `ReportServiceTests` | дефіцит (`[Theory]`), детермінований топ-N, групування, порожнє джерело |
| `ImportTests` | скасування, зіпсований запис, повторний імпорт, зв'язані токени, прогрес |
| `CsvImportTests`, `StorageTests` | CSV-імпорт з помилками, файлове і кешуюче сховища, потоковий імпорт |

### Додаткові завдання

1. **Потоковий імпорт**: `JsonCatalogImporter.StreamAsync` повертає `IAsyncEnumerable<Product>`
   (з `[EnumeratorCancellation]`), сервіс читає його через `await foreach`. Прапорець `--stream`.
2. **Зв'язані токени**: `CancellationTokenSource.CreateLinkedTokenSource(userToken, timeoutToken)`;
   тест `ImportAsync_WithLinkedTokens_IsCancelledByTimeout_NotByUser` доводить, що спрацював саме таймаут.
3. **IProgress<int>**: `ImportAsync(..., progress)` повідомляє відсоток; у Cli прапорець `--progress`.
4. **Покриття**: `dotnet test --collect:"XPlat Code Coverage"` – 63,1 % рядків Core. Не покриті
   `EnvironmentInfo` (залежить від машини, де запускають), `SampleData` (демо-дані), `MixedCsvImporter`
   і `ProductJsonImporter` (додаткові імпортери ЛР3), частина звітів з додаткових завдань ЛР7.
   Це прийнятно: основні правила домену, сервіс, сховища й обов'язкові звіти покриті на 84–100 %.

## ЛР9. Шляхи, JSON, конфігурація, secrets

У коді немає абсолютних шляхів і паролів. Усі шляхи будуються через `Path.Combine` від
`AppContext.BaseDirectory`, тому запуск з кореня solution і з `src/Cli` працює з тим самим файлом даних.

### Профілі

| Профіль | Як увімкнути | Лог | Де лежать дані |
|---|---|---|---|
| Development (за замовчуванням) | нічого не робити | рівень `Debug` – детальний | `data/items.json` поруч із застосунком |
| Production | `CROSSAPP_ENVIRONMENT=Production` | рівень `Warning` | `%LOCALAPPDATA%/CrossApp/data/items.json` (каталог локальних даних користувача) |

### Ключі конфігурації

| Ключ | За замовчуванням | Змінна середовища | Призначення |
|---|---|---|---|
| `EnvironmentName` | `Development` | `CROSSAPP_ENVIRONMENT` (вибір файла профілю) | назва середовища |
| `Storage:DataPath` | `data` | `CROSSAPP_Storage__DataPath` або проста `CROSSAPP_DATA_PATH` | каталог даних (відносний – від каталогу застосунку) |
| `Storage:FileName` | `items.json` | `CROSSAPP_Storage__FileName` | ім'я файлу даних |
| `Storage:UseLocalAppData` | `false` | `CROSSAPP_Storage__UseLocalAppData` | тримати дані в каталозі користувача |
| `Logging:MinimumLevel` | `Information` | `CROSSAPP_Logging__MinimumLevel` | `Debug` / `Information` / `Warning` / `Error` |
| `Warehouse:DefaultUnit` | `шт` | `CROSSAPP_Warehouse__DefaultUnit` | одиниця виміру для команди `add` |
| `Integration:ApiKey` | немає | `CROSSAPP_Integration__ApiKey` | secret; у репозиторії відсутній |

Порядок джерел (пізніше перекриває попереднє): `appsettings.json` → `appsettings.{середовище}.json` →
user-secrets → змінні середовища з префіксом `CROSSAPP_` → аргументи виду `--Storage:FileName=x.json`.
Налаштування біндяться в `Core/Configuration/AppOptions` через `config.Get<AppOptions>()`.

### Перевизначення

PowerShell:

```powershell
$env:CROSSAPP_DATA_PATH = "$env:TEMP\crossdata"
dotnet run --project src/Cli -- export
Remove-Item Env:CROSSAPP_DATA_PATH
```

bash:

```bash
CROSSAPP_DATA_PATH=/tmp/crossdata dotnet run --project src/Cli -- export
CROSSAPP_Storage__FileName=other.json dotnet run --project src/Cli -- config
```

### Secret

`Integration:ApiKey` зберігається в user-secrets, поза репозиторієм; у `Cli.csproj` є лише `UserSecretsId`.

```powershell
dotnet user-secrets set "Integration:ApiKey" "<значення>" --project src/Cli
dotnet user-secrets list --project src/Cli
```

Файл лежить у `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json` (Windows) або
`~/.microsoft/usersecrets/<UserSecretsId>/secrets.json` (Linux, macOS). Програма друкує лише факт
наявності: `ApiKey: присутній, довжина 13`. Це засіб для розробки; у продакшені secret передають
змінною середовища або зі спеціального сховища.

### JSON

`export` записує всю колекцію, `import-async <файл>` читає її назад. Файлове сховище і експорт
використовують один формат (`Core/Storage/CatalogJson.cs`): camelCase, відступи, кирилиця без `\uXXXX`.
Запис атомарний: спершу у `items.json.tmp`, потім `File.Move` з заміною – якщо програма впаде
посеред запису, старий файл залишиться цілим. Зіпсований файл даних дає зрозуміле повідомлення,
а не stack trace.

```text
> dotnet run --project src/Cli -- export
Середовище : Development
Файл даних : ...\src\Cli\bin\Debug\net10.0\data\items.json
Файл логу  : ...\src\Cli\bin\Debug\net10.0\logs\app.log (рівень Debug)
Одиниця за замовчуванням : шт
ApiKey: присутній, довжина 13
Експортовано записів: 18
```

```json
[
  {
    "id": "P-001",
    "sku": "SKU-001",
    "name": "Цемент М400 25кг",
    "unit": "шт",
    "quantity": 120
  }
]
```

Каталог `data/` у корені репозиторію містить лише зразкові вхідні файли; під час збірки вони копіюються
в каталог `samples` поруч із застосунком. Робочі дані (`data/items.json`) і лог створюються під час
роботи всередині `bin`, тому в Git не потрапляють.

## ЛР10. Minimal API: CRUD, валідація, OpenAPI (Scalar)

Друга точка входу – `src/Api`. Вона використовує той самий Core, що й Cli; Core не має посилань на ASP.NET.

### Запуск

```powershell
dotnet run --project src/Api
```

Порт зафіксовано в `src/Api/Properties/launchSettings.json`: **http://localhost:5080**.
Якщо порт зайнятий: `dotnet run --project src/Api --urls http://localhost:5090`.

- Scalar UI: http://localhost:5080/scalar/v1
- Документ OpenAPI: http://localhost:5080/openapi/v1.json
- Перевірка стану: http://localhost:5080/health

OpenAPI і Scalar увімкнені лише в середовищі Development.

### Маршрути

| Метод | Шлях | Тіло | Коди відповідей |
|---|---|---|---|
| GET | `/api/products?page=1&pageSize=50&search=...` | – | 200 `{ items, total, page, pageSize }` |
| GET | `/api/products/{id}` | – | 200, 404 |
| POST | `/api/products` | `SaveProductRequest` | 201 + `Location`, 400, 409 (артикул уже є) |
| PUT | `/api/products/{id}` | `SaveProductRequest` | 200, 400, 404, 409 |
| DELETE | `/api/products/{id}` | – | 204, 404 |
| GET | `/api/products/low-stock?threshold=10` | – | 200 |
| POST | `/api/products/{id}/receive` | `{ "quantity": 10 }` | 200, 400, 404 |
| POST | `/api/products/{id}/issue` | `{ "quantity": 10 }` | 200, 400, 404, 409 (не вистачає залишку) |
| GET | `/health` | – | 200 |

`SaveProductRequest`: `{ "sku": "...", "name": "...", "unit": "...", "quantity": 0 }`.
Відповідь – `ProductDto` (`id, sku, name, unit, quantity`), а не доменна сутність.

Помилки мають формат ProblemDetails (RFC 9457): 400 – з переліком полів у `errors`,
404 – ресурсу немає, 409 – порушено бізнес-правило. Stack trace клієнтові не віддається.

### Приклади curl

```bash
curl -i -X POST http://localhost:5080/api/products \
  -H "Content-Type: application/json" \
  -d '{"sku":"A-100","name":"Cement M400","unit":"bag","quantity":40}'
# HTTP/1.1 201 Created
# Location: /api/products/cc76da2b
# {"id":"cc76da2b","sku":"A-100","name":"Cement M400","unit":"bag","quantity":40}

curl -i http://localhost:5080/api/products/cc76da2b            # 200
curl -i -X DELETE http://localhost:5080/api/products/cc76da2b  # 204
curl -i http://localhost:5080/api/products/cc76da2b            # 404

curl -i -X POST http://localhost:5080/api/products \
  -H "Content-Type: application/json" \
  -d '{"sku":"","name":"X","unit":"","quantity":-1}'
# HTTP/1.1 400 Bad Request
# {"title":"One or more validation errors occurred.","status":400,
#  "errors":{"Sku":["Артикул обов'язковий."],"Unit":["Одиниця виміру обов'язкова."],
#            "Quantity":["Кількість не може бути від'ємною."]}}
```

У Windows PowerShell команда `curl` – це інший інструмент; використовуйте `curl.exe` і файл з тілом
(`--data-binary "@body.json"`) або готові запити з `src/Api/Api.http` у Visual Studio.

### DI

На ЛР10 сховище було в пам'яті (`InMemoryCatalogStore`, Singleton, захищене `lock`). З ЛР11 це
`EfCatalogStore` з життєвим циклом **Scoped** – див. розділ ЛР11.
Обробники маршрутів отримують сховище і сервіс параметрами делегата. Читання йде напряму зі сховища,
зміни – через сервіс: він перевіряє правила і піднімає події, які записує логер Api.

### Додаткові завдання

1. **Пагінація і пошук**: `GET /api/products?page=1&pageSize=20&search=...` → `{ items, total, page, pageSize }`.
2. **FluentValidation** через `AddEndpointFilter<ValidationFilter<T>>()`: обробник не викликається, якщо тіло не пройшло правила.
3. **DomainException → 409** окремим обробником винятків `DomainExceptionHandler` (`IExceptionHandler`).
4. **Інтеграційні тести** `tests/Api.Tests` на `WebApplicationFactory`: POST дає 201, GET за `Location` – 200;
   також 400 з полями, 404, 409 і видалення.

## ЛР11. EF Core + SQLite: міграції, зв'язок 1:N

Api тепер зберігає дані в базі SQLite – один файл `src/Api/data/app.db`. Маршрути і формат відповідей
ЛР10 не змінилися: замінено лише реалізацію `ICatalogStore` і її реєстрацію в DI.

```text
Core            – домен та інтерфейси, жодного пакета EF Core
Infrastructure  – залежить від Core і EF Core, реалізує ICatalogStore
Api             – залежить від Core та Infrastructure, реєструє реалізації
```

### Схема

| Таблиця | Поля | Ключі та обмеження |
|---|---|---|
| `Products` | Id TEXT(32), Sku TEXT(50), Name TEXT(200), Unit TEXT(20), Quantity INTEGER | PK `Id`; унікальний індекс `IX_Products_Sku` |
| `Batches` | Id INTEGER, ProductId TEXT, Amount INTEGER, ReceivedAt TEXT | PK `Id` (автоінкремент); FK `ProductId` → `Products.Id`, `ON DELETE CASCADE` |

Зв'язок 1:N: у товару багато партій (надходжень). `Quantity` – поточний залишок, `Batches` – журнал
надходжень: рядок додає лише операція «прихід» (`POST /api/products/{id}/receive`); переглянути –
`GET /api/products/{id}/batches`. EF Core мапить `ProductRow`, а не доменний `Product`; рядок
перетворюється на сутність через `Product.Create`, тобто з усіма перевірками.

### Команди

Потрібен інструмент міграцій (один раз на машину):

```powershell
dotnet tool install --global dotnet-ef --version "10.*"
```

```powershell
dotnet ef migrations add <Назва> --project src/Infrastructure --startup-project src/Api
dotnet ef database update --project src/Infrastructure --startup-project src/Api
dotnet ef migrations list --project src/Infrastructure --startup-project src/Api
```

У середовищі Development Api сам застосовує міграції під час запуску; у Production – командою `database update`.

**Скинути базу:** зупинити Api, видалити `src/Api/data/app.db` і виконати `database update`
(або просто запустити Api в Development) – схема відтвориться з міграцій.

### Конфігурація

Рядок підключення – ключ `ConnectionStrings:AppDb` у `src/Api/appsettings.json`
(`Data Source=data/app.db`). Відносний шлях рахується від каталогу проєкту Api (`ContentRootPath`),
каталог створюється автоматично. Перевизначення змінною середовища: `ConnectionStrings__AppDb`.

Файли `*.db`, `*.db-shm`, `*.db-wal` додані в `.gitignore`; каталог `Migrations` – у Git.

### Перевірка

```text
POST /api/products {"sku":"P-220511",...}        -> 201
POST /api/products/{id}/receive {"quantity":10}  -> 200, quantity 50
повторний POST з тим самим артикулом             -> 409 (а не 500 з UNIQUE constraint)
... зупинка Api і повторний запуск ...
GET /api/products?search=P-220511                -> запис на місці, quantity 50
```

`DbContext`, `EfCatalogStore` і `CatalogService` мають життєвий цикл Scoped: окремий екземпляр на кожен HTTP-запит.
Інтеграційні тести (`tests/Api.Tests`) працюють із тимчасовим файлом SQLite і перевіряють, зокрема,
що дані переживають перезапуск Api.

## ЛР12. Blazor-клієнт до власного API

`src/Web` – браузерний інтерфейс до того самого Api. Web не посилається на Infrastructure і не має
рядка підключення до бази: він знає лише базову адресу Api, маршрути та формат JSON.

### Режим Blazor

Обрано **Blazor Web App, Interactive Server** (`dotnet new blazor --interactivity Server --all-interactive`).
C#-код компонентів виконується **на сервері Web**; браузер отримує готовий HTML і далі обмінюється
подіями та оновленнями інтерфейсу через з'єднання SignalR. Запити до Api робить сервер Web
(server-to-server), а не браузер. Режим задано глобально в `Components/App.razor`
(`<Routes @rendermode="InteractiveServer" />`).

### Запуск: два процеси

```powershell
# термінал 1
dotnet run --project src/Api          # http://localhost:5080
# термінал 2
dotnet run --project src/Web          # http://localhost:5090  ->  відкрити /products
```

### Конфігурація

| Де | Ключ | Значення | Змінна середовища |
|---|---|---|---|
| `src/Web/appsettings.json` | `Api:BaseUrl` | `http://localhost:5080` | `Api__BaseUrl` |
| `src/Web/appsettings.json` | `Warehouse:LowStockThreshold` | `10` – межа підсвітки рядків | `Warehouse__LowStockThreshold` |
| `src/Api/appsettings.json` | `Cors:WebOrigin` | `http://localhost:5090` | `Cors__WebOrigin` |

У `.razor` і `.cs` файлах Web немає жодної адреси: `ProductsApiClient` зареєстровано через
`AddHttpClient<ProductsApiClient>` з `BaseAddress` із конфігурації.

**CORS.** Api дозволяє запити з origin Web (`Cors:WebOrigin`). Браузер блокує запити сторінки до іншого
origin, якщо сервер їх явно не дозволив; це потрібно клієнту, який працює в браузері (Blazor WebAssembly).
Для Interactive Server ця політика не потрібна, бо до Api звертається сервер Web, а не браузер;
вона додана, щоб те саме Api підходило і для WASM-клієнта.

### Сторінки

| Сторінка | Маршрут UI | Виклик Api | Коди відповіді |
|---|---|---|---|
| Список товарів | `/products` | `GET /api/products?search=&sortBy=&desc=` | 200 |
| Видалення (у списку) | `/products` | `DELETE /api/products/{id}` | 204, 404 |
| Новий товар | `/products/new` | `POST /api/products` | 201, 400, 409 |
| Редагування | `/products/{id}` | `GET /api/products/{id}`, потім `PUT /api/products/{id}` | 200, 400, 404, 409 |

Список показує стан завантаження, текст «Записів ще немає.» для порожньої колекції і підсвічує товари
із залишком нижче межі. Форма перевіряє поля локально (Data Annotations), а помилки 400 від сервера
показує біля відповідних полів; 409 – загальним повідомленням. Якщо Api зупинено, сторінка показує
«Api недоступний. Перевірте, чи запущено src/Api.», а не падає.

### Додаткові завдання

1. **Видалення з підтвердженням**: кнопка «Видалити» показує запит на підтвердження; після видалення
   список перезавантажується з Api без повного перезавантаження сторінки.
2. **Пошук і сортування на боці Api**: `GET /api/products?search=...&sortBy=sku|name|quantity&desc=true`;
   UI лише передає параметри, нічого не фільтрує сам.
3. **Спільний проєкт `src/Contracts`** з DTO для Api і Web. Плюс: контракт описано один раз, розбіжність
   назв полів між сервером і клієнтом неможлива, зміну контракту одразу показує компілятор. Мінус:
   клієнт і сервер зв'язані спільною збіркою і мають оновлюватися разом; клієнту не на .NET це не допоможе.
4. **Скасування запиту**: `Products.razor` тримає `CancellationTokenSource`; новий пошук або вихід зі
   сторінки (`Dispose`) скасовує попередній запит до Api.
