# SupportDemo — учебный проект для лабораторной работы № 2

**Технологии:** ASP.NET Core Minimal API (.NET 9), Npgsql, PostgreSQL.

Это **исходное приложение**, которое нужно контейнеризировать в ходе лабораторной работы. Dockerfile, `.dockerignore` и `compose.yaml` намеренно **отсутствуют**: их создаёт студент.

## 1. Что умеет приложение

| Метод | Путь | Действие |
|---|---|---|
| GET | `/health` | Проверка самого API; БД не требуется |
| GET | `/health/db` | Проверка подключения к PostgreSQL (`200` либо `503`) |
| POST | `/api/tickets` | Создание обращения (`201`) |
| GET | `/api/tickets` | Список обращений (`200`) |
| GET | `/api/tickets/{id}` | Обращение по ID (`200` / `404`) |
| PATCH | `/api/tickets/{id}/status` | Изменение статуса (`200` / `404`) |

Сущность: `id` (int), `title` (string), `description` (string), `status` (`New`, `InProgress`, `Closed`), `createdAt` (UTC).

## 2. Требования

- .NET SDK 9;
- PostgreSQL, доступный на хосте для локального запуска с БД;
- Docker / Docker Compose для выполнения лабораторной.

NuGet-пакет `Npgsql` автоматически восстанавливается при выполнении `dotnet restore`. Для первой сборки требуется доступ к источнику NuGet либо настроенный локальный кэш.

## 3. Запуск без Docker

В каталоге с `SupportDemo.csproj`:

```bash
dotnet restore
dotnet build
dotnet run
```

Приложение по профилю `Properties/launchSettings.json` слушает `http://localhost:8080`.

Проверка API без базы данных:

```bash
curl -i http://localhost:8080/health
```

Ожидаемый код: `200 OK`.

## 4. Подключение PostgreSQL

Создайте на локальном PostgreSQL пользователя `supportuser`, базу `supportdb` и назначьте пользователю права на подключение и создание таблицы в схеме `public`. Пароль придумайте сами, для учебной БД. Не помещайте реальные учётные данные в исходный код и отчёт.

Пример команд `psql`, исполняемых администратором PostgreSQL (подставьте **свой** пароль):

```sql
CREATE USER supportuser WITH PASSWORD 'YOUR_LOCAL_PASSWORD';
CREATE DATABASE supportdb OWNER supportuser;
```

Если права на схему `public` были ранее ограничены, выполните в `supportdb` от администратора:

```sql
GRANT USAGE, CREATE ON SCHEMA public TO supportuser;
```

Настройте переменную среды `ConnectionStrings__DefaultConnection`.

**PowerShell:**

```powershell
$env:ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=supportdb;Username=supportuser;Password=YOUR_LOCAL_PASSWORD'
dotnet run
```

**Linux/macOS Bash:**

```bash
export ConnectionStrings__DefaultConnection='Host=localhost;Port=5432;Database=supportdb;Username=supportuser;Password=YOUR_LOCAL_PASSWORD'
dotnet run
```

Замените `YOUR_LOCAL_PASSWORD` на ваш локальный пароль, не используемый нигде более. Файл `.env.example` нужен как шаблон переменных для будущего Docker Compose; **само приложение не читает `.env` автоматически**.

Таблица `tickets` создаётся автоматически при первом обращении к `/api/tickets` с доступной БД (`CREATE TABLE IF NOT EXISTS`). Также имеется эквивалентный необязательный скрипт `Database/init.sql`. Повторная инициализация записи не удаляет.

```bash
curl -i http://localhost:8080/health/db
```

Ожидаемый код при корректном подключении — `200`.

## 5. Проверка HTTP API

В `Requests/SupportDemo.http` лежат примеры запросов для IDE с поддержкой `.http`.

**Создайте обращение:**

```bash
curl -i -X POST http://localhost:8080/api/tickets \
  -H 'Content-Type: application/json' \
  -d '{"title":"Не открывается личный кабинет","description":"При входе появляется сообщение об ошибке"}'
```

Код `201 Created`; в JSON будет серверный `id` и `status: "New"`.

**Посмотрите список:**

```bash
curl -i http://localhost:8080/api/tickets
```

**Получите запись по ID** (вместо `1` используйте `id` из ответа POST):

```bash
curl -i http://localhost:8080/api/tickets/1
```

**Измените статус:**

```bash
curl -i -X PATCH http://localhost:8080/api/tickets/1/status \
  -H 'Content-Type: application/json' \
  -d '{"status":"InProgress"}'
```

Команды с переносами `\` предназначены для Bash; в Windows можно воспользоваться `Requests/SupportDemo.http`, PowerShell `Invoke-RestMethod` либо указать весь curl-запрос одной строкой.

Ошибки: незаполненный `title`/`description` или недопустимый `status` — `400`, неизвестный ID — `404`.

## 6. Требования к будущему Docker Compose

В лабораторной работе студент создаёт:

- Dockerfile с этапами SDK (`restore` / `publish`) и ASP.NET runtime;
- `.dockerignore` для `bin/`, `obj/`, `.git/`, `.env` и прочего мусора;
- `compose.yaml` с сервисами `api` и `db`;
- named volume с файлами PostgreSQL;
- переменную `ConnectionStrings__DefaultConnection` в контейнере API;
- переменные `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PASSWORD` у PostgreSQL через локальный `.env`;
- публикацию API `8080:8080` и настройку `ASPNETCORE_URLS=http://+:8080`.

**Пример значения строки подключения ВНУТРИ контейнера API** (не готовый Compose):

```text
Host=db;Port=5432;Database=supportdb;Username=supportuser;Password=<локальный пароль из окружения>
```

`db` — DNS-имя сервиса PostgreSQL в общей сети Compose. `localhost` внутри контейнера API не ведёт к другому контейнеру.

На шаге проверки одиночного контейнера `/health` доступен без PostgreSQL. Проверка `/health/db` и операций с заявками проводится после запуска БД.

## 7. Критерий сохранности

1. Поднимите `docker compose up -d --build`.
2. Создайте обращение, зафиксируйте `id` и данные.
3. Выполните `docker compose down` **без `-v`**.
4. Поднимите стенд снова: `docker compose up -d`.
5. Получите заявку по сохранённому `id`. Поля не должны измениться.

**Границы задания:** не реализовывать новый API, не добавлять UI, не изменять бизнес-логику; выполнить именно контейнеризацию.
