# FlowClaude

A modern, cross-platform desktop UI for Claude Code with native TTY support, Git Worktree isolation, and Fluent Design.

## Features

- 🤖 **Claude Agent Integration** - Full support for Plan and Agent modes
- 🌍 **Git Worktree Isolation** - Each workspace runs in its own isolated git worktree
- 💻 **Integrated Terminal** - Native TTY with xterm-style interface
- 📝 **Diff Preview** - Real-time git diff and file change tracking
- 🎨 **Fluent Design** - Modern UI with dark/light theme support
- 🔒 **Local-First** - All data stays on your machine
- 📦 **Cross-Platform** - Runs on macOS, Windows, and Linux

## Architecture

```
FlowClaude/
├── src/
│   ├── FlowClaude.Core/         # Domain models and interfaces
│   ├── FlowClaude.Infrastructure/ # Database, Git, Terminal services
│   ├── FlowClaude.Agent/        # Claude API integration
│   └── FlowClaude.App/          # Avalonia UI application
├── test/
└── FlowClaude.sln
```

## Building

### Prerequisites

- .NET 8.0 SDK or later
- Git
- Xcode Command Line Tools (macOS)

### Build & Run

```bash
# Clone the repository
git clone https://github.com/yourusername/FlowClaude.git
cd FlowClaude

# Build the solution
dotnet build

# Run the application
dotnet run --project src/FlowClaude.App
```

### Publishing

```bash
# macOS
dotnet publish src/FlowClaude.App -c Release -r osx-x64 -o publish/osx

# Windows
dotnet publish src/FlowClaude.App -c Release -r win-x64 -o publish/windows

# Linux
dotnet publish src/FlowClaude.App -c Release -r linux-x64 -o publish/linux
```

## Configuration

Set the following environment variables:

- `ANTHROPIC_API_KEY` - Your Anthropic API key for Claude
- `FLOWCLAUDE_DATA_DIR` - Custom data directory (default: `~/.flowclaude`)

## License

Apache License 2.0
