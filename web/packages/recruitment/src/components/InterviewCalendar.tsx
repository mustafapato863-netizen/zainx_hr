import React, { useState } from 'react';
import {
  Card,
  Badge,
  Button,
  Spinner,
} from '@zainx/design-system';
import {
  useGetInterviewsForApplication,
  Interview,
} from '@zainx/contracts';

interface InterviewCalendarProps {
  onSelectInterview?: (interview: Interview) => void;
}

export const InterviewCalendar: React.FC<InterviewCalendarProps> = ({
  onSelectInterview,
}) => {
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
        <div className="grid grid-cols-7 gap-2 border-b border-border pb-3 text-center text-xs font-semibold text-muted-foreground">
          <div>Sun</div>
          <div>Mon</div>
          <div>Tue</div>
          <div>Wed</div>
          <div>Thu</div>
          <div>Fri</div>
          <div>Sat</div>
        </div>

        <div className="grid grid-cols-7 gap-2 pt-3 min-h-[360px]">
          {Array.from({ length: 14 }).map((_, i) => (
            <div
              key={i}
              className="border border-border/60 rounded-lg p-2 min-h-[100px] bg-muted/10 space-y-1"
            >
              <span className="text-[11px] font-semibold text-muted-foreground">
                Day {i + 1}
              </span>
              {i === 2 && (
                <div
                  className="p-1.5 bg-primary/10 border border-primary/30 rounded text-[11px] font-medium text-primary cursor-pointer hover:bg-primary/20"
                  onClick={() =>
                    onSelectInterview?.({
                      id: 'demo-interview-1',
                      title: 'Senior Backend Architecture',
                      interviewType: 'Technical',
                      status: 'Scheduled',
                      scheduledStartUtc: new Date().toISOString(),
                      scheduledEndUtc: new Date().toISOString(),
                      timezone: selectedTimezone,
                    } as any)
                  }
                >
                  10:00 AM • System Design Panel
                </div>
              )}
              {i === 4 && (
                <div
                  className="p-1.5 bg-success/10 border border-success/30 rounded text-[11px] font-medium text-success cursor-pointer hover:bg-success/20"
                >
                  02:00 PM • VP Culture Interview
                </div>
              )}
            </div>
          ))}
        </div>
      </Card>
    </div>
  );
};
