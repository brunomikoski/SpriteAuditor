# Sprite Auditor
[![License: MIT](https://img.shields.io/badge/License-MIT-brightgreen.svg)](https://github.com/badawe/PresetManager/blob/develop/LICENSE)
[![openupm](https://img.shields.io/npm/v/com.brunomikoski.spriteauditor?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.brunomikoski.spriteauditor/)

![](https://img.shields.io/github/followers/badawe?label=Follow&style=social) ![](https://img.shields.io/twitter/follow/brunomikoski?style=social)

![inspector](/Documentation~/main-view.png)

Sprite auditor its the tool to give you the visibility when optimizing your atlases for smaller binary size or performance, 
allowing you see what atlas are reused between multiple scenes, and what sprites are in use from that atlas, giving you the visibility to now only gain performance by making sure you are only using the right atlas on the right place, 
but also helping you see witch texture are in use that could be smaller.

![custom-search](/Documentation~/custom-search.gif)


## Features
- Records the session of the game identifying sprites been used by the UI or SpriteRender giving you valuable information
- Allow customization of specific listeners to report special cases sprite usages
- Give you insights about images to canvas size, allow you understand if you can increase or decrease the image size
- Allows you see per scene what atlas are currently in use and what sprites are been used where.
- See what sprites are been used per scene and tweak your atlases to accomodate that properly.
- Can auto fix single sprites to the best possible usage

## How to use
 - Set your target Resolution on the Game View 
 - Open the Sprite Auditor from the `Tools / Sprite Auditor` Menu
 - Make sure you you select the `Record on Play` option enable.
 - Play thought the project or let your Unit Test run to the project.
 - After this you can start optimizing your atlas


## System Requirements
Unity 2018.4.0 or later versions


## Installation

### OpenUPM
The package is available on the [openupm registry](https://openupm.com). It's recommended to install it via [openupm-cli](https://github.com/openupm/openupm-cli).

```
openupm add com.brunomikoski.spriteauditor
```

### Manifest
You can also install via git URL by adding this entry in your **manifest.json**
```
"com.brunomikoski.spriteauditor": "https://github.com/badawe/SpriteAuditor.git"
```

### Unity Package Manager
```
from Window->Package Manager, click on the + sign and Add from git: https://github.com/badawe/SpriteAuditor.git
```
