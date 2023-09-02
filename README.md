### SteamUGCKVPViewer
SteamUGCKVPViewer is a tool that allows you you check all Key-Value-Pairs (added via SteamUGC::AddItemKeyValueTag) that a Steam workshop mod currently has  
First you need to enter the correct steamapp id in steam__appid.txt (currently defaults to the game Rain World) you want to check.  
Then you need to ensure that the Steam client is running and online.  
If everything works out correctly the console application will give you a JSON output of all mods you are currently subscribed to (for the game specified via steam_appid.txt_)

Although not confirmed it seems there might be a limit for 17 values per key, if this limit is reached you will very likely get a k_EResultInvalidParam error when calling AddItemKeyValueTag with that key.  
This was at least observed for the game Rain World.  
This tool is useful for checking if any mods reached that limit.