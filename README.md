# Board Persistence Plugin

This is a plugin for TaleSpire using BepInEx.

## Install

Currently you need to either follow the build guide down below or use the R2ModMan.

## Usage

This plugin is specifically for developers to easily implement extra Radial buttons based on a entity properties.
Developers should reference the DLL for their own projects. This does not provide anything out of the box.

Messages are sent using a key/value pair system. Typically a plugin will set one or more keys and
will monitor for those key changes. To set a key with a value, issue the following command:

```C#
BoardPersistence.SetInfo(*keyName*, *content*);
```

Where keyName is a unique string that identifies the content. The keyName can be considered to
identify the communication channel (not the piece of data). Typically a plugin will use one key
for its information but in some cases a plugin may more than one key.

Where content is a string of the data to be sent.

It should be noted that BoardPersistence works on changes. Sending the same content as was already
posted will not generate new notifications. If it is possible that the same content needs to be
send multiple times, the plugin will need to implement a reset (e.g. change content to blank and
have the plugin ignore blank changes) and then repost the desired content.

To clear a key that is no loner needed, use the following code:

```C#
BoardPersistence.ClearInfo(*keyName*);
```


## How to Compile / Modify

Open ```BoardPersistencePlugin.sln``` in Visual Studio.

You will need to add references to:

```
* BepInEx.dll  (Download from the BepInEx project.)
* Bouncyrock.TaleSpire.Runtime (found in Steam\steamapps\common\TaleSpire\TaleSpire_Data\Managed)
* UnityEngine.dll
* UnityEngine.CoreModule.dll
* UnityEngine.InputLegacyModule.dll 
* UnityEngine.UI
* Unity.TextMeshPro
```

Build the project.

Browse to the newly created ```bin/Debug``` or ```bin/Release``` folders and copy the ```BoardPersistencePlugin.dll``` to ```Steam\steamapps\common\TaleSpire\BepInEx\plugins```

## Changelog
1.0.1: Bump, Add Tags for ThunderStore
1.0.0: Initial release

## Shoutouts
Shoutout to my Patreons on https://www.patreon.com/HolloFox recognising your
mighty contribution to my caffeine addiciton:
- John Fuller

This plugin was modified from LordAshes's StatMessaging Plugin.
Instead of being oriented on a mini, data is oriented on a board.