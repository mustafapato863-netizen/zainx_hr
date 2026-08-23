# React / TypeScript Implementation Contract

## Principles

- primitives contain no HR/payroll business logic,
- enterprise components compose primitives,
- product components compose enterprise components,
- module pages compose product components and page patterns,
- semantic props are preferred over styling props,
- no `any` in public APIs,
- no business authorization logic only in the UI.

## Recommended component folder model

```text
src/design-system/
  tokens/
  primitives/
  forms/
  navigation/
  data/
  feedback/
  overlays/
  enterprise/
  product/
    people/
    attendance/
    leave/
    payroll/
    recruitment/
    approvals/
    admin/
    ai/
  signature/
  hooks/
  utilities/
```

## Example semantic types

```ts
export type SemanticTone =
  | "neutral"
  | "info"
  | "success"
  | "warning"
  | "danger"
  | "ai";

export type Density = "compact" | "standard" | "comfortable";
export type Direction = "ltr" | "rtl";
```

## Button

```ts
export interface ButtonProps {
  variant?: "primary" | "secondary" | "tertiary" | "ghost" | "danger";
  size?: "xs" | "sm" | "md" | "lg";
  loading?: boolean;
  disabled?: boolean;
  leadingIcon?: React.ReactNode;
  trailingIcon?: React.ReactNode;
  children: React.ReactNode;
}
```

## Money

```ts
export interface MoneyProps {
  amount: number | string;
  currency: string;
  locale?: string;
  variant?: "default" | "compact" | "variance" | "sensitive";
  masked?: boolean;
}
```

## SensitiveValue

```ts
export interface SensitiveValueProps {
  value?: string;
  maskedValue?: string;
  access: "allowed" | "masked" | "denied";
  revealable?: boolean;
  label: string;
}
```

## StatusBadge

```ts
export interface StatusBadgeProps {
  label: string;
  tone: SemanticTone;
  icon?: React.ReactNode;
  size?: "sm" | "md";
}
```

## SpotlightCard

```ts
export interface SpotlightCardProps {
  tone?: "brand" | "ai" | "success" | "warning" | "danger";
  interactiveSpotlight?: boolean;
  intensity?: "subtle" | "standard";
  children: React.ReactNode;
}
```

Do not expose arbitrary glow intensity.

## Product state rule

If a product state exists in backend domain contracts, map it to a known UI state/token.

Do not invent local status names for the same backend state.

## Testing

Every component should have:
- unit behavior tests when interactive,
- accessibility tests,
- keyboard tests,
- light/dark visual snapshots,
- RTL snapshots,
- reduced-motion snapshots for signature components.

High-risk product components require scenario tests.
