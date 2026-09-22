// this index.js file exports the public accessible firebase functions

const { checkBackendConnection } = require("./functions/checkBackendConnection");
const { registerPlayerForGame } = require("./functions/registerPlayerForGame");
const { commitGameResult } = require("./functions/commitGameResult");
const { cancelWaitingGame } = require("./functions/cancelWaitingGame");
const { getPlayerPixelMap } = require("./functions/getPlayerPixelMap");
const { placePixelOnGlobalCanvas } = require("./functions/placePixelOnGlobalCanvas");
const { clearGlobalCanvas } = require("./functions/clearGlobalCanvas");
const { registerFriendGame } = require("./functions/registerFriendGame");
const { joinFriendGame } = require("./functions/joinFriendGame");

exports.checkBackendConnection = checkBackendConnection;
exports.registerPlayerForGame = registerPlayerForGame;
exports.commitGameResult = commitGameResult;
exports.cancelWaitingGame = cancelWaitingGame;
exports.getPlayerPixelMap = getPlayerPixelMap;
exports.placePixelOnGlobalCanvas = placePixelOnGlobalCanvas;
exports.clearGlobalCanvas = clearGlobalCanvas;
exports.registerFriendGame = registerFriendGame;
exports.joinFriendGame = joinFriendGame;