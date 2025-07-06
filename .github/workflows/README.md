# GitHub Actions Workflows

This repository uses GitHub Actions for continuous integration and validation.

## Workflows

### 🔄 CI (`ci.yml`)
**Triggers:** Push to `main` or `develop` branches, Pull requests to `main` or `develop`
- **Build and Test**: Full solution build with test execution
- **Code Quality**: Format validation and code analysis  
- **Security Scan**: Vulnerability assessment for dependencies

### 🔍 Pull Request Validation (`pr-validation.yml`)
**Triggers:** Pull request events (opened, synchronized, reopened, ready_for_review)
- Validates non-draft pull requests only
- Runs comprehensive tests with result reporting
- Checks for breaking changes and validates project structure
- Enforces code quality standards (no TODO/FIXME/HACK comments)

### ⚡ Commit Validation (`commit-validation.yml`)
**Triggers:** Every commit to any branch
- Quick compilation check
- Basic test execution (continues on error)
- Commit message format validation (conventional commits)
- Fast feedback for development workflow

### 🤖 Dependabot (`dependabot.yml`)
**Schedule:** Weekly on Mondays at 9:00 AM
- Automatically updates NuGet packages
- Updates GitHub Actions versions
- Groups related dependencies for cleaner PRs

## Workflow Status

All workflows provide status badges and detailed logs for debugging build issues. The workflows are designed to run in parallel where possible to minimize CI time.