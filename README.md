# Preset Manager
[![License: MIT](https://img.shields.io/badge/License-MIT-brightgreen.svg)](https://github.com/badawe/PresetManager/blob/develop/LICENSE)
[![openupm](https://img.shields.io/npm/v/com.brunomikoski.presetmanager?label=openupm&registry_uri=https://package.openupm.com)](https://openupm.com/packages/com.brunomikoski.presetmanager/)

![](https://img.shields.io/github/followers/badawe?label=Follow&style=social) ![](https://img.shields.io/twitter/follow/brunomikoski?style=social)

Preset Manager is the missing per folder preset for Unity.

Applies Preset to objects respecting the folder structure and settings.


## Features
- Load all available Presets in the Project
- When a folder is selected, verify if there's an asset that can use one of the available presets.
- Display the Options per asset type 
- Automatically applies to new assets
- You can apply the specific preset per folder.
- *Allow customization of ignored properties!*
- Relative changes based on specific folder

## System Requirements
Unity 2018.4.0 or later versions. Feel free to try an older version, should work just fine.

## Installation

### OpenUPM
The package is available on the [openupm registry](https://openupm.com). It's recommended to install it via [openupm-cli](https://github.com/openupm/openupm-cli).

```
openupm add com.brunomikoski.presetmanager
```

### Manifest
You can also install via git URL by adding this entry in your **manifest.json**
```
"com.brunomikoski.presetmanager": "https://github.com/badawe/PresetManager.git"
```

### Unity Package Manager
```
from Window->Package Manager, click on the + sign and Add from git: https://github.com/badawe/PresetManager.git
```

![inspector](/Documentation~/general-usage.gif) ![Manager](/Documentation~/add-new-from-manager.gif) ![properties](/Documentation~/properties-modification.gif)
