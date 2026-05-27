> [!WARNING]
> You are currently on the rewrite branch. This version of Pawn Editor is not stable and intended to be used in a playthrough. For the current stable version available on Steam, [check out the main branch.](https://github.com/ISOR3X/pawn-editor/tree/main)

![Preview](data/PawnEditor/About/Preview.png)
# Pawn Editor
A rewrite of Pawn Editor. The original code was difficult too maintain and suffered from some core issues.

## Repository structure
This repository uses an different structure compared to other mods:
```
├───data/
│   └───PawnEditor/ # XML files and other static data
│       │   LoadFolders.xml
│       ├───About
│       ├───Data 
│       ├───Languages
│       └───Textures
└───src
    ├───PawnEditor # Source code for the mod
    ├───Taffy
    └───Void
```
Defs are stored under `/Data` (same as the game itself uses). It uses nested naming for loadfolders to keep the `LoadFolder.xml` itself readable.

## Status
1. `SectionDefs` and their `SectionWorker` are small pieces of code that define a group of items that can be modified through the editor. For example [`SectionWorker_Backstory`](src/PawnEditor/Sections/Bio/SectionWorker_Backstory.cs) only contains code about how to change the pawns backstory.
2. `TabDef` contains `SectionDefs` and how they are laid out. As this is done through XML, other mods can easily patch in more sections. `mayRequire` is also supported.
3. UI and component library. Wrap RimWorld widgets with some QoL improvements (number inputs with increment buttons and scroll incrementing, text inputs only setting on blur). It also contains RimWorld bindings for `Taffy`, including Flexbox and CSS Grid.
4. [Taffy](https://github.com/DioxusLabs/taffy) C# port. Taffy is a UI layout library that uses the HTML/ CSS vocabulary. 

## Contributing
Check out [CONTRIBUTING.md](CONTRIBUTING.md)