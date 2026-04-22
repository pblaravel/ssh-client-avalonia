# SSH Client Avalonia

Базовый стартовый проект Avalonia для дальнейшей реализации SSH-клиента по ТЗ из `tz.txt`.

## Требования

- Ubuntu/Linux, macOS или Windows
- .NET SDK 10.0+

Проверка установленного SDK:

- `dotnet --list-sdks`

## Установка шаблонов Avalonia (один раз)

- `dotnet new install Avalonia.Templates`

## Сборка

- `dotnet restore SSHClientAvalonia.sln`
- `dotnet build SSHClientAvalonia.sln -c Debug`

## Запуск приложения

- `dotnet run --project src/SSHClientAvalonia/SSHClientAvalonia.csproj`

После запуска откроется окно Avalonia с тестовой главной страницей.
