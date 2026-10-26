import { NextRequest, NextResponse } from "next/server";
import { auth0 } from "@/lib/auth0";

const SAFE_METHODS = new Set(["GET", "HEAD"]);
const METHODS_WITH_BODY = new Set(["POST", "PUT", "PATCH"]);

function problem(status: number, title: string, detail: string) {
  return NextResponse.json({ title, status, detail }, { status });
}

async function handle(
  request: NextRequest,
  context: { params: Promise<{ path: string[] }> },
) {
  const apiUrl = process.env.TALLIX_API_URL;
  const appBaseUrl = process.env.APP_BASE_URL;

  if (!apiUrl || !appBaseUrl || !process.env.AUTH0_AUDIENCE) {
    return problem(
      500,
      "Server misconfigured",
      "TALLIX_API_URL, APP_BASE_URL and AUTH0_AUDIENCE must be set.",
    );
  }

  // Browsers attach cookies even to requests that another website triggers.
  // For anything that changes data, only accept requests coming from our own site.
  if (
    !SAFE_METHODS.has(request.method) &&
    request.headers.get("origin") !== new URL(appBaseUrl).origin
  ) {
    return problem(403, "Forbidden", "Cross-origin requests are not allowed.");
  }

  const { path } = await context.params;

  // Never let a request climb out of /api/ on the backend.
  if (path.some((segment) => segment === "." || segment === "..")) {
    return problem(400, "Bad request", "Invalid path.");
  }

  let token: string;
  try {
    token = (await auth0.getAccessToken()).token;
  } catch {
    return problem(401, "Not signed in", "Sign in to continue.");
  }

  const target = new URL(`/api/${path.map(encodeURIComponent).join("/")}`, apiUrl);
  target.search = request.nextUrl.search;

  // Forward only what the API needs. The browser's cookies never go to the API.
  const headers = new Headers({
    Authorization: `Bearer ${token}`,
    Accept: "application/json",
  });
  const contentType = request.headers.get("content-type");
  if (contentType) headers.set("Content-Type", contentType);

  let upstream: Response;
  try {
    upstream = await fetch(target, {
      method: request.method,
      headers,
      body: METHODS_WITH_BODY.has(request.method) ? await request.arrayBuffer() : undefined,
      cache: "no-store",
      signal: AbortSignal.timeout(15_000),
    });
  } catch {
    return problem(502, "API unavailable", "Could not reach the Tallix API.");
  }

  const responseHeaders = new Headers({ "Cache-Control": "no-store" });
  const upstreamType = upstream.headers.get("content-type");
  if (upstreamType) responseHeaders.set("Content-Type", upstreamType);

  return new Response(upstream.body, { status: upstream.status, headers: responseHeaders });
}

export const GET = handle;
export const POST = handle;
export const PUT = handle;
export const DELETE = handle;