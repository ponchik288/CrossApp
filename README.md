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
      Domain/                сутності з інваріантами (ЛР4)
      Abstractions/          інтерфейс сховища ICatalogStore (ЛР5)
      Storage/               сховища: у пам'яті, файлове, кешуюче (ЛР5)
      Services/              CatalogService – бізнес-операції (ЛР5)
      Events/                дані події зміни каталогу (ЛР6)
      SampleData.cs          зразкові дані (18 товарів)
    Cli/                     консольний застосунок
      Cli.csproj             містить ProjectReference на Core
      Program.cs             composition root і вибір команди
      CatalogCommand.cs      сценарій роботи з каталогом
      EventsCommand.cs       підписка на події, лог у файл
      Sinks.cs               одержувачі сповіщень (консоль, файл)
      StoreFactory.cs        вибір сховища через фабрику
      ImportCommand.cs       імпорт із файлу і вивід результату
      DomainDemo.cs          демонстрація доменної моделі
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
dotnet run --project src/Cli                             # каталог, сховище в пам'яті
dotnet run --project src/Cli -- --file                   # каталог, файлове сховище
dotnet run --project src/Cli -- events                   # події сервісу і лог logs/app.log
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
| 4 | Не можна видати більше, ніж є на залишку | `InvalidOperationException` | `Product.Issue` |
| 5 | Нова назва товару не порожня | `ArgumentException` | `Product.Rename` |
| 6 | На одному складі немає двох товарів з однаковим SKU | `InvalidOperationException` | `Warehouse.AddProduct` |
| 7 | Переміщати можна лише той самий товар (однаковий SKU) | `InvalidOperationException` | `StockTransfer.Move` |
| 8 | Підтверджене або скасоване переміщення змінити не можна | `InvalidOperationException` | `Movement.Confirm`, `Movement.Cancel` |

`Argument*` – некоректний сам аргумент; `InvalidOperation` – аргумент нормальний, але операція
неможлива в поточному стані. `FromDto` викликає `Create`, тому дані з файлу проходять ті самі перевірки.
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
  видача більша за залишок: InvalidOperationException — Не можна видати 1000: залишок SKU-001 = 120
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

Файлове сховище зберігає дані у `data/catalog.json` поруч із зібраним застосунком
(`src/Cli/bin/Debug/net10.0/data/catalog.json`); повний шлях програма друкує на початку.
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
