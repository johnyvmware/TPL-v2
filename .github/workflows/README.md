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
- Validates non-draft pull requests only
- **Build, Test & Quality**: Solution build, test execution, coverage reporting, and code formatting
- **Security Scan**: Vulnerability assessment for dependencies and secret detection

### ⚡ Commit Validation (`commit-validation.yml`)
**Triggers:** Every commit to `main` branch
- Quick compilation check
- Basic test execution (continues on error)
- Commit message format validation (conventional commits)
- Fast feedback for development workflow

### 🤖 Dependabot (`dependabot.yml`)
**Schedule:** Weekly on Mondays at 9:00 AM
- Automatically updates NuGet packages
- Updates GitHub Actions versions
- Groups related dependencies for cleaner PRs

## Consistent Job Structure

Both CI and PR validation workflows now use the same organized job structure:

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

### 📦 Job 3: Build Artifacts (CI Only)
**Purpose:** Production artifact generation
- Application publishing for deployment
- Release artifact upload
- Only runs on main branch after other jobs succeed

## Aligned Quality Checks

Both workflows run identical quality checks in the same logical order:

### ✅ Build & Test
- Solution restoration and compilation
- Unit test execution with coverage collection
- Test result reporting and artifact upload
- Code coverage reporting to Codecov

### ✅ Code Quality
- Code formatting validation (`dotnet format --verify-no-changes`)
- Consistent code style enforcement

### ✅ Security Scanning
- NuGet package vulnerability scanning
- Secret detection using Gitleaks
- Dependency security assessment

### ✅ Artifacts & Reporting
- Test result artifacts for debugging
- Coverage reports for quality tracking
- Security scan logs for audit trails

## Workflow Status

All workflows provide status badges and detailed logs for debugging build issues. The workflows are designed to run in parallel where possible to minimize CI time.

### Status Badges
- **CI/CD Pipeline**: ![CI Status](https://github.com/johnyvmware/TPL-v2/actions/workflows/ci.yml/badge.svg)
- **PR Validation**: ![PR Status](https://github.com/johnyvmware/TPL-v2/actions/workflows/pr-validation.yml/badge.svg)
- **Code Coverage**: ![Code Coverage](https://codecov.io/gh/johnyvmware/TPL-v2/branch/main/graph/badge.svg)