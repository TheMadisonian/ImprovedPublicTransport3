# BetterBusStopPosition — CalculateSegmentPosition Rewrite

## Root Cause: SmootherStep Mismatch

Vanilla `NetLane.CalculateStopPositionAndDirection` uses a `SmootherStep(0.5, 0, |laneOffset - 0.5|)` curve to apply the lateral curb offset (`stopOffset`). This curve peaks at exactly 1.0 when `laneOffset = 0.5` (the vanilla lane-centre stop) and tapers to 0 at both ends.

BBSP shifts the stop forward — `targetOffset` now ranges from ~0.6 to ~0.8 depending on vehicle length. At those positions, `SmootherStep` yields only ~0.5–0.7, so the vanilla call applies only a partial curb pull. The result is a visible arc/swoop: buses steer toward the curb as they cross the 0.5 mark, then drift away as the lerp value falls back toward 0 at the actual stop point.

## Solution: Replace the Call Entirely

Instead of patching around the SmootherStep value, `CalculateStopPositionAndDirection` is **replaced entirely** by a transpiler that calls `CalculateModifiedStopPosition`.

The replacement:
1. Computes `pos` and `dir` directly from the Bezier at `targetOffset` (skipping SmootherStep).
2. Applies the full `stopOffset` lateral displacement unconditionally at the actual stop point via `Vector3.Cross(Vector3.up, dir).normalized * stopOffset`.

Because the steering physics already produces a smooth physical approach, no mathematical smoothing of the curb pull is needed — the bus drives smoothly to its curbside position naturally.

## Transpiler Changes (`BusAI_Patch.CalculateSegmentPosition`)

The transpiler makes three insertions into the IL of `BusAI.CalculateSegmentPosition`:

1. **Capture `NetSegment.Flags`** — inserts `Dup` + `Stloc_S` immediately after the `Ldfld m_flags` load so the flags value is available later.

2. **Inject extra arguments** — after the `laneOffset * 0.003921569f` multiply (which leaves `[ref NetLane] [laneOffset]` on the stack), inserts:
   - `Ldarg_1` — vehicleId (`ushort`)
   - `Ldarg_2` — vehicleData (`ref Vehicle`)
   - `Ldloc_2` — lane (`NetInfo.Lane`, pre-verified as local #2)
   - `Ldloc_S localFlags` — the captured flags

3. **Replace the call** — rewrites the `Call CalculateStopPositionAndDirection` instruction to `Call CalculateModifiedStopPosition`.

`TrolleybusAI_Patch` re-uses the same transpiler unchanged.

## `CalculateModifiedStopPosition` Logic

```
targetOffset = laneOffset  // fallback: vanilla position

if laneLength ≥ 1 AND vehicle is not Leaving:
    margin = laneLength / 6
    vehicleLength = vehicleData.Info.m_generatedInfo.m_size.z
    newStopOffset = 1 - (margin + vehicleLength/2) / laneLength

    if newStopOffset ≥ 0.5:
        account for lane/segment invert flags
        targetOffset = adjusted position along Bezier

pos = bezier.Position(targetOffset)
dir = bezier.Tangent(targetOffset)

if stopOffset ≠ 0:
    pos += Cross(up, dir).normalized * stopOffset   // full lateral displacement
```

### Key corrections vs. earlier draft

- **`dist` was wrong**: an earlier version computed `|laneOffset - targetOffset|` as a static constant and passed it to SmootherStep — but that static value cannot represent the per-frame approach position that SmootherStep needs. Discarded entirely.
- **SmootherStep = 1.0 at vanilla offset**: at `laneOffset = 0.5` (vanilla), `SmootherStep(0.5, 0, 0) = 1.0`, so the original full displacement is applied. The new code also applies the full displacement, matching vanilla exactly in the fallback case.
- **`stopOffset` parameter**: this is the *lane's* lateral curb offset (from `m_stopOffset`), not the Bezier position. It is passed in as an additional argument captured from the existing IL before the replaced call.

## Files Changed

| File | Change |
|------|--------|
| `Integration/BetterBusStopPosition/BusAI.cs` | Transpiler rewritten (+62 −74 net); `CalculateModifiedStopPosition` replaced (+11 −19 net) |
