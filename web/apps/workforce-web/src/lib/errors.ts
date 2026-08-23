// Phase 1A: RFC 7807 ProblemDetails to UI Normalization

export type ErrorCategory = 
  | 'VALIDATION' 
  | 'AUTHORIZATION' 
  | 'NOT_FOUND' 
  | 'CONFLICT' 
  | 'BUSINESS_RULE' 
  | 'NETWORK' 
  | 'INTERNAL';

export interface NormalizedError {
  category: ErrorCategory;
  title: string;
  detail: string;
  correlationId?: string;
  traceId?: string;
  fieldErrors?: Record<string, string[]>;
}

export function normalizeError(error: any): NormalizedError {
  // If it's an Axios/Orval error with response
  if (error?.response?.data) {
    const problem = error.response.data;
    
    // Check if it looks like RFC 7807 ProblemDetails
    if (problem.type || problem.title) {
      let category: ErrorCategory = 'INTERNAL';
      
      switch (error.response.status) {
        case 400: category = 'VALIDATION'; break;
        case 401:
        case 403: category = 'AUTHORIZATION'; break;
        case 404: category = 'NOT_FOUND'; break;
        case 409: category = 'CONFLICT'; break;
        case 422: category = 'BUSINESS_RULE'; break;
      }
      
      return {
        category,
        title: problem.title || 'An error occurred',
        detail: problem.detail || 'The operation could not be completed.',
        correlationId: problem.extensions?.correlationId || error.response.headers?.['x-correlation-id'],
        traceId: problem.extensions?.traceId || error.response.headers?.['x-trace-id'],
        fieldErrors: problem.errors // Used in standard .NET 400 ValidationProblemDetails
      };
    }
  }
  
  // Network / timeout fallback
  if (error?.isAxiosError && !error.response) {
    return {
      category: 'NETWORK',
      title: 'Network Error',
      detail: 'Could not communicate with the server. Please check your connection.',
    };
  }

  // Unknown fallback
  return {
    category: 'INTERNAL',
    title: 'Unexpected Error',
    detail: error?.message || 'An unexpected error occurred.',
  };
}
