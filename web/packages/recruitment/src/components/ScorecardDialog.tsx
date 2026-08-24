import React, { useState } from 'react';
import {
  Button,
  Textarea,
} from '@zainx/design-system';
import {
  useSubmitScorecard,
} from '@zainx/contracts';
import { ScorecardRecommendation } from '../types';

interface ScorecardDialogProps {
  isOpen: boolean;
  interviewId: string;
  expectedRowVersion?: number;
  onClose: () => void;
  onSubmitted: () => void;
}

export const ScorecardDialog: React.FC<ScorecardDialogProps> = ({
  isOpen,
  interviewId,
  expectedRowVersion = 1,
  onClose,
  onSubmitted,
}) => {
  const submitMutation = useSubmitScorecard();

  const [ratings, setRatings] = useState({
    technicalCompetence: 4,
    problemSolving: 5,
    communication: 4,
    cultureFit: 4,
  });

  const [strengths, setStrengths] = useState('');
  const [concerns, setConcerns] = useState('');
  const [recommendation, setRecommendation] = useState<ScorecardRecommendation>('Yes');
  const [error, setError] = useState<string | null>(null);

  if (!isOpen) return null;

  const recommendationMap: Record<ScorecardRecommendation, number> = {
    StrongYes: 0,
    Yes: 1,
    Neutral: 2,
    No: 3,
    StrongNo: 4,
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    try {
      await submitMutation.mutateAsync({
        id: interviewId,
        data: {
          ratingsJson: JSON.stringify(ratings),
          strengths,
          concerns,
          recommendation: (recommendationMap[recommendation] ?? 1) as any,
          expectedRowVersion,
        },
      });
      onSubmitted();
    } catch (err: any) {
      setError(err?.response?.data?.detail || err.message || 'Failed to submit scorecard');
    }
  };

  return (
    <div
      className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 backdrop-blur-xs p-4"
      data-testid="scorecard-modal"
    >
      <div className="bg-card w-full max-w-lg rounded-xl border border-border shadow-2xl p-6 space-y-4">
        <div className="flex items-center justify-between border-b border-border pb-3">
          <h3 className="text-lg font-semibold">Submit Evaluation Scorecard</h3>
          <Button size="sm" variant="ghost" onClick={onClose}>
            ✕
          </Button>
        </div>

        {error && (
          <div className="p-3 text-sm rounded bg-destructive/15 text-destructive border border-destructive/30">
            {error}
          </div>
        )}

        <form onSubmit={handleSubmit} className="space-y-4">
          <div className="space-y-3">
            <h4 className="text-xs font-semibold text-muted-foreground uppercase tracking-wider">
              Structured Criteria Ratings (1 - 5)
            </h4>
            <div className="grid grid-cols-2 gap-3 text-sm">
              <div>
                <label htmlFor="range-tech" className="text-xs text-muted-foreground block mb-1">
                  Technical Competence ({ratings.technicalCompetence}/5)
                </label>
                <input
                  type="range"
                  min="1"
                  max="5"
                  className="w-full cursor-pointer accent-primary"
                  aria-label="Technical Competence"
                  value={ratings.technicalCompetence}
                  onChange={(e) =>
                    setRatings({ ...ratings, technicalCompetence: Number(e.target.value) })
                  }
                  id="range-tech"
                />
              </div>

              <div>
                <label htmlFor="range-problem" className="text-xs text-muted-foreground block mb-1">
                  Problem Solving ({ratings.problemSolving}/5)
                </label>
                <input
                  type="range"
                  min="1"
                  max="5"
                  className="w-full cursor-pointer accent-primary"
                  aria-label="Problem Solving"
                  value={ratings.problemSolving}
                  onChange={(e) =>
                    setRatings({ ...ratings, problemSolving: Number(e.target.value) })
                  }
                  id="range-problem"
                />
              </div>

              <div>
                <label htmlFor="range-comm" className="text-xs text-muted-foreground block mb-1">
                  Communication ({ratings.communication}/5)
                </label>
                <input
                  type="range"
                  min="1"
                  max="5"
                  className="w-full cursor-pointer accent-primary"
                  aria-label="Communication"
                  value={ratings.communication}
                  onChange={(e) =>
                    setRatings({ ...ratings, communication: Number(e.target.value) })
                  }
                  id="range-comm"
                />
              </div>

              <div>
                <label htmlFor="range-culture" className="text-xs text-muted-foreground block mb-1">
                  Culture & Collaboration ({ratings.cultureFit}/5)
                </label>
                <input
                  type="range"
                  min="1"
                  max="5"
                  className="w-full cursor-pointer accent-primary"
                  aria-label="Culture and Collaboration"
                  value={ratings.cultureFit}
                  onChange={(e) =>
                    setRatings({ ...ratings, cultureFit: Number(e.target.value) })
                  }
                  id="range-culture"
                />
              </div>
            </div>
          </div>

          <div>
            <label htmlFor="input-strengths" className="text-xs font-medium text-muted-foreground block mb-1">
              Key Strengths & Evidence
            </label>
            <Textarea
              placeholder="Demonstrated deep understanding of distributed locking and Npgsql connection pooling..."
              value={strengths}
              onChange={(e) => setStrengths(e.target.value)}
              id="input-strengths"
              rows={2}
              required
            />
          </div>

          <div>
            <label htmlFor="input-concerns" className="text-xs font-medium text-muted-foreground block mb-1">
              Concerns / Growth Areas
            </label>
            <Textarea
              placeholder="Less familiar with event-driven architecture nuances..."
              value={concerns}
              onChange={(e) => setConcerns(e.target.value)}
              id="input-concerns"
              rows={2}
            />
          </div>

          <div>
            <label htmlFor="select-recommendation" className="text-xs font-medium text-muted-foreground block mb-1">
              Hiring Recommendation
            </label>
            <select
              className="w-full h-10 px-3 rounded-md border border-input bg-background text-sm font-semibold"
              aria-label="Hiring Recommendation"
              value={recommendation}
              onChange={(e) => setRecommendation(e.target.value as ScorecardRecommendation)}
              id="select-recommendation"
            >
              <option value="StrongYes">Strong Yes (Exceptional candidate)</option>
              <option value="Yes">Yes (Meets bar)</option>
              <option value="Neutral">Neutral / Mixed</option>
              <option value="No">No (Does not meet bar)</option>
              <option value="StrongNo">Strong No (Definite reject)</option>
            </select>
          </div>

          <div className="flex items-center justify-end gap-2 border-t border-border pt-4">
            <Button variant="outline" type="button" onClick={onClose}>
              Cancel
            </Button>
            <Button
              variant="primary"
              type="submit"
              disabled={submitMutation.isPending}
              id="btn-submit-scorecard"
            >
              {submitMutation.isPending ? 'Submitting...' : 'Finalize & Submit'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};
