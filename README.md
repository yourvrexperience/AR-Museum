# Application

Everything specific to this project lives under `Assets/Application`. Keeping it in a single root separates our own code and content from the third-party SDKs and imported packages that sit elsewhere in `Assets`, which makes updating or replacing an external dependency a self-contained operation.

## Folder structure

```
Assets/Application/
├── AppMuseum/          # Project-specific scripts and assets (MVC)
├── AppResources/       # Project resources
├── Editor/
│   └── AssetBundle/    # Graphic resources packed into the asset bundle
└── Libraries/          # Reusable, self-contained functionality
```

| Folder | Description |
| --- | --- |
| [`AppMuseum`](AppMuseum) | Scripts specific to this project. |
| `AppResources` | The project's resources. |
| [`Editor/AssetBundle`](Editor/AssetBundle) | The graphic resources we pack into the asset bundle. |
| [`Libraries`](Libraries) | A collection of libraries that handle different functionalities. |

## How the folders relate to each other

The four folders are not siblings in the architectural sense — they sit at different levels, and knowing which depends on which is the fastest way to find your bearings in the codebase.

### AppMuseum — the application layer

This is the only folder that knows it belongs to a museum application. It holds the concrete behaviour of *this* product: the flow between screens, the states the visit goes through, what happens when a visitor interacts with a point of interest, and how the tour guide behaves. It follows an **MVC** structure, with `MainController` acting as the entry point that wires the rest of the system together.

If you're reading the code for the first time, start at `MainController` in the main scene and follow the controllers it initialises.

See [`AppMuseum`](AppMuseum) for the detailed breakdown.

### Libraries — the reusable layer

Each library solves one problem and knows nothing about museums: user management, AI backend calls, analytics, narration, networking, social providers, speech, voice streaming, VR, and general-purpose utilities.

The dependency direction is deliberately one-way — `AppMuseum` uses the libraries, and no library references `AppMuseum` in return. This means any library can be extracted and dropped into a different project without dragging the application along with it, and it also means a change inside `AppMuseum` can never break a library.

See [`Libraries`](Libraries) for the full list.

### Editor/AssetBundle — the content layer

A museum can hold a large collection of graphic assets, so shipping them inside the build isn't viable: it inflates both build time and build size, and it would force a new app release every time the museum changes its content. Instead these assets are packaged into an **asset bundle** that is downloaded and loaded at runtime.

The folder sits under `Editor` because bundling is a build-time operation performed from the Unity Editor — the packing code never ships with the player.

See [`Editor/AssetBundle`](Editor/AssetBundle) for the contents.

### AppResources — shared project resources

Resources used across the application that are part of the build itself, as opposed to the content delivered through the asset bundle.

## Adding new code

As a rule of thumb:

- If the code mentions museums, visitors, tours or points of interest, it belongs in **`AppMuseum`**.
- If it would still make sense in a completely different application, it belongs in **`Libraries`** — and it must not reference anything in `AppMuseum`.
- If it's museum content rather than code, it belongs in the **asset bundle**, not in the build.
