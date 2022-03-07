# FizzySteamworks

This is a community maintained repo forked from **[RayStorm](https://github.com/Raystorms/FizzySteamyMirror)**. 

Mirror **[docs](https://mirror-networking.com/docs/Transports/Fizzy.html)** and the official community **[Discord](https://discord.gg/N9QVxbM)**.

FizzySteamworks brings together **[Steam](https://store.steampowered.com)** and **[Mirror](https://github.com/vis2k/Mirror)** . It supports both the old SteamNetworking and the new SteamSockets.

## Dependencies
If you want an easy import, skip the steps below & download the latest **[unitypackage](https://github.com/Chykary/FizzySteamworks/releases)**, **it has SteamWorks.Net already included**.

**Note: If you already have SteamWorks.Net in your project, you might need to delete either your import or the one included in the FizzySteamworks [unitypackage](https://github.com/Chykary/FizzySteamworks/releases).**

Both of these projects need to be installed and working before you can use this transport.
1. **[SteamWorks.NET](https://github.com/rlabrecque/Steamworks.NET)** FizzySteamworks relies on Steamworks.NET to communicate with the **[Steamworks API](https://partner.steamgames.com/doc/sdk)**. **Requires .Net 4.x**  
2. **[Mirror](https://github.com/vis2k/Mirror)** FizzySteamworks is also obviously dependant on Mirror which is a streamline, bug fixed, maintained version of UNET for Unity.

## Setting Up

1. Install Mirror **(Requires Mirror 35.0+)** from the Unity asset store **[Download Mirror](https://assetstore.unity.com/packages/tools/network/mirror-129321)**.
2. Install FizzySteamworks **[unitypackage](https://github.com/Chykary/FizzySteamworks/releases)** from the release section.
3. In your **"NetworkManager"** object replace **"Telepathy"** script with **"FizzySteamworks"** script.
4. Enter your Steam App ID in the **"FizzySteamworks"** script.

**Note: The  default 480(Spacewar) appid is a very grey area, technically, it's not allowed but they don't really do anything about it. When you have your own appid from steam then replace the 480 with your own game appid.
If you know a better way around this please make a [Issue ticket.](https://github.com/Chykary/FizzySteamworks/issues)**

## Host
To be able to have your game working you need to make sure you have Steam running in the background. **SteamManager will print a Debug Message if it initializes correctly.**

## Client
Before sending your game to your buddy make sure you have your **steamID64** ready. To find your **steamID64** the transport prints the hosts **steamID64** in the console when the server has started.

1. Send the game to your buddy. The transport shows your Steam User ID after you have started a server.
2. Your buddy needs your **steamID64** to be able to connect.
3. Place the **steamID64** into **"localhost"** then click **"Client"**
5. Then they will be connected to you.

## Testing your game locally
You cant connect to yourself locally while using **FizzySteamworks** since it's using steams P2P. If you want to test your game locally you'll have to use **"Telepathy Transport"** instead of **"FizzySteamworks"**.
