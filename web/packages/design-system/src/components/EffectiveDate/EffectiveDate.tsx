import * as React from "react"
import { cn } from "../../lib/utils"
import { Button } from "../Button/Button"
import { Icon } from "../Icon/Icon"

export interface EffectiveDatePreset {
  id: string
  label: string
  getDate: () => string
}

const defaultPresets: EffectiveDatePreset[] = [
  {
    id: "today",
    label: "Immediate (Today)",
    getDate: () => new Date().toISOString().split("T")[0],
  },
  {
    id: "next-month",
    label: "1st of Next Month",
    getDate: () => {
      const now = new Date()
      const nextMonth = new Date(now.getFullYear(), now.getMonth() + 1, 1)
      return nextMonth.toISOString().split("T")[0]
    },
  },
]

export interface EffectiveDateProps {
  className?: string
  value?: string
  selectedPresetId?: string
  /** Injected custom presets (e.g. Next Payroll Cycle provided by Payroll module) */
  presets?: EffectiveDatePreset[]
  onChange?: (date: string, presetId: string) => void
  disabled?: boolean
  label?: string
}

export function EffectiveDate({
  className,
  value,
  selectedPresetId = "today",
  presets = defaultPresets,
  onChange,
  disabled = false,
  label = "Effective Date",
}: EffectiveDateProps) {
  const [activePresetId, setActivePresetId] = React.useState<string>(selectedPresetId)
  const [dateValue, setDateValue] = React.useState<string>(
    value || new Date().toISOString().split("T")[0]
  )

  const handlePresetSelect = (presetId: string) => {
    setActivePresetId(presetId)
    if (presetId === "custom") {
      onChange?.(dateValue, "custom")
      return
    }

    const preset = presets.find((p) => p.id === presetId)
    if (preset) {
      const calculated = preset.getDate()
      setDateValue(calculated)
      onChange?.(calculated, presetId)
    }
  }

  return (
    <div className={cn("flex flex-col gap-2 rounded-lg border border-border-default bg-surface p-3", className)}>
      <div className="flex items-center justify-between">
        <label className="text-xs font-semibold uppercase tracking-wider text-text-secondary">
          {label}
        </label>
        <span className="inline-flex items-center gap-1 text-xs font-medium text-primary">
          <Icon name="clock" size="xs" />
          {dateValue}
        </span>
      </div>

      <div className="flex flex-wrap gap-1.5">
        {presets.map((p) => (
          <Button
            key={p.id}
            type="button"
            size="xs"
            variant={activePresetId === p.id ? "primary" : "secondary"}
            disabled={disabled}
            onClick={() => handlePresetSelect(p.id)}
          >
            {p.label}
          </Button>
        ))}
        <Button
          type="button"
          size="xs"
          variant={activePresetId === "custom" ? "primary" : "secondary"}
          disabled={disabled}
          onClick={() => handlePresetSelect("custom")}
        >
          Custom Date
        </Button>
      </div>

      {activePresetId === "custom" && (
        <div className="mt-1">
          <input
            type="date"
            value={dateValue}
            disabled={disabled}
            onChange={(e) => {
              setDateValue(e.target.value)
              onChange?.(e.target.value, "custom")
            }}
            className="flex h-8 w-full rounded-md border border-border-default bg-surface-input px-2 py-1 text-xs text-text-primary focus:outline-none focus:ring-2 focus:ring-primary"
          />
        </div>
      )}
    </div>
  )
}
