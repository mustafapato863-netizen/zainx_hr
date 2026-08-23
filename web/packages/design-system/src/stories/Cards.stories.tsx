import type { Meta, StoryObj } from "@storybook/react"
import { Card, CardHeader, CardTitle, CardDescription, CardContent, CardFooter } from "../components/Card/Card"
import { EmphasisCard } from "../components/Card/EmphasisCard"
import { SpotlightCard } from "../components/Card/SpotlightCard"
import { Button } from "../components/Button/Button"
import { Badge } from "../components/Badge/Badge"

const meta: Meta = {
  title: "Data/Card Hierarchy",
  tags: ["autodocs"],
}

export default meta

export const CardHierarchyMatrix: StoryObj = {
  render: () => (
    <div className="flex flex-col gap-6 max-w-2xl">
      <div className="space-y-2">
        <div className="text-xs font-semibold uppercase text-text-tertiary">
          1. Base Utility Card (Quiet, neutral, standard background)
        </div>
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <CardTitle>Standard Employee Profile</CardTitle>
              <Badge variant="neutral">Active</Badge>
            </div>
            <CardDescription>Department: Corporate Strategy & Operations</CardDescription>
          </CardHeader>
          <CardContent>
            <p className="text-text-secondary text-xs">
              Base card styling is calm and neutral with no artificial glow or distracting borders.
            </p>
          </CardContent>
          <CardFooter>
            <Button variant="secondary" size="xs">View Record</Button>
          </CardFooter>
        </Card>
      </div>

      <div className="space-y-2">
        <div className="text-xs font-semibold uppercase text-text-tertiary">
          2. Emphasis Card (Bordered and elevated for active tasks)
        </div>
        <EmphasisCard status="warning">
          <CardHeader>
            <div className="flex items-center justify-between">
              <CardTitle>Payroll Discrepancy Flagged</CardTitle>
              <Badge variant="warning" dot>Requires Action</Badge>
            </div>
            <CardDescription>Overtime hours exceed statutory weekly threshold of 48 hours</CardDescription>
          </CardHeader>
          <CardContent>
            <p className="text-xs text-text-secondary">
              Emphasis cards draw the operator's eye to high-priority workflows without visual chaos.
            </p>
          </CardContent>
          <CardFooter>
            <Button variant="danger" size="xs">Resolve Overtime</Button>
          </CardFooter>
        </EmphasisCard>
      </div>

      <div className="space-y-2">
        <div className="text-xs font-semibold uppercase text-text-tertiary">
          3. Spotlight Card (Reserved for AI Insights & Significant Operational Milestones)
        </div>
        <SpotlightCard badgeText="ZainX AI Assistant">
          <CardHeader>
            <CardTitle className="pe-24">Payroll Optimization Recommendation</CardTitle>
            <CardDescription>Automated Tax & GOSI Compliance Advisor</CardDescription>
          </CardHeader>
          <CardContent>
            <p className="text-xs text-text-secondary leading-relaxed">
              Based on the new statutory salary baseline, reallocating the housing allowance bracket for 18 staff members will optimize total employer contribution by <strong className="text-primary">SAR 14,200/mo</strong> while remaining 100% compliant with Saudi Labor Law.
            </p>
          </CardContent>
          <CardFooter>
            <Button variant="primary" size="xs">Apply Recommendation</Button>
          </CardFooter>
        </SpotlightCard>
      </div>
    </div>
  ),
}
