// Get these values from Firebase Console -> Project settings -> Your apps -> Web app.
// Safe to commit: this is a public client identifier, not a secret. Actual access
// control lives in firestore.rules (only your Google account can read/write).
// See SETUP.md at the repo root for how to get these.
export const firebaseConfig = {
  apiKey: "REPLACE_ME",
  authDomain: "REPLACE_ME.firebaseapp.com",
  projectId: "REPLACE_ME",
  storageBucket: "REPLACE_ME.appspot.com",
  messagingSenderId: "REPLACE_ME",
  appId: "REPLACE_ME",
};
