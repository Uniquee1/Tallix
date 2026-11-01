// A rule was broken (the server's HTTP 400).
export class ValidationError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "ValidationError";
  }
}

// The request is fine, but collides with existing data (the server's HTTP 409).
export class ConflictError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "ConflictError";
  }
}