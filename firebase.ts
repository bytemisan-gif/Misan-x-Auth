
import { initializeApp } from 'firebase/app';
import { getAuth, setPersistence, browserSessionPersistence } from 'firebase/auth';
import { getDatabase } from 'firebase/database';
import { FIREBASE_CONFIG } from '../constants';

const app = initializeApp(FIREBASE_CONFIG);
export const auth = getAuth(app);
setPersistence(auth, browserSessionPersistence).catch((err) => {
    console.error("Failed to set main auth persistence:", err);
});
export const db = getDatabase(app);

const resellerApp = initializeApp(FIREBASE_CONFIG, 'reseller');
export const resellerAuth = getAuth(resellerApp);
setPersistence(resellerAuth, browserSessionPersistence).catch((err) => {
    console.error("Failed to set reseller auth persistence:", err);
});
export const resellerDb = getDatabase(resellerApp);
