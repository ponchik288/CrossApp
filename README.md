# CrossApp

Наскрізний проєкт з крос-платформного програмування.

Предметна область: Склад.

Сутності: Product, StockBatch, Warehouse, Movement.

Призначення: облік залишків товарів по партіях.

## Запуск

dotnet build

dotnet run --project src/Cli

## Середовище

.NET SDK 10.0, Windows 10 x64.

## Додаткове завдання

### JSON-режим

Програму можна запустити з параметром `--json`:

dotnet run --project src/Cli -- --json

У цьому режимі інформація про середовище виводиться одним JSON-рядком засобами System.Text.Json.

### Self-contained publish

Було виконано self-contained публікацію для двох RID:

- win-x64 — 76,84 MB
- win-arm64 — 86,34 MB

Розмір каталогів publish відрізняється через різні runtime-компоненти для відповідних архітектур.