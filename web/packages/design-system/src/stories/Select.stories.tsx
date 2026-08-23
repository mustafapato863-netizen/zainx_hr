import type { Meta, StoryObj } from "@storybook/react"
import { Select, SelectItem } from "../components/Select/Select"
import { ComboBox, ComboBoxItem } from "../components/ComboBox/ComboBox"

const meta: Meta = {
  title: "Forms/Select & ComboBox",
  tags: ["autodocs"],
}

export default meta

const departments = [
  { id: "eng", label: "Software Engineering & Infrastructure", description: "Product dev & platform SRE" },
  { id: "fin", label: "Corporate Finance & Treasury", description: "Payroll, tax & financial audits" },
  { id: "hr", label: "People Operations & Talent Acquisition", description: "Recruitment, onboarding & compliance" },
  { id: "leg", label: "Legal, Risk & Regulatory Compliance", description: "Labor law & contracts", disabled: true },
  { id: "ops", label: "Operational Logistics", description: "Facilities & supply chain" },
]

const arabicDepartments = [
  { id: "eng", label: "هندسة البرمجيات والبنية التحتية", description: "تطوير المنتجات والمنصة" },
  { id: "fin", label: "المالية والخزينة المؤسسية", description: "الرواتب والضرائب والتدقيق" },
  { id: "hr", label: "الموارد البشرية والمواهب", description: "التوظيف والامتثال واللوائح" },
]

export const CanonicalSelect: StoryObj = {
  render: () => (
    <div className="flex flex-col gap-6 max-w-md">
      <Select
        label="Assigned Department"
        description="Select primary reporting unit"
        placeholder="Choose department..."
        items={departments}
      >
        {(item: any) => (
          <SelectItem key={item.id} id={item.id} textValue={item.label} isDisabled={item.disabled}>
            <div className="flex flex-col">
              <span className="font-medium text-xs text-text-primary">{item.label}</span>
              <span className="text-[11px] text-text-tertiary">{item.description}</span>
            </div>
          </SelectItem>
        )}
      </Select>

      <Select
        label="Invalid State Example"
        error="Department selection is mandatory for payroll allocation"
        placeholder="Select department..."
        items={departments}
      />

      <Select
        label="Disabled Select"
        disabled
        placeholder="Cannot change locked department"
        items={departments}
      />
    </div>
  ),
}

export const CanonicalComboBox: StoryObj = {
  render: () => (
    <div className="flex flex-col gap-6 max-w-md">
      <ComboBox
        label="Search Employee / Cost Center"
        description="Type to filter matching records dynamically"
        placeholder="Type name or code..."
        items={departments}
      >
        {(item: any) => (
          <ComboBoxItem key={item.id} id={item.id} textValue={item.label}>
            <div className="flex flex-col">
              <span className="font-medium text-xs text-text-primary">{item.label}</span>
              <span className="text-[11px] text-text-tertiary">{item.description}</span>
            </div>
          </ComboBoxItem>
        )}
      </ComboBox>
    </div>
  ),
}

export const ArabicSelectAndComboBox: StoryObj = {
  render: () => (
    <div className="flex flex-col gap-6 max-w-md font-arabic" dir="rtl">
      <Select
        label="القسم / الإدارة"
        description="اختر الإدارة الرئيسية للموظف"
        placeholder="اختر الإدارة..."
        items={arabicDepartments}
      >
        {(item: any) => (
          <SelectItem key={item.id} id={item.id} textValue={item.label}>
            <div className="flex flex-col">
              <span className="font-medium text-xs text-text-primary">{item.label}</span>
              <span className="text-[11px] text-text-tertiary">{item.description}</span>
            </div>
          </SelectItem>
        )}
      </Select>

      <ComboBox
        label="البحث عن مركز التكلفة"
        placeholder="ابحث بالاسم أو الرمز..."
        items={arabicDepartments}
      />
    </div>
  ),
  parameters: {
    globals: {
      direction: "rtl",
    },
  },
}
