# Tests

This folder contains standalone test and repro projects for the layout engine.

`Taffy.Tests` is a `net472` xUnit project that references `src/Taffy/Taffy.csproj` directly. It includes:

- The recovered flexbox and grid test suites that previously lived under `src/Taffy/tests/TaffyTests`.
- A nested flex regression case to help investigate the current layout issue.

If your shell environment can resolve `Microsoft.NET.Sdk`, you can build it with:

```powershell
& 'C:\Users\Joram Hoogerwerf\AppData\Local\Programs\Rider\tools\MSBuild\Current\Bin\MSBuild.exe' tests\Taffy.Tests\Taffy.Tests.csproj
```

On the current terminal session, invoking `MSBuild.exe` works, but the build still fails before compilation if no .NET SDK is discoverable outside Rider.
