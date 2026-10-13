# Курсовое проектирование. Этап 4. Отладка отдельных модулей программного проекта

**МДК:** 02.02 «Инструментальные средства разработки программного обеспечения»  
**Продолжительность:** 4 часа  
**Основной стек:** C# / .NET 9, Git  
**Форма:** этап курсового проектирования

## 1. Цель

Получить проверяемый результат по теме «Отладка отдельных модулей программного проекта», связанный с собственным курсовым проектом «Разработка информационной системы фитнес-центра», и оформить его в репозитории так, чтобы решение можно было воспроизвести и проверить.

## 2. Предварительные условия

- тема курсового проекта согласована с преподавателем;
- создан репозиторий проекта;
- установлен .NET SDK 9 и Git;
- структура проекта соответствует его фактическим задачам;
- зафиксировано исходное состояние репозитория перед началом этапа (`git status`, ветка `main`, тег `v0.3-modules-integration`).

---

## 3. Выполнение задания по систематической отладке модулей

В рамках этапа проведена диагностика и отладка двух дефектов в отдельных модулях системы:
- **Случай № 1:** Модуль `FitnessClub.MembershipModule` (`MembershipService.cs`) — пропуск неактивных абонементов со статусом `"Frozen"` или со случайными пробелами (`" Blocked "`).
- **Случай № 2:** Модуль `FitnessClub.VisitModule` (`VisitService.cs`) — дублирование посещений одного клиента при повторном сканировании абонемента в один и тот же день.

---

### Случай № 1. Отладка модуля `FitnessClub.MembershipModule`

#### 3.1.1. Симптом
При регистрации визита клиента, чей абонемент переведен в статус заморозки (`"Frozen"`) или чей статус `" Blocked "` содержит ведущие/завершающие пробелы после пользовательского ввода, система ошибочно разрешает проход (`IsValid = true`) и регистрирует посещение.

#### 3.1.2. Версия / коммит
Ветка `main`, релизный тег 3-го этапа `v0.3-modules-integration` (состояние кода после завершения интеграции модулей).

#### 3.1.3. Условия воспроизведения
1. Создать экземпляр `MembershipService`.
2. Вызвать метод `CreateMembership(...)`, передав статус `status: "Frozen"` или `status: " Blocked "`.
3. Вызвать `CheckValidityAsync(new MembershipCheckRequest(membership.Id, visitDate))` на дату внутри интервала действия абонемента.
4. Метод возвращает `ValidationResult(IsValid: true, ErrorCode: null)` вместо отказа `MEMBERSHIP_BLOCKED`.

#### 3.1.4. Минимум две гипотезы
- **Гипотеза 1:** В `VisitService.RegisterVisitAsync` некорректно интерпретируется поле `validation.IsValid`, из-за чего отрицательный результат проверки игнорируется.
- **Гипотеза 2:** В методе `MembershipService.CheckValidityAsync` проверка статуса реализована по «черному списку» (сравнение только со строкой `"Blocked"` без `Trim()`), вместо проверки «белого списка» единственно допустимого статуса `"Active"`.

#### 3.1.5. Выбранный инструмент диагностики
Отладчик Visual Studio (условная точка останова в методе `MembershipService.CheckValidityAsync`, окно локальных переменных **Locals / Watch**) и контекстный журнал `ILogger<MembershipService>`.

#### 3.1.6. Наблюдаемый факт, подтверждающий первопричину
При остановке на точке останова на строке проверки статуса в окне **Watch** зафиксировано значение `membership.Status = "Frozen"` (и во втором прогоне `" Blocked "`). Выражение:

```csharp
string.Equals(membership.Status, "Blocked", StringComparison.OrdinalIgnoreCase)
```

вычисляется в `false`, после чего управление переходит к проверке дат и возвращает `new ValidationResult(true, null)`. В логах `ILogger` выводится запись: `"Абонемент {MembershipId} успешно прошел проверку"`. Гипотеза № 2 полностью подтвердилась.

#### 3.1.7. Исправление
В файле `FitnessClub.MembershipModule/MembershipService.cs` условие проверки статуса заменено на нормализацию строки через `Trim()` и проверку на соответствие статусу `"Active"`:

```csharp
var normalizedStatus = membership.Status?.Trim() ?? string.Empty;
if (!string.Equals(normalizedStatus, "Active", StringComparison.OrdinalIgnoreCase))
{
    _logger.LogWarning(
        "Абонемент {MembershipId} недоступен для посещения (Статус: {Status})",
        request.MembershipId, membership.Status);
    return Task.FromResult(new ValidationResult(false, "MEMBERSHIP_BLOCKED"));
}
```

#### 3.1.8. Регрессионный тест
В проект `FitnessClub.Tests` (`FitnessClubTests.cs`) добавлен регрессионный тест `MembershipService_WhenStatusIsFrozenOrHasSpaces_ReturnsMembershipBlockedError`:

```csharp
[Fact]
public async Task MembershipService_WhenStatusIsFrozenOrHasSpaces_ReturnsMembershipBlockedError()
{
    var service = new MembershipService(NullLogger<MembershipService>.Instance);
    var startDate = new DateTime(2026, 10, 1);
    var frozenMembership = service.CreateMembership(Guid.NewGuid(), "Годовой", startDate, 365, 25000m, status: "Frozen");
    var spacedBlockedMembership = service.CreateMembership(Guid.NewGuid(), "Месячный", startDate, 30, 3500m, status: " Blocked ");

    var resultFrozen = await service.CheckValidityAsync(new MembershipCheckRequest(frozenMembership.Id, new DateTime(2026, 10, 15)));
    var resultSpaced = await service.CheckValidityAsync(new MembershipCheckRequest(spacedBlockedMembership.Id, new DateTime(2026, 10, 15)));

    Assert.False(resultFrozen.IsValid);
    Assert.Equal("MEMBERSHIP_BLOCKED", resultFrozen.ErrorCode);
    Assert.False(resultSpaced.IsValid);
    Assert.Equal("MEMBERSHIP_BLOCKED", resultSpaced.ErrorCode);
}
```

#### 3.1.9. Вывод, почему симптом отличался от первопричины
Внешний симптом выглядел как ошибка модуля регистрации посещений (`VisitModule`), который пропускал клиента с недействительным абонементом. Однако первопричина крылась внутри модуля `MembershipModule`: логическая ошибка фильтрации по единственному запрещенному значению `"Blocked"` вместо проверки разрешенного состояния `"Active"` и отсутствие очистки строки от пробелов (`Trim()`).

---

### Случай № 2. Отладка модуля `FitnessClub.VisitModule`

#### 3.2.1. Симптом
При повторном прикладывании клубной карты на турникете в тот же день в списке посещений (`GetVisits()`) создаются дублирующиеся записи `VisitDto` для одного и того же клиента на одну и ту же дату.

#### 3.2.2. Версия / коммит
Ветка `fix/stage4-debugging` (после исправления первого дефекта).

#### 3.2.3. Условия воспроизведения
1. Создать активный абонемент для клиента `customerId`.
2. Дважды вызвать метод `visitService.RegisterVisitAsync(request)` с одинаковыми `CustomerId`, `MembershipId` и датой визита `2026-10-15` (например, в `10:00` и в `10:01`).
3. Оба вызова возвращают `Success = true`, а `visitService.GetVisits()` содержит 2 визита одного клиента за один день.

#### 3.2.4. Минимум две гипотезы
- **Гипотеза 1:** Метод `CreateMembership` или `RegisterVisitAsync` генерирует одинаковый `Guid`, но коллекция `List<VisitDto>` допускает хранение полных дубликатов объектов.
- **Гипотеза 2:** В методе `VisitService.RegisterVisitAsync` отсутствует проверка наличия уже зарегистрированного визита данного клиента на календарную дату `request.VisitDate.Date` перед добавлением новой записи в коллекцию `_visits`.

#### 3.2.5. Выбранный инструмент диагностики
Точка останова в методе `VisitService.RegisterVisitAsync`, анализ стека вызовов (**Call Stack**), инспекция коллекции `_visits` в окне отладчика и анализ записей `ILogger<VisitService>`.

#### 3.2.6. Наблюдаемый факт, подтверждающий первопричину
При втором вызове `RegisterVisitAsync` в отладчике видно, что коллекция `_visits` уже содержит элемент с тем же `CustomerId` и `VisitDate.Date == 2026-10-15`. Однако после успешного ответа от `_membershipValidator.CheckValidityAsync` метод безусловно создает новый объект `VisitDto` с новым `Guid.NewGuid()` и выполняет `_visits.Add(visit)`. Гипотеза № 2 полностью подтвердилась.

#### 3.2.7. Исправление
В метод `RegisterVisitAsync` класса `VisitService` (`FitnessClub.VisitModule/VisitService.cs`) добавлена проверка на дубликат посещения клиента в текущую дату с записью предупреждения в лог и возвратом кода ошибки `"DUPLICATE_VISIT"`:

```csharp
if (_visits.Any(v => v.CustomerId == request.CustomerId && v.VisitDate.Date == request.VisitDate.Date))
{
    _logger.LogWarning(
        "Отказ в регистрации повторного визита для клиента {CustomerId} на дату {VisitDate:yyyy-MM-dd}. Код ошибки: DUPLICATE_VISIT",
        request.CustomerId, request.VisitDate);

    return (false, "DUPLICATE_VISIT", null);
}
```

#### 3.2.8. Регрессионный тест
В проект `FitnessClub.Tests` (`FitnessClubTests.cs`) добавлен регрессионный интеграционный тест `VisitIntegration_WhenDuplicateVisitOnSameDay_RejectsSecondVisit`:

```csharp
[Fact]
public async Task VisitIntegration_WhenDuplicateVisitOnSameDay_RejectsSecondVisit()
{
    var membershipService = new MembershipService(NullLogger<MembershipService>.Instance);
    var visitService = new VisitService(membershipService, NullLogger<VisitService>.Instance);

    var customerId = Guid.NewGuid();
    var membership = membershipService.CreateMembership(customerId, "Месячный безлимит", new DateTime(2026, 10, 1), 30, 3500m);

    var firstRequest = new RegisterVisitRequest(customerId, membership.Id, new DateTime(2026, 10, 15, 10, 0, 0));
    var secondRequest = new RegisterVisitRequest(customerId, membership.Id, new DateTime(2026, 10, 15, 18, 30, 0));

    var firstResult = await visitService.RegisterVisitAsync(firstRequest);
    var secondResult = await visitService.RegisterVisitAsync(secondRequest);

    Assert.True(firstResult.Success);
    Assert.False(secondResult.Success);
    Assert.Equal("DUPLICATE_VISIT", secondResult.ErrorCode);
    Assert.Single(visitService.GetVisits());
}
```

#### 3.2.9. Вывод, почему симптом отличался от первопричины
Симптом проявлялся в итоговой статистике посещений как «задвоение записей в хранилище `_visits`», что могло указывать на ошибку структуры данных или повторную отправку события. Первопричиной же оказалось отсутствие бизнес-правила идемпотентности (контроля уникальности связки `CustomerId + VisitDate.Date`) в самом сервисе `VisitService` до сохранения записи.

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

Этап считается завершенным, когда результат существует не только в коде или словах автора, но и подтверждается репозиторием, документацией и проверками.