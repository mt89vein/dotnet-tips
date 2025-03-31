## Результаты бенчмарков

Запускал на машине:

AMD Ryzen 9 5950X, 1 CPU, 32 logical and 16 physical cores  
.NET 9, build 9.0.200  
СУБД Postgres 16. Запущенный в единственном экземпляре в Docker Desktop, который не ограничен в CPU, но имеет лимит в память 2 GB.  
Поднимается командой ниже в терминале из корня репозитория

```bash
docker-compose -f docker-compose.debug.yml up -d
```

Для генерации тестового набора данных, использован скрипт ниже на базе данных users.

```sql
-- функция для генерации uuid v7
create or replace function uuid_generate_v7()
returns uuid
as $$
begin
  -- use random v4 uuid as starting point (which has the same variant we need)
  -- then overlay timestamp
  -- then set version 7 by flipping the 2 and 1 bit in the version 4 string
  return encode(
    set_bit(
      set_bit(
        overlay(uuid_send(gen_random_uuid())
                placing substring(int8send(floor(extract(epoch from clock_timestamp()) * 1000)::bigint) from 3)
                from 1 for 6
        ),
        52, 1
      ),
      53, 1
    ),
    'hex')::uuid;
end
$$
language plpgsql
volatile;

-- генерируем данные в таблицу
insert into users
select
    uuid_generate_v7() as id,
    arrays.firstnames[s.a % ARRAY_LENGTH(arrays.firstnames,1) + 1] AS name,
    substring('ABCDEFGHIJKLMNOPQRSTUVWXYZ' from s.a%26+1 for 1) AS patronymic,
    arrays.lastnames[s.a % ARRAY_LENGTH(arrays.lastnames,1) + 1] AS surname,
    timestamp '2025-01-01 05:00:00' + random() * (timestamp '2025-01-01 05:00:00'- timestamp '2025-04-01 05:00:00') as created_at,
    random() < 0.9 as is_active
FROM generate_series(1, 1_000_000) AS s(a)
CROSS JOIN(
    SELECT ARRAY[
    'Adam','Bill','Bob','Calvin','Donald','Dwight','Frank','Fred','George','Howard',
    'James','John','Jacob','Jack','Martin','Matthew','Max','Michael',
    'Paul','Peter','Phil','Roland','Ronald','Samuel','Steve','Theo','Warren','William',
    'Abigail','Alice','Allison','Amanda','Anne','Barbara','Betty','Carol','Cleo','Donna',
    'Jane','Jennifer','Julie','Martha','Mary','Melissa','Patty','Sarah','Simone','Susan'
    ] AS firstnames,
    ARRAY[
        'Matthews','Smith','Jones','Davis','Jacobson','Williams','Donaldson','Maxwell','Peterson','Stevens',
        'Franklin','Washington','Jefferson','Adams','Jackson','Johnson','Lincoln','Grant','Fillmore','Harding','Taft',
        'Truman','Nixon','Ford','Carter','Reagan','Bush','Clinton','Hancock'
    ] AS lastnames
) AS arrays

-- обновляем статистику таблицы
analyze users
```

После этого будет создано 1 млн пользователей с рандомными данными, похожими на настоящие. Общий объем таблицы 116 мб.

Приложения запускаются каждый в своей консоли в релизной сборке:

```bash
dotnet run --project ./SyncDataBenchmark/SomeService/SomeService.csproj -c Release
```

```bash
dotnet run --project ./SyncDataBenchmark/SyncDataSource/SyncDataSource.csproj -c Release
```

Для запуска конкретного сценария нужно перейти по ссылке: http://localhost:5180/sync?f=NpgsqlCopy&i=NpgsqlCopy
Тест кейс выбирается через query params.  
f - FetchApproach, i - InsertApproach. Выбирайте любые интересующие из таблицы с кейсами ниже.  
Запуск производится в режиме fire & forget, так что ожидайте лог с результатом с консоли приложения SomeService.

В docker-compose также поднимается prometheus и grafana. Дэшборд в нем уже настроен и доступен по ссылке http://localhost:3000/d/KdDACDp4z/asp-net-core?orgId=1

Результаты бенчмарков:

| TestCase                 | Rank | FetchApproach  | InsertApproach | Process Time (best of 3) | SomeService Memory Allocated | SomeService Physical Memory | SomeService GC Collections    | SomeService GC Pause Duration | SyncDataSource Memory Allocated | SyncDataSource Physical Memory | SyncDataSource GC Collections | SyncDataSource GC Pause Duration |
| ------------------------ | ---- | -------------- | -------------- | ------------------------ | ---------------------------- | --------------------------- | ----------------------------- | ----------------------------- | ------------------------------- | ------------------------------ | ----------------------------- | -------------------------------- |
| [1](./artifacts/1.png)   | 10   | EFOffsetPaging | EFTypical      | 131 813 ms               | 23.9 GB                      | 1.21 GB                     | gen0: 128, gen1: 22, gen2: 73 | 10300 ms                      | 1.74 GB                         | 131 MB                         | gen0: 296, gen1: 2, gen2: 3   | 233 ms                           |
| [2](./artifacts/2.png)   | 9    | EFKeysetPaging | EFTypical      | 34 559 ms                | 26.7 GB                      | 1.38 GB                     | gen0: 91, gen1: 33, gen 2: 61 | 6320 ms                       | 1.76 GB                         | 132 MB                         | gen0: 316, gen1: 2, gen2: 3   | 238 ms                           |
| [3](./artifacts/3.png)   | 8    | EFOffsetPaging | EFBulk         | 101 411 ms               | 2.57 GB                      | 150 MB                      | gen0: 42, gen1: 63, gen2: 3   | 144 ms                        | 1.74 GB                         | 131 MB                         | gen0: 297, gen1: 2, gen2: 3   | 211 ms                           |
| [4](./artifacts/4.png)   | 5    | EFKeysetPaging | EFBulk         | 6 588 ms                 | 2.57 GB                      | 141 MB                      | gen0: 56, gen1: 71, gen2: 3   | 144 ms                        | 1.76 GB                         | 132 MB                         | gen0: 299, gen1: 2, gen2: 3   | 209 ms                           |
| [5](./artifacts/5.png)   | 4    | EFStream       | EFBulk         | 5 390 ms                 | 1.97 GB                      | 147 MB                      | gen0: 37, gen1: 49, gen2: 3   | 108 ms                        | 1.42 GB                         | 120 MB                         | gen0: 387, gen1: 2, gen2: 16  | 140 ms                           |
| [6](./artifacts/6.png)   | 7    | EFStream       | Linq2DB        | 11 945 ms                | 4.27 GB                      | 153 MB                      | gen0: 67, gen1: 95, gen2: 217 | 316 ms                        | 1.28 GB                         | 116 MB                         | gen0: 457, gen1: 2, gen2: 9   | 173 ms                           |
| [7](./artifacts/7.png)   | 3    | NpgsqlCopy     | EFBulk         | 5 310 ms                 | 1.97 GB                      | 139 MB                      | gen0: 39, gen1: 44, gen2: 3   | 124 ms                        | 549 MB                          | 111 MB                         | gen0: 190, gen1: 2, gen2: 3   | 86 ms                            |
| [8](./artifacts/8.png)   | 6    | NpgsqlCopy     | Linq2DB        | 12 550 ms                | 4.27 GB                      | 155 MB                      | gen0: 66, gen1: 95, gen2: 217 | 318 ms                        | 549 MB                          | 109 MB                         | gen0: 190, gen1: 2, gen2: 3   | 93.8 ms                          |
| [9](./artifacts/9.png)   | 1    | NpgsqlCopy     | NpgsqlCopy     | 1 842 ms                 | 741 MB                       | 118 MB                      | gen0: 238, gen1: 1, gen2: 2   | 119 ms                        | 549 MB                          | 112 MB                         | gen0: 190, gen1: 2, gen2: 3   | 90.4 ms                          |
| [10](./artifacts/10.png) | 2    | EFKeysetPaging | NpgsqlCopy     | 3 104 ms                 | 1.35 GB                      | 126 MB                      | gen0: 239, gen1: 1, gen2: 2   | 136 ms                        | 1.77 GB                         | 134 MB                         | gen0: 239, gen1: 1, gen2: 2   | 212 ms                           |

Пояснения к сценариям. В таблице приведены результаты трех прогонов каждого сценария. Время - лучшее из трех прогонов, остальные - сумма, для наглядности.

1. Решение в лоб. Не используется стриминг ни в каком виде. Для получения данных обычная пагинация на оффсетах, вставка через EFCore add.
2. Как и первый вариант, только пагинация не на оффсетах, а на keyset, что позволяет СУБД быстрее отдавать данные.
3. Пагинация на оффсетах, но потимизирована вставка. На 30 секунд быстрее чем 1ый кейс и сильно меньше жрет памяти.
4. Комбинация двух оптимизаций выше. Keyset пагинация + bulk extensions. Уже отличный результат, 6.5 секунд на обработку и сносное потребление памяти, небольшой GC Pause Duration. Многовато в gen1 попадает, но главное что нет сборок gen2.
5. Здесь попытка стримить данные из базы данных в API + bulk extensions на вставку. Быстрее прошлого результата, меньшее потребление памяти как на клиенте так и на сервере. Единственное, на сервере многовато сборок gen2.
6. Альтернативная реализация для предыдущего. Вместо платного bulk extensions, бесплатный Linq2DB. Получилось вдвое медленней, больше потребления памяти и ужасное кол-во gen2 сборок.
7. Тут используется COPY TO STDOUT в связке с npgsql. Это самый быстрый способ читать данные из постреса в стриминговом режиме. Никакой материализации данных в памяти перед отправкой. Для вставки bulk extensions. Результат третий.
8. Как и в 6 варианте, пробуем Linq2DB. Медленно, много жрет памяти.
9. COPY TO STDOUT и COPY FROM STDIN. Самый быстрый способ чтения и записи в postgres. Ожидаемо низкий memory traffic. Все объекты собираются в gen0. Один из самых низкий gc pause duration.  
    Менее чем 2 секунды вычитывает 1 млн записей из бд и записывает в другую бд прогоняя данные через 2 AspNetCore приложения в режиме стриминга данных. К слову, генерация этих данных у меня заняла 5 секунд :)
   Прогонял и 20 раз и 30 раз, всё стабильно быстро и работает без попадания в gen1/gen2.
   Минус данного варианта это сам COPY TO/FROM. Нужно будет явно задавать типы данных, порядок вставки и прочее. Не критично, если покрывать интеграционным тестами, просто потребует быть аккуратным при изменениях таблиц.  
   Еще я пробовал увеличить Read/Write Buffer Size до 32кб, будет быстрее всего на 100 мс, все остальное останется без изменений.

10. Тут я забыл что не тестировал Keyset пагинацию в связке с COPY FROM STDIN для записи. Результат - второй по всем показателям.

Эти тесты показывают базовое поведение при стандартных настройках. Я не тюнил размеры батчей или транзакционность у bulk extensions / linq2db.
NpgsqlCopy вообще пишет данные за 1 запрос, в то время как linq2db под капотом отправляет множество запросов.
