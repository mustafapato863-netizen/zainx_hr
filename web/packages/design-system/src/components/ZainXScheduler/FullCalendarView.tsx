import * as React from "react"
import FullCalendar from "@fullcalendar/react"
import dayGridPlugin from "@fullcalendar/daygrid"
import timeGridPlugin from "@fullcalendar/timegrid"
import listPlugin from "@fullcalendar/list"
import interactionPlugin from "@fullcalendar/interaction"
import type { EventClickArg, DateSelectArg } from "@fullcalendar/core"
import type { ZainXSchedulerEvent } from "./ZainXScheduler"

export interface FullCalendarViewProps {
  calendarRef: React.RefObject<FullCalendar | null>
  events: ZainXSchedulerEvent[]
  initialView: "dayGridMonth" | "timeGridWeek" | "timeGridDay" | "listWeek"
  locale: string
  isRtl: boolean
  height: string | number
  editable: boolean
  onEventClick?: (arg: EventClickArg) => void
  onDateSelect?: (arg: DateSelectArg) => void
}

export default function FullCalendarView({
  calendarRef,
  events,
  initialView,
  locale,
  isRtl,
  height,
  editable,
  onEventClick,
  onDateSelect,
}: FullCalendarViewProps) {
  return (
    <FullCalendar
      ref={calendarRef}
      plugins={[dayGridPlugin, timeGridPlugin, listPlugin, interactionPlugin]}
      initialView={initialView}
      headerToolbar={false}
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
  )
}
