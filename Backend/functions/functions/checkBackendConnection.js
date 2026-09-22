const { onCall } = require("firebase-functions/v2/https");

// returns backendName, backendVersion, timestamp, connected
// this function only exists to check if firebase functions still works for our project and can be called via shell script
const checkBackendConnection = onCall(async (request) => {
    const currentTimestamp = new Date().toISOString();
    return {
        backendName: "ColorHunt",
        backendVersion: 1.0,
        timestamp: currentTimestamp,
        connected: true
    };
});

module.exports = { checkBackendConnection };