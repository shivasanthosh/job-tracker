# Tailored resumes

Per-application resumes live here, next to the application they belong to in the tracker.

**The source of truth for content is [`aboutme/resume/CONTEXT.md`](../../aboutme/resume/CONTEXT.md)**
(a sibling repo) — every role, project, and metric, written in full. The general-purpose
resume also lives there, at `aboutme/resume/base/`. This repo never duplicates either; it
only holds the per-company tailored copies.

## Making a tailored resume

1. Copy the template from the sibling repo: `cp -r ../aboutme/resume/base resume/tailored/<company>-<role-slug>`
2. Re-read `aboutme/resume/CONTEXT.md` against the job description; in the copied `.tex`,
   reorder/trim skills and swap which experience bullets lead — pull only from what's already
   in `CONTEXT.md`, never fabricate.
3. Commit, push, then compile:
   ```sh
   gh workflow run tailor-resume.yml -f folder=<company>-<role-slug>
   gh run watch
   gh run download --name resume-<company>-<role-slug>
   ```
   It uploads the compiled PDF as a build artifact — not published anywhere public, since a
   resume tailored for one company shouldn't sit at a public URL next to one tailored for
   another.

No local LaTeX install is needed or expected — CI compiles with XeLaTeX (this template
requires it).
