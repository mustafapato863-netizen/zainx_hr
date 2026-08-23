import * as React from "react"
import { AgGridReact } from "ag-grid-react"
import { ModuleRegistry, AllEnterpriseModule } from "ag-grid-enterprise"
import type { ColDef, GridOptions, GridReadyEvent, RowSelectedEvent, ICellRendererParams } from "ag-grid-enterprise"
import { cn } from "../../lib/utils"

export type { ColDef, GridOptions, GridReadyEvent, RowSelectedEvent, ICellRendererParams }

// Register AG Grid modules internally inside the wrapper
try {
  ModuleRegistry.registerModules([AllEnterpriseModule])
} catch {
  // Ignore duplicate registration in hot reload environments
}

export interface ZainXColumnDef<TData = any> extends ColDef<TData> {
  sensitive?: boolean
}

export interface ZainXDataGridProps<TData = any> {
  className?: string
  rowData?: TData[] | null
  columnDefs: ZainXColumnDef<TData>[]
  gridOptions?: GridOptions<TData>
  density?: "compact" | "standard" | "comfortable"
  loading?: boolean
  onGridReady?: (event: GridReadyEvent<TData>) => void
  onRowSelected?: (event: RowSelectedEvent<TData>) => void
  height?: string | number
}

export function ZainXDataGrid<TData = any>({
  className,
  rowData,
  columnDefs,
  gridOptions,
  density = "standard",
  loading = false,
  onGridReady,
  onRowSelected,
  height = "400px",
}: ZainXDataGridProps<TData>) {
  const rowHeightMap = {
    compact: 32,
    standard: 40,
    comfortable: 48,
  }

  const defaultColDef = React.useMemo<ColDef>(() => ({
    sortable: true,
    filter: true,
    resizable: true,
    ...gridOptions?.defaultColDef,
  }), [gridOptions?.defaultColDef])

  return (
    <div
      className={cn(
        "ag-theme-alpine w-full overflow-hidden rounded-lg border border-border-default bg-surface shadow-xs",
        className
      )}
      style={{ height }}
    >
      <AgGridReact<TData>
        rowData={rowData}
        columnDefs={columnDefs}
        defaultColDef={defaultColDef}
        rowHeight={rowHeightMap[density]}
        headerHeight={rowHeightMap[density] + 4}
        loading={loading}
        onGridReady={onGridReady}
        onRowSelected={onRowSelected}
        rowSelection={{ mode: "multiRow" }}
        pagination={false}
        animateRows={true}
        {...gridOptions}
      />
    </div>
  )
}
