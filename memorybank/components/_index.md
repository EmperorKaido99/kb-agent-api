# Component Documentation

This folder contains detailed documentation for each feature/component in the system. Each file documents one logical component — its business rules, data flow, UI interactions, validation, and integration points.

## Purpose

When an agent needs to modify a feature, it reads the relevant component doc to understand:
- **What** the feature does (business rules, user-facing behavior)
- **How** data flows through the system (frontend → API → service → DB → external systems)
- **Where** the code lives (files, modules, layers involved)
- **Why** certain decisions were made (constraints, edge cases, gotchas)

## File Naming

Use kebab-case matching the feature name:
- `person-edit.md` — Editing a person record
- `duplicate-detection.md` — Finding and resolving duplicate entries
- `date-validation.md` — Date input rules and validation logic
- `authentication.md` — Auth flow across frontend and API

## Template

Each component doc should follow this structure:

---

# [Component/Feature Name]

## Overview
_(1–2 sentences: what this feature does from the user's perspective)_

## Business Rules
- Rule 1: ...
- Rule 2: ...
- Rule 3: ...

## Data Flow

```
[Trigger] → [Frontend Component] → [API Endpoint] → [Service] → [Repository/DB]
                                                   ↘ [External System]
```

### Step-by-step:
1. User does X in the UI
2. Frontend calls `POST /api/...`
3. Controller validates input, calls service
4. Service applies business logic
5. Repository persists changes
6. (Optional) External system is notified

## Validation Rules

| Field | Rule | Where Enforced | Error Message |
|-------|------|----------------|---------------|
| dateOfBirth | Must be in the past | API + Client | "Date must be before today" |

## Key Files

| File | Role |
|------|------|
| `src/Components/EditPerson.razor` | UI form |
| `src/Controllers/PersonController.cs` | API endpoint |
| `src/Services/PersonService.cs` | Business logic |

## Edge Cases & Gotchas
- _(Things that have caused bugs or confusion before)_

## Related Components
- [Link to related component doc]

---
