# DU Build System

This repository contains the sources for a Windows .NET8 application to create
packaged, *installable* LUA scripts for the game *Dual Universe* by Novaquark.  
Here *installable* means JSON files, that can be pasted onto ingame flight controllers
or programming boards.

Originally developed by the ingame organisation '*Shadow Templars*', the authors left the game
but kindly released the sources to the public in spring of 2024.  

For further details please also visit the [Horizon2 repository](https://github.com/Otixa/horizon2/)

New maintainers on these repositories are working on updating the codebase to become compatible with the latest game version's LUA API.

## Updates

### 2024-02-26 by @tobitege

* Codebase revised to fix deprecated DU API entry points (start() -> onStart() etc.).
* Created this README.md file.
