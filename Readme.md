
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
![Результат команды docker ps](screenshots/docker-images.png)
  

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

  

![Результат команды docker ps](screenshots/docker-ps.png)

  
  

## ⚠️ Шаг 2: Решение возможных проблем с запуском Docker

ВАЖНО! Если после перезагрузки компьютера Docker не стартует или долго висит в статусе Docker Desktop is starting..., выполните следующие действия:

1. Полностью закройте Docker Desktop.

2. Нажмите Win + Q и введите в поиске: Безопасность Windows.

3. В левом меню выберите Управление приложениями и браузером.

4. Внизу страницы нажмите на ссылку Защита от эксплойтов.

5. Перейдите на вкладку Параметры программ.

6. Найдите в списке C:\Windows\System32\VmCompute.exe и нажмите кнопку [Изменить].

7. Найдите блок `Защита потока управления (CFG)` и снимите галочку ✔ с главного пункта "Переопределить системные параметры".

![Окно "Защита от эксплойтов" в настройках Windows ps](screenshots/exploit-protection.png)

8. Сохраните изменения и запустите Docker Desktop заново.

  

## 💻 Шаг 3: Установка и настройка DBeaver

1. Скачайте DBeaver Community с официального сайта [DBeaver Community](https://dbeaver.io/download/) (Download EXE).

![Окно "Скачивания DBeaver Community"](screenshots/Download-Dbeaver-Community.png)

2. Установите программу и перезагрузите компьютер (если потребуется).

3. Запустите DBeaver. На верхней панели выберите: База данных → Новое соединение (или нажмите Ctrl + Shift + N).

![Окно создания нового соединения в DBeaver](screenshots/dbeaver-New-Connection.png)

4. В открывшемся окне выберите PostgreSQL и нажмите [Далее].

![Окно создания нового соединения в DBeaver выбор БД](screenshots/dbeaver-New-Connection-PostgreSQL.png)

5. Настройте параметры подключения к нашей базе данных:
> * **Хост (Host):** localhost
> * **Порт (Port):** 5433
> * **База данных (Database):** TaskFlow (или postgres для первичного входа)
> * **Имя пользователя (Username):** postgres
> * **Пароль (Password):** StrongP@ssw0rdHere

![Окно с заполненными полями](screenshots/dbeaver-connection-settings.png)

6. Нажмите кнопку **[Test Connection...]**. Вы должны получить сообщение об успешном подключении:

![Окно с заполненными полями](screenshots/dbeaver-success.png)

  


## 🗄️ Шаг 4: Создание схемы базы данных (Database First)

Поскольку мы используем подход **Database First**, сначала мы создаем структуру таблиц в PostgreSQL, а затем сгенерируем C#-классы с помощью EF Core.

1. Откройте **DBeaver**, подключитесь к базе данных `TaskFlow` (порт `5433`).
2. Создайте новый SQL-скрипт (`Ctrl + ]`) или правая кнопка мыши по соединению → SQL Editor → New SQL Script).

![Окно создания нового скрипта в DBeaver](screenshots/Dbeaver-New-Sql-Script.png)

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

![Окно схемы БД "TaskFlow"](screenshots/Dbeaver-Schema-Tables.png)


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
![Окно схемы БД "TaskFlow"](screenshots/Dbeaver-Admin-Created.png)


> 💡 **Почему такой формат хеша?**
> 
> * Мы не используем сторонние пакеты (как BCrypt), а берем встроенный в .NET `Rfc2898DeriveBytes` (PBKDF2).
> * Формат `100000:salt:hash` позволяет нам хранить все необходимые параметры для проверки пароля в одной строке БД.
> * Позже мы напишем класс `PasswordHasher` в слое Infrastructure, который будет генерировать и проверять такие строки.
