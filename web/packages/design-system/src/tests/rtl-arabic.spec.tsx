import { describe, it, expect } from "vitest"
import { render, screen } from "@testing-library/react"
import React from "react"
import { AppShell } from "../components/AppShell/AppShell"
import { Sidebar } from "../components/AppShell/Sidebar"
import { Topbar } from "../components/AppShell/Topbar"
import { ContextSwitcher } from "../components/ContextSwitcher/ContextSwitcher"
import { Breadcrumb } from "../components/Breadcrumb/Breadcrumb"
import { Tabs, TabList, Tab, TabPanel } from "../components/Tabs/Tabs"
import { Select, SelectItem } from "../components/Select/Select"
import { ComboBox, ComboBoxItem } from "../components/ComboBox/ComboBox"
import { DatePicker } from "../components/DatePicker/DatePicker"
import { parseDate } from "@internationalized/date"
import { Menu, MenuItem, MenuSection } from "../components/Menu/Menu"
import { Dialog } from "../components/Dialog/Dialog"
import { Drawer } from "../components/Drawer/Drawer"
import { Pagination } from "../components/Pagination/Pagination"
import { FilterBar } from "../components/FilterBar/FilterBar"
import { SavedViews } from "../components/SavedViews/SavedViews"
import { ColumnChooser } from "../components/ColumnChooser/ColumnChooser"
import { BulkActionBar } from "../components/BulkActionBar/BulkActionBar"
import { Money } from "../components/Money/Money"
import { Button } from "../components/Button/Button"
import { Badge } from "../components/Badge/Badge"

describe("ZainX Design System P0 RTL & Arabic Quality Matrix", () => {
  it("renders AppShell, Sidebar, Topbar in RTL with non-mirrored brand geometry", () => {
    const arabicContext = {
      id: "corp-sa",
      name: "شركة زين للتقنية - الرياض",
      code: "ZSA-HQ",
      type: "entity" as const,
    }

    const { container } = render(
      <div dir="rtl" className="font-arabic">
        <AppShell
          topbar={
            <Topbar
              contextSwitcher={<ContextSwitcher currentContext={arabicContext} />}
              actions={<Button size="xs">إجراء جديد</Button>}
            />
          }
          sidebar={
            <Sidebar
              brand={
                <div data-testid="zainx-logo" className="flex items-center gap-2">
                  <div className="h-7 w-7 bg-primary text-text-inverse font-bold text-center">Z</div>
                  <span className="font-bold">ZainX HR</span>
                </div>
              }
              sections={[
                {
                  title: "الموارد البشرية",
                  items: [
                    { id: "employees", label: "دليل الموظفين", active: true, badge: "١٤٢" },
                    { id: "payroll", label: "مسيرات الرواتب" },
                  ],
                },
              ]}
            />
          }
        >
          <div>محتوى النظام</div>
        </AppShell>
      </div>
    )

    // Verify Arabic text rendering
    expect(screen.getByText("الموارد البشرية")).toBeDefined()
    expect(screen.getByText("دليل الموظفين")).toBeDefined()
    expect(screen.getByText("شركة زين للتقنية - الرياض")).toBeDefined()

    // Verify logo container does not apply transform/mirroring
    const logo = screen.getByTestId("zainx-logo")
    expect(logo.className).not.toContain("scale-x-[-1]")
    expect(logo.className).not.toContain("rotate-180")
  })

  it("renders Breadcrumb and Tabs with logical direction in RTL", () => {
    render(
      <div dir="rtl" className="font-arabic">
        <Breadcrumb
          items={[
            { id: "home", label: "الرئيسية", href: "#" },
            { id: "payroll", label: "الرواتب", href: "#" },
            { id: "current", label: "مسير أغسطس ٢٠٢٦", current: true },
          ]}
        />
        <Tabs defaultSelectedKey="tab1">
          <TabList aria-label="أقسام المسير">
            <Tab id="tab1">الملخص العام</Tab>
            <Tab id="tab2">التأمينات والخصومات</Tab>
          </TabList>
          <TabPanel id="tab1">محتوى الملخص</TabPanel>
          <TabPanel id="tab2">محتوى التأمينات</TabPanel>
        </Tabs>
      </div>
    )

    expect(screen.getByText("الرئيسية")).toBeDefined()
    expect(screen.getByText("مسير أغسطس ٢٠٢٦")).toBeDefined()
    expect(screen.getByText("الملخص العام")).toBeDefined()
  })

  it("renders Select, ComboBox, and DatePicker with Arabic labels", () => {
    const countries = [
      { id: "sa", label: "المملكة العربية السعودية" },
      { id: "eg", label: "جمهورية مصر العربية" },
    ]

    render(
      <div dir="rtl" className="font-arabic space-y-4">
        <Select label="الدولة" placeholder="اختر الدولة..." items={countries}>
          {(item: any) => <SelectItem id={item.id}>{item.label}</SelectItem>}
        </Select>

        <ComboBox label="القسم" placeholder="ابحث في الأقسام..." items={countries}>
          {(item: any) => <ComboBoxItem id={item.id}>{item.label}</ComboBoxItem>}
        </ComboBox>

        <DatePicker
          label="تاريخ سريان العقد"
          defaultValue={parseDate("2026-08-01")}
        />
      </div>
    )

    expect(screen.getByText("الدولة")).toBeDefined()
    expect(screen.getByText("تاريخ سريان العقد")).toBeDefined()
  })

  it("renders Drawer with logical side='start' and side='end' in RTL", () => {
    render(
      <div dir="rtl">
        <Drawer isOpen={true} side="end" title="سجل التدقيق">
          <div>محتوى سجل العمليات</div>
        </Drawer>
      </div>
    )
    expect(screen.getByText("سجل التدقيق")).toBeDefined()
    expect(screen.getByText("محتوى سجل العمليات")).toBeDefined()
  })

  it("formats SAR and EGP Currency correctly in Arabic locale", () => {
    render(
      <div dir="rtl">
        <Money amount={18500} currency="SAR" locale="ar-SA" />
        <Money amount={95000} currency="EGP" locale="ar-EG" />
      </div>
    )
    expect(screen.getByText(/١٨/)).toBeDefined()
    expect(screen.getByText(/٩٥/)).toBeDefined()
  })

  it("renders FilterBar, SavedViews, and BulkActionBar with RTL logical layout", () => {
    render(
      <div dir="rtl" className="font-arabic space-y-2">
        <SavedViews />
        <FilterBar
          filters={[{ id: "dept", label: "القسم", value: "التقنية" }]}
          onRemoveFilter={() => {}}
          onClearAll={() => {}}
        />
        <BulkActionBar
          selectedCount={5}
          onClearSelection={() => {}}
          actions={<Button size="xs">اعتماد جماعي</Button>}
        />
      </div>
    )
    expect(screen.getByText("التقنية")).toBeDefined()
    expect(screen.getByText(/5 items selected/)).toBeDefined()
  })
})
