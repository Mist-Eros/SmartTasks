# SmartTasks

A personal task manager that parses natural-language input into structured tasks using Google Gemini, with confirm-then-save editing.

![Screenshot](docs/screenshot.png)

## What it does

- Type a task in natural language (e.g. `meeting with Sara next Tuesday at 2pm, high priority`)
- Gemini extracts the title, date, time, priority, and tags
- Review the parsed result, then save it to your list
- Edit or delete saved tasks inline

## Tech stack

- **.NET 10** — hosted Blazor WebAssembly (Client + Server + Shared)
- **Google Gemini** (`gemini-3.6-flash`) — natural-language task parsing
- **Entity Framework Core + SQLite** — task persistence
- **Bootstrap 5 + Bootstrap Icons** — base styling, customized with a dark theme

## Architecture

Hosted Blazor WebAssembly: the Server project serves both the API and the Client's static files from the same origin. The Gemini API key lives in User Secrets on the server and never reaches the browser.

## Getting started

Prerequisites: .NET 10 SDK, a Google Gemini API key.

```bash
git clone https://github.com/Mist-Eros/SmartTasks.git
cd SmartTasks/SmartTasks.Server
dotnet user-secrets set "Gemini:ApiKey" "YOUR_KEY_HERE"
dotnet ef database update
dotnet run --launch-profile https
```

Open https://localhost:7107

## Project structure

```
SmartTasks.sln
SmartTasks.Client/    Blazor WebAssembly frontend
SmartTasks.Server/    ASP.NET Core Web API + Gemini integration + EF Core
SmartTasks.Shared/    DTOs shared between Client and Server
```

## Roadmap

- [x] Natural-language task parsing via Gemini
- [x] Task persistence (SQLite + EF Core)
- [x] Inline edit and delete
- [x] Dark theme
- [ ] User accounts and authentication
- [ ] Voice input via Web Speech API
- [ ] Deployment
