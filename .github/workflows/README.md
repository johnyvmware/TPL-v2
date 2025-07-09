# GitHub Actions Workflows

This repository uses GitHub Actions for continuous integration and validation.

## Workflows

### 🔄 CI (`ci.yml`)
**Triggers:** Push to `main` branch, Manual dispatch
- **Build, Test & Quality**: Solution build, test execution, coverage reporting, and code formatting
- **Security Scan**: Vulnerability assessment for dependencies and secret detection
- **Build Artifacts**: Application publishing (main branch only, depends on other jobs)

### 🔍 Pull Request Validation (`pr-validation.yml`)
**Triggers:** Pull request events targeting `main` branch (opened, synchronized, reopened, ready_for_review)
- **Build, Test & Quality**: Solution build, test execution, coverage reporting, and code formatting
- **Security Scan**: Vulnerability assessment for dependencies and secret detection

### 🤖 Dependabot (`dependabot.yml`)
**Schedule:** Weekly on Mondays at 9:00 AM
- Automatically updates NuGet packages
- Updates GitHub Actions versions
- Groups related dependencies for cleaner PRs

##  Job Structure
### 🏗️ Job 1: Build, Test & Quality
**Purpose:** Core development workflow validation
- Solution restoration and compilation
- Unit test execution with coverage collection
- Test result reporting and artifact upload
- Code coverage reporting to Codecov
- Code formatting validation (`dotnet format --verify-no-changes`)

### 🔒 Job 2: Security Scan
**Purpose:** Security and compliance validation
- NuGet package vulnerability scanning
- Secret detection using Gitleaks
- Dependency security assessment
- Security scan logs for audit trails

### 📦 Job 3: Build Artifacts
**Purpose:** Production artifact generation
- Application publishing for deployment
- Release artifact upload
- Only runs on main branch after other jobs succeed

## Workflow Status

All workflows provide status badges and detailed logs for debugging build issues. The workflows are designed to run in parallel where possible to minimize CI time.

### Status Badges
- **CI/CD Pipeline**: ![CI Status](https://github.com/johnyvmware/TPL-v2/actions/workflows/ci.yml/badge.svg)
- **PR Validation**: ![PR Status](https://github.com/johnyvmware/TPL-v2/actions/workflows/pr-validation.yml/badge.svg)
- **Code Coverage**: ![Code Coverage](https://codecov.io/gh/johnyvmware/TPL-v2/branch/main/graph/badge.svg)