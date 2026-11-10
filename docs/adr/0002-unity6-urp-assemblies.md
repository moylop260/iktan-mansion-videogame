# ADR-0002: Unity 6, URP and separate assemblies

Status: accepted

## Context
On Unity 6.6 the built-in default materials render pink without a render pipeline asset. Tests cannot
reference the default `Assembly-CSharp`.

## Decision
Use the Universal Render Pipeline (Forward+, soft shadows, post-processing). `Assets/Editor/MansionEditor.cs`
creates the URP asset and the base materials (`Resources/BaseLit`, `BaseLitEmit`, so the emission shader
variant is included in builds). Game code is `Mansion.Runtime`; editor code is `Mansion.Editor`.
Packages must match the editor version (Input System 1.11.x and Code Coverage 1.2.x do not compile on 6.6).

## How to verify
`-executeMethod MansionEditor.BuildMac` succeeds and the running app is not pink.
