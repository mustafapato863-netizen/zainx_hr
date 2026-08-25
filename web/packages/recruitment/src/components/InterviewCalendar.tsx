import React, { useState } from 'react';
import {
  Card,
} from '@zainx/design-system';
import type { Interview } from '@zainx/contracts';

interface InterviewCalendarProps {
  onSelectInterview?: (interview: Interview) => void;
}

export const InterviewCalendar: React.FC<InterviewCalendarProps> = () => {
  const [selectedTimezone, setSelectedTimezone] = useState('Africa/Cairo');

  return (
    <div className="space-y-4" data-testid="interview-calendar">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-xl font-bold tracking-tight">Interview Rounds & Schedule</h2>
          <p className="text-sm text-muted-foreground">
            Synchronized calendar view of interview panels, scorecards, and candidate availability.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <span className="text-xs text-muted-foreground">Display Timezone:</span>
          <select
            className="h-9 px-2.5 rounded-md border border-input bg-background text-xs"
            aria-label="Display Timezone"
            value={selectedTimezone}
            onChange={(e) => setSelectedTimezone(e.target.value)}
          >
            <option value="Africa/Cairo">Africa/Cairo (EET/EEST)</option>
            <option value="Asia/Riyadh">Asia/Riyadh (AST)</option>
            <option value="UTC">UTC</option>
          </select>
        </div>
      </div>

      <Card className="p-6">
        <div
          role="status"
          className="flex min-h-[280px] items-center justify-center rounded-lg border border-dashed border-border-default bg-surface-subtle p-6 text-center text-sm text-muted-foreground"
        >
          Interview schedule data is unavailable for the current context. No placeholder interviews are shown.
        </div>
      </Card>
    </div>
  );
};
