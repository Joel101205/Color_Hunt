const { onCall, HttpsError } = require("firebase-functions/v2/https");
const { firestoreDatabase } = require("../firebaseComponents");
const { FieldValidationService } = require("./services");

// returns a map of the pixels a player can use
const getPlayerPixelMap = onCall(async (request) => {
    FieldValidationService(request, ["playerId"]);

    const playerId = request.data.playerId;

    // get playerDoc for playerId
    const playerDoc = firestoreDatabase.collection("players").doc(playerId);

    // transaction to avoid race conditions
    const pixels = await firestoreDatabase.runTransaction(async (ta) => {
        const playerSnap = await ta.get(playerDoc);

        // player id doc doesnt exist
        if (!playerSnap.exists) {
            throw new HttpsError("not-found", "Player not found");
        }

        // create default empty pixel map
        const emptyPixelMap = {
            "#000000": 0,   // black
            "#FFFFFF": 0,   // white
            "#0000FF": 0,   // blue
            "#00FFFF": 0,   // cyan
            "#00FF00": 0,   // green
            "#FFFF00": 0,   // yellow
            "#FFA500": 0,   // orange
            "#FF0000": 0,   // red
            "#FF00FF": 0,   // magenta
            "#8000FF": 0    // purple
        };

        // return the firestore pixel map
        return { ...emptyPixelMap, ...playerSnap.data()?.pixels };
    });

    return { pixels };
});

// export public functions
module.exports = { getPlayerPixelMap };
