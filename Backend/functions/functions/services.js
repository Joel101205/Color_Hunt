const { onCall, HttpsError } = require("firebase-functions/v2/https");
const { firebaseBackendSettings, firestoreDatabase, FieldValue } = require("../firebaseComponents");

// validates request data against requiredFields (existing, not null and not empty)
function FieldValidationService(request, requiredFields) {
    const data = request.data || {};
    for (const field of requiredFields) {
        if (data[field] === undefined ||
            data[field] === null ||
            (typeof data[field] === "string" && data[field].trim() === "")) {
            throw new HttpsError("invalid-argument", `missing or empty data field: ${field}`);
        }
    }
}

// generates a hex code with the algorithm specified in: firestore -> game-settings -> backend-settings -> hexCodeGenVersion
async function HexCodeGeneratorService() {
    const backendSettingsSnap = await firebaseBackendSettings.get();
    const hexCodeGenVersion = backendSettingsSnap.data()?.hexCodeGenVersion;
    switch (hexCodeGenVersion) {
        case 1:
            // random hex code generation
            return "#" + Math.floor(Math.random() * 0xffffff).toString(16).padStart(6, "0");
        case 2:
            // this generates two hex codes for the presentation in two consecutive games
            const presentationGameNumber = backendSettingsSnap.data()?.presentationGameNumber;

            if (presentationGameNumber == 1) {
                // increment presentationGameNumber
                await firestoreDatabase.runTransaction(async (ta) => {
                    ta.update(firebaseBackendSettings, {
                        presentationGameNumber: FieldValue.increment(1),
                    });
                });
                // first hexCode color: 0xFD2626 (almost red) -> wants TUM logo to look like the mario stones, replace blue pixel with red
                return "#FD2626";
            }
            if (presentationGameNumber == 2) {
                // reset presentationGameNumber and hexCodeGenVersion
                await firestoreDatabase.runTransaction(async (ta) => {
                    ta.update(firebaseBackendSettings, {
                        presentationGameNumber: 1,
                        hexCodeGenVersion: 1,
                    });
                });

                // second hexCode color: 0x3C3CFA (almost blue) -> needs good grade so wants to keep TUM logo blue, replace red pixel back to blue
                return "#3C3CFA";
            }
            break;
        default:
            throw new HttpsError("unimplemented", `backend-settings: hexCodeGenVersion ${hexCodeGenVersion} is not implemented!`);
    }
}

// clamps the hexValue to the closest color from a hexList
function HexColorClampService(hexValue, hexList) {
    return hexList.reduce((closest, color) =>
        colorDistance(hexValue, color) < colorDistance(hexValue, closest) ? color : closest
    );
}

function colorDistance(hex1, hex2) {
    const [r1, g1, b1] = hexToRgb(hex1);
    const [r2, g2, b2] = hexToRgb(hex2);
    return Math.sqrt((r1 - r2) ** 2 + (g1 - g2) ** 2 + (b1 - b2) ** 2);
}

function hexToRgb(hex) {
    const n = parseInt(hex.replace('#', ''), 16);
    return [(n >> 16) & 255, (n >> 8) & 255, n & 255];
}

// export services
module.exports = { FieldValidationService, HexColorClampService, HexCodeGeneratorService }