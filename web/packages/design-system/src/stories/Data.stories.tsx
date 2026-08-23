import * as React from "react"
import type { Meta, StoryObj } from "@storybook/react"
import { Table, TableHeader, TableBody, TableRow, TableHead, TableCell } from "../components/Table/Table"
import { Pagination } from "../components/Pagination/Pagination"
import { Money } from "../components/Money/Money"
import { SensitiveValue, SensitiveValueState } from "../components/SensitiveValue/SensitiveValue"
import { FilterBar } from "../components/FilterBar/FilterBar"
import { SavedViews } from "../components/SavedViews/SavedViews"
import { ColumnChooser } from "../components/ColumnChooser/ColumnChooser"
import { DensitySwitcher } from "../components/DensitySwitcher/DensitySwitcher"
import { BulkActionBar } from "../components/BulkActionBar/BulkActionBar"
import { ZainXDataGrid, ZainXColumnDef } from "../components/ZainXDataGrid/ZainXDataGrid"
import { Badge } from "../components/Badge/Badge"
import { Button } from "../components/Button/Button"
import { Icon } from "../components/Icon/Icon"

const meta: Meta = {
  title: "Data/Tables & Formats",
  tags: ["autodocs"],
}

export default meta

// Fictional test fixtures (NEVER real sensitive or corporate data)
const sampleEmployees = [
  { id: "FICT-001", name: "Khalid Al-Mansoor", dept: "Engineering", title: "Principal Architect", salary: 32000, currency: "SAR", ibanMasked: "••••••••••••", status: "Active" },
  { id: "FICT-002", name: "Noura Al-Shehri", dept: "Finance", title: "Senior Payroll Manager", salary: 24500, currency: "SAR", ibanMasked: "••••••••••••", status: "Active" },
  { id: "FICT-003", name: "Faisal Al-Harbi", dept: "HR & People", title: "Talent Specialist", salary: 4500, currency: "USD", ibanMasked: "••••••••••••", status: "On Leave" },
  { id: "FICT-004", name: "Reem Al-Ghamdi", dept: "Engineering", title: "Frontend SRE", salary: 65000, currency: "EGP", ibanMasked: "••••••••••••", status: "Active" },
]

export const CurrencyFormattingGeneric: StoryObj = {
  render: () => (
    <div className="flex flex-col gap-3 max-w-md p-4 border border-border-default rounded-xl bg-surface">
      <div className="text-xs font-semibold uppercase text-text-tertiary">Generic ISO 4217 Currency Support</div>
      <div className="grid grid-cols-2 gap-2 text-sm">
        <span className="text-text-secondary">Saudi Riyal (SAR):</span>
        <Money amount={18500} currency="SAR" />

        <span className="text-text-secondary">Egyptian Pound (EGP):</span>
        <Money amount={95400} currency="EGP" />

        <span className="text-text-secondary">US Dollar (USD):</span>
        <Money amount={5200} currency="USD" />

        <span className="text-text-secondary">UAE Dirham (AED):</span>
        <Money amount={14800} currency="AED" />

        <span className="text-text-secondary">Euro (EUR):</span>
        <Money amount={4100} currency="EUR" />

        <span className="text-text-secondary">Positive Adjustment:</span>
        <Money amount={2500} currency="SAR" showSign />

        <span className="text-text-secondary">Negative Deduction:</span>
        <Money amount={-750} currency="SAR" showSign />
      </div>
    </div>
  ),
}

export const SensitiveValueStates: StoryObj = {
  render: () => {
    const [state, setState] = React.useState<SensitiveValueState>("masked")
    const [revealedValue, setRevealedValue] = React.useState<string | undefined>(undefined)

    const simulateReveal = () => {
      setState("pending")
      setTimeout(() => {
        setState("revealed")
        // Fictional authorized mock value returned from backend
        setRevealedValue("IBAN-MOCK-AUTHORIZED-992")
      }, 1000)
    }

    const simulateError = () => {
      setState("pending")
      setTimeout(() => {
        setState("error")
      }, 1000)
    }

    return (
      <div className="flex flex-col gap-6 max-w-lg p-6 border border-border-default rounded-xl bg-surface">
        <div>
          <h4 className="text-sm font-semibold text-text-primary mb-1">Sensitive Value Security Contract</h4>
          <p className="text-xs text-text-secondary">
            Client button triggers server authorization. Backend audits the read event and returns plaintext.
          </p>
        </div>

        <div className="space-y-3">
          <div className="flex items-center justify-between border-b border-border-default pb-2">
            <span className="text-xs text-text-secondary">1. Masked State (Default):</span>
            <SensitiveValue state="masked" />
          </div>

          <div className="flex items-center justify-between border-b border-border-default pb-2">
            <span className="text-xs text-text-secondary">2. Authorization Pending:</span>
            <SensitiveValue state="pending" />
          </div>

          <div className="flex items-center justify-between border-b border-border-default pb-2">
            <span className="text-xs text-text-secondary">3. Authorized & Revealed (Fictional token):</span>
            <SensitiveValue state="revealed" value="IBAN-FICTIONAL-DEMO-001" onMask={() => {}} />
          </div>

          <div className="flex items-center justify-between border-b border-border-default pb-2">
            <span className="text-xs text-text-secondary">4. Authorization Denied / Error:</span>
            <SensitiveValue state="error" errorMessage="403: Missing payroll:read_sensitive capability" />
          </div>

          <div className="flex items-center justify-between pt-2">
            <span className="text-xs font-medium text-text-primary">Interactive Server Simulation:</span>
            <SensitiveValue
              state={state}
              value={revealedValue}
              errorMessage="Unauthorized by Security Policy"
              onRevealRequest={simulateReveal}
              onMask={() => {
                setState("masked")
                setRevealedValue(undefined)
              }}
            />
          </div>
        </div>

        <div className="flex gap-2">
          <Button size="xs" variant="secondary" onClick={simulateReveal}>
            Simulate Server Grant
          </Button>
          <Button size="xs" variant="danger" onClick={simulateError}>
            Simulate Server Deny
          </Button>
          <Button size="xs" variant="ghost" onClick={() => { setState("masked"); setRevealedValue(undefined); }}>
            Reset
          </Button>
        </div>
      </div>
    )
  },
}

export const ComposedConfidentialMoney: StoryObj = {
  render: () => (
    <div className="flex items-center gap-2 p-4 border border-border-default rounded-lg bg-surface">
      <span className="text-xs text-text-secondary">Confidential Base Salary:</span>
      <SensitiveValue
        state="masked"
        onRevealRequest={() => {}}
      />
    </div>
  ),
}

export const SemanticTable: StoryObj = {
  render: () => {
    const [page, setPage] = React.useState(1)
    const [selectedIds, setSelectedIds] = React.useState<string[]>([])

    const toggleSelect = (id: string) => {
      setSelectedIds((prev) =>
        prev.includes(id) ? prev.filter((item) => item !== id) : [...prev, id]
      )
    }

    return (
      <div className="space-y-4">
        <div className="flex flex-wrap items-center justify-between gap-3 border-b border-border-default pb-3">
          <SavedViews />
          <div className="flex items-center gap-2">
            <DensitySwitcher density="standard" onChange={() => {}} />
            <ColumnChooser
              columns={[
                { id: "empId", label: "Employee ID", visible: true, required: true },
                { id: "name", label: "Full Name", visible: true, required: true },
                { id: "dept", label: "Department", visible: true },
                { id: "salary", label: "Base Salary", visible: true },
                { id: "iban", label: "Bank Account", visible: true },
              ]}
              onToggleColumn={() => {}}
            />
          </div>
        </div>

        <FilterBar
          filters={[
            { id: "dept", label: "Department", value: "Engineering, Finance" },
            { id: "status", label: "Status", value: "Active" },
          ]}
          onRemoveFilter={() => {}}
          onClearAll={() => {}}
        />

        <div className="rounded-lg border border-border-default overflow-hidden">
          <Table>
            <TableHeader>
              <TableRow>
                <TableHead className="w-10">
                  <input
                    type="checkbox"
                    className="rounded border-border-strong"
                    checked={selectedIds.length === sampleEmployees.length}
                    onChange={(e) =>
                      setSelectedIds(e.target.checked ? sampleEmployees.map((e) => e.id) : [])
                    }
                  />
                </TableHead>
                <TableHead>Employee ID</TableHead>
                <TableHead>Full Name</TableHead>
                <TableHead>Department</TableHead>
                <TableHead>Status</TableHead>
                <TableHead>Base Salary</TableHead>
                <TableHead>Confidential Account</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {sampleEmployees.map((emp) => (
                <TableRow key={emp.id} data-state={selectedIds.includes(emp.id) ? "selected" : undefined}>
                  <TableCell>
                    <input
                      type="checkbox"
                      className="rounded border-border-strong"
                      checked={selectedIds.includes(emp.id)}
                      onChange={() => toggleSelect(emp.id)}
                    />
                  </TableCell>
                  <TableCell className="font-mono text-xs text-text-tertiary">{emp.id}</TableCell>
                  <TableCell className="font-medium text-text-primary">{emp.name}</TableCell>
                  <TableCell>{emp.dept}</TableCell>
                  <TableCell>
                    <Badge variant={emp.status === "Active" ? "success" : "warning"} size="sm" dot>
                      {emp.status}
                    </Badge>
                  </TableCell>
                  <TableCell>
                    <Money amount={emp.salary} currency={emp.currency} />
                  </TableCell>
                  <TableCell>
                    <SensitiveValue state="masked" onRevealRequest={() => {}} />
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
        </div>

        <Pagination
          page={page}
          pageSize={10}
          totalItems={42}
          onPageChange={setPage}
        />

        <BulkActionBar
          selectedCount={selectedIds.length}
          onClearSelection={() => setSelectedIds([])}
          actions={
            <>
              <Button variant="secondary" size="xs">
                <Icon name="download" size="xs" />
                <span>Export Selected</span>
              </Button>
              <Button variant="primary" size="xs">
                <Icon name="check" size="xs" />
                <span>Bulk Approve</span>
              </Button>
            </>
          }
        />
      </div>
    )
  },
}

export const EnterpriseDataGridDemo: StoryObj = {
  render: () => {
    const colDefs: ZainXColumnDef<(typeof sampleEmployees)[0]>[] = [
      { field: "id", headerName: "ID", width: 120 },
      { field: "name", headerName: "Employee Name", flex: 1 },
      { field: "dept", headerName: "Department", flex: 1 },
      { field: "title", headerName: "Job Title", flex: 1 },
      {
        field: "salary",
        headerName: "Base Salary",
        width: 160,
        valueFormatter: (p: any) => `${p.data?.currency} ${p.value?.toLocaleString()}`,
      },
    ]

    return (
      <div className="space-y-3">
        <div className="text-xs text-text-secondary">
          ZainXDataGrid wraps AG Grid Enterprise cleanly behind our token theme with zero vendor leak to feature consumers.
        </div>
        <ZainXDataGrid
          rowData={sampleEmployees}
          columnDefs={colDefs}
          height={260}
          density="standard"
        />
      </div>
    )
  },
}
