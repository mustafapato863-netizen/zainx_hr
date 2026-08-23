import * as React from "react"
import { DndContext, useDraggable, useDroppable } from "@dnd-kit/core"
import type { DragEndEvent } from "@dnd-kit/core"
import { cn } from "../../lib/utils"
import { Button } from "../Button/Button"
import { Icon } from "../Icon/Icon"

export interface ZainXKanbanItem {
  id: string
  title: string
  subtitle?: string
  columnId: string
  badge?: string
}

export interface ZainXKanbanColumn {
  id: string
  title: string
  itemIds: string[]
}

export interface ZainXKanbanProps {
  className?: string
  columns: ZainXKanbanColumn[]
  items: Record<string, ZainXKanbanItem>
  onItemMove?: (itemId: string, sourceColId: string, targetColId: string) => Promise<boolean> | boolean
  isRtl?: boolean
  readOnly?: boolean
}

/**
 * Draggable Card Component with Accessible Move Menu
 */
function KanbanCard({
  item,
  currentColumnIndex,
  totalColumns,
  onMoveRequested,
  readOnly,
}: {
  item: ZainXKanbanItem
  currentColumnIndex: number
  totalColumns: number
  onMoveRequested: (direction: "forward" | "backward") => void
  readOnly?: boolean
}) {
  const { attributes, listeners, setNodeRef, transform, isDragging } = useDraggable({
    id: item.id,
    disabled: readOnly,
  })

  const style = transform
    ? {
        transform: `translate3d(${transform.x}px, ${transform.y}px, 0)`,
      }
    : undefined

  return (
    <div
      ref={setNodeRef}
      style={style}
      {...listeners}
      {...attributes}
      className={cn(
        "group relative flex flex-col gap-1.5 rounded-md border border-border-default bg-surface p-3 shadow-2xs transition-all hover:border-border-hover hover:shadow-xs",
        isDragging && "opacity-50 ring-2 ring-primary z-50",
        !readOnly && "cursor-grab active:cursor-grabbing"
      )}
    >
      <div className="flex items-start justify-between gap-2">
        <h4 className="text-xs font-semibold text-text-primary">{item.title}</h4>
        {item.badge && (
          <span className="rounded bg-primary-subtle px-1.5 py-0.5 text-[10px] font-medium text-primary-subtle-text">
            {item.badge}
          </span>
        )}
      </div>

      {item.subtitle && <p className="text-[11px] text-text-tertiary">{item.subtitle}</p>}

      {/* Accessible Non-Drag Alternative Action Bar */}
      {!readOnly && (
        <div className="mt-1 flex items-center justify-end gap-1 opacity-0 group-hover:opacity-100 focus-within:opacity-100 transition-opacity">
          {currentColumnIndex > 0 && (
            <Button
              variant="ghost"
              size="xs"
              aria-label={`Move ${item.title} backward`}
              className="h-6 w-6 p-0"
              onPress={() => onMoveRequested("backward")}
            >
              <Icon name="arrow-left" size="xs" />
            </Button>
          )}
          {currentColumnIndex < totalColumns - 1 && (
            <Button
              variant="ghost"
              size="xs"
              aria-label={`Move ${item.title} forward`}
              className="h-6 w-6 p-0"
              onPress={() => onMoveRequested("forward")}
            >
              <Icon name="arrow-right" size="xs" />
            </Button>
          )}
        </div>
      )}
    </div>
  )
}

/**
 * Droppable Column Container
 */
function KanbanColumnContainer({
  column,
  columnIndex,
  totalColumns,
  items,
  onCardMove,
  readOnly,
}: {
  column: ZainXKanbanColumn
  columnIndex: number
  totalColumns: number
  items: Record<string, ZainXKanbanItem>
  onCardMove: (itemId: string, direction: "forward" | "backward") => void
  readOnly?: boolean
}) {
  const { setNodeRef, isOver } = useDroppable({
    id: column.id,
  })

  return (
    <div
      ref={setNodeRef}
      className={cn(
        "flex flex-col rounded-lg border border-border-default bg-surface-subtle/50 p-3 min-w-[240px] flex-1 min-h-[350px] transition-colors",
        isOver && "border-primary bg-primary-subtle/20"
      )}
    >
      <div className="mb-2.5 flex items-center justify-between">
        <h3 className="text-xs font-semibold uppercase tracking-wider text-text-secondary">
          {column.title}
        </h3>
        <span className="rounded-full bg-surface px-2 py-0.5 text-[10px] font-bold text-text-tertiary border border-border-subtle">
          {column.itemIds.length}
        </span>
      </div>

      <div className="flex flex-col gap-2 flex-1">
        {column.itemIds.map((itemId) => {
          const item = items[itemId]
          if (!item) return null
          return (
            <KanbanCard
              key={item.id}
              item={item}
              currentColumnIndex={columnIndex}
              totalColumns={totalColumns}
              onMoveRequested={(dir) => onCardMove(item.id, dir)}
              readOnly={readOnly}
            />
          )
        })}
      </div>
    </div>
  )
}

/**
 * ZainXKanban
 *
 * Encapsulates dnd-kit for column and stage transitions.
 *
 * ARCHITECTURAL MANDATE:
 * 1. UI drag is convenience; backend command authority owns domain state truth.
 * 2. If the backend mutation rejects or errors, the UI immediately rolls back.
 * 3. Every card provides accessible button controls for non-mouse / keyboard / AT users.
 */
export function ZainXKanban({
  className,
  columns: initialColumns,
  items: initialItems,
  onItemMove,
  isRtl = false,
  readOnly = false,
}: ZainXKanbanProps) {
  const [columns, setColumns] = React.useState(initialColumns)
  const [items] = React.useState(initialItems)

  React.useEffect(() => {
    setColumns(initialColumns)
  }, [initialColumns])

  const performTransition = async (itemId: string, sourceColId: string, targetColId: string) => {
    if (sourceColId === targetColId) return

    const previousColumns = [...columns]
    setColumns((prev) =>
      prev.map((col) => {
        if (col.id === sourceColId) {
          return { ...col, itemIds: col.itemIds.filter((id) => id !== itemId) }
        }
        if (col.id === targetColId) {
          return { ...col, itemIds: [...col.itemIds, itemId] }
        }
        return col
      })
    )

    if (onItemMove) {
      try {
        const success = await onItemMove(itemId, sourceColId, targetColId)
        if (!success) {
          setColumns(previousColumns)
        }
      } catch {
        setColumns(previousColumns)
      }
    }
  }

  const handleDragEnd = async (event: DragEndEvent) => {
    const { active, over } = event
    if (!over) return

    const itemId = String(active.id)
    const targetColId = String(over.id)

    const sourceCol = columns.find((c) => c.itemIds.includes(itemId))
    if (!sourceCol) return

    await performTransition(itemId, sourceCol.id, targetColId)
  }

  const handleAccessibleMove = async (itemId: string, direction: "forward" | "backward") => {
    const sourceColIndex = columns.findIndex((c) => c.itemIds.includes(itemId))
    if (sourceColIndex === -1) return

    const targetColIndex = direction === "forward" ? sourceColIndex + 1 : sourceColIndex - 1
    if (targetColIndex < 0 || targetColIndex >= columns.length) return

    const sourceCol = columns[sourceColIndex]
    const targetCol = columns[targetColIndex]

    await performTransition(itemId, sourceCol.id, targetCol.id)
  }

  return (
    <DndContext onDragEnd={handleDragEnd}>
      <div
        className={cn("flex flex-wrap gap-4 overflow-x-auto pb-4", className)}
        dir={isRtl ? "rtl" : "ltr"}
      >
        {columns.map((col, idx) => (
          <KanbanColumnContainer
            key={col.id}
            column={col}
            columnIndex={idx}
            totalColumns={columns.length}
            items={items}
            onCardMove={handleAccessibleMove}
            readOnly={readOnly}
          />
        ))}
      </div>
    </DndContext>
  )
}
