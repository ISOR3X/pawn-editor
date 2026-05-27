# Contributing

Below are some notes for contributing to the project
- Please use [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/)
- The project uses C# 14 language version features
- On build, the project should automatically place all necesarry files in your RimWorld mod folder (only on Windows). If this is not the case, set the `RIMWORLD_MODS_PATH` environment variable to specify the exact location.
- `data/` contains your regular XML and other static files, while `src/` contains the source code for PawnEditor, Taffy and Void.
- `src/PawnEditor/` contains code specific to the mod, while `src/Void/` contains common UI logic code. `src/Taffy/` contains the Taffy C# port of its Rust-based original and should generally not be touched.
