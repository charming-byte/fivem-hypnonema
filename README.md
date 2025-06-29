<h1>Hypnonema 🎬 🎥</h1>
<h5>Media Player Resource for FiveM</h5>

![GitHub Repo stars](https://img.shields.io/github/stars/thiago-dev/fivem-hypnonema?style=social)
![GitHub release (latest by date including pre-releases)](https://img.shields.io/github/v/release/thiago-dev/fivem-hypnonema?include_prereleases&style=flat-square)
![GitHub all releases](https://img.shields.io/github/downloads/thiago-dev/fivem-hypnonema/total?style=flat-square)
[![CC BY-NC-SA 4.0][cc-by-nc-sa-shield]][cc-by-nc-sa]
![GitHub issues](https://img.shields.io/github/issues-raw/thiago-dev/fivem-hypnonema?style=flat-square)


[![Hypnonema](https://raw.githubusercontent.com/thiago-dev/fivem-hypnonema/gh-pages/HypnonemaTitle.jpg)](https://raw.githubusercontent.com/thiago-dev/fivem-hypnonema/gh-pages/HypnonemaTitle.jpg "Hypnonema")
## Table of Contents

- [Table of Contents](#table-of-contents)
- [Features](#features)
- [Demo](#demo)
- [Requirements](#requirements)
- [Installation](#installation)
- [Permissions](#permissions)
  - [Example Permission Config](#example-permission-config)
- [(Optional) Example Map](#optional-example-map)
- [Customization](#customization)
  - [Change command](#change-command)
- [Exports](#exports)
  - [Server-Side](#server-side)
    - [play](#play)
    - [pause](#pause)
    - [stop](#stop)
    - [resume](#resume)
    - [repeat](#repeat)
    - [seek](#seek)
    - [getScreenList](#getscreenlist)
    - [createScreen](#createscreen)
    - [editScreen](#editscreen)
    - [deleteScreen](#deletescreen)
    - [getDuiState](#getduistate)
- [License](#license)
- [Support](#support)

## Features
- Synchronized
- All HTML5 supported audio and video types including HLS/DASH plus Twitch / YouTube / DailyMotion / Facebook / Vimeo / Streamable / Vidme / Wistia / SoundCloud
- Multi-Screen capable (watch multiple movies at once)
- NUI Frontend
- ACE-Permissions
- Live-Edit Feature (simplifies scaleform placement)

## Demo
[Click me](https://youtu.be/JckYo8bKdnE)

## Requirements
- Recent [FiveM-Server](https://runtime.fivem.net/artifacts/fivem/build_server_windows/master/?=t) Build

## Installation
1. Download the latest Release from [Github](https://github.com/thiago-dev/fivem-hypnonema/releases) and extract the zip file to your resources directory.
2. (Optional) Edit permissions.cfg to your likings. See [Permissions](#permissions) for more information.
3. Edit the `server.cfg` and add following lines:
```
exec @hypnonema/permissions.cfg
start hypnonema
```

## Permissions
By default only members of `group.admin` are allowed to interact with hypnonema.
To permit users to make use of all available functionality make sure to edit the `permissions.cfg` file inside the resource directory.

Below is a list of all available permission settings.
| Permission                  | Description                                  |
|-----------------------------|----------------------------------------------|
| `hypnonema.screens.edit`    | Allow editing existing screens               |
| `hypnonema.screens.create`  | Allow creating screens                       |
| `hypnonema.screens.delete`  | Allow deleting screens                       |
| `hypnonema.playback.play`   | Allow playing videos                         |
| `hypnonema.playback.resume` | Allow resuming paused videos                 |
| `hypnonema.playback.repeat` | Allow enabling/disabling repeating of videos |
| `hypnonema.playback.pause`  | Allow pausing videos                         |
| `hypnonema.playback.stop`   | Allow stopping videos                        |

### Example permission config
To restrict creating / editing screens to admins only but allow everyone to control the playback use following settings.
```
add_ace group.admin hypnonema.screens allow
add_ace builtin.everyone hypnonema.playback allow
```

## (Optional) Example Map
There is an example map included, just copy the folder **hypnonema-map** to your resource directory and paste following line into your `server.cfg`.
```
start hypnonema-map
``` 


## Customization
### Change command
If you want to use your own command just edit the `fxmanifest.lua`. Don't add preceding slashes or any special characters.

```
hypnonema_command_name 'mynewcommand'
```
## Exports
### Server-side
#### play
```lua
exports.hypnonema:play(screenName, url)
```
#### pause
```lua
exports.hypnonema:pause(screenName)
```

#### stop
```lua
exports.hypnonema:stop(screenName)
```

#### resume
```lua
exports.hypnonema:resume(screenName)
```

#### repeat
```lua 
exports.hypnonema:repeat(screenName, shouldRepeat)
```

#### seek
```lua
exports.hypnonema:seek(screenName, time)
```

#### getScreenList
```lua
local screens = json.decode(exports.hypnonema:getScreenList())
```

#### createScreen
_Note: The parameter has to be in JSON format._
```lua
local exampleScreen = {
    AlwaysOn = false,
    DuiBrowserSettings = {
        GlobalVolume = 100,
        Is3DAudioEnabled = true,
        SoundAttenuation = 10,
        SoundMaxDistance = 200,
        SoundMinDistance = 10,
    },
    Is3DRendered = true,
    Name = "Hypnonema Example Screen",
    PositionalSettings = {
        PositionX = -1678.949,
        PositionY = -928.3431,
        PositionZ = 20.6290932,
        RotationX = 0,
        RotationY = 0,
        RotationZ = -140,
        ScaleX = 0.969999969,
        ScaleY = 0.484999985,
        ScaleZ = -0.1,
    }
}

exports.hypnonema:createScreen(json.encode(exampleScreen))
```

#### editScreen
_Note: The parameter has to be in JSON format_
```lua
exports.hypnonema:editScreen(jsonScreen)
```

#### deleteScreen
```lua
exports.hypnonema:deleteScreen(screenName)
```

#### getDuiState
returns a list containing current player states.
```lua
local duiState = json.decode(exports.hypnonema.getDuiState())
```


## License
This work is licensed under a
[Creative Commons Attribution-NonCommercial 4.0 International License][cc-by-nc-sa].

[![CC BY-NC 4.0][cc-by-nc-sa-image]][cc-by-nc-sa]

## Support
Please use the [fivem-thread](https://forum.fivem.net/t/release-hypnonema-a-cinema-resource-update-now-with-twitch-support-c/783324) for support.

[cc-by-nc-sa-image]: https://licensebuttons.net/l/by-nc-sa/4.0/88x31.png

[cc-by-nc-sa]: http://creativecommons.org/licenses/by-nc-sa/4.0/
[cc-by-nc-sa-shield]: https://img.shields.io/badge/License-CC%20BY--NC--SA%204.0-lightgrey.svg
