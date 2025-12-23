# Array Notation Bug Fix

## Issue Description

When attempting to update time slot parameters like `TimeSlot3GridCharge`, which maps to the field path `timeOfUse.slots[2].allowGridCharge`, the system had two critical bugs:

1. **Array notation parsing bug**: The path `slots[2]` was being treated as a dictionary key instead of array access
2. **Serialization type mixing bug**: After updating, the structure contained mixed types (`List<object>`, `Dictionary<string, object>`, `JsonElement`) that didn't serialize correctly when sending to the device

### The Bug

**Expected Behavior:**
```json
{
  "timeOfUse": {
    "slots": [
      { /* slot 0 */ },
      { /* slot 1 */ },
      {
        "allowGridCharge": true  // Update this value in existing slot 2
      }
    ]
  }
}
```

**Actual Behavior:**
```json
{
  "timeOfUse": {
    "slots": [
      { /* slot 0 */ },
      { /* slot 1 */ },
      { /* slot 2 */ },
      { /* slot 3 */ }
    ],
    "slots[2]": {  // BUG: Created as a dictionary key instead of array access!
      "allowGridCharge": true
    }
  }
}
```

### Root Cause

The `SetNestedValue` and `GetNestedValue` methods in `DeviceRpcExecutorService.cs` were splitting the path by `.` but treating each part as a simple dictionary key. They did not parse or handle array notation `[index]`.

When given the path `timeOfUse.slots[2].allowGridCharge`, the code:
1. Navigated to `timeOfUse` ✅ (correct)
2. Tried to find a key named `"slots[2]"` ❌ (wrong - should navigate to `slots` array, then index `2`)
3. Created a new dictionary entry with key `"slots[2]"` ❌ (wrong)

## The Fix

### Changes Made

**File:** `src/Infrastructure/Services/DeviceRpcExecutorService.cs`

#### 1. Added `ParseArrayNotation` Helper Method

```csharp
/// <summary>
/// Parses array notation from a path part (e.g., "slots[2]" returns ("slots", 2))
/// </summary>
private static (string propertyName, int? arrayIndex) ParseArrayNotation(string pathPart)
{
    var openBracket = pathPart.IndexOf('[');
    if (openBracket == -1)
    {
        return (pathPart, null);  // No array notation
    }

    var closeBracket = pathPart.IndexOf(']', openBracket);
    if (closeBracket == -1)
    {
        throw new ArgumentException($"Invalid array notation: {pathPart}");
    }

    var propertyName = pathPart.Substring(0, openBracket);
    var indexStr = pathPart.Substring(openBracket + 1, closeBracket - openBracket - 1);

    if (!int.TryParse(indexStr, out var index))
    {
        throw new ArgumentException($"Invalid array index: {indexStr}");
    }

    return (propertyName, index);
}
```

**Examples:**
- `"slots[2]"` → `("slots", 2)`
- `"items[0]"` → `("items", 0)`
- `"enabled"` → `("enabled", null)`

#### 2. Updated `SetNestedValue` Method

The updated method now:
1. Parses each path part for array notation
2. Handles navigation to arrays properly
3. Accesses array elements by index
4. Preserves existing array elements when updating a specific index

**Key improvements:**
- Converts `JsonElement` arrays to `List<object>` for modification
- Ensures arrays have sufficient capacity before accessing indices
- Properly handles nested objects within arrays
- Maintains all existing array elements while updating specific indices

#### 3. Updated `GetNestedValue` Method

The updated method now:
1. Parses array notation in path parts
2. Accesses array elements correctly
3. Handles both `List<object>` and `JsonElement` arrays

#### 4. Added `NormalizeJsonStructure` Method

This critical method solves the serialization problem by:
1. Serializing the modified structure to JSON string
2. Deserializing back to `JsonElement` (which creates a clean, consistent structure)
3. Converting `JsonElement` back to appropriate .NET types (`Dictionary<string, object>`, `List<object>`, primitives)

```csharp
private static object NormalizeJsonStructure(object obj)
{
    // Serialize to JSON string and parse back to JsonElement for clean structure
    var json = JsonSerializer.Serialize(obj);
    var element = JsonSerializer.Deserialize<JsonElement>(json);

    // Convert JsonElement back to appropriate .NET types for the RPC service
    return ConvertJsonElement(element);
}
```

**Why this is necessary:**

After modifying the device configuration in memory, we end up with a mixed-type structure:
- Original fields from device: `JsonElement` objects
- Modified arrays: `List<object>` containing mix of `JsonElement` and `Dictionary<string, object>`
- Modified nested objects: `Dictionary<string, object>`

When `SendTwoWayRpcAsync` tries to serialize this mixed structure, the serializer gets confused and may:
- Fail to serialize
- Create incorrect JSON
- Send malformed data to the device

The normalization step ensures that before sending to the device, the entire structure is converted to clean, consistent .NET types that serialize correctly.

#### 5. Added `ConvertJsonElement` Helper

Recursively converts `JsonElement` to appropriate .NET types:
- `JsonValueKind.Object` → `Dictionary<string, object>`
- `JsonValueKind.Array` → `List<object>`
- `JsonValueKind.Number` → `int`, `long`, `decimal`, or `double` (tries in order)
- `JsonValueKind.String` → `string`
- `JsonValueKind.True/False` → `bool`
- `JsonValueKind.Null` → `null`

## How It Works Now

### Complete Update Flow

When updating a parameter like `TimeSlot3GridCharge`:

1. **Get current device configuration** via RPC (`getSystemWorkModeConfig`)
   - Receives JSON from device
   - Deserializes to `Dictionary<string, object>` (contains `JsonElement` values)

2. **Update the specific field** using `SetNestedValue`
   - Parses path `timeOfUse.slots[2].allowGridCharge`
   - Navigates: `timeOfUse` → `slots` array → index `2` → `allowGridCharge` field
   - Converts `JsonElement` arrays to `List<object>` as needed
   - Updates only the target field
   - Result: Mixed-type structure with `List<object>`, `Dictionary<string, object>`, and `JsonElement`

3. **Normalize the structure** using `NormalizeJsonStructure`
   - Serializes the mixed-type structure to JSON string
   - Deserializes to `JsonElement` (creates clean structure)
   - Converts to consistent .NET types
   - Result: Clean `Dictionary<string, object>` with `List<object>` arrays

4. **Send to device** via RPC (`setSystemWorkModeConfig`)
   - The normalized structure serializes correctly
   - Device receives valid JSON
   - Device confirms the update

### Example 1: Update TimeSlot3GridCharge

**Path:** `timeOfUse.slots[2].allowGridCharge`

**Process:**
1. Parse `timeOfUse` → Navigate to `timeOfUse` dictionary
2. Parse `slots[2]` → Navigate to `slots` array, then to element at index `2`
3. Parse `allowGridCharge` → Set value in the slot 2 object
4. Normalize structure → Convert mixed types to clean JSON structure

**Resulting JSON:**
```json
{
  "kind": "SystemWorkModeConfigState",
  "timeOfUse": {
    "enabled": true,  // Preserved from current config
    "daysActive": {   // Preserved from current config
      "monday": true,
      // ... other days
    },
    "slots": [
      {
        // Slot 0 - preserved completely
        "startTimeLocal": "08:00",
        "endTimeLocal": "12:00",
        // ... other slot 0 fields
      },
      {
        // Slot 1 - preserved completely
        "startTimeLocal": "12:00",
        "endTimeLocal": "16:00",
        // ... other slot 1 fields
      },
      {
        // Slot 2 - only allowGridCharge updated, rest preserved
        "startTimeLocal": "16:00",      // Preserved
        "endTimeLocal": "20:00",        // Preserved
        "allowGridCharge": true,        // UPDATED
        "allowGeneratorCharge": false,  // Preserved
        "maxBatteryDischargePowerW": 4000,  // Preserved
        "batteryThresholdSocPercent": 15,   // Preserved
        "batteryThresholdVoltageV": 47.0    // Preserved
      },
      {
        // Slot 3 - preserved completely
      },
      {
        // Slot 4 - preserved completely
      },
      {
        // Slot 5 - preserved completely
      }
    ]
  }
}
```

### Example 2: Batch Update Multiple Time Slots

**Request:**
```json
PUT /api/DeviceSettings/device/{deviceId}/parameters
{
  "parameters": [
    { "parameterId": "guid-1", "value": "true" },   // TimeSlot1GridCharge
    { "parameterId": "guid-2", "value": "false" },  // TimeSlot3GenCharge
    { "parameterId": "guid-3", "value": "5000" }    // TimeSlot5MaxDischarge
  ]
}
```

**RPC Payload Sent:**
```json
{
  "method": "setSystemWorkModeConfig",
  "params": {
    "kind": "SystemWorkModeConfigState",
    "timeOfUse": {
      // All other fields preserved...
      "slots": [
        {
          // Slot 0 - only allowGridCharge updated
          "allowGridCharge": true,
          // ... all other fields preserved
        },
        {
          // Slot 1 - completely preserved
        },
        {
          // Slot 2 - only allowGeneratorCharge updated
          "allowGeneratorCharge": false,
          // ... all other fields preserved
        },
        {
          // Slot 3 - completely preserved
        },
        {
          // Slot 4 - only maxBatteryDischargePowerW updated
          "maxBatteryDischargePowerW": 5000,
          // ... all other fields preserved
        },
        {
          // Slot 5 - completely preserved
        }
      ]
    }
  }
}
```

## Testing

### Manual Test

1. Update a time slot parameter:
   ```bash
   PUT /api/DeviceSettings/device/{deviceId}/parameters/{parameterId}
   {
     "value": "true"  // For TimeSlot3GridCharge
   }
   ```

2. Inspect the RPC payload sent to the device (check logs or debugger)

3. Verify the `timeOfUse.slots` array structure:
   - Should be a proper array with 6 elements
   - Should NOT have a key named `"slots[2]"`
   - Only the specific field in the specific slot should be updated

### Expected Results

✅ Array structure is correct
✅ Only the target field in the target slot is modified
✅ All other slots and fields are preserved
✅ Device accepts and processes the update successfully

## Impact

This fix affects all time slot parameter updates:
- TimeSlot1-6 Start/End times
- TimeSlot1-6 GridCharge/GenCharge settings
- TimeSlot1-6 MaxDischarge values
- TimeSlot1-6 MinBatterySOC/MinBatteryVoltage thresholds

**Total parameters affected:** 42 (6 slots × 7 parameters each)

All these parameters now work correctly with proper array handling.

## Additional Notes

- The fix is backward compatible with non-array paths (e.g., `inverterMode.solarSellEnabled`)
- The implementation handles `JsonElement` arrays from device responses correctly
- Array capacity is automatically managed (arrays are extended if needed)
- Error handling for invalid array notation (malformed brackets or non-numeric indices)
