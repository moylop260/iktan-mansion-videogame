# ADR-0008: Psychological-horror audio

Status: accepted

## Decision
All audio is synthesized (`AudioGen`). A low looping drone is always present (seamless loop). While
exploring, a random scare plays every 16-38 s from a random direction (whisper, creak, drip, distant thud,
child giggle). Footsteps follow movement. Death scenes add roars (growl + lion-like pulses), screams
(formant filtered), stingers, heartbeat, skitter, static, siren. Ambient is muted during death scenes.

## How to verify
`AudioGenTests`: every clip is audible, finite, not clipping; the drone loops without a click.
