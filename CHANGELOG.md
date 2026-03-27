# Changelog — Improved Public Transport 3

## [3.0.1] Public Transport Unstucker integration

- Integrated Public Transport Unstucker support directly into IPT3.
- Added on/off toggle in the Unbunching settings tab.
- Added conflict detection logging for key economy/transport patches:
  - `EconomyPanel.Awake` patch checks for other Harmony owners before applying.
  - `TransportLine.SimulationStep` patch checks for existing prefix/postfix/transpilers before applying.
  - Logs warnings with other patch owners to help diagnose IPT3 budget/income anomalies in mod combinations.
- Added targeted debug logging to help track budget ordering issues:
  - `CanLeaveStopPatch` logs line, wait time, and chosen result.
  - `SimulationStepPatch` logs maintenance cost applied a line and active vehicle count.
  - `TicketPriceCustomizer` logs each ticket price adjustment and the number of modified lines.

## [3.0.0] UI, Performance, and Safety enhancements from previous IPT2 version

### UI Enhancements

- **Depot names now show user-assigned names first.** If a depot building has a custom name set in-game, that name is displayed. If not, the prefab name is used, qualified with the district name in parentheses when the depot is inside a district (e.g. `Bus Depot (Downtown)`). Falls back to the raw prefab `Info.name` if neither is available.
- **Label "Depot:" narrowed** from 97 px to 60 px and gap reduced from 6 px to 2 px so the dropdown has more room.
- **Dropdown widened** from 167 px to 241 px, making longer depot names fully visible.
- **District names in the dropdown now refresh live.** Added a lightweight hash of the district names of visible depots; the dropdown is repopulated whenever the hash changes (e.g. after renaming a district in-game), with no per-frame cost when nothing has changed.

- **Intercity Buses:** now selectable and editable in Vehicle Editor
- **Line Info Panel:** 'Number of vehicles:' shortened to 'Vehicle count:' so it doesn't overlap with 'Refresh Line Color/Name' button.


### Safety — Null-reference guards

- **`ActiveVehiclesQuery`**: Added `if (info == null) continue;` guard before accessing `VehicleInfo.m_class`, preventing a `NullReferenceException` if a vehicle slot holds a prefab that has since been unloaded.
- **`DepotAI.StartTransfer` redirection guard (IPT + More Vehicles compatibility)**: In `StartTransferPatch.StartTransferPre`, validate redirected depot before calling `StartTransfer` and abort with warning if depot is invalid. This prevents infinite redirection loops when using vehicle expansion mods such as More Vehicles Renewed.
- **`WaitingPassengerCountQuery`**: Added `citizenInstance.Info != null` guard before calling `TransportArriveAtSource`, preventing a crash when a citizen instance references an unloaded `CitizenInfo` prefab. Also cached `ref var citizenInstance` to eliminate five repeated buffer dereferences per loop iteration.
- **`PanelExtenderVehicle`**: Added null check on `TransportLine.Info` before accessing `Info.m_class` in `UpdateBindings`, preventing a crash when a line has no assigned prefab.
- **`PanelExtenderVehicle`**: Rewrote vehicle ID resolution in `UpdateBindings` to use `GetFirstVehicle()` so passenger exchange stats are always read from leading vehicle (fixes 0|0 passenger exchange on trailers). 
- **`PanelExtenderLine`**: Added null checks in `GetDepotDistrictNamesHash` (verifying depot array is not null before iteration) and `IDToName` (validating building IDs), fixing repeated NullReferenceException spam in error logs.

### Safety — BetterBoarding integration crash fixes

- Added bounds checks in BetterBoarding `LoadPassengers` prefixes for all vehicle types (bus, trolleybus, tram, train, ferry, helicopter, blimp): verify `CachedVehicleData.m_cachedVehicleData != null && vehicleID < ...Length` before `BoardPassengers`.
- Added node guard in BetterBoarding `LoadPassengers` prefixes: verify `CachedNodeData.m_cachedNodeData != null && currentStop < ...Length` before `PassengersIn +=`.
- **`BoardingUtility.ProcessRankedChoices` critical guards**: Added bounds checks for `chosenVehicleID` (verify `> 0 && < vehicleBuffer.Length`) and `citizenID` (verify `!= 0`) before buffer access. Added null checks for `citizenInfo` and `citizenInfo.m_citizenAI` before calling instance methods.


### Performance — Dictionary lookup (O(1)) replacing linear search (O(n))

- **`VehiclePrefabs`**: Added a `Dictionary<int, PrefabData> _prefabDataByIndex` field, populated in `RegisterPrefab`. New public `FindByIndex(int prefabDataIndex)` method does an O(1) lookup by `m_prefabDataIndex`.
- **`SimulationStepPatch`**: Replaced `Array.Find(prefabs, item => item.PrefabDataIndex == ...)` (O(n) linear scan executed once per vehicle per simulation tick) with `VehiclePrefabs.instance.FindByIndex(vInfo.m_prefabDataIndex)` (O(1) dictionary lookup). Also added a null guard on `vInfo` before the lookup.
- **`QueuedVehicleQuery`**: Replaced the O(n×m) nested loop (iterate all queued vehicles × iterate all known prefabs) with a single-pass approach: build a `Dictionary<string, PrefabData>` from the prefab list once, then do O(1) `TryGetValue` per queued vehicle. Also added an early-return when the queue is empty.

### Performance — Allocation reduction

- **`CachedTransportLineData.GetRandomPrefab`**: Removed a `HashSet.ToArray()` allocation that previously occurred on every vehicle spawn. The method now counts `prefabs.Count`, picks a random index, and iterates the `HashSet` with an index counter to retrieve the nth element — no intermediate array is created.
- **`VehiclePrefabs.GetPrefabsNoLogging` (all-levels overload)**: Replaced a four-stage `.Concat().Concat().Concat().ToArray()` LINQ chain (three intermediate arrays) with a single pre-allocated array filled via `CopyTo` — one array, zero intermediate allocations.
- **`VehiclePrefabs.FindByName`**: Changed from direct dictionary indexer (throws `KeyNotFoundException` on missing key) to `TryGetValue`, preventing a crash and eliminating the associated exception overhead.
- **`BoardingUtility`**: Replaced `freeVehiclesList.OrderBy(item => Vector3.Distance(...))` LINQ sort (allocates `IEnumerable` + enumerator per passenger) with a stack-allocated copy into a `VehicleOccupancyInfo[]` sort buffer followed by `System.Array.Sort` with a comparison delegate. Also changed `Vector3.Distance` (computes a square root) to `Vector3.SqrMagnitude` (no square root) since only relative ordering matters.

### Performance — Critical: Spatial grid replaces O(32,768) linear scan

- **`PublicTransportStopWorldInfoPanel.ProcessNodes`**: Replaced a full linear scan of all 32,768 net nodes with NetManager's built-in spatial node grid. The grid divides the world into 270×270 64-unit cells; the fix searches only the 3×3 cells surrounding the stop position, reducing the worst-case from 32,768 iterations to typically <50. This eliminates the main source of lag when renaming a stop or clicking "ungroup nearby stops". Also added a `?.` null-guard on `netNode1.Info` to prevent a rare NullReferenceException.

### Performance — Throttling: reduce per-frame citizen grid scan

- **`PublicTransportStopWorldInfoPanel.UpdateBindings`**: `WaitingPassengerCountQuery.Query()` was called every `LateUpdate` frame while the stop panel was open. The result is now cached and only re-queried at most every 0.5 seconds, eliminating redundant citizen grid scans during busy rush-hour periods.

### Performance — Per-frame Singleton and buffer caching

- **`PanelExtenderLine.UpdateBindings`**: Cached `Singleton<TransportManager>.instance` into a local `tm` variable, eliminating 4 repeated singleton lookups per frame while the line panel is open.
- **`PanelExtenderVehicle.UpdateBindings`**: Added `ref var vehicle = ref vm.m_vehicles.m_buffer[(int)vehicleID]` to cache the vehicle buffer slot by reference, eliminating 5+ repeated struct dereferences per frame while the vehicle panel is open. Also replaced `Array.Find(VehiclePrefabs.instance.GetPrefabs(...), lambda)` with `VehiclePrefabs.instance.FindByIndex(vehicle.Info.m_prefabDataIndex)` (O(1) dictionary lookup), and added a null guard when no prefab is found rather than crashing.

### Safety — Additional null guards

- **`SelectVehicleTypesCommand.Execute`**: Added `.Where(v => v.Info != null)` filter before `.Select(v => v.Info.name)` to prevent a NullReferenceException if a selected prefab item has a null Info reference.
- **`PanelExtenderVehicle.UpdateBindings`**: Added `if (vehicleID == 0) return;` and `if (vehicle.Info == null) return;` guards before accessing vehicle buffer data, preventing crashes when the panel is accessed with no vehicle selected or with an unloaded prefab.

### Performance — LINQ allocation removal in data classes

- **`PrefabData.TotalCapacity`**: Replaced `_trailerData.Select((t, index) => _trailerData[index].Capacity).Sum()` (allocates an `IEnumerable<int>` and enumerator per call) with a plain `for` loop — zero allocations.
- **`PrefabData.CarCount`**: Replaced `_trailerData.Count(t => t.Info.GetSubService() == ...)` (allocates a lambda closure per call) with a `for` loop that caches `Info.GetSubService()` into a local, ensuring the virtual call happens once instead of once per trailer. Zero allocations.
- **`PrefabData` carriage-aware costs**: `TotalCapacity`, `CarCount`, and maintenance cost are now based on `Info.m_trailers` each access, supporting runtime dynamic carriage add/remove mods (e.g., CarriageNumberSelector) and preventing stale maintenance cost after behavior-modifying train length changes.

### Performance — CityService panel: cache stop/vehicle data to eliminate per-frame allocations

- **`PanelExtenderCityService.Update` (bus/metro/train/ship/plane/monorail/trolleybus branch)**: `GetStationStops()` (which walks the building's net-node → segment → lane graph and allocates a `List<ushort>`) was called every frame while the city-service panel was open. The result is now cached in a `_cachedStopArray` field and only recomputed when the displayed building changes. The `Concat().ToArray()` LINQ chain used when a sub-building contributes additional stops is replaced with a pre-allocated array filled via two `CopyTo` calls — zero intermediate allocations.
- **`PanelExtenderCityService.Update` (taxi/cable-car branch)**: `GetDepotVehicles()` (which allocates a `List<ushort>`) was called unconditionally every frame. The fix first counts owned vehicles by walking the lightweight `m_ownVehicles` linked list (no allocation), then only calls `GetDepotVehicles()` and rebuilds the UI list when the count actually changes.

