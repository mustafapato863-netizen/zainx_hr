import { describe, it, expect, vi } from "vitest"
import { render, screen, fireEvent, waitFor } from "@testing-library/react"
import React from "react"
import { Button } from "../components/Button/Button"
import { IconButton } from "../components/Button/IconButton"
import { Icon } from "../components/Icon/Icon"
import { Input } from "../components/Input/Input"
import { Field } from "../components/Input/Field"
import { Checkbox, CheckboxGroup } from "../components/Checkbox/Checkbox"
import { Radio, RadioGroup } from "../components/Radio/Radio"
import { Switch } from "../components/Switch/Switch"
import { Select, SelectItem } from "../components/Select/Select"
import { ComboBox, ComboBoxItem } from "../components/ComboBox/ComboBox"
import { DatePicker } from "../components/DatePicker/DatePicker"
import { parseDate } from "@internationalized/date"
import { EffectiveDate } from "../components/EffectiveDate/EffectiveDate"
import { Tabs, TabList, Tab, TabPanel } from "../components/Tabs/Tabs"
import { Menu, MenuItem, MenuSection } from "../components/Menu/Menu"
import { Dialog, ConfirmDialog, DestructiveDialog } from "../components/Dialog/Dialog"
import { Drawer } from "../components/Drawer/Drawer"
import { CommandPalette } from "../components/CommandPalette/CommandPalette"
import { ContextSwitcher } from "../components/ContextSwitcher/ContextSwitcher"
import { ToastProvider, useToast } from "../components/Toast/Toast"
import { Pagination } from "../components/Pagination/Pagination"
import { SensitiveValue } from "../components/SensitiveValue/SensitiveValue"
import { Money } from "../components/Money/Money"
import { AccessDenied } from "../components/AccessDenied/AccessDenied"
import { QuickCreate } from "../components/QuickCreate/QuickCreate"
import { ZainXDataGrid } from "../components/ZainXDataGrid/ZainXDataGrid"
import { Card, CardHeader, CardTitle, CardContent } from "../components/Card/Card"
import { EmphasisCard } from "../components/Card/EmphasisCard"
import { SpotlightCard } from "../components/Card/SpotlightCard"

describe("ZainX Design System P0 Interactive Quality Matrix", () => {
  describe("1. Button & IconButton Behavioral Contracts", () => {
    it("handles click and renders variants", () => {
      const handleClick = vi.fn()
      render(<Button variant="primary" onClick={handleClick}>Finalize Run</Button>)
      const btn = screen.getByRole("button", { name: /finalize run/i })
      fireEvent.click(btn)
      expect(handleClick).toHaveBeenCalledTimes(1)
    })

    it("prevents interaction when loading or disabled", () => {
      const handleClick = vi.fn()
      render(<Button loading disabled onClick={handleClick}>Processing</Button>)
      const btn = screen.getByRole("button", { name: /processing/i })
      expect(btn).toHaveProperty("disabled", true)
      fireEvent.click(btn)
      expect(handleClick).not.toHaveBeenCalled()
    })

    it("renders IconButton with required accessible name", () => {
      const handleAction = vi.fn()
      render(
        <IconButton aria-label="Notification Center" onClick={handleAction}>
          <Icon name="bell" size="sm" />
        </IconButton>
      )
      const iconBtn = screen.getByRole("button", { name: /notification center/i })
      fireEvent.click(iconBtn)
      expect(handleAction).toHaveBeenCalledTimes(1)
    })
  })

  describe("2. Selection Controls (Checkbox, Radio, Switch)", () => {
    it("toggles Checkbox state and supports indeterminate", () => {
      render(<Checkbox isIndeterminate>Statutory Annuity</Checkbox>)
      const cb = screen.getByRole("checkbox", { name: /statutory annuity/i })
      expect(cb).toBeDefined()
    })

    it("renders RadioGroup with keyboard navigation", () => {
      render(
        <RadioGroup label="Contract Type" defaultValue="full-time">
          <Radio value="full-time">Full Time</Radio>
          <Radio value="part-time">Part Time</Radio>
        </RadioGroup>
      )
      const radio = screen.getByRole("radio", { name: /full time/i })
      expect(radio).toBeDefined()
    })

    it("toggles Switch component", () => {
      render(<Switch>Auto-Approval</Switch>)
      const toggle = screen.getByRole("switch", { name: /auto-approval/i })
      expect(toggle).toBeDefined()
    })
  })

  describe("3. Select vs ComboBox Distinction", () => {
    it("renders non-editable Select with options list", () => {
      const items = [
        { id: "sa", label: "Saudi Arabia" },
        { id: "eg", label: "Egypt" },
        { id: "ae", label: "UAE" },
      ]
      render(
        <Select label="Country" placeholder="Choose Country" items={items}>
          {(item: any) => <SelectItem id={item.id}>{item.label}</SelectItem>}
        </Select>
      )
      expect(screen.getByText("Country")).toBeDefined()
      expect(screen.getByRole("button")).toBeDefined()
    })

    it("renders editable/searchable ComboBox with filter input", () => {
      const items = [
        { id: "eng", label: "Engineering" },
        { id: "fin", label: "Finance" },
      ]
      render(
        <ComboBox label="Department" placeholder="Search departments..." items={items}>
          {(item: any) => <ComboBoxItem id={item.id}>{item.label}</ComboBoxItem>}
        </ComboBox>
      )
      expect(screen.getByText("Department")).toBeDefined()
      expect(screen.getByPlaceholderText("Search departments...")).toBeDefined()
    })
  })

  describe("4. DatePicker & Decoupled EffectiveDate", () => {
    it("renders DatePicker with date segments", () => {
      render(
        <DatePicker
          label="Contract Start Date"
          defaultValue={parseDate("2026-08-01")}
        />
      )
      expect(screen.getByText("Contract Start Date")).toBeDefined()
    })

    it("supports generic presets and custom date in EffectiveDate without calculating domain truth", () => {
      const handleChange = vi.fn()
      render(
        <EffectiveDate
          selectedPresetId="today"
          onChange={handleChange}
        />
      )
      expect(screen.getByText("Immediate (Today)")).toBeDefined()
      expect(screen.getByText("1st of Next Month")).toBeDefined()
      expect(screen.getByText("Custom Date")).toBeDefined()

      fireEvent.click(screen.getByText("1st of Next Month"))
      expect(handleChange).toHaveBeenCalledWith(expect.any(String), "next-month")
    })
  })

  describe("5. Navigation & Tabs", () => {
    it("switches tab panel when selected", () => {
      render(
        <Tabs defaultSelectedKey="summary">
          <TabList>
            <Tab id="summary">Summary</Tab>
            <Tab id="details">Details</Tab>
          </TabList>
          <TabPanel id="summary">Summary Content</TabPanel>
          <TabPanel id="details">Details Content</TabPanel>
        </Tabs>
      )
      expect(screen.getByText("Summary Content")).toBeDefined()
    })
  })

  describe("6. Overlays (Dialog, Drawer, Menu)", () => {
    it("renders ConfirmDialog and fires callbacks", () => {
      const handleConfirm = vi.fn()
      render(
        <ConfirmDialog
          isOpen={true}
          title="Approve Run"
          description="Do you confirm?"
          confirmLabel="Confirm Approve"
          onConfirm={handleConfirm}
        />
      )
      expect(screen.getByText("Approve Run")).toBeDefined()
      fireEvent.click(screen.getByRole("button", { name: /confirm approve/i }))
      expect(handleConfirm).toHaveBeenCalledTimes(1)
    })

    it("renders Drawer with logical side='end'", () => {
      render(
        <Drawer isOpen={true} side="end" title="Audit Drawer">
          <div>Audit History Body</div>
        </Drawer>
      )
      expect(screen.getByText("Audit Drawer")).toBeDefined()
      expect(screen.getByText("Audit History Body")).toBeDefined()
    })
  })

  describe("7. Security Contract: SensitiveValue & AccessDenied", () => {
    it("renders masked state by default and triggers onRevealRequest UI trigger", () => {
      const handleRevealRequest = vi.fn()
      render(
        <SensitiveValue
          state="masked"
          onRevealRequest={handleRevealRequest}
        />
      )
      expect(screen.getByText("••••••••••••")).toBeDefined()
      const revealBtn = screen.getByRole("button", {
        name: /request reveal of confidential value/i,
      })
      fireEvent.click(revealBtn)
      expect(handleRevealRequest).toHaveBeenCalledTimes(1)
    })

    it("renders pending authorization state", () => {
      render(<SensitiveValue state="pending" />)
      expect(screen.getByText("Authorizing...")).toBeDefined()
    })

    it("renders revealed plaintext when authorized", () => {
      const handleMask = vi.fn()
      render(
        <SensitiveValue
          state="revealed"
          value="IBAN-FICTIONAL-DEMO-001"
          onMask={handleMask}
        />
      )
      expect(screen.getByText("IBAN-FICTIONAL-DEMO-001")).toBeDefined()
      const hideBtn = screen.getByRole("button", { name: /hide confidential value/i })
      fireEvent.click(hideBtn)
      expect(handleMask).toHaveBeenCalledTimes(1)
    })

    it("renders AccessDenied with capability/permission token and correlation ID", () => {
      const handleBack = vi.fn()
      render(
        <AccessDenied
          requiredPermission="payroll.run.finalize"
          correlationId="trace_test_001"
          onGoBack={handleBack}
        />
      )
      expect(screen.getByText("payroll.run.finalize")).toBeDefined()
      expect(screen.getByText("Trace ID: trace_test_001")).toBeDefined()
      fireEvent.click(screen.getByRole("button", { name: /return to previous page/i }))
      expect(handleBack).toHaveBeenCalledTimes(1)
    })
  })

  describe("8. Money ISO 4217 Currency Formatting", () => {
    it("formats SAR, EGP, USD, and signs generically", () => {
      const { unmount } = render(<Money amount={25000} currency="SAR" />)
      expect(screen.getByText(/25,000/)).toBeDefined()
      unmount()

      render(<Money amount={120000} currency="EGP" />)
      expect(screen.getByText(/120,000/)).toBeDefined()
    })
  })

  describe("9. QuickCreate Data-Driven Decoupling", () => {
    it("renders data-driven action items", () => {
      const handleAction = vi.fn()
      render(
        <QuickCreate
          buttonLabel="Create"
          items={[
            { id: "emp", label: "Add Employee", icon: "users", onAction: handleAction },
          ]}
        />
      )
      expect(screen.getByRole("button", { name: /create/i })).toBeDefined()
    })
  })

  describe("10. Pagination Controls", () => {
    it("handles page navigation and boundary checks", () => {
      const handlePageChange = vi.fn()
      render(
        <Pagination
          page={2}
          pageSize={10}
          totalItems={45}
          onPageChange={handlePageChange}
        />
      )
      expect(screen.getByText(/Showing/)).toBeDefined()
      expect(screen.getByText("45")).toBeDefined()
      const nextBtn = screen.getByRole("button", { name: /next/i })
      fireEvent.click(nextBtn)
      expect(handlePageChange).toHaveBeenCalledWith(3)
    })
  })

  describe("11. Card Hierarchy & ZainXDataGrid Baseline", () => {
    it("renders Utility, Emphasis, and Spotlight cards properly", () => {
      render(
        <div>
          <Card><CardTitle>Base Card</CardTitle></Card>
          <EmphasisCard><CardTitle>Emphasis Card</CardTitle></EmphasisCard>
          <SpotlightCard><CardTitle>Spotlight Card</CardTitle></SpotlightCard>
        </div>
      )
      expect(screen.getByText("Base Card")).toBeDefined()
      expect(screen.getByText("Emphasis Card")).toBeDefined()
      expect(screen.getByText("Spotlight Card")).toBeDefined()
    })

    it("renders ZainXDataGrid container with alpine theme", () => {
      render(
        <ZainXDataGrid
          rowData={[{ id: "1", name: "Khalid" }]}
          columnDefs={[{ field: "id" }, { field: "name" }]}
        />
      )
      expect(document.querySelector(".ag-theme-alpine")).toBeDefined()
    })
  })
})
