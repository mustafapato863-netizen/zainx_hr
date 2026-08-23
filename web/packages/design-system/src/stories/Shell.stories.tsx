import * as React from "react"
import type { Meta, StoryObj } from "@storybook/react"
import { AppShell } from "../components/AppShell/AppShell"
import { Sidebar } from "../components/AppShell/Sidebar"
import { Topbar } from "../components/AppShell/Topbar"
import { ContextSwitcher } from "../components/ContextSwitcher/ContextSwitcher"
import { Breadcrumb } from "../components/Breadcrumb/Breadcrumb"
import { Tabs, TabList, Tab, TabPanel } from "../components/Tabs/Tabs"
import { PageHeader } from "../components/PageHeader/PageHeader"
import { PageToolbar } from "../components/PageToolbar/PageToolbar"
import { QuickCreate } from "../components/QuickCreate/QuickCreate"
import { Button } from "../components/Button/Button"
import { Badge } from "../components/Badge/Badge"
import { Icon } from "../components/Icon/Icon"

const meta: Meta = {
  title: "Shell/Navigation & Layout",
  tags: ["autodocs"],
}

export default meta

export const FullAppShellDemo: StoryObj = {
  render: () => {
    return (
      <div className="h-[650px] w-full border border-border-default rounded-xl overflow-hidden shadow-2xl">
        <AppShell
          topbar={
            <Topbar
              contextSwitcher={<ContextSwitcher />}
              actions={
                <div className="flex items-center gap-2">
                  <QuickCreate
                    items={[
                      { id: "emp", label: "New Employee Record", icon: "users", permission: "people.employee.create" },
                      { id: "leave", label: "Submit Leave Request", icon: "calendar", permission: "leave.request.create" },
                      { id: "payroll", label: "Initiate Payroll Batch", icon: "dollar-sign", permission: "payroll.run.create" },
                    ]}
                  />
                  <Button variant="ghost" size="icon-sm" aria-label="Notifications">
                    <Icon name="bell" size="sm" />
                  </Button>
                </div>
              }
              user={
                <div className="flex items-center gap-2 ps-2 border-s border-border-default">
                  <div className="flex h-7 w-7 items-center justify-center rounded-full bg-primary text-text-inverse text-xs font-semibold">
                    MA
                  </div>
                  <div className="hidden sm:flex flex-col text-start">
                    <span className="text-xs font-semibold text-text-primary leading-tight">Mustafa A.</span>
                    <span className="text-[10px] text-text-tertiary">HR Admin</span>
                  </div>
                </div>
              }
            />
          }
          sidebar={
            <Sidebar
              brand={
                <div className="flex items-center gap-2">
                  <div className="flex h-7 w-7 items-center justify-center rounded-lg bg-primary text-text-inverse font-bold text-sm">
                    Z
                  </div>
                  <span className="font-bold tracking-tight text-sm text-text-primary">
                    ZainX <span className="text-primary font-normal">HR</span>
                  </span>
                </div>
              }
              sections={[
                {
                  title: "Core HR",
                  items: [
                    { id: "dash", label: "Dashboard", icon: "grid", active: true },
                    { id: "people", label: "Employees & Directory", icon: "users", badge: "142" },
                    { id: "org", label: "Organization Chart", icon: "building" },
                  ],
                },
                {
                  title: "Operations",
                  items: [
                    { id: "payroll", label: "Payroll Processing", icon: "dollar-sign", badge: "Pending" },
                    { id: "time", label: "Time & Attendance", icon: "clock" },
                    { id: "leave", label: "Leave Approvals", icon: "calendar" },
                  ],
                },
              ]}
              footer={
                <div className="flex items-center justify-between text-xs text-text-tertiary px-2">
                  <span>v2.0 Platform Kernel</span>
                  <Badge variant="success" size="sm">Online</Badge>
                </div>
              }
            />
          }
        >
          <PageHeader
            title="August 2026 Payroll Execution"
            subtitle="Review employee compensation, statutory GOSI deductions, and WPS compliance before batch disbursement."
            badge={<Badge variant="primary" dot>Run in Progress</Badge>}
            breadcrumbs={
              <Breadcrumb
                items={[
                  { id: "home", label: "Home", href: "#" },
                  { id: "payroll", label: "Payroll", href: "#" },
                  { id: "current", label: "August 2026 Run", current: true },
                ]}
              />
            }
            actions={
              <>
                <Button variant="secondary">
                  <Icon name="download" size="xs" />
                  <span>Export SAMA File</span>
                </Button>
                <Button variant="primary">
                  <Icon name="check" size="xs" />
                  <span>Authorize Disbursement</span>
                </Button>
              </>
            }
          />

          <PageToolbar
            left={
              <div className="flex items-center gap-2">
                <Button variant="secondary" size="xs">
                  <Icon name="filter" size="xs" />
                  <span>Filter by Branch</span>
                </Button>
              </div>
            }
            right={
              <span className="text-xs text-text-tertiary">
                Showing 142 of 142 Active Employees
              </span>
            }
          />

          <Tabs defaultSelectedKey="summary">
            <TabList>
              <Tab id="summary">Executive Summary</Tab>
              <Tab id="employees">Detailed Payroll Sheet</Tab>
              <Tab id="gosi">GOSI & Deductions</Tab>
              <Tab id="audit">Audit Log</Tab>
            </TabList>
            <TabPanel id="summary">
              <div className="grid grid-cols-1 md:grid-cols-3 gap-4 pt-2">
                <div className="p-4 rounded-xl border border-border-default bg-surface shadow-xs">
                  <div className="text-xs text-text-secondary">Gross Payroll Budget</div>
                  <div className="text-2xl font-bold font-mono text-text-primary mt-1">SAR 1,842,500.00</div>
                </div>
                <div className="p-4 rounded-xl border border-border-default bg-surface shadow-xs">
                  <div className="text-xs text-text-secondary">Total Statutory GOSI</div>
                  <div className="text-2xl font-bold font-mono text-text-primary mt-1">SAR 182,410.00</div>
                </div>
                <div className="p-4 rounded-xl border border-border-default bg-surface shadow-xs">
                  <div className="text-xs text-text-secondary">Net Disbursement Required</div>
                  <div className="text-2xl font-bold font-mono text-primary mt-1">SAR 1,660,090.00</div>
                </div>
              </div>
            </TabPanel>
          </Tabs>
        </AppShell>
      </div>
    )
  },
}
