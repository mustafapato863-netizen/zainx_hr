import { describe, it, expect } from "vitest"
import { render } from "@testing-library/react"
import React from "react"
import { axe } from "vitest-axe"
import * as matchers from "vitest-axe/matchers"
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
import { Tabs, TabList, Tab, TabPanel } from "../components/Tabs/Tabs"
import { Badge } from "../components/Badge/Badge"
import { Tag } from "../components/Tag/Tag"
import { Alert, Banner } from "../components/Alert/Alert"
import { Pagination } from "../components/Pagination/Pagination"
import { Money } from "../components/Money/Money"
import { SensitiveValue } from "../components/SensitiveValue/SensitiveValue"
import { Card, CardHeader, CardTitle, CardContent } from "../components/Card/Card"
import { EmphasisCard } from "../components/Card/EmphasisCard"
import { SpotlightCard } from "../components/Card/SpotlightCard"
import { Breadcrumb } from "../components/Breadcrumb/Breadcrumb"
import { PageHeader } from "../components/PageHeader/PageHeader"
import { SectionHeader } from "../components/SectionHeader/SectionHeader"
import { AccessDenied } from "../components/AccessDenied/AccessDenied"
import { Table, TableHeader, TableRow, TableHead, TableBody, TableCell } from "../components/Table/Table"

expect.extend(matchers)

describe("ZainX Design System P0 Automated Axe Accessibility Audit (WCAG AA)", () => {
  it("Button & IconButton pass axe audit", async () => {
    const { container } = render(
      <div>
        <Button variant="primary">Authorize Batch</Button>
        <IconButton aria-label="Notifications">
          <Icon name="bell" size="sm" />
        </IconButton>
      </div>
    )
    const results = await axe(container)
    expect(results).toHaveNoViolations()
  })

  it("Field, Input, Textarea with associations pass axe audit", async () => {
    const { container } = render(
      <Field label="National Identifier" description="Official government ID" required>
        <Input placeholder="Enter 10-digit ID" />
      </Field>
    )
    const results = await axe(container)
    expect(results).toHaveNoViolations()
  })

  it("Field with FieldError passes axe audit", async () => {
    const { container } = render(
      <Field label="Work Email" error="Invalid domain format" required>
        <Input invalid defaultValue="invalid-email" />
      </Field>
    )
    const results = await axe(container)
    expect(results).toHaveNoViolations()
  })

  it("Checkbox and CheckboxGroup pass axe audit", async () => {
    const { container } = render(
      <CheckboxGroup label="Statutory Benefits">
        <Checkbox defaultSelected>GOSI Annuity</Checkbox>
        <Checkbox isIndeterminate>Saned Unemployment</Checkbox>
      </CheckboxGroup>
    )
    const results = await axe(container)
    expect(results).toHaveNoViolations()
  })

  it("RadioGroup & Radio pass axe audit", async () => {
    const { container } = render(
      <RadioGroup label="Contract Type" defaultValue="full-time">
        <Radio value="full-time">Full Time</Radio>
        <Radio value="part-time">Part Time</Radio>
      </RadioGroup>
    )
    const results = await axe(container)
    expect(results).toHaveNoViolations()
  })

  it("Switch passes axe audit", async () => {
    const { container } = render(<Switch>Overtime Auto-Approval</Switch>)
    const results = await axe(container)
    expect(results).toHaveNoViolations()
  })

  it("Select passes axe audit", async () => {
    const items = [
      { id: "sa", label: "Saudi Arabia" },
      { id: "eg", label: "Egypt" },
    ]
    const { container } = render(
      <Select label="Country" placeholder="Choose Country" items={items}>
        {(item: any) => <SelectItem id={item.id}>{item.label}</SelectItem>}
      </Select>
    )
    const results = await axe(container)
    expect(results).toHaveNoViolations()
  })

  it("ComboBox passes axe audit", async () => {
    const items = [
      { id: "eng", label: "Engineering" },
      { id: "fin", label: "Finance" },
    ]
    const { container } = render(
      <ComboBox label="Department" placeholder="Search departments..." items={items}>
        {(item: any) => <ComboBoxItem id={item.id}>{item.label}</ComboBoxItem>}
      </ComboBox>
    )
    const results = await axe(container)
    expect(results).toHaveNoViolations()
  })

  it("Tabs pass axe audit", async () => {
    const { container } = render(
      <Tabs defaultSelectedKey="summary">
        <TabList aria-label="Payroll Navigation">
          <Tab id="summary">Summary</Tab>
          <Tab id="details">Details</Tab>
        </TabList>
        <TabPanel id="summary">Summary content</TabPanel>
        <TabPanel id="details">Details content</TabPanel>
      </Tabs>
    )
    const results = await axe(container)
    expect(results).toHaveNoViolations()
  })

  it("Alerts and Banners pass axe audit", async () => {
    const { container } = render(
      <div>
        <Banner variant="warning">Scheduled System Maintenance</Banner>
        <Alert variant="info" title="Direct Deposit Notice">
          Batch processing scheduled for 18:00 UTC.
        </Alert>
      </div>
    )
    const results = await axe(container)
    expect(results).toHaveNoViolations()
  })

  it("Pagination passes axe audit", async () => {
    const { container } = render(
      <Pagination page={1} pageSize={10} totalItems={40} onPageChange={() => {}} />
    )
    const results = await axe(container)
    expect(results).toHaveNoViolations()
  })

  it("SensitiveValue and Money pass axe audit", async () => {
    const { container } = render(
      <div>
        <Money amount={15000} currency="SAR" />
        <SensitiveValue state="masked" onRevealRequest={() => {}} />
      </div>
    )
    const results = await axe(container)
    expect(results).toHaveNoViolations()
  })

  it("Card hierarchy passes axe audit", async () => {
    const { container } = render(
      <div>
        <Card><CardTitle>Utility</CardTitle><CardContent>Content</CardContent></Card>
        <EmphasisCard><CardTitle>Emphasis</CardTitle><CardContent>Content</CardContent></EmphasisCard>
        <SpotlightCard><CardTitle>Spotlight</CardTitle><CardContent>Content</CardContent></SpotlightCard>
      </div>
    )
    const results = await axe(container)
    expect(results).toHaveNoViolations()
  })

  it("AccessDenied passes axe audit", async () => {
    const { container } = render(
      <AccessDenied
        requiredPermission="payroll.run.finalize"
        correlationId="trace-12345"
      />
    )
    const results = await axe(container)
    expect(results).toHaveNoViolations()
  })

  it("Semantic Table passes axe audit", async () => {
    const { container } = render(
      <Table>
        <TableHeader>
          <TableRow>
            <TableHead>Employee ID</TableHead>
            <TableHead>Name</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          <TableRow>
            <TableCell>EMP-001</TableCell>
            <TableCell>Khalid Al-Mansoor</TableCell>
          </TableRow>
        </TableBody>
      </Table>
    )
    const results = await axe(container)
    expect(results).toHaveNoViolations()
  })
})
