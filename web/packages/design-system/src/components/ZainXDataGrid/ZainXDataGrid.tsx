import * as React from "react"
import type { ColDef, GridOptions, GridReadyEvent, RowSelectedEvent, ICellRendererParams } from "ag-grid-enterprise"
import { cn } from "../../lib/utils"
import type { AgGridViewProps } from "./AgGridView"

const AgGridView = React.lazy(() => import("./AgGridView")) as React.LazyExoticComponent<
  React.ComponentType<AgGridViewProps<any>>
>

export type { ColDef, GridOptions, GridReadyEvent, RowSelectedEvent, ICellRendererParams }

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
  return (
    <div
      className={cn(
        "ag-theme-alpine w-full overflow-hidden rounded-lg border border-border-default bg-surface shadow-xs",
        className
      )}
      style={{ height }}
    >
      <React.Suspense
        fallback={
          <div className="flex h-full min-h-32 items-center justify-center bg-surface-subtle text-sm text-text-secondary">
            Loading data grid…
          </div>
        }
      >
        <AgGridView
          rowData={rowData}
          columnDefs={columnDefs}
          gridOptions={gridOptions}
          density={density}
          loading={loading}
          onGridReady={onGridReady}
          onRowSelected={onRowSelected}
        />
      </React.Suspense>
    </div>
  )
}
