using Microsoft.SqlServer.Management.UI.Grid;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Automation;
using System.Windows.Forms;
using System.Drawing;


namespace Zraxion.Extension
{
    /// <summary>
    /// Provides read-only diagnostics for discovering the SSMS Results Grid
    /// through the Windows UI Automation tree.
    /// </summary>
    internal static class ResultsGridProbe
    {


        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        [DllImport("user32.dll")]
        private static extern bool EnumChildWindows(
            IntPtr hWndParent,
            EnumWindowsProc lpEnumFunc,
            IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetClassName(
            IntPtr hWnd,
            StringBuilder lpClassName,
            int nMaxCount);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetWindowText(
            IntPtr hWnd,
            StringBuilder lpString,
            int nMaxCount);


        private const int MaxCandidates = 40;

        /// <summary>
        /// Inspects the UI Automation hierarchy of the current SSMS process
        /// and reports controls that appear related to query results.
        /// </summary>
        public static string InspectAutomationTree()
        {
            var process = Process.GetCurrentProcess();

            if (process.MainWindowHandle == IntPtr.Zero)
            {
                return "SSMS main window handle was not available.";
            }

            AutomationElement root;

            try
            {
                root = AutomationElement.FromHandle(process.MainWindowHandle);
            }
            catch (Exception ex)
            {
                return $"Unable to access the SSMS UI Automation root.\n\n{ex.Message}";
            }

            var candidates = new List<string>();

            try
            {
                var elements = root.FindAll(
                    TreeScope.Descendants,
                    Condition.TrueCondition);

                foreach (AutomationElement element in elements)
                {
                    if (candidates.Count >= MaxCandidates)
                    {
                        break;
                    }

                    InspectElement(element, candidates);
                }
            }
            catch (ElementNotAvailableException)
            {
                // The SSMS UI may change while the automation tree is being inspected.
            }

            var builder = new StringBuilder();

            builder.AppendLine($"Automation candidates: {candidates.Count}");
            builder.AppendLine();

            if (candidates.Count == 0)
            {
                builder.AppendLine(
                    "No Results Grid candidates were detected through UI Automation.");

                return builder.ToString();
            }

            for (var i = 0; i < candidates.Count; i++)
            {
                builder.AppendLine($"{i + 1}. {candidates[i]}");
                builder.AppendLine();
            }

            return builder.ToString();
        }

        /// <summary>
        /// Inspects one UI Automation element and records it when it
        /// appears relevant to the SSMS Results Grid.
        /// </summary>
        private static void InspectElement(
            AutomationElement element,
            ICollection<string> candidates)
        {
            try
            {
                var name = element.Current.Name ?? string.Empty;
                var className = element.Current.ClassName ?? string.Empty;
                var automationId = element.Current.AutomationId ?? string.Empty;
                var controlType = element.Current.ControlType;

                var controlTypeName =
                    controlType?.ProgrammaticName ?? string.Empty;

                var searchable =
                    $"{name} {className} {automationId} {controlTypeName}";

                if (!LooksRelevant(searchable, controlType))
                {
                    return;
                }

                candidates.Add(
                    $"Name: {Normalize(name)}\n" +
                    $"Class: {Normalize(className)}\n" +
                    $"AutomationId: {Normalize(automationId)}\n" +
                    $"ControlType: {Normalize(controlTypeName)}\n" +
                    $"HWND: 0x{element.Current.NativeWindowHandle:X}\n" +
                    $"Keyboard focus: {element.Current.HasKeyboardFocus}\n" +
                    $"Patterns: {GetPatternNames(element)}");
            }
            catch (ElementNotAvailableException)
            {
                // Ignore controls that disappear while SSMS updates its UI.
            }
        }

        /// <summary>
        /// Determines whether an automation element is likely to represent
        /// the SSMS Results Grid or one of its immediate containers.
        /// </summary>
        private static bool LooksRelevant(
            string value,
            ControlType? controlType)
        {
            // Strong UI Automation candidates.
            if (controlType == ControlType.DataGrid ||
                controlType == ControlType.Table)
            {
                return true;
            }

            // SSMS may expose its Results Grid as a custom/native control.
            if (value.IndexOf(
                    "Grid",
                    StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }

            if (value.IndexOf(
                    "Result",
                    StringComparison.OrdinalIgnoreCase) >= 0 &&
                (controlType == ControlType.Pane ||
                 controlType == ControlType.Custom ||
                 controlType == ControlType.Table ||
                 controlType == ControlType.DataGrid))
            {
                return true;
            }

            return false;
        }

        /// <summary>
/// Finds the active SSMS Results pane and inspects all native child windows
/// hosted underneath it.
/// </summary>
public static string InspectResultsPaneChildren()
{
    var process = Process.GetCurrentProcess();

    if (process.MainWindowHandle == IntPtr.Zero)
    {
        return "SSMS main window handle was not available.";
    }

    AutomationElement root;

    try
    {
        root = AutomationElement.FromHandle(process.MainWindowHandle);
    }
    catch (Exception ex)
    {
        return $"Unable to access the SSMS automation root.\n\n{ex.Message}";
    }

    var resultsPanes = root.FindAll(
        TreeScope.Descendants,
        new AndCondition(
            new PropertyCondition(
                AutomationElement.NameProperty,
                "Results"),
            new PropertyCondition(
                AutomationElement.ControlTypeProperty,
                ControlType.Pane)));

    AutomationElement? activeResultsPane = null;

    foreach (AutomationElement element in resultsPanes)
    {
        try
        {
            if (element.Current.HasKeyboardFocus)
            {
                activeResultsPane = element;
                break;
            }

            if (activeResultsPane is null &&
                !element.Current.IsOffscreen)
            {
                activeResultsPane = element;
            }
        }
        catch (ElementNotAvailableException)
        {
            // Ignore controls that disappear while SSMS updates its UI.
        }
    }

    if (activeResultsPane is null)
    {
        return "No active Results pane was detected.";
    }

    var nativeHandle =
        new IntPtr(activeResultsPane.Current.NativeWindowHandle);

    if (nativeHandle == IntPtr.Zero)
    {
        return "The active Results pane does not expose a native HWND.";
    }

    var entries = new List<string>();

    // Inspect the Results host itself.
    AddNativeWindowEntry(nativeHandle, entries);

    // Inspect every native child hosted inside Results.
    EnumChildWindows(
        nativeHandle,
        (childHandle, _) =>
        {
            AddNativeWindowEntry(childHandle, entries);
            return entries.Count < 100;
        },
        IntPtr.Zero);

    var builder = new StringBuilder();

    builder.AppendLine(
        $"Results HWND: 0x{nativeHandle.ToInt64():X}");

    builder.AppendLine(
        $"Native controls found: {entries.Count}");

    builder.AppendLine();

    for (var i = 0; i < entries.Count; i++)
    {
        builder.AppendLine($"{i + 1}. {entries[i]}");
        builder.AppendLine();
    }

    return builder.ToString();
}



        /// <summary>
        /// Captures diagnostic information about one native child window.
        /// </summary>
        private static void AddNativeWindowEntry(
            IntPtr handle,
            ICollection<string> entries)
        {
            var classBuilder = new StringBuilder(512);
            GetClassName(
                handle,
                classBuilder,
                classBuilder.Capacity);

            var textBuilder = new StringBuilder(1024);
            GetWindowText(
                handle,
                textBuilder,
                textBuilder.Capacity);

            Control? control = null;

            try
            {
                control = Control.FromHandle(handle);
            }
            catch
            {
                // A native child does not necessarily have a managed WinForms wrapper.
            }

            var managedType =
                control?.GetType().AssemblyQualifiedName
                ?? "<none>";

            var controlName =
                string.IsNullOrWhiteSpace(control?.Name)
                    ? "<none>"
                    : control!.Name;

            entries.Add(
                $"HWND: 0x{handle.ToInt64():X}\n" +
                $"Class: {Normalize(classBuilder.ToString())}\n" +
                $"Text: {Normalize(textBuilder.ToString())}\n" +
                $"Managed type: {managedType}\n" +
                $"Control name: {controlName}");
        }
        /// <summary>
        /// Reports UI Automation patterns supported by an element.
        /// </summary>
        private static string GetPatternNames(AutomationElement element)
        {
            var patterns = new List<string>();

            AddPattern<GridPattern>(
                element,
                GridPattern.Pattern,
                "Grid",
                patterns);

            AddPattern<TablePattern>(
                element,
                TablePattern.Pattern,
                "Table",
                patterns);

            AddPattern<SelectionPattern>(
                element,
                SelectionPattern.Pattern,
                "Selection",
                patterns);

            AddPattern<SelectionItemPattern>(
                element,
                SelectionItemPattern.Pattern,
                "SelectionItem",
                patterns);

            AddPattern<ValuePattern>(
                element,
                ValuePattern.Pattern,
                "Value",
                patterns);

            return patterns.Count == 0
                ? "<none>"
                : string.Join(", ", patterns);
        }

        private static void AddPattern<T>(
            AutomationElement element,
            AutomationPattern pattern,
            string name,
            ICollection<string> patterns)
        {
            if (element.TryGetCurrentPattern(pattern, out _))
            {
                patterns.Add(name);
            }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value)
                ? "<none>"
                : value;
        }

        /// <summary>
        /// Inspects the runtime type information exposed by the active SSMS
        /// Results Grid without modifying the query results.
        /// </summary>
        public static string InspectGridRuntimeType()
        {
            var process = Process.GetCurrentProcess();

            if (process.MainWindowHandle == IntPtr.Zero)
            {
                return "SSMS main window handle was not available.";
            }

            AutomationElement root;

            try
            {
                root = AutomationElement.FromHandle(process.MainWindowHandle);
            }
            catch (Exception ex)
            {
                return $"Unable to access the SSMS automation root.\n\n{ex.Message}";
            }

            var resultsPanes = root.FindAll(
                TreeScope.Descendants,
                new AndCondition(
                    new PropertyCondition(
                        AutomationElement.NameProperty,
                        "Results"),
                    new PropertyCondition(
                        AutomationElement.ControlTypeProperty,
                        ControlType.Pane)));

            foreach (AutomationElement element in resultsPanes)
            {
                try
                {
                    if (!element.Current.HasKeyboardFocus)
                    {
                        continue;
                    }

                    var handle =
                        new IntPtr(element.Current.NativeWindowHandle);

                    if (handle == IntPtr.Zero)
                    {
                        continue;
                    }

                    var control = Control.FromHandle(handle);

                    if (control is null)
                    {
                        return "The Results HWND does not map to a managed control.";
                    }

                    return DescribeRuntimeType(control);
                }
                catch (ElementNotAvailableException)
                {
                    // SSMS may rebuild the Results UI while probing it.
                }
            }

            return "No focused Results Grid control was detected.";
        }

        /// <summary>
        /// Returns useful runtime metadata for the discovered SSMS Grid control.
        /// </summary>
        private static string DescribeRuntimeType(Control control)
        {
            var type = control.GetType();
            var builder = new StringBuilder();

            builder.AppendLine($"Type: {type.FullName}");
            builder.AppendLine($"Assembly: {type.Assembly.FullName}");
            builder.AppendLine($"Assembly location: {type.Assembly.Location}");
            builder.AppendLine($"Base type: {type.BaseType?.FullName ?? "<none>"}");
            builder.AppendLine();

            builder.AppendLine("Interfaces:");

            foreach (var interfaceType in type.GetInterfaces()
                         .OrderBy(item => item.FullName))
            {
                builder.AppendLine($"- {interfaceType.FullName}");
            }

            builder.AppendLine();
            builder.AppendLine("Interesting properties:");

            var properties = type.GetProperties(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            foreach (var property in properties
                         .Where(property =>
                             IsInterestingMember(property.Name))
                         .OrderBy(property => property.Name))
            {
                builder.AppendLine(
                    $"- {property.PropertyType.FullName} {property.Name}");
            }

            builder.AppendLine();
            builder.AppendLine("Interesting fields:");

            var fields = type.GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            foreach (var field in fields
                         .Where(field =>
                             IsInterestingMember(field.Name))
                         .OrderBy(field => field.Name))
            {
                builder.AppendLine(
                    $"- {field.FieldType.FullName} {field.Name}");
            }

            builder.AppendLine();
            builder.AppendLine("Interesting methods:");

            var methods = type.GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic);

            foreach (var method in methods
                         .Where(method =>
                             IsInterestingMember(method.Name))
                         .Select(method => method.Name)
                         .Distinct()
                         .OrderBy(name => name))
            {
                builder.AppendLine($"- {method}");
            }

            return builder.ToString();
        }

        private static bool IsInterestingMember(string name)
        {
            return name.IndexOf("Grid", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Select", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Storage", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Cell", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Row", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Column", StringComparison.OrdinalIgnoreCase) >= 0
                || name.IndexOf("Data", StringComparison.OrdinalIgnoreCase) >= 0;
        }


        public static string InspectSelectedCells()
        {
            var process = Process.GetCurrentProcess();

            if (process.MainWindowHandle == IntPtr.Zero)
            {
                return "SSMS main window handle was not available.";
            }

            AutomationElement root;

            try
            {
                root = AutomationElement.FromHandle(process.MainWindowHandle);
            }
            catch (Exception ex)
            {
                return $"Unable to access the SSMS automation root.\n\n{ex.Message}";
            }

            var resultsPanes = root.FindAll(
                TreeScope.Descendants,
                new AndCondition(
                    new PropertyCondition(
                        AutomationElement.NameProperty,
                        "Results"),
                    new PropertyCondition(
                        AutomationElement.ControlTypeProperty,
                        ControlType.Pane)));

            foreach (AutomationElement element in resultsPanes)
            {
                try
                {
                    if (!element.Current.HasKeyboardFocus)
                    {
                        continue;
                    }

                    var handle = new IntPtr(
                        element.Current.NativeWindowHandle);

                    if (handle == IntPtr.Zero)
                    {
                        continue;
                    }

                    var control = Control.FromHandle(handle);

                    if (control is not IGridControl grid)
                    {
                        continue;
                    }

                    return DescribeSelection(grid);
                }
                catch (ElementNotAvailableException)
                {
                    // Ignore transient UI changes.
                }
            }

            return "No focused SSMS Results Grid was detected.";
        }



        private static string DescribeSelection(IGridControl grid)
        {
            var builder = new StringBuilder();

            var selectedBlocks = grid.SelectedCells;
            var storage = grid.GridStorage;

            builder.AppendLine(
                $"Rows in storage: {storage?.NumRows() ?? 0}");

            builder.AppendLine(
                $"Columns: {grid.ColumnsNumber}");

            builder.AppendLine(
                $"Selected blocks: {selectedBlocks?.Count ?? 0}");

            builder.AppendLine();

            if (selectedBlocks is null || selectedBlocks.Count == 0)
            {
                builder.AppendLine("No cells are currently selected.");
                return builder.ToString();
            }

            var blockIndex = 0;

            foreach (BlockOfCells block in selectedBlocks)
            {
                blockIndex++;

                builder.AppendLine($"Block {blockIndex}:");
                builder.AppendLine(
                    $"Rows: {block.Y} -> {block.Bottom}");

                builder.AppendLine(
                    $"Columns: {block.X} -> {block.Right}");

                builder.AppendLine(
                    $"Width: {block.Width}");

                builder.AppendLine(
                    $"Height: {block.Height}");

                builder.AppendLine();
            }

            return builder.ToString();
        }


        /// <summary>
        /// Reads values from the cells currently selected in the active SSMS Results Grid.
        /// This operation is read-only and does not re-run the SQL query.
        /// </summary>
        public static string InspectSelectedCellValues()
        {
            var process = Process.GetCurrentProcess();

            if (process.MainWindowHandle == IntPtr.Zero)
            {
                return "SSMS main window handle was not available.";
            }

            AutomationElement root;

            try
            {
                root = AutomationElement.FromHandle(process.MainWindowHandle);
            }
            catch (Exception ex)
            {
                return $"Unable to access the SSMS automation root.\n\n{ex.Message}";
            }

            var resultsPanes = root.FindAll(
                TreeScope.Descendants,
                new AndCondition(
                    new PropertyCondition(
                        AutomationElement.NameProperty,
                        "Results"),
                    new PropertyCondition(
                        AutomationElement.ControlTypeProperty,
                        ControlType.Pane)));

            foreach (AutomationElement element in resultsPanes)
            {
                try
                {
                    if (!element.Current.HasKeyboardFocus)
                    {
                        continue;
                    }

                    var handle =
                        new IntPtr(element.Current.NativeWindowHandle);

                    if (handle == IntPtr.Zero)
                    {
                        continue;
                    }

                    var control = Control.FromHandle(handle);

                    if (control is not GridControl grid)
                    {
                        continue;
                    }

                    return DescribeSelectedCellValues(grid);
                }
                catch (ElementNotAvailableException)
                {
                    // SSMS may rebuild the Results UI while the probe is running.
                }
            }

            return "No focused SSMS Results Grid was detected.";
        }


        /// <summary>
        /// Returns diagnostic information and values for selected grid cells.
        /// </summary>
        private static string DescribeSelectedCellValues(GridControl grid)
        {
            const int maxCellsToDisplay = 100;

            var selectedBlocks = grid.SelectedCells;
            var storage = grid.GridStorage;

            if (storage is null)
            {
                return "The active Results Grid does not expose grid storage.";
            }

            if (selectedBlocks is null || selectedBlocks.Count == 0)
            {
                return "No cells are currently selected.";
            }

            var builder = new StringBuilder();

            var displayedCells = 0;
            long totalSelectedCells = 0;

            foreach (BlockOfCells block in selectedBlocks)
            {
                totalSelectedCells +=
                    block.Height * block.Width;
            }

            builder.AppendLine(
                $"Selected cells: {totalSelectedCells}");

            builder.AppendLine(
                $"Rows in storage: {storage.NumRows()}");

            builder.AppendLine();

            foreach (BlockOfCells block in selectedBlocks)
            {
                for (long row = block.Y; row <= block.Bottom; row++)
                {
                    for (var uiColumn = block.X;
                         uiColumn <= block.Right;
                         uiColumn++)
                    {
                        if (displayedCells >= maxCellsToDisplay)
                        {
                            builder.AppendLine();
                            builder.AppendLine(
                                $"Output limited to {maxCellsToDisplay} cells.");

                            return builder.ToString();
                        }

                        var storageColumn =
                            grid.GetStorageColumnIndexByUIIndex(uiColumn);

                        if (storageColumn < 0)
                        {
                            builder.AppendLine(
                                $"Row {row} | UI Col {uiColumn} | " +
                                "Storage Col <invalid>");

                            continue;
                        }

                        string value;

                        try
                        {
                            value = storage.GetCellDataAsString(
                                        row,
                                        storageColumn)
                                    ?? "<null>";
                        }
                        catch (Exception ex)
                        {
                            value =
                                $"<read error: {ex.GetType().Name}>";
                        }

                        builder.AppendLine(
                            $"Row {row} | " +
                            $"UI Col {uiColumn} | " +
                            $"Storage Col {storageColumn} | " +
                            $"Value: {value}");

                        displayedCells++;
                    }
                }
            }

            return builder.ToString();
        }

        public static string SumSelectedCells()
        {
            var process = Process.GetCurrentProcess();

            if (process.MainWindowHandle == IntPtr.Zero)
            {
                return "SSMS main window handle was not available.";
            }

            AutomationElement root;

            try
            {
                root = AutomationElement.FromHandle(process.MainWindowHandle);
            }
            catch (Exception ex)
            {
                return $"Unable to access the SSMS automation root.\n\n{ex.Message}";
            }

            var resultsPanes = root.FindAll(
                TreeScope.Descendants,
                new AndCondition(
                    new PropertyCondition(
                        AutomationElement.NameProperty,
                        "Results"),
                    new PropertyCondition(
                        AutomationElement.ControlTypeProperty,
                        ControlType.Pane)));

            foreach (AutomationElement element in resultsPanes)
            {
                try
                {
                    if (!element.Current.HasKeyboardFocus)
                    {
                        continue;
                    }

                    var handle = new IntPtr(
                        element.Current.NativeWindowHandle);

                    if (handle == IntPtr.Zero)
                    {
                        continue;
                    }

                    var control = Control.FromHandle(handle);

                    if (control is not GridControl grid)
                    {
                        continue;
                    }

                    return CalculateSelectionSum(grid);
                }
                catch (ElementNotAvailableException)
                {
                    // SSMS may rebuild the Results pane while it is being inspected.
                }
            }

            return "No focused SSMS Results Grid was detected.";
        }

        private static string CalculateSelectionSum(GridControl grid)
        {
            GridSelectionAnalytics.NumericSelectionSummary summary;

            try
            {
                summary = GridSelectionAnalytics.Analyze(grid);
            }
            catch (OverflowException)
            {
                return
                    "SUM could not be calculated because the selected " +
                    "numeric values exceed the supported decimal range.";
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message;
            }

            if (summary.SelectedCount == 0)
            {
                return "No cells are currently selected.";
            }

            if (summary.NumericCount == 0)
            {
                return
                    $"No numeric values were found.\n\n" +
                    $"Selected: {summary.SelectedCount}\n" +
                    $"NULL / Empty: {summary.NullOrEmptyCount}\n" +
                    $"Non-numeric: {summary.NonNumericCount}";
            }

            return
                $"SUM = {summary.Sum.ToString("G29", CultureInfo.CurrentCulture)}\n\n" +
                $"Numeric cells: {summary.NumericCount}\n" +
                $"Selected cells: {summary.SelectedCount}\n" +
                $"NULL / Empty: {summary.NullOrEmptyCount}\n" +
                $"Non-numeric: {summary.NonNumericCount}";
        }




        public static string AverageSelectedCells()
        {
            return FormatNumericStatistic(
                "AVERAGE",
                summary => summary.Average);
        }

        public static string MinSelectedCells()
        {
            return FormatNumericStatistic(
                "MIN",
                summary => summary.Min);
        }

        public static string MaxSelectedCells()
        {
            return FormatNumericStatistic(
                "MAX",
                summary => summary.Max);
        }

        public static string CountSelectedCells()
        {
            return FormatNumericCount();
        }

        private static string FormatNumericStatistic(
            string label,
            Func<GridSelectionAnalytics.NumericSelectionSummary, decimal?> selector)
        {
            var grid = GetFocusedResultsGrid();

            if (grid is null)
            {
                return "No focused SSMS Results Grid was detected.";
            }

            GridSelectionAnalytics.NumericSelectionSummary summary;

            try
            {
                summary = GridSelectionAnalytics.Analyze(grid);
            }
            catch (OverflowException)
            {
                return
                    $"{label} could not be calculated because the selected " +
                    "numeric values exceed the supported decimal range.";
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message;
            }

            if (summary.SelectedCount == 0)
            {
                return "No cells are currently selected.";
            }

            if (summary.NumericCount == 0)
            {
                return
                    $"No numeric values were found.\n\n" +
                    $"Selected: {summary.SelectedCount}\n" +
                    $"NULL / Empty: {summary.NullOrEmptyCount}\n" +
                    $"Non-numeric: {summary.NonNumericCount}";
            }

            var value = selector(summary);

            return
                $"{label} = {value?.ToString("G29", CultureInfo.CurrentCulture)}\n\n" +
                $"Numeric cells: {summary.NumericCount}\n" +
                $"Selected cells: {summary.SelectedCount}\n" +
                $"NULL / Empty: {summary.NullOrEmptyCount}\n" +
                $"Non-numeric: {summary.NonNumericCount}";
        }

        private static string FormatNumericCount()
        {
            var grid = GetFocusedResultsGrid();

            if (grid is null)
            {
                return "No focused SSMS Results Grid was detected.";
            }

            GridSelectionAnalytics.NumericSelectionSummary summary;

            try
            {
                summary = GridSelectionAnalytics.Analyze(grid);
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message;
            }

            if (summary.SelectedCount == 0)
            {
                return "No cells are currently selected.";
            }

            return
                $"COUNT = {summary.NumericCount}\n\n" +
                $"Selected cells: {summary.SelectedCount}\n" +
                $"NULL / Empty: {summary.NullOrEmptyCount}\n" +
                $"Non-numeric: {summary.NonNumericCount}";
        }


        private static GridControl? GetFocusedResultsGrid()
        {
            var process = Process.GetCurrentProcess();

            if (process.MainWindowHandle == IntPtr.Zero)
            {
                return null;
            }

            AutomationElement root;

            try
            {
                root = AutomationElement.FromHandle(process.MainWindowHandle);
            }
            catch
            {
                return null;
            }

            var resultsPanes = root.FindAll(
                TreeScope.Descendants,
                new AndCondition(
                    new PropertyCondition(
                        AutomationElement.NameProperty,
                        "Results"),
                    new PropertyCondition(
                        AutomationElement.ControlTypeProperty,
                        ControlType.Pane)));

            foreach (AutomationElement element in resultsPanes)
            {
                try
                {
                    if (!element.Current.HasKeyboardFocus)
                    {
                        continue;
                    }

                    var handle = new IntPtr(
                        element.Current.NativeWindowHandle);

                    if (handle == IntPtr.Zero)
                    {
                        continue;
                    }

                    var control = Control.FromHandle(handle);

                    if (control is GridControl grid)
                    {
                        return grid;
                    }
                }
                catch (ElementNotAvailableException)
                {
                    // SSMS may rebuild the Results pane while it is inspected.
                }
            }

            return null;
        }

        internal static GridControl? GetFocusedResultsGridForUi()
        {
            return GetFocusedResultsGrid();
        }

        internal static int GetResultGridIndexForUi(
            GridControl grid)
        {
            return GetResultGridIndex(grid);
        }


        public static string SetCurrentSelectionAsComparisonBaseline()
        {
            var grid = GetFocusedResultsGrid();

            if (grid is null)
            {
                return "No focused SSMS Results Grid was detected.";
            }

            GridAnalyticsSnapshot snapshot;

            try
            {
                snapshot = GridSelectionAnalytics.CreateSnapshot(
                    grid,
                    "A");
            }
            catch (OverflowException)
            {
                return
                    "The comparison baseline could not be created because " +
                    "the selected numeric values exceed the supported decimal range.";
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message;
            }

            if (snapshot.SelectedCount == 0)
            {
                return "No cells are currently selected.";
            }

            if (snapshot.NumericCount == 0)
            {
                return
                    "The selected cells do not contain numeric values.";
            }

            var resultGridIndex =
                GetResultGridIndex(grid);

            int? baselineUiColumn =
                TryGetSingleSelectedUiColumn(
                    grid,
                    out var selectedUiColumn)
                    ? selectedUiColumn
                    : null;

            var comparisonContext =
                GridAnalyticsBarManager
                    .GetComparisonContextForGrid(
                        grid);

            if (comparisonContext is null)
            {
                return
                    "Zraxion could not resolve the active Results tab for this baseline.";
            }

            GridComparisonSession.SetLeft(
                comparisonContext,
                snapshot,
                resultGridIndex,
                baselineUiColumn);

            return
                $"Comparison baseline A saved.\n\n" +
                $"SUM = {snapshot.Sum.ToString("G29", CultureInfo.CurrentCulture)}\n" +
                $"Numeric cells: {snapshot.NumericCount}\n" +
                $"Selected cells: {snapshot.SelectedCount}";
        }

        private static bool TryGetSingleSelectedUiColumn(
            GridControl grid,
            out int uiColumn)
        {
            uiColumn = -1;

            var selectedBlocks =
                grid.SelectedCells;

            if (selectedBlocks is null ||
                selectedBlocks.Count == 0)
            {
                return false;
            }

            foreach (BlockOfCells block in selectedBlocks)
            {
                if (block.X != block.Right)
                {
                    return false;
                }

                if (uiColumn < 0)
                {
                    uiColumn = block.X;
                    continue;
                }

                if (block.X != uiColumn)
                {
                    return false;
                }
            }

            return uiColumn >= 0;
        }

        public static string CompareCurrentSelectionWithBaseline()
        {
            var grid = GetFocusedResultsGrid();

            if (grid is null)
            {
                return "No focused SSMS Results Grid was detected.";
            }

            var comparisonContext =
                GridAnalyticsBarManager
                    .GetComparisonContextForGrid(
                        grid);

            if (comparisonContext is null)
            {
                return
                    "Zraxion could not resolve the active Results tab for comparison.";
            }

            var baseline =
                GridComparisonSession.GetLeftSnapshot(
                    comparisonContext);

            if (baseline is null)
            {
                return
                    "No comparison baseline has been saved for this Results tab.\n\n" +
                    "Select numeric cells and use 'Set Selection as A' first.";
            }


            GridAnalyticsSnapshot current;

            try
            {
                current = GridSelectionAnalytics.CreateSnapshot(
                    grid,
                    "B");
            }
            catch (OverflowException)
            {
                return
                    "The comparison could not be completed because " +
                    "the selected numeric values exceed the supported decimal range.";
            }
            catch (InvalidOperationException ex)
            {
                return ex.Message;
            }

            if (current.SelectedCount == 0)
            {
                return "No cells are currently selected.";
            }

            if (current.NumericCount == 0)
            {
                return
                    "The current selection does not contain numeric values.";
            }

            GridAnalyticsComparison comparison;

            try
            {
                comparison =
                    new GridAnalyticsComparison(
                        baseline,
                        current,
                        GridComparisonSession.GetTolerancePolicy(
                            comparisonContext));
            }
            catch (OverflowException)
            {
                return
                    "The comparison could not be completed because " +
                    "the calculated difference exceeds the supported decimal range.";
            }

            GridAnalyticsBarManager.ShowComparison(
                comparison);

           

            return string.Empty;
        }
        public static string InspectResultsGridParentChain()
        {
            var grid = GetFocusedResultsGrid();

            if (grid is null)
            {
                return "No focused SSMS Results Grid was detected.";
            }

            var builder = new StringBuilder();

            Control? current = grid;
            var level = 0;

            while (current is not null && level < 12)
            {
                builder.AppendLine($"Level {level}");
                builder.AppendLine($"Type: {current.GetType().FullName}");
                builder.AppendLine($"Name: {current.Name}");
                builder.AppendLine($"Dock: {current.Dock}");
                builder.AppendLine($"Bounds: {current.Bounds}");
                builder.AppendLine($"ClientSize: {current.ClientSize}");
                builder.AppendLine($"Children: {current.Controls.Count}");
                builder.AppendLine();

                current = current.Parent;
                level++;
            }

            return builder.ToString();
        }
        public static string InspectResultsContainerChildren()
        {
            var grid = GetFocusedResultsGrid();

            if (grid is null)
            {
                return "No focused SSMS Results Grid was detected.";
            }

            var smartPanel = grid.Parent;
            var multiControlPanel = smartPanel?.Parent;

            if (smartPanel is null || multiControlPanel is null)
            {
                return "Expected Results Grid container hierarchy was not found.";
            }

            var builder = new StringBuilder();

            builder.AppendLine("=== SmartPanel ===");
            builder.AppendLine($"Type: {smartPanel.GetType().FullName}");
            builder.AppendLine($"Children: {smartPanel.Controls.Count}");
            builder.AppendLine();

            for (var i = 0; i < smartPanel.Controls.Count; i++)
            {
                var child = smartPanel.Controls[i];

                builder.AppendLine($"Child {i}");
                builder.AppendLine($"Type: {child.GetType().FullName}");
                builder.AppendLine($"Dock: {child.Dock}");
                builder.AppendLine($"Bounds: {child.Bounds}");
                builder.AppendLine();
            }

            builder.AppendLine("=== MultiControlPanel ===");
            builder.AppendLine($"Type: {multiControlPanel.GetType().FullName}");
            builder.AppendLine($"Children: {multiControlPanel.Controls.Count}");
            builder.AppendLine();

            for (var i = 0; i < multiControlPanel.Controls.Count; i++)
            {
                var child = multiControlPanel.Controls[i];

                builder.AppendLine($"Child {i}");
                builder.AppendLine($"Type: {child.GetType().FullName}");
                builder.AppendLine($"Dock: {child.Dock}");
                builder.AppendLine($"Bounds: {child.Bounds}");
                builder.AppendLine($"Children: {child.Controls.Count}");
                builder.AppendLine();
            }

            return builder.ToString();
        }

        public static string InspectSmartPanelRuntimeStructure()
        {
            var grid = GetFocusedResultsGrid();

            if (grid is null)
            {
                return "No focused SSMS Results Grid was detected.";
            }

            var smartPanel = grid.Parent;

            if (smartPanel is null)
            {
                return "SmartPanel parent was not found.";
            }

            var type = smartPanel.GetType();
            var builder = new StringBuilder();

            builder.AppendLine($"Type: {type.FullName}");
            builder.AppendLine();

            builder.AppendLine("=== Properties ===");

            foreach (var property in type.GetProperties(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic))
            {
                if (property.Name.IndexOf("panel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    property.Name.IndexOf("control", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    property.Name.IndexOf("layout", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    property.Name.IndexOf("split", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    property.Name.IndexOf("size", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    builder.AppendLine(
                        $"{property.PropertyType.FullName} {property.Name}");
                }
            }

            builder.AppendLine();
            builder.AppendLine("=== Fields ===");

            foreach (var field in type.GetFields(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic))
            {
                if (field.Name.IndexOf("panel", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    field.Name.IndexOf("control", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    field.Name.IndexOf("layout", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    field.Name.IndexOf("split", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    field.Name.IndexOf("size", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    builder.AppendLine(
                        $"{field.FieldType.FullName} {field.Name}");
                }
            }

            builder.AppendLine();
            builder.AppendLine("=== Methods ===");

            foreach (var method in type.GetMethods(
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic))
            {
                if (method.Name.IndexOf("layout", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    method.Name.IndexOf("control", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    method.Name.IndexOf("split", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    method.Name.IndexOf("panel", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    builder.AppendLine(method.ToString());
                }
            }

            return builder.ToString();
        }


        public static string AttachTestAnalyticsBar()
        {
            var grid = GetFocusedResultsGrid();

            if (grid is null)
            {
                return "No focused SSMS Results Grid was detected.";
            }

            Control? current = grid;

            while (current is not null &&
                   current.GetType().FullName !=
                   "Microsoft.SqlServer.Management.UI.VSIntegration.Editors.GridResultsTabPage")
            {
                current = current.Parent;
            }

            if (current is null)
            {
                return "GridResultsTabPage was not found.";
            }

            const string barName = "ZraxionAnalyticsBar";
            const string labelName = "ZraxionAnalyticsLabel";

            Panel? bar = current.Controls[barName] as Panel;
            Label? label = null;

            if (bar is null)
            {
                bar = new Panel
                {
                    Name = barName,
                    Dock = DockStyle.Bottom,
                    Height = 36,
                    Padding = new Padding(10, 0, 10, 0)
                };

                label = new Label
                {
                    Name = labelName,
                    AutoSize = false,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                };

                bar.Controls.Add(label);

                current.SuspendLayout();

                try
                {
                    current.Controls.Add(bar);
                    bar.BringToFront();
                }
                finally
                {
                    current.ResumeLayout(true);
                }
            }
            else
            {
                label = bar.Controls[labelName] as Label;
            }

            if (label is null)
            {
                return "Zraxion Analytics Bar exists, but its label could not be found.";
            }

            GridSelectionAnalytics.NumericSelectionSummary summary;

            try
            {
                summary = GridSelectionAnalytics.Analyze(grid);
            }
            catch (OverflowException)
            {
                label.Text =
                    "Zraxion  |  Selected values exceed the supported decimal range.";

                return "Analytics bar updated with an overflow warning.";
            }
            catch (InvalidOperationException ex)
            {
                label.Text = $"Zraxion  |  {ex.Message}";
                return ex.Message;
            }

            var resultIndex = GetResultGridIndex(grid);

            if (summary.SelectedCount == 0)
            {
                label.Text =
                    $"Zraxion  |  Result {resultIndex}  |  No cells selected";

                return "Analytics bar updated.";
            }

            if (summary.NumericCount == 0)
            {
                label.Text =
                    $"Zraxion  |  Result {resultIndex}  |  " +
                    $"No numeric values  |  Selected {summary.SelectedCount}";

                return "Analytics bar updated.";
            }

            var averageText =
                summary.Average.HasValue
                    ? summary.Average.Value.ToString(
                        "N2",
                        CultureInfo.CurrentCulture)
                    : "-";

            label.Text =
                $"Zraxion  |  Result {resultIndex}  |  " +
                $"SUM {summary.Sum.ToString("N2", CultureInfo.CurrentCulture)}  |  " +
                $"COUNT {summary.NumericCount}  |  " +
                $"AVG {averageText}";

            return
                $"Analytics bar updated for Result {resultIndex}.";
        }

        private static int GetResultGridIndex(GridControl grid)
        {
            var parent = grid.Parent;

            if (parent is null)
            {
                return 1;
            }

            var resultIndex = 0;

            foreach (Control control in parent.Controls)
            {
                if (control is GridControl resultGrid)
                {
                    resultIndex++;

                    if (ReferenceEquals(resultGrid, grid))
                    {
                        return resultIndex;
                    }
                }
            }

            return 1;
        }


    }
}