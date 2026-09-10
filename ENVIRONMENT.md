# Development Environment

Machine: macOS 26.5.2 (Darwin 25.5.0), Apple Silicon (arm64).
Set up for the VR Emergency Response Training Simulator (Milestone 0).

## Installed toolchain

| Tool | Version | Location | Purpose |
|---|---|---|---|
| Unity Hub | latest (arm64) | `/Applications/Unity Hub.app` | Editor + module management, license |
| Unity Editor | 6000.0.83f1 LTS (Apple silicon) | `/Applications/Unity/Hub/Editor/6000.0.83f1` | Scenes, rendering, physics, UI, audio, builds |
| Unity Android Build Support | bundled module (+ SDK, NDK, OpenJDK) | inside the editor install | Standalone Quest 2 / Quest 3 builds |
| Unity macOS IL2CPP | bundled module | inside the editor install | Desktop test builds |
| Blender | 4.5.13 LTS | `/Applications/Blender.app` | Low-poly environment + emergency-equipment assets |
| .NET SDK | 8.0.425 | `~/.dotnet` | C# tooling, Roslyn analyzers, VS Code C# language server |
| Git | 2.50.1 (Apple Git-155) | system | Version control |
| Git LFS | 3.7.0 | `~/.local/bin/git-lfs` | Binary asset storage (models, textures, audio) |
| Android platform-tools (adb) | 37.0.1 | `~/Android/platform-tools` | Sideloading and device logs on the headset |
| Xcode Command Line Tools | installed | `/Applications/Xcode.app` | Native toolchain dependency |
| JDK | Temurin 25 | `/Library/Java/JavaVirtualMachines/temurin-25.jdk` | System JDK (Unity uses its own bundled OpenJDK) |
| VS Code | installed | `/Applications/Visual Studio Code.app` | C# editing |

### VS Code extensions

- `visualstudiotoolsforunity.vstuc` — Unity integration (debugger, project awareness)
- `ms-dotnettools.csharp` — C# language support
- `ms-dotnettools.csdevkit` — C# Dev Kit
- `ms-dotnettools.vscode-dotnet-runtime` — runtime acquisition

## Shell configuration

`~/.zshrc` contains a `VR-SIM-ENV` block that exports:

```sh
export PATH="$HOME/.local/bin:$HOME/Android/platform-tools:$PATH"
export ANDROID_SDK_ROOT="$HOME/Android"
export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"
```

Open a new terminal (or `source ~/.zshrc`) to pick these up.

## Repository configuration

- `.gitignore` — Unity, Blender, macOS and IDE artefacts (`Library/`, `Temp/`, `Logs/`, `Build/`, `*.csproj`, `.DS_Store`, …).
- `.gitattributes` — LF normalisation for text assets, `merge=unityyamlmerge` for Unity YAML, and Git LFS tracking for `.blend`, `.fbx`, textures, audio and video.
- `.vscode/` — workspace settings and recommended extensions.
- Branching per the specification: `main` (stable, demo-ready), `develop` (integrated features), `feature/*`, `fix/*`.

### UnityYAMLMerge

Configured in the local git config so scene and prefab merges use Unity's
semantic merge tool rather than a line-based diff.

## Unity project configuration

The project itself is configured by [Assets/Editor/Setup/ProjectBootstrap.cs](Assets/Editor/Setup/ProjectBootstrap.cs),
which is idempotent and re-runnable from **Tools > VR Sim > Run Project Bootstrap** or in batch mode:

```sh
"/Applications/Unity/Hub/Editor/6000.0.83f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics -projectPath . \
  -executeMethod VRSim.EditorTools.ProjectBootstrap.RunAll -quit
```

It applies:

- The folder layout from section 6 of the specification, plus `Assets/Settings`, `Assets/Tests` and `Assets/XR`.
- Empty `MainMenu`, `TrainingRoom` and `FireEvacuation` scenes, registered in the build settings.
- A Quest-oriented URP asset (`Assets/Settings/QuestRenderPipeline.asset`): 4x MSAA, HDR off,
  25 m shadow distance, no depth or opaque texture. Assigned as the default pipeline and to every quality level.
- OpenXR as the XR loader for Android and Standalone, initialised on start.
- OpenXR features: Meta Quest support and the Oculus Touch and Meta Quest Touch Pro controller
  profiles on Android; Oculus Touch, Valve Index and Microsoft Motion controller profiles on desktop.
- Android player settings: IL2CPP, ARM64 only, min SDK 32, Vulkan then GLES3, ASTC textures,
  linear colour space, landscape-left, multithreaded rendering, application id `com.aryanlakhani.vrertsim`.
- The Input System package as the sole active input handler, as XR Interaction Toolkit 3.x requires.
- TextMeshPro essential resources.

### Installed Unity packages

| Package | Version |
|---|---|
| com.unity.xr.interaction.toolkit | 3.0.11 |
| com.unity.xr.openxr | 1.16.1 |
| com.unity.xr.management | 4.5.4 |
| com.unity.xr.core-utils | 2.6.0 |
| com.unity.inputsystem | 1.19.0 |
| com.unity.render-pipelines.universal | 17.0.4 |
| com.unity.ugui (TextMeshPro) | 2.0.0 |
| com.unity.test-framework | 1.6.0 |

### Tests

`VRSim.Tests.EditMode` and `VRSim.Tests.PlayMode` assembly definitions live under `Assets/Tests`.
Run the edit-mode suite headless:

```sh
"/Applications/Unity/Hub/Editor/6000.0.83f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics -projectPath . \
  -runTests -testPlatform EditMode -testResults /tmp/testresults.xml
```

### Builds

[Assets/Editor/Setup/BuildScript.cs](Assets/Editor/Setup/BuildScript.cs) produces `Build/Android/VRERTSim.apk`
via **Tools > VR Sim > Build Quest APK** or:

```sh
"/Applications/Unity/Hub/Editor/6000.0.83f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics -projectPath . -buildTarget Android \
  -executeMethod VRSim.EditorTools.BuildScript.BuildQuest -quit
```

## Verification performed

- Project opens and compiles with zero errors.
- Edit-mode test suite runs headless: 1 test, 1 passed.
- A Quest APK builds end to end: 36 MB, `lib/arm64-v8a` only, containing `libil2cpp.so`,
  `libopenxr_loader.so` and `libUnityOpenXR.so`.
- `adb` starts and lists devices (no headset attached yet).

## Manual step that still requires you

**Headset developer mode.** Enable developer mode for the Quest through the Meta Horizon mobile
app, connect over USB and accept the "Allow USB debugging" prompt. Verify with `adb devices`,
then install a build with `adb install -r Build/Android/VRERTSim.apk`.
