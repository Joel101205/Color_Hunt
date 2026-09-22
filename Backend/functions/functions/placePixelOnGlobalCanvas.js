const { onCall, HttpsError } = require("firebase-functions/v2/https");
// const bmp = require("bmp-js");
const { PNG } = require("pngjs");
const {
  firestoreDatabase,
  firebaseStorage,
  FieldValue,
} = require("../firebaseComponents");
const { FieldValidationService } = require("./services");

// returns xx
// - xx:
const placePixelOnGlobalCanvas = onCall(async (request) => {
  FieldValidationService(request, ["playerId", "xCoord", "yCoord", "hexCode"]);
  const playerId = request.data.playerId;
  const xCoord = Number(request.data.xCoord);
  const yCoord = Number(request.data.yCoord);
  const hexCode = request.data.hexCode;

  // check if player has pixel with color
  const playerDoc = firestoreDatabase.collection("players").doc(playerId);
  await firestoreDatabase.runTransaction(async (ta) => {
    const playerSnap = await ta.get(playerDoc);
    const pixels = playerSnap.data()?.pixels;

    if (pixels == null || pixels[hexCode] == null || pixels[hexCode] <= 0) {
      throw new HttpsError(
        "permission-denied",
        `Player does not have a pixel with color ${hexCode}!`,
      );
    }
  });

  // lock globalCanvas.bmp mutex
  const globalCanvasMutexDoc = firestoreDatabase
    .collection("locks")
    .doc("global-canvas");
  await firestoreDatabase.runTransaction(async (ta) => {
    const mutexSnap = await ta.get(globalCanvasMutexDoc);
    const now = Date.now();
    if (mutexSnap.exists) {
      const data = mutexSnap.data();
      const stillValid = data.expiresAt && data.expiresAt.toMillis() > now;
      const ownedByMe = data.lockedBy === playerId;

      if (stillValid && !ownedByMe) {
        throw new HttpsError(
          "failed-precondition",
          "globalCanvas.bmp is mutex locked, try again later!",
        );
      }
    }

    ta.set(globalCanvasMutexDoc, {
      lockedBy: playerId,
      lockedAt: new Date(now),
      expiresAt: new Date(now + 5000),
    });
  });

  // get global canvas
  let image;
  let file;
  try {
    const bucket = firebaseStorage.bucket();
    file = bucket.file("canvas/globalCanvas.png");
    const [buffer] = await file.download();
    image = PNG.sync.read(buffer);
  } catch (err) {
    throw new HttpsError(
      "internal",
      "Something went wrong while loading the glovalCanvas file!",
    );
  }

  // png size and coordinate guard
  if (
    xCoord < 0 ||
    xCoord >= image.width ||
    yCoord < 0 ||
    yCoord >= image.height
  ) {
    throw new HttpsError(
      "out-of-range",
      `Specified Coordinates out of globalCanvas bounds (xCoord: ${xCoord} [0..${image.width}], yCoord: ${yCoord} [0..${image.height}])!`,
    );
  }

  // set pixel
  const flippedY = image.height - 1 - yCoord;
  const index = (flippedY * image.width + xCoord) * 4;
  const { r, g, b } = hexStringToRgb(hexCode);
  image.data[index] = r;
  image.data[index + 1] = g;
  image.data[index + 2] = b;
  image.data[index + 3] = 255;

  // upload modified image
  const encoded = PNG.sync.write(image);
  await file.save(encoded, {
    contentType: "image/png",
    metadata: {
      cacheControl: "no-cache, max-age=0",
    },
  });

  // update firestore changed document
  const globalCanvasChangedDoc = firestoreDatabase
    .collection("canvas")
    .doc("global-canvas");
  await firestoreDatabase.runTransaction(async (ta) => {
    const canvasChangedSnap = await ta.get(globalCanvasChangedDoc);
    ta.set(globalCanvasChangedDoc, {
      lastChanged: FieldValue.serverTimestamp(),
    });
  });

  // release globalCanvas.bmp mutex
  await firestoreDatabase.runTransaction(async (ta) => {
    const mutexSnap = await ta.get(globalCanvasMutexDoc);
    if (!mutexSnap.exists) {
      return;
    }
    const data = mutexSnap.data();

    if (data.lockedBy === playerId && data.expiresAt.toMillis() > Date.now()) {
      ta.delete(globalCanvasMutexDoc);
    }
  });

  // remove pixel from players pixels
  await firestoreDatabase.runTransaction(async (ta) => {
    await ta.update(playerDoc, {
      [`pixels.${hexCode}`]: FieldValue.increment(-1),
    });
  });
});

// minimal bmp file validity guard
function isBufferValidBmp(buffer) {
  const isBuffer = Buffer.isBuffer(buffer);
  const minBmpHeaderSize = buffer.length >= 54;
  const correctBmpHeaderBytes = buffer[0] === 0x42 && buffer[1] === 0x4d;
  if (!(isBuffer && minBmpHeaderSize && correctBmpHeaderBytes)) {
    throw new HttpsError(
      "failed-precondition",
      "Firebase Storage globalCanvas.bmp is not a valid Bitmap file!",
    );
  }
}

// converts a hex string to an rgb object
function hexStringToRgb(hex) {
  const h = hex.replace("#", "");
  return {
    r: parseInt(h.substring(0, 2), 16),
    g: parseInt(h.substring(2, 4), 16),
    b: parseInt(h.substring(4, 6), 16),
  };
}

// export public functions
module.exports = { placePixelOnGlobalCanvas };
