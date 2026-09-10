
# 🚀 TaskFlow: Настройка окружения для C# (.NET 11.0.0-preview.7.26381.103) проекта (Clean Architecture + Database First)

Данная инструкция описывает развертывание PostgreSQL в Docker, инициализацию схемы базы данных и подготовку тестовых данных для последующей разработки системы управления задачами с JWT-аутентификацией.

> *P.s.: Разработка производится в Visual Studio Community 2026 Insiders [12120.281]*
> *возможно некоторые советы по исправлению уже не актуальны!*
>
> *P.s.s.: Дата разработки 06.09.2026 (актуализация 10.09.2026)*

---

## 📋 Предварительные требования

1. Скачайте и установите [Docker Desktop](https://docs.docker.com/desktop/setup/install/windows-install/) (Docker Desktop for Windows - x86_64).
2. Перезагрузите компьютер после установки.

---

## 🐳 Шаг 1: Установка PostgreSQL в Docker

1. Откройте **PowerShell** и скачайте официальный образ PostgreSQL:

```powershell
docker pull postgres:16-alpine
```

2. Убедитесь, что образ успешно скачался:
```powershell
docker images
```
![Результат команды docker ps](Docs/Screenshots/docker-images.png)

> 💡 Почему именно версия alpine?
>
> Alpine — это минималистичный Linux-дистрибутив, который идеально подходит для локальной разработки:
>  * **Размер (главная причина)**: postgres:16-alpine весит ~50-100 MB, тогда как обычный postgres:16 — ~300-400 MB.
>  * **Безопасность**: Меньше пакетов = меньше потенциальных уязвимостей. Alpine создан с фокусом на безопасность.
>  * **Производительность**: Потребляет меньше ресурсов и быстрее запускается.
>  * **Для разработки**: Внутри контейнера не нужны лишние инструменты, так как подключение к БД происходит снаружи (через DBeaver/pgAdmin).

3. Создайте и запустите новый контейнер:

```powershell
docker run --name taskflow-db `
-e POSTGRES_USER=postgres `
-e POSTGRES_PASSWORD=StrongP@ssw0rdHere `
-e POSTGRES_DB=TaskFlow `
-e TZ=Europe/Moscow `
-p 5433:5432 `
-v taskflow_data:/var/lib/postgresql/data `
-d postgres:16-alpine
```

4. Проверьте, что контейнер успешно запущен:

```powershell
docker ps
```

Ожидаемый результат:

![Результат команды docker ps](Docs/Screenshots/docker-ps.png)

---

## ⚠️ Шаг 2: Решение возможных проблем с запуском Docker

ВАЖНО! Если после перезагрузки компьютера Docker не стартует или долго висит в статусе `Docker Desktop is starting...`, выполните следующие действия:

1. Полностью закройте Docker Desktop.
2. Нажмите `Win + Q` и введите в поиске: **Безопасность Windows**.
3. В левом меню выберите **Управление приложениями и браузером**.
4. Внизу страницы нажмите на ссылку **Защита от эксплойтов**.
5. Перейдите на вкладку **Параметры программ**.
6. Найдите в списке `C:\Windows\System32\VmCompute.exe` и нажмите кнопку [Изменить].
7. Найдите блок `Защита потока управления (CFG)` и снимите галочку ✔ с главного пункта "Переопределить системные параметры".

![Окно "Защита от эксплойтов" в настройках Windows ps](Docs/Screenshots/exploit-protection.png)

8. Сохраните изменения и запустите Docker Desktop заново.

---

## 💻 Шаг 3: Установка и настройка DBeaver

1. Скачайте DBeaver Community с официального сайта [DBeaver Community](https://dbeaver.io/download/) (Download EXE).

![Окно "Скачивания DBeaver Community"](Docs/Screenshots/download-dbeaver-Community.png)

2. Установите программу и перезагрузите компьютер (если потребуется).
3. Запустите DBeaver. На верхней панели выберите: База данных → Новое соединение (или нажмите `Ctrl + Shift + N`).

![Окно создания нового соединения в DBeaver](Docs/Screenshots/dbeaver-New-Connection.png)

4. В открывшемся окне выберите PostgreSQL и нажмите [Далее].

![Окно создания нового соединения в DBeaver выбор БД](Docs/Screenshots/dbeaver-New-Connection-PostgreSQL.png)

5. Настройте параметры подключения к нашей базе данных:
> * **Хост (Host):** localhost
> * **Порт (Port):** 5433
> * **База данных (Database):** TaskFlow (или postgres для первичного входа)
> * **Имя пользователя (Username):** postgres
> * **Пароль (Password):** StrongP@ssw0rdHere

![Окно с заполненными полями](Docs/Screenshots/dbeaver-connection-settings.png)

6. Нажмите кнопку **[Test Connection...]**. Вы должны получить сообщение об успешном подключении:

![Окно с заполненными полями](Docs/Screenshots/dbeaver-success.png)

---

## 🗄️ Шаг 4: Создание схемы базы данных (Database First)

Поскольку мы используем подход **Database First**, сначала мы создаем структуру таблиц в PostgreSQL, а затем сгенерируем C#-классы с помощью EF Core.

1. Откройте **DBeaver**, подключитесь к базе данных `TaskFlow` (порт `5433`).
2. Создайте новый SQL-скрипт (`Ctrl + ]`) или правая кнопка мыши по соединению → SQL Editor → New SQL Script.
3. Выполните следующий скрипт для создания таблиц ролей, пользователей и токенов:

![Окно создания нового скрипта в DBeaver](Docs/Screenshots/Dbeaver-New-Sql-Script.png)

3. Выполните следующий скрипт для создания таблиц пользователей и токенов:
```sql
-- 1. Таблица ролей
CREATE TABLE public."Roles" (
    "Id" SERIAL PRIMARY KEY,
    "Name" VARCHAR(50) NOT NULL UNIQUE,
    "Description" VARCHAR(255),
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 2. Таблица пользователей
CREATE TABLE public."Users" (
    "Id" SERIAL PRIMARY KEY,
    "Username" VARCHAR(50) NOT NULL UNIQUE,
    "Email" VARCHAR(100) NOT NULL UNIQUE,
    "PasswordHash" VARCHAR(255) NOT NULL,
    "RoleId" INT NOT NULL,
    "MustChangePassword" BOOLEAN NOT NULL DEFAULT false,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT "FK_Users_Roles_RoleId" 
        FOREIGN KEY ("RoleId") 
        REFERENCES public."Roles"("Id") 
        ON DELETE RESTRICT
);

-- 3. Добавляем базовые роли
INSERT INTO public."Roles" ("Name", "Description") 
VALUES 
('Admin', 'Полный доступ к системе'),
('User', 'Стандартный пользователь');

-- 4. Таблица Refresh Tokens
CREATE TABLE public."RefreshTokens" (
    "Id" SERIAL PRIMARY KEY,
    "UserId" INT NOT NULL,
    "Token" VARCHAR(255) NOT NULL UNIQUE,
    "ExpiresAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "IsRevoked" BOOLEAN NOT NULL DEFAULT false,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    
    -- Внешний ключ с каскадным удалением
    CONSTRAINT "FK_RefreshTokens_Users_UserId" 
        FOREIGN KEY ("UserId") 
        REFERENCES public."Users"("Id") 
        ON DELETE CASCADE
);

-- 5. Индексы для производительности
CREATE INDEX "ix_refreshtokens_token" ON public."RefreshTokens"("Token");
CREATE INDEX "ix_refreshtokens_userid" ON public."RefreshTokens"("UserId");
CREATE INDEX "ix_refreshtokens_expiresat" ON public."RefreshTokens"("ExpiresAt");
```
4. После выполнения нажмите кнопку Refresh (F5) в навигаторе баз данных.
5. Убедитесь, что в схеме public появились таблицы Users и RefreshTokens.

![Окно схемы БД "TaskFlow"](Docs/Screenshots/Dbeaver-Schema-Tables.png)

> 💡 **Почему таблица `Roles` вместо строковой колонки?**
> Enum или строка для ролей — это антипаттерн в production. Таблица позволяет:
>  * Добавлять новые роли без перекомпиляции приложения
>  * Редактировать названия ролей через админку
>  * Хранить описания ролей
>  * В будущем легко расширить до системы прав (`RolePermissions`)

4. После выполнения нажмите кнопку **Refresh (F5)** в навигаторе баз данных.
5. Убедитесь, что в схеме `public` появились таблицы `Roles`, `Users` и `RefreshTokens`.

---

## 🔐 Шаг 5: Создание тестового пользователя (Admin)

Для удобства тестирования JWT-аутентификации создадим тестового администратора. Мы используем алгоритм **PBKDF2**, встроенный в .NET, чтобы избежать лишних зависимостей.

1. Откройте SQL Editor в DBeaver.
2. Выполните скрипт для создания пользователя:

```sql
-- Создаем тестового пользователя admin
-- Пароль: Admin123 
-- Формат хеша: итерации:base64_соль:base64_хеш (PBKDF2-HMAC-SHA256, 100k итераций)
INSERT INTO public."Users" ("Username", "Email", "PasswordHash", "RoleId", "MustChangePassword")
VALUES (
    'admin',
    'admin@taskflow.local',
    '100000:NDQyY34PFV8T9l5WOcItfQ==:POxpZySDaIgiYHCy8qtVUZnUZeEFAT26H3VWtkJpwYU=',
    1,
    false
);

-- Проверяем результат
SELECT u."Id", u."Username", u."Email", r."Name" AS "RoleName", u."MustChangePassword"
FROM public."Users" u
JOIN public."Roles" r ON u."RoleId" = r."Id"
WHERE u."Username" = 'admin';
```
![Окно схемы БД "TaskFlow"](Docs/Screenshots/Dbeaver-Admin-Created.png)

> 💡 Почему такой формат хеша?
> Мы не используем сторонние пакеты (как BCrypt), а берем встроенный в .NET `Rfc2898DeriveBytes` (PBKDF2).
> Формат `100000:salt:hash` позволяет нам хранить все необходимые параметры для проверки пароля в одной строке БД.
> Позже мы напишем класс `PasswordHasher` в слое Infrastructure, который будет генерировать и проверять такие строки.

---

## 🏗️ Шаг 6: Инициализация структуры решения (Clean Architecture)

После настройки БД мы создаем структуру решения в Visual Studio, используя **.NET 11 Preview**. 

1. **Создание проектов**
   В решении `TaskFlow` создаются 4 проекта типа **Class Library** (кроме **WebAPI**):
   * `TaskFlow.Domain` (Ядро, без зависимостей)
   * `TaskFlow.Application` (Бизнес-логика и интерфейсы)
   * `TaskFlow.Infrastructure` (Реализация интерфейсов, работа с БД)
   * `TaskFlow.WebAPI` (Точка входа, контроллеры, настройки)
![Окно создания Web.API](Docs/Screenshots/csharp_web_api_project.png)

> ⚠️ **Важно:** При создании всех проектов необходимо явно выбрать одну и ту же целевую платформу (например, `.NET 11.0 Preview`), чтобы избежать конфликтов версий при сборке.
>

![Окно создания выбора версии .net](Docs/Screenshots/csharp_class_library_version.png)

2. **Очистка от шаблонов**
   Сразу после создания удаляем мусор, сгенерированный Visual Studio:
   * В `TaskFlow.WebAPI`: удалить `WeatherForecast.cs` и `WeatherForecastController.cs`.
   * В `Domain`, `Application`, `Infrastructure`: удалить `Class1.cs`.

3. **Настройка зависимостей (Правило направленных внутрь связей)**
   Ссылки между проектами добавляются строго в одном направлении:
   * `Application` ➔ ссылается на `Domain`
   * `Infrastructure` ➔ ссылается на `Domain` и `Application`
   * `WebAPI` ➔ ссылается на `Application` и `Infrastructure`
   * `Domain` ➔ **ни на что не ссылается** (абсолютно независим)

После этих действий решение должно успешно собираться (`Build: 4 succeeded, 0 failed`), несмотря на использование Preview-версии .NET.

---

## 📦 Шаг 7: Установка NuGet-пакетов для EF Core

Чтобы Entity Framework Core мог подключиться к нашей PostgreSQL и сгенерировать C#-код, нам нужно установить необходимые пакеты. Мы делаем это в соответствии с правилами Clean Architecture.

1. В Visual Studio откройте **Консоль диспетчера пакетов** (Средства → Диспетчер пакетов NuGet → Консоль диспетчера пакетов).
![Окно PMC:](Docs/Screenshots/csharp_tools_nuget_pmc.png)
2. В выпадающем списке **Проект по умолчанию** (Default project) выберите **TaskFlow.Infrastructure** и выполните команды:
![Окно выбора проекта по умолчанию:](Docs/Screenshots/csharp_tools_change_default_project.png)

```powershell
Install-Package Npgsql.EntityFrameworkCore.PostgreSQL
Install-Package Microsoft.EntityFrameworkCore.Design
```
3.  Смените **Проект по умолчанию** на **TaskFlow.WebAPI** и выполните:
```powershell
Install-Package Microsoft.EntityFrameworkCore.Tools
```

> 💡 Почему именно такое распределение?
> * **Infrastructure**: Здесь будет жить `DbContext` и провайдер базы данных (`Npgsql`). Это слой, отвечающий за внешние зависимости.
> * **WebAPI**: Пакет `Tools` нужен для запуска команд генерации кода (Scaffold) из консоли, поэтому он должен быть установлен в стартовом (запускаемом) проекте.
> * **Domain и Application**: Остаются абсолютно чистыми! Мы не тянем зависимости от EF Core в ядро проекта.

4. Нажмите **Сборка → Пересобрать решение** (Rebuild Solution), чтобы убедиться, что все пакеты корректно интегрировались и конфликтов версий нет (`Build: 4 succeeded, 0 failed`).

---

## 🔄 Шаг 8: Генерация кода из БД (Database First)

> ⚠️ **ВАЖНО:** Перед выполнением этого шага убедитесь, что Docker-контейнер с PostgreSQL запущен!
> Проверьте командой: `docker ps`
> Если контейнер `taskflow-db` не отображается в списке, запустите его:
> ```powershell
> docker start taskflow-db
> ```
> Без работающего контейнера команда `Scaffold-DbContext` не сможет подключиться к базе данных и завершится с ошибкой.

Теперь, когда пакеты установлены, мы используем Entity Framework Core для автоматической генерации C#-классов на основе нашей схемы PostgreSQL.

1. Откройте **Консоль диспетчера пакетов** (Средства → Диспетчер пакетов NuGet → Консоль диспетчера пакетов).
2. В списке **Проект по умолчанию** выберите `TaskFlow.Infrastructure`.
3. Выполните следующую команду:

```powershell
Scaffold-DbContext "Host=localhost;Port=5433;Database=TaskFlow;Username=postgres;Password=StrongP@ssw0rdHere" Npgsql.EntityFrameworkCore.PostgreSQL -OutputDir Entities -Context AppDbContext -Project TaskFlow.Infrastructure -StartupProject TaskFlow.WebAPI -Force
```

**Разбор параметров команды:**
* `Host=...` — строка подключения к нашему Docker-контейнеру.
* `-OutputDir Entities` — указывает папку для генерации сущностей.
* `-Context AppDbContext` — задает имя для главного класса контекста базы данных.
* `-Project TaskFlow.Infrastructure` — проект, куда будут добавлены файлы.
* `-StartupProject TaskFlow.WebAPI` — проект запуска (нужен EF Core для чтения конфигураций).
* `-Force` — разрешает перезапись файлов при повторном запуске.

После выполнения в проекте `TaskFlow.Infrastructure` появится папка `Entities` с файлами:
* `AppDbContext.cs`
* `User.cs`
* `RefreshToken.cs`
* `Role.cs`

> ⚠️ **Важное примечание по Clean Architecture:** По умолчанию EF Core генерирует всё в указанный проект. Однако по правилам Clean Architecture, сущности (`User`, `RefreshToken`, `Role`) должны находиться в слое `Domain`, а `AppDbContext` — в `Infrastructure`. На следующем шаге мы проведем рефакторинг и разнесем эти файлы по правильным слоям.

---

## 🏗️ Шаг 9: Рефакторинг сгенерированного кода и настройка подключения к БД

По умолчанию EF Core генерирует весь код в один проект. Чтобы соблюсти правила Clean Architecture, мы разнесем сущности и контекст по правильным слоям, а строку подключения вынесем в конфигурацию.

### 1. Распределение файлов по слоям

* В проекте `TaskFlow.Domain` создайте папку `Entities` и переместите туда файлы `User.cs`, `RefreshToken.cs` и `Role.cs`.
* В проекте `TaskFlow.Infrastructure` создайте папку `Context` и переместите туда файл `AppDbContext.cs`. Старую папку `Entities` в Infrastructure можно удалить.
* Исправьте пространства имен (`namespace`) в перемещенных файлах:
  * В `User.cs`, `RefreshToken.cs` и `Role.cs`: `namespace TaskFlow.Domain.Entities;`
   * В `AppDbContext.cs`: `namespace TaskFlow.Infrastructure.Context;`
* Добавьте `using TaskFlow.Domain.Entities;` в начало файла `AppDbContext.cs`, чтобы он увидел сущности из другого проекта.

### 2. Вынос строки подключения (Dependency Injection)

Хардкодить строку подключения внутри `AppDbContext` — это антипаттерн. Мы вынесем её во внешний слой (`WebAPI`).

* Откройте `AppDbContext.cs` и полностью удалите сгенерированный метод `OnConfiguring`.
* Откройте `appsettings.json` в проекте `TaskFlow.WebAPI` и добавьте секцию `ConnectionStrings`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=TaskFlow;Username=postgres;Password=StrongP@ssw0rdHere"
  }
}
```

### 3.  Откройте `Program.cs` в проекте **TaskFlow.WebAPI**. Добавьте необходимые `using` в начало файла:
```csharp
using Microsoft.EntityFrameworkCore;
using TaskFlow.Infrastructure.Context;
```

### 4.  Зарегистрируйте `AppDbContext` в контейнере зависимостей (перед строкой `var app = builder.Build();`):

```csharp
// Регистрация DbContext с использованием строки из appsettings.json
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
```

> 💡 Почему именно так?
> * **Слабая связанность**: Слой `Infrastructure` больше ничего не знает о строке подключения или файлах конфигурации. Он просто получает готовый `DbContext` через конструктор.
> * **Безопасность**: Строка подключения хранится в `appsettings.json` (который, кстати, часто добавляют в `.gitignore` для production-версий, оставляя только безопасные шаблоны).
> * **Гибкость**: При необходимости вы сможете легко подменить реализацию БД или использовать разные строки для тестов и продакшена.

---

## 🔐 Шаг 10: Создание AuthController и тестирование JWT

> ⚠️ **ВАЖНО:** Перед запуском проекта убедитесь, что Docker-контейнер с PostgreSQL запущен!
> Проверьте командой: `docker ps`
> Если контейнер `taskflow-db` не отображается в списке, запустите его:
> ```powershell
> docker start taskflow-db
> ```
> Без работающего контейнера приложение не сможет подключиться к базе данных и выдаст ошибку при попытке аутентификации.

Финальный этап: создаем точку входа (API) для аутентификации и проверяем выдачу токена.

### 1. Создание контроллера

> 💡 В версии `.NET 11.0.0-preview.7.26381.103` нет возможности добавить контроллер как в предыдущих версиях через `Add => Controller`. При попытке добавить контроллер таким образом, вы увидите сообщение `Scaffolding is not supported for .NET 11 or later projects.`
> По этому добавление контроллера производится через локальное меню в `Solution Explorer` (Правой кнопкой мыши вызовите локальное меню папки `Controllers` выберите пункт `Add` далее `New Item`)
> В открывшемся списке найдите `API Controller - Empty` и измените имя контроллера на: `AuthController.cs`

После создание вставьте данный код:

```csharp
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Contracts.Authentication;
using TaskFlow.Application.Interfaces;

namespace TaskFlow.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    private readonly IAuthService _authService = authService;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var response = await _authService.LoginAsync(request);
        if (response is null)
        {
            return Unauthorized(new { message = "Неверное имя пользователя или пароль" });
        }
        return Ok(response);
    }
}
```

> ⚠️ **Критически важно:** Убедитесь, что подключен именно ваш `using TaskFlow.Application.Contracts.Authentication;`. Visual Studio по ошибке может предложить `using Microsoft.AspNetCore.Identity.Data;`, что приведет к конфликту типов `LoginRequest` и ошибкам компиляции.

### 2. Тестирование API (рекомендуется Bruno)

Для тестирования мы используем Bruno (или Postman/Insomnia), так как он легче и надежнее встроенного Swagger в .NET Preview-версиях.

1. Запустите проект `TaskFlow.WebAPI` (F5).
2.  Откройте Bruno и создайте новый запрос:
   * Метод: `POST`
   * URL: `https://localhost:7053/api/Auth/login` (порт может отличаться, проверьте в свойствах проекта)
   * Body: `application/json`

**✅ Тест 1: Успешная аутентификация**

Request Body:
```json
{
  "username": "admin",
  "password": "Admin123"
}
```

Expected Response (200 OK):
```json
{
  "id": 1,
  "username": "admin",
  "email": "admin@taskflow.local",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "refreshToken": "...",
  "mustChangePassword": false
}
```

**❌ Тест 2: Неверный логин или пароль**

Request Body:
```json
{
  "username": "admi",
  "password": "Admin123"
}
```

Expected Response (401 Unauthorized):
```json
{
  "message": "Неверное имя пользователя или пароль"
}
```

> 💡 **Итог:** Мы получили полностью рабочий, архитектурно правильный скелет Clean Architecture с JWT-аутентификацией. Слой `Domain` абсолютно чист, `Infrastructure` инкапсулирует логику БД и хеширования, а `WebAPI` управляет маршрутизацией и внедрением зависимостей (DI).

### 3. Альтернатива: Тестирование через .http-файл в Visual Studio

Если у вас нет возможности использовать Bruno (или вы предпочитаете не выходить из IDE), Visual Studio имеет встроенный REST-клиент для работы с файлами `.http` или `.rest`.

**Преимущества:**
* ✅ Тесты живут прямо в репозитории (можно коммитить в Git)
* ✅ Не нужны внешние инструменты (Postman, Bruno, Insomnia)
* ✅ Любой разработчик, склонировавший проект, может сразу тестировать API
* ✅ Работает оффлайн и не зависит от Swagger

**Как использовать:**
1. В проекте `TaskFlow.WebAPI` откройте файл `TaskFlow.WebAPI.http` (создается автоматически при создании проекта Web API).
2. Замените его содержимое на следующий код:

```http
@TaskFlow.WebAPI_HostAddress = https://localhost:7053

### Тест 1: Успешная аутентификация
POST {{TaskFlow.WebAPI_HostAddress}}/api/Auth/login
Accept: application/json
Content-Type: application/json

{
  "username": "admin",
  "password": "Admin123"
}

### Тест 2: Неверный логин или пароль
POST {{TaskFlow.WebAPI_HostAddress}}/api/Auth/login
Accept: application/json
Content-Type: application/json

{
  "username": "admi",
  "password": "Admin123"
}
```

> 💡 Как это работает:
> * `@TaskFlow.WebAPI_HostAddress` — это переменная, которую можно использовать во всех запросах (удобно менять порт в одном месте).
> * `###` — разделитель между запросами.
> * Рядом с каждым запросом появляется зеленая стрелочка ▶ — нажмите на неё, чтобы выполнить запрос.
> * Ответ появится в правой части окна Visual Studio.

3.  Запустите проект (`F5`) и нажмите на зеленую стрелочку рядом с первым запросом.
4.  Вы увидите ответ `200 OK` с JWT-токеном прямо в Visual Studio!

![Тестирование через http-файл:](Docs/Screenshots/csharp_testing_endpoints_by_http.png)

> ⚠️ **Важно:** Убедитесь, что порт в переменной `@TaskFlow.WebAPI_HostAddress` совпадает с портом вашего запущенного проекта (проверьте в `launchSettings.json` или в свойствах проекта).

---

## 🛡️ Шаг 11: Глобальная обработка исключений и профессиональное логирование

Чтобы приложение не "падало" с непонятными ошибками и не спамило консоль, если база данных (Docker) выключена, мы добавили глобальный Middleware. Он перехватывает ошибку подключения, возвращает клиенту понятный JSON-ответ (статус `503 Service Unavailable`) и записывает полный стектрейс в лог-файл для разработчика.

Мы используем нативное логирование без сторонних библиотек (вроде Serilog), разделяя логи на два типа для удобства:
* `Logs/AppLog/` — чистая история жизни приложения (SQL-запросы, инфо, ворнинги).
* `Logs/Errors/` — критические ошибки с полными стектрейсами и разделителями.

### 1. Настройка логирования в `appsettings.json`

Откройте `appsettings.json` в проекте `TaskFlow.WebAPI` и обновите секции `Logging`, а также добавьте `LoggingConfig` с разделением папок:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning",
      "Microsoft.EntityFrameworkCore.Database.Connection": "None",
      "Microsoft.EntityFrameworkCore.Database.Command": "None"
    }
  },
  "LoggingConfig": {
    "AppLogDirectory": "Logs/AppLog",
    "ErrorLogDirectory": "Logs/Errors"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5433;Database=TaskFlow;Username=postgres;Password=StrongP@ssw0rdHere"
  },
  "JwtSettings": {
    "SecretKey": "SuperSecretKeyForTaskFlowProject2026!@#",
    "Issuer": "TaskFlowAPI",
    "Audience": "TaskFlowClient",
    "AccessTokenExpirationMinutes": 15,
    "RefreshTokenExpirationDays": 7
  }
}
```

> 💡 Почему именно так? Мы глушим спам от EF Core (`"None"`), так как сами обработаем ошибку подключения, и указываем папки для логов. Middleware будет автоматически создавать новый файл с текущей датой и временем (например, `taskflow-errors_2026-09-07_05-07-00.log`), что предотвращает разрастание одного огромного файла.

### 2. Создание нативного файлового логгера (`NativeFileLogger.cs`)

В проекте `TaskFlow.WebAPI` создайте папку `Logging`, а в ней файл `NativeFileLogger.cs`. Это легкий, потокобезопасный логгер, который пишет в файл ровно то же, что вы видите в консоли, с пустыми строками для читаемости.

```csharp
using Microsoft.Extensions.Logging;
using System.IO;

namespace TaskFlow.WebAPI.Logging;

// Провайдер, который создает логгеры
public class NativeFileLoggerProvider : ILoggerProvider
{
    private readonly string _filePath;
    public NativeFileLoggerProvider(string filePath) => _filePath = filePath;
    public ILogger CreateLogger(string categoryName) => new NativeFileLogger(_filePath, categoryName);
    public void Dispose() { }
}

// Сам логгер, который пишет в файл
public class NativeFileLogger : ILogger
{
    private readonly string _filePath;
    private readonly string _category;
    private static readonly object _lock = new object();

    public NativeFileLogger(string filePath, string category)
    {
        _filePath = filePath;
        _category = category;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel)) return;

        var message = formatter(state, exception);
        if (string.IsNullOrEmpty(message) && exception == null) return;

        // Блокируем поток, чтобы несколько одновременных запросов не испортили файл
        lock (_lock)
        {
            try
            {
                var logDir = Path.GetDirectoryName(_filePath);
                if (!string.IsNullOrEmpty(logDir)) Directory.CreateDirectory(logDir);

                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{logLevel,-13}] [{_category}]\n      {message}\n";
                File.AppendAllText(_filePath, logEntry);

                if (exception != null)
                {
                    File.AppendAllText(_filePath, $"      {exception}\n");
                }

                // Пустая строка для разделения записей и улучшения читаемости
                File.AppendAllText(_filePath, "\n");
            }
            catch
            {
                // Если не удалось записать в файл, мы не должны ронять всё приложение
            }
        }
    }
}
```

### 3. Создание Middleware

В проекте `TaskFlow.WebAPI` создайте папку `Middleware`, а в ней файл `ExceptionHandlingMiddleware.cs`:

```csharp
using System.Data.Common;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using TaskFlow.Application.Exceptions;

namespace TaskFlow.WebAPI.Middleware;

public class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger,
    IConfiguration configuration,
    IWebHostEnvironment env)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (Exception ex) { await HandleExceptionAsync(context, ex); }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // <-- НОВОЕ: Обработка бизнес-исключений (400 Bad Request)
        if (exception is BusinessException businessEx)
        {
            logger.LogWarning("⚠️ Бизнес-ошибка: {Message}", businessEx.Message);
            context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                title = "Ошибка валидации",
                detail = businessEx.Message,
                status = 400
            }));
            return;
        }

        bool isDbConnectionError = exception is DbException ||
                                   exception is SocketException ||
                                   (exception is InvalidOperationException && exception.InnerException is SocketException) ||
                                   exception.Message.Contains("Подключение не установлено", StringComparison.OrdinalIgnoreCase) ||
                                   exception.Message.Contains("Connection refused", StringComparison.OrdinalIgnoreCase) ||
                                   exception.Message.Contains("failed to connect", StringComparison.OrdinalIgnoreCase) ||
                                   (exception is InvalidOperationException inv && inv.Message.Contains("transient failure", StringComparison.OrdinalIgnoreCase));

        if (isDbConnectionError)
        {
            logger.LogWarning("⚠️ Ошибка подключения к БД: {Message}. Возможно, не запущен Docker-контейнер.", exception.Message);

            try
            {
                // 1. Берем папку для ошибок из конфига (по умолчанию "Logs/Errors")
                var errorLogDirectory = configuration["LoggingConfig:ErrorLogDirectory"] ?? "Logs/Errors";

                // 2. Формируем имя файла с датой и временем: taskflow-errors_2026-09-07_05-07-00.log
                // Формат yyyy-MM-dd_HH-mm-ss обеспечивает хронологическую сортировку в проводнике
                var fileName = $"taskflow-errors_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log";
                var logFullPath = Path.Combine(env.ContentRootPath, errorLogDirectory, fileName);

                // 3. Создаем папку, если её нет
                var logDir = Path.GetDirectoryName(logFullPath);
                if (!string.IsNullOrEmpty(logDir))
                {
                    Directory.CreateDirectory(logDir);
                }

                // 4. Формируем читаемую запись с длинным разделителем (80 символов)
                var separator = new string('-', 80);
                var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] DB Connection Error:\n{exception}\n{separator}\n\n";

                // 5. Дописываем в файл (на случай, если за одну секунду произойдет несколько ошибок)
                File.AppendAllText(logFullPath, logEntry);
            }
            catch (Exception fileEx)
            {
                logger.LogError(fileEx, "Не удалось записать ошибку в лог-файл");
            }

            context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(JsonSerializer.Serialize(new
            {
                title = "База данных недоступна",
                detail = "Не удалось подключиться к PostgreSQL. Убедитесь, что Docker-контейнер 'taskflow-db' запущен (команда: docker ps).",
                status = 503
            }));
            return;
        }

        logger.LogError(exception, "⚠ Необработанное исключение: {Message}", exception.Message);
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            title = "Внутренняя ошибка сервера",
            detail = "Произошла непредвиденная ошибка. Проверьте логи.",
            status = 500
        }));
    }
}
```

> 💡 **Почему такой формат имени файла?**
>
> - `yyyy-MM-dd_HH-mm-ss` — обеспечивает хронологическую сортировку в проводнике Windows (файлы стоят ровно по порядку создания).
> - Двоеточия `:` заменены на дефисы `-`, так как двоеточия запрещены в именах файлов Windows.
> - 24-часовой формат `HH` исключает путаницу между AM/PM.
> - Имя начинается с `taskflow-errors_`, что упрощает поиск и фильтрацию логов.

### 4. Регистрация в `Program.cs`

Откройте `Program.cs` в проекте `TaskFlow.WebAPI`.

Добавьте `using` в начало файла:

```csharp
using TaskFlow.WebAPI.Middleware;
using TaskFlow.WebAPI.Logging; // <-- Добавлено для NativeFileLogger
```

Добавьте строку `Console.OutputEncoding = System.Text.Encoding.UTF8;` в самое начало файла (чтобы эмодзи в консоли отображались корректно).

Настройте нативное логирование сразу после `var builder = WebApplication.CreateBuilder(args);`:

```csharp
Console.OutputEncoding = System.Text.Encoding.UTF8; // Для корректного отображения эмодзи

var builder = WebApplication.CreateBuilder(args);

// --- НАСТРОЙКА НАТИВНОГО ЛОГИРОВАНИЯ ---
var appLogDirectory = builder.Configuration["LoggingConfig:AppLogDirectory"] ?? "Logs/AppLog";
var logFileName = $"app-log_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log";
var logFullPath = Path.Combine(builder.Environment.ContentRootPath, appLogDirectory, logFileName);

builder.Logging.ClearProviders(); // Убираем стандартный шум
builder.Logging.AddConsole();     // Оставляем вывод в консоль
builder.Logging.AddProvider(new NativeFileLoggerProvider(logFullPath)); // Добавляем наш файловый логгер
// ---------------------------------------

builder.Services.AddControllers();
// ... (остальная регистрация сервисов: DbContext, JWT и т.д.)
```

Зарегистрируйте Middleware сразу после `var app = builder.Build();` (это критически важно, чтобы он перехватывал ошибки от всех последующих компонентов):

```csharp
var app = builder.Build();

// Глобальная обработка исключений (СТРОГО первой!)
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
```

### 5. Результат

В папке `Logs` теперь идеальная структура:

- `Logs/AppLog/app-log_2026-09-07_05-06-30.log` — чистая история жизни приложения (SQL-запросы, инфо, ворнинги) с пустыми строками для читаемости.
- `Logs/Errors/taskflow-errors_2026-09-07_05-07-00.log` — критические ошибки с полными стектрейсами и длинными разделителями.

> 💡 **Итог:** Enterprise-уровень логирования и обработки ошибок, 0 сторонних NuGet-пакетов, полная читаемость и контроль.

### 6. Тестирование "Защиты от дурака"

1. Остановите контейнер: `docker stop taskflow-db`
2. Запустите проект (`Ctrl + F5`).
3. Отправьте запрос на логин через `.http` файл или Bruno.

**Ожидаемый результат:**

В ответе вы получите чистый JSON со статусом `503`:

```json
{
       "title": "База данных недоступна",
       "detail": "Не удалось подключиться к PostgreSQL. Убедитесь, что Docker-контейнер 'taskflow-db' запущен (команда: docker ps).",
       "status": 503
}
```

- В консоли будет только одна чистая строка с предупреждением `⚠️`.
- В папке `Logs/Errors/` появится файл `taskflow-errors_2026-09-07_HH-mm-ss.log` с полным стектрейсом для отладки.

---

## 🔐 Шаг 12: Реализация Refresh Token, Logout и ротация токенов

Чтобы пользователю не приходилось вводить логин/пароль каждые 15 минут (когда истекает Access Token), мы реализуем механизм Refresh Token с ротацией. Также мы добавим эндпоинт `Logout` и автоматический отзыв старых токенов при входе для обеспечения "одной активной сессии на пользователя".

### 1. Создание DTO для Refresh Token

В проекте `TaskFlow.Application` в папке `Contracts/Authentication` создайте файл `RefreshTokenRequest.cs`:

```csharp
namespace TaskFlow.Application.Contracts.Authentication;

public record RefreshTokenRequest(string RefreshToken);
```

### 2. Обновление AuthResponse

Откройте `AuthResponse.cs` и добавьте поля `RefreshToken` и `MustChangePassword`:

```csharp
namespace TaskFlow.Application.Contracts.Authentication;

public record AuthResponse(
    int Id, 
    string Username, 
    string Email, 
    string Token,
    string RefreshToken,
    bool MustChangePassword
);
```

### 3. Обновление интерфейса IAuthService

Добавьте новые методы в `IAuthService.cs`:

```csharp
using TaskFlow.Application.Contracts.Authentication;

namespace TaskFlow.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse?> LoginAsync(LoginRequest request);
    Task<AuthResponse?> RefreshTokenAsync(RefreshTokenRequest request);
    Task LogoutAsync(int userId, string refreshToken);
    Task<bool> CreateUserAsync(CreateUserRequest request);
    Task ChangePasswordAsync(int userId, ChangePasswordRequest request);
}
```

### 4. Реализация AuthService с ротацией и очисткой токенов

Полностью замените содержимое `AuthService.cs` в проекте `TaskFlow.Infrastructure/Services`:

```csharp
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Contracts.Authentication;
using TaskFlow.Application.Contracts.Users;
using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces;
using TaskFlow.Application.Settings;
using TaskFlow.Application.Validators;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Context;
using Microsoft.Extensions.Options;

namespace TaskFlow.Infrastructure.Services;

public class AuthService(
    AppDbContext _context,
    IPasswordHasher _passwordHasher,
    IJwtTokenGenerator _jwtTokenGenerator,
    IOptions<JwtSettings> jwtOptions) : IAuthService
{
    private readonly JwtSettings _jwtSettings = jwtOptions.Value;

    public async Task<AuthResponse?> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user is null) return null;

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            return null;

        // Отзываем ВСЕ старые активные токены этого пользователя (soft delete)
        await RevokeAllUserTokensAsync(user.Id);

        var accessToken = _jwtTokenGenerator.GenerateToken(user);
        var refreshToken = await GenerateAndSaveRefreshTokenAsync(user.Id);

        await _context.SaveChangesAsync();

        return new AuthResponse(
            user.Id, 
            user.Username, 
            user.Email, 
            accessToken, 
            refreshToken,
            user.MustChangePassword
        );
    }

    public async Task<AuthResponse?> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var storedToken = await _context.RefreshTokens
            .Include(rt => rt.User)
            .ThenInclude(u => u.Role)
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

        if (storedToken is null || storedToken.IsRevoked || storedToken.ExpiresAt < DateTime.UtcNow)
            return null;

        storedToken.IsRevoked = true;

        var newAccessToken = _jwtTokenGenerator.GenerateToken(storedToken.User);
        var newRefreshToken = await GenerateAndSaveRefreshTokenAsync(storedToken.UserId);

        await _context.SaveChangesAsync();

        return new AuthResponse(
            storedToken.User.Id,
            storedToken.User.Username,
            storedToken.User.Email,
            newAccessToken,
            newRefreshToken,
            storedToken.User.MustChangePassword
        );
    }

    public async Task LogoutAsync(int userId, string refreshToken)
    {
        // При выходе отзываем ВСЕ активные токены этого пользователя
        await RevokeAllUserTokensAsync(userId);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> CreateUserAsync(CreateUserRequest request)
    {
        // 1. Валидация сложности пароля
        PasswordValidator.Validate(request.Password);

        // 2. Проверка уникальности с понятными ошибками
        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            throw new BusinessException("Пользователь с таким именем уже существует");

        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            throw new BusinessException("Пользователь с таким email уже существует");

        // 3. Ищем роль в БД по имени
        var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name == request.Role);
        if (role == null)
            throw new BusinessException($"Роль '{request.Role}' не найдена в системе. Доступны: Admin, User");

        // 4. Создаём сущность пользователя
        var newUser = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            RoleId = role.Id,
            MustChangePassword = true // Пароль считается временным
        };

        // 5. Сохраняем с дополнительной защитой от UNIQUE constraint
        try
        {
            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("23505") == true || ex.InnerException?.Message.Contains("unique") == true)
        {
            throw new BusinessException("Нарушение уникальности: имя пользователя или email уже заняты.");
        }

        return true;
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request)
    {
        // 1. Базовая проверка совпадения
        if (request.NewPassword != request.ConfirmPassword)
            throw new BusinessException("Новый пароль и подтверждение не совпадают");

        // 2. Проверяем сложность нового пароля
        PasswordValidator.Validate(request.NewPassword);

        // 3. Ищем пользователя
        var user = await _context.Users.FindAsync(userId);
        if (user is null)
            throw new BusinessException("Пользователь не найден");

        // 4. Проверяем старый пароль
        if (!_passwordHasher.Verify(request.OldPassword, user.PasswordHash))
            throw new BusinessException("Текущий пароль введен неверно");

        // 5. Проверяем, что пароли разные
        if (request.OldPassword == request.NewPassword)
            throw new BusinessException("Новый пароль должен отличаться от старого");

        // 6. Обновляем хеш и сбрасываем флаг обязательной смены
        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.MustChangePassword = false;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    // Централизованная очистка всех активных токенов пользователя (DRY-принцип)
    private async Task RevokeAllUserTokensAsync(int userId)
    {
        var activeTokens = await _context.RefreshTokens
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync();

        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
        }
    }

    private async Task<string> GenerateAndSaveRefreshTokenAsync(int userId)
    {
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);

        var refreshToken = Convert.ToBase64String(randomBytes);

        var refreshTokenEntity = new RefreshToken
        {
            UserId = userId,
            Token = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays),
            IsRevoked = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        return refreshToken;
    }
}
```

### 5. Добавление эндпоинтов в AuthController

Откройте `AuthController.cs` и добавьте методы `Refresh`, `Logout` и `ChangePassword`:

```csharp
[HttpPost("refresh")]
public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
{
    var response = await _authService.RefreshTokenAsync(request);
    if (response is null)
        return Unauthorized(new { message = "Недействительный или истекший refresh token" });
    return Ok(response);
}

[Authorize]
[HttpPost("logout")]
public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request)
{
    var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
    if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        return Unauthorized(new { message = "Не удалось определить пользователя" });

    await _authService.LogoutAsync(userId, request.RefreshToken);
    return Ok(new { message = "Выход выполнен успешно!" });
}

[Authorize]
[HttpPost("change-password")]
public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
{
    var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
    if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
        return Unauthorized(new { message = "Не удалось определить пользователя" });

    await _authService.ChangePasswordAsync(userId, request);
    return Ok(new { message = "Пароль успешно изменен" });
}
```

### 6. Тестирование

1. Выполните запрос на `/api/Auth/login` и скопируйте значение `refreshToken`.
2. Отправьте запрос на `/api/Auth/refresh` для обновления токена.
3. Отправьте запрос на `/api/Auth/logout` для выхода.
4. Проверьте в БД: все токены пользователя теперь имеют `IsRevoked = true`.

> 💡 **Почему это безопасно?**
>
> - Мы используем **Refresh Token Rotation**: при каждом обновлении старый Refresh Token помечается как `IsRevoked = true` и создается новый.
> - При каждом `Login` и `Logout` все старые токены автоматически отзываются (принцип DRY).
> - Refresh Token генерируется криптографически стойким методом (`RandomNumberGenerator`).

---

## 🔒 Шаг 13: Проверка защиты эндпоинтов через [Authorize]

Теперь, когда у нас есть токены, убедимся, что защищенные ресурсы действительно недоступны без них.

### 1. Создание тестового защищенного контроллера

В проекте `TaskFlow.WebAPI` создайте `Controllers/TestController.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TaskFlow.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // <-- Этот атрибут требует валидный JWT-токен в заголовке
public class TestController : ControllerBase
{
    [HttpGet("secure-data")]
    public IActionResult GetSecureData()
    {
        return Ok(new { message = "Доступ разрешен! Вы успешно аутентифицированы." });
    }
}
```

### 2. Тестирование в `.http` файле

Добавьте в ваш `TaskFlow.WebAPI.http` следующие запросы:

```http
### Тест 4: Попытка доступа БЕЗ токена (Ожидаем 401 Unauthorized)
GET {{TaskFlow.WebAPI_HostAddress}}/api/Test/secure-data

### Тест 5: Доступ С токеном (Ожидаем 200 OK)
@token = eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...

GET {{TaskFlow.WebAPI_HostAddress}}/api/Test/secure-data
Authorization: Bearer {{token}}
```

> 💡 **Итог:** Система аутентификации, авторизации и логирования полностью готова к использованию в бизнес-логике TaskFlow!

---

## 👑 Шаг 14: Реализация ролей, валидации и управления пользователями

Для полноценной многопользовательской системы нам нужна табличная модель ролей, строгая валидация паролей и возможность создавать новых пользователей через API.

### 1. Обновление сущности User

Откройте `TaskFlow.Domain/Entities/User.cs`. Теперь вместо строковой роли у нас связь через `RoleId`:

```csharp
using System.Collections.Generic;

namespace TaskFlow.Domain.Entities;

public partial class User
{
    public int Id { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    
    // Связь с таблицей Roles
    public int RoleId { get; set; }
    public virtual Role Role { get; set; } = null!;
    
    // Флаг обязательной смены временного пароля
    public bool MustChangePassword { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
```

### 2. Создание сущности Role

Создайте `TaskFlow.Domain/Entities/Role.cs`:

```csharp
using System.Collections.Generic;

namespace TaskFlow.Domain.Entities;

public partial class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
```

### 3. Обновление AppDbContext

Откройте `TaskFlow.Infrastructure/Context/AppDbContext.cs` и настройте связи:

```csharp
using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Context;

public partial class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public virtual DbSet<Role> Roles { get; set; }
    public virtual DbSet<User> Users { get; set; }
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Roles");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Description).HasMaxLength(255);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Username).HasMaxLength(50).IsRequired();
            entity.HasIndex(e => e.Username).IsUnique();
            entity.Property(e => e.Email).HasMaxLength(100).IsRequired();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.PasswordHash).HasMaxLength(255).IsRequired();

            entity.HasOne(e => e.Role)
                  .WithMany(r => r.Users)
                  .HasForeignKey(e => e.RoleId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("RefreshTokens");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).HasMaxLength(255).IsRequired();
            entity.HasIndex(e => e.Token).IsUnique();

            entity.HasOne(e => e.User)
                  .WithMany(u => u.RefreshTokens)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        base.OnModelCreating(modelBuilder);
    }
}
```

### 4. Обновление JwtTokenGenerator

Откройте `TaskFlow.Infrastructure/Services/JwtTokenGenerator.cs` и добавьте Claim с ролью из объекта `Role`:

```csharp
var claims = new[]
{
    new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
    new Claim(JwtRegisteredClaimNames.Email, user.Email),
    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
    new Claim(ClaimTypes.Role, user.Role.Name) // <-- Берём Name из объекта Role
};
```

### 5. Реализация метода Hash в PasswordHasher

Откройте `TaskFlow.Infrastructure/Services/PasswordHasher.cs` и добавьте метод `Hash`:

```csharp
public string Hash(string password)
{
    // 1. Генерируем криптографически стойкую случайную соль
    byte[] salt = new byte[16];
    using var rng = RandomNumberGenerator.Create();
    rng.GetBytes(salt);

    // 2. Генерируем хеш из пароля, соли и итераций
    byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
        Encoding.UTF8.GetBytes(password),
        salt,
        100000,
        HashAlgorithmName.SHA256,
        32
    );

    // 3. Собираем всё в одну строку формата "iterations:salt:hash"
    return $"100000:{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
}
```

### 6. Создание BusinessException

В проекте `TaskFlow.Application` создайте папку `Exceptions` и файл `BusinessException.cs`:

```csharp
namespace TaskFlow.Application.Exceptions;

public class BusinessException : Exception
{
    public BusinessException(string message) : base(message) { }
    public BusinessException(string message, Exception innerException) : base(message, innerException) { }
}
```

> 💡 **Зачем это нужно?** Мы используем `BusinessException` для возврата клиенту понятных ошибок валидации (например, "Email уже используется") со статусом `400 Bad Request`, вместо того чтобы "падать" с `500 Internal Server Error`.

### 7. Создание PasswordValidator

В проекте `TaskFlow.Application` создайте папку `Validators` и файл `PasswordValidator.cs`:

```csharp
using System.Text.RegularExpressions;
using TaskFlow.Application.Exceptions;

namespace TaskFlow.Application.Validators;

public static class PasswordValidator
{
    public static void Validate(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new BusinessException("Пароль не может быть пустым");

        if (password.Length < 8)
            throw new BusinessException("Пароль должен содержать минимум 8 символов");

        if (!Regex.IsMatch(password, "[A-Z]"))
            throw new BusinessException("Пароль должен содержать хотя бы одну заглавную букву (A-Z)");

        if (!Regex.IsMatch(password, "[0-9]") && !Regex.IsMatch(password, "[!@#$%^&*()_+\\-=\\[\\]{};':\"\\\\|,.<>\\/?]"))
            throw new BusinessException("Пароль должен содержать хотя бы одну цифру (0-9) или спецсимвол (!@#$%^&*)");
    }
}
```

### 8. Создание DTO для создания пользователя и смены пароля

В проекте `TaskFlow.Application` создайте папку `Contracts/Users` и файл `CreateUserRequest.cs`:

```csharp
namespace TaskFlow.Application.Contracts.Users;

public record CreateUserRequest(
    string Username,
    string Email,
    string Password,
    string Role = "User"
);
```

В папке `Contracts/Authentication` создайте файл `ChangePasswordRequest.cs`:

```csharp
namespace TaskFlow.Application.Contracts.Authentication;

public record ChangePasswordRequest(
    string OldPassword,
    string NewPassword,
    string ConfirmPassword
);
```

### 9. Создание AdminController

В проекте `TaskFlow.WebAPI` создайте `Controllers/AdminController.cs`:

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Contracts.Users;
using TaskFlow.Application.Interfaces;

namespace TaskFlow.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")] // <-- Только для админов!
public class AdminController(IAuthService authService) : ControllerBase
{
    private readonly IAuthService _authService = authService;

    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var success = await _authService.CreateUserAsync(request);
        return Ok(new { message = $"Пользователь {request.Username} успешно создан с ролью {request.Role}. Требуется смена временного пароля." });
    }
}
```

### 10. Тестирование

Добавьте в `.http` файл:

```http
### Тест 7: Создание нового пользователя (Только для Admin)
POST {{TaskFlow.WebAPI_HostAddress}}/api/Admin/users
Accept: application/json
Content-Type: application/json
Authorization: Bearer {{token}}

{
  "username": "manager",
  "email": "manager@taskflow.local",
  "password": "Manager123",
  "role": "User"
}
```

> 💡 **Итог:** Теперь у нас есть полноценная система ролей и управления пользователями. Только администраторы могут создавать новых пользователей через защищённый эндпоинт. Пароли валидируются по строгим правилам, а при первом входе пользователь обязан сменить временный пароль.

---

## 🧹 Шаг 15: Административная очистка просроченных токенов

Вместо ручных SQL-скриптов мы реализовали прозрачные API-эндпоинты для управления устаревшими токенами. Это позволяет администратору видеть, **какие именно** токены будут удалены, перед выполнением операции.

### 1. Эндпоинты в `AdminController`

Методы защищены атрибутом `[Authorize(Roles = "Admin")]` и поддерживают как глобальную очистку, так и фильтрацию по `UserId`:

```csharp
[HttpGet("tokens/expired/{userId?}")]
public async Task<IActionResult> GetExpiredTokens(int? userId = null)
{
    var expiredIds = await _authService.GetExpiredTokenIdsAsync(userId);
    var message = userId.HasValue 
        ? $"Найдено просроченных токенов для пользователя с Id: {userId}" 
        : "Найдено просроченных токенов во всей системе";
    return Ok(new { count = expiredIds.Count, expiredIds, message });
}

[HttpDelete("tokens/expired/{userId?}")]
public async Task<IActionResult> DeleteExpiredTokens(int? userId = null)
{
    var deletedIds = await _authService.DeleteExpiredTokensAsync(userId);
    var message = userId.HasValue 
        ? $"Удалено просроченных токенов для пользователя с Id: {userId}" 
        : "Удалено просроченных токенов во всей системе";
    return Ok(new { deletedCount = deletedIds.Count, deletedIds, message });
}
```
> 💡 **Почему это лучше SQL-скрипта?**
> * **Прозрачность**: Ответ содержит массив `expiredIds` / `deletedIds`, что даёт полный аудит действий.
> * **Безопасность**: Доступ есть только у роли `Admin`.
> * **Гибкость**: Легко интегрируется в любую админ-панель (React, Vue, Blazor).

---

## 🧪 Шаг 16: Полное тестирование через .http файл

Используйте файл `TaskFlow.WebAPI.http` в корне проекта `WebAPI` для проверки всех сценариев:

1. Логин админа.
2. Создание пользователя админом (и проверка ошибки при дублировании).
3. Логин нового пользователя (проверка `mustChangePassword: true`).
4. Смена пароля (проверка валидации `PasswordValidator`).
5. Повторный логин (проверка `mustChangePassword: false`).
6. Refresh Token Rotation.
7. Logout с отзывом всех токенов.
8. Очистка просроченных токенов.

💡 **Итог:** Мы получили полностью рабочий, архитектурно правильный скелет Clean Architecture с enterprise-уровнем безопасности, логирования и администрирования. Готовы к переходу к бизнес-логике (Phase 2: Project и Task).

---

## 📊 Шаг 17: Реализация управления проектами и задачами с RBAC

На этом этапе мы добавляем полноценную систему управления проектами и задачами с командной работой на основе ролевой модели доступа (RBAC).

### 🗄️ 17.1. Создание таблиц базы данных

Откройте DBeaver и выполните следующий SQL-скрипт для создания справочников и основных таблиц:

```sql
-- Удаляем старые таблицы (если остались от предыдущих попыток)
DROP TABLE IF EXISTS public."ProjectTasks";
DROP TABLE IF EXISTS public."Projects";
DROP TABLE IF EXISTS public."ProjectTaskStatuses";
DROP TABLE IF EXISTS public."ProjectTaskPriorities";
DROP TABLE IF EXISTS public."ProjectMembers";
DROP TABLE IF EXISTS public."ProjectMemberRoles";

-- 1. Справочник статусов задач проекта
CREATE TABLE public."ProjectTaskStatuses" (
    "Id" INT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    "Name" VARCHAR(50) NOT NULL UNIQUE,
    "Description" VARCHAR(255),
    "SortOrder" INT NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 2. Справочник приоритетов задач проекта
CREATE TABLE public."ProjectTaskPriorities" (
    "Id" INT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    "Name" VARCHAR(50) NOT NULL UNIQUE,
    "Description" VARCHAR(255),
    "Color" VARCHAR(20),
    "SortOrder" INT NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 3. Заполняем справочники
INSERT INTO public."ProjectTaskStatuses" ("Name", "Description", "SortOrder") VALUES
('Pending',    'Новая задача, ещё не взята в работу', 1),
('InProgress', 'Задача в работе',                      2),
('Completed',  'Задача завершена',                     3),
('Cancelled',  'Задача отменена',                      4);

INSERT INTO public."ProjectTaskPriorities" ("Name", "Description", "Color", "SortOrder") VALUES
('Low',      'Низкий приоритет (можно отложить)',                         '#6c757d', 1),
('Medium',   'Средний приоритет (стандартная задача)',                    '#0d6efd', 2),
('High',     'Высокий приоритет (важная задача)',                         '#fd7e14', 3),
('Critical', 'Критический приоритет (требует немедленного внимания)',     '#dc3545', 4);

-- 4. Справочник ролей в проекте
CREATE TABLE public."ProjectMemberRoles" (
    "Id" INT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    "Name" VARCHAR(50) NOT NULL UNIQUE,
    "Description" VARCHAR(255),
    "SortOrder" INT NOT NULL DEFAULT 0,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 5. Заполняем роли в проекте
INSERT INTO public."ProjectMemberRoles" ("Name", "Description", "SortOrder") VALUES
('Owner',  'Создатель проекта. Полный доступ, может удалять проект', 1),
('Admin',  'Администратор проекта. Может управлять участниками и задачами', 2),
('Member', 'Участник. Может создавать и редактировать свои задачи', 3),
('Viewer', 'Наблюдатель. Только просмотр', 4);

-- 6. Таблица проектов
CREATE TABLE public."Projects" (
    "Id" INT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    "Name" VARCHAR(100) NOT NULL,
    "Description" TEXT,
    "OwnerId" INT NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    
    CONSTRAINT "FK_Projects_Users_OwnerId" 
        FOREIGN KEY ("OwnerId") 
        REFERENCES public."Users"("Id") 
        ON DELETE CASCADE
);

-- 7. Таблица участников проектов (Many-to-Many)
CREATE TABLE public."ProjectMembers" (
    "Id" INT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    "ProjectId" INT NOT NULL,
    "UserId" INT NOT NULL,
    "RoleId" INT NOT NULL,
    "JoinedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,

    CONSTRAINT "FK_ProjectMembers_Projects_ProjectId"
        FOREIGN KEY ("ProjectId")
        REFERENCES public."Projects"("Id")
        ON DELETE CASCADE,

    CONSTRAINT "FK_ProjectMembers_Users_UserId"
        FOREIGN KEY ("UserId")
        REFERENCES public."Users"("Id")
        ON DELETE CASCADE,

    CONSTRAINT "FK_ProjectMembers_ProjectMemberRoles_RoleId"
        FOREIGN KEY ("RoleId")
        REFERENCES public."ProjectMemberRoles"("Id")
        ON DELETE RESTRICT,

    -- Уникальность: один пользователь = одна роль в одном проекте
    CONSTRAINT "UQ_ProjectMembers_Project_User"
        UNIQUE ("ProjectId", "UserId")
);

-- 8. Таблица задач проекта
CREATE TABLE public."ProjectTasks" (
    "Id" INT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    "Title" VARCHAR(200) NOT NULL,
    "Description" TEXT,
    "StatusId" INT NOT NULL DEFAULT 1,
    "PriorityId" INT NOT NULL DEFAULT 2,
    "ProjectId" INT NOT NULL,
    "AssigneeId" INT,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "DueDate" TIMESTAMP WITH TIME ZONE,
    
    CONSTRAINT "FK_ProjectTasks_Projects_ProjectId" 
        FOREIGN KEY ("ProjectId") 
        REFERENCES public."Projects"("Id") 
        ON DELETE CASCADE,
    
    CONSTRAINT "FK_ProjectTasks_Users_AssigneeId" 
        FOREIGN KEY ("AssigneeId") 
        REFERENCES public."Users"("Id") 
        ON DELETE SET NULL,
    
    CONSTRAINT "FK_ProjectTasks_ProjectTaskStatuses_StatusId" 
        FOREIGN KEY ("StatusId") 
        REFERENCES public."ProjectTaskStatuses"("Id") 
        ON DELETE RESTRICT,
    
    CONSTRAINT "FK_ProjectTasks_ProjectTaskPriorities_PriorityId" 
        FOREIGN KEY ("PriorityId") 
        REFERENCES public."ProjectTaskPriorities"("Id") 
        ON DELETE RESTRICT
);

-- 9. Индексы
CREATE INDEX "ix_projects_ownerid" ON public."Projects"("OwnerId");
CREATE INDEX "ix_projectmembers_projectid" ON public."ProjectMembers"("ProjectId");
CREATE INDEX "ix_projectmembers_userid" ON public."ProjectMembers"("UserId");
CREATE INDEX "ix_projectmembers_roleid" ON public."ProjectMembers"("RoleId");
CREATE INDEX "ix_projecttasks_projectid" ON public."ProjectTasks"("ProjectId");
CREATE INDEX "ix_projecttasks_assigneeid" ON public."ProjectTasks"("AssigneeId");
CREATE INDEX "ix_projecttasks_statusid" ON public."ProjectTasks"("StatusId");
CREATE INDEX "ix_projecttasks_priorityid" ON public."ProjectTasks"("PriorityId");
CREATE INDEX "ix_projecttasks_duedate" ON public."ProjectTasks"("DueDate");
CREATE UNIQUE INDEX "ix_projects_name_owner" ON public."Projects"("Name", "OwnerId");

-- 10. Тестовые данные
INSERT INTO public."Projects" ("Name", "Description", "OwnerId")
VALUES ('TaskFlow Development', 'Основной проект разработки системы управления задачами', 1);

INSERT INTO public."ProjectMembers" ("ProjectId", "UserId", "RoleId")
VALUES 
(1, 1, 1),  -- admin = Owner проекта
(1, 2, 3);  -- manager = Member проекта

INSERT INTO public."ProjectTasks" ("Title", "Description", "StatusId", "PriorityId", "ProjectId", "AssigneeId", "DueDate")
VALUES 
('Спроектировать архитектуру',     'Создать ER-диаграмму и определить связи между сущностями', 3, 4, 1, 1, NULL),
('Реализовать JWT аутентификацию', 'Добавить JWT токены с ротацией refresh tokens',           3, 3, 1, 1, NULL),
('Создать CRUD для проектов',      'Реализовать создание, чтение, обновление и удаление проектов', 1, 3, 1, 1, '2026-09-15'),
('Создать CRUD для задач',         'Реализовать создание, чтение, обновление и удаление задач',    1, 3, 1, NULL, '2026-09-20'),
('Написать документацию',          'Обновить README и создать API документацию',                  2, 2, 1, 1, '2026-09-25');
```

### 💡 17.2. Модель прав доступа (RBAC)

Система использует двухуровневую модель ролей:

**Глобальные роли (таблица `Roles`):**
- `Admin` — полный доступ ко всей системе
- `User` — стандартный пользователь

**Роли в проекте (таблица `ProjectMemberRoles`):**
- `Owner` — создатель проекта, может удалять проект и управлять участниками
- `Admin` — администратор проекта, может управлять задачами и участниками
- `Member` — участник, может создавать и редактировать свои задачи
- `Viewer` — наблюдатель, только просмотр

**Правила доступа:**

| Действие | Требование |
|----------|------------|
| Видеть проект | Участник проекта (`ProjectMembers`) ИЛИ глобальный Admin |
| Создать задачу | Участник проекта с ролью Member/Admin/Owner ИЛИ глобальный Admin |
| Редактировать задачу | Assignee задачи ИЛИ участник проекта ИЛИ глобальный Admin |
| Удалить задачу | Owner/Admin проекта ИЛИ глобальный Admin |
| Удалить проект | Owner проекта ИЛИ глобальный Admin |

### 🔄 17.3. Генерация сущностей из БД

После создания таблиц перегенерируйте сущности:

```powershell
Scaffold-DbContext "Host=localhost;Port=5433;Database=TaskFlow;Username=postgres;Password=StrongP@ssw0rdHere" Npgsql.EntityFrameworkCore.PostgreSQL -OutputDir Entities -Context AppDbContext -Project TaskFlow.Infrastructure -StartupProject TaskFlow.WebAPI -Force
```

Перенесите новые сущности (`Project.cs`, `ProjectTask.cs`, `ProjectMember.cs`, `ProjectMemberRole.cs`, `ProjectTaskStatus.cs`, `ProjectTaskPriority.cs`) в `TaskFlow.Domain/Entities` и обновите `AppDbContext.cs` в `TaskFlow.Infrastructure/Context`.

### 📋 17.4. Создание DTO

Создайте DTO для проектов и задач в папках `TaskFlow.Application/Contracts/Projects` и `TaskFlow.Application/Contracts/Tasks`:

**CreateProjectRequest.cs:**

```csharp
namespace TaskFlow.Application.Contracts.Projects;

public record CreateProjectRequest(
    string Name,
    string? Description
);
```

**ProjectResponse.cs:**

```csharp
namespace TaskFlow.Application.Contracts.Projects;

public record ProjectResponse(
    int Id,
    string Name,
    string? Description,
    int OwnerId,
    string OwnerUsername,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);
```

**CreateProjectTaskRequest.cs:**

```csharp
namespace TaskFlow.Application.Contracts.Tasks;

public record CreateProjectTaskRequest(
    string Title,
    string? Description,
    int ProjectId,
    int? AssigneeId,
    int StatusId = 1,
    int PriorityId = 2,
    DateTime? DueDate = null
);
```

**ProjectTaskResponse.cs:**

```csharp
namespace TaskFlow.Application.Contracts.Tasks;

public record ProjectTaskResponse(
    int Id,
    string Title,
    string? Description,
    int StatusId,
    string StatusName,
    int PriorityId,
    string PriorityName,
    int ProjectId,
    string ProjectName,
    int? AssigneeId,
    string? AssigneeUsername,
    DateTime CreatedAt,
    DateTime? DueDate
);
```

### 🔧 17.5. Реализация сервисов

Создайте интерфейсы `IProjectService` и `IProjectTaskService` в `TaskFlow.Application/Interfaces`, а затем их реализации `ProjectService.cs` и `ProjectTaskService.cs` в `TaskFlow.Infrastructure/Services`.

**Ключевые особенности:**
- Все методы проверяют права доступа через `ProjectMembers`
- При создании проекта пользователь автоматически становится `Owner`
- При создании задачи проверяется, что Assignee является участником проекта
- Удаление проекта запрещено, если в нём есть задачи

### 🌐 17.6. Контроллеры

Создайте `ProjectController.cs` и `ProjectTaskController.cs` в `TaskFlow.WebAPI/Controllers`:

**ProjectController:**
- `GET /api/Project` — получить все проекты (фильтрация по правам)
- `GET /api/Project/{id}` — получить проект по ID
- `POST /api/Project` — создать проект
- `PUT /api/Project/{id}` — обновить проект
- `DELETE /api/Project/{id}` — удалить проект

**ProjectTaskController:**
- `GET /api/ProjectTask?projectId=&statusId=&priorityId=&assigneeId=` — получить задачи с фильтрами
- `GET /api/ProjectTask/{id}` — получить задачу по ID
- `POST /api/ProjectTask` — создать задачу
- `PUT /api/ProjectTask/{id}` — обновить задачу
- `PATCH /api/ProjectTask/{id}/status` — быстро изменить статус
- `DELETE /api/ProjectTask/{id}` — удалить задачу

### 🧪 17.7. Тестирование

Добавьте в `.http` файл тестовые запросы:

```http
### Получить все проекты (Admin видит все)
GET {{TaskFlow.WebAPI_HostAddress}}/api/Project
Authorization: Bearer {{adminToken}}

### Создать проект от имени manager
POST {{TaskFlow.WebAPI_HostAddress}}/api/Project
Accept: application/json
Content-Type: application/json
Authorization: Bearer {{managerToken}}

{
  "name": "Мобильное приложение",
  "description": "Разработка кроссплатформенного приложения"
}

### Создать задачу в проекте
POST {{TaskFlow.WebAPI_HostAddress}}/api/ProjectTask
Accept: application/json
Content-Type: application/json
Authorization: Bearer {{managerToken}}

{
  "title": "Протестировать новый контроллер",
  "description": "Проверить вертикальный срез разработки",
  "projectId": 1,
  "assigneeId": 2,
  "statusId": 1,
  "priorityId": 3,
  "dueDate": "2026-09-30T12:00:00Z"
}

### Получить задачи с фильтрацией (только Pending)
GET {{TaskFlow.WebAPI_HostAddress}}/api/ProjectTask?statusId=1
Authorization: Bearer {{adminToken}}

### Быстрая смена статуса задачи
PATCH {{TaskFlow.WebAPI_HostAddress}}/api/ProjectTask/6/status
Accept: application/json
Content-Type: application/json
Authorization: Bearer {{adminToken}}

{
  "newStatusId": 2
}
```

💡 **Итог:** Мы получили полноценную систему управления проектами и задачами с командной работой, гибкой системой прав доступа и вертикальной разработкой (каждый сервис → контроллер → тестирование). Готовы к следующему этапу развития!

---

## 📚 Раздел: Архитектурные решения и лучшие практики

### ✅ Почему мы используем Clean Architecture

**Преимущества:**
1. **Независимость от фреймворков** — бизнес-логика в `Domain` и `Application` не зависит от EF Core, ASP.NET Core или PostgreSQL
2. **Тестируемость** — легко писать unit-тесты для сервисов, подменяя зависимости через интерфейсы
3. **Поддерживаемость** — чёткое разделение ответственности, новый разработчик быстро понимает структуру
4. **Масштабируемость** — можно добавлять новые функции, не ломая существующий код

### ✅ Почему Database First

**Наши аргументы:**
1. **Контроль над схемой БД** — мы явно создаём таблицы, индексы, внешние ключи
2. **Оптимальная производительность** — сами решаем, какие индексы нужны
3. **Миграции** — SQL-скрипты версионируются в Git, легко откатить
4. **Командная работа** — DBA и разработчики работают параллельно

### ✅ Почему табличные роли вместо enum

**Проблема enum:**

```csharp
// ❌ Плохо: требует перекомпиляции для добавления роли
public enum UserRole { Admin, User }
```

**Наше решение:**

```sql
-- ✅ Хорошо: можно добавить роль через SQL без перекомпиляции
INSERT INTO "Roles" ("Name", "Description") VALUES ('Moderator', 'Модератор');
```

### ✅ Почему IDENTITY вместо SERIAL

**SERIAL (устаревший):**

```sql
"Id" SERIAL PRIMARY KEY  -- Создаёт sequence с именем "table_id_seq"
```

**IDENTITY (стандарт SQL:2003):**

```sql
"Id" INT GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY  -- Стандартный синтаксис
```

**Преимущества IDENTITY:**
- Переносимость между СУБД (PostgreSQL, Oracle, DB2)
- Не создаёт лишних sequence-объектов
- Лучшая интеграция с EF Core

### ✅ Почему вертикальная разработка (Vertical Slice)

**Проблема горизонтальной:**
1. Пишем все DTO → все интерфейсы → все сервисы → все контроллеры
2. Запускаем тесты → получаем 100500 ошибок
3. Часы отладки, непонятно где ошибка

**Наш подход:**
1. DTO для Project → ProjectService → ProjectController → Тест
2. ✅ Работает! Коммитим.
3. DTO для Task → TaskService → TaskController → Тест
4. ✅ Работает! Коммитим.

**Результат:** Меньше стресса, быстрее фидбек, чистая история Git.

---

## 🎯 Чек-лист для нового разработчика

Перед началом работы убедитесь, что:

- [ ] Docker Desktop запущен (`docker ps` показывает `taskflow-db`)
- [ ] DBeaver подключён к БД (localhost:5433, TaskFlow)
- [ ] Решение собирается без ошибок (`Build: 4 succeeded`)
- [ ] Проект запускается (`F5` → видите "Now listening on: https://localhost:7053")
- [ ] `.http` файл работает (зелёная стрелка → `200 OK`)

---

## 🚀 Быстрый старт (Quick Start)

1. **Запуск БД:**

   ```bash
   docker start taskflow-db
   ```

2. **Запуск приложения:**

   ```bash
   cd TaskFlow.WebAPI
   dotnet run
   ```

3. **Первый запрос (через .http файл):**
   - Откройте `TaskFlow.WebAPI.http`
   - Нажмите ▶ рядом с `POST /api/Auth/login`
   - Скопируйте токен
   - Вставьте в `@token = ...`
   - Выполните `GET /api/Project`

---

## 📞 Поддержка и вопросы

Если возникли проблемы:

1. Проверьте логи в `Logs/Errors/`
2. Убедитесь, что Docker-контейнер запущен
3. Проверьте строку подключения в `appsettings.json`
4. Перечитайте раздел "Решение возможных проблем с запуском Docker"

---

**Дата последней актуализации:** 11.09.2026  
**Версия проекта:** 1.0.0 (MVP с проектами и задачами)  
**Статус:** ✅ Production Ready (для внутренней разработки)