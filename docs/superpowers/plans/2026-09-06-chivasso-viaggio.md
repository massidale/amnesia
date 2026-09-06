# Chivasso And Return Travel

## Approved Scope

Preserve the canonical two-hour coach journey and the 14:00 departure. Build a compact low-poly Chivasso district, Wanda's ground-floor home at Via Sant'Orsola 14, a station and a stationary regional train. San Rocco's railway remains a disused siding. Both scenes remain free-exploration scenes, not a replacement for the narrative session.

## Implementation

1. Add `tools/check-chivasso.mjs`: assert both scenes, reciprocal destinations, coach and train prefabs, house identity and two resident IDs. Run it before implementation and confirm the missing scene fails.
2. Add `Viaggio1987` in the existing runtime asset folder. A deliberate interaction near the coach boards the passenger, animates departure, fades, and loads the destination asynchronously. Spawn beside the destination coach, never automatically reboard. Handle missing scenes before disabling movement.
3. Add `SanRoccoTransport` editor geometry: coach prefab with seats and wheels, regional train prefab with bogies, station platforms and disused track buffers/ballast. Preserve building access routes.
4. Replace the minimal `Build.Chivasso()` stub with `ChivassoScene.Create()`. Build station, district streets, furnished home, cartoleria, cafe and residential buildings as separate prefabs. Place Wanda and Elena using their existing IDs; no family-name signs. Generate both scenes from the existing map command, without enabling Bootstrap.
5. Add `SanRoccoTour` editor inspection: renderer/material/ground checks, camera routes covering every village building and Chivasso, frame sequences and per-scene reports. Encode the sequences to MP4 with local ffmpeg and inspect representative frames, including thresholds and interiors. Resolve identified geometry defects.
6. Enable Standard material GPU instancing and measure the effect on the editor graphics-buffer warning. Preserve independent prefab transforms and colliders.
7. Verify all Node checks, Unity geometry checks, both Play-mode journeys, and scene/character counts after the return. Request a focused code review. Document remaining free-visit limitations honestly.

## Verification Contracts

- `Viaggio1987.DestinazioneScena`: `Chivasso1987` or `SanRocco1987`.
- `Viaggio1987.PuntoSalita` and `PuntoArrivo`: local child transforms with clear ground and no vehicle intersection.
- `ChivassoScene.ScenePath`: `Assets/Scenes/Chivasso1987.unity`.
- Independent vehicle prefabs: `Props/corriera_1987.prefab`, `Props/treno_regionale_1987.prefab`.
- `ChivassoScene.Check()` must find exactly Wanda and Elena, a furnished `casa_wanda`, one visitor camera and one return coach.
- Narrative JSON and prompt travel references remain unchanged.
- No commits or unrelated scene changes are required for this task.
