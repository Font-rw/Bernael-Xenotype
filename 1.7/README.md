# RimWorld 1.7

Bernael Xenotype supports RimWorld 1.7. Version-specific definitions, patches,
assemblies and shaders live in `1.7/`. Textures, sounds and translations shared
across versions live in `Common/`.

## Building

To build the three core assemblies, run these commands from the repository root
with the .NET 10 SDK.

```powershell
dotnet build 1.7/Source/BernaelXenotype17/BernaelXenotype17.csproj -c Release
dotnet build 1.7/Source/BernaelSoul/BernaelSoul.csproj -c Release
dotnet build 1.7/Source/BernaelVanquish/BernaelVanquish.csproj -c Release
```

The projects use game references and Harmony from NuGet and write their DLLs to
`1.7/Assemblies/`. To also build the optional Vanilla Psycasts Expanded integration
on Windows, install Vanilla Expanded Framework in Steam Workshop and run
`dotnet build 1.7/Source/BernaelXenotype.slnx -c Release`.

## Rendering

The effects use native ShaderDefs and GLSL sources in `1.7/Shaders/Bernael/`.
Changes belong in those GLSL files and `1.7/Defs/ShaderDefs/BX_Shaders.xml`.
Imported Piloto Studio texture sources and transformations are listed in
`Common/Textures/Effects/Piloto/SOURCES.json`.

Spatial noise uses the integer lattice hash in `BX_ShaderCommon.glsl`.
Texture samples that need automatic mip selection must execute outside
divergent branches.

Each Shadow mire vent feeds tongues of cursed fire into one rising current.
Their timing, strength and drift vary with the field, wave and vent. Tongues
can merge near the oil, part higher up or shed a glowing tip as the fuel runs
out. The fire and its reflection sample the same cross-sections. Their shape
at any game tick can be reconstructed from an existing save.

## Gameplay tests

The `Tests/` tree contains gameplay and visual scenarios in `.rwtest` format.
They are maintained alongside the corresponding abilities and effects.
The release archive contains only game files; source and test files stay in
the repository.

## Design

[Bernael xenotype](https://docs.google.com/document/d/1BgKwyOawYDHXorZIPq1jJHHtnLrTC3mw_e1Bu71AsiE/edit?tab=t.0)
contains the shared design. The Shadow mire and cursed flame descriptions were
checked on 2026-11-20 (Asia/Shanghai) against the revision last modified at
2026-11-16T19:55:15.133Z.
