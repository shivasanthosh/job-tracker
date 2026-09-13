# One-time setup

Everything below is a manual, one-time step in the Firebase Console because it needs your
Google login — I can't do it for you. Takes about 5 minutes, entirely on the free Spark plan
(no credit card).

## 1. Create the Firebase project

1. Go to https://console.firebase.google.com/ → **Add project**.
2. Name it anything (e.g. `job-tracker`). Google Analytics is not needed — skip it.
3. Stay on the **Spark (free) plan** — it's the default; nothing here needs Blaze.

## 2. Enable Google sign-in

1. In the project, go to **Build → Authentication → Get started**.
2. Under **Sign-in method**, enable **Google**.
3. Set a project support email (your own).

## 3. Create Firestore

1. Go to **Build → Firestore Database → Create database**.
2. Any region is fine (pick one close to you).
3. Start in **production mode** (the security rules in this repo lock it down anyway).

## 4. Register the web app and get your config

1. In **Project settings** (gear icon) → **Your apps** → **</> (Web)**.
2. Give it any nickname, no need to set up Firebase Hosting (we use GitHub Pages).
3. Copy the `firebaseConfig` object it shows you.
4. Paste those values into `UI/wwwroot/js/firebase-config.js` in this repo, replacing the
   `REPLACE_ME` placeholders. This file is safe to commit — it's a public client identifier,
   not a secret.
5. Also put the project ID into `.firebaserc` (replace `REPLACE_WITH_YOUR_FIREBASE_PROJECT_ID`).

## 5. Deploy the security rules

`firestore.rules` in this repo already restricts all reads/writes to your Google account
(`shivasanthosh531@gmail.com` — edit that file first if you want to use a different one).
Deploy it with the Firebase CLI (no install needed, `npx` runs it once):

```sh
npx firebase-tools login
npx firebase-tools deploy --only firestore:rules
```

## 6. Enable GitHub Pages for this repo

Repo → **Settings → Pages → Source**: set to the `gh-pages` branch (created automatically the
first time `deploy.yml` runs on a push to `main`). The site will then be live at
`https://shivasanthosh.github.io/job-tracker/`.

That's it — after this, sign-in and the applications list should just work.
