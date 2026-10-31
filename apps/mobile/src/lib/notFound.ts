// "Whatever you asked for isn't there", whether the answer came from the server or the phone.
export class NotFoundError extends Error {
  constructor(message: string) {
    super(message);
    this.name = "NotFoundError";
  }
}