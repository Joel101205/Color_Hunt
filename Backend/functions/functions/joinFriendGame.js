const { onCall, HttpsError } = require("firebase-functions/v2/https");
const { firestoreDatabase } = require("../firebaseComponents");
const { FieldValidationService, HexCodeGeneratorService } = require("./services");



const joinFriendGame = onCall(async (request) => {
    FieldValidationService(request, ["gameId", "join"]);
    const gameId = request.data.gameId;
    const join = request.data.join;

    await firestoreDatabase.runTransaction(async (ta) => {
        // get docs and snaps
        const gameDoc = firestoreDatabase.collection("active-games").doc(gameId);
        const gameSnap = await ta.get(gameDoc);
        if (!gameSnap.exists) {
            throw new HttpsError("not-found", `gameDoc with id ${gameId} does not exist!`);
        }

        const playerId = gameSnap.data().player1;
        const friendId = gameSnap.data().player2;

        const playerDoc = firestoreDatabase.collection("players").doc(playerId);
        const friendDoc = firestoreDatabase.collection("players").doc(friendId);

        ta.set(playerDoc, {
            waitForFriendGame: ""
        }, { merge: true });
        ta.set(friendDoc, {
            waitForFriendGame: ""
        }, { merge: true });

        if (join) {
            const hex = await HexCodeGeneratorService();
            ta.set(gameDoc, {
                hexCode: hex,
                status: "running"
            }, { merge: true });
        } else {
            ta.delete(gameDoc);
        }
    });
});

// export public functions
module.exports = { joinFriendGame };