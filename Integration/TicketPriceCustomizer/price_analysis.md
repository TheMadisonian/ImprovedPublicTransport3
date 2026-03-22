# CS1 Ticket Price Implementation Analysis

## COMPLETE FINDINGS:

### 1. Ticket Price & Demand Calculation
**TransportLine.cs - GetPullValue() [Line 168]**
```
num = Mathf.Max(0, num + 50 - m_ticketPrice * 50 / Mathf.Max(1, info.m_ticketPrice));
```
- **Direct impact**: Higher ticket price directly reduces passenger demand
- Base demand calculated from traffic at stops
- Penalty = (current_price * 50) / default_price
- Each price unit reduces demand proportionally

### 2. Passenger Spawning Flow
**TransportLine.cs - SimulationStep() [Line 1662+]**
1. Calculates GetPullValue() to determine demand
2. Calls TransferManager.AddIncomingOffer() with passenger count
3. Passengers spawn up to line's vehicle capacity
4. Transfer manager matches offers to satisfied citizens

### 3. Pathfinding Cost Consideration
**PathFind.cs - Lines 825, 986, 1187 / NetLane.cs line 67 / NetManager.cs line 475**
- `NetLane.m_ticketCost` is a `byte` (0–255) and IS read by PathFind
- PathFind formula: `num8 += (ticketCost * random(0..1999)) * 3.9215686e-7`
- **HOWEVER: transit lanes are NEVER assigned a non-zero m_ticketCost**
  - `NetManager.cs` initialises all lanes to `m_ticketCost = 0`
  - Only `TollBoothAI` and `ParkGateAI` write non-zero values
  - Bus/tram/train lanes always read 0 here — pathfinding is NOT affected by ticket price

### 4. Money Collection
**VehicleAI - GetTicketPrice() [BusAI Line 842]**
- Citizens pay at boarding
- FreeTransport policy overrides (returns 0)
- Amount deducted from citizen funds

## Impact Summary:
✓ Ticket prices REDUCE passenger demand via GetPullValue() — the ONLY mechanism for transit
✗ Ticket prices do NOT affect pathfinding for transit (m_ticketCost is always 0 on transit lanes)
✓ FreeTransport policy sets price to 0
✓ Default price from VehicleInfo can be overridden per line
✓ m_ticketPrice is a ushort (0–65535) — no overflow risk at 500%

## 5. Transport Types That Actually CHARGE (Confirmed Revenue Types)
Detailed charging behavior:

### Flat Per-Ride Charging (via GetTicketPrice override = 100 by default):
- **Bus** (BusAI.GetTicketPrice) - flat 100
  - **Includes: regular city bus + intercity bus** (both use same slider, classified only by ItemClass.Level)
- **TouristBus** (BusAI.GetTicketPrice) - flat 100
- **Metro** (MetroTrainAI → PassengerTrainAI.GetTicketPrice) - flat 100
- **Train** (PassengerTrainAI.GetTicketPrice) - flat 100
- **Tram** (TramAI.GetTicketPrice) - flat 100
- **Passenger Ship** (PassengerShipAI.GetTicketPrice) - flat 100
- **Passenger Ferry** (PassengerFerryAI.GetTicketPrice) - flat 100 (or 125 with HighTicketPrices policy)
- **Airplane** (PassengerPlaneAI.GetTicketPrice) - flat 100
- **Helicopter** (PassengerHelicopterAI.GetTicketPrice) - flat 100
- **CableCar** (CableCarAI.GetTicketPrice) - flat 100 (or 125 with HighTicketPrices policy)
- **Trolleybus** (TrolleybusAI.GetTicketPrice) - flat 100
- **Blimp** (PassengerBlimpAI.GetTicketPrice) - flat 100
- **Monorail** (PassengerTrainAI or TrainAI subclass depending on implementation) - flat 100 if charging

### Distance-Based Charging (Mileage Taxi Integration):
- **Taxi** (TaxiAI, line 513) - charges per distance:
  - `fare = m_transportInfo.m_ticketPrice * distance_traveled * 0.001`
  - With integration, this is now explicitly referred to as **mileage taxi service**.
  - Base fare value maps to `m_transportInfo.m_ticketPrice` (default 100); multiplier sliders affect this base value in TicketPriceCustomizer.
  - Revenue is added to `EconomyManager.Resource.PublicIncome`.
  - The same taxi multiplier can be adjusted separately for day/night by `TaxiMultiplier` / `TaxiNightMultiplier` in settings.

### Tour Types (Special Handling):
- **TouristBus** (handles "Sightseeing Bus" lines, charges flat 100 via BusAI)
- **Pedestrian / Walking Tours** (ParkLife DLC - identified by transport name "Pedestrian", uses base VehicleAI.GetTicketPrice → **NO REVENUE**, free walking tours)

### Non-Revenue Service Types:
- **EvacuationBus** (no GetTicketPrice override → base returns 0)
- **HotAirBalloon** (BalloonAI, no GetTicketPrice override → base returns 0, **NO REVENUE** despite displaying in tour category)
- **Post** (service vehicle)
- **Fishing** (service vehicle)

**Note on Hot Air Balloon:** HotAirBalloon/PassengerBalloon transport uses BalloonAI which has NO GetTicketPrice override, so it returns 0 like the base. It does NOT share BlimpAI pricing. These are independent types—see separate PassengerBlimpAI entry above which DOES charge.

## 6. 500% Ticket Price Increase Impact
- A 500% multiplier sets `m_ticketPrice = 500` (default base 100). GetPullValue demand formula:
  `num = Mathf.Max(0, num + 50 - m_ticketPrice*50/info.m_ticketPrice)`
- At 100% (price=100): penalty = 50 → neutral (num +50 −50 = unchanged)
- At 200% (price=200): penalty = 100 → demand −50 vs baseline
- At 300% (price=300): penalty = 150 → demand −100 vs baseline
- At ~600% (price=600): penalty = 300 → demand → 0 on typical lines (natural hard cap)
- `Mathf.Max(0,...)` floors demand at zero — lines go empty before money becomes infinite
- **This is already anti-exploit by design**: the game kills ridership before extreme prices pay off
- Pathfinding is NOT separately affected (m_ticketCost = 0 for all transit lanes — see Section 3)

## 7. UI Slider Mechanics & Price Adjustment Strategy

### Current Game Slider (Why It's Confusing):
- Slider value = **raw 100** (not normalized to 1.0)
- UI displays: `value / 100` for currency formatting only
- Actual calculation uses raw: **100 raw = $1.00 fare**
- Problem: Players see "100" on slider, not intuitive for fare pricing

### Your Mod's Independent Sliders (Bus vs Intercity):
You already have separate control—good. Question is: **how to implement 500% feature?**

### Answer: TWO VIABLE APPROACHES

**APPROACH 1: Direct Modification (Simplest)**
```
New price = oldPrice * 5  (for 500%)
E.g., Bus: 100 → 500, Intercity: 100 → 500
```
- Directly multiply `m_ticketPrice` by your multiplier
- No extra fields needed
- Works immediately for all transport types
- **Best for:** "Global 500% price increase this save"
- Con: If player later changes slider, multiplier is lost

**APPROACH 2: Persistent Multiplier Field (Better)**
```
TransportLine gets new field: float m_priceMultiplier = 1.0f
Actual fare = basePrice * m_priceMultiplier
E.g., Bus slider at 200 = 100 * 2.0 = 200 raw fare
```
- Add multiplier field to TransportLine class
- Patch GetTicketPrice() in each AI to multiply result
- Player adjusts slider → multiplier updates → persists in save
- **Best for:** "Dynamic 500% slider per line"

### RECOMMENDATION FOR YOUR MOD:

Since you want **per-line independent control AND 500% feature:**

**Use Approach 2 + Normalize Slider Display:**
1. Change slider **display** from raw 100 → normalized 1.0
   - Slider range: 0.5 to 5.0 (50% to 500%)
   - User sees: "Bus Price: 2.5x" instead of "250"
2. Store as multiplier internally
3. Calculate actual fare: `TransportInfo.m_ticketPrice * m_priceMultiplier`
4. Patch each GetTicketPrice() override to use multiplier

**Code pattern:**
```csharp
// In BusAI.GetTicketPrice()
int basePrice = (m_transportInfo.m_ticketPrice);
float multiplier = Singleton<TransportManager>.instance.m_lines.m_buffer[vehicleData.m_transportLine].m_priceMultiplier;
return Mathf.RoundToInt(basePrice * multiplier);
```

**Benefits:**
- ✓ Intuitive UI (1.0 = 100%, 5.0 = 500%)
- ✓ Per-line independent (Bus and Intercity separate)
- ✓ Persistent (saves with line)
- ✓ Works with all transport types
- ✓ No magic numbers (100 raw confusion gone)
