const { onCall, HttpsError } = require("firebase-functions/v2/https");
const { firestoreDatabase } = require("../firebaseComponents");
const { FieldValidationService } = require("./services");
const { randomUUID } = require("crypto");


// returns gameDoc
// - gameDoc: string, name of the document in firestore db [active-games] to listen to for hexcode and game result
const registerFriendGame = onCall(async (request) => {
    FieldValidationService(request, ["playerId", "friendId"]);
    const playerId = request.data.playerId;
    const friendId = request.data.friendId;

    // check if player with friendId exists
    const friendDoc = firestoreDatabase.collection("players").doc(friendId);
    const friendSnap = await friendDoc.get();
    if (!friendSnap.exists) {
        throw new HttpsError("failed-precondition", `Friend with playerId ${friendId} doesn't exist!`);
    }

    // check if player with friendId is in queue or active game
    const player1FriendSnap = await firestoreDatabase
        .collection("active-games")
        .where("player1", "==", friendId)
        .get();
    if (!player1FriendSnap.empty) {
        throw new HttpsError("aborted", `Friend with playerId ${friendId} already in game`);
    }
    const player2FriendSnap = await firestoreDatabase
        .collection("active-games")
        .where("player2", "==", friendId)
        .get();
    if (!player2FriendSnap.empty) {
        throw new HttpsError("aborted", `Friend with playerId ${friendId} already in game`);
    }


    // generate gameId and gameDoc reference
    const gameId = randomUUID();
    const gameDoc = firestoreDatabase.collection("active-games").doc(gameId);

    await firestoreDatabase.runTransaction(async (ta) => {

        // get docs and snaps
        const gameDoc = firestoreDatabase.collection("active-games").doc(gameId);
        const friendDoc = firestoreDatabase.collection("players").doc(friendId);
        const playerDoc = firestoreDatabase.collection("players").doc(playerId);
        const gameSnap = await ta.get(gameDoc);
        const friendSnap = await ta.get(friendDoc);
        const playerSnap = await ta.get(playerDoc);

        // update friend document
        const friendData = friendSnap.data();
        if (friendData.waitForFriendGame != undefined && friendData.waitForFriendGame != "") {
            throw new HttpsError("failed-precondition", `Friend with playerId ${friendId} already waits for a friend game with another player!`);
        }
        ta.set(friendDoc, {
            waitForFriendGame: gameId
        }, { merge: true });

        // update player document
        const playerData = playerSnap.data();
        if (playerData.waitForFriendGame != undefined && playerData.waitForFriendGame != "") {
            throw new HttpsError("failed-precondition", `Player with id ${playerId} already waits for a friend game with another player!`);
        }
        ta.set(playerDoc, {
            waitForFriendGame: gameId
        }, { merge: true });

        // register game
        if (!gameSnap.exists) {
            // register new gameDoc with status waiting_for_friend
            ta.set(gameDoc, {
                player1: playerId,
                player1_colorAccuracy: 0.0,
                player1_distance: 0.0,
                player1_time: 0,
                player1_image: "",
                player1_score: 0,
                player2: friendId,
                player2_colorAccuracy: 0.0,
                player2_distance: 0.0,
                player2_time: 0,
                player2_image: "",
                player2_score: 0,
                hexCode: "",
                created: new Date().toISOString(),
                status: "waiting_for_friend"
            });
        } else {
            throw new HttpsError("aborted", `gameDoc with id ${gameId} already exists!`);
        }
    });

    // return gameId as gameDoc
    return {
        gameId: gameId
    };
});

// export public functions
module.exports = { registerFriendGame };