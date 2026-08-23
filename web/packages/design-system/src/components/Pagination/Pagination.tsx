import * as React from "react"
import { cn } from "../../lib/utils"
import { Button } from "../Button/Button"
import { Icon } from "../Icon/Icon"

export interface PaginationProps {
  className?: string
  page: number
  pageSize: number
  totalItems: number
  onPageChange: (page: number) => void
  onPageSizeChange?: (pageSize: number) => void
  pageSizeOptions?: number[]
}

export function Pagination({
  className,
  page,
  pageSize,
  totalItems,
  onPageChange,
  onPageSizeChange,
  pageSizeOptions = [10, 25, 50, 100],
}: PaginationProps) {
  const totalPages = Math.max(1, Math.ceil(totalItems / pageSize))
  const startItem = totalItems === 0 ? 0 : (page - 1) * pageSize + 1
  const endItem = Math.min(page * pageSize, totalItems)

  return (
    <div
      className={cn(
        "flex flex-wrap items-center justify-between gap-4 py-3 text-xs text-text-secondary select-none",
        className
      )}
    >
      <div className="flex items-center gap-2">
        <span>
          Showing <span className="font-semibold text-text-primary">{startItem}</span> to{" "}
          <span className="font-semibold text-text-primary">{endItem}</span> of{" "}
          <span className="font-semibold text-text-primary">{totalItems}</span> results
        </span>

        {onPageSizeChange && (
          <div className="ms-4 flex items-center gap-1.5">
            <span>Rows per page:</span>
            <select
              value={pageSize}
              onChange={(e) => onPageSizeChange(Number(e.target.value))}
              className="rounded-md border border-border-default bg-surface px-2 py-1 text-xs text-text-primary focus:outline-none focus:ring-1 focus:ring-primary"
            >
              {pageSizeOptions.map((opt) => (
                <option key={opt} value={opt}>
                  {opt}
                </option>
              ))}
            </select>
          </div>
        )}
      </div>

      <div className="flex items-center gap-1.5">
        <Button
          variant="secondary"
          size="xs"
          disabled={page <= 1}
          onClick={() => onPageChange(page - 1)}
          aria-label="Previous Page"
        >
          <Icon name="chevron-left" size="xs" />
          <span>Previous</span>
        </Button>

        <span className="px-2 font-medium text-text-primary">
          Page {page} of {totalPages}
        </span>

        <Button
          variant="secondary"
          size="xs"
          disabled={page >= totalPages}
          onClick={() => onPageChange(page + 1)}
          aria-label="Next Page"
        >
          <span>Next</span>
          <Icon name="chevron-right" size="xs" />
        </Button>
      </div>
    </div>
  )
}
