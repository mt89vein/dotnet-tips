# Разрешение конфликтов

Данный пример показывает один из способов реализации обработки конфликтов конкурентных обновлений с возможностью разрешения конфликтов.


## Установка
Для запуска примера, необходимо установить [C# .NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0),
Docker и [Node.js LTS версию](https://nodejs.org/en).

## Запуск

Из текущей директории где находится readme.md вызвать:

```bash
docker-compose -f docker-compose.debug.yml up -d
```

Centrifugo будет запущен на 8_000 порту админка (admin/secret) и на 10_000 порту gRPC API.


```bash
dotnet run ./src/Backend
```

Сервер будет запущен тут http://localhost:5261

```bash
npm run --prefix ./src/Frontend dev
```

Клиент будет запущен скорее всего тут http://localhost:5173/
Смотрите вывод в консоли
