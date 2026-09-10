# 🗺️ TaskFlow Project Roadmap

**Статус проекта:** Фаза 2 завершена ✅  
**Последнее обновление:** 11.09.2026  
**Текущий спринт:** Качество кода (FluentValidation, Swagger, Unit-тесты)

---

## 📊 Общий прогресс
████████████████████████████░░  85%

**Завершено:** 20 из 23 основных задач


---

## ✅ Фаза 1: Фундамент и Инфраструктура (ЗАВЕРШЕНА)

### 🐳 Инфраструктура и БД
- [x] 🐳 PostgreSQL 16-alpine в Docker
- [x] 🔧 Настройка DBeaver для подключения (порт 5433)
- [x] 📐 Создание схемы БД (Database First подход)
  - [x] Таблица `Roles` (Id, Name, Description, CreatedAt)
  - [x] Таблица `Users` (Id, Username, Email, PasswordHash, RoleId, MustChangePassword, CreatedAt, UpdatedAt)
  - [x] Таблица `RefreshTokens` (Id, UserId, Token, ExpiresAt, IsRevoked, CreatedAt)
  - [x] Индексы для производительности (`ix_refreshtokens_token`, `ix_refreshtokens_userid`, `ix_refreshtokens_expiresat`)
  - [x] Внешний ключ с каскадным удалением (`FK_RefreshTokens_Users_UserId`)
  - [x] Внешний ключ с защитой от удаления роли (`FK_Users_Roles_RoleId` с `ON DELETE RESTRICT`)
- [x] 👤 Создание тестового администратора (PBKDF2 хеш, пароль: Admin123)

### 🏗️ Архитектура проекта
- [x] 🏗️ Clean Architecture структура
  - [x] `TaskFlow.Domain` (ядро, без зависимостей)
  - [x] `TaskFlow.Application` (бизнес-логика, интерфейсы)
  - [x] `TaskFlow.Infrastructure` (реализация, EF Core, БД)
  - [x] `TaskFlow.WebAPI` (контроллеры, DI, middleware)
- [x] 🔄 Правильные зависимости между слоями
- [x] 📦 NuGet пакеты (Npgsql.EntityFrameworkCore.PostgreSQL, Microsoft.EntityFrameworkCore.Tools)
- [x] 🏗️ Генерация кода из БД (Scaffold-DbContext)
- [x] ♻️ Рефакторинг: перенос сущностей в Domain, DbContext в Infrastructure

### 🔐 Аутентификация и Авторизация
- [x] 🔐 JWT Access Token (15 минут)
- [x] 🔄 Refresh Token (7 дней) с ротацией
- [x] 🚪 Эндпоинт Logout с отзывом всех токенов
- [x] 🛡️ Автоматический отзыв старых токенов при Login
- [x] 👑 Ролевая модель (таблица Roles вместо Enum)
  - [x] Таблица `Roles` с полями Name, Description
  - [x] Связь Users -> Roles через RoleId (внешний ключ)
  - [x] Role claim в JWT токене (берётся из `user.Role.Name`)
  - [x] Атрибут `[Authorize(Roles = "Admin")]`
- [x] 🔐 Хеширование паролей (PBKDF2, 100k итераций, SHA-256)
  - [x] Метод `PasswordHasher.Hash()` для создания пользователей
  - [x] Метод `PasswordHasher.Verify()` для проверки паролей
  - [x] FixedTimeEquals для защиты от timing attacks
- [x] ✅ Валидация сложности пароля (`PasswordValidator`)
  - [x] Минимум 8 символов
  - [x] Минимум 1 заглавная буква (A-Z)
  - [x] Минимум 1 цифра (0-9) или спецсимвол (!@#$%^&*)
- [x] 🔄 Флаг обязательной смены временного пароля (`MustChangePassword`)
  - [x] При создании пользователя админом устанавливается `MustChangePassword = true`
  - [x] Эндпоинт `POST /api/Auth/change-password` для смены пароля
  - [x] После успешной смены флаг сбрасывается в `false`
  - [x] Флаг возвращается в `AuthResponse` при логине и рефреше

### 🛡️ Безопасность и Обработка ошибок
- [x] 🌍 Глобальный ExceptionHandlingMiddleware
  - [x] Перехват ошибок подключения к БД
  - [x] Возврат 503 Service Unavailable с понятным сообщением
  - [x] Логирование полного стектрейса
  - [x] Обработка `BusinessException` (возврат 400 Bad Request с понятным текстом)
- [x] 🔒 Защита эндпоинтов через `[Authorize]`
- [x] 🧪 Тестовый контроллер TestController для проверки авторизации
- [x] 🚫 Обработка нарушения UNIQUE constraint (понятные сообщения при дублировании email/username)

### 📝 Логирование (Native, без сторонних библиотек)
- [x] 📝 Разделение логов
  - [x] `Logs/AppLog/` — история работы приложения
  - [x] `Logs/Errors/` — критические ошибки с разделителями
- [x] 📄 NativeFileLogger (потокобезопасный, с lock)
- [x] 📅 Ежедневная ротация файлов (формат: `yyyy-MM-dd_HH-mm-ss`)
- [x] 🎛️ Настройка через `LoggingConfig` в appsettings.json
- [x] 🔇 Отключение спама от EF Core

### 👨‍💼 Администрирование
- [x] 📋 AdminController с защитой по роли
- [x] ➕ POST `/api/Admin/users` — создание новых пользователей (с валидацией и установкой `MustChangePassword = true`)
- [x] 👁️ GET `/api/Admin/tokens/expired/{userId?}` — просмотр просроченных токенов
  - [x] Возврат count + массив expiredIds (полная прозрачность)
  - [x] Поддержка глобального запроса (без userId)
- [x] 🗑️ DELETE `/api/Admin/tokens/expired/{userId?}` — удаление просроченных токенов
  - [x] Возврат deletedCount + массив deletedIds (аудит действий)
  - [x] Удаление только по ExpiresAt (не затрагивая IsRevoked для истории)

### 🧪 Тестирование
- [x] 📄 Профессиональный `.http` файл в репозитории
  - [x] Переменные для токенов и хоста
  - [x] 11 тестовых сценариев (Auth, Logout, Admin, Token Cleanup, ChangePassword)
  - [x] Разделение на логические секции с комментариями
- [x] ✅ Тестирование через Visual Studio REST Client и Bruno

### 📚 Документация
- [x] 📚 Полный README.md (17 шагов)
- [x] Все скриншоты сохранены
- [x] Примеры кода для каждого шага
- [x] Объяснения архитектурных решений
- [x] Troubleshooting (Docker, ошибки подключения)

---

## ✅ Фаза 2: Бизнес-логика TaskFlow (ЗАВЕРШЕНА)

### 📋 Сущности ядра
- [x] 📁 Создание сущности `Project` в Domain
  - [x] Свойства: Id, Name, Description, OwnerId, CreatedAt, UpdatedAt
  - [x] Связь: One-to-Many с ProjectTask
- [x] 📁 Создание сущности `ProjectTask` в Domain (вместо `Task` — избежали конфликта с `System.Threading.Tasks.Task`)
  - [x] Свойства: Id, Title, Description, StatusId, PriorityId, ProjectId, AssigneeId, CreatedAt, DueDate
  - [x] Справочники: `ProjectTaskStatus`, `ProjectTaskPriority` (таблицы вместо enum)
- [x] 🗄️ Обновление схемы БД
  - [x] SQL скрипт создания таблиц Projects, ProjectTasks, ProjectMembers, ProjectMemberRoles
  - [x] Индексы для производительности
  - [x] Внешние ключи
  - [x] Справочники статусов и приоритетов
- [x] 🤝 Командная модель (Many-to-Many через ProjectMembers)
  - [x] Таблица `ProjectMemberRoles` (Owner, Admin, Member, Viewer)
  - [x] Уникальный индекс (ProjectId, UserId) — одна роль на пользователя в проекте
  - [x] Автоматическое добавление создателя как Owner

### ⚙️ CRUD операции
- [x] 📁 ProjectController
  - [x] GET `/api/Project` — список проектов (фильтрация по правам через ProjectMembers)
  - [x] GET `/api/Project/{id}` — детали проекта
  - [x] POST `/api/Project` — создание проекта (авто-добавление Owner в ProjectMembers)
  - [x] PUT `/api/Project/{id}` — обновление проекта
  - [x] DELETE `/api/Project/{id}` — удаление проекта (защита от удаления с задачами)
- [x] 📋 ProjectTaskController
  - [x] GET `/api/ProjectTask?projectId=&statusId=&priorityId=&assigneeId=` — список задач с фильтрами
  - [x] GET `/api/ProjectTask/{id}` — детали задачи
  - [x] POST `/api/ProjectTask` — создание задачи (с проверкой, что Assignee — участник проекта)
  - [x] PUT `/api/ProjectTask/{id}` — обновление задачи
  - [x] PATCH `/api/ProjectTask/{id}/status` — быстрое изменение статуса
  - [x] DELETE `/api/ProjectTask/{id}` — удаление задачи

### 🔒 Защита бизнес-логики
- [x] 🛡️ Применение `[Authorize]` ко всем контроллерам
- [x] 🔐 Проверка прав доступа через ProjectMembers (RBAC)
  - [x] Пользователь видит только проекты/задачи, где он участник
  - [x] Admin видит всё
  - [x] Owner проекта может управлять им
  - [x] Member может создавать задачи
  - [x] Assignee может редактировать свои задачи
- [x] ✅ Валидация данных (BusinessException)
  - [x] Проверка существования StatusId и PriorityId
  - [x] Проверка, что Assignee — участник проекта
  - [x] Уникальность названия проекта для владельца

---

## 🌟 Фаза 3: Улучшения и Оптимизация (ПЛАН)

### 🔄 Фоновые задачи
- [ ] 🔄 IHostedService для автоматической очистки токенов
  - [ ] 🕐 Запуск раз в сутки (03:00)
  - [ ] 🗑️ Удаление токенов с ExpiresAt < NOW()
  - [ ] 📝 Логирование количества удалённых записей

### 📊 Расширенное логирование
- [ ] 📝 Чистый SQL лог в консоли
  - [ ] 🔍 Вывод реальных значений параметров вместо `?`
  - [ ] ⚙️ Настройка через RelationalEventId
  - [ ] 🔇 Без лишнего шума (только важные запросы)

### 📦 Дополнительные функции
- [ ] 📧 Email уведомления (при назначении задачи, дедлайне)
- [ ] 📈 Статистика и отчёты (выполненные задачи, загрузка пользователей)
- [ ] 🔍 Полнотекстовый поиск по задачам и проектам
- [ ] 📎 Вложения файлов к задачам

### 🧪 Тестирование
- [ ] 🧪 Unit тесты (xUnit + Moq)
  - [ ] Тесты для AuthService
  - [ ] Тесты для PasswordHasher
  - [ ] Тесты для PasswordValidator
  - [ ] Тесты для бизнес-логики (ProjectService, ProjectTaskService)
- [ ] 🔗 Integration тесты
  - [ ] Тесты контроллеров с TestServer
  - [ ] Тесты с реальной БД (Testcontainers)

### 📱 Frontend (опционально)
- [ ] 🌐 SPA приложение (React/Vue/Blazor)
  - [ ] Страница логина/регистрации
  - [ ] Дашборд с задачами
  - [ ] Управление проектами
  - [ ] Админ-панель

---

## 📊 Метрики качества

| Категория | Статус | Прогресс |
|-----------|--------|----------|
| Архитектура | ✅ Отлично | 100% |
| Безопасность | ✅ Отлично | 100% |
| Логирование | ✅ Отлично | 100% |
| Документация | ✅ Отлично | 100% |
| Бизнес-логика | ✅ Завершена | 100% |
| Тестирование | ⚠️ Требует внимания | 30% |
| UI/UX | 📅 Запланировано | 0% |

---

## 🎯 Ближайшие цели (Sprint 2)

1. Внедрить FluentValidation для DTO
2. Настроить Swagger/OpenAPI документацию
3. Написать базовые unit-тесты (xUnit + Moq)
4. Добавить интеграционные тесты

**Цель спринта:** Повышение качества кода и автоматизация тестирования к 15.09.2026

---

## 🏆 Достижения

- ✅ Clean Architecture — правильная структура с нуля
- ✅ Zero Dependencies — нативное логирование без Serilog/NLog
- ✅ Enterprise Security — PBKDF2, Refresh Token Rotation, Timing-safe comparison
- ✅ Transparent Admin API — полная аудируемость действий (expiredIds/deletedIds)
- ✅ Production Ready — глобальная обработка ошибок, 503 при недоступности БД
- ✅ Table-based Roles — гибкая система ролей без перекомпиляции
- ✅ Password Validation — строгая валидация сложности паролей
- ✅ Temporary Passwords — механизм обязательной смены временного пароля
- ✅ RBAC на уровне проектов — гибкая система прав (Owner/Admin/Member/Viewer)
- ✅ Vertical Slice Architecture — каждый сервис → контроллер → тест
- ✅ ProjectTask вместо Task — избежали конфликта с System.Threading.Tasks
- ✅ IDENTITY вместо SERIAL — стандарт SQL:2003, переносимость между СУБД
- ✅ Командная работа — многие-ко-многим через ProjectMembers

---

## 🚀 Фаза 4: Оптимизация и Масштабирование (БУДУЩЕЕ)

### 📦 Кэширование (Redis)
- [ ] 🐳 Поднять Redis в Docker (аналогично PostgreSQL)
- [ ] 📦 Установить NuGet-пакет `Microsoft.Extensions.Caching.StackExchangeRedis`
- [ ] ⚙️ Настроить `IDistributedCache` в `Program.cs`
- [ ] 🧠 Реализовать кэширование для "тяжёлых" эндпоинтов:
  - [ ] GET `/api/Project` — список проектов
  - [ ] GET `/api/ProjectTask` — список задач (с учётом фильтров)
  - [ ] GET `/api/Admin/users` — список пользователей (для админа)
- [ ] 🔄 Настроить стратегию инвалидации кэша (Cache-Aside Pattern)
- [ ] 📊 Замерить производительность до/после (Stopwatch + логирование)

### 📨 Брокер сообщений (RabbitMQ)
- [ ] 🐰 Поднять RabbitMQ в Docker (с Management UI на порту 15672)
- [ ] 📦 Установить NuGet-пакет `RabbitMQ.Client`
- [ ] 🏗️ Создать слой `TaskFlow.MessageBroker` (или добавить в Infrastructure)
- [ ] 📤 Реализовать Publisher для событий:
  - [ ] `TaskCreated` — при создании задачи
  - [ ] `TaskStatusChanged` — при смене статуса
  - [ ] `UserAssigned` — при назначении исполнителя
- [ ] 📥 Реализовать Consumer для фоновой обработки:
  - [ ] Отправка email-уведомлений
  - [ ] Логирование действий в отдельный audit-лог
  - [ ] Обновление статистики
- [ ] 🔄 Реализовать паттерн Dead Letter Queue (для ошибок обработки)
- [ ] 🧪 Тестирование через RabbitMQ Management UI

### 📧 Уведомления (зависит от RabbitMQ)
- [ ] 📧 Интеграция с SMTP (отправка email)
- [ ] 🔔 WebSocket для real-time уведомлений на фронтенде

---

## 📝 Примечания

- **Ветка разработки:** `feature/project-work-items` (текущая), `master` (стабильная)
- **Версия .NET:** 11.0.0-preview.7.26381.103
- **IDE:** Visual Studio Community 2026 Insiders [12120.281]
- **База данных:** PostgreSQL 16-alpine (Docker)
- **Порт БД:** 5433
- **Порт API:** 7053 (HTTPS)
- **Инструмент тестирования:** Bruno (с поддержкой Secret Variables и .env)
- **Последнее обновление:** 11.09.2026 | Автор: Илюша