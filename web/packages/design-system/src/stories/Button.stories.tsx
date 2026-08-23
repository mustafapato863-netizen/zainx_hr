import type { Meta, StoryObj } from "@storybook/react"
import { Button } from "../components/Button/Button"
import { IconButton } from "../components/Button/IconButton"
import { Icon } from "../components/Icon/Icon"

const meta: Meta<typeof Button> = {
  title: "Forms/Button",
  component: Button,
  tags: ["autodocs"],
  argTypes: {
    variant: {
      control: "select",
      options: ["primary", "secondary", "tertiary", "ghost", "danger", "outline"],
    },
    size: {
      control: "select",
      options: ["xs", "sm", "md", "lg"],
    },
    disabled: { control: "boolean" },
    loading: { control: "boolean" },
  },
}

export default meta
type Story = StoryObj<typeof Button>

export const Primary: Story = {
  args: {
    children: "Approve Payroll Run",
    variant: "primary",
    size: "md",
  },
}

export const Secondary: Story = {
  args: {
    children: "Cancel / Dismiss",
    variant: "secondary",
    size: "md",
  },
}

export const Danger: Story = {
  args: {
    children: "Terminate Employee Record",
    variant: "danger",
    size: "md",
  },
}

export const LoadingState: Story = {
  args: {
    children: "Calculating Severance...",
    loading: true,
    variant: "primary",
  },
}

export const DisabledState: Story = {
  args: {
    children: "Locked Action",
    disabled: true,
    variant: "secondary",
  },
}

export const ArabicRTL: Story = {
  args: {
    children: "الموافقة على مسير الرواتب",
    variant: "primary",
    size: "md",
  },
  parameters: {
    globals: {
      direction: "rtl",
    },
  },
}

export const WithIcons: Story = {
  render: () => (
    <div className="flex flex-wrap items-center gap-3">
      <Button variant="primary">
        <Icon name="plus" size="xs" />
        <span>Add Employee</span>
      </Button>
      <Button variant="secondary">
        <Icon name="download" size="xs" />
        <span>Export GOSI Report</span>
      </Button>
      <IconButton aria-label="Edit employee" variant="secondary" size="icon-sm">
        <Icon name="edit" size="xs" />
      </IconButton>
      <IconButton aria-label="Delete employee" variant="danger" size="icon-sm">
        <Icon name="trash" size="xs" />
      </IconButton>
    </div>
  ),
}

export const AllVariantsMatrix: Story = {
  render: () => (
    <div className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center gap-3">
        <Button variant="primary">Primary</Button>
        <Button variant="secondary">Secondary</Button>
        <Button variant="tertiary">Tertiary</Button>
        <Button variant="ghost">Ghost</Button>
        <Button variant="danger">Danger</Button>
        <Button variant="outline">Outline</Button>
      </div>
      <div className="flex flex-wrap items-center gap-3">
        <Button size="xs">Extra Small</Button>
        <Button size="sm">Small</Button>
        <Button size="md">Medium</Button>
        <Button size="lg">Large</Button>
      </div>
    </div>
  ),
}
