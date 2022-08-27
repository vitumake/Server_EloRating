# Server_EloRating
Mod for a game called Blockland

Elo is calculated on each kill or death and the difference is subtracted from the losing player's Elo rating and added to the winning player.

The equation:
%expectedScore = 1 / (1 + mPow(10, ((2Elo - %1Elo)/400)))
%eloDifference = %K * (%score - %expectedScore)
%K is the elo coefficient
%Score is the outcome of the fight. In this case either 1 or 0

Commands:
/stats - name/bl_id

Admins:
/eloReset - name/bl_id/all

Prefs:
$Pref::EloRating::eloKVal - Change coefficient value
$Pref::EloRating::fightsToRating - How many fight until you get your rating.
$Pref::EloRating::fightsWelcomeMsg - Display fights left till rating on join.
$Pref::EloRating::resetPerm /eloReset command permissions
$Pref::EloRating::resetAllHost /eloReset ALL command host only.

Prefs can be changed from the glass server menu.
If you are an admin you can type /printEloPrefs to see all the preferences ingame.
