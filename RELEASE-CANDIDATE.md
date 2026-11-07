# Unified update candidate
Replaces the earlier technical UI, adds SDL3 navigation, minimal own details screen, embedded WebView2 Steam store and fallback/cached artwork. This repository version consolidates the source archive into Program.cs and Gamepad.cs. Compilation and runtime tests are pending.

Extract the entire artifact: SDL3.dll, WebView2 assemblies and all other files must stay together. This is a multi-file self-contained deployment, not a standalone EXE. WebView2 Runtime must already be installed; missing runtime shows an error and does not launch an external browser.

Gamepad: SDL recognized devices, first connected controller only. D-pad/stick navigation, south confirm, east back, west launch, north focus search, shoulders sections. A/B button toggles confirmation/cancel and is not persisted. No full remapping UI, on-screen keyboard or guarantee for every controller. Controller processing disabled while inactive/minimized. Launch minimizes the shell; no automatic detection of game exit. Web panel supports scroll/back, not gamepad-only login or checkout.

Import uses local SteamID64/API-key entry, never writes the key to disk. Refresh rereads installations and cached account games; remote sync requires repeated import. Cache is in LocalAppData/SteamConsoleLauncher. Family sharing and all Steam categories are not guaranteed complete.

Artwork tries cover jpg, 2x jpg, header and hero, validates image decoding, caches files and limits requests to four concurrent loads. Fallback still may fail; card title remains. No cache size limit or virtualized grid yet: large libraries require performance testing.

Steam store navigates internally. Non-Steam top-level redirects/popups are blocked; third-party payment flows unsupported. Descriptions/reviews/videos remain on the embedded official page; own details are minimal. No payment implementation. Local web cookies are stored under the application's WebView2 profile.

Acceptance tests pending: Windows build/start, Well Dweller artwork, account import, 8BitDo receiver and other recognized gamepads, hotplug, focus/background safety, large-library RAM/scrolling, embedded navigation and back. Old PROTOTYPE.md and UI-V2.md describe previous versions only.
