# ============================================
# FlowClaude Build System
# ============================================

# --- Configuration ---
PROJECT_NAME      := FlowClaude
SOLUTION_FILE     := FlowClaude.sln
CONFIG            ?= Release
BUILD_NUMBER      ?= $(shell date +%Y%m%d%H%M%S)

# --- .NET Settings ---
DOTNET_ARGS       := --configuration $(CONFIG) --no-restore
DOTNET_SDK        := $(shell dotnet --version 2>/dev/null || echo "not found")

# --- Platform Detection ---
OS                := $(shell uname -s)
ifeq ($(OS),Darwin)
    PLATFORM       := osx
    ARCH           := x64
    RUNTIME        := osx-x64
    RUNTIME_IDENTIFIER := osx-x64
    PLATFORM_NAME  := macOS
else ifeq ($(OS),Linux)
    PLATFORM       := linux
    ARCH           := x64
    RUNTIME        := linux-x64
    RUNTIME_IDENTIFIER := linux-x64
    PLATFORM_NAME  := Linux
else
    PLATFORM       := windows
    ARCH           := x64
    RUNTIME        := win-x64
    RUNTIME_IDENTIFIER := win-x64
    PLATFORM_NAME  := Windows
endif

# --- Output Directories ---
OUTPUT_DIR        := publish/$(PLATFORM)-$(ARCH)
ARTIFACTS_DIR     := .artifacts

# --- Colors ---
RED    := \033[31m
GREEN  := \033[32m
YELLOW := \033[33m
BLUE   := \033[34m
CYAN   := \033[36m
RESET  := \033[0m

# ============================================
# Phony Targets
# ============================================
.PHONY: help info build build-dev build-all restore clean \
        run run-dev publish publish-osx publish-linux publish-windows \
        publish-all test test-coverage deps-check version

# ============================================
# Help & Info
# ============================================
help: info
	@echo ""
	@echo "Usage: make <target> [OPTIONS]"
	@echo ""
	@echo "Targets:"
	@echo "  help             Show this help message"
	@echo "  info             Show project and environment info"
	@echo "  build            Build solution (Release) - DEFAULT"
	@echo "  build-dev        Build solution in Debug mode"
	@echo "  build-all        Build all projects in solution"
	@echo "  restore          Restore NuGet dependencies"
	@echo "  run              Run the application (Release)"
	@echo "  run-dev          Run the application (Debug)"
	@echo "  publish          Publish for current platform"
	@echo "  publish-osx      Publish for macOS"
	@echo "  publish-linux    Publish for Linux"
	@echo "  publish-windows  Publish for Windows"
	@echo "  publish-all      Publish for all platforms"
	@echo "  test             Run unit tests"
	@echo "  test-coverage    Run tests with coverage report"
	@echo "  clean            Clean all build artifacts"
	@echo "  version          Show version info"
	@echo ""
	@echo "Options:"
	@echo "  CONFIG=Debug     Override build configuration"
	@echo "  BUILD_NUMBER=x   Set custom build number"
	@echo ""

info: version
	@echo ""
	@echo "=============================================="
	@echo "  $(PROJECT_NAME) Build Information"
	@echo "=============================================="
	@echo ""
	@echo "  Project:       $(PROJECT_NAME)"
	@echo "  Solution:      $(SOLUTION_FILE)"
	@echo "  Configuration: $(CONFIG)"
	@echo "  Platform:      $(PLATFORM_NAME) ($(ARCH))"
	@echo "  Runtime:       $(RUNTIME)"
	@echo "  .NET SDK:      $(DOTNET_SDK)"
	@echo "  Build Number:  $(BUILD_NUMBER)"
	@echo "  Output:        $(OUTPUT_DIR)"
	@echo ""

version:
	@echo "FlowClaude v1.0.0"

# ============================================
# Dependency Check
# ============================================
deps-check:
	@echo "$(CYAN)[$(PROJECT_NAME)]$(RESET) Checking dependencies..."
	@which dotnet > /dev/null 2>&1 && \
		echo "$(GREEN)[✓]$(RESET) .NET SDK found" || \
		echo "$(RED)[✗]$(RESET) .NET SDK not found - please install .NET 8.0+"
	@which git > /dev/null 2>&1 && \
		echo "$(GREEN)[✓]$(RESET) Git found" || \
		echo "$(RED)[✗]$(RESET) Git not found"

# ============================================
# Restore
# ============================================
restore:
	@echo "$(CYAN)[$(PROJECT_NAME)]$(RESET) Restoring NuGet dependencies..."
	@dotnet restore $(SOLUTION_FILE)
	@echo "$(GREEN)[✓]$(RESET) Dependencies restored"

# ============================================
# Build
# ============================================
build: deps-check restore
	@echo "$(CYAN)[$(PROJECT_NAME)]$(RESET) Building solution ($(CONFIG))..."
	@dotnet build $(SOLUTION_FILE) $(DOTNET_ARGS)
	@echo "$(GREEN)[✓]$(RESET) Build completed successfully"

build-dev:
	@echo "$(CYAN)[$(PROJECT_NAME)]$(RESET) Building solution (Debug)..."
	@dotnet build $(SOLUTION_FILE) --configuration Debug
	@echo "$(GREEN)[✓]$(RESET) Build completed successfully"

build-all:
	@echo "$(CYAN)[$(PROJECT_NAME)]$(RESET) Building all projects..."
	@dotnet build $(SOLUTION_FILE) --configuration $(CONFIG) --no-incremental
	@echo "$(GREEN)[✓]$(RESET) All projects built successfully"

# ============================================
# Run
# ============================================
run: build
	@echo "$(CYAN)[$(PROJECT_NAME)]$(RESET) Running application..."
	@dotnet run --project src/FlowClaude.App $(DOTNET_ARGS)

run-dev: build-dev
	@echo "$(CYAN)[$(PROJECT_NAME)]$(RESET) Running application (Debug mode)..."
	@dotnet run --project src/FlowClaude.App --configuration Debug

# ============================================
# Publish
# ============================================
publish: deps-check restore build
	@echo "$(CYAN)[$(PROJECT_NAME)]$(RESET) Publishing for $(PLATFORM_NAME)..."
	@mkdir -p $(OUTPUT_DIR)
	@dotnet publish src/FlowClaude.App \
		$(DOTNET_ARGS) \
		-r $(RUNTIME_IDENTIFIER) \
		--self-contained false \
		-p:PublishSingleFile=false \
		-o $(OUTPUT_DIR)
	@echo "$(GREEN)[✓]$(RESET) Published to $(OUTPUT_DIR)"
	@ls -la $(OUTPUT_DIR)

publish-osx: deps-check restore build
	@echo "$(CYAN)[$(PROJECT_NAME)]$(RESET) Publishing for macOS..."
	@mkdir -p publish/osx-x64
	@dotnet publish src/FlowClaude.App \
		$(DOTNET_ARGS) \
		-r osx-x64 \
		--self-contained false \
		-o publish/osx-x64
	@echo "$(GREEN)[✓]$(RESET) Published to publish/osx-x64"

publish-linux: deps-check restore build
	@echo "$(CYAN)[$(PROJECT_NAME)]$(RESET) Publishing for Linux..."
	@mkdir -p publish/linux-x64
	@dotnet publish src/FlowClaude.App \
		$(DOTNET_ARGS) \
		-r linux-x64 \
		--self-contained false \
		-o publish/linux-x64
	@echo "$(GREEN)[✓]$(RESET) Published to publish/linux-x64"

publish-windows: deps-check restore build
	@echo "$(CYAN)[$(PROJECT_NAME)]$(RESET) Publishing for Windows..."
	@mkdir -p publish/win-x64
	@dotnet publish src/FlowClaude.App \
		$(DOTNET_ARGS) \
		-r win-x64 \
		--self-contained false \
		-o publish/win-x64
	@echo "$(GREEN)[✓]$(RESET) Published to publish/win-x64"

publish-all: publish-osx publish-linux publish-windows
	@echo "$(GREEN)[✓]$(RESET) All platform builds completed"

# ============================================
# Self-Contained Publishing
# ============================================
publish-sc: deps-check restore build
	@echo "$(CYAN)[$(PROJECT_NAME)]$(RESET) Publishing self-contained for $(PLATFORM_NAME)..."
	@mkdir -p $(OUTPUT_DIR)-sc
	@dotnet publish src/FlowClaude.App \
		$(DOTNET_ARGS) \
		-r $(RUNTIME_IDENTIFIER) \
		--self-contained true \
		-p:PublishSingleFile=true \
		-o $(OUTPUT_DIR)-sc
	@echo "$(GREEN)[✓]$(RESET) Self-contained published to $(OUTPUT_DIR)-sc"

# ============================================
# Test
# ============================================
test:
	@echo "$(CYAN)[$(PROJECT_NAME)]$(RESET) Running unit tests..."
	@dotnet test $(SOLUTION_FILE) --configuration $(CONFIG) --no-build --verbosity normal
	@echo "$(GREEN)[✓]$(RESET) Tests completed"

test-coverage:
	@echo "$(CYAN)[$(PROJECT_NAME)]$(RESET) Running tests with coverage..."
	@dotnet test $(SOLUTION_FILE) \
		--configuration $(CONFIG) \
		--no-build \
		--collect:"XPlat Code Coverage" \
		-- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura
	@echo "$(GREEN)[✓]$(RESET) Coverage report generated"

# ============================================
# Clean
# ============================================
clean:
	@echo "$(CYAN)[$(PROJECT_NAME)]$(RESET) Cleaning build artifacts..."
	@dotnet clean $(SOLUTION_FILE) -v quiet 2>/dev/null || true
	@find . -type d -name bin -exec rm -rf {} + 2>/dev/null || true
	@find . -type d -name obj -exec rm -rf {} + 2>/dev/null || true
	@rm -rf $(ARTIFACTS_DIR) 2>/dev/null || true
	@rm -rf publish/* 2>/dev/null || true
	@echo "$(GREEN)[✓]$(RESET) Clean completed"

# ============================================
# Install (Homebrew for macOS)
# ============================================
install-homebrew:
	@echo "$(CYAN)[$(PROJECT_NAME)]$(RESET) Creating Homebrew formula..."
	@cat > Formula/flowclaude.rb <<-EOF
	class Flowclaude < Formula
	  desc "A modern, cross-platform desktop UI for Claude Code"
	  homepage "https://github.com/xiusin/FlowClaude"
	  url "https://github.com/xiusin/FlowClaude/releases/download/v1.0.0/FlowClaude-1.0.0-osx-x64.tar.gz"
	  sha256 ""
	  license "Apache-2.0"

	  depends_on :dotnet => :build

	  def install
	    bin.install "FlowClaude.App"
	  end
	end
	EOF
	@echo "$(GREEN)[✓]$(RESET) Homebrew formula created at Formula/flowclaude.rb"
