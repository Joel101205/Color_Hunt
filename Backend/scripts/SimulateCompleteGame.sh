# register normal game with two players
curl -X POST -H "Content-Type: application/json" -d '{"data":{"playerId":"simulation_player_1_dont_delete","gameId":"simulationGame_12345"}}' https://us-central1-colorhunt-fdc67.cloudfunctions.net/registerPlayerForGame
echo -e "\t<- register player1 for normal game" 
curl -X POST -H "Content-Type: application/json" -d '{"data":{"playerId":"simulation_player_2_dont_delete","gameId":"simulationGame_12345"}}' https://us-central1-colorhunt-fdc67.cloudfunctions.net/registerPlayerForGame
echo -e "\t<- register player2 for normal game"

# commit first player result (winning player)
curl -X POST -H "Content-Type: application/json" -d '{"data":{
    "playerId":"simulation_player_1_dont_delete",
    "gameId":"simulationGame_12345",
    "accuracy":"100",
    "distance":"100",
    "time":"100",
    "image":"test1img"}}' https://us-central1-colorhunt-fdc67.cloudfunctions.net/commitGameResult
echo -e "\t<- commit player1 game result"

# commit second player result (losing player)
curl -X POST -H "Content-Type: application/json" -d '{"data":{
    "playerId":"simulation_player_2_dont_delete",
    "gameId":"simulationGame_12345",
    "accuracy":"90",
    "distance":"100",
    "time":"100",
    "image":"test1img"}}' https://us-central1-colorhunt-fdc67.cloudfunctions.net/commitGameResult
echo -e "\t<- commit player2 game result"

# wait for game to be correctly finished
sleep 2
echo -e "wait for game to be cleaned up"

# play friend game
response=$(curl -s -X POST -H "Content-Type: application/json" -d '{"data":{"playerId":"simulation_player_1_dont_delete","friendId":"simulation_player_2_dont_delete"}}' https://us-central1-colorhunt-fdc67.cloudfunctions.net/registerFriendGame)
gameId=$(echo -e "$response" | jq -r '.result.gameId')
echo -e "$response\t<- register friend game"
curl -X POST -H "Content-Type: application/json" -d "{\"data\":{\"gameId\":\"$gameId\",\"join\":true}}" https://us-central1-colorhunt-fdc67.cloudfunctions.net/joinFriendGame
echo -e "\t<- join friend game"

# commit first player result (winning player)
curl -X POST -H "Content-Type: application/json" \
  -d "{\"data\":{
    \"playerId\":\"simulation_player_1_dont_delete\",
    \"gameId\":\"$gameId\",
    \"accuracy\":\"100\",
    \"distance\":\"100\",
    \"time\":\"100\",
    \"image\":\"test1img\"}}" \
  https://us-central1-colorhunt-fdc67.cloudfunctions.net/commitGameResult
echo -e "\t<- commit friend1 game result"

# commit second player result (losing player)
curl -X POST -H "Content-Type: application/json" \
  -d "{\"data\":{
    \"playerId\":\"simulation_player_2_dont_delete\",
    \"gameId\":\"$gameId\",
    \"accuracy\":\"90\",
    \"distance\":\"100\",
    \"time\":\"100\",
    \"image\":\"test1img\"}}" \
  https://us-central1-colorhunt-fdc67.cloudfunctions.net/commitGameResult
echo -e "\t<- commit friend2 game result"