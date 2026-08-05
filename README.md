# kb-agent-api

## Development Workflow (D5)

All development follows the **D5 agentic workflow** with continuous progress tracking.

| Command | Description |
|---------|-------------|
| `StartTask TASK-XXXX` | Start a new task through the D5 phases |
| `ReviewTasks` | Review all incomplete tasks and resume where you left off |

### Agent Configuration

| File | Purpose |
|------|--------|
| `.github/copilot-instructions.md` | D5 workflow definition, task tracking, phase gates |
| `agents.md` | Behavioral guidelines — think first, simplicity, surgical changes |
| `project-context.md` | Tech stack rules, coding conventions, anti-patterns |

### Documentation (Memory Bank)

| File | Contents |
|------|----------|
| `memorybank/architecture.md` | Solution structure, layers, key design decisions |
| `memorybank/features.md` | Feature list and high-level behavior |
| `memorybank/integrations.md` | External services and APIs |
| `memorybank/setup.md` | Local development setup |
| `memorybank/ticket-progress.md` | Status tracker for all tickets across sessions |
| `memorybank/changelog.md` | Ad-hoc changes outside task files |
| `memorybank/components/` | Per-feature documentation (business rules, data flow, validation) |

### Component Documentation

| File | Description |
|------|-------------|
| `memorybank/components/_index.md` | Template and conventions for per-feature docs (no components documented yet) |
