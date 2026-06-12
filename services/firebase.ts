
import { initializeApp } from 'firebase/app';
import { getAuth, setPersistence, browserSessionPersistence } from 'firebase/auth';
import { getDatabase } from 'firebase/database';
import { FIREBASE_CONFIG } from '../constants';

const app = initializeApp(FIREBASE_CONFIG);
export const auth = getAuth(app);
setPersistence(auth, browserSessionPersistence).catch((err) => {
    console.error("Failed to set auth persistence:", err);
});
export const db = getDatabase(app);
