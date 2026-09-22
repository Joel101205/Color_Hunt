const { onCall, HttpsError } = require("firebase-functions/v2/https");
const { firestoreDatabase } = require("../firebaseComponents");
const { FieldValidationService } = require("./services");


const cancelWaitingGame = onCall(async (request) => {
    FieldValidationService(request, ["gameId"]);

    const gameId = request.data.gameId;

    // sleep for 2 seconds to ensure a gameDoc exists
    // (If cancelWaitingGame is called right after registerPlayerForGame, the doc may not exist yet)
    await new Promise(resolve => setTimeout(resolve, 2000));

    // get gameDoc for gameId
    const gameDoc = firestoreDatabase.collection("active-games").doc(gameId);

    // transaction to avoid race conditions
    await firestoreDatabase.runTransaction(async (ta) => {
        const gameSnap = await ta.get(gameDoc);
        if (gameSnap.exists) {
            // delete game
            ta.delete(gameDoc);
        }
    });
});

// export public functions
module.exports = { cancelWaitingGame };
