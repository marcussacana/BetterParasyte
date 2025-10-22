# BetterParasyte
A tool that parasyte the discord updater tool to ensure each startup that BetterDiscord is installed.

## How to install
- Download BetterParasyte
- Install the BetterDiscord if never installed before
- Execute the BetterParasyte
- Type `Y` to confirm the installation
- Start the Discord

## How to uninstall
- Delete the `Discord` folder at `%localappdata%`

## Limitations
- Once discord updates, it will start one time without better discord  

This limitation is because on first launch of each discord update, the discord needs to upgrade from 'minimal' installation to the 'full' installation, and the patching can be done only after the discord is fully updated.
To "fix" you just need to restart the discord once.

- Require .NET 4.6.1

This tool patches the discord updater executable, wich has made with .Net 4.5, due this tool dependency the minimal version that it works is the .NET 4.6.1.

## Advantages
We already have some tools that aims to recovery the betterdiscord automatically, but those tools relay on windows startup to ensure the betterdiscord installation every time you boot up the computer.
This method does not relay on that, the check is done when you start the discord only, 
