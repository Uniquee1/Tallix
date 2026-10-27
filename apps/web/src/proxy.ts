import { NextRequest, NextResponse } from "next/server";
import { auth0 } from "./lib/auth0";

export async function proxy(request: NextRequest) {
  // Always run the SDK first. It serves /auth/* and keeps the session cookie fresh.
  const authResponse = await auth0.middleware(request);
  const { pathname, search } = request.nextUrl;

  if (pathname === "/auth" || pathname.startsWith("/auth/")) {
    return authResponse;
  }

  // The API proxy answers its own 401 as JSON, which is what fetch() expects.
  if (pathname.startsWith("/api/")) {
    return authResponse;
  }

  const session = await auth0.getSession(request);

  // Behind a hosting platform the request's own URL may be an internal address,
  // so redirects are built from the configured public one when we have it.
  const base = process.env.APP_BASE_URL ?? request.url;

  if (pathname === "/") {
    // Signed-in visitors skip the landing page.
    return session ? NextResponse.redirect(new URL("/dashboard", base)) : authResponse;
  }

  // Everything else is private. Anything added later is protected by default.
  if (!session) {
    const returnTo = encodeURIComponent(pathname + search);
    return NextResponse.redirect(new URL(`/auth/login?returnTo=${returnTo}`, base));
  }

  return authResponse;
}

export const config = {
  matcher: [
    /*
     * Match all request paths except Next.js static assets and metadata files.
     */
    "/((?!_next/static|_next/image|favicon.ico|sitemap.xml|robots.txt).*)",
  ],
};