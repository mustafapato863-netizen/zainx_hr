import * as React from "react"
import { cn } from "../../lib/utils"

export interface AppShellProps extends React.HTMLAttributes<HTMLDivElement> {
  sidebar?: React.ReactNode
  topbar?: React.ReactNode
}

const AppShell = React.forwardRef<HTMLDivElement, AppShellProps>(
  ({ className, sidebar, topbar, children, ...props }, ref) => {
    return (
      <div
        ref={ref}
        className={cn(
          "flex h-screen w-full flex-col overflow-hidden bg-canvas text-text-primary md:flex-row",
          className
        )}
        {...props}
      >
        {/* Sidebar Container */}
        {sidebar && (
          <aside className="hidden md:flex md:w-64 md:flex-col md:flex-shrink-0 border-e border-border-default bg-surface-sidebar">
            {sidebar}
          </aside>
        )}

        {/* Main Content Viewport */}
        <div className="flex flex-1 flex-col overflow-hidden">
          {topbar && (
            <header className="flex h-14 flex-shrink-0 items-center justify-between border-b border-border-default bg-surface-topbar px-4 md:px-6">
              {topbar}
            </header>
          )}

          <main className="flex-1 overflow-y-auto bg-canvas p-4 md:p-6 lg:p-8">
            {children}
          </main>
        </div>
      </div>
    )
  }
)
AppShell.displayName = "AppShell"

export { AppShell }
