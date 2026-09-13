# CLAUDE.md

Guidance for Claude Code when working in this repository.

## What this is

A personal, single-user job application tracker. Blazor WebAssembly (.NET 9), deployed as a
static site to **GitHub Pages** at `https://shivasanthosh.github.io/job-tracker/`. No custom
backend — auth and storage are **Firebase** (Google sign-in + Firestore), called directly from
the browser. There is no test project.

This repo is a sibling of `aboutme` (Shiva's portfolio) but intentionally standalone — it is
not part of the portfolio site or its build.

## Commands

```sh
dotnet build UI/UI.csproj
dotnet run --project UI/UI.csproj                    # dev server at http://localhost:5217
dotnet publish UI/UI.csproj -c Release -o release    # what CI runs
```

Deployment is automatic on push to `main` via `.github/workflows/deploy.yml`, publishing
`release/wwwroot` to `gh-pages`. Never commit to `gh-pages` directly.

## Architecture

- `UI/` is the only project. Single page (`UI/Pages/Home.razor` + `Home.razor.cs`, code-behind
  convention like `aboutme`) with two states: signed out (Google sign-in button) and signed in
  (applications table + add/edit form).
- **Auth + data live in JS, not C#**: `UI/wwwroot/js/firebase-interop.js` is an ES module that
  imports the Firebase SDK straight from `gstatic.com` (no npm/bundler) and exposes plain
  functions on `window.jobTracker` (`signIn`, `signOutUser`, `registerAuthCallback`,
  `registerApplicationsListener`, `addApplication`, `updateApplication`, `deleteApplication`).
  `Home.razor.cs` calls these via `IJSRuntime.InvokeVoidAsync`, and Firestore's realtime
  listener calls back into `Home` via `DotNetObjectReference` + `[JSInvokable]`
  (`OnAuthStateChanged`, `OnApplicationsChanged`).
- `UI/wwwroot/js/firebase-config.js` holds the Firebase web config (project ID, API key, etc.).
  **Not a secret** — safe to commit as-is; Firebase access control is `firestore.rules`
  (repo root), which restricts every read/write to one Google account by email. If you ever
  add multi-user support, switch that rule to per-document ownership instead.
- One Firestore collection: `applications`. Fields: `company`, `role`, `jobLink`, `status`
  (one of `UI/Models/JobApplication.cs`'s `ApplicationStatus.All`), `dateApplied`,
  `lastUpdate`, `notes` — all strings, dates as `yyyy-MM-dd`.
- **Base path:** served under `/job-tracker/` on GitHub Pages. Source keeps `<base href="/">`
  for local `dotnet run`; the deploy workflow `sed`s it to `/job-tracker/` in the published
  `index.html`/`404.html`. `404.html` mirrors `index.html` so deep links survive a GH Pages
  reload the same way as `aboutme`.

## Resume tailoring (cross-repo)

`resume/tailored/<company>-<role-slug>/` holds per-application resumes, meant to sit next to
the tracker entry they belong to. **The content source of truth stays in the sibling `aboutme`
repo**, at `aboutme/resume/CONTEXT.md` (every role/project/metric) and `aboutme/resume/base/`
(the general-purpose template to copy from). Nothing here duplicates that — see
`resume/README.md` for the exact workflow. This only works assuming both repos are checked out
as sibling directories on the same machine; if that ever changes, revisit how the two repos
share context (a git submodule would be the next step, not needed today).

`.github/workflows/tailor-resume.yml` compiles a given `resume/tailored/<folder>/` with
XeLaTeX and uploads the PDF as a build artifact only — never published anywhere public.
