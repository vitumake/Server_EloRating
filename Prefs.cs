registerPreferenceAddon("Server_EloRating", "EloRating", "gun");
new ScriptObject(Preference) {
  className      = "EloRating";
  addon          = "Server_EloRating";
  category       = "Settings";
  title          = "Elo coefficient value";
  type           = "num";
  params         = "0 40";
  variable       = "$Pref::Server::EloRating::eloKVal";
  defaultValue   = "40";
  updateCallback = "correctKVal";
  loadCallback   = "";
};

new ScriptObject(Preference) {
  className      = "EloRating"; 
  addon          = "Server_EloRating"; 
  category       = "Settings";
  title          = "Fights before recieving rating";
  type           = "num";
  params         = "0 100"; 
  variable       = "$Pref::Server::EloRating::fightsToRating"; 
  defaultValue   = "10"; 
  updateCallback = "updatePrefix"; 
  loadCallback   = ""; 
};

new ScriptObject(Preference) {
  className      = "EloRating"; 
  addon          = "Server_EloRating"; 
  category       = "Settings";
  title          = "Display fights left on join";
  type           = "bool";
  params         = ""; 
  variable       = "$Pref::Server::EloRating::fightsWelcomeMsg"; 
  defaultValue   = "1"; 
  updateCallback = ""; 
  loadCallback   = ""; 
};

new ScriptObject(Preference) {
  className      = "EloRating";
  addon          = "Server_EloRating";
  category       = "Permissions";
  title          = "Elo reset command permission";
  type           = "dropdown";
  params         = "Admin 1 SuperAdmin 2 Host 3";
  variable       = "$Pref::Server::EloRating::resetPerm";
  defaultValue   = "SuperAdmin 2"; 
  updateCallback = ""; 
  loadCallback   = ""; 
  hostOnly       = true; 
};

new ScriptObject(Preference) {
  className      = "EloRating";
  addon          = "Server_EloRating";
  category       = "Permissions";
  title          = "Elo reset ALL host only";
  type           = "bool";
  params         = "";
  variable       = "$Pref::Server::EloRating::resetAllHost";
  defaultValue   = "1";
  updateCallback = "";
  loadCallback   = ""; 
  hostOnly       = true;
};

//Pref update function and fixes to avoid blglass retardation
function updatePrefix(%this, %val)
{
  if(%val < 5)
    $Pref::Server::EloRating::fightsToRating = 5;
  for(%a = 0; %a < NPL_List.rowCount(); %a++)
  {
    findClientByBL_ID(getField(NPL_List.getRowText(%a), 3)).updateElo();
  }
}
//This is needed cause glass makes it fucking impossible to change the value if I use the built in method
function correctKVal(%this, %val)
{
  if(%val < 10)
    $Pref::Server::EloRating::eloKVal = 10;
}
