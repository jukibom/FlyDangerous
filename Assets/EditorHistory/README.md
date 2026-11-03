# Editor History
<a href="https://openupm.com/packages/com.bedtime.editor-history/">
  <img src="https://img.shields.io/npm/v/com.bedtime.editor-history?label=openupm&amp;registry_uri=https://package.openupm.com" />
</a>

A small tool with a huge workflow impact!
With Editor History installed you can navigate back and forth through your selection history with the back/forward buttons on your mouse!
The selection history can also be navigated through its own window by clicking the Editor History button in the Unity toolbar.

![image](https://user-images.githubusercontent.com/1178324/168482537-0880cd56-038d-44fc-8684-a7de92e6b830.png)

By default this window acts as a modal that will close if you click away from it, but in the top right corner of the window you can pop it out into a seperate window that can be docked anywhere, should you want to keep it open permanently.

# Getting started
Simply install the package, and now by default, your Back/Forward mouse buttons will navigate back and forth through your history. The shortcuts can be changed to any other key from Unity's Shortcut manager.
**Unity has no support for mouse shortcuts prior to version 2022.2! Instead the package will fall back to hardcoded Mouse4/5 button polling directly from the WIN32 API.**
(This also means that there is no mouse support at all for MacOS Unity versions prior to 2022.2)

## Git + Unity Package Manager
Add this URL to your Unity Package manager as a git package

```https://github.com/BedtimeDigitalGames/Unity-Editor-History.git#1.4.1``` 

![image](https://user-images.githubusercontent.com/104233613/164909451-0ca62c24-0106-463b-9c4b-e7fbcd6409ad.png)

## OpenUPM
```$ openupm add com.bedtime.editor-history```

## UnityPackage
[Download](https://github.com/BedtimeDigitalGames/Unity-Editor-History/releases/download/1.4.1/com.bedtime.editor-history.unitypackage)

# How to use
 - Click back and forwards on your mouse to navigate through your selection history
 - Alternatively click the Editor History button (clock icon) in the top Unity toolbar to open a list of your selection history
