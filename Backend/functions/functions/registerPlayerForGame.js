const { onCall, HttpsError } = require("firebase-functions/v2/https");
const { firestoreDatabase, firebaseStorage, firebaseBackendSettings } = require("../firebaseComponents");
const { FieldValidationService, HexCodeGeneratorService } = require("./services");


// returns gameDoc
// - gameDoc: string, name of the document in firestore db [active-games] to listen to for hexcode and game result
const registerPlayerForGame = onCall(async (request) => {
    FieldValidationService(request, ["playerId", "gameId"]);
    const playerId = request.data.playerId;
    const gameId = request.data.gameId;

    // get gameDoc for gameId
    const gameDoc = firestoreDatabase.collection("active-games").doc(gameId);

    // transaction to avoid race conditions
    await firestoreDatabase.runTransaction(async (ta) => {
        const gameSnap = await ta.get(gameDoc);
        if (!gameSnap.exists) {
            // register new gameDoc with status waiting for gameId
            ta.set(gameDoc, {
                player1: playerId,
                player1_colorAccuracy: 0.0,
                player1_distance: 0.0,
                player1_time: 0,
                player1_image: "",
                player1_score: 0,
                player2: "",
                player2_colorAccuracy: 0.0,
                player2_distance: 0.0,
                player2_time: 0,
                player2_image: "",
                player2_score: 0,
                hexCode: "",
                created: new Date().toISOString(),
                status: "waiting"
            });
        } else {
            // first player already registered, create hexcode and set status running for gameId
            const hexCode = await HexCodeGeneratorService();
            ta.update(gameDoc, {
                player2: playerId,
                hexCode: hexCode,
                status: "running"
            });
        }
    });

    // return gameId as gameDoc
    return {
        gameDoc: gameId
    };
});

// export public functions
module.exports = { registerPlayerForGame };