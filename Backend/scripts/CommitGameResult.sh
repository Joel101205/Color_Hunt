curl -X POST -H "Content-Type: application/json" -d '{"data":{
    "playerId":"1ZBxvjxaj4M4AHwnProG3wWCWBs2",
    "gameId":"12345",
    "accuracy":"100",
    "distance":"100",
    "time":"10",
    "image":"test"}}' https://us-central1-colorhunt-fdc67.cloudfunctions.net/commitGameResult | python3 -m json.tool