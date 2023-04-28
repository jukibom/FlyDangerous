<p align="center">
    <img src="https://github.com/jukibom/FlyDangerous/blob/main/Assets/Textures/UI/logo-dropshadow.png" alt="" data-canonical-src="https://github.com/jukibom/FlyDangerous/blob/main/Assets/Textures/UI/logo-dropshadow.png" width="400" />
</p>

## A love letter to the Elite Dangerous racing community ❤

Yes, racing exists in Elite and yes it's awesome -- but accessible it is not. 

This project aims to provide a ground-up reimplementation of a similar-enough flight model to provide a training ground and common set of tools to track leaderboards, ghosts and general tom-foolery. Feedback is extremely welcome!

[Download free on Steam](https://steam.flydangero.us)

[Download non-steam version on itch.io](https://itch.flydangero.us)

[Join the discord](https://discord.flydangero.us)

## Features

[Roadmap](https://github.com/users/jukibom/projects/1/views/5)

Implemented: 

* Flight sticks of all shapes and sizes
* Basic time trial map types
  * Sprint: 1 start, checkpoints, 1 end
  * Laps 1 start block, checkpoints, lap counter
* Leaderboards with automatic replay / ghost upload via Steam
  * Download and race against ghosts
* Basic Multiplayer
* VR Support
* Infinite terrain generation
  * Freeplay flight, seed generation

Planned:

* Record + share racetracks (format implementation supports it, needs in-game tools)
* Watch own or downloaded replays from various camera angles etc
* Multiplayer game modes for races and small-scale tournaments
* Score attack game mode

Check the issues / roadmap for more details!

## Integrations

The game supports various peripherals, accessible from the Integrations tab in the Options panel.

### VR

The game uses [OpenXR](https://www.khronos.org/openxr/) for headset VR integration. It can be toggled mid-game from the option panel or launched with the parameter `-vr` to start in VR mode.

VR is currently headset-only, gamepad / joystick / mouse + kb input and menu navigation is required.

**Unfortunately, Unity only supports OpenXR on Windows (I know, right?!) so for Linux it is advised to use Proton.**

### OpenTrack

[OpenTrack](https://github.com/opentrack/opentrack) is a head tracking application which interfaces with various hardware and webcams to provide head-tracking on flat screen displays.
Enable it with the option toggle and set OpenTrack to UDP output on 127.0.0.1, port 4242. 

For webcams, the neuralnet tracker input module is excellent. It is advised you bind a shortcut to recenter in the OpenTrack application.

### TrackIR

[TrackIR](https://www.trackir.com/) is a proprietary hardware head tracking solution. Fly Dangerous supports direct integration, simply enable the checkbox and go.
Be sure to have TrackIR installed and running when launching before launching into a map as the SDK is initialised when the player is created.

### bHaptics

[bHaptics](https://www.bhaptics.com/) is a suite of haptic feedback hardware. Fly Dangerous supports the TactSuit (x16 / x40) body vest, Tactocity for Arms and Tactal head feedback modules. Enable with the checkbox and suit should power up in-game.

### Sim Racing Studio

[Sim Racing Studio](https://www.simracingstudio.com/) is a universal motion simulation software suite with integration for various peripherals, motion rigs and related devices. Enable the checkbox here to integrate with SRS.

By default it uses 127.0.0.1 and port 33001 (SRS defaults) but may be modified in the config file (`simRacingStudioOutputAddress` and `simRacingStudioOutputPort`).

### UDP Telemetry

The game supports raw telemetry output in the form of a binary byte or json firehose for arbitrary custom integrations. This is an advanced feature, see [this ticket for more information](https://github.com/jukibom/FlyDangerous/issues/115).

## Building

Unity build: 2022.2.13f1

The release build is reliant on some paid assets (follow instructions underneath to build without for dev purposes!):

* Map Magic 2 Bundle 2.1.9 - https://assetstore.unity.com/packages/tools/terrain/mapmagic-2-bundle-178682 
* AllSky 5.1.0 - https://assetstore.unity.com/packages/2d/textures-materials/sky/allsky-200-sky-skybox-set-10109
* SC Post Effects 2.1.9 - https://assetstore.unity.com/packages/vfx/shaders/fullscreen-camera-effects/sc-post-effects-pack-108753
* GPU Instancer 1.7.2 - https://assetstore.unity.com/packages/tools/utilities/gpu-instancer-117566

**IF YOU DO NOT HAVE THESE ASSETS** (and, why would you?) and do not wish to buy them, follow these instructions to build:

* Ensure you have https://assetstore.unity.com/packages/tools/terrain/mapmagic-2-165180 in your Unity account and download it with the in-build package manager under the "My Assets" tab (the free version of map magic will suffice, although you will miss some features and may not be able to load some terrains). 
 
* Clone the repository

* Go to Edit > Project Settings > Player > Other Settings > Scripting Define Symbols. Add a new define, call it `NO_PAID_ASSETS`. This will disable any code references to non-free assets.

![image](https://user-images.githubusercontent.com/5649179/121093848-8eabe400-c7e5-11eb-83a4-ba646ec68ffe.png)

* Add Map Magic 2 to the project in the My Assets tab. You won't need the Compatibility or Demo folder, nor any of the additional assets. 

![image](https://user-images.githubusercontent.com/5649179/221643540-58c737fa-9846-4097-9a73-8355a409f277.png)

Once it's imported, move it into the `Standard Assets` folder so that it'll be ignored by git. If there's already a folder in there, just delete it first.

* **Optional**: If you do happen to have SC Post Effects, got to `Assets/Settings/UniversalRP-HighQuality` and swap out the `ForwardRendererNoFog` for `ForwardRenderer` (this is what is used in builds).

### Building for Linux and Apple (Silicon)

By default, the terrain generation is only supported on Windows and Intel Macs. Additional plugins have been compiled for this purpose which can be downloaded from [this thread](https://github.com/jukibom/FlyDangerous/issues/108#issuecomment-1498022236) (compiled from [source here](https://gitlab.com/denispahunov/tools/-/tree/master/.NativeSources)). Extract the zip file to your installation of MapMagic in the Tools/Plugins folder.

### Steam Integration

If you wish to build without Steam integration, add the Define Symbol `DISABLESTEAMWORKS`. This will completely disable all steam integration and UI functionality, and the multiplayer component will work with regular IPs + port forwarding.

### Running in the Unity Editor

The easiest way to get going is simply to load the Main Menu scene (`Assets/Scenes/UI/Main Menu`) but if you want to be able to jump right in when testing stuff, load `@Test Scene` from the root scenes folder and additively load a map and environment from their respective folders (set the environment as active scene for correct lighting etc). A ship player will be spawned at the location of the entity inside the `@Test Scene` root entity.

## License

All code is distributed under the GPLv3 license (see LICENSE).
All models, textures and materials which are developed solely for this project (e.g. are not third party assets) are distributed under the Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International license (see ASSET-LICENSE)
Assets may include additional LICENSE files as appropriate.
Permission to reuse the logo and name is not permitted.
