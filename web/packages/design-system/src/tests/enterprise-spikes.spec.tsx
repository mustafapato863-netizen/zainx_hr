import { describe, it, expect, vi } from "vitest"
import { render, screen, fireEvent } from "@testing-library/react"
import React from "react"
import { ZainXDataGrid } from "../components/ZainXDataGrid/ZainXDataGrid"
import { ZainXScheduler } from "../components/ZainXScheduler/ZainXScheduler"
import { ZainXChart } from "../components/ZainXChart/ZainXChart"
import { ZainXRichTextEditor } from "../components/ZainXRichTextEditor/ZainXRichTextEditor"
import { ZainXKanban } from "../components/ZainXDnD/ZainXDnD"

describe("ZainX Design System P0 Phase 1C Enterprise Engine Spikes", () => {
  describe("1. ZainXDataGrid Enterprise Wrapper", () => {
    it("renders data grid container and accepts rowData & columnDefs", () => {
      const rowData = [
        { id: "EMP-001", name: "Khalid Mansoor", department: "Engineering" },
      ]
      const columnDefs = [
        { field: "id", headerName: "ID" },
        { field: "name", headerName: "Name" },
      ]

      const { container } = render(
        <ZainXDataGrid rowData={rowData} columnDefs={columnDefs} />
      )

      expect(container.querySelector(".ag-theme-alpine")).toBeDefined()
    })
  })

  describe("2. ZainXScheduler FullCalendar Wrapper", () => {
    it("renders scheduler header and accessible fallback action button", () => {
      const handleAdd = vi.fn()
      const events = [
        { id: "1", title: "Morning Shift", start: "2026-08-25T08:00:00" },
      ]

      render(
        <ZainXScheduler
          events={events}
          onAddEventRequest={handleAdd}
        />
      )

      const addBtn = screen.getByRole("button", { name: /add event/i })
      expect(addBtn).toBeDefined()
      fireEvent.click(addBtn)
      expect(handleAdd).toHaveBeenCalled()
    })
  })

  describe("3. ZainXChart Apache ECharts Wrapper", () => {
    it("renders chart view and toggles to accessible semantic data table", () => {
      const data = [
        { label: "Engineering", value: 45, formattedValue: "45 FTE" },
        { label: "Finance", value: 15, formattedValue: "15 FTE" },
      ]

      render(
        <ZainXChart
          title="Headcount Distribution"
          type="donut"
          data={data}
          unit="FTE"
          allowTableView={true}
        />
      )

      expect(screen.getByText("Headcount Distribution")).toBeDefined()

      // Toggle to accessible table view
      const tableToggleBtn = screen.getByRole("button", { name: /switch to accessible table/i })
      fireEvent.click(tableToggleBtn)

      expect(screen.getByText("Engineering")).toBeDefined()
      expect(screen.getByText("45 FTE")).toBeDefined()
    })
  })

  describe("4. ZainXRichTextEditor Tiptap Wrapper", () => {
    it("renders editor container and cleanses malicious script injection via DOMPurify", async () => {
      const maliciousHtml = '<p>Normal text</p><script>alert("XSS")</script>'
      const handleChange = vi.fn()

      const { container } = render(
        <ZainXRichTextEditor
          value={maliciousHtml}
          onChange={handleChange}
        />
      )

      expect(container.querySelector("script")).toBeNull()
      expect(await screen.findByText("Normal text")).toBeDefined()
    })
  })

  describe("5. ZainXKanban dnd-kit Wrapper", () => {
    it("renders Kanban columns and executes accessible keyboard move actions", async () => {
      const columns = [
        { id: "col-1", title: "Applied", itemIds: ["cand-1"] },
        { id: "col-2", title: "Screening", itemIds: [] },
      ]
      const items = {
        "cand-1": { id: "cand-1", title: "Omar Al-Ghamdi", columnId: "col-1" },
      }
      const handleMove = vi.fn().mockResolvedValue(true)

      render(
        <ZainXKanban
          columns={columns}
          items={items}
          onItemMove={handleMove}
        />
      )

      expect(screen.getByText("Applied")).toBeDefined()
      expect(screen.getByText("Omar Al-Ghamdi")).toBeDefined()

      const moveForwardBtn = screen.getByRole("button", { name: /move omar al-ghamdi forward/i })
      fireEvent.click(moveForwardBtn)

      expect(handleMove).toHaveBeenCalledWith("cand-1", "col-1", "col-2")
    })
  })
})
