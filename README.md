# Sprite Auditor
[![License: MIT](https://img.shields.io/badge/License-MIT-brightgreen.svg)](https://github.com/badawe/PresetManager/blob/develop/LICENSE)
[![openupm](https://img.shields.io/npm/v/com.brunomikoski.spriteauditor?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.brunomikoski.spriteauditor/)

![](https://img.shields.io/github/followers/badawe?label=Follow&style=social) ![](https://img.shields.io/twitter/follow/brunomikoski?style=social)

![inspector](/Documentation~/main-view.png)

Sprite auditor its the tool to give you the visibility when optimizing your atlases for smaller binary size or performance

## Features
- Records the session of the game identifying sprites been used by the UI or SpriteRender giving you valuable information
- Allow customization of specific listeners to report special cases sprite usages
- Give you insights about images to canvas size, allow you understand if you can increase or decrease the image size
- Allows you see per scene what atlas are currently in use and what sprites are been used where.
- See what sprites are been used per scene and tweak your atlases to accomodate that properly.


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
