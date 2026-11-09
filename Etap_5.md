# Курсовое проектирование. Этап 5. Инспекция кода модулей проекта

**МДК:** 02.02 «Инструментальные средства разработки программного обеспечения»  
**Продолжительность:** 4 часа  
**Основной стек:** C# / .NET 9, Git  
**Форма:** этап курсового проектирования

## 1. Цель

Получить проверяемый результат по теме «Инспекция кода модулей проекта», связанный с собственным курсовым проектом «Разработка информационной системы фитнес-центра», и оформить его в репозитории так, чтобы решение можно было воспроизвести и проверить.

## 2. Предварительные условия

- тема курсового проекта согласована с преподавателем;
- создан репозиторий проекта (`https://github.com/Danilkaaa332/fitness-center-management-system`);
- установлен .NET SDK 9 и Git;
- структура проекта соответствует его фактическим задачам;
- зафиксировано исходное состояние репозитория перед началом этапа (`git status`, ветка `main`, тег `v0.4-debugging`).

---

## 3. Выполнение задания по инспекции кода

### 3.1. Исходное состояние и рабочая ветка

Перед началом этапа проверено состояние репозитория и создана рабочая ветка:

```bash
git status
git log -1 --oneline
git checkout -b refactor/stage5-code-inspection
```

### 3.2. Проверка модулей по всем 14 критериям инспекции

Проведена формализованная проверка проектов `FitnessClub.Contracts`, `FitnessClub.MembershipModule`, `FitnessClub.VisitModule` и `FitnessClub.Tests`:

1. **Понятность имен:** имена классов, методов и DTO соответствуют предметной области и стандартам C# (`PascalCase`, `_camelCase`, суффикс `Async`).
2. **Ответственность классов и методов:** принцип SRP соблюден (`MembershipService` — абонементы, `VisitService` — визиты, `Contracts` — DTO и интерфейсы).
3. **Направление зависимостей:** зависимости направлены строго к `FitnessClub.Contracts`, циклических ссылок между модулями нет.
4. **Дублирование логики:** устранено дублирование строковых литералов статусов и кодов ошибок за счет вынесения их в константы.
5. **Обработка `null`:** включен `<Nullable>enable</Nullable>`, добавлены проверки `ArgumentNullException.ThrowIfNull` в конструкторах и публичных методах.
6. **Валидация входных данных:** добавлена проверка `customerId`, `type`, `durationDays > 0`, `price >= 0` и непустых `Guid` при регистрации визита.
7. **Обработка исключений:** для некорректных аргументов создания абонемента выбрасываются `ArgumentException` / `ArgumentOutOfRangeException`, а для бизнес-отказов используется паттерн Result (`ValidationResult`, `RegisterVisitResult`).
8. **Использование `CancellationToken`:** в `CheckValidityAsync` и `RegisterVisitAsync` добавлен `CancellationToken cancellationToken = default` с вызовом `ThrowIfCancellationRequested()`.
9. **Работа с конфигурацией и секретами:** захардкоженные секреты и пароли отсутствуют, локальные настройки исключены в `.gitignore`.
10. **Корректность внешних контрактов:** возвращаемый кортеж в `VisitService.RegisterVisitAsync` заменен на типизированный рекорд `RegisterVisitResult` в `FitnessClub.Contracts`.
11. **Наличие тестов:** сохранены 5 тестов прошлых этапов и добавлены 2 новых теста на валидацию входных данных и `CancellationToken`.
12. **Отсутствие мертвого и закомментированного кода:** неиспользуемые методы, переменные и закомментированные строки отсутствуют.
13. **Соответствие `.editorconfig`:** подтверждено командой `dotnet format --verify-no-changes`.
14. **Предупреждения компилятора:** сборка `dotnet build` проходит с `0` предупреждений и `0` ошибок.

---

### 3.3. Таблица замечаний

| № | Место | Проблема | Риск | Рекомендация | Статус |
|---:|---|---|---|---|---|
| 1 | `IMembershipValidationService`, `MembershipService.cs`, `VisitService.cs` | Методы `CheckValidityAsync` и `RegisterVisitAsync` не принимают `CancellationToken` | Невозможность отменить долгий запрос при таймауте клиента | Добавить параметр `CancellationToken cancellationToken = default` и `ThrowIfCancellationRequested()` | Исправлено |
| 2 | `MembershipService.cs`, `VisitService.cs` | Отсутствует проверка ссылочных аргументов на `null` в конструкторах и публичных методах | Возникновение `NullReferenceException` внутри бизнес-логики | Добавить `ArgumentNullException.ThrowIfNull(...)` в конструкторах и методах | Исправлено |
| 3 | `MembershipService.CreateMembership` | Нет валидации `customerId`, `type`, `durationDays` и `price` | Создание абонементов с `durationDays <= 0`, `price < 0` или пустым типом | Добавить проверки с выбросом `ArgumentException` / `ArgumentOutOfRangeException` | Исправлено |
| 4 | `VisitService.RegisterVisitAsync`, `Contracts.cs` | Метод возвращает кортеж вместо DTO контракта и не проверяет `Guid.Empty` | Усложнение сериализации контракта и пропуск пустых идентификаторов | Ввести `record RegisterVisitResult` и проверку `Guid.Empty` | Исправлено |
| 5 | `MembershipService.cs`, `VisitService.cs` | Коды ошибок и статусы заданы «магическими строками» | Опечатки при сопоставлении кодов ошибок между модулями | Вынести константы в `FitnessErrorCodes`, `MembershipStatuses` и `VisitStatuses` | Исправлено |
| 6 | `MembershipService.cs` (`_memberships`), `VisitService.cs` (`_visits`) | Коллекции `Dictionary` и `List` не потокобезопасны | Состояние гонки (`Race Condition`) при параллельных запросах | Зафиксировать как ограничение in-memory прототипа до перехода на PostgreSQL | Принято к сведению |

---

### 3.4. Внесенные исправления в код и обоснование решений

**Обоснование решений:** вынесение кодов ошибок в константы и замена кортежа на позиционный `record RegisterVisitResult` повышают надежность межмодульного контракта на этапе компиляции, сохраняя обратную совместимость с деконструкцией в существующих тестах. Добавление `ArgumentNullException.ThrowIfNull` и валидации параметров реализует принцип *Fail Fast* (раннее обнаружение ошибок на границе модуля).

#### 1. `FitnessClub.Contracts/Contracts.cs`

```csharp
namespace FitnessClub.Contracts;

public static class MembershipStatuses
{
    public const string Active = "Active";
    public const string Blocked = "Blocked";
    public const string Frozen = "Frozen";
}

public static class VisitStatuses
{
    public const string Registered = "Registered";
    public const string Cancelled = "Cancelled";
}

public static class FitnessErrorCodes
{
    public const string InvalidRequest = "INVALID_REQUEST";
    public const string MembershipNotFound = "MEMBERSHIP_NOT_FOUND";
    public const string MembershipBlocked = "MEMBERSHIP_BLOCKED";
    public const string MembershipExpired = "MEMBERSHIP_EXPIRED";
    public const string DuplicateVisit = "DUPLICATE_VISIT";
}

public record RegisterVisitResult(bool Success, string? ErrorCode = null, VisitDto? Visit = null);

public interface IMembershipValidationService
{
    Task<ValidationResult> CheckValidityAsync(
        MembershipCheckRequest request,
        CancellationToken cancellationToken = default);
}
```

#### 2. `FitnessClub.MembershipModule/MembershipService.cs`

```csharp
public MembershipService(ILogger<MembershipService> logger)
{
    ArgumentNullException.ThrowIfNull(logger);
    _logger = logger;
}

public MembershipDto CreateMembership(
    Guid customerId,
    string type,
    DateTime startDate,
    int durationDays,
    decimal price,
    string status = MembershipStatuses.Active)
{
    if (customerId == Guid.Empty)
        throw new ArgumentException("Идентификатор клиента не может быть пустым.", nameof(customerId));
    if (string.IsNullOrWhiteSpace(type))
        throw new ArgumentException("Тип абонемента не может быть пустым.", nameof(type));
    if (durationDays <= 0)
        throw new ArgumentOutOfRangeException(nameof(durationDays), "Длительность должна быть больше 0.");
    if (price < 0)
        throw new ArgumentOutOfRangeException(nameof(price), "Стоимость не может быть отрицательной.");
    if (string.IsNullOrWhiteSpace(status))
        throw new ArgumentException("Статус абонемента не может быть пустым.", nameof(status));

    var membership = new MembershipDto(
        Guid.NewGuid(), customerId, type.Trim(), startDate, startDate.AddDays(durationDays), price, status.Trim());

    _memberships[membership.Id] = membership;
    return membership;
}

public Task<ValidationResult> CheckValidityAsync(
    MembershipCheckRequest request,
    CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(request);
    cancellationToken.ThrowIfCancellationRequested();

    if (!_memberships.TryGetValue(request.MembershipId, out var membership))
        return Task.FromResult(new ValidationResult(false, FitnessErrorCodes.MembershipNotFound));

    var normalizedStatus = membership.Status?.Trim() ?? string.Empty;
    if (!string.Equals(normalizedStatus, MembershipStatuses.Active, StringComparison.OrdinalIgnoreCase))
        return Task.FromResult(new ValidationResult(false, FitnessErrorCodes.MembershipBlocked));

    if (request.VisitDate.Date < membership.StartDate.Date || request.VisitDate.Date > membership.EndDate.Date)
        return Task.FromResult(new ValidationResult(false, FitnessErrorCodes.MembershipExpired));

    return Task.FromResult(new ValidationResult(true, null));
}
```

#### 3. `FitnessClub.VisitModule/VisitService.cs`

```csharp
public VisitService(IMembershipValidationService membershipValidator, ILogger<VisitService> logger)
{
    ArgumentNullException.ThrowIfNull(membershipValidator);
    ArgumentNullException.ThrowIfNull(logger);
    _membershipValidator = membershipValidator;
    _logger = logger;
}

public async Task<RegisterVisitResult> RegisterVisitAsync(
    RegisterVisitRequest request,
    CancellationToken cancellationToken = default)
{
    ArgumentNullException.ThrowIfNull(request);
    cancellationToken.ThrowIfCancellationRequested();

    if (request.CustomerId == Guid.Empty || request.MembershipId == Guid.Empty)
        return new RegisterVisitResult(false, FitnessErrorCodes.InvalidRequest, null);

    var validation = await _membershipValidator.CheckValidityAsync(
        new MembershipCheckRequest(request.MembershipId, request.VisitDate),
        cancellationToken);

    if (!validation.IsValid)
        return new RegisterVisitResult(false, validation.ErrorCode, null);

    if (_visits.Any(v => v.CustomerId == request.CustomerId && v.VisitDate.Date == request.VisitDate.Date))
        return new RegisterVisitResult(false, FitnessErrorCodes.DuplicateVisit, null);

    var visit = new VisitDto(Guid.NewGuid(), request.CustomerId, request.MembershipId, request.VisitDate, VisitStatuses.Registered);
    _visits.Add(visit);
    return new RegisterVisitResult(true, null, visit);
}
```

#### 4. Новые тесты в `FitnessClub.Tests/FitnessClubTests.cs`

```csharp
[Fact]
public void CreateMembership_WhenDurationOrPriceIsInvalid_ThrowsArgumentOutOfRangeException()
{
    var service = new MembershipService(NullLogger<MembershipService>.Instance);
    var startDate = new DateTime(2026, 10, 1);

    Assert.Throws<ArgumentOutOfRangeException>(() =>
        service.CreateMembership(Guid.NewGuid(), "Месячный", startDate, durationDays: 0, price: 3500m));
    Assert.Throws<ArgumentOutOfRangeException>(() =>
        service.CreateMembership(Guid.NewGuid(), "Месячный", startDate, durationDays: 30, price: -100m));
}

[Fact]
public async Task RegisterVisitAsync_WhenCancellationRequested_ThrowsOperationCanceledException()
{
    var membershipService = new MembershipService(NullLogger<MembershipService>.Instance);
    var visitService = new VisitService(membershipService, NullLogger<VisitService>.Instance);
    using var cts = new CancellationTokenSource();
    cts.Cancel();

    var request = new RegisterVisitRequest(Guid.NewGuid(), Guid.NewGuid(), new DateTime(2026, 10, 15));

    await Assert.ThrowsAsync<OperationCanceledException>(() =>
        visitService.RegisterVisitAsync(request, cts.Token));
}
```

---

### 3.5. Инструментальная проверка и фиксация в Git

Выполнены обязательные команды проверки:

```bash
dotnet format --verify-no-changes
dotnet build
dotnet test
```

- `dotnet format --verify-no-changes` — форматирование полностью соответствует `.editorconfig`.
- `dotnet build` — сборка успешна (`Предупреждений: 0, Ошибок: 0`).
- `dotnet test` — все 7 тестов пройдены успешно (`Пройдено: 7, Не пройдено: 0`).

---

## 4. Контрольные вопросы

1. Какой конкретный результат этапа можно независимо проверить?
2. Какие артефакты были созданы или обновлены?
3. Какими командами или тестами подтвержден результат?
4. Какие интеграционные риски уменьшены?
5. Какие изменения в будущем потребуют обновления артефактов этого этапа?
6. Как по истории Git определить выполненные действия?
7. Какие ошибки были найдены при самопроверке?
8. Что необходимо другому разработчику для повторения результата?

## 5. Вывод

В ходе этапа было достигнута цель получить проверяемый результат по теме «Инспекция кода модулей проекта», связанный с собственным курсовым проектом «Разработка информационной системы фитнес-центра», и оформить его в репозитории так, чтобы решение можно было воспроизвести и проверить.