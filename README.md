# SmartTasks

A voice-enabled task manager that parses natural language into structured tasks using Google Gemini.

## What it does

- Type a task in natural language (e.g. `meeting with Sara next Tuesday at 2pm, high priority`)
- Or record it with the microphone — voice is transcribed by Gemini and dropped into the input
- Gemini extracts the title, date, time, priority, and tags
- Review the parsed result, then save it to your list
- Edit or delete saved tasks inline
- Sort by date, priority, or filter by tag using the triangular sort widget
- Create an account — each user sees only their own tasks

## Tech stack

- **.NET 10** — hosted Blazor WebAssembly (Client + Server + Shared)
- **Google Gemini** (`gemini-3.5-flash-lite` for parsing, `gemini-3.6-flash` for audio transcription)
- **Entity Framework Core + SQLite** — task and user persistence
- **Cookie authentication** with `PasswordHasher<User>`
- **Bootstrap 5 + Bootstrap Icons** — base styling, customized with a dark cream/red theme
- **MediaRecorder API** + JS interop — browser audio recording, WAV encoding in JavaScript

## Architecture

Hosted Blazor WebAssembly: the Server project serves both the API and the Client's static files from the same origin. The Gemini API key lives in User Secrets on the server and never reaches the browser. Auth uses HTTP-only cookies, so the WASM client doesn't handle tokens at all.

## Getting started

Prerequisites:
- .NET 10 SDK
- A Google Gemini API key (free at https://aistudio.google.com/apikey)

```bash
git clone https://github.com/Mist-Eros/SmartTasks.git
cd SmartTasks/SmartTasks.Server
dotnet user-secrets set "Gemini:ApiKey" "YOUR_KEY_HERE"
dotnet ef database update
dotnet run --launch-profile https
```

Open https://localhost:7107 in a browser. Register an account and start adding tasks.

## Project structure

```
SmartTasks.sln
SmartTasks.Client/           Blazor WebAssembly frontend
  Pages/                     Home, Auth
  Layout/                    MainLayout (header + welcome/logout)
  wwwroot/js/                audioRecorder.js — MediaRecorder + WAV encoder
SmartTasks.Server/           ASP.NET Core Web API
  Data/                      AppDbContext, TaskEntity, User, TaskMapper
  Services/                  GeminiService (parse + transcribe)
  Endpoints/                 AuthEndpoints (register, login, logout, me)
  Migrations/                EF Core migrations
SmartTasks.Shared/           TaskDto, ParseRequest
```

## Build log

A step-by-step record of how the app was built, in order:

1. **Scaffold** — three-project hosted Blazor WASM solution (Client / Server / Shared). Manual setup, no template, because .NET 10 removed the `-ho` flag.
2. **Middleware pipeline** — wired `UseBlazorFrameworkFiles` → `UseStaticFiles` → endpoints → `MapFallbackToFile`. Learned why the order matters (Blazor framework files must come before static files, and the SPA fallback must be last).
3. **Gemini integration** — first via a throwaway `/api/gemini-test` endpoint to prove the key worked, then replaced with a real `GeminiService` and a system prompt for JSON extraction.
4. **Persistence** — EF Core + SQLite, a `TaskEntity` separate from `TaskDto`, and a `TaskMapper` for the conversion. Migrations from the start, not `EnsureCreated`.
5. **Confirm-then-save** — parse returns a DTO with `Id = 0`; the user reviews and clicks Save; then `POST /api/tasks` persists it. Classic human-in-the-loop pattern.
6. **Edit and delete** — `PUT` and `DELETE` endpoints, inline edit form in Blazor, two-click delete confirmation.
7. **Dark theme** — Bootstrap 5 overridden with a warm dark palette (`#1a1815` background, `#ebe3c6` cream text, `#fc3b49` red accent), pill buttons, rounded cards, `Inter` font, animated polka dot background.
8. **Relative time handling** — the first version of the system prompt didn't understand "in 2 hours", and the server was using `DateTime.UtcNow` instead of local time. Fixed by sending `DateTime.Now` in the prompt and adding duration rules.
9. **Star-based priority** — 5 priority levels rendered as 3 or 4 stars, cream for normal, red for `critical`.
10. **Sort widget** — a triangular toggle with three corner buttons (clock, fire, tag) and a sliding knob. Custom-built, no library.
11. **Auth** — cookie authentication, `PasswordHasher<User>`, register / login / logout, per-user task isolation via a `UserId` foreign key.
12. **Voice input** — `MediaRecorder` in the browser records WebM, converts to WAV in JavaScript (because Gemini's OpenAI-compatible endpoint only accepts wav/mp3), sends to `/api/transcribe`, which forwards to Gemini and returns text into the textarea.

## What I learned

- **Middleware order in ASP.NET Core matters.** `UseBlazorFrameworkFiles` before `UseStaticFiles` or the `_framework/*` files 404. `MapFallbackToFile` must be last or it eats the API routes.
- **Separate your DTO from your entity.** `TaskDto` has no `CreatedAt` or `UserId`; `TaskEntity` does. The mapper is where they meet.
- **Never trust an LLM's JSON output.** Gemini usually returns valid JSON, but sometimes wraps it in markdown fences. Strip them before deserializing, catch parse failures, log the raw response.
- **`DateTime.UtcNow` is not `DateTime.Now`.** The first version of the parser was off by a day because the server was in Romania but the code was using UTC.
- **Don't hardcode URLs in WASM.** `HostEnvironment.BaseAddress` lets the client call the server on whatever origin it was served from — works in dev, works if you ever deploy.
- **Cookie auth vs JWT is a fit question, not a quality question.** Same-origin browser app → cookies. Multi-client API → JWT.
- **Some browser APIs aren't universal.** The Web Speech API (the easy voice path) only works in Chrome/Edge/Safari. `MediaRecorder` works everywhere, so we used that instead and transcribed server-side.
- **Read OpenCode's diffs.** It writes code fast, but it doesn't always know what you actually want. Checking what it changed before running it saved a lot of debugging.

## Future ideas

- Deployment — someday, unknown when. Would need Postgres (Render's free tier wipes the filesystem on restart) and a Dockerfile.
- A calendar grid view of tasks by date.
- Recurring tasks.
- Push notifications for tasks due today.
