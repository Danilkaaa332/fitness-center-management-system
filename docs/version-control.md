# 16. Правила ведения репозитория

Для проекта используются следующие основные ветки:

```text
main
develop
feature/*
fix/*
docs/*
```

## main

Содержит стабильную версию проекта.

## develop

Используется для объединения результатов разработки.

## feature/*

Используется для разработки новых функций.

Примеры:

```text
feature/visit-tracking
feature/customer-crud
```

## fix/*

Используется для исправления дефектов.

Например:

```text
fix/membership-date-calc
```

## docs/*

Используется при изменениях документации.

---

# 17. Правила коммитов

Один коммит должен содержать одно логически завершенное изменение.

Используются следующие обозначения:

```text
feat: новая функциональность
fix: исправление
docs: документация
test: тесты
refactor: изменение структуры кода
chore: техническое изменение
```

Примеры:

```text
docs: add project scope
docs: add architecture diagram
feat: add membership DTOs
test: add validation tests
```