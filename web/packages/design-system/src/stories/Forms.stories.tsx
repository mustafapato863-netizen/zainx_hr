import type { Meta, StoryObj } from "@storybook/react"
import { Input } from "../components/Input/Input"
import { Textarea } from "../components/Input/Textarea"
import { Field } from "../components/Input/Field"
import { NumberInput } from "../components/NumberInput/NumberInput"
import { Checkbox, CheckboxGroup } from "../components/Checkbox/Checkbox"
import { Radio, RadioGroup } from "../components/Radio/Radio"
import { Switch } from "../components/Switch/Switch"
import { EffectiveDate } from "../components/EffectiveDate/EffectiveDate"

const meta: Meta = {
  title: "Forms/Controls",
  tags: ["autodocs"],
}

export default meta

export const InputFields: StoryObj = {
  render: () => (
    <div className="flex flex-col gap-4 max-w-md">
      <Field label="Employee Full Name" description="Legal name as stated on passport/national ID" required>
        <Input placeholder="e.g. Sarah Al-Otaibi" />
      </Field>

      <Field label="Work Email Address" error="Please enter a valid corporate email" required>
        <Input placeholder="sarah@company.com" invalid defaultValue="sarah-invalid-email" />
      </Field>

      <Field label="National Identifier (Read-Only)">
        <Input defaultValue="1092837465" disabled />
      </Field>

      <Field label="Job Description / Responsibilities">
        <Textarea placeholder="Outline core operational duties and KPIs..." rows={3} />
      </Field>
    </div>
  ),
}

export const NumericAndCurrency: StoryObj = {
  render: () => (
    <div className="flex flex-col gap-4 max-w-md">
      <NumberInput
        label="Basic Monthly Salary (SAR)"
        defaultValue={18500}
        currency="SAR"
        description="Statutory GOSI base calculation amount"
      />

      <NumberInput
        label="Housing Allowance Rate (%)"
        defaultValue={25}
        minValue={0}
        maxValue={100}
      />
    </div>
  ),
}

export const SelectionControls: StoryObj = {
  render: () => (
    <div className="flex flex-col gap-6 max-w-md">
      <CheckboxGroup label="Statutory Deductions Applicable">
        <Checkbox defaultSelected>GOSI Saudi National Annuity (9.75%)</Checkbox>
        <Checkbox defaultSelected>SANED Unemployment Insurance (0.75%)</Checkbox>
        <Checkbox isIndeterminate>Occupational Hazards Coverage (2.00%)</Checkbox>
        <Checkbox isDisabled>Exempt Foreign Worker Levy</Checkbox>
      </CheckboxGroup>

      <RadioGroup label="Employment Contract Type" defaultValue="indefinite">
        <Radio value="indefinite">Indefinite Duration Contract</Radio>
        <Radio value="fixed">Fixed Term (12-24 Months)</Radio>
        <Radio value="temporary" isDisabled>Seasonal / Project Worker</Radio>
      </RadioGroup>

      <div className="flex flex-col gap-2">
        <label className="text-sm font-medium text-text-primary">System Preferences</label>
        <Switch defaultSelected>Enable Automated Overtime Approvals</Switch>
        <Switch>Multi-Factor Authentication on Salary Reveal</Switch>
      </div>

      <EffectiveDate label="Contract Amendment Effective Date" />
    </div>
  ),
}

export const ArabicFormFields: StoryObj = {
  render: () => (
    <div className="flex flex-col gap-4 max-w-md font-arabic" dir="rtl">
      <Field label="الاسم الكامل للموظف" description="الاسم القانوني كما هو مدون في الهوية الوطنية" required>
        <Input placeholder="مثال: سارة العتيبي" />
      </Field>

      <Field label="البريد الإلكتروني للعمل" error="الرجاء إدخال بريد إلكتروني صالح">
        <Input defaultValue="wrong-email" invalid />
      </Field>

      <RadioGroup label="نوع العقد" defaultValue="saudi">
        <Radio value="saudi">عقد غير محدد المدة (سعودي)</Radio>
        <Radio value="expat">عقد محدد المدة</Radio>
      </RadioGroup>

      <Switch defaultSelected>تفعيل الإشعارات الفورية للرواتب</Switch>
    </div>
  ),
  parameters: {
    globals: {
      direction: "rtl",
    },
  },
}
