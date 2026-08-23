import * as React from "react"
import { EmptyState } from "./EmptyState"
import { Icon } from "../Icon/Icon"
import { Button } from "../Button/Button"

export interface NoResultsProps {
  className?: string
  title?: string
  description?: string
  onClearFilters?: () => void
}

export function NoResults({
  className,
  title = "No matching records",
  description = "No results match your search and filter criteria. Try adjusting your filters.",
  onClearFilters,
}: NoResultsProps) {
  return (
    <EmptyState
      className={className}
      icon={<Icon name="search" size="lg" className="text-text-tertiary" />}
      title={title}
      description={description}
      action={
        onClearFilters ? (
          <Button variant="secondary" size="sm" onClick={onClearFilters}>
            Clear all filters
          </Button>
        ) : undefined
      }
    />
  )
}
