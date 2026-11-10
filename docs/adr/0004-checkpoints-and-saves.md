# ADR-0004: Checkpoints, saves and retry

Status: accepted

## Decision
Blue lamps are checkpoints (green when active). Touching one saves `SaveData` (items, opened doors,
position, yaw, death count) as JSON in PlayerPrefs. After a death the player retries from the last
checkpoint (the world is rebuilt and doors re-applied) or restarts from scratch. The title screen offers
Continue when a save exists. The death count is persisted at the moment of death.

## How to verify
`DataTests` (JSON round trip) and the PlayMode retry/continue tests.
