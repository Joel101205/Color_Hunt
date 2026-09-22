const { initializeApp } = require("firebase-admin/app");
const { getFirestore, FieldValue } = require("firebase-admin/firestore");
const { getStorage } = require("firebase-admin/storage");

initializeApp();

const firestoreDatabase = getFirestore();
const firebaseStorage = getStorage();

const firebaseBackendSettings =
    firestoreDatabase.collection("game-settings").doc("backend-settings");

module.exports = {
    firestoreDatabase,
    firebaseStorage,
    firebaseBackendSettings,
    FieldValue
};