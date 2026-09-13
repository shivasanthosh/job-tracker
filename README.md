# Job Tracker

A tiny, zero-cost job application tracker: Blazor WebAssembly (.NET 9), Google sign-in, and
Firebase Firestore as the database, deployed as a static site to GitHub Pages at
`https://shivasanthosh.github.io/job-tracker/`.

It also holds per-application **tailored resumes** (`resume/tailored/`), compiled on demand —
see `resume/README.md`. The resume content source of truth stays in the sibling `aboutme`
repo (`aboutme/resume/CONTEXT.md`).

## First-time setup

See [`SETUP.md`](SETUP.md) — a few manual steps in the Firebase Console (needs your Google
login), all on the free Spark plan.

## Commands

```sh
dotnet run --project UI/UI.csproj              # dev server at http://localhost:5217
dotnet publish UI/UI.csproj -c Release -o release   # what CI runs
```

Deployment is automatic: `.github/workflows/deploy.yml` publishes on every push to `main` and
pushes `release/wwwroot` to the `gh-pages` branch.

## How auth + data work

- `UI/wwwroot/js/firebase-interop.js` wraps the Firebase JS SDK (loaded straight from Google's
  CDN, no npm/bundler) and exposes plain functions on `window.jobTracker` that Blazor calls via
  `IJSRuntime`. Firestore's realtime listener calls back into the `Home` component via
  `DotNetObjectReference` + `[JSInvokable]`.
- `UI/wwwroot/js/firebase-config.js` holds the Firebase web config. It's not a secret (Firebase
  is designed for this to be public) — actual access control is `firestore.rules`, which
  restricts every read/write to one Google account by email.
- There's exactly one Firestore collection, `applications`, no ownership field needed since
  it's gated at the account level.
