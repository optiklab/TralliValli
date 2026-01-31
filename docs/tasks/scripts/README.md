# Scripts

This directory contains utility scripts for the TralliValli project.

## create-issues.sh

Script to create GitHub issues from the project roadmap.

### Prerequisites

- [GitHub CLI (gh)](https://cli.github.com/) installed
- GitHub CLI authenticated (`gh auth login`)
- Write permissions to the repository

### Usage

```bash
# Dry run (shows what would be created without creating issues)
./create-issues.sh --dry-run

# Actually create issues
./create-issues.sh
```

### Note

The current script creates Phase 1 issues as an example (Tasks 1-10). To create all 74 issues, you need to extend the script with the remaining phases (2-9). See `docs/PROJECT_ROADMAP.md` for complete task details.

### Labels

The script uses these labels (create them in GitHub first):
- backend
- frontend
- infrastructure
- setup
- docker
- database
- mongodb
- redis
- rabbitmq
- authentication
- jwt
- api
- signalr
- real-time
- workers
- messaging
- presence
- conversations
- notifications
- archival
- backup
- azure
- storage
- react
- typescript
- websocket
- http
- state-management
- offline
- ui
- files
- media
- encryption
- e2ee
- key-management
- invites
- qr-code
- registration
- email
- bicep
- deployment
- production
- ssl
- security
- ci
- cd
- github-actions
- monitoring
- testing
- unit-tests
- integration-tests
- component-tests
- hook-tests
- e2e
- playwright
- domain
- entities
- services
- test-data
- factories
- documentation
- architecture
- user-guide

### Milestones

Create these milestones in GitHub:
- Phase 1: Backend Foundation
- Phase 2: Real-Time Messaging Backend
- Phase 3: Message Retention, Archival & Backup
- Phase 4: Web Client
- Phase 5: End-to-End Encryption
- Phase 6: File Sharing & Media
- Phase 7: Azure Deployment
- Phase 8: Testing
- Phase 9: Documentation

## Alternative: Manual Issue Creation

If you prefer to create issues manually:

1. Review `docs/PROJECT_ROADMAP.md` for the complete list of 74 tasks
2. For each task, create a GitHub issue with:
   - Title: "Task N: [Task Title]"
   - Body: Task description and acceptance criteria from roadmap
   - Labels: As specified in roadmap
   - Milestone: Phase number
3. Individual task templates are available in `docs/tasks/` directory
