# AppMuseum

🎬 [Video Tutorial for Project structure](https://youtu.be/FwBn5uD0lKI)

The main application. It contains the project's specific scripts and assets, and its architecture is based on the **MVC (Model–View–Controller)** design pattern.

## Folder structure

```
AppMuseum/
└── Scripts/
    ├── Controller/          # Application logic
    │   ├── States/          # State-pattern scripts
    │   └── POIs/            # Points Of Interest interactions
    ├── View/                # Presentation layer
    │   ├── GameElements/    # 3D elements
    │   └── Screens/
    │       ├── InGame/      # Screens shown once the museum level is loaded
    │       └── Menus/       # Start-up, area selection, language, profile, settings
    ├── Scenes/              # Unity scenes
    └── Server/
        └── Editor/
            ├── Database/        # Sample database
            ├── PHP/             # Content, user management and analytics endpoints
            ├── Python/          # AI operations and Google authentication endpoints
            └── Administration/  # Admin frontend
```

## Controller

`AppMuseum/Scripts/Controller`

The controllers that handle the different functionalities of the project. The most critical one is **`MainController`**, responsible for the global organization.

| Folder | Description |
| --- | --- |
| `Controller/States` | State-pattern scripts that handle the project's different states. |
| `Controller/POIs` | Scripts that handle the interactions performed on the POIs (Points Of Interest). |

## View

`AppMuseum/Scripts/View`

The elements used to display information.

### View/GameElements

Scripts that display the application's 3D elements. The most critical ones are:

| Script | Description |
| --- | --- |
| `PlayerView` | Handles the player representation. |
| `LevelView` | Handles level management. |
| `TourGuideView` | Handles the tour guide. |

### View/Screens

Scripts for the screens used during the application.

| Folder | Description |
| --- | --- |
| `Screens/InGame` | The screens used once the museum level has loaded. |
| `Screens/Menus` | The screens used at the start of the application, to select the museum area to play, the language, the profile, and other settings. |

## Scenes

`AppMuseum/Scripts/Scenes`

The scenes used in the project. These are the elements used in the **main scene**:

| Element | Description |
| --- | --- |
| `MainController` | The main controller, which globally manages the whole application. |
| `ARMaxSTController` | The controller enabled when you use **MaxST** as the space-tracking provider. |
| `ARVuforiaController` | The controller enabled when you use **Vuforia** as the space-tracking provider. |
| `NianticController` | The controller enabled when you use **Niantic Spatial (VPS)** as the space-tracking provider. It tracks the VPS anchor and loads the corresponding level on top of it. |
| `AssetsBundleController` | Handles the asset bundles. |
| `CommController` | Handles HTTP communication. |
| `ScreenController` | Manages the screens. |
| `CameraController` | Handles creating the right camera. Since the project supports VR interaction, different cameras are instantiated depending on the target platform. |
| `SoundsController` | Handles the sound system. |
| `NetworkController` | Handles real-time network communication. |
| `SpeechRecognitionController`, `GCSpeechRecognition`, `GCTextToSpeech` | Manage speech recognition and generation through the Google API. |
| `SystemEventController`, `UIEventController` | Custom internal event controllers that dispatch events through the system. |
| `UsersController` | Manages users against a database in the backend. |
| `SpeechDatabaseController` | Handles AI speech generation. |
| `GoogleAuth` | Handles Google authentication. |

## Server

`AppMuseum/Scripts/Server`

Scripts that implement the backend endpoints for managing the database and the AI services.

| Folder | Description |
| --- | --- |
| `Server/Editor/Database` | Contains the empty database with the structure. For the **full database sample** [download from this link](https://www.ar-museum.com/template6dof.zip)	 |
| `Server/Editor/PHP` | Backend scripts, run against the database, that handle museum content creation, user management and analytics. The most critical one is **`ConfigurationUserManagement.php`**, which establishes the database connection and holds a collection of functions used by the other scripts. |
| `Server/Editor/Python` | Backend scripts that handle the AI operations and the endpoints for Google authentication. The most critical script is **`AILLMEndpoints.py`**, where the endpoints are implemented. |
| `Server/Editor/Administration` | Frontend scripts that display the pages for handling analytics and user management. |
