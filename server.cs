////////////////////////////////////////////////////////////////////
// By: c[_] (40725)										      	  //
////////////////////////////////////////////////////////////////////
// Elo based player prefix							 	          //
// Code written using Pecon's (9643) levels mod as a base <3      //
////////////////////////////////////////////////////////////////////
// The equation												      //
// %expectedScore = 1 / (1 + mPow(10, ((2Elo - %1Elo)/400)))	  //
// %eloDifference = %K * (%score - %expectedScore)			      //
// %K is the elo coefficient				                      //
// %Score is the outcome of the fight. In this case either 1 or 0 //
////////////////////////////////////////////////////////////////////

//Glass preferences
if(isFunction(registerPreferenceAddon))
{
	exec("./Prefs.cs");
}
else
{
	if($Pref::Server::EloRating::eloKVal $= "" || $Pref::Server::EloRating::eloKVal < 40)
		$Pref::Server::EloRating::eloKVal = 40;
	if($Pref::Server::EloRating::fightsToRating $= "" || $Pref::Server::EloRating::fightsToRating < 5)
		$Pref::Server::EloRating::fightsToRating = 10;
	if($Pref::Server::EloRating::resetPerm $= "")
		$Pref::Server::EloRating::resetPerm = 2;
	if($Pref::Server::EloRating::resetAllHost $= "")
		$Pref::Server::EloRating::resetAllHost = 1;
	if($Pref::Server::EloRating::fightsWelcomeMsg $= "")
		$Pref::Server::EloRating::fightsWelcomeMsg = 1;
	if($Pref::Server::EloRating::debug $= "")
		$Pref::Server::EloRating::debug = 0;
}
if(isPackage(eloRating))
	deactivatePackage(eloRating);
package eloRating
{
	function player::damage(%this, %obj, %pos, %directDamage, %damageType)
	{
		if(isObject(%obj.sourceObject.client) && %obj.sourceObject != %this && %this.getClassName() $= "Player")
		{
			if(%directDamage >= %this.getDatablock().maxDamage - %this.getDamageLevel())
			{
				%p1 = %this.client;
				%p2 = %obj.sourceObject.client;
				%p2.kills++;
				%p1.deaths++;
				%p1.elo = %p1.elo + mEloDiff(%p1.elo, %p2.elo, 0);
				%p2.elo = %p2.elo + mEloDiff(%p2.elo, %p1.elo, 1);
				%p1.updateElo();
				%p2.updateElo();
			}
		}
		parent::damage(%this, %obj, %pos, %directDamage, %damageType);
	}
	function gameConnection::autoAdminCheck(%this)
	{
		%this.resetAllCheck = 0;
		%this.loadElo();
		if(%this.name $= "" || %this.elo $= "")
		{
			%this.elo = 0;
			%this.deaths = 0;
			%this.kills = 0;
		}
		if(%this.kills + %this.deaths < $Pref::Server::EloRating::fightsToRating && $Pref::Server::EloRating::fightsWelcomeMsg)
			messageClient(%this, '', "\c5You have" SPC ($Pref::Server::EloRating::fightsToRating - (%this.kills + %this.deaths)) SPC "fights left before you recieve your rating!");
		%this.updateElo(1);
		return parent::autoAdminCheck(%this);
	}
	function serverCmdeloReset(%client, %arg)
	{
		switch($Pref::Server::EloRating::resetPerm)
		{
			case 1:
				if(!%client.isAdmin)
					return %client.chatMessage("You have to be an Admin to use this command!");
			case 2:
				if(!%client.isSuperAdmin)
					return %client.chatMessage("You have to be a Super Admin to use this command!");
			case 3:
				if(%client.bl_id != getNumKeyID())
					return %client.chatMessage("This command is host only!");
			default:
				return talk("How have you managed this?");
		}
		if(%arg $= "")
		{
			%client.chatMessage("\c6Please specify the \c2NAME\c6 or the \c2BL_ID\c6 of the target.");
			return %client.chatMessage("\c6To wipe everything use \c0/eloReset ALL");
		}
		if(%arg $= "all" && %client.isSuperAdmin)
		{
			if($Pref::Server::EloRating::resetAllHost && %client.bl_id != getNumKeyID())
			{
				return %client.chatMessage("This command is host only!");
			}
			else
			{
			%client.chatMessage("\c0This command will reset everything and you can not reverse this.");
			%client.chatMessage("\c6Type \"\c0RESETALL\c6\" if you are sure you want to delete everyhting");
			%client.chatMessage("\c6Type \c0cancel\c6 to cancel");
			%client.resetAllCheck = 1;
			return;
			}
		}
		else
		{
			if(isFile("config/server/EloRating/playerData/" @ %arg @ ".dat"))
				%id = %arg;
			else if(searchDataByName(%arg, "BLID") != false)
				%id = searchDataByName(%arg, "BLID");
			else
				return %client.chatMessage("Player not found!");
			%file = new FileObject(){};
			%file.openForRead("config/server/EloRating/playerData/" @ %id @ ".dat");
			while(!%file.isEOF())
			{
				%line = %file.readLine();
				if(getField(%line, 0) $= "NAME")
					%name = getField(%line, 1);
			}
			%file.close();
			%file.openForWrite("config/server/EloRating/playerData/" @ %id @ ".dat");
			%file.writeLine("ELO" TAB 0);
			%file.writeLine("KILLS" TAB 0);
			%file.writeLine("DEATHS" TAB 0);
			%file.writeLine("NAME" TAB %name);
			%file.close();
			%file.delete();
			findClientByBL_ID(%id).loadElo();
			findClientByBL_ID(%id).updateElo();
			%client.chatMessage("\c2" @ %id SPC %name @ "\'s stats have been reset!");
		}
	}
	function serverCmdMessageSent(%client, %msg)
	{
		if(getWord(%msg, 0) $= "resetall" && %client.resetAllCheck == 1)
		{
				%startTime = getRealTime();
				%dataCount = getFileCount("config/server/EloRating/playerData/*.dat");
				%file = new FileObject(){};
				%id = strreplace(findFirstFile("config/server/EloRating/playerData/*dat"), "config/server/EloRating/playerData/", "");
				%id = strreplace(%id, ".dat", "");
				for(%a = 0; %a < %dataCount; %a++)
				{
					if(%id $= "")
						break;
					%file.openForRead("config/server/EloRating/playerData/" @ %id @ ".dat");
					while(!%file.isEOF())
					{
						%line = %file.readLine();
						if(getField(%line, 0) $= "NAME")
							%name = getField(%line, 1);
					}
					%file.close();
					%file.openForWrite("config/server/EloRating/playerData/" @ %id @ ".dat");
					%file.writeLine("ELO" TAB 0);
					%file.writeLine("KILLS" TAB 0);
					%file.writeLine("DEATHS" TAB 0);
					%file.writeLine("NAME" TAB %name);
					%file.close();
					findClientByBL_ID(%id).loadElo();
					findClientByBL_ID(%id).updateElo();
					%id = strreplace(findNextFile("config/server/EloRating/playerData/*dat"), "config/server/EloRating/playerData/", "");
					%id = strreplace(%id, ".dat", "");
				}
				%file.delete();
				%timeElapsed = (getRealTime() - %startTime)/1000;
				messageAll('',"\c2Server_Elorating:" SPC "\c6All stats have been reset!");
				messageAll('MsgClearBricks',"\c2" @ %dataCount SPC "\c6files cleared in\c2" SPC %timeElapsed SPC "\c6seconds!");
				%client.resetAllCheck = 0;
				return;
		}
		else if(%client.resetAllCheck == 1)
		{
			%client.resetAllCheck = 0;
			return %client.chatMessage("\c2Action Canceled. \c6No stats have been lost.");
		}
		return parent::serverCmdMessageSent(%client, %msg);
	}
	function servercmdStats(%client, %id)
	{
		if(%id $= "")
			%id = %client.bl_id;
		else if(searchDataByName(%id, "BLID"))
			%id = searchDataByName(%id, "BLID");
		else
			return %client.chatMessage("User not found");
		%file = new FileObject(){};
		%file.openForRead("config/server/EloRating/playerData/" @ %id @ ".dat");
		while(!%file.isEOF())
		{
			%line = %file.readLine();
			%field = getField(%line, 0);
			%value = getField(%line, 1);
			switch$(%field)
			{
				case "ELO":
					%elo = %value;
				case "KILLS":
					%kills = %value;
				case "NAME":
					%name = %value;
				case "DEATHS":
					%deaths = %value;
			}
		}
		%file.close();
		%file.delete();
		%client.chatMessage("\c6Player:\c2" SPC %name SPC %id);
		if(%kills + %deaths >= $Pref::Server::EloRating::fightsToRating)
			%client.chatMessage("\c6Elo\c2" SPC mFloatLength(%elo, 1));
		else
			%client.chatMessage("\c2" @ ($Pref::Server::EloRating::fightsToRating - (%kills + %deaths)) SPC "\c6fights left");
		if(%kills + %deaths > 1)
			%client.chatMessage("\c6Winrate\c2" SPC mFloor(%kills/(%kills + %deaths)*100) @ "%%%");
		%client.chatMessage("\c6Kills\c2" SPC %kills);
		%client.chatMessage("\c6Deaths\c2" SPC %deaths);
	}
	function serverCmdprintEloPrefs(%client)
	{
		if(!%client.isAdmin)
			return;
		%client.chatMessage("$Pref::Server::EloRating::eloKVal =" SPC $Pref::Server::EloRating::eloKVal);
		%client.chatMessage("$Pref::Server::EloRating::fightsToRating =" SPC $Pref::Server::EloRating::fightsToRating);
		%client.chatMessage("$Pref::Server::EloRating::resetPerm =" SPC $Pref::Server::EloRating::resetPerm);
		%client.chatMessage("$Pref::Server::EloRating::resetAllHost =" SPC $Pref::Server::EloRating::resetAllHost);
		%client.chatMessage("$Pref::Server::EloRating::fightsWelcomeMsg =" SPC $Pref::Server::EloRating::fightsWelcomeMsg);
		%client.chatMessage("$Pref::Server::EloRating::debug =" SPC $Pref::Server::EloRating::debug);
	}
};
activatePackage("eloRating");
function searchDataByName(%name, %search)
{
	if(%name $= "")
		return -1;
	%file = new FileObject(){};
	%fileCount = getFileCount("config/server/EloRating/playerData/*dat");
	%dataFile = findFirstFile("config/server/EloRating/playerData/*dat");
	for(%a = 0; %a < %fileCount; %a++)
	{
		%file.openForRead(%dataFile);
		while(!%file.isEOF())
		{
			%line = %file.readLine();
			if(getField(%line, 0) $= "NAME")
			{
				if(getField(%line, 1) $= %name)
				{
					%file.close();
					%file.openForRead(%dataFile);
					while(!%file.isEOF())
					{
						%line = %file.readLine();
						if(%search $= "BLID")
						{
							%result = strreplace(%dataFile, "config/server/EloRating/playerData/", "");
							%result = strreplace(%result, ".dat", "");
						}
						else
						{
							if(getField(%line, 0) == %search)
								%result = getField(%line, 1);
							else
								%result = false;
						}
						%file.close();
						%file.delete();
						return %result;
					}
				}
			}
		}
		%file.close();
		%dataFile = findNextFile("config/server/EloRating/playerData/*dat");
	}
  %file.close();
  %file.delete();
	return false;
}
function gameConnection::debugElo(%client, %oppElo, %win)
{
	if($Pref::Server::EloRating::debug)
	{
		if(%win)
			%client.kills++;
		else
			%client.deaths++;
		%eloDiff = mEloDiff(%client.elo, %oppElo, %win);
		%client.elo += %eloDiff;
		%client.updateElo();
		talk(%client.elo);
	}
}
function mEloDiff(%opp1, %opp2, %win)
{
	%expScore = 1 / (1 + mPow(10, ((%opp2 - %opp1)/400)));
	%eloDiff = $Pref::Server::EloRating::eloKVal * (%win - %expScore);
	return %eloDiff;
}
function gameConnection::updateElo(%this, %gameConn)
{
	if(%this.elo < 0)
		%this.elo = 0;
	if(%this.kills + %this.deaths < $Pref::Server::EloRating::fightsToRating)
	{
		%toRank = $Pref::Server::EloRating::fightsToRating - (%this.kills + %this.deaths);
		if(%toRank == 5 && !%gameConn)
			messageClient(%this, '', "\c5You have" SPC %toRank SPC "fights left before you recieve your rating!");
		else if(%toRank == 1 && !%gameConn)
			messageClient(%this, '', "\c5You have" SPC %toRank SPC "fight left before you recieve your rating!");

		%this.clanPrefix = %this.oldPrefix @ "\c7[\c1" @ %toRank @ "\c7]";
		%this.saveElo();
		//updateLeaderboard();
		return;
	}
	else if(%this.kills + %this.deaths == $Pref::Server::EloRating::fightsToRating && !%gameConn)
		messageClient(%this, '', "\c5You have recieved your rating!");

	%this.clanPrefix = %this.oldPrefix @ "\c7[\c6" @ mFloor(%this.elo) @ "\c7]";
	%this.saveElo();
	searchEloData();
}
function gameConnection::saveElo(%this)
{
	%file = new FileObject(){};
	%file.openForWrite("config/server/EloRating/playerData/" @ %this.BL_ID @ ".dat");
	%file.writeLine("ELO" TAB %this.elo);
	%file.writeLine("KILLS" TAB %this.kills);
	%file.writeLine("DEATHS" TAB %this.deaths);
	%file.writeLine("NAME" TAB %this.name);
	%file.close();
	%file.delete();
}
function gameConnection::loadElo(%this)
{
	%file = new FileObject(){};
	%file.openForRead("config/server/EloRating/playerData/" @ %this.BL_ID @ ".dat");
	while(!%file.isEOF())
	{
		%line = %file.readLine();
		%field = getField(%line, 0);
		%value = getField(%line, 1);
		switch$(%field)
		{
			case "ELO":
				%this.elo = %value;
			case "KILLS":
				%this.kills = %value;
			case "DEATHS":
				%this.deaths = %value;
		}
	}
	%file.close();
	%file.delete();
}
