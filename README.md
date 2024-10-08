# COM3D2.ShortStartLoader
Yet another COM3D2 plugin that fixes more of Kiss's Horrendous code. But also some .NET Framework stuff. This mod has been in private-beta hell for about two years now, I'd come back to it from time to time to make minor enhancements and stuff.

## Why ##
As we all know, KISS can't code. This leads to the many issues with the game once you reach certain points. What SSL targets is the incredibly large load delay when you start your game. This one is caused by various bad design choices and unoptimized function usage. SSL fixes this as best as it can.

## What ##
To get technical, we optimize various calls by sorta juggling the slower parts of the code. You can credit @hatena_37 for portions of the code found in ModMenuAccel that indeed confer a speed boost. I ported that code to BepInEx and implemented it in a cleaner fashion into SSL. Apart from that, the biggest optimization is implementing parallelism for mod gathering. This can slough off various seconds or even minutes as the game identifies mod files while it loads itself.

Please keep in mind this won't fix the massive delay you'll experience when starting the game, the crappy code is still unmanaged, this only helps somewhat especially when you have very large mod folders.

## Installation ##
Make sure you're on at least assembly version 2.38 of the game before installing!

1. Download file from here: https://github.com/krypto5863/COM3D2.ShortStartLoader/releases under assets.
2. Place into Bepinex/plugins
3. REMOVE `COM3D2.ModMenuAccel.Hook.dll` and `COM3D2.ModMenuAccel.Patcher.dll`, they are no longer needed!
4. Profit. You may configure it to disable features in the F1 menu or by editing the config file in the config directory.
