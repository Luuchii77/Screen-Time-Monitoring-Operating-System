# Fixed Issues - Running Applications Table

## Issues Fixed

### 1. **Apps Disappearing After 5 Seconds** ✅
**Problem**: When an app closed, it would disappear from the table after 5 seconds instead of persisting with "Closed" status.

**Root Cause**: The `RefreshHistoryData()` method was called every 5 seconds by `_historyAutoRefreshTimer` and would clear the entire list with `_historyItems.Clear()`. Closed apps were added briefly but then removed on the next refresh.

**Solution**: Changed the refresh logic to:
- Update existing items instead of clearing the entire list
- Mark closed apps as "Closed" status (persistent)
- Only add new apps when they first appear
- Preserve all apps including closed ones across refresh cycles

**File**: [MainWindow.xaml.cs](ScreenTimeMonitor.UI.WPF/Views/MainWindow.xaml.cs) lines 768-914

**Result**: ✅ Closed apps now remain in the table with Status="Closed"

---

### 2. **DataGrid Column Misalignment** ✅
**Problem**: Columns were misaligned - headers didn't match up with the data. The display showed corrupted text like "00: 1 Rui".

**Root Cause**: Fixed column widths were too large, causing columns to overflow and misalign. The Application column width was set to 180px minimum which was too restrictive.

**Solution**: 
- Adjusted column widths for better fit:
  - **Application**: `*` (responsive) with `MinWidth="140"`
  - **Total Time**: `100` (reduced from 150)
  - **Sessions**: `75` (reduced from 100)
  - **Times Opened**: `100` (new column)
  - **Status**: `90` (reduced from 120)
- Added `VerticalAlignment="Center"` to all ElementStyle templates for proper text centering

**File**: [MainWindow.xaml](ScreenTimeMonitor.UI.WPF/Views/MainWindow.xaml) lines 251-271

**Result**: ✅ Columns now properly aligned with headers matching data

---

### 3. **Added "Times Opened" Column** ✅
**Purpose**: Track how many times each application has been opened.

**Implementation**:
- Added `TimesOpened` property to `AppUsageHistoryItem` class (default = 0)
- New column displays the count in orange color (#FF9800)
- Column width: 100px, center-aligned

**File**: 
- [MainWindow.xaml.cs](ScreenTimeMonitor.UI.WPF/Views/MainWindow.xaml.cs) line 1463 (TimesOpened property)
- [MainWindow.xaml](ScreenTimeMonitor.UI.WPF/Views/MainWindow.xaml) lines 260-269 (UI column)

**Result**: ✅ "Times Opened" column now displays in the table

---

## DataGrid Column Layout (After Fix)

| Column | Width | Alignment | Color | Purpose |
|--------|-------|-----------|-------|---------|
| Application | * (responsive) | Left | #E8EAED (white) | App name |
| Total Time | 100px | Center | #00D9FF (cyan) | Usage duration |
| Sessions | 75px | Center | #00BCD4 (light cyan) | Session count |
| Times Opened | 100px | Center | #FF9800 (orange) | Launch count |
| Status | 90px | Center | #4CAF50 (green) | Running/Closed state |

---

## Behavior Changes

### Before:
- App closes → Marked "Closed" briefly → Disappears after 5 seconds
- Table columns misaligned with garbled text
- No "Times Opened" data

### After:
- App closes → Marked "Closed" → **Persists permanently** in the table
- Table properly aligned with correct column widths
- Shows "Times Opened" count for each application
- Status colors clearly indicate app state (green for all states currently)

---

## Code Changes Summary

### AppUsageHistoryItem Class
```csharp
public int TimesOpened { get; set; } = 0;  // NEW
```

### RefreshHistoryData Logic
- Removed `_historyItems.Clear()` call
- Changed from replacing entire list to updating existing items
- New apps get `Status = "Running"` and `TimesOpened = 1`
- Closed apps get `Status = "Closed"` and remain in list
- Running apps are updated with latest duration/status

### XAML DataGrid Changes
- Added new column for "Times Opened" with orange color (#FF9800)
- Adjusted all column widths for better layout
- Added `VerticalAlignment="Center"` to ElementStyle templates
- Columns now properly separated and aligned

---

## Build Status
✅ Build succeeded with 0 errors

## Testing Notes
- Service running without errors
- UI running without errors  
- DataGrid displays properly aligned
- Closed apps persist in table
- New "Times Opened" column visible
