import * as React from "react"
import type { Meta, StoryObj } from "@storybook/react"
import { Tooltip } from "../components/Tooltip/Tooltip"
import { Popover } from "../components/Popover/Popover"
import { Menu, MenuItem, MenuSection, MenuSeparator } from "../components/Menu/Menu"
import { Dialog, ConfirmDialog, DestructiveDialog } from "../components/Dialog/Dialog"
import { Drawer } from "../components/Drawer/Drawer"
import { Button } from "../components/Button/Button"
import { Icon } from "../components/Icon/Icon"

const meta: Meta = {
  title: "Overlays/Popups & Drawers",
  tags: ["autodocs"],
}

export default meta

export const TooltipsAndPopovers: StoryObj = {
  render: () => (
    <div className="flex flex-wrap items-center gap-6 p-8">
      <Tooltip content="Direct Deposit Bank Verification">
        <Button variant="secondary" size="sm">Hover for Tooltip</Button>
      </Tooltip>

      <Popover
        trigger={<Button variant="secondary" size="sm">Open Popover</Button>}
      >
        <div className="space-y-2 p-2">
          <h4 className="font-medium text-xs text-text-primary">Audit Log Meta</h4>
          <p className="text-xs text-text-secondary">
            Generated on 2026-08-24 by automated scheduler.
          </p>
        </div>
      </Popover>

      <Menu
        trigger={<Button variant="secondary" size="sm">Actions Menu <Icon name="chevron-down" size="xs" className="ms-1" /></Button>}
      >
        <MenuSection title="Document Actions">
          <MenuItem onAction={() => {}}>View PDF Preview</MenuItem>
          <MenuItem onAction={() => {}}>Download Encrypted Archive</MenuItem>
        </MenuSection>
        <MenuSeparator />
        <MenuSection title="Danger Zone">
          <MenuItem destructive onAction={() => {}}>Revoke Certificate</MenuItem>
        </MenuSection>
      </Menu>
    </div>
  ),
}

export const DialogsAndModals: StoryObj = {
  render: () => {
    const [isConfirmOpen, setIsConfirmOpen] = React.useState(false)
    const [isDestructiveOpen, setIsDestructiveOpen] = React.useState(false)
    const [isDrawerOpen, setIsDrawerOpen] = React.useState(false)

    return (
      <div className="flex flex-wrap gap-4 p-8">
        <Button variant="secondary" size="sm" onClick={() => setIsConfirmOpen(true)}>
          Open Confirm Dialog
        </Button>
        <ConfirmDialog
          isOpen={isConfirmOpen}
          onOpenChange={setIsConfirmOpen}
          title="Finalize Payroll Calculation"
          description="Are you sure you want to approve this monthly payroll batch? This will freeze attendance inputs."
          confirmLabel="Approve Batch"
          onConfirm={() => setIsConfirmOpen(false)}
        />

        <Button variant="danger" size="sm" onClick={() => setIsDestructiveOpen(true)}>
          Open Destructive Dialog
        </Button>
        <DestructiveDialog
          isOpen={isDestructiveOpen}
          onOpenChange={setIsDestructiveOpen}
          title="Delete Employee Record"
          description="This action cannot be undone. All historical timecards and payroll logs will be archived."
          confirmLabel="Permanent Delete"
          onConfirm={() => setIsDestructiveOpen(false)}
        />

        <Button variant="primary" size="sm" onClick={() => setIsDrawerOpen(true)}>
          Open Side Drawer (End)
        </Button>
        <Drawer
          isOpen={isDrawerOpen}
          onOpenChange={setIsDrawerOpen}
          side="end"
          title="Employee Audit History"
          description="Viewing complete temporal change records"
        >
          <div className="space-y-3 text-xs text-text-secondary">
            <div className="p-3 rounded bg-surface-subtle border border-border-default">
              <div className="font-semibold text-text-primary">Salary Updated</div>
              <div className="text-text-tertiary">2026-08-01 • Author: HR Operations</div>
            </div>
            <div className="p-3 rounded bg-surface-subtle border border-border-default">
              <div className="font-semibold text-text-primary">Department Transferred</div>
              <div className="text-text-tertiary">2026-06-15 • Author: VP Engineering</div>
            </div>
          </div>
        </Drawer>
      </div>
    )
  },
}
