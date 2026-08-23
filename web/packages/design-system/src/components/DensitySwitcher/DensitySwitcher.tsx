import * as React from "react"
import { cn } from "../../lib/utils"
import { Icon } from "../Icon/Icon"
import { Menu, MenuItem, MenuSection } from "../Menu/Menu"

export type DensityType = "compact" | "standard" | "comfortable"

export interface DensitySwitcherProps {
  className?: string
  density: DensityType
  onChange: (density: DensityType) => void
}

export function DensitySwitcher({
  className,
  density = "standard",
  onChange,
}: DensitySwitcherProps) {
  const densityLabels = {
    compact: "Compact (32px)",
    standard: "Standard (40px)",
    comfortable: "Comfortable (48px)",
  }

  return (
    <div className={cn("inline-flex items-center", className)}>
      <Menu
        trigger={
          <button
            type="button"
            className="flex items-center gap-1.5 rounded-md border border-border-default bg-surface px-2.5 py-1 text-xs font-medium text-text-primary hover:bg-surface-subtle focus:outline-none focus:ring-1 focus:ring-primary shadow-xs"
            title="Adjust table row height"
          >
            <Icon name="sliders" size="xs" className="text-text-tertiary" />
            <span className="capitalize">{density}</span>
            <Icon name="chevron-down" size="xs" className="text-text-tertiary opacity-70" />
          </button>
        }
      >
        <MenuSection title="Row Density">
          {(["compact", "standard", "comfortable"] as DensityType[]).map((d) => (
            <MenuItem key={d} onAction={() => onChange(d)}>
              <div className="flex w-full items-center justify-between gap-4">
                <span>{densityLabels[d]}</span>
                {d === density && <Icon name="check" size="xs" className="text-primary" />}
              </div>
            </MenuItem>
          ))}
        </MenuSection>
      </Menu>
    </div>
  )
}
