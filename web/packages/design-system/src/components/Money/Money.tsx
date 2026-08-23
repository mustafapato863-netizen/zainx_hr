import * as React from "react"
import { cn } from "../../lib/utils"

export interface MoneyProps extends React.HTMLAttributes<HTMLSpanElement> {
  /** Numeric monetary amount */
  amount: number
  /** ISO 4217 currency code (e.g. 'EGP', 'USD', 'SAR', 'AED', 'EUR') */
  currency: string
  /** BCP 47 language tag (e.g. 'en-US', 'ar-SA', 'ar-EG') */
  locale?: string
  /** Explicitly show positive sign ('+SAR 100') */
  showSign?: boolean
  /** Decimal places (default 2) */
  fractionDigits?: number
}

export function Money({
  className,
  amount,
  currency,
  locale = "en-US",
  showSign = false,
  fractionDigits = 2,
  ...props
}: MoneyProps) {
  const formatted = new Intl.NumberFormat(locale, {
    style: "currency",
    currency: currency.toUpperCase(),
    signDisplay: showSign ? "always" : "auto",
    minimumFractionDigits: fractionDigits,
    maximumFractionDigits: fractionDigits,
  }).format(amount)

  const isNegative = amount < 0
  const isPositive = amount > 0

  return (
    <span
      className={cn(
        "font-mono font-medium tracking-tight whitespace-nowrap",
        showSign && isPositive && "text-success",
        showSign && isNegative && "text-danger",
        className
      )}
      {...props}
    >
      {formatted}
    </span>
  )
}
