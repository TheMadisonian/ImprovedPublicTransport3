# Ticket Price Customizer

Set individual ticket price multipliers for each transport type — separately for day and night if you have the After Dark DLC.

---

## How ticket prices affect ridership

Raising fares **directly reduces how many citizens choose to use that line**. The game calculates demand for each line based on foot traffic near its stops, then subtracts a penalty based on how much your price exceeds the default fare. 

The practical result:

| Price setting | Effect on a typical busy line |
|---|---|
| 50% | Noticeably more riders, less revenue per passenger |
| 100% | Vanilla — no change |
| 150% | Moderate ridership drop |
| 200% | Significant drop; quiet lines may go nearly empty |
| 250% | Heavy drop; only high-traffic lines stay well-used |

At 250% a line in a low-traffic area can lose most of its riders entirely. A major city-centre route with heavy foot traffic will still run but at reduced capacity. **The cap is 250% for this reason** — beyond that the demand penalty outpaces any revenue benefit on most lines, making it effectively self-defeating.

Reducing prices below 100% will attract more riders and can help lines that are struggling with low ridership.

---

## Day / Night pricing

If After Dark is installed, each transport type has a separate night multiplier. This lets you charge more during peak hours (day) and offer cheaper off-peak night fares — or vice versa for services like taxis that see higher night demand.

---

## Revenue display

The **₡ figure** on the right of each row shows estimated weekly income for that transport type based on current traffic and your multiplier. It updates live as you adjust the slider. Refer to the Public Transport tab at the bottom of the Economy panel for detailed Income/Expenses.

---

## Notes

- **Taxi** pricing is handled by the **Mileage Taxi Services** integration — taxis charge per distance travelled, not a flat boarding fee. The multiplier scales the per-distance fare, so higher multipliers increase the cost per km/mile rather than a fixed ride price.
- **Free Transport** city policy overrides all prices to zero regardless of your settings here.
- **Hot Air Balloons** and **Walking Tours** are free by design in the base game and are not affected by these sliders.
- Settings persist across saves. If you load a save after changing multipliers, prices are reapplied on load.
