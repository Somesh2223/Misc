# Embedded, patched copy

Copied from Unity-Technologies/multiplayer-community-contributions
(Transports/com.community.netcode.transport.facepunch, commit 0fab638470379ace12b0149dfb41c043d23dbce5, MIT license).

Changes for Netcode for GameObjects 2.x:
- `OnEarlyUpdate()` override replaced with `Update()` (the base method was removed in NGO 2.x), which only pumps Steam callbacks once Steam is running.
