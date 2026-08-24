export type RequisitionStatus =
  | 'Draft'
  | 'PendingApproval'
  | 'Approved'
  | 'Open'
  | 'OnHold'
  | 'Closed'
  | 'Cancelled';

export type StageKind =
  | 'Applied'
  | 'Screening'
  | 'Assessment'
  | 'Interview'
  | 'Offer'
  | 'Hired'
  | 'Rejected';

export type ApplicationStatus =
  | 'Active'
  | 'Withdrawn'
  | 'Rejected'
  | 'Hired';

export type InterviewType =
  | 'Screening'
  | 'Technical'
  | 'Behavioral'
  | 'Manager'
  | 'Panel';

export type InterviewStatus =
  | 'Scheduled'
  | 'Completed'
  | 'Cancelled'
  | 'Rescheduled'
  | 'NoShow';

export type ScorecardRecommendation =
  | 'StrongYes'
  | 'Yes'
  | 'Neutral'
  | 'No'
  | 'StrongNo';

export type OfferStatus =
  | 'Draft'
  | 'PendingApproval'
  | 'Approved'
  | 'Issued'
  | 'Accepted'
  | 'Declined'
  | 'Expired'
  | 'Withdrawn';
