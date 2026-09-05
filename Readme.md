
# 🚀 TaskFlow: Настройка окружения для C# (.NET 11.0.0-preview.7.26381.103) проекта (Clean Architecture + Database First)

Данная инструкция описывает развертывание PostgreSQL в Docker, инициализацию схемы базы данных и подготовку тестовых данных для последующей разработки системы управления задачами с JWT-аутентификацией.


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

![Окно "Скачивания DBeaver Community"](Docs/Screenshots/Download-Dbeaver-Community.png)

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
В решении `TaskFlow` создаются 4 проекта типа **Class Library** (кроме WebAPI):
1. `TaskFlow.Domain` (Ядро, без зависимостей)
2. `TaskFlow.Application` (Бизнес-логика и интерфейсы)
3. `TaskFlow.Infrastructure` (Реализация интерфейсов, работа с БД)
4. `TaskFlow.WebAPI` (Точка входа, контроллеры, настройки)

> ⚠️ **Важно:** При создании всех проектов необходимо явно выбрать одну и ту же целевую платформу (например, `.NET 11.0 Preview`), чтобы избежать конфликтов версий при сборке.

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

##  Шаг 7: Установка NuGet-пакетов для EF Core

Чтобы Entity Framework Core мог подключиться к нашей PostgreSQL и сгенерировать C#-код, нам нужно установить необходимые пакеты. Мы делаем это в соответствии с правилами Clean Architecture.

1. В Visual Studio откройте **Консоль диспетчера пакетов** (Средства → Диспетчер пакетов NuGet → Консоль диспетчера пакетов).
2. В выпадающем списке **Проект по умолчанию** (Default project) выберите **TaskFlow.Infrastructure** и выполните команды:

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

##  Шаг 8: Генерация кода из БД (Database First)

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
