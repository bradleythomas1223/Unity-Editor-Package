<p align="center">
  <img align="center" src="icon.png">
</p>

<p align="center">
  An in-game explorer and a suite of debugging tools for <a href="https://docs.unity3d.com/Manual/IL2CPP.html">IL2CPP</a> and <b>Mono</b> Unity games, using <a href="https://github.com/HerpDerpinstine/MelonLoader">MelonLoader</a> and <a href="https://github.com/BepInEx/BepInEx">BepInEx</a>.<br><br>

  <a href="../../releases/latest">
    <img src="https://img.shields.io/github/release/sinai-dev/Explorer.svg" />
  </a>
 
  <img src="https://img.shields.io/github/downloads/sinai-dev/Explorer/total.svg" />
</p>

<p align="center">
  <img src="https://raw.githubusercontent.com/sinai-dev/Explorer/master/overview.png">  
</p>

- [Releases](#releases)
- [How to install](#how-to-install)
- [How to use](#how-to-use)
  - [Mod Config](#mod-config)
- [Features](#features)
  - [Mouse Control](#mouse-control)
- [Building](#building)
- [Credits](#credits)

## Releases

| Mod Loader  | Il2Cpp | Mono |
| ----------- | ------ | ---- |
| [MelonLoader](https://github.com/HerpDerpinstine/MelonLoader) | ✔️ [link](https://github.com/sinai-dev/Explorer/releases/latest/download/Explorer.MelonLoader.Il2Cpp.zip) | ✔️ [link](https://github.com/sinai-dev/Explorer/releases/latest/download/Explorer.MelonLoader.Mono.zip) | 
| [BepInEx](https://github.com/BepInEx/BepInEx) | ❔ [link](https://github.com/sinai-dev/Explorer/releases/latest/download/Explorer.BepInEx.Il2Cpp.zip) | ✔️ [link](https://github.com/sinai-dev/Explorer/releases/latest/download/Explorer.BepInEx.Mono.zip) |

<b>Il2Cpp Issues:</b>
* Some methods may still fail with a `MissingMethodException`, please let me know if you experience this (with full MelonLoader log please).
* Reflection may fail with certain types, see [here](https://github.com/knah/Il2CppAssemblyUnhollower#known-issues) for more details.
* Scrolling with mouse wheel in the Explorer menu may not work on all games at the moment.

## How to install

### MelonLoader
Requires [MelonLoader](https://github.com/HerpDerpinstine/MelonLoader) to be installed for your game.

1. Download the relevant release from above.
2. Unzip the file into the `Mods` folder in your game's installation directory, created by MelonLoader.
3. Make sure it's not in a sub-folder, `Explorer.dll` should be directly in the `Mods\` folder.

### BepInEx
Requires [BepInEx](https://github.com/BepInEx/BepInEx) to be installed for your game.

1. Download the relevant release from above.
2. Unzip the file into the `BepInEx\plugins\` folder in your game's installation directory, created by BepInEx.
3. Make sure it's not in a sub-folder, `Explorer.dll` should be directly in the `plugins\` folder.

## How to use

* Press F7 to show or hide the menu.
* Use the Scene Explorer or the Object Search to start Exploring, or the C# Console to test some code.
* See below for more specific details.

### Mod Config

There is a simple Mod Config for the Explorer. You can access the settings via the "Options" page of the main menu.

`Main Menu Toggle` (KeyCode)
* Sets the keybinding for the Main Menu toggle (show/hide all Explorer windows)
* See [this article](https://docs.unity3d.com/ScriptReference/KeyCode.html) for a full list of all accepted KeyCodes.
* Default: `F7`

`Default Window Size` (Vector2)
* Sets the default width and height for all Explorer windows when created.
* `x` is width, `y` is height.
* Default: `<x>550</x> <y>700</y>`

`Default Items per Page` (Int)
* Sets the default items per page when viewing lists or search results.
* Default: `20`

## Features

### Scene Explorer

* A simple menu which allows you to traverse the Transform heirarchy of the scene.
* Click on a GameObject to set it as the current path, or <b>Inspect</b> it to send it to an Inspector Window.

### Inspectors

Explorer has two main inspector modes: <b>GameObject Inspector</b>, and <b>Reflection Inspector</b>.

<b>Tips:</b> 
* When in Tab View, GameObjects are denoted by a [G] prefix, and Reflection objects are denoted by a [R] prefix.
* Hold <b>Left Shift</b> when you click the Inspect button to force Reflection mode for GameObjects and Transforms.

### GameObject Inspector

* Allows you to see the children and components on a GameObject.
* Can use some basic GameObject Controls such as translating and rotating the object, destroy it, clone it, etc.

### Reflection Inspector

* The Reflection Inspector is used for all other supported objects.
* Allows you to inspect Properties, Fields and basic Methods, as well as set primitive values and evaluate primitive methods.
* Can search and filter members for the ones you are interested in.

### Object Search

* You can search for an `UnityEngine.Object` with the Object Search feature.
* Filter by name, type, etc.
* For GameObjects and Transforms you can filter which scene they are found in too.

### C# console

* A simple C# console, allows you to execute a method body on the fly.

### Inspect-under-mouse

* Press Shift+RMB (Right Mouse Button) while the Explorer menu is open to begin Inspect-Under-Mouse.
* Hover over your desired object, if you see the name appear then you can click on it to inspect it.
* Only objects with Colliders are supported.

### Mouse Control

Explorer can force the mouse to be visible and unlocked when the menu is open, if you have enabled "Force Unlock Mouse" (Left-Alt toggle). However, you may also want to prevent the mouse clicking-through onto the game behind Explorer, this is possible but it requires specific patches for that game.

* For VRChat, use [VRCExplorerMouseControl](https://github.com/sinai-dev/VRCExplorerMouseControl)
* For Hellpoint, use [HPExplorerMouseControl](https://github.com/sinai-dev/Hellpoint-Mods/tree/master/HPExplorerMouseControl/HPExplorerMouseControl)
* You can create your own plugin using one of the two plugins above as an example. Usually only a few simple Harmony patches are needed to fix the problem.

For example:
```csharp
using Explorer;
using Harmony; // or 'using HarmonyLib;' for BepInEx
// ...
[HarmonyPatch(typeof(MyGame.MenuClass), nameof(MyGame.MenuClass.CursorUpdate)]
public class MenuClass_CursorUpdate 
{
    [HarmonyPrefix]
    public static bool Prefix()
    {
        // prevent method running if menu open, let it run if not.
        return !ExplorerCore.ShowMenu;
    }
}
```

## Building

If you'd like to build this yourself, you will need to have installed BepInEx and/or MelonLoader for at least one Unity game. If you want to build all 4 versions, you will need at least one Il2Cpp and one Mono game, with BepInEx and MelonLoader installed for both.

1. Install MelonLoader or BepInEx for your game.
2. Open the `src\Explorer.csproj` file in a text editor.
3. Set the relevant `GameFolder` values for the versions you want to build, eg. set `MLCppGameFolder` if you want to build for a MelonLoader Il2Cpp game.
4. Open the `src\Explorer.sln` project.
5. Select `Solution 'Explorer' (1 of 1 project)` in the Solution Explorer panel, and set the <b>Active config</b> property to the version you want to build, then build it.
5. The DLLs are built to the `Release\` folder in the root of the repository.
6. If ILRepack fails or is missing, use the NuGet package manager to re-install `ILRepack.Lib.MSBuild.Task`, then re-build.

## Credits

Written by Sinai.

Thanks to:
* [ManlyMarco](https://github.com/ManlyMarco) for their [Runtime Unity Editor](https://github.com/ManlyMarco/RuntimeUnityEditor), which I used for the REPL Console and the "Find instances" snippet, and the UI style.
* [denikson](https://github.com/denikson) for [mcs-unity](https://github.com/denikson/mcs-unity). I commented out the `SkipVisibilityExt` constructor since it was causing an exception with the Hook it attempted.
