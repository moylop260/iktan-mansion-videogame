# ADR-0009: Automated tests and coverage

Status: accepted

## Decision
Unity Test Framework: EditMode tests for pure logic (audio, textures, data), PlayMode tests for the world
(structure, route, deaths, retry, creatures, audio manager). Code Coverage 1.3.0 reports on `Mansion.Runtime`.
`GameManager` exposes small seams for tests (`Hint`, `FlashOn`, `PickOption`, `Deaths`, `CurrentCheckpoint`).
PlayMode tests build their own `GameManager` (it is not DontDestroyOnLoad) and speed up time for death scenes.

## How to run
`./tools/run-tests.sh`; reports in `CodeCoverage/`.
