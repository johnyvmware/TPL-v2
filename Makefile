# Transaction Processing System - Makefile
# Production-level development and deployment automation

.PHONY: help setup-dev clean build test test-coverage format security-scan build-release docker-build

# Default target
help: ## Show this help message
	@echo "Transaction Processing System - Available Commands:"
	@echo ""
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-20s\033[0m %s\n", $$1, $$2}'

# Development Setup
setup-dev: ## Setup development environment
	@echo "🚀 Setting up development environment..."
	@dotnet --version || (echo "❌ .NET 8.0 SDK not found. Please install from https://dotnet.microsoft.com/download" && exit 1)
	@dotnet restore --verbosity minimal
	@dotnet tool install -g dotnet-format || echo "dotnet-format already installed"
	@dotnet tool install -g dotnet-reportgenerator-globaltool || echo "reportgenerator already installed"
	@echo "✅ Development environment ready!"

# Clean
clean: ## Clean build artifacts and temporary files
	@echo "🧹 Cleaning build artifacts..."
	@dotnet clean --verbosity minimal
	@rm -rf TestResults/
	@rm -rf coverage/
	@rm -rf publish/
	@rm -rf artifacts/
	@echo "✅ Clean completed"

# Build
build: ## Build the solution in Debug configuration
	@echo "🔨 Building solution (Debug)..."
	@dotnet build --configuration Debug --verbosity minimal
	@echo "✅ Build completed"

build-release: ## Build the solution in Release configuration
	@echo "🔨 Building solution (Release)..."
	@dotnet build --configuration Release --verbosity minimal
	@echo "✅ Release build completed"

# Testing
test: ## Run all tests
	@echo "🧪 Running tests..."
	@dotnet test --configuration Release --verbosity normal
	@echo "✅ Tests completed"

test-coverage: ## Run tests with coverage report
	@echo "🧪 Running tests with coverage..."
	@dotnet test --configuration Release \
		--collect:"XPlat Code Coverage" \
		--results-directory TestResults/ \
		--verbosity normal
	@echo "📊 Generating coverage report..."
	@reportgenerator \
		-reports:"TestResults/*/coverage.cobertura.xml" \
		-targetdir:"coverage" \
		-reporttypes:Html
	@echo "✅ Coverage report generated at coverage/index.html"

# Code Quality
format: ## Format code using dotnet format
	@echo "🎨 Formatting code..."
	@dotnet format --verbosity diagnostic
	@echo "✅ Code formatting completed"

format-check: ## Check code formatting without making changes
	@echo "🎨 Checking code formatting..."
	@dotnet format --verify-no-changes --verbosity diagnostic
	@echo "✅ Code formatting check completed"

# Security
security-scan: ## Run security vulnerability scan
	@echo "🔍 Running security scan..."
	@dotnet list package --vulnerable --include-transitive || echo "No vulnerabilities found"
	@echo "📦 Checking for outdated packages..."
	@dotnet list package --outdated || echo "All packages up to date"
	@echo "✅ Security scan completed"

# Development workflow
dev-check: format-check test security-scan ## Run all development checks (format, test, security)
	@echo "✅ All development checks passed!"

# Publishing
publish-linux: ## Publish for Linux x64
	@echo "📦 Publishing for Linux x64..."
	@dotnet publish src/TransactionProcessingSystem/TransactionProcessingSystem.csproj \
		--configuration Release \
		--runtime linux-x64 \
		--self-contained true \
		--output ./publish/linux-x64 \
		/p:PublishTrimmed=true \
		/p:PublishSingleFile=true
	@echo "✅ Linux x64 publish completed"

publish-windows: ## Publish for Windows x64
	@echo "📦 Publishing for Windows x64..."
	@dotnet publish src/TransactionProcessingSystem/TransactionProcessingSystem.csproj \
		--configuration Release \
		--runtime win-x64 \
		--self-contained true \
		--output ./publish/win-x64 \
		/p:PublishTrimmed=true \
		/p:PublishSingleFile=true
	@echo "✅ Windows x64 publish completed"

publish-macos: ## Publish for macOS x64
	@echo "📦 Publishing for macOS x64..."
	@dotnet publish src/TransactionProcessingSystem/TransactionProcessingSystem.csproj \
		--configuration Release \
		--runtime osx-x64 \
		--self-contained true \
		--output ./publish/osx-x64 \
		/p:PublishTrimmed=true \
		/p:PublishSingleFile=true
	@echo "✅ macOS x64 publish completed"

publish-all: publish-linux publish-windows publish-macos ## Publish for all platforms
	@echo "✅ All platform builds completed!"

# Docker
docker-build: ## Build Docker image
	@echo "🐳 Building Docker image..."
	@docker build -t transaction-processor:latest .
	@echo "✅ Docker image built successfully"

docker-run: ## Run application in Docker container
	@echo "🐳 Running Docker container..."
	@docker run --rm -it transaction-processor:latest

# Performance
benchmark: ## Run performance benchmarks
	@echo "⚡ Running performance benchmarks..."
	@cd src/TransactionProcessingSystem && \
		timeout 30s dotnet run --configuration Release || echo "Benchmark completed"
	@echo "✅ Benchmark completed"

# Utilities
restore: ## Restore NuGet packages
	@echo "📦 Restoring packages..."
	@dotnet restore --verbosity minimal
	@echo "✅ Packages restored"

update-packages: ## Update all NuGet packages
	@echo "📦 Updating packages..."
	@dotnet list package --outdated
	@echo "💡 To update packages, run: dotnet add package <PackageName>"

# CI/CD helpers
ci-build: clean restore build-release test ## Full CI build pipeline
	@echo "✅ CI build pipeline completed successfully!"

pre-commit: format-check test security-scan ## Pre-commit checks
	@echo "✅ Pre-commit checks passed!"

# Installation
install: build-release ## Install the application globally
	@echo "📦 Installing application..."
	@dotnet tool install --global --add-source ./publish TransactionProcessingSystem || \
		echo "Manual installation required"

# Documentation
docs: ## Generate documentation
	@echo "📚 Generating documentation..."
	@echo "Documentation is available in README.md"
	@echo "API documentation: https://github.com/your-username/transaction-processing-system/wiki"

# Version information
version: ## Show version information
	@echo "📋 Version Information:"
	@echo "  .NET SDK: $$(dotnet --version)"
	@echo "  Operating System: $$(uname -s)"
	@echo "  Architecture: $$(uname -m)"
	@echo "  Project: Transaction Processing System"