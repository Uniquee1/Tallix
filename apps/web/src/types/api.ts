// Matches UserDto in the API.
export type User = {
  id: string;
  email: string;
  displayName: string;
};

// What the API returns for errors (problem details).
export type ProblemDetails = {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
};