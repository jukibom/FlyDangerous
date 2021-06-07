![logo](https://user-images.githubusercontent.com/5649179/115070912-78705e80-9eed-11eb-9b18-70e6e05b2c8f.png)

## A love letter to the Elite Dangerous racing community ❤

Yes, racing exists in Elite and yes it's awesome -- but accessible it is not. 

This project aims to provide a ground-up reimplementation of a similar-enough flight model to provide a training ground and common set of tools to track leaderboards, ghosts and general tom-foolery. Feedback is extremely welcome!

[Download on itch.io](https://jukibom.itch.io/fly-dangerous)

[Join the discord](https://discord.gg/4daSEUKZ6A)

## Planned Features

* Flight sticks of all shapes and sizes (done!)
* Basic time trial map types
  * Sprint: 1 start, checkpoints, 1 end (done!)
  * Laps 1 start block, checkpoints, lap counter
* Leaderboards with automatic replay / ghost upload via Steam
* Basic Multiplayer
* VR Support
* Infinite terrain generation (done!)
  * Freeplay flight, seed generation (done!)
  * Record + share racetracks (format supports it)

## Building

Unity build: 2021.1.7f1

Relies on following assets:

* Map Magic 2 Bundle - https://assetstore.unity.com/packages/tools/terrain/mapmagic-2-bundle-178682 
* Allsky - https://assetstore.unity.com/packages/2d/textures-materials/sky/allsky-200-sky-skybox-set-10109
* SC Post Effects - https://assetstore.unity.com/packages/vfx/shaders/fullscreen-camera-effects/sc-post-effects-pack-108753

IF YOU DO NOT HAVE THESE ASSETS (and, why would you?) and do not wish to buy them, follow these instructions to build:

* Ensure you have https://assetstore.unity.com/packages/tools/terrain/mapmagic-2-165180 in your account (the free version of map magic will suffice, although you will miss some features and may not be able to load some terrains).
 
* Clone the repository

* Find the forward renderer asset in `Assets/Settings` and remove the missing renderer feature in the inspector. This is the SC Post fog effect.

* Go to Edit > Project Settings > Player > Other Settings > Scripting Define Symbols. Add a new define, call it `NO_PAID_ASSETS`. This will disable any code references to non-free assets.

![image](https://user-images.githubusercontent.com/5649179/121093848-8eabe400-c7e5-11eb-83a4-ba646ec68ffe.png)


## License

All code is distributed under the GPLv3 license (see LICENSE).
All models, textures and materials which are developed solely for this project (e.g. are not third party assets) are distributed under the Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International license (see ASSET-LICENSE)
Permission to reuse the logo and name is not permitted.
