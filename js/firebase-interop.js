// Thin wrapper around the Firebase JS SDK, exposed on window.jobTracker so Blazor's
// JSInterop can call plain function names. All Firestore access control happens in
// firestore.rules (single account, gated by email) -- this file has no secrets in it.
import { initializeApp } from "https://www.gstatic.com/firebasejs/10.13.2/firebase-app.js";
import {
  getAuth,
  GoogleAuthProvider,
  signInWithPopup,
  signOut,
  onAuthStateChanged,
} from "https://www.gstatic.com/firebasejs/10.13.2/firebase-auth.js";
import {
  getFirestore,
  collection,
  addDoc,
  updateDoc,
  deleteDoc,
  doc,
  onSnapshot,
  query,
  orderBy,
} from "https://www.gstatic.com/firebasejs/10.13.2/firebase-firestore.js";
import { firebaseConfig } from "./firebase-config.js";

const app = initializeApp(firebaseConfig);
const auth = getAuth(app);
const db = getFirestore(app);
const provider = new GoogleAuthProvider();

let applicationsUnsubscribe = null;

function toPlainUser(user) {
  if (!user) return null;
  return {
    uid: user.uid,
    email: user.email,
    displayName: user.displayName,
    photoUrl: user.photoURL,
  };
}

// Every Firestore/auth call below returns a plain { ok, code, message } result instead of
// letting rejections escape -- an uncaught JS promise rejection surfaces to Blazor as an
// unhandled JSException and trips the generic #blazor-error-ui banner, which is not a
// helpful way to tell a signed-in-but-unauthorized user (or a flaky network) what happened.
function errorResult(error) {
  console.error(error);
  return { ok: false, code: error?.code ?? "unknown", message: error?.message ?? String(error) };
}

window.jobTracker = {
  registerAuthCallback(dotNetRef) {
    onAuthStateChanged(auth, (user) => {
      dotNetRef.invokeMethodAsync("OnAuthStateChanged", toPlainUser(user));
    });
  },

  async signIn() {
    try {
      await signInWithPopup(auth, provider);
      return { ok: true };
    } catch (error) {
      return errorResult(error);
    }
  },

  async signOutUser() {
    if (applicationsUnsubscribe) {
      applicationsUnsubscribe();
      applicationsUnsubscribe = null;
    }
    await signOut(auth);
  },

  registerApplicationsListener(dotNetRef) {
    if (applicationsUnsubscribe) applicationsUnsubscribe();
    const q = query(collection(db, "applications"), orderBy("dateApplied", "desc"));
    applicationsUnsubscribe = onSnapshot(
      q,
      (snapshot) => {
        const apps = snapshot.docs.map((d) => ({ id: d.id, ...d.data() }));
        dotNetRef.invokeMethodAsync("OnApplicationsChanged", JSON.stringify(apps));
      },
      (error) => dotNetRef.invokeMethodAsync("OnApplicationsError", errorResult(error))
    );
  },

  async addApplication(data) {
    try {
      await addDoc(collection(db, "applications"), data);
      return { ok: true };
    } catch (error) {
      return errorResult(error);
    }
  },

  async updateApplication(id, data) {
    try {
      await updateDoc(doc(db, "applications", id), data);
      return { ok: true };
    } catch (error) {
      return errorResult(error);
    }
  },

  async deleteApplication(id) {
    try {
      await deleteDoc(doc(db, "applications", id));
      return { ok: true };
    } catch (error) {
      return errorResult(error);
    }
  },
};
