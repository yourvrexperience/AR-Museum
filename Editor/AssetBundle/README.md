# Asset Bundle

A museum can have a large collection of graphic assets to display during the experience, so including them in the build isn't an option — it would inflate both the build time and the build size. We package them into an **asset bundle** instead, which is loaded at runtime.

## Contents

`Assets/Application/Editor/AssetBundle`

| Folder | Description |
| --- | --- |
| `Guide` | The assets for the tour guide, for each of the three current visitor profiles. |
| `Levels` | The assets for the different areas we've scanned and built virtual tours for. |
| `POIs` | The collection of museum graphic assets we want to display in the virtual tour. |
