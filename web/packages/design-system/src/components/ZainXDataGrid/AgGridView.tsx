import * as React from "react"
import { AgGridReact } from "ag-grid-react"
import { ModuleRegistry, AllEnterpriseModule, LicenseManager } from "ag-grid-enterprise"
import type { ColDef, GridOptions, GridReadyEvent, RowSelectedEvent, ICellRendererParams } from "ag-grid-enterprise"
import type { ZainXColumnDef } from "./ZainXDataGrid"

export type { ColDef, GridOptions, GridReadyEvent, RowSelectedEvent, ICellRendererParams }

let isAgGridRegistered = false

/**
 * Lazily registers AG Grid modules and applies commercial license key if present in env.
 * Executed only upon component instantiation to prevent top-level module side effects.
 */
export function ensureAgGridRegistered() {
  if (isAgGridRegistered) return
  try {
    const licenseKey = (import.meta as any)?.env?.VITE_AG_GRID_LICENSE_KEY
    if (licenseKey && typeof LicenseManager !== 'undefined') {
      LicenseManager.setLicenseKey(licenseKey)
    }
    ModuleRegistry.registerModules([AllEnterpriseModule])
    isAgGridRegistered = true
  } catch {
    // Graceful fallback / ignore duplicate registration in test or hot reload environments
  }
}

export interface AgGridViewProps<TData = any> {
  rowData?: TData[] | null
  columnDefs: ZainXColumnDef<TData>[]
  gridOptions?: GridOptions<TData>
  density?: "compact" | "standard" | "comfortable"
  loading?: boolean
  onGridReady?: (event: GridReadyEvent<TData>) => void
  onRowSelected?: (event: RowSelectedEvent<TData>) => void
}

export default function AgGridView<TData = any>({
  rowData,
  columnDefs,
  gridOptions,
  density = "standard",
  loading = false,
  onGridReady,
  onRowSelected,
}: AgGridViewProps<TData>) {
  ensureAgGridRegistered()

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
  )
}
