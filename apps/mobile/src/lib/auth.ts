import Auth0 from "react-native-auth0";
import { config } from "./config";

const auth0 = new Auth0({ domain: config.auth0Domain, clientId: config.auth0ClientId });

// offline_access is what makes Auth0 issue a refresh token, so the app can renew
// its access token without asking the user to log in again.
const SCOPE = "openid profile email offline_access";

export async function login(): Promise<void> {
  const credentials = await auth0.webAuth.authorize(
    // The audience makes the access token one that OUR API accepts.
    { scope: SCOPE, audience: config.auth0Audience },
    { customScheme: config.authScheme },
  );

  await auth0.credentialsManager.saveCredentials(credentials);
}

export async function logout(): Promise<void> {
  try {
    await auth0.webAuth.clearSession({}, { customScheme: config.authScheme });
  } finally {
    // Whatever happened in the browser, nothing stays on the device.
    await auth0.credentialsManager.clearCredentials();
  }
}

export function hasSession(): Promise<boolean> {
  return auth0.credentialsManager.hasValidCredentials();
}

export async function getAccessToken(): Promise<string> {
  // Returns the stored token, or renews it with the refresh token when it has expired.
  const credentials = await auth0.credentialsManager.getCredentials();

  return credentials.accessToken;
}