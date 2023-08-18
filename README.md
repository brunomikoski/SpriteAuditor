# Sprite Auditor


[![openupm](https://img.shields.io/npm/v/com.brunomikoski.spriteauditor?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.brunomikoski.spriteauditor/)

![](https://img.shields.io/github/followers/brunomikoski?label=Follow&style=social) ![](https://img.shields.io/twitter/follow/brunomikoski?style=social)

[Wrote a small guide about how to use it](https://medium.com/@bada/optimizing-build-size-and-performance-with-proper-sprite-setup-7c76c91626b6)

![inspector](/Documentation~/home-view.png)

Sprite auditor its the tool to give you the visibility when optimizing your atlases for smaller binary size or performance, 
allowing you see what atlas are reused between multiple scenes, and what sprites are in use from that atlas, giving you the visibility to now only gain performance by making sure you are only using the right atlas on the right place, 
but also helping you see witch texture are in use that could be smaller.

![custom-search](/Documentation~/custom-search.gif)


## Features
- Captures frame by frame all the sprites usages, so I can recognize when a Sprite has transform scales and can show what would be the ideals size for that sprite.
- Records the session of the game identifying sprites been used by the UI or SpriteRender giving you valuable information
- Allow customization of specific listeners to report special cases sprite usages
- Give you insights about images to canvas size, allow you understand if you can increase or decrease the image size
- Allows you see per scene what atlas are currently in use and what sprites are been used where.
- See what sprites are been used per scene and tweak your atlases to accomodate that properly.
- Can auto fix single sprites to the best possible usage

## How to use
 - Set your target Resolution on the Game View 
 - Open the Sprite Auditor from the `Window / Analysis / Sprite Auditor` Menu
 - Make sure you you select the `Record on Play` option enable.
 - Play thought the project or let your Unit Test run to the project.
 - After this you can start optimizing your atlas


## System Requirements
Unity 2018.4.0 or later versions


## How to install

<details>
<summary>Add from OpenUPM <em>| via scoped registry, recommended</em></summary>

This package is available on OpenUPM: https://openupm.com/packages/com.brunomikoski.spriteauditor

To add it the package to your project:

- open `Edit/Project Settings/Package Manager`
- add a new Scoped Registry:
  ```
  Name: OpenUPM
  URL:  https://package.openupm.com/
  Scope(s): com.brunomikoski
  ```
- click <kbd>Save</kbd>
- open Package Manager
- click <kbd>+</kbd>
- select <kbd>Add from Git URL</kbd>
- paste `com.brunomikoski.spriteauditor`
- click <kbd>Add</kbd>
</details>

<details>
<summary>Add from GitHub | <em>not recommended, no updates :( </em></summary>

You can also add it directly from GitHub on Unity 2019.4+. Note that you won't be able to receive updates through Package Manager this way, you'll have to update manually.

- open Package Manager
- click <kbd>+</kbd>
- select <kbd>Add from Git URL</kbd>
- paste `https://github.com/brunomikoski/SpriteAuditor.git`
- click <kbd>Add</kbd>
</details>
