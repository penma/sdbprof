# sdbprof
Profile running Mono apps (e.g. Unity game) via soft debugger

This project aims to provide a profiler for Mono code that is running embedded in some other application, and therefore cannot be profiled using normal methods. A typical use case is a Release build of a Unity game that you want to profile because you are writing a mod for it.

In this case, running the game with a standalone version of Mono (or even a completely different runtime) is a hard task, not only because the game might not support the different runtime, but you might also have to work with some kind of platform launcher (e.g. Steam).

## How to setup a Unity game for profiling
The game still needs to be modified, of course. The embedded Mono runtime has to be replaced with a version that supports remote debugging. Thankfully 0xd4d has already made the work and provides recompiled versions of mono.dll for Unity players of most/all versions. https://github.com/0xd4d/dnSpy/wiki/Debugging-Unity-Games - head over there and grab an updated version (or get some other way to get the runtime to open a debug socket)

Replace the game's mono.dll (in `GAMENAME_Data/Mono` - there may be other copies around, but replacing this one should suffice) with a debug version. It is a good idea to keep the old version around for regular gaming, because the debug version runs a bit slower even without a debugger attached. Even though you might be able to recover the original easily (e.g. Steam "Check file integrity") this still takes longer than just renaming the backup version.

When launched, the game should now still run mostly normally and additionally expose a Mono soft debugger interface on localhost:55555 that sdbprof can attach to.

## Using sdbprof
To be added.

## License

MIT license
