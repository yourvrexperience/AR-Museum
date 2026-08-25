# AR Museum

An augmented reality guide for museums and cultural institutions, built in Unity. Visitors follow a virtual tour guide through a scanned museum area, listen to narration in their own language, and ask questions out loud that are answered by an LLM. Museum staff can edit the narration and regenerate the audio themselves, without technical support.

This README is the setup guide: it takes you from an empty Unity project to a running experience, then to each optional subsystem (AI, networking, VR, AR space tracking).

---

## Table of contents

- [Who this is for](#who-this-is-for)
- [Requirements](#requirements)
- [What's in this repository](#whats-in-this-repository)
- [Setup](#setup)
  - [1. Create the Unity project](#1-create-the-unity-project)
  - [2. Set up the local web server and database](#2-set-up-the-local-web-server-and-database)
  - [3. Connect Unity to the backend](#3-connect-unity-to-the-backend)
  - [4. Build the asset bundle](#4-build-the-asset-bundle)
  - [5. Set up the AI backend](#5-set-up-the-ai-backend)
  - [6. Set up networking](#6-set-up-networking)
  - [7. Set up VR](#7-set-up-vr)
  - [8. Set up an AR space-tracking provider](#8-set-up-an-ar-space-tracking-provider)
- [Reference](#reference)
  - [Scripting define symbols](#scripting-define-symbols)
  - [Layers](#layers)
  - [Configuration files](#configuration-files)
- [Troubleshooting](#troubleshooting)
- [Project structure](#project-structure)
- [License](#license)

---

## Who this is for

This project is aimed at professionals. You need to be proficient in general programming and, above all, in Unity. The other languages involved (PHP, Python) are far less critical — with a solid programming background you should pick them up easily. Use an AI assistant to clear up questions, especially around the backend endpoints, where configuration and testing can be hard to debug.

Once you're fully familiar with the project, you should be able to deliver a solution to a museum in about four weeks — less, if the museum doesn't require an application redesign.

## Requirements

### Hardware

| Requirement | Why |
| --- | --- |
| A LiDAR-capable device | Required to scan the museum areas used by the space-tracking providers. |
| An Android device | Target platform for the visitor app. |
| A Meta Quest headset | Only if you intend to use the VR mode. |

### Software

| Requirement | Version / Source |
| --- | --- |
| Unity | `6000.3.18f1` |
| XAMPP | For the local web server and MySQL database |
| Python 3 | For the AI backend |

### Accounts and API keys

You are responsible for obtaining your own keys for every third-party service you enable:

- **Google Cloud** — speech recognition and text-to-speech ([API credentials](https://console.cloud.google.com/apis/credentials))
- **LLM providers** — any of OpenAI, Mistral, Gemini, DeepSeek, OpenRouter
- **Text-to-speech providers** — ElevenLabs, Speechify
- **AR provider** — a license key for MaxST, Vuforia or Niantic Spatial
- **Photon** — only if you use Photon instead of Mirror for networking

## What's in this repository

> [!IMPORTANT]
> This is **not** a ready-to-build Unity project. It contains only the files created specifically for this project, released under the MIT license. You create an empty Unity project yourself and copy the downloaded folder into its `Assets` folder.

**External libraries are not included** — importing them is up to you: the AR providers (Vuforia, MaxST, Niantic Spatial), Photon or Mirror for networking, the Meta XR SDK, and the Asset Store packages listed below.

**Download rather than clone.** Everything lives in a single repository instead of being split into multiple repositories with independent libraries. What you get is a snapshot of the development structure. Each major update is released as a new snapshot and is **incompatible with previous ones** — take a snapshot and make it your own.

---

## Setup

### 1. Create the Unity project

[Video Tutorial for sections 1 to 4](https://youtu.be/eIoQCUUrVTU)

1. Create a new project with Unity `6000.3.18f1`.
2. Copy the contents of this repository into the project's `Assets` folder.
3. Switch the build target to **Android**.
4. Install the following packages through the Package Manager:

   | Package | Identifier / Source |
   | --- | --- |
   | Newtonsoft Json | `com.unity.nuget.newtonsoft-json` |
   | SharpZipLib | `com.unity.sharp-zip-lib` |
   | iTween | [Asset Store](https://assetstore.unity.com/packages/tools/animation/itween-84) |

5. Install the speech packages. Both are needed so visitors can ask questions out loud and hear the reply spoken back:

   | Package | Source |
   | --- | --- |
   | Speech Recognition (Google Cloud) | [Asset Store](https://assetstore.unity.com/packages/tools/ai-ml-integration/speech-recognition-using-google-cloud-pro-2025-321307) |
   | Text-To-Speech (Google Cloud) | [Asset Store](https://assetstore.unity.com/packages/tools/ai-ml-integration/text-to-speech-using-google-cloud-pro-115170) |

6. Add NVorbis for OGG audio decoding:
   - Download `NVorbis.dll` from [the NVorbis repository](https://github.com/NVorbis/NVorbis) and place it in the `Assets` folder.
   - Add the `USE_NVORBIS` define (see [Scripting define symbols](#scripting-define-symbols)).

7. In **Project Settings > Player > Other Settings**:
   - Set **Allow downloads over HTTP** to `Always allowed`.
   - Set **Active Input Handling** to `Both`.

At this point the project should compile without errors.

### 2. Set up the local web server and database

1. Install XAMPP and start Apache and MySQL.
2. Copy the PHP backend scripts from `Assets/Application/AppMuseum/Scripts/Server/Editor/PHP` into `xampp/htdocs/template6dof`.
3. Open **phpMyAdmin** and create a database named `template6dof`.
4. Import the sample database, which already comes filled with sample data:

	- [Download MySQL history sample database](https://www.ar-museum.com/template6dof.zip)	

### 3. Connect Unity to the backend

The endpoint paths live in a scriptable singleton reachable from the `MainController` prefab in the scene, under the **NarrationData** property:

```csharp
GameLevelData.Instance.URLBase
```

Set **Url Base** and **Url Base Management** to your XAMPP folder:

```
http://localhost:8080/template6dof/
```

### 4. Build the asset bundle

> [!WARNING]
> Create the layers **before** building the bundle. Building with the layers missing produces a bundle that misbehaves at runtime.

1. Create the [layers](#layers) listed in the reference section.
2. Create a folder named `AssetsBundles` in the Unity project root.
3. Go to **Asset Bundles > Build Asset Bundle Android** and wait for the process to finish. The build tool lives at `Assets/Application/Editor/AssetBundle`.
4. The `template6dof` bundle appears in the `AssetsBundles` folder.
5. On the XAMPP server, create the following folders and copy the bundle into **both**:

   ```
   /Android/dev/
   /Android/prod/
   ```

You should now be able to run the experience in production mode.

### 5. Set up the AI backend

[Video Tutorial for AI set up](https://youtu.be/eIoQCUUrVTU)

The AI layer is fully removable — some clients don't want AI in any capacity, and the `ENABLE_AI_OPERATIONS` define strips every AI operation from the build.

1. Add the `ENABLE_AI_OPERATIONS` define.
2. Copy the Python scripts to the machine that will host the AI endpoints. They only act as a bridge to the AI providers, so they aren't demanding — a local machine or a small board such as a Jetson Orin on your LAN is plenty.
3. Run the server once to discover the missing Python libraries, installing them until it starts cleanly:

   ```bash
   python ServerAIEnglish.py
   ```

4. Set your provider API keys as environment variables in the `.env` file.
5. Copy the server's local address into the **AI Data** property of the `MainController` prefab, under **Server Chat GPT**:

   ```
   http://<host>:5001/ai/
   ```

6. Login with the test demo credentials:

   ```
    • user: esteban@yourvrexperience.com 
    • password: 12345
   ```

7. In the case it didn’t work, check these prefabs **CommsUsersConstants,CommsAnalysisConstants**  in the scene and update their URL field to:

	```	
	http://localhost:8080/template6dof/
	```
	
8. Add the `ENABLE_SPEECH` and `ENABLE_GOOGLE_SPEECH` defines to enable text-to-speech.

#### Choosing providers

Run the project in the editor, open the settings and log in with an account from the imported sample data. Once logged in, the AI section lets you choose the LLM provider and the text-to-speech provider.

For development, pick the cheapest models and avoid OpenRouter, which adds a cost you don't want while testing. A good development pairing is **GPT-5.4-Nano** for the LLM and **Speechify** for text-to-speech — roughly a hundred times cheaper than ElevenLabs. Press **Set Provider** and check the Python server log to confirm the endpoint was triggered.

#### Testing the AI

1. Play any floor, then select the button in the top-left corner of the museum area.
2. Speech recognition only works on mobile devices, so type the question in the editor instead.
3. The LLM responds first, then text-to-speech generation completes and plays the reply. Follow-up questions work, since the conversation is remembered.

#### Testing tour edition mode

1. Switch the development-mode toggle, stop the editor and play again — the content edition button now appears.
2. Select the floor you want to edit, then select any POI and edit its narration in the field matching the current application language.
3. Press the **Play** audio button to generate the audio for the new text.
4. Translate the text into the other languages, generate their audio tracks, and save your progress.

Speech recognition itself must be verified in a mobile build.

### 6. Set up networking

[Video Tutorial for Networking set up](https://youtu.be/eIoQCUUrVTU)

The multiplayer session exists so a human guide can drive the experience, extending their explanations with virtual objects. The first client to run becomes the server on the local Wi-Fi network and controls the clients that connect afterwards.

Two backends are supported — pick one.

<details>
<summary><b>Mirror</b></summary>

1. Import [Mirror](https://assetstore.unity.com/packages/tools/network/mirror-129321).
2. Add the `ENABLE_NETWORKING` and `ENABLE_MIRROR` defines.
3. Activate the networking toggle and press play. Once the museum area loads, the top-left corner should read `[MIRROR]::UID[1]::Server[True]`.
4. To test multiplayer properly, copy the entire Unity project into another folder, open that copy, and run it after the first one.

</details>

<details>
<summary><b>Photon</b></summary>

1. Import the [Photon SDK](https://www.photonengine.com/sdks#pun-unity).
2. Add the `ENABLE_NETWORKING` and `ENABLE_PHOTON` defines.
3. Enter your API key in both clients to establish the connection.

</details>

### 7. Set up VR

[Video Tutorial for VR set up](https://youtu.be/eIoQCUUrVTU)

1. Import the [Meta XR SDK](https://assetstore.unity.com/packages/sdk/meta-xr-sdk-9022845).
2. From **Package Manager > Unity Registry**, install **XR Interaction Toolkit**.
3. In **XR Plug-in Management > PC**, enable **Initialize XR on Startup** and select **OpenXR > Meta XR** feature group.
4. Add the `ENABLE_OCULUS` define.
5. Connect the headset with the Meta Link cable and test.

### 8. Set up an AR space-tracking provider

Three providers are supported. The workflow is the same for all of them — only the SDK, the scanner app and the define change.

| Provider | SDK | Scanner app | Define | How-To |
| --- | --- | --- | --- | --- |
| MaxST | [Download](https://developer.maxst.com/MD/downloadsdk) | [BITMAX AR Scanner](https://developer.maxst.com/MD/downloadtools) | `ENABLE_MAXST` | [Video How-To](https://youtu.be/eIoQCUUrVTU) |
| Vuforia | [Download](https://developer.vuforia.com/downloads/sdk) | [Vuforia Creator](https://developer.vuforia.com/downloads/tools) | `ENABLE_VUFORIA`  | [Video How-To](https://youtu.be/eIoQCUUrVTU) |
| Niantic Spatial | [Download](https://www.nianticspatial.com/docs/nsdk/downloads) | [Scaniverse – 3D Scanner](https://apps.apple.com/us/app/scaniverse-3d-scanner/id1541433223) | `ENABLE_NIANTIC`  | [Video How-To](https://youtu.be/eIoQCUUrVTU) |

Then, for any provider:

1. Import the SDK and add its define.
2. Install the scanner app on a LiDAR-capable device and scan the area.
3. Import the scan into Unity. For MaxST, download the scan from the developer site and copy it into the `StreamingAssets` folder.
4. Replace an existing level in the asset bundle with the scan, and build the collision walls you want the pathfinding to account for.
5. Rebuild the asset bundle and update it on your web server.
6. Create the content for the new level, set up your license API key, make a build and test on location.

---

## Reference

### Scripting define symbols

Every optional subsystem is gated behind a define, so any of them can be stripped from a build a client doesn't want. Set these in **Project Settings > Player > Other Settings > Scripting Define Symbols**.

| Define | Enables |
| --- | --- |
| `USE_NVORBIS` | OGG audio decoding through NVorbis |
| `ENABLE_AI_OPERATIONS` | All AI operations — without it, AI is fully removed |
| `ENABLE_SPEECH` | Text-to-speech functionality |
| `ENABLE_GOOGLE_SPEECH` | Google's speech services |
| `ENABLE_NETWORKING` | The networking layer |
| `ENABLE_MIRROR` | Mirror as the networking backend |
| `ENABLE_PHOTON` | Photon as the networking backend |
| `ENABLE_OCULUS` | VR support on Meta Quest |
| `ENABLE_MAXST` | MaxST as the space-tracking provider |
| `ENABLE_VUFORIA` | Vuforia as the space-tracking provider |
| `ENABLE_NIANTIC` | Niantic Spatial as the space-tracking provider |

### Layers

These user layers must exist before the asset bundle is built:

| Layer | Name |
| --- | --- |
| 6 | `Player` |
| 7 | `Navigation` |
| 8 | `EasterEgg` |
| 9 | `Video` |
| 10 | `Discover` |
| 11 | `Replay` |
| 12 | `Floor` |

### Configuration files

**`Assets/Application/AppResources/Resources/Data/GameTexts.xml`** holds all the application text for every language, plus AI configuration at the top:

| Tag | Purpose |
| --- | --- |
| `<narration>` | The Speechify voice IDs used for narration. Pick them from Speechify's *Voices* section. |
| `<speech>` | The Google Text-to-Speech configuration, used to answer visitor questions. Cheaper than Speechify. |
| `<ai_instructions>` | The information the museum wants the LLM to use when replying to visitors. |

**`.env`** (AI backend) holds the API keys for your LLM and text-to-speech providers.

---

## Troubleshooting

<details>
<summary><b>The experience doesn't load / the backend isn't reached</b></summary>

The paths probably don't match. Open the `MainController` prefab, check the **NarrationData** property, and make sure **Url Base** and **Url Base Management** point to the XAMPP folder where you placed the PHP scripts (`http://localhost:8080/template6dof/`).

Also check the `CommsUsersConstants` and `CommsAnalysisConstants` prefabs in the scene and update their URL to the same value.

</details>

<details>
<summary><b>Database login failure</b></summary>

Open `xampp/htdocs/template6dof/ConfigurationUserManagement.php` and check the database user and password. The default is usually `root` with no password, but your XAMPP setup may differ.

</details>

<details>
<summary><b>The Python AI server won't start</b></summary>

It will report missing libraries one at a time. Install each one until the script runs cleanly, then confirm the `.env` file contains valid API keys.

</details>

---

## Project structure

[Video Tutorial for Project structure](https://youtu.be/eIoQCUUrVTU)

The code lives under `Assets/Application`. Each folder has its own README:

| Folder | Contents |
| --- | --- |
| [`AppMuseum`](AppMuseum) | Scripts specific to this project, organised as MVC. |
| [`AppResources`](AppResources) | The project's resources. |
| [`Editor/AssetBundle`](Editor/AssetBundle) | The graphic resources packed into the asset bundle. |
| [`Libraries`](Libraries) | Self-contained libraries handling AI, analytics, narration, networking, social, speech, users, voice, VR and utilities. |

The backend is split in three:

- **PHP** — database endpoints for museum content, user management and analytics.
- **Python** — the AI endpoints and Google authentication.
- **Administration** — the pages used to analyse the collected data.

## License

Released under the MIT License. See [`LICENSE`](LICENSE) for details.
