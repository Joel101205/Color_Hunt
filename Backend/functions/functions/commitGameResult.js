const { onCall, HttpsError } = require("firebase-functions/v2/https");
const {
  firestoreDatabase,
  firebaseStorage,
  firebaseBackendSettings,
  FieldValue,
} = require("../firebaseComponents");
const { FieldValidationService, HexColorClampService } = require("./services");

// returns score
// - score: float, calculated score from the transmitted data based on the specified computeGameScoreVersion
const commitGameResult = onCall(async (request) => {
  FieldValidationService(request, [
    "playerId",
    "gameId",
    "accuracy",
    "distance",
    "time",
    "image",
  ]);
  const playerId = request.data.playerId;
  const gameId = request.data.gameId;
  const accuracy = request.data.accuracy; // 0..100 (float)
  const distance = request.data.distance; // in meters
  const time = request.data.time; // in ms
  const image = request.data.image; // path to image in storage

  // get backend-settings from database
  const backendSettingsSnap = await firebaseBackendSettings.get();
  const computeGameScoreVersion =
    backendSettingsSnap.data()?.computeGameScoreVersion;
  const computeCanvasPixelVersion =
    backendSettingsSnap.data()?.computeCanvasPixelVersion;

  // get frontend-settings from db
  const frontendSettingsSnap = await firestoreDatabase
    .collection("game-settings")
    .doc("frontend-settings")
    .get();

  const maxPlayTimeSeconds =
    frontendSettingsSnap.data()?.playTimeSeconds || 180;

  // compute score
  const score = computeGameScore(
    computeGameScoreVersion,
    accuracy,
    time,
    distance,
    maxPlayTimeSeconds,
  );

  // get gameDoc for gameId
  const gameDoc = firestoreDatabase.collection("active-games").doc(gameId);

  // transaction to avoid race conditions
  await firestoreDatabase.runTransaction(async (ta) => {
    const gameSnap = await ta.get(gameDoc);
    if (!gameSnap.exists) {
      // gameDoc doesn't exist, gameId missmatch
      throw new HttpsError(
        "not-found",
        `no active game with id ${gameId} found!`,
      );
    } else {
      // check playerId to match gameDoc player indexing
      const isPlayer1 = gameSnap.data()?.player1 === playerId;
      const isPlayer2 = gameSnap.data()?.player2 === playerId;

      // check if gameDoc contains a player with transmitted id
      if (!isPlayer1 && !isPlayer2) {
        throw new HttpsError(
          "permission-denied",
          `player ${playerId} is not part of game ${gameId}!`,
        );
      }

      // set game results based on transmitted playerId
      if (isPlayer1) {
        ta.update(gameDoc, {
          player1_colorAccuracy: accuracy,
          player1_distance: distance,
          player1_time: time,
          player1_image: image,
          player1_score: score,
          status: "committed",
        });
      } else {
        ta.update(gameDoc, {
          player2_colorAccuracy: accuracy,
          player2_distance: distance,
          player2_time: time,
          player2_image: image,
          player2_score: score,
          status: "committed",
        });
      }
    }
  });

  // game finished routine
  const updatedData = (await gameDoc.get()).data();
  if (
    updatedData.status === "committed" &&
    updatedData.player1_image != "" &&
    updatedData.player2_image != ""
  ) {
    // move gameDoc to finished-games
    const finishedGameDoc = firestoreDatabase
      .collection("finished-games")
      .doc(gameId);
    await finishedGameDoc.set(updatedData);
    await gameDoc.delete();

    // award pixel to winning player
    const finishedGameData = (await finishedGameDoc.get()).data();
    const player1_score = finishedGameData?.player1_score;
    const player2_score = finishedGameData?.player2_score;
    const hexCode = finishedGameData?.hexCode;
    const key = computeCanvasPixel(computeCanvasPixelVersion, hexCode);

    let player1_reward = 0;
    let player2_reward = 0;

    if (player1_score === player2_score) {
      // draw -> both players get one pixel
      player1_reward = 1;
      player2_reward = 1;
    } else if (player1_score > player2_score) {
      // player 1 wins, winner gets 3 pixels
      player1_reward = 3;
      player2_reward = 1;
    } else {
      // Player 2 wins
      player1_reward = 1;
      player2_reward = 3;
    }

    const playerDoc1 = firestoreDatabase
      .collection("players")
      .doc(finishedGameData?.player1);
    const playerDoc2 = firestoreDatabase
      .collection("players")
      .doc(finishedGameData?.player2);

    await Promise.all([
      playerDoc1.update({
        [`pixels.${key}`]: FieldValue.increment(player1_reward),
        "statistics.gamesPlayed": FieldValue.increment(1),
        "statistics.gamesWon": FieldValue.increment(player1_reward > player2_reward ? 1 : 0),
      }),
      playerDoc2.update({
        [`pixels.${key}`]: FieldValue.increment(player2_reward),
        "statistics.gamesPlayed": FieldValue.increment(1),
        "statistics.gamesWon": FieldValue.increment(player2_reward > player1_reward ? 1 : 0),
      }),
    ]);
  }

  // return score
  return {
    score: score,
  };
});

// compute the score for a game with the algorithm specified in: firestore -> game-settings -> backend-settings -> computeGameScoreVersion
function computeGameScore(
  computeGameScoreVersion,
  accuracy,
  time,
  distance,
  maxPlayTimeSeconds,
) {
  switch (computeGameScoreVersion) {
    case 1:
      // accuracy - (1/2 * seconds) + distance
      let score = accuracy - 0.5 * (time / 1000) + distance;
      score = Math.round(score * 100) / 100;
      return Math.max(0, score);
    case 2:
      // bucket (accuracy max 1000, time and location max 500 points respectively) = max 2000 points
      const timeSeconds = time / 1000;
      const accuracyScore = accuracy * 10;

      const timeScore = Math.max(
        0,
        500 * (1 - timeSeconds / maxPlayTimeSeconds),
      );
      const targetDistance = 500;
      const distanceScore = Math.min(500, 500 * (distance / targetDistance));
      let totalScore = (accuracyScore + timeScore + distanceScore) / 2.0;
      return Math.round(totalScore);
    default:
      throw new HttpsError(
        "unimplemented",
        `backend-settings: computeGameScoreVersion ${computeGameScoreVersion} is not implemented!`,
      );
  }
}

// compute the awarded pixel with the algorithm specified in: firestore -> game-settings -> backend-settings -> computeCanvasPixelVersion
function computeCanvasPixel(computeCanvasPixelVersion, searchHex) {
  switch (computeCanvasPixelVersion) {
    case 1:
      // map searchHex to default 10 color wheel
      const palette = [
        "#000000",
        "#FFFFFF",
        "#0000FF",
        "#00FFFF",
        "#00FF00",
        "#FFFF00",
        "#FFA500",
        "#FF0000",
        "#FF00FF",
        "#8000FF",
      ];
      return HexColorClampService(searchHex, palette);
    default:
      throw new HttpsError(
        "unimplemented",
        `backend-settings: computeCanvasPixelVersion ${computeCanvasPixelVersion} is not implemented!`,
      );
  }
}

// export public functions
module.exports = { commitGameResult };
