import type { Meta, StoryObj } from "@storybook/react"
import * as React from "react"
import { ZainXDataGrid } from "../components/ZainXDataGrid/ZainXDataGrid"
import { ZainXScheduler } from "../components/ZainXScheduler/ZainXScheduler"
import { ZainXChart } from "../components/ZainXChart/ZainXChart"
import { ZainXRichTextEditor } from "../components/ZainXRichTextEditor/ZainXRichTextEditor"
import { ZainXKanban } from "../components/ZainXDnD/ZainXDnD"
import { Button } from "../components/Button/Button"
import { Badge } from "../components/Badge/Badge"

const meta: Meta = {
  title: "Enterprise Spikes (Phase 1C)",
  parameters: {
    layout: "padded",
  },
}

export default meta

// 1. DataGrid Benchmark Story (10k Synthetic Rows)
export const DataGridEnterpriseBenchmark: StoryObj = {
  render: () => {
    const generateSyntheticData = (count: number) => {
      const depts = ["Engineering", "Finance", "Human Capital", "Operations", "Legal"]
      const titles = ["Senior Specialist", "Manager", "Analyst", "Director", "Lead Architect"]
      const statuses = ["Active", "On Leave", "Probation", "Finalized"]

      return Array.from({ length: count }, (_, i) => ({
        id: `SYNTH-${10000 + i}`,
        name: `Synthetic Person ${i + 1}`,
        department: depts[i % depts.length],
        jobTitle: titles[i % titles.length],
        baseSalary: 12000 + (i % 25) * 1500,
        currency: "SAR",
        status: statuses[i % statuses.length],
        hireDate: `202${(i % 5) + 1}-0${(i % 9) + 1}-15`,
      }))
    }

    const rowData = React.useMemo(() => generateSyntheticData(10000), [])

    const columnDefs: any[] = [
      { field: "id", headerName: "ID", width: 120, pinned: "left" as const },
      { field: "name", headerName: "Full Name", width: 220, filter: true },
      { field: "department", headerName: "Department", width: 160, filter: true },
      { field: "jobTitle", headerName: "Job Title", width: 180 },
      {
        field: "baseSalary",
        headerName: "Base Salary",
        width: 150,
        valueFormatter: (p: any) => `${p.value?.toLocaleString()} SAR`,
      },
      {
        field: "status",
        headerName: "Status",
        width: 130,
        cellRenderer: (p: any) => (
          <Badge variant={p.value === "Active" ? "success" : "neutral"} size="sm">
            {p.value}
          </Badge>
        ),
      },
      { field: "hireDate", headerName: "Effective Date", width: 140 },
    ]

    return (
      <div className="space-y-4">
        <div>
          <h2 className="text-lg font-bold text-text-primary">ZainXDataGrid Enterprise Benchmark</h2>
          <p className="text-xs text-text-tertiary">
            Rendering 10,000 synthetic records with column pinning, sorting, filtering, and cell renderers.
          </p>
        </div>
        <ZainXDataGrid rowData={rowData} columnDefs={columnDefs} height="450px" />
      </div>
    )
  },
}

// 2. Scheduler Story (FullCalendar Open Source Wrapper)
export const SchedulerEnterpriseSpike: StoryObj = {
  render: () => {
    const events = [
      {
        id: "ev-1",
        title: "Team Shift - Morning Core",
        start: "2026-08-25T08:00:00",
        end: "2026-08-25T16:00:00",
        category: "shift" as const,
      },
      {
        id: "ev-2",
        title: "Annual Leave - Khalid A.",
        start: "2026-08-26",
        end: "2026-08-28",
        allDay: true,
        category: "leave" as const,
      },
      {
        id: "ev-3",
        title: "Senior Eng Technical Interview",
        start: "2026-08-27T10:30:00",
        end: "2026-08-27T11:30:00",
        category: "interview" as const,
      },
    ]

    return (
      <div className="space-y-4">
        <div>
          <h2 className="text-lg font-bold text-text-primary">ZainXScheduler Spike</h2>
          <p className="text-xs text-text-tertiary">
            FullCalendar wrapper with custom accessible controls, view switches, and command fallback.
          </p>
        </div>
        <ZainXScheduler
          events={events}
          onAddEventRequest={() => alert("Accessible Add Event Triggered")}
        />
      </div>
    )
  },
}

// 3. Charts Story (Apache ECharts Wrapper with Accessible Table Fallback)
export const ChartsEnterpriseSpike: StoryObj = {
  render: () => {
    const data = [
      { label: "Jan 2026", value: 1420000, formattedValue: "1,420,000 SAR" },
      { label: "Feb 2026", value: 1450000, formattedValue: "1,450,000 SAR" },
      { label: "Mar 2026", value: 1485000, formattedValue: "1,485,000 SAR" },
      { label: "Apr 2026", value: 1510000, formattedValue: "1,510,000 SAR" },
      { label: "May 2026", value: 1535000, formattedValue: "1,535,000 SAR" },
      { label: "Jun 2026", value: 1590000, formattedValue: "1,590,000 SAR" },
      { label: "Jul 2026", value: 1620000, formattedValue: "1,620,000 SAR" },
      { label: "Aug 2026", value: 1680000, formattedValue: "1,680,000 SAR" },
    ]

    return (
      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <ZainXChart
          title="Gross Payroll Variance Trend"
          description="Monthly organizational payroll trajectory"
          type="area"
          data={data}
          unit="SAR"
        />
        <ZainXChart
          title="Headcount by Department"
          description="Active statutory personnel allocation"
          type="donut"
          data={[
            { label: "Engineering", value: 65 },
            { label: "Operations", value: 42 },
            { label: "Finance", value: 18 },
            { label: "Human Capital", value: 12 },
            { label: "Legal", value: 5 },
          ]}
          unit="FTE"
        />
      </div>
    )
  },
}

// 4. RichTextEditor Story (Tiptap with DOMPurify Sanitization)
export const RichTextEditorSpike: StoryObj = {
  render: () => {
    const [content, setContent] = React.useState(
      "<h2>Job Description: Principal Workforce Engineer</h2><p>We are seeking an experienced architect to lead our <strong>Platform Infrastructure</strong>.</p><ul><li>Design high-throughput systems</li><li>Ensure 100% compliance with statutory labor laws</li></ul>"
    )

    return (
      <div className="space-y-4 max-w-2xl">
        <div>
          <h2 className="text-lg font-bold text-text-primary">ZainXRichTextEditor Spike</h2>
          <p className="text-xs text-text-tertiary">
            Tiptap open-source starter-kit with strict DOMPurify sanitization and keyboard formatting.
          </p>
        </div>
        <ZainXRichTextEditor value={content} onChange={setContent} />
      </div>
    )
  },
}

// 5. Kanban / DnD Story (dnd-kit with Command Authority & Rollback)
export const KanbanDnDSpike: StoryObj = {
  render: () => {
    const initialColumns = [
      { id: "applied", title: "Applied", itemIds: ["cand-1", "cand-2"] },
      { id: "screen", title: "Screening", itemIds: ["cand-3"] },
      { id: "interview", title: "Interview", itemIds: ["cand-4"] },
      { id: "offer", title: "Offer Extended", itemIds: [] },
    ]

    const initialItems = {
      "cand-1": { id: "cand-1", title: "Omar Al-Ghamdi", subtitle: "Senior Architect", columnId: "applied", badge: "Score 94%" },
      "cand-2": { id: "cand-2", title: "Sara Al-Husseini", subtitle: "Lead Frontend Engineer", columnId: "applied", badge: "Score 89%" },
      "cand-3": { id: "cand-3", title: "Tariq Mansoor", subtitle: "DevOps Specialist", columnId: "screen", badge: "Score 91%" },
      "cand-4": { id: "cand-4", title: "Mona Al-Shehri", subtitle: "Product Operations", columnId: "interview", badge: "Score 96%" },
    }

    return (
      <div className="space-y-4">
        <div>
          <h2 className="text-lg font-bold text-text-primary">ZainXKanban Drag-and-Drop Spike</h2>
          <p className="text-xs text-text-tertiary">
            dnd-kit stage transition orchestration with server command authority and accessible keyboard fallback controls.
          </p>
        </div>
        <ZainXKanban
          columns={initialColumns}
          items={initialItems}
          onItemMove={(id, src, tgt) => {
            console.log(`Transitioning item ${id} from ${src} to ${tgt}`);
            return true;
          }}
        />
      </div>
    )
  },
}
