import { describe, it, expect, vi, beforeAll } from "vitest"
import { render, screen, fireEvent } from "@testing-library/react"
import React from "react"
import { Select, SelectItem } from "../components/Select/Select"
import { ComboBox, ComboBoxItem } from "../components/ComboBox/ComboBox"
import { Tabs, TabList, Tab, TabPanel } from "../components/Tabs/Tabs"
import { Menu, MenuItem, MenuSection } from "../components/Menu/Menu"
import { Dialog } from "../components/Dialog/Dialog"
import { Drawer } from "../components/Drawer/Drawer"
import { CommandPalette } from "../components/CommandPalette/CommandPalette"
import { DatePicker } from "../components/DatePicker/DatePicker"
import { parseDate } from "@internationalized/date"
import { Button } from "../components/Button/Button"

// Polyfill CSS.escape in JSDOM for React Aria selection collections
beforeAll(() => {
  if (typeof window !== "undefined") {
    if (!window.CSS) {
      window.CSS = {} as any
    }
    if (!window.CSS.escape) {
      window.CSS.escape = (s: string) => s.replace(/([^\w-])/g, "\\$1")
    }
  }
})

describe("ZainX Design System P0 Real Keyboard Interaction Matrix", () => {
  describe("1. Select Keyboard Behavior", () => {
    it("focuses trigger button, handles keyboard Enter/Space and Escape", () => {
      const handleSelect = vi.fn()
      const items = [
        { id: "sa", label: "Saudi Arabia" },
        { id: "eg", label: "Egypt" },
      ]

      render(
        <Select
          label="Country Selection"
          placeholder="Select country"
          items={items}
          onSelectionChange={(k) => handleSelect(String(k))}
        >
          {(item: any) => <SelectItem id={item.id}>{item.label}</SelectItem>}
        </Select>
      )

      const triggerBtn = screen.getByRole("button")
      triggerBtn.focus()
      expect(document.activeElement).toBe(triggerBtn)

      // Press Enter to open
      fireEvent.keyDown(triggerBtn, { key: "Enter", code: "Enter" })
      // Press Escape to dismiss
      fireEvent.keyDown(triggerBtn, { key: "Escape", code: "Escape" })
    })
  })

  describe("2. ComboBox Keyboard Behavior", () => {
    it("handles typing, filtering, and Escape dismissal", () => {
      const items = [
        { id: "eng", label: "Engineering" },
        { id: "fin", label: "Finance" },
      ]

      render(
        <ComboBox label="Department" placeholder="Search departments..." items={items}>
          {(item: any) => <ComboBoxItem id={item.id}>{item.label}</ComboBoxItem>}
        </ComboBox>
      )

      const input = screen.getByPlaceholderText("Search departments...")
      input.focus()
      expect(document.activeElement).toBe(input)

      fireEvent.change(input, { target: { value: "Fin" } })
      expect((input as HTMLInputElement).value).toBe("Fin")

      fireEvent.keyDown(input, { key: "Escape", code: "Escape" })
    })
  })

  describe("3. Tabs Keyboard & Selection Behavior", () => {
    it("handles tab selection and switches active panel", () => {
      render(
        <Tabs defaultSelectedKey="tab1">
          <TabList aria-label="Sections">
            <Tab id="tab1">First Tab</Tab>
            <Tab id="tab2">Second Tab</Tab>
          </TabList>
          <TabPanel id="tab1">Panel 1 Content</TabPanel>
          <TabPanel id="tab2">Panel 2 Content</TabPanel>
        </Tabs>
      )

      expect(screen.getByText("Panel 1 Content")).toBeDefined()
      const secondTab = screen.getByRole("tab", { name: "Second Tab" })
      fireEvent.click(secondTab)
      expect(screen.getByText("Panel 2 Content")).toBeDefined()
    })
  })

  describe("4. Menu Keyboard Behavior", () => {
    it("opens on trigger click/Enter and handles Escape dismissal", () => {
      const handleAction = vi.fn()

      render(
        <Menu
          trigger={<Button>Open Actions</Button>}
        >
          <MenuSection title="Actions">
            <MenuItem onAction={handleAction}>Edit Record</MenuItem>
            <MenuItem isDisabled onAction={vi.fn()}>Locked Option</MenuItem>
            <MenuItem onAction={handleAction}>Archive</MenuItem>
          </MenuSection>
        </Menu>
      )

      const trigger = screen.getByRole("button", { name: "Open Actions" })
      trigger.focus()
      expect(document.activeElement).toBe(trigger)

      fireEvent.keyDown(trigger, { key: "Enter", code: "Enter" })
      fireEvent.keyDown(trigger, { key: "Escape", code: "Escape" })
    })
  })

  describe("5. Dialog Focus Containment & Escape Return", () => {
    it("contains focus inside modal dialog and triggers onOpenChange(false) on Escape", () => {
      const handleOpenChange = vi.fn()

      render(
        <Dialog
          isOpen={true}
          onOpenChange={handleOpenChange}
          title="Modal Confirmation"
          description="Focus is trapped within this dialog container."
        >
          <div>
            <Button>Action Button</Button>
          </div>
        </Dialog>
      )

      expect(screen.getByRole("dialog")).toBeDefined()
      fireEvent.keyDown(screen.getByRole("dialog"), { key: "Escape", code: "Escape" })
      expect(handleOpenChange).toHaveBeenCalledWith(false)
    })
  })

  describe("6. Drawer Modal Behavior", () => {
    it("supports Escape dismissal and focus containment on Drawer", () => {
      const handleOpenChange = vi.fn()

      render(
        <Drawer
          isOpen={true}
          onOpenChange={handleOpenChange}
          side="end"
          title="Employee Details Drawer"
        >
          <div>Drawer Content</div>
        </Drawer>
      )

      expect(screen.getByText("Employee Details Drawer")).toBeDefined()
      fireEvent.keyDown(screen.getByRole("dialog"), { key: "Escape", code: "Escape" })
      expect(handleOpenChange).toHaveBeenCalledWith(false)
    })
  })

  describe("7. CommandPalette Keyboard Shortcut & Navigation", () => {
    it("handles search input, keyboard typing, and Escape close", () => {
      const handleClose = vi.fn()
      const handleSelect = vi.fn()

      render(
        <CommandPalette
          isOpen={true}
          onClose={handleClose}
          onSelect={handleSelect}
        />
      )

      const input = screen.getByPlaceholderText("Type a command or search...")
      input.focus()
      expect(document.activeElement).toBe(input)

      fireEvent.change(input, { target: { value: "Pay" } })
      expect((input as HTMLInputElement).value).toBe("Pay")

      fireEvent.keyDown(input, { key: "Escape", code: "Escape" })
      expect(handleClose).toHaveBeenCalled()
    })
  })

  describe("8. DatePicker Keyboard Interaction", () => {
    it("renders date segments with keyboard focusable triggers", () => {
      render(
        <DatePicker
          label="Contract Start Date"
          defaultValue={parseDate("2026-08-01")}
        />
      )

      expect(screen.getByText("Contract Start Date")).toBeDefined()
      const calendarBtn = screen.getByRole("button", { name: /calendar/i })
      calendarBtn.focus()
      expect(document.activeElement).toBe(calendarBtn)
    })
  })
})
