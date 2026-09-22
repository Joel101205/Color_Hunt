const { onCall, HttpsError } = require("firebase-functions/v2/https");
const bmp = require("bmp-js");
const { firebaseStorage } = require("../firebaseComponents");


const clearGlobalCanvas = onCall(async (request) => {

    // get global canvas
    const bucket = firebaseStorage.bucket();
    const file = bucket.file("canvas/globalCanvas.bmp");
    const [buffer] = await file.download();
    isBufferValidBmp(buffer);
    const image = bmp.decode(buffer);

    // set all canvas pixels to white
    for (let i = 0; i < image.data.length; i += 4) {
        image.data[i] = 255;
        image.data[i + 1] = 255;
        image.data[i + 2] = 255;
        image.data[i + 3] = 255;
    }

    // upload modified image
    const encoded = bmp.encode({
        data: image.data,
        width: image.width,
        height: image.height
    });

    await file.save(encoded.data, { contentType: "image/bmp" });

    return { success: true };
});

// minimal bmp file validity guard
function isBufferValidBmp(buffer) {
    const isBuffer = Buffer.isBuffer(buffer);
    const minBmpHeaderSize = buffer.length >= 54;
    const correctBmpHeaderBytes = buffer[0] === 0x42 && buffer[1] === 0x4D;
    if (!(isBuffer && minBmpHeaderSize && correctBmpHeaderBytes)) {
        throw new HttpsError("failed-precondition", "Firebase Storage globalCanvas.bmp is not a valid Bitmap file!");
    }
}

// export public functions
module.exports = { clearGlobalCanvas };
