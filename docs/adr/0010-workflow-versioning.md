# ADR-0010: Workflow and versioning

Status: accepted

## Decision
Public repository `moylop260/iktan-mansion-videogame`. **Every change is its own commit, pushed.** Commit
messages are English with a short imperative subject and a body that explains why. No AI co-author trailers
and no session links. Versions use **bump2version** (`ProjectSettings/ProjectSettings.asset` `bundleVersion`
and `README.md`), which commits and tags `v<version>`. Independent reviews are welcome; their findings are
fixed in separate commits.
