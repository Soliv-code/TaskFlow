

# 🚀 TaskFlow: Настройка окружения для C# (.NET 11.0.0-preview.7.26381.103) проекта (Clean Architecture + Database First)

Данная инструкция описывает развертывание PostgreSQL в Docker, инициализацию схемы базы данных и подготовку тестовых данных для последующей разработки системы управления задачами с JWT-аутентификацией.

> *P.s.: Разработка производится в Visual Studio Community 2026 Insiders [12120.281]*
> *возможно некоторые советы по исправлению уже не актуальны!*
>
> *P.s.s.: Дата разработки 06.09.2026*

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
>  * Размер (главная причина): postgres:16-alpine весит ~50-100 MB, тогда как обычный postgres:16 — ~300-400 MB.
>  * Безопасность: Меньше пакетов = меньше потенциальных уязвимостей. Alpine создан с фокусом на безопасность.
>  * Производительность: Потребляет меньше ресурсов и быстрее запускается.
>  * Для разработки: Внутри контейнера не нужны лишние инструменты, так как подключение к БД происходит снаружи (через DBeaver/pgAdmin).

  
  

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

  
  

## ⚠️ Шаг 2: Решение возможных проблем с запуском Docker

ВАЖНО! Если после перезагрузки компьютера Docker не стартует или долго висит в статусе Docker Desktop is starting..., выполните следующие действия:

1. Полностью закройте Docker Desktop.

2. Нажмите Win + Q и введите в поиске: Безопасность Windows.

3. В левом меню выберите Управление приложениями и браузером.

4. Внизу страницы нажмите на ссылку Защита от эксплойтов.

5. Перейдите на вкладку Параметры программ.

6. Найдите в списке C:\Windows\System32\VmCompute.exe и нажмите кнопку [Изменить].

7. Найдите блок `Защита потока управления (CFG)` и снимите галочку ✔ с главного пункта "Переопределить системные параметры".

![Окно "Защита от эксплойтов" в настройках Windows ps](Docs/Screenshots/exploit-protection.png)

8. Сохраните изменения и запустите Docker Desktop заново.

  

## 💻 Шаг 3: Установка и настройка DBeaver

1. Скачайте DBeaver Community с официального сайта [DBeaver Community](https://dbeaver.io/download/) (Download EXE).

![Окно "Скачивания DBeaver Community"](Docs/Screenshots/download-dbeaver-Community.png)

2. Установите программу и перезагрузите компьютер (если потребуется).

3. Запустите DBeaver. На верхней панели выберите: База данных → Новое соединение (или нажмите Ctrl + Shift + N).

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

  


## 🗄️ Шаг 4: Создание схемы базы данных (Database First)

Поскольку мы используем подход **Database First**, сначала мы создаем структуру таблиц в PostgreSQL, а затем сгенерируем C#-классы с помощью EF Core.

1. Откройте **DBeaver**, подключитесь к базе данных `TaskFlow` (порт `5433`).
2. Создайте новый SQL-скрипт (`Ctrl + ]`) или правая кнопка мыши по соединению → SQL Editor → New SQL Script).

![Окно создания нового скрипта в DBeaver](Docs/Screenshots/Dbeaver-New-Sql-Script.png)

3. Выполните следующий скрипт для создания таблиц пользователей и токенов:

```sql
-- 1. Таблица пользователей
CREATE TABLE public."Users" (
    "Id" SERIAL PRIMARY KEY,
    "Username" VARCHAR(50) NOT NULL UNIQUE,
    "Email" VARCHAR(100) NOT NULL UNIQUE,
    "PasswordHash" VARCHAR(255) NOT NULL,
    "CreatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP,
    "UpdatedAt" TIMESTAMP WITH TIME ZONE DEFAULT CURRENT_TIMESTAMP
);

-- 2. Таблица Refresh Tokens
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

-- 3. Индексы для производительности
CREATE INDEX "ix_refreshtokens_token" ON public."RefreshTokens"("Token");
CREATE INDEX "ix_refreshtokens_userid" ON public."RefreshTokens"("UserId");
CREATE INDEX "ix_refreshtokens_expiresat" ON public."RefreshTokens"("ExpiresAt");
```
4. После выполнения нажмите кнопку Refresh (F5) в навигаторе баз данных.
5. Убедитесь, что в схеме public появились таблицы Users и RefreshTokens.

![Окно схемы БД "TaskFlow"](Docs/Screenshots/Dbeaver-Schema-Tables.png)


## 🔐 Шаг 5: Создание тестового пользователя (Admin)

Для удобства тестирования JWT-аутентификации создадим тестового администратора. Мы используем алгоритм **PBKDF2**, встроенный в .NET, чтобы избежать лишних зависимостей.

1. Откройте SQL Editor в DBeaver.
2. Выполните скрипт для создания пользователя:

```sql
-- Создаем тестового пользователя admin
-- Пароль: Admin123 
-- Формат хеша: итерации:base64_соль:base64_хеш (PBKDF2-HMAC-SHA256, 100k итераций)
INSERT INTO public."Users" ("Username", "Email", "PasswordHash")
VALUES (
    'admin',
    'admin@taskflow.local',
    '100000:NDQyY34PFV8T9l5WOcItfQ==:POxpZySDaIgiYHCy8qtVUZnUZeEFAT26H3VWtkJpwYU='
);

-- Проверяем результат
SELECT "Id", "Username", "Email", "CreatedAt" 
FROM public."Users" 
WHERE "Username" = 'admin';
```
![Окно схемы БД "TaskFlow"](Docs/Screenshots/Dbeaver-Admin-Created.png)


> 💡 **Почему такой формат хеша?**
> 
> * Мы не используем сторонние пакеты (как BCrypt), а берем встроенный в .NET `Rfc2898DeriveBytes` (PBKDF2).
> * Формат `100000:salt:hash` позволяет нам хранить все необходимые параметры для проверки пароля в одной строке БД.
> * Позже мы напишем класс `PasswordHasher` в слое Infrastructure, который будет генерировать и проверять такие строки.
---

## 🏗️ Шаг 6: Инициализация структуры решения (Clean Architecture)

После настройки БД мы создаем структуру решения в Visual Studio, используя **.NET 11 Preview**. 

### 1. Создание проектов
В решении `TaskFlow` создаются 4 проекта типа **Class Library** (кроме **WebAPI**):
![Окно создания Class Library](Docs/Screenshots/csharp_class_library_project.png)
1. `TaskFlow.Domain` (Ядро, без зависимостей)
2. `TaskFlow.Application` (Бизнес-логика и интерфейсы)
3. `TaskFlow.Infrastructure` (Реализация интерфейсов, работа с БД)
4. `TaskFlow.WebAPI` (Точка входа, контроллеры, настройки)
![Окно создания Web.API](Docs/Screenshots/csharp_web_api_project.png)

> ⚠️ **Важно:** При создании всех проектов необходимо явно выбрать одну и ту же целевую платформу (например, `.NET 11.0 Preview`), чтобы избежать конфликтов версий при сборке.
>

![Окно создания выбора версии .net](Docs/Screenshots/csharp_class_library_version.png)

### 2. Очистка от шаблонов
Сразу после создания удаляем мусор, сгенерированный Visual Studio:
* В `TaskFlow.WebAPI`: удалить `WeatherForecast.cs` и `WeatherForecastController.cs`.
* В `Domain`, `Application`, `Infrastructure`: удалить `Class1.cs`.

### 3. Настройка зависимостей (Правило направленных внутрь связей)
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
>💡 **Почему именно такое распределение?**
> * **Infrastructure**: Здесь будет жить `DbContext` и провайдер базы данных (`Npgsql`). Это слой, отвечающий за внешние зависимости.
> * **WebAPI**: Пакет `Tools` нужен для запуска команд генерации кода (Scaffold) из консоли, поэтому он должен быть установлен в стартовом (запускаемом) проекте.
> * **Domain и Application**: Остаются абсолютно чистыми! Мы не тянем зависимости от EF Core в ядро проекта.

4.  После установки нажмите **Сборка** → **Пересобрать решение** (Rebuild Solution), чтобы убедиться, что все пакеты корректно интегрировались и конфликтов версий нет (`Build: 4 succeeded, 0 failed`).

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
2. В выпадающем списке **Проект по умолчанию** выберите **TaskFlow.Infrastructure**.
3. Выполните следующую команду:

```powershell
Scaffold-DbContext "Host=localhost;Port=5433;Database=TaskFlow;Username=postgres;Password=StrongP@ssw0rdHere" Npgsql.EntityFrameworkCore.PostgreSQL -OutputDir Entities -Context AppDbContext -Project TaskFlow.Infrastructure -StartupProject TaskFlow.WebAPI -Force
```

**Разбор параметров команды:**
> -   `Host=...` — строка подключения к нашему Docker-контейнеру.
> -   `-OutputDir Entities` — указывает папку для генерации сущностей.
> -   `-Context AppDbContext` — задает имя для главного класса контекста базы данных.
> -   `-Project TaskFlow.Infrastructure` — проект, куда будут добавлены файлы.
> -   `-StartupProject TaskFlow.WebAPI` — проект запуска (нужен EF Core для чтения конфигураций).
> -   `-Force` — разрешает перезапись файлов при повторном запуске.

4.  После выполнения в проекте **TaskFlow.Infrastructure** появится папка `Entities` с файлами:
    -   `AppDbContext.cs`
    -   `User.cs`
    -   `RefreshToken.cs`

> ⚠️ **Важное примечание по Clean Architecture:** По умолчанию EF Core генерирует всё в указанный проект. Однако по правилам Clean Architecture, сущности (**`User`**, **`RefreshToken`**) должны находиться в слое **`Domain`**, а **`AppDbContext`** — в **`Infrastructure`**. На следующем шаге мы проведем рефакторинг и разнесем эти файлы по правильным слоям.

---

## 🏗️ Шаг 9: Рефакторинг сгенерированного кода и настройка подключения к БД

По умолчанию EF Core генерирует весь код в один проект. Чтобы соблюсти правила **Clean Architecture**, мы разнесем сущности и контекст по правильным слоям, а строку подключения вынесем в конфигурацию.

### 1. Распределение файлов по слоям
1. В проекте **TaskFlow.Domain** создайте папку `Entities` и переместите туда файлы `User.cs` и `RefreshToken.cs`.
2. В проекте **TaskFlow.Infrastructure** создайте папку `Context` и переместите туда файл `AppDbContext.cs`. Старую папку `Entities` в Infrastructure можно удалить.
3. Исправьте пространства имен (`namespace`) в перемещенных файлах:
   * В `User.cs` и `RefreshToken.cs`: `namespace TaskFlow.Domain.Entities;`
   * В `AppDbContext.cs`: `namespace TaskFlow.Infrastructure.Context;`
4. Добавьте `using TaskFlow.Domain.Entities;` в начало файла `AppDbContext.cs`, чтобы он увидел сущности из другого проекта.

### 2. Вынос строки подключения (Dependency Injection)
Хардкодить строку подключения внутри `AppDbContext` — это антипаттерн. Мы вынесем её во внешний слой (`WebAPI`).

1. Откройте `AppDbContext.cs` и **полностью удалите** сгенерированный метод `OnConfiguring`.
2. Откройте `appsettings.json` в проекте **TaskFlow.WebAPI** и добавьте секцию `ConnectionStrings`:

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

>💡 **Почему именно так?**
>
>-   **Слабая связанность**: Слой `Infrastructure` больше ничего не знает о строке подключения или файлах конфигурации. Он просто получает готовый `DbContext` через конструктор.
>-   **Безопасность**: Строка подключения хранится в `appsettings.json` (который, кстати, часто добавляют в `.gitignore` для production-версий, оставляя только безопасные шаблоны).
>-   **Гибкость**: При необходимости вы сможете легко подменить реализацию БД или использовать разные строки для тестов и продакшена.

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
По этому добавление контроллера производится через локальное меню в `Solution Explorer` (Правой кнопкой мыши вызовите локальное меню папки `Controllers` выберите пункт `Add` далее `New Item`

![Добавление пустого контроллера:](Docs/Screenshots/csharp_add_authcontroller.png)

В открывшемся списке найдите `API Controller - Empty` и измените имя контроллера на: `AuthController.cs`

![Создание пустого контроллера:](Docs/Screenshots/csharp_authcontroller.png)

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
>⚠️ **Критически важно:** Убедитесь, что подключен именно ваш `using 
> TaskFlow.Application.Contracts.Authentication;`. Visual Studio по ошибке может предложить `using 
>  Microsoft.AspNetCore.Identity.Data;`, что приведет к конфликту типов `LoginRequest` и ошибкам компиляции.

### 2. Тестирование API (рекомендуется Bruno)

Для тестирования мы используем **Bruno** (или Postman/Insomnia), так как он легче и надежнее встроенного Swagger в .NET Preview-версиях.

1.  Запустите проект **TaskFlow.WebAPI** (F5).
2.  Откройте Bruno и создайте новый запрос:
    -   **Метод:**  `POST`
    -   **URL:**  `https://localhost:7053/api/Auth/login`  _(порт может отличаться, проверьте в свойствах проекта)_
    -   **Body:**  `application/json`

#### ✅ Тест 1: Успешная аутентификация

**Request Body:**
```json
{
  "username": "admin",
  "password": "Admin123"
}
```
**Expected Response (200 OK):**
```json
{
  "id": 1,
  "username": "admin",
  "email": "admin@taskflow.local",
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiIxIiwiZW1haWwiOiJhZG1pbkB0YXNrZmxvdy5sb2NhbCIsImp0aSI6IjRjZTZkZDIyLTg5MDUtNDJmYy1iZTQwLWU3ODA4NmI5ZDE0NiIsImV4cCI6MTc4ODY2MTk3MCwiaXNzIjoiVGFza0Zsb3dBUEkiLCJhdWQiOiJUYXNrRmxvd0NsaWVudCJ9.n2bdQTAkxEQ4_y0DzIEowUxd5R4o9mz51KzKhoDnDPk"
}
```
#### ❌ Тест 2: Неверный логин или пароль

**Request Body:**
```json
{
  "username": "admi",
  "password": "Admin123"
}
```
**Expected Response (401 Unauthorized):**
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
@TaskFlow.WebAPI_HostAddress = http://localhost:5076

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
> * @TaskFlow.WebAPI_HostAddress — это переменная, которую можно использовать во всех запросах (удобно менять порт в одном месте).
> * '###' — разделитель между запросами.
> * Рядом с каждым запросом появляется зеленая стрелочка ▶ — нажмите на неё, чтобы выполнить запрос.
> * Ответ появится в правой части окна Visual Studio.

3.  Запустите проект (`F5`) и нажмите на зеленую стрелочку рядом с первым запросом.
4.  Вы увидите ответ `200 OK` с JWT-токеном прямо в Visual Studio!

![Тестирование через http-файл:](Docs/Screenshots/csharp_testing_endpoints_by_http.png)

> ⚠️ **Важно:** Убедитесь, что порт в переменной `@TaskFlow.WebAPI_HostAddress` совпадает с портом вашего запущенного проекта (проверьте в `launchSettings.json` или в свойствах проекта).