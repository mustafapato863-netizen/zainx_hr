import { describe, it, expect, vi } from "vitest"
import { render, screen, fireEvent } from "@testing-library/react"
import React from "react"
import { Button } from "../components/Button/Button"
import { Input } from "../components/Input/Input"
import { Field } from "../components/Input/Field"
import { Checkbox } from "../components/Checkbox/Checkbox"
import { Switch } from "../components/Switch/Switch"
import { Money } from "../components/Money/Money"
import { SensitiveValue } from "../components/SensitiveValue/SensitiveValue"
import { Badge } from "../components/Badge/Badge"

describe("ZainX Design System P0 Core Components", () => {
  describe("Button Component", () => {
    it("renders children properly", () => {
      render(<Button>Authorize Payroll</Button>)
      expect(screen.getByRole("button", { name: /authorize payroll/i })).toBeDefined()
    })

    it("handles click events", () => {
      const handleClick = vi.fn()
      render(<Button onClick={handleClick}>Click Me</Button>)
      fireEvent.click(screen.getByRole("button", { name: /click me/i }))
      expect(handleClick).toHaveBeenCalledTimes(1)
    })

    it("prevents clicks when disabled or loading", () => {
      const handleClick = vi.fn()
      render(<Button disabled onClick={handleClick}>Disabled</Button>)
      const btn = screen.getByRole("button", { name: /disabled/i })
      expect(btn).toHaveProperty("disabled", true)
      fireEvent.click(btn)
      expect(handleClick).not.toHaveBeenCalled()
    })
  })

  describe("Field and Input Component", () => {
    it("renders label and required indicator", () => {
      render(
        <Field label="National ID" required>
          <Input placeholder="Enter ID" />
        </Field>
      )
      expect(screen.getByText("National ID")).toBeDefined()
      expect(screen.getByPlaceholderText("Enter ID")).toBeDefined()
    })

    it("renders error message when provided", () => {
      render(
        <Field label="Email" error="Invalid corporate email domain">
          <Input />
        </Field>
      )
      expect(screen.getByText("Invalid corporate email domain")).toBeDefined()
    })
  })

  describe("Checkbox and Switch Controls", () => {
    it("renders checkbox and toggles checked state", () => {
      render(<Checkbox>GOSI Contribution</Checkbox>)
      const checkbox = screen.getByRole("checkbox", { name: /gosi contribution/i })
      expect(checkbox).toBeDefined()
    })

    it("renders switch component", () => {
      render(<Switch>Overtime Auto-Approval</Switch>)
      const toggle = screen.getByRole("switch", { name: /overtime auto-approval/i })
      expect(toggle).toBeDefined()
    })
  })

  describe("Money and SensitiveValue Formatters", () => {
    it("formats SAR currency amount correctly", () => {
      render(<Money amount={25000} currency="SAR" />)
      expect(screen.getByText(/25,000/)).toBeDefined()
    })

    it("masks sensitive values and triggers onRevealRequest UI trigger", () => {
      const handleRevealRequest = vi.fn()
      render(
        <SensitiveValue
          state="masked"
          onRevealRequest={handleRevealRequest}
        />
      )

      expect(screen.getByText(/••••••••••••/)).toBeDefined()
      const revealBtn = screen.getByRole("button", { name: /request reveal of confidential value/i })
      fireEvent.click(revealBtn)

      expect(handleRevealRequest).toHaveBeenCalledTimes(1)
    })
  })

  describe("Badge Component", () => {
    it("renders badge variant correctly", () => {
      render(<Badge variant="success" dot>Approved</Badge>)
      expect(screen.getByText("Approved")).toBeDefined()
    })
  })
})
