import { Auth0Client } from "@auth0/nextjs-auth0/server";

export const auth0 = new Auth0Client({
  authorizationParameters: {
    // Without an audience, Auth0 issues a token that is not for our API, and the API rejects it.
    audience: process.env.AUTH0_AUDIENCE,
    // offline_access is what makes Auth0 issue a refresh token.
    scope: "openid profile email offline_access",
  },
});