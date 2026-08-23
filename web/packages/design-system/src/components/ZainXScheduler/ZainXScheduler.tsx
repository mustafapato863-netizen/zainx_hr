import * as React from "react"
import FullCalendar from "@fullcalendar/react"
import dayGridPlugin from "@fullcalendar/daygrid"
import timeGridPlugin from "@fullcalendar/timegrid"
import listPlugin from "@fullcalendar/list"
import interactionPlugin from "@fullcalendar/interaction"
import type { EventInput, EventClickArg, DateSelectArg } from "@fullcalendar/core"
import { cn } from "../../lib/utils"
import { Button } from "../Button/Button"
import { Icon } from "../Icon/Icon"

export interface ZainXSchedulerEvent extends EventInput {
  id: string
  title: string
  start: string | Date
  end?: string | Date
  allDay?: boolean
  category?: "shift" | "leave" | "interview" | "holiday" | "meeting"
  status?: "scheduled" | "confirmed" | "cancelled" | "pending"
  metadata?: Record<string, any>
}

export interface ZainXSchedulerProps {
  className?: string
  events?: ZainXSchedulerEvent[]
  initialView?: "dayGridMonth" | "timeGridWeek" | "timeGridDay" | "listWeek"
  locale?: string
  isRtl?: boolean
  height?: string | number
  onEventClick?: (arg: EventClickArg) => void
  onDateSelect?: (arg: DateSelectArg) => void
  onAddEventRequest?: () => void
  editable?: boolean
}

/**
 * ZainXScheduler
 *
 * Encapsulates FullCalendar standard open-source scheduling capabilities
 * behind a unified, accessible, and RTL-compliant component contract.
 *
 * ACCESSIBILITY & FALLBACK MANDATE:
 * Drag-and-drop is NEVER the sole interaction mechanism.
 * The accessible toolbar action provides a direct command/form alternative
 * for creating and managing schedule events via keyboard and screen readers.
 */
export function ZainXScheduler({
  className,
  events = [],
  initialView = "dayGridMonth",
  locale = "en",
  isRtl = false,
  height = "auto",
  onEventClick,
  onDateSelect,
  onAddEventRequest,
  editable = false,
}: ZainXSchedulerProps) {
  const calendarRef = React.useRef<FullCalendar | null>(null)
  const [viewMode, setViewMode] = React.useState<"dayGridMonth" | "timeGridWeek" | "timeGridDay" | "listWeek">(initialView)
  const [currentTitle, setCurrentTitle] = React.useState<string>("")

  const handlePrev = () => {
    calendarRef.current?.getApi().prev()
    setCurrentTitle(calendarRef.current?.getApi().view.title || "")
  }

  const handleNext = () => {
    calendarRef.current?.getApi().next()
    setCurrentTitle(calendarRef.current?.getApi().view.title || "")
  }

  const handleToday = () => {
    calendarRef.current?.getApi().today()
    setCurrentTitle(calendarRef.current?.getApi().view.title || "")
  }

  const handleViewChange = (mode: "dayGridMonth" | "timeGridWeek" | "timeGridDay" | "listWeek") => {
    setViewMode(mode)
    calendarRef.current?.getApi().changeView(mode)
    setCurrentTitle(calendarRef.current?.getApi().view.title || "")
  }

  React.useEffect(() => {
    if (calendarRef.current) {
      setCurrentTitle(calendarRef.current.getApi().view.title || "")
    }
  }, [])

  return (
    <div className={cn("rounded-lg border border-border-default bg-surface p-4 shadow-xs", className)}>
      {/* Accessible Header Controls */}
      <div className="mb-4 flex flex-wrap items-center justify-between gap-3 border-b border-border-subtle pb-3">
        <div className="flex items-center gap-2">
          <Button variant="secondary" size="xs" onPress={handleToday}>
            {locale.startsWith("ar") ? "اليوم" : "Today"}
          </Button>
          <div className="flex items-center gap-1">
            <Button
              variant="ghost"
              size="xs"
              aria-label={locale.startsWith("ar") ? "السابق" : "Previous"}
              onPress={handlePrev}
            >
              <Icon name="chevron-left" className={isRtl ? "rotate-180" : ""} size="xs" />
            </Button>
            <Button
              variant="ghost"
              size="xs"
              aria-label={locale.startsWith("ar") ? "التالي" : "Next"}
              onPress={handleNext}
            >
              <Icon name="chevron-right" className={isRtl ? "rotate-180" : ""} size="xs" />
            </Button>
          </div>
          <h2 className="text-base font-semibold text-text-primary ms-2" aria-live="polite">
            {currentTitle}
          </h2>
        </div>

        <div className="flex items-center gap-2">
          {/* Accessible Form/Action Trigger (Non-drag Alternative) */}
          {onAddEventRequest && (
            <Button variant="primary" size="xs" onPress={onAddEventRequest}>
              <Icon name="plus" size="xs" />
              {locale.startsWith("ar") ? "إضافة حدث" : "Add Event"}
            </Button>
          )}

          {/* View Mode Switcher */}
          <div className="flex rounded-md border border-border-default bg-surface-subtle p-0.5 text-xs">
            <button
              type="button"
              className={cn(
                "rounded px-2.5 py-1 font-medium transition-colors",
                viewMode === "dayGridMonth"
                  ? "bg-surface text-text-primary shadow-xs"
                  : "text-text-secondary hover:text-text-primary"
              )}
              onClick={() => handleViewChange("dayGridMonth")}
            >
              {locale.startsWith("ar") ? "شهر" : "Month"}
            </button>
            <button
              type="button"
              className={cn(
                "rounded px-2.5 py-1 font-medium transition-colors",
                viewMode === "timeGridWeek"
                  ? "bg-surface text-text-primary shadow-xs"
                  : "text-text-secondary hover:text-text-primary"
              )}
              onClick={() => handleViewChange("timeGridWeek")}
            >
              {locale.startsWith("ar") ? "أسبوع" : "Week"}
            </button>
            <button
              type="button"
              className={cn(
                "rounded px-2.5 py-1 font-medium transition-colors",
                viewMode === "timeGridDay"
                  ? "bg-surface text-text-primary shadow-xs"
                  : "text-text-secondary hover:text-text-primary"
              )}
              onClick={() => handleViewChange("timeGridDay")}
            >
              {locale.startsWith("ar") ? "يوم" : "Day"}
            </button>
            <button
              type="button"
              className={cn(
                "rounded px-2.5 py-1 font-medium transition-colors",
                viewMode === "listWeek"
                  ? "bg-surface text-text-primary shadow-xs"
                  : "text-text-secondary hover:text-text-primary"
              )}
              onClick={() => handleViewChange("listWeek")}
            >
              {locale.startsWith("ar") ? "قائمة" : "List"}
            </button>
          </div>
        </div>
      </div>

      {/* Calendar View Container */}
      <div className={cn("zainx-calendar-wrapper", isRtl && "fc-direction-rtl")}>
        <FullCalendar
          ref={calendarRef}
          plugins={[dayGridPlugin, timeGridPlugin, listPlugin, interactionPlugin]}
          initialView={initialView}
          headerToolbar={false} // Uses custom accessible header above
          events={events}
          editable={editable}
          selectable={true}
          locale={locale}
          direction={isRtl ? "rtl" : "ltr"}
          height={height}
          eventClick={onEventClick}
          select={onDateSelect}
          eventTimeFormat={{
            hour: "2-digit",
            minute: "2-digit",
            meridiem: "short",
            hour12: true,
          }}
        />
      </div>
    </div>
  )
}
