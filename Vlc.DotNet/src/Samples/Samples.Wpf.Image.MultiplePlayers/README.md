# WPF sample using Images (MultiplePlayers)

A sample WPF project demonstrating that we can use the Image class without using the WPF control (which is using Image anyway...)

Two videos are played.

In libvlc 3.0.20, there is a problem in multiplayer where the volume setting is applied to all players.
The workaround is to set "--aout=directsound".
Not sure what will happen in libvlc 4.
https://code.videolan.org/videolan/vlc/-/issues/28194